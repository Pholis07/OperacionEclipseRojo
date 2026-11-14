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
