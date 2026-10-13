using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Recorte de muros: los muros de salas y pasillos (WallN/S/E/W, Pared*) que tapan a Abby o el suelo que tiene
/// alrededor se dejan de ver mientras estorban, junto con su tapa. Siguen proyectando sombra (ShadowsOnly),
/// así la luz de la sala no cambia, y la colisión es la misma.
/// Los muros ya se ocultan solos cuando la cámara queda detrás de ellos (son de una cara); esto cubre el caso
/// contrario: cámara alta que ve la cara del muro y detrás está Abby (puertas, pasillos, salas vecinas).
/// MouseOrbitCamera lo agrega solo a la cámara.
/// </summary>
[DefaultExecutionOrder(160)]
public class WallCutaway : MonoBehaviour
{
    [Tooltip("Segundos que un muro sigue oculto después de dejar de estorbar (evita parpadeos en el borde).")]
    [SerializeField] float holdTime = 0.25f;
    [Tooltip("Metros por delante de Abby (hacia arriba en pantalla) que se mantienen a la vista.")]
    [SerializeField] float clearRadius = 2.5f;

    class Wall
    {
        public Renderer rend, cap;
        public Bounds bounds;
        public UnityEngine.Rendering.ShadowCastingMode mode, capMode;
        public bool hidden;
        public float hideUntil;
    }

    readonly List<Wall> walls = new List<Wall>();
    readonly Vector3[] targets = new Vector3[7];
    Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        foreach (var r in FindObjectsByType<MeshRenderer>())
        {
            string n = r.name;
            if (!(n.StartsWith("Wall") || n.StartsWith("Pared")) || n.Contains("Roca")) continue;   // ParedRoca = acantilado del Exterior
            var w = new Wall { rend = r, bounds = r.bounds, mode = r.shadowCastingMode };
            w.bounds.Expand(new Vector3(0.05f, 0f, 0.05f));
            // la tapa (WallCap) va justo después de su muro en la jerarquía
            var t = r.transform; int i = t.GetSiblingIndex() + 1;
            if (t.parent != null && i < t.parent.childCount)
            {
                var next = t.parent.GetChild(i);
                if (next.GetComponent<WallCap>() != null) { w.cap = next.GetComponent<Renderer>(); if (w.cap != null) w.capMode = w.cap.shadowCastingMode; }
            }
            walls.Add(w);
        }
    }

    void OnDisable()
    {
        foreach (var w in walls) if (w.hidden) Show(w, true);
    }

    void LateUpdate()
    {
        var p = Hazards.Player;
        if (cam == null || p == null || walls.Count == 0) return;

        Vector3 from = cam.transform.position;
        Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
        Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
        Vector3 pos = p.position;
        targets[0] = pos + Vector3.up * 1.1f;
        targets[1] = pos + Vector3.up * 0.4f;
        targets[2] = pos + Vector3.up * 0.2f + f * clearRadius;
        targets[3] = pos + Vector3.up * 0.2f + f * clearRadius * 2f;
        targets[4] = pos + Vector3.up * 0.2f + r * 1.5f;   // a los lados poco: en un pasillo de 4 m no se recortan sus muros
        targets[5] = pos + Vector3.up * 0.2f - r * 1.5f;
        targets[6] = pos + Vector3.up * 0.2f + f * clearRadius * 1.5f;

        float now = Time.unscaledTime;
        float playerDepth = Vector3.Dot(pos, f);
        foreach (var w in walls)
        {
            if (w.rend == null) continue;
            // solo los muros que quedan entre la cámara y Abby; el del fondo de la sala nunca se recorta
            float wallDepth = Vector3.Dot(w.bounds.center, f) - Mathf.Abs(Vector3.Dot(w.bounds.extents, new Vector3(Mathf.Abs(f.x), 0f, Mathf.Abs(f.z))));
            if (wallDepth < playerDepth - 0.3f && Blocks(w.bounds, from)) w.hideUntil = now + holdTime;
            bool hide = now < w.hideUntil;
            if (hide != w.hidden) Show(w, !hide);
        }
    }

    bool Blocks(Bounds b, Vector3 from)
    {
        for (int i = 0; i < targets.Length; i++)
        {
            Vector3 d = targets[i] - from;
            float len = d.magnitude;
            if (b.IntersectRay(new Ray(from, d / len), out float hit) && hit < len - 0.3f) return true;
        }
        return false;
    }

    static void Show(Wall w, bool on)
    {
        w.hidden = !on;
        Set(w.rend, w.mode, on);
        if (w.cap != null) Set(w.cap, w.capMode, on);
    }

    /// <summary>Oculto = solo sombra; si el muro no daba sombra, simplemente no se dibuja.</summary>
    static void Set(Renderer r, UnityEngine.Rendering.ShadowCastingMode original, bool on)
    {
        if (r == null) return;   // al salir de Play o cambiar de escena el muro puede destruirse antes que la cámara
        if (original == UnityEngine.Rendering.ShadowCastingMode.Off) r.forceRenderingOff = !on;
        else r.shadowCastingMode = on ? original : UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
    }
}
