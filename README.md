# Desconecta

Desconecta es un videojuego educativo en 3D desarrollado en Unity dirigido a niños de 8 a 12 años, cuyo propósito es promover el uso responsable del agua, la energía eléctrica y el gas dentro del hogar, la experiencia se desarrolla en una vivienda que el jugador debe explorar para identificar situaciones cotidianas de desperdicio y reconocer las acciones adecuadas para solucionarlas.  Basado en el [GDD EcoHogar](https://miro.com/app/board/uXjVHnfIILI=/).

![Inicio de Desconecta](Documentation/Screenshots/Captura-Inicio.png)

## Abrir el proyecto

1. Instala Unity **6000.3.17f1 LTS** con el módulo Windows Build Support (Mono).
2. Clona este repositorio y descarga sus archivos grandes:

   ```bash
   git lfs install
   git lfs pull
   ```

3. Abre la carpeta en Unity Hub y espera a que termine la importación.
4. Abre **Assets/Level/Scenes/Desconecta_Stage3.unity** y pulsa Play.

La escena habilitada para compilar es Desconecta_Stage3. `Nivel1_Recovered.unity` conserva la casa recuperada del historial. Las escenas anteriores no son el punto de entrada de esta entrega.

## Reglas y controles

Hay diez minutos, con modo opcional sin límite. Cada acierto suma 100 puntos y cada error resta 25 sin bajar de cero. Se puede reintentar; cada situación puntúa una sola vez. El reloj se detiene al leer una decisión y en la pausa.

| Acción | Control |
|---|---|
| Moverse | Flechas o WASD |
| Mirar | Mouse; Q/E para girar y R/F para mirar arriba/abajo |
| Revisar el objetivo señalado | Clic, Enter o Espacio |
| Elegir respuesta | 1 o 2, o los botones de la pantalla |
| Pausa / volver a explorar | Esc |
| Pedir una pista | H |
| Navegar por menús | Tab o flechas; Enter activa; flechas ajustan deslizadores |

Los siete objetivos son: grifo de cocina, grifo del baño, luz de la sala, televisor, cargador, lámpara y estufa. Con el cargador y la estufa se elige pedir ayuda a un adulto.

## Accesibilidad y sonido

El menú **A tu ritmo** incluye texto grande, movimiento reducido, tiempo ilimitado, silencio, volúmenes independientes y sensibilidad del mouse. Se guardan entre sesiones. Los estados usan números y palabras; los efectos sonoros tienen descripción escrita. Los controles aceptan tanto Input System como eventos heredados; mantener **Active Input Handling: Both**.

El informe describe límites: no hay lector de pantalla, locución, mando ni remapeo completo. Se usan WCAG 2.2 y DUA como referencia, sin declarar certificación de conformidad integral.

## Estructura

- `Assets/Code/Scripts`: lógica de juego y UI, con identificadores y comentarios en inglés.
- `Assets/Code/Editor`: constructor de escena, comprobaciones y conexión local MCP.
- `Assets/Level/Scenes`: escena jugable y copia recuperada de la casa.
- `Assets/Art/StageThree`: materiales y la ilustración generada.
- `Assets/Audio/Generated`: música y efectos sintetizados originales.
- `Assets/Tests/Editor`: seis pruebas NUnit de las reglas.
- `Documentation`: informes PDF, declaración de IA, validación y capturas.
- `Tools/create_audio.py`: síntesis reproducible de los ocho WAV.
- `Packages/com.coplaydev.unity-mcp`: MCP for Unity 10.3.0, con licencia MIT.

## Verificación y compilación

En **Window > General > Test Runner**, ejecuta EditMode, ensamblado `Desconecta.Tests`. La entrega registra seis pruebas aprobadas. En Play Mode, `StageThreeVerification.VerifyRuntime()` verifica diez condiciones y el acceso físico a los siete objetivos; deja la sesión en el menú inicial al terminar.

Para generar Windows x64: abre **File > Build Profiles**, selecciona Windows y usa la única escena habilitada. Backend Mono, ventana de 1600 x 1000. No se necesita servidor MCP para jugar. La copia compilada se entrega por separado del código fuente.

**Desconecta > Build Stage Three** reconstruye la escena generada desde `Nivel1_Recovered.unity`; sobrescribe los ajustes manuales posteriores de Desconecta_Stage3. Guarda o confirma esos cambios en Git antes de reconstruir.

## Desarrollo mediante MCP

Se verificó una conexión MCP HTTP exclusivamente local en `http://127.0.0.1:8080/mcp`, con el servidor oficial de [CoplayDev](https://github.com/CoplayDev/unity-mcp), revisión `81542a7e86292b5c4c21b1782c5af23b567f45ce`. El paquete del editor se incluye en el proyecto. Para otra sesión, inicia el servidor según su documentación y usa **Desconecta > Connect Local MCP**. El proyecto no inicia un servidor automáticamente ni exige una cuenta en la nube.

## Procedencia, IA y equipo

La estructura de la vivienda y los muebles proceden del proyecto existente. La ilustración se generó con la herramienta de imágenes de OpenAI integrada en Codex; el prompt original en inglés y el posprocesamiento están en `Documentation/AI-Usage.json`. La versión exacta del modelo de imagen no fue expuesta. El audio se sintetizó mediante Python, sin grabaciones ni canciones descargadas. `Switch.wav` queda como recurso disponible; los otros siete clips se integraron en la escena.

Codex asistió en recuperación, programación, automatización, pruebas y documentos. Los commits nuevos declaran esa asistencia. El historial anterior registra aportes bajo el nombre Karen Zarate; **los nombres completos, aportes reales, roles del equipo y responsable de entrega requieren confirmación de sus integrantes**. No se crearon identidades ni commits ficticios para aparentar colaboración.

Se excluyeron por petición del usuario la sesión de crítica, el video y su publicación. Los retos extra son opcionales. La revisión en Turnitin y la entrega en el campus no se realizaron desde este entorno.
