using UnityEngine;

/// <summary>
/// GALERÍAS — Ráfagas de viento: cada pocos segundos el viento sopla hacia un lado y empuja a Abby (y se ven las
/// líneas de aire). Avisa un instante antes con un silbido de polvo. Cambia de lado entre ráfagas.
/// </summary>
public class GustZone : MonoBehaviour
{
    public Vector3 size = new Vector3(10f, 4f, 10f);
    public float strength = 2.5f;
    public float gust = 2f, calm = 3.5f;
    public float offset;
    public ParticleSystem streaks;

    float t;
    float dir = 1f;
    bool blowing;

    void Start() { t = offset; if (streaks != null) streaks.Stop(); }

    void Update()
    {
        t += Time.deltaTime;
        float cycle = gust + calm;
        if (t >= cycle) { t -= cycle; dir = -dir; }
        bool now = t < gust;
        if (now != blowing)
        {
            blowing = now;
            if (streaks != null)
            {
                streaks.transform.localRotation = Quaternion.Euler(0f, dir > 0 ? 90f : -90f, 0f);
                if (blowing) streaks.Play(); else streaks.Stop();
            }
        }
        if (!blowing) return;
        var motor = Hazards.Motor;
        if (motor == null) return;
        Vector3 lp = transform.InverseTransformPoint(motor.transform.position);
        if (Mathf.Abs(lp.x) > size.x / 2f || Mathf.Abs(lp.z) > size.z / 2f || lp.y < -1f || lp.y > size.y) return;
        var dash = motor.GetComponent<AbbyDash>();
        if (dash != null && dash.IsDashing) return;
        float k = Mathf.Sin(t / gust * Mathf.PI);   // sube y baja dentro de la ráfaga
        motor.Push(transform.right * dir * strength * k);
    }
}
