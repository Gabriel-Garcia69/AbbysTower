using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fuego, fogatas, braseros, carpas, pozo y santuario bien hechos: llamas de partículas (no una esfera que brilla),
/// piedras irregulares, leños en tipi, carpas cerradas con lona por los dos lados, vientos y estacas.
/// Todo usa su propio azar sembrado con la posición: no toca el rng del mapa (si no, se moverían árboles y demás).
/// </summary>
public partial class DioramaBuilder
{
    Material mLlama, mCeniza, mHierro, mCuerda;

    void MaterialesFuego()
    {
        if (mLlama != null) return;
        mLlama = Saved("Dio_Llama", () =>
        {
            var m = new Material(mDust);   // Abby/Particle con el punto suave, aditivo
            m.SetColor("_BaseColor", new Color(2.2f, 1.6f, 1.1f, 1f));
            return m;
        });
        mCeniza = Plain("Dio_Ceniza", new Color(0.09f, 0.08f, 0.075f), 0.05f);
        mHierro = Plain("Dio_Hierro", new Color(0.16f, 0.15f, 0.15f), 0.45f);
        mCuerda = Plain("Dio_Cuerda", new Color(0.62f, 0.52f, 0.36f), 0.05f);
    }

    static System.Random Azar(Vector3 p) => new System.Random(Mathf.RoundToInt(p.x * 73.1f) ^ Mathf.RoundToInt(p.z * 191.7f) ^ Mathf.RoundToInt(p.y * 37f));
    static float Rf(System.Random r, float a, float b) => a + (float)r.NextDouble() * (b - a);

    // ------------------------------------------------------------------ fuego

    /// <summary>Llamas de verdad: núcleo amarillo, lenguas naranjas que se apagan en rojo, chispas que suben y humo.</summary>
    void Llamas(Transform parent, Vector3 pos, float esc, bool humo = true)
    {
        MaterialesFuego();
        var g = Group("Llamas", parent);
        g.localPosition = pos;
        // núcleo: pequeño, muy brillante
        Particulas(g, "Nucleo", mLlama, esc, rate: 38f, life: (0.4f, 0.7f), speed: (0.8f, 1.4f), size: (0.55f, 0.85f), radius: 0.16f,
            col: new[] { new Color(1f, 0.95f, 0.7f), new Color(1f, 0.7f, 0.25f), new Color(0.9f, 0.3f, 0.08f) }, alpha: new[] { 1f, 0.85f, 0f }, shrink: 0.25f, noise: 0.5f, gravity: -0.15f);
        // lenguas: más grandes, naranja a rojo
        Particulas(g, "Lenguas", mLlama, esc, rate: 26f, life: (0.55f, 1f), speed: (1f, 1.8f), size: (0.7f, 1.1f), radius: 0.24f,
            col: new[] { new Color(1f, 0.6f, 0.15f), new Color(0.95f, 0.32f, 0.06f), new Color(0.5f, 0.08f, 0.03f) }, alpha: new[] { 0.8f, 0.6f, 0f }, shrink: 0.15f, noise: 0.9f, gravity: -0.2f);
        // chispas
        Particulas(g, "Chispas", mLlama, esc, rate: 7f, life: (0.8f, 1.8f), speed: (1.2f, 2.4f), size: (0.04f, 0.08f), radius: 0.25f,
            col: new[] { new Color(1f, 0.85f, 0.4f), new Color(1f, 0.5f, 0.15f), new Color(0.8f, 0.2f, 0.05f) }, alpha: new[] { 1f, 1f, 0f }, shrink: 0.6f, noise: 1.6f, gravity: -0.05f, max: 30);
        if (humo)
        {
            var h = Particulas(g, "Humo", mSmoke, esc, rate: 3f, life: (3f, 5f), speed: (0.5f, 0.9f), size: (0.5f, 0.9f), radius: 0.15f,
                col: new[] { new Color(0.35f, 0.33f, 0.32f), new Color(0.45f, 0.44f, 0.44f), new Color(0.55f, 0.55f, 0.56f) }, alpha: new[] { 0f, 0.35f, 0f }, shrink: 2.4f, noise: 0.35f, gravity: -0.02f, max: 24);
            h.transform.localPosition = Vector3.up * 0.9f * esc;
        }
    }

    ParticleSystem Particulas(Transform parent, string name, Material mat, float esc, float rate, (float, float) life, (float, float) speed, (float, float) size,
        float radius, Color[] col, float[] alpha, float shrink, float noise, float gravity, int max = 60)
    {
        var ps = new GameObject(name).AddComponent<ParticleSystem>();
        ps.transform.SetParent(parent, false);
        ps.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life.Item1, life.Item2);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speed.Item1 * esc, speed.Item2 * esc);
        main.startSize = new ParticleSystem.MinMaxCurve(size.Item1 * esc, size.Item2 * esc);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.gravityModifier = gravity;
        main.maxParticles = max;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = rate;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 8f; sh.radius = radius * esc;
        var c = ps.colorOverLifetime; c.enabled = true;
        var gr = new Gradient();
        gr.SetKeys(new[] { new GradientColorKey(col[0], 0f), new GradientColorKey(col[1], 0.45f), new GradientColorKey(col[2], 1f) },
                   new[] { new GradientAlphaKey(alpha[0], 0f), new GradientAlphaKey(alpha[1], 0.4f), new GradientAlphaKey(alpha[2], 1f) });
        c.color = gr;
        var sz = ps.sizeOverLifetime; sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, shrink));
        var n = ps.noise; n.enabled = true; n.strength = noise * esc; n.frequency = 1.4f; n.scrollSpeed = 1.2f; n.quality = ParticleSystemNoiseQuality.Low;
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        ps.Play();
        return ps;
    }

    // ------------------------------------------------------------------ piedras

    static readonly Dictionary<int, Mesh> piedras = new Dictionary<int, Mesh>();

    /// <summary>Piedra de pocas caras, irregular y aplanada (caras planas, como talladas): no una esfera.</summary>
    static Mesh PiedraMesh(int semilla)
    {
        semilla = ((semilla % 6) + 6) % 6;
        if (piedras.TryGetValue(semilla, out var cached) && cached != null) return cached;
        var r = new System.Random(semilla * 977 + 13);
        // octaedro subdividido una vez, con los vértices empujados al azar
        var baseV = new List<Vector3> { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
        int[] baseT = { 0, 4, 3, 0, 3, 5, 0, 5, 2, 0, 2, 4, 1, 3, 4, 1, 5, 3, 1, 2, 5, 1, 4, 2 };
        var mids = new Dictionary<long, int>();
        int Mid(int a, int b)
        {
            long k = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (mids.TryGetValue(k, out int i)) return i;
            baseV.Add((baseV[a] + baseV[b]).normalized); return mids[k] = baseV.Count - 1;
        }
        var tris = new List<int>();
        for (int i = 0; i < baseT.Length; i += 3)
        {
            int a = baseT[i], b = baseT[i + 1], c = baseT[i + 2];
            int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
            tris.AddRange(new[] { a, ab, ca, ab, b, bc, ca, bc, c, ab, bc, ca });
        }
        for (int i = 0; i < baseV.Count; i++)
        {
            var p = baseV[i] * (0.78f + (float)r.NextDouble() * 0.38f);
            if (p.y < 0f) p.y *= 0.35f;   // base plana para que asiente en el suelo
            baseV[i] = p;
        }
        // caras planas: vértices propios por triángulo
        var v = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i < tris.Count; i++) { v.Add(baseV[tris[i]]); t.Add(i); }
        var m = new Mesh { name = "Piedra" + semilla };
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return piedras[semilla] = m;
    }

    GameObject Piedra(Transform parent, Vector3 pos, Vector3 escala, float yaw, int semilla, Material mat)
    {
        var go = MeshObject(parent, "Piedra", PiedraMesh(semilla), mat, false);
        go.transform.localPosition = pos;
        go.transform.localScale = escala;
        go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        return go;
    }

    GameObject Leno(Transform parent, Vector3 pos, float largo, float grosor, Quaternion rot, Material mat)
    {
        var l = Prim(parent, PrimitiveType.Cylinder, "Leño", pos, new Vector3(grosor, largo / 2f, grosor), mat);
        l.transform.localRotation = rot;
        return l;
    }

    // ------------------------------------------------------------------ fogata

    void FogataBonita(Transform parent, Vector3 pos)
    {
        MaterialesFuego();
        var r = Azar(pos);
        var f = Group("Fogata", parent);
        f.localPosition = pos;
        // ceniza y brasas en el suelo
        var ash = Prim(f, PrimitiveType.Cylinder, "Ceniza", new Vector3(0f, 0.015f, 0f), new Vector3(1.15f, 0.015f, 1.15f), mCeniza);
        ash.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        for (int i = 0; i < 6; i++)
        {
            float a = Rf(r, 0f, Mathf.PI * 2f), d = Rf(r, 0f, 0.3f);
            var b = Piedra(f, new Vector3(Mathf.Cos(a) * d, 0.03f, Mathf.Sin(a) * d), Vector3.one * Rf(r, 0.07f, 0.12f), Rf(r, 0f, 360f), i, mGlow);
            b.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        // anillo de piedras irregulares
        int n = 13;
        for (int i = 0; i < n; i++)
        {
            float a = i / (float)n * Mathf.PI * 2f + Rf(r, -0.12f, 0.12f), rad = Rf(r, 0.62f, 0.72f);
            float s = Rf(r, 0.11f, 0.16f);
            Piedra(f, new Vector3(Mathf.Cos(a) * rad, s * 0.2f, Mathf.Sin(a) * rad), new Vector3(s * 1.3f, s * Rf(r, 0.8f, 1.1f), s), Rf(r, 0f, 360f), i + 1, mStone);
        }
        // leños en tipi (se juntan arriba del centro) y dos atravesados abajo
        int lenos = 5;
        float giro = Rf(r, 0f, 360f);
        for (int i = 0; i < lenos; i++)
        {
            float a = giro + i * 360f / lenos;
            var q = Quaternion.Euler(0f, a, 0f) * Quaternion.Euler(32f, 0f, 0f);
            Leno(f, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.3f, -0.2f), 0.75f, 0.1f, q, mTimber);
        }
        foreach (float a in new[] { giro + 20f, giro + 110f })
            Leno(f, new Vector3(0f, 0.07f, 0f), 0.9f, 0.13f, Quaternion.Euler(0f, a, 90f), mTimber);
        Llamas(f, new Vector3(0f, 0.12f, 0f), 1f);
        var l = AddLight(f, new Vector3(0f, 0.9f, 0f), new Color(1f, 0.58f, 0.28f), 9f, 4f);
        l.gameObject.AddComponent<LightFlicker>().amount = 0.3f;
        blocked.Add(new Rect(pos.x - 1f, pos.z - 1f, 2f, 2f));
    }

    // ------------------------------------------------------------------ brasero

    void BraseroBonito(Transform parent, Vector3 pos)
    {
        MaterialesFuego();
        var b = Group("Brasero", parent);
        b.localPosition = pos;
        // trípode de hierro
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f + 30f;
            var leg = Box(b, "Pata", Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.5f, 0.28f), new Vector3(0.07f, 1.05f, 0.07f), mHierro, false);
            leg.transform.localRotation = Quaternion.Euler(0f, a, 0f) * Quaternion.Euler(-14f, 0f, 0f);
            Box(b, "Pie", Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.03f, 0.42f), new Vector3(0.14f, 0.06f, 0.2f), mHierro, false);
        }
        Box(b, "Aro", new Vector3(0f, 0.55f, 0f), new Vector3(0.5f, 0.04f, 0.5f), mHierro, false).transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        // cuenco: aro grueso + fondo
        MeshObject(b, "Cuenco", Ring(0.5f, 0.42f, 0.24f, 16), mHierro, false).transform.localPosition = new Vector3(0f, 0.98f, 0f);
        Prim(b, PrimitiveType.Cylinder, "Fondo", new Vector3(0f, 1f, 0f), new Vector3(0.86f, 0.03f, 0.86f), mHierro);
        // carbones encendidos
        var r = Azar(pos);
        for (int i = 0; i < 7; i++)
        {
            float a = Rf(r, 0f, Mathf.PI * 2f), d = Rf(r, 0f, 0.25f);
            var c = Piedra(b, new Vector3(Mathf.Cos(a) * d, 1.07f, Mathf.Sin(a) * d), Vector3.one * Rf(r, 0.08f, 0.13f), Rf(r, 0f, 360f), i, i % 3 == 0 ? mCeniza : mGlow);
            c.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        Llamas(b, new Vector3(0f, 1.1f, 0f), 0.75f, humo: false);
        var l = AddLight(b, new Vector3(0f, 1.8f, 0f), new Color(1f, 0.6f, 0.3f), 7f, 3f);
        l.gameObject.AddComponent<LightFlicker>().amount = 0.25f;
        var col = b.gameObject.AddComponent<CapsuleCollider>(); col.radius = 0.45f; col.height = 1.2f; col.center = new Vector3(0f, 0.6f, 0f);
    }

    // ------------------------------------------------------------------ carpa

    /// <summary>Malla de lona de dos caras (se ve de los dos lados), un panel de 'ancho' × 'largo'.</summary>
    static Mesh LonaMesh(float ancho, float largo)
    {
        float hx = ancho / 2f, hz = largo / 2f;
        var v = new List<Vector3> { new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz), new Vector3(-hx, 0, hz), new Vector3(hx, 0, hz) };
        v.AddRange(v.ToArray());
        var uv = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        uv.AddRange(uv.ToArray());
        var t = new List<int> { 0, 2, 3, 0, 3, 1, 4, 7, 6, 4, 5, 7 };
        var m = new Mesh { name = "Lona" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        var n = new List<Vector3>(); for (int i = 0; i < 4; i++) n.Add(Vector3.up); for (int i = 0; i < 4; i++) n.Add(Vector3.down);
        m.SetNormals(n); m.RecalculateBounds();
        return m;
    }

    /// <summary>Triángulo de dos caras en el plano XY (fondo de la carpa y solapas), base en y = 0.</summary>
    static Mesh TrianguloMesh(float ancho, float alto)
    {
        var v = new List<Vector3> { new Vector3(-ancho / 2f, 0, 0), new Vector3(ancho / 2f, 0, 0), new Vector3(0, alto, 0) };
        v.AddRange(v.ToArray());
        var t = new List<int> { 0, 2, 1, 3, 4, 5 };
        var m = new Mesh { name = "Triangulo" };
        m.SetVertices(v); m.SetTriangles(t, 0);
        m.SetNormals(new List<Vector3> { Vector3.back, Vector3.back, Vector3.back, Vector3.forward, Vector3.forward, Vector3.forward });
        m.RecalculateBounds();
        return m;
    }

    /// <summary>Paño triangular del frente (dos caras): esquina de afuera en el suelo, borde recogido y la punta arriba al centro.</summary>
    static Mesh PanoFrente(float xAfuera, float xRecogido, float alto)
    {
        var v = new List<Vector3> { new Vector3(xAfuera, 0f, 0f), new Vector3(xRecogido, 0f, 0f), new Vector3(0f, alto, 0f) };
        v.AddRange(v.ToArray());
        var t = new List<int> { 0, 2, 1, 3, 4, 5 };
        var m = new Mesh { name = "PanoFrente" };
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals();
        var n = new Vector3[6]; for (int i = 0; i < 3; i++) { n[i] = Vector3.forward; n[i + 3] = Vector3.back; }
        m.normals = n; m.RecalculateBounds();
        return m;
    }

    void CarpaBonita(Transform parent, Vector3 pos, float yaw, Material cloth)
    {
        MaterialesFuego();
        var t = Group("Carpa", parent);
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, yaw, 0f);
        float largo = 3.2f, base_ = 2.8f, alto = 1.9f;
        float lado = Mathf.Sqrt(base_ * base_ / 4f + alto * alto), ang = Mathf.Atan2(alto, base_ / 2f) * Mathf.Rad2Deg;
        // dos paneles de lona que se juntan en la cumbrera (el frente queda en +Z)
        foreach (float sx in new[] { -1f, 1f })
        {
            var p = MeshObject(t, "Lona", LonaMesh(lado, largo), cloth, false);
            p.transform.localPosition = new Vector3(sx * base_ / 4f, alto / 2f, 0f);
            p.transform.localRotation = Quaternion.Euler(0f, 0f, sx * -ang);
        }
        // fondo cerrado
        var fondo = MeshObject(t, "Fondo", TrianguloMesh(base_, alto), cloth, false);
        fondo.transform.localPosition = new Vector3(0f, 0f, -largo / 2f);
        // frente: dos paños en el mismo plano que el frente, recogidos hacia los lados y amarrados (la entrada queda
        // como un triángulo abierto al centro, derecha)
        foreach (float sx in new[] { -1f, 1f })
        {
            var sol = MeshObject(t, "Solapa", PanoFrente(sx * base_ / 2f, sx * base_ * 0.2f, alto), cloth, false);
            sol.transform.localPosition = new Vector3(0f, 0f, largo / 2f + 0.01f);
            Box(t, "Amarre", new Vector3(sx * base_ * 0.3f, alto * 0.38f, largo / 2f + 0.04f), new Vector3(0.05f, 0.05f, 0.05f), mCuerda, false);
        }
        // interior oscuro (para que se lea la entrada) y petate
        var piso = Box(t, "Piso", new Vector3(0f, 0.02f, 0f), new Vector3(base_ * 0.9f, 0.03f, largo - 0.1f), mCeniza, false);
        piso.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var petate = Prim(t, PrimitiveType.Cylinder, "Petate", new Vector3(0.4f, 0.12f, -0.3f), new Vector3(0.24f, 0.7f, 0.24f), mClothB);
        petate.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        // palos (frente y fondo) y cumbrera
        foreach (float sz in new[] { -1f, 1f })
            Box(t, "Palo", new Vector3(0f, alto / 2f + 0.05f, sz * (largo / 2f + 0.05f)), new Vector3(0.08f, alto + 0.1f, 0.08f), mTimber, false);
        Box(t, "Cumbrera", new Vector3(0f, alto + 0.03f, 0f), new Vector3(0.07f, 0.07f, largo + 0.3f), mTimber, false);
        // vientos: cuerda de cada palo a una estaca en el suelo
        foreach (float sz in new[] { -1f, 1f })
        {
            Vector3 top = new Vector3(0f, alto + 0.05f, sz * (largo / 2f + 0.05f)), est = new Vector3(0f, 0f, sz * (largo / 2f + 1.2f));
            Cuerda(t, top, est);
            Box(t, "Estaca", est + Vector3.up * 0.1f, new Vector3(0.06f, 0.22f, 0.06f), mTimber, false);
        }
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -0.4f, 0.4f })
                Box(t, "Estaca", new Vector3(sx * (base_ / 2f + 0.12f), 0.08f, sz * largo), new Vector3(0.05f, 0.18f, 0.05f), mTimber, false);
        var col = t.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0f, 0.8f, 0f); col.size = new Vector3(2.8f, 1.6f, 3.2f);
        blocked.Add(new Rect(pos.x - 2.5f, pos.z - 2.5f, 5f, 5f));
    }

    void Cuerda(Transform parent, Vector3 a, Vector3 b)
    {
        var c = Box(parent, "Cuerda", (a + b) / 2f, new Vector3(0.025f, 0.025f, Vector3.Distance(a, b)), mCuerda, false);
        c.transform.localRotation = Quaternion.LookRotation(b - a);
        c.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // ------------------------------------------------------------------ santuario

    /// <summary>Octaedro estirado (un cristal), de radio 1 y alto 2.</summary>
    static Mesh CristalMesh()
    {
        var v = new List<Vector3>(); var t = new List<int>();
        Vector3 top = Vector3.up, bot = Vector3.down * 0.8f;
        int n = 6;
        for (int i = 0; i < n; i++)
        {
            float a0 = i / (float)n * Mathf.PI * 2f, a1 = (i + 1) / (float)n * Mathf.PI * 2f;
            Vector3 p0 = new Vector3(Mathf.Cos(a0) * 0.45f, 0.1f, Mathf.Sin(a0) * 0.45f), p1 = new Vector3(Mathf.Cos(a1) * 0.45f, 0.1f, Mathf.Sin(a1) * 0.45f);
            int b = v.Count; v.AddRange(new[] { top, p1, p0 }); t.AddRange(new[] { b, b + 1, b + 2 });
            b = v.Count; v.AddRange(new[] { bot, p0, p1 }); t.AddRange(new[] { b, b + 1, b + 2 });
        }
        var m = new Mesh { name = "Cristal" };
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    void SantuarioBonito(Transform parent, Vector3 pos)
    {
        MaterialesFuego();
        var s = Group("Santuario", parent);
        s.localPosition = pos;
        // base escalonada octagonal
        Prim(s, PrimitiveType.Cylinder, "Escalon", new Vector3(0f, 0.12f, 0f), new Vector3(2.6f, 0.12f, 2.6f), mStone, true);
        Prim(s, PrimitiveType.Cylinder, "Escalon2", new Vector3(0f, 0.32f, 0f), new Vector3(1.9f, 0.1f, 1.9f), mStone);
        Prim(s, PrimitiveType.Cylinder, "Altar", new Vector3(0f, 0.62f, 0f), new Vector3(0.75f, 0.22f, 0.75f), mStone);
        Prim(s, PrimitiveType.Cylinder, "Mesa", new Vector3(0f, 0.86f, 0f), new Vector3(0.95f, 0.04f, 0.95f), mStone);
        var runa = Glow("Dio_RunaVerde", new Color(0.35f, 1f, 0.55f), 1.4f);
        var crystalMat = Glow("Dio_CristalSanador", new Color(0.3f, 1f, 0.55f), 1.5f);
        // cuatro pilarcitos con una runa que brilla
        for (int i = 0; i < 4; i++)
        {
            float a = i * 90f + 45f;
            Vector3 p = Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0f, 1.05f);
            Box(s, "Pilar", p + Vector3.up * 0.75f, new Vector3(0.22f, 0.9f, 0.22f), mStone, false).transform.localRotation = Quaternion.Euler(0f, a, 0f);
            Box(s, "Capitel", p + Vector3.up * 1.24f, new Vector3(0.3f, 0.08f, 0.3f), mStone, false).transform.localRotation = Quaternion.Euler(0f, a, 0f);
            var rn = Box(s, "Runa", p + Vector3.up * 0.8f + (Quaternion.Euler(0f, a, 0f) * Vector3.forward) * 0.115f, new Vector3(0.1f, 0.3f, 0.02f), runa, false);
            rn.transform.localRotation = Quaternion.Euler(0f, a, 0f);
            rn.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        // cristal que flota y gira (HealingShrine lo anima), con dos esquirlas al lado
        var crystal = MeshObject(s, "Cristal", CristalMesh(), crystalMat, false);
        crystal.transform.localPosition = new Vector3(0f, 1.75f, 0f);
        crystal.transform.localScale = new Vector3(0.6f, 0.65f, 0.6f);
        crystal.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        foreach (float sx in new[] { -1f, 1f })
        {
            var e = MeshObject(crystal.transform, "Esquirla", CristalMesh(), crystalMat, false);
            e.transform.localPosition = new Vector3(sx * 1.3f, -0.4f, 0f);
            e.transform.localScale = Vector3.one * 0.35f;
            e.transform.localRotation = Quaternion.Euler(0f, 0f, sx * 18f);
            e.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        // brillo suave que sube
        var g = Particulas(s, "Destellos", mDust, 1f, rate: 5f, life: (1.5f, 2.5f), speed: (0.3f, 0.6f), size: (0.06f, 0.12f), radius: 0.9f,
            col: new[] { new Color(0.5f, 1f, 0.7f), new Color(0.4f, 1f, 0.6f), new Color(0.3f, 0.9f, 0.5f) }, alpha: new[] { 0f, 1f, 0f }, shrink: 0.5f, noise: 0.3f, gravity: -0.03f, max: 20);
        g.transform.localPosition = Vector3.up * 0.9f;
        var lt = AddLight(s, new Vector3(0f, 1.8f, 0f), new Color(0.45f, 1f, 0.65f), 7f, 4f);
        var shrine = s.gameObject.AddComponent<HealingShrine>();
        shrine.crystal = crystal.transform; shrine.glow = lt;
        blocked.Add(new Rect(pos.x - 1.8f, pos.z - 1.8f, 3.6f, 3.6f));
    }
}
