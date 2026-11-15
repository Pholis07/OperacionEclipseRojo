# Renovación visual: cohetes, constructor, lanzamiento y lecturas

Encargo 3.1 de Diego (15 de noviembre de 2026). Rama `feat/renovacion-visual`, sobre `main` en `8ac11e9`.
No cambia reglas, recursos, probabilidades ni finales: todas las suites de reglas pasan sin tocarse.

## Identificación

| | |
|---|---|
| Unity | 6000.6.9f1 |
| Pipeline | URP 17.6. WebGL usa la calidad «Mobile» (Forward, sin textura de profundidad) |
| Plataforma | WebGL, build de 58 MB |
| Navegador de prueba | Chromium 151 sin cabeza, WebGL por software (SwiftShader), 1920×1080, español |
| Pruebas | 440 de 440 EditMode y 34 de 34 PlayMode |

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

## Estado de los hitos

| Hito | Implementado | Editor (pruebas) | Navegador | Pendiente |
|---|---|---|---|---|
| H0 auditoría | Sí | — | — | — |
| H1 recorrido conectado | Sí | Sí | Hasta la Operación | Ver un escaneo y el informe final |
| H2 constructor | Sí | Sí | Radar y térmica | Los otros tres instrumentos (nivel 1 no los abre) |
| H3 escenario y lanzamiento | Sí | Los 6 lanzadores | Solo Explorador | Intersecciones del despliegue, omitir en cada toma |
| H4 investigación | Parcial | Sí | No | Nave visible en la Operación; lecturas agrupadas |
| H5 integración | Parcial | Sí | Parcial | 720p, inglés, finales, rendimiento |

## Lo que no se hizo o no se comprobó

- **Nave visible durante la Operación:** no se hizo. La nave se sigue viendo solo durante el escaneo.
- **Varias lecturas en un mismo mes:** se sigue destacando una; no se añadieron fichas con las demás.
- **El instrumento no acciona su mecanismo durante el escaneo** (ya va desplegado en órbita).
- **Paneles solares de los satélites de la NASA:** son mallas de una pieza y no se separaron; se despliegan los mecanismos de los instrumentos, no los paneles.
- **Franjas del Jupiter-C:** el modelo llega sin su patrón; el Pionero queda blanco con anillos.
- **No se vio en navegador:** ningún escaneo con las lecturas nuevas, el informe final, «Modificar mi misión», inglés, 1280×720, ni los cinco lanzadores restantes.
- **Rendimiento:** sin medir. El navegador de prueba dibuja por software.
- **Prueba con tres personas (WOW01):** sin hacer.
- **Sonido:** se reutilizan los efectos existentes (servo); no se revisó la mezcla de oído.

La matriz completa está en `RENOVACION_MATRIZ.csv`.

## Recursos

No entra ningún recurso externo nuevo. Toda la geometría añadida es original, hecha por código con primitivas. No se gastó nada en ElevenLabs.

## Cómo revisar y revertir

- Regenerar escenas: «Herramientas > Construir escenas (sin Menu)». `Menu.unity` no se tocó.
- Capturas antes y después: `Docs/Claude/capturas/renovacion/`.
- Cada bloque es un commit: cohetes (`277e1c2`), constructor (`b4e876a`), plataforma y lecturas (`1524d98`) y este último con los ajustes de cámara. Se revierten por separado con `git revert`.
