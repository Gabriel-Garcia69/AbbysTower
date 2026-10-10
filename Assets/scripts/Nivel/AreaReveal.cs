using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sala o pasillo que solo se ve cuando Abby está dentro o muy cerca: así la cámara no enseña todo el piso a la vez.
/// Oculta: sus renderers no se dibujan (forceRenderingOff, no toca enabled) y sus luces se apagan.
/// Al acercarse: aparece en penumbra y sus luces suben poco a poco, como si se encendieran las antorchas;
/// al alejarse se apagan y desaparece. Los colliders y la lógica siguen activos siempre.
/// FloorLayout la pone en cada sala y pasillo. Los escondites usan un margen casi cero (se revelan al entrar).
/// </summary>
[DefaultExecutionOrder(-500)]
public class AreaReveal : MonoBehaviour
{
    [Tooltip("Tamaño del área en el plano (x, z), en espacio local, centrada en este objeto.")]
    public Vector2 size = new Vector2(16f, 12f);
    [Tooltip("Rango vertical local en el que cuenta que Abby está en el área.")]
    public float yMin = -2f, yMax = 6f;
    [Tooltip("Metros alrededor del área desde los que ya se ve (asomarse por la puerta).")]
    public float margin = 3.5f;
    public float fadeSeconds = 0.6f;

    public static readonly List<AreaReveal> All = new List<AreaReveal>();
    /// <summary>false = todo visible (para depurar o capturas del piso completo).</summary>
    public static bool Enabled = true;

    public bool Visible { get; private set; }
    public float Fade => fade;

    Renderer[] renderers;
    Light[] lights;
    ParticleSystem[] particles;
    float[] lightBase;
    float fade;            // 0 oculta … 1 visible
    readonly System.Collections.Generic.HashSet<ParticleSystem> pausedByMe = new System.Collections.Generic.HashSet<ParticleSystem>();
    bool hiddenApplied, scanned;
    static Transform player;

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start() => Scan();

    /// <summary>Vuelve a buscar renderers y luces (llamar si se agregan objetos a la sala en juego).</summary>
    public void Scan()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        lights = GetComponentsInChildren<Light>(true);
        particles = GetComponentsInChildren<ParticleSystem>(true);
        lightBase = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) lightBase[i] = lights[i].intensity;
        scanned = true;
        bool inside = Contains(PlayerPos(), margin);
        fade = inside || !Enabled ? 1f : 0f;
        Visible = fade > 0f;
        Apply(true);
    }

    static Vector3 PlayerPos()
    {
        if (player == null) { var p = GameObject.FindWithTag("Player"); if (p != null) player = p.transform; }
        return player != null ? player.position : new Vector3(1e6f, 0f, 1e6f);
    }

    public bool Contains(Vector3 world, float extra)
    {
        Vector3 lp = transform.InverseTransformPoint(world);
        return Mathf.Abs(lp.x) <= size.x / 2f + extra && Mathf.Abs(lp.z) <= size.y / 2f + extra && lp.y >= yMin && lp.y <= yMax;
    }

    void Update()
    {
        if (!scanned) return;
        // devuelve a las luces la intensidad "real" antes de que otros scripts (parpadeo, cofres) la cambien este cuadro
        if (fade > 0f && fade < 1f)
            for (int i = 0; i < lights.Length; i++) if (lights[i] != null) lights[i].intensity = lightBase[i];

        bool want = !Enabled || Contains(PlayerPos(), margin);
        float target = want ? 1f : 0f;
        float before = fade;
        fade = Mathf.MoveTowards(fade, target, Time.deltaTime / Mathf.Max(0.01f, fadeSeconds));
        if (fade != before || (fade == 0f) != hiddenApplied) Apply(false);
    }

    void LateUpdate()
    {
        if (!scanned || fade <= 0f || fade >= 1f) return;
        float k = fade * fade;   // las luces tardan un poco más en llegar: se siente como encender
        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) { lightBase[i] = lights[i].intensity; lights[i].intensity = lightBase[i] * k; }
    }

    void Apply(bool force)
    {
        bool hide = fade <= 0f;
        Visible = !hide;
        if (hide == hiddenApplied && !force)
        {
            if (fade >= 1f) for (int i = 0; i < lights.Length; i++) if (lights[i] != null) lights[i].intensity = lightBase[i];
            return;
        }
        hiddenApplied = hide;
        // al apagar deja la intensidad completa guardada: al volver a encender se parte de ella
        if (hide) for (int i = 0; i < lights.Length; i++) if (lights[i] != null) lights[i].intensity = lightBase[i];
        foreach (var r in renderers) if (r != null) r.forceRenderingOff = hide;
        foreach (var l in lights) if (l != null) l.enabled = !hide;
        // partículas: en pausa mientras nadie las ve (las que estaban apagadas, como el fuego de los respiraderos, no se tocan)
        foreach (var p in particles)
        {
            if (p == null) continue;
            if (hide) { if (p.isPlaying) { p.Pause(false); pausedByMe.Add(p); } }
            else if (pausedByMe.Remove(p)) p.Play(false);
        }
    }
}
