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
    [Tooltip("Nombre que muestra el HUD sobre la barra de vida.")]
    public string heroName = "ABBY";

    public string floorTitle = "PISO 1";
    public string floorSubtitle = "La Base de la Torre";
    [Tooltip("Escena a la que se vuelve al completar el piso (si no hay siguiente piso todavía).")]
    public string returnScene = "Exterior";
    [Tooltip("Material con el shader Abby/Particle (para que el shader de los efectos entre en el build).")]
    public Material fxMaterial;
    public float playerMaxHealth = 100f;
    [Tooltip("Siguiente piso (al terminar este). Vacío = vuelve a returnScene.")]
    public string nextScene = "";
    [Tooltip("Más abajo que esto = cayó al vacío (reaparece en el último suelo firme).")]
    public float killY = -3f;
    [Tooltip("Pecado del piso: niebla, luz ambiente y color de los enemigos (Base = lo que tenga la escena).")]
    public FloorTheme theme = FloorTheme.Base;

    public static FloorDirector Instance { get; private set; }

    struct PopupText { public Vector3 pos; public string text; public Color color; public float t; public float drift; public float life; }

    Transform player;
    PlayerHealth health;
    AbbyBlock block;
    Sprite retrato, retratoFijo, retratoMedido;
    Rect uvRetrato;
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
        if (heroName == "ABIS") heroName = Historia.Protagonista.ToUpper();   // escenas guardadas antes del GDD
        if (fxMaterial != null) CombatFx.sourceMaterial = fxMaterial;
        if (theme != FloorTheme.Base)
        {
            var p = FloorThemes.Get(theme);
            FloorThemes.ApplyAtmosphere(theme);
            TowerEnemy.ThemeBody = p.enemyBody;
            TowerEnemy.ThemeEye = p.enemyEye;
        }
        else { TowerEnemy.ThemeBody = null; TowerEnemy.ThemeEye = null; }
    }

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            health = p.GetComponent<PlayerHealth>();
            if (health == null) health = p.AddComponent<PlayerHealth>();
            health.SetMaxHealth(playerMaxHealth, true);   // GDD: vida 100, sin mejoras
            // la vida con la que salió de la escena anterior (la stamina siempre empieza llena); el tutorial (Dante) no arrastra nada
            string escena = SceneManager.GetActiveScene().name;
            if (escena == "Expedicion") RunState.ClearHealth();
            else if (RunState.HasSavedHealth) health.SetHealth(RunState.SavedHealth);
            if (p.GetComponent<AbbyDash>() == null) p.AddComponent<AbbyDash>();
            var fall = p.GetComponent<FallGuard>(); if (fall == null) fall = p.AddComponent<FallGuard>();
            fall.killY = killY;
            if (p.GetComponent<AbbyBlock>() == null) p.AddComponent<AbbyBlock>();
            if (p.GetComponent<Pasos>() == null) p.AddComponent<Pasos>();
            if (p.GetComponent<AbbyAttack>() == null) p.AddComponent<AbbyAttack>();
            block = p.GetComponent<AbbyBlock>();
            var anim = p.GetComponentInChildren<AbbySpriteAnimator>(true);
            retrato = retratoFijo != null ? retratoFijo : anim != null ? anim.Retrato : null;
            health.Died += OnPlayerDied;
            health.Damaged += _ => hurtVignette = 1f;
            shownHealth = health.Normalized;
        }

        if (GetComponent<PauseMenu>() == null) gameObject.AddComponent<PauseMenu>();
        if (GetComponent<DanteFinal>() == null) gameObject.AddComponent<DanteFinal>();   // final de la demo (si el piso tiene a Dante)

        rooms = FindObjectsByType<RoomEncounter>();
        // GDD: si Abby cayó peleando con el jefe, se reinicia el combate con el jefe (las hordas ya están limpias)
        if (ReintentoJefe.Escena == SceneManager.GetActiveScene().name)
        {
            RoomEncounter jefe = null;
            foreach (var r in rooms) { if (r.isBoss) jefe = r; else r.DebugClear(); }
            if (jefe != null && player != null)
            {
                Vector3 pos = jefe.transform.position - jefe.transform.forward * (jefe.size.y / 2f + 2.5f);
                if (Physics.Raycast(pos + Vector3.up * 20f, Vector3.down, out var hit, 60f)) pos = hit.point + Vector3.up * 0.1f;
                var cc = player.GetComponent<CharacterController>();
                if (cc != null) cc.enabled = false;
                player.position = pos;
                if (cc != null) cc.enabled = true;
            }
            floorSubtitle = "Reintenta contra el jefe";
            // sin la toma de apertura: está pensada para la entrada del mapa y, junto al jefe, la cámara miraba al vacío
            foreach (var s in FindObjectsByType<CameraShot>()) if (s.introSeconds > 0f) s.Terminada = true;
        }
        foreach (var r in rooms)
        {
            var room = r;
            room.Began += () => OnRoomBegan(room);
            room.Cleared += () => OnRoomCleared(room);
            room.Escaped += () => OnRoomEscaped(room);
        }
        TowerEnemy.AnyDied += OnEnemyDied;
        TowerEnemy.BossEnraged += OnBossEnraged;
        RunState.Changed += OnRunChanged;

        runStart = Time.time;
        guia = gameObject.AddComponent<GuiaHordas>();   // baliza y flecha hacia la siguiente pelea
        RanurasEscaleras.Revisar();                     // sin zanjas junto a las escaleras donde Abby se atoraba
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
        if (!room.isBoss) Musica.Pelea(true);
        if (room.isBoss)
        {
            Banner((string.IsNullOrEmpty(room.bossName) ? "Guardián" : room.bossName).ToUpper(), "Derrótalo para abrir el camino", new Color(1f, 0.35f, 0.3f), 3.5f);
            CameraShake.Shake(0.3f, 0.8f);
        }
        else Banner("¡EMBOSCADA!", string.IsNullOrEmpty(room.displayName) ? "" : room.displayName, new Color(1f, 0.65f, 0.35f), 2.2f);
    }

    GuiaHordas guia;

    void OnRoomCleared(RoomEncounter room)
    {
        if (room != activeRoom) return;
        activeRoom = null; boss = null;
        Musica.Pelea(false);
        if (room.isBoss) Banner("¡JEFE DERROTADO!", "La salida se ha abierto", new Color(1f, 0.85f, 0.45f), 4f);
        else if (room.Total > 0)
        {
            // cuántas hordas faltan y hacia dónde seguir (la flecha y la baliza marcan la siguiente)
            int faltan = guia != null ? guia.Restantes : 0;
            string titulo = (string.IsNullOrEmpty(room.displayName) ? "HORDA" : room.displayName.ToUpper()) + " DERROTADA";
            if (faltan > 0) Banner(titulo, faltan == 1 ? "Falta 1 horda: sigue la luz morada" : $"Faltan {faltan} hordas: sigue la luz morada", new Color(0.5f, 1f, 0.85f), 3f);
            else if (guia != null && guia.JefePendiente) Banner("¡HORDAS DERROTADAS!", "Sigue la luz: algo te espera", new Color(1f, 0.55f, 0.85f), 3.5f);
            else Banner("¡PISO DESPEJADO!", "Sigue la luz hasta la salida", new Color(1f, 0.85f, 0.45f), 3.5f);
        }
    }

    void OnRoomEscaped(RoomEncounter room)
    {
        if (room != activeRoom) return;
        activeRoom = null; boss = null;
        Musica.Pelea(false);
        Banner("TE ALEJASTE", "Los enemigos volvieron a su guarida", new Color(0.7f, 0.8f, 1f), 1.8f);
    }

    void OnEnemyDied(TowerEnemy e) => kills++;

    float fragPulse;
    void OnRunChanged() => fragPulse = 1f;

    void OnBossEnraged(TowerEnemy e) => Banner("¡" + e.DisplayName.ToUpper() + " SE ENFURECE!", "", new Color(1f, 0.3f, 0.2f), 2f);

    void OnPlayerDied()
    {
        RunState.ClearHealth();   // el reintento arranca con la vida completa
        dead = true; endTimer = 0f;
    }

    /// <summary>Escena del menú principal (Esc en la pantalla de derrota).</summary>
    public const string MenuScene = "Menu";

    /// <summary>Lo llama FloorExit al pisar la salida del jefe (sin siguiente escena asignada).</summary>
    public void CompleteFloor(string next = null)
    {
        if (completed) return;
        if (!string.IsNullOrEmpty(next)) nextScene = next;
        completed = true; endTimer = 0f;
        if (health != null && !health.IsDead) RunState.SaveHealth(health.Health);   // la vida pasa a la escena siguiente
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

    /// <summary>Encuadre del retrato: un cuadrado centrado en la cabeza. El lienzo no sirve de referencia porque la espada corre al personaje a un lado.</summary>
    void MedirRetrato()
    {
        retratoMedido = retrato;
        if (retrato == null) return;
        var tx = retrato.texture; var tr = retrato.textureRect;
        // las texturas de los sprites no se pueden leer directo: se copian a una temporal
        var rt = RenderTexture.GetTemporary(tx.width, tx.height, 0, RenderTextureFormat.ARGB32);
        var antes = RenderTexture.active;
        Graphics.Blit(tx, rt);
        RenderTexture.active = rt;
        var copia = new Texture2D(tx.width, tx.height, TextureFormat.RGBA32, false);
        copia.ReadPixels(new Rect(0, 0, tx.width, tx.height), 0, 0);
        copia.Apply();
        RenderTexture.active = antes;
        RenderTexture.ReleaseTemporary(rt);
        var px = copia.GetPixels32();
        int x0 = (int)tr.x, y0 = (int)tr.y, w = (int)tr.width, h = (int)tr.height, arriba = -1, abajo = -1;
        for (int y = y0 + h - 1; y >= y0; y--)
            for (int x = x0; x < x0 + w; x++)
                if (px[y * tx.width + x].a > 128) { if (arriba < 0) arriba = y; abajo = y; break; }
        Destroy(copia);
        if (arriba < 0) { uvRetrato = new Rect(tr.x / tx.width, tr.y / tx.height, tr.width / tx.width, tr.height / tx.height); return; }
        float alto = arriba - abajo + 1;
        // la cabeza: lo más ancho de la franja de arriba; la espada va abajo y no cuenta
        int izq = int.MaxValue, der = int.MinValue, fin = Mathf.RoundToInt(arriba - alto * 0.28f);
        for (int y = arriba; y >= fin; y--)
            for (int x = x0; x < x0 + w; x++)
                if (px[y * tx.width + x].a > 128) { if (x < izq) izq = x; if (x > der) der = x; }
        float centro = (izq + der + 1) / 2f, lado = alto * 0.62f, techo = arriba + 1 + alto * 0.03f;
        uvRetrato = new Rect((centro - lado / 2f) / tx.width, (techo - lado) / tx.height, lado / tx.width, lado / tx.height);
    }

    void Update()
    {
        if (retrato != retratoMedido) MedirRetrato();
        float dt = Time.unscaledDeltaTime;
        if (player != null) Shader.SetGlobalVector("_AbbyJugador", player.position);   // los límites del piso se encienden cerca de Abby
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
            // GDD: pantalla de derrota → Reintentar reinicia el piso actual (o el jefe)
            if (dead && endTimer > 1.6f && Continue()) Cargador.Ir(SceneManager.GetActiveScene().name);
            if (dead && endTimer > 1.6f && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && Application.CanStreamedLevelBeLoaded(MenuScene))
                Cargador.Ir(MenuScene);
            if (completed && endTimer > 1.5f && Continue())
            {
                bool hasNext = !string.IsNullOrEmpty(nextScene) && Application.CanStreamedLevelBeLoaded(nextScene);
                bool hasReturn = !string.IsNullOrEmpty(returnScene) && Application.CanStreamedLevelBeLoaded(returnScene);
                Cargador.Ir(hasNext ? nextScene : hasReturn ? returnScene : SceneManager.GetActiveScene().name);
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
        if (DanteFinal.Activo) return;   // la escena final usa toda la pantalla
        if (PauseMenu.IsPaused) return;   // en pausa, la pausa ocupa la pantalla (el emblema va donde está el retrato)
        float sw = Screen.width, sh = Screen.height, u = sh / 100f;
        var tex = Texture2D.whiteTexture;
        Color prev = GUI.color;
        var cam = Camera.main;

        // números de daño
        if (cam != null)
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 3.2f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            foreach (var p in popups)
            {
                Vector3 sp = cam.WorldToScreenPoint(p.pos + new Vector3(p.drift, Mathf.Min(p.t, 0.7f) * 1.2f, 0f));
                if (sp.z < 0f) continue;
                float a = 1f - Mathf.Clamp01((p.t - (p.life - 0.4f)) / 0.4f);
                float pop = 1f + Mathf.Max(0f, 0.25f - p.t) * 2f;
                st.fontSize = FuentePixel.Tam(u * 3.2f * pop);
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

        // GDD: retrato de Abby arriba a la izquierda, barra de vida y barra de stamina
        if (health != null)
        {
            float px = u * 3f, py = u * 3f, ps = u * 10f;
            // marco del HUD con el mismo estilo de los cuadros de texto (Octopath)
            CuadroNarracion.Marco(new Rect(px - u * 1.2f, py - u * 1.2f, ps + u * 34.5f, ps + u * 2.4f), 0.82f);
            var marco = new Rect(px, py, ps, ps);
            Box(new Rect(marco.x - 3, marco.y - 3, marco.width + 6, marco.height + 6), new Color(0.75f, 0.6f, 1f, 0.9f));
            Box(marco, new Color(0.08f, 0.06f, 0.14f, 0.92f));
            if (retrato != null && retrato == retratoMedido)
            {
                GUI.color = Color.white;
                GUI.DrawTextureWithTexCoords(new Rect(marco.x + u * 0.5f, marco.y + u * 0.5f, marco.width - u, marco.height - u), retrato.texture, uvRetrato, true);
            }
            var label = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 2.6f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
            float bx = marco.xMax + u * 1.5f, bw = u * 30f;
            Text(new Rect(bx, py - u * 0.4f, bw, u * 3f), heroName, label, new Color(1f, 0.95f, 0.85f));
            var bar = new Rect(bx, py + u * 3f, bw, u * 2.4f);
            bool baja = health.Normalized < 0.3f && !health.IsDead;
            float parpadeo = baja ? 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 10f) : 1f;   // vida baja: la barra parpadea
            Box(new Rect(bar.x - 2, bar.y - 2, bar.width + 4, bar.height + 4), new Color(0f, 0f, 0f, 0.7f));
            Box(new Rect(bar.x, bar.y, bar.width * shownHealth, bar.height), new Color(1f, 0.85f, 0.55f, 0.9f));
            Color hc = Color.Lerp(new Color(1f, 0.25f, 0.3f), new Color(0.45f, 1f, 0.6f), health.Normalized);
            Box(new Rect(bar.x, bar.y, bar.width * health.Normalized, bar.height), new Color(hc.r, hc.g, hc.b, parpadeo));
            var num = new GUIStyle(label) { fontSize = FuentePixel.Tam(u * 1.8f), alignment = TextAnchor.MiddleCenter, padding = new RectOffset(), margin = new RectOffset() };
            Text(bar, $"VIDA  {Mathf.CeilToInt(health.Health)} / {Mathf.RoundToInt(health.maxHealth)}", num, Color.white);
            if (block != null)
            {
                var sb = new Rect(bx, bar.yMax + u * 1.1f, bw * 0.8f, u * 2.3f);
                // stamina baja (no alcanza para bloquear otro golpe): cambia de color; si intentó bloquear sin stamina, parpadea
                bool poca = block.Stamina < AbbyBlock.CostoPorGolpe * 2f;
                Color sc = block.Stamina < AbbyBlock.CostoPorGolpe ? new Color(1f, 0.35f, 0.25f) : poca ? new Color(1f, 0.7f, 0.25f) : new Color(0.45f, 0.75f, 1f);
                if (block.Blocking) sc = Color.Lerp(sc, Color.white, 0.35f);
                Box(new Rect(sb.x - 2, sb.y - 2, sb.width + 4, sb.height + 4), new Color(0f, 0f, 0f, 0.7f + block.EmptyFlash * 0.3f));
                Box(new Rect(sb.x, sb.y, sb.width * block.Normalized, sb.height), sc);
                var sn = new GUIStyle(num) { fontSize = FuentePixel.Tam(u * 1.6f) };
                Text(sb, "STAMINA", sn, new Color(1f, 1f, 1f, 0.9f));
                if (block.BlockFlash > 0f) Box(new Rect(marco.x - 3, marco.y - 3, marco.width + 6, marco.height + 6), new Color(0.6f, 0.8f, 1f, 0.5f * block.BlockFlash));
            }
        }

        // enemigos restantes
        if (activeRoom != null && !activeRoom.isBoss && activeRoom.Current == RoomEncounter.State.Combate)
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 2.6f), fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperRight };
            Text(new Rect(sw - u * 43f, u * 3f, u * 40f, u * 4f), $"Enemigos: {activeRoom.Remaining}", st, new Color(1f, 0.75f, 0.55f));
        }

        // barra del jefe
        if (boss != null && boss.Health > 0f)
        {
            float w = sw * 0.55f, h = u * 2.4f;
            var bar = new Rect((sw - w) / 2f, sh - u * 9f, w, h);
            var st = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 2.8f), fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerCenter };
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
            Box(new Rect(0, sh * 0.2f - u * 1.5f, sw, u * 14f), new Color(0f, 0f, 0f, 0.5f * a));
            Box(new Rect(sw * 0.2f, sh * 0.2f - u * 1.5f, sw * 0.6f, u * 0.25f), new Color(bannerColor.r, bannerColor.g, bannerColor.b, 0.8f * a));
            Box(new Rect(sw * 0.2f, sh * 0.2f + u * 12.25f, sw * 0.6f, u * 0.25f), new Color(bannerColor.r, bannerColor.g, bannerColor.b, 0.8f * a));
            var big = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 6.5f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            var small = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 2.6f), fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter };
            Text(new Rect(slide, sh * 0.2f, sw, u * 8f), bannerTop, big, new Color(bannerColor.r, bannerColor.g, bannerColor.b, a));
            if (!string.IsNullOrEmpty(bannerBottom)) Text(new Rect(-slide, sh * 0.2f + u * 7.5f, sw, u * 4f), bannerBottom, small, new Color(1f, 1f, 1f, a * 0.85f));
        }

        // GDD: controles siempre visibles en la parte inferior (y la tecla de la fuente cuando Abby está junto a una)
        if (!dead && !completed)
        {
            BarraControles(sw, sh, u);
            var st = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 1.9f), alignment = TextAnchor.MiddleCenter };
            if (HealingShrine.Cerca != null)
            {
                var big = new GUIStyle(st) { fontSize = FuentePixel.Tam(u * 2.8f), fontStyle = FontStyle.Bold };
                float blink = 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 5f);
                Text(new Rect(0, sh - u * 26f, sw, u * 5f), "E  ·  Usar la fuente de curación", big, new Color(0.6f, 1f, 0.75f, blink));
            }
        }

        // negro (inicio, muerte, final)
        if (fade > 0f) Box(new Rect(0, 0, sw, sh), new Color(0f, 0f, 0f, fade));

        if (dead)
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 8f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            Text(new Rect(0, sh * 0.36f, sw, u * 12f), "DERROTA", st, new Color(1f, 0.35f, 0.35f, Mathf.Clamp01(endTimer / 0.8f)));
            var s2 = new GUIStyle(st) { fontSize = FuentePixel.Tam(u * 2.6f), fontStyle = FontStyle.Italic };
            Text(new Rect(0, sh * 0.36f + u * 11f, sw, u * 4f), "Abby cayó.", s2, new Color(1f, 1f, 1f, Mathf.Clamp01((endTimer - 0.6f) / 0.8f)));
            if (endTimer > 1.6f)
            {
                var s3 = new GUIStyle(s2) { fontSize = FuentePixel.Tam(u * 2.6f), fontStyle = FontStyle.Bold };
                float blink = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 4f);
                Text(new Rect(0, sh * 0.36f + u * 18f, sw, u * 4f), "Enter  ·  Reintentar el piso          Esc  ·  Menú principal", s3, new Color(1f, 1f, 1f, blink));
            }
        }

        if (completed)
        {
            float a = Mathf.Clamp01(endTimer / 1f);
            var st = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 8f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            string done = floorTitle.StartsWith("PISO") ? $"¡{floorTitle} COMPLETADO!" : floorTitle.StartsWith("LA ") ? $"¡{floorTitle}, CONQUISTADA!" : $"¡{floorTitle} SUPERADO!";
            Text(new Rect(0, sh * 0.28f, sw, u * 12f), done, st, new Color(1f, 0.88f, 0.5f, a));
            var s2 = new GUIStyle(st) { fontSize = FuentePixel.Tam(u * 3f), fontStyle = FontStyle.Normal };
            Text(new Rect(0, sh * 0.28f + u * 13f, sw, u * 5f), $"Tiempo  {completeTime}        Enemigos derrotados  {kills}", s2, new Color(1f, 1f, 1f, a));
            if (endTimer > 1.5f)
            {
                var s3 = new GUIStyle(s2) { fontSize = FuentePixel.Tam(u * 2.4f), fontStyle = FontStyle.Italic };
                float blink = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 4f);
                Text(new Rect(0, sh * 0.28f + u * 21f, sw, u * 4f), "Enter / Espacio para continuar", s3, new Color(1f, 1f, 1f, blink));
            }
        }
        GUI.color = prev;

        void Box(Rect r, Color c) { GUI.color = c; GUI.DrawTexture(r, tex); }
    }

    /// <summary>Barra de controles del GDD (teclado y mouse).</summary>
    public const string Controles = "WASD  Mover     Clic izq.  Atacar (combo de 3)     Clic der.  Bloquear     Espacio  Saltar     Shift  Impulso     E  Interactuar     F3  Cámara libre     Esc  Pausa";

    /// <summary>Retrato propio del HUD (el tutorial se juega con Dante); se queda aunque Start corra antes o después.</summary>
    public void PonerRetrato(Sprite s) { retratoFijo = s; if (s != null) retrato = s; }

    /// <summary>Barra de controles: cada tecla en un chip y la acción al lado; dos filas centradas.</summary>
    static readonly (string tecla, string accion)[][] FilasControles =
    {
        new[] { ("WASD", "Mover"), ("Espacio", "Saltar"), ("Shift", "Impulso"), ("Clic izq.", "Atacar (combo de 3)"), ("Clic der.", "Bloquear") },
        new[] { ("E", "Interactuar"), ("F3", "Cámara libre"), ("Esc", "Pausa") },
    };

    void BarraControles(float sw, float sh, float u)
    {
        // con el cuadro de diálogo a la vista la barra se atenúa (casi desaparece) para no encimarse con él
        float k = 1f - CuadroNarracion.DialogoVisible * 0.92f;
        if (k < 0.04f) return;
        void Box(Rect r, Color c) { c.a *= k; GUI.color = c; GUI.DrawTexture(r, Texture2D.whiteTexture); }   // el de OnGUI es local a OnGUI
        float alto = u * 7.4f;
        Box(new Rect(0, sh - alto, sw, alto), new Color(0f, 0f, 0f, 0.6f));
        Box(new Rect(0, sh - alto, sw, u * 0.2f), new Color(0.75f, 0.6f, 1f, 0.5f));
        var tecla = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 1.9f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        var accion = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 2f), alignment = TextAnchor.MiddleLeft };
        float hFila = u * 3.2f, hueco = u * 3f;
        for (int f = 0; f < FilasControles.Length; f++)
        {
            var fila = FilasControles[f];
            var anchoT = new float[fila.Length]; var anchoA = new float[fila.Length];
            float total = 0f;
            for (int i = 0; i < fila.Length; i++)
            {
                anchoT[i] = tecla.CalcSize(new GUIContent(fila[i].tecla)).x + u * 2f;
                anchoA[i] = accion.CalcSize(new GUIContent(fila[i].accion)).x + u * 0.6f;
                total += anchoT[i] + u * 0.8f + anchoA[i] + (i < fila.Length - 1 ? hueco : 0f);
            }
            float x = (sw - total) / 2f, y = sh - alto + u * 0.6f + f * hFila;
            for (int i = 0; i < fila.Length; i++)
            {
                var chip = new Rect(x, y + u * 0.25f, anchoT[i], hFila - u * 0.5f);
                Box(chip, new Color(0.1f, 0.08f, 0.18f, 0.95f));
                Box(new Rect(chip.x, chip.y, chip.width, 2f), new Color(0.75f, 0.6f, 1f, 0.9f));
                Text(chip, fila[i].tecla, tecla, new Color(1f, 0.85f, 0.5f, k));
                x += anchoT[i] + u * 0.8f;
                Text(new Rect(x, y, anchoA[i], hFila), fila[i].accion, accion, new Color(1f, 1f, 1f, 0.92f * k));
                x += anchoA[i] + hueco;
            }
        }
    }

    static void Text(Rect r, string text, GUIStyle st, Color c)
    {
        // GUI.color tiñe las etiquetas: si quedó negro de un rectángulo anterior, el texto salía oscuro
        GUI.color = Color.white;
        float o = Mathf.Max(1.5f, st.fontSize * 0.06f);
        st.normal.textColor = new Color(0f, 0f, 0f, c.a * 0.85f);
        foreach (var d in new[] { new Vector2(o, o), new Vector2(-o, o), new Vector2(o, -o), new Vector2(-o, -o), new Vector2(o * 1.6f, o * 1.6f) })
            GUI.Label(new Rect(r.x + d.x, r.y + d.y, r.width, r.height), text, st);
        st.normal.textColor = c;
        GUI.Label(r, text, st);
    }
}
