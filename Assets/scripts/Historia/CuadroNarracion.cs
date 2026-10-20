using UnityEngine;

/// <summary>
/// Cuadros de texto de la historia (al estilo de Octopath Traveler): un marco azul oscuro con doble filete dorado y
/// adornos en las esquinas. Narración (centrada abajo), diálogo (con placa del nombre) y tarjetas de fecha.
/// Se dibujan con OnGUI desde quien los use (cinemática, tutorial).
/// </summary>
public static class CuadroNarracion
{
    static Texture2D fondo, blanco;
    static readonly Color Oro = new Color(0.92f, 0.78f, 0.45f);
    static readonly Color OroOscuro = new Color(0.55f, 0.42f, 0.2f);

    static void Preparar()
    {
        if (fondo != null) return;
        // degradado vertical: azul noche arriba, casi negro abajo
        fondo = new Texture2D(1, 32, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, hideFlags = HideFlags.HideAndDontSave };
        for (int y = 0; y < 32; y++)
        {
            float k = y / 31f;
            fondo.SetPixel(0, y, Color.Lerp(new Color(0.02f, 0.03f, 0.07f, 0.92f), new Color(0.08f, 0.1f, 0.2f, 0.88f), k));
        }
        fondo.Apply();
        blanco = Texture2D.whiteTexture;
    }

    /// <summary>Cuántas letras se ven después de t segundos (efecto máquina de escribir).</summary>
    public static int Letras(string texto, float t, float porSegundo = 45f) => Mathf.Clamp(Mathf.FloorToInt(t * porSegundo), 0, texto.Length);

    static void Rect(Rect r, Color c) { var prev = GUI.color; GUI.color = c; GUI.DrawTexture(r, blanco); GUI.color = prev; }

    /// <summary>El marco: fondo degradado, doble filete dorado y rombos en las esquinas.</summary>
    public static void Marco(Rect r, float alpha)
    {
        Preparar();
        float u = Screen.height / 100f;
        var prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.DrawTexture(r, fondo);
        GUI.color = prev;
        float t = Mathf.Max(1f, u * 0.22f);
        Color oro = new Color(Oro.r, Oro.g, Oro.b, alpha), oscuro = new Color(OroOscuro.r, OroOscuro.g, OroOscuro.b, alpha);
        // filete exterior
        Rect(new Rect(r.x, r.y, r.width, t), oro); Rect(new Rect(r.x, r.yMax - t, r.width, t), oro);
        Rect(new Rect(r.x, r.y, t, r.height), oro); Rect(new Rect(r.xMax - t, r.y, t, r.height), oro);
        // filete interior
        float g = u * 0.7f;
        var ri = new Rect(r.x + g, r.y + g, r.width - 2f * g, r.height - 2f * g);
        Rect(new Rect(ri.x, ri.y, ri.width, t * 0.6f), oscuro); Rect(new Rect(ri.x, ri.yMax - t * 0.6f, ri.width, t * 0.6f), oscuro);
        Rect(new Rect(ri.x, ri.y, t * 0.6f, ri.height), oscuro); Rect(new Rect(ri.xMax - t * 0.6f, ri.y, t * 0.6f, ri.height), oscuro);
        // rombos en las esquinas
        foreach (var c in new[] { new Vector2(r.x, r.y), new Vector2(r.xMax, r.y), new Vector2(r.x, r.yMax), new Vector2(r.xMax, r.yMax) })
        {
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, c);
            float s = u * 1.3f;
            Rect(new Rect(c.x - s / 2f, c.y - s / 2f, s, s), oro);
            Rect(new Rect(c.x - s / 4f, c.y - s / 4f, s / 2f, s / 2f), new Color(0.1f, 0.12f, 0.22f, alpha));
            GUI.matrix = m;
        }
    }

    static GUIStyle Estilo(float tam, FontStyle fs = FontStyle.Normal, TextAnchor a = TextAnchor.UpperLeft)
        => new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height / 100f * tam), fontStyle = fs, alignment = a, wordWrap = true, richText = false };

    static void Texto(Rect r, string s, GUIStyle st, Color c)
    {
        GUI.color = Color.white;
        st.normal.textColor = new Color(0f, 0f, 0f, c.a * 0.8f);
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), s, st);
        st.normal.textColor = c;
        GUI.Label(r, s, st);
    }

    /// <summary>Indicador ▼ que parpadea cuando el texto terminó (presiona para seguir).</summary>
    static void Siguiente(Rect caja, float alpha)
    {
        float u = Screen.height / 100f;
        float a = alpha * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f));
        Texto(new Rect(caja.xMax - u * 5f, caja.yMax - u * 4.6f, u * 3f, u * 3f), "▼", Estilo(2.2f, FontStyle.Bold, TextAnchor.MiddleCenter), new Color(Oro.r, Oro.g, Oro.b, a));
    }

    /// <summary>Narración: caja centrada abajo con el texto (se escribe solo).</summary>
    public static void Narracion(string texto, int letras, float alpha)
    {
        float sw = Screen.width, sh = Screen.height, u = sh / 100f;
        float w = Mathf.Min(sw * 0.72f, u * 150f);
        var st = Estilo(2.6f, FontStyle.Normal, TextAnchor.MiddleCenter);
        float h = st.CalcHeight(new GUIContent(texto), w - u * 10f) + u * 7f;
        var caja = new Rect((sw - w) / 2f, sh - h - u * 8f, w, h);
        Marco(caja, alpha);
        string vis = texto.Substring(0, Mathf.Clamp(letras, 0, texto.Length));
        // se reserva el alto final para que el texto no "salte" al escribirse
        Texto(new Rect(caja.x + u * 5f, caja.y + u * 3.5f, caja.width - u * 10f, caja.height - u * 7f), vis, st, new Color(0.97f, 0.95f, 0.9f, alpha));
        if (letras >= texto.Length) Siguiente(caja, alpha);
    }

    /// <summary>Diálogo: caja abajo con placa dorada del que habla.</summary>
    public static void Dialogo(string quien, string texto, int letras, float alpha)
    {
        float sw = Screen.width, sh = Screen.height, u = sh / 100f;
        float w = Mathf.Min(sw * 0.8f, u * 160f);
        var st = Estilo(2.5f);
        float h = Mathf.Max(u * 14f, st.CalcHeight(new GUIContent(texto), w - u * 10f) + u * 8f);
        var caja = new Rect((sw - w) / 2f, sh - h - u * 6f, w, h);
        Marco(caja, alpha);
        var placaSt = Estilo(2.3f, FontStyle.Bold, TextAnchor.MiddleCenter);
        float pw = placaSt.CalcSize(new GUIContent(quien)).x + u * 6f;
        var placa = new Rect(caja.x + u * 4f, caja.y - u * 3.2f, pw, u * 5f);
        Marco(placa, alpha);
        Texto(placa, quien, placaSt, new Color(Oro.r, Oro.g, Oro.b, alpha));
        string vis = texto.Substring(0, Mathf.Clamp(letras, 0, texto.Length));
        Texto(new Rect(caja.x + u * 5f, caja.y + u * 4f, caja.width - u * 10f, caja.height - u * 6f), vis, st, new Color(0.97f, 0.95f, 0.9f, alpha));
        if (letras >= texto.Length) Siguiente(caja, alpha);
    }

    /// <summary>Tarjeta grande al centro (fecha, título de capítulo).</summary>
    public static void Tarjeta(string grande, string chico, float alpha)
    {
        float sw = Screen.width, sh = Screen.height, u = sh / 100f;
        var big = Estilo(6.5f, FontStyle.Bold, TextAnchor.MiddleCenter);
        var small = Estilo(2.6f, FontStyle.Italic, TextAnchor.MiddleCenter);
        Rect(new Rect(sw * 0.3f, sh * 0.42f - u * 1f, sw * 0.4f, u * 0.25f), new Color(Oro.r, Oro.g, Oro.b, alpha * 0.8f));
        Texto(new Rect(0, sh * 0.42f, sw, u * 9f), grande, big, new Color(0.98f, 0.95f, 0.88f, alpha));
        if (!string.IsNullOrEmpty(chico)) Texto(new Rect(0, sh * 0.42f + u * 8.5f, sw, u * 4f), chico, small, new Color(Oro.r, Oro.g, Oro.b, alpha));
        Rect(new Rect(sw * 0.3f, sh * 0.42f + u * 13f, sw * 0.4f, u * 0.25f), new Color(Oro.r, Oro.g, Oro.b, alpha * 0.8f));
    }

    /// <summary>Franjas de cine arriba y abajo (0 = sin franjas, 1 = completas).</summary>
    public static void Franjas(float k)
    {
        if (k <= 0f) return;
        float sh = Screen.height, h = sh * 0.1f * k;
        Rect(new Rect(0, 0, Screen.width, h), Color.black);
        Rect(new Rect(0, sh - h, Screen.width, h), Color.black);
    }

    public static void Negro(float a) { if (a > 0f) Rect(new Rect(0, 0, Screen.width, Screen.height), new Color(0f, 0f, 0f, a)); }

    /// <summary>Ayuda chiquita abajo a la derecha (cómo avanzar / saltar).</summary>
    public static void Ayuda(string s, float alpha)
    {
        float u = Screen.height / 100f;
        Texto(new Rect(Screen.width - u * 60f, Screen.height - u * 4f, u * 58f, u * 3f), s, Estilo(1.6f, FontStyle.Normal, TextAnchor.MiddleRight), new Color(1f, 1f, 1f, alpha * 0.6f));
    }
}
