using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// El final (GDD, Acto 3), en su propio escenario y sin enemigos: Abby llega a la última sala y encuentra a la sombra tendida a
/// unos escalones de la salida, con el colgante. Entiende que Dante venía de regreso. El jefe dice "Abby…" y corte a negro.
/// Los créditos corren sobre la casa en la nieve, con la luz de Abby prendida; al terminar, se enciende la de Dante.
/// </summary>
public class EscenaFinal : MonoBehaviour
{
    float negro = 1f, textoT, tarjetaA, recuerdoA, creditosT;
    string quien, texto, tarjeta, subtarjeta;
    Texture2D recuerdo;
    bool creditos;
    SpriteRenderer dante;
    Camera cam;

    IEnumerator Start()
    {
        DanteFinal.Activo = true;
        ReintentoJefe.Escena = null;
        Progreso.Borrar();
        Musica.Poner(Musica.Pista.Ninguna);
        cam = Camera.main;
        CameraShake.Shake(0f, 0f);
        foreach (var t in FindObjectsByType<TiltShiftFocus>()) t.enabled = false;
        // un volumen propio manda sobre los del mapa: sin desenfoque y con la imagen abierta
        var vgo = new GameObject("VolumenFinal");
        var vol = vgo.AddComponent<UnityEngine.Rendering.Volume>();
        vol.isGlobal = true; vol.priority = 1000f;
        var perfil = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
        perfil.Add<UnityEngine.Rendering.Universal.DepthOfField>(true).mode.Override(UnityEngine.Rendering.Universal.DepthOfFieldMode.Off);
        perfil.Add<UnityEngine.Rendering.Universal.Vignette>(true).intensity.Override(0.22f);
        perfil.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true).postExposure.Override(0.45f);
        vol.sharedProfile = perfil;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.5f, 0.32f, 0.5f);
        RenderSettings.ambientEquatorColor = new Color(0.34f, 0.22f, 0.36f);
        RenderSettings.ambientGroundColor = new Color(0.16f, 0.1f, 0.18f);
        foreach (var b in FindObjectsByType<MouseOrbitCamera>()) b.enabled = false;
        foreach (var b in FindObjectsByType<CameraRig>()) b.enabled = false;
        var p = GameObject.FindWithTag("Player");
        var abby = p != null ? p.GetComponentInChildren<AbbySpriteAnimator>(true) : null;
        if (p != null)
            foreach (var b in p.GetComponents<MonoBehaviour>())
                if (b is PlayerInputReader || b is AbbyAttack || b is AbbyBlock || b is AbbyDash || b is PlayerMotor || b is PlayerController) b.enabled = false;
        var cc = p != null ? p.GetComponent<CharacterController>() : null;
        if (cc != null) cc.enabled = false;
        if (cam != null) { cam.transform.SetParent(null, true); cam.nearClipPlane = 0.1f; cam.fieldOfView = 46f; cam.farClipPlane = Mathf.Max(cam.farClipPlane, 900f); }

        // en los escalones: la sombra tendida, con un brazo hacia la puerta, y el colgante un poco más abajo
        float Suelo(float x, float z, float y0)
        {
            return Physics.Raycast(new Vector3(x, y0 + 12f, z), Vector3.down, out var h0, 30f, ~0, QueryTriggerInteraction.Ignore) ? h0.point.y + 0.05f : y0;
        }
        Vector3 cuerpo = new Vector3(0.5f, DioramaBuilder.FinalDescanso + 0.02f, 23f);
        Vector3 pos = new Vector3(-0.3f, DioramaBuilder.FinalDescanso + 0.02f, 20.8f);
        Vector3 ante = new Vector3(-0.4f, DioramaBuilder.FinalDescanso + 0.05f, 19.6f);
        dante = CrearDante(cuerpo, abby);
        var colgante = DanteFinal.CrearColgante(pos);
        var sombra = StartCoroutine(SombraSube());
        Vector3 puerta = new Vector3(0f, DioramaBuilder.FinalRellano + 11f, DioramaBuilder.FinalPuertaZ);

        // Abby camina sola mientras la cámara cambia de plano
        Vector3 inicio = DioramaBuilder.FinalSpawn;
        if (p != null) p.transform.position = inicio;
        Vector3 Yo() => p != null ? p.transform.position : ante;
        Vector3 cara = Vector3.up * 1f;
        if (abby != null) { abby.enEscena = true; abby.caminando = true; abby.caminarHacia = (ante - inicio).normalized; }
        float durCamino = 21f;
        bool llego = false;
        StartCoroutine(Caminar(p, inicio, ante, durCamino, Suelo, abby, cuerpo, () => llego = true));
        // una melodía suave mientras camina sola; se apaga cuando lo reconoce
        Musica.Cinematica(false);
        Musica.Poner(Musica.Pista.Final);   // "Despedida": piano solo; el violín entra cuando lo encuentra

        // pocas tomas, largas y lentas: que se sienta el silencio de ese lugar
        // 1) muy lejos: el pedazo de torre en el vacío, la puerta encendida y Abby, un punto que camina
        StartCoroutine(Fundir(0f, 3.5f));
        yield return Plano(new Vector3(-46f, 14f, -52f), new Vector3(-41f, 12.5f, -46f), () => new Vector3(0f, 8f, 20f), 34f, 8f);
        // 2) detrás de ella: la escalinata, la luz al fondo, y algo tendido a media subida
        yield return Pasar();
        yield return Plano(() => Yo() + new Vector3(1f, 2.4f, -7.5f), () => Yo() + new Vector3(0.5f, 3f, -6f), () => Yo() + new Vector3(0f, 2.2f, 12f), 44f, 9f);
        // 3) de lado, quieta la cámara: ella sube los últimos escalones y se detiene frente a él
        yield return Pasar();
        int idLlegada = planoId + 1;
        StartCoroutine(Plano(cuerpo + new Vector3(10f, 2.6f, -2.5f), cuerpo + new Vector3(9.4f, 2.6f, -2f), () => Vector3.Lerp(cuerpo, Yo(), 0.5f) + Vector3.up * 0.8f, 36f, 30f));
        while (!llego) yield return null;
        yield return new WaitForSeconds(1.6f);
        yield return Decir(Historia.Protagonista, "¿Quién... eres?", 3f);
        yield return new WaitForSeconds(0.8f);

        // 4) el colgante en el escalón, muy cerca; ella se arrodilla
        yield return Pasar();
        Sonido.Tocar(Sonido.Efecto.Fuente, 0.8f, 0.7f);
        var calida = StartCoroutine(LuzCalida(colgante));
        StartCoroutine(Plano(pos + new Vector3(-1.7f, 0.75f, 0.1f), pos + new Vector3(-1.3f, 0.6f, 0.05f), () => pos + Vector3.up * 0.1f, 30f, 6f));   // de lado: de frente, la espada de él tapaba el cuadro
        yield return new WaitForSeconds(1.8f);
        if (abby != null) abby.deRodillas = true;
        yield return Decir(Historia.Protagonista, "Este colgante...", 2.6f);
        // 5) su cara, de rodillas
        yield return Pasar();
        StartCoroutine(Plano(Yo() + new Vector3(-3.1f, 0.85f, 0.9f), Yo() + new Vector3(-2.8f, 0.82f, 0.8f), () => Yo() + Vector3.up * 0.7f, 30f, 6f));
        yield return Decir(Historia.Protagonista, "Era de papá. Yo te lo di.", 3.4f);

        // los recuerdos
        yield return Recuerdo("Intro/12_despedida", Historia.Hermano, "Con él al frente estaremos bien. Volveré...", 4.6f);
        yield return Recuerdo("Intro/13_colgante", Historia.Hermano, "Te lo devuelvo en persona. Te lo prometo.", 4.6f);
        yield return Recuerdo("Intro/14_regreso", Historia.Hermano, "Voy de regreso, Abby. Te dije que volvería.", 5.2f);

        // 6) desde lo alto, por detrás: los dos, tan chiquitos, y la puerta a unos pasos
        StartCoroutine(Plano(new Vector3(4f, 15f, 35f), new Vector3(2.5f, 14f, 33.5f), () => Vector3.Lerp(cuerpo, ante, 0.5f) + Vector3.up * 0.4f, 38f, 11f));
        yield return Decir(Historia.Protagonista, "Las huellas. Las hojas.", 3f);
        yield return Decir(Historia.Protagonista, "Venías de regreso... todo este tiempo.", 3.6f);
        yield return Decir(Historia.Protagonista, "Estabas a unos escalones.", 3.4f);
        // 7) detrás de ella, abajo: la puerta se abre sola y la luz los cubre; a él se le deshace la sombra. Una sola toma
        yield return Pasar();
        StartCoroutine(Plano(() => Yo() + new Vector3(1.6f, 1.1f, -4.2f), () => Yo() + new Vector3(1.2f, 1.4f, -3.2f), () => Vector3.Lerp(cuerpo, puerta, 0.35f), 44f, 13f));
        yield return AbrirPuerta();
        StopCoroutine(sombra);
        CombatFx.Burst(cuerpo + Vector3.up * 0.5f, new Color(0.35f, 0.2f, 0.5f), 60, 3f, 0.3f, 2.2f, -0.8f);
        if (dante != null)
        {
            Color c0 = dante.color, c1 = new Color(0.78f, 0.74f, 0.74f);
            for (float t = 0f; t < 3.5f; t += Time.deltaTime) { dante.color = Color.Lerp(c0, c1, t / 3.5f); yield return null; }
        }
        yield return new WaitForSeconds(1.4f);
        Sonido.Tocar(Sonido.Efecto.Latido, 1f);
        // 8) desde él, mirándola
        yield return Pasar();
        StartCoroutine(Plano(cuerpo + new Vector3(0.3f, 0.5f, -0.6f), cuerpo + new Vector3(0.25f, 0.5f, -0.8f), () => Yo() + Vector3.up * 0.75f, 34f, 7f));
        yield return new WaitForSeconds(1.6f);

        // la música se apaga antes de su voz
        Musica.Cruce = 3f;
        Musica.Poner(Musica.Pista.Ninguna);
        yield return new WaitForSeconds(1.4f);
        Musica.Cruce = 2.5f;
        // "Abby…": corte a negro
        yield return Decir("???", "Abby…", 3.6f);
        negro = 1f;
        StopCoroutine(calida);
        planoId++;
        yield return new WaitForSeconds(1.6f);
        Sonido.Tocar(Sonido.Efecto.Latido, 0.8f, 0.9f);
        yield return new WaitForSeconds(1.2f);
        tarjeta = "Volví."; subtarjeta = "";
        for (float t = 0f; t < 3.8f; t += Time.deltaTime) { tarjetaA = Mathf.Clamp01(Mathf.Min(t / 1.2f, (3.8f - t) / 1f)); yield return null; }
        tarjeta = null;
        yield return new WaitForSeconds(1f);

        // la casa en la nieve: la luz de su cuarto, prendida, esperando; los créditos corren encima
        PonerCasa();
        Musica.Poner(Musica.Pista.Menu);
        StartCoroutine(Fundir(0f, 3f));
        creditos = true;
        Vector3 cas = DioramaBuilder.FinalCasa;
        Vector3 c0c = cas + new Vector3(0f, 2.6f, 6f), c1c = cas + new Vector3(0f, 3f, 14f);
        for (creditosT = 0f; creditosT < DanteFinal.DuracionCreditos; creditosT += Time.deltaTime)
        {
            float e = creditosT / DanteFinal.DuracionCreditos;
            if (cam != null)
            {
                cam.transform.position = Vector3.Lerp(c0c, c1c, e);
                cam.transform.rotation = Quaternion.LookRotation(cas + new Vector3(0f, 4f, 34f) - cam.transform.position);
            }
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (creditosT > 3f && kb != null && (kb.enterKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)) break;
            yield return null;
        }
        creditos = false;
        yield return new WaitForSeconds(1.5f);
        // la otra ventana se enciende
        EncenderVentana();
        Sonido.Tocar(Sonido.Efecto.Fuente, 0.9f, 0.85f);
        yield return new WaitForSeconds(3.2f);
        tarjeta = "Continuará…"; subtarjeta = Historia.Titulo;
        for (float t = 0f; t < 4f; t += Time.deltaTime) { tarjetaA = Mathf.Clamp01(Mathf.Min(t / 0.8f, (4f - t) / 0.8f)); yield return null; }
        tarjeta = null;
        yield return Fundir(1f, 2f);
        DanteFinal.Activo = false;
        Cargador.Ir("Menu");
    }

    void OnDisable() { DanteFinal.Activo = false; }

    SpriteRenderer CrearDante(Vector3 pos, AbbySpriteAnimator abby)
    {
        // Dante chibi tendido (PixelLab, el mismo personaje), con la cabeza hacia la puerta; se ve del lado que toque según la cámara
        var go = new GameObject("DanteTendido");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        var abbySr = abby != null ? abby.GetComponent<SpriteRenderer>() : null;
        if (abbySr != null) sr.sharedMaterial = abbySr.sharedMaterial;
        var vista = go.AddComponent<MirarCamara>();
        string[] dirs = { "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west" };
        for (int i = 0; i < 8; i++) vista.poses[i] = Resources.Load<Sprite>("DanteHumano/Tendido/" + dirs[i]);
        vista.rumbo = Vector3.forward;   // hacia la puerta
        vista.sr = sr;
        sr.sprite = vista.poses[0];
        sr.color = new Color(0.13f, 0.08f, 0.2f);
        return sr;
    }

    class MirarCamara : MonoBehaviour
    {
        public Sprite[] poses = new Sprite[8];
        public Vector3 rumbo;
        public SpriteRenderer sr;
        void LateUpdate()
        {
            var c = Camera.main;
            if (c == null) return;
            Vector3 f = c.transform.forward; f.y = 0f;
            if (f.sqrMagnitude < 0.001f) return;
            transform.rotation = Quaternion.LookRotation(f, Vector3.up);
            // hacia dónde apunta su cuerpo visto desde la cámara: 0 = de espaldas a ella (norte), luego en el sentido del reloj
            float ang = Vector3.SignedAngle(f, rumbo, Vector3.up);
            int k = Mathf.RoundToInt(Mathf.Repeat(ang, 360f) / 45f) % 8;
            if (sr != null && poses[k] != null) sr.sprite = poses[k];
        }
    }

    IEnumerator SombraSube()
    {
        while (true)
        {
            if (dante != null) CombatFx.Burst(dante.transform.position + new Vector3(Random.Range(-1f, 1f), 0.3f, 0f), new Color(0.3f, 0.12f, 0.42f), 5, 1.2f, 0.24f, 2f, -0.5f);
            yield return new WaitForSeconds(0.28f);
        }
    }

    IEnumerator Fundir(float a, float dur)
    {
        float de = negro;
        for (float t = 0f; t < dur; t += Time.deltaTime) { negro = Mathf.Lerp(de, a, t / dur); yield return null; }
        negro = a;
    }

    /// <summary>Un plano: la cámara va despacio de un punto a otro (pueden seguir a Abby), siempre mirando a su objetivo.</summary>
    IEnumerator Plano(System.Func<Vector3> a, System.Func<Vector3> b, System.Func<Vector3> mira, float fov, float dur)
    {
        int id = ++planoId;
        if (cam != null) cam.fieldOfView = fov;
        for (float t = 0f; t < dur && id == planoId; t += Time.deltaTime)
        {
            if (cam != null)
            {
                float e = Mathf.SmoothStep(0f, 1f, t / dur);
                Vector3 c = Vector3.Lerp(a(), b(), e);
                cam.transform.position = c;
                cam.transform.rotation = Quaternion.LookRotation(mira() - c);
            }
            yield return null;
        }
    }

    IEnumerator Plano(Vector3 a, Vector3 b, System.Func<Vector3> mira, float fov, float dur) => Plano(() => a, () => b, mira, fov, dur);

    int planoId;

    /// <summary>Un parpadeo a negro entre planos.</summary>
    /// <summary>Paso entre tomas: se oscurece despacio y vuelve, sin parpadeo.</summary>
    IEnumerator Pasar()
    {
        yield return Fundir(1f, 0.55f);
        planoId++;
        yield return new WaitForSeconds(0.25f);
        StartCoroutine(Fundir(0f, 0.7f));
    }

    IEnumerator Corte(float dur)
    {
        planoId++;
        float de = negro;
        negro = 1f;
        yield return new WaitForSeconds(dur);
        negro = de;
    }

    IEnumerator Caminar(GameObject p, Vector3 de, Vector3 a, float dur, System.Func<float, float, float, float> suelo, AbbySpriteAnimator abby, Vector3 mira, System.Action alLlegar)
    {
        if (p == null) yield break;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            // frena en el último par de segundos
            float k = t / dur, e = k < 0.85f ? k : Mathf.Lerp(0.85f, 1f, Mathf.Sin((k - 0.85f) / 0.15f * Mathf.PI * 0.5f));
            Vector3 q = Vector3.Lerp(de, a, e);
            q.y = suelo(q.x, q.z, Mathf.Lerp(de.y, a.y, e));
            p.transform.position = q;
            yield return null;
        }
        p.transform.position = a;
        if (abby != null)
        {
            abby.caminando = false;   // ya no camina en su lugar
            yield return new WaitForSeconds(0.9f);
            Vector3 d = mira - a; d.y = 0f;
            if (d.sqrMagnitude > 0.01f) abby.caminarHacia = d.normalized;   // y luego voltea hacia él
        }
        alLlegar?.Invoke();
    }

    /// <summary>Los pedazos de la torre flotan muy despacio.</summary>
    public class Flotar : MonoBehaviour
    {
        [SerializeField] float fase, vel, alto;
        Vector3 p0;
        public void Iniciar(float f, float v, float a) { fase = f; vel = v; alto = a; }
        void Start() => p0 = transform.position;
        void Update()
        {
            transform.position = p0 + Vector3.up * Mathf.Sin(Time.time * vel * 0.4f + fase) * alto;
            transform.Rotate(0f, vel * 2f * Time.deltaTime, vel * Time.deltaTime, Space.World);
        }
    }

    IEnumerator Decir(string q, string s0, float dur)
    {
        quien = q; texto = s0; textoT = 0f;
        for (float t = 0f; t < dur; t += Time.deltaTime) { textoT += Time.deltaTime; yield return null; }
        texto = null;
        yield return new WaitForSeconds(0.25f);
    }

    IEnumerator Recuerdo(string imagen, string q, string s0, float dur)
    {
        recuerdo = imagen != null ? Resources.Load<Texture2D>(imagen) : null;
        quien = q; texto = s0; textoT = 0f;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            recuerdoA = Mathf.Clamp01(Mathf.Min(t / 0.9f, (dur - t) / 0.9f));
            textoT += Time.deltaTime;
            yield return null;
        }
        recuerdoA = 0f; recuerdo = null; texto = null;
    }

    /// <summary>Se va el frío: la sala entra en la luz de la puerta, el colgante brilla y sube ceniza dorada.</summary>
    IEnumerator LuzCalida(GameObject colgante)
    {
        var frias = GameObject.FindObjectsByType<Light>();
        var sol = RenderSettings.sun;
        Color s0 = sol != null ? sol.color : Color.white;
        float i0 = sol != null ? sol.intensity : 1f;
        Color a0 = RenderSettings.ambientSkyColor, a1 = RenderSettings.ambientEquatorColor, f0 = RenderSettings.fogColor;
        var luz = colgante.GetComponentInChildren<Light>();
        for (float t = 0f; t < 24f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 24f);
            if (sol != null) { sol.color = Color.Lerp(s0, new Color(1f, 0.72f, 0.46f), k); sol.intensity = Mathf.Lerp(i0, 1.25f, k); }
            RenderSettings.ambientSkyColor = Color.Lerp(a0, new Color(0.42f, 0.33f, 0.3f), k);
            RenderSettings.ambientEquatorColor = Color.Lerp(a1, new Color(0.42f, 0.29f, 0.24f), k);
            RenderSettings.fogColor = Color.Lerp(f0, new Color(0.42f, 0.27f, 0.2f), k);
            if (luz != null) { luz.color = Color.Lerp(new Color(0.75f, 0.45f, 1f), new Color(1f, 0.74f, 0.42f), k); luz.range = Mathf.Lerp(2.5f, 6f, k); luz.intensity = Mathf.Lerp(1.2f, 4f, k); }
            foreach (var l in frias) if (l != null && l.name == "LuzFria") l.color = Color.Lerp(new Color(0.6f, 0.56f, 0.78f), new Color(1f, 0.74f, 0.5f), k);
            if (Mathf.Repeat(t, 0.2f) < Time.deltaTime) CombatFx.Burst(colgante.transform.position + new Vector3(Random.Range(-7f, 7f), 0.2f, Random.Range(-5f, 8f)), Color.Lerp(new Color(0.55f, 0.3f, 0.8f), new Color(1f, 0.74f, 0.4f), k), 3, 0.5f, 0.1f, 3.4f, -0.25f);
            yield return null;
        }
    }

    IEnumerator AbrirPuerta()
    {
        var iz = Busca("HojaIzq"); var de = Busca("HojaDer"); var lz = Busca("LuzPuerta");
        var luz = lz != null ? lz.GetComponent<Light>() : null;
        float i0 = luz != null ? luz.intensity : 0f;
        Quaternion a = iz != null ? iz.localRotation : Quaternion.identity, b = de != null ? de.localRotation : Quaternion.identity;
        Sonido.Tocar(Sonido.Efecto.Temblor, 0.5f, 0.6f);
        for (float t = 0f; t < 3f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 3f);
            if (iz != null) iz.localRotation = a * Quaternion.Euler(0f, k * 26f, 0f);
            if (de != null) de.localRotation = b * Quaternion.Euler(0f, -k * 26f, 0f);
            if (luz != null) luz.intensity = Mathf.Lerp(i0, i0 * 3f, k);
            yield return null;
        }
    }

    static Transform Busca(string n)
    {
        foreach (var t in Object.FindObjectsByType<Transform>()) if (t.name == n) return t;
        return null;
    }

    /// <summary>Noche sin niebla, para la casa.</summary>
    void PonerCasa()
    {
        RenderSettings.ambientSkyColor = new Color(0.2f, 0.24f, 0.4f);
        RenderSettings.ambientEquatorColor = new Color(0.14f, 0.15f, 0.26f);
        RenderSettings.ambientGroundColor = new Color(0.08f, 0.09f, 0.16f);
        RenderSettings.fogColor = new Color(0.05f, 0.06f, 0.12f);
        RenderSettings.fogStartDistance = 30f; RenderSettings.fogEndDistance = 180f;
        if (RenderSettings.sun != null) { RenderSettings.sun.color = new Color(0.5f, 0.56f, 0.8f); RenderSettings.sun.intensity = 0.5f; }
        var cielo = FindAnyObjectByType<ExteriorSky>();
        if (cielo != null) { cielo.zenith = new Color(0.01f, 0.015f, 0.05f); cielo.horizon = new Color(0.06f, 0.07f, 0.16f); cielo.ground = new Color(0.04f, 0.05f, 0.1f); cielo.sunColor = new Color(0.5f, 0.55f, 0.8f); cielo.cloudCover = 0.2f; cielo.Apply(); }
        Vector3 cas = DioramaBuilder.FinalCasa;
        if (cam != null) { cam.fieldOfView = 34f; cam.transform.position = cas + new Vector3(0f, 2.6f, 6f); cam.transform.rotation = Quaternion.LookRotation(cas + new Vector3(0f, 4f, 34f) - cam.transform.position); }
    }

    void EncenderVentana()
    {
        var v = Busca("VentanaDante"); var a = Busca("VentanaAbby"); var l = Busca("LuzDante");
        if (v != null && a != null) v.GetComponent<Renderer>().sharedMaterial = a.GetComponent<Renderer>().sharedMaterial;
        if (l != null) l.GetComponent<Light>().enabled = true;
    }

    void OnGUI()
    {
        GUI.depth = -960;
        if (recuerdoA > 0f)
        {
            CuadroNarracion.Negro(recuerdoA * 0.94f);
            if (recuerdo != null)
            {
                float sw = Screen.width, sh = Screen.height, esc = Mathf.Max(sw / recuerdo.width, sh / recuerdo.height) * (1f + (1f - recuerdoA) * 0.03f);
                float w = recuerdo.width * esc, h = recuerdo.height * esc;
                GUI.color = new Color(0.86f, 0.82f, 0.8f, recuerdoA);
                GUI.DrawTexture(new Rect((sw - w) / 2f, (sh - h) / 2f, w, h), recuerdo, ScaleMode.StretchToFill);
                GUI.color = Color.white;
            }
        }
        CuadroNarracion.Negro(negro);
        if (creditos) DanteFinal.DibujarCreditos(creditosT, false);
        if (!string.IsNullOrEmpty(texto)) CuadroNarracion.Dialogo(quien, texto, CuadroNarracion.Letras(texto, textoT * 0.6f), 1f);
        if (!string.IsNullOrEmpty(tarjeta)) CuadroNarracion.Tarjeta(tarjeta, subtarjeta, tarjetaA);
    }
}
