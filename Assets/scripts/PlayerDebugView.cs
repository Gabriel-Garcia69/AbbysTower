using UnityEngine;

/// <summary>
/// Visor de pruebas SIN animaciones: barras de vida/stamina, nombre del estado con un cuadro de color
/// (siempre visible) y, si el material lo permite, tiñe el sprite. Bórralo cuando tengas las animaciones reales.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerDebugView : MonoBehaviour
{
    public bool tintSprite = true;   // no funciona si el shader ignora el color del SpriteRenderer

    PlayerController pc;
    Color baseColor = Color.white;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
        if (pc.sprite != null) baseColor = pc.sprite.color;
    }

    static Color StateColor(PlayerState s)
    {
        switch (s)
        {
            case PlayerState.Attack: return new Color(1f, 0.55f, 0.1f);       // naranja
            case PlayerState.Dash:   return new Color(0.5f, 0.9f, 1f);        // celeste
            case PlayerState.Block:  return new Color(0.3f, 1f, 0.4f);        // verde
            case PlayerState.Hurt:   return new Color(1f, 0.2f, 0.2f);        // rojo
            case PlayerState.Dead:   return Color.gray;
            default:                 return Color.white;
        }
    }

    void LateUpdate()
    {
        if (!tintSprite || pc.sprite == null) return;
        Color c = pc.State == PlayerState.Move ? baseColor : StateColor(pc.State);
        if (pc.State == PlayerState.Dash) c.a = 0.5f;
        pc.sprite.color = c;
    }

    void OnGUI()
    {
        DrawBar(new Rect(20, 20, 220, 18), pc.Health01, new Color(0.85f, 0.15f, 0.2f));
        DrawBar(new Rect(20, 44, 220, 12), pc.Stamina01, new Color(0.9f, 0.8f, 0.2f));

        GUI.color = StateColor(pc.State);
        GUI.DrawTexture(new Rect(20, 64, 18, 18), Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.Label(new Rect(44, 62, 300, 24), pc.State.ToString());
    }

    static void DrawBar(Rect r, float t, Color fill)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = fill;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(t), r.height), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
