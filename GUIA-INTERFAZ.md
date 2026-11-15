# Guía para modificar la interfaz

Para Diego. Explica qué se puede cambiar en las pantallas, dónde está cada parámetro y qué no hay que romper. Valores vigentes al 2026-11-14 (fase de arte: fuentes, fotos de NASA, nave nueva, animaciones y audio preparado).

## Quién toca qué

| Escena | Cómo se cambia | Quién |
|--------|----------------|-------|
| `Assets/Escenas/Menu.unity` | A mano en el editor de Unity (Hierarchy e Inspector) | Diego |
| `Assets/Escenas/Mision.unity` (Briefing, Configuración, Revisión, Operación) y `Assets/Escenas/Resultado.unity` | Cambiando los parámetros de `Assets/Editor/ConstructorEscenas.cs` y ejecutando **Herramientas > Construir escenas (sin Menu)** | Adrian/Claude. No las edites a mano hasta acordar el cambio: el constructor las rehace desde cero y se perderían tus cambios |

Dos opciones rehacen el Menú desde cero y **borran tus cambios**; úsalas solo si el Menú queda inservible o para partir del diseño nuevo:
- **Herramientas > Diseñar Menu estilo HUD**: el Menú actual (pantalla de título, lista a la izquierda, nave girando sobre Marte, Opciones).
- **Herramientas > Recuperar Menu original**: el Menú de la fase 5 (columna centrada).

Otras opciones:
- **Herramientas > Recrear fuentes de la interfaz**: vuelve a generar las fuentes TextMeshPro de `Assets/Arte/Fuentes`. Úsala si añades caracteres o cambias una fuente.

## Dirección de arte

Referencias: Starfield (editor de nave y personaje), una tabla de acciones de misión espacial y Vehicle Designer. Las reglas:
1. **Datos reales como protagonistas:** cada pantalla lleva una foto real de Marte (NASA/JPL), oscurecida, con un tinte frío y viñeta. Créditos en `CREDITOS.md` y en pantalla.
2. **Colores con significado:** el rojo marciano (acento) es para acciones, el cian es para datos e indicadores y el semáforo, para el estado.
3. **Jerarquía con tipografía:** títulos en Rajdhani, texto en Barlow y datos en IBM Plex Mono; iconos de Lucide dentro del texto.
4. **Todo responde:** los botones crecen al pasar el cursor, las vistas entran con un fundido, las barras se animan y la nave «instala» cada instrumento. Nada de esto retrasa la lógica.
5. **Accesibilidad:**
   - Fuente mínima de 24 pt.
   - El color nunca va solo: lo acompañan iconos y textos.
   - Opción «Reducir movimiento».
   - El juego se puede jugar entero con el teclado.

## Paleta

| Uso | Color |
|-----|-------|
| Fondo | `#0B0B10` |
| Tarjetas y paneles | `#1E1E23` (sobre la escena 3D, al 90 %) |
| Opción sin elegir y pestaña inactiva | `#2A2A31` |
| Opción elegida | `#5A2614` |
| Caja de ayuda | `#15151A` al 95 % |
| Texto principal | `#F8FAFC` |
| Texto secundario | `#94A3B8` |
| Acento (botones principales, etiquetas, pestaña activa) | `#E2643A` |
| Texto sobre el acento | `#0B0B10` |
| Cian: datos, iconos de datos, barras de recursos y anillo de órbita | `#5EC8E5` |
| Fondo de las barras | `#15161C` |
| Líneas HUD (marcos, subrayados) | `#3B5568`, 2 px |
| Escuadra de las esquinas | Acento, 48 x 4 px |
| Semáforo | Gris `#9CA3AF`, Rojo `#EF4444`, Amarillo `#EAB308`, Verde `#22C55E` |

## Fuentes e iconos

| Uso | Fuente (TextMeshPro, en `Assets/Arte/Fuentes`) |
|-----|-----------------------------------------------|
| Títulos, botones y texto en negrita de 40 pt o más | `Titulos SDF` (Rajdhani Bold) |
| Etiquetas en negrita de menos de 40 pt | `Etiquetas SDF` (Rajdhani SemiBold) |
| Texto corrido (con `<b>` usa Barlow SemiBold de verdad) | `Texto SDF` (Barlow Regular) |
| Pasos, datos y créditos de las miniaturas | `Mono SDF` (IBM Plex Mono Medium) |
| Iconos | `Iconos SDF` (Lucide), como fuente de reserva de las demás |

- `ConstructorEscenas.Texto` elige la fuente sola según el tamaño y la negrita. Rajdhani es estrecha, así que se agranda un 8 %.
- **Iconos:** están en `Assets/Scripts/UI/Estilo/Iconos.cs` (masa, potencia, costo, espacios, órbita, lanzador, instrumentos, semáforo, etc.). Basta con escribir el carácter dentro de cualquier texto, por ejemplo `Iconos.Masa + " 100 kg"`.
- **Añadir un icono:** busca su código en `codepoints.json` de `lucide-static`, añádelo a `Iconos` (también a `Todos`) y usa **Recrear fuentes de la interfaz**.

## Escena Menu (la editas tú)

Resolución de referencia 1920x1080 (Canvas > Canvas Scaler, «Scale With Screen Size», match 0.5). Los porcentajes son anclas del RectTransform (mín x, mín y → máx x, máx y).

| Objeto (Hierarchy) | Qué es | Valores actuales |
|--------------------|--------|------------------|
| `Main Camera` | Cámara de la escena 3D, con posprocesado | Posición 0, 0.2, -8.5; fondo `#080D17` |
| `Estrellas` | Cielo de estrellas en un plano lejano; se mueve un poco con el ratón (`Parallax`) | Posición 0, 0.2, 60; escala 128 x 72 |
| `Marte`, `TormentaPolvo` y `Atmosfera` | Planeta con el shader `Planeta` (mapa de 8K, relieve, detalle), la capa de la tormenta y el borde de atmósfera; gira despacio | Posición 6.6, 0.1, 9; diámetro 13.5 |
| `SolEclipse` | El Sol asomando por el borde de Marte (brillo aditivo que respira, `BrilloRespira`) | Detrás y arriba a la derecha del planeta |
| `OrbitaMenu` (+ `Brillo`, `Estela`) y `SateliteEnOrbita` | Órbita cian con halo y estela; el MRO de NASA la recorre (`OrbitaDecorativa`) | Semieje 9.6, excentricidad 0.18 |
| `Anomalia` | Pulso de la región de la anomalía cada 6 s (`PulsoAnomalia`) | Sobre la cara visible |
| `Sol`, `LuzContorno`, `PostProceso` | Luz cálida de lado, luz fría de contorno; Bloom, ACES, viñeta | Intensidad 2.1 y 1.1 |
| `Audio` | Música del Menú y ambiente de la sala de control | Música: Menú |
| `Canvas/Velo` | Franja oscura detrás del texto, con línea a la derecha | Anclas 0,0 → 0.44,1 |
| `Canvas/Contenido` | Etiqueta, título, lema, premisa, `BotonIniciar` y la fila `Secundarios` (`BotonComoJugar`, `BotonOpciones`, `BotonCreditos`) | Anclas 0.05,0.16 → 0.41,0.9 |
| `Canvas/AvisoSenal` y `Canvas/Objetivo` | «SEÑAL ANÓMALA DETECTADA» y «OBJETIVO · MARTE» | Arriba y abajo a la derecha |
| `Canvas/Patrocinio` | «PATROCINADO POR» con los dos logos (`FranjaPatrocinio`) | Anclas 0.05,0.055 → 0.41,0.14 |
| `Canvas/Pie` | Aviso de valores ficticios (corto; el detalle está en Créditos) | 14–18 pt |
| `Canvas/Teclas` | «[ ENTER ] Seleccionar · [ ↑ ↓ ← → ] Moverse» | 18–22 pt |
| `Canvas/Opciones` | Script `OpcionesUI` (volúmenes y «Reducir movimiento») | No lo borres |
| `Canvas/PanelOpciones`, `PanelComoJugar`, `PanelCreditos` | Ventanas, ocultas al empezar | |
| `Canvas/PantallaTitulo` | Título animado (letra a letra, línea, lema, logos) con viñeta. Al pulsar: destello y los bloques del Menú entran escalonados. Se ve una vez por sesión y permite el audio en el navegador | Último hijo del Canvas |
| `MenuUI` | Script que conecta los botones | No lo borres |
| `EventSystem` | Necesario para los clics | Debe haber exactamente uno |

**No rompas esto** (las pruebas lo comprueban):
- El objeto `MenuUI` y sus cuatro referencias en el Inspector: `Boton Iniciar`, `Boton Como Jugar`, `Boton Cerrar Como Jugar` y `Panel Como Jugar`. Si borras un botón y haces otro, vuelve a arrastrarlo a su campo.
- El botón se tiene que seguir llamando `BotonIniciar`: la prueba del recorrido lo busca por ese nombre.
- Un solo `EventSystem` con «Input System UI Input Module» (el proyecto usa el Input System nuevo; el «Standalone Input Module» no funciona).
- El Canvas Scaler en 1920x1080 «Scale With Screen Size».
- El orden al final del Canvas: `PanelOpciones`, `PanelComoJugar` y `PantallaTitulo` (lo último tapa lo anterior).

Puedes cambiar libremente: colores, tamaños, posiciones, textos, añadir imágenes de fondo o decoración (propias, CC0 o de NASA, anotando la fuente en `CREDITOS.md`).

## Escenas Mision y Resultado (por parámetros)

Todo está en `Assets/Editor/ConstructorEscenas.cs`. Busca el nombre entre comillas para encontrar la línea. Después de cambiar algo: **Herramientas > Construir escenas (sin Menu)**.

**Piezas comunes**

- **Paleta:** al principio del archivo (`Fondo`, `Tarjeta`, `TextoClaro`, `TextoSecundario`, `Acento`, `TextoSobreAcento`, `Cian`, `LineaHud`, `TarjetaTranslucida`, `CajaAyuda`, `FondoBarra`).
- **Estilo HUD:** `GrosorLinea` (2), `LargoEsquina` (48) y `GrosorEsquina` (4). Piezas: `MarcoHud`, `Bordes`, `Esquina`, `Encabezado` (28 pt, subrayado), `Linea`, `FondoFoto` (foto a pantalla completa con líneas de barrido), `Miniatura` (imagen con pie) y `Aparecer` (animación de entrada de la vista).
- **Pasos:** `NombresPasos`. `Ruta` arma la barra de pasos del Briefing y `Paso` el «PASO n DE 4» de las demás vistas.
- **Botones (método `Boton`):**
  - Alto 88, ancho 520, texto de 34 pt en mayúsculas.
  - Sprite con chaflán (`Assets/Arte/Interfaz/Chaflan_*.png`, 9-slice).
  - Los secundarios llevan marco fino.
  - Todos tienen `BotonVivo`: crecen al pasar el cursor o con el foco del teclado, se hunden al pulsar y suenan.

**Briefing** (método `ConstruirMision`)

- Fondo con la foto de la torre de polvo (`Fondos/Fondo_Briefing.jpg`).
- Barra de pasos arriba (24 pt, mono).
- Columna izquierda con anclas 0.05,0.15 → 0.51,0.885:
  - Título 64 pt.
  - Miniatura «REGIÓN OBSERVADA» (HiRISE), de 120 px.
  - Objetivo 34 pt.
  - Requisitos 30 pt.
  - «NIVEL DE DIFICULTAD» con los tres botones de nivel (64 px); los textos y colores los pone `MisionUI` con los datos.
- Columna derecha con anclas 0.55,0.15 → 0.95,0.885: tarjetas LÍMITES y EL SEMÁFORO (28 pt).
- Abajo, la pista (26 pt) y «Continuar».

**Configuración** (método `ConstruirConfiguracion`)

- **Panel izquierdo** (anclas 0.015,0.03 → 0.36,0.97):
  - Título 48 pt.
  - Pestañas con icono, de 72 px de alto y texto de 30 pt que baja solo hasta 24 pt.
  - Tarjetas de opción de 78 px: la casilla, el icono y el nombre arriba, y los «chips» de datos con icono abajo (el texto lo arma `PresentacionMision.TextoOpcion`).
  - Ayuda de 24 pt.
  - «Ver briefing», de 64 px.
- **Panel derecho** (anclas 0.655,0.03 → 0.985,0.97):
  - Barras de 20 px, en cian y segmentadas en 10 bloques, que se animan (`BarraAnimada`). Al pasarse del límite se ponen rojas y suenan.
  - Luz del semáforo redonda de 64 px, con icono y halo (`LuzAnimada`), que late y sacude el panel al pasar a rojo.
  - Estado de 48 pt, mensaje de 26 pt y «Revisar misión».
- **Centro (0.36 a 0.655):** queda libre para la escena 3D. Arriba, «PASO 2 DE 4»; abajo, la leyenda sobre un fondo oscuro.

**Revisión, Operación y lanzamiento**

- **Revisión** (método `ConstruirRevision`): tarjeta con anclas 0.25,0.08 → 0.75,0.92, con «Corregir» y «Lanzar».
- **Operación** (método `ConstruirOperacion`, la fase de juego de Adrian), sobre un fondo de estrellas:
  - Izquierda (anclas 0.012,0.03 → 0.30,0.97): instrumentos (filas de 64 px), energía con barra segmentada, órbitas y «ÚLTIMA SEÑAL». Esta última muestra la imagen real (NASA) del instrumento que envió la última transmisión, con su crédito y el destello «SEÑAL RECIBIDA» (`VisorEvidencia`).
  - Centro (0.31 → 0.69): el paso, el mapa cuadrado con marco HUD y la caja de la última lectura.
  - Derecha (0.70 → 0.988): transmisión (24 pt, con efecto de máquina de escribir: `Escritura`), progreso en cian, eventos, herramientas y «Terminar misión».
  - Las imágenes y créditos por instrumento están en `Assets/Arte/Arte.asset`.
- **Lanzamiento** (`ConstruirEfectoLanzamiento`): destello «LANZAMIENTO» encima de la Operación al pulsar «Lanzar». La música cambia a la de la operación (`musica_investigacion`).

**Resultado** (método `ConstruirResultado`)

- Fondo con Marte durante la tormenta.
- Columna izquierda: título de 80 pt en mayúsculas, conclusión de 32 pt, recursos y «Modificar mi misión» (ese texto exacto lo comprueban las pruebas).
- Tarjeta derecha: evidencias, lo que faltó, los puntos y una miniatura de Marte.

**Escena 3D**

- **Nave** (método `ConstruirNave`):
  - Bus hexagonal con lámina dorada, antena parabólica, alas solares con celdas y toberas encendidas.
  - Un módulo por instrumento (`Modulo_<id>`, con la forma de cada instrumento y el acento encendido) que se «instala» al elegirlo (`Instalacion`).
  - Materiales en `Assets/Materiales/Nave_*`; mallas en `Assets/Arte/Mallas`.
  - En Mision está en 0, 0.55, 0 con escala 0.5.
- **Marte** (método `ConstruirMarte`): esfera de alta resolución (`Esfera_Planeta`), textura de Solar System Scope a 4096 px con relieve (`Marte_Normal`), capa de polvo de la tormenta de 2018 (`Marte_Polvo`, shader `CapaPolvo`) y borde de atmósfera. En Configuración está en 0, -17.5, 16, con diámetro 26 y el anillo cian de la órbita elegida (`AnilloOrbita`: polar, media inclinada o elíptica, con un marcador que la recorre).
- **Cohetes** (método `ConstruirCohetes`, en la plataforma `Plataforma` = 70, 0, 0): Explorador inspirado en el Soyuz y Titán inspirado en el SLS de Artemis II, a escala aproximada entre ellos, hechos con mallas de revolución (`Torno`). El anillo de acento bajo cada uno se enciende y el cohete gira cuando está elegido.
- **Cámara de Configuración** (`CamaraConfiguracion`, en la cámara principal): pestaña Órbita, el planeta; pestaña Lanzador, la plataforma; pestaña Instrumentos, la nave. Al instalar un instrumento, la cámara se acerca a su módulo y una etiqueta «INSTALADO AQUÍ» lo señala. Los encuadres se asignan en `ConstruirMision` (`AsignarEncuadre`).
- **Navegación de Configuración** (`GuiaConfiguracion`): ✓ en cada pestaña ya completada, instrucción con el número de paso (versión 6: «1 · ELIGE EL COHETE», después satélite, instrumentos y órbita), botón «Siguiente», leyenda según la pestaña y teclas 1 a 4.
- **Órbita única (versión 7)** (`AnilloOrbita`, `Trayectoria`): en el visor solo se dibuja la órbita elegida; sin elección, ninguna. El nombre y la altitud van en el rótulo fijo bajo el planeta (`GuiaConfiguracion.RotuloOrbita`), y `CamaraConfiguracion.EncajarOrbita` aleja o acerca la cámara para que planeta y órbita quepan entre los paneles. Para añadir una órbita: su asset, una fila en la tabla de `Trayectoria` y su par en `Traducciones`.
- **Luz y posprocesado:** `ConstruirIluminacion` (Sol en 24, 38, 0) y `PerfilPostProceso` (Bloom con umbral 1.15 e intensidad 0.55: solo brilla lo encendido). Los valores del perfil se aplican cada vez que se construye.
- **Semáforo:** el halo de la luz es suave a propósito (alfa 0.22, apenas sobresale de la luz).

Colores de las opciones elegidas y de las barras: en `Assets/Scripts/UI/ConfiguracionUI.cs` (`ColorElegida`, `ColorNormal`, `ColorPestanaActiva`, `ColorExcedida`, `ColorBarra`). Colores del semáforo: `Assets/Scripts/UI/PresentacionMision.cs` (`ColorSemaforo`).

## Animación y audio

- **Código:** todo está en `Assets/Scripts/UI/Estilo`. Las animaciones solo cambian alfa, escala o posición y nunca retrasan la lógica. Las pruebas PlayMode pulsan los botones un cuadro después de cada acción, así que esto no se puede romper.
- **Reducir movimiento:** con la opción activada (Menú > Opciones) no hay sacudidas, parallax ni máquina de escribir.
- **Audio:** ver `AUDIO-ELEVENLABS.md`. Los archivos van en `Assets/Arte/Audio` con nombres fijos; sin ellos el juego queda en silencio.

## Textos

- Todos en español.
- **No escribas números del juego en la interfaz** (créditos, kg, W, espacios): salen de `Assets/Datos`. Las pruebas lo comprueban en el Briefing y en las barras.
- Los requisitos del Briefing (`Assets/Scripts/UI/TextosBriefing.cs`) son del diseño (decisión 13): no pueden nombrar instrumentos. Si los cambias, las pruebas fallan a propósito.
- Los textos de las transmisiones están en `Assets/Datos/Transmision_*.asset` y son los de `Transmisiones.md`. Si se cambian, que sea en los dos sitios.
- Ningún campo ni propiedad del código puede llevar «semaforo» en el nombre (lo comprueba una prueba): para el sonido del semáforo se usan `luzVerde` y `luzRoja`.

## Antes de hacer commit

1. `git pull` antes de empezar, para trabajar sobre lo último.
2. Window > General > **Test Runner** > EditMode > **Run All** y después PlayMode > **Run All**. Todo tiene que salir en verde.
3. Pulsa Play desde `Menu` y recorre: título, Menú, Opciones, Cómo jugar, Iniciar misión, Briefing, Configuración, Revisión, Lanzar, Operación y Resultado.
4. Commit solo de lo que tocaste (por ejemplo `Assets/Escenas/Menu.unity` y las imágenes nuevas con sus `.meta`), mensaje en español, y push.
5. Avisa a Adrian si cambiaste algo de `Mision`, para no pisarnos.

## Pendiente

- **Audio:** generado con ElevenLabs en tres tandas (`AUDIO-ELEVENLABS.md`). Falta escucharlo en el juego.
- **Logos del patrocinador (hecho, 2026-11-14):** National Bulwark Services, sin fondo (`Assets/Arte/Patrocinio`). Están en el splash, la página de carga, el título, el Menú (abajo a la izquierda), Resultado y los dos cohetes. Para cambiarlos, ver `Herramientas/logos.py`.
