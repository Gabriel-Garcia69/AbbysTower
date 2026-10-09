using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hitbox por OverlapBox (sin triggers ni Rigidbody). Va en la raíz del personaje.
/// Se orienta según la dirección de "facing", así funciona con la cámara rotando.
/// </summary>
public class Hitbox : MonoBehaviour
{
    public Transform owner;
    public LayerMask targetMask = ~0;
    public Vector3 offset = new Vector3(0f, 0.6f, 0.9f);       // relativo a facing (z = frente)
    public Vector3 halfExtents = new Vector3(0.7f, 0.6f, 0.8f);

    HitInfo info;
    Vector3 facing = Vector3.forward;
    bool active;
    readonly HashSet<IHittable> alreadyHit = new HashSet<IHittable>();
    readonly Collider[] buffer = new Collider[16];

    void Reset() { owner = transform; }
    void Awake() { if (owner == null) owner = transform; }

    public void Begin(HitInfo hitInfo, Vector3 facingDir)
    {
        info = hitInfo;
        facing = facingDir.sqrMagnitude > 0.001f ? facingDir.normalized : Vector3.forward;
        alreadyHit.Clear();
        active = true;
    }

    public void End() { active = false; }

    void Update()
    {
        if (!active) return;

        Quaternion rot = Quaternion.LookRotation(facing);
        Vector3 center = owner.position + rot * offset;
        int n = Physics.OverlapBoxNonAlloc(center, halfExtents, buffer, rot, targetMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < n; i++)
        {
            Collider col = buffer[i];
            if (col.transform.IsChildOf(owner)) continue;

            IHittable target = col.GetComponentInParent<IHittable>();
            if (target == null || alreadyHit.Contains(target)) continue;

            alreadyHit.Add(target);
            if (target.ReceiveHit(info)) HitStop.Do(info.hitStop);
        }
    }

    void OnDrawGizmosSelected()
    {
        Transform o = owner != null ? owner : transform;
        Quaternion rot = Quaternion.LookRotation(Application.isPlaying ? facing : o.forward);
        Gizmos.color = active ? Color.red : new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.matrix = Matrix4x4.TRS(o.position + rot * offset, rot, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
    }
}
