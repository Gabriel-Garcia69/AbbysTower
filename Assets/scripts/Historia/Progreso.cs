using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Guardado de la partida: al entrar a un lugar de la ruta (de Cala Sombría a la cima) se anota en PlayerPrefs, y el menú
/// ofrece "Continuar" desde ahí. Al terminar el juego (Dante) se borra.
/// </summary>
public static class Progreso
{
    const string Clave = "progreso_escena";
    static readonly string[] Ruta = { "Exterior", "Diorama", "CuartosA", "Puerto", "Cascadas", "Acantilado", "Cima" };

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        SceneManager.sceneLoaded -= AlCargar;
        SceneManager.sceneLoaded += AlCargar;
    }

    static void AlCargar(Scene s, LoadSceneMode m)
    {
        if (m != LoadSceneMode.Single || System.Array.IndexOf(Ruta, s.name) < 0) return;
        PlayerPrefs.SetString(Clave, s.name);
        PlayerPrefs.Save();
    }
}
