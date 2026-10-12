using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Piezas compartidas por los mundos de la ruta HD-2D: materiales por mundo (suelo, paredes, lecho), puentes colgantes,
/// palmeras, cristales, estalagmitas, carpas, fogatas, nieve que cae, tierra de afuera y límites.
/// </summary>
public partial class DioramaBuilder
{
    Material mSandPix, mSnowPix, mSandstone, mCaveRock, mCaveBrick, mStoneBrick, mIce, mCrystalA, mCrystalB, mMushroom, mCloud;

    Material TopMat => layout == Layout.Templo ? mSandPix : layout == Layout.Nevado ? mSnowPix : layout == Layout.Cueva ? mCaveRock : mGrass;
    Material WallMat => layout == Layout.Cueva ? mCaveRock : layout == Layout.Templo ? mSandPix : mRock;
    Material BedMat => layout == Layout.Pueblo ? mDirt : layout == Layout.Cueva ? mCaveRock : layout == Layout.Nevado ? mIce : mSand ?? mDirt;

    void LoadWorldMaterials()
    {
        mSand = Plain("Dio_Arena", new Color(0.86f, 0.78f, 0.58f), 0.1f);
        mWhite = Plain("Dio_Blanco", new Color(0.95f, 0.93f, 0.88f), 0.15f);
        mRedPaint = Plain("Dio_RojoFaro", new Color(0.72f, 0.16f, 0.14f), 0.2f);
        mSail = Saved("Dio_Vela", () =>
        {
            var m = new Material(LitShader);
            m.SetColor("_BaseColor", new Color(0.96f, 0.92f, 0.82f));
            m.SetFloat("_Cull", 0f);   // se ve de los dos lados
            return m;
        });
        mHull = Plain("Dio_Casco", new Color(0.32f, 0.2f, 0.12f), 0.15f);
        mSandPix = PixelMat("Dio_ArenaPix2", "pix_sand", new Color(1.15f, 1f, 0.82f), 0.05f);
        mSnowPix = PixelMat("Dio_NievePix", "pix_snow", Color.white, 0.25f);
        mSandstone = PixelMat("Dio_ArenisPix", "pix_sandstone", Color.white, 0.08f);
        mCaveRock = PixelMat("Dio_RocaGrutaPix2", "pix_caverock", new Color(1f, 1.05f, 1.2f), 0.3f);
        mCaveBrick = PixelMat("Dio_SillarGrutaPix", "pix_cavebrick", new Color(0.8f, 0.95f, 1.2f), 0.2f);
        mStoneBrick = PixelMat("Dio_SillarPix", "pix_stonebrick", Color.white, 0.1f);
        mIce = Saved("Dio_Hielo", () => { var m = PixelMat("Dio_HieloBase", "pix_ice", Color.white, 0.85f); var c = new Material(m); c.SetFloat("_Smoothness", 0.9f); return c; });
        // brillo moderado: con más intensidad el tonemapping los vuelve blancos y se pierde el color
        mCrystalA = Glow("Dio_CristalAzul2", new Color(0.3f, 0.75f, 1f), 1.35f);
        mCrystalB = Glow("Dio_CristalLila2", new Color(0.7f, 0.4f, 1f), 1.35f);
        mMushroom = Glow("Dio_Hongo2", new Color(0.3f, 1f, 0.75f), 1.2f);
        mCloud = Plain("Dio_Nube", new Color(1f, 0.86f, 0.88f), 0.05f);
    }

    // ------------------------------------------------------------------ puentes

    /// <summary>Puente colgante de tablones entre dos puntos (puede subir o bajar), con barandal de cuerda y colisión.</summary>
    void RopeBridge(Transform parent, string name, Vector3 a, Vector3 b, float width = 3.6f)
    {
        var root2 = Group(name, parent);
        Vector3 flat = b - a; float rise = flat.y; flat.y = 0f;
        float len = flat.magnitude;
        root2.localPosition = a;
        root2.localRotation = Quaternion.LookRotation(flat.normalized);
        float L = Mathf.Sqrt(len * len + rise * rise), slope = Mathf.Atan2(rise, len) * Mathf.Rad2Deg;
        int n = Mathf.Max(4, Mathf.RoundToInt(L / 0.45f));
        for (int i = 0; i < n; i++)
        {
            float t = (i + 0.5f) / n;
            float sag = Mathf.Sin(t * Mathf.PI) * Mathf.Min(0.6f, len * 0.025f);   // se cuelga un poco al centro
            var p = Box(root2, "Tablon", new Vector3(R(-0.05f, 0.05f), rise * t - sag + 0.04f, len * t), new Vector3(width, 0.08f, L / n * 0.85f), mPlank, false);
            p.transform.localRotation = Quaternion.Euler(-slope + R(-2f, 2f), R(-2f, 2f), R(-2f, 2f));
        }
        foreach (float sx in new[] { -1f, 1f })
        {
            for (int i = 0; i <= 4; i++)
            {
                float t = i / 4f;
                float sag = Mathf.Sin(t * Mathf.PI) * Mathf.Min(0.6f, len * 0.025f);
                Box(root2, "Poste", new Vector3(sx * width / 2f, rise * t - sag + 0.55f, len * t), new Vector3(0.12f, 1.1f, 0.12f), mTimber, false);
            }
            foreach (float y in new[] { 0.55f, 1f })
            {
                var r = Box(root2, "Cuerda", new Vector3(sx * width / 2f, rise / 2f + y - Mathf.Min(0.6f, len * 0.025f) * 0.6f, len / 2f), new Vector3(0.05f, 0.05f, L), mPlank, false);
                r.transform.localRotation = Quaternion.Euler(-slope, 0f, 0f);
            }
            var rc = new GameObject("Collider_Baranda");
            rc.transform.SetParent(root2, false);
            rc.transform.localPosition = new Vector3(sx * (width / 2f + 0.1f), rise / 2f + 1f, len / 2f);
            rc.transform.localRotation = Quaternion.Euler(-slope, 0f, 0f);
            rc.AddComponent<BoxCollider>().size = new Vector3(0.2f, 2f, L);
        }
        var col = new GameObject("Collider_Tablero");
        col.transform.SetParent(root2, false);
        col.transform.localPosition = new Vector3(0f, rise / 2f - 0.12f, len / 2f);
        col.transform.localRotation = Quaternion.Euler(-slope, 0f, 0f);
        col.AddComponent<BoxCollider>().size = new Vector3(width, 0.25f, L + 0.6f);
        // estacas en las dos puntas
        foreach (float t in new[] { 0f, 1f })
            foreach (float sx in new[] { -1f, 1f })
                Box(root2, "Estaca", new Vector3(sx * (width / 2f + 0.15f), rise * t + 0.7f, len * t), new Vector3(0.22f, 1.4f, 0.22f), mTimber, false);
        Vector3 lo = Vector3.Min(a, b), hi = Vector3.Max(a, b);
        blocked.Add(new Rect(lo.x - width, lo.z - width, hi.x - lo.x + width * 2f, hi.z - lo.z + width * 2f));
    }

    // ------------------------------------------------------------------ vegetación y piedras especiales

    void Palm(Transform parent, Vector3 pos, float s)
    {
        var p = Group("Palmera", parent);
        p.localPosition = pos;
        p.localRotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
        p.localScale = Vector3.one * s;
        Vector3 at = Vector3.zero; float lean = R(6f, 16f);
        for (int i = 0; i < 6; i++)
        {
            var seg = Prim(p, PrimitiveType.Cylinder, "Tronco", at + new Vector3(0f, 0.4f, 0f), new Vector3(0.32f - i * 0.02f, 0.42f, 0.32f - i * 0.02f), mBarkPix);
            seg.transform.localRotation = Quaternion.Euler(lean * i / 5f, 0f, 0f);
            at += Quaternion.Euler(lean * i / 5f, 0f, 0f) * new Vector3(0f, 0.8f, 0f);
        }
        for (int k = 0; k < 8; k++)
        {
            var leaf = Group("Hoja", p);
            leaf.localPosition = at;
            leaf.localRotation = Quaternion.Euler(R(15f, 40f), k * 45f + R(-10f, 10f), 0f);
            var blade = Box(leaf, "Lamina", new Vector3(0f, 0f, 1.3f), new Vector3(0.55f, 0.05f, 2.6f), mLeavesPix, false);
            blade.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
        }
        Prim(p, PrimitiveType.Sphere, "Cocos", at + Vector3.down * 0.25f, Vector3.one * 0.45f, mDoor);
    }

    /// <summary>Racimo de cristales que brillan (con una luz si 'light').</summary>
    void Crystals(Transform parent, Vector3 pos, float s, bool violet, bool light)
    {
        var c = Group("Cristales", parent);
        c.localPosition = pos;
        var mat = violet ? mCrystalB : mCrystalA;
        int n = rng.Next(4, 8);
        for (int i = 0; i < n; i++)
        {
            float h = R(0.8f, 2.4f) * s;
            var k = Box(c, "Cristal", new Vector3(R(-0.6f, 0.6f) * s, h * 0.4f, R(-0.6f, 0.6f) * s), new Vector3(0.35f * s, h, 0.35f * s), mat, false);
            k.transform.localRotation = Quaternion.Euler(R(-25f, 25f), R(0f, 90f), R(-25f, 25f));
            k.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        if (light) AddLight(c, new Vector3(0f, 1.5f * s, 0f), violet ? new Color(0.7f, 0.45f, 1f) : new Color(0.35f, 0.8f, 1f), 9f * s, 3.5f);
    }

    /// <summary>Estalagmita (cono de roca).</summary>
    void Stalagmite(Transform parent, Vector3 pos, float h)
    {
        var m = MeshObject(parent, "Estalagmita", Cone(1f, 1f, 7), mCaveRock, true);
        m.transform.localPosition = pos;
        m.transform.localScale = new Vector3(h * 0.28f, h, h * 0.28f);
        m.transform.localRotation = Quaternion.Euler(R(-5f, 5f), R(0f, 360f), R(-5f, 5f));
    }

    void Mushrooms(Transform parent, Vector3 pos)
    {
        var g = Group("Hongos", parent);
        g.localPosition = pos;
        for (int i = 0; i < rng.Next(3, 6); i++)
        {
            float s = R(0.25f, 0.6f);
            var p = new Vector3(R(-0.6f, 0.6f), 0f, R(-0.6f, 0.6f));
            Prim(g, PrimitiveType.Cylinder, "Pie", p + Vector3.up * s * 0.5f, new Vector3(s * 0.25f, s * 0.5f, s * 0.25f), mWhite);
            Prim(g, PrimitiveType.Sphere, "Sombrero", p + Vector3.up * s, new Vector3(s, s * 0.45f, s), mMushroom);
        }
    }

    /// <summary>Carpa de viajero con fogata.</summary>
    void Tent(Transform parent, Vector3 pos, float yaw, Material cloth)
    {
        var t = Group("Carpa", parent);
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var roof = MeshObject(t, "Lona", Roof(3.2f, 2.8f, 1.9f), cloth, true);
        roof.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        Box(t, "Palo", new Vector3(0f, 0.95f, 1.5f), new Vector3(0.1f, 1.9f, 0.1f), mTimber, false);
        Box(t, "Palo", new Vector3(0f, 0.95f, -1.5f), new Vector3(0.1f, 1.9f, 0.1f), mTimber, false);
        var col = t.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0f, 0.8f, 0f); col.size = new Vector3(2.8f, 1.6f, 3.2f);
        blocked.Add(new Rect(pos.x - 2.5f, pos.z - 2.5f, 5f, 5f));
    }

    void Campfire(Transform parent, Vector3 pos)
    {
        var f = Group("Fogata", parent);
        f.localPosition = pos;
        for (int i = 0; i < 7; i++)
        {
            float a = i / 7f * Mathf.PI * 2f;
            Prim(f, PrimitiveType.Sphere, "Piedra", new Vector3(Mathf.Cos(a) * 0.55f, 0.08f, Mathf.Sin(a) * 0.55f), new Vector3(0.3f, 0.2f, 0.3f), mStone);
        }
        for (int i = 0; i < 3; i++)
        {
            var log = Prim(f, PrimitiveType.Cylinder, "Leño", new Vector3(0f, 0.12f, 0f), new Vector3(0.12f, 0.4f, 0.12f), mBarkPix);
            log.transform.localRotation = Quaternion.Euler(80f, i * 60f, 0f);
        }
        var fire = Prim(f, PrimitiveType.Sphere, "Fuego", new Vector3(0f, 0.3f, 0f), new Vector3(0.45f, 0.5f, 0.45f), mGlow);
        fire.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var l = AddLight(f, new Vector3(0f, 1f, 0f), new Color(1f, 0.6f, 0.3f), 9f, 4f);
        l.gameObject.AddComponent<LightFlicker>().amount = 0.25f;
        Smoke(f, new Vector3(0f, 0.4f, 0f), new Color(1f, 0.55f, 0.25f, 0.6f), true);
        blocked.Add(new Rect(pos.x - 1f, pos.z - 1f, 2f, 2f));
    }

    /// <summary>Farol en poste; de noche alumbra.</summary>
    void LitLamp(Transform parent, Vector3 pos)
    {
        PortLamp(parent, pos);
    }

    void Snowfall(Transform parent)
    {
        var ps = new GameObject("Nieve").AddComponent<ParticleSystem>();
        ps.transform.SetParent(parent, false);
        ps.transform.localPosition = new Vector3(0f, 22f, 0f);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.startLifetime = 12f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startColor = new Color(1f, 1f, 1f, 0.9f);
        main.maxParticles = 4000; main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 320f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(110f, 1f, 110f);
        ps.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.25f;
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mDust;
        ps.Play();
    }

    // ------------------------------------------------------------------ alrededores y límites

    /// <summary>Tierra de afuera: planos a una altura alrededor del área (excepto los lados indicados) con bosque encima.</summary>
    void OuterLand(Transform g, float y, bool north, bool south, bool east, bool west, int trees, bool pines = true)
    {
        var a = TerrainArea;
        const float far = 700f;
        if (west) Flat(g, "AfueraOeste", new Vector3((-far + a.xMin + 1f) / 2f, y, (a.yMin + a.yMax) / 2f), far + a.xMin + 1f, far * 2f, TopMat, 0.25f);
        if (east) Flat(g, "AfueraEste", new Vector3((a.xMax - 1f + far) / 2f, y, (a.yMin + a.yMax) / 2f), far - a.xMax + 1f, far * 2f, TopMat, 0.25f);
        if (south) Flat(g, "AfueraSur", new Vector3((a.xMin + a.xMax) / 2f, y, (-far + a.yMin + 1f) / 2f), a.width, far + a.yMin + 1f, TopMat, 0.25f);
        if (north) Flat(g, "AfueraNorte", new Vector3((a.xMin + a.xMax) / 2f, y, (a.yMax - 1f + far) / 2f), a.width, far - a.yMax + 1f, TopMat, 0.25f);
        for (int i = 0, tries = 0; i < trees && tries < trees * 20; tries++)
        {
            var q = new Vector2(R(a.xMin - 160f, a.xMax + 160f), R(a.yMin - 160f, a.yMax + 160f));
            if (a.Contains(q)) continue;
            if (q.y < a.yMin && q.y > a.yMin - 40f && q.x > a.xMin - 20f && q.x < a.xMax + 20f && rng.NextDouble() < 0.85) continue;
            bool ok = (west && q.x < a.xMin) || (east && q.x > a.xMax) || (south && q.y < a.yMin && q.x > a.xMin && q.x < a.xMax) || (north && q.y > a.yMax && q.x > a.xMin && q.x < a.xMax);
            if (!ok) continue;
            if (pines || rng.NextDouble() < 0.6) Pine(g, q, y, R(1.2f, 2f), false); else Tree(g, q, y, R(1.2f, 2f), false);
            i++;
        }
    }

    void WalkBoundary(Rect r)
    {
        var g = Group("Limites", root);
        SolidBox(g, "Limite_O", new Vector3(r.xMin - 0.5f, 10f, r.center.y), new Vector3(1f, 60f, r.height));
        SolidBox(g, "Limite_E", new Vector3(r.xMax + 0.5f, 10f, r.center.y), new Vector3(1f, 60f, r.height));
        SolidBox(g, "Limite_S", new Vector3(r.center.x, 10f, r.yMin - 0.5f), new Vector3(r.width, 60f, 1f));
        SolidBox(g, "Limite_N", new Vector3(r.center.x, 10f, r.yMax + 0.5f), new Vector3(r.width, 60f, 1f));
    }

    /// <summary>Lo común al final de cada mundo: vegetación, ambiente y tomas.</summary>
    void FinishWorld(bool trees = true, bool grass = true)
    {
        if (trees) BuildTrees();
        if (grass) BuildGrass();
        BuildAtmosphere();
#if UNITY_EDITOR
        if (!Application.isPlaying) UnityEditor.EditorUtility.SetDirty(gameObject);
#endif
    }

    /// <summary>Ruido de orillas (mismo que el terreno) para los mundos nuevos.</summary>
    static float EdgeNoise(float x, float z) =>
        (Noise(x * 0.18f, z * 0.18f) * 2f - 1f) + (Noise(x * 0.5f + 40f, z * 0.5f) * 2f - 1f) * 0.35f + (Noise(x * 1.3f - 17f, z * 1.3f + 9f) * 2f - 1f) * 0.25f;
}
