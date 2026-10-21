using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Efecto maqueta (tilt-shift) del diorama: el desenfoque de profundidad (Bokeh) del Volume enfoca siempre a Abby,
/// así lo lejano (y lo que pasa pegado a la cámara) se suaviza como en Octopath, sin tocar lo que rodea a Abby.
/// Arranca apagado (todo nítido); F7 lo prende/apaga para comparar. Va en la cámara principal.
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
    [Tooltip("Milímetros de lente por metro de distancia al foco: a 34 m da ~290 mm (maqueta); a 3 m, ~25 mm (casi nada borroso).")]
    public float focalPerMeter = 6f;
    public float maxFocalLength = 220f;
    [Tooltip("Apertura (f/). Más alto = más zona nítida alrededor de Abby.")]
    public float aperture = 4f;
    [Tooltip("Arranca con el desenfoque prendido. Apagado por defecto: el diorama se ve nítido; F7 lo prende para comparar.")]
    public bool startOn = false;

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
        if (dof != null) { focus = dof.focusDistance.value; dof.active = startOn; }
    }

    void LateUpdate()
    {
        if (dof == null || player == null) return;
        // (F7 ya no lo cambia: solo quedan F3 y F4 para la cámara)
        dof.aperture.value = aperture;
        float want = Vector3.Distance(transform.position, player.position + Vector3.up * focusHeight);
        focus = Mathf.SmoothDamp(focus, want, ref vel, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        dof.focusDistance.value = focus;
        // efecto maqueta solo de lejos: con la cámara cerca (F3) una lente larga dejaría todo borroso
        dof.focalLength.value = Mathf.Clamp(focus * focalPerMeter, 18f, maxFocalLength);
    }
}
