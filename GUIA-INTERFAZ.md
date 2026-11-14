# Guía para modificar la interfaz

Para Diego. Explica qué se puede cambiar en las pantallas, dónde está cada parámetro y qué no hay que romper. Valores vigentes al 2026-11-14 (fase 7 y estilo HUD).

## Quién toca qué

| Escena | Cómo se cambia | Quién |
|--------|----------------|-------|
| `Assets/Escenas/Menu.unity` | A mano en el editor de Unity (Hierarchy e Inspector) | Diego |
| `Assets/Escenas/Mision.unity` (Briefing, Configuración, Revisión, Investigación) y `Assets/Escenas/Resultado.unity` | Cambiando los parámetros de `Assets/Editor/ConstructorEscenas.cs` y ejecutando **Herramientas > Construir escenas (sin Menu)** | Por ahora Adrian/Claude. No las edites a mano hasta acordar el cambio: el constructor las rehace desde cero y se perderían tus cambios. Cuando el juego esté completo se puede pasar también a edición manual, como el Menú |

Dos opciones rehacen el Menú desde cero y **borran tus cambios**; úsalas solo si el Menú queda inservible:
- **Herramientas > Diseñar Menu estilo HUD**: el Menú actual (lista a la izquierda, nave girando sobre Marte).
- **Herramientas > Recuperar Menu original**: el Menú de la fase 5 (columna centrada).

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
| Horizonte de Marte del Menú original | RGBA 0.55, 0.18, 0.08, 0.55 |
| Semáforo | Gris `#9CA3AF`, Rojo `#EF4444`, Amarillo `#EAB308`, Verde `#22C55E` |
| Líneas HUD (marcos, subrayados) | `#3B5568`, 2 px |
| Escuadra de las esquinas | Acento, 48 x 4 px |
| Paneles sobre la escena 3D | `#1E1E23` al 92 % |
| Rejilla del suelo | Fondo `#0B0B10`, líneas `#1C2A38`, líneas mayores `#2C4256` |
| Planeta Marte del Menú | `#A8472A` |

Estilo HUD (referencias: Starfield y Vehicle Designer): paneles con marco fino y escuadra de acento arriba a la izquierda (`MarcoHud`), etiquetas en mayúsculas subrayadas (`Encabezado`), botones secundarios con marco fino y el paso actual a la vista en cada pantalla («PASO 2 DE 4 · CONFIGURACIÓN»).

Fuente: Liberation Sans (TextMeshPro). Si cambias de fuente, que sea libre (OFL o similar) y avisa para anotarla en los créditos.

## Escena Menu (la editas tú)

Resolución de referencia 1920x1080 (Canvas > Canvas Scaler, «Scale With Screen Size», match 0.5). Los porcentajes son anclas del RectTransform (mín x, mín y → máx x, máx y).

| Objeto (Hierarchy) | Qué es | Valores actuales |
|--------------------|--------|------------------|
| `Main Camera` | Cámara de la escena 3D | Posición 0, 0.2, -8.5; fondo `#0B0B10` |
| `Marte` | Esfera del planeta, gira despacio (`GiroLento`) | Posición 4.5, -13.5, 8; escala 22 |
| `Nave` | La nave con todos los módulos, gira despacio (`GiroLento`, 6 grados por segundo) | Posición 3.2, 0.6, 0; escala 0.9 |
| `Canvas/Velo` | Franja oscura detrás del texto, con marco fino | `#0B0B10` al 75 %, anclas 0,0 → 0.46,1 |
| `Canvas/Contenido` | Columna alineada a la izquierda (Vertical Layout Group, espacio 28) | Anclas 0.05,0.12 → 0.42,0.9 |
| `Contenido/Etiqueta` | «SIMULACIÓN DE DISEÑO DE MISIONES», subrayada | 28 pt negrita, acento |
| `Contenido/Titulo` | «Operación / Eclipse Rojo» en dos líneas | 96 pt, negrita, `#F8FAFC`, a la izquierda |
| `Contenido/Premisa` | «Una señal rara aparece en Marte y una tormenta de polvo dificulta la investigación.» | 36 pt, `#94A3B8` |
| `Contenido/BotonIniciar` | «Iniciar misión» (principal). Tiene el foco del teclado al empezar (`EventSystem > First Selected`) | 520 x 88, fondo `#E2643A`, texto 40 pt negrita `#0B0B10` |
| `Contenido/BotonComoJugar` | «Cómo jugar» (secundario, con marco fino) | 520 x 88, fondo `#1E1E23`, texto 40 pt `#F8FAFC` |
| `Canvas/Pie` | Aviso de valores ficticios | 24 pt, `#94A3B8`, abajo a la izquierda |
| `Canvas/Teclas` | «ENTER Seleccionar · FLECHAS Moverse» | 24 pt, `#94A3B8`, abajo a la derecha |
| `Canvas/PanelComoJugar` | Ventana de «Cómo jugar», oculta al empezar | Negro al 85 %, toda la pantalla |
| `PanelComoJugar/Tarjeta` | Caja del texto, con marco HUD | Anclas 0.2,0.2 → 0.8,0.8, relleno 48, `#1E1E23` |
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

- **Paleta:** al principio del archivo (`Fondo`, `Tarjeta`, `TextoClaro`, `TextoSecundario`, `Acento`, `TextoSobreAcento`, y las del estilo HUD: `LineaHud`, `TarjetaTranslucida`, `CajaAyuda`, `Rejilla*`, `Marte`). Cambiarla ahí cambia todas las pantallas de Mision.
- **Estilo HUD:** `GrosorLinea` (2), `LargoEsquina` (48) y `GrosorEsquina` (4). Piezas: `MarcoHud`, `Bordes`, `Esquina`, `Encabezado` (28 pt, espaciado de letras 2, subrayado), `Linea`.
- **Pasos:** `NombresPasos` (Briefing, Configuración, Revisión, Investigación). `Ruta` arma la barra de pasos del Briefing y `Paso` el «PASO n DE 4» de las demás vistas.
- **Botones:** método `Boton`: alto 88, ancho 520, texto 40 pt. Los secundarios llevan marco fino; el foco del teclado se ve igual que el ratón encima.
- **Briefing** (método `ConstruirMision`): barra de pasos arriba (24 pt, anclas 0.05,0.92 → 0.95,0.97, con línea debajo). Columna izquierda con anclas 0.05,0.17 → 0.51,0.88: título 64 pt, objetivo 36 pt, requisitos 32 pt (interlineado 20). Columna derecha con anclas 0.55,0.17 → 0.95,0.88: tarjetas LÍMITES y EL SEMÁFORO (28 pt, con marco HUD). Abajo, la pista (26 pt) a la izquierda y «Continuar» (520 x 88) a la derecha.
- **Configuración** (método `ConstruirConfiguracion`):
  - Panel izquierdo, anclas 0.015,0.03 → 0.36,0.97, translúcido con marco HUD: título 48 pt, pestañas de 72 px de alto con texto de 30 pt que baja solo hasta 24 pt si no cabe, lista de opciones de 420 px de alto con opciones de 72 px y texto 26 pt, ayuda 26 pt, botón «Ver briefing».
  - Panel derecho, anclas 0.655,0.03 → 0.985,0.97, translúcido con marco HUD: textos de las barras 28 pt, barras de 24 px de alto, luz del semáforo de 56 x 56, estado 40 pt negrita, mensaje 26 pt, botón «Revisar misión».
  - El centro (0.36 a 0.655) queda libre para la nave: arriba «PASO 2 DE 4» (24 pt) y abajo la leyenda «VISTA DE LA NAVE» (26 y 24 pt).
- **Revisión** (método `ConstruirRevision`): tarjeta con marco HUD y anclas 0.25,0.08 → 0.75,0.92, «PASO 3 DE 4» 24 pt, título 56 pt, resumen 30 pt, botones «Corregir» y «Lanzar».
- **Investigación** (método `ConstruirInvestigacion`): tarjeta con marco HUD y anclas 0.15,0.15 → 0.85,0.85, «PASO 4 DE 4» 24 pt, encabezado 36 pt, texto de la transmisión 40 pt, botón «Continuar investigación» (en la última dice «Ver informe»).
- **Resultado** (método `ConstruirResultado`): columna izquierda con anclas 0.05,0.06 → 0.48,0.94 (título 72 pt, conclusión 34 pt, recursos 28 pt con marco fino, botón «Modificar mi misión»); tarjeta derecha con marco HUD y anclas 0.52,0.06 → 0.95,0.94 (evidencias y lo que faltó, 28 pt).
- **Nave** (método `ConstruirNave`): primitivas de Unity, materiales en `Assets/Materiales` (cuerpo `#B8BEC6`, paneles `#1E3A8A`, módulos del color de acento). Rotación 10, -25, 0. En Mision está en 0, 0.4, 0 con escala 0.55 para que quepa entre los paneles. Cámara en 0, 0.2, -8.5.
- **Suelo** (método `ConstruirSuelo`): plano con rejilla en 0, -2.6, 6, escala 8. La textura `Assets/Materiales/Rejilla.png` se genera en `CrearMaterialRejilla` (celdas de 4 unidades).

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
