using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Iluminación "de verdad" de los pisos: cada luz sale de un objeto que la justifica (antorcha con soporte de hierro
/// y llama de partículas, farol colgante, candelabro con velas, vela, lámpara de cristal…), con halo, chispas y parpadeo.
/// </summary>
public partial class FloorLayout
{
    ThemePalette palette;

    // ------------------------------------------------------------------ salas

    /// <summary>
    /// Luz de una sala: un candelabro colgante (la luz principal, con sombra) y antorchas en los muros N, E y O
    /// (el S casi nunca se ve), esquivando puertas. Según el tema cambian los aparatos: faroles, lámparas, cristales…
    /// </summary>
    void AddRoomLighting(Transform root, RoomSpec r, List<Gap> gaps)
    {
        Color c = theme == FloorTheme.Base ? RoomLightColor(r.type) : TorchColorFor(r.type);
        float y = r.feature == RoomFeature.Ascenso ? 4.2f : wallHeight * 0.68f;
        int shadowsLeft = shadowTorchesPerRoom;
        var lights = NewChild(root, "Iluminacion");

        // luz principal: candelabro / lámpara colgante en el centro (alto: no estorba la vista)
        // en salas altas el candelabro cuelga de cadenas largas hasta ~5 m: si queda arriba, su luz no llega al suelo
        float hang = Mathf.Min(roomHeight + 0.6f, Mathf.Max(wallHeight + 0.8f, 5f));
        float intensity = centerLightIntensity * Mathf.Max(1f, Mathf.Max(r.size.x, r.size.y) / 16f) * Mathf.Max(1f, hang / 3.6f);
        Color centerColor = theme == FloorTheme.Base ? Color.Lerp(c, Color.white, 0.55f) : palette.center;
        var mainPos = new Vector3(0f, hang, -r.size.y * 0.1f);
        switch (theme)
        {
            case FloorTheme.Lujuria:
            case FloorTheme.Soberbia:
                Chandelier(lights, mainPos, 1.1f, 10, centerColor, intensity, Mathf.Max(r.size.x, r.size.y) * 0.95f, true, true);
                break;
            case FloorTheme.Envidia:
                CrystalLamp(lights, mainPos, palette.accent, intensity, Mathf.Max(r.size.x, r.size.y) * 0.95f, true);
                break;
            case FloorTheme.Ira:
                CageBrazier(lights, mainPos, palette.torch, intensity, Mathf.Max(r.size.x, r.size.y) * 0.95f, true);
                break;
            default:
                Chandelier(lights, mainPos, 0.9f, 8, centerColor, intensity, Mathf.Max(r.size.x, r.size.y) * 0.9f, true, false);
                break;
        }
        // salas grandes: dos candelabros más (sin sombra) para que no quede un pozo de luz al centro
        if (r.size.x >= 24f && r.feature != RoomFeature.Laberinto)
            foreach (float sx in new[] { -1f, 1f })
                Chandelier(lights, new Vector3(sx * r.size.x * 0.3f, hang, r.size.y * 0.12f), 0.7f, 6, centerColor, intensity * 0.55f, Mathf.Max(r.size.x, r.size.y) * 0.6f, false, theme == FloorTheme.Soberbia || theme == FloorTheme.Lujuria);

        HighTorches(lights, r, gaps, c);

        // antorchas o apliques en los muros
        Side[] sides = r.feature == RoomFeature.Ascenso || r.feature == RoomFeature.Escalinata ? new[] { Side.E, Side.W } : new[] { Side.N, Side.E, Side.W };
        foreach (Side side in sides)
        {
            float len = side == Side.N ? r.size.x : r.size.y;
            int count = Mathf.Max(2, Mathf.RoundToInt(len / 7f));   // una antorcha cada ~7 m
            for (int ti = 0; ti < count; ti++)
            {
                float along = ((ti + 0.5f) / count - 0.5f) * len;
                bool blocked = false;
                foreach (var g in gaps)
                    if (g.side == side && Mathf.Abs(along - g.offset) < g.width / 2f + 0.8f) blocked = true;
                if (blocked) continue;
                Vector3 wallPos, inward;
                switch (side)
                {
                    case Side.N: wallPos = new Vector3(along, y, r.size.y / 2f); inward = Vector3.back; break;
                    case Side.E: wallPos = new Vector3(r.size.x / 2f, y, along); inward = Vector3.left; break;
                    default: wallPos = new Vector3(-r.size.x / 2f, y, along); inward = Vector3.right; break;
                }
                bool shadow = shadowsLeft-- > 0;
                switch (theme)
                {
                    case FloorTheme.Lujuria:
                    case FloorTheme.Soberbia:
                    case FloorTheme.Avaricia:
                        WallLamp(lights, wallPos, inward, theme == FloorTheme.Avaricia ? palette.torch : palette.center, shadow);
                        break;
                    case FloorTheme.Envidia:
                        WallCrystal(lights, wallPos, inward, palette.accent, shadow);
                        break;
                    default:
                        WallTorch(lights, wallPos, inward, c, shadow);
                        break;
                }
            }
        }
    }

    /// <summary>En salas de muros altos, antorchas grandes a media altura en E y O: la sala se lee completa, no solo abajo.</summary>
    void HighTorches(Transform lights, RoomSpec r, List<Gap> gaps, Color c)
    {
        if (curWall < 5.5f) return;
        float y = curWall * 0.62f;
        foreach (var side in new[] { Side.E, Side.W })
            foreach (float along in new[] { -r.size.y * 0.12f, r.size.y * 0.38f })
            {
                bool blocked = false;
                foreach (var g in gaps) if (g.side == side && Mathf.Abs(along - g.offset) < g.width / 2f + 1f && g.elevation + wallHeight > y - 0.5f) blocked = true;
                if (blocked) continue;
                Vector3 pos = side == Side.E ? new Vector3(r.size.x / 2f, y, along) : new Vector3(-r.size.x / 2f, y, along);
                Vector3 inward = side == Side.E ? Vector3.left : Vector3.right;
                WallTorch(lights, pos, inward, c, false, 1.3f);
            }
    }

    Color TorchColorFor(RoomType t)
    {
        switch (t)
        {
            case RoomType.Jefe: return Color.Lerp(palette.torch, new Color(1f, 0.25f, 0.2f), 0.45f);
            case RoomType.Descanso: return Color.Lerp(palette.torch, new Color(1f, 0.8f, 0.5f), 0.4f);
            case RoomType.Antesala: return Color.Lerp(palette.torch, new Color(0.7f, 0.45f, 1f), 0.35f);
            default: return palette.torch;
        }
    }

    /// <summary>Pasillos: faroles colgando cada ~6 m (o apliques del tema), sin sombra.</summary>
    void CorridorLights(Transform root, bool alongZ, float width, float len, float y0, float y1)
    {
        Color c = theme == FloorTheme.Base ? new Color(1f, 0.72f, 0.45f) : palette.corridor;
        int n = Mathf.Max(1, Mathf.RoundToInt(len / 6f));
        for (int i = 0; i < n; i++)
        {
            float t = (i + 0.5f) / n;
            float along = (t - 0.5f) * len;
            float y = Mathf.Lerp(y0, y1, t);
            // alterna de muro para que la luz no sea simétrica
            float side = (i % 2 == 0 ? 1f : -1f) * (width / 2f);
            Vector3 wallPos = alongZ ? new Vector3(side, y + wallHeight * 0.66f, along) : new Vector3(along, y + wallHeight * 0.66f, side);
            Vector3 inward = alongZ ? new Vector3(-Mathf.Sign(side), 0f, 0f) : new Vector3(0f, 0f, -Mathf.Sign(side));
            if (theme == FloorTheme.Base || theme == FloorTheme.Galerias || theme == FloorTheme.Pereza || theme == FloorTheme.Gula || theme == FloorTheme.Ira)
                WallTorch(root, wallPos, inward, c, false, 0.75f);
            else if (theme == FloorTheme.Envidia)
                WallCrystal(root, wallPos, inward, c, false, 0.75f);
            else
                WallLamp(root, wallPos, inward, c, false, 0.75f);
        }
    }

    // ------------------------------------------------------------------ aparatos

    /// <summary>Antorcha: soporte de hierro atornillado al muro, mango de madera inclinado y llama de partículas.</summary>
    void WallTorch(Transform parent, Vector3 wallPos, Vector3 inward, Color color, bool shadows, float power = 1f)
    {
        var t = NewChild(parent, "Antorcha");
        t.localPosition = wallPos;
        t.localRotation = Quaternion.LookRotation(inward);   // +Z local = hacia la sala
        var iron = MetalMaterial();
        var wood = LitMaterial("MaderaOscura", new Color(0.26f, 0.16f, 0.09f));
        Prim(t, PrimitiveType.Cube, "Placa", new Vector3(0f, -0.05f, 0.03f), new Vector3(0.16f, 0.32f, 0.04f), iron, false);
        Prim(t, PrimitiveType.Cube, "Brazo", new Vector3(0f, -0.1f, 0.15f), new Vector3(0.05f, 0.05f, 0.26f), iron, false);
        var ring = Prim(t, PrimitiveType.Cylinder, "Aro", new Vector3(0f, -0.08f, 0.27f), new Vector3(0.13f, 0.02f, 0.13f), iron, false);
        var handle = Prim(t, PrimitiveType.Cylinder, "Mango", new Vector3(0f, 0.02f, 0.3f), new Vector3(0.06f, 0.22f, 0.06f), wood, false);
        handle.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
        var cup = Prim(t, PrimitiveType.Cylinder, "Copa", new Vector3(0f, 0.22f, 0.37f), new Vector3(0.12f, 0.05f, 0.12f), iron, false);
        cup.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
        var flamePos = new Vector3(0f, 0.3f, 0.4f);
        Flame(t, flamePos, 1f, color);
        // la luz va un poco hacia la sala (si queda pegada al muro, el muro "se come" la luz)
        var lt = AddLight(t, flamePos + new Vector3(0f, 0.05f, 0.35f), color, torchRange, torchIntensity * power, shadows);
        lt.gameObject.AddComponent<LightFlicker>().amount = 0.22f;
    }

    /// <summary>Aplique de pared con globo de vidrio (palacios: Lujuria, Soberbia, Avaricia).</summary>
    void WallLamp(Transform parent, Vector3 wallPos, Vector3 inward, Color color, bool shadows, float power = 1f)
    {
        var t = NewChild(parent, "Aplique");
        t.localPosition = wallPos;
        t.localRotation = Quaternion.LookRotation(inward);
        var brass = BrassMaterial();
        Prim(t, PrimitiveType.Cube, "Placa", new Vector3(0f, -0.05f, 0.02f), new Vector3(0.2f, 0.36f, 0.03f), brass, false);
        var arm = Prim(t, PrimitiveType.Cylinder, "Brazo", new Vector3(0f, -0.12f, 0.14f), new Vector3(0.035f, 0.13f, 0.035f), brass, false);
        arm.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Prim(t, PrimitiveType.Cylinder, "Base", new Vector3(0f, -0.1f, 0.28f), new Vector3(0.14f, 0.03f, 0.14f), brass, false);
        var globe = Prim(t, PrimitiveType.Sphere, "Globo", new Vector3(0f, 0.06f, 0.28f), new Vector3(0.24f, 0.28f, 0.24f), GlassMaterial(color), false);
        globe.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Flame(t, new Vector3(0f, 0.02f, 0.28f), 0.45f, color, false);
        Halo(t, new Vector3(0f, 0.06f, 0.28f), 1.1f, color, 0.22f);
        var lt = AddLight(t, new Vector3(0f, 0.1f, 0.6f), color, torchRange, torchIntensity * 0.9f * power, shadows);
        lt.gameObject.AddComponent<LightFlicker>().amount = 0.06f;
    }

    /// <summary>Cristal mágico engarzado en el muro (Envidia): luz fría que late.</summary>
    void WallCrystal(Transform parent, Vector3 wallPos, Vector3 inward, Color color, bool shadows, float power = 1f)
    {
        var t = NewChild(parent, "Cristal");
        t.localPosition = wallPos;
        t.localRotation = Quaternion.LookRotation(inward);
        Prim(t, PrimitiveType.Cube, "Engarce", new Vector3(0f, -0.2f, 0.06f), new Vector3(0.3f, 0.1f, 0.14f), BrassMaterial(), false);
        foreach (var (pos, rot, s) in new[] { (new Vector3(0f, 0.02f, 0.1f), new Vector3(0f, 0f, 0f), 0.32f), (new Vector3(-0.09f, -0.06f, 0.09f), new Vector3(0f, 0f, 28f), 0.2f), (new Vector3(0.1f, -0.07f, 0.1f), new Vector3(0f, 0f, -24f), 0.18f) })
        {
            var shard = Prim(t, PrimitiveType.Cube, "Esquirla", pos, new Vector3(0.09f, s, 0.09f), GlowMaterial("Glow_" + ColorUtility.ToHtmlStringRGB(color), color), false);
            shard.transform.localRotation = Quaternion.Euler(rot + new Vector3(0f, 45f, 0f));
            shard.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        Halo(t, new Vector3(0f, 0f, 0.12f), 1.3f, color, 0.25f);
        Motes(t, new Vector3(0f, 0f, 0.2f), new Vector3(0.3f, 0.3f, 0.2f), color, 4f, 0.25f, 2f);
        var lt = AddLight(t, new Vector3(0f, 0.05f, 0.5f), color, torchRange, torchIntensity * 0.8f * power, shadows);
        var f = lt.gameObject.AddComponent<LightFlicker>(); f.amount = 0.12f; f.speed = 1.2f;
    }

    /// <summary>Farol de hierro y vidrio colgado de una cadena (pasillos y salas de piedra).</summary>
    void HangingLantern(Transform parent, Vector3 pos, float chain, Color color, float intensity, float range, bool shadows)
    {
        var t = NewChild(parent, "Farol");
        t.localPosition = pos;
        t.gameObject.AddComponent<OccluderFade>();   // si tapa a Abby se deja de dibujar (la luz sigue)
        var iron = MetalMaterial();
        for (float y = 0.25f; y < chain; y += 0.16f)
        {
            var link = Prim(t, PrimitiveType.Cube, "Eslabon", new Vector3(0f, y, 0f), new Vector3(0.03f, 0.12f, 0.06f), iron, false);
            link.transform.localRotation = Quaternion.Euler(0f, (y * 37f) % 2 > 1 ? 90f : 0f, 0f);
        }
        Prim(t, PrimitiveType.Cube, "Tapa", new Vector3(0f, 0.2f, 0f), new Vector3(0.3f, 0.06f, 0.3f), iron, false);
        Prim(t, PrimitiveType.Cube, "Fondo", new Vector3(0f, -0.2f, 0f), new Vector3(0.26f, 0.05f, 0.26f), iron, false);
        foreach (var x in new[] { -0.12f, 0.12f })
            foreach (var z in new[] { -0.12f, 0.12f })
                Prim(t, PrimitiveType.Cube, "Varilla", new Vector3(x, 0f, z), new Vector3(0.03f, 0.4f, 0.03f), iron, false);
        var glass = Prim(t, PrimitiveType.Cube, "Vidrio", Vector3.zero, new Vector3(0.22f, 0.34f, 0.22f), GlassMaterial(color), false);
        glass.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Flame(t, new Vector3(0f, -0.1f, 0f), 0.4f, color, false);
        Halo(t, Vector3.zero, 1.4f, color, 0.2f);
        var lt = AddLight(t, new Vector3(0f, -0.35f, 0f), color, range, intensity, shadows);
        lt.gameObject.AddComponent<LightFlicker>().amount = 0.1f;
        t.gameObject.AddComponent<LanternSway>();
    }

    /// <summary>
    /// Candelabro colgante: aro de hierro (o latón) con velas encendidas, cadenas al techo y una luz principal con sombra.
    /// </summary>
    void Chandelier(Transform parent, Vector3 pos, float radius, int candles, Color color, float intensity, float range, bool shadows, bool fancy)
    {
        var t = NewChild(parent, "Candelabro");
        t.localPosition = pos;
        t.gameObject.AddComponent<OccluderFade>();   // si tapa a Abby se deja de dibujar (la luz sigue)
        var metal = fancy ? BrassMaterial() : MetalMaterial();
        var wax = LitMaterial("Cera", new Color(0.92f, 0.86f, 0.72f));
        var ring = new GameObject("Aro");
        ring.transform.SetParent(t, false);
        ring.AddComponent<MeshFilter>().sharedMesh = Annulus(radius - 0.06f, radius + 0.06f, 48);
        ring.AddComponent<MeshRenderer>().sharedMaterial = metal;
        var under = new GameObject("AroBajo");
        under.transform.SetParent(t, false);
        under.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);   // la cara de abajo también
        under.AddComponent<MeshFilter>().sharedMesh = Annulus(radius - 0.06f, radius + 0.06f, 48);
        under.AddComponent<MeshRenderer>().sharedMaterial = metal;
        Prim(t, PrimitiveType.Sphere, "Centro", new Vector3(0f, -0.15f, 0f), new Vector3(0.3f, 0.4f, 0.3f), metal, false);
        // cadenas: tres que suben hasta perderse
        for (int k = 0; k < 3; k++)
        {
            float a = k / 3f * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            var chain = Prim(t, PrimitiveType.Cube, "Cadena", dir * 0.5f + Vector3.up * 1.2f, new Vector3(0.025f, 2.5f, 0.025f), metal, false);
            chain.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (Vector3.up * 2.4f - dir).normalized);
        }
        for (int i = 0; i < candles; i++)
        {
            float a = (i + 0.5f) / candles * Mathf.PI * 2f;
            var p = new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Prim(t, PrimitiveType.Cylinder, "Platillo", p + Vector3.up * 0.02f, new Vector3(0.13f, 0.015f, 0.13f), metal, false);
            float h = 0.12f + ((i * 7) % 5) * 0.025f;   // velas a medio consumir
            Prim(t, PrimitiveType.Cylinder, "Vela", p + Vector3.up * (0.04f + h / 2f), new Vector3(0.06f, h / 2f, 0.06f), wax, false);
            Flame(t, p + Vector3.up * (0.06f + h), 0.32f, color, false, false);
        }
        if (fancy)
            for (int i = 0; i < 12; i++)   // lágrimas de cristal
            {
                float a = i / 12f * Mathf.PI * 2f;
                var drop = Prim(t, PrimitiveType.Sphere, "Lagrima", new Vector3(Mathf.Cos(a) * radius * 0.7f, -0.3f - (i % 3) * 0.08f, Mathf.Sin(a) * radius * 0.7f), new Vector3(0.06f, 0.12f, 0.06f), GlassMaterial(color), false);
                drop.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        Halo(t, Vector3.zero, radius * 3f, color, 0.12f);
        var lt = AddLight(t, new Vector3(0f, -0.4f, 0f), color, range, intensity, shadows);
        lt.gameObject.AddComponent<LightFlicker>().amount = 0.07f;
        t.gameObject.AddComponent<LanternSway>().angle = 1.5f;
        foreach (var mr in t.GetComponentsInChildren<MeshRenderer>()) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // no tapar la sala con su sombra
    }

    /// <summary>Lámpara colgante de cristal verde (Envidia).</summary>
    void CrystalLamp(Transform parent, Vector3 pos, Color color, float intensity, float range, bool shadows)
    {
        var t = NewChild(parent, "LamparaCristal");
        t.localPosition = pos;
        t.gameObject.AddComponent<OccluderFade>();   // si tapa a Abby se deja de dibujar (la luz sigue)
        Prim(t, PrimitiveType.Cube, "Cadena", new Vector3(0f, 1.3f, 0f), new Vector3(0.03f, 2.6f, 0.03f), BrassMaterial(), false);
        for (int i = 0; i < 7; i++)
        {
            float a = i / 7f * Mathf.PI * 2f;
            var s = Prim(t, PrimitiveType.Cube, "Prisma", new Vector3(Mathf.Cos(a) * 0.25f, -0.1f - (i % 2) * 0.15f, Mathf.Sin(a) * 0.25f), new Vector3(0.1f, 0.5f, 0.1f), GlowMaterial("Glow_" + ColorUtility.ToHtmlStringRGB(color), color), false);
            s.transform.localRotation = Quaternion.Euler(Mathf.Sin(a) * 15f, a * Mathf.Rad2Deg, Mathf.Cos(a) * 15f);
            s.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        Halo(t, Vector3.zero, 3f, color, 0.15f);
        Motes(t, Vector3.down * 0.3f, new Vector3(0.6f, 0.2f, 0.6f), color, 6f, -0.2f, 2.5f);
        var lt = AddLight(t, new Vector3(0f, -0.5f, 0f), color, range, intensity, shadows);
        var f = lt.gameObject.AddComponent<LightFlicker>(); f.amount = 0.1f; f.speed = 1.5f;
        t.gameObject.AddComponent<LanternSway>().angle = 2f;
    }

    /// <summary>Jaula de hierro colgante con brasas ardiendo (Ira).</summary>
    void CageBrazier(Transform parent, Vector3 pos, Color color, float intensity, float range, bool shadows)
    {
        var t = NewChild(parent, "JaulaBrasas");
        t.localPosition = pos;
        t.gameObject.AddComponent<OccluderFade>();   // si tapa a Abby se deja de dibujar (la luz sigue)
        var iron = MetalMaterial();
        Prim(t, PrimitiveType.Cube, "Cadena", new Vector3(0f, 1.4f, 0f), new Vector3(0.05f, 2.6f, 0.05f), iron, false);
        for (int i = 0; i < 8; i++)
        {
            float a = i / 8f * Mathf.PI * 2f;
            Prim(t, PrimitiveType.Cube, "Barra", new Vector3(Mathf.Cos(a) * 0.4f, -0.1f, Mathf.Sin(a) * 0.4f), new Vector3(0.05f, 0.7f, 0.05f), iron, false);
        }
        Prim(t, PrimitiveType.Cylinder, "Cuenco", new Vector3(0f, -0.42f, 0f), new Vector3(0.85f, 0.06f, 0.85f), iron, false);
        Flame(t, new Vector3(0f, -0.3f, 0f), 2.2f, color, true);
        var lt = AddLight(t, new Vector3(0f, -0.7f, 0f), color, range, intensity * 1.1f, shadows);
        lt.gameObject.AddComponent<LightFlicker>().amount = 0.3f;
        t.gameObject.AddComponent<LanternSway>().angle = 2.5f;
    }

    /// <summary>Grupo de velas en el suelo o sobre un mueble, con una sola luz pequeña para todas.</summary>
    void Candles(Transform parent, Vector3 pos, int count, Color color, float spread = 0.25f, bool light = true)
    {
        var t = NewChild(parent, "Velas");
        t.localPosition = pos;
        var wax = LitMaterial("Cera", new Color(0.92f, 0.86f, 0.72f));
        for (int i = 0; i < count; i++)
        {
            float a = i * 2.39996f, d = spread * Mathf.Sqrt((i + 0.5f) / count);   // espiral de girasol
            var p = new Vector3(Mathf.Cos(a) * d, 0f, Mathf.Sin(a) * d);
            float h = DR(0.08f, 0.3f);
            Prim(t, PrimitiveType.Cylinder, "Vela", p + Vector3.up * h / 2f, new Vector3(0.07f, h / 2f, 0.07f), wax, false);
            Flame(t, p + Vector3.up * (h + 0.03f), 0.28f, color, false, false);
        }
        if (light)
        {
            var lt = AddLight(t, Vector3.up * 0.5f, color, 4.5f, 2.5f + count * 0.4f, false);
            lt.gameObject.AddComponent<LightFlicker>().amount = 0.25f;
        }
    }

    /// <summary>Candelabro de pie (de suelo) con tres velas.</summary>
    void FloorCandelabra(Transform parent, Vector3 pos, Color color)
    {
        var t = NewChild(parent, "CandelabroPie");
        t.localPosition = pos;
        var metal = theme == FloorTheme.Soberbia || theme == FloorTheme.Avaricia || theme == FloorTheme.Lujuria ? BrassMaterial() : MetalMaterial();
        var wax = LitMaterial("Cera", new Color(0.92f, 0.86f, 0.72f));
        Prim(t, PrimitiveType.Cylinder, "Pie", new Vector3(0f, 0.05f, 0f), new Vector3(0.4f, 0.05f, 0.4f), metal, false);
        Prim(t, PrimitiveType.Cylinder, "Asta", new Vector3(0f, 0.75f, 0f), new Vector3(0.06f, 0.7f, 0.06f), metal, false);
        Prim(t, PrimitiveType.Cube, "Cruz", new Vector3(0f, 1.42f, 0f), new Vector3(0.7f, 0.04f, 0.05f), metal, false);
        foreach (float x in new[] { -0.33f, 0f, 0.33f })
        {
            float y = x == 0f ? 1.55f : 1.45f;
            Prim(t, PrimitiveType.Cylinder, "Vela", new Vector3(x, y, 0f), new Vector3(0.07f, 0.1f, 0.07f), wax, false);
            Flame(t, new Vector3(x, y + 0.13f, 0f), 0.3f, color, false, false);
        }
        var lt = AddLight(t, new Vector3(0f, 1.8f, 0f), color, 6f, 4f, false);
        lt.gameObject.AddComponent<LightFlicker>().amount = 0.2f;
    }

    // ------------------------------------------------------------------ fuego

    /// <summary>
    /// Llama de partículas: núcleo amarillo que se vuelve naranja y rojo al subir, más un halo suave y (opcional) chispas.
    /// size 1 = antorcha; 0.3 = vela; 2 = brasero.
    /// </summary>
    void Flame(Transform parent, Vector3 pos, float size, Color color, bool sparks = true, bool halo = true)
    {
        var mat = FxMaterial();
        if (mat == null) return;
        var go = new GameObject("Llama");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.rotation = Quaternion.identity;   // siempre hacia arriba aunque el padre esté girado
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true; main.playOnAwake = true; main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.28f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f * size, 0.6f * size);
        main.startSize = new ParticleSystem.MinMaxCurve(0.14f * size, 0.24f * size);
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 34f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6f; sh.radius = 0.035f * size;
        sh.rotation = new Vector3(-90f, 0f, 0f);
        var sz = ps.sizeOverLifetime; sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.1f)));
        var col = ps.colorOverLifetime; col.enabled = true;
        Color hot = Color.Lerp(color, new Color(1f, 0.95f, 0.75f), 0.65f);
        Color deep = Color.Lerp(color, new Color(0.8f, 0.15f, 0.05f), 0.4f);
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(hot, 0f), new GradientColorKey(color, 0.35f), new GradientColorKey(deep, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.9f, 0.1f), new GradientAlphaKey(0.5f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f * size; noise.frequency = 2f; noise.scrollSpeed = 1.5f;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();

        if (halo) Halo(go.transform, Vector3.up * 0.06f * size, 1.2f * size, color, 0.22f);
        if (sparks) Sparks(go.transform, size, color);
    }

    /// <summary>Halo suave (una partícula fija y grande): el resplandor del aire alrededor de una llama.</summary>
    void Halo(Transform parent, Vector3 pos, float size, Color color, float alpha)
    {
        var mat = FxMaterial();
        if (mat == null) return;
        var go = new GameObject("Halo");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true; main.playOnAwake = true; main.prewarm = true;
        main.startLifetime = 1.2f; main.startSpeed = 0f; main.startSize = size;
        main.startColor = new Color(color.r, color.g, color.b, alpha);
        main.maxParticles = 3;
        var em = ps.emission; em.rateOverTime = 2f;
        var sh = ps.shape; sh.enabled = false;
        var sz = ps.sizeOverLifetime; sz.enabled = true;
        sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.9f), new Keyframe(0.5f, 1.05f), new Keyframe(1f, 0.9f)));
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
    }

    void Sparks(Transform parent, float size, Color color)
    {
        var mat = FxMaterial();
        var go = new GameObject("Chispas");
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true; main.playOnAwake = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.3f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f * size, 1.4f * size);
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.045f);
        main.startColor = Color.Lerp(color, new Color(1f, 0.9f, 0.6f), 0.5f);
        main.gravityModifier = -0.05f;
        main.maxParticles = 30;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 3f * size;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 25f; sh.radius = 0.05f * size; sh.rotation = new Vector3(-90f, 0f, 0f);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 1.5f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.6f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
    }

    Light AddLight(Transform parent, Vector3 localPos, Color color, float range, float intensity, bool shadows)
    {
        var lt = NewChild(parent, "Luz").gameObject.AddComponent<Light>();
        lt.transform.localPosition = localPos;
        lt.type = LightType.Point; lt.range = range; lt.intensity = intensity; lt.color = color;
        lt.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        if (shadows) { lt.shadowStrength = 0.85f; lt.shadowNearPlane = 0.2f; }
        return lt;
    }

    // ------------------------------------------------------------------ materiales de los aparatos

    Material BrassMaterial() => SavedMaterial("Laton", () =>
    {
        var m = new Material(LitShader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.78f, 0.6f, 0.3f));
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.9f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.65f);
        return m;
    });

    /// <summary>Vidrio que brilla por dentro (no es transparente: así no hay problemas de orden de dibujo).</summary>
    Material GlassMaterial(Color c) => SavedMaterial("Vidrio_" + ColorUtility.ToHtmlStringRGB(c), () =>
    {
        var m = new Material(LitShader);
        Color tint = Color.Lerp(c, Color.white, 0.5f);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", tint);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.95f);
        m.EnableKeyword("_EMISSION");
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c * 1.6f);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return m;
    });
}
