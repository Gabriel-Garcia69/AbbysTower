using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Tutorial en el piso 20 (propuesta del guion): se juega con el hermano de Abby dentro de la expedición del capitán.
/// Enseña a moverse, girar la cámara, saltar, atacar, recoger minerales y usar el impulso; la expedición pelea al lado.
/// Al llegar al borde del abismo aparece una criatura enorme y la expedición es derrotada. Después empieza el juego de
/// Abby (piso 1).
/// </summary>
public class ExpedicionPiso20 : MonoBehaviour
{
    public string siguienteEscena = "Intro";   // Menú → "Despertar" → Expedicion → Intro (cinemática) → Exterior → Diorama...

    Transform player;
    PlayerHealth health;
    PlayerInputReader input;
    readonly List<Transform> miembros = new List<Transform>();
    Transform capitan;
    RoomEncounter entrenamiento, oleada;
    Transform coloso;

    Light luzDante, luzGrupo;
    string pista;                 // indicación de controles (arriba)
    float pistaAlpha;
    string quien, texto;          // diálogo (abajo)
    float tTexto, dialogoAlpha, negro = 1f;
    bool presionado, cinematica;
    string tarjeta;
    float tarjetaAlpha;

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p == null) { enabled = false; return; }
        player = p.transform;
        PersonaSprite.Aplicar();   // la expedición y el coloso con su pixel art, si ya lo tienen
        health = p.GetComponent<PlayerHealth>();
        input = p.GetComponent<PlayerInputReader>();
        // se juega con Dante, con sus propios sprites
        var anim = p.GetComponentInChildren<AbbySpriteAnimator>(true);
        var conjunto = ConjuntoDante();
        if (anim != null) anim.Aplicar(conjunto);
        var fd = FindAnyObjectByType<FloorDirector>();
        if (fd != null)
        {
            fd.heroName = Historia.Hermano.ToUpper();
            fd.PonerRetrato(conjunto.retrato != null ? conjunto.retrato : anim != null ? anim.Retrato : null);
        }
        // SpriteLit3D usa la normal hacia ARRIBA: los sprites se alumbran por lo que tienen encima, así que las luces van arriba y hacia la
        // cámara (no pegadas al cuerpo: ahí hacen un punto blanco en la cabeza). Una para Dante y una compartida para la expedición.
        luzDante = NuevaLuz("LuzDante", 1.3f, 10f);   // la cueva es oscura: Dante lleva su propia luz
        luzGrupo = NuevaLuz("LuzExpedicion", 1.2f, 13f);
        var exp = GameObject.Find("Expedicion");
        if (exp != null)
            foreach (Transform t in exp.transform) { if (t.name == "Capitan") capitan = t; else miembros.Add(t); }
        foreach (var r in FindObjectsByType<RoomEncounter>())
        {
            if (r.name.Contains("Entrenamiento")) entrenamiento = r;
            if (r.name.Contains("oleada")) oleada = r;
        }
        var c = GameObject.Find("Coloso");
        if (c != null) coloso = c.transform;
        StartCoroutine(Guion());
    }

    static Light NuevaLuz(string nombre, float intensidad, float rango)
    {
        var l = new GameObject(nombre).AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(0.72f, 0.77f, 1f); l.range = rango; l.intensity = intensidad; l.shadows = LightShadows.None;
        return l;
    }

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null) return;
        Vector3 f = cam.transform.forward; f.y = 0f;
        if (f.sqrMagnitude < 0.0001f) return;
        f.Normalize();
        if (luzDante != null && player != null) luzDante.transform.position = player.position + Vector3.up * 2.5f - f * 2.5f;   // arriba y hacia la cámara
        if (luzGrupo != null)
        {
            Vector3 c = Vector3.zero; int n = 0;
            if (capitan != null) { c += capitan.position; n++; }
            foreach (var m in miembros) if (m != null) { c += m.position; n++; }
            if (n > 0) luzGrupo.transform.position = c / n + Vector3.up * 3.5f - f * 2.5f;
        }
    }

    void Update()
    {
        var kb = Keyboard.current; var mouse = Mouse.current; var pad = Gamepad.current;
        if ((kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) || (mouse != null && mouse.leftButton.wasPressedThisFrame)
            || (pad != null && pad.buttonSouth.wasPressedThisFrame)) presionado = true;
        tTexto += Time.deltaTime;
        // en el tutorial no se muere (la vida no baja de 1 hasta el final); la vida solo se cura en la fuente (GDD)
        if (health != null) health.noMorir = !cinematica;
    }

    // ------------------------------------------------------------------ guion del tutorial

    IEnumerator Guion()
    {
        yield return Fundir(0f, 1.5f);
        yield return Hablar(Historia.Capitan, "Piso 20. Ninguna expedición había bajado tanto.");
        yield return Hablar(Historia.Capitan, "Mantengan la formación. Lo que encontremos aquí abajo, lo enfrentamos juntos.");
        yield return Hablar(Historia.Hermano, "Todavía traigo tu colgante, Abby. Te lo voy a devolver.");

        // moverse
        Vector3 inicio = player.position;
        yield return Pista("WASD / stick izquierdo: moverte", () => Vector3.Distance(Plano(player.position), Plano(inicio)) > 4f);
        Mover(-36f);
        // saltar
        yield return Pista("Espacio: saltar. Sube al escalón", () => player.position.y > 0.7f && player.position.z > -30f);
        // atacar
        Mover(-10f);
        yield return Pista("Sigue a la expedición", () => entrenamiento != null && entrenamiento.Current != RoomEncounter.State.Esperando);
        yield return Pista("Clic izquierdo: atacar   ·   tres clics seguidos: combo (el tercer golpe pega más)", () => entrenamiento == null || entrenamiento.IsCleared);
        // la fuente de curación (E): la única forma de recuperar vida
        Mover(38f);
        yield return Pista("Las fuentes de curación restauran toda tu vida. Acércate y presiona E (cada fuente sirve una vez)",
            () => health == null || health.Normalized >= 0.999f && FuenteUsadaOLlena(), 3f);
        // la oleada, junto a la expedición
        Mover(66f);
        yield return Pista("Sigue a la expedición", () => oleada != null && oleada.Current != RoomEncounter.State.Esperando);
        StartCoroutine(ExpedicionPelea());
        yield return Pista("Clic derecho sostenido: bloquear. Recibes solo el 8 % del daño, pero gasta stamina. Las sombras rojas del suelo avisan cada ataque",
            () => oleada == null || oleada.IsCleared);
        yield return Hablar(Historia.Capitan, "Bien hecho. Sigamos hasta el borde.");
        Mover(112f);
        yield return Pista("Acércate al borde del abismo", () => player.position.z > 116f);
        yield return Derrota();
    }

    IEnumerator Derrota()
    {
        cinematica = true;
        pista = null;
        if (input != null) input.enabled = false;
        var cam = Camera.main;
        foreach (var b in FindObjectsByType<MouseOrbitCamera>()) b.enabled = false;
        foreach (var b in FindObjectsByType<CameraRig>()) b.enabled = false;
        Vector3 edge = new Vector3(0f, 0f, 128f);
        // Dante se queda antes de donde se ponen las cámaras bajas: si no, le pasan pegadas a la cara
        if (player.position.z > 116f)
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            player.position = new Vector3(player.position.x, player.position.y, 116f);
            if (cc != null) cc.enabled = true;
        }
        Vector3 yo = player.position;
        Vector3 a = coloso != null ? coloso.position : edge, arriba = new Vector3(a.x, -6f, a.z);

        var cuerpo = coloso != null ? coloso.GetComponent<PersonaSprite>() : null;
        if (coloso != null) foreach (var l in coloso.GetComponentsInChildren<Light>(true)) l.enabled = true;   // sus luces, solo ahora
        var rugido = CuadrosColoso("Rugido"); var zarpazo = CuadrosColoso("Zarpazo"); var golpe = CuadrosColoso("Golpe");
        // lente abierto y cámara casi a ras del suelo: la criatura se ve entera y enorme (la cámara de juego usa 24°, que la corta en los hombros)
        Vector3 centro = new Vector3(a.x, 13f, a.z);
        if (cam != null) cam.fieldOfView = 52f;

        // 1) tiembla la gruta: la expedición, lejana y de espaldas, mirando al borde; cae polvo del techo
        Sonido.Tocar(Sonido.Efecto.Temblor, 1f);
        CameraShake.Shake(1.2f, 0.6f);
        CombatFx.Burst(edge + Vector3.up, new Color(0.6f, 0.5f, 0.45f), 60, 8f, 0.3f, 1.2f);
        for (int k = 0; k < 8; k++) CombatFx.Burst(yo + new Vector3((k - 3.5f) * 5f, 18f, 8f + k * 4f), new Color(0.5f, 0.46f, 0.44f), 14, 1.5f, 0.28f, 1.8f, 1.1f);
        yield return Toma(cam, yo + new Vector3(0f, 3.5f, -24f), yo + new Vector3(0f, 3.2f, -21f), () => edge + Vector3.up * 5f, 1.8f,
            t => Subir(a, arriba, t * 0.3f));

        // a partir de aquí solo se ve a la criatura: el jugador no debe saber qué le pasó a Dante ni a la expedición
        foreach (var r in player.GetComponentsInChildren<Renderer>(true)) r.forceRenderingOff = true;
        foreach (var m in miembros) if (m != null) m.gameObject.SetActive(false);
        if (capitan != null) capitan.gameObject.SetActive(false);

        // 2) contrapicado desde el suelo: la cámara se queda quieta y la criatura se alza del abismo hasta llenar el cuadro
        Sonido.Tocar(Sonido.Efecto.Temblor, 1f, 0.8f);
        yield return Toma(cam, edge + new Vector3(-7f, 0.9f, -8f), edge + new Vector3(-5f, 0.8f, -6f), () => centro, 3.6f,
            t => { Subir(a, arriba, 0.3f + t * 0.7f); if (Mathf.Repeat(t * 3.2f, 1.1f) < Time.deltaTime) { CameraShake.Shake(0.6f, 0.5f); Sonido.Tocar(Sonido.Efecto.Alerta, 0.9f, 0.8f); } });
        // 3) el rugido: de frente y desde abajo, abre los brazos; la cámara retrocede por el golpe de aire
        StartCoroutine(Cuadros(cuerpo, rugido, 0, rugido.Length, 2.8f));
        Sonido.Tocar(Sonido.Efecto.Rugido, 1f);
        CameraShake.Shake(1.1f, 1.8f);
        CombatFx.Burst(edge + Vector3.up * 1.5f, new Color(0.55f, 0.48f, 0.5f), 120, 14f, 0.45f, 1.6f, 0.4f);
        yield return Toma(cam, edge + new Vector3(3f, 1f, -5f), edge + new Vector3(1.5f, 0.9f, -8f), () => centro, 2.8f, t =>
        {
            if (cam != null) cam.fieldOfView = 52f + Mathf.Sin(Mathf.PI * Mathf.Clamp01(t * 1.6f)) * 8f;
            if (Mathf.Repeat(t * 2.8f, 0.35f) < Time.deltaTime) CameraShake.Shake(0.45f, 0.3f);
        });
        if (cam != null) cam.fieldOfView = 50f;
        // 4) plano abierto de la criatura: el capitán se oye, pero no se le ve
        StartCoroutine(Toma(cam, edge + new Vector3(-1f, 1f, -11f), edge + new Vector3(-0.5f, 1.1f, -14f), () => centro, 14f, null));
        yield return Hablar(Historia.Capitan, "¿Qué es eso...?");
        // 5) primer ataque: el zarpazo
        if (zarpazo.Length > 0)
        {
            yield return Cuadros(cuerpo, zarpazo, 0, 3, 0.8f);
            Sonido.Tocar(Sonido.Efecto.Impulso, 1f, 0.55f);
            CameraShake.Shake(0.7f, 0.5f);
            CombatFx.Burst(new Vector3(-8f, 0.5f, edge.z - 2f), new Color(0.5f, 0.46f, 0.44f), 70, 14f, 0.5f, 1.2f, 0.7f);
            yield return Cuadros(cuerpo, zarpazo, 3, 3, 0.7f);
        }
        // 6) segundo ataque: alza los puños despacio y los estrella contra el suelo
        Sonido.Tocar(Sonido.Efecto.Alerta, 1f, 0.7f);
        if (golpe.Length > 0)
        {
            yield return Cuadros(cuerpo, golpe, 0, 4, 1.3f);
            yield return Cuadros(cuerpo, golpe, 4, 1, 0.1f);
        }
        // el impacto corta a negro: lo que pasó después no se ve
        Sonido.Tocar(Sonido.Efecto.Impacto, 1f);
        Sonido.Tocar(Sonido.Efecto.Temblor, 1f, 0.7f);
        CameraShake.Shake(1.8f, 1.2f);
        negro = 1f;
        tomaId++;
        yield return new WaitForSeconds(1.6f);
        yield return Hablar(Historia.Capitan, "¡Retirada! ¡Todos atrás!");
        yield return Tarjeta("La expedición nunca regresó.", 3.5f);
        Cargador.Ir(siguienteEscena);
    }

    int tomaId;

    static Sprite[] CuadrosColoso(string anim)
    {
        var fr = Resources.LoadAll<Sprite>("ColosoAnim/" + anim);
        System.Array.Sort(fr, (x, y) => string.CompareOrdinal(x.name, y.name));
        return fr;
    }

    /// <summary>Pasa cuadros del coloso, repartidos en dur segundos; el último se queda puesto.</summary>
    IEnumerator Cuadros(PersonaSprite cuerpo, Sprite[] fr, int desde, int cuantos, float dur)
    {
        if (cuerpo == null || fr.Length == 0) yield break;
        for (float t = 0f; t < dur; t += Time.deltaTime)
        {
            int k = desde + Mathf.Min(cuantos - 1, Mathf.FloorToInt(t / dur * cuantos));
            cuerpo.Forzado = fr[Mathf.Clamp(k, 0, fr.Length - 1)];
            yield return null;
        }
        cuerpo.Forzado = fr[Mathf.Clamp(desde + cuantos - 1, 0, fr.Length - 1)];
    }

    /// <summary>Un plano de cámara: corte a p0, se acerca despacio a p1 mirando a 'mira' (que puede moverse).</summary>
    IEnumerator Toma(Camera cam, Vector3 p0, Vector3 p1, System.Func<Vector3> mira, float dur, System.Action<float> cada)
    {
        if (cam == null) yield break;
        int id = ++tomaId;
        for (float t = 0f; t < dur && id == tomaId; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            cada?.Invoke(t / dur);
            cam.transform.position = Vector3.Lerp(p0, p1, k);
            cam.transform.rotation = Quaternion.LookRotation(mira() - cam.transform.position);
            yield return null;
        }
    }

    void Subir(Vector3 a, Vector3 b, float t)
    {
        if (coloso == null) return;
        var p = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t)));
        var prop = coloso.GetComponent<CriaturaProp>();
        if (prop != null) prop.Colocar(p); else coloso.position = p;
    }

    /// <summary>Dónde está la cabeza de la criatura.</summary>
    Vector3 Cabeza()
    {
        if (coloso == null) return new Vector3(0f, 10f, 150f);
        return coloso.position + new Vector3(0f, 23f, -1f);   // la cabeza del sprite (mide unos 32 m, y la cabeza va arriba)
    }

    /// <summary>Ya usó la fuente del campamento, o no le falta vida (no hay nada que enseñar).</summary>
    bool FuenteUsadaOLlena()
    {
        var f = FindAnyObjectByType<HealingShrine>();
        return f == null || f.Usada || health == null || health.Normalized >= 0.999f;
    }

    // ------------------------------------------------------------------ la expedición

    void Mover(float z)
    {
        // la expedición se reparte a los dos lados del sendero (entre él y los braseros), nunca encima de él, a 1,2 m o más entre sí y a 2 m
        // o más de Dante; el capitán toma el puesto de más adelante
        var rng = new System.Random(Mathf.RoundToInt(z * 10f));
        Vector2? yo = player != null ? new Vector2(player.position.x, player.position.z) : (Vector2?)null;
        var n = miembros.Count + (capitan != null ? 1 : 0);
        var puestos = DistribucionExpedicion.Puestos(DistribucionExpedicion.Franjas(DioramaBuilder.SenderoX, z, 5f), n, yo, DioramaBuilder.SenderoX, rng);
        int k = 0;
        if (capitan != null) { if (k < puestos.Length) StartCoroutine(Caminar(capitan, new Vector3(puestos[k].x, 0f, puestos[k].y))); k++; }
        for (int i = 0; i < miembros.Count && k < puestos.Length; i++, k++)
            StartCoroutine(Caminar(miembros[i], new Vector3(puestos[k].x, 0f, puestos[k].y)));
    }

    IEnumerator Caminar(Transform t, Vector3 destino)
    {
        Vector3 a = t.position;
        bool billboard = t.GetComponentInChildren<FiguraBillboard>() != null;
        float dist = Vector3.Distance(Plano(a), Plano(destino)), dur = Mathf.Max(0.5f, dist / 4.5f);
        Vector3 dir = (destino - a); dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f) t.rotation = Quaternion.LookRotation(-dir.normalized);   // las figuras miran hacia -Z local
        for (float k = 0f; k < dur; k += Time.deltaTime)
        {
            var p = Vector3.Lerp(a, destino, k / dur);
            p.y = Suelo(p) + (billboard ? 0f : Mathf.Abs(Mathf.Sin(k * 9f)) * 0.08f);   // un saltito al caminar (los sprites ya hacen su balanceo)
            t.position = p;
            yield return null;
        }
        destino.y = Suelo(destino);
        t.position = destino;
        t.rotation = Quaternion.Euler(0f, 180f, 0f);   // de frente al abismo (norte)
    }

    static float Suelo(Vector3 p) => Physics.Raycast(p + Vector3.up * 5f, Vector3.down, out var h, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) ? h.point.y : p.y;

    /// <summary>Mientras dura la oleada, la expedición también pega (con su energía) a las criaturas cercanas.</summary>
    IEnumerator ExpedicionPelea()
    {
        while (oleada != null && !oleada.IsCleared)
        {
            foreach (var m in miembros)
            {
                TowerEnemy cerca = null; float best = 9f;
                foreach (var e in TowerEnemy.All)
                {
                    float d = Vector3.Distance(e.transform.position, m.position);
                    if (d < best && e.Health > 0f) { best = d; cerca = e; }
                }
                if (cerca == null || Random.value > 0.35f) continue;
                CombatFx.Burst(cerca.transform.position + Vector3.up, new Color(0.4f, 0.85f, 1f), 14, 5f, 0.12f, 0.35f);
                cerca.ReceiveHit(new HitInfo { damage = 7f, knockback = 3f, hitStop = 0.02f, sourcePosition = m.position });
            }
            yield return new WaitForSeconds(0.7f);
        }
    }

    static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);

    // ------------------------------------------------------------------ textos

    IEnumerator Pista(string s, System.Func<bool> listo, float minimo = 0f)
    {
        pista = s;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime) { pistaAlpha = t / 0.3f; yield return null; }
        pistaAlpha = 1f;
        float t0 = Time.time;
        while ((listo != null && !listo()) || Time.time - t0 < minimo) yield return null;
        for (float t = 0f; t < 0.3f; t += Time.deltaTime) { pistaAlpha = 1f - t / 0.3f; yield return null; }
        pista = null;
    }

    IEnumerator Hablar(string q, string s)
    {
        quien = q; texto = s; tTexto = 0f; presionado = false;
        bool bloquea = !cinematica && input != null;
        if (bloquea) input.enabled = false;   // mientras habla el capitán, quieto
        for (float t = 0f; t < 0.2f; t += Time.deltaTime) { dialogoAlpha = t / 0.2f; yield return null; }
        dialogoAlpha = 1f;
        while (true)
        {
            bool completo = CuadroNarracion.Letras(texto, tTexto) >= texto.Length;
            if (presionado) { presionado = false; if (!completo) tTexto = 999f; else break; }
            if (completo && tTexto - texto.Length / 45f > 2.5f) break;
            yield return null;
        }
        for (float t = 0f; t < 0.2f; t += Time.deltaTime) { dialogoAlpha = 1f - t / 0.2f; yield return null; }
        texto = null;
        if (bloquea) input.enabled = true;
    }

    IEnumerator Fundir(float hasta, float dur)
    {
        float desde = negro;
        for (float t = 0f; t < dur; t += Time.deltaTime) { negro = Mathf.Lerp(desde, hasta, t / dur); yield return null; }
        negro = hasta;
    }

    IEnumerator Tarjeta(string s, float dur)
    {
        tarjeta = s;
        for (float t = 0f; t < dur; t += Time.deltaTime) { tarjetaAlpha = Mathf.Clamp01(Mathf.Min(t / 0.8f, (dur - t) / 0.8f)); yield return null; }
        tarjeta = null;
    }

    void OnGUI()
    {
        GUI.depth = -950;
        float sw = Screen.width, u = Screen.height / 100f;
        if (!string.IsNullOrEmpty(pista) && pistaAlpha > 0f)
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 2.3f), alignment = TextAnchor.MiddleCenter, wordWrap = true };
            float w = Mathf.Min(sw * 0.6f, st.CalcSize(new GUIContent(pista)).x + u * 8f);
            var r = new Rect((sw - w) / 2f, u * 16f, w, u * 6f);   // debajo del marco del HUD (que llega a ~14u)
            CuadroNarracion.Marco(r, pistaAlpha * 0.95f);
            GUI.color = Color.white;
            st.normal.textColor = new Color(1f, 0.93f, 0.75f, pistaAlpha);
            GUI.Label(r, pista, st);
        }
        CuadroNarracion.Negro(negro);
        if (!string.IsNullOrEmpty(texto)) CuadroNarracion.Dialogo(quien, texto, CuadroNarracion.Letras(texto, tTexto), dialogoAlpha);
        if (!string.IsNullOrEmpty(tarjeta)) CuadroNarracion.Tarjeta(tarjeta, "", tarjetaAlpha);
    }

    static readonly string[] DirsDante = { "east", "north-east", "north", "north-west", "west", "south-west", "south", "south-east" };

    /// <summary>Dante humano (Resources/DanteHumano): de pie en 8 direcciones; caminar y atacar en 5, el resto se voltea.</summary>
    static ConjuntoSprites ConjuntoDante()
    {
        var c = ScriptableObject.CreateInstance<ConjuntoSprites>();
        c.nombre = Historia.Hermano;
        c.soloPropios = true;
        for (int i = 0; i < 8; i++)
        {
            c.idle[i] = Resources.Load<Sprite>("DanteHumano/Quieto/" + DirsDante[i]);
            c.walk[i] = CuadrosDante("Caminar", DirsDante[i]);
            c.slash[i] = CuadrosDante("Atacar", DirsDante[i]);
        }
        c.retrato = c.idle[6];
        return c;
    }

    static AbbySpriteAnimator.Frames CuadrosDante(string anim, string dir)
    {
        var fr = Resources.LoadAll<Sprite>($"DanteHumano/{anim}/{dir}");
        System.Array.Sort(fr, (a, b) => string.CompareOrdinal(a.name, b.name));
        return new AbbySpriteAnimator.Frames { frames = fr };
    }
}
