"""Cráter Gale (sitio de aterrizaje de Curiosity) a partir del modelo de NASA 3D Resources.
Uso: python3 -I gale.py <Curiosity Landing Site.stl> <carpeta Assets/Arte>

El modelo de NASA es solo relieve (STL, sin textura). De él se sacan:
- Texturas/Gale_Altura.png: alturas (gris), 1024×1024, recorte cuadrado centrado en el cráter.
- Texturas/Gale_Normal.png: normales para el relieve.
- Texturas/Gale_Color.png: relieve coloreado con tonos de Marte (fondos oscuros, laderas ocre, polvo claro en
  las zonas altas) y sombreado de colinas, para el mapa de la investigación y el parche del globo.
- Modelos/NASA/Terreno_Gale.obj: malla de terreno simplificada (rejilla de 161×161) con UV.
"""
import math
import random
import struct
import sys
from pathlib import Path

from PIL import Image, ImageFilter

ORIGEN = Path(sys.argv[1])
ARTE = Path(sys.argv[2])
N = 1024

datos = ORIGEN.read_bytes()
cuantos = struct.unpack_from("<I", datos, 80)[0]
tris = []
for i in range(cuantos):
    base = 84 + i * 50
    v = struct.unpack_from("<12f", datos, base)
    tris.append(((v[3], v[4], v[5]), (v[6], v[7], v[8]), (v[9], v[10], v[11])))

# Eje «arriba»: el de menor extensión (es una losa).
mins = [min(t[k][e] for t in tris for k in range(3)) for e in range(3)]
maxs = [max(t[k][e] for t in tris for k in range(3)) for e in range(3)]
ext = [maxs[e] - mins[e] for e in range(3)]
arriba = ext.index(min(ext))
ejes = [e for e in range(3) if e != arriba]
ax, ay = ejes
print("triángulos", cuantos, "extensión", [round(x, 3) for x in ext], "arriba", arriba)

# Recorte cuadrado centrado en el cráter: lado = extensión menor del plano.
lado = min(ext[ax], ext[ay])
cx = mins[ax] + ext[ax] * 0.5
cy = mins[ay] + ext[ay] * 0.5
x0, y0 = cx - lado / 2, cy - lado / 2
alto = [[None] * N for _ in range(N)]

for (a, b, c) in tris:
    # Solo la cara de arriba (la losa tiene base y bordes).
    ux, uy, uz = b[0] - a[0], b[1] - a[1], b[2] - a[2]
    vx, vy, vz = c[0] - a[0], c[1] - a[1], c[2] - a[2]
    nrm = (uy * vz - uz * vy, uz * vx - ux * vz, ux * vy - uy * vx)
    if nrm[arriba] <= 0:
        continue
    pts = [((p[ax] - x0) / lado * (N - 1), (p[ay] - y0) / lado * (N - 1), p[arriba]) for p in (a, b, c)]
    minx = max(0, int(math.floor(min(p[0] for p in pts))))
    maxx = min(N - 1, int(math.ceil(max(p[0] for p in pts))))
    miny = max(0, int(math.floor(min(p[1] for p in pts))))
    maxy = min(N - 1, int(math.ceil(max(p[1] for p in pts))))
    if minx > maxx or miny > maxy:
        continue
    (x1, y1, z1), (x2, y2, z2), (x3, y3, z3) = pts
    det = (y2 - y3) * (x1 - x3) + (x3 - x2) * (y1 - y3)
    if abs(det) < 1e-12:
        continue
    for py in range(miny, maxy + 1):
        for px in range(minx, maxx + 1):
            l1 = ((y2 - y3) * (px - x3) + (x3 - x2) * (py - y3)) / det
            l2 = ((y3 - y1) * (px - x3) + (x1 - x3) * (py - y3)) / det
            l3 = 1 - l1 - l2
            if l1 < -1e-4 or l2 < -1e-4 or l3 < -1e-4:
                continue
            z = l1 * z1 + l2 * z2 + l3 * z3
            fila = alto[py]
            if fila[px] is None or z > fila[px]:
                fila[px] = z

# Huecos: se rellenan creciendo desde los vecinos válidos (varias pasadas).
for _ in range(40):
    cambio = False
    for y in range(N):
        for x in range(N):
            if alto[y][x] is None:
                vec = [alto[yy][xx] for xx, yy in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1))
                       if 0 <= xx < N and 0 <= yy < N and alto[yy][xx] is not None]
                if vec:
                    alto[y][x] = sum(vec) / len(vec)
                    cambio = True
    if not cambio:
        break
validos = [z for fila in alto for z in fila if z is not None]
zmin, zmax = min(validos), max(validos)
for y in range(N):
    for x in range(N):
        if alto[y][x] is None:
            alto[y][x] = zmin
print("alturas", round(zmin, 4), round(zmax, 4))

# Imagen de alturas (0–255): sin escalones (mediana + desenfoque) y con detalle fino propio (ruido en
# varias octavas y cráteres pequeños), porque el modelo de NASA es de poca resolución.
img = Image.new("L", (N, N))
pi = img.load()
for y in range(N):
    for x in range(N):
        pi[x, N - 1 - y] = int((alto[y][x] - zmin) / (zmax - zmin) * 255)
img = img.filter(ImageFilter.MedianFilter(7)).filter(ImageFilter.GaussianBlur(5))
random.seed(9)
detalle = Image.new("L", (N, N), 128)
for radio, peso in ((96, 22), (32, 14), (10, 9), (3, 6)):
    capa = Image.effect_noise((N // radio + 2, N // radio + 2), 64).resize((N, N), Image.BICUBIC)
    detalle = Image.blend(detalle, capa, peso / 60)
pd = detalle.load()
pi = img.load()
crateres = [(random.randrange(N), random.randrange(N), random.uniform(4, 22)) for _ in range(70)]
for y in range(N):
    for x in range(N):
        v = pi[x, y] + (pd[x, y] - 128) * 0.35
        for cx_, cy_, r in crateres:
            d = math.hypot(x - cx_, y - cy_) / r
            if d < 1.4:
                v += (-6 * (1 - d * d) if d < 1 else 3 * (1.4 - d) / 0.4) * (r / 22)
        pi[x, y] = max(0, min(255, int(v)))
img.save(ARTE / "Texturas/Gale_Altura.png", optimize=True)
h = img.load()

# Normales y sombreado de colinas (luz del noroeste, como en los mapas).
fuerza = 9.0
normal = Image.new("RGB", (N, N))
pn = normal.load()
sombra = [[0.0] * N for _ in range(N)]
luz = (-0.6, 0.6, 0.55)
ll = math.sqrt(sum(c * c for c in luz))
luz = tuple(c / ll for c in luz)
for y in range(N):
    for x in range(N):
        dx = (h[min(N - 1, x + 1), y] - h[max(0, x - 1), y]) / 255.0 * fuerza
        dy = (h[x, max(0, y - 1)] - h[x, min(N - 1, y + 1)]) / 255.0 * fuerza
        l = math.sqrt(dx * dx + dy * dy + 1.0)
        nx, ny, nz = -dx / l, -dy / l, 1.0 / l
        pn[x, y] = (int((nx * 0.5 + 0.5) * 255), int((ny * 0.5 + 0.5) * 255), int((nz * 0.5 + 0.5) * 255))
        sombra[y][x] = max(0.0, nx * luz[0] + ny * luz[1] + nz * luz[2])
normal.save(ARTE / "Texturas/Gale_Normal.png", optimize=True)

# Color: paleta de Marte por altura y pendiente, con grano.
random.seed(4)
paleta = [  # (altura 0–1, color)
    (0.00, (92, 50, 34)),     # fondos más oscuros (arena basáltica)
    (0.35, (146, 80, 46)),    # llanura ocre
    (0.65, (178, 104, 60)),
    (1.00, (212, 150, 102)),  # cumbres con polvo claro
]


def tono(t):
    for i in range(len(paleta) - 1):
        a, ca = paleta[i]
        b, cb = paleta[i + 1]
        if t <= b:
            k = (t - a) / (b - a)
            return tuple(ca[j] + (cb[j] - ca[j]) * k for j in range(3))
    return paleta[-1][1]


grano = Image.effect_noise((N, N), 18).filter(ImageFilter.GaussianBlur(0.6)).load()
manchas = Image.effect_noise((N // 64, N // 64), 70).resize((N, N), Image.BICUBIC).load()   # campos de dunas oscuras
color = Image.new("RGB", (N, N))
pc = color.load()
for y in range(N):
    for x in range(N):
        t = h[x, y] / 255.0
        c = tono(0.2 + 0.7 * t)
        oscuro = max(0.0, (128 - manchas[x, y]) / 128.0) * (1 - t) * 0.45
        s = 0.3 + 0.95 * sombra[y][x]
        g = (grano[x, y] - 128) / 128.0 * 0.05
        pc[x, y] = tuple(max(0, min(255, int(ch * (s + g) * (1 - oscuro)))) for ch in c)
color.save(ARTE / "Texturas/Gale_Color.png", optimize=True)

# Malla de terreno: rejilla 161×161 (lado 1, alto relativo), UV del recorte.
M = 161
escala_alto = 0.06
lineas = ["# Terreno del cráter Gale (NASA 3D Resources, Curiosity Landing Site), simplificado", "mtllib Terreno_Gale.mtl", "o Terreno_Gale"]
for j in range(M):
    for i in range(M):
        u, v = i / (M - 1), j / (M - 1)
        z = h[min(N - 1, int(u * (N - 1))), min(N - 1, int((1 - v) * (N - 1)))] / 255.0
        lineas.append(f"v {u - 0.5:.5f} {z * escala_alto:.5f} {v - 0.5:.5f}")
for j in range(M):
    for i in range(M):
        lineas.append(f"vt {i / (M - 1):.5f} {j / (M - 1):.5f}")
lineas.append("usemtl terreno")
for j in range(M - 1):
    for i in range(M - 1):
        a = j * M + i + 1
        b = a + 1
        c = a + M
        d = c + 1
        lineas.append(f"f {a}/{a} {c}/{c} {b}/{b}")
        lineas.append(f"f {b}/{b} {c}/{c} {d}/{d}")
(ARTE / "Modelos/NASA/Terreno_Gale.obj").write_text("\n".join(lineas) + "\n")
(ARTE / "Modelos/NASA/Terreno_Gale.mtl").write_text("newmtl terreno\nKd 0.8 0.5 0.3\n")
print("listo")
