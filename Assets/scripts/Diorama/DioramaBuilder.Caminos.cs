using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Caminos sin parpadeo: antes cada tramo era su propia franja o rectángulo a la misma altura y donde se cruzaban o
/// daban vuelta había dos superficies encimadas (z-fighting) o huecos. Ahora los tramos solo se anotan y al final se arma
/// UNA malla por material con "marching squares" sobre una rejilla: la unión de todos los tramos, con la orilla
/// suave (y un poco irregular en los de tierra) y pegada al terreno. Cada mapa tiene su propio piso.
/// </summary>
public partial class DioramaBuilder
{
    struct TramoCamino { public Vector2 a, b; public float ancho; public bool rect; public Vector2 tam; }

    readonly Dictionary<Material, List<TramoCamino>> tramos = new Dictionary<Material, List<TramoCamino>>();
    Material mGrava, mTierraOscura;

    void AnotarCamino(Material mat, TramoCamino t)
    {
        if (!tramos.TryGetValue(mat, out var l)) tramos[mat] = l = new List<TramoCamino>();
        l.Add(t);
    }

    void AnotarTramo(Material mat, Vector2 a, Vector2 b, float ancho) => AnotarCamino(mat, new TramoCamino { a = a, b = b, ancho = ancho });
    void AnotarPlaza(Material mat, Vector2 centro, Vector2 tam) => AnotarCamino(mat, new TramoCamino { a = centro, tam = tam, rect = true });

    /// <summary>El piso de los senderos de tierra según el mapa (los caminos también son distintos por piso).</summary>
    Material PisoSendero()
    {
        if (mGrava == null)
        {
            mGrava = Saved("Dio_Grava", () => { var m = new Material(mDirt); m.SetColor("_BaseColor", new Color(1.12f, 1.08f, 1f)); return m; });
            mTierraOscura = Saved("Dio_TierraOscura", () => { var m = new Material(mDirt); m.SetColor("_BaseColor", new Color(0.7f, 0.64f, 0.62f)); return m; });
        }
        switch (layout)
        {
            case Layout.Cascadas: case Layout.Lago: return mGrava;
            case Layout.Expedicion: case Layout.Cueva: return mTierraOscura;
            default: return mDirt;
        }
    }

    /// <summary>Valor del campo en un punto: &gt; 0 adentro del camino (metros hasta la orilla).</summary>
    static float Campo(List<TramoCamino> l, float x, float z)
    {
        float best = -999f;
        var p = new Vector2(x, z);
        foreach (var t in l)
        {
            float f;
            if (t.rect)
            {
                Vector2 d = new Vector2(Mathf.Abs(x - t.a.x) - t.tam.x / 2f, Mathf.Abs(z - t.a.y) - t.tam.y / 2f);
                f = -Mathf.Max(d.x, d.y);
            }
            else
            {
                Vector2 ab = t.b - t.a;
                float k = ab.sqrMagnitude < 1e-4f ? 0f : Mathf.Clamp01(Vector2.Dot(p - t.a, ab) / ab.sqrMagnitude);
                f = t.ancho / 2f - Vector2.Distance(p, t.a + ab * k);
            }
            if (f > best) best = f;
        }
        return best;
    }

    void CaminosMalla()
    {
        if (root == null || tramos.Count == 0) return;
        var g = Group("CaminosUnidos", root);
        const float celda = 0.35f;
        foreach (var kv in tramos)
        {
            var l = kv.Value;
            bool tierra = kv.Key != mCobble && kv.Key != mPlaza;
            // límites
            float x0 = 1e9f, x1 = -1e9f, z0 = 1e9f, z1 = -1e9f;
            foreach (var t in l)
            {
                Vector2 e = t.rect ? t.tam / 2f : Vector2.one * t.ancho / 2f;
                Vector2 lo = t.rect ? t.a - e : Vector2.Min(t.a, t.b) - e, hi = t.rect ? t.a + e : Vector2.Max(t.a, t.b) + e;
                x0 = Mathf.Min(x0, lo.x); z0 = Mathf.Min(z0, lo.y); x1 = Mathf.Max(x1, hi.x); z1 = Mathf.Max(z1, hi.y);
            }
            x0 -= 1f; z0 -= 1f; x1 += 1f; z1 += 1f;
            int nx = Mathf.CeilToInt((x1 - x0) / celda) + 1, nz = Mathf.CeilToInt((z1 - z0) / celda) + 1;
            var f = new float[nx, nz]; var hgt = new float[nx, nz];
            for (int i = 0; i < nx; i++)
                for (int j = 0; j < nz; j++)
                {
                    float x = x0 + i * celda, z = z0 + j * celda;
                    float v = Campo(l, x, z);
                    if (tierra) v += (Mathf.PerlinNoise(x * 0.45f + 11f, z * 0.45f + 7f) - 0.5f) * 0.55f;   // orilla irregular
                    f[i, j] = v;
                    hgt[i, j] = v > -0.6f ? Height(x, z) : 0f;
                }
            var verts = new List<Vector3>(); var uvs = new List<Vector2>(); var tris = new List<int>();
            var poly = new List<Vector2>(8); var polyH = new List<float>(8);
            for (int i = 0; i + 1 < nx; i++)
                for (int j = 0; j + 1 < nz; j++)
                {
                    float a = f[i, j], b = f[i + 1, j], c = f[i + 1, j + 1], d = f[i, j + 1];
                    if (a <= 0f && b <= 0f && c <= 0f && d <= 0f) continue;
                    float ha = hgt[i, j], hb = hgt[i + 1, j], hc = hgt[i + 1, j + 1], hd = hgt[i, j + 1];
                    if (Mathf.Max(Mathf.Max(ha, hb), Mathf.Max(hc, hd)) - Mathf.Min(Mathf.Min(ha, hb), Mathf.Min(hc, hd)) > 0.08f) continue;   // escalón o escalera
                    // recorta el cuadro a la parte de adentro (campo > 0), interpolando sobre las aristas
                    poly.Clear(); polyH.Clear();
                    float cx = x0 + i * celda, cz = z0 + j * celda;
                    Vector2[] q = { new Vector2(cx, cz), new Vector2(cx + celda, cz), new Vector2(cx + celda, cz + celda), new Vector2(cx, cz + celda) };
                    float[] fv = { a, b, c, d }, hv = { ha, hb, hc, hd };
                    for (int k = 0; k < 4; k++)
                    {
                        int k2 = (k + 1) % 4;
                        if (fv[k] > 0f) { poly.Add(q[k]); polyH.Add(hv[k]); }
                        if ((fv[k] > 0f) != (fv[k2] > 0f))
                        {
                            float s = fv[k] / (fv[k] - fv[k2]);
                            poly.Add(Vector2.Lerp(q[k], q[k2], s)); polyH.Add(Mathf.Lerp(hv[k], hv[k2], s));
                        }
                    }
                    if (poly.Count < 3) continue;
                    int b0 = verts.Count;
                    for (int k = 0; k < poly.Count; k++)
                    {
                        verts.Add(new Vector3(poly[k].x, polyH[k] + 0.02f, poly[k].y));
                        uvs.Add(poly[k] * 0.5f);
                    }
                    for (int k = 1; k + 1 < poly.Count; k++) tris.AddRange(new[] { b0, b0 + k + 1, b0 + k });
                }
            if (tris.Count == 0) continue;
            var m = new Mesh { name = "Caminos_" + kv.Key.name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            m.SetVertices(verts); m.SetUVs(0, uvs); m.SetTriangles(tris, 0);
            var n = new List<Vector3>(); for (int k = 0; k < verts.Count; k++) n.Add(Vector3.up);
            m.SetNormals(n); m.RecalculateTangents(); m.RecalculateBounds();
            var go = MeshObject(g, "Camino_" + kv.Key.name, m, kv.Key, false);
            go.GetComponent<MeshRenderer>().receiveShadows = true;
        }
        tramos.Clear();
    }
}
