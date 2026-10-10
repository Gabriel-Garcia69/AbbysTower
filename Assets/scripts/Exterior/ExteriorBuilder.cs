using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Genera el exterior antes del primer nivel: una meseta suspendida sobre el ABISMO.
/// - Plaza con fuente central al sur.
/// - La Abby's Tower en lo alto de un risco de roca al norte, con escalinata desde la plaza (su portal lleva al Nivel 1).
/// - Un lago al este que se derrama por el borde en una cascada hacia el abismo.
/// - Acantilados que bajan a un mar de nubes, islas flotando a lo lejos, cielo de atardecer.
/// - Árboles, pasto, rocas, faroles con sombras y luciérnagas.
///
/// Todo es procedural (texturas pixel-art incluidas); con arte real se reemplazan los materiales en Assets/Materials/Exterior.
/// Uso: clic derecho en el componente → "Generar exterior". Solo funciona en el editor.
/// </summary>
public class ExteriorBuilder : MonoBehaviour
{
    [Header("General")]
    public int seed = 7;
    [Tooltip("Escena que carga el portal de la torre (tiene que estar en Build Settings).")]
    public string towerScene = "Nivel1";

    [Header("Meseta (metros)")]
    public Vector2 plateauCenter = new Vector2(0f, 15f);
    public float plateauRadius = 58f;
    public float plazaRadius = 12f;

    [Header("Torre en el risco")]
    public Vector2 towerCenter = new Vector2(0f, 80f);
    public float mesaRadius = 24f;
    public float mesaHeight = 14f;

    [Header("Lago y cascada")]
    public Vector2 lakeCenter = new Vector2(50f, 2f);
    public float lakeRadius = 18f;
    public float waterLevel = -0.6f;

    [Header("Abismo")]
    public float cliffDepth = 170f;

    [Header("Cantidades")]
    public int trees = 55;
    public int grassTufts = 4000;
    public int rocks = 30;
    public int floatingIslands = 12;

    const string Prefix = "Ext_";
    const string MatDir = "Assets/Materials/Exterior";
    const string TexDir = "Assets/Textures/Exterior";
    static readonly Vector2 StarCenter = new Vector2(0f, 40f);   // desde aquí se traza el borde de la meseta
    const int ContourPoints = 360;

    System.Random rng;
    float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

    Vector2[] contour;      // borde de la meseta (una vuelta)

    // ------------------------------------------------------------------ forma del mundo

    static float SmoothMin(float a, float b, float k)
    {
        float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
        return Mathf.Lerp(b, a, h) - k * h * (1f - h);
    }

    /// <summary>Distancia con signo al borde de la meseta (negativa = adentro). Une la meseta principal y el risco de la torre.</summary>
    public float PlateauSdf(Vector2 p)
    {
        float d1 = Vector2.Distance(p, plateauCenter) - plateauRadius;
        float d2 = Vector2.Distance(p, towerCenter) - (mesaRadius + 6f);
        float n = (Mathf.PerlinNoise(p.x * 0.06f + 3.1f, p.y * 0.06f + 7.7f) - 0.5f) * 9f;
        return SmoothMin(d1, d2, 12f) + n;
    }

    float DistTower(Vector2 p) => Vector2.Distance(p, towerCenter);

    /// <summary>Altura del suelo dentro de la meseta: plano en la plaza, el risco de la torre y el hueco del lago.</summary>
    public float GroundHeight(float x, float z)
    {
        var p = new Vector2(x, z);
        float dt = DistTower(p);
        if (dt < mesaRadius - 0.5f) return mesaHeight + (Mathf.PerlinNoise(x * 0.1f, z * 0.1f) - 0.5f) * 0.3f * Mathf.Clamp01((dt - 18f) / 4f);

        float dl = Vector2.Distance(p, lakeCenter);
        float lake = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((lakeRadius - dl) / 7f));
        float plaza = Mathf.Clamp01((p.magnitude - plazaRadius - 4f) / 8f);
        float path = (z > -40f && z < StairsStartZ) ? Mathf.Clamp01((Mathf.Abs(x) - 3f) / 3f) : 1f;   // plano bajo los caminos
        float bumps = (Mathf.PerlinNoise(x * 0.07f + 9f, z * 0.07f + 2f) - 0.5f) * 0.9f * plaza * path * (1f - lake);
        float lip = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((PlateauSdf(p) + 3f) / 3f)) * 0.4f;   // el borde se redondea
        return -2.4f * lake + bumps - lip;
    }

    /// <summary>Punto del borde en la dirección de p (visto desde el centro de trazado).</summary>
    Vector2 EdgePointToward(Vector2 p)
    {
        Vector2 d = p - StarCenter;
        float a = Mathf.Atan2(d.y, d.x);
        float f = (a / (Mathf.PI * 2f) + 1f) % 1f * ContourPoints;
        int i0 = Mathf.FloorToInt(f) % ContourPoints, i1 = (i0 + 1) % ContourPoints;
        return Vector2.Lerp(contour[i0], contour[i1], f - Mathf.Floor(f));
    }

    void TraceContour()
    {
        contour = new Vector2[ContourPoints];
        for (int i = 0; i < ContourPoints; i++)
        {
            float a = i / (float)ContourPoints * Mathf.PI * 2f;
            var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            float lo = 0f, hi = 0f;
            for (float r = 1f; r < 250f; r += 1f)
                if (PlateauSdf(StarCenter + dir * r) > 0f) { lo = r - 1f; hi = r; break; }
            for (int k = 0; k < 14; k++)
            {
                float m = (lo + hi) / 2f;
                if (PlateauSdf(StarCenter + dir * m) > 0f) hi = m; else lo = m;
            }
            contour[i] = StarCenter + dir * lo;
        }
    }

    Vector2 Outward(int i)
    {
        Vector2 a = contour[(i + ContourPoints - 1) % ContourPoints], b = contour[(i + 1) % ContourPoints];
        Vector2 t = (b - a).normalized;
        return new Vector2(t.y, -t.x);   // el contorno va en sentido antihorario: la normal a la derecha apunta afuera
    }

    // ------------------------------------------------------------------ generación

    [ContextMenu("Borrar exterior")]
    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i).gameObject;
            if (c.name.StartsWith(Prefix)) DestroyImmediate(c);
        }
    }

    /// <summary>Punto de aparición de Abby: al sur de la fuente, mirando hacia la torre.</summary>
    public Vector3 PlayerSpawn => transform.TransformPoint(new Vector3(0f, 0f, -22f));

#if UNITY_EDITOR
    Material mGrass, mStone, mStoneDark, mBrick, mBark, mMetal, mTuft, mRock, mCliff, mThatch, mPlank, mDoor;
    Material[] mLeaves;
    Material mLampGlow, mWindowLit, mWindowDark, mRune, mPortal, mBeacon, mFountainWater, mLakeWater, mWaterfall, mSpray, mFirefly, mCloud;

    [ContextMenu("Generar exterior")]
    public void Build()
    {
        Clear();
        rng = new System.Random(seed);
        CreateAssets();
        TraceContour();

        BuildGround();
        BuildCliffs();
        BuildPlazaAndPaths();
        BuildFountain();
        BuildMesaAndStairs();
        BuildTower();
        BuildLakeAndWaterfall();
        BuildAbyss();
        BuildHuts();
        BuildTrees();
        BuildRocks();
        BuildGrass();
        BuildLamps();
        BuildFireflies();
        BuildBoundary();
        EditorUtility.SetDirty(gameObject);
    }

    // ---------------- suelo de la meseta (recortado exactamente al borde)

    void BuildGround()
    {
        var root = Group("Terreno");
        var mesh = ClippedGrid(1f, p => GroundHeight(p.x, p.y), p => PlateauSdf(p) < 0f, 4f);
        var go = MeshObject(root, "Meseta", mesh, mGrass, true, false);
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }

    /// <summary>
    /// Malla de cuadrícula dentro de la meseta: los vértices de afuera se llevan al borde, así el suelo termina justo
    /// donde empieza el acantilado. inside() decide qué celdas se conservan.
    /// </summary>
    Mesh ClippedGrid(float cell, System.Func<Vector2, float> height, System.Func<Vector2, bool> inside, float tile, Rect? area = null)
    {
        Rect r = area ?? ContourBounds(2f);
        int nx = Mathf.CeilToInt(r.width / cell), nz = Mathf.CeilToInt(r.height / cell);
        var verts = new Vector3[(nx + 1) * (nz + 1)];
        var uvs = new Vector2[verts.Length];
        var isIn = new bool[verts.Length];
        for (int z = 0; z <= nz; z++)
            for (int x = 0; x <= nx; x++)
            {
                int i = z * (nx + 1) + x;
                var p = new Vector2(r.xMin + x * cell, r.yMin + z * cell);
                isIn[i] = inside(p);
                if (PlateauSdf(p) >= 0f) p = EdgePointToward(p);
                verts[i] = new Vector3(p.x, height(p), p.y);
                uvs[i] = p / tile;
            }
        var tris = new List<int>();
        for (int z = 0; z < nz; z++)
            for (int x = 0; x < nx; x++)
            {
                int a = z * (nx + 1) + x, b = a + 1, c = a + nx + 1, d = c + 1;
                if (!(isIn[a] || isIn[b] || isIn[c] || isIn[d])) continue;
                tris.AddRange(new[] { a, c, b, b, c, d });
            }
        var m = new Mesh { name = "Grid", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, vertices = verts, uv = uvs };
        m.SetTriangles(tris, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    Rect ContourBounds(float margin)
    {
        Vector2 min = contour[0], max = contour[0];
        foreach (var p in contour) { min = Vector2.Min(min, p); max = Vector2.Max(max, p); }
        return Rect.MinMaxRect(min.x - margin, min.y - margin, max.x + margin, max.y + margin);
    }

    // ---------------- acantilados hacia el abismo

    void BuildCliffs()
    {
        var root = Group("Acantilados");
        float[] depths = { 0f, 2f, 6f, 13f, 25f, 45f, 75f, 115f, cliffDepth };
        int rows = depths.Length;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        float along = 0f;
        for (int i = 0; i <= ContourPoints; i++)
        {
            int ci = i % ContourPoints;
            if (i > 0) along += Vector2.Distance(contour[ci], contour[i - 1]);
            Vector2 p = contour[ci], o = Outward(ci);
            float top = GroundHeight(p.x, p.y);
            for (int j = 0; j < rows; j++)
            {
                float d = depths[j];
                // roca irregular que se va estrechando hacia abajo (la meseta flota sobre el abismo)
                float jitter = j == 0 ? 0f : (Mathf.PerlinNoise(along * 0.08f, d * 0.05f) - 0.5f) * 5f + Mathf.Sin(along * 0.31f + d) * 0.6f;
                float taper = -d * 0.22f;
                Vector2 q = p + o * (jitter + taper);
                v.Add(new Vector3(q.x, top - d, q.y));
                uv.Add(new Vector2(along / 6f, (top - d) / 6f));
            }
        }
        for (int i = 0; i < ContourPoints; i++)
            for (int j = 0; j < rows - 1; j++)
            {
                int a = i * rows + j, b = a + 1, c = a + rows, d = c + 1;
                tri.AddRange(new[] { a, c, b, c, d, b });   // normal hacia afuera (hacia el abismo)
            }
        var m = new Mesh { name = "Cliffs", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        MeshObject(root, "Paredes", m, mCliff, true, true);
    }

    // ---------------- plaza, caminos

    void BuildPlazaAndPaths()
    {
        var root = Group("Plaza");
        MeshObject(root, "Piso", Disc(plazaRadius, 64, 8, 2f, 0.03f), mStone, false, false);
        Strip(root, "Camino_Norte", new Vector3(0f, 0.025f, (plazaRadius + StairsStartZ) / 2f), 4.5f, StairsStartZ - plazaRadius + 0.5f);
        Strip(root, "Camino_Sur", new Vector3(0f, 0.025f, -plazaRadius - 9f), 4.5f, 20f);
    }

    GameObject Strip(Transform parent, string name, Vector3 pos, float width, float length)
    {
        var go = MeshObject(parent, name, Quad(width, length, 2f), mStone, false, false);
        go.transform.localPosition = pos;
        return go;
    }

    // ---------------- fuente

    void BuildFountain()
    {
        var root = Group("Fuente");
        var basin = MeshObject(root, "Pileta", Ring(4.6f, 4.1f, 0.7f, 64, 1.5f), mStone, true, true);
        basin.AddComponent<MeshCollider>().sharedMesh = basin.GetComponent<MeshFilter>().sharedMesh;
        MeshObject(root, "Fondo", Disc(4.1f, 48, 4, 1.5f, 0.04f), mStoneDark, false, false);
        MeshObject(root, "Agua", Disc(4.12f, 96, 24, 1f, 0.52f), mFountainWater, false, false);

        var col = MeshObject(root, "Columna", Cylinder(0.55f, 0.4f, 1.65f, 20, 1.5f, true), mStone, true, true);
        col.AddComponent<CapsuleCollider>().radius = 0.6f;
        var bowl = MeshObject(root, "Plato", Ring(1.75f, 1.5f, 0.35f, 40, 1.5f), mStone, true, true);
        bowl.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        MeshObject(root, "PlatoFondo", Cylinder(1.75f, 0.6f, 0.25f, 40, 1.5f, false), mStone, true, false).transform.localPosition = new Vector3(0f, 1.3f, 0f);
        MeshObject(root, "PlatoBase", Disc(1.5f, 40, 2, 1.5f, 1.58f), mStoneDark, false, false);
        MeshObject(root, "AguaPlato", Disc(1.52f, 64, 10, 1f, 1.82f), mFountainWater, false, false);
        MeshObject(root, "Surtidor", Cylinder(0.2f, 0.12f, 0.75f, 12, 1f, true), mStone, true, false).transform.localPosition = new Vector3(0f, 1.6f, 0f);

        Particles(root, "Chorro", new Vector3(0f, 2.35f, 0f), mSpray, ps =>
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.15f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.8f, 3.4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
            main.gravityModifier = 1f;
            main.startColor = new Color(0.8f, 0.95f, 1f, 0.75f);
            main.maxParticles = 600;
            var em = ps.emission; em.rateOverTime = 240f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 9f; sh.radius = 0.04f; sh.rotation = new Vector3(-90f, 0f, 0f);
        });
        Particles(root, "Cortina", new Vector3(0f, 1.85f, 0f), mSpray, ps =>
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.gravityModifier = 1f;
            main.startColor = new Color(0.75f, 0.92f, 1f, 0.6f);
            main.maxParticles = 800;
            var em = ps.emission; em.rateOverTime = 420f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.72f; sh.radiusThickness = 0f; sh.rotation = new Vector3(-90f, 0f, 0f);
        });
        Particles(root, "Bruma", new Vector3(0f, 0.6f, 0f), mSpray, ps =>
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.gravityModifier = -0.02f;
            main.startColor = new Color(0.85f, 0.95f, 1f, 0.12f);
            var em = ps.emission; em.rateOverTime = 25f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = 1.9f; sh.rotation = new Vector3(-90f, 0f, 0f);
        });
        var l = AddLight(root, "LuzAgua", new Vector3(0f, 0.3f, 0f), new Color(0.35f, 0.85f, 1f), 7f, 6f, LightShadows.None);
        l.gameObject.AddComponent<LightFlicker>().amount = 0.08f;
    }

    // ---------------- risco de la torre y escalinata

    float StairsStartZ => towerCenter.y - mesaRadius - mesaHeight * 1.9f;   // pendiente ~28°
    float StairsEndZ => towerCenter.y - mesaRadius + 0.3f;

    void BuildMesaAndStairs()
    {
        var root = Group("Risco");

        // pared de roca irregular alrededor del risco (tapa el escalón del terreno)
        int segs = 72;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        float[] hs = { -1f, 2f, 5f, 8f, 11f, mesaHeight };
        for (int s = 0; s <= segs; s++)
        {
            float a = s / (float)segs * Mathf.PI * 2f;
            var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            for (int j = 0; j < hs.Length; j++)
            {
                float jitter = j == hs.Length - 1 ? 0.3f : (Mathf.PerlinNoise(s * 0.35f, j * 0.7f) - 0.3f) * 2.2f;
                float r = mesaRadius + jitter + (hs.Length - 1 - j) * 0.35f;
                v.Add(new Vector3(towerCenter.x, 0f, towerCenter.y) + d * r + Vector3.up * hs[j]);
                uv.Add(new Vector2(s / (float)segs * Mathf.PI * 2f * mesaRadius / 6f, hs[j] / 6f));
            }
        }
        for (int s = 0; s < segs; s++)
        {
            float mid = (s + 0.5f) / segs * 360f;
            if (Mathf.Abs(mid - 180f) < 9f) continue;   // hueco al sur: ahí llega la escalinata
            for (int j = 0; j < hs.Length - 1; j++)
            {
                int a = s * hs.Length + j, b = a + 1, c = a + hs.Length, dd = c + 1;
                tri.AddRange(new[] { a, c, b, b, c, dd });
            }
        }
        var wall = new Mesh { name = "MesaWall" };
        wall.SetVertices(v); wall.SetUVs(0, uv); wall.SetTriangles(tri, 0);
        wall.RecalculateNormals(); wall.RecalculateBounds();
        var w = MeshObject(root, "ParedRoca", wall, mCliff, true, true);
        w.AddComponent<MeshCollider>().sharedMesh = wall;

        // escalinata (escalones visibles; rampa lisa invisible como collider)
        var stairs = Group("Escalinata", root);
        float z0 = StairsStartZ, z1 = StairsEndZ, width = 5f;
        int steps = Mathf.RoundToInt(mesaHeight / 0.35f);
        float stepD = (z1 - z0) / steps, stepH = mesaHeight / steps;
        var sv = new List<Vector3>(); var suv = new List<Vector2>(); var st = new List<int>();
        void Box(Vector3 min, Vector3 max)
        {
            // solo caras visibles: arriba, frente y los lados
            void Face(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector2 size)
            {
                int i = sv.Count;
                sv.Add(a); sv.Add(b); sv.Add(c); sv.Add(d);
                suv.Add(Vector2.zero); suv.Add(new Vector2(size.x, 0f)); suv.Add(new Vector2(0f, size.y)); suv.Add(size);
                st.AddRange(new[] { i, i + 2, i + 3, i, i + 3, i + 1 });
            }
            float t = 2f;
            Face(new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z), new Vector3(min.x, max.y, max.z), new Vector3(max.x, max.y, max.z), new Vector2(max.x - min.x, max.z - min.z) / t);   // arriba
            Face(new Vector3(min.x, min.y, min.z), new Vector3(max.x, min.y, min.z), new Vector3(min.x, max.y, min.z), new Vector3(max.x, max.y, min.z), new Vector2(max.x - min.x, max.y - min.y) / t);   // frente
            Face(new Vector3(min.x, min.y, max.z), new Vector3(min.x, min.y, min.z), new Vector3(min.x, max.y, max.z), new Vector3(min.x, max.y, min.z), new Vector2(max.z - min.z, max.y - min.y) / t);   // izq
            Face(new Vector3(max.x, min.y, min.z), new Vector3(max.x, min.y, max.z), new Vector3(max.x, max.y, min.z), new Vector3(max.x, max.y, max.z), new Vector2(max.z - min.z, max.y - min.y) / t);   // der
        }
        // cada escalón solo ocupa su propio fondo (antes todos llegaban hasta arriba y sus lados se encimaban: parpadeo)
        for (int i = 0; i < steps; i++)
            Box(new Vector3(-width / 2f, 0f, z0 + i * stepD), new Vector3(width / 2f, (i + 1) * stepH, i == steps - 1 ? z1 + 0.5f : z0 + (i + 1) * stepD));
        // bloques de piedra a los lados, donde la escalinata entra al risco (con collider: se puede subir encima)
        foreach (float side in new[] { -1f, 1f })
        {
            float xa = side * (width / 2f + 0.3f), xb = side * (width / 2f + 2.2f);
            var bmin = new Vector3(Mathf.Min(xa, xb), 0f, z1 - 7f); var bmax = new Vector3(Mathf.Max(xa, xb), mesaHeight, z1 + 1f);
            Box(bmin, bmax);
            var bc = new GameObject("BloqueLado");
            bc.transform.SetParent(stairs, false);
            bc.transform.localPosition = (bmin + bmax) / 2f;
            bc.AddComponent<BoxCollider>().size = bmax - bmin;
        }
        var sm = new Mesh { name = "Stairs", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        sm.SetVertices(sv); sm.SetUVs(0, suv); sm.SetTriangles(st, 0);
        sm.RecalculateNormals(); sm.RecalculateBounds();
        MeshObject(stairs, "Escalones", sm, mStone, true, true);

        // collider: rampa lisa + barandales invisibles
        var ramp = new GameObject("Rampa");
        ramp.transform.SetParent(stairs, false);
        float len = Mathf.Sqrt((z1 - z0) * (z1 - z0) + mesaHeight * mesaHeight);
        float ang = Mathf.Atan2(mesaHeight, z1 - z0) * Mathf.Rad2Deg;
        ramp.transform.localPosition = new Vector3(0f, mesaHeight / 2f - 0.25f, (z0 + z1) / 2f);
        ramp.transform.localRotation = Quaternion.Euler(-ang, 0f, 0f);
        ramp.AddComponent<BoxCollider>().size = new Vector3(width, 0.5f, len);
        foreach (float side in new[] { -1f, 1f })
        {
            var rail = Primitive(stairs, PrimitiveType.Cube, "Barandal", Vector3.zero, Vector3.one, mMetal);
            rail.transform.localPosition = new Vector3(side * (width / 2f + 0.15f), mesaHeight / 2f + 0.6f, (z0 + z1) / 2f);
            rail.transform.localRotation = Quaternion.Euler(-ang, 0f, 0f);
            rail.transform.localScale = new Vector3(0.2f, 0.12f, len);
            for (int k = 0; k <= 6; k++)
            {
                float f = k / 6f;
                var post = Primitive(stairs, PrimitiveType.Cube, "Poste", new Vector3(side * (width / 2f + 0.15f), Mathf.Lerp(0f, mesaHeight, f) + 0.3f, Mathf.Lerp(z0, z1, f)), new Vector3(0.18f, 1.2f, 0.18f), mMetal);
                post.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
            var block = new GameObject("BloqueoLateral");
            block.transform.SetParent(stairs, false);
            block.transform.localPosition = new Vector3(side * (width / 2f + 0.3f), mesaHeight / 2f + 1f, (z0 + z1) / 2f);
            block.transform.localRotation = Quaternion.Euler(-ang, 0f, 0f);
            block.AddComponent<BoxCollider>().size = new Vector3(0.4f, 3f, len);
        }
    }

    // ---------------- torre

    void BuildTower()
    {
        var root = Group("AbbysTower");
        root.localPosition = new Vector3(towerCenter.x, mesaHeight, towerCenter.y);

        var plinth = MeshObject(root, "Base", Cylinder(18f, 18f, 0.25f, 64, 2f, true), mStone, true, true);
        plinth.AddComponent<MeshCollider>().sharedMesh = plinth.GetComponent<MeshFilter>().sharedMesh;

        const int segments = 6;
        const float segH = 20f;
        float r0 = 14f, r1 = 9f;
        for (int i = 0; i < segments; i++)
        {
            float ra = Mathf.Lerp(r0, r1, i / (float)segments), rb = Mathf.Lerp(r0, r1, (i + 1) / (float)segments);
            float y = 0.25f + i * segH;
            MeshObject(root, "Cuerpo_" + i, Cylinder(ra, rb, segH, 48, 3f, false), mBrick, true, true).transform.localPosition = new Vector3(0f, y, 0f);
            MeshObject(root, "Anillo_" + i, Cylinder(rb + 0.7f, rb + 0.7f, 1.2f, 48, 2f, true), mMetal, true, true).transform.localPosition = new Vector3(0f, y + segH - 0.6f, 0f);
            MeshObject(root, "Runa_" + i, Cylinder(rb + 0.75f, rb + 0.75f, 0.18f, 48, 2f, false), mRune, false, false).transform.localPosition = new Vector3(0f, y + segH - 0.1f, 0f);

            int windows = 9 - i;
            for (int w = 0; w < windows; w++)
            {
                float a = (w + R(0.1f, 0.9f)) / windows * Mathf.PI * 2f;
                float wy = y + R(4f, segH - 5f);
                float rr = Mathf.Lerp(ra, rb, (wy - y) / segH) + 0.06f;
                var win = MeshObject(root, "Ventana", Quad(1.1f, 2.2f, 10f, true), R(0f, 1f) < 0.7f ? mWindowLit : mWindowDark, false, false);
                win.transform.localPosition = new Vector3(Mathf.Sin(a) * rr, wy, Mathf.Cos(a) * rr);
                win.transform.localRotation = Quaternion.LookRotation(-new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)));   // la cara visible del quad mira a -Z local
            }
        }
        float topY = 0.25f + segments * segH;
        MeshObject(root, "Aguja", Cylinder(r1, 0.4f, 34f, 48, 3f, false), mBrick, true, true).transform.localPosition = new Vector3(0f, topY, 0f);
        var orb = Primitive(root, PrimitiveType.Sphere, "Faro", new Vector3(0f, topY + 36f, 0f), Vector3.one * 4f, mBeacon);
        orb.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        AddLight(root, "LuzFaro", orb.transform.localPosition, new Color(0.75f, 0.45f, 1f), 140f, 600f, LightShadows.None).gameObject.AddComponent<LightFlicker>().amount = 0.12f;

        var body = new GameObject("Collider_Cuerpo");
        body.transform.SetParent(root, false);
        var mc = body.AddComponent<MeshCollider>();
        mc.sharedMesh = Cylinder(r0, r0, 40f, 32, 1f, true);
        mc.convex = true;

        // portal (mira al sur, hacia la escalinata)
        var door = Group("Puerta", root);
        door.localPosition = new Vector3(0f, 0.25f, -r0 - 0.05f);
        Primitive(door, PrimitiveType.Cube, "PilarIzq", new Vector3(-2.6f, 3.3f, -0.4f), new Vector3(0.9f, 6.6f, 1f), mMetal);
        Primitive(door, PrimitiveType.Cube, "PilarDer", new Vector3(2.6f, 3.3f, -0.4f), new Vector3(0.9f, 6.6f, 1f), mMetal);
        Primitive(door, PrimitiveType.Cube, "Dintel", new Vector3(0f, 6.9f, -0.4f), new Vector3(6.1f, 0.8f, 1f), mMetal);
        Primitive(door, PrimitiveType.Cube, "RunaIzq", new Vector3(-2.6f, 3.3f, -0.92f), new Vector3(0.15f, 6f, 0.05f), mRune);
        Primitive(door, PrimitiveType.Cube, "RunaDer", new Vector3(2.6f, 3.3f, -0.92f), new Vector3(0.15f, 6f, 0.05f), mRune);
        MeshObject(door, "Portal", Quad(4.3f, 6.5f, 10f, true), mPortal, false, false).transform.localPosition = new Vector3(0f, 3.25f, -0.1f);
        AddLight(door, "LuzPortal", new Vector3(0f, 3f, -2f), new Color(0.7f, 0.4f, 1f), 14f, 10f, LightShadows.Soft).gameObject.AddComponent<LightFlicker>().amount = 0.1f;

        var entry = new GameObject("Entrada");
        entry.transform.SetParent(door, false);
        entry.transform.localPosition = new Vector3(0f, 0f, -1.2f);
        var exit = entry.AddComponent<FloorExit>();
        exit.nextScene = towerScene;
        exit.radius = 1.8f;

        foreach (float x in new[] { -5f, 5f })
        {
            var b = Primitive(door, PrimitiveType.Cylinder, "Brasero", new Vector3(x, 0.6f, -2.5f), new Vector3(0.9f, 0.6f, 0.9f), mMetal);
            var f = Primitive(b.transform, PrimitiveType.Sphere, "Fuego", new Vector3(0f, 1.1f, 0f), new Vector3(0.7f, 0.9f, 0.7f), mLampGlow);
            f.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            AddLight(door, "LuzBrasero", new Vector3(x, 2.2f, -2.5f), new Color(1f, 0.55f, 0.25f), 12f, 18f, LightShadows.Soft).gameObject.AddComponent<LightFlicker>().amount = 0.25f;
        }
    }

    // ---------------- lago y cascada

    void BuildLakeAndWaterfall()
    {
        var root = Group("Lago");

        // superficie del agua: termina exactamente en el borde de la meseta
        var area = new Rect(lakeCenter.x - lakeRadius - 2f, lakeCenter.y - lakeRadius - 2f, lakeRadius * 2f + 4f, lakeRadius * 2f + 4f);
        var water = ClippedGrid(1f, p => waterLevel, p => Vector2.Distance(p, lakeCenter) < lakeRadius - 1f, 1f, area);
        MeshObject(root, "Agua", water, mLakeWater, false, false);

        // no se puede entrar al lago
        var block = new GameObject("Bloqueo");
        block.transform.SetParent(root, false);
        block.transform.localPosition = new Vector3(lakeCenter.x, 0f, lakeCenter.y);
        var cap = block.AddComponent<CapsuleCollider>();
        cap.radius = lakeRadius - 4f; cap.height = 60f;

        // juncos en la orilla
        var batch = new List<CombineInstance>();
        var tuft = TuftMesh();
        for (int i = 0; i < 220; i++)
        {
            float a = R(0f, Mathf.PI * 2f), d = lakeRadius * R(0.72f, 0.9f);
            var p = lakeCenter + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
            if (PlateauSdf(p) > -2f) continue;
            float s = R(1.3f, 2.2f);
            batch.Add(new CombineInstance { mesh = tuft, transform = Matrix4x4.TRS(new Vector3(p.x, GroundHeight(p.x, p.y), p.y), Quaternion.Euler(0f, R(0f, 360f), 0f), new Vector3(s * 0.7f, s, s * 0.7f)) });
        }
        var reeds = new Mesh { name = "Juncos", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        reeds.CombineMeshes(batch.ToArray(), true, true);
        MeshObject(root, "Juncos", reeds, mTuft, false, false);

        // cascada: tramo del borde que toca el lago
        var arc = new List<int>();
        for (int i = 0; i < ContourPoints; i++)
            if (Vector2.Distance(contour[i], lakeCenter) < lakeRadius - 1.5f) arc.Add(i);
        if (arc.Count < 2) return;
        // ordena el tramo de forma continua (puede cruzar el índice 0)
        int start = 0;
        for (int k = 0; k < arc.Count; k++)
            if (!arc.Contains((arc[k] + ContourPoints - 1) % ContourPoints)) { start = arc[k]; break; }
        var ordered = new List<int>();
        for (int k = 0; k < arc.Count; k++) ordered.Add((start + k) % ContourPoints);

        float[] drops = { 0f, 0.4f, 1.2f, 2.5f, 4.5f, 7.5f, 12f, 18f, 27f, 40f, 58f, 82f, 115f, 150f };
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        float along = 0f;
        for (int k = 0; k < ordered.Count; k++)
        {
            int ci = ordered[k];
            if (k > 0) along += Vector2.Distance(contour[ci], contour[ordered[k - 1]]);
            Vector2 p = contour[ci], o = Outward(ci);
            float edge = Mathf.Min(1f, Mathf.Min(k, ordered.Count - 1 - k) / 4f);   // los extremos más delgados
            foreach (float d in drops)
            {
                float outward = 0.3f + 1.6f * Mathf.Sqrt(d) * 0.55f + d * 0.04f;     // el agua sale en arco y se separa de la roca
                Vector2 q = p + o * outward * Mathf.Lerp(0.6f, 1f, edge);
                v.Add(new Vector3(q.x, waterLevel + 0.02f - d, q.y));
                uv.Add(new Vector2(along, d));
            }
        }
        int rows = drops.Length;
        for (int k = 0; k < ordered.Count - 1; k++)
            for (int j = 0; j < rows - 1; j++)
            {
                int a = k * rows + j, b = a + 1, c = a + rows, d = c + 1;
                tri.AddRange(new[] { a, b, c, c, b, d });
            }
        var fall = new Mesh { name = "Waterfall", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        fall.SetVertices(v); fall.SetUVs(0, uv); fall.SetTriangles(tri, 0);
        fall.RecalculateNormals(); fall.RecalculateBounds();
        MeshObject(root, "Cascada", fall, mWaterfall, false, false);

        // bruma en el labio de la cascada y más abajo
        Vector2 mid = contour[ordered[ordered.Count / 2]], mo = Outward(ordered[ordered.Count / 2]);
        float span = Vector2.Distance(contour[ordered[0]], contour[ordered[ordered.Count - 1]]);
        foreach (var lvl in new[] { new Vector2(-1f, 0.35f), new Vector2(-25f, 1f), new Vector2(-60f, 1.8f) })
        {
            var pos = mid + mo * (2f + lvl.y * 3f);
            Particles(root, "BrumaCascada", new Vector3(pos.x, waterLevel + lvl.x, pos.y), mCloud, ps =>
            {
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
                main.startSize = new ParticleSystem.MinMaxCurve(2f * lvl.y, 5f * lvl.y);
                main.startColor = new Color(0.9f, 0.95f, 1f, 0.22f);
                main.maxParticles = 200;
                main.prewarm = true;
                main.gravityModifier = 0.05f;
                var em = ps.emission; em.rateOverTime = 30f;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(span, 2f, 3f);
            }).transform.rotation = Quaternion.LookRotation(new Vector3(mo.x, 0f, mo.y));
        }
        AddLight(root, "LuzLago", new Vector3(lakeCenter.x - 6f, 3f, lakeCenter.y), new Color(0.5f, 0.75f, 1f), 18f, 5f, LightShadows.None);
    }

    // ---------------- abismo: mar de nubes e islas flotantes

    void BuildAbyss()
    {
        var root = Group("Abismo");
        foreach (var layer in new[] { new Vector3(-55f, 1f, 0.32f), new Vector3(-110f, 1.6f, 0.55f) })
        {
            Particles(root, "MarDeNubes", new Vector3(plateauCenter.x, layer.x, plateauCenter.y), mCloud, ps =>
            {
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(60f, 90f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
                main.startSize = new ParticleSystem.MinMaxCurve(40f * layer.y, 90f * layer.y);
                main.startColor = new Color(0.85f, 0.75f, 0.85f, layer.z);
                main.maxParticles = 260;
                main.prewarm = true;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                var em = ps.emission; em.rateOverTime = 4f;
                var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(900f, 12f, 900f);
            });
        }

        // islas flotantes: las primeras más cerca (se ven bien desde la meseta y en la cinemática), el resto a lo lejos
        for (int i = 0, tries = 0; i < floatingIslands && tries < 400; tries++)
        {
            bool near = i < floatingIslands / 3;
            float a = R(0f, Mathf.PI * 2f), d = near ? R(105f, 160f) : R(170f, 380f);
            var p2 = plateauCenter + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * d;
            float size = near ? R(7f, 14f) : R(12f, 28f);
            if (Vector2.Distance(p2, towerCenter) < 70f + size) continue;             // no tapar la torre
            bool crowded = false;
            foreach (Transform o in root)
                if (o.name.StartsWith("Isla_") && Vector2.Distance(p2, new Vector2(o.localPosition.x, o.localPosition.z)) < size * 2.6f + 30f) crowded = true;
            if (crowded) continue;
            FloatingIsland(root, i, new Vector3(p2.x, near ? R(-22f, 12f) : R(-45f, 35f), p2.y), size, near && size >= 9f);
            i++;
        }
    }

    /// <summary>
    /// Isla flotante: tapa de pasto con borde irregular (un poco abombada) y debajo roca que se afila en punta.
    /// La tapa y la roca comparten exactamente el mismo contorno a y = 0, y los árboles se apoyan en la altura real
    /// de la tapa: nada queda enterrado ni flotando.
    /// </summary>
    void FloatingIsland(Transform root, int index, Vector3 pos, float size, bool withHut = false)
    {
        var isl = Group("Isla_" + index, root);
        isl.localPosition = pos;
        isl.localRotation = Quaternion.Euler(R(-3f, 3f), R(0f, 360f), R(-3f, 3f));
        var bob = isl.gameObject.AddComponent<FloatingBob>();
        bob.amplitude = R(0.4f, 1.1f); bob.period = R(7f, 12f); bob.phase = R(0f, 10f);

        const int segs = 20;
        float p1 = R(0f, 6.3f), p2 = R(0f, 6.3f), p3 = R(0f, 6.3f);
        var outline = new float[segs + 1];
        for (int s = 0; s <= segs; s++)
        {
            float ang = s / (float)segs * Mathf.PI * 2f;
            outline[s] = size * (1f + 0.13f * Mathf.Sin(2f * ang + p1) + 0.08f * Mathf.Sin(3f * ang + p2) + 0.05f * Mathf.Sin(5f * ang + p3));
        }
        outline[segs] = outline[0];
        float dome = size * 0.06f;
        float TopHeight(float ang, float r)
        {
            float edge = outline[Mathf.Clamp(Mathf.RoundToInt((ang / (Mathf.PI * 2f) % 1f + 1f) % 1f * segs), 0, segs)];
            float k = Mathf.Clamp01(r / edge);
            return dome * (1f - k * k);
        }

        // roca: anillos que bajan y se estrechan, con la punta desviada; arriba coincide con el borde de la tapa
        float depth = size * R(1.4f, 2.2f);
        float[] ts = { 0f, 0.05f, 0.14f, 0.27f, 0.42f, 0.58f, 0.74f, 0.88f, 1f };
        Vector2 drift = new Vector2(R(-1f, 1f), R(-1f, 1f)) * size * 0.25f;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        int rows = ts.Length;
        for (int s = 0; s <= segs; s++)
        {
            float ang = s / (float)segs * Mathf.PI * 2f;
            var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            for (int j = 0; j < rows; j++)
            {
                float t = ts[j];
                float scale = j == 1 ? 1.04f : Mathf.Lerp(1f, 0.04f, Mathf.Pow(t, 0.75f));   // pequeño labio bajo el pasto
                float jitter = j == 0 ? 0f : (Hash(s % segs + index * 31, j) - 0.5f) * 0.22f * size * (1f - t * 0.6f);
                Vector3 center = new Vector3(drift.x, 0f, drift.y) * t * t;
                v.Add(center + dir * (outline[s] * scale + jitter) + Vector3.down * (t * depth + (j == 1 ? 0.3f : 0f)));
                uv.Add(new Vector2(s / (float)segs * size * 6.28f / 6f, -t * depth / 6f));
            }
        }
        for (int s = 0; s < segs; s++)
            for (int j = 0; j < rows - 1; j++)
            {
                int a = s * rows + j, b = a + 1, c = a + rows, d = c + 1;
                tri.AddRange(new[] { a, c, b, c, d, b });
            }
        var rock = new Mesh { name = "IslaRoca" };
        rock.SetVertices(v); rock.SetUVs(0, uv); rock.SetTriangles(tri, 0);
        rock.RecalculateNormals(); rock.RecalculateBounds();
        MeshObject(isl, "Roca", rock, mCliff, true, true);

        // tapa de pasto: abanico abombado con el mismo contorno
        var gv = new List<Vector3> { new Vector3(0f, dome, 0f) };
        var guv = new List<Vector2> { Vector2.zero };
        var gt = new List<int>();
        foreach (float ring in new[] { 0.5f, 1f })
            for (int s = 0; s < segs; s++)
            {
                float ang = s / (float)segs * Mathf.PI * 2f;
                float r = outline[s] * ring;
                var q = new Vector3(Mathf.Cos(ang) * r, TopHeight(ang, r), Mathf.Sin(ang) * r);
                gv.Add(q); guv.Add(new Vector2(q.x, q.z) / 4f);
            }
        for (int s = 0; s < segs; s++)
        {
            int s1 = (s + 1) % segs;
            gt.AddRange(new[] { 0, 1 + s1, 1 + s });
            int a0 = 1, b0 = 1 + segs;
            gt.AddRange(new[] { a0 + s, a0 + s1, b0 + s1, a0 + s, b0 + s1, b0 + s });
        }
        var cap = new Mesh { name = "IslaPasto" };
        cap.SetVertices(gv); cap.SetUVs(0, guv); cap.SetTriangles(gt, 0);
        cap.RecalculateNormals(); cap.RecalculateBounds();
        MeshObject(isl, "Pasto", cap, mGrass, true, false);

        // árboles sobre la tapa (lejos del borde), sin encimarse
        int n = Mathf.Clamp(Mathf.RoundToInt(size * size / 45f), 2, 14);
        var placedTrees = new List<Vector2>();
        for (int k = 0, tries = 0; k < n && tries < 80; tries++)
        {
            float ang = R(0f, Mathf.PI * 2f);
            float edge = outline[Mathf.RoundToInt(ang / (Mathf.PI * 2f) * segs) % segs];
            float r = Mathf.Sqrt(R(0f, 1f)) * edge * 0.62f;
            var tp = new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
            bool tooClose = false;
            foreach (var o in placedTrees) if (Vector2.Distance(o, tp) < 2.8f) tooClose = true;
            if (withHut && tp.magnitude < 3.6f) tooClose = true;   // espacio para la casita
            if (tooClose) continue;
            placedTrees.Add(tp);
            Tree(isl, tp.x, tp.y, TopHeight(ang, r) + 0.1f, R(1.3f, 2.3f), false);
            k++;
        }

        if (withHut) Hut(isl, new Vector3(0f, dome - 0.05f, 0f), R(0f, 360f), 0.85f);
        // cristales que brillan en algunas islas y rocas sueltas flotando debajo
        if (!withHut && R(0f, 1f) < 0.5f)
            for (int c = 0; c < 3; c++)
            {
                float ang = R(0f, Mathf.PI * 2f), r = R(0.2f, 0.5f) * size;
                var cr = Primitive(isl, PrimitiveType.Cube, "Cristal", new Vector3(Mathf.Cos(ang) * r, TopHeight(ang, r) + 0.6f, Mathf.Sin(ang) * r), new Vector3(0.5f, R(1.4f, 2.6f), 0.5f), mRune);
                cr.transform.localRotation = Quaternion.Euler(R(-20f, 20f), R(0f, 90f), R(-20f, 20f));
                Object.DestroyImmediate(cr.GetComponent<Collider>());
            }
        int debris = rng.Next(1, 4);
        for (int k = 0; k < debris; k++)
        {
            var piece = Group("Roca_" + k, isl);
            float ang = R(0f, Mathf.PI * 2f);
            piece.localPosition = new Vector3(Mathf.Cos(ang) * size * R(0.9f, 1.4f), -depth * R(0.35f, 0.8f), Mathf.Sin(ang) * size * R(0.9f, 1.4f));
            float ps = size * R(0.08f, 0.16f);
            var pm = new List<Vector3>(); var pt = new List<int>();
            // rocalla: octaedro deformado
            pm.Add(new Vector3(0f, ps * R(0.4f, 0.7f), 0f)); pm.Add(new Vector3(0f, -ps * R(1.2f, 1.8f), 0f));
            for (int s = 0; s < 5; s++) { float pa = s / 5f * Mathf.PI * 2f; pm.Add(new Vector3(Mathf.Cos(pa), 0f, Mathf.Sin(pa)) * ps * R(0.8f, 1.2f)); }
            for (int s = 0; s < 5; s++)
            {
                int s0 = 2 + s, s1 = 2 + (s + 1) % 5;
                pt.AddRange(new[] { 0, s1, s0, 1, s0, s1 });
            }
            var pmsh = new Mesh { name = "Rocalla" };
            pmsh.SetVertices(pm); pmsh.SetTriangles(pt, 0); pmsh.RecalculateNormals(); pmsh.RecalculateBounds();
            MeshObject(piece, "Roca", pmsh, mCliff, true, false);
            var pb = piece.gameObject.AddComponent<FloatingBob>();
            pb.amplitude = R(0.6f, 1.5f); pb.period = R(4f, 8f); pb.phase = R(0f, 10f); pb.spin = R(-6f, 6f);
        }
    }

    // ---------------- vegetación, rocas, pasto

    // ---------------- caserío: casitas junto al camino sur y alrededor de la plaza

    readonly List<Vector2> huts = new List<Vector2>();

    void BuildHuts()
    {
        huts.Clear();
        var root = Group("Caserio");
        // (x, z): a los lados del camino sur y en las orillas de la plaza; la puerta mira hacia el camino
        Vector2[] spots =
        {
            new Vector2(-9f, -19f), new Vector2(9.5f, -24f), new Vector2(-9.5f, -31f), new Vector2(10f, -35f),
            new Vector2(-21f, -6f), new Vector2(-24f, 6f), new Vector2(20f, -12f), new Vector2(-18f, 22f), new Vector2(17f, 24f),
        };
        foreach (var s in spots)
        {
            if (!Free(s, 2.5f)) continue;
            Vector2 toPath = s.y < -plazaRadius ? new Vector2(-Mathf.Sign(s.x), 0f) : -s.normalized;   // al camino o a la fuente
            float yaw = Mathf.Atan2(toPath.x, toPath.y) * Mathf.Rad2Deg + R(-12f, 12f);
            Hut(root, new Vector3(s.x, GroundHeight(s.x, s.y), s.y), yaw, R(0.95f, 1.2f));
            huts.Add(s);
        }
    }

    /// <summary>Casita de madera sobre base de piedra, techo de dos aguas, chimenea con humo, ventanas encendidas y farol en la puerta.</summary>
    void Hut(Transform parent, Vector3 pos, float yaw, float scale)
    {
        var h = Group("Casita", parent);
        h.localPosition = pos;
        h.localRotation = Quaternion.Euler(0f, yaw, 0f);
        h.localScale = Vector3.one * scale;
        float w = 3.2f, d = 2.8f, wallH = 2.1f;

        var b = Primitive(h, PrimitiveType.Cube, "Base", new Vector3(0f, 0.15f, 0f), new Vector3(w + 0.4f, 0.5f, d + 0.4f), mStone);
        b.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        var walls = Primitive(h, PrimitiveType.Cube, "Paredes", new Vector3(0f, 0.4f + wallH / 2f, 0f), new Vector3(w, wallH, d), mPlank);
        Object.DestroyImmediate(walls.GetComponent<Collider>());
        // vigas en las esquinas
        foreach (var c in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
            Object.DestroyImmediate(Primitive(h, PrimitiveType.Cube, "Viga", new Vector3(c.x * w / 2f, 0.4f + wallH / 2f, c.y * d / 2f), new Vector3(0.22f, wallH, 0.22f), mBark).GetComponent<Collider>());

        // techo de dos aguas (con alero)
        var roof = MeshObject(h, "Techo", Roof(w + 0.8f, d + 0.9f, 1.5f), mThatch, true, true);
        roof.transform.localPosition = new Vector3(0f, 0.4f + wallH, 0f);
        var gableMat = mPlank;
        MeshObject(h, "Hastial", Gable(w, 1.4f, d / 2f + 0.01f), gableMat, true, true).transform.localPosition = new Vector3(0f, 0.4f + wallH, 0f);

        // puerta (frente = +Z), ventanas, farol
        Object.DestroyImmediate(Primitive(h, PrimitiveType.Cube, "Puerta", new Vector3(0f, 0.4f + 0.75f, d / 2f + 0.03f), new Vector3(0.85f, 1.5f, 0.08f), mDoor).GetComponent<Collider>());
        foreach (var wp in new[] { new Vector3(-1f, 1.55f, d / 2f + 0.03f), new Vector3(1f, 1.55f, d / 2f + 0.03f) })
        {
            var win = MeshObject(h, "Ventana", Quad(0.55f, 0.55f, 10f, true), R(0f, 1f) < 0.8f ? mWindowLit : mWindowDark, false, false);
            win.transform.localPosition = wp + Vector3.forward * 0.01f;
            win.transform.localRotation = Quaternion.LookRotation(Vector3.back);
        }
        var side = MeshObject(h, "VentanaLado", Quad(0.55f, 0.55f, 10f, true), mWindowLit, false, false);
        side.transform.localPosition = new Vector3(w / 2f + 0.03f, 1.55f, 0f);
        side.transform.localRotation = Quaternion.LookRotation(Vector3.left);
        var lantern = Primitive(h, PrimitiveType.Cube, "Farol", new Vector3(0.75f, 2f, d / 2f + 0.25f), new Vector3(0.2f, 0.28f, 0.2f), mLampGlow);
        Object.DestroyImmediate(lantern.GetComponent<Collider>());
        lantern.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var l = AddLight(h, "LuzPuerta", new Vector3(0.75f, 2f, d / 2f + 0.7f), new Color(1f, 0.7f, 0.4f), 6f, 5f, LightShadows.None);
        l.gameObject.AddComponent<LightFlicker>().amount = 0.12f;

        // chimenea con humo
        var chim = Primitive(h, PrimitiveType.Cube, "Chimenea", new Vector3(w * 0.28f, 0.4f + wallH + 1.1f, -d * 0.2f), new Vector3(0.45f, 1.6f, 0.45f), mStone);
        Object.DestroyImmediate(chim.GetComponent<Collider>());
        Particles(h, "Humo", new Vector3(w * 0.28f, 0.4f + wallH + 2f, -d * 0.2f), mCloud, ps =>
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startColor = new Color(0.75f, 0.7f, 0.75f, 0.28f);
            main.maxParticles = 40;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 4f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = 0.1f; sh.rotation = new Vector3(-90f, 0f, 0f);
            var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 2f));
        });

        // barril y leña junto a la casa
        var barrel = Primitive(h, PrimitiveType.Cylinder, "Barril", new Vector3(-w / 2f - 0.5f, 0.45f, d / 2f - 0.3f), new Vector3(0.6f, 0.45f, 0.6f), mDoor);
        barrel.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        for (int k = 0; k < 3; k++)
        {
            var log = Primitive(h, PrimitiveType.Cylinder, "Leña", new Vector3(-w / 2f - 0.45f, 0.15f + k * 0.22f, -0.5f + (k % 2) * 0.1f), new Vector3(0.22f, 0.6f, 0.22f), mBark);
            log.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Object.DestroyImmediate(log.GetComponent<Collider>());
        }

        // collider sólido de la casa (paredes + base)
        var col = h.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 1.5f, 0f);
        col.size = new Vector3(w + 0.4f, 3f, d + 0.4f);
    }

    /// <summary>Techo de dos aguas: dos planos inclinados con la cumbrera a lo largo de X. Base en y = 0.</summary>
    static Mesh Roof(float w, float d, float h)
    {
        float hx = w / 2f, hz = d / 2f;
        var v = new List<Vector3>
        {
            // agua frontal (+Z)
            new Vector3(-hx, 0f, hz), new Vector3(hx, 0f, hz), new Vector3(-hx, h, 0f), new Vector3(hx, h, 0f),
            // agua trasera (-Z)
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

    bool Free(Vector2 p, float margin)
    {
        if (PlateauSdf(p) > -2f - margin) return false;
        foreach (var h in huts) if (Vector2.Distance(p, h) < 4.2f + margin) return false;          // casitas
        if (p.magnitude < plazaRadius + 2f + margin) return false;
        if (Mathf.Abs(p.x) < 3.5f + margin && p.y > -35f && p.y < StairsEndZ + 1f) return false;     // caminos y escalinata
        if (Vector2.Distance(p, lakeCenter) < lakeRadius - 3f + margin) return false;
        float dt = DistTower(p);
        if (dt < 19f + margin) return false;                                                           // base de la torre
        if (dt > mesaRadius - 2f - margin && dt < mesaRadius + 2.5f + margin) return false;            // pared del risco
        return true;
    }

    void BuildTrees()
    {
        var root = Group("Arboles");
        for (int placed = 0, tries = 0; placed < trees && tries < 8000; tries++)
        {
            var b = ContourBounds(0f);
            var p = new Vector2(R(b.xMin, b.xMax), R(b.yMin, b.yMax));
            if (!Free(p, 2f) || p.magnitude < 18f) continue;
            Tree(root, p.x, p.y, GroundHeight(p.x, p.y), R(0.85f, 1.3f), true);
            placed++;
        }
    }

    Mesh trunkMesh, sphereMesh;

    void Tree(Transform parent, float x, float z, float y, float s, bool collider)
    {
        if (trunkMesh == null) trunkMesh = Cylinder(0.24f, 0.15f, 2.8f, 7, 1f, false);
        if (sphereMesh == null) sphereMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
        var t = new GameObject("Arbol").transform;
        t.SetParent(parent, false);
        t.localPosition = new Vector3(x, y - 0.1f, z);
        t.localRotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
        t.localScale = Vector3.one * s;
        MeshObject(t, "Tronco", trunkMesh, mBark, true, false);
        var leaf = mLeaves[rng.Next(mLeaves.Length)];
        int blobs = rng.Next(2, 4);
        for (int b = 0; b < blobs; b++)
        {
            var go = MeshObject(t, "Copa", sphereMesh, leaf, true, false);
            go.transform.localPosition = new Vector3(R(-0.5f, 0.5f), 2.6f + b * 0.8f + R(0f, 0.3f), R(-0.5f, 0.5f));
            float w = R(2f, 2.6f) - b * 0.4f;
            go.transform.localScale = new Vector3(w, w * R(0.7f, 0.9f), w);
        }
        if (collider)
        {
            var cap = t.gameObject.AddComponent<CapsuleCollider>();
            cap.radius = 0.35f; cap.height = 3f; cap.center = new Vector3(0f, 1.5f, 0f);
        }
    }

    void BuildRocks()
    {
        var root = Group("Rocas");
        if (sphereMesh == null) sphereMesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
        var b = ContourBounds(0f);
        for (int i = 0, tries = 0; i < rocks && tries < 4000; tries++)
        {
            var p = new Vector2(R(b.xMin, b.xMax), R(b.yMin, b.yMax));
            if (!Free(p, 0.5f)) continue;
            var go = MeshObject(root, "Roca", sphereMesh, mRock, true, false);
            float s = R(0.4f, 1.4f);
            go.transform.localPosition = new Vector3(p.x, GroundHeight(p.x, p.y) + s * 0.15f, p.y);
            go.transform.localRotation = Quaternion.Euler(R(0f, 30f), R(0f, 360f), R(0f, 30f));
            go.transform.localScale = new Vector3(s * R(1f, 1.6f), s * R(0.5f, 0.9f), s * R(1f, 1.4f));
            i++;
        }
    }

    Mesh tuftMeshCache;
    Mesh TuftMesh()
    {
        if (tuftMeshCache != null) return tuftMeshCache;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        for (int q = 0; q < 2; q++)
        {
            Quaternion rot = Quaternion.Euler(0f, q * 90f + 45f, 0f);
            int b = v.Count;
            v.Add(rot * new Vector3(-0.3f, 0f, 0f)); v.Add(rot * new Vector3(0.3f, 0f, 0f));
            v.Add(rot * new Vector3(-0.3f, 0.5f, 0f)); v.Add(rot * new Vector3(0.3f, 0.5f, 0f));
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(1, 0)); uv.Add(new Vector2(0, 1)); uv.Add(new Vector2(1, 1));
            tri.AddRange(new[] { b, b + 2, b + 3, b, b + 3, b + 1 });
        }
        var m = new Mesh { name = "Tuft" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        var n = new Vector3[v.Count]; for (int i = 0; i < n.Length; i++) n[i] = Vector3.up;   // se ilumina como el suelo
        m.normals = n; m.RecalculateBounds();
        return tuftMeshCache = m;
    }

    void BuildGrass()
    {
        var root = Group("Pasto");
        var tuft = TuftMesh();
        var b = ContourBounds(0f);
        var batch = new List<CombineInstance>();
        int chunk = 0;
        for (int i = 0, tries = 0; i < grassTufts && tries < grassTufts * 8; tries++)
        {
            var p = new Vector2(R(b.xMin, b.xMax), R(b.yMin, b.yMax));
            if (!Free(p, -1.5f)) continue;
            float s = R(0.7f, 1.4f);
            batch.Add(new CombineInstance { mesh = tuft, transform = Matrix4x4.TRS(new Vector3(p.x, GroundHeight(p.x, p.y), p.y), Quaternion.Euler(0f, R(0f, 360f), 0f), Vector3.one * s) });
            i++;
            if (batch.Count == 1500) { FlushGrass(root, batch, chunk++); batch.Clear(); }
        }
        if (batch.Count > 0) FlushGrass(root, batch, chunk);
    }

    void FlushGrass(Transform root, List<CombineInstance> batch, int index)
    {
        var m = new Mesh { name = "Pasto_" + index, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.CombineMeshes(batch.ToArray(), true, true);
        MeshObject(root, "Pasto_" + index, m, mTuft, false, false);
    }

    // ---------------- luces

    void BuildLamps()
    {
        var root = Group("Faroles");
        for (int i = 0; i < 6; i++)
        {
            float a = i / 6f * Mathf.PI * 2f;   // 0°, 60°, 120°…: ninguno queda sobre los caminos norte/sur
            Lamp(root, new Vector3(Mathf.Cos(a) * (plazaRadius - 0.8f), 0f, Mathf.Sin(a) * (plazaRadius - 0.8f)), true);
        }
        for (float z = plazaRadius + 6f; z < StairsStartZ - 1f; z += 9f)
            foreach (float x in new[] { -3.2f, 3.2f }) Lamp(root, new Vector3(x, 0f, z), false);
        foreach (float x in new[] { -4f, 4f }) Lamp(root, new Vector3(x, 0f, StairsStartZ - 0.5f), true);           // pie de la escalinata
        foreach (float x in new[] { -4f, 4f }) Lamp(root, new Vector3(x, mesaHeight, StairsEndZ + 1.5f), true);      // arriba
        foreach (float z in new[] { -plazaRadius - 6f, -plazaRadius - 16f })
            foreach (float x in new[] { -3.2f, 3.2f }) Lamp(root, new Vector3(x, 0f, z), false);
    }

    void Lamp(Transform parent, Vector3 pos, bool shadows)
    {
        var t = Group("Farol", parent);
        t.localPosition = pos;
        var post = MeshObject(t, "Poste", Cylinder(0.08f, 0.06f, 3.2f, 8, 1f, true), mMetal, true, false);
        post.AddComponent<CapsuleCollider>().radius = 0.15f;
        Primitive(t, PrimitiveType.Cube, "Base", new Vector3(0f, 0.15f, 0f), new Vector3(0.35f, 0.3f, 0.35f), mMetal);
        Primitive(t, PrimitiveType.Cube, "Tapa", new Vector3(0f, 3.62f, 0f), new Vector3(0.5f, 0.08f, 0.5f), mMetal);
        var glow = Primitive(t, PrimitiveType.Cube, "Vidrio", new Vector3(0f, 3.35f, 0f), new Vector3(0.32f, 0.45f, 0.32f), mLampGlow);
        glow.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var l = AddLight(t, "Luz", new Vector3(0f, 3.1f, 0f), new Color(1f, 0.72f, 0.42f), 11f, 14f, shadows ? LightShadows.Soft : LightShadows.None);
        l.gameObject.AddComponent<LightFlicker>().amount = 0.06f;
    }

    void BuildFireflies()
    {
        var root = Group("Luciernagas");
        Particles(root, "Luciernagas", new Vector3(plateauCenter.x, 1.2f, plateauCenter.y - 5f), mFirefly, ps =>
        {
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSpeed = 0.1f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
            main.startColor = new Color(1f, 0.95f, 0.5f, 1f);
            main.maxParticles = 160;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.prewarm = true;
            var em = ps.emission; em.rateOverTime = 25f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(80f, 2f, 80f);
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.6f; noise.frequency = 0.3f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(0.2f, 0.5f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        });
    }

    void BuildBoundary()
    {
        // muros invisibles siguiendo el borde (un poco hacia adentro): nadie cae al abismo
        var root = Group("Limites");
        const int step = 3;
        for (int i = 0; i < ContourPoints; i += step)
        {
            int j = (i + step) % ContourPoints;
            Vector2 a = contour[i] - Outward(i) * 1f, b = contour[j] - Outward(j) * 1f;
            Vector2 mid = (a + b) / 2f;
            float y = GroundHeight(mid.x, mid.y);
            var go = new GameObject("Muro_" + i);
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(mid.x, y + 3f, mid.y);
            Vector2 dir = b - a;
            go.transform.localRotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.y));
            go.AddComponent<BoxCollider>().size = new Vector3(1f, 8f, dir.magnitude + 0.8f);
        }
    }

    // ------------------------------------------------------------------ mallas

    static Mesh Disc(float radius, int segs, int rings, float tile, float y)
    {
        var v = new List<Vector3> { new Vector3(0f, y, 0f) };
        var uv = new List<Vector2> { Vector2.zero };
        var tri = new List<int>();
        for (int r = 1; r <= rings; r++)
        {
            float rr = radius * r / rings;
            for (int s = 0; s < segs; s++)
            {
                float a = s / (float)segs * Mathf.PI * 2f;
                var p = new Vector3(Mathf.Cos(a) * rr, y, Mathf.Sin(a) * rr);
                v.Add(p); uv.Add(new Vector2(p.x, p.z) / tile);
            }
        }
        for (int s = 0; s < segs; s++) tri.AddRange(new[] { 0, 1 + (s + 1) % segs, 1 + s });
        for (int r = 1; r < rings; r++)
        {
            int a0 = 1 + (r - 1) * segs, b0 = 1 + r * segs;
            for (int s = 0; s < segs; s++)
            {
                int s1 = (s + 1) % segs;
                tri.AddRange(new[] { a0 + s, a0 + s1, b0 + s1, a0 + s, b0 + s1, b0 + s });
            }
        }
        var m = new Mesh { name = "Disc", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>Cilindro/cono con base en y = 0. UVs en metros / tile.</summary>
    static Mesh Cylinder(float rBottom, float rTop, float h, int segs, float tile, bool capTop)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        float circ = Mathf.PI * (rBottom + rTop);
        for (int s = 0; s <= segs; s++)
        {
            float a = s / (float)segs * Mathf.PI * 2f;
            var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            v.Add(d * rBottom); v.Add(d * rTop + Vector3.up * h);
            float u = s / (float)segs * circ / tile;
            uv.Add(new Vector2(u, 0f)); uv.Add(new Vector2(u, h / tile));
        }
        for (int s = 0; s < segs; s++)
        {
            int i = s * 2;
            tri.AddRange(new[] { i, i + 1, i + 3, i, i + 3, i + 2 });
        }
        if (capTop && rTop > 0.001f)
        {
            int c = v.Count; v.Add(Vector3.up * h); uv.Add(Vector2.zero);
            int start = v.Count;
            for (int s = 0; s <= segs; s++)
            {
                float a = s / (float)segs * Mathf.PI * 2f;
                var p = new Vector3(Mathf.Sin(a) * rTop, h, Mathf.Cos(a) * rTop);
                v.Add(p); uv.Add(new Vector2(p.x, p.z) / tile);
            }
            for (int s = 0; s < segs; s++) tri.AddRange(new[] { c, start + s, start + s + 1 });
        }
        var m = new Mesh { name = "Cylinder" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>Anillo grueso (muro de pileta): cara exterior, interior y borde superior.</summary>
    static Mesh Ring(float rOut, float rIn, float h, int segs, float tile)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        void Band(float r0, float y0, float r1, float y1, bool flip)
        {
            int b = v.Count;
            for (int s = 0; s <= segs; s++)
            {
                float a = s / (float)segs * Mathf.PI * 2f;
                var d = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                v.Add(d * r0 + Vector3.up * y0); v.Add(d * r1 + Vector3.up * y1);
                float u = s / (float)segs * Mathf.PI * 2f * Mathf.Max(r0, r1) / tile;
                uv.Add(new Vector2(u, 0f)); uv.Add(new Vector2(u, (Mathf.Abs(y1 - y0) + Mathf.Abs(r1 - r0)) / tile));
            }
            for (int s = 0; s < segs; s++)
            {
                int i = b + s * 2;
                if (!flip) tri.AddRange(new[] { i, i + 1, i + 3, i, i + 3, i + 2 });
                else tri.AddRange(new[] { i, i + 3, i + 1, i, i + 2, i + 3 });
            }
        }
        Band(rOut, 0f, rOut, h, false);
        Band(rIn, 0f, rIn, h, true);
        Band(rOut, h, rIn, h, true);   // tapa: mira hacia arriba (antes quedaba al revés: invisible y sin colisión por encima)
        var m = new Mesh { name = "Ring" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>Quad. Horizontal (en XZ, mirando arriba) o vertical (en XY, visible desde -Z).</summary>
    static Mesh Quad(float w, float l, float tile, bool vertical = false)
    {
        var m = new Mesh { name = "Quad" };
        m.vertices = vertical
            ? new[] { new Vector3(-w / 2, -l / 2, 0f), new Vector3(w / 2, -l / 2, 0f), new Vector3(-w / 2, l / 2, 0f), new Vector3(w / 2, l / 2, 0f) }
            : new[] { new Vector3(-w / 2, 0f, -l / 2), new Vector3(w / 2, 0f, -l / 2), new Vector3(-w / 2, 0f, l / 2), new Vector3(w / 2, 0f, l / 2) };
        m.uv = new[] { new Vector2(0, 0), new Vector2(w / tile, 0), new Vector2(0, l / tile), new Vector2(w / tile, l / tile) };
        m.triangles = new[] { 0, 2, 3, 0, 3, 1 };
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // ------------------------------------------------------------------ objetos

    Transform Group(string name, Transform parent = null)
    {
        var go = new GameObject(parent == null ? Prefix + name : name);
        go.transform.SetParent(parent == null ? transform : parent, false);
        return go.transform;
    }

    static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material mat, bool castShadows, bool twoSidedShadows)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = !castShadows ? UnityEngine.Rendering.ShadowCastingMode.Off
            : twoSidedShadows ? UnityEngine.Rendering.ShadowCastingMode.TwoSided : UnityEngine.Rendering.ShadowCastingMode.On;
        return go;
    }

    static GameObject Primitive(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static Light AddLight(Transform parent, string name, Vector3 pos, Color color, float range, float intensity, LightShadows shadows)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point; l.color = color; l.range = range; l.intensity = intensity; l.shadows = shadows;
        return l;
    }

    static ParticleSystem Particles(Transform parent, string name, Vector3 pos, Material mat, System.Action<ParticleSystem> setup)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.playOnAwake = true; main.loop = true;
        setup(ps);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ps.Play();
        return ps;
    }

    // ------------------------------------------------------------------ texturas y materiales (pixel art procedural)

    void CreateAssets()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials/Exterior")) AssetDatabase.CreateFolder("Assets/Materials", "Exterior");
        if (!AssetDatabase.IsValidFolder("Assets/Textures/Exterior")) AssetDatabase.CreateFolder("Assets/Textures", "Exterior");

        var grass = Tex("Grass", 64, (x, y) =>
        {
            float n = Mathf.PerlinNoise(x * 0.11f, y * 0.11f) * 0.6f + Hash(x, y) * 0.4f;
            Color[] pal = { new Color(0.22f, 0.36f, 0.17f), new Color(0.28f, 0.45f, 0.2f), new Color(0.34f, 0.52f, 0.23f), new Color(0.42f, 0.58f, 0.27f) };
            return pal[Mathf.Clamp((int)(n * 4f), 0, 3)];
        });
        var stone = Tex("Stone", 64, (x, y) =>
        {
            int row = y / 16; int ox = (row % 2) * 8; int cx = (x + ox) % 16, cy = y % 16;
            if (cx == 0 || cy == 0) return new Color(0.2f, 0.19f, 0.2f);
            float v = 0.55f + Hash((x + ox) / 16 + row * 7, row) * 0.15f + (Hash(x, y) - 0.5f) * 0.08f;
            if (cx == 1 || cy == 1) v += 0.08f;
            return new Color(v * 0.95f, v * 0.92f, v * 0.9f);
        });
        var brick = Tex("TowerBrick", 64, (x, y) =>
        {
            int row = y / 12; int ox = (row % 2) * 16; int cx = (x + ox) % 32, cy = y % 12;
            if (cx == 0 || cy == 0) return new Color(0.16f, 0.14f, 0.2f);
            float v = 0.42f + Hash((x + ox) / 32 + row * 5, row) * 0.12f + (Hash(x, y) - 0.5f) * 0.06f;
            if (cy == 1) v += 0.06f;
            return new Color(v * 0.88f, v * 0.86f, v * 1.02f);
        });
        var cliff = Tex("Cliff", 64, (x, y) =>
        {
            // estratos horizontales de roca con grietas
            float strata = Mathf.PerlinNoise(x * 0.04f, y * 0.35f);
            float crack = Mathf.Abs(Mathf.PerlinNoise(x * 0.15f + 4f, y * 0.05f) - 0.5f) < 0.03f ? -0.15f : 0f;
            float v = 0.3f + strata * 0.22f + (Hash(x, y) - 0.5f) * 0.06f + crack;
            if (y % 11 == 0) v -= 0.07f;
            return new Color(v * 1.0f, v * 0.9f, v * 0.86f);
        });
        var bark = Tex("Bark", 32, (x, y) =>
        {
            float v = 0.3f + Mathf.PerlinNoise(x * 0.5f, y * 0.06f) * 0.15f + (Hash(x, y) - 0.5f) * 0.05f;
            return new Color(v * 1.1f, v * 0.8f, v * 0.55f);
        });
        var leaves = Tex("Leaves", 32, (x, y) =>
        {
            float n = Hash(x, y) * 0.5f + Mathf.PerlinNoise(x * 0.3f, y * 0.3f) * 0.5f;
            Color[] pal = { new Color(0.12f, 0.28f, 0.14f), new Color(0.18f, 0.38f, 0.17f), new Color(0.27f, 0.48f, 0.2f) };
            return pal[Mathf.Clamp((int)(n * 3f), 0, 2)];
        });
        var tuft = Tex("GrassTuft", 32, (x, y) =>
        {
            for (int b = 0; b < 7; b++)
            {
                float bx = 3f + b * 4.3f + (Hash(b, 3) - 0.5f) * 2f;
                float top = 14f + Hash(b, 9) * 17f;
                float lean = (Hash(b, 5) - 0.5f) * 0.35f;
                float cx = bx + lean * y;
                float width = Mathf.Lerp(1.6f, 0.4f, y / top);
                if (y < top && Mathf.Abs(x - cx) < width)
                {
                    float v = 0.3f + y / 32f * 0.35f;
                    return new Color(v * 0.8f, v * 1.45f, v * 0.6f, 1f);
                }
            }
            return new Color(0f, 0f, 0f, 0f);
        });
        var dot = Tex("SoftDot", 32, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(16f, 16f)) / 16f;
            float a = Mathf.Clamp01(1f - d); a *= a;
            return new Color(1f, 1f, 1f, a);
        }, FilterMode.Bilinear, TextureWrapMode.Clamp);
        var cloud = Tex("CloudPuff", 64, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(32f, 32f)) / 32f;
            float n = Mathf.PerlinNoise(x * 0.09f, y * 0.09f) * 0.7f + Mathf.PerlinNoise(x * 0.2f + 5f, y * 0.2f) * 0.3f;
            float a = Mathf.Clamp01((1f - d) * 1.6f - 0.2f) * Mathf.Clamp01(n * 1.6f - 0.25f);
            return new Color(1f, 1f, 1f, a);
        }, FilterMode.Bilinear, TextureWrapMode.Clamp);

        mGrass = Lit("Grass", grass, Color.white, 0.05f);
        mStone = Lit("Stone", stone, Color.white, 0.15f);
        mStoneDark = Lit("StoneDark", stone, new Color(0.25f, 0.42f, 0.5f), 0.3f);
        mBrick = Lit("TowerBrick", brick, Color.white, 0.1f);
        mCliff = Lit("Cliff", cliff, Color.white, 0.05f);
        mBark = Lit("Bark", bark, Color.white, 0.05f);
        mRock = Lit("Rock", stone, new Color(0.7f, 0.7f, 0.75f), 0.1f);
        mMetal = Lit("Metal", null, new Color(0.16f, 0.15f, 0.2f), 0.55f, 0.7f);
        mThatch = Lit("Thatch", bark, new Color(0.95f, 0.6f, 0.35f), 0.02f);
        mPlank = Lit("Plank", bark, new Color(1.25f, 1.05f, 0.85f), 0.05f);
        mDoor = Lit("Door", bark, new Color(0.55f, 0.4f, 0.32f), 0.05f);
        mLeaves = new[]
        {
            Lit("Leaves_A", leaves, Color.white, 0.05f),
            Lit("Leaves_B", leaves, new Color(0.85f, 1f, 0.75f), 0.05f),
            Lit("Leaves_C", leaves, new Color(1f, 0.85f, 0.65f), 0.05f),
        };
        mTuft = Lit("GrassTuft", tuft, Color.white, 0f, 0f, true);

        mLampGlow = Emissive("LampGlow", new Color(1f, 0.75f, 0.45f), 6f);
        mWindowLit = Emissive("WindowLit", new Color(1f, 0.78f, 0.45f), 3f);
        mWindowDark = Emissive("WindowDark", new Color(0.1f, 0.1f, 0.16f), 1f);
        mRune = Emissive("Rune", new Color(0.35f, 0.9f, 1f), 2.5f);
        mPortal = Emissive("Portal", new Color(0.45f, 0.22f, 0.9f), 1.3f);
        mBeacon = Emissive("Beacon", new Color(0.75f, 0.5f, 1f), 12f);

        mFountainWater = Water("FountainWater", new Color(0.12f, 0.55f, 0.7f, 0.72f), new Color(0.02f, 0.18f, 0.34f, 0.95f), 0.45f, 0.35f);
        mLakeWater = Water("LakeWater", new Color(0.18f, 0.5f, 0.55f, 0.5f), new Color(0.02f, 0.1f, 0.2f, 0.95f), 1.8f);
        mWaterfall = GetOrCreate("Waterfall", "Abby/Waterfall");

        mSpray = ParticleMat("WaterSpray", dot, false);
        mFirefly = ParticleMat("Firefly", dot, true);
        mCloud = ParticleMat("CloudPuff", cloud, false);
        AssetDatabase.SaveAssets();
    }

    static float Hash(int x, int y)
    {
        uint h = (uint)(x * 374761393 + y * 668265263);
        h = (h ^ (h >> 13)) * 1274126177u;
        return (h ^ (h >> 16)) / (float)uint.MaxValue;
    }

    static Texture2D Tex(string name, int size, System.Func<int, int, Color> px,
                         FilterMode filter = FilterMode.Point, TextureWrapMode wrap = TextureWrapMode.Repeat)
    {
        string path = TexDir + "/" + name + ".png";
        var t = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) t.SetPixel(x, y, px(x, y));
        t.Apply();
        System.IO.File.WriteAllBytes(path, t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(path);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Default;
        imp.filterMode = filter;
        imp.wrapMode = wrap;
        imp.mipmapEnabled = filter != FilterMode.Point;
        imp.alphaIsTransparency = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static Material GetOrCreate(string name, string shader)
    {
        string path = MatDir + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(Shader.Find(shader)) { name = name };
            AssetDatabase.CreateAsset(m, path);
        }
        else if (m.shader.name != shader) m.shader = Shader.Find(shader);
        return m;
    }

    static Material Lit(string name, Texture2D tex, Color color, float smoothness, float metallic = 0f, bool cutoutTwoSided = false)
    {
        var m = GetOrCreate(name, "Universal Render Pipeline/Lit");
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", color);
        m.SetFloat("_Smoothness", smoothness);
        m.SetFloat("_Metallic", metallic);
        if (cutoutTwoSided)
        {
            m.SetFloat("_AlphaClip", 1f); m.EnableKeyword("_ALPHATEST_ON"); m.SetFloat("_Cutoff", 0.5f);
            m.SetFloat("_Cull", 0f); m.doubleSidedGI = true;
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
        }
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Emissive(string name, Color color, float intensity)
    {
        var m = GetOrCreate(name, "Universal Render Pipeline/Unlit");
        Color hdr = color * intensity; hdr.a = 1f;   // HDR: el Bloom lo hace brillar
        m.SetColor("_BaseColor", hdr);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Water(string name, Color shallow, Color deep, float depth, float reflection = 0.85f)
    {
        var m = GetOrCreate(name, "Abby/Water");
        m.SetColor("_ShallowColor", shallow);
        m.SetColor("_DeepColor", deep);
        m.SetFloat("_DepthDistance", depth);
        m.SetFloat("_Reflection", reflection);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material ParticleMat(string name, Texture2D tex, bool additive)
    {
        var m = GetOrCreate(name, "Abby/Particle");
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", additive ? new Color(3f, 2.8f, 1.4f, 1f) : Color.white);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(m);
        return m;
    }
#endif
}
