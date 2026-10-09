using UnityEngine;

/// <summary>
/// Tapa de piedra encima de un muro (le da grosor cuando la cámara mira desde arriba).
/// Los muros son de una cara y desaparecen cuando la cámara queda detrás de ellos; esta tapa hace lo mismo
/// (si la cámara está del lado de afuera del muro, se oculta) para no dejar una franja flotando.
/// </summary>
public class WallCap : MonoBehaviour
{
    [Tooltip("Hacia dónde mira la cara visible del muro (hacia adentro de la sala / pasillo), en el espacio del padre.")]
    public Vector3 inward = Vector3.forward;

    Renderer rend;
    Transform cam;

    void Awake() => rend = GetComponent<Renderer>();

    void LateUpdate()
    {
        if (rend == null) return;
        if (cam == null) { var c = Camera.main; if (c == null) return; cam = c.transform; }
        Vector3 n = transform.parent != null ? transform.parent.TransformDirection(inward) : inward;
        Vector3 toCam = cam.position - transform.position; toCam.y = 0f;
        rend.enabled = Vector3.Dot(n, toCam) > -0.5f;
    }
}
