using UnityEngine;

/// <summary>ENVIDIA — En esta sala, al empezar el combate aparece La Otra (EnvyShadow); al limpiarla se desvanece.</summary>
[RequireComponent(typeof(RoomEncounter))]
public class EnvyRoom : MonoBehaviour
{
    EnvyShadow shadow;

    void Start()
    {
        var enc = GetComponent<RoomEncounter>();
        enc.Began += () => shadow = EnvyShadow.Spawn(transform);
        enc.Cleared += () =>
        {
            if (shadow == null) return;
            CombatFx.Burst(shadow.transform.position + Vector3.up, new Color(0.3f, 1f, 0.45f), 30, 4f, 0.14f, 0.9f, -0.2f);
            Destroy(shadow.gameObject);
        };
    }
}
