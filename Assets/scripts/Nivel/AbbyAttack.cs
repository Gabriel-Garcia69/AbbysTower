using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Ataque cuerpo a cuerpo de Abby para los pisos (sobre PlayerMotor): combo de 3 tajos con J / clic izquierdo / X del control.
/// Usa el Hitbox del sistema de combate (golpea cualquier IHittable) y apunta hacia donde mira Abby.
/// El tercer golpe es más fuerte, empuja más y congela un instante (hit-stop).
/// </summary>
[RequireComponent(typeof(Hitbox))]
public class AbbyAttack : MonoBehaviour
{
    public float[] damage = { 14f, 14f, 26f };
    public float[] knockback = { 5f, 5f, 11f };
    public float windup = 0.05f;
    public float active = 0.12f;
    public float recover = 0.16f;
    [Tooltip("Tiempo después de un golpe para encadenar el siguiente.")]
    public float comboWindow = 0.35f;
    [Tooltip("Pasito hacia adelante en cada golpe (m/s mientras está activo).")]
    public float lunge = 5f;
    public Color slashColor = new Color(0.6f, 0.95f, 1f, 1f);

    public bool Busy => phase != Phase.None;
    public Vector3 Facing { get; private set; } = Vector3.forward;

    enum Phase { None, Windup, Active, Recover }
    Phase phase;
    float timer, comboTimer;
    int combo;
    bool queued;
    Hitbox hitbox;
    PlayerMotor motor;
    CharacterController cc;
    IHittable self;

    void Awake()
    {
        hitbox = GetComponent<Hitbox>();
        hitbox.owner = transform;
        hitbox.offset = new Vector3(0f, 0.7f, 0.95f);
        hitbox.halfExtents = new Vector3(0.95f, 0.7f, 0.85f);
        motor = GetComponent<PlayerMotor>();
        cc = GetComponent<CharacterController>();
        self = GetComponent<IHittable>();
        dash = GetComponent<AbbyDash>();
    }

    void Start()
    {
        if (dash == null) dash = GetComponent<AbbyDash>();
        var health = GetComponent<PlayerHealth>();
        if (health != null) health.PerfectDodge += OnPerfectDodge;
    }

    void OnDestroy()
    {
        var health = GetComponent<PlayerHealth>();
        if (health != null) health.PerfectDodge -= OnPerfectDodge;
    }

    /// <summary>Esquiva perfecta: cámara lenta un instante y el siguiente golpe es crítico.</summary>
    void OnPerfectDodge()
    {
        critUntil = Time.time + 1.6f;
        HitStop.Do(0.35f, 0.25f);
        CombatFx.Burst(transform.position + Vector3.up * 0.8f, new Color(1f, 0.5f, 0.95f), 25, 4f, 0.15f, 0.5f, 0f);
        FloorDirector.Popup(transform.position + Vector3.up * 2.2f, "¡ESQUIVA PERFECTA!", new Color(1f, 0.6f, 1f), 1.2f);
    }

    AbbyDash dash;
    float critUntil;

    void OnDisable() { hitbox.End(); phase = Phase.None; }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dash != null && dash.IsDashing) Facing = dash.Direction;
        else if (motor != null && motor.WorldMoveDirection.sqrMagnitude > 0.01f) Facing = motor.WorldMoveDirection;

        if (!PauseMenu.IsPaused && Pressed())
        {
            if (phase == Phase.None) Begin();
            else queued = true;
        }

        if (comboTimer > 0f) { comboTimer -= dt; if (comboTimer <= 0f) combo = 0; }
        if (phase == Phase.None) return;

        timer += dt;
        switch (phase)
        {
            case Phase.Windup:
                if (timer >= windup)
                {
                    phase = Phase.Active; timer = 0f;
                    int i = Mathf.Clamp(combo, 0, damage.Length - 1);
                    bool last = i == damage.Length - 1;
                    // tajo de impulso: atacar durante el dash o justo al terminarlo
                    bool dashStrike = dash != null && (dash.IsDashing || Time.time - dash.LastDashEnd < 0.2f);
                    if (dashStrike) Facing = dash.Direction;
                    // crítico: después de una esquiva perfecta
                    bool crit = Time.time < critUntil;
                    float mult = RunState.DamageMultiplier * (dashStrike ? 1.6f : 1f) * (crit ? 2f : 1f);
                    if (crit) critUntil = 0f;
                    hitbox.Begin(new HitInfo
                    {
                        damage = damage[i] * mult, knockback = dashStrike ? 12f : knockback[i],
                        hitStop = last || dashStrike || crit ? 0.07f : 0.035f,
                        sourcePosition = transform.position, attacker = self,
                    }, Facing);
                    Color col = crit ? new Color(1f, 0.4f, 0.9f) : dashStrike ? new Color(0.5f, 1f, 1f) : last ? new Color(1f, 0.85f, 0.5f) : slashColor;
                    CombatFx.Slash(transform.position + Vector3.up * 0.75f + Facing * 0.55f, Facing, last || dashStrike || crit ? 2f : 1.5f, col, combo % 2 == 1);
                    if (dashStrike) FloorDirector.Popup(transform.position + Vector3.up * 2.2f, "¡Tajo de impulso!", new Color(0.6f, 1f, 1f));
                    if (crit) FloorDirector.Popup(transform.position + Vector3.up * 2.5f, "¡CRÍTICO!", new Color(1f, 0.5f, 0.95f));
                }
                break;
            case Phase.Active:
                if (cc != null && cc.enabled) cc.Move(Facing * lunge * dt);
                if (timer >= active) { hitbox.End(); phase = Phase.Recover; timer = 0f; }
                break;
            case Phase.Recover:
                if (queued && combo < damage.Length - 1) { combo++; queued = false; phase = Phase.Windup; timer = 0f; }
                else if (timer >= recover)
                {
                    phase = Phase.None; queued = false;
                    combo = combo >= damage.Length - 1 ? 0 : combo + 1;
                    comboTimer = combo == 0 ? 0f : comboWindow;
                }
                break;
        }
    }

    void Begin()
    {
        phase = Phase.Windup; timer = 0f; queued = false;
    }

    static bool Pressed()
    {
        var kb = Keyboard.current; var ms = Mouse.current; var gp = Gamepad.current;
        return (kb != null && kb.jKey.wasPressedThisFrame)
            || (ms != null && ms.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked)
            || (gp != null && gp.buttonWest.wasPressedThisFrame);
    }
}
