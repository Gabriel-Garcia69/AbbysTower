using UnityEngine;

/// <summary>Muestra un cartel (FloorDirector.Banner) la primera vez que Abby entra al área (x, z) centrada en este objeto.</summary>
public class HintZone : MonoBehaviour
{
    public Vector2 size = new Vector2(10f, 10f);
    public string title = "";
    [TextArea] public string text = "";
    public Color color = new Color(0.45f, 1f, 0.9f);

    Transform player;
    bool shown;

    void Update()
    {
        if (shown) return;
        if (player == null) { var p = GameObject.FindWithTag("Player"); if (p == null) return; player = p.transform; }
        Vector3 lp = transform.InverseTransformPoint(player.position);
        if (Mathf.Abs(lp.x) < size.x / 2f && Mathf.Abs(lp.z) < size.y / 2f)
        {
            shown = true;
            FloorDirector.Banner(title, text, color, 4.5f);
        }
    }
}
