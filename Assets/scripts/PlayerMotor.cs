using UnityEngine;

/// <summary>
/// Movimiento relativo a la cámara + gravedad + salto con CharacterController.
/// No rota el objeto raíz: la "dirección a la que mira" es solo un dato (WorldMoveDirection);
/// el sprite se encarga de mostrarla.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] CameraRig cameraRig;

    [Header("Movimiento")]
    [SerializeField] float speed = 5f;
    [SerializeField] float acceleration = 45f;

    [Header("Salto y gravedad")]
    [SerializeField] float jumpHeight = 1.2f;
    [SerializeField] float gravityScale = 1.5f;
    [SerializeField] float coyoteTime = 0.1f;

    /// <summary>Última dirección de movimiento en el mundo (plano XZ). Se conserva al detenerse.</summary>
    public Vector3 WorldMoveDirection { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsGrounded { get; private set; }

    CharacterController _cc;
    IMoveInput _input;
    Vector3 _horizontalVelocity;
    float _verticalVelocity;
    float _lastGroundedTime;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _input = GetComponent<IMoveInput>();
    }

    void Start()
    {
        // Mirando hacia la cámara (= "abajo" en pantalla)
        WorldMoveDirection = -cameraRig.transform.forward;
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // El rig solo tiene yaw, así que forward/right ya son planos (no hace falta ProjectOnPlane)
        Vector2 move = _input.Move;
        Vector3 wish = cameraRig.transform.right * move.x + cameraRig.transform.forward * move.y;

        IsMoving = wish.sqrMagnitude > 0.0001f;
        if (IsMoving) WorldMoveDirection = wish.normalized;

        _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, wish * speed, acceleration * dt);

        // Suelo / coyote time / salto
        bool grounded = _cc.isGrounded;
        if (grounded)
        {
            _lastGroundedTime = Time.time;
            if (_verticalVelocity < 0f) _verticalVelocity = -2f; // mantiene pegado al suelo
        }

        bool canJump = Time.time - _lastGroundedTime <= coyoteTime;
        if (canJump && _input.ConsumeJump())
        {
            float g = Mathf.Abs(Physics.gravity.y) * gravityScale;
            _verticalVelocity = Mathf.Sqrt(2f * g * jumpHeight);
            _lastGroundedTime = float.NegativeInfinity; // evita doble salto
        }

        _verticalVelocity += Physics.gravity.y * gravityScale * dt;

        // UNA sola llamada a Move por frame
        _cc.Move((_horizontalVelocity + Vector3.up * _verticalVelocity) * dt);
        IsGrounded = _cc.isGrounded;
    }
}
