# Cinemáticas, investigación orbital, satélites de NASA y audio: lo que se implementó

**Fecha:** 2026-11-14 · **Rama:** `feat/eclipse-cinematics-audio` (sin fusionar a `main`: falta la revisión del equipo).

Este documento sigue el formato de entrega del paquete de especificaciones (`Operacion_Eclipse_Rojo_Paquete_Completo_Para_Claude.zip`, `00_INICIO/PROMPT_MAESTRO_PARA_CLAUDE.md`). Distingue entre lo **implementado**, lo **compilado**, lo **probado en Play Mode** (pruebas automáticas en batch) y lo **probado en WebGL**.

## 1. Inventario real (auditoría)

| Tema | Estado encontrado |
|---|---|
| Unity y render | 6000.6.9f1, URP 17.6, Input System, uGUI + TextMeshPro. **No hay Cinemachine ni Timeline**: las cámaras se hicieron en C# sin añadir paquetes. |
| Escenas | `Menu`, `Mision` y `Resultado`, las tres acordadas. Briefing, Configuración, Revisión y Operación son vistas de `Mision`. `Mision` y `Resultado` las genera `Assets/Editor/ConstructorEscenas.cs`; `Menu`, `DisenarMenuHud`. |
| Fuente de verdad | `EstadoJuego.Configuracion` (ids), `Evaluador` y `Calculador` (recursos y semáforo), `Operacion` (fase de investigación de Adrian: mapa, energía, eventos y herramientas) y los datos en `Assets/Datos` (`CatalogoDatos`). |
| Lanzamiento | `RevisionUI`: «Lanzar» solo se habilita si `Evaluacion.SePuedeLanzar`. Al pulsar, `MisionUI.MostrarInvestigacion()` activa la Operación en el mismo cuadro, y las pruebas dependen de eso. |
| Investigación | No es la de «Continuar investigación» del documento maestro: es la **Operación** de Adrian, donde se enciende un instrumento y se pulsa en el mapa para escanear. Cada transmisión llega al encontrar la anomalía con el instrumento correcto. |
| Órbitas | `AnilloOrbita` (visual de Configuración). |
| Nave | `NaveUI` (módulos `Modulo_*`). |
| Audio | `ArteDatos` + `MusicaEscena` + `ReproductorUI` + `Preferencias`, con volúmenes por categoría. **No hay AudioMixer.** |
| Kit adjunto (`05_KIT_UNITY_ORIGINAL`) | No estaba en el repo. Ya había equivalentes de `InstrumentLoadoutView`, `OrbitPreview` y `EclipseRojoKitBuilder` (`NaveUI`, `AnilloOrbita` y el constructor), así que **solo se importaron los 5 modelos OBJ** de instrumentos. Los scripts no se copiaron, para no duplicar la fuente de verdad. |
| Pruebas | 136 EditMode y 19 PlayMode (`RecorridoTests`, que pulsa los botones reales). |

### Discrepancias con el documento maestro (no se corrigieron; las decide el equipo)

| Dato | Documento maestro | Juego (datos de Adrian y pruebas) |
|---|---|---|
| Capacidad del Titán | 1,100 kg | **750 kg** (`InterfazTests` lo comprueba) |
| Consumo del espectrómetro | 130 W + 70 W = 200 W | **260 W** (130 + 130). Por eso el caso 7 da 760 W y no 700 W |
| Investigación | Transmisiones con «Continuar investigación» | Operación con mapa, energía, eventos y herramientas (fase 8 de Adrian) |
| Tipo de satélite | No existe | Nuevo (petición de Diego): **solo aspecto**, sin reglas. Las mecánicas las pone Adrian |

### Equivalencias de los eventos del contrato

| Evento sugerido | Equivalente real |
|---|---|
| `MissionValidated` | `Evaluador.Evaluar` → `RevisionUI.Mostrar` (habilita «Lanzar») |
| `LaunchRequested` | `RevisionUI.BotonLanzar.onClick` (lo escuchan `RevisionUI` y `DirectorCinematica`) |
| `LaunchCinematicFinished` | `DirectorCinematica.AlTerminar` (una sola vez por lanzamiento, también al omitir) |
| `InvestigationRequested` | `MapaMarteUI.AlPulsar` → `OperacionUI.EscanearEn` |
| `ScanFinished` | `OperacionUI.AlEscanear` (nuevo: la lógica ya terminó) → `SecuenciaEscaneo` |
| `ScientificTransmissionAcknowledged` | No aplica: en la Operación las transmisiones llegan con los escaneos |

## 2. Qué se hizo y en qué archivos

### Satélites de NASA (pestaña «Satélite»)
- 5 modelos de NASA 3D Resources: MRO, Magellan, Chandra, Aquarius y LLCD. Están convertidos a OBJ en `Assets/Arte/Modelos/NASA` con `Herramientas/modelos_nasa`.
- **Nuevo:**
  - `SateliteDatos` (datos) y `Assets/Datos/Satelite_*.asset`, enlazados en `CatalogoDatos.satelites`.
  - `NaveSatelite`: enciende el cuerpo elegido y lleva cada `Modulo_*` a su montaje en ese satélite.
- **Editado:**
  - `Configuracion.SateliteId` (opcional, fuera de las reglas).
  - `ConfiguracionUI`: pestaña `Satelite`, añadida al final del enum para no cambiar los demás valores.
  - `GuiaConfiguracion`: 4 pestañas y teclas 1 a 4.
  - `CamaraConfiguracion`: encuadre más cercano y giro lento en la pestaña Satélite.
  - `PresentacionMision`: tarjeta y ayuda del satélite.
- **Instrumentos:** son los OBJ del kit, montados sobre el casco con un soporte. Los montajes se calculan lanzando rayos contra la malla de cada satélite (`ConstructorEscenas.Nasa.cs`), así que no flotan.
- **Titán:** ahora es el modelo **SLS de NASA**, pintado por zonas y partido en etapa inferior y superior. El Explorador sigue siendo el Soyuz hecho por código, con la etapa superior agrupada para la separación.

### Menú «Centro de control en alerta» (`DisenarMenuHud`)
- **3D:**
  - Marte entero a la derecha.
  - Órbita cian fina (#69C6D2) con el MRO de NASA recorriéndola (`OrbitaDecorativa`).
  - Pulso de la anomalía cada 6 s (`PulsoAnomalia`).
- **Interfaz:**
  - «MISIÓN ESPACIAL / 01», el título con «Rojo» en acento y «La señal no debería estar ahí.».
  - Botón principal «Iniciar misión» y, debajo, «Cómo jugar», «Opciones» y «Créditos» (nuevo panel, `AbrirPanel`).
  - Aviso «Señal anómala detectada» con un punto que late y la tarjeta «Objetivo: Marte · misión orbital».
- **Opciones:** se añadieron volumen de **Ambiente** y **Silenciar todo** (`AudioListener`).

### Cinemática Tierra → Marte (storyboard A): `DirectorCinematica` + `ConstructorEscenas.Cine.cs`

Son 8 tomas con cortes a negro, unos 26 s en total:
1. Toberas y cuenta atrás.
2. Ignición y despegue, con llama y humo de 400 partículas como máximo.
3. Ascenso: el cielo pasa de azul a negro.
4. Separación de etapas sobre la Tierra.
5. Se libera el satélite elegido con sus instrumentos.
6. Mapa de transferencia de Hohmann orientativo, con «tiempo acelerado» y sin cifras.
7. Aproximación e inserción orbital.
8. Plano héroe en la **órbita elegida**; la cámara termina en el encuadre de la investigación.

Cómo funciona:
- Usa el lanzador, el satélite, los instrumentos y la órbita elegidos.
- Se dibuja con una cámara aparte (`CamaraCine`) en una textura **encima** de todo. La Operación ya está activa debajo, así que la lógica y las pruebas no esperan.
- **«Omitir» o Escape** llevan al mismo final: satélite en el ancla de su órbita, música de investigación, ambiente orbital y efectos cortados.
- Con **Reducir movimiento** no se reproduce.
- Al lanzar se apaga la cámara de Configuración (rendimiento en el navegador).
- `EfectoLanzamiento` (el destello anterior) se quitó: la cinemática lo sustituye.

### Investigación orbital híbrida (storyboard B): `SecuenciaEscaneo` + `EscenarioOrbital`
- Después de cada escaneo, la vista 3D aparece en el lugar del mapa:
  1. Objetivo fijado.
  2. Avance por la órbita, orientando los instrumentos hacia la región.
  3. Escaneo con el efecto del instrumento: barrido térmico, pulsos de radar, enfoque de la cámara, cono de polvo del sensor atmosférico y bandas del espectrómetro.
  4. Datos (el mismo resultado que el texto de lectura).
  5. Regreso al encuadre.
- La región escaneada es **el mismo punto del mapa** sobre el Marte 3D: el planeta gira sobre su eje para ponerla bajo la órbita.
- **Duración:** unos 7 s si llega una transmisión nueva; versión corta (unos 4 s) si no llega.
- **Sin deriva:** cada escaneo empieza y termina en la misma pose de presentación (`EscenarioOrbital.FaseAncla`).
- **Sin escaneos dobles:** mientras se ve la vista, el mapa no recibe clics. Un clic la salta.
- La forma de las órbitas está ahora en un solo sitio, `Trayectoria`: la usan el anillo de Configuración, la cinemática y la investigación.
- **Cambio en el código de Adrian:** un evento en `OperacionUI` (`AlEscanear`) que se lanza al final de `EscanearEn`. No cambia ninguna regla.

### Audio (ElevenLabs)
Ver `AUDIO-ELEVENLABS.md`, «Segunda tanda». Son 16 audios nuevos por 624 créditos:
- Música de la cinemática.
- 13 efectos de la cinemática y del escaneo.
- 2 ambientes en bucle.

Están enlazados por nombre en `Arte.asset` (`ConstructorEstilo.Arte`) y normalizados con ffmpeg. Si falta un archivo, el juego sigue en silencio sin errores.

## 3. Pruebas ejecutadas (resultados reales)

| Prueba | Resultado |
|---|---|
| Compilación (batch, Linux64 y WebGL) | **Compila** sin errores |
| EditMode (batch) | **136 de 136** |
| PlayMode `RecorridoTests` (batch, los 19 casos, con la cinemática en marcha) | **19 de 19**. «Modificar mi misión» conserva la selección y los casos bloqueados no lanzan |
| PlayMode temporal de la cinemática, solo en la copia de trabajo (Titán · elíptica · Chandra y Explorador · polar · MRO) | **2 de 2**. La cinemática dura 26.5 y 27 s y termina sola. Escaneo completo: 7.2 s; corto: 4.3 s. Se revisaron 10 fotogramas de la cinemática y 10 de los escaneos |
| Capturas en batch de todas las pantallas (1920×1080 y 1366×768) | Revisadas |
| Build WebGL (batch, `ConstructorWebGL.Construir`) | **Compila**: 35.3 MB, 0 errores, unos 6 min. **No se abrió en el navegador** |

**No se probó a mano** (con ratón, en el editor ni en el navegador): el sonido real, la sensación de la cámara en movimiento ni el rendimiento en un portátil.

## 4. Pendientes y pasos manuales

1. **Escuchar los audios nuevos** en el juego. Se generaron y midieron, pero no se escucharon a oído.
2. **Probar en el navegador** (servidor HTTP): FPS, que el audio empiece tras el primer clic y que la cinemática se vea bien en 16:9 y en ventanas pequeñas.
3. Adrian:
   - Las **mecánicas de cada satélite**: si cuestan, pesan o cambian algo. Ahora `Configuracion.SateliteId` es solo aspecto.
   - Decidir las discrepancias de la sección 1 (Titán 750/1,100 kg, espectrómetro 260/200 W).
4. `Terreno_Gusev` (cráter de Spirit) está importado pero **sin usar**. Podría servir para la región de la anomalía en el Briefing.
5. P1 sin hacer:
   - Bajar la música mientras habla la voz (ducking).
   - Los audios del CSV que se reutilizaron o quedaron sin generar (ver `AUDIO-ELEVENLABS.md`).
6. Al abrir el proyecto: cerrar Unity antes de `git pull` y, después, **Herramientas > Construir escenas (sin Menu)** solo si se cambia el constructor. Las escenas ya van construidas en la rama.
