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
| Polvo sobre los paneles | Límite −150 W; se limpia solo tras 3 órbitas | Limpieza de paneles: lo quita al momento |
| Panel solar dañado | Límite −200 W hasta repararlo | Kit de reparación: lo repara |
| Sobrecalentamiento | Se apaga el instrumento encendido de mayor consumo y no se puede encender durante 2 órbitas | Refrigeración de emergencia: lo libera |
| Pérdida de orientación | El próximo escaneo no da lectura | Giroscopio de respaldo: lo evita |
| Ventana despejada (buena) | +1 órbita | — |

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
