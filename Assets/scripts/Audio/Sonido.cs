using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Efectos de sonido del GDD (05 · Audio): golpe de espada (corte), bloqueo (metal apagado), daño recibido (golpe
/// sordo), fuente de curación (campana suave), alerta de sombra (pulso grave), pasos en piedra y muerte (caída).
/// Estéreo simple, sin audio espacial. Si existe Resources/Audio/&lt;Nombre&gt; (wav/ogg de Francisco) se usa ese; si no,
/// suena uno provisional sintetizado aquí. Volumen = general × efectos (Ajustes).
/// </summary>
public static class Sonido
{
    public enum Efecto { Espada, Bloqueo, Dano, Fuente, Alerta, Paso, Muerte, Clic, Destello }

    const int Hz = 22050;
    static readonly Dictionary<Efecto, AudioClip> clips = new Dictionary<Efecto, AudioClip>();
    static readonly Dictionary<Efecto, float> ultimo = new Dictionary<Efecto, float>();
    static AudioSource[] voces;
    static int siguiente;
    static System.Random rng = new System.Random(7);

    public static void Tocar(Efecto e, float volumen = 1f, float tono = 1f)
    {
        // el mismo efecto muchas veces en el mismo instante (hordas) suena a ruido: se espacia
        float ahora = Time.unscaledTime;
        float minimo = e == Efecto.Alerta ? 0.12f : e == Efecto.Paso ? 0.05f : 0.03f;
        if (ultimo.TryGetValue(e, out float u) && ahora - u < minimo) return;
        ultimo[e] = ahora;
        Preparar();
        var v = voces[siguiente];
        siguiente = (siguiente + 1) % voces.Length;
        v.pitch = tono;
        v.PlayOneShot(Clip(e), Mathf.Clamp01(volumen) * Ajustes.General * Ajustes.Efectos);
    }

    static void Preparar()
    {
        if (voces != null && voces[0] != null) return;
        var go = new GameObject("Sonido");
        Object.DontDestroyOnLoad(go);
        voces = new AudioSource[10];
        for (int i = 0; i < voces.Length; i++)
        {
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false; s.spatialBlend = 0f; s.priority = 64;
            voces[i] = s;
        }
        if (Object.FindAnyObjectByType<AudioListener>() == null) go.AddComponent<AudioListener>();
    }

    static AudioClip Clip(Efecto e)
    {
        if (clips.TryGetValue(e, out var c) && c != null) return c;
        c = Resources.Load<AudioClip>("Audio/" + e);
        if (c == null) c = Sintetizar(e);
        clips[e] = c;
        return c;
    }

    // ------------------------------------------------------------------ provisionales sintetizados

    static float Ruido() => (float)(rng.NextDouble() * 2.0 - 1.0);

    static AudioClip Sintetizar(Efecto e)
    {
        float dur = e == Efecto.Fuente ? 1.8f : e == Efecto.Muerte ? 1.1f : e == Efecto.Alerta ? 0.45f : e == Efecto.Bloqueo ? 0.35f
                  : e == Efecto.Paso ? 0.08f : e == Efecto.Clic ? 0.05f : e == Efecto.Destello ? 1.6f : 0.22f;
        int n = Mathf.CeilToInt(dur * Hz);
        var d = new float[n];
        float lp = 0f, lp2 = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Hz, k = t / dur, s = 0f;
            switch (e)
            {
                case Efecto.Espada:   // corte: ruido que barre de agudo a grave, con un silbido
                {
                    float corte = Mathf.Lerp(0.85f, 0.15f, k);
                    lp += (Ruido() - lp) * corte;
                    s = (Ruido() - lp) * 0.6f * Mathf.Pow(1f - k, 2f) * Mathf.Min(1f, t * 200f);
                    s += Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(2400f, 900f, k) * t) * 0.12f * (1f - k);
                    break;
                }
                case Efecto.Bloqueo:   // metal apagado: parciales inarmónicos que se apagan rápido
                {
                    float env = Mathf.Exp(-t * 14f);
                    s = (Mathf.Sin(2f * Mathf.PI * 523f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 1231f * t) * 0.3f + Mathf.Sin(2f * Mathf.PI * 1789f * t) * 0.2f) * env;
                    s += Ruido() * 0.25f * Mathf.Exp(-t * 60f);
                    break;
                }
                case Efecto.Dano:   // golpe sordo: un bombo grave con ruido apagado
                {
                    float f = Mathf.Lerp(110f, 45f, k);
                    lp += (Ruido() - lp) * 0.08f;
                    s = Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * 16f) * 0.9f + lp * 1.4f * Mathf.Exp(-t * 25f);
                    break;
                }
                case Efecto.Fuente:   // campana suave
                {
                    float env = Mathf.Min(1f, t * 80f) * Mathf.Exp(-t * 2.4f);
                    s = (Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.45f + Mathf.Sin(2f * Mathf.PI * 1318.5f * t) * 0.22f + Mathf.Sin(2f * Mathf.PI * 2217f * t) * 0.1f
                         + Mathf.Sin(2f * Mathf.PI * 660f * t) * 0.18f) * env;
                    break;
                }
                case Efecto.Alerta:   // pulso grave: avisa que viene un ataque
                {
                    float env = Mathf.Sin(Mathf.PI * Mathf.Min(1f, k * 1.2f));
                    s = (Mathf.Sin(2f * Mathf.PI * 62f * t) * 0.8f + Mathf.Sin(2f * Mathf.PI * 124f * t) * 0.25f) * env * 0.9f;
                    break;
                }
                case Efecto.Paso:   // pisada en piedra: un chasquido corto y grave
                {
                    lp += (Ruido() - lp) * 0.25f;
                    s = lp * 1.6f * Mathf.Exp(-t * 70f) + Mathf.Sin(2f * Mathf.PI * 140f * t) * 0.3f * Mathf.Exp(-t * 60f);
                    break;
                }
                case Efecto.Muerte:   // caída: un tono que baja y un golpe al final
                {
                    float f = Mathf.Lerp(330f, 70f, Mathf.Pow(k, 0.7f));
                    s = Mathf.Sin(2f * Mathf.PI * f * t) * 0.45f * (1f - k);
                    if (k > 0.55f) { lp += (Ruido() - lp) * 0.06f; s += lp * 1.6f * Mathf.Exp(-(t - dur * 0.55f) * 12f); }
                    break;
                }
                case Efecto.Clic:
                    s = Mathf.Sin(2f * Mathf.PI * 1500f * t) * Mathf.Exp(-t * 120f) * 0.5f;
                    break;
                case Efecto.Destello:   // energía que sube (la torre, el haz)
                {
                    lp2 += (Ruido() - lp2) * 0.04f;
                    float env = Mathf.Min(1f, t * 6f) * Mathf.Exp(-t * 1.6f);
                    s = (Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(80f, 240f, k) * t) * 0.4f + lp2 * 2f) * env;
                    break;
                }
            }
            d[i] = Mathf.Clamp(s, -1f, 1f);
        }
        var clip = AudioClip.Create("Sonido_" + e, n, 1, Hz, false);
        clip.SetData(d, 0);
        return clip;
    }
}
