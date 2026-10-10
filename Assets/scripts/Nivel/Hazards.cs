using UnityEngine;

/// <summary>Utilidades compartidas por secretos y trampas: encontrar a Abby y lastimarla.</summary>
public static class Hazards
{
    static Transform player;
    static PlayerHealth health;
    static PlayerMotor motor;

    public static Transform Player
    {
        get
        {
            if (player == null) { var p = GameObject.FindWithTag("Player"); if (p != null) { player = p.transform; health = p.GetComponent<PlayerHealth>(); motor = p.GetComponent<PlayerMotor>(); } }
            return player;
        }
    }
    public static PlayerHealth Health { get { if (Player != null && health == null) health = player.GetComponent<PlayerHealth>(); return health; } }
    public static PlayerMotor Motor { get { if (Player != null && motor == null) motor = player.GetComponent<PlayerMotor>(); return motor; } }

    public static bool Hurt(Vector3 from, float damage, float knockback = 6f)
    {
        var h = Health;
        if (h == null || h.IsDead) return false;
        return h.ReceiveHit(new HitInfo { damage = damage, knockback = knockback, hitStop = 0.03f, sourcePosition = from });
    }

    /// <summary>Distancia horizontal de Abby a un punto, y si está a una altura parecida.</summary>
    public static float FlatDistance(Vector3 p, out bool sameLevel)
    {
        sameLevel = false;
        if (Player == null) return 999f;
        Vector3 d = player.position - p;
        sameLevel = Mathf.Abs(d.y) < 1.6f;
        d.y = 0f;
        return d.magnitude;
    }
}
