using UnityEngine;

/// <summary>
/// Traduce la dirección del mundo a una de 8 direcciones EN PANTALLA (relativa a la cámara)
/// y se la pasa al Animator. Así, al girar la cámara 90°, el personaje se ve girar de verdad.
/// </summary>
public class SpriteDirectionAnimator : MonoBehaviour
{
    [SerializeField] PlayerMotor motor;
    [SerializeField] CameraRig cameraRig;
    [SerializeField] Animator animator;

    static readonly int DirectionHash = Animator.StringToHash("Direction");
    static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    // Orden = umbrales del Blend Tree 1D:
    // 0 Down | 1 DownLeft | 2 Left | 3 UpLeft | 4 Up | 5 UpRight | 6 Right | 7 DownRight

    void LateUpdate()
    {
        // x = derecha en pantalla, z = "hacia arriba" en pantalla (alejándose de la cámara)
        Vector3 local = cameraRig.transform.InverseTransformDirection(motor.WorldMoveDirection);
        float angle = Mathf.Atan2(local.x, -local.z) * Mathf.Rad2Deg; // 0 = abajo, 90 = derecha

        int index = Mathf.RoundToInt(-angle / 45f);
        index = ((index % 8) + 8) % 8;

        animator.SetFloat(DirectionHash, index);
        animator.SetBool(IsMovingHash, motor.IsMoving);
    }
}
