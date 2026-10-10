using UnityEngine;

/// <summary>
/// LUJURIA — Remolino de pétalos: atrae a Abby hacia el centro, donde hay un anillo de espinas que lastima.
/// Algo hermoso que te jala y te hiere. El impulso (dash) rompe la atracción por un momento.
/// </summary>
public class LustVortex : MonoBehaviour
{
    public float radius = 5f;
    public float pull = 3.2f;
    public float thornRadius = 1.1f;
    public float damage = 9f;
    public Transform spin;

    void Update()
    {
        if (spin != null) spin.Rotate(0f, 90f * Time.deltaTime, 0f);
        var motor = Hazards.Motor;
        if (motor == null) return;
        Vector3 to = transform.position - motor.transform.position;
        float dy = to.y; to.y = 0f;
        float d = to.magnitude;
        if (d > radius || Mathf.Abs(dy) > 1.6f) return;
        var dash = motor.GetComponent<AbbyDash>();
        if (dash != null && dash.IsDashing) return;
        // más fuerte cerca del centro, y con un poco de giro
        float k = 1f - d / radius;
        Vector3 dir = d > 0.01f ? to / d : Vector3.zero;
        Vector3 swirl = Vector3.Cross(Vector3.up, dir);
        motor.Push((dir * (0.4f + k) + swirl * 0.35f) * pull);
        if (d < thornRadius) Hazards.Hurt(transform.position, damage, 7f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.8f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
