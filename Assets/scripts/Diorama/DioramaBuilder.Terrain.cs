using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Terreno del diorama: terrazas de roca escalonadas (estilo Octopath). Una función de altura por niveles decide la
/// forma (cañón del río, cascada, risco al valle, bancales en los bordes) y se arma una malla de celdas: arriba pasto,
/// a los lados paredes de roca con musgo en la orilla. La misma malla es el collider.
/// </summary>
public partial class DioramaBuilder
{
    const float Cell = 0.7f;
    static readonly Rect TerrainArea = new Rect(-90f, -62f, 180f, 162f);
    const float RiverStartX = -24f, FenceZ = 10.8f, WaterLevel = -4.1f, StreamWater = -0.35f;
    const float Chunk = 30f;

    // ------------------------------------------------------------------ forma

    /// <summary>Curva suave del río a lo largo de X.</summary>
    static float Meander(float x) => Mathf.Sin(x * 0.07f) * 0.8f;

    /// <summary>Altura del terreno (m) en (x, z): siempre uno de unos cuantos niveles, así salen bancales.</summary>
    public float Height(float x, float z)
    {
        float e = (Noise(x * 0.18f, z * 0.18f) * 2f - 1f) + (Noise(x * 0.5f + 40f, z * 0.5f) * 2f - 1f) * 0.35f
                + (Noise(x * 1.3f - 17f, z * 1.3f + 9f) * 2f - 1f) * 0.25f;   // orillas irregulares, nada de líneas rectas
        float h = 0f;

        // bancales que enmarcan el pueblo (este, oeste y sur): suben en escalones con bosque encima
        float ex = Mathf.Abs(x) - 37f + e * 2f;
        if (ex > 0f) h = Mathf.Max(h, ex < 5f ? 1.5f : ex < 10f ? 3f : ex < 16f ? 4.8f : 7f);
        float es = -z - 35f + e * 2f;
        if (es > 0f) h = Mathf.Max(h, es < 6f ? 1.5f : 3f);

        // al norte el terreno baja al valle en escalones
        float en = z - CliffZ + e * 2.5f;
        if (en > 0f) h = en < 4f ? -3f : en < 8f ? -6.5f : en < 13f ? -10.5f : en < 19f ? -15f : en < 26f ? -21f : en < 34f ? -29f : -ValleyDepth;

        // el río: arroyo arriba (oeste) y cañón con bancales desde la cascada
        float rz = Mathf.Abs(z - Meander(x));
        bool inCanyon = rz < 3.2f ? x > RiverStartX : x > RiverStartX + e * 1.2f;   // la orilla de la cascada es recta
        if (inCanyon)
        {
            float d = rz + e * 1.6f;
            float c = d < 2.6f ? -4.6f : d < 4.4f ? -3.2f : d < 6f ? -1.8f : d < 7.2f ? -0.9f : 0f;
            h = Mathf.Min(h, c);
        }
        else if (rz + e * 0.3f < 2.2f) h = Mathf.Min(h, -0.8f);
        return h;
    }

    static float Noise(float x, float y)
    {
        int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
        float tx = x - xi, ty = y - yi;
        tx = tx * tx * (3f - 2f * tx); ty = ty * ty * (3f - 2f * ty);
        float a = Hash(xi, yi), b = Hash(xi + 1, yi), c = Hash(xi, yi + 1), d = Hash(xi + 1, yi + 1);
        return Mathf.Lerp(Mathf.Lerp(a, b, tx), Mathf.Lerp(c, d, tx), ty);
    }

    static float Hash(int x, int y)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xffff) / 65535f;
        }
    }

    // ------------------------------------------------------------------ malla

    void BuildTerrain()
    {
        var g = Group("Terreno", root);
        int nx = Mathf.CeilToInt(TerrainArea.width / Cell), nz = Mathf.CeilToInt(TerrainArea.height / Cell);
        var hgt = new float[nx, nz];
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
                hgt[i, j] = Height(TerrainArea.xMin + (i + 0.5f) * Cell, TerrainArea.yMin + (j + 0.5f) * Cell);

        int per = Mathf.RoundToInt(Chunk / Cell);
        for (int ci = 0; ci < nx; ci += per)
            for (int cj = 0; cj < nz; cj += per)
                BuildTerrainChunk(g, hgt, nx, nz, ci, cj, Mathf.Min(ci + per, nx), Mathf.Min(cj + per, nz));
    }

    void BuildTerrainChunk(Transform parent, float[,] hgt, int nx, int nz, int i0, int j0, int i1, int j1)
    {
        var v = new List<Vector3>(); var n = new List<Vector3>(); var uv = new List<Vector2>();
        var tops = new List<int>(); var sides = new List<int>(); var beds = new List<int>();
        const float tile = 0.5f, wallTile = 4f;   // 2 m por repetición arriba; 4 m de alto en la roca

        void Quad(List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal, Vector2 ua, Vector2 ub, Vector2 uc, Vector2 ud)
        {
            int k = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            n.Add(normal); n.Add(normal); n.Add(normal); n.Add(normal);
            uv.Add(ua); uv.Add(ub); uv.Add(uc); uv.Add(ud);
            tris.AddRange(new[] { k, k + 2, k + 3, k, k + 3, k + 1 });
        }

        for (int i = i0; i < i1; i++)
            for (int j = j0; j < j1; j++)
            {
                float h = hgt[i, j];
                float x0 = TerrainArea.xMin + i * Cell, z0 = TerrainArea.yMin + j * Cell, x1 = x0 + Cell, z1 = z0 + Cell;
                // arriba
                Quad(h <= -4.5f ? beds : tops,
                     new Vector3(x0, h, z0), new Vector3(x1, h, z0), new Vector3(x0, h, z1), new Vector3(x1, h, z1), Vector3.up,
                     new Vector2(x0, z0) * tile, new Vector2(x1, z0) * tile, new Vector2(x0, z1) * tile, new Vector2(x1, z1) * tile);
                // paredes hacia los vecinos más bajos (la cara mira hacia el vecino)
                TrySide(i + 1, j, new Vector3(x1, 0f, z1), new Vector3(x1, 0f, z0), Vector3.right, z1, z0);
                TrySide(i - 1, j, new Vector3(x0, 0f, z0), new Vector3(x0, 0f, z1), Vector3.left, z0, z1);
                TrySide(i, j + 1, new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1), Vector3.forward, x0, x1);
                TrySide(i, j - 1, new Vector3(x1, 0f, z0), new Vector3(x0, 0f, z0), Vector3.back, x1, x0);

                void TrySide(int ni, int nj, Vector3 pa, Vector3 pb, Vector3 normal, float ua, float ub)
                {
                    float lo = ni < 0 || nj < 0 || ni >= nx || nj >= nz ? -ValleyDepth - 2f : hgt[ni, nj];
                    if (lo >= h - 0.01f) return;
                    var tp = pa; pa = pb; pb = tp; float tu = ua; ua = ub; ub = tu;   // orden para que la cara mire al vecino
                    // v = 1 en la orilla de arriba: ahí cae el musgo de la textura
                    float vlo = 1f - (h - lo) / wallTile;
                    Quad(sides, new Vector3(pa.x, lo, pa.z), new Vector3(pb.x, lo, pb.z), new Vector3(pa.x, h, pa.z), new Vector3(pb.x, h, pb.z), normal,
                         new Vector2(ua * 0.5f, vlo), new Vector2(ub * 0.5f, vlo), new Vector2(ua * 0.5f, 1f), new Vector2(ub * 0.5f, 1f));
                }
            }

        var m = new Mesh { name = "Terreno", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(v); m.SetNormals(n); m.SetUVs(0, uv);
        m.subMeshCount = 3;
        m.SetTriangles(tops, 0); m.SetTriangles(sides, 1); m.SetTriangles(beds, 2);
        m.RecalculateTangents(); m.RecalculateBounds();
        var go = new GameObject("Terreno_" + i0 + "_" + j0);
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = m;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = new[] { mGrass, mRock, mDirt };
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
        go.AddComponent<MeshCollider>().sharedMesh = m;
    }

    // ------------------------------------------------------------------ agua

    void BuildWater()
    {
        var g = Group("Agua", root);
        // río del cañón: tira que sigue la curva
        var strip = RiverStrip(RiverStartX - 0.2f, TerrainArea.xMax, WaterLevel, 10f);
        var river = MeshObject(g, "Rio", strip, mWater, false);
        // arroyo de arriba
        MeshObject(g, "Arroyo", RiverStrip(TerrainArea.xMin, RiverStartX + 0.3f, StreamWater, 4.6f), mWater, false);

        // cascada al oeste: el arroyo cae al cañón
        Waterfall(g, new Vector3(RiverStartX + 0.05f, (StreamWater + WaterLevel) / 2f, Meander(RiverStartX)), Vector3.right, 4.6f, StreamWater - WaterLevel);

        // rocas en el río con espuma (rápidos)
        for (int i = 0; i < 26; i++)
        {
            float x = R(RiverStartX + 2f, 70f), z = Meander(x) + R(-2.2f, 2.2f);
            float s = R(0.5f, 1.4f);
            var rock = Prim(g, PrimitiveType.Sphere, "RocaRio", new Vector3(x, WaterLevel - 0.1f, z), new Vector3(s * R(1f, 1.6f), s * 0.8f, s), mRock);
            rock.transform.localRotation = Quaternion.Euler(R(0f, 20f), R(0f, 360f), 0f);
            if (i % 2 == 0) Foam(g, new Vector3(x - s * 0.6f, WaterLevel + 0.05f, z), 0.8f, 6f);
        }
        Foam(g, new Vector3(RiverStartX + 1f, WaterLevel + 0.1f, Meander(RiverStartX)), 2.4f, 30f);

        // muro invisible en la orilla de la cascada (el arroyo se puede pisar, pero no caer al cañón)
        SolidBox(g, "Collider_Cascada", new Vector3(RiverStartX - 1.5f, 1f, 0f), new Vector3(0.6f, 6f, 2f * FenceZ));
    }

    Mesh RiverStrip(float xa, float xb, float y, float width)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        int segs = Mathf.CeilToInt((xb - xa) / 1.5f);
        for (int s = 0; s <= segs; s++)
        {
            float x = Mathf.Lerp(xa, xb, s / (float)segs), z = Meander(x);
            v.Add(new Vector3(x, y, z - width / 2f)); v.Add(new Vector3(x, y, z + width / 2f));
            uv.Add(new Vector2(x * 0.1f, 0f)); uv.Add(new Vector2(x * 0.1f, width * 0.1f));
            if (s < segs) { int k = s * 2; t.AddRange(new[] { k, k + 1, k + 3, k, k + 3, k + 2 }); }
        }
        var m = new Mesh { name = "Rio" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    void Waterfall(Transform parent, Vector3 center, Vector3 facing, float width, float height)
    {
        var m = new Mesh { name = "Cascada" };
        m.vertices = new[] { new Vector3(-width / 2f, -height / 2f, 0f), new Vector3(width / 2f, -height / 2f, 0f), new Vector3(-width / 2f, height / 2f, 0f), new Vector3(width / 2f, height / 2f, 0f) };
        // uv: x = metros a lo ancho, y = metros de caída desde el labio (así lo espera el shader Abby/Waterfall)
        m.uv = new[] { new Vector2(0f, height), new Vector2(width, height), new Vector2(0f, 0f), new Vector2(width, 0f) };
        m.triangles = new[] { 0, 2, 3, 0, 3, 1 };
        m.RecalculateNormals(); m.RecalculateBounds();
        var go = MeshObject(parent, "Cascada", m, mWaterfall, false);
        go.transform.localPosition = center;
        go.transform.localRotation = Quaternion.LookRotation(-facing, Vector3.up);
        var spray = new GameObject("Bruma").AddComponent<ParticleSystem>();
        spray.transform.SetParent(parent, false);
        spray.transform.localPosition = center - Vector3.up * height / 2f + facing * 0.4f;
        spray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = spray.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startColor = new Color(0.95f, 0.97f, 1f, 0.22f);
        main.maxParticles = 80; main.prewarm = true;
        var em = spray.emission; em.rateOverTime = 22f;
        var sh = spray.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(width, 0.2f, 0.4f);
        spray.GetComponent<ParticleSystemRenderer>().sharedMaterial = mSmoke;
        spray.Play();
    }

    void Foam(Transform parent, Vector3 pos, float size, float rate)
    {
        var ps = new GameObject("Espuma").AddComponent<ParticleSystem>();
        ps.transform.SetParent(parent, false);
        ps.transform.localPosition = pos;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(size * 0.3f, size * 0.7f);
        main.startColor = new Color(1f, 1f, 1f, 0.35f);
        main.maxParticles = 50; main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = rate;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Circle; sh.radius = size * 0.5f; sh.rotation = new Vector3(90f, 0f, 0f);
        ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = mSmoke;
        ps.Play();
    }

    // ------------------------------------------------------------------ puentes y cercas

    /// <summary>Puente largo de tablones sobre el cañón, con caballetes que bajan hasta el río y barandas.</summary>
    void BuildCanyonBridge(float x)
    {
        var b = Group("PuenteCanon", root);
        b.localPosition = new Vector3(x, 0f, 0f);
        float w = 3.4f, half = FenceZ + 0.6f;
        for (float z = -half; z < half; z += 0.45f)
        {
            var p = Box(b, "Tablon", new Vector3(R(-0.06f, 0.06f), 0.05f, z + 0.22f), new Vector3(w, 0.12f, 0.4f), mPlank, false);
            p.transform.localRotation = Quaternion.Euler(R(-1f, 1f), R(-1.5f, 1.5f), 0f);
        }
        foreach (float sx in new[] { -1f, 1f })
        {
            Box(b, "Larguero", new Vector3(sx * (w / 2f - 0.15f), -0.22f, 0f), new Vector3(0.3f, 0.35f, half * 2f), mTimber, false);
            for (float z = -half + 0.2f; z <= half; z += 1.8f)
                Box(b, "Poste", new Vector3(sx * w / 2f, 0.6f, z), new Vector3(0.16f, 1.2f, 0.16f), mTimber, false);
            Box(b, "Pasamanos", new Vector3(sx * w / 2f, 1.15f, 0f), new Vector3(0.12f, 0.1f, half * 2f), mTimber, false);
            Box(b, "Travesano", new Vector3(sx * w / 2f, 0.6f, 0f), new Vector3(0.08f, 0.08f, half * 2f), mTimber, false);
            SolidBox(b, "Collider_Baranda", new Vector3(sx * (w / 2f + 0.1f), 1f, 0f), new Vector3(0.2f, 2f, half * 2f));
        }
        // caballetes: patas que bajan hasta el suelo de cada bancal
        for (float z = -half + 2f; z < half - 1f; z += 3.2f)
        {
            float ground = Height(x, z);
            if (ground > -0.5f) continue;
            float len = -ground;
            foreach (float sx in new[] { -1f, 1f })
                Box(b, "Pata", new Vector3(sx * (w / 2f - 0.3f), ground / 2f - 0.2f, z), new Vector3(0.26f, len, 0.26f), mTimber, false);
            var cross = Box(b, "Cruz", new Vector3(0f, ground / 2f, z), new Vector3(w - 0.4f, 0.14f, 0.12f), mTimber, false);
            cross.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(len, w) * Mathf.Rad2Deg);
        }
        SolidBox(b, "Collider_Tablero", new Vector3(0f, -0.1f, 0f), new Vector3(w, 0.3f, half * 2f + 0.4f));
        blocked.Add(new Rect(x - w / 2f - 0.6f, -half - 0.6f, w + 1.2f, half * 2f + 1.2f));
    }

    /// <summary>Puentecito sobre el arroyo.</summary>
    void BuildStreamBridge(float x)
    {
        var b = Group("PuenteArroyo", root);
        b.localPosition = new Vector3(x, 0f, Meander(x));
        float w = 2.6f, half = 3f;
        for (float z = -half; z < half; z += 0.45f)
            Box(b, "Tablon", new Vector3(0f, 0.05f, z + 0.22f), new Vector3(w, 0.12f, 0.4f), mPlank, false);
        foreach (float sx in new[] { -1f, 1f })
        {
            Box(b, "Larguero", new Vector3(sx * (w / 2f - 0.15f), -0.2f, 0f), new Vector3(0.25f, 0.3f, half * 2f), mTimber, false);
            foreach (float z in new[] { -half + 0.2f, 0f, half - 0.2f })
                Box(b, "Poste", new Vector3(sx * w / 2f, 0.5f, z), new Vector3(0.14f, 1f, 0.14f), mTimber, false);
            Box(b, "Pasamanos", new Vector3(sx * w / 2f, 0.95f, 0f), new Vector3(0.1f, 0.08f, half * 2f), mTimber, false);
            SolidBox(b, "Collider_Baranda", new Vector3(sx * (w / 2f + 0.1f), 1f, 0f), new Vector3(0.2f, 2f, half * 2f));
        }
        SolidBox(b, "Collider_Tablero", new Vector3(0f, -0.1f, 0f), new Vector3(w, 0.3f, half * 2f + 0.4f));
        blocked.Add(new Rect(x - w / 2f - 0.6f, -half - 0.6f, w + 1.2f, half * 2f + 1.2f));
    }

    /// <summary>Cercas de madera en la orilla del cañón (no se cae una al río) con hueco en el puente.</summary>
    void BuildFences(float bridgeX)
    {
        var g = Group("Cercas", root);
        foreach (float sz in new[] { -1f, 1f })
        {
            Fence(g, new Vector2(RiverStartX - 1.5f, sz * FenceZ), new Vector2(bridgeX - 1.8f, sz * FenceZ));
            Fence(g, new Vector2(bridgeX + 1.8f, sz * FenceZ), new Vector2(Walk.xMax, sz * FenceZ));
        }
        // a lo ancho, en la orilla de la cascada (cruza el arroyo)
        Fence(g, new Vector2(RiverStartX - 1.5f, -FenceZ), new Vector2(RiverStartX - 1.5f, -2.6f));
        Fence(g, new Vector2(RiverStartX - 1.5f, 2.6f), new Vector2(RiverStartX - 1.5f, FenceZ));
    }

    void Fence(Transform parent, Vector2 a, Vector2 b)
    {
        Vector2 d = b - a;
        float len = d.magnitude;
        if (len < 0.3f) return;
        Vector2 dir = d / len;
        int posts = Mathf.Max(1, Mathf.RoundToInt(len / 1.6f));
        for (int i = 0; i <= posts; i++)
        {
            Vector2 p = a + dir * (len * i / posts);
            var post = Box(parent, "Poste", new Vector3(p.x, 0.45f, p.y), new Vector3(0.14f, 0.95f, 0.14f), mTimber, false);
            post.transform.localRotation = Quaternion.Euler(R(-3f, 3f), R(0f, 90f), R(-3f, 3f));
        }
        Vector2 c = (a + b) / 2f;
        float yaw = Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg;
        foreach (float y in new[] { 0.4f, 0.78f })
        {
            var rail = Box(parent, "Travesano", new Vector3(c.x, y, c.y), new Vector3(0.07f, 0.09f, len), mTimber, false);
            rail.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
        var col = new GameObject("Collider_Cerca");
        col.transform.SetParent(parent, false);
        col.transform.localPosition = new Vector3(c.x, 1f, c.y);
        col.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        col.AddComponent<BoxCollider>().size = new Vector3(0.3f, 2f, len);
    }
}
