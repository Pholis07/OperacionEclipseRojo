# Reporte de la fase 1 — Piloto Eclipse Ink, informe final y cinemática

Fecha: 2026-11-15. Rama `feat/eclipse-ink`, creada desde `feat/alcance-ayuda-informe`. Sin push.

| Campo | Evidencia |
|---|---|
| Objetivo | Piloto del estilo en el satélite y los instrumentos; informe final rediseñado; las 10 fases de la cinemática a su tiempo |
| Commits | `f3c9074` piloto, `217680c` cinemática, `f2f2652` informe, y el de Opciones |
| Sistema | Unity 6000.6.9f1, Linux |
| Compilación C# | COMPROBADO: sin errores |
| Pruebas en el editor | COMPROBADO: 336 EditMode y 24 PlayMode en verde (antes 298 y 23) |
| WebGL | COMPROBADO: build de 56.4 MB sin errores; partida completa en Chromium 151 (WebGL por software) en español a 1920×1080, hasta el informe y de vuelta al menú; consola sin errores ni shaders rotos |
| Rendimiento | NO EJECUTADO: FPS con tarjeta gráfica real. El build pesa lo mismo que antes (57 MB → 56.4 MB) |
| Audio | Sin cambios y sin gasto en ElevenLabs |
| Git | Árbol limpio tras cada commit; no se tocó `Menu.unity` |

## Piloto del estilo

- Shader `OperacionEclipseRojo/Tinta` (`Assets/Arte/Shaders/Tinta.shader`): luz en 3 bandas, relleno frío en la sombra, brillo técnico, luz de borde y contorno por malla invertida.
- Aplicado a 73 materiales: los cinco satélites de la NASA, el CubeSat, los cinco instrumentos y sus soportes.
- Los vidrios y lo que emite luz llevan bandas pero no contorno. Las piezas casi negras llevan la línea en azul pizarra.
- No lo llevan Marte, la Tierra, las atmósferas, el Sol, las órbitas, los haces, las partículas, los logos ni la interfaz (`TintaTests` lo comprueba).
- Como los materiales son compartidos, el satélite cambia a la vez en el Menú, la Configuración, la cinemática, el escaneo y el informe.

Capturas con la misma cámara, Original / Eclipse Ink / Ligero:

- `capturas/AB_satelite_cerca.jpg`
- `capturas/AB_satelite_orbita.jpg` (la distancia real del juego)
- `capturas/AB_fondo_claro.jpg`

## Cómo volver atrás

- **El jugador:** Opciones → «Contornos de tinta». Apagado es el modo ligero: quedan las bandas y no se dibuja el contorno.
- **El equipo:** menú «Herramientas → Eclipse Ink → Quitar», o `EstiloTinta = false` en `ConstructorEscenas.Tinta.cs` y reconstruir. Los materiales vuelven a URP/Lit con sus colores y texturas.
- **Git:** revertir `f3c9074`.

## Informe final

- Veredicto tipo póster: `capturas/AB_informe_*_veredicto.jpg`.
- Detalles como tablero: `capturas/AB_informe_*_detalle.jpg`.
- Botón «← Menú» nuevo en los detalles.
- En el fracaso la tormenta se queda al 50 % (antes 95 %).
- No cambian los textos de cada resultado, la puntuación, la animación de entrada ni los sonidos.

## Cinemática

- Las 10 fases vuelven a sus tiempos originales. Medido en PlayMode: 44.3 s (español, Explorador) y 43.3 s (inglés, Titán).
- Se conserva de la versión 7: las frases de control empiezan con su toma y nunca suenan dos a la vez.

## Un fallo anterior que apareció

En Opciones las marcas de las casillas tenían tamaño negativo y no se veían nunca. Arreglado por código (`OpcionesUI.ArreglarMarca`), con su prueba.

## Falta para cerrar

- Que Diego vea el piloto en su navegador y diga si se extiende al cohete, la plataforma y el lanzamiento (fase 2).
- FPS en un equipo con tarjeta gráfica.
- En el navegador solo se jugó el fracaso. La victoria y el parcial se comprobaron en el editor y con pruebas.
- No se probó en el navegador a 1366×768 ni en inglés en esta ronda (sí en el editor).
