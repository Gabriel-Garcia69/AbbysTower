using UnityEngine;

/// <summary>
/// Piso 6 — La Aldea de la Nieve Eterna (noche estrellada). Una aldea de montaña: plaza empedrada, casas con techos
/// nevados y ventanas encendidas, faroles cálidos, capilla con campanario, bosque de pinos nevados en bancales y un lago
/// congelado al sureste donde espera Escarcha. Nieva sin parar.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect SnowArea = new Rect(-110f, -95f, 220f, 200f);
    static readonly Rect SnowWalk = new Rect(-60f, -64f, 120f, 132f);
    static readonly Vector3 SnowSpawn = new Vector3(0f, 0.1f, -56f);
    static readonly Vector2 SnowLake = new Vector2(36f, -8f);
    const float StreetZ0 = -64f, StreetZ1 = 54f;
    const float IceY = -0.3f;

    float SnowHeight(float x, float z)
    {
        float e = EdgeNoise(x, z);
        if (Vector2.Distance(new Vector2(x, z), SnowLake) < 13.5f + e * 0.8f) return -0.8f;
        float v = Mathf.Max(Mathf.Abs(x) - 54f, Mathf.Max(z - 62f, -z - 66f)) + e * 2f;
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
        var ice = Flat(g, "Hielo", new Vector3(SnowLake.x, IceY, SnowLake.y), 30f, 30f, mIce, 0.25f);
        ice.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        SolidBox(g, "Collider_Hielo", new Vector3(SnowLake.x, IceY - 0.25f, SnowLake.y), new Vector3(30f, 0.5f, 30f));
        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * Mathf.PI * 2f;
            Crystals(g, new Vector3(SnowLake.x + Mathf.Cos(a) * 12.5f, 0f, SnowLake.y + Mathf.Sin(a) * 12.5f), R(0.5f, 0.9f), false, i % 4 == 0);
        }
        blocked.Add(new Rect(SnowLake.x - 14f, SnowLake.y - 14f, 28f, 28f));

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
        Snowman(g, new Vector3(SnowLake.x - 18f, 0f, SnowLake.y - 12f));

        // combate: el claro del oeste, el bosque del noreste y el jefe sobre el lago
        Arena(lvl, "El claro helado", new Vector3(-34f, 0f, -6f), new Vector2(18f, 18f), 5);
        Arena(lvl, "El bosque del norte", new Vector3(34f, 0f, 36f), new Vector2(18f, 16f), 5);
        var boss = Arena(lvl, "Jefe", new Vector3(SnowLake.x, IceY, SnowLake.y), new Vector2(22f, 22f), 1, true);
        Exit(lvl, new Vector3(SnowLake.x + 6f, IceY, SnowLake.y + 6f), boss);
        // la posada: mercader y santuario en la plaza
        MerchantPost(lvl, new Vector3(-8f, 0f, -5f), 90f);
        HealShrine(lvl, new Vector3(8f, 0f, -4f));
        Chest(lvl, new Vector3(-52f, 0f, -54f), 30, 45f);
        Chest(lvl, new Vector3(50f, 0f, 56f), 28, 200f);
        Note(lvl, new Vector3(4f, 0f, -56f), 180f, LoreNote.Kind.Inscripcion, "dio6_aldea", "Bienvenidos a la aldea",
             "Aquí siempre es la última noche del invierno.\nPasa, caliéntate. Nadie se queda mucho.");
        Note(lvl, new Vector3(-4f, 0f, StreetZ1 - 4f), 180f, LoreNote.Kind.Diario, "dio6_diario", "Campanas",
             "La campana de la capilla suena cada vez que alguien llega a la cima.\nLa oí una vez, hace mucho. Creo que era yo.", "— A.");

        Snowfall(Group("Clima", root));
        WalkBoundary(SnowWalk);
        var bg = Group("Fondo", root);
        OuterLand(bg, 9.5f, true, true, true, true, 320);
        EnsurePlantMeshes();
        var peak = Saved("Dio_MontanaNevada", () => { var m = new Material(mSnowPix); m.SetColor("_BaseColor", new Color(0.72f, 0.8f, 1f)); m.mainTextureScale = new Vector2(4f, 4f); return m; });
        for (int i = 0; i < 18; i++)
        {
            float x = -320f + i * 38f + R(-10f, 10f), z = R(380f, 480f), h = R(140f, 220f), r = R(60f, 90f);
            var m = MeshObject(bg, "Montana", blobMeshes[rng.Next(blobMeshes.Length)], peak, false);
            m.transform.localPosition = new Vector3(x, 9.5f - h * 0.1f, z);
            m.transform.localScale = new Vector3(r * 2.2f, h, r * 1.6f);
        }

        var shots = Group("Tomas", root);
        Shot(shots, "Apertura", Vector3.zero, Vector2.one, 0f, 30f, 95f, 32f, 5f, new Vector3(0f, 0f, 8f), 2.6f, 10, intro: 4.5f);
        Shot(shots, "Lago", new Vector3(SnowLake.x - 17f, 0f, SnowLake.y), new Vector2(6f, 8f), 90f, 14f, 22f, 36f, 8f, new Vector3(8f, 0f, 0f), 2f, 2,
             "EL LAGO DORMIDO", "", still: true);
        Shot(shots, "Capilla", new Vector3(0f, 0f, StreetZ1 - 16f), new Vector2(6f, 8f), 0f, 12f, 20f, 38f, 10f, new Vector3(0f, 2f, 8f), 2f, 2,
             "LA CAPILLA DE LA NIEVE", "", still: true);
        FinishWorld(true, false);
    }
}
