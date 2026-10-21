using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Música del GDD: melancólica, con teclado, órgano y violín; tres pistas (cinemática y menú, combate, jefe), sin
/// música dinámica: cambia al cambiar de escena (y al empezar la pelea con el jefe). Si existe Resources/Audio/Musica_&lt;pista&gt;
/// (las de Francisco) se usa esa; si no, suena una provisional sintetizada aquí. Volumen = general × música, más bajo en
/// la cinemática, y siempre por debajo de los efectos (mezcla del GDD).
/// </summary>
public class Musica : MonoBehaviour
{
    public enum Pista { Ninguna, Menu, Combate, Jefe }

    const int Hz = 22050;
    static Musica instancia;
    static readonly Dictionary<Pista, AudioClip> clips = new Dictionary<Pista, AudioClip>();
    AudioSource a, b;
    Pista actual;
    float baja = 1f;

    public static void Poner(Pista p)
    {
        Asegurar();
        instancia.Cambiar(p);
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
        SceneManager.sceneLoaded += (s, m) => PorEscena(s.name);
        PorEscena(SceneManager.GetActiveScene().name);
    }

    static void PorEscena(string n)
    {
        if (n == "Menu" || n == "Intro") Poner(Pista.Menu);
        else if (n == "Diorama" || n == "Puerto" || n == "Cascadas" || n == "Acantilado" || n == "Cima" || n == "Expedicion") Poner(Pista.Combate);
        Cinematica(n == "Intro");
    }

    void Cambiar(Pista p)
    {
        if (p == actual) return;
        actual = p;
        StopAllCoroutines();
        StartCoroutine(Cruzar(p));
    }

    IEnumerator Cruzar(Pista p)
    {
        var sale = a.isPlaying ? a : b;
        var entra = sale == a ? b : a;
        if (p != Pista.Ninguna)
        {
            entra.clip = Clip(p);
            entra.volume = 0f;
            entra.Play();
        }
        float v0 = sale.volume;
        for (float t = 0f; t < 1.5f; t += Time.unscaledDeltaTime)
        {
            float k = t / 1.5f;
            sale.volume = v0 * (1f - k);
            if (p != Pista.Ninguna) entra.volume = Volumen() * k;
            yield return null;
        }
        sale.Stop();
    }

    float Volumen() => 0.32f * Ajustes.General * Ajustes.Musica * baja;

    void Update()
    {
        var s = a.isPlaying && a.volume >= b.volume ? a : b;
        if (s.isPlaying) s.volume = Mathf.MoveTowards(s.volume, Volumen(), Time.unscaledDeltaTime * 0.5f);
    }

    // ------------------------------------------------------------------ pistas provisionales

    static AudioClip Clip(Pista p)
    {
        if (clips.TryGetValue(p, out var c) && c != null) return c;
        c = Resources.Load<AudioClip>("Audio/Musica_" + p);
        if (c == null) c = Componer(p);
        clips[p] = c;
        return c;
    }

    static float Nota(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

    /// <summary>Acordes de órgano, un arpegio de teclado y una línea de violín sobre una progresión menor que se repite.</summary>
    static AudioClip Componer(Pista p)
    {
        // progresiones (raíz MIDI y si es menor) y tempo de cada pista
        int[][] acordes; float bpm; int compasPorAcorde = 1;
        switch (p)
        {
            case Pista.Combate:
                acordes = new[] { new[] { 50, 0 }, new[] { 46, 1 }, new[] { 41, 1 }, new[] { 45, 1 } };   // Rem Sib Fa La
                bpm = 92f; compasPorAcorde = 2; break;
            case Pista.Jefe:
                acordes = new[] { new[] { 45, 0 }, new[] { 43, 1 }, new[] { 41, 1 }, new[] { 40, 1 } };   // Lam Sol Fa Mi
                bpm = 124f; compasPorAcorde = 2; break;
            default:
                acordes = new[] { new[] { 45, 0 }, new[] { 41, 1 }, new[] { 48, 1 }, new[] { 40, 1 } };   // Lam Fa Do Mi
                bpm = 64f; break;
        }
        float tiempo = 60f / bpm, compas = tiempo * 4f;
        float dur = compas * compasPorAcorde * acordes.Length;
        int n = Mathf.CeilToInt(dur * Hz);
        var d = new float[n];
        var rng = new System.Random((int)p * 31 + 5);
        // melodía de violín: grados del acorde, una nota por tiempo (en combate/jefe, a corcheas)
        float paso = p == Pista.Menu ? tiempo * 2f : p == Pista.Combate ? tiempo : tiempo * 0.5f;
        int notas = Mathf.CeilToInt(dur / paso);
        var mel = new int[notas];
        for (int i = 0; i < notas; i++)
        {
            int ac = Mathf.Min(acordes.Length - 1, Mathf.FloorToInt(i * paso / (compas * compasPorAcorde)));
            int raiz = acordes[ac][0] + 24, menor = acordes[ac][1] == 0 ? 3 : 4;
            int[] grados = { 0, menor, 7, 12, menor + 12 };
            mel[i] = raiz + grados[rng.Next(grados.Length)] - (rng.NextDouble() < 0.3 ? 12 : 0);
        }
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Hz;
            int ac = Mathf.Min(acordes.Length - 1, Mathf.FloorToInt(t / (compas * compasPorAcorde)));
            float tAc = t - ac * compas * compasPorAcorde, durAc = compas * compasPorAcorde;
            int raiz = acordes[ac][0], tercera = acordes[ac][1] == 0 ? 3 : 4;
            float s = 0f;
            // órgano: el acorde con armónicos, ataque y caída lentos
            float envO = Mathf.Min(1f, tAc / 0.6f) * Mathf.Min(1f, (durAc - tAc) / 0.4f);
            foreach (int iv in new[] { 0, tercera, 7, 12 })
            {
                float f = Nota(raiz + 12 + iv);
                s += (Mathf.Sin(2f * Mathf.PI * f * t) * 0.5f + Mathf.Sin(4f * Mathf.PI * f * t) * 0.22f + Mathf.Sin(6f * Mathf.PI * f * t) * 0.1f) * 0.11f * envO;
            }
            // bajo del órgano (en combate y jefe, pulsando)
            float fb = Nota(raiz);
            float pulso = p == Pista.Menu ? 1f : 0.55f + 0.45f * Mathf.Exp(-Mathf.Repeat(t, p == Pista.Jefe ? tiempo * 0.5f : tiempo) * 6f);
            s += Mathf.Sin(2f * Mathf.PI * fb * t) * 0.22f * envO * pulso;
            // teclado: arpegio suave (campanita que se apaga)
            float pasoA = p == Pista.Jefe ? tiempo * 0.25f : tiempo * 0.5f;
            int ia = Mathf.FloorToInt(tAc / pasoA);
            float ta = tAc - ia * pasoA;
            int[] arp = { 0, tercera, 7, 12, 7, tercera };
            float fa = Nota(raiz + 24 + arp[ia % arp.Length]);
            s += Mathf.Sin(2f * Mathf.PI * fa * t) * Mathf.Exp(-ta * 5f) * (p == Pista.Menu ? 0.09f : 0.06f);
            // violín: la melodía con vibrato y un poco de aspereza
            int im = Mathf.Min(notas - 1, Mathf.FloorToInt(t / paso));
            float tm = t - im * paso;
            float fv = Nota(mel[im]) * (1f + 0.006f * Mathf.Sin(2f * Mathf.PI * 5.5f * t) * Mathf.Min(1f, tm * 3f));
            float envV = Mathf.Min(1f, tm / (paso * 0.25f)) * Mathf.Min(1f, (paso - tm) / (paso * 0.2f));
            float fase = 2f * Mathf.PI * fv * t;
            s += (Mathf.Sin(fase) * 0.6f + Mathf.Sin(2f * fase) * 0.25f + Mathf.Sin(3f * fase) * 0.12f) * envV * (p == Pista.Menu ? 0.13f : 0.1f);
            if (p == Pista.Jefe) s += Mathf.Sin(2f * Mathf.PI * Nota(raiz + 12) * t) * 0.08f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 12f * t));   // trémolo de cuerdas
            d[i] = Mathf.Clamp(s * 0.9f, -1f, 1f);
        }
        // sin chasquido al dar la vuelta
        int borde = Hz / 20;
        for (int i = 0; i < borde; i++) { float k = i / (float)borde; d[i] *= k; d[n - 1 - i] *= k; }
        var clip = AudioClip.Create("Musica_" + p, n, 1, Hz, false);
        clip.SetData(d, 0);
        return clip;
    }
}
