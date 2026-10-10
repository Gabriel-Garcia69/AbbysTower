using UnityEngine;

/// <summary>Balanceo suave de faroles y candelabros colgantes (como si hubiera corriente de aire).</summary>
public class LanternSway : MonoBehaviour
{
    public float angle = 3f;
    public float speed = 0.7f;

    Quaternion baseRot;
    float seed;

    void Awake() { baseRot = transform.localRotation; seed = Random.value * 10f; }

    void Update()
    {
        float t = Time.time * speed + seed;
        transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(t) * angle, 0f, Mathf.Sin(t * 0.77f + 1.3f) * angle * 0.6f);
    }
}
