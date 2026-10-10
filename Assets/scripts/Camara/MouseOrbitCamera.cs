using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cámara orbital con el mouse (y stick derecho del control): gira libre alrededor de Abby
/// en vez de los saltos de 90° con Q/E. Va en el mismo objeto que CameraRig (que sigue al jugador),
/// y mueve el yaw del rig y el pitch del CameraArm.
/// Opcional (apagado por defecto, se activa por escena):
///  - Zoom con la rueda del mouse / gatillos del control (LT acerca, RT aleja).
///  - Colisión: si entre Abby y la cámara hay suelo o pared, la cámara se acerca (SphereCast) y luego regresa suave.
/// Cursor: se oculta y queda fijo al hacer clic en el juego; Esc lo libera.
/// Para quitar Q/E deja vacío el campo Input del CameraRig en la escena.
/// </summary>
[DefaultExecutionOrder(50)]
public class MouseOrbitCamera : MonoBehaviour
{
    [Tooltip("El hijo con el pitch (CameraArm). Vacío = primer hijo.")]
    public Transform arm;
    [Tooltip("Altura del pivote sobre los pies de Abby (m). 0 = no tocar la posición del CameraArm.")]
    [SerializeField] float pivotHeight = 0f;

    [Header("Sensibilidad")]
    [Tooltip("Grados por píxel que se mueve el mouse.")]
    public float mouseSensitivity = 0.12f;
    [Tooltip("Grados por segundo con el stick derecho al máximo.")]
    public float stickSpeed = 150f;
    public bool invertY = false;

    [Header("Inclinación (grados, positivo = mira hacia abajo)")]
    public float minPitch = -5f;
    public float maxPitch = 40f;

    [Header("Zoom (rueda del mouse, LT/RT del control)")]
    [SerializeField] bool zoomEnabled = false;
    [SerializeField] float minDistance = 2f;
    [SerializeField] float maxDistance = 18f;
    [Tooltip("Metros por cada paso de la rueda.")]
    [SerializeField] float zoomStep = 1.5f;
    [Tooltip("Metros por segundo con un gatillo a fondo.")]
    [SerializeField] float padZoomSpeed = 10f;
    [Tooltip("Suavizado del zoom (s).")]
    [SerializeField] float zoomSmoothTime = 0.12f;

    [Header("Colisión con el escenario")]
    [SerializeField] bool collisionEnabled = false;
    [SerializeField] float collisionRadius = 0.3f;
    [Tooltip("Capas que bloquean la cámara. Los colliders del jugador se ignoran siempre.")]
    [SerializeField] LayerMask collisionMask = ~0;
    [Tooltip("Lo más cerca que puede quedar la cámara del pivote (m).")]
    [SerializeField] float minCollisionDistance = 0.8f;
    [Tooltip("Qué tan rápido se acerca al chocar (s). Bajo = no atraviesa paredes.")]
    [SerializeField] float pullInSmoothTime = 0.05f;
    [Tooltip("Qué tan lento regresa a su distancia cuando ya no hay obstáculo (s).")]
    [SerializeField] float returnSmoothTime = 0.35f;
    [Tooltip("Tiempo sin obstáculo antes de empezar a regresar (s): evita que salte al rozar paredes.")]
    [SerializeField] float returnDelay = 0.15f;

    [Header("Cursor")]
    public bool lockCursor = true;

    float yaw, pitch;
    Camera cam;
    float targetDistance, zoomedDistance, zoomVel, currentDistance, distVel, clearTimer;
    Transform player;
    readonly RaycastHit[] hits = new RaycastHit[16];

    void Start()
    {
        if (arm == null && transform.childCount > 0) arm = transform.GetChild(0);
        yaw = transform.eulerAngles.y;
        pitch = arm != null ? Mathf.DeltaAngle(0f, arm.localEulerAngles.x) : 10f;
        if (arm != null && pivotHeight > 0f) arm.localPosition = new Vector3(0f, pivotHeight, 0f);
        cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            targetDistance = zoomedDistance = currentDistance = Mathf.Max(0.1f, -cam.transform.localPosition.z);
            if (zoomEnabled) targetDistance = zoomedDistance = currentDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }
        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
        if (lockCursor) Lock(true);
    }

    void OnEnable()
    {
        // al volver (p. ej. tras la cinemática) parte de donde quedó la cámara
        if (cam != null) currentDistance = zoomedDistance = Mathf.Max(0.1f, -cam.transform.localPosition.z);
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
        bool mouseActive = mouse != null && (!lockCursor || Cursor.lockState == CursorLockMode.Locked);
        if (mouseActive)
            look += mouse.delta.ReadValue() * mouseSensitivity;
        var pad = Gamepad.current;
        if (pad != null)
            look += pad.rightStick.ReadValue() * stickSpeed * Time.deltaTime;

        yaw += look.x;
        pitch += invertY ? look.y : -look.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (arm != null) arm.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        if (zoomEnabled)
        {
            // un paso por cada evento de la rueda (el valor crudo cambia según plataforma)
            if (mouse != null && Time.timeScale > 0f)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f) targetDistance -= Mathf.Sign(scroll) * zoomStep;
            }
            if (pad != null)
                targetDistance += (pad.rightTrigger.ReadValue() - pad.leftTrigger.ReadValue()) * padZoomSpeed * Time.unscaledDeltaTime;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }
    }

    // después de que CameraRig movió el rig (LateUpdate, orden 0): así el pivote ya está en su lugar
    void LateUpdate()
    {
        if (cam == null || arm == null || (!zoomEnabled && !collisionEnabled)) return;
        float dt = Time.unscaledDeltaTime;

        zoomedDistance = Mathf.SmoothDamp(zoomedDistance, targetDistance, ref zoomVel, zoomSmoothTime, Mathf.Infinity, dt);
        float allowed = zoomedDistance;

        if (collisionEnabled)
        {
            Vector3 pivot = arm.position, dir = -arm.forward;
            int n = Physics.SphereCastNonAlloc(pivot, collisionRadius, dir, hits, zoomedDistance, collisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                if (h.distance <= 0f && h.point == Vector3.zero) continue;   // empezó dentro del collider: no sirve para medir
                if (player != null && h.collider.transform.IsChildOf(player)) continue;
                allowed = Mathf.Min(allowed, Mathf.Max(minCollisionDistance, h.distance));
            }
        }

        if (allowed < currentDistance - 0.01f)
        {
            // obstáculo: se acerca rápido para no atravesarlo
            clearTimer = returnDelay;
            currentDistance = Mathf.SmoothDamp(currentDistance, allowed, ref distVel, pullInSmoothTime, Mathf.Infinity, dt);
            if (currentDistance > allowed + 0.15f) { currentDistance = allowed + 0.15f; distVel = 0f; }   // el radio de la esfera cubre estos 15 cm
        }
        else
        {
            // sin obstáculo: espera un poco y regresa suave (amortigua el "salto" al rozar paredes)
            clearTimer -= dt;
            if (clearTimer <= 0f) currentDistance = Mathf.SmoothDamp(currentDistance, allowed, ref distVel, returnSmoothTime, Mathf.Infinity, dt);
            else distVel = 0f;
        }

        var lp = cam.transform.localPosition;
        cam.transform.localPosition = new Vector3(lp.x, lp.y, -currentDistance);
    }

    static void Lock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
