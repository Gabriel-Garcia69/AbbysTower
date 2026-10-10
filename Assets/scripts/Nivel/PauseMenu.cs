using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú de pausa (Esc / P / Start): congela el juego y ofrece continuar, reiniciar la escena, volver al exterior o salir.
/// Mientras está abierto apaga la cámara de mouse y libera el cursor. No se abre con la tienda o la cinemática activas.
/// FloorDirector lo agrega solo en los pisos; en el exterior va en cualquier objeto de la escena.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public string exteriorScene = "Exterior";

    public static bool IsPaused { get; private set; }
    /// <summary>Otros menús (tienda) lo ponen en true para que Esc no abra la pausa.</summary>
    public static bool Blocked;

    MouseOrbitCamera[] orbit;
    float savedScale = 1f;

    void OnDisable() { if (IsPaused) Resume(); }

    void Update()
    {
        var kb = Keyboard.current; var gp = Gamepad.current;
        bool toggle = (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)) || (gp != null && gp.startButton.wasPressedThisFrame);
        if (!toggle) return;
        if (IsPaused) Resume();
        else if (!Blocked && !IntroPlaying()) Pause();
    }

    static bool IntroPlaying()
    {
        var intro = FindFirstObjectByType<ExteriorIntro>();
        return intro != null && intro.enabled;
    }

    void Pause()
    {
        IsPaused = true;
        savedScale = Time.timeScale > 0.05f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        orbit = FindObjectsByType<MouseOrbitCamera>(FindObjectsSortMode.None);
        foreach (var o in orbit) o.enabled = false;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }

    void Resume()
    {
        IsPaused = false;
        Time.timeScale = savedScale;
        if (orbit != null) foreach (var o in orbit) if (o != null) o.enabled = true;
    }

    void Load(string scene)
    {
        IsPaused = false; Blocked = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }

    void OnGUI()
    {
        if (!IsPaused) return;
        float sw = Screen.width, sh = Screen.height, u = sh / 100f;
        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 7f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        title.normal.textColor = new Color(1f, 0.9f, 0.7f);
        GUI.Label(new Rect(0, sh * 0.18f, sw, u * 10f), "PAUSA", title);

        var btn = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(u * 3f), fontStyle = FontStyle.Bold };
        float bw = u * 36f, bh = u * 6.5f, x = (sw - bw) / 2f, y = sh * 0.34f;
        string current = SceneManager.GetActiveScene().name;
        if (GUI.Button(new Rect(x, y, bw, bh), "Continuar", btn)) Resume();
        y += bh + u * 2f;
        if (GUI.Button(new Rect(x, y, bw, bh), current == exteriorScene ? "Reiniciar exterior" : "Reiniciar piso", btn)) Load(current);
        y += bh + u * 2f;
        if (current != exteriorScene && Application.CanStreamedLevelBeLoaded(exteriorScene))
        {
            if (GUI.Button(new Rect(x, y, bw, bh), "Volver al exterior", btn)) Load(exteriorScene);
            y += bh + u * 2f;
        }
        if (GUI.Button(new Rect(x, y, bw, bh), "Salir del juego", btn))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        var hint = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 2.2f), alignment = TextAnchor.MiddleCenter };
        hint.normal.textColor = new Color(1f, 1f, 1f, 0.6f);
        GUI.Label(new Rect(0, sh * 0.88f, sw, u * 4f), "Esc / P / Start: continuar", hint);
        GUI.color = prev;
    }
}
