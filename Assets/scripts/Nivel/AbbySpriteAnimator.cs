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

    [Header("Conjunto de otro personaje (vacío = los sprites de Abby de este prefab)")]
    public ConjuntoSprites conjunto;
    [Tooltip("Se multiplica por el color del sprite en todo momento; Aplicar lo toma del conjunto.")]
    public Color tinteBase = Color.white;
    /// <summary>Lo manda el final: Abby camina hacia ahí aunque el jugador ya no la controle, o se queda de rodillas.</summary>
    [System.NonSerialized] public Vector3 caminarHacia;
    [System.NonSerialized] public bool deRodillas;

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
    [Tooltip("Correr (8 direcciones). Si falta una dirección, se usa la de caminar.")]
    public Frames[] run = new Frames[8];
    [Header("Poses (un solo dibujo por dirección, mismo personaje): golpe y guardia")]
    public Sprite[] golpePose = new Sprite[8];
    public Sprite[] guardia = new Sprite[8];
    [Header("Derrota: cae de rodillas y luego queda tendida")]
    public Sprite[] rodilla = new Sprite[8];
    public Sprite[] tendida = new Sprite[8];
    public float tiempoRodilla = 0.8f;
    [Tooltip("Usa las poses corregidas (quieta, golpe y guardia: espada en la mano derecha y 1.31 m) en lugar de las animaciones de respirar, espadazo y bloqueo.")]
    public bool posesCorregidas = true;

    // tajos del combo (Resources/AbbyAnim/Tajo1..3/<dirección>/frame_XXX): [tajo][dirección][cuadro]; las direcciones del oeste que falten se espejan
    Sprite[][][] tajos;
    bool espejo;   // el material de los sprites ignora flipX: se voltea con la escala
    static readonly int[] EspejoDir = { 4, 3, 2, 1, 0, 7, 6, 5 };

    void CargarTajos()
    {
        tajos = new Sprite[3][][];
        for (int k = 0; k < 3; k++)
        {
            Sprite[][] t = null;
            for (int d = 0; d < 8; d++)
            {
                var fr = Resources.LoadAll<Sprite>($"AbbyAnim/Tajo{k + 1}/{Names[d]}");
                if (fr == null || fr.Length == 0) continue;
                System.Array.Sort(fr, (a, b) => string.CompareOrdinal(a.name, b.name));
                if (t == null) t = new Sprite[8][];
                t[d] = fr;
            }
            tajos[k] = t;
        }
    }

    /// <summary>Cuadros del tajo que toca en la dirección actual (el del combo, o el primero si ese aún no existe); null si no hay.</summary>
    Sprite[] CuadrosTajo(int k, int d, out bool volteado)
    {
        volteado = false;
        var t = tajos != null ? (tajos[Mathf.Clamp(k, 0, 2)] ?? tajos[0]) : null;
        if (t == null) return null;
        if (t[d] != null) return t[d];
        volteado = t[EspejoDir[d]] != null;
        return t[EspejoDir[d]];
    }
    public AbbyBlock block;
    public PlayerHealth health;

    [Header("Ritmo")]
    [Header("Pasos (el ritmo sigue a la velocidad real para que los pies no patinen)")]
    public float walkFps = 12f;
    [Tooltip("Velocidad (m/s) a la que el caminar corre a walkFps; a más velocidad, más rápido.")]
    public float walkRefSpeed = 2.5f;
    public float runFps = 14f;
    [Tooltip("Velocidad (m/s) a la que el correr corre a runFps (la velocidad normal de Abby es 5).")]
    public float runRefSpeed = 5f;
    [Tooltip("Pasa a correr por encima de runEnter y vuelve a caminar por debajo de runExit (m/s).")]
    public float runEnter = 3.6f, runExit = 3f;
    [Tooltip("Cuadros del dash que se usan como pose de ataque (estocada).")]
    public int attackPoseFrom = 2, attackPoseTo = 5;
    [Tooltip("Respiración en reposo: cuánto se estira el sprite en vertical.")]
    public float breathe = 0.012f;
    public float idleFps = 6f;
    [Tooltip("Lo que dura la reacción al recibir un golpe.")]
    public float hurtTime = 0.3f;

    /// <summary>Retrato del HUD: el del conjunto si lo trae, si no el sprite de Abby de frente.</summary>
    public Sprite Retrato => conjunto != null && conjunto.retrato != null ? conjunto.retrato : (idle != null && idle.Length > 6 ? idle[6] : null);

    /// <summary>Carga un conjunto de sprites: lo que el conjunto trae reemplaza a lo de Abby; lo que deja vacío se queda.</summary>
    public void Aplicar(ConjuntoSprites c)
    {
        conjunto = c;
        tinteBase = c != null ? c.tinte : Color.white;
        if (c == null) return;
        if (c.soloPropios)
        {
            // nada de Abby: ni tajos, ni guardia, ni impulso, ni derrota
            tajos = null;
            golpePose = guardia = new Sprite[8];
            rodilla = tendida = c.idle;
            dashFrames = new Frames[8]; run = new Frames[8]; blockFrames = new Frames[8]; hurt = new Frames[8]; idleAnim = new Frames[8];
        }
        if (Alguno(c.idle)) idle = c.idle;
        if (Alguno(c.golpePose)) golpePose = c.golpePose;
        if (Alguno(c.guardia)) guardia = c.guardia;
        if (Alguno(c.rodilla)) rodilla = c.rodilla;
        if (Alguno(c.tendida)) tendida = c.tendida;
        if (Alguno(c.walk)) walk = c.walk;
        if (Alguno(c.run)) run = c.run;
        if (Alguno(c.dashFrames)) dashFrames = c.dashFrames;
        if (Alguno(c.slash)) slash = c.slash;
        if (Alguno(c.blockFrames)) blockFrames = c.blockFrames;
        if (Alguno(c.hurt)) hurt = c.hurt;
        if (Alguno(c.idleAnim)) idleAnim = c.idleAnim;
    }

    static bool Alguno(Sprite[] s) { if (s != null) foreach (var x in s) if (x != null) return true; return false; }
    static bool Alguno(Frames[] f) { if (f != null) foreach (var x in f) if (x != null && x.Has) return true; return false; }

    static readonly string[] Names = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };

    int dir = 6;
    bool correndo;
    float walkTime, dashTime, attackTime, idleTime, blockTime, hurtLeft;
    int lastSwing;
    float muerteTime;
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
        CargarTajos();
        if (posesCorregidas && conjunto == null)
        {
            if (Alguno(idle)) idleAnim = new Frames[8];
            if (Alguno(golpePose)) slash = new Frames[8];
            if (Alguno(guardia)) blockFrames = new Frames[8];
            // al norte y al sur la carrera pierde la espada en la mitad de los cuadros: ahí corre con los cuadros de caminar
            if (run != null && run.Length == 8) { run[2] = new Frames(); run[6] = new Frames(); }
        }
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
        if (caminarHacia.sqrMagnitude > 0.01f) facing = caminarHacia;
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

        float giro = 0f; Vector3 empuje = Vector3.zero; Color tinte = tinteBase;
        if (health != null && health.IsDead)
        {
            // derrota: de rodillas un momento y luego tendida en el suelo (mirando hacia donde miraba)
            muerteTime += dt;
            var poses = muerteTime < tiempoRodilla ? rodilla : tendida;
            if (poses != null && poses.Length > dir && poses[dir] != null) s = poses[dir];
            else if (rodilla != null && rodilla.Length > dir && rodilla[dir] != null) s = rodilla[dir];
            if (s != null)
            {
                float k = Mathf.Clamp01(muerteTime / 0.25f);
                stretch = muerteTime < tiempoRodilla ? 1f - 0.06f * (1f - k) : 1f;
                tinte = tinteBase * Color.Lerp(new Color(1f, 0.55f, 0.55f), new Color(0.82f, 0.78f, 0.9f), Mathf.Clamp01(muerteTime / 1.5f));
            }
        }
        else muerteTime = 0f;
        espejo = false;
        if (s == null && deRodillas && rodilla != null && rodilla.Length > dir && rodilla[dir] != null) s = rodilla[dir];
        if (s == null && caminarHacia.sqrMagnitude > 0.01f && walk[dir] != null && walk[dir].Has)
        {
            walkTime += dt * walkFps * 0.7f;   // pasos lentos
            s = walk[dir].frames[Mathf.FloorToInt(walkTime) % walk[dir].frames.Length];
        }
        if (s == null && conjunto != null && conjunto.soloPropios)
        {
            // de pie tiene las 8 direcciones; caminar y atacar, las del oeste se voltean
            var pose = attacking ? slash : (dashing || motor.IsMoving || !motor.IsGrounded) ? walk : null;
            if (pose != null)
            {
                int d = Has(pose, dir) ? dir : EspejoDir[dir];
                if (Has(pose, d))
                {
                    espejo = d != dir;
                    var fr = pose[d].frames;
                    if (attacking)
                    {
                        if (attack.Swings != lastSwing) { lastSwing = attack.Swings; attackTime = 0f; }
                        attackTime += dt;
                        s = fr[Mathf.Clamp(Mathf.FloorToInt(attackTime / Mathf.Max(0.05f, attack.SwingLength) * fr.Length), 0, fr.Length - 1)];
                    }
                    else
                    {
                        walkTime += dt * walkFps * Mathf.Clamp(motor.HorizontalSpeed / Mathf.Max(0.1f, walkRefSpeed), 0.4f, 1.8f);
                        s = fr[Mathf.FloorToInt(walkTime) % fr.Length];
                    }
                }
            }
        }
        var cuadrosTajo = s == null && attacking ? CuadrosTajo(attack.ComboIndex, dir, out espejo) : null;
        if (s != null) { }
        else if (cuadrosTajo != null)
        {
            // el primer cuadro es la carga (dura el windup) y los demás se reparten en lo que dura el golpe
            if (attack.Swings != lastSwing) { lastSwing = attack.Swings; attackTime = 0f; }
            attackTime += dt;
            int n = cuadrosTajo.Length - 1;
            float u = (attackTime - attack.windup) / Mathf.Max(0.05f, attack.SwingLength - attack.windup);
            s = cuadrosTajo[attackTime < attack.windup ? 0 : Mathf.Clamp(1 + Mathf.FloorToInt(u * n), 1, n)];
        }
        else if (attacking && !Has(slash, dir) && golpePose != null && golpePose.Length > dir && golpePose[dir] != null)
        {
            // pose de golpe: un instante de carga (alza la espada con la pose de guardia) y luego el tajo, sin voltear la espada
            if (attack.Swings != lastSwing) { lastSwing = attack.Swings; attackTime = 0f; }
            attackTime += dt;
            bool fuerte = attack.ComboIndex >= 2;
            float lado = DirX(dir);
            if (attackTime < attack.windup)
            {
                s = guardia != null && guardia.Length > dir && guardia[dir] != null ? guardia[dir] : idle[dir] != null ? idle[dir] : sr.sprite;
                stretch = 0.97f;
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
        if (s == null && blocking && !Has(blockFrames, dir) && guardia != null && guardia.Length > dir && guardia[dir] != null)
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
            float vel = motor.HorizontalSpeed;
            correndo = vel > (correndo ? runExit : runEnter);
            bool corre = correndo && Has(run, dir);
            var f = corre ? run[dir] : walk[dir];
            if (f != null && f.Has)
            {
                float fps = (corre ? runFps : walkFps) * Mathf.Clamp(vel / Mathf.Max(0.1f, corre ? runRefSpeed : walkRefSpeed), 0.4f, 1.8f);
                walkTime += dt * fps;   // fase en cuadros: el ritmo cambia sin saltos de cuadro
                s = f.frames[Mathf.FloorToInt(walkTime) % f.frames.Length];
            }
        }
        else if (!motor.IsMoving) { walkTime = 0f; correndo = false; }

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
            tinte = tinteBase * Color.Lerp(Color.white, new Color(1f, 0.45f, 0.45f), k);
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
            transform.localScale = new Vector3(espejo ? -sc.x : sc.x, sc.y * stretch, sc.z);
            transform.rotation = transform.rotation * Quaternion.Euler(0f, 0f, giro);
        }
        else
        {
            transform.localScale = new Vector3(espejo ? -baseScale.x : baseScale.x, baseScale.y * stretch, baseScale.z);
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
    const string PixelLab = "Assets/sprites/Abby/PixelLab";

    /// <summary>Carpeta de PixelLab cuyo nombre (largo, generado) empieza por el prefijo.</summary>
    static string Carpeta(string prefijo)
    {
        foreach (var sub in AssetDatabase.GetSubFolders(PixelLab + "/Idle/animations"))
            if (System.IO.Path.GetFileName(sub).StartsWith(prefijo)) return System.IO.Path.GetFileName(sub);
        return prefijo;
    }

    [ContextMenu("Auto-asignar sprites de Abby")]
    public void AutoAssign()
    {
        Undo.RecordObject(this, "Auto-asignar sprites de Abby");
        idle = new Sprite[8]; walk = new Frames[8]; dashFrames = new Frames[8];
        slash = new Frames[8]; blockFrames = new Frames[8]; hurt = new Frames[8]; idleAnim = new Frames[8]; run = new Frames[8];
        for (int i = 0; i < 8; i++)
        {
            idle[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/rotations/{Names[i]}.png");
            dashFrames[i] = new Frames { frames = LoadFrames($"{Root}/animations/Dash/{Names[i]}") };
            idleAnim[i] = new Frames { frames = LoadFrames($"{PixelLab}/Idle/animations/{Carpeta("idle_breathing")}/{Names[i]}") };
            walk[i] = new Frames { frames = LoadFrames($"{PixelLab}/Idle/animations/Walking/{Names[i]}") };
            slash[i] = new Frames { frames = LoadFrames($"{PixelLab}/Idle/animations/{Carpeta("quick_horizontal_sword_slash")}/{Names[i]}") };
            blockFrames[i] = new Frames { frames = LoadFrames($"{PixelLab}/Idle/animations/{Carpeta("defensive_block")}/{Names[i]}") };
            hurt[i] = new Frames { frames = LoadFrames($"{PixelLab}/Idle/animations/{Carpeta("taking_a_hit")}/{Names[i]}") };
            run[i] = new Frames { frames = LoadFrames($"{PixelLab}/Idle_copy_2/animations/Running/{Names[i]}") };
        }
        // Noroeste: la espada queda detrás del cuerpo en el Walking (0-1 px); el Running tiene más píxeles de espada
        if (Has(run, 3)) walk[3] = run[3];
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
