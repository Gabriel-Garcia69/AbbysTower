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

    void Update()
    {
        if (motor != null && motor.enabled && motor.IsGrounded)
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

    void Respawn()
    {
        Vector3 to = safe.Count > 0 ? safe.Peek() : transform.position + Vector3.up * 10f;
        if (cc != null) cc.enabled = false;
        transform.position = to + Vector3.up * 0.1f;
        if (cc != null) cc.enabled = true;
        safe.Clear(); safe.Enqueue(to);
        if (health == null) health = GetComponent<PlayerHealth>();
        if (health != null)
            health.ReceiveHit(new HitInfo { damage = damage, knockback = 0f, hitStop = 0f, sourcePosition = to });
        FloorDirector.Popup(to + Vector3.up * 2f, "¡Caíste al vacío!", new Color(0.8f, 0.6f, 1f), 1.5f);
        CombatFx.Burst(to + Vector3.up * 0.8f, new Color(0.7f, 0.5f, 1f), 30, 4f, 0.15f, 0.6f, -0.3f);
    }
}
