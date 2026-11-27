"""Concept mock: the home farm as a Korean countryside homestead (direction C).

Not wired into the game. One screen (24 x 14 tiles at 32px), early autumn:
stone walls, a tin-roofed house with a wooden porch, jang-dok-dae (sauce jars),
a persimmon tree over a pyeong-sang, ridged vegetable plots (one resting under straw),
terraced rice paddies with wrapped bales, a stream with stepping stones,
pine hill behind, utility poles along the village lane.

    python3 Tools/pixelart/mock_kvillage.py <out_dir>
"""
import os
import sys
from PIL import Image, ImageDraw

from pixel import Canvas, P, ramp, darken_c, lighten_c, seeded
from backgrounds import BG, grass, patch, path, tuft, _h, gray_canvas

W, H = 768, 448

YARD = ramp('#5e4a2e', '#8c7048', '#b49668', '#d6bd8e')
WALLSTONE = ramp('#3a3632', '#645e56', '#8f877b', '#bcb3a4')
ROOF = ramp('#1d3644', '#2d5568', '#457a90', '#7fb0c2')
PLASTER = ramp('#7e7566', '#bdb4a2', '#e2dac8', '#f7f2e6')
ONGGI = ramp('#26140e', '#46271a', '#6a3f27', '#9a6b47')
RICE = ramp('#6a5418', '#a6852a', '#d4b244', '#f0d986')
STUBBLE = ramp('#54422a', '#7e663e', '#a2875a', '#c4a97a')
PINE = ramp('#14301e', '#22492a', '#356a35', '#5f9049')
PINEBARK = ramp('#3e1e14', '#6e3424', '#9a5238', '#c27a56')
CABBAGE = ramp('#2c5228', '#548e3e', '#8fc463', '#d2eca0')
SOIL = ramp('#33200f', '#55351c', '#77502d', '#9b6c42')
GROUND = P['leaf2']


# ---------------------------------------------------------------- props
def stone_wall_h(length, seed=1):
    """Dry stone wall (dol-dam) seen from the front-top: a rounded cap and two courses of stones."""
    c = Canvas(length, 16)
    rnd = seeded(seed)
    c.shadow(length / 2, 15, length / 2, 1.5, alpha=60)
    for row, (y0, y1) in enumerate(((4, 9), (9, 14))):
        x = -rnd.randint(0, 4) if row else 0
        while x < length:
            w = rnd.randint(5, 9)
            for yy in range(y0, y1):
                for xx in range(max(0, x), min(length, x + w - 1)):
                    edge_l, edge_t = xx == max(0, x), yy == y0
                    edge_r, edge_b = xx == min(length, x + w - 1) - 1, yy == y1 - 1
                    col = WALLSTONE[2]
                    if edge_t or edge_l:
                        col = WALLSTONE[3]
                    if edge_b or edge_r:
                        col = WALLSTONE[1]
                    if (edge_l or edge_r) and (edge_t or edge_b):
                        col = None                      # rounded corners show the mortar
                    c.set(xx, yy, col or WALLSTONE[0])
            x += w
    # cap: a row of smaller rounded stones and a little moss
    x = 0
    while x < length:
        w = rnd.randint(4, 7)
        for xx in range(x, min(length, x + w - 1)):
            c.set(xx, 2, WALLSTONE[3] if xx > x else WALLSTONE[2])
            c.set(xx, 3, WALLSTONE[2])
        x += w
        if rnd.random() < 0.3:
            c.set(x - 2, 2, P['leaf2'][2]); c.set(x - 3, 2, P['leaf2'][1])
    c.outline()
    return c


def stone_wall_v(length, seed=2):
    c = Canvas(10, length)
    rnd = seeded(seed)
    y = 0
    while y < length:
        h = rnd.randint(4, 7)
        for yy in range(y, min(length, y + h - 1)):
            for xx in range(1, 9):
                col = WALLSTONE[2]
                if yy == y:
                    col = WALLSTONE[3]
                elif yy == y + h - 2 or xx == 8:
                    col = WALLSTONE[1]
                c.set(xx, yy, col)
        y += h
    c.outline()
    return c


def house(roof=ROOF):
    """A one-storey country house: tin roof, white walls between timber posts, paper doors, wooden porch."""
    c = Canvas(140, 104)
    c.shadow(70, 101, 66, 3)
    # stone plinth
    for x in range(8, 132):
        for y in range(92, 100):
            c.set(x, y, WALLSTONE[1] if (x // 7 + y // 4) % 2 else WALLSTONE[2])
    c.rect(8, 92, 131, 92, WALLSTONE[3])
    # porch (toenmaru): planks running sideways, a darker front edge
    for y in range(82, 92):
        for x in range(12, 128):
            c.set(x, y, P['woodlight'][2] if (y - 82) % 3 else P['woodlight'][1])
    c.rect(12, 82, 127, 82, P['woodlight'][3])
    c.rect(12, 91, 127, 91, P['wood'][1])
    # stepping stone in front of the porch
    c.rect(62, 99, 77, 101, WALLSTONE[2]); c.rect(62, 99, 77, 99, WALLSTONE[3])
    # wall between posts
    c.rect(16, 46, 123, 81, PLASTER[2])
    c.rect(16, 46, 123, 49, PLASTER[1])                       # eave shadow
    posts = [16, 42, 70, 98, 123]
    for px in posts:
        c.rect(px - 1, 46, px + 1, 81, P['wood'][1]); c.rect(px - 1, 46, px - 1, 81, P['wood'][2])
    c.rect(16, 78, 123, 81, P['wood'][1])                      # sill beam
    # paper doors (changho) in the middle bays, a small window in the side bays
    for (x0, x1) in ((45, 67), (73, 95)):
        c.rect(x0, 53, x1, 77, PLASTER[3])
        for x in range(x0, x1 + 1, 4):
            c.line(x, 53, x, 77, P['woodlight'][1])
        for y in range(53, 78, 5):
            c.line(x0, y, x1, y, P['woodlight'][1])
        c.rect(x0, 53, x1, 53, P['wood'][1]); c.rect(x0, 77, x1, 77, P['wood'][1])
    for (x0, x1) in ((22, 36), (104, 118)):
        c.rect(x0, 56, x1, 68, PLASTER[3])
        c.line((x0 + x1) // 2, 56, (x0 + x1) // 2, 68, P['woodlight'][1])
        c.line(x0, 62, x1, 62, P['woodlight'][1])
        c.rect(x0 - 1, 55, x1 + 1, 55, P['wood'][1]); c.rect(x0 - 1, 69, x1 + 1, 69, P['wood'][1])
    # roof: wide overhang, corrugated tin (vertical waves), ridge on top
    pts = [(0, 50), (14, 10), (126, 10), (140, 50)]
    c.poly(pts, roof, light_axis='y')
    for y in range(10, 51):
        for x in range(0, 140):
            if c.get(x, y) is not None and y > 11 and y < 49:
                k = (x + (y > 30)) % 5
                if k == 0:
                    c.set(x, y, roof[1])
                elif k == 1 and y < 40:
                    c.set(x, y, roof[3] if y < 22 else roof[2])
    c.rect(14, 8, 126, 11, roof[3]); c.rect(14, 11, 126, 11, roof[1])
    c.rect(0, 49, 139, 51, roof[0])                             # eave edge
    for x in range(2, 138, 9):
        c.set(x, 52, roof[0])                                   # gutter brackets
    # a little stovepipe at the side
    c.rect(122, 0, 125, 14, P['metal'][2]); c.rect(122, 0, 125, 0, P['metal'][3]); c.rect(121, 0, 126, 1, P['metal'][1])
    c.outline()
    return c


def jar(w, h, lid=True):
    c = Canvas(w + 2, h + 3)
    c.ellipse(w / 2 + 1, h * 0.58 + 1, w / 2, h * 0.45, ONGGI)
    c.rect(int(w * 0.28) + 1, 1, int(w * 0.72), int(h * 0.2) + 1, ONGGI[1])     # neck
    if lid:
        c.ellipse(w / 2 + 1, 2, w * 0.3, 1.8, ONGGI, flat=True)
        c.set(int(w / 2) + 1, 0, ONGGI[3])
    c.set(int(w * 0.3) + 1, int(h * 0.45), lighten_c(ONGGI[3], 0.5))         # glaze glint
    c.set(int(w * 0.3) + 1, int(h * 0.45) + 1, ONGGI[3])
    c.outline()
    return c


def jangdokdae():
    """Raised stone platform with sauce jars (big ones at the back)."""
    c = Canvas(78, 52)
    c.shadow(39, 50, 37, 2)
    rnd = seeded(5)
    for y in range(36, 49):
        for x in range(2, 76):
            c.set(x, y, WALLSTONE[1] if (x // 6 + (y // 4) * 3) % 4 == 0 else WALLSTONE[2])
    c.rect(2, 36, 75, 37, WALLSTONE[3])
    for x in range(2, 76):
        c.set(x, 30 + (1 if x % 9 == 0 else 0), None)
    c.rect(2, 32, 75, 36, YARD[3])
    jars = [(6, 6, 20, 26), (28, 4, 22, 28), (52, 7, 19, 25), (12, 22, 13, 15), (34, 24, 11, 13), (50, 22, 14, 15), (64, 25, 10, 12)]
    for (x, y, w, h) in jars:
        j = jar(w, h)
        for yy in range(j.h):
            for xx in range(j.w):
                p = j.get(xx, yy)
                if p is not None:
                    c.set(x + xx, y + yy, p)
    c.outline()
    return c


def pyeongsang():
    c = Canvas(52, 26)
    c.shadow(26, 24, 25, 2)
    for x in (4, 46):
        c.box(x, 12, x + 2, 23, P['wood'])
    c.box(1, 8, 50, 14, P['woodlight'])
    for x in range(3, 50, 6):
        c.line(x, 9, x, 13, P['woodlight'][1])
    # a straw mat and a tray of drying peppers
    c.rect(10, 9, 30, 12, P['straw'][2]); c.line(10, 9, 30, 9, P['straw'][3])
    for x in range(12, 29, 3):
        c.set(x, 10, P['red'][2]); c.set(x + 1, 11, P['red'][1])
    c.outline()
    return c


def persimmon_tree():
    c = Canvas(76, 84)
    c.shadow(38, 82, 26, 3)
    rnd = seeded(11)
    # twisted trunk with two limbs
    c.poly([(33, 82), (36, 50), (41, 50), (44, 82)], P['wood'], light_axis='x')
    c.thick_line(37, 56, 24, 40, P['wood'][1], 3)
    c.thick_line(40, 54, 54, 38, P['wood'][1], 3)
    c.set(38, 66, P['wood'][3]); c.set(39, 72, P['wood'][0])
    leaf = ramp('#2a4a22', '#4d7a32', '#7fa646', '#c2cf68')
    for (cx, cy, rx, ry) in [(38, 30, 30, 20), (20, 38, 16, 13), (56, 37, 17, 13), (30, 20, 16, 12), (48, 22, 15, 12)]:
        c.ellipse(cx, cy, rx, ry, leaf)
    for i in range(60):                                     # loose leaf edge
        x, y = rnd.randint(6, 70), rnd.randint(6, 52)
        if c.get(x, y) is not None and c.get(x, y + 2) is None:
            c.set(x, y + 1, leaf[1])
    for i in range(26):                                     # persimmons
        x, y = rnd.randint(10, 66), rnd.randint(10, 48)
        if c.get(x, y) is not None and c.get(x + 1, y + 1) is not None:
            c.set(x, y, P['orange'][2]); c.set(x + 1, y, P['orange'][2])
            c.set(x, y + 1, P['orange'][1]); c.set(x + 1, y + 1, P['orange'][2])
            c.set(x, y - 1, P['orange'][3])
    c.outline()
    return c


def pine(seed=0, h=60):
    rnd = seeded(seed)
    c = Canvas(48, h)
    c.shadow(24, h - 2, 14, 2.5)
    lean = rnd.randint(-4, 4)
    c.thick_line(24, h - 2, 24 + lean, h * 0.35, PINEBARK[2], 3)
    c.line(23, h - 3, 23 + lean, h * 0.4, PINEBARK[1])
    c.set(25, h - 10, PINEBARK[3]); c.set(25 + lean // 2, h - 22, PINEBARK[3])
    # flat, layered clumps like a Korean red pine
    for k, (dy, rx) in enumerate(((0.18, 15), (0.34, 19), (0.5, 14))):
        cx = 24 + lean + rnd.randint(-5, 5)
        cy = h * dy + 4
        c.ellipse(cx, cy, rx, 6, PINE)
        c.ellipse(cx - rx * 0.5, cy + 2, rx * 0.5, 4, PINE)
        for x in range(int(cx - rx), int(cx + rx), 3):
            if c.get(x, int(cy) - 4) is not None:
                c.set(x, int(cy) - 5, PINE[3])
    c.outline()
    return c


def pepper_plant():
    c = Canvas(9, 13)
    c.line(4, 12, 4, 5, P['leaf'][1])
    for (x, y) in [(2, 4), (6, 4), (1, 6), (7, 7), (3, 2), (5, 2), (4, 1)]:
        c.set(x, y, P['leaf'][2]); c.set(x, y + 1, P['leaf'][1])
    for (x, y) in [(2, 7), (6, 9), (5, 5)]:
        c.set(x, y, P['red'][2]); c.set(x, y + 1, P['red'][2]); c.set(x, y + 2, P['red'][1])
    c.outline()
    return c


def cabbage():
    c = Canvas(12, 10)
    c.ellipse(6, 5.5, 5.2, 4.2, CABBAGE)
    c.ellipse(6, 4.5, 3, 2.6, ramp('#9cc66a', '#c4e08c', '#e4f2b8', '#f8fde4'))
    c.line(6, 2, 6, 7, CABBAGE[1])
    c.outline()
    return c


def scallion():
    c = Canvas(7, 12)
    for x, top in ((2, 1), (4, 0), (3, 3), (5, 4)):
        c.line(x, 11, x, top, P['leaf'][2])
    c.set(4, 0, P['leaf'][3]); c.set(2, 1, P['leaf'][3])
    c.rect(2, 10, 5, 11, PLASTER[3])
    c.outline()
    return c


def bale():
    c = Canvas(20, 16)
    c.shadow(10, 15, 9, 1.5)
    c.ellipse(10, 8, 9, 7, P['white'])
    c.ellipse(5, 8, 3.2, 6, ramp('#9a9aa4', '#c8c8d0', '#e2e2ea', '#f4f4f8'), flat=True)
    c.line(13, 2, 13, 14, P['white'][1])
    c.outline()
    return c


def pole(speaker=False, lamp=True):
    """A concrete utility pole. lamp: a small 보안등 on a bent arm to the left (lit at night, N-1:
    the light sits 6 px left of the pole's centre and 70 px above its foot, see NightLight.PoleLampHead)."""
    c = Canvas(30 if speaker else 18, 92)
    c.shadow(9, 90, 5, 1.5)
    c.rect(8, 6, 10, 89, P['stone'][2]); c.rect(8, 6, 8, 89, P['stone'][3]); c.rect(10, 6, 10, 89, P['stone'][1])
    c.rect(1, 10, 16, 11, P['wood'][1])
    for x in (2, 8, 15):
        c.rect(x, 7, x + 1, 9, P['white'][2])
    c.rect(11, 40, 13, 46, P['metal'][2])                      # transformer box
    if lamp:                                                  # the security lamp: a bent pipe arm, a flat hood, a pale bulb
        for (x, y) in ((7, 23), (6, 22), (5, 21), (4, 21)):
            c.set(x, y, P['metal'][1])
        c.rect(1, 21, 5, 21, P['metal'][2]); c.rect(1, 22, 5, 22, P['metal'][1])
        c.rect(2, 23, 4, 23, P['sun'][3]); c.set(3, 24, P['white'][3])
    if speaker:                                               # the village loudspeaker: two gray horns on a bracket
        c.rect(10, 18, 20, 19, P['metal'][1])
        for (x0, d) in ((18, 1),):
            c.poly([(x0, 14), (x0 + 8 * d, 10), (x0 + 8 * d, 24), (x0, 20)], P['white'], light_axis='x')
        c.poly([(12, 22), (22, 20), (22, 32), (12, 28)], P['white'], light_axis='x')
        c.rect(26, 11, 27, 23, P['white'][1]); c.rect(22, 21, 23, 31, P['white'][1])
    c.outline()
    return c


def loudspeaker_icon():
    """HUD icon for the village broadcast: a horn loudspeaker with sound arcs."""
    c = Canvas(32, 32)
    c.poly([(6, 12), (18, 5), (18, 27), (6, 20)], P['white'], light_axis='x')
    c.box(2, 12, 7, 20, P['metal'])
    for (r, col) in ((5, P['sun'][2]), (9, P['sun'][3])):
        for a in range(-50, 51, 10):
            import math
            x = 20 + r * math.cos(math.radians(a)); y = 16 + r * math.sin(math.radians(a))
            c.set(int(x), int(y), col)
    c.outline()
    return c


def stepping_stone():
    c = Canvas(12, 7)
    c.ellipse(6, 3.5, 5.5, 3, WALLSTONE)
    c.outline()
    return c


# ---------------------------------------------------------------- ground
def blob(bg, pts_fn, color_fn):
    for y in range(H):
        for x in range(W):
            if pts_fn(x, y):
                col = color_fn(x, y)
                if col:
                    bg.c.set(x, y, col)


def ridged_plot(bg, x0, y0, x1, y1, kind, seed):
    """A vegetable plot of raised ridges (ireang) and furrows (gorang), with a ragged grass edge."""
    rnd = seeded(seed)
    period = 9
    for y in range(y0, y1):
        for x in range(x0, x1):
            # ragged outline: indent the corners and nibble the edge
            dx = min(x - x0, x1 - 1 - x)
            dy = min(y - y0, y1 - 1 - y)
            if dx + dy < 5 or (dx == 0 and _h(x, y // 3, seed) % 3 == 0) or (dy == 0 and _h(x // 3, y, seed) % 3 == 0):
                continue
            k = (y - y0) % period
            if k in (0, 1):
                col = SOIL[0] if k == 0 else SOIL[1]           # furrow
            elif k == 2:
                col = SOIL[3]                                  # lit top of the ridge
            elif k in (3, 4, 5):
                col = SOIL[2]
            else:
                col = SOIL[1]                                  # shaded front of the ridge
            if _h(x, y, seed + 3) % 41 == 0:
                col = SOIL[3] if col != SOIL[3] else SOIL[2]   # clods
            bg.c.set(x, y, col)
    # grass creeping in at the edge
    for x in range(x0, x1, 4):
        if _h(x, 0, seed) % 2:
            tuft(bg, x, y0 + 1, GROUND)
        if _h(x, 1, seed) % 2:
            tuft(bg, x, y1 - 1, GROUND)
    # crops along the ridges
    for ry in range(y0 + 5, y1 - 3, period):
        for x in range(x0 + 8, x1 - 6, {'pepper': 10, 'cabbage': 13, 'scallion': 7}.get(kind, 99)):
            if kind == 'pepper':
                bg.prop(pepper_plant(), x=x + rnd.randint(-1, 1), y=ry + 1)
            elif kind == 'cabbage':
                bg.prop(cabbage(), x=x + rnd.randint(-1, 1), y=ry + 2)
            elif kind == 'scallion':
                bg.prop(scallion(), x=x, y=ry + 1)
    if kind == 'resting':
        # straw laid over the ridges: the plot is resting this season
        for i in range((x1 - x0) * (y1 - y0) // 14):
            x, y = rnd.randint(x0 + 3, x1 - 5), rnd.randint(y0 + 3, y1 - 4)
            ln = rnd.randint(3, 6)
            dx = rnd.choice((1, 1, -1))
            for t in range(ln):
                bg.c.set(x + t * dx, y + (t // 3), P['straw'][2] if t % 2 else P['straw'][3])
            bg.c.set(x, y + 1, P['straw'][1])


def paddies(bg, y_top):
    """Terraced paddies along the bottom: golden rice, one harvested field with wrapped bales, grass levees."""
    fields = [(0, 190, 'rice'), (196, 430, 'stubble'), (436, 600, 'rice'), (606, W, 'rice')]
    tops = [y_top + 6, y_top, y_top + 10, y_top + 3]
    for (x0, x1, kind), ty in zip(fields, tops):
        for y in range(ty, H):
            for x in range(x0, x1):
                lev = y - ty < 4 or x - x0 < 3 or x1 - 1 - x < 3
                if lev:
                    col = GROUND[2] if (y - ty) % 4 else GROUND[3]
                    if x - x0 < 3 or x1 - 1 - x < 3:
                        col = GROUND[1] if x1 - 1 - x < 3 else GROUND[2]
                elif kind == 'rice':
                    row = (y - ty) % 6
                    stalk = (x + (y - ty) // 6 * 2) % 4
                    col = RICE[2]
                    if row == 0:
                        col = RICE[1]
                    elif stalk == 0 and row < 4:
                        col = RICE[3]
                    elif stalk == 2 and row == 4:
                        col = RICE[1]
                else:
                    col = STUBBLE[2]
                    if (y - ty) % 5 == 0 and (x // 2) % 3 == 0:
                        col = STUBBLE[0]
                    elif (y - ty) % 5 == 1 and (x // 2) % 3 == 0:
                        col = STUBBLE[3]
                bg.c.set(x, y, col)
        # levee shadow onto the field
        for x in range(x0 + 3, x1 - 3):
            bg.c.set(x, ty + 4, darken_c(RICE[1] if kind == 'rice' else STUBBLE[1], 0.85))
    for (x, y) in ((250, y_top + 44), (292, y_top + 66), (338, y_top + 40), (372, y_top + 78)):
        bg.prop(bale(), x=x, y=y)


def stream(bg):
    """A small stream coming down from the hill on the left, into the paddies."""
    import math
    water = set()
    for y in range(0, H):
        x = 74 - y * 0.07 + 11 * math.sin(y / 46.0) + 5 * math.sin(y / 17.0 + 1.3)
        half = 6 + 2 * math.sin(y / 31.0) + (2 if 200 < y < 260 else 0)
        for dx in range(-int(half), int(half) + 1):
            water.add((int(x + dx), y))
    for (x, y) in water:
        bg.c.set(x, y, P['water'][2])
    for (x, y) in water:
        for dx, dy in ((1, 0), (-1, 0)):
            q = (x + dx, y + dy)
            if q not in water and 0 <= q[0] < W:
                bg.c.set(q[0], q[1], YARD[1])           # muddy bank
                bg.c.set(q[0] + dx, q[1], WALLSTONE[2] if _h(q[0], q[1], 4) % 5 == 0 else YARD[2])
        if _h(x, y, 9) % 37 == 0:
            bg.c.set(x, y, P['water'][3]); bg.c.set(x + 1, y, P['water'][3])
        elif _h(x, y, 5) % 23 == 0:
            bg.c.set(x, y, P['water'][1])
    return water


# ---------------------------------------------------------------- scene
def scene():
    bg = BG(W, H, W / 32, H / 32)
    bg.season = 'autumn'
    grass(bg, base=GROUND)
    for (x, y, rx, ry) in [(600, 104, 46, 9), (130, 296, 34, 8), (420, 302, 26, 6)]:
        patch(bg, x, y, rx, ry, lighten_c(GROUND[2], 0.12), GROUND, P['leaf2'])

    # hill behind: darker ground band and red pines
    for y in range(0, 74):
        for x in range(W):
            if y < 60 + int(8 * ((x / 90.0) % 1 < 0.5)) and bg.c.get(x, y) is not None:
                bg.c.set(x, y, darken_c(bg.c.get(x, y), 0.82))
    for i, x in enumerate(range(-6, W + 30, 34)):                      # back row, smaller
        bg.prop(pine(seed=40 + i, h=44 + (i * 5) % 10), x=x + (i * 13) % 11, y=34 + (i * 7) % 6)
    for i, x in enumerate(range(-20, W + 30, 41)):                     # front row, irregular gaps
        if _h(i, 0, 3) % 5 == 0:
            continue
        bg.prop(pine(seed=i, h=56 + (i * 7) % 18), x=x + (_h(i, 1, 3) % 15), y=62 + (_h(i, 2, 3) % 12))

    stream(bg)
    paddies(bg, 336)

    # yard (packed earth) and the village lane
    yard = path(bg, [(168, 160, 470, 262)], r=YARD)
    path(bg, [(304, 262, 336, 318), (304, 300, 768, 330)], r=YARD)
    path(bg, [(100, 226, 168, 240)], r=YARD)                    # foot path west to the stream
    for (x, y) in ((83, 236), (66, 230), (49, 237)):            # stepping stones across the stream
        bg.prop(stepping_stone(), x=x, y=y)

    # vegetable plots outside the wall (different sizes, ragged edges)
    ridged_plot(bg, 500, 128, 612, 206, 'pepper', 1)
    ridged_plot(bg, 622, 140, 746, 232, 'cabbage', 2)
    ridged_plot(bg, 514, 220, 628, 290, 'resting', 3)
    ridged_plot(bg, 642, 246, 724, 292, 'scallion', 4)

    # stone wall around the yard, with a gap for the gate
    bg.prop(stone_wall_h(140, 1), x=230, y=272)
    bg.prop(stone_wall_h(138, 3), x=411, y=272)
    vw = stone_wall_v(150, 2)
    bg.prop(vw, x=160, y=272)
    bg.prop(stone_wall_v(150, 4), x=482, y=272)
    bg.prop(stone_wall_h(80, 5), x=200, y=124)
    bg.prop(stone_wall_h(80, 6), x=442, y=124)

    bg.prop(house(), x=320, y=192)
    bg.prop(jangdokdae(), x=428, y=246)
    bg.prop(persimmon_tree(), x=206, y=214)
    bg.prop(pyeongsang(), x=222, y=250)

    for x in (486, 744):
        bg.prop(pole(), x=x, y=334)

    img = bg.image()
    # power lines between the poles (and off to the village)
    d = ImageDraw.Draw(img)
    for (ax, bx) in ((486, 744), (744, 800)):
        for dy in (0, 6):
            pts = []
            for t in range(0, 41):
                f = t / 40
                x = ax + (bx - ax) * f
                y = 334 - 92 + 9 + dy + 10 * 4 * f * (1 - f)
                pts.append((x, y))
            d.line(pts, fill=(40, 36, 34, 255), width=1)

    # the player for scale, standing in the yard
    here = os.path.dirname(os.path.abspath(__file__))
    pl = os.path.join(here, '..', '..', 'Assets', 'Resources', 'PixelArt', 'Player', 'player_down_0.png')
    if os.path.exists(pl):
        p = Image.open(pl).convert('RGBA')
        img.alpha_composite(p, (300, 214))
    return img


def to_gray(img, darker=0.86):
    out = img.copy()
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            lum = (0.3 * r + 0.59 * g + 0.11 * b) * darker
            px[x, y] = (int(lum * 0.94 + r * 0.06), int(lum * 0.95 + g * 0.05), int(min(255, lum + 8)), a)
    return out


if __name__ == '__main__':
    out = sys.argv[1] if len(sys.argv) > 1 else '.'
    os.makedirs(out, exist_ok=True)
    img = scene()
    img.resize((W * 2, H * 2), Image.NEAREST).save(os.path.join(out, 'mock_kvillage_color.png'))
    to_gray(img).resize((W * 2, H * 2), Image.NEAREST).save(os.path.join(out, 'mock_kvillage_gray.png'))
    print('ok')
