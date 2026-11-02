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
        if (b == null || b.CurrentMood != DioramaBuilder.Mood.Dia) return;

        // sol bajo de lado: caras iluminadas en naranja y sombras largas
        var sol = RenderSettings.sun;
        if (sol == null) foreach (var l in Object.FindObjectsByType<Light>()) if (l.type == LightType.Directional) { sol = l; break; }
        if (sol != null)
        {
            sol.transform.rotation = Quaternion.Euler(24f, 118f, 0f);
            sol.color = new Color(1f, 0.6f, 0.36f);
            sol.intensity = 1.55f;
            sol.shadowStrength = 0.85f;
        }

        var cielo = Object.FindAnyObjectByType<ExteriorSky>();
        if (cielo != null)
        {
            cielo.zenith = new Color(0.18f, 0.16f, 0.4f);
            cielo.horizon = new Color(1f, 0.5f, 0.27f);
            cielo.sunColor = new Color(1f, 0.58f, 0.3f) * 2.3f;
            cielo.sunSize = 0.011f;
            cielo.cloudColor = new Color(0.98f, 0.6f, 0.52f);
            cielo.cloudCover = 0.5f;
            cielo.driveFog = false;
            cielo.driveAmbient = false;
        }
        Color bruma = new Color(0.62f, 0.42f, 0.44f);
        if (cielo != null) { cielo.ground = bruma; cielo.Apply(); }
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = bruma;
        RenderSettings.fogStartDistance = 90f;
        RenderSettings.fogEndDistance = 640f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.5f, 0.42f, 0.58f);
        RenderSettings.ambientEquatorColor = new Color(0.46f, 0.34f, 0.34f);
        RenderSettings.ambientGroundColor = new Color(0.16f, 0.12f, 0.15f);

        // colores más apagados (tejas de barro oscuro, pasto de atardecer); con un bloque de propiedades, no en el material
        var mpb = new MaterialPropertyBlock();
        foreach (var r in Object.FindObjectsByType<Renderer>())
        {
            var m = r.sharedMaterial;
            if (m == null || r is ParticleSystemRenderer) continue;
            Color k;
            if (m.name.StartsWith("Dio_Tejas")) k = new Color(0.62f, 0.5f, 0.52f);
            else if (m.name.StartsWith("Dio_Pasto") || m.name.StartsWith("Dio_Hojas") || m.name.StartsWith("Dio_Pino")) k = new Color(0.78f, 0.76f, 0.66f);
            else continue;
            r.GetPropertyBlock(mpb);
            if (m.HasProperty(BaseColorId)) mpb.SetColor(BaseColorId, m.GetColor(BaseColorId) * k);
            if (m.HasProperty(ColorId)) mpb.SetColor(ColorId, m.GetColor(ColorId) * k);
            r.SetPropertyBlock(mpb);
        }

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
}
