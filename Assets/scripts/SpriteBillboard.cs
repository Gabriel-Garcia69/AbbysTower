using UnityEngine;

/// <summary>
/// Billboard cilíndrico: el sprite siempre mira a la cámara pero se mantiene vertical.
/// Ponlo en el hijo "Visual" (NO en el objeto con el CharacterController).
/// El pivote del sprite debe estar en los pies (Pivot = Bottom) y el hijo en y=0 del raíz.
/// </summary>
[DefaultExecutionOrder(100)] // corre después de que la cámara se actualiza
public class SpriteBillboard : MonoBehaviour
{
    [SerializeField] Camera targetCamera;
    [SerializeField] bool keepUpright = true;
    [Tooltip("Estira el sprite en Y para compensar el acortamiento por la inclinación de la cámara (look Octopath).")]
    [SerializeField] bool compensatePitch = true;

    Vector3 _baseScale;

    void Awake()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        _baseScale = transform.localScale;
    }

    void LateUpdate()
    {
        Transform cam = targetCamera.transform;

        if (keepUpright)
        {
            Vector3 flatForward = cam.forward;
            flatForward.y = 0f;
            if (flatForward.sqrMagnitude < 0.0001f) flatForward = cam.up; // cámara mirando recto hacia abajo
            transform.rotation = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
        }
        else
        {
            transform.rotation = cam.rotation;
        }

        if (compensatePitch && keepUpright)
        {
            float pitch = Mathf.Asin(Mathf.Clamp(-cam.forward.y, -1f, 1f));
            float k = 1f / Mathf.Max(Mathf.Cos(pitch), 0.5f);
            transform.localScale = new Vector3(_baseScale.x, _baseScale.y * k, _baseScale.z);
        }
    }
}
