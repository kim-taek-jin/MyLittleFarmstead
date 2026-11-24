"""The home farm as a Korean countryside homestead (direction C), drawn from Assets/Resources/Maps/farm.json.

Exports (through generate.py):
  World/bg_farmmap_<season>   the whole farm (36 x 20 tiles), house drawn gray until the ending
  World/cottage_color[_winter] the same house in colour (laid over the gray one after the ending)
  World/plot_hard_<0..2>       gray, hardened crust over a plot cell (until compost restores the plot)
  World/plot_straw_<0..2>      straw laid over an empty ridge of a resting plot
  World/compost_heap           the compost heap
  Items/compost                a bag of compost
"""
import json
import math
import os

from PIL import ImageDraw

from pixel import Canvas, P, ramp, darken_c, lighten_c, seeded
import backgrounds as B
from backgrounds import BG, grass, patch, path, tuft, _h, SNOW, SUMMER, AUTUMN_GRASS, AUT_ORANGE, AUT_YELLOW, ICE
import kvillage as K

HERE = os.path.dirname(os.path.abspath(__file__))
MAP = os.path.join(HERE, '..', '..', 'Assets', 'Resources', 'Maps', 'farm.json')

WATER = P['water']
WINTER_GROUND = ramp('#8c98ac', '#c2ccda', '#e6ecf4', '#ffffff')
SPRING_PADDY = ramp('#2a4c6a', '#3f6e92', '#6c9cc0', '#b6d8ee')
SEEDLING = ramp('#2c5a24', '#4a8c34', '#7cbc4c', '#b8e27a')
SUMMER_RICE = ramp('#1c4a1e', '#2e7230', '#4c9a3c', '#8cc858')


def load_map():
    with open(MAP) as f:
        return json.load(f)


def ground_for(season):
    return {'winter': WINTER_GROUND, 'spring': P['leaf'], 'summer': SUMMER, 'autumn': AUTUMN_GRASS}[season]


# ---------------------------------------------------------------- props that change with the season
def persimmon(season):
    rnd = seeded(11)
    c = Canvas(76, 84)
    c.shadow(38, 82, 26, 3)
    c.poly([(33, 82), (36, 50), (41, 50), (44, 82)], P['wood'], light_axis='x')
    c.thick_line(37, 56, 24, 40, P['wood'][1], 3)
    c.thick_line(40, 54, 54, 38, P['wood'][1], 3)
    if season == 'winter':
        # bare branches with a few persimmons left for the magpies (kkachi-bap)
        for (x0, y0, x1, y1) in [(24, 40, 14, 26), (24, 40, 28, 18), (54, 38, 64, 24), (54, 38, 48, 16), (38, 50, 38, 12), (38, 30, 30, 20), (38, 26, 46, 14)]:
            c.line(x0, y0, x1, y1, P['wood'][1])
        for (x, y) in ((14, 26), (46, 14), (64, 24)):
            c.set(x, y + 1, P['orange'][2]); c.set(x + 1, y + 1, P['orange'][2]); c.set(x, y + 2, P['orange'][1]); c.set(x + 1, y + 2, P['orange'][1])
        c.outline()
        return c
    leaf = {
        'spring': ramp('#2f5a24', '#4f8a32', '#86c04c', '#c8e888'),
        'summer': ramp('#1c3c1a', '#2e5e26', '#4a8236', '#7fae52'),
        'autumn': ramp('#2a4a22', '#4d7a32', '#7fa646', '#c2cf68'),
    }[season]
    for (cx, cy, rx, ry) in [(38, 30, 30, 20), (20, 38, 16, 13), (56, 37, 17, 13), (30, 20, 16, 12), (48, 22, 15, 12)]:
        c.ellipse(cx, cy, rx, ry, leaf)
    for i in range(60):
        x, y = rnd.randint(6, 70), rnd.randint(6, 52)
        if c.get(x, y) is not None and c.get(x, y + 2) is None:
            c.set(x, y + 1, leaf[1])
    if season != 'spring':
        fruit = P['orange'] if season == 'autumn' else ramp('#3a5a1a', '#5f8a2a', '#8cb83c', '#c8e070')
        for i in range(26 if season == 'autumn' else 14):
            x, y = rnd.randint(10, 66), rnd.randint(10, 48)
            if c.get(x, y) is not None and c.get(x + 1, y + 1) is not None:
                c.set(x, y, fruit[2]); c.set(x + 1, y, fruit[2]); c.set(x, y + 1, fruit[1]); c.set(x + 1, y + 1, fruit[2]); c.set(x, y - 1, fruit[3])
    else:
        for i in range(18):                                   # pale persimmon blossoms
            x, y = rnd.randint(10, 66), rnd.randint(10, 48)
            if c.get(x, y) is not None:
                c.set(x, y, P['paper'][3])
    c.outline()
    return c


def house(season, gray=False):
    h = K.house()
    if season == 'winter':
        h.nosnow = False
    if gray:
        B.gray_canvas(h)
    return h


def cottage_color(season):
    c = K.house()
    if season == 'winter':
        B.snow_cap(c)
    return c


# ---------------------------------------------------------------- small sprites
def plot_hard(v):
    """A gray, hardened crust over one ridge cell: cracks, dry stubble, a pebble or two."""
    rnd = seeded(30 + v)
    c = Canvas(32, 32)
    crust = ramp('#4a4744', '#6e6a64', '#8f8a82', '#b2ada4')
    for y in range(2, 30):
        for x in range(1, 31):
            k = (y - 2) % 9
            col = crust[1] if k in (0, 1) else crust[3] if k == 2 else crust[2]
            if _h(x, y, v) % 23 == 0:
                col = crust[1]
            c.set(x, y, col)
    for i in range(3):                                           # cracks
        x, y = rnd.randint(4, 26), rnd.randint(4, 24)
        for t in range(rnd.randint(4, 8)):
            c.set(x, y, crust[0])
            x += rnd.choice((-1, 0, 1)); y += 1
    for i in range(4):                                           # dry stubble
        x, y = rnd.randint(3, 28), rnd.randint(4, 27)
        c.line(x, y, x + rnd.choice((-1, 1)), y - 3, ramp('#5a5444', '#80785e', '#a49a78', '#c8bea0')[2])
    x, y = rnd.randint(6, 24), rnd.randint(8, 24)
    c.rect(x, y, x + 2, y + 1, crust[3]); c.rect(x, y + 2, x + 2, y + 2, crust[0])
    # ragged edge so cells don't read as squares
    for x in range(1, 31):
        if _h(x, 0, v) % 3 == 0:
            c.set(x, 2, None)
        if _h(x, 1, v) % 3 == 0:
            c.set(x, 29, None)
    return c


def plot_straw(v):
    """Straw laid over a resting ridge (the soil shows through)."""
    rnd = seeded(50 + v)
    c = Canvas(32, 32)
    for i in range(46):
        x, y = rnd.randint(1, 28), rnd.randint(5, 27)
        ln = rnd.randint(3, 7)
        dx = rnd.choice((1, 1, -1))
        for t in range(ln):
            c.set(x + t * dx, y + (t // 3), P['straw'][2] if t % 2 else P['straw'][3])
        c.set(x, y + 1, P['straw'][1])
    return c


def compost_heap():
    c = Canvas(40, 34)
    c.shadow(20, 32, 18, 2)
    # a low frame of old planks
    c.box(2, 18, 37, 31, P['wood'])
    for x in (2, 13, 25, 37):
        c.rect(x, 16, x + 1, 31, P['wood'][1])
    # the heap: dark crumbly soil with leaves and peels on top
    soil = ramp('#22150c', '#3e2614', '#5c3a1e', '#7e5430')
    c.ellipse(20, 17, 15, 9, soil)
    rnd = seeded(77)
    for i in range(22):
        x, y = rnd.randint(8, 32), rnd.randint(9, 22)
        if c.get(x, y) is not None:
            col = rnd.choice([P['leaf'][2], P['leaf2'][2], P['orange'][2], P['straw'][3], P['red'][2]])
            c.set(x, y, col)
    for (x, y) in ((14, 9), (16, 8), (24, 10)):               # a little steam: it's working
        c.set(x, y - 4, P['white'][2]); c.set(x + 1, y - 6, P['white'][3])
    c.outline()
    return c


def compost_icon():
    c = Canvas(32, 32)
    c.shadow(16, 29, 11, 2)
    sack = ramp('#5a4428', '#8a6a40', '#b8935e', '#dcc08a')
    c.poly([(7, 28), (8, 10), (24, 10), (25, 28)], sack, light_axis='x')
    c.rect(10, 7, 22, 10, sack[1])
    c.line(9, 9, 23, 9, sack[0])
    soil = ramp('#22150c', '#3e2614', '#5c3a1e', '#7e5430')
    c.ellipse(16, 8, 6, 2.6, soil, flat=True)
    c.set(13, 7, P['leaf'][2]); c.set(18, 7, P['leaf'][3]); c.set(16, 6, P['orange'][2])
    c.rect(12, 16, 20, 21, sack[3])                            # a label patch with a leaf mark
    c.line(14, 20, 18, 17, P['leaf'][1]); c.set(17, 17, P['leaf'][2]); c.set(15, 19, P['leaf'][2])
    c.outline()
    return c


# ---------------------------------------------------------------- the big background
def farmmap(season='spring'):
    m = load_map()
    W, H, OX, OY = m['width'], m['height'], m['originX'], m['originY']
    rows = m['rows']
    bg = BG(W * 32, H * 32, W, H, (OX + W / 2, OY - H / 2))
    bg.season = season
    g = ground_for(season)
    grass(bg, base=g)
    snow = season == 'winter'

    def cells(chars):
        out = []
        for r, row in enumerate(rows):
            for c, ch in enumerate(row):
                if ch in chars:
                    out.append((c, r))
        return out

    def rect(c, r):
        return (c * 32, r * 32, c * 32 + 31, r * 32 + 31)

    def bottom(c, r):          # pixel at the bottom-middle of a cell
        return (c * 32 + 16, r * 32 + 32)

    # soft patches of a second grass colour
    patch_col = {'winter': SNOW[1], 'spring': P['leaf2'][2], 'summer': SUMMER[1], 'autumn': AUT_ORANGE[1]}[season]
    for (x, y, rx, ry) in [(700, 330, 40, 9), (100, 420, 30, 8), (980, 350, 34, 8)]:
        patch(bg, x, y, rx, ry, lighten_c(g[2], 0.1) if not snow else SNOW[1], g, P['leaf2'] if not snow else SNOW)

    # back hill: darker ground under the pines
    for (c, r) in cells('P'):
        for y in range(r * 32, r * 32 + 32):
            for x in range(c * 32, c * 32 + 32):
                p = bg.c.get(x, y)
                if p is not None:
                    bg.c.set(x, y, darken_c(p, 0.8))

    # lanes and the yard (packed earth); in winter trampled snow
    lane = K.YARD if not snow else B.TRAMPLED
    path(bg, [rect(c, r) for (c, r) in cells('=s')], r=lane)
    path(bg, [rect(c, r) for (c, r) in cells('yHJKBW')], r=lane)

    # the stream (frozen at the edges in winter), stepping stones on the forest path
    water = ICE if snow else WATER
    wcells = cells('ws')
    wset = set(wcells)
    for (c, r) in wcells:
        x0, y0, x1, y1 = rect(c, r)
        # a gentle wobble on the open sides so the stream doesn't look like stacked squares
        for y in range(y0, y1 + 1):
            wob = int(2.5 + 2.5 * math.sin((y + c * 7) / 9.0))
            lx = x0 + (wob if (c - 1, r) not in wset else 0)
            rx = x1 - (4 - wob if (c + 1, r) not in wset else 0)
            for x in range(lx, rx + 1):
                bg.c.set(x, y, water[2] if (x + y // 3) % 23 else water[1])
    for (c, r) in wcells:                                       # banks where the stream meets land
        for dc in (-1, 1):
            if (c + dc, r) not in wset:
                for y in range(r * 32, r * 32 + 32):
                    wob = int(2.5 + 2.5 * math.sin((y + c * 7) / 9.0))
                    xe = c * 32 + wob - 1 if dc < 0 else c * 32 + 31 - (4 - wob) + 1
                    bg.c.set(xe, y, lane[1]); bg.c.set(xe + dc, y, lane[2] if _h(xe, y, 3) % 4 else K.WALLSTONE[2])
        for y in range(r * 32, r * 32 + 32, 5):
            x = c * 32 + 6 + _h(c, y, 2) % 18
            bg.c.set(x, y, water[3]); bg.c.set(x + 1, y, water[3])
    for (c, r) in cells('s'):
        bx, by = bottom(c, r)
        bg.prop(K.stepping_stone(), x=bx - 6 + (r % 2) * 10, y=by - 8)
        bg.prop(K.stepping_stone(), x=bx + 4 - (r % 2) * 10, y=by - 22)

    # plots: ridges and furrows (the tilled tile is drawn on top by the game)
    for (c, r) in cells('p'):
        x0, y0, x1, y1 = rect(c, r)
        for y in range(y0, y1 + 1):
            k = y - y0
            for x in range(x0, x1 + 1):
                if k < 4:
                    col = K.SOIL[0] if k < 2 else K.SOIL[1]
                elif k < 6:
                    col = K.SOIL[3]
                elif k < 24:
                    col = K.SOIL[2]
                else:
                    col = K.SOIL[1]
                if snow and 4 <= k < 9:
                    col = SNOW[2] if _h(x, y, 1) % 3 else SNOW[3]
                if _h(x, y, 5) % 37 == 0:
                    col = K.SOIL[3]
                bg.c.set(x, y, col)
    pset = set(cells('p'))
    for (c, r) in pset:                                         # grass creeping in around each plot
        for (dc, dr, side) in ((0, -1, 'top'), (0, 1, 'bot'), (-1, 0, 'left'), (1, 0, 'right')):
            if (c + dc, r + dr) in pset:
                continue
            for t in range(0, 32, 4):
                if side == 'top':
                    tuft(bg, c * 32 + t + 2, r * 32 + 1, g)
                elif side == 'bot':
                    tuft(bg, c * 32 + t + 2, r * 32 + 31, g)
                elif side == 'left':
                    tuft(bg, c * 32 + 1, r * 32 + t + 2, g)
                else:
                    tuft(bg, c * 32 + 30, r * 32 + t + 2, g)

    # paddies: flooded with seedlings in spring, green in summer, golden in autumn, stubble and snow in winter
    def paddy_px(x, y, x0, y0, kind):
        k = y - y0
        if kind == 'S' and season != 'autumn':
            kind = 'R'                     # the harvested field is only harvested in autumn
        if kind == 'R' and season == 'spring':
            col = SPRING_PADDY[2] if (x + y) % 9 else SPRING_PADDY[3]
            if k % 8 in (3, 4) and x % 6 in (1, 2):
                col = SEEDLING[2] if k % 8 == 3 else SEEDLING[1]
            return col
        if kind == 'R' and season == 'summer':
            row, stalk = k % 6, (x + k // 6 * 2) % 4
            return SUMMER_RICE[1] if row == 0 else SUMMER_RICE[3] if stalk == 0 and row < 4 else SUMMER_RICE[2]
        if kind == 'R' and season == 'autumn':
            row, stalk = k % 6, (x + k // 6 * 2) % 4
            return K.RICE[1] if row == 0 else K.RICE[3] if stalk == 0 and row < 4 else K.RICE[1] if stalk == 2 and row == 4 else K.RICE[2]
        # stubble (harvested), and every paddy in winter
        col = K.STUBBLE[2]
        if k % 5 == 0 and (x // 2) % 3 == 0:
            col = K.STUBBLE[0]
        elif k % 5 == 1 and (x // 2) % 3 == 0:
            col = K.STUBBLE[3]
        if snow and _h(x, y, 4) % 3:
            col = SNOW[2] if _h(x, y, 6) % 4 else SNOW[3]
        return col

    for kind in ('R', 'S'):
        group = set(cells(kind + ('b' if kind == 'S' else '')))
        for (c, r) in group:
            x0, y0, x1, y1 = rect(c, r)
            for y in range(y0, y1 + 1):
                for x in range(x0, x1 + 1):
                    bg.c.set(x, y, paddy_px(x, y, x0, y0, kind))
        lev = g if not snow else SNOW
        for (c, r) in group:                                    # levees (grass ridges) round each field
            x0, y0, x1, y1 = rect(c, r)
            if (c, r - 1) not in group:
                for x in range(x0, x1 + 1):
                    for y in range(y0, y0 + 4):
                        bg.c.set(x, y, lev[3] if y == y0 else lev[2])
                    bg.c.set(x, y0 + 4, darken_c(lev[1], 0.9))
            if (c - 1, r) not in group:
                for y in range(y0, y1 + 1):
                    bg.c.set(x0, y, lev[2]); bg.c.set(x0 + 1, y, lev[2]); bg.c.set(x0 + 2, y, lev[1])
            if (c + 1, r) not in group:
                for y in range(y0, y1 + 1):
                    bg.c.set(x1, y, lev[1]); bg.c.set(x1 - 1, y, lev[2])
            if (c, r + 1) not in group:
                for x in range(x0, x1 + 1):
                    bg.c.set(x, y1, lev[1]); bg.c.set(x, y1 - 1, lev[2])
    if season in ('autumn', 'winter'):
        for (c, r) in cells('b'):
            bx, by = bottom(c, r)
            bg.prop(K.bale(), x=bx, y=by - 6)

    # pines on the back hill (two staggered rows, a gap for the path up to the village)
    gap = [c for (c, r) in cells('=') if r == 0]
    gx0, gx1 = (min(gap) * 32 - 10, (max(gap) + 1) * 32 + 10) if gap else (-1, -1)
    for i, x in enumerate(range(-6, W * 32 + 30, 34)):
        if gx0 < x < gx1:
            continue
        bg.prop(K.pine(seed=40 + i, h=44 + (i * 5) % 10), x=x + (i * 13) % 11, y=30 + (i * 7) % 6)
    for i, x in enumerate(range(-20, W * 32 + 30, 41)):
        xx = x + _h(i, 1, 3) % 15
        if gx0 - 14 < xx < gx1 + 14 or _h(i, 0, 3) % 5 == 0:
            continue
        bg.prop(K.pine(seed=i, h=56 + (i * 7) % 18), x=xx, y=58 + _h(i, 2, 3) % 8)

    # a wooden gate over the path up to the village behind (the haggle gate)
    if gap:
        arch = B.gate_arch(60)
        bg.prop(arch, x=(min(gap) + max(gap) + 1) * 16, y=46)

    # stone walls: horizontal runs along rows, vertical runs down columns
    wset = set(cells('W'))
    done = set()
    for r in range(H):
        c = 0
        while c < W:
            if (c, r) in wset and ((c - 1, r) in wset or (c + 1, r) in wset):
                start = c
                while c < W and (c, r) in wset:
                    done.add((c, r)); c += 1
                length = (c - start) * 32
                bg.prop(K.stone_wall_h(length, seed=r * 7 + start), x=start * 32 + length // 2, y=r * 32 + 26)
            else:
                c += 1
    for c in range(W):
        r = 0
        while r < H:
            if (c, r) in wset and (c, r) not in done:
                start = r
                while r < H and (c, r) in wset and (c, r) not in done:
                    r += 1
                length = (r - start) * 32 + 12
                bg.prop(K.stone_wall_v(length, seed=c * 5 + start), x=c * 32 + 16, y=r * 32 + 8)
            else:
                r += 1

    # the house (gray until the ending), the jars, the persimmon tree and the low wooden platform
    hc = cells('H')
    if hc:
        hx = (min(c for c, r in hc) + max(c for c, r in hc) + 1) * 16
        hy = (max(r for c, r in hc) + 1) * 32
        bg.prop(house(season, gray=True), x=hx, y=hy)
    jc = cells('J')
    if jc:
        jx = (min(c for c, r in jc) + max(c for c, r in jc) + 1) * 16
        jy = (max(r for c, r in jc) + 1) * 32
        bg.prop(K.jangdokdae(), x=jx, y=jy)
    kc = cells('K')
    if kc:
        kx = (min(c for c, r in kc) + max(c for c, r in kc) + 1) * 16
        ky = (max(r for c, r in kc) + 1) * 32
        bg.prop(persimmon(season), x=kx, y=ky)
    bc = cells('B')
    if bc:
        px = (min(c for c, r in bc) + max(c for c, r in bc) + 1) * 16
        py = (max(r for c, r in bc) + 1) * 32
        pc = K.pyeongsang()
        if season != 'autumn':                                  # drying peppers only in autumn
            for x in range(10, 31):
                for y in range(9, 13):
                    if pc.get(x, y) in (P['red'][2], P['red'][1], P['straw'][2], P['straw'][3]):
                        pc.set(x, y, P['woodlight'][2])
        bg.prop(pc, x=px, y=py - 2)

    # utility poles and the wires between them
    poles = sorted(cells('|'))
    for (c, r) in poles:
        bx, by = bottom(c, r)
        bg.prop(K.pole(), x=bx, y=by - 2)

    # signposts (map signs)
    icons = {'Market': B.coin_bag, 'Forest': _icon_tree, 'Sunset Hill': _icon_sun, 'Lake': _icon_water}
    for s in m.get('signs', []):
        x, y = bg.px(s['x'], s['y'])
        bg.prop(B.signpost(icons.get(s['text'], B.coin_bag)), x=x, y=y)

    # flowers and small seasonal details on '*' cells
    k = 0
    for (c, r) in cells('*'):
        x, y = c * 32 + 14, r * 32 + 20
        k += 1
        if snow:
            for dx, dy in ((0, 0), (2, 1), (-1, 2)):
                bg.c.set(x + dx, y + dy, P['red'][2]); bg.c.set(x + dx, y + dy + 1, P['leaf2'][1])
        elif season == 'autumn':
            B.leaf_pile(bg, x, y, 7)
        else:
            cols = [P['jam'], P['white'], P['sun'], P['cloth'], P['red']]
            B.flower_cluster(bg, x, y, cols[k % len(cols)], 5 if season == 'summer' else 4)

    img = bg.image()
    d = ImageDraw.Draw(img)
    pts = [bottom(c, r) for (c, r) in poles]
    if pts:
        pts.append((W * 32 + 40, pts[-1][1]))
    for (a, b) in zip(pts, pts[1:]):
        for dy in (0, 6):
            line = []
            for t in range(0, 41):
                f = t / 40
                x = a[0] + (b[0] - a[0]) * f
                y = a[1] - 2 - 92 + 9 + dy + 10 * 4 * f * (1 - f)
                line.append((x, y))
            d.line(line, fill=(40, 36, 34, 255), width=1)
    return img


# small sign icons (borrowed from the old map art)
def _icon_tree():
    import farmmap
    return farmmap.icon_tree()


def _icon_sun():
    import farmmap
    return farmmap.icon_sun()


def _icon_water():
    import farmmap
    return farmmap.icon_water()


if __name__ == '__main__':
    import sys
    out = sys.argv[1] if len(sys.argv) > 1 else '.'
    os.makedirs(out, exist_ok=True)
    for s in ('winter', 'spring', 'summer', 'autumn'):
        farmmap(s).save(os.path.join(out, f'bg_farmmap_{s}.png'))
    print('ok')
