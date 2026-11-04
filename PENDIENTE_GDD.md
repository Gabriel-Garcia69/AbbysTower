# Qué falta según el GDD — 3 de noviembre de 2026

Rama `animaciones`. El GDD congela funciones nuevas el **10 de noviembre**; la demo es el **20**.
GDD: `Downloads/GDD_Abbys_Tower_detallado.docx` (v0.6.0). Detalle de lo que ya se hizo: `NOTAS_PULIDO.md` y `Docs/Cambios_respecto_al_GDD.md`.

## Por dónde seguir
1. **Revisar el color y la luz.** `AmbienteHistoria.cs` tiene color por piso (Pueblo ámbar, Cascadas azul violeta, Acantilado carmesí) y una luz suave que sigue a Abby, porque con el atardecer quedaba como silueta. Compila, pero **no lo he visto en Play** y **no tiene commit**. La prueba quedó a medias: correr `PruebaPisos.Combates`, ver capturas y ajustar.
2. **Medir el `.exe`.** `Assets/scripts/MedidorBuild.cs` (temporal, sin subir). Armar con `ConstruirJuego.Windows` y abrir `AbbysTower.exe -medir <archivo>`. Metas: 60 FPS, menos de 5 s de carga por piso, máximo 2 GB.
3. **Borrar el medidor y volver a compilar** el `.exe` final: `MedidorBuild.cs` no debe ir en la entrega, y el build actual es anterior a los tajos.
4. **Hacer `git push`** de los commits locales (GitHub puede pedir iniciar sesión).

## Falta para cumplir el GDD
| Falta | Quién | Estado |
|---|---|---|
| Audio final (teclado, órgano y violín; 7 efectos) en `Assets/Resources/Audio/` | Francisco | Suena lo sintetizado provisional |
| Jugar la partida entera seguida, 15 minutos, de principio a fin | Equipo | Probé cada parte por separado, no todo seguido |
| Prueba por otra persona ("terminado" lo exige) | Karla o Ricardo | Sin hacer |
| Pasar `animaciones` a `dev` con un PR | Gabriel / Rodrigo | Sin hacer, unos 90 commits de ventaja |
| Rendimiento del `.exe` | Claude | Medidor listo, sin correr |
| Revisar que los colores cumplan la paleta DB32 | Equipo | Sin revisar |
| Actualizar el GDD con los cambios | Equipo | Lista en `Docs/Cambios_respecto_al_GDD.md` |

## Decisiones del equipo
- **Orden:** el GDD dice Cinemática → Tutorial; el juego hace Tutorial → Cinemática.
- **Biblioteca (CuartosA):** no está en el GDD y tiene 1 horda, no 4. Ya no tiene mercader ni cofre.
- **Continuar partida:** el GDD dice "sin guardado"; lo dejamos porque ayuda si algo falla en la presentación.
- **Jefes sin usar:** Guardián, Capitán, Torrente y Gárgola tienen sprites y animaciones, pero **no salen en el juego**. El único jefe es Dante, como pide el GDD. ¿Se quedan fuera o se agregan?

## Detalles menores
- Dante en el tutorial todavía es Abby con tinte azul.
- La imagen 12 de la intro muestra una torre en ruinas, no la morada.
- Falta el ícono del juego.
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
- **Se queda en "Reloading Domain" muchos minutos:** se colgó una vez justo después de que se desconectó el relay del paquete de IA de Unity. Hay que cerrarlo y volver a abrirlo.
- **Disco lleno:** el aviso "Layout update failed" infla el log. Se borra con Unity cerrado.
- **Con Unity minimizado el juego no avanza:** las pruebas usan `Application.runInBackground = true`.
