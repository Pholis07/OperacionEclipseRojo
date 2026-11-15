"""Cielo de estrellas propio (Assets/Arte/Texturas/Estrellas.png, 2048×1152).
Uso: python3 -I estrellas.py <carpeta Assets/Arte/Texturas>

Estrellas de varios tamaños y tonos (azuladas, blancas y cálidas), las más brillantes con un halo suave,
una banda tenue de Vía Láctea en diagonal y un par de nebulosas muy discretas. Todo procedural (sin fotos).
"""
import math
import random
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

SALIDA = Path(sys.argv[1])
W, H = 2048, 1152
random.seed(2026)

fondo = Image.new("RGB", (W, H), (6, 9, 18))

# Banda de la Vía Láctea: una franja diagonal con ruido de manchas, muy desenfocada.
banda = Image.new("L", (W, H), 0)
db = ImageDraw.Draw(banda)
for _ in range(2600):
    t = random.random()
    x = t * W * 1.2 - W * 0.1
    y = H * 0.85 - t * H * 0.7 + random.gauss(0, H * 0.09)
    r = random.uniform(6, 40)
    db.ellipse((x - r, y - r, x + r, y + r), fill=random.randint(14, 40))
banda = banda.filter(ImageFilter.GaussianBlur(28))
tinte = Image.merge("RGB", (banda.point(lambda v: int(v * 0.85)), banda.point(lambda v: int(v * 0.8)), banda.point(lambda v: int(v * 1.0))))
fondo = ImageChops.add(fondo, tinte)

# Nebulosas discretas (rojiza y azulada).
for color, (cx, cy), radio in (((40, 12, 10), (0.18, 0.22), 340), ((10, 20, 42), (0.82, 0.7), 420)):
    capa = Image.new("RGB", (W, H), (0, 0, 0))
    dc = ImageDraw.Draw(capa)
    for _ in range(90):
        x = cx * W + random.gauss(0, radio * 0.5)
        y = cy * H + random.gauss(0, radio * 0.35)
        r = random.uniform(30, 120)
        k = random.uniform(0.3, 1.0)
        dc.ellipse((x - r, y - r, x + r, y + r), fill=tuple(int(c * k) for c in color))
    fondo = ImageChops.add(fondo, capa.filter(ImageFilter.GaussianBlur(60)))

# Estrellas: muchas tenues y pocas brillantes; más densas cerca de la banda.
estrellas = Image.new("RGB", (W, H), (0, 0, 0))
de = ImageDraw.Draw(estrellas)
halos = Image.new("RGB", (W, H), (0, 0, 0))
dh = ImageDraw.Draw(halos)
tonos = [(170, 195, 255), (215, 228, 255), (255, 255, 255), (255, 240, 220), (255, 214, 170)]
pb = banda.load()
colocadas = 0
while colocadas < 7000:
    x, y = random.randrange(W), random.randrange(H)
    if random.random() > 0.35 + pb[x, y] / 30:
        continue
    colocadas += 1
    brillo = random.random() ** 3.2
    tono = random.choice(tonos)
    c = tuple(int(v * (0.25 + 0.75 * brillo)) for v in tono)
    if brillo > 0.94:
        r = 1.6
        de.ellipse((x - r, y - r, x + r, y + r), fill=c)
        g = random.uniform(3, 6)
        dh.ellipse((x - g, y - g, x + g, y + g), fill=tuple(int(v * 0.25) for v in tono))
    elif brillo > 0.35:
        de.rectangle((x, y, x + 1, y + 1), fill=c)
    else:
        de.point((x, y), fill=c)
halos = halos.filter(ImageFilter.GaussianBlur(4))
estrellas = ImageChops.add(estrellas, halos)
# Unas pocas con destello en cruz.
dd = ImageDraw.Draw(estrellas)
for _ in range(14):
    x, y = random.randrange(W), random.randrange(H)
    largo = random.randint(10, 22)
    for i in range(1, largo):
        a = int(200 * (1 - i / largo) ** 2)
        for px, py in ((x + i, y), (x - i, y), (x, y + i), (x, y - i)):
            if 0 <= px < W and 0 <= py < H:
                dd.point((px, py), fill=(a, a, a))
    dd.ellipse((x - 2, y - 2, x + 2, y + 2), fill=(255, 255, 255))

cielo = ImageChops.add(fondo, estrellas)
cielo.save(SALIDA / "Estrellas.png", optimize=True)
print("listo")
