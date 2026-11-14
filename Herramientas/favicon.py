# Favicon de Operación Eclipse Rojo: Marte rojizo con la sombra de un eclipse. Recurso propio.
import sys
from PIL import Image, ImageDraw, ImageFilter
L = 256                                   # se dibuja grande y se reduce para suavizar bordes
img = Image.new("RGBA", (L, L), (0, 0, 0, 0))
d = ImageDraw.Draw(img)
d.ellipse((8, 8, L - 8, L - 8), fill=(11, 11, 16, 255))                       # fondo del juego
for i in range(60, 0, -1):                                                     # disco de Marte con degradado
    t = i / 60
    color = (int(140 + 86 * t), int(45 + 55 * t), int(20 + 38 * t), 255)
    m = 40 + (60 - i) * 1.4
    d.ellipse((m, m, L - m, L - m), fill=color)
sombra = Image.new("L", (L, L), 0)
ImageDraw.Draw(sombra).ellipse((100, 54, L + 40, L - 30), fill=210)           # eclipse que tapa el borde derecho
sombra = sombra.filter(ImageFilter.GaussianBlur(10))
mascara = Image.new("L", (L, L), 0)
ImageDraw.Draw(mascara).ellipse((40, 40, L - 40, L - 40), fill=255)
oscuro = Image.new("RGBA", (L, L), (11, 11, 16, 255))
img.paste(oscuro, (0, 0), Image.composite(sombra, Image.new("L", (L, L), 0), mascara))
d = ImageDraw.Draw(img)
d.arc((38, 38, L - 38, L - 38), start=300, end=60, fill=(226, 100, 58, 255), width=6)   # filo iluminado
img.resize((64, 64), Image.LANCZOS).save(sys.argv[1])
print("ok", sys.argv[1])
