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
