"""The forest (map stage 3), drawn from Assets/Resources/Maps/forest.json.

Two versions of every season: full colour, and a deep gray one. The game shows the gray forest and
lets the colour back in through a round mask over each clearing you restore, and everywhere once the
old tree wakes up. The gray is a fixed per-colour mapping, so it stays a limited pixel-art palette.
"""
import json
import os

import backgrounds as B
from backgrounds import BG, SNOW, SUMMER, AUTUMN_GRASS, AUT_ORANGE, AUT_RED, AUT_YELLOW, snow_cap
from pixel import Canvas, P, ramp, darken_c, lighten_c
from PIL import Image

ROOT = os.path.join(os.path.dirname(__file__), '..', '..')
MAP_PATH = os.path.join(ROOT, 'Assets', 'Resources', 'Maps', 'forest.json')

FLOOR = {
    'spring': ramp('#16351f', '#21502b', '#2f6634', '#4f8a45'),
    'summer': ramp('#102e18', '#1a4a24', '#25602c', '#3f8038'),
    'autumn': ramp('#3a3018', '#5a4a22', '#76602c', '#a08440'),
    'winter': ramp('#7d8ea6', '#a9b8cc', '#d4dee9', '#f2f6fa'),
}
CLEARING = {'spring': P['leaf'], 'summer': SUMMER, 'autumn': AUTUMN_GRASS, 'winter': SNOW}
PINE = ramp('#123321', '#1d4d2e', '#2c6b3a', '#5a9a52')
OAK = {'spring': P['leaf'], 'summer': SUMMER, 'autumn': None, 'winter': None}
BLOSSOM = P['jam']


def load_map():
    with open(MAP_PATH) as f:
        return json.load(f)


def _h(x, y, k=0):
    v = (x * 374761393 + y * 668265263 + k * 2246822519) & 0xFFFFFFFF
    v = (v ^ (v >> 13)) * 1274126177 & 0xFFFFFFFF
    return v ^ (v >> 16)


# ----------------------------------------------------------------- props
def oak(season, variant=0):
    """A round broadleaf tree. Bare branches in winter, warm colours in autumn."""
    c = Canvas(40, 52)
    c.shadow(20, 49, 13, 3)
    c.box(17, 32, 22, 49, P['wood'])
    c.set(19, 38, P['wood'][1]); c.set(20, 44, P['wood'][1])
    c.thick_line(17, 48, 13, 50, P['wood'][1], 2); c.thick_line(22, 48, 26, 50, P['wood'][1], 2)
    if season == 'winter':
        for (x0, y0, x1, y1) in [(19, 33, 9, 14), (20, 33, 31, 12), (19, 30, 20, 4), (13, 22, 6, 18), (27, 20, 34, 17), (20, 18, 25, 9)]:
            c.thick_line(x0, y0, x1, y1, P['wood'][1], 2)
        c.outline()
        return c
    r = OAK[season] or [AUT_ORANGE, AUT_RED, AUT_YELLOW][variant % 3]
    for (cx, cy, rx, ry) in [(12, 24, 10, 9), (28, 24, 10, 9), (20, 15, 13, 12), (20, 26, 12, 8)]:
        c.ellipse(cx, cy, rx, ry, r)
    for (x, y) in [(10, 20), (16, 9), (25, 12), (30, 21), (14, 27)]:
        c.set(x, y, r[3])
    if season == 'spring' and variant % 2 == 0:
        for (x, y) in [(12, 16), (22, 10), (29, 18), (17, 23), (25, 26)]:
            c.set(x, y, BLOSSOM[3]); c.set(x + 1, y, BLOSSOM[2])
    c.outline()
    return c


def pine():
    import world
    return world.tree(PINE)


def bush(season):
    r = {'spring': P['leaf'], 'summer': SUMMER, 'autumn': AUT_RED, 'winter': P['leaf2']}[season]
    return B.bush(30, 22, r, P['jam'] if season == 'spring' else None)


def seat():
    """A stump worn smooth by years of sitting, by the spring."""
    c = Canvas(22, 18)
    c.shadow(11, 16, 9, 2)
    c.box(4, 7, 17, 15, P['wood'])
    c.ellipse(10.5, 7, 7, 3, P['woodlight'], flat=True)
    c.ellipse(10.5, 7, 4, 1.5, [P['woodlight'][1]] * 4, flat=True)
    c.set(7, 11, P['leaf'][2]); c.set(15, 13, P['leaf'][3])
    c.outline()
    return c


def mushroom_ring(bg, x, y):
    for (dx, dy) in [(0, 0), (5, 1), (9, -1), (3, 4), (8, 4)]:
        bg.c.set(x + dx, y + dy, P['white'][2]); bg.c.set(x + dx + 1, y + dy, P['white'][3]); bg.c.set(x + dx, y + dy + 1, P['tan'][1])


# ----------------------------------------------------------------- runtime sprites
def old_tree(awake):
    """The old tree at the heart of the forest: gray and leafless asleep, blossoming awake."""
    c = Canvas(104, 120)
    c.shadow(52, 115, 40, 5)
    bark = P['wood'] if awake else ramp('#2a2a30', '#45454e', '#66666f', '#8c8c96')
    # roots and trunk
    c.poly([(36, 116), (42, 70), (62, 70), (68, 116)], bark, light_axis='x')
    for (x0, x1) in [(36, 22), (40, 30), (64, 80), (68, 86)]:
        c.thick_line(x0, 110, x1, 117, bark[1], 3)
    for y in range(76, 112, 7):
        c.line(46, y, 48, y + 4, bark[1]); c.line(56, y + 3, 58, y + 7, bark[1])
    c.ellipse(52, 92, 4, 6, [bark[0]] * 4, flat=True)            # a hollow
    # main limbs
    for (x0, y0, x1, y1) in [(46, 74, 18, 40), (58, 74, 88, 38), (52, 72, 52, 20), (40, 60, 26, 58), (64, 58, 80, 60)]:
        c.thick_line(x0, y0, x1, y1, bark[2], 5)
    if awake:
        leaf = P['leaf']
        for (cx, cy, rx, ry) in [(22, 42, 20, 16), (82, 40, 20, 16), (52, 24, 28, 20), (36, 54, 16, 10), (70, 54, 16, 10), (52, 44, 22, 14)]:
            c.ellipse(cx, cy, rx, ry, leaf)
        for k in range(70):                                         # blossoms, scattered by hand-picked hash
            x, y = 6 + _h(k, 1, 5) % 94, 6 + _h(k, 2, 5) % 58
            if c.get(x, y) is not None:
                col = BLOSSOM if k % 3 else P['white']
                c.set(x, y, col[3]); c.set(x + 1, y, col[2]); c.set(x, y + 1, col[2])
    else:
        gray = ramp('#3a3a42', '#5a5a64', '#7c7c86', '#a2a2ac')
        for (x0, y0, x1, y1) in [(18, 40, 6, 26), (18, 40, 22, 18), (88, 38, 98, 24), (88, 38, 80, 16), (52, 20, 40, 4), (52, 20, 64, 6)]:
            c.thick_line(x0, y0, x1, y1, gray[1], 3)
        for (x, y) in [(10, 28), (24, 20), (96, 26), (78, 18), (42, 6), (62, 8)]:   # a few dry leaves
            c.set(x, y, gray[3]); c.set(x + 1, y + 1, gray[2])
    c.outline()
    return c


def sapling(stage):
    """-1: an empty dug ring (plant an acorn here), 0: acorn planted, 1: sprout, 2: sapling,
    3: young oak (the clearing is restored)."""
    if stage < 3:
        c = Canvas(24, 26)
        c.ellipse(12, 22, 9, 3, P['dirt'], flat=True)
        c.ellipse(12, 22, 6, 2, [P['dirt'][1]] * 4, flat=True)
        if stage == -1:
            for (x, y) in [(6, 21), (18, 21), (12, 19), (9, 23), (15, 23)]:     # a few pebbles marking the spot
                c.set(x, y, P['stone'][3]); c.set(x + 1, y, P['stone'][2])
        elif stage == 0:
            c.ellipse(12, 20, 2.5, 2, P['tan']); c.rect(11, 17, 13, 18, P['wood'][1])
        elif stage == 1:
            c.line(12, 22, 12, 15, P['leaf'][1])
            c.ellipse(9.5, 15, 2.5, 1.5, P['leaf']); c.ellipse(14.5, 14, 2.5, 1.5, P['leaf'])
        else:
            c.line(12, 22, 12, 6, P['wood'][2]); c.line(12, 14, 8, 11, P['wood'][2]); c.line(12, 10, 16, 7, P['wood'][2])
            for (cx, cy) in [(8, 10), (16, 6), (12, 5), (9, 15), (15, 12)]:
                c.ellipse(cx, cy, 2.6, 1.8, P['leaf'])
        c.outline()
        return c
    c = Canvas(34, 48)
    c.shadow(17, 45, 11, 2.5)
    c.box(15, 28, 18, 45, P['wood'])
    for (cx, cy, rx, ry) in [(10, 22, 8, 7), (24, 22, 8, 7), (17, 13, 10, 10)]:
        c.ellipse(cx, cy, rx, ry, P['leaf'])
    for (x, y) in [(10, 17), (19, 8), (25, 19), (14, 23)]:
        c.set(x, y, BLOSSOM[3]); c.set(x + 1, y, BLOSSOM[2])
    c.outline()
    return c


def log():
    c = Canvas(40, 26)
    c.shadow(20, 23, 18, 2.5)
    c.box(4, 9, 33, 21, P['wood'])
    for x in range(7, 32, 5):
        c.line(x, 11, x + 2, 20, P['wood'][1])
    c.ellipse(34.5, 15, 4.5, 6.5, P['woodlight'], flat=True)
    c.ellipse(34.5, 15, 2.5, 4, [P['woodlight'][1]] * 4, flat=True)
    c.set(34, 15, P['wood'][1])
    for (x, y) in [(6, 9), (7, 8), (12, 9), (19, 8), (20, 9), (26, 9)]:   # moss
        c.set(x, y, P['leaf'][2]); c.set(x, y - 1, P['leaf'][3])
    c.thick_line(14, 21, 11, 24, P['wood'][1], 2)                       # a broken branch
    c.outline()
    return c


def flower_arch(leaf=None, blossoms=None):
    """The gray thorns of the forest lane, bloomed into an arch once spring comes."""
    leaf = leaf or P['leaf']
    blossoms = blossoms or (BLOSSOM, P['white'], P['sun'])
    c = Canvas(56, 52)
    c.shadow(28, 49, 25, 3)
    for (x0, y0, x1, y1) in [(6, 50, 6, 18), (50, 50, 50, 18)]:
        c.thick_line(x0, y0, x1, y1, P['wood'][2], 3)
    for t in range(0, 41):
        x = 6 + t * 44 / 40
        y = 18 - 12 * (1 - ((t - 20) / 20) ** 2)
        c.thick_line(x, y, x, y + 2, P['wood'][2], 2)
    for k in range(40):
        x = [5, 7, 51, 49][k % 4] + (k % 3) - 1 if k < 20 else 8 + (k * 11) % 40
        y = 46 - (k * 7) % 32 if k < 20 else 6 + (k * 5) % 12
        c.ellipse(x, y, 2.5, 2, leaf)
        if k % 2 == 0:
            col = blossoms[k % 3]
            c.set(x, y - 1, col[3]); c.set(x + 1, y - 1, col[2])
    c.outline()
    return c


def ground_item(kind):
    """Small (16 px) forage you find on the forest floor."""
    c = Canvas(16, 16)
    c.shadow(8, 14, 5, 1.2)
    if kind == 'wild_garlic':
        for (x0, x1) in [(5, 2), (8, 8), (11, 14)]:
            c.thick_line(x0 + 1, 14, x1 + 1, 4, P['leaf'][2], 2)
        for (x, y) in [(3, 4), (9, 3), (14, 5)]:
            for dx, dy in ((0, -1), (-1, 0), (1, 0), (0, 1)):
                c.set(x + dx, y + dy, P['white'][3])
            c.set(x, y, P['yellow'][3])
    elif kind == 'fern_sprout':
        for (ox, h) in [(4, 9), (9, 11)]:
            c.thick_line(ox, 14, ox, 14 - h, P['leaf2'][2], 2)
            c.ellipse(ox + 2, 14 - h, 2.5, 2.5, P['leaf2'])
            c.set(ox + 2, 14 - h, P['leaf2'][0])
    elif kind == 'morel':
        c.rect(6, 10, 9, 14, P['apron'][2])
        c.ellipse(7.5, 7, 4, 5, P['tan'])
        for (x, y) in [(6, 5), (8, 7), (6, 9), (9, 4), (9, 9)]:
            c.set(x, y, P['tan'][0])
    elif kind == 'acorn':
        c.ellipse(8, 10, 4, 4.5, P['tan'])
        c.ellipse(8, 6.5, 4.5, 2.5, P['wood'])
        c.rect(8, 2, 8, 4, P['wood'][1])
        c.set(6, 9, P['tan'][3])
    c.outline()
    return c


# ----------------------------------------------------------------- item icons (32 px)
def _icon(kind, scale_y=0):
    c = Canvas(32, 32)
    small = ground_item(kind)
    for y in range(16):
        for x in range(16):
            p = small.get(x, y)
            if p is not None:
                c.rect(x * 2, y * 2, x * 2 + 1, y * 2 + 1, p)
    return c


def acorn_icon():
    c = Canvas(32, 32)
    c.ellipse(16, 20, 8, 9, P['tan'])
    c.ellipse(16, 12, 9.5, 5, P['wood'])
    for x in range(9, 24, 3):
        c.set(x, 11, P['wood'][3])
    c.thick_line(16, 3, 18, 7, P['wood'][1], 2)
    c.set(12, 18, P['tan'][3]); c.set(12, 19, P['tan'][3])
    c.outline()
    return c


def wild_garlic_icon():
    c = Canvas(32, 32)
    for (x0, x1, top) in [(12, 6, 8), (16, 16, 4), (20, 26, 9)]:
        c.thick_line(x0, 29, x1, top + 4, P['leaf'][2], 3)
        c.line(x0, 28, x1, top + 5, P['leaf'][3])
    c.ellipse(16, 27, 5, 3, P['white'])
    for (x, y) in [(6, 7), (16, 4), (26, 8), (11, 11)]:
        for dx, dy in ((0, -1), (-1, 0), (1, 0), (0, 1)):
            c.set(x + dx, y + dy, P['white'][3])
        c.set(x, y, P['yellow'][3])
    c.outline()
    return c


def fern_sprout_icon():
    c = Canvas(32, 32)
    for (ox, h, r) in [(10, 20, 4.5), (20, 16, 4)]:
        c.thick_line(ox, 29, ox + 2, 29 - h, P['leaf2'][2], 3)
        c.ellipse(ox + 5, 29 - h, r, r, P['leaf2'])
        c.ellipse(ox + 5, 29 - h, r - 2, r - 2, [P['leaf2'][1]] * 4, flat=True)
        c.set(ox + 5, 29 - h, P['leaf2'][3])
    c.outline()
    return c


def morel_icon():
    c = Canvas(32, 32)
    c.box(12, 19, 19, 29, P['apron'])
    c.ellipse(15.5, 13, 8, 10, P['tan'])
    for (x, y) in [(11, 9), (15, 7), (19, 10), (12, 14), (17, 15), (14, 19), (19, 18), (10, 18)]:
        c.set(x, y, P['tan'][0]); c.set(x + 1, y, P['tan'][1])
    c.outline()
    return c


def mattock():
    """Pick on one side, adze blade on the other: digs out stumps, boulders and fallen logs."""
    c = Canvas(32, 32)
    c.thick_line(8, 29, 20, 7, P['woodlight'][1], 3)
    c.line(8, 28, 19, 7, P['woodlight'][3])
    c.poly([(9, 6), (20, 3), (28, 5), (30, 9), (24, 7), (20, 9), (14, 9)], P['metal'], light_axis='y')
    c.line(20, 4, 27, 5, P['metal'][3])
    c.outline()
    return c


def spring_greens():
    import items
    c = Canvas(32, 32)
    items.plate(c)
    for (x, y, col) in [(10, 17, P['leaf']), (14, 15, P['leaf2']), (19, 16, P['leaf']), (22, 18, P['leaf2']), (16, 19, P['leaf'])]:
        c.ellipse(x, y, 3.5, 2.5, col)
    for (x, y) in [(12, 15), (18, 14), (21, 17)]:
        c.set(x, y, P['white'][3])
    c.outline()
    return c


def morel_soup():
    import items
    c = Canvas(32, 32)
    items.bowl(c, P['bread'])
    for (x, y) in [(11, 15), (16, 14), (21, 15)]:
        c.ellipse(x, y, 2, 1.5, P['tan']); c.set(x, y, P['tan'][0])
    c.set(14, 16, P['leaf'][2]); c.set(19, 16, P['leaf'][2])
    for x in (11, 17, 22):
        c.set(x, 9, (255, 255, 255, 140)); c.set(x + 1, 7, (255, 255, 255, 110)); c.set(x, 5, (255, 255, 255, 80))
    c.outline()
    return c


ITEMS = {
    'mattock': mattock,
    'acorn': acorn_icon,
    'wild_garlic': wild_garlic_icon,
    'fern_sprout': fern_sprout_icon,
    'morel': morel_icon,
    'spring_greens': spring_greens,
    'morel_soup': morel_soup,
}


def world_sprites():
    out = {
        'old_tree_sleep': old_tree(False),
        'old_tree_awake': old_tree(True),
        'forest_log': log(),
        'forest_log_winter': snow_cap(log()),
        'flower_arch': flower_arch(),
        'flower_arch_winter': snow_cap(flower_arch()),
    }
    out['sapling_spot'] = sapling(-1)
    for i in range(4):
        out[f'sapling_{i}'] = sapling(i)
    for k in ('wild_garlic', 'fern_sprout', 'morel', 'acorn'):
        out[f'{k}_ground'] = ground_item(k)
    out['forest_mask'] = mask()
    return out


def mask(radius=112):
    """Round mask for a restored clearing: solid inside, a dithered (ordered) edge, no soft alpha."""
    size = radius * 2 + 8
    c = Canvas(size, size)
    bayer = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]
    for y in range(size):
        for x in range(size):
            d = ((x + 0.5 - size / 2) ** 2 + (y + 0.5 - size / 2) ** 2) ** 0.5
            edge = 18
            if d <= radius - edge:
                c.set(x, y, (255, 255, 255, 255))
            elif d <= radius:
                t = (radius - d) / edge                 # 1 inside -> 0 at the rim
                if t * 16 > bayer[y % 4][x % 4]:
                    c.set(x, y, (255, 255, 255, 255))
    return c


# ----------------------------------------------------------------- the forest background
def forest(season='spring'):
    m = load_map()
    W, H, OX, OY = m['width'], m['height'], m['originX'], m['originY']
    rows = m['rows']
    bg = BG(W * 32, H * 32, W, H, (OX + W / 2, OY - H / 2))
    bg.season = season
    floor = FLOOR[season]
    clear = CLEARING[season]

    def at(c, r):
        return rows[r][c] if 0 <= r < H and 0 <= c < W else '#'

    def base(c, r):
        ch = at(c, r)
        if ch in 'f':
            return '-' if any(at(c + dx, r + dy) == '-' for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))) else '.'
        if ch == 'c':
            return '-'
        if ch == 'L':
            return '='
        return ch

    # ground: forest floor everywhere, lighter grass in clearings and around the old tree
    B.grass(bg, base=floor)
    # clearings: the tile mask, smoothed (bilinear between tile centres) so the edges are round, not stepped
    M = [[1.0 if base(c, r) == '-' else 0.0 for c in range(W)] for r in range(H)]

    def mval(c, r):
        return M[min(H - 1, max(0, r))][min(W - 1, max(0, c))]
    for y in range(H * 32):
        for x in range(W * 32):
            u, v = (x + 0.5) / 32 - 0.5, (y + 0.5) / 32 - 0.5
            c0, r0 = int(u // 1), int(v // 1)
            fu, fv = u - c0, v - r0
            val = (mval(c0, r0) * (1 - fu) * (1 - fv) + mval(c0 + 1, r0) * fu * (1 - fv)
                   + mval(c0, r0 + 1) * (1 - fu) * fv + mval(c0 + 1, r0 + 1) * fu * fv)
            if val > 0.5:
                bg.c.set(x, y, clear[2] if val > 0.62 else clear[1])
    # soften clearing edges with tufts, and leaf litter on the forest floor
    for r in range(H):
        for c in range(W):
            hv = _h(c, r, 3) % 10
            x, y = c * 32 + 6 + hv * 2, r * 32 + 8 + hv
            if base(c, r) == '-':
                B.tuft(bg, x, y, clear)
            elif base(c, r) in '.#' and hv < 4:
                col = (AUT_ORANGE if season == 'autumn' else floor)
                bg.c.set(x, y, col[1]); bg.c.set(x + 1, y, col[3]); bg.c.set(x + 3, y + 2, col[1])

    # paths (one mask so junctions don't get seams), the bridge planks go on top later
    rects = []
    for r in range(H):
        for c in range(W):
            if base(c, r) in '=b':
                # pull the path in from sides that aren't path, so floor shows along its edges
                inset = lambda dc, dr: 0 if base(c + dc, r + dr) in '=b' else 5
                rects.append((c * 32 + inset(-1, 0), r * 32 + inset(0, -1), c * 32 + 31 - inset(1, 0), r * 32 + 31 - inset(0, 1)))
    B.path(bg, rects, B.TRAMPLED if season == 'winter' else P['tan'])

    # the stream: a smooth meander through its tiles (banks, water, a few ripples), a plank bridge on the path
    water = P['water'] if season != 'winter' else B.ICE
    centers = []
    for r in range(H):
        cols = [c for c in range(W) if at(c, r) in '~b']
        centers.append(((min(cols) + max(cols) + 1) * 16) if cols else None)
    for y in range(H * 32):
        fr = (y + 0.5) / 32 - 0.5
        r0 = max(0, min(H - 1, int(fr // 1)))
        r1 = max(0, min(H - 1, r0 + 1))
        t = min(1, max(0, fr - r0))
        t = t * t * (3 - 2 * t)
        if centers[r0] is None or centers[r1] is None:
            continue
        cx = centers[r0] * (1 - t) + centers[r1] * t
        for x in range(int(cx - 30), int(cx + 31)):
            d = abs(x + 0.5 - cx)
            if d > 28:
                col = P['dirt'][1]
            elif d > 25:
                col = water[0]
            elif d > 19:
                col = water[1]
            else:
                col = water[2]
            bg.c.set(x, y, col)
        if _h(y // 9, 7) % 5 == 0 and y % 9 == 0:
            ox = int(cx - 12 + _h(y, 3) % 18)
            bg.c.line(ox, y, ox + 5, y, water[3])
    for r in range(H):
        for c in range(W):
            if at(c, r) == 'b':                               # wooden bridge planks across the path
                for y in range(r * 32 - 2, r * 32 + 34):
                    for x in range(c * 32, c * 32 + 32):
                        k = (x - c * 32) % 8
                        bg.c.set(x, y, P['woodlight'][1] if k == 7 else P['woodlight'][2] if k else P['woodlight'][3])
    # the spring: a small round pool
    ws = [(c, r) for r in range(H) for c in range(W) if at(c, r) == 'w']
    if ws:
        px = (min(c for c, _ in ws) + max(c for c, _ in ws) + 1) * 16
        py = (min(r for _, r in ws) + max(r for _, r in ws) + 1) * 16
        B.pond(bg, px, py, 26, 20, frozen=(season == 'winter'))
    # stones around the spring
    for (c, r) in [(16, 10), (17, 10), (18, 11), (18, 12), (16, 13), (17, 13), (15, 11)]:
        x, y = c * 32 + 16, r * 32 + 24
        if at(c, r) not in '~bw':
            pass

    cx, cy = 13 * 32 + 16, 9 * 32 + 30
    if season != 'winter':
        mushroom_ring(bg, cx + 40, cy + 34)

    # seat by the spring
    for r in range(H):
        for c in range(W):
            if at(c, r) == 's':
                bg.prop(seat(), x=c * 32 + 16, y=r * 32 + 28)

    # flowers in the clearings
    if season in ('spring', 'summer'):
        cols = [P['jam'], P['white'], P['sun'], P['cloth']]
        k = 0
        for r in range(H):
            for c in range(W):
                if base(c, r) == '-' and _h(c, r, 9) % 3 == 0:
                    k += 1
                    B.flower_cluster(bg, c * 32 + 8 + _h(c, r) % 14, r * 32 + 20, cols[k % 4], 3)
    elif season == 'autumn':
        for r in range(H):
            for c in range(W):
                if base(c, r) in '-.' and _h(c, r, 9) % 4 == 0:
                    B.leaf_pile(bg, c * 32 + 10, r * 32 + 20, 6)

    # trees and bushes (props, sorted by their base)
    for r in range(H):
        for c in range(W):
            ch = at(c, r)
            jx = _h(c, r, 1) % 9 - 4
            if ch == '#':
                v = _h(c, r, 2) % 5
                t = pine() if v < 2 else oak(season, v)
                bg.prop(t, x=c * 32 + 16 + jx, y=r * 32 + 31 + (_h(c, r, 4) % 3))
            elif ch == 'B':
                bg.prop(bush(season), x=c * 32 + 16, y=r * 32 + 28)
    return bg.image()


def gray(img, darker=0.86):
    """The 'forgotten' forest: every colour mapped to a cool deep gray (same pixels, no noise)."""
    out = Image.new('RGBA', img.size)
    src = img.load()
    dst = out.load()
    cache = {}
    for y in range(img.height):
        for x in range(img.width):
            p = src[x, y]
            if p not in cache:
                r, g, b, a = p
                lum = (0.3 * r + 0.59 * g + 0.11 * b) * darker
                cache[p] = (int(lum * 0.94 + r * 0.06), int(lum * 0.95 + g * 0.05), int(min(255, lum * 1.0 + 8)), a)
            dst[x, y] = cache[p]
    return out
