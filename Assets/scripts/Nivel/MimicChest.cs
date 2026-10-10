using UnityEngine;

/// <summary>
/// AVARICIA — Cofre mímico: parece un cofre normal. Al acercarse abre la tapa con dientes y de adentro sale un Bruto.
/// Si lo derrotas suelta un buen botín. Pista para los atentos: respira (la tapa se mueve apenas).
/// </summary>
public class MimicChest : MonoBehaviour
{
    public Transform lid;
    public Transform roomRoot;
    public Vector2 roomSize = new Vector2(14f, 12f);
    public float radius = 1.6f;
    public int bounty = 30;

    bool sprung;
    TowerEnemy spawned;
    float t;

    void Update()
    {
        t += Time.deltaTime;
        if (!sprung)
        {
            if (lid != null) lid.localRotation = Quaternion.Euler(-2.5f - Mathf.Sin(t * 2.2f) * 2.5f, 0f, 0f);   // respira
            float d = Hazards.FlatDistance(transform.position, out bool same);
            if (d > radius || !same) return;
            sprung = true;
            FloorDirector.Banner("¡ERA UN MÍMICO!", "La avaricia tiene dientes", new Color(1f, 0.45f, 0.3f), 2.5f);
            CameraShake.Shake(0.3f, 0.4f);
            spawned = TowerEnemy.Create(TowerEnemy.Kind.Bruto, transform.position + transform.forward * 1.2f, transform.parent, null, Hazards.Player);
            spawned.customName = "Mímico";
            spawned.boundsRoot = roomRoot; spawned.boundsSize = roomSize;
            return;
        }
        if (lid != null) lid.localRotation = Quaternion.Euler(-60f - Mathf.Abs(Mathf.Sin(t * 9f)) * 30f, 0f, 0f);   // mastica
        if (spawned == null && bounty > 0)
        {
            ShardPickup.Drop(transform.position + Vector3.up * 0.4f, bounty);
            FloorDirector.Banner("MÍMICO DERROTADO", $"+{bounty} fragmentos", new Color(1f, 0.85f, 0.45f), 2f);
            bounty = 0;
            enabled = false;
        }
    }
}
