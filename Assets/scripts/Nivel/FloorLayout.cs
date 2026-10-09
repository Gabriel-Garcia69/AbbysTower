using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public enum RoomType { Inicio, Combate, Descanso, Antesala, Jefe }

[Serializable]
public class RoomSpec
{
    public string name = "Sala";
    public RoomType type = RoomType.Combate;
    [Tooltip("Centro de la sala en el plano (x, z), en metros.")]
    public Vector2 center;
    [Tooltip("Tamaño del área jugable (ancho x, fondo z), en metros.")]
    public Vector2 size = new Vector2(16f, 12f);
    [Tooltip("Cuántos enemigos aparecen al entrar (solo Combate). En Jefe siempre es 1.")]
    public int enemies = 3;
}

[Serializable]
public class CorridorSpec
{
    [Tooltip("Índices de las salas que une. Tienen que estar alineadas en X o en Z.")]
    public int from, to;
    public float width = 4f;
}

/// <summary>
/// Genera un piso completo de la torre: salas + pasillos rectos, muros con huecos para las puertas,
/// puertas de energía que se cierran durante los combates, puntos de aparición y la salida al siguiente piso.
///
/// Los muros visibles son de UNA cara (miran hacia adentro): los que quedan entre la cámara y Abby
/// se vuelven invisibles solos, pero sus colliders siguen bloqueando. Mismo truco que LevelRoom.
///
/// Uso: clic derecho en el componente → "Generar piso". Los datos por defecto son el Piso 1.
/// </summary>
public class FloorLayout : MonoBehaviour
{
    [Header("Salas y pasillos (por defecto: Piso 1)")]
    public RoomSpec[] rooms = DefaultFloor1Rooms();
    public CorridorSpec[] corridors = DefaultFloor1Corridors();

    [Header("Medidas")]
    public float wallHeight = 3f;
    public float colliderThickness = 1f;
    [Tooltip("Metros que cubre una repetición de la textura (128 px = 2 m a 64 px por metro).")]
    public float tileMeters = 2f;

    [Header("Materiales (si faltan se usan los de Assets/Materials)")]
    public Material floorMaterial;
    public Material wallMaterial;
    public Material doorMaterial;
    [Tooltip("Suelo oscuro bajo todo el piso: es lo que ve la cámara cuando queda fuera de una sala.")]
    public Material outsideMaterial;
    public float outsideMargin = 30f;

    [Header("Iluminación")]
    [Tooltip("Antorchas en los muros de cada sala (el color depende del tipo de sala).")]
    public bool torches = true;
    public float torchRange = 10f;
    public float torchIntensity = 16f;
    [Tooltip("Luz colgante en el centro de cada sala (da la sombra principal de los personajes).")]
    public float centerLightIntensity = 14f;
    [Tooltip("Cuántas antorchas por sala proyectan sombra (cuestan rendimiento).")]
    public int shadowTorchesPerRoom = 1;

    [Header("Enemigos (vacío = las salas no se cierran todavía)")]
    public GameObject enemyPrefab;
    public GameObject bossPrefab;

    [Header("Decoración")]
    [Tooltip("Pilares, alfombras, estandartes, escombros, brasas flotando, arena y trono del jefe, mercader y santuario.")]
    public bool decorations = true;
    public int decorSeed = 11;

    [Header("Salida")]
    [Tooltip("Escena que se carga al usar la salida (tiene que estar en Build Settings). Vacío = pantalla de 'Piso completado' y vuelve a returnScene.")]
    public string nextScene = "";
    public string returnScene = "Exterior";
    public string floorTitle = "PISO 1";
    public string floorSubtitle = "La Base de la Torre";

    const string Prefix = "Floor_";

    public static RoomSpec[] DefaultFloor1Rooms() => new[]
    {
        new RoomSpec { name = "0 Inicio",              type = RoomType.Inicio,   center = new Vector2(0f, 0f),    size = new Vector2(12f, 10f), enemies = 0 },
        new RoomSpec { name = "1 Combate (horda 1)",   type = RoomType.Combate,  center = new Vector2(0f, 22f),   size = new Vector2(18f, 14f), enemies = 3 },
        new RoomSpec { name = "2 Combate (horda 2)",   type = RoomType.Combate,  center = new Vector2(30f, 22f),  size = new Vector2(18f, 14f), enemies = 5 },
        new RoomSpec { name = "3 Descanso / Tienda",   type = RoomType.Descanso, center = new Vector2(30f, 0f),   size = new Vector2(14f, 10f), enemies = 0 },
        new RoomSpec { name = "4 Combate (horda 3)",   type = RoomType.Combate,  center = new Vector2(30f, 46f),  size = new Vector2(22f, 16f), enemies = 7 },
        new RoomSpec { name = "5 Antesala",            type = RoomType.Antesala, center = new Vector2(30f, 66f),  size = new Vector2(10f, 8f),  enemies = 0 },
        new RoomSpec { name = "6 Jefe",                type = RoomType.Jefe,     center = new Vector2(30f, 88f),  size = new Vector2(26f, 20f), enemies = 1 },
    };

    public static CorridorSpec[] DefaultFloor1Corridors() => new[]
    {
        new CorridorSpec { from = 0, to = 1 },
        new CorridorSpec { from = 1, to = 2 },
        new CorridorSpec { from = 2, to = 3 },   // desvío opcional a la tienda
        new CorridorSpec { from = 2, to = 4 },
        new CorridorSpec { from = 4, to = 5 },
        new CorridorSpec { from = 5, to = 6 },
    };

    // ------------------------------------------------------------------ generación

    enum Side { N, S, E, W }

    struct Gap { public Side side; public float offset, width; }

    void Awake()
    {
        // Red de seguridad: si olvidaste generarlo en el editor, se crea al iniciar.
        if (transform.Find(Prefix + "Rooms") == null) Build();
    }

    void OnValidate()
    {
        wallHeight = Mathf.Max(1f, wallHeight);
        colliderThickness = Mathf.Max(0.1f, colliderThickness);
        tileMeters = Mathf.Max(0.25f, tileMeters);
    }

    [ContextMenu("Generar piso")]
    public void Build()
    {
        Clear();
#if UNITY_EDITOR
        LoadDefaultMaterials();
#endif
        var roomsRoot = NewChild(transform, Prefix + "Rooms");
        var corrRoot = NewChild(transform, Prefix + "Corridors");

        // 1) huecos en los muros de cada sala, según los pasillos que llegan
        var gaps = new List<Gap>[rooms.Length];
        for (int i = 0; i < rooms.Length; i++) gaps[i] = new List<Gap>();
        foreach (var c in corridors)
        {
            if (!ValidCorridor(c, out Side sideFrom, out Side sideTo)) continue;
            // los pasillos son rectos y salen del centro del muro (las salas están alineadas)
            gaps[c.from].Add(new Gap { side = sideFrom, width = c.width, offset = 0f });
            gaps[c.to].Add(new Gap { side = sideTo, width = c.width, offset = 0f });
        }

        // 2) salas
        var roomObjects = new Transform[rooms.Length];
        var encounters = new RoomEncounter[rooms.Length];
        for (int i = 0; i < rooms.Length; i++)
        {
            RoomSpec r = rooms[i];
            var root = NewChild(roomsRoot, r.name);
            root.localPosition = new Vector3(r.center.x, 0f, r.center.y);
            roomObjects[i] = root;

            CreateFloor(root, r.size.x, r.size.y);
            foreach (Side s in new[] { Side.N, Side.S, Side.E, Side.W })
                CreateWallWithGaps(root, r.size, s, gaps[i].FindAll(g => g.side == s));

            // puertas: una por hueco
            var doors = new List<LevelDoor>();
            foreach (var g in gaps[i]) doors.Add(CreateDoor(root, r.size, g));

            encounters[i] = SetupRoomContent(root, r, doors);
            if (torches) AddTorches(root, r, gaps[i]);
            if (decorations) AddDecor(root, r, gaps[i], i);
        }

        // 3) pasillos
        foreach (var c in corridors)
        {
            if (!ValidCorridor(c, out Side sideFrom, out _)) continue;
            CreateCorridor(corrRoot, rooms[c.from], rooms[c.to], sideFrom, c.width);
        }

        // 4) suelo exterior oscuro, un poco por debajo para no pelear con los pisos
        CreateOutside();

        // 5) salida en la sala del jefe (se activa al derrotarlo)
        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].type != RoomType.Jefe) continue;
            var exit = CreateExit(roomObjects[i], rooms[i]);
            exit.unlockedBy = encounters[i];
        }

        // 6) director del piso: vida y ataque de Abby, HUD, muerte y final del piso
        var systems = NewChild(transform, Prefix + "Systems");
        var director = systems.gameObject.AddComponent<FloorDirector>();
        director.floorTitle = floorTitle;
        director.floorSubtitle = floorSubtitle;
        director.returnScene = returnScene;
        director.fxMaterial = FxMaterial();

#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(gameObject);
#endif
    }

    [ContextMenu("Borrar piso")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject c = transform.GetChild(i).gameObject;
            if (!c.name.StartsWith(Prefix)) continue;
            if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
        }
    }

    /// <summary>Posición (mundo) donde aparece Abby: centro de la sala de Inicio.</summary>
    public Vector3 PlayerSpawn
    {
        get
        {
            foreach (var r in rooms)
                if (r.type == RoomType.Inicio)
                    return transform.TransformPoint(new Vector3(r.center.x, 0f, r.center.y));
            return transform.position;
        }
    }

    bool ValidCorridor(CorridorSpec c, out Side sideFrom, out Side sideTo)
    {
        sideFrom = sideTo = Side.N;
        if (c.from < 0 || c.to < 0 || c.from >= rooms.Length || c.to >= rooms.Length || c.from == c.to)
        {
            Debug.LogWarning($"FloorLayout: pasillo {c.from}→{c.to} con índices inválidos.", this);
            return false;
        }
        Vector2 a = rooms[c.from].center, b = rooms[c.to].center;
        if (Mathf.Abs(a.x - b.x) < 0.01f) { sideFrom = b.y > a.y ? Side.N : Side.S; sideTo = b.y > a.y ? Side.S : Side.N; return true; }
        if (Mathf.Abs(a.y - b.y) < 0.01f) { sideFrom = b.x > a.x ? Side.E : Side.W; sideTo = b.x > a.x ? Side.W : Side.E; return true; }
        Debug.LogWarning($"FloorLayout: las salas {rooms[c.from].name} y {rooms[c.to].name} no están alineadas en X ni en Z; el pasillo se omite.", this);
        return false;
    }

    // ------------------------------------------------------------------ piezas

    void CreateFloor(Transform parent, float w, float d)
    {
        float k = 1f / tileMeters, t = colliderThickness;
        CreateFace(parent, "Floor", Vector3.zero, Vector3.up, w, d, floorMaterial, k, k);
        CreateCollider(parent, "Collider_Floor", new Vector3(0f, -t / 2, 0f), new Vector3(w, t, d));
    }

    /// <summary>Muro de un lado de la sala, partido en tramos para dejar los huecos de las puertas.</summary>
    void CreateWallWithGaps(Transform parent, Vector2 size, Side side, List<Gap> gaps)
    {
        bool ns = side == Side.N || side == Side.S;
        float length = ns ? size.x : size.y;
        float half = length / 2f;

        // tramos sólidos entre huecos (coordenada a lo largo del muro, de -half a +half)
        var cuts = new List<Vector2>();
        foreach (var g in gaps) cuts.Add(new Vector2(g.offset - g.width / 2f, g.offset + g.width / 2f));
        cuts.Sort((p, q) => p.x.CompareTo(q.x));

        float cursor = -half;
        int n = 0;
        foreach (var cut in cuts)
        {
            if (cut.x > cursor) CreateWallSegment(parent, size, side, cursor, cut.x, n++);
            cursor = Mathf.Max(cursor, cut.y);
        }
        if (cursor < half) CreateWallSegment(parent, size, side, cursor, half, n);
    }

    void CreateWallSegment(Transform parent, Vector2 size, Side side, float a, float b, int index)
    {
        float len = b - a;
        if (len < 0.01f) return;
        float mid = (a + b) / 2f, h = wallHeight, t = colliderThickness, k = 1f / tileMeters;
        float hx = size.x / 2f, hz = size.y / 2f;

        Vector3 pos, inward, colCenter, colSize;
        switch (side)
        {
            case Side.N: pos = new Vector3(mid, h / 2, hz);  inward = Vector3.back;    colCenter = new Vector3(mid, h / 2, hz + t / 2);  colSize = new Vector3(len, h + 2f, t); break;
            case Side.S: pos = new Vector3(mid, h / 2, -hz); inward = Vector3.forward; colCenter = new Vector3(mid, h / 2, -hz - t / 2); colSize = new Vector3(len, h + 2f, t); break;
            case Side.E: pos = new Vector3(hx, h / 2, mid);  inward = Vector3.left;    colCenter = new Vector3(hx + t / 2, h / 2, mid);  colSize = new Vector3(t, h + 2f, len); break;
            default:     pos = new Vector3(-hx, h / 2, mid); inward = Vector3.right;   colCenter = new Vector3(-hx - t / 2, h / 2, mid); colSize = new Vector3(t, h + 2f, len); break;
        }
        string id = "Wall" + side + (index > 0 ? "_" + index : "");
        CreateFace(parent, id, pos, inward, len, h, wallMaterial, k, k, true);
        CreateCollider(parent, "Collider_" + id, colCenter, colSize);
    }

    void CreateCorridor(Transform parent, RoomSpec a, RoomSpec b, Side sideFromA, float width)
    {
        float h = wallHeight, t = colliderThickness, k = 1f / tileMeters;
        bool alongZ = sideFromA == Side.N || sideFromA == Side.S;
        Vector3 start, end;   // bordes de las dos salas, en el eje del pasillo
        if (alongZ)
        {
            float sgn = sideFromA == Side.N ? 1f : -1f;
            start = new Vector3(a.center.x, 0f, a.center.y + sgn * a.size.y / 2f);
            end = new Vector3(b.center.x, 0f, b.center.y - sgn * b.size.y / 2f);
        }
        else
        {
            float sgn = sideFromA == Side.E ? 1f : -1f;
            start = new Vector3(a.center.x + sgn * a.size.x / 2f, 0f, a.center.y);
            end = new Vector3(b.center.x - sgn * b.size.x / 2f, 0f, b.center.y);
        }
        float len = Vector3.Distance(start, end);
        if (len < 0.01f) return;

        var root = NewChild(parent, $"Pasillo {a.name.Split(' ')[0]}-{b.name.Split(' ')[0]}");
        root.localPosition = (start + end) / 2f;

        float w = alongZ ? width : len, d = alongZ ? len : width;
        CreateFloor(root, w, d);
        if (torches)
        {
            var lt = NewChild(root, "Luz").gameObject.AddComponent<Light>();
            lt.transform.localPosition = new Vector3(0f, wallHeight - 0.4f, 0f);
            lt.type = LightType.Point; lt.range = Mathf.Max(6f, len * 0.7f); lt.intensity = 6f;
            lt.color = new Color(0.55f, 0.75f, 1f); lt.shadows = LightShadows.None;
        }

        if (alongZ)
        {
            CreateFace(root, "WallE", new Vector3(width / 2, h / 2, 0f), Vector3.left, len, h, wallMaterial, k, k, true);
            CreateFace(root, "WallW", new Vector3(-width / 2, h / 2, 0f), Vector3.right, len, h, wallMaterial, k, k, true);
            CreateCollider(root, "Collider_WallE", new Vector3(width / 2 + t / 2, h / 2, 0f), new Vector3(t, h + 2f, len + 2 * t));
            CreateCollider(root, "Collider_WallW", new Vector3(-width / 2 - t / 2, h / 2, 0f), new Vector3(t, h + 2f, len + 2 * t));
        }
        else
        {
            CreateFace(root, "WallN", new Vector3(0f, h / 2, width / 2), Vector3.back, len, h, wallMaterial, k, k, true);
            CreateFace(root, "WallS", new Vector3(0f, h / 2, -width / 2), Vector3.forward, len, h, wallMaterial, k, k, true);
            CreateCollider(root, "Collider_WallN", new Vector3(0f, h / 2, width / 2 + t / 2), new Vector3(len + 2 * t, h + 2f, t));
            CreateCollider(root, "Collider_WallS", new Vector3(0f, h / 2, -width / 2 - t / 2), new Vector3(len + 2 * t, h + 2f, t));
        }
    }

    LevelDoor CreateDoor(Transform parent, Vector2 size, Gap g)
    {
        float h = wallHeight, hx = size.x / 2f, hz = size.y / 2f;
        Vector3 pos; Quaternion rot;
        switch (g.side)
        {
            case Side.N: pos = new Vector3(g.offset, 0f, hz);  rot = Quaternion.identity; break;
            case Side.S: pos = new Vector3(g.offset, 0f, -hz); rot = Quaternion.identity; break;
            case Side.E: pos = new Vector3(hx, 0f, g.offset);  rot = Quaternion.Euler(0f, 90f, 0f); break;
            default:     pos = new Vector3(-hx, 0f, g.offset); rot = Quaternion.Euler(0f, 90f, 0f); break;
        }
        var door = NewChild(parent, "Puerta" + g.side);
        door.localPosition = pos;
        door.localRotation = rot;

        // barrera visible desde los dos lados (dos caras opuestas) + collider
        var visual = NewChild(door, "Barrera");
        float us = 1f / 2f, vs = 1f / h; // la textura de energía mide 2 m de ancho y cubre el alto una vez
        CreateFace(visual, "CaraA", new Vector3(0f, h / 2, 0f), Vector3.back, g.width, h, doorMaterial, us, vs, false);
        CreateFace(visual, "CaraB", new Vector3(0f, h / 2, 0f), Vector3.forward, g.width, h, doorMaterial, us, vs, false);
        var col = door.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, h / 2, 0f);
        col.size = new Vector3(g.width, h + 2f, 0.5f);

        var ld = door.gameObject.AddComponent<LevelDoor>();
        ld.barrier = visual.gameObject;
        ld.blocker = col;
        var glow = NewChild(door, "Brillo").gameObject.AddComponent<Light>();
        glow.transform.localPosition = new Vector3(0f, h * 0.5f, 0f);
        glow.type = LightType.Point; glow.range = 7f; glow.intensity = 10f;
        glow.color = new Color(0.35f, 0.95f, 1f); glow.shadows = LightShadows.None;
        ld.glow = glow;
        ld.SetClosed(false);
        return ld;
    }

    RoomEncounter SetupRoomContent(Transform root, RoomSpec r, List<LevelDoor> doors)
    {
        switch (r.type)
        {
            case RoomType.Inicio:
            {
                var spawn = NewChild(root, "PlayerSpawn");
                spawn.localPosition = Vector3.zero;
                return null;
            }
            case RoomType.Descanso:
            {
                // Placeholder de la tienda: mostrador al fondo (con decoración se arma el puesto del mercader y el santuario).
                // La economía (recursos → monedas → equipo) va aparte.
                if (!decorations)
                {
                    var counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    counter.name = "Tienda (placeholder)";
                    counter.transform.SetParent(root, false);
                    counter.transform.localPosition = new Vector3(0f, 0.6f, r.size.y / 2f - 1.5f);
                    counter.transform.localScale = new Vector3(4f, 1.2f, 1f);
                }
                var light = NewChild(root, "Luz");
                light.localPosition = new Vector3(0f, 2.5f, 0f);
                var l = light.gameObject.AddComponent<Light>();
                l.type = LightType.Point; l.range = 10f; l.intensity = 3f; l.color = new Color(1f, 0.8f, 0.5f);
                return null;
            }
            case RoomType.Combate:
            case RoomType.Jefe:
            {
                bool boss = r.type == RoomType.Jefe;
                int count = boss ? 1 : Mathf.Max(0, r.enemies);
                var points = NewChild(root, "SpawnPoints");
                var list = new List<Transform>();
                for (int i = 0; i < count; i++)
                {
                    var p = NewChild(points, "Spawn_" + i);
                    p.localPosition = boss ? new Vector3(0f, 0f, r.size.y * 0.25f) : SpawnOffset(i, count, r.size);
                    list.Add(p);
                }
                var enc = root.gameObject.AddComponent<RoomEncounter>();
                enc.size = r.size;
                enc.doors = doors.ToArray();
                enc.spawnPoints = list.ToArray();
                enc.enemyPrefab = boss ? bossPrefab : enemyPrefab;
                enc.isBoss = boss;
                enc.maxAlive = 4;
                if (boss) enc.displayName = "Trono del Guardián";
                else
                {
                    int n = 0;
                    foreach (var other in rooms) { if (other.type == RoomType.Combate) n++; if (other == r) break; }
                    enc.displayName = "Sala de la Horda " + (n <= 3 ? new[] { "I", "II", "III" }[n - 1] : n.ToString());
                }
                return enc;
            }
            default:
                return null;
        }
    }

    static Color RoomLightColor(RoomType t)
    {
        switch (t)
        {
            case RoomType.Inicio:   return new Color(0.6f, 0.8f, 1f);    // frío, tranquilo
            case RoomType.Descanso: return new Color(1f, 0.78f, 0.45f);  // cálido, seguro
            case RoomType.Antesala: return new Color(0.75f, 0.45f, 1f);  // morado, inquietante
            case RoomType.Jefe:     return new Color(1f, 0.3f, 0.2f);    // rojo, peligro
            default:                return new Color(1f, 0.6f, 0.3f);    // antorcha
        }
    }

    /// <summary>Antorchas a 1/4 y 3/4 de los muros N, E y O (el S casi nunca se ve), esquivando los huecos de las puertas.</summary>
    void AddTorches(Transform root, RoomSpec r, List<Gap> gaps)
    {
        Color c = RoomLightColor(r.type);
        float y = wallHeight * 0.7f;
        int shadowsLeft = shadowTorchesPerRoom;

        var center = NewChild(root, "LuzCentral").gameObject.AddComponent<Light>();
        center.transform.localPosition = new Vector3(0f, wallHeight + 2f, -r.size.y * 0.1f);
        center.type = LightType.Point;
        center.range = Mathf.Max(r.size.x, r.size.y) * 0.9f;
        center.intensity = centerLightIntensity * Mathf.Max(1f, Mathf.Max(r.size.x, r.size.y) / 16f);
        center.color = Color.Lerp(c, Color.white, 0.55f);
        center.shadows = LightShadows.Soft;
        foreach (Side side in new[] { Side.N, Side.E, Side.W })
        {
            bool ns = side == Side.N;
            float len = ns ? r.size.x : r.size.y;
            foreach (float along in new[] { -len / 4f, len / 4f })
            {
                bool blocked = false;
                foreach (var g in gaps)
                    if (g.side == side && Mathf.Abs(along - g.offset) < g.width / 2f + 0.8f) blocked = true;
                if (blocked) continue;

                Vector3 wallPos, inward;
                switch (side)
                {
                    case Side.N: wallPos = new Vector3(along, y, r.size.y / 2f); inward = Vector3.back; break;
                    case Side.E: wallPos = new Vector3(r.size.x / 2f, y, along); inward = Vector3.left; break;
                    default:     wallPos = new Vector3(-r.size.x / 2f, y, along); inward = Vector3.right; break;
                }
                var torch = NewChild(root, "Antorcha");
                torch.localPosition = wallPos + inward * 0.15f;

                var flame = GameObject.CreatePrimitive(PrimitiveType.Cube);
                flame.name = "Llama";
                flame.transform.SetParent(torch, false);
                flame.transform.localScale = new Vector3(0.18f, 0.28f, 0.18f);
                DestroyImmediate(flame.GetComponent<Collider>());
                var fr = flame.GetComponent<MeshRenderer>();
                fr.sharedMaterial = TorchMaterialFor(c);
                fr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                var lt = NewChild(torch, "Luz").gameObject.AddComponent<Light>();
                lt.transform.localPosition = inward * 0.5f;
                lt.type = LightType.Point; lt.range = torchRange; lt.intensity = torchIntensity; lt.color = c;
                lt.shadows = shadowsLeft-- > 0 ? LightShadows.Soft : LightShadows.None;
                torch.gameObject.AddComponent<LightFlicker>();
            }
        }
    }

    readonly Dictionary<Color, Material> torchMats = new Dictionary<Color, Material>();

    /// <summary>Material emisivo del color de la sala (se reutiliza; en el editor se guarda en Assets/Materials).</summary>
    Material TorchMaterialFor(Color c)
    {
        if (torchMats.TryGetValue(c, out var m) && m != null) return m;
#if UNITY_EDITOR
        string path = "Assets/Materials/Torch_" + ColorUtility.ToHtmlStringRGB(c) + ".mat";
        m = AssetDatabase.LoadAssetAtPath<Material>(path);
#endif
        if (m == null)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            m = new Material(sh) { name = "Torch" };
            Color hdr = c * 4f; hdr.a = 1f;   // HDR para que el Bloom la haga brillar
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", hdr);
            if (m.HasProperty("_Color")) m.SetColor("_Color", hdr);
#if UNITY_EDITOR
            AssetDatabase.CreateAsset(m, path);
#endif
        }
        torchMats[c] = m;
        return m;
    }

    /// <summary>Reparte los enemigos en un anillo alrededor del centro, lejos de las puertas.</summary>
    static Vector3 SpawnOffset(int i, int count, Vector2 size)
    {
        float ang = (i + 0.5f) / count * Mathf.PI * 2f;
        return new Vector3(Mathf.Cos(ang) * size.x * 0.28f, 0f, Mathf.Sin(ang) * size.y * 0.28f);
    }

    FloorExit CreateExit(Transform room, RoomSpec r)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Salida";
        go.transform.SetParent(room, false);
        go.transform.localPosition = new Vector3(0f, 0.05f, r.size.y / 2f - (decorations ? 4.6f : 2.5f));   // delante del trono
        go.transform.localScale = new Vector3(2.5f, 0.05f, 2.5f);
        DestroyImmediate(go.GetComponent<Collider>());   // no debe bloquear; se detecta por distancia
        var exit = go.AddComponent<FloorExit>();
        exit.nextScene = nextScene;
        exit.radius = 1.4f;

        // haz de luz + chispas que suben (se encienden al derrotar al jefe)
        var fx = NewChild(room, "Salida_Haz");
        fx.localPosition = go.transform.localPosition;
        var beam = Prim(fx, PrimitiveType.Cylinder, "Haz", new Vector3(0f, 4f, 0f), new Vector3(2.2f, 4f, 2.2f), BeamMaterial(), false);
        beam.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var lt = NewChild(fx, "Luz").gameObject.AddComponent<Light>();
        lt.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        lt.type = LightType.Point; lt.range = 9f; lt.intensity = 12f; lt.color = new Color(0.4f, 1f, 0.95f); lt.shadows = LightShadows.None;
        fx.gameObject.AddComponent<LightFlicker>().amount = 0.15f;
        Motes(fx, Vector3.zero, new Vector3(2f, 0.2f, 2f), new Color(0.5f, 1f, 0.95f), 30f, 1.6f, 2.5f);
        exit.unlockedFx = fx.gameObject;
        fx.gameObject.SetActive(false);
        return exit;
    }

    // ------------------------------------------------------------------ decoración

    System.Random drng;
    float DR(float a, float b) => a + (float)drng.NextDouble() * (b - a);

    void AddDecor(Transform root, RoomSpec r, List<Gap> gaps, int index)
    {
        drng = new System.Random(decorSeed * 97 + index);
        var deco = NewChild(root, "Decoracion");
        float hx = r.size.x / 2f, hz = r.size.y / 2f, h = wallHeight;
        Color tint = RoomLightColor(r.type);

        // pilares en las esquinas
        if (Mathf.Min(r.size.x, r.size.y) >= 10f)
            foreach (var c in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
                Pillar(deco, new Vector3(c.x * (hx - 0.9f), 0f, c.y * (hz - 0.9f)), 0.42f, h + 0.3f);

        // brasas flotando por toda la sala, del color de la sala
        Motes(deco, new Vector3(0f, 1.2f, 0f), new Vector3(r.size.x - 2f, 2f, r.size.y - 2f), Color.Lerp(tint, Color.white, 0.3f), 6f, 0.15f, 7f);

        switch (r.type)
        {
            case RoomType.Inicio:
            {
                RuneRing(deco, Vector3.up * 0.02f, 2.1f, 2.4f, new Color(0.45f, 0.8f, 1f), "CirculoInicio");
                RuneRing(deco, Vector3.up * 0.02f, 1.2f, 1.35f, new Color(0.45f, 0.8f, 1f), "CirculoInicio2");
                Crate(deco, new Vector3(-hx + 1.8f, 0f, hz - 1.0f), 0.8f);
                Crate(deco, new Vector3(-hx + 2.7f, 0f, hz - 1.1f), 0.6f);
                Crate(deco, new Vector3(hx - 1.8f, 0f, -hz + 1.6f), 0.7f);
                Banners(deco, r, gaps, new Color(0.2f, 0.35f, 0.7f));
                break;
            }
            case RoomType.Combate:
            {
                Rug(deco, new Vector2(r.size.x * 0.55f, r.size.y * 0.5f), new Color(0.38f, 0.07f, 0.08f), new Color(0.75f, 0.58f, 0.25f));
                Banners(deco, r, gaps, new Color(0.55f, 0.1f, 0.12f));
                Rubble(deco, r, gaps, 7);
                break;
            }
            case RoomType.Descanso:
            {
                MerchantStall(deco, new Vector3(0f, 0f, hz - 1.4f));
                Shrine(deco, new Vector3(0f, 0f, -0.8f));
                Rug(deco, new Vector2(4f, r.size.y * 0.7f), new Color(0.15f, 0.3f, 0.22f), new Color(0.75f, 0.65f, 0.35f));
                Crate(deco, new Vector3(hx - 1.4f, 0f, hz - 1.2f), 0.75f);
                Crate(deco, new Vector3(hx - 1.5f, 0.75f, hz - 1.2f), 0.5f);
                Crate(deco, new Vector3(-hx + 1.4f, 0f, hz - 1.3f), 0.8f);
                break;
            }
            case RoomType.Antesala:
            {
                foreach (float x in new[] { -2.9f, 2.9f }) Brazier(deco, new Vector3(x, 0f, hz - 1.1f), tint, 1f);
                for (int k = 0; k < 3; k++) RuneRing(deco, new Vector3(0f, 0.02f, -hz + 2f + k * 2f), 0.45f, 0.6f, tint, "Runa");
                Rubble(deco, r, gaps, 3);
                break;
            }
            case RoomType.Jefe:
            {
                // arena: anillos de runas, cuatro pilares grandes con braseros y el trono al fondo
                RuneRing(deco, Vector3.up * 0.02f, 7.4f, 7.8f, new Color(1f, 0.25f, 0.2f), "Arena");
                RuneRing(deco, Vector3.up * 0.02f, 3.0f, 3.2f, new Color(1f, 0.25f, 0.2f), "ArenaCentro");
                for (int k = 0; k < 8; k++)
                {
                    float a = k / 8f * Mathf.PI * 2f;
                    var glyph = Prim(deco, PrimitiveType.Cube, "Glifo", new Vector3(Mathf.Cos(a) * 5.3f, 0.02f, Mathf.Sin(a) * 5.3f), new Vector3(0.5f, 0.02f, 0.5f), GlowMaterial("Glow_Jefe", new Color(1f, 0.25f, 0.2f)), false);
                    glyph.transform.localRotation = Quaternion.Euler(0f, 45f + a * Mathf.Rad2Deg, 0f);
                }
                foreach (var c in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
                {
                    var p = new Vector3(c.x * r.size.x * 0.33f, 0f, c.y * r.size.y * 0.3f);
                    Pillar(deco, p, 0.75f, h + 1.5f);
                    Brazier(deco, p + Vector3.up * (h + 1.5f), tint, 0.9f, c.y > 0);
                }
                Throne(deco, new Vector3(0f, 0f, hz - 1.6f));
                Banners(deco, r, gaps, new Color(0.35f, 0.05f, 0.08f));
                Rubble(deco, r, gaps, 6);
                break;
            }
        }
    }

    void Pillar(Transform parent, Vector3 pos, float radius, float height)
    {
        var p = NewChild(parent, "Pilar");
        p.localPosition = pos;
        var shaft = Prim(p, PrimitiveType.Cylinder, "Fuste", new Vector3(0f, height / 2f, 0f), new Vector3(radius * 2f, height / 2f, radius * 2f), StoneMaterial(), true);
        shaft.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        Prim(p, PrimitiveType.Cube, "Base", new Vector3(0f, 0.18f, 0f), new Vector3(radius * 2.7f, 0.36f, radius * 2.7f), StoneMaterial(), false);
        Prim(p, PrimitiveType.Cube, "Capitel", new Vector3(0f, height - 0.15f, 0f), new Vector3(radius * 2.8f, 0.3f, radius * 2.8f), StoneMaterial(), false);
        var ring = Prim(p, PrimitiveType.Cylinder, "Runa", new Vector3(0f, height * 0.62f, 0f), new Vector3(radius * 2.08f, 0.04f, radius * 2.08f), GlowMaterial("Glow_Runa", new Color(0.35f, 0.85f, 1f)), false);
        ring.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void Rug(Transform parent, Vector2 size, Color inner, Color border)
    {
        float k = 1f / tileMeters;
        CreateFace(parent, "Alfombra_Borde", new Vector3(0f, 0.01f, 0f), Vector3.up, size.x, size.y, LitMaterial("Tela_" + ColorUtility.ToHtmlStringRGB(border), border), k, k);
        CreateFace(parent, "Alfombra", new Vector3(0f, 0.015f, 0f), Vector3.up, size.x - 0.5f, size.y - 0.5f, LitMaterial("Tela_" + ColorUtility.ToHtmlStringRGB(inner), inner), k, k);
    }

    /// <summary>Estandartes colgando del muro norte, a los lados (esquivando la puerta y las antorchas).</summary>
    void Banners(Transform parent, RoomSpec r, List<Gap> gaps, Color color)
    {
        float hz = r.size.y / 2f, len = r.size.x;
        var mat = LitMaterial("Tela_" + ColorUtility.ToHtmlStringRGB(color), color);
        var trim = GlowMaterial("Glow_Oro", new Color(1f, 0.75f, 0.35f));
        foreach (float along in new[] { -len / 4f - 1.5f, len / 4f + 1.5f })
        {
            if (Mathf.Abs(along) > len / 2f - 1.8f) continue;
            bool blocked = false;
            foreach (var g in gaps) if (g.side == Side.N && Mathf.Abs(along - g.offset) < g.width / 2f + 0.8f) blocked = true;
            if (blocked) continue;
            float bh = wallHeight * 0.6f;
            CreateFace(parent, "Estandarte", new Vector3(along, wallHeight - 0.25f - bh / 2f, hz - 0.04f), Vector3.back, 1f, bh, mat, 1f, 1f, true);
            CreateFace(parent, "Estandarte_Emblema", new Vector3(along, wallHeight - 0.25f - bh * 0.45f, hz - 0.05f), Vector3.back, 0.35f, 0.35f, trim, 1f, 1f);
            Prim(parent, PrimitiveType.Cube, "Barra", new Vector3(along, wallHeight - 0.22f, hz - 0.08f), new Vector3(1.3f, 0.06f, 0.06f), MetalMaterial(), false);
        }
    }

    void Rubble(Transform parent, RoomSpec r, List<Gap> gaps, int count)
    {
        float hx = r.size.x / 2f, hz = r.size.y / 2f;
        for (int k = 0, tries = 0; k < count && tries < 60; tries++)
        {
            // pegado a un muro (no el sur: casi no se ve) y lejos de las puertas
            int side = drng.Next(3);
            Vector3 p = side == 0 ? new Vector3(DR(-hx + 1.5f, hx - 1.5f), 0f, hz - DR(0.4f, 1.2f))
                      : side == 1 ? new Vector3(hx - DR(0.4f, 1.2f), 0f, DR(-hz + 1.5f, hz - 1.5f))
                                  : new Vector3(-hx + DR(0.4f, 1.2f), 0f, DR(-hz + 1.5f, hz - 1.5f));
            bool nearDoor = false;
            foreach (var g in gaps)
            {
                Vector3 d = g.side == Side.N ? new Vector3(g.offset, 0f, hz) : g.side == Side.S ? new Vector3(g.offset, 0f, -hz)
                          : g.side == Side.E ? new Vector3(hx, 0f, g.offset) : new Vector3(-hx, 0f, g.offset);
                if (Vector3.Distance(d, p) < g.width / 2f + 1.5f) nearDoor = true;
            }
            if (nearDoor) continue;
            float s = DR(0.2f, 0.55f);
            var rock = Prim(parent, PrimitiveType.Cube, "Escombro", p + Vector3.up * s * 0.35f, new Vector3(s * DR(0.8f, 1.6f), s * DR(0.5f, 1f), s * DR(0.8f, 1.4f)), StoneMaterial(), false);
            rock.transform.localRotation = Quaternion.Euler(DR(-15f, 15f), DR(0f, 360f), DR(-15f, 15f));
            k++;
        }
    }

    void Crate(Transform parent, Vector3 pos, float s)
    {
        var c = Prim(parent, PrimitiveType.Cube, "Caja", pos + Vector3.up * s / 2f, Vector3.one * s, LitMaterial("Madera", new Color(0.42f, 0.27f, 0.15f)), true);
        c.transform.localRotation = Quaternion.Euler(0f, DR(-20f, 20f), 0f);
        Prim(c.transform, PrimitiveType.Cube, "Fleje", Vector3.zero, new Vector3(1.02f, 0.12f, 1.02f), MetalMaterial(), false);
    }

    void Brazier(Transform parent, Vector3 pos, Color color, float scale, bool shadows = false)
    {
        var b = NewChild(parent, "Brasero");
        b.localPosition = pos;
        Prim(b, PrimitiveType.Cylinder, "Pie", new Vector3(0f, 0.45f, 0f) * scale, new Vector3(0.18f, 0.45f, 0.18f) * scale, MetalMaterial(), false);
        Prim(b, PrimitiveType.Cylinder, "Cuenco", new Vector3(0f, 0.95f, 0f) * scale, new Vector3(0.75f, 0.1f, 0.75f) * scale, MetalMaterial(), false);
        var fire = Prim(b, PrimitiveType.Sphere, "Fuego", new Vector3(0f, 1.2f, 0f) * scale, new Vector3(0.5f, 0.65f, 0.5f) * scale, TorchMaterialFor(color), false);
        fire.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var lt = NewChild(b, "Luz").gameObject.AddComponent<Light>();
        lt.transform.localPosition = new Vector3(0f, 1.6f, 0f) * scale;
        lt.type = LightType.Point; lt.range = 8f; lt.intensity = 10f; lt.color = color;
        lt.shadows = shadows ? LightShadows.Soft : LightShadows.None;
        b.gameObject.AddComponent<LightFlicker>().amount = 0.25f;
        Motes(b, new Vector3(0f, 1.3f, 0f) * scale, new Vector3(0.4f, 0.1f, 0.4f), color, 10f, 1.2f, 1.4f);
    }

    void MerchantStall(Transform parent, Vector3 pos)
    {
        var s = NewChild(parent, "Mercader");
        s.localPosition = pos;
        var wood = LitMaterial("Madera", new Color(0.42f, 0.27f, 0.15f));
        Prim(s, PrimitiveType.Cube, "Mostrador", new Vector3(0f, 0.55f, 0f), new Vector3(4.2f, 1.1f, 0.9f), wood, true);
        Prim(s, PrimitiveType.Cube, "Tablero", new Vector3(0f, 1.13f, 0f), new Vector3(4.4f, 0.06f, 1.05f), LitMaterial("MaderaClara", new Color(0.6f, 0.43f, 0.26f)), false);
        foreach (float x in new[] { -2.05f, 2.05f })
            Prim(s, PrimitiveType.Cube, "Poste", new Vector3(x, 1.4f, 0.35f), new Vector3(0.14f, 2.8f, 0.14f), wood, false);
        // toldo a rayas
        for (int k = 0; k < 6; k++)
        {
            var stripe = Prim(s, PrimitiveType.Cube, "Toldo", new Vector3(-1.9f + k * 0.76f, 2.75f, -0.05f), new Vector3(0.76f, 0.05f, 1.6f),
                              LitMaterial(k % 2 == 0 ? "Tela_Toldo_A" : "Tela_Toldo_B", k % 2 == 0 ? new Color(0.6f, 0.15f, 0.3f) : new Color(0.85f, 0.75f, 0.55f)), false);
            stripe.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
        }
        // pociones brillando sobre el mostrador
        Color[] potions = { new Color(1f, 0.3f, 0.35f), new Color(0.35f, 0.7f, 1f), new Color(0.5f, 1f, 0.5f), new Color(1f, 0.8f, 0.3f) };
        for (int k = 0; k < potions.Length; k++)
        {
            var bottle = Prim(s, PrimitiveType.Sphere, "Pocion", new Vector3(-1.4f + k * 0.9f, 1.32f, -0.1f), new Vector3(0.26f, 0.32f, 0.26f), GlowMaterial("Glow_" + ColorUtility.ToHtmlStringRGB(potions[k]), potions[k]), false);
            bottle.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var lt = NewChild(s, "Farol").gameObject.AddComponent<Light>();
        lt.transform.localPosition = new Vector3(0f, 2.3f, -0.6f);
        lt.type = LightType.Point; lt.range = 6f; lt.intensity = 6f; lt.color = new Color(1f, 0.75f, 0.45f); lt.shadows = LightShadows.Soft;
        lt.gameObject.AddComponent<LightFlicker>().amount = 0.1f;
    }

    void Shrine(Transform parent, Vector3 pos)
    {
        var s = NewChild(parent, "Santuario");
        s.localPosition = pos;
        Prim(s, PrimitiveType.Cylinder, "Pedestal", new Vector3(0f, 0.3f, 0f), new Vector3(1.4f, 0.3f, 1.4f), StoneMaterial(), true);
        Prim(s, PrimitiveType.Cylinder, "Pedestal2", new Vector3(0f, 0.7f, 0f), new Vector3(0.8f, 0.12f, 0.8f), StoneMaterial(), false);
        RuneRing(s, Vector3.up * 0.02f, 1.5f, 1.75f, new Color(0.4f, 1f, 0.6f), "CirculoSanacion");
        var crystal = Prim(s, PrimitiveType.Cube, "Cristal", new Vector3(0f, 1.6f, 0f), new Vector3(0.45f, 0.75f, 0.45f), GlowMaterial("Glow_Sanacion", new Color(0.4f, 1f, 0.6f)), false);
        crystal.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
        crystal.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var lt = NewChild(s, "Luz").gameObject.AddComponent<Light>();
        lt.transform.localPosition = new Vector3(0f, 1.8f, 0f);
        lt.type = LightType.Point; lt.range = 7f; lt.intensity = 5f; lt.color = new Color(0.45f, 1f, 0.65f); lt.shadows = LightShadows.None;
        Motes(s, new Vector3(0f, 0.3f, 0f), new Vector3(1.6f, 0.1f, 1.6f), new Color(0.5f, 1f, 0.7f), 8f, 0.8f, 2.5f);
        var shrine = s.gameObject.AddComponent<HealingShrine>();
        shrine.crystal = crystal.transform;
        shrine.glow = lt;
    }

    void Throne(Transform parent, Vector3 pos)
    {
        var t = NewChild(parent, "Trono");
        t.localPosition = pos;
        Prim(t, PrimitiveType.Cube, "Grada1", new Vector3(0f, 0.15f, 0f), new Vector3(5f, 0.3f, 2.2f), StoneMaterial(), true);
        Prim(t, PrimitiveType.Cube, "Grada2", new Vector3(0f, 0.45f, 0.25f), new Vector3(3.6f, 0.3f, 1.6f), StoneMaterial(), true);
        Prim(t, PrimitiveType.Cube, "Asiento", new Vector3(0f, 0.95f, 0.35f), new Vector3(1.6f, 0.7f, 1.1f), MetalMaterial(), true);
        Prim(t, PrimitiveType.Cube, "Respaldo", new Vector3(0f, 2.1f, 0.8f), new Vector3(1.6f, 2.6f, 0.25f), MetalMaterial(), true);
        foreach (float x in new[] { -0.9f, 0.9f })
        {
            Prim(t, PrimitiveType.Cube, "Brazo", new Vector3(x, 1.35f, 0.35f), new Vector3(0.25f, 0.3f, 1.1f), MetalMaterial(), false);
            var spike = Prim(t, PrimitiveType.Cube, "Punta", new Vector3(x * 0.85f, 3.6f, 0.8f), new Vector3(0.18f, 0.7f, 0.18f), GlowMaterial("Glow_Jefe", new Color(1f, 0.25f, 0.2f)), false);
            spike.transform.localRotation = Quaternion.Euler(0f, 0f, x > 0 ? -12f : 12f);
        }
        var gem = Prim(t, PrimitiveType.Sphere, "Gema", new Vector3(0f, 3.05f, 0.65f), Vector3.one * 0.38f, GlowMaterial("Glow_Jefe", new Color(1f, 0.25f, 0.2f)), false);
        gem.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void RuneRing(Transform parent, Vector3 pos, float inner, float outer, Color color, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.AddComponent<MeshFilter>().sharedMesh = Annulus(inner, outer, 64);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = GlowMaterial("Glow_" + ColorUtility.ToHtmlStringRGB(color), color);
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static Mesh Annulus(float inner, float outer, int segs)
    {
        var v = new List<Vector3>(); var t = new List<int>();
        for (int i = 0; i <= segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f;
            var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            v.Add(d * inner); v.Add(d * outer);
            if (i < segs) { int b = i * 2; t.AddRange(new[] { b, b + 3, b + 1, b, b + 2, b + 3 }); }
        }
        var m = new Mesh { name = "Annulus" };
        m.SetVertices(v); m.SetTriangles(t, 0); m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>Partículas lentas que flotan (brasas, polvo mágico).</summary>
    void Motes(Transform parent, Vector3 pos, Vector3 box, Color color, float rate, float rise, float life)
    {
        var mat = FxMaterial();
        if (mat == null) return;
        var go = new GameObject("Brasas");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.loop = true; main.playOnAwake = true; main.prewarm = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f);
        main.startColor = color;
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = rate;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = box;
        var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
        vel.x = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f); vel.y = new ParticleSystem.MinMaxCurve(rise * 0.5f, rise); vel.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.3f; noise.frequency = 0.4f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.6f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
    }

    GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (!keepCollider) DestroyImmediate(go.GetComponent<Collider>());
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        return go;
    }

    // ------------------------------------------------------------------ materiales de decoración (se guardan en Assets/Materials/Nivel)

    readonly Dictionary<string, Material> decoMats = new Dictionary<string, Material>();

    Material SavedMaterial(string name, System.Func<Material> make)
    {
        if (decoMats.TryGetValue(name, out var m) && m != null) return m;
#if UNITY_EDITOR
        const string dir = "Assets/Materials/Nivel";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Materials", "Nivel");
        string path = dir + "/" + name + ".mat";
        m = AssetDatabase.LoadAssetAtPath<Material>(path);
#endif
        if (m == null)
        {
            m = make();
            m.name = name;
#if UNITY_EDITOR
            if (!Application.isPlaying) AssetDatabase.CreateAsset(m, path);
#endif
        }
        decoMats[name] = m;
        return m;
    }

    static Shader LitShader => Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

    Material LitMaterial(string name, Color color) => SavedMaterial(name, () =>
    {
        var m = new Material(LitShader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.1f);
        return m;
    });

    Material GlowMaterial(string name, Color color) => SavedMaterial(name, () =>
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
        Color hdr = color * 3f; hdr.a = 1f;   // HDR: el Bloom lo hace brillar
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", hdr);
        return m;
    });

    Material StoneMaterial() => SavedMaterial("Piedra", () =>
    {
        var m = new Material(LitShader);
#if UNITY_EDITOR
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Exterior/Stone.png");
        if (tex != null && m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
#endif
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.62f, 0.6f, 0.68f));
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.12f);
        return m;
    });

    Material MetalMaterial() => SavedMaterial("MetalOscuro", () =>
    {
        var m = new Material(LitShader);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.16f, 0.15f, 0.2f));
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.7f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.5f);
        return m;
    });

    /// <summary>Aditivo (shader Abby/Particle) para brasas y efectos de combate.</summary>
    Material FxMaterial() => SavedMaterial("FX_Brasas", () =>
    {
        var sh = Shader.Find("Abby/Particle");
        if (sh == null) return new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        var m = new Material(sh);
#if UNITY_EDITOR
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Exterior/SoftDot.png");
        if (tex != null) m.SetTexture("_BaseMap", tex);
#endif
        m.SetColor("_BaseColor", new Color(2.5f, 2.5f, 2.5f, 1f));
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return m;
    });

    Material BeamMaterial() => SavedMaterial("FX_HazSalida", () =>
    {
        var sh = Shader.Find("Abby/Particle");
        var m = new Material(sh != null ? sh : Shader.Find("Universal Render Pipeline/Unlit"));
        m.SetColor("_BaseColor", new Color(0.35f, 1.2f, 1.1f, 0.35f));
        if (sh != null)
        {
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        }
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return m;
    });

    void CreateOutside()
    {
        if (rooms.Length == 0) return;
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue), max = -min;
        foreach (var r in rooms)
        {
            min = Vector2.Min(min, r.center - r.size / 2f);
            max = Vector2.Max(max, r.center + r.size / 2f);
        }
        min -= Vector2.one * outsideMargin; max += Vector2.one * outsideMargin;
        var root = NewChild(transform, Prefix + "Outside");
        Vector2 c = (min + max) / 2f, size = max - min;
        float k = 1f / tileMeters;
        CreateFace(root, "Suelo", new Vector3(c.x, -0.05f, c.y), Vector3.up, size.x, size.y, outsideMaterial, k, k);
    }

    // ------------------------------------------------------------------ utilidades

    static Transform NewChild(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static void CreateFace(Transform parent, string faceName, Vector3 center, Vector3 inwardNormal, float width, float height, Material mat, float uScale, float vScale, bool castShadows = false)
    {
        var go = new GameObject(faceName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        if (inwardNormal == Vector3.up)        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        else if (inwardNormal == Vector3.down) go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        else go.transform.localRotation = Quaternion.LookRotation(-inwardNormal, Vector3.up); // la cara visible del quad mira a -Z local

        go.AddComponent<MeshFilter>().sharedMesh = MakeQuad(width, height, uScale, vScale);
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        // los muros son de una cara: TwoSided para que su sombra exista aunque se vean de espaldas
        mr.shadowCastingMode = castShadows ? UnityEngine.Rendering.ShadowCastingMode.TwoSided : UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    static void CreateCollider(Transform parent, string colliderName, Vector3 center, Vector3 size)
    {
        var go = new GameObject(colliderName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.AddComponent<BoxCollider>().size = size;
    }

    /// <summary>Quad de una cara (visible desde -Z). uScale/vScale = repeticiones de textura por metro.</summary>
    static Mesh MakeQuad(float w, float h, float uScale, float vScale)
    {
        float u = w * uScale, v = h * vScale;
        var m = new Mesh { name = "FloorQuad" };
        m.vertices = new[]
        {
            new Vector3(-w / 2, -h / 2, 0f), new Vector3(w / 2, -h / 2, 0f),
            new Vector3(-w / 2,  h / 2, 0f), new Vector3(w / 2,  h / 2, 0f),
        };
        m.uv = new[] { new Vector2(0, 0), new Vector2(u, 0), new Vector2(0, v), new Vector2(u, v) };
        m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
        m.triangles = new[] { 0, 2, 3, 0, 3, 1 };
        m.RecalculateBounds();
        return m;
    }

#if UNITY_EDITOR
    void LoadDefaultMaterials()
    {
        if (floorMaterial == null) floorMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Room_Floor.mat");
        if (wallMaterial == null) wallMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Room_Wall.mat");
        if (doorMaterial == null) doorMaterial = DoorMaterial();
        if (outsideMaterial == null) outsideMaterial = OutsideMaterial();
    }

    /// <summary>El mismo piso de baldosas pero en penumbra: fuera de las salas se ve "más torre", no un hueco.</summary>
    Material OutsideMaterial()
    {
        const string path = "Assets/Materials/Floor_Outside.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var m = floorMaterial != null ? new Material(floorMaterial) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.name = "Floor_Outside";
        var dim = new Color(0.5f, 0.5f, 0.58f);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", dim);
        if (m.HasProperty("_Color")) m.SetColor("_Color", dim);
        AssetDatabase.CreateAsset(m, path);
        AssetDatabase.SaveAssets();
        return m;
    }

    /// <summary>Barrera de energía (recorte por alfa + brillo) con la textura de Limits. Se guarda en Assets/Materials.</summary>
    static Material DoorMaterial()
    {
        const string path = "Assets/Materials/Door_Energy.mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;

        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Limits/Limit_Energy.png");
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        var m = new Material(sh) { name = "Door_Energy" };
        if (tex != null)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            m.EnableKeyword("_EMISSION");
            if (m.HasProperty("_EmissionMap")) m.SetTexture("_EmissionMap", tex);
        }
        if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", new Color(1.2f, 1.2f, 1.2f));
        if (m.HasProperty("_AlphaClip")) m.SetFloat("_AlphaClip", 1f);
        m.EnableKeyword("_ALPHATEST_ON");
        if (m.HasProperty("_Cutoff")) m.SetFloat("_Cutoff", 0.5f);
        if (m.HasProperty("_Cull")) m.SetFloat("_Cull", 2f);
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0f);
        m.renderQueue = 2450;
        AssetDatabase.CreateAsset(m, path);
        AssetDatabase.SaveAssets();
        return m;
    }
#endif

    void OnDrawGizmos()
    {
        if (rooms == null) return;
        Gizmos.matrix = transform.localToWorldMatrix;
        foreach (var r in rooms)
        {
            Gizmos.color = r.type switch
            {
                RoomType.Inicio => new Color(0.3f, 1f, 0.4f),
                RoomType.Descanso => new Color(1f, 0.8f, 0.3f),
                RoomType.Jefe => new Color(1f, 0.25f, 0.25f),
                RoomType.Antesala => new Color(0.8f, 0.5f, 1f),
                _ => new Color(0.4f, 0.8f, 1f),
            };
            Gizmos.DrawWireCube(new Vector3(r.center.x, 0.05f, r.center.y), new Vector3(r.size.x, 0.1f, r.size.y));
        }
    }
}
