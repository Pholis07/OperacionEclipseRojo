"""Imágenes compuestas a partir de los logos ya recortados (logos.py) y texturas de interfaz.
Uso: python3 -I composiciones.py <carpeta Assets/Arte>

- Patrocinio/Patrocinio_Splash.png: los dos logos claros juntos, para el splash de Unity.
- Interfaz/Vineta.png: viñeta radial (negro con alfa) para el velo de la pantalla de título.
- Interfaz/Degradado.png: degradado vertical de alfa (abajo opaco, arriba transparente).
- Interfaz/Resplandor.png: brillo radial que llega a 0 en el borde (para materiales aditivos).
- Interfaz/Anillo.png: aro con cuatro marcas para los zócalos de instrumentos.
- Interfaz/Reticula.png: cuadro de la retícula del mapa de la región (se repite).
"""
import math
import sys
from pathlib import Path

from PIL import Image

ARTE = Path(sys.argv[1])

# Splash: los dos logos claros lado a lado, con aire entre ellos.
a = Image.open(ARTE / "Patrocinio/Patrocinio_Fence_Claro.png").convert("RGBA")
b = Image.open(ARTE / "Patrocinio/Patrocinio_Security_Claro.png").convert("RGBA")
lado = 440
a = a.resize((lado, lado), Image.LANCZOS)
b = b.resize((lado, lado), Image.LANCZOS)
hueco = 120
splash = Image.new("RGBA", (lado * 2 + hueco + 40, lado + 40), (0, 0, 0, 0))
splash.paste(a, (20, 20), a)
splash.paste(b, (20 + lado + hueco, 20), b)
splash.save(ARTE / "Patrocinio/Patrocinio_Splash.png", optimize=True)

# Viñeta: centro con alfa 0.45 y bordes casi opacos (curva suave).
ancho, alto = 512, 288
vineta = Image.new("RGBA", (ancho, alto))
pv = vineta.load()
for y in range(alto):
    for x in range(ancho):
        dx = (x + 0.5) / ancho * 2 - 1
        dy = (y + 0.5) / alto * 2 - 1
        d = min(1.0, math.sqrt(dx * dx * 0.85 + dy * dy * 1.1) / 1.25)
        t = d * d * (3 - 2 * d)
        pv[x, y] = (0, 0, 0, round(255 * (0.45 + 0.52 * t)))
vineta.save(ARTE / "Interfaz/Vineta.png", optimize=True)

# Degradado vertical: abajo opaco, arriba transparente (franjas inferiores sobre la escena).
deg = Image.new("RGBA", (8, 256))
pd = deg.load()
for y in range(256):
    t = 1 - y / 255
    for x in range(8):
        pd[x, y] = (255, 255, 255, round(255 * t * t))
deg.save(ARTE / "Interfaz/Degradado.png", optimize=True)
# Resplandor: radial que llega exactamente a 0 en el borde (brillos aditivos sin que se vea el cuadrado).
N = 256
res = Image.new("RGBA", (N, N))
pr = res.load()
for y in range(N):
    for x in range(N):
        d = min(1.0, math.hypot((x + 0.5) / N * 2 - 1, (y + 0.5) / N * 2 - 1))
        v = (1 - d) ** 2.2 * (1 - d * d)
        pr[x, y] = (255, 255, 255, round(255 * v))
res.save(ARTE / "Interfaz/Resplandor.png", optimize=True)

# Anillo de zócalo: aro fino con cuatro marcas, suavizado (para los montajes de instrumentos).
N = 256
anillo = Image.new("RGBA", (N, N))
pa = anillo.load()
for y in range(N):
    for x in range(N):
        dx, dy = (x + 0.5) / N * 2 - 1, (y + 0.5) / N * 2 - 1
        d = math.hypot(dx, dy)
        aro = max(0.0, 1 - abs(d - 0.78) / 0.05)
        brillo = max(0.0, 1 - abs(d - 0.78) / 0.22) * 0.25
        ang = math.atan2(dy, dx)
        marca = 0.0
        if 0.84 < d < 0.98 and min(abs(math.sin(ang * 2)), 1) < 0.08:
            marca = 1.0
        v = min(1.0, aro + brillo + marca)
        pa[x, y] = (255, 255, 255, round(255 * v))
anillo.save(ARTE / "Interfaz/Anillo.png", optimize=True)

# Retícula del mapa: un cuadro con borde fino y marcas en las esquinas (se repite 4×4 sobre el mapa).
R = 256
ret = Image.new("RGBA", (R, R), (0, 0, 0, 0))
pr2 = ret.load()
for i in range(R):
    for (x, y) in ((i, 0), (0, i)):
        pr2[x, y] = (255, 255, 255, 200 if i % 8 < 4 else 90)
for k in range(-6, 7):   # cruz pequeña en el centro del cuadro
    pr2[R // 2 + k, R // 2] = (255, 255, 255, 150)
    pr2[R // 2, R // 2 + k] = (255, 255, 255, 150)
ret.save(ARTE / "Interfaz/Reticula.png", optimize=True)
print("listo")
