using UnityEngine;

/// <summary>
/// Vaivén lento para lo que flota en el abismo (islas, rocas sueltas): sube y baja con un seno y puede girar despacio.
/// Guarda la posición de inicio, así no se va desplazando con el tiempo.
/// </summary>
public class FloatingBob : MonoBehaviour
{
    public float amplitude = 0.8f;   // metros
    public float period = 9f;        // segundos por vaivén
    public float phase;
    public float spin;               // grados por segundo en Y

    Vector3 basePos;

    void Start() => basePos = transform.localPosition;

    void Update()
    {
        float t = (Time.time + phase) / Mathf.Max(0.1f, period) * Mathf.PI * 2f;
        transform.localPosition = basePos + Vector3.up * Mathf.Sin(t) * amplitude;
        if (spin != 0f) transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.Self);
    }
}
