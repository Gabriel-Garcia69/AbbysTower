using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Salida del piso (en la sala del jefe) o portal (en el exterior). Está apagada hasta que se limpia la sala que la
/// desbloquea; al pisarla funde a negro y carga la siguiente escena. Sin escena asignada, avisa a FloorDirector
/// ("piso completado") o, si no hay director, solo muestra el mensaje.
/// </summary>
public class FloorExit : MonoBehaviour
{
    public RoomEncounter unlockedBy;   // vacío = siempre activa
    public string nextScene = "";
    public float radius = 1.3f;
    public Transform player;           // vacío = busca el tag "Player"
    public Color lockedColor = new Color(0.25f, 0.25f, 0.3f);
    public Color unlockedColor = new Color(0.3f, 1f, 0.9f);
    [Tooltip("Se enciende al desbloquearse (haz de luz, partículas...).")]
    public GameObject unlockedFx;
    public float fadeTime = 0.8f;

    Renderer rend;
    bool used, wasUnlocked;
    float messageTimer, fadeT = -1f;

    bool Unlocked => unlockedBy == null || unlockedBy.IsCleared;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (unlockedFx != null) unlockedFx.SetActive(Unlocked);
        wasUnlocked = Unlocked;
    }

    void Update()
    {
        bool unlocked = Unlocked;
        if (rend != null) rend.material.color = unlocked ? unlockedColor : lockedColor;
        if (unlocked != wasUnlocked)
        {
            wasUnlocked = unlocked;
            if (unlockedFx != null) unlockedFx.SetActive(unlocked);
            if (unlocked) CombatFx.Burst(transform.position + Vector3.up * 0.5f, unlockedColor, 60, 6f, 0.2f, 1.2f, -0.4f);
        }
        if (messageTimer > 0f) messageTimer -= Time.deltaTime;

        if (fadeT >= 0f)
        {
            fadeT += Time.unscaledDeltaTime;
            if (fadeT >= fadeTime) { fadeT = -1f; SceneManager.LoadScene(nextScene); }
            return;
        }
        if (used || !unlocked || player == null) return;

        Vector3 d = player.position - transform.position; d.y = 0f;
        if (d.magnitude > radius) return;

        used = true;
        if (!string.IsNullOrEmpty(nextScene)) fadeT = 0f;
        else if (FloorDirector.Instance != null) FloorDirector.Instance.CompleteFloor();
        else { Debug.Log("¡Piso completado!"); messageTimer = 4f; }
    }

    void OnGUI()
    {
        if (fadeT >= 0f)
        {
            Color prev = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, Mathf.Clamp01(fadeT / fadeTime));
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = prev;
        }
        if (messageTimer <= 0f) return;
        var style = new GUIStyle(GUI.skin.label) { fontSize = 32, alignment = TextAnchor.MiddleCenter };
        GUI.Label(new Rect(0, Screen.height * 0.4f, Screen.width, 60), "¡Piso completado!", style);
    }
}
