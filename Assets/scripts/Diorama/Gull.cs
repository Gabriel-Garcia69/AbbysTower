using UnityEngine;

/// <summary>
/// Gaviota: vuela en círculos sobre un punto, sube y baja un poco y aletea (dos alas que giran en su raíz).
/// DioramaBuilder la arma con primitivas; las alas son los dos primeros hijos.
/// </summary>
public class Gull : MonoBehaviour
{
    public Vector3 center;
    public float radius = 12f;
    public float speed = 0.35f;       // radianes por segundo
    public float height = 9f;
    public float phase;
    public Transform wingL, wingR;

    void Update()
    {
        float t = Time.time * speed + phase;
        float bob = Mathf.Sin(t * 2.3f) * 0.8f;
        var pos = center + new Vector3(Mathf.Cos(t) * radius, height + bob, Mathf.Sin(t) * radius);
        var dir = new Vector3(-Mathf.Sin(t), 0f, Mathf.Cos(t)) * Mathf.Sign(speed);
        transform.localPosition = pos;
        transform.localRotation = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 0f, -12f * Mathf.Sign(speed));   // se inclina en la vuelta
        // aletea a ratos y planea el resto
        float flap = Mathf.Sin(Time.time * 9f + phase * 3f) * 35f * Mathf.Clamp01(Mathf.Sin(t * 1.7f) * 2f + 0.3f);
        if (wingL != null) wingL.localRotation = Quaternion.Euler(0f, 0f, 10f + flap);
        if (wingR != null) wingR.localRotation = Quaternion.Euler(0f, 0f, -10f - flap);
    }
}
