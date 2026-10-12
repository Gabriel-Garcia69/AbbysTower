using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vegetación del diorama: árboles de copa irregular (bultos deformados con textura pixel de hojas), pinos de faldas
/// en picos, arbustos y pasto. Nada de esto se oculta: con la cámara de maqueta lo de enfrente solo se desenfoca.
/// </summary>
public partial class DioramaBuilder
{
    // cachés de mallas generadas: no se serializan (al recargar scripts Unity las dejaría vacías)
    [System.NonSerialized] Mesh[] blobMeshes;
    [System.NonSerialized] Mesh[] skirtMeshes;
    [System.NonSerialized] Mesh trunkTaper;

    void BuildTrees()
    {
        var g = Group("Arboles", root);
        // unos cuantos dentro del pueblo
        int placed = 0;
        for (int tries = 0; placed < 14 && tries < 4000; tries++)
        {
            Vector2 q = new Vector2(R(Walk.xMin + 2f, Walk.xMax - 2f), R(Walk.yMin + 2f, Walk.yMax - 2f));
            if (!Free(q, 2.2f) || InWater(q) || !Flat(q, 1.6f, 0f)) continue;
            if (q.y < Walk.yMin + 12f) continue;   // cerca del borde sur taparía la cámara
            if (rng.NextDouble() < 0.55) Tree(g, q, 0f, R(0.9f, 1.2f), true); else Pine(g, q, 0f, R(0.9f, 1.2f), true);
            blocked.Add(new Rect(q.x - 1.6f, q.y - 1.6f, 3.2f, 3.2f));
            placed++;
        }
        // bosque en los bancales y alrededor (fuera de la zona jugable)
        bool big = layout >= Layout.Lago;   // mapas largos: el bosque cubre todo el terreno, también dentro (lejos del sendero)
        var ta = TerrainArea;
        for (int i = 0, tries = 0; i < (big ? 520 : 230) && tries < (big ? 20000 : 9000); tries++)
        {
            Vector2 q = big ? new Vector2(R(ta.xMin + 4f, ta.xMax - 4f), R(ta.yMin + 4f, ta.yMax - 4f)) : new Vector2(R(-85f, 85f), R(-58f, 95f));
            if (big) { if (Walk.Contains(q) && (!Free(q, 2.6f) || ShadowsTrail(q))) continue; }
            else if (Walk.Contains(q) && q.y < 36f) continue;
            if (q.y < Walk.yMin && q.y > Walk.yMin - 30f && rng.NextDouble() < 0.9) continue;   // al sur está la cámara: no se le pone bosque enfrente
            float h = Height(q.x, q.y);
            if (Underwater(h) || InWater(q) || !Flat(q, 1.2f, h)) continue;
            if (!Free(q, 1f)) continue;
            float s = R(0.95f, 1.6f) * (h < -5f ? 1.3f : 1f);
            if (rng.NextDouble() < 0.6 || h >= SnowAbove) Pine(g, q, h, s, false); else Tree(g, q, h, s, false);   // en la nieve, solo pinos
            i++;
        }
        // arbustos en las orillas de los caminos y al pie de los bancales
        for (int i = 0, tries = 0; i < 90 && tries < 4000; tries++)
        {
            Vector2 q = layout >= Layout.Lago ? new Vector2(R(Walk.xMin, Walk.xMax), R(Walk.yMin, Walk.yMax)) : new Vector2(R(-40f, 40f), R(-40f, 45f));
            float h = Height(q.x, q.y);
            if (Underwater(h) || InWater(q) || !Free(q, 0.3f) || !Flat(q, 0.8f, h)) continue;
            if (layout >= Layout.Lago && ShadowsTrail(q, 6f)) continue;
            if (q.y < Walk.yMin + 3f) continue;   // detrás del borde sur quedarían pegados a la cámara
            Bush(g, new Vector3(q.x, h, q.y), R(0.7f, 1.3f));
            i++;
        }
    }

    /// <summary>¿Esa altura queda bajo el agua? (en el puerto, todo lo que está bajo el nivel del mar)</summary>
    bool Underwater(float h)
    {
        switch (layout)
        {
            case Layout.Pueblo: return h < -4.4f;
            case Layout.Cueva: return h < -0.6f || h > 8f;   // ni en el lago ni en lo alto de las paredes
            case Layout.Cima: return h < -10f;
            case Layout.Templo: case Layout.Montana: return false;
            case Layout.Lago: return h < -0.5f;
            case Layout.Cascadas: return false;   // el cauce lo marca InWater
            default: return h < -0.3f;
        }
    }

    /// <summary>¿El suelo es parejo en un radio (todo al mismo nivel)?</summary>
    bool Flat(Vector2 q, float r, float h)
    {
        foreach (var o in new[] { Vector2.zero, new Vector2(r, 0f), new Vector2(-r, 0f), new Vector2(0f, r), new Vector2(0f, -r) })
            if (Mathf.Abs(Height(q.x + o.x, q.y + o.y) - h) > 0.05f) return false;
        return true;
    }

    /// <summary>Árbol de hoja: tronco que se adelgaza con dos ramas y una copa de 5 a 8 bultos irregulares.</summary>
    void Tree(Transform parent, Vector2 q, float y, float s, bool collider)
    {
        if (layout == Layout.Nevado) { Pine(parent, q, y, s, collider); return; }   // en la nieve solo pinos
        EnsurePlantMeshes();
        var t = Group("Arbol", parent);
        t.localPosition = new Vector3(q.x, y, q.y);
        t.localRotation = Quaternion.Euler(R(-3f, 3f), R(0f, 360f), R(-3f, 3f));
        t.localScale = Vector3.one * s;
        var trunk = MeshObject(t, "Tronco", trunkTaper, mBarkPix, true);
        trunk.transform.localScale = new Vector3(0.42f, 2.6f, 0.42f);
        for (int b = 0; b < 2; b++)
        {
            var br = MeshObject(t, "Rama", trunkTaper, mBarkPix, true);
            br.transform.localPosition = new Vector3(0f, 1.7f + b * 0.4f, 0f);
            br.transform.localRotation = Quaternion.Euler(R(35f, 50f), b * 180f + R(-30f, 30f), 0f);
            br.transform.localScale = new Vector3(0.18f, 1.3f, 0.18f);
        }
        var leaf = rng.NextDouble() < 0.18 ? mLeavesAutumn : mLeavesPix;
        int blobs = rng.Next(5, 9);
        for (int k = 0; k < blobs; k++)
        {
            float a = k / (float)blobs * Mathf.PI * 2f + R(-0.3f, 0.3f);
            float ring = k == 0 ? 0f : R(0.6f, 1.2f);
            var c = MeshObject(t, "Copa", blobMeshes[rng.Next(blobMeshes.Length)], leaf, true);
            float w = k == 0 ? R(2.2f, 2.6f) : R(1.3f, 1.9f);
            c.transform.localPosition = new Vector3(Mathf.Cos(a) * ring, 3f + (k == 0 ? 0.7f : R(-0.3f, 0.6f)), Mathf.Sin(a) * ring);
            c.transform.localRotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
            c.transform.localScale = new Vector3(w, w * R(0.72f, 0.88f), w);
        }
        if (collider) { var cap = t.gameObject.AddComponent<CapsuleCollider>(); cap.radius = 0.4f; cap.height = 3f; cap.center = new Vector3(0f, 1.5f, 0f); }
    }

    /// <summary>Pino: tronco y 4-5 faldas de ramas en picos que cuelgan, cada vez más chicas.</summary>
    void Pine(Transform parent, Vector2 q, float y, float s, bool collider)
    {
        EnsurePlantMeshes();
        var t = Group("Pino", parent);
        t.localPosition = new Vector3(q.x, y, q.y);
        t.localRotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
        t.localScale = Vector3.one * s;
        var trunk = MeshObject(t, "Tronco", trunkTaper, mBarkPix, true);
        trunk.transform.localScale = new Vector3(0.32f, 2.2f, 0.32f);
        int tiers = rng.Next(4, 6);
        for (int k = 0; k < tiers; k++)
        {
            var c = MeshObject(t, "Falda", skirtMeshes[rng.Next(skirtMeshes.Length)], mPinePix, true);
            float r = 1.7f - k * (1.25f / tiers);
            c.transform.localPosition = new Vector3(0f, 1f + k * 0.85f, 0f);
            c.transform.localRotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
            c.transform.localScale = new Vector3(r, 1.35f, r);
        }
        if (collider) { var cap = t.gameObject.AddComponent<CapsuleCollider>(); cap.radius = 0.4f; cap.height = 3f; cap.center = new Vector3(0f, 1.5f, 0f); }
    }

    void Bush(Transform parent, Vector3 pos, float s)
    {
        EnsurePlantMeshes();
        var b = Group("Arbusto", parent);
        b.localPosition = pos;
        int n = rng.Next(2, 4);
        for (int k = 0; k < n; k++)
        {
            var c = MeshObject(b, "Bulto", blobMeshes[rng.Next(blobMeshes.Length)], rng.NextDouble() < 0.15 ? mLeavesAutumn : mLeavesPix, false);
            c.transform.localPosition = new Vector3(R(-0.5f, 0.5f) * s, 0.35f * s, R(-0.5f, 0.5f) * s);
            c.transform.localScale = new Vector3(s, s * 0.7f, s) * R(0.8f, 1.1f);
        }
    }

    void EnsurePlantMeshes()
    {
        if (blobMeshes == null || blobMeshes.Length == 0 || blobMeshes[0] == null)
        {
            blobMeshes = new Mesh[4];
            for (int i = 0; i < blobMeshes.Length; i++) blobMeshes[i] = LumpySphere(2, 0.22f, i * 17 + 3);
        }
        if (skirtMeshes == null || skirtMeshes.Length == 0 || skirtMeshes[0] == null)
        {
            skirtMeshes = new Mesh[3];
            for (int i = 0; i < skirtMeshes.Length; i++) skirtMeshes[i] = Skirt(14 + i * 2, 0.7f, i * 11 + 5);
        }
        if (trunkTaper == null) trunkTaper = Taper(7);
    }

    /// <summary>Esfera facetada (icosaedro subdividido) con bultos: radio 0.5, sombreado plano para que se lea pixel/low-poly.</summary>
    static Mesh LumpySphere(int subdiv, float lump, int seed)
    {
        float t = (1f + Mathf.Sqrt(5f)) / 2f;
        var verts = new List<Vector3>
        {
            new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
            new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
            new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1),
        };
        var faces = new List<int[]>
        {
            new[]{0,11,5}, new[]{0,5,1}, new[]{0,1,7}, new[]{0,7,10}, new[]{0,10,11}, new[]{1,5,9}, new[]{5,11,4}, new[]{11,10,2}, new[]{10,7,6}, new[]{7,1,8},
            new[]{3,9,4}, new[]{3,4,2}, new[]{3,2,6}, new[]{3,6,8}, new[]{3,8,9}, new[]{4,9,5}, new[]{2,4,11}, new[]{6,2,10}, new[]{8,6,7}, new[]{9,8,1},
        };
        for (int s = 0; s < subdiv; s++)
        {
            var cache = new Dictionary<long, int>();
            int Mid(int a, int b)
            {
                long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
                if (cache.TryGetValue(key, out int m)) return m;
                verts.Add((verts[a] + verts[b]) / 2f);
                cache[key] = verts.Count - 1;
                return verts.Count - 1;
            }
            var next = new List<int[]>();
            foreach (var f in faces)
            {
                int a = Mid(f[0], f[1]), b = Mid(f[1], f[2]), c = Mid(f[2], f[0]);
                next.Add(new[] { f[0], a, c }); next.Add(new[] { f[1], b, a }); next.Add(new[] { f[2], c, b }); next.Add(new[] { a, b, c });
            }
            faces = next;
        }
        // bultos: desplaza cada vértice según ruido de su dirección (mismo valor en vértices compartidos: sin grietas)
        for (int i = 0; i < verts.Count; i++)
        {
            Vector3 d = verts[i].normalized;
            float k = Noise(d.x * 2.3f + seed, d.z * 2.3f + d.y * 1.7f) - 0.5f;
            float flat = d.y < -0.3f ? 0.75f : 1f;   // abajo más plano
            verts[i] = d * 0.5f * (1f + k * lump * 2f) * new Vector3(1f, flat, 1f).y;
            if (d.y < -0.3f) verts[i] = new Vector3(verts[i].x, verts[i].y * 0.8f, verts[i].z);
        }
        // caras planas: vértices propios por triángulo
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var tri = new List<int>();
        foreach (var f in faces)
        {
            // orden para que la normal apunte hacia afuera
            Vector3 a = verts[f[0]], b = verts[f[1]], c = verts[f[2]];
            if (Vector3.Dot(Vector3.Cross(b - a, c - a), a + b + c) < 0f) { var tmp = b; b = c; c = tmp; }
            int k = v.Count;
            v.Add(a); v.Add(b); v.Add(c);
            uv.Add(new Vector2(a.x + a.y, a.z + a.y) * 1.5f); uv.Add(new Vector2(b.x + b.y, b.z + b.y) * 1.5f); uv.Add(new Vector2(c.x + c.y, c.z + c.y) * 1.5f);
            tri.Add(k); tri.Add(k + 1); tri.Add(k + 2);
        }
        var m = new Mesh { name = "Copa" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(tri, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>Falda de pino: cono abierto con borde en picos que cuelgan (radio 1, alto 1) y una tapa oscura abajo.</summary>
    static Mesh Skirt(int points, float inner, int seed)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        var rnd = new System.Random(seed);
        Vector3 apex = new Vector3(0f, 1f, 0f);
        var rim = new List<Vector3>();
        for (int i = 0; i < points * 2; i++)
        {
            float a = i / (float)(points * 2) * Mathf.PI * 2f;
            bool tip = i % 2 == 0;
            float r = tip ? 1f + (float)rnd.NextDouble() * 0.12f : inner;
            float y = tip ? -0.18f - (float)rnd.NextDouble() * 0.1f : 0.12f;
            rim.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
        }
        for (int i = 0; i < rim.Count; i++)
        {
            Vector3 a = rim[i], b = rim[(i + 1) % rim.Count];
            int k = v.Count;
            // cara de afuera (apex, b, a) y de abajo (a, b, centro bajo)
            v.Add(apex); v.Add(b); v.Add(a);
            uv.Add(new Vector2(0.5f, 1f)); uv.Add(new Vector2(b.x * 0.5f + 0.5f, 0f)); uv.Add(new Vector2(a.x * 0.5f + 0.5f, 0f));
            t.Add(k); t.Add(k + 1); t.Add(k + 2);
            k = v.Count;
            Vector3 under = new Vector3(0f, 0.15f, 0f);
            v.Add(a); v.Add(b); v.Add(under);
            uv.Add(new Vector2(0.1f, 0.1f)); uv.Add(new Vector2(0.2f, 0.1f)); uv.Add(new Vector2(0.15f, 0.2f));
            t.Add(k); t.Add(k + 1); t.Add(k + 2);
        }
        var m = new Mesh { name = "Falda" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    /// <summary>Tronco: cilindro de alto 1 que se adelgaza hacia arriba (base en y = 0, radio 0.5 → 0.3).</summary>
    static Mesh Taper(int segs)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int i = 0; i <= segs; i++)
        {
            float a = i / (float)segs * Mathf.PI * 2f;
            var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            v.Add(d * 0.5f); v.Add(d * 0.3f + Vector3.up);
            uv.Add(new Vector2(i / (float)segs * 2f, 0f)); uv.Add(new Vector2(i / (float)segs * 2f, 2f));
        }
        for (int i = 0; i < segs; i++)
        {
            int k = i * 2;
            t.AddRange(new[] { k, k + 1, k + 3, k, k + 3, k + 2 });
        }
        var m = new Mesh { name = "Tronco" };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return m;
    }

    // ------------------------------------------------------------------ pasto

    void BuildGrass()
    {
        var g = Group("Pasto", root);
        var tuft = TuftMesh();
        var batch = new List<CombineInstance>();
        int chunk = 0;
        int tufts = layout >= Layout.Lago ? grassTufts * 3 : grassTufts;
        for (int i = 0, tries = 0; i < tufts && tries < tufts * 8; tries++)
        {
            Vector2 q = layout >= Layout.Lago ? new Vector2(R(Walk.xMin - 6f, Walk.xMax + 6f), R(Walk.yMin, Walk.yMax + 6f)) : new Vector2(R(-50f, 50f), R(-45f, 60f));
            if (!Free(q, 0f) || (layout == Layout.Pueblo && Terrace.Contains(q))) continue;
            float h = Height(q.x, q.y);
            if (Underwater(h) || InWater(q) || !Flat(q, 0.35f, h)) continue;
            float s = R(0.6f, 1.3f);
            batch.Add(new CombineInstance { mesh = tuft, transform = Matrix4x4.TRS(new Vector3(q.x, h, q.y), Quaternion.Euler(0f, R(0f, 360f), 0f), Vector3.one * s) });
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
}
