using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Pantalla de carga: todos los cambios de escena del juego pasan por Cargador.Ir(escena). Funde a negro, carga la escena en segundo
/// plano (LoadSceneAsync, sin activarla hasta el 90 %), muestra el nombre del piso, una barra y un consejo, y al terminar se deshace
/// con un fundido. Mientras carga, el mundo de atrás queda quieto (timeScale 0). Se dibuja con OnGUI y Press Start 2P, como el resto.
/// La carga aditiva del fondo del menú (FondoMenu) no pasa por aquí.
/// </summary>
public class Cargador : MonoBehaviour
{
    /// <summary>Tiempo mínimo en pantalla: que se alcance a leer el nombre del piso aunque la escena cargue al instante.</summary>
    public const float MinimoSegundos = 1.1f;

    static Cargador instancia;
    public static bool Cargando => instancia != null && instancia.activo;

    static readonly string[] Consejos =
    {
        "Clic derecho sostenido: bloquear. Recibes solo el 8 % del daño, pero gasta stamina.",
        "Las sombras rojas del suelo avisan cada ataque.",
        "Shift: impulso. Atraviesa las barreras de energía.",
        "Las fuentes de curación restauran toda tu vida, pero cada una sirve una vez.",
        "Los minerales del abismo se gastan con el mercader: filo de runa o corazón de torre.",
        "Tres clics seguidos hacen un combo: el tercer golpe pega más.",
        "Si el piso te supera, el santuario del descanso te cura antes de seguir.",
    };

    // Cámara y oído de repuesto (hijos del Cargador, que sobrevive a las escenas): mientras carga, la escena de origen puede ya no tener cámara
    // (la del menú la crea MenuPrincipal.Start y al saltarlo no existe) o dejar de tenerla un cuadro al cambiar de escena. Sin ellas el Game view
    // dice "No cameras rendering" y la consola "There are no audio listeners". Se quitan en cuanto la escena nueva está activa.
    Camera camAux;
    AudioListener oidoAux;

    bool activo, modoTarjeta, tarjetaLista, tarjetaPulsada;
    float alfa, progreso, t, alfaTexto = 1f;
    string titulo = "", subtitulo = "", consejo = "";

    /// <summary>Carga la escena con la pantalla de carga. Si no está en Build Settings, lo avisa y se queda donde está.</summary>
    public static void Ir(string escena)
    {
        if (string.IsNullOrEmpty(escena)) return;
        if (!Application.CanStreamedLevelBeLoaded(escena)) { Debug.LogError("[Cargador] La escena \"" + escena + "\" no está en Build Settings."); return; }
        if (instancia == null)
        {
            var go = new GameObject("Cargador");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<Cargador>();
        }
        if (instancia.activo) return;   // ya hay una carga en curso
        instancia.StartCoroutine(instancia.Cargar(escena));
    }

    /// <summary>
    /// Carga con tarjeta: pantalla negra con el texto en dorado y "Presiona clic" parpadeando. La escena se carga en segundo plano
    /// mientras tanto y entra al pulsar clic, Espacio, Enter o A (si aún no está lista, espera a que lo esté). Se usa para el "Despertar".
    /// </summary>
    public static void IrConTarjeta(string escena, string texto, bool desdeNegro = false)
    {
        if (string.IsNullOrEmpty(escena)) return;
        if (!Application.CanStreamedLevelBeLoaded(escena)) { Debug.LogError("[Cargador] La escena \"" + escena + "\" no está en Build Settings."); return; }
        if (instancia == null)
        {
            var go = new GameObject("Cargador");
            DontDestroyOnLoad(go);
            instancia = go.AddComponent<Cargador>();
        }
        if (instancia.activo) return;
        instancia.StartCoroutine(instancia.CargarConTarjeta(escena, texto, desdeNegro));
    }

    static bool EntradaDeAvance()
    {
        var kb = Keyboard.current; var mouse = Mouse.current; var pad = Gamepad.current;
        return (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            || (mouse != null && mouse.leftButton.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
    }

    void CrearAuxiliares()
    {
        if (camAux != null) return;
        var go = new GameObject("CamaraCarga");
        go.transform.SetParent(transform, false);
        camAux = go.AddComponent<Camera>();
        camAux.clearFlags = CameraClearFlags.SolidColor;
        camAux.backgroundColor = Color.black;
        camAux.cullingMask = 0;      // no dibuja nada: solo el negro de fondo
        camAux.depth = -100f;        // por debajo de cualquier otra cámara
        camAux.allowHDR = false; camAux.allowMSAA = false;
        go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = false;
        oidoAux = go.AddComponent<AudioListener>();
        ActualizarOido();
    }

    /// <summary>El oído de repuesto solo suena si no hay otro AudioListener activo (dos a la vez dan un aviso).</summary>
    void ActualizarOido()
    {
        if (oidoAux == null) return;
        bool hayOtro = false;
        foreach (var l in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (l != oidoAux && l.enabled && l.gameObject.activeInHierarchy) { hayOtro = true; break; }
        if (oidoAux.enabled == hayOtro) oidoAux.enabled = !hayOtro;
    }

    void QuitarAuxiliares()
    {
        if (camAux != null) Destroy(camAux.gameObject);   // se lleva también su AudioListener
        camAux = null; oidoAux = null;
    }

    IEnumerator CargarConTarjeta(string escena, string texto, bool desdeNegro)
    {
        CrearAuxiliares();
        activo = true; modoTarjeta = true; titulo = texto; tarjetaLista = false; tarjetaPulsada = false;
        progreso = 0f; t = 0f; alfaTexto = 1f;
        Time.timeScale = 0f;
        if (!desdeNegro) for (float f = 0f; f < 0.6f; f += Time.unscaledDeltaTime) { alfa = f / 0.6f; t += Time.unscaledDeltaTime; yield return null; }
        alfa = 1f;   // al abrir el juego se empieza ya en negro: ni un cuadro de la escena de atrás

        var op = SceneManager.LoadSceneAsync(escena);
        op.allowSceneActivation = false;
        float inicio = Time.unscaledTime;
        while (true)
        {
            if (Time.frameCount % 8 == 0) ActualizarOido();
            tarjetaLista = op.progress >= 0.9f;
            // el clic con el que se pulsó Jugar no cuenta: se ignora el primer medio segundo; uno anticipado se recuerda
            if (Time.unscaledTime - inicio > 0.5f && EntradaDeAvance()) tarjetaPulsada = true;
            if (tarjetaPulsada && tarjetaLista) break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        for (float f = 0f; f < 0.5f; f += Time.unscaledDeltaTime) { alfaTexto = 1f - f / 0.5f; t += Time.unscaledDeltaTime; yield return null; }
        alfaTexto = 0f;
        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;
        Time.timeScale = 1f;
        for (int i = 0; i < 3; i++) yield return null;
        QuitarAuxiliares();   // la escena nueva ya trae su cámara y su oído
        for (float f = 0f; f < 0.6f; f += Time.unscaledDeltaTime) { alfa = 1f - f / 0.6f; yield return null; }   // la escena nueva empieza en negro y se aclara sola
        alfa = 0f; alfaTexto = 1f;
        modoTarjeta = false; activo = false;
    }

    IEnumerator Cargar(string escena)
    {
        CrearAuxiliares();
        activo = true;
        Nombre(escena, out titulo, out subtitulo);
        consejo = Consejos[Random.Range(0, Consejos.Length)];
        progreso = 0f; t = 0f;
        Time.timeScale = 0f;   // el mundo de atrás se queda quieto mientras carga
        for (float f = 0f; f < 0.25f; f += Time.unscaledDeltaTime) { alfa = f / 0.25f; t += Time.unscaledDeltaTime; yield return null; }
        alfa = 1f;

        var op = SceneManager.LoadSceneAsync(escena);
        op.allowSceneActivation = false;
        float inicio = Time.unscaledTime;
        while (op.progress < 0.9f || Time.unscaledTime - inicio < MinimoSegundos)
        {
            float meta = Mathf.Clamp01(op.progress / 0.9f) * Mathf.Clamp01((Time.unscaledTime - inicio) / MinimoSegundos);   // la barra también avanza con el tiempo mínimo
            if (Time.frameCount % 8 == 0) ActualizarOido();
            progreso = Mathf.MoveTowards(progreso, meta, Time.unscaledDeltaTime * 1.5f);
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        progreso = 1f;
        op.allowSceneActivation = true;
        while (!op.isDone) { t += Time.unscaledDeltaTime; yield return null; }
        Time.timeScale = 1f;
        for (int i = 0; i < 3; i++) yield return null;   // la escena nueva arranca debajo
        QuitarAuxiliares();   // ya trae su cámara y su oído
        for (float f = 0f; f < 0.45f; f += Time.unscaledDeltaTime) { alfa = 1f - f / 0.45f; t += Time.unscaledDeltaTime; yield return null; }
        alfa = 0f;
        activo = false;
    }

    /// <summary>Nombre del piso para la pantalla de carga (título grande y subtítulo).</summary>
    static void Nombre(string escena, out string titulo, out string sub)
    {
        sub = "";
        switch (escena)
        {
            case "Menu": titulo = "MENÚ PRINCIPAL"; break;
            case "Intro": titulo = "PRÓLOGO"; sub = Historia.Ciudad + ", 2030"; break;
            case "Expedicion": titulo = "PISO 20"; sub = "La expedición"; break;
            case "Exterior": titulo = Historia.Ciudad.ToUpperInvariant(); sub = "Al pie de la torre"; break;
            case "Diorama": titulo = "PISO 1"; sub = "El Pueblo del Risco"; break;
            case "Puerto": titulo = "PISO 2"; sub = "El Puerto de las Mareas"; break;
            case "Cascadas": titulo = "PISO 3"; sub = "El Valle de las Cascadas"; break;
            case "Acantilado": titulo = "PISO 4"; sub = "El Sendero del Acantilado"; break;
            case "Cima": titulo = "PISO 5"; sub = "La cima de la torre"; break;
            case "CuartosA": titulo = "CUARTOS"; sub = "Pereza · La Biblioteca Dormida"; break;
            case "CuartosB": titulo = "CUARTOS"; sub = "Gula · Los Salones del Banquete"; break;
            case "CuartosC": titulo = "CUARTOS"; sub = "Avaricia · La Bóveda del Acaparador"; break;
            case "CuartosD": titulo = "CUARTOS"; sub = "Envidia · La Galería de los Espejos"; break;
            default:
                titulo = escena.StartsWith("Nivel") ? "PISO " + escena.Substring(5) : escena.ToUpperInvariant();
                break;
        }
    }

    void OnGUI()
    {
        if (alfa <= 0.001f) return;
        GUI.depth = -3000;   // encima de todo
        float sw = Screen.width, sh = Screen.height, u = sh / 100f;
        void Caja(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); }
        void Texto(Rect r, string s, GUIStyle st, Color c)
        {
            GUI.color = Color.white;
            st.normal.textColor = new Color(0f, 0f, 0f, c.a * 0.8f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), s, st);
            st.normal.textColor = c;
            GUI.Label(r, s, st);
        }

        if (modoTarjeta)
        {
            Caja(new Rect(0, 0, sw, sh), new Color(0f, 0f, 0f, alfa));
            var grande = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 9f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            Texto(new Rect(0, sh * 0.38f, sw, u * 13f), titulo, grande, new Color(1f, 0.85f, 0.5f, alfa * alfaTexto));
            var pide = new GUIStyle(grande) { fontSize = FuentePixel.Tam(u * 2.6f), fontStyle = FontStyle.Normal };
            float parpadeo = 0.3f + 0.7f * Mathf.Abs(Mathf.Sin(t * 2.2f));
            if (tarjetaPulsada && !tarjetaLista) Texto(new Rect(0, sh * 0.82f, sw, u * 5f), "Cargando" + new string('.', 1 + (int)(t * 2.5f) % 3), pide, new Color(1f, 1f, 1f, 0.7f * alfa * alfaTexto));
            else Texto(new Rect(0, sh * 0.82f, sw, u * 5f), "Presiona clic", pide, new Color(1f, 1f, 1f, parpadeo * alfa * alfaTexto));
            GUI.color = Color.white;
            return;
        }
        Caja(new Rect(0, 0, sw, sh), new Color(0.04f, 0.025f, 0.08f, alfa));
        Caja(new Rect(0, sh * 0.35f, sw, sh * 0.3f), new Color(0.09f, 0.05f, 0.16f, 0.55f * alfa));   // franja violeta de en medio

        // rombo de runas que gira despacio (dos cuadrados girados 45°, uno en cada sentido)
        var centro = new Vector2(sw / 2f, sh * 0.33f);
        for (int i = 0; i < 2; i++)
        {
            float lado = u * (i == 0 ? 7f : 4.6f);
            var prev = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f + t * (i == 0 ? 25f : -40f), centro);
            Caja(new Rect(centro.x - lado / 2f, centro.y - lado / 2f, lado, lado), new Color(0.75f, 0.55f, 1f, (i == 0 ? 0.35f : 0.8f) * alfa));
            Caja(new Rect(centro.x - lado / 2f + 3f, centro.y - lado / 2f + 3f, lado - 6f, lado - 6f), new Color(0.04f, 0.025f, 0.08f, alfa));
            GUI.matrix = prev;
        }

        var tit = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 6.5f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        Texto(new Rect(0, sh * 0.42f, sw, u * 9f), titulo, tit, new Color(1f, 0.92f, 0.75f, alfa));
        if (!string.IsNullOrEmpty(subtitulo))
        {
            var sub = new GUIStyle(tit) { fontSize = FuentePixel.Tam(u * 2.8f), fontStyle = FontStyle.Italic };
            Texto(new Rect(0, sh * 0.42f + u * 9f, sw, u * 4f), subtitulo, sub, new Color(0.85f, 0.78f, 1f, alfa));
        }

        // barra de progreso con el mismo marco dorado y violeta del HUD
        float bw = Mathf.Min(sw * 0.5f, u * 90f), bh = u * 2f, bx = (sw - bw) / 2f, by = sh * 0.66f;
        Caja(new Rect(bx - 3, by - 3, bw + 6, bh + 6), new Color(0.75f, 0.6f, 1f, 0.9f * alfa));
        Caja(new Rect(bx, by, bw, bh), new Color(0.08f, 0.06f, 0.14f, alfa));
        Caja(new Rect(bx, by, bw * progreso, bh), new Color(1f, 0.85f, 0.55f, alfa));
        var cargando = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 2.2f), alignment = TextAnchor.MiddleCenter };
        Texto(new Rect(0, by + bh + u * 1.2f, sw, u * 3f), "Cargando" + new string('.', 1 + (int)(t * 2.5f) % 3), cargando, new Color(1f, 1f, 1f, 0.85f * alfa));

        var tip = new GUIStyle(cargando) { fontSize = FuentePixel.Tam(u * 1.9f), wordWrap = true };
        Texto(new Rect(sw * 0.2f, sh - u * 14f, sw * 0.6f, u * 8f), consejo, tip, new Color(0.9f, 0.85f, 1f, 0.8f * alfa));
        GUI.color = Color.white;
    }
}
