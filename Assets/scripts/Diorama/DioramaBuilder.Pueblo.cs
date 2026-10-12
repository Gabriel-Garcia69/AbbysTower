using UnityEngine;

/// <summary>Contenido de juego del Piso 1 (Pueblo del Risco): emboscada frente a las ruinas, mercader, santuario, notas, jefe y salida.</summary>
public partial class DioramaBuilder
{
    RoomEncounter townBoss;

    void BuildTownLevel()
    {
        var g = Group("Nivel", root);
        // emboscada en la franja entre el cañón y las ruinas
        Arena(g, "Emboscada frente a las ruinas", new Vector3(-4f, 0f, 16f), new Vector2(30f, 8f), 4);
        HealShrine(g, new Vector3(5.5f, 0f, -25.5f));
        Chest(g, new Vector3(-30.5f, 0f, -3.5f), 18, 90f);
        Chest(g, new Vector3(LookoutX - 1f, 0.1f, CliffFenceZ + 1.6f), 25, 180f);
        Note(g, new Vector3(-3.2f, 0f, -24.5f), 180f, LoreNote.Kind.Inscripcion, "dio1_bienvenida", "Bienvenida, viajera",
             "Este pueblo existió de verdad, al pie de la torre. Cuando se quedó vacío, el Arquitecto lo guardó aquí adentro para que nadie lo olvidara.\nCada piso de la torre es un recuerdo como este.\nPara subir, vence al guardián de cada piso: al caer, su portal se enciende y te lleva al siguiente.");
        Note(g, new Vector3(-27f, 0f, 9f), 0f, LoreNote.Kind.Diario, "dio1_diario", "Día 1",
             "Las chimeneas echan humo, pero nadie abre la puerta.\nEn mi bolsa encontré esta libreta y ya tenía páginas escritas con mi letra. No recuerdo haberlas escrito.\nVoy a ir dejando hojas por el camino, por si me toca empezar otra vez.", "— A.");
        Exit(g, new Vector3(0f, TerraceY, Terrace.yMax - 4.5f), townBoss);
    }
}
