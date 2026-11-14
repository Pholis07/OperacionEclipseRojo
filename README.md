# Operación Eclipse Rojo

Juego de un jugador hecho para el NASA Space Apps Challenge 2026 (reto Space Mission Design Game), equipo GangBangers.

Una señal térmica rara aparece en una región de Marte y una tormenta de polvo dificulta la investigación. El jugador diseña una misión: elige órbita, lanzador e instrumentos sin pasarse de los límites de masa, potencia, costo y espacios, y recibe un informe que explica el resultado. Los indicios del juego no son prueba de agua líquida actual ni de vida.

Los valores del juego son ficticios: no son capacidades ni precios oficiales de NASA.

## Requisitos

- Unity 6000.6.9f1 (Unity 6 LTS) con WebGL Build Support.

## Estructura

- `Assets/Scripts/Datos`, `Logica`, `UI`: código del juego (assembly `SpaceMission`).
- `Assets/Datos`: ScriptableObjects con los valores.
- `Assets/Escenas`: `Menu`, `Mision` y `Resultado`.
- `Assets/Tests/EditMode` y `Assets/Tests/PlayMode`: pruebas.
- `Assets/WebGLTemplates/PantallaCompleta`: plantilla web del build.
- `Herramientas/`: scripts (arte, favicon y preparación de GitHub Pages).
- `respaldo-nginx/`: respaldo local con Podman.

## Cómo se juega

1. **Briefing:** lees el objetivo y los requisitos, y eliges el nivel (Entrenamiento, Misión estándar o Tormenta severa).
2. **Configuración y revisión:** eliges órbita, lanzador e instrumentos. El semáforo avisa si te pasas de un límite o si te falta evidencia.
3. **Operación:** enciendes instrumentos sin pasarte de la energía, escaneas el mapa de Marte para encontrar la anomalía y resuelves los eventos de la nave con sus herramientas. Si la nave se apaga, pierdes tiempo y créditos.
4. **Informe:** conclusión, evidencias, recursos usados y puntos.

## Pruebas

En Unity: Window > General > Test Runner, pestañas EditMode y PlayMode, «Run All». El último resultado está en `INFORME-PRUEBAS.md`.

## Publicar

Ver `PUBLICAR.md` (build WebGL, respaldo con nginx en Podman y GitHub Pages).

## Documentos

- `PROPUESTA-GAMEPLAY.md`: reglas de la fase de Operación, niveles y puntos.
- `GUIA-INTERFAZ.md`: cómo cambiar las pantallas.
- `CREDITOS.md`: recursos de terceros y sus licencias.
- `IA.md`: uso de herramientas de IA en el proyecto.
- `AUDIO-ELEVENLABS.md`: audio pendiente.

## Licencia

MIT (ver `LICENSE`) para el código y los recursos propios. Los recursos de terceros conservan su licencia (ver `CREDITOS.md`).
