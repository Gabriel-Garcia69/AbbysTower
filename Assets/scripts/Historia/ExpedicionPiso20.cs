using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Tutorial en el piso 20 (propuesta del guion): se juega con el hermano de Abis dentro de la expedición del capitán.
/// Enseña a moverse, girar la cámara, saltar, atacar, recoger minerales y usar el impulso; la expedición pelea al lado.
/// Al llegar al borde del abismo aparece una criatura enorme y la expedición es derrotada. Después empieza el juego de
/// Abis (piso 1).
/// </summary>
public class ExpedicionPiso20 : MonoBehaviour
{
    public string siguienteEscena = "Diorama";

    Transform player;
    PlayerHealth health;
    PlayerInputReader input;
    readonly List<Transform> miembros = new List<Transform>();
    Transform capitan;
    RoomEncounter entrenamiento, oleada;
    Transform coloso;

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
        health = p.GetComponent<PlayerHealth>();
        input = p.GetComponent<PlayerInputReader>();
        // se juega con el hermano de Abis (mismo cuerpo, otro color y un poco más alto, hasta que haya sprite propio)
        foreach (var sr in p.GetComponentsInChildren<SpriteRenderer>()) sr.color = new Color(0.72f, 0.82f, 1f);
        var fd = FindAnyObjectByType<FloorDirector>();
        if (fd != null) fd.heroName = Historia.Hermano.ToUpper();
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

    void Update()
    {
        var kb = Keyboard.current; var mouse = Mouse.current; var pad = Gamepad.current;
        if ((kb != null && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) || (mouse != null && mouse.leftButton.wasPressedThisFrame)
            || (pad != null && pad.buttonSouth.wasPressedThisFrame)) presionado = true;
        tTexto += Time.deltaTime;
        // en el tutorial no se muere: la expedición cura a quien se queda sin vida (hasta el final)
        if (!cinematica && health != null && !health.IsDead && health.Normalized < 0.35f) health.Heal(health.maxHealth * 0.4f);
    }

    // ------------------------------------------------------------------ guion del tutorial

    IEnumerator Guion()
    {
        yield return Fundir(0f, 1.5f);
        yield return Hablar(Historia.Capitan, "Piso 20. Ninguna expedición había bajado tanto.");
        yield return Hablar(Historia.Capitan, "Mantengan la formación. Lo que encontremos aquí abajo, lo enfrentamos juntos.");

        // moverse
        Vector3 inicio = player.position;
        yield return Pista("WASD / stick izquierdo: moverte", () => Vector3.Distance(Plano(player.position), Plano(inicio)) > 4f);
        // cámara
        float t0 = Time.time;
        yield return Pista("Q / E: girar la cámara   ·   clic derecho sostenido: asomarte",
            () => (Keyboard.current != null && (Keyboard.current.qKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame)) || Time.time - t0 > 8f);
        Mover(-36f);
        // saltar
        yield return Pista("Espacio: saltar. Sube al escalón", () => player.position.y > 0.7f && player.position.z > -30f);
        // atacar
        Mover(-10f);
        yield return Pista("Sigue a la expedición", () => entrenamiento != null && entrenamiento.Current != RoomEncounter.State.Esperando);
        yield return Pista("Clic izquierdo / J: atacar (encadena 3 golpes)", () => entrenamiento == null || entrenamiento.IsCleared);
        yield return Pista("Las criaturas sueltan minerales: pasa sobre ellos para recogerlos", null, 4f);
        // impulso
        Mover(16f);
        yield return Hablar(Historia.Capitan, "Una barrera de energía. Crúcenla con impulso.");
        yield return Pista("Shift: impulso. Atraviesa la barrera de energía", () => player.position.z > 5.5f);
        // la oleada, junto a la expedición
        Mover(66f);
        yield return Pista("Sigue a la expedición", () => oleada != null && oleada.Current != RoomEncounter.State.Esperando);
        StartCoroutine(ExpedicionPelea());
        yield return Pista("Usa el impulso justo antes de un golpe: esquiva perfecta (el siguiente ataque es crítico)", () => oleada == null || oleada.IsCleared);
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
        Vector3 yo = player.position;
        Vector3 a = coloso != null ? coloso.position : edge, arriba = new Vector3(a.x, -6f, a.z);

        // 1) tiembla la gruta: plano detrás de la expedición, mirando al borde
        CameraShake.Shake(1.2f, 0.6f);
        CombatFx.Burst(edge + Vector3.up, new Color(0.6f, 0.5f, 0.45f), 60, 8f, 0.3f, 1.2f);
        yield return Toma(cam, yo + new Vector3(0f, 2.4f, -9f), yo + new Vector3(0f, 2f, -7.5f), () => edge + Vector3.up * 5f, 1.8f,
            t => Subir(a, arriba, t * 0.3f));
        // 2) desde el borde, mirando hacia arriba: la criatura sube del abismo; la cámara sigue su cabeza
        yield return Toma(cam, edge + new Vector3(-10f, 5f, -9f), edge + new Vector3(-8f, 3.5f, -5f), () => { var c = Cabeza(); c.y = Mathf.Max(c.y, 1f); return c; }, 3.2f,
            t => { Subir(a, arriba, 0.3f + t * 0.7f); if (Mathf.Repeat(t * 3.2f, 1.1f) < Time.deltaTime) CameraShake.Shake(0.6f, 0.5f); });
        // 3) primer plano: los ojos
        Vector3 ojos = Cabeza();
        CameraShake.Shake(0.9f, 0.8f);
        yield return Toma(cam, ojos + new Vector3(3f, -1.5f, -22f), ojos + new Vector3(1.5f, -0.6f, -15f), Cabeza, 2.2f,
            t => Subir(a, arriba, 1f));
        // 4) plano abierto: la expedición, pequeña, frente a la criatura
        Vector3 medio = Vector3.Lerp(yo, Cabeza(), 0.6f);
        StartCoroutine(Toma(cam, yo + new Vector3(9f, 2f, -20f), yo + new Vector3(8f, 2.6f, -26f), () => medio, 6f, null));   // abajo, detrás de la expedición: ellos pequeños, la criatura enorme
        yield return Hablar(Historia.Capitan, "¿Qué es eso...?");
        // el golpe: destello, la expedición cae
        tomaId++;   // corta el plano abierto: la cámara se queda ahí para el golpe
        CombatFx.Burst(edge + Vector3.up * 2f, new Color(1f, 0.25f, 0.15f), 200, 18f, 0.5f, 1.4f);
        CameraShake.Shake(1.6f, 1f);
        negro = 0.7f;
        foreach (var m in miembros) StartCoroutine(Caer(m));
        if (capitan != null) StartCoroutine(Caer(capitan));
        if (health != null)
        {
            Vector3 src = player.position + Vector3.forward * 3f;
            health.ReceiveHit(new HitInfo { damage = Mathf.Max(0f, health.Health - 1f), knockback = 16f, hitStop = 0.1f, sourcePosition = src });
        }
        yield return Fundir(0.2f, 0.6f);
        yield return Hablar(Historia.Capitan, "¡Retirada! ¡Todos atrás!");
        yield return Fundir(1f, 2.5f);
        yield return Tarjeta("La expedición nunca regresó.", 3.5f);
        SceneManager.LoadScene(siguienteEscena);
    }

    int tomaId;

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

    /// <summary>Dónde están los ojos de la criatura (a 53% de su altura, al frente).</summary>
    Vector3 Cabeza()
    {
        if (coloso == null) return new Vector3(0f, 10f, 150f);
        var rs = coloso.GetComponentsInChildren<Renderer>();
        foreach (var r in rs) if (r.name == "Ojo") return new Vector3(coloso.position.x, r.bounds.center.y, r.bounds.center.z);
        return coloso.position + Vector3.up * 16f;
    }

    // ------------------------------------------------------------------ la expedición

    void Mover(float z)
    {
        // el capitán al frente; los demás en dos filas detrás
        if (capitan != null) StartCoroutine(Caminar(capitan, new Vector3(0f, 0f, z + 3f)));
        for (int i = 0; i < miembros.Count; i++)
            StartCoroutine(Caminar(miembros[i], new Vector3(-7.5f + (i % 3) * 7.5f + (i / 3) * 2f, 0f, z - (i / 3) * 3f)));
    }

    IEnumerator Caminar(Transform t, Vector3 destino)
    {
        Vector3 a = t.position;
        float dist = Vector3.Distance(Plano(a), Plano(destino)), dur = Mathf.Max(0.5f, dist / 4.5f);
        Vector3 dir = (destino - a); dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f) t.rotation = Quaternion.LookRotation(-dir.normalized);   // las figuras miran hacia -Z local
        for (float k = 0f; k < dur; k += Time.deltaTime)
        {
            var p = Vector3.Lerp(a, destino, k / dur);
            p.y = Suelo(p) + Mathf.Abs(Mathf.Sin(k * 9f)) * 0.08f;   // un saltito al caminar
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

    IEnumerator Caer(Transform t)
    {
        yield return new WaitForSeconds(Random.Range(0f, 0.25f));
        Quaternion a = t.rotation, b = a * Quaternion.Euler(-85f, 0f, Random.Range(-20f, 20f));
        Vector3 p0 = t.position, p1 = p0 + new Vector3(Random.Range(-2f, 2f), 0f, -Random.Range(3f, 6f));
        for (float k = 0f; k < 0.6f; k += Time.deltaTime)
        {
            t.rotation = Quaternion.Slerp(a, b, k / 0.6f);
            t.position = Vector3.Lerp(p0, p1, k / 0.6f) + Vector3.up * Mathf.Sin(k / 0.6f * Mathf.PI) * 1.2f;
            yield return null;
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
            var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 2.3f), alignment = TextAnchor.MiddleCenter, wordWrap = true };
            float w = Mathf.Min(sw * 0.6f, st.CalcSize(new GUIContent(pista)).x + u * 8f);
            var r = new Rect((sw - w) / 2f, u * 12f, w, u * 6f);
            CuadroNarracion.Marco(r, pistaAlpha * 0.95f);
            GUI.color = Color.white;
            st.normal.textColor = new Color(1f, 0.93f, 0.75f, pistaAlpha);
            GUI.Label(r, pista, st);
        }
        if (!string.IsNullOrEmpty(texto)) CuadroNarracion.Dialogo(quien, texto, CuadroNarracion.Letras(texto, tTexto), dialogoAlpha);
        CuadroNarracion.Negro(negro);
        if (!string.IsNullOrEmpty(tarjeta)) CuadroNarracion.Tarjeta(tarjeta, "", tarjetaAlpha);
    }
}
