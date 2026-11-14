# Créditos y licencias

Todo recurso externo del juego tiene que estar en esta lista con su fuente y su licencia. Si añades una imagen, fuente, icono o sonido, anótalo aquí en el mismo commit.

## Licencia del proyecto

El código y los recursos propios del juego tienen licencia MIT (ver `LICENSE`). Los recursos de terceros de esta lista conservan su propia licencia, indicada en cada fila.

## Motor

El build WebGL incluye el runtime de Unity 6000.6.9f1, que no es de código abierto: se distribuye según los términos de Unity (Unity Personal). Está pendiente confirmar con los organizadores si cuenta como material de terceros según los términos de participación.

## Uso de imágenes de NASA

El juego no usa el logo ni la insignia de NASA: sus normas de uso de imágenes no permiten dar a entender que NASA respalda el proyecto. Las imágenes de NASA/JPL son de uso libre con crédito.

## Imágenes de Marte (NASA)

Descargadas de la NASA Image and Video Library (images.nasa.gov) el 2026-11-14. Las procesa `Herramientas/arte.py` (ver «Cómo se generaron»): recorte, color, desenfoque y viñeta.

| Archivo en `Assets/Arte` | Imagen original | Crédito |
|---|---|---|
| `Texturas/Marte_Polvo.png` | PIA22329, «Dust Storm Covers Opportunity» (MARCI, MRO, 2018). Mapa global de la tormenta con los huecos rellenados, convertido en capa de polvo semitransparente | NASA/JPL-Caltech/MSSS |
| `Evidencias/Evidencia_mision.jpg`, `Fondos/Fondo_Resultado.jpg` | PIA22329 (la misma, ya rellenada) | NASA/JPL-Caltech/MSSS |
| `Fondos/Fondo_Briefing.jpg`, `Evidencias/Evidencia_atmosferico.jpg` | PIA23513, «A Mars Dust Tower Stands Out» (MARCI, MRO) | NASA/JPL-Caltech/MSSS |
| `Evidencias/Evidencia_region.jpg` | PIA26329, «Gullies in the Depths of Hellas» (HiRISE, MRO) | NASA/JPL-Caltech/University of Arizona |
| `Evidencias/Evidencia_termica.jpg` | PIA06399, «Arsia Mons by Day and Night» (THEMIS, Mars Odyssey). Tira de infrarrojo nocturno con **colores añadidos** (paleta térmica) | NASA/JPL/Arizona State University |
| `Evidencias/Evidencia_radar.jpg` | PIA26555, «SHARAD's View of Mars During a 'Very Large Roll'» (SHARAD, MRO) | NASA/JPL-Caltech/ASI/University of Rome/PSI/Smithsonian Institution |
| `Evidencias/Evidencia_alta_res.jpg` | PIA19940, «Possible Sulfates in the Northeast Syrtis Major Region» (HiRISE, MRO) | NASA/JPL-Caltech/Univ. of Arizona |
| `Evidencias/Evidencia_espectrometro.jpg` | PIA20763, «Adding Composition Data About Mars Gullies» (CRISM y HiRISE, MRO), mitad inferior | NASA/JPL-Caltech/UA/JHUAPL |

Los créditos se muestran en el juego junto a cada imagen (Briefing, Operación y Resultado) y en el pie del Menú.

## Mapa de la superficie de Marte

| Archivo en `Assets/Arte` | Origen | Licencia |
|---|---|---|
| `Texturas/Marte_Superficie.jpg` (y `Texturas/Marte_Normal.png`, relieve calculado a partir de ella) | «8k Mars» de Solar System Scope (solarsystemscope.com/textures), basado en datos de elevación e imágenes de NASA. Reducido a 4096 x 2048. Es la superficie del planeta en 3D y también el mapa de la fase de Operación (cada misión muestra una zona distinta, sin modificar la imagen) | Creative Commons Attribution 4.0 (CC BY 4.0). Crédito: Solar System Scope |

## Mapa de la Tierra

| Archivo en `Assets/Arte` | Origen | Licencia |
|---|---|---|
| `Texturas/Tierra_Superficie.jpg` | «2k Earth Day Map» de Solar System Scope (solarsystemscope.com/textures), basado en imágenes de NASA. Se usa en la cinemática de lanzamiento (órbita terrestre y mapa de la transferencia) | Creative Commons Attribution 4.0 (CC BY 4.0). Crédito: Solar System Scope |

## Modelos 3D (NASA 3D Resources)

Descargados de [science.nasa.gov/3d-resources](https://science.nasa.gov/3d-resources/) el 2026-11-14 y convertidos a OBJ con `Herramientas/modelos_nasa` (ver su `LEEME.md`): sin Draco, centrados, escalados y, los más pesados, simplificados. Son de NASA y se usan con crédito según sus guías de medios; NASA no respalda el juego. En el juego son el **aspecto** de la nave y del Titán: las capacidades son ficticias.

| Archivo en `Assets/Arte/Modelos/NASA` | Modelo de NASA 3D Resources | Uso en el juego |
|---|---|---|
| `Sat_MRO.*` | Mars Reconnaissance Orbiter (MRO) (C) | Satélite «Orbitador MRO» y satélite del Menú |
| `Sat_Magellan.*` | Magellan | Satélite «Cartógrafo Magellan» |
| `Sat_Chandra.*` | Chandra X-ray Observatory | Satélite «Observatorio Chandra» |
| `Sat_Aquarius.*` | Aquarius (B) (unfurled) | Satélite «Plataforma Aquarius» |
| `Sat_LLCD.*` | Lunar Laser Communications Demonstration (LLCD) | Satélite «Relé láser LLCD» |
| `Cohete_SLS_Inferior.*`, `Cohete_SLS_Superior.*` | Space Launch System (SLS), modelo para impresión 3D, pintado por zonas | Lanzador Titán (plataforma y cinemática) |
| `Terreno_Gusev.*` | Mars – Spirit Landing Site (cráter Gusev), modelo para impresión 3D | Importado, **sin usar todavía** |

También se revisaron «Space Launch System (SLS) Block 1» (no se usa: trae la base de impresión pegada).

## Modelos de instrumentos

`Assets/Arte/Modelos/Instrumentos/*.obj`: los cinco modelos del kit del equipo (`Operacion_Eclipse_Rojo_Kit_Unity`, «activos originales generados para el proyecto, para su uso y modificación dentro del videojuego del equipo»). Se montan sobre el casco de cada satélite.

## Fuentes

| Archivo en `Assets/Arte/Fuentes` | Fuente | Licencia |
|---|---|---|
| `Rajdhani-Bold.ttf`, `Rajdhani-SemiBold.ttf` | Rajdhani, de Indian Type Foundry (títulos, etiquetas y botones) | SIL Open Font License 1.1 (`OFL-Rajdhani.txt`) |
| `Barlow-Regular.ttf`, `Barlow-SemiBold.ttf` | Barlow, de Jeremy Tribby (texto) | SIL Open Font License 1.1 (`OFL-Barlow.txt`) |
| `IBMPlexMono-Medium.ttf` | IBM Plex Mono, de IBM (pasos y datos) | SIL Open Font License 1.1 (`OFL-IBMPlexMono.txt`) |
| Liberation Sans (TextMeshPro) | Fuente de reserva para caracteres poco comunes | SIL Open Font License 1.1 |

Descargadas del repositorio de Google Fonts (github.com/google/fonts).

## Iconos

| Archivo | Fuente | Licencia |
|---|---|---|
| `Assets/Arte/Fuentes/Lucide.ttf` | Lucide Icons, paquete `lucide-static` 1.73.0 (fuente de iconos) | ISC (`LICENSE-Lucide.txt`) |

## Recursos propios

Hechos para este juego, sin licencias de terceros:
- **Generados por `Herramientas/arte.py`:** el cielo de estrellas (`Texturas/Estrellas.png`), la lámina dorada y las celdas solares de la nave, y los sprites de interfaz (`Interfaz/*`: chaflanes, círculo, halo, segmentos y barrido).
- **Generado por `Herramientas/favicon.py`:** el icono de la pestaña del navegador (`Assets/WebGLTemplates/PantallaCompleta/TemplateData/favicon.png`), Marte con la sombra de un eclipse. Se genera con `python3 -I Herramientas/favicon.py <ruta de salida>`.
- **Generados por código en el constructor:** el cohete Explorador (inspirado en el Soyuz), la plataforma y la torre, la llama y el humo del despegue, el mapa de la transferencia, el shader de atmósfera y las mallas de `Assets/Arte/Mallas`.

## Audio

Todo el audio de `Assets/Arte/Audio` (música, efectos, ambiente y voces) se generó con ElevenLabs el 2026-11-14 desde la cuenta de Diego (plan Starter, de pago, que incluye licencia de uso comercial sin atribución obligatoria). Se anota igualmente como cortesía.

| Archivos | Herramienta de ElevenLabs |
|---|---|
| `Musica/musica_*.mp3` (6 pistas: menú, configuración, operación, victoria, parcial, fracaso) | Eleven Music v2.5, instrumental |
| `Efectos/*.mp3` (11 efectos de interfaz, lanzamiento, señal y ambiente de tormenta) | Sound Effects v2 |
| `Voces/voz_TX-00.mp3` a `voz_TX-05.mp3` (las transmisiones que se reciben en la Operación) | Multilingual v2 con la voz de biblioteca «César Augusto — Spanish (Neutral)», y el filtro «Bright Phone» de Audio Effects para el sonido de radio |

| `Musica/MUS_Lanzamiento_Cinematica.mp3` (cinemática de lanzamiento, 25 s) | Eleven Music v2.5, instrumental |
| `Efectos/SFX_*.mp3` (13 efectos de la cinemática y del escaneo orbital) y `Efectos/AMB_*.mp3` (ambiente de la sala de control y de la nave en órbita, 20 s en bucle) | Sound Effects v2. Generados el 2026-11-14 con las indicaciones de `02_AUDIO_ELEVENLABS/01_PROMPTS_LISTOS_ELEVENLABS.csv` del paquete del equipo; volumen normalizado con ffmpeg |

Las indicaciones usadas están en `AUDIO-ELEVENLABS.md`.

## Cómo se generaron las imágenes

`Herramientas/arte.py` (Python 3 con Pillow) se usa así: `python3 -I Herramientas/arte.py <carpeta con las descargas PIA*.jpg> <carpeta de salida>`. Hace lo siguiente:
- Parte de la versión «large» de cada imagen, descargada a mano o con la API de images.nasa.gov.
- Para el mapa de Marte: recorta la fecha y la franja polar dentada, rellena los huecos negros entre órbitas con el color de alrededor y quita marcadores sueltos.
- Para los fondos: desenfoque, tinte frío y viñeta.
- Para la térmica: le aplica la paleta de colores.
