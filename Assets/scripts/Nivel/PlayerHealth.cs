using System;
using UnityEngine;

/// <summary>
/// Vida de Abby para los pisos de la torre (funciona con PlayerMotor + CharacterController).
/// Recibe golpes de los enemigos por IHittable: retroceso, parpadeo de invulnerabilidad y sacudida de cámara.
/// Al morir apaga el input; FloorDirector se encarga de reiniciar el piso.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerHealth : MonoBehaviour, IHittable
{
    public float maxHealth = 100f;
    [Tooltip("Segundos sin recibir daño después de un golpe.")]
    public float invulnTime = 0.7f;
    public float knockDecay = 22f;

    public float Health { get; private set; }
    public bool IsDead { get; private set; }
    public float Normalized => Health / Mathf.Max(1f, maxHealth);
    public event Action<float> Damaged;
    public event Action Healed;
    public event Action Died;

    CharacterController cc;
    SpriteRenderer[] sprites;
    Vector3 knock;
    float invuln, hurtFlash;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        Health = maxHealth;
    }

    void Start() => sprites = GetComponentsInChildren<SpriteRenderer>(true);

    void Update()
    {
        float dt = Time.deltaTime;
        if (knock.sqrMagnitude > 0.01f && cc.enabled)
        {
            cc.Move(knock * dt);
            knock = Vector3.MoveTowards(knock, Vector3.zero, knockDecay * dt);
        }

        if (invuln > 0f) invuln -= dt;
        if (hurtFlash > 0f) hurtFlash -= dt;
        Color c = Color.white;
        if (hurtFlash > 0f) c = new Color(1f, 0.35f, 0.35f);
        if (invuln > 0f && !IsDead) c.a = Mathf.Repeat(Time.time * 12f, 1f) < 0.5f ? 0.35f : 1f;
        if (sprites != null) foreach (var s in sprites) if (s != null) s.color = c;
    }

    public bool ReceiveHit(HitInfo hit)
    {
        if (IsDead || invuln > 0f || hit.attacker == (IHittable)this) return false;
        Health = Mathf.Max(0f, Health - hit.damage);
        Vector3 away = transform.position - hit.sourcePosition; away.y = 0f;
        knock = (away.sqrMagnitude > 0.001f ? away.normalized : -transform.forward) * hit.knockback;
        invuln = invulnTime;
        hurtFlash = 0.15f;
        CameraShake.Shake(0.22f, 0.25f);
        CombatFx.Burst(transform.position + Vector3.up * 0.8f, new Color(1f, 0.35f, 0.4f), 14, 4f, 0.14f, 0.4f);
        FloorDirector.Popup(transform.position + Vector3.up * 1.8f, "-" + Mathf.RoundToInt(hit.damage), new Color(1f, 0.4f, 0.4f));
        Damaged?.Invoke(hit.damage);
        if (Health <= 0f) Die();
        return true;
    }

    public void Stagger(float duration) { }

    public void SetMaxHealth(float value, bool fill)
    {
        maxHealth = Mathf.Max(1f, value);
        Health = fill ? maxHealth : Mathf.Min(Health, maxHealth);
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        Health = Mathf.Min(maxHealth, Health + amount);
        CombatFx.Burst(transform.position + Vector3.up * 0.6f, new Color(0.4f, 1f, 0.6f), 30, 3f, 0.16f, 0.9f, -0.3f);
        FloorDirector.Popup(transform.position + Vector3.up * 1.8f, "+" + Mathf.RoundToInt(amount), new Color(0.5f, 1f, 0.6f));
        Healed?.Invoke();
    }

    void Die()
    {
        IsDead = true;
        foreach (var b in GetComponents<MonoBehaviour>())
            if (b is PlayerInputReader || b is AbbyAttack) b.enabled = false;
        CameraShake.Shake(0.4f, 0.5f);
        CombatFx.Burst(transform.position + Vector3.up * 0.8f, new Color(0.8f, 0.6f, 1f), 50, 6f, 0.2f, 1f);
        Died?.Invoke();
    }
}
