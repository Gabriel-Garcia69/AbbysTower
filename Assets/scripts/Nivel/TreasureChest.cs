using UnityEngine;

/// <summary>
/// Cofre escondido (normalmente detrás de un Velo de Fase): al tocarlo se abre la tapa y suelta fragmentos.
/// </summary>
public class TreasureChest : MonoBehaviour
{
    public int fragments = 20;
    public float radius = 1.3f;
    public Transform lid;
    public Light glow;

    Transform player;
    bool opened;
    float t;

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (opened)
        {
            t += Time.deltaTime;
            if (lid != null) lid.localRotation = Quaternion.Euler(Mathf.Lerp(0f, -110f, Mathf.SmoothStep(0f, 1f, t / 0.5f)), 0f, 0f);
            if (glow != null) glow.intensity = Mathf.MoveTowards(glow.intensity, 0.5f, Time.deltaTime * 4f);
            return;
        }
        if (glow != null) glow.intensity = 3f + Mathf.Sin(Time.time * 3f);
        if (player == null) return;
        Vector3 d = player.position - transform.position; d.y = 0f;
        if (d.magnitude > radius) return;

        opened = true;
        ShardPickup.Drop(transform.position + Vector3.up * 0.4f, fragments);
        CombatFx.Burst(transform.position + Vector3.up * 0.8f, new Color(1f, 0.85f, 0.4f), 40, 5f, 0.18f, 0.9f, -0.2f);
        FloorDirector.Banner("¡COFRE OCULTO!", $"+{fragments} fragmentos", new Color(1f, 0.85f, 0.45f), 2.2f);
    }
}
