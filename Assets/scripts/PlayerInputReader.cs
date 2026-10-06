using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Contrato de input. Permite reemplazar el teclado por IA, replay o cutscenes.</summary>
public interface IMoveInput
{
    Vector2 Move { get; }
    bool ConsumeJump();
}

/// <summary>Solo lee input. No sabe nada de movimiento, cámara ni animación.</summary>
[DefaultExecutionOrder(-100)]
public class PlayerInputReader : MonoBehaviour, IMoveInput
{
    [SerializeField] float jumpBufferTime = 0.15f;

    public Vector2 Move { get; private set; }
    /// <summary>-1 = girar a la izquierda, +1 = girar a la derecha.</summary>
    public event Action<int> CameraRotateRequested;

    InputAction _move, _jump, _rotLeft, _rotRight;
    float _jumpBufferedUntil = -1f;

    void Awake()
    {
        _move = new InputAction("Move", InputActionType.Value);
        _move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        _move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        _move.AddBinding("<Gamepad>/leftStick");

        _jump = new InputAction("Jump", InputActionType.Button);
        _jump.AddBinding("<Keyboard>/space");
        _jump.AddBinding("<Gamepad>/buttonSouth");

        _rotLeft = new InputAction("CameraLeft", InputActionType.Button);
        _rotLeft.AddBinding("<Keyboard>/q");
        _rotLeft.AddBinding("<Gamepad>/leftShoulder");

        _rotRight = new InputAction("CameraRight", InputActionType.Button);
        _rotRight.AddBinding("<Keyboard>/e");
        _rotRight.AddBinding("<Gamepad>/rightShoulder");
    }

    void OnEnable()
    {
        _move.Enable(); _jump.Enable(); _rotLeft.Enable(); _rotRight.Enable();
    }

    void OnDisable()
    {
        _move.Disable(); _jump.Disable(); _rotLeft.Disable(); _rotRight.Disable();
    }

    void Update()
    {
        Move = Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f);

        if (_jump.WasPressedThisFrame())
            _jumpBufferedUntil = Time.time + jumpBufferTime;

        if (_rotLeft.WasPressedThisFrame()) CameraRotateRequested?.Invoke(-1);
        if (_rotRight.WasPressedThisFrame()) CameraRotateRequested?.Invoke(1);
    }

    /// <summary>Devuelve true una sola vez por pulsación (con buffer de salto).</summary>
    public bool ConsumeJump()
    {
        if (Time.time > _jumpBufferedUntil) return false;
        _jumpBufferedUntil = -1f;
        return true;
    }
}
