using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Mapa de prueba "Diorama HD-2D": un pueblito al pie de la torre visto como maqueta (estilo Octopath / Triangle Strategy).
/// Canal con puente de piedra y puente de madera, casas de entramado con techo de tejas, mercado, pozo, faroles,
/// árboles y pinos, y al norte una escalinata que sube a un patio en ruinas (pelea con oleadas). Al fondo montañas
/// y la torre. Todo lo genera este script: clic derecho en el componente → "Generar diorama".
/// Se entra desde Pausa → Opciones → "Probar diorama HD-2D".
/// </summary>
public class DioramaBuilder : MonoBehaviour
{
    public int seed = 7;
    [Tooltip("Mechones de pasto (malla combinada).")]
    public int grassTufts = 2600;

    const string Prefix = "Dio_";
    const float TerraceY = 2.6f, CanalHalf = 2.5f, CanalFloor = -1.6f, WaterY = -0.75f;
    static readonly Rect Walk = new Rect(-33f, -29f, 66f, 67f);      // zona jugable (x, z)
    static readonly Rect Terrace = new Rect(-15f, 16f, 30f, 18f);     // patio en alto
    const float StairZ0 = 10f, StairW = 5f;
    const float CliffZ = 48f, ValleyDepth = 40f;

    System.Random rng;
    float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

    Material mGrass, mCobble, mPlaza, mStone, mBrick, mPlaster, mTimber, mRoof, mDoor, mPlank, mBark, mPine, mTuft,
             mMetal, mWindow, mWindowLit, mWater, mCliff, mSnow, mCloth, mClothB, mFruitR, mFruitG, mFruitY, mGlow, mDust, mSmoke;
    Material[] mLeaves, mFlowers;
    readonly List<Rect> blocked = new List<Rect>();   // donde no van árboles ni pasto
    Transform root;

    // ------------------------------------------------------------------ generación

    [ContextMenu("Generar diorama")]
    public void Build()
    {
        Clear();
        rng = new System.Random(seed);
        blocked.Clear();
        LoadMaterials();
        root = Group(Prefix + "Mapa", transform);

        BuildGround();
        BuildCanal();
        BuildMainBridge(0f);
        BuildWoodBridge(-20f);
        BuildPaths();
        BuildTerrace();
        BuildRuins();
        BuildVillage();
        BuildLamps();
        BuildTrees();
        BuildGrass();
        BuildBackdrop();
        BuildBoundary();
        BuildAtmosphere();
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

    // ------------------------------------------------------------------ suelo y agua

    void BuildGround()
    {
        var g = Group("Suelo", root);
        // pasto (más grande que la zona jugable: al girar la cámara no se ve el borde)
        Flat(g, "PastoSur", new Vector3(0f, 0f, -36.25f), 140f, 67.5f, mGrass, 0.25f);
        Flat(g, "PastoNorte", new Vector3(0f, 0f, (CanalHalf + CliffZ) / 2f), 140f, CliffZ - CanalHalf, mGrass, 0.25f);
        SolidBox(g, "Collider_Sur", new Vector3(0f, -0.5f, -36.25f), new Vector3(140f, 1f, 67.5f));
        SolidBox(g, "Collider_Norte", new Vector3(0f, -0.5f, (CanalHalf + CliffZ) / 2f), new Vector3(140f, 1f, CliffZ - CanalHalf));
        // el pueblo está en lo alto de un risco: al norte el terreno cae al valle (así la cámara ve el paisaje a lo lejos)
        Wall(g, "Risco", new Vector3(0f, -ValleyDepth / 2f, CliffZ), Vector3.forward, 140f, ValleyDepth, mCliff, 0.08f);
        for (int i = 0; i < 40; i++)
        {
            var rock = Prim(g, PrimitiveType.Sphere, "Peña", new Vector3(R(-70f, 70f), R(-ValleyDepth, -2f), CliffZ + R(0.5f, 4f)), new Vector3(R(4f, 9f), R(3f, 7f), R(3f, 6f)), mCliff);
            rock.transform.localRotation = Quaternion.Euler(R(0f, 40f), R(0f, 360f), R(0f, 40f));
            rock.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        Flat(g, "Valle", new Vector3(0f, -ValleyDepth, 200f), 700f, 320f, mGrass, 0.05f);
        var river = Flat(g, "Rio", new Vector3(0f, -ValleyDepth + 0.3f, 105f), 700f, 14f, mWater, 0.05f);
        river.transform.localRotation = Quaternion.Euler(0f, 8f, 0f);
    }

    void BuildCanal()
    {
        var c = Group("Canal", root);
        float len = 140f;
        Flat(c, "Fondo", new Vector3(0f, CanalFloor, 0f), len, CanalHalf * 2f, mCliff, 0.3f);
        SolidBox(c, "Collider_Fondo", new Vector3(0f, CanalFloor - 0.5f, 0f), new Vector3(len, 1f, CanalHalf * 2f));
        float h = -CanalFloor;
        Wall(c, "OrillaSur", new Vector3(0f, CanalFloor + h / 2f, -CanalHalf), Vector3.forward, len, h, mBrick, 0.5f);
        Wall(c, "OrillaNorte", new Vector3(0f, CanalFloor + h / 2f, CanalHalf), Vector3.back, len, h, mBrick, 0.5f);
        var water = Flat(c, "Agua", new Vector3(0f, WaterY, 0f), len, CanalHalf * 2f, mWater, 0.2f);
        water.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        // bordillo de piedra sobre las orillas
        foreach (float sz in new[] { -1f, 1f })
            Box(c, "Bordillo", new Vector3(0f, 0.06f, sz * (CanalHalf + 0.25f)), new Vector3(len, 0.12f, 0.5f), mStone, false);

        // parapeto almenado a los dos lados, con huecos para los puentes
        var gaps = new[] { new Vector2(-1.9f, 1.9f), new Vector2(-21.5f, -18.5f) };
        foreach (float sz in new[] { -1f, 1f })
        {
            float z = sz * (CanalHalf + 0.25f);
            float x = Walk.xMin;
            foreach (var gap in gaps.OrderedByX())
            {
                Parapet(c, x, gap.x, z);
                x = gap.y;
            }
            Parapet(c, x, Walk.xMax, z);
        }
        blocked.Add(new Rect(-70f, -CanalHalf - 1f, 140f, CanalHalf * 2f + 2f));
    }

    /// <summary>Murete de 0.7 m con almenas, de x0 a x1, centrado en z. Bloquea el paso.</summary>
    void Parapet(Transform parent, float x0, float x1, float z)
    {
        float len = x1 - x0;
        if (len < 0.3f) return;
        float cx = (x0 + x1) / 2f;
        Box(parent, "Parapeto", new Vector3(cx, 0.35f, z), new Vector3(len, 0.7f, 0.5f), mStone, true);
        for (float x = x0 + 0.45f; x < x1 - 0.3f; x += 1.3f)
            Box(parent, "Almena", new Vector3(x, 0.85f, z), new Vector3(0.6f, 0.3f, 0.55f), mStone, false);
        SolidBox(parent, "Collider_Parapeto", new Vector3(cx, 1f, z), new Vector3(len, 2f, 0.5f));
    }

    /// <summary>Puente de piedra con arco (se ve el agua por debajo) y pretiles.</summary>
    void BuildMainBridge(float x)
    {
        var b = Group("PuentePiedra", root);
        b.localPosition = new Vector3(x, 0f, 0f);
        float w = 4.4f, half = CanalHalf + 0.6f, r = 2.25f, cy = -2.9f;   // el arco nace bajo el agua y su clave queda bajo el tablero
        Box(b, "Tablero", new Vector3(0f, -0.15f, 0f), new Vector3(w, 0.34f, half * 2f), mCobble, false);
        SolidBox(b, "Collider_Tablero", new Vector3(0f, -0.2f, 0f), new Vector3(w, 0.4f, half * 2f + 0.4f));
        // pretiles con remate
        foreach (float sx in new[] { -1f, 1f })
        {
            Box(b, "Pretil", new Vector3(sx * (w / 2f - 0.2f), 0.45f, 0f), new Vector3(0.4f, 0.9f, half * 2f), mStone, false);
            Box(b, "Remate", new Vector3(sx * (w / 2f - 0.2f), 0.95f, 0f), new Vector3(0.55f, 0.12f, half * 2f + 0.1f), mPlaster, false);
            SolidBox(b, "Collider_Pretil", new Vector3(sx * (w / 2f - 0.2f), 1f, 0f), new Vector3(0.4f, 2f, half * 2f));
            foreach (float sz in new[] { -1f, 1f })   // pilares en las puntas con bola
            {
                Box(b, "Pilar", new Vector3(sx * (w / 2f - 0.2f), 0.6f, sz * half), new Vector3(0.6f, 1.2f, 0.6f), mStone, false);
                Prim(b, PrimitiveType.Sphere, "Bola", new Vector3(sx * (w / 2f - 0.2f), 1.42f, sz * half), Vector3.one * 0.42f, mStone);
            }
        }
        // arco: dovelas en las dos caras y bóveda por debajo
        int n = 11;
        for (int i = 0; i <= n; i++)
        {
            float a = Mathf.PI * i / n;
            float z = Mathf.Cos(a) * r, y = cy + Mathf.Sin(a) * r;
            var rot = Quaternion.Euler(-Mathf.Rad2Deg * a + 90f, 0f, 0f);
            foreach (float sx in new[] { -1f, 1f })
            {
                var v = Box(b, "Dovela", new Vector3(sx * (w / 2f - 0.1f), y, z), new Vector3(0.25f, 0.55f, Mathf.PI * r / n + 0.04f), mStone, false);
                v.transform.localRotation = rot;
            }
            var vault = Box(b, "Boveda", new Vector3(0f, y + 0.12f, z), new Vector3(w - 0.3f, 0.12f, 0.6f), mBrick, false);
            vault.transform.localRotation = rot;
            // tímpano: relleno de sillar entre el lomo del arco y el tablero, en las dos caras
            float top = -0.32f, bottom = Mathf.Max(y + 0.27f, CanalFloor);
            if (top - bottom > 0.05f && Mathf.Abs(z) < CanalHalf + 0.3f)
                foreach (float sx in new[] { -1f, 1f })
                    Box(b, "Timpano", new Vector3(sx * (w / 2f - 0.12f), (top + bottom) / 2f, z), new Vector3(0.24f, top - bottom, r * Mathf.PI / n + 0.05f), mStone, false);
        }
        blocked.Add(new Rect(x - w / 2f - 0.5f, -half - 0.5f, w + 1f, half * 2f + 1f));
    }

    /// <summary>Puente de tablones con postes y cuerda.</summary>
    void BuildWoodBridge(float x)
    {
        var b = Group("PuenteMadera", root);
        b.localPosition = new Vector3(x, 0f, 0f);
        float w = 3f, half = CanalHalf + 0.6f;
        for (float z = -half; z < half - 0.1f; z += 0.42f)
        {
            var p = Box(b, "Tablon", new Vector3(R(-0.05f, 0.05f), 0.04f, z + 0.2f), new Vector3(w, 0.1f, 0.38f), mPlank, false);
            p.transform.localRotation = Quaternion.Euler(R(-1.5f, 1.5f), R(-2f, 2f), 0f);
        }
        foreach (float sx in new[] { -1f, 1f })
        {
            Box(b, "Larguero", new Vector3(sx * (w / 2f - 0.2f), -0.2f, 0f), new Vector3(0.25f, 0.3f, half * 2f), mBark, false);
            for (float z = -half; z <= half + 0.01f; z += half)
                Box(b, "Poste", new Vector3(sx * (w / 2f), 0.55f, z), new Vector3(0.18f, 1.2f, 0.18f), mBark, false);
            Box(b, "Cuerda", new Vector3(sx * (w / 2f), 1f, 0f), new Vector3(0.05f, 0.05f, half * 2f), mPlank, false);
            SolidBox(b, "Collider_Baranda", new Vector3(sx * (w / 2f + 0.1f), 1f, 0f), new Vector3(0.2f, 2f, half * 2f));
            Box(b, "Pilote", new Vector3(sx * (w / 2f - 0.2f), (CanalFloor - 0.2f) / 2f, 0f), new Vector3(0.3f, -CanalFloor, 0.3f), mBark, false);
        }
        SolidBox(b, "Collider_Tablero", new Vector3(0f, -0.1f, 0f), new Vector3(w, 0.3f, half * 2f + 0.4f));
        blocked.Add(new Rect(x - w / 2f - 0.5f, -half - 0.5f, w + 1f, half * 2f + 1f));
    }

    void BuildPaths()
    {
        var p = Group("Caminos", root);
        Strip(p, new Vector2(0f, -29f), new Vector2(0f, -CanalHalf - 0.6f), 3.6f);
        Strip(p, new Vector2(0f, CanalHalf + 0.6f), new Vector2(0f, StairZ0), 3.6f);
        Strip(p, new Vector2(-27f, -12f), new Vector2(27f, -12f), 3f);
        Strip(p, new Vector2(-20f, -12f), new Vector2(-20f, -CanalHalf - 0.6f), 2.4f);
        Strip(p, new Vector2(-20f, CanalHalf + 0.6f), new Vector2(-20f, 7.5f), 2.4f);
        Strip(p, new Vector2(-20f, 7.5f), new Vector2(0f, 7.5f), 2.4f);
        // plazuela del mercado
        Flat(p, "Plazuela", new Vector3(5f, 0.012f, -12f), 12f, 8f, mCobble, 0.5f);
        blocked.Add(new Rect(-1f, -16f, 12f, 8f));
    }

    /// <summary>Tira de empedrado entre dos puntos (eje X o Z).</summary>
    void Strip(Transform parent, Vector2 a, Vector2 b, float width)
    {
        Vector2 d = b - a;
        float len = d.magnitude;
        bool alongZ = Mathf.Abs(d.y) > Mathf.Abs(d.x);
        Vector2 c = (a + b) / 2f;
        Flat(parent, "Camino", new Vector3(c.x, 0.01f, c.y), alongZ ? width : len, alongZ ? len : width, mCobble, 0.5f);
        var rect = alongZ ? new Rect(c.x - width / 2f - 0.4f, Mathf.Min(a.y, b.y), width + 0.8f, len) : new Rect(Mathf.Min(a.x, b.x), c.y - width / 2f - 0.4f, len, width + 0.8f);
        blocked.Add(rect);
        // bordillo de piedra a los lados del camino
        foreach (float sgn in new[] { -1f, 1f })
        {
            Vector3 off = alongZ ? new Vector3(sgn * (width / 2f + 0.12f), 0.05f, 0f) : new Vector3(0f, 0.05f, sgn * (width / 2f + 0.12f));
            var curb = Box(parent, "Bordillo", new Vector3(c.x, 0f, c.y) + off, alongZ ? new Vector3(0.25f, 0.12f, len) : new Vector3(len, 0.12f, 0.25f), mStone, false);
            curb.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
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
            var v = Prim(t, PrimitiveType.Sphere, "Hiedra", new Vector3(x, R(0.8f, TerraceY), r.yMin - 0.12f), new Vector3(R(0.6f, 1.3f), R(0.6f, 1.6f), 0.35f), mLeaves[rng.Next(mLeaves.Length)]);
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
        float[] zs = { 19.5f, 25f, 30.5f };
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
        var fallen = Prim(g, PrimitiveType.Cylinder, "ColumnaCaida", new Vector3(-6f, y + 0.45f, 21f), new Vector3(0.9f, 1.8f, 0.9f), mStone, true);
        fallen.transform.localRotation = Quaternion.Euler(0f, 30f, 90f);
        // braseros en las esquinas del patio
        foreach (var c in new[] { new Vector2(-12.5f, 17.8f), new Vector2(12.5f, 17.8f), new Vector2(-12.5f, 31.5f), new Vector2(12.5f, 31.5f) })
            Brazier(g, new Vector3(c.x, y, c.y));
        // estatua al centro del fondo (pedestal + figura)
        Box(g, "Pedestal", new Vector3(0f, y + 0.6f, zb - 0.2f), new Vector3(1.6f, 1.2f, 1.6f), mStone, true);
        Prim(g, PrimitiveType.Capsule, "Estatua", new Vector3(0f, y + 2.3f, zb - 0.2f), new Vector3(0.8f, 1.1f, 0.8f), mPlaster);
        Prim(g, PrimitiveType.Sphere, "Cabeza", new Vector3(0f, y + 3.6f, zb - 0.2f), Vector3.one * 0.55f, mPlaster);
        // hiedra en ruinas
        for (int i = 0; i < 30; i++)
        {
            var v = Prim(g, PrimitiveType.Sphere, "Hiedra", new Vector3(R(r.xMin + 1f, r.xMax - 1f), y + R(0.1f, 0.5f), R(r.yMax - 3f, r.yMax - 0.6f)), new Vector3(R(0.5f, 1.2f), R(0.3f, 0.7f), R(0.5f, 1.2f)), mLeaves[rng.Next(mLeaves.Length)]);
            v.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // la pelea: oleadas de sombras y brutos al entrar al patio
        var arena = Group("PatioRuinas", g);
        arena.localPosition = new Vector3(0f, y, r.center.y);
        var enc = arena.gameObject.AddComponent<RoomEncounter>();
        enc.size = new Vector2(r.width - 2f, r.height - 2f);
        enc.triggerInset = 3f;
        enc.doors = new LevelDoor[0];
        enc.displayName = "Patio en ruinas";
        enc.maxAlive = 4;
        var sp = new List<Transform>();
        foreach (var p in new[] { new Vector2(-8f, -4f), new Vector2(8f, -4f), new Vector2(0f, 4f), new Vector2(-8f, 4f), new Vector2(8f, 4f), new Vector2(0f, -3f) })
        {
            var t = Group("Aparicion", arena);
            t.localPosition = new Vector3(p.x, 0.1f, p.y);
            sp.Add(t);
        }
        enc.spawnPoints = sp.ToArray();
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
            var v = Prim(c, PrimitiveType.Sphere, "Hiedra", new Vector3(R(-0.45f, 0.45f), R(0.5f, height * 0.8f), R(-0.45f, 0.45f)), new Vector3(R(0.4f, 0.7f), R(0.5f, 1f), R(0.4f, 0.7f)), mLeaves[rng.Next(mLeaves.Length)]);
            v.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var col = c.gameObject.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, height / 2f, 0f); col.size = new Vector3(1.1f, height, 1.1f);
        c.gameObject.AddComponent<OccluderFade>();
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
        House(v, new Vector3(-15f, 0f, -18f), 12f, 6.5f, 5f, 3.4f, true);
        House(v, new Vector3(16f, 0f, -19f), -8f, 7.5f, 5.5f, 3.6f, true);
        House(v, new Vector3(-26f, 0f, -6.5f), 90f, 5f, 4.2f, 3f, false);
        House(v, new Vector3(24f, 0f, -6.5f), -90f, 5.5f, 4.5f, 3.2f, false);
        House(v, new Vector3(-24f, 0f, 14f), 160f, 6f, 5f, 3.4f, true);
        House(v, new Vector3(24f, 0f, 13f), 200f, 6f, 4.6f, 3.2f, false);
        Stall(v, new Vector3(7.5f, 0f, -10f), -90f);
        Well(v, new Vector3(-7f, 0f, -21f));
        // barriles y cajas junto a casas y el mercado
        foreach (var p in new[] { new Vector3(10.5f, 0f, -8.6f), new Vector3(-11f, 0f, -15f), new Vector3(12f, 0f, -16.5f), new Vector3(-21f, 0f, -9f), new Vector3(20.5f, 0f, -9.5f), new Vector3(-4f, 0f, 9f) })
            Clutter(v, p);
        // huerto con cerca junto a la primera casa
        Garden(v, new Vector3(-8f, 0f, -26f), 7f, 4f);
        // flores
        for (int i = 0; i < 140; i++)
        {
            Vector2 q = new Vector2(R(Walk.xMin + 2f, Walk.xMax - 2f), R(Walk.yMin + 2f, 9f));
            if (!Free(q, 0.3f)) continue;
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
        h.gameObject.AddComponent<OccluderFade>();
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
        foreach (var p in new[] { new Vector2(2.6f, -25f), new Vector2(-2.6f, -17f), new Vector2(2.6f, -6f), new Vector2(-2.6f, 5f), new Vector2(3f, 9f), new Vector2(-3f, 9f), new Vector2(-17.5f, -13.8f), new Vector2(17f, -13.8f) })
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

    void BuildTrees()
    {
        var g = Group("Arboles", root);
        // unos cuantos dentro del pueblo
        int placed = 0;
        for (int tries = 0; placed < 16 && tries < 3000; tries++)
        {
            Vector2 q = new Vector2(R(Walk.xMin + 2f, Walk.xMax - 2f), R(Walk.yMin + 2f, Walk.yMax - 2f));
            if (!Free(q, 2f)) continue;
            if (rng.NextDouble() < 0.5) Tree(g, q, R(0.9f, 1.25f)); else Pine(g, q, R(0.9f, 1.3f));
            blocked.Add(new Rect(q.x - 1.5f, q.y - 1.5f, 3f, 3f));
            placed++;
        }
        // bosque alrededor (fuera de la zona jugable), más denso al norte detrás de las ruinas
        for (int i = 0, tries = 0; i < 150 && tries < 6000; tries++)
        {
            Vector2 q = new Vector2(R(-62f, 62f), R(-62f, CliffZ - 2.5f));
            bool outside = !Walk.Contains(q) || q.y > 34f;
            if (!outside || Mathf.Abs(q.y) < CanalHalf + 2f) continue;
            if (q.y < Walk.yMin && q.y > Walk.yMin - 14f && rng.NextDouble() < 0.85) continue;   // al sur, despejado: ahí está la cámara
            if (q.y > 34f && Mathf.Abs(q.x) < 12f && q.y < 42f) continue;   // claro detrás del patio
            if (rng.NextDouble() < 0.65) Pine(g, q, R(1f, 1.7f), collider: false); else Tree(g, q, R(1f, 1.5f), collider: false);
            i++;
        }
    }

    Mesh coneMesh, trunkMesh;

    void Pine(Transform parent, Vector2 q, float s, bool collider = true)
    {
        if (coneMesh == null) coneMesh = Cone(1f, 1f, 9);
        var t = Group("Pino", parent);
        t.localPosition = new Vector3(q.x, 0f, q.y);
        t.localRotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
        t.localScale = Vector3.one * s;
        Prim(t, PrimitiveType.Cylinder, "Tronco", new Vector3(0f, 0.7f, 0f), new Vector3(0.3f, 0.7f, 0.3f), mBark);
        for (int i = 0; i < 4; i++)
        {
            var c = MeshObject(t, "Copa", coneMesh, mPine, true);
            float r = 1.6f - i * 0.33f;
            c.transform.localPosition = new Vector3(0f, 1.1f + i * 0.95f, 0f);
            c.transform.localScale = new Vector3(r, 1.6f, r);
        }
        if (collider) { var cap = t.gameObject.AddComponent<CapsuleCollider>(); cap.radius = 0.4f; cap.height = 3f; cap.center = new Vector3(0f, 1.5f, 0f); }
        t.gameObject.AddComponent<OccluderFade>();   // si queda entre la cámara y Abby se deja de dibujar
    }

    void Tree(Transform parent, Vector2 q, float s, bool collider = true)
    {
        var t = Group("Arbol", parent);
        t.localPosition = new Vector3(q.x, 0f, q.y);
        t.localRotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
        t.localScale = Vector3.one * s;
        Prim(t, PrimitiveType.Cylinder, "Tronco", new Vector3(0f, 1.2f, 0f), new Vector3(0.38f, 1.2f, 0.38f), mBark);
        var leaf = mLeaves[rng.Next(mLeaves.Length)];
        int blobs = rng.Next(3, 5);
        for (int b = 0; b < blobs; b++)
        {
            float w = R(1.8f, 2.6f) - b * 0.25f;
            Prim(t, PrimitiveType.Sphere, "Copa", new Vector3(R(-0.7f, 0.7f), 2.7f + b * 0.55f + R(0f, 0.3f), R(-0.7f, 0.7f)), new Vector3(w, w * R(0.75f, 0.9f), w), leaf);
        }
        if (collider) { var cap = t.gameObject.AddComponent<CapsuleCollider>(); cap.radius = 0.45f; cap.height = 3f; cap.center = new Vector3(0f, 1.5f, 0f); }
        t.gameObject.AddComponent<OccluderFade>();
    }

    void BuildGrass()
    {
        var g = Group("Pasto", root);
        var tuft = TuftMesh();
        var batch = new List<CombineInstance>();
        int chunk = 0;
        for (int i = 0, tries = 0; i < grassTufts && tries < grassTufts * 6; tries++)
        {
            Vector2 q = new Vector2(R(-45f, 45f), R(-45f, CliffZ - 1f));
            if (!Free(q, 0f) || Terrace.Contains(q)) continue;
            float s = R(0.7f, 1.5f);
            batch.Add(new CombineInstance { mesh = tuft, transform = Matrix4x4.TRS(new Vector3(q.x, 0f, q.y), Quaternion.Euler(0f, R(0f, 360f), 0f), Vector3.one * s) });
            i++;
            if (batch.Count == 1500) { FlushGrass(g, batch, chunk++); batch.Clear(); }
        }
        if (batch.Count > 0) FlushGrass(g, batch, chunk);
    }

    void FlushGrass(Transform parent, List<CombineInstance> batch, int index)
    {
        var m = new Mesh { name = "Pasto_" + index, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.CombineMeshes(batch.ToArray(), true, true);
        MeshObject(parent, "Pasto_" + index, m, mTuft, false);
    }

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
        // cordillera al norte (la niebla la azula)
        for (int i = 0; i < 16; i++)
        {
            float x = -200f + i * 27f + R(-8f, 8f), z = R(220f, 300f), h = R(80f, 140f), r = R(50f, 80f);
            var m = MeshObject(g, "Montana", coneMesh, mCliff, false);
            m.transform.localPosition = new Vector3(x, -ValleyDepth, z);
            m.transform.localScale = new Vector3(r, h, r * R(0.7f, 1f));
            m.transform.localRotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
            var snow = MeshObject(g, "Nieve", coneMesh, mSnow, false);
            snow.transform.localPosition = new Vector3(x, -ValleyDepth + h * 0.62f, z);
            snow.transform.localScale = new Vector3(r * 0.4f, h * 0.38f + 0.5f, r * 0.4f * 0.9f);
        }
        // colinas y bosque en el valle
        for (int i = 0; i < 12; i++)
        {
            var hill = Prim(g, PrimitiveType.Sphere, "Colina", new Vector3(-140f + i * 26f + R(-8f, 8f), -ValleyDepth - 4f, R(150f, 200f)), new Vector3(R(40f, 70f), R(20f, 35f), R(30f, 45f)), mGrass);
            hill.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        for (int i = 0; i < 140; i++)
        {
            var q = new Vector2(R(-160f, 160f), R(70f, 190f));
            if (Mathf.Abs(q.y - (105f + q.x * Mathf.Tan(8f * Mathf.Deg2Rad))) < 10f) continue;   // el río
            var t = Group("PinoValle", g);
            t.localPosition = new Vector3(q.x, -ValleyDepth, q.y);
            float s = R(2.5f, 4.5f);
            for (int k = 0; k < 3; k++)
            {
                var c = MeshObject(t, "Copa", coneMesh, mPine, false);
                c.transform.localPosition = new Vector3(0f, s * (0.8f + k * 0.9f), 0f);
                c.transform.localScale = new Vector3(s * (1.5f - k * 0.35f), s * 1.6f, s * (1.5f - k * 0.35f));
            }
        }
        // la torre, a lo lejos (con su faro arriba)
        var tower = Group("Torre", g);
        tower.localPosition = new Vector3(-55f, -ValleyDepth, 170f);
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
        SolidBox(g, "Limite_N", new Vector3(r.center.x, 3f, r.yMax + 0.5f), new Vector3(r.width, 8f, 1f));
    }

    void BuildAtmosphere()
    {
        var g = Group("Ambiente", root);
        // sol de media tarde, cálido, con sombras suaves
        var sun = new GameObject("Sol").AddComponent<Light>();
        sun.transform.SetParent(g, false);
        sun.transform.localRotation = Quaternion.Euler(42f, 32f, 0f);
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.92f, 0.8f);
        sun.intensity = 1.7f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.8f;
        RenderSettings.sun = sun;

        var sky = g.gameObject.AddComponent<ExteriorSky>();
        sky.zenith = new Color(0.3f, 0.52f, 0.86f);
        sky.horizon = new Color(0.98f, 0.86f, 0.7f);
        sky.ground = new Color(0.36f, 0.34f, 0.3f);
        sky.sunColor = new Color(1f, 0.88f, 0.7f) * 1.5f;
        sky.cloudColor = new Color(1f, 0.97f, 0.94f);
        sky.cloudCover = 0.42f;
        sky.fogStart = 70f;
        sky.fogEnd = 320f;
#if UNITY_EDITOR
        var skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Exterior/Sky.mat");
        if (skyMat != null) RenderSettings.skybox = skyMat;
#endif
        sky.Apply();

        // polen y motas de luz flotando sobre el pueblo
        var ps = new GameObject("Polen").AddComponent<ParticleSystem>();
        ps.transform.SetParent(g, false);
        ps.transform.localPosition = new Vector3(0f, 2.5f, 2f);
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
        main.startColor = new Color(1f, 0.92f, 0.65f, 0.7f);
        main.maxParticles = 400;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 45f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(60f, 5f, 60f);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 0.2f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mDust;
        ps.Play();
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

    Material Plain(string name, Color c, float smooth) => Saved(name, () =>
    {
        var m = new Material(LitShader);
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", smooth);
        return m;
    });

    Material Glow(string name, Color c, float intensity) => Saved(name, () =>
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        Color hdr = c * intensity; hdr.a = 1f;
        m.SetColor("_BaseColor", hdr);
        return m;
    });

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

    Mesh tuftCache;
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
