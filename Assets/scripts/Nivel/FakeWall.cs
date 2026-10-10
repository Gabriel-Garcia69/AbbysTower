using UnityEngine;

/// <summary>
/// Tramo de muro falso (sin collider) frente a un escondite. Cuando Abby está adentro del escondite, el muro deja de
/// dibujarse para que se vea a sí misma (si no, quedaría tapada por el muro).
/// </summary>
public class FakeWall : MonoBehaviour
{
    [Tooltip("Centro (en el espacio del padre: la sala) y tamaño del escondite.")]
    public Vector3 nicheCenter;
    public Vector2 nicheSize = new Vector2(2f, 2.8f);

    Renderer rend;

    void Awake() => rend = GetComponent<Renderer>();

    void LateUpdate()
    {
        if (rend == null || Hazards.Player == null || transform.parent == null) return;
        Vector3 lp = transform.parent.InverseTransformPoint(Hazards.Player.position) - nicheCenter;
        bool inside = Mathf.Abs(lp.x) < nicheSize.x / 2f && Mathf.Abs(lp.z) < nicheSize.y / 2f + 0.4f && lp.y > -1f && lp.y < 3f;
        // ojo: AreaReveal usa forceRenderingOff; aquí usamos enabled para no pelearnos con la sala
        rend.enabled = !inside;
    }
}
