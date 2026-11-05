using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Dirección de arte de la historia (las imágenes de la intro: pueblo costero al atardecer, cielo naranja y morado, faroles
/// encendidos, piedra y madera oscuras): los pisos generados con el ambiente "Día" (tarde soleada de cuento, pasto verde
/// brillante, tejas rojas) se pasan al atardecer al cargar la escena, sin regenerarlos.
/// - Sol bajo y naranja de lado, cielo violeta arriba y naranja en el horizonte, bruma cálida.
/// - Luz de ambiente cálida-violeta (lo que da la espalda al sol no queda negro).
/// - Tejas, pasto y hojas más apagados (como en el pixel art), sin tocar los materiales guardados.
/// - Los faroles, que de día no tenían luz, se encienden.
/// </summary>
public static class AmbienteHistoria
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
        Aplicar();
    }

    static void AlCargar(Scene s, LoadSceneMode m) { if (m == LoadSceneMode.Single) Aplicar(); }

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    static void Aplicar()
    {
        var b = Object.FindAnyObjectByType<DioramaBuilder>();
        bool exterior = Object.FindAnyObjectByType<ExteriorBuilder>() != null;   // Cala Sombría: ya es de atardecer, solo los colores
        if (exterior) { Apagar(); return; }
        if (b == null) return;
        bool cascadas = b.layout == DioramaBuilder.Layout.Cascadas, acantilado = b.layout == DioramaBuilder.Layout.Acantilado;
        if (b.CurrentMood != DioramaBuilder.Mood.Dia && !acantilado) return;

        // GDD: cada piso tiene su color dominante. Pueblo: ámbar · Cascadas: azul violeta · Acantilado: carmesí
        // (Puerto y Cima ya tienen su tono propio: atardecer rosa y crepúsculo)
        Color luz = new Color(1f, 0.6f, 0.36f), arriba = new Color(0.18f, 0.16f, 0.4f), horizonte = new Color(1f, 0.5f, 0.27f);
        Color disco = new Color(1f, 0.58f, 0.3f), nubes = new Color(0.98f, 0.6f, 0.52f), bruma = new Color(0.62f, 0.42f, 0.44f);
        Color ambCielo = new Color(0.66f, 0.56f, 0.72f), ambMedio = new Color(0.64f, 0.48f, 0.44f), ambSuelo = new Color(0.24f, 0.19f, 0.21f);
        if (cascadas)
        {
            luz = new Color(0.72f, 0.74f, 1f); arriba = new Color(0.07f, 0.12f, 0.34f); horizonte = new Color(0.56f, 0.5f, 0.86f);
            disco = new Color(0.8f, 0.78f, 1f); nubes = new Color(0.62f, 0.6f, 0.88f); bruma = new Color(0.42f, 0.46f, 0.66f);
            ambCielo = new Color(0.56f, 0.62f, 0.82f); ambMedio = new Color(0.46f, 0.5f, 0.64f); ambSuelo = new Color(0.18f, 0.2f, 0.26f);
        }
        else if (acantilado)
        {
            luz = new Color(1f, 0.46f, 0.36f); arriba = new Color(0.27f, 0.17f, 0.4f); horizonte = new Color(1f, 0.46f, 0.3f);
            disco = new Color(1f, 0.52f, 0.3f); nubes = new Color(0.98f, 0.56f, 0.5f); bruma = new Color(0.58f, 0.3f, 0.36f);
            ambCielo = new Color(0.66f, 0.46f, 0.6f); ambMedio = new Color(0.62f, 0.4f, 0.4f); ambSuelo = new Color(0.24f, 0.16f, 0.2f);
        }

        // sol bajo de lado: caras iluminadas y sombras largas
        var sol = RenderSettings.sun;
        if (sol == null) foreach (var l in Object.FindObjectsByType<Light>()) if (l.type == LightType.Directional) { sol = l; break; }
        if (sol != null)
        {
            sol.transform.rotation = Quaternion.Euler(28f, 118f, 0f);
            sol.color = luz;
            sol.intensity = cascadas ? 1.7f : 2.1f;
            sol.shadowStrength = 0.85f;
        }

        var cielo = Object.FindAnyObjectByType<ExteriorSky>();
        if (cielo != null)
        {
            cielo.zenith = arriba;
            cielo.horizon = horizonte;
            cielo.sunColor = disco * 2.3f;
            cielo.sunSize = 0.011f;
            cielo.cloudColor = nubes;
            cielo.cloudCover = 0.5f;
            cielo.driveFog = false;
            cielo.driveAmbient = false;
            cielo.ground = bruma;
            cielo.Apply();
        }
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = bruma;
        RenderSettings.fogStartDistance = 90f;
        RenderSettings.fogEndDistance = 640f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambCielo;
        RenderSettings.ambientEquatorColor = ambMedio;
        RenderSettings.ambientGroundColor = ambSuelo;

        Apagar();
        var jugador = GameObject.FindWithTag("Player");
        if (jugador != null) new GameObject("LuzAbby").AddComponent<LuzPersonaje>().objetivo = jugador.transform;

        // faroles: de día se generaron sin luz
        int n = 0;
        foreach (var t in Object.FindObjectsByType<Transform>())
        {
            if (t.name != "Farol" || t.GetComponentInChildren<Light>(true) != null) continue;
            var vidrio = t.Find("Vidrio");
            var go = new GameObject("LuzFarol");
            go.transform.SetParent(t, false);
            go.transform.position = (vidrio != null ? vidrio.position : t.position + Vector3.up * 2.9f) + Vector3.down * 0.2f;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.72f, 0.42f); l.range = 8f; l.intensity = 3f; l.shadows = LightShadows.None;
            go.AddComponent<LightFlicker>().amount = 0.06f;
            if (++n >= 40) break;   // tope: muchas luces puntuales cuestan
        }
    }

    /// <summary>
    /// Luz suave que sigue a Abby, arriba y hacia la cámara: SpriteLit3D alumbra los sprites por lo que tienen encima y el sol bajo
    /// del atardecer casi no les pega (quedaban como silueta). Alcanza también a los enemigos cercanos: el combate se lee mejor.
    /// </summary>
    class LuzPersonaje : MonoBehaviour
    {
        public Transform objetivo;
        void Start()
        {
            var l = gameObject.AddComponent<Light>();
            l.type = LightType.Point; l.color = new Color(1f, 0.9f, 0.8f); l.range = 9f; l.intensity = 1.1f; l.shadows = LightShadows.None;
        }
        void LateUpdate()
        {
            var cam = Camera.main;
            if (objetivo == null || cam == null) return;
            Vector3 f = cam.transform.forward; f.y = 0f;
            transform.position = objetivo.position + Vector3.up * 2.8f - f.normalized * 2.5f;
        }
    }

    /// <summary>Colores más apagados (tejas de barro oscuro, pasto de atardecer); con un bloque de propiedades, no en el material.</summary>
    static void Apagar()
    {
        var mpb = new MaterialPropertyBlock();
        foreach (var r in Object.FindObjectsByType<Renderer>())
        {
            var m = r.sharedMaterial;
            if (m == null || r is ParticleSystemRenderer) continue;
            Color k;
            if (m.name.StartsWith("Dio_Tejas")) k = new Color(0.74f, 0.6f, 0.6f);
            else if (m.name.StartsWith("Dio_Pasto") || m.name.StartsWith("Dio_Hojas") || m.name.StartsWith("Dio_Pino")) k = new Color(0.88f, 0.85f, 0.74f);
            else continue;
            r.GetPropertyBlock(mpb);
            if (m.HasProperty(BaseColorId)) mpb.SetColor(BaseColorId, m.GetColor(BaseColorId) * k);
            if (m.HasProperty(ColorId)) mpb.SetColor(ColorId, m.GetColor(ColorId) * k);
            r.SetPropertyBlock(mpb);
        }
    }
}
