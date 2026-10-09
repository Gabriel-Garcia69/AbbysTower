using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Cerebro del piso en juego: prepara a Abby (vida + ataque), escucha las salas y dibuja un HUD provisional
/// (vida, enemigos restantes, barra del jefe, carteles, números de daño). También maneja la muerte (reinicia el piso)
/// y la pantalla de "piso completado". Ricardo puede reemplazar el HUD por la UI final; la lógica queda igual.
/// Lo crea FloorLayout dentro de Floor_Systems.
/// </summary>
[DefaultExecutionOrder(-50)]
public class FloorDirector : MonoBehaviour
{
    public string floorTitle = "PISO 1";
    public string floorSubtitle = "La Base de la Torre";
    [Tooltip("Escena a la que se vuelve al completar el piso (si no hay siguiente piso todavía).")]
    public string returnScene = "Exterior";
    [Tooltip("Material con el shader Abby/Particle (para que el shader de los efectos entre en el build).")]
    public Material fxMaterial;
    public float playerMaxHealth = 100f;

    public static FloorDirector Instance { get; private set; }

    struct PopupText { public Vector3 pos; public string text; public Color color; public float t; public float drift; public float life; }

    Transform player;
    PlayerHealth health;
    RoomEncounter[] rooms;
    RoomEncounter activeRoom;
    TowerEnemy boss;
    readonly List<PopupText> popups = new List<PopupText>();

    float fade = 1f, shownHealth = 1f, hurtVignette;
    float runStart; int kills;
    string bannerTop, bannerBottom; Color bannerColor; float bannerTime, bannerLength;
    bool dead, completed; float endTimer; string completeTime;
    float hintTimer = 9f;

    // ------------------------------------------------------------------ preparación

    void Awake()
    {
        Instance = this;
        if (fxMaterial != null) CombatFx.sourceMaterial = fxMaterial;
    }

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            health = p.GetComponent<PlayerHealth>();
            if (health == null) health = p.AddComponent<PlayerHealth>();
            health.SetMaxHealth(playerMaxHealth + RunState.MaxHealthBonus, true);   // mejoras compradas al mercader
            if (p.GetComponent<AbbyDash>() == null) p.AddComponent<AbbyDash>();
            if (p.GetComponent<AbbyAttack>() == null) p.AddComponent<AbbyAttack>();
            health.Died += OnPlayerDied;
            health.Damaged += _ => hurtVignette = 1f;
            shownHealth = health.Normalized;
        }

        if (GetComponent<PauseMenu>() == null) gameObject.AddComponent<PauseMenu>();

        rooms = FindObjectsByType<RoomEncounter>(FindObjectsSortMode.None);
        foreach (var r in rooms)
        {
            var room = r;
            room.Began += () => OnRoomBegan(room);
            room.Cleared += () => OnRoomCleared(room);
        }
        TowerEnemy.AnyDied += OnEnemyDied;
        TowerEnemy.BossEnraged += OnBossEnraged;
        RunState.Changed += OnRunChanged;

        runStart = Time.time;
        Banner(floorTitle, floorSubtitle, new Color(0.75f, 0.85f, 1f), 4f);
    }

    void OnDestroy()
    {
        TowerEnemy.AnyDied -= OnEnemyDied;
        TowerEnemy.BossEnraged -= OnBossEnraged;
        RunState.Changed -= OnRunChanged;
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    // ------------------------------------------------------------------ eventos

    void OnRoomBegan(RoomEncounter room)
    {
        activeRoom = room;
        if (room.isBoss)
        {
            Banner("GUARDIÁN DE LA BASE", "Derrótalo para abrir el camino", new Color(1f, 0.35f, 0.3f), 3.5f);
            CameraShake.Shake(0.3f, 0.8f);
        }
        else Banner("¡EMBOSCADA!", string.IsNullOrEmpty(room.displayName) ? "" : room.displayName, new Color(1f, 0.65f, 0.35f), 2.2f);
    }

    void OnRoomCleared(RoomEncounter room)
    {
        if (room != activeRoom) return;
        activeRoom = null; boss = null;
        if (room.isBoss) Banner("¡GUARDIÁN DERROTADO!", "La salida se ha abierto", new Color(1f, 0.85f, 0.45f), 4f);
        else if (room.Total > 0) Banner("SALA DESPEJADA", "Las puertas se abren", new Color(0.5f, 1f, 0.85f), 2f);
    }

    void OnEnemyDied(TowerEnemy e) => kills++;

    float fragPulse;
    void OnRunChanged() => fragPulse = 1f;

    void OnBossEnraged(TowerEnemy e) => Banner("¡EL GUARDIÁN SE ENFURECE!", "", new Color(1f, 0.3f, 0.2f), 2f);

    void OnPlayerDied()
    {
        dead = true; endTimer = 0f;
        lostFragments = RunState.Fragments - RunState.Fragments / 2;
        RunState.OnDeath();
    }

    int lostFragments;

    /// <summary>Lo llama FloorExit al pisar la salida del jefe (sin siguiente escena asignada).</summary>
    public void CompleteFloor()
    {
        if (completed) return;
        completed = true; endTimer = 0f;
        float secs = Time.time - runStart;
        completeTime = $"{(int)(secs / 60f)}:{(int)(secs % 60f):00}";
        if (player != null)
        {
            foreach (var b in player.GetComponents<MonoBehaviour>())
                if (b is PlayerInputReader || b is AbbyAttack) b.enabled = false;
        }
    }

    // ------------------------------------------------------------------ API para otros scripts

    public static void Popup(Vector3 worldPos, string text, Color color, float life = 0.9f)
    {
        if (Instance == null) return;
        Instance.popups.Add(new PopupText { pos = worldPos, text = text, color = color, t = 0f, drift = life > 1.5f ? 0f : Random.Range(-0.4f, 0.4f), life = life });
    }

    public static void Banner(string top, string bottom, Color color, float seconds)
    {
        if (Instance == null) return;
        Instance.bannerTop = top; Instance.bannerBottom = bottom; Instance.bannerColor = color;
        Instance.bannerTime = 0f; Instance.bannerLength = seconds;
    }

    // ------------------------------------------------------------------ ciclo

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        bannerTime += dt;
        if (hintTimer > 0f) hintTimer -= dt;
        hurtVignette = Mathf.MoveTowards(hurtVignette, 0f, dt * 2.5f);
        fragPulse = Mathf.MoveTowards(fragPulse, 0f, dt * 3f);
        if (health != null) shownHealth = Mathf.MoveTowards(shownHealth, health.Normalized, dt * 0.8f);
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            var p = popups[i]; p.t += dt; popups[i] = p;
            if (p.t > p.life) popups.RemoveAt(i);
        }

        if (activeRoom != null && activeRoom.isBoss && boss == null)
            foreach (var e in TowerEnemy.All) if (e.IsBoss) { boss = e; break; }

        if (dead || completed)
        {
            endTimer += dt;
            fade = dead ? Mathf.Clamp01((endTimer - 1.5f) / 1.2f) : Mathf.Min(0.75f, endTimer / 1.5f);
            if (dead && endTimer > 3f) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            if (completed && endTimer > 1.5f && Continue())
            {
                bool hasReturn = !string.IsNullOrEmpty(returnScene) && Application.CanStreamedLevelBeLoaded(returnScene);
                SceneManager.LoadScene(hasReturn ? returnScene : SceneManager.GetActiveScene().name);
            }
        }
        else fade = Mathf.MoveTowards(fade, 0f, dt / 1.2f);
    }

    static bool Continue()
    {
        var kb = Keyboard.current; var gp = Gamepad.current;
        return (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame))
            || (gp != null && (gp.buttonSouth.wasPressedThisFrame || gp.startButton.wasPressedThisFrame));
    }

    // ------------------------------------------------------------------ HUD

    void OnGUI()
    {
        float sw = Screen.width, sh = Screen.height, u = sh / 100f;
        var tex = Texture2D.whiteTexture;
        Color prev = GUI.color;
        var cam = Camera.main;

        // números de daño
        if (cam != null)
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 3.2f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            foreach (var p in popups)
            {
                Vector3 sp = cam.WorldToScreenPoint(p.pos + new Vector3(p.drift, Mathf.Min(p.t, 0.7f) * 1.2f, 0f));
                if (sp.z < 0f) continue;
                float a = 1f - Mathf.Clamp01((p.t - (p.life - 0.4f)) / 0.4f);
                float pop = 1f + Mathf.Max(0f, 0.25f - p.t) * 2f;
                st.fontSize = Mathf.RoundToInt(u * 3.2f * pop);
                Text(new Rect(sp.x - 400, sh - sp.y - 20, 800, 40), p.text, st, new Color(p.color.r, p.color.g, p.color.b, a));
            }

            // barritas sobre enemigos heridos (no jefe)
            foreach (var e in TowerEnemy.All)
            {
                if (e.IsBoss || Time.time - e.LastHitTime > 2.5f || e.Health <= 0f) continue;
                Vector3 sp = cam.WorldToScreenPoint(e.transform.position + Vector3.up * (e.kind == TowerEnemy.Kind.Bruto ? 2.8f : 1.9f));
                if (sp.z < 0f) continue;
                float w = u * (e.kind == TowerEnemy.Kind.Bruto ? 9f : 6f), h = u * 0.7f;
                var r = new Rect(sp.x - w / 2f, sh - sp.y, w, h);
                Box(r, new Color(0f, 0f, 0f, 0.6f));
                Box(new Rect(r.x + 1, r.y + 1, (r.width - 2) * e.Normalized, r.height - 2), e.eyeColor);
            }
        }

        // viñeta roja al recibir daño
        if (hurtVignette > 0f)
        {
            float vw = sw * 0.06f;
            Color vc = new Color(0.8f, 0.05f, 0.05f, 0.35f * hurtVignette);
            Box(new Rect(0, 0, vw, sh), vc); Box(new Rect(sw - vw, 0, vw, sh), vc);
            Box(new Rect(0, 0, sw, vw * 0.6f), vc); Box(new Rect(0, sh - vw * 0.6f, sw, vw * 0.6f), vc);
        }

        // vida de Abby
        if (health != null)
        {
            var frame = new Rect(u * 3f, u * 3f, u * 34f, u * 4.2f);
            var label = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 2.6f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            Text(new Rect(frame.x, frame.y - u * 0.2f, frame.width, u * 3f), "ABBY", label, new Color(1f, 0.95f, 0.85f));
            var bar = new Rect(frame.x, frame.y + u * 3f, frame.width, u * 2.2f);
            Box(new Rect(bar.x - 2, bar.y - 2, bar.width + 4, bar.height + 4), new Color(0f, 0f, 0f, 0.7f));
            Box(new Rect(bar.x, bar.y, bar.width * shownHealth, bar.height), new Color(1f, 0.85f, 0.55f, 0.9f));
            Color hc = Color.Lerp(new Color(1f, 0.25f, 0.3f), new Color(0.45f, 1f, 0.6f), health.Normalized);
            Box(new Rect(bar.x, bar.y, bar.width * health.Normalized, bar.height), hc);
            var num = new GUIStyle(label) { fontSize = Mathf.RoundToInt(u * 1.8f), alignment = TextAnchor.MiddleCenter };
            Text(bar, $"{Mathf.CeilToInt(health.Health)} / {Mathf.RoundToInt(health.maxHealth)}", num, Color.white);

            // fragmentos (moneda) y mejoras
            var frag = new GUIStyle(label) { fontSize = Mathf.RoundToInt(u * 2.4f) };
            float pulse = Mathf.Clamp01(fragPulse);
            string ups = (RunState.DamageLevel > 0 ? $"   Filo +{RunState.DamageLevel * 25}%" : "") + (RunState.HeartLevel > 0 ? $"   Corazón +{RunState.HeartLevel * 25}" : "");
            Text(new Rect(bar.x, bar.yMax + u * 0.8f, u * 60f, u * 3.5f), $"◆ {RunState.Fragments}" + ups, frag,
                 Color.Lerp(new Color(0.55f, 0.95f, 1f), Color.white, pulse));
        }

        // enemigos restantes
        if (activeRoom != null && !activeRoom.isBoss && activeRoom.Current == RoomEncounter.State.Combate)
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 2.6f), fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperRight };
            Text(new Rect(sw - u * 43f, u * 3f, u * 40f, u * 4f), $"Enemigos: {activeRoom.Remaining}", st, new Color(1f, 0.75f, 0.55f));
        }

        // barra del jefe
        if (boss != null && boss.Health > 0f)
        {
            float w = sw * 0.55f, h = u * 2.4f;
            var bar = new Rect((sw - w) / 2f, sh - u * 9f, w, h);
            var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 2.8f), fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerCenter };
            Text(new Rect(bar.x, bar.y - u * 4.2f, bar.width, u * 4f), boss.DisplayName.ToUpper() + (boss.Enraged ? "  ·  FURIA" : ""), st, new Color(1f, 0.8f, 0.75f));
            Box(new Rect(bar.x - 3, bar.y - 3, bar.width + 6, bar.height + 6), new Color(0f, 0f, 0f, 0.75f));
            Box(new Rect(bar.x, bar.y, bar.width * boss.Normalized, bar.height), boss.Enraged ? new Color(1f, 0.2f, 0.15f) : new Color(0.75f, 0.2f, 0.3f));
            foreach (float mark in new[] { 0.66f, 0.33f }) Box(new Rect(bar.x + bar.width * mark - 1, bar.y, 2, bar.height), new Color(1f, 1f, 1f, 0.5f));
        }

        // cartel central
        if (!string.IsNullOrEmpty(bannerTop) && bannerTime < bannerLength)
        {
            float a = Mathf.Clamp01(Mathf.Min(bannerTime / 0.3f, (bannerLength - bannerTime) / 0.6f));
            float slide = (1f - Mathf.Clamp01(bannerTime / 0.35f)) * u * 3f;
            Box(new Rect(0, sh * 0.2f - u * 1f, sw, u * 13f), new Color(0f, 0f, 0f, 0.35f * a));
            var big = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 6.5f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            var small = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 2.6f), fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter };
            Text(new Rect(slide, sh * 0.2f, sw, u * 8f), bannerTop, big, new Color(bannerColor.r, bannerColor.g, bannerColor.b, a));
            if (!string.IsNullOrEmpty(bannerBottom)) Text(new Rect(-slide, sh * 0.2f + u * 7.5f, sw, u * 4f), bannerBottom, small, new Color(1f, 1f, 1f, a * 0.85f));
        }

        // controles
        if (hintTimer > 0f && !dead && !completed)
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 2f), alignment = TextAnchor.LowerCenter };
            Text(new Rect(0, sh - u * 6f, sw, u * 4f), "WASD mover  ·  Espacio saltar  ·  Shift impulso  ·  Clic / J atacar (3 golpes)  ·  E comerciar  ·  Mouse cámara",
                 st, new Color(1f, 1f, 1f, 0.75f * Mathf.Clamp01(hintTimer)));
        }

        // negro (inicio, muerte, final)
        if (fade > 0f) Box(new Rect(0, 0, sw, sh), new Color(0f, 0f, 0f, fade));

        if (dead)
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 8f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            Text(new Rect(0, sh * 0.38f, sw, u * 12f), "CAÍSTE", st, new Color(1f, 0.35f, 0.35f, Mathf.Clamp01(endTimer / 0.8f)));
            var s2 = new GUIStyle(st) { fontSize = Mathf.RoundToInt(u * 2.6f), fontStyle = FontStyle.Italic };
            Text(new Rect(0, sh * 0.38f + u * 11f, sw, u * 4f), "La torre te devuelve a la entrada del piso..." + (lostFragments > 0 ? $"   (perdiste {lostFragments} ◆)" : ""), s2, new Color(1f, 1f, 1f, Mathf.Clamp01((endTimer - 0.6f) / 0.8f)));
        }

        if (completed)
        {
            float a = Mathf.Clamp01(endTimer / 1f);
            var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(u * 8f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            Text(new Rect(0, sh * 0.28f, sw, u * 12f), $"¡{floorTitle} COMPLETADO!", st, new Color(1f, 0.88f, 0.5f, a));
            var s2 = new GUIStyle(st) { fontSize = Mathf.RoundToInt(u * 3f), fontStyle = FontStyle.Normal };
            Text(new Rect(0, sh * 0.28f + u * 13f, sw, u * 5f), $"Tiempo  {completeTime}        Enemigos derrotados  {kills}        Fragmentos  {RunState.Fragments} ◆", s2, new Color(1f, 1f, 1f, a));
            if (endTimer > 1.5f)
            {
                var s3 = new GUIStyle(s2) { fontSize = Mathf.RoundToInt(u * 2.4f), fontStyle = FontStyle.Italic };
                float blink = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);
                Text(new Rect(0, sh * 0.28f + u * 21f, sw, u * 4f), "Enter / Espacio para continuar", s3, new Color(1f, 1f, 1f, blink));
            }
        }
        GUI.color = prev;

        void Box(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, tex); }
    }

    static void Text(Rect r, string text, GUIStyle st, Color c)
    {
        st.normal.textColor = new Color(0f, 0f, 0f, c.a * 0.75f);
        GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, st);
        st.normal.textColor = c;
        GUI.Label(r, text, st);
    }
}
