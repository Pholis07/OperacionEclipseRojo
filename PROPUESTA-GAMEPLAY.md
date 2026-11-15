# Gameplay: diseño aprobado (versión 2)

Adrian aprobó la versión 1 el 2026-11-14 (escaneo, eventos, puntos y niveles; nivel 3 con 1,150 créditos) y pidió además: eventos aleatorios de la nave que se resuelven con herramientas, un límite de energía que los eventos reducen, decidir qué instrumentos encender sin poder encender todos, y que pasarse no haga perder la partida sino tiempo de navegación y, por eso, recursos.

Los números de abajo los propuso Claude para concretar el pedido. **Todos viven en los datos** (`Assets/Datos`), así que cambiarlos no toca código. Pendiente: revisión de Diego de los textos de eventos.

**Principio:** la categoría del resultado (victoria, parcial o fracaso) sigue dependiendo solo de la configuración, como en el diseño original. Los 19 casos y sus pruebas siguen valiendo. La fase nueva cambia la experiencia, el tiempo, los recursos y los puntos.

## La fase de Operación (sustituye a la Investigación)

Después de «Lanzar», la nave está en órbita sobre la región. El jugador ve:
- Un **mapa** de la región cubierto de polvo, con la anomalía escondida (cambia de sitio en cada misión).
- Sus **instrumentos**, cada uno con interruptor de encendido y su consumo.
- La **energía**: consumo actual / límite actual.
- El **tiempo de navegación**: órbitas que quedan.
- Las **herramientas** de la nave y los **eventos** activos.

### Energía

- Límite al empezar = potencia del nivel (800 / 700 / 650 W). Consumo = 100 W de la nave + el consumo total de cada instrumento encendido.
- Los instrumentos empiezan apagados. Encender y apagar no gasta tiempo.
- Si el consumo supera el límite, **la nave se apaga** (modo seguro). No se pierde la partida: se pierden **2 órbitas**, se apagan todos los instrumentos y el reinicio cuesta **50 créditos** del presupuesto sobrante.
- Como los eventos bajan el límite, muchas veces no se pueden tener todos encendidos: hay que decidir.

### Tiempo y escaneo

- Órbitas al empezar: 12 / 10 / 8 según el nivel.
- **Escanear cuesta 1 órbita.** Se elige un instrumento encendido y se pulsa en el mapa. La lectura dice **frío, tibio o caliente** según la distancia a la anomalía.
- Si el escaneo cae cerca de la anomalía, ese instrumento la encuentra y sale su transmisión. Los complementarios solo dan revelación si la órbita la desbloquea, como en el diseño.
- TX-00 sale al empezar.
- Si se acaban las órbitas, la misión termina: las transmisiones que falten llegan «con baja calidad» (el resultado no cambia, se pierden puntos).
- Cuando todos los instrumentos con transmisión encontraron la anomalía, se puede pasar al informe.

### Eventos aleatorios y herramientas

Cada nivel tiene un número de eventos (0 / 2 / 3), que aparecen al azar después de algunos escaneos. Cada herramienta se puede usar una vez por misión.

| Evento | Efecto | Herramienta que lo resuelve |
|--------|--------|-----------------------------|
| Paso por la sombra de Marte | Límite −150 W; pasa solo tras 3 órbitas | Batería de respaldo: lo compensa al momento |
| Impacto de micrometeorito | Límite −200 W hasta desviar la corriente | Reconfiguración de energía: lo resuelve |
| Sobrecalentamiento | Se apaga el instrumento encendido de mayor consumo y no se puede encender durante 2 órbitas | Refrigeración de emergencia: lo libera |
| Pérdida de orientación | El próximo escaneo no da lectura | Giroscopio de respaldo: lo evita |
| Ventana despejada (buena) | +1 órbita | — |

Cambio respecto a la primera redacción (2026-11-14): «polvo sobre los paneles» y «panel dañado con kit de reparación» no son plausibles para una nave en órbita (la tormenta no llega a la órbita y nadie puede repararla). Se cambiaron por el paso por la sombra de Marte y el impacto de un micrometeorito, con el mismo efecto en el juego.

Si al bajar el límite el consumo queda por encima, la nave se apaga (ver Energía): el jugador puede evitarlo apagando instrumentos o usando la herramienta.

## Puntos (revierte la decisión 7 y cambia PM2)

| Concepto | Puntos |
|----------|--------|
| Victoria completa / éxito parcial / fracaso | 1,000 / 500 / 100 |
| Presupuesto sobrante (después de reinicios) | +1 por cada 2 créditos |
| Cada revelación complementaria obtenida | +100 |
| Cada órbita sobrante | +25 |
| Cada transmisión de baja calidad | −100 |
| Cada apagón | −50 (además de los créditos) |

El informe muestra el desglose. Mejor puntaje por nivel guardado en el navegador.

## Niveles (se eligen en el Briefing)

| Nivel | Presupuesto | Potencia | Órbitas | Eventos |
|-------|-------------|----------|---------|---------|
| 1. Entrenamiento | 1,500 cr | 800 W | 12 | 0 |
| 2. Misión estándar (valores del diseño) | 1,250 cr | 700 W | 10 | 2 |
| 3. Tormenta severa | 1,150 cr | 650 W | 8 | 3 |

En el nivel 3 la victoria en elíptica (1,130 cr) cabe justo. Los 19 casos de prueba se verifican con el nivel 2.

## Orden de construcción

1. Lógica de la Operación (energía, órbitas, escaneo, apagón) con pruebas.
2. Eventos y herramientas.
3. Puntos.
4. Niveles.
5. Interfaz de la Operación y del informe.
6. Pruebas PlayMode del recorrido y build.

Cada paso deja el juego funcionando.

---

# Versión 3: más decisiones (aprobada)

Adrian pidió el 2026-11-14 misiones más interactivas, con más decisiones y mecánicas. Eligió las cuatro: eventos con decisiones, combustible y maniobras, memoria y envío a Tierra, y equipar herramientas con objetivos extra. Los números los propone Claude y viven en los datos. **Adrian la aprobó tal cual el 2026-11-14.** Avance: pasos 1 a 7 hechos (con la rama de Diego fusionada).

**Se mantiene el principio:** la categoría (victoria, parcial o fracaso) sigue saliendo solo de la configuración, y los 19 casos no cambian. Lo nuevo se calcula **después** de evaluar y solo usa lo que sobra (créditos y capacidad del lanzador).

**Reparto con Diego:** Claude hace la lógica, los datos, las pruebas y una interfaz básica funcional. Diego, que ahora trabaja en interfaz, animaciones, arte y una pestaña nueva, la estiliza después. Para no pisarnos, Claude no toca sus scripts de `UI/Estilo` ni las pestañas de Configuración.

## 1. Carga de la nave (pantalla nueva entre Revisión y Lanzar)

- Después de «Revisar», una pantalla de **Carga** deja elegir qué herramientas se suben. La bodega tiene **2 espacios**, así que no caben todas. Solo se paga con lo que sobra: créditos (presupuesto − costo) y masa (capacidad − masa).

| Herramienta | Créditos | Masa |
|---|---|---|
| Batería de respaldo | 40 | 20 kg |
| Reconfiguración de energía | 30 | 10 kg |
| Refrigeración de emergencia | 30 | 15 kg |
| Giroscopio de respaldo | 40 | 15 kg |

- **Combustible:** la capacidad que el lanzador sigue teniendo libre después de las herramientas se convierte en combustible, a **1 unidad por cada 10 kg** (máximo 20). Así cargar herramientas te deja con menos combustible.
- En el nivel 3, con victoria en elíptica (1,130 cr), sobran 20 créditos: no alcanza para ninguna herramienta. Es intencional, porque es el nivel difícil.

## 2. Combustible y maniobras (en la Operación)

- Hay dos **altitudes**, normal y baja. Cambiar de una a otra cuesta **3 de combustible**.
- **En baja**, el radio de detección y las distancias de frío, tibio y caliente crecen un **50 %**, así que encuentras antes. A cambio, el arrastre gasta **1 de combustible por órbita**. Si se acaba, la nave sube sola y pierdes 1 órbita.
- El combustible también paga respuestas a los eventos (ver 4).
- Al final, cada unidad que sobra da +10 puntos.

## 3. Memoria y envío a Tierra

- La memoria de la nave guarda **4 bloques** en el nivel 1, **3** en el 2 y **3** en el 3. Cada escaneo ocupa 1 bloque. Si está llena, no puedes escanear.
- Una transmisión encontrada **no cuenta como recibida hasta que se envía**. Mientras tanto queda «en memoria».
- Solo puedes **enviar** durante una **ventana de contacto con la Tierra**, que se abre 1 órbita de cada 3 y siempre en la última órbita (cambio de Adrian del 2026-11-14); la pantalla avisa cuántas faltan. Enviar vacía la memoria y no gasta órbita. Mientras tanto el transmisor consume **150 W**, así que muchas veces hay que apagar instrumentos para no provocar un apagón.
- Lo que siga en memoria al terminar llega con baja calidad (−100 por transmisión, como ahora).

## 4. Eventos con decisiones

Cuando ocurre un evento, la Operación se pausa y Control de Misión pide elegir. La opción de la herramienta solo aparece si la cargaste.

| Evento | A: herramienta | B: alternativa | C: aguantar |
|---|---|---|---|
| Sombra de Marte (−150 W) | Batería: se resuelve | Maniobrar: −4 combustible, dura 1 órbita | Dura 3 órbitas |
| Micrometeorito (−200 W) | Reconfiguración: se resuelve | Desviar la corriente a mano: −1 órbita, se resuelve | Sigue hasta el final |
| Sobrecalentamiento | Refrigeración: se libera | Girar la nave hacia la sombra: −2 combustible, bloqueo de 1 órbita | Bloqueo de 2 órbitas |
| Pérdida de orientación | Giroscopio: se evita | Corregir con propulsores: −3 combustible | Próximo escaneo sin lectura |
| Ventana despejada (buena) | — | +1 órbita | Abrir ahora una ventana de contacto con la Tierra |
| **Nuevo:** Petición de la NASA | — | Aceptar: objetivo extra (+150) de escanear un punto marcado | Rechazar: nada |
| **Nuevo:** Tormenta creciendo | — | Bajar de altitud gratis esta vez | Seguir: el próximo escaneo cuenta como tibio a menos que sea caliente o encontrado |

Los eventos por nivel siguen siendo 0, 2 y 3.

## 5. Objetivos secundarios

Al empezar la Operación salen 2 objetivos opcionales, sacados al azar de esta lista:

| Objetivo | Puntos |
|---|---|
| Encontrar todo en 5 escaneos o menos | +150 |
| Ningún apagón | +100 |
| Terminar con 5 o más de combustible | +100 |
| Enviar todo antes de la última órbita | +100 |
| No usar ninguna herramienta | +150 |

El informe los muestra cumplidos o no.

## Orden de construcción

1. Datos nuevos (herramientas con costo y masa, opciones de eventos, objetivos, memoria por nivel) y lógica de la Carga, con pruebas.
2. Combustible y altitud, con pruebas.
3. Memoria, ventanas y envío, con pruebas.
4. Eventos con decisiones y los 2 eventos nuevos, con pruebas.
5. Objetivos y puntos.
6. Interfaz básica: pantalla de Carga, controles de altitud, memoria y envío, ventana de decisión y objetivos. Diego la estiliza.
7. PlayMode del recorrido, build y verificación en el navegador.

Cada paso deja el juego funcionando y con todas las pruebas pasando.

---

# Satélites: mecánicas (aprobadas)

Adrian las aprobó tal cual el 2026-11-14. El satélite no entra en el Evaluador ni en el semáforo, así que los 19 casos no cambian. Se paga en la Carga con lo que sobra, antes que las herramientas, y sus efectos solo existen en la Operación. Sin satélite elegido se usa el MRO, que es neutro.

| Satélite | Costo | Masa | Ventaja | Desventaja |
|---|---|---|---|---|
| MRO (por defecto) | 0 cr | 0 kg | Equilibrado | — |
| Magellan | 30 cr | 10 kg | Radar +30 % de alcance; cambiar de altitud cuesta 2 | 1 bloque menos de memoria |
| Chandra | 30 cr | 40 kg | No le afecta la pérdida de orientación; térmica y alta resolución +20 % de alcance | Muy pesado (−4 de combustible) |
| Aquarius | 40 cr | 20 kg | +1 bloque de memoria; la tormenta creciendo no le da lecturas falsas | +50 W de consumo en la Operación |
| LLCD | 50 cr | 10 kg | Transmisor de 50 W; ventana de contacto cada 2 órbitas | Si sigue en una tormenta creciendo, esa órbita no puede enviar |
| AztechSat-1 (añadido el 2026-11-14) | 15 cr | 0 kg | Enlace por la constelación Globalstar: puede enviar con la ventana cerrada (fuera de la ventana cuesta 1 órbita) | −100 W de límite de energía y 1 bloque menos de memoria |

- Si lo que sobra no alcanza, la Carga bloquea «Lanzar» y ofrece «Usar el MRO».
- El AztechSat-1 es el único, además del MRO, que cabe en el nivel 3 con victoria en elíptica (sobran 20 cr).
- La Revisión sigue mostrando el consumo sin el Aquarius; la Operación lo suma.
- Diego muestra estos datos en su pestaña Satélite (campos `ventaja` y `desventaja` de `SateliteDatos`).

---

# Versión 4: Operación por meses (implementada en la rama `feat/meses`)

Viene del paquete «Operación Eclipse Rojo» v1.0 (14 de noviembre de 2026). Adrian confirmó los dos cambios de reglas: **el resultado sale de lo enviado en la Operación** y **el escaneo por clic se sustituye por un plan mensual**. Quedaba menos de un día, así que se hicieron las fases 2, 3 y 4 del paquete. Los valores son las propuestas del paquete y viven en los datos (`DatosVersion3.cs`).

## El mes

1. Preparar el plan: encender o apagar instrumentos, fijar en el mapa la región de cada uno, pedir una maniobra y pedir el envío. **Nada de esto gasta.**
2. «Resolver mes» lo calcula una sola vez, en este orden: validar → maniobrar y cobrar → días útiles → observar → almacenar → transmitir → avanzar → evento de cierre.
3. Resumen del mes (qué decidí, qué ocurrió, qué gasté, qué sigue) y, si hubo evento, su decisión. Con una decisión pendiente no se puede resolver el mes siguiente.

- Campaña de **12 meses** en los tres niveles. Eventos: 0, 3 y 5.
- Los instrumentos encendidos y sus regiones se conservan de un mes al siguiente.
- No se puede encender lo que no cabe en el límite de potencia. Un evento que deja el consumo por encima provoca modo seguro: se apaga todo, 50 créditos y 15 días menos el mes siguiente.

## Observación y evidencia

- Cada instrumento encendido con región deja un **registro** por mes: térmica 1 bloque, radar 2, alta resolución 1, atmosférico 1, espectrómetro 2. Memoria de 6 bloques. Si no cabe, no se registra y no se pisa nada.
- Calidad = 70 × días útiles / 30, × 0.8 si se siguió en una tormenta, × 1.2 en órbita baja.
- **Evidencia válida:** el instrumento observó la anomalía y la calidad es 60 o más. Si no, se puede volver a intentar otro mes.
- **Contacto con la Tierra:** meses pares y siempre el último. El envío va al final del mes y no libera memoria para ese mismo mes. Los instrumentos se apagan solos durante la ventana: solo hace falta potencia para la base y el transmisor (150 W).

## Resultado (`ResultadoCientifico`)

| Resultado | Condición |
|---|---|
| Victoria | La configuración podía ganar y llegó a la Tierra la evidencia de todos los instrumentos que exige la órbita |
| Parcial | Llegó al menos una evidencia principal (térmica o radar) |
| Fracaso | No llegó ninguna evidencia principal |

El Evaluador no cambió: sigue dando el semáforo y el máximo que permite la configuración. Lo que queda en memoria o sin observar al terminar **no cuenta** (ya no existe «baja calidad»).

## Maniobra

Cambiar de altitud (normal o baja): 40 créditos, 3 de combustible y 2 días de ese mes (28 útiles). Una por mes; se cobra al resolver. Destino igual al actual o sin recursos: no hay maniobra ni cobro. En baja, el arrastre gasta 1 de combustible por mes; sin combustible la nave sube sola y el mes siguiente pierde 10 días.

## Lo que estaba en órbitas, convertido

| Antes | Ahora |
|---|---|
| Sombra: 3 órbitas | 3 meses |
| Sobrecalentamiento: bloqueo de 2 órbitas | 2 meses |
| Desviar la corriente: −1 órbita | El mes siguiente pierde 10 días |
| Pérdida de orientación: el próximo escaneo sin lectura | El mes siguiente sin lecturas |
| Tormenta, seguir: lectura falsa | Calidad × 0.8 el mes siguiente |
| Ventana despejada: +1 órbita o abrir contacto | +1 mes de campaña o contacto el mes siguiente |
| Apagón: −2 órbitas | −15 días el mes siguiente |
| LLCD: ventana cada 2 órbitas | Contacto todos los meses |
| AztechSat-1: enviar sin ventana cuesta 1 órbita | Sin contacto solo caben 2 bloques por mes |
| Puntos por órbita sobrante | Por mes sobrante |

## Fuera de esta versión

Las 2 órbitas nuevas y la tabla de transiciones; modos de alta precisión e hiperespectral; tutorial que termina en fracaso (confirmado por Adrian: es lo primero a retomar); progresión y supervivencia; guardar y cargar; límite de enlace y descarte de registros; storyboard nuevo del lanzamiento, ducking de la voz, volúmenes separados, teclado completo e inglés.

---

# Versión 5: datos de la NASA, buffs y debuffs visibles, guía (aprobada el 2026-11-15)

Adrian pidió que todo se base en datos de la NASA (eligió el modo mixto: proporciones reales, números ajustados para el juego), que los buffs y debuffs se vean claros, más guía para el jugador (tutorial jugable), mejores texturas y skybox, y el juego en español e inglés. Aprobó la propuesta tal cual; se hace por bloques: 1 valores, 2 buffs y debuffs, 3 guía, 4 tutorial, 5 idioma, 6 texturas.

## Bloque 1: valores NASA escalados

Masa = real × 3 y potencia = real × 5 (redondeo a 5). Los créditos son moneda de juego.

| Instrumento | Real (fuente) | Masa | Potencia | Costo |
|---|---|---|---|---|
| Cámara térmica | THEMIS (Odyssey): 11.2 kg, 14 W | 35 kg | 70 W | 180 |
| Radar de subsuelo | SHARAD (MRO): 15 kg, 10 W | 45 kg | 50 W | 260 |
| Alta resolución | HiRISE (MRO): ~65 kg, 60 W | 195 kg | 300 W | 200 |
| Sensor atmosférico | Mars Climate Sounder (MRO): 9 kg, 11 W | 25 kg | 55 W | 90 |
| Espectrómetro | CRISM (MRO): 33 kg, ~45 W | 100 kg | 225 W | 220 |

- La tormenta ya no suma potencia a cada instrumento (`extraTormentaW = 0`).
- Explorador 500 kg (antes 650); Titán 750 kg. Lanzadores con valores de juego: la proporción real Soyuz/SLS (1 a más de 20) rompería el juego.
- Órbitas con su misión real: polar baja = MRO (255 × 320 km, 93°), media = TGO (400 km, 74°), elíptica = MAVEN (150 × 6,200 km, 75°).
- Cada instrumento, órbita, lanzador y satélite guarda el dato real en inglés y en español, con su fuente (`DatosNasa.cs`), y la ayuda lo muestra.
- **Los 19 casos conservan su resultado** (verde, amarillo, rojo o gris); solo cambian masa, potencia y costo. Algunos rojos lo son por otro límite: 7, B4 y B5 por costo, B4b por potencia, B2 por masa y espacios.
- Con menos masa, la nave sale con más combustible: 7 en el caso 2 (antes 3) y el tope de 20 con el Titán.

## Bloque 2: buffs y debuffs visibles

- `Modificadores.cs` genera los buffs y debuffs a partir de los números de los datos, así que lo que se ve siempre coincide con lo que hace el juego. Flecha arriba en verde (buff) o abajo en rojo (debuff), con la cifra: «Memory 6 → 5 blocks», «Power limit 700 → 600 W», «40 kg: -4 fuel».
- Se muestran en la ayuda del satélite (Configuración), en la Carga y en un panel «Active modifiers» de la Operación, que suma la altitud, los eventos activos con los meses que quedan, los bloqueos, lo que ya se sabe del mes siguiente y las herramientas a bordo.
- Las órbitas muestran su ventaja y su limitación con las mismas flechas.
- El resumen del mes desglosa la calidad: «70 base ↓ × 28/30 days ↑ × 1.2 low orbit = 78 (valid evidence needs 60)».
- El Chandra gana «-1 memory block»: el real solo guardaba 1.8 Gb (el MRO, 160 Gb).

## Bloque 3: guía en la partida

- Arriba del mapa, donde antes decía «STEP 4 OF 4», una línea con bombilla dice el siguiente paso, y un contorno que late señala lo que hay que usar (la fila del instrumento, el mapa, «Send at month end», «Resolve month» o «View report»).
- `Guia.cs` decide el paso según el estado, en este orden:
  1. Fin o toda la evidencia recibida: abrir el informe.
  2. Decisión pendiente.
  3. Contacto con evidencia en memoria (o con la memoria a punto de llenarse): activar el envío.
  4. Encender un instrumento que aún busca su evidencia.
  5. Marcar su región en el mapa.
  6. Aviso de memoria llena o de evidencia que ya no se podrá enviar (en ámbar).
  7. Pista según la última lectura: frío, tibio, caliente, sin lectura o calidad insuficiente (con la leyenda de colores del mapa).
  8. Resolver el mes.
- El tutorial jugable (Misión 0) es el bloque 4.

## Bloque 4: tutorial jugable (Misión 0)

- En el Briefing, junto a «Continue»: «New? Play Mission 0 first» (o «Replay Mission 0» si ya se completó).
- Nave fija (Explorador, órbita media, térmica, radar y MRO), sin configuración ni cinemática, sin eventos al azar y con la anomalía en un punto fijo.
- Seis pasos, cada uno con una tarjeta que explica el concepto, más la guía y el resaltado del bloque 3:
  1. Encender la térmica.
  2. Marcar el punto morado en el mapa.
  3. Resolver el mes 1.
  4. Leer el resumen.
  5. Activar «Send at month end», porque el mes 2 tiene contacto.
  6. Resolver el mes 2.
- «Resolve month» no hace nada fuera de su paso.
- **Siempre termina en fracaso:** al resolver el mes 2, una tormenta global como la de 2018 corta el contacto antes de enviar. El fracaso no depende de lo que haga el jugador. Mensaje final del paquete: «The first mission detected the signal, but could not send enough evidence. Now we know what went wrong. Design the next mission.»
- Se puede saltar en cualquier momento («Skip tutorial» o «End mission»). No toca récords ni créditos, guarda que se completó y deja la campaña limpia.

## Bloque 5: idioma (español e inglés)

- Selector «English / Español» arriba a la derecha de la pantalla de título y en Opciones. Se guarda (`PlayerPrefs` «idioma»); por defecto, inglés.
- El cambio se ve al momento, sin recargar la escena.
- Los ids, los datos guardados y las pruebas siguen en inglés; solo cambia lo que se muestra.
- Las voces de las transmisiones siguen en inglés (no se generan audios nuevos).

## Bloque 6: texturas y cielo

- Cielo de las cinemáticas: Deep Star Maps 2020 de la NASA (Goddard SVS), en coordenadas galácticas, a 8k. Es de dominio público y queda en Créditos con su crédito («NASA/Goddard Space Flight Center Scientific Visualization Studio. Gaia DR2: ESA/Gaia/DPAC»).
- Fondos planos (Menu y Operación): un recorte 1:1 del mismo mapa, en la zona donde la Vía Láctea es tenue para no competir con la interfaz.
- Materiales propios generados por código:
  - lámina dorada arrugada con relieve en los instrumentos;
  - celdas solares con rejilla en el AztechSat-1 y en los paneles de los instrumentos;
  - espuma naranja con relieve en el tanque del Titán (SLS);
  - metal cepillado en el Explorador, el AztechSat-1 y los instrumentos.
- Los modelos de NASA 3D conservan sus texturas originales. Se revisaron y no llevan el logo de la NASA.
- El build crece unos 5 MB (de 48 a 53 MB).

---

# Versión 6: alcance real del lanzador, ayuda en el modo normal, informe en dos pantallas, voces por idioma y Marte unificado (2026-11-15)

Diego probó la versión 5 y pidió: entender el juego sin pasar por la Misión 0, un informe menos saturado, elegir primero el lanzador y que su alcance (con física real y datos de la NASA, a la vista) decida las órbitas, el Marte del menú en todas partes con más detalle, que los diálogos no se encimen y que las voces sigan al idioma. Adrian estuvo de acuerdo con cambiar la lógica.

## Alcance del lanzador (física real, sin aerofrenado)

**Revierte la regla de la sección «Satélites»: el satélite ahora cuenta en la masa.**

- Orden de la configuración: **Lanzador → Satélite → Instrumentos → Órbita**.
- Frenado que pide cada órbita, en el punto más bajo (vis-viva): `√(v∞² + 2μ/rp) − √(μ(2/rp − 1/a))`, con GM 42,828 km³/s² y radio 3,389.5 km (NASA NSSDC) y llegada a 2.7 km/s.

  | Órbita | Altitudes reales | Frenado |
  |---|---|---|
  | Polar baja (MRO) | 255 × 320 km | 2,106 m/s |
  | Media (TGO) | 400 × 400 km | 2,106 m/s |
  | Elíptica (MAVEN) | 150 × 6,200 km | 1,407 m/s |

  En la realidad polar y media cuestan lo mismo; no se inventa diferencia.
- Frenado que tiene la nave (ecuación del cohete): `Isp × g0 × ln(masa inyectada ÷ masa seca)`, Isp 230 s (hidracina, familia MR-107 del MRO: 222 a 236 s).
- Masa seca = base 350 kg + satélite + instrumentos.
- Masa que cada lanzador manda hacia Marte:
  - Explorador: 1,223 kg (dato real: Mars Express en Soyuz-Fregat, ESA).
  - Titán: 1,850 kg (valor de juego, 1.5 veces el Explorador).
- Masa seca máxima con la que se llega:

  | Lanzador | Órbitas bajas | Elíptica |
  |---|---|---|
  | Explorador | 480 kg | 655 kg (manda su tope de 500 kg) |
  | Titán | 727 kg | 991 kg (manda su tope de 750 kg) |

- Validación: con 2,180 kg al salir y 1,031 kg en seco, el MRO real tiene 1,688 m/s y no llega a 255 × 320 km. Coincide con la historia: frenó ~1 km/s hasta 400 × 35,000 km y luego hizo seis meses de aerofrenado.
- Límite nuevo `Limite.Alcance` y quinta barra «Frenado» (lo que pide la órbita sobre lo que tiene la nave).
  - Las órbitas fuera de alcance se ven bloqueadas y no se eligen.
  - Una órbita ya elegida que deja de alcanzarse se conserva en rojo.
  - La nota «¿Hasta dónde llegas?» muestra la fórmula con los números de la nave y la fuente.
- Las barras muestran los parciales desde el primer paso, aunque falte algo.
- Los 19 casos conservan su color; B2 suma el límite de alcance. Cada nivel sigue siendo ganable.
- La Carga no cuenta dos veces el satélite. Sus números (combustible, masa libre) no cambian.

## Ayuda en el modo normal

- Tarjeta con el siguiente paso en Briefing, Configuración, Revisión y Carga, y contorno que late en el control que toca (`GuiaPantallas`).
- Botón «?» en todas las pantallas de la misión: explicación completa de esa pantalla. Desde ahí, o con «×», se ocultan los consejos.
- «Cómo jugar» con los pasos reales del juego.

## Informe en dos pantallas

1. Veredicto: título, conclusión, puntos y sello, con el botón «Ver detalles».
2. Detalle: evidencias, lo que faltó, desglose de puntos y recursos usados, con «Modificar mi misión» (vuelve a la configuración) y «Volver al veredicto».

## Voces

- Las 10 voces (4 de control y 6 transmisiones) suenan en el idioma elegido. Las grabaciones en español ya existían y coinciden con el texto en pantalla, así que no hubo que generar audio nuevo.
- En la cinemática, cada toma dura lo que su frase (con tope).
- Las transmisiones van en fila: no se cortan y se oyen todas.
- La música, el ambiente y el motor bajan mientras habla una voz.

## Marte

- El shader del planeta (compartido por el menú, la configuración, la cinemática y el informe) suma:
  - nubes de hielo de agua (textura propia generada por código);
  - escarcha con brillo en los casquetes;
  - crepúsculo azulado.
- El Briefing muestra el mismo Marte 3D en vez de una foto.
- El mapa de la Operación sigue con el relieve de Gale de la NASA: el mapa global de 8K se vería peor a esa escala.
