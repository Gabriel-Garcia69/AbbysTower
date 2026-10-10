"""Generador de texturas realistas y sin costura para Abby's Tower.
Cada textura produce: <name>_albedo.png (RGB), <name>_normal.png, <name>_mask.png (R metálico, A suavidad)
y opcionalmente <name>_emission.png. Todo periódico (se repite sin costuras).
Uso: python -I texgen.py <carpeta_salida>
"""
import sys, os
import numpy as np
from PIL import Image

N = 512
OUT = sys.argv[1]
os.makedirs(OUT, exist_ok=True)
yy, xx = np.mgrid[0:N, 0:N].astype(np.float32)
U, V = xx / N, yy / N  # 0..1


def rng(seed):
    return np.random.default_rng(seed)


def fbm(seed, beta=2.0, n=N, aniso=(1.0, 1.0)):
    """Ruido fractal periódico por FFT (espectro 1/f^beta). aniso estira en x/y. Devuelve 0..1."""
    r = rng(seed)
    white = r.standard_normal((n, n))
    F = np.fft.fft2(white)
    fy = np.fft.fftfreq(n)[:, None] * aniso[1]
    fx = np.fft.fftfreq(n)[None, :] * aniso[0]
    f = np.sqrt(fx * fx + fy * fy)
    f[0, 0] = 1.0
    F = F / f ** (beta / 2.0)
    F[0, 0] = 0
    out = np.real(np.fft.ifft2(F))
    out = (out - out.min()) / (out.max() - out.min() + 1e-9)
    return out.astype(np.float32)


def voronoi(seed, count, jitter=1.0):
    """Celdas de Voronoi periódicas: devuelve (f1, f2, id) con distancias en unidades de textura."""
    r = rng(seed)
    pts = r.random((count, 2)).astype(np.float32)
    f1 = np.full((N, N), 9.0, np.float32); f2 = np.full((N, N), 9.0, np.float32); ids = np.zeros((N, N), np.int32)
    for i, (px, py) in enumerate(pts):
        dx = np.abs(U - px); dx = np.minimum(dx, 1 - dx)
        dy = np.abs(V - py); dy = np.minimum(dy, 1 - dy)
        d = np.sqrt(dx * dx + dy * dy)
        closer = d < f1
        f2 = np.where(closer, f1, np.minimum(f2, d))
        ids = np.where(closer, i, ids)
        f1 = np.where(closer, d, f1)
    return f1, f2, ids


def rand_per_id(ids, seed, lo=0.0, hi=1.0):
    r = rng(seed)
    table = r.uniform(lo, hi, ids.max() + 2).astype(np.float32)
    return table[ids]


def bricks(rows, cols, mortar, offset=0.5, seed=1):
    """Ladrillos en hilera trabada. Devuelve (máscara ladrillo suave 0..1, id por ladrillo, distancia al borde)."""
    ry = V * rows
    row = np.floor(ry).astype(np.int32)
    rx = U * cols + (row % 2) * offset
    col = np.floor(rx).astype(np.int32) % cols
    fx = rx - np.floor(rx); fy = ry - np.floor(ry)
    # distancia al borde en unidades de textura (aprox)
    ex = np.minimum(fx, 1 - fx) / cols
    ey = np.minimum(fy, 1 - fy) / rows
    edge = np.minimum(ex, ey)
    mask = np.clip((edge - mortar) / (mortar * 0.8 + 1e-6), 0, 1)
    ids = row * 97 + col
    return mask, ids, edge


def lerp(a, b, t):
    t = np.asarray(t)[..., None] if np.ndim(t) == 2 else t
    return a + (b - a) * t


def col(*c):
    return np.array(c, np.float32) / 255.0


def normal_from_height(h, strength):
    gx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5
    gy = (np.roll(h, 1, 0) - np.roll(h, -1, 0)) * 0.5  # +y = arriba en la textura
    nx, ny, nz = -gx * strength, -gy * strength, np.ones_like(h)
    l = np.sqrt(nx * nx + ny * ny + nz * nz)
    n = np.stack([nx / l, ny / l, nz / l], -1)
    return ((n * 0.5 + 0.5) * 255).astype(np.uint8)


def save(name, albedo, height, smooth, metal=None, emission=None, nstrength=6.0):
    a = np.clip(albedo, 0, 1)
    Image.fromarray((a * 255).astype(np.uint8), "RGB").save(f"{OUT}/{name}_albedo.png")
    Image.fromarray(normal_from_height(height.astype(np.float32), nstrength * N / 64.0), "RGB").save(f"{OUT}/{name}_normal.png")
    m = np.zeros((N, N, 4), np.float32)
    m[..., 0] = 0 if metal is None else metal
    m[..., 3] = smooth
    Image.fromarray((np.clip(m, 0, 1) * 255).astype(np.uint8), "RGBA").save(f"{OUT}/{name}_mask.png")
    if emission is not None:
        Image.fromarray((np.clip(emission, 0, 1) * 255).astype(np.uint8), "RGB").save(f"{OUT}/{name}_emission.png")
    print("ok", name)


def grime(seed, amount=0.25):
    g = fbm(seed, 2.2)
    return 1 - amount * np.clip((g - 0.45) * 2.5, 0, 1)


# ------------------------------------------------------------------ BASE (pisos 1 y 2): losas y sillares azul pizarra

def base_floor():
    f1, f2, ids = voronoi(11, 18)
    edge = f2 - f1
    gap = np.clip((edge - 0.006) / 0.01, 0, 1)
    tone = rand_per_id(ids, 12, 0.75, 1.05)
    n = fbm(13, 2.3); fine = fbm(14, 1.2)
    stone = lerp(col(70, 78, 92), col(118, 126, 142), n)
    alb = stone * tone[..., None] * (0.85 + 0.15 * fine[..., None])
    alb = lerp(col(28, 30, 36), alb, gap) * grime(15, 0.3)[..., None]
    h = gap * (0.6 + 0.4 * np.clip(edge * 25, 0, 1)) + n * 0.25 + fine * 0.08
    save("base_floor", alb, h, 0.18 + 0.25 * gap * fine)


def base_wall():
    m, ids, e = bricks(8, 4, 0.006, seed=2)
    tone = rand_per_id(ids, 21, 0.78, 1.08)
    n = fbm(22, 2.2); fine = fbm(23, 1.3)
    stone = lerp(col(60, 68, 96), col(102, 110, 140), n) * tone[..., None]
    alb = lerp(col(30, 32, 40), stone * (0.88 + 0.12 * fine[..., None]), m)
    alb *= grime(24, 0.35)[..., None]
    h = m * (0.55 + 0.45 * np.clip(e * 60, 0, 1)) + n * 0.2 + fine * 0.1
    save("base_wall", alb, h, 0.1 + 0.15 * m)


# ------------------------------------------------------------------ PEREZA: madera vieja polvorienta y libreros

def wood_planks(name, seed, c0, c1, planks=6, dust=0.0, smooth=0.3):
    row = np.floor(V * planks).astype(np.int32)
    shift = rand_per_id(row, seed, 0, 1)
    length = 1.0
    seam_x = (U + shift) % length
    pid = row * 31 + np.floor((U + shift) / length).astype(np.int32)
    grain = fbm(seed + 1, 2.0, aniso=(0.08, 1.0))  # vetas largas en x
    rings = 0.5 + 0.5 * np.sin((V * planks * 6 + grain * 8) * np.pi)
    tone = rand_per_id(pid, seed + 2, 0.75, 1.1)
    alb = lerp(c0, c1, grain * 0.6 + rings * 0.4) * tone[..., None]
    fy = V * planks - row
    seam = np.clip(np.minimum(fy, 1 - fy) * planks * 60, 0, 1) * np.clip(np.minimum(seam_x, length - seam_x) * 300, 0, 1)
    alb = lerp(alb * 0.35, alb, seam)
    if dust:
        d = np.clip((fbm(seed + 3, 2.5) - 0.4) * 2.2, 0, 1) * dust
        alb = lerp(alb, col(150, 140, 125), d)
    h = seam * 0.8 + grain * 0.15 + rings * 0.05
    save(name, alb, h, smooth * seam * (1 - 0.6 * (dust or 0)))


def pereza_wall():
    # librero: estantes cada 1/4 de alto, lomos de libros de colores apagados
    shelves = 4
    sy = V * shelves
    srow = np.floor(sy).astype(np.int32)
    fy = sy - srow
    board = fy > 0.86
    r = rng(31)
    alb = np.zeros((N, N, 3), np.float32); h = np.zeros((N, N), np.float32); sm = np.zeros((N, N), np.float32)
    palette = [col(96, 40, 38), col(48, 70, 58), col(52, 58, 92), col(120, 96, 52), col(84, 60, 44), col(60, 40, 70), col(140, 120, 90), col(40, 50, 44)]
    back = col(28, 20, 16)
    alb[:] = back
    for s in range(shelves):
        x = 0.0
        y0, y1 = s / shelves, (s + 0.86) / shelves
        while x < 1.0:
            w = r.uniform(0.012, 0.03)
            hh = r.uniform(0.62, 0.97)
            lean = 0
            c = palette[r.integers(len(palette))] * r.uniform(0.7, 1.15)
            top = y1 - (y1 - y0) * hh
            x1 = min(1.0, x + w)
            sel = (U >= x) & (U < x1) & (V >= top) & (V < y1)
            # banda dorada en el lomo
            band = sel & (np.abs((V - top) / (y1 - top) - r.uniform(0.15, 0.3)) < 0.025)
            shade = 0.75 + 0.25 * np.sin((U - x) / w * np.pi)
            alb[sel] = (c[None, :] * shade[sel][:, None])
            alb[band] = col(190, 160, 90)
            h[sel] = 0.6 + 0.3 * shade[sel]
            sm[sel] = 0.25
            x = x1 + (r.uniform(0, 0.01) if r.random() < 0.15 else 0)
    grain = fbm(32, 2.0, aniso=(0.1, 1.0))
    wood = lerp(col(70, 46, 30), col(110, 78, 50), grain)
    alb[board] = wood[board]
    h[board] = 1.0
    sm[board] = 0.3
    dust = np.clip((fbm(33, 2.4) - 0.5) * 2, 0, 1) * 0.35
    alb = lerp(alb, col(140, 130, 115), dust)
    alb *= grime(34, 0.2)[..., None]
    save("pereza_wall", alb, h, sm, nstrength=4)


# ------------------------------------------------------------------ GULA: azulejo ajedrezado gastado y ladrillo con hollín

def gula_floor():
    t = 8
    cx = np.floor(U * t).astype(np.int32); cy = np.floor(V * t).astype(np.int32)
    checker = ((cx + cy) % 2).astype(np.float32)
    fx = U * t - cx; fy = V * t - cy
    grout = np.clip(np.minimum(np.minimum(fx, 1 - fx), np.minimum(fy, 1 - fy)) * t * 40, 0, 1)
    n = fbm(41, 2.2); fine = fbm(42, 1.2)
    a = lerp(col(150, 70, 44), col(176, 92, 60), n)      # terracota
    b = lerp(col(214, 196, 160), col(232, 220, 190), n)  # crema
    alb = lerp(a, b, checker) * (0.9 + 0.1 * fine[..., None])
    wear = np.clip((fbm(43, 2.0) - 0.55) * 3, 0, 1)
    alb = lerp(alb, col(120, 96, 76), wear * 0.5)
    stains = np.clip((fbm(44, 2.6) - 0.6) * 3, 0, 1)
    alb = lerp(alb, col(70, 40, 24), stains * 0.45)
    alb = lerp(col(60, 50, 44), alb, grout)
    h = grout * 0.9 + fine * 0.05
    save("gula_floor", alb, h, (0.55 - 0.4 * wear) * grout)


def gula_wall():
    m, ids, e = bricks(16, 6, 0.0035, seed=5)
    tone = rand_per_id(ids, 51, 0.7, 1.1)
    n = fbm(52, 2.2)
    brick = lerp(col(120, 52, 36), col(160, 80, 52), n) * tone[..., None]
    soot = np.clip((fbm(53, 2.0, aniso=(1, 0.3)) - 0.4) * 1.6, 0, 1) * np.clip(1.2 - V, 0, 1)
    alb = lerp(col(150, 136, 118), brick, m)
    alb = lerp(alb, col(25, 18, 14), soot * 0.7)
    h = m * (0.6 + 0.4 * np.clip(e * 120, 0, 1)) + n * 0.15
    save("gula_wall", alb, h, 0.12 * m)


# ------------------------------------------------------------------ AVARICIA: mármol verde con oro y placas de bóveda

def marble(seed, c0, c1, vein, freq=3.0, turb=4.0):
    t = fbm(seed, 2.0)
    v = np.abs(np.sin((U * freq + V * freq * 0.6 + t * turb) * np.pi))
    v = v ** 0.25
    base = lerp(c0, c1, fbm(seed + 1, 2.4))
    return lerp(vein, base, v), v, t


def avaricia_floor():
    t = 4
    cx = np.floor(U * t).astype(np.int32); cy = np.floor(V * t).astype(np.int32)
    checker = ((cx + cy) % 2).astype(np.float32)
    green, v1, _ = marble(61, col(14, 70, 52), col(30, 110, 80), col(200, 230, 210), 2.5)
    dark, v2, _ = marble(63, col(12, 22, 18), col(26, 40, 32), col(190, 160, 90), 2.0)
    alb = lerp(dark, green, checker)
    fx = U * t - cx; fy = V * t - cy
    d = np.minimum(np.minimum(fx, 1 - fx), np.minimum(fy, 1 - fy))
    gold = d < 0.035
    gold_c = lerp(col(150, 110, 40), col(240, 200, 110), fbm(64, 1.5))
    alb[gold] = gold_c[gold]
    h = np.where(gold, 1.0, 0.6) + (1 - v1) * 0.03
    metal = gold.astype(np.float32)
    save("avaricia_floor", alb, h, np.where(gold, 0.75, 0.82), metal=metal, nstrength=3)


def avaricia_wall():
    rows, cols = 3, 2
    ry, rx = V * rows, U * cols
    fy, fx = ry - np.floor(ry), rx - np.floor(rx)
    d = np.minimum(np.minimum(fx, 1 - fx) / rows * cols, np.minimum(fy, 1 - fy))
    plate = np.clip((d - 0.01) * 80, 0, 1)
    n = fbm(71, 2.2); scratch = fbm(72, 1.0, aniso=(1, 0.05))
    bronze = lerp(col(110, 76, 30), col(190, 150, 70), n * 0.7 + scratch * 0.3)
    alb = lerp(col(40, 28, 12), bronze, plate)
    # remaches cerca del borde
    rv = np.zeros((N, N), np.float32)
    for i in range(cols):
        for j in range(rows):
            for (ax, ay) in [(0.08, 0.1), (0.92, 0.1), (0.08, 0.9), (0.92, 0.9), (0.5, 0.1), (0.5, 0.9)]:
                px, py = (i + ax) / cols, (j + ay) / rows
                dd = np.sqrt(((U - px) * cols * 0.5) ** 2 + ((V - py) * rows / 3 * 1.5) ** 2)
                rv = np.maximum(rv, np.clip(1 - dd / 0.025, 0, 1))
    alb = lerp(alb, col(220, 190, 120), np.clip(rv * 2, 0, 1))
    # bajorrelieve: rombo central
    lx, ly = fx - 0.5, fy - 0.5
    diamond = np.abs(lx) + np.abs(ly)
    emb = np.clip(1 - np.abs(diamond - 0.3) * 30, 0, 1)
    alb = lerp(alb, col(230, 200, 120), emb * 0.6)
    h = plate * 0.6 + rv * 0.5 + emb * 0.3 + n * 0.05
    save("avaricia_wall", alb, h, 0.45 + 0.25 * plate, metal=0.85 * plate, nstrength=4)


# ------------------------------------------------------------------ ENVIDIA: malaquita y papel tapiz verde con marcos

def envidia_floor():
    f1, f2, ids = voronoi(81, 10)
    edge = f2 - f1
    t = fbm(82, 2.0)
    bands = 0.5 + 0.5 * np.sin((f1 * 60 + t * 6) * np.pi)
    alb = lerp(col(8, 60, 36), col(60, 170, 100), bands ** 1.5)
    alb *= rand_per_id(ids, 83, 0.8, 1.05)[..., None]
    gap = np.clip((edge - 0.004) / 0.006, 0, 1)
    alb = lerp(col(10, 16, 12), alb, gap)
    h = gap * 0.7 + bands * 0.05
    save("envidia_floor", alb, h, 0.75 * gap, nstrength=3)


def envidia_wall():
    cols = 2
    fx = U * cols - np.floor(U * cols)
    frame = (np.abs(fx - 0.5) > 0.42) | (np.abs(V - 0.5) > 0.44)
    inner_frame = ((np.abs(fx - 0.5) > 0.38) & (np.abs(fx - 0.5) < 0.40)) | ((np.abs(V - 0.5) > 0.40) & (np.abs(V - 0.5) < 0.42))
    # damasco: patrón de rombos con flores por senos
    px, py = (U * 8) % 1 - 0.5, (V * 6) % 1 - 0.5
    motif = np.clip(1 - np.abs(np.abs(px) + np.abs(py) - 0.3) * 8, 0, 1) + np.clip(1 - np.sqrt(px * px + py * py) * 9, 0, 1)
    paper = lerp(col(20, 54, 34), col(34, 84, 52), fbm(91, 2.3))
    paper = lerp(paper, col(70, 130, 80), np.clip(motif, 0, 1) * 0.5)
    wood = lerp(col(26, 34, 22), col(52, 64, 40), fbm(92, 2.0, aniso=(0.1, 1)))
    alb = np.where(frame[..., None], wood, paper)
    alb = np.where(inner_frame[..., None], col(170, 160, 90), alb)
    h = np.where(frame, 1.0, 0.4) + np.clip(motif, 0, 1) * 0.08 + inner_frame * 0.3
    save("envidia_wall", alb, h, np.where(frame, 0.45, 0.15), metal=inner_frame.astype(np.float32) * 0.8, nstrength=3)


# ------------------------------------------------------------------ LUJURIA: mármol rosa y terciopelo carmesí con damasco

def lujuria_floor():
    t = 2
    cx = np.floor(U * t).astype(np.int32); cy = np.floor(V * t).astype(np.int32)
    checker = ((cx + cy) % 2).astype(np.float32)
    pink, v, _ = marble(101, col(220, 170, 180), col(244, 214, 220), col(150, 70, 100), 2.0, 5.0)
    white, v2, _ = marble(103, col(236, 226, 228), col(250, 246, 246), col(170, 120, 140), 1.5, 4.0)
    alb = lerp(pink, white, checker)
    fx = U * t - cx; fy = V * t - cy
    d = np.minimum(np.minimum(fx, 1 - fx), np.minimum(fy, 1 - fy))
    seam = np.clip(d * 600, 0, 1)
    alb = lerp(col(120, 80, 90), alb, seam)
    save("lujuria_floor", alb, seam * 0.8 + (1 - v) * 0.02, 0.9 * seam, nstrength=2)


def lujuria_wall():
    px, py = (U * 4) % 1 - 0.5, (V * 3) % 1 - 0.5
    r_ = np.sqrt(px * px + py * py); a = np.arctan2(py, px)
    petals = np.clip(1 - np.abs(r_ - (0.22 + 0.08 * np.cos(a * 6))) * 18, 0, 1)
    heart_core = np.clip(1 - r_ * 14, 0, 1)
    lattice = np.clip(1 - np.abs(np.abs(px) + np.abs(py) - 0.48) * 25, 0, 1)
    nap = fbm(111, 1.0, aniso=(0.3, 1.0))
    velvet = lerp(col(70, 6, 24), col(130, 20, 50), nap)
    motif = np.clip(petals + heart_core + lattice, 0, 1)
    alb = lerp(velvet, col(200, 150, 90), motif * 0.55)
    h = motif * 0.4 + nap * 0.15
    save("lujuria_wall", alb, h, 0.12 + motif * 0.4, metal=motif * 0.6, nstrength=3)


# ------------------------------------------------------------------ IRA: basalto con grietas de lava y muro calcinado

def lava_cracks(seed, count, width):
    f1, f2, ids = voronoi(seed, count)
    edge = f2 - f1
    crack = np.clip(1 - edge / width, 0, 1) ** 1.5
    return crack, ids, edge


def ira_floor():
    crack, ids, edge = lava_cracks(121, 14, 0.012)
    n = fbm(122, 2.2); fine = fbm(123, 1.0)
    rock = lerp(col(22, 18, 18), col(58, 48, 46), n) * rand_per_id(ids, 124, 0.7, 1.1)[..., None]
    rock *= (0.85 + 0.15 * fine[..., None])
    hot = crack
    glow = lerp(col(255, 60, 0), col(255, 210, 80), hot ** 3)
    alb = lerp(rock, glow * 0.6, np.clip(hot * 1.5, 0, 1))
    emission = glow * (hot ** 1.2)[..., None]
    h = (1 - crack) * 0.8 + n * 0.25 + fine * 0.1
    save("ira_floor", alb, h, 0.25 * (1 - crack) + 0.1, emission=emission, nstrength=8)


def ira_wall():
    m, ids, e = bricks(6, 3, 0.008, seed=9)
    crack, _, _ = lava_cracks(131, 10, 0.006)
    n = fbm(132, 2.2)
    stone = lerp(col(30, 24, 24), col(70, 56, 52), n) * rand_per_id(ids, 133, 0.7, 1.05)[..., None]
    char = np.clip((fbm(134, 2.0) - 0.35) * 2, 0, 1)
    stone = lerp(stone, col(12, 10, 10), char * 0.7)
    mortar_glow = (1 - m) * (0.4 + 0.6 * fbm(135, 1.5))
    hot = np.clip(crack * m + mortar_glow * 0.8, 0, 1)
    glow = lerp(col(200, 30, 0), col(255, 150, 40), hot)
    alb = lerp(stone, glow * 0.5, hot * 0.8)
    emission = glow * (hot ** 2)[..., None] * 0.9
    h = m * 0.7 + n * 0.2 - crack * 0.3
    save("ira_wall", alb, h, 0.12, emission=emission, nstrength=7)


# ------------------------------------------------------------------ SOBERBIA: mármol blanco con incrustaciones de oro

def soberbia_floor():
    t = 2
    cx = np.floor(U * t).astype(np.int32); cy = np.floor(V * t).astype(np.int32)
    w, v, _ = marble(141, col(225, 222, 216), col(248, 246, 240), col(120, 116, 120), 1.6, 4.5)
    fx = U * t - cx; fy = V * t - cy
    d = np.minimum(np.minimum(fx, 1 - fx), np.minimum(fy, 1 - fy))
    gold_line = (d < 0.018)
    # estrella de 8 puntas en el centro de cada losa
    lx, ly = fx - 0.5, fy - 0.5
    r_ = np.sqrt(lx * lx + ly * ly); a = np.arctan2(ly, lx)
    star = np.abs(r_ - (0.18 + 0.06 * np.cos(a * 8))) < 0.008
    ring = np.abs(r_ - 0.3) < 0.006
    gold = gold_line | star | ring
    gc = lerp(col(170, 130, 50), col(250, 215, 120), fbm(142, 1.5))
    alb = np.where(gold[..., None], gc, w)
    h = np.where(gold, 0.9, 0.7) + (1 - v) * 0.02
    save("soberbia_floor", alb, h, np.where(gold, 0.8, 0.9), metal=gold.astype(np.float32), nstrength=2)


def soberbia_wall():
    cols = 2
    fx = U * cols - np.floor(U * cols)
    pil = np.abs(fx - 0.5) > 0.4
    flute = pil & (np.abs(np.sin(fx * cols * np.pi * 10)) < 0.25)
    w, v, _ = marble(151, col(214, 210, 204), col(240, 238, 232), col(140, 136, 138), 1.2, 4.0)
    band = (np.abs(V - 0.12) < 0.012) | (np.abs(V - 0.88) < 0.012)
    panel = (~pil) & (np.abs(fx - 0.5) < 0.33) & (np.abs(V - 0.5) < 0.34)
    panel_edge = (~pil) & (np.abs(np.maximum(np.abs(fx - 0.5) / 0.33, np.abs(V - 0.5) / 0.34) - 1) < 0.03)
    gold = band | panel_edge
    gc = lerp(col(170, 130, 50), col(250, 215, 120), fbm(152, 1.5))
    alb = np.where(gold[..., None], gc, w)
    alb = np.where(flute[..., None], alb * 0.82, alb)
    h = np.where(pil, 0.9, 0.6) - flute * 0.2 + gold * 0.15 + panel * 0.05
    save("soberbia_wall", alb, h, np.where(gold, 0.8, 0.7), metal=gold.astype(np.float32), nstrength=3)


if __name__ == "__main__" and len(sys.argv) == 2:
    base_floor(); base_wall()
    wood_planks("pereza_floor", 201, col(70, 50, 34), col(118, 88, 60), planks=6, dust=0.55, smooth=0.25)
    pereza_wall()
    gula_floor(); gula_wall()
    avaricia_floor(); avaricia_wall()
    envidia_floor(); envidia_wall()
    lujuria_floor(); lujuria_wall()
    ira_floor(); ira_wall()
    soberbia_floor(); soberbia_wall()


# ------------------------------------------------------------------ GALERÍAS (piso 2): tablones con herrajes y roca de acantilado

def galerias_floor():
    planks = 5
    row = np.floor(V * planks).astype(np.int32)
    grain = fbm(301, 2.0, aniso=(0.08, 1.0))
    tone = rand_per_id(row, 302, 0.7, 1.05)
    alb = lerp(col(64, 58, 54), col(110, 98, 86), grain) * tone[..., None]
    fy = V * planks - row
    seam = np.clip(np.minimum(fy, 1 - fy) * planks * 50, 0, 1)
    alb = lerp(alb * 0.3, alb, seam)
    # flejes de hierro cada medio tablero, con remaches
    strap = (np.abs(((U * 2) % 1) - 0.5) < 0.035)
    alb = np.where(strap[..., None], lerp(col(40, 40, 46), col(70, 70, 78), fbm(303, 1.5)), alb)
    rivet = strap & (np.abs(fy - 0.5) < 0.08)
    alb = np.where(rivet[..., None], col(120, 120, 130), alb)
    moss = np.clip((fbm(304, 2.4) - 0.62) * 3, 0, 1) * (1 - seam * 0.5)
    alb = lerp(alb, col(60, 80, 50), moss * 0.5)
    h = seam * 0.7 + grain * 0.15 + strap * 0.35 + rivet * 0.3
    save("galerias_floor", alb, h, 0.2 + 0.4 * strap, metal=strap.astype(np.float32) * 0.8, nstrength=5)


def galerias_wall():
    n = fbm(311, 2.2)
    strata = 0.5 + 0.5 * np.sin((V * 9 + fbm(312, 2.0) * 1.6) * np.pi * 2)
    rock = lerp(col(58, 54, 66), col(118, 108, 120), n * 0.6 + strata * 0.4)
    cracks, ids, _ = lava_cracks(313, 16, 0.006)
    rock = lerp(rock, col(24, 22, 28), cracks * 0.8)
    rock *= rand_per_id(ids, 314, 0.85, 1.08)[..., None]
    lichen = np.clip((fbm(315, 2.6) - 0.6) * 3, 0, 1)
    rock = lerp(rock, col(150, 160, 110), lichen * 0.35)
    h = strata * 0.4 + n * 0.4 - cracks * 0.4
    save("galerias_wall", rock, h, 0.12, nstrength=7)


if __name__ == "__main__" and len(sys.argv) > 2 and sys.argv[2] == "galerias":
    galerias_floor(); galerias_wall()


def lava_surface():
    """Superficie de lava (para material sin luz): magma brillante con placas de costra que flotan."""
    t = fbm(401, 2.0); fine = fbm(402, 1.3)
    crust_f1, crust_f2, ids = voronoi(403, 22)
    edge = crust_f2 - crust_f1
    crust = np.clip((edge - 0.012) / 0.02, 0, 1) * np.clip((fbm(404, 2.4) - 0.35) * 2.5, 0, 1)
    hot = lerp(col(255, 120, 20), col(255, 230, 120), np.clip(t * 1.2 - 0.1 + fine * 0.3, 0, 1))
    dark = lerp(col(40, 12, 6), col(90, 30, 10), fine)
    alb = lerp(hot, dark, crust * 0.92)
    Image.fromarray((np.clip(alb, 0, 1) * 255).astype(np.uint8), "RGB").save(f"{OUT}/lava_albedo.png")
    print("ok lava")


if __name__ == "__main__" and len(sys.argv) > 2 and sys.argv[2] == "lava":
    lava_surface()
