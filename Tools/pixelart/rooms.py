"""H-2: the insides of the mill (bangakgan), the corner store (haengbok syupeo) and the village hall.

Same scale as the house (home.py): 1 m is about 20 px, the player sprite 32 px (about 1.6 m).
Each room is a 16:9 picture. Its layout (walls, the door, where people and things stand) goes to
Assets/Resources/Maps/rooms.json, which the game (InteriorRooms.cs) and the web preview both read.

World placement: the rooms live in the Market scene, out of sight to the east of the market
(origin x 40 / 52 / 64). Picture pixel (px, py) -> world (originX + (px - w/2) / 32, originY + (h/2 - py) / 32).
"""
import json
import os
from PIL import Image
from pixel import Canvas, P, ramp, darken_c, lighten_c, seeded
from backgrounds import _h
import home as HM
import villagers as V
import k3

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
WALL_H, FRONT_H = 32, 16

BLOCK = ramp('#9a968c', '#bcb8ae', '#d6d2c8', '#ecebe4')         # white-painted cement block
MINT = ramp('#6a8c84', '#8cb0a6', '#acd0c4', '#cce8de')          # the corner store's painted wall
CEMENT = ramp('#6e6a62', '#8a857c', '#a29d92', '#bab4a8')
LINO = ramp('#5a6a5e', '#7a8a7c', '#98a898', '#b8c6b6')
METAL_G = ramp('#22402e', '#2e5a40', '#46805c', '#70a882')       # machine green
RED_M = ramp('#5a1414', '#8e2020', '#c03838', '#e07060')


# ---------------------------------------------------------------- the room frame
def frame(w, h, door, wall='plaster', floor='jangpan', posts=()):
    c = Canvas(w, h)
    wr = {'plaster': HM.PLASTER, 'block': BLOCK, 'mint': MINT}[wall]
    for y in range(WALL_H):
        for x in range(w):
            if wall == 'block':
                k = 1 if (y % 8 == 7 or (x + (y // 8) * 9) % 18 == 0) else 2   # block courses
            elif wall == 'mint':
                k = 2 if y > 6 else 3
            else:
                k = 1 if _h(x // 3, y // 2, 7) % 19 == 0 else 2
            c.set(x, y, wr[k])
    HM._beam(c, 0, 0, w - 1, 3)
    HM._beam(c, 0, WALL_H - 3, w - 1, WALL_H - 1)
    for px in posts:
        HM._post(c, px, 0, px + 4, WALL_H - 1)
    fy0, fy1 = WALL_H, h - FRONT_H
    for y in range(fy0, fy1):
        for x in range(w):
            if floor == 'jangpan':
                col = HM.JANGPAN[2] if (x // 36 + (y - fy0) // 26) % 3 else tuple((a + b) // 2 for a, b in zip(HM.JANGPAN[1], HM.JANGPAN[2]))
                if x % 36 == 0 or (y - fy0) % 26 == 0:
                    col = darken_c(col, 0.88)
            elif floor == 'cement':
                import math
                n = math.sin(x / 17.0 + y / 23.0) * math.sin(y / 11.0 - x / 29.0) + 0.3 * math.sin(x / 7.0 - y / 5.0)
                faint = tuple((a + 2 * b) // 3 for a, b in zip(CEMENT[1][:3], CEMENT[2][:3])) + (255,)
                col = faint if n > 0.9 + 0.1 * math.sin(x / 41.0) else CEMENT[2]   # a few faint old stains
            else:                                               # linoleum tiles
                col = LINO[2] if ((x // 16) + ((y - fy0) // 16)) % 2 else LINO[3]
                if x % 16 == 0 or (y - fy0) % 16 == 0:
                    col = LINO[1]
            c.set(x, y, col)
    for y in range(fy0, fy0 + 2):
        for x in range(w):
            c.set(x, y, darken_c(c.get(x, y), 0.85))
    HM._beam(c, 0, fy1, w - 1, fy1 + 2)                          # the front wall and its door
    for y in range(fy1 + 3, h):
        for x in range(w):
            c.set(x, y, wr[1] if (x // 3 + y // 2) % 11 else wr[0])
    HM._beam(c, 0, h - 3, w - 1, h - 1)
    d0, d1 = door
    for y in range(fy1, h):
        for x in range(d0, d1 + 1):
            c.set(x, y, (214, 210, 198, 255) if (y - fy1) % 4 else (186, 180, 166, 255))
    HM._post(c, d0 - 2, fy1, d0 - 1, h - 1)
    HM._post(c, d1 + 1, fy1, d1 + 2, h - 1)
    for y in range(fy1 - 14, fy1):                               # daylight in through the door
        t = (fy1 - y) / 14
        for x in range(int(d0 + 2 - t * 3), int(d1 - 2 + t * 3)):
            if (x + y) % 2 == 0 and c.get(x, y):
                c.set(x, y, lighten_c(c.get(x, y), 0.1 * (1 - t)))
    for x in (0, w - 6):
        HM._post(c, x, 0, x + 5, h - 1)
    return c


def paste(c, src, x, y):
    """Paste a canvas (or a PIL image) with its top-left at (x, y)."""
    img = src.image() if hasattr(src, 'image') else src
    for yy in range(img.height):
        for xx in range(img.width):
            p = img.getpixel((xx, yy))
            if p[3] > 0:
                c.set(x + xx, y + yy, p if p[3] == 255 else blend(c.get(x + xx, y + yy), p))


def blend(dst, p):
    if dst is None:
        return p
    a = p[3] / 255
    return tuple(int(dst[i] * (1 - a) + p[i] * a) for i in range(3)) + (255,)


def framed(c, x0, y0, w, h, inner):
    HM._beam(c, x0, y0, x0 + w - 1, y0)
    HM._beam(c, x0, y0 + h - 1, x0 + w - 1, y0 + h - 1)
    HM._post(c, x0, y0, x0, y0 + h - 1)
    HM._post(c, x0 + w - 1, y0, x0 + w - 1, y0 + h - 1)
    for y in range(y0 + 1, y0 + h - 1):
        for x in range(x0 + 1, x0 + w - 1):
            c.set(x, y, inner(x - x0, y - y0))


# ---------------------------------------------------------------- the mill (방앗간)
def mill():
    w, h, door = 256, 144, (196, 218)
    c = frame(w, h, door, wall='block', floor='cement')
    # a small window, the price board, and his photo
    HM.lattice_window(c, 100, 8, 124, 22)
    framed(c, 140, 6, 30, 20, lambda x, y: HM.HANJI[3] if y > 2 else (176, 40, 44, 255))
    for k in range(4):
        c.line(143, 11 + k * 3, 160, 11 + k * 3, (70, 60, 56, 255)); c.line(162, 11 + k * 3, 166, 11 + k * 3, (176, 40, 44, 255))
    gray = lambda x, y: (60 + (x * 7 + y * 5) % 40,) * 3 + (255,)
    framed(c, 182, 8, 10, 13, lambda x, y: (150, 146, 140, 255) if 3 <= x <= 5 and 2 <= y <= 4 else (90, 88, 86, 255) if 2 <= x <= 6 and y >= 5 else (210, 206, 198, 255))
    c.rect(185, 5, 188, 6, (30, 30, 30, 255))                         # a black ribbon over the frame
    # the rice polisher (jeongmigi): a green metal cabinet with a hopper and a belt wheel
    x0, y0 = 14, 14
    for y in range(y0 + 8, y0 + 44):
        for x in range(x0, x0 + 30):
            c.set(x, y, METAL_G[3] if x == x0 else METAL_G[1] if x >= x0 + 28 else METAL_G[2])
    c.poly([(x0 + 4, y0 + 8), (x0 + 26, y0 + 8), (x0 + 22, y0), (x0 + 8, y0)], P['metal'], light_axis='x')   # hopper
    c.ellipse(x0 + 34, y0 + 22, 5, 5, P['metal']); c.line(x0 + 30, y0 + 18, x0 + 34, y0 + 17, (30, 30, 30, 255))
    c.rect(x0 + 8, y0 + 30, x0 + 20, y0 + 36, (30, 40, 34, 255))
    c.line(x0 + 14, y0 + 44, x0 + 18, y0 + 50, P['metal'][2])         # the chute
    c.shadow(x0 + 15, y0 + 46, 16, 1.5)
    # the red-pepper grinder
    x0 = 58
    for y in range(36, 60):
        for x in range(x0, x0 + 18):
            c.set(x, y, RED_M[3] if x == x0 else RED_M[1] if x >= x0 + 16 else RED_M[2])
    c.poly([(x0 + 2, 36), (x0 + 16, 36), (x0 + 13, 29), (x0 + 5, 29)], P['metal'], light_axis='x')
    for (x, y) in [(x0 + 3, 62), (x0 + 9, 63), (x0 + 15, 61), (x0 + 20, 63)]:
        c.set(x, y, RED_M[2])                                         # pepper dust
    # rice sacks stacked by the left wall (woven white bags with red print)
    sack = ramp('#a8a298', '#cec8bc', '#e8e2d6', '#f8f4ec')
    for i, (sx, sy) in enumerate([(10, 92), (26, 92), (18, 82), (10, 104), (26, 104)]):
        for y in range(sy, sy + 12):
            for x in range(sx, sx + 15):
                c.set(x, y, sack[3] if y == sy else sack[1] if y == sy + 11 else sack[2])
        c.rect(sx + 4, sy + 4, sx + 10, sy + 6, RED_M[2])
    # flour dust where it falls: under the polisher's chute and round the rice-cake table
    rnd = seeded(5)
    for (cx, cy, r, n) in [(32, 66, 9, 24), (128, 88, 18, 30)]:
        for _ in range(n):
            x, y = cx + rnd.randint(-r, r), cy + rnd.randint(-r // 2, r // 2)
            if c.get(x, y) and abs(x - cx) / r + abs(y - cy) / (r / 2) < 1.2:
                c.set(x, y, (226, 222, 212, 255))
    # the rice-cake table in the middle: garaetteok sticks, a tray of injeolmi, a bowl of bean flour
    tx, ty = 104, 66
    c.shadow(tx + 24, ty + 18, 26, 2)
    for y in range(ty, ty + 12):
        for x in range(tx, tx + 48):
            c.set(x, y, HM.WOOD[3] if y == ty else HM.WOOD[2])
    for x in (tx + 2, tx + 44):
        c.rect(x, ty + 12, x + 2, ty + 18, HM.WOOD[1])
    for k in range(4):
        c.line(tx + 4, ty + 3 + k * 2, tx + 24, ty + 3 + k * 2, HM.COTTON[3])         # garaetteok
    c.rect(tx + 28, ty + 3, tx + 40, ty + 9, (196, 160, 90, 255))                       # injeolmi tray
    for x in range(tx + 29, tx + 40, 3):
        c.set(x, ty + 5, (232, 200, 120, 255)); c.set(x + 1, ty + 7, (232, 200, 120, 255))
    c.ellipse(tx + 44, ty + 4, 3, 2, P['white'])
    return c, {
        'id': 'mill', 'w': w, 'h': h, 'door': door, 'originX': 40.0, 'originY': 0.0,
        'nameEn': 'the mill', 'nameKo': '방앗간',
        'entrance': [-5.8, 1.9], 'exit': [-5.8, 1.35],
        'furniture': [[14, 52, 48, 60], [58, 54, 76, 62], [10, 80, 41, 116], [104, 78, 152, 84]],
        'anchors': {'keeper': [128, 46], 'steamer': [182, 66], 'pot': [170, 100], 'arrive': [207, 98]},
    }


# ---------------------------------------------------------------- the corner store (행복슈퍼)
def store():
    w, h, door = 224, 126, (168, 190)
    c = frame(w, h, door, wall='mint', floor='lino')
    # posters on the wall
    framed(c, 116, 6, 16, 20, lambda x, y: (240, 236, 226, 255) if y < 12 else (230, 90, 120, 255) if (x + y) % 5 else (250, 220, 90, 255))
    # the drinks fridge (glass door, bottles)
    x0 = 10
    for y in range(4, 52):
        for x in range(x0, x0 + 26):
            c.set(x, y, (210, 214, 216, 255) if x in (x0, x0 + 25) or y in (4, 51) else (150, 196, 214, 255))
    bottle_cols = [(60, 150, 70, 255), (200, 60, 50, 255), (240, 200, 80, 255), (90, 110, 200, 255), (240, 240, 240, 255)]
    for r in range(4):
        c.line(x0 + 2, 15 + r * 10, x0 + 23, 15 + r * 10, (120, 140, 150, 255))
        for k in range(7):
            col = bottle_cols[(k + r * 2) % len(bottle_cols)]
            c.rect(x0 + 3 + k * 3, 9 + r * 10, x0 + 4 + k * 3, 14 + r * 10, col)
    c.shadow(x0 + 13, 53, 14, 1.5)
    # back shelves: ramen boxes, snack packs, bottles
    sx0, sx1 = 44, 108
    for r, y in enumerate((8, 20, 32)):
        HM._beam(c, sx0, y + 9, sx1, y + 10)
        for x in range(sx0 + 2, sx1 - 2, 6):
            k = (x // 6 + r) % 4
            col = [(200, 40, 40, 255), (240, 180, 40, 255), (60, 120, 200, 255), (230, 110, 40, 255)][k]
            hh = 8 if r < 2 else 6
            c.rect(x, y + 9 - hh, x + 4, y + 8, col)
            c.set(x + 1, y + 9 - hh + 1, lighten_c(col, 0.4))
    for x in (sx0, sx1):
        HM._post(c, x, 6, x + 1, 44)
    # ice-cream chest freezer
    fx, fy = 54, 66
    c.shadow(fx + 20, fy + 20, 22, 2)
    for y in range(fy, fy + 18):
        for x in range(fx, fx + 40):
            c.set(x, y, (236, 238, 240, 255) if y < fy + 6 else (214, 218, 222, 255))
    c.rect(fx + 3, fy + 1, fx + 36, fy + 4, (160, 210, 230, 255))
    c.rect(fx + 4, fy + 9, fx + 20, fy + 12, (230, 70, 90, 255))       # the brand sticker
    # sacks of soybeans and salt by the front wall (K-2)
    for i, (sx, col) in enumerate([(12, (190, 160, 100, 255)), (28, (232, 232, 228, 255))]):
        for y in range(84, 104):
            for x in range(sx, sx + 14):
                c.set(x, y, lighten_c(col, 0.08) if y == 84 else darken_c(col, 0.85) if y == 103 else col)
        c.ellipse(sx + 7, 85, 5, 2, [(200, 170, 110, 255)] * 4 if i == 0 else [(250, 250, 248, 255)] * 4, flat=True)
    # the cash register on the counter beside the auntie
    rx, ry = 170, 46
    c.rect(rx - 6, ry + 8, rx + 22, ry + 18, HM.WOOD[2]); c.line(rx - 6, ry + 8, rx + 22, ry + 8, HM.WOOD[3])
    c.rect(rx, ry, rx + 12, ry + 8, (90, 96, 104, 255)); c.rect(rx + 2, ry + 1, rx + 10, ry + 3, (120, 200, 120, 255))
    return c, {
        'id': 'store', 'w': w, 'h': h, 'door': door, 'originX': 52.0, 'originY': 0.0,
        'nameEn': 'the corner store', 'nameKo': '행복슈퍼',
        'entrance': [7.51, 1.9], 'exit': [7.51, 1.4],
        'furniture': [[10, 46, 36, 53], [54, 76, 94, 84], [12, 94, 42, 104], [164, 58, 192, 64]],
        'anchors': {'keeper': [148, 56], 'arrive': [179, 82]},
    }


# ---------------------------------------------------------------- the village hall (마을회관)
LAVENDER = V.LAVENDER


def recolor(canvas, swaps):
    out = Canvas(canvas.w, canvas.h)
    out.shadows = list(canvas.shadows)
    for y in range(canvas.h):
        for x in range(canvas.w):
            p = canvas.get(x, y)
            if p is not None:
                for src, dst in swaps:
                    for i in range(4):
                        if p[:3] == src[i][:3]:
                            p = dst[i]
                            break
                out.set(x, y, p)
    return out


def elders():
    """Three elders sitting at hwatu: two grandmas and a grandpa (sitting poses made from the villager art)."""
    gran1 = recolor(k3.sit(V.rosa(), k3.HIPS['rosa']), [(V.MAROON, LAVENDER), (V.MOMPPAE, ramp('#2a3a5a', '#3a5a8a', '#5a80b8', '#90b0e0'))])
    gran2 = recolor(k3.sit(V.rosa(), k3.HIPS['rosa']), [(V.MAROON, ramp('#2a4a2a', '#3a6a3a', '#5a9a4a', '#8ac870'))])
    gramp = recolor(k3.sit(V.theo(), k3.HIPS['theo']), [(V.KHAKI, ramp('#3a2a1a', '#5a4028', '#7e5a38', '#a07a52')),
                                                        (P['hair'], ramp('#5a5a5a', '#8a8a8a', '#b4b4b4', '#d8d8d8'))])
    return gran1, gran2, gramp


def hall():
    w, h, door = 256, 144, (196, 218)
    c = frame(w, h, door, wall='plaster', floor='jangpan', posts=(84, 168))
    # framed photos and certificates, the clock
    framed(c, 24, 6, 26, 16, lambda x, y: (120 + (x * 3 + y * 7) % 50,) * 3 + (255,))
    for k, x in enumerate((100, 118, 136)):
        framed(c, x, 7, 14, 11, lambda xx, yy: (244, 236, 200, 255) if (xx + yy) % 7 else (200, 160, 60, 255))
    HM.wall_clock(c, 186, 12)
    # the broadcast corner: a desk, an amplifier, a microphone on a stand
    bx, by = 204, 30
    c.shadow(bx + 18, by + 16, 20, 1.5)
    c.rect(bx, by + 6, bx + 36, by + 9, HM.WOOD[3]); c.rect(bx, by + 10, bx + 36, by + 12, HM.WOOD[2])
    for x in (bx + 1, bx + 33):
        c.rect(x, by + 13, x + 1, by + 16, HM.WOOD[1])
    c.rect(bx + 4, by - 2, bx + 18, by + 5, (60, 62, 68, 255))
    for k in range(4):
        c.set(bx + 6 + k * 3, by + 1, (220, 200, 90, 255) if k % 2 else (120, 220, 120, 255))
    c.line(bx + 26, by + 5, bx + 26, by - 6, P['metal'][1]); c.ellipse(bx + 26, by - 7, 2, 2, P['metal'])
    c.line(bx + 18, by + 4, bx + 25, by + 4, (30, 30, 30, 255))
    # the briquette stove with its pipe and a kettle
    sx, sy = 62, 56
    c.shadow(sx + 7, sy + 22, 9, 1.5)
    for y in range(sy, sy + 20):
        for x in range(sx, sx + 14):
            c.set(x, y, P['metal'][2] if x == sx else P['metal'][0] if x >= sx + 12 else P['metal'][1])
    c.rect(sx + 3, sy + 12, sx + 10, sy + 16, P['fire'][1]); c.rect(sx + 5, sy + 13, sx + 8, sy + 15, P['fire'][2])
    c.rect(sx + 5, 12, sx + 8, sy - 1, P['metal'][1])                    # the chimney pipe up the wall
    c.line(sx + 5, 12, sx + 8, 12, P['metal'][3])
    c.ellipse(sx + 7, sy - 2, 5, 3, ramp('#7a6a40', '#a8925a', '#d0b878', '#f0dca0'))   # yellow aluminium kettle
    c.line(sx + 11, sy - 3, sx + 14, sy - 5, (208, 184, 120, 255))
    for k in range(3):
        c.set(sx + 6 + k, sy - 7 - k * 2, (255, 255, 255, 140 - k * 30))
    # elders at hwatu on an army blanket
    g1, g2, gp = elders()
    paste(c, g1, 98, 52)
    paste(c, gp, 124, 50)
    paste(c, g2, 150, 52)
    blanket = ramp('#3a4224', '#56603a', '#727c4c', '#909a68')
    for y in range(80, 94):
        for x in range(104, 178):
            c.set(x, y, blanket[3] if y == 80 else blanket[1] if y == 93 else blanket[2])
    for (x, y, col) in [(116, 84, (220, 40, 40, 255)), (126, 86, (240, 240, 240, 255)), (140, 83, (220, 40, 40, 255)), (152, 87, (40, 40, 40, 255)), (160, 84, (220, 40, 40, 255))]:
        c.rect(x, y, x + 2, y + 3, col); c.set(x + 1, y + 1, (250, 250, 250, 255))   # hwatu cards
    # floor cushions stacked by the wall, the shoe rack by the door
    for k in range(4):
        HM.cushion(c, 30, 52 - k * 3, [HM.SILK_R, HM.SILK_G, HM.SILK_Y, HM.SILK_R][k])
    rk = 170
    HM._beam(c, rk, 112, rk + 20, 113); HM._beam(c, rk, 120, rk + 20, 121)
    for k in range(4):
        c.rect(rk + 2 + k * 5, 109, rk + 5 + k * 5, 111, [(240, 240, 236, 255), (40, 40, 44, 255)][k % 2])   # rubber shoes
    return c, {
        'id': 'hall', 'w': w, 'h': h, 'door': door, 'originX': 64.0, 'originY': 0.0,
        'nameEn': 'the village hall', 'nameKo': '마을회관',
        'entrance': [6.85, -3.25], 'exit': [6.85, -3.55],
        'furniture': [[62, 72, 76, 78], [98, 70, 180, 94], [204, 40, 240, 46], [18, 50, 42, 56], [170, 106, 190, 122]],
        'anchors': {'mayorSeat': [56, 100], 'arrive': [207, 98]},
    }


ROOMS = [mill, store, hall]


def export_layout(layouts):
    path = os.path.join(ROOT, 'Assets', 'Resources', 'Maps', 'rooms.json')
    data = {'rooms': []}
    for L in layouts:
        r = dict(L)
        r['doorX0'], r['doorX1'] = L['door']
        del r['door']
        r['entranceX'], r['entranceY'] = L['entrance']
        r['exitX'], r['exitY'] = L['exit']
        del r['entrance'], r['exit']
        r['furniture'] = [{'x0': a, 'y0': b, 'x1': cc, 'y1': d} for (a, b, cc, d) in L['furniture']]
        r['anchors'] = [{'name': k, 'x': v[0], 'y': v[1]} for k, v in L['anchors'].items()]
        r['wallH'], r['frontH'] = WALL_H, FRONT_H
        data['rooms'].append(r)
    with open(path, 'w', encoding='utf-8') as f:
        json.dump(data, f, ensure_ascii=False, indent=1)
    return path


def all_rooms():
    out, layouts = {}, []
    for fn in ROOMS:
        canvas, layout = fn()
        out['room_' + layout['id']] = canvas
        layouts.append(layout)
    return out, layouts


if __name__ == '__main__':
    import sys
    outdir = sys.argv[1] if len(sys.argv) > 1 else '.'
    rooms, layouts = all_rooms()
    for name, c in rooms.items():
        c.image().resize((c.w * 4, c.h * 4), Image.NEAREST).save(os.path.join(outdir, name + '.png'))
    print(export_layout(layouts))
