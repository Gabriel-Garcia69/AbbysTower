using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Intro como imágenes que van pasando (pixel art con zoom lento, fundidos y el texto del guion).
/// Lee los PNG de Assets/Resources/Intro/ (01_*.png … 12_*.png, 16:9, por ejemplo 640x360).
/// Si falta la carpeta, Cinematica sigue usando la cinemática 3D de siempre (ver Cinematica.Start).
/// Saltar: mantener Esc / Start (1 s). Avanzar el texto: Espacio / Enter / clic / A.
/// El texto sale de Historia.cs, igual que en la cinemática 3D.
/// </summary>
public class IntroImagenes : MonoBehaviour
{
    public string siguienteEscena = "Exterior";

    struct Pieza
    {
        public int imagen;                 // número del PNG (01..12)
        public Historia.Linea[] lineas;    // vacío = solo imagen
        public float minimo;               // segundos mínimos si no hay texto
        public Pieza(int i, float min, params Historia.Linea[] l) { imagen = i; lineas = l; minimo = min; }
    }

    static Historia.Linea N(string t) => new Historia.Linea("", t);

    static Pieza[] Guion() => new[]
    {
        new Pieza(1,  3f, N(Historia.Torre[0])),
        new Pieza(2,  3.5f),
        new Pieza(3,  3f, N(Historia.Abismo[0])),
        new Pieza(4,  3f, N(Historia.Abismo[1])),
        new Pieza(5,  3f, N(Historia.Canalizadores[0])),
        new Pieza(6,  3f, N(Historia.Minerales[0])),
        new Pieza(7,  3f, Historia.Funeral[0], Historia.Funeral[1]),
        new Pieza(8,  3f, Historia.Funeral[2]),
        // aquí va la tarjeta "Año 2035" (se dibuja sola)
        new Pieza(9,  3f, Historia.Discurso[0], Historia.Discurso[1]),
        new Pieza(10, 3f, Historia.Discurso[2]),
        new Pieza(11, 3f, Historia.Discurso[3], Historia.Discurso[4]),
        new Pieza(12, 3f, Historia.Despedida[0], Historia.Despedida[1]),
        new Pieza(13, 3f, Historia.Despedida[2], Historia.Despedida[3]),   // el colgante en sus manos
    };

    const int ImagenAntesDeTarjeta = 8;

    /// <summary>¿Hay imágenes de intro en el proyecto? (si no, se usa la cinemática 3D)</summary>
    public static bool Hay() => Resources.LoadAll<Texture2D>("Intro").Length >= 3;

    // Nitidez: cada PNG (256x144) se amplía x4 con filtro Point a un RenderTexture (1024x576) y el Ken Burns muestra ese RT con filtro
    // suave: los píxeles quedan nítidos y el zoom y el paneo no saltan (con Point directo a escala fraccionaria los píxeles bailan).
    const int Ampliar = 4;
    readonly Dictionary<int, RenderTexture> imgs = new Dictionary<int, RenderTexture>();
    readonly Dictionary<RenderTexture, Texture2D> origen = new Dictionary<RenderTexture, Texture2D>();
    Texture actual;
    float t, dur = 1f, negro = 1f, alphaTexto, tTexto, escHeld;
    int dir;
    bool presionado, terminando;
    string quien = "", texto = "", tarjeta = "", tarjetaChica = "";
    bool mostrarTitulo;
    GUIStyle sNarr, sQuien, sTarj, sTarjChica, sTit;
    Texture2D pix;

    void Start()
    {
        var player = GameObject.FindWithTag("Player"); if (player != null) player.SetActive(false);
        var fd = FindAnyObjectByType<FloorDirector>(); if (fd != null) fd.gameObject.SetActive(false);

        foreach (var tx in Resources.LoadAll<Texture2D>("Intro"))
        {
            tx.filterMode = FilterMode.Point;      // pixel art nítido al escalar
            tx.wrapMode = TextureWrapMode.Clamp;
            var n = tx.name; int k = 0;
            for (int i = 0; i < n.Length && char.IsDigit(n[i]); i++) k = k * 10 + (n[i] - '0');
            if (k > 0) imgs[k] = CrearRT(tx);
        }
        pix = new Texture2D(1, 1); pix.SetPixel(0, 0, Color.white); pix.Apply();
        StartCoroutine(Reproducir());
    }

    RenderTexture CrearRT(Texture2D src)
    {
        int w = Mathf.Min(src.width * Ampliar, 4096), h = Mathf.Min(src.height * Ampliar, 4096);
        var rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
        {
            name = "Intro_" + src.name, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, useMipMap = false, autoGenerateMips = false,
        };
        rt.Create();
        origen[rt] = src;
        Rehacer(rt);
        return rt;
    }

    /// <summary>Vuelve a dibujar la imagen en su RT (un RT puede perder su contenido, p. ej. al cambiar de ventana).</summary>
    void Rehacer(RenderTexture rt)
    {
        var src = origen[rt];
        src.filterMode = FilterMode.Point;   // la ampliación es con Point: cada píxel original pasa a ser un bloque de 4x4
        Graphics.Blit(src, rt);
    }

    void OnDestroy()
    {
        foreach (var rt in imgs.Values) if (rt != null) rt.Release();
    }

    void Update()
    {
        if (actual is RenderTexture rtActual && !rtActual.IsCreated()) { rtActual.Create(); Rehacer(rtActual); }
        var kb = Keyboard.current; var mouse = Mouse.current; var pad = Gamepad.current;
        if ((kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            || (mouse != null && mouse.leftButton.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame))
            presionado = true;
        bool esc = (kb != null && kb.escapeKey.isPressed) || (pad != null && pad.startButton.isPressed);
        escHeld = esc ? escHeld + Time.deltaTime : 0f;
        if (escHeld > 1f && !terminando) { terminando = true; StopAllCoroutines(); StartCoroutine(Salir()); }
        t += Time.deltaTime; tTexto += Time.deltaTime;
    }

    // ------------------------------------------------------------------ guion

    IEnumerator Reproducir()
    {
        var piezas = Guion();
        foreach (var p in piezas)
        {
            actual = imgs.TryGetValue(p.imagen, out var rtImg) ? rtImg : null;   // falta una imagen: se salta la parte visual, el texto sigue
            dir = p.imagen % 4; t = 0f; dur = 1f;
            texto = quien = ""; alphaTexto = 0f;
            yield return Fundir(0f, 0.9f);

            if (p.lineas.Length == 0) { dur = p.minimo; yield return Esperar(p.minimo); }
            else
            {
                dur = 0f; foreach (var l in p.lineas) dur += Duracion(l.texto);
                foreach (var l in p.lineas) yield return Decir(l);
            }
            texto = quien = ""; alphaTexto = 0f;
            yield return Fundir(1f, 0.8f);

            if (p.imagen == ImagenAntesDeTarjeta)
            {
                tarjeta = Historia.Anio2035; tarjetaChica = "";
                yield return Esperar(2.8f);
                tarjeta = "";
            }
        }
        yield return Salir();
    }

    static float Duracion(string s) => Mathf.Clamp(2.2f + s.Length / 16f, 3f, 11f);

    IEnumerator Salir()
    {
        yield return Fundir(1f, 1f);
        texto = quien = ""; actual = null;
        mostrarTitulo = true;
        yield return Fundir(0f, 1f);
        yield return Esperar(3f);
        yield return Fundir(1f, 1f);
        Cargador.Ir(siguienteEscena);
    }

    IEnumerator Fundir(float hasta, float d)
    {
        float desde = negro, tt = 0f;
        while (tt < d) { tt += Time.deltaTime; negro = Mathf.Lerp(desde, hasta, tt / d); yield return null; }
        negro = hasta;
    }

    IEnumerator Esperar(float s) { float tt = 0f; presionado = false; while (tt < s && !presionado) { tt += Time.deltaTime; yield return null; } }

    IEnumerator Decir(Historia.Linea l)
    {
        quien = l.quien; texto = l.texto; tTexto = 0f; presionado = false;
        float espera = Duracion(l.texto), tt = 0f;
        while (alphaTexto < 1f) { alphaTexto = Mathf.MoveTowards(alphaTexto, 1f, Time.deltaTime * 4f); yield return null; }
        while (tt < espera)
        {
            tt += Time.deltaTime;
            if (presionado)
            {   // el primer toque completa el texto; el segundo pasa al siguiente
                if (tTexto * 45f < texto.Length) { tTexto = texto.Length; presionado = false; }
                else break;
            }
            yield return null;
        }
        while (alphaTexto > 0f) { alphaTexto = Mathf.MoveTowards(alphaTexto, 0f, Time.deltaTime * 4f); yield return null; }
    }

    // ------------------------------------------------------------------ dibujo

    void Estilos()
    {
        if (sNarr != null) return;
        sNarr = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = true };
        sNarr.normal.textColor = sNarr.hover.textColor = new Color(0.95f, 0.93f, 0.88f);
        sQuien = new GUIStyle(sNarr) { fontStyle = FontStyle.Bold };
        sQuien.normal.textColor = sQuien.hover.textColor = new Color(1f, 0.82f, 0.45f);
        sTarj = new GUIStyle(sNarr) { fontStyle = FontStyle.Bold };
        sTarj.normal.textColor = sTarj.hover.textColor = new Color(0.9f, 0.85f, 1f);
        sTarjChica = new GUIStyle(sNarr);
        sTarjChica.normal.textColor = sTarjChica.hover.textColor = new Color(0.7f, 0.65f, 0.8f);
        sTit = new GUIStyle(sTarj);
    }

    void OnGUI()
    {
        if (Event.current.type != EventType.Repaint) return;
        Estilos();
        float W = Screen.width, H = Screen.height;
        GUI.color = Color.black; GUI.DrawTexture(new Rect(0, 0, W, H), pix);

        if (actual != null)
        {   // "cover" + zoom lento + un poco de paneo (la dirección cambia según la imagen)
            float prog = Mathf.Clamp01(t / Mathf.Max(0.1f, dur + 1.7f));
            bool zoomOut = dir == 3;
            float z = zoomOut ? Mathf.Lerp(1.12f, 1.02f, prog) : Mathf.Lerp(1.02f, 1.12f, prog);
            float ar = (float)actual.width / actual.height;
            float rw = H * ar, rh = H;
            if (rw < W) { rw = W; rh = W / ar; }
            rw *= z; rh *= z;
            float px = (dir == 1 ? 1f : dir == 2 ? -1f : 0f) * Mathf.Lerp(-0.02f, 0.02f, prog) * W;
            float py = (dir == 0 ? 1f : 0f) * Mathf.Lerp(0.015f, -0.015f, prog) * H;
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect((W - rw) * 0.5f + px, (H - rh) * 0.5f + py, rw, rh), actual, ScaleMode.StretchToFill);
            // viñeta suave arriba y abajo
            GUI.color = new Color(0, 0, 0, 0.35f); GUI.DrawTexture(new Rect(0, H * 0.82f, W, H * 0.18f), pix);
            GUI.color = new Color(0, 0, 0, 0.25f); GUI.DrawTexture(new Rect(0, 0, W, H * 0.08f), pix);
        }

        if (alphaTexto > 0.01f && texto.Length > 0)
        {
            // el mismo cuadro de la historia que en el resto del juego: crece con el texto y achica la letra si no cabe
            int n = Mathf.Clamp(Mathf.FloorToInt(tTexto * 45f), 0, texto.Length);
            GUI.color = Color.white;
            if (quien.Length > 0) CuadroNarracion.Dialogo(quien, texto, n, alphaTexto);
            else CuadroNarracion.Narracion(texto, n, alphaTexto);
        }

        if (tarjeta.Length > 0)
        {
            sTarj.fontSize = Mathf.RoundToInt(H / 9f);
            GUI.color = new Color(1, 1, 1, 1f - negro);
            GUI.Label(new Rect(0, H * 0.35f, W, H * 0.3f), tarjeta, sTarj);
        }
        if (mostrarTitulo)
        {
            // en una sola línea y a lo ancho que quepa: la fuente pixel es muy ancha y con ajuste de línea se partía en dos
            // renglones que no cabían en el cuadro (se cortaba arriba); sin negrita falsa, que la ensuciaba
            sTit.font = FuentePixel.FuenteLogo; sTit.fontStyle = FontStyle.Normal; sTit.wordWrap = false; sTit.clipping = TextClipping.Overflow;
            sTit.fontSize = Mathf.RoundToInt(H / 9f);
            float ancho = sTit.CalcSize(new GUIContent(Historia.Titulo)).x;
            if (ancho > W * 0.86f) sTit.fontSize = Mathf.Max(8, Mathf.FloorToInt(sTit.fontSize * W * 0.86f / ancho));
            sTarjChica.fontSize = Mathf.RoundToInt(H / 22f);
            GUI.color = new Color(1, 1, 1, 1f - negro);
            GUI.Label(new Rect(0, H * 0.3f, W, H * 0.22f), Historia.Titulo, sTit);
            GUI.Label(new Rect(0, H * 0.52f, W, H * 0.1f), Historia.Subtitulo, sTarjChica);
        }

        if (negro > 0.001f) { GUI.color = new Color(0, 0, 0, negro); GUI.DrawTexture(new Rect(0, 0, W, H), pix); }

        GUI.color = new Color(1, 1, 1, 0.5f);
        sTarjChica.fontSize = Mathf.RoundToInt(H / 40f);
        GUI.Label(new Rect(0, H - H * 0.04f, W, H * 0.04f), Mando.T("Mantén Esc: saltar", "Mantén " + Mando.Start + ": saltar"), sTarjChica);
        GUI.color = Color.white;
    }
}
