using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Toma de cámara para un lugar del mapa (estilo Octopath / DQ3 HD-2D): mientras Abby está dentro del área, la cámara
/// se mueve suave a un encuadre pensado para lucir la vista (ángulo, distancia, lente y hacia dónde se corre el cuadro).
/// Al salir regresa a la cámara normal. Con introSeconds > 0 es la toma de apertura de la escena (no depende del lugar).
/// La aplica MouseOrbitCamera en los estilos que la permiten (Diorama).
/// </summary>
public class CameraShot : MonoBehaviour
{
    public static readonly List<CameraShot> All = new List<CameraShot>();

    [Header("Área (local, centrada en este objeto)")]
    public Vector2 size = new Vector2(10f, 10f);
    public float yMin = -3f, yMax = 6f;

    [Header("Encuadre")]
    [Tooltip("Hacia dónde mira la cámara (grados, 0 = norte).")]
    public float yaw;
    public float pitch = 25f;
    public float distance = 25f;
    public float fov = 28f;
    [Tooltip("Grados que la lente sube sobre Abby: deja más cielo/paisaje arriba.")]
    public float lookUp;
    [Tooltip("Cuánto se corre el centro del cuadro desde Abby (mundo): pone la vista a un lado y a Abby en un tercio.")]
    public Vector3 offset;
    [Tooltip("Toma de vista: solo entra si Abby se detiene un momento en el área (no interrumpe al ir de paso).")]
    public bool whenStill;
    [Tooltip("Segundos que tarda en llegar al encuadre.")]
    public float blendTime = 1.4f;
    public int priority;

    [Header("Apertura de escena")]
    [Tooltip("> 0: toma de apertura que dura estos segundos al cargar la escena.")]
    public float introSeconds;

    [Header("Letrero (la primera vez que se entra)")]
    public string title = "";
    public string subtitle = "";
    [System.NonSerialized] public bool shown;

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    public bool Contains(Vector3 world)
    {
        if (introSeconds > 0f) return Time.timeSinceLevelLoad < introSeconds;
        Vector3 p = transform.InverseTransformPoint(world);
        return Mathf.Abs(p.x) <= size.x / 2f && Mathf.Abs(p.z) <= size.y / 2f && p.y >= yMin && p.y <= yMax;
    }

    /// <summary>La toma que manda en esta posición (la de mayor prioridad), o null.</summary>
    public static CameraShot At(Vector3 world)
    {
        CameraShot best = null;
        foreach (var s in All)
            if (s != null && s.isActiveAndEnabled && s.Contains(world) && (best == null || s.priority > best.priority)) best = s;
        return best;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(new Vector3(0f, (yMin + yMax) / 2f, 0f), new Vector3(size.x, yMax - yMin, size.y));
    }
}
