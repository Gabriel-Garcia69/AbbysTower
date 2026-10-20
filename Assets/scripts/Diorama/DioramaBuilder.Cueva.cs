using UnityEngine;

/// <summary>
/// Piso 4 — La Gruta de Cristal. Una caverna enorme con un lago subterráneo: se entra por un túnel, se baja una escalinata
/// a la orilla, y pasarelas de tablones cruzan el lago a dos islas. Cristales azules y lilas iluminan las paredes,
/// hongos que brillan, estalagmitas, haces de luz que caen de grietas del techo y una cascada al fondo.
/// En la isla del norte, entre ruinas de piedra azul, late el Corazón de Geoda.
/// </summary>
public partial class DioramaBuilder
{
    static readonly Rect CaveArea = new Rect(-70f, -62f, 140f, 128f);
    static readonly Rect CaveWalk = new Rect(-45f, -53.5f, 90f, 101.5f);
    static readonly Vector3 CaveSpawn = new Vector3(0f, 3.6f, -50f);
    static readonly Vector2 CaveI1 = new Vector2(0f, 4f), CaveI2 = new Vector2(0f, 26f);
    const float CaveLakeY = -1.2f;

    float CaveEll(float x, float z, float e) => Mathf.Sqrt((x / 52f) * (x / 52f) + ((z - 8f) / 42f) * ((z - 8f) / 42f)) + e * 0.03f;

    float CaveHeight(float x, float z)
    {
        float e = EdgeNoise(x, z);
        if (Mathf.Abs(x) < 3.4f && z > -31f && z < -19f) return 0f;    // escalinata de bajada
        if (Mathf.Abs(x) < 6.5f + e * 0.3f && z <= -31f && z > -54f) return 3.5f;   // túnel de entrada (cerrado al fondo)
        float ell = CaveEll(x, z, e);
        if (ell > 1f) return z < -24f ? 5.5f : 14f;   // al sur (lado de la cámara) la pared es baja: se ve la gruta por encima
        if (Vector2.Distance(new Vector2(x, z), CaveI1) < 6f + e * 0.5f) return 0.3f;
        if (Vector2.Distance(new Vector2(x, z), CaveI2) < 8.5f + e * 0.5f) return 0.6f;
        if (Vector2.Distance(new Vector2(x, z), new Vector2(14f, 45f)) < 4.5f) return -4f;   // poza de la cascada
        if (ell > 0.86f) return 3.5f;
        if (ell > 0.72f) return 1.5f;
        if (ell > 0.55f) return 0f;
        return -4f;
    }

    void BuildCave()
    {
        BuildTerrain();
        var g = Group("Gruta", root);
        var lvl = Group("Nivel", root);
        var water = Flat(g, "Lago", new Vector3(0f, CaveLakeY, 8f), 140f, 120f, mWater, 0.08f);
        water.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        Stairs(lvl, "Bajada", new Vector3(0f, 0f, -19.5f), new Vector3(0f, 3.5f, -31f), 5.6f, mCaveBrick);
        // arco de la entrada
        var gate = Group("Entrada", g);
        gate.localPosition = new Vector3(0f, 3.5f, -33f);
        foreach (float sx in new[] { -1f, 1f }) Box(gate, "Jamba", new Vector3(sx * 5.6f, 3f, 0f), new Vector3(1.4f, 6f, 1.6f), mCaveBrick, true);
        Box(gate, "Dintel", new Vector3(0f, 6.4f, 0f), new Vector3(12.8f, 1f, 1.8f), mCaveBrick, false);
        foreach (float sx in new[] { -1f, 1f }) Brazier(gate, new Vector3(sx * 3.6f, 0f, 1.6f));

        // pasarelas sobre el lago
        Deck(lvl, -2.6f, 2.6f, -18.6f, -1.4f);
        Rail(lvl, new Vector2(-2.6f, -18.2f), new Vector2(-2.6f, -1.8f));
        Rail(lvl, new Vector2(2.6f, -18.2f), new Vector2(2.6f, -1.8f));
        Deck(lvl, -2.6f, 2.6f, 9.4f, 18.4f);
        Rail(lvl, new Vector2(-2.6f, 9.8f), new Vector2(-2.6f, 18f));
        Rail(lvl, new Vector2(2.6f, 9.8f), new Vector2(2.6f, 18f));
        blocked.Add(new Rect(-4f, -20f, 8f, 40f));

        // cascada al fondo
        float wz = 45f;
        for (float z = 40f; z < 60f; z += 0.2f) if (CaveEll(14f, z, 0f) > 1f) { wz = z - 0.15f; break; }
        Waterfall(g, new Vector3(14f, (14f + CaveLakeY) / 2f, wz), Vector3.back, 4f, 14f - CaveLakeY);
        Foam(g, new Vector3(14f, CaveLakeY + 0.1f, wz - 1.5f), 2.6f, 26f);

        // cristales en terrazas y paredes (algunos alumbran), hongos y estalagmitas
        int lit = 0;
        for (int i = 0, tries = 0; i < 46 && tries < 3000; tries++)
        {
            var q = new Vector2(R(-55f, 55f), R(-40f, 55f));
            float e = EdgeNoise(q.x, q.y), ell = CaveEll(q.x, q.y, e);
            if (ell < 0.95f || ell > 1.05f) continue;   // al pie de la pared: las terrazas quedan libres para caminar
            float h = CaveHeight(q.x, q.y);
            bool light = lit < 14 && i % 3 == 0;
            Crystals(g, new Vector3(q.x, Mathf.Min(h, 14f), q.y), R(0.8f, 1.8f), rng.NextDouble() < 0.4, light);
            if (light) lit++;
            i++;
        }
        for (int i = 0, tries = 0; i < 40 && tries < 2000; tries++)
        {
            var q = new Vector2(R(-45f, 45f), R(-30f, 45f));
            float h = CaveHeight(q.x, q.y);
            float el = CaveEll(q.x, q.y, EdgeNoise(q.x, q.y));
            bool foot = (el > 0.83f && el < 0.86f) || el > 0.96f;   // pegadas al muro de la terraza de arriba
            if (h < 1f || h > 4f || !foot || !Free(q, 1f)) continue;
            Stalagmite(g, new Vector3(q.x, h, q.y), R(1.5f, 4.5f));
            i++;
        }
        for (int i = 0, tries = 0; i < 26 && tries < 2000; tries++)
        {
            var q = new Vector2(R(-40f, 40f), R(-20f, 42f));
            float h = CaveHeight(q.x, q.y);
            if (h < -0.1f || h > 4f || !Free(q, 0.5f)) continue;
            Mushrooms(g, new Vector3(q.x, h, q.y));
            i++;
        }

        // ruinas de piedra azul en la isla del norte y la geoda
        var ruins = Group("Ruinas", g);
        ruins.localPosition = new Vector3(CaveI2.x, 0.6f, CaveI2.y);
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2f + 0.2f;
            if (Mathf.Abs(Mathf.Sin(a) + 1f) < 0.35f) continue;   // hueco donde llega la pasarela (sur)
            bool broken = i % 3 == 1;
            float hh = broken ? R(1.2f, 2.5f) : 4.6f;
            Box(ruins, "Columna", new Vector3(Mathf.Cos(a) * 7f, hh / 2f, Mathf.Sin(a) * 7f), new Vector3(0.9f, hh, 0.9f), mCaveBrick, true);
            if (!broken) Box(ruins, "Capitel", new Vector3(Mathf.Cos(a) * 7f, hh + 0.15f, Mathf.Sin(a) * 7f), new Vector3(1.2f, 0.3f, 1.2f), mCaveBrick, false);
        }
        Crystals(ruins, new Vector3(0f, 0f, 3.5f), 2.6f, true, true);
        var lt0 = AddLight(ruins, new Vector3(0f, 4f, 3.5f), new Color(0.75f, 0.5f, 1f), 18f, 5f);
        lt0.gameObject.AddComponent<LightFlicker>().amount = 0.1f;

        // orilla este: campamento del mercader; oeste: emboscada; islas: pelea y jefe
        MerchantPost(lvl, new Vector3(32f, 0f, 12f), -90f);
        HealShrine(lvl, new Vector3(31f, 0f, 2f));
        Campfire(lvl, new Vector3(34f, 0f, 6.5f));
        Arena(lvl, "Emboscada en la orilla", new Vector3(-33f, 0f, 6f), new Vector2(8f, 16f), 5);
        Arena(lvl, "La isla de los ecos", new Vector3(CaveI1.x, 0.3f, CaveI1.y), new Vector2(9f, 9f), 4);
        var boss = Arena(lvl, "Jefe", new Vector3(CaveI2.x, 0.6f, CaveI2.y - 1f), new Vector2(12f, 11f), 1, true);
        Exit(lvl, new Vector3(CaveI2.x, 0.6f, CaveI2.y + 4f), boss);
        Chest(lvl, new Vector3(-33f, 0f, 18f), 30, 90f);
        Chest(lvl, new Vector3(36f, 0f, 22f), 24, -90f);
        Note(lvl, new Vector3(-3.8f, 3.5f, -38f), 90f, LoreNote.Kind.Diario, "dio4_diario", "Diario de la expedición — día 9",
             "El piso 5 es una gruta llena de cristales. Brillan con la misma energía que usamos en las armas.\nAquí los minerales salen más grandes. Con lo de este piso pagaríamos todas las deudas, Abis. Todas.", "— " + Historia.Hermano);
        Note(lvl, new Vector3(5.5f, 0.6f, CaveI2.y - 3f), -90f, LoreNote.Kind.Inscripcion, "dio4_geoda", "Grabado en la piedra azul",
             "Al fondo de la gruta, una piedra que late como un corazón.\nLos canalizadores dicen que la energía de la torre sale de aquí, o pasa por aquí. Nadie lo ha confirmado.");

        WalkBoundary(CaveWalk);
        var bg = Group("Fondo", root);
        OuterLand(bg, 14f, true, false, true, true, 0);
        OuterLand(bg, 5.5f, false, true, false, false, 0);

        var shots = Group("Tomas", root);
        Shot(shots, "Apertura", Vector3.zero, Vector2.one, 0f, 34f, 80f, 34f, 0f, new Vector3(0f, 0f, 22f), 2.6f, 10, intro: 4.5f);
        Shot(shots, "Lago", new Vector3(0f, 0f, -10f), new Vector2(3.4f, 14f), 90f, 18f, 22f, 36f, 4f, new Vector3(0f, 0f, 6f), 1.6f, 2,
             "EL LAGO DE CRISTAL", "");
        Shot(shots, "Campamento", new Vector3(32f, 0f, 7f), new Vector2(7f, 14f), -70f, 16f, 18f, 38f, 3f, new Vector3(-8f, 0f, 4f), 2f, 2,
             "EL CAMPAMENTO DEL MERCADER", "", still: true);
        FinishWorld(false, false);
    }
}
