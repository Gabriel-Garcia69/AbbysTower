using UnityEngine;

/// <summary>
/// La noche de la Cala (la crea ExteriorBuilder.Noche.cs dentro de Ext_Cala.prefab, solo cuando horaDelDia = Noche):
/// pone el skybox nocturno, el ambiente en gradiente y la niebla exponencial; lleva el cielo (luna, planeta, nubes y aurora)
/// pegado a la cámara para que esté siempre "en el infinito"; y en juego le pone a Abby una luz suave para que se lea de noche.
/// Es solo del Exterior: ExteriorSky (y sus valores por defecto) lo siguen usando Diorama y Cinematica tal cual.
/// </summary>
[ExecuteAlways]
[DefaultExecutionOrder(2000)]   // después de la cámara de ExteriorIntro y del CameraRig
public class ExteriorNoche : MonoBehaviour
{
    [Header("Cielo y ambiente")]
    public Material skybox;
    [Tooltip("Hijo con la luna, el planeta, las nubes y la aurora; sigue la posición de la cámara.")]
    public Transform cielo;
    public Color ambienteCielo = new Color(0.14f, 0.14f, 0.2f);
    public Color ambienteEcuador = new Color(0.16f, 0.135f, 0.21f);
    public Color ambienteSuelo = new Color(0.05f, 0.045f, 0.065f);
    public Color niebla = new Color(0.05f, 0.04f, 0.07f);
    public float densidadNiebla = 0.0034f;

    [Header("Luz que sigue a Abby (sus sprites son Lit: sin esto se pierden en la sombra)")]
    public bool luzDeAbby = true;
    public Color colorLuzAbby = new Color(0.9f, 0.84f, 0.8f);
    public float rangoLuzAbby = 13f, intensidadLuzAbby = 10f;

    Light luz;
    Camera cam;

    void OnEnable() => Aplicar();

    void Update()
    {
        if (!Application.isPlaying) { Aplicar(); return; }   // en el editor se ve sin dar Play
        if (!luzDeAbby) return;
        if (luz == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p == null) return;
            var go = new GameObject("LuzNocheAbby");
            go.transform.SetParent(p.transform, false);
            go.transform.localPosition = new Vector3(0f, 6f, 0f);   // alta: pegada a la cabeza la quemaba (el sprite tiene la normal hacia arriba)
            luz = go.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = colorLuzAbby; luz.range = rangoLuzAbby; luz.intensity = intensidadLuzAbby;
            luz.shadows = LightShadows.None;
        }
    }

    void LateUpdate()
    {
        if (cielo == null) return;
        if (cam == null) cam = Camera.main;
        if (cam != null && Application.isPlaying) cielo.position = cam.transform.position;
    }

    void OnDisable()
    {
        if (luz != null) { if (Application.isPlaying) Destroy(luz.gameObject); else DestroyImmediate(luz.gameObject); }
    }

    void Aplicar()
    {
        if (skybox != null && RenderSettings.skybox != skybox) RenderSettings.skybox = skybox;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambienteCielo;
        RenderSettings.ambientEquatorColor = ambienteEcuador;
        RenderSettings.ambientGroundColor = ambienteSuelo;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogColor = niebla;
        RenderSettings.fogDensity = densidadNiebla;
    }
}
