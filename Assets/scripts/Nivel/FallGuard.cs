using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Si Abby cae al vacío (más abajo que killY), reaparece en el último suelo firme donde estuvo
/// (de hace ~1 s, para no reaparecer justo en el borde) y pierde un poco de vida.
/// Lo agrega FloorDirector al jugador.
/// </summary>
public class FallGuard : MonoBehaviour
{
    public float killY = -3f;
    public float damage = 10f;

    readonly Queue<Vector3> safe = new Queue<Vector3>();
    CharacterController cc;
    PlayerMotor motor;
    PlayerHealth health;
    float timer;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        motor = GetComponent<PlayerMotor>();
        health = GetComponent<PlayerHealth>();
    }

    void Start() => safe.Enqueue(transform.position);

    float bajoTierra;

    void Update()
    {
        // atorada debajo del terreno (al saltar de un risco se puede colar por la unión de dos pedazos del suelo): de vuelta
        bool debajo = DebajoDelTerreno();
        bajoTierra = debajo ? bajoTierra + Time.deltaTime : 0f;
        if (bajoTierra > 0.35f) { bajoTierra = 0f; Respawn("¡De vuelta al camino!", 0f, new Color(0.8f, 0.85f, 1f)); return; }
        // atorada en un hueco del que no se sale saltando (la ranura junto a una escalera, entre rocas o casas): de vuelta
        bool encerrada = Encerrada();
        if (encerrada && motor != null && motor.enabled)
        {
            if ((transform.position - marcaHueco).sqrMagnitude > 4f) { marcaHueco = transform.position; enHueco = 0f; }
            enHueco += Time.deltaTime;
            if (enHueco > 2.5f) { enHueco = 0f; Respawn("¡De vuelta al camino!", 0f, new Color(0.8f, 0.85f, 1f)); return; }
        }
        else { enHueco = 0f; marcaHueco = transform.position; }
        if (motor != null && motor.enabled && motor.IsGrounded && !debajo && !encerrada)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                timer = 0.25f;
                safe.Enqueue(transform.position);
                while (safe.Count > 5) safe.Dequeue();   // la más vieja es de hace ~1 s
            }
        }
        if (transform.position.y < killY) Respawn();
    }

    readonly RaycastHit[] hits = new RaycastHit[8];
    float enHueco;
    Vector3 marcaHueco;

    /// <summary>
    /// ¿Paredes alrededor más altas de lo que Abby alcanza saltando? Rayos en 8 direcciones a 1.75 m del suelo: si 6 o
    /// más chocan a menos de 1.3 m, está en una ranura o un pozo. (Un callejón o una puerta no cuentan: ahí chocan 2 o 4.)
    /// </summary>
    bool Encerrada()
    {
        Vector3 o = transform.position + Vector3.up * 1.75f;
        int paredes = 0;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI / 4f;
            var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            int n = Physics.RaycastNonAlloc(o, d, hits, 1.3f, ~0, QueryTriggerInteraction.Ignore);
            for (int k = 0; k < n; k++)
            {
                var c = hits[k].collider;
                if (c.transform.IsChildOf(transform) || c.GetComponentInParent<TowerEnemy>() != null) continue;
                paredes++;
                break;
            }
        }
        return paredes >= 6;
    }

    /// <summary>¿Hay terreno (no techos ni puentes: solo el suelo del mapa) más de 1.5 m por encima de sus pies?</summary>
    bool DebajoDelTerreno()
    {
        Vector3 p = transform.position;
        int n = Physics.RaycastNonAlloc(p + Vector3.up * 60f, Vector3.down, hits, 58.5f, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var c = hits[i].collider;
            bool terreno = c is TerrainCollider || (c is MeshCollider && (c.name.StartsWith("Terreno") || c.name.StartsWith("Terrain")));
            if (terreno && hits[i].normal.y > 0.3f && hits[i].point.y > p.y + 1.5f) return true;
        }
        return false;
    }

    /// <summary>Lava, pinchos del vacío…: vuelve al último suelo firme con daño y un aviso propio.</summary>
    public void Respawn(string message, float hurt, Color color)
    {
        pendingMsg = message; pendingDamage = hurt; pendingColor = color;
        Respawn();
    }

    string pendingMsg; float pendingDamage = -1f; Color pendingColor;

    void Respawn()
    {
        Vector3 to = safe.Count > 0 ? safe.Peek() : transform.position + Vector3.up * 10f;
        // sin suelo firme guardado: encima de lo que haya arriba (no 10 m más arriba, que podía seguir bajo el terreno)
        if (safe.Count == 0 && Physics.Raycast(transform.position + Vector3.up * 60f, Vector3.down, out var arriba, 120f, ~0, QueryTriggerInteraction.Ignore))
            to = arriba.point;
        if (cc != null) cc.enabled = false;
        transform.position = to + Vector3.up * 0.1f;
        if (cc != null) cc.enabled = true;
        safe.Clear(); safe.Enqueue(to);
        if (health == null) health = GetComponent<PlayerHealth>();
        float dano = pendingDamage >= 0f ? pendingDamage : damage;
        if (health != null && dano > 0f)
            health.ReceiveHit(new HitInfo { damage = pendingDamage >= 0f ? pendingDamage : damage, knockback = 0f, hitStop = 0f, sourcePosition = to });
        Color c = pendingDamage >= 0f ? pendingColor : new Color(0.8f, 0.6f, 1f);
        FloorDirector.Popup(to + Vector3.up * 2f, pendingMsg ?? "¡Caíste al vacío!", c, 1.5f);
        CombatFx.Burst(to + Vector3.up * 0.8f, c, 30, 4f, 0.15f, 0.6f, -0.3f);
        pendingMsg = null; pendingDamage = -1f;
    }
}
