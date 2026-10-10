using UnityEngine;

/// <summary>
/// IRA — Respiradero de fuego: la rejilla se pone al rojo (aviso) y estalla una columna de fuego que quema.
/// Ciclo con desfase propio; en la sala del jefe los ciclos se acortan cuando él se enfurece.
/// </summary>
public class WrathVent : MonoBehaviour
{
    public float radius = 1.1f;
    public float damage = 18f;
    public float idle = 3f, warn = 0.9f, burst = 0.8f;
    public float offset;
    public ParticleSystem fire;
    public Renderer grate;
    public Light glow;

    float t;
    float lastHurt = -9f;
    MaterialPropertyBlock mpb;
    static readonly int ColorId = Shader.PropertyToID("_BaseColor");

    void Start() { t = offset; mpb = new MaterialPropertyBlock(); if (fire != null) fire.Stop(); }

    void Update()
    {
        float d = Hazards.FlatDistance(transform.position, out bool same);
        if (d > 16f) { if (fire != null && fire.isPlaying) fire.Stop(); return; }
        float speed = 1f;
        foreach (var e in TowerEnemy.All) if (e.IsBoss && e.Enraged) { speed = 1.5f; break; }
        float cycle = idle + warn + burst;
        t = (t + Time.deltaTime * speed) % cycle;
        float heat = 0f;
        bool erupting = false;
        if (t > idle && t < idle + warn) heat = (t - idle) / warn;
        else if (t >= idle + warn) { heat = 1f; erupting = true; }
        if (fire != null)
        {
            if (erupting && !fire.isPlaying) { fire.Play(); CameraShake.Shake(0.08f, 0.2f); }
            if (!erupting && fire.isPlaying) fire.Stop();
        }
        if (grate != null)
        {
            grate.GetPropertyBlock(mpb);
            mpb.SetColor(ColorId, Color.Lerp(new Color(0.25f, 0.06f, 0.03f), new Color(4f, 1.2f, 0.2f), heat * (erupting ? 1f : 0.6f + 0.4f * Mathf.Sin(Time.time * 25f))));
            grate.SetPropertyBlock(mpb);
        }
        if (glow != null) glow.intensity = heat * (erupting ? 14f : 5f);
        if (erupting && d < radius && same && Time.time - lastHurt > 0.5f)
        {
            lastHurt = Time.time;
            Hazards.Hurt(transform.position, damage, 6f);
        }
    }
}
