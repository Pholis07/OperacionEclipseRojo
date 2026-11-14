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
