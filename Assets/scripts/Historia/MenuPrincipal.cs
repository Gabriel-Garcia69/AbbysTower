using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú principal (GDD): Jugar/Continuar · Nueva partida · Configuración · Créditos · Salir (pide confirmación). Diseño de
/// la maqueta: título en dos líneas a la izquierda, menú sin panel, la torre grande y nítida a la derecha y las teclas abajo. De fondo van pasando paisajes de algunos pisos (FondoMenu: puerto, cascadas, acantilado y cima) con
/// la cámara paneando despacio. Va en la escena "Menu" (vacía: solo este objeto). Jugar abre la cinemática inicial.
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    public string escenaJugar = "Expedicion";   // Menú → tarjeta "Despertar" → Expedicion (tutorial) → Intro → Exterior → Diorama...
    public Vector3 centro = new Vector3(22f, 2f, 8f);   // la bahía, entre el muelle y el faro
    public float radio = 52f, altura = 15f;
    [Tooltip("A qué altura sobre el centro mira la cámara.")]
    public float alturaMira = 3f;
    [Tooltip("Radianes por segundo que gira la cámara.")]
    public float giro = 0.035f;

    [Header("Arranque")]
    [Tooltip("Al abrir el juego se va directo a la tarjeta \"Despertar\" y al tutorial, sin mostrar el menú. El menú solo se ve al volver desde la pausa o tras el final.")]
    public bool saltarMenuAlIniciar = false;   // GDD: Inicio → Menú → Jugar
    [Tooltip("En el editor el menú se sigue viendo (para probarlo); marca esto para que el editor también arranque saltándolo.")]
    public bool saltarEnEditor = false;

    /// <summary>¿Ya pasó el arranque del juego en esta ejecución? Si es así, el menú se muestra normal (venimos de la pausa o del final).</summary>
    static bool yaIniciado;
    bool saltando;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() { yaIniciado = false; }   // con "Enter Play Mode" sin recargar el dominio, cada ejecución cuenta como nueva

    enum Pagina { Principal, Configuracion, Creditos, Salir }
    Pagina pagina;
    int sel;
    float t, entrada;
    Camera cam;
    FondoMenu fondo;

    void Awake()
    {
        // antes de que la cinemática empiece (su Start corre después de todos los Awake)
        foreach (var c in FindObjectsByType<Cinematica>()) Destroy(c);
        ReintentoJefe.Escena = null;   // empezar de nuevo desde el menú
        // el fondo es un mapa jugable: sin Abby, sin peleas ni HUD del piso
        var fd = FindAnyObjectByType<FloorDirector>();
        if (fd != null) fd.gameObject.SetActive(false);
        var abby = GameObject.FindWithTag("Player");
        if (abby != null) abby.SetActive(false);
        foreach (var e in FindObjectsByType<RoomEncounter>()) e.enabled = false;
    }

    void Start()
    {
        bool primera = !yaIniciado;
        yaIniciado = true;
        // con partida guardada sí se muestra el menú (para poder continuar)
        if (primera && saltarMenuAlIniciar && !Progreso.Hay && (!Application.isEditor || saltarEnEditor))
        {
            // primera vez en esta ejecución: ni se dibuja el menú, solo la tarjeta "Despertar" sobre negro y de ahí el tutorial
            saltando = true;
            RunState.ClearHealth();
            Musica.Poner(Musica.Pista.Ninguna);
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            Time.timeScale = 1f;
            Cargador.IrConTarjeta(escenaJugar, "DESPERTAR", true);
            return;
        }
        // sin HUD del piso ni cámara de juego (esa sigue a Abby y pisaría la del menú): una cámara propia, como la cinemática
        var fd = FindAnyObjectByType<FloorDirector>();
        if (fd != null) fd.gameObject.SetActive(false);
        var main = Camera.main;
        var go = new GameObject("CamaraMenu");
        cam = go.AddComponent<Camera>();
        if (main != null) { cam.CopyFrom(main); main.transform.root.gameObject.SetActive(false); }
        cam.nearClipPlane = 0.3f; cam.farClipPlane = 3000f;
        fondo = gameObject.AddComponent<FondoMenu>();   // los paisajes que van cambiando
        fondo.cam = cam;
        go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>().renderPostProcessing = true;
        go.AddComponent<AudioListener>();
        go.tag = "MainCamera";
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (saltando) return;
        // la escena es una copia de la Intro: si su cinemática aparece (al llegar desde otro piso se vuelve a crear), fuera
        if (t < 3f) foreach (var c in FindObjectsByType<Cinematica>()) { c.StopAllCoroutines(); c.enabled = false; Destroy(c.gameObject); }
        t += Time.deltaTime; entrada += Time.deltaTime;
        if (cam != null && fondo == null)
        {
            float a = t * giro;
            cam.transform.position = centro + new Vector3(Mathf.Sin(a) * radio, altura, -Mathf.Cos(a) * radio);
            cam.transform.rotation = Quaternion.LookRotation(centro + Vector3.up * alturaMira - cam.transform.position);
        }
        var kb = Keyboard.current;
        if (kb == null) return;
        int n = Opciones().Length;
        if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) { sel = (sel + 1) % n; Sonido.Tocar(Sonido.Efecto.Clic, 0.6f); }
        if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) { sel = (sel + n - 1) % n; Sonido.Tocar(Sonido.Efecto.Clic, 0.6f); }
        if (pagina == Pagina.Configuracion && MenuConfiguracion.Ajustable(sel))
        {
            int paso = kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame ? 1 : kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame ? -1 : 0;
            if (paso != 0) MenuConfiguracion.Ajustar(sel, paso);
        }
        if (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) Elegir(sel);
        if (kb.escapeKey.wasPressedThisFrame) { if (pagina != Pagina.Principal) Ir(Pagina.Principal); else Ir(Pagina.Salir); }
    }

    string[] Opciones()
    {
        switch (pagina)
        {
            case Pagina.Configuracion: return MenuConfiguracion.Opciones();   // la misma que en la pausa
            case Pagina.Salir: return new[] { "No, quedarme", "Sí, salir del juego" };
            case Pagina.Creditos: return new[] { "Volver" };
            default: return Progreso.Hay
                ? new[] { "Continuar  ·  " + Progreso.Nombre, "Nueva partida", "Configuración", "Créditos", "Salir" }
                : new[] { "Jugar", "Configuración", "Créditos", "Salir" };
        }
    }

    void Ir(Pagina p) { pagina = p; sel = 0; entrada = 0f; }

    void Elegir(int i)
    {
        Sonido.Tocar(Sonido.Efecto.Clic);
        switch (pagina)
        {
            case Pagina.Principal:
                if (Progreso.Hay)
                {
                    // con partida guardada: Continuar (al último lugar) · Nueva partida · y el resto corrido un lugar
                    if (i == 0) { RunState.ClearHealth(); Cargador.Ir(Progreso.Escena); break; }
                    i--;
                }
                if (i == 0) { Progreso.Borrar(); RunState.ClearHealth(); Cargador.IrConTarjeta(escenaJugar, "DESPERTAR"); }
                else if (i == 1) Ir(Pagina.Configuracion);
                else if (i == 2) Ir(Pagina.Creditos);
                else Ir(Pagina.Salir);
                break;
            case Pagina.Creditos:
                Ir(Pagina.Principal);
                break;
            case Pagina.Configuracion:
                if (MenuConfiguracion.Elegir(i)) Ir(Pagina.Principal);
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
        if (saltando) return;
        GUI.depth = -900;
        float sw = Screen.width, sh = Screen.height, u = sh / 100f;
        if (pagina == Pagina.Creditos)
        {
            // créditos sobre el fondo del menú, con un velo oscuro; clic o cualquier tecla de aceptar regresa
            GUI.color = new Color(0.02f, 0.01f, 0.05f, 0.82f); GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
            DanteFinal.DibujarCreditos(entrada, false);
            var ayudaC = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 1.8f), alignment = TextAnchor.MiddleCenter };
            Sombra(new Rect(0, sh - u * 6f, sw, u * 4f), "Esc  regresar", ayudaC, new Color(1f, 1f, 1f, 0.7f));
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0) { Ir(Pagina.Principal); Event.current.Use(); }
            GUI.color = Color.white;
            return;
        }
        // ── diseño de la maqueta (base 1280×720; todo escala con la altura y se ancla a su lado) ──
        float S = sh / 720f, a = Mathf.Clamp01(entrada / 0.25f), aT = Mathf.Clamp01(t / 0.8f);
        Preparar();

        // fondo: la escena 3D se queda; encima un velo y una viñeta para que se lean el texto y la torre
        Pintar(new Rect(0, 0, sw, sh), Texture2D.whiteTexture, new Color(0.043f, 0.024f, 0.094f, 0.30f));
        Pintar(new Rect(0, 0, Mathf.Max(sw * 0.62f, 720f * S), sh), Degradado.Horizontal, new Color(0.043f, 0.024f, 0.094f, 0.78f));   // #0B0618 a la izquierda
        Pintar(new Rect(0, 0, sw, sh), vineta, new Color(0.02f, 0.01f, 0.05f, 0.75f));
        Pintar(new Rect(0, sh - 70f * S, sw, 70f * S), Arriba, new Color(0.03f, 0.015f, 0.06f, 0.85f));

        // derecha: la torre, nítida (sin suavizado), con un resplandor morado detrás y su sombra morada
        var torre = EmblemaTorre.Logo;
        if (torre != null)
        {
            // más chica que antes (tapaba medio paisaje) y nunca más de un tercio del ancho
            float prop = (float)torre.width / torre.height;
            float th = Mathf.Min(540f * S, sw * 0.32f / prop), tw = th * prop;
            float tx = sw - 64f * S - tw, ty = (sh - th) / 2f - 6f * S + Mathf.Sin(t * 1.1f) * 3f * S;
            float cx = tx + tw / 2f, cy = ty + th * 0.5f;
            float pulso = 0.85f + 0.15f * Mathf.Sin(t * 1.6f);
            // un nicho oscuro: el paisaje se apaga alrededor de la torre en vez de chocar con ella
            float ancho = Mathf.Max(sw * 0.5f, tw * 2.2f);
            Pintar(new Rect(sw - ancho, 0, ancho, sh), Degradado.Horizontal, new Color(0.043f, 0.024f, 0.094f, 0.75f * aT), espejo: true);
            float nr = th * 1.75f;
            Pintar(new Rect(cx - nr / 2f, cy - nr / 2f, nr, nr), Halo, new Color(0.043f, 0.024f, 0.094f, 0.95f * aT));
            // el resplandor morado y la sombra en el piso, para que la torre quede asentada
            float gr = th * 1.2f;
            Pintar(new Rect(cx - gr / 2f, cy - gr / 2f, gr, gr), Halo, new Color(190f / 255f, 90f / 255f, 1f, 0.45f * pulso * aT));
            Pintar(new Rect(cx - tw * 0.75f, ty + th * 0.9f, tw * 1.5f, th * 0.16f), Halo, new Color(0.02f, 0f, 0.05f, 0.8f * aT));
            float sombra = 8f * S;
            Pintar(new Rect(tx - sombra, ty - sombra * 0.4f, tw + sombra * 2f, th + sombra * 1.4f), torre, new Color(0.55f, 0.2f, 0.95f, 0.3f * aT), true);
            Pintar(new Rect(tx, ty, tw, th), torre, new Color(1f, 1f, 1f, aT), true);
        }

        // izquierda: el título en dos líneas, con sombra escalonada de pixel y un brillo oscuro detrás
        float x0 = 84f * S;
        Pintar(new Rect(x0 - 70f * S, 30f * S, 520f * S, 260f * S), Halo, new Color(0.02f, 0.0f, 0.05f, 0.55f));
        tituloSt.fontSize = Mathf.Max(8, Mathf.RoundToInt(56f * S));
        foreach (var (linea, yl) in new[] { ("ABBY'S", 86f), ("TOWER", 168f) })
        {
            var r = new Rect(x0, yl * S, 700f * S, 72f * S);
            Texto(new Rect(r.x + 6f * S, r.y + 6f * S, r.width, r.height), linea, tituloSt, new Color(0.29f, 0.11f, 0.227f, aT));   // #4A1C3A
            Texto(new Rect(r.x + 3f * S, r.y + 3f * S, r.width, r.height), linea, tituloSt, new Color(0.706f, 0.337f, 0.11f, aT));   // #B4561C
            Texto(r, linea, tituloSt, new Color(1f, 0.89f, 0.627f, aT));   // #FFE3A0
        }
        subSt.fontSize = Mathf.Max(8, Mathf.RoundToInt(22f * S));
        Espaciado(x0, 272f * S, "La torre del abismo", subSt, 2.2f * S, new Color(0.788f, 0.706f, 1f, aT), true);   // #C9B4FF

        // el menú: texto a la izquierda, sin panel; el elegido en dorado con barra y degradado
        var ops = Opciones();
        bool config = pagina == Pagina.Configuracion;
        float fila = config ? 40f : 52f, letra = config ? 22f : 30f, letraSel = config ? 25f : 34f;
        float y0 = (config ? 348f : 362f) * S;
        if (pagina == Pagina.Salir)
        {
            itemSt.fontSize = Mathf.Max(8, Mathf.RoundToInt(22f * S));
            Texto(new Rect(x0, y0 - 6f * S, 600f * S, 30f * S), "¿Salir del juego?", itemSt, new Color(0.788f, 0.706f, 1f, a));
            y0 += 36f * S;
        }
        var ev = Event.current;
        float y = y0;
        for (int i = 0; i < ops.Length; i++)
        {
            bool on = i == sel;
            string texto = ops[i], segunda = null;
            if (pagina == Pagina.Principal && Progreso.Hay && i == 0) { texto = "Continuar"; segunda = Progreso.Nombre.ToUpperInvariant(); }
            float alto = (fila + (segunda != null ? 16f : 0f)) * S;
            var r = new Rect(x0 - 2f * S, y, 560f * S, alto);
            if (ev.type == EventType.MouseMove && r.Contains(ev.mousePosition) && sel != i) { sel = i; Sonido.Tocar(Sonido.Efecto.Clic, 0.4f); }
            if (ev.type == EventType.MouseDown && ev.button == 0 && r.Contains(ev.mousePosition))
            {
                sel = i;
                if (config && MenuConfiguracion.Ajustable(i)) MenuConfiguracion.Ajustar(i, ev.mousePosition.x > r.center.x ? 1 : -1);
                else Elegir(i);
                ev.Use();
                return;
            }
            if (on)
            {
                // barra vertical #FFC24A y degradado dorado → transparente
                Pintar(new Rect(r.x, r.y + 4f * S, 520f * S, alto - 8f * S), Degradado.Horizontal, new Color(1f, 0.76f, 0.29f, 0.22f * a));
                Pintar(new Rect(r.x, r.y + 4f * S, Mathf.Max(2f, 4f * S), alto - 8f * S), Texture2D.whiteTexture, new Color(1f, 0.76f, 0.29f, a));
            }
            float tx = x0 + 38f * S + (on ? 2f * S * Mathf.Clamp01(entrada / 0.15f) : 0f);
            itemSt.font = on ? fuenteSel : fuenteItem;
            itemSt.fontSize = Mathf.Max(8, Mathf.RoundToInt((on ? letraSel : letra) * S));
            bool apagado = (pagina == Pagina.Principal && i == ops.Length - 1) || (config && i == MenuConfiguracion.Volver && !on);
            Color c = on ? new Color(1f, 0.89f, 0.627f) : apagado ? new Color(0.616f, 0.565f, 0.784f) : new Color(0.914f, 0.894f, 1f);   // #FFE3A0 / #9D90C8 / #E9E4FF
            if (config && i == MenuConfiguracion.Dificultad && !on) c = Color.Lerp(c, MenuConfiguracion.ColorDificultad, 0.45f);
            c.a *= a;
            float alturaTexto = fila * S;
            Texto(new Rect(tx, y + 1f * S, 900f * S, alturaTexto), texto, itemSt, c, sombra: true);
            if (segunda != null)
            {
                pisoSt.fontSize = Mathf.Max(7, Mathf.RoundToInt(12f * S));
                Espaciado(tx + 2f * S, y + alturaTexto - 12f * S, segunda, pisoSt, 1.6f * S, new Color(1f, 0.76f, 0.29f, (on ? 1f : 0.7f) * a), false);   // #FFC24A
            }
            y += alto;
        }

        // ayuda de la opción (configuración) bajo la lista
        string nota = config ? MenuConfiguracion.Ayuda(sel) : null;
        if (nota != null)
        {
            notaSt.fontSize = Mathf.Max(8, Mathf.RoundToInt(15f * S));
            Texto(new Rect(x0 + 38f * S, y + 6f * S, 620f * S, 24f * S), nota, notaSt,
                  sel == MenuConfiguracion.Dificultad ? MenuConfiguracion.ColorDificultad * new Color(1f, 1f, 1f, a) : new Color(0.79f, 0.76f, 0.91f, a));
        }

        // barra de ayuda: teclas como "keycaps"
        float hx = x0 - 2f * S, hy = sh - 40f * S;
        foreach (var (tecla, accion) in new[] { ("↑↓", "elegir"), ("Enter", "aceptar"), ("←→", config ? "ajustar" : "volumen"), ("Esc", "regresar") })
            hx = Keycap(hx, hy, tecla, accion, S);
        GUI.color = Color.white;
    }

    // ───────────────────────────── utilidades de dibujo del menú ─────────────────────────────

    static GUIStyle tituloSt, subSt, itemSt, pisoSt, notaSt, capSt, capTextoSt, capBordeSt;
    static Font fuenteItem, fuenteSel;
    static Texture2D vineta, halo, arriba, borde;
    static Texture2D Halo => halo;
    static Texture2D Arriba => arriba;

    static void Preparar()
    {
        if (tituloSt != null) return;
        fuenteItem = Resources.Load<Font>("Fuentes/PixelifySans-Medium");
        fuenteSel = Resources.Load<Font>("Fuentes/PixelifySans-SemiBold") ?? fuenteItem;
        var inter = Resources.Load<Font>("Fuentes/Inter-SemiBold");
        tituloSt = new GUIStyle(GUI.skin.label) { font = FuentePixel.FuenteLogo, alignment = TextAnchor.UpperLeft, wordWrap = false, clipping = TextClipping.Overflow, richText = false };
        subSt = new GUIStyle(tituloSt) { font = fuenteItem, fontStyle = FontStyle.Italic };
        itemSt = new GUIStyle(tituloSt) { font = fuenteItem, alignment = TextAnchor.MiddleLeft };
        pisoSt = new GUIStyle(tituloSt) { font = fuenteSel };
        notaSt = new GUIStyle(itemSt) { fontStyle = FontStyle.Italic };
        capSt = new GUIStyle(tituloSt) { font = inter, alignment = TextAnchor.MiddleCenter };   // Pixelify no trae flechas: las teclas van en Inter
        capTextoSt = new GUIStyle(tituloSt) { font = fuenteItem, alignment = TextAnchor.MiddleLeft };
        vineta = Radial(128, d => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.35f, d)));
        halo = Radial(128, d => Mathf.Pow(Mathf.Clamp01(1f - d), 2f));
        arriba = Lineal(k => k);   // transparente arriba, oscuro abajo
        borde = Redondeado(16, 4f, 1f);
        capBordeSt = new GUIStyle { normal = { background = borde }, border = new RectOffset(6, 6, 6, 6) };
    }

    static void Pintar(Rect r, Texture tex, Color c, bool nitido = false, bool espejo = false)
    {
        if (Event.current.type != EventType.Repaint || tex == null) return;
        if (nitido && tex is Texture2D t2 && t2.filterMode != FilterMode.Point) t2.filterMode = FilterMode.Point;   // pixel art sin suavizado
        GUI.color = c;
        if (espejo) GUI.DrawTextureWithTexCoords(r, tex, new Rect(1f, 0f, -1f, 1f), true);   // el degradado al revés (oscuro a la derecha)
        else GUI.DrawTexture(r, tex, nitido ? ScaleMode.ScaleToFit : ScaleMode.StretchToFill, true);
        GUI.color = Color.white;
    }

    static void Texto(Rect r, string s, GUIStyle st, Color c, bool sombra = false)
    {
        GUI.color = Color.white;
        if (sombra)
        {
            st.normal.textColor = st.hover.textColor = new Color(0f, 0f, 0f, c.a * 0.6f);
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), s, st);
        }
        st.normal.textColor = st.hover.textColor = c;
        GUI.Label(r, s, st);
    }

    /// <summary>Texto con espacio entre letras (OnGUI no tiene letter-spacing): letra por letra.</summary>
    static void Espaciado(float x, float y, string s, GUIStyle st, float espacio, Color c, bool sombra)
    {
        foreach (char ch in s)
        {
            var cont = new GUIContent(ch.ToString());
            var tam = st.CalcSize(cont);
            if (ch != ' ')
            {
                if (sombra) { st.normal.textColor = st.hover.textColor = new Color(0f, 0f, 0f, c.a * 0.6f); GUI.Label(new Rect(x + 2f, y + 2f, tam.x, tam.y), cont, st); }
                st.normal.textColor = st.hover.textColor = c;
                GUI.Label(new Rect(x, y, tam.x, tam.y), cont, st);
            }
            x += (ch == ' ' ? tam.x * 0.9f + st.fontSize * 0.25f : tam.x) + espacio;
        }
    }

    /// <summary>Una tecla con borde redondeado (#8A7CB8) y su acción al lado. Devuelve dónde sigue la siguiente.</summary>
    static float Keycap(float x, float y, string tecla, string accion, float S)
    {
        capSt.fontSize = Mathf.Max(7, Mathf.RoundToInt(12f * S));
        capTextoSt.fontSize = Mathf.Max(7, Mathf.RoundToInt(13f * S));
        float w = Mathf.Max(26f * S, capSt.CalcSize(new GUIContent(tecla)).x + 14f * S), h = 20f * S;
        var r = new Rect(x, y, w, h);
        if (Event.current.type == EventType.Repaint)
        {
            GUI.color = new Color(0.04f, 0.02f, 0.09f, 0.55f); GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = new Color(0.541f, 0.486f, 0.722f, 1f); capBordeSt.Draw(r, false, false, false, false);
            GUI.color = Color.white;
        }
        Texto(r, tecla, capSt, new Color(0.93f, 0.91f, 1f));
        float aw = capTextoSt.CalcSize(new GUIContent(accion)).x;
        Texto(new Rect(x + w + 7f * S, y, aw + 4f, h), accion, capTextoSt, new Color(0.79f, 0.76f, 0.91f));
        return x + w + 7f * S + aw + 22f * S;
    }

    static Texture2D Radial(int n, System.Func<float, float> alfa)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                px[y * n + x] = new Color(1f, 1f, 1f, alfa(Mathf.Sqrt(dx * dx + dy * dy)));
            }
        tex.SetPixels(px); tex.Apply();
        return tex;
    }

    static Texture2D Lineal(System.Func<float, float> alfa)
    {
        var tex = new Texture2D(2, 256, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        for (int y = 0; y < 256; y++) { float k = 1f - y / 255f; var c = new Color(1f, 1f, 1f, alfa(k)); tex.SetPixel(0, y, c); tex.SetPixel(1, y, c); }
        tex.Apply();
        return tex;
    }

    /// <summary>Solo el borde de un rectángulo redondeado (blanco, para teñir).</summary>
    static Texture2D Redondeado(int n, float radio, float linea)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        float half = n / 2f;
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - half) - (half - radio), qy = Mathf.Abs(y + 0.5f - half) - (half - radio);
                float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radio;
                float aa = Mathf.Clamp01(0.5f - d) * Mathf.Clamp01(0.5f + d + linea);
                px[y * n + x] = new Color(1f, 1f, 1f, aa);
            }
        tex.SetPixels(px); tex.Apply();
        return tex;
    }

    static void Sombra(Rect r, string s, GUIStyle st, Color c)
    {
        GUI.color = Color.white;
        st.normal.textColor = st.hover.textColor = new Color(0f, 0f, 0f, c.a * 0.8f);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), s, st);
        st.normal.textColor = st.hover.textColor = c;
        GUI.Label(r, s, st);
    }
}

/// <summary>
/// Configuración que se guarda (GDD: solo el volumen general y el de música). Si falla la lectura, quedan los valores por
/// defecto. La música (Francisco) debe multiplicar su volumen por Ajustes.Musica.
/// </summary>
public static class Ajustes
{
    const float PorDefectoGeneral = 0.8f, PorDefectoMusica = 0.7f, PorDefectoEfectos = 0.9f;

    public static float Efectos
    {
        get => PlayerPrefs.GetFloat("vol_efectos", PorDefectoEfectos);
        set { PlayerPrefs.SetFloat("vol_efectos", Mathf.Clamp01(Mathf.Round(value * 10f) / 10f)); PlayerPrefs.Save(); }
    }

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

    public static void Restablecer() { General = PorDefectoGeneral; Musica = PorDefectoMusica; Efectos = PorDefectoEfectos; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Aplicar() => AudioListener.volume = General;
}
