using System.Collections.Generic;
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

    static void Rect(Rect r, Color c) { Preparar(); var prev = GUI.color; GUI.color = c; GUI.DrawTexture(r, blanco); GUI.color = prev; }

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
        => new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(Screen.height / 100f * tam), fontStyle = fs, alignment = a, wordWrap = true, richText = false };

    /// <summary>Espacio de una línea a la siguiente, en veces el tamaño de la letra (OnGUI no deja cambiar el interlineado: las líneas se dibujan una por una).</summary>
    const float Interlineado = 1.5f;

    /// <summary>Parte el texto en líneas que caben en ancho.</summary>
    public static List<string> Partir(string texto, GUIStyle st, float ancho)
    {
        var lineas = new List<string>();
        foreach (var parrafo in texto.Split('\n'))
        {
            string linea = "";
            foreach (var palabra in parrafo.Split(' '))
            {
                string prueba = linea.Length == 0 ? palabra : linea + " " + palabra;
                if (linea.Length > 0 && st.CalcSize(new GUIContent(prueba)).x > ancho) { lineas.Add(linea); linea = palabra; }
                else linea = prueba;
            }
            lineas.Add(linea);
        }
        return lineas;
    }

    static string ultimoTexto; static float ultimoAncho, ultimoTam, ultimoFinal; static List<string> ultimasLineas;

    /// <summary>La letra más grande (hasta tam) con la que el texto completo no pasa de altoMax: los textos largos se achican en vez de salirse del cuadro. Se recuerda porque esto corre en cada cuadro.</summary>
    static GUIStyle Cabe(float tam, FontStyle fs, TextAnchor a, string texto, float ancho, float altoMax, out List<string> lineas)
    {
        if (texto != ultimoTexto || ancho != ultimoAncho || tam != ultimoTam)
        {
            ultimoTexto = texto; ultimoAncho = ancho; ultimoTam = tam;
            float t = tam;
            var prueba = Estilo(t, fs, a);
            ultimasLineas = Partir(texto, prueba, ancho);
            while (t > 1.7f && ultimasLineas.Count * prueba.fontSize * Interlineado > altoMax)
            {
                t -= 0.15f;
                prueba = Estilo(t, fs, a);
                ultimasLineas = Partir(texto, prueba, ancho);
            }
            ultimoFinal = t;
        }
        lineas = ultimasLineas;
        return Estilo(ultimoFinal, fs, a);
    }

    /// <summary>Dibuja las líneas con su interlineado, solo hasta la letra que va la máquina de escribir; cada línea ya está en su lugar final, así no brinca al escribirse.</summary>
    static void Lineas(List<string> lineas, int letras, Rect r, GUIStyle st, bool centrado, Color c)
    {
        var una = new GUIStyle(st) { wordWrap = false, alignment = TextAnchor.UpperLeft };
        float paso = st.fontSize * Interlineado;
        int quedan = letras;
        for (int i = 0; i < lineas.Count && quedan > 0; i++)
        {
            string l = lineas[i];
            float x = centrado ? r.x + (r.width - st.CalcSize(new GUIContent(l)).x) / 2f : r.x;
            Texto(new Rect(x, r.y + i * paso, r.width, paso), l.Substring(0, Mathf.Min(quedan, l.Length)), una, c);
            quedan -= l.Length + 1;
        }
    }

    static void Texto(Rect r, string s, GUIStyle st, Color c)
    {
        GUI.color = Color.white;
        st.normal.textColor = st.hover.textColor = new Color(0f, 0f, 0f, c.a * 0.8f);
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), s, st);
        st.normal.textColor = st.hover.textColor = c;
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
        var st = Cabe(3.6f, FontStyle.Normal, TextAnchor.UpperLeft, texto, w - u * 10f, sh * 0.32f, out var lineas);
        float alto = lineas.Count * st.fontSize * Interlineado;
        float h = alto + u * 5f;
        var caja = new Rect((sw - w) / 2f, sh - h - u * 8f, w, h);
        Marco(caja, alpha);
        Lineas(lineas, letras, new Rect(caja.x + u * 5f, caja.y + u * 2.5f, caja.width - u * 10f, alto), st, true, new Color(0.97f, 0.95f, 0.9f, alpha));
        if (letras >= texto.Length) Siguiente(caja, alpha);
    }

    /// <summary>Diálogo: caja abajo con placa dorada del que habla.</summary>
    static float ultimoDialogo; static int cuadroDialogo = -10;
    /// <summary>Qué tan visible está el cuadro de diálogo (0 = nada); el HUD atenúa su barra de controles con esto para no encimarse.</summary>
    public static float DialogoVisible => Time.frameCount - cuadroDialogo <= 2 ? ultimoDialogo : 0f;

    public static void Dialogo(string quien, string texto, int letras, float alpha)
    {
        ultimoDialogo = alpha; cuadroDialogo = Time.frameCount;
        float sw = Screen.width, sh = Screen.height, u = sh / 100f;
        float w = Mathf.Min(sw * 0.8f, u * 160f);
        var st = Cabe(3.5f, FontStyle.Normal, TextAnchor.UpperLeft, texto, w - u * 10f, sh * 0.32f, out var lineas);
        float alto = lineas.Count * st.fontSize * Interlineado;
        float h = Mathf.Max(u * 12f, alto + u * 7.5f);
        var caja = new Rect((sw - w) / 2f, sh - h - u * 6f, w, h);
        Marco(caja, alpha);
        var placaSt = Estilo(2.9f, FontStyle.Bold, TextAnchor.MiddleCenter);
        float pw = placaSt.CalcSize(new GUIContent(quien)).x + u * 6f;
        var placa = new Rect(caja.x + u * 4f, caja.y - u * 3.2f, pw, u * 5f);
        Marco(placa, alpha);
        Texto(placa, quien, placaSt, new Color(Oro.r, Oro.g, Oro.b, alpha));
        // centrado a lo alto, un poco abajo por la placa del nombre
        Lineas(lineas, letras, new Rect(caja.x + u * 5f, caja.y + (h - alto) / 2f + u * 0.8f, caja.width - u * 10f, alto), st, false, new Color(0.97f, 0.95f, 0.9f, alpha));
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
