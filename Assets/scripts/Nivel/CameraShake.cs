using UnityEngine;

/// <summary>
/// Sacudida de cámara: CameraShake.Shake(fuerza, duración). Se agrega sola a la cámara principal la primera vez.
/// Solo suma un desplazamiento a la posición local de la cámara (el rig y el brazo siguen igual) y lo quita al terminar.
/// Se aplica sobre la posición actual, así no pelea con el zoom/colisión/estilos de MouseOrbitCamera.
/// </summary>
[DefaultExecutionOrder(2000)]
public class CameraShake : MonoBehaviour
{
    static CameraShake instance;

    Vector3 applied;   // desplazamiento sumado el cuadro anterior
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

    void LateUpdate()
    {
        if (time <= 0f) return;
        Vector3 basePos = transform.localPosition - applied;   // MouseOrbitCamera solo reescribe z: x/y aún traen el desplazamiento anterior
        time -= Time.unscaledDeltaTime;
        float k = Mathf.Clamp01(time / Mathf.Max(0.01f, duration));
        if (time <= 0f) { transform.localPosition = basePos; applied = Vector3.zero; strength = 0f; return; }
        float s = strength * k * k;
        float n = Time.unscaledTime * 38f;
        applied = new Vector3(Mathf.PerlinNoise(n, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, n) - 0.5f, 0f) * 2f * s;
        transform.localPosition = basePos + applied;
    }
}
