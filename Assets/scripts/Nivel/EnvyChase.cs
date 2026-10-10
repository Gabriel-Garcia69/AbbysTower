using UnityEngine;

/// <summary>ENVIDIA — Mientras Abby recorre esta zona (el laberinto de espejos), La Otra la sigue; al salir se desvanece.</summary>
public class EnvyChase : MonoBehaviour
{
    public Vector2 size = new Vector2(28f, 28f);
    EnvyShadow shadow;

    void Update()
    {
        var p = Hazards.Player;
        if (p == null) return;
        Vector3 lp = transform.InverseTransformPoint(p.position);
        bool inside = Mathf.Abs(lp.x) < size.x / 2f - 1f && Mathf.Abs(lp.z) < size.y / 2f - 1f && Mathf.Abs(lp.y) < 3f;
        if (inside && shadow == null) shadow = EnvyShadow.Spawn(transform);
        else if (!inside && shadow != null)
        {
            CombatFx.Burst(shadow.transform.position + Vector3.up, new Color(0.3f, 1f, 0.45f), 24, 3f, 0.12f, 0.8f, -0.2f);
            Destroy(shadow.gameObject);
            shadow = null;
        }
    }
}
