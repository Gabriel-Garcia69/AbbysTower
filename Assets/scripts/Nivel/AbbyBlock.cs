using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bloqueo y stamina de Abby (GDD, Mecánica 02): con clic derecho sostenido Abby se cubre y recibe solo el 8 % del
/// daño, pero cada golpe bloqueado gasta 20 de stamina; sin stamina no puede bloquear. La stamina (100) se regenera a
/// 25 por segundo después de 1 s sin bloquear. Atacar no gasta stamina. Mientras bloquea camina más despacio y no ataca.
/// </summary>
[DefaultExecutionOrder(-90)]
public class AbbyBlock : MonoBehaviour
{
    public const float MaxStamina = 100f, CostoPorGolpe = 20f, Regeneracion = 25f, EsperaRegeneracion = 1f, DanoRecibido = 0.08f;

    public float Stamina { get; private set; } = MaxStamina;
    public float Normalized => Stamina / MaxStamina;
    /// <summary>Clic derecho sostenido y con stamina para aguantar al menos un golpe.</summary>
    public bool Blocking { get; private set; }
    /// <summary>Destello breve al bloquear un golpe (para el HUD y el sprite).</summary>
    public float BlockFlash { get; private set; }
    /// <summary>Quiso bloquear sin stamina (el HUD parpadea la barra).</summary>
    public float EmptyFlash { get; private set; }

    float sinBloquear;
    bool antes;
    PlayerHealth health;

    PlayerMotor motor;

    void Awake() { health = GetComponent<PlayerHealth>(); motor = GetComponent<PlayerMotor>(); }

    void Update()
    {
        float dt = Time.deltaTime;
        var ms = Mouse.current; var gp = Gamepad.current;
        bool held = !PauseMenu.IsPaused && LoreNote.Open == null && (health == null || !health.IsDead) &&
                    ((ms != null && ms.rightButton.isPressed) || (gp != null && gp.leftTrigger.isPressed));
        bool wanted = held;
        Blocking = held && Stamina >= CostoPorGolpe;
        if (wanted && !Blocking && (ms != null && ms.rightButton.wasPressedThisFrame)) EmptyFlash = 1f;

        if (Blocking && !antes) CombatFx.Ring(transform.position, 1.3f, new Color(0.5f, 0.75f, 1f, 0.8f), 0.18f);   // se cubre
        antes = Blocking;
        if (Blocking)
        {
            sinBloquear = 0f;
            if (motor != null) motor.Slow(0.45f, 0.05f);   // cubierta, camina despacio
        }
        else
        {
            sinBloquear += dt;
            if (sinBloquear >= EsperaRegeneracion) Stamina = Mathf.Min(MaxStamina, Stamina + Regeneracion * dt);
        }
        BlockFlash = Mathf.MoveTowards(BlockFlash, 0f, dt * 4f);
        EmptyFlash = Mathf.MoveTowards(EmptyFlash, 0f, dt * 2f);
    }

    /// <summary>Lo llama PlayerHealth al recibir un golpe: si está bloqueando, gasta stamina y reduce el daño.</summary>
    public bool TryBlock(ref HitInfo hit)
    {
        if (!Blocking) return false;
        Stamina = Mathf.Max(0f, Stamina - CostoPorGolpe);
        sinBloquear = 0f;
        hit.damage *= DanoRecibido;
        hit.knockback *= 0.35f;
        BlockFlash = 1f;
        Sonido.Tocar(Sonido.Efecto.Bloqueo, 0.9f);
        if (Stamina < CostoPorGolpe) Blocking = false;
        CombatFx.Burst(transform.position + Vector3.up * 0.9f, new Color(0.55f, 0.75f, 1f), 18, 4f, 0.12f, 0.3f);
        CombatFx.Ring(transform.position, 1.6f, new Color(0.5f, 0.75f, 1f, 0.9f), 0.25f);
        FloorDirector.Popup(transform.position + Vector3.up * 2.1f, "BLOQUEO", new Color(0.6f, 0.8f, 1f));
        return true;
    }

    /// <summary>Al reiniciar un piso o recibir la fuente.</summary>
    public void Refill() => Stamina = MaxStamina;
}
