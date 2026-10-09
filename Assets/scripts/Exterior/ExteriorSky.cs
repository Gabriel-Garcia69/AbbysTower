using UnityEngine;

/// <summary>
/// Publica los colores del cielo (Abby/Sky) como variables globales para que el agua (Abby/Water)
/// refleje exactamente el mismo cielo. También ajusta la luz ambiental y la niebla para que combinen.
/// Va en cualquier objeto de la escena (ExecuteAlways: se ve en el editor sin dar Play).
/// </summary>
[ExecuteAlways]
public class ExteriorSky : MonoBehaviour
{
    [Header("Cielo")]
    public Color zenith = new Color(0.16f, 0.28f, 0.58f);
    public Color horizon = new Color(1f, 0.6f, 0.38f);
    public Color ground = new Color(0.22f, 0.2f, 0.24f);
    [ColorUsage(false, true)] public Color sunColor = new Color(1f, 0.78f, 0.52f);
    [Range(0.0005f, 0.02f)] public float sunSize = 0.0025f;

    [Header("Nubes")]
    public Color cloudColor = new Color(1f, 0.9f, 0.88f);
    [Range(0f, 1f)] public float cloudCover = 0.45f;
    public float cloudSpeed = 0.01f;

    [Header("Ambiente y niebla")]
    public bool driveAmbient = true;
    public bool driveFog = true;
    public float fogStart = 90f;
    public float fogEnd = 650f;

    static readonly int Zenith = Shader.PropertyToID("_AbbySkyZenith");
    static readonly int Horizon = Shader.PropertyToID("_AbbySkyHorizon");
    static readonly int Ground = Shader.PropertyToID("_AbbySkyGround");
    static readonly int Sun = Shader.PropertyToID("_AbbySunColor");
    static readonly int Cloud = Shader.PropertyToID("_AbbyCloudColor");
    static readonly int Cover = Shader.PropertyToID("_AbbyCloudCover");
    static readonly int Speed = Shader.PropertyToID("_AbbyCloudSpeed");
    static readonly int SunSize = Shader.PropertyToID("_AbbySunSize");

    void OnEnable() => Apply();
    void OnValidate() => Apply();
    void Update() => Apply();

    public void Apply()
    {
        Shader.SetGlobalColor(Zenith, zenith.linear);
        Shader.SetGlobalColor(Horizon, horizon.linear);
        Shader.SetGlobalColor(Ground, ground.linear);
        Shader.SetGlobalColor(Sun, sunColor.linear);
        Shader.SetGlobalColor(Cloud, cloudColor.linear);
        Shader.SetGlobalFloat(Cover, cloudCover);
        Shader.SetGlobalFloat(Speed, cloudSpeed);
        Shader.SetGlobalFloat(SunSize, sunSize);

        if (driveAmbient)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Color.Lerp(zenith, horizon, 0.25f) * 0.9f;
            RenderSettings.ambientEquatorColor = Color.Lerp(horizon, ground, 0.4f) * 0.7f;
            RenderSettings.ambientGroundColor = ground * 0.5f;
        }
        if (driveFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Color.Lerp(horizon, zenith, 0.5f) * 0.9f;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
        }
    }
}
