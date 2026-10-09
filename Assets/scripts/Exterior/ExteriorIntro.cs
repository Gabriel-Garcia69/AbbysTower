using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Cinemática de entrada del exterior: varias tomas por el mapa (el abismo, las islas, la cascada, la fuente,
/// la torre) y al final la cámara baja hasta quedar exactamente en la vista de juego.
/// Mientras dura: la cámara no se puede mover con el mouse y Abby no se mueve. Enter / Espacio / Esc / clic la saltan.
///
/// Las tomas se calculan con los datos del ExteriorBuilder de la escena, así siguen funcionando si se regenera el mapa.
/// Va en el CameraArm (o en cualquier padre de la cámara).
/// </summary>
[DefaultExecutionOrder(1000)]   // después de CameraRig y MouseOrbitCamera
public class ExteriorIntro : MonoBehaviour
{
    [Tooltip("Multiplica la duración de todas las tomas.")]
    public float speed = 1f;
    public bool skippable = true;
    public string title = "ABBY'S TOWER";
    public string subtitle = "Piso 1 · La Base de la Torre";
    public float endFov = 35f;

    [Tooltip("Se apagan mientras dura la cinemática. Vacío = busca MouseOrbitCamera, PlayerInputReader y CombatInputReader.")]
    public Behaviour[] disableDuring;

    class Shot
    {
        public Vector3[] pos, look;
        public float duration, fovA, fovB;
        public string caption;
        public bool finalShot;
    }

    Camera cam;
    Vector3 camLocalPos; Quaternion camLocalRot;
    readonly List<Shot> shots = new List<Shot>();
    readonly List<Behaviour> disabled = new List<Behaviour>();
    int index;
    float t, totalT, fade = 1f, bars = 1f, captionAlpha, titleAlpha;
    bool skipping, done;

    const float FadeTime = 0.45f;

    void Awake()
    {
        cam = GetComponentInChildren<Camera>();
        if (cam == null) { enabled = false; return; }
        camLocalPos = cam.transform.localPosition;
        camLocalRot = cam.transform.localRotation;

        var list = new List<Behaviour>();
        if (disableDuring != null) foreach (var b in disableDuring) if (b != null) list.Add(b);
        if (list.Count == 0)
        {
            foreach (var b in FindObjectsByType<MouseOrbitCamera>(FindObjectsSortMode.None)) list.Add(b);
            foreach (var b in FindObjectsByType<PlayerInputReader>(FindObjectsSortMode.None)) list.Add(b);
            foreach (var b in FindObjectsByType<CombatInputReader>(FindObjectsSortMode.None)) list.Add(b);
        }
        foreach (var b in list) if (b.enabled) { b.enabled = false; disabled.Add(b); }
    }

    void Start()
    {
        BuildShots();
        if (shots.Count == 0) { Finish(); return; }
        Apply(0f);
    }

    // ------------------------------------------------------------------ tomas

    void BuildShots()
    {
        var b = FindFirstObjectByType<ExteriorBuilder>();
        if (b == null) return;
        Transform w = b.transform;
        Vector3 W(float x, float y, float z) => w.TransformPoint(new Vector3(x, y, z));
        Vector2 pc = b.plateauCenter, tc = b.towerCenter, lc = b.lakeCenter;
        float mh = b.mesaHeight;
        float edgeS = pc.y - b.plateauRadius;   // borde sur de la meseta (aprox.)

        // 1) desde el abismo: subiendo entre el mar de nubes, mirando los acantilados hasta la torre
        shots.Add(new Shot
        {
            pos = new[] { W(-70f, -85f, edgeS - 105f), W(-40f, -40f, edgeS - 75f), W(-14f, 6f, edgeS - 52f) },
            look = new[] { W(0f, -40f, edgeS + 10f), W(0f, -5f, edgeS + 20f), W(tc.x, mh + 40f, tc.y) },
            duration = 6.5f, fovA = 52f, fovB = 44f, caption = "El Abismo",
        });

        // 2) pasada junto a la isla flotante más cercana
        Transform island = null; float best = float.MaxValue;
        var abyss = w.Find("Ext_Abismo");
        if (abyss != null)
            foreach (Transform c in abyss)
            {
                if (!c.name.StartsWith("Isla_")) continue;
                float d = Vector3.Distance(c.position, W(pc.x, 0f, pc.y));
                if (d < best) { best = d; island = c; }
            }
        if (island != null)
        {
            float size = 10f;
            var rs = island.GetComponentsInChildren<Renderer>();
            if (rs.Length > 0)
            {
                var bb = rs[0].bounds; foreach (var r in rs) bb.Encapsulate(r.bounds);
                size = Mathf.Max(bb.extents.x, bb.extents.z);
            }
            Vector3 p = island.position;
            Vector3 toward = W(pc.x, p.y, pc.y) - p; toward.y = 0f; toward.Normalize();   // de la isla hacia la meseta
            Vector3 side = Vector3.Cross(Vector3.up, toward);
            shots.Add(new Shot
            {
                pos = new[] { p + toward * size * 2.6f + side * size * 2.4f + Vector3.down * size * 0.6f,
                              p + toward * size * 2.2f + Vector3.up * size * 0.4f,
                              p + toward * size * 2.4f - side * size * 2.2f + Vector3.up * size * 1.1f },
                look = new[] { p + Vector3.down * size * 0.4f, p, p + Vector3.up * size * 0.2f - side * size * 0.3f },
                duration = 5.5f, fovA = 40f, fovB = 46f, caption = "Islas a la deriva",
            });
        }

        // 3) la cascada: desde abajo, subiendo por la caída de agua hasta asomarse al lago
        var fall = w.Find("Ext_Lago/Cascada");
        if (fall != null && fall.TryGetComponent<Renderer>(out var fr))
        {
            Bounds fb = fr.bounds;
            Vector3 lake = W(lc.x, b.waterLevel, lc.y);
            Vector3 lip = new Vector3(fb.center.x, fb.max.y, fb.center.z);
            Vector3 outDir = fb.center - lake; outDir.y = 0f; outDir.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, outDir);
            shots.Add(new Shot
            {
                pos = new[] { lip + outDir * 75f + side * 12f + Vector3.down * 60f,
                              lip + outDir * 48f + side * 5f + Vector3.down * 18f,
                              lip + outDir * 14f - side * 4f + Vector3.up * 7f },
                look = new[] { lip + Vector3.down * 60f, lip + Vector3.down * 18f, lake + Vector3.up * 2f },
                duration = 6f, fovA = 48f, fovB = 42f, caption = "La Cascada Eterna",
            });
        }

        // 4) la fuente: giro bajo alrededor de la pileta
        {
            Vector3 f = W(0f, 0f, 0f);
            var ps = new Vector3[4]; var ls = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                float k = i / 3f;
                float a = Mathf.Lerp(210f, 320f, k) * Mathf.Deg2Rad;
                float r = Mathf.Lerp(10f, 7f, k);
                ps[i] = f + new Vector3(Mathf.Cos(a) * r, Mathf.Lerp(1.1f, 2.6f, k), Mathf.Sin(a) * r);
                ls[i] = f + Vector3.up * Mathf.Lerp(1.3f, 1.9f, k);
            }
            shots.Add(new Shot { pos = ps, look = ls, duration = 5f, fovA = 38f, fovB = 34f, caption = "Plaza de la Fuente" });
        }

        // 5) la torre: del pie de la escalinata hacia arriba, hasta el faro
        {
            float r0 = 14f;
            float z0 = tc.y - b.mesaRadius - mh * 1.9f, z1 = tc.y - b.mesaRadius;
            float doorZ = tc.y - r0;
            shots.Add(new Shot
            {
                pos = new[] { W(tc.x + 7f, 1.4f, z0 - 7f), W(tc.x + 4f, mh * 0.55f, (z0 + z1) / 2f), W(tc.x + 2f, mh + 2.5f, doorZ - 12f) },
                look = new[] { W(tc.x, mh + 5f, doorZ), W(tc.x, mh + 40f, tc.y), W(tc.x, mh + 150f, tc.y) },
                duration = 7f, fovA = 44f, fovB = 58f, caption = null,
            });
        }

        // 6) llegada: desde lo alto sobre el borde sur, bajando hasta la vista de juego (se completa en Apply)
        shots.Add(new Shot
        {
            pos = new[] { W(pc.x + 12f, 34f, edgeS - 30f), W(pc.x + 4f, 12f, edgeS - 6f) },
            look = new[] { W(tc.x, mh + 30f, tc.y), W(0f, 2f, -8f) },
            duration = 5.5f, fovA = 50f, fovB = endFov, finalShot = true,
        });
    }

    // ------------------------------------------------------------------ reproducción

    void LateUpdate()
    {
        if (done) return;
        Cursor.visible = false;

        if (skippable && !skipping && SkipPressed()) skipping = true;
        if (skipping)
        {
            fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / 0.3f);
            if (fade >= 1f) { index = shots.Count - 1; t = shots[index].duration * speed; Apply(1f); Finish(); }
            else Apply(Progress());
            return;
        }

        float dur = shots[index].duration * speed;
        t += Time.deltaTime; totalT += Time.deltaTime;
        if (t >= dur && index < shots.Count - 1) { index++; t = 0f; dur = shots[index].duration * speed; }

        var shot = shots[index];
        // fundido a negro entre tomas (la última no se funde al terminar: queda la vista de juego)
        float fin = Mathf.Clamp01(t / FadeTime);
        float fout = shot.finalShot ? 1f : Mathf.Clamp01((dur - t) / FadeTime);
        fade = 1f - Mathf.Min(fin, fout);

        // franjas de cine: se retiran en el último segundo y medio
        bars = shot.finalShot ? Mathf.Clamp01((dur - t) / 1.5f) : 1f;

        captionAlpha = shot.caption == null ? 0f : Mathf.Clamp01(Mathf.Min((t - 0.6f) / 0.6f, (dur - 0.7f - t) / 0.6f));
        bool titleShot = index == shots.Count - 2;
        titleAlpha = titleShot ? Mathf.Clamp01(Mathf.Min((t - 1.8f) / 1f, (dur - 0.5f - t) / 0.8f)) : 0f;

        Apply(Progress());
        if (shot.finalShot && t >= dur) Finish();
    }

    float Progress() => Mathf.Clamp01(t / Mathf.Max(0.01f, shots[index].duration * speed));

    void Apply(float k)
    {
        var shot = shots[index];
        float e = k * k * (3f - 2f * k);   // suave al empezar y al terminar
        Vector3[] pos = shot.pos, look = shot.look;
        if (shot.finalShot)
        {
            // el último punto es la pose real de juego (se calcula cada cuadro porque el rig sigue a Abby)
            Transform parent = cam.transform.parent;
            Vector3 gPos = parent.TransformPoint(camLocalPos);
            Quaternion gRot = parent.rotation * camLocalRot;
            pos = new[] { shot.pos[0], shot.pos[1], gPos };
            look = new[] { shot.look[0], shot.look[1], gPos + gRot * Vector3.forward * 12f };
        }
        Vector3 p = Spline(pos, e), l = Spline(look, e);
        cam.transform.position = p;
        if ((l - p).sqrMagnitude > 0.0001f) cam.transform.rotation = Quaternion.LookRotation(l - p, Vector3.up);
        cam.fieldOfView = Mathf.Lerp(shot.fovA, shot.fovB, e);
    }

    /// <summary>Catmull-Rom que pasa por todos los puntos (extremos repetidos).</summary>
    static Vector3 Spline(Vector3[] pts, float k)
    {
        if (pts.Length == 1) return pts[0];
        float f = k * (pts.Length - 1);
        int i = Mathf.Min(Mathf.FloorToInt(f), pts.Length - 2);
        float u = f - i;
        Vector3 p0 = pts[Mathf.Max(i - 1, 0)], p1 = pts[i], p2 = pts[i + 1], p3 = pts[Mathf.Min(i + 2, pts.Length - 1)];
        return 0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u * u + (-p0 + 3f * p1 - 3f * p2 + p3) * u * u * u);
    }

    static bool SkipPressed()
    {
        var kb = Keyboard.current; var ms = Mouse.current; var gp = Gamepad.current;
        return (kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame))
            || (ms != null && ms.leftButton.wasPressedThisFrame)
            || (gp != null && (gp.startButton.wasPressedThisFrame || gp.buttonSouth.wasPressedThisFrame));
    }

    void Finish()
    {
        done = true;
        cam.transform.localPosition = camLocalPos;
        cam.transform.localRotation = camLocalRot;
        cam.fieldOfView = endFov;
        foreach (var b in disabled) if (b != null) b.enabled = true;
        fadeOutAfter = skipping ? 1f : 0f;
        bars = 0f; captionAlpha = titleAlpha = 0f;
    }

    float fadeOutAfter;

    void Update()
    {
        // tras saltar la cinemática, aclara desde negro ya con la vista de juego
        if (!done) return;
        if (fadeOutAfter > 0f) fadeOutAfter = Mathf.MoveTowards(fadeOutAfter, 0f, Time.unscaledDeltaTime / 0.5f);
        else enabled = false;
    }

    // ------------------------------------------------------------------ pantalla

    void OnGUI()
    {
        float sw = Screen.width, sh = Screen.height;
        var tex = Texture2D.whiteTexture;
        Color prev = GUI.color;

        if (!done && bars > 0f)
        {
            float bh = sh * 0.11f * bars;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, sw, bh), tex);
            GUI.DrawTexture(new Rect(0, sh - bh, sw, bh), tex);
        }

        if (!done && index < shots.Count && captionAlpha > 0f && shots[index].caption != null)
        {
            var st = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(sh / 26f), fontStyle = FontStyle.Italic, alignment = TextAnchor.LowerLeft };
            Shadowed(new Rect(sw * 0.06f, sh * 0.70f, sw * 0.6f, sh * 0.12f), shots[index].caption, st, captionAlpha);
        }

        if (!done && titleAlpha > 0f)
        {
            var big = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(sh / 8f), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            var small = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(sh / 28f), alignment = TextAnchor.MiddleCenter };
            Shadowed(new Rect(0, sh * 0.30f, sw, sh * 0.2f), title, big, titleAlpha);
            Shadowed(new Rect(0, sh * 0.48f, sw, sh * 0.08f), subtitle, small, titleAlpha);
        }

        if (!done && skippable)
        {
            var hint = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(sh / 48f), alignment = TextAnchor.LowerRight };
            Shadowed(new Rect(0, sh - sh * 0.1f, sw - sw * 0.03f, sh * 0.08f), "Enter / Espacio: saltar", hint, 0.6f * Mathf.Clamp01(totalT - 1f));
        }

        float black = done ? fadeOutAfter : fade;
        if (black > 0f)
        {
            GUI.color = new Color(0f, 0f, 0f, black);
            GUI.DrawTexture(new Rect(0, 0, sw, sh), tex);
        }
        GUI.color = prev;
    }

    static void Shadowed(Rect r, string text, GUIStyle st, float alpha)
    {
        if (alpha <= 0f) return;
        st.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.7f);
        GUI.Label(new Rect(r.x + 3, r.y + 3, r.width, r.height), text, st);
        st.normal.textColor = new Color(1f, 0.95f, 0.88f, alpha);
        GUI.Label(r, text, st);
    }
}
