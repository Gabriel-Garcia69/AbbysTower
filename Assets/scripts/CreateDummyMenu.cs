#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Menú: GameObject → Aby → Crear Dummy (pasivo / agresivo).
/// Crea un cubo con TrainingDummy, a 3 m a la derecha de Aby, con el Target ya asignado.
/// </summary>
public static class CreateDummyMenu
{
    [MenuItem("GameObject/Aby/Crear Dummy (pasivo)", false, 10)]
    static void CreatePassive() { Create(false); }

    [MenuItem("GameObject/Aby/Crear Dummy (agresivo: ataca con aviso amarillo)", false, 11)]
    static void CreateAggressive() { Create(true); }

    static void Create(bool attacks)
    {
        var player = Object.FindFirstObjectByType<PlayerController>();
        if (player == null)
            Debug.LogWarning("No encontré a Aby (PlayerController) en la escena: el dummy se crea sin Target.");

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Dummy";
        Undo.RegisterCreatedObjectUndo(go, "Crear Dummy");

        go.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        Vector3 pos = player != null ? player.transform.position + new Vector3(3f, 0f, 0f) : Vector3.zero;
        pos.y = 0.5f;                       // la base del cubo toca el piso
        go.transform.position = pos;

        var dummy = go.AddComponent<TrainingDummy>();   // agrega Hitbox solo (RequireComponent)
        dummy.target = player != null ? player.transform : null;
        dummy.attacks = attacks;

        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("Dummy creado (" + (attacks ? "agresivo" : "pasivo") + "). Dale Play: J = golpe, Espacio = dash, K = bloquear.");
    }
}
#endif
