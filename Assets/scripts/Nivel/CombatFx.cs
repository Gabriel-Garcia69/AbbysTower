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

    /// <summary>Columna de luz morada que se ve de lejos: chispas que suben, un haz suave y una luz. La mueve GuiaHordas.</summary>
    public static GameObject Baliza()
    {
        var go = new GameObject("FX_Baliza");
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true; main.playOnAwake = false; main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 3.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.65f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.8f, 0.45f, 1f, 1f), new Color(0.55f, 0.75f, 1f, 1f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        var em = ps.emission; em.rateOverTime = 50f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 0.9f; sh.rotation = new Vector3(-90f, 0f, 0f);
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = Additive;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
        // anillo en el suelo
        if (ringMesh == null) ringMesh = BuildRing(0.86f, 48);
        var anillo = new GameObject("Anillo");
        anillo.transform.SetParent(go.transform, false);
        anillo.transform.localPosition = Vector3.up * 0.06f;
        anillo.transform.localScale = Vector3.one * 1.6f;
        anillo.AddComponent<MeshFilter>().sharedMesh = ringMesh;
        var ar = anillo.AddComponent<MeshRenderer>();
        ar.sharedMaterial = AdditiveFlat;
        ar.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var mpb = new MaterialPropertyBlock(); mpb.SetColor(BaseColor, new Color(0.75f, 0.4f, 1f, 0.8f)); ar.SetPropertyBlock(mpb);
        // haz vertical suave (se ve desde lejos aunque las chispas sean chicas)
        var haz = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(haz.GetComponent<Collider>());
        haz.name = "Haz";
        haz.transform.SetParent(go.transform, false);
        haz.transform.localPosition = Vector3.up * 7f;
        haz.transform.localScale = new Vector3(1.1f, 7f, 1.1f);
        var hr = haz.GetComponent<MeshRenderer>();
        hr.sharedMaterial = AdditiveFlat;
        hr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var hp = new MaterialPropertyBlock(); hp.SetColor(BaseColor, new Color(0.65f, 0.35f, 1f, 0.22f)); hr.SetPropertyBlock(hp);
        haz.AddComponent<FxPulso>();
        var l = new GameObject("Luz").AddComponent<Light>();
        l.transform.SetParent(go.transform, false);
        l.transform.localPosition = Vector3.up * 2f;
        l.type = LightType.Point; l.color = new Color(0.7f, 0.4f, 1f); l.range = 10f; l.intensity = 3f; l.shadows = LightShadows.None;
        return go;
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
        borde.AddComponent<MeshFilter>().sharedMesh = ringMesh;
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

    /// <summary>El haz de la baliza late despacio.</summary>
    class FxPulso : MonoBehaviour
    {
        MeshRenderer mr; MaterialPropertyBlock mpb;
        void Update()
        {
            if (mr == null) { mr = GetComponent<MeshRenderer>(); mpb = new MaterialPropertyBlock(); }
            mpb.SetColor(BaseColor, new Color(0.65f, 0.35f, 1f, 0.16f + 0.1f * Mathf.Sin(Time.time * 2.5f)));
            mr.SetPropertyBlock(mpb);
        }
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
            borde.transform.localScale = Vector3.one * radius * k * 1.04f;
            borde.transform.localRotation = Quaternion.Euler(0f, t * 40f, 0f);
            mpb.SetColor(BaseColor, new Color(0.75f, 0.35f, 1f, k * pulso));
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
