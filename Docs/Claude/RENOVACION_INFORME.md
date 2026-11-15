# Renovación visual: cohetes, constructor, lanzamiento y lecturas

Encargo 3.1 de Diego (15 de noviembre de 2026). Rama `feat/renovacion-visual`, sobre `main` en `8ac11e9`.
No cambia reglas, recursos, probabilidades ni finales: todas las suites de reglas pasan sin tocarse.

## Identificación

| | |
|---|---|
| Unity | 6000.6.9f1 |
| Pipeline | URP 17.6. WebGL usa la calidad «Mobile» (Forward, sin textura de profundidad) |
| Plataforma | WebGL, build de 58.8 MB |
| Navegador de prueba | Chromium 151 sin cabeza, WebGL por software (SwiftShader): 1920×1080 en español y 1280×720 en inglés |
| Pruebas | 447 de 447 EditMode y 36 de 36 PlayMode |

## Qué cambió

### Cohetes
- **Pionero (Jupiter-C):** 7 mallas del modelo traían triángulos al revés y caras dobles; el cuerpo se veía a trozos negros. Ahora se orientan hacia fuera al construir.
- **Horizonte (Ares 1 A):** traía 5 nodos repetidos en el mismo sitio y ningún color (todos los materiales eran el mismo gris, que un tinte dejaba en blanco). Se descartan los repetidos y se pinta por zonas.
- **Corte entre etapas:** parte los triángulos en el plano (antes borde en sierra) y cada boca lleva tapa; la etapa superior tiene tobera. El Titán también lleva sus dos tapas.
- **Brazos de la torre:** telescópicos y a la medida del cohete elegido. Antes atravesaban a Coloso, Goliat y Titán.
- **Carga y llamas:** el satélite se escala al radio libre medido de cada cofia; las llamas salen de las toberas medidas (en el Saturn V estaban a 0.30 del eje y las campanas están a 0.54).
- 122 mallas viejas sin uso eliminadas (una malla por parte y cohete).

### Constructor
- **Montaje en seis tiempos** al instalar: llega de lado, se orienta, se aproxima frenando, asienta, cierran dos fijaciones y se enciende el indicador. La cámara se acerca a la pieza.
- **Retirada visible:** una copia hace el recorrido inverso; el soporte queda con tapa ciega.
- **Mecanismo propio por instrumento**, con pivotes: botalones del radar, parasol y tapa de la cámara, visera y tapa de la térmica, mástil del sensor, radiador y obturador del espectrómetro.
- **Inspección:** arrastrar gira, la rueda acerca, foco por instrumento instalado y «Restablecer vista».
- **Hangar de integración** como escenario de la nave.
- **Revisión:** el resumen va a la izquierda y la nave se ve en el centro.

### Lanzamiento
- **Plataforma:** foso de gases real, pedestal con cuatro sujeciones, barreras y carteles; la vegetación queda lejos.
- **Despliegue:** tras liberarse, la nave abre sus mecanismos uno tras otro. Ver la cinemática u omitirla deja la nave igual.

### Lecturas
- Los cinco instrumentos tienen lectura durante el escaneo (antes dos). Se añadieron la térmica (falso color, escala relativa), el radar (perfil de ecos) y el sensor atmosférico (opacidad del polvo).
- El rasgo solo aparece si la lectura real dio con la anomalía; si no aporta evidencia, la nota lo dice.

## Segunda ronda (pendientes de la primera)

- **«Tu nave» en la Operación:** vista fija de la nave en órbita sobre una esquina del mapa. No tapa los clics, se puede plegar y enfoca el instrumento que se enciende o se apaga.
- **Paneles con bisagra:** las alas de panel del MRO, el Magellan, el Chandra y el LLCD se separaron de la malla y se pliegan y despliegan; el CubeSat despliega sus cuatro antenas. Van plegados en el hangar y se abren tras la liberación.
- **El instrumento acciona su mecanismo al escanear** y la secuencia nombra las otras lecturas del mes y cuántas aportaron evidencia.
- **Pionero:** cuerpo a cuadros blancos y negros, como el Jupiter-C.
- **Ruta orbital más fina** y rótulo del Explorador corregido (aspecto del Ares I, carga de un Soyuz-Fregat).
- **Pruebas nuevas:** omitir en cada una de las 10 tomas, diez escaneos seguidos sin deriva, paneles y «Tu nave».

## Estado de los hitos

| Hito | Implementado | Editor (pruebas) | Navegador | Pendiente |
|---|---|---|---|---|
| H0 auditoría | Sí | — | — | — |
| H1 recorrido conectado | Sí | Sí | Completo, hasta «Modificar mi misión» | — |
| H2 constructor | Sí | Sí | Tres instrumentos | Alta resolución y espectrómetro en navegador |
| H3 escenario y lanzamiento | Sí | Los 6 lanzadores | Explorador y Coloso | Los otros cuatro en navegador |
| H4 investigación | Sí | Sí | Lectura del radar | Las otras cuatro lecturas y un hallazgo en navegador |
| H5 integración | Sí | Sí | es 1080p, en 720p | Rendimiento, teclado |

## Lo que no se hizo o no se comprobó

- **Aquarius:** su modelo no da dos alas separables; queda rígido (solo se despliegan sus instrumentos).
- **«Tu nave» tapa una esquina del mapa.** Se pliega con un clic en su cabecera, pero plegada no es el estado inicial.
- **No se vio en navegador:** un escaneo que encuentre la anomalía, las lecturas de la térmica, la cámara, el sensor atmosférico y el espectrómetro, ni los lanzadores Pionero, Horizonte, Titán y Goliat. Están cubiertos por las pruebas automáticas.
- **Intersecciones al desplegar:** revisadas en capturas por satélite, no con cada combinación de instrumentos.
- **Teclado en la inspección y zoom con la rueda:** probados solo por código.
- **Rendimiento:** sin medir. El navegador de prueba dibuja por software. «Tu nave» añade una cámara que dibuja 4 veces por segundo.
- **Prueba con tres personas (WOW01):** sin hacer; necesita personas.
- **Sonido:** se reutilizan los efectos existentes; la mezcla no se revisó de oído.

La matriz completa está en `RENOVACION_MATRIZ.csv`.

## Recursos

No entra ningún recurso externo nuevo. Toda la geometría añadida es original, hecha por código con primitivas. No se gastó nada en ElevenLabs.

## Cómo revisar y revertir

- Regenerar escenas: «Herramientas > Construir escenas (sin Menu)». `Menu.unity` no se tocó.
- Capturas antes y después: `Docs/Claude/capturas/renovacion/`.
- Cada bloque es un commit: cohetes (`277e1c2`), constructor (`b4e876a`), plataforma y lecturas (`1524d98`), ajustes de cámara (`46e2d19`) y la segunda ronda. Se revierten por separado con `git revert`.
