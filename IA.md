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
