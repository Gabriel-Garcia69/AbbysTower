using UnityEngine;

/// <summary>
/// Sacudida de cámara: CameraShake.Shake(fuerza, duración). Se agrega sola a la cámara principal la primera vez.
/// Solo mueve la posición local de la cámara (el rig y el brazo siguen igual), y la regresa al terminar.
/// </summary>
[DefaultExecutionOrder(2000)]
public class CameraShake : MonoBehaviour
{
    static CameraShake instance;

    Vector3 basePos;
    float strength, time, duration;

    public static void Shake(float strength, float duration)
    {
        if (instance == null)
        {
            var cam = Camera.main;
            if (cam == null) return;
            instance = cam.GetComponent<CameraShake>();
            if (instance == null) instance = cam.gameObject.AddComponent<CameraShake>();
        }
        if (strength >= instance.strength * (instance.time / Mathf.Max(0.01f, instance.duration)))
        {
            instance.strength = strength; instance.duration = duration; instance.time = duration;
        }
    }

    void Awake() => basePos = transform.localPosition;

    void LateUpdate()
    {
        if (time <= 0f) return;
        time -= Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(time / Mathf.Max(0.01f, duration));
        if (time <= 0f) { transform.localPosition = basePos; strength = 0f; return; }
        float s = strength * k * k;
        float n = Time.unscaledTime * 38f;
        transform.localPosition = basePos + new Vector3(Mathf.PerlinNoise(n, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, n) - 0.5f, 0f) * 2f * s;
    }
}
