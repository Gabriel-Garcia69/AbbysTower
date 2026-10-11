using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pasillos entre salas. Un pasillo es recto y sale de cualquier punto del muro (offset), así una sala puede tener
/// varias puertas en el mismo lado y las salas no tienen que estar alineadas por el centro.
/// Tipos: pasillo con muros, pasillo-escalera (sube o baja) y puente abierto sobre el vacío.
/// </summary>
public partial class FloorLayout
{
    struct CorrGeo
    {
        public bool ok, alongZ;
        public Side sideA, sideB;
        public Vector3 start, end;    // puntos sobre el muro de A y de B (y = altura del suelo de cada sala)
        public float offA, offB;      // posición de la puerta a lo largo del muro de cada sala
        public float len;
    }

    /// <summary>Geometría de un pasillo: hacia dónde sale, dónde están las puertas y cuánto mide.</summary>
    CorrGeo Geo(CorridorSpec c)
    {
        var g = new CorrGeo();
        if (c.from < 0 || c.to < 0 || c.from >= rooms.Length || c.to >= rooms.Length || c.from == c.to) return g;
        RoomSpec a = rooms[c.from], b = rooms[c.to];
        float aMinX = a.center.x - a.size.x / 2f, aMaxX = a.center.x + a.size.x / 2f, aMinZ = a.center.y - a.size.y / 2f, aMaxZ = a.center.y + a.size.y / 2f;
        float bMinX = b.center.x - b.size.x / 2f, bMaxX = b.center.x + b.size.x / 2f, bMinZ = b.center.y - b.size.y / 2f, bMaxZ = b.center.y + b.size.y / 2f;
        bool zSep = bMinZ >= aMaxZ - 0.01f || bMaxZ <= aMinZ + 0.01f;
        bool xSep = bMinX >= aMaxX - 0.01f || bMaxX <= aMinX + 0.01f;
        g.alongZ = zSep && (!xSep || Mathf.Abs(b.center.y - a.center.y) >= Mathf.Abs(b.center.x - a.center.x));
        if (!zSep && !xSep) { Debug.LogWarning($"FloorLayout: las salas {a.name} y {b.name} se enciman; el pasillo se omite.", this); return g; }
        float hw = c.width / 2f;
        if (g.alongZ)
        {
            float line = a.center.x + c.offset;
            if (line - hw < Mathf.Max(aMinX, bMinX) + 0.2f || line + hw > Mathf.Min(aMaxX, bMaxX) - 0.2f)
            { Debug.LogWarning($"FloorLayout: el pasillo {a.name}→{b.name} (x={line}) no cabe en el muro de alguna de las dos salas.", this); return g; }
            bool north = b.center.y > a.center.y;
            g.sideA = north ? Side.N : Side.S; g.sideB = north ? Side.S : Side.N;
            g.start = new Vector3(line, a.elevation, north ? aMaxZ : aMinZ);
            g.end = new Vector3(line, b.elevation, north ? bMinZ : bMaxZ);
            g.offA = line - a.center.x; g.offB = line - b.center.x;
        }
        else
        {
            float line = a.center.y + c.offset;
            if (line - hw < Mathf.Max(aMinZ, bMinZ) + 0.2f || line + hw > Mathf.Min(aMaxZ, bMaxZ) - 0.2f)
            { Debug.LogWarning($"FloorLayout: el pasillo {a.name}→{b.name} (z={line}) no cabe en el muro de alguna de las dos salas.", this); return g; }
            bool east = b.center.x > a.center.x;
            g.sideA = east ? Side.E : Side.W; g.sideB = east ? Side.W : Side.E;
            g.start = new Vector3(east ? aMaxX : aMinX, a.elevation, line);
            g.end = new Vector3(east ? bMinX : bMaxX, b.elevation, line);
            g.offA = line - a.center.y; g.offB = line - b.center.y;
        }
        Vector3 flat = g.end - g.start; flat.y = 0f;
        g.len = flat.magnitude;
        g.ok = g.len > 0.01f;
        return g;
    }

    bool ValidCorridor(CorridorSpec c, out Side sideFrom, out Side sideTo)
    {
        var g = Geo(c);
        sideFrom = g.sideA; sideTo = g.sideB;
        return g.ok;
    }

    void BuildCorridor(Transform parent, CorridorSpec c)
    {
        var g = Geo(c);
        if (!g.ok) return;
        RoomSpec a = rooms[c.from], b = rooms[c.to];
        roomHeight = wallHeight;
        if (c.bridge) CreateBridge(parent, c, g);
        else if (c.stairs && Mathf.Abs(a.elevation - b.elevation) > 0.05f) CreateStairCorridor(parent, c, g);
        else CreateWalledCorridor(parent, c, g, Mathf.Max(a.elevation, b.elevation));
    }

    /// <summary>Pasillo recto con muros, tapas y luces. Si va en alto, lleva muro de carga por fuera hasta abajo.</summary>
    void CreateWalledCorridor(Transform parent, CorridorSpec c, CorrGeo g, float elevation)
    {
        RoomSpec a = rooms[c.from], b = rooms[c.to];
        float h = wallHeight, t = colliderThickness, len = g.len, width = c.width;
        var root = NewChild(parent, $"Pasillo {a.name.Split(' ')[0]}-{b.name.Split(' ')[0]}");
        root.localPosition = new Vector3((g.start.x + g.end.x) / 2f, elevation, (g.start.z + g.end.z) / 2f);
        root.localRotation = Quaternion.LookRotation(new Vector3(g.end.x - g.start.x, 0f, g.end.z - g.start.z).normalized);   // +Z = a lo largo
        CreateFloor(root, width, len);
        foreach (float sx in new[] { -1f, 1f })
        {
            var inward = new Vector3(-sx, 0f, 0f);
            CreateFace(root, sx > 0 ? "WallE" : "WallW", new Vector3(sx * width / 2f, h / 2f, 0f), inward, len, h, wallMaterial, WallK, WallK, true);
            AddCap(root, new Vector3(sx * width / 2f, h, 0f), inward, len);
            CreateCollider(root, "Collider_Muro", new Vector3(sx * (width / 2f + t / 2f), h / 2f, 0f), new Vector3(t, h + 2f, len));
        }
        if (ceilings) CorridorCeiling(root, width, len, h);
        float drop = elevation - LowestElevation;
        if (drop > 0.3f) Support(root, width + 2f * t, len, drop);
        if (torches) CorridorLights(root, true, width, len, 0f, 0f);
        var reveal = root.gameObject.AddComponent<AreaReveal>();
        reveal.size = new Vector2(width, len);
        reveal.yMin = -2.5f; reveal.yMax = h + 2f; reveal.margin = 2f; reveal.falloff = 7f;
    }

    /// <summary>Pasillo-escalera: descansos macizos en los extremos y tramo de escalones con lados cerrados.</summary>
    void CreateStairCorridor(Transform parent, CorridorSpec c, CorrGeo g)
    {
        RoomSpec a = rooms[c.from], b = rooms[c.to];
        float len = g.len, width = c.width;
        if (len < 2.5f) { Debug.LogWarning($"FloorLayout: el pasillo-escalera {a.name}→{b.name} es muy corto.", this); return; }
        float rise = b.elevation - a.elevation;
        float landing = Mathf.Min(1.2f, len * 0.15f);
        float run = len - 2f * landing;
        if (Mathf.Abs(rise) / run > 0.8f) Debug.LogWarning($"FloorLayout: la escalera {a.name}→{b.name} es muy empinada ({Mathf.Abs(rise):0.0} m en {run:0.0} m).", this);

        var root = NewChild(parent, $"Escalera {a.name.Split(' ')[0]}-{b.name.Split(' ')[0]}");
        root.localPosition = new Vector3((g.start.x + g.end.x) / 2f, a.elevation, (g.start.z + g.end.z) / 2f);
        root.localRotation = Quaternion.LookRotation(new Vector3(g.end.x - g.start.x, 0f, g.end.z - g.start.z).normalized);
        float h = wallHeight, t = colliderThickness;
        float lo = Mathf.Min(0f, rise), hi = Mathf.Max(0f, rise);
        float floorLo = LowestElevation - a.elevation;   // hasta dónde bajan los cimientos (en local)
        var half = new Vector2(999f, 999f);              // todas las caras de los descansos se dibujan

        // descansos macizos (si están en alto, su base baja hasta el piso más bajo)
        Block(root, "DescansoA", new Vector3(-width / 2f, Mathf.Min(floorLo, 0f) - 0.01f, -len / 2f), new Vector3(width / 2f, 0f, -len / 2f + landing), half, true);
        Block(root, "DescansoB", new Vector3(-width / 2f, Mathf.Min(floorLo, rise) - 0.01f, len / 2f - landing), new Vector3(width / 2f, rise, len / 2f), half, true);
        StairRun(root, new Vector3(0f, 0f, -len / 2f + landing), Vector3.forward, run, rise, width, true, floorLo);

        float wh = hi - lo + h, wy = lo + wh / 2f;
        foreach (float sx in new[] { -1f, 1f })
        {
            var inward = new Vector3(-sx, 0f, 0f);
            CreateFace(root, sx > 0 ? "WallE" : "WallW", new Vector3(sx * width / 2f, wy, 0f), inward, len, wh, wallMaterial, WallK, WallK, true);
            AddCap(root, new Vector3(sx * width / 2f, lo + wh, 0f), inward, len);
            CreateCollider(root, "Collider_Muro", new Vector3(sx * (width / 2f + t / 2f), wy + 1f, 0f), new Vector3(t, wh + 2f, len));
        }
        if (ceilings) CorridorCeiling(root, width, len, lo + wh);
        if (lo - floorLo > 0.3f) { var s = NewChild(root, "Base"); s.localPosition = new Vector3(0f, lo, 0f); Support(s, width + 2f * t, len, lo - floorLo); }
        if (torches) CorridorLights(root, true, width, len, 0f, rise);
        var reveal = root.gameObject.AddComponent<AreaReveal>();
        reveal.size = new Vector2(width, len);
        reveal.yMin = lo - 2.5f; reveal.yMax = hi + h + 2f; reveal.margin = 2f; reveal.falloff = 7f;
    }

    /// <summary>
    /// Puente abierto sobre el vacío: tablero grueso de madera con vigas, barandales de cuerda (con colisión baja),
    /// faroles en postes y arcos de piedra en las entradas. Si las salas están a distinta altura, el tablero sube en rampa.
    /// </summary>
    void CreateBridge(Transform parent, CorridorSpec c, CorrGeo g)
    {
        RoomSpec a = rooms[c.from], b = rooms[c.to];
        float len = g.len, width = c.width, rise = b.elevation - a.elevation;
        var root = NewChild(parent, $"Puente {a.name.Split(' ')[0]}-{b.name.Split(' ')[0]}");
        root.localPosition = new Vector3((g.start.x + g.end.x) / 2f, a.elevation, (g.start.z + g.end.z) / 2f);
        root.localRotation = Quaternion.LookRotation(new Vector3(g.end.x - g.start.x, 0f, g.end.z - g.start.z).normalized);
        float slope = Mathf.Atan2(rise, len) * Mathf.Rad2Deg;
        var deck = NewChild(root, "Tablero");
        deck.localPosition = new Vector3(0f, rise / 2f, 0f);
        deck.localRotation = Quaternion.Euler(-slope, 0f, 0f);
        float L = Mathf.Sqrt(len * len + rise * rise);
        var wood = LitMaterial("MaderaOscura", new Color(0.26f, 0.16f, 0.09f));
        // tablero: tablones (material del suelo) + caja maciza debajo
        CreateFace(deck, "Tablones", new Vector3(0f, 0.005f, 0f), Vector3.up, width, L, floorMaterial, 1f / tileMeters, 1f / tileMeters);
        Prim(deck, PrimitiveType.Cube, "Viga", new Vector3(0f, -0.2f, 0f), new Vector3(width, 0.4f, L), wood, false);
        foreach (float x in new[] { -width / 2f + 0.15f, width / 2f - 0.15f })
            Prim(deck, PrimitiveType.Cube, "Larguero", new Vector3(x, -0.55f, 0f), new Vector3(0.3f, 0.4f, L), wood, false);
        for (float z = -L / 2f + 1f; z < L / 2f - 0.5f; z += 2f)
            Prim(deck, PrimitiveType.Cube, "Travesano", new Vector3(0f, -0.5f, z), new Vector3(width + 0.4f, 0.25f, 0.3f), wood, false);
        var col = deck.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, -0.2f, 0f); col.size = new Vector3(width, 0.4f, L + 0.2f);
        // barandales: postes, cuerda y colisión baja (no se cae una por accidente; el viento sí empuja)
        var rope = LitMaterial("Cuerda", new Color(0.6f, 0.5f, 0.35f));
        foreach (float sx in new[] { -1f, 1f })
        {
            float x = sx * (width / 2f - 0.1f);
            for (float z = -L / 2f + 0.3f; z <= L / 2f - 0.2f; z += 2f)
                Prim(deck, PrimitiveType.Cube, "Poste", new Vector3(x, 0.55f, z), new Vector3(0.12f, 1.1f, 0.12f), wood, false);
            Prim(deck, PrimitiveType.Cube, "Cuerda", new Vector3(x, 1.0f, 0f), new Vector3(0.05f, 0.05f, L), rope, false);
            Prim(deck, PrimitiveType.Cube, "Cuerda", new Vector3(x, 0.55f, 0f), new Vector3(0.04f, 0.04f, L), rope, false);
            var rc = deck.gameObject.AddComponent<BoxCollider>();
            rc.center = new Vector3(x, 0.6f, 0f); rc.size = new Vector3(0.2f, 1.2f, L);
        }
        // arcos de piedra en las dos entradas
        foreach (float sz in new[] { -1f, 1f })
        {
            float y = sz < 0 ? 0f : rise;
            var arch = NewChild(root, "Arco");
            arch.localPosition = new Vector3(0f, y, sz * (len / 2f - 0.3f));
            foreach (float sx in new[] { -1f, 1f })
                Prim(arch, PrimitiveType.Cube, "Jamba", new Vector3(sx * (width / 2f + 0.25f), wallHeight / 2f, 0f), new Vector3(0.5f, wallHeight, 0.6f), ThemeStone(), true);
            Prim(arch, PrimitiveType.Cube, "Dintel", new Vector3(0f, wallHeight + 0.2f, 0f), new Vector3(width + 1f, 0.4f, 0.7f), ThemeStone(), false);
        }
        // faroles en postes
        Color lc = theme == FloorTheme.Base ? new Color(1f, 0.72f, 0.45f) : palette.corridor;
        int n = Mathf.Max(1, Mathf.RoundToInt(L / 7f));
        for (int i = 0; i < n; i++)
        {
            float z = ((i + 0.5f) / n - 0.5f) * L;
            float sx = i % 2 == 0 ? 1f : -1f;
            var post = NewChild(deck, "PosteFarol");
            post.localPosition = new Vector3(sx * (width / 2f + 0.15f), 0f, z);
            Prim(post, PrimitiveType.Cube, "Asta", new Vector3(0f, 1.3f, 0f), new Vector3(0.12f, 2.6f, 0.12f), wood, false);
            Prim(post, PrimitiveType.Cube, "Brazo", new Vector3(-sx * 0.35f, 2.55f, 0f), new Vector3(0.7f, 0.08f, 0.08f), wood, false);
            HangingLantern(post, new Vector3(-sx * 0.65f, 2.1f, 0f), 0.45f, lc, 6f, 9f, false);
        }
        if (theme == FloorTheme.Galerias) WindAt(root, new Vector3(0f, rise / 2f, 0f), new Vector3(width + 1f, 3f, L), 2.6f, 2.2f);
        var reveal = root.gameObject.AddComponent<AreaReveal>();
        reveal.size = new Vector2(width + 2f, len);
        reveal.yMin = Mathf.Min(0f, rise) - 3f; reveal.yMax = Mathf.Max(0f, rise) + 5f; reveal.margin = 3f; reveal.falloff = 9f;
    }

    /// <summary>Muro de carga bajo un pasillo en alto: caras por fuera (y en las puntas) hasta el piso más bajo.</summary>
    void Support(Transform root, float width, float len, float drop)
    {
        var s = NewChild(root, "MuroDeCarga");
        var stone = FoundationMaterial();
        foreach (float sx in new[] { -1f, 1f })
            CreateFace(s, "Lado", new Vector3(sx * width / 2f, -drop / 2f, 0f), new Vector3(sx, 0f, 0f), len, drop, stone, WallK, WallK, true);
    }
}
