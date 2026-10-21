using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Final de la demo (GDD, Acto 3): al bajar la vida del jefe a cero la pelea se interrumpe y el jefe cae. Queda en el
/// suelo una pieza de Dante (su colgante), el jefe dice "Abby…" en texto y corte a negro. Termina en cliffhanger.
/// Lo agrega FloorDirector; espera el evento TowerEnemy.DanteDerrotado.
/// </summary>
public class DanteFinal : MonoBehaviour
{
    public string escenaAlTerminar = "Menu";

    float negro, textoT, tarjetaA;
    string quien, texto, tarjeta, subtarjeta;
    bool corriendo;
    /// <summary>La escena final está corriendo (el HUD del piso se esconde).</summary>
    public static bool Activo { get; private set; }

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
        // la pelea se interrumpe: Abby queda quieta, los demás enemigos se deshacen
        var p = GameObject.FindWithTag("Player");
        if (p != null)
            foreach (var b in p.GetComponents<MonoBehaviour>())
                if (b is PlayerInputReader || b is AbbyAttack || b is AbbyBlock || b is AbbyDash) b.enabled = false;
        foreach (var e in TowerEnemy.All.ToArray())
            if (e != null && e != dante) { CombatFx.Burst(e.transform.position + Vector3.up, e.eyeColor, 20, 4f, 0.2f, 0.5f); Destroy(e.gameObject); }
        HitStop.Do(0.6f, 0.05f);
        CameraShake.Shake(0.5f, 0.8f);

        var cam = Camera.main;
        foreach (var b in FindObjectsByType<MouseOrbitCamera>()) b.enabled = false;
        foreach (var b in FindObjectsByType<CameraRig>()) b.enabled = false;
        Vector3 d = dante.transform.position;
        Vector3 desde = p != null ? p.transform.position : d + Vector3.back * 4f;
        Vector3 lado = Vector3.Cross(Vector3.up, (d - desde).normalized);

        // cae: se dobla y queda tendido; la sombra se le va desprendiendo
        var sprite = dante.GetComponentInChildren<TowerEnemy.DanteSprite>();
        if (sprite != null) sprite.enabled = false;
        var sr = dante.GetComponentInChildren<SpriteRenderer>();
        Transform cuerpo = sr != null ? sr.transform : null;
        Quaternion r0 = cuerpo != null ? cuerpo.rotation : Quaternion.identity;
        for (float t = 0f; t < 2.2f; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / 2.2f);
            if (cuerpo != null)
            {
                cuerpo.rotation = r0 * Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, 82f, Mathf.SmoothStep(0f, 1f, (t - 0.6f) / 1.4f)));
                if (Mathf.Repeat(t, 0.25f) < Time.deltaTime) CombatFx.Burst(cuerpo.position + Vector3.up * 0.6f, new Color(0.25f, 0.1f, 0.35f), 10, 2f, 0.25f, 1.2f, -0.6f);
            }
            if (sr != null) sr.color = Color.Lerp(new Color(0.16f, 0.09f, 0.24f), new Color(0.32f, 0.26f, 0.4f), k);   // se le ve algo debajo de la sombra
            if (cam != null) Encuadre(cam, d + Vector3.up * 7f - (d - desde).normalized * 6f + lado * 1.5f, d + Vector3.up * 0.6f, 1.5f);   // alta, del lado de Abby: no choca con almenas ni columnas
            yield return null;
        }

        // la pieza de Dante: su colgante, en el suelo junto a él
        Vector3 pos = Vector3.Lerp(d, desde, 0.4f);   // entre Dante y Abby: siempre suelo libre (no junto a las almenas)
        pos.y = d.y;
        Colgante(pos);
        for (float t = 0f; t < 2.6f; t += Time.deltaTime)
        {
            if (cam != null) Encuadre(cam, pos + Vector3.up * 3.6f - (d - desde).normalized * 0.8f, pos, 2f);   // casi desde arriba, sobre el colgante
            yield return null;
        }

        // "Abby…"
        quien = "???"; texto = "Abby…"; textoT = 0f;
        for (float t = 0f; t < 3.6f; t += Time.deltaTime) { textoT += Time.deltaTime; yield return null; }
        texto = null;

        // corte a negro
        negro = 1f;
        yield return new WaitForSeconds(1.2f);
        tarjeta = "Continuará…"; subtarjeta = Historia.Titulo;
        for (float t = 0f; t < 4f; t += Time.deltaTime) { tarjetaA = Mathf.Clamp01(Mathf.Min(t / 0.8f, (4f - t) / 0.8f)); yield return null; }
        tarjeta = null;
        yield return new WaitForSeconds(0.6f);
        string sig = Application.CanStreamedLevelBeLoaded(escenaAlTerminar) ? escenaAlTerminar : SceneManager.GetSceneByBuildIndex(0).name;
        SceneManager.LoadScene(Application.CanStreamedLevelBeLoaded(sig) ? sig : "Intro");
    }

    GameObject Colgante(Vector3 pos)
    {
        var g = new GameObject("ColganteDeDante");
        g.transform.position = pos + Vector3.up * 0.06f;
        var lit = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        lit.SetColor("_BaseColor", new Color(0.75f, 0.68f, 0.5f)); lit.SetFloat("_Metallic", 0.8f); lit.SetFloat("_Smoothness", 0.7f);
        var gema = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        gema.SetColor("_BaseColor", new Color(0.2f, 0.1f, 0.3f)); gema.EnableKeyword("_EMISSION");
        gema.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        gema.SetColor("_EmissionColor", new Color(0.7f, 0.35f, 1f) * 2f);
        var disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        Destroy(disco.GetComponent<Collider>());
        disco.transform.SetParent(g.transform, false);
        disco.transform.localScale = new Vector3(0.28f, 0.02f, 0.28f);
        disco.GetComponent<Renderer>().sharedMaterial = lit;
        var piedra = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Destroy(piedra.GetComponent<Collider>());
        piedra.transform.SetParent(g.transform, false);
        piedra.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        piedra.transform.localScale = new Vector3(0.12f, 0.05f, 0.12f);
        piedra.GetComponent<Renderer>().sharedMaterial = gema;
        for (int i = 0; i < 14; i++)   // la cadena
        {
            var e = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(e.GetComponent<Collider>());
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
        l.type = LightType.Point; l.color = new Color(0.75f, 0.45f, 1f); l.range = 3f; l.intensity = 3f; l.shadows = LightShadows.None;
        CombatFx.Burst(pos + Vector3.up * 0.2f, new Color(0.8f, 0.6f, 1f), 30, 2f, 0.12f, 0.8f, -0.4f);
        return g;
    }

    static void Encuadre(Camera cam, Vector3 pos, Vector3 mira, float vel)
    {
        cam.transform.position = Vector3.Lerp(cam.transform.position, pos, Time.deltaTime * vel);
        cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(mira - cam.transform.position), Time.deltaTime * vel * 1.5f);
    }

    void OnGUI()
    {
        if (!corriendo) return;
        GUI.depth = -960;
        if (!string.IsNullOrEmpty(texto)) CuadroNarracion.Dialogo(quien, texto, CuadroNarracion.Letras(texto, textoT * 0.6f), 1f);
        CuadroNarracion.Negro(negro);
        if (!string.IsNullOrEmpty(tarjeta)) CuadroNarracion.Tarjeta(tarjeta, subtarjeta, tarjetaA);
    }
}
