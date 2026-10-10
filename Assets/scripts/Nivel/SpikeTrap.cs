using UnityEngine;

/// <summary>
/// Losa de pinchos: brilla (aviso), saltan los pinchos y lastiman a quien esté encima, luego se esconden.
/// Varias losas con desfases distintos forman oleadas que hay que leer para cruzar.
/// </summary>
public class SpikeTrap : MonoBehaviour
{
    public Vector2 size = new Vector2(2f, 2f);
    public float damage = 14f;
    public float idle = 1.6f, warn = 0.6f, up = 0.7f;
    public float offset;
    public Transform spikes;
    public Renderer plate;

    float t;
    float lastHurt = -9f;
    MaterialPropertyBlock mpb;
    static readonly int ColorId = Shader.PropertyToID("_BaseColor");

    void Start() { t = offset; mpb = new MaterialPropertyBlock(); }

    void Update()
    {
        float d = Hazards.FlatDistance(transform.position, out bool same);
        if (d > 20f) return;
        float cycle = idle + warn + up + 0.3f;
        t = (t + Time.deltaTime) % cycle;
        float y = -0.45f, glow = 0f;
        bool armed = false;
        if (t > idle && t < idle + warn) { glow = (t - idle) / warn; y = -0.4f + glow * 0.08f; }
        else if (t >= idle + warn && t < idle + warn + up) { glow = 1f; y = 0f; armed = true; }
        else if (t >= idle + warn + up) y = Mathf.Lerp(0f, -0.45f, (t - idle - warn - up) / 0.3f);
        if (spikes != null) spikes.localPosition = new Vector3(0f, y, 0f);
        if (plate != null)
        {
            plate.GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, Color.Lerp(new Color(0.18f, 0.16f, 0.2f), new Color(2.2f, 0.5f, 0.3f), glow * (armed ? 1f : 0.6f + 0.4f * Mathf.Sin(Time.time * 30f))));
            plate.SetPropertyBlock(mpb);
        }
        if (!armed || !same || Time.time - lastHurt < 0.6f || Hazards.Player == null) return;
        Vector3 lp = transform.InverseTransformPoint(Hazards.Player.position);
        if (Mathf.Abs(lp.x) < size.x / 2f && Mathf.Abs(lp.z) < size.y / 2f)
        {
            lastHurt = Time.time;
            Hazards.Hurt(transform.position, damage, 5f);
        }
    }
}
