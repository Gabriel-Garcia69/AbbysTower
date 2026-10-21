using UnityEngine;

/// <summary>
/// Dante, el jefe final del GDD: un humanoide cubierto de sombra que reutiliza los cuadros de animación de Abby
/// (mismo esqueleto). Ataques: combo de 3 (espejo del de Abby), embestida larga y golpe de área señalado con una sombra
/// en el suelo. Al 50 % de vida acelera (Enraged). Al llegar a 0 no muere: cae y DanteFinal hace el cliffhanger.
/// </summary>
public partial class TowerEnemy
{
    bool ChooseDante(float dist)
    {
        Vector3 to = target != null ? target.position - transform.position : facing; to.y = 0f;
        lungeDir = to.sqrMagnitude > 0.01f ? to.normalized : facing;
        float vel = Enraged ? 0.75f : 1f;
        if (dist > 4.5f)
        {
            // embestida: cruza el área en línea recta; su sombra es larga
            lungeSpeed = 22f; lungeTime = 0.42f; chain = 0; telegraph = 0.7f;
            ShowLine();
            Go(State.Telegraph);
            cooldown = 1.1f * vel;
            return true;
        }
        if (attackCount % 3 == 0)
        {
            // golpe de área: salta y cae; el círculo morado en el suelo avisa dónde pega
            Go(State.Slam);
            telegraphFx = CombatFx.Ring(transform.position, 4.6f, new Color(0.75f, 0.3f, 1f, 0.9f), 1.05f * vel);
            cooldown = 1.2f * vel;
            return true;
        }
        // combo de 3 golpes cortos, como el de Abby
        lungeSpeed = 11f; lungeTime = 0.18f; chain = 2; telegraph = 0.55f;
        ShowLine();
        Go(State.Telegraph);
        cooldown = Random.Range(0.8f, 1.2f) * vel;
        return true;
    }

    /// <summary>La silueta de Abby (sus cuadros de animación) teñida de sombra, con un brillo morado.</summary>
    bool DanteVisual()
    {
        var p = GameObject.FindWithTag("Player");
        var anim = p != null ? p.GetComponentInChildren<AbbySpriteAnimator>(true) : null;
        var srAbby = anim != null ? anim.GetComponent<SpriteRenderer>() : null;
        if (anim == null || srAbby == null) return false;

        visual = new GameObject("Visual").transform;
        visual.SetParent(transform, false);
        var go = new GameObject("Cuerpo");
        go.transform.SetParent(transform, false);   // fuera de "Visual": Visual gira hacia donde mira, el sprite no
        go.transform.localScale = srAbby.transform.lossyScale * 1.18f;   // un poco más alto que Abby
        go.transform.localPosition = Vector3.up * Mathf.Max(0f, srAbby.transform.position.y - p.transform.position.y) * 1.18f;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = srAbby.sharedMaterial;
        sr.color = new Color(0.16f, 0.09f, 0.24f);
        sr.sprite = srAbby.sprite;
        var ds = go.AddComponent<DanteSprite>();
        ds.fuente = anim; ds.enemigo = this; ds.sr = sr; ds.baseLocal = go.transform.localPosition;
        body = null;
        eyes = new Renderer[0];
        var l = new GameObject("Brillo").AddComponent<Light>();
        l.transform.SetParent(visual, false);
        l.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        l.type = LightType.Point; l.color = new Color(0.7f, 0.3f, 1f); l.range = 7f; l.intensity = 4f; l.shadows = LightShadows.None;
        glow = l;
        return true;
    }

    /// <summary>Cuadros de Abby para Dante: caminar, embestir/atacar (pose del dash) y quieto, según la cámara.</summary>
    public class DanteSprite : MonoBehaviour
    {
        public AbbySpriteAnimator fuente;
        public TowerEnemy enemigo;
        public SpriteRenderer sr;
        public Vector3 baseLocal;
        Vector3 ultima;
        float paso, golpe;
        int dir = 6;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || fuente == null || enemigo == null || sr == null) return;
            // de frente a la cámara, derecho (como los sprites de Abby)
            Vector3 f = cam.transform.forward; f.y = 0f;
            if (f.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(f.normalized, Vector3.up);
            if (enemigo.visual != null) transform.localPosition = baseLocal + Vector3.up * enemigo.visual.localPosition.y;   // salta con el golpe de área

            Vector3 mov = enemigo.transform.position - ultima; mov.y = 0f;
            ultima = enemigo.transform.position;
            Vector3 mira = enemigo.facing.sqrMagnitude > 0.01f ? enemigo.facing : Vector3.back;
            Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
            float ang = Mathf.Atan2(Vector3.Dot(mira, f.normalized), Vector3.Dot(mira, r)) * Mathf.Rad2Deg;
            dir = ((Mathf.RoundToInt(ang / 45f) % 8) + 8) % 8;

            Sprite s = null;
            bool atacando = enemigo.state == State.Telegraph || enemigo.state == State.Lunge || enemigo.state == State.Slam;
            if (atacando && fuente.dashFrames != null && fuente.dashFrames.Length > dir && fuente.dashFrames[dir] != null && fuente.dashFrames[dir].Has)
            {
                golpe += Time.deltaTime;
                var fr = fuente.dashFrames[dir].frames;
                s = fr[Mathf.Clamp(enemigo.state == State.Telegraph ? 1 : 2 + Mathf.FloorToInt(golpe / 0.07f), 0, fr.Length - 1)];
            }
            else golpe = 0f;
            if (s == null && mov.magnitude > 0.004f && fuente.walk != null && fuente.walk.Length > dir && fuente.walk[dir] != null && fuente.walk[dir].Has)
            {
                paso += Time.deltaTime;
                var fr = fuente.walk[dir].frames;
                s = fr[Mathf.FloorToInt(paso * fuente.walkFps) % fr.Length];
            }
            if (s == null && fuente.idle != null && fuente.idle.Length > dir && fuente.idle[dir] != null) s = fuente.idle[dir];
            if (s != null) sr.sprite = s;
            // sombra viva: tiembla un poco; al enfurecer, un brillo morado
            float pulso = enemigo.Enraged ? 0.08f + 0.06f * Mathf.Sin(Time.time * 12f) : 0f;
            sr.color = new Color(0.16f + pulso, 0.09f, 0.24f + pulso * 2f);
        }
    }
}
