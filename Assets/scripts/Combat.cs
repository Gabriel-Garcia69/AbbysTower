using UnityEngine;

public struct HitInfo
{
    public float damage;
    public float knockback;
    public float hitStop;
    public Vector3 sourcePosition;
    public IHittable attacker;
}

public interface IHittable
{
    /// <summary>true si el golpe conectó (false = esquivado / ignorado).</summary>
    bool ReceiveHit(HitInfo hit);
    void Stagger(float duration);
}
