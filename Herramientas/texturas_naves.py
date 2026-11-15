"""Versión 5, bloque 6: texturas propias de las naves y cielo de la NASA.
- Lámina dorada arrugada (MLI) con su mapa de normales: facetas de Voronoi con inclinación al azar.
- Espuma naranja del SLS con normales: ruido suave en varias escalas, con vetas verticales.
- Metal cepillado: gris claro con trazos horizontales (el color lo pone el material).
- Cielo: el Deep Star Map 2020 (NASA SVS, dominio público) en coordenadas galácticas, de EXR a JPG 8k,
  y un recorte de 2048x1152 a escala 1:1 para los fondos planos.
Todas las texturas de nave se repiten sin costura (todo se calcula con vueltas, np.roll).
Uso: python3 -I texturas_naves.py <carpeta Assets/Arte> [starmap_2020_8k_gal.exr]
El cielo necesita oiiotool y ImageMagick (magick)."""
import subprocess
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ARTE = Path(sys.argv[1])
EXR = Path(sys.argv[2]) if len(sys.argv) > 2 else None
LADO = 512
rng = np.random.default_rng(2026)


def guardar(arr, nombre):
    ruta = ARTE / "Texturas" / nombre
    Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8)).save(ruta, optimize=True)
    print("guardado", ruta.name)


def normales(altura, fuerza):
    """Mapa de normales (tangente, verde arriba como Unity) a partir de una altura 0..1 que se repite."""
    dx = (np.roll(altura, -1, 1) - np.roll(altura, 1, 1)) * fuerza
    dy = (np.roll(altura, -1, 0) - np.roll(altura, 1, 0)) * fuerza
    n = np.dstack([-dx, dy, np.ones_like(altura)])
    n /= np.linalg.norm(n, axis=2, keepdims=True)
    return (n * 0.5 + 0.5) * 255


def suave(ruido, radio):
    """Desenfoque con vuelta (por FFT): el resultado se repite sin costura."""
    f = np.fft.fftfreq(ruido.shape[0])
    k = np.exp(-(f[:, None] ** 2 + f[None, :] ** 2) * (2 * np.pi * radio) ** 2 / 2)
    return np.real(np.fft.ifft2(np.fft.fft2(ruido) * k))


def normalizar(a):
    return (a - a.min()) / (a.max() - a.min())


# ---------- Lámina dorada arrugada ----------
y, x = np.mgrid[0:LADO, 0:LADO].astype(np.float32)
puntos = rng.random((140, 2)) * LADO
inclinacion = np.clip(rng.normal(0, 0.7, (140, 2)), -1.5, 1.5)
d1 = np.full((LADO, LADO), np.inf, np.float32)
d2 = np.full((LADO, LADO), np.inf, np.float32)
celda = np.zeros((LADO, LADO), np.int32)
for i, (px, py) in enumerate(puntos):
    dx = np.abs(x - px); dx = np.minimum(dx, LADO - dx)
    dy = np.abs(y - py); dy = np.minimum(dy, LADO - dy)
    d = np.hypot(dx, dy)
    mas_cerca = d < d1
    d2 = np.where(mas_cerca, d1, np.minimum(d2, d))
    celda = np.where(mas_cerca, i, celda)
    d1 = np.where(mas_cerca, d, d1)
# Cada faceta es un plano inclinado; los pliegues son el salto de inclinación entre facetas.
gx, gy = inclinacion[celda, 0], inclinacion[celda, 1]
pliegue = 0.7 + 0.3 * np.clip((d2 - d1) / 3.0, 0, 1)
n = np.dstack([gx * 0.35 * pliegue, gy * 0.35 * pliegue, np.ones_like(gx)])
n /= np.linalg.norm(n, axis=2, keepdims=True)
guardar((n * 0.5 + 0.5) * 255, "Lamina_Arrugada_Normal.png")
brillo = 0.88 + 0.12 * (gx * 0.6 + gy * 0.4) / 2.5
dorado = np.array([226, 172, 72], np.float32)
guardar(dorado[None, None, :] * brillo[..., None], "Lamina_Arrugada.png")

# ---------- Espuma naranja (tanque del SLS) ----------
altura = sum(normalizar(suave(rng.random((LADO, LADO)), r)) * p for r, p in ((24, 0.5), (8, 0.3), (2, 0.2)))
vetas = normalizar(suave(rng.random((1, LADO)).repeat(LADO, 0), 3))   # vetas verticales del rociado
altura = normalizar(altura * 0.8 + vetas * 0.2)
guardar(normales(altura, 3.0), "Espuma_Naranja_Normal.png")
tono = 0.84 + 0.22 * altura
naranja = np.array([214, 112, 44], np.float32)
guardar(naranja[None, None, :] * tono[..., None], "Espuma_Naranja.png")

# ---------- Metal cepillado ----------
trazos = suave(rng.random((LADO, LADO)), 1)
trazos = np.real(np.fft.ifft2(np.fft.fft2(trazos) * np.exp(-(np.fft.fftfreq(LADO)[None, :] * 2 * np.pi * 40) ** 2 / 2)))
gris = 205 + 40 * (normalizar(trazos) - 0.5)
guardar(np.dstack([gris] * 3), "Metal_Cepillado.png")

# ---------- Cielo de la NASA ----------
if EXR is not None:
    tif = ARTE / "Texturas" / "_cielo_temporal.tif"
    # El EXR es lineal y muy oscuro (media 0.0035): ×2.5 deja la Vía Láctea suave y las estrellas nítidas.
    subprocess.run(["oiiotool", str(EXR), "--mulc", "2.5,2.5,2.5", "--colorconvert", "linear", "sRGB",
                    "-d", "uint8", "-o", str(tif)], check=True)
    subprocess.run(["magick", str(tif), "-quality", "90", str(ARTE / "Texturas" / "Cielo_NASA.jpg")], check=True)
    # Zona del anticentro galáctico: la banda de la Vía Láctea cruza tenue y no compite con la interfaz.
    subprocess.run(["magick", str(tif), "-crop", "2048x1152+6000+1100", "+repage",
                    str(ARTE / "Texturas" / "Estrellas.png")], check=True)
    tif.unlink()
    print("guardado Cielo_NASA.jpg y Estrellas.png")
