using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Mapa de prueba "Diorama HD-2D": un pueblito al pie de la torre visto como maqueta (estilo Octopath / DQ3 HD-2D).
/// Terrazas de roca con musgo, cañón con río turquesa, cascadas, puente de madera con caballetes, muelle con barca,
/// casas de entramado con techo de tejas, mercado, pozo, árboles y pinos pixel-art, y al norte una escalinata que sube
/// a un patio en ruinas (pelea con oleadas). Al fondo el valle, montañas y la torre. Partes: .Terrain (suelo y agua),
/// .Plants (vegetación). Todo lo genera este script: clic derecho en el componente → "Generar diorama".
/// Se entra desde Pausa → Opciones → "Probar diorama HD-2D".
/// </summary>
public partial class DioramaBuilder : MonoBehaviour
{
    public enum Layout { Pueblo, Puerto, Acantilado, Cueva, Templo, Nevado, Cima, Lago, Cascadas, Montana, Intro, Expedicion }
    [Tooltip("Qué mapa genera: el pueblo del risco o el muelle de puerto.")]
    public Layout layout = Layout.Pueblo;
    public int seed = 7;
    [Tooltip("Mechones de pasto (malla combinada).")]
    public int grassTufts = 2600;

    const string Prefix = "Dio_";
    const float TerraceY = 2.6f;
    static readonly Rect TownWalk = new Rect(-33f, -29f, 66f, 82.4f);    // zona jugable del pueblo (x, z); al norte llega a la orilla del risco
    Rect Walk
    {
        get
        {
            switch (layout)
            {
                case Layout.Puerto: return PortWalk;
                case Layout.Acantilado: return CliffWalk;
                case Layout.Cueva: return CaveWalk;
                case Layout.Templo: return TempleWalk;
                case Layout.Nevado: return SnowWalk;
                case Layout.Cima: return SummitWalk;
                case Layout.Lago: return LakeWalk;
                case Layout.Cascadas: return FallsWalk;
                case Layout.Montana: return MountWalk;
                case Layout.Expedicion: return ExpWalk;
                default: return TownWalk;
            }
        }
    }
    static readonly Rect Terrace = new Rect(-15f, 28f, 30f, 18f);     // patio en alto
    const float StairZ0 = 21f, StairW = 7f;   // escalinata ancha y lejos del puente: entre los dos queda una plaza
    const float CliffZ = 58f, ValleyDepth = 40f;
    const float LookoutX = 19f, CliffFenceZ = 53.3f;

    System.Random rng;
    float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

    Material mGrass, mCobble, mPlaza, mStone, mBrick, mPlaster, mTimber, mRoof, mDoor, mPlank, mBark, mPine, mTuft,
             mMetal, mWindow, mWindowLit, mWater, mCliff, mSnow, mCloth, mClothB, mFruitR, mFruitG, mFruitY, mGlow, mDust, mSmoke,
             mRiver, mMountain, mRock, mDirt, mWaterfall, mBarkPix, mPinePix, mLeavesPix, mLeavesAutumn, mRay, mSparkle, mLily, mLilyFlower;
    Material[] mLeaves, mFlowers;
    readonly List<Rect> blocked = new List<Rect>();   // donde no van árboles ni pasto
    Transform root;

    // ------------------------------------------------------------------ generación

    /// <summary>Súbelo cada vez que cambie lo que genera Build(): los pisos viejos se regeneran solos al abrirlos.</summary>
    public const int BuildVersion = 27;
    [SerializeField, HideInInspector] int builtVersion;
    public bool IsCurrent => builtVersion == BuildVersion;

    [ContextMenu("Generar diorama")]
    public void Build()
    {
        builtVersion = BuildVersion;
        Clear();
        rng = new System.Random(seed);
        blocked.Clear();
        LoadMaterials();
        LoadWorldMaterials();
        root = Group(Prefix + "Mapa", transform);
        switch (layout)
        {
            case Layout.Puerto: BuildPort(); Pulir(); return;
            case Layout.Acantilado: BuildCliffs(); Pulir(); return;
            case Layout.Cueva: BuildCave(); Pulir(); return;
            case Layout.Templo: BuildTemple(); Pulir(); return;
            case Layout.Nevado: BuildSnow(); Pulir(); return;
            case Layout.Cima: BuildSummit(); Pulir(); return;
            case Layout.Lago: BuildLake(); Pulir(); return;
            case Layout.Cascadas: BuildFalls(); Pulir(); return;
            case Layout.Montana: BuildMountain(); Pulir(); return;
            case Layout.Intro: BuildIntro(); Pulir(); return;
            case Layout.Expedicion: BuildExpedition(); Pulir(); return;
        }

        BuildTerrain();
        BuildWater();
        BuildCanyonBridge(0f);
        BuildStreamBridge(-29f);
        BuildFences(0f);
        BuildLookout(LookoutX);
        BuildPaths();
        BuildRiverside();
        BuildTerrace();
        BuildRuins();
        BuildVillage();
        BuildLamps();
        BuildTrees();
        BuildGrass();
        BuildBackdrop();
        BuildOuterWorld();
        BuildBoundary();
        BuildAtmosphere();
        BuildShots();
        BuildTownLevel();
        Pulir();
#if UNITY_EDITOR
        if (!Application.isPlaying) EditorUtility.SetDirty(gameObject);
#endif
    }

    [ContextMenu("Borrar diorama")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i).gameObject;
            if (!c.name.StartsWith(Prefix)) continue;
            if (Application.isPlaying) Destroy(c); else DestroyImmediate(c);
        }
    }

    /// <summary>Dónde aparece Abby: al sur, mirando hacia el pueblo y la torre.</summary>
    public static Vector3 Spawn => new Vector3(0f, 0.1f, -21f);
    /// <summary>Dónde aparece Abby en el mapa que genera este componente.</summary>
    public Vector3 SpawnPoint
    {
        get
        {
            switch (layout)
            {
                case Layout.Puerto: return PortSpawn;
                case Layout.Acantilado: return CliffSpawn;
                case Layout.Cueva: return CaveSpawn;
                case Layout.Templo: return TempleSpawn;
                case Layout.Nevado: return SnowSpawn;
                case Layout.Cima: return SummitSpawn;
                case Layout.Lago: return LakeSpawn;
                case Layout.Cascadas: return FallsSpawn;
                case Layout.Montana: return MountSpawn;
                case Layout.Expedicion: return ExpSpawn;
                default: return Spawn;
            }
        }
    }

    // ------------------------------------------------------------------ caminos

    void BuildPaths()
    {
        var p = Group("Caminos", root);
        Strip(p, new Vector2(0f, -29f), new Vector2(0f, -FenceZ - 0.6f), 5f);
        Strip(p, new Vector2(0f, FenceZ + 0.6f), new Vector2(0f, StairZ0), 5f);
        Flat(p, "PlazaRuinas", new Vector3(0f, 0.013f, (FenceZ + StairZ0) / 2f + 0.3f), 16f, StairZ0 - FenceZ - 1.2f, mDirt, 0.5f);
        Strip(p, new Vector2(-31f, -13f), new Vector2(27f, -13f), 4.2f);
        Strip(p, new Vector2(-29f, -13f), new Vector2(-29f, -3.4f), 3.6f);
        Strip(p, new Vector2(-29f, 3.4f), new Vector2(-29f, 16f), 3.6f);
        Strip(p, new Vector2(-29f, 16f), new Vector2(0f, 16f), 3.6f);
        // al mirador: por el lado este del patio hasta la orilla del risco
        Strip(p, new Vector2(2.5f, 16f), new Vector2(LookoutX, 16f), 3.6f);
        Strip(p, new Vector2(LookoutX, 16f), new Vector2(LookoutX, CliffFenceZ), 3.6f);
        // plazuela del mercado
        Flat(p, "Plazuela", new Vector3(6f, 0.012f, -17.5f), 12f, 5f, mCobble, 0.5f);
        blocked.Add(new Rect(0f, -20.5f, 12f, 6f));
    }

    /// <summary>Camino de tierra entre dos puntos (eje X o Z).</summary>
    void Strip(Transform parent, Vector2 a, Vector2 b, float width)
    {
        Vector2 d = b - a;
        float len = d.magnitude;
        bool alongZ = Mathf.Abs(d.y) > Mathf.Abs(d.x);
        Vector2 c = (a + b) / 2f;
        Flat(parent, "Camino", new Vector3(c.x, 0.01f, c.y), alongZ ? width : len, alongZ ? len : width, mDirt, 0.5f);
        var rect = alongZ ? new Rect(c.x - width / 2f - 0.4f, Mathf.Min(a.y, b.y), width + 0.8f, len) : new Rect(Mathf.Min(a.x, b.x), c.y - width / 2f - 0.4f, len, width + 0.8f);
        blocked.Add(rect);
        // matas de pasto que se comen el borde del camino
        for (float t = 0.6f; t < len; t += R(0.8f, 1.5f))
            foreach (float sg in new[] { -1f, 1f })
            {
                if (rng.NextDouble() < 0.5) continue;
                Vector2 q = a + d.normalized * t + (alongZ ? new Vector2(sg * (width / 2f - R(0f, 0.3f)), 0f) : new Vector2(0f, sg * (width / 2f - R(0f, 0.3f))));
                var tuft = MeshObject(parent, "Mata", TuftMesh(), mTuft, false);
                tuft.transform.localPosition = new Vector3(q.x, 0f, q.y);
                tuft.transform.localScale = Vector3.one * R(0.8f, 1.3f);
            }
    }

    // ------------------------------------------------------------------ mirador y tomas de cámara

    /// <summary>
    /// Mirador del valle: plataforma de tablones que sale volando sobre el risco norte, con barandal, banca y catalejo.
    /// Desde ahí se ve el valle, el río, las montañas y la torre. Cerca a lo largo de la orilla del risco.
    /// </summary>
    void BuildLookout(float x)
    {
        var g = Group("Mirador", root);
        // cerca en la orilla del risco, con hueco para la plataforma
        Fence(g, new Vector2(Walk.xMin, CliffFenceZ), new Vector2(x - 2.4f, CliffFenceZ));
        Fence(g, new Vector2(x + 2.4f, CliffFenceZ), new Vector2(Walk.xMax, CliffFenceZ));

        var d = Group("Plataforma", g);
        d.localPosition = new Vector3(x, 0f, 0f);
        float w = 4.6f, z0 = CliffFenceZ - 0.3f, z1 = CliffFenceZ + 5.6f;
        for (float z = z0; z < z1; z += 0.42f)
        {
            var pl = Box(d, "Tablon", new Vector3(0f, 0.05f, z + 0.2f), new Vector3(w, 0.1f, 0.38f), mPlank, false);
            pl.transform.localRotation = Quaternion.Euler(R(-1f, 1f), R(-1.5f, 1.5f), 0f);
        }
        foreach (float sx in new[] { -1f, 1f })
        {
            Box(d, "Larguero", new Vector3(sx * (w / 2f - 0.15f), -0.2f, (z0 + z1) / 2f), new Vector3(0.3f, 0.32f, z1 - z0), mTimber, false);
            // patas hasta el risco con riostras
            for (float z = z0 + 1.2f; z < z1; z += 1.8f)
            {
                float ground = Height(x + sx * (w / 2f - 0.3f), z);
                if (ground > -0.4f) continue;
                Box(d, "Pata", new Vector3(sx * (w / 2f - 0.3f), ground / 2f - 0.1f, z), new Vector3(0.24f, -ground, 0.24f), mTimber, false);
                var br = Box(d, "Riostra", new Vector3(sx * (w / 2f - 0.3f), -0.9f, z - 0.7f), new Vector3(0.12f, 0.12f, 2f), mTimber, false);
                br.transform.localRotation = Quaternion.Euler(-40f, 0f, 0f);
            }
            for (float z = z0 + 0.3f; z <= z1; z += 1.4f)
                Box(d, "Poste", new Vector3(sx * w / 2f, 0.6f, z), new Vector3(0.14f, 1.2f, 0.14f), mTimber, false);
            Box(d, "Pasamanos", new Vector3(sx * w / 2f, 1.15f, (z0 + z1) / 2f + 0.15f), new Vector3(0.12f, 0.1f, z1 - z0 - 0.3f), mTimber, false);
            SolidBox(d, "Collider_Baranda", new Vector3(sx * (w / 2f + 0.1f), 1f, (z0 + z1) / 2f), new Vector3(0.2f, 2f, z1 - z0));
        }
        Box(d, "PasamanosFondo", new Vector3(0f, 1.15f, z1 - 0.1f), new Vector3(w, 0.1f, 0.12f), mTimber, false);
        foreach (float px in new[] { -w / 2f, -w / 6f, w / 6f, w / 2f })
            Box(d, "Poste", new Vector3(px, 0.6f, z1 - 0.1f), new Vector3(0.14f, 1.2f, 0.14f), mTimber, false);
        SolidBox(d, "Collider_Fondo", new Vector3(0f, 1f, z1 + 0.05f), new Vector3(w, 2f, 0.2f));
        SolidBox(d, "Collider_Tablero", new Vector3(0f, -0.1f, (z0 + z1) / 2f), new Vector3(w, 0.3f, z1 - z0 + 0.4f));
        // banca mirando al valle
        Box(d, "Asiento", new Vector3(-0.6f, 0.45f, z1 - 1.6f), new Vector3(1.6f, 0.08f, 0.45f), mPlank, false);
        Box(d, "Respaldo", new Vector3(-0.6f, 0.75f, z1 - 1.85f), new Vector3(1.6f, 0.4f, 0.06f), mPlank, false);
        foreach (float bx in new[] { -1.3f, 0.1f }) Box(d, "PataBanca", new Vector3(bx, 0.22f, z1 - 1.6f), new Vector3(0.08f, 0.44f, 0.4f), mTimber, false);
        // catalejo en su trípode
        var spy = Group("Catalejo", d);
        spy.localPosition = new Vector3(1.1f, 0f, z1 - 0.7f);
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f;
            var leg = Box(spy, "Pata", new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 0.18f, 0.5f, Mathf.Cos(a * Mathf.Deg2Rad) * 0.18f), new Vector3(0.05f, 1.05f, 0.05f), mTimber, false);
            leg.transform.localRotation = Quaternion.Euler(Mathf.Cos(a * Mathf.Deg2Rad) * 12f, 0f, -Mathf.Sin(a * Mathf.Deg2Rad) * 12f);
        }
        var tube = Prim(spy, PrimitiveType.Cylinder, "Tubo", new Vector3(0f, 1.1f, 0.15f), new Vector3(0.1f, 0.35f, 0.1f), mMetal);
        tube.transform.localRotation = Quaternion.Euler(75f, 0f, 0f);
        // farol del mirador
        var lamp = Group("Farol", d);
        lamp.localPosition = new Vector3(-w / 2f + 0.1f, 0f, z0 + 0.6f);
        Prim(lamp, PrimitiveType.Cylinder, "Poste", new Vector3(0f, 1.3f, 0f), new Vector3(0.12f, 1.3f, 0.12f), mTimber);
        Box(lamp, "Vidrio", new Vector3(0f, 2.75f, 0f), new Vector3(0.3f, 0.4f, 0.3f), mWindowLit, false);
        blocked.Add(new Rect(x - 3f, CliffFenceZ - 3f, 6f, 9f));
    }

    /// <summary>
    /// Tomas de cámara: lugares donde la vista se acomoda para lucir el paisaje (y un letrero la primera vez).
    /// </summary>
    void BuildShots()
    {
        var g = Group("Tomas", root);
        // apertura: plano general del pueblo, el cañón y las ruinas; luego baja hasta Abby
        Shot(g, "Apertura", Vector3.zero, Vector2.one, 30f, 26f, 95f, 30f, 5f, new Vector3(6f, 0f, 30f), 2.6f, 10, intro: 4.5f);
        // el puente: de lado, río abajo hasta la cascada
        Shot(g, "Puente", new Vector3(0f, 0f, 0f), new Vector2(4.4f, 2f * FenceZ + 1.2f), -68f, 16f, 24f, 32f, 6f, new Vector3(-5f, 0f, 0f), 1.6f, 2,
             "EL PUENTE DEL CAÑÓN", "Río abajo, el arroyo cae al cañón");
        // la cascada vista desde el arroyo, cañón abajo
        Shot(g, "Cascada", new Vector3(-29.5f, 0f, 0f), new Vector2(7f, 2f * FenceZ), -100f, 20f, 20f, 34f, 6f, new Vector3(3f, -1.5f, -2f), 1.6f, 2,
             "LA CAÍDA DEL ARROYO", "", still: true);
        // el muelle: desde el camino, la vista baja al río
        Shot(g, "Muelle", new Vector3(12.5f, 0f, -12.4f), new Vector2(9f, 3.4f), 15f, 40f, 18f, 32f, 0f, new Vector3(0f, -1.5f, 3.2f), 1.6f, 1,
             "EL MUELLE", "", still: true);
        // lo alto del patio: se voltea a ver todo el pueblo
        Shot(g, "Patio", new Vector3(0f, TerraceY, Terrace.yMin + 2.2f), new Vector2(24f, 3.6f), 180f, 28f, 28f, 34f, 5f, new Vector3(0f, 0f, -9f), 1.8f, 2,
             "LO ALTO DEL PATIO", "El pueblo del risco a tus pies", still: true);
        // el mirador: plano abierto y bajo hacia el valle, las montañas y la torre
        Shot(g, "Mirador", new Vector3(LookoutX, 0f, CliffFenceZ + 2.4f), new Vector2(3.6f, 6.4f), -22f, 12f, 10f, 44f, 6f, new Vector3(0f, 1.2f, 2.5f), 2.2f, 3,
             "MIRADOR DEL VALLE", "El abismo sigue más abajo", still: true);
    }

    void Shot(Transform parent, string name, Vector3 center, Vector2 size, float yaw, float pitch, float dist, float fov, float lookUp,
              Vector3 offset, float blend, int priority, string title = "", string subtitle = "", float intro = 0f, bool still = false)
    {
        var t = Group("Toma_" + name, parent);
        t.localPosition = center;
        var s = t.gameObject.AddComponent<CameraShot>();
        s.size = size; s.yMin = -3f; s.yMax = 6f;
        s.yaw = yaw; s.pitch = pitch; s.distance = dist; s.fov = fov; s.lookUp = lookUp; s.offset = offset;
        s.blendTime = blend; s.priority = priority; s.title = title; s.subtitle = subtitle; s.introSeconds = intro; s.whenStill = still;
    }

    // ------------------------------------------------------------------ orilla del río: muelle, barca, lirios, enredaderas

    void BuildRiverside()
    {
        var g = Group("Orilla", root);
        // muelle de tablones sobre pilotes en el bancal sur del río, con barca amarrada
        float x0 = 8f, x1 = 17f;
        float zr = Meander((x0 + x1) / 2f);
        float deckY = WaterLevel + 0.45f;
        for (float x = x0; x < x1; x += 0.42f)
        {
            var p = Box(g, "Tablon", new Vector3(x + 0.2f, deckY, zr - 2.4f), new Vector3(0.38f, 0.1f, 2.6f), mPlank, false);
            p.transform.localRotation = Quaternion.Euler(0f, R(-2f, 2f), R(-1.5f, 1.5f));
        }
        for (float x = x0; x <= x1 + 0.01f; x += 1.5f)
            foreach (float dz in new[] { -3.6f, -1.2f })
                Prim(g, PrimitiveType.Cylinder, "Pilote", new Vector3(x, deckY - 0.5f, zr + dz), new Vector3(0.22f, 0.8f, 0.22f), mTimber);
        Boat(g, new Vector3((x0 + x1) / 2f, WaterLevel + 0.05f, zr + 0.2f), 4f);
        // canastas y barriles sobre el muelle
        for (int i = 0; i < 4; i++)
            Prim(g, PrimitiveType.Cylinder, "Canasta", new Vector3(R(x0 + 0.5f, x1 - 0.5f), deckY + 0.25f, zr - R(2.6f, 3.4f)), new Vector3(0.55f, 0.22f, 0.55f), mPlank);

        // lirios y nenúfares en los remansos
        for (int i = 0; i < 70; i++)
        {
            float x = R(RiverStartX + 3f, 60f), z = Meander(x) + R(-3.2f, 3.2f) * (rng.NextDouble() < 0.5 ? 1f : -1f);
            if (Mathf.Abs(z - Meander(x)) < 1.6f) continue;   // al centro corre el agua
            var pad = Prim(g, PrimitiveType.Cylinder, "Nenufar", new Vector3(x, WaterLevel + 0.02f, z), new Vector3(R(0.5f, 0.9f), 0.01f, R(0.5f, 0.9f)), mLily);
            pad.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (rng.NextDouble() < 0.35)
                Prim(g, PrimitiveType.Sphere, "Flor", new Vector3(x, WaterLevel + 0.12f, z), new Vector3(0.22f, 0.14f, 0.22f), mLilyFlower);
        }

        // enredaderas colgando de las orillas del cañón
        for (int i = 0, tries = 0; i < 40 && tries < 2000; tries++)
        {
            float x = R(RiverStartX + 1f, 45f), sz = rng.NextDouble() < 0.5 ? 1f : -1f;
            for (float dz = 0f; dz < 9f; dz += 0.35f)
            {
                float z = Meander(x) + sz * (9.2f - dz);
                float hi = Height(x, z), lo = Height(x, z - sz * 0.6f);
                if (hi - lo < 0.8f) continue;
                Vine(g, new Vector3(x, hi, z - sz * 0.32f), Mathf.Min(hi - lo, 3.5f));
                i++;
                break;
            }
        }
    }

    void Boat(Transform parent, Vector3 pos, float len)
    {
        var b = Group("Barca", parent);
        b.localPosition = pos;
        b.localRotation = Quaternion.Euler(0f, R(-8f, 8f), 0f);
        Box(b, "Fondo", new Vector3(0f, 0.05f, 0f), new Vector3(len - 0.6f, 0.12f, 1.1f), mPlank, false);
        foreach (float sz in new[] { -1f, 1f })
        {
            var side = Box(b, "Costado", new Vector3(0f, 0.3f, sz * 0.62f), new Vector3(len - 0.3f, 0.45f, 0.1f), mDoor, false);
            side.transform.localRotation = Quaternion.Euler(sz * 14f, 0f, 0f);
        }
        foreach (float sx in new[] { -1f, 1f })
        {
            var bow = Box(b, "Proa", new Vector3(sx * (len / 2f - 0.25f), 0.3f, 0f), new Vector3(0.1f, 0.45f, 1.1f), mDoor, false);
            bow.transform.localRotation = Quaternion.Euler(0f, 0f, sx * -20f);
        }
        Box(b, "Banco", new Vector3(0f, 0.32f, 0f), new Vector3(0.35f, 0.08f, 1.15f), mPlank, false);
        var oar = Box(b, "Remo", new Vector3(0.6f, 0.4f, 0.3f), new Vector3(2.2f, 0.06f, 0.1f), mTimber, false);
        oar.transform.localRotation = Quaternion.Euler(0f, 20f, 8f);
    }

    /// <summary>Enredadera que cuelga de una orilla: bultos de hojas que se van haciendo chicos hacia abajo.</summary>
    void Vine(Transform parent, Vector3 top, float drop)
    {
        EnsurePlantMeshes();
        var v = Group("Enredadera", parent);
        v.localPosition = top;
        int n = Mathf.Max(2, Mathf.RoundToInt(drop / 0.45f));
        for (int i = 0; i < n; i++)
        {
            float s = Mathf.Lerp(0.9f, 0.35f, i / (float)n);
            var c = MeshObject(v, "Hojas", blobMeshes[rng.Next(blobMeshes.Length)], mLeavesPix, false);
            c.transform.localPosition = new Vector3(R(-0.25f, 0.25f), -i * 0.45f, R(-0.1f, 0.1f));
            c.transform.localScale = new Vector3(s * R(0.9f, 1.4f), s, s * 0.6f);
        }
    }

    /// <summary>Hiedra pegada a un muro o columna (bulto de hojas pixel).</summary>
    GameObject Ivy(Transform parent, Vector3 pos, Vector3 scale)
    {
        EnsurePlantMeshes();
        var go = MeshObject(parent, "Hiedra", blobMeshes[rng.Next(blobMeshes.Length)], mLeavesPix, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        return go;
    }

    // ------------------------------------------------------------------ terraza y ruinas

    void BuildTerrace()
    {
        var t = Group("Terraza", root);
        var r = Terrace;
        float cx = r.center.x, cz = r.center.y;
        // cuerpo macizo con muros de sillar
        Box(t, "Cuerpo", new Vector3(cx, TerraceY / 2f, cz), new Vector3(r.width, TerraceY, r.height), mBrick, false, 0.5f);
        SolidBox(t, "Collider_Terraza", new Vector3(cx, TerraceY / 2f, cz), new Vector3(r.width, TerraceY, r.height));
        Flat(t, "Losas", new Vector3(cx, TerraceY + 0.012f, cz), r.width - 0.6f, r.height - 0.6f, mPlaza, 0.35f);
        // cornisa
        Box(t, "CornisaSur", new Vector3(cx, TerraceY - 0.1f, r.yMin - 0.1f), new Vector3(r.width + 0.4f, 0.25f, 0.4f), mStone, false);
        Box(t, "CornisaE", new Vector3(r.xMax + 0.1f, TerraceY - 0.1f, cz), new Vector3(0.4f, 0.25f, r.height), mStone, false);
        Box(t, "CornisaO", new Vector3(r.xMin - 0.1f, TerraceY - 0.1f, cz), new Vector3(0.4f, 0.25f, r.height), mStone, false);
        // pretil almenado al frente y a los lados (hueco de la escalinata)
        float zf = r.yMin + 0.25f;
        TerraceParapet(t, new Vector3((r.xMin + -StairW / 2f) / 2f, 0f, zf), new Vector3(-StairW / 2f - r.xMin, 0f, 0.5f));
        TerraceParapet(t, new Vector3((r.xMax + StairW / 2f) / 2f, 0f, zf), new Vector3(r.xMax - StairW / 2f, 0f, 0.5f));
        TerraceParapet(t, new Vector3(r.xMin + 0.25f, 0f, cz), new Vector3(0.5f, 0f, r.height));
        TerraceParapet(t, new Vector3(r.xMax - 0.25f, 0f, cz), new Vector3(0.5f, 0f, r.height));
        TerraceParapet(t, new Vector3(cx, 0f, r.yMax - 0.25f), new Vector3(r.width, 0f, 0.5f));
        // contrafuertes en el muro del frente
        for (float x = r.xMin + 3f; x < r.xMax - 1f; x += 6f)
            if (Mathf.Abs(x) > StairW / 2f + 1f)
                Box(t, "Contrafuerte", new Vector3(x, TerraceY / 2f - 0.2f, r.yMin - 0.35f), new Vector3(0.9f, TerraceY - 0.4f, 0.7f), mStone, true);
        // hiedra colgando del muro
        for (int i = 0; i < 26; i++)
        {
            float x = R(r.xMin + 0.5f, r.xMax - 0.5f);
            if (Mathf.Abs(x) < StairW / 2f + 0.3f) continue;
            var v = Ivy(t, new Vector3(x, R(0.8f, TerraceY), r.yMin - 0.12f), new Vector3(R(0.6f, 1.3f), R(0.6f, 1.6f), 0.35f));
            v.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // escalinata
        var s = Group("Escalinata", t);
        int steps = 13;
        float run = r.yMin - StairZ0, depth = run / steps, rise = TerraceY / steps;
        for (int i = 0; i < steps; i++)
        {
            float top = (i + 1) * rise;
            Box(s, "Escalon", new Vector3(0f, top / 2f, StairZ0 + i * depth + depth / 2f), new Vector3(StairW, top, depth), i % 2 == 0 ? mStone : mCobble, false);
        }
        foreach (float sx in new[] { -1f, 1f })
        {
            var rail = Box(s, "Zanca", new Vector3(sx * (StairW / 2f + 0.3f), TerraceY / 2f + 0.2f, StairZ0 + run / 2f), new Vector3(0.6f, 0.5f, Mathf.Sqrt(run * run + TerraceY * TerraceY)), mStone, false);
            rail.transform.localRotation = Quaternion.Euler(-Mathf.Atan2(TerraceY, run) * Mathf.Rad2Deg, 0f, 0f);
            Box(s, "Pedestal", new Vector3(sx * (StairW / 2f + 0.3f), 0.6f, StairZ0 + 0.2f), new Vector3(0.8f, 1.2f, 0.8f), mStone, true);
            Prim(s, PrimitiveType.Sphere, "Bola", new Vector3(sx * (StairW / 2f + 0.3f), 1.45f, StairZ0 + 0.2f), Vector3.one * 0.55f, mStone);
            SolidBox(s, "Collider_Zanca", new Vector3(sx * (StairW / 2f + 0.3f), TerraceY / 2f + 0.8f, StairZ0 + run / 2f), new Vector3(0.6f, TerraceY + 1.6f, run));
        }
        // rampa invisible (se sube sin trabarse en cada escalón)
        float L = Mathf.Sqrt(run * run + TerraceY * TerraceY);
        var ramp = new GameObject("Collider_Rampa");
        ramp.transform.SetParent(s, false);
        ramp.transform.localPosition = new Vector3(0f, TerraceY / 2f - 0.08f, StairZ0 + run / 2f);
        ramp.transform.localRotation = Quaternion.Euler(-Mathf.Atan2(TerraceY, run) * Mathf.Rad2Deg, 0f, 0f);
        ramp.AddComponent<BoxCollider>().size = new Vector3(StairW, 0.15f, L + 0.3f);
        blocked.Add(new Rect(r.xMin - 1f, StairZ0 - 1f, r.width + 2f, r.yMax - StairZ0 + 2f));
    }

    void TerraceParapet(Transform parent, Vector3 center, Vector3 size)
    {
        Box(parent, "Pretil", new Vector3(center.x, TerraceY + 0.4f, center.z), new Vector3(size.x, 0.8f, size.z), mStone, false);
        SolidBox(parent, "Collider_Pretil", new Vector3(center.x, TerraceY + 1f, center.z), new Vector3(size.x, 2f, size.z));
        bool alongX = size.x > size.z;
        float len = alongX ? size.x : size.z;
        for (float d = -len / 2f + 0.4f; d < len / 2f - 0.2f; d += 1.4f)
            Box(parent, "Almena", center + (alongX ? new Vector3(d, TerraceY + 0.95f, 0f) : new Vector3(0f, TerraceY + 0.95f, d)), new Vector3(alongX ? 0.6f : 0.55f, 0.3f, alongX ? 0.55f : 0.6f), mStone, false);
    }

    void BuildRuins()
    {
        var g = Group("Ruinas", root);
        var r = Terrace;
        float y = TerraceY;
        // columnas: unas enteras con capitel, otras rotas
        float[] xs = { -11f, 11f };
        float[] zs = { 31.5f, 37f, 42.5f };
        int k = 0;
        foreach (float x in xs)
            foreach (float z in zs)
            {
                bool broken = (k++ % 3) == 1;
                Column(g, new Vector3(x, y, z), broken ? R(1.4f, 2.6f) : 5.2f, broken);
            }
        // arcada del fondo: cuatro pilares con arcos de medio punto
        float zb = r.yMax - 1.6f;
        float[] px = { -9f, -3f, 3f, 9f };
        foreach (float x in px) Column(g, new Vector3(x, y, zb), 4.4f, false, square: true);
        for (int i = 0; i < px.Length - 1; i++)
        {
            if (i == 2) continue;   // un arco caído
            Arch(g, new Vector3((px[i] + px[i + 1]) / 2f, y + 4.4f, zb), (px[i + 1] - px[i]) / 2f - 0.45f);
        }
        // restos del arco caído en el suelo
        for (int i = 0; i < 4; i++)
        {
            var b = Box(g, "Escombro", new Vector3(6f + R(-1.5f, 1.5f), y + 0.25f, zb - R(1.5f, 3f)), new Vector3(R(0.6f, 1.1f), 0.5f, R(0.5f, 0.9f)), mStone, true);
            b.transform.localRotation = Quaternion.Euler(R(-8f, 8f), R(0f, 90f), R(-8f, 8f));
        }
        // columna caída
        var fallen = Prim(g, PrimitiveType.Cylinder, "ColumnaCaida", new Vector3(-6f, y + 0.45f, 33f), new Vector3(0.9f, 1.8f, 0.9f), mStone, true);
        fallen.transform.localRotation = Quaternion.Euler(0f, 30f, 90f);
        // braseros en las esquinas del patio
        foreach (var c in new[] { new Vector2(-12.5f, 29.8f), new Vector2(12.5f, 29.8f), new Vector2(-12.5f, 43.5f), new Vector2(12.5f, 43.5f) })
            Brazier(g, new Vector3(c.x, y, c.y));
        // estatua al centro del fondo (pedestal + figura)
        Box(g, "Pedestal", new Vector3(0f, y + 0.6f, zb - 0.2f), new Vector3(1.6f, 1.2f, 1.6f), mStone, true);
        Prim(g, PrimitiveType.Capsule, "Estatua", new Vector3(0f, y + 2.3f, zb - 0.2f), new Vector3(0.8f, 1.1f, 0.8f), mPlaster);
        Prim(g, PrimitiveType.Sphere, "Cabeza", new Vector3(0f, y + 3.6f, zb - 0.2f), Vector3.one * 0.55f, mPlaster);
        // hiedra en ruinas
        for (int i = 0; i < 30; i++)
        {
            var v = Ivy(g, new Vector3(R(r.xMin + 1f, r.xMax - 1f), y + R(0.1f, 0.5f), R(r.yMax - 3f, r.yMax - 0.6f)), new Vector3(R(0.5f, 1.2f), R(0.3f, 0.7f), R(0.5f, 1.2f)));
            v.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // la pelea: oleadas de sombras y brutos al entrar al patio
        var arena = Group("PatioRuinas", g);
        // al fondo del patio: subir la escalinata y asomarse no inicia la pelea
        arena.localPosition = new Vector3(0f, y, r.center.y + 3.5f);
        var enc = arena.gameObject.AddComponent<RoomEncounter>();
        enc.size = new Vector2(r.width - 4f, 9f);
        enc.triggerInset = 1.5f;
        enc.doors = new LevelDoor[0];
        // el jefe de la sección: el Guardián de las Ruinas
        enc.isBoss = true;
        enc.bossName = Info.boss;
        enc.displayName = Info.boss;
        enc.healthMultiplier = Info.hp;
        enc.maxAlive = 4;
        enc.roster = Roster;
        enc.leash = 22f;   // baja la escalinata detrás de Abby hasta la plaza
        var t0 = Group("Aparicion", arena);
        t0.localPosition = new Vector3(0f, 0.1f, 2.5f);
        enc.spawnPoints = new[] { t0 };
        townBoss = enc;
    }

    void Column(Transform parent, Vector3 basePos, float height, bool broken, bool square = false)
    {
        var c = Group(square ? "Pilar" : "Columna", parent);
        c.localPosition = basePos;
        Box(c, "Basa", new Vector3(0f, 0.2f, 0f), new Vector3(1.3f, 0.4f, 1.3f), mStone, false);
        float shaft = height - (broken ? 0.4f : 0.9f);
        if (square) Box(c, "Fuste", new Vector3(0f, 0.4f + shaft / 2f, 0f), new Vector3(0.9f, shaft, 0.9f), mBrick, false);
        else Prim(c, PrimitiveType.Cylinder, "Fuste", new Vector3(0f, 0.4f + shaft / 2f, 0f), new Vector3(0.8f, shaft / 2f, 0.8f), mPlaster);
        if (!broken)
        {
            Box(c, "Capitel", new Vector3(0f, height - 0.3f, 0f), new Vector3(1.2f, 0.25f, 1.2f), mStone, false);
            Box(c, "Abaco", new Vector3(0f, height - 0.1f, 0f), new Vector3(1.35f, 0.2f, 1.35f), mStone, false);
        }
        else
        {
            var top = Prim(c, PrimitiveType.Cylinder, "Rotura", new Vector3(0.05f, 0.4f + shaft + 0.08f, 0f), new Vector3(0.78f, 0.1f, 0.78f), mPlaster);
            top.transform.localRotation = Quaternion.Euler(R(-12f, 12f), 0f, R(-12f, 12f));
            var piece = Prim(c, PrimitiveType.Cylinder, "Trozo", new Vector3(R(1.2f, 1.8f), 0.38f, R(-0.8f, 0.8f)), new Vector3(0.78f, 0.6f, 0.78f), mPlaster);
            piece.transform.localRotation = Quaternion.Euler(0f, R(0f, 180f), 90f);
        }
        // hiedra trepando
        for (int i = 0; i < 4; i++)
        {
            var v = Ivy(c, new Vector3(R(-0.45f, 0.45f), R(0.5f, height * 0.8f), R(-0.45f, 0.45f)), new Vector3(R(0.4f, 0.7f), R(0.5f, 1f), R(0.4f, 0.7f)));
            v.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var col = c.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, height / 2f, 0f); col.size = new Vector3(1.1f, height, 1.1f);
    }

    /// <summary>Arco de medio punto de dovelas, con su arranque a la altura 'top' (centro del arco en x).</summary>
    void Arch(Transform parent, Vector3 center, float radius)
    {
        var a = Group("Arco", parent);
        a.localPosition = center;
        int n = 9;
        for (int i = 0; i <= n; i++)
        {
            float t = Mathf.PI * i / n;
            var v = Box(a, "Dovela", new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(t) * radius, 0f), new Vector3(Mathf.PI * radius / n + 0.04f, 0.6f, 0.9f), mStone, false);
            v.transform.localRotation = Quaternion.Euler(0f, 0f, t * Mathf.Rad2Deg - 90f);
        }
        Box(a, "Clave", new Vector3(0f, radius + 0.35f, 0f), new Vector3(radius * 2f + 1.2f, 0.4f, 0.95f), mStone, false);
    }

    void Brazier(Transform parent, Vector3 pos)
    {
        var b = Group("Brasero", parent);
        b.localPosition = pos;
        Prim(b, PrimitiveType.Cylinder, "Pie", new Vector3(0f, 0.5f, 0f), new Vector3(0.25f, 0.5f, 0.25f), mMetal);
        Prim(b, PrimitiveType.Cylinder, "Cuenco", new Vector3(0f, 1.05f, 0f), new Vector3(0.9f, 0.1f, 0.9f), mMetal);
        var fire = Prim(b, PrimitiveType.Sphere, "Brasas", new Vector3(0f, 1.15f, 0f), new Vector3(0.6f, 0.25f, 0.6f), mGlow);
        fire.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var l = AddLight(b, new Vector3(0f, 1.7f, 0f), new Color(1f, 0.6f, 0.3f), 7f, 3f);
        l.gameObject.AddComponent<LightFlicker>().amount = 0.2f;
        Smoke(b, new Vector3(0f, 1.3f, 0f), new Color(1f, 0.55f, 0.25f, 0.5f), true);
        var col = b.gameObject.AddComponent<CapsuleCollider>(); col.radius = 0.45f; col.height = 1.2f; col.center = new Vector3(0f, 0.6f, 0f);
    }

    // ------------------------------------------------------------------ pueblo

    void BuildVillage()
    {
        var v = Group("Pueblo", root);
        House(v, new Vector3(-15f, 0f, -19.5f), 12f, 6.5f, 5f, 3.4f, true);
        House(v, new Vector3(17f, 0f, -21f), -8f, 7.5f, 5.5f, 3.6f, true);
        House(v, new Vector3(-26.5f, 0f, -21.5f), 90f, 5f, 4.2f, 3f, false);
        House(v, new Vector3(27f, 0f, -18f), -90f, 5.5f, 4.5f, 3.2f, false);
        House(v, new Vector3(-24f, 0f, 26f), 160f, 6f, 5f, 3.4f, true);
        House(v, new Vector3(27f, 0f, 28f), 200f, 6f, 4.6f, 3.2f, false);
        Stall(v, new Vector3(8.5f, 0f, -17.5f), -90f);
        MerchantPost(v, new Vector3(-22.5f, 0f, -16.2f), 0f);
        Well(v, new Vector3(-7f, 0f, -23f));
        // barriles y cajas junto a casas y el mercado
        foreach (var p in new[] { new Vector3(11.5f, 0f, -15.2f), new Vector3(-10.5f, 0f, -16.4f), new Vector3(22f, 0f, -16.2f), new Vector3(-9.5f, 0f, 20f), new Vector3(22f, 0f, 21f) })
            Clutter(v, p);
        // huerto con cerca junto a la primera casa
        Garden(v, new Vector3(-8f, 0f, -26.8f), 7f, 3f);
        // flores
        for (int i = 0; i < 140; i++)
        {
            Vector2 q = new Vector2(R(Walk.xMin + 2f, Walk.xMax - 2f), R(Walk.yMin + 2f, Walk.yMax - 2f));
            if (!Free(q, 0.3f) || Mathf.Abs(Height(q.x, q.y)) > 0.01f || Terrace.Contains(q)) continue;
            var f = Prim(v, PrimitiveType.Sphere, "Flor", new Vector3(q.x, 0.12f, q.y), Vector3.one * R(0.12f, 0.22f), mFlowers[rng.Next(mFlowers.Length)]);
            f.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    /// <summary>Casa de entramado: zócalo de piedra, muros encalados con vigas oscuras, techo de tejas, chimenea y ventanas.</summary>
    void House(Transform parent, Vector3 pos, float yaw, float w, float d, float wallH, bool chimney)
    {
        var h = Group("Casa", parent);
        h.localPosition = pos;
        h.localRotation = Quaternion.Euler(0f, yaw, 0f);
        float baseH = 0.6f;
        Box(h, "Zocalo", new Vector3(0f, baseH / 2f, 0f), new Vector3(w + 0.3f, baseH, d + 0.3f), mStone, false);
        Box(h, "Muros", new Vector3(0f, baseH + wallH / 2f, 0f), new Vector3(w, wallH, d), mPlaster, false);
        float top = baseH + wallH;
        // entramado: postes, carreras y diagonales en el frente y los lados
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                Box(h, "Poste", new Vector3(sx * w / 2f, baseH + wallH / 2f, sz * d / 2f), new Vector3(0.24f, wallH, 0.24f), mTimber, false);
        foreach (float sz in new[] { -1f, 1f })
        {
            Box(h, "Carrera", new Vector3(0f, top - 0.1f, sz * (d / 2f + 0.02f)), new Vector3(w, 0.2f, 0.12f), mTimber, false);
            Box(h, "Carrera", new Vector3(0f, baseH + wallH * 0.5f, sz * (d / 2f + 0.02f)), new Vector3(w, 0.16f, 0.12f), mTimber, false);
            for (float x = -w / 2f + w / 4f; x < w / 2f - 0.1f; x += w / 4f)
                Box(h, "Pie", new Vector3(x, baseH + wallH / 2f, sz * (d / 2f + 0.02f)), new Vector3(0.16f, wallH, 0.12f), mTimber, false);
        }
        foreach (float sx in new[] { -1f, 1f })
        {
            Box(h, "Carrera", new Vector3(sx * (w / 2f + 0.02f), top - 0.1f, 0f), new Vector3(0.12f, 0.2f, d), mTimber, false);
            var diag = Box(h, "Riostra", new Vector3(sx * (w / 2f + 0.02f), baseH + wallH * 0.25f, 0f), new Vector3(0.12f, 0.14f, d * 0.75f), mTimber, false);
            diag.transform.localRotation = Quaternion.Euler(sx * 18f, 0f, 0f);
        }
        // techo de tejas a dos aguas (cumbrera a lo largo de X) y hastiales encalados
        float roofH = d * 0.55f;
        var roof = MeshObject(h, "Tejado", Roof(w + 0.9f, d + 1f, roofH), mRoof, true);
        roof.transform.localPosition = new Vector3(0f, top, 0f);
        MeshObject(h, "Hastial", Gable(w, roofH * (d / (d + 1f)), d / 2f + 0.01f), mPlaster, true).transform.localPosition = new Vector3(0f, top, 0f);
        Box(h, "Cumbrera", new Vector3(0f, top + roofH + 0.05f, 0f), new Vector3(w + 1f, 0.18f, 0.22f), mTimber, false);
        // puerta, ventanas con marco y jardinera (frente = +Z)
        Box(h, "Puerta", new Vector3(w * 0.15f, baseH + 0.9f, d / 2f + 0.05f), new Vector3(0.95f, 1.8f, 0.1f), mDoor, false);
        Box(h, "Dintel", new Vector3(w * 0.15f, baseH + 1.85f, d / 2f + 0.08f), new Vector3(1.2f, 0.16f, 0.12f), mTimber, false);
        Box(h, "Escalon", new Vector3(w * 0.15f, 0.1f, d / 2f + 0.5f), new Vector3(1.3f, 0.2f, 0.6f), mStone, false);
        foreach (float x in new[] { -w * 0.25f, w * 0.38f })
        {
            if (Mathf.Abs(x - w * 0.15f) < 0.9f) continue;
            Window(h, new Vector3(x, baseH + wallH * 0.62f, d / 2f + 0.04f), 0f);
        }
        Window(h, new Vector3(w / 2f + 0.04f, baseH + wallH * 0.62f, 0f), 90f);
        Window(h, new Vector3(-w / 2f - 0.04f, baseH + wallH * 0.62f, 0f), -90f);
        if (chimney)
        {
            Box(h, "Chimenea", new Vector3(w * 0.3f, top + roofH * 0.7f, -d * 0.15f), new Vector3(0.6f, roofH * 1.2f + 0.4f, 0.6f), mBrick, false);
            Smoke(h, new Vector3(w * 0.3f, top + roofH * 1.35f + 0.3f, -d * 0.15f), new Color(0.85f, 0.85f, 0.88f, 0.3f), false);
        }
        var col = h.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, (top + roofH) / 2f, 0f); col.size = new Vector3(w + 0.3f, top + roofH, d + 0.3f);
        float rad = Mathf.Max(w, d) / 2f + 1.2f;
        blocked.Add(new Rect(pos.x - rad, pos.z - rad, rad * 2f, rad * 2f));
    }

    void Window(Transform parent, Vector3 pos, float yaw)
    {
        var t = Group("Ventana", parent);
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, yaw, 0f);
        Box(t, "Vidrio", Vector3.zero, new Vector3(0.7f, 0.8f, 0.04f), rng.NextDouble() < 0.3 ? mWindowLit : mWindow, false);
        Box(t, "Marco", new Vector3(0f, 0.45f, 0.02f), new Vector3(0.9f, 0.1f, 0.08f), mTimber, false);
        Box(t, "Marco", new Vector3(0f, -0.45f, 0.02f), new Vector3(0.9f, 0.1f, 0.08f), mTimber, false);
        Box(t, "Cruz", new Vector3(0f, 0f, 0.03f), new Vector3(0.06f, 0.8f, 0.05f), mTimber, false);
        Box(t, "Jardinera", new Vector3(0f, -0.58f, 0.15f), new Vector3(0.85f, 0.18f, 0.25f), mDoor, false);
        for (int i = 0; i < 4; i++)
        {
            var f = Prim(t, PrimitiveType.Sphere, "Flor", new Vector3(-0.3f + i * 0.2f, -0.45f, 0.15f), Vector3.one * 0.17f, mFlowers[rng.Next(mFlowers.Length)]);
            f.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    /// <summary>Puesto de mercado: mostrador, toldo a rayas, cajas de fruta y barriles.</summary>
    void Stall(Transform parent, Vector3 pos, float yaw)
    {
        var s = Group("Puesto", parent);
        s.localPosition = pos;
        s.localRotation = Quaternion.Euler(0f, yaw, 0f);
        float w = 3.6f, d = 1.8f;
        foreach (float sx in new[] { -1f, 1f })
            foreach (float sz in new[] { -1f, 1f })
                Box(s, "Poste", new Vector3(sx * w / 2f, 1.3f + (sz > 0 ? 0f : 0.3f), sz * d / 2f), new Vector3(0.14f, sz > 0 ? 2.6f : 3.2f, 0.14f), mTimber, false);
        Box(s, "Mostrador", new Vector3(0f, 0.5f, d / 2f - 0.3f), new Vector3(w, 1f, 0.6f), mPlank, false);
        Box(s, "Tabla", new Vector3(0f, 1.03f, d / 2f - 0.3f), new Vector3(w + 0.2f, 0.07f, 0.8f), mDoor, false);
        // toldo a rayas inclinado
        int stripes = 7;
        for (int i = 0; i < stripes; i++)
        {
            var st = Box(s, "Toldo", new Vector3(-w / 2f + (i + 0.5f) * w / stripes, 2.9f, 0f), new Vector3(w / stripes + 0.01f, 0.06f, d + 0.8f), i % 2 == 0 ? mCloth : mClothB, false);
            st.transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
        }
        // fruta en cajas sobre el mostrador
        Material[] fruit = { mFruitR, mFruitG, mFruitY };
        for (int c = 0; c < 3; c++)
        {
            float x = -w / 2f + 0.65f + c * 1.15f;
            Box(s, "Caja", new Vector3(x, 1.17f, d / 2f - 0.3f), new Vector3(0.9f, 0.22f, 0.6f), mPlank, false);
            for (int i = 0; i < 6; i++)
                Prim(s, PrimitiveType.Sphere, "Fruta", new Vector3(x + R(-0.3f, 0.3f), 1.33f + R(0f, 0.05f), d / 2f - 0.3f + R(-0.18f, 0.18f)), Vector3.one * 0.2f, fruit[c]);
        }
        var col = s.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.6f, d / 2f - 0.3f); col.size = new Vector3(w, 1.2f, 0.8f);
        Clutter(s, new Vector3(w / 2f + 0.8f, 0f, 0f), local: true);
    }

    void Well(Transform parent, Vector3 pos)
    {
        var w = Group("Pozo", parent);
        w.localPosition = pos;
        MeshObject(w, "Brocal", Ring(1.2f, 0.85f, 0.9f, 20), mStone, true);
        var water = Prim(w, PrimitiveType.Cylinder, "Agua", new Vector3(0f, 0.5f, 0f), new Vector3(1.7f, 0.01f, 1.7f), mWater);
        water.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        foreach (float sx in new[] { -1f, 1f })
            Box(w, "Poste", new Vector3(sx * 1f, 1.6f, 0f), new Vector3(0.18f, 2.2f, 0.18f), mTimber, false);
        var roof = MeshObject(w, "Tejadillo", Roof(2.6f, 1.9f, 0.8f), mRoof, true);
        roof.transform.localPosition = new Vector3(0f, 2.65f, 0f);
        roof.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        var axle = Prim(w, PrimitiveType.Cylinder, "Torno", new Vector3(0f, 2f, 0f), new Vector3(0.14f, 0.95f, 0.14f), mTimber);
        axle.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Prim(w, PrimitiveType.Cylinder, "Cubeta", new Vector3(0f, 1.3f, 0f), new Vector3(0.35f, 0.18f, 0.35f), mDoor);
        var col = w.gameObject.AddComponent<CapsuleCollider>(); col.radius = 1.25f; col.height = 2f; col.center = Vector3.up;
        blocked.Add(new Rect(pos.x - 2f, pos.z - 2f, 4f, 4f));
    }

    void Garden(Transform parent, Vector3 pos, float w, float d)
    {
        var g = Group("Huerto", parent);
        g.localPosition = pos;
        for (int row = 0; row < 3; row++)
            for (float x = -w / 2f + 0.6f; x < w / 2f - 0.4f; x += 0.7f)
            {
                var c = Prim(g, PrimitiveType.Sphere, "Col", new Vector3(x, 0.15f, -d / 2f + 0.8f + row * 1.2f), new Vector3(0.45f, 0.3f, 0.45f), mLeaves[row % mLeaves.Length]);
                c.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        // cerca de estacas
        for (float x = -w / 2f; x <= w / 2f + 0.01f; x += 0.9f)
            foreach (float z in new[] { -d / 2f, d / 2f })
                Box(g, "Estaca", new Vector3(x, 0.45f, z), new Vector3(0.1f, 0.9f, 0.1f), mTimber, false);
        for (float z = -d / 2f; z <= d / 2f + 0.01f; z += 0.9f)
            foreach (float x in new[] { -w / 2f, w / 2f })
                Box(g, "Estaca", new Vector3(x, 0.45f, z), new Vector3(0.1f, 0.9f, 0.1f), mTimber, false);
        foreach (float z in new[] { -d / 2f, d / 2f })
            foreach (float y in new[] { 0.35f, 0.7f })
                Box(g, "Travesano", new Vector3(0f, y, z), new Vector3(w, 0.07f, 0.06f), mTimber, false);
        foreach (float x in new[] { -w / 2f, w / 2f })
            foreach (float y in new[] { 0.35f, 0.7f })
                Box(g, "Travesano", new Vector3(x, y, 0f), new Vector3(0.06f, 0.07f, d), mTimber, false);
        var col = g.gameObject.AddComponent<BoxCollider>(); col.center = new Vector3(0f, 0.5f, 0f); col.size = new Vector3(w, 1f, d);
        blocked.Add(new Rect(pos.x - w / 2f - 0.5f, pos.z - d / 2f - 0.5f, w + 1f, d + 1f));
    }

    void Clutter(Transform parent, Vector3 pos, bool local = false)
    {
        var c = Group("Cosas", parent);
        c.localPosition = pos;
        int barrels = rng.Next(1, 3);
        for (int i = 0; i < barrels; i++)
        {
            var b = Prim(c, PrimitiveType.Cylinder, "Barril", new Vector3(i * 0.75f, 0.5f, R(-0.2f, 0.2f)), new Vector3(0.65f, 0.5f, 0.65f), mDoor, true);
            Prim(b.transform, PrimitiveType.Cylinder, "Aro", new Vector3(0f, 0.5f, 0f), new Vector3(1.04f, 0.04f, 1.04f), mMetal);
            Prim(b.transform, PrimitiveType.Cylinder, "Aro", new Vector3(0f, -0.5f, 0f), new Vector3(1.04f, 0.04f, 1.04f), mMetal);
        }
        int crates = rng.Next(1, 4);
        for (int i = 0; i < crates; i++)
        {
            var b = Box(c, "Caja", new Vector3(R(-0.3f, 0.3f), 0.32f + (i == 2 ? 0.64f : 0f), -0.9f + (i == 2 ? 0f : i * 0.7f)), Vector3.one * 0.64f, mPlank, true);
            b.transform.localRotation = Quaternion.Euler(0f, R(-15f, 15f), 0f);
        }
        if (!local) blocked.Add(new Rect(pos.x - 1.5f, pos.z - 1.8f, 3f, 3f));
    }

    // ------------------------------------------------------------------ faroles, árboles y pasto

    void BuildLamps()
    {
        var g = Group("Faroles", root);
        foreach (var p in new[] { new Vector2(2.5f, -26f), new Vector2(-2.5f, -18f), new Vector2(-3.4f, -11.6f), new Vector2(3.6f, 12.4f), new Vector2(-17.5f, -15.8f), new Vector2(17f, -15.8f), new Vector2(-31.6f, 13f), new Vector2(-14f, 18.4f), new Vector2(-5.6f, 20f), new Vector2(5.6f, 20f) })
        {
            var t = Group("Farol", g);
            t.localPosition = new Vector3(p.x, 0f, p.y);
            Prim(t, PrimitiveType.Cylinder, "Poste", new Vector3(0f, 1.5f, 0f), new Vector3(0.14f, 1.5f, 0.14f), mMetal, true);
            Box(t, "Base", new Vector3(0f, 0.15f, 0f), new Vector3(0.4f, 0.3f, 0.4f), mStone, false);
            Box(t, "Tapa", new Vector3(0f, 3.4f, 0f), new Vector3(0.5f, 0.1f, 0.5f), mMetal, false);
            Box(t, "Vidrio", new Vector3(0f, 3.1f, 0f), new Vector3(0.3f, 0.45f, 0.3f), mWindowLit, false);
            blocked.Add(new Rect(p.x - 0.6f, p.y - 0.6f, 1.2f, 1.2f));
        }
    }

    [System.NonSerialized] Mesh coneMesh;

    bool Free(Vector2 q, float margin)
    {
        foreach (var r in blocked)
            if (q.x > r.xMin - margin && q.x < r.xMax + margin && q.y > r.yMin - margin && q.y < r.yMax + margin) return false;
        return true;
    }

    // ------------------------------------------------------------------ fondo, límites y ambiente

    void BuildBackdrop()
    {
        var g = Group("Fondo", root);
        if (coneMesh == null) coneMesh = Cone(1f, 1f, 9);
        Flat(g, "Valle", new Vector3(0f, -ValleyDepth - 0.3f, 1240f), 3000f, 2400f, mGrass, 0.05f);   // hasta donde llega la bruma
        var river = Flat(g, "RioValle", new Vector3(0f, -ValleyDepth + 0.3f, 125f), 700f, 14f, mWater, 0.05f);
        river.transform.localRotation = Quaternion.Euler(0f, 8f, 0f);
        // cordillera al norte: bultos de roca facetados (la niebla los azula)
        EnsurePlantMeshes();
        for (int i = 0; i < 18; i++)
        {
            float x = -260f + i * 30f + R(-10f, 10f), z = R(420f, 520f), h = R(110f, 190f), r = R(55f, 85f);   // lejos: se ven azules y dejan cielo
            var m = MeshObject(g, "Montana", blobMeshes[rng.Next(blobMeshes.Length)], mMountain, false);
            m.transform.localPosition = new Vector3(x, -ValleyDepth - h * 0.1f, z);
            m.transform.localScale = new Vector3(r * 2.4f, h * 0.9f, r * 1.6f);   // cordillera baja y ancha: deja ver el cielo
            m.transform.localRotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
        }
        // colinas y bosque en el valle
        for (int i = 0; i < 12; i++)
        {
            var hill = Prim(g, PrimitiveType.Sphere, "Colina", new Vector3(-140f + i * 26f + R(-8f, 8f), -ValleyDepth - 4f, R(150f, 200f)), new Vector3(R(40f, 70f), R(20f, 35f), R(30f, 45f)), mGrass);
            hill.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        for (int i = 0; i < 140; i++)
        {
            var q = new Vector2(R(-160f, 160f), R(85f, 200f));
            if (Mathf.Abs(q.y - (125f + q.x * Mathf.Tan(8f * Mathf.Deg2Rad))) < 10f) continue;   // el río
            float gy = TerrainArea.Contains(q) ? Height(q.x, q.y) : -ValleyDepth;
            if (gy > -ValleyDepth + 0.5f) continue;
            var t = Group("PinoValle", g);
            t.localPosition = new Vector3(q.x, gy, q.y);
            float s = R(2.5f, 4.5f);
            for (int k = 0; k < 3; k++)
            {
                var c = MeshObject(t, "Copa", coneMesh, mPinePix, false);
                c.transform.localPosition = new Vector3(0f, s * (0.8f + k * 0.9f), 0f);
                c.transform.localScale = new Vector3(s * (1.5f - k * 0.35f), s * 1.6f, s * (1.5f - k * 0.35f));
            }
        }
        // la torre, a lo lejos (con su faro arriba)
        var tower = Group("Torre", g);
        tower.localPosition = new Vector3(-80f, -ValleyDepth, 340f);   // lejos: silueta detrás de las montañas
        Prim(tower, PrimitiveType.Cylinder, "Cuerpo", new Vector3(0f, 70f, 0f), new Vector3(16f, 70f, 16f), mBrick);
        for (int i = 1; i <= 6; i++)
            Prim(tower, PrimitiveType.Cylinder, "Anillo", new Vector3(0f, i * 22f, 0f), new Vector3(17.5f, 0.8f, 17.5f), mStone);
        var cone = MeshObject(tower, "Aguja", coneMesh, mRoof, false);
        cone.transform.localPosition = new Vector3(0f, 140f, 0f);
        cone.transform.localScale = new Vector3(9.5f, 28f, 9.5f);
        Prim(tower, PrimitiveType.Sphere, "Faro", new Vector3(0f, 171f, 0f), Vector3.one * 4f, mGlow);
    }

    void BuildBoundary()
    {
        var g = Group("Limites", root);
        var r = Walk;
        SolidBox(g, "Limite_O", new Vector3(r.xMin - 0.5f, 3f, r.center.y), new Vector3(1f, 8f, r.height));
        SolidBox(g, "Limite_E", new Vector3(r.xMax + 0.5f, 3f, r.center.y), new Vector3(1f, 8f, r.height));
        SolidBox(g, "Limite_S", new Vector3(r.center.x, 3f, r.yMin - 0.5f), new Vector3(r.width, 8f, 1f));
        float gx0 = LookoutX - 2.3f, gx1 = LookoutX + 2.3f;
        SolidBox(g, "Limite_N", new Vector3((r.xMin + gx0) / 2f, 3f, r.yMax + 0.5f), new Vector3(gx0 - r.xMin, 8f, 1f));
        SolidBox(g, "Limite_N", new Vector3((gx1 + r.xMax) / 2f, 3f, r.yMax + 0.5f), new Vector3(r.xMax - gx1, 8f, 1f));
    }

    public enum Mood { Dia, Atardecer, Noche, Gruta, Crepusculo, Amanecer }

    /// <summary>Hora y ambiente de cada piso.</summary>
    public Mood CurrentMood
    {
        get
        {
            switch (layout)
            {
                case Layout.Puerto: case Layout.Acantilado: return Mood.Atardecer;
                case Layout.Templo: case Layout.Nevado: return Mood.Noche;
                case Layout.Cueva: return Mood.Gruta;
                case Layout.Cima: return Mood.Crepusculo;
                case Layout.Lago: return Mood.Amanecer;
                case Layout.Intro: return Mood.Atardecer;
                case Layout.Expedicion: return Mood.Gruta;
                default: return Mood.Dia;
            }
        }
    }

    /// <summary>De noche (o en la gruta) los faroles alumbran de verdad.</summary>
    bool LampsLit => CurrentMood != Mood.Dia;

    void BuildAtmosphere()
    {
        var g = Group("Ambiente", root);
        var mood = CurrentMood;
        var sun = new GameObject("Sol").AddComponent<Light>();
        sun.transform.SetParent(g, false);
        sun.type = LightType.Directional;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.8f;
        RenderSettings.sun = sun;
        var sky = g.gameObject.AddComponent<ExteriorSky>();
        sky.driveFog = false;
        Color fog; float fogStart, fogEnd;
        bool stars = false, fireflies = false;
        switch (mood)
        {
            case Mood.Atardecer:
                // sol bajo enfrente de la cámara: el mar y el agua brillan en naranja
                sun.transform.localRotation = Quaternion.Euler(22f, 118f, 0f);   // de lado: se ven las caras iluminadas y el cielo naranja a la izquierda
                sun.color = new Color(1f, 0.58f, 0.32f); sun.intensity = 1.7f;
                sky.zenith = new Color(0.22f, 0.27f, 0.56f); sky.horizon = new Color(1f, 0.52f, 0.26f); sky.ground = new Color(0.45f, 0.32f, 0.38f);
                sky.sunColor = new Color(1f, 0.6f, 0.3f) * 2.4f; sky.sunSize = 0.012f;
                sky.cloudColor = new Color(1f, 0.62f, 0.48f); sky.cloudCover = 0.5f;
                fog = new Color(0.86f, 0.56f, 0.46f); fogStart = 110f; fogEnd = 760f;
                break;
            case Mood.Noche:
                // la luz principal es la luna (su disco se dibuja en el cielo), azul y suave
                sun.transform.localRotation = Quaternion.Euler(38f, 205f, 0f);
                sun.color = new Color(0.6f, 0.7f, 1f); sun.intensity = 0.95f;
                sky.zenith = new Color(0.015f, 0.025f, 0.08f); sky.horizon = new Color(0.1f, 0.13f, 0.3f); sky.ground = new Color(0.04f, 0.05f, 0.1f);
                sky.sunColor = new Color(0.75f, 0.82f, 1f) * 1.2f; sky.sunSize = 0.006f;
                sky.cloudColor = new Color(0.22f, 0.25f, 0.4f); sky.cloudCover = 0.3f;
                fog = new Color(0.06f, 0.09f, 0.18f); fogStart = 90f; fogEnd = 1100f;
                stars = true; fireflies = layout != Layout.Nevado;
                break;
            case Mood.Gruta:
                // casi sin luz de afuera: alumbran los cristales; la oscuridad esconde los bordes
                sun.transform.localRotation = Quaternion.Euler(60f, 30f, 0f);
                sun.color = new Color(0.4f, 0.55f, 0.95f); sun.intensity = 0.45f;
                sky.zenith = new Color(0.005f, 0.01f, 0.02f); sky.horizon = new Color(0.02f, 0.04f, 0.07f); sky.ground = new Color(0.01f, 0.02f, 0.03f);
                sky.sunColor = Color.black; sky.cloudCover = 0f;
                fog = new Color(0.03f, 0.07f, 0.12f); fogStart = 40f; fogEnd = 220f;
                fireflies = true;
                break;
            case Mood.Crepusculo:
                sun.transform.localRotation = Quaternion.Euler(20f, 118f, 0f);
                sun.color = new Color(1f, 0.62f, 0.55f); sun.intensity = 1.5f;
                sky.zenith = new Color(0.06f, 0.06f, 0.22f); sky.horizon = new Color(0.98f, 0.45f, 0.42f); sky.ground = new Color(0.5f, 0.42f, 0.6f);
                sky.sunColor = new Color(1f, 0.55f, 0.45f) * 2.2f; sky.sunSize = 0.01f;
                sky.cloudColor = new Color(1f, 0.7f, 0.75f); sky.cloudCover = 0.35f;
                fog = new Color(0.62f, 0.45f, 0.6f); fogStart = 120f; fogEnd = 1000f;
                stars = true;
                break;
            case Mood.Amanecer:
                // amanecer: cielo lila que se vuelve rosa y oro en el horizonte; el lago lo refleja
                sun.transform.localRotation = Quaternion.Euler(16f, -62f, 0f);
                sun.color = new Color(1f, 0.82f, 0.68f); sun.intensity = 2f;
                sky.zenith = new Color(0.36f, 0.48f, 0.82f); sky.horizon = new Color(1f, 0.74f, 0.62f); sky.ground = new Color(0.6f, 0.58f, 0.66f);
                sky.sunColor = new Color(1f, 0.8f, 0.55f) * 2.2f; sky.sunSize = 0.009f;
                sky.cloudColor = new Color(1f, 0.8f, 0.82f); sky.cloudCover = 0.45f;
                fog = new Color(0.86f, 0.78f, 0.84f); fogStart = 240f; fogEnd = 1100f;
                break;
            default:
                // tarde dorada de cuento: azul claro arriba, durazno en el horizonte, nubes cálidas
                sun.transform.localRotation = Quaternion.Euler(38f, 30f, 0f);
                sun.color = new Color(1f, 0.88f, 0.7f); sun.intensity = 1.85f;
                sky.zenith = new Color(0.42f, 0.62f, 0.9f); sky.horizon = new Color(1f, 0.84f, 0.64f); sky.ground = new Color(0.6f, 0.68f, 0.78f);
                sky.sunColor = new Color(1f, 0.85f, 0.6f) * 1.7f; sky.sunSize = 0.004f;
                sky.cloudColor = new Color(1f, 0.93f, 0.86f); sky.cloudCover = 0.55f;
                // bruma azul clara (perspectiva aérea): lo lejano se funde con el cielo sin apagar los colores
                fog = new Color(0.74f, 0.83f, 0.93f); fogStart = 120f; fogEnd = 820f;
                break;
        }
        sky.ground = fog;   // bajo el horizonte el cielo es del color de la bruma: no se nota dónde acaba el suelo de afuera
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = fog;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;
#if UNITY_EDITOR
        var skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Exterior/Sky.mat");
        if (skyMat != null) RenderSettings.skybox = skyMat;
#endif
        sky.Apply();
        // al atardecer, luz de ambiente cálida (lo que da la espalda al sol no queda negro)
        if (mood == Mood.Atardecer || mood == Mood.Crepusculo || mood == Mood.Amanecer)
        {
            sky.driveAmbient = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            bool dawn = mood == Mood.Amanecer;
            RenderSettings.ambientSkyColor = dawn ? new Color(0.72f, 0.7f, 0.86f) : new Color(0.58f, 0.45f, 0.55f);
            RenderSettings.ambientEquatorColor = dawn ? new Color(0.72f, 0.6f, 0.58f) : new Color(0.5f, 0.36f, 0.34f);
            RenderSettings.ambientGroundColor = dawn ? new Color(0.26f, 0.24f, 0.26f) : new Color(0.18f, 0.14f, 0.16f);
        }
        // de noche y en la gruta, luz de ambiente azul (si no, todo lo que no alumbra un farol queda negro)
        if (mood == Mood.Noche || mood == Mood.Gruta)
        {
            sky.driveAmbient = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = mood == Mood.Gruta ? new Color(0.16f, 0.26f, 0.4f) : new Color(0.22f, 0.28f, 0.5f);
            RenderSettings.ambientEquatorColor = mood == Mood.Gruta ? new Color(0.1f, 0.17f, 0.26f) : new Color(0.15f, 0.18f, 0.32f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.06f, 0.1f);
        }

        // polen de día; luciérnagas de noche y en la gruta
        var ps = new GameObject(fireflies ? "Luciernagas" : "Polen").AddComponent<ParticleSystem>();
        ps.transform.SetParent(g, false);
        ps.transform.localPosition = new Vector3(0f, 2.5f, 2f);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startSize = fireflies ? new ParticleSystem.MinMaxCurve(0.08f, 0.18f) : new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = fireflies ? (mood == Mood.Gruta ? new Color(0.5f, 0.9f, 1f, 1f) : new Color(0.85f, 1f, 0.45f, 1f)) : new Color(1f, 0.92f, 0.65f, 0.7f);
        main.maxParticles = 400;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = fireflies ? 30f : 45f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(Mathf.Max(70f, Walk.width), 5f, Mathf.Max(70f, Walk.height));
        if (layout >= Layout.Lago) { ps.transform.localPosition = new Vector3(Walk.center.x, 2.5f + SpawnPoint.y, Walk.center.y); main.maxParticles = 1200; em.rateOverTime = fireflies ? 90f : 130f; }
        var noise = ps.noise; noise.enabled = true; noise.strength = fireflies ? 0.5f : 0.25f; noise.frequency = 0.2f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = fireflies ? mSparkle : mDust;
        ps.Play();

        if (stars) Stars(g);
        if (layout == Layout.Puerto || layout == Layout.Acantilado) SeaSparkles(g);
        else if (layout == Layout.Pueblo) Sparkles(g);
    }

    /// <summary>Cielo estrellado: puntos quietos en una cúpula lejana, algunos más grandes y azulados.</summary>
    void Stars(Transform parent)
    {
        var ps = new GameObject("Estrellas").AddComponent<ParticleSystem>();
        ps.transform.SetParent(parent, false);
        ps.transform.localPosition = new Vector3(0f, 0f, 0f);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.startLifetime = 100000f;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.7f, 2.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.85f), new Color(0.7f, 0.82f, 1f));
        main.maxParticles = 2500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.loop = false;
        var em = ps.emission; em.rateOverTime = 0f; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 2200) });
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Hemisphere; sh.radius = 420f; sh.radiusThickness = 0f;
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mSparkle;
        r.maxParticleSize = 0.02f;
        ps.Play();
    }

    /// <summary>Destellos del sol en el agua: puntitos que aparecen y se apagan sobre el río (el sello del HD-2D).</summary>
    void Sparkles(Transform parent)
    {
        float len = 90f;
        int n = 6;
        for (int i = 0; i < n; i++)
        {
            float x = RiverStartX + (i + 0.5f) * len / n;
            var ps = new GameObject("Destellos").AddComponent<ParticleSystem>();
            ps.transform.SetParent(parent, false);
            ps.transform.localPosition = new Vector3(x, WaterLevel + 0.06f, Meander(x));
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.16f);
            main.startColor = new Color(1f, 1f, 1f, 1f);
            main.maxParticles = 200; main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 140f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(len / n, 0.02f, 6.4f);
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)));
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mSparkle;
            ps.Play();
        }
    }

    /// <summary>Rayos de sol inclinados que cruzan el aire (planos aditivos que miran a la cámara en Y).</summary>
    void GodRays(Transform parent)
    {
        var g = Group("Rayos", parent);
        foreach (var p in new[] { new Vector3(-14f, 9f, -4f), new Vector3(6f, 10f, 6f), new Vector3(20f, 9f, -10f), new Vector3(-4f, 11f, 26f), new Vector3(-24f, 8f, 8f) })
        {
            var m = new Mesh { name = "Rayo" };
            float w = R(4f, 7f), h = 22f;
            m.vertices = new[] { new Vector3(-w / 2f, -h, 0f), new Vector3(w / 2f, -h, 0f), new Vector3(-w / 2f, 0f, 0f), new Vector3(w / 2f, 0f, 0f) };
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            m.triangles = new[] { 0, 2, 3, 0, 3, 1, 0, 3, 2, 0, 1, 3 };   // dos caras
            m.RecalculateNormals(); m.RecalculateBounds();
            var go = MeshObject(g, "Rayo", m, mRay, false);
            go.transform.localPosition = p + Vector3.up * 8f;
            go.transform.localRotation = Quaternion.Euler(-28f, 34f + R(-10f, 10f), 0f);   // inclinados como el sol
        }
    }

    void Smoke(Transform parent, Vector3 pos, Color color, bool embers)
    {
        var ps = new GameObject(embers ? "Chispas" : "Humo").AddComponent<ParticleSystem>();
        ps.transform.SetParent(parent, false);
        ps.transform.localPosition = pos;
        ps.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.startLifetime = embers ? new ParticleSystem.MinMaxCurve(0.6f, 1.2f) : new ParticleSystem.MinMaxCurve(3f, 5f);
        main.startSpeed = embers ? new ParticleSystem.MinMaxCurve(0.8f, 1.6f) : new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
        main.startSize = embers ? new ParticleSystem.MinMaxCurve(0.05f, 0.12f) : new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
        main.startColor = color;
        main.maxParticles = 60;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = embers ? 12f : 4f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = embers ? 0.3f : 0.1f;
        if (!embers) { var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 2f)); }
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = embers ? mDust : mSmoke;
        ps.Play();
    }

    // ------------------------------------------------------------------ materiales

    void LoadMaterials()
    {
        mGrass = Saved("Dio_Pasto", () => { var m = new Material(Ext("Grass")); m.SetColor("_BaseColor", new Color(0.82f, 0.86f, 0.62f)); return m; }); mBark = Ext("Bark"); mPlank = Ext("Plank"); mDoor = Ext("Door"); mMetal = Ext("Metal");
        mCliff = Ext("Cliff"); mStone = Ext("Stone"); mBrick = Ext("TowerBrick"); mTuft = Ext("GrassTuft");
        mWindow = Ext("WindowDark"); mWindowLit = Ext("WindowLit"); mWater = Ext("LakeWater"); mSmoke = Ext("CloudPuff"); mDust = Ext("Firefly");
        mLeaves = new[] { Ext("Leaves_A"), Ext("Leaves_B"), Ext("Leaves_C") };
        mCobble = TexMat("Dio_Empedrado", "Assets/Textures/Exterior/Stone.png", null, new Color(0.95f, 0.88f, 0.78f), 0.12f);
        mPlaza = TexMat("Dio_Losas", "Assets/Textures/Pisos/base_floor_albedo.png", "Assets/Textures/Pisos/base_floor_normal.png", new Color(1.1f, 1.05f, 0.95f), 0.12f);
        mPlaster = Plain("Dio_Encalado", new Color(0.93f, 0.89f, 0.8f), 0.05f);
        mTimber = Plain("Dio_Viga", new Color(0.26f, 0.17f, 0.11f), 0.1f);
        mPine = Plain("Dio_Pino", new Color(0.16f, 0.32f, 0.2f), 0.05f);
        mSnow = Plain("Dio_Nieve", new Color(0.95f, 0.96f, 1f), 0.2f);
        mCloth = Plain("Dio_TelaRoja", new Color(0.75f, 0.18f, 0.16f), 0.05f);
        mClothB = Plain("Dio_TelaCrema", new Color(0.95f, 0.9f, 0.78f), 0.05f);
        mFruitR = Plain("Dio_Manzana", new Color(0.8f, 0.12f, 0.1f), 0.5f);
        mFruitG = Plain("Dio_Pera", new Color(0.45f, 0.7f, 0.2f), 0.4f);
        mFruitY = Plain("Dio_Limon", new Color(0.95f, 0.8f, 0.2f), 0.4f);
        mGlow = Glow("Dio_Brasa", new Color(1f, 0.55f, 0.2f), 4f);
        mFlowers = new[] { Plain("Dio_FlorRoja", new Color(0.9f, 0.2f, 0.25f), 0.2f), Plain("Dio_FlorAmarilla", new Color(1f, 0.85f, 0.25f), 0.2f),
                           Plain("Dio_FlorBlanca", new Color(1f, 0.97f, 0.95f), 0.2f), Plain("Dio_FlorLila", new Color(0.65f, 0.45f, 0.9f), 0.2f) };
        mRoof = TexMat("Dio_Tejas", RoofTexture(), null, Color.white, 0.1f);

        // pixel-art (Tools/texgen/pixel_diorama.py): filtro Point para que se vea el pixel de cerca
        mGrass = PixelMat("Dio_PastoPix", "pix_grass", new Color(1.05f, 1.08f, 0.95f), 0.05f);
        mRock = PixelMat("Dio_RocaPix", "pix_rock", Color.white, 0.12f);
        mDirt = PixelMat("Dio_TierraPix", "pix_dirt", Color.white, 0.05f);
        mBarkPix = PixelMat("Dio_CortezaPix", "pix_bark", Color.white, 0.05f);
        mPinePix = PixelMat("Dio_PinoPix", "pix_pine", new Color(1.1f, 1.15f, 1.05f), 0.05f);
        mLeavesPix = PixelMat("Dio_HojasPix", "pix_leaves", new Color(1.15f, 1.2f, 1.05f), 0.08f);
        mLeavesAutumn = PixelMat("Dio_HojasOtonoPix", "pix_leaves_autumn", new Color(1.25f, 1.05f, 0.85f), 0.08f);
        mTuft = Saved("Dio_Mechon", () => { var m = new Material(Ext("GrassTuft")); m.SetColor("_BaseColor", new Color(0.78f, 0.9f, 0.55f)); return m; });
        mWaterfall = Saved("Dio_Cascada", () => { var m = new Material(Ext("Waterfall")); m.SetColor("_Color", new Color(0.35f, 0.82f, 0.92f, 0.85f)); return m; });
        mMountain = Saved("Dio_Montana4", () => { var m = new Material(mRock); m.SetColor("_BaseColor", new Color(0.68f, 0.78f, 0.92f)); m.mainTextureScale = new Vector2(3f, 3f); return m; });
        mPlank = PixelMat("Dio_TablonPix", "pix_bark", new Color(1.55f, 1.4f, 1.25f), 0.05f);   // madera gastada, no naranja
        mLily = Plain("Dio_Nenufar", new Color(0.3f, 0.55f, 0.25f), 0.4f);
        mLilyFlower = Plain("Dio_Lirio", new Color(1f, 0.62f, 0.8f), 0.3f);
        // agua turquesa brillante (DQ3 HD-2D)
        mWater = Saved("Dio_Agua", () =>
        {
            var m = new Material(Ext("LakeWater"));
            m.SetColor("_ShallowColor", new Color(0.25f, 0.85f, 0.92f, 0.55f));
            m.SetColor("_DeepColor", new Color(0.03f, 0.35f, 0.6f, 0.92f));
            m.SetFloat("_DepthDistance", 1.4f);
            m.SetFloat("_Reflection", 0.6f);
            m.SetFloat("_WaveHeight", 0.03f);
            m.SetFloat("_WaveSpeed", 0.9f);
            m.SetFloat("_FoamDistance", 0.45f);
            return m;
        });
        // ríos y arroyos: la misma agua, pero con corriente hacia +X (río abajo)
        mRiver = Saved("Dio_AguaRio", () => { var m = new Material(mWater); m.SetFloat("_FoamDistance", 0.6f); return m; });
        mRiver.SetVector("_Flow", Vector4.zero);   // la corriente la lleva cada malla (uv1): lagos y pozas quietos, ríos río abajo
        mRiver.SetFloat("_VertexFlow", 1f);
        // rayos de luz y destellos (aditivos, shader Abby/Particle)
        mRay = Saved("Dio_Rayo", () =>
        {
            var m = new Material(Shader.Find("Abby/Particle"));
#if UNITY_EDITOR
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Diorama/rayo.png"));
#endif
            m.SetColor("_BaseColor", new Color(1f, 0.9f, 0.7f, 0.18f));
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            m.renderQueue = 3100;
            return m;
        });
        mSparkle = Saved("Dio_Destello", () =>
        {
            var m = new Material(mDust);
            m.SetColor("_BaseColor", new Color(1.6f, 1.8f, 2f, 1f));
            return m;
        });
    }

    Material PixelMat(string name, string tex, Color tint, float smooth)
    {
        string path = "Assets/Textures/Diorama/" + tex + ".png";
#if UNITY_EDITOR
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && (imp.filterMode != FilterMode.Point || imp.textureCompression != TextureImporterCompression.Uncompressed))
        {
            imp.filterMode = FilterMode.Point;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.SaveAndReimport();
        }
#endif
        return TexMat(name, path, null, tint, smooth);
    }

    static Shader LitShader => Shader.Find("Universal Render Pipeline/Lit");

    Material Ext(string name)
    {
#if UNITY_EDITOR
        var m = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Exterior/" + name + ".mat");
        if (m != null) return m;
#endif
        return Plain("Dio_" + name, Color.gray, 0.1f);
    }

    Material Saved(string name, System.Func<Material> make)
    {
#if UNITY_EDITOR
        const string dir = "Assets/Materials/Diorama";
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/Materials", "Diorama");
        string path = dir + "/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var m = make(); m.name = name;
        AssetDatabase.CreateAsset(m, path);
        return m;
#else
        return make();
#endif
    }

    Material Plain(string name, Color c, float smooth)
    {
        var m = Saved(name, () =>
        {
            var n = new Material(LitShader);
            n.SetColor("_BaseColor", c);
            n.SetFloat("_Smoothness", smooth);
            return n;
        });
        Grano(m);
        return m;
    }

    /// <summary>
    /// Lo que brilla (runas, ventanas, lámparas, energía): un material Lit con emisión HDR sobre una base oscura del
    /// mismo color. Brilla con el Bloom como neón, pero tiene brillo y sombra como un objeto real (antes era Unlit plano).
    /// Se aplica también a los materiales ya guardados.
    /// </summary>
    Material Glow(string name, Color c, float intensity)
    {
        var m = Saved(name, () => new Material(LitShader));
        if (m.shader != LitShader) m.shader = LitShader;
        Color base0 = c * 0.12f; base0.a = 1f;
        Color hdr = c * intensity * 0.6f; hdr.a = 1f;   // con más, el tonemap lo quema a blanco y pierde el color
        if (m.GetColor("_BaseColor") != base0) m.SetColor("_BaseColor", base0);
        if (m.GetColor("_EmissionColor") != hdr) m.SetColor("_EmissionColor", hdr);
        if (m.GetFloat("_Smoothness") != 0.6f) m.SetFloat("_Smoothness", 0.6f);
        if (!m.IsKeywordEnabled("_EMISSION")) m.EnableKeyword("_EMISSION");
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;   // URP solo deja _EMISSION si la marca dice que emite
        if (m.HasProperty("_EmissionEnabled")) m.SetFloat("_EmissionEnabled", 1f);
        return m;
    }

    static Texture2D granoTex;

    /// <summary>Grano fino (mapa de detalle) para que los colores lisos se vean como piedra, madera o tela y no plástico.</summary>
    void Grano(Material m)
    {
#if UNITY_EDITOR
        if (m == null || m.shader != LitShader || m.GetTexture("_BaseMap") != null) return;
        if (granoTex == null) granoTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Textures/Diorama/detalle_grano.png");
        if (granoTex == null || m.GetTexture("_DetailAlbedoMap") == granoTex) return;
        m.SetTexture("_DetailAlbedoMap", granoTex);
        m.SetTextureScale("_DetailAlbedoMap", new Vector2(3f, 3f));
        m.SetFloat("_DetailAlbedoMapScale", 1f);
        m.EnableKeyword("_DETAIL_MULX2");
#endif
    }

    Material TexMat(string name, string albedo, string normal, Color tint, float smooth) => Saved(name, () =>
    {
        var m = new Material(LitShader);
#if UNITY_EDITOR
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(albedo);
        if (tex != null) m.SetTexture("_BaseMap", tex);
        if (normal != null)
        {
            var n = AssetDatabase.LoadAssetAtPath<Texture2D>(normal);
            if (n != null) { m.SetTexture("_BumpMap", n); m.EnableKeyword("_NORMALMAP"); }
        }
#endif
        m.SetColor("_BaseColor", tint);
        m.SetFloat("_Smoothness", smooth);
        return m;
    });

    /// <summary>Textura de tejas de barro (filas escalonadas con sombra en el borde de abajo). Se guarda como PNG.</summary>
    string RoofTexture()
    {
        const string path = "Assets/Textures/Diorama/Tejas.png";
#if UNITY_EDITOR
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null) return path;
        if (!AssetDatabase.IsValidFolder("Assets/Textures/Diorama")) AssetDatabase.CreateFolder("Assets/Textures", "Diorama");
        const int size = 128, rowH = 16, colW = 16;
        var tex = new Texture2D(size, size, TextureFormat.RGB24, false);
        var r = new System.Random(3);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int row = y / rowH;
                int xo = (x + (row % 2) * colW / 2) % size;
                float u = (xo % colW) / (float)colW, v = (y % rowH) / (float)rowH;
                // teja curva: más clara al centro, oscura en los lados y en el borde de abajo (sombra de la de arriba)
                float curve = 1f - Mathf.Pow(Mathf.Abs(u - 0.5f) * 2f, 2f) * 0.45f;
                float lip = v < 0.18f ? 0.55f + v * 2f : 1f;
                float gap = (xo % colW == 0) ? 0.55f : 1f;
                int tile = (xo / colW) * 31 + row * 17;
                float var = 0.85f + ((tile * 7919) % 97) / 97f * 0.3f;
                float k = curve * lip * gap * var;
                tex.SetPixel(x, y, new Color(0.72f * k, 0.32f * k, 0.2f * k));
            }
        tex.Apply();
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
#endif
        return path;
    }

    // ------------------------------------------------------------------ piezas

    [System.NonSerialized] System.Collections.Generic.Dictionary<PrimitiveType, Mesh> stillMeshes;

    Transform Group(string name, Transform parent)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        return t;
    }

    GameObject Prim(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, bool keepCollider = false)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        if (!keepCollider) DestroyImmediate(go.GetComponent<Collider>());
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        // agua en una primitiva (pozo, estanque, lago redondo): su uv1 (de iluminación) el shader lo leería como corriente
        if (mat != null && mat.shader != null && mat.shader.name == "Abby/Water")
        {
            var mf = go.GetComponent<MeshFilter>();
            if (stillMeshes == null) stillMeshes = new System.Collections.Generic.Dictionary<PrimitiveType, Mesh>();
            if (!stillMeshes.TryGetValue(type, out var still) || still == null)
            {
                still = Instantiate(mf.sharedMesh); still.name = mf.sharedMesh.name + "_Quieta"; still.uv2 = null;
                stillMeshes[type] = still;
            }
            mf.sharedMesh = still;
        }
        return go;
    }

    /// <summary>Caja con la textura repetida por metro (un cubo de Unity la estira en muros largos).</summary>
    GameObject Box(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool collider, float tile = 0.5f)
    {
        var go = MeshObject(parent, name, TiledCube(size, tile), mat, true);
        go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        go.transform.localPosition = pos;
        if (collider) go.AddComponent<BoxCollider>().size = size;
        return go;
    }

    static Mesh TiledCube(Vector3 s, float tile)
    {
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        Vector3 h = s / 2f;
        void Face(Vector3 normal, Vector3 u, Vector3 w, float uLen, float wLen)
        {
            int i = v.Count;
            Vector3 c = Vector3.Scale(normal, h);
            Vector3 du = u * uLen / 2f, dw = w * wLen / 2f;
            v.Add(c - du - dw); v.Add(c + du - dw); v.Add(c - du + dw); v.Add(c + du + dw);
            for (int k = 0; k < 4; k++) n.Add(normal);
            uv.Add(new Vector2(0f, 0f)); uv.Add(new Vector2(uLen * tile, 0f)); uv.Add(new Vector2(0f, wLen * tile)); uv.Add(new Vector2(uLen * tile, wLen * tile));
            t.AddRange(new[] { i, i + 2, i + 3, i, i + 3, i + 1 });
        }
        Face(Vector3.forward, Vector3.left, Vector3.up, s.x, s.y);
        Face(Vector3.back, Vector3.right, Vector3.up, s.x, s.y);
        Face(Vector3.right, Vector3.forward, Vector3.up, s.z, s.y);
        Face(Vector3.left, Vector3.back, Vector3.up, s.z, s.y);
        Face(Vector3.up, Vector3.right, Vector3.forward, s.x, s.z);
        Face(Vector3.down, Vector3.right, Vector3.back, s.x, s.z);
        var m = new Mesh { name = "Caja" };
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateTangents(); m.RecalculateBounds();
        return m;
    }

    void SolidBox(Transform parent, string name, Vector3 center, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = center;
        go.AddComponent<BoxCollider>().size = size;
    }

    GameObject MeshObject(Transform parent, string name, Mesh mesh, Material mat, bool shadows)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = shadows ? UnityEngine.Rendering.ShadowCastingMode.TwoSided : UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }

    /// <summary>Plano horizontal (mira hacia arriba) con la textura repetida 'tile' veces por metro.</summary>
    GameObject Flat(Transform parent, string name, Vector3 center, float w, float d, Material mat, float tile)
    {
        var m = new Mesh { name = name };
        m.vertices = new[] { new Vector3(-w / 2f, 0f, -d / 2f), new Vector3(w / 2f, 0f, -d / 2f), new Vector3(-w / 2f, 0f, d / 2f), new Vector3(w / 2f, 0f, d / 2f) };
        m.uv = new[] { new Vector2(0f, 0f), new Vector2(w * tile, 0f), new Vector2(0f, d * tile), new Vector2(w * tile, d * tile) };
        m.triangles = new[] { 0, 2, 3, 0, 3, 1 };
        m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
        var go = MeshObject(parent, name, m, mat, false);
        go.transform.localPosition = center;
        return go;
    }

    /// <summary>Muro vertical de una cara mirando hacia 'facing'.</summary>
    void Wall(Transform parent, string name, Vector3 center, Vector3 facing, float w, float h, Material mat, float tile)
    {
        var m = new Mesh { name = name };
        m.vertices = new[] { new Vector3(-w / 2f, -h / 2f, 0f), new Vector3(w / 2f, -h / 2f, 0f), new Vector3(-w / 2f, h / 2f, 0f), new Vector3(w / 2f, h / 2f, 0f) };
        m.uv = new[] { new Vector2(0f, 0f), new Vector2(w * tile, 0f), new Vector2(0f, h * tile), new Vector2(w * tile, h * tile) };
        m.triangles = new[] { 0, 2, 3, 0, 3, 1 };
        m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
        var go = MeshObject(parent, name, m, mat, false);
        go.transform.localPosition = center;
        go.transform.localRotation = Quaternion.LookRotation(-facing, Vector3.up);
    }

    Light AddLight(Transform parent, Vector3 pos, Color c, float range, float intensity)
    {
        var l = new GameObject("Luz").AddComponent<Light>();
        l.transform.SetParent(parent, false);
        l.transform.localPosition = pos;
        l.type = LightType.Point; l.color = c; l.range = range; l.intensity = intensity; l.shadows = LightShadows.None;
        return l;
    }

    [System.NonSerialized] Mesh tuftCache;
    Mesh TuftMesh()
    {
        if (tuftCache != null) return tuftCache;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        for (int q = 0; q < 2; q++)
        {
            var rot = Quaternion.Euler(0f, q * 90f + 45f, 0f);
            int b = v.Count;
            v.Add(rot * new Vector3(-0.3f, 0f, 0f)); v.Add(rot * new Vector3(0.3f, 0f, 0f));
            v.Add(rot * new Vector3(-0.3f, 0.5f, 0f)); v.Add(rot * new Vector3(0.3f, 0.5f, 0f));
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(0, 1)); uv.Add(new Vector2(1, 1));
            tri.AddRange(new[] { b, b + 2, b + 3, b, b + 3, b + 1 });
        }
        var m = new Mesh { name = "Tuft" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        var n = new Vector3[v.Count]; for (int i = 0; i < n.Length; i++) n[i] = Vector3.up;
        m.normals = n; m.RecalculateBounds();
        return tuftCache = m;
    }

    /// <summary>Cono de radio 1 y alto 1 con base en y = 0 (pinos, montañas, agujas).</summary>
    static Mesh Cone(float r, float h, int segs)
    {
        var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
        for (int i = 0; i < segs; i++)
        {
            float a0 = i / (float)segs * Mathf.PI * 2f, a1 = (i + 1) / (float)segs * Mathf.PI * 2f;
            int b = v.Count;
            v.Add(new Vector3(Mathf.Cos(a0) * r, 0f, Mathf.Sin(a0) * r)); v.Add(new Vector3(0f, h, 0f)); v.Add(new Vector3(Mathf.Cos(a1) * r, 0f, Mathf.Sin(a1) * r));
            uv.Add(new Vector2(i / (float)segs * 4f, 0f)); uv.Add(new Vector2((i + 0.5f) / segs * 4f, 2f)); uv.Add(new Vector2((i + 1f) / segs * 4f, 0f));
            t.AddRange(new[] { b, b + 1, b + 2 });
        }
        var m = new Mesh { name = "Cone" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    static Mesh Ring(float rOut, float rIn, float h, int segs)
    {
        var v = new List<Vector3>(); var t = new List<int>(); var uv = new List<Vector2>();
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int i = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(0, 1)); uv.Add(new Vector2(1, 1));
            t.AddRange(new[] { i, i + 2, i + 3, i, i + 3, i + 1 });
        }
        for (int s = 0; s < segs; s++)
        {
            float a0 = s / (float)segs * Mathf.PI * 2f, a1 = (s + 1) / (float)segs * Mathf.PI * 2f;
            Vector3 o0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)), o1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1));
            Quad(o0 * rOut, o1 * rOut, o0 * rOut + Vector3.up * h, o1 * rOut + Vector3.up * h);      // cara de afuera
            Quad(o1 * rIn, o0 * rIn, o1 * rIn + Vector3.up * h, o0 * rIn + Vector3.up * h);          // cara de adentro
            Quad(o1 * rIn + Vector3.up * h, o0 * rIn + Vector3.up * h, o1 * rOut + Vector3.up * h, o0 * rOut + Vector3.up * h);   // borde
        }
        var m = new Mesh { name = "Ring" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>Techo de dos aguas con la cumbrera a lo largo de X; base en y = 0.</summary>
    static Mesh Roof(float w, float d, float h)
    {
        float hx = w / 2f, hz = d / 2f;
        var v = new List<Vector3>
        {
            new Vector3(-hx, 0f, hz), new Vector3(hx, 0f, hz), new Vector3(-hx, h, 0f), new Vector3(hx, h, 0f),
            new Vector3(hx, 0f, -hz), new Vector3(-hx, 0f, -hz), new Vector3(hx, h, 0f), new Vector3(-hx, h, 0f),
        };
        float slope = Mathf.Sqrt(hz * hz + h * h);
        var uv = new List<Vector2>
        {
            new Vector2(0, 0), new Vector2(w / 2f, 0), new Vector2(0, slope / 2f), new Vector2(w / 2f, slope / 2f),
            new Vector2(0, 0), new Vector2(w / 2f, 0), new Vector2(0, slope / 2f), new Vector2(w / 2f, slope / 2f),
        };
        var t = new List<int> { 0, 3, 2, 0, 1, 3, 4, 7, 6, 4, 5, 7 };
        var m = new Mesh { name = "Roof" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>Los dos triángulos que cierran los lados del techo (hastiales), en x = ±w/2.</summary>
    static Mesh Gable(float w, float h, float hz)
    {
        float hx = w / 2f;
        var v = new List<Vector3>
        {
            new Vector3(hx, 0f, hz), new Vector3(hx, 0f, -hz), new Vector3(hx, h, 0f),
            new Vector3(-hx, 0f, -hz), new Vector3(-hx, 0f, hz), new Vector3(-hx, h, 0f),
        };
        var uv = new List<Vector2> { new Vector2(0, 0), new Vector2(hz, 0), new Vector2(hz / 2f, h / 2f), new Vector2(0, 0), new Vector2(hz, 0), new Vector2(hz / 2f, h / 2f) };
        var t = new List<int> { 0, 1, 2, 3, 4, 5 };
        var m = new Mesh { name = "Gable" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }
}

static class DioramaRectExt
{
    /// <summary>Huecos ordenados de izquierda a derecha.</summary>
    public static Vector2[] OrderedByX(this Vector2[] gaps)
    {
        var c = (Vector2[])gaps.Clone();
        System.Array.Sort(c, (a, b) => a.x.CompareTo(b.x));
        return c;
    }
}
