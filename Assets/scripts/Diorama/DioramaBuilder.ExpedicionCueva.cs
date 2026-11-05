using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// La roca de la gruta del tutorial sin formas rectas: paredes, techo y fondo del abismo en mallas con ruido, y rocas y
/// columnas de contorno irregular. El suelo se queda plano para que las runas y el sendero se lean y nada estorbe al pelear.
/// </summary>
public partial class DioramaBuilder
{
    /// <summary>Altura del techo: una bóveda irregular, nunca tan baja como para tapar la cámara de juego.</summary>
    static float ExpTechoEn(float x, float z) =>
        ExpTecho + 4f - Noise(x * 0.035f + 2f, z * 0.035f + 6f) * 10f - Noise(x * 0.12f, z * 0.12f + 3f) * 4f - Noise(x * 0.4f + 5f, z * 0.4f) * 1.2f;

    /// <summary>Malla de roca a partir de una rejilla de puntos. La cara mira hacia 'dentro'; el UV va en metros según el eje que domina.</summary>
    GameObject MallaRoca(Transform parent, string nombre, int nu, int nv, System.Func<int, int, Vector3> punto, Vector3 dentro, Material mat, bool techo)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int i = 0; i <= nu; i++)
            for (int j = 0; j <= nv; j++)
            {
                var p = punto(i, j);
                v.Add(p);
                uv.Add((techo ? new Vector2(p.x, p.z) : new Vector2(p.x + p.z, p.y)) * 0.25f);
            }
        int Id(int i, int j) => i * (nv + 1) + j;
        for (int i = 0; i < nu; i++)
            for (int j = 0; j < nv; j++)
            {
                t.Add(Id(i, j)); t.Add(Id(i, j + 1)); t.Add(Id(i + 1, j + 1));
                t.Add(Id(i, j)); t.Add(Id(i + 1, j + 1)); t.Add(Id(i + 1, j));
            }
        // el sentido de las caras depende de la rejilla: se revisa con una de ellas
        Vector3 n0 = Vector3.Cross(v[t[1]] - v[t[0]], v[t[2]] - v[t[0]]);
        if (Vector3.Dot(n0, dentro) < 0f)
            for (int k = 0; k < t.Count; k += 3) { int q = t[k + 1]; t[k + 1] = t[k + 2]; t[k + 2] = q; }
        var m = new Mesh { name = nombre, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
        return MeshObject(parent, nombre, m, mat, false);
    }

    /// <summary>La bóveda y las paredes del abismo.</summary>
    void BovedaOrganica(Transform g)
    {
        const float paso = 2.5f, x0 = -62f, z0 = -114f;
        float z1 = ExpEdgeZ + 54f;
        MallaRoca(g, "TechoGruta", Mathf.CeilToInt(124f / paso), Mathf.CeilToInt((z1 - z0) / paso),
            (i, j) => { float x = x0 + i * paso, z = z0 + j * paso; return new Vector3(x, ExpTechoEn(x, z), z); }, Vector3.down, mExpPared, true);
        // el fondo del abismo, detrás de la criatura
        MallaRoca(g, "FondoAbismo", 48, 34, (i, j) =>
        {
            float x = -60f + i * paso, y = -48f + j * paso;
            float hondo = Noise(x * 0.05f + 3f, y * 0.05f) * 12f + Noise(x * 0.17f, y * 0.17f + 4f) * 4f + Noise(x * 0.5f + 2f, y * 0.5f) * 1.2f;
            return new Vector3(x, y, ExpEdgeZ + 54f - hondo);
        }, Vector3.back, mExpFondo, false);
        // los costados del abismo, hasta unirse con las paredes de arriba
        foreach (float sx in new[] { -1f, 1f })
            MallaRoca(g, "CostadoAbismo", 24, 22, (i, j) =>
            {
                float z = ExpEdgeZ - 4f + i * paso, y = -48f + j * paso;
                float hondo = Noise(z * 0.06f + sx * 3f, y * 0.06f) * 8f + Noise(z * 0.2f, y * 0.2f + sx) * 3f;
                return new Vector3(sx * (20f + hondo), y, z);
            }, new Vector3(-sx, 0f, 0f), mExpFondo, false);
    }

    /// <summary>Roca de contorno irregular: anillos con ruido. 'perfil' da el radio relativo según la altura (0 abajo, 1 arriba).</summary>
    Mesh RocaIrregular(float radio, float alto, System.Func<float, float> perfil, int segs = 11, int anillos = 7)
    {
        float semilla = R(0f, 50f);
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        for (int a = 0; a <= anillos; a++)
        {
            float k = a / (float)anillos;
            for (int s = 0; s <= segs; s++)
            {
                float ang = (s % segs) / (float)segs * Mathf.PI * 2f;
                float c = Mathf.Cos(ang), n = Mathf.Sin(ang);
                float ruido = 1f + (Noise(c * 1.1f + semilla, n * 1.1f + k * 2.2f) - 0.5f) * 0.9f;
                float r = radio * perfil(k) * ruido;
                v.Add(new Vector3(c * r, k * alto + (Noise(c + semilla, n) - 0.5f) * alto * 0.05f, n * r));
                uv.Add(new Vector2(s / (float)segs * radio * 6.28f, k * alto) * 0.25f);
            }
        }
        for (int a = 0; a < anillos; a++)
            for (int s = 0; s < segs; s++)
            {
                int i0 = a * (segs + 1) + s, i1 = i0 + 1, i2 = i0 + segs + 1, i3 = i2 + 1;
                t.AddRange(new[] { i0, i2, i3, i0, i3, i1 });
            }
        var m = new Mesh { name = "Roca", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        m.SetVertices(v); m.SetUVs(0, uv); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateTangents(); m.RecalculateBounds();
        return m;
    }

    static float PerfilPico(float k) => Mathf.Pow(1f - k, 0.85f) + 0.04f;
    static float PerfilPenasco(float k) => Mathf.Sqrt(Mathf.Max(0.02f, 1f - k * k));
    static float PerfilColumna(float k) { float d = Mathf.Abs(2f * k - 1f); return 0.72f + 0.9f * d * d * d; }

    /// <summary>Estalagmita: un pico de roca que sale del suelo.</summary>
    void PilaDeRoca(Transform parent, Vector3 pos, float alto)
    {
        var o = MeshObject(parent, "Estalagmita", RocaIrregular(alto * 0.3f + 0.4f, alto, PerfilPico), rng.NextDouble() < 0.5 ? mExpPared : mExpParedAlt, true);
        o.transform.localPosition = pos + Vector3.down * 0.15f;
        o.transform.localRotation = Quaternion.Euler(R(-5f, 5f), R(0f, 360f), R(-5f, 5f));
        MarcarOcluible(o.GetComponent<MeshRenderer>());
    }

    /// <summary>Peñasco redondeado, medio enterrado.</summary>
    void Penasco(Transform parent, Vector3 pos, float radio)
    {
        var o = MeshObject(parent, "Penasco", RocaIrregular(radio, radio * R(0.7f, 1.05f), PerfilPenasco), rng.NextDouble() < 0.5 ? mExpPared : mExpParedAlt, true);
        o.transform.localPosition = pos + Vector3.down * radio * 0.1f;
        o.transform.localRotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
    }

    /// <summary>Peñascos al pie de la pared y en las orillas del escalón (salvo donde pasa el sendero): rompen las líneas rectas.</summary>
    void RocasDelSuelo(Transform g)
    {
        var rocas = Group("Penascos", g);
        foreach (float z in new[] { ExpLedgeZ0, ExpLedgeZ1 })
            for (float x = -30f; x <= 30f; x += R(2f, 3.6f))
                if (Mathf.Abs(x - SenderoX(z)) > 4.5f) Penasco(rocas, new Vector3(x, 0f, z + R(-0.8f, 0.8f)), R(0.8f, 1.7f));
        for (int i = 0; i < 70; i++)
        {
            float z = R(-76f, 124f), x = (rng.NextDouble() < 0.5 ? -1f : 1f) * (ExpMitad(z) - R(-1f, 5f));
            if (CercaDelSendero(x, z, 5f)) continue;
            Penasco(rocas, new Vector3(x, 0f, z), R(0.7f, 2.6f));
        }
        Fundir(rocas);
    }
}
