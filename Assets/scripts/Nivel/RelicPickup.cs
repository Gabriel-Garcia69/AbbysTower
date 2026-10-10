using UnityEngine;

/// <summary>
/// Reliquia (easter egg): objeto raro que gira y flota. Al tocarlo se guarda y se muestra su descripción.
/// Si ya la tenías de otra partida, aparece translúcida y solo te recuerda que es tuya.
/// </summary>
public class RelicPickup : MonoBehaviour
{
    public string id = "";
    public string relicName = "";
    [TextArea] public string description = "";
    public Transform model;
    public float radius = 1.1f;

    bool taken;
    Vector3 basePos;

    void Start()
    {
        if (model == null) model = transform;
        basePos = model.localPosition;
    }

    void Update()
    {
        if (taken) return;
        model.localPosition = basePos + Vector3.up * (Mathf.Sin(Time.time * 2f) * 0.08f);
        model.Rotate(0f, 50f * Time.deltaTime, 0f, Space.World);
        float d = Hazards.FlatDistance(transform.position, out bool same);
        if (d > radius || !same) return;
        taken = true;
        bool first = SecretTracker.Mark("reliquia:" + id);
        FloorDirector.Banner(first ? "¡RELIQUIA ENCONTRADA!" : "RELIQUIA (ya era tuya)", $"{relicName} — {SecretTracker.Relics}/{SecretTracker.TotalRelics}", new Color(1f, 0.85f, 0.4f), 4.5f);
        FloorDirector.Popup(transform.position + Vector3.up * 1.6f, description, new Color(1f, 0.95f, 0.8f), 5f);
        CombatFx.Burst(transform.position + Vector3.up * 0.6f, new Color(1f, 0.85f, 0.4f), 60, 6f, 0.16f, 1.2f, -0.3f);
        CameraShake.Shake(0.15f, 0.3f);
        gameObject.SetActive(false);
    }
}
