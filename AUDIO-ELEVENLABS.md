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

## Segunda tanda: cinemática y escaneo orbital (2026-11-14)

Del paquete de especificaciones del equipo (`02_AUDIO_ELEVENLABS/01_PROMPTS_LISTOS_ELEVENLABS.csv`). Se generaron con las mismas indicaciones en inglés del CSV (ninguna nombra obras, juegos ni autores). Hubo una toma de cada uno: 16 audios por **624 créditos**. Quedaron unos 13,200 créditos del plan.

No se generaron los que ya tenían equivalente en el juego. Se reutilizan estos:

| Del CSV | Se usa |
|---|---|
| `MUS_Menu_Alerta` | `musica_menu` |
| `MUS_Orbital_Ambience` | `musica_investigacion` |
| `SFX_UI_Confirm` y `SFX_UI_Error` | `ui_click`, `ui_luz_verde` y `ui_error` |
| `SFX_UI_Hover` | `ui_hover` |
| `SFX_Transmission_Received` | `senal_entrante` |
| `SFX_Result_*` | `musica_victoria`, `musica_parcial` y `musica_fracaso` |

Quedan sin generar `SFX_Rocket_Ignition`, `SFX_UI_Transition`, `MUS_Revelacion_Breve` y `AMB_Configuracion_Sistemas`. Son P1 y no hacen falta en el flujo actual.

| Archivo | Dónde suena | Ajuste |
|---|---|---|
| `Musica/MUS_Lanzamiento_Cinematica` | Toda la cinemática (25 s) | −18 LUFS |
| `Efectos/SFX_Countdown_System` | Toma 1, cuenta atrás | −20 LUFS |
| `Efectos/SFX_Rocket_Liftoff` | Toma 2, despegue (en la atmósfera) | −20 LUFS |
| `Efectos/SFX_Stage_Separation` | Toma 4, separación (sonido estructural, dentro del vehículo) | −20 LUFS |
| `Efectos/SFX_Solar_Array_Deploy` | Toma 5, se libera el satélite | −20 LUFS |
| `Efectos/SFX_Transfer_Swoosh` | Toma 6, mapa de la transferencia (no diegético) | −20 LUFS |
| `Efectos/SFX_Orbital_Thruster` | Toma 7 y aproximación del escaneo (vibración interna) | −20 LUFS |
| `Efectos/SFX_Satellite_Servo` | El satélite se orienta hacia la región | −20 LUFS |
| `Efectos/SFX_Target_Lock` | «Objetivo fijado» | −20 LUFS |
| `Efectos/SFX_Scan_Thermal`, `_Radar`, `_HighRes`, `_Atmospheric`, `_Spectrometer` | Escaneo con cada instrumento | −20 LUFS (el radar, −17 dB fijos: es un tren de pulsos) |
| `Efectos/AMB_Menu_Control` | Bajo la música del Menú (bucle) | −30 LUFS |
| `Efectos/AMB_Investigacion_Orbital` | Bajo la música de la Operación, tras la cinemática (bucle) | −30 LUFS |

Los sonidos se revisaron con `ffmpeg`: duración, picos, volumen medio y silencios. Venían a niveles muy distintos (algunos efectos a −5 dB de media), así que se normalizaron con `loudnorm` y se les puso un fundido de 5 ms al entrar y de 80 ms al salir, para que no hagan clic. **No se escucharon a oído:** conviene que alguien del equipo los escuche en el juego antes de entregar.

Los motores solo suenan a pleno en la plataforma. En el espacio, los efectos son vibración de la estructura o sonidos de interfaz, nunca sonido que viaje por el vacío.

## Tercera tanda: título, despegue, meses y final (2026-11-14)

18 audios en el mismo flow, una toma de cada uno: **unos 644 créditos** (208 de efectos y 436 de voz: las cuatro frases se generaron dos veces porque el nodo del filtro de radio vuelve a generar la voz que tiene conectada). Normalizados con ffmpeg aplicando solo ganancia (los bucles no pierden la costura) y un limitador.

| Archivo | Dónde suena | Volumen |
|---|---|---|
| `Efectos/SFX_Title_Confirm` | Al cerrar la pantalla de título | −15 LUFS |
| `Efectos/AMB_Launch_Coast` | Ambiente de la costa en las tomas de la plataforma (bucle) | −24 LUFS |
| `Efectos/SFX_Ignition_Sparks` | Chispas antes del encendido | −16 LUFS |
| `Efectos/SFX_Engine_Ignition` | Encendido de los motores | −16 LUFS |
| `Efectos/SFX_Engine_Roar_Close` | Rugido del motor mientras sube por la atmósfera (bucle). Se corta al llegar al espacio | −17 LUFS |
| `Efectos/SFX_Tower_Arms_Release` | Los brazos de la torre se retiran | −16 LUFS |
| `Efectos/SFX_Booster_Separation` | Se sueltan los propulsores laterales (sonido estructural) | −17 LUFS |
| `Voces/Control/VOZ_Encendido`, `VOZ_Despegue`, `VOZ_Separacion`, `VOZ_Orbita` | Control de misión en la cinemática | −16 LUFS |
| `Efectos/SFX_Month_Pass` | Transición «pasa un mes» | −16 LUFS |
| `Efectos/SFX_Stamp` | El sello del resultado | −22 LUFS (transitorio corto) |
| `Efectos/SFX_Outcome_Victory`, `_Partial`, `_Failure` | Remate de cada final | −15 LUFS |
| `Efectos/SFX_Signal_Lost` | Final fallido: se pierde la señal | −16 LUFS |
| `Efectos/SFX_Score_Tick` | El total de puntos contando | −22 LUFS |

Frases de las voces (español neutro): «Secuencia de encendido iniciada. Motores principales, encendidos.» · «¡Despegue! El cohete dejó la torre.» · «Separación confirmada. La etapa superior continúa el ascenso.» · «Inserción orbital completada. Estamos en órbita de Marte.»

**No se escucharon a oído.**

## Voces en inglés (2026-11-15)

El juego pasó a inglés. Se generaron las 6 transmisiones y las 4 frases de control en inglés con «Udem – Calm & Clear Educational Narrator» (voz de biblioteca, inglés americano, serena) y el mismo filtro «Bright Phone»: **1,531 créditos** (solo se ejecutaron los nodos del filtro, que generan la voz una vez). Normalizadas a −16 LUFS. Archivos con sufijo `_en`; las voces en español siguen en el proyecto pero no se enlazan. Los textos exactos están en `Assets/Scripts/UI/Estilo/Subtitulos.cs`, que también da los subtítulos en inglés o en español (Opciones).

**No se escucharon a oído.**

## Dónde está el código

- **`Assets/Scripts/UI/Estilo/MusicaEscena.cs`:** música y ambiente de cada escena, con entrada suave. En Resultado elige el tema según el resultado.
- **`Assets/Scripts/UI/Estilo/ReproductorUI.cs`:** efectos de la interfaz y voces.
- **`Assets/Scripts/UI/Estilo/Preferencias.cs`:** volúmenes de música, ambiente, efectos y voz, «Silenciar todo» (AudioListener) y «Reducir movimiento». Se cambian en Menú > Opciones.
- **`DirectorCinematica.cs` y `SecuenciaEscaneo.cs`:** cada uno tiene su propio AudioSource para sus efectos, así que «Omitir» o salir de la escena los corta sin dejar sonidos sueltos. No usan AudioMixer: el proyecto ya tenía volúmenes por categoría en Preferencias, y crear un mixer desde código no es posible con la API pública.
- En WebGL el navegador no deja sonar nada hasta el primer clic o tecla. La pantalla de título («Pulsa cualquier tecla») resuelve eso.
