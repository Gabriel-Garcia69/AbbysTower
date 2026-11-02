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
    public static string Nombre
    {
        get
        {
            switch (Escena)
            {
                case "Exterior": return "Cala Sombría";
                case "Diorama": return "Piso 1";
                case "CuartosA": return "Piso 1 · Biblioteca";
                case "Puerto": return "Piso 2";
                case "Cascadas": return "Piso 3";
                case "Acantilado": return "Piso 4";
                case "Cima": return "Piso 5";
                default: return Escena;
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
