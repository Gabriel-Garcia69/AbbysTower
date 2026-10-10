using UnityEngine;

/// <summary>
/// Fragmento de torre que suelta un enemigo al morir: salta hacia afuera, flota girando y, cuando Abby se acerca,
/// vuela hacia ella y suma a RunState. Se crea con ShardPickup.Drop.
/// </summary>
public class ShardPickup : MonoBehaviour
{
    public int value = 1;

    static Material mat;
    static readonly Color ShardColor = new Color(0.45f, 0.95f, 1f);

    Transform player;
    Vector3 velocity;
    float age, groundY;
    bool magnet;

    /// <summary>Suelta 'total' fragmentos repartidos en varias piezas.</summary>
    public static void Drop(Vector3 pos, int total)
    {
        int pieces = Mathf.Clamp(total, 1, 8);
        int each = Mathf.Max(1, total / pieces), rest = total - each * pieces;
        for (int i = 0; i < pieces; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Fragmento";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = pos + Vector3.up * 0.6f;
            go.transform.localScale = new Vector3(0.18f, 0.32f, 0.18f);
            go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 45f);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Fragmento (runtime)" };
                Color hdr = ShardColor * 4f; hdr.a = 1f;
                mat.SetColor("_BaseColor", hdr);
            }
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var s = go.AddComponent<ShardPickup>();
            s.value = each + (i < rest ? 1 : 0);
            float a = Random.Range(0f, Mathf.PI * 2f);
            s.velocity = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(1.5f, 3.5f) + Vector3.up * Random.Range(3.5f, 5.5f);
            s.groundY = pos.y + 0.35f;
        }
    }

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        age += dt;
        transform.Rotate(0f, 220f * dt, 0f, Space.World);

        Vector3 target = player != null ? player.position + Vector3.up * 0.8f : transform.position;
        if (!magnet && player != null && age > 0.5f && (target - transform.position).sqrMagnitude < 4.5f * 4.5f) magnet = true;

        if (magnet)
        {
            Vector3 d = target - transform.position;
            velocity = Vector3.Lerp(velocity, d.normalized * Mathf.Max(8f, d.magnitude * 6f), 10f * dt);
            transform.position += velocity * dt;
            if (d.sqrMagnitude < 0.45f * 0.45f)
            {
                RunState.AddFragments(value);
                CombatFx.Burst(transform.position, ShardColor, 6, 2f, 0.1f, 0.25f, 0f);
                Destroy(gameObject);
            }
            return;
        }

        // salto inicial y luego flota en su lugar
        velocity.y -= 14f * dt;
        transform.position += velocity * dt;
        if (transform.position.y < groundY)
        {
            transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
            velocity = Vector3.zero;
        }
        if (velocity == Vector3.zero)
            transform.position = new Vector3(transform.position.x, groundY + Mathf.Sin(age * 3f) * 0.08f, transform.position.z);
        if (age > 40f) Destroy(gameObject);
    }
}
