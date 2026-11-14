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
| `Texturas/Marte_Superficie.jpg` | PIA22329, «Dust Storm Covers Opportunity» (MARCI, MRO, 2018). Mapa global con los huecos entre órbitas rellenados. También es el mapa de la fase de Operación (se muestra una zona distinta en cada misión, sin modificar la imagen) | NASA/JPL-Caltech/MSSS |
| `Evidencias/Evidencia_mision.jpg`, `Fondos/Fondo_Resultado.jpg` | PIA22329 (la misma, ya rellenada) | NASA/JPL-Caltech/MSSS |
| `Fondos/Fondo_Briefing.jpg`, `Evidencias/Evidencia_atmosferico.jpg` | PIA23513, «A Mars Dust Tower Stands Out» (MARCI, MRO) | NASA/JPL-Caltech/MSSS |
| `Evidencias/Evidencia_region.jpg` | PIA26329, «Gullies in the Depths of Hellas» (HiRISE, MRO) | NASA/JPL-Caltech/University of Arizona |
| `Evidencias/Evidencia_termica.jpg` | PIA06399, «Arsia Mons by Day and Night» (THEMIS, Mars Odyssey). Tira de infrarrojo nocturno con **colores añadidos** (paleta térmica) | NASA/JPL/Arizona State University |
| `Evidencias/Evidencia_radar.jpg` | PIA26555, «SHARAD's View of Mars During a 'Very Large Roll'» (SHARAD, MRO) | NASA/JPL-Caltech/ASI/University of Rome/PSI/Smithsonian Institution |
| `Evidencias/Evidencia_alta_res.jpg` | PIA19940, «Possible Sulfates in the Northeast Syrtis Major Region» (HiRISE, MRO) | NASA/JPL-Caltech/Univ. of Arizona |
| `Evidencias/Evidencia_espectrometro.jpg` | PIA20763, «Adding Composition Data About Mars Gullies» (CRISM y HiRISE, MRO), mitad inferior | NASA/JPL-Caltech/UA/JHUAPL |

Los créditos se muestran en el juego junto a cada imagen (Briefing, Operación y Resultado) y en el pie del Menú.

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
- **Generados por código en el constructor:** la nave, el shader de atmósfera y las mallas de `Assets/Arte/Mallas`.

## Audio

Pendiente. Se generará con ElevenLabs (ver `AUDIO-ELEVENLABS.md`). Al añadirlo, anotar aquí el plan de ElevenLabs usado y la atribución que pida su licencia.

## Cómo se generaron las imágenes

`Herramientas/arte.py` (Python 3 con Pillow) se usa así: `python3 -I Herramientas/arte.py <carpeta con las descargas PIA*.jpg> <carpeta de salida>`. Hace lo siguiente:
- Parte de la versión «large» de cada imagen, descargada a mano o con la API de images.nasa.gov.
- Para el mapa de Marte: recorta la fecha y la franja polar dentada, rellena los huecos negros entre órbitas con el color de alrededor y quita marcadores sueltos.
- Para los fondos: desenfoque, tinte frío y viñeta.
- Para la térmica: le aplica la paleta de colores.
