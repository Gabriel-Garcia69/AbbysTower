using UnityEngine;

/// <summary>
/// La página de Configuración, igual en el menú principal y en la pausa: los tres volúmenes, la dificultad, pantalla
/// completa, restablecer y volver. Los que tienen &lt; &gt; se cambian con ← → (o clic a la izquierda / derecha).
/// </summary>
public static class MenuConfiguracion
{
    public const int General = 0, Musica = 1, Efectos = 2, Dificultad = 3, Pantalla = 4, Restablecer = 5, Volver = 6;

    public static string[] Opciones() => new[]
    {
        $"Volumen general   < {Por(Ajustes.General)} >",
        $"Volumen de música   < {Por(Ajustes.Musica)} >",
        $"Volumen de efectos   < {Por(Ajustes.Efectos)} >",
        $"Dificultad   < {Difficulty.Active.name} >",
        "Pantalla completa:  " + (Screen.fullScreen ? "Sí" : "No"),
        "Restablecer",
        "Volver",
    };

    static string Por(float v) => Mathf.RoundToInt(v * 100f) + " %";

    /// <summary>¿Se cambia con ← →?</summary>
    public static bool Ajustable(int i) => i <= Dificultad;

    public static void Ajustar(int i, int dir)
    {
        switch (i)
        {
            case General: Ajustes.General += 0.1f * dir; break;
            case Musica: Ajustes.Musica += 0.1f * dir; break;
            case Efectos: Ajustes.Efectos += 0.1f * dir; break;
            case Dificultad:
                int n = Difficulty.Presets.Length;
                Difficulty.Current = (Difficulty.Level)(((int)Difficulty.Current + dir + n) % n);
                break;
        }
        // se oye cómo queda: el de efectos con un golpe de espada
        Sonido.Tocar(i == Efectos ? Sonido.Efecto.Golpe : Sonido.Efecto.Clic, 0.75f);
    }

    /// <summary>Enter / clic sobre una opción. Devuelve true si fue "Volver".</summary>
    public static bool Elegir(int i)
    {
        if (Ajustable(i)) { Ajustar(i, 1); return false; }
        Sonido.Tocar(Sonido.Efecto.Clic);
        if (i == Pantalla) Screen.fullScreen = !Screen.fullScreen;
        else if (i == Restablecer) { Ajustes.Restablecer(); Difficulty.Current = Difficulty.Level.Normal; }
        return i == Volver;
    }

    /// <summary>Una línea de ayuda bajo el panel para la opción elegida (o null).</summary>
    public static string Ayuda(int i)
    {
        if (i == Dificultad) return Difficulty.Active.description;
        if (i == Restablecer) return "Vuelve los volúmenes y la dificultad a como venían.";
        return null;
    }

    /// <summary>Color de la dificultad (para pintar su opción).</summary>
    public static Color ColorDificultad => Difficulty.Active.color;
}
