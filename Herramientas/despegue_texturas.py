"""Texturas propias (procedurales) del complejo de lanzamiento de la cinemática.
Uso: python3 -I despegue_texturas.py <carpeta Assets/Arte>

- Texturas/Nubes.png: nubes que se repiten (para el shader Cielo), 512×512, gris en el canal alfa.
- Interfaz/Nube.png: bocanada suave de nube o humo (partículas), 256×256.
- Texturas/Pasto.png: pasto y tierra que se repiten, 512×512.
- Texturas/Losa.png: losa de concreto de la plataforma con marcas amarillas y negras, 512×512.
- Interfaz/Ave.png: silueta de ave en «V» (bandada que sale volando al encender), 64×64.
- Interfaz/Chispa.png: chispa alargada (partículas estiradas), 32×128.
"""
import math
import random
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ARTE = Path(sys.argv[1])
random.seed(11)


def ruido(n, celdas):
    """Ruido de valor que se repite (n×n) con interpolación suave."""
    rejilla = [[random.random() for _ in range(celdas)] for _ in range(celdas)]

    def v(x, y):
        gx, gy = x * celdas / n, y * celdas / n
        x0, y0 = int(gx) % celdas, int(gy) % celdas
        x1, y1 = (x0 + 1) % celdas, (y0 + 1) % celdas
        fx, fy = gx - int(gx), gy - int(gy)
        fx, fy = fx * fx * (3 - 2 * fx), fy * fy * (3 - 2 * fy)
        a = rejilla[y0][x0] * (1 - fx) + rejilla[y0][x1] * fx
        b = rejilla[y1][x0] * (1 - fx) + rejilla[y1][x1] * fx
        return a * (1 - fy) + b * fy
    return v


def fbm(n, octavas):
    capas = [(ruido(n, c), p) for c, p in octavas]
    return lambda x, y: sum(f(x, y) * p for f, p in capas)


# Nubes del cielo: manchas blandas, con huecos de cielo azul (alfa = densidad).
N = 512
f = fbm(N, [(4, 0.5), (8, 0.25), (16, 0.15), (32, 0.1)])
nubes = Image.new("RGBA", (N, N))
pn = nubes.load()
for y in range(N):
    for x in range(N):
        d = max(0.0, min(1.0, (f(x, y) - 0.48) * 3.2))
        pn[x, y] = (255, 255, 255, int(255 * d))
nubes.save(ARTE / "Texturas/Nubes.png", optimize=True)

# Bocanada: círculo suave con borde irregular (humo, vapor y nubes en partículas).
M = 256
g = fbm(M, [(4, 0.6), (8, 0.4)])
nube = Image.new("RGBA", (M, M))
pb = nube.load()
for y in range(M):
    for x in range(M):
        d = math.hypot((x + 0.5) / M * 2 - 1, (y + 0.5) / M * 2 - 1)
        borde = 0.75 + 0.25 * g(x, y)
        a = max(0.0, 1 - d / borde) ** 1.6
        pb[x, y] = (255, 255, 255, int(255 * a))
nube.filter(ImageFilter.GaussianBlur(1.5)).save(ARTE / "Interfaz/Nube.png", optimize=True)

# Pasto: verdes con manchas de tierra.
h = fbm(N, [(8, 0.5), (32, 0.3), (128, 0.2)])
pasto = Image.new("RGB", (N, N))
pp = pasto.load()
for y in range(N):
    for x in range(N):
        v = h(x, y)
        tierra = max(0.0, min(1.0, (v - 0.62) * 4))
        r = int((70 + 40 * v) * (1 - tierra) + 120 * tierra)
        gg = int((105 + 50 * v) * (1 - tierra) + 100 * tierra)
        b = int((45 + 20 * v) * (1 - tierra) + 70 * tierra)
        pp[x, y] = (r, gg, b)
pasto.save(ARTE / "Texturas/Pasto.png", optimize=True)

# Losa: concreto con juntas, manchas de quemado al centro y marcas amarillas y negras.
c = fbm(N, [(16, 0.5), (64, 0.5)])
losa = Image.new("RGB", (N, N))
pl = losa.load()
for y in range(N):
    for x in range(N):
        v = c(x, y)
        quemado = max(0.0, 1 - math.hypot(x - N / 2, y - N / 2) / (N * 0.32))
        base = 150 + 40 * v - 90 * quemado ** 1.5
        pl[x, y] = (int(base), int(base * 0.98), int(base * 0.94))
dl = ImageDraw.Draw(losa)
for k in range(0, N, 64):   # juntas
    dl.line((k, 0, k, N), fill=(105, 104, 100), width=2)
    dl.line((0, k, N, k), fill=(105, 104, 100), width=2)
for i in range(0, N, 24):   # borde de seguridad amarillo y negro
    color = (225, 180, 30) if (i // 24) % 2 == 0 else (25, 25, 25)
    for (a, b) in ((0, 0), (0, N - 14)):
        dl.polygon([(i, b), (i + 24, b), (i + 34, b + 14), (i + 10, b + 14)], fill=color)
        dl.polygon([(b, i), (b, i + 24), (b + 14, i + 34), (b + 14, i + 10)], fill=color)
dl.ellipse((N / 2 - 150, N / 2 - 150, N / 2 + 150, N / 2 + 150), outline=(225, 180, 30), width=5)
losa.save(ARTE / "Texturas/Losa.png", optimize=True)

# Ave: «V» con alas curvas.
A = 64
ave = Image.new("RGBA", (A * 4, A * 4))
da = ImageDraw.Draw(ave)
pts_i = [(A * 4 * t, A * 4 * (0.45 + 0.25 * math.sin(math.pi * t))) for t in [i / 20 for i in range(11)]]
da.line([(x, y) for x, y in pts_i], fill=(20, 22, 26, 255), width=18, joint="curve")
pts_d = [(A * 4 * (0.5 + t), A * 4 * (0.45 + 0.25 * math.sin(math.pi * (1 - t)))) for t in [i / 20 for i in range(11)]]
da.line([(x, y) for x, y in pts_d], fill=(20, 22, 26, 255), width=18, joint="curve")
ave.resize((A, A), Image.LANCZOS).save(ARTE / "Interfaz/Ave.png", optimize=True)

# Chispa alargada.
ch = Image.new("RGBA", (32, 128))
pc = ch.load()
for y in range(128):
    for x in range(32):
        dx = abs((x + 0.5) / 32 * 2 - 1)
        dy = abs((y + 0.5) / 128 * 2 - 1)
        a = max(0.0, 1 - dx) ** 2 * max(0.0, 1 - dy) ** 1.2
        pc[x, y] = (255, 255, 255, int(255 * a))
ch.save(ARTE / "Interfaz/Chispa.png", optimize=True)
print("listo")
