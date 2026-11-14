# Guía para modificar la interfaz

Para Diego. Explica qué se puede cambiar en las pantallas, dónde está cada parámetro y qué no hay que romper. Valores vigentes al 2026-11-14 (fase 6).

## Quién toca qué

| Escena | Cómo se cambia | Quién |
|--------|----------------|-------|
| `Assets/Escenas/Menu.unity` | A mano en el editor de Unity (Hierarchy e Inspector) | Diego |
| `Assets/Escenas/Mision.unity` (Briefing, Configuración, Revisión, Investigación) y `Assets/Escenas/Resultado.unity` | Cambiando los parámetros de `Assets/Editor/ConstructorEscenas.cs` y ejecutando **Herramientas > Construir escenas (sin Menu)** | Por ahora Adrian/Claude. No las edites a mano hasta acordar el cambio: el constructor las rehace desde cero y se perderían tus cambios. Cuando el juego esté completo se puede pasar también a edición manual, como el Menú |

La opción **Herramientas > Recuperar Menu original** rehace el Menú desde cero y **borra tus cambios**. Úsala solo si el Menú queda inservible.

## Paleta (igual en todas las pantallas)

| Uso | Color |
|-----|-------|
| Fondo | `#0B0B10` |
| Tarjetas y paneles | `#1E1E23` |
| Opción sin elegir y pestaña inactiva | `#2A2A31` |
| Opción elegida | `#5A2614` |
| Caja de ayuda | `#15151A` |
| Texto principal | `#F8FAFC` |
| Texto secundario y barras | `#94A3B8` |
| Acento (botones principales, etiquetas, pestaña activa) | `#E2643A` |
| Texto sobre el acento | `#0B0B10` |
| Horizonte de Marte del Menú | RGBA 0.55, 0.18, 0.08, 0.55 |
| Semáforo | Gris `#9CA3AF`, Rojo `#EF4444`, Amarillo `#EAB308`, Verde `#22C55E` |

Fuente: Liberation Sans (TextMeshPro). Si cambias de fuente, que sea libre (OFL o similar) y avisa para anotarla en los créditos.

## Escena Menu (la editas tú)

Resolución de referencia 1920x1080 (Canvas > Canvas Scaler, «Scale With Screen Size», match 0.5). Los porcentajes son anclas del RectTransform (mín x, mín y → máx x, máx y).

| Objeto (Hierarchy) | Qué es | Valores actuales |
|--------------------|--------|------------------|
| `Canvas/Fondo` | Fondo liso | `#0B0B10`, ocupa toda la pantalla |
| `Canvas/HorizonteMarte` | Franja roja abajo | Anclas 0,0 → 1,0.35 |
| `Canvas/Contenido` | Columna centrada (Vertical Layout Group, espacio 32) | Anclas 0.2,0.15 → 0.8,0.85 |
| `Contenido/Titulo` | «Operación Eclipse Rojo» | 96 pt, negrita, `#F8FAFC`, centrado |
| `Contenido/Premisa` | «Una señal rara aparece en Marte y una tormenta de polvo dificulta la investigación.» | 40 pt, `#94A3B8`, centrado |
| `Contenido/BotonIniciar` | «Iniciar misión» (principal) | 520 x 88, fondo `#E2643A`, texto 40 pt negrita `#0B0B10` |
| `Contenido/BotonComoJugar` | «Cómo jugar» (secundario) | 520 x 88, fondo `#1E1E23`, texto 40 pt `#F8FAFC` |
| `Canvas/PanelComoJugar` | Ventana de «Cómo jugar», oculta al empezar | Negro al 85 %, toda la pantalla |
| `PanelComoJugar/Tarjeta` | Caja del texto | Anclas 0.2,0.2 → 0.8,0.8, relleno 48, `#1E1E23` |
| `Tarjeta/TituloComoJugar` | «Cómo jugar» | 56 pt negrita |
| `Tarjeta/Pasos` | Los 4 pasos | 36 pt |
| `Tarjeta/BotonCerrar` | «Entendido» | Botón principal |
| `MenuUI` | Script que conecta los botones | No lo borres |
| `EventSystem` | Necesario para los clics | Debe haber exactamente uno |

**No rompas esto** (las pruebas lo comprueban):
- El objeto `MenuUI` y sus cuatro referencias en el Inspector: `Boton Iniciar`, `Boton Como Jugar`, `Boton Cerrar Como Jugar` y `Panel Como Jugar`. Si borras un botón y haces otro, vuelve a arrastrarlo a su campo.
- Un solo `EventSystem` con «Input System UI Input Module» (el proyecto usa el Input System nuevo; el «Standalone Input Module» no funciona).
- El Canvas Scaler en 1920x1080 «Scale With Screen Size».
- `PanelComoJugar` tiene que quedar encima de todo (último hijo del Canvas), si no, tapa mal o no tapa.

Puedes cambiar libremente: colores, tamaños, posiciones, textos, añadir imágenes de fondo o decoración (propias o CC0, anotando la fuente).

## Escena Mision (por parámetros, hasta terminar la fase 7)

Todo está en `Assets/Editor/ConstructorEscenas.cs`. Busca el nombre entre comillas para encontrar la línea. Después de cambiar algo: **Herramientas > Construir escenas (sin Menu)**.

- **Paleta:** al principio del archivo (`Fondo`, `Tarjeta`, `TextoClaro`, `TextoSecundario`, `Acento`, `TextoSobreAcento`). Cambiarla ahí cambia todas las pantallas de Mision.
- **Botones:** método `Boton`: alto 88, ancho 520, texto 40 pt.
- **Briefing** (método `ConstruirMision`): columna con anclas 0.08,0.06 → 0.92,0.94; título 64 pt; etiquetas (OBJETIVO, REQUISITOS, LÍMITES, EL SEMÁFORO) 28 pt en el color de acento; objetivo 36 pt; requisitos 32 pt; límites y semáforo 28 pt en dos tarjetas lado a lado.
- **Configuración** (método `ConstruirConfiguracion`):
  - Panel izquierdo, anclas 0.015,0.03 → 0.36,0.97: título 48 pt, pestañas de 72 px de alto con texto 30 pt, lista de opciones de 420 px de alto con opciones de 72 px y texto 26 pt, ayuda 26 pt, botón «Ver briefing».
  - Panel derecho, anclas 0.655,0.03 → 0.985,0.97: textos de las barras 28 pt, barras de 24 px de alto, luz del semáforo de 56 x 56, estado 40 pt negrita, mensaje 26 pt, botón «Revisar misión».
  - El centro (0.36 a 0.655) queda vacío para que se vea la nave.
- **Revisión** (método `ConstruirRevision`): tarjeta con anclas 0.25,0.08 → 0.75,0.92, título 56 pt, resumen 30 pt, botones «Corregir» y «Lanzar».
- **Investigación** (método `ConstruirInvestigacion`): tarjeta con anclas 0.15,0.15 → 0.85,0.85, encabezado 36 pt, texto de la transmisión 40 pt, botón «Continuar investigación» (en la última dice «Ver informe»).
- **Resultado** (método `ConstruirResultado`): columna izquierda con anclas 0.05,0.06 → 0.48,0.94 (título 72 pt, conclusión 34 pt, recursos 28 pt, botón «Modificar mi misión»); tarjeta derecha con anclas 0.52,0.06 → 0.95,0.94 (evidencias y lo que faltó, 28 pt).
- **Nave** (método `ConstruirNave`): primitivas de Unity, materiales en `Assets/Materiales` (cuerpo `#B8BEC6`, paneles `#1E3A8A`, módulos del color de acento). Rotación 10, -25, 0. Cámara en 0, 0.2, -8.5.

Colores de las opciones elegidas y de las barras: en `Assets/Scripts/UI/ConfiguracionUI.cs` (`ColorElegida`, `ColorNormal`, `ColorPestanaActiva`, `ColorExcedida`, `ColorBarra`). Colores del semáforo: `Assets/Scripts/UI/PresentacionMision.cs` (`ColorSemaforo`).

## Textos

- Todos en español.
- **No escribas números del juego en la interfaz** (créditos, kg, W, espacios): salen de `Assets/Datos`. Las pruebas lo comprueban en el Briefing y en las barras.
- Los requisitos del Briefing (`Assets/Scripts/UI/TextosBriefing.cs`) son del diseño (decisión 13): no pueden nombrar instrumentos. Si los cambias, las pruebas fallan a propósito.
- Los textos de las transmisiones están en `Assets/Datos/Transmision_*.asset` y son los de `Transmisiones.md`. Si se cambian, que sea en los dos sitios.

## Antes de hacer commit

1. `git pull` antes de empezar, para trabajar sobre lo último.
2. Window > General > **Test Runner** > EditMode > **Run All**. Todo tiene que salir en verde.
3. Pulsa Play desde `Menu` y recorre: Menú, Cómo jugar, Iniciar misión, Briefing, Continuar.
4. Commit solo de lo que tocaste (por ejemplo `Assets/Escenas/Menu.unity` y las imágenes nuevas con sus `.meta`), mensaje en español, y push.
5. Avisa a Adrian si cambiaste algo de `Mision`, para no pisarnos.

## Pendiente

- **Logos del patrocinador:** falta saber qué patrocinador, tener los archivos (PNG con fondo transparente) y el permiso. Propuesta: franja grande abajo en el Menú, logo en una esquina del Briefing y franja en la pantalla de Resultado.
