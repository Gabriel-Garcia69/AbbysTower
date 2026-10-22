using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guía hacia la siguiente pelea: una baliza de luz morada en la próxima horda (o en Dante, o en la salida cuando ya
/// no queda nada) y, en el HUD, una flecha en el borde de la pantalla con lo que es y a cuántos metros está.
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
    static Texture2D flecha;

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

    void OnGUI()
    {
        if (!hay || jugador == null || DanteFinal.Activo || PauseMenu.IsPaused) return;
        if (baliza == null || !baliza.activeSelf) return;
        var cam = Camera.main;
        if (cam == null) return;
        float sw = Screen.width, sh = Screen.height, u = sh / 100f;
        if (flecha == null) flecha = CrearFlecha();
        Vector3 sp = cam.WorldToScreenPoint(objetivo + Vector3.up * 2f);
        bool detras = sp.z < 0f;
        Vector2 p = new Vector2(sp.x, sh - sp.y);
        if (detras) p = new Vector2(sw - p.x, sh - p.y);
        float m = u * 7f;
        bool dentro = !detras && p.x > m && p.x < sw - m && p.y > m && p.y < sh - m;
        Vector2 centro = new Vector2(sw / 2f, sh / 2f);
        Vector2 dir = (p - centro).normalized;
        if (!dentro)
        {
            // al borde de la pantalla, en la dirección del objetivo
            float k = Mathf.Min((sw / 2f - m) / Mathf.Max(0.001f, Mathf.Abs(dir.x)), (sh / 2f - m) / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
            p = centro + dir * k;
        }
        float t = 0.75f + 0.25f * Mathf.Sin(Time.time * 4f);
        Color prev = GUI.color;
        GUI.color = new Color(0.85f, 0.6f, 1f, t);
        float s = u * 4.5f;
        var mat = GUI.matrix;
        float ang = dentro ? 90f : Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        GUIUtility.RotateAroundPivot(ang, p);
        GUI.DrawTexture(new Rect(p.x - s / 2f, p.y - s / 2f, s, s), flecha);
        GUI.matrix = mat;
        GUI.color = prev;
        var st = new GUIStyle(GUI.skin.label) { fontSize = FuentePixel.Tam(u * 2.4f), alignment = TextAnchor.MiddleCenter };
        string txt = nombre + "  " + Mathf.RoundToInt(Plano(objetivo - jugador.position)) + " m";
        Vector2 lp = p - dir * (dentro ? 0f : s * 1.6f) + (dentro ? new Vector2(0f, -s) : Vector2.zero);
        var rect = new Rect(lp.x - u * 25f, lp.y - u * 2f, u * 50f, u * 4f);
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), txt, st);
        GUI.color = new Color(0.9f, 0.75f, 1f, 1f);
        GUI.Label(rect, txt, st);
        GUI.color = prev;
    }

    /// <summary>Triángulo que apunta a la derecha (se gira hacia el objetivo).</summary>
    static Texture2D CrearFlecha()
    {
        const int n = 32;
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float fy = Mathf.Abs(y - n / 2f + 0.5f) / (n / 2f);
                bool dentro = x > 4 && x < n - 4 && fy < (1f - (x - 4f) / (n - 8f)) * 0.9f;
                t.SetPixel(x, y, dentro ? Color.white : Color.clear);
            }
        t.Apply();
        return t;
    }
}
