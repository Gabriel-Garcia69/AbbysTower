using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Efectos de combate hechos en tiempo de ejecución (sin prefabs): tajos, estallidos de partículas,
/// anillos de aviso en el suelo y líneas de embestida. Todo usa el shader Abby/Particle (aditivo).
/// FloorDirector le pasa un material del proyecto para que el shader entre en el build.
/// </summary>
public static class CombatFx
{
    public static Material sourceMaterial;   // lo asigna FloorDirector (opcional)

    static Material additive;
    static Mesh slashMesh, ringMesh, quadMesh;
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    static Material Additive
    {
        get
        {
            if (additive != null) return additive;
            Shader sh = sourceMaterial != null ? sourceMaterial.shader : Shader.Find("Abby/Particle");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            additive = new Material(sh) { name = "FX_Additive (runtime)" };
            additive.SetTexture("_BaseMap", SoftDot());
            additive.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            additive.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            additive.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return additive;
        }
    }

    static Texture2D dot;
    static Texture2D SoftDot()
    {
        if (dot != null) return dot;
        dot = new Texture2D(32, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f)) / 16f;
                float a = Mathf.Clamp01(1f - d); a *= a;
                dot.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        dot.Apply();
        return dot;
    }

    static Texture2D white;
    static Material additiveFlat;
    /// <summary>Igual que Additive pero sin el degradado redondo (para mallas con alfa por vértice).</summary>
    static Material AdditiveFlat
    {
        get
        {
            if (additiveFlat != null) return additiveFlat;
            additiveFlat = new Material(Additive) { name = "FX_AdditiveFlat (runtime)" };
            if (white == null) { white = new Texture2D(2, 2); white.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white }); white.Apply(); }
            additiveFlat.SetTexture("_BaseMap", white);
            return additiveFlat;
        }
    }

    // ------------------------------------------------------------------ tajo

    /// <summary>Media luna brillante delante del personaje. arcDeg: abertura; flip invierte el sentido del barrido.</summary>
    public static void Slash(Vector3 center, Vector3 facing, float radius, Color color, bool flip, float life = 0.16f)
    {
        if (slashMesh == null) slashMesh = BuildArc(130f, 0.55f, 24);
        facing.y = 0f;
        if (facing.sqrMagnitude < 0.001f) facing = Vector3.forward;
        var go = new GameObject("FX_Tajo");
        go.transform.position = center;
        go.transform.rotation = Quaternion.LookRotation(facing) * Quaternion.Euler(flip ? 12f : -12f, 0f, flip ? 180f : 0f);
        go.transform.localScale = Vector3.one * radius;
        go.AddComponent<MeshFilter>().sharedMesh = slashMesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = AdditiveFlat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<FxFade>().Init(color, life, 1.15f);
    }

    /// <summary>Arco plano en XZ (mira a +Z) con alfa por vértice: más intenso en el borde exterior.</summary>
    static Mesh BuildArc(float arcDeg, float inner, int segs)
    {
        var v = new List<Vector3>(); var c = new List<Color>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int i = 0; i <= segs; i++)
        {
            float k = i / (float)segs;
            float a = Mathf.Lerp(-arcDeg / 2f, arcDeg / 2f, k) * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            float taper = Mathf.Sin(k * Mathf.PI);                    // las puntas se afinan
            float alpha = Mathf.Pow(k, 0.6f);                          // el final del barrido brilla más
            v.Add(dir * Mathf.Lerp(1f, inner, taper)); c.Add(new Color(1, 1, 1, 0f));
            v.Add(dir); c.Add(new Color(1, 1, 1, alpha));
            uv.Add(new Vector2(k, 0)); uv.Add(new Vector2(k, 1));
            if (i < segs) { int b = i * 2; t.AddRange(new[] { b, b + 1, b + 3, b, b + 3, b + 2 }); }
        }
        var m = new Mesh { name = "Arc" };
        m.SetVertices(v); m.SetColors(c); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateBounds();
        return m;
    }

    // ------------------------------------------------------------------ partículas

    /// <summary>Estallido de chispas (golpes, muertes, curación).</summary>
    public static void Burst(Vector3 pos, Color color, int count, float speed = 5f, float size = 0.18f, float life = 0.5f, float gravity = 0.6f)
    {
        var go = new GameObject("FX_Estallido");
        go.transform.position = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = false; main.playOnAwake = false;
        main.duration = 0.1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.4f, speed);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.5f, size);
        main.startColor = color;
        main.gravityModifier = gravity;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.Destroy;
        var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 0.2f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 1, 1, 0.2f));
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = Additive;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
    }

    // ------------------------------------------------------------------ avisos en el suelo

    /// <summary>Anillo en el suelo que se llena durante 'time' segundos (aviso del golpe en área del jefe). Devuelve el objeto.</summary>
    public static GameObject Ring(Vector3 pos, float radius, Color color, float time)
    {
        if (ringMesh == null) ringMesh = BuildRing(0.86f, 48);
        var go = new GameObject("FX_Aviso");
        go.transform.position = pos + Vector3.up * 0.04f;
        go.transform.localScale = Vector3.one * radius;
        go.AddComponent<MeshFilter>().sharedMesh = ringMesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = AdditiveFlat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<FxFade>().Init(color, time, 1f, true);

        // relleno que crece desde el centro: cuando toca el borde, pega
        var fill = new GameObject("Relleno");
        fill.transform.SetParent(go.transform, false);
        fill.AddComponent<MeshFilter>().sharedMesh = BuildDisc(32);
        var fr = fill.AddComponent<MeshRenderer>();
        fr.sharedMaterial = AdditiveFlat;
        fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fill.AddComponent<FxGrow>().Init(new Color(color.r, color.g, color.b, color.a * 0.35f), time);
        return go;
    }

    /// <summary>Franja en el suelo que marca hacia dónde va a embestir un enemigo.</summary>
    public static GameObject Line(Vector3 from, Vector3 dir, float length, float width, Color color, float time)
    {
        if (quadMesh == null)
        {
            quadMesh = new Mesh { name = "FxQuad" };
            quadMesh.SetVertices(new List<Vector3> { new Vector3(-0.5f, 0, 0), new Vector3(0.5f, 0, 0), new Vector3(-0.5f, 0, 1), new Vector3(0.5f, 0, 1) });
            quadMesh.SetColors(new List<Color> { Color.white, Color.white, new Color(1, 1, 1, 0.1f), new Color(1, 1, 1, 0.1f) });
            quadMesh.SetTriangles(new[] { 0, 2, 3, 0, 3, 1 }, 0);
            quadMesh.RecalculateBounds();
        }
        dir.y = 0f;
        var go = new GameObject("FX_Embestida");
        go.transform.position = from + Vector3.up * 0.05f;
        go.transform.rotation = Quaternion.LookRotation(dir.sqrMagnitude > 0.001f ? dir : Vector3.forward);
        go.transform.localScale = new Vector3(width, 1f, length);
        go.AddComponent<MeshFilter>().sharedMesh = quadMesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = AdditiveFlat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        go.AddComponent<FxFade>().Init(color, time, 1f, true);
        return go;
    }

    static Mesh BuildRing(float inner, int segs)
    {
        var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
        for (int i = 0; i <= segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f;
            var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            v.Add(d * inner); c.Add(new Color(1, 1, 1, 0.2f));
            v.Add(d); c.Add(Color.white);
            if (i < segs) { int b = i * 2; t.AddRange(new[] { b, b + 3, b + 1, b, b + 2, b + 3 }); }
        }
        var m = new Mesh { name = "FxRing" };
        m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0); m.RecalculateBounds();
        return m;
    }

    static Mesh discMesh;
    static Mesh BuildDisc(int segs)
    {
        if (discMesh != null) return discMesh;
        var v = new List<Vector3> { Vector3.zero }; var c = new List<Color> { new Color(1, 1, 1, 0.3f) }; var t = new List<int>();
        for (int i = 0; i < segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f;
            v.Add(new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a))); c.Add(Color.white);
            t.AddRange(new[] { 0, 1 + (i + 1) % segs, 1 + i });
        }
        discMesh = new Mesh { name = "FxDisc" };
        discMesh.SetVertices(v); discMesh.SetColors(c); discMesh.SetTriangles(t, 0); discMesh.RecalculateBounds();
        return discMesh;
    }

    // ------------------------------------------------------------------ componentes de apoyo

    /// <summary>Desvanece (y opcionalmente agranda) un efecto y lo destruye.</summary>
    class FxFade : MonoBehaviour
    {
        Color color; float life, t, grow; bool pulse;
        MeshRenderer mr; MaterialPropertyBlock mpb; Vector3 baseScale;

        public void Init(Color c, float l, float g, bool pulsing = false)
        {
            color = c; life = Mathf.Max(0.01f, l); grow = g; pulse = pulsing;
            mr = GetComponent<MeshRenderer>(); mpb = new MaterialPropertyBlock(); baseScale = transform.localScale;
            Set(0f);
        }

        void Update()
        {
            t += Time.deltaTime;
            Set(t / life);
            if (t >= life) Destroy(gameObject);
        }

        void Set(float k)
        {
            float a = pulse ? (0.55f + 0.45f * Mathf.Sin(t * 18f)) * Mathf.Clamp01(k * 4f) : 1f - k * k;
            mpb.SetColor(BaseColor, new Color(color.r, color.g, color.b, color.a * a));
            mr.SetPropertyBlock(mpb);
            if (!pulse) transform.localScale = baseScale * Mathf.Lerp(1f, grow, k);
        }
    }

    class FxGrow : MonoBehaviour
    {
        Color color; float life, t; MeshRenderer mr; MaterialPropertyBlock mpb;

        public void Init(Color c, float l)
        {
            color = c; life = Mathf.Max(0.01f, l);
            mr = GetComponent<MeshRenderer>(); mpb = new MaterialPropertyBlock();
            transform.localScale = Vector3.one * 0.01f;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / life);
            transform.localScale = Vector3.one * Mathf.Max(0.01f, k);
            mpb.SetColor(BaseColor, color * new Color(1, 1, 1, 0.5f + 0.5f * k));
            mr.SetPropertyBlock(mpb);
        }
    }
}
