using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Texturas y detalle de la gruta del tutorial: materiales con las texturas pixel art de Assets/Textures/Expedicion (ver
/// TexturasExpedicion), mallas con UV en metros (32 px por metro), cristales facetados y decales de suelo (grietas y charcos).
/// </summary>
public partial class DioramaBuilder
{
    const string TexExp = "Assets/Textures/Expedicion/";
    Material mExpSuelo, mExpGrava, mExpPared, mExpParedAlt, mExpCristal, mExpGrieta, mExpCharco, mExpReflejo;

    static Color Desat(Color c, float k)
    {
        float g = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
        return new Color(Mathf.Lerp(g, c.r, k), Mathf.Lerp(g, c.g, k), Mathf.Lerp(g, c.b, k), c.a);
    }

    /// <summary>Copia de un material con el color base desaturado (los de Materials/Exterior no se tocan: se copian).</summary>
    Material DesatMat(Material src, string name, float k) => Saved(name, () =>
    {
        var m = new Material(src);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Desat(src.GetColor("_BaseColor"), k));
        return m;
    });

    /// <summary>Si el material se creó antes de que existiera la textura, se la pone ahora.</summary>
    static Material ConTextura(Material m, string ruta)
    {
#if UNITY_EDITOR
        if (m != null && m.GetTexture("_BaseMap") == null)
        {
            var t = AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
            if (t != null) { m.SetTexture("_BaseMap", t); EditorUtility.SetDirty(m); }
        }
#endif
        return m;
    }

    Material MatTex(string nombre, string textura, Color tinte, float suave) => ConTextura(TexMat(nombre, TexExp + textura + ".png", null, tinte, suave), TexExp + textura + ".png");

    /// <summary>Materiales de la gruta del tutorial. Se llama antes de construir nada: todas las rocas (mCaveRock) pasan a pared_roca.</summary>
    void MaterialesExpedicion()
    {
        avisoSprites = false;
#if UNITY_EDITOR
        TexturasExpedicion.Asegurar();
#endif
        // el tono frío viene de la luz, no del albedo: las texturas van sin tinte azul
        mExpSuelo = MatTex("Dio_ExpSuelo", "suelo_gruta", Color.white, 0.12f);
        mExpGrava = MatTex("Dio_ExpGrava", "suelo_grava", Color.white, 0.1f);
        mExpPared = MatTex("Dio_ExpPared", "pared_roca", Color.white, 0.2f);
        mExpParedAlt = MatTex("Dio_ExpParedAlt", "pared_alternativa", Color.white, 0.2f);
        mCaveRock = mExpPared;
        mSenderoExp = MatTex("Dio_ExpSendero", "sendero_losas", new Color(1.1f, 1f, 0.9f), 0.12f);
        mCloth = MatTex("Dio_ExpLonaRoja", "lona_tienda", new Color(0.7f, 0.32f, 0.3f), 0.05f);     // la roja sigue roja, pero apagada
        mClothB = MatTex("Dio_ExpLonaCrema", "lona_tienda", new Color(0.85f, 0.8f, 0.7f), 0.05f);

        // postes, letreros y cajas: sin textura, solo desaturados
        mTimber = Plain("Dio_ExpViga", Desat(new Color(0.26f, 0.17f, 0.11f), 0.75f), 0.1f);
        mPlank = DesatMat(mPlank, "Dio_ExpTablon", 0.75f);
        mMetal = DesatMat(mMetal, "Dio_ExpMetal", 0.75f);
        mDoor = DesatMat(mDoor, "Dio_ExpPuerta", 0.75f);

        mExpCristal = Saved("Dio_ExpCristal", () => new Material(Shader.Find("Abby/CristalFacetado")));
        mExpGrieta = MatDecal("Dio_ExpGrieta", "decal_grietas", false, Color.white);
        mExpCharco = MatDecal("Dio_ExpCharco", "decal_charco", false, Color.white);
        mExpReflejo = MatDecal("Dio_ExpReflejo", "decal_reflejo", true, new Color(1.1f, 0.55f, 0.22f) * 0.55f);   // el fuego en el agua
    }

    Material MatDecal(string nombre, string textura, bool aditivo, Color color)
    {
        var m = Saved(nombre, () => new Material(Shader.Find("Abby/CieloSprite")));
        color.a = 1f;
        m.SetColor("_BaseColor", color);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", aditivo ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
#if UNITY_EDITOR
        var t = AssetDatabase.LoadAssetAtPath<Texture2D>(TexExp + textura + ".png");
        if (t != null) m.SetTexture("_BaseMap", t);
        EditorUtility.SetDirty(m);
#endif
        return m;
    }

    // ---------------- oclusión suave: utilería que se atenúa cuando queda entre la cámara y Dante

    /// <summary>
    /// Marca un renderer de utilería (huesos, pilares, estalagmitas) para que OclusionSuave lo atenúe a ~25 % de alfa cuando está
    /// entre la cámara y Dante: guarda su material y un gemelo transparente (un asset por material, nunca por frame).
    /// El suelo, las paredes y el techo no se marcan.
    /// </summary>
    void MarcarOcluible(Renderer r)
    {
        if (r == null || r.sharedMaterial == null) return;
        var u = r.gameObject.AddComponent<UtileriaOcluible>();
        u.original = r.sharedMaterial;
        u.transparente = GemeloTransparente(u.original);
    }

    Material GemeloTransparente(Material src) => Saved(src.name + "_Fade", () =>
    {
        var m = new Material(src);
        m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetOverrideTag("RenderType", "Transparent");
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return m;
    });

    // ---------------- mallas con UV en metros (tile = repeticiones por metro: 0,25 = 4 m)

    /// <summary>Cono de caras planas (como Cone) con la base en y = 0; el UV mide metros reales: no se estira al cambiar de tamaño.</summary>
    static Mesh ConoMetrico(float radio, float alto, int segs, float tile)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        float lado = Mathf.Sqrt(radio * radio + alto * alto), arco = Mathf.PI * 2f * radio / segs;
        for (int i = 0; i < segs; i++)
        {
            float a0 = i / (float)segs * Mathf.PI * 2f, a1 = (i + 1) / (float)segs * Mathf.PI * 2f;
            int b = v.Count;
            v.Add(new Vector3(Mathf.Cos(a0) * radio, 0f, Mathf.Sin(a0) * radio)); v.Add(new Vector3(0f, alto, 0f)); v.Add(new Vector3(Mathf.Cos(a1) * radio, 0f, Mathf.Sin(a1) * radio));
            uv.Add(new Vector2(i * arco, 0f) * tile); uv.Add(new Vector2((i + 0.5f) * arco, lado) * tile); uv.Add(new Vector2((i + 1f) * arco, 0f) * tile);
            t.AddRange(new[] { b, b + 1, b + 2 });
        }
        var m = new Mesh { name = "ConoMetrico" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>Cilindro abierto (sin tapas) con la base en y = 0 y UV en metros.</summary>
    static Mesh CilindroMetrico(float radio, float alto, int segs, float tile)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        float circ = Mathf.PI * 2f * radio;
        for (int i = 0; i <= segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f, u = i / (float)segs * circ * tile;
            var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            v.Add(d * radio); uv.Add(new Vector2(u, 0f));
            v.Add(d * radio + Vector3.up * alto); uv.Add(new Vector2(u, alto * tile));
        }
        for (int i = 0; i < segs; i++) { int b = i * 2; t.AddRange(new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2 }); }
        var m = new Mesh { name = "CilindroMetrico" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // ---------------- cristales facetados, al pie de paredes y rocas

    /// <summary>Prisma de 6 caras con punta: una cara por lado con su brillo (alfa del color), gradiente de base a punta en uv.y y aristas en uv.x.</summary>
    Mesh CristalFacetadoMesh(float alto, float radio, Color baseCol)
    {
        const int lados = 6;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var col = new List<Color>(); var t = new List<int>();
        float yH = alto * 0.72f, rH = radio * 0.88f, fH = 0.72f;
        for (int s = 0; s < lados; s++)
        {
            float a0 = s / (float)lados * Mathf.PI * 2f, a1 = (s + 1) / (float)lados * Mathf.PI * 2f;
            var b0 = new Vector3(Mathf.Cos(a0) * radio, 0f, Mathf.Sin(a0) * radio); var b1 = new Vector3(Mathf.Cos(a1) * radio, 0f, Mathf.Sin(a1) * radio);
            var h0 = new Vector3(Mathf.Cos(a0) * rH, yH, Mathf.Sin(a0) * rH); var h1 = new Vector3(Mathf.Cos(a1) * rH, yH, Mathf.Sin(a1) * rH);
            var c = new Color(baseCol.r, baseCol.g, baseCol.b, R(0.7f, 1f));
            int i = v.Count;
            v.Add(b0); v.Add(b1); v.Add(h1); v.Add(h0);
            uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f)); uv.Add(new Vector2(1f, fH)); uv.Add(new Vector2(0f, fH));
            for (int k = 0; k < 4; k++) col.Add(c);
            t.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            int j = v.Count;
            v.Add(h0); v.Add(h1); v.Add(new Vector3(0f, alto, 0f));
            uv.Add(new Vector2(0f, fH)); uv.Add(new Vector2(1f, fH)); uv.Add(new Vector2(0.5f, 1f));
            var c2 = new Color(c.r, c.g, c.b, Mathf.Min(1f, c.a + 0.1f));
            for (int k = 0; k < 3; k++) col.Add(c2);
            t.AddRange(new[] { j, j + 1, j + 2 });
        }
        var m = new Mesh { name = "Cristal" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetColors(col); m.SetTriangles(t, 0);
        m.RecalculateBounds();
        return m;
    }

    /// <summary>
    /// Racimo de 3 a 5 cristales pequeños (escala 0,35 a 0,9) al pie de una pared o roca: cada uno se apoya en lo que haya debajo
    /// de verdad (raycast) y, si no hay superficie, no se pone. Morados y cian apagados; con luz en algunos racimos.
    /// </summary>
    void CristalesFacetados(Transform parent, Vector3 pos, bool violeta, bool luz)
    {
        var c = Group("Cristales", parent);
        c.localPosition = pos;
        Color col = violeta ? new Color(0.58f, 0.46f, 0.86f) : new Color(0.38f, 0.72f, 0.82f);
        int n = rng.Next(3, 6), puestos = 0;
        for (int i = 0; i < n; i++)
        {
            float s = R(0.35f, 0.9f), alto = R(0.9f, 2.4f) * s, radio = 0.16f * s + 0.04f;
            float ox = R(-0.55f, 0.55f), oz = R(-0.55f, 0.55f);
            if (!SuperficieBajo(pos.x + ox, pos.z + oz, out float y)) continue;
            var go = MeshObject(c, "Cristal", CristalFacetadoMesh(alto, radio, col), mExpCristal, false);
            go.transform.localPosition = new Vector3(ox, y - pos.y - alto * 0.06f, oz);
            go.transform.localRotation = Quaternion.Euler(R(-18f, 18f), R(0f, 360f), R(-18f, 18f));
            puestos++;
        }
        if (puestos > 0 && luz) AddLight(c, new Vector3(0f, 0.9f, 0f), violeta ? new Color(0.7f, 0.52f, 1f) : new Color(0.42f, 0.78f, 0.92f), 7f, 2.2f);
    }

    // ---------------- decales del suelo: grietas y charcos con el reflejo del fuego

    bool CercaDelSendero(float x, float z, float d)
    {
        foreach (var p in trail) if (Mathf.Abs(p.x - x) < d && Mathf.Abs(p.y - z) < d) return true;
        return false;
    }

    static void PlanoDecal(List<Vector3> v, List<Vector2> uv, List<int> t, Vector3 centro, float lado, float yaw)
    {
        int i = v.Count;
        var q = Quaternion.Euler(0f, yaw, 0f);
        float h = lado / 2f;
        v.Add(centro + q * new Vector3(-h, 0f, -h)); v.Add(centro + q * new Vector3(h, 0f, -h)); v.Add(centro + q * new Vector3(-h, 0f, h)); v.Add(centro + q * new Vector3(h, 0f, h));
        uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(1f, 0f)); uv.Add(new Vector2(0f, 1f)); uv.Add(new Vector2(1f, 1f));
        t.AddRange(new[] { i, i + 2, i + 3, i, i + 3, i + 1 });
    }

    void DecalesSuelo(Transform g)
    {
        var grp = Group("Decales", g);
        var gv = new List<Vector3>(); var guv = new List<Vector2>(); var gt = new List<int>();
        var cv = new List<Vector3>(); var cuv = new List<Vector2>(); var ct = new List<int>();
        var rv = new List<Vector3>(); var ruv = new List<Vector2>(); var rt = new List<int>();

        // grietas repartidas por el suelo, nunca sobre el sendero
        for (int i = 0, intentos = 0; i < 36 && intentos < 500; intentos++)
        {
            float x = R(-20f, 20f), z = R(-60f, 124f);
            if (CercaDelSendero(x, z, 2.6f) || !SuperficieBajo(x, z, out float y) || Mathf.Abs(y) > 0.35f) continue;
            PlanoDecal(gv, guv, gt, new Vector3(x, y + 0.03f, z), R(1.4f, 3.2f), R(0f, 360f));
            i++;
        }
        // charcos junto a las fogatas (el campamento y algunos braseros), con una mancha naranja encima que hace de reflejo
        var fuegos = new List<Vector2> { new Vector2(0f, -50f) };
        int k = 0;
        for (float z = -40f; z < ExpEdgeZ; z += 16f, k++) if (k % 3 == 1) fuegos.Add(new Vector2((k % 2 == 0 ? -1f : 1f) * 7f, z));
        foreach (var f in fuegos)
            for (int p = 0, intentos = 0; p < (f.x == 0f ? 4 : 1) && intentos < 40; intentos++)
            {
                float ang = R(0f, Mathf.PI * 2f), d = f.x == 0f ? R(3.2f, 6.5f) : R(2.2f, 3.4f);
                float x = f.x + Mathf.Cos(ang) * d, z = f.y + Mathf.Sin(ang) * d;
                if (CercaDelSendero(x, z, 1.8f) || Mathf.Abs(x) > 19f || !SuperficieBajo(x, z, out float y) || Mathf.Abs(y) > 0.35f) continue;
                float lado = R(1.8f, 3.4f), yaw = R(0f, 360f);
                PlanoDecal(cv, cuv, ct, new Vector3(x, y + 0.035f, z), lado, yaw);
                PlanoDecal(rv, ruv, rt, new Vector3(x - Mathf.Cos(ang) * lado * 0.12f, y + 0.045f, z - Mathf.Sin(ang) * lado * 0.12f), lado * 0.55f, yaw);   // el reflejo se corre hacia el fuego
                p++;
            }
        void Malla(string nombre, List<Vector3> v, List<Vector2> uv, List<int> t, Material mat)
        {
            if (v.Count == 0) return;
            var m = new Mesh { name = nombre };
            m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            m.RecalculateNormals(); m.RecalculateBounds();
            MeshObject(grp, nombre, m, mat, false);
        }
        Malla("Grietas", gv, guv, gt, mExpGrieta);
        Malla("Charcos", cv, cuv, ct, mExpCharco);
        Malla("Reflejos", rv, ruv, rt, mExpReflejo);
    }
}
