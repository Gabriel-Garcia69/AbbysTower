/// <summary>
/// La historia del juego tal como se entregó en el guion ("GUION CINEMÁTICA INICIAL"). Los textos van igual que en el
/// guion; lo único que se llena aquí son los datos que el guion dejó "a definir" (ciudad, nombres, profesión).
/// Para cambiarlos basta con editar las constantes de abajo: todo el juego los toma de aquí.
/// </summary>
public static class Historia
{
    // ------------------------------------------------------------------ datos "a definir" en el guion
    /// <summary>Ciudad donde aparece la torre ("la ciudad [a definir]").</summary>
    public const string Ciudad = "Cancún";
    /// <summary>Nombre del hermano de Abby ("[Nombre del hermano de Abby]"). Definido en el GDD 0.6.0: Dante.</summary>
    public const string Hermano = "Dante";
    /// <summary>Nombre del capitán de la expedición ("Capitán [Nombre aún por definir]"). GDD 0.6.0: Elías Vargas.</summary>
    public const string CapitanNombre = "Elías Vargas";
    /// <summary>El padre (su funeral abre la historia). GDD 0.6.0: Angelo.</summary>
    public const string Padre = "Angelo";
    public const string Capitan = "Capitán " + CapitanNombre;
    /// <summary>Profesión del hermano ("[nombre de profesión a definir]").</summary>
    public const string Profesion = "cazador del abismo";

    public const string Protagonista = "Abby";

    // ------------------------------------------------------------------ contexto y presentación de la torre
    public static readonly string[] Torre =
    {
        "En el año 2030, en medio de la ciudad " + Ciudad + " se materializó una torre misteriosa de gran tamaño.",
    };
    public static readonly string[] Abismo =
    {
        "Cuando el ejército ingresó a la torre lo que descubrieron unas enormes escaleras que llevaban a un abismo oscuro.",
        "Al adentrarse en él descubrieron que ahí habitaban criaturas que solo existían en nuestras pesadillas y cuantos más pisos bajaran más fuertes se volvían.",
    };
    public static readonly string[] Canalizadores =
    {
        "Junto con la torre empezaron a aparecer personas con la capacidad de canalizar la extraña energía que emana la torre, potenciando sus armas, sus cuerpos y hasta permitiéndoles realizar hechizos",
    };
    public static readonly string[] Minerales =
    {
        "Al morir las criaturas dejaban caer minerales raros nunca antes vistos en la tierra, la humanidad no tardó en descubrir su valor y comenzó a utilizarlos en joyería, energía, industrialización. Estos minerales se volvieron el nuevo centro de la economía global.",
    };

    // ------------------------------------------------------------------ presentación y contexto de Abby
    public struct Linea { public string quien, texto; public Linea(string q, string t) { quien = q; texto = t; } }

    public static readonly Linea[] Funeral =
    {
        new Linea("Invitada 1", "Pobrecitos. Huérfanos a tan corta edad."),
        new Linea("Invitada 2", "Su padre murió cumpliendo su deber cuando apareció esa misteriosa torre."),
        new Linea("Invitada 1", Hermano + " solo tiene 17 como va a mantener a su hermana ahora que su padre no está y Abby solo tiene 12 años. ¡Qué futuro les aguarda?"),
    };

    public const string Anio2035 = "Año 2035";

    public static readonly Linea[] Discurso =
    {
        new Linea(Capitan, "Escuchen todos. Han pasado 5 años desde que la torre del abismo apareció y con ella las aberraciones que residen dentro."),
        new Linea(Capitan, "Cuando enfrentamos por primera vez a las criaturas del abismo nuestras armas de pólvora no les hacían ni cosquillas, perdimos muchas vidas para superar el primer piso."),
        new Linea(Capitan, "Entonces la humanidad aprendió a usar la misma energía de la torre contra ella desde entonces hemos llegado hasta el piso 15."),
        new Linea(Capitan, "Pero pese a todo el avance aun no sabemos que es esta torre, de donde proviene o por qué apareció."),
        new Linea(Capitan, "Ahora es momento de que la humanidad y nosotros unamos fuerzas para ir más allá del piso 15 y desvelar todos los misterios que se encuentran dentro de la torre, dentro del abismo..."),
    };

    public static readonly Linea[] Despedida =
    {
        new Linea(Protagonista, "Hermano, no te vayas, no tienes que ser un " + Profesion + ". Buscaremos otra forma, buscaré otro trabajo, dejaré de estudiar para ayudarte a pagar nuestras deudas, pero por favor, no vayas a la expedición, es muy peligroso..."),
        new Linea(Hermano, "No Abby!, no puedes abandonar tus estudios, tienes que salir adelante. No te preocupes por nada más, la paga es buena y el capitán " + CapitanNombre + " liderará la expedición, con el al frente estaremos bien. Volveré..."),
    };

    /// <summary>Nombre del juego en pantalla (título del proyecto entregado).</summary>
    public const string Titulo = "ABBY'S TOWER";
    public const string Subtitulo = "La torre del abismo";
}
