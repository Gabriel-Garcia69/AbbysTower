using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Input del combate (independiente de tu PlayerInputReader existente).
/// Funciona con el Input System nuevo o con el Input Manager clásico.
/// Controles: WASD/flechas/stick = mover | J / clic izq / X = atacar | Espacio / Shift / A = dash | K / clic der / LB = bloquear
/// </summary>
[DefaultExecutionOrder(-100)]
public class CombatInputReader : MonoBehaviour
{
    public Vector2 Move { get; private set; }
    public bool AttackDown { get; private set; }
    public bool DashDown { get; private set; }
    public bool BlockDown { get; private set; }
    public bool BlockHeld { get; private set; }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        Mouse ms = Mouse.current;
        Gamepad gp = Gamepad.current;

        Vector2 m = Vector2.zero;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) m.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) m.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) m.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) m.x -= 1f;
        }
        if (gp != null)
        {
            Vector2 s = gp.leftStick.ReadValue();
            if (s.sqrMagnitude > m.sqrMagnitude) m = s;
        }
        Move = Vector2.ClampMagnitude(m, 1f);

        AttackDown = (kb != null && kb.jKey.wasPressedThisFrame)
                  || (ms != null && ms.leftButton.wasPressedThisFrame)
                  || (gp != null && gp.buttonWest.wasPressedThisFrame);

        DashDown = (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.leftShiftKey.wasPressedThisFrame))
                || (gp != null && gp.buttonSouth.wasPressedThisFrame);

        BlockDown = (kb != null && kb.kKey.wasPressedThisFrame)
                 || (ms != null && ms.rightButton.wasPressedThisFrame)
                 || (gp != null && gp.leftShoulder.wasPressedThisFrame);

        BlockHeld = (kb != null && kb.kKey.isPressed)
                 || (ms != null && ms.rightButton.isPressed)
                 || (gp != null && gp.leftShoulder.isPressed);
#else
        Vector2 m = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Move = Vector2.ClampMagnitude(m, 1f);

        AttackDown = Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.JoystickButton2);
        DashDown = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.JoystickButton0);
        BlockDown = Input.GetKeyDown(KeyCode.K) || Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.JoystickButton4);
        BlockHeld = Input.GetKey(KeyCode.K) || Input.GetMouseButton(1) || Input.GetKey(KeyCode.JoystickButton4);
#endif
    }
}
