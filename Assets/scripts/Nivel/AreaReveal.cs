using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Sala, pasillo o escondite que se ilumina conforme Abby se acerca. Siempre se dibuja (lo lejano se ve en penumbra,
/// nunca como un hueco negro), pero sus luces se apagan con la distancia y se encienden suave al acercarse,
/// como si las antorchas cobraran vida. Las luces apagadas no cuestan nada; las partículas lejanas se pausan.
/// FloorLayout la pone en cada sala, pasillo y escondite.
/// </summary>
[DefaultExecutionOrder(-500)]
public class AreaReveal : MonoBehaviour
{
    [Tooltip("Tamaño del área en el plano (x, z), en espacio local, centrada en este objeto.")]
    public Vector2 size = new Vector2(16f, 12f);
    [Tooltip("Rango vertical local en el que cuenta que Abby está en el área.")]
    public float yMin = -2f, yMax = 6f;
    [Tooltip("Metros alrededor del área en los que la luz ya está completa (asomarse por la puerta).")]
    public float margin = 3.5f;
    [Tooltip("Metros más allá del margen en los que la luz se va apagando hasta la penumbra.")]
    public float falloff = 9f;
    [Tooltip("Luz que conservan las áreas lejanas (0 = apagadas del todo).")]
    [Range(0f, 0.5f)] public float minLight = 0f;
    public float fadeSpeed = 1.6f;

    public static readonly List<AreaReveal> All = new List<AreaReveal>();
    /// <summary>false = todo iluminado (depurar o capturas del piso completo).</summary>
    public static bool Enabled = true;

    public bool Visible => level > 0.02f;
    public float Fade => level;

    Light[] lights;
    float[] lightBase;
    ParticleSystem[] particles;
    Renderer[] glows;   // llamas, halos y cristales: brillan solos aunque la luz esté apagada
    readonly HashSet<ParticleSystem> pausedByMe = new HashSet<ParticleSystem>();
    float level = 1f;
    bool scanned, lightsOn = true, particlesOn = true, glowsOn = true;
    static Transform player;

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    void Start() => Scan();

    public void Scan()
    {
        lights = GetComponentsInChildren<Light>(true);
        lightBase = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) lightBase[i] = lights[i].intensity;
        particles = GetComponentsInChildren<ParticleSystem>(true);
        var g = new List<Renderer>();
        foreach (var r in GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer) { g.Add(r); continue; }
            var m = r.sharedMaterial;
            if (m != null && m.shader != null && (m.shader.name.Contains("Unlit") || m.shader.name.Contains("Particle"))) g.Add(r);
        }
        glows = g.ToArray();
        scanned = true;
        level = Target();
        ApplyState(true);
    }

    static Vector3 PlayerPos()
    {
        if (player == null) { var p = GameObject.FindWithTag("Player"); if (p != null) player = p.transform; }
        return player != null ? player.position : new Vector3(1e6f, 0f, 1e6f);
    }

    /// <summary>Distancia (m) de un punto al borde del área en el plano; 0 si está dentro. Fuera del rango vertical cuenta como más lejos.</summary>
    public float Distance(Vector3 world)
    {
        Vector3 lp = transform.InverseTransformPoint(world);
        float dx = Mathf.Max(0f, Mathf.Abs(lp.x) - size.x / 2f), dz = Mathf.Max(0f, Mathf.Abs(lp.z) - size.y / 2f);
        float dy = lp.y < yMin ? yMin - lp.y : lp.y > yMax ? lp.y - yMax : 0f;
        return Mathf.Sqrt(dx * dx + dz * dz) + dy * 2f;
    }

    public bool Contains(Vector3 world, float extra) => Distance(world) <= extra;

    float Target()
    {
        if (!Enabled) return 1f;
        float d = Distance(PlayerPos());
        float k = 1f - Mathf.Clamp01((d - margin) / Mathf.Max(0.1f, falloff));
        k = k * k * (3f - 2f * k);   // suave
        return Mathf.Max(minLight, k);
    }

    void Update()
    {
        if (!scanned) return;
        // devuelve la intensidad "real" antes de que otros scripts (parpadeo, cofres) la cambien este cuadro
        if (lightsOn && level < 0.999f)
            for (int i = 0; i < lights.Length; i++) if (lights[i] != null) lights[i].intensity = lightBase[i];
        level = Mathf.MoveTowards(level, Target(), Time.deltaTime * fadeSpeed);
        ApplyState(false);
    }

    void LateUpdate()
    {
        if (!scanned || !lightsOn || level >= 0.999f) return;
        float k = level * level;   // las luces tardan un poco más en llegar: se siente como encender
        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) { lightBase[i] = lights[i].intensity; lights[i].intensity = lightBase[i] * k; }
    }

    void ApplyState(bool force)
    {
        bool wantLights = level > 0.01f;
        if (wantLights != lightsOn || force)
        {
            lightsOn = wantLights;
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] == null) continue;
                if (!wantLights) lights[i].intensity = lightBase[i];   // al apagar deja guardada la intensidad completa
                lights[i].enabled = wantLights;
            }
        }
        // partículas: las de áreas lejanas se pausan
        bool wantParticles = level > 0.001f || Distance(PlayerPos()) < margin + falloff * 1.5f;
        if (wantParticles != particlesOn || force)
        {
            particlesOn = wantParticles;
            foreach (var p in particles)
            {
                if (p == null) continue;
                if (!wantParticles) { if (p.isPlaying) { p.Pause(false); pausedByMe.Add(p); } }
                else if (pausedByMe.Remove(p)) p.Play(false);
            }
        }
        // con la luz apagada tampoco brillan llamas ni halos: nada de fuegos flotando a lo lejos en la negrura
        bool wantGlow = level > 0.05f;
        if (wantGlow != glowsOn || force)
        {
            glowsOn = wantGlow;
            foreach (var r in glows) if (r != null) r.forceRenderingOff = !wantGlow;
        }
    }
}
