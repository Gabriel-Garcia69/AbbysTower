using UnityEngine;

/// <summary>
/// Parpadeo suave de fuego para antorchas, faroles y braseros: varía intensidad (y un poco el alcance)
/// con ruido Perlin. Va en el objeto padre de la luz o en la luz misma.
/// </summary>
public class LightFlicker : MonoBehaviour
{
    [Range(0f, 1f)] public float amount = 0.18f;
    public float speed = 6f;

    Light lt;
    float baseIntensity, baseRange, seed;

    void Awake()
    {
        lt = GetComponentInChildren<Light>();
        if (lt == null) { enabled = false; return; }
        baseIntensity = lt.intensity;
        baseRange = lt.range;
        seed = Random.value * 100f;
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(seed, Time.time * speed) * 2f - 1f;   // -1..1
        lt.intensity = baseIntensity * (1f + n * amount);
        lt.range = baseRange * (1f + n * amount * 0.25f);
    }
}
