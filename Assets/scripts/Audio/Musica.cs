using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Música del GDD: melancólica, con teclado (piano) y violín (sin órgano sostenido: sonaba a zumbido); tres pistas (cinemática y menú, combate, jefe), sin
/// música dinámica: cambia al cambiar de escena (y al empezar la pelea con el jefe). Cada pista es el archivo
/// Resources/Audio/Musica_&lt;pista&gt; (Menu, Combate, Jefe, Paisaje, Vacia, Final). Volumen = general × música, más bajo en
/// la cinemática, y siempre por debajo de los efectos (mezcla del GDD).
/// </summary>
public class Musica : MonoBehaviour
{
    public enum Pista { Ninguna, Menu, Combate, Jefe, Paisaje, Final, Vacia }

    static Musica instancia;
    static readonly Dictionary<Pista, AudioClip> clips = new Dictionary<Pista, AudioClip>();
    AudioSource a, b;
    Pista actual;
    float baja = 1f;

    public static void Poner(Pista p)
    {
        Asegurar();
        instancia.Cambiar(p, Cruce, Cruce, 0f, false);
    }

    /// <summary>Durante la cinemática la música va más baja (GDD).</summary>
    public static void Cinematica(bool si) { Asegurar(); instancia.baja = si ? 0.6f : 1f; }

    static void Asegurar()
    {
        if (instancia != null) return;
        var go = new GameObject("Musica");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<Musica>();
        instancia.a = go.AddComponent<AudioSource>();
        instancia.b = go.AddComponent<AudioSource>();
        foreach (var s in new[] { instancia.a, instancia.b }) { s.loop = true; s.playOnAwake = false; s.spatialBlend = 0f; s.volume = 0f; s.priority = 0; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AlCargar()
    {
        SceneManager.sceneLoaded += (s, m) => { if (m == LoadSceneMode.Single) PorEscena(s.name); };   // los paisajes del menú (aditivos) no mandan
        PorEscena(SceneManager.GetActiveScene().name);
    }

    /// <summary>Los mapas con río, mar o cascadas: al explorarlos suena "Paisaje" (nostálgica y feliz).</summary>
    public static bool ConAgua(string n) => n == "Diorama" || n == "Puerto" || n == "Cascadas" || n == "Acantilado" || n == "Lago";

    /// <summary>Al empezar o terminar una horda: combate mientras se pelea; en los mapas con agua, vuelve el paisaje.</summary>
    public static void Pelea(bool empieza)
    {
        string n = SceneManager.GetActiveScene().name;
        Pista p;
        if (n == "Nevado" || n == "Cueva") p = empieza ? Pista.Combate : Pista.Vacia;
        else if (ConAgua(n)) p = empieza ? Pista.Combate : Pista.Paisaje;
        else return;
        Asegurar();
        // al empezar: el combate entra rápido. Al terminar: un respiro, el combate se apaga despacio y la pista del lugar
        // vuelve suave desde donde se había quedado (antes arrancaba de cero en 2.5 s y se sentía forzado)
        if (empieza) instancia.Cambiar(p, 1.4f, 1.6f, 0f, false);
        else instancia.Cambiar(p, 4f, 6f, 1.8f, true);
    }

    static void PorEscena(string n)
    {
        if (n == "Menu" || n == "Intro") Poner(Pista.Menu);
        else if (n == "Nevado" || n == "Cueva") Poner(Pista.Vacia);   // el pueblo vacío y la gruta: caja de música inquietante (la Cueva antes heredaba la del mapa anterior)
        else if (ConAgua(n)) Poner(Pista.Paisaje);
        else if (n == "Cima" || n == "Expedicion" || n.StartsWith("Cuartos")) Poner(Pista.Combate);   // las zonas de cuartos usan la pista de combate
        Cinematica(n == "Intro");
    }

    void Cambiar(Pista p, float durSale, float durEntra, float retraso, bool reanudar)
    {
        if (p == actual) return;
        actual = p;
        StopAllCoroutines();
        cruzando = false;
        StartCoroutine(Cruzar(p, durSale, durEntra, retraso, reanudar));
    }

    Pista enA, enB;   // qué pista tiene cada fuente
    readonly Dictionary<Pista, float> posicion = new Dictionary<Pista, float>();   // dónde se quedó cada pista al salir

    /// <summary>Segundos del cruce entre pistas: la que sale se apaga despacio mientras la nueva sube, nunca de golpe.</summary>
    public static float Cruce = 2.5f;

    IEnumerator Cruzar(Pista p, float durSale, float durEntra, float retraso, bool reanudar)
    {
        // entra por la fuente más baja; si un cruce anterior quedó a medias, la otra sigue bajando desde donde iba
        var entra = a.volume <= b.volume ? a : b;
        var sale = entra == a ? b : a;
        Pista pistaSale = sale == a ? enA : enB;
        if (entra.isPlaying) { Recordar(entra == a ? enA : enB, entra); entra.Stop(); }
        float v0 = sale.isPlaying ? sale.volume : 0f;
        entra.volume = 0f;
        if (p != Pista.Ninguna)
        {
            entra.clip = Clip(p);
            if (entra == a) enA = p; else enB = p;
        }
        durSale = Mathf.Max(0.1f, durSale); durEntra = Mathf.Max(0.1f, durEntra);
        float total = Mathf.Max(durSale, retraso + durEntra);
        cruzando = true;
        for (float t = 0f; t < total; t += Time.unscaledDeltaTime)
        {
            // la que sale: curva de igual potencia (no hay hueco ni subidón a la mitad)
            float ks = Mathf.Clamp01(t / durSale);
            sale.volume = v0 * Mathf.Cos(ks * Mathf.PI * 0.5f);
            if (p != Pista.Ninguna && t >= retraso)
            {
                if (!entra.isPlaying)
                {
                    float pos = 0f;
                    if (reanudar && posicion.TryGetValue(p, out pos) && entra.clip != null && entra.clip.length > 1f) entra.time = pos % entra.clip.length;
                    else entra.time = 0f;
                    entra.Play();
                }
                // la que entra: empieza muy suave y va tomando cuerpo
                float ke = Mathf.Clamp01((t - retraso) / durEntra);
                entra.volume = Volumen() * (1f - Mathf.Cos(ke * Mathf.PI * 0.5f));
            }
            yield return null;
        }
        Recordar(pistaSale, sale);
        sale.volume = 0f;
        sale.Stop();
        cruzando = false;
    }

    bool cruzando;

    void Recordar(Pista p, AudioSource s)
    {
        if (p != Pista.Ninguna && s.clip != null && s.isPlaying) posicion[p] = s.time;
    }

    float Volumen() => 0.32f * Ajustes.General * Ajustes.Musica * baja;

    void Update()
    {
        if (cruzando || (a.isPlaying && b.isPlaying)) return;   // en pleno cruce manda Cruzar
        var s = a.isPlaying ? a : b;
        if (s.isPlaying) s.volume = Mathf.MoveTowards(s.volume, Volumen(), Time.unscaledDeltaTime * 0.5f);
    }

    // ------------------------------------------------------------------ archivos

    /// <summary>La pista es Resources/Audio/Musica_&lt;pista&gt;; se carga la primera vez que suena y se queda en caché.</summary>
    static AudioClip Clip(Pista p)
    {
        if (clips.TryGetValue(p, out var c) && c != null) return c;
        c = Resources.Load<AudioClip>("Audio/Musica_" + p);
        if (c == null) Debug.LogWarning("Musica: falta Resources/Audio/Musica_" + p);
        clips[p] = c;
        return c;
    }
}
