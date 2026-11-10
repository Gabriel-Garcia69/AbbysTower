using UnityEngine;

/// <summary>
/// El escenario del final: un pedazo roto de la torre flotando sobre el abismo, alargado e irregular. Una escalinata enorme sube
/// hacia una puerta entreabierta, sola en el aire, por la que entra el amanecer: hasta aquí llegó Dante.
/// Muy lejos de ahí, la casa en la nieve donde terminan los créditos.
/// No tiene enemigos ni HUD: lo maneja EscenaFinal.
/// </summary>
public partial class DioramaBuilder
{
    public static readonly Vector3 FinalSpawn = new Vector3(0f, 0.1f, -26f);
    public static readonly Rect FinalWalk = new Rect(-30f, -40f, 60f, 90f);
    static readonly Rect FinalArea = new Rect(-90f, -80f, 180f, 180f);
    public const float FinalEscalonZ0 = 6f, FinalDescansoZ0 = 13f, FinalDescansoZ1 = 31f, FinalEscalonZ1 = 37f, FinalRellano = 6f, FinalDescanso = 3f, FinalPuertaZ = 52f;
    /// <summary>Donde está la casa de los créditos, lejos de la sala.</summary>
    public static readonly Vector3 FinalCasa = new Vector3(600f, 0f, 0f);

    /// <summary>Plano arriba y con el contorno roto: se angosta hacia la puerta, con entrantes y salientes, nada redondo.</summary>
    float FinalHeight(float x, float z)
    {
        if (z < -46f || z > FinalPuertaZ + 6f) return -80f;
        float ancho = Mathf.Lerp(30f, 17f, Mathf.InverseLerp(-46f, FinalPuertaZ, z));
        float corte = (Noise(z * 0.05f + (x > 0f ? 4f : 9f), 2f) - 0.5f) * 22f + (Noise(z * 0.2f, x > 0f ? 7f : 1f) - 0.5f) * 8f;
        float punta = Mathf.Clamp01((z + 46f) / 10f) * Mathf.Clamp01((FinalPuertaZ + 6f - z) / 4f);
        return Mathf.Abs(x) < (ancho + corte) * punta ? 0f : -80f;
    }

    void BuildFinal()
    {
        MaterialesExpedicion();
        mCaveBrick = Saved("Dio_SillarFinal", () => new Material(mExpParedAlt));
        mCaveBrick.SetColor("_BaseColor", new Color(0.62f, 0.6f, 0.68f));
        BuildTerrain();
        foreach (Transform t in root.Find("Terreno")) t.name = "RocaGruta";
        var g = Group("Sala", root);
        // debajo cuelga la roca de donde se arrancó, y alrededor flotan los pedazos de la torre
        var bajo = MeshObject(g, "RocaBajo", RocaIrregular(1f, 1f, k => Mathf.Pow(1f - k, 0.7f) + 0.02f, 22, 12), mExpPared, false);
        bajo.transform.localPosition = new Vector3(0f, -0.4f, 2f);
        bajo.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
        bajo.transform.localScale = new Vector3(30f, 90f, 54f);
        // abajo, un mar de nubes que se funde con el horizonte (sin esto se veía una franja negra y luego el cielo)
        var nubeFinal = Saved("Dio_NubeLisa", () => new Material(Shader.Find("Universal Render Pipeline/Unlit")));
        nubeFinal.SetColor("_BaseColor", new Color(0.5f, 0.22f, 0.3f));
        var marNubes = Flat(g, "Nubes", new Vector3(0f, -26f, 0f), 2400f, 2400f, nubeFinal, 0.01f);
        marNubes.GetComponent<MeshRenderer>().receiveShadows = false;
        marNubes.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        BrumaAbismo(g, -24f);
        var flotan = Group("Flotan", g);
        for (int i = 0; i < 70; i++)
        {
            float ang = R(0f, Mathf.PI * 2f), dist = R(48f, 190f), tam = R(1.5f, dist > 110f ? 15f : 7f);
            var m = MeshObject(flotan, "Pedazo", RocaIrregular(tam, tam * R(0.8f, 1.8f), k => Mathf.Sin(Mathf.PI * Mathf.Clamp(k, 0.03f, 0.97f)) * 0.9f + 0.1f, 9, 6), i % 2 == 0 ? mExpPared : mExpParedAlt, false);
            m.transform.localPosition = new Vector3(Mathf.Cos(ang) * dist, R(-50f, 55f), 8f + Mathf.Sin(ang) * dist);
            m.transform.localRotation = Quaternion.Euler(R(0f, 360f), R(0f, 360f), R(0f, 360f));
            m.AddComponent<EscenaFinal.Flotar>().Iniciar(R(0f, 6f), R(0.25f, 0.9f), R(0.5f, 3f));
        }

        // la escalinata y el rellano
        var lvl = Group("Nivel", root);
        // un tramo corto, un descanso grande (ahí se quedó él) y otro tramo a la explanada de la puerta
        Stairs(lvl, "EscalinataBaja", new Vector3(0f, 0f, FinalEscalonZ0), new Vector3(0f, FinalDescanso, FinalDescansoZ0), 22f, mCaveBrick);
        Box(lvl, "Descanso", new Vector3(0f, FinalDescanso / 2f, (FinalDescansoZ0 + FinalDescansoZ1) / 2f), new Vector3(26f, FinalDescanso, FinalDescansoZ1 - FinalDescansoZ0), mCaveBrick, true, 0.25f);
        Stairs(lvl, "EscalinataAlta", new Vector3(0f, FinalDescanso, FinalDescansoZ1), new Vector3(0f, FinalRellano, FinalEscalonZ1), 18f, mCaveBrick);
        foreach (float sx in new[] { -1f, 1f })
        {
            Brazier(lvl, new Vector3(sx * 11.5f, FinalDescanso, FinalDescansoZ0 + 2f));
            Brazier(lvl, new Vector3(sx * 11.5f, FinalDescanso, FinalDescansoZ1 - 2f));
            Brazier(lvl, new Vector3(sx * 11f, FinalRellano, FinalEscalonZ1 + 4f));
            // columnas rotas en las orillas del descanso
            for (int k = 0; k < 3; k++)
            {
                var col = MeshObject(lvl, "ColumnaDescanso", RocaIrregular(R(0.9f, 1.2f), R(2f, 7f), q => 1f - q * 0.2f, 10, 6), mExpParedAlt, true);
                col.transform.localPosition = new Vector3(sx * R(10.5f, 12f), FinalDescanso - 0.1f, FinalDescansoZ0 + 4f + k * 5.5f);
                col.transform.localRotation = Quaternion.Euler(R(-6f, 6f), R(0f, 360f), R(-6f, 6f));
            }
        }
        Box(lvl, "Rellano", new Vector3(0f, FinalRellano / 2f, (FinalEscalonZ1 + FinalPuertaZ) / 2f), new Vector3(26f, FinalRellano, FinalPuertaZ - FinalEscalonZ1), mCaveBrick, true, 0.25f);

        // la puerta enorme, entreabierta; atrás de ella, el amanecer
        var puerta = Group("Puerta", lvl);
        puerta.localPosition = new Vector3(0f, FinalRellano, FinalPuertaZ);
        foreach (float sx in new[] { -1f, 1f })
            Box(puerta, "Jamba", new Vector3(sx * 8.5f, 11f, 0f), new Vector3(3f, 22f, 3.4f), mCaveBrick, true, 0.25f);
        Box(puerta, "Dintel", new Vector3(0f, 23f, 0f), new Vector3(20f, 3f, 3.4f), mCaveBrick, true, 0.25f);
        // atrás de la puerta: su pueblo al amanecer, la casa a la que él volvía
        var aire = Saved("Dio_PuebloTrasPuerta", () => new Material(Shader.Find("Universal Render Pipeline/Unlit")));
        var pueblo = Resources.Load<Texture2D>("Intro/01_pueblo");
        if (pueblo != null)
        {
            aire.SetTexture("_BaseMap", pueblo);
            aire.SetTextureScale("_BaseMap", new Vector2(0.36f, 1f));   // solo el pedazo que cabe en el hueco de la puerta
            aire.SetTextureOffset("_BaseMap", new Vector2(0.34f, 0f));
            aire.SetColor("_BaseColor", new Color(1.5f, 1.25f, 1.05f));
        }
        else aire = Glow("Dio_Amanecer", new Color(1f, 0.72f, 0.42f), 2.2f);
        var luz = Prim(puerta, PrimitiveType.Quad, "Luz", new Vector3(0f, 11f, 1.4f), new Vector3(14.2f, 22f, 1f), aire);
        luz.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        luz.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        foreach (float sx in new[] { -1f, 1f })
        {
            var hoja = Group("Hoja" + (sx < 0f ? "Izq" : "Der"), puerta);
            hoja.localPosition = new Vector3(sx * 7f, 0f, 0f);
            hoja.localRotation = Quaternion.Euler(0f, sx * -6f, 0f);   // casi cerrada: EscenaFinal la abre un poco
            Box(hoja, "Madera", new Vector3(-sx * 3.55f, 11f, -0.2f), new Vector3(7.1f, 22f, 0.8f), Plain("Dio_PuertaVieja", new Color(0.13f, 0.1f, 0.09f), 0.1f), true);
        }
        var luzPuerta = AddLight(puerta, new Vector3(0f, 12f, -7f), new Color(1f, 0.72f, 0.46f), 48f, 220f);
        luzPuerta.name = "LuzPuerta";

        // el haz de luz que baja por las escaleras
        var haz = MatDecal("Dio_HazFinal", "haz", true, new Color(1f, 0.72f, 0.44f) * 0.2f);
        for (int k = 0; k < 3; k++)
        {
            var q = Prim(puerta, PrimitiveType.Quad, "Haz", new Vector3((k - 1) * 3.4f, 4f, -16f), new Vector3(9f, 38f, 1f), haz);
            q.transform.localRotation = Quaternion.Euler(62f, 180f, 0f);
            q.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ruinas: columnas rotas a los lados del camino, rocas y cristales en las orillas
        for (int side = -1; side <= 1; side += 2)
            for (int k = 0; k < 4; k++)
            {
                float z = -34f + k * 10f + R(-2f, 2f);
                var col = MeshObject(g, "ColumnaRota", RocaIrregular(R(1.3f, 1.9f), R(3f, 15f), q => 1f - q * 0.25f, 11, 6), mExpParedAlt, true);
                col.transform.localPosition = new Vector3(side * R(11f, 15f), -0.2f, z);
                col.transform.localRotation = Quaternion.Euler(R(-9f, 9f), R(0f, 360f), R(-9f, 9f));
            }
        for (int i = 0; i < 80; i++)
        {
            float z = R(-44f, 10f), x = R(-30f, 30f);
            if (FinalHeight(x, z) < -1f || Mathf.Abs(x) < 8f) continue;
            if (i % 3 == 0) CristalesFacetados(g, new Vector3(x, 0f, z), rng.NextDouble() < 0.75, i % 12 == 0);
            else Penasco(g, new Vector3(x, 0f, z), R(0.6f, 2.2f));
        }
        foreach (float z in new[] { -30f, -6f, 12f })
            AddLight(g, new Vector3(0f, 18f, z), new Color(0.72f, 0.6f, 0.9f), 46f, 340f).name = "LuzFria";
        foreach (float sx in new[] { -1f, 1f })
        {
            Brazier(lvl, new Vector3(sx * 13f, 0f, 2f));
            Brazier(lvl, new Vector3(sx * 8f, 0f, -18f));
        }
        VolumenExpedicion();

        // ---- la casa de la nieve, para los créditos
        CasaDeLaNieve();

        var dir = new GameObject("Final");
        dir.transform.SetParent(root, false);
        dir.AddComponent<EscenaFinal>();
        BuildAtmosphere();
    }

    /// <summary>Bancos de bruma sobre el mar de nubes.</summary>
    void BrumaAbismo(Transform g, float y)
    {
        var ps = new GameObject("Bruma").AddComponent<ParticleSystem>();
        ps.transform.SetParent(g, false);
        ps.transform.localPosition = new Vector3(0f, y, 0f);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.startLifetime = 30f; main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(26f, 60f); main.startColor = new Color(1f, 0.66f, 0.58f, 0.2f);
        main.maxParticles = 500; main.prewarm = true; main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 15f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(500f, 4f, 500f);
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mSmoke;
        ps.Play();
    }

    void CasaDeLaNieve()
    {
        mPaja = Plain("Dio_TechoNevado", new Color(0.66f, 0.72f, 0.86f), 0.12f);
        var c = Group("CasaNieve", root);
        c.position = FinalCasa;
        Flat(c, "Nieve", new Vector3(0f, 0f, 30f), 300f, 240f, mSnowPix != null ? mSnowPix : mStone, 0.25f);
        var casa = Group("Casa", c);
        casa.localPosition = new Vector3(0f, 0f, 34f);
        casa.localRotation = Quaternion.Euler(0f, 180f, 0f);
        const float w = 11f, d = 8f, h = 4.6f;
        CasaPiedra(casa, Vector3.zero, 0f, w, d, h, true);
        var encendida = Glow("Dio_VentanaCasa", new Color(1f, 0.66f, 0.32f), 2.4f);
        var apagada = Plain("Dio_VentanaOscura", new Color(0.03f, 0.04f, 0.07f), 0.7f);
        Box(casa, "VentanaAbby", new Vector3(-2.8f, 2.9f, d / 2f + 0.12f), new Vector3(1.5f, 1.8f, 0.14f), encendida, false);
        Box(casa, "VentanaDante", new Vector3(2.8f, 2.9f, d / 2f + 0.12f), new Vector3(1.5f, 1.8f, 0.14f), apagada, false);
        var luz = AddLight(casa, new Vector3(-2.8f, 2.8f, d / 2f + 2.4f), new Color(1f, 0.68f, 0.38f), 12f, 16f);
        luz.name = "LuzAbby";
        var luz2 = AddLight(casa, new Vector3(2.8f, 2.8f, d / 2f + 2.4f), new Color(1f, 0.68f, 0.38f), 12f, 16f);
        luz2.name = "LuzDante"; luz2.enabled = false;
        for (int i = 0; i < 46; i++)
        {
            float x = R(-90f, 90f), z = R(36f, 120f);
            if (Mathf.Abs(x) < 12f && z < 50f) continue;
            Pine(c, new Vector2(x, z), 0f, R(1.1f, 2.2f), false);
        }
        var nieve = Group("Nieve", c);
        nieve.localPosition = new Vector3(0f, 0f, 20f);
        Snowfall(nieve);
        var luna = AddLight(c, new Vector3(-20f, 40f, 0f), new Color(0.55f, 0.62f, 0.9f), 120f, 900f);
        luna.name = "LuzLuna";
    }
}
