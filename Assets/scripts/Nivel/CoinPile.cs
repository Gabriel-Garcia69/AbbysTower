using UnityEngine;

/// <summary>
/// AVARICIA — Montón de monedas: al tocarlo da fragmentos. Algunos están malditos (no se distinguen… casi:
/// brillan un poco más rojizo): explotan en púas alrededor. "La avaricia rompe el saco."
/// </summary>
public class CoinPile : MonoBehaviour
{
    public int fragments = 4;
    public bool cursed;
    public float radius = 0.9f;
    public float damage = 18f;

    void Update()
    {
        float d = Hazards.FlatDistance(transform.position, out bool same);
        if (d > radius || !same) return;
        if (cursed)
        {
            FloorDirector.Popup(transform.position + Vector3.up * 1.6f, "¡La avaricia rompe el saco!", new Color(1f, 0.4f, 0.3f), 1.6f);
            CombatFx.Ring(transform.position, 2.2f, new Color(1f, 0.3f, 0.2f), 0.4f);
            CombatFx.Burst(transform.position + Vector3.up * 0.4f, new Color(1f, 0.75f, 0.3f), 30, 7f, 0.12f, 0.5f, 0.8f);
            Hazards.Hurt(transform.position, damage, 9f);
            CameraShake.Shake(0.2f, 0.25f);
        }
        else
        {
            ShardPickup.Drop(transform.position + Vector3.up * 0.3f, fragments);
            CombatFx.Burst(transform.position + Vector3.up * 0.3f, new Color(1f, 0.85f, 0.35f), 18, 3f, 0.1f, 0.5f);
        }
        gameObject.SetActive(false);
    }
}
