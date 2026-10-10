using UnityEngine;

/// <summary>
/// Plataforma que se desmorona: al pararse Abby tiembla, a los 'delay' segundos cae (sin colisión) y a los 'respawn'
/// segundos vuelve a su lugar. Hay que cruzarla rápido.
/// </summary>
public class CrumblePlatform : MonoBehaviour
{
    public float delay = 0.7f;
    public float respawn = 4f;
    public Vector2 top = new Vector2(2.4f, 2.4f);

    enum State { Firme, Temblando, Cayendo }
    State state;
    float t;
    Vector3 origin;
    Collider[] cols;
    Renderer[] rends;

    void Start()
    {
        origin = transform.localPosition;
        cols = GetComponentsInChildren<Collider>(true);
        rends = GetComponentsInChildren<Renderer>(true);
    }

    void Update()
    {
        switch (state)
        {
            case State.Firme:
            {
                var p = Hazards.Player;
                if (p == null) return;
                Vector3 lp = transform.InverseTransformPoint(p.position);
                if (Mathf.Abs(lp.x) < top.x / 2f + 0.1f && Mathf.Abs(lp.z) < top.y / 2f + 0.1f && lp.y > -0.2f && lp.y < 0.6f)
                {
                    state = State.Temblando; t = 0f;
                    CombatFx.Burst(transform.position, new Color(0.6f, 0.55f, 0.5f), 10, 2f, 0.1f, 0.5f);
                }
                break;
            }
            case State.Temblando:
                t += Time.deltaTime;
                transform.localPosition = origin + new Vector3(Mathf.Sin(t * 70f), 0f, Mathf.Cos(t * 63f)) * 0.04f * (t / delay);
                if (t >= delay)
                {
                    state = State.Cayendo; t = 0f;
                    foreach (var c in cols) if (c != null) c.enabled = false;
                    CombatFx.Burst(transform.position, new Color(0.6f, 0.55f, 0.5f), 24, 3f, 0.14f, 0.7f, 1f);
                }
                break;
            case State.Cayendo:
                t += Time.deltaTime;
                transform.localPosition = origin + Vector3.down * (t * t * 6f);
                if (t > 1f) foreach (var r in rends) if (r != null) r.enabled = false;
                if (t >= respawn)
                {
                    state = State.Firme;
                    transform.localPosition = origin;
                    foreach (var c in cols) if (c != null) c.enabled = true;
                    foreach (var r in rends) if (r != null) r.enabled = true;
                }
                break;
        }
    }
}
