using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Guardado de la partida: al entrar a un lugar de la ruta (de Cala Sombría a la cima) se anota en PlayerPrefs, y el menú
/// ofrece "Continuar" desde ahí. Al terminar el juego (Dante) se borra.
/// Aparte, cada lugar al que se llega queda desbloqueado para siempre (ni el final ni "Nueva partida" lo borran): el menú
/// los ofrece en "Elegir piso" para volver a jugar cualquiera sin empezar de cero. Al terminar el juego se abren todos.
/// </summary>
public static class Progreso
{
    const string Clave = "progreso_escena";
    // toda la ruta: antes faltaban Cueva, Lago y Nevado y "Continuar" regresaba al Acantilado
    static readonly string[] Ruta = { "Exterior", "Diorama", "CuartosA", "Puerto", "Cascadas", "Acantilado", "Cueva", "Lago", "Nevado", "Cima" };

    public static bool Hay => !string.IsNullOrEmpty(Escena);
    public static string Escena => PlayerPrefs.GetString(Clave, "");

    /// <summary>Nombre para el menú: "Piso 2 · Puerto", etc.</summary>
    public static string Nombre => NombreDe(Escena);

    /// <summary>Nombre del piso de cualquier escena de la ruta (la pausa lo muestra arriba).</summary>
    public static string NombreDe(string escena)
    {
        {
            switch (escena)
            {
                case "Exterior": return "Cala Sombría";
                case "Expedicion": return "La expedición";
                case "Diorama": return "Piso 1";
                case "CuartosA": return "Piso 1 · Biblioteca";
                case "Puerto": return "Piso 2";
                case "Cascadas": return "Piso 3";
                case "Acantilado": return "Piso 4";
                case "Cueva": return "Piso 5";
                case "Lago": return "Piso 6";
                case "Nevado": return "Piso 7";
                case "Cima": return "Piso 8";
                default: return escena;
            }
        }
    }

    public static void Borrar() { PlayerPrefs.DeleteKey(Clave); PlayerPrefs.Save(); }

    // ------------------------------------------------------------------ pisos desbloqueados

    const string ClaveDesbloqueo = "pisos_desbloqueados";

    /// <summary>Los lugares de la ruta, en orden (lo que muestra "Elegir piso").</summary>
    public static string[] Pisos => Ruta;

    /// <summary>Nombre del lugar, para acompañar al número de piso en "Elegir piso".</summary>
    public static string Lugar(string escena)
    {
        switch (escena)
        {
            case "Diorama": return "El pueblo";
            case "Puerto": return "El puerto";
            case "Cascadas": return "Las cascadas";
            case "Acantilado": return "El acantilado";
            case "Cueva": return "La gruta";
            case "Lago": return "El lago";
            case "Nevado": return "El pueblo nevado";
            case "Cima": return "La cima";
            default: return "";
        }
    }

    public static bool Desbloqueado(string escena) => Desbloqueados().Contains(escena);
    public static bool HayDesbloqueados => Desbloqueados().Count > 0;

    static System.Collections.Generic.HashSet<string> cache;

    static System.Collections.Generic.HashSet<string> Desbloqueados()
    {
        if (cache != null) return cache;
        // partidas de antes de que existiera esto: se abre todo hasta donde iba el guardado
        if (!PlayerPrefs.HasKey(ClaveDesbloqueo))
        {
            int hasta = System.Array.IndexOf(Ruta, Escena);
            var lista = new System.Collections.Generic.List<string>();
            for (int i = 0; i <= hasta; i++) lista.Add(Ruta[i]);
            PlayerPrefs.SetString(ClaveDesbloqueo, string.Join(",", lista));
            PlayerPrefs.Save();
        }
        var set = new System.Collections.Generic.HashSet<string>();
        foreach (var e in PlayerPrefs.GetString(ClaveDesbloqueo, "").Split(',')) if (e.Length > 0) set.Add(e);
        return cache = set;
    }

    static void Desbloquear(System.Collections.Generic.IEnumerable<string> escenas)
    {
        var set = Desbloqueados();
        int antes = set.Count;
        foreach (var e in escenas) set.Add(e);
        if (set.Count == antes) return;
        // se guardan en el orden de la ruta
        var orden = new System.Collections.Generic.List<string>();
        foreach (var e in Ruta) if (set.Contains(e)) orden.Add(e);
        PlayerPrefs.SetString(ClaveDesbloqueo, string.Join(",", orden));
        PlayerPrefs.Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        cache = null;   // (sin recargar el dominio entre ejecuciones del editor)
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    static void AlCargar(Scene s, LoadSceneMode m)
    {
        if (m != LoadSceneMode.Single) return;
        if (s.name == "Final") { Desbloquear(Ruta); return; }   // terminó el juego: todos los pisos abiertos
        if (System.Array.IndexOf(Ruta, s.name) < 0) return;
        Desbloqueados();   // (si era una partida vieja, antes de anotar el nuevo lugar)
        PlayerPrefs.SetString(Clave, s.name);
        PlayerPrefs.Save();
        Desbloquear(new[] { s.name });
    }
}
