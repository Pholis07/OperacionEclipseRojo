# Modelos de NASA 3D Resources → Unity

`convertir.mjs` pasa los modelos de [NASA 3D Resources](https://science.nasa.gov/3d-resources/) a OBJ + MTL + PNG, que Unity importa sin paquetes extra (los GLB vienen con compresión Draco y texturas WebP, que Unity no lee sin glTFast).

Hace cuatro cosas:
- Descomprime Draco, aplica las transformaciones de cada nodo y agrupa por material. Toma las texturas PNG que el GLB trae como respaldo de las WebP.
- Centra el modelo y lo escala para que su lado mayor mida `tamano` (2 por defecto).
- Simplifica con meshoptimizer si pasa de `triangulosMax`.
- Para STL (modelos de impresión 3D):
  - Suelda vértices y calcula normales.
  - Para el SLS, pinta por zonas: etapa central naranja, propulsores blancos, toberas oscuras.
  - Con `CORTE=0.665` separa la etapa superior (para la separación de la cinemática).

```bash
cd Herramientas/modelos_nasa && npm install
node convertir.mjs mro.glb ../../Assets/Arte/Modelos/NASA Sat_MRO 20000 2
CORTE=0.665 node convertir.mjs "Space Launch System (SLS).stl" ../../Assets/Arte/Modelos/NASA Cohete_SLS 36000 2 y
```

El último argumento `y` indica que el STL ya viene con Y hacia arriba (si no se pone, se gira de Z a Y).

| Modelo | Archivo de NASA | Triángulos en el juego |
|---|---|---|
| `Sat_MRO` | Mars Reconnaissance Orbiter (MRO) (C).glb | 14,753 |
| `Sat_Magellan` | Magellan.glb | 7,364 |
| `Sat_Chandra` | Chandra X-ray Observatory.glb | 20,000 (de 30,132) |
| `Sat_Aquarius` | Aquarius (B) (unfurled).glb | 10,362 |
| `Sat_LLCD` | Lunar Laser Communications Demonstration (LLCD).glb | 17,374 (de 182,818) |
| `Cohete_SLS_Inferior` / `_Superior` | Space Launch System (SLS).stl (zip) | 36,000 (de 1,555,637) |
| `Terreno_Gusev` | Mars - Spirit Landing Site.stl | 40,000 (de 488,890). Importado pero **sin usar todavía** |

Modelos y texturas: NASA (uso según las [guías de medios de NASA](https://www.nasa.gov/nasa-brand-center/images-and-media/)). NASA no respalda el juego.
