# Abby's Tower — Cambios respecto al GDD 0.6.0

Rama `animaciones` · actualizado el 3 de noviembre de 2026 · Demo: 20 de noviembre de 2026

Este documento resume cómo quedó el juego frente al GDD (`GDD_Abbys_Tower_detallado.docx`, v0.6.0), qué se decidió distinto y qué falta. Sirve para actualizar el GDD y para revisar el PR a `dev`.

## Decisiones tomadas (Gabriel, 20 oct 2026)

| Tema | GDD | Juego | Por qué |
|---|---|---|---|
| Protagonista | Abby | Abby (no "Abis" del guion) | El GDD manda sobre el guion |
| Combate | Combo 10/10/15, bloqueo con stamina | Igual, **y se conservan salto e impulso**; el impulso gasta 20 de stamina | Los mapas usan saltos y el impulso; así no se abusa de él |
| Ruta | 5 pisos + Dante | Pueblo → Puerto → Cascadas → Acantilado → Cima (Dante) | Recorte a 5 pisos |
| Objetos | Solo notas | Solo notas (fuera mercaderes, cofres, pociones y minerales del HUD) | GDD |
| Cámara | Fija | Fija; **F3** cámara libre para apreciar el mundo, **F4** vuelve | Pedido para mostrar los mapas |
| Guardado | Sin guardado | **Continuar partida**: guarda el último lugar de la ruta y se borra al terminar | Si algo falla en la presentación, no se empieza de cero |
| Lugares | Solo pisos | Además **Cala Sombría** (exterior, antes de la torre) y la **Biblioteca** (entre el Piso 1 y el 2), sin mercader ni cofre | Rodrigo los agregó; la Biblioteca tiene una sola horda, el GDD pide 4 por piso |
| Orden | Cinemática → Tutorial | Tutorial (Expedición) → Cinemática | Del guion; **pendiente de confirmar con el equipo** |

## Lo que está hecho

### Flujo y pantallas
- Menú principal (Jugar, Configuración con volumen general / música / efectos, Salir con confirmación), pausa con Esc, pantalla de derrota con Reintentar, cinemática de inicio saltable.
- Morir con Dante reinicia solo la pelea del jefe; final con cliffhanger ("Abby…", "Continuará…") y créditos.

### Abby
- Combo de 3 tajos, bloqueo con clic derecho (8 % de daño, stamina 100, −20 por golpe, se recupera 25/s después de 1 s), salto e impulso.
- Sprites coherentes en 8 direcciones: de pie, caminar, impulso, **pose de golpe** y **pose de guardia** (mismo dibujo, misma cara y tamaño, espada siempre en la misma mano). El golpe recibido es un tinte rojo con sacudida.

### Enemigos (GDD: uno nuevo por piso)
| Piso | Enemigo nuevo | Tipo en código |
|---|---|---|
| 1 | Larva del Abismo | `Sombra` |
| 2 | Acechador y Bruto | `Saltador`, `Bruto` |
| 3 | Embestidor | `Embestidor` |
| 4 | Guardián (escudo) | `Escudero` |
| 5 | Espectro, antes de Dante | `Hechicero` |

- Todos con pixel art de PixelLab en 8 direcciones (`Assets/Resources/EnemigosSprites/<Tipo>/`), con movimiento hecho en código: respiran, rebotan al caminar, se agachan al avisar, se estiran al lanzarse, tiemblan al recibir y se hacen sombra al morir.
- Aparecen saliendo de una **grieta del abismo** en el suelo (pozo negro, borde morado, humo y luz), no de la nada.
- El único jefe en el juego es Dante. Hay dibujos y animaciones de otros cuatro jefes (Guardián de las Ruinas, Capitán sin Barco, Señor del Torrente y Gárgola del Faro Roto), sin usar. Dante usa su propio dibujo de sombra.
- 4 hordas por piso.

### Mapas
- Casas distintas por piso con techo de verdad: entramado (Pueblo), pescador encalada (Puerto), piedra con paja (Cascadas), pizarra (Acantilado).
- Fogatas y braseros con llamas de partículas, carpas de lona, santuario de curación con cristal, pozo, puestos de mercado, faro roto.
- Caminos en una sola malla por material (sin parpadeo en esquinas) y distintos por mapa: tierra, empedrado y grava.
- Rocas facetadas en lugar de esferas.

### Audio
- Música provisional sintetizada (Menú, Combate, Jefe y Paisaje para los mapas con agua) y 7 efectos. Se reemplazan solos con los archivos de Francisco en `Assets/Resources/Audio/` (`Musica_<Pista>`, `<Efecto>`).

### Rendimiento
- La música se compone en otro hilo al abrir el juego (antes trababa al empezar cada horda).
- Sombra del sol a 2048. Todas las escenas de la ruta corren a 60 fps en el editor y sin errores en consola.

## Pendiente
- **Audio final** de Francisco (`Assets/Resources/Audio/`, hoy suena el sintetizado).
- Prueba completa de principio a fin por alguien del equipo (Karla o Ricardo) y pase a `dev`.
- Ícono del juego.
- En el tutorial, Dante todavía es Abby con tinte azul.
- Guardián de las Ruinas, Capitán, Torrente y Gárgola tienen sprites y animaciones, pero **no aparecen en el juego**: el único jefe es Dante, como pide el GDD.
- Sin revisar si los colores cumplen la paleta DB32.

## Para actualizar el GDD
1. Sección de controles: añadir F3 / F4 (cámara libre) y mantener salto e impulso.
2. Sección de enemigos: nombres y tipos de la tabla de arriba.
3. Sección de arte: el combo de Abby son 3 tajos animados de 4-5 cuadros (`Resources/AbbyAnim`); el bloqueo es una pose (guardia).
4. Ruta: Cima es el piso 5 con Dante.

## Cambios del 5 de noviembre
- Tutorial: se juega con Dante humano y sprites propios. La gruta es una cueva de roca natural, más oscura.
- Criatura del tutorial: nueva cinemática con rugido, zarpazo y golpe. La expedición y Dante dejan de verse y el golpe corta a negro: no se muestra qué les pasó.
- Impulso: gasta 20 de stamina y tiene sonido propio.
- Fuente del juego: Inter (licencia OFL); el logo se queda en Press Start 2P. Diálogos más grandes y con más interlineado.
