# Reporte de la fase 2 — Eclipse Ink en el lanzamiento

Fecha: 2026-11-15. Rama `feat/eclipse-ink`, commit `9952fe3`. Sin push. Diego aprobó extender el piloto.

| Campo | Evidencia |
|---|---|
| Objetivo | Llevar el estilo al cohete, la torre, la plataforma, los edificios, los árboles y el suelo; luz de hora dorada; planos más cercanos |
| Compilación C# | COMPROBADO: sin errores |
| Pruebas en el editor | COMPROBADO: 349 EditMode y 24 PlayMode en verde |
| WebGL | COMPROBADO: build de 56.6 MB sin errores; cinemática con el Titán vista en Chromium 151 (WebGL por software) hasta la fase 8, sin materiales magenta: `capturas/Fase2_cinematica_WebGL.jpg` |
| Consola del navegador | NO EJECUTADO en esta pasada: la partida se cortó por tiempo antes de guardar la consola |
| Rendimiento | NO EJECUTADO: FPS con tarjeta gráfica real |
| Duración de la cinemática | 43.6 s (español, Explorador) y 43.4 s (inglés, Titán) |
| Audio | Sin cambios y sin gasto en ElevenLabs |

## Qué cambió

- **Estilo:** los dos cohetes (también las cofias), la plataforma, la torre, los mástiles y los edificios llevan bandas y contorno. Los árboles, contorno fino. El suelo (pasto, arena, mar, camino, colinas, losa), bandas sin contorno.
- **Fuera del estilo:** focos, toberas encendidas, logos del patrocinador, cielo, llamas, humo, Tierra y Marte.
- **Hora dorada:** horizonte cálido, bruma dorada y Sol teñido mientras la toma está en tierra. En el espacio y al terminar, el Sol vuelve a su color.
- **Humo** teñido por el fuego. **Paleta** nueva en árboles, colinas y el acento de la torre.
- **Planos más cercanos** en el encendido (fase 2) y en la separación de etapas (fase 6).
- Las 10 fases, su orden, las voces y los sonidos no cambian.

Capturas: `capturas/Fase2_complejo_editor.jpg` y `capturas/Fase2_cinematica_WebGL.jpg`.

## Cómo volver atrás

Igual que en la fase 1: «Herramientas → Eclipse Ink → Quitar», o revertir `9952fe3`. La luz dorada y los planos están en `DirectorCinematica.cs` y `ConstructorEscenas.Despegue.cs`.

## Falta

- Ver en el navegador las fases 9 y 10 y la cinemática con el Explorador (en el editor pasan las pruebas con los dos).
- FPS con tarjeta gráfica.
- No se cambió la forma de la llama ni se añadieron planos nuevos: solo se acercaron dos que ya existían.
- El menú y el escaneo no tienen cambios propios en esta fase; el satélite ya llevaba el estilo desde la fase 1.
