/// <summary>
/// La historia del juego tal como se entregó en el guion ("GUION CINEMÁTICA INICIAL"). Los textos van igual que en el
/// guion; lo único que se llena aquí son los datos que el guion dejó "a definir" (ciudad, nombres, profesión).
/// Para cambiarlos basta con editar las constantes de abajo: todo el juego los toma de aquí.
/// </summary>
public static class Historia
{
    // ------------------------------------------------------------------ datos "a definir" en el guion
    /// <summary>Ciudad donde aparece la torre ("la ciudad [a definir]").</summary>
    public const string Ciudad = "Cala Sombría";
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
        "En el año 2030, en medio del pueblo de " + Ciudad + " se materializó una torre misteriosa de gran tamaño.",
    };
    public static readonly string[] Abismo =
    {
        "Cuando el ejército ingresó a la torre, descubrieron unas enormes escaleras que llevaban a un abismo oscuro.",
        "Al adentrarse en él descubrieron que ahí habitaban criaturas que solo existían en nuestras pesadillas, y cuantos más pisos bajaban, más fuertes se volvían.",
    };
    public static readonly string[] Canalizadores =
    {
        "Junto con la torre empezaron a aparecer personas con la capacidad de canalizar la extraña energía que emana la torre, potenciando sus armas, sus cuerpos y hasta permitiéndoles realizar hechizos.",
    };
    public static readonly string[] Minerales =
    {
        "Al morir, las criaturas dejaban caer minerales raros nunca antes vistos en la Tierra. La humanidad no tardó en descubrir su valor y comenzó a utilizarlos en joyería, energía e industria. Estos minerales se volvieron el nuevo centro de la economía global.",
    };

    // ------------------------------------------------------------------ presentación y contexto de Abby
    public struct Linea { public string quien, texto; public Linea(string q, string t) { quien = q; texto = t; } }

    public static readonly Linea[] Funeral =
    {
        new Linea("Invitada 1", "Pobrecitos. Huérfanos a tan corta edad..."),
        new Linea("Invitada 2", "Su padre murió cumpliendo su deber cuando apareció esa misteriosa torre."),
        new Linea("Invitada 1", Hermano + " solo tiene 17. ¿Cómo va a mantener a su hermana ahora que su padre no está? Y Abby solo tiene 12 años. ¿Qué futuro les aguarda?"),
    };

    public const string Anio2035 = "Año 2035";

    public static readonly Linea[] Discurso =
    {
        new Linea(Capitan, "Escuchen todos. Han pasado 5 años desde que la torre del abismo apareció y, con ella, las aberraciones que residen dentro."),
        new Linea(Capitan, "Cuando enfrentamos por primera vez a las criaturas del abismo, nuestras armas de pólvora no les hacían ni cosquillas. Perdimos muchas vidas para superar el primer piso."),
        new Linea(Capitan, "Entonces la humanidad aprendió a usar la misma energía de la torre contra ella. Desde entonces hemos llegado hasta el piso 15."),
        new Linea(Capitan, "Pero, pese a todo el avance, aún no sabemos qué es esta torre, de dónde proviene o por qué apareció."),
        new Linea(Capitan, "Ahora es momento de que la humanidad y nosotros unamos fuerzas para ir más allá del piso 15 y desvelar todos los misterios que se encuentran dentro de la torre... dentro del abismo."),
    };

    public static readonly Linea[] Despedida =
    {
        new Linea(Protagonista, "Hermano, no te vayas, no tienes que ser un " + Profesion + ". Buscaremos otra forma: buscaré otro trabajo, dejaré de estudiar para ayudarte a pagar nuestras deudas, pero, por favor, no vayas a la expedición, es muy peligroso..."),
        new Linea(Hermano, "¡No, Abby! No puedes abandonar tus estudios, tienes que salir adelante. No te preocupes por nada más: la paga es buena y el capitán " + CapitanNombre + " liderará la expedición. Con él al frente estaremos bien. Volveré..."),
        new Linea(Protagonista, "Entonces llévate esto. Era de papá. Me lo devuelves cuando vuelvas."),
        new Linea(Hermano, "Te lo devuelvo en persona. Te lo prometo."),
    };

    // ------------------------------------------------------------------ lo que Abby va descubriendo al subir
    public class LineasPiso { public string[] inicio, mitad, fin; }

    static LineasPiso P(string[] inicio, string[] mitad, string[] fin) => new LineasPiso { inicio = inicio, mitad = mitad, fin = fin };

    /// <summary>Por escena: al entrar, a la mitad de las hordas y al abrirse la salida. El hilo: las huellas que bajan... y las que suben.</summary>
    public static readonly System.Collections.Generic.Dictionary<string, LineasPiso> Relato = new System.Collections.Generic.Dictionary<string, LineasPiso>
    {
        ["Exterior"] = P(new[] { "Hace tres meses que la expedición entró. A las dos semanas dejaron de buscarla.", "Nadie va a entrar por él. Entonces entro yo." }, null, null),
        ["Diorama"] = P(new[] { "Un pueblo entero, con las puertas abiertas. Y nadie.", Hermano + " pasó por aquí. Tuvo que dejar algo." },
            new[] { "Cuando papá entró, en 2030, dijo que volvería para la cena." },
            new[] { "Ni rastro de él todavía. Sigo." }),
        ["CuartosA"] = P(new[] { "Libros, en un lugar donde nadie lee." }, null,
            new[] { Hermano + " me hacía repasar en voz alta antes de irse a trabajar. Yo fingía que no me importaba." }),
        ["Puerto"] = P(new[] { "El mar, otra vez. Papá nos traía a ver los barcos antes de la torre." },
            new[] { "El colgante de papá. Se lo di el día que se fue: me lo devuelves cuando vuelvas, le dije." },
            new[] { "Todavía me lo debe." }),
        ["Cascadas"] = P(new[] { "Me hizo prometer que seguiría estudiando." },
            new[] { "Dejé la escuela el día que cerraron la búsqueda. Si me viera, se enojaría." },
            new[] { "Que se enoje. Pero que me lo diga en la cara." }),
        ["Acantilado"] = P(new[] { "Hay huellas en la piedra. Muchas botas, y todas van hacia adentro." },
            new[] { "Aquí hay otras, encima de las primeras. Las mismas botas..." },
            new[] { "...pero estas vienen de regreso. Y son de una sola persona." }),
        ["Cueva"] = P(new[] { "Estos minerales pagaron nuestra casa. Nunca pregunté de dónde salían." },
            new[] { "Hay una mano marcada en el cristal. Negra, como quemada." },
            new[] { "Alguien se apoyó aquí para poder seguir caminando." }),
        ["Lago"] = P(new[] { "Todo está en calma. Es lo primero que me da miedo en esta torre." },
            new[] { "En el agua se ve mi cuarto. Con la luz prendida, como él la dejaba." },
            new[] { "Por un momento vi a alguien detrás de mí, en el reflejo. Llevaba su abrigo." }),
        ["Nevado"] = P(new[] { "Las ventanas están prendidas, pero adentro no hay nadie." },
            new[] { "Una fogata apagada y una sola manta. Nadie duerme así si viaja con otros." },
            new[] { Hermano + ", ¿qué te pasó allá abajo?" }),
        ["Cima"] = P(new[] { "Aquí se acaban las huellas." },
            new[] { "Hay algo más adelante. Lo siento desde que entré. No se mueve... me está esperando." }, null),
    };

    /// <summary>El texto de las notas que cambió al crecer la historia. Se aplica al leer: así no hay que regenerar los pisos. (título, texto, firma)</summary>
    public static readonly System.Collections.Generic.Dictionary<string, string[]> Notas = new System.Collections.Generic.Dictionary<string, string[]>
    {
        ["dio2_diario"] = new[] { "Diario de la expedición — día 2", "El piso 2 tiene mar. Barcos amarrados, gaviotas que vuelan en círculos y ni una persona.\nTraigo el colgante de papá debajo de la camisa, Abby. Lo voy a cuidar. Es tuyo.", "— " + Hermano },
        ["dio3_diario"] = new[] { "Diario de la expedición — día 6", "En este piso el sol nunca termina de ponerse. Cruzamos los puentes colgantes de uno en uno.\nCuando su sombra cae al suelo, hay que moverse. Eso me salvó hoy.\nTe lo apunto por si acaso. Aunque no vas a venir, ¿verdad?", "— " + Hermano },
        ["dio4_diario"] = new[] { "Diario de la expedición — día 9", "El piso 5 es una gruta llena de cristales. Brillan con la misma energía que usamos en las armas.\nCon lo de este piso pagaríamos todas las deudas, Abby. Todas.\nLos canalizadores dicen que no hay que tocarlos con la mano desnuda. Que la energía se queda.", "— " + Hermano },
        ["dio_lago_pueblo"] = new[] { "Piedra de los pescadores", "Piso 6. El lago refleja un cielo que no es el de afuera.\nLos que se asoman dicen ver su casa. Mejor no quedarse mucho tiempo mirando.", "" },
        ["dio6_aldea"] = new[] { "Bienvenidos a la aldea", "Piso 7. Aquí siempre es la última noche del invierno.\nHay posada en la plaza. Las luces no se apagan nunca.", "" },
        ["dio6_diario"] = new[] { "Hoja sin fecha", "Ya no cuento los días. Del piso 20 salí solo.\nEsa cosa nos alcanzó a todos. El capitán me empujó hacia las escaleras y no volví a verlo.\nMe quedó algo negro en el brazo y no se quita. Ya me llega al hombro. No duele. Eso es lo peor.\nVoy de regreso, Abby. Te dije que volvería.", "— " + Hermano },
        ["dio7_diario"] = new[] { "La última hoja", "Se me olvida cómo era tu voz. La letra ya casi no me sale.\nTraigo el colgante en la mano para acordarme de a dónde voy.\nQuedan tan poquitos pisos.\nSi ves algo en el camino, no le tengas miedo. Pero no te acerques.\nVolveré.", "— D." },
    };

    /// <summary>Nombre del juego en pantalla (título del proyecto entregado).</summary>
    public const string Titulo = "ABBY'S TOWER";
    public const string Subtitulo = "La torre del abismo";
}
