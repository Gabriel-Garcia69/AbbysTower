using UnityEngine;

/// <summary>Escondite: la primera vez que Abby entra al área, se anuncia y se cuenta en los secretos.</summary>
public class SecretSpot : MonoBehaviour
{
    public string id = "";
    public Vector2 size = new Vector2(2f, 2f);
    public string message = "Encontraste un escondite";
    bool done;

    void Update()
    {
        if (done || Hazards.Player == null) return;
        Vector3 lp = transform.InverseTransformPoint(Hazards.Player.position);
        if (Mathf.Abs(lp.x) > size.x / 2f || Mathf.Abs(lp.z) > size.y / 2f || Mathf.Abs(lp.y) > 2f) return;
        done = true;
        bool first = SecretTracker.Mark("escondite:" + id);
        FloorDirector.Banner("¡ESCONDITE!", message + (first ? "" : " (ya lo conocías)"), new Color(0.6f, 1f, 0.85f), 3f);
        CombatFx.Burst(Hazards.Player.position + Vector3.up, new Color(0.6f, 1f, 0.85f), 30, 4f, 0.14f, 0.8f, -0.2f);
    }
}
