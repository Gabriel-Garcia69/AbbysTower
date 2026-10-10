using UnityEngine;

/// <summary>
/// Zona para trepar (escaleras, enredaderas). Va delante de la escalera, con su eje Z (forward) apuntando hacia
/// quien trepa; la plataforma a la que se sube queda hacia -forward.
/// Abby entra a la zona y pulsa W (adelante) → trepa: W sube, S baja, Espacio se suelta de un salto.
/// Arriba se baja sola sobre la plataforma. Mientras trepa se apagan PlayerMotor, el dash y el ataque.
/// </summary>
public class ClimbZone : MonoBehaviour
{
    [Tooltip("Alto de la escalera desde la base de esta zona (m).")]
    public float height = 2.25f;
    [Tooltip("Ancho (x) y fondo (z) de la zona donde se puede empezar a trepar.")]
    public Vector2 footprint = new Vector2(1.2f, 0.9f);
    public float speed = 3.2f;

    static ClimbZone active;
    static bool hintShown;

    Transform player;
    CharacterController cc;
    PlayerMotor motor;
    PlayerInputReader input;
    AbbyDash dash;
    AbbyAttack attack;
    bool climbing;

    public static bool AnyClimbing => active != null;

    void Start() => FindPlayer();

    void FindPlayer()
    {
        var p = GameObject.FindWithTag("Player");
        if (p == null) return;
        player = p.transform;
        cc = p.GetComponent<CharacterController>();
        motor = p.GetComponent<PlayerMotor>();
        input = p.GetComponent<PlayerInputReader>();
    }

    void OnDisable() { if (climbing) End(); }

    void Update()
    {
        if (player == null) { FindPlayer(); if (player == null) return; }
        if (dash == null) dash = player.GetComponent<AbbyDash>();
        if (attack == null) attack = player.GetComponent<AbbyAttack>();

        Vector3 lp = transform.InverseTransformPoint(player.position);
        bool inside = Mathf.Abs(lp.x) < footprint.x / 2f && Mathf.Abs(lp.z) < footprint.y / 2f + 0.3f && lp.y > -0.4f && lp.y < height + 0.3f;
        float v = input != null && input.enabled ? input.Move.y : 0f;

        if (!climbing)
        {
            if (!inside || active != null || PauseMenu.IsPaused) return;
            if (!hintShown) { hintShown = true; FloorDirector.Popup(player.position + Vector3.up * 2f, "W: trepar", new Color(0.5f, 1f, 0.9f), 1.8f); }
            if (v > 0.3f && lp.y < height - 0.3f && (dash == null || !dash.IsDashing)) Begin();
            return;
        }

        // trepando
        if (input != null && input.ConsumeJump())
        {
            End();
            cc.Move(transform.forward * 0.6f);   // se suelta hacia atrás (alejándose de la pared)
            return;
        }
        float dt = Time.deltaTime;
        // pegada a la escalera: se centra suave en x y en la profundidad de la zona
        Vector3 target = transform.TransformPoint(new Vector3(0f, lp.y, 0f));
        Vector3 side = target - player.position; side.y = 0f;
        cc.Move(side * Mathf.Min(1f, 10f * dt) + Vector3.up * v * speed * dt);

        lp = transform.InverseTransformPoint(player.position);
        if (lp.y >= height - 0.05f && v > 0f)
        {
            // arriba: sube un poco más y se baja sobre la plataforma
            cc.Move(Vector3.up * 0.35f);
            cc.Move(-transform.forward * 1.1f);
            End();
        }
        else if (lp.y <= 0.02f && v < 0f) End();
    }

    void Begin()
    {
        climbing = true; active = this;
        if (motor != null) motor.enabled = false;
        if (dash != null) dash.enabled = false;
        if (attack != null) attack.enabled = false;
    }

    void End()
    {
        climbing = false;
        if (active == this) active = null;
        if (motor != null) motor.enabled = true;
        if (dash != null) dash.enabled = true;
        if (attack != null) attack.enabled = true;
    }

    void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.35f, 1f, 0.85f, 0.6f);
        Gizmos.DrawWireCube(new Vector3(0f, height / 2f, 0f), new Vector3(footprint.x, height, footprint.y));
    }
}
