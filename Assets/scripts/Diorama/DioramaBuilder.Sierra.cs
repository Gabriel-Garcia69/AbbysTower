using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lo de afuera de los mapas de valle (Lago, Cascadas, Montaña): una sierra continua en lugar de llanos planos a distintas
/// alturas. Los llanos dejaban cortes rectos de kilómetros entre ellos (se veían como dos paredes gigantes al fondo del
/// valle). Aquí las laderas bajan suaves hacia el valle de enfrente y de atrás, y los montes crecen al alejarse hasta un
/// filo alto que tapa el horizonte. En la orilla del mapa coincide con la altura de sus paredes.
/// </summary>
public partial class DioramaBuilder
{
    Rect sierraA;
    float sierraLado, sierraNorte, sierraSur, sierraBajadaSur = 60f, sierraBajadaNorte = 60f;

    public float SierraH(float x, float z)
    {
        var a = sierraA;
        float dx = Mathf.Max(0f, Mathf.Max(a.xMin - x, x - a.xMax));
        float dS = a.yMin - z, dN = z - a.yMax;
        // la base: altura de las paredes a los lados; enfrente y atrás, el valle (bajando en ladera desde los lados)
        float lado = sierraLado;
        if (dS > 0f) lado = Mathf.Lerp(sierraLado, sierraSur, Mathf.SmoothStep(0f, 1f, dS / sierraBajadaSur));
        if (dN > 0f) lado = Mathf.Lerp(sierraLado, sierraNorte, Mathf.SmoothStep(0f, 1f, dN / sierraBajadaNorte));
        float valle = dS > 0f ? sierraSur : dN > 0f ? sierraNorte : sierraLado;
        float h = Mathf.Lerp(valle, lado, Mathf.SmoothStep(0f, 1f, dx / 30f));
        // pegada a la orilla del mapa arranca a la altura real del terreno ahí (sin huecos ni escalones), y se va mezclando
        float cx = Mathf.Clamp(x, a.xMin + 0.5f, a.xMax - 0.5f), cz = Mathf.Clamp(z, a.yMin + 0.5f, a.yMax - 0.5f);
        float borde = Mathf.Sqrt((x - cx) * (x - cx) + (z - cz) * (z - cz));
        h = Mathf.Lerp(Height(cx, cz), h, Mathf.SmoothStep(0f, 1f, borde / 36f));
        // montes: crecen al alejarse a los lados (y se apagan en los valles de enfrente y atrás), y un filo lejano alrededor
        float n = Noise(x * 0.008f + 11f, z * 0.008f), n2 = Noise(x * 0.03f + 2f, z * 0.03f);
        float valles = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Max(dS, dN) / 70f);
        float monte = Mathf.SmoothStep(0f, 1f, (dx - 12f) / 320f) * (60f + 90f * n) * valles;
        float filo = Mathf.SmoothStep(0f, 1f, (Mathf.Max(dS, dN) - 330f) / 260f) * (70f + 80f * n);
        float detalle = (n2 - 0.5f) * 5f * Mathf.SmoothStep(0f, 1f, (dx - 20f) / 60f);   // los valles quedan planos (lago, árboles, casas)
        return h + Mathf.Max(monte, filo) + detalle;
    }

    void Sierra(Transform parent, Rect area, float lado, float norte, float sur, int arboles, bool pinos)
    {
        sierraA = area; sierraLado = lado; sierraNorte = norte; sierraSur = sur;
        const float cell = 12f;   // de lejos no se nota y la escena pesa la mitad
        float x0 = area.xMin - 456f, x1 = area.xMax + 456f, z0 = area.yMin - 552f, z1 = area.yMax + 552f;
        int nx = Mathf.CeilToInt((x1 - x0) / cell), nz = Mathf.CeilToInt((z1 - z0) / cell);
        var hh = new float[nx + 1, nz + 1];
        for (int i = 0; i <= nx; i++) for (int j = 0; j <= nz; j++) hh[i, j] = SierraH(x0 + i * cell, z0 + j * cell);
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var pasto = new List<int>(); var roca = new List<int>();
        var dentro = new Rect(area.xMin + 1f, area.yMin + 1f, area.width - 2f, area.height - 2f);
        for (int i = 0; i < nx; i++)
            for (int j = 0; j < nz; j++)
            {
                float xa = x0 + i * cell, za = z0 + j * cell;
                if (dentro.Contains(new Vector2(xa, za)) && dentro.Contains(new Vector2(xa + cell, za + cell))) continue;   // ahí va el terreno del mapa
                var p00 = new Vector3(xa, hh[i, j], za); var p10 = new Vector3(xa + cell, hh[i + 1, j], za);
                var p01 = new Vector3(xa, hh[i, j + 1], za + cell); var p11 = new Vector3(xa + cell, hh[i + 1, j + 1], za + cell);
                SierraTri(v, uv, pasto, roca, p00, p01, p11);
                SierraTri(v, uv, pasto, roca, p00, p11, p10);
            }
        var m = new Mesh { name = "Sierra", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(v); m.SetUVs(0, uv);
        m.subMeshCount = 2; m.SetTriangles(pasto, 0); m.SetTriangles(roca, 1);
        m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
        var go = MeshObject(parent, "Sierra", m, TopMat, true);
        go.GetComponent<MeshRenderer>().sharedMaterials = new[] { TopMat, WallMat };
        // árboles en lo plano de la sierra, cerca (lejos ya no se distinguen y solo cuestan)
        for (int i = 0, tries = 0; i < arboles && tries < arboles * 25; tries++)
        {
            var q = new Vector2(R(area.xMin - 260f, area.xMax + 260f), R(area.yMin - 260f, area.yMax + 260f));
            if (area.Contains(q) || (q.x > area.xMin - 4f && q.x < area.xMax + 4f)) continue;   // solo a los lados del valle
            float h = SierraH(q.x, q.y);
            if (Mathf.Abs(SierraH(q.x + 3f, q.y) - h) > 1.6f || Mathf.Abs(SierraH(q.x, q.y + 3f) - h) > 1.6f) continue;
            if (pinos || rng.NextDouble() < 0.5) Pine(parent, q, h - 0.15f, R(1.1f, 1.9f), false); else Tree(parent, q, h - 0.15f, R(1.1f, 1.6f), false);
            i++;
        }
    }

    static void SierraTri(List<Vector3> v, List<Vector2> uv, List<int> pasto, List<int> roca, Vector3 a, Vector3 b, Vector3 c)
    {
        var n = Vector3.Cross(b - a, c - a).normalized;
        bool plano = n.y > 0.78f;
        int k = v.Count;
        foreach (var p in new[] { a, b, c })
        {
            v.Add(p);
            uv.Add(plano ? new Vector2(p.x, p.z) * 0.25f : Mathf.Abs(n.x) > Mathf.Abs(n.z) ? new Vector2(p.z, p.y) * 0.25f : new Vector2(p.x, p.y) * 0.25f);
        }
        var lista = plano ? pasto : roca;
        lista.Add(k); lista.Add(k + 1); lista.Add(k + 2);
    }
}
