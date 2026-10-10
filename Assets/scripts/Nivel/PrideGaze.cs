using UnityEngine;

/// <summary>
/// SOBERBIA — El Ojo que Juzga: un ojo dorado flota alto y barre la sala con un cono de luz. Si te ve
/// (estás dentro del cono y nada se interpone), te quema y te hace pequeña (lenta). Las columnas y estatuas tapan
/// su mirada: hay que avanzar de sombra en sombra.
/// </summary>
public class PrideGaze : MonoBehaviour
{
    public float range = 16f;
    public float coneAngle = 16f;
    public float sweepSpeed = 32f;       // grados por segundo
    public float sweepArc = 140f;        // barre de -arc/2 a +arc/2 (0 = gira completo)
    public float damagePerTick = 6f;
    public float tick = 0.45f;
    public Transform eye;
    public Light spot;
    public Transform beam;               // cono visible

    float angle, phase, lastTick, seenFlash;
    float baseYaw;

    void Start() { baseYaw = transform.eulerAngles.y; }

    void Update()
    {
        // barrido
        float speed = sweepSpeed;
        foreach (var e in TowerEnemy.All) if (e.IsBoss && e.Enraged) { speed *= 1.6f; break; }
        phase += Time.deltaTime * speed;
        // ida y vuelta suave sobre el arco (o vuelta completa si el arco es 0)
        angle = sweepArc > 0f ? Mathf.Sin(phase / sweepArc * Mathf.PI) * sweepArc / 2f : phase;
        Quaternion look = Quaternion.Euler(52f, baseYaw + angle, 0f);
        if (eye != null) eye.rotation = look;
        if (spot != null) spot.transform.rotation = look;
        if (beam != null) beam.rotation = look;

        var player = Hazards.Player;
        if (player == null || eye == null) return;
        Vector3 target = player.position + Vector3.up * 0.7f;
        Vector3 to = target - eye.position;
        float dist = to.magnitude;
        bool seen = dist < range && Vector3.Angle(look * Vector3.forward, to) < coneAngle;
        if (seen && Physics.Raycast(eye.position, to / dist, out var hit, dist - 0.4f, ~0, QueryTriggerInteraction.Ignore))
            if (!hit.transform.IsChildOf(player)) seen = false;   // algo lo tapa: estás a salvo
        seenFlash = Mathf.MoveTowards(seenFlash, seen ? 1f : 0f, Time.deltaTime * 4f);
        if (spot != null) spot.color = Color.Lerp(new Color(1f, 0.9f, 0.6f), new Color(1f, 0.35f, 0.2f), seenFlash);
        if (seen && Time.time - lastTick > tick)
        {
            lastTick = Time.time;
            Hazards.Hurt(eye.position, damagePerTick, 2f);
            Hazards.Motor?.Slow(0.65f, 0.6f);
            if (Random.value < 0.3f) FloorDirector.Popup(player.position + Vector3.up * 2f, Random.value < 0.5f ? "\"Insignificante\"" : "\"¿Tú? ¿Subir?\"", new Color(1f, 0.85f, 0.45f), 1.4f);
        }
    }
}
