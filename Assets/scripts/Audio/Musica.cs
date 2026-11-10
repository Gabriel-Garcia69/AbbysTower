using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Música del GDD: melancólica, con teclado (piano) y violín (sin órgano sostenido: sonaba a zumbido); tres pistas (cinemática y menú, combate, jefe), sin
/// música dinámica: cambia al cambiar de escena (y al empezar la pelea con el jefe). Si existe Resources/Audio/Musica_&lt;pista&gt;
/// (las de Francisco) se usa esa; si no, suena una provisional sintetizada aquí. Volumen = general × música, más bajo en
/// la cinemática, y siempre por debajo de los efectos (mezcla del GDD).
/// </summary>
public class Musica : MonoBehaviour
{
    public enum Pista { Ninguna, Menu, Combate, Jefe, Paisaje, Final, Vacia }

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
        Precalcular();
        SceneManager.sceneLoaded += (s, m) => { if (m == LoadSceneMode.Single) PorEscena(s.name); };   // los paisajes del menú (aditivos) no mandan
        PorEscena(SceneManager.GetActiveScene().name);
    }

    /// <summary>Los mapas con río, mar o cascadas: al explorarlos suena "Paisaje" (nostálgica y feliz).</summary>
    public static bool ConAgua(string n) => n == "Diorama" || n == "Puerto" || n == "Cascadas" || n == "Acantilado" || n == "Lago";

    /// <summary>Al empezar o terminar una horda: combate mientras se pelea; en los mapas con agua, vuelve el paisaje.</summary>
    public static void Pelea(bool empieza)
    {
        string n = SceneManager.GetActiveScene().name;
        if (n == "Nevado") { Poner(empieza ? Pista.Combate : Pista.Vacia); return; }
        if (!ConAgua(n)) return;
        Poner(empieza ? Pista.Combate : Pista.Paisaje);
    }

    static void PorEscena(string n)
    {
        if (n == "Menu" || n == "Intro") Poner(Pista.Menu);
        else if (n == "Nevado") Poner(Pista.Vacia);   // el pueblo vacío: caja de música inquietante
        else if (ConAgua(n)) Poner(Pista.Paisaje);
        else if (n == "Cima" || n == "Expedicion" || n.StartsWith("Cuartos")) Poner(Pista.Combate);   // las zonas de cuartos usan la pista de combate
        Cinematica(n == "Intro");
    }

    void Cambiar(Pista p)
    {
        if (p == actual) return;
        actual = p;
        StopAllCoroutines();
        StartCoroutine(Cruzar(p));
    }

    /// <summary>Segundos del cruce entre pistas: la que sale se apaga despacio mientras la nueva sube, nunca de golpe.</summary>
    public static float Cruce = 2.5f;

    IEnumerator Cruzar(Pista p)
    {
        // entra por la fuente más baja; si un cruce anterior quedó a medias, la otra sigue bajando desde donde iba
        var entra = a.volume <= b.volume ? a : b;
        var sale = entra == a ? b : a;
        float v0 = sale.isPlaying ? sale.volume : 0f, e0 = 0f;
        if (p != Pista.Ninguna)
        {
            entra.clip = Clip(p);
            entra.volume = 0f;
            entra.Play();
        }
        else { entra.Stop(); entra.volume = 0f; }
        float dur = Mathf.Max(0.1f, Cruce);
        for (float t = 0f; t < dur; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            sale.volume = v0 * (1f - k);
            if (p != Pista.Ninguna) entra.volume = Mathf.Lerp(e0, Volumen(), k);
            yield return null;
        }
        sale.volume = 0f;
        sale.Stop();
    }

    float Volumen() => 0.32f * Ajustes.General * Ajustes.Musica * baja;

    void Update()
    {
        if (a.isPlaying && b.isPlaying) return;   // en pleno cruce manda Cruzar
        var s = a.isPlaying ? a : b;
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

    /// <summary>Una nota de piano: fundamental con dos armónicos suaves, ataque inmediato y caída exponencial.</summary>
    static float Piano(float f, float t, float caida)
    {
        if (t < 0f) return 0f;
        float env = Mathf.Min(1f, t * 400f) * Mathf.Exp(-t * caida);
        float w = 2f * Mathf.PI * f * t;
        return (Mathf.Sin(w) + Mathf.Sin(2f * w) * 0.3f * Mathf.Exp(-t * 3f) + Mathf.Sin(3f * w) * 0.12f * Mathf.Exp(-t * 5f)) * env;
    }

    /// <summary>
    /// Un violín sintetizado: suma de armónicos (como una onda de diente de sierra, que es lo que hace el arco) cuyo volumen
    /// lo da la caja del violín (resonancias cerca de 280, 450, 1000 y 2800 Hz) y que se apagan arriba de 5 kHz; vibrato
    /// de 5.5 Hz que aparece después de 0.25 s, pequeñas variaciones de afinación y el ruido del arco.
    /// </summary>
    class Violin
    {
        readonly float desafina; readonly System.Random r;
        double fase; float ruido, deriva;
        public Violin(float desafina, int semilla) { this.desafina = desafina; r = new System.Random(semilla); }

        static float Caja(float f)
        {
            float l = Mathf.Log(f);
            float g = 0.25f;
            g += 0.7f * Mathf.Exp(-Sq((l - Mathf.Log(280f)) / 0.18f));
            g += 0.9f * Mathf.Exp(-Sq((l - Mathf.Log(450f)) / 0.2f));
            g += 0.45f * Mathf.Exp(-Sq((l - Mathf.Log(1000f)) / 0.25f));
            g += 1.0f * Mathf.Exp(-Sq((l - Mathf.Log(2800f)) / 0.3f));
            return g / (1f + Sq(f / 5000f) * Sq(f / 5000f));
        }

        static float Sq(float x) => x * x;

        public float Muestra(float f, float tNota)
        {
            float vib = 0.0045f * Mathf.Sin(2f * Mathf.PI * 5.5f * tNota + desafina * 900f) * Mathf.SmoothStep(0f, 1f, (tNota - 0.25f) / 0.35f);
            deriva += ((float)r.NextDouble() - 0.5f) * 0.00004f; deriva *= 0.999f;
            float fr = f * (1f + desafina + vib + deriva);
            fase += fr / Hz;
            if (fase > 1000.0) fase -= 1000.0;
            float w = (float)(2.0 * System.Math.PI * (fase % 1.0));
            float s = 0f;
            for (int k = 1; k * fr < 8000f && k <= 24; k++) s += Mathf.Sin(w * k) * Caja(k * fr) / k;
            // roce del arco: un poco de ruido suave
            ruido += (((float)r.NextDouble() * 2f - 1f) - ruido) * 0.3f;
            return s * 0.6f + ruido * 0.035f;
        }
    }

    static float Nota(int midi) => 440f * Mathf.Pow(2f, (midi - 69) / 12f);

    /// <summary>Piano (bajo, acordes y arpegio) y una línea de violín sobre una progresión menor que se repite.</summary>
    static float[] Muestras(Pista p)
    {
        if (p == Pista.Final) return MuestrasFinal();
        if (p == Pista.Vacia) return MuestrasVacia();
        // progresiones (raíz MIDI y si es menor) y tempo de cada pista
        int[][] acordes; float bpm; int compasPorAcorde = 1;
        switch (p)
        {
            case Pista.Combate:
                acordes = new[] { new[] { 50, 0 }, new[] { 46, 1 }, new[] { 41, 1 }, new[] { 45, 1 } };   // Rem Sib Fa La
                bpm = 92f; compasPorAcorde = 2; break;
            case Pista.Paisaje:
                // Re mayor: Re · La · Sim · Sol (I-V-vi-IV), lento y luminoso, con un poco de melancolía en el Si menor
                acordes = new[] { new[] { 50, 1 }, new[] { 45, 1 }, new[] { 47, 0 }, new[] { 43, 1 } };
                bpm = 78f; compasPorAcorde = 2; break;
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
        float paso = p == Pista.Menu ? tiempo * 2f : p == Pista.Paisaje ? tiempo * 2f : p == Pista.Combate ? tiempo : tiempo * 0.5f;
        int notas = Mathf.CeilToInt(dur / paso);
        var mel = new int[notas];
        for (int i = 0; i < notas; i++)
        {
            int ac = Mathf.Min(acordes.Length - 1, Mathf.FloorToInt(i * paso / (compas * compasPorAcorde)));
            int raiz = acordes[ac][0] + 24, menor = acordes[ac][1] == 0 ? 3 : 4;
            int[] grados = { 0, menor, 7, 12, menor + 12 };
            // la nota del acorde más cercana a la anterior (a veces la siguiente): una línea que canta, sin saltos raros
            int prev = i > 0 ? mel[i - 1] : raiz + 7, mejor = raiz, dist = 99;
            foreach (int g in grados)
                for (int oct = -12; oct <= 0; oct += 12)
                {
                    int cand = raiz + g + oct, dd = Mathf.Abs(cand - prev) + (cand == prev ? 3 : 0) + rng.Next(0, 4);
                    if (cand >= 64 && cand <= 84 && dd < dist) { dist = dd; mejor = cand; }
                }
            mel[i] = mejor;
        }
        var cuerdas = new Violin[] { new Violin(-0.0025f, 1), new Violin(0.0025f, 2) };   // dos violines un pelo desafinados: suena a sección
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Hz;
            int ac = Mathf.Min(acordes.Length - 1, Mathf.FloorToInt(t / (compas * compasPorAcorde)));
            float tAc = t - ac * compas * compasPorAcorde, durAc = compas * compasPorAcorde;
            int raiz = acordes[ac][0], tercera = acordes[ac][1] == 0 ? 3 : 4;
            float s = 0f;
            // (sin órgano sostenido: el acorde con armónicos y el bajo pulsando sonaban como un zumbido de fondo)
            // piano: bajo al inicio de cada compás y el acorde suave encima; todo se apaga solo como una tecla
            float tCompas = Mathf.Repeat(tAc, compas);
            s += Piano(Nota(raiz + 12), tCompas, 1.6f) * 0.2f;
            if (p != Pista.Menu)
            {
                float medio = tCompas - compas * 0.5f;   // en combate y jefe, el bajo repite a medio compás
                if (medio >= 0f) s += Piano(Nota(raiz + 12), medio, 2f) * 0.13f;
            }
            foreach (int iv in new[] { 0, tercera, 7 })
                s += Piano(Nota(raiz + 24 + iv), tCompas, 1.1f) * 0.07f;
            // piano: el arpegio
            float pasoA = p == Pista.Jefe ? tiempo * 0.25f : tiempo * 0.5f;
            int ia = Mathf.FloorToInt(tAc / pasoA);
            float ta = tAc - ia * pasoA;
            int[] arp = p == Pista.Paisaje ? new[] { 0, 7, 12, tercera + 12, 14, tercera + 12, 12, 7 } : new[] { 0, tercera, 7, 12, 7, tercera };   // en el paisaje, con la novena: corre como agua
            s += Piano(Nota(raiz + 36 + arp[ia % arp.Length]), ta, p == Pista.Paisaje ? 2.4f : 3.5f) * (p == Pista.Menu ? 0.16f : p == Pista.Paisaje ? 0.13f : 0.12f);
            if (p == Pista.Paisaje)
            {
                // gotas: campanitas agudas sueltas, como agua que cae (posiciones fijas por semilla para que el loop sea igual)
                float pasoG = tiempo * 0.5f;
                int ig = Mathf.FloorToInt(t / pasoG);
                uint h = (uint)(ig * 2654435761u);
                if ((h >> 7) % 5 == 0)
                {
                    float tg = t - ig * pasoG;
                    int[] gotas = { 12, 14, 16, 19, 21, 24 };
                    float fg = Nota(raiz + 48 + gotas[(h >> 11) % (uint)gotas.Length] - (raiz % 12 == 7 ? 12 : 0));
                    s += (Mathf.Sin(2f * Mathf.PI * fg * t) + Mathf.Sin(2f * Mathf.PI * fg * 2.76f * t) * 0.25f) * Mathf.Exp(-tg * 7f) * Mathf.Min(1f, tg * 600f) * 0.045f;
                }
            }
            // violín: arco con muchos armónicos pasados por la caja de resonancia, roce del arco y vibrato que entra tarde
            int im = Mathf.Min(notas - 1, Mathf.FloorToInt(t / paso));
            float tm = t - im * paso;
            float f0 = Nota(mel[im]), fAnt = Nota(mel[Mathf.Max(0, im - 1)]);
            float fv = Mathf.Lerp(fAnt, f0, Mathf.SmoothStep(0f, 1f, tm / 0.07f));   // se desliza a la nota nueva (legato)
            // arcada: entra rápido pero suave, crece un poco y al final de la nota baja sin cortarse del todo (ligado)
            float envV = (0.55f + 0.45f * Mathf.SmoothStep(0f, 1f, tm / Mathf.Min(0.18f, paso * 0.4f))) * (1f - 0.35f * Mathf.SmoothStep(0f, 1f, (tm - paso * 0.75f) / (paso * 0.25f)));
            if (im == 0) envV *= Mathf.SmoothStep(0f, 1f, tm / 0.3f);
            float vol = p == Pista.Menu ? 0.085f : p == Pista.Paisaje ? 0.07f : 0.06f;
            foreach (var v in cuerdas) s += v.Muestra(fv, tm) * envV * vol;
            d[i] = Mathf.Clamp(s * 1.1f, -1f, 1f);
        }
        // sin chasquido al dar la vuelta
        int borde = Hz / 20;
        for (int i = 0; i < borde; i++) { float k = i / (float)borde; d[i] *= k; d[n - 1 - i] *= k; }
        return d;
    }

    /// <summary>
    /// "Ventanas encendidas", la música de la aldea vacía: chiptune de consola portátil (ondas cuadradas delgadas y un bajo de
    /// triángulo). Un arpegio agudo y rápido que da vueltas con un tritono que no encaja, una melodía que baja de medio en medio
    /// tono y un bajo quieto. Pieza original, con la inquietud de los pueblos fantasma de los RPG de los 90.
    /// </summary>
    static float[] MuestrasVacia()
    {
        const float bpm = 116f;
        float tiempo = 60f / bpm, corchea = tiempo * 0.5f, compas = tiempo * 4f;
        // dos compases por acorde: La, Fa, Re, Mi (raíz del bajo)
        int[] bajos = { 45, 41, 38, 40 };
        // el arpegio agudo: cuatro notas que se repiten; la tercera choca (tritono / séptima mayor)
        int[][] arps =
        {
            new[] { 81, 88, 87, 84 },   // La Mi Re# Do
            new[] { 77, 84, 83, 81 },   // Fa Do Si La
            new[] { 74, 81, 80, 77 },   // Re La Sol# Fa
            new[] { 76, 83, 80, 82 },   // Mi Si Sol# La#
        };
        // la melodía: baja de medio en medio tono, a blancas (0 = sigue la anterior)
        int[] mel = { 69, 0, 68, 0, 67, 0, 66, 0, 65, 0, 64, 0, 62, 0, 64, 0 };
        int compases = 16;   // dos vueltas: la segunda con la melodía una octava arriba y el viento
        float dur = compas * compases;
        int n = Mathf.CeilToInt(dur * Hz);
        var d = new float[n];
        double fArp = 0, fMel = 0, fBaj = 0;
        float lp = 0f, ruido = 0f;
        var rng = new System.Random(77);
        float Cuadrada(double fase, float ciclo) => (float)(fase % 1.0) < ciclo ? 1f : -1f;
        float Triangulo(double fase) { float x = (float)(fase % 1.0); return x < 0.5f ? x * 4f - 1f : 3f - x * 4f; }
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Hz;
            int c = Mathf.FloorToInt(t / compas), vuelta = c / 8, ac = (c % 8) / 2;
            float s = 0f;
            // arpegio a corcheas, cuadrada de pulso muy delgado, con la caída de volumen de las consolas
            int ic = Mathf.FloorToInt(t / corchea);
            float tn = t - ic * corchea;
            int nota = arps[ac][ic % 4];
            fArp += Nota(nota) / Hz;
            float envA = Mathf.Max(0.25f, 1f - tn / corchea * 0.75f);
            s += Cuadrada(fArp, 0.125f) * envA * 0.05f;
            // melodía: pulso de un cuarto, con vibrato que entra tarde
            float blanca = tiempo * 2f;
            int im = Mathf.FloorToInt((t % (compas * 8f)) / blanca), ini = Mathf.Min(im, mel.Length - 1);
            while (ini > 0 && mel[ini] == 0) ini--;
            float tm = (t % (compas * 8f)) - ini * blanca;
            float fm = Nota(mel[ini] + (vuelta == 1 ? 12 : 0)) * (1f + 0.006f * Mathf.Sin(2f * Mathf.PI * 5.5f * tm) * Mathf.Clamp01((tm - 0.3f) / 0.3f));
            fMel += fm / Hz;
            float largo = blanca * 2f;
            float envM = Mathf.Min(1f, tm * 60f) * (1f - 0.5f * Mathf.Clamp01(tm / largo));
            s += Cuadrada(fMel, 0.25f) * envM * 0.045f;
            // bajo: triángulo a negras, raíz y octava
            int iq = Mathf.FloorToInt(t / tiempo);
            float tq = t - iq * tiempo;
            fBaj += Nota(bajos[ac] - 12 + (iq % 2 == 1 ? 12 : 0)) / Hz;
            s += Triangulo(fBaj) * Mathf.Min(1f, tq * 80f) * Mathf.Exp(-tq * 1.5f) * 0.16f;
            // en la segunda vuelta, viento: ruido muy suave que sube y baja
            if (vuelta == 1)
            {
                ruido += (((float)rng.NextDouble() * 2f - 1f) - ruido) * 0.02f;
                s += ruido * 0.5f * (0.5f + 0.5f * Mathf.Sin(t * 0.6f));
            }
            // un filtro suave: la cuadrada de consola sin que chille
            lp += (s - lp) * 0.55f;
            d[i] = lp;
        }
        // un eco corto, como un cuarto de piedra
        int r1 = (int)(0.19f * Hz);
        var o = new float[n];
        for (int i = 0; i < n; i++) o[i] = Mathf.Clamp(d[i] + (i >= r1 ? o[i - r1] * 0.22f : 0f), -1f, 1f);
        int borde = Hz / 20;
        for (int i = 0; i < borde; i++) { float k = i / (float)borde; o[i] *= k; o[n - 1 - i] *= k; }
        return o;
    }

    /// <summary>
    /// "Despedida", la música del final: Re menor, lenta. Ocho compases de piano solo, como caja de música (Abby camina sola);
    /// la misma melodía con violín (lo encuentra); una parte que sube (los recuerdos); y vuelve al piano para acabar en Re mayor.
    /// Con eco de sala, para que suene a lugar grande y vacío.
    /// </summary>
    static float[] MuestrasFinal()
    {
        const float bpm = 58f;
        float tiempo = 60f / bpm, compas = tiempo * 4f;
        // un acorde por compás: raíz MIDI y si es mayor (1) o menor (0)
        int[][] A = { new[] { 50, 0 }, new[] { 46, 1 }, new[] { 41, 1 }, new[] { 48, 1 }, new[] { 43, 0 }, new[] { 46, 1 }, new[] { 45, 1 }, new[] { 45, 1 } };   // Rem Sib Fa Do Solm Sib La La
        int[][] B = { new[] { 46, 1 }, new[] { 41, 1 }, new[] { 48, 1 }, new[] { 45, 0 }, new[] { 43, 0 }, new[] { 50, 0 }, new[] { 46, 1 }, new[] { 45, 1 } };   // Sib Fa Do Lam Solm Rem Sib La
        int[][] Fin = { new[] { 50, 0 }, new[] { 46, 1 }, new[] { 41, 1 }, new[] { 48, 1 }, new[] { 43, 0 }, new[] { 46, 1 }, new[] { 45, 1 }, new[] { 50, 1 } };   // ... y Re mayor
        // la melodía: dos blancas por compás (0 = nota ligada a la anterior)
        int[] melA = { 69, 65, 74, 72, 69, 72, 67, 64, 70, 69, 67, 65, 64, 73, 69, 0 };
        int[] melB = { 77, 74, 72, 69, 76, 79, 76, 72, 74, 70, 69, 65, 77, 74, 73, 0 };
        int[] melFin = { 69, 65, 74, 72, 69, 72, 67, 64, 70, 69, 67, 65, 64, 73, 74, 0 };
        var acordes = new List<int[]>(); var mel = new List<int>(); var seccion = new List<int>();
        void Agregar(int[][] ac, int[] m, int sec) { acordes.AddRange(ac); mel.AddRange(m); for (int k = 0; k < ac.Length; k++) seccion.Add(sec); }
        Agregar(A, melA, 0); Agregar(A, melA, 1); Agregar(B, melB, 2); Agregar(Fin, melFin, 3);
        float dur = compas * acordes.Count + 4f;   // y unos segundos para que se apague
        int n = Mathf.CeilToInt(dur * Hz);
        var d = new float[n];
        var cuerdas = new Violin[] { new Violin(-0.0025f, 11), new Violin(0.0025f, 12) };
        var segunda = new Violin(0.001f, 13);
        float blanca = tiempo * 2f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)Hz;
            int c = Mathf.Min(acordes.Count - 1, Mathf.FloorToInt(t / compas));
            float tc = t - c * compas;
            if (t > compas * acordes.Count) tc = t - c * compas;   // el último acorde se queda sonando
            int raiz = acordes[c][0], tercera = acordes[c][1] == 0 ? 3 : 4, sec = seccion[c];
            bool ultimo = c == acordes.Count - 1;
            float s = 0f;
            // piano: el bajo y el acorde al empezar el compás, el acorde otra vez suave a la mitad
            s += Piano(Nota(raiz), tc, ultimo ? 0.35f : 0.9f) * 0.17f;
            foreach (int iv in new[] { 0, tercera, 7 })
            {
                s += Piano(Nota(raiz + 12 + iv), tc, ultimo ? 0.45f : 1f) * 0.06f;
                float med = tc - compas * 0.5f;
                if (med >= 0f && !ultimo) s += Piano(Nota(raiz + 12 + iv), med, 1.2f) * 0.035f;
            }
            // arpegio lento a negras, más quieto al principio y al final
            if (!ultimo)
            {
                int[] arp = { 12, 19, 24, 12 + tercera + 12 };
                int ia = Mathf.FloorToInt(tc / tiempo);
                float ta = tc - ia * tiempo;
                s += Piano(Nota(raiz + 12 + arp[ia % arp.Length]), ta, 1.8f) * (sec == 0 || sec == 3 ? 0.07f : 0.09f);
            }
            // la melodía: buscar la nota que suena (las ligadas siguen a la anterior)
            int im = Mathf.Min(mel.Count - 1, Mathf.FloorToInt(t / blanca)), inicio = im;
            while (inicio > 0 && mel[inicio] == 0) inicio--;
            float tm = t - inicio * blanca;
            float f0 = Nota(mel[inicio]);
            if (sec == 0 || sec == 3)
            {
                // piano agudo, con un brillo de caja de música
                float k = Mathf.Min(1f, tm * 400f) * Mathf.Exp(-tm * (ultimo ? 0.5f : 0.9f));
                s += Piano(Nota(mel[inicio] + 12), tm, ultimo ? 0.5f : 0.9f) * 0.17f;
                s += Mathf.Sin(2f * Mathf.PI * Nota(mel[inicio] + 24) * t) * k * 0.035f;
            }
            else
            {
                // violín: legato de una nota a la otra, con arcada que crece y baja
                int ant = inicio;
                do { ant--; } while (ant > 0 && mel[ant] == 0);
                float fAnt = ant >= 0 ? Nota(mel[ant]) : f0;
                float fv = Mathf.Lerp(fAnt, f0, Mathf.SmoothStep(0f, 1f, tm / 0.12f));
                float largo = blanca * (inicio + 1 < mel.Count && mel[inicio + 1] == 0 ? 2f : 1f);
                float env = (0.5f + 0.5f * Mathf.SmoothStep(0f, 1f, tm / 0.45f)) * (1f - 0.3f * Mathf.SmoothStep(0f, 1f, (tm - largo * 0.7f) / (largo * 0.3f)));
                // al empezar la sección, el violín entra poco a poco
                float entra = Mathf.SmoothStep(0f, 1f, (t - compas * (sec == 1 ? 8f : 16f)) / (compas * 1.5f));
                foreach (var v in cuerdas) s += v.Muestra(fv, tm) * env * 0.075f * (sec == 1 ? entra : 1f);
                if (sec == 2) s += segunda.Muestra(Nota(mel[inicio] - (raiz % 12 == 9 || raiz % 12 == 4 ? 3 : 4)), tm) * env * 0.04f;   // una segunda voz, más abajo
            }
            d[i] = s;
        }
        // eco de sala: dos reflexiones que se van apagando
        int r1 = (int)(0.17f * Hz), r2 = (int)(0.41f * Hz), r3 = (int)(0.83f * Hz);
        for (int i = 0; i < n; i++)
        {
            float e = 0f;
            if (i >= r1) e += d[i - r1] * 0.28f;
            if (i >= r2) e += d[i - r2] * 0.18f;
            if (i >= r3) e += d[i - r3] * 0.1f;
            d[i] = Mathf.Clamp((d[i] + e) * 1.05f, -1f, 1f);
        }
        int borde = Hz / 10;
        for (int i = 0; i < borde; i++) { float k = i / (float)borde; d[i] *= k; d[n - 1 - i] *= k; }
        return d;
    }

    /// <summary>Muestras ya calculadas en segundo plano (componer una pista tarda; hacerlo al empezar una pelea trababa el juego).</summary>
    static readonly System.Collections.Concurrent.ConcurrentDictionary<Pista, float[]> listas = new System.Collections.Concurrent.ConcurrentDictionary<Pista, float[]>();
    static bool precalculando;

    /// <summary>Compone todas las pistas en otro hilo al arrancar el juego: Mathf y System.Random no tocan la API de Unity.</summary>
    static void Precalcular()
    {
        if (precalculando) return;
        precalculando = true;
        var pistas = new List<Pista>();
        foreach (Pista p in System.Enum.GetValues(typeof(Pista)))
            if (p != Pista.Ninguna && Resources.Load<AudioClip>("Audio/Musica_" + p) == null) pistas.Add(p);
        System.Threading.Tasks.Task.Run(() =>
        {
            foreach (var p in pistas)
                if (!listas.ContainsKey(p)) listas.TryAdd(p, Muestras(p));
        });
    }

    static AudioClip Componer(Pista p)
    {
        if (!listas.TryGetValue(p, out var d)) d = Muestras(p);   // aún no estaba lista: se compone aquí (una sola vez)
        var clip = AudioClip.Create("Musica_" + p, d.Length, 1, Hz, false);
        clip.SetData(d, 0);
        return clip;
    }
}
