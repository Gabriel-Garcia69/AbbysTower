"""
Texturas pixel-art del Diorama HD-2D (estilo Octopath): paletas cortas, ruido por bloques y tramado.
Uso:  python Tools/texgen/pixel_diorama.py   (desde la raíz del proyecto)
Salida: Assets/Textures/Diorama/pix_*.png  (Unity las importa con filtro Point: DioramaBuilder lo ajusta).
"""
import math
import os
import random

from PIL import Image

OUT = os.path.join("Assets", "Textures", "Diorama")

GRASS = [(38, 52, 30), (52, 70, 36), (68, 88, 42), (86, 106, 50), (108, 122, 60), (132, 140, 72)]
MOSS = [(30, 42, 26), (44, 60, 32), (60, 80, 38), (82, 100, 46)]
ROCK = [(44, 40, 38), (60, 54, 48), (78, 70, 60), (98, 88, 74), (120, 108, 90), (142, 130, 108)]
DIRT = [(58, 46, 36), (76, 62, 46), (96, 80, 58), (118, 100, 72), (140, 122, 90)]
LEAF = [(26, 40, 26), (38, 58, 32), (54, 78, 38), (72, 98, 44), (96, 120, 54), (124, 140, 66)]
PINE = [(22, 38, 30), (32, 54, 40), (44, 70, 48), (58, 88, 56), (78, 106, 64)]
BARK = [(40, 30, 26), (56, 42, 34), (74, 56, 42), (94, 72, 52)]


def value_noise(w, h, cell, seed):
    """Ruido por bloques que se repite sin costura (tileable)."""
    rnd = random.Random(seed)
    gw, gh = max(1, w // cell), max(1, h // cell)
    grid = [[rnd.random() for _ in range(gw)] for _ in range(gh)]

    def at(x, y):
        fx, fy = x / cell, y / cell
        x0, y0 = int(fx) % gw, int(fy) % gh
        x1, y1 = (x0 + 1) % gw, (y0 + 1) % gh
        tx, ty = fx - int(fx), fy - int(fy)
        tx, ty = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
        a = grid[y0][x0] * (1 - tx) + grid[y0][x1] * tx
        b = grid[y1][x0] * (1 - tx) + grid[y1][x1] * tx
        return a * (1 - ty) + b * ty

    return at


BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def pick(pal, v, x, y):
    """v en [0,1] -> color de la paleta con tramado ordenado (se ve pixel-art)."""
    v = min(max(v, 0.0), 0.999)
    f = v * (len(pal) - 1)
    i = int(f)
    frac = f - i
    if frac > (BAYER[y % 4][x % 4] + 0.5) / 16 and i + 1 < len(pal):
        i += 1
    return pal[i]


def fbm(w, h, seed, cells=(16, 8, 4)):
    layers = [value_noise(w, h, c, seed + k * 31) for k, c in enumerate(cells)]
    weights = [0.55, 0.3, 0.15][: len(layers)]
    return lambda x, y: sum(l(x, y) * wt for l, wt in zip(layers, weights))


def grass(size=64, seed=1):
    img = Image.new("RGB", (size, size))
    n = fbm(size, size, seed)
    rnd = random.Random(seed)
    px = img.load()
    for y in range(size):
        for x in range(size):
            px[x, y] = pick(GRASS, n(x, y) * 0.9, x, y)
    # briznas: pares de pixeles claros con sombra abajo
    for _ in range(size * size // 14):
        x, y = rnd.randrange(size), rnd.randrange(size)
        c = GRASS[rnd.randint(3, 5)]
        px[x, y] = c
        px[x, (y + 1) % size] = GRASS[min(5, GRASS.index(c) + 0)]
        px[x, (y - 1) % size] = GRASS[1]
    # florecitas sueltas
    for _ in range(6):
        x, y = rnd.randrange(size), rnd.randrange(size)
        px[x, y] = rnd.choice([(196, 186, 120), (170, 150, 190), (210, 200, 180)])
    return img


def rock(w=64, h=128, seed=2):
    """Pared de roca en estratos; las filas de arriba llevan musgo que gotea (la UV pone la orilla arriba)."""
    img = Image.new("RGB", (w, h))
    px = img.load()
    rnd = random.Random(seed)
    n = fbm(w, h, seed, (16, 8, 4))
    # estratos: bandas horizontales de alto variable con bordes ondulados
    bands, y = [], 0
    while y < h:
        bh = rnd.randint(9, 18)
        bands.append((y, bh, rnd.random()))
        y += bh
    wave = value_noise(w, h, 8, seed + 7)
    for y in range(h):
        for x in range(w):
            yy = y + int((wave(x, 0) - 0.5) * 4)
            band = next((b for b in bands if b[0] <= yy % h < b[0] + b[1]), bands[-1])
            local = (yy % h - band[0]) / band[1]
            v = 0.35 + band[2] * 0.3 + (n(x, y) - 0.5) * 0.5
            if local < 0.12:
                v += 0.22          # luz en el canto de cada estrato
            elif local > 0.85:
                v -= 0.28          # sombra debajo
            px[x, y] = pick(ROCK, v, x, y)
    # grietas verticales
    for _ in range(7):
        x = rnd.randrange(w)
        y0 = rnd.randrange(h)
        for k in range(rnd.randint(6, 20)):
            yy = (y0 + k) % h
            px[x % w, yy] = ROCK[0]
            if rnd.random() < 0.3:
                x += rnd.choice([-1, 1])
    # musgo arriba: la fila 0 de la imagen es el borde de arriba (v = 1 en Unity), donde DioramaBuilder pone la orilla
    for x in range(w):
        depth = 5 + int(value_noise(w, h, 6, seed + 3)(x, 0) * 8)
        if rnd.random() < 0.18:
            depth += rnd.randint(4, 12)   # gotas de musgo que cuelgan
        for y in range(depth):
            v = 0.75 - y / max(1, depth) * 0.6
            px[x, y] = pick(MOSS, v, x, y)
    return img


def dirt(size=64, seed=3):
    img = Image.new("RGB", (size, size))
    px = img.load()
    n = fbm(size, size, seed, (8, 4, 2))
    rnd = random.Random(seed)
    for y in range(size):
        for x in range(size):
            px[x, y] = pick(DIRT, 0.25 + n(x, y) * 0.55, x, y)
    # piedritas
    for _ in range(40):
        cx, cy, r = rnd.randrange(size), rnd.randrange(size), rnd.randint(1, 3)
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                if dx * dx + dy * dy <= r * r:
                    v = 0.7 if dy < 0 else 0.5
                    px[(cx + dx) % size, (cy + dy) % size] = pick(ROCK, v + 0.2, cx + dx, cy + dy)
        px[cx % size, (cy + r + 1) % size] = DIRT[0]
    return img


def leaves(size=64, seed=4, pal=LEAF):
    """Racimos de hojas: círculos con luz arriba a la izquierda y sombra abajo."""
    img = Image.new("RGB", (size, size), pal[1])
    px = img.load()
    rnd = random.Random(seed)
    for _ in range(90):
        cx, cy, r = rnd.randrange(size), rnd.randrange(size), rnd.randint(3, 6)
        base = rnd.uniform(0.35, 0.75)
        for dy in range(-r, r + 1):
            for dx in range(-r, r + 1):
                d = math.sqrt(dx * dx + dy * dy)
                if d > r:
                    continue
                v = base + (-dx - dy) / (2.5 * r) * 0.35 - (d / r) * 0.15
                x, y = (cx + dx) % size, (cy + dy) % size
                px[x, y] = pick(pal, v, x, y)
        px[cx % size, (cy + r) % size] = pal[0]
    return img


def pine(size=64, seed=5):
    """Agujas de pino: trazos diagonales hacia abajo, en capas."""
    img = Image.new("RGB", (size, size), PINE[1])
    px = img.load()
    rnd = random.Random(seed)
    for _ in range(260):
        x, y = rnd.randrange(size), rnd.randrange(size)
        v = rnd.uniform(0.2, 0.95)
        d = rnd.choice([-1, 1])
        for k in range(rnd.randint(3, 6)):
            xx, yy = (x + d * k // 2) % size, (y + k) % size
            px[xx, yy] = pick(PINE, v - k * 0.06, xx, yy)
    return img


def bark(w=32, h=64, seed=6):
    img = Image.new("RGB", (w, h))
    px = img.load()
    n = value_noise(w, h, 4, seed)
    for y in range(h):
        for x in range(w):
            stripe = 0.5 + 0.5 * math.sin(x * 1.3 + n(x, y) * 4)
            px[x, y] = pick(BARK, stripe * 0.8 + (n(x, y) - 0.5) * 0.3, x, y)
    return img


SAND = [(150, 112, 70), (180, 140, 90), (206, 168, 112), (226, 192, 136), (240, 214, 162)]
SNOW = [(150, 166, 196), (180, 196, 222), (206, 220, 238), (228, 236, 248), (246, 250, 255)]
SANDSTONE = [(92, 62, 38), (122, 84, 50), (156, 112, 68), (186, 140, 88), (210, 168, 112)]
ICE = [(70, 120, 160), (100, 156, 194), (140, 190, 220), (180, 218, 240), (222, 242, 252)]
CAVE = [(38, 44, 66), (54, 64, 92), (74, 88, 120), (98, 114, 148), (128, 144, 176)]


def sand(size=64, seed=11):
    """Arena con ondas de viento."""
    img = Image.new("RGB", (size, size))
    px = img.load()
    n = fbm(size, size, seed, (16, 8, 4))
    for y in range(size):
        for x in range(size):
            ripple = 0.5 + 0.5 * math.sin((x * 0.35 + y * 0.9) + n(x, y) * 5)
            px[x, y] = pick(SAND, 0.3 + n(x, y) * 0.4 + ripple * 0.25, x, y)
    rnd = random.Random(seed)
    for _ in range(30):
        x, y = rnd.randrange(size), rnd.randrange(size)
        px[x, y] = SAND[4]
    return img


def snow(size=64, seed=12):
    img = Image.new("RGB", (size, size))
    px = img.load()
    n = fbm(size, size, seed, (16, 8, 4))
    rnd = random.Random(seed)
    for y in range(size):
        for x in range(size):
            px[x, y] = pick(SNOW, 0.45 + n(x, y) * 0.5, x, y)
    for _ in range(50):   # destellos de hielo
        x, y = rnd.randrange(size), rnd.randrange(size)
        px[x, y] = (255, 255, 255)
    return img


def bricks(pal, w=64, h=64, seed=13, rows=8, cols=4):
    """Sillares escalonados con junta oscura, luz arriba y sombra abajo (estilo Octopath)."""
    img = Image.new("RGB", (w, h))
    px = img.load()
    n = fbm(w, h, seed, (8, 4, 2))
    rh, cw = h // rows, w // cols
    rnd = random.Random(seed)
    tone = {}
    for y in range(h):
        row = y // rh
        for x in range(w):
            xo = (x + (row % 2) * cw // 2) % w
            col = xo // cw
            key = (row, col)
            if key not in tone:
                tone[key] = rnd.uniform(-0.12, 0.12)
            u, v = xo % cw, y % rh
            if u == 0 or v == 0:
                px[x, y] = pal[0]
                continue
            val = 0.55 + tone[key] + (n(x, y) - 0.5) * 0.35
            if v == 1:
                val += 0.25
            elif v == rh - 1 or u == cw - 1:
                val -= 0.25
            px[x, y] = pick(pal, val, x, y)
    return img


def ray(w=64, h=256):
    """Rayo de luz (god ray): blanco, suave a los lados y que se desvanece hacia abajo. Se usa aditivo."""
    img = Image.new("RGBA", (w, h))
    px = img.load()
    for y in range(h):
        for x in range(w):
            u = (x + 0.5) / w * 2 - 1
            v = y / (h - 1)
            a = math.exp(-u * u * 4.5) * (1 - v) ** 1.4 * min(1.0, v * 6 + 0.2)
            px[x, y] = (255, 255, 255, int(255 * a))
    return img


def main():
    os.makedirs(OUT, exist_ok=True)
    grass().save(os.path.join(OUT, "pix_grass.png"))
    rock().save(os.path.join(OUT, "pix_rock.png"))
    dirt().save(os.path.join(OUT, "pix_dirt.png"))
    leaves().save(os.path.join(OUT, "pix_leaves.png"))
    leaves(seed=9, pal=[(40, 34, 22), (62, 54, 30), (86, 76, 38), (112, 98, 46), (140, 120, 56), (168, 146, 70)]).save(os.path.join(OUT, "pix_leaves_autumn.png"))
    pine().save(os.path.join(OUT, "pix_pine.png"))
    bark().save(os.path.join(OUT, "pix_bark.png"))
    ray().save(os.path.join(OUT, "rayo.png"))
    sand().save(os.path.join(OUT, "pix_sand.png"))
    snow().save(os.path.join(OUT, "pix_snow.png"))
    bricks(SANDSTONE).save(os.path.join(OUT, "pix_sandstone.png"))
    bricks(CAVE, seed=21, rows=6, cols=3).save(os.path.join(OUT, "pix_cavebrick.png"))
    bricks([(40, 40, 46), (62, 62, 70), (88, 88, 98), (118, 118, 128), (150, 150, 160)], seed=31).save(os.path.join(OUT, "pix_stonebrick.png"))
    img = Image.new("RGB", (64, 64)); n = fbm(64, 64, 14, (16, 8)); px = img.load()
    for y in range(64):
        for x in range(64):
            px[x, y] = pick(ICE, 0.45 + n(x, y) * 0.45 + (0.25 if (x + y * 2) % 23 == 0 else 0), x, y)
    img.save(os.path.join(OUT, "pix_ice.png"))
    img = Image.new("RGB", (64, 128)); n = fbm(64, 128, 15, (16, 8, 4)); px = img.load()
    for y in range(128):
        for x in range(64):
            px[x, y] = pick(CAVE, 0.3 + n(x, y) * 0.55 + (0.2 if y % 14 < 2 else 0), x, y)
    img.save(os.path.join(OUT, "pix_caverock.png"))
    print("ok ->", OUT)


if __name__ == "__main__":
    main()
