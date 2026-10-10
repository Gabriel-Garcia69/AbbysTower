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

    TowerEnemy Prepare(TowerEnemy e)
    {
        e.ScaleHealth(healthMultiplier);
        if (e.IsBoss && !string.IsNullOrEmpty(bossName)) e.customName = bossName;
        return e;
    }
    public Transform player;   // vacío = busca el tag "Player"

    public State Current { get; private set; } = State.Esperando;
    public bool IsCleared => Current == State.Limpia;
    /// <summary>Enemigos vivos + los que faltan por llegar.</summary>
    public int Remaining => alive.Count + queue.Count;
    public int Total { get; private set; }
    public event Action Began;
    public event Action Cleared;

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
                if (player != null && PlayerInside()) Begin();
                break;

            case State.Combate:
                alive.RemoveAll(e => e == null || !e.activeInHierarchy);
                if (queue.Count > 0 && alive.Count < Mathf.Max(1, maxAlive) && Time.time >= nextSpawn)
                {
                    SpawnAt(queue.Dequeue());
                    nextSpawn = Time.time + 0.45f;
                }
                if (alive.Count == 0 && queue.Count == 0) Finish();
                break;
        }
    }

    bool PlayerInside()
    {
        Vector3 local = transform.InverseTransformPoint(player.position);
        return Mathf.Abs(local.x) < size.x / 2f - triggerInset && Mathf.Abs(local.z) < size.y / 2f - triggerInset;
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
        foreach (var d in doors) if (d != null) d.SetClosed(true);
        for (int i = 0; i < spawnPoints.Length; i++) if (spawnPoints[i] != null) queue.Enqueue(i);
        Total = queue.Count;
        int first = isBoss ? queue.Count : Mathf.Min(queue.Count, Mathf.Max(1, maxAlive));
        for (int i = 0; i < first; i++) SpawnAt(queue.Dequeue());
        nextSpawn = Time.time + 1f;
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
        var kind = isBoss ? TowerEnemy.Kind.Jefe : (index % 3 == 2 ? TowerEnemy.Kind.Bruto : TowerEnemy.Kind.Sombra);
        alive.Add(Prepare(TowerEnemy.Create(kind, sp.position, transform, this, player)).gameObject);
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
        Current = State.Limpia;
        foreach (var d in doors) if (d != null) d.SetClosed(false);
        Cleared?.Invoke();
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
