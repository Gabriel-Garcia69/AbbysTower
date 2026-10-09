using UnityEngine;

/// <summary>
/// Maniquí de pruebas: recibe daño, se tambalea con parry y (opcional) ataca con telegraph amarillo
/// para probar bloqueo, parry y dash. Requiere Hitbox (con targetMask = capa del jugador) y un collider.
/// </summary>
[RequireComponent(typeof(Hitbox))]
public class TrainingDummy : MonoBehaviour, IHittable
{
    public float maxHealth = 100f;
    public Transform target;               // arrastra al jugador
    public bool attacks = true;
    public float attackRange = 3.5f;
    public float attackInterval = 2.5f;
    public float telegraphTime = 0.6f;
    public float activeTime = 0.15f;
    public float damage = 15f;
    public float knockDecay = 20f;

    enum Phase { Idle, Telegraph, Active }
    Phase phase;
    float health, timer, staggerTimer, flashTimer;
    Vector3 knock, facing = Vector3.forward;
    Hitbox hitbox;
    SpriteRenderer sr;
    Renderer rend;
    Color baseColor = Color.white;

    void Awake()
    {
        hitbox = GetComponent<Hitbox>();
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null) rend = GetComponentInChildren<Renderer>();
        baseColor = sr != null ? sr.color : (rend != null ? rend.material.color : Color.white);
        health = maxHealth;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        transform.position += knock * dt;
        knock = Vector3.MoveTowards(knock, Vector3.zero, knockDecay * dt);

        if (staggerTimer > 0f)
        {
            staggerTimer -= dt;
            phase = Phase.Idle; timer = 0f; hitbox.End();
            SetColor(Color.cyan);
            return;
        }

        flashTimer -= dt;
        Color idle = flashTimer > 0f ? Color.white : baseColor;

        if (target != null)
        {
            Vector3 d = target.position - transform.position; d.y = 0f;
            if (d.sqrMagnitude > 0.01f && phase == Phase.Idle) facing = d.normalized;
        }

        switch (phase)
        {
            case Phase.Idle:
                SetColor(idle);
                if (attacks && target != null && Vector3.Distance(target.position, transform.position) < attackRange)
                {
                    timer += dt;
                    if (timer >= attackInterval) { phase = Phase.Telegraph; timer = 0f; }
                }
                break;

            case Phase.Telegraph:
                SetColor(Color.yellow);
                timer += dt;
                if (timer >= telegraphTime)
                {
                    phase = Phase.Active; timer = 0f;
                    hitbox.Begin(new HitInfo { damage = damage, knockback = 6f, hitStop = 0.05f, sourcePosition = transform.position, attacker = this }, facing);
                }
                break;

            case Phase.Active:
                SetColor(Color.red);
                timer += dt;
                if (timer >= activeTime) { hitbox.End(); phase = Phase.Idle; timer = 0f; }
                break;
        }
    }

    public bool ReceiveHit(HitInfo hit)
    {
        health -= hit.damage;
        Vector3 away = transform.position - hit.sourcePosition; away.y = 0f;
        knock = away.normalized * hit.knockback;
        flashTimer = 0.08f;
        if (health <= 0f) { health = maxHealth; Debug.Log("Dummy derrotado (se reinicia)"); }
        return true;
    }

    public void Stagger(float duration) { staggerTimer = duration; }

    void SetColor(Color c)
    {
        if (sr != null) sr.color = c;
        else if (rend != null) rend.material.color = c;
    }
}
