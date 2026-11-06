# Qué falta según el GDD — 5 de noviembre de 2026

Rama `animaciones`. El GDD congela funciones nuevas el **10 de noviembre**; la demo es el **20**.
GDD: `Downloads/GDD_Abbys_Tower_detallado.docx` (v0.6.0). Detalle de lo que ya se hizo: `NOTAS_PULIDO.md` y `Docs/Cambios_respecto_al_GDD.md`.

## Por dónde seguir
1. **Color y luz: revisados el 4 de noviembre** en capturas de los 6 pisos con enemigos, sin errores. Falta verlos jugando; en Cima y Puerto, mirar si el fondo claro convence.
2. **`.exe` medido el 4 de noviembre** en esta laptop (RTX 5050, 1920x1080), en cada escena sin combate: 93 a 346 FPS, carga de 0.2 a 0.6 s, 1089 MB de memoria reservada. Cumple las metas del GDD. Falta probar en combate y en una PC más modesta.
3. **Build final:** `C:\Users\Gabriel\Unity\Build_AbbysTower\AbbysTower.exe`, ya sin medidor y con ícono.

## Cala Sombría (4 de noviembre)
- Mapa unas 3 veces más grande: 73 casas, calles anchas, plaza del mercado, campamento y torre al 60 %, a 217 m de la salida.
- Noche oscura y desaturada: perfil `ExteriorProfile_Noche`, ambiente y niebla en `ExteriorNoche.cs`. Lo único con color es el fuego y el aura de la torre.
- Tomas nuevas en la intro y textos legibles. Las casas que tapan a Abby dejan de dibujarse (`OccluderFade`).
- Se corrigió que hubiera casas en medio de la Calle Mayor. Caminata de prueba: de la salida a la torre en 33 s a 7 m/s, y entra a Diorama. Abby camina a 5 m/s, así que son unos 45 s; si se siente largo, se mueve `CalaSpawn`.
- Cualquier cambio en `ExteriorBuilder*` necesita regenerar con el menú `Abby/Exterior/Hora: Noche` y guardar la escena.

## Dante humano (5 de noviembre)
- PixelLab, personaje `22ccdc52-092c-4438-a3ea-850e00c8db7d`: 17 años, abrigo azul marino, espada, estilo de Abby. Mide 1.45 m en el juego.
- En `Assets/Resources/DanteHumano`: de pie en 8 direcciones; caminar y atacar en 5 (las del oeste se voltean por código). No tiene impulso, bloqueo ni golpe recibido propios: usa caminar, de pie y el tinte rojo.
- Solo se usa en el tutorial. `Personajes/Dante` y `EnemigosAnim/Dante` siguen siendo su sombra de jefe final, en Cima.

## Gruta del tutorial y HUD (5 de noviembre)
- **Gruta (Expedición):** cueva de roca natural: paredes (`ExpRoca`, `ParedesGruta`), techo y fondo del abismo (`DioramaBuilder.ExpedicionCueva.cs`) en mallas con ruido, columnas y peñascos irregulares. El suelo se queda plano para que las runas se vean. Luz baja en `LuzDeSala`, ambiente en `BuildAtmosphere`. Para ver cambios del builder: regenerar la escena y guardarla.
- **Jefe:** `Resources/ColosoAnim`: rugido, zarpazo y golpe (PixelLab, personaje `44035936`), con los ojos visibles. Cinemática en `ExpedicionPiso20.Derrota`: en contrapicado con lente abierto; desde que sale la criatura no se ve a Dante ni a la expedición y el golpe corta a negro, para que no se sepa qué les pasó. Sonidos sintetizados: `Rugido`, `Temblor`, `Impacto`.
- **HUD:** fuente Inter (el logo sigue pixelado), diálogos más grandes con interlineado, retrato centrado en la cabeza, barra de stamina más gruesa.
- **Impulso:** gasta 20 de stamina y tiene sonido propio. Los sonidos no los pude escuchar: hay que revisarlos jugando.
- **Cámara:** se corrigió un bug: lo que subía para no meterse al suelo nunca se deshacía.

## Ocho pisos, final y mapas abiertos (5 de noviembre)
- **Ruta:** Diorama, Puerto, Cascadas, Acantilado, Cueva, Lago, Nevado, Cima y la escena `Final`. El GDD pide 5 pisos: los 3 extra son decisión de Gabriel y están en `Docs/Cambios_respecto_al_GDD.md`.
- **Historia:** Abby comenta en cada piso (`RelatoPiso`), hay notas nuevas en `Historia.Notas` y el colgante une principio y final. Se mantiene el "Abby…" y "Continuará…" del GDD.
- **Final:** escena propia sin enemigos (`DioramaBuilder.Final`, `EscenaFinal`): pedazo de torre sobre el abismo, 11 planos de cámara, recuerdos, casa en la nieve con créditos.
- **Mapas:** Acantilado bajado de 9.5 m a 5 m, con islotes más anchos y casas fuera del paso. Puerto con bancales bajos. Caminata de prueba completa en Acantilado sin atorarse.
- **Cámara:** la apertura barre el paisaje, si Abby se queda quieta 6 s la cámara se abre y recorre la vista (no en Expedición, Cueva ni Final). Casas y árboles que tapan a Abby dejan de dibujarse.
- **Superficies encimadas:** corregidas con el menú `Abby/Separar superficies encimadas`. **Hay que volver a correrlo si se regenera un mapa.** Quedan pocas en cada mapa (42 en Puerto, casi todas tablones).
- **Nivel 1:** la estela que estorbaba en el camino de tierra se movió a un lado, en la escena y en el código.
- **Imágenes nuevas (PixelLab, 30 generaciones):** `Intro/12_despedida` rehecha (Abby de 17, colgante, torre morada), `13_colgante` y `14_regreso`. Se usan en la intro y en los recuerdos del final. `Final/colgante` es el colgante recortado de la imagen 13.
- **Sin revisar en movimiento:** nada de esto lo jugó una persona; se revisó con capturas y pruebas automáticas.

## Rediseños del 6 de noviembre
- **Piso 4 (Acantilado):** rehecho desde cero en cubos, como el piso 1: prado, escalinata a la playa con aldea y esqueleto de barco, isla del naufragio, puente largo, promontorio con el faro; cascada, laguna, arcos y farallones.
- **Piso 5 (Gruta):** caverna inundada, cerrada por los cuatro lados: lago turquesa con cuatro islas de bloques y puentes. Pueblo minero, bosque de cristal, torre del pozo y Corazón de Geoda; techo de bloques con cristales colgando.
- **Pisos 2 y 3:** Puerto con tejas menos rojas; Cascadas más clara y con bancales anchos en lugar de murallas.
- **Entrada a cada mundo:** volvió la toma aérea del piso 1, que baja hasta Abby. Los demás pisos usan ese mismo tipo de toma, desde más arriba; sus valores están en `ZonasCamara.Aperturas`. La gruta la hace bajo el techo.
- **Cámara:** Abby va más arriba en pantalla. F5 es la normal y F6 la misma con 5° más de inclinación; se recuerda. `ZonasCamara` gira la vista unos grados en ciertas zonas, también peleando; la tabla está en ese archivo para ajustarla.
- **Final:** Dante tendido nuevo (`DanteHumano/Tendido`) sobre un descanso plano de la escalinata. La Cima y el final ya no tienen franja negra en el horizonte.
- **Créditos y menú:** degradados en una sola textura (`Degradado`), sin líneas.

## Falta para cumplir el GDD
| Falta | Quién | Estado |
|---|---|---|
| Audio final (teclado, órgano y violín; 7 efectos) en `Assets/Resources/Audio/` | Francisco | Suena lo sintetizado provisional |
| Jugar la partida entera seguida, 15 minutos, de principio a fin | Equipo | Probé cada parte por separado, no todo seguido |
| Prueba por otra persona ("terminado" lo exige) | Karla o Ricardo | Sin hacer |
| Pasar `animaciones` a `dev` con un PR | Gabriel / Rodrigo | Sin hacer: 97+ commits de ventaja y `dev` tiene 2 que `animaciones` no. Falta integrarlos |
| Rendimiento del `.exe` en combate y en otra PC | Equipo | `.exe` sin combate: cumple. Con 6 enemigos, en el editor: unos 57 FPS en los 6 pisos. Falta el `.exe` en combate y otra PC |
| Decidir la paleta DB32 | Equipo | Revisado el 5 de noviembre: 0 % de píxeles exactos en Abby, Dante, Coloso e imágenes de la intro. Forzarla cambia todo el arte: mejor anotarlo como cambio al GDD |
| Actualizar el GDD con los cambios | Equipo | Lista en `Docs/Cambios_respecto_al_GDD.md` |

## Decisiones del equipo
- **Orden:** el GDD dice Cinemática → Tutorial; el juego hace Tutorial → Cinemática.
- **Biblioteca (CuartosA):** no está en el GDD y tiene 1 horda, no 4. Ya no tiene mercader ni cofre.
- **Continuar partida:** el GDD dice "sin guardado"; lo dejamos porque ayuda si algo falla en la presentación.
- **Jefes sin usar:** Guardián, Capitán, Torrente y Gárgola tienen sprites y animaciones, pero **no salen en el juego**. El único jefe es Dante, como pide el GDD. ¿Se quedan fuera o se agregan?

## Detalles menores
- El bloqueo de Abby es una pose de guardia, sin animación (no gasté generaciones en eso).
- Antes de entregar, borrar las herramientas de `Assets/Editor`: `PruebaAnim`, `TourExterior`, `TourCala`, `TourExp`, `TourRevision`, `TourRevisionPlay`, `TourCamino`, `TourFinal`, `TourNuevos`, `CamaraRemota`, `PruebaPisos`, `PruebaMover`, `PruebaReglas` y `ConstruirJuego`. No van en el `.exe` ni están en Git, pero siguen en la carpeta del proyecto.

## Ya probado en Play (3 de noviembre)
- Combo de 3 tajos animados de Abby (4-5 cuadros), en las direcciones este, oeste, sur y norte. Las diagonales tienen sus cuadros (las del oeste se voltean), pero no se vieron jugando.
- Bloqueo: 8 % del daño, −20 de stamina por golpe, sin stamina no bloquea, recupera unos 25 por segundo.
- Fuente: cura al 100 %, un solo uso.
- Morir y reintentar: reinicia el piso con la vida llena.
- Final con Dante: "Abby…", "Continuará…", créditos y menú; el guardado se borra.

## Si Unity da problemas
- **Ventana "Connect to server":** es Plastic. Dale Cancel, sin meter datos. Sale al abrir Unity sin pasar por el Hub, y mientras esté abierta Unity no avanza.
- **Se queda en "Reloading Domain" muchos minutos:** volvió a pasar el 4 de noviembre, ya sin el paquete de IA, así que la causa sigue sin conocerse. Si la CPU de Unity está casi en cero, está colgado: hay que cerrarlo y volver a abrirlo.
- **Disco lleno:** el aviso "Layout update failed" infla el log. Se borra con Unity cerrado.
- **Con Unity minimizado el juego no avanza:** las pruebas usan `Application.runInBackground = true`.
