using UnityEngine;

/// <summary>
/// Solo existe desde un ángulo: los renderers y colliders hijos aparecen cuando la cámara mira en la dirección
/// indicada (yaw en grados, ± tolerancia). Sirve para puentes "de perspectiva" y pasajes que solo ves al girar.
/// Si Abby está parada encima, no desaparece bajo sus pies hasta que se baja.
/// </summary>
public class AngleReveal : MonoBehaviour
{
    [Tooltip("Hacia dónde tiene que mirar la cámara (yaw del mundo en grados: 0 = norte/+Z, 90 = este).")]
    public float yaw;
    public float tolerance = 22f;
    [Tooltip("Abby sobre esta zona (local) mantiene el objeto sólido.")]
    public Vector3 holdSize = new Vector3(3f, 3f, 3f);

    Renderer[] rends;
    Collider[] cols;
    bool shown = true;
    float shimmer;

    void Start()
    {
        rends = GetComponentsInChildren<Renderer>(true);
        cols = GetComponentsInChildren<Collider>(true);
        Set(false, true);
    }

    void Update()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 f = cam.transform.forward; f.y = 0f;
        if (f.sqrMagnitude < 0.001f) return;
        float camYaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
        bool want = Mathf.Abs(Mathf.DeltaAngle(camYaw, yaw)) < tolerance;
        if (!want && shown && Hazards.Player != null)
        {
            Vector3 lp = transform.InverseTransformPoint(Hazards.Player.position);
            if (Mathf.Abs(lp.x) < holdSize.x / 2f && Mathf.Abs(lp.z) < holdSize.z / 2f && lp.y > -0.5f && lp.y < holdSize.y) want = true;
        }
        if (want != shown) Set(want, false);
    }

    void Set(bool on, bool silent)
    {
        shown = on;
        foreach (var r in rends) if (r != null) r.enabled = on;
        foreach (var c in cols) if (c != null) c.enabled = on;
        if (!silent && on) CombatFx.Burst(transform.position + Vector3.up * 0.3f, new Color(0.7f, 0.9f, 1f), 16, 2f, 0.1f, 0.6f, -0.1f);
    }
}
