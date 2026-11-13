using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bloqueo y stamina de Abby (GDD, Mecánica 02). Con clic derecho, K o LT sostenido Abby levanta un escudo de
/// energía que se ve alrededor de ella:
///   · Bloqueo: recibe solo el 8 % del daño y gasta 20 de stamina.
///   · Bloqueo perfecto: si lo levanta en los 0.2 s antes del golpe, no recibe daño, no gasta stamina (recupera un poco),
///     el enemigo queda aturdido y el siguiente tajo de Abby es crítico.
///   · Guardia rota: si el golpe llega y ya no alcanza la stamina, el escudo se rompe (recibe la mitad del daño) y no
///     puede volver a cubrirse por un momento. Antes simplemente no bloqueaba y parecía que no funcionaba.
/// La stamina (100) se regenera a 25 por segundo después de 1 s sin bloquear. El impulso gasta 20. Mientras bloquea
/// camina más despacio y no ataca. El escudo se pone naranja y parpadea cuando ya no aguanta otro golpe.
/// </summary>
[DefaultExecutionOrder(-90)]
public class AbbyBlock : MonoBehaviour
{
    public const float MaxStamina = 100f, CostoPorGolpe = 20f, CostoImpulso = 20f, Regeneracion = 25f, EsperaRegeneracion = 1f, DanoRecibido = 0.08f;
    public const float VentanaPerfecta = 0.2f, DanoGuardiaRota = 0.5f, EsperaTrasRotura = 0.9f;

    public float Stamina { get; private set; } = MaxStamina;
    public float Normalized => Stamina / MaxStamina;
    /// <summary>Cubierta: el botón sostenido, con algo de stamina y sin la guardia rota.</summary>
    public bool Blocking { get; private set; }
    /// <summary>Destello breve al bloquear un golpe (para el HUD y el sprite).</summary>
    public float BlockFlash { get; private set; }
    /// <summary>Quiso bloquear sin stamina o se le rompió la guardia (el HUD parpadea la barra).</summary>
    public float EmptyFlash { get; private set; }
    /// <summary>¿Acaba de levantar el escudo? (un golpe ahora es bloqueo perfecto)</summary>
    public bool EnVentanaPerfecta => Blocking && Time.time - inicio <= VentanaPerfecta;

    float sinBloquear, inicio = -10f, rotoHasta;
    bool antes;
    PlayerHealth health;
    PlayerMotor motor;
    AbbyAttack ataque;

    void Awake() { health = GetComponent<PlayerHealth>(); motor = GetComponent<PlayerMotor>(); ataque = GetComponent<AbbyAttack>(); }

    /// <summary>Clic derecho, K o LT (antes K no bloqueaba de verdad; LB es para girar la cámara).</summary>
    static bool Sostenido()
    {
        var kb = Keyboard.current; var ms = Mouse.current; var gp = Gamepad.current;
        return (ms != null && ms.rightButton.isPressed) || (kb != null && kb.kKey.isPressed)
            || Mando.BloqueoSostenido;   // LT calibrado: un gatillo que en reposo no marca 0 ya no deja el escudo puesto
    }

    static bool Pulsado()
    {
        var kb = Keyboard.current; var ms = Mouse.current; var gp = Gamepad.current;
        return (ms != null && ms.rightButton.wasPressedThisFrame) || (kb != null && kb.kKey.wasPressedThisFrame)
            || Mando.BloqueoPulsado;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        bool held = !PauseMenu.IsPaused && LoreNote.Open == null && (health == null || !health.IsDead) && Sostenido();
        bool roto = Time.time < rotoHasta;
        Blocking = held && Stamina > 0.5f && !roto;
        if (held && !Blocking && Pulsado()) { EmptyFlash = 1f; Sonido.Tocar(Sonido.Efecto.Clic, 0.5f, 0.6f); }

        if (Blocking && !antes)
        {
            // levanta el escudo: aquí empieza la ventana del bloqueo perfecto
            inicio = Time.time;
            escudoGolpe = 0.5f;
            Sonido.Tocar(Sonido.Efecto.Espada, 0.15f, 1.5f);
            CombatFx.Ring(transform.position, 1.3f, new Color(0.5f, 0.75f, 1f, 0.8f), 0.18f);
        }
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

    /// <summary>Gasta stamina si alcanza (la recuperación vuelve a esperar); si no, la barra parpadea y devuelve false.</summary>
    public bool Gastar(float costo)
    {
        if (Stamina < costo) { EmptyFlash = 1f; return false; }
        Stamina -= costo;
        sinBloquear = 0f;
        return true;
    }

    /// <summary>
    /// Lo llama PlayerHealth al recibir un golpe. true = el escudo lo paró (perfecto: daño 0; normal: 8 %).
    /// false = no estaba cubierta, o se le rompió la guardia (en ese caso el daño ya viene a la mitad).
    /// </summary>
    public bool TryBlock(ref HitInfo hit)
    {
        if (!Blocking) return false;
        Vector3 pecho = transform.position + Vector3.up * 0.9f;
        sinBloquear = 0f;

        if (Time.time - inicio <= VentanaPerfecta)
        {
            // ── bloqueo perfecto
            hit.damage = 0f; hit.knockback = 0f;
            Stamina = Mathf.Min(MaxStamina, Stamina + 10f);
            BlockFlash = 1f; escudoGolpe = 1.6f; perfecto = 1f;
            inicio = -10f;   // uno por levantada
            Sonido.Tocar(Sonido.Efecto.Parada, 1f);
            HitStop.Do(0.14f, 0.05f);
            CameraShake.Shake(0.12f, 0.15f);
            if (hit.attacker != null && hit.attacker != (IHittable)health) hit.attacker.Stagger(1.2f);
            if (ataque != null) ataque.DarCritico(1.8f);
            CombatFx.Burst(pecho, new Color(1f, 0.92f, 0.6f), 30, 6f, 0.13f, 0.45f, 0f);
            CombatFx.Ring(transform.position, 2.4f, new Color(1f, 0.9f, 0.55f, 1f), 0.3f);
            FloorDirector.Popup(transform.position + Vector3.up * 2.3f, "¡BLOQUEO PERFECTO!", new Color(1f, 0.88f, 0.45f), 1.1f);
            return true;
        }

        if (Stamina >= CostoPorGolpe)
        {
            // ── bloqueo normal
            Stamina -= CostoPorGolpe;
            hit.damage *= DanoRecibido;
            hit.knockback *= 0.35f;
            BlockFlash = 1f; escudoGolpe = 1f;
            Sonido.Tocar(Sonido.Efecto.Bloqueo, 0.75f, Random.Range(0.95f, 1.05f));
            CombatFx.Burst(pecho, new Color(0.55f, 0.75f, 1f), 18, 4f, 0.12f, 0.3f);
            CombatFx.Ring(transform.position, 1.6f, new Color(0.5f, 0.75f, 1f, 0.9f), 0.25f);
            FloorDirector.Popup(transform.position + Vector3.up * 2.1f, "BLOQUEO", new Color(0.6f, 0.8f, 1f), 0.6f);
            return true;
        }

        // ── guardia rota: no alcanzó la stamina
        Stamina = 0f;
        Blocking = false; antes = false;
        rotoHasta = Time.time + EsperaTrasRotura;
        hit.damage *= DanoGuardiaRota;
        EmptyFlash = 1f; escudoGolpe = 0f; escudoA = 0f; roto = 1f;
        Sonido.Tocar(Sonido.Efecto.GuardiaRota, 1f);
        CombatFx.Burst(pecho, new Color(1f, 0.55f, 0.2f), 26, 6f, 0.14f, 0.5f);
        FloorDirector.Popup(transform.position + Vector3.up * 2.3f, "¡GUARDIA ROTA!", new Color(1f, 0.55f, 0.25f), 1f);
        return false;
    }

    /// <summary>Al reiniciar un piso o recibir la fuente.</summary>
    public void Refill() => Stamina = MaxStamina;

    // ───────────────────────────── escudo visible ─────────────────────────────

    GameObject escudo;
    Material escudoMat;
    MeshRenderer escudoMr;
    MaterialPropertyBlock mpb;
    float escudoA, escudoGolpe, perfecto, roto;
    static Mesh quad;
    static Texture2D texEscudo;
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    void LateUpdate()
    {
        float dt = Time.deltaTime;
        escudoA = Mathf.MoveTowards(escudoA, Blocking ? 1f : 0f, dt * (Blocking ? 12f : 6f));
        escudoGolpe = Mathf.MoveTowards(escudoGolpe, 0f, dt * 4f);
        perfecto = Mathf.MoveTowards(perfecto, 0f, dt * 2.5f);
        roto = Mathf.MoveTowards(roto, 0f, dt * 3f);
        if (escudoA <= 0.01f && perfecto <= 0f)
        {
            if (escudo != null && escudo.activeSelf) escudo.SetActive(false);
            return;
        }
        if (escudo == null) CrearEscudo();
        if (!escudo.activeSelf) escudo.SetActive(true);

        var cam = Camera.main;
        float t = Time.time;
        Vector3 haciaCam = cam != null ? -cam.transform.forward : Vector3.back;
        escudo.transform.position = transform.position + Vector3.up * 0.95f + haciaCam * 0.3f;
        if (cam != null) escudo.transform.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, t * 25f);
        float tam = 2.3f * (0.8f + 0.2f * escudoA) * (1f + 0.16f * escudoGolpe);
        escudo.transform.localScale = new Vector3(tam, tam, 1f);

        // azul normal; dorado en la ventana del bloqueo perfecto y al lograrlo; naranja parpadeando si ya no aguanta otro golpe
        Color c = new Color(0.45f, 0.75f, 1f);
        float a = 0.7f + 0.1f * Mathf.Sin(t * 6f);
        if (Blocking && Stamina < CostoPorGolpe)
        {
            c = new Color(1f, 0.3f, 0.05f);
            a = 0.95f * (Mathf.Repeat(t * 6f, 1f) < 0.5f ? 1f : 0.65f);
        }
        if (EnVentanaPerfecta) c = Color.Lerp(c, new Color(1f, 0.95f, 0.75f), 0.55f);
        c = Color.Lerp(c, new Color(1f, 0.88f, 0.5f), perfecto);
        a = Mathf.Clamp01(a * Mathf.Max(escudoA, perfecto) + escudoGolpe * 0.35f);
        mpb.SetColor(BaseColor, new Color(c.r, c.g, c.b, a));
        escudoMr.SetPropertyBlock(mpb);
    }

    void CrearEscudo()
    {
        escudo = new GameObject("EscudoAbby");
        escudo.transform.SetParent(transform, false);
        if (quad == null)
        {
            quad = new Mesh { name = "QuadEscudo" };
            quad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f) };
            quad.colors = new[] { Color.white, Color.white, Color.white, Color.white };
            quad.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
            quad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            quad.RecalculateBounds();
        }
        escudo.AddComponent<MeshFilter>().sharedMesh = quad;
        var mr = escudoMr = escudo.AddComponent<MeshRenderer>();
        mpb = new MaterialPropertyBlock();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        escudoMat = CombatFx.MaterialAditivo(TexturaEscudo());
        mr.sharedMaterial = escudoMat;
    }

    void OnDestroy() { if (escudoMat != null) Destroy(escudoMat); }

    /// <summary>Burbuja de energía: borde brillante, brillo que crece hacia la orilla, una trama de triángulos y seis runas.</summary>
    static Texture2D TexturaEscudo()
    {
        if (texEscudo != null) return texEscudo;
        const int n = 128;
        texEscudo = new Texture2D(n, n, TextureFormat.RGBA32, false) { name = "EscudoAbby", wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var px = new Color[n * n];
        Vector2[] normales = { new Vector2(1f, 0f), new Vector2(0.5f, 0.866f), new Vector2(-0.5f, 0.866f) };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var p = new Vector2((x + 0.5f) / n * 2f - 1f, (y + 0.5f) / n * 2f - 1f);
                float r = p.magnitude, a = 0f;
                // borde
                a += Mathf.Clamp01(1f - Mathf.Abs(r - 0.9f) / 0.055f);
                // brillo hacia la orilla (como una burbuja vista de frente)
                if (r < 0.9f) a += Mathf.Pow(Mathf.SmoothStep(0.3f, 0.9f, r), 2f) * 0.45f;
                // trama de triángulos, más visible hacia el borde
                if (r < 0.86f)
                {
                    float linea = 0f;
                    foreach (var nm in normales)
                    {
                        float v = Vector2.Dot(p, nm) / 0.2f;
                        linea = Mathf.Max(linea, Mathf.Clamp01(1f - Mathf.Abs(v - Mathf.Round(v)) / 0.07f));
                    }
                    a += linea * (0.1f + 0.22f * Mathf.SmoothStep(0.2f, 0.86f, r));
                }
                // seis runas en el anillo de adentro
                float ang = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
                float sector = Mathf.Abs(Mathf.DeltaAngle(ang, Mathf.Round(ang / 60f) * 60f));
                if (r > 0.72f && r < 0.79f && sector < 9f) a += 0.8f;
                if (r >= 0.97f) a = 0f;
                px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(a));
            }
        texEscudo.SetPixels(px); texEscudo.Apply();
        return texEscudo;
    }
}
