using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Final de la pelea (GDD, Acto 3): al bajar la vida del jefe a cero se acaba todo: se van los enemigos, el jefe cae y la
/// pantalla se funde. El final sigue en su propio escenario (EscenaFinal). Aquí viven también el colgante y los créditos.
/// Lo agrega FloorDirector; espera el evento TowerEnemy.DanteDerrotado.
/// </summary>
public class DanteFinal : MonoBehaviour
{
    public string escenaAlTerminar = "Menu";

    float negro;
    bool corriendo;
    /// <summary>La escena final está corriendo (el HUD del piso se esconde).</summary>
    public static bool Activo { get; set; }

    void OnEnable() => TowerEnemy.DanteDerrotado += Empezar;
    void OnDisable() { TowerEnemy.DanteDerrotado -= Empezar; Activo = false; }

    void Empezar(TowerEnemy dante)
    {
        if (corriendo) return;
        corriendo = true; Activo = true;
        StartCoroutine(Final(dante));
    }

    IEnumerator Final(TowerEnemy dante)
    {
        // la pelea se acaba de golpe: Abby queda quieta y no queda ningún enemigo ni horda por salir
        var p = GameObject.FindWithTag("Player");
        if (p != null)
            foreach (var b in p.GetComponents<MonoBehaviour>())
                if (b is PlayerInputReader || b is AbbyAttack || b is AbbyBlock || b is AbbyDash) b.enabled = false;
        foreach (var r in FindObjectsByType<RoomEncounter>()) r.enabled = false;
        foreach (var e in TowerEnemy.All.ToArray())
            if (e != null && e != dante) { CombatFx.Burst(e.transform.position + Vector3.up, e.eyeColor, 20, 4f, 0.2f, 0.5f); Destroy(e.gameObject); }
        var h = p != null ? p.GetComponent<PlayerHealth>() : null;
        if (h != null) h.enabled = false;
        HitStop.Do(0.6f, 0.05f);
        CameraShake.Shake(0.5f, 0.8f);
        Musica.Poner(Musica.Pista.Ninguna);
        Sonido.Tocar(Sonido.Efecto.Impacto, 0.8f, 0.8f);

        // cae doblándose, se le desprende la sombra, y huye hacia la luz: Abby lo sigue
        var sprite = dante.GetComponentInChildren<TowerEnemy.DanteSprite>();
        if (sprite != null) sprite.enabled = false;
        var sr = dante.GetComponentInChildren<SpriteRenderer>();
        Transform cuerpo = sr != null ? sr.transform : null;
        Quaternion r0 = cuerpo != null ? cuerpo.rotation : Quaternion.identity;
        for (float t = 0f; t < 2.4f; t += Time.deltaTime)
        {
            if (cuerpo != null)
            {
                cuerpo.rotation = r0 * Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 40f, Mathf.SmoothStep(0f, 1f, t / 2.4f)));
                if (Mathf.Repeat(t, 0.16f) < Time.deltaTime) CombatFx.Burst(cuerpo.position + Vector3.up * 0.6f, new Color(0.3f, 0.12f, 0.42f), 14, 2.4f, 0.26f, 1.4f, -0.7f);
            }
            if (sr != null) sr.color = Color.Lerp(sr.color, new Color(0.1f, 0.06f, 0.16f, 0f), t / 2.4f * 0.06f);
            negro = Mathf.Clamp01((t - 1.2f) / 1.2f);
            yield return null;
        }
        negro = 1f;
        yield return new WaitForSeconds(0.6f);
        ReintentoJefe.Escena = null;
        Activo = true;
        SceneManager.LoadScene("Final");
    }

    public static GameObject CrearColgante(Vector3 pos)
    {
        var g = new GameObject("ColganteDeDante");
        g.transform.position = pos + Vector3.up * 0.06f;
        g.transform.localScale = Vector3.one * 1.1f;
        var lit = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        lit.SetColor("_BaseColor", new Color(0.95f, 0.74f, 0.36f)); lit.SetFloat("_Metallic", 0.8f); lit.SetFloat("_Smoothness", 0.7f);
        var gema = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        gema.SetColor("_BaseColor", new Color(0.5f, 0.2f, 0.08f)); gema.EnableKeyword("_EMISSION");
        gema.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        gema.SetColor("_EmissionColor", new Color(1f, 0.55f, 0.22f) * 0.45f);   // brillo suave (con más, el Bloom lo volvía una mancha)
        var disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Object.Destroy(disco.GetComponent<Collider>());
        disco.transform.SetParent(g.transform, false);
        disco.transform.localScale = new Vector3(0.28f, 0.02f, 0.28f);
        disco.GetComponent<Renderer>().sharedMaterial = lit;
        var piedra = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Object.Destroy(piedra.GetComponent<Collider>());
        piedra.transform.SetParent(g.transform, false);
        piedra.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        piedra.transform.localScale = new Vector3(0.12f, 0.05f, 0.12f);
        piedra.GetComponent<Renderer>().sharedMaterial = gema;
        for (int i = 0; i < 14; i++)   // la cadena
        {
            var e = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(e.GetComponent<Collider>());
            e.transform.SetParent(g.transform, false);
            float a = i / 13f * 3.6f;
            e.transform.localPosition = new Vector3(Mathf.Sin(a) * 0.35f, 0.01f, 0.18f + Mathf.Cos(a) * 0.25f);
            e.transform.localScale = new Vector3(0.035f, 0.02f, 0.06f);
            e.transform.localRotation = Quaternion.Euler(0f, a * 57f, 0f);
            e.GetComponent<Renderer>().sharedMaterial = lit;
        }
        var l = new GameObject("Brillo").AddComponent<Light>();
        l.transform.SetParent(g.transform, false);
        l.transform.localPosition = Vector3.up * 0.4f;
        l.type = LightType.Point; l.color = new Color(0.75f, 0.45f, 1f); l.range = 2.5f; l.intensity = 1.2f; l.shadows = LightShadows.None;
        CombatFx.Burst(pos + Vector3.up * 0.2f, new Color(0.8f, 0.6f, 1f), 30, 2f, 0.12f, 0.8f, -0.4f);
        // el de la ilustración, en pixel art, sobre la piedra (el modelo de arriba queda solo si faltara la imagen)
        var tex = Resources.Load<Texture2D>("Final/colgante");
        if (tex != null)
        {
            tex.filterMode = FilterMode.Point;
            foreach (var rn in g.GetComponentsInChildren<Renderer>()) rn.enabled = false;
            var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(q.GetComponent<Collider>());
            q.name = "ColganteArte";
            q.transform.SetParent(g.transform, false);
            q.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = Vector3.one * 0.38f;
            var mq = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mq.SetTexture("_BaseMap", tex);
            mq.SetFloat("_AlphaClip", 1f); mq.SetFloat("_Cutoff", 0.5f); mq.EnableKeyword("_ALPHATEST_ON");
            var qr = q.GetComponent<Renderer>();
            qr.sharedMaterial = mq;
            qr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        return g;
    }

    public const float DuracionCreditos = 30f;

    // (qué hizo, quién); sin "qué hizo" es una línea sola
    public static readonly string[][] Creditos =
    {
        new[] { "Dirección y programación", "Rodrigo Herrera" },
        new[] { "Programación y arte", "Gabriel García" },
        new[] { "Música y sonido", "Francisco Uc" },
        new[] { "Experiencia de usuario", "Ricardo Cetina" },
        new[] { "Pruebas y experiencia de usuario", "Karla Damaris" },
        new[] { "Un proyecto de", "Estudio 02" },
        new[] { "Integradora · IDYGS103", "Universidad Tecnológica de Cancún" },
    };

    static void Linea(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = Color.white; }

    /// <summary>
    /// Los créditos subiendo a la izquierda, sobre un velo que se difumina; lo de atrás sigue a la vista. El logo arriba y una
    /// dedicatoria al final. t = segundos desde el principio. Los usa también el menú principal.
    /// </summary>
    public static void DibujarCreditos(float t, bool fondoNegro = true)
    {
        float sw = Screen.width, sh = Screen.height, u = sh / 100f;
        if (fondoNegro) { GUI.color = Color.black; GUI.DrawTexture(new Rect(0, 0, sw, sh), Texture2D.whiteTexture); }
        // velo: de casi negro a transparente
        GUI.color = new Color(0.015f, 0.01f, 0.035f, 0.88f);
        GUI.DrawTexture(new Rect(0f, 0f, sw * 0.56f, sh), Degradado.Horizontal);
        GUI.color = Color.white;
        float x = sw * 0.07f, w = sw * 0.4f;
        float largo = u * (34f + Creditos.Length * 17f + 34f);
        float y = sh - Mathf.Repeat(t, DuracionCreditos) / DuracionCreditos * (largo + sh * 0.35f);
        Color Oro(float a) => new Color(0.95f, 0.78f, 0.45f, a);
        float Al(float yy) => Mathf.Clamp01(Mathf.Min((yy + u * 3f) / (sh * 0.16f), (sh - yy - u * 3f) / (sh * 0.16f)));

        // el logo
        var logo = FuentePixel.Logo(new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = false }, u * 7.4f);
        float a0 = Al(y);
        logo.normal.textColor = new Color(0f, 0f, 0f, a0 * 0.8f); GUI.Label(new Rect(x + 3f, y + 3f, sw, u * 10f), Historia.Titulo, logo);
        logo.normal.textColor = new Color(1f, 0.92f, 0.75f, a0); GUI.Label(new Rect(x, y, sw, u * 10f), Historia.Titulo, logo);
        var lema = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 2.8f), fontStyle = FontStyle.Italic };
        lema.normal.textColor = new Color(0.85f, 0.78f, 1f, a0);
        GUI.Label(new Rect(x, y + u * 10f, w, u * 4.5f), Historia.Subtitulo, lema);
        Linea(new Rect(x, y + u * 16f, w, Mathf.Max(1f, u * 0.2f)), Oro(a0 * 0.8f));
        y += u * 34f;

        var cargo = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 2.5f), wordWrap = false };
        var nombre = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 4.9f), fontStyle = FontStyle.Bold, wordWrap = false };
        foreach (var c in Creditos)
        {
            float a = Al(y);
            Linea(new Rect(x, y + u * 1.2f, u * 2.6f, Mathf.Max(1f, u * 0.25f)), Oro(a));
            cargo.normal.textColor = Oro(a);
            GUI.Label(new Rect(x + u * 3.6f, y - u * 0.8f, w, u * 4f), c[0].ToUpperInvariant(), cargo);
            nombre.normal.textColor = new Color(0f, 0f, 0f, a * 0.7f); GUI.Label(new Rect(x + 2f, y + u * 3.2f + 2f, w, u * 7f), c[1], nombre);
            nombre.normal.textColor = new Color(1f, 0.97f, 0.92f, a); GUI.Label(new Rect(x, y + u * 3.2f, w, u * 7f), c[1], nombre);
            y += u * 17f;
        }
        y += u * 12f;
        var ded = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 3.4f), fontStyle = FontStyle.Italic, wordWrap = true };
        float a1 = Al(y);
        ded.normal.textColor = new Color(0.92f, 0.86f, 1f, a1);
        GUI.Label(new Rect(x, y, w, u * 12f), "Para los que todavía dejan una luz prendida.", ded);
        ded.fontStyle = FontStyle.Bold; ded.normal.textColor = Oro(Al(y + u * 12f));
        GUI.Label(new Rect(x, y + u * 12f, w, u * 6f), "Gracias por jugar", ded);
        GUI.color = Color.white;
    }

    void OnGUI()
    {
        if (!corriendo) return;
        GUI.depth = -960;
        CuadroNarracion.Negro(negro);
    }
}
