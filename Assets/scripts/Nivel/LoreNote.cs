using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Libro, nota o inscripción que se puede leer (E / Y del control). Al acercarse aparece "E · Leer"; al leer se pausa
/// el juego y se muestra el texto en una hoja de pergamino. Brilla un poco (polvo dorado) hasta que se lee.
/// FloorLayout los coloca con la historia de cada piso.
/// </summary>
public class LoreNote : MonoBehaviour
{
    public enum Kind { Libro, Nota, Inscripcion, Diario }

    public string id = "";
    public Kind kind = Kind.Nota;
    public string title = "";
    [TextArea(4, 12)] public string text = "";
    public string signature = "";
    public float radius = 1.6f;
    public GameObject sparkle;   // se apaga después de leer

    public static LoreNote Open { get; private set; }

    Transform player;
    bool near;
    float openTime;
    Vector2 scroll;

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
        if (sparkle != null && SecretTracker.Has("nota:" + id)) sparkle.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;
        var kb = Keyboard.current; var gp = Gamepad.current;
        bool press = (kb != null && kb.eKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame);

        if (Open == this)
        {
            bool close = press || (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
                         || (gp != null && (gp.buttonEast.wasPressedThisFrame || gp.buttonSouth.wasPressedThisFrame));
            if (close && Time.unscaledTime - openTime > 0.15f) Close();
            return;
        }

        Vector3 d = player.position - transform.position; d.y = 0f;
        near = d.magnitude < radius && Mathf.Abs(player.position.y - transform.position.y) < 2f;
        if (near && press && Open == null && !PauseMenu.IsPaused) Show();
    }

    void Show()
    {
        Open = this;
        openTime = Time.unscaledTime;
        PauseMenu.Blocked = true;
        Time.timeScale = 0f;
        scroll = Vector2.zero;
        if (SecretTracker.Mark("nota:" + id) && sparkle != null) sparkle.SetActive(false);
    }

    void Close()
    {
        Open = null;
        PauseMenu.Blocked = false;
        Time.timeScale = 1f;
    }

    void OnDisable() { if (Open == this) Close(); }

    void OnGUI()
    {
        float sw = Screen.width, sh = Screen.height, u = sh / 100f;
        if (Open == null && near && player != null && !PauseMenu.IsPaused)
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.2f);
            if (sp.z < 0f) return;
            var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 2.2f), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            string verb = kind == Kind.Inscripcion ? "Examinar" : "Leer";
            var r = new Rect(sp.x - u * 12f, sh - sp.y - u * 2.5f, u * 24f, u * 4f);
            st.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), "E · " + verb, st);
            st.normal.textColor = new Color(1f, 0.92f, 0.7f);
            GUI.Label(r, "E · " + verb, st);
            return;
        }
        if (Open != this) return;

        GUI.depth = -900;
        var prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture);

        // hoja: pergamino (o piedra si es inscripción)
        float pw = Mathf.Min(sw - 6f * u, 80f * u);
        var measure = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 2.25f), wordWrap = true, richText = true };
        float needed = measure.CalcHeight(new GUIContent(text), pw - 14f * u) + 26f * u;   // la hoja crece con el texto
        float ph = Mathf.Clamp(needed, 40f * u, sh - 10f * u);
        var page = new Rect((sw - pw) / 2f, (sh - ph) / 2f, pw, ph);
        bool stone = kind == Kind.Inscripcion;
        Color paper = stone ? new Color(0.32f, 0.31f, 0.34f) : new Color(0.86f, 0.78f, 0.6f);
        Color ink = stone ? new Color(0.92f, 0.9f, 0.84f) : new Color(0.22f, 0.14f, 0.08f);
        GUI.color = new Color(paper.r * 0.6f, paper.g * 0.6f, paper.b * 0.6f, 1f);
        GUI.DrawTexture(new Rect(page.x - 0.6f * u, page.y - 0.6f * u, page.width + 1.2f * u, page.height + 1.2f * u), Texture2D.whiteTexture);
        GUI.color = paper;
        GUI.DrawTexture(page, Texture2D.whiteTexture);
        // manchas en las orillas
        GUI.color = new Color(0f, 0f, 0f, 0.08f);
        GUI.DrawTexture(new Rect(page.x, page.y, page.width, 2f * u), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(page.x, page.yMax - 2f * u, page.width, 2f * u), Texture2D.whiteTexture);
        GUI.color = Color.white;

        var titleSt = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 3.4f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        titleSt.normal.textColor = ink;
        GUI.Label(new Rect(page.x + 4f * u, page.y + 3f * u, page.width - 8f * u, 6f * u), title, titleSt);
        var kindSt = new GUIStyle(titleSt) { fontSize = Mathf.RoundToInt(u * 1.7f), fontStyle = FontStyle.Italic };
        kindSt.normal.textColor = new Color(ink.r, ink.g, ink.b, 0.6f);
        GUI.Label(new Rect(page.x, page.y + 9f * u, page.width, 3f * u), kind == Kind.Libro ? "— libro —" : kind == Kind.Diario ? "— página de diario —" : kind == Kind.Inscripcion ? "— inscripción en la piedra —" : "— nota —", kindSt);

        var body = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 2.25f), wordWrap = true, alignment = TextAnchor.UpperLeft, richText = true };
        body.normal.textColor = ink;
        var area = new Rect(page.x + 6f * u, page.y + 13.5f * u, page.width - 12f * u, page.height - 23f * u);
        float textH = body.CalcHeight(new GUIContent(text), area.width - 2f * u);
        scroll = GUI.BeginScrollView(area, scroll, new Rect(0, 0, area.width - 2f * u, textH), false, false, GUIStyle.none, GUI.skin.verticalScrollbar);
        GUI.Label(new Rect(0, 0, area.width - 2f * u, textH), text, body);
        GUI.EndScrollView();

        if (!string.IsNullOrEmpty(signature))
        {
            var sig = new GUIStyle(body) { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Italic };
            GUI.Label(new Rect(page.x + 6f * u, page.yMax - 9f * u, page.width - 12f * u, 4f * u), signature, sig);
        }
        var hint = new GUIStyle(kindSt) { fontStyle = FontStyle.Normal };
        GUI.Label(new Rect(page.x, page.yMax - 4.5f * u, page.width, 3f * u), "Espacio / Enter / E / Esc  cerrar", hint);
        GUI.color = prev;
    }
}
