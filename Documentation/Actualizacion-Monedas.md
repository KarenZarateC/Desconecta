# Actualización de monedas de Desconecta

Esta actualización incorpora recolección de monedas al recorrido de EcoHogar para incentivar la exploración de la casa. Incluye 24 monedas 3D, contador durante la partida, estadísticas al finalizar y los audios suministrados en la carpeta Camilo. Las siete decisiones sobre agua, energía y gas conservan su papel como objetivo principal.

Rama: `KarenZarateC/actualizacion-monedas`. Base: `codex/etapa3-completada`, commit `0ee1cbe`. Escena: `Assets/Level/Scenes/Desconecta_Stage3.unity`. Editor: Unity 6000.3.17f1 LTS, URP.

## Cambios para quien juega

| Elemento | Comportamiento |
|---|---|
| Monedas en el mapa | 24 unidades distribuidas en corredores, cocina, baño, habitación y sala, con espacio para el controlador del jugador |
| Modelo 3D | Disco de 64 segmentos, borde biselado, anillo interior y marca vertical en ambas caras; materiales dorados |
| Animación continua | Giro de 360 grados cada 2 segundos y flotación suave |
| Recogida | Automática al tocar el área de la moneda; ascenso, giro, reducción y destellos durante 0,48 segundos |
| Puntuación | +10 por moneda, sin duplicados; +100 por situación correcta y −25 por error según las reglas existentes |
| Contador de partida | MONEDAS recogidas / total en la barra superior; el puntaje incluye la bonificación |
| Estadísticas finales | Monedas recogidas / total, porcentaje, puntos de bonificación y puntaje total, al ganar y al agotar el tiempo |
| Nueva partida | Contador y bonificación en cero; todas las monedas y sus áreas de recogida se restauran |

Por ejemplo, resolver las siete situaciones y recoger 18 de 24 monedas sin errores muestra **75% de recolección**, **+180 puntos por monedas** y **880 puntos totales**. Recoger todas las monedas añade 240 puntos; junto con las siete situaciones, el máximo sin errores es 940. Recolectar monedas no termina la partida: todavía deben resolverse las situaciones.

La recogida está bloqueada en el menú, la pausa, las decisiones y los resultados. Una pared impide recoger una moneda del otro lado. La animación se detiene en pausa y se sustituye por retroalimentación inmediata en movimiento reducido. El sonido de recogida también se describe en texto.

## Audio y referencia visual

La referencia visual fue la imagen de moneda adjunta por la usuaria, `Coin_MK8_artwork.webp`. La malla se construyó con geometría del proyecto; la imagen sirve como referencia visual y no se utiliza como textura ni se incorpora al repositorio.

Los archivos originales `Coin.mpeg` y `Musica de fondo.mpeg` de `Assets/Audio/Camilo` contienen audio MP3. Se conservó su contenido y se corrigió su extensión a `.mp3`, para que Unity utilice `AudioImporter` en lugar de intentar leerlos como video. La música se reproduce en bucle con carga por streaming; el efecto se importa en mono y se reproduce al recoger una moneda. Música y efectos respetan sus respectivos controles de volumen y el silencio general.

Los audios fueron proporcionados por la usuaria. El nombre de la carpeta no acredita por sí solo autoría ni licencia. El equipo debe conservar los créditos y permisos que correspondan antes de una distribución pública. La programación, el modelo procedural, la integración y esta documentación se realizaron con asistencia de Codex.

## Organización para desarrollo

| Archivo o carpeta | Responsabilidad |
|---|---|
| `Assets/Code/Scripts/CoinCollectionProgress.cs` | Registro de identificadores válidos, recogidas únicas, total y bonificación |
| `Assets/Code/Scripts/CollectibleCoin.cs` | Contacto del jugador, bloqueo por obstáculos, animaciones, partículas y reinicio |
| `Assets/Code/Scripts/GameSession.cs` | Coordinación de monedas y estados; puntaje total y anuncio de sonido |
| `Assets/Code/Scripts/GameInterface.cs` | Contador del HUD y estadísticas finales |
| `Assets/Art/Coins` | Malla guardada, materiales y clips `CoinSpin.anim` y `CoinCollect.anim` |
| `Assets/Level/Prefabs/Collectibles/GoldCoin.prefab` | Moneda reutilizable con collider trigger, Rigidbody cinemático, Animation y partículas |
| `Assets/Code/Editor/CoinUpdateBuilder.cs` | Generación de recursos y colocación en posiciones alcanzables desde el inicio |
| `Assets/Code/Editor/CoinUpdateVerification.cs` | Verificación de integración y ejecución de pruebas desde el editor |
| `Assets/Tests/Editor/CoinCollectionProgressTests.cs` | Pruebas de puntuación, duplicados, identificadores y reinicio |

Para añadir monedas manualmente, coloca instancias de `GoldCoin.prefab` a la altura del jugador, asigna un `coinId` único y agrega cada instancia al arreglo `coins` de `DesconectaSession`. El contador toma su total del registro y no exige escribir 24 en el código. El número 24 pertenece a la distribución inicial y a sus comprobaciones de entrega. Guarda la escena después de editar.

**Desconecta > Coins > Apply Coin Update** vuelve a crear las 24 instancias generadas y sus recursos; reemplaza los cambios manuales en esas monedas. Guarda esos ajustes en Git antes de reconstruir. El constructor general **Build Stage Three** también incorpora la actualización para que las monedas y los audios se conserven al regenerar la escena.

## Probar en Unity

1. Cambia a la rama de actualización y ejecuta `git lfs pull`.
2. Abre el proyecto en Unity 6000.3.17f1 y la escena `Desconecta_Stage3`.
3. Pulsa Play y comienza el recorrido. Camina hacia una moneda: debe desaparecer después de la animación, sonar una vez y sumar una unidad y 10 puntos.
4. Pausa y reanuda: no debe haber recogida ni giro durante la pausa.
5. Termina las siete situaciones y revisa las estadísticas. Prueba también una partida que termine por tiempo.
6. Usa Jugar otra vez: el contador debe volver a cero y las 24 monedas deben reaparecer.
7. En movimiento reducido, la moneda se retira inmediatamente al recogerla; en silencio, el texto y el contador siguen mostrando el resultado.

Las pruebas automáticas se ejecutan con **Desconecta > Coins > Run EditMode Tests** fuera de Play Mode. Para la integración, entra en Play Mode y ejecuta **Desconecta > Coins > Run Runtime Verification**. Esta comprobación modifica temporalmente la partida para probar recogida, reinicio, pausa, victoria y tiempo agotado; restaura las preferencias y termina en el menú inicial.

Se aprobaron **11 pruebas EditMode** (6 de misiones y 5 de monedas) y **28 comprobaciones de integración** en Play Mode. Los resultados reproducibles quedan en [CoinTests.xml](CoinTests.xml) y [CoinValidation.json](CoinValidation.json). Las capturas de [partida](Screenshots/Coin-Playing.png) y [resultados](Screenshots/Coin-Results.png) muestran la presentación final. Las estadísticas pertenecen a la partida actual; no se almacena un historial entre sesiones.
