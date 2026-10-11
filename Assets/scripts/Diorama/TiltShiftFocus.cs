using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Efecto maqueta (tilt-shift) del diorama: el desenfoque de profundidad (Bokeh) del Volume enfoca siempre a Abby,
/// así lo cercano y lo lejano se ven borrosos como en Octopath. Va en la cámara principal.
/// </summary>
[DefaultExecutionOrder(3000)]
public class TiltShiftFocus : MonoBehaviour
{
    [Tooltip("Vacío = el primer Volume global de la escena.")]
    public Volume volume;
    [Tooltip("Altura del punto de enfoque sobre los pies de Abby (m).")]
    public float focusHeight = 0.7f;
    [Tooltip("Suavizado del enfoque (s).")]
    public float smoothTime = 0.15f;

    DepthOfField dof;
    Transform player;
    float focus, vel;

    void Start()
    {
        if (volume == null) volume = FindAnyObjectByType<Volume>();
        // .profile clona el asset en juego: no se toca el perfil guardado
        if (volume != null && volume.profile != null) volume.profile.TryGet(out dof);
        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
        if (dof != null) focus = dof.focusDistance.value;
    }

    void LateUpdate()
    {
        if (dof == null || player == null) return;
        float want = Vector3.Distance(transform.position, player.position + Vector3.up * focusHeight);
        focus = Mathf.SmoothDamp(focus, want, ref vel, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        dof.focusDistance.value = focus;
    }
}
