using UnityEngine;

/// <summary>
/// GULA — Fauces en el suelo: un círculo que se ilumina (aviso), y de golpe salen colmillos y muerde.
/// Ciclo: dormida → aviso (el anillo se enciende) → mordida → cierra. Desfasadas para que no muerdan todas juntas.
/// </summary>
public class MawTrap : MonoBehaviour
{
    public float radius = 1.3f;
    public float damage = 16f;
    public float idle = 2.6f, warn = 1f, bite = 0.35f;
    public float offset;
    public Transform teeth;      // sube al morder
    public Renderer ring;        // se enciende en el aviso

    float t;
    bool bitThisCycle;
    MaterialPropertyBlock mpb;
    static readonly int ColorId = Shader.PropertyToID("_BaseColor");

    void Start() { t = offset; mpb = new MaterialPropertyBlock(); }

    void Update()
    {
        // solo cuando Abby anda cerca (no muerde en salas vacías ni gasta tiempo)
        float d = Hazards.FlatDistance(transform.position, out bool same);
        if (d > 14f) return;
        float cycle = idle + warn + bite + 0.4f;
        t = (t + Time.deltaTime) % cycle;
        float k = 0f, teethY = -0.5f;
        if (t > idle && t < idle + warn) { k = (t - idle) / warn; teethY = -0.5f + k * 0.15f; bitThisCycle = false; }
        else if (t >= idle + warn && t < idle + warn + bite)
        {
            k = 1f; teethY = 0.15f;
            if (!bitThisCycle)
            {
                bitThisCycle = true;
                CameraShake.Shake(0.12f, 0.15f);
                CombatFx.Burst(transform.position + Vector3.up * 0.3f, new Color(0.9f, 0.3f, 0.25f), 14, 4f, 0.12f, 0.35f);
                if (d < radius && same) Hazards.Hurt(transform.position, damage, 7f);
            }
        }
        else if (t >= idle + warn + bite) teethY = Mathf.Lerp(0.15f, -0.5f, (t - idle - warn - bite) / 0.4f);
        if (teeth != null) teeth.localPosition = new Vector3(0f, teethY, 0f);
        if (ring != null)
        {
            ring.GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, Color.Lerp(new Color(0.5f, 0.12f, 0.08f), new Color(4f, 0.8f, 0.3f), k * (0.6f + 0.4f * Mathf.Sin(Time.time * 30f))));
            ring.SetPropertyBlock(mpb);
        }
    }
}
