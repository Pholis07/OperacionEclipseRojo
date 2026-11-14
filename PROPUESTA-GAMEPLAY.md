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

Adrian pidió el 2026-11-14 misiones más interactivas, con más decisiones y mecánicas. Eligió las cuatro: eventos con decisiones, combustible y maniobras, memoria y envío a Tierra, y equipar herramientas con objetivos extra. Los números los propone Claude y viven en los datos. **Adrian la aprobó tal cual el 2026-11-14.** Avance: paso 1 hecho.

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
- Solo puedes **enviar** durante una **ventana de contacto con la Tierra**, que se abre 1 órbita de cada 3; la pantalla avisa cuántas faltan. Enviar vacía la memoria y no gasta órbita. Mientras tanto el transmisor consume **150 W**, así que muchas veces hay que apagar instrumentos para no provocar un apagón.
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
