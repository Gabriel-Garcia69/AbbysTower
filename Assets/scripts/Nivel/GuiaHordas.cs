using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guía hacia la siguiente pelea: una baliza de luz morada en la próxima horda (o en Dante, o en la salida cuando ya
/// no queda nada) y, en el HUD, una insignia con chevrón en el borde de la pantalla con lo que es y a cuántos metros está.
/// Antes había que dar vueltas al mapa para encontrar dónde aparecían los enemigos. FloorDirector la agrega.
/// </summary>
public class GuiaHordas : MonoBehaviour
{
    readonly List<RoomEncounter> hordas = new List<RoomEncounter>();
    RoomEncounter jefe;
    FloorExit salida;
    Transform jugador;
    GameObject baliza;
    Vector3 objetivo;
    string nombre = "";
    bool hay;

    void Start()
    {
        var p = GameObject.FindWithTag("Player");
        if (p != null) jugador = p.transform;
        foreach (var r in FindObjectsByType<RoomEncounter>(FindObjectsSortMode.None))
        {
            if (r.isBoss) jefe = r; else hordas.Add(r);
        }
        // en el orden del recorrido: "Horda 1 de 4", "Horda 2 de 4"... (si no tienen número, por cercanía al inicio)
        Vector3 inicio = jugador != null ? jugador.position : Vector3.zero;
        hordas.Sort((a, b) =>
        {
            int na = Numero(a.displayName), nb = Numero(b.displayName);
            if (na != nb) return na.CompareTo(nb);
            return (a.transform.position - inicio).sqrMagnitude.CompareTo((b.transform.position - inicio).sqrMagnitude);
        });
        salida = FindAnyObjectByType<FloorExit>();
        baliza = CombatFx.Baliza();
        baliza.SetActive(false);
    }

    static int Numero(string s)
    {
        if (string.IsNullOrEmpty(s)) return 999;
        var partes = s.Split(' ');
        return partes.Length > 1 && int.TryParse(partes[1], out int n) ? n : 999;
    }

    /// <summary>Cuántas hordas faltan y cuántas hay (para el letrero al despejar una).</summary>
    public int Restantes { get { int n = 0; foreach (var h in hordas) if (h != null && !h.IsCleared) n++; return n; } }
    public int Total => hordas.Count;
    /// <summary>¿Queda el jefe del piso (Dante) por pelear?</summary>
    public bool JefePendiente => jefe != null && !jefe.IsCleared;

    void Update()
    {
        hay = false;
        bool peleando = false;
        foreach (var h in hordas) if (h != null && h.Current == RoomEncounter.State.Combate) peleando = true;
        if (jefe != null && jefe.Current == RoomEncounter.State.Combate) peleando = true;

        RoomEncounter sig = null;
        foreach (var h in hordas) if (h != null && !h.IsCleared) { sig = h; break; }
        if (sig != null) { objetivo = sig.transform.position; nombre = (sig.displayName ?? "Horda").ToUpper(); hay = true; }
        else if (jefe != null && !jefe.IsCleared) { objetivo = jefe.transform.position; nombre = string.IsNullOrEmpty(jefe.bossName) ? "JEFE" : jefe.bossName.ToUpper(); hay = true; }
        else if (salida != null) { objetivo = salida.transform.position; nombre = "SALIDA"; hay = true; }

        float dist = jugador != null && hay ? Plano(objetivo - jugador.position) : 0f;
        bool ver = hay && !peleando && dist > 5f && !PauseMenu.IsPaused;
        if (baliza != null)
        {
            if (baliza.activeSelf != ver) baliza.SetActive(ver);
            if (ver) baliza.transform.position = objetivo;
        }
    }

    static float Plano(Vector3 v) { v.y = 0f; return v.magnitude; }

    static Texture2D rombo, chevron, insignia;
    float alfaGui;

    void OnGUI()
    {
        bool mostrar = hay && jugador != null && !DanteFinal.Activo && !PauseMenu.IsPaused && baliza != null && baliza.activeSelf;
        alfaGui = Mathf.MoveTowards(alfaGui, mostrar ? 1f : 0f, Time.unscaledDeltaTime * 3f);
        if (alfaGui <= 0.01f || jugador == null) return;
        var cam = Camera.main;
        if (cam == null) return;
        if (rombo == null) { rombo = CrearRombo(); chevron = CrearChevron(); insignia = CrearInsignia(); }
        float sw = Screen.width, sh = Screen.height, u = sh / 100f;
        float dist = Plano(objetivo - jugador.position);
        // de cerca se desvanece (la baliza ya se ve)
        float a = alfaGui * Mathf.Clamp01((dist - 6f) / 6f);
        if (a <= 0.01f) return;

        Vector3 sp = cam.WorldToScreenPoint(objetivo + Vector3.up * 3.2f);
        bool detras = sp.z < 0f;
        Vector2 p = new Vector2(sp.x, sh - sp.y);
        if (detras) p = new Vector2(sw - p.x, sh - p.y);
        float m = u * 8f;
        bool dentro = !detras && p.x > m && p.x < sw - m && p.y > m && p.y < sh - m * 1.6f;
        Vector2 centro = new Vector2(sw / 2f, sh / 2f);
        Vector2 dir = (p - centro).normalized;
        if (!dentro)
        {
            float k = Mathf.Min((sw / 2f - m) / Mathf.Max(0.001f, Mathf.Abs(dir.x)), (sh / 2f - m * 1.6f) / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
            p = centro + dir * k;
        }

        Color prev = GUI.color;
        Color lav = new Color(0.78f, 0.7f, 1f);
        var titulo = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 2.1f), alignment = TextAnchor.MiddleCenter };
        var sub = new GUIStyle(titulo) { fontSize = FuentePixel.Tam(u * 1.7f) };
        string nom = nombre.Replace(" DE ", "/"), metros = Mathf.RoundToInt(dist) + " m";

        if (dentro)
        {
            // rombo pixel flotando sobre la baliza, con el nombre y la distancia debajo
            float s = u * 3.2f, flota = Mathf.Sin(Time.unscaledTime * 2.4f) * u * 0.5f;
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.DrawTexture(new Rect(p.x - s / 2f, p.y - s / 2f + flota, s, s), rombo);
            Texto(new Rect(p.x - u * 20f, p.y + s * 0.6f + flota, u * 40f, u * 3f), nom, titulo, lav, a);
            Texto(new Rect(p.x - u * 20f, p.y + s * 0.6f + u * 2.6f + flota, u * 40f, u * 3f), metros, sub, Color.white, a * 0.85f);
        }
        else
        {
            // insignia redonda en el borde con un chevrón que apunta hacia la siguiente pelea
            float s = u * 6f;
            GUI.color = new Color(1f, 1f, 1f, a * 0.9f);
            GUI.DrawTexture(new Rect(p.x - s / 2f, p.y - s / 2f, s, s), insignia);
            var mat = GUI.matrix;
            float pulso = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 4f);
            float c = u * 3f * pulso;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, p);
            GUI.color = new Color(lav.r, lav.g, lav.b, a);
            GUI.DrawTexture(new Rect(p.x - c / 2f + u * 0.4f, p.y - c / 2f, c, c), chevron);
            GUI.matrix = mat;
            Vector2 lp = p - dir * s * 1.25f;
            Texto(new Rect(lp.x - u * 20f, lp.y - u * 2.6f, u * 40f, u * 3f), nom, titulo, lav, a);
            Texto(new Rect(lp.x - u * 20f, lp.y, u * 40f, u * 3f), metros, sub, Color.white, a * 0.85f);
        }
        GUI.color = prev;
    }

    static void Texto(Rect r, string s, GUIStyle st, Color c, float a)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.75f * a);
        GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), s, st);
        GUI.color = new Color(c.r, c.g, c.b, a);
        GUI.Label(r, s, st);
    }

    // ------------------------------------------------------------------ texturas pixel (sin archivos)

    static Texture2D Nueva(int n) => new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };

    /// <summary>Rombo de 16 px: borde oscuro, relleno lavanda con brillo arriba.</summary>
    static Texture2D CrearRombo()
    {
        const int n = 16; var t = Nueva(n);
        var borde = new Color(0.12f, 0.08f, 0.22f, 1f); var relleno = new Color(0.72f, 0.62f, 1f, 1f); var brillo = new Color(0.95f, 0.92f, 1f, 1f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Abs(x - 7.5f) + Mathf.Abs(y - 7.5f);
                Color c = Color.clear;
                if (d <= 7.5f) c = borde;
                if (d <= 6f) c = relleno;
                if (d <= 6f && y > 8 && x < 8) c = brillo;
                t.SetPixel(x, y, c);
            }
        t.Apply(); return t;
    }

    /// <summary>Chevrón de 16 px que apunta a la derecha.</summary>
    static Texture2D CrearChevron()
    {
        const int n = 16; var t = Nueva(n);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dy = Mathf.Abs(y - 7.5f);
                float c0 = 3f + (7f - dy) * 1.1f;   // la punta a la derecha, los brazos hacia atrás
                bool on = dy <= 6.5f && x >= c0 && x <= c0 + 2.6f;
                t.SetPixel(x, y, on ? Color.white : Color.clear);
            }
        t.Apply(); return t;
    }

    /// <summary>Insignia redonda: disco oscuro translúcido con aro lavanda.</summary>
    static Texture2D CrearInsignia()
    {
        const int n = 32; var t = Nueva(n);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = new Vector2(x - 15.5f, y - 15.5f).magnitude;
                Color c = Color.clear;
                if (d <= 15f) c = new Color(0.07f, 0.05f, 0.14f, 0.78f);
                if (d > 13f && d <= 15f) c = new Color(0.72f, 0.62f, 1f, 0.95f);
                t.SetPixel(x, y, c);
            }
        t.Apply(); return t;
    }
}
