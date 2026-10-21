using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Cinemática inicial (guion "Contexto y presentación de la torre" + "Presentación y contexto de Abby"):
/// la torre aparece en la ciudad, el ejército frente al abismo, los canalizadores contra las criaturas, los minerales,
/// el funeral (2030), el discurso del capitán (2035) y la despedida de Abby y su hermano. Al final pasa al tutorial.
/// Los sets los arma DioramaBuilder (layout Intro); aquí solo se mueve la cámara y se muestran los cuadros de texto.
/// Espacio / Enter / clic: avanzar el texto. Mantener Esc: saltar la cinemática.
/// </summary>
public class Cinematica : MonoBehaviour
{
    public string siguienteEscena = "Expedicion";

    Camera cam;
    ExteriorSky sky;
    Light sun;
    Coroutine toma;
    GameObject lluvia;
    Vector3 shakeOffset;
    float shake;

    // cuadro de texto actual
    enum Modo { Nada, Narracion, Dialogo, Tarjeta }
    Modo modo;
    string quien, texto, tarjetaChica;
    float tTexto, alphaTexto, franjas = 1f, negro = 1f, escHeld;
    bool presionado, terminando;

    // ------------------------------------------------------------------ arranque

    void Start()
    {
        // fuera el jugador, el HUD y la cámara de juego: aquí manda la cinemática
        var player = GameObject.FindWithTag("Player");
        if (player != null) player.SetActive(false);
        var fd = FindAnyObjectByType<FloorDirector>();
        if (fd != null) fd.gameObject.SetActive(false);
        var main = Camera.main;
        var go = new GameObject("CamaraCinematica");
        cam = go.AddComponent<Camera>();
        if (main != null)
        {
            cam.CopyFrom(main);
            var rig = main.transform.root;
            rig.gameObject.SetActive(false);
        }
        cam.nearClipPlane = 0.2f; cam.farClipPlane = 3000f;
        var data = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
        go.AddComponent<AudioListener>();
        go.tag = "MainCamera";
        sky = FindAnyObjectByType<ExteriorSky>();
        lluvia = GameObject.Find("Lluvia");
        sun = RenderSettings.sun;
        StartCoroutine(Guion());
    }

    void Update()
    {
        var kb = Keyboard.current; var mouse = Mouse.current; var pad = Gamepad.current;
        if ((kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame))
            || (mouse != null && mouse.leftButton.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame))
            presionado = true;
        bool esc = (kb != null && kb.escapeKey.isPressed) || (pad != null && pad.startButton.isPressed);
        escHeld = esc ? escHeld + Time.deltaTime : 0f;
        if (escHeld > 1f && !terminando) { terminando = true; StopAllCoroutines(); StartCoroutine(Salir()); }
        tTexto += Time.deltaTime;
        if (shake > 0f)
        {
            shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime * 0.6f);
            shakeOffset = new Vector3(Mathf.PerlinNoise(Time.time * 20f, 0f) - 0.5f, Mathf.PerlinNoise(0f, Time.time * 20f) - 0.5f, 0f) * shake;
        }
        else shakeOffset = Vector3.zero;
    }

    void LateUpdate()
    {
        if (cam != null && shakeOffset != Vector3.zero) cam.transform.position += cam.transform.rotation * shakeOffset;
    }

    // ------------------------------------------------------------------ el guion

    IEnumerator Guion()
    {
        Transform ciudad = Set("Set_Ciudad"), abismo = Set("Set_Abismo"), batalla = Set("Set_Batalla"), funeral = Set("Set_Funeral"), plaza = Set("Set_Plaza");
        Vector3 C = ciudad.position, A = abismo.position, B = batalla.position, F = funeral.position, P = plaza.position;

        // 1. En el año 2030... la ciudad al atardecer; luego la torre aparece
        Clima("atardecer");
        Toma(C + new Vector3(-190f, 120f, -260f), C + new Vector3(0f, 30f, 0f), C + new Vector3(-150f, 90f, -210f), C + new Vector3(0f, 40f, 0f), 14f, 38f);
        yield return Fundir(0f, 1.5f);
        yield return Narrar(Historia.Torre[0], 3f);
        var torre = Buscar(ciudad, "Torre");
        Toma(C + new Vector3(-70f, 6f, -110f), C + new Vector3(0f, 50f, 0f), C + new Vector3(-80f, 8f, -125f), C + new Vector3(0f, 120f, 0f), 9f, 46f);
        var polvo = Buscar(ciudad, "Polvo");
        if (polvo != null) foreach (var ps in polvo.GetComponentsInChildren<ParticleSystem>()) ps.Play();
        shake = 1.2f;
        if (torre != null)
        {
            Vector3 a = torre.localPosition, b = new Vector3(a.x, 0f, a.z);
            for (float t = 0f; t < 7f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 7f);
                torre.localPosition = Vector3.Lerp(a, b, k);
                if (shake < 0.8f) shake = 0.8f * (1f - k) + 0.2f;
                yield return null;
            }
            torre.localPosition = b;
        }
        yield return Esperar(2f);

        // 2. El ejército entra: las escaleras que bajan al abismo
        yield return Fundir(1f, 0.8f);
        Clima("interior");
        Toma(A + new Vector3(0f, 6f, -26f), A + new Vector3(0f, -20f, 60f), A + new Vector3(0f, 4f, -10f), A + new Vector3(0f, -40f, 110f), 20f, 50f);
        yield return Fundir(0f, 1f);
        yield return Narrar(Historia.Abismo[0], 2.5f);
        yield return Narrar(Historia.Abismo[1], 3f);

        // 3. Los canalizadores contra las criaturas; los minerales
        yield return Fundir(1f, 0.8f);
        Clima("batalla");
        Orbita(B + new Vector3(0f, 1.5f, 0f), 17f, 5.5f, 200f, 250f, 16f, 44f);
        yield return Fundir(0f, 1f);
        yield return Narrar(Historia.Canalizadores[0], 2.5f);
        var minerales = Buscar(batalla, "Minerales");
        Vector3 m = minerales != null ? minerales.position : B;
        Toma(m + new Vector3(-6f, 4f, -7f), m + Vector3.up * 0.5f, m + new Vector3(-3.5f, 2.2f, -4f), m + Vector3.up * 0.4f, 12f, 40f);
        yield return Narrar(Historia.Minerales[0], 3f);

        // 4. El funeral (2030)
        yield return Fundir(1f, 1.2f);
        Clima("lluvia");
        Toma(F + new Vector3(0f, 7f, -24f), F + new Vector3(0f, 1.5f, 2f), F + new Vector3(0f, 5f, -18f), F + new Vector3(0f, 1.3f, 2f), 10f, 40f);
        yield return Fundir(0f, 1.5f);
        yield return Esperar(2f);
        var inv = Buscar(funeral, "Invitadas");
        Vector3 iv = inv != null ? inv.position : F;
        Toma(iv + new Vector3(3.5f, 2.2f, -5f), iv + new Vector3(0f, 1.5f, 0f), iv + new Vector3(2.8f, 2f, -4.2f), iv + new Vector3(0f, 1.5f, 0f), 14f, 38f);
        yield return Decir(Historia.Funeral[0]);
        yield return Decir(Historia.Funeral[1]);
        var hermanos = Buscar(funeral, "Hermanos");
        Vector3 hv = hermanos != null ? hermanos.position : F;
        Toma(hv + new Vector3(0f, 2f, -5.5f), hv + new Vector3(0f, 1.2f, 0f), hv + new Vector3(0f, 1.7f, -4.2f), hv + new Vector3(0f, 1.2f, 0f), 12f, 36f);
        yield return Decir(Historia.Funeral[2]);
        yield return Esperar(1.5f);

        // 5. Año 2035
        yield return Fundir(1f, 1.2f);
        yield return MostrarTarjeta(Historia.Anio2035, "", 3f);

        // 6. El discurso del capitán
        Clima("manana");
        Toma(P + new Vector3(-40f, 30f, -60f), P + new Vector3(0f, 25f, 80f), P + new Vector3(-25f, 16f, -40f), P + new Vector3(0f, 10f, 60f), 12f, 42f);
        yield return Fundir(0f, 1.2f);
        yield return Esperar(2.5f);
        var escenario = Buscar(plaza, "Escenario");
        Vector3 es = escenario != null ? escenario.position : P + new Vector3(0f, 0f, 40f);
        Vector3 capi = es + new Vector3(0f, 2.8f, 0f);
        for (int i = 0; i < Historia.Discurso.Length; i++)
        {
            if (i % 2 == 0) Toma(capi + new Vector3(2.5f, 1.6f, -8f), capi + Vector3.down * 0.4f, capi + new Vector3(1.5f, 1.4f, -6.5f), capi + Vector3.down * 0.4f, 14f, 34f);   // el capitán (por encima del podio)
            else Toma(es + new Vector3(-14f, 5f, -2f), es + new Vector3(0f, 1.5f, -14f), es + new Vector3(-12f, 4f, -6f), es + new Vector3(0f, 1.5f, -14f), 14f, 40f);   // la expedición escuchando
            yield return Decir(Historia.Discurso[i]);
        }

        // 7. Abby se despide de su hermano
        yield return Fundir(1f, 1f);
        var desp = Buscar(plaza, "Despedida");
        Vector3 d = desp != null ? desp.position : P;
        Toma(d + new Vector3(-4.5f, 1.8f, -3f), d + new Vector3(0f, 1.2f, 0f), d + new Vector3(-3.6f, 1.6f, -2.4f), d + new Vector3(0f, 1.2f, 0f), 18f, 36f);
        yield return Fundir(0f, 1f);
        yield return Decir(Historia.Despedida[0]);
        yield return Decir(Historia.Despedida[1]);
        yield return Esperar(1.5f);

        yield return Salir();
    }

    IEnumerator Salir()
    {
        modo = Modo.Nada;
        yield return Fundir(1f, 1.2f);
        yield return MostrarTarjeta(Historia.Titulo, Historia.Subtitulo, 3f);
        SceneManager.LoadScene(siguienteEscena);
    }

    // ------------------------------------------------------------------ piezas del guion

    Transform Set(string name)
    {
        var go = GameObject.Find(name);
        return go != null ? go.transform : transform;
    }

    static Transform Buscar(Transform raiz, string name)
    {
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
        return null;
    }

    IEnumerator Esperar(float s) { for (float t = 0f; t < s; t += Time.deltaTime) yield return null; }

    IEnumerator Fundir(float hasta, float dur)
    {
        float desde = negro;
        for (float t = 0f; t < dur; t += Time.deltaTime) { negro = Mathf.Lerp(desde, hasta, t / dur); yield return null; }
        negro = hasta;
    }

    IEnumerator Narrar(string s, float sostener) { yield return Mostrar(Modo.Narracion, "", s, sostener); }
    IEnumerator Decir(Historia.Linea l) { yield return Mostrar(Modo.Dialogo, l.quien, l.texto, 3f); }

    IEnumerator Mostrar(Modo m, string q, string s, float sostener)
    {
        modo = m; quien = q; texto = s; tTexto = 0f; presionado = false;
        for (float t = 0f; t < 0.25f; t += Time.deltaTime) { alphaTexto = t / 0.25f; yield return null; }
        alphaTexto = 1f;
        // se escribe; un toque lo completa, otro (o el tiempo) pasa al siguiente
        float lectura = sostener + s.Length * 0.025f;
        while (true)
        {
            bool completo = CuadroNarracion.Letras(texto, tTexto) >= texto.Length;
            if (presionado)
            {
                presionado = false;
                if (!completo) tTexto = 999f;
                else break;
            }
            if (completo && tTexto - texto.Length / 45f > lectura) break;
            yield return null;
        }
        for (float t = 0f; t < 0.25f; t += Time.deltaTime) { alphaTexto = 1f - t / 0.25f; yield return null; }
        alphaTexto = 0f; modo = Modo.Nada;
    }

    IEnumerator MostrarTarjeta(string grande, string chico, float dur)
    {
        modo = Modo.Tarjeta; texto = grande; tarjetaChica = chico;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            alphaTexto = Mathf.Clamp01(Mathf.Min(t / 0.6f, (dur - t) / 0.6f));
            yield return null;
        }
        modo = Modo.Nada; alphaTexto = 0f;
    }

    void Toma(Vector3 p0, Vector3 l0, Vector3 p1, Vector3 l1, float dur, float fov)
    {
        if (toma != null) StopCoroutine(toma);
        toma = StartCoroutine(Mover(p0, l0, p1, l1, dur, fov));
    }

    IEnumerator Mover(Vector3 p0, Vector3 l0, Vector3 p1, Vector3 l1, float dur, float fov)
    {
        cam.fieldOfView = fov;
        for (float t = 0f; ; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
            Vector3 p = Vector3.Lerp(p0, p1, k), l = Vector3.Lerp(l0, l1, k);
            cam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(l - p));
            yield return null;
        }
    }

    void Orbita(Vector3 centro, float radio, float alto, float a0, float a1, float dur, float fov)
    {
        if (toma != null) StopCoroutine(toma);
        toma = StartCoroutine(Orbitar(centro, radio, alto, a0, a1, dur, fov));
    }

    IEnumerator Orbitar(Vector3 c, float r, float h, float a0, float a1, float dur, float fov)
    {
        cam.fieldOfView = fov;
        for (float t = 0f; ; t += Time.deltaTime)
        {
            float a = Mathf.Lerp(a0, a1, Mathf.Clamp01(t / dur)) * Mathf.Deg2Rad;
            Vector3 p = c + new Vector3(Mathf.Sin(a) * r, h, Mathf.Cos(a) * r);
            cam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(c - p));
            yield return null;
        }
    }

    // ------------------------------------------------------------------ ambiente de cada escena

    void Clima(string c)
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        if (lluvia != null) lluvia.SetActive(c == "lluvia");
        switch (c)
        {
            case "interior":   // adentro de la torre: oscuro, la luz la ponen las linternas
                Sol(new Color(0.45f, 0.5f, 0.75f), 0.15f, new Vector3(60f, 30f, 0f));
                Ambiente(new Color(0.08f, 0.09f, 0.14f), new Color(0.05f, 0.05f, 0.09f), new Color(0.01f, 0.01f, 0.02f));
                Niebla(new Color(0.01f, 0.01f, 0.02f), 25f, 170f);
                Cielo(new Color(0f, 0f, 0f), new Color(0.02f, 0.02f, 0.04f), 0f);
                break;
            case "batalla":
                Sol(new Color(0.5f, 0.6f, 1f), 0.35f, new Vector3(55f, 40f, 0f));
                Ambiente(new Color(0.16f, 0.2f, 0.32f), new Color(0.1f, 0.12f, 0.2f), new Color(0.03f, 0.03f, 0.05f));
                Niebla(new Color(0.03f, 0.05f, 0.09f), 20f, 140f);
                Cielo(new Color(0.01f, 0.01f, 0.03f), new Color(0.04f, 0.06f, 0.1f), 0f);
                break;
            case "lluvia":     // el funeral: gris, frío, lluvia
                Sol(new Color(0.75f, 0.8f, 0.9f), 0.7f, new Vector3(50f, 150f, 0f));
                Ambiente(new Color(0.45f, 0.48f, 0.55f), new Color(0.35f, 0.37f, 0.42f), new Color(0.15f, 0.15f, 0.17f));
                Niebla(new Color(0.5f, 0.53f, 0.58f), 20f, 220f);
                Cielo(new Color(0.36f, 0.39f, 0.45f), new Color(0.55f, 0.57f, 0.62f), 0.95f);
                break;
            case "manana":     // 2035, mañana clara
                Sol(new Color(1f, 0.92f, 0.8f), 1.7f, new Vector3(35f, -20f, 0f));
                Ambiente(new Color(0.6f, 0.68f, 0.82f), new Color(0.55f, 0.55f, 0.55f), new Color(0.22f, 0.2f, 0.2f));
                Niebla(new Color(0.72f, 0.8f, 0.9f), 150f, 1100f);
                Cielo(new Color(0.35f, 0.55f, 0.88f), new Color(0.85f, 0.88f, 0.9f), 0.35f);
                break;
            default:           // atardecer en la ciudad
                Sol(new Color(1f, 0.6f, 0.35f), 1.6f, new Vector3(14f, -60f, 0f));
                Ambiente(new Color(0.55f, 0.45f, 0.55f), new Color(0.5f, 0.38f, 0.34f), new Color(0.15f, 0.12f, 0.14f));
                Niebla(new Color(0.85f, 0.56f, 0.46f), 160f, 1100f);
                Cielo(new Color(0.22f, 0.27f, 0.56f), new Color(1f, 0.52f, 0.26f), 0.45f);
                break;
        }
    }

    void Sol(Color c, float i, Vector3 rot) { if (sun == null) return; sun.color = c; sun.intensity = i; sun.transform.rotation = Quaternion.Euler(rot); }
    static void Ambiente(Color s, Color e, Color g) { RenderSettings.ambientSkyColor = s; RenderSettings.ambientEquatorColor = e; RenderSettings.ambientGroundColor = g; }
    static void Niebla(Color c, float a, float b) { RenderSettings.fogColor = c; RenderSettings.fogStartDistance = a; RenderSettings.fogEndDistance = b; }
    void Cielo(Color zenith, Color horizon, float nubes)
    {
        if (sky == null) return;
        sky.driveAmbient = false; sky.driveFog = false;
        sky.zenith = zenith; sky.horizon = horizon; sky.ground = RenderSettings.fogColor; sky.cloudCover = nubes;
        sky.cloudColor = Color.Lerp(horizon, Color.white, 0.4f);
        sky.sunColor = sun != null ? sun.color * 1.5f : Color.white;
        sky.Apply();
    }

    // ------------------------------------------------------------------ pantalla

    void OnGUI()
    {
        GUI.depth = -1000;
        CuadroNarracion.Franjas(franjas);
        int letras = texto != null ? CuadroNarracion.Letras(texto, tTexto) : 0;
        switch (modo)
        {
            case Modo.Narracion: CuadroNarracion.Narracion(texto, letras, alphaTexto); break;
            case Modo.Dialogo: CuadroNarracion.Dialogo(quien, texto, letras, alphaTexto); break;
        }
        CuadroNarracion.Negro(negro);
        if (modo == Modo.Tarjeta) CuadroNarracion.Tarjeta(texto, tarjetaChica, alphaTexto);
        CuadroNarracion.Ayuda(escHeld > 0f ? "Saltando…" : "Espacio / Enter: continuar   ·   Mantén Esc: saltar", 1f);
    }
}
