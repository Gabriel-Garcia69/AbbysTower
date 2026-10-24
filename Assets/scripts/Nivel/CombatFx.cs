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

    // ------------------------------------------------------------------ baliza (marca la siguiente horda)

    static readonly Color ColorBaliza = new Color(0.68f, 0.58f, 1f);
    static Mesh hazMesh, aroFinoMesh, runasMesh;

    /// <summary>
    /// Marca de la siguiente pelea: un haz de luz que se desvanece hacia arriba y hacia los lados (tres planos cruzados,
    /// se ve "volumétrico" desde cualquier ángulo), un resplandor suave en el suelo, dos aros finos que giran en sentidos
    /// contrarios (el de adentro en segmentos, como un círculo de runas), motas de luz que suben despacio y una luz tenue.
    /// </summary>
    public static GameObject Baliza()
    {
        var go = new GameObject("FX_Baliza");
        if (hazMesh == null) hazMesh = BuildHaz(0.7f, 11f);
        if (aroFinoMesh == null) aroFinoMesh = BuildRing(0.955f, 64);
        if (runasMesh == null) runasMesh = BuildRunas(16, 0.72f, 0.8f, 0.55f);

        var haz = Pieza(go.transform, "Haz", hazMesh, AdditiveFlat, Vector3.zero, Vector3.one);
        var disco = Pieza(go.transform, "Resplandor", QuadSuelo(), Additive, Vector3.up * 0.05f, Vector3.one * 2.3f);
        var aro = Pieza(go.transform, "Aro", aroFinoMesh, AdditiveFlat, Vector3.up * 0.07f, Vector3.one * 2.1f);
        var runas = Pieza(go.transform, "Runas", runasMesh, AdditiveFlat, Vector3.up * 0.08f, Vector3.one * 2.1f);

        // motas de luz que suben despacio
        var motas = new GameObject("Motas");
        motas.transform.SetParent(go.transform, false);
        var ps = motas.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true; main.playOnAwake = false; main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.16f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.8f, 1f, 1f), new Color(0.6f, 0.75f, 1f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 50;
        var em = ps.emission; em.rateOverTime = 12f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.2f; sh.rotation = new Vector3(-90f, 0f, 0f);
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var nz = ps.noise; nz.enabled = true; nz.strength = 0.25f; nz.frequency = 0.8f; nz.quality = ParticleSystemNoiseQuality.Low;
        var r = motas.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = Additive;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();

        var l = new GameObject("Luz").AddComponent<Light>();
        l.transform.SetParent(go.transform, false);
        l.transform.localPosition = Vector3.up * 1.5f;
        l.type = LightType.Point; l.color = ColorBaliza; l.range = 7f; l.intensity = 1.6f; l.shadows = LightShadows.None;

        go.AddComponent<FxBaliza>().Init(haz.GetComponent<MeshRenderer>(), disco.GetComponent<MeshRenderer>(),
            aro.GetComponent<MeshRenderer>(), runas.GetComponent<MeshRenderer>(), l);
        return go;
    }

    static GameObject Pieza(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 escala)
    {
        var p = new GameObject(name);
        p.transform.SetParent(parent, false);
        p.transform.localPosition = pos;
        p.transform.localScale = escala;
        p.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = p.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        return p;
    }

    static Mesh quadSuelo;
    /// <summary>Cuadro acostado de 2 x 2 con UV completas (para usar el punto suave como resplandor en el suelo).</summary>
    static Mesh QuadSuelo()
    {
        if (quadSuelo != null) return quadSuelo;
        quadSuelo = new Mesh { name = "QuadSuelo" };
        quadSuelo.SetVertices(new List<Vector3> { new Vector3(-1, 0, -1), new Vector3(1, 0, -1), new Vector3(-1, 0, 1), new Vector3(1, 0, 1) });
        quadSuelo.SetUVs(0, new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) });
        quadSuelo.SetColors(new List<Color> { Color.white, Color.white, Color.white, Color.white });
        quadSuelo.SetTriangles(new[] { 0, 2, 3, 0, 3, 1 }, 0);
        quadSuelo.RecalculateBounds();
        return quadSuelo;
    }

    /// <summary>Haz de luz: tres planos verticales cruzados; alfa máximo al centro y abajo, cero en los bordes y arriba.</summary>
    static Mesh BuildHaz(float medioAncho, float alto)
    {
        var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
        float[] ys = { 0f, 0.15f, 0.45f, 1f };
        float[] alfas = { 0.55f, 1f, 0.45f, 0f };
        for (int plano = 0; plano < 3; plano++)
        {
            var rot = Quaternion.Euler(0f, plano * 60f, 0f);
            int b = v.Count;
            for (int fila = 0; fila < ys.Length; fila++)
                for (int k = 0; k < 3; k++)
                {
                    float x = (k - 1) * medioAncho * (1f - ys[fila] * 0.35f);   // un poco más angosto arriba
                    v.Add(rot * new Vector3(x, ys[fila] * alto, 0f));
                    c.Add(new Color(1f, 1f, 1f, k == 1 ? alfas[fila] : 0f));
                }
            for (int fila = 0; fila + 1 < ys.Length; fila++)
                for (int k = 0; k < 2; k++)
                {
                    int a0 = b + fila * 3 + k, a1 = a0 + 1, b0 = a0 + 3, b1 = b0 + 1;
                    t.AddRange(new[] { a0, b0, b1, a0, b1, a1 });
                }
        }
        var m = new Mesh { name = "Haz" };
        m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0); m.RecalculateBounds();
        return m;
    }

    /// <summary>Aro en segmentos (radio interno ri, externo re; lleno = fracción de cada segmento que se dibuja).</summary>
    static Mesh BuildRunas(int segmentos, float ri, float re, float lleno)
    {
        var v = new List<Vector3>(); var c = new List<Color>(); var t = new List<int>();
        for (int s = 0; s < segmentos; s++)
        {
            float a0 = s / (float)segmentos * Mathf.PI * 2f, a1 = a0 + lleno / segmentos * Mathf.PI * 2f;
            int b = v.Count;
            const int pasos = 4;
            for (int k = 0; k <= pasos; k++)
            {
                float a = Mathf.Lerp(a0, a1, k / (float)pasos);
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                float alfa = Mathf.Sin(k / (float)pasos * Mathf.PI) * 0.6f + 0.4f;
                v.Add(d * ri); c.Add(new Color(1, 1, 1, alfa * 0.6f));
                v.Add(d * re); c.Add(new Color(1, 1, 1, alfa));
                if (k < pasos) { int q = b + k * 2; t.AddRange(new[] { q, q + 3, q + 1, q, q + 2, q + 3 }); }
            }
        }
        var m = new Mesh { name = "Runas" };
        m.SetVertices(v); m.SetColors(c); m.SetTriangles(t, 0); m.RecalculateBounds();
        return m;
    }

    /// <summary>Anima la baliza: entra suave, los aros giran en sentidos contrarios y el haz y el resplandor laten despacio.</summary>
    class FxBaliza : MonoBehaviour
    {
        MeshRenderer haz, disco, aro, runas; Light luz; MaterialPropertyBlock mpb;
        float aparecer;

        public void Init(MeshRenderer h, MeshRenderer d, MeshRenderer a, MeshRenderer r, Light l)
        {
            haz = h; disco = d; aro = a; runas = r; luz = l; mpb = new MaterialPropertyBlock();
        }

        void OnEnable() { aparecer = 0f; }

        void Tono(MeshRenderer mr, float a)
        {
            mpb.SetColor(BaseColor, new Color(ColorBaliza.r, ColorBaliza.g, ColorBaliza.b, a));
            mr.SetPropertyBlock(mpb);
        }

        void Update()
        {
            if (mpb == null) return;
            aparecer = Mathf.Min(1f, aparecer + Time.deltaTime * 1.5f);   // entra suave al cambiar de horda
            float k = aparecer * aparecer * (3f - 2f * aparecer);
            float p = 0.85f + 0.15f * Mathf.Sin(Time.time * 1.8f);
            aro.transform.localRotation = Quaternion.Euler(0f, Time.time * 14f, 0f);
            runas.transform.localRotation = Quaternion.Euler(0f, -Time.time * 22f, 0f);
            Tono(haz, 0.32f * p * k);
            Tono(disco, 0.45f * p * k);
            Tono(aro, 0.8f * k);
            Tono(runas, 0.75f * k);
            luz.intensity = 1.6f * p * k;
            transform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, k);
        }
    }

    // ------------------------------------------------------------------ grieta del abismo (aparición de enemigos)

    static Material darkPool;
    /// <summary>Mancha oscura con mezcla normal (no aditiva): el agujero negro de la grieta.</summary>
    static Material DarkPool
    {
        get
        {
            if (darkPool != null) return darkPool;
            darkPool = new Material(AdditiveFlat) { name = "FX_Grieta (runtime)" };
            darkPool.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            darkPool.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent - 1;
            return darkPool;
        }
    }

    /// <summary>
    /// Grieta del abismo en el suelo: se abre un pozo negro con borde morado que palpita, sube humo de sombra y una luz
    /// morada parpadea; a los 'abre' segundos sale el enemigo (estallido) y la grieta se cierra sola al llegar a 'total'.
    /// </summary>
    public static void Grieta(Vector3 pos, float radius, float abre, float total)
    {
        if (ringMesh == null) ringMesh = BuildRing(0.86f, 48);
        var go = new GameObject("FX_Grieta");
        go.transform.position = pos + Vector3.up * 0.03f;

        // pozo negro (un disco suave)
        var pozo = new GameObject("Pozo");
        pozo.transform.SetParent(go.transform, false);
        pozo.AddComponent<MeshFilter>().sharedMesh = PozoMesh();
        var pr = pozo.AddComponent<MeshRenderer>();
        pr.sharedMaterial = DarkPool;
        pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // borde que brilla
        var borde = new GameObject("Borde");
        borde.transform.SetParent(go.transform, false);
        borde.transform.localPosition = Vector3.up * 0.01f;
        if (runasMesh == null) runasMesh = BuildRunas(16, 0.72f, 0.8f, 0.55f);
        borde.AddComponent<MeshFilter>().sharedMesh = runasMesh;   // círculo de runas, como la baliza
        var br = borde.AddComponent<MeshRenderer>();
        br.sharedMaterial = AdditiveFlat;
        br.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // humo de sombra que sube del pozo
        var humo = new GameObject("Humo");
        humo.transform.SetParent(go.transform, false);
        var ps = humo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true; main.playOnAwake = false; main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f * radius, 0.55f * radius);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.45f, 0.15f, 0.75f, 0.8f), new Color(0.12f, 0.04f, 0.2f, 0.9f));
        main.gravityModifier = -0.35f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;
        var em = ps.emission; em.rateOverTime = 34f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = radius * 0.7f;
        sh.rotation = new Vector3(-90f, 0f, 0f);
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 1.4f));
        var rr = humo.GetComponent<ParticleSystemRenderer>();
        rr.sharedMaterial = Additive;
        rr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();

        // luz morada que tiembla
        var l = new GameObject("Luz").AddComponent<Light>();
        l.transform.SetParent(go.transform, false);
        l.transform.localPosition = Vector3.up * 0.6f;
        l.type = LightType.Point; l.color = new Color(0.7f, 0.3f, 1f); l.range = radius * 6f; l.intensity = 0f; l.shadows = LightShadows.None;

        go.AddComponent<FxGrieta>().Init(radius, abre, total, pr, br, ps, l);
    }

    static Mesh pozoMesh;
    /// <summary>Disco negro por dentro (alfa 1 hasta el 65 % del radio) que se difumina hacia la orilla, con borde irregular.</summary>
    static Mesh PozoMesh()
    {
        if (pozoMesh != null) return pozoMesh;
        const int segs = 40;
        var v = new List<Vector3> { Vector3.zero }; var c = new List<Color> { Color.white }; var t = new List<int>();
        for (int i = 0; i < segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f;
            float irregular = 1f + 0.12f * Mathf.Sin(a * 5f) + 0.07f * Mathf.Sin(a * 11f + 1.3f);   // grieta, no círculo perfecto
            var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * irregular;
            v.Add(d * 0.65f); c.Add(Color.white);
            v.Add(d); c.Add(new Color(1f, 1f, 1f, 0f));
        }
        for (int i = 0; i < segs; i++)
        {
            int a0 = 1 + i * 2, a1 = 1 + ((i + 1) % segs) * 2;
            t.AddRange(new[] { 0, a1, a0 });                       // centro
            t.AddRange(new[] { a0, a1, a1 + 1, a0, a1 + 1, a0 + 1 });   // orilla difuminada
        }
        pozoMesh = new Mesh { name = "FxPozo" };
        pozoMesh.SetVertices(v); pozoMesh.SetColors(c); pozoMesh.SetTriangles(t, 0); pozoMesh.RecalculateBounds();
        return pozoMesh;
    }

    class FxGrieta : MonoBehaviour
    {
        float radius, abre, total, t; bool salio;
        MeshRenderer pozo, borde; ParticleSystem humo; Light luz; MaterialPropertyBlock mpb;

        public void Init(float r, float a, float tot, MeshRenderer p, MeshRenderer b, ParticleSystem h, Light l)
        {
            radius = r; abre = a; total = Mathf.Max(a + 0.3f, tot); pozo = p; borde = b; humo = h; luz = l;
            mpb = new MaterialPropertyBlock();
            Update();
        }

        void Update()
        {
            t += Time.deltaTime;
            // se abre rápido (con un poco de rebote), se mantiene y al final se cierra
            float abrir = Mathf.Clamp01(t / (abre * 0.6f));
            abrir = 1f - (1f - abrir) * (1f - abrir) * (1f - abrir);
            float cerrar = Mathf.Clamp01((total - t) / 0.5f);
            float k = abrir * cerrar;
            float pulso = 0.75f + 0.25f * Mathf.Sin(t * 14f) + 0.1f * Mathf.Sin(t * 37f);

            pozo.transform.localScale = Vector3.one * radius * k * (1f + 0.04f * Mathf.Sin(t * 9f));
            mpb.SetColor(BaseColor, new Color(0.01f, 0f, 0.025f, 0.97f * k));
            pozo.SetPropertyBlock(mpb);
            borde.transform.localScale = Vector3.one * radius * k * 1.25f;
            borde.transform.localRotation = Quaternion.Euler(0f, t * 40f, 0f);
            mpb.SetColor(BaseColor, new Color(0.7f, 0.55f, 1f, k * pulso));
            borde.SetPropertyBlock(mpb);
            luz.intensity = 5f * k * pulso;

            if (!salio && t >= abre)
            {
                // sale el enemigo: chispazo de sombra hacia arriba
                salio = true;
                Burst(transform.position + Vector3.up * 0.3f, new Color(0.7f, 0.3f, 1f), 26, 4.5f, 0.22f, 0.6f, -0.4f);
                Burst(transform.position + Vector3.up * 0.2f, new Color(0.15f, 0.05f, 0.25f), 18, 2.5f, 0.45f, 0.8f, -0.6f);
                luz.intensity *= 2.2f;
            }
            if (t >= total - 0.5f && humo.isEmitting) humo.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (t >= total + 1.4f) Destroy(gameObject);   // deja que el humo termine de subir
        }
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
