# Audio con ElevenLabs

**Estado: generado el 2026-11-14** (23 archivos, unos 6,700 créditos del plan Starter). Todo está en el flow «Operación Eclipse Rojo · Audio del juego» de la cuenta de ElevenLabs de Diego: desde ahí se puede regenerar una pista o probar otra voz y volver a descargarla con el mismo nombre.

- Voz de las transmisiones: «César Augusto — Spanish (Neutral)» (biblioteca de ElevenLabs), modelo Multilingual v2, con el filtro «Bright Phone» (Audio Effects) para el sonido de radio.
- Solo se grabaron TX-00 a TX-05: son las que se reciben en la Operación. Las TX-A son texto del informe y no se oyen.
- **Importante para compilar WebGL en Linux:** Unity convierte el audio a AAC con `ffmpeg`, y tiene que estar instalado (`sudo apt install ffmpeg`). Sin él, los audios fallan al importarse para WebGL y el juego queda en silencio.

El código del audio funciona en silencio si falta algún archivo. Para cambiar uno: genera el audio, ponerlos en `Assets/Arte/Audio` con el nombre exacto de abajo y volver a construir las escenas (**Herramientas > Construir escenas (sin Menu)** y **Herramientas > Diseñar Menu estilo HUD**). `ConstructorEstilo.Arte()` los enlaza en `Assets/Arte/Arte.asset` por nombre.

## Antes de generar

- **Licencia:** revisar el plan de ElevenLabs. El plan gratuito pide atribución y puede no permitir uso comercial. Anotar en `CREDITOS.md` el plan usado y la atribución.
- **Voces:** solo voces de la biblioteca de ElevenLabs. Nunca clonar la voz de una persona real.
- **Formato:** MP3 u OGG. La música en estéreo; los efectos y las voces en mono.
- **WebGL no admite filtros de audio en Unity:** el efecto de radio de las voces tiene que ir dentro del archivo.

## Lista de archivos

### Música (`Assets/Arte/Audio/Musica/`)

| Archivo | Dónde suena | Indicación |
|---|---|---|
| `musica_menu` | Menú (bucle) | Ambient espacial lento, sintetizadores cálidos, misterio, sin percusión fuerte, 90 s, bucle sin cortes |
| `musica_configuracion` | Briefing y Configuración (bucle) | Misma tonalidad que el menú, pulso suave y concentrado, sensación de taller de ingeniería, 90 s en bucle |
| `musica_investigacion` | Operación, tras «Lanzar» (bucle) | Más tensión: drones graves, arpegio lejano, sensación de señal que llega de muy lejos, 90 s en bucle |
| `musica_victoria` | Resultado: misión cumplida | Cierre luminoso y esperanzador, 15 s |
| `musica_parcial` | Resultado: parcial | Cierre agridulce y abierto, 15 s |
| `musica_fracaso` | Resultado: fracaso | Cierre sobrio y melancólico, sin dramatismo, 15 s |

### Efectos (`Assets/Arte/Audio/Efectos/`)

| Archivo | Cuándo suena | Indicación |
|---|---|---|
| `ui_hover` | Cursor o foco del teclado sobre un botón | Tic de interfaz futurista, muy corto y suave, < 0.1 s |
| `ui_click` | Pulsar un botón | Clic de interfaz futurista, limpio, < 0.2 s |
| `ui_elegir` | Un instrumento se instala en la nave | Acople mecánico ligero con zumbido eléctrico, < 0.4 s |
| `ui_quitar` | Quitar un instrumento | Desacople suave, < 0.3 s |
| `ui_pestana` | Cambiar de pestaña | Deslizamiento corto de interfaz, < 0.2 s |
| `ui_error` | Una barra pasa de su límite | Aviso grave corto, no estridente, < 0.4 s |
| `ui_luz_verde` | El semáforo pasa a verde | Confirmación ascendente de dos notas, < 0.5 s |
| `ui_luz_roja` | El semáforo pasa a rojo | Alerta descendente de dos notas, < 0.5 s |
| `lanzamiento` | Pulsar «Lanzar» | Encendido de motores y despegue que se aleja, 2 a 3 s |
| `senal_entrante` | Llega cada transmisión | Estática de radio breve con un pitido de enlace, < 1 s |
| `ambiente_tormenta` | Debajo de la música del Menú y la misión (bucle) | Viento de Marte, polvo, grave y continuo, 60 s en bucle |

### Voces (`Assets/Arte/Audio/Voces/`)

Una por transmisión, con el texto exacto de `Assets/Datos/Transmision_<id>.asset` (campo `texto`). Nombre: `voz_<id>`, por ejemplo `voz_TX-01`.

- Ids: `TX-00`, `TX-01`, `TX-02`, `TX-03`, `TX-04`, `TX-05`, `TX-A1`, `TX-A2`, `TX-A3`, `TX-A4` y `TX-A5`.
- Voz: control de misión, español latino neutro, tono sereno y claro, ritmo pausado.
- Efecto de radio dentro del archivo: filtro de paso de banda y una estática ligera.
- Los subtítulos siempre se ven: la transmisión escrita es la fuente y la voz es un extra.

## Dónde está el código

- **`Assets/Scripts/UI/Estilo/MusicaEscena.cs`:** música y ambiente de cada escena, con entrada suave. En Resultado elige el tema según el resultado.
- **`Assets/Scripts/UI/Estilo/ReproductorUI.cs`:** efectos de la interfaz y voces.
- **`Assets/Scripts/UI/Estilo/Preferencias.cs`:** volúmenes de música, efectos y voz, y «Reducir movimiento». Se cambian en Menú > Opciones.
- En WebGL el navegador no deja sonar nada hasta el primer clic o tecla. La pantalla de título («Pulsa cualquier tecla») resuelve eso.
