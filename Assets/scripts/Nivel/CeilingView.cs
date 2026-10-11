using UnityEngine;

/// <summary>
/// Techo de una sala o pasillo: si la cámara está por encima (cámaras altas, Exterior…) sus vigas y molduras dejan
/// de dibujarse; el plano de la bóveda ya es invisible desde arriba porque es de una cara. Desde adentro se ve todo.
/// </summary>
public class CeilingView : MonoBehaviour
{
    Renderer[] rends;
    bool hidden;

    void Start() => rends = GetComponentsInChildren<Renderer>(true);

    void LateUpdate()
    {
        var cam = Camera.main;
        if (cam == null || rends == null) return;
        bool hide = cam.transform.position.y > transform.position.y - 0.1f;
        if (hide == hidden) return;
        hidden = hide;
        foreach (var r in rends) if (r != null) r.forceRenderingOff = hide;
    }
}
