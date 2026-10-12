using UnityEngine;

/// <summary>Contenido de juego del Piso 2 (Puerto): pelea en el mercado, cabo del faro, mercader, santuario, jefe en la punta del muelle.</summary>
public partial class DioramaBuilder
{
    void BuildPortLevel()
    {
        var g = Group("Nivel", root);
        // escaleras al cabo y al faro
        Stairs(g, "EscaleraCabo", new Vector3(33f, 0f, -6.5f), new Vector3(33f, 2.5f, 0.6f), 6f);
        Stairs(g, "EscaleraFaro", new Vector3(LighthouseX, 2.5f, 11.5f), new Vector3(LighthouseX, 4.5f, 18.6f), 6f);
        foreach (var p in new[] { new Vector3(29.4f, 0f, -7.2f), new Vector3(33.6f, 0f, -7.2f), new Vector3(42f, 2.5f, 10.8f), new Vector3(46f, 2.5f, 10.8f) })
            PortLamp(g, p);
        Arena(g, "Emboscada en el mercado", new Vector3(-12f, 0f, -12f), new Vector2(16f, 12f), 5);
        Arena(g, "El cabo del faro", new Vector3(LighthouseX - 1f, 4.5f, LighthouseZ - 5.5f), new Vector2(11f, 6f), 4);
        var boss = Arena(g, "Jefe", new Vector3(PierX, 0.12f, PierEnd - 6f), new Vector2(14f, 10f), 1, true);
        Exit(g, new Vector3(PierX, 0.12f, PierEnd - 3f), boss);
        // el puesto del mercado es el del mercader
        MerchantPost(g, new Vector3(18f, 0f, -9f), 0f);
        HealShrine(g, new Vector3(4f, 0f, -24f));
        Chest(g, new Vector3(LighthouseX + 4f, 4.5f, LighthouseZ + 2f), 28, 90f);
        Chest(g, new Vector3(-24.4f, 0.1f, 19.6f), 20, 0f);
        Note(g, new Vector3(-4f, 0f, -24f), 180f, LoreNote.Kind.Diario, "dio2_diario", "El mar de adentro",
             "La torre también tiene mar. Los barcos están amarrados y nadie los suelta.\nUn pescador me contó que el capitán perdió su barco en este puerto y se quedó a esperarlo en la punta del muelle.\nSi vas para allá, no le des la espalda: ataca en abanico.", "— A.");
        Note(g, new Vector3(LighthouseX - 3f, 4.5f, LighthouseZ + 3f), 90f, LoreNote.Kind.Libro, "dio2_arquitecto", "Registro del Arquitecto",
             "Piso dos: el puerto.\nLo hice para los que quieren irse. Pero el mar de la torre no lleva a ningún lado, así que los barcos se quedan amarrados.\nEs mejor así: desde aquí nadie se pierde.");
    }
}
