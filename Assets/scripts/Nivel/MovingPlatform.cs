using UnityEngine;

/// <summary>
/// Plataforma que va y viene entre su posición inicial y 'travel' (local). Si Abby está parada encima, la lleva con ella.
/// Se mueve antes que el jugador (orden de ejecución) para que el arrastre no tiemble.
/// </summary>
[DefaultExecutionOrder(-200)]
public class MovingPlatform : MonoBehaviour
{
    public Vector3 travel = new Vector3(4f, 0f, 0f);
    public float period = 4f;
    public float phase;
    [Tooltip("Tamaño de la superficie (x, z) donde se considera que Abby va parada.")]
    public Vector2 top = new Vector2(2.4f, 2.4f);

    Vector3 origin, last;
    CharacterController cc;

    void Start() { origin = transform.localPosition; last = transform.position; }

    void Update()
    {
        float t = (Mathf.Sin((Time.time / Mathf.Max(0.5f, period) + phase) * Mathf.PI * 2f) + 1f) * 0.5f;
        transform.localPosition = origin + travel * Mathf.SmoothStep(0f, 1f, t);
        Vector3 delta = transform.position - last;
        last = transform.position;
        var p = Hazards.Player;
        if (p == null || delta.sqrMagnitude < 1e-8f) return;
        Vector3 lp = transform.InverseTransformPoint(p.position);
        if (Mathf.Abs(lp.x) < top.x / 2f + 0.2f && Mathf.Abs(lp.z) < top.y / 2f + 0.2f && lp.y > -0.2f && lp.y < 0.5f)
        {
            if (cc == null) cc = p.GetComponent<CharacterController>();
            if (cc != null && cc.enabled) cc.Move(delta);
        }
    }
}
