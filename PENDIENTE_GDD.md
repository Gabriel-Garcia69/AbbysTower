# Qué falta según el GDD — 4 de noviembre de 2026

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
- **Gruta (Expedición):** paredes de bloques en terrazas (`ExpHeight`), salas anchas (`ExpMitad`), luz sombría sin azul (`LuzDeSala`, `VolumenExpedicion`), suelo gris. Para ver cambios del builder: regenerar la escena y guardarla.
- **Jefe:** animaciones `Resources/ColosoAnim` (rugido y golpe, PixelLab) y cinemática nueva en `ExpedicionPiso20.Derrota`, en contrapicado con lente abierto. Sonidos sintetizados: `Rugido`, `Temblor`, `Impacto`.
- **HUD:** fuente Inter (el logo sigue pixelado), diálogos más grandes con interlineado, retrato centrado en la cabeza, barra de stamina más gruesa.
- **Impulso:** gasta 20 de stamina y tiene sonido propio. Los sonidos no los pude escuchar: hay que revisarlos jugando.
- **Cámara:** se corrigió un bug: lo que subía para no meterse al suelo nunca se deshacía.

## Falta para cumplir el GDD
| Falta | Quién | Estado |
|---|---|---|
| Audio final (teclado, órgano y violín; 7 efectos) en `Assets/Resources/Audio/` | Francisco | Suena lo sintetizado provisional |
| Jugar la partida entera seguida, 15 minutos, de principio a fin | Equipo | Probé cada parte por separado, no todo seguido |
| Prueba por otra persona ("terminado" lo exige) | Karla o Ricardo | Sin hacer |
| Pasar `animaciones` a `dev` con un PR | Gabriel / Rodrigo | Sin hacer, unos 90 commits de ventaja |
| Rendimiento del `.exe` en combate y en otra PC | Equipo | Medido sin combate en la laptop de Gabriel: cumple |
| Revisar que los colores cumplan la paleta DB32 | Equipo | Sin revisar |
| Actualizar el GDD con los cambios | Equipo | Lista en `Docs/Cambios_respecto_al_GDD.md` |

## Decisiones del equipo
- **Orden:** el GDD dice Cinemática → Tutorial; el juego hace Tutorial → Cinemática.
- **Biblioteca (CuartosA):** no está en el GDD y tiene 1 horda, no 4. Ya no tiene mercader ni cofre.
- **Continuar partida:** el GDD dice "sin guardado"; lo dejamos porque ayuda si algo falla en la presentación.
- **Jefes sin usar:** Guardián, Capitán, Torrente y Gárgola tienen sprites y animaciones, pero **no salen en el juego**. El único jefe es Dante, como pide el GDD. ¿Se quedan fuera o se agregan?

## Detalles menores
- La imagen 12 de la intro muestra una torre en ruinas, no la morada.
- El bloqueo de Abby es una pose de guardia, sin animación (no gasté generaciones en eso).
- Antes de entregar, borrar las herramientas de `Assets/Editor`: `PruebaAnim`, `TourExterior`, `CamaraRemota`, `PruebaPisos`, `PruebaMover`, `PruebaReglas` y `ConstruirJuego`.

## Ya probado en Play (3 de noviembre)
- Combo de 3 tajos animados de Abby (4-5 cuadros), en las direcciones este, oeste, sur y norte; las diagonales no se revisaron en el juego.
- Bloqueo: 8 % del daño, −20 de stamina por golpe, sin stamina no bloquea, recupera unos 25 por segundo.
- Fuente: cura al 100 %, un solo uso.
- Morir y reintentar: reinicia el piso con la vida llena.
- Final con Dante: "Abby…", "Continuará…", créditos y menú; el guardado se borra.

## Si Unity da problemas
- **Ventana "Connect to server":** es Plastic. Dale Cancel, sin meter datos. Sale al abrir Unity sin pasar por el Hub, y mientras esté abierta Unity no avanza.
- **Se queda en "Reloading Domain" muchos minutos:** volvió a pasar el 4 de noviembre, ya sin el paquete de IA, así que la causa sigue sin conocerse. Si la CPU de Unity está casi en cero, está colgado: hay que cerrarlo y volver a abrirlo.
- **Disco lleno:** el aviso "Layout update failed" infla el log. Se borra con Unity cerrado.
- **Con Unity minimizado el juego no avanza:** las pruebas usan `Application.runInBackground = true`.
