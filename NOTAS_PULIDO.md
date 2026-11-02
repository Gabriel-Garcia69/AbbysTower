# Notas del pulido — noche del 1 al 2 de noviembre de 2026

Todo está en la rama `animaciones` (subida a GitHub). Cada punto dice qué se arregló y en qué archivo.

## Enemigos y jefes
- **Animaciones de caminar y atacar** de los 6 enemigos, los 4 jefes y Dante (PixelLab, Skeleton v3), en `Assets/Resources/EnemigosAnim`.
- **Los ataques siguen al dibujo** (`TowerEnemy.cs`, `FaseAtaque()`): la primera mitad de la animación es la carga (aviso, agacharse, apuntar) y la segunda es el golpe. Funciona en todos los tipos de ataque: embestida, pisotón, disparo, salto, giro y lluvia. Al recuperarse termina el gesto.
- **El enemigo mira hacia donde va a atacar**: antes podía mirar a un lado mientras la franja roja apuntaba a otro.
- **Larvas al revés hacia la izquierda**: el material de los sprites (`SpriteLit3D`) ignora `flipX`. Ahora el espejo se hace con la escala, en enemigos y en Dante.
- **El Espectro** apunta el bastón y lanza la esfera de magia de su dibujo (`Hechicero/Proyectil`). Cada tercer ataque hace caer golpes alrededor de Abby, y si te le pegas desaparece y reaparece lejos.
- Jefes con animación: el pisotón casi no despega del suelo, porque el golpe lo hace el dibujo.

## Abby
- **Usa sus sprites corregidos** (quieta, golpe y guardia con la espada en la mano derecha), no las animaciones viejas de respirar, espadazo y bloqueo (`AbbySpriteAnimator.posesCorregidas`).
- **Mismo tamaño en todo** (1.31 m sin la espada): el importador de PixelLab (`PixelLabImporter.cs`) forzaba 64 px por unidad y borraba el ajuste cada vez que Unity reimportaba. Ahora calcula el tamaño solo.
- **Correr a la izquierda sin espada**: el oeste ahora es el este espejado (como las diagonales). Al norte y al sur la carrera perdía la espada en la mitad de los cuadros, así que ahí corre con los cuadros de caminar.

## Cámara (`MouseOrbitCamera.cs`)
- **Ya no atraviesa el terreno**: si queda debajo de una loma, sube (solo cuenta el terreno, no techos ni libreros).
- **Ya no se pega a la cara de Abby** (la "frente gigante"): la cámara del juego no se acerca por lo que estorbe. La libre (F3) sí choca, pero nunca a menos de 2.5 m.
- **F3 marea menos**: giro más suave, menos adelanto y ya no choca con cada columna.

## Historia
- **Los textos ya caben**: la intro usa el mismo cuadro de historia del juego, que crece con el texto, y si es muy largo achica la letra (`CuadroNarracion.Cabe`).
- Ortografía del guion (comas, acentos, signos de pregunta).
- La imagen `03_escaleras` tenía una franja gris en el borde derecho.

## Mapas
- **Pisos de "día" pasan al atardecer de la historia** (`AmbienteHistoria.cs`): sol bajo naranja, cielo morado, bruma cálida, tejas y pasto más apagados y faroles encendidos. Sin regenerar escenas.
- **Cala Sombría (Exterior)**:
  - pueblo mucho más lleno (unas 30 casas, árboles y faroles a lo largo de la Calle Mayor);
  - el portal de la torre tenía un hueco negro triangular, y ahora tiene fondo de abismo;
  - **la bahía era pasto**: el terreno ponía el fondo del mar encima del agua. Ahora hay agua de verdad, con olas y espuma, y el plano del mar ya no está bajo el pueblo.
- `FloorExit.cs`: la salida vuelve a buscar a Abby si no la tenía (para que el portal siempre funcione).

- **Probado en Play:** Abby llega al portal y carga el Piso 1. Antes parecía no funcionar porque, con Unity minimizado, el juego no corre cuadros.
- El letrero decía "SOMBRíA", porque la fuente pixel no tiene la Í mayúscula; ahora va sin acentos.
- **Sombras de enemigos y Dante** (`SombraAlSol.cs`): antes eran una rayita que giraba con la cámara; ahora la sombra se proyecta de frente al sol.
- La salida del Acantilado (en la isla del faro, por el puente colgante) sí se alcanza.

## Pruebas hechas en Play (con Unity en segundo plano, `runInBackground`)
- **Combate en los 5 pisos y la biblioteca** (enemigos del piso + jefe alrededor de Abby, 30 s cada uno): **0 errores**. Los jefes (Guardián, Capitán, Torrente, Gárgola y Dante) atacan con su animación y sus avisos en el suelo.
- **Menú, Intro y Expedición** (60 s cada una): **0 errores**. Los textos de la intro caben completos.
- **Cala Sombría**: Abby entra a la torre y carga el Piso 1.
- Un jefe en un piso sin dibujo propio ya no sale como bloque 3D (usa el del Guardián).
- Los pisos solo usan los enemigos del GDD (Larva, Acechador, Bruto, Embestidor, Guardián, Espectro). Arquero y Torbellino, que no tienen sprite, no aparecen.

## Ejecutable de Windows
- **El juego compila como .exe** (`C:\Users\Gabriel\Unity\Build_AbbysTower\AbbysTower.exe`, unos 330 MB), con solo las 10 escenas de la ruta.
- **Arreglado:** en el ejecutable se apagaba todo el posproceso (bloom, color, viñeta), porque URP quitaba un shader que no se usaba (Panini) y entonces desactivaba todo. Ya no recorta los recursos de posproceso (`UniversalRenderPipelineGlobalSettings.asset`).

## Menú y guardado
- **Continuar partida** (`Progreso.cs`): al entrar a un piso se guarda dónde vas. El menú muestra "Continuar · Piso N" y "Nueva partida". Al vencer a Dante se borra.
- Con partida guardada, al abrir el juego ya no te manda directo al tutorial: sale el menú.
- El ejecutable y la ventana se llaman **"Abby's Tower"** (antes "GameProyectUT").
- **Build Settings** ordenadas: las 10 escenas de la ruta van primero y activas (Menu = 0). Las viejas siguen en la lista, pero apagadas.

## Unity
- **Disco lleno**: Unity 6000.6.4 repite sin parar el aviso "Layout update failed to stabilize" de su barra de herramientas. El log llegó a 900 MB y llenó el disco. Si vuelve a pasar, borra `%LOCALAPPDATA%\Unity\Editor\Editor.log` con Unity cerrado. Es un bug de Unity, no del juego. Agrandar la ventana de Unity suele calmarlo.
- Unity crasheó una vez por el plugin `AppUINativePlugin` (paquete `com.unity.ai.assistant` 2.20.0-pre.1, beta). Si sigue crasheando, quitar ese paquete en el Package Manager. **No mandes el reporte de bug** (adjunta todo el proyecto).

## Qué falta para publicarlo
1. **Jugarlo completo de principio a fin con control y teclado** (yo probé cada parte por separado, no la partida entera seguida): que cada salida lleve al siguiente piso y que el final con Dante regrese al menú.
2. ~~Escenas del build~~ (hecho). ~~Guardado~~ (hecho: Continuar). ~~Nombre del ejecutable~~ (hecho). El menú ya tenía Configuración (volúmenes y pantalla completa) y Créditos.
3. **Revisar el texto de los créditos** (que estén todos los del equipo, la música y PixelLab).
4. **Ícono del juego** en *Player Settings* (por ahora usa el de Unity).
5. Quitar el paquete beta `com.unity.ai.assistant` si sigue crasheando Unity.

## IMPORTANTE al despertar
- **Sube los últimos commits**: el push se quedó esperando que inicies sesión en GitHub (Git Credential Manager). Abre Git Bash en el proyecto y escribe `git push`.

## Pendiente o por revisar
- La imagen 12 (despedida) muestra una torre en ruinas, no la torre morada: rehacerla si se quiere que combine.
- Herramientas temporales en `Assets/Editor` (`PruebaAnim`, `TourExterior`, `CamaraRemota`): no se suben y hay que borrarlas antes de entregar.
