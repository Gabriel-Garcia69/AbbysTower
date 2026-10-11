using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Objeto grande (columna, librero, muro del laberinto, estatua, bloque) que deja de dibujarse mientras tapa a Abby
/// desde la cámara, igual que los muros. Así al girar la cámara nada se interpone. La colisión no cambia.
/// </summary>
[DefaultExecutionOrder(150)]
public class OccluderFade : MonoBehaviour
{
    static readonly List<OccluderFade> all = new List<OccluderFade>();
    /// <summary>true = no se oculta nada (cámara "desde adentro": choca con las cosas en vez de atravesarlas).</summary>
    public static bool Suspended;
    static int lastFrame = -1;

    Renderer[] rends;
    Bounds bounds;
    bool hidden;

    void OnEnable() => all.Add(this);
    void OnDisable() { all.Remove(this); if (hidden) Show(true); }

    void Start()
    {
        rends = GetComponentsInChildren<Renderer>(true);
        bool any = false;
        foreach (var r in rends)
        {
            if (r is ParticleSystemRenderer) continue;
            if (!any) { bounds = r.bounds; any = true; } else bounds.Encapsulate(r.bounds);
        }
        bounds.Expand(0.1f);
    }

    void LateUpdate()
    {
        if (Time.frameCount == lastFrame) return;   // un solo objeto hace el trabajo de todos cada cuadro
        lastFrame = Time.frameCount;
        if (Suspended)
        {
            foreach (var o in all) if (o.hidden) o.Show(true);
            return;
        }
        var cam = Camera.main; var p = Hazards.Player;
        if (cam == null || p == null) return;
        Vector3 from = cam.transform.position;
        // dos rayos: a la cabeza y a la cintura de Abby
        Vector3 head = p.position + Vector3.up * 1.1f, waist = p.position + Vector3.up * 0.5f;
        float dh = Vector3.Distance(from, head), dw = Vector3.Distance(from, waist);
        var rh = new Ray(from, (head - from) / dh); var rw = new Ray(from, (waist - from) / dw);
        foreach (var o in all)
        {
            if (o.rends == null) continue;
            bool block = (o.bounds.IntersectRay(rh, out float a) && a < dh - 0.6f) || (o.bounds.IntersectRay(rw, out float b) && b < dw - 0.6f);
            if (block && o.bounds.Contains(waist)) block = false;   // está adentro (p. ej. en un bloque grande): no ocultar
            if (block != o.hidden) o.Show(!block);
        }
    }

    void Show(bool on)
    {
        hidden = !on;
        foreach (var r in rends) if (r != null && !(r is ParticleSystemRenderer)) r.forceRenderingOff = !on;
    }
}
