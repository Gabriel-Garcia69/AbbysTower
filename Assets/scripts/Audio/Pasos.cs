using UnityEngine;

/// <summary>Pasos en piedra (GDD): un golpecito por pisada mientras Abby camina por el suelo.</summary>
public class Pasos : MonoBehaviour
{
    public float cadencia = 0.34f;
    PlayerMotor motor;
    float t;

    void Awake() => motor = GetComponent<PlayerMotor>();

    void Update()
    {
        if (motor == null || !motor.IsGrounded || !motor.IsMoving) { t = cadencia * 0.6f; return; }
        t -= Time.deltaTime * motor.SpeedScale;
        if (t > 0f) return;
        t = cadencia;
        Sonido.Tocar(Sonido.Efecto.Paso, 0.35f, Random.Range(0.85f, 1.15f));
    }
}
