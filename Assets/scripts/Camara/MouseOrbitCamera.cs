using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Estilos de cámara que se pueden probar en juego con F1–F5 (o Select/Back del control).</summary>
public enum CameraStyle { Original, Pulido, TerceraPersona, Isometrica, Sala, Diorama }

/// <summary>Valores de un estilo de cámara.</summary>
[System.Serializable]
public class CameraStyleProfile
{
    [Tooltip("Campo de visión vertical (grados).")]
    public float fov = 38f;
    [Tooltip("Distancia de la cámara al pivote (m).")]
    public float distance = 10f;
    [Tooltip("Altura del pivote sobre los pies de Abby (m).")]
    public float pivotHeight = 0.6f;
    [Tooltip("Inclinación con la que entra el estilo (grados, positivo = mira hacia abajo).")]
    public float startPitch = 34f;
    public float minPitch = 20f;
    public float maxPitch = 52f;
    [Tooltip("Grados que la cámara levanta la vista sobre Abby.")]
    public float lookUp = 5f;
    [Tooltip("Metros que la cámara se adelanta hacia donde camina Abby. 0 = siempre centrada.")]
    public float lookAhead = 0.8f;
    [Tooltip("Gira libre con el mouse/stick. Apagado = solo gira en pasos con Q/E (LB/RB).")]
    public bool freeOrbit = true;
    [Tooltip("Grados por paso con Q/E cuando no es órbita libre.")]
    public float yawStep = 45f;
    [Tooltip("Suavizado del giro (s). Bajo = responde al instante.")]
    public float rotationSmoothTime = 0.04f;
    [Tooltip("Sin órbita libre: mantener clic derecho (o mover el stick derecho) deja asomarse con el mouse; al soltar vuelve a su ángulo.")]
    public bool peekHold = false;
    [Tooltip("Encuadra la sala o pasillo donde está Abby: la cámara no se sale de sus bordes (no se ve el vacío de afuera).")]
    public bool roomFraming = false;
    [Tooltip("Cámara desde adentro: choca con muros, techo y objetos grandes (se acerca) en vez de que se vuelvan invisibles.")]
    public bool interior = false;
    [Tooltip("Nada se oculta (casas, árboles, columnas): en la maqueta lo de enfrente solo se desenfoca.")]
    public bool keepOccluders = false;
    [Tooltip("Usa las tomas de cámara del mapa (CameraShot): encuadres que lucen las vistas al pasar por ciertos lugares.")]
    public bool allowShots = false;
}

/// <summary>
/// Cámara orbital con el mouse (y stick derecho del control): gira libre alrededor de Abby
/// en vez de los saltos de 90° con Q/E. Va en el mismo objeto que CameraRig (que sigue al jugador),
/// y mueve el yaw del rig y el pitch del CameraArm.
/// Estilos (F1–F4 en juego, se recuerda entre escenas):
///  - Original: los valores de la escena, tal cual.
///  - Pulido: mismo estilo con FOV más abierto, pivote a media altura y algo de mirada adelante.
///  - Tercera persona: más cerca y baja, mira hacia donde caminas; desde adentro de la sala (choca con muros y techo,
///    nada se vuelve invisible).
///  - Isométrica: alta y fija en diagonal; Q/E (LB/RB) la giran 45°.
///  - Sala (HD-2D): ángulo fijo que encuadra la sala/pasillo actual (AreaReveal) sin salirse de sus bordes;
///    Q/E giran 90° y con clic derecho sostenido (o el stick derecho) te asomas para buscar secretos.
///  - Diorama (F6): lente larga desde lejos y en alto, como maqueta (Octopath); con TiltShiftFocus desenfoca lo cercano y lo lejano.
/// Opcional (apagado por defecto, se activa por escena):
///  - Zoom con la rueda del mouse / gatillos del control (LT acerca, RT aleja).
///  - Colisión: si entre Abby y la cámara hay suelo o pared, la cámara se acerca (SphereCast) y luego regresa suave.
/// Cursor: se oculta y queda fijo al hacer clic en el juego; Esc lo libera.
/// Para quitar Q/E del CameraRig deja vacío su campo Input en la escena.
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

    [Header("Inclinación (grados, positivo = mira hacia abajo) — estilo Original")]
    public float minPitch = -5f;
    public float maxPitch = 40f;
    [Tooltip("Grados que la cámara levanta la vista sobre Abby: ella queda más abajo en pantalla y se ve más de lo que hay adelante. 0 = centrada.")]
    public float lookUp = 0f;

    // Estilos (F1 Original, F2 Pulido, F3 Tercera persona, F4 Isométrica, F5 Sala): valores en código, no en la escena,
    // para que un cambio aquí llegue a todos los pisos sin volver a guardarlos.
    [System.NonSerialized] CameraStyleProfile pulido = new CameraStyleProfile();
    [System.NonSerialized] CameraStyleProfile terceraPersona = new CameraStyleProfile
    {
        fov = 48f, distance = 6.5f, pivotHeight = 1f, startPitch = 14f, minPitch = -18f, maxPitch = 42f,
        lookUp = 4f, lookAhead = 1.6f, freeOrbit = true, rotationSmoothTime = 0.04f, interior = true
    };
    [System.NonSerialized] CameraStyleProfile isometrica = new CameraStyleProfile
    {
        fov = 32f, distance = 17f, pivotHeight = 0.5f, startPitch = 52f, minPitch = 52f, maxPitch = 52f,
        lookUp = 0f, lookAhead = 1.2f, freeOrbit = false, yawStep = 45f, rotationSmoothTime = 0.22f, peekHold = true
    };
    [System.NonSerialized] CameraStyleProfile sala = new CameraStyleProfile
    {
        fov = 32f, distance = 19f, pivotHeight = 0.6f, startPitch = 48f, minPitch = 48f, maxPitch = 48f,
        lookUp = 2f, lookAhead = 1.4f, freeOrbit = false, yawStep = 90f, rotationSmoothTime = 0.25f,
        peekHold = true, roomFraming = true
    };
    [System.NonSerialized] CameraStyleProfile diorama = new CameraStyleProfile
    {
        fov = 22f, distance = 36f, pivotHeight = 0.8f, startPitch = 20f, minPitch = 20f, maxPitch = 20f,
        lookUp = 6f, lookAhead = 1.6f, freeOrbit = false, yawStep = 45f, rotationSmoothTime = 0.3f, peekHold = true, keepOccluders = true, allowShots = true
    };
    [Tooltip("Esta escena arranca con su propio estilo (p. ej. el diorama), sin cambiar el que se guardó para los pisos.")]
    [SerializeField] bool useSceneStyle = false;
    [Tooltip("Escena al aire libre (diorama): con la cámara desde adentro (F3) solo choca con el terreno; barandales, postes, casas y árboles no la acercan.")]
    [SerializeField] bool outdoorScene = false;
    [SerializeField] CameraStyle sceneStyle = CameraStyle.Diorama;
    bool sceneStyleActive;
    [Tooltip("Qué tan rápido pasa de un estilo a otro (s).")]
    [SerializeField] float styleBlendTime = 0.35f;
    [Tooltip("Suavizado de la mirada adelante (s). Alto = la cámara tarda más en adelantarse.")]
    [SerializeField] float lookAheadSmoothTime = 0.45f;
    [Tooltip("Inclinación mínima y máxima al asomarse con clic derecho (grados).")]
    [SerializeField] Vector2 peekPitchRange = new Vector2(8f, 70f);
    [Tooltip("Suavizado con el que la cámara de Sala sigue el encuadre (s). Alto = se desliza más lento al cambiar de sala.")]
    [SerializeField] float framingSmoothTime = 0.3f;
    [Tooltip("Qué parte de la pantalla puede quedar fuera de la sala por los lados (0 = nada, 1 = media pantalla).")]
    [Range(0f, 1f)] [SerializeField] float framingSideSlack = 0.15f;

    /// <summary>Estilo activo (se guarda en PlayerPrefs para que todos los pisos usen el mismo).</summary>
    public static CameraStyle Style
    {
        get { if (!styleLoaded) { style = (CameraStyle)Mathf.Clamp(PlayerPrefs.GetInt(StylePref, 0), 0, StyleCount - 1); styleLoaded = true; } return style; }
        set { style = value; styleLoaded = true; PlayerPrefs.SetInt(StylePref, (int)value); }
    }
    const string StylePref = "CamStyle";
    const int StyleCount = 6;
    static CameraStyle style;
    static bool styleLoaded;

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
    [Tooltip("Si tiene nombres, solo bloquean la cámara los colliders que cuelgan de un objeto con alguno de esos nombres " +
             "(p. ej. el suelo y la torre). Vacío = bloquea todo. Por nombre para que sobreviva a regenerar la escena.")]
    [SerializeField] string[] onlyBlockUnder = new string[0];
    [Tooltip("Colliders más chicos que esto (su lado más largo, en m) no bloquean: postes, braseros, rocas…")]
    [SerializeField] float minBlockerSize = 0f;
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

    float yaw, pitch, shownYaw, shownPitch, yawVel, pitchVel;
    Camera cam;
    WallCutaway cutaway;
    float targetDistance, zoomedDistance, zoomVel, currentDistance, distVel, clearTimer;
    Transform player;
    readonly RaycastHit[] hits = new RaycastHit[16];

    CameraStyleProfile original, active;
    CameraStyle appliedStyle = (CameraStyle)(-1);
    Vector3 originalArmPos;
    float shownFov, fovVel, shownPivot, pivotVel, shownLookUp, lookUpVel;
    Vector3 lastPlayerPos, playerVel, lookOffset, lookOffsetVel;
    Vector3 focus, focusVel;
    // tomas de cámara (CameraShot): valores de la toma actual que se mezclan con la cámara normal
    CameraShot shot;
    float shotW, shotWVel, sYaw, sPitch, sDist, sFov, sLookUp, sYawVel, sPitchVel, sDistVel, sFovVel, sLookVel;
    Vector3 sOffset, sOffsetVel;
    float moveYaw;   // yaw del rig (marco de los controles): no cambia mientras se sostiene la misma dirección
    IMoveInput moveInput;
    Vector2 heldInput;
    Vector2 fxzVel;
    AreaReveal currentArea;
    bool peeking;
    float labelUntil;

    void Start()
    {
        if (arm == null && transform.childCount > 0) arm = transform.GetChild(0);
        yaw = shownYaw = transform.eulerAngles.y;
        pitch = shownPitch = arm != null ? Mathf.DeltaAngle(0f, arm.localEulerAngles.x) : 10f;
        if (arm != null && pivotHeight > 0f) arm.localPosition = new Vector3(0f, pivotHeight, 0f);
        if (arm != null) originalArmPos = arm.localPosition;
        cam = GetComponentInChildren<Camera>();
        if (cam != null) cutaway = cam.GetComponent<WallCutaway>() ?? cam.gameObject.AddComponent<WallCutaway>();
        if (cam != null)
        {
            targetDistance = zoomedDistance = currentDistance = Mathf.Max(0.1f, -cam.transform.localPosition.z);
            if (zoomEnabled) targetDistance = zoomedDistance = currentDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
        }

        // el estilo Original son los valores que trae la escena
        original = new CameraStyleProfile
        {
            fov = cam != null ? cam.fieldOfView : 60f, distance = targetDistance, pivotHeight = originalArmPos.y,
            startPitch = pitch, minPitch = minPitch, maxPitch = maxPitch, lookUp = lookUp,
            lookAhead = 0f, freeOrbit = true, rotationSmoothTime = 0f
        };
        shownFov = original.fov; shownPivot = original.pivotHeight; shownLookUp = lookUp;

        var p = GameObject.FindWithTag("Player");
        if (p != null) { player = p.transform; lastPlayerPos = player.position; moveInput = p.GetComponent<IMoveInput>(); }
        sceneStyleActive = useSceneStyle;
        ApplyStyle(sceneStyleActive ? sceneStyle : Style, instant: true);
        labelUntil = Time.unscaledTime + 4f;   // recuerda las teclas al entrar a la escena
        if (lockCursor) Lock(true);
    }

    void OnEnable()
    {
        // al volver (p. ej. tras la cinemática) parte de donde quedó la cámara
        if (cam != null) currentDistance = zoomedDistance = Mathf.Max(0.1f, -cam.transform.localPosition.z);
        shownYaw = yaw; shownPitch = pitch; yawVel = pitchVel = 0f;
        if (player != null) lastPlayerPos = player.position;
    }

    void OnDisable()
    {
        Lock(false);
        OccluderFade.Suspended = false;
    }

    CameraStyleProfile ProfileFor(CameraStyle s) =>
        s == CameraStyle.Pulido ? pulido :
        s == CameraStyle.TerceraPersona ? terceraPersona :
        s == CameraStyle.Isometrica ? isometrica :
        s == CameraStyle.Sala ? sala :
        s == CameraStyle.Diorama ? diorama : original;

    /// <summary>Estilo que el jugador eligió con las teclas: se guarda y deja de mandar el estilo propio de la escena.</summary>
    void ChooseStyle(CameraStyle s)
    {
        sceneStyleActive = false;
        ApplyStyle(s, false);
    }

    void ApplyStyle(CameraStyle s, bool instant)
    {
        if (!sceneStyleActive) Style = s;
        appliedStyle = s;
        active = ProfileFor(s);
        if (active == null) active = original;

        pitch = Mathf.Clamp(active.startPitch, active.minPitch, active.maxPitch);
        if (!active.freeOrbit && active.yawStep > 0f) yaw = Mathf.Round(yaw / active.yawStep) * active.yawStep;
        peeking = false;
        targetDistance = zoomEnabled ? Mathf.Clamp(active.distance, minDistance, maxDistance) : active.distance;
        // desde adentro nada se oculta: la cámara se acerca al chocar
        OccluderFade.Suspended = active.interior || active.keepOccluders;
        if (cutaway != null) cutaway.enabled = !active.interior;
        labelUntil = Time.unscaledTime + 2.5f;

        if (instant)
        {
            shownYaw = yaw; shownPitch = pitch; moveYaw = yaw;
            shownFov = active.fov; shownPivot = active.pivotHeight; shownLookUp = active.lookUp;
            zoomedDistance = currentDistance = targetDistance;
            if (cam != null) cam.fieldOfView = shownFov;
        }
    }

    void Update()
    {
        var mouse = Mouse.current;
        var kb = Keyboard.current;
        var pad = Gamepad.current;

        if (kb != null)
        {
            if (kb.f1Key.wasPressedThisFrame) ChooseStyle(CameraStyle.Original);
            else if (kb.f2Key.wasPressedThisFrame) ChooseStyle(CameraStyle.Pulido);
            else if (kb.f3Key.wasPressedThisFrame) ChooseStyle(CameraStyle.TerceraPersona);
            else if (kb.f4Key.wasPressedThisFrame) ChooseStyle(CameraStyle.Isometrica);
            else if (kb.f5Key.wasPressedThisFrame) ChooseStyle(CameraStyle.Sala);
            else if (kb.f6Key.wasPressedThisFrame) ChooseStyle(CameraStyle.Diorama);
        }
        if (pad != null && pad.selectButton.wasPressedThisFrame) ChooseStyle((CameraStyle)(((int)appliedStyle + 1) % StyleCount));
        if (!sceneStyleActive && appliedStyle != Style) ApplyStyle(Style, false);   // otra escena/objeto lo cambió

        if (lockCursor)
        {
            if (kb != null && kb.escapeKey.wasPressedThisFrame) Lock(false);
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) Lock(true);
        }

        Vector2 look = Vector2.zero;
        bool mouseActive = mouse != null && (!lockCursor || Cursor.lockState == CursorLockMode.Locked);
        if (mouseActive)
            look += mouse.delta.ReadValue() * mouseSensitivity;
        Vector2 stick = pad != null ? pad.rightStick.ReadValue() : Vector2.zero;
        look += stick * stickSpeed * Time.deltaTime;

        bool wasPeeking = peeking;
        peeking = !active.freeOrbit && active.peekHold &&
                  ((mouseActive && mouse.rightButton.isPressed) || stick.sqrMagnitude > 0.02f);

        if (active.freeOrbit || peeking)
        {
            yaw += look.x;
            pitch += invertY ? look.y : -look.y;
        }
        else
        {
            // ángulo fijo: el mouse no la mueve; Q/E (LB/RB) la giran un paso
            if ((kb != null && kb.qKey.wasPressedThisFrame) || (pad != null && pad.leftShoulder.wasPressedThisFrame)) yaw -= active.yawStep;
            if ((kb != null && kb.eKey.wasPressedThisFrame) || (pad != null && pad.rightShoulder.wasPressedThisFrame)) yaw += active.yawStep;
            if (wasPeeking)
            {
                // al soltar regresa a su ángulo, al paso de giro más cercano
                if (active.yawStep > 0f) yaw = Mathf.Round(yaw / active.yawStep) * active.yawStep;
                pitch = active.startPitch;
            }
        }
        pitch = peeking ? Mathf.Clamp(pitch, peekPitchRange.x, peekPitchRange.y) : Mathf.Clamp(pitch, active.minPitch, active.maxPitch);

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
        if (cam == null || arm == null) return;
        float dt = Time.unscaledDeltaTime;
        float gdt = Time.deltaTime;

        UpdatePlayerVelocity(gdt);

        float rotSmooth = peeking ? 0.04f : active.rotationSmoothTime;
        if (rotSmooth > 0f)
        {
            shownYaw = Mathf.SmoothDampAngle(shownYaw, yaw, ref yawVel, rotSmooth, Mathf.Infinity, dt);
            shownPitch = Mathf.SmoothDampAngle(shownPitch, pitch, ref pitchVel, Mathf.Max(rotSmooth, 0.08f), Mathf.Infinity, dt);
        }
        else { shownYaw = yaw; shownPitch = pitch; yawVel = pitchVel = 0f; }

        shownFov = Mathf.SmoothDamp(shownFov, active.fov, ref fovVel, styleBlendTime, Mathf.Infinity, dt);
        shownPivot = Mathf.SmoothDamp(shownPivot, active.pivotHeight, ref pivotVel, styleBlendTime, Mathf.Infinity, dt);
        shownLookUp = Mathf.SmoothDamp(shownLookUp, active.lookUp, ref lookUpVel, styleBlendTime, Mathf.Infinity, dt);

        // tomas del mapa: la vista se va a un encuadre armado y la cámara normal queda debajo
        float viewYaw = shownYaw, viewPitch = shownPitch, viewFov = shownFov, viewLookUp = shownLookUp;
        UpdateShot(dt);
        float w = shotW * shotW * (3f - 2f * shotW);
        // Q/E durante una toma giran la toma también (p. ej. en el puente, para ver el otro lado)
        float shotTurn = shot != null ? Mathf.DeltaAngle(shotBaseYaw, shownYaw) : 0f;
        if (w > 0.0001f)
        {
            viewYaw = Mathf.LerpAngle(shownYaw, sYaw + shotTurn, w);
            viewPitch = Mathf.Lerp(shownPitch, sPitch, w);
            viewFov = Mathf.Lerp(shownFov, sFov, w);
            viewLookUp = Mathf.Lerp(shownLookUp, sLookUp, w);
            if (!zoomEnabled) targetDistance = Mathf.Lerp(active.distance, sDist, w);
        }
        else if (!zoomEnabled && active.allowShots) targetDistance = active.distance;
        // si una toma gira la cámara mientras sostienes una dirección, Abby sigue derecho (el marco de los
        // controles se queda); en cuanto sueltas o cambias de dirección, los controles se alinean con la vista
        Vector2 inp = moveInput != null ? moveInput.Move : Vector2.zero;
        bool sameInput = inp.sqrMagnitude > 0.04f && heldInput.sqrMagnitude > 0.04f && Vector2.Angle(inp, heldInput) < 25f;
        if (!active.allowShots || !sameInput) { moveYaw = viewYaw; heldInput = inp; }

        // mirada adelante: el pivote se adelanta hacia donde camina Abby
        Vector3 wantOffset = active.lookAhead > 0f ? Vector3.ClampMagnitude(playerVel / 5f, 1f) * active.lookAhead : Vector3.zero;
        lookOffset = Vector3.SmoothDamp(lookOffset, wantOffset, ref lookOffsetVel, lookAheadSmoothTime, Mathf.Infinity, gdt);

        transform.rotation = Quaternion.Euler(0f, moveYaw, 0f);
        arm.localRotation = Quaternion.Euler(0f, Mathf.DeltaAngle(moveYaw, viewYaw), 0f) * Quaternion.Euler(viewPitch, 0f, 0f);
        if (active.roomFraming && player != null)
        {
            // Sala: el pivote sigue a Abby pero sin dejar que la vista se salga de la sala/pasillo donde está
            Vector3 want = FrameInRoom(player.position + lookOffset, player.position);
            Vector2 fxz = Vector2.SmoothDamp(new Vector2(focus.x, focus.z), new Vector2(want.x, want.z), ref fxzVel, framingSmoothTime, Mathf.Infinity, gdt);
            float fy = Mathf.SmoothDamp(focus.y, player.position.y, ref focusVel.y, 0.3f, Mathf.Infinity, gdt);   // los saltos casi no la mueven
            focus = new Vector3(fxz.x, fy, fxz.y);
            Vector3 local = transform.InverseTransformPoint(focus);
            arm.localPosition = new Vector3(local.x, local.y + shownPivot, local.z);
        }
        else
        {
            arm.localPosition = new Vector3(originalArmPos.x, shownPivot, originalArmPos.z) + transform.InverseTransformDirection(lookOffset + Quaternion.Euler(0f, shotTurn, 0f) * sOffset * w);
            focus = transform.TransformPoint(new Vector3(arm.localPosition.x, arm.localPosition.y - shownPivot, arm.localPosition.z));
            fxzVel = Vector2.zero; focusVel = Vector3.zero;
        }
        cam.transform.localRotation = Quaternion.Euler(-viewLookUp, 0f, 0f);
        cam.fieldOfView = viewFov;

        float smooth = zoomEnabled ? zoomSmoothTime : styleBlendTime;
        zoomedDistance = Mathf.SmoothDamp(zoomedDistance, targetDistance, ref zoomVel, smooth, Mathf.Infinity, dt);
        float allowed = zoomedDistance;

        if (collisionEnabled || active.interior)
        {
            Vector3 pivot = arm.position, dir = -arm.forward;
            int n = Physics.SphereCastNonAlloc(pivot, collisionRadius, dir, hits, zoomedDistance, collisionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                if (h.distance <= 0f && h.point == Vector3.zero) continue;   // empezó dentro del collider: no sirve para medir
                if (player != null && h.collider.transform.IsChildOf(player)) continue;
                if (!Blocks(h.collider)) continue;
                if (!collisionEnabled && !Structural(h.collider)) continue;   // estilo interior en los pisos: solo muros, techos y cosas grandes
                allowed = Mathf.Min(allowed, Mathf.Max(minCollisionDistance, h.distance));
            }
        }

        if (allowed < currentDistance - 0.01f)
        {
            // obstáculo (o el estilo pide estar más cerca): se acerca rápido para no atravesarlo
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

    /// <summary>Busca la toma del lugar donde está Abby y acerca suavemente los valores de la toma; shotW sube o baja.</summary>
    void UpdateShot(float dt)
    {
        // candidata: la toma del lugar; ninguna mientras hay pelea (que no estorbe al combatir)
        var cand = active.allowShots && !peeking && player != null && RoomEncounter.ActiveCombats == 0 ? CameraShot.At(player.position) : null;
        stillTime = playerVel.sqrMagnitude < 0.09f ? stillTime + dt : 0f;
        CameraShot next;
        if (cand == null || cand == shot) { pending = null; next = cand; }
        else
        {
            // otra toma: hay que estar un rato dentro (y quieto si es de vista) y no recién salido de otra
            if (cand != pending) { pending = cand; pendingTime = 0f; }
            pendingTime += dt;
            bool ready = cand.introSeconds > 0f ||
                         (pendingTime >= ShotEnterDelay && (!cand.whenStill || stillTime >= ShotStillDelay) && Time.unscaledTime >= shotCooldownUntil);
            next = ready ? cand : (shot != null && !leaving && shot.Contains(player.position) ? shot : null);
        }
        if (next == null && shot != null && !leaving) shotCooldownUntil = Time.unscaledTime + ShotCooldown;
        if (next != null && next != shot)
        {
            if (shot == null || shotW < 0.01f)
            {
                // entrando desde la cámara normal: la toma arranca en su encuadre y el peso hace la mezcla
                sYaw = next.yaw; sPitch = next.pitch; sDist = next.distance; sFov = next.fov; sLookUp = next.lookUp; sOffset = next.offset;
                sYawVel = sPitchVel = sDistVel = sFovVel = sLookVel = 0f; sOffsetVel = Vector3.zero;
                shotBaseYaw = yaw;
            }
            if (!next.shown && !string.IsNullOrEmpty(next.title))
            {
                next.shown = true;
                FloorDirector.Banner(next.title, next.subtitle, new Color(1f, 0.9f, 0.65f), 3.2f);
            }
            shot = next;
        }
        leaving = next == null;   // al salir, se queda la última toma mientras se desvanece
        float blend = shot != null ? Mathf.Max(0.2f, shot.blendTime) : 1f;
        float targetW = shot != null && !leaving ? 1f : 0f;
        shotW = Mathf.SmoothDamp(shotW, targetW, ref shotWVel, blend * 0.45f, Mathf.Infinity, dt);
        if (shotW < 0.002f && leaving) { shot = null; leaving = false; shotW = 0f; }
        if (shot == null) return;
        float t = blend * 0.45f;
        sYaw = Mathf.SmoothDampAngle(sYaw, shot.yaw, ref sYawVel, t, Mathf.Infinity, dt);
        sPitch = Mathf.SmoothDamp(sPitch, shot.pitch, ref sPitchVel, t, Mathf.Infinity, dt);
        sDist = Mathf.SmoothDamp(sDist, shot.distance, ref sDistVel, t, Mathf.Infinity, dt);
        sFov = Mathf.SmoothDamp(sFov, shot.fov, ref sFovVel, t, Mathf.Infinity, dt);
        sLookUp = Mathf.SmoothDamp(sLookUp, shot.lookUp, ref sLookVel, t, Mathf.Infinity, dt);
        sOffset = Vector3.SmoothDamp(sOffset, shot.offset, ref sOffsetVel, t, Mathf.Infinity, dt);
    }

    bool leaving;
    float shotBaseYaw;   // yaw de la cámara normal al entrar a la toma (lo girado después se suma a la toma)
    CameraShot pending;
    float pendingTime, stillTime, shotCooldownUntil;
    const float ShotEnterDelay = 0.5f, ShotStillDelay = 0.6f, ShotCooldown = 2.5f;

    /// <summary>Mueve el punto que mira la cámara para que lo visible no se salga de la sala/pasillo donde está Abby.</summary>
    Vector3 FrameInRoom(Vector3 target, Vector3 playerPos)
    {
        var area = AreaAt(playerPos);
        if (area == null) return target;

        // cuánto suelo se ve alrededor del punto enfocado: a lo ancho, hacia la cámara y hacia el fondo
        float d = currentDistance, theta = (shownPitch - shownLookUp) * Mathf.Deg2Rad, v = shownFov * 0.5f * Mathf.Deg2Rad;
        float h = d * Mathf.Sin(theta), b = d * Mathf.Cos(theta);
        float halfW = d * Mathf.Tan(v) * cam.aspect;
        float halfNear = b - h / Mathf.Tan(Mathf.Min(theta + v, 89f * Mathf.Deg2Rad));
        float farA = theta - v;
        float halfFar = farA > 5f * Mathf.Deg2Rad ? h / Mathf.Tan(farA) - b : 100f;

        Vector3 R = transform.right, F = transform.forward;
        float minR = float.MaxValue, maxR = float.MinValue, minF = float.MaxValue, maxF = float.MinValue;
        for (int i = 0; i < 4; i++)
        {
            var c = area.transform.TransformPoint(new Vector3(((i & 1) == 0 ? -0.5f : 0.5f) * area.size.x, 0f, ((i & 2) == 0 ? -0.5f : 0.5f) * area.size.y));
            float r = Vector3.Dot(c, R), f = Vector3.Dot(c, F);
            minR = Mathf.Min(minR, r); maxR = Mathf.Max(maxR, r);
            minF = Mathf.Min(minF, f); maxF = Mathf.Max(maxF, f);
        }

        float tR = Vector3.Dot(target, R), tF = Vector3.Dot(target, F);
        float pR = Vector3.Dot(playerPos, R), pF = Vector3.Dot(playerPos, F);

        // a lo ancho: si la sala cabe, centrada; si no, sigue a Abby sin enseñar más allá de los muros
        float side = halfW * (1f - framingSideSlack);
        float lo = minR + side, hi = maxR - side;
        float nR = lo > hi ? (minR + maxR) * 0.5f : Mathf.Clamp(tR, lo, hi);

        // de frente: el borde cercano (su muro se oculta) no entra en pantalla; hacia el fondo puede llegar casi al muro
        lo = minF + halfNear * 0.95f; hi = maxF - Mathf.Min(halfFar * 0.15f, 2f);
        float nF = lo > hi ? lo : Mathf.Clamp(tF, lo, hi);

        // Abby siempre bien dentro de la pantalla
        nR = Mathf.Clamp(nR, pR - halfW * 0.7f, pR + halfW * 0.7f);
        nF = Mathf.Clamp(nF, pF - halfFar * 0.35f, pF + halfNear * 0.7f);

        return target + R * (nR - tR) + F * (nF - tF);
    }

    /// <summary>Sala/pasillo donde está Abby: el más chico que la contiene (en las puertas gana el pasillo). Se queda con la actual mientras siga dentro.</summary>
    AreaReveal AreaAt(Vector3 p)
    {
        if (currentArea != null && currentArea.isActiveAndEnabled && currentArea.Contains(p, 0.4f)) return currentArea;
        AreaReveal best = null; float bestSize = float.MaxValue;
        foreach (var a in AreaReveal.All)
        {
            if (a == null || !a.Contains(p, 0f)) continue;
            float s = a.size.x * a.size.y;
            if (s < bestSize) { bestSize = s; best = a; }
        }
        if (best != null) { currentArea = best; return best; }
        return currentArea != null && currentArea.Contains(p, 2f) ? currentArea : null;
    }

    void UpdatePlayerVelocity(float dt)
    {
        if (player == null || dt <= 0f) return;
        Vector3 v = (player.position - lastPlayerPos) / dt;
        lastPlayerPos = player.position;
        v.y = 0f;
        if (v.sqrMagnitude > 30f * 30f) v = Vector3.zero;   // teletransporte (cambio de sala, respawn): no cuenta
        playerVel = Vector3.Lerp(playerVel, v, 1f - Mathf.Exp(-12f * dt));
    }

    static readonly string[] styleNames = { "Original", "Pulido", "Tercera persona", "Isométrica", "Sala (HD-2D)", "Diorama" };
    GUIStyle labelStyle;

    void OnGUI()
    {
        float left = labelUntil - Time.unscaledTime;
        if (left <= 0f) return;
        if (labelStyle == null)
            labelStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 20, fontStyle = FontStyle.Bold };
        float a = Mathf.Clamp01(left / 0.5f);
        string extra = active != null && !active.freeOrbit ? "   ·   Q/E giran · clic derecho para asomarte" : "";
        string text = "Cámara: " + styleNames[(int)appliedStyle] + extra + "\nF1 Original · F2 Pulido · F3 Tercera persona · F4 Isométrica · F5 Sala · F6 Diorama";
        var r = new Rect(0f, Screen.height * 0.08f, Screen.width, 60f);
        GUI.color = new Color(0f, 0f, 0f, 0.7f * a);
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, labelStyle);
        GUI.color = new Color(1f, 1f, 1f, a);
        GUI.Label(r, text, labelStyle);
        GUI.color = Color.white;
    }

    readonly System.Collections.Generic.Dictionary<Collider, bool> structCache = new System.Collections.Generic.Dictionary<Collider, bool>();

    /// <summary>¿Es parte del edificio (muro, techo, suelo, pilar, librero…)? Enemigos, objetos que se mueven y cosas chicas no acercan la cámara.</summary>
    bool Structural(Collider c)
    {
        if (structCache.TryGetValue(c, out bool b)) return b;
        if (outdoorScene)
        {
            // al aire libre solo el terreno acerca la cámara: lo demás se ve de cerca sin pegarse a Abby
            b = c is MeshCollider && c.name.StartsWith("Terreno");
            structCache[c] = b;
            return b;
        }
        Vector3 s = c.bounds.size;
        b = c.attachedRigidbody == null && !(c is CharacterController) && c.GetComponentInParent<TowerEnemy>() == null
            && Mathf.Max(s.x, Mathf.Max(s.y, s.z)) >= 1.2f;
        structCache[c] = b;
        return b;
    }

    readonly System.Collections.Generic.Dictionary<Collider, bool> blockCache = new System.Collections.Generic.Dictionary<Collider, bool>();

    /// <summary>¿Este collider debe acercar la cámara? (se calcula una vez por collider)</summary>
    bool Blocks(Collider c)
    {
        if (blockCache.TryGetValue(c, out bool b)) return b;
        b = true;
        if (minBlockerSize > 0f)
        {
            Vector3 s = c.bounds.size;
            b = Mathf.Max(s.x, Mathf.Max(s.y, s.z)) >= minBlockerSize;
        }
        if (b && onlyBlockUnder != null && onlyBlockUnder.Length > 0)
        {
            b = false;
            for (var t = c.transform; t != null && !b; t = t.parent)
                foreach (var n in onlyBlockUnder)
                    if (t.name == n) { b = true; break; }
        }
        blockCache[c] = b;
        return b;
    }

    static void Lock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
