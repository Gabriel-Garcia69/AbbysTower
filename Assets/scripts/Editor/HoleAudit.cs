using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// QA visual de los pisos HD-2D: desde cada lugar importante (inicio, arenas, salida, notas, cofres y una rejilla sobre
/// la zona jugable) mira en 8 direcciones con la cámara F3 (cerca) y la de maqueta (lejos) y cuenta los "huecos":
/// pixeles debajo del horizonte donde no hay nada dibujado (se vería el cielo o negro donde debería haber suelo).
/// Abby → QA: huecos en el mapa abierto. Devuelve un reporte y guarda una cuadrícula con las peores vistas.
/// </summary>
public static class HoleAudit
{
    static readonly Color Key = new Color(1f, 0f, 1f);

    struct View { public Vector3 pos; public Quaternion rot; public float fov; public string label; public float hole; }

    [MenuItem("Abby/QA: huecos en el mapa abierto")]
    static void RunMenu() => Debug.Log(Run(EditorSceneManager.GetActiveScene().name, "Temp/huecos.png", 12));

    public static string Run(string sceneName, string outPng, int worst) => Run(sceneName, outPng, worst, 24f);

    public static string Run(string sceneName, string outPng, int worst, float grid)
    {
        var sc = EditorSceneManager.GetActiveScene();
        if (sc.name != sceneName) EditorSceneManager.OpenScene("Assets/Scenes/" + sceneName + ".unity");
        Physics.SyncTransforms();
        var pts = new List<KeyValuePair<string, Vector3>>();
        var player = GameObject.FindWithTag("Player");
        if (player != null) pts.Add(new KeyValuePair<string, Vector3>("Inicio", player.transform.position));
        foreach (var r in Object.FindObjectsByType<RoomEncounter>(FindObjectsInactive.Exclude)) pts.Add(new KeyValuePair<string, Vector3>(r.name, r.transform.position));
        foreach (var e in Object.FindObjectsByType<FloorExit>(FindObjectsInactive.Exclude)) pts.Add(new KeyValuePair<string, Vector3>("Salida", e.transform.position));
        foreach (var ln in Object.FindObjectsByType<LoreNote>(FindObjectsInactive.Exclude)) pts.Add(new KeyValuePair<string, Vector3>("Nota " + ln.title, ln.transform.position));
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Exclude))
            if (t.name == "Cofre" || t.name == "Mercader" || t.name == "Santuario") pts.Add(new KeyValuePair<string, Vector3>(t.name, t.position));
        // rejilla en la zona jugable (dentro de los límites)
        float x0 = -60f, x1 = 60f, z0 = -60f, z1 = 60f;
        foreach (var c in Object.FindObjectsByType<BoxCollider>(FindObjectsInactive.Exclude))
        {
            var b = c.bounds;
            if (c.name == "Limite_O") x0 = b.max.x; if (c.name == "Limite_E") x1 = b.min.x;
            if (c.name == "Limite_S") z0 = b.max.z; if (c.name == "Limite_N") z1 = b.min.z;
        }
        for (float x = x0 + 6f; x < x1; x += grid)
            for (float z = z0 + 6f; z < z1; z += grid)
                if (Physics.Raycast(new Vector3(x, 200f, z), Vector3.down, out var hit, 400f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    pts.Add(new KeyValuePair<string, Vector3>($"Rejilla ({x:F0},{z:F0})", hit.point));

        var go = new GameObject("__qa");
        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Key;
        cam.nearClipPlane = 0.2f; cam.farClipPlane = 4000f;
        const int W = 192, H = 108;
        var rt = new RenderTexture(W, H, 24);
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        cam.targetTexture = rt;
        var views = new List<View>();
        foreach (var kv in pts)
        {
            Vector3 p = kv.Value;
            for (int k = 0; k < 8; k++)
            {
                float yaw = k * 45f;
                foreach (var mode in new[] { 0, 1, 2 })
                {
                    float pitch = mode == 0 ? 14f : mode == 1 ? 34f : 20f, dist = mode == 2 ? 36f : 6.5f, fov = mode == 2 ? 22f : 48f;
                    var arm = Quaternion.Euler(pitch, yaw, 0f);
                    Vector3 pivot = p + Vector3.up * (mode == 2 ? 0.8f : 1f);
                    // como la cámara del juego: el terreno la acerca (no se mete en la roca)
                    var hits = Physics.SphereCastAll(pivot, 0.3f, -(arm * Vector3.forward), dist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    foreach (var h in hits)
                        if (h.collider is MeshCollider && h.collider.name.StartsWith("Terreno") && h.distance > 0f) dist = Mathf.Min(dist, Mathf.Max(1.2f, h.distance));
                    if (Physics.Raycast(pivot, -(arm * Vector3.forward), out var thin, dist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                        && thin.collider is MeshCollider && thin.collider.name.StartsWith("Terreno")) dist = Mathf.Min(dist, Mathf.Max(1.2f, thin.distance - 0.3f));
                    var v = new View { pos = pivot - arm * Vector3.forward * dist, rot = arm * Quaternion.Euler(mode == 2 ? -6f : 0f, 0f, 0f), fov = fov,
                                       label = $"{kv.Key} · {(mode == 2 ? "maqueta" : "F3")} {yaw:F0}°" + (mode == 1 ? " (alta)" : "") };
                    cam.transform.SetPositionAndRotation(v.pos, v.rot); cam.fieldOfView = fov;
                    cam.Render();
                    RenderTexture.active = rt; tex.ReadPixels(new Rect(0, 0, W, H), 0, 0); tex.Apply(); RenderTexture.active = null;
                    var px = tex.GetPixels();
                    int below = 0, holes = 0;
                    for (int y = 0; y < H; y += 2)
                        for (int x = 0; x < W; x += 2)
                        {
                            var ray = cam.ViewportPointToRay(new Vector3((x + 0.5f) / W, (y + 0.5f) / H, 0f));
                            if (ray.direction.y > -0.02f) continue;
                            // si el rayo llegaría al suelo ya dentro de la bruma, lo que se ve es bruma: no cuenta
                            if (40f / -ray.direction.y > RenderSettings.fogEndDistance * 0.92f) continue;
                            below++;
                            var c = px[y * W + x];
                            if (c.r > 0.95f && c.g < 0.05f && c.b > 0.95f) holes++;
                        }
                    v.hole = below > 0 ? holes / (float)below : 0f;
                    views.Add(v);
                }
            }
        }
        views.Sort((a, b) => b.hole.CompareTo(a.hole));
        var sb = new StringBuilder();
        int bad = 0;
        foreach (var v in views) if (v.hole > 0.003f) bad++;
        sb.AppendLine($"{sceneName}: {pts.Count} lugares, {views.Count} vistas, {bad} con huecos");
        for (int i = 0; i < Mathf.Min(worst, views.Count); i++)
            if (views[i].hole > 0.003f) sb.AppendLine($"  {views[i].hole * 100f:F1}%  {views[i].label}  cam {views[i].pos:F0}");

        // cuadrícula con las peores vistas (render normal, con cielo)
        int n = Mathf.Min(worst, bad);
        if (n > 0 && !string.IsNullOrEmpty(outPng))
        {
            var main = Camera.main;
            if (main != null) { cam.CopyFrom(main); }
            cam.nearClipPlane = 0.2f; cam.farClipPlane = 4000f;
            const int TW = 480, TH = 270;
            var big = new RenderTexture(TW, TH, 24);
            cam.targetTexture = big;
            var shot = new Texture2D(TW, TH, TextureFormat.RGB24, false);
            int cols = 3, rows = (n + cols - 1) / cols;
            var sheet = new Texture2D(TW * cols, TH * rows, TextureFormat.RGB24, false);
            for (int i = 0; i < n; i++)
            {
                // en la cuadrícula los huecos se pintan magenta (con cielo de fondo no se notan igual)
                cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Key;
                cam.transform.SetPositionAndRotation(views[i].pos, views[i].rot); cam.fieldOfView = views[i].fov;
                cam.Render(); cam.Render();
                RenderTexture.active = big; shot.ReadPixels(new Rect(0, 0, TW, TH), 0, 0); shot.Apply(); RenderTexture.active = null;
                sheet.SetPixels((i % cols) * TW, (rows - 1 - i / cols) * TH, TW, TH, shot.GetPixels());
            }
            sheet.Apply();
            System.IO.File.WriteAllBytes(outPng, sheet.EncodeToPNG());
            Object.DestroyImmediate(big); Object.DestroyImmediate(shot); Object.DestroyImmediate(sheet);
        }
        cam.targetTexture = null;
        Object.DestroyImmediate(rt); Object.DestroyImmediate(tex); Object.DestroyImmediate(go);
        return sb.ToString();
    }
}
