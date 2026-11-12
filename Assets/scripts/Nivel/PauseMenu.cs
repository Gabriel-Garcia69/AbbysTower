using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú de pausa (Esc / P / Start), con el mismo estilo que el menú principal: el emblema de la torre en la esquina,
/// "PAUSA" en la letra del título, el marco dorado y la franja morada en la opción elegida.
/// Continuar · Configuración (la misma del menú: volúmenes, dificultad, pantalla completa) · Reiniciar piso · Salir al
/// menú (pide confirmación). Se maneja con mouse, teclado (↑↓ / WS, ← → ajusta, Enter) o mando (cruceta/stick, A, B).
/// Congela el juego, apaga la cámara de mouse y libera el cursor. No se abre con la tienda o la cinemática activas.
/// Corre después de los demás scripts: al cerrar con Enter / A, Abby no ataca ni hace el impulso en ese mismo cuadro.
/// </summary>
[DefaultExecutionOrder(500)]
public class PauseMenu : MonoBehaviour
{
    public string exteriorScene = "Exterior";

    public static bool IsPaused { get; private set; }
    /// <summary>Otros menús (tienda) lo ponen en true para que Esc no abra la pausa.</summary>
    public static bool Blocked;

    enum Pagina { Principal, Configuracion, Salir }
    Pagina pagina;
    int sel;
    float abierto, entrada, repetir;
    MouseOrbitCamera[] orbit;
    float savedScale = 1f;
    CursorLockMode cursorAntes;
    bool cursorVisibleAntes;

    void OnDisable() { if (IsPaused) Resume(); }

    // ───────────────────────────── Entrada ─────────────────────────────

    void Update()
    {
        var kb = Keyboard.current; var gp = Gamepad.current;
        bool toggle = (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)) || (gp != null && gp.startButton.wasPressedThisFrame);
        if (!IsPaused)
        {
            if (toggle && !Blocked && !IntroPlaying()) Pause();
            return;
        }
        abierto += Time.unscaledDeltaTime; entrada += Time.unscaledDeltaTime;

        if (toggle || (gp != null && gp.buttonEast.wasPressedThisFrame))
        {
            Sonido.Tocar(Sonido.Efecto.Clic, 0.6f);
            if (pagina != Pagina.Principal) Ir(Pagina.Principal, pagina == Pagina.Configuracion ? 1 : 3);
            else Resume();
            return;
        }

        int n = Opciones().Length, dv = 0, dh = 0;
        if (kb != null)
        {
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) dv = -1;
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) dv = 1;
            if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame) dh = -1;
            if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) dh = 1;
        }
        if (gp != null)
        {
            if (gp.dpad.up.wasPressedThisFrame) dv = -1;
            if (gp.dpad.down.wasPressedThisFrame) dv = 1;
            if (gp.dpad.left.wasPressedThisFrame) dh = -1;
            if (gp.dpad.right.wasPressedThisFrame) dh = 1;
            Vector2 st = gp.leftStick.ReadValue();
            if (st.magnitude > 0.6f)
            {
                repetir -= Time.unscaledDeltaTime;
                if (repetir <= 0f)
                {
                    if (Mathf.Abs(st.y) > Mathf.Abs(st.x)) dv = st.y > 0f ? -1 : 1; else dh = st.x > 0f ? 1 : -1;
                    repetir = 0.22f;
                }
            }
            else repetir = 0f;
        }
        if (dv != 0) { sel = (sel + dv + n) % n; Sonido.Tocar(Sonido.Efecto.Clic, 0.6f); }
        if (dh != 0 && pagina == Pagina.Configuracion && MenuConfiguracion.Ajustable(sel)) MenuConfiguracion.Ajustar(sel, dh);

        bool confirm = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                    || (gp != null && gp.buttonSouth.wasPressedThisFrame);
        if (confirm) Elegir(sel);
    }

    static bool IntroPlaying()
    {
        var intro = FindAnyObjectByType<ExteriorIntro>();
        return intro != null && intro.enabled;
    }

    void Pause()
    {
        IsPaused = true;
        savedScale = Time.timeScale > 0.05f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
        orbit = FindObjectsByType<MouseOrbitCamera>();
        foreach (var o in orbit) o.enabled = false;
        cursorAntes = Cursor.lockState; cursorVisibleAntes = Cursor.visible;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        abierto = 0f;
        Ir(Pagina.Principal, 0);
        Sonido.Tocar(Sonido.Efecto.Clic, 0.8f, 0.8f);
    }

    void Resume()
    {
        IsPaused = false;
        Time.timeScale = savedScale;
        if (orbit != null) foreach (var o in orbit) if (o != null) o.enabled = true;
        Cursor.lockState = cursorAntes; Cursor.visible = cursorVisibleAntes;
    }

    void Load(string scene)
    {
        IsPaused = false; Blocked = false;
        Time.timeScale = 1f;
        Cargador.Ir(scene);
    }

    void Ir(Pagina p, int seleccion) { pagina = p; sel = seleccion; entrada = 0f; }

    bool HayMenu => Application.CanStreamedLevelBeLoaded(FloorDirector.MenuScene);
    bool Fuera => SceneManager.GetActiveScene().name == exteriorScene;

    string[] Opciones()
    {
        switch (pagina)
        {
            case Pagina.Configuracion: return MenuConfiguracion.Opciones();
            case Pagina.Salir: return new[] { "No, seguir jugando", "Sí, salir al menú" };
            default:
                return HayMenu
                    ? new[] { "Continuar", "Configuración", Fuera ? "Reiniciar la cala" : "Reiniciar piso", "Salir al menú" }
                    : new[] { "Continuar", "Configuración", Fuera ? "Reiniciar la cala" : "Reiniciar piso" };
        }
    }

    void Elegir(int i)
    {
        switch (pagina)
        {
            case Pagina.Principal:
                Sonido.Tocar(Sonido.Efecto.Clic);
                if (i == 0) Resume();
                else if (i == 1) Ir(Pagina.Configuracion, 0);
                else if (i == 2) Load(SceneManager.GetActiveScene().name);
                else Ir(Pagina.Salir, 0);
                break;
            case Pagina.Configuracion:
                if (MenuConfiguracion.Elegir(i)) Ir(Pagina.Principal, 1);
                break;
            case Pagina.Salir:
                Sonido.Tocar(Sonido.Efecto.Clic);
                if (i == 0) Ir(Pagina.Principal, 3);
                else Load(FloorDirector.MenuScene);
                break;
        }
    }

    // ───────────────────────────── Dibujo ─────────────────────────────

    void OnGUI()
    {
        if (!IsPaused) return;
        GUI.depth = -1000;
        float sw = Screen.width, sh = Screen.height, u = sh / 100f, t = Time.unscaledTime;
        float a = Mathf.Clamp01(abierto / 0.2f), ap = Mathf.Clamp01(entrada / 0.2f);

        // el juego queda detrás, oscurecido (más abajo, donde va el panel)
        GUI.color = new Color(0.02f, 0.01f, 0.05f, 0.62f * a);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);
        GUI.color = new Color(0.01f, 0.005f, 0.03f, 0.6f * a);
        GUI.DrawTexture(new Rect(0f, sh * 0.35f, sw, sh * 0.65f), Degradado.Vertical);

        EmblemaTorre.Dibujar(u * 3f, u * 3f, u * 17f, a, t);

        // título
        var titulo = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        string encabezado = pagina == Pagina.Configuracion ? "CONFIGURACIÓN" : "PAUSA";
        Sombra(new Rect(0, sh * 0.08f, sw, u * 13f), encabezado, FuentePixel.Logo(titulo, pagina == Pagina.Configuracion ? u * 7.5f : u * 11f), new Color(1f, 0.92f, 0.75f, a));
        var sub = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic, fontSize = FuentePixel.Tam(u * 3f) };
        float ySub = sh * 0.08f + u * 13f;
        Sombra(new Rect(0, ySub, sw, u * 5f), Lugar(), sub, new Color(0.88f, 0.82f, 1f, a));
        float tiraW = u * 22f, tiraH = Mathf.Max(1f, u * 0.2f);
        GUI.color = new Color(0.92f, 0.78f, 0.45f, 0.8f * a);
        GUI.DrawTexture(new Rect(sw / 2f - u * 18f - tiraW, ySub + u * 2.4f, tiraW, tiraH), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(sw / 2f + u * 18f, ySub + u * 2.4f, tiraW, tiraH), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // el panel de opciones, igual que el del menú
        var ops = Opciones();
        float w = Mathf.Min(sw * 0.8f, u * 92f), h = ops.Length > 5 ? u * 6f : u * 7f;
        var caja = new Rect((sw - w) / 2f, (ops.Length > 5 ? sh * 0.33f : sh * 0.4f) + (1f - ap) * u * 1.5f, w, h * ops.Length + u * 4f);
        if (pagina == Pagina.Salir)
        {
            var q = new GUIStyle(sub) { fontStyle = FontStyle.Bold, fontSize = FuentePixel.Tam(u * 2.8f) };
            Sombra(new Rect(caja.x, caja.y - u * 6f, caja.width, u * 5f), "¿Salir al menú? Volverás a empezar este piso.", q, new Color(1f, 1f, 1f, a));
        }
        CuadroNarracion.Marco(caja, 0.92f * a * ap);
        var st = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 3.6f), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        var ev = Event.current;
        for (int i = 0; i < ops.Length; i++)
        {
            var r = new Rect(caja.x + u * 2f, caja.y + u * 2f + i * h, caja.width - u * 4f, h);
            if (ev.type == EventType.MouseMove && r.Contains(ev.mousePosition) && sel != i) { sel = i; Sonido.Tocar(Sonido.Efecto.Clic, 0.4f); }
            if (ev.type == EventType.MouseDown && ev.button == 0 && r.Contains(ev.mousePosition))
            {
                sel = i;
                if (pagina == Pagina.Configuracion && MenuConfiguracion.Ajustable(i)) MenuConfiguracion.Ajustar(i, ev.mousePosition.x > r.center.x ? 1 : -1);
                else Elegir(i);
                ev.Use();
                return;
            }
            bool on = i == sel;
            if (on)
            {
                float fy = r.y + u * 0.7f, fh = r.height - u * 1.4f, filete = Mathf.Max(1f, u * 0.14f);
                GUI.color = new Color(0.62f, 0.42f, 0.95f, 0.5f * a * ap);
                GUI.DrawTexture(new Rect(r.x, fy, r.width, fh), Degradado.Centro);
                GUI.color = new Color(0.95f, 0.78f, 0.45f, 0.9f * a * ap);
                GUI.DrawTexture(new Rect(r.x, fy, r.width, filete), Degradado.Centro);
                GUI.DrawTexture(new Rect(r.x, fy + fh, r.width, filete), Degradado.Centro);
            }
            Color c = on ? new Color(1f, 0.87f, 0.55f) : new Color(1f, 1f, 1f, 0.82f);
            if (pagina == Pagina.Configuracion && i == MenuConfiguracion.Dificultad && !on) c = Color.Lerp(c, MenuConfiguracion.ColorDificultad, 0.5f);
            c.a *= a * ap;
            Sombra(r, (on ? "◆   " : "") + ops[i] + (on ? "   ◆" : ""), st, c);
        }

        // abajo: ayuda de la opción y lo encontrado
        var chica = new GUIStyle(sub) { fontStyle = FontStyle.Normal, fontSize = FuentePixel.Tam(u * 2.2f) };
        string nota = pagina == Pagina.Configuracion ? MenuConfiguracion.Ayuda(sel)
                    : pagina == Pagina.Principal ? (SecretTracker.Notes == 1 ? "1 nota encontrada" : $"{SecretTracker.Notes} notas encontradas") : null;
        if (nota != null)
        {
            Color cn = pagina == Pagina.Configuracion && sel == MenuConfiguracion.Dificultad ? MenuConfiguracion.ColorDificultad : new Color(1f, 0.85f, 0.55f, 0.85f);
            cn.a *= a;
            Sombra(new Rect(caja.x, caja.yMax + u * 1.2f, caja.width, u * 4f), nota, chica, cn);
        }
        var ayuda = new GUIStyle(chica) { fontSize = FuentePixel.Tam(u * 1.8f) };
        string teclas = pagina == Pagina.Configuracion
            ? "↑ ↓  elegir     ← →  ajustar     Enter / A  aceptar     Esc / B  volver"
            : "↑ ↓  elegir     Enter / A  aceptar     Esc / B  " + (pagina == Pagina.Principal ? "continuar" : "volver");
        Sombra(new Rect(0, sh - u * 6f, sw, u * 4f), teclas, ayuda, new Color(1f, 1f, 1f, 0.7f * a));
        GUI.color = Color.white;
    }

    string Lugar()
    {
        string n = SceneManager.GetActiveScene().name;
        if (n == exteriorScene) return "Cala Sombría";
        string nombre = Progreso.NombreDe(n);
        return string.IsNullOrEmpty(nombre) ? "La torre del abismo" : nombre;
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
