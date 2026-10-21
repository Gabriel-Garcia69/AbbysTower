using UnityEngine;

/// <summary>
/// Fuente de curación (GDD): un punto fijo del piso que restaura el 100 % de la vida de Abby. Se activa con E y se puede
/// usar una sola vez (solo se gasta si le falta vida). Brilla para verse de lejos; al usarse se apaga.
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
    public bool Usada => used;
    /// <summary>Abby está cerca y puede usarla (el HUD muestra "E  Usar la fuente").</summary>
    public static HealingShrine Cerca { get; private set; }

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

        if (Cerca == this) Cerca = null;
        if (used || player == null) return;
        if (health == null) health = player.GetComponent<PlayerHealth>();
        if (health == null) return;
        Vector3 d = player.position - transform.position; d.y = 0f;
        if (d.magnitude > radius + 0.6f) return;
        Cerca = this;

        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null || !kb.eKey.wasPressedThisFrame || PauseMenu.IsPaused || LoreNote.Open != null) return;
        if (health.Health >= health.maxHealth)
        {
            if (hintCooldown <= 0f) { FloorDirector.Popup(transform.position + Vector3.up * 2.2f, "Vida al máximo", new Color(0.6f, 1f, 0.8f)); hintCooldown = 1f; }
            return;
        }
        used = true; Cerca = null;
        health.Heal(health.maxHealth);
        Sonido.Tocar(Sonido.Efecto.Fuente);
        FloorDirector.Banner("FUENTE DE CURACIÓN", "Tu vida se ha restaurado", new Color(0.5f, 1f, 0.7f), 2.2f);
    }
}
