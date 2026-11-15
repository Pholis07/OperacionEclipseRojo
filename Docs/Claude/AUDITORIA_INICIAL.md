# Auditoría inicial — Eclipse Ink (versión 8)

Fecha: 2026-11-15. Hecha en solo lectura antes de cambiar nada, como pide `00_EMPIEZA_AQUI_FABLE_5_2.md`.
Todo lo de esta página se comprobó en los archivos, salvo lo marcado como **inferido**.

## Proyecto

- **Raíz de Unity:** confirmada (`Assets/`, `Packages/manifest.json`, `ProjectSettings/`).
- **Unity:** 6000.6.9f1. **Destino:** WebGL.
- **Render:** URP 17.6.0. No hay shaders de Shader Graph propios; los seis shaders del proyecto son HLSL a mano (`Assets/Arte/Shaders/`).
- **Sin Cinemachine ni Timeline.** Las cinemáticas las lleva un director propio por corrutinas (`DirectorCinematica.cs`).
- **Git al empezar:** rama `feat/alcance-ayuda-informe` (versiones 6 y 7, sin subir), árbol limpio. Sin seguir: `Assets/_Recovery/` y `Data/`, que no se tocan.

## Calidad y pipeline (lo que decide la técnica del contorno)

| | WebGL (calidad «Mobile») | Editor (calidad «PC») |
|---|---|---|
| Asset | `Assets/Settings/Mobile_RPAsset.asset` | `Assets/Settings/PC_RPAsset.asset` |
| Modo | Forward | Forward+ |
| Renderer features | ninguna | SSAO |
| Textura de profundidad | no | sí |
| MSAA | no | no |

El WebGL no tiene textura de profundidad ni renderer features. Un contorno de pantalla habría exigido cambiar el pipeline. Por eso el contorno es de **malla invertida**: una pasada más dentro del propio shader, sin tocar el pipeline, las capas ni los paquetes.

Todos los objetos están en la capa 0 y no hay capas propias. Tampoco se añadieron.

## Escenas y cómo se construyen

- Tres escenas: `Menu`, `Mision`, `Resultado`.
- `Menu.unity` se edita a mano y no se regenera. `Mision` y `Resultado` las genera `ConstructorEscenas.ConstruirTodo`.
- No hay prefabs ni FBX. Los modelos son OBJ de NASA 3D Resources (cinco satélites y el SLS), cinco instrumentos del kit del equipo, y piezas hechas por código (cohete Explorador, CubeSat, torre, árboles).
- **Todos los materiales opacos salen de `CrearMaterial`** (`ConstructorEscenas.cs`), siempre con URP/Lit. También los de la Tierra, el Sol, el marcador de órbita y los logos. Por eso el estilo se aplica por lista explícita de nombres.
- `TexturasTests` y `AnilloOrbita` usan los nombres de propiedades de URP/Lit. El shader nuevo los conserva.

## Flujo real del juego

Briefing → Configuración (lanzador, satélite, instrumentos, órbita) → Revisión → Carga → cinemática de 10 fases → Operación por meses, con escaneos → informe (`Resultado`) con tres resultados → «Modificar mi misión».

- Órbita: `EstadoJuego.Configuracion.OrbitaId`. Ya se dibuja solo la elegida (versión 7, `AnilloOrbita` y `OrbitasTests`).
- Instrumentos: los cinco se ven montados en la nave según la selección (`NaveUI`).
- Escaneo: cada uno recoloca la cámara en la pose ancla, así que no acumula deriva (`EscenarioOrbital`, `SecuenciaEscaneo`). Un instrumento que no va a bordo no se puede encender.
- Audio: los 29 audios del paquete ya existen, incluidas las tres músicas de resultado. No hizo falta generar nada.

## Diferencias entre el paquete y el juego

| Tema | Paquete | Juego | Qué se hizo |
|---|---|---|---|
| Órbitas ecuatorial y areoestacionaria | Propuestas; no activarlas sin acuerdo del equipo | Jugables desde la versión 7, por decisión de Diego | No se tocan. **Pendiente de que Adrian las revise** |
| Duración de la cinemática | ~49 s es la referencia; no recortar | 29.7 s (la versión 7 la acortó siguiendo el paquete anterior) | Se restauraron los tiempos: 44 s medidos |
| Valores de masa, potencia y costo | Los del plan maestro | Los de las versiones 5 y 6 (datos NASA y alcance por frenado) | No se tocan |
| AudioMixer con buses | Sugerido | Volúmenes por preferencia y atenuación propia | No se cambió |

## Problemas vistos en las capturas base

- El satélite es casi negro contra el espacio: los metales de URP/Lit dependen de reflejos que en el espacio no hay.
- En el informe, un velo oscuro del 74 % tapaba Marte y la nave medía casi nada. En el fracaso la tormenta subía al 95 % y borraba el relieve.
- Los detalles del informe eran párrafos.
- `GUIA-INTERFAZ.md` describía un informe de una sola vista.

## Línea base medida

- 298 pruebas EditMode y 23 PlayMode en verde.
- Build WebGL: 57 MB.
- 31 capturas «Original» con cámara fija (satélite sobre Marte con tres satélites, Configuración, informe en los tres resultados y Menú).
- **No medido:** FPS en un navegador con tarjeta gráfica. El navegador de prueba dibuja por software.
