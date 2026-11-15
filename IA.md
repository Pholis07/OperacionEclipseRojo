# Uso de IA en este proyecto

Este proyecto se construyó con ayuda de Claude Code (modelo Claude Sonnet 5.5, de Anthropic). Este archivo lista qué hizo la IA en cada fase de construcción. Las decisiones de diseño y la revisión son del equipo (Adrian y Diego).

Antes de la construcción, la IA ayudó con los documentos de diseño (plan, decisiones, plan de pruebas y textos de transmisiones) y con la lista de fuentes de NASA. Eso está registrado en el repositorio de documentos, fuera de esta carpeta.

## Fase 0: verificación previa

- Revisó que el proyecto existe, que es Unity 6000.6.9f1 con WebGL Build Support y que el editor estaba cerrado.
- No cambió nada.

## Fase 1: proyecto y estructura

- Creó el repositorio git local (con `core.fileMode false` por estar en NTFS) y el `.gitignore` de Unity.
- Creó las carpetas, las dos assembly definitions (`SpaceMission` y `SpaceMission.Tests`) y la clase `EstadoJuego`, con una prueba mínima.
- Creó las tres escenas vacías (`Menu`, `Mision`, `Resultado`), las agregó al Build Settings y ajustó WebGL (Gzip, Decompression Fallback, 1920x1080). Lo hizo con un script temporal en modo batch, que borró después.
- Escribió `README.md` e `IA.md`.

## Fase 2: datos

- Escribió los ScriptableObjects (`OrbitaDatos`, `LanzadorDatos`, `InstrumentoDatos`, `MisionDatos`, `TransmisionDatos`, `CatalogoDatos`) y los tipos `CategoriaResultado` y `TipoTransmision`.
- Creó los assets de `Assets/Datos` con los valores de las tablas del diseño, con un script temporal en modo batch que borró después. La primera versión del script guardaba los assets vacíos; las pruebas lo detectaron y se corrigió el script, no las pruebas.
- Redactó textos que las tablas no traían: la evidencia que aporta cada instrumento, su texto de ayuda y la descripción breve de cada órbita. Están pendientes de revisión de Diego.
- Escribió 22 pruebas EditMode de datos (ids, listas de las órbitas, valores, desglose del consumo en la ayuda, categorías de resultado y que no se guarde el semáforo).

## Fase 3: cálculo y semáforo

- Escribió primero las pruebas con los 19 casos de la tabla del diseño, el borde de los límites exactos (700 W no bloquea), el cambio de lanzador y el formato de la línea de registro.
- Escribió `Configuracion`, `Calculador` y `ResultadoCalculo`: masa, potencia, costo, espacios, límites superados y estado Gris, Rojo o Válida. Los números salen de los assets.
- Cambió `EstadoJuego` para que guarde una `Configuracion`.
- Las 47 pruebas pasaron en el primer intento.

## Fase 4: evaluación y transmisiones

- Creó los 11 assets de transmisiones. Los textos se extrajeron con un script de `Transmisiones.md` (el documento de diseño del equipo) y se comprobó que quedaron idénticos; la IA no los reescribió.
- Escribió primero las pruebas con los 19 casos de la tabla (semáforo, resultado, transmisiones en orden y ausentes) y después `Evaluador` y `Evaluacion`.
- Redactó los mensajes del semáforo (Gris, Rojo, Amarillo con «fracaso seguro», Verde), con las palabras de las evidencias. Pendientes de revisión del equipo.
- Movió la línea de registro `[Misión]` al Evaluador, que ya conoce el semáforo final y el resultado.
- Las 72 pruebas pasaron en el primer intento.

## Fase 5: menú y Briefing

- Escribió `MenuUI`, `MisionUI` y `TextosBriefing`. Los requisitos del Briefing se copiaron del diseño (decisión 13) y los límites se leen del catálogo.
- Escribió `Assets/Editor/ConstructorEscenas.cs`, que arma la interfaz de las escenas por código (menú Herramientas > Construir escenas). Eligió la paleta (fondo oscuro, acento rojo marciano) a partir de las recomendaciones de la skill ui-ux-pro-max.
- Redactó los textos de «Cómo jugar», la explicación de los colores del semáforo y las etiquetas de la pantalla. Pendientes de revisión del equipo.
- Importó los recursos esenciales de TextMeshPro, que traen la fuente Liberation Sans (licencia SIL Open Font License).
- Escribió 6 pruebas de interfaz (la parte automática de PM1 e integridad de las escenas). Una prueba de la fase 2 detectó un campo con «semáforo» en el nombre; se renombró el campo, no se cambió la prueba. Pasan las 78 pruebas.

## Fase 6: configuración y revisión

- Escribió `ConfiguracionUI` (pestañas, opciones generadas desde los datos, ayuda contextual, cuatro barras y semáforo), `RevisionUI` («Corregir» y «Lanzar», bloqueado en Gris y Rojo), `NaveUI` y `PresentacionMision` (textos de barras, colores y resumen).
- Amplió el constructor de escenas con estas vistas y una nave 3D hecha con primitivas de Unity (cuerpo, dos paneles y un módulo por instrumento) con tres materiales propios.
- Escribió 21 pruebas: los casos 1 a 7 y B1 a B8 con los valores y colores de la tabla, el recorrido en la escena y el bloqueo de «Lanzar».
- Fallos encontrados: otro nombre de campo con «semáforo» (se renombró el campo) y una expectativa mal escrita por la propia IA en la prueba de B8 (faltaba la unidad «kg»; se corrigió la expectativa porque el código era coherente con las demás barras). Pasan las 99 pruebas.

## Fase 7: investigación y resultado

- Escribió `InvestigacionUI` (transmisiones de una en una con «Continuar investigación»), `InformeMision` (conclusión, evidencias por instrumento, revelación complementaria, lo que faltó y recursos usados), `ResultadoUI` y la vuelta a Configuración con «Modificar mi misión».
- Los títulos y textos de las tres conclusiones se extrajeron con un script del diseño del equipo, sin reescribirlos; se añadió el punto final de la frase, que en el documento va fuera de las comillas.
- Amplió el constructor con la vista Investigación y la escena Resultado. Dejó de reconstruir la escena Menu, que pasa a editarse a mano (ver `GUIA-INTERFAZ.md`, que también escribió).
- Escribió 13 pruebas: transmisiones de los casos 1 a 6 en orden, caso 7 sin Investigación, F1, PM2, escena Resultado sin «Nueva misión» ni puntos, y «Modificar mi misión». Pasan las 112 pruebas en el primer intento.

## Fase 8: pruebas finales

- Escribió pruebas PlayMode que recorren el juego en modo Play pulsando los botones reales para los 19 casos de la tabla; los casos se generaron con un script desde la tabla del diseño.
- Corrió las pruebas en una copia del proyecto, porque el editor estaba abierto con el trabajo de Diego: 112 EditMode y 19 PlayMode, todas pasan.
- Generó `INFORME-PRUEBAS.md` desde los resultados reales (líneas `[Misión]` obtenidas en el juego). Queda pendiente la revisión visual, que hace el equipo.

## Gameplay nuevo, paso 1: lógica de la Operación (pedido de Adrian, 2026-11-14)

- Concretó en reglas y números el pedido de Adrian (escaneo, eventos con herramientas, límite de energía, apagón que cuesta tiempo y recursos, puntos y niveles) en `PROPUESTA-GAMEPLAY.md`; Adrian aprobó la versión 1 y eligió 1,150 créditos para el nivel 3. Los números los propuso la IA.
- Cambió dos eventos de su propia propuesta que no eran plausibles para una nave en órbita.
- Escribió la lógica pura `Operacion` y `Puntuacion`, los datos (reglas, 3 niveles, 5 eventos, 4 herramientas) y 23 pruebas. El cálculo y la evaluación aceptan ahora el nivel; sin nivel usan la misión del diseño, así que las pruebas anteriores no cambiaron.
- Por estar el editor abierto con el trabajo de Diego, generó los datos y corrió las pruebas en una copia del proyecto y copió los archivos resultantes. Pasan 135 pruebas.

## Gameplay nuevo, paso 2: interfaz de la Operación, niveles y puntos

- Escribió `OperacionUI` (instrumentos con interruptor, energía, órbitas, mapa para escanear, eventos y herramientas) y `MapaMarteUI` (mapa de Marte generado por código, sin imágenes externas). Sustituyen a la Investigación.
- Añadió el selector de nivel en el Briefing y los puntos con su desglose y el mejor puntaje por nivel en el informe; el informe marca los datos de baja calidad.
- Adaptó las pruebas al diseño aprobado: las transmisiones ya no llegan en orden fijo sino al encontrar la anomalía, y la escena Resultado sí muestra puntos (se revirtió la decisión 7). La prueba PlayMode juega la Operación con eventos aleatorios reales y herramientas. Pasan 136 EditMode y 19 PlayMode.

## Estilo HUD de la interfaz (con Diego)

- A partir de cuatro referencias que eligió Diego (Starfield, una tabla de acciones de misión espacial y Vehicle Designer), rediseñó el Menú y reestilizó Mision y Resultado sin cambiar su lógica ni sus pruebas.
- Menú: generado con «Herramientas > Diseñar Menu estilo HUD» como punto de partida para que Diego lo retoque a mano. Tiene la lista de acciones a la izquierda, la nave girando sobre Marte (script `GiroLento`) y las pistas de teclas. El foco del teclado empieza en «Iniciar misión», y `MenuUI` lo mueve al abrir y cerrar «Cómo jugar».
- Constructor: piezas HUD nuevas (marco fino con escuadra de acento, etiquetas subrayadas, botones secundarios con marco), barra de pasos en el Briefing y «PASO n DE 4» en las demás vistas, paneles translúcidos y suelo con rejilla bajo la nave. La textura de la rejilla se genera por código.
- Revisó el resultado con capturas generadas en modo batch con un script temporal, que borró después. Las capturas mostraron tres fallos, que corrigió: la nave tapada por los paneles (se redujo en Mision), la pestaña «Instrumentos» partida en dos líneas (tamaño automático de 30 a 24 pt) y la luz del semáforo estirada (las filas de alto fijo ya no crecen).
- Actualizó `GUIA-INTERFAZ.md`. Pasan las 112 pruebas.

## Fase de arte: fuentes, fotos de NASA, nave, movimiento y audio preparado (con Diego)

- Diego eligió la dirección (referencias de Starfield y Vehicle Designer, fotos reales de NASA, audio con ElevenLabs) y pidió mejorar la nave, los fondos, la tipografía y el movimiento.
- Descargó imágenes de la NASA Image and Video Library y las procesó con `Herramientas/arte.py`:
  - Rellenó los huecos del mapa global de Marte.
  - Aplicó una paleta térmica a la tira de THEMIS.
  - Oscureció y entintó los fondos.
  - Los créditos están en `CREDITOS.md` y en pantalla.
- Añadió fuentes OFL (Rajdhani, Barlow e IBM Plex Mono) e iconos de Lucide (ISC) como fuentes TextMeshPro de atlas fijo (`ConstructorEstilo`).
- Rehízo el constructor de escenas sin cambiar la lógica:
  - Botones con chaflán, opciones como tarjetas con iconos y datos, y barras segmentadas en cian.
  - Semáforo redondo con halo.
  - Fondos con foto, miniatura de la región en el Briefing e imagen real de cada instrumento en la Investigación.
  - Estrellas, Marte texturizado con atmósfera (shader propio) y anillo de la órbita elegida.
  - Nave nueva de primitivas y mallas propias, con materiales PBR, luces y posprocesado.
- Añadió animaciones que solo cambian alfa, escala o posición, para que las pruebas PlayMode sigan pulsando los botones al instante:
  - Entrada de vistas y respuesta de los botones.
  - Barras, latido del semáforo e instalación de módulos.
  - Máquina de escribir, destello de lanzamiento y pantalla de título.
- Añadió Opciones (volúmenes y «Reducir movimiento») y dejó listo el audio. Los archivos y las indicaciones para ElevenLabs están en `AUDIO-ELEVENLABS.md` y faltan por generar.
- Como el editor de Diego estaba abierto, construyó, capturó y probó en una copia del proyecto, y trajo las escenas y los recursos generados.
- Fallos encontrados y corregidos:
  - `.meta` mínimos que Unity importó como Cubemap.
  - Una prueba que exige el texto exacto «Modificar mi misión».
  - El panel de Opciones visible en la escena.
  - La caja de ayuda desbordada.
- Pasan las 112 pruebas EditMode y las 19 PlayMode.
- Al subirlo, integró el gameplay nuevo de Adrian (Operación, niveles y puntos), que se había subido mientras tanto. Restyló la vista de Operación con el mismo estilo: barras segmentadas, mapa con marco HUD e imagen real del instrumento de la última señal (`VisorEvidencia`, ahora lee `OperacionUI`). También restyló el selector de nivel del Briefing y los puntos del Resultado. Borró el marcador azul del rover que traía la foto de Marte. Pasan las 136 pruebas EditMode y las 19 PlayMode.

## Revisión del push de Diego y arreglos para la publicación (2026-11-14)

- Revisó los 3 commits de Diego (interfaz HUD, arte y fusión con el gameplay): sin secretos, licencias documentadas, 136 + 19 pruebas en verde, build WebGL correcto y juego jugable en el navegador (lo comprobó con capturas en Chromium).
- Arreglos que hizo después de la revisión: plantilla web propia a pantalla completa (la de Unity fijaba 1920x1080 y se cortaba en pantallas pequeñas), retiro del paquete Sentis (rompía el build WebGL y no se usa), resolución de render al 100 % en WebGL (con 0.8 se activaba un escalado que desactivaba el postprocesado), mapa de la Operación con la foto de Marte de NASA que ya estaba en el proyecto, y el pie de «Vista de la nave», que se salía de su caja.
- El disco donde estaba el proyecto quedó marcado como sucio tras un reinicio; lo montó en solo lectura y copió el proyecto a `~/Projects/OperacionEclipseRojo` antes de comitear.
- Corrigió el mapa de la Operación: recortaba una zona que no era cuadrada en píxeles (la foto es 2:1) y se estiraba; ahora muestra 512x512 píxeles de la foto, casi 1:1. Generó un favicon propio con `Herramientas/favicon.py` (Marte con la sombra de un eclipse) y lo enlazó en la plantilla web. Verificó el build a 1366x768 en Chromium: no se corta nada y la consola no tiene errores.

## Fase 9: build y publicación

- Creó `LICENSE` con la licencia MIT, que eligió el equipo, y completó `CREDITOS.md` con la licencia del proyecto y la nota del runtime de Unity.
- Escribió `PUBLICAR.md` y `Herramientas/preparar-pages.sh`, que prepara la rama local `gh-pages` con el build sin hacer push. Lo probó: la rama local quedó con el build y no se publicó nada.
- Actualizó el README con cómo se juega, las pruebas, la publicación, los documentos y la licencia.

## Cohetes, zoom y navegación de Configuración (con Diego)

- Diego pidió cohetes basados en referencias reales, acercamiento al planeta al elegir la órbita y a la nave al instalar instrumentos, mejores texturas y mejor navegación. También pidió contener los brillos del semáforo.
- Construyó dos cohetes con mallas de revolución: el Explorador (inspirado en el Soyuz) y el Titán (inspirado en el SLS de Artemis II, con Orion y torre de escape), a escala aproximada entre ellos y sin logotipos.
- Añadió `CamaraConfiguracion`, que encuadra el planeta, la plataforma o la nave según la pestaña. También se acerca al módulo de cada instrumento instalado, con una etiqueta flotante.
- Añadió `GuiaConfiguracion`: ✓ por pestaña completada, botón «Siguiente», instrucción por paso, leyenda según la pestaña y teclas 1, 2 y 3.
- Mejoró Marte:
  - Mapa de Solar System Scope (CC BY 4.0) con relieve calculado y una capa de polvo semitransparente con la tormenta real de 2018.
  - Esfera de alta resolución.
  - Marcador de la nave recorriendo el anillo de órbita.
- Bajó el halo del semáforo y el Bloom.
- Fallos encontrados con las capturas y corregidos: la textura en espejo y los triángulos de la esfera invertidos (se veía la cara interior). También la órbita elíptica, que entraba en el planeta, y el Sol demasiado de lado.
- El audio sigue pendiente: las herramientas de ElevenLabs no se cargaron en esta sesión.
- Pasan las 136 pruebas EditMode y las 19 PlayMode.

## Audio con ElevenLabs (con Diego)

- Diego conectó su cuenta de ElevenLabs (plan Starter). La IA comprobó los créditos, calculó el costo de cada audio antes de generarlo y armó un flow con todo el audio del juego. Generó una sola toma de cada audio para no pasarse del presupuesto: unos 6,700 de 18,600 créditos.
- Generó:
  - Música con Eleven Music: menú, configuración y operación (60 s en bucle) y cierres de victoria, parcial y fracaso (12 s).
  - 11 efectos con Sound Effects, entre ellos el ambiente de la tormenta en bucle.
  - Las voces de TX-00 a TX-05 con la voz de biblioteca «César Augusto» y un filtro de radio gratuito (Audio Effects). Los textos son exactamente los de los assets de transmisión.
- Añadió ajustes de importación de audio para WebGL (Vorbis; efectos en mono y descomprimidos al cargar) y enlazó todo en `Arte.asset` reconstruyendo las escenas.
- Encontró que en Linux la importación de audio para WebGL necesita `ffmpeg` instalado; sin él los clips fallan y el juego queda en silencio. Lo anotó en `AUDIO-ELEVENLABS.md`.
- Pasan las 136 pruebas EditMode y las 19 PlayMode.

## Gameplay versión 3, paso 1: datos y Carga de la nave

- Adrian pidió misiones más interactivas. La IA propuso cuatro mecánicas (eventos con decisiones, combustible y maniobras, memoria y envío a Tierra, herramientas y objetivos) con números concretos en `PROPUESTA-GAMEPLAY.md` (versión 3), y Adrian la aprobó tal cual.
- Paso 1: datos nuevos y lógica de la Carga.
  - Herramientas con costo y masa, bodega de 2, combustible (1 por cada 10 kg libres, máximo 20), memoria por nivel (4, 3 y 3), respuestas de cada evento y 5 objetivos secundarios.
  - Los escribe `Assets/Editor/DatosVersion3.cs`, que se puede volver a correr.
  - Lógica nueva: `Carga.cs` y `Objetivos.cs`. `Operacion` recibe la carga y solo deja usar las herramientas que subieron; `Puntuacion` resta del sobrante lo gastado en la carga.
  - Sin la pantalla de Carga (llega en el paso 6), la Operación sigue como antes, con todas las herramientas a bordo.
- La categoría del resultado no cambia: la carga se calcula después de evaluar y solo usa lo que sobra.
- 18 pruebas nuevas en `CargaTests.cs`. Pasan las 154 pruebas EditMode y las 19 PlayMode.

## Gameplay versión 3, paso 2: combustible y altitud

- Hay dos altitudes, normal y baja. Cambiar cuesta 3 de combustible. En baja, las lecturas alcanzan 1.5 veces más lejos y el arrastre gasta 1 de combustible por órbita. Si no queda, la nave sube sola y se pierde 1 órbita más.
- Cada unidad de combustible sobrante da +10 puntos. Los números están en `ReglasOperacion.asset` y los escribe `DatosVersion3.cs`.
- Sin pantalla de Carga (hasta el paso 6), la nave sale con 0 de combustible y no puede bajar, así que el juego actual no cambia.
- 8 pruebas nuevas en `AltitudTests.cs`. Pasan las 162 pruebas EditMode y las 19 PlayMode.

## Gameplay versión 3, paso 3: memoria, ventanas de contacto y envío a Tierra

- Cada escaneo ocupa 1 bloque de memoria (4, 3 y 3 según el nivel). Con la memoria llena no se puede escanear, y el intento no gasta órbita.
- Lo encontrado queda «en memoria» y solo cuenta como recibido al enviarlo.
- La ventana de contacto con la Tierra se abre 1 órbita de cada 3 (órbitas 2, 5, 8…). `AbrirContacto()` abre una extra en la órbita actual, para la ventana despejada del paso 4.
- Enviar no gasta órbita, pero el transmisor suma 150 W. Si no caben, la nave se apaga y lo de la memoria no se pierde.
- Lo que siga en memoria al terminar llega con baja calidad.
- Como en los pasos anteriores, la memoria solo existe con Carga. Sin ella (el juego actual hasta el paso 6), lo encontrado llega al momento.
- 9 pruebas nuevas en `MemoriaTests.cs`. Pasan las 171 pruebas EditMode y las 19 PlayMode.

## Gameplay versión 3, paso 4: eventos con decisiones y dos eventos nuevos

- Con Carga, cada evento pausa la Operación: no se puede escanear, enviar, cambiar de altitud ni usar herramientas hasta que el jugador elige con `Decidir`. El efecto no se aplica hasta la decisión.
- Las respuestas son las de la tabla de la propuesta. La de herramienta solo aparece si la herramienta subió a la nave. Las que piden combustible no se pueden elegir si no alcanza.
- Eventos nuevos:
  - Petición de la NASA: si se acepta, hay que escanear un punto marcado y da +150 puntos.
  - Tormenta creciendo: se puede bajar de altitud gratis o seguir; si se sigue, el próximo escaneo frío se lee como tibio.
- Sin Carga (el juego actual hasta el paso 6), los eventos se aplican al momento como antes y los dos nuevos no salen al azar.
- La prueba de la v2 que contaba 5 eventos ahora cuenta 7, porque la propuesta aprobada añade dos. Las pruebas de memoria y carga que usaban eventos ahora eligen una respuesta.
- 14 pruebas nuevas en `DecisionesTests.cs`. Pasan las 185 pruebas EditMode y las 19 PlayMode.

## Gameplay versión 3, paso 5: objetivos y puntos

- Con Carga, cada misión saca 2 objetivos al azar. Sin Carga no hay objetivos, así que los puntos del modo anterior no cambian.
- En el desglose de puntos:
  - Cada objetivo cumplido suma sus puntos como «Objetivo: …».
  - Ya estaban, de los pasos 2 y 4: el combustible sobrante (+10 por unidad) y la petición de la NASA cumplida (+150).
- El informe de recursos dice cuánto costó la carga, cuánto combustible quedó, si se cumplió la petición de la NASA y qué objetivos se cumplieron. Lo arma `Objetivos.Resumen`.
- 11 pruebas nuevas en `ObjetivosTests.cs`. Pasan las 196 pruebas EditMode y las 19 PlayMode.

## Cinemáticas, investigación orbital, satélites de NASA y segunda tanda de audio (con Diego)

- Diego pasó el paquete de especificaciones del equipo:
  - Prompt maestro, storyboards de lanzamiento e investigación, menú, órbitas, arquitectura, biblia sonora, CSV de audio, QA y el kit de instrumentos.
  - Siete enlaces de NASA 3D Resources, para que el jugador elija su tipo de satélite (solo eso; Adrian pondrá las mecánicas).
- La IA:
  - Auditó el proyecto real antes de editar y trabajó en la rama `feat/eclipse-cinematics-audio`.
  - Reutilizó lo que ya existía (NaveUI, AnilloOrbita, audio) en lugar de los scripts del kit.
  - Anotó las discrepancias de datos con el documento maestro sin corregirlas (Titán 750/1,100 kg, espectrómetro 260/200 W).
- Convirtió los modelos de NASA (GLB con Draco y STL) a OBJ con una herramienta propia en Node (`Herramientas/modelos_nasa`), sin añadir paquetes a Unity. Pintó el SLS por zonas y lo partió en dos etapas.
- Implementó:
  - Pestaña «Satélite» con 5 modelos e instrumentos montados en el casco por rayos contra la malla.
  - El Titán con el SLS de NASA.
  - Menú «Centro de control en alerta» con Créditos.
  - Cinemática de 8 tomas (~26 s) con «Omitir».
  - Investigación orbital en 3D tras cada escaneo, con regreso exacto al encuadre.
  - Ambiente, volumen de ambiente y «Silenciar todo».
- Generó en ElevenLabs 16 audios del CSV, con una toma cada uno y 624 créditos. Los midió y normalizó con ffmpeg; nadie los ha escuchado a oído todavía.
- Probó la cinemática y los escaneos con una prueba PlayMode temporal que guarda fotogramas. Con ellos corrigió:
  - un domo de estrellas que se veía en el despegue,
  - el haz opaco,
  - la sombra gigante del satélite,
  - la atmósfera facetada,
  - el encuadre lejano.
- Pasan las 136 pruebas EditMode y las 19 PlayMode. El detalle está en `ECLIPSE-IMPLEMENTACION.md`.

## Gameplay versión 3, paso 6: interfaz básica, fusionada con la rama de Diego

- Interfaz básica (Diego la estiliza):
  - Pantalla de Carga entre Revisión y Lanzar. El botón de la Revisión ahora dice «Cargar la nave».
  - En la Operación: altitud y combustible, memoria con la cuenta para la ventana de contacto, «Enviar» y «Esperar órbita», ventana de decisión con las respuestas del evento, y objetivos extra. Solo aparecen las herramientas que subieron.
  - Los marcos cuadrados morados del mapa son la petición de la NASA.
- Hueco del diseño que se encontró al probar: con la memoria llena y la ventana cerrada no había forma de avanzar. Se añadió `Operacion.Esperar()` (deja pasar una órbita sin escanear y sin eventos).
- Objetivo imposible: «Terminar con 5 de combustible» ya no sale si la nave arranca con menos de 5.
- Fusión con `feat/eclipse-cinematics-audio` (cinemática, investigación orbital 3D, satélites de NASA, audio):
  - La cinemática de Diego ahora empieza con «Lanzar» de la Carga (`ConstruirCinematica` recibe el botón).
  - La ventana de decisión espera a que terminen la cinemática y la vista 3D del escaneo.
  - `AlEscanear` cuenta como «nueva» también la transmisión que queda en memoria.
  - Se quitó `ConstruirEfectoLanzamiento`, porque Diego borró `EfectoLanzamiento`.
- Aviso para Diego: «Construir escenas (sin Menu)» reescribe 3 materiales del Aquarius (`NASA_Sat_Aquarius_tex_01` acaba con la textura `shiny_panel_right_001`) y cambia la emisión de `Marcador_Orbita`, `Nave_Tobera` y `Plataforma_Resalte`. Se dejaron sus versiones de la rama y no se tocó `Resultado.unity`.
- Verificado en el navegador a 1366x768 (build de 35.3 MB): Carga, cinemática con «Omitir», Operación, escaneos con la vista 3D, memoria llena y ventana de contacto. En esa prueba no salió ningún evento al azar, así que la ventana de decisión solo está probada en las pruebas automáticas.
- Pasan las 198 pruebas EditMode y las 19 PlayMode (el recorrido ahora pasa por la Carga, responde eventos y envía a la Tierra).

## Gameplay versión 3, paso 7: partidas completas en el navegador

- Se jugaron dos partidas completas a 1366x768 en el build WebGL, viendo cada pantalla antes de decidir la jugada. El navegador quedó abierto con un control local, sin cambiar el juego.
- Partida 1, nivel 3 (Tormenta severa: 3 eventos seguros entre los escaneos 1 y 6):
  - Configuración del caso 2. Carga: refrigeración y reconfiguración (25 kg, 0 de combustible).
  - Cinemática con «Omitir» y escaneos con la vista 3D.
  - Eventos y decisiones:
    - Petición de la NASA: se aceptó, apareció el punto morado y se cumplió.
    - Sombra de Marte: sin batería y sin combustible solo quedaba aguantar.
    - Sobrecalentamiento: apareció la opción de la refrigeración porque se cargó, y se usó.
  - Envío en las ventanas de contacto. La anomalía se encontró con la térmica y el radar.
  - Informe: «Misión cumplida», 1,185 puntos (1,000 + 135 + 150 por la petición − 100 por un dato de baja calidad).
- Partida 2, nivel 1: memoria llena, «Esperar órbita» hasta la ventana, envío y radar. 1,695 puntos, con dos objetivos cumplidos y combustible sobrante. El desglose cuadra.
- Arreglado:
  - Con un evento de texto largo, la transmisión se encimaba con «Transmisiones recibidas». Ahora el texto de la transmisión ajusta su tamaño solo (16 a 24).
  - `ConfiguracionTests` buscaba «la primera» `NaveUI`, y desde la vista orbital de Diego hay dos. Fallaba o pasaba según el orden en que se regenerara la escena. Ahora usa la `NaveUI` que maneja la Configuración.
- Nota de balance, sin cambiar:
  - En el nivel 3 (8 órbitas), la última ventana de contacto cae justo cuando se acaban las órbitas, así que lo que se encuentre después de la órbita 5 llega con baja calidad. Es la tensión que busca el diseño, pero conviene que Adrian decida si se deja así.
  - Con un evento en pausa, el texto del evento sale dos veces (en la ventana y en el panel derecho).
- Pasan las 198 pruebas EditMode y las 19 PlayMode. Build de 35.3 MB sin errores.

## Gameplay versión 3: decisiones de Adrian tras el paso 7

- **La última órbita siempre tiene ventana de contacto.** `OrbitasParaContacto` toma la menor entre la ventana normal y las órbitas que faltan para la última. Así, lo que se encuentra al final ya no se pierde solo por el calendario. En el nivel 3, lo de la órbita 6 ahora se puede enviar en la última.
- **Sin texto duplicado:** con un evento en pausa, el panel derecho muestra solo el nombre y «Control de Misión espera tu decisión». El texto completo queda en la ventana de decisión. Verificado en el navegador con una «Ventana despejada».
- 2 pruebas nuevas en `MemoriaTests.cs`. Pasan las 200 pruebas EditMode y las 19 PlayMode. Build de 35.3 MB sin errores.

## Mecánicas de los satélites

- La IA propuso una ventaja y una desventaja por satélite, ligadas a lo que hizo la misión real. Adrian las aprobó tal cual (tabla en `PROPUESTA-GAMEPLAY.md`).
- `SateliteDatos` ganó costo, masa, textos de ventaja y desventaja, y modificadores. Los escribe `DatosVersion3.cs`.
- Cómo funciona:
  - `Carga` cobra el satélite primero, bloquea «Lanzar» (`PuedeLanzar`) si no alcanza y tiene `UsarMro()`.
  - `Operacion` aplica los efectos: alcance por instrumento, costo de altitud, memoria, consumo, transmisor, ciclo de contacto, inmunidades y bloqueo del láser.
  - Sin Carga no hay satélite y nada cambia.
- Interfaz:
  - La Carga muestra el satélite con su ventaja y desventaja, y el botón «Usar el MRO».
  - La Operación usa los valores del satélite en los botones y avisa cuando el polvo bloquea el láser o cuando el Chandra evita la pérdida de orientación.
  - El informe dice el satélite.
- Verificado en el navegador a 1366x768: con el Chandra en el caso 2 falta masa (−10 kg), «Lanzar» se bloquea y «Usar el MRO» lo desbloquea.
- 22 pruebas nuevas en `SatelitesTests.cs`. Pasan las 222 pruebas EditMode y las 19 PlayMode. Build de 35.3 MB sin errores.

## Sexto satélite: AztechSat-1

- Adrian pidió añadir el AztechSat-1. La IA verificó en internet lo que hizo la misión real: CubeSat 1U de la UPAEP, lanzado en la CRS-19 (5 de diciembre de 2019), liberado desde la Estación Espacial en febrero de 2020 y con el objetivo de comunicarse a través de la constelación Globalstar. Propuso mecánicas a partir de eso y Adrian las aprobó:
  - 15 cr y 0 kg.
  - Envío por la constelación con la ventana cerrada (cuesta 1 órbita).
  - −100 W de límite y −1 bloque de memoria.
- No hay modelo de NASA y solo se usan assets propios o CC0, así que Adrian eligió que la IA hiciera uno simple: `CuerpoCubeSat` en `ConstructorEscenas.Nasa.cs`. Es un cubo de aluminio con celdas solares, rieles y antenas, con 4 materiales `CubeSat_*`. La lista `Satelites` de Diego tiene ahora 6 entradas, para que «Construir escenas» no lo borre.
- Interfaz:
  - La lista de opciones de la Configuración cabe con 6 filas.
  - El botón de envío dice «Globalstar (+1 órbita)» cuando la ventana está cerrada.
  - «Usar el MRO» solo aparece si «Lanzar» está bloqueado.
  - El pie y la ayuda del satélite ya no dicen «solo cambia el aspecto».
- Verificado en el navegador a 1366x768: las 6 opciones, el modelo, la Carga y la Operación (límite de 600 W y memoria de 2 en el nivel 2). Un clic de la prueba no acertó al botón de envío porque se movió, así que el envío por Globalstar solo está probado en las pruebas automáticas.
- 5 pruebas nuevas en `SatelitesTests.cs`. Pasan las 227 pruebas EditMode y las 19 PlayMode. Build de 35.3 MB sin errores.

## Arreglo: texturas de los satélites de NASA al regenerar

- **Causa:** `MaterialNasa` tomaba la textura del material que crea el importador de OBJ de Unity, y el importador a veces le asigna a cada material la textura del anterior, según el orden de importación. La versión comiteada ya tenía 10 materiales mal: 5 con la textura de otro (4 del Aquarius y el `foil_gold` del MRO) y 5 sin textura (3 del Magellan, el `foil_gold` del Chandra y el `AO_other_dep` del Aquarius).
- **Arreglo:** la textura se carga por nombre, `<modelo>_<material>.png`, la misma convención que siguen todos los `.mtl`. El importador solo se usa si no hay archivo. Se comprobó que los 58 materiales `NASA_Sat_*` quedan con su textura.
- `Marcador_Orbita`, `Nave_Tobera` y `Plataforma_Resalte` ahora llevan la palabra clave `_EMISSION`, que es lo que pide el constructor. La versión anterior tenía el color de emisión pero sin la palabra clave, así que no brillaban.
- El build pasa de 35.3 a 36.7 MB, porque ahora sí entran las texturas que faltaban. Pasan las 227 pruebas EditMode y las 19 PlayMode.

## Versión 4: Operación por meses (rama `feat/meses`)

- Adrian pasó el paquete «Operación Eclipse Rojo» v1.0 (encargo, reglas, audio, plan de pruebas, maquetas). La IA lo leyó entero y lo contrastó con el código real.
- Antes de planificar, la IA preguntó tres cosas, porque el paquete pedía 9 fases y cambiaba reglas aprobadas el mismo día. Adrian respondió: menos de un día de plazo; sí a los dos cambios de reglas; y que la IA lo hiciera todo.
- Con eso, la IA recortó el plan a las fases 2, 3 y 4 del paquete y dejó el resto fuera, anotado en `PROPUESTA-GAMEPLAY.md` (versión 4). Adrian aprobó el plan.
- Decisión de diseño de la IA: no reescribir la Operación, sino convertir la «órbita» en «mes» y reutilizar memoria, contacto, eventos, decisiones, satélites y objetivos.
- Fase A (molestias):
  - Pasar el cursor ya no suena; el montaje de un instrumento suena una vez y tapa al clic; `ReproductorUI` deja un solo sonido por acción (120 ms, gana el más importante); la preescucha del volumen suena al soltar.
  - La cinemática ya no apaga la cámara principal (causa de «No cameras rendering» en el editor): la deja sin capas.
  - Los textos del informe se achican en vez de encimarse.
- Fases B, C y D:
  - `Operacion.ResolverMes` resuelve el mes una sola vez; `Registro` y `ResultadoMes` llevan los hechos; `ResultadoCientifico` da la categoría según lo enviado.
  - `OperacionUI` nueva: plan del mes, previsión, «Resolver mes», resumen y decisión. La vista 3D de Diego se dispara una vez por mes.
  - Las 5 pruebas de la v3 que las reglas nuevas invalidan están en `archivo/pruebas-v3` (fuera de `Assets`). Las sustituyen `MesesTests.cs` (con los ids del paquete: MES, ORB, MEM, CIE, EVT) y versiones nuevas de `SatelitesTests`, `InvestigacionTests` y `RecorridoTests`.
- Fallos de las pruebas nuevas que la IA corrigió sin tocar la lógica: tres expectativas mal calculadas (un instrumento que sigue encendido vuelve a observar el mes siguiente).
- La IA no puede escuchar: el sonido solo está comprobado por código y por una prueba que cuenta peticiones.

## Revisión independiente de la versión 4 antes de subirla

- Antes de subir la fusión, la IA lanzó una revisión de solo lectura del diff: 4 revisores con ángulos distintos (lógica, interfaz, resultado, audio y datos) y un verificador por hallazgo que intentaba refutarlo. Salieron 13 hallazgos distintos; se verificaron los 6 más graves y los 6 se confirmaron.
- Arreglados:
  - **Esquivar un evento (media):** durante la animación del escaneo se podían apagar los instrumentos con la decisión pendiente y así anular el sobrecalentamiento o el modo seguro. Ahora el plan no se puede cambiar con un resumen o una decisión pendientes.
  - **Informe injusto (media):** la conclusión de «Investigación fallida» o «Señal incompleta» culpaba a la configuración aunque el fallo fuera no enviar los datos. Si la nave podía conseguir más, ahora dice que faltó enviar la evidencia (`TextosResultado.TextoSinEnviar`).
  - **Ventana despejada inútil (baja):** «Abrir una ventana de contacto» no hacía nada si el mes siguiente ya tenía contacto. Ahora abre el primer mes que no lo tenía.
  - **Herramienta gastada en vano (baja):** el giroscopio o la refrigeración seguían apareciendo como útiles cuando el efecto ya había pasado. Ahora el evento se da por resuelto al pasar su efecto.
  - **Contador (baja):** «Evidencias recibidas» contaba TX-00, que es de la misión.
  - **Vista 3D (baja):** decía «ZONA YA ANALIZADA» cuando la anomalía se observaba por primera vez sin calidad suficiente. Ahora dice «ANOMALÍA OBSERVADA · SIN EVIDENCIA NUEVA», en ámbar.
- También, sin verificador pero comprobados al leer el código: el texto del Aquarius hablaba de «lecturas falsas», que ya no existen; y el enfriamiento de `ReproductorUI` se reinicia al activarse, porque si el editor no recarga el dominio podía callar todos los sonidos en la segunda sesión de Play.
- Sin verificar ni arreglar, todos de gravedad baja:
  - Con una configuración que no puede dar evidencia, dos objetivos extra se cumplen enviando cualquier registro.
  - Con teclado o mando, el deslizador de efectos no da preescucha.
- 3 pruebas nuevas. Pasan las 211 pruebas EditMode y las 19 PlayMode.

## Puntos: lo que sobra solo cuenta con evidencia enviada

- La IA jugó en el navegador una campaña parcial (1366x768: «Señal incompleta», 975 puntos) y una fracasada (1280x720: «Investigación fallida», 745 puntos). Vio que fracasar pronto daba casi tantos puntos como ganar a medias: 250 por meses sobrantes y 215 por presupuesto.
- Adrian aprobó que el presupuesto y los meses sobrantes solo sumen si llegó a la Tierra al menos una evidencia (`Puntuacion.Desglose`). La misma partida fracasada pasa de 745 a 130 puntos (fracaso y combustible sobrante).
- Prueba nueva en `MesesTests`. Tres pruebas antiguas que miraban la línea del presupuesto en misiones sin evidencia ahora envían una antes de mirar los puntos. Pasan las 212 pruebas EditMode y las 19 PlayMode.
- Después Adrian pidió la misma regla para el combustible sobrante. Ahora, sin evidencia enviada, solo cuentan los 100 puntos del fracaso (y los objetivos o la petición de la NASA, si se cumplen). La prueba `SinEvidenciaEnviadaNoSumaNadaDeLoQueSobra` lo comprueba. Pasan las 212 pruebas EditMode y las 19 PlayMode.

## Pulido de presentación con el patrocinador (Diego, 2026-11-14)

- Diego pidió: un arranque más profesional con el logo del patrocinador (National Bulwark Services) donde salía el de Unity; Marte con mejores texturas; órbitas que se distingan; solo el cohete elegido en la pestaña Lanzador; instrumentos mejor montados; un despegue con mucho más detalle según el lanzador (árboles, fuego, logo en los cohetes); una transición de meses al estilo de los juegos de supervivencia; y un final animado según lo que pasó.
- Decisiones de Diego: los dos logos, sin fondo; patrocinador y después Unity en el splash; sí generar audio nuevo.
- La IA recortó los logos con un script propio (se conserva el blanco interior del toro), hizo el mapa de Marte de 8K en dos mitades de 4096 para WebGL, shaders propios (`Planeta`, `Cielo`, `Llama`, `Aditivo`), el complejo de lanzamiento por código, la transición de meses, el final animado y 18 audios con ElevenLabs (unos 644 créditos).
- Único cambio en código de Adrian: el evento `OperacionUI.AlResolverMes` y la transición en la condición que retrasa el dibujo del resumen. La lógica sigue igual y en el mismo cuadro.
- Detalle completo en `ECLIPSE-IMPLEMENTACION.md` (sección «Pulido de presentación»).

## Versión 5, bloque 1: valores basados en datos de la NASA

- Adrian pidió que todo se base en datos de la NASA. La IA lanzó dos investigaciones con fuentes (instrumentos, naves y lanzadores; órbitas, comunicaciones y texturas) y preguntó el alcance. Adrian eligió el modo mixto: proporciones reales con números ajustados al juego.
- La IA buscó un factor que conservara el resultado de los 19 casos y lo comprobó con un cálculo antes de proponerlo: masa × 3 y potencia × 5 de THEMIS, SHARAD, HiRISE, MCS y CRISM. Solo cambian el costo de la alta resolución (200) y la capacidad del Explorador (500 kg). Adrian lo aprobó.
- `DatosNasa.cs` escribe los valores y el dato real (inglés y español, con fuente) de instrumentos, órbitas, lanzadores y satélites. La ayuda de cada opción lo muestra como «NASA data: …».
- Las pruebas que fijaban los números viejos se actualizaron:
  - Las tablas de los 19 casos se recalcularon con un script.
  - Las pruebas que dependían de poco combustible usan ahora naves más cargadas (radar con espectrómetro, o el Titán con alta resolución).
  - Prueba nueva: cada opción tiene su dato NASA en los dos idiomas.
- Pasan las 213 pruebas EditMode y las 19 PlayMode.

## Versión 5, bloque 2: buffs y debuffs visibles

- `Modificadores.cs` calcula los buffs y debuffs desde los datos del satélite y del estado de la Operación; `PresentacionMision.Modificadores` los pinta con flecha y color (iconos nuevos de Lucide: arrow-up, arrow-down y lightbulb, añadidos a `Iconos.Todos`).
- Se ven en la ayuda del satélite, en la Carga, en el panel nuevo «Active modifiers» de la Operación (constructor de escenas) y en el desglose de la calidad del resumen (`ResultadoMes` guarda los factores).
- Chandra: -1 bloque de memoria por su dato real (1.8 Gb).
- Comprobado en el navegador a 1366x768: Configuración, Carga y Operación. La ayuda del satélite se reordenó porque con tanto texto se achicaba demasiado.
- Al volver a correr los scripts de datos, Unity reescribe las comillas del YAML de muchos assets sin cambiar el contenido: la IA lo verificó y solo comiteó el cambio real (el Chandra).
- 9 pruebas nuevas en `ModificadoresTests.cs`. Pasan las 221 pruebas EditMode y las 19 PlayMode.

## Versión 5, bloque 3: guía en la partida

- `Guia.cs` (lógica, sin interfaz) decide el siguiente paso; `OperacionUI.DibujarGuia` lo muestra con bombilla encima del mapa y mueve `ResaltarGuia` (contorno que late; fijo con «Reducir movimiento») al elemento que toca.
- `Operacion.Historial` guarda todas las observaciones, para que la guía dé pistas con la última lectura de cada instrumento.
- Primer intento: la guía pedía enviar aunque en memoria no hubiera evidencia, y el resaltado tocaba un componente ya destruido. Las pruebas lo detectaron y se corrigió.
- Comprobado en el navegador a 1366x768: la guía y el resaltado al empezar, al encender, tras una lectura fría y en el mes con contacto.
- 10 pruebas nuevas en `GuiaTests.cs`. Pasan las 231 pruebas EditMode y las 19 PlayMode.

## Versión 5, bloque 4: tutorial jugable (Misión 0)

- `TutorialUI.cs`: pasos y textos de la Misión 0. Se engancha a `OperacionUI` con `GuiaForzada` (texto y resaltado de cada paso), `AntesDeResolver` (bloquea «Resolve month» fuera de su paso y dispara la tormenta) y `TerminarForzado` («End mission» = saltar). La lógica de la Operación no cambió.
- `MisionUI.IniciarTutorial` monta la nave fija y abre la Operación. `EstadoJuego.Tutorial` marca la partida.
- La tarjeta va en la parte baja del centro (la anomalía del tutorial está arriba del mapa) y se oculta durante el resumen para no tapar su botón.
- Cumple TUT-01 del paquete: siempre en fracaso, se puede saltar y repetir, sin penalización, guarda que se completó.
- Jugado entero en el navegador a 1366x768, del Briefing al final y de vuelta al Briefing, sin errores en la consola.
- 5 pruebas nuevas en `TutorialTests.cs`. Pasan las 236 pruebas EditMode y las 19 PlayMode.

## Versión 5, bloque 5: idioma (español e inglés)

- `Traducciones.cs`: diccionario inglés → español (unos 700 pares). La IA lo generó a partir del commit de traducción de Diego (`a9d50e7`, que pasó el juego al inglés) más pares escritos a mano para lo nuevo.
- `Idioma.cs`: `Idioma.T(texto)` traduce si está en español. `EspanolDe` también resuelve textos con icono delante, flecha detrás, icono con color y varias líneas.
- Los datos (`InstrumentoDatos`, `OrbitaDatos`, etc.) tienen propiedades (`Nombre`, `Descripcion`, `DatoNasa`…) que la interfaz usa en vez de los campos; los campos guardados siguen en inglés.
- `TraductorEscena.cs` traduce en tiempo de ejecución los textos fijos que pone el constructor y los devuelve al inglés. No corre en las pruebas EditMode.
- `SelectorIdioma.cs` + `ConstruirSelectorIdioma` en el constructor: selector en el título y en Opciones. `PantallaTitulo` no se cierra al pulsar el selector.
- Arreglos que salieron al probar en el navegador:
  - El título revelaba un número fijo de letras y cortaba «ROJO».
  - El tamaño mínimo del título no dejaba caber el español.
  - Créditos quedaba tapado por su botón.
  - Faltaban textos sueltos: barra de pasos, semáforo, resumen del mes, «Cómo jugar» y Créditos.
- Comprobado en el navegador a 1366x768: título, menú, Cómo jugar, Créditos y Opciones; Briefing, Configuración, Revisión y Carga; cinemática, Operación, resumen del mes, evento con decisión e informe en español; vuelta al inglés desde Opciones. Sin errores en la consola.
- 9 pruebas nuevas en `IdiomaTests.cs`. Pasan las 245 pruebas EditMode y las 19 PlayMode.
- Pendiente: las voces siguen en inglés. En pantalla táctil, tocar el selector del título también cierra el título (el filtro solo mira el ratón).

## Versión 5, bloque 6: texturas y cielo

- `Herramientas/texturas_naves.py` (numpy y PIL) genera `Lamina_Arrugada`, `Espuma_Naranja` (cada una con su `_Normal`) y `Metal_Cepillado`, todas repetibles sin costura.
- Con el EXR de NASA SVS (`starmap_2020_8k_gal.exr`, 160 MB, no se guarda en el repo) genera también `Cielo_NASA.jpg` y el recorte `Estrellas.png`. Usa oiiotool (exposición ×2.5, de lineal a sRGB) y ImageMagick.
  - Ojo: `arte.py` y `estrellas.py` también escriben `Estrellas.png`. Si se vuelven a correr, hay que correr después `texturas_naves.py`.
- Licencia: la página 4851 de SVS no indica restricciones, y SVS declara su contenido de dominio público salvo aviso. El crédito incluye Gaia DR2 (ESA/Gaia/DPAC), como pide la página.
- `ImportadorArte`:
  - UV por proyección de caja en los OBJ que no traen (instrumentos del kit y SLS). Las mallas del SLS en `Assets/Arte/Mallas` se regeneraron con UV.
  - Normales para todo lo que termina en `_Normal`.
  - `Cielo_NASA` a 8192, con Crunch y sin mipmaps.
- Constructor: `Relieve()` pone el mapa de normales. `CieloEstrellas` usa el material nuevo `Cielo_NASA.mat`. Los materiales del kit eligen su textura por el nombre del MTL (oro, panel, titanio o metal). La sección «Stars» se agregó a Créditos, también en español.
- Las láminas de los satélites de NASA no se tocaron: sus texturas son rampas que dependen de sus UV, y repetir una textura encima las rompería.
- Logo: la IA revisó a mano las 10 texturas grandes de los modelos NASA (bisagras, paneles, mantas) y no lleva el logo de la NASA. Las otras son colores planos de menos de 4 KB.
- Comprobado en el navegador a 1366x768: Titán, Explorador, AztechSat-1, instrumentos de cerca y cinemática hasta la inserción orbital, sin errores en la consola.
  - La primera lámina parecía empedrado y luego purpurina: se quitaron los contornos oscuros, se agrandaron las facetas y se bajó el brillo.
- 12 pruebas nuevas en `TexturasTests.cs`. Pasan las 257 pruebas EditMode y las 19 PlayMode.
- Riesgo: la textura de 8192 necesita una GPU que la acepte. Los equipos de escritorio sí; un móvil viejo podría mostrar el cielo negro.

## Versión 6 (Diego, 2026-11-15)

- Diego probó la versión 5 y pidió cinco cambios. La IA exploró el código con tres agentes y diseñó la mecánica del alcance con un cuarto. Le preguntó a Diego:
  - el modelo de alcance: delta-v real sin aerofrenado;
  - si Adrian estaba de acuerdo con cambiar su lógica: sí;
  - qué ayuda quería: guía por pantalla, «?» y «Cómo jugar»;
  - qué hacer con el mapa de Marte.
- La IA verificó en fuentes los datos que el juego rotula como «NASA data»: GM y radio de Marte (NSSDC), 1,223 kg de Mars Express (ESA), inserción del MRO de ~1 km/s más aerofrenado (NSSDC) e Isp de la familia MR-107 (222 a 236 s).
- Lo hizo en bloques, con un commit por bloque y todas las pruebas en verde en cada uno: informe en dos pantallas, voces, alcance, ayuda y Marte.
- Pruebas: la línea base de main tenía 1 fallo de 257, solo en un equipo con configuración regional en español («×1,5»). Se corrigió usando formato invariante.
  - Pruebas nuevas: `AlcanceTests`, `VocesTests`, `GuiaPantallasTests` y un recorrido PlayMode con el orden nuevo.
  - Al final pasan las 282 EditMode y las 20 PlayMode.
- La IA revisó cada pantalla con capturas en batch, en español y en inglés. De ahí salieron varios arreglos:
  - la tarjeta de órbita bloqueada partía «kg» en tres líneas;
  - la guía tapaba el título del paso;
  - el «?» de la Operación se encimaba con un panel;
  - el planeta del Briefing quedaba detrás de los cuadros.
- Sin numpy en el equipo, las texturas nuevas (nubes de Marte y degradado del Briefing) se generan con un script de editor propio (`GeneradorCapasMarte`).

### Versión 6, segunda vuelta (Diego, 2026-11-15)

- Diego jugó la versión 6 y pidió tres cosas:
  - un Briefing que se lea: eligió «solo lo esencial»;
  - los datos de la NASA detrás de un botón;
  - buscar errores de animación, voces y mecánicas.
- Errores encontrados al revisar el código, que se arreglaron:
  - la transmisión inicial se narraba encima de la cinemática del lanzamiento: era la voz que se encimaba con control de misión;
  - las órbitas y sus etiquetas se dibujaban detrás del Briefing;
  - el «?» tapaba texto.
- Revisión independiente con un agente que no conocía esas conclusiones: confirmó lo anterior y encontró siete fallos menores, todos arreglados:
  - «Usar el MRO» no rehacía la evaluación;
  - la guía ignoraba el rojo por alcance en dos pestañas;
  - el «?» flotaba sobre los paneles de la Operación;
  - la transmisión seguía sonando al saltar la Misión 0;
  - la reserva de idioma de las voces usaba `??` con objetos de Unity;
  - la fórmula mostraba 9.81 sin dar exactamente el número mostrado;
  - dos pruebas cubrían un caso que el juego ya no permite.
- Pruebas nuevas (`CinematicaTests`, PlayMode):
  - cinemática entera en español y en inglés: nunca suenan dos voces a la vez, la transmisión solo empieza al terminar y en el idioma del juego;
  - saltar la cinemática corta a control de misión.
- Partida real en el navegador: el Chromium de Playwright, controlado por CDP desde Node, sin instalar nada.
  - Recorrido: título, menú, Briefing, configuración en el orden nuevo, Revisión, Carga, cinemática, Operación (encender, marcar, resolver un mes, resumen) e informe en dos pantallas hasta «Modificar mi misión».
  - En español a 1920×1080 y en inglés a 1366×768. La consola del navegador quedó sin errores.
  - Lo que salió de esa partida y se arregló:
    - las nubes de Marte se veían como vetas grises en la sombra (no se notaba en las capturas del editor);
    - el botón de la Misión 0 partía el «0» a otra línea;
    - el cuadro de recursos del detalle se estiraba vacío.
- Pasan las 283 pruebas EditMode y las 23 PlayMode.
- Lo que no se pudo comprobar: la mezcla de audio a oído (volúmenes, si la voz se entiende sobre el motor).

## Versión 7 (Diego, 2026-11-15)

- Diego reportó tres cosas al jugar (falta un botón para regresar en el Briefing, Marte cortado por los paneles en la pestaña Órbita, la Revisión sin scroll), pidió quitar a Sofía de los créditos y entregó un paquete de diseño para incorporarlo.
- La IA comparó el paquete con el juego con un agente de solo lectura. Resultado: casi todo ya existía; faltaban la regla de «una sola órbita visible», dos órbitas, los modos del espectrómetro y la cámara, tres audios, pantalla completa y los tiempos de los storyboards.
- Preguntó a Diego qué entraba (todo), cómo manejar las dos órbitas (jugables con los valores propuestos por la IA) y cómo mostrar Marte (encajado en el centro).
- Decisiones de la IA que conviene revisar:
  - Las dos órbitas nuevas y sus valores. El paquete los daba por «pendientes del equipo».
  - Desde la areoestacionaria el radar y la cámara de alta resolución no dan evidencia.
  - La cinemática quedó en 29.7 s y no en los 24 s del storyboard, para no encimar voces.
- ElevenLabs: la IA preparó los seis nodos, pidió el estimado (518 créditos), esperó el visto bueno de Diego y generó (253 créditos reales).
- Pruebas: 298 EditMode y 23 PlayMode. Nuevas: `OrbitasTests` (una sola trayectoria en las 20 transiciones, reglas de las dos órbitas) y `ModoEscaneoTests`.
- Un fallo que salió y se corrigió: con el fundido nuevo del menú, la prueba del recorrido esperaba la escena por cuadros y en batch se quedaba corta.

## Versión 8 (Diego, 2026-11-15)

- Diego pidió mejorar las pantallas finales y rediseñar los modelos 3D con el paquete «Fable 5.2» (estilo Eclipse Ink).
- La IA auditó el proyecto con tres agentes de solo lectura antes de cambiar nada (`Docs/Claude/AUDITORIA_INICIAL.md`) y preguntó a Diego el alcance: solo estilo de tinta sobre los modelos actuales; cinemática a criterio de la IA con las 10 fases; informe con Marte y la nave protagonistas, tablero y botón al menú.
- Lo que hizo la IA:
  - Shader `Tinta` (cel shading de 3 bandas y contorno por malla invertida) como piloto en el satélite y los instrumentos.
  - Informe final rediseñado: veredicto tipo póster y detalles como tablero.
  - Cinemática de vuelta a sus tiempos originales (44 s medidos).
- Decisiones de la IA que conviene revisar:
  - El contorno es de malla invertida porque el WebGL no tiene textura de profundidad.
  - Tono de la sombra, grosor de la línea (1.2 a 1.5 px a 1080p) y línea azul pizarra en las piezas negras.
  - En el fracaso la tormenta de Marte se queda al 50 % en vez del 95 %.
  - La duración de la cinemática: se deshizo el recorte de la versión 7.
- El paquete pide no activar las dos órbitas nuevas sin acuerdo del equipo. Siguen jugables por la decisión de Diego en la versión 7; falta que Adrian las revise.
- Un fallo anterior que apareció al probar: las marcas de las casillas de Opciones no se veían nunca.
- Pruebas: 336 EditMode y 24 PlayMode. Nuevas: `TintaTests`, `ResultadoTests` y `OpcionesTests`.
- No se gastaron créditos de ElevenLabs.
- Lo que no se pudo comprobar: los FPS con tarjeta gráfica real.
- Fase 2 (mismo día): Diego vio el piloto, notó que la animación casi no cambiaba y pidió extender el estilo. La IA lo llevó a los cohetes, la torre, los edificios, los árboles y el suelo, puso luz de hora dorada y acercó dos planos. Pruebas: 349 EditMode y 24 PlayMode. Detalle en `Docs/Claude/REPORTE_FASE_2.md`.

## Explorador con el modelo Ares 1 (B) de NASA (2026-11-15)

- Hecho en la rama local `feat/ares1b`, sobre la rama del PR #1 (`feat/eclipse-ink`), porque ese PR cambia el constructor, la cinemática y el estilo de los cohetes.
- `ConstructorEscenas.Ares.cs`, nuevo: instancia `ares1b.fbx`. Unity ya lo importa de pie, sin girarlo. Lo escala a 7.5 de alto y lo parte por altura (fracciones medidas en el modelo):
  - primera etapa con interestadio: el cohete;
  - `EtapaSuperior`;
  - el Orion y la torre de escape en dos mitades con bisagra (`CofiaFrente` y `CofiaDetras`).

  El FBX trae casi todo el cohete en una sola malla, así que hubo que cortarla.
- El Ares no tiene propulsores laterales: en la separación cae la primera etapa entera (`propulsoresExplorador` queda vacío). El Ares no lleva cofia, así que el satélite sale al abrirse la nariz. Esa es la adaptación.
- Alto 7.5 en vez de 6.1, porque el Ares es muy delgado. El Ares real medía casi lo mismo que el SLS, pero se dejó más bajo que el Titán (11) para que se lea cuál es el pequeño. Se ajustó en `DirectorCinematica` y en el encuadre de la pestaña Lanzador.
- Colores planos del modelo (materiales `NASA_Cohete_Ares_*`) con el estilo de tinta de los cohetes. No se añadieron texturas. El logo «Fence» del patrocinador pasó de la cofia del Soyuz a la etapa superior.
- Las mallas del Soyuz que quedaron sin uso se movieron a `archivo/soyuz-explorador/`.
- Pruebas: 4 nuevas en `AresTests.cs`. Pasan las 353 EditMode y las 24 PlayMode. Build WebGL de 57 MB.
- Comprobado en el navegador a 1366x768: la pestaña Lanzador y la cinemática completa con el Explorador (despegue, separación, apertura de la nariz y satélite liberado) hasta la Operación, sin errores en la consola.
- Pendiente: la leyenda del Explorador dice «Like the Soyuz-Fregat». Se refiere a la capacidad, que sigue siendo cierta, pero el aspecto ya es del Ares.

## Versión 9: informe menos saturado, consejos opcionales, plazo del mes 6 y feedback (2026-11-15)

Pedido de Adrian: un informe menos saturado y un juego informativo sin ser invasivo. También, que quien no juegue pierda a los 6 meses, y que haya más feedback de cada decisión.

- **Informe:** en los detalles se ven solo las evidencias y el total de puntos. Recursos, fichas, objetivos extra y cada línea de puntos van en «Show breakdown», plegado al entrar (`ResultadoUI.AlternarDesglose`). El récord se calcula una vez por informe.
- **Consejos opcionales:**
  - «Show tips» en Opciones.
  - Apagados, la Operación no muestra el paso a paso ni el resaltado. Los avisos y la Misión 0 se mantienen.
  - La ayuda «?» de la Operación se abre sola solo la primera vez (`Preferencias.AyudaOperacionVista`).
- **Plazo:**
  - Si al cerrar el mes 6 no llegó ninguna evidencia a la Tierra, Control de Misión cancela la misión, que cuenta como fracaso. El límite está en `ReglasOperacionDatos.mesLimiteSinEvidencia = 6`.
  - El contador de evidencia muestra el plazo desde el mes 1: en ámbar a partir del mes 4 y en rojo en el mes 6.
  - El resumen del mes y el informe explican la cancelación. `MesesTests` apaga el plazo porque prueba los 12 meses.
- **Feedback de cada decisión:** encender, apagar, marcar región, maniobrar y enviar dicen qué causan (W, bloques, créditos, combustible, días).
- El idioma ya arrancaba en inglés y las voces ya estaban en los dos idiomas (PR #1): no se tocó.
- Pruebas: 366/366 EditMode (6 nuevas en `PlazoEvidenciaTests.cs`, 1 en `GuiaTests`, 1 en `ResultadoTests`) y 24/24 PlayMode. Build WebGL correcto.
- Comprobado en el navegador a 1366x768:
  - Opciones con «Show tips».
  - Detalles plegados y abiertos.
  - Operación con los consejos apagados.
  - Ayuda abierta sola la primera vez.
  - Efecto al encender el radar.
  - Cancelación al cerrar el mes 6 e informe de la misión cancelada.

## Versión 9: semáforo, Opciones, Coloso (Saturn V) y desbloqueos (2026-11-15)

- **Semáforo:** un límite superado se ve en rojo aunque falte la órbita (antes, con todos los instrumentos, salía gris). El mensaje dice el límite y lo que falta.
- **Opciones:** dos columnas (Sonido y Progreso | Pantalla, Ayuda e Idioma), volúmenes en una fila y casillas con borde cian y ✓.
- **Coloso:** tercer lanzador con el Saturn V de NASA 3D Resources.
  - La IA lo convirtió con Blender (`scratchpad`, sin guardar el script en el repo) y quitó las texturas con «USA», «United States» y banderas.
  - `ConstruirPorEtapas` generaliza lo del Ares (cortes en 0.62 y 0.79 del alto) y la cinemática lo trata como el Explorador: cae la primera etapa entera.
  - Datos en `DatosVersion9.cs`: 520 cr, 950 kg, 5 espacios, 2,600 kg hacia Marte. Son valores de juego.
- **Desbloqueos (`Progreso`):**
  - Al empezar solo hay lo básico. Ganar el nivel 1 abre el nivel 2, Titán, Magellan, Chandra, Aquarius, alta resolución, sensor atmosférico y elíptica. Ganar el nivel 2 abre el nivel 3 y el resto.
  - Lo bloqueado sale con candado y dice qué nivel ganar; el veredicto anuncia lo desbloqueado.
  - Opciones tiene «Unlock everything» (jurado) y «Reset progress». Las pruebas corren con todo abierto (`ProgresoAbierto`).
- Pruebas: 373/373 EditMode y 24/24 PlayMode. Build WebGL correcto.
- Comprobado en el navegador a 1366x768: candados en el Briefing y en la pestaña Lanzador, desbloquear todo desde Opciones y cinemática completa con el Coloso.
- **Más lanzadores (mismo día):**
  - Seis en total, del más barato al más caro: Pionero (Jupiter-C, 1 espacio, abierto desde el principio), Explorador, Horizonte (Ares 1 A, 3 espacios, nivel 1), Titán, Goliat (Saturn V en gris, nivel 2) y Coloso.
  - Los modelos nuevos se convirtieron con Blender sin las texturas con «USA».
  - `ConstructorEscenas.CohetesExtra` es la tabla de modelo, cortes, tinte y posición. La cinemática usa la lista `DirectorCinematica.cohetesExtra`, y `PlataformaCohetes.Alto` da el alto de cada uno.
- **Equipo:** el nombre cambió de GangBangers a DualDynamics en el juego, la licencia y el README.
- Pruebas: 377/377 EditMode y 24/24 PlayMode. Build de 58 MB. Revisado en el navegador: los seis en la plataforma, candados y cinemática completa con el Pionero.

## Renovación visual (15 de noviembre de 2026)

Claude (Fable 5.2), a pedido de Diego. Detalle, pendientes y matriz en `Docs/Claude/RENOVACION_INFORME.md`.

- Cohetes sin defectos de modelo, brazos de la torre a medida y plataforma con foso real.
- Constructor con montaje físico, retirada visible, mecanismos por instrumento, inspección y hangar.
- Despliegue tras la liberación y lecturas para los cinco instrumentos.
- Pruebas: 440 EditMode y 34 PlayMode. En navegador: del menú a la Operación en español, consola limpia.
- No cambia reglas ni resultados. No se vio en navegador ningún escaneo ni el informe final.
