using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Abby → Exterior → Importar piezas del Diorama: copia casas, muelles, botes, braseros, faroles, etc. de las escenas
/// del Diorama/Puerto a prefabs en Assets/Prefabs/Exterior/, según la tabla PiezasDiorama.asset.
/// - Las mallas generadas (que viven dentro de la escena) se guardan como .asset en Assets/Prefabs/Exterior/Mallas/.
/// - Los materiales se DUPLICAN en Assets/Materials/Cala/Piezas/: el exterior los puede recolorear sin tocar los pisos.
///   Si la copia ya existe se reutiliza (volver a importar no deshace la paleta del exterior).
/// - Solo quedan LightFlicker y FloatingBob; cualquier otro script (santuarios, gaviotas...) se quita.
///
/// Las escenas de origen se abren en una escena temporal aparte y se cierran SIN guardar (si DioramaRoute las
/// regenera al abrirlas, ese cambio se descarta). Al terminar se vuelven a abrir las escenas que tenías.
/// El reporte (tamaño de cada pieza) queda en la consola y en Logs/PiezasDiorama.txt.
/// </summary>
public static class PiezasDioramaTool
{
    const string TablaPath = "Assets/Prefabs/Exterior/PiezasDiorama.asset";
    const string PrefabDir = "Assets/Prefabs/Exterior";
    const string MallaDir = "Assets/Prefabs/Exterior/Mallas";
    const string MatDir = "Assets/Materials/Cala/Piezas";
    const string Reporte = "Logs/PiezasDiorama.txt";
    static readonly System.Type[] Permitidos = { typeof(LightFlicker), typeof(FloatingBob) };

    [MenuItem("Abby/Exterior/Importar piezas del Diorama")]
    public static void Importar() => Ejecutar(false);

    [MenuItem("Abby/Exterior/Listar candidatos del Diorama")]
    public static void Listar() => Ejecutar(true);

    static PiezasDiorama Tabla()
    {
        CrearCarpeta(PrefabDir);
        var t = AssetDatabase.LoadAssetAtPath<PiezasDiorama>(TablaPath);
        if (t != null) return t;
        t = ScriptableObject.CreateInstance<PiezasDiorama>();
        t.piezas = PiezasDiorama.PorDefecto();
        AssetDatabase.CreateAsset(t, TablaPath);
        AssetDatabase.SaveAssets();
        return t;
    }

    static void Ejecutar(bool soloListar)
    {
        if (Application.isPlaying) { Debug.LogWarning("[Piezas] Sal de Play primero."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var tabla = Tabla();
        var setup = EditorSceneManager.GetSceneManagerSetup().Where(s => !string.IsNullOrEmpty(s.path)).ToArray();   // sin las "Untitled"
        if (setup.Length > 0 && !setup.Any(s => s.isActive)) setup[0].isActive = true;
        var sb = new StringBuilder();
        var copiasMat = new Dictionary<Material, Material>();
        int hechas = 0, fallas = 0;
        try
        {
            // escena temporal activa: los ExteriorSky de las escenas de origen escriben su ambiente aquí, no en las tuyas
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var escenas = tabla.piezas.Select(p => p.escena).Distinct().ToList();
            for (int e = 0; e < escenas.Count; e++)
            {
                string ruta = "Assets/Scenes/" + escenas[e] + ".unity";
                EditorUtility.DisplayProgressBar("Piezas del Diorama", escenas[e], e / (float)escenas.Count);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ruta) == null) { sb.AppendLine($"!! No existe {ruta}"); fallas++; continue; }
                var origen = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Additive);
                try
                {
                    var filas = tabla.piezas.Where(p => p.escena == escenas[e]).ToList();
                    if (soloListar) ListarEscena(origen, filas, sb);
                    else
                        foreach (var p in filas)
                        {
                            var src = Buscar(origen, p);
                            if (src == null) { sb.AppendLine($"!! {p.prefab}: no hay \"{p.nombre}\" #{p.indice} en {p.escena}" + (p.dentroDe != "" ? $" dentro de \"{p.dentroDe}\"" : "")); fallas++; continue; }
                            sb.AppendLine(Copiar(src, p, copiasMat));
                            hechas++;
                        }
                }
                finally { EditorSceneManager.CloseScene(origen, true); }   // sin guardar
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            else EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            foreach (var sky in Object.FindObjectsByType<ExteriorSky>()) sky.Apply();   // cielo global de tus escenas
        }
        string titulo = soloListar ? "[Piezas] Candidatos" : $"[Piezas] {hechas} prefabs en {PrefabDir}" + (fallas > 0 ? $", {fallas} con problema" : "");
        System.IO.Directory.CreateDirectory("Logs");
        System.IO.File.WriteAllText(Reporte, titulo + "\n" + sb);
        if (fallas > 0) Debug.LogWarning(titulo + "\n" + sb); else Debug.Log(titulo + "\n" + sb);
    }

    // ------------------------------------------------------------------ búsqueda

    static IEnumerable<Transform> Recorrer(Scene s)
    {
        var pila = new Stack<Transform>();
        var raices = s.GetRootGameObjects();
        for (int i = raices.Length - 1; i >= 0; i--) pila.Push(raices[i].transform);
        while (pila.Count > 0)
        {
            var t = pila.Pop();
            yield return t;
            for (int i = t.childCount - 1; i >= 0; i--) pila.Push(t.GetChild(i));
        }
    }

    static string RutaPadres(Transform t)
    {
        var partes = new List<string>();
        for (var p = t.parent; p != null; p = p.parent) partes.Insert(0, p.name);
        return string.Join("/", partes);
    }

    static List<Transform> Coincidencias(Scene s, string nombre, string dentroDe) =>
        Recorrer(s).Where(t => t.name == nombre && (string.IsNullOrEmpty(dentroDe) || RutaPadres(t).Contains(dentroDe))).ToList();

    static Transform Buscar(Scene s, PiezasDiorama.Pieza p)
    {
        var l = Coincidencias(s, p.nombre, p.dentroDe);
        return p.indice >= 0 && p.indice < l.Count ? l[p.indice] : null;
    }

    static Bounds Limites(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer)).ToArray();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static void ListarEscena(Scene s, List<PiezasDiorama.Pieza> filas, StringBuilder sb)
    {
        sb.AppendLine($"== {s.name}");
        foreach (var nombre in filas.Select(f => f.nombre).Distinct())
        {
            var l = Coincidencias(s, nombre, "");
            sb.AppendLine($"  {nombre}: {l.Count}");
            for (int i = 0; i < l.Count && i < 40; i++)
            {
                var b = Limites(l[i].gameObject);
                sb.AppendLine($"    #{i}  {RutaPadres(l[i])}  pos {l[i].position:F1}  tamaño {b.size:F1}");
            }
        }
    }

    // ------------------------------------------------------------------ copia

    static string Copiar(Transform src, PiezasDiorama.Pieza p, Dictionary<Material, Material> copiasMat)
    {
        var go = Object.Instantiate(src.gameObject);
        go.name = p.prefab;
        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);   // la yaw del mapa se pierde: frente = +Z local
        go.transform.localScale = src.lossyScale;

        var quitados = new HashSet<string>();
        foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true).Reverse())
        {
            if (mb == null) continue;
            var tipo = mb.GetType();
            bool quitar = !Permitidos.Contains(tipo) || (p.quitarFlotar && tipo == typeof(FloatingBob)) || (p.quitarLuces && tipo == typeof(LightFlicker));
            if (quitar) { quitados.Add(tipo.Name); Object.DestroyImmediate(mb); }
        }
        if (p.quitarLuces)
            foreach (var l in go.GetComponentsInChildren<Light>(true)) { quitados.Add("Light"); Object.DestroyImmediate(l); }

        // mallas de escena → assets
        string carpetaMallas = MallaDir + "/" + p.prefab;
        var mallas = new Dictionary<Mesh, Mesh>();
        Mesh Guardar(Mesh m)
        {
            if (m == null || !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m))) return m;   // ya es asset (o primitiva de Unity)
            if (mallas.TryGetValue(m, out var g)) return g;
            CrearCarpeta(carpetaMallas);
            string ruta = $"{carpetaMallas}/{Limpio(m.name)}_{mallas.Count}.asset";
            var copia = Object.Instantiate(m);
            copia.name = m.name;
            var existente = AssetDatabase.LoadAssetAtPath<Mesh>(ruta);
            if (existente != null) { EditorUtility.CopySerialized(copia, existente); Object.DestroyImmediate(copia); copia = existente; }   // conserva el GUID
            else AssetDatabase.CreateAsset(copia, ruta);
            mallas[m] = copia;
            return copia;
        }
        foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true)) mf.sharedMesh = Guardar(mf.sharedMesh);
        foreach (var mc in go.GetComponentsInChildren<MeshCollider>(true)) mc.sharedMesh = Guardar(mc.sharedMesh);

        // materiales → copias propias del exterior
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = CopiaMaterial(mats[i], copiasMat);
            r.sharedMaterials = mats;
            if (r is ParticleSystemRenderer pr && pr.trailMaterial != null) pr.trailMaterial = CopiaMaterial(pr.trailMaterial, copiasMat);
        }

        var b = Limites(go);
        int luces = go.GetComponentsInChildren<Light>(true).Length;
        PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{p.prefab}.prefab");
        Object.DestroyImmediate(go);
        return $"{p.prefab,-17} ← {p.escena}/{RutaPadres(src)}/{src.name} #{p.indice}   tamaño {b.size:F1} m, base y {b.min.y:F2}, {mallas.Count} mallas, {luces} luces"
               + (quitados.Count > 0 ? "   quitado: " + string.Join(", ", quitados) : "");
    }

    static Material CopiaMaterial(Material m, Dictionary<Material, Material> copias)
    {
        if (m == null) return null;
        if (copias.TryGetValue(m, out var c)) return c;
        CrearCarpeta(MatDir);
        string ruta = $"{MatDir}/{Limpio(m.name)}.mat";
        var existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (existente == null)
        {
            string orig = AssetDatabase.GetAssetPath(m);
            if (orig.EndsWith(".mat")) AssetDatabase.CopyAsset(orig, ruta);
            else AssetDatabase.CreateAsset(new Material(m) { name = m.name }, ruta);   // material que vivía dentro de la escena
            existente = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        }
        copias[m] = existente;
        return existente;
    }

    static string Limpio(string s)
    {
        foreach (char ch in System.IO.Path.GetInvalidFileNameChars()) s = s.Replace(ch, '_');
        s = s.Replace(" (Instance)", "").Trim();
        return s.Length == 0 ? "sin_nombre" : s;
    }

    static void CrearCarpeta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta)) return;
        int i = ruta.LastIndexOf('/');
        CrearCarpeta(ruta.Substring(0, i));
        AssetDatabase.CreateFolder(ruta.Substring(0, i), ruta.Substring(i + 1));
    }
}
