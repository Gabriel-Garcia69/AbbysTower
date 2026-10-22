using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Piezas para los mapas largos de la ruta (Lago, Cascadas, Montaña): sendero de tierra que se pega al terreno,
/// regla de "no tapar la cámara" para la vegetación, puentes de tablones a cualquier altura, orillas que no dejan
/// caer al río y miradores.
/// </summary>
public partial class DioramaBuilder
{
    /// <summary>Puntos del sendero principal (x, z) cada ~0.7 m.</summary>
    readonly List<Vector2> trail = new List<Vector2>();

    /// <summary>Sendero de tierra por los puntos dados. La franja sigue la altura del terreno y se corta donde hay
    /// escalón (ahí van escaleras o puentes). Árboles y arbustos no se ponen encima ni entre el sendero y la cámara.</summary>
    void Trail(Transform parent, float width, params Vector2[] pts)
    {
        var piso = PisoSendero();
        for (int i = 0; i + 1 < pts.Length; i++)
        {
            Vector2 a = pts[i], b = pts[i + 1];
            float len = Vector2.Distance(a, b);
            if (len < 0.01f) continue;
            AnotarTramo(piso, a, b, width);   // la malla se arma al final, unida (CaminosMalla)
            int n = Mathf.Max(1, Mathf.CeilToInt(len / 0.7f));
            Vector2 dir = (b - a) / len, right = new Vector2(dir.y, -dir.x) * (width / 2f);
            for (int k = 0; k < n; k++)
            {
                Vector2 p0 = Vector2.Lerp(a, b, k / (float)n), p1 = Vector2.Lerp(a, b, (k + 1) / (float)n);
                trail.Add(p0);
                if (k % 3 == 0) blocked.Add(new Rect(p0.x - width / 2f - 0.6f, p0.y - width / 2f - 0.6f, width + 1.2f, width + 1.2f));
            }
        }
        trail.Add(pts[pts.Length - 1]);
    }

    /// <summary>¿Algo alto en q taparía el sendero desde la cámara? (la cámara ve al norte desde el sur)</summary>
    bool ShadowsTrail(Vector2 q, float reach = 18f)
    {
        for (int i = 0; i < trail.Count; i += 2)
        {
            var p = trail[i];
            float dx = Mathf.Abs(q.x - p.x), dz = p.y - q.y;
            if (dx < 3.6f && Mathf.Abs(dz) < 3.6f) return true;           // encima del sendero
            if (dz > 0f && dz < reach && dx < 5.5f) return true;          // al sur del sendero, en la línea de la cámara
        }
        return false;
    }

    /// <summary>Puente de tablones recto (con barandal y pilotes) a una altura: de a a b, ancho w.</summary>
    void PlankBridge(Transform parent, string name, Vector3 a, Vector3 b, float w)
    {
        var g = Group(name, parent);
        g.localPosition = new Vector3(0f, a.y, 0f);
        bool alongX = Mathf.Abs(b.x - a.x) > Mathf.Abs(b.z - a.z);
        float x0 = Mathf.Min(a.x, b.x), x1 = Mathf.Max(a.x, b.x), z0 = Mathf.Min(a.z, b.z), z1 = Mathf.Max(a.z, b.z);
        if (alongX) { z0 = a.z - w / 2f; z1 = a.z + w / 2f; } else { x0 = a.x - w / 2f; x1 = a.x + w / 2f; }
        Deck(g, x0, x1, z0, z1);
        if (alongX) { Rail(g, new Vector2(x0, z0), new Vector2(x1, z0)); Rail(g, new Vector2(x0, z1), new Vector2(x1, z1)); }
        else { Rail(g, new Vector2(x0, z0), new Vector2(x0, z1)); Rail(g, new Vector2(x1, z0), new Vector2(x1, z1)); }
        blocked.Add(new Rect(x0 - 1f, z0 - 1f, x1 - x0 + 2f, z1 - z0 + 2f));
    }

    /// <summary>Muro invisible a lo largo de una orilla (puntos a la altura del suelo), con huecos donde 'skip' lo pide.</summary>
    void BankGuard(Transform parent, List<Vector3> pts, System.Func<Vector3, bool> skip)
    {
        for (int i = 0; i + 1 < pts.Count; i++)
        {
            Vector3 a = pts[i], b = pts[i + 1];
            if (skip(a) || skip(b)) continue;
            Vector3 c = (a + b) / 2f, d = b - a; d.y = 0f;
            if (d.sqrMagnitude < 0.01f) continue;
            var go = new GameObject("Collider_Orilla");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(c.x, Mathf.Max(a.y, b.y) + 1.2f, c.z);
            go.transform.localRotation = Quaternion.LookRotation(d.normalized);
            go.AddComponent<BoxCollider>().size = new Vector3(0.3f, 2.4f, d.magnitude + 0.3f);
        }
    }

    /// <summary>Juncos y piedras en una orilla (decoran donde está el muro invisible).</summary>
    void Reeds(Transform parent, Vector3 pos)
    {
        var g = Group("Juncos", parent);
        g.localPosition = pos;
        for (int i = 0; i < rng.Next(4, 8); i++)
        {
            float h = R(0.6f, 1.3f);
            var r = Box(g, "Junco", new Vector3(R(-0.5f, 0.5f), h / 2f, R(-0.5f, 0.5f)), new Vector3(0.06f, h, 0.06f), mLeavesPix, false);
            r.transform.localRotation = Quaternion.Euler(R(-12f, 12f), 0f, R(-12f, 12f));
        }
        if (rng.NextDouble() < 0.5)
            Prim(g, PrimitiveType.Sphere, "Piedra", new Vector3(R(-0.6f, 0.6f), 0.1f, R(-0.6f, 0.6f)), new Vector3(R(0.5f, 0.9f), R(0.3f, 0.5f), R(0.5f, 0.8f)), mRock);
    }

    /// <summary>Agua de los mapas nuevos: dónde no van plantas.</summary>
    bool NewWorldWater(Vector2 q)
    {
        switch (layout)
        {
            case Layout.Lago: return LakeHeight(q.x, q.y) < -0.4f;
            case Layout.Cascadas: return FallsInRiver(q.x, q.y);
            default: return false;
        }
    }

    /// <summary>¿Esa celda es lecho (grava del río) aunque esté arriba? (cauce del valle de las cascadas)</summary>
    bool IsBedAt(float x, float z, float h)
    {
        return layout == Layout.Cascadas && FallsInRiver(x, z);
    }

    /// <summary>Montañas lejanas (bultos facetados) en un arco, con cumbre nevada opcional.</summary>
    void FarPeaks(Transform parent, Vector3 center, float radius, float a0, float a1, int count, float hMin, float hMax, bool snow)
    {
        EnsurePlantMeshes();
        var rockMat = Saved("Dio_MontanaLejos", () => { var m = new Material(mRock); m.SetColor("_BaseColor", new Color(0.62f, 0.66f, 0.78f)); m.mainTextureScale = new Vector2(6f, 6f); return m; });
        var snowMat = Saved("Dio_MontanaNevada", () => { var m = new Material(mSnowPix); m.SetColor("_BaseColor", new Color(0.72f, 0.8f, 1f)); m.mainTextureScale = new Vector2(4f, 4f); return m; });
        for (int i = 0; i < count; i++)
        {
            float a = Mathf.Lerp(a0, a1, (i + R(0.2f, 0.8f)) / count) * Mathf.Deg2Rad;
            float r = radius * R(0.85f, 1.15f), h = R(hMin, hMax), w = R(50f, 90f);
            var p = center + new Vector3(Mathf.Sin(a) * r, 0f, Mathf.Cos(a) * r);
            var m = MeshObject(parent, "Montana", blobMeshes[rng.Next(blobMeshes.Length)], rockMat, false);
            m.transform.localPosition = new Vector3(p.x, p.y - h * 0.15f, p.z);
            m.transform.localScale = new Vector3(w * 2f, h, w * 1.6f);
            if (snow)
            {
                var c = MeshObject(parent, "Nieve", blobMeshes[rng.Next(blobMeshes.Length)], snowMat, false);
                c.transform.localPosition = new Vector3(p.x, p.y - h * 0.15f + h * 0.55f, p.z);
                c.transform.localScale = new Vector3(w * 0.95f, h * 0.5f, w * 0.75f);
            }
        }
    }
}
