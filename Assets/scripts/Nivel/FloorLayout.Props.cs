using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Decoración y trampas de los pisos de los pecados. Cada tema tiene sus muebles (libreros, mesas de banquete,
/// montañas de oro, espejos, rosales, yunques, estatuas…) y su mecánica (niebla de sopor, fauces, monedas malditas,
/// la sombra que imita, remolinos, respiraderos de fuego, el Ojo que juzga).
/// </summary>
public partial class FloorLayout
{
    const float BibliotecaMezzanine = 3f;

    // ------------------------------------------------------------------ decoración por sala

    void ThemeDecor(Transform deco, RoomSpec r, List<Gap> gaps, int index)
    {
        float hx = r.size.x / 2f, hz = r.size.y / 2f, h = curWall;
        Motes(deco, new Vector3(0f, 1.2f, 0f), new Vector3(r.size.x - 2f, 2f, r.size.y - 2f), palette.motes, 7f, theme == FloorTheme.Pereza ? 0.05f : 0.15f, 8f);
        bool plainRoom = r.feature == RoomFeature.Ninguna;

        if (Mathf.Min(r.size.x, r.size.y) >= 10f && r.type != RoomType.Prueba && r.type != RoomType.Jefe)
            foreach (var c in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
                ThemePillar(deco, new Vector3(c.x * (hx - 0.9f), 0f, c.y * (hz - 0.9f)), 0.42f, h + 0.3f);

        switch (r.type)
        {
            case RoomType.Inicio:
                RuneRing(deco, Vector3.up * 0.02f, 2.1f, 2.4f, palette.accent, "CirculoInicio");
                RuneRing(deco, Vector3.up * 0.02f, 1.2f, 1.35f, palette.accent, "CirculoInicio2");
                Banners(deco, r, gaps, palette.banner);
                ThemeProps(deco, r, gaps, 3);
                Candles(deco, new Vector3(-hx + 1.2f, 0f, hz - 1.2f), 6, palette.torch, 0.35f);
                break;
            case RoomType.Combate:
                if (plainRoom || r.feature == RoomFeature.Espejos) Rug(deco, new Vector2(r.size.x * 0.55f, r.size.y * 0.5f), palette.rug, palette.rugBorder);
                Banners(deco, r, gaps, palette.banner);
                ThemeProps(deco, r, gaps, plainRoom ? 6 : 3);
                if (theme == FloorTheme.Base || theme == FloorTheme.Galerias)
                {
                    int n = 0;
                    foreach (var other in rooms) { if (other.type == RoomType.Combate) n++; if (other == r) break; }
                    if (n == 2) SecretAlcove(deco, r, gaps);
                }
                break;
            case RoomType.Descanso:
                MerchantStall(deco, new Vector3(0f, 0f, hz - 2.4f));
                Shrine(deco, new Vector3(0f, 0f, -0.8f));
                Rug(deco, new Vector2(4f, r.size.y * 0.7f), palette.rug, palette.rugBorder);
                ThemeProps(deco, r, gaps, 3);
                FloorCandelabra(deco, new Vector3(-hx + 1f, 0f, -hz + 1f), palette.torch);
                break;
            case RoomType.Antesala:
                foreach (float x in new[] { -2.9f, 2.9f }) Brazier(deco, new Vector3(x, 0f, hz - 1.1f), palette.torch, 1f);
                for (int k = 0; k < 3; k++) RuneRing(deco, new Vector3(0f, 0.02f, -hz + 2f + k * 2f), 0.45f, 0.6f, palette.accent, "Runa");
                FloorCandelabra(deco, new Vector3(-hx + 0.8f, 0f, 0f), palette.torch);
                FloorCandelabra(deco, new Vector3(hx - 0.8f, 0f, 0f), palette.torch);
                break;
            case RoomType.Jefe:
            {
                Color boss = Color.Lerp(palette.accent, new Color(1f, 0.25f, 0.2f), 0.4f) * 0.45f;   // GlowMaterial lo multiplica: sin esto se ve blanco
                RuneRing(deco, Vector3.up * 0.02f, 7.4f, 7.8f, boss, "Arena");
                RuneRing(deco, Vector3.up * 0.02f, 3.0f, 3.2f, boss, "ArenaCentro");
                foreach (var c in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
                {
                    var p = new Vector3(c.x * r.size.x * 0.33f, 0f, c.y * r.size.y * 0.3f);
                    ThemePillar(deco, p, 0.75f, h + 1.5f);
                    Brazier(deco, p + Vector3.up * (h + 1.5f), palette.torch, 0.9f, c.y > 0);
                }
                Throne(deco, new Vector3(0f, 0f, hz - 1.6f));
                Banners(deco, r, gaps, palette.banner);
                ThemeProps(deco, r, gaps, 4);
                break;
            }
            case RoomType.Prueba:
                if (r.feature == RoomFeature.Escalinata) ThemeProps(deco, r, gaps, 4);
                else if (r.feature == RoomFeature.Ninguna)
                {
                    // sala de desvío: premio por explorar
                    Rug(deco, new Vector2(r.size.x * 0.5f, r.size.y * 0.45f), palette.rug, palette.rugBorder);
                    ThemeProps(deco, r, gaps, 7);
                    TreasureChestAt(deco, new Vector3(0f, 0f, hz - 1.6f), 18 + floorNumber * 3, 180f);
                    Candles(deco, new Vector3(-1.4f, 0f, hz - 1.2f), 5, palette.torch, 0.3f);
                    Candles(deco, new Vector3(1.4f, 0f, hz - 1.2f), 4, palette.torch, 0.25f);
                    if (theme == FloorTheme.Galerias) WindAt(deco, Vector3.zero, new Vector3(r.size.x, 4f, r.size.y), 2.2f, 1.2f);
                }
                break;
        }
    }

    /// <summary>Pilar con el material del tema y un anillo del color de acento.</summary>
    void ThemePillar(Transform parent, Vector3 pos, float radius, float height)
    {
        var p = NewChild(parent, "Pilar");
        p.localPosition = pos;
        var stone = ThemeStone();
        var shaft = Prim(p, PrimitiveType.Cylinder, "Fuste", new Vector3(0f, height / 2f, 0f), new Vector3(radius * 2f, height / 2f, radius * 2f), stone, true);
        shaft.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        Prim(p, PrimitiveType.Cube, "Base", new Vector3(0f, 0.18f, 0f), new Vector3(radius * 2.7f, 0.36f, radius * 2.7f), stone, false);
        Prim(p, PrimitiveType.Cube, "Capitel", new Vector3(0f, height - 0.15f, 0f), new Vector3(radius * 2.8f, 0.3f, radius * 2.8f), stone, false);
        p.gameObject.AddComponent<OccluderFade>();
        bool gold = theme == FloorTheme.Avaricia || theme == FloorTheme.Soberbia || theme == FloorTheme.Lujuria;
        var ring = Prim(p, PrimitiveType.Cylinder, "Anillo", new Vector3(0f, height * 0.62f, 0f), new Vector3(radius * 2.08f, 0.04f, radius * 2.08f),
                        gold ? BrassMaterial() : GlowMaterial("Glow_" + ColorUtility.ToHtmlStringRGB(palette.accent), palette.accent), false);
        ring.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    /// <summary>Color de la piedra de las tapas de muro según el tema (en Base, la piedra azulada de siempre).</summary>
    Color CapColor()
    {
        switch (theme)
        {
            case FloorTheme.Pereza: return new Color(0.36f, 0.27f, 0.2f);
            case FloorTheme.Gula: return new Color(0.45f, 0.26f, 0.18f);
            case FloorTheme.Avaricia: return new Color(0.42f, 0.36f, 0.18f);
            case FloorTheme.Envidia: return new Color(0.16f, 0.26f, 0.2f);
            case FloorTheme.Lujuria: return new Color(0.62f, 0.42f, 0.5f);
            case FloorTheme.Ira: return new Color(0.2f, 0.15f, 0.14f);
            case FloorTheme.Soberbia: return new Color(0.92f, 0.9f, 0.86f);
            default: return new Color(0.42f, 0.4f, 0.48f);
        }
    }

    Material ThemeStone() => SavedMaterial("Piedra_" + palette.key, () =>
    {
        var m = new Material(StoneMaterial());
        Color c;
        switch (theme)
        {
            case FloorTheme.Soberbia: c = new Color(0.95f, 0.94f, 0.92f); break;
            case FloorTheme.Lujuria: c = new Color(0.85f, 0.7f, 0.75f); break;
            case FloorTheme.Avaricia: c = new Color(0.35f, 0.5f, 0.42f); break;
            case FloorTheme.Envidia: c = new Color(0.3f, 0.45f, 0.35f); break;
            case FloorTheme.Ira: c = new Color(0.28f, 0.22f, 0.22f); break;
            case FloorTheme.Gula: c = new Color(0.7f, 0.55f, 0.45f); break;
            case FloorTheme.Pereza: c = new Color(0.5f, 0.55f, 0.62f); break;
            default: c = new Color(0.62f, 0.6f, 0.68f); break;
        }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", theme == FloorTheme.Soberbia || theme == FloorTheme.Lujuria ? 0.6f : 0.15f);
        return m;
    });

    /// <summary>Busca un lugar pegado a un muro (N, E u O) lejos de las puertas. inward = hacia la sala.</summary>
    bool WallSpot(RoomSpec r, List<Gap> gaps, float inset, float clearance, out Vector3 pos, out Vector3 inward)
    {
        float hx = r.size.x / 2f, hz = r.size.y / 2f;
        for (int tries = 0; tries < 30; tries++)
        {
            int side = drng.Next(3);
            pos = side == 0 ? new Vector3(DR(-hx + 1.6f, hx - 1.6f), 0f, hz - inset)
                : side == 1 ? new Vector3(hx - inset, 0f, DR(-hz + 1.6f, hz - 1.6f))
                            : new Vector3(-hx + inset, 0f, DR(-hz + 1.6f, hz - 1.6f));
            inward = side == 0 ? Vector3.back : side == 1 ? Vector3.left : Vector3.right;
            bool bad = false;
            foreach (var g in gaps)
            {
                Vector3 d = g.side == Side.N ? new Vector3(g.offset, 0f, hz) : g.side == Side.S ? new Vector3(g.offset, 0f, -hz)
                          : g.side == Side.E ? new Vector3(hx, 0f, g.offset) : new Vector3(-hx, 0f, g.offset);
                if (Vector3.Distance(d, pos) < g.width / 2f + clearance) bad = true;
            }
            // tampoco donde van las antorchas (a 1/4 de cada muro)
            float along = side == 0 ? pos.x : pos.z, len = side == 0 ? r.size.x : r.size.y;
            if (Mathf.Abs(Mathf.Abs(along) - len / 4f) < 0.9f) bad = true;
            if (!bad) return true;
        }
        pos = Vector3.zero; inward = Vector3.back;
        return false;
    }

    /// <summary>Muebles del tema repartidos junto a los muros.</summary>
    void ThemeProps(Transform deco, RoomSpec r, List<Gap> gaps, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (!WallSpot(r, gaps, 0.8f, 1.8f, out var pos, out var inward)) continue;
            var t = NewChild(deco, "Mueble");
            t.localPosition = pos;
            t.localRotation = Quaternion.LookRotation(inward);   // +Z local = hacia el centro de la sala
            int v = drng.Next(3);
            switch (theme)
            {
                case FloorTheme.Pereza:
                    if (v == 0) Bookcase(t, 1.6f, 2.4f); else if (v == 1) BookStacks(t, 4); else Armchair(t);
                    break;
                case FloorTheme.Gula:
                    if (v == 0) Barrels(t); else if (v == 1) Cauldron(t); else Sacks(t);
                    break;
                case FloorTheme.Avaricia:
                    if (v == 0) GoldHeap(t, 0.9f); else if (v == 1) GoldBars(t); else Scales(t);
                    break;
                case FloorTheme.Envidia:
                    if (v == 0) Mirror(t, 1.2f, 2.3f); else if (v == 1) Portrait(t); else Curtain(t);
                    break;
                case FloorTheme.Lujuria:
                    if (v == 0) RoseBush(t, 0.8f); else if (v == 1) Cushions(t); else RoseVase(t);
                    break;
                case FloorTheme.Ira:
                    if (v == 0) Anvil(t); else if (v == 1) WeaponRack(t); else BrokenPillar(t);
                    break;
                case FloorTheme.Soberbia:
                    if (v == 0) Statue(t, true); else if (v == 1) GoldVase(t); else Statue(t, false);
                    break;
                case FloorTheme.Galerias:
                    if (v == 0) RopeCoils(t); else if (v == 1) { Crate(t, new Vector3(-0.3f, 0f, 0f), 0.8f); Crate(t, new Vector3(0.45f, 0f, 0.1f), 0.6f); Crate(t, new Vector3(-0.25f, 0.8f, 0.05f), 0.5f); } else Sacks(t);
                    break;
                default:
                    if (v == 0) Barrels(t); else if (v == 1) WeaponRack(t); else { Crate(t, new Vector3(-0.3f, 0f, 0f), 0.8f); Crate(t, new Vector3(0.45f, 0f, 0.1f), 0.6f); }
                    break;
            }
        }
    }

    // ------------------------------------------------------------------ mecánica de cada piso

    /// <summary>Lo especial de cada sala según el tema: estructura (biblioteca, banquete…) y trampas del pecado.</summary>
    void BuildThemeFeature(Transform root, RoomSpec r, List<Gap> gaps, int index, RoomEncounter enc)
    {
        if (theme == FloorTheme.Base) return;
        drng = new System.Random(decorSeed * 53 + index);
        var t = NewChild(root, "Pecado");
        float hx = r.size.x / 2f, hz = r.size.y / 2f;
        bool combat = r.type == RoomType.Combate || r.type == RoomType.Jefe;
        switch (theme)
        {
            case FloorTheme.Pereza:
                if (enc != null && r.type == RoomType.Combate) enc.sleepingEnemies = true;
                if (r.feature == RoomFeature.Biblioteca) BuildLibrary(t, r, gaps);
                else if (combat) { MistAt(t, new Vector3(-hx * 0.45f, 0f, 0f), new Vector3(4.5f, 2f, r.size.y * 0.6f)); MistAt(t, new Vector3(hx * 0.5f, 0f, hz * 0.35f), new Vector3(4f, 2f, 4f)); }
                break;
            case FloorTheme.Gula:
                if (r.feature == RoomFeature.Banquete) BuildBanquet(t, r);
                else if (combat)
                {
                    MawAt(t, new Vector3(-hx * 0.4f, 0f, hz * 0.3f), 0f);
                    MawAt(t, new Vector3(hx * 0.4f, 0f, -hz * 0.25f), 1.6f);
                    if (r.type == RoomType.Jefe) { MawAt(t, new Vector3(0f, 0f, -hz * 0.45f), 0.8f); MawAt(t, new Vector3(-hx * 0.55f, 0f, -hz * 0.4f), 2.4f); MawAt(t, new Vector3(hx * 0.55f, 0f, hz * 0.2f), 3f); }
                    FeastTable(t, new Vector3(-hx + 1.2f, 0f, 0f), Quaternion.Euler(0f, 90f, 0f), 2.4f, 2);
                }
                break;
            case FloorTheme.Avaricia:
                if (r.feature == RoomFeature.Boveda) BuildVault(t, r, root);
                else if (combat)
                    for (int i = 0; i < (r.type == RoomType.Jefe ? 6 : 3); i++)
                        CoinPileAt(t, new Vector3(DR(-hx + 2f, hx - 2f), 0f, DR(-hz + 2f, hz - 2f)), drng.NextDouble() < 0.4, 3 + drng.Next(4));
                break;
            case FloorTheme.Envidia:
                if (enc != null) root.gameObject.AddComponent<EnvyRoom>();
                if (r.feature == RoomFeature.Espejos) BuildMirrorHall(t, r, gaps);
                break;
            case FloorTheme.Lujuria:
                if (r.feature == RoomFeature.Jardin) BuildGarden(t, r);
                else if (combat)
                {
                    VortexAt(t, new Vector3(hx * 0.45f, 0f, hz * 0.2f), 4.2f);
                    if (r.type == RoomType.Jefe) VortexAt(t, new Vector3(-hx * 0.5f, 0f, -hz * 0.3f), 4.5f);
                }
                break;
            case FloorTheme.Ira:
                if (r.feature == RoomFeature.Forja) BuildForge(t, r, gaps);
                if (combat)
                {
                    int n = r.type == RoomType.Jefe ? 6 : r.feature == RoomFeature.Forja ? 5 : 3;
                    for (int i = 0; i < n; i++)
                    {
                        float a = (i + 0.5f) / n * Mathf.PI * 2f + DR(-0.3f, 0.3f);
                        VentAt(t, new Vector3(Mathf.Cos(a) * hx * 0.55f, 0f, Mathf.Sin(a) * hz * 0.5f), i * 0.9f);
                    }
                }
                break;
            case FloorTheme.Soberbia:
                if (r.feature == RoomFeature.Salon) BuildJudgementHall(t, r);
                else if (r.type == RoomType.Jefe) GazeAt(t, new Vector3(0f, 7.5f, 0f), 22f, 0f);
                else if (r.type == RoomType.Combate && index == 1)
                    foreach (float x in new[] { -hx * 0.45f, hx * 0.45f })
                        foreach (float z in new[] { -hz * 0.3f, hz * 0.3f })
                        {
                            var s = NewChild(t, "Estatua"); s.localPosition = new Vector3(x, 0f, z); s.localRotation = Quaternion.Euler(0f, x > 0 ? -90f : 90f, 0f);
                            Statue(s, true);
                        }
                break;
        }
    }

    // ------------------------------------------------------------------ salas especiales

    /// <summary>
    /// PEREZA — Biblioteca de dos pisos: entrepisos a lo largo de los muros este y oeste (cada uno con su escalera),
    /// libreros en hileras abajo y contra los muros arriba, mesas de lectura, niebla de sopor y libros flotando.
    /// Las puertas sur y norte quedan libres.
    /// </summary>
    void BuildLibrary(Transform t, RoomSpec r, List<Gap> gaps)
    {
        float hx = r.size.x / 2f, hz = r.size.y / 2f, top = BibliotecaMezzanine, depth = 3.4f;
        var half = new Vector2(hx, hz);
        float run = top / 0.6f, z0 = -hz + 1f;
        var wood = LitMaterial("MaderaOscura", new Color(0.26f, 0.16f, 0.09f));
        foreach (float sx in new[] { -1f, 1f })
        {
            float xIn = sx * (hx - depth);   // borde interior del entrepiso
            Block(t, sx < 0 ? "EntrepisoO" : "EntrepisoE", new Vector3(sx < 0 ? -hx : hx - depth, 0f, z0 + run), new Vector3(sx < 0 ? -hx + depth : hx, top, hz), half);
            // escalera que sube pegada al muro hasta el entrepiso
            StairRun(t, new Vector3(sx * (hx - 1.1f), 0f, z0), Vector3.forward, run, top, 2f, true);
            // barandal del lado de la sala
            for (float z = z0 + run + 0.4f; z < hz - 0.2f; z += 1.3f)
                Prim(t, PrimitiveType.Cube, "Balaustre", new Vector3(xIn, top + 0.5f, z), new Vector3(0.07f, 1f, 0.07f), wood, false);
            float railLen = hz - (z0 + run);
            Prim(t, PrimitiveType.Cube, "Pasamanos", new Vector3(xIn, top + 1f, (z0 + run + hz) / 2f), new Vector3(0.09f, 0.07f, railLen), wood, false);
            CreateCollider(t, "Collider_Barandal", new Vector3(xIn, top + 0.6f, (z0 + run + hz) / 2f), new Vector3(0.15f, 1.2f, railLen));
            // libreros arriba contra el muro
            for (float z = z0 + run + 1.2f; z < hz - 0.9f; z += 1.9f)
            {
                var b = NewChild(t, "LibreroAlto"); b.localPosition = new Vector3(sx * (hx - 0.3f), top, z); b.localRotation = Quaternion.Euler(0f, sx < 0 ? 90f : -90f, 0f);
                Bookcase(b, 1.7f, Mathf.Min(2.8f, curWall - top - 0.3f));
            }
            Candles(t, new Vector3(sx * (hx - 1.6f), top, hz - 1.2f), 4, palette.torch, 0.25f);
        }
        // hileras de libreros abajo (dejan el pasillo central libre de puerta a puerta)
        float inner = hx - depth - 1.2f;
        foreach (float z in new[] { -hz * 0.4f, -hz * 0.05f, hz * 0.3f })
            foreach (float x in new[] { -inner * 0.62f, inner * 0.62f })
            {
                var b = NewChild(t, "Librero"); b.localPosition = new Vector3(x, 0f, z);
                Bookcase(b, Mathf.Min(3.2f, inner * 0.75f), 2.5f, true);
            }
        foreach (var p in new[] { new Vector3(-inner * 0.6f, 0f, -hz * 0.75f), new Vector3(inner * 0.6f, 0f, hz * 0.62f) })
        { var d = NewChild(t, "MesaLectura"); d.localPosition = p; ReadingTable(d); }
        MistAt(t, new Vector3(0f, 0f, -hz * 0.22f), new Vector3(inner * 2f, 2f, 3f));
        MistAt(t, new Vector3(0f, 0f, hz * 0.12f), new Vector3(inner * 2f, 2f, 2.6f));
        FloatingBooks(t, new Vector3(0f, top + 1.5f, 0f), new Vector3(inner * 2f, 2f, r.size.y - 6f), 16);
        Hint(t.parent, r, "LA BIBLIOTECA DORMIDA", "La niebla da sueño: crúzala con el impulso (Shift). Arriba, en los entrepisos, alguien dejó algo");
    }

    /// <summary>GULA — Banquete: dos mesas larguísimas llenas de comida (cura pero empacha) y fauces en el suelo.</summary>
    void BuildBanquet(Transform t, RoomSpec r)
    {
        float hx = r.size.x / 2f, hz = r.size.y / 2f;
        FeastTable(t, new Vector3(-hx * 0.42f, 0f, 0f), Quaternion.identity, r.size.y * 0.62f, 5);
        FeastTable(t, new Vector3(hx * 0.42f, 0f, 0f), Quaternion.identity, r.size.y * 0.62f, 5);
        MawAt(t, new Vector3(0f, 0f, hz * 0.35f), 0f);
        MawAt(t, new Vector3(0f, 0f, -hz * 0.3f), 1.4f);
        MawAt(t, new Vector3(-hx * 0.75f, 0f, -hz * 0.6f), 2.2f);
        MawAt(t, new Vector3(hx * 0.75f, 0f, hz * 0.6f), 0.7f);
        for (int i = 0; i < 4; i++) HangingHam(t, new Vector3(DR(-hx + 2f, hx - 2f), wallHeight + 0.4f, DR(-hz + 2f, hz - 2f)));
        Hint(t.parent, r, "EL BANQUETE", "La comida cura… pero te pone lenta. Las fauces muerden cuando se iluminan");
    }

    /// <summary>AVARICIA — Bóveda: montañas de oro, monedas sueltas (algunas malditas), dos mímicos y un cofre de verdad.</summary>
    void BuildVault(Transform t, RoomSpec r, Transform roomRoot)
    {
        float hx = r.size.x / 2f, hz = r.size.y / 2f;
        foreach (var p in new[] { new Vector3(-hx + 2f, 0f, hz - 2f), new Vector3(hx - 2.2f, 0f, hz - 2.4f), new Vector3(-hx + 1.8f, 0f, -hz + 2.2f) })
        { var g = NewChild(t, "MontanaOro"); g.localPosition = p; GoldHeap(g, 1.8f); }
        for (int i = 0; i < 9; i++)
        {
            float a = i / 9f * Mathf.PI * 2f;
            CoinPileAt(t, new Vector3(Mathf.Cos(a) * hx * 0.55f, 0f, Mathf.Sin(a) * hz * 0.5f), i % 3 == 1, 3 + drng.Next(4));
        }
        // pedestales con tres cofres: dos muerden
        float[] xs = { -3f, 0f, 3f };
        int real = drng.Next(3);
        for (int i = 0; i < 3; i++)
        {
            var ped = NewChild(t, "Pedestal"); ped.localPosition = new Vector3(xs[i], 0f, hz * 0.15f);
            Prim(ped, PrimitiveType.Cube, "Base", new Vector3(0f, 0.2f, 0f), new Vector3(1.5f, 0.4f, 1.1f), ThemeStone(), true);
            if (i == real) TreasureChestAt(ped, new Vector3(0f, 0.4f, 0f), 35);
            else MimicAt(ped, new Vector3(0f, 0.4f, 0f), roomRoot, r.size);
        }
        Hint(t.parent, r, "LA BÓVEDA", "No todo lo que brilla es tuyo. Los cofres que respiran muerden");
    }

    /// <summary>ENVIDIA — Galería de espejos: espejos altos en los muros y vitrinas con objetos ajenos.</summary>
    void BuildMirrorHall(Transform t, RoomSpec r, List<Gap> gaps)
    {
        float hx = r.size.x / 2f, hz = r.size.y / 2f;
        for (float x = -hx + 2f; x < hx - 1.5f; x += 2.6f)
        {
            if (Mathf.Abs(Mathf.Abs(x) - r.size.x / 4f) < 0.9f) continue;   // deja lugar a las lámparas
            bool door = false; foreach (var g in gaps) if (g.side == Side.N && Mathf.Abs(g.offset - x) < g.width / 2f + 1f) door = true;
            if (door) continue;
            var m = NewChild(t, "Espejo"); m.localPosition = new Vector3(x, 0f, hz - 0.12f); m.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Mirror(m, 1.3f, 2.5f);
        }
        foreach (var p in new[] { new Vector3(-hx * 0.45f, 0f, -hz * 0.2f), new Vector3(hx * 0.45f, 0f, -hz * 0.2f), new Vector3(0f, 0f, hz * 0.25f) })
        { var v = NewChild(t, "Vitrina"); v.localPosition = p; Showcase(v); }
        Hint(t.parent, r, "LA GALERÍA DE LOS ESPEJOS", "Otra tú te sigue con retraso. No la dejes alcanzarte: sigue moviéndote");
    }

    /// <summary>LUJURIA — Jardín: rosales, una fuente rosa al centro y dos remolinos de pétalos con espinas.</summary>
    void BuildGarden(Transform t, RoomSpec r)
    {
        float hx = r.size.x / 2f, hz = r.size.y / 2f;
        for (int i = 0; i < 14; i++)
        {
            var b = NewChild(t, "Rosal");
            float x = DR(-hx + 1f, hx - 1f), z = DR(-hz + 1f, hz - 1f);
            if (Mathf.Abs(x) < 3f && Mathf.Abs(z) < 3f) continue;
            b.localPosition = new Vector3(x, 0f, z);
            RoseBush(b, DR(0.6f, 1.1f));
        }
        var f = NewChild(t, "Fuente"); f.localPosition = Vector3.zero;
        Prim(f, PrimitiveType.Cylinder, "Pila", new Vector3(0f, 0.3f, 0f), new Vector3(3f, 0.3f, 3f), ThemeStone(), true);
        Prim(f, PrimitiveType.Cylinder, "Agua", new Vector3(0f, 0.55f, 0f), new Vector3(2.7f, 0.02f, 2.7f), GlassMaterial(new Color(1f, 0.5f, 0.75f)), false);
        Prim(f, PrimitiveType.Cylinder, "Columna", new Vector3(0f, 1.1f, 0f), new Vector3(0.3f, 0.6f, 0.3f), ThemeStone(), false);
        Prim(f, PrimitiveType.Sphere, "Corazon", new Vector3(0f, 1.9f, 0f), new Vector3(0.55f, 0.5f, 0.4f), GlowMaterial("Glow_" + ColorUtility.ToHtmlStringRGB(palette.accent), palette.accent), false);
        Motes(f, new Vector3(0f, 1.9f, 0f), new Vector3(0.4f, 0.2f, 0.4f), palette.motes, 14f, -0.4f, 1.5f);
        AddLight(f, new Vector3(0f, 2.2f, 0f), palette.accent, 7f, 6f, false);
        VortexAt(t, new Vector3(-hx * 0.55f, 0f, hz * 0.35f), 4.5f);
        VortexAt(t, new Vector3(hx * 0.55f, 0f, -hz * 0.3f), 4.5f);
        Hint(t.parent, r, "EL JARDÍN CARMESÍ", "Los remolinos te jalan hacia sus espinas: escapa con el impulso");
    }

    /// <summary>IRA — Forja: yunques, cadenas, canales de lava (las rejillas escupen fuego).</summary>
    void BuildForge(Transform t, RoomSpec r, List<Gap> gaps)
    {
        float hx = r.size.x / 2f, hz = r.size.y / 2f;
        // canales de lava junto a los muros E y O (bordillo con lava adentro), cortados donde hay puerta
        foreach (float x in new[] { -hx + 1.2f, hx - 1.2f })
        {
            Side side = x < 0f ? Side.W : Side.E;
            var segments = new List<Vector2> { new Vector2(-hz + 1f, hz - 1f) };
            foreach (var g in gaps)
            {
                if (g.side != side) continue;
                var next = new List<Vector2>();
                foreach (var sgm in segments)
                {
                    float a = g.offset - g.width / 2f - 0.6f, b = g.offset + g.width / 2f + 0.6f;
                    if (a > sgm.x + 0.5f) next.Add(new Vector2(sgm.x, Mathf.Min(a, sgm.y)));
                    if (b < sgm.y - 0.5f) next.Add(new Vector2(Mathf.Max(b, sgm.x), sgm.y));
                }
                segments = next;
            }
            foreach (var sgm in segments)
            {
                float len = sgm.y - sgm.x, mid = (sgm.x + sgm.y) / 2f;
                var ch = NewChild(t, "CanalLava"); ch.localPosition = new Vector3(x, 0f, mid);
                Prim(ch, PrimitiveType.Cube, "Bordillo", new Vector3(0f, 0.15f, 0f), new Vector3(1.2f, 0.3f, len), ThemeStone(), true);
                var lava = Prim(ch, PrimitiveType.Cube, "Lava", new Vector3(0f, 0.29f, 0f), new Vector3(0.8f, 0.04f, len - 0.4f), LavaMaterial(), false);
                lava.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Motes(ch, new Vector3(0f, 0.4f, 0f), new Vector3(0.8f, 0.1f, len - 0.4f), new Color(1f, 0.5f, 0.15f), 2f + len, 1f, 1.5f);
                AddLight(ch, new Vector3(0f, 0.8f, 0f), new Color(1f, 0.4f, 0.1f), Mathf.Max(5f, len * 0.7f), 5f, false);
            }
        }
        foreach (var p in new[] { new Vector3(-hx * 0.4f, 0f, hz * 0.55f), new Vector3(hx * 0.4f, 0f, -hz * 0.55f) })
        { var a = NewChild(t, "Yunque"); a.localPosition = p; Anvil(a); }
        for (int i = 0; i < 5; i++) HangingChain(t, new Vector3(DR(-hx + 2f, hx - 2f), wallHeight + 1f, DR(-hz + 2f, hz - 2f)));
        Hint(t.parent, r, "LA FORJA ENCADENADA", "Las rejillas se ponen al rojo antes de escupir fuego");
    }

    /// <summary>SOBERBIA — Salón del Juicio: estatuas y columnas (tapan la mirada) y el Ojo flotando en lo alto.</summary>
    void BuildJudgementHall(Transform t, RoomSpec r)
    {
        float hx = r.size.x / 2f, hz = r.size.y / 2f;
        // alfombra roja de la puerta sur a la salida este
        Rug(t, new Vector2(3f, r.size.y - 1f), palette.rug, palette.rugBorder);
        var cover = new[] { new Vector3(-hx * 0.5f, 0f, -hz * 0.45f), new Vector3(hx * 0.1f, 0f, -hz * 0.2f), new Vector3(-hx * 0.2f, 0f, hz * 0.15f),
                            new Vector3(hx * 0.5f, 0f, hz * 0.05f), new Vector3(-hx * 0.6f, 0f, hz * 0.55f), new Vector3(hx * 0.3f, 0f, hz * 0.6f) };
        for (int i = 0; i < cover.Length; i++)
        {
            if (i % 2 == 0) ThemePillar(t, cover[i], 0.6f, wallHeight + 2f);
            else { var s = NewChild(t, "Estatua"); s.localPosition = cover[i]; s.localRotation = Quaternion.Euler(0f, DR(0f, 360f), 0f); Statue(s, false); }
        }
        GazeAt(t, new Vector3(0f, 7f, 0f), 20f, 90f);
        Hint(t.parent, r, "EL SALÓN DEL JUICIO", "El Ojo te quema si te ve. Avanza escondida tras columnas y estatuas");
    }

    // ------------------------------------------------------------------ trampas

    void MistAt(Transform parent, Vector3 pos, Vector3 size)
    {
        var m = NewChild(parent, "NieblaSopor");
        m.localPosition = pos;
        var mist = m.gameObject.AddComponent<SlothMist>();
        mist.size = size;
        var mat = FxMaterial();
        if (mat == null) return;
        var go = new GameObject("Bruma");
        go.transform.SetParent(m, false);
        go.transform.localPosition = Vector3.up * 0.5f;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true; main.prewarm = true; main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(1.6f, 2.8f);
        main.startColor = new Color(0.55f, 0.7f, 1f, 0.09f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.maxParticles = 60;
        var em = ps.emission; em.rateOverTime = size.x * size.z * 0.35f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(size.x, 0.4f, size.z);
        var vel = ps.velocityOverLifetime; vel.enabled = true;
        vel.x = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f); vel.y = new ParticleSystem.MinMaxCurve(0.01f, 0.05f); vel.z = new ParticleSystem.MinMaxCurve(-0.08f, 0.08f);
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = mat; rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
    }

    void MawAt(Transform parent, Vector3 pos, float offset)
    {
        var m = NewChild(parent, "Fauces");
        m.localPosition = pos;
        var ringGo = new GameObject("Labio");
        ringGo.transform.SetParent(m, false);
        ringGo.transform.localPosition = Vector3.up * 0.02f;
        ringGo.AddComponent<MeshFilter>().sharedMesh = Annulus(1.05f, 1.35f, 40);
        var ring = ringGo.AddComponent<MeshRenderer>();
        ring.sharedMaterial = GlowMaterial("Glow_Fauces", new Color(0.5f, 0.12f, 0.08f));
        ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        CreateFace(m, "Garganta", Vector3.up * 0.015f, Vector3.up, 2.1f, 2.1f, LitMaterial("Garganta", new Color(0.12f, 0.02f, 0.03f)), 1f, 1f);
        var teeth = NewChild(m, "Colmillos");
        teeth.localPosition = Vector3.down * 0.5f;
        var bone = LitMaterial("Hueso", new Color(0.92f, 0.88f, 0.78f));
        for (int i = 0; i < 12; i++)
        {
            float a = i / 12f * Mathf.PI * 2f;
            var tooth = Prim(teeth, PrimitiveType.Cube, "Colmillo", new Vector3(Mathf.Cos(a) * 1.05f, 0.25f, Mathf.Sin(a) * 1.05f), new Vector3(0.14f, 0.5f, 0.14f), bone, false);
            tooth.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 20f, 45f, -Mathf.Cos(a) * 20f);
        }
        var trap = m.gameObject.AddComponent<MawTrap>();
        trap.teeth = teeth; trap.ring = ring; trap.offset = offset;
    }

    void VentAt(Transform parent, Vector3 pos, float offset)
    {
        var v = NewChild(parent, "Respiradero");
        v.localPosition = pos;
        var grate = Prim(v, PrimitiveType.Cylinder, "Rejilla", new Vector3(0f, 0.02f, 0f), new Vector3(1.6f, 0.02f, 1.6f), GlowMaterial("Glow_Rejilla", new Color(0.25f, 0.06f, 0.03f)), false);
        grate.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        for (int i = -2; i <= 2; i++)
            Prim(v, PrimitiveType.Cube, "Barra", new Vector3(i * 0.28f, 0.05f, 0f), new Vector3(0.08f, 0.05f, 1.5f), MetalMaterial(), false);
        // columna de fuego (apagada hasta que estalla)
        var mat = FxMaterial();
        ParticleSystem ps = null;
        if (mat != null)
        {
            var go = new GameObject("Columna");
            go.transform.SetParent(v, false);
            ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.maxParticles = 200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 120f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 8f; sh.radius = 0.5f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.6f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.2f)));
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.95f, 0.6f), 0f), new GradientColorKey(new Color(1f, 0.45f, 0.1f), 0.4f), new GradientColorKey(new Color(0.5f, 0.08f, 0.02f), 1f) },
                      new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.7f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = mat; rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var lt = AddLight(v, Vector3.up * 1.2f, new Color(1f, 0.45f, 0.15f), 6f, 0f, false);
        var vent = v.gameObject.AddComponent<WrathVent>();
        vent.fire = ps; vent.grate = grate.GetComponent<Renderer>(); vent.glow = lt; vent.offset = offset;
    }

    void VortexAt(Transform parent, Vector3 pos, float radius)
    {
        var v = NewChild(parent, "Remolino");
        v.localPosition = pos;
        RuneRing(v, Vector3.up * 0.02f, radius - 0.15f, radius, new Color(0.5f, 0.12f, 0.32f), "Borde");
        var spin = NewChild(v, "Giro");
        var thorn = LitMaterial("Espina", new Color(0.18f, 0.25f, 0.12f));
        for (int i = 0; i < 10; i++)
        {
            float a = i / 10f * Mathf.PI * 2f;
            var s = Prim(spin, PrimitiveType.Cube, "Espina", new Vector3(Mathf.Cos(a) * 0.9f, 0.3f, Mathf.Sin(a) * 0.9f), new Vector3(0.1f, 0.6f, 0.1f), thorn, false);
            s.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 30f, 0f, -Mathf.Cos(a) * 30f);
        }
        Prim(spin, PrimitiveType.Sphere, "Flor", new Vector3(0f, 0.4f, 0f), new Vector3(0.6f, 0.4f, 0.6f), GlowMaterial("Glow_" + ColorUtility.ToHtmlStringRGB(palette.accent), palette.accent), false);
        // pétalos girando hacia el centro
        var mat = FxMaterial();
        if (mat != null)
        {
            var go = new GameObject("Petalos");
            go.transform.SetParent(v, false);
            go.transform.localPosition = Vector3.up * 0.4f;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true; main.prewarm = true; main.playOnAwake = true;
            main.startLifetime = 2.5f; main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.3f, 0.5f), new Color(1f, 0.7f, 0.85f));
            main.maxParticles = 150;
            var em = ps.emission; em.rateOverTime = 45f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = radius; sh.radiusThickness = 0.1f; sh.rotation = new Vector3(90f, 0f, 0f);
            var orb = ps.velocityOverLifetime; orb.enabled = true; orb.space = ParticleSystemSimulationSpace.Local;
            orb.orbitalY = new ParticleSystem.MinMaxCurve(2.5f); orb.radial = new ParticleSystem.MinMaxCurve(-radius / 2.6f);
            orb.x = new ParticleSystem.MinMaxCurve(0f, 0f); orb.y = new ParticleSystem.MinMaxCurve(0.05f, 0.3f); orb.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.sharedMaterial = mat; rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ps.Play();
        }
        var lv = v.gameObject.AddComponent<LustVortex>();
        lv.radius = radius; lv.spin = spin;
    }

    void GazeAt(Transform parent, Vector3 pos, float range, float yaw)
    {
        var g = NewChild(parent, "OjoQueJuzga");
        g.localPosition = pos;
        g.localRotation = Quaternion.Euler(0f, yaw, 0f);
        var eye = NewChild(g, "Ojo");
        var gold = BrassMaterial();
        Prim(eye, PrimitiveType.Sphere, "Globo", Vector3.zero, Vector3.one * 1.1f, LitMaterial("Esclerotica", new Color(0.95f, 0.93f, 0.88f)), false);
        Prim(eye, PrimitiveType.Sphere, "Iris", new Vector3(0f, 0f, 0.42f), new Vector3(0.55f, 0.55f, 0.3f), GlowMaterial("Glow_Iris", new Color(1f, 0.75f, 0.25f)), false);
        Prim(eye, PrimitiveType.Sphere, "Pupila", new Vector3(0f, 0f, 0.53f), new Vector3(0.22f, 0.3f, 0.12f), LitMaterial("Pupila", new Color(0.02f, 0.02f, 0.02f)), false);
        for (int i = 0; i < 8; i++)   // corona de rayos dorados
        {
            float a = i / 8f * Mathf.PI * 2f;
            var ray = Prim(g, PrimitiveType.Cube, "Rayo", new Vector3(Mathf.Cos(a) * 0.95f, Mathf.Sin(a) * 0.95f, -0.1f), new Vector3(0.1f, 0.5f, 0.05f), gold, false);
            ray.transform.localRotation = Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg - 90f);
        }
        foreach (var mr in g.GetComponentsInChildren<MeshRenderer>()) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var spot = NewChild(g, "Mirada").gameObject.AddComponent<Light>();
        spot.type = LightType.Spot; spot.spotAngle = 36f; spot.innerSpotAngle = 20f; spot.range = range; spot.intensity = 60f;
        spot.color = new Color(1f, 0.9f, 0.6f); spot.shadows = LightShadows.Soft;
        var beam = NewChild(g, "Haz");
        var cone = new GameObject("Cono");
        cone.transform.SetParent(beam, false);
        float reach = pos.y / Mathf.Sin(52f * Mathf.Deg2Rad);   // del ojo al suelo con la inclinación de la mirada
        cone.AddComponent<MeshFilter>().sharedMesh = ConeMesh(Mathf.Tan(16f * Mathf.Deg2Rad) * reach, reach, 24);
        var cr = cone.AddComponent<MeshRenderer>();
        cr.sharedMaterial = GazeMaterial(); cr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        AddLight(g, Vector3.zero, new Color(1f, 0.85f, 0.5f), 6f, 6f, false);
        var gaze = g.gameObject.AddComponent<PrideGaze>();
        gaze.eye = eye; gaze.spot = spot; gaze.beam = beam; gaze.range = range;
    }

    Material LavaMaterial() => SavedMaterial("Lava", () =>
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
        m.SetColor("_BaseColor", new Color(1.6f, 0.38f, 0.04f));
        return m;
    });

    /// <summary>Superficie de lava con textura (magma y costra), sin luz y en HDR para que brille con el Bloom.</summary>
    Material LavaSurfaceMaterial() => SavedMaterial("LavaSuperficie", () =>
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture"));
#if UNITY_EDITOR
        var tex = LoadThemeTexture("lava_albedo", UnityEditor.TextureImporterType.Default, true);
        if (tex != null) m.SetTexture("_BaseMap", tex);
#endif
        m.SetColor("_BaseColor", new Color(1.5f, 1.25f, 1.1f));
        return m;
    });

    Material GazeMaterial() => SavedMaterial("FX_Mirada", () =>
    {
        var sh = Shader.Find("Abby/Particle");
        var m = new Material(sh != null ? sh : Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", new Color(1f, 0.85f, 0.5f, 0.07f));
        if (sh != null)
        {
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        }
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return m;
    });

    /// <summary>Cono abierto a lo largo de +Z (vértice en el origen) para el haz de luz.</summary>
    static Mesh ConeMesh(float radius, float length, int segs)
    {
        var v = new List<Vector3> { Vector3.zero }; var t = new List<int>();
        for (int i = 0; i <= segs; i++) { float a = i / (float)segs * Mathf.PI * 2f; v.Add(new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, length)); }
        for (int i = 1; i <= segs; i++) { t.AddRange(new[] { 0, i, i + 1 }); t.AddRange(new[] { 0, i + 1, i }); }   // las dos caras
        var m = new Mesh { name = "Cono" };
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    void CoinPileAt(Transform parent, Vector3 pos, bool cursed, int fragments)
    {
        var c = NewChild(parent, cursed ? "Monedas" : "Monedas");   // mismo nombre: que no se delate en el editor tampoco
        c.localPosition = pos;
        var gold = cursed ? SavedMaterial("LatonRojizo", () => { var m = new Material(BrassMaterial()); m.SetColor("_BaseColor", new Color(0.8f, 0.52f, 0.3f)); return m; }) : BrassMaterial();
        for (int i = 0; i < 9; i++)
        {
            float a = i * 2.39996f, d = 0.32f * Mathf.Sqrt((i + 0.5f) / 9f);
            var coin = Prim(c, PrimitiveType.Cylinder, "Moneda", new Vector3(Mathf.Cos(a) * d, 0.02f + (i % 3) * 0.03f, Mathf.Sin(a) * d), new Vector3(0.16f, 0.012f, 0.16f), gold, false);
            coin.transform.localRotation = Quaternion.Euler(DR(-15f, 15f), 0f, DR(-15f, 15f));
        }
        Prim(c, PrimitiveType.Sphere, "Monton", new Vector3(0f, 0.04f, 0f), new Vector3(0.45f, 0.14f, 0.45f), gold, false);
        Motes(c, Vector3.up * 0.2f, new Vector3(0.4f, 0.1f, 0.4f), new Color(1f, 0.85f, 0.4f), 2f, 0.2f, 1f);
        var pile = c.gameObject.AddComponent<CoinPile>();
        pile.cursed = cursed; pile.fragments = fragments;
    }

    void MimicAt(Transform parent, Vector3 pos, Transform roomRoot, Vector2 roomSize)
    {
        var chest = NewChild(parent, "Cofre");
        chest.localPosition = pos;
        var wood = LitMaterial("Madera", new Color(0.42f, 0.27f, 0.15f));
        Prim(chest, PrimitiveType.Cube, "Caja", new Vector3(0f, 0.3f, 0f), new Vector3(1f, 0.6f, 0.65f), wood, true);
        var lid = NewChild(chest, "TapaPivote");
        lid.localPosition = new Vector3(0f, 0.6f, -0.32f);
        Prim(lid, PrimitiveType.Cube, "Tapa", new Vector3(0f, 0.1f, 0.32f), new Vector3(1.04f, 0.2f, 0.68f), wood, false);
        var bone = LitMaterial("Hueso", new Color(0.92f, 0.88f, 0.78f));
        for (int i = 0; i < 6; i++)   // dientes: escondidos bajo la tapa hasta que se abre
        {
            Prim(lid, PrimitiveType.Cube, "Diente", new Vector3(-0.4f + i * 0.16f, -0.05f, 0.62f), new Vector3(0.07f, 0.14f, 0.06f), bone, false);
            Prim(chest, PrimitiveType.Cube, "Diente", new Vector3(-0.4f + i * 0.16f + 0.08f, 0.62f, 0.28f), new Vector3(0.07f, 0.14f, 0.06f), bone, false);
        }
        Prim(chest, PrimitiveType.Cube, "Herraje", new Vector3(0f, 0.3f, 0f), new Vector3(1.04f, 0.1f, 0.68f), GlowMaterial("Glow_Oro", new Color(1f, 0.75f, 0.35f)), false);
        var lt = NewChild(chest, "Brillo").gameObject.AddComponent<Light>();
        lt.transform.localPosition = new Vector3(0f, 1f, 0f);
        lt.type = LightType.Point; lt.range = 4f; lt.intensity = 3f; lt.color = new Color(1f, 0.8f, 0.4f); lt.shadows = LightShadows.None;
        var m = chest.gameObject.AddComponent<MimicChest>();
        m.lid = lid; m.roomRoot = roomRoot; m.roomSize = roomSize;
    }

    // ------------------------------------------------------------------ muebles

    void Bookcase(Transform t, float width, float height, bool twoSided = false)
    {
        var wood = LitMaterial("MaderaOscura", new Color(0.26f, 0.16f, 0.09f));
        float d = twoSided ? 0.7f : 0.45f;
        Prim(t, PrimitiveType.Cube, "Mueble", new Vector3(0f, height / 2f, 0f), new Vector3(width, height, d), wood, true);
        // frente(s) de libros con la textura del librero
        var books = wallMaterial;
        CreateFace(t, "Libros", new Vector3(0f, height / 2f - 0.05f, d / 2f + 0.005f), Vector3.forward, width - 0.12f, height - 0.2f, books, 1f / 2.5f, 1f / 2.5f);
        if (twoSided) CreateFace(t, "Libros2", new Vector3(0f, height / 2f - 0.05f, -d / 2f - 0.005f), Vector3.back, width - 0.12f, height - 0.2f, books, 1f / 2.5f, 1f / 2.5f);
        Prim(t, PrimitiveType.Cube, "Cornisa", new Vector3(0f, height + 0.05f, 0f), new Vector3(width + 0.1f, 0.1f, d + 0.08f), wood, false);
        t.gameObject.AddComponent<OccluderFade>();
    }

    void BookStacks(Transform t, int stacks)
    {
        Color[] cols = { new Color(0.4f, 0.15f, 0.14f), new Color(0.18f, 0.28f, 0.22f), new Color(0.2f, 0.22f, 0.38f), new Color(0.48f, 0.38f, 0.2f) };
        for (int s = 0; s < stacks; s++)
        {
            var p = new Vector3(DR(-0.7f, 0.7f), 0f, DR(-0.3f, 0.3f));
            int n = 2 + drng.Next(5);
            float y = 0f;
            for (int i = 0; i < n; i++)
            {
                float hb = DR(0.05f, 0.09f);
                var b = Prim(t, PrimitiveType.Cube, "Libro", p + new Vector3(0f, y + hb / 2f, 0f), new Vector3(DR(0.28f, 0.4f), hb, DR(0.2f, 0.28f)), LitMaterial("Tela_" + ColorUtility.ToHtmlStringRGB(cols[i % cols.Length]), cols[(i + s) % cols.Length]), false);
                b.transform.localRotation = Quaternion.Euler(0f, DR(-25f, 25f), 0f);
                y += hb;
            }
        }
        Candles(t, new Vector3(0.6f, 0f, 0.2f), 2, palette.torch, 0.08f, false);
    }

    void Armchair(Transform t)
    {
        var fabric = LitMaterial("Tela_" + ColorUtility.ToHtmlStringRGB(palette.rug), palette.rug);
        Prim(t, PrimitiveType.Cube, "Asiento", new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.35f, 0.8f), fabric, true);
        Prim(t, PrimitiveType.Cube, "Respaldo", new Vector3(0f, 0.8f, -0.33f), new Vector3(0.9f, 0.75f, 0.18f), fabric, false);
        foreach (float x in new[] { -0.45f, 0.45f }) Prim(t, PrimitiveType.Cube, "Brazo", new Vector3(x, 0.55f, 0f), new Vector3(0.16f, 0.3f, 0.8f), fabric, false);
        Prim(t, PrimitiveType.Sphere, "Cojin", new Vector3(0.1f, 0.55f, 0.05f), new Vector3(0.4f, 0.15f, 0.35f), LitMaterial("Tela_" + ColorUtility.ToHtmlStringRGB(palette.rugBorder), palette.rugBorder), false);
    }

    void ReadingTable(Transform t)
    {
        var wood = LitMaterial("MaderaOscura", new Color(0.26f, 0.16f, 0.09f));
        Prim(t, PrimitiveType.Cube, "Tablero", new Vector3(0f, 0.8f, 0f), new Vector3(2f, 0.08f, 1f), wood, true);
        foreach (float x in new[] { -0.9f, 0.9f }) foreach (float z in new[] { -0.4f, 0.4f })
            Prim(t, PrimitiveType.Cube, "Pata", new Vector3(x, 0.4f, z), new Vector3(0.08f, 0.8f, 0.08f), wood, false);
        var paper = LitMaterial("Papel", new Color(0.9f, 0.84f, 0.7f));
        Prim(t, PrimitiveType.Cube, "LibroAbierto", new Vector3(-0.4f, 0.86f, 0f), new Vector3(0.55f, 0.03f, 0.38f), paper, false);
        Candles(t, new Vector3(0.6f, 0.84f, 0.1f), 3, palette.torch, 0.12f);
    }

    void FloatingBooks(Transform parent, Vector3 pos, Vector3 box, int count)
    {
        var root = NewChild(parent, "LibrosFlotando");
        root.localPosition = pos;
        for (int i = 0; i < count; i++)
        {
            var b = NewChild(root, "Libro");
            b.localPosition = new Vector3(DR(-box.x / 2f, box.x / 2f), DR(-box.y / 2f, box.y / 2f), DR(-box.z / 2f, box.z / 2f));
            b.localRotation = Quaternion.Euler(DR(-30f, 30f), DR(0f, 360f), DR(-30f, 30f));
            Prim(b, PrimitiveType.Cube, "Tapa", Vector3.zero, new Vector3(0.35f, 0.06f, 0.26f), LitMaterial("Tela_" + ColorUtility.ToHtmlStringRGB(palette.banner), palette.banner), false);
            var bob = b.gameObject.AddComponent<FloatingBob>();
            bob.amplitude = DR(0.1f, 0.3f); bob.period = DR(3f, 6f); bob.phase = DR(0f, 6f); bob.spin = DR(-20f, 20f);
        }
    }

    void FeastTable(Transform parent, Vector3 pos, Quaternion rot, float length, int foods)
    {
        var t = NewChild(parent, "MesaBanquete");
        t.localPosition = pos; t.localRotation = rot;
        var wood = LitMaterial("Madera", new Color(0.42f, 0.27f, 0.15f));
        Prim(t, PrimitiveType.Cube, "Tablero", new Vector3(0f, 0.85f, 0f), new Vector3(1.4f, 0.1f, length), wood, true);
        Prim(t, PrimitiveType.Cube, "Mantel", new Vector3(0f, 0.905f, 0f), new Vector3(1.2f, 0.01f, length - 0.2f), LitMaterial("Tela_" + ColorUtility.ToHtmlStringRGB(palette.rug), palette.rug), false);
        foreach (float z in new[] { -length / 2f + 0.3f, length / 2f - 0.3f }) foreach (float x in new[] { -0.6f, 0.6f })
            Prim(t, PrimitiveType.Cube, "Pata", new Vector3(x, 0.42f, z), new Vector3(0.1f, 0.84f, 0.1f), wood, false);
        var plate = LitMaterial("Plato", new Color(0.85f, 0.85f, 0.8f));
        for (int i = 0; i < Mathf.Max(3, (int)(length / 0.9f)); i++)
        {
            float z = -length / 2f + 0.5f + i * 0.9f;
            if (z > length / 2f - 0.4f) break;
            Prim(t, PrimitiveType.Cylinder, "Plato", new Vector3(DR(-0.35f, 0.35f), 0.92f, z), new Vector3(0.3f, 0.01f, 0.3f), plate, false);
            if (i % 2 == 0) Prim(t, PrimitiveType.Cylinder, "Copa", new Vector3(DR(-0.4f, 0.4f), 1f, z + 0.25f), new Vector3(0.08f, 0.09f, 0.08f), BrassMaterial(), false);
        }
        Candles(t, new Vector3(0f, 0.9f, 0f), 3, palette.torch, 0.15f);
        // comida que se puede comer (en los bordes de la mesa, al alcance)
        for (int i = 0; i < foods; i++)
        {
            float z = Mathf.Lerp(-length / 2f + 0.6f, length / 2f - 0.6f, foods > 1 ? i / (float)(foods - 1) : 0.5f);
            FoodAt(t, new Vector3(i % 2 == 0 ? -0.5f : 0.5f, 0.92f, z), i);
        }
    }

    void FoodAt(Transform parent, Vector3 pos, int kind)
    {
        var f = NewChild(parent, "Comida");
        f.localPosition = pos;
        switch (kind % 4)
        {
            case 0:   // pierna asada
                Prim(f, PrimitiveType.Sphere, "Carne", new Vector3(0f, 0.08f, 0f), new Vector3(0.3f, 0.18f, 0.2f), LitMaterial("Asado", new Color(0.55f, 0.25f, 0.1f)), false);
                Prim(f, PrimitiveType.Cylinder, "Hueso", new Vector3(0.18f, 0.08f, 0f), new Vector3(0.05f, 0.08f, 0.05f), LitMaterial("Hueso", new Color(0.92f, 0.88f, 0.78f)), false).transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                break;
            case 1:   // pan
                Prim(f, PrimitiveType.Sphere, "Pan", new Vector3(0f, 0.07f, 0f), new Vector3(0.32f, 0.14f, 0.18f), LitMaterial("Pan", new Color(0.8f, 0.55f, 0.3f)), false);
                break;
            case 2:   // manzanas
                for (int i = 0; i < 3; i++) Prim(f, PrimitiveType.Sphere, "Manzana", new Vector3(i * 0.1f - 0.1f, 0.06f, (i % 2) * 0.08f), Vector3.one * 0.11f, LitMaterial("Manzana", new Color(0.75f, 0.1f, 0.1f)), false);
                break;
            default:  // pastel
                Prim(f, PrimitiveType.Cylinder, "Pastel", new Vector3(0f, 0.08f, 0f), new Vector3(0.3f, 0.08f, 0.3f), LitMaterial("Betun", new Color(1f, 0.85f, 0.9f)), false);
                break;
        }
        f.gameObject.AddComponent<FeastFood>();
    }

    void HangingHam(Transform parent, Vector3 pos)
    {
        var h = NewChild(parent, "Jamon");
        h.localPosition = pos;
        Prim(h, PrimitiveType.Cube, "Cuerda", new Vector3(0f, 0.6f, 0f), new Vector3(0.03f, 1.2f, 0.03f), LitMaterial("Cuerda", new Color(0.6f, 0.5f, 0.35f)), false);
        Prim(h, PrimitiveType.Sphere, "Pieza", Vector3.zero, new Vector3(0.35f, 0.6f, 0.3f), LitMaterial("Asado", new Color(0.55f, 0.25f, 0.1f)), false);
        h.gameObject.AddComponent<LanternSway>().angle = 4f;
    }

    void Barrels(Transform t)
    {
        var wood = LitMaterial("Madera", new Color(0.42f, 0.27f, 0.15f));
        foreach (var p in new[] { new Vector3(-0.4f, 0f, 0f), new Vector3(0.4f, 0f, 0.1f), new Vector3(0f, 0.95f, 0.05f) })
        {
            Prim(t, PrimitiveType.Cylinder, "Barril", p + Vector3.up * 0.45f, new Vector3(0.7f, 0.45f, 0.7f), wood, p.y < 0.1f);
            foreach (float y in new[] { 0.15f, 0.75f }) Prim(t, PrimitiveType.Cylinder, "Aro", p + Vector3.up * y, new Vector3(0.72f, 0.02f, 0.72f), MetalMaterial(), false);
        }
    }

    void Cauldron(Transform t)
    {
        Prim(t, PrimitiveType.Sphere, "Caldero", new Vector3(0f, 0.5f, 0f), new Vector3(1.1f, 0.8f, 1.1f), MetalMaterial(), true);
        Prim(t, PrimitiveType.Cylinder, "Guiso", new Vector3(0f, 0.82f, 0f), new Vector3(0.85f, 0.02f, 0.85f), GlowMaterial("Glow_Guiso", new Color(0.6f, 0.45f, 0.1f)), false);
        Motes(t, new Vector3(0f, 0.9f, 0f), new Vector3(0.6f, 0.1f, 0.6f), new Color(1f, 0.9f, 0.7f), 6f, 0.6f, 2f);
        Flame(t, new Vector3(0f, 0.05f, 0f), 1.2f, palette.torch);
        AddLight(t, new Vector3(0f, 0.4f, 0.6f), palette.torch, 4f, 3f, false).gameObject.AddComponent<LightFlicker>().amount = 0.3f;
    }

    void Sacks(Transform t)
    {
        var cloth = LitMaterial("Arpillera", new Color(0.62f, 0.52f, 0.36f));
        foreach (var p in new[] { new Vector3(-0.35f, 0f, 0f), new Vector3(0.3f, 0f, 0.1f), new Vector3(0f, 0.45f, 0f) })
            Prim(t, PrimitiveType.Sphere, "Costal", p + Vector3.up * 0.3f, new Vector3(0.6f, 0.65f, 0.5f), cloth, p.y < 0.1f);
    }

    void GoldHeap(Transform t, float size)
    {
        var gold = BrassMaterial();
        Prim(t, PrimitiveType.Sphere, "Monton", new Vector3(0f, size * 0.15f, 0f), new Vector3(size * 1.6f, size * 0.7f, size * 1.4f), gold, true);
        for (int i = 0; i < 10; i++)
        {
            var c = Prim(t, PrimitiveType.Cylinder, "Moneda", new Vector3(DR(-size, size), DR(0.02f, size * 0.4f), DR(-size, size) * 0.8f), new Vector3(0.18f, 0.012f, 0.18f), gold, false);
            c.transform.localRotation = Quaternion.Euler(DR(0f, 80f), DR(0f, 360f), 0f);
        }
        var gem = Prim(t, PrimitiveType.Cube, "Gema", new Vector3(DR(-0.3f, 0.3f), size * 0.5f, 0f), Vector3.one * 0.15f, GlowMaterial("Glow_" + ColorUtility.ToHtmlStringRGB(palette.accent), new Color(0.3f, 1f, 0.6f)), false);
        gem.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
        Motes(t, new Vector3(0f, size * 0.4f, 0f), new Vector3(size * 1.4f, 0.2f, size), new Color(1f, 0.9f, 0.5f), 3f, 0.2f, 1.2f);
    }

    void GoldBars(Transform t)
    {
        var gold = BrassMaterial();
        for (int row = 0; row < 3; row++)
            for (int i = 0; i < 4 - row; i++)
                Prim(t, PrimitiveType.Cube, "Lingote", new Vector3((i - (3 - row) / 2f) * 0.3f, 0.07f + row * 0.13f, 0f), new Vector3(0.26f, 0.12f, 0.5f), gold, row == 0 && i == 0);
    }

    void Scales(Transform t)
    {
        var gold = BrassMaterial();
        Prim(t, PrimitiveType.Cylinder, "Base", new Vector3(0f, 0.05f, 0f), new Vector3(0.5f, 0.05f, 0.5f), gold, true);
        Prim(t, PrimitiveType.Cylinder, "Asta", new Vector3(0f, 0.7f, 0f), new Vector3(0.06f, 0.65f, 0.06f), gold, false);
        var beam = Prim(t, PrimitiveType.Cube, "Cruz", new Vector3(0f, 1.35f, 0f), new Vector3(1.1f, 0.04f, 0.04f), gold, false);
        beam.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);   // desequilibrada: siempre pesa más un lado
        Prim(t, PrimitiveType.Cylinder, "PlatoA", new Vector3(-0.5f, 1.1f, 0f), new Vector3(0.35f, 0.02f, 0.35f), gold, false);
        Prim(t, PrimitiveType.Cylinder, "PlatoB", new Vector3(0.5f, 1.25f, 0f), new Vector3(0.35f, 0.02f, 0.35f), gold, false);
    }

    void Mirror(Transform t, float w, float h)
    {
        t.gameObject.AddComponent<OccluderFade>();
        var gold = BrassMaterial();
        Prim(t, PrimitiveType.Cube, "Marco", new Vector3(0f, h / 2f + 0.1f, 0f), new Vector3(w + 0.16f, h + 0.16f, 0.08f), gold, true);
        var glass = SavedMaterial("Espejo_" + palette.key, () =>
        {
            var m = new Material(LitShader);
            m.SetColor("_BaseColor", Color.Lerp(new Color(0.05f, 0.08f, 0.07f), palette.accent, 0.08f));
            m.SetFloat("_Metallic", 1f); m.SetFloat("_Smoothness", 0.98f);
            return m;
        });
        CreateFace(t, "Luna", new Vector3(0f, h / 2f + 0.1f, 0.045f), Vector3.forward, w, h, glass, 1f, 1f);
        // una silueta tenue dentro del espejo (alguien del otro lado)
        var ghost = Prim(t, PrimitiveType.Capsule, "Silueta", new Vector3(DR(-0.2f, 0.2f), 0.8f, -0.05f), new Vector3(0.35f, 0.55f, 0.02f), GlowMaterial("Glow_Silueta", new Color(0.08f, 0.25f, 0.12f)), false);
        ghost.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void Portrait(Transform t)
    {
        var gold = BrassMaterial();
        Prim(t, PrimitiveType.Cube, "Marco", new Vector3(0f, 1.7f, -0.3f), new Vector3(0.9f, 1.1f, 0.06f), gold, false);
        // retrato: fondo oscuro y una figura de cabello morado (todos son de "A.")
        CreateFace(t, "Lienzo", new Vector3(0f, 1.7f, -0.26f), Vector3.forward, 0.76f, 0.96f, LitMaterial("Lienzo", new Color(0.1f, 0.14f, 0.1f)), 1f, 1f);
        Prim(t, PrimitiveType.Sphere, "Cabeza", new Vector3(0f, 1.82f, -0.25f), new Vector3(0.22f, 0.26f, 0.02f), LitMaterial("Piel", new Color(0.85f, 0.7f, 0.6f)), false);
        Prim(t, PrimitiveType.Sphere, "Pelo", new Vector3(0f, 1.88f, -0.255f), new Vector3(0.28f, 0.26f, 0.02f), LitMaterial("Tela_Abby", new Color(0.42f, 0.22f, 0.55f)), false);
        Prim(t, PrimitiveType.Cube, "Cuerpo", new Vector3(0f, 1.45f, -0.25f), new Vector3(0.36f, 0.32f, 0.02f), LitMaterial("Tela_Saco", new Color(0.2f, 0.25f, 0.45f)), false);
        Prim(t, PrimitiveType.Cube, "Mesa", new Vector3(0f, 0.4f, 0f), new Vector3(0.8f, 0.8f, 0.4f), LitMaterial("MaderaOscura", new Color(0.26f, 0.16f, 0.09f)), true);
        Candles(t, new Vector3(0.2f, 0.8f, 0f), 2, palette.torch, 0.08f);
    }

    void Curtain(Transform t)
    {
        var cloth = LitMaterial("Tela_" + ColorUtility.ToHtmlStringRGB(palette.banner), palette.banner);
        for (int i = 0; i < 5; i++)
            Prim(t, PrimitiveType.Cylinder, "Pliegue", new Vector3(-0.6f + i * 0.3f, 1.4f, -0.3f + (i % 2) * 0.08f), new Vector3(0.22f, 1.4f, 0.1f), cloth, false);
        Prim(t, PrimitiveType.Cube, "Barra", new Vector3(0f, 2.85f, -0.3f), new Vector3(1.7f, 0.05f, 0.05f), BrassMaterial(), false);
    }

    void Showcase(Transform t)
    {
        var wood = LitMaterial("MaderaOscura", new Color(0.26f, 0.16f, 0.09f));
        Prim(t, PrimitiveType.Cube, "Base", new Vector3(0f, 0.45f, 0f), new Vector3(1.2f, 0.9f, 0.8f), wood, true);
        var glass = Prim(t, PrimitiveType.Cube, "Vidrio", new Vector3(0f, 1.2f, 0f), new Vector3(1.1f, 0.6f, 0.7f), GlassMaterial(palette.accent * 0.4f), false);
        glass.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // objetos ajenos: una corona, un zapato, una espada (cosas que alguien quería tener)
        var obj = drng.Next(3);
        if (obj == 0) Prim(t, PrimitiveType.Cylinder, "Corona", new Vector3(0f, 1.05f, 0f), new Vector3(0.3f, 0.08f, 0.3f), BrassMaterial(), false);
        else if (obj == 1) Prim(t, PrimitiveType.Cube, "Zapato", new Vector3(0f, 0.97f, 0f), new Vector3(0.12f, 0.1f, 0.3f), LitMaterial("Tela_Abby", new Color(0.42f, 0.22f, 0.55f)), false);
        else Prim(t, PrimitiveType.Cube, "Espada", new Vector3(0f, 0.95f, 0f), new Vector3(0.6f, 0.03f, 0.06f), MetalMaterial(), false).transform.localRotation = Quaternion.Euler(0f, 30f, 0f);
    }

    void RoseBush(Transform t, float size)
    {
        var leaf = LitMaterial("Hojas", new Color(0.12f, 0.3f, 0.12f));
        var rose = LitMaterial("Rosa", new Color(0.8f, 0.05f, 0.2f));
        for (int i = 0; i < 4; i++)
            Prim(t, PrimitiveType.Sphere, "Follaje", new Vector3(DR(-0.25f, 0.25f), size * 0.45f + DR(0f, 0.2f), DR(-0.25f, 0.25f)) * 1f, Vector3.one * size * DR(0.6f, 0.9f), leaf, i == 0);
        for (int i = 0; i < 7; i++)
            Prim(t, PrimitiveType.Sphere, "Rosa", new Vector3(DR(-0.4f, 0.4f) * size, size * DR(0.55f, 1f), DR(-0.4f, 0.4f) * size), Vector3.one * 0.13f, rose, false);
    }

    void Cushions(Transform t)
    {
        Color[] cols = { palette.rug, palette.rugBorder, new Color(0.9f, 0.5f, 0.65f) };
        for (int i = 0; i < 4; i++)
            Prim(t, PrimitiveType.Sphere, "Cojin", new Vector3(DR(-0.6f, 0.6f), 0.12f + (i == 3 ? 0.2f : 0f), DR(-0.3f, 0.3f)), new Vector3(0.6f, 0.25f, 0.5f), LitMaterial("Tela_" + ColorUtility.ToHtmlStringRGB(cols[i % 3]), cols[i % 3]), i == 0);
        Candles(t, new Vector3(0.7f, 0f, 0.4f), 4, palette.torch, 0.15f);
    }

    void RoseVase(Transform t)
    {
        Prim(t, PrimitiveType.Cylinder, "Pedestal", new Vector3(0f, 0.45f, 0f), new Vector3(0.45f, 0.45f, 0.45f), ThemeStone(), true);
        Prim(t, PrimitiveType.Sphere, "Jarron", new Vector3(0f, 1.1f, 0f), new Vector3(0.5f, 0.55f, 0.5f), BrassMaterial(), false);
        var rose = LitMaterial("Rosa", new Color(0.8f, 0.05f, 0.2f));
        for (int i = 0; i < 6; i++) Prim(t, PrimitiveType.Sphere, "Rosa", new Vector3(DR(-0.2f, 0.2f), 1.45f + DR(0f, 0.25f), DR(-0.2f, 0.2f)), Vector3.one * 0.14f, rose, false);
    }

    void Anvil(Transform t)
    {
        var iron = MetalMaterial();
        Prim(t, PrimitiveType.Cube, "Tronco", new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 0.6f, 0.6f), LitMaterial("MaderaOscura", new Color(0.26f, 0.16f, 0.09f)), true);
        Prim(t, PrimitiveType.Cube, "Cuerpo", new Vector3(0f, 0.72f, 0f), new Vector3(0.35f, 0.25f, 0.3f), iron, false);
        Prim(t, PrimitiveType.Cube, "Cara", new Vector3(0f, 0.9f, 0f), new Vector3(0.75f, 0.12f, 0.32f), iron, false);
        var horn = Prim(t, PrimitiveType.Cylinder, "Cuerno", new Vector3(0.48f, 0.9f, 0f), new Vector3(0.12f, 0.15f, 0.12f), iron, false);
        horn.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        // pieza al rojo sobre el yunque
        Prim(t, PrimitiveType.Cube, "Hoja", new Vector3(-0.05f, 0.98f, 0f), new Vector3(0.5f, 0.03f, 0.08f), LavaMaterial(), false);
        Sparks(t, 0.5f, new Color(1f, 0.6f, 0.2f));
    }

    void WeaponRack(Transform t)
    {
        var wood = LitMaterial("MaderaOscura", new Color(0.26f, 0.16f, 0.09f));
        Prim(t, PrimitiveType.Cube, "Travesano", new Vector3(0f, 1.4f, -0.2f), new Vector3(1.6f, 0.08f, 0.1f), wood, false);
        Prim(t, PrimitiveType.Cube, "Base", new Vector3(0f, 0.1f, -0.1f), new Vector3(1.6f, 0.2f, 0.4f), wood, true);
        for (int i = 0; i < 4; i++)
        {
            var w = Prim(t, PrimitiveType.Cube, i % 2 == 0 ? "Espada" : "Lanza", new Vector3(-0.6f + i * 0.4f, 0.85f, -0.15f), new Vector3(0.05f, i % 2 == 0 ? 1.3f : 1.6f, 0.03f), MetalMaterial(), false);
            w.transform.localRotation = Quaternion.Euler(-8f, 0f, DR(-4f, 4f));
        }
    }

    void BrokenPillar(Transform t)
    {
        var stone = ThemeStone();
        Prim(t, PrimitiveType.Cylinder, "Tocon", new Vector3(0f, 0.6f, 0f), new Vector3(0.8f, 0.6f, 0.8f), stone, true);
        var piece = Prim(t, PrimitiveType.Cylinder, "Trozo", new Vector3(0.6f, 0.35f, 0.4f), new Vector3(0.7f, 0.5f, 0.7f), stone, false);
        piece.transform.localRotation = Quaternion.Euler(80f, 30f, 0f);
        // grieta incandescente
        Prim(t, PrimitiveType.Cube, "Grieta", new Vector3(0f, 0.7f, 0.41f), new Vector3(0.05f, 0.9f, 0.01f), LavaMaterial(), false);
    }

    void RopeCoils(Transform t)
    {
        var rope = LitMaterial("Cuerda", new Color(0.6f, 0.5f, 0.35f));
        for (int i = 0; i < 3; i++)
            Prim(t, PrimitiveType.Cylinder, "Rollo", new Vector3(-0.4f + i * 0.4f, 0.08f + (i == 1 ? 0.16f : 0f), 0f), new Vector3(0.5f, 0.08f, 0.5f), rope, i == 0);
        Prim(t, PrimitiveType.Cube, "Polea", new Vector3(0.6f, 0.25f, -0.2f), new Vector3(0.3f, 0.5f, 0.1f), LitMaterial("MaderaOscura", new Color(0.26f, 0.16f, 0.09f)), false);
    }

    void HangingChain(Transform parent, Vector3 pos)
    {
        var c = NewChild(parent, "Cadena");
        c.localPosition = pos;
        var iron = MetalMaterial();
        for (float y = 0f; y > -2.4f; y -= 0.18f)
            Prim(c, PrimitiveType.Cube, "Eslabon", new Vector3(0f, y, 0f), new Vector3(0.05f, 0.15f, 0.1f), iron, false).transform.localRotation = Quaternion.Euler(0f, (y * 100f) % 2 > 1 ? 90f : 0f, 0f);
        Prim(c, PrimitiveType.Cube, "Gancho", new Vector3(0f, -2.5f, 0.05f), new Vector3(0.06f, 0.25f, 0.06f), iron, false);
        c.gameObject.AddComponent<LanternSway>().angle = 3f;
    }

    /// <summary>Estatua de mármol: arrodillada (los que se rindieron) o de pie con la barbilla alta (los soberbios).</summary>
    void Statue(Transform t, bool kneeling)
    {
        if (t.GetComponent<OccluderFade>() == null) t.gameObject.AddComponent<OccluderFade>();
        var marble = ThemeStone();
        Prim(t, PrimitiveType.Cube, "Pedestal", new Vector3(0f, 0.3f, 0f), new Vector3(1f, 0.6f, 1f), marble, true);
        float y = 0.6f;
        if (kneeling)
        {
            Prim(t, PrimitiveType.Capsule, "Cuerpo", new Vector3(0f, y + 0.55f, 0.05f), new Vector3(0.45f, 0.45f, 0.4f), marble, true).transform.localRotation = Quaternion.Euler(25f, 0f, 0f);
            Prim(t, PrimitiveType.Sphere, "Cabeza", new Vector3(0f, y + 0.95f, 0.3f), Vector3.one * 0.26f, marble, false);
            Prim(t, PrimitiveType.Cube, "Piernas", new Vector3(0f, y + 0.15f, -0.1f), new Vector3(0.4f, 0.3f, 0.6f), marble, false);
        }
        else
        {
            Prim(t, PrimitiveType.Capsule, "Cuerpo", new Vector3(0f, y + 0.9f, 0f), new Vector3(0.45f, 0.75f, 0.35f), marble, true);
            Prim(t, PrimitiveType.Sphere, "Cabeza", new Vector3(0f, y + 1.85f, -0.05f), Vector3.one * 0.3f, marble, false).transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
            Prim(t, PrimitiveType.Cylinder, "Corona", new Vector3(0f, y + 2.05f, -0.06f), new Vector3(0.28f, 0.05f, 0.28f), BrassMaterial(), false);
            var arm = Prim(t, PrimitiveType.Capsule, "Brazo", new Vector3(0.32f, y + 1.3f, 0.15f), new Vector3(0.14f, 0.35f, 0.14f), marble, false);
            arm.transform.localRotation = Quaternion.Euler(-60f, 0f, -20f);
        }
    }

    void GoldVase(Transform t)
    {
        Prim(t, PrimitiveType.Cylinder, "Pedestal", new Vector3(0f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0.5f), ThemeStone(), true);
        Prim(t, PrimitiveType.Sphere, "Vasija", new Vector3(0f, 1.25f, 0f), new Vector3(0.6f, 0.7f, 0.6f), BrassMaterial(), false);
        Prim(t, PrimitiveType.Cylinder, "Cuello", new Vector3(0f, 1.65f, 0f), new Vector3(0.25f, 0.12f, 0.25f), BrassMaterial(), false);
    }
}
