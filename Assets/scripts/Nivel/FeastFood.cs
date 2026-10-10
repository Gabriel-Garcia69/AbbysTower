using UnityEngine;

/// <summary>
/// GULA — Comida del banquete: cura, pero empacha (Abby queda lenta unos segundos). Cuanta más comes seguida,
/// más dura el empacho. Decide si de verdad necesitas comer.
/// </summary>
public class FeastFood : MonoBehaviour
{
    public float heal = 14f;
    public float radius = 0.9f;

    static float lastEat = -99f;
    static int streak;

    void Update()
    {
        float d = Hazards.FlatDistance(transform.position, out bool same);
        if (d > radius || !same) return;
        var h = Hazards.Health;
        if (h == null || h.IsDead) return;
        streak = Time.time - lastEat < 8f ? streak + 1 : 1;
        lastEat = Time.time;
        h.Heal(heal);
        float secs = 1.5f + streak * 1.2f;
        Hazards.Motor?.Slow(0.6f, secs);
        FloorDirector.Popup(transform.position + Vector3.up * 1.5f, streak >= 3 ? "¡EMPACHO TOTAL!" : "Empacho...", new Color(1f, 0.75f, 0.35f), 1.4f);
        CombatFx.Burst(transform.position + Vector3.up * 0.3f, new Color(1f, 0.8f, 0.4f), 16, 2.5f, 0.1f, 0.5f);
        gameObject.SetActive(false);
    }
}
