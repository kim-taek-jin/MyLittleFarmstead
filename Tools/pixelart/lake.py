"""The lake (map stage 4-1), drawn from Assets/Resources/Maps/lake.json.

Like the forest: a colour version and a gray version per season. The game shows the gray lake and lets
colour back through round masks over each zone you clear by releasing fish, and everywhere once the lake
is clear (then fireflies come out on summer nights).
"""
import json
import os

import backgrounds as B
from backgrounds import BG, SNOW, SUMMER, AUTUMN_GRASS, AUT_ORANGE, AUT_RED, AUT_YELLOW, ICE, snow_cap
from pixel import Canvas, P, ramp
import forest
import items

ROOT = os.path.join(os.path.dirname(__file__), '..', '..')
MAP_PATH = os.path.join(ROOT, 'Assets', 'Resources', 'Maps', 'lake.json')
SAND = ramp('#7a5a34', '#b8925a', '#dcbc82', '#f2dcaa')
GRASS = {'spring': P['leaf'], 'summer': SUMMER, 'autumn': AUTUMN_GRASS, 'winter': SNOW}


def load_map():
    with open(MAP_PATH) as f:
        return json.load(f)


_h = forest._h


# ----------------------------------------------------------------- props
def reeds(season, n=5, gray=False):
    r = ramp('#4e4a40', '#6e6a5e', '#928e82', '#bab6aa') if gray else \
        {'autumn': AUTUMN_GRASS, 'winter': ramp('#5e5a40', '#8a8462', '#b2aa84', '#d8d2ae')}.get(season, P['leaf2'])
    c = Canvas(20, 26)
    for k in range(n):
        x = 3 + k * 3 + (k % 2)
        h = 14 + (k * 5) % 9
        c.line(x, 25, x + (1 if k % 2 else -1), 25 - h, r[1] if k % 2 else r[2])
        if k % 2 == 0:
            c.rect(x - 1, 25 - h, x, 25 - h + 4, P['wood'][1] if not gray else r[0])   # cattail heads
    c.outline()
    return c


def willow(season):
    """A weeping willow: a round crown with strands hanging down to the water."""
    r = {'autumn': AUT_YELLOW, 'winter': None}.get(season, P['leaf'])
    c = Canvas(72, 80)
    c.shadow(36, 77, 26, 3)
    c.box(32, 44, 39, 77, P['wood'])
    c.thick_line(33, 76, 27, 79, P['wood'][1], 2); c.thick_line(38, 76, 44, 79, P['wood'][1], 2)
    if r is None:
        for (x0, y0, x1, y1) in [(35, 46, 14, 22), (36, 46, 58, 20), (35, 44, 36, 10)]:
            c.thick_line(x0, y0, x1, y1, P['wood'][1], 3)
        for x in range(10, 64, 5):
            c.line(x, 22 + (x % 3), x + 1, 50 + (x * 7) % 12, P['wood'][2])
        c.outline()
        return c
    c.ellipse(36, 26, 30, 20, r)
    for x in range(8, 66, 3):                          # hanging strands
        top = 26 + int(((x - 36) / 30) ** 2 * 14)
        length = 24 + (x * 13) % 18
        c.line(x, top, x + (1 if x % 2 else 0), top + length, r[1] if x % 2 else r[2])
        c.set(x, top + length, r[3])
    c.outline()
    return c


def bench():
    c = Canvas(30, 20)
    c.shadow(15, 18, 13, 2)
    for x in (4, 24):
        c.box(x, 9, x + 2, 18, P['wood'])
    c.box(1, 8, 28, 11, P['woodlight'])
    c.box(2, 1, 27, 5, P['woodlight'])
    c.rect(4, 5, 5, 8, P['wood'][1]); c.rect(24, 5, 25, 8, P['wood'][1])
    c.outline()
    return c


def steps():
    """Flat stones stepping down into the water, where you let fish go."""
    c = Canvas(30, 26)
    for (cx, cy, rx) in [(8, 6, 7), (17, 12, 7.5), (10, 19, 7)]:
        c.ellipse(cx, cy, rx, 3.6, P['stone'])
        c.line(cx - rx + 2, cy - 2, cx + rx - 3, cy - 2, P['stone'][3])
    c.outline()
    return c


# ----------------------------------------------------------------- runtime sprites
def reed_gate(open_):
    """The lake lane on the farm: tall gray reeds, or green reeds parted around a path."""
    c = Canvas(56, 52)
    c.shadow(28, 49, 25, 3)
    gray = ramp('#4e4a40', '#6e6a5e', '#928e82', '#bab6aa')
    r = P['leaf2'] if open_ else gray
    xs = [3, 6, 9, 12, 15, 18, 40, 43, 46, 49, 52] if open_ else list(range(3, 54, 3))
    for k, x in enumerate(xs):
        h = 30 + (k * 11) % 16
        c.thick_line(x, 50, x + (1 if k % 2 else -1), 50 - h, r[1] if k % 2 else r[2], 2)
        if k % 2 == 0:
            c.rect(x - 1, 50 - h, x + 1, 50 - h + 6, P['wood'][1] if open_ else gray[0])
    if open_:
        for (x, y) in [(8, 20), (46, 18), (14, 26), (50, 28)]:      # a few flowers in the reeds
            c.set(x, y, P['sun'][3]); c.set(x + 1, y, P['sun'][2])
    c.outline()
    return c


def firefly():
    """A firefly: a bright core with a small halo (pixel steps of transparency, no blur)."""
    c = Canvas(9, 9)
    for (x, y) in [(4, 1), (1, 4), (7, 4), (4, 7)]:
        c.set(x, y, (200, 255, 120, 60))
    for (x, y) in [(3, 3), (5, 3), (3, 5), (5, 5), (4, 2), (2, 4), (6, 4), (4, 6)]:
        c.set(x, y, (210, 255, 130, 120))
    for (x, y) in [(4, 3), (3, 4), (5, 4), (4, 5)]:
        c.set(x, y, (230, 255, 150, 230))
    c.set(4, 4, (255, 255, 220, 255))
    return c


def ground_item(kind):
    c = Canvas(16, 16)
    c.shadow(8, 14, 5, 1.2)
    if kind == 'wild_raspberry':
        for (x0, x1) in [(6, 3), (10, 13)]:
            c.line(x0, 14, x1, 6, P['leaf'][1])
        for (cx, cy) in [(4, 6), (8, 8), (12, 6)]:
            c.ellipse(cx, cy, 2.2, 2.2, P['red'])
            c.set(cx - 1, cy - 1, P['red'][3])
    elif kind == 'watercress':
        for (cx, cy) in [(5, 9), (9, 6), (11, 10), (7, 12)]:
            c.ellipse(cx, cy, 2.6, 2, P['leaf'])
        c.set(9, 5, P['white'][3]); c.set(5, 8, P['white'][3])
    c.outline()
    return c


# ----------------------------------------------------------------- item icons (32 px)
def bluegill():
    c = Canvas(32, 32)
    items._fish(c, 14, 17, 18, 12, P['cloth'], P['denim'], belly=P['sun'][2])
    for y in (13, 16, 19):
        c.set(11, y, P['denim'][0]); c.set(15, y, P['denim'][0])
    c.outline()
    return c


def moonfin():
    c = Canvas(32, 32)
    for (r, a) in [(12, 40)]:
        for y in range(32):
            for x in range(32):
                if (x - 15.5) ** 2 + (y - 16.5) ** 2 <= r * r:
                    c.set(x, y, (180, 200, 255, a))
    items._fish(c, 14, 17, 18, 8, ramp('#2a2a5a', '#4a4a8e', '#8a8ad0', '#dcdcff'), P['white'], belly=P['white'][3])
    c.set(18, 14, P['white'][3]); c.set(19, 15, P['white'][3])
    c.outline()
    return c


def glassfish():
    """Nearly see-through: you only find it once the lake runs clear."""
    c = Canvas(32, 32)
    body = [(200, 236, 255, 150), (210, 242, 255, 170), (226, 248, 255, 190), (255, 255, 255, 220)]
    items._fish(c, 14, 17, 18, 9, body, body, belly=None)
    c.line(9, 17, 21, 17, (150, 190, 220, 230))                   # its spine shows through
    for x in range(11, 21, 2):
        c.set(x, 16, (150, 190, 220, 200)); c.set(x, 18, (150, 190, 220, 200))
    c.outline(darken=0.6)
    return c


def raspberry_icon():
    c = Canvas(32, 32)
    for (cx, cy) in [(12, 18), (19, 17), (15, 23), (16, 13)]:
        c.ellipse(cx, cy, 4.5, 4.5, P['red'])
        c.set(cx - 2, cy - 2, P['red'][3])
    c.ellipse(16, 8, 5, 2.5, P['leaf'])
    c.outline()
    return c


def watercress_icon():
    c = Canvas(32, 32)
    for k, (cx, cy) in enumerate([(10, 14), (17, 10), (22, 15), (14, 20), (20, 22)]):
        c.ellipse(cx, cy, 5, 4, P['leaf'])
        c.set(cx - 1, cy - 1, P['leaf'][3])
    c.thick_line(16, 29, 16, 22, P['leaf2'][1], 2)
    c.outline()
    return c


ITEMS = {
    'bluegill': bluegill,
    'moonfin': moonfin,
    'glassfish': glassfish,
    'wild_raspberry': raspberry_icon,
    'watercress': watercress_icon,
}


def world_sprites():
    out = {
        'reeds_gray': reed_gate(False),
        'reeds_gray_winter': snow_cap(reed_gate(False)),
        'reeds_open': reed_gate(True),
        'reeds_open_winter': snow_cap(reed_gate(True)),
        'firefly': firefly(),
        'lake_mask_s': forest.mask(150),
        'lake_mask_l': forest.mask(230),
    }
    for k in ('wild_raspberry', 'watercress'):
        out[f'{k}_ground'] = ground_item(k)
    return out


# ----------------------------------------------------------------- the lake background
def lake(season='summer'):
    m = load_map()
    W, H, OX, OY = m['width'], m['height'], m['originX'], m['originY']
    rows = m['rows']
    bg = BG(W * 32, H * 32, W, H, (OX + W / 2, OY - H / 2))
    bg.season = season
    grass = GRASS[season]

    def at(c, r):
        return rows[r][c] if 0 <= r < H and 0 <= c < W else '#'

    def is_water(c, r):
        ch = at(c, r)
        if ch in '~p':
            return True
        if ch == 'F':
            return sum(at(c + dx, r + dy) in '~p' for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))) >= 3
        return False

    def is_sand(c, r):
        return at(c, r) in ',RXF' and not is_water(c, r)

    def smooth(test):
        """Bilinear blend of a tile mask (round edges instead of steps)."""
        M = [[1.0 if test(c, r) else 0.0 for c in range(W)] for r in range(H)]

        def mv(c, r):
            return M[min(H - 1, max(0, r))][min(W - 1, max(0, c))]

        def val(x, y):
            u, v = (x + 0.5) / 32 - 0.5, (y + 0.5) / 32 - 0.5
            c0, r0 = int(u // 1), int(v // 1)
            fu, fv = u - c0, v - r0
            return (mv(c0, r0) * (1 - fu) * (1 - fv) + mv(c0 + 1, r0) * fu * (1 - fv)
                    + mv(c0, r0 + 1) * (1 - fu) * fv + mv(c0 + 1, r0 + 1) * fu * fv)
        return val

    B.grass(bg, base=grass)
    sand_v = smooth(lambda c, r: is_sand(c, r) or is_water(c, r))
    water_v = smooth(is_water)
    # depth: tiles far from the shore are deeper
    dist = {}
    frontier = [(c, r) for r in range(H) for c in range(W) if not is_water(c, r)]
    for (c, r) in frontier:
        dist[(c, r)] = 0
    k = 0
    while k < len(frontier):
        c, r = frontier[k]; k += 1
        for dc, dr in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            q = (c + dc, r + dr)
            if 0 <= q[0] < W and 0 <= q[1] < H and q not in dist:
                dist[q] = dist[(c, r)] + 1
                frontier.append(q)
    deep_v = smooth(lambda c, r: dist.get((c, r), 0) >= 3)       # smooth contour for the deep middle
    deep = [P['water'][0], (36, 82, 140, 255), P['water'][1], P['water'][2]]
    sand = SAND if season != 'winter' else ramp('#8a96a8', '#b4c0d0', '#dce4ee', '#f6f9fc')
    water = P['water'] if season != 'winter' else ICE
    for y in range(H * 32):
        for x in range(W * 32):
            sv = sand_v(x, y)
            if sv <= 0.5:
                continue
            wv = water_v(x, y)
            if wv > 0.5:
                col = water[2] if wv < 0.64 else water[1]
                if season != 'winter' and deep_v(x, y) > 0.5:
                    col = deep[1]
                if wv < 0.56:
                    col = water[3] if season != 'winter' else water[2]          # foam / thin ice at the edge
            else:
                col = sand[2] if sv > 0.6 else sand[1]
                if (x * 7 + y * 13) % 53 == 0:
                    col = sand[3]
            bg.c.set(x, y, col)
    # ripples and sparkles on open water, cracks on ice
    for r in range(H):
        for c in range(W):
            if not is_water(c, r) or at(c, r) == 'p':
                continue
            hv = _h(c, r, 5) % 7
            x, y = c * 32 + 4 + hv * 3, r * 32 + 8 + hv * 2
            if season == 'winter':
                if hv < 2:
                    bg.c.line(x, y, x + 6, y + 3, ICE[0])
            elif hv < 3:
                bg.c.line(x, y, x + 5, y, water[3])
    # lily pads in the coves (not in winter)
    if season != 'winter':
        for (c, r) in [(9, 12), (11, 13), (22, 5), (24, 6), (14, 12), (21, 8)]:
            if is_water(c, r):
                lp = Canvas(11, 7)
                lp.ellipse(5.5, 3.5, 5, 3, P['leaf'])
                lp.set(5, 1, None); lp.set(6, 2, None)
                if (c + r) % 2:
                    lp.set(8, 2, P['jam'][3]); lp.set(8, 3, P['jam'][2])
                bg.prop(lp, x=c * 32 + 16, y=r * 32 + 22)

    # paths (inset from non-path sides, like the forest)
    rects = []
    for r in range(H):
        for c in range(W):
            if at(c, r) == '=':
                inset = lambda dc, dr: 0 if at(c + dc, r + dr) == '=' else 5
                rects.append((c * 32 + inset(-1, 0), r * 32 + inset(0, -1), c * 32 + 31 - inset(1, 0), r * 32 + 31 - inset(0, 1)))
    B.path(bg, rects, B.TRAMPLED if season == 'winter' else P['tan'])

    # the pier: planks across, posts at the sides
    for r in range(H):
        for c in range(W):
            if at(c, r) == 'p' or (at(c, r) == 'F' and is_water(c, r)):
                for y in range(r * 32, r * 32 + 32):
                    for x in range(c * 32 + 3, c * 32 + 29):
                        k2 = (y - r * 32) % 8
                        bg.c.set(x, y, P['woodlight'][1] if k2 == 7 else P['woodlight'][2] if k2 else P['woodlight'][3])
                for x in (c * 32 + 2, c * 32 + 28):
                    bg.c.rect(x, r * 32 + 26, x + 2, r * 32 + 31, P['wood'][1])

    # props: trees, bushes, reeds, the willow and its bench, the stepping stones
    for r in range(H):
        for c in range(W):
            ch = at(c, r)
            jx = _h(c, r, 1) % 9 - 4
            if ch == '#':
                v = _h(c, r, 2) % 5
                t = forest.pine() if v < 1 else forest.oak(season, v)
                bg.prop(t, x=c * 32 + 16 + jx, y=r * 32 + 31 + (_h(c, r, 4) % 3))
            elif ch == 'B':
                bg.prop(forest.bush(season), x=c * 32 + 16, y=r * 32 + 28)
            elif ch == 'R':
                bg.prop(reeds(season), x=c * 32 + 12, y=r * 32 + 30)
                bg.prop(reeds(season, 4), x=c * 32 + 24, y=r * 32 + 26)
            elif ch == 'W':
                bg.prop(willow(season), x=c * 32 + 16, y=r * 32 + 31)
            elif ch == 'h':
                bg.prop(bench(), x=c * 32 + 16, y=r * 32 + 28)
            elif ch == 'X':
                bg.prop(steps(), x=c * 32 + 16, y=r * 32 + 30)

    # flowers and seasonal details on grass
    for r in range(H):
        for c in range(W):
            if at(c, r) not in '.f' or _h(c, r, 9) % 3:
                continue
            x, y = c * 32 + 8 + _h(c, r) % 14, r * 32 + 20
            if season in ('spring', 'summer'):
                B.flower_cluster(bg, x, y, [P['sun'], P['white'], P['cloth'], P['jam']][_h(c, r, 3) % 4], 3)
            elif season == 'autumn':
                B.leaf_pile(bg, x, y, 6)
    return bg.image()
