# Informe de pruebas: Operación Eclipse Rojo

## Versión 4 (Operación por meses), 2026-11-14

Ejecutado en esta sesión sobre la rama `feat/meses`, commit `e9430e8` más el ajuste de marcas del mapa (`87fb996`). Unity 6000.6.9f1 en modo batch, Linux. Sustituye a los resultados de abajo para todo lo que toca la Operación: las reglas nuevas invalidan las pruebas de la v3.

| Conjunto | Total | Pasan | Fallan |
|---|---|---|---|
| EditMode | 208 | 208 | 0 |
| PlayMode (`RecorridoTests`, los 19 casos jugando meses con los botones reales) | 19 | 19 | 0 |
| Build WebGL | 36.7 MB | 0 errores | |

Matriz de aceptación del paquete v1.0:

| Id | Estado | Dónde |
|---|---|---|
| MES-01, 02, 03, 06 | pasa | `MesesTests` |
| MES-04 (omitir animación = mismo estado) | pasa por diseño | La animación solo lee `ResultadoMes`; el recorrido PlayMode cierra el resumen sin esperarla |
| MES-05, SAV-01, SAV-02 (guardado) | no ejecutada | Fuera de esta versión |
| MEM-01, 02, 03, 04 | pasa | `MesesTests` |
| ORB-01, 02, 03 | pasa | `MesesTests` (ORB-01 con el contrato de `escenario_mes_01.json`) |
| CIE-01, 02, 03, 04 | pasa | `MesesTests` |
| EVT-01, 02 | pasa | `MesesTests` |
| TUT-01, PRO-01, PRO-02 | no ejecutada | Fuera de esta versión |
| UI-01 (una activación por intención) | pasa | `InvestigacionTests` (con el resumen abierto no se resuelve otra vez) |
| AUD-01 (reconstruir no suena) | pasa | `ConfiguracionTests` |

Prueba manual en el navegador (Chromium con SwiftShader, 1366×768, build servido en local): una campaña ganada en el nivel 2, caso 2. Plan con dos instrumentos, tormenta (bajar gratis), pérdida de orientación, memoria llena («sin memoria»), envío en el mes 4, subida forzada por falta de combustible, envío en el mes 6 e informe «Misión cumplida» con 1,465 puntos. Consola sin errores.

No ejecutado: campañas parcial y fracasada en el navegador (sí en pruebas automáticas), 1920×1080 y 1280×720, escucha real del audio, FPS y memoria en un equipo con GPU.

Fase 8 (verificación completa), 2026-11-14. Commit probado: `7bff139` más las pruebas PlayMode nuevas. Unity 6000.6.9f1, modo batch.

## Resumen

| Conjunto | Total | Pasan | Fallan |
|----------|-------|-------|--------|
| Pruebas EditMode (datos, cálculo, evaluación, interfaz, escenas) | 112 | 112 | 0 |
| Pruebas PlayMode (recorrido del juego pulsando los botones) | 19 | 19 | 0 |

**Fallos: 0.**

## Casos en el juego (línea [Misión] de la consola)

Cada caso se recorrió en modo Play con los botones reales: Menú → Iniciar misión → Continuar → pestañas y opciones → Revisar misión → Lanzar → transmisiones → informe → Modificar mi misión. La tabla esperada es la del prompt 08 (generada con un script, sin copiarla a mano); «obtenido» es la última línea `[Misión]` que escribió el juego.

| Caso | Esperado | Obtenido | Estado |
|------|----------|----------|--------|
| 1 | `masa=620 potencia=430 costo=850 semáforo=VERDE resultado=VICTORIA` | `masa=620 potencia=430 costo=850 semáforo=VERDE resultado=VICTORIA` | Pasa |
| 2 | `masa=620 potencia=430 costo=820 semáforo=VERDE resultado=VICTORIA` | `masa=620 potencia=430 costo=820 semáforo=VERDE resultado=VICTORIA` | Pasa |
| 3 | `masa=620 potencia=430 costo=870 semáforo=AMARILLO resultado=PARCIAL` | `masa=620 potencia=430 costo=870 semáforo=AMARILLO resultado=PARCIAL` | Pasa |
| 4 | `masa=670 potencia=500 costo=1130 semáforo=VERDE resultado=VICTORIA` | `masa=670 potencia=500 costo=1130 semáforo=VERDE resultado=VICTORIA` | Pasa |
| 5 | `masa=500 potencia=300 costo=650 semáforo=AMARILLO resultado=PARCIAL` | `masa=500 potencia=300 costo=650 semáforo=AMARILLO resultado=PARCIAL` | Pasa |
| 6 | `masa=485 potencia=320 costo=800 semáforo=AMARILLO resultado=FRACASO` | `masa=485 potencia=320 costo=800 semáforo=AMARILLO resultado=FRACASO` | Pasa |
| 7 | `masa=780 potencia=760 costo=1300 semáforo=ROJO resultado=-` | `masa=780 potencia=760 costo=1300 semáforo=ROJO resultado=-` | Pasa |
| B1 | `masa=- potencia=- costo=- semáforo=GRIS resultado=-` | `masa=- potencia=- costo=- semáforo=GRIS resultado=-` | Pasa |
| B2 | `masa=585 potencia=450 costo=780 semáforo=ROJO resultado=-` | `masa=585 potencia=450 costo=780 semáforo=ROJO resultado=-` | Pasa |
| B4 | `masa=755 potencia=650 costo=1210 semáforo=ROJO resultado=-` | `masa=755 potencia=650 costo=1210 semáforo=ROJO resultado=-` | Pasa |
| B4b | `masa=695 potencia=710 costo=1170 semáforo=ROJO resultado=-` | `masa=695 potencia=710 costo=1170 semáforo=ROJO resultado=-` | Pasa |
| B5 | `masa=730 potencia=690 costo=1260 semáforo=ROJO resultado=-` | `masa=730 potencia=690 costo=1260 semáforo=ROJO resultado=-` | Pasa |
| B6 | `masa=520 potencia=300 costo=640 semáforo=AMARILLO resultado=PARCIAL` | `masa=520 potencia=300 costo=640 semáforo=AMARILLO resultado=PARCIAL` | Pasa |
| B7 | `masa=- potencia=- costo=- semáforo=GRIS resultado=-` | `masa=- potencia=- costo=- semáforo=GRIS resultado=-` | Pasa |
| B8 | `masa=- potencia=- costo=- semáforo=GRIS resultado=-` | `masa=- potencia=- costo=- semáforo=GRIS resultado=-` | Pasa |
| RV1 | `masa=645 potencia=640 costo=1110 semáforo=AMARILLO resultado=PARCIAL` | `masa=645 potencia=640 costo=1110 semáforo=AMARILLO resultado=PARCIAL` | Pasa |
| RV2 | `masa=610 potencia=560 costo=1040 semáforo=AMARILLO resultado=PARCIAL` | `masa=610 potencia=560 costo=1040 semáforo=AMARILLO resultado=PARCIAL` | Pasa |
| RV3 | `masa=585 potencia=450 costo=1000 semáforo=AMARILLO resultado=PARCIAL` | `masa=585 potencia=450 costo=1000 semáforo=AMARILLO resultado=PARCIAL` | Pasa |
| RV4 | `masa=500 potencia=300 costo=680 semáforo=AMARILLO resultado=PARCIAL` | `masa=500 potencia=300 costo=680 semáforo=AMARILLO resultado=PARCIAL` | Pasa |

En el mismo recorrido se comprobó en cada caso:
- «Lanzar» habilitado solo en Amarillo y Verde; en Gris y Rojo, bloqueado y «Corregir» vuelve a Configuración.
- Número de transmisiones mostradas igual a la tabla, con «Continuar investigación» entre cada una.
- El informe muestra el título de su resultado.
- «Modificar mi misión» abre Configuración y conserva órbita, lanzador e instrumentos.

## Comprobaciones de datos e interfaz

| Prueba | Qué comprueba | Cómo | Estado |
|--------|---------------|------|--------|
| DT1 | Ids únicos | EditMode `DatosTests` | Pasa |
| DT2 | Las listas de las órbitas apuntan a instrumentos que existen | EditMode `DatosTests` | Pasa |
| DT3 | Cada instrumento tiene transmisión de presente y de ausente | EditMode `DatosTests` | Pasa |
| DT4 | Categoría solo victoria, parcial o fracaso; no hay semáforo guardado | EditMode `DatosTests` | Pasa |
| PM1 | Briefing: objetivo, tres requisitos sin nombres de instrumentos, límites (Titán 750 kg) | EditMode `InterfazTests` | Pasa la parte automática. **Pendiente la revisión visual en pantalla** |
| PT1 | La ayuda de cada instrumento muestra el desglose y las barras usan el consumo total | EditMode `DatosTests` y `ConfiguracionTests` | Pasa |
| PM2 | El caso 2 (820 cr) da victoria, muestra los recursos usados y no da puntos | EditMode `InvestigacionTests` | Pasa |
| F1 | Texto de fracaso exacto (decisión 6) e informe del caso 6 con la revelación de alta resolución | EditMode `InvestigacionTests` | Pasa |

## Lo que no cubren estas pruebas

- **Lo visual:** que los textos se lean bien, que nada se corte y que la nave se vea en el centro. Las pruebas corren sin pantalla (`-nographics`). Hay que mirarlo con Play.
- **El build WebGL en el navegador:** se prueba en la fase 9.
- **Los cambios de Diego al Menú** que todavía no están comiteados: este informe es del commit `7bff139`.

## Cómo repetirlo

- En el editor: Window > General > Test Runner, pestañas EditMode y PlayMode, «Run All».
- En terminal, con el editor cerrado: `Unity -batchmode -nographics -projectPath <proyecto> -runTests -testPlatform EditMode -testResults resultados.xml` (y lo mismo con `PlayMode`).
