using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Salas con estructura propia (lo que hace distinto a cada piso más allá de las texturas):
/// columnata, laberinto, foso largo con plataformas móviles y que se desmoronan, pasillo de pinchos,
/// río de lava con piedras y garganta llena de fauces.
/// </summary>
public partial class FloorLayout
{
    void BuildStructureFeature(Transform root, RoomSpec r, List<Gap> gaps, int index, RoomEncounter enc)
    {
        drng = new System.Random(decorSeed * 71 + index * 13 + floorNumber);
        switch (r.feature)
        {
            case RoomFeature.Columnata: BuildColonnade(root, r, gaps); break;
            case RoomFeature.Laberinto: BuildMaze(root, r, gaps); break;
            case RoomFeature.FosoLargo: BuildLongPit(root, r, gaps); break;
            case RoomFeature.Pinchos: BuildSpikeHall(root, r, gaps); break;
            case RoomFeature.RioLava: BuildLavaRiver(root, r, gaps); break;
            case RoomFeature.Garganta: BuildGullet(root, r, gaps); break;
        }
    }

    // ------------------------------------------------------------------ utilidades

    /// <summary>Carriles libres desde cada puerta hacia el centro (para no tapar el paso con columnas o trampas).</summary>
    static bool InDoorLane(RoomSpec r, List<Gap> gaps, Vector3 p, float halfWidth)
    {
        float hx = r.size.x / 2f, hz = r.size.y / 2f;
        foreach (var g in gaps)
        {
            if (g.secret) continue;
            switch (g.side)
            {
                case Side.N: case Side.S: if (Mathf.Abs(p.x - g.offset) < halfWidth && (g.side == Side.N ? p.z > 0f : p.z < 0f)) return true; break;
                default: if (Mathf.Abs(p.z - g.offset) < halfWidth && (g.side == Side.E ? p.x > 0f : p.x < 0f)) return true; break;
            }
        }
        // centro de la sala siempre libre (cruce de carriles)
        return Mathf.Abs(p.x) < halfWidth && Mathf.Abs(p.z) < halfWidth;
    }

    /// <summary>Losa de pinchos con su placa, puntas y trampa.</summary>
    void SpikeAt(Transform parent, Vector3 pos, float size, float offset)
    {
        var t = NewChild(parent, "Pinchos");
        t.localPosition = pos;
        var plate = Prim(t, PrimitiveType.Cube, "Placa", new Vector3(0f, 0.01f, 0f), new Vector3(size - 0.1f, 0.02f, size - 0.1f), GlowMaterial("Glow_Placa", new Color(0.06f, 0.05f, 0.07f)), false);
        plate.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var spikes = NewChild(t, "Puntas");
        spikes.localPosition = Vector3.down * 0.45f;
        var iron = MetalMaterial();
        int n = 3;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                var s = Prim(spikes, PrimitiveType.Cube, "Punta", new Vector3((i - 1) * size / 3.2f, 0.22f, (j - 1) * size / 3.2f), new Vector3(0.12f, 0.45f, 0.12f), iron, false);
                s.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            }
        var trap = t.gameObject.AddComponent<SpikeTrap>();
        trap.size = Vector2.one * size; trap.spikes = spikes; trap.plate = plate.GetComponent<Renderer>(); trap.offset = offset;
    }

    /// <summary>Ráfagas de viento (Galerías): zona + líneas de aire.</summary>
    void WindAt(Transform parent, Vector3 pos, Vector3 size, float strength, float offset)
    {
        var w = NewChild(parent, "Viento");
        w.localPosition = pos;
        var gz = w.gameObject.AddComponent<GustZone>();
        gz.size = size; gz.strength = strength; gz.offset = offset;
        var mat = FxMaterial();
        if (mat == null) return;
        var go = new GameObject("LineasDeAire");
        go.transform.SetParent(w, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true; main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(9f, 14f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
        main.startColor = new Color(0.85f, 0.88f, 1f, 0.35f);
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = size.x * size.z * 0.8f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(0.2f, size.y, Mathf.Max(size.x, size.z));
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat; r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.12f; r.lengthScale = 2f;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        gz.streaks = ps;
    }

    /// <summary>Suelo solo en franjas junto a las puertas (el resto es vacío o lava). Cada franja es un bloque macizo.</summary>
    void CreateBandFloor(Transform root, RoomSpec r, List<Gap> gaps)
    {
        float hx = r.size.x / 2f, hz = r.size.y / 2f, d = 3.5f, k = 1f / tileMeters, t = colliderThickness;
        float depth = r.feature == RoomFeature.RioLava ? 1.6f : PitDepth;
        foreach (var rect in BandRects(r, gaps, d))
        {
            var c = rect.center;
            CreateFace(root, "Orilla", new Vector3(c.x, 0f, c.y), Vector3.up, rect.width, rect.height, floorMaterial, k, k);
            CreateCollider(root, "Collider_Orilla", new Vector3(c.x, -t / 2f, c.y), new Vector3(rect.width, t, rect.height));
            // caras que dan al vacío (macizas)
            var stone = FoundationMaterial();
            if (rect.yMax < hz - 0.01f) CreateFace(root, "CaraOrilla", new Vector3(c.x, -depth / 2f, rect.yMax), Vector3.forward, rect.width, depth, stone, WallK, WallK, true);
            if (rect.yMin > -hz + 0.01f) CreateFace(root, "CaraOrilla", new Vector3(c.x, -depth / 2f, rect.yMin), Vector3.back, rect.width, depth, stone, WallK, WallK, true);
            if (rect.xMax < hx - 0.01f) CreateFace(root, "CaraOrilla", new Vector3(rect.xMax, -depth / 2f, c.y), Vector3.right, rect.height, depth, stone, WallK, WallK, true);
            if (rect.xMin > -hx + 0.01f) CreateFace(root, "CaraOrilla", new Vector3(rect.xMin, -depth / 2f, c.y), Vector3.left, rect.height, depth, stone, WallK, WallK, true);
        }
        // los muros de la sala siguen hacia abajo
        var dark = LitMaterial("PiedraFoso_" + palette.key, Color.Lerp(CapColor(), Color.black, 0.45f));
        CreateFace(root, "ParedFosoN", new Vector3(0f, -depth / 2f, hz), Vector3.back, r.size.x, depth, dark, WallK, WallK);
        CreateFace(root, "ParedFosoS", new Vector3(0f, -depth / 2f, -hz), Vector3.forward, r.size.x, depth, dark, WallK, WallK);
        CreateFace(root, "ParedFosoE", new Vector3(hx, -depth / 2f, 0f), Vector3.left, r.size.y, depth, dark, WallK, WallK);
        CreateFace(root, "ParedFosoO", new Vector3(-hx, -depth / 2f, 0f), Vector3.right, r.size.y, depth, dark, WallK, WallK);
    }

    List<Rect> BandRects(RoomSpec r, List<Gap> gaps, float d)
    {
        float hx = r.size.x / 2f, hz = r.size.y / 2f;
        var list = new List<Rect>();
        bool s = false, n = false;
        foreach (var g in gaps) { if (g.side == Side.S) s = true; if (g.side == Side.N) n = true; }
        if (s) list.Add(new Rect(-hx, -hz, r.size.x, d));
        if (n) list.Add(new Rect(-hx, hz - d, r.size.x, d));
        foreach (var g in gaps)
        {
            if (g.secret) continue;
            if (g.side == Side.E) list.Add(new Rect(hx - d, g.offset - 3.5f, d, 7f));
            if (g.side == Side.W) list.Add(new Rect(-hx, g.offset - 3.5f, d, 7f));
        }
        return list;
    }

    /// <summary>Losa flotante maciza (sin columna): para plataformas móviles o que se caen.</summary>
    Transform Slab(Transform parent, Vector3 topCenter, float size, Color rune)
    {
        var p = NewChild(parent, "Losa");
        p.localPosition = topCenter;
        float k = 1f / tileMeters;
        CreateFace(p, "Tapa", new Vector3(0f, 0.01f, 0f), Vector3.up, size, size, floorMaterial, k, k);
        Prim(p, PrimitiveType.Cube, "Cuerpo", new Vector3(0f, -0.35f, 0f), new Vector3(size, 0.7f, size), ThemeStone(), true);
        var under = Prim(p, PrimitiveType.Cube, "Punta", new Vector3(0f, -1f, 0f), new Vector3(size * 0.5f, 0.6f, size * 0.5f), ThemeStone(), false);
        under.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
        RuneRing(p, Vector3.up * 0.02f, size * 0.36f, size * 0.42f, rune, "Runa");
        return p;
    }

    // ------------------------------------------------------------------ columnata

    void BuildColonnade(Transform root, RoomSpec r, List<Gap> gaps)
    {
        var t = NewChild(root, "Columnata");
        float hx = r.size.x / 2f, hz = r.size.y / 2f, h = curWall;
        float step = 5f;
        int spikes = 0;
        for (float x = -hx + 3f; x <= hx - 2.9f; x += step)
            for (float z = -hz + 3f; z <= hz - 2.9f; z += step)
            {
                var p = new Vector3(x, 0f, z);
                if (InDoorLane(r, gaps, p, 2.6f)) continue;
                ThemePillar(t, p, 0.85f, h + 0.5f);
                // entre columnas, losas de pinchos en algunas casillas (piso 1)
                var between = p + new Vector3(step / 2f, 0f, step / 2f);
                if (theme == FloorTheme.Base && between.x < hx - 2f && between.z < hz - 2f && !InDoorLane(r, gaps, between, 2.2f) && drng.NextDouble() < 0.5)
                { SpikeAt(t, between, 2f, (spikes++ % 4) * 0.55f); }
            }
        // braseros altos en las columnas de las esquinas
        foreach (var c in new[] { new Vector2(-1, 1), new Vector2(1, 1) })
            Brazier(t, new Vector3(c.x * (hx - 1.5f), 0f, c.y * (hz - 1.5f)), TorchColorFor(r.type), 1.1f);
        Hint(root, r, "LA GALERÍA DE COLUMNAS", theme == FloorTheme.Base ? "Entre las columnas hay losas que escupen pinchos: espera a que se escondan" : "Las columnas tapan la mirada del Ojo");
    }

    // ------------------------------------------------------------------ laberinto

    void BuildMaze(Transform root, RoomSpec r, List<Gap> gaps)
    {
        var t = NewChild(root, "Laberinto");
        const float cell = 4f;
        int nx = Mathf.Max(3, Mathf.RoundToInt(r.size.x / cell)), nz = Mathf.Max(3, Mathf.RoundToInt(r.size.y / cell));
        float ox = -nx * cell / 2f, oz = -nz * cell / 2f;
        // paredes: east[x,z] = muro entre (x,z) y (x+1,z); north[x,z] = entre (x,z) y (x,z+1)
        var east = new bool[nx, nz]; var north = new bool[nx, nz];
        for (int x = 0; x < nx; x++) for (int z = 0; z < nz; z++) { east[x, z] = x < nx - 1; north[x, z] = z < nz - 1; }
        var seen = new bool[nx, nz];
        var stack = new Stack<Vector2Int>();
        var startCell = DoorCell(r, gaps, Side.S, nx, nz, cell);
        stack.Push(startCell); seen[startCell.x, startCell.y] = true;
        var dirs = new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
        while (stack.Count > 0)
        {
            var c = stack.Peek();
            var options = new List<Vector2Int>();
            foreach (var d in dirs) { var n = c + d; if (n.x >= 0 && n.y >= 0 && n.x < nx && n.y < nz && !seen[n.x, n.y]) options.Add(n); }
            if (options.Count == 0) { stack.Pop(); continue; }
            var next = options[drng.Next(options.Count)];
            if (next.x != c.x) east[Mathf.Min(c.x, next.x), c.y] = false; else north[c.x, Mathf.Min(c.y, next.y)] = false;
            seen[next.x, next.y] = true; stack.Push(next);
        }
        // algunos atajos (bucles): el laberinto no es de un solo camino
        for (int i = 0; i < nx * nz / 6; i++)
        {
            int x = drng.Next(nx), z = drng.Next(nz);
            if (drng.NextDouble() < 0.5) { if (x < nx - 1) east[x, z] = false; } else if (z < nz - 1) north[x, z] = false;
        }

        float mh = theme == FloorTheme.Lujuria ? 2.2f : theme == FloorTheme.Envidia ? 2.6f : 2.8f, thick = 0.6f;
        for (int x = 0; x < nx; x++)
            for (int z = 0; z < nz; z++)
            {
                if (east[x, z]) MazeWall(t, new Vector3(ox + (x + 1) * cell, 0f, oz + (z + 0.5f) * cell), new Vector3(thick, mh, cell + thick));
                if (north[x, z]) MazeWall(t, new Vector3(ox + (x + 0.5f) * cell, 0f, oz + (z + 1) * cell), new Vector3(cell + thick, mh, thick));
            }

        // distancias desde la entrada: el cofre va en el callejón más lejano
        var dist = new int[nx, nz];
        for (int x = 0; x < nx; x++) for (int z = 0; z < nz; z++) dist[x, z] = -1;
        var q = new Queue<Vector2Int>(); q.Enqueue(startCell); dist[startCell.x, startCell.y] = 0;
        Vector2Int far = startCell;
        var deadEnds = new List<Vector2Int>();
        while (q.Count > 0)
        {
            var c = q.Dequeue();
            int open = 0;
            foreach (var d in dirs)
            {
                var n = c + d;
                if (n.x < 0 || n.y < 0 || n.x >= nx || n.y >= nz) continue;
                bool wall = d.x != 0 ? east[Mathf.Min(c.x, n.x), c.y] : north[c.x, Mathf.Min(c.y, n.y)];
                if (wall) continue;
                open++;
                if (dist[n.x, n.y] >= 0) continue;
                dist[n.x, n.y] = dist[c.x, c.y] + 1; q.Enqueue(n);
            }
            if (open == 1 && c != startCell) deadEnds.Add(c);
            if (dist[c.x, c.y] > dist[far.x, far.y]) far = c;
        }
        Vector2Int chestCell = far;
        int best = -1;
        foreach (var d in deadEnds) if (dist[d.x, d.y] > best) { best = dist[d.x, d.y]; chestCell = d; }
        Vector3 CellPos(Vector2Int c) => new Vector3(ox + (c.x + 0.5f) * cell, 0f, oz + (c.y + 0.5f) * cell);
        TreasureChestAt(t, CellPos(chestCell), 30 + floorNumber * 2);
        Candles(t, CellPos(chestCell) + new Vector3(1.2f, 0f, 1.2f), 4, palette.torch, 0.25f);

        // luz entre los pasillos: faroles colgando sobre una casilla de cada dos, y velas en algunos rincones
        for (int x = 0; x < nx; x += 2)
            for (int zc = 0; zc < nz; zc += 2)
                HangingLantern(t, CellPos(new Vector2Int(x, zc)) + Vector3.up * (mh + 1.2f), 0.9f, palette.torch, 6f, 7f, false);
        for (int i = 0; i < 6; i++)
        {
            var c = new Vector2Int(drng.Next(nx), drng.Next(nz));
            Candles(t, CellPos(c) + new Vector3(-1.3f, 0f, -1.3f), 3, palette.torch, 0.2f);
        }
        switch (theme)
        {
            case FloorTheme.Pereza:
                for (int i = 0; i < 4; i++) { var c = deadEnds.Count > i ? deadEnds[(i * 3) % deadEnds.Count] : new Vector2Int(drng.Next(nx), drng.Next(nz)); MistAt(t, CellPos(c), new Vector3(cell, 2f, cell)); }
                FloatingBooks(t, new Vector3(0f, 3.6f, 0f), new Vector3(r.size.x - 4f, 1.2f, r.size.y - 4f), 14);
                Hint(root, r, "EL LABERINTO DE LIBREROS", "Los pasillos sin salida guardan algo… y mucha niebla");
                break;
            case FloorTheme.Envidia:
                t.gameObject.AddComponent<EnvyChase>().size = r.size;
                Hint(root, r, "EL LABERINTO DE ESPEJOS", "La Otra te sigue mientras estés aquí. Si te detienes, te alcanza");
                break;
            case FloorTheme.Lujuria:
                VortexAt(t, CellPos(new Vector2Int(nx / 2, nz / 2)), 3.2f);
                if (deadEnds.Count > 2) VortexAt(t, CellPos(deadEnds[1]), 2.6f);
                Hint(root, r, "EL LABERINTO DE SETOS", "Las rosas del centro atraen. No te pierdas en lo que quieres");
                break;
        }
    }

    Vector2Int DoorCell(RoomSpec r, List<Gap> gaps, Side side, int nx, int nz, float cell)
    {
        foreach (var g in gaps)
            if (g.side == side && !g.secret)
            {
                int i = Mathf.Clamp(Mathf.FloorToInt((g.offset + (side == Side.N || side == Side.S ? nx : nz) * cell / 2f) / cell), 0, (side == Side.N || side == Side.S ? nx : nz) - 1);
                return side == Side.S ? new Vector2Int(i, 0) : side == Side.N ? new Vector2Int(i, nz - 1) : side == Side.W ? new Vector2Int(0, i) : new Vector2Int(nx - 1, i);
            }
        return new Vector2Int(nx / 2, 0);
    }

    /// <summary>Tramo de muro del laberinto: libreros (Pereza), espejos (Envidia) o setos (Lujuria). Macizo y con colisión.</summary>
    void MazeWall(Transform parent, Vector3 basePos, Vector3 size)
    {
        var w = NewChild(parent, "MuroLaberinto");
        w.localPosition = basePos + Vector3.up * size.y / 2f;
        Material side, top;
        switch (theme)
        {
            case FloorTheme.Envidia: side = SavedMaterial("Espejo_" + palette.key, () => { var m = new Material(LitShader); m.SetColor("_BaseColor", Color.Lerp(new Color(0.05f, 0.08f, 0.07f), palette.accent, 0.08f)); m.SetFloat("_Metallic", 1f); m.SetFloat("_Smoothness", 0.98f); return m; }); top = BrassMaterial(); break;
            case FloorTheme.Lujuria: side = HedgeMaterial(); top = HedgeMaterial(); break;
            default: side = wallMaterial; top = LitMaterial("MaderaOscura", new Color(0.26f, 0.16f, 0.09f)); break;
        }
        float k = 1f / 2.5f;
        Vector3 hs = size / 2f;
        CreateFace(w, "Arriba", new Vector3(0f, hs.y, 0f), Vector3.up, size.x, size.z, top, k, k);
        CreateFace(w, "N", new Vector3(0f, 0f, hs.z), Vector3.forward, size.x, size.y, side, k, k, true);
        CreateFace(w, "S", new Vector3(0f, 0f, -hs.z), Vector3.back, size.x, size.y, side, k, k, true);
        CreateFace(w, "E", new Vector3(hs.x, 0f, 0f), Vector3.right, size.z, size.y, side, k, k, true);
        CreateFace(w, "O", new Vector3(-hs.x, 0f, 0f), Vector3.left, size.z, size.y, side, k, k, true);
        w.gameObject.AddComponent<BoxCollider>().size = size;
        if (theme == FloorTheme.Envidia)   // marco dorado arriba y abajo
        {
            Prim(w, PrimitiveType.Cube, "Marco", new Vector3(0f, hs.y - 0.06f, 0f), new Vector3(size.x + 0.04f, 0.12f, size.z + 0.04f), BrassMaterial(), false);
            Prim(w, PrimitiveType.Cube, "Zocalo", new Vector3(0f, -hs.y + 0.1f, 0f), new Vector3(size.x + 0.04f, 0.2f, size.z + 0.04f), BrassMaterial(), false);
        }
        if (theme == FloorTheme.Lujuria && drng.NextDouble() < 0.6)   // rosas en lo alto del seto
            for (int i = 0; i < 4; i++)
                Prim(w, PrimitiveType.Sphere, "Rosa", new Vector3(DR(-hs.x, hs.x), hs.y + 0.05f, DR(-hs.z, hs.z)), Vector3.one * 0.16f, LitMaterial("Rosa", new Color(0.8f, 0.05f, 0.2f)), false);
        w.gameObject.AddComponent<OccluderFade>();
    }

    Material HedgeMaterial() => SavedMaterial("Seto", () =>
    {
        var m = new Material(LitShader);
#if UNITY_EDITOR
        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Exterior/Leaves.png");
        if (tex != null) m.SetTexture("_BaseMap", tex);
#endif
        m.SetColor("_BaseColor", new Color(0.45f, 0.75f, 0.42f));
        m.SetFloat("_Smoothness", 0.1f);
        return m;
    });

    // ------------------------------------------------------------------ foso largo

    /// <summary>
    /// Foso larguísimo (de sur a norte): plataformas en zigzag, algunas se mueven de lado y otras se desmoronan;
    /// un hueco pide el impulso; una plataforma apartada guarda un cofre. En las Galerías sopla el viento.
    /// </summary>
    void BuildLongPit(Transform root, RoomSpec r, List<Gap> gaps)
    {
        var t = NewChild(root, "FosoLargo");
        float hx = r.size.x / 2f, hz = r.size.y / 2f, band = 3.5f;
        Color rune = palette.accent;
        // fondo: bruma que sube
        CreateFace(t, "Fondo", new Vector3(0f, -PitDepth, 0f), Vector3.up, r.size.x, r.size.y, LitMaterial("FondoFoso", new Color(0.05f, 0.03f, 0.09f)), 1f, 1f);
        Motes(t, new Vector3(0f, -PitDepth + 0.5f, 0f), new Vector3(r.size.x - 1f, 0.5f, r.size.y - 1f), rune, 40f, 1.4f, 6f);
        AddLight(t, new Vector3(0f, -PitDepth + 2f, 0f), rune, r.size.y * 0.6f, 14f, false);

        float z = -hz + band + 2f, zEnd = hz - band - 1.2f;
        float[] xs = { -3f, 1.5f, -1f, 3.5f, -2.5f, 2f, -0.5f, 3f, -3f, 1f };
        string[] kinds = { "fija", "fija", "movil", "cae", "fija", "salto", "movil", "cae", "fija", "movil" };
        int i = 0;
        Vector3 lastFixed = Vector3.zero;
        while (z < zEnd && i < 14)
        {
            float x = Mathf.Clamp(xs[i % xs.Length], -hx + 2f, hx - 2f);
            string kind = kinds[i % kinds.Length];
            float y = (i % 3 == 1) ? 0.5f : (i % 3 == 2 ? -0.3f : 0f);
            var pos = new Vector3(x, y, z);
            if (kind == "movil")
            {
                var s = Slab(t, pos, 2.4f, rune);
                var mp = s.gameObject.AddComponent<MovingPlatform>();
                mp.travel = new Vector3(x > 0 ? -4.5f : 4.5f, 0f, 0f); mp.period = 4.5f; mp.phase = i * 0.17f;
            }
            else if (kind == "cae")
            {
                var s = Slab(t, pos, 2.4f, new Color(1f, 0.55f, 0.3f));
                s.gameObject.AddComponent<CrumblePlatform>();
                // grietas visibles: la pista de que se va a caer
                Prim(s, PrimitiveType.Cube, "Grieta", new Vector3(0f, 0.02f, 0f), new Vector3(2.2f, 0.01f, 0.05f), LitMaterial("Grieta", new Color(0.05f, 0.04f, 0.04f)), false).transform.localRotation = Quaternion.Euler(0f, 35f, 0f);
            }
            else
            {
                var p = Platform(t, pos, 2.6f);
                lastFixed = pos;
                if (kind == "salto") RuneRing(p, Vector3.up * 0.03f, 0.3f, 0.45f, new Color(1f, 0.85f, 0.4f), "MarcaImpulso");
            }
            z += kind == "salto" ? 6.4f : 4.4f;   // el hueco después de la plataforma marcada pide el impulso
            i++;
        }
        // plataforma apartada con cofre (desde la mitad del foso, con impulso)
        var side = Platform(t, new Vector3(hx - 1.8f, 1.2f, 0f), 2.2f);
        TreasureChestAt(side, Vector3.zero, 24, -90f);
        if (theme == FloorTheme.Galerias)
        {
            WindAt(t, new Vector3(0f, 0f, -hz * 0.25f), new Vector3(r.size.x, 4f, r.size.y * 0.35f), 2.4f, 0f);
            WindAt(t, new Vector3(0f, 0f, hz * 0.3f), new Vector3(r.size.x, 4f, r.size.y * 0.3f), 2.8f, 2.7f);
        }
        Hint(root, r, "EL FOSO LARGO", "Plataformas que se mueven y otras agrietadas que se caen. Donde brilla la marca dorada, salta + impulso");
    }

    // ------------------------------------------------------------------ pasillo de pinchos

    void BuildSpikeHall(Transform root, RoomSpec r, List<Gap> gaps)
    {
        var t = NewChild(root, "Pinchos");
        float hx = r.size.x / 2f, hz = r.size.y / 2f, s = 2f;
        int row = 0;
        for (float z = -hz + 4f; z <= hz - 4f; z += s, row++)
            for (float x = -hx + 1f; x <= hx - 0.99f; x += s)
                SpikeAt(t, new Vector3(x + s / 2f - 0.0f, 0f, z), s, row * 0.32f);   // la ola avanza hacia el norte
        // monedas como cebo encima de algunas losas (la avaricia rompe el saco… o te pincha)
        for (int i = 0; i < 6; i++)
            CoinPileAt(t, new Vector3(DR(-hx + 1.5f, hx - 1.5f), 0f, DR(-hz + 5f, hz - 5f)), false, 3 + drng.Next(3));
        // estatuas de guardianes vigilando a los lados
        foreach (float sz in new[] { -hz + 2f, hz - 2f })
            foreach (float sx in new[] { -hx + 1f, hx - 1f })
            { var st = NewChild(t, "Estatua"); st.localPosition = new Vector3(sx, 0f, sz); st.localRotation = Quaternion.Euler(0f, sx > 0 ? -90f : 90f, 0f); Statue(st, false); }
        Hint(root, r, "EL PASILLO DE LAS TRAMPAS", "Los pinchos salen en oleadas hacia el norte: avanza detrás de la ola");
    }

    // ------------------------------------------------------------------ río de lava

    void BuildLavaRiver(Transform root, RoomSpec r, List<Gap> gaps)
    {
        var t = NewChild(root, "RioDeLava");
        float hx = r.size.x / 2f, hz = r.size.y / 2f, band = 3.5f;
        // la lava llena el hueco entre las orillas (con el material de magma y costra)
        float lz = r.size.y - 2f * band;
        var lava = CreateFace(t, "Lava", new Vector3(0f, -1.2f, 0f), Vector3.up, r.size.x, lz, LavaSurfaceMaterial(), 1f / 6f, 1f / 6f);
        lava.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var lp = NewChild(t, "Quemadura"); lp.localPosition = new Vector3(0f, -1.6f, 0f);
        var pit = lp.gameObject.AddComponent<LavaPit>(); pit.size = new Vector3(r.size.x, 1.25f, r.size.y - 2f * band + 0.5f);
        Motes(t, new Vector3(0f, -1f, 0f), new Vector3(r.size.x - 1f, 0.2f, r.size.y - 2f * band), new Color(1f, 0.5f, 0.15f), 60f, 1.4f, 2f);
        for (int i = -1; i <= 1; i++) AddLight(t, new Vector3(0f, -0.2f, i * r.size.y * 0.28f), new Color(1f, 0.4f, 0.1f), 12f, 8f, false);

        // piedras para saltar: camino principal en zigzag de sur a norte
        float z = -hz + band + 1.4f, zEnd = hz - band - 1f;
        int n = 0;
        while (z < zEnd)
        {
            float x = (n % 2 == 0 ? -1f : 1f) * (1.2f + (n % 3) * 0.6f);
            Stone(t, new Vector3(x, 0f, z), 1.9f, n % 4 == 3);
            z += 3.3f; n++;
        }
        // ramal hacia la puerta oeste (si hay)
        foreach (var g in gaps)
            if (g.side == Side.W)
                for (float x = -2.5f; x > -hx + band; x -= 3.2f)
                    Stone(t, new Vector3(x, 0f, g.offset + (x < -6f ? 0.8f : -0.6f)), 1.8f, false);
        Hint(root, r, "EL RÍO DE LAVA", "Salta de piedra en piedra. Las que humean se hunden");
    }

    /// <summary>Piedra de basalto que asoma de la lava (maciza). Las que humean se hunden al pisarlas.</summary>
    void Stone(Transform parent, Vector3 top, float size, bool sinks)
    {
        var s = NewChild(parent, sinks ? "PiedraQueSeHunde" : "Piedra");
        s.localPosition = top;
        var k = 1f / tileMeters;
        CreateFace(s, "Tapa", Vector3.up * 0.01f, Vector3.up, size, size, floorMaterial, k, k);
        Prim(s, PrimitiveType.Cube, "Cuerpo", new Vector3(0f, -0.9f, 0f), new Vector3(size, 1.8f, size), ThemeStone(), true);
        if (sinks)
        {
            Motes(s, Vector3.up * 0.1f, new Vector3(size * 0.8f, 0.1f, size * 0.8f), new Color(0.6f, 0.6f, 0.6f), 6f, 0.8f, 1.5f);
            var cp = s.gameObject.AddComponent<CrumblePlatform>(); cp.top = Vector2.one * size; cp.delay = 0.9f;
        }
    }

    // ------------------------------------------------------------------ garganta (Gula)

    void BuildGullet(Transform root, RoomSpec r, List<Gap> gaps)
    {
        var t = NewChild(root, "Garganta");
        float hx = r.size.x / 2f, hz = r.size.y / 2f;
        int row = 0;
        for (float z = -hz + 4f; z <= hz - 3.5f; z += 3.6f, row++)
            for (float x = -hx + 3f; x <= hx - 2.9f; x += 4f)
                MawAt(t, new Vector3(x + (row % 2) * 1.5f, 0f, z), row * 0.7f + (x > 0 ? 0.35f : 0f));
        for (int i = 0; i < 4; i++) FoodAt(t, new Vector3(DR(-hx + 1f, hx - 1f), 0f, DR(-hz + 2f, hz - 2f)), i);
        for (int i = 0; i < 3; i++) HangingHam(t, new Vector3(DR(-hx + 2f, hx - 2f), curWall - 0.5f, DR(-hz + 2f, hz - 2f)));
        Hint(root, r, "LA GARGANTA", "El suelo tiene hambre: las fauces muerden en oleadas");
    }
}
