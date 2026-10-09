using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Impulso (dash) de Abby: Shift (o B / RB en el control). Ráfaga corta hacia donde se mueve (o hacia donde mira),
/// funciona en el aire, deja estelas y da invulnerabilidad breve:
/// - Si un golpe la iba a alcanzar durante el dash → ESQUIVA PERFECTA (cámara lenta + siguiente golpe crítico, ver AbbyAttack).
/// - Atacar justo al terminar el dash → TAJO DE IMPULSO (más daño y empuje).
/// - Mientras dura, atraviesa los Velos de Fase (PhaseVeil).
/// Mientras dura apaga PlayerMotor (para que no pelee con el movimiento) y lo reactiva al terminar.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class AbbyDash : MonoBehaviour
{
    public float speed = 17f;
    public float duration = 0.18f;
    public float cooldown = 0.5f;
    [Tooltip("Segundos de invulnerabilidad desde que empieza (un poco más que la duración).")]
    public float iframes = 0.26f;
    public Color trailColor = new Color(0.45f, 0.9f, 1f, 0.55f);

    public bool IsDashing { get; private set; }
    public float LastDashEnd { get; private set; } = -10f;
    public float CooldownLeft => Mathf.Max(0f, cd);
    public Vector3 Direction { get; private set; }

    CharacterController cc;
    PlayerMotor motor;
    PlayerInputReader input;
    PlayerHealth health;
    SpriteRenderer mainSprite;
    float timer, cd, trailTimer;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        motor = GetComponent<PlayerMotor>();
        input = GetComponent<PlayerInputReader>();
        health = GetComponent<PlayerHealth>();
    }

    void Start()
    {
        foreach (var s in GetComponentsInChildren<SpriteRenderer>())
            if (s.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly && s.enabled) { mainSprite = s; break; }
    }

    void OnDisable() { if (IsDashing) End(); }

    void Update()
    {
        float dt = Time.deltaTime;
        if (cd > 0f) cd -= dt;
        if (health == null) health = GetComponent<PlayerHealth>();

        if (!IsDashing)
        {
            bool canAct = !PauseMenu.IsPaused && (input == null || input.enabled) && (health == null || !health.IsDead);
            if (canAct && cd <= 0f && Pressed()) Begin();
            return;
        }

        timer += dt;
        cc.Move((Direction * speed + Vector3.down * 2f) * dt);
        trailTimer -= dt;
        if (trailTimer <= 0f) { trailTimer = 0.035f; Afterimage(); }
        if (timer >= duration) End();
    }

    void Begin()
    {
        Vector3 dir = Vector3.zero;
        var cam = Camera.main;
        if (input != null && input.Move.sqrMagnitude > 0.01f && cam != null)
        {
            Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
            Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
            dir = r * input.Move.x + f * input.Move.y;
        }
        if (dir.sqrMagnitude < 0.01f && motor != null) dir = motor.WorldMoveDirection;
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
        dir.y = 0f;
        Direction = dir.normalized;

        IsDashing = true; timer = 0f; trailTimer = 0f;
        if (motor != null) motor.enabled = false;
        if (health != null) health.SetDodge(iframes);
        PhaseVeil.SetPassable(cc, true);
        CombatFx.Burst(transform.position + Vector3.up * 0.3f, trailColor, 10, 3f, 0.12f, 0.3f, 0f);
        CameraShake.Shake(0.05f, 0.1f);
    }

    void End()
    {
        IsDashing = false;
        LastDashEnd = Time.time;
        cd = cooldown;
        if (motor != null) motor.enabled = true;
        PhaseVeil.SetPassable(cc, false);
    }

    /// <summary>Copia del sprite actual que se desvanece (estela).</summary>
    void Afterimage()
    {
        if (mainSprite == null || mainSprite.sprite == null) return;
        var go = new GameObject("Estela");
        go.transform.SetPositionAndRotation(mainSprite.transform.position, mainSprite.transform.rotation);
        go.transform.localScale = mainSprite.transform.lossyScale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = mainSprite.sprite;
        sr.flipX = mainSprite.flipX;
        sr.sharedMaterial = mainSprite.sharedMaterial;
        sr.sortingOrder = mainSprite.sortingOrder - 1;
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sr.color = trailColor;
        go.AddComponent<Fade>().Init(sr, 0.22f);
    }

    static bool Pressed()
    {
        var kb = Keyboard.current; var gp = Gamepad.current;
        return (kb != null && (kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame))
            || (gp != null && (gp.buttonEast.wasPressedThisFrame || gp.rightShoulder.wasPressedThisFrame));
    }

    class Fade : MonoBehaviour
    {
        SpriteRenderer sr; float life, t; Color c;
        public void Init(SpriteRenderer r, float l) { sr = r; life = l; c = r.color; }
        void Update()
        {
            t += Time.deltaTime;
            sr.color = new Color(c.r, c.g, c.b, c.a * (1f - t / life));
            if (t >= life) Destroy(gameObject);
        }
    }
}
