"""Marte en alta resolución para el shader Planeta (8K efectivo sin pasar el límite de 4096 de WebGL).
Uso: python3 -I marte_hd.py <8k_mars.jpg de Solar System Scope> <carpeta Assets/Arte/Texturas>

- Marte_Superficie_Oeste.jpg / _Este.jpg: las dos mitades (longitud 0–180° y 180–360°) a 4096×4096.
- Marte_Normal.png: relieve aproximado a partir del brillo, ahora a 4096×2048.
- Marte_Detalle.png: ruido fino que se repite (512×512, gris medio) para que de cerca no se vea borroso.
"""
import math
import random
import sys
from pathlib import Path

from PIL import Image, ImageEnhance, ImageFilter

ORIGEN = Path(sys.argv[1])
SALIDA = Path(sys.argv[2])

mapa = Image.open(ORIGEN).convert("RGB")
if mapa.size != (8192, 4096):
    mapa = mapa.resize((8192, 4096), Image.LANCZOS)
mapa = ImageEnhance.Color(mapa).enhance(0.95)
mapa = ImageEnhance.Contrast(mapa).enhance(1.06)
mapa.crop((0, 0, 4096, 4096)).save(SALIDA / "Marte_Superficie_Oeste.jpg", quality=88, optimize=True)
mapa.crop((4096, 0, 8192, 4096)).save(SALIDA / "Marte_Superficie_Este.jpg", quality=88, optimize=True)

# Normales: diferencias del brillo (suavizado) con continuidad en la costura de longitud.
alto = mapa.convert("L").resize((4096, 2048), Image.LANCZOS).filter(ImageFilter.GaussianBlur(1.2))
w, h = alto.size
datos = alto.tobytes()
fuerza = 2.6
salida = bytearray(w * h * 3)
for y in range(h):
    fila = y * w
    arriba = max(0, y - 1) * w
    abajo = min(h - 1, y + 1) * w
    for x in range(w):
        dx = (datos[fila + (x + 1) % w] - datos[fila + (x - 1) % w]) / 255.0 * fuerza
        dy = (datos[abajo + x] - datos[arriba + x]) / 255.0 * fuerza
        l = math.sqrt(dx * dx + dy * dy + 1.0)
        i = (fila + x) * 3
        salida[i] = int((-dx / l * 0.5 + 0.5) * 255)
        salida[i + 1] = int((dy / l * 0.5 + 0.5) * 255)
        salida[i + 2] = int((1.0 / l * 0.5 + 0.5) * 255)
Image.frombytes("RGB", (w, h), bytes(salida)).save(SALIDA / "Marte_Normal.png", optimize=True)

# Detalle: ruido de valor en varias octavas, que se repite sin costura.
random.seed(7)
N = 512


def octava(celdas):
    rejilla = [[random.random() for _ in range(celdas)] for _ in range(celdas)]

    def valor(x, y):
        gx, gy = x * celdas / N, y * celdas / N
        x0, y0 = int(gx) % celdas, int(gy) % celdas
        x1, y1 = (x0 + 1) % celdas, (y0 + 1) % celdas
        fx, fy = gx - int(gx), gy - int(gy)
        fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
        a = rejilla[y0][x0] * (1 - fx) + rejilla[y0][x1] * fx
        b = rejilla[y1][x0] * (1 - fx) + rejilla[y1][x1] * fx
        return a * (1 - fy) + b * fy
    return valor


octavas = [(octava(8), 0.45), (octava(32), 0.3), (octava(128), 0.25)]
detalle = Image.new("L", (N, N))
pd = detalle.load()
for y in range(N):
    for x in range(N):
        v = sum(f(x, y) * p for f, p in octavas)
        pd[x, y] = max(0, min(255, int(128 + (v - 0.5) * 200)))
detalle.save(SALIDA / "Marte_Detalle.png", optimize=True)
print("listo")
