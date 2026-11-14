"""Procesa las imágenes de NASA y genera las texturas propias del juego.
Uso: python3 -I arte.py <carpeta_descargas_nasa> <carpeta_salida>"""
import math
import random
import sys
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageEnhance

ORIGEN = Path(sys.argv[1])
SALIDA = Path(sys.argv[2])
for sub in ("Fondos", "Evidencias", "Texturas", "Interfaz"):
    (SALIDA / sub).mkdir(parents=True, exist_ok=True)

FONDO = (11, 11, 16)


def abrir(nombre):
    return Image.open(ORIGEN / f"{nombre}.jpg").convert("RGB")


def cubrir(im, ancho, alto):
    """Escala y recorta al centro para llenar ancho x alto."""
    escala = max(ancho / im.width, alto / im.height)
    im = im.resize((math.ceil(im.width * escala), math.ceil(im.height * escala)), Image.LANCZOS)
    x = (im.width - ancho) // 2
    y = (im.height - alto) // 2
    return im.crop((x, y, x + ancho, y + alto))


def vineta(im, fuerza=0.75):
    w, h = im.size
    mascara = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(mascara)
    for i in range(40):
        t = i / 40
        d.ellipse((-w * 0.25 + t * w * 0.2, -h * 0.35 + t * h * 0.2, w * 1.25 - t * w * 0.2, h * 1.35 - t * h * 0.2),
                  fill=int(255 * t))
    mascara = mascara.filter(ImageFilter.GaussianBlur(w / 20))
    oscuro = Image.new("RGB", (w, h), FONDO)
    return Image.composite(im, Image.blend(im, oscuro, fuerza), mascara)


def tratar_fondo(im, oscurecer=0.72, desenfoque=3):
    """Estilo común de los fondos: desenfocado, oscuro, algo frío y con viñeta."""
    im = im.filter(ImageFilter.GaussianBlur(desenfoque))
    im = ImageEnhance.Color(im).enhance(0.8)
    frio = Image.new("RGB", im.size, (40, 70, 95))
    im = Image.blend(im, frio, 0.12)
    im = Image.blend(im, Image.new("RGB", im.size, FONDO), oscurecer)
    return vineta(im, 0.6)


def rellenar_huecos(im, umbral=14, pasos=60):
    """Rellena las zonas negras (huecos entre órbitas del mosaico) con el color de alrededor."""
    gris = im.convert("L")
    hueco = gris.point(lambda v: 255 if v < umbral else 0).filter(ImageFilter.MaxFilter(5))
    valido = ImageChops.invert(hueco)
    actual = im.copy()
    negro = Image.new("RGB", im.size, (0, 0, 0))
    for _ in range(pasos):
        # Solo los píxeles válidos aportan color (los demás se ponen a negro antes de desenfocar).
        borroso = Image.composite(actual, negro, valido).filter(ImageFilter.GaussianBlur(6))
        peso = valido.filter(ImageFilter.GaussianBlur(6))
        # Normaliza: color medio de los vecinos válidos.
        px_b, px_p, px_a = borroso.load(), peso.load(), actual.load()
        px_v = valido.load()
        nuevo_valido = valido.copy()
        px_nv = nuevo_valido.load()
        w, h = im.size
        for y in range(0, h):
            for x in range(0, w):
                if px_v[x, y] == 0 and px_p[x, y] > 20:
                    f = 255.0 / px_p[x, y]
                    r, g, b = px_b[x, y]
                    px_a[x, y] = (min(255, int(r * f)), min(255, int(g * f)), min(255, int(b * f)))
                    px_nv[x, y] = 255
        valido = nuevo_valido
        if valido.getextrema()[0] == 255:
            break
    return actual


def mapa_termico(gris):
    """Paleta tipo cámara térmica (negro, violeta, rojo, naranja, amarillo, blanco)."""
    paradas = [(0, (0, 0, 4)), (0.25, (60, 9, 101)), (0.5, (188, 55, 84)), (0.75, (249, 142, 9)), (0.92, (252, 230, 120)),
               (1.0, (255, 255, 255))]
    tabla = []
    for v in range(256):
        t = v / 255
        for (t0, c0), (t1, c1) in zip(paradas, paradas[1:]):
            if t0 <= t <= t1:
                k = (t - t0) / (t1 - t0)
                tabla.append(tuple(int(c0[i] + (c1[i] - c0[i]) * k) for i in range(3)))
                break
    lut = [c[0] for c in tabla] + [c[1] for c in tabla] + [c[2] for c in tabla]
    return gris.convert("L").convert("RGB").point(lut)


def guardar(im, ruta, calidad=88):
    ruta = SALIDA / ruta
    if ruta.suffix == ".jpg":
        im.convert("RGB").save(ruta, quality=calidad, optimize=True)
    else:
        im.save(ruta, optimize=True)
    print("guardado", ruta.relative_to(SALIDA), im.size)


# ---------- Marte: textura del planeta (tormenta global de 2018, MARCI) ----------
marte = abrir("PIA22329")
marte = marte.crop((0, 70, marte.width, marte.height))  # quita la fecha impresa y la franja polar dentada
marte = marte.resize((960, 480), Image.LANCZOS)
marte = rellenar_huecos(marte).filter(ImageFilter.MedianFilter(5))
# Borra el marcador azul del rover Opportunity que trae la foto: los píxeles claramente azules toman el color
# del entorno (versión muy desenfocada).
entorno = marte.filter(ImageFilter.GaussianBlur(10))
azul = Image.eval(ImageChops.subtract(marte.split()[2], marte.split()[0]), lambda v: 255 if v > 25 else 0)
marte = Image.composite(entorno, marte, azul.filter(ImageFilter.MaxFilter(9)))
marte = marte.resize((2048, 1024), Image.LANCZOS).filter(ImageFilter.GaussianBlur(1.2))
marte = ImageEnhance.Contrast(marte).enhance(1.15)
# Ya no es la superficie del planeta: queda como foto de la tormenta (fondos, evidencia de la misión) y como
# capa de polvo semitransparente encima del planeta.
polvo_gris = ImageEnhance.Contrast(marte.convert("L")).enhance(1.6)
alfa = polvo_gris.point(lambda v: max(0, min(255, int((v - 105) * 1.6))))
polvo = Image.merge("RGBA", (Image.new("L", marte.size, 236), Image.new("L", marte.size, 196),
                             Image.new("L", marte.size, 150), alfa.filter(ImageFilter.GaussianBlur(3))))
guardar(polvo, "Texturas/Marte_Polvo.png")

# Superficie del planeta: mapa global de Solar System Scope (CC BY 4.0, basado en datos de NASA), opcional.
mapa_global = ORIGEN / "8k_mars.jpg"
if mapa_global.exists():
    sup = Image.open(mapa_global).convert("RGB").resize((4096, 2048), Image.LANCZOS)
    sup = ImageEnhance.Color(sup).enhance(0.92)
    guardar(sup, "Texturas/Marte_Superficie.jpg", 90)
    # Relieve aproximado a partir del brillo (detalle de cráteres y cañones): mapa de normales.
    alto = Image.open(mapa_global).convert("L").resize((2048, 1024), Image.LANCZOS).filter(ImageFilter.GaussianBlur(1.0))
    import array
    w, h = alto.size
    px = alto.load()
    normal = Image.new("RGB", (w, h))
    pn = normal.load()
    fuerza = 2.2
    for y in range(h):
        for x in range(w):
            dx = (px[(x + 1) % w, y] - px[(x - 1) % w, y]) / 255.0 * fuerza
            dy = (px[x, min(h - 1, y + 1)] - px[x, max(0, y - 1)]) / 255.0 * fuerza
            nx, ny, nz = -dx, dy, 1.0
            l = math.sqrt(nx * nx + ny * ny + nz * nz)
            pn[x, y] = (int((nx / l * 0.5 + 0.5) * 255), int((ny / l * 0.5 + 0.5) * 255), int((nz / l * 0.5 + 0.5) * 255))
    guardar(normal, "Texturas/Marte_Normal.png")
else:
    guardar(marte, "Texturas/Marte_Superficie.jpg")

# ---------- Fondos ----------
guardar(tratar_fondo(cubrir(abrir("PIA23513"), 1920, 1080), oscurecer=0.62), "Fondos/Fondo_Briefing.jpg")
guardar(tratar_fondo(cubrir(marte, 1920, 1080), oscurecer=0.78, desenfoque=4), "Fondos/Fondo_Resultado.jpg")

# ---------- Evidencias (una por instrumento y la de la misión) ----------
def evidencia(im, nombre, ancho=1024, alto=640):
    im = cubrir(im, ancho, alto)
    im = ImageEnhance.Contrast(im).enhance(1.1)
    guardar(im, f"Evidencias/{nombre}.jpg", 90)

termica = abrir("PIA06399").crop((575, 60, 880, 1290))  # tira nocturna de THEMIS
termica = ImageEnhance.Contrast(termica.convert("L")).enhance(1.4)
termica = mapa_termico(termica).rotate(90, expand=True)
evidencia(termica, "Evidencia_termica")
evidencia(abrir("PIA26555"), "Evidencia_radar")
evidencia(abrir("PIA19940"), "Evidencia_alta_res")
crism = abrir("PIA20763")
evidencia(crism.crop((0, crism.height // 2 + 4, crism.width, crism.height)), "Evidencia_espectrometro")
evidencia(abrir("PIA23513"), "Evidencia_atmosferico")
evidencia(abrir("PIA26329"), "Evidencia_region")
evidencia(marte.crop((300, 150, 1750, 900)), "Evidencia_mision")

# ---------- Estrellas (propias, procedurales) ----------
random.seed(7)
w, h = 2048, 1152
cielo = Image.new("RGB", (w, h), FONDO)
neb = Image.new("RGB", (w // 8, h // 8), FONDO)
pn = neb.load()
for _ in range(60):
    cx, cy, r = random.randrange(w // 8), random.randrange(h // 8), random.randrange(10, 40)
    color = random.choice([(30, 40, 70), (55, 28, 40), (25, 45, 60)])
    for y in range(max(0, cy - r), min(h // 8, cy + r)):
        for x in range(max(0, cx - r), min(w // 8, cx + r)):
            k = max(0.0, 1 - math.hypot(x - cx, y - cy) / r) * 0.18
            c = pn[x, y]
            pn[x, y] = tuple(min(255, int(c[i] + color[i] * k)) for i in range(3))
cielo = neb.resize((w, h), Image.BICUBIC).filter(ImageFilter.GaussianBlur(18))
pc = cielo.load()
for _ in range(5200):
    x, y = random.randrange(w), random.randrange(h)
    brillo = random.random() ** 3
    tinte = random.choice([(1, 1, 1), (0.8, 0.9, 1), (1, 0.9, 0.8)])
    v = int(70 + 185 * brillo)
    pc[x, y] = tuple(min(255, int(v * t)) for t in tinte)
    if brillo > 0.55:  # estrellas grandes con halo en cruz
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if 0 <= x + dx < w and 0 <= y + dy < h:
                c = pc[x + dx, y + dy]
                pc[x + dx, y + dy] = tuple(min(255, c[i] + v // 3) for i in range(3))
guardar(cielo, "Texturas/Estrellas.png")

# ---------- Texturas de la nave ----------
# Lámina aislante dorada (MLI): arrugas suaves.
lam = Image.new("L", (512, 512), 128)
pl = lam.load()
for _ in range(900):
    x0, y0 = random.randrange(512), random.randrange(512)
    largo, ang = random.randrange(20, 90), random.random() * math.pi
    tono = random.randrange(70, 200)
    for t in range(largo):
        x = int(x0 + math.cos(ang) * t) % 512
        y = int(y0 + math.sin(ang) * t) % 512
        pl[x, y] = tono
lam = lam.filter(ImageFilter.GaussianBlur(2.2))
lam = ImageEnhance.Contrast(lam).enhance(1.6)
dorado = Image.merge("RGB", [lam.point(lambda v: int(150 + v * 0.42)), lam.point(lambda v: int(105 + v * 0.36)),
                             lam.point(lambda v: int(35 + v * 0.18))])
guardar(dorado, "Texturas/Lamina_Dorada.png")

# Celdas solares: rejilla azul oscura con líneas plateadas.
celdas = Image.new("RGB", (512, 512), (16, 28, 64))
dc = ImageDraw.Draw(celdas)
paso = 64
for i in range(0, 513, paso):
    dc.line((i, 0, i, 512), fill=(150, 160, 175), width=3)
    dc.line((0, i, 512, i), fill=(150, 160, 175), width=3)
for y in range(0, 512, paso):
    for x in range(0, 512, paso):
        dc.rectangle((x + 4, y + 4, x + paso - 4, y + paso - 4), fill=(20 + random.randrange(8), 36 + random.randrange(10), 82 + random.randrange(14)))
        dc.line((x + paso // 2, y + 4, x + paso // 2, y + paso - 4), fill=(40, 58, 110), width=1)
guardar(celdas, "Texturas/Celdas_Solares.png")

# ---------- Interfaz ----------
def chaflan(nombre, relleno, borde=None, lado=64, corte=14, grosor=2):
    """Sprite 9-slice con las esquinas superior izquierda e inferior derecha cortadas."""
    im = Image.new("RGBA", (lado * 4, lado * 4), (0, 0, 0, 0))  # x4 para suavizar
    d = ImageDraw.Draw(im)
    L, c = lado * 4, corte * 4
    poli = [(c, 0), (L, 0), (L, L - c), (L - c, L), (0, L), (0, c)]
    if relleno:
        d.polygon(poli, fill=(255, 255, 255, 255))
    if borde:
        g = grosor * 4
        d.line(poli + [poli[0]], fill=(255, 255, 255, 255), width=g, joint="curve")
    im = im.resize((lado, lado), Image.LANCZOS)
    guardar(im, f"Interfaz/{nombre}.png")

chaflan("Chaflan_Relleno", True)
chaflan("Chaflan_Borde", False, borde=True)

# Círculo suave para la luz del semáforo y su halo.
for nombre, duro in (("Circulo", True), ("Halo", False)):
    L = 256
    im = Image.new("L", (L, L), 0)
    d = ImageDraw.Draw(im)
    if duro:
        d.ellipse((8, 8, L - 8, L - 8), fill=255)
        im = im.filter(ImageFilter.GaussianBlur(1))
    else:
        d.ellipse((40, 40, L - 40, L - 40), fill=255)
        im = im.filter(ImageFilter.GaussianBlur(30))
    rgba = Image.merge("RGBA", [Image.new("L", (L, L), 255)] * 3 + [im])
    guardar(rgba, f"Interfaz/{nombre}.png")

# Segmentos para las barras de recursos (se repite 10 veces a lo ancho).
seg = Image.new("RGBA", (32, 8), (0, 0, 0, 0))
ImageDraw.Draw(seg).rectangle((28, 0, 31, 7), fill=(11, 11, 16, 255))
guardar(seg, "Interfaz/Segmentos.png")

# Líneas de barrido muy tenues para los fondos.
barrido = Image.new("RGBA", (4, 4), (0, 0, 0, 0))
ImageDraw.Draw(barrido).line((0, 0, 3, 0), fill=(0, 0, 0, 70))
guardar(barrido, "Interfaz/Barrido.png")
