using UnityEngine;

/// <summary>
/// Lava: si Abby cae dentro del volumen, se quema y vuelve al último suelo firme (FallGuard).
/// </summary>
public class LavaPit : MonoBehaviour
{
    public Vector3 size = new Vector3(10f, 1.5f, 10f);
    public float damage = 18f;

    void Update()
    {
        var p = Hazards.Player;
        if (p == null) return;
        Vector3 lp = transform.InverseTransformPoint(p.position);
        if (Mathf.Abs(lp.x) > size.x / 2f || Mathf.Abs(lp.z) > size.z / 2f || lp.y > size.y || lp.y < -2f) return;
        var fall = p.GetComponent<FallGuard>();
        CombatFx.Burst(p.position + Vector3.up * 0.3f, new Color(1f, 0.45f, 0.1f), 30, 5f, 0.15f, 0.6f, -0.5f);
        if (fall != null) fall.Respawn("¡Quema!", damage, new Color(1f, 0.5f, 0.2f));
    }
}
