using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ENVIDIA — La Otra: una copia oscura de Abby que repite tu camino con unos segundos de retraso
/// (quiere ser tú, llegar a donde llegaste). Tocarla duele. No se puede matar; desaparece al limpiar la sala.
/// Moverte en círculos la hace chocar contigo: hay que avanzar.
/// </summary>
public class EnvyShadow : MonoBehaviour
{
    public float delay = 2.2f;
    public float damage = 10f;
    public float radius = 0.55f;

    struct Sample { public float t; public Vector3 pos; public Sprite sprite; }

    readonly Queue<Sample> trail = new Queue<Sample>();
    SpriteRenderer mine, source;
    float born;

    /// <summary>Crea la sombra detrás de Abby (copiando su sprite en cada cuadro).</summary>
    public static EnvyShadow Spawn(Transform parent)
    {
        var player = Hazards.Player;
        if (player == null) return null;
        var go = new GameObject("La Otra");
        go.transform.SetParent(parent, true);
        go.transform.position = player.position;
        var e = go.AddComponent<EnvyShadow>();
        e.source = player.GetComponentInChildren<SpriteRenderer>();
        var vis = new GameObject("Visual");
        vis.transform.SetParent(go.transform, false);
        if (e.source != null) vis.transform.localPosition = e.source.transform.localPosition;
        e.mine = vis.AddComponent<SpriteRenderer>();
        e.mine.color = new Color(0.12f, 0.55f, 0.25f, 0.85f);
        if (e.source != null) e.mine.sharedMaterial = e.source.sharedMaterial;
        var bb = vis.AddComponent<SpriteBillboard>();
        e.born = Time.time;
        CombatFx.Burst(player.position + Vector3.up, new Color(0.3f, 1f, 0.45f), 24, 3f, 0.12f, 0.8f);
        FloorDirector.Popup(player.position + Vector3.up * 2f, "\"Yo también quiero subir\"", new Color(0.5f, 1f, 0.6f), 2.2f);
        return e;
    }

    void Update()
    {
        var player = Hazards.Player;
        if (player == null || source == null) return;
        trail.Enqueue(new Sample { t = Time.time, pos = player.position, sprite = source.sprite });
        while (trail.Count > 0 && Time.time - trail.Peek().t > delay)
        {
            var s = trail.Dequeue();
            transform.position = s.pos;
            if (mine != null) { mine.sprite = s.sprite; }
        }
        if (mine != null)
        {
            var c = mine.color;
            c.a = Mathf.Clamp01((Time.time - born - delay) / 0.5f) * (0.7f + 0.15f * Mathf.Sin(Time.time * 6f));
            mine.color = c;
        }
        if (Time.time - born < delay + 0.3f) return;
        float d = Hazards.FlatDistance(transform.position, out bool same);
        if (d < radius && same) Hazards.Hurt(transform.position, damage, 5f);
    }
}
