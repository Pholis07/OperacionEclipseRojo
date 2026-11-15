# Estado de Eclipse Ink

Última actualización: 2026-11-15. Rama `feat/eclipse-ink` (sin push).

| Fase | Estado |
|---|---|
| F0 Auditoría | Hecha: `AUDITORIA_INICIAL.md` |
| F1 Piloto (satélite e instrumentos) | Hecho: `REPORTE_FASE_1.md`. **Esperando la revisión de Diego** |
| Informe final rediseñado | Hecho |
| Cinemática a 44 s | Hecho |
| F2 Cohetes, plataforma, torre, árboles y luz del lanzamiento | Pendiente del visto bueno del piloto |
| F5 Menú coherente con el estilo | Pendiente (el satélite del menú ya lleva el estilo) |
| F6 Matriz completa y PR | Pendiente |

## Para seguir en la fase 2

- La lista de materiales con estilo está en `AjusteDe` (`Assets/Editor/ConstructorEscenas.Tinta.cs`). Añadir ahí `Cohete_*`, `NASA_Cohete_*`, `Plataforma`, `Torre_*`, `Lanzamiento_*` y `Arbol_*`.
- Las mallas hechas por código necesitan la normal suavizada: pasar `contorno: true` a `GuardarMalla`.
- Las cofias usan materiales de dos caras (`_Cull 0`). Probar el contorno en ellas antes de darlo por bueno.
- Los árboles no tienen UV y son de caras planas: bandas sí, contorno mínimo o ninguno.
- Capturas en batch: el guion está en la sesión de trabajo (no en el repositorio).

## Decisiones pendientes del equipo

- Las dos órbitas nuevas (ecuatorial y areoestacionaria) siguen jugables por decisión de Diego. Falta que Adrian revise sus valores.
- Si el estilo se extiende o se ajusta su fuerza (grosor de la línea, tono de la sombra).
