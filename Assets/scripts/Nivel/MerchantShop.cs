using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mercader de la sala de descanso: al acercarse aparece "E: comerciar". El menú pausa a Abby y ofrece:
/// 1) Poción (cura 50), 2) Filo de Runa (+25% daño, sube de precio), 3) Corazón de Torre (+25 vida máx., sube de precio).
/// Se paga con fragmentos (RunState). Se cierra con E, Esc o el botón Salir. HUD provisional con OnGUI.
/// </summary>
public class MerchantShop : MonoBehaviour
{
    public float radius = 2.8f;
    public int potionCost = 12;
    public int damageBaseCost = 30;
    public int heartBaseCost = 35;
    public int costStep = 15;
    public int maxLevel = 3;

    Transform player;
    PlayerHealth health;
    bool open, near;
    string feedback; float feedbackTimer;
    Behaviour[] paused;

    int DamageCost => damageBaseCost + costStep * RunState.DamageLevel;
    int HeartCost => heartBaseCost + costStep * RunState.HeartLevel;

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) { player = p.transform; health = p.GetComponent<PlayerHealth>(); }
    }

    void Update()
    {
        if (player == null || PauseMenu.IsPaused) return;
        if (health == null) health = player.GetComponent<PlayerHealth>();
        Vector3 d = player.position - transform.position; d.y = 0f;
        near = d.magnitude < radius && (health == null || !health.IsDead);
        if (feedbackTimer > 0f) feedbackTimer -= Time.unscaledDeltaTime;

        var kb = Keyboard.current; var gp = Gamepad.current;
        bool interact = (kb != null && kb.eKey.wasPressedThisFrame) || (gp != null && gp.buttonNorth.wasPressedThisFrame);
        bool back = (kb != null && kb.escapeKey.wasPressedThisFrame) || (gp != null && gp.buttonEast.wasPressedThisFrame);

        if (!open)
        {
            if (near && interact) SetOpen(true);
            return;
        }
        if (!near || back || interact) { SetOpen(false); return; }
        if (kb != null)
        {
            if (kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame) Buy(0);
            if (kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame) Buy(1);
            if (kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame) Buy(2);
        }
    }

    void OnDisable() { if (open) SetOpen(false); PauseMenu.Blocked = false; }

    void LateUpdate() { if (!open) PauseMenu.Blocked = false; }

    void SetOpen(bool value)
    {
        open = value;
        if (value) PauseMenu.Blocked = true;
        if (value)
        {
            // congela a Abby y la cámara mientras compra
            var list = new System.Collections.Generic.List<Behaviour>();
            foreach (var b in player.GetComponents<Behaviour>())
                if (b is PlayerInputReader || b is AbbyAttack) list.Add(b);
            foreach (var b in FindObjectsByType<MouseOrbitCamera>(FindObjectsSortMode.None)) list.Add(b);
            paused = list.ToArray();
            foreach (var b in paused) b.enabled = false;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        }
        else if (paused != null)
        {
            foreach (var b in paused) if (b != null) b.enabled = true;
            paused = null;
        }
    }

    void Buy(int item)
    {
        switch (item)
        {
            case 0:
                if (health != null && health.Health >= health.maxHealth) { Say("Ya tienes la vida al máximo."); return; }
                if (!RunState.Spend(potionCost)) { Say("No te alcanzan los fragmentos."); return; }
                health.Heal(50f);
                Say("¡Poción bebida! +50 de vida");
                break;
            case 1:
                if (RunState.DamageLevel >= maxLevel) { Say("Tu filo ya no se puede mejorar más."); return; }
                if (!RunState.Spend(DamageCost)) { Say("No te alcanzan los fragmentos."); return; }
                RunState.BuyDamage();
                Say($"Filo de Runa nivel {RunState.DamageLevel}: +{RunState.DamageLevel * 25}% de daño");
                break;
            case 2:
                if (RunState.HeartLevel >= maxLevel) { Say("Tu corazón ya no se puede mejorar más."); return; }
                if (!RunState.Spend(HeartCost)) { Say("No te alcanzan los fragmentos."); return; }
                RunState.BuyHeart();
                if (health != null) { health.maxHealth += 25f; health.Heal(25f); }
                Say($"Corazón de Torre nivel {RunState.HeartLevel}: +25 de vida máxima");
                break;
        }
        CombatFx.Burst(transform.position + Vector3.up * 1.5f, new Color(1f, 0.85f, 0.4f), 25, 3f, 0.14f, 0.7f, -0.2f);
    }

    void Say(string text) { feedback = text; feedbackTimer = 2.5f; }

    void OnGUI()
    {
        float sw = Screen.width, sh = Screen.height, u = sh / 100f;
        Color prev = GUI.color;
        var tex = Texture2D.whiteTexture;

        if (near && !open)
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 2.8f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(sw / 2f - u * 14f, sh * 0.72f, u * 28f, u * 5f), tex);
            GUI.color = Color.white;
            st.normal.textColor = new Color(1f, 0.9f, 0.6f);
            GUI.Label(new Rect(sw / 2f - u * 14f, sh * 0.72f, u * 28f, u * 5f), "E: comerciar", st);
        }

        if (open)
        {
            float w = u * 70f, h = u * 52f;
            var panel = new Rect((sw - w) / 2f, (sh - h) / 2f, w, h);
            GUI.color = new Color(0.05f, 0.04f, 0.08f, 0.92f); GUI.DrawTexture(panel, tex);
            GUI.color = new Color(0.8f, 0.62f, 0.3f, 1f);
            GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 3), tex);
            GUI.DrawTexture(new Rect(panel.x, panel.yMax - 3, panel.width, 3), tex);
            GUI.color = Color.white;

            var title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 4.2f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = new Color(1f, 0.85f, 0.5f);
            GUI.Label(new Rect(panel.x, panel.y + u * 2f, panel.width, u * 6f), "EL MERCADER", title);
            var sub = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 2.2f), fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter };
            sub.normal.textColor = new Color(1f, 1f, 1f, 0.75f);
            GUI.Label(new Rect(panel.x, panel.y + u * 7.5f, panel.width, u * 4f), $"\"Los fragmentos de la torre valen oro aquí arriba...\"     Tienes: {RunState.Fragments} ◆", sub);

            string[] names = { "Poción de vida", "Filo de Runa", "Corazón de Torre" };
            string[] desc =
            {
                "Cura 50 de vida.",
                RunState.DamageLevel >= maxLevel ? "Nivel máximo." : $"+25% de daño (nivel {RunState.DamageLevel}/{maxLevel}).",
                RunState.HeartLevel >= maxLevel ? "Nivel máximo." : $"+25 de vida máxima (nivel {RunState.HeartLevel}/{maxLevel}).",
            };
            int[] costs = { potionCost, DamageCost, HeartCost };
            bool[] maxed = { false, RunState.DamageLevel >= maxLevel, RunState.HeartLevel >= maxLevel };
            var nameSt = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 2.8f), fontStyle = FontStyle.Bold };
            var descSt = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 2.1f) };
            var btn = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(u * 2.4f), fontStyle = FontStyle.Bold };
            for (int i = 0; i < 3; i++)
            {
                var row = new Rect(panel.x + u * 4f, panel.y + u * (14f + i * 9.5f), panel.width - u * 8f, u * 8f);
                GUI.color = new Color(1f, 1f, 1f, 0.06f); GUI.DrawTexture(row, tex); GUI.color = Color.white;
                bool afford = RunState.Fragments >= costs[i] && !maxed[i];
                nameSt.normal.textColor = afford ? Color.white : new Color(1f, 1f, 1f, 0.5f);
                descSt.normal.textColor = new Color(0.8f, 0.85f, 0.9f, afford ? 0.9f : 0.5f);
                GUI.Label(new Rect(row.x + u * 2f, row.y + u * 0.6f, row.width * 0.6f, u * 4f), $"{i + 1}. {names[i]}", nameSt);
                GUI.Label(new Rect(row.x + u * 2f, row.y + u * 4.2f, row.width * 0.6f, u * 3.5f), desc[i], descSt);
                GUI.enabled = afford;
                if (GUI.Button(new Rect(row.xMax - u * 18f, row.y + u * 1.5f, u * 16f, u * 5f), maxed[i] ? "—" : $"{costs[i]} ◆", btn)) Buy(i);
                GUI.enabled = true;
            }
            if (GUI.Button(new Rect(panel.center.x - u * 8f, panel.yMax - u * 8f, u * 16f, u * 5f), "Salir (E)", btn)) SetOpen(false);
            if (feedbackTimer > 0f)
            {
                var fb = new GUIStyle(sub) { fontStyle = FontStyle.Bold };
                fb.normal.textColor = new Color(1f, 0.9f, 0.6f, Mathf.Clamp01(feedbackTimer));
                GUI.Label(new Rect(panel.x, panel.yMax - u * 13f, panel.width, u * 4f), feedback, fb);
            }
        }
        GUI.color = prev;
    }
}
