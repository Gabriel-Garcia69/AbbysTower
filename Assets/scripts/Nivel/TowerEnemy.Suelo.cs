using UnityEngine;

/// <summary>
/// Enemigos de los pisos abiertos (HD-2D, arenas con correa): pisan el suelo de verdad. Suben rampas, escaleras y
/// puentes; no atraviesan muros ni se meten debajo de las estructuras; no se tiran al río ni por un barranco.
/// Persiguen a Abby fuera de su arena hasta la correa (RoomEncounter.leash).
/// Las salas de la torre (sin correa) siguen como antes: pegados al piso de la sala.
/// </summary>
public partial class TowerEnemy
{
    const float StepUp = 0.75f, MaxDrop = 1.4f;
    static readonly RaycastHit[] groundHits = new RaycastHit[16];
    static readonly Collider[] overlapHits = new Collider[16];

    bool FollowsGround => room != null && room.leash > 0f;

    static bool Ignored(Collider c) =>
        c.isTrigger || c.GetComponentInParent<TowerEnemy>() != null || c.GetComponentInParent<CharacterController>() != null;

    /// <summary>Altura del suelo más alto bajo p que esté por debajo de fromY.</summary>
    static bool GroundAt(Vector3 p, float fromY, out float y)
    {
        y = 0f;
        bool found = false;
        float best = float.NegativeInfinity;
        int n = Physics.RaycastNonAlloc(new Vector3(p.x, fromY, p.z), Vector3.down, groundHits, 60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            if (Ignored(groundHits[i].collider)) continue;
            if (groundHits[i].point.y > best) { best = groundHits[i].point.y; found = true; }
        }
        if (found) y = best;
        return found;
    }

    /// <summary>¿Hay un muro (algo más alto que un escalón) entre from y to?</summary>
    bool Blocked(Vector3 from, Vector3 to)
    {
        Vector3 d = to - from; d.y = 0f;
        float len = d.magnitude;
        if (len < 0.0001f) return false;
        float r = Mathf.Min(radius, 0.6f) * 0.8f;
        Vector3 o = from + Vector3.up * (StepUp + r + 0.05f);
        int n = Physics.SphereCastNonAlloc(o, r, d / len, groundHits, len + 0.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var h = groundHits[i];
            if (Ignored(h.collider)) continue;
            if (h.distance <= 0f && h.point == Vector3.zero) continue;   // ya estaba encimado: que pueda salir
            return true;
        }
        return false;
    }

    static readonly float[] SteerAngles = { 35f, 70f, 105f, 140f };
    float steerSide = 1f, steerHold, steerAngle;

    /// <summary>Si choca, rodea el obstáculo: prueba girar el paso a un lado y al otro (y se queda con el lado que
    /// funcionó un momento, para no titubear en las esquinas, p. ej. entre la cerca y el barandal del puente).</summary>
    Vector3 Slide(Vector3 start, Vector3 to)
    {
        Vector3 step = to - start; step.y = 0f;
        foreach (float ang in SteerAngles)
            for (int k = 0; k < 2; k++)
            {
                float side = k == 0 ? steerSide : -steerSide;
                var cand = start + Quaternion.Euler(0f, ang * side, 0f) * step * Mathf.Lerp(1f, 0.7f, ang / 140f);
                if (Blocked(start, cand) || !GroundAt(cand, start.y + StepUp, out float gy) || gy < start.y - MaxDrop) continue;
                steerSide = side; steerHold = 0.7f; steerAngle = ang * side;
                return cand;
            }
        return start;
    }

    /// <summary>Corrige el paso de este cuadro: muros, escalones, suelo y orillas.</summary>
    void FollowGround(Vector3 start)
    {
        if (!FollowsGround || state == State.Leap || state == State.Spawning) return;
        if (steerHold > 0f) steerHold -= Time.deltaTime; else steerSide = Random.value < 0.5f ? 1f : -1f;
        Vector3 p = transform.position;
        var flat = new Vector3(p.x, start.y, p.z);
        // mientras rodea un obstáculo, sigue el rodeo un momento (si no, regresa a la esquina y titubea)
        if (steerHold > 0f && state == State.Chase)
        {
            var turned = start + Quaternion.Euler(0f, steerAngle, 0f) * (flat - start);
            if (!Blocked(start, turned)) flat = turned;
        }
        if ((flat - start).sqrMagnitude > 1e-6f && Blocked(start, flat)) flat = Slide(start, flat);
        if (!GroundAt(flat, start.y + StepUp, out float gy) || gy < start.y - MaxDrop)
        {
            flat = start;   // orilla, río o barranco: no da el paso
            if (!GroundAt(flat, start.y + StepUp, out gy)) { transform.position = start; return; }
        }
        flat.y = gy >= start.y ? gy : Mathf.MoveTowards(start.y, gy, 14f * Time.deltaTime);
        transform.position = flat;
    }

    /// <summary>Al aparecer: si el punto cae dentro de una estructura, se corre hacia el centro de la arena; luego al suelo.</summary>
    void SettleSpawn()
    {
        if (!FollowsGround) return;
        Vector3 home = room.transform.position, p = transform.position;
        for (int tries = 0; tries < 12; tries++)
        {
            if (GroundAt(p, p.y + 3f, out float gy) && gy < p.y + 1.5f && !Inside(new Vector3(p.x, gy, p.z)))
            {
                transform.position = new Vector3(p.x, gy, p.z);
                return;
            }
            p = Vector3.Lerp(p, home, 0.3f);
        }
        if (GroundAt(home, home.y + 3f, out float hy)) transform.position = new Vector3(home.x, hy, home.z);
    }

    bool Inside(Vector3 feet)
    {
        float r = Mathf.Min(radius, 0.6f);
        int n = Physics.OverlapCapsuleNonAlloc(feet + Vector3.up * (StepUp + r), feet + Vector3.up * 1.8f, r, overlapHits,
                                               Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++) if (!Ignored(overlapHits[i])) return true;
        return false;
    }

    /// <summary>¿Se puede caer ahí? (suelo firme a esa altura y sin muro encima). En salas sin correa, siempre.</summary>
    bool GoodLanding(Vector3 p)
    {
        if (!FollowsGround) return true;
        return GroundAt(p, p.y + 0.4f, out float y) && Mathf.Abs(y - p.y) < 0.4f && !Inside(p);
    }

    /// <summary>Altura a la que cae un salto o un parpadeo: el suelo bajo Abby (en pisos abiertos) o el de la sala.</summary>
    float LandY(Vector3 p)
    {
        if (!FollowsGround) return transform.position.y;
        float from = Mathf.Max(transform.position.y, target != null ? target.position.y : p.y) + 2f;
        return GroundAt(p, from, out float y) ? y : transform.position.y;
    }
}
