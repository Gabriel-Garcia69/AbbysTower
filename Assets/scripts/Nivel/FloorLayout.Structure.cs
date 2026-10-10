using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Estructura vertical de los pisos: escaleras normales (pasillos que suben o bajan entre salas a distinta altura,
/// escalinatas dentro de las salas), cimientos bajo las salas elevadas y los materiales de cada tema.
/// </summary>
public partial class FloorLayout
{
    [Header("Texturas")]
    [Tooltip("Metros que cubre una repetición de la textura de los muros (0 = igual que el suelo).")]
    public float wallTileMeters = 0f;

    float WallK => 1f / (wallTileMeters > 0.01f ? wallTileMeters : tileMeters);

    float LowestElevation
    {
        get
        {
            float lowest = 0f;
            if (rooms != null) foreach (var r in rooms) lowest = Mathf.Min(lowest, r.elevation);
            return lowest;
        }
    }

    // ------------------------------------------------------------------ escaleras

    const float StepRise = 0.2f;

    /// <summary>
    /// Pasillo-escalera entre dos salas a distinta altura: descanso, tramo de escalones (con rampa invisible para caminar
    /// suave) y descanso. Las puertas quedan al nivel del suelo de cada sala.
    /// </summary>
    void CreateStairCorridor(Transform parent, RoomSpec a, RoomSpec b, Side sideFromA, float width)
    {
        bool alongZ = sideFromA == Side.N || sideFromA == Side.S;
        Vector3 start, end;
        if (alongZ)
        {
            float sgn = sideFromA == Side.N ? 1f : -1f;
            start = new Vector3(a.center.x, a.elevation, a.center.y + sgn * a.size.y / 2f);
            end = new Vector3(b.center.x, b.elevation, b.center.y - sgn * b.size.y / 2f);
        }
        else
        {
            float sgn = sideFromA == Side.E ? 1f : -1f;
            start = new Vector3(a.center.x + sgn * a.size.x / 2f, a.elevation, a.center.y);
            end = new Vector3(b.center.x - sgn * b.size.x / 2f, b.elevation, b.center.y);
        }
        Vector3 flat = end - start; flat.y = 0f;
        float len = flat.magnitude;
        if (len < 2.5f) { Debug.LogWarning($"FloorLayout: el pasillo-escalera {a.name}→{b.name} es muy corto.", this); return; }
        float rise = b.elevation - a.elevation;
        float landing = Mathf.Min(1.2f, len * 0.15f);
        float run = len - 2f * landing;
        if (Mathf.Abs(rise) / run > 0.8f) Debug.LogWarning($"FloorLayout: la escalera {a.name}→{b.name} es muy empinada ({Mathf.Abs(rise):0.0} m en {run:0.0} m).", this);

        var root = NewChild(parent, $"Escalera {a.name.Split(' ')[0]}-{b.name.Split(' ')[0]}");
        root.localPosition = new Vector3((start.x + end.x) / 2f, a.elevation, (start.z + end.z) / 2f);
        root.localRotation = Quaternion.LookRotation(flat.normalized);   // +Z local = de A hacia B
        float h = wallHeight, t = colliderThickness, k = 1f / tileMeters;
        float lo = Mathf.Min(0f, rise), hi = Mathf.Max(0f, rise);

        // descansos
        var la = NewChild(root, "DescansoA"); la.localPosition = new Vector3(0f, 0f, -len / 2f + landing / 2f);
        CreateFloor(la, width, landing);
        var lb = NewChild(root, "DescansoB"); lb.localPosition = new Vector3(0f, rise, len / 2f - landing / 2f);
        CreateFloor(lb, width, landing);

        StairRun(root, new Vector3(0f, 0f, -len / 2f + landing), Vector3.forward, run, rise, width, false);

        // muros laterales (tan altos como hace falta para cubrir los dos niveles) con tapa
        float wh = hi - lo + h, wy = lo + wh / 2f;
        foreach (float sx in new[] { -1f, 1f })
        {
            var inward = new Vector3(-sx, 0f, 0f);
            CreateFace(root, sx > 0 ? "WallE" : "WallW", new Vector3(sx * width / 2f, wy, 0f), inward, len, wh, wallMaterial, WallK, WallK, true);
            AddCap(root, new Vector3(sx * width / 2f, lo + wh, 0f), inward, len);
            CreateCollider(root, "Collider_Muro", new Vector3(sx * (width / 2f + t / 2f), wy + 1f, 0f), new Vector3(t, wh + 2f, len));
        }
        if (torches) CorridorLights(root, true, width, len, 0f, rise);

        var reveal = root.gameObject.AddComponent<AreaReveal>();
        reveal.size = new Vector2(width, len);
        reveal.yMin = lo - 2.5f; reveal.yMax = hi + h + 2f; reveal.margin = 2f;
    }

    /// <summary>
    /// Tramo de escalones macizos desde 'start' (borde de abajo… o de arriba si rise &lt; 0) avanzando 'run' metros en 'dir'.
    /// Cada escalón tiene huella (material del suelo) y contrahuella (piedra). Colisión: una rampa lisa.
    /// </summary>
    Transform StairRun(Transform parent, Vector3 start, Vector3 dir, float run, float rise, float width, bool sides)
    {
        if (rise < 0f)   // bajar = la misma escalera vista desde el otro extremo
        {
            start = start + dir * run + Vector3.up * rise;
            dir = -dir; rise = -rise;
        }
        var s = NewChild(parent, "Escalones");
        s.localPosition = start;
        s.localRotation = Quaternion.LookRotation(dir);
        int n = Mathf.Max(2, Mathf.RoundToInt(rise / StepRise));
        float depth = run / n, hStep = rise / n, k = 1f / tileMeters;
        var riserMat = CapMaterial();
        var nose = MetalMaterial();
        for (int i = 0; i < n; i++)
        {
            float z0 = i * depth, top = (i + 1) * hStep;
            CreateFace(s, "Huella", new Vector3(0f, top, z0 + depth / 2f), Vector3.up, width, depth, floorMaterial, k, k);
            CreateFace(s, "Contrahuella", new Vector3(0f, top - hStep / 2f, z0), Vector3.back, width, hStep, riserMat, k, k);
            // canto: listón fino que marca cada escalón (se lee la escalera aunque esté a contraluz)
            var edge = Prim(s, PrimitiveType.Cube, "Canto", new Vector3(0f, top - 0.015f, z0 + 0.03f), new Vector3(width, 0.03f, 0.06f), nose, false);
            edge.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (sides)
                foreach (float sx in new[] { -1f, 1f })
                    CreateFace(s, "Lado", new Vector3(sx * width / 2f, top / 2f, z0 + depth / 2f), new Vector3(sx, 0f, 0f), depth, top, wallMaterial, WallK, WallK, true);
        }
        // rampa: la superficie pasa por el borde de cada escalón
        float L = Mathf.Sqrt(run * run + rise * rise), ang = Mathf.Atan2(rise, run) * Mathf.Rad2Deg, thick = 0.4f;
        var ramp = new GameObject("Rampa");
        ramp.transform.SetParent(s, false);
        Vector3 mid = new Vector3(0f, rise / 2f, run / 2f);
        Vector3 normal = Quaternion.Euler(-ang, 0f, 0f) * Vector3.up;
        ramp.transform.localPosition = mid - normal * (thick / 2f);
        ramp.transform.localRotation = Quaternion.Euler(-ang, 0f, 0f);
        ramp.AddComponent<BoxCollider>().size = new Vector3(width, thick, L + 0.2f);
        return s;
    }

    /// <summary>
    /// Escalinata: la puerta norte de la sala está en alto (otra sala más arriba). Una escalinata ancha de piedra sube
    /// pegada al muro oeste hasta una cornisa a lo largo del muro norte. Debajo de la cornisa queda un hueco oscuro
    /// (buen lugar para esconder cosas).
    /// </summary>
    void BuildGrandStair(Transform root, RoomSpec r, List<Gap> gaps)
    {
        var a = NewChild(root, "Escalinata");
        float hx = r.size.x / 2f, hz = r.size.y / 2f;
        float top = 0f;
        foreach (var g in gaps) top = Mathf.Max(top, g.elevation);
        if (top < 0.5f) return;
        float ledgeDepth = 2.6f, stairW = 2.6f;
        float ledgeZ = hz - ledgeDepth;
        Block(a, "Cornisa", new Vector3(-hx, 0f, ledgeZ), new Vector3(hx, top, hz), new Vector2(hx, hz));
        float run = Mathf.Min(top / 0.62f, r.size.y - ledgeDepth - 1.5f);
        // la escalinata llega a la cornisa desde el sur, pegada al muro oeste
        var stair = StairRun(a, new Vector3(-hx + stairW / 2f + 0.05f, 0f, ledgeZ - run), Vector3.forward, run, top, stairW, true);
        // barandal del lado abierto: postes y pasamanos de metal
        var rail = NewChild(a, "Barandal");
        for (int i = 0; i <= 5; i++)
        {
            float t = i / 5f;
            var p = new Vector3(-hx + stairW + 0.1f, t * top, ledgeZ - run + t * run);
            Prim(rail, PrimitiveType.Cube, "Poste", p + Vector3.up * 0.5f, new Vector3(0.07f, 1f, 0.07f), MetalMaterial(), false);
        }
        var hand = Prim(rail, PrimitiveType.Cube, "Pasamanos", new Vector3(-hx + stairW + 0.1f, top / 2f + 1f, ledgeZ - run / 2f), new Vector3(0.06f, 0.06f, Mathf.Sqrt(run * run + top * top)), MetalMaterial(), false);
        hand.transform.localRotation = Quaternion.Euler(-Mathf.Atan2(top, run) * Mathf.Rad2Deg, 0f, 0f);
        // barandal de la cornisa (que no parezca que se cae uno al vacío)
        for (float x = -hx + stairW + 0.6f; x < hx - 0.3f; x += 1.2f)
            Prim(rail, PrimitiveType.Cube, "Poste", new Vector3(x, top + 0.5f, ledgeZ + 0.1f), new Vector3(0.07f, 1f, 0.07f), MetalMaterial(), false);
        Prim(rail, PrimitiveType.Cube, "Pasamanos", new Vector3((stairW + 0.15f) / 2f, top + 1f, ledgeZ + 0.1f), new Vector3(r.size.x - stairW - 0.6f, 0.06f, 0.06f), MetalMaterial(), false);
        CreateCollider(rail, "Collider_Barandal", new Vector3((stairW + 0.15f) / 2f, top + 0.6f, ledgeZ + 0.1f), new Vector3(r.size.x - stairW - 0.6f, 1.2f, 0.15f));
        // velas en los escalones de abajo
        if (decorations) Candles(a, new Vector3(-hx + stairW + 0.5f, 0f, ledgeZ - run - 0.4f), 5, theme == FloorTheme.Base ? new Color(1f, 0.7f, 0.4f) : palette.torch, 0.3f);
    }

    /// <summary>Cimiento de piedra bajo una sala elevada: la sala parece un bloque macizo y no una caja flotando.</summary>
    void Foundation(Transform root, RoomSpec r)
    {
        float depth = r.elevation - LowestElevation;
        float hx = r.size.x / 2f + colliderThickness, hz = r.size.y / 2f + colliderThickness;
        var f = NewChild(root, "Cimiento");
        var stone = FoundationMaterial();
        CreateFace(f, "N", new Vector3(0f, -depth / 2f, hz), Vector3.forward, r.size.x + colliderThickness * 2f, depth, stone, WallK, WallK, true);
        CreateFace(f, "S", new Vector3(0f, -depth / 2f, -hz), Vector3.back, r.size.x + colliderThickness * 2f, depth, stone, WallK, WallK, true);
        CreateFace(f, "E", new Vector3(hx, -depth / 2f, 0f), Vector3.right, r.size.y + colliderThickness * 2f, depth, stone, WallK, WallK, true);
        CreateFace(f, "O", new Vector3(-hx, -depth / 2f, 0f), Vector3.left, r.size.y + colliderThickness * 2f, depth, stone, WallK, WallK, true);
    }

    Material FoundationMaterial() => SavedMaterial("Cimiento_" + palette.key, () =>
    {
        var m = wallMaterial != null ? new Material(wallMaterial) : new Material(LitShader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.45f, 0.45f, 0.5f));
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
        return m;
    });

    // ------------------------------------------------------------------ materiales del tema

    /// <summary>Suelo y muros con las texturas realistas del tema (Assets/Textures/Pisos/&lt;tema&gt;_floor|wall_*.png).</summary>
    void ApplyThemeMaterials()
    {
#if UNITY_EDITOR
        var f = ThemeSurface(palette.key + "_floor", palette.floorSmooth, palette.emissiveFloor);
        var w = ThemeSurface(palette.key + "_wall", palette.wallSmooth, palette.emissiveWall);
        if (f != null)
        {
            floorMaterial = f;
            tileMeters = palette.floorTile;
            outsideMaterial = SavedMaterial("Exterior_" + palette.key, () =>
            {
                var m = new Material(f);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.34f));
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", Color.black);
                m.DisableKeyword("_EMISSION");
                return m;
            });
        }
        if (w != null) { wallMaterial = w; wallTileMeters = palette.wallTile; }
#endif
    }

#if UNITY_EDITOR
    const string ThemeTextures = "Assets/Textures/Pisos/";

    Material ThemeSurface(string key, float smooth, bool emissive)
    {
        var albedo = LoadThemeTexture(key + "_albedo", TextureImporterType.Default, true);
        if (albedo == null) return null;
        var normal = LoadThemeTexture(key + "_normal", TextureImporterType.NormalMap, false);
        var mask = LoadThemeTexture(key + "_mask", TextureImporterType.Default, false);
        var emission = emissive ? LoadThemeTexture(key + "_emission", TextureImporterType.Default, true) : null;
        return SavedMaterial("Superficie_" + key, () =>
        {
            var m = new Material(LitShader);
            m.SetTexture("_BaseMap", albedo);
            m.SetColor("_BaseColor", Color.white);
            if (normal != null) { m.SetTexture("_BumpMap", normal); m.SetFloat("_BumpScale", 1f); m.EnableKeyword("_NORMALMAP"); }
            if (mask != null) { m.SetTexture("_MetallicGlossMap", mask); m.EnableKeyword("_METALLICSPECGLOSSMAP"); m.SetFloat("_Metallic", 1f); }
            m.SetFloat("_Smoothness", smooth);
            if (emission != null)
            {
                m.SetTexture("_EmissionMap", emission);
                m.SetColor("_EmissionColor", new Color(2.2f, 2.2f, 2.2f));
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            return m;
        });
    }

    /// <summary>Carga una textura del tema y le pone la importación correcta (normal map, lineal, filtro, mipmaps).</summary>
    static Texture2D LoadThemeTexture(string name, TextureImporterType type, bool srgb)
    {
        string path = ThemeTextures + name + ".png";
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return null;
        bool dirty = ti.textureType != type || (type != TextureImporterType.NormalMap && ti.sRGBTexture != srgb) || ti.filterMode != FilterMode.Trilinear || !ti.mipmapEnabled || ti.anisoLevel < 4;
        if (dirty)
        {
            ti.textureType = type;
            if (type != TextureImporterType.NormalMap) ti.sRGBTexture = srgb;
            ti.filterMode = FilterMode.Trilinear;
            ti.mipmapEnabled = true;
            ti.anisoLevel = 4;
            ti.wrapMode = TextureWrapMode.Repeat;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
#endif
}
