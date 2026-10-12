using UnityEngine;

/// <summary>
/// Tipos de enemigo con estilos de pelea propios y patrones de jefe por piso:
/// - Arquero: guarda distancia y dispara ráfagas de tres orbes en abanico; huye si te le pegas.
/// - Saltador: marca un círculo donde estás y cae encima de un salto.
/// - Hechicero: se teletransporta y hace llover golpes marcados en el suelo alrededor de Abby.
/// - Torbellino: gira con aspas afiladas persiguiéndote; al terminar queda mareado (castígalo).
/// - Escudero: bloquea de frente; rómpele la guardia con el 3er tajo o un tajo de impulso, o pégale por la espalda.
/// Jefes (bossStyle): 1 Capitán (torbellino + abanicos), 2 Gárgola (saltos encadenados + pisotón), 3 Geoda (lluvia de
/// cristales + teletransporte + anillo de orbes), 4 Guardián Dorado (pisotón + abanicos + embestida), 5 Escarcha
/// (carámbanos + giro + teletransporte), 6 Arquitecto (todo).
/// </summary>
public partial class TowerEnemy
{
    public int bossStyle;
    float stunTime = 0.3f;
    Vector3 leapFrom, leapTo;
    Transform spinner;
    Renderer shield;
    int volley;

    // ------------------------------------------------------------------ configuración y forma

    void ConfigureExtra()
    {
        switch (kind)
        {
            case Kind.Arquero:
                maxHealth = 32f; moveSpeed = 2.9f; radius = 0.42f; damage = 8f; attackRange = 11f;
                telegraph = 0.55f; recover = 0.9f; poise = 0f; knockTaken = 1f;
                eyeColor = new Color(0.45f, 1f, 0.55f); bodyColor = new Color(0.08f, 0.14f, 0.1f);
                break;
            case Kind.Saltador:
                maxHealth = 55f; moveSpeed = 3.2f; radius = 0.5f; damage = 14f; attackRange = 7.5f;
                telegraph = 0.45f; recover = 0.95f; poise = 12f; knockTaken = 0.8f;
                eyeColor = new Color(1f, 0.65f, 0.2f); bodyColor = new Color(0.18f, 0.1f, 0.05f);
                break;
            case Kind.Hechicero:
                maxHealth = 45f; moveSpeed = 2.2f; radius = 0.45f; damage = 12f; attackRange = 12f;
                telegraph = 0.6f; recover = 1.1f; poise = 0f; knockTaken = 1f;
                eyeColor = new Color(0.8f, 0.45f, 1f); bodyColor = new Color(0.12f, 0.06f, 0.2f);
                break;
            case Kind.Torbellino:
                maxHealth = 75f; moveSpeed = 2.6f; radius = 0.6f; damage = 9f; attackRange = 4f;
                telegraph = 0.55f; recover = 1.4f; poise = 18f; knockTaken = 0.6f;
                eyeColor = new Color(0.4f, 0.9f, 1f); bodyColor = new Color(0.06f, 0.1f, 0.16f);
                break;
            case Kind.Escudero:
                maxHealth = 95f; moveSpeed = 2.1f; radius = 0.7f; damage = 16f; attackRange = 3f;
                telegraph = 0.7f; lungeSpeed = 10f; lungeTime = 0.26f; recover = 1f; poise = 30f; knockTaken = 0.4f;
                eyeColor = new Color(1f, 0.85f, 0.35f); bodyColor = new Color(0.16f, 0.14f, 0.18f);
                break;
        }
    }

    void BuildExtraVisual(float s)
    {
        switch (kind)
        {
            case Kind.Arquero:
            {
                var hood = Part(PrimitiveType.Sphere, "Capucha", new Vector3(0f, 1.15f, -0.05f) * s, new Vector3(0.7f, 0.55f, 0.75f) * s, litMat);
                SetColor(hood.GetComponent<Renderer>(), bodyColor * 1.6f);
                var bow = Part(PrimitiveType.Cube, "Arco", new Vector3(0.45f, 0.85f, 0.25f) * s, new Vector3(0.06f, 1f, 0.12f) * s, litMat);
                bow.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
                SetColor(bow.GetComponent<Renderer>(), new Color(0.45f, 0.3f, 0.15f));
                break;
            }
            case Kind.Saltador:
            {
                foreach (float x in new[] { -0.3f, 0.3f })
                {
                    var leg = Part(PrimitiveType.Cylinder, "Pata", new Vector3(x, 0.25f, 0f) * s, new Vector3(0.22f, 0.3f, 0.22f) * s, litMat);
                    leg.transform.localRotation = Quaternion.Euler(0f, 0f, x > 0 ? -18f : 18f);
                    SetColor(leg.GetComponent<Renderer>(), bodyColor * 1.5f);
                }
                foreach (float x in new[] { -0.22f, 0.22f })
                {
                    var ear = Part(PrimitiveType.Cube, "Oreja", new Vector3(x, 1.35f, -0.05f) * s, new Vector3(0.12f, 0.45f, 0.08f) * s, litMat);
                    ear.transform.localRotation = Quaternion.Euler(-15f, 0f, x > 0 ? -12f : 12f);
                }
                break;
            }
            case Kind.Hechicero:
            {
                var hat = Part(PrimitiveType.Cylinder, "Sombrero", new Vector3(0f, 1.45f, 0f) * s, new Vector3(0.75f, 0.04f, 0.75f) * s, litMat);
                SetColor(hat.GetComponent<Renderer>(), bodyColor * 1.4f);
                var tip = Part(PrimitiveType.Cube, "Punta", new Vector3(0f, 1.8f, -0.05f) * s, new Vector3(0.3f, 0.7f, 0.3f) * s, litMat);
                tip.transform.localRotation = Quaternion.Euler(-14f, 45f, 0f);
                SetColor(tip.GetComponent<Renderer>(), bodyColor * 1.4f);
                spinner = new GameObject("Orbes").transform;
                spinner.SetParent(visual, false);
                spinner.localPosition = new Vector3(0f, 0.9f * s, 0f);
                spinner.gameObject.AddComponent<EnemySpin>().speed = 90f;
                for (int i = 0; i < 3; i++)
                {
                    float a = i / 3f * Mathf.PI * 2f;
                    var orb = Part(PrimitiveType.Sphere, "Orbe", new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.75f * s, Vector3.one * 0.16f * s, eyeMat, spinner);
                    SetColor(orb.GetComponent<Renderer>(), eyeColor * 3f);
                }
                break;
            }
            case Kind.Torbellino:
            {
                spinner = new GameObject("Aspas").transform;
                spinner.SetParent(visual, false);
                spinner.localPosition = new Vector3(0f, 0.55f * s, 0f);
                spinner.gameObject.AddComponent<EnemySpin>().speed = 60f;
                for (int i = 0; i < 4; i++)
                {
                    var blade = Part(PrimitiveType.Cube, "Aspa", Vector3.zero, new Vector3(0.9f, 0.05f, 0.18f) * s, litMat, spinner);
                    blade.transform.localRotation = Quaternion.Euler(0f, i * 90f, 8f);
                    blade.transform.localPosition = blade.transform.localRotation * new Vector3(0.65f * s, 0f, 0f);
                    SetColor(blade.GetComponent<Renderer>(), new Color(0.75f, 0.85f, 0.95f));
                }
                break;
            }
            case Kind.Escudero:
            {
                var sh = Part(PrimitiveType.Cube, "Escudo", new Vector3(0f, 0.75f, 0.55f) * s, new Vector3(0.95f, 1.2f, 0.12f) * s, litMat);
                shield = sh.GetComponent<Renderer>();
                SetColor(shield, new Color(0.55f, 0.5f, 0.4f));
                var boss = Part(PrimitiveType.Sphere, "Umbo", new Vector3(0f, 0.8f, 0.62f) * s, new Vector3(0.3f, 0.3f, 0.12f) * s, eyeMat);
                SetColor(boss.GetComponent<Renderer>(), eyeColor * 2f);
                var helm = Part(PrimitiveType.Cube, "Yelmo", new Vector3(0f, 1.25f, 0f) * s, new Vector3(0.6f, 0.35f, 0.6f) * s, litMat);
                SetColor(helm.GetComponent<Renderer>(), new Color(0.4f, 0.38f, 0.42f));
                break;
            }
        }
    }

    // ------------------------------------------------------------------ prefabs de apariencia

    /// <summary>Lo usa el generador de prefabs: arma la forma con primitivas aunque exista el prefab.</summary>
    public static bool ForceProceduralVisual;

    /// <summary>
    /// Si existe Resources/Enemigos/&lt;Tipo&gt;, se usa como apariencia (se puede editar o reemplazar por arte).
    /// Piezas que reconoce por nombre: "Cuerpo" (parpadea al recibir golpes), "Ojo" (brillan al avisar),
    /// "Aspas"/"Orbes" (giran), "Escudo" (destella al bloquear) y una Light (brillo del jefe).
    /// </summary>
    bool UsePrefabVisual()
    {
        var pf = Resources.Load<GameObject>("Enemigos/" + kind);
        if (pf == null) return false;
        visual = Instantiate(pf, transform).transform;
        visual.name = "Visual";
        visual.localPosition = Vector3.zero;
        var eyeList = new System.Collections.Generic.List<Renderer>();
        foreach (var r in visual.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name == "Cuerpo") body = r;
            else if (r.name.StartsWith("Ojo")) eyeList.Add(r);
            else if (r.name == "Escudo") shield = r;
        }
        eyes = eyeList.ToArray();
        foreach (Transform t in visual.GetComponentsInChildren<Transform>(true))
            if (t.name == "Aspas" || t.name == "Orbes") spinner = t;
        glow = visual.GetComponentInChildren<Light>(true);
        if (body != null) SetColor(body, bodyColor);
        foreach (var e in eyes) SetColor(e, eyeColor * 4f);
        visual.localScale = Vector3.one * 0.05f;
        return true;
    }

    /// <summary>Para el generador de prefabs: configura el tipo y arma su forma; devuelve el objeto "Visual".</summary>
    public Transform BuildForPrefab(Kind k)
    {
        kind = k;
        ForceProceduralVisual = true;
        try { Configure(); } finally { ForceProceduralVisual = false; }
        visual.localScale = Vector3.one;
        return visual;
    }

    // ------------------------------------------------------------------ movimiento

    Vector3 ChaseMove(float dist, float speed)
    {
        switch (kind)
        {
            case Kind.Arquero:
            case Kind.Hechicero:
                // guarda distancia: se acerca si está lejos, se aleja si Abby se le pega, y si no, rodea
                if (dist > 9f) return facing * speed;
                if (dist < 5f) return -facing * speed * 1.1f;
                return Vector3.Cross(Vector3.up, facing) * speed * 0.6f * (Mathf.Sin(bob * 0.7f) > 0f ? 1f : -1f);
            default:
                return dist > attackRange * 0.7f ? facing * speed : Vector3.zero;
        }
    }

    // ------------------------------------------------------------------ ataques

    bool ChooseExtra(float dist)
    {
        if (kind == Kind.Jefe)
        {
            if (bossStyle == 0) return false;
            return ChooseBoss(dist);
        }
        float cd = Difficulty.Active.attackCooldown;
        switch (kind)
        {
            case Kind.Arquero:
                volley = 0;
                Go(State.Shoot);
                cooldown = Random.Range(1.4f, 2.2f) * cd;
                return true;
            case Kind.Saltador:
                StartLeap(2.4f);
                cooldown = Random.Range(1.6f, 2.4f) * cd;
                return true;
            case Kind.Hechicero:
                if (attackCount % 2 == 1) Go(State.Blink); else StartRain(3, 2.2f, 1f);
                cooldown = Random.Range(1.6f, 2.4f) * cd;
                return true;
            case Kind.Torbellino:
                Go(State.Spin);
                cooldown = Random.Range(2.2f, 3f) * cd;
                return true;
        }
        return false;   // Escudero y los demás: embestida normal
    }

    bool ChooseBoss(float dist)
    {
        float cd = Difficulty.Active.attackCooldown;
        int k = attackCount;
        string[] plan;
        switch (bossStyle)
        {
            case 1: plan = new[] { "spin", "fan", "lunge", "fan" }; break;
            case 2: plan = new[] { "leap", "leap", "slam", "lunge" }; break;
            case 3: plan = new[] { "rain", "blink", "ring", "rain" }; break;
            case 4: plan = new[] { "slam", "fan", "lunge", "ring" }; break;
            case 5: plan = new[] { "rain", "spin", "blink", "fan" }; break;
            default: plan = new[] { "leap", "rain", "spin", "slam", "fan", "blink", "ring" }; break;
        }
        string a = plan[k % plan.Length];
        if (a == "lunge" && dist > 9f) a = "fan";
        cooldown = (Enraged ? 0.9f : 1.3f) * cd;
        switch (a)
        {
            case "spin": Go(State.Spin); return true;
            case "fan": volley = 0; Go(State.Shoot); return true;
            case "leap": StartLeap(4.2f); return true;
            case "rain": StartRain(Enraged ? 7 : 5, 3.2f, 1.1f); return true;
            case "blink": Go(State.Blink); return true;
            case "ring": FireBolts(Enraged ? 16 : 12); Go(State.Recover); return true;
            case "slam":
                Go(State.Slam);
                telegraphFx = CombatFx.Ring(transform.position, 5.2f, new Color(1f, 0.25f, 0.2f, 0.9f), 1.05f * (Enraged ? 0.75f : 1f));
                return true;
            default: return false;   // embestida
        }
    }

    void StartLeap(float landRadius)
    {
        leapFrom = transform.position;
        leapTo = target != null ? target.position : transform.position + facing * 4f;
        leapTo.y = LandY(leapTo);
        if (!GoodLanding(leapTo)) leapTo = leapFrom;   // no salta al río ni dentro de un muro
        Go(State.Leap);
        telegraphFx = CombatFx.Ring(leapTo, landRadius, new Color(1f, 0.4f, 0.15f, 0.9f), telegraph + 0.6f);
    }

    void StartRain(int count, float spread, float delay)
    {
        if (target == null) return;
        Go(State.Rain);
        for (int i = 0; i < count; i++)
        {
            Vector3 p = target.position;
            if (i > 0)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                p += new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(spread * 0.4f, spread);
            }
            p.y = FollowsGround ? LandY(p) : transform.position.y;
            Blast.Drop(p, IsBoss ? 1.8f : 1.4f, delay + i * 0.12f, IsBoss ? 16f : damage, eyeColor, this);
        }
    }

    void UpdateExtra(float dt, Vector3 toPlayer, float dist, PlayerHealth ph, bool playerAlive)
    {
        switch (state)
        {
            case State.Shoot:
            {
                // apunta temblando, dispara un abanico; el jefe dispara tres seguidos
                if (dist > 0.01f) facing = Vector3.Slerp(facing, toPlayer / dist, 10f * dt).normalized;
                visual.localPosition = new Vector3(Mathf.Sin(timer * 60f) * 0.02f * Scale, 0f, 0f);
                foreach (var e in eyes) SetColor(e, eyeColor * Mathf.Lerp(4f, 10f, timer / telegraph));
                if (timer >= telegraph)
                {
                    int n = IsBoss ? (Enraged ? 7 : 5) : 3;
                    float spreadDeg = IsBoss ? 50f : 26f;
                    for (int i = 0; i < n; i++)
                    {
                        float ang = n == 1 ? 0f : Mathf.Lerp(-spreadDeg / 2f, spreadDeg / 2f, i / (float)(n - 1));
                        Bolt.Fire(transform.position + Vector3.up * 0.9f * Scale + facing * radius, Quaternion.Euler(0f, ang, 0f) * facing,
                                  IsBoss ? 8f : 9f, IsBoss ? 14f : damage, eyeColor, this);
                    }
                    volley++;
                    if (IsBoss && volley < 3) timer = telegraph * 0.55f;
                    else { foreach (var e in eyes) SetColor(e, eyeColor * 4f); Go(State.Recover); }
                }
                break;
            }
            case State.Leap:
            {
                float crouch = telegraph, air = IsBoss ? 0.75f : 0.6f;
                if (timer < crouch)
                {
                    visual.localScale = new Vector3(1.1f, 0.8f, 1.1f);   // se agacha
                    break;
                }
                float k = Mathf.Clamp01((timer - crouch) / air);
                visual.localScale = Vector3.one;
                transform.position = Vector3.Lerp(leapFrom, leapTo, k);
                visual.localPosition = Vector3.up * Mathf.Sin(k * Mathf.PI) * (IsBoss ? 5f : 3f);
                if (k >= 1f)
                {
                    visual.localPosition = Vector3.zero;
                    float r = IsBoss ? 4.2f : 2.4f;
                    CameraShake.Shake(IsBoss ? 0.45f : 0.18f, 0.3f);
                    CombatFx.Burst(transform.position + Vector3.up * 0.2f, eyeColor, IsBoss ? 50 : 24, 7f, 0.2f, 0.5f, 1.2f);
                    CombatFx.Ring(transform.position, r, eyeColor, 0.25f);
                    Vector3 d = target != null ? target.position - transform.position : Vector3.one * 99f; d.y = 0f;
                    if (playerAlive && ph != null && d.magnitude < r)
                        ph.ReceiveHit(new HitInfo { damage = IsBoss ? 24f : damage, knockback = IsBoss ? 12f : 8f, hitStop = 0.05f, sourcePosition = transform.position, attacker = this });
                    if (IsBoss && Enraged) FireBolts(8);
                    // la gárgola encadena saltos cuando está furiosa
                    if (IsBoss && bossStyle == 2 && Enraged && chain < 2) { chain++; StartLeap(4.2f); }
                    else { chain = 0; Go(State.Recover); }
                }
                break;
            }
            case State.Spin:
            {
                float windup = telegraph, dur = IsBoss ? 2.6f : 1.8f;
                var sp = spinner != null ? spinner.GetComponent<EnemySpin>() : null;
                if (timer < windup)
                {
                    visual.localPosition = new Vector3(Mathf.Sin(timer * 70f) * 0.04f * Scale, 0f, 0f);
                    if (sp != null) sp.speed = Mathf.Lerp(60f, 900f, timer / windup);
                    break;
                }
                if (sp != null) sp.speed = 1100f;
                visual.Rotate(0f, 900f * dt, 0f, Space.Self);
                if (dist > 0.01f) facing = Vector3.Slerp(facing, toPlayer / dist, 2.5f * dt).normalized;
                transform.position += facing * (IsBoss ? 5f : 3.8f) * dt;
                // golpe en área cada poco (puede pegar varias veces)
                if (Mathf.Repeat(timer - windup, 0.4f) < dt)
                {
                    float r = radius + (IsBoss ? 2.2f : 1.3f);
                    CombatFx.Ring(transform.position, r, eyeColor, 0.15f);
                    if (playerAlive && ph != null && dist < r)
                        ph.ReceiveHit(new HitInfo { damage = IsBoss ? 12f : damage, knockback = 7f, hitStop = 0.03f, sourcePosition = transform.position, attacker = this });
                }
                if (timer >= windup + dur)
                {
                    if (sp != null) sp.speed = 60f;
                    // mareado: abre una ventana para castigarlo
                    if (!IsBoss) FloorDirector.Popup(transform.position + Vector3.up * (1.6f * Scale + 0.4f), "@_@", new Color(0.7f, 0.9f, 1f), 1f);
                    stunTime = IsBoss ? 0.6f : 1.3f;
                    Go(State.Stunned);
                }
                break;
            }
            case State.Blink:
            {
                // se desvanece, reaparece a un lado de Abby y suelta un abanico o lluvia
                if (timer < 0.3f) { visual.localScale = Vector3.one * (1f - timer / 0.3f); break; }
                if (timer - dt < 0.3f && target != null)
                {
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    Vector3 p = target.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(5f, 7f);
                    p.y = LandY(p);
                    if (!GoodLanding(p)) p = transform.position;   // no reaparece dentro de un muro
                    CombatFx.Burst(transform.position + Vector3.up, eyeColor, 18, 4f, 0.15f, 0.4f, 0f);
                    transform.position = p;
                    KeepInRoom();
                    CombatFx.Burst(transform.position + Vector3.up, eyeColor, 18, 4f, 0.15f, 0.4f, 0f);
                }
                visual.localScale = Vector3.one * Mathf.Clamp01((timer - 0.3f) / 0.3f);
                if (timer >= 0.65f)
                {
                    visual.localScale = Vector3.one;
                    if (IsBoss) { volley = 0; Go(State.Shoot); }
                    else Go(State.Recover);
                }
                break;
            }
            case State.Rain:
            {
                // brazos arriba mientras caen los golpes
                visual.localPosition = Vector3.up * Mathf.Sin(timer * 8f) * 0.08f * Scale;
                foreach (var e in eyes) SetColor(e, eyeColor * 9f);
                if (timer >= 0.9f) { foreach (var e in eyes) SetColor(e, eyeColor * 4f); Go(State.Recover); }
                break;
            }
        }
    }

    // ------------------------------------------------------------------ escudo

    /// <summary>El Escudero para de frente lo que no rompa la guardia. Devuelve true si bloqueó.</summary>
    bool Blocks(HitInfo hit)
    {
        if (kind != Kind.Escudero || state == State.Stunned) return false;
        Vector3 toSrc = hit.sourcePosition - transform.position; toSrc.y = 0f;
        if (toSrc.sqrMagnitude < 0.0001f || Vector3.Dot(facing, toSrc.normalized) < 0.3f) return false;   // por la espalda o de lado: entra
        if (hit.knockback >= 11f)
        {
            // guardia rota: queda abierto un rato
            stunTime = 1.6f;
            hitbox.End();
            Go(State.Stunned);
            CameraShake.Shake(0.15f, 0.2f);
            CombatFx.Burst(transform.position + Vector3.up * Scale, new Color(1f, 0.9f, 0.5f), 30, 7f, 0.18f, 0.5f);
            FloorDirector.Popup(transform.position + Vector3.up * (1.6f * Scale + 0.4f), "¡GUARDIA ROTA!", new Color(1f, 0.85f, 0.4f), 1.2f);
            return false;   // y además recibe el golpe
        }
        CombatFx.Burst(transform.position + Vector3.up * 0.8f * Scale + facing * 0.6f, new Color(1f, 0.95f, 0.7f), 10, 4f, 0.1f, 0.25f);
        FloorDirector.Popup(transform.position + Vector3.up * (1.6f * Scale + 0.3f), "¡BLOQUEO!", new Color(0.8f, 0.85f, 1f), 0.8f);
        knock = -toSrc.normalized * 2f;
        if (shield != null) { flash = 0f; SetColor(shield, Color.white * 2f); Invoke(nameof(ResetShield), 0.08f); }
        return true;
    }

    void ResetShield() { if (shield != null) SetColor(shield, new Color(0.55f, 0.5f, 0.4f)); }

    // ------------------------------------------------------------------ golpe marcado en el suelo

    /// <summary>Círculo de aviso en el suelo; al cumplirse el tiempo cae un cristal/carámbano y daña si Abby está dentro.</summary>
    class Blast : MonoBehaviour
    {
        float radius, delay, dmg, t; Color color; IHittable owner; Transform shard;
        static Material mat;

        public static void Drop(Vector3 pos, float radius, float delay, float damage, Color color, IHittable owner)
        {
            var go = new GameObject("GolpeMarcado");
            go.transform.position = pos;
            var b = go.AddComponent<Blast>();
            b.radius = radius; b.delay = delay; b.dmg = damage; b.color = color; b.owner = owner;
            CombatFx.Ring(pos, radius, new Color(color.r, color.g, color.b, 0.9f), delay);
            if (mat == null) mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { name = "Cristal (runtime)" };
            var s = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(s.GetComponent<Collider>());
            s.transform.SetParent(go.transform, false);
            s.transform.localScale = new Vector3(0.35f, 1.4f, 0.35f);
            s.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            s.transform.localPosition = Vector3.up * 14f;
            var r = s.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            var mpb = new MaterialPropertyBlock(); mpb.SetColor(BaseColorId, color * 2.5f); r.SetPropertyBlock(mpb);
            b.shard = s.transform;
        }

        void Update()
        {
            t += Time.deltaTime;
            // el cristal cae en los últimos 0.35 s (se ve venir)
            float fall = Mathf.Clamp01((t - (delay - 0.35f)) / 0.35f);
            if (shard != null) shard.localPosition = Vector3.up * Mathf.Lerp(14f, 0.6f, fall * fall);
            if (t < delay) return;
            var p = Hazards.Player;
            if (p != null)
            {
                Vector3 d = p.position - transform.position; d.y = 0f;
                if (d.magnitude < radius && Hazards.Health != null)
                    Hazards.Health.ReceiveHit(new HitInfo { damage = dmg, knockback = 5f, hitStop = 0.03f, sourcePosition = transform.position, attacker = owner });
            }
            CombatFx.Burst(transform.position + Vector3.up * 0.3f, color, 18, 5f, 0.16f, 0.45f, 1f);
            CameraShake.Shake(0.08f, 0.1f);
            Destroy(gameObject);
        }
    }
}
