using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tabla de piezas que se copian de las escenas del Diorama/Puerto a prefabs para el exterior (Cala Sombría).
/// Cada fila dice en qué escena buscar, cómo se llama el objeto y cuál de los que se llaman igual tomar.
/// Las casas de todos los mapas se llaman "Casa": el tipo (pescador, piedra, pizarra, entramado) lo da la escena.
/// Abby → Exterior → Listar candidatos muestra qué hay en cada escena para ajustar índices.
/// </summary>
public class PiezasDiorama : ScriptableObject
{
    [System.Serializable]
    public class Pieza
    {
        [Tooltip("Escena de origen, sin ruta ni extensión (Assets/Scenes/<escena>.unity).")]
        public string escena;
        [Tooltip("Nombre exacto del objeto en la jerarquía.")]
        public string nombre;
        [Tooltip("Opcional: la ruta de sus padres tiene que contener este texto (ej. \"Pueblo\").")]
        public string dentroDe;
        [Tooltip("Cuál de los que coinciden, en orden de jerarquía (0 = el primero).")]
        public int indice;
        [Tooltip("Nombre del prefab en Assets/Prefabs/Exterior/.")]
        public string prefab;
        [Tooltip("Quita las luces (y su LightFlicker): el brillo queda en el material emisivo.")]
        public bool quitarLuces;
        [Tooltip("Quita el vaivén (FloatingBob) de botes y barcas.")]
        public bool quitarFlotar;
    }

    public List<Pieza> piezas = new List<Pieza>();

    static Pieza P(string escena, string nombre, int indice, string prefab, string dentroDe = "", bool quitarLuces = false, bool quitarFlotar = false) =>
        new Pieza { escena = escena, nombre = nombre, indice = indice, prefab = prefab, dentroDe = dentroDe, quitarLuces = quitarLuces, quitarFlotar = quitarFlotar };

    public static List<Pieza> PorDefecto() => new List<Pieza>
    {
        // casas: dos tamaños de cada tipo
        P("Puerto", "Casa", 0, "CasaPescador_A", "Pueblo"),
        P("Puerto", "Casa", 1, "CasaPescador_B", "Pueblo"),
        P("Cascadas", "Casa", 0, "CasaPiedra_A"),
        P("Cascadas", "Casa", 1, "CasaPiedra_B"),
        P("Acantilado", "Casa", 0, "CasaPizarra_A"),
        P("Acantilado", "Casa", 1, "CasaPizarra_B"),
        P("Diorama", "Casa", 0, "CasaEntramado_A"),
        P("Diorama", "Casa", 1, "CasaEntramado_B"),
        // puerto
        P("Puerto", "MuelleDePiedra", 0, "MuelleDePiedra"),
        P("Puerto", "Embarcadero", 0, "Embarcadero"),
        P("Puerto", "Barca", 0, "Barca", "Embarcadero"),
        P("Puerto", "Velero", 0, "Velero"),
        // fuego y luz (provisionales hasta las carteleras de la fase 5)
        P("Puerto", "Farol", 0, "Farol", "Pueblo"),
        P("Diorama", "Brasero", 0, "Brasero"),
        P("Cascadas", "Fogata", 0, "Fogata"),
        P("Cascadas", "Carpa", 0, "Carpa"),
        // vegetación (provisional hasta las carteleras de la fase 5)
        P("Puerto", "Pino", 0, "Pino"),
        P("Puerto", "Arbol", 0, "Arbol"),
    };
}
