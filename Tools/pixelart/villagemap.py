"""The village map (M key): a hand-drawn map on hanji paper, 640 x 360 (shown at 2x).

Places sit roughly where they are in the game: the farm in the middle, the market lane east, the back hill
west over the stream, Sunset Hill south, the reservoir south-east and Star Hill up north-east at the end of
the vine. Each place that isn't open yet is hidden under its own gray fog layer (vmap_fog_<id>), so the game
just stacks the base map and the fogs that still apply. Region positions are written to
Assets/Resources/Maps/villagemap.json, which the game and the browser preview read for labels and the pin.
"""
import json
import math
import os

from PIL import Image

import kvillage as K
from pixel import Canvas, P, ramp

HERE = os.path.dirname(os.path.abspath(__file__))
OUT_JSON = os.path.join(HERE, '..', '..', 'Assets', 'Resources', 'Maps', 'villagemap.json')

MW, MH = 640, 360
PAPER = ramp('#a8906a', '#cdb88e', '#e4d4ae', '#f2e8cc')
INK = (74, 52, 36, 255)
INK_SOFT = (120, 92, 66, 255)
WASH_GREEN = ramp('#5e7a4a', '#7e9a62', '#a2b882', '#c4d4a2')
WASH_HILL = ramp('#6e7a56', '#8e9a70', '#aab68a', '#c8d0a6')
WASH_WATER = ramp('#3e6a8a', '#5a8aaa', '#86b0c8', '#b8d4e2')
WASH_FIELD = ramp('#7e8a46', '#9aa85a', '#b8c47a', '#d4dc9e')
WASH_EARTH = ramp('#8a6a46', '#a6845a', '#c2a276', '#dcc4a0')
FOG = ramp('#6c6c76', '#8e8e98', '#b0b0b8', '#d2d2d8')
NIGHT = ramp('#1e2244', '#2c3466', '#44508c', '#7a86c0')

# id -> label anchor (x, y) on the 640 x 360 map, and the fog cloud (cx, cy, rx, ry)
REGIONS = {
    'farm':     {'label': (300, 214), 'pin': (300, 176)},
    'market':   {'label': (532, 196), 'pin': (532, 156)},
    'forest':   {'label': (104, 208), 'pin': (104, 150), 'fog': (104, 138, 92, 76)},
    'hill':     {'label': (218, 334), 'pin': (218, 282), 'fog': (218, 282, 74, 52)},
    'lake':     {'label': (474, 334), 'pin': (474, 282), 'fog': (474, 284, 84, 54)},
    'starhill': {'label': (412, 96),  'pin': (412, 52),  'fog': (412, 52, 60, 40)},
}


def _h(x, y, k=0):
    v = (x * 374761393 + y * 668265263 + k * 2246822519) & 0xFFFFFFFF
    v = (v ^ (v >> 13)) * 1274126177 & 0xFFFFFFFF
    return v ^ (v >> 16)


BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


class Sheet:
    """A plain pixel sheet with a few drawing helpers (no outlines: this is ink and wash on paper)."""

    def __init__(self, w, h):
        self.img = Image.new('RGBA', (w, h), (0, 0, 0, 0))
        self.px = self.img.load()
        self.w, self.h = w, h

    def set(self, x, y, c):
        if 0 <= x < self.w and 0 <= y < self.h and c is not None:
            self.px[int(x), int(y)] = c

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.px[int(x), int(y)]
        return None

    def blob(self, cx, cy, rx, ry, r, wobble=0.12, seed=0, edge_ink=False):
        """A soft-edged wash: dithered rim, slightly lumpy outline (hand-painted, not a perfect ellipse)."""
        for y in range(int(cy - ry - 4), int(cy + ry + 5)):
            for x in range(int(cx - rx - 4), int(cx + rx + 5)):
                a = math.atan2((y - cy) / max(1, ry), (x - cx) / max(1, rx))
                lump = 1 + wobble * math.sin(a * 3 + seed) + wobble * 0.6 * math.sin(a * 7 + seed * 2)
                d = math.hypot((x - cx) / rx, (y - cy) / ry) / lump
                if d > 1:
                    continue
                if d > 0.86 and (1 - d) * 16 / 0.14 < BAYER[y % 4][x % 4]:
                    continue
                col = r[2] if d < 0.6 else r[1]
                if d < 0.35 and (x + y) % 7 == 0:
                    col = r[3]
                self.set(x, y, col)
                if edge_ink and 0.93 < d <= 1:
                    self.set(x, y, INK_SOFT)

    def line(self, x0, y0, x1, y1, c, w=1):
        n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
        for i in range(n):
            t = i / max(1, n - 1)
            x, y = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            for dx in range(w):
                for dy in range(w):
                    self.set(round(x) + dx - w // 2, round(y) + dy - w // 2, c)

    def stamp(self, canvas, x, y):
        """Paste a pixel Canvas with its bottom-middle at (x, y)."""
        ox, oy = int(x - canvas.w / 2), int(y - canvas.h)
        for yy in range(canvas.h):
            for xx in range(canvas.w):
                p = canvas.get(xx, yy)
                if p is not None and (len(p) < 4 or p[3] > 0):
                    self.set(ox + xx, oy + yy, p if len(p) == 4 else tuple(p) + (255,))


def _curve(pts, steps=24):
    """Catmull-Rom through hand-placed points: roads and the stream bend instead of running straight."""
    out = []
    pts = [pts[0]] + pts + [pts[-1]]
    for i in range(1, len(pts) - 2):
        p0, p1, p2, p3 = pts[i - 1], pts[i], pts[i + 1], pts[i + 2]
        for s in range(steps):
            t = s / steps
            t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * ((2 * p1[k]) + (-p0[k] + p2[k]) * t + (2 * p0[k] - 5 * p1[k] + 4 * p2[k] - p3[k]) * t2
                                    + (-p0[k] + 3 * p1[k] - 3 * p2[k] + p3[k]) * t3) for k in (0, 1)))
    out.append(pts[-2])
    return out


def _road(sh, pts, dotted=False):
    path = _curve(pts)
    for i, (x, y) in enumerate(path):
        if dotted and (i // 3) % 2:
            continue
        for dx in (-2, -1, 0, 1, 2):
            for dy in (-1, 0, 1):
                sh.set(round(x) + dx, round(y) + dy, WASH_EARTH[2] if abs(dx) < 2 else WASH_EARTH[1])
    for i, (x, y) in enumerate(path):                       # ink edges, broken here and there like a quick pen line
        if _h(i // 5, 3) % 6 == 0 or (dotted and (i // 3) % 2):
            continue
        sh.set(round(x), round(y) - 2, INK_SOFT)
        if not dotted:
            sh.set(round(x), round(y) + 2, INK_SOFT)


# ---------------------------------------------------------------- little drawings of each place
def _house():
    c = Canvas(40, 26)
    c.box(7, 12, 32, 24, K.PLASTER)
    c.rect(17, 16, 21, 24, P['wood'][1])
    c.poly([(1, 13), (8, 4), (31, 4), (38, 13)], K.ROOF, light_axis='x')
    c.line(8, 4, 31, 4, K.ROOF[3])
    c.outline()
    return c


def _shop(w, roof, wall=None):
    c = Canvas(w + 2, 22)
    c.box(1, 8, w, 21, wall or ramp('#6c6a66', '#9c9a94', '#c4c2bc', '#e6e4de'))
    c.box(0, 4, w + 1, 8, roof)
    c.rect(3, 13, w - 2, 17, P['water'][2])
    c.outline()
    return c


def _pavilion():
    c = Canvas(30, 22)
    giwa = ramp('#22262c', '#363c44', '#4e565f', '#76808a')
    pil = ramp('#4a1c14', '#6e2c1e', '#94422c', '#b8664a')
    c.box(3, 17, 26, 21, K.WALLSTONE)
    for x in (6, 23):
        c.box(x - 1, 9, x + 1, 17, pil)
    c.poly([(0, 10), (6, 3), (23, 3), (29, 10)], giwa, light_axis='x')
    c.outline()
    return c


def _tower():
    c = Canvas(12, 22)
    con = ramp('#5c5c58', '#8a8a84', '#b2b2aa', '#d8d8d0')
    c.box(3, 8, 8, 21, con)
    c.box(2, 3, 9, 8, con)
    c.poly([(1, 4), (5.5, 0), (10, 4)], K.ROOF, light_axis='x')
    c.outline()
    return c


def _zelkova():
    c = Canvas(30, 30)
    c.box(13, 18, 16, 27, P['wood'])
    c.ellipse(15, 26, 10, 3, K.WALLSTONE, flat=True)
    for (cx, cy, rx, ry) in [(8, 13, 7, 6), (22, 13, 7, 6), (15, 8, 9, 7), (15, 15, 10, 5)]:
        c.ellipse(cx, cy, rx, ry, ramp('#1e4a1c', '#2f6e2a', '#4a9a38', '#8ccc58'))
    c.outline()
    return c


def base():
    sh = Sheet(MW, MH)
    # hanji: warm paper with fibres, darker towards the edges, a few long fibres and flecks
    for y in range(MH):
        for x in range(MW):
            d = min(x, y, MW - 1 - x, MH - 1 - y)
            col = PAPER[2]
            if d < 14 and (14 - d) * 1.1 > BAYER[y % 4][x % 4]:
                col = PAPER[1]
            if d < 5 and (5 - d) * 3 > BAYER[y % 4][x % 4]:
                col = PAPER[0]
            hv = _h(x, y, 1) % 1000
            if hv < 6:
                col = PAPER[3]
            elif hv < 9:
                col = PAPER[1]
            sh.set(x, y, col)
    for k in range(60):                                       # long fibres
        x, y = _h(k, 1, 2) % MW, _h(k, 2, 2) % MH
        a = (_h(k, 3, 2) % 100) / 100 * math.pi
        for i in range(6 + _h(k, 4, 2) % 10):
            sh.set(x + math.cos(a) * i, y + math.sin(a) * i, PAPER[3])

    # the back mountains along the top, ink ridge lines
    for (cx, cy, rx, ry, s) in [(70, 40, 120, 46, 1), (220, 30, 110, 38, 2), (520, 34, 150, 42, 3), (640, 50, 90, 50, 4)]:
        sh.blob(cx, cy, rx, ry, WASH_HILL, wobble=0.08, seed=s)
    for x in range(14, MW - 14):
        y = 54 + 10 * math.sin(x / 47) + 6 * math.sin(x / 19 + 1)
        if _h(x // 6, 5) % 5:
            sh.set(x, int(y), INK_SOFT)

    # rice paddies south of the farm: a grid of pale squares with ink bunds
    for gy in range(3):
        for gx in range(4):
            x0, y0 = 290 + gx * 26 + (gy % 2) * 6, 244 + gy * 18
            for y in range(y0, y0 + 15):
                for x in range(x0, x0 + 23):
                    sh.set(x, y, WASH_FIELD[2] if (x + y) % 9 else WASH_FIELD[3])
            for x in range(x0 - 1, x0 + 24):
                sh.set(x, y0 - 1, INK_SOFT); sh.set(x, y0 + 15, INK_SOFT)
            for y in range(y0, y0 + 15):
                sh.set(x0 - 1, y, INK_SOFT); sh.set(x0 + 23, y, INK_SOFT)

    # areas: the back hill (forest wash), Sunset Hill terraces, the reservoir, the market lane, Star Hill at night
    sh.blob(104, 148, 80, 60, WASH_GREEN, seed=5, edge_ink=True)
    sh.blob(218, 284, 66, 44, WASH_FIELD, seed=6, edge_ink=True)
    for k, y in enumerate((272, 286, 300)):                    # terrace lines
        for x in range(170 + k * 6, 266 - k * 6):
            if _h(x // 4, k, 7) % 4:
                sh.set(x, y, INK_SOFT)
    sh.blob(474, 284, 76, 46, WASH_GREEN, seed=7)
    sh.blob(480, 282, 54, 30, WASH_WATER, seed=8, edge_ink=True)
    for x in range(440, 520):                                  # the dam along the south shore
        sh.set(x, 312, INK); sh.set(x, 313, K.WALLSTONE[1])
    sh.blob(532, 150, 74, 40, WASH_EARTH, seed=9)
    sh.blob(412, 50, 46, 30, NIGHT, seed=10, edge_ink=True)
    for k in range(14):                                         # stars
        x, y = 380 + _h(k, 1, 11) % 64, 30 + _h(k, 2, 11) % 34
        sh.set(x, y, (255, 240, 180, 255))
        if k % 3 == 0:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                sh.set(x + dx, y + dy, (255, 220, 140, 255))

    # the stream down the west side
    stream = _curve([(196, 40), (190, 100), (180, 150), (186, 210), (176, 270), (182, 350)])
    for (x, y) in stream:
        for dx in range(-4, 5):
            sh.set(round(x) + dx, round(y), WASH_WATER[2] if abs(dx) < 3 else WASH_WATER[1])
    for i, (x, y) in enumerate(stream):
        if i % 7 == 0:
            sh.set(round(x) - 1, round(y), WASH_WATER[3]); sh.set(round(x), round(y), WASH_WATER[3])

    # roads: the village lane east to the market, west over the stepping stones, south and south-east,
    # and the star vine (dotted) up to Star Hill
    _road(sh, [(300, 186), (360, 188), (430, 170), (500, 160), (560, 158)])
    _road(sh, [(282, 182), (230, 176), (190, 168), (150, 160), (112, 156)])
    _road(sh, [(296, 196), (270, 230), (240, 262), (222, 280)])
    _road(sh, [(330, 194), (380, 220), (430, 250), (460, 270)])
    _road(sh, [(352, 178), (380, 130), (400, 90), (410, 64)], dotted=True)
    for k in range(3):                                         # stepping stones where the road crosses the stream
        sh.set(184 + k * 3, 166, K.WALLSTONE[3]); sh.set(185 + k * 3, 166, K.WALLSTONE[2])

    # little drawings
    sh.stamp(_house(), 296, 184)
    for k, (dx, dy) in enumerate(((26, -16), (40, -16), (26, -6), (40, -6))):    # the plots beside the house
        for y in range(176 + dy, 184 + dy):
            for x in range(298 + dx, 310 + dx):
                sh.set(x, y, P['dirt'][2] if (y % 3) else P['dirt'][1])
    for (x, y) in [(268, 168), (330, 168)]:
        sh.stamp(K.pine(seed=(x + y) % 5, h=30), x, y)
    sh.stamp(_shop(26, ramp('#4a1a16', '#7e2c22', '#b04432', '#d87a5e')), 512, 156)
    sh.stamp(_shop(22, K.ROOF), 540, 154)
    sh.stamp(_shop(24, ramp('#123a24', '#1e5a36', '#2e7e4c', '#6ab486')), 566, 158)
    for (x, y, s) in [(70, 130, 1), (88, 118, 2), (128, 124, 3), (140, 150, 4), (62, 166, 0), (150, 176, 1), (84, 182, 2)]:
        sh.stamp(K.pine(seed=s, h=30 + s * 2), x, y)
    sh.stamp(_zelkova(), 106, 166)
    sh.stamp(_pavilion(), 218, 270)
    sh.stamp(_tower(), 506, 284)
    for k in range(6):                                          # the vine up to Star Hill
        x, y = 404 + k, 60 + k * 2
        sh.set(x, y, P['leaf'][2])

    # a compass rose in the corner (N with a little ink star)
    cx, cy = 590, 312
    for a in range(0, 360, 45):
        r = 14 if a % 90 == 0 else 8
        sh.line(cx, cy, cx + math.sin(math.radians(a)) * r, cy - math.cos(math.radians(a)) * r, INK)
    sh.set(cx, cy, P['red'][2])
    for (x, y) in [(587, 286), (587, 287), (587, 288), (588, 287), (589, 288), (590, 286), (590, 287), (590, 288)]:
        sh.set(x, y, INK)                                       # a tiny "N"
    # a frame line, slightly inside the edge
    for x in range(8, MW - 8):
        sh.set(x, 8, INK_SOFT); sh.set(x, MH - 9, INK_SOFT)
    for y in range(8, MH - 8):
        sh.set(8, y, INK_SOFT); sh.set(MW - 9, y, INK_SOFT)
    return sh.img


def fog(region):
    """Gray fog over one place: a heap of round puffs, lighter on top, with a dithered rim and a few swirls."""
    cx, cy, rx, ry = REGIONS[region]['fog']
    sh = Sheet(MW, MH)
    puffs = []
    k = 0
    for j in range(9):
        a = j / 9 * 2 * math.pi
        puffs.append((cx + math.cos(a) * rx * 0.62, cy + math.sin(a) * ry * 0.55, rx * 0.42, ry * 0.5))
    puffs.append((cx, cy, rx * 0.72, ry * 0.72))
    puffs.sort(key=lambda p: p[1])                                 # lower puffs in front, like a heap of cloud
    for (px, py, prx, pry) in puffs:
        k += 1
        for y in range(int(py - pry - 3), int(py + pry + 4)):
            for x in range(int(px - prx - 3), int(px + prx + 4)):
                d = math.hypot((x - px) / prx, (y - py) / pry)
                if d > 1:
                    continue
                if d > 0.8 and (1 - d) * 16 / 0.2 < BAYER[y % 4][x % 4]:
                    continue
                # round shading: lit from the upper left, a darker rim underneath
                lx, ly = (x - px) / prx, (y - py) / pry
                lit = -0.5 * lx - 0.85 * ly
                col = FOG[3] if lit > 0.45 else FOG[2] if lit > -0.35 else FOG[1]
                if ly > 0.78 and d > 0.82:
                    col = FOG[0]
                sh.set(x, y, col)
    for s in range(3):                                             # swirls
        y0 = cy - ry * 0.2 + s * ry * 0.25
        for x in range(int(cx - rx * 0.4), int(cx + rx * 0.4)):
            y = y0 + 3 * math.sin((x - cx) / 7 + s)
            if sh.get(x, int(y)) and _h(x // 3, s, 9) % 3:
                sh.set(x, int(y), FOG[0])
    return sh.img


def pin():
    """Where you are: a red map pin with a white dot."""
    c = Canvas(14, 20)
    c.ellipse(7, 6.5, 5.5, 5.5, P['red'])
    c.poly([(3, 9), (7, 18), (11, 9)], P['red'], light_axis='x')
    c.set(6, 5, P['white'][3]); c.set(7, 5, P['white'][3]); c.set(6, 6, P['white'][3]); c.set(7, 6, P['white'][2])
    c.outline()
    return c


def fog_bank(seed=0):
    """In-world fog drifting past a path that isn't open yet: overlapping gray puffs in a few alpha steps
    (no smooth gradients: three hard levels, dithered between)."""
    w, h = 112, 64
    c = Canvas(w, h)
    puffs = [(24, 38, 22, 16), (54, 30, 28, 20), (86, 38, 22, 15), (40, 46, 24, 12), (72, 46, 24, 12)]
    for (px, py, rx, ry) in puffs:
        px += (_h(seed, int(px), 3) % 7) - 3
        for y in range(h):
            for x in range(w):
                d = math.hypot((x - px) / rx, (y - py) / ry)
                if d > 1:
                    continue
                a = 200 if d < 0.55 else 150 if d < 0.8 else 90
                if d > 0.8 and (1 - d) * 16 / 0.2 < BAYER[y % 4][x % 4]:
                    continue
                old = c.get(x, y)
                if old is not None and old[3] >= a:
                    continue
                top = y < py - ry * 0.3
                c.set(x, y, (214, 214, 222, a) if top else (182, 182, 192, a))
    return c


def write_json():
    data = {'width': MW, 'height': MH, 'regions': [
        {'id': rid, 'labelX': r['label'][0], 'labelY': r['label'][1], 'pinX': r['pin'][0], 'pinY': r['pin'][1]}
        for rid, r in REGIONS.items()]}
    with open(OUT_JSON, 'w') as f:
        json.dump(data, f, indent=1)


def images():
    out = {'vmap': base()}
    for rid, r in REGIONS.items():
        if 'fog' in r:
            out[f'vmap_fog_{rid}'] = fog(rid)
    return out


def world_sprites():
    return {'vmap_pin': pin(), 'fog_bank': fog_bank(0), 'fog_bank_2': fog_bank(1)}


if __name__ == '__main__':
    write_json()
    imgs = images()
    full = imgs['vmap'].copy()
    for k, v in imgs.items():
        if k != 'vmap':
            full.alpha_composite(v)
    full.resize((MW * 2, MH * 2), 0).save('/tmp/vmap_preview.png')
    print('ok')
