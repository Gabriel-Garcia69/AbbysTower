using System;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState { Move, Attack, Dash, Block, Hurt, Dead }

[Serializable]
public class ComboStep
{
    public float startup = 0.08f;
    public float active = 0.10f;
    public float recovery = 0.20f;
    public float damage = 10f;
    public float knockback = 4f;
    public float lunge = 3f;        // avance hacia adelante durante startup+active
    public float hitStop = 0.05f;
    public float staminaCost = 10f;
}

/// <summary>
/// Controlador de combate de Aby: movimiento, combo de espada con input buffer,
/// dash con i-frames (cancela recovery), bloqueo + parry, stamina, vida y hurt.
/// Requiere: CharacterController, CombatInputReader, Hitbox (todos en la raíz).
/// Animator (opcional) parámetros: Speed(float) Block(bool) Attack(trigger) ComboStep(int) Dash(trigger) Hurt(trigger) Parry(trigger) Dead(trigger)
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(CombatInputReader))]
[RequireComponent(typeof(Hitbox))]
public class PlayerController : MonoBehaviour, IHittable
{
    [Header("Referencias")]
    public Animator animator;
    public SpriteRenderer sprite;      // el sprite se asume dibujado mirando a la derecha
    public Transform cam;              // si está vacío usa Camera.main

    [Header("Movimiento")]
    public float moveSpeed = 5f;
    public float gravity = -20f;

    [Header("Combo (espada)")]
    public ComboStep[] combo =
    {
        new ComboStep(),
        new ComboStep { damage = 12f, lunge = 3.5f },
        new ComboStep { damage = 22f, startup = 0.14f, active = 0.12f, recovery = 0.35f, knockback = 8f, hitStop = 0.09f, staminaCost = 18f, lunge = 5f }
    };
    public float inputBuffer = 0.2f;                         // tiempo que se recuerda un input
    [Range(0f, 1f)] public float comboLinkPoint = 0.4f;      // % del recovery desde el que se puede encadenar

    [Header("Dash")]
    public float dashSpeed = 13f;
    public float dashDuration = 0.22f;
    public float dashIFrames = 0.16f;
    public float dashCooldown = 0.3f;
    public float dashStamina = 20f;

    [Header("Bloqueo / Parry")]
    public float parryWindow = 0.15f;
    [Range(0f, 1f)] public float blockDamageMultiplier = 0.2f;
    public float blockStaminaPerDamage = 1.5f;
    public float parryStagger = 1.2f;
    public float parryStaminaRefund = 25f;
    public float blockMoveMultiplier = 0.3f;

    [Header("Stamina")]
    public float maxStamina = 100f;
    public float staminaRegen = 35f;
    public float staminaRegenDelay = 0.6f;

    [Header("Vida")]
    public float maxHealth = 100f;
    public float hurtDuration = 0.35f;
    public float postHitInvuln = 0.5f;
    public float knockDecay = 25f;

    public PlayerState State { get; private set; } = PlayerState.Move;
    public float Health { get; private set; }
    public float Stamina { get; private set; }
    public float Health01 => Health / maxHealth;
    public float Stamina01 => Stamina / maxStamina;
    public event Action Died;

    [Header("Visual")]
    public bool autoFlip = true;       // AbyDirectionalSprite lo apaga (usa los 8 sprites en vez de voltear)
    public Vector3 Facing => facing;   // hacia dónde mira Aby (mundo, plano XZ)
    public float Speed => vel.magnitude;

    CharacterController cc;
    CombatInputReader input;
    Hitbox hitbox;

    Vector3 facing = Vector3.forward;
    Vector3 vel;            // velocidad horizontal propia
    Vector3 knock;          // empuje externo (decae)
    float vy;

    float stateTimer;
    float attackBuffer, dashBuffer;
    float dashCooldownTimer, staminaRegenTimer, invulnTimer, parryTimer, currentHurtDuration;
    int attackStep;
    bool hitboxOn;
    Vector3 dashDir;

    readonly HashSet<string> animParams = new HashSet<string>();

    bool Invulnerable => (State == PlayerState.Dash && stateTimer < dashIFrames) || invulnTimer > 0f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        input = GetComponent<CombatInputReader>();
        hitbox = GetComponent<Hitbox>();
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (animator != null) foreach (var p in animator.parameters) animParams.Add(p.name);
        Health = maxHealth;
        Stamina = maxStamina;
    }

    void Update()
    {
        if (State == PlayerState.Dead) { ApplyMovement(Time.deltaTime); return; }

        float dt = Time.deltaTime;

        // buffers
        attackBuffer = input.AttackDown ? inputBuffer : attackBuffer - dt;
        dashBuffer = input.DashDown ? inputBuffer : dashBuffer - dt;
        dashCooldownTimer -= dt;
        invulnTimer -= dt;

        // stamina
        staminaRegenTimer -= dt;
        if (staminaRegenTimer <= 0f && State != PlayerState.Block)
            Stamina = Mathf.Min(maxStamina, Stamina + staminaRegen * dt);

        Vector3 moveDir = GetMoveDir();

        switch (State)
        {
            case PlayerState.Move: UpdateMove(moveDir); break;
            case PlayerState.Attack: UpdateAttack(moveDir, dt); break;
            case PlayerState.Dash: UpdateDash(dt); break;
            case PlayerState.Block: UpdateBlock(moveDir, dt); break;
            case PlayerState.Hurt: UpdateHurt(dt); break;
        }

        ApplyMovement(dt);

        SetFloat("Speed", vel.magnitude);
        SetBool("Block", State == PlayerState.Block);
    }

    void LateUpdate()
    {
        if (autoFlip && sprite != null && cam != null)
        {
            float side = Vector3.Dot(facing, cam.right);
            if (Mathf.Abs(side) > 0.1f) sprite.flipX = side < 0f;
        }
    }

    // ---------- Estados ----------

    void UpdateMove(Vector3 moveDir)
    {
        if (TryDash(moveDir)) return;
        if (TryAttack(0)) return;
        if (input.BlockDown || (input.BlockHeld && Stamina > 0f)) { EnterBlock(); return; }

        vel = moveDir * moveSpeed;
        Face(moveDir);
    }

    void UpdateAttack(Vector3 moveDir, float dt)
    {
        ComboStep s = combo[attackStep];
        stateTimer += dt;

        float activeStart = s.startup;
        float activeEnd = s.startup + s.active;
        float end = activeEnd + s.recovery;

        vel = stateTimer < activeEnd ? facing * s.lunge : Vector3.zero;

        bool inActive = stateTimer >= activeStart && stateTimer < activeEnd;
        if (inActive && !hitboxOn)
        {
            hitbox.Begin(new HitInfo { damage = s.damage, knockback = s.knockback, hitStop = s.hitStop, sourcePosition = transform.position, attacker = this }, facing);
            hitboxOn = true;
        }
        else if (!inActive && hitboxOn)
        {
            hitbox.End();
            hitboxOn = false;
        }

        if (stateTimer >= activeEnd)
        {
            if (TryDash(moveDir)) return;   // dash-cancel del recovery

            float linkTime = activeEnd + s.recovery * comboLinkPoint;
            if (stateTimer >= linkTime && TryAttack(attackStep + 1)) return;

            if (input.BlockDown) { ExitAttack(); EnterBlock(); return; }
        }

        if (stateTimer >= end) { ExitAttack(); State = PlayerState.Move; }
    }

    void UpdateDash(float dt)
    {
        stateTimer += dt;
        vel = dashDir * dashSpeed;
        if (stateTimer >= dashDuration) { vel = Vector3.zero; State = PlayerState.Move; }
    }

    void UpdateBlock(Vector3 moveDir, float dt)
    {
        parryTimer -= dt;
        if (TryDash(moveDir)) return;

        if (!input.BlockHeld || Stamina <= 0f) { State = PlayerState.Move; return; }

        vel = moveDir * moveSpeed * blockMoveMultiplier;
        Face(moveDir);
    }

    void UpdateHurt(float dt)
    {
        stateTimer += dt;
        vel = Vector3.zero;
        if (stateTimer >= currentHurtDuration) State = PlayerState.Move;
    }

    // ---------- Acciones ----------

    bool TryAttack(int step)
    {
        if (attackBuffer <= 0f || step >= combo.Length) return false;
        if (Stamina < combo[step].staminaCost) return false;

        ExitAttack();
        attackBuffer = 0f;
        Spend(combo[step].staminaCost);
        Face(GetMoveDir());              // aim assist: gira hacia donde apuntas el stick
        attackStep = step;
        stateTimer = 0f;
        State = PlayerState.Attack;
        SetInt("ComboStep", step);
        Trigger("Attack");
        return true;
    }

    bool TryDash(Vector3 moveDir)
    {
        if (dashBuffer <= 0f || dashCooldownTimer > 0f || Stamina < dashStamina) return false;

        ExitAttack();
        dashBuffer = 0f;
        Spend(dashStamina);
        dashDir = moveDir.sqrMagnitude > 0.01f ? moveDir.normalized : facing;
        Face(dashDir);
        dashCooldownTimer = dashCooldown;
        stateTimer = 0f;
        State = PlayerState.Dash;
        Trigger("Dash");
        return true;
    }

    void EnterBlock()
    {
        State = PlayerState.Block;
        parryTimer = parryWindow;
        stateTimer = 0f;
        vel = Vector3.zero;
    }

    void EnterHurt(float duration)
    {
        ExitAttack();
        State = PlayerState.Hurt;
        stateTimer = 0f;
        currentHurtDuration = duration;
        invulnTimer = postHitInvuln;
        Trigger("Hurt");
    }

    void ExitAttack()
    {
        if (hitboxOn) { hitbox.End(); hitboxOn = false; }
    }

    // ---------- Recibir daño ----------

    public bool ReceiveHit(HitInfo hit)
    {
        if (State == PlayerState.Dead || Invulnerable) return false;

        Vector3 toSource = hit.sourcePosition - transform.position;
        toSource.y = 0f;
        toSource.Normalize();
        Vector3 away = -toSource;

        if (State == PlayerState.Block && Vector3.Dot(facing, toSource) > 0.1f)
        {
            if (parryTimer > 0f)   // PARRY
            {
                Stamina = Mathf.Min(maxStamina, Stamina + parryStaminaRefund);
                hit.attacker?.Stagger(parryStagger);
                invulnTimer = 0.2f;
                Trigger("Parry");
                HitStop.Do(0.12f);
                return true;
            }

            // BLOQUEO normal
            Health -= hit.damage * blockDamageMultiplier;
            Spend(hit.damage * blockStaminaPerDamage);
            knock = away * hit.knockback * 0.4f;
            if (Health <= 0f) { Die(); return true; }
            if (Stamina <= 0f) { Stamina = 0f; EnterHurt(hurtDuration * 1.5f); }  // guard break
            return true;
        }

        Health -= hit.damage;
        knock = away * hit.knockback;
        if (Health <= 0f) { Die(); return true; }
        EnterHurt(hurtDuration);
        return true;
    }

    public void Stagger(float duration) { if (State != PlayerState.Dead) EnterHurt(duration); }

    void Die()
    {
        ExitAttack();
        State = PlayerState.Dead;
        vel = Vector3.zero;
        Trigger("Dead");
        Died?.Invoke();
    }

    // ---------- Utilidades ----------

    Vector3 GetMoveDir()
    {
        if (cam == null) return new Vector3(input.Move.x, 0f, input.Move.y);
        Vector3 f = cam.forward; f.y = 0f; f.Normalize();
        Vector3 r = cam.right; r.y = 0f; r.Normalize();
        return Vector3.ClampMagnitude(f * input.Move.y + r * input.Move.x, 1f);
    }

    void Face(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f) facing = dir.normalized;
    }

    void Spend(float amount)
    {
        Stamina = Mathf.Max(0f, Stamina - amount);
        staminaRegenTimer = staminaRegenDelay;
    }

    void ApplyMovement(float dt)
    {
        knock = Vector3.MoveTowards(knock, Vector3.zero, knockDecay * dt);
        if (cc.isGrounded && vy < 0f) vy = -2f;
        vy += gravity * dt;

        Vector3 total = vel + knock;
        total.y = vy;
        cc.Move(total * dt);
    }

    void Trigger(string n) { if (animator != null && animParams.Contains(n)) animator.SetTrigger(n); }
    void SetBool(string n, bool v) { if (animator != null && animParams.Contains(n)) animator.SetBool(n, v); }
    void SetFloat(string n, float v) { if (animator != null && animParams.Contains(n)) animator.SetFloat(n, v); }
    void SetInt(string n, int v) { if (animator != null && animParams.Contains(n)) animator.SetInteger(n, v); }
}
