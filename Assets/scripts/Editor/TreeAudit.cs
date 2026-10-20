using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// QA de vegetación: árboles, pinos, arbustos y palmeras que flotan (la base no toca el suelo), que se hunden o que
/// quedaron dentro del agua. Mira el suelo real con un rayo (terreno con collider) o, fuera del terreno, la altura de
/// los planos de afuera. Devuelve un reporte; con fix = true borra los que están mal.
/// </summary>
public static class TreeAudit
{
    static readonly string[] Plantas = { "Pino", "Arbol", "Arbusto", "Palmera", "PinoValle" };

    public static string Run(string scene, bool fix)
    {
        if (EditorSceneManager.GetActiveScene().name != scene) EditorSceneManager.OpenScene("Assets/Scenes/" + scene + ".unity");
        Physics.SyncTransforms();
        var water = new System.Collections.Generic.List<Renderer>();
        foreach (var r in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
        {
            var m = r.sharedMaterial;
            if (m != null && m.shader != null && m.shader.name == "Abby/Water") water.Add(r);
        }
        int total = 0, flotan = 0, hundidos = 0, agua = 0;
        var sb = new StringBuilder();
        var borrar = new System.Collections.Generic.List<GameObject>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
        {
            if (System.Array.IndexOf(Plantas, t.name) < 0) continue;
            total++;
            Vector3 p = t.position;
            string mal = null;
            // ¿dentro del agua? (una superficie de agua por encima de la base, en su huella)
            foreach (var w in water)
            {
                var b = w.bounds;
                if (p.x > b.min.x && p.x < b.max.x && p.z > b.min.z && p.z < b.max.z && b.max.y > p.y + 0.05f && b.min.y < p.y + 3f)
                {
                    // confirmar con un rayo hacia abajo desde arriba: si lo primero que toca es más bajo que el agua
                    mal = "en el agua"; break;
                }
            }
            if (mal == null)
            {
                // el suelo más alto bajo la base (sin contar al propio árbol)
                float best = float.NegativeInfinity; bool terreno = false;
                foreach (var h in Physics.RaycastAll(p + Vector3.up * 2f, Vector3.down, 30f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (h.collider.transform.IsChildOf(t)) continue;
                    if (h.point.y > best) { best = h.point.y; terreno = h.collider.name.StartsWith("Terreno"); }
                }
                if (!float.IsNegativeInfinity(best))
                {
                    if (best < p.y - 0.35f) mal = "flota " + (p.y - best).ToString("F1") + " m";
                    else if (best > p.y + 0.6f && terreno) mal = "hundido " + (best - p.y).ToString("F1") + " m";
                }
            }
            if (mal == null) continue;
            if (mal.StartsWith("flota")) flotan++; else if (mal.StartsWith("hundido")) hundidos++; else agua++;
            if (sb.Length < 3000) sb.AppendLine($"  {t.name} {p:F0} {mal} ({(t.parent ? t.parent.name : "-")})");
            borrar.Add(t.gameObject);
        }
        if (fix && borrar.Count > 0)
        {
            foreach (var go in borrar) Object.DestroyImmediate(go);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
        return $"{scene}: {total} plantas, flotan {flotan}, hundidas {hundidos}, en el agua {agua}\n" + sb;
    }
}
