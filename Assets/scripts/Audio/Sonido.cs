using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Efectos de sonido del GDD (05 · Audio): golpe de espada (corte), bloqueo (metal apagado), daño recibido (golpe
/// sordo), fuente de curación (campana suave), alerta de sombra (pulso grave), pasos en piedra, muerte (caída) e impulso (ráfaga de aire), golpe que conecta (uno o varios), bloqueo perfecto y guardia rota.
/// Estéreo simple, sin audio espacial. Si existe Resources/Audio/&lt;Nombre&gt; (wav/ogg de Francisco) se usa ese; si no,
/// suena uno provisional sintetizado aquí. Volumen = general × efectos (Ajustes).
/// </summary>
public static class Sonido
{
    public enum Efecto { Espada, Bloqueo, Dano, Fuente, Alerta, Paso, Muerte, Clic, Destello, Impulso, Rugido, Temblor, Impacto, Latido, Colgante, Ventana, Golpe, GolpeMulti, Parada, GuardiaRota }

    const int Hz = 22050;
    static readonly Dictionary<Efecto, AudioClip> clips = new Dictionary<Efecto, AudioClip>();
    static readonly Dictionary<Efecto, float> ultimo = new Dictionary<Efecto, float>();
    static AudioSource[] voces;
    static int siguiente;
    static System.Random rng = new System.Random(7);

    /// <summary>Todos los efectos un poco más abajo: con los pasos, tajos y avisos juntos, la mezcla cansaba.</summary>
    const float Mezcla = 0.8f;

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
        v.PlayOneShot(Clip(e), Mathf.Clamp01(volumen) * Ajustes.General * Ajustes.Efectos * Mezcla);
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
        go.AddComponent<OyenteRespaldo>();   // oído solo cuando no hay otro (antes quedaban dos y Unity avisaba sin parar)
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

    static float Duracion(Efecto e)
    {
        switch (e)
        {
            case Efecto.Colgante: return 4.2f;
            case Efecto.Ventana: return 2.6f;
            case Efecto.Fuente: return 1.8f;
            case Efecto.Muerte: return 1.1f;
            case Efecto.Alerta: return 0.45f;
            case Efecto.Bloqueo: return 0.38f;
            case Efecto.Paso: return 0.09f;
            case Efecto.Clic: return 0.05f;
            case Efecto.Destello: return 1.6f;
            case Efecto.Impulso: return 0.32f;
            case Efecto.Rugido: return 2.6f;
            case Efecto.Temblor: return 1.8f;
            case Efecto.Impacto: return 1.5f;
            case Efecto.Latido: return 0.9f;
            case Efecto.Espada: return 0.19f;
            case Efecto.Golpe: return 0.26f;
            case Efecto.GolpeMulti: return 0.5f;
            case Efecto.Parada: return 0.9f;
            case Efecto.GuardiaRota: return 0.7f;
            default: return 0.22f;
        }
    }

    /// <summary>Los que pasan por filtros resonantes o van en capas: se normalizan para que nunca saturen ni queden bajitos.</summary>
    static bool Normaliza(Efecto e) => e == Efecto.Rugido || e == Efecto.Temblor || e == Efecto.Impacto || e == Efecto.Impulso || e == Efecto.Espada
                                     || e == Efecto.Bloqueo || e == Efecto.Golpe || e == Efecto.GolpeMulti || e == Efecto.Parada || e == Efecto.GuardiaRota;

    static AudioClip Sintetizar(Efecto e)
    {
        float dur = Duracion(e);
        int n = Mathf.CeilToInt(dur * Hz);
        var d = new float[n];
        float lp = 0f, lp2 = 0f, banda = 0f, fase = 0f, lp3 = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Hz, k = t / dur, s = 0f;
            switch (e)
            {
                case Efecto.Espada:   // tajo al aire: un "fsht" de aire cortado que baja de agudo a medio, sin pitido
                {
                    float fc = Mathf.Lerp(4200f, 850f, Mathf.Pow(k, 0.6f));
                    float g = 2f * Mathf.Sin(Mathf.PI * fc / Hz);
                    lp2 += g * banda;
                    banda += g * (Ruido() - lp2 - 0.4f * banda);
                    float env = Mathf.Min(1f, t * 260f) * Mathf.Pow(1f - k, 1.6f);
                    lp += (Ruido() - lp) * 0.5f;
                    s = banda * env + (Ruido() - lp) * 0.1f * env;
                    break;
                }
                case Efecto.Bloqueo:   // la espada para el golpe: choque de metal con cuerpo, un golpe grave y el metal vibrando poco
                {
                    float env = Mathf.Exp(-t * 13f);
                    s = (Mathf.Sin(2f * Mathf.PI * 410f * t) * 0.45f + Mathf.Sin(2f * Mathf.PI * 965f * t) * 0.3f
                         + Mathf.Sin(2f * Mathf.PI * 1540f * t) * 0.22f * Mathf.Exp(-t * 8f) + Mathf.Sin(2f * Mathf.PI * 2310f * t) * 0.15f * Mathf.Exp(-t * 14f)) * env;
                    lp += (Ruido() - lp) * 0.3f;
                    s += lp * Mathf.Exp(-t * 45f) * 1.4f;
                    s += Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 70f, k) * t) * Mathf.Exp(-t * 20f) * 0.5f;
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
                case Efecto.Colgante:   // el colgante: tres notas de caja de música, lentas y lejanas, que se quedan sonando
                {
                    float[] at = { 0f, 0.55f, 1.25f };
                    float[] fr = { 1046.5f, 783.99f, 659.26f };   // Do, Sol, Mi: bajando, como un recuerdo
                    for (int q = 0; q < 3; q++)
                    {
                        float tq = t - at[q];
                        if (tq < 0f) continue;
                        float env = Mathf.Min(1f, tq * 500f) * Mathf.Exp(-tq * 1.6f);
                        float w = 2f * Mathf.PI * fr[q] * tq;
                        s += (Mathf.Sin(w) + Mathf.Sin(w * 2.76f) * 0.25f * Mathf.Exp(-tq * 6f) + Mathf.Sin(w * 0.5f) * 0.15f) * env * 0.33f;
                    }
                    break;
                }
                case Efecto.Ventana:   // la luz que se prende en la casa: un acorde tibio y suave que se abre, sin golpe
                {
                    float env = Mathf.SmoothStep(0f, 1f, t / 0.5f) * Mathf.Exp(-Mathf.Max(0f, t - 0.5f) * 1.4f);
                    foreach (float f in new[] { 261.63f, 329.63f, 392f, 523.25f })
                        s += Mathf.Sin(2f * Mathf.PI * f * t + Mathf.Sin(t * 5f) * 0.02f) * 0.13f * env;
                    break;
                }
                case Efecto.Alerta:   // pulso grave: avisa que viene un ataque
                {
                    float env = Mathf.Sin(Mathf.PI * Mathf.Min(1f, k * 1.2f));
                    s = (Mathf.Sin(2f * Mathf.PI * 62f * t) * 0.8f + Mathf.Sin(2f * Mathf.PI * 124f * t) * 0.25f) * env * 0.9f;
                    break;
                }
                case Efecto.Paso:   // pisada: un toque sordo y corto, sin chasquido
                {
                    lp += (Ruido() - lp) * 0.1f;
                    s = lp * 1.4f * Mathf.Exp(-t * 55f) * Mathf.Min(1f, t * 600f) + Mathf.Sin(2f * Mathf.PI * 95f * t) * 0.25f * Mathf.Exp(-t * 50f);
                    break;
                }
                case Efecto.Muerte:   // caída: un tono que baja y un golpe al final
                {
                    float f = Mathf.Lerp(330f, 70f, Mathf.Pow(k, 0.7f));
                    s = Mathf.Sin(2f * Mathf.PI * f * t) * 0.45f * (1f - k);
                    if (k > 0.55f) { lp += (Ruido() - lp) * 0.06f; s += lp * 1.6f * Mathf.Exp(-(t - dur * 0.55f) * 12f); }
                    break;
                }
                case Efecto.Impulso:   // impulso: un soplo de aire suave y oscuro que pasa, sin silbido ni golpe
                {
                    float fc = 260f + 900f * Mathf.Sin(Mathf.PI * Mathf.Pow(k, 0.8f));
                    float g = 2f * Mathf.Sin(Mathf.PI * fc / Hz);
                    lp2 += g * banda;
                    banda += g * (Ruido() - lp2 - 1.1f * banda);   // banda ancha y sin resonar: aire, no pitido
                    lp += (banda - lp) * 0.35f;                     // y le quita lo áspero de arriba
                    float env = Mathf.Pow(Mathf.Sin(Mathf.PI * Mathf.Pow(k, 0.6f)), 1.6f);
                    s = lp * env;
                    break;
                }
                case Efecto.Rugido:   // garganta enorme: grave áspero que sube, se sostiene y se apaga, con un rasposo encima
                {
                    float env = Mathf.Clamp01(k * 5f) * Mathf.Clamp01((1f - k) * 2.4f);
                    float f = Mathf.Lerp(46f, 74f, Mathf.Sin(Mathf.PI * Mathf.Pow(k, 0.7f))) + Mathf.Sin(t * 37f) * 3f;
                    fase += f / Hz;
                    float sierra = 2f * (fase - Mathf.Floor(fase)) - 1f;
                    float fc = 500f + 1100f * Mathf.Sin(Mathf.PI * k);
                    float g = 2f * Mathf.Sin(Mathf.PI * fc / Hz);
                    lp2 += g * banda;
                    banda += g * (sierra * 0.8f + Ruido() * 0.5f - lp2 - 0.7f * banda);
                    s = (lp2 * 0.75f + banda * 0.55f + Mathf.Sin(2f * Mathf.PI * fase * 0.5f) * 0.45f) * env;
                    break;
                }
                case Efecto.Temblor:   // la gruta tiembla: retumbo grave con golpes de piedra
                {
                    lp += (Ruido() - lp) * 0.03f;
                    float env = Mathf.Clamp01(k * 6f) * Mathf.Pow(1f - k, 1.2f);
                    lp2 += (Ruido() - lp2) * 0.2f;
                    float piedras = lp2 * Mathf.Max(0f, Mathf.Sin(t * 19f) - 0.75f) * 3f;
                    s = lp * 6f * env + Mathf.Sin(2f * Mathf.PI * 38f * t) * 0.35f * env + piedras * env;
                    break;
                }
                case Efecto.Impacto:   // los puños contra el suelo: golpe grave enorme, estallido de piedra y cola larga
                {
                    float f = Mathf.Lerp(90f, 30f, Mathf.Min(1f, k * 4f));
                    fase += f / Hz;
                    lp += (Ruido() - lp) * 0.3f;
                    lp2 += (Ruido() - lp2) * 0.04f;
                    s = Mathf.Sin(2f * Mathf.PI * fase) * Mathf.Exp(-t * 3.2f) + lp * 1.2f * Mathf.Exp(-t * 18f) + lp2 * 3.5f * Mathf.Exp(-t * 2.6f);
                    break;
                }
                case Efecto.Latido:   // dos golpes graves y suaves, como un corazón
                {
                    float g1 = Mathf.Exp(-t * 16f), t2 = t - 0.26f, g2 = t2 > 0f ? Mathf.Exp(-t2 * 13f) * 0.75f : 0f;
                    s = (Mathf.Sin(2f * Mathf.PI * 52f * t) * g1 + Mathf.Sin(2f * Mathf.PI * 44f * Mathf.Max(0f, t2)) * g2) * 0.95f;
                    break;
                }
                case Efecto.Golpe:   // el tajo conecta: chasquido, crujido de sombra y un golpe con cuerpo
                {
                    fase += Mathf.Lerp(170f, 55f, Mathf.Min(1f, k * 2.2f)) / Hz;
                    float cuerpo = Mathf.Sin(2f * Mathf.PI * fase) * Mathf.Exp(-t * 18f);
                    float g = 2f * Mathf.Sin(Mathf.PI * 1400f / Hz);
                    lp2 += g * banda;
                    banda += g * (Ruido() - lp2 - 0.8f * banda);
                    float crujido = banda * Mathf.Exp(-t * 38f) * 1.3f;
                    float chasquido = Ruido() * Mathf.Exp(-t * 400f) * 0.8f;
                    lp += (Ruido() - lp) * 0.12f;
                    s = cuerpo + crujido + chasquido + lp * Mathf.Exp(-t * 16f) * 2.2f;
                    break;
                }
                case Efecto.GolpeMulti:   // el tajo agarra a varios: más grave, más largo, dos chasquidos y una cola
                {
                    fase += Mathf.Lerp(130f, 40f, Mathf.Min(1f, k * 2f)) / Hz;
                    float cuerpo = Mathf.Sin(2f * Mathf.PI * fase) * Mathf.Exp(-t * 9f) * 1.1f;
                    float g = 2f * Mathf.Sin(Mathf.PI * 1000f / Hz);
                    lp2 += g * banda;
                    banda += g * (Ruido() - lp2 - 0.7f * banda);
                    float crujido = banda * Mathf.Exp(-t * 20f) * 1.5f;
                    lp3 += (Ruido() - lp3) * 0.35f;
                    float t2 = t - 0.035f;
                    float chasquidos = (Ruido() - lp3) * (Mathf.Exp(-t * 90f) + (t2 > 0f ? Mathf.Exp(-t2 * 90f) * 0.8f : 0f));
                    lp += (Ruido() - lp) * 0.05f;
                    s = cuerpo + crujido + chasquidos + lp * Mathf.Exp(-t * 5f) * 1.6f;
                    break;
                }
                case Efecto.Parada:   // bloqueo perfecto: "ting" de metal brillante que queda sonando y un brillo que sube
                {
                    float env = Mathf.Exp(-t * 5f) * Mathf.Min(1f, t * 900f);
                    s = (Mathf.Sin(2f * Mathf.PI * 1870f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 2805f * t) * 0.35f * Mathf.Exp(-t * 3f)
                         + Mathf.Sin(2f * Mathf.PI * 4120f * t) * 0.22f * Mathf.Exp(-t * 6f) + Mathf.Sin(2f * Mathf.PI * 5530f * t) * 0.15f * Mathf.Exp(-t * 9f)) * env;
                    s += Ruido() * Mathf.Exp(-t * 250f) * 0.6f;
                    fase += Mathf.Lerp(1300f, 2600f, k) / Hz;
                    s += Mathf.Sin(2f * Mathf.PI * fase) * 0.08f * Mathf.Sin(Mathf.PI * k);
                    break;
                }
                case Efecto.GuardiaRota:   // la guardia se rompe: crujido seco, un tono que se cae y un golpe grave
                {
                    lp += (Ruido() - lp) * 0.4f;
                    float crack = (Ruido() - lp) * Mathf.Exp(-t * 35f);
                    fase += Mathf.Lerp(620f, 140f, Mathf.Pow(k, 0.6f)) / Hz;
                    float caida = (Mathf.Sin(2f * Mathf.PI * fase) + Mathf.Sin(4f * Mathf.PI * fase) * 0.35f) * Mathf.Exp(-t * 3f) * 0.4f;
                    float golpe = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(100f, 45f, k) * t) * Mathf.Exp(-t * 12f) * 0.7f;
                    s = crack + caida + golpe;
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
            d[i] = Normaliza(e) ? s : Mathf.Clamp(s, -1f, 1f);
        }
        if (Normaliza(e))
        {
            // el filtro resuena: se normaliza para que nunca sature ni quede bajito
            float pico = 0.001f;
            foreach (var x in d) pico = Mathf.Max(pico, Mathf.Abs(x));
            for (int i = 0; i < n; i++) d[i] *= 0.92f / pico;
        }
        var clip = AudioClip.Create("Sonido_" + e, n, 1, Hz, false);
        clip.SetData(d, 0);
        return clip;
    }
}
