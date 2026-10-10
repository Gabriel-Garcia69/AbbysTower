using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lleva la cuenta de lo que Abby ha descubierto: notas leídas, escondites y reliquias (easter eggs).
/// Se guarda en PlayerPrefs (sobrevive a cerrar el juego). Cada cosa tiene un id único por piso.
/// </summary>
public static class SecretTracker
{
    public const int TotalRelics = 7;
    const string Key = "abby.secrets";

    static HashSet<string> found;
    public static event Action Changed;

    static HashSet<string> Found
    {
        get
        {
            if (found == null)
            {
                found = new HashSet<string>();
                string raw = PlayerPrefs.GetString(Key, "");
                foreach (var s in raw.Split('|')) if (!string.IsNullOrEmpty(s)) found.Add(s);
            }
            return found;
        }
    }

    public static bool Has(string id) => Found.Contains(id);

    /// <summary>Marca algo como descubierto. Devuelve true si es la primera vez.</summary>
    public static bool Mark(string id)
    {
        if (string.IsNullOrEmpty(id) || !Found.Add(id)) return false;
        PlayerPrefs.SetString(Key, string.Join("|", Found));
        PlayerPrefs.Save();
        Changed?.Invoke();
        return true;
    }

    public static int Count(string prefix)
    {
        int n = 0;
        foreach (var s in Found) if (s.StartsWith(prefix)) n++;
        return n;
    }

    public static int Notes => Count("nota:");
    public static int Hideouts => Count("escondite:");
    public static int Relics => Count("reliquia:");
}
