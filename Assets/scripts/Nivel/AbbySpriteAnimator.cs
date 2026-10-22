using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Sprite de Abby en 8 direcciones relativas a la cámara, sin Animator, para el jugador de PlayerMotor
/// (exterior y pisos de la torre). Estados: quieta (Idle/dir respirando, o rotations), caminar (Walking/dir),
/// dash (Dash/dir), ataque (Espadazo/dir; sin él, la estocada del dash), bloqueo (Bloqueo/dir),
/// golpeada (Golpe/dir) y en el aire (cuadro de zancada fijo).
/// Va en Player/Visual junto al SpriteRenderer; apaga el Animator y SpriteDirectionAnimator de ese objeto.
/// Clic derecho en el componente → "Auto-asignar sprites de Abby" llena los arreglos desde las carpetas.
/// </summary>
[DefaultExecutionOrder(105)]   // después del movimiento y del billboard (100), antes de SpriteShadowCasters (110)
public class AbbySpriteAnimator : MonoBehaviour
{
    [System.Serializable]
    public class Frames
    {
        public Sprite[] frames = new Sprite[0];
        public bool Has => frames != null && frames.Length > 0;
    }

    [Header("Referencias (vacío = se buscan solas)")]
    public PlayerMotor motor;
    public AbbyDash dash;
    public AbbyAttack attack;
    public SpriteRenderer sr;
    public Transform cam;

    [Header("Sprites por dirección (0=E 1=NE 2=N 3=NO 4=O 5=SO 6=S 7=SE)")]
    public Sprite[] idle = new Sprite[8];
    public Frames[] walk = new Frames[8];
    public Frames[] dashFrames = new Frames[8];
    public Frames[] slash = new Frames[8];
    public Frames[] blockFrames = new Frames[8];
    public Frames[] hurt = new Frames[8];
    public Frames[] idleAnim = new Frames[8];
    [Header("Poses (un solo dibujo por dirección, mismo personaje): golpe y guardia")]
    public Sprite[] golpePose = new Sprite[8];
    public Sprite[] guardia = new Sprite[8];
    public AbbyBlock block;
    public PlayerHealth health;

    [Header("Ritmo")]
    public float walkFps = 12f;
    [Tooltip("Cuadros del dash que se usan como pose de ataque (estocada).")]
    public int attackPoseFrom = 2, attackPoseTo = 5;
    [Tooltip("Respiración en reposo: cuánto se estira el sprite en vertical.")]
    public float breathe = 0.012f;
    public float idleFps = 6f;
    [Tooltip("Lo que dura la reacción al recibir un golpe.")]
    public float hurtTime = 0.3f;

    static readonly string[] Names = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };

    int dir = 6;
    float walkTime, dashTime, attackTime, idleTime, blockTime, hurtLeft;
    int lastSwing;
    bool subscribed;
    Vector3 baseScale, basePos;
    Quaternion baseRot;
    SpriteBillboard billboard;

    void Awake()
    {
        if (motor == null) motor = GetComponentInParent<PlayerMotor>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        baseRot = transform.localRotation;
        billboard = GetComponent<SpriteBillboard>();
        basePos = transform.localPosition;
        // el animador viejo (Animator + blend tree de 4 caminatas) pelearía por el sprite
        var anim = GetComponent<Animator>(); if (anim != null) anim.enabled = false;
        var old = GetComponent<SpriteDirectionAnimator>(); if (old != null) old.enabled = false;
    }

    void LateUpdate()
    {
        if (motor == null || sr == null) return;
        if (dash == null) dash = motor.GetComponent<AbbyDash>();         // FloorDirector las agrega en Start
        if (attack == null) attack = motor.GetComponent<AbbyAttack>();
        if (block == null) block = motor.GetComponent<AbbyBlock>();
        if (health == null) health = motor.GetComponent<PlayerHealth>();
        if (!subscribed && health != null) { health.Damaged += OnDamaged; subscribed = true; }
        if (cam == null) { var c = Camera.main; if (c == null) return; cam = c.transform; }
        float dt = Time.deltaTime;

        bool dashing = dash != null && dash.IsDashing;
        bool attacking = attack != null && attack.Busy && !dashing;
        Vector3 facing = dashing ? dash.Direction : attacking ? attack.Facing : motor.WorldMoveDirection;
        if (facing.sqrMagnitude > 0.01f) dir = ScreenDirection(facing);

        Sprite s = null;
        float stretch = 1f;
        if (dashing)
        {
            dashTime += dt;
            var f = dashFrames[dir];
            if (f != null && f.Has)
            {
                // el cuadro 0 es la pose de pie: el dash empieza en el impulso (1) y se estira a lo que dura
                int n = f.frames.Length - 1;
                int i = 1 + Mathf.Min(n - 1, Mathf.FloorToInt(dashTime / Mathf.Max(0.05f, dash.duration) * n));
                s = f.frames[Mathf.Clamp(i, 0, f.frames.Length - 1)];
            }
        }
        else dashTime = 0f;

        float giro = 0f; Vector3 empuje = Vector3.zero; Color tinte = Color.white;
        if (s == null && attacking && golpePose != null && golpePose.Length > dir && golpePose[dir] != null)
        {
            // pose de golpe: un instante de carga (se encoge con el dibujo de pie) y luego el tajo, sin voltear la espada
            if (attack.Swings != lastSwing) { lastSwing = attack.Swings; attackTime = 0f; }
            attackTime += dt;
            bool fuerte = attack.ComboIndex >= 2;
            float lado = DirX(dir);
            if (attackTime < attack.windup)
            {
                s = idle[dir] != null ? idle[dir] : sr.sprite;
                stretch = 0.94f;
                giro = lado * 6f;
            }
            else
            {
                s = golpePose[dir];
                float k = Mathf.Clamp01((attackTime - attack.windup) / Mathf.Max(0.05f, attack.SwingLength - attack.windup));
                giro = -lado * (fuerte ? 12f : 7f) * (1f - k);
                empuje = Vector3.right * lado * 0.08f * (1f - k);
                stretch = 1f + (fuerte ? 0.05f : 0.03f) * (1f - k);
            }
        }
        else if (s == null && attacking && Has(slash, dir))
        {
            // espadazo completo en cada golpe del combo; el cuadro 0 es la pose de pie
            if (attack.Swings != lastSwing) { lastSwing = attack.Swings; attackTime = 0f; }
            attackTime += dt;
            var fr = slash[dir].frames;
            int n = fr.Length - 1;
            s = fr[Mathf.Clamp(1 + Mathf.FloorToInt(attackTime / Mathf.Max(0.05f, attack.SwingLength) * n), 1, n)];
        }
        else if (s == null && attacking)
        {
            attackTime += dt;
            var f = dashFrames[dir];
            if (f != null && f.Has)
            {
                int a = Mathf.Clamp(attackPoseFrom, 0, f.frames.Length - 1), b = Mathf.Clamp(attackPoseTo, a, f.frames.Length - 1);
                s = f.frames[Mathf.Min(b, a + Mathf.FloorToInt(attackTime / 0.07f))];
            }
        }
        else if (!attacking) attackTime = 0f;

        bool blocking = block != null && block.Blocking && !dashing && !attacking;
        if (s == null && blocking && guardia != null && guardia.Length > dir && guardia[dir] != null)
        {
            s = guardia[dir];
            blockTime += dt;
            stretch = 1f - 0.03f * Mathf.Clamp01(blockTime * 8f);   // se planta
        }
        else if (s == null && blocking && Has(blockFrames, dir))
        {
            // se cubre y se queda en la última pose mientras sostenga el clic derecho
            blockTime += dt;
            var fr = blockFrames[dir].frames;
            s = fr[Mathf.Min(fr.Length - 1, Mathf.FloorToInt(blockTime * 20f))];
        }
        else if (!blocking) blockTime = 0f;

        if (hurtLeft > 0f) hurtLeft -= dt;
        if (s == null && hurtLeft > 0f && Has(hurt, dir))
        {
            var fr = hurt[dir].frames;
            s = fr[Mathf.Clamp(Mathf.FloorToInt((1f - hurtLeft / hurtTime) * fr.Length), 0, fr.Length - 1)];
        }

        if (s == null && !motor.IsGrounded)
        {
            var f = walk[dir];
            if (f != null && f.Has) s = f.frames[Mathf.Min(1, f.frames.Length - 1)];   // zancada fija en el aire
        }

        if (s == null && motor.IsMoving)
        {
            walkTime += dt;
            var f = walk[dir];
            if (f != null && f.Has) s = f.frames[Mathf.FloorToInt(walkTime * walkFps) % f.frames.Length];
        }
        else if (!motor.IsMoving) walkTime = 0f;

        if (s == null)
        {
            idleTime += dt;
            if (Has(idleAnim, dir)) s = idleAnim[dir].frames[Mathf.FloorToInt(idleTime * idleFps) % idleAnim[dir].frames.Length];
            else
            {
                s = idle[dir] != null ? idle[dir] : sr.sprite;
                stretch = 1f + Mathf.Sin(idleTime * 2.4f) * breathe;
            }
        }
        else idleTime = 0f;

        sr.sprite = s;
        // golpe recibido sin cuadros propios: se tiñe de rojo, se sacude y se echa para atrás
        if (hurtLeft > 0f && !Has(hurt, dir))
        {
            float k = hurtLeft / hurtTime;
            tinte = Color.Lerp(Color.white, new Color(1f, 0.45f, 0.45f), k);
            empuje += Vector3.right * Mathf.Sin(Time.time * 80f) * 0.05f * k;
            giro += DirX(dir) * -8f * k;
            stretch *= 1f - 0.05f * k;
        }
        sr.flipX = false;
        sr.color = tinte;
        // el billboard ya dejó escala (con su compensación de inclinación) y giro de este cuadro: encima va lo nuestro
        if (billboard != null && billboard.enabled)
        {
            var sc = transform.localScale;
            transform.localScale = new Vector3(sc.x, sc.y * stretch, sc.z);
            transform.rotation = transform.rotation * Quaternion.Euler(0f, 0f, giro);
        }
        else
        {
            transform.localScale = new Vector3(baseScale.x, baseScale.y * stretch, baseScale.z);
            transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, giro);
        }
        transform.localPosition = basePos + transform.parent.InverseTransformDirection(transform.rotation * empuje) * 1f;
    }

    /// <summary>Hacia qué lado de la pantalla mira esa dirección: 1 derecha, -1 izquierda, 0 de frente o de espaldas.</summary>
    static float DirX(int d) => d == 0 || d == 1 || d == 7 ? 1f : d == 3 || d == 4 || d == 5 ? -1f : 0.4f;

    void OnDamaged(float amount) { if (amount > 0f) hurtLeft = hurtTime; }

    void OnDestroy() { if (subscribed && health != null) health.Damaged -= OnDamaged; }

    static bool Has(Frames[] set, int d) => set != null && set.Length > d && set[d] != null && set[d].Has;

    /// <summary>Dirección en pantalla (0 = derecha, sentido antihorario de 45 en 45) vista desde la cámara.</summary>
    int ScreenDirection(Vector3 world)
    {
        Vector3 r = cam.right; r.y = 0f; r.Normalize();
        Vector3 f = cam.forward; f.y = 0f; f.Normalize();
        float ang = Mathf.Atan2(Vector3.Dot(world, f), Vector3.Dot(world, r)) * Mathf.Rad2Deg;
        int d = Mathf.RoundToInt(ang / 45f);
        return ((d % 8) + 8) % 8;
    }

#if UNITY_EDITOR
    const string Root = "Assets/sprites/Abby/Idle";

    [ContextMenu("Auto-asignar sprites de Abby")]
    public void AutoAssign()
    {
        Undo.RecordObject(this, "Auto-asignar sprites de Abby");
        idle = new Sprite[8]; walk = new Frames[8]; dashFrames = new Frames[8];
        slash = new Frames[8]; blockFrames = new Frames[8]; hurt = new Frames[8]; idleAnim = new Frames[8];
        for (int i = 0; i < 8; i++)
        {
            idle[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/rotations/{Names[i]}.png");
            walk[i] = new Frames { frames = LoadFrames($"{Root}/animations/Walking/{Names[i]}") };
            dashFrames[i] = new Frames { frames = LoadFrames($"{Root}/animations/Dash/{Names[i]}") };
            slash[i] = new Frames { frames = LoadFrames($"{Root}/animations/Espadazo/{Names[i]}") };
            blockFrames[i] = new Frames { frames = LoadFrames($"{Root}/animations/Bloqueo/{Names[i]}") };
            hurt[i] = new Frames { frames = LoadFrames($"{Root}/animations/Golpe/{Names[i]}") };
            idleAnim[i] = new Frames { frames = LoadFrames($"{Root}/animations/Idle/{Names[i]}") };
        }
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (motor == null) motor = GetComponentInParent<PlayerMotor>();
        EditorUtility.SetDirty(this);
    }

    static Sprite[] LoadFrames(string folder)
    {
        var list = new System.Collections.Generic.List<Sprite>();
        for (int f = 0; f < 64; f++)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/frame_{f:000}.png");
            if (s == null) break;
            list.Add(s);
        }
        return list.ToArray();
    }
#endif
}
