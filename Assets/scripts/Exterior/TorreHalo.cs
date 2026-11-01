using UnityEngine;

/// <summary>
/// Mantiene un quad de resplandor detrás de la torre respecto de la cámara (la torre lo tapa, así el halo queda como
/// un aura alrededor de la silueta desde cualquier lado). Lo crea ExteriorBuilder.Torre.cs; sin cámara se queda donde lo dejó el builder.
/// </summary>
public class TorreHalo : MonoBehaviour
{
    [Tooltip("Centro del halo, en el espacio local del padre (la torre).")]
    public Vector3 centro;
    [Tooltip("Distancia detrás del centro, en el plano horizontal, alejándose de la cámara.")]
    public float atras = 30f;

    Camera cam;

    void LateUpdate()
    {
        if (cam == null) { cam = Camera.main; if (cam == null) return; }
        Vector3 c = transform.parent.TransformPoint(centro);
        Vector3 d = c - cam.transform.position; d.y = 0f;
        if (d.sqrMagnitude < 1f) return;
        d.Normalize();
        transform.SetPositionAndRotation(c + d * atras, Quaternion.LookRotation(d));
    }
}
