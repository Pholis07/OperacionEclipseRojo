"""Quita el fondo blanco de los logos del patrocinador y genera una versión clara para fondos oscuros.
Uso: python3 -I logos.py <logo.jpg> <nombre_salida> <carpeta_salida>

- El fondo es el blanco conectado con los bordes de la imagen: el blanco de dentro del logo (rodeado por
  el contorno negro) se conserva.
- En el borde entre la tinta negra y el fondo se calcula el alfa a partir de la luminancia, para que no
  quede un halo gris.
- «<nombre>.png»: colores originales. «<nombre>_Claro.png»: la tinta negra pasa a blanco (el rojo, el azul y
  el blanco del toro no cambian), para usarlo sobre fondos oscuros.
"""
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

ORIGEN = Path(sys.argv[1])
NOMBRE = sys.argv[2]
SALIDA = Path(sys.argv[3])
SALIDA.mkdir(parents=True, exist_ok=True)

UMBRAL_CLARO = 200   # canal mínimo por encima del cual un píxel cuenta como «blanco»
LADO_MAXIMO = 1024

im = Image.open(ORIGEN).convert("RGB")
ancho, alto = im.size
px = im.load()

# 1. Máscara de píxeles claros y relleno desde todo el borde: lo que se alcanza es el fondo.
claros = Image.new("L", im.size, 0)
pc = claros.load()
for y in range(alto):
    for x in range(ancho):
        r, g, b = px[x, y]
        if min(r, g, b) > UMBRAL_CLARO:
            pc[x, y] = 255
FONDO = 128
for x in range(ancho):
    for y in (0, alto - 1):
        if pc[x, y] == 255:
            ImageDraw.floodfill(claros, (x, y), FONDO, thresh=0)
for y in range(alto):
    for x in (0, ancho - 1):
        if pc[x, y] == 255:
            ImageDraw.floodfill(claros, (x, y), FONDO, thresh=0)

fondo = claros.point(lambda v: 255 if v == FONDO else 0)
cerca = fondo.filter(ImageFilter.MaxFilter(5))  # franja de 2 px alrededor del fondo
pf, pn = fondo.load(), cerca.load()

# 2. Alfa: 0 en el fondo; en la franja del borde, la tinta es negra: observado = a·negro + (1 − a)·blanco.
salida = Image.new("RGBA", im.size)
clara = Image.new("RGBA", im.size)
ps, pl = salida.load(), clara.load()
for y in range(alto):
    for x in range(ancho):
        r, g, b = px[x, y]
        if pf[x, y]:
            ps[x, y] = pl[x, y] = (0, 0, 0, 0)
            continue
        maximo, minimo = max(r, g, b), min(r, g, b)
        saturacion = (maximo - minimo) / maximo if maximo else 0
        if pn[x, y] and saturacion < 0.25:
            lum = (r * 299 + g * 587 + b * 114) // 1000
            a = max(0, min(255, round((255 - lum) * 255 / 200)))
            ps[x, y] = (0, 0, 0, a)
            pl[x, y] = (255, 255, 255, a)
            continue
        ps[x, y] = (r, g, b, 255)
        # Tinta: oscura y sin color. Pasa a blanco conservando el antialias con el relleno de color.
        if saturacion < 0.25 and maximo < 110:
            pl[x, y] = (255, 255, 255, 255)
        elif saturacion < 0.25 and maximo < 200 and not (minimo > 150):
            t = (maximo - 110) / 90
            v = round(255 - (255 - maximo) * t)
            pl[x, y] = (v, v, v, 255)
        else:
            pl[x, y] = (r, g, b, 255)

# 3. El toro se queda como el original en la versión clara (contorno negro y colores) con un borde blanco
#    fino tipo «pegatina»: solo el texto y las estrellas pasan a blanco.
relleno = Image.new("L", im.size, 0)
pr = relleno.load()
semillas = {}   # una en el rojo y otra en el azul: en algunos logos una línea oscura los separa
for y in range(alto):
    for x in range(ancho):
        r, g, b = px[x, y]
        maximo, minimo = max(r, g, b), min(r, g, b)
        coloreado = maximo > 80 and (maximo - minimo) / maximo > 0.35
        if coloreado or pc[x, y] == 255:
            pr[x, y] = 255
            if coloreado and "rojo" not in semillas and r > 150 and g < 90 and b < 90:
                semillas["rojo"] = (x, y)
            if coloreado and "azul" not in semillas and b > 120 and r < 100:
                semillas["azul"] = (x, y)
for semilla in semillas.values():
    if pr[semilla] == 255:
        ImageDraw.floodfill(relleno, semilla, 128, thresh=0)
toro = relleno.point(lambda v: 255 if v == 128 else 0)
grosor = max(4, round(ancho * 0.011))   # pasadas de 2 px: el contorno mide ~1.7 % del ancho
for _ in range(grosor):
    toro = toro.filter(ImageFilter.MaxFilter(5))   # cubre el contorno negro del toro
pt = toro.load()
opaco = Image.new("L", im.size, 0)
po = opaco.load()
for y in range(alto):
    for x in range(ancho):
        if pt[x, y] and ps[x, y][3] > 0:
            pl[x, y] = ps[x, y]
            po[x, y] = ps[x, y][3]
borde = opaco
for _ in range(2):
    borde = borde.filter(ImageFilter.MaxFilter(5))
borde = borde.filter(ImageFilter.GaussianBlur(1.2))
pb = borde.load()
for y in range(alto):
    for x in range(ancho):
        if pt[x, y]:
            r, g, b, a = pl[x, y]
            fa = pb[x, y]
            if a < 255 and fa > 0:
                # el original encima del borde blanco
                af = a / 255
                bb = fa / 255
                ao = af + bb * (1 - af)
                if ao > 0:
                    c = [round((ch * af + 255 * bb * (1 - af)) / ao) for ch in (r, g, b)]
                    pl[x, y] = (*c, round(ao * 255))

# 4. Recorte al contenido, cuadrado con margen, y tamaño máximo.
caja = clara.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
x0, y0, x1, y1 = caja
lado = max(x1 - x0, y1 - y0)
margen = round(lado * 0.03)
lado += margen * 2
for imagen, sufijo in ((salida, ""), (clara, "_Claro")):
    lienzo = Image.new("RGBA", (lado, lado), (0, 0, 0, 0))
    lienzo.paste(imagen.crop((x0, y0, x1, y1)), ((lado - (x1 - x0)) // 2, (lado - (y1 - y0)) // 2))
    if lado > LADO_MAXIMO:
        lienzo = lienzo.resize((LADO_MAXIMO, LADO_MAXIMO), Image.LANCZOS)
    lienzo.save(SALIDA / f"{NOMBRE}{sufijo}.png", optimize=True)
    print(SALIDA / f"{NOMBRE}{sufijo}.png", lienzo.size)
