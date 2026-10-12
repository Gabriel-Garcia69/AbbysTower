using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Abby → Regenerar ruta HD-2D: abre cada piso de la ruta, lo vuelve a generar con el código actual, pone a Abby y
/// la cámara en el punto de inicio, ajusta el piso (título, siguiente, caída) y guarda.
/// Además, al abrir un piso generado con una versión vieja del código, se regenera solo (hay que guardar la escena).
/// </summary>
[InitializeOnLoad]
public static class DioramaRoute
{
    static DioramaRoute()
    {
        EditorSceneManager.sceneOpened += (scene, mode) =>
        {
            if (Application.isPlaying) return;
            foreach (var root in scene.GetRootGameObjects())
            {
                var b = root.GetComponentInChildren<DioramaBuilder>();
                if (b == null || b.IsCurrent) continue;
                Rebuild(b, scene.name);
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"[Diorama] {scene.name}: regenerado con la versión actual. Guarda la escena (Ctrl+S).");
            }
        };
    }

    [MenuItem("Abby/Regenerar ruta HD-2D")]
    public static void RegenerateAll()
    {
        if (Application.isPlaying) { Debug.LogWarning("[Diorama] Sal de Play para regenerar."); return; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        foreach (var name in DioramaBuilder.Route)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            var b = Object.FindAnyObjectByType<DioramaBuilder>();
            if (b == null) continue;
            Rebuild(b, name);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log("[Diorama] Ruta HD-2D regenerada y guardada.");
    }

    static void Rebuild(DioramaBuilder b, string sceneName)
    {
        b.Build();
        var p = GameObject.FindWithTag("Player");
        if (p != null) p.transform.position = b.SpawnPoint;
        var rig = Object.FindAnyObjectByType<CameraRig>();
        if (rig != null) rig.transform.position = b.SpawnPoint;
        b.ConfigureScene(b.KillY);
    }
}
