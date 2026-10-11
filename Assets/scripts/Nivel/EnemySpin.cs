using UnityEngine;

/// <summary>Gira en su eje Y (corona del jefe, aspas del torbellino, orbes del hechicero).</summary>
public class EnemySpin : MonoBehaviour
{
    public float speed = 30f;
    void Update() => transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.Self);
}
