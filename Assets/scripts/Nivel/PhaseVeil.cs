using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Velo de Fase: pared de energía morada que bloquea a Abby caminando, pero se atraviesa con el impulso (AbbyDash).
/// Sirve para esconder cofres y atajos. La primera vez que Abby se acerca muestra la pista "Shift: atraviesa el velo".
/// Necesita un Collider (el muro) en el mismo objeto; el brillo late y destella cuando Abby lo cruza.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PhaseVeil : MonoBehaviour
{
    static readonly List<PhaseVeil> all = new List<PhaseVeil>();
    static bool hintShown;

    Collider col;
    Renderer[] rends;
    MaterialPropertyBlock mpb;
    float flash;
    Transform player;
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    public Color color = new Color(0.7f, 0.35f, 1f, 0.45f);

    void Awake()
    {
        col = GetComponent<Collider>();
        rends = GetComponentsInChildren<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    void OnEnable() => all.Add(this);
    void OnDisable() => all.Remove(this);

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;
    }

    /// <summary>Lo llama AbbyDash al empezar y terminar el impulso.</summary>
    public static void SetPassable(Collider playerCollider, bool passable)
    {
        foreach (var v in all)
        {
            if (v.col == null || playerCollider == null) continue;
            Physics.IgnoreCollision(playerCollider, v.col, passable);
            if (passable && v.player != null && v.col.bounds.SqrDistance(v.player.position) < 9f) v.flash = 1f;
        }
    }

    void Update()
    {
        if (flash > 0f) flash -= Time.deltaTime * 3f;
        float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 4f + transform.position.x);
        Color c = Color.Lerp(color * pulse, Color.white, Mathf.Clamp01(flash));
        c.a = Mathf.Lerp(color.a, 0.9f, Mathf.Clamp01(flash));
        mpb.SetColor(BaseColor, c);
        foreach (var r in rends) r.SetPropertyBlock(mpb);

        if (!hintShown && player != null && col.bounds.SqrDistance(player.position) < 3.5f * 3.5f)
        {
            hintShown = true;
            FloorDirector.Banner("VELO DE FASE", "Shift: atraviésalo con el impulso", new Color(0.8f, 0.55f, 1f), 3f);
        }
    }
}
