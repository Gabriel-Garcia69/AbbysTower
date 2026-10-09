using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemigo de la torre, hecho sin prefabs ni arte (formas + brillo) para poder jugar el piso ya; con arte real
/// se reemplaza solo el "Visual". Tres tipos:
/// - Sombra: rápida y frágil; avisa con una franja en el suelo y embiste.
/// - Bruto: lento, aguanta golpes (solo lo frena el 3er tajo) y pega fuerte.
/// - Jefe (Guardián de la Base): embestidas, golpe en área con anillo de aviso, invoca sombras al 66% y 33%
///   y bajo 40% se enfurece (más rápido, embestidas encadenadas y ráfagas de orbes).
/// Se mueve sin físicas dentro de los límites de su sala (RoomEncounter). Se crea con TowerEnemy.Create.
/// </summary>
public class TowerEnemy : MonoBehaviour, IHittable
{
    public enum Kind { Sombra, Bruto, Jefe }

    public Kind kind;
    public float maxHealth = 40f;
    public float moveSpeed = 3f;
    public float radius = 0.45f;
    public float damage = 10f;
    public float attackRange = 2.6f;
    public float telegraph = 0.5f;
    public float lungeSpeed = 11f;
    public float lungeTime = 0.22f;
    public float recover = 0.7f;
    [Tooltip("Daño acumulado necesario para frenarlo (0 = cualquier golpe lo frena).")]
    public float poise;
    [Tooltip("1 = retroceso completo, 0 = no se mueve al recibir golpes.")]
    public float knockTaken = 1f;
    public Color eyeColor = new Color(0.75f, 0.45f, 1f);

    public Transform target;
    public RoomEncounter room;

    public float Health { get; private set; }
    public float Normalized => Health / Mathf.Max(1f, maxHealth);
    public bool IsBoss => kind == Kind.Jefe;
    public bool Enraged { get; private set; }
    public float LastHitTime { get; private set; } = -10f;
    public string DisplayName => kind == Kind.Jefe ? "Guardián de la Base" : kind == Kind.Bruto ? "Bruto" : "Sombra";

    public static readonly List<TowerEnemy> All = new List<TowerEnemy>();
    public static event Action<TowerEnemy> AnyDied;
    public static event Action<TowerEnemy> BossEnraged;

    enum State { Spawning, Chase, Telegraph, Lunge, Slam, Recover, Stunned, Dying }
    State state = State.Spawning;
    float timer, cooldown, poiseDamage, bob;
    Vector3 knock, facing = Vector3.back, lungeDir;
    int chain, attackCount;
    bool summoned66, summoned33;
    GameObject telegraphFx;

    Transform visual;
    Renderer body;
    Renderer[] eyes;
    Light glow;
    Hitbox hitbox;
    MaterialPropertyBlock mpb;
    Color bodyColor;
    float flash;

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    // ------------------------------------------------------------------ creación

    public static TowerEnemy Create(Kind kind, Vector3 position, Transform parent, RoomEncounter room, Transform target)
    {
        var go = new GameObject(kind.ToString());
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        var e = go.AddComponent<TowerEnemy>();
        e.kind = kind; e.room = room; e.target = target;
        e.Configure();
        return e;
    }

    void Configure()
    {
        switch (kind)
        {
            case Kind.Sombra:
                maxHealth = 40f; moveSpeed = 3.3f; radius = 0.45f; damage = 10f; attackRange = 3f;
                telegraph = 0.55f; lungeSpeed = 12f; lungeTime = 0.22f; recover = 0.75f; poise = 0f; knockTaken = 1f;
                eyeColor = new Color(0.75f, 0.45f, 1f); bodyColor = new Color(0.1f, 0.07f, 0.17f);
                break;
            case Kind.Bruto:
                maxHealth = 120f; moveSpeed = 2.1f; radius = 0.8f; damage = 20f; attackRange = 3.2f;
                telegraph = 0.85f; lungeSpeed = 9f; lungeTime = 0.32f; recover = 1.1f; poise = 25f; knockTaken = 0.35f;
                eyeColor = new Color(1f, 0.5f, 0.15f); bodyColor = new Color(0.22f, 0.08f, 0.06f);
                break;
            case Kind.Jefe:
                maxHealth = 650f; moveSpeed = 2.5f; radius = 1.4f; damage = 22f; attackRange = 4.5f;
                telegraph = 0.75f; lungeSpeed = 15f; lungeTime = 0.36f; recover = 0.9f; poise = 99999f; knockTaken = 0.08f;
                eyeColor = new Color(1f, 0.22f, 0.15f); bodyColor = new Color(0.09f, 0.05f, 0.11f);
                break;
        }
        Health = maxHealth;
        BuildVisual();

        var col = gameObject.AddComponent<CapsuleCollider>();
        col.radius = radius; col.height = Mathf.Max(radius * 2f, 1.8f * Scale); col.center = Vector3.up * col.height / 2f;

        hitbox = gameObject.AddComponent<Hitbox>();
        hitbox.owner = transform;
        hitbox.offset = new Vector3(0f, 0.6f * Scale, radius * 0.6f);
        hitbox.halfExtents = new Vector3(0.55f, 0.6f, 0.55f) * Scale + new Vector3(radius * 0.4f, 0f, radius * 0.4f);
    }

    float Scale => kind == Kind.Jefe ? 2.6f : kind == Kind.Bruto ? 1.5f : 0.95f;

    static Material litMat, eyeMat;

    void BuildVisual()
    {
        if (litMat == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Lit");
            litMat = new Material(sh) { name = "Enemy_Body (runtime)" };
            litMat.SetFloat("_Smoothness", 0.35f);
        }
        if (eyeMat == null)
        {
            var sh = Shader.Find("Universal Render Pipeline/Unlit");
            eyeMat = new Material(sh) { name = "Enemy_Eye (runtime)" };
        }
        mpb = new MaterialPropertyBlock();

        visual = new GameObject("Visual").transform;
        visual.SetParent(transform, false);
        float s = Scale;

        // cuerpo: gota (esfera alargada) + "capa" (cono invertido de cubo girado)
        var b = Part(PrimitiveType.Sphere, "Cuerpo", new Vector3(0f, 0.75f, 0f) * s, new Vector3(0.9f, 1.05f, 0.9f) * s, litMat);
        body = b.GetComponent<Renderer>();
        var cape = Part(PrimitiveType.Cube, "Capa", new Vector3(0f, 0.35f, 0f) * s, new Vector3(0.75f, 0.7f, 0.75f) * s, litMat);
        cape.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        SetColor(cape.GetComponent<Renderer>(), bodyColor * 0.8f);

        // ojos
        var eyeList = new List<Renderer>();
        foreach (float x in new[] { -0.17f, 0.17f })
        {
            var eye = Part(PrimitiveType.Cube, "Ojo", new Vector3(x, 0.92f, 0.4f) * s, new Vector3(0.13f, 0.2f, 0.06f) * s, eyeMat);
            eyeList.Add(eye.GetComponent<Renderer>());
        }
        eyes = eyeList.ToArray();

        if (kind == Kind.Bruto)
        {
            foreach (float x in new[] { -0.32f, 0.32f })
            {
                var horn = Part(PrimitiveType.Cube, "Cuerno", new Vector3(x, 1.3f, 0.05f) * s, new Vector3(0.12f, 0.4f, 0.12f) * s, litMat);
                horn.transform.localRotation = Quaternion.Euler(0f, 0f, x > 0 ? -25f : 25f);
                SetColor(horn.GetComponent<Renderer>(), new Color(0.75f, 0.68f, 0.55f));
                var fist = Part(PrimitiveType.Sphere, "Puño", new Vector3(x * 2.1f, 0.5f, 0.2f) * s, Vector3.one * 0.38f * s, litMat);
                SetColor(fist.GetComponent<Renderer>(), bodyColor * 1.4f);
            }
        }
        if (kind == Kind.Jefe)
        {
            // corona de púas brillantes que gira, hombreras y una luz roja
            var crown = new GameObject("Corona").transform;
            crown.SetParent(visual, false);
            crown.localPosition = new Vector3(0f, 1.4f * s, 0f);
            crown.gameObject.AddComponent<Spin>().speed = 40f;
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                var spike = Part(PrimitiveType.Cube, "Pua", Vector3.zero, new Vector3(0.1f, 0.45f, 0.1f) * s, eyeMat, crown);
                spike.transform.localPosition = new Vector3(Mathf.Cos(a) * 0.32f, 0.1f, Mathf.Sin(a) * 0.32f) * s;
                spike.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 18f, 0f, -Mathf.Cos(a) * 18f);
                SetColor(spike.GetComponent<Renderer>(), eyeColor * 3f);
            }
            foreach (float x in new[] { -0.5f, 0.5f })
            {
                var pad = Part(PrimitiveType.Cube, "Hombrera", new Vector3(x, 1.05f, 0f) * s, new Vector3(0.38f, 0.22f, 0.45f) * s, litMat);
                pad.transform.localRotation = Quaternion.Euler(0f, 0f, x > 0 ? -20f : 20f);
                SetColor(pad.GetComponent<Renderer>(), new Color(0.3f, 0.26f, 0.32f));
            }
            var l = new GameObject("Luz").AddComponent<Light>();
            l.transform.SetParent(visual, false);
            l.transform.localPosition = new Vector3(0f, 1.2f * s, 0.8f);
            l.type = LightType.Point; l.color = eyeColor; l.range = 9f; l.intensity = 6f; l.shadows = LightShadows.None;
            glow = l;
        }
        SetColor(body, bodyColor);
        foreach (var e in eyes) SetColor(e, eyeColor * 4f);
        visual.localScale = Vector3.one * 0.05f;
    }

    GameObject Part(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Transform parent = null)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        Destroy(go.GetComponent<Collider>());
        go.transform.SetParent(parent != null ? parent : visual, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        var r = go.GetComponent<Renderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = mat == eyeMat ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
        return go;
    }

    void SetColor(Renderer r, Color c)
    {
        if (r == null) return;
        r.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, c);
        r.SetPropertyBlock(mpb);
    }

    // ------------------------------------------------------------------ ciclo

    void OnEnable() => All.Add(this);
    void OnDisable() { All.Remove(this); if (telegraphFx != null) Destroy(telegraphFx); }

    void Start()
    {
        if (target == null) { var p = GameObject.FindWithTag("Player"); if (p != null) target = p.transform; }
        CombatFx.Ring(transform.position, radius * 2.2f + 0.6f, eyeColor, 0.8f);
        cooldown = UnityEngine.Random.Range(0.4f, 1.2f);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        timer += dt;
        if (cooldown > 0f) cooldown -= dt;
        bob += dt;

        // retroceso (en cualquier estado)
        if (knock.sqrMagnitude > 0.01f)
        {
            transform.position += knock * dt;
            knock = Vector3.MoveTowards(knock, Vector3.zero, 26f * dt);
        }

        Vector3 toPlayer = Vector3.zero; float dist = 999f;
        var ph = target != null ? target.GetComponent<PlayerHealth>() : null;
        bool playerAlive = target != null && (ph == null || !ph.IsDead);
        if (target != null) { toPlayer = target.position - transform.position; toPlayer.y = 0f; dist = toPlayer.magnitude; }

        switch (state)
        {
            case State.Spawning:
            {
                float k = Mathf.Clamp01(timer / 0.8f);
                visual.localScale = Vector3.one * Mathf.Lerp(0.05f, 1f, 1f - (1f - k) * (1f - k));
                visual.localPosition = Vector3.down * (1f - k) * 1.2f;
                if (k >= 1f) Go(State.Chase);
                break;
            }
            case State.Chase:
            {
                if (!playerAlive) break;
                if (dist > 0.01f) facing = Vector3.Slerp(facing, toPlayer / dist, 8f * dt).normalized;
                float speed = moveSpeed * (Enraged ? 1.35f : 1f);
                Vector3 move = dist > attackRange * 0.7f ? facing * speed : Vector3.zero;
                move += Separation() * speed;
                transform.position += move * dt;
                if (IsBoss) CheckBossPhases();
                if (dist < attackRange && cooldown <= 0f) ChooseAttack(dist);
                break;
            }
            case State.Telegraph:
            {
                // tiembla y brilla antes de atacar
                float tel = telegraph * (Enraged ? 0.7f : 1f);
                visual.localPosition = new Vector3(Mathf.Sin(timer * 70f) * 0.04f * Scale, 0f, 0f);
                foreach (var e in eyes) SetColor(e, eyeColor * Mathf.Lerp(4f, 12f, timer / tel));
                if (timer >= tel)
                {
                    visual.localPosition = Vector3.zero;
                    hitbox.Begin(new HitInfo { damage = damage, knockback = IsBoss ? 12f : 8f, hitStop = 0.04f, sourcePosition = transform.position, attacker = this }, lungeDir);
                    Go(State.Lunge);
                }
                break;
            }
            case State.Lunge:
            {
                transform.position += lungeDir * lungeSpeed * dt;
                facing = lungeDir;
                if (timer >= lungeTime)
                {
                    hitbox.End();
                    foreach (var e in eyes) SetColor(e, eyeColor * 4f);
                    if (Enraged && IsBoss && chain > 0 && playerAlive)
                    {
                        chain--;
                        lungeDir = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : lungeDir;
                        ShowLine();
                        Go(State.Telegraph);
                        timer = telegraph * 0.35f;   // las siguientes de la cadena avisan menos
                    }
                    else Go(State.Recover);
                }
                break;
            }
            case State.Slam:
            {
                float tel = 1.05f * (Enraged ? 0.75f : 1f);
                float k = Mathf.Clamp01(timer / tel);
                visual.localPosition = Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.6f;   // salta y cae
                if (timer >= tel)
                {
                    visual.localPosition = Vector3.zero;
                    const float slamRadius = 5.2f;
                    CameraShake.Shake(0.5f, 0.45f);
                    CombatFx.Burst(transform.position + Vector3.up * 0.3f, eyeColor, 60, 9f, 0.25f, 0.6f, 1.5f);
                    CombatFx.Ring(transform.position, slamRadius, eyeColor, 0.25f);
                    if (playerAlive && dist < slamRadius && ph != null)
                        ph.ReceiveHit(new HitInfo { damage = 28f, knockback = 14f, hitStop = 0.06f, sourcePosition = transform.position, attacker = this });
                    if (Enraged) FireBolts(10);
                    Go(State.Recover);
                }
                break;
            }
            case State.Recover:
            {
                transform.position += Separation() * moveSpeed * dt;
                if (timer >= recover * (Enraged ? 0.6f : 1f)) Go(State.Chase);
                break;
            }
            case State.Stunned:
            {
                if (timer >= 0.3f) Go(State.Chase);
                break;
            }
            case State.Dying:
            {
                float k = Mathf.Clamp01(timer / (IsBoss ? 1.6f : 0.3f));
                visual.localScale = Vector3.one * (1f - k) * (1f + Mathf.Sin(timer * 40f) * 0.05f);
                if (IsBoss && Mathf.Repeat(timer, 0.2f) < dt)
                    CombatFx.Burst(transform.position + Vector3.up * UnityEngine.Random.Range(0.5f, 3f), eyeColor, 20, 7f, 0.25f, 0.6f);
                if (k >= 1f)
                {
                    CombatFx.Burst(transform.position + Vector3.up * Scale * 0.7f, eyeColor, IsBoss ? 160 : 40, IsBoss ? 12f : 6f, IsBoss ? 0.35f : 0.2f, IsBoss ? 1.4f : 0.7f);
                    Destroy(gameObject);
                }
                break;
            }
        }

        // flotar suave, mirar hacia donde va, parpadeo blanco al recibir golpes
        if (state != State.Spawning && state != State.Dying && state != State.Slam && state != State.Telegraph)
            visual.localPosition = Vector3.up * Mathf.Sin(bob * 3f) * 0.06f * Scale;
        if (facing.sqrMagnitude > 0.001f) visual.rotation = Quaternion.Slerp(visual.rotation, Quaternion.LookRotation(facing), 12f * dt);
        if (flash > 0f) { flash -= dt; SetColor(body, flash > 0f ? Color.white * 2f : bodyColor); }
        if (glow != null) glow.intensity = (Enraged ? 10f : 6f) * (0.85f + Mathf.PerlinNoise(bob * 3f, 0f) * 0.3f);

        KeepInRoom();
    }

    void Go(State s)
    {
        state = s; timer = 0f;
        if (s == State.Recover || s == State.Chase) cooldown = Mathf.Max(cooldown, s == State.Recover ? 0.2f : 0f);
        if (s != State.Telegraph && s != State.Lunge && telegraphFx != null) { Destroy(telegraphFx); telegraphFx = null; }
    }

    void ChooseAttack(float dist)
    {
        attackCount++;
        if (IsBoss && (dist < 4f || attackCount % 3 == 0))
        {
            Go(State.Slam);
            telegraphFx = CombatFx.Ring(transform.position, 5.2f, new Color(1f, 0.25f, 0.2f, 0.9f), 1.05f * (Enraged ? 0.75f : 1f));
            cooldown = 1.2f;
            return;
        }
        lungeDir = target != null ? (target.position - transform.position) : facing;
        lungeDir.y = 0f; lungeDir = lungeDir.sqrMagnitude > 0.01f ? lungeDir.normalized : facing;
        chain = IsBoss && Enraged ? 2 : 0;
        ShowLine();
        Go(State.Telegraph);
        cooldown = UnityEngine.Random.Range(0.6f, 1.4f);
    }

    void ShowLine()
    {
        if (telegraphFx != null) Destroy(telegraphFx);
        float len = lungeSpeed * lungeTime + radius + 1f;
        telegraphFx = CombatFx.Line(transform.position, lungeDir, len, radius * 2f + 0.4f, new Color(1f, 0.3f, 0.25f, 0.8f), telegraph);
    }

    Vector3 Separation()
    {
        Vector3 push = Vector3.zero;
        foreach (var o in All)
        {
            if (o == this) continue;
            Vector3 d = transform.position - o.transform.position; d.y = 0f;
            float min = radius + o.radius + 0.3f;
            if (d.sqrMagnitude < min * min && d.sqrMagnitude > 0.0001f) push += d.normalized * (1f - d.magnitude / min);
        }
        if (target != null)
        {
            Vector3 d = transform.position - target.position; d.y = 0f;
            float min = radius + 0.6f;
            if (d.sqrMagnitude < min * min && d.sqrMagnitude > 0.0001f) push += d.normalized * 1.5f;
        }
        return push;
    }

    void KeepInRoom()
    {
        if (room == null) return;
        Vector3 local = room.transform.InverseTransformPoint(transform.position);
        float hx = room.size.x / 2f - radius - 0.4f, hz = room.size.y / 2f - radius - 0.4f;
        local.x = Mathf.Clamp(local.x, -hx, hx);
        local.z = Mathf.Clamp(local.z, -hz, hz);
        local.y = 0f;
        transform.position = room.transform.TransformPoint(local);
    }

    // ------------------------------------------------------------------ jefe

    void CheckBossPhases()
    {
        if (!summoned66 && Normalized < 0.66f) { summoned66 = true; Summon(2); }
        if (!summoned33 && Normalized < 0.33f) { summoned33 = true; Summon(3); }
        if (!Enraged && Normalized < 0.4f)
        {
            Enraged = true;
            CameraShake.Shake(0.35f, 0.8f);
            CombatFx.Burst(transform.position + Vector3.up * 2f, eyeColor, 80, 8f, 0.3f, 1f, -0.2f);
            FloorDirector.Popup(transform.position + Vector3.up * 4f, "¡FURIA!", new Color(1f, 0.3f, 0.2f));
            BossEnraged?.Invoke(this);
        }
    }

    void Summon(int count)
    {
        if (room == null) return;
        CameraShake.Shake(0.25f, 0.6f);
        for (int i = 0; i < count; i++)
        {
            float a = (i + 0.5f) / count * Mathf.PI * 2f;
            Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 4f;
            room.SpawnExtra(Kind.Sombra, p);
        }
    }

    void FireBolts(int count)
    {
        float offset = UnityEngine.Random.Range(0f, 360f);
        for (int i = 0; i < count; i++)
        {
            float a = (offset + i * 360f / count) * Mathf.Deg2Rad;
            Bolt.Fire(transform.position + Vector3.up * 1f, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), 6.5f, 12f, eyeColor, this);
        }
    }

    // ------------------------------------------------------------------ daño

    public bool ReceiveHit(HitInfo hit)
    {
        if (state == State.Spawning || state == State.Dying) return false;
        if (hit.attacker is TowerEnemy) return false;   // sin fuego amigo

        Health -= hit.damage;
        LastHitTime = Time.time;
        flash = 0.08f;
        Vector3 away = transform.position - hit.sourcePosition; away.y = 0f;
        away = away.sqrMagnitude > 0.001f ? away.normalized : -facing;
        knock = away * hit.knockback * knockTaken;
        CombatFx.Burst(transform.position + Vector3.up * 0.8f * Scale, eyeColor, 12, 5f, 0.14f, 0.35f);
        FloorDirector.Popup(transform.position + Vector3.up * (1.6f * Scale + 0.3f), Mathf.RoundToInt(hit.damage).ToString(),
                            hit.damage >= 20f ? new Color(1f, 0.85f, 0.4f) : Color.white);
        CameraShake.Shake(hit.damage >= 20f ? 0.12f : 0.06f, 0.12f);

        if (Health <= 0f) { Die(); return true; }

        poiseDamage += hit.damage;
        if (poiseDamage >= poise && (state == State.Telegraph || state == State.Chase || state == State.Recover))
        {
            poiseDamage = 0f;
            hitbox.End();
            Go(State.Stunned);
        }
        return true;
    }

    public void Stagger(float duration)
    {
        if (IsBoss || state == State.Dying) return;
        hitbox.End();
        Go(State.Stunned);
    }

    void Die()
    {
        hitbox.End();
        GetComponent<Collider>().enabled = false;
        Go(State.Dying);
        CameraShake.Shake(IsBoss ? 0.6f : 0.1f, IsBoss ? 1.2f : 0.15f);
        if (IsBoss) HitStop.Do(0.25f, 0.05f);
        AnyDied?.Invoke(this);
    }

    // ------------------------------------------------------------------ apoyo

    class Spin : MonoBehaviour
    {
        public float speed = 30f;
        void Update() => transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.Self);
    }

    /// <summary>Orbe lento del jefe: avanza en línea recta y daña a Abby si la toca.</summary>
    class Bolt : MonoBehaviour
    {
        Vector3 dir; float speed, life, damage; IHittable owner; Transform player; PlayerHealth ph;

        static Material mat;

        public static void Fire(Vector3 pos, Vector3 dir, float speed, float damage, Color color, IHittable owner)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Orbe";
            Destroy(go.GetComponent<Collider>());
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * 0.45f;
            if (mat == null) mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Orbe (runtime)" };
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var mpb = new MaterialPropertyBlock(); mpb.SetColor(BaseColorId, color * 5f); r.SetPropertyBlock(mpb);
            var b = go.AddComponent<Bolt>();
            b.dir = dir; b.speed = speed; b.damage = damage; b.owner = owner; b.life = 5f;
            var p = GameObject.FindWithTag("Player");
            if (p != null) { b.player = p.transform; b.ph = p.GetComponent<PlayerHealth>(); }
        }

        void Update()
        {
            transform.position += dir * speed * Time.deltaTime;
            life -= Time.deltaTime;
            if (life <= 0f) { Destroy(gameObject); return; }
            if (player == null || ph == null) return;
            Vector3 d = player.position + Vector3.up * 0.8f - transform.position;
            if (d.sqrMagnitude < 0.7f * 0.7f)
            {
                ph.ReceiveHit(new HitInfo { damage = damage, knockback = 6f, hitStop = 0.03f, sourcePosition = transform.position - dir, attacker = owner });
                CombatFx.Burst(transform.position, new Color(1f, 0.4f, 0.3f), 14, 4f, 0.15f, 0.4f);
                Destroy(gameObject);
            }
        }
    }
}
