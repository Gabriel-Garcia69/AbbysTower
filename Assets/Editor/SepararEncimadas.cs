using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Dos objetos con caras en el mismo plano y encimadas parpadean en pantalla (z-fighting). Esto las busca en la escena abierta y
/// separa unos milímetros la más chica. Hay que correrlo otra vez si se regenera un mapa.
/// </summary>
public static class SepararEncimadas
{
    const float Tolerancia = 0.012f, Separacion = 0.025f;

    struct Cara { public MeshRenderer r; public string mat; public int clave; public float d, a0, a1, b0, b1; }

    static readonly string[] Terreno = { "Terreno", "Falda", "Meseta", "Afuera", "Costa", "RocaGruta", "Tierra", "Fondo", "Alrededores" };

    static bool EsTerreno(Transform t)
    {
        foreach (var n in Terreno) if (t.name.StartsWith(n)) return true;
        return false;
    }

    [MenuItem("Abby/Separar superficies encimadas")]
    public static void EscenaAbierta()
    {
        int total = 0;
        for (int pasada = 0; pasada < 3; pasada++)
        {
            int n = Pasada();
            total += n;
            if (n == 0) break;
        }
        var esc = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(esc);
        EditorSceneManager.SaveScene(esc);
        Debug.Log($"[TourRevision] {esc.name}: {total} objetos separados, quedan {Contar()}");
    }

    public static void Todos(string[] escenas)
    {
        foreach (var n in escenas)
        {
            EditorSceneManager.OpenScene($"Assets/Scenes/{n}.unity");
            EscenaAbierta();
        }
    }

    static List<Cara> Caras()
    {
        var caras = new List<Cara>();
        foreach (var mr in Object.FindObjectsByType<MeshRenderer>())
        {
            var mf = mr.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null || !mr.enabled || !mr.gameObject.activeInHierarchy || EsTerreno(mr.transform)) continue;
            var mesh = mf.sharedMesh;
            if (mesh.vertexCount > 3000 || !mesh.isReadable || mr.sharedMaterial == null) continue;
            var v = mesh.vertices; var t = mesh.triangles; var tr = mr.transform.localToWorldMatrix;
            var porPlano = new Dictionary<(int, int), Cara>();
            for (int i = 0; i < t.Length; i += 3)
            {
                Vector3 a = tr.MultiplyPoint3x4(v[t[i]]), b = tr.MultiplyPoint3x4(v[t[i + 1]]), c = tr.MultiplyPoint3x4(v[t[i + 2]]);
                var nor = Vector3.Cross(b - a, c - a);
                float ar = nor.magnitude;
                if (ar < 0.01f) continue;
                nor /= ar;
                int eje = Mathf.Abs(nor.y) > 0.999f ? 1 : Mathf.Abs(nor.x) > 0.999f ? 0 : Mathf.Abs(nor.z) > 0.999f ? 2 : -1;
                if (eje < 0) continue;
                int e1 = (eje + 1) % 3, e2 = (eje + 2) % 3, clave = (eje + 1) * (nor[eje] > 0f ? 1 : -1);
                var key = (clave, Mathf.RoundToInt(a[eje] * 200f));
                float a0 = Mathf.Min(a[e1], b[e1], c[e1]), a1 = Mathf.Max(a[e1], b[e1], c[e1]), b0 = Mathf.Min(a[e2], b[e2], c[e2]), b1 = Mathf.Max(a[e2], b[e2], c[e2]);
                if (porPlano.TryGetValue(key, out var f)) { a0 = Mathf.Min(a0, f.a0); a1 = Mathf.Max(a1, f.a1); b0 = Mathf.Min(b0, f.b0); b1 = Mathf.Max(b1, f.b1); }
                porPlano[key] = new Cara { r = mr, mat = mr.sharedMaterial.name, clave = clave, d = a[eje], a0 = a0, a1 = a1, b0 = b0, b1 = b1 };
            }
            caras.AddRange(porPlano.Values);
        }
        caras.Sort((x, y) => x.d.CompareTo(y.d));
        return caras;
    }

    static IEnumerable<(Cara, Cara)> Pares(List<Cara> caras)
    {
        for (int i = 0; i < caras.Count; i++)
            for (int j = i + 1; j < caras.Count && caras[j].d - caras[i].d < Tolerancia; j++)
            {
                Cara c = caras[i], o = caras[j];
                if (o.r == c.r || o.clave != c.clave) continue;
                float wa = Mathf.Min(c.a1, o.a1) - Mathf.Max(c.a0, o.a0), wb = Mathf.Min(c.b1, o.b1) - Mathf.Max(c.b0, o.b0);
                if (wa >= 0.15f && wb >= 0.15f) yield return (c, o);
            }
    }

    static int Contar() { int n = 0; foreach (var _ in Pares(Caras())) n++; return n; }

    static int Pasada()
    {
        var movidos = new HashSet<MeshRenderer>();
        foreach (var (c, o) in Pares(Caras()))
        {
            float ac = (c.a1 - c.a0) * (c.b1 - c.b0), ao = (o.a1 - o.a0) * (o.b1 - o.b0);
            var cual = ac < ao ? c : o;
            if (movidos.Contains(c.r) || movidos.Contains(o.r) || !movidos.Add(cual.r)) continue;
            int eje = Mathf.Abs(cual.clave) - 1;
            Vector3 dir = Vector3.zero;
            dir[eje] = Mathf.Sign(cual.clave);
            cual.r.transform.position += dir * Separacion;
            EditorUtility.SetDirty(cual.r.transform);
        }
        Physics.SyncTransforms();
        return movidos.Count;
    }
}
