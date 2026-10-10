using UnityEngine;

/// <summary>
/// Santuario de la sala de descanso: un cristal flotante que cura a Abby por completo una sola vez
/// (solo se gasta si le falta vida). Al usarse se apaga su brillo.
/// </summary>
public class HealingShrine : MonoBehaviour
{
    public float radius = 1.8f;
    public float amount = 999f;
    public Transform crystal;
    public Light glow;

    Transform player;
    PlayerHealth health;
    bool used;
    float baseY, t, hintCooldown;

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
        if (crystal != null) baseY = crystal.localPosition.y;
    }

    void Update()
    {
        t += Time.deltaTime;
        if (crystal != null)
        {
            crystal.localPosition = new Vector3(crystal.localPosition.x, baseY + Mathf.Sin(t * 1.6f) * 0.12f, crystal.localPosition.z);
            crystal.Rotate(0f, (used ? 10f : 45f) * Time.deltaTime, 0f, Space.World);
        }
        if (glow != null) glow.intensity = Mathf.MoveTowards(glow.intensity, used ? 0.6f : 5f + Mathf.Sin(t * 3f), Time.deltaTime * 6f);
        if (hintCooldown > 0f) hintCooldown -= Time.deltaTime;

        if (used || player == null) return;
        if (health == null) health = player.GetComponent<PlayerHealth>();
        if (health == null) return;
        Vector3 d = player.position - transform.position; d.y = 0f;
        if (d.magnitude > radius) return;

        if (health.Health >= health.maxHealth)
        {
            if (hintCooldown <= 0f) { FloorDirector.Popup(transform.position + Vector3.up * 2.2f, "Vida al máximo", new Color(0.6f, 1f, 0.8f)); hintCooldown = 2f; }
            return;
        }
        used = true;
        health.Heal(amount);
        FloorDirector.Banner("SANTUARIO", "Tu vida se ha restaurado", new Color(0.5f, 1f, 0.7f), 2.2f);
    }
}
