using UnityEngine;

/// <summary>
/// Piso 8 — La Aldea de la Nieve Eterna (noche estrellada). Una aldea de montaña (calle larga, plaza, casas con techos
/// nevados, capilla con campanario); detrás de la capilla, el bosque de pinos con la cabaña del cazador, un puente sobre
/// la garganta helada con su cascada congelada y, al fondo, el lago dormido donde espera Escarcha. Nieva sin parar.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect SnowArea = new Rect(-110f, -95f, 220f, 380f);
    static readonly Rect SnowWalk = new Rect(-60f, -64f, 120f, 312f);
    static readonly Vector3 SnowSpawn = new Vector3(0f, 0.1f, -56f);
    static readonly Vector2 SnowLake = new Vector2(0f, 214f);
    static readonly Vector2 SnowPond = new Vector2(36f, -8f);
    const float StreetZ0 = -64f, StreetZ1 = 54f;
    const float IceY = -0.3f, GorgeZ0 = 150f, GorgeZ1 = 166f, SnowLakeR = 22f;

    float SnowHeight(float x, float z)
    {
        float e = EdgeNoise(x, z);
        if (Vector2.Distance(new Vector2(x, z), SnowLake) < SnowLakeR + e * 0.8f) return -0.8f;
        if (Vector2.Distance(new Vector2(x, z), SnowPond) < 8.5f + e * 0.6f) return -0.8f;
        // la garganta helada (se cruza por el puente)
        if (z > GorgeZ0 + e * 1.2f && z < GorgeZ1 + e * 1.2f && Mathf.Abs(x) < 70f) return -7f;
        float v = Mathf.Max(Mathf.Abs(x) - 54f, Mathf.Max(z - 242f, -z - 66f)) + e * 2f;
        if (v <= 0f) return 0f;
        float b = v < 5f ? 1.5f : v < 10f ? 3f : v < 16f ? 4.8f : v < 23f ? 7f : 9.5f;
        if (b >= 4.8f && b < 9.5f && Noise(x * 0.09f + 5f, z * 0.09f) > 0.62f) b += 2.5f;
        return b;
    }

    void Snowman(Transform parent, Vector3 pos)
    {
        var s = Group("MonoDeNieve", parent);
        s.localPosition = pos;
        Prim(s, PrimitiveType.Sphere, "Base", new Vector3(0f, 0.5f, 0f), Vector3.one * 1f, mSnowPix, true);
        Prim(s, PrimitiveType.Sphere, "Cuerpo", new Vector3(0f, 1.2f, 0f), Vector3.one * 0.72f, mSnowPix);
        Prim(s, PrimitiveType.Sphere, "Cabeza", new Vector3(0f, 1.75f, 0f), Vector3.one * 0.5f, mSnowPix);
        var nose = MeshObject(s, "Nariz", Cone(1f, 1f, 6), mFruitY, false);
        nose.transform.localPosition = new Vector3(0f, 1.75f, -0.24f);
        nose.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        nose.transform.localScale = new Vector3(0.06f, 0.3f, 0.06f);
        Box(s, "Bufanda", new Vector3(0f, 1.5f, 0f), new Vector3(0.6f, 0.1f, 0.6f), mCloth, false);
        blocked.Add(new Rect(pos.x - 1f, pos.z - 1f, 2f, 2f));
    }

    /// <summary>Detrás de la capilla: el bosque de pinos, la cabaña del cazador, la garganta helada y el lago dormido.</summary>
    void BuildSnowNorth(Transform g)
    {
        var lvl = Group("NivelNorte", root);
        // sendero: rodea la capilla, cruza el bosque y el puente, y llega al lago
        Trail(g, 4f, new Vector2(0f, StreetZ1 - 6f), new Vector2(-11f, StreetZ1 + 2f), new Vector2(-11f, 76f), new Vector2(0f, 86f),
              new Vector2(8f, 100f), new Vector2(-6f, 120f), new Vector2(4f, 138f), new Vector2(0f, GorgeZ0 - 3f));
        Trail(g, 4f, new Vector2(0f, GorgeZ1 + 3f), new Vector2(0f, 180f), new Vector2(0f, SnowLake.y - SnowLakeR - 1f));
        RopeBridge(lvl, "PuenteDeHielo", new Vector3(0f, 0f, GorgeZ0 - 2.5f), new Vector3(0f, 0f, GorgeZ1 + 2.5f), 4.6f);
        // cercas en las orillas de la garganta (menos en el puente)
        foreach (float gz in new[] { GorgeZ0 - 2f, GorgeZ1 + 2f })
        {
            Fence(g, new Vector2(-54f, gz), new Vector2(-2.8f, gz));
            Fence(g, new Vector2(2.8f, gz), new Vector2(54f, gz));
        }
        var floor = Flat(g, "HieloGarganta", new Vector3(0f, -6.9f, (GorgeZ0 + GorgeZ1) / 2f), 150f, GorgeZ1 - GorgeZ0 + 4f, mIce, 0.15f);
        floor.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // la cascada congelada en la pared norte de la garganta
        var fall = Group("CascadaHelada", g);
        fall.localPosition = new Vector3(28f, 0f, GorgeZ1 + 0.4f);
        for (int i = 0; i < 9; i++)
        {
            var c = Box(fall, "Hielo", new Vector3(R(-3f, 3f), R(-6.5f, 0.5f), R(-0.6f, 0.2f)), new Vector3(R(1f, 2.4f), R(2f, 5f), R(0.6f, 1.2f)), mIce, false);
            c.transform.localRotation = Quaternion.Euler(R(-6f, 6f), R(-20f, 20f), R(-6f, 6f));
        }
        Crystals(fall, new Vector3(0f, -6.8f, -2f), 1.4f, false, true);
        Crystals(fall, new Vector3(-3f, 0f, 1.5f), 0.9f, true, false);
        // cabaña del cazador en el bosque
        var hunter = new Vector3(-30f, 0f, 104f);
        House(g, hunter, 90f, 6.5f, 5f, 3.4f, true);
        Campfire(lvl, hunter + new Vector3(6.5f, 0f, -3f));
        Clutter(g, hunter + new Vector3(5f, 0f, 4f));
        for (int i = 0; i < 3; i++) Box(g, "Lena", hunter + new Vector3(4.6f, 0.25f + i * 0.3f, -6f), new Vector3(2f, 0.3f, 0.9f), mBarkPix, true);
        Snowman(g, new Vector3(14f, 0f, 92f));
        foreach (var lp in new[] { new Vector3(-7f, 0f, 70f), new Vector3(5f, 0f, 92f), new Vector3(-3f, 0f, 128f), new Vector3(4f, 0f, GorgeZ0 - 6f),
                                   new Vector3(-4f, 0f, GorgeZ1 + 6f), new Vector3(4f, 0f, 186f) })
            PortLamp(lvl, lp);
        // el bosque de pinos nevados
        EnsurePlantMeshes();
        for (int i = 0, tries = 0; i < 260 && tries < 9000; tries++)
        {
            var q = new Vector2(R(-58f, 58f), R(StreetZ1 + 18f, 252f));
            if (q.y > GorgeZ0 - 4f && q.y < GorgeZ1 + 4f) continue;
            float h = SnowHeight(q.x, q.y);
            if (h < -0.2f || !Free(q, 2.4f) || ShadowsTrail(q)) continue;
            if (!Flat(q, 1.2f, h)) continue;
            Pine(g, q, h, R(1f, 1.6f), true);
            blocked.Add(new Rect(q.x - 1.4f, q.y - 1.4f, 2.8f, 2.8f));
            i++;
        }
        Chest(lvl, hunter + new Vector3(0f, 0f, 6f), 26, 180f);
        Chest(lvl, new Vector3(44f, 0f, GorgeZ0 - 10f), 30, -90f);
        Note(lvl, hunter + new Vector3(4f, 0f, -6f), 180f, LoreNote.Kind.Nota, "dio6_cazador", "Nota de un cazador",
             "Salí a revisar las trampas. Si llegaste hasta aquí, sírvete de la fogata.\nNo cruces el lago de noche: algo duerme bajo el hielo y no le gusta que lo despierten.");
    }

    void BuildSnow()
    {
        // techos nevados y pinos escarchados
        mRoof = PixelMat("Dio_TejadoNieve", "pix_snow", new Color(0.92f, 0.95f, 1f), 0.2f);
        mPinePix = PixelMat("Dio_PinoNevado", "pix_pine", new Color(1.45f, 1.6f, 1.7f), 0.1f);
        mWindow = mWindowLit;   // de noche todas las ventanas encendidas
        BuildTerrain();
        var g = Group("Aldea", root);
        var lvl = Group("Nivel", root);

        // lago congelado (se puede caminar)
        var ice = Flat(g, "Hielo", new Vector3(SnowLake.x, IceY, SnowLake.y), SnowLakeR * 2f + 4f, SnowLakeR * 2f + 4f, mIce, 0.2f);
        ice.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        SolidBox(g, "Collider_Hielo", new Vector3(SnowLake.x, IceY - 0.25f, SnowLake.y), new Vector3(SnowLakeR * 2f + 4f, 0.5f, SnowLakeR * 2f + 4f));
        for (int i = 0; i < 16; i++)
        {
            float a = i / 16f * Mathf.PI * 2f;
            if (Mathf.Abs(Mathf.Sin(a) + 1f) < 0.25f) continue;   // por el sur se entra al lago
            Crystals(g, new Vector3(SnowLake.x + Mathf.Cos(a) * (SnowLakeR + 0.6f), 0f, SnowLake.y + Mathf.Sin(a) * (SnowLakeR + 0.6f)), R(0.6f, 1.1f), i % 3 == 0, i % 4 == 0);
        }
        blocked.Add(new Rect(SnowLake.x - SnowLakeR - 2f, SnowLake.y - SnowLakeR - 2f, SnowLakeR * 2f + 4f, SnowLakeR * 2f + 4f));
        // estanque de patinar en la aldea (el de antes, más chico)
        var pond = Flat(g, "Estanque", new Vector3(SnowPond.x, IceY, SnowPond.y), 18f, 18f, mIce, 0.25f);
        pond.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        SolidBox(g, "Collider_Estanque", new Vector3(SnowPond.x, IceY - 0.25f, SnowPond.y), new Vector3(18f, 0.5f, 18f));
        blocked.Add(new Rect(SnowPond.x - 10f, SnowPond.y - 10f, 20f, 20f));
        BuildSnowNorth(g);

        // la aldea es una calle larga hacia el norte (cómoda con la cámara de maqueta): casas a los dos lados,
        // bien separadas, una plaza ancha a la mitad y la capilla al fondo
        Flat(g, "Calle", new Vector3(0f, 0.011f, (StreetZ0 + StreetZ1) / 2f), 5f, StreetZ1 - StreetZ0, mStoneBrick, 0.5f);
        Flat(g, "Plaza", new Vector3(0f, 0.012f, 0f), 30f, 16f, mStoneBrick, 0.5f);
        blocked.Add(new Rect(-4f, StreetZ0, 8f, StreetZ1 - StreetZ0 + 8f));
        blocked.Add(new Rect(-16f, -9f, 32f, 18f));
        float[] rows = { -44f, -26f, 22f, 38f };
        for (int i = 0; i < rows.Length; i++)
        {
            House(g, new Vector3(-13f, 0f, rows[i]), 90f, 7f, 5.5f, 3.6f + (i % 2) * 0.4f, true);
            House(g, new Vector3(13f, 0f, rows[i] + 4f), -90f, 6.5f, 5f, 3.4f + ((i + 1) % 2) * 0.4f, true);
            Clutter(g, new Vector3(-8.5f, 0f, rows[i] + 5f));
            Clutter(g, new Vector3(8.5f, 0f, rows[i] - 1f));
        }
        // casitas sueltas más lejos (no en círculo: dispersas por las orillas)
        House(g, new Vector3(-44f, 0f, -36f), 60f, 5.5f, 4.5f, 3.2f, true);
        House(g, new Vector3(-46f, 0f, 30f), 120f, 6f, 4.8f, 3.4f, true);
        // capilla con campanario al final de la calle
        var chapel = Group("Capilla", g);
        chapel.localPosition = new Vector3(0f, 0f, StreetZ1 + 4f);
        House(chapel, Vector3.zero, 180f, 8f, 7f, 5f, false);
        var tower = Group("Campanario", chapel);
        tower.localPosition = new Vector3(0f, 0f, 4.5f);
        Box(tower, "Torre", new Vector3(0f, 5f, 0f), new Vector3(3f, 10f, 3f), mStoneBrick, true);
        Box(tower, "Campana", new Vector3(0f, 9.2f, -1.55f), new Vector3(1f, 1.2f, 0.1f), mGlow, false);
        var spire = MeshObject(tower, "Aguja", Cone(1f, 1f, 4), mRoof, true);
        spire.transform.localPosition = new Vector3(0f, 10f, 0f);
        spire.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        spire.transform.localScale = new Vector3(2.4f, 4f, 2.4f);
        AddLight(tower, new Vector3(0f, 9f, -2.5f), new Color(1f, 0.75f, 0.45f), 12f, 3f);
        blocked.Add(new Rect(-6f, StreetZ1 - 2f, 12f, 16f));

        // faroles a lo largo de la calle (alternados), pozo y monos de nieve en la plaza
        for (float z = StreetZ0 + 8f; z < StreetZ1; z += 11f)
            PortLamp(lvl, new Vector3((Mathf.RoundToInt(z) / 11) % 2 == 0 ? 3.4f : -3.4f, 0f, z));
        foreach (var lp in new[] { new Vector3(-14f, 0f, -7f), new Vector3(14f, 0f, -7f), new Vector3(-14f, 0f, 7f), new Vector3(14f, 0f, 7f), new Vector3(22f, 0f, -6f) })
            PortLamp(lvl, lp);
        Well(g, new Vector3(-8f, 0f, 3f));
        Snowman(g, new Vector3(9f, 0f, 4f));
        Snowman(g, new Vector3(SnowPond.x - 12f, 0f, SnowPond.y - 6f));

        // combate: el claro del oeste, el bosque del noreste y el jefe sobre el lago
        Arena(lvl, "El claro helado", new Vector3(-34f, 0f, -6f), new Vector2(18f, 18f), 5);
        Arena(lvl, "El bosque del norte", new Vector3(34f, 0f, 36f), new Vector2(18f, 16f), 5);
        Arena(lvl, "El bosque de pinos", new Vector3(-4f, 0f, 118f), new Vector2(22f, 14f), 5);
        Arena(lvl, "El puente de hielo", new Vector3(0f, 0f, 176f), new Vector2(22f, 12f), 5);
        var boss = Arena(lvl, "Jefe", new Vector3(SnowLake.x, IceY, SnowLake.y), new Vector2(30f, 30f), 1, true);
        Exit(lvl, new Vector3(SnowLake.x, IceY, SnowLake.y + 12f), boss);
        // la posada: mercader y santuario en la plaza
        MerchantPost(lvl, new Vector3(-8f, 0f, -5f), 90f);
        HealShrine(lvl, new Vector3(8f, 0f, -4f));
        Chest(lvl, new Vector3(-52f, 0f, -54f), 30, 45f);
        Chest(lvl, new Vector3(50f, 0f, 56f), 28, 200f);
        Note(lvl, new Vector3(4f, 0f, -56f), 180f, LoreNote.Kind.Inscripcion, "dio6_aldea", "Bienvenidos a la aldea",
             "Piso 8. Aquí siempre es la última noche del invierno.\nHay posada en la plaza. Los que siguen bajando deben cruzar la garganta helada.");
        Note(lvl, new Vector3(-4f, 0f, StreetZ1 - 4f), 180f, LoreNote.Kind.Diario, "dio6_diario", "Diario de la expedición — día 18",
             "Hace un frío que no se quita. Dormimos en la posada vacía.\nAbis, si algún día lees esto: perdóname por no hacerte caso. Te dije que volvería, y voy a hacer todo para cumplirlo.", "— " + Historia.Hermano);

        foreach (float sz in new[] { 0f, 110f, 220f }) { var cl = Group("Clima", root); cl.localPosition = new Vector3(0f, 0f, sz); Snowfall(cl); }
        WalkBoundary(SnowWalk);
        var bg = Group("Fondo", root);
        OuterLand(bg, 9.5f, true, true, true, true, 320);
        EnsurePlantMeshes();
        var peak = Saved("Dio_MontanaNevada", () => { var m = new Material(mSnowPix); m.SetColor("_BaseColor", new Color(0.72f, 0.8f, 1f)); m.mainTextureScale = new Vector2(4f, 4f); return m; });
        for (int i = 0; i < 18; i++)
        {
            float x = -320f + i * 38f + R(-10f, 10f), z = R(560f, 680f), h = R(160f, 240f), r = R(60f, 90f);
            var m = MeshObject(bg, "Montana", blobMeshes[rng.Next(blobMeshes.Length)], peak, false);
            m.transform.localPosition = new Vector3(x, 9.5f - h * 0.1f, z);
            m.transform.localScale = new Vector3(r * 2.2f, h, r * 1.6f);
        }

        var shots = Group("Tomas", root);
        Shot(shots, "Apertura", Vector3.zero, Vector2.one, 0f, 30f, 95f, 32f, 5f, new Vector3(0f, 0f, 8f), 2.6f, 10, intro: 4.5f);
        Shot(shots, "Lago", new Vector3(SnowLake.x, 0f, SnowLake.y - SnowLakeR - 6f), new Vector2(10f, 6f), 0f, 14f, 30f, 40f, 8f, new Vector3(0f, 0f, 10f), 2f, 2,
             "EL LAGO DORMIDO", "Aquí duerme Escarcha", still: true);
        Shot(shots, "Garganta", new Vector3(0f, 0f, GorgeZ0 - 6f), new Vector2(8f, 5f), 60f, 16f, 30f, 40f, 4f, new Vector3(6f, -2f, 8f), 2f, 2,
             "LA GARGANTA HELADA", "", still: true);
        Shot(shots, "Capilla", new Vector3(0f, 0f, StreetZ1 - 16f), new Vector2(6f, 8f), 0f, 12f, 20f, 38f, 10f, new Vector3(0f, 2f, 8f), 2f, 2,
             "LA CAPILLA DE LA NIEVE", "", still: true);
        FinishWorld(true, false);
    }
}
