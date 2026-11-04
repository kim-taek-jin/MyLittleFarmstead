"""Sunset Hill (map stage 4-2), drawn from Assets/Resources/Maps/hill.json, plus the overlook panorama.

Like the forest and the lake: a colour and a gray version per season. Each old fruit tree you bring back
lets colour in around it (round mask); all six colour the whole orchard.

The panorama is what you see from the overlook bench: a sunset over the whole valley. The sky is always
in colour; the land is gray except the places you have brought back (farm, forest, lake, village, orchard),
which are separate layers the game puts on top.
"""
import json
import os

import backgrounds as B
from backgrounds import BG, SNOW, SUMMER, AUTUMN_GRASS, AUT_ORANGE, AUT_RED, AUT_YELLOW, snow_cap
from pixel import Canvas, P, ramp
from PIL import Image
import forest
import items

ROOT = os.path.join(os.path.dirname(__file__), '..', '..')
MAP_PATH = os.path.join(ROOT, 'Assets', 'Resources', 'Maps', 'hill.json')
GRASS = {'spring': P['leaf'], 'summer': SUMMER, 'autumn': AUTUMN_GRASS, 'winter': SNOW}
CLIFF = ramp('#3a2a20', '#5e4430', '#806048', '#a88466')
_h = forest._h


def load_map():
    with open(MAP_PATH) as f:
        return json.load(f)


# ----------------------------------------------------------------- props
def railing(length):
    c = Canvas(length, 18)
    for x in range(1, length - 1, 10):
        c.box(x, 2, x + 2, 17, P['wood'])
    c.rect(0, 4, length - 1, 6, P['woodlight'][2])
    c.line(0, 4, length - 1, 4, P['woodlight'][3])
    c.rect(0, 10, length - 1, 11, P['woodlight'][1])
    c.outline()
    return c


def overlook_bench():
    c = Canvas(60, 22)
    c.shadow(30, 20, 27, 2)
    for x in (6, 52):
        c.box(x, 11, x + 2, 20, P['wood'])
    c.box(2, 10, 57, 13, P['woodlight'])
    c.box(3, 2, 56, 6, P['woodlight'])
    for x in (8, 51):
        c.rect(x, 6, x + 1, 10, P['wood'][1])
    c.outline()
    return c


# ----------------------------------------------------------------- runtime sprites: orchard trees and the rack
def _trunk(c, bark):
    c.shadow(24, 53, 16, 3)
    c.poly([(19, 54), (21, 30), (27, 30), (29, 54)], bark, light_axis='x')
    c.thick_line(21, 53, 15, 56, bark[1], 2); c.thick_line(27, 53, 33, 56, bark[1], 2)
    for (x0, y0, x1, y1) in [(22, 32, 10, 18), (26, 32, 38, 16), (24, 30, 24, 8)]:
        c.thick_line(x0, y0, x1, y1, bark[2], 3)


def fruit_tree(kind, state, season='autumn'):
    """state: 'gray' (old and dry), 'pruned' (gray, cut back), 'bare' (alive, no fruit), 'fruit' (alive with fruit)."""
    c = Canvas(48, 58)
    gray = state in ('gray', 'pruned')
    bark = ramp('#2e2e34', '#4a4a52', '#6c6c76', '#90909a') if gray else P['wood']
    _trunk(c, bark)
    if state == 'gray':
        for (x0, y0, x1, y1) in [(10, 18, 4, 10), (10, 18, 14, 6), (38, 16, 44, 8), (38, 16, 32, 4), (24, 8, 18, 2), (24, 8, 30, 1)]:
            c.line(x0, y0, x1, y1, bark[1]); c.line(x0 + 1, y0, x1 + 1, y1, bark[2])     # thin twigs, two pixels
        for (x, y) in [(5, 9), (15, 5), (43, 8), (31, 4), (19, 2)]:
            c.set(x, y, bark[3])
    elif state == 'pruned':
        for (x, y) in [(10, 18), (38, 16), (24, 8)]:
            c.ellipse(x, y, 2, 1.5, P['tan'], flat=True)          # fresh cut ends
    else:
        leaf = {'winter': None, 'autumn': AUT_ORANGE if kind == 'persimmon' else AUT_YELLOW if kind == 'pear' else P['leaf2']}.get(season, P['leaf'])
        if leaf is not None:
            for (cx, cy, rx, ry) in [(12, 18, 11, 9), (36, 17, 11, 9), (24, 11, 15, 10), (24, 22, 13, 6)]:
                c.ellipse(cx, cy, rx, ry, leaf)
        if state == 'fruit' and season != 'winter':
            fruit = {'apple': P['red'], 'pear': ramp('#6a6a1a', '#b8b03a', '#e2dc6a', '#fbf7b0'), 'persimmon': P['orange']}[kind]
            for (x, y) in [(10, 20), (17, 13), (24, 21), (31, 12), (37, 20), (21, 7), (29, 24), (14, 25)]:
                c.ellipse(x, y, 2.2, 2.2, fruit)
                c.set(x - 1, y - 1, fruit[3])
    c.outline()
    return c


def drying_rack(state):
    """'empty', 'hung' (fresh persimmons), 'dried' (dark, wrinkled, ready)."""
    c = Canvas(40, 42)
    c.shadow(20, 40, 18, 2)
    for x in (3, 35):
        c.box(x, 6, x + 2, 40, P['wood'])
    c.box(1, 4, 38, 7, P['woodlight'])
    if state != 'empty':
        fruit = P['orange'] if state == 'hung' else ramp('#4a1e08', '#7a3610', '#a8521e', '#cc7a3c')
        for k, x in enumerate(range(9, 33, 5)):
            c.line(x, 8, x, 34, P['tan'][1])
            for y in range(12, 34, 6):
                c.ellipse(x, y, 2, 2.2 if state == 'hung' else 1.8, fruit)
                c.set(x, y - 2, P['leaf2'][1])
    c.outline()
    return c


# ----------------------------------------------------------------- item icons
def pear():
    c = Canvas(32, 32)
    r = ramp('#5a5a14', '#a8a034', '#dcd664', '#fbf7b0')
    c.ellipse(16, 21, 8, 8, r)
    c.ellipse(16, 12, 5, 6, r)
    c.line(16, 6, 17, 3, P['wood'][1]); c.set(18, 4, P['leaf'][2]); c.set(19, 4, P['leaf'][3])
    c.set(12, 17, r[3]); c.set(12, 18, r[3])
    c.outline()
    return c


def persimmon():
    c = Canvas(32, 32)
    c.ellipse(16, 18, 10, 8.5, P['orange'])
    c.poly([(9, 11), (16, 8), (23, 11), (16, 13)], P['leaf2'], light_axis='y')
    c.rect(15, 6, 16, 9, P['wood'][1])
    c.set(11, 15, P['orange'][3]); c.set(12, 14, P['orange'][3])
    c.outline()
    return c


def dried_persimmon():
    c = Canvas(32, 32)
    r = ramp('#4a1e08', '#7a3610', '#a8521e', '#cc7a3c')
    for (cx, cy) in [(11, 19), (21, 19)]:
        c.ellipse(cx, cy, 6.5, 6, r)
        c.line(cx - 3, cy - 1, cx + 2, cy + 2, r[0]); c.line(cx + 2, cy - 3, cx - 1, cy + 3, r[0])
        c.set(cx - 3, cy - 3, (255, 255, 255, 160))                 # a little sugar bloom
    c.thick_line(11, 13, 16, 7, P['tan'][1], 1); c.thick_line(21, 13, 16, 7, P['tan'][1], 1)
    c.outline()
    return c


def maple_arch():
    """The hill lane's gray thorns, turned into an arch of autumn leaves."""
    return forest.flower_arch(leaf=AUT_ORANGE, blossoms=(AUT_RED, AUT_YELLOW, P['orange']))


ITEMS = {'pear': pear, 'persimmon': persimmon, 'dried_persimmon': dried_persimmon}


def world_sprites():
    out = {}
    for kind in ('apple', 'pear', 'persimmon'):
        out[f'orchard_{kind}_fruit'] = fruit_tree(kind, 'fruit')
        out[f'orchard_{kind}_bare'] = fruit_tree(kind, 'bare')
        out[f'orchard_{kind}_bare_spring'] = fruit_tree(kind, 'bare', 'spring')
        out[f'orchard_{kind}_fruit_spring'] = fruit_tree(kind, 'fruit', 'spring')
        out[f'orchard_{kind}_bare_summer'] = fruit_tree(kind, 'bare', 'summer')
        out[f'orchard_{kind}_fruit_summer'] = fruit_tree(kind, 'fruit', 'summer')
        out[f'orchard_{kind}_bare_winter'] = snow_cap(fruit_tree(kind, 'bare', 'winter'))
    out['orchard_gray'] = fruit_tree('apple', 'gray')
    out['orchard_gray_winter'] = snow_cap(fruit_tree('apple', 'gray'))
    out['orchard_pruned'] = fruit_tree('apple', 'pruned')
    out['orchard_pruned_winter'] = snow_cap(fruit_tree('apple', 'pruned'))
    for st in ('empty', 'hung', 'dried'):
        out[f'drying_rack_{st}'] = drying_rack(st)
    out['maple_arch'] = maple_arch()
    out['maple_arch_winter'] = snow_cap(maple_arch())
    return out


# ----------------------------------------------------------------- the hill background
def hill(season='autumn'):
    m = load_map()
    W, H, OX, OY = m['width'], m['height'], m['originX'], m['originY']
    rows = m['rows']
    bg = BG(W * 32, H * 32, W, H, (OX + W / 2, OY - H / 2))
    bg.season = season
    grass = GRASS[season]

    def at(c, r):
        return rows[r][c] if 0 <= r < H and 0 <= c < W else '#'

    B.grass(bg, base=grass)
    # a second grass tone on each terrace, lighter towards the top (the hill catches the evening light)
    patch_col = {'winter': SNOW[1], 'spring': P['leaf2'][2], 'summer': SUMMER[1], 'autumn': AUT_YELLOW[1]}[season]
    for (wx, wy, rx, ry) in [(OX + 9, OY - 16.5, 70, 18), (OX + 20, OY - 12, 60, 16), (OX + 12, OY - 7, 60, 14)]:
        x, y = bg.px(wx, wy)
        B.patch(bg, x, y, rx, ry, patch_col, grass, grass)

    # paths
    rects = []
    for r in range(H):
        for c in range(W):
            if at(c, r) in '=':
                inset = lambda dc, dr: 0 if at(c + dc, r + dr) in '=S' else 5
                rects.append((c * 32 + inset(-1, 0), r * 32 + inset(0, -1), c * 32 + 31 - inset(1, 0), r * 32 + 31 - inset(0, 1)))
    B.path(bg, rects, B.TRAMPLED if season == 'winter' else P['tan'])

    # terrace cliffs: a grass lip on top, then an earth face with stones; stone stairs in the gaps
    for r in range(H):
        for c in range(W):
            ch = at(c, r)
            if ch == 'C':
                for y in range(r * 32, r * 32 + 32):
                    for x in range(c * 32, c * 32 + 32):
                        dy = y - r * 32
                        if dy < 4:
                            col = grass[1] if season != 'winter' else SNOW[3]
                        elif dy < 6:
                            col = CLIFF[0]
                        elif dy > 28:
                            col = CLIFF[0]
                        else:
                            col = CLIFF[2] if (x // 9 + dy // 7) % 4 else CLIFF[1]
                            if (x * 5 + y * 3) % 41 == 0:
                                col = CLIFF[3]
                        bg.c.set(x, y, col)
            elif ch == 'S':
                for y in range(r * 32, r * 32 + 32):
                    for x in range(c * 32, c * 32 + 32):
                        k = (y - r * 32) % 8
                        bg.c.set(x, y, P['stone'][1] if k == 7 else P['stone'][3] if k == 0 else P['stone'][2])

    # props
    for r in range(H):
        for c in range(W):
            ch = at(c, r)
            jx = _h(c, r, 1) % 9 - 4
            if ch == '#':
                v = _h(c, r, 2) % 5
                t = forest.pine() if v < 2 else forest.oak(season, v)
                bg.prop(t, x=c * 32 + 16 + jx, y=r * 32 + 31 + (_h(c, r, 4) % 3))
            elif ch == 'B':
                bg.prop(forest.bush(season), x=c * 32 + 16, y=r * 32 + 28)
    rc = [c for c in range(W) if at(c, 1) == 'R']
    if rc:
        bg.prop(railing((max(rc) - min(rc) + 1) * 32), x=(min(rc) + max(rc) + 1) * 16, y=1 * 32 + 26)
    vc = [c for c in range(W) if at(c, 2) == 'V']
    if vc:
        bg.prop(overlook_bench(), x=(min(vc) + max(vc) + 1) * 16, y=2 * 32 + 28)

    # flowers / leaves on the terraces
    for r in range(H):
        for c in range(W):
            if at(c, r) != '.' or _h(c, r, 9) % 4:
                continue
            x, y = c * 32 + 8 + _h(c, r) % 14, r * 32 + 20
            if season == 'autumn':
                B.leaf_pile(bg, x, y, 6)
            elif season in ('spring', 'summer'):
                B.flower_cluster(bg, x, y, [P['sun'], P['white'], P['jam']][_h(c, r, 3) % 3], 3)
    return bg.image()


# ----------------------------------------------------------------- the overlook panorama (640 x 360)
PW, PH = 640, 360
HORIZON = 150
SKY = [(58, 38, 92), (112, 52, 110), (186, 74, 100), (236, 120, 84), (252, 176, 92), (255, 222, 140)]
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def _sky():
    c = Canvas(PW, PH)
    bands = len(SKY)
    for y in range(HORIZON + 4):
        t = y / (HORIZON + 4) * (bands - 1)
        i = int(t)
        f = t - i
        for x in range(PW):
            col = SKY[min(bands - 1, i + 1)] if f * 16 > BAYER[y % 4][x % 4] else SKY[i]
            c.set(x, y, col + (255,))
    # the sun sinking behind the far hills
    for y in range(HORIZON - 40, HORIZON + 4):
        for x in range(420, 500):
            if (x - 460) ** 2 + (y - HORIZON + 6) ** 2 <= 34 ** 2:
                c.set(x, y, (255, 240, 190, 255) if (x - 460) ** 2 + (y - HORIZON + 6) ** 2 <= 26 ** 2 else (255, 214, 150, 255))
    # a few clouds lit from below
    for (cx, cy, w) in [(120, 40, 70), (300, 70, 90), (540, 36, 60)]:
        for x in range(cx - w // 2, cx + w // 2):
            for y in range(cy - 3, cy + 4):
                if abs(y - cy) * w / 6 + abs(x - cx) < w / 2:
                    c.set(x, y, (255, 190, 150, 255) if y > cy else (238, 150, 140, 255))
    return c


def _mountains():
    """Two ridges of soft hills, built from a few overlapping slopes (no saw-tooth)."""
    import math
    c = Canvas(PW, PH)
    far = (96, 70, 120, 255)
    near = (78, 60, 98, 255)
    for x in range(PW):
        h1 = 20 + 9 * math.sin(x / 61.0) + 6 * math.sin(x / 23.0 + 1.3) + 3 * math.sin(x / 9.0)
        for y in range(int(HORIZON - h1), HORIZON + 6):
            c.set(x, y, far)
        h2 = 9 + 6 * math.sin(x / 47.0 + 2.0) + 3 * math.sin(x / 17.0)
        for y in range(int(HORIZON - h2 + 6), HORIZON + 10):
            c.set(x, y, near)
    return c


def _city():
    """The gray city, small and far away on the left horizon."""
    c = Canvas(PW, PH)
    gray = [(84, 84, 96, 255), (104, 104, 116, 255)]
    x = 24
    k = 0
    while x < 150:
        w = 6 + (k * 5) % 7
        h = 14 + (k * 11) % 22
        for yy in range(HORIZON - h, HORIZON + 2):
            for xx in range(x, x + w):
                c.set(xx, yy, gray[k % 2])
        for yy in range(HORIZON - h + 3, HORIZON - 2, 4):
            for xx in range(x + 1, x + w - 1, 3):
                if (xx + yy + k) % 5 == 0:
                    c.set(xx, yy, (220, 210, 150, 255))
        x += w + 1
        k += 1
    return c


def _land_base():
    """Rolling fields: bands of grass whose edges follow gentle curves, with a few hedgerows."""
    import math
    c = Canvas(PW, PH)
    tones = [P['leaf'][2], P['leaf2'][2], P['leaf'][1], P['leaf2'][1]]
    for y in range(HORIZON + 6, PH):
        for x in range(PW):
            wave = 7 * math.sin(x / 53.0 + y / 90.0) + 4 * math.sin(x / 19.0 + 0.7)
            band = int((y - HORIZON + wave) / (14 + (y - HORIZON) * 0.12))
            col = tones[band % len(tones)]
            c.set(x, y, col)
    for k, (y0, amp) in enumerate([(HORIZON + 34, 5), (HORIZON + 70, 7), (HORIZON + 118, 6)]):   # hedgerows
        for x in range(PW):
            y = int(y0 + amp * math.sin(x / (37.0 + k * 11) + k))
            c.set(x, y, P['leaf'][0]); c.set(x, y - 1, P['leaf2'][0])
            if (x * 7 + k) % 23 == 0:
                c.ellipse(x, y - 2, 2.5, 2, P['leaf'])
    return c


def _forest():
    """A wood on the left: back rows smaller and darker, front rows bigger, no two crowns alike."""
    c = Canvas(PW, PH)
    rows = [(HORIZON + 20, 5, P['leaf2']), (HORIZON + 30, 6, P['leaf']), (HORIZON + 42, 7, P['leaf2']),
            (HORIZON + 55, 8, P['leaf']), (HORIZON + 70, 9, P['leaf'])]
    for ri, (y0, r, ramp_) in enumerate(rows):
        x = 4 + (ri * 7) % 11
        while x < 236 - ri * 6:
            k = forest._h(x, ri, 7)
            rr = r + (k % 3) - 1
            y = y0 + (k >> 3) % 5
            col = ramp_ if (k >> 5) % 5 else AUT_ORANGE
            c.ellipse(x, y, rr, rr * 0.85, col)
            x += int(rr * 1.4) + (k >> 7) % 4
    c.outline()
    return c


def _lake():
    c = Canvas(PW, PH)
    cx, cy, rx, ry = 520, HORIZON + 62, 100, 34
    for y in range(int(cy - ry - 4), int(cy + ry + 5)):
        for x in range(int(cx - rx - 4), int(cx + rx + 5)):
            d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
            if d <= 1:
                col = P['water'][1] if (y - cy) < -ry * 0.3 else P['water'][2]
                if (y % 6 == 0) and (x // 5) % 3 == 0 and abs(x - 460) < 60:
                    col = (255, 210, 150, 255)                 # the sunset on the water
                c.set(x, y, col)
            elif d <= 1.16:
                c.set(x, y, (220, 196, 140, 255))
    for (x, y) in [(470, HORIZON + 58), (560, HORIZON + 74), (500, HORIZON + 82)]:
        c.ellipse(x, y, 4, 2, P['leaf'])
    return c


def _village():
    c = Canvas(PW, PH)
    roofs = [P['shirt'], P['cloth'], P['copper'], P['jam'], P['shirt']]
    for i, x in enumerate(range(286, 400, 22)):
        y = HORIZON + 40 + (i % 2) * 6
        c.box(x, y, x + 16, y + 12, P['woodlight'])
        c.poly([(x - 2, y + 1), (x + 8, y - 8), (x + 18, y + 1)], roofs[i % len(roofs)], light_axis='y')
        c.set(x + 7, y + 6, P['sun'][3]); c.set(x + 8, y + 6, P['sun'][3])
    for x in range(300, 380, 10):                                   # market bunting
        c.set(x, HORIZON + 30, roofs[(x // 10) % 5][2]); c.set(x + 1, HORIZON + 31, roofs[(x // 10) % 5][2])
    c.outline()
    return c


def _farm():
    c = Canvas(PW, PH)
    # fields in rows, the cottage, the old field now cleared
    for k in range(6):
        y = HORIZON + 96 + k * 8
        for x in range(230, 330):
            c.set(x, y, P['dirt'][2]); c.set(x, y + 1, P['dirt'][1])
            if (x + k * 3) % 7 == 0:
                c.set(x, y - 1, P['leaf'][2])
    for y in range(HORIZON + 94, HORIZON + 144):                    # the old field, cleared and growing
        for x in range(350, 440):
            row = (y - HORIZON - 94) % 7
            if row < 4:
                c.set(x, y, P['dirt'][2] if row else P['dirt'][1])
            elif (x * 3 + y) % 5:
                c.set(x, y, P['leaf'][2] if (x + y) % 9 else P['leaf'][3])
    c.outline()
    return c


def _home():
    """Your cottage, on its own layer: it stays gray until the ending."""
    c = Canvas(PW, PH)
    x, y = 180, HORIZON + 112
    c.box(x, y, x + 26, y + 16, P['woodlight'])
    c.poly([(x - 3, y + 1), (x + 13, y - 11), (x + 29, y + 1)], P['shirt'], light_axis='y')
    c.set(x + 12, y + 10, P['wood'][1]); c.set(x + 12, y + 11, P['wood'][1])
    c.rect(x + 4, y + 4, x + 7, y + 7, P['sun'][3]); c.rect(x + 19, y + 4, x + 22, y + 7, P['sun'][3])
    c.outline()
    return c


def _orchard():
    """The hill you are standing on: orchard trees and the railing in the foreground."""
    c = Canvas(PW, PH)
    for x in range(PW):
        top = PH - 46 + int(6 * abs(((x * 3) % 200) - 100) / 100)
        for y in range(top, PH):
            c.set(x, y, AUTUMN_GRASS[1] if y < top + 3 else AUTUMN_GRASS[2])
    for i, x in enumerate([40, 130, 520, 600]):
        y = PH - 40
        c.box(x - 2, y - 12, x + 2, y + 4, P['wood'])
        r = [AUT_ORANGE, AUT_YELLOW, P['leaf2'], AUT_RED][i]
        c.ellipse(x, y - 22, 20, 14, r)
        fruit = [P['orange'], P['red'], P['sun'], P['orange']][i]
        for (dx, dy) in [(-8, -20), (6, -26), (10, -16), (-2, -14)]:
            c.ellipse(x + dx, y + dy, 1.8, 1.8, fruit)
    for x in range(0, PW, 18):                                      # the railing along the overlook
        c.box(x + 4, PH - 22, x + 6, PH - 4, P['wood'])
    c.rect(0, PH - 20, PW - 1, PH - 18, P['woodlight'][2])
    c.outline()
    return c


PANO_LAYERS = [('forest', _forest), ('lake', _lake), ('village', _village), ('farm', _farm), ('home', _home), ('orchard', _orchard)]


def panorama():
    """Returns {name: image}: 'pano_base' (colour sky + gray land) and one colour overlay per region,
    each containing only the pixels of that region that nothing in front of it covers."""
    sky = _sky().image()
    mountains = _mountains().image()
    city = _city().image()
    land = _land_base().image()
    layers = [(name, fn().image()) for name, fn in PANO_LAYERS]

    def gray_img(img):
        return forest.gray(img)

    base = sky.copy()
    base.alpha_composite(gray_img(mountains))
    base.alpha_composite(city)
    base.alpha_composite(gray_img(land))
    for _, im in layers:
        base.alpha_composite(gray_img(im))
    out = {'pano_base': base}
    # colour land underneath every region overlay (the grass around a place colours with it)
    for i, (name, im) in enumerate(layers):
        ov = Image.new('RGBA', (PW, PH), (0, 0, 0, 0))
        ov.alpha_composite(im)
        px = ov.load()
        front = [l for _, l in layers[i + 1:]]
        for y in range(PH):
            for x in range(PW):
                if px[x, y][3] and any(f.getpixel((x, y))[3] for f in front):
                    px[x, y] = (0, 0, 0, 0)
        out['pano_' + name] = ov
    out['pano_land'] = land        # coloured grass, shown once everything is back
    hills = mountains.copy()
    hills.alpha_composite(city)    # the city stays gray even when the hills are in colour
    out['pano_mountains'] = hills
    return out
