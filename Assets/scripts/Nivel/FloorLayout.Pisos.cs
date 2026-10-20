using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Los siete pisos de los pecados (3 a 9): planos (salas a distintas alturas unidas por escaleras), nombres, jefes,
/// historia (notas y libros), escondites tras muros falsos y reliquias que solo aparecen desde cierto ángulo.
///
/// La historia: la torre apareció una noche sobre el abismo. Un Arquitecto la levantó para encerrar a los siete
/// pecados, cada uno con un carcelero que alguna vez fue un Ascendiente. Alguien que firma "A." dejó notas por todos
/// los pisos… y en la cima se descubre que A. es la propia Abby: cada vez que cae, la torre la devuelve al inicio sin
/// recuerdos, pero sus notas se quedan.
/// </summary>
public partial class FloorLayout
{
    [Header("Número de piso (historia, secretos)")]
    public int floorNumber = 1;

    // ------------------------------------------------------------------ planos

    public struct FloorPreset
    {
        public FloorTheme theme;
        public int number;
        public string title, subtitle, boss, next;
        public float hp, wall;
        public RoomSpec[] rooms;
        public CorridorSpec[] corridors;
    }

    static RoomSpec R(string name, RoomType type, float x, float z, float w, float d, int enemies = 0, float elev = 0f, RoomFeature f = RoomFeature.Ninguna, float height = 0f)
        => new RoomSpec { name = name, type = type, center = new Vector2(x, z), size = new Vector2(w, d), enemies = enemies, elevation = elev, feature = f, height = height };

    static CorridorSpec C(int a, int b, bool stairs = false, float width = 4f, float offset = 0f, bool bridge = false)
        => new CorridorSpec { from = a, to = b, stairs = stairs, width = width, offset = offset, bridge = bridge };

    static CorridorSpec B(int a, int b, float width = 3.5f) => new CorridorSpec { from = a, to = b, width = width, bridge = true };

    /// <summary>
    /// Plano de cada piso. Cada uno tiene su propia forma: ramas, bucles, varias puertas por sala, alturas distintas
    /// y una sala de estructura única (columnata, foso largo, laberinto, banquete, trampas, río de lava, corte).
    /// </summary>
    public static FloorPreset Preset(FloorTheme t)
    {
        switch (t)
        {
            case FloorTheme.Galerias:
                // Piso 2: vértigo. Mirador → foso larguísimo → puente al viento → plataforma central con tres salidas
                // (tienda y mirador roto por puentes, el ascenso al norte) → antesala y jefe en alto.
                return new FloorPreset
                {
                    theme = t, number = 2, title = "PISO 2", subtitle = "Las Galerías Colgantes", boss = "Vigía de las Galerías", next = "Nivel3", hp = 1.12f, wall = 4f,
                    rooms = new[]
                    {
                        R("0 Inicio", RoomType.Inicio, 0, 0, 14, 12, 0, 0, RoomFeature.Ninguna, 5),
                        R("1 Combate (mirador)", RoomType.Combate, 0, 28, 22, 16, 5, 0, RoomFeature.Ninguna, 5),
                        R("2 Prueba (foso largo)", RoomType.Prueba, 0, 68, 20, 44, 0, 0, RoomFeature.FosoLargo, 6),
                        R("3 Combate (plataforma del viento)", RoomType.Combate, 0, 116, 24, 18, 6, 0, RoomFeature.Ninguna, 5),
                        R("4 Descanso / Tienda", RoomType.Descanso, -32, 116, 14, 12, 0, 0, RoomFeature.Ninguna, 5),
                        R("5 Prueba (ascenso)", RoomType.Prueba, 0, 146, 18, 18, 0, 0, RoomFeature.Ascenso, 6),
                        R("6 Antesala", RoomType.Antesala, 0, 170, 12, 10, 0, 4.5f, RoomFeature.Ninguna, 5),
                        R("7 Jefe", RoomType.Jefe, 0, 198, 30, 26, 1, 4.5f, RoomFeature.Ninguna, 8),
                        R("8 Prueba (mirador roto)", RoomType.Prueba, 32, 116, 14, 12, 0, 1.5f),
                    },
                    corridors = new[] { C(0, 1), C(1, 2), B(2, 3), B(3, 4), C(3, 5), C(5, 6), C(6, 7), B(3, 8) },
                };
            case FloorTheme.Pereza:
                // Piso 3: se hunde. Sala de lectura → laberinto de libreros (con la tienda a un lado) → biblioteca de dos pisos
                // → escalera que baja al archivo hundido → antesala y jefe abajo.
                return new FloorPreset
                {
                    theme = t, number = 3, title = "PISO 3 · PEREZA", subtitle = "La Biblioteca Dormida", boss = "Acedia, la que no Despierta", next = "Nivel4", hp = 1.25f, wall = 4f,
                    rooms = new[]
                    {
                        R("0 Inicio", RoomType.Inicio, 0, 0, 14, 12, 0, 0, RoomFeature.Ninguna, 5),
                        R("1 Combate (sala de lectura)", RoomType.Combate, 0, 28, 24, 18, 5, 0, RoomFeature.Ninguna, 5),
                        R("2 Prueba (laberinto de libreros)", RoomType.Prueba, 0, 64, 28, 28, 0, 0, RoomFeature.Laberinto, 6),
                        R("3 Descanso / Tienda", RoomType.Descanso, 32, 64, 14, 12, 0, 0, RoomFeature.Ninguna, 5),
                        R("4 Prueba (biblioteca)", RoomType.Prueba, 0, 104, 26, 24, 0, 0, RoomFeature.Biblioteca, 8),
                        R("5 Combate (archivo hundido)", RoomType.Combate, 0, 140, 22, 18, 6, -3.5f, RoomFeature.Ninguna, 6),
                        R("6 Antesala", RoomType.Antesala, 0, 164, 12, 10, 0, -3.5f, RoomFeature.Ninguna, 5),
                        R("7 Jefe", RoomType.Jefe, 0, 190, 30, 26, 1, -3.5f, RoomFeature.Ninguna, 8),
                    },
                    corridors = new[] { C(0, 1), C(1, 2), C(2, 3), C(2, 4), C(4, 5, true), C(5, 6), C(6, 7) },
                };
            case FloorTheme.Gula:
                // Piso 4: un gran salón con cuatro puertas. Dos caminos a la antesala: por la Garganta (fauces) o por la Bodega.
                return new FloorPreset
                {
                    theme = t, number = 4, title = "PISO 4 · GULA", subtitle = "Los Salones del Banquete", boss = "Gargantúa, el Insaciable", next = "Nivel5", hp = 1.4f, wall = 4f,
                    rooms = new[]
                    {
                        R("0 Inicio", RoomType.Inicio, 0, 0, 14, 12, 0, 0, RoomFeature.Ninguna, 5),
                        R("1 Combate (cocinas)", RoomType.Combate, 0, 28, 22, 16, 5, 0, RoomFeature.Ninguna, 5),
                        R("2 Combate (gran banquete)", RoomType.Combate, 0, 66, 38, 28, 8, 0, RoomFeature.Banquete, 8),
                        R("3 Combate (despensa)", RoomType.Combate, -38, 66, 18, 16, 5, -2.5f, RoomFeature.Ninguna, 5),
                        R("4 Descanso / Tienda", RoomType.Descanso, -38, 92, 14, 12, 0, -2.5f, RoomFeature.Ninguna, 5),
                        R("5 Prueba (garganta)", RoomType.Prueba, 38, 66, 18, 22, 0, 0, RoomFeature.Garganta, 6),
                        R("6 Prueba (bodega)", RoomType.Prueba, 0, 96, 18, 14, 0, 0, RoomFeature.Ninguna, 5),
                        R("7 Antesala", RoomType.Antesala, 38, 96, 12, 10, 0, 0, RoomFeature.Ninguna, 5),
                        R("8 Jefe", RoomType.Jefe, 38, 124, 30, 26, 1, 0, RoomFeature.Ninguna, 8),
                    },
                    corridors = new[] { C(0, 1), C(1, 2), C(2, 3, true), C(3, 4), C(2, 5), C(5, 7), C(2, 6), C(6, 7), C(7, 8) },
                };
            case FloorTheme.Avaricia:
                // Piso 5: sube. Casa de cambio → pasillo de trampas (sube) → bóveda enorme (dos salidas) → contaduría con
                // escalinata → antesala y jefe en lo más alto. La cámara de lingotes y la tienda son un desvío.
                return new FloorPreset
                {
                    theme = t, number = 5, title = "PISO 5 · AVARICIA", subtitle = "La Bóveda del Acaparador", boss = "Mammón, el Acaparador", next = "Nivel6", hp = 1.55f, wall = 4f,
                    rooms = new[]
                    {
                        R("0 Inicio", RoomType.Inicio, 0, 0, 14, 12, 0, 0, RoomFeature.Ninguna, 5),
                        R("1 Combate (casa de cambio)", RoomType.Combate, 26, 0, 20, 14, 4, 0, RoomFeature.Ninguna, 5),
                        R("2 Prueba (pasillo de las trampas)", RoomType.Prueba, 26, 34, 12, 36, 0, 3f, RoomFeature.Pinchos, 5),
                        R("3 Prueba (bóveda)", RoomType.Prueba, 26, 82, 30, 26, 0, 3f, RoomFeature.Boveda, 9),
                        R("4 Combate (cámara de lingotes)", RoomType.Combate, -10, 82, 20, 16, 6, 3f, RoomFeature.Ninguna, 5),
                        R("5 Descanso / Tienda", RoomType.Descanso, -10, 108, 14, 12, 0, 3f, RoomFeature.Ninguna, 5),
                        R("6 Prueba (contaduría)", RoomType.Prueba, 26, 120, 18, 18, 0, 3f, RoomFeature.Escalinata, 9),
                        R("7 Antesala", RoomType.Antesala, 26, 142, 12, 10, 0, 7.5f, RoomFeature.Ninguna, 5),
                        R("8 Jefe", RoomType.Jefe, 26, 168, 32, 26, 1, 7.5f, RoomFeature.Ninguna, 9),
                    },
                    corridors = new[] { C(0, 1), C(1, 2, true), C(2, 3), C(3, 4), C(4, 5), C(3, 6), C(6, 7), C(7, 8) },
                };
            case FloorTheme.Envidia:
                // Piso 6: galería ancha → laberinto de espejos (La Otra te persigue) → baja a la sala de las copias, que reparte
                // a la galería de espejos (desvío con escondite), la tienda y la escalinata.
                return new FloorPreset
                {
                    theme = t, number = 6, title = "PISO 6 · ENVIDIA", subtitle = "La Galería de los Espejos", boss = "El Reflejo Envidioso", next = "Nivel7", hp = 1.7f, wall = 4f,
                    rooms = new[]
                    {
                        R("0 Inicio", RoomType.Inicio, 0, 0, 14, 12, 0, 0, RoomFeature.Ninguna, 5),
                        R("1 Combate (galería de retratos)", RoomType.Combate, 0, 28, 30, 14, 5, 0, RoomFeature.Ninguna, 6),
                        R("2 Prueba (laberinto de espejos)", RoomType.Prueba, 0, 62, 28, 28, 0, 0, RoomFeature.Laberinto, 5),
                        R("3 Combate (sala de las copias)", RoomType.Combate, 0, 102, 22, 18, 6, -3f, RoomFeature.Ninguna, 6),
                        R("4 Descanso / Tienda", RoomType.Descanso, 30, 102, 14, 12, 0, -3f, RoomFeature.Ninguna, 5),
                        R("5 Combate (galería de espejos)", RoomType.Combate, -32, 102, 22, 16, 5, -3f, RoomFeature.Espejos, 6),
                        R("6 Prueba (escalinata)", RoomType.Prueba, 0, 132, 18, 18, 0, -3f, RoomFeature.Escalinata, 9),
                        R("7 Antesala", RoomType.Antesala, 0, 154, 12, 10, 0, 1.5f, RoomFeature.Ninguna, 5),
                        R("8 Jefe", RoomType.Jefe, 0, 180, 30, 26, 1, 1.5f, RoomFeature.Ninguna, 8),
                    },
                    corridors = new[] { C(0, 1), C(1, 2), C(2, 3, true), C(3, 4), C(3, 5), C(3, 6), C(6, 7), C(7, 8) },
                };
            case FloorTheme.Lujuria:
                // Piso 7: invernadero → laberinto de setos (sube) → jardín con fuente → tocador, que da a la tienda y a la escalinata.
                return new FloorPreset
                {
                    theme = t, number = 7, title = "PISO 7 · LUJURIA", subtitle = "El Jardín Carmesí", boss = "Asmodea, Señora del Anhelo", next = "Nivel8", hp = 1.85f, wall = 4f,
                    rooms = new[]
                    {
                        R("0 Inicio", RoomType.Inicio, 0, 0, 14, 12, 0, 0, RoomFeature.Ninguna, 5),
                        R("1 Combate (invernadero)", RoomType.Combate, 0, 28, 22, 16, 5, 0, RoomFeature.Ninguna, 6),
                        R("2 Prueba (laberinto de setos)", RoomType.Prueba, 0, 66, 28, 28, 0, 2.5f, RoomFeature.Laberinto, 5),
                        R("3 Prueba (jardín)", RoomType.Prueba, 38, 66, 26, 22, 0, 2.5f, RoomFeature.Jardin, 7),
                        R("4 Combate (tocador)", RoomType.Combate, 38, 98, 20, 16, 6, 2.5f, RoomFeature.Ninguna, 6),
                        R("5 Descanso / Tienda", RoomType.Descanso, 6, 98, 14, 12, 0, 2.5f, RoomFeature.Ninguna, 5),
                        R("6 Prueba (escalinata)", RoomType.Prueba, 38, 126, 18, 18, 0, 2.5f, RoomFeature.Escalinata, 9),
                        R("7 Antesala", RoomType.Antesala, 38, 148, 12, 10, 0, 7f, RoomFeature.Ninguna, 5),
                        R("8 Jefe", RoomType.Jefe, 38, 174, 30, 26, 1, 7f, RoomFeature.Ninguna, 9),
                    },
                    corridors = new[] { C(0, 1), C(1, 2, true), C(2, 3), C(3, 4), C(4, 5), C(4, 6), C(6, 7), C(7, 8) },
                };
            case FloorTheme.Ira:
                // Piso 8: fundición → baja al río de lava (tres salidas: forja al oeste, puente sobre la lava al norte) →
                // armería → escalinata → antesala y jefe.
                return new FloorPreset
                {
                    theme = t, number = 8, title = "PISO 8 · IRA", subtitle = "La Forja Encadenada", boss = "Furia, la Llama Encadenada", next = "Nivel9", hp = 2f, wall = 4f,
                    rooms = new[]
                    {
                        R("0 Inicio", RoomType.Inicio, 0, 0, 14, 12, 0, 0, RoomFeature.Ninguna, 5),
                        R("1 Combate (fundición)", RoomType.Combate, 0, 28, 22, 16, 5, 0, RoomFeature.Ninguna, 6),
                        R("2 Prueba (río de lava)", RoomType.Prueba, 0, 66, 24, 34, 0, -3f, RoomFeature.RioLava, 7),
                        R("3 Combate (forja)", RoomType.Combate, -36, 66, 24, 20, 7, -3f, RoomFeature.Forja, 7),
                        R("4 Descanso / Tienda", RoomType.Descanso, -36, 94, 14, 12, 0, -3f, RoomFeature.Ninguna, 5),
                        R("5 Combate (armería)", RoomType.Combate, 0, 108, 22, 16, 6, -3f, RoomFeature.Ninguna, 6),
                        R("6 Prueba (escalinata)", RoomType.Prueba, 0, 136, 18, 18, 0, -3f, RoomFeature.Escalinata, 9),
                        R("7 Antesala", RoomType.Antesala, 0, 158, 12, 10, 0, 1.5f, RoomFeature.Ninguna, 5),
                        R("8 Jefe", RoomType.Jefe, 0, 184, 30, 26, 1, 1.5f, RoomFeature.Ninguna, 9),
                    },
                    corridors = new[] { C(0, 1), C(1, 2, true), C(2, 3), C(3, 4), B(2, 5, 4f), C(5, 6), C(6, 7), C(7, 8) },
                };
            case FloorTheme.Soberbia:
                // Piso 9: la corte. Del Salón del Juicio salen dos rutas (trofeos + tienda al este, columnata + capilla al oeste)
                // que se juntan en la gran escalinata; arriba, la antesala y el trono.
                return new FloorPreset
                {
                    theme = t, number = 9, title = "PISO 9 · SOBERBIA", subtitle = "La Corte de Mármol", boss = "El Coronado", next = "", hp = 2.2f, wall = 4.5f,
                    rooms = new[]
                    {
                        R("0 Inicio", RoomType.Inicio, 0, 0, 16, 14, 0, 0, RoomFeature.Ninguna, 7),
                        R("1 Combate (vestíbulo de estatuas)", RoomType.Combate, 0, 30, 26, 18, 6, 0, RoomFeature.Ninguna, 7),
                        R("2 Prueba (salón del juicio)", RoomType.Prueba, 0, 68, 30, 28, 0, 3f, RoomFeature.Salon, 10),
                        R("3 Combate (salón de trofeos)", RoomType.Combate, 38, 68, 20, 16, 7, 3f, RoomFeature.Ninguna, 7),
                        R("4 Prueba (galería de columnas)", RoomType.Prueba, -38, 68, 20, 22, 0, 3f, RoomFeature.Columnata, 9),
                        R("5 Descanso / Tienda", RoomType.Descanso, 38, 94, 14, 12, 0, 3f, RoomFeature.Ninguna, 6),
                        R("6 Prueba (capilla)", RoomType.Prueba, -38, 96, 14, 12, 0, 3f, RoomFeature.Ninguna, 7),
                        R("7 Prueba (gran escalinata)", RoomType.Prueba, 0, 104, 24, 22, 0, 3f, RoomFeature.Escalinata, 10),
                        R("8 Antesala", RoomType.Antesala, 0, 128, 12, 10, 0, 7.5f, RoomFeature.Ninguna, 6),
                        R("9 Jefe", RoomType.Jefe, 0, 158, 36, 30, 1, 7.5f, RoomFeature.Ninguna, 11),
                    },
                    corridors = new[] { C(0, 1), C(1, 2, true), C(2, 3), C(2, 4), C(3, 5), C(4, 6), C(5, 7, false, 4f, 2f), C(6, 7), C(7, 8), C(8, 9) },
                };
            default:
                // Piso 1: el patio de armas reparte a dos ramas (cuartel al oeste, columnata al este) que suben por escaleras
                // y se juntan en la antesala; arriba, el jefe en una sala enorme.
                return new FloorPreset
                {
                    theme = FloorTheme.Base, number = 1, title = "PISO 1", subtitle = "La Base de la Torre", boss = "Guardián de la Base", next = "Nivel2", hp = 1f, wall = 4f,
                    rooms = new[]
                    {
                        R("0 Inicio", RoomType.Inicio, 0, 0, 16, 14, 0, 0, RoomFeature.Ninguna, 6),
                        R("1 Combate (patio de armas)", RoomType.Combate, 0, 30, 26, 20, 4, 0, RoomFeature.Ninguna, 5),
                        R("2 Combate (cuartel oeste)", RoomType.Combate, -32, 30, 18, 16, 4, 0, RoomFeature.Ninguna, 5),
                        R("3 Prueba (galería de columnas)", RoomType.Prueba, 32, 30, 20, 22, 0, 0, RoomFeature.Columnata, 8),
                        R("4 Descanso / Tienda", RoomType.Descanso, -32, 56, 16, 12, 0, 2.5f, RoomFeature.Ninguna, 5),
                        R("5 Combate (sala de la guardia)", RoomType.Combate, 32, 58, 22, 16, 6, 2.5f, RoomFeature.Ninguna, 5),
                        R("6 Antesala", RoomType.Antesala, 0, 58, 14, 12, 0, 2.5f, RoomFeature.Ninguna, 6),
                        R("7 Jefe", RoomType.Jefe, 0, 88, 34, 28, 1, 2.5f, RoomFeature.Ninguna, 9),
                    },
                    corridors = new[] { C(0, 1), C(1, 2), C(1, 3), C(2, 4, true), C(3, 5, true), C(4, 6), C(5, 6), C(6, 7) },
                };
        }
    }

    /// <summary>Carga el plano, nombres y jefe del tema elegido (borra las salas actuales).</summary>
    [ContextMenu("Cargar plano del tema")]
    public void LoadPreset()
    {
        var p = Preset(theme);
        rooms = p.rooms; corridors = p.corridors;
        floorNumber = p.number; floorTitle = p.title; floorSubtitle = p.subtitle;
        bossName = p.boss; nextScene = p.next; enemyHealthMultiplier = p.hp;
        if (p.wall > 0f) wallHeight = p.wall;
#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
#endif
    }

    // ------------------------------------------------------------------ historia

    struct Lore { public int room; public Vector3 pos; public float yaw; public LoreNote.Kind kind; public string title, text, sign; }

    static Lore L(int room, float x, float z, LoreNote.Kind kind, string title, string text, string sign = "", float yaw = 0f, float y = 0f)
        => new Lore { room = room, pos = new Vector3(x, y, z), kind = kind, title = title, text = text, sign = sign, yaw = yaw };

    List<Lore> LoreFor(int floor)
    {
        var list = new List<Lore>();
        switch (floor)
        {
            case 1:
                list.Add(L(0, 3.6f, 2.6f, LoreNote.Kind.Inscripcion, "Bienvenida, Ascendiente",
                    "Siete pecados duermen en esta torre, uno encima de otro, como libros en un estante.\n\nQuien llegue a la cima podrá pedir lo que quiera.\n\nQuien caiga, volverá a empezar."));
                list.Add(L(4, -5f, 2.5f, LoreNote.Kind.Diario, "Día 1",
                    "El mercader dice que no es la primera vez que me ve. Yo nunca había estado aquí.\n\nLe compré una poción por si acaso. Me la cobró a mitad de precio y me guiñó un ojo. Qué raro.", "— A."));
                break;
            case 2:
                list.Add(L(0, 3.6f, 2.6f, LoreNote.Kind.Diario, "Las galerías",
                    "Las galerías cuelgan del vacío. Alguien construyó esto para que solo subieran los que saben saltar… o los que no le tienen miedo a caer.\n\nYo sí le tengo miedo. Subo igual.", "— A."));
                list.Add(L(6, 3f, 1.5f, LoreNote.Kind.Libro, "Registro del Arquitecto",
                    "Segunda planta terminada.\n\nLas siete plantas de arriba serán cárceles: Pereza, Gula, Avaricia, Envidia, Lujuria, Ira y Soberbia. Cada una tendrá su carcelero.\n\nNinguno sabrá que también es prisionero."));
                break;
            case 3:
                list.Add(L(1, -6.5f, 5f, LoreNote.Kind.Libro, "Tratado del Buen Descanso",
                    "Capítulo I: Mañana.\nCapítulo II: Mañana.\nCapítulo III: Quizá pasado mañana.\n\n(El resto de las páginas están en blanco. En la última alguien escribió con letra muy cansada: \"ya casi\".)"));
                list.Add(L(4, -11.3f, 6f, LoreNote.Kind.Diario, "Polvo en las pestañas",
                    "Me quedé dormida entre los libros. Soñé que ya había subido toda la torre y que arriba no había nada.\n\nDesperté con polvo en las pestañas. ¿Cuánto tiempo pasó? Las velas siguen igual de altas. Aquí nada se consume, nada avanza.", "— A.", 0f, BibliotecaMezzanine));
                list.Add(L(6, 0f, 2.6f, LoreNote.Kind.Inscripcion, "Epitafio",
                    "Aquí yace Acedia, que iba a cambiar el mundo en cuanto terminara de descansar."));
                break;
            case 4:
                list.Add(L(1, 6.5f, 5.2f, LoreNote.Kind.Nota, "Menú del día",
                    "Entrada: todo.\nPlato fuerte: lo demás.\nPostre: lo que quede de ti.\n\n(Abajo, con otra letra: \"no comas en la mesa grande si estás peleando, te vas a poner lenta\".)"));
                list.Add(L(2, -14f, 11f, LoreNote.Kind.Diario, "Un hueco",
                    "Comí hasta que dolió. La comida no sabía a nada, pero no podía parar.\n\nCreo que entiendo al Guardián: no tiene hambre. Tiene un hueco. Y los huecos no se llenan comiendo.", "— A."));
                list.Add(L(3, -5.5f, 4.8f, LoreNote.Kind.Libro, "Registro del Arquitecto",
                    "Gargantúa fue el primer cocinero de la torre. Le pedí que alimentara a los Ascendientes para que tuvieran fuerzas.\n\nNunca le dije cuándo parar."));
                break;
            case 5:
                list.Add(L(1, -5.5f, 4.2f, LoreNote.Kind.Libro, "Libro de cuentas",
                    "Fragmentos guardados: 1 000 000.\nFragmentos gastados: 0.\nFragmentos necesarios para ser feliz: uno más.\n\n(Las cuentas siguen por cien páginas. Todas terminan igual.)"));
                list.Add(L(3, 10.5f, -8.5f, LoreNote.Kind.Diario, "Cofres con dientes",
                    "Abrí un cofre y tenía dientes. Abrí otro y tenía dientes. El tercero tenía oro.\n\nFui por un cuarto. Por eso tengo esta cicatriz en la mano. Los que respiran, muerden.", "— A."));
                list.Add(L(7, 0f, 2.6f, LoreNote.Kind.Inscripcion, "Grabado en oro",
                    "Mammón contó todas las monedas de la torre.\n\nSe le olvidó contar los años."));
                break;
            case 6:
                list.Add(L(1, -6.5f, 5f, LoreNote.Kind.Nota, "Los retratos",
                    "Todos los retratos de esta galería son de la misma persona.\n\nTodos tienen una \"A.\" pintada en la esquina."));
                list.Add(L(5, 7.5f, 5f, LoreNote.Kind.Diario, "La que llega tarde",
                    "En los espejos de este piso hay otra yo que se mueve tarde. Me sigue a todas partes.\n\nQuiere lo que tengo, aunque lo único que tengo es esta subida. Si me detengo, me alcanza. Así que no me detengo.", "— A."));
                list.Add(L(3, 6.5f, 4.8f, LoreNote.Kind.Libro, "Registro del Arquitecto",
                    "El Reflejo nació de una Ascendiente que pasó demasiado tiempo mirando cómo subían los demás.\n\nNunca subió ella. Ahora solo puede seguir."));
                break;
            case 7:
                list.Add(L(1, 6.5f, 5f, LoreNote.Kind.Nota, "Carta",
                    "Te esperé en el jardín hasta que las rosas me crecieron por dentro.\n\nSi algún día vuelves, no toques las espinas. Todo lo que es bonito aquí, jala."));
                list.Add(L(3, -10.5f, 8f, LoreNote.Kind.Diario, "Lo que jala",
                    "Hay algo en este jardín que me jala, como cuando quieres algo tanto que ya no importa si duele.\n\nMe amarré la espada a la mano para no soltarla. Si me jala, me impulso hacia el otro lado.", "— A."));
                list.Add(L(7, 0f, 2.6f, LoreNote.Kind.Inscripcion, "Sobre la puerta",
                    "Asmodea no quería ser amada.\nQuería ser necesitada.\n\nAquí sigue, esperando a que alguien no pueda irse."));
                break;
            case 8:
                list.Add(L(1, -6.5f, 5f, LoreNote.Kind.Nota, "Nota quemada",
                    "…y entonces rompí la puerta, y la pared, y el piso, y cuando ya no quedaba nada que romper, me quedé yo."));
                list.Add(L(3, 9f, 7f, LoreNote.Kind.Diario, "El fuego contesta",
                    "Grité tan fuerte que el fuego me contestó.\n\nNo sé con quién estoy enojada. Creo que conmigo, por tardar tanto. Por caer tantas veces. Por no acordarme de por qué subo.", "— A."));
                list.Add(L(5, 6.5f, 4.8f, LoreNote.Kind.Libro, "Registro del Arquitecto",
                    "Encadené a Furia con eslabones forjados de sus propios golpes.\n\nCada vez que golpea, la cadena se acorta."));
                break;
            case 9:
                list.Add(L(6, 0f, 3.5f, LoreNote.Kind.Inscripcion, "En el altar de la capilla",
                    "Aquí rezaban los que dudaban antes de la última escalera.\n\nNadie dudó nunca. Por eso el altar está limpio."));
                list.Add(L(1, 0f, -4.5f, LoreNote.Kind.Inscripcion, "A los pies de las estatuas",
                    "Arrodíllate.\n\nTodos los que llegaron hasta aquí se arrodillaron. Los que no, ya no están."));
                list.Add(L(2, -11f, -9f, LoreNote.Kind.Libro, "Registro del Arquitecto (última página)",
                    "Construí la torre para encerrar los siete pecados.\n\nMe faltó el octavo: creer que yo podía hacerlo.\n\nAhora el Coronado lleva mi corona y yo soy la torre. Cada piedra. Cada escalón. Cada vez que alguien cae, lo siento."));
                list.Add(L(8, 0f, 2.6f, LoreNote.Kind.Diario, "La última nota",
                    "Ya lo entendí. Las notas. La letra. La cicatriz en la mano. El mercader que me guiña el ojo.\n\nCada vez que caigo, la torre me devuelve al principio y se me olvida todo. Pero las notas se quedan.\n\nSi estás leyendo esto, Abis: yo soy tú.\n\nEsta vez, llega arriba.", "— A."));
                break;
        }
        return list;
    }

    // ------------------------------------------------------------------ secretos por piso

    /// <summary>Notas, escondites y reliquias. Todo va en Floor_Secrets (fuera de las salas) para que cada escondite se revele solo.</summary>
    void PlaceThemeExtras(Transform[] roomObjects, List<Gap>[] gaps)
    {
        var secrets = NewChild(transform, Prefix + "Secrets");
        drng = new System.Random(decorSeed * 31 + floorNumber);

        foreach (var l in LoreFor(floorNumber))
        {
            if (l.room < 0 || l.room >= roomObjects.Length) continue;
            var r = rooms[l.room];
            var pos = new Vector3(Mathf.Clamp(l.pos.x, -r.size.x / 2f + 1f, r.size.x / 2f - 1f), l.pos.y, Mathf.Clamp(l.pos.z, -r.size.y / 2f + 1f, r.size.y / 2f - 1f));
            Readable(roomObjects[l.room], pos, l, $"p{floorNumber}_{l.room}_{l.title.GetHashCode() & 0xffff:x}");
        }

        foreach (var n in niches) BuildNiche(secrets, roomObjects[n.room], rooms[n.room], n);

        if (floorNumber >= 3) PlaceRelic(secrets, roomObjects);
    }

    // ------------------------------------------------------------------ piezas de historia

    void Readable(Transform room, Vector3 pos, Lore l, string id)
    {
        var t = NewChild(room, "Lectura_" + l.kind);
        t.localPosition = pos;
        t.localRotation = Quaternion.Euler(0f, l.yaw, 0f);
        GameObject sparkleRoot;
        switch (l.kind)
        {
            case LoreNote.Kind.Inscripcion:
            {
                // estela de piedra con letras que brillan tenue
                Prim(t, PrimitiveType.Cube, "Estela", new Vector3(0f, 0.75f, 0f), new Vector3(1.1f, 1.5f, 0.3f), StoneMaterial(), true);
                Prim(t, PrimitiveType.Cube, "Remate", new Vector3(0f, 1.55f, 0f), new Vector3(1.25f, 0.14f, 0.4f), StoneMaterial(), false);
                for (int i = 0; i < 4; i++)
                {
                    var line = Prim(t, PrimitiveType.Cube, "Letras", new Vector3(0f, 1.25f - i * 0.2f, -0.16f), new Vector3(0.7f - (i % 2) * 0.2f, 0.04f, 0.01f), GlowMaterial("Glow_" + ColorUtility.ToHtmlStringRGB(palette.accent), palette.accent), false);
                    line.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                sparkleRoot = NewChild(t, "Brillo").gameObject;
                sparkleRoot.transform.localPosition = new Vector3(0f, 1.1f, -0.3f);
                break;
            }
            case LoreNote.Kind.Libro:
            {
                // atril de madera con libro abierto y una vela
                var wood = LitMaterial("MaderaOscura", new Color(0.26f, 0.16f, 0.09f));
                Prim(t, PrimitiveType.Cube, "Pie", new Vector3(0f, 0.5f, 0f), new Vector3(0.12f, 1f, 0.12f), wood, true);
                Prim(t, PrimitiveType.Cube, "BasePie", new Vector3(0f, 0.03f, 0f), new Vector3(0.6f, 0.06f, 0.45f), wood, false);
                var desk = Prim(t, PrimitiveType.Cube, "Atril", new Vector3(0f, 1.02f, 0f), new Vector3(0.7f, 0.05f, 0.5f), wood, false);
                desk.transform.localRotation = Quaternion.Euler(-25f, 0f, 0f);
                foreach (float x in new[] { -0.16f, 0.16f })
                {
                    var page = Prim(t, PrimitiveType.Cube, "Pagina", new Vector3(x, 1.06f, 0f), new Vector3(0.3f, 0.02f, 0.4f), LitMaterial("Papel", new Color(0.9f, 0.84f, 0.7f)), false);
                    page.transform.localRotation = Quaternion.Euler(-25f, 0f, x > 0 ? -5f : 5f);
                }
                Candles(t, new Vector3(0.45f, 0f, 0.25f), 2, theme == FloorTheme.Base ? new Color(1f, 0.7f, 0.4f) : palette.torch, 0.08f);
                sparkleRoot = NewChild(t, "Brillo").gameObject;
                sparkleRoot.transform.localPosition = new Vector3(0f, 1.15f, 0f);
                break;
            }
            default:
            {
                // nota o página de diario sobre una caja / mesita, con piedra encima para que no vuele
                var wood = LitMaterial("Madera", new Color(0.42f, 0.27f, 0.15f));
                Prim(t, PrimitiveType.Cube, "Mesita", new Vector3(0f, 0.35f, 0f), new Vector3(0.7f, 0.7f, 0.55f), wood, true);
                var paper = Prim(t, PrimitiveType.Cube, "Papel", new Vector3(0f, 0.71f, 0f), new Vector3(0.36f, 0.01f, 0.46f), LitMaterial("Papel", new Color(0.9f, 0.84f, 0.7f)), false);
                paper.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
                Prim(t, PrimitiveType.Sphere, "Piedrita", new Vector3(0.1f, 0.74f, 0.12f), new Vector3(0.09f, 0.05f, 0.08f), StoneMaterial(), false);
                if (l.kind == LoreNote.Kind.Diario)   // el diario de A.: con su marca, una cinta morada
                    Prim(t, PrimitiveType.Cube, "Cinta", new Vector3(-0.12f, 0.715f, 0f), new Vector3(0.04f, 0.012f, 0.48f), LitMaterial("Tela_Abby", new Color(0.42f, 0.22f, 0.55f)), false).transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
                sparkleRoot = NewChild(t, "Brillo").gameObject;
                sparkleRoot.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                break;
            }
        }
        Motes(sparkleRoot.transform, Vector3.zero, new Vector3(0.4f, 0.2f, 0.4f), new Color(1f, 0.9f, 0.6f), 5f, 0.3f, 1.6f);
        var note = t.gameObject.AddComponent<LoreNote>();
        note.id = id; note.kind = l.kind; note.title = l.title; note.text = l.text; note.signature = l.sign;
        note.sparkle = sparkleRoot;
    }
}
