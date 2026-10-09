using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cámara orbital con el mouse (y stick derecho del control): gira libre alrededor de Abby
/// en vez de los saltos de 90° con Q/E. Va en el mismo objeto que CameraRig (que sigue al jugador),
/// y mueve el yaw del rig y el pitch del CameraArm.
/// Cursor: se oculta y queda fijo al hacer clic en el juego; Esc lo libera.
/// Para quitar Q/E deja vacío el campo Input del CameraRig en la escena.
/// </summary>
[DefaultExecutionOrder(50)]
public class MouseOrbitCamera : MonoBehaviour
{
    [Tooltip("El hijo con el pitch (CameraArm). Vacío = primer hijo.")]
    public Transform arm;

    [Header("Sensibilidad")]
    [Tooltip("Grados por píxel que se mueve el mouse.")]
    public float mouseSensitivity = 0.12f;
    [Tooltip("Grados por segundo con el stick derecho al máximo.")]
    public float stickSpeed = 150f;
    public bool invertY = false;

    [Header("Inclinación (grados, positivo = mira hacia abajo)")]
    public float minPitch = -5f;
    public float maxPitch = 40f;

    [Header("Cursor")]
    public bool lockCursor = true;

    float yaw, pitch;

    void Start()
    {
        if (arm == null && transform.childCount > 0) arm = transform.GetChild(0);
        yaw = transform.eulerAngles.y;
        pitch = arm != null ? Mathf.DeltaAngle(0f, arm.localEulerAngles.x) : 10f;
        if (lockCursor) Lock(true);
    }

    void OnDisable() => Lock(false);

    void Update()
    {
        var mouse = Mouse.current;
        var kb = Keyboard.current;

        if (lockCursor)
        {
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Lock(false);
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) Lock(true);
        }

        Vector2 look = Vector2.zero;
        if (mouse != null && (!lockCursor || Cursor.lockState == CursorLockMode.Locked))
            look += mouse.delta.ReadValue() * mouseSensitivity;
        var pad = Gamepad.current;
        if (pad != null)
            look += pad.rightStick.ReadValue() * stickSpeed * Time.deltaTime;

        yaw += look.x;
        pitch += invertY ? look.y : -look.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (arm != null) arm.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    static void Lock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
