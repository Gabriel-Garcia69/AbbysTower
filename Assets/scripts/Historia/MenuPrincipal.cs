using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú principal (GDD): Jugar · Configuración (volumen general y de música, pantalla completa) · Salir (pide
/// confirmación). De fondo, la torre del abismo en la plaza, con la cámara girando despacio. Va en la escena "Menu"
/// (una copia de la escena Intro con la cinemática apagada). Jugar abre la cinemática inicial.
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    public string escenaJugar = "Intro";
    public Vector3 centro = new Vector3(0f, 0f, -1850f);   // la torre en el set de la plaza
    public float radio = 260f, altura = 22f;

    enum Pagina { Principal, Configuracion, Salir }
    Pagina pagina;
    int sel;
    float t, entrada;
    Camera cam;

    void Awake()
    {
        // antes de que la cinemática empiece (su Start corre después de todos los Awake)
        foreach (var c in FindObjectsByType<Cinematica>()) Destroy(c);
    }

    void Start()
    {
        // sin HUD del piso ni cámara de juego (esa sigue a Abby y pisaría la del menú): una cámara propia, como la cinemática
        var fd = FindAnyObjectByType<FloorDirector>();
        if (fd != null) fd.gameObject.SetActive(false);
        var main = Camera.main;
        var go = new GameObject("CamaraMenu");
        cam = go.AddComponent<Camera>();
        if (main != null) { cam.CopyFrom(main); main.transform.root.gameObject.SetActive(false); }
        cam.nearClipPlane = 0.3f; cam.farClipPlane = 3000f;
        go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;
        go.AddComponent<AudioListener>();
        go.tag = "MainCamera";
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        Time.timeScale = 1f;
    }

    void Update()
    {
        // la escena es una copia de la Intro: si su cinemática aparece (al llegar desde otro piso se vuelve a crear), fuera
        if (t < 3f) foreach (var c in FindObjectsByType<Cinematica>()) { c.StopAllCoroutines(); c.enabled = false; Destroy(c.gameObject); }
        t += Time.deltaTime; entrada += Time.deltaTime;
        if (cam != null)
        {
            float a = t * 0.04f;
            cam.transform.position = centro + new Vector3(Mathf.Sin(a) * radio, altura, -Mathf.Cos(a) * radio);
            cam.transform.rotation = Quaternion.LookRotation(centro + Vector3.up * 75f - cam.transform.position);
        }
        var kb = Keyboard.current;
        if (kb == null) return;
        int n = Opciones().Length;
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) sel = (sel + 1) % n;
        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) sel = (sel + n - 1) % n;
        if (pagina == Pagina.Configuracion && (sel == 0 || sel == 1))
        {
            float paso = kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame ? 0.1f : kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame ? -0.1f : 0f;
            if (paso != 0f) { if (sel == 0) Ajustes.General += paso; else Ajustes.Musica += paso; }
        }
        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Elegir(sel);
        if (kb.escapeKey.wasPressedThisFrame) { if (pagina != Pagina.Principal) Ir(Pagina.Principal); else Ir(Pagina.Salir); }
    }

    string[] Opciones()
    {
        switch (pagina)
        {
            case Pagina.Configuracion:
                return new[] { $"Volumen general   ◀ {Mathf.RoundToInt(Ajustes.General * 100f)} % ▶", $"Volumen de música   ◀ {Mathf.RoundToInt(Ajustes.Musica * 100f)} % ▶",
                               "Pantalla completa: " + (Screen.fullScreen ? "Sí" : "No"), "Restablecer", "Volver" };
            case Pagina.Salir: return new[] { "No, quedarme", "Sí, salir del juego" };
            default: return new[] { "Jugar", "Configuración", "Salir" };
        }
    }

    void Ir(Pagina p) { pagina = p; sel = 0; entrada = 0f; }

    void Elegir(int i)
    {
        switch (pagina)
        {
            case Pagina.Principal:
                if (i == 0) SceneManager.LoadScene(escenaJugar);
                else if (i == 1) Ir(Pagina.Configuracion);
                else Ir(Pagina.Salir);
                break;
            case Pagina.Configuracion:
                if (i == 2) Screen.fullScreen = !Screen.fullScreen;
                else if (i == 3) Ajustes.Restablecer();
                else if (i == 4) Ir(Pagina.Principal);
                break;
            case Pagina.Salir:
                if (i == 0) Ir(Pagina.Principal);
                else
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
                break;
        }
    }

    void OnGUI()
    {
        GUI.depth = -900;
        float sw = Screen.width, sh = Screen.height, u = sh / 100f;
        // título
        var titulo = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 9f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        Sombra(new Rect(0, sh * 0.1f, sw, u * 12f), "ABBY´S TOWER", titulo, new Color(1f, 0.92f, 0.75f));
        var sub = new GUIStyle(titulo) { fontSize = Mathf.RoundToInt(u * 2.6f), fontStyle = FontStyle.Italic };
        Sombra(new Rect(0, sh * 0.1f + u * 11f, sw, u * 4f), "La torre del abismo", sub, new Color(0.85f, 0.78f, 1f));

        var ops = Opciones();
        float w = pagina == Pagina.Configuracion ? u * 56f : u * 36f, h = u * 6.2f;
        var caja = new Rect((sw - w) / 2f, sh * 0.48f, w, h * ops.Length + u * 4f);
        float a = Mathf.Clamp01(entrada / 0.25f);
        CuadroNarracion.Marco(caja, 0.92f * a);
        if (pagina == Pagina.Salir)
        {
            var q = new GUIStyle(sub) { fontStyle = FontStyle.Bold, fontSize = Mathf.RoundToInt(u * 2.8f) };
            Sombra(new Rect(caja.x, caja.y - u * 6f, caja.width, u * 5f), "¿Salir del juego?", q, Color.white);
        }
        var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 2.9f), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        var ev = Event.current;
        for (int i = 0; i < ops.Length; i++)
        {
            var r = new Rect(caja.x + u * 2f, caja.y + u * 2f + i * h, caja.width - u * 4f, h);
            if (ev.type == EventType.MouseMove && r.Contains(ev.mousePosition)) sel = i;
            if (ev.type == EventType.MouseDown && ev.button == 0 && r.Contains(ev.mousePosition))
            {
                sel = i;
                if (pagina == Pagina.Configuracion && i <= 1)
                {
                    float paso = ev.mousePosition.x > r.center.x ? 0.1f : -0.1f;
                    if (i == 0) Ajustes.General += paso; else Ajustes.Musica += paso;
                }
                else Elegir(i);
                ev.Use();
            }
            bool on = i == sel;
            if (on)
            {
                GUI.color = new Color(0.75f, 0.55f, 1f, 0.28f * a);
                GUI.DrawTexture(new Rect(r.x, r.y + u * 0.6f, r.width, r.height - u * 1.2f), Texture2D.whiteTexture);
            }
            Sombra(r, (on ? "›  " : "") + ops[i] + (on ? "  ‹" : ""), st, on ? new Color(1f, 0.85f, 0.5f, a) : new Color(1f, 1f, 1f, 0.85f * a));
        }
        var ayuda = new GUIStyle(sub) { fontSize = Mathf.RoundToInt(u * 1.8f), fontStyle = FontStyle.Normal };
        Sombra(new Rect(0, sh - u * 6f, sw, u * 4f), "↑ ↓  elegir     Enter  aceptar     ← →  ajustar volumen     Esc  regresar", ayuda, new Color(1f, 1f, 1f, 0.7f));
        GUI.color = Color.white;
    }

    static void Sombra(Rect r, string s, GUIStyle st, Color c)
    {
        GUI.color = Color.white;
        st.normal.textColor = new Color(0f, 0f, 0f, c.a * 0.8f);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), s, st);
        st.normal.textColor = c;
        GUI.Label(r, s, st);
    }
}

/// <summary>
/// Configuración que se guarda (GDD: solo el volumen general y el de música). Si falla la lectura, quedan los valores por
/// defecto. La música (Francisco) debe multiplicar su volumen por Ajustes.Musica.
/// </summary>
public static class Ajustes
{
    const float PorDefectoGeneral = 0.8f, PorDefectoMusica = 0.7f;

    public static float General
    {
        get => PlayerPrefs.GetFloat("vol_general", PorDefectoGeneral);
        set { PlayerPrefs.SetFloat("vol_general", Mathf.Clamp01(Mathf.Round(value * 10f) / 10f)); PlayerPrefs.Save(); Aplicar(); }
    }

    public static float Musica
    {
        get => PlayerPrefs.GetFloat("vol_musica", PorDefectoMusica);
        set { PlayerPrefs.SetFloat("vol_musica", Mathf.Clamp01(Mathf.Round(value * 10f) / 10f)); PlayerPrefs.Save(); }
    }

    public static void Restablecer() { General = PorDefectoGeneral; Musica = PorDefectoMusica; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Aplicar() => AudioListener.volume = General;
}
