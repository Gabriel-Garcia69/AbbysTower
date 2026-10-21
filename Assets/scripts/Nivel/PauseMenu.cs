using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú de pausa (Esc / P / Start): congela el juego y ofrece continuar, opciones (modo de juego), reiniciar la escena,
/// volver al exterior o salir. En Opciones también está el acceso al mapa de prueba "Diorama HD-2D". Se maneja con mouse, teclado (↑↓ / WS + Enter) o mando (cruceta/stick + A, B regresa).
/// Mientras está abierto apaga la cámara de mouse y libera el cursor. No se abre con la tienda o la cinemática activas.
/// FloorDirector lo agrega solo en los pisos; en el exterior va en cualquier objeto de la escena.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    public string exteriorScene = "Exterior";
    [Tooltip("Mapa de prueba con estilo maqueta (Octopath) que se abre desde Opciones.")]
    public string dioramaScene = "Diorama";
    [Tooltip("Mapa de prueba del muelle de puerto (mismo estilo).")]
    public string portScene = "Puerto";

    public static bool IsPaused { get; private set; }
    /// <summary>Otros menús (tienda) lo ponen en true para que Esc no abra la pausa.</summary>
    public static bool Blocked;

    enum Page { Main, Options }
    struct Item { public string label; public Action action; }

    static readonly Color Gold = new Color(1f, 0.82f, 0.45f);
    static readonly Color PanelFill = new Color(0.07f, 0.05f, 0.12f, 0.95f);
    const int OptionsDiorama = 4;   // botón "Diorama: pueblo" en la página de opciones
    const int OptionsPort = 5;      // botón "Diorama: puerto"
    const int OptionsBack = 6;      // botón "Volver" en la página de opciones

    MouseOrbitCamera[] orbit;
    float savedScale = 1f;
    Page page;
    int sel;
    float selAnim, openedAt, pageAt, stickRepeat, alpha;
    Vector2 lastMouse;
    readonly List<Item> items = new List<Item>();

    static Texture2D fillTex, lineTex, glowTex, vignetteTex;
    static GUIStyle fillStyle, lineStyle, text;

    void OnDisable() { if (IsPaused) Resume(); }

    void Update()
    {
        var kb = Keyboard.current; var gp = Gamepad.current;
        bool toggle = (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)) || (gp != null && gp.startButton.wasPressedThisFrame);
        if (!IsPaused)
        {
            if (toggle && !Blocked && !IntroPlaying()) Pause();
            return;
        }

        if (toggle || (gp != null && gp.buttonEast.wasPressedThisFrame))
        {
            if (page == Page.Options) OpenPage(Page.Main, 1);
            else Resume();
            return;
        }

        int count = ItemCount();
        int dir = 0;
        if (kb != null)
        {
            if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame) dir = -1;
            if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) dir = 1;
        }
        if (gp != null)
        {
            if (gp.dpad.up.wasPressedThisFrame) dir = -1;
            if (gp.dpad.down.wasPressedThisFrame) dir = 1;
            float y = gp.leftStick.ReadValue().y;
            if (Mathf.Abs(y) > 0.6f)
            {
                stickRepeat -= Time.unscaledDeltaTime;
                if (stickRepeat <= 0f) { dir = y > 0f ? -1 : 1; stickRepeat = 0.22f; }
            }
            else stickRepeat = 0f;
        }
        if (dir != 0) sel = (sel + dir + count) % count;
        selAnim = Mathf.Lerp(selAnim, sel, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));

        bool confirm = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)) || (gp != null && gp.buttonSouth.wasPressedThisFrame);
        if (confirm) Activate(sel);
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
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        openedAt = Time.unscaledTime;
        OpenPage(Page.Main, 0);
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

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OpenPage(Page p, int selection)
    {
        page = p; sel = selection; selAnim = selection;
        pageAt = Time.unscaledTime;
    }

    void BuildItems()
    {
        items.Clear();
        string current = SceneManager.GetActiveScene().name;
        bool outside = current == exteriorScene;
        // GDD: pausa con Esc; continuar, reiniciar el piso, volver al menú principal o salir (sin dificultad ni mapas de prueba)
        items.Add(new Item { label = "Continuar", action = Resume });
        items.Add(new Item { label = outside ? "Reiniciar exterior" : "Reiniciar piso", action = () => Load(current) });
        if (Application.CanStreamedLevelBeLoaded(FloorDirector.MenuScene))
            items.Add(new Item { label = "Menú principal", action = () => Load(FloorDirector.MenuScene) });
        items.Add(new Item { label = "Salir del juego", action = Quit });
    }

    int ItemCount()
    {
        if (page == Page.Options) return OptionsBack + 1;
        BuildItems();
        return items.Count;
    }

    void Activate(int i)
    {
        if (page == Page.Options)
        {
            if (i == OptionsBack) OpenPage(Page.Main, 1);
            else if (i == OptionsDiorama) { if (Application.CanStreamedLevelBeLoaded(DioramaBuilder.Route[0])) Load(DioramaBuilder.Route[0]); }
            else if (i == OptionsPort)
            {
                // siguiente piso de la ruta (fuera de la ruta: el primero)
                int k = System.Array.IndexOf(DioramaBuilder.Route, SceneManager.GetActiveScene().name);
                string next = DioramaBuilder.Route[(k + 1) % DioramaBuilder.Route.Length];
                if (Application.CanStreamedLevelBeLoaded(next)) Load(next);
            }
            else Difficulty.Current = (Difficulty.Level)i;
            return;
        }
        BuildItems();
        if (i >= 0 && i < items.Count) items[i].action();
    }

    // ───────────────────────────── Dibujo ─────────────────────────────

    void OnGUI()
    {
        if (!IsPaused) return;
        EnsureAssets();
        GUI.depth = -1000;
        float sw = Screen.width, sh = Screen.height, u = sh / 100f, t = Time.unscaledTime;
        Color prev = GUI.color;
        alpha = Ease(Mathf.Clamp01((t - openedAt) / 0.25f));

        // Fondo: oscurece, viñeta (roja y latiendo si el modo elegido es Hardcore)
        var full = new Rect(0, 0, sw, sh);
        bool hardcore = page == Page.Options && Difficulty.Current == Difficulty.Level.Hardcore;
        Tint(new Color(0.03f, 0.02f, 0.07f, 0.7f));
        GUI.DrawTexture(full, Texture2D.whiteTexture);
        Tint(hardcore ? new Color(0.65f, 0f, 0.05f, 0.7f + 0.25f * Mathf.Sin(t * 6f)) : new Color(0f, 0f, 0f, 0.9f));
        GUI.DrawTexture(full, vignetteTex);

        if (page == Page.Main) DrawMain(sw, sh, u, t);
        else DrawOptions(sw, sh, u, t);

        if (Event.current.type == EventType.Repaint) lastMouse = Event.current.mousePosition;
        GUI.color = prev;
    }

    void DrawMain(float sw, float sh, float u, float t)
    {
        BuildItems();
        float pw = Mathf.Min(sw - 4f * u, 56f * u), bh = 6.6f * u, gap = 1.3f * u;
        float ph = 25f * u + items.Count * (bh + gap) + 11f * u;
        Rect panel = PanelRect(sw, sh, u, pw, ph);
        DrawPanel(panel, u);

        Text(new Rect(panel.x, panel.y + 3f * u, pw, 9f * u), "P A U S A", 7f * u, true, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.65f));
        Divider(panel.center.x, panel.y + 13f * u, pw * 0.6f, u);
        Text(new Rect(panel.x, panel.y + 14.6f * u, pw, 3f * u), "ABBY'S TOWER   ·   " + SceneLabel(), 1.9f * u, false, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.55f));
        var diff = Difficulty.Active;
        Text(new Rect(panel.x + 2f * u, panel.y + 17.6f * u, pw - 4f * u, 3f * u), "Modo: " + diff.name, FitSize("Modo: " + diff.name, 1.9f * u, pw - 4f * u, true), true, TextAnchor.MiddleCenter, diff.color);
        string found = $"◆ Reliquias {SecretTracker.Relics}/{SecretTracker.TotalRelics}   ·   Escondites {SecretTracker.Hideouts}   ·   Notas {SecretTracker.Notes}";
        Text(new Rect(panel.x, panel.yMax - 9.5f * u, pw, 3f * u), found, FitSize(found, 1.7f * u, pw - 4f * u, false), false, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.55f, 0.75f));

        float y0 = panel.y + 22f * u, x = panel.x + 3f * u, w = pw - 6f * u;
        DrawHighlight(new Rect(x, y0 + selAnim * (bh + gap), w, bh), Gold, u, t);
        for (int i = 0; i < items.Count; i++)
        {
            var r = new Rect(x, y0 + i * (bh + gap), w, bh);
            if (Interact(r, i)) return;
            DrawButton(r, items[i].label, i == sel, u, t);
        }
        Footer(panel, u);
    }

    void DrawOptions(float sw, float sh, float u, float t)
    {
        float pw = Mathf.Min(sw - 4f * u, 88f * u), ch = 10.5f * u, gap = 1.2f * u, bh = 6.4f * u;
        float ph = 20f * u + 4f * (ch + gap) + 5f * u + bh + 8f * u;
        Rect panel = PanelRect(sw, sh, u, pw, ph);
        DrawPanel(panel, u);

        Text(new Rect(panel.x, panel.y + 2.5f * u, pw, 8f * u), "O P C I O N E S", 6f * u, true, TextAnchor.MiddleCenter, new Color(1f, 0.9f, 0.65f));
        Divider(panel.center.x, panel.y + 11.5f * u, pw * 0.5f, u);
        Text(new Rect(panel.x + 3f * u, panel.y + 13.5f * u, pw, 3f * u), "M O D O   D E   J U E G O", 1.8f * u, true, TextAnchor.MiddleLeft, Gold * new Color(1f, 1f, 1f, 0.85f));

        float x = panel.x + 3f * u, w = pw - 6f * u, y = panel.y + 18f * u;
        int current = (int)Difficulty.Current;
        for (int i = 0; i < Difficulty.Presets.Length; i++)
        {
            var r = new Rect(x, y + i * (ch + gap), w, ch);
            if (Interact(r, i)) return;
            DrawCard(r, Difficulty.Presets[i], i == current, i == sel, i == (int)Difficulty.Level.Hardcore, u, t);
        }

        // Resumen del modo bajo el cursor
        float sy = y + 4f * (ch + gap);
        var p = Difficulty.Presets[Mathf.Clamp(sel, 0, Difficulty.Presets.Length - 1)];
        string lost = p.fragmentsLost >= 1f ? "todos tus ◆" : Mathf.RoundToInt(p.fragmentsLost * 100f) + "% de tus ◆";
        string stats = $"Daño recibido x{p.damageTaken:0.##}   ·   Vida enemigos x{p.enemyHealth:0.##}   ·   Curación x{p.healing:0.##}   ·   Al morir pierdes {lost}{(p.loseUpgrades ? " y mejoras" : "")}";
        Text(new Rect(x, sy, w, 4f * u), stats, FitSize(stats, 1.7f * u, w, false), false, TextAnchor.MiddleCenter, sel < OptionsDiorama ? new Color(1f, 1f, 1f, 0.7f) : new Color(1f, 1f, 1f, 0f));

        // pruebas: mapa diorama (estilo Octopath) y volver
        float bw = (w - 4f * u) / 3f;
        var diorama = new Rect(x, sy + 5f * u, bw, bh);
        var port = new Rect(x + bw + 2f * u, sy + 5f * u, bw, bh);
        var back = new Rect(x + 2f * (bw + 2f * u), sy + 5f * u, bw, bh);
        var testColor = new Color(0.55f, 0.85f, 1f);
        if (sel == OptionsDiorama) DrawHighlight(diorama, testColor, u, t);
        if (sel == OptionsPort) DrawHighlight(port, testColor, u, t);
        if (sel == OptionsBack) DrawHighlight(back, Gold, u, t);
        if (Interact(diorama, OptionsDiorama)) return;
        if (Interact(port, OptionsPort)) return;
        if (Interact(back, OptionsBack)) return;
        DrawButton(diorama, "Ruta HD-2D", sel == OptionsDiorama, u, t);
        DrawButton(port, "Piso siguiente", sel == OptionsPort, u, t);
        DrawButton(back, "Volver", sel == OptionsBack, u, t);
        if (sel == OptionsDiorama || sel == OptionsPort)
        {
            string tip = sel == OptionsDiorama ? "RUTA HD-2D · 10 pisos del abismo con jefe, mercader y salida"
                                               : "Salta al siguiente piso de la ruta HD-2D (para probar) · Q/E giran la cámara, clic derecho para asomarte";
            Text(new Rect(x, sy, w, 4f * u), tip, FitSize(tip, 1.7f * u, w, false), false, TextAnchor.MiddleCenter, new Color(testColor.r, testColor.g, testColor.b, 0.85f));
        }
        Footer(panel, u);
    }

    Rect PanelRect(float sw, float sh, float u, float pw, float ph)
    {
        float pageIn = Ease(Mathf.Clamp01((Time.unscaledTime - pageAt) / 0.2f));
        return new Rect((sw - pw) / 2f, (sh - ph) / 2f + (1f - alpha) * 4f * u + (1f - pageIn) * 1.5f * u, pw, ph);
    }

    void DrawPanel(Rect panel, float u)
    {
        Tint(new Color(Gold.r, Gold.g, Gold.b, 0.1f));
        Draw(Expand(panel, 12f * u), glowTex);
        Box(panel, PanelFill, new Color(Gold.r, Gold.g, Gold.b, 0.6f));
        Box(Expand(panel, -0.8f * u), new Color(0f, 0f, 0f, 0f), new Color(Gold.r, Gold.g, Gold.b, 0.15f));
    }

    void DrawHighlight(Rect r, Color c, float u, float t)
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(t * 4f);
        Tint(new Color(c.r, c.g, c.b, 0.12f + 0.06f * pulse));
        Draw(Expand(r, 2.5f * u), glowTex);
        Box(r, new Color(c.r, c.g, c.b, 0.16f), new Color(c.r, c.g, c.b, 0.85f));
    }

    void DrawButton(Rect r, string label, bool selected, float u, float t)
    {
        if (!selected) Box(r, new Color(1f, 1f, 1f, 0.035f), new Color(1f, 1f, 1f, 0.08f));
        Text(r, label, 2.8f * u, true, TextAnchor.MiddleCenter, selected ? new Color(1f, 0.95f, 0.8f) : new Color(0.75f, 0.72f, 0.82f));
        if (!selected) return;
        float a = 0.6f + 0.4f * Mathf.Sin(t * 5f), off = Mathf.Sin(t * 5f) * 0.3f * u;
        var dc = new Color(Gold.r, Gold.g, Gold.b, a);
        Text(new Rect(r.x + 1.5f * u + off, r.y, 4f * u, r.height), "◆", 1.8f * u, false, TextAnchor.MiddleCenter, dc);
        Text(new Rect(r.xMax - 5.5f * u - off, r.y, 4f * u, r.height), "◆", 1.8f * u, false, TextAnchor.MiddleCenter, dc);
    }

    void DrawCard(Rect r, Difficulty.Preset p, bool active, bool selected, bool crazy, float u, float t)
    {
        Color c = p.color;
        if (selected) { Tint(new Color(c.r, c.g, c.b, 0.12f)); Draw(Expand(r, 2.5f * u), glowTex); }
        Box(r, selected ? new Color(c.r, c.g, c.b, 0.14f) : new Color(1f, 1f, 1f, 0.035f),
               active ? new Color(c.r, c.g, c.b, 0.95f) : selected ? new Color(c.r, c.g, c.b, 0.5f) : new Color(1f, 1f, 1f, 0.08f));
        Tint(new Color(c.r, c.g, c.b, active || selected ? 1f : 0.5f));
        Draw(new Rect(r.x + 1.3f * u, r.y + 2f * u, 0.5f * u, r.height - 4f * u), Texture2D.whiteTexture);

        float tagW = active ? 11f * u : 0f, nameW = r.width - 5f * u - tagW;
        Color nameColor = c;
        Vector2 jitter = Vector2.zero;
        if (crazy)
        {
            nameColor = Color.Lerp(new Color(1f, 0.15f, 0.2f), new Color(1f, 0.6f, 0.1f), 0.5f + 0.5f * Mathf.Sin(t * 9f));
            float k = (selected || active ? 0.3f : 0.1f) * u;
            jitter = new Vector2(Mathf.Sin(t * 53f) * k, Mathf.Cos(t * 47f) * k);
        }
        if (!active && !selected) nameColor.a = 0.75f;
        var nameRect = new Rect(r.x + 3.5f * u + jitter.x, r.y + 1.2f * u + jitter.y, nameW, 4.5f * u);
        Text(nameRect, p.name, FitSize(p.name, 3.2f * u, nameW, true), true, TextAnchor.MiddleLeft, nameColor);
        Text(new Rect(r.x + 3.5f * u, r.y + 5.6f * u, r.width - 6f * u, 4.2f * u), p.description, 1.75f * u, false, TextAnchor.UpperLeft,
             new Color(1f, 1f, 1f, selected || active ? 0.75f : 0.45f), true);
        if (active)
            Text(new Rect(r.xMax - tagW - 1.5f * u, r.y + 1.2f * u, tagW, 4.5f * u), "●  ACTIVO", 1.6f * u, true, TextAnchor.MiddleRight, c);
    }

    void Footer(Rect panel, float u)
    {
        string hint = page == Page.Main ? "↑ ↓  elegir      Enter / A  aceptar      Esc / B  continuar"
                                        : "↑ ↓  elegir      Enter / A  seleccionar      Esc / B  volver";
        Text(new Rect(panel.x, panel.yMax - 5.5f * u, panel.width, 4f * u), hint, FitSize(hint, 1.7f * u, panel.width - 4f * u, false), false, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.4f));
    }

    void Divider(float cx, float y, float w, float u)
    {
        float th = Mathf.Max(1f, 0.15f * u), half = w / 2f, hole = 2.2f * u;
        Tint(new Color(Gold.r, Gold.g, Gold.b, 0.55f));
        Draw(new Rect(cx - half, y - th / 2f, half - hole, th), Texture2D.whiteTexture);
        Draw(new Rect(cx + hole, y - th / 2f, half - hole, th), Texture2D.whiteTexture);
        Text(new Rect(cx - hole, y - 2f * u, hole * 2f, 4f * u), "◆", 2f * u, false, TextAnchor.MiddleCenter, Gold);
    }

    /// <summary>Mouse: pasar encima selecciona, clic activa. Devuelve true si se activó (la página pudo cambiar).</summary>
    bool Interact(Rect r, int index)
    {
        var e = Event.current;
        if (!r.Contains(e.mousePosition)) return false;
        if (e.type == EventType.Repaint && (e.mousePosition - lastMouse).sqrMagnitude > 0.5f) sel = index;
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            sel = index;
            e.Use();
            Activate(index);
            return true;
        }
        return false;
    }

    string SceneLabel()
    {
        string n = SceneManager.GetActiveScene().name;
        if (n == exteriorScene) return "EL ABISMO";
        if (n.StartsWith("Nivel")) return "PISO " + n.Substring(5);
        if (n == dioramaScene) return "DIORAMA HD-2D (PRUEBA)";
        if (n == portScene) return "EL PUERTO (PRUEBA)";
        int k = System.Array.IndexOf(DioramaBuilder.Route, n);
        if (k >= 0) return "RUTA HD-2D · PISO " + (k + 1);
        return n.ToUpperInvariant();
    }

    // ───────────────────────────── Utilidades ─────────────────────────────

    void Tint(Color c) { c.a *= alpha; GUI.color = c; }

    static void Draw(Rect r, Texture tex)
    {
        if (Event.current.type == EventType.Repaint) GUI.DrawTexture(r, tex);
    }

    void Box(Rect r, Color fill, Color outline)
    {
        if (Event.current.type != EventType.Repaint) return;
        if (fill.a > 0f) { Tint(fill); fillStyle.Draw(r, false, false, false, false); }
        if (outline.a > 0f) { Tint(outline); lineStyle.Draw(r, false, false, false, false); }
    }

    void Text(Rect r, string s, float size, bool bold, TextAnchor anchor, Color c, bool wrap = false)
    {
        text.fontSize = Mathf.Max(8, Mathf.RoundToInt(size));
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        text.alignment = anchor;
        text.wordWrap = wrap;
        GUI.color = Color.white;
        float o = Mathf.Max(1f, size * 0.06f);
        text.normal.textColor = new Color(0f, 0f, 0f, 0.6f * c.a * alpha);
        GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), s, text);
        text.normal.textColor = new Color(c.r, c.g, c.b, c.a * alpha);
        GUI.Label(r, s, text);
    }

    /// <summary>Achica la letra hasta que el texto quepa en una línea del ancho dado.</summary>
    static float FitSize(string s, float size, float width, bool bold)
    {
        text.wordWrap = false;
        text.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        var content = new GUIContent(s);
        while (size > 8f)
        {
            text.fontSize = Mathf.RoundToInt(size);
            if (text.CalcSize(content).x <= width) break;
            size -= 1f;
        }
        return size;
    }

    static Rect Expand(Rect r, float d) => new Rect(r.x - d, r.y - d, r.width + 2f * d, r.height + 2f * d);
    static float Ease(float x) => 1f - (1f - x) * (1f - x) * (1f - x);

    static void EnsureAssets()
    {
        if (text == null) text = new GUIStyle(GUI.skin.label) { richText = false, clipping = TextClipping.Overflow };
        if (fillTex != null) return;
        const int size = 64, radius = 14;
        fillTex = MakeRounded(size, radius, 0f);
        lineTex = MakeRounded(size, radius, 2f);
        fillStyle = new GUIStyle { normal = { background = fillTex }, border = new RectOffset(radius + 2, radius + 2, radius + 2, radius + 2) };
        lineStyle = new GUIStyle { normal = { background = lineTex }, border = new RectOffset(radius + 2, radius + 2, radius + 2, radius + 2) };
        glowTex = MakeRadial(64, d => Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f));
        vignetteTex = MakeRadial(128, d => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.25f, d)));
    }

    /// <summary>Rectángulo redondeado blanco (relleno o solo borde de <paramref name="line"/> px), para teñir con GUI.color.</summary>
    static Texture2D MakeRounded(int size, float radius, float line)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        float half = size / 2f;
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x + 0.5f - half) - (half - radius), qy = Mathf.Abs(y + 0.5f - half) - (half - radius);
                float d = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
                float a = Mathf.Clamp01(0.5f - d);
                if (line > 0f) a *= Mathf.Clamp01(0.5f + d + line);
                px[y * size + x] = new Color(1f, 1f, 1f, a);
            }
        tex.SetPixels(px); tex.Apply();
        return tex;
    }

    static Texture2D MakeRadial(int size, Func<float, float> alphaByDistance)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
        var px = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                px[y * size + x] = new Color(1f, 1f, 1f, alphaByDistance(Mathf.Sqrt(dx * dx + dy * dy)));
            }
        tex.SetPixels(px); tex.Apply();
        return tex;
    }
}
