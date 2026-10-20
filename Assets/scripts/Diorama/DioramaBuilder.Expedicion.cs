using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tutorial — la expedición en el piso 20 (propuesta del guion: "que sea la expedición en el piso 20 y muestre cómo
/// fueron derrotados"). Una gruta oscura: el campamento de la expedición, un escalón para aprender a saltar, la zona de
/// entrenamiento, la barrera de energía que se cruza con impulso, la pelea junto a la expedición y el borde del abismo
/// donde aparece la criatura que los derrota. Lo maneja ExpedicionPiso20.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect ExpArea = new Rect(-70f, -84f, 140f, 280f);
    static readonly Rect ExpWalk = new Rect(-24f, -66f, 48f, 196f);
    static readonly Vector3 ExpSpawn = new Vector3(0f, 0.1f, -58f);
    const float ExpLedgeZ0 = -30f, ExpLedgeZ1 = -14f, ExpVeilZ = 4f, ExpEdgeZ = 128f;

    float ExpHeight(float x, float z)
    {
        float e = EdgeNoise(x, z);
        if (z > ExpEdgeZ + e * 0.8f) return -40f;   // el abismo
        // las paredes no van en el terreno por celdas (en pendiente se ven como escalones): son la malla lisa de ExpRoca
        if (z > ExpLedgeZ0 + e * 0.6f && z < ExpLedgeZ1 + e * 0.6f) return 0.9f;   // el escalón (hay que saltar)
        return 0f;
    }

    /// <summary>Altura de la roca de las paredes de la gruta: sube en curva con bultos (0 = suelo).</summary>
    static float ExpRoca(float x, float z)
    {
        float e = EdgeNoise(x, z);
        float d = Mathf.Max(Mathf.Abs(x) - 21f + e * 2.5f, -(z + 66f) + e * 2.5f);
        if (d <= 0f) return 0f;
        return Mathf.Min(ExpTecho + 2f, d * d * 0.22f + d * 0.9f + Noise(x * 0.22f + 3f, z * 0.22f) * 4f * Mathf.Clamp01(d / 4f));
    }

    /// <summary>Las paredes de la gruta en una malla lisa (normales suaves), con colisión para la cámara.</summary>
    void ParedesGruta(Transform parent)
    {
        const float cell = 1.25f;
        float x0 = -48f, z0 = -92f, x1 = 48f, z1 = ExpEdgeZ + 3f;
        int nx = Mathf.CeilToInt((x1 - x0) / cell), nz = Mathf.CeilToInt((z1 - z0) / cell);
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        var idx = new int[nx + 1, nz + 1];
        for (int i = 0; i <= nx; i++)
            for (int j = 0; j <= nz; j++)
            {
                float x = x0 + i * cell, z = z0 + j * cell, y = ExpRoca(x, z) - 0.05f;
                idx[i, j] = v.Count;
                v.Add(new Vector3(x, y, z));
                uv.Add(new Vector2((z + Mathf.Abs(x)) * 0.25f, (y + Mathf.Abs(x) * 0.5f) * 0.25f));
            }
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                int a = idx[i, j], b = idx[i + 1, j], c = idx[i, j + 1], d = idx[i + 1, j + 1];
                if (v[a].y < 0f && v[b].y < 0f && v[c].y < 0f && v[d].y < 0f) continue;   // suelo: lo pone el terreno
                t.Add(a); t.Add(c); t.Add(d); t.Add(a); t.Add(d); t.Add(b);
            }
        var m = new Mesh { name = "ParedesGruta", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
        var go = MeshObject(parent, "ParedesGruta", m, mCaveRock, true);
        go.AddComponent<MeshCollider>().sharedMesh = m;
    }

    /// <summary>El sendero serpentea suave (una curva con muchos puntos, sin esquinas).</summary>
    static Vector2[] Sendero(float za, float zb)
    {
        int n = Mathf.CeilToInt((zb - za) / 2.5f);
        var p = new Vector2[n + 1];
        for (int i = 0; i <= n; i++) { float z = Mathf.Lerp(za, zb, i / (float)n); p[i] = new Vector2(Mathf.Sin(z * 0.06f) * 2.6f + Mathf.Sin(z * 0.17f + 1f) * 0.8f, z); }
        return p;
    }

    void BuildExpedition()
    {
        BuildTerrain();
        var g = Group("Gruta", root);
        ParedesGruta(g);
        var lvl = Group("Nivel", root);

        // cristales y estalagmitas por las paredes
        for (int i = 0; i < 60; i++)
        {
            float z = R(-64f, 126f), x = (rng.NextDouble() < 0.5 ? -1f : 1f) * R(17f, 21f);
            float y = Mathf.Max(ExpHeight(x, z), ExpRoca(x, z)) - 0.1f;
            if (rng.NextDouble() < 0.5) Crystals(g, new Vector3(x, y, z), R(0.6f, 1.6f), rng.NextDouble() < 0.4, i % 4 == 0);
            else Stalagmite(g, new Vector3(x, y, z), R(2f, 6f));
        }
        // el campamento de la expedición
        Tent(g, new Vector3(-10f, 0f, -56f), 30f, mCloth);
        Tent(g, new Vector3(10f, 0f, -54f), -30f, mClothB);
        Campfire(g, new Vector3(0f, 0f, -50f));
        Clutter(g, new Vector3(-6f, 0f, -60f));
        Clutter(g, new Vector3(7f, 0f, -61f));
        foreach (float sx in new[] { -1f, 1f })
        {
            var tri = Group("Reflector", g);
            tri.localPosition = new Vector3(sx * 12f, 0f, -44f);
            Box(tri, "Poste", new Vector3(0f, 1.5f, 0f), new Vector3(0.15f, 3f, 0.15f), mMetal, false);
            Box(tri, "Foco", new Vector3(0f, 3.1f, 0.2f), new Vector3(0.7f, 0.5f, 0.6f), mMetal, false);
            var l = new GameObject("Haz").AddComponent<Light>();
            l.transform.SetParent(tri, false);
            l.transform.localPosition = new Vector3(0f, 3.1f, 0.6f);
            l.transform.localRotation = Quaternion.Euler(25f, -sx * 15f, 0f);
            l.type = LightType.Spot; l.spotAngle = 55f; l.range = 50f; l.intensity = 14f; l.color = new Color(1f, 0.95f, 0.85f); l.shadows = LightShadows.None;
        }
        // un letrero de la expedición: "PISO 20"
        var sign = Group("Letrero", g);
        sign.localPosition = new Vector3(5f, 0f, -45f);
        Box(sign, "Poste", new Vector3(0f, 0.9f, 0f), new Vector3(0.12f, 1.8f, 0.12f), mTimber, false);
        Box(sign, "Tabla", new Vector3(0f, 1.7f, -0.05f), new Vector3(1.4f, 0.6f, 0.08f), mPlank, false);
        Box(sign, "Marca", new Vector3(0f, 1.7f, -0.1f), new Vector3(1f, 0.25f, 0.02f), Glow("Dio_MarcaPiso", new Color(1f, 0.8f, 0.4f), 1.2f), false);

        // la barrera de energía (solo se cruza con impulso)
        var veil = new GameObject("VeloDeFase");
        veil.transform.SetParent(lvl, false);
        veil.transform.localPosition = new Vector3(0f, 3f, ExpVeilZ);
        veil.AddComponent<BoxCollider>().size = new Vector3(48f, 8f, 0.4f);
        // una cortina de energía translúcida (antes era una pared rosa opaca)
        var energia = Saved("Dio_VeloEnergia", () => new Material(Shader.Find("Abby/Energia")));
        energia.SetColor("_Color", new Color(0.55f, 0.3f, 1.4f));
        var face = Prim(veil.transform, PrimitiveType.Quad, "Cortina", new Vector3(0f, 1.5f, 0f), new Vector3(44f, 9f, 1f), energia, false);
        face.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        AddLight(veil.transform, new Vector3(0f, -1f, -1.5f), new Color(0.6f, 0.35f, 1f), 14f, 2.5f);
        veil.AddComponent<PhaseVeil>();

        // el borde del abismo y la oscuridad de abajo
        for (int i = 0; i < 9; i++) Crystals(g, new Vector3(-16f + i * 4f, 0f, ExpEdgeZ - 1.2f), R(0.4f, 0.7f), true, i % 3 == 0);
        Flat(g, "Fondo", new Vector3(0f, -39f, ExpEdgeZ + 60f), 200f, 120f, Plain("Dio_Negro", Color.black, 0f), 0.1f);
        // la criatura que espera abajo (la cinemática la sube)
        var colosoPf = Resources.Load<GameObject>("Enemigos/Jefe");
        var coloso = Group("Coloso", lvl);
        coloso.localPosition = new Vector3(0f, -48f, ExpEdgeZ + 28f);   // mide ~31 m: al subir, los ojos quedan sobre el borde
        if (colosoPf != null)
        {
            var v = (GameObject)Instantiate(colosoPf, coloso);
            v.transform.localScale = Vector3.one * 7f;
            v.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var prop = coloso.gameObject.AddComponent<CriaturaProp>();
            prop.cuerpo = new Color(0.1f, 0.08f, 0.13f); prop.ojos = new Color(1f, 0.15f, 0.1f); prop.bob = 0.6f;
        }
        AddLight(coloso, new Vector3(0f, 18f, -14f), new Color(1f, 0.2f, 0.15f), 60f, 7f);
        AddLight(coloso, new Vector3(-12f, 26f, 10f), new Color(0.45f, 0.75f, 1f), 45f, 6f);   // contraluz: se lee la silueta contra la pared
        AddLight(coloso, new Vector3(0f, 4f, -24f), new Color(0.35f, 0.7f, 1f), 40f, 3f);     // la luz de las runas del borde, desde abajo

        // la expedición: el capitán y los cazadores (ExpedicionPiso20 los va moviendo)
        var exp = Group("Expedicion", lvl);
        var capP = new Persona { torso = new Color(0.14f, 0.17f, 0.26f), piernas = new Color(0.1f, 0.11f, 0.16f), piel = new Color(0.72f, 0.55f, 0.43f),
                                 pelo = new Color(0.4f, 0.38f, 0.36f), escala = 1.05f, sombrero = 2, medallas = true, arma = 2, energia = new Color(1f, 0.8f, 0.4f), mochila = true };
        Figura(exp, "Capitan", new Vector3(-2f, 0f, -48f), 180f, capP);
        Color[] e = { new Color(0.35f, 0.8f, 1f), new Color(1f, 0.55f, 0.2f), new Color(0.5f, 1f, 0.6f), new Color(0.9f, 0.4f, 1f), new Color(0.35f, 0.8f, 1f), new Color(1f, 0.55f, 0.2f) };
        for (int i = 0; i < 6; i++)
        {
            var pe = i % 3 == 2 ? Persona.Soldado(true) : Persona.Cazador(e[i]);
            if (i == 3) pe.arma = 3;
            Figura(exp, "Miembro" + i, new Vector3(-8f + i * 3f, 0f, -52f + (i % 2) * 2f), 0f, pe);
        }

        // combate del tutorial: entrenamiento y la oleada con la expedición
        var a1 = Arena(lvl, "Entrenamiento", new Vector3(0f, 0f, 30f), new Vector2(30f, 18f), 3);
        a1.roster = new[] { TowerEnemy.Kind.Sombra };
        a1.leash = 10f;
        var a2 = Arena(lvl, "La oleada", new Vector3(0f, 0f, 78f), new Vector2(34f, 22f), 6);
        a2.roster = new[] { TowerEnemy.Kind.Sombra, TowerEnemy.Kind.Bruto, TowerEnemy.Kind.Sombra, TowerEnemy.Kind.Saltador };
        a2.leash = 12f;

        Trail(g, 3.6f, Sendero(-60f, ExpLedgeZ0 - 1f));
        Trail(g, 3.6f, Sendero(ExpLedgeZ1 + 1f, ExpEdgeZ - 3f));
        for (float z = -40f; z < ExpEdgeZ; z += 16f) foreach (float sx in new[] { -1f, 1f }) Brazier(lvl, new Vector3(sx * 7f, 0f, z));

        GrutaUnica(g);
        WalkBoundary(ExpWalk);
        var bg = Group("Fondo", root);
        OuterLand(bg, 14f, true, true, true, true, 0);

        var dir = new GameObject("Tutorial");
        dir.transform.SetParent(root, false);
        dir.AddComponent<ExpedicionPiso20>();
        BuildAtmosphere();
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
    }
}
