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
        Note(g, new Vector3(-3.2f, 0f, -24.5f), 180f, LoreNote.Kind.Inscripcion, "dio1_bienvenida", "Placa a la entrada del piso 1",
             "PISO 1. Asegurado por el ejército en 2031.\nLas criaturas de este piso son las más débiles de la torre. Cuanto más abajo, más fuertes.\nTodo mineral recuperado se entrega en el puesto de comercio.");
        Note(g, new Vector3(-27f, 0f, 9f), 0f, LoreNote.Kind.Diario, "dio1_diario", "Diario de la expedición — día 1",
             "Abby: entramos hoy. El piso 1 parece un pueblo de verdad, con río y casas, pero no hay nadie.\nEl capitán dice que la torre copia lugares, que nadie sabe de dónde.\nVoy a ir dejando estas hojas. Si un día bajas, no lo hagas. Pero si bajas, que te sirvan.", "— " + Historia.Hermano);
        Exit(g, new Vector3(0f, TerraceY, Terrace.yMax - 4.5f), townBoss);
    }
}
