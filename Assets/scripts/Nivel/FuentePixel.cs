using UnityEngine;

/// <summary>
/// Fuente pixel para todo el HUD y los menús (todos se dibujan con OnGUI): Press Start 2P (licencia OFL, ver
/// Resources/Fuentes/PressStart2P-OFL.txt). Se pone en la piel por defecto antes de que dibuje cualquier otro script,
/// así los GUIStyle que se crean a partir de GUI.skin.label la heredan sin tocar cada pantalla.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class FuentePixel : MonoBehaviour
{
    static Font fuente;
    static bool intentado;

    /// <summary>La pixel es monoespaciada y casi el doble de ancha que la de antes: los tamaños de cada pantalla se escalan aquí.</summary>
    public const float Escala = 0.6f;

    /// <summary>Tamaño de letra para OnGUI: el que pide cada pantalla, ajustado a la fuente pixel.</summary>
    public static int Tam(float tam)
    {
        Cargar();
        return Mathf.Max(6, Mathf.RoundToInt(tam * (fuente != null ? Escala : 1f)));
    }

    static void Cargar()
    {
        if (intentado) return;
        intentado = true;
        fuente = Resources.Load<Font>("Fuentes/PressStart2P-Regular");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Crear()
    {
        var go = new GameObject("FuentePixel");
        go.hideFlags = HideFlags.HideInHierarchy;
        DontDestroyOnLoad(go);
        go.AddComponent<FuentePixel>();
    }

    void OnGUI()
    {
        Cargar();
        if (fuente != null && GUI.skin.font != fuente) GUI.skin.font = fuente;
    }
}
