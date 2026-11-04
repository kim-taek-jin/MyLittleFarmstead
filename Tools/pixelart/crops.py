"""Crop growth stages (32x32, centred on a farm cell) and ground tiles."""
from pixel import Canvas, P, darken_c, lighten_c, seeded
import items

S = 32
GROUND = 27  # y of the soil line inside the cell


def mound(c, w=7):
    c.ellipse(16, GROUND, w, 2.5, P['dirt'])


def leaf(c, x, y, dx, dy, length, r, width=2):
    items._leaf(c, x, y, dx, dy, length, r, width)


def stem(c, x0, y0, x1, y1, r=P['leaf']):
    c.line(x0, y0, x1, y1, r[1])
    c.line(x0 + 1, y0, x1 + 1, y1, r[2])


def seedling(r=P['leaf']):
    c = Canvas(S, S)
    mound(c)
    c.set(15, GROUND - 3, r[2]); c.set(16, GROUND - 4, r[3]); c.set(17, GROUND - 3, r[2])
    c.set(16, GROUND - 3, r[1])
    c.outline()
    return c


def sprout(r=P['leaf']):
    c = Canvas(S, S)
    mound(c)
    stem(c, 15, GROUND - 2, 15, GROUND - 7, r)
    leaf(c, 15, GROUND - 7, -1, -0.5, 5, r)
    leaf(c, 16, GROUND - 7, 1, -0.6, 5, r)
    c.outline()
    return c


# ---------- carrot ----------
def carrot_2():
    c = Canvas(S, S)
    mound(c, 8)
    for dx, dy, l in [(-0.6, -1, 9), (0, -1, 11), (0.6, -1, 9), (-1, -0.5, 7), (1, -0.5, 7)]:
        leaf(c, 16, GROUND - 2, dx, dy, l, P['leaf'], width=2)
    c.outline()
    return c


def carrot_3():
    c = Canvas(S, S)
    mound(c, 9)
    # orange shoulder peeking out of the soil
    c.ellipse(16, GROUND - 2, 4.5, 3.5, P['orange'])
    c.rect(11, GROUND, 21, GROUND + 2, None)
    mound(c, 9)
    c.ellipse(16, GROUND - 2, 4, 2.5, P['orange'])
    for dx, dy, l in [(-0.7, -1, 11), (-0.2, -1, 13), (0.3, -1, 13), (0.8, -1, 11), (-1, -0.5, 8), (1, -0.5, 8)]:
        leaf(c, 16, GROUND - 4, dx, dy, l, P['leaf'], width=2)
    c.outline()
    return c


# ---------- potato ----------
def potato_2():
    c = Canvas(S, S)
    mound(c, 9)
    for (x, y, rx, ry) in [(12, 19, 5, 4), (20, 19, 5, 4), (16, 15, 6, 5)]:
        c.ellipse(x, y, rx, ry, P['leaf2'])
    stem(c, 15, GROUND - 2, 15, GROUND - 6, P['leaf2'])
    c.outline()
    return c


def potato_3():
    c = Canvas(S, S)
    mound(c, 11)
    for (x, y, rx, ry) in [(10, 17, 6, 5), (22, 17, 6, 5), (16, 11, 7, 6), (16, 18, 6, 4)]:
        c.ellipse(x, y, rx, ry, P['leaf2'])
    # little white flowers
    for (x, y) in [(13, 9), (19, 12), (9, 15), (23, 14)]:
        c.set(x, y, P['white'][3]); c.set(x + 1, y, P['white'][2]); c.set(x, y + 1, P['yellow'][2])
    # potatoes peeking out
    c.ellipse(10, GROUND - 0.5, 3, 2, P['tan'])
    c.ellipse(21, GROUND, 3.5, 2, P['tan'])
    c.outline()
    return c


# ---------- corn ----------
def corn_2():
    c = Canvas(S, S)
    mound(c, 6)
    stem(c, 15, GROUND - 2, 15, 9, P['leaf2'])
    for (y, d) in [(22, -1), (18, 1), (14, -1), (11, 1)]:
        leaf(c, 16, y, d * 1, -0.35, 8, P['leaf2'], width=2)
    c.outline()
    return c


def corn_3():
    c = Canvas(S, S)
    mound(c, 7)
    stem(c, 15, GROUND - 2, 15, 3, P['leaf2'])
    for (y, d) in [(24, -1), (21, 1), (17, -1), (13, 1), (9, -1)]:
        leaf(c, 16, y, d * 1, -0.35, 9, P['leaf2'], width=2)
    # tassel
    for dx in (-2, 0, 2):
        c.line(16, 3, 16 + dx, 0, P['straw'][2])
    # cobs
    for (x, y) in [(20, 15), (11, 19)]:
        c.ellipse(x, y, 2.5, 4.5, P['yellow'])
        c.line(x - 2, y + 3, x - 3, y + 6, P['leaf2'][2])
        c.set(x, y - 5, P['tan'][3])
    c.outline()
    return c


# ---------- strawberry ----------
def strawberry_2():
    c = Canvas(S, S)
    mound(c, 9)
    for (x, y) in [(11, 20), (21, 20), (16, 17), (14, 22), (18, 22)]:
        c.ellipse(x, y, 4, 3, P['leaf'])
        c.line(x, y + 2, 16, GROUND - 1, P['leaf'][1])
    # white flower
    for (x, y) in [(16, 14)]:
        for dx, dy in [(-1, 0), (1, 0), (0, -1), (0, 1)]:
            c.set(x + dx, y + dy, P['white'][3])
        c.set(x, y, P['yellow'][2])
    c.outline()
    return c


def strawberry_3():
    c = Canvas(S, S)
    mound(c, 11)
    for (x, y) in [(9, 18), (23, 18), (16, 14), (12, 21), (20, 21), (16, 20)]:
        c.ellipse(x, y, 4.5, 3.5, P['leaf'])
    # berries
    for (x, y) in [(10, 23), (21, 24), (16, 24), (25, 21)]:
        c.poly([(x - 2.5, y - 2), (x + 2.5, y - 2), (x + 1, y + 2.5), (x - 1, y + 2.5)], P['red'], light_axis='diag')
        c.set(x - 1, y - 1, P['red'][3]); c.set(x, y, P['yellow'][3])
        c.set(x, y - 3, P['leaf'][2])
    c.outline()
    return c


CROPS = {
    'carrot': [seedling, sprout, carrot_2, carrot_3],
    'potato': [seedling, lambda: sprout(P['leaf2']), potato_2, potato_3],
    'corn': [seedling, lambda: sprout(P['leaf2']), corn_2, corn_3],
    'strawberry': [seedling, sprout, strawberry_2, strawberry_3],
}


# ---------- ground tiles (seamless 32x32) ----------
def dirt_tile(seed=1):
    c = Canvas(S, S)
    rnd = seeded(seed)
    r = P['dirt']
    for y in range(S):
        for x in range(S):
            c.set(x, y, r[2])
    for _ in range(70):
        x, y = rnd.randrange(S), rnd.randrange(S)
        c.set(x, y, r[1] if rnd.random() < 0.7 else r[3])
    for _ in range(6):  # pebbles (warm, low contrast)
        x, y = rnd.randrange(S), rnd.randrange(S)
        c.set(x, y, P['tan'][2]); c.set((x + 1) % S, y, P['dirt'][1])
    for _ in range(3):  # tiny grass tufts
        x, y = rnd.randrange(S), rnd.randrange(2, S)
        c.set(x, y, P['leaf'][1]); c.set(x, y - 1, P['leaf'][2])
    return c


def tilled_tile(wet=False, seed=2):
    c = Canvas(S, S)
    rnd = seeded(seed)
    base = P['dirt'] if not wet else [darken_c(P['dirt'][0], 0.7), darken_c(P['dirt'][1], 0.72), darken_c(P['dirt'][2], 0.7), darken_c(P['dirt'][3], 0.72)]
    for y in range(S):
        for x in range(S):
            c.set(x, y, base[1])
    # furrows: 4 raised ridges per tile
    for row in range(4):
        y0 = row * 8
        for x in range(S):
            c.set(x, y0 + 1, base[3])
            c.set(x, y0 + 2, base[2])
            c.set(x, y0 + 3, base[2])
            c.set(x, y0 + 4, base[1])
            c.set(x, y0 + 6, base[0])
    for _ in range(40):
        x, y = rnd.randrange(S), rnd.randrange(S)
        p = c.get(x, y)
        c.set(x, y, lighten_c(p, 0.12) if rnd.random() < 0.5 else darken_c(p, 0.85))
    if wet:
        for _ in range(7):  # glints of water on the ridges
            x, y = rnd.randrange(S), rnd.randrange(4) * 8 + 2
            c.set(x, y, (104, 128, 150, 255)); c.set((x + 1) % S, y, (80, 100, 122, 255))
    return c


TILES = {
    'dirt': dirt_tile,
    'tilled': lambda: tilled_tile(False),
    'tilled_wet': lambda: tilled_tile(True),
}


# ---------- sunflower (content update) ----------
def sunflower_2():
    c = Canvas(S, S)
    mound(c, 6)
    stem(c, 15, GROUND - 2, 15, 8)
    for (y, d) in [(22, -1), (17, 1), (12, -1)]:
        leaf(c, 16, y, d, -0.3, 7, P['leaf'], width=3)
    c.ellipse(16, 7, 3, 3, P['leaf2'])                  # closed bud
    c.outline()
    return c


def sunflower_3():
    import math
    c = Canvas(S, S)
    mound(c, 6)
    stem(c, 15, GROUND - 2, 15, 12)
    for (y, d) in [(23, -1), (18, 1), (14, -1)]:
        leaf(c, 16, y, d, -0.3, 7, P['leaf'], width=3)
    for i in range(10):
        a = i * math.pi / 5
        c.ellipse(16 + math.cos(a) * 6, 7 + math.sin(a) * 5.5, 2.5, 2.5, P['yellow'])
    c.ellipse(16, 7, 4, 3.5, P['wood'])
    c.set(15, 6, P['wood'][0]); c.set(17, 8, P['wood'][0])
    c.outline()
    return c


CROPS['sunflower'] = [seedling, sprout, sunflower_2, sunflower_3]


# ---------- pumpkin (autumn, content update) ----------
def _vine(c, n):
    for i in range(n):
        x = 6 + i * 5
        c.ellipse(x, GROUND - 4 - (i % 2) * 2, 3.5, 3, P['leaf'])
    c.line(5, GROUND - 2, 6 + n * 5, GROUND - 3, P['leaf'][1])


def pumpkin_2():
    c = Canvas(S, S)
    mound(c, 10)
    _vine(c, 4)
    c.set(20, GROUND - 9, P['yellow'][3]); c.set(21, GROUND - 9, P['yellow'][2])   # flower
    c.outline()
    return c


def pumpkin_3():
    c = Canvas(S, S)
    mound(c, 11)
    _vine(c, 5)
    for cx in (12, 16, 20):
        c.ellipse(cx, GROUND - 6, 4, 5, P['orange'])
    c.line(14, GROUND - 10, 14, GROUND - 2, P['orange'][1]); c.line(18, GROUND - 10, 18, GROUND - 2, P['orange'][1])
    c.rect(15, GROUND - 13, 16, GROUND - 11, P['leaf2'][1])
    c.outline()
    return c


CROPS['pumpkin'] = [seedling, sprout, pumpkin_2, pumpkin_3]
