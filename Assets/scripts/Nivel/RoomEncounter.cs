using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Combate de una sala: cuando Abby entra por completo, se cierran las puertas y aparece la horda
/// (un enemigo por SpawnPoint). Al morir todos, se abren las puertas y la sala queda limpia.
/// Un enemigo cuenta como muerto cuando su GameObject se destruye o se desactiva.
///
/// Sin enemyPrefab usa los enemigos integrados (TowerEnemy: sombras y brutos; en la sala del jefe, el Guardián).
/// Si hay más enemigos que maxAlive, llegan por oleadas: cuando cae uno, aparece el siguiente.
/// Con builtInEnemies apagado y sin prefab, la sala no se cierra (para recorrer el piso).
/// La crea FloorLayout; se puede ajustar a mano después.
/// </summary>
public class RoomEncounter : MonoBehaviour
{
    public enum State { Esperando, Combate, Limpia }

    [Tooltip("Área jugable de la sala (x, z) centrada en este objeto.")]
    public Vector2 size = new Vector2(16f, 12f);
    [Tooltip("Qué tan adentro tiene que estar Abby para que empiece el combate (así no queda atrapada en la puerta).")]
    public float triggerInset = 2f;
    public LevelDoor[] doors;
    public Transform[] spawnPoints;
    public GameObject enemyPrefab;
    public bool isBoss;
    [Tooltip("Sin prefab: usa TowerEnemy (sombras, brutos y el jefe).")]
    public bool builtInEnemies = true;
    [Tooltip("Cuántos enemigos a la vez; el resto llega por oleadas.")]
    public int maxAlive = 4;
    [Tooltip("Nombre que muestra el HUD al entrar.")]
    public string displayName = "";
    [Tooltip("Multiplica la vida de los enemigos integrados.")]
    public float healthMultiplier = 1f;
    [Tooltip("Nombre del jefe (si esta sala es la del jefe).")]
    public string bossName = "";

    [Tooltip("Los enemigos aparecen dormidos y despiertan al acercarse Abby (piso de la Pereza).")]
    public bool sleepingEnemies;
    [Tooltip("Tipos de enemigo por orden de aparición (se repite). Vacío = sombras y cada 3er enemigo un bruto.")]
    public TowerEnemy.Kind[] roster = new TowerEnemy.Kind[0];
    [Tooltip("Patrón del jefe: 0 Guardián, 1 Capitán, 2 Gárgola, 3 Geoda, 4 Guardián Dorado, 5 Escarcha, 6 Arquitecto.")]
    public int bossStyle;
    [Tooltip("Qué jefe aparece en la sala del jefe (Jefe = los guardianes de siempre; Dante = jefe final del GDD).")]
    public TowerEnemy.Kind bossKind = TowerEnemy.Kind.Jefe;
    [Tooltip("No empieza hasta que estas salas estén limpias (GDD: el jefe final llega después de las 4 hordas).")]
    public RoomEncounter[] esperaA = new RoomEncounter[0];
    [Tooltip("Pisos abiertos: cuántos metros pueden salir los enemigos de la arena persiguiendo a Abby (siguen el suelo: " +
             "suben escaleras y puentes). 0 = sala cerrada de la torre, se quedan dentro.")]
    public float leash;
    [Tooltip("Con correa: si Abby se aleja tanto más allá de la correa, la pelea se suspende y los enemigos se van.")]
    public float escapeDistance = 16f;

    TowerEnemy Prepare(TowerEnemy e)
    {
        e.ScaleHealth(healthMultiplier);
        if (sleepingEnemies && !e.IsBoss) e.asleep = true;
        if (e.IsBoss && !string.IsNullOrEmpty(bossName)) e.customName = bossName;
        return e;
    }
    public Transform player;   // vacío = busca el tag "Player"

    public State Current { get; private set; } = State.Esperando;
    /// <summary>Peleas en curso en la escena (las tomas de cámara no interrumpen una pelea).</summary>
    public static int ActiveCombats { get; private set; }
    public bool IsCleared => Current == State.Limpia;
    /// <summary>Enemigos vivos + los que faltan por llegar.</summary>
    public int Remaining => alive.Count + queue.Count;
    public int Total { get; private set; }
    public event Action Began;
    public event Action Cleared;
    /// <summary>Abby se fue lejos y la pelea se suspendió (pisos abiertos).</summary>
    public event Action Escaped;

    readonly List<GameObject> alive = new List<GameObject>();
    readonly Queue<int> queue = new Queue<int>();   // índices de spawn points pendientes
    float nextSpawn;

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
        foreach (var d in doors) if (d != null) d.SetClosed(false);
    }

    void Update()
    {
        switch (Current)
        {
            case State.Esperando:
                if (player != null && PlayerInside() && Listo()) Begin();
                else if (player != null && PlayerInside() && Time.time > avisoListo)
                {
                    avisoListo = Time.time + 4f;
                    FloorDirector.Banner("AÚN NO", "Quedan hordas por derrotar en este piso", new Color(0.8f, 0.7f, 1f), 2.2f);
                }
                break;

            case State.Combate:
                alive.RemoveAll(e => e == null || !e.activeInHierarchy);
                if (queue.Count > 0 && alive.Count < Mathf.Max(1, maxAlive) && Time.time >= nextSpawn)
                {
                    SpawnAt(queue.Dequeue());
                    nextSpawn = Time.time + 0.45f;
                }
                if (alive.Count == 0 && queue.Count == 0) Finish();
                else if (leash > 0f && player != null) CheckEscape();
                break;
        }
    }

    float avisoListo;

    bool Listo()
    {
        if (esperaA != null) foreach (var r in esperaA) if (r != null && !r.IsCleared) return false;
        return true;
    }

    bool PlayerInside()
    {
        Vector3 local = transform.InverseTransformPoint(player.position);
        if (Mathf.Abs(local.x) < size.x / 2f - triggerInset && Mathf.Abs(local.z) < size.y / 2f - triggerInset) return true;
        // también al llegar cerca de la marca (por si la arena quedó junto a una cerca o un río y no se puede pisar el centro)
        Vector3 d = player.position - transform.position;
        return !isBoss && new Vector2(d.x, d.z).magnitude < 7f && Mathf.Abs(d.y) < 4f;
    }

    float escapeTimer;

    /// <summary>Abby huyó lejos (pisos abiertos): tras unos segundos la pelea se suspende y se reinicia al volver.</summary>
    void CheckEscape()
    {
        Vector3 local = transform.InverseTransformPoint(player.position);
        float m = leash + escapeDistance;
        bool far = Mathf.Abs(local.x) > size.x / 2f + m || Mathf.Abs(local.z) > size.y / 2f + m;
        escapeTimer = far ? escapeTimer + Time.deltaTime : 0f;
        if (escapeTimer < 3f) return;
        escapeTimer = 0f;
        foreach (var e in alive) if (e != null) Destroy(e);
        alive.Clear();
        queue.Clear();
        ActiveCombats = Mathf.Max(0, ActiveCombats - 1);
        foreach (var d in doors) if (d != null) d.SetClosed(false);
        Current = State.Esperando;
        Escaped?.Invoke();
    }

    void Begin()
    {
        if ((enemyPrefab == null && !builtInEnemies) || spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.Log($"{name}: sin enemigos asignados todavía, la sala se da por limpia.", this);
            Finish();
            return;
        }

        Current = State.Combate;
        ActiveCombats++;
        foreach (var d in doors) if (d != null) d.SetClosed(true);
        for (int i = 0; i < spawnPoints.Length; i++) if (spawnPoints[i] != null) queue.Enqueue(i);
        Total = queue.Count;
        int first = isBoss ? queue.Count : Mathf.Min(queue.Count, Mathf.Max(1, maxAlive));
        for (int i = 0; i < first; i++) SpawnAt(queue.Dequeue());
        nextSpawn = Time.time + 1f;
        if (isBoss && bossKind == TowerEnemy.Kind.Dante)
        {
            ReintentoJefe.Escena = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Musica.Poner(Musica.Pista.Jefe);
        }
        Began?.Invoke();
    }

    void SpawnAt(int index)
    {
        Transform sp = spawnPoints[index];
        if (enemyPrefab != null)
        {
            alive.Add(Instantiate(enemyPrefab, sp.position, sp.rotation, transform));
            return;
        }
        var kind = isBoss ? bossKind
                 : roster != null && roster.Length > 0 ? roster[index % roster.Length]
                 : (index % 3 == 2 ? TowerEnemy.Kind.Bruto : TowerEnemy.Kind.Sombra);
        Vector3 pos = isBoss || player == null ? sp.position : PuntoCercaDeAbby(sp.position, index);
        var e = TowerEnemy.Create(kind, pos, transform, this, player, isBoss ? bossStyle : 0);
        alive.Add(Prepare(e).gameObject);
    }

    /// <summary>
    /// Dónde sale un enemigo de la horda: alrededor de Abby (5.5 a 8 m), en suelo firme a su altura y sin nada en medio
    /// (cerca, casa, río, orilla). Así ninguna horda queda imposible por salir detrás de algo. Si no hay hueco, su punto.
    /// </summary>
    Vector3 PuntoCercaDeAbby(Vector3 respaldo, int index)
    {
        Vector3 c = player.position;
        for (int intento = 0; intento < 12; intento++)
        {
            float ang = (index * 2.4f + intento * 0.9f) % (Mathf.PI * 2f);
            float rad = 5.5f + (intento % 3) * 1.2f;
            Vector3 q = c + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * rad;
            if (!Physics.Raycast(q + Vector3.up * 6f, Vector3.down, out var hit, 14f, ~0, QueryTriggerInteraction.Ignore)) continue;
            if (Mathf.Abs(hit.point.y - c.y) > 1.2f) continue;                     // cauce del río, risco o techo
            Vector3 a = c + Vector3.up * 1f, b = hit.point + Vector3.up * 1f;
            if (Physics.Linecast(a, b, ~0, QueryTriggerInteraction.Ignore)) continue;   // algo en medio
            return hit.point + Vector3.up * 0.05f;
        }
        return respaldo;
    }

    /// <summary>Agrega un enemigo a la pelea en curso (invocaciones del jefe).</summary>
    public void SpawnExtra(TowerEnemy.Kind kind, Vector3 position)
    {
        if (Current != State.Combate) return;
        alive.Add(Prepare(TowerEnemy.Create(kind, position, transform, this, player)).gameObject);
        Total++;
    }

    void Finish()
    {
        if (Current == State.Combate) ActiveCombats = Mathf.Max(0, ActiveCombats - 1);
        Current = State.Limpia;
        foreach (var d in doors) if (d != null) d.SetClosed(false);
        Cleared?.Invoke();
    }

    void OnDestroy()
    {
        if (Current == State.Combate) ActiveCombats = Mathf.Max(0, ActiveCombats - 1);
    }

    /// <summary>Para pruebas: mata a todos los enemigos vivos de la sala.</summary>
    [ContextMenu("Limpiar sala (debug)")]
    public void DebugClear()
    {
        foreach (var e in alive) if (e != null) Destroy(e);
        alive.Clear();
        queue.Clear();
        if (Current != State.Limpia) Finish();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = isBoss ? new Color(1f, 0.25f, 0.25f) : new Color(0.4f, 0.8f, 1f);
        Gizmos.DrawWireCube(new Vector3(0f, 1f, 0f), new Vector3(size.x - 2 * triggerInset, 2f, size.y - 2 * triggerInset));
        if (spawnPoints == null) return;
        Gizmos.matrix = Matrix4x4.identity;
        foreach (var sp in spawnPoints) if (sp != null) Gizmos.DrawWireSphere(sp.position + Vector3.up * 0.5f, 0.5f);
    }
}
