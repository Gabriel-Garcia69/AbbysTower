using UnityEngine;

/// <summary>
/// Fuente de todo el HUD, los menús y los textos (todos se dibujan con OnGUI): Inter SemiBold (licencia OFL, ver
/// Resources/Fuentes/Inter-OFL.txt). El logo del juego sigue en Press Start 2P (ver Logo). Se pone en la piel por defecto antes de que dibuje cualquier otro script,
/// así los GUIStyle que se crean a partir de GUI.skin.label la heredan sin tocar cada pantalla.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class FuentePixel : MonoBehaviour
{
    static Font fuente, pixel;
    static bool intentado;

    /// <summary>Los tamaños de cada pantalla se escalan aquí, para que los textos en mayúsculas sigan cabiendo.</summary>
    public const float Escala = 0.92f;

    /// <summary>La fuente pixel del logo.</summary>
    public static Font FuenteLogo { get { Cargar(); return pixel; } }

    /// <summary>Pone el logo en fuente pixel; es monoespaciada y muy ancha, por eso lleva su propia escala.</summary>
    public static GUIStyle Logo(GUIStyle st, float tam)
    {
        st.font = FuenteLogo;
        st.fontSize = Mathf.Max(6, Mathf.RoundToInt(tam * 0.6f));
        return st;
    }

    /// <summary>Tamaño de letra para OnGUI: el que pide cada pantalla, ajustado a la fuente.</summary>
    public static int Tam(float tam)
    {
        Cargar();
        return Mathf.Max(6, Mathf.RoundToInt(tam * (fuente != null ? Escala : 1f)));
    }

    static void Cargar()
    {
        if (intentado) return;
        intentado = true;
        fuente = Resources.Load<Font>("Fuentes/Inter-SemiBold");
        pixel = Resources.Load<Font>("Fuentes/PressStart2P-Regular");
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
