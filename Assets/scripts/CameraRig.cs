using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Pivote de cámara. Solo rota en Y (yaw) en saltos de 90°.
/// Jerarquía:  CameraRig (este script, SOLO yaw)
///               └─ CameraArm (rotación X = pitch, ej. 35°)
///                    └─ Main Camera (posición local 0,0,-12)
/// IMPORTANTE: el CameraRig NO es hijo del jugador; lo sigue por script.
/// </summary>
public class CameraRig : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] Transform followTarget;
    [SerializeField] PlayerInputReader input;

    [Header("Seguimiento")]
    [SerializeField] float followSmoothTime = 0.1f;

    [Header("Rotación")]
    [SerializeField] float stepDegrees = 90f;
    [SerializeField] float rotateDuration = 0.5f;
    [SerializeField] AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    public Camera Camera { get; private set; }
    public bool IsRotating { get; private set; }
    public event Action RotationFinished;

    Vector3 _followVelocity;

    void Awake()
    {
        Camera = GetComponentInChildren<Camera>();
    }

    void OnEnable()
    {
        if (input != null) input.CameraRotateRequested += HandleRotateRequest;
    }

    void OnDisable()
    {
        if (input != null) input.CameraRotateRequested -= HandleRotateRequest;
    }

    void HandleRotateRequest(int direction)
    {
        if (IsRotating) return; // ignora spam mientras gira
        StartCoroutine(RotateRoutine(direction * stepDegrees));
    }

    IEnumerator RotateRoutine(float delta)
    {
        IsRotating = true;
        float startYaw = transform.eulerAngles.y;
        float endYaw = startYaw + delta;

        for (float t = 0f; t < 1f; t += Time.deltaTime / rotateDuration)
        {
            SetYaw(Mathf.LerpUnclamped(startYaw, endYaw, ease.Evaluate(t)));
            yield return null;
        }

        SetYaw(endYaw); // snap exacto, sin drift acumulado
        IsRotating = false;
        RotationFinished?.Invoke();
    }

    void SetYaw(float yaw) => transform.rotation = Quaternion.Euler(0f, yaw, 0f);

    void LateUpdate()
    {
        if (followTarget == null) return;
        transform.position = Vector3.SmoothDamp(
            transform.position, followTarget.position, ref _followVelocity, followSmoothTime);
    }
}
