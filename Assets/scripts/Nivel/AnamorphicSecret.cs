using UnityEngine;

/// <summary>
/// Figura anamórfica: piezas sueltas flotando que solo forman un símbolo cuando la cámara mira exactamente en la
/// dirección 'viewDir' y Abby está parada en el punto marcado. Al alinearse brillan y se abre la recompensa.
/// FloorLayout coloca las piezas (proyección paralela a lo largo de viewDir).
/// </summary>
public class AnamorphicSecret : MonoBehaviour
{
    public string id = "";
    public Vector3 viewDir = new Vector3(0f, -0.57f, 0.82f);
    public float tolerance = 7f;
    [Tooltip("Dónde tiene que estar parada Abby (local) y con qué radio.")]
    public Vector3 standPoint;
    public float standRadius = 2.2f;
    public Renderer[] pieces;
    public GameObject reward;
    public string message = "Las piezas encajan";

    bool solved;
    float glow;
    MaterialPropertyBlock mpb;
    static readonly int EmissionId = Shader.PropertyToID("_BaseColor");

    void Start()
    {
        if (reward != null) reward.SetActive(false);
        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (solved || Hazards.Player == null) return;
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 sp = transform.TransformPoint(standPoint);
        Vector3 d = Hazards.Player.position - sp; d.y = 0f;
        bool standing = d.magnitude < standRadius;
        float angle = Vector3.Angle(cam.transform.forward, transform.TransformDirection(viewDir).normalized);
        float target = standing ? Mathf.Clamp01(1f - (angle - tolerance) / 20f) : 0f;   // se iluminan al acercarse al ángulo
        glow = Mathf.MoveTowards(glow, target, Time.deltaTime * 2f);
        foreach (var p in pieces)
        {
            if (p == null) continue;
            p.GetPropertyBlock(mpb);
            mpb.SetColor(EmissionId, Color.Lerp(new Color(0.15f, 0.15f, 0.2f), new Color(3f, 2.6f, 1.4f), glow));
            p.SetPropertyBlock(mpb);
        }
        if (standing && angle < tolerance)
        {
            solved = true;
            SecretTracker.Mark("escondite:" + id);
            FloorDirector.Banner("¡PERSPECTIVA!", message, new Color(1f, 0.9f, 0.5f), 3.5f);
            foreach (var p in pieces) if (p != null) CombatFx.Burst(p.transform.position, new Color(1f, 0.9f, 0.5f), 6, 3f, 0.1f, 0.6f, 0f);
            if (reward != null) { reward.SetActive(true); CombatFx.Burst(reward.transform.position + Vector3.up, new Color(1f, 0.9f, 0.5f), 40, 5f, 0.16f, 1f, -0.2f); }
            CameraShake.Shake(0.2f, 0.4f);
        }
    }
}
