"""Tiny pixel-art toolkit: palette ramps, primitive shapes, lighting and selective outlines.

Every sprite is drawn on a small RGBA canvas with hard pixels (no anti-aliasing),
then finished with an automatic 1px outline that uses a darkened version of the
neighbouring colour ("sel-out"), which gives the whole set a consistent look.
"""
import math
import random
from PIL import Image


def hex2rgba(h, a=255):
    h = h.lstrip('#')
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), a)


def ramp(*hexes):
    return [hex2rgba(h) for h in hexes]


# ---- Shared palette (dark -> light). Warm, slightly desaturated, cozy. ----
P = {
    'outline':   hex2rgba('#2a1a14'),
    'leaf':      ramp('#1e4a2c', '#2f7438', '#4ea347', '#93d66a'),
    'leaf2':     ramp('#24402a', '#3b6b34', '#5f9a42', '#a9cf6a'),   # olive (corn, potato)
    'orange':    ramp('#7a2e10', '#c2541a', '#ec8a2c', '#ffc266'),
    'red':       ramp('#561020', '#9c1f2e', '#dc3a3e', '#ff8c7e'),
    'yellow':    ramp('#7a560e', '#c4941c', '#f0cf48', '#fff09a'),
    'tan':       ramp('#4a301c', '#8a623c', '#b98d5a', '#e2c08a'),
    'bread':     ramp('#5a3210', '#9e6226', '#d39a4c', '#f4d18a'),
    'wood':      ramp('#3a2212', '#673c20', '#985f33', '#c88e52'),
    'woodlight': ramp('#5b3a1e', '#8e5f34', '#bf8a52', '#e6b97c'),
    'metal':     ramp('#2c323e', '#4b5468', '#7f8ca2', '#cad3df'),
    'copper':    ramp('#57260f', '#9b4d28', '#d07a3e', '#f4b77c'),
    'stone':     ramp('#2e2e38', '#535466', '#82849a', '#babdcd'),
    'dirt':      ramp('#3a2313', '#5b381e', '#7a512e', '#9a6b40'),
    'water':     ramp('#1c3454', '#2e6aae', '#58a4e4', '#c0e6ff'),
    'paper':     ramp('#6a4828', '#c4a066', '#e6cc94', '#fff2cc'),
    'sun':       ramp('#8a5600', '#e09e00', '#ffd648', '#fffbd2'),
    'spirit':    ramp('#1b4838', '#2c8864', '#5cd29a', '#ccffe8'),
    'fire':      ramp('#7a1c08', '#e0461a', '#ff9a2a', '#ffe37a'),
    'skin':      ramp('#7a4430', '#c07a56', '#eab08a', '#ffd9bc'),
    'hair':      ramp('#2e1a10', '#553220', '#7c4c2c', '#a06a40'),
    'straw':     ramp('#6e4c14', '#b88a2c', '#e8c25a', '#fff0a6'),
    'denim':     ramp('#1c2a4c', '#2f4a86', '#4a72b8', '#86aee0'),
    'shirt':     ramp('#5a1418', '#a8262c', '#d8444a', '#f6a0a0'),
    'white':     ramp('#8a8a96', '#c4c4cc', '#ececf0', '#ffffff'),
    'cloth':     ramp('#24305a', '#3a5aa0', '#5f8ad6', '#a8c8f4'),
    'apron':     ramp('#6a5a3a', '#a89464', '#d6c494', '#f6ecc6'),
    'soup':      ramp('#7a5212', '#c48a24', '#eebc4c', '#ffe9a0'),
    'jam':       ramp('#3c0a1a', '#7c1430', '#b8244a', '#f06a8a'),
    'glass':     ramp('#4a6a7a', '#8ab4c4', '#c8e4ec', '#ffffff'),
}

LIGHT = (-0.55, -0.65, 0.52)  # light from top-left, slightly toward the viewer


def _norm(v):
    l = math.sqrt(sum(c * c for c in v)) or 1.0
    return tuple(c / l for c in v)


LIGHT = _norm(LIGHT)


class Canvas:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.px = [[None] * w for _ in range(h)]  # None = transparent
        self.shadows = []  # soft shadow ellipses, composited under the sprite after outlining

    # ---------- basic ----------
    def set(self, x, y, c):
        x, y = int(round(x)), int(round(y))
        if 0 <= x < self.w and 0 <= y < self.h and c is not None:
            self.px[y][x] = c

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.px[y][x]
        return None

    def rect(self, x0, y0, x1, y1, c):
        for y in range(int(y0), int(y1) + 1):
            for x in range(int(x0), int(x1) + 1):
                self.set(x, y, c)

    def line(self, x0, y0, x1, y1, c):
        steps = int(max(abs(x1 - x0), abs(y1 - y0))) or 1
        for i in range(steps + 1):
            t = i / steps
            self.set(x0 + (x1 - x0) * t, y0 + (y1 - y0) * t, c)

    def thick_line(self, x0, y0, x1, y1, c, w=2):
        steps = int(max(abs(x1 - x0), abs(y1 - y0))) or 1
        for i in range(steps + 1):
            t = i / steps
            cx, cy = x0 + (x1 - x0) * t, y0 + (y1 - y0) * t
            for dy in range(w):
                for dx in range(w):
                    self.set(cx + dx - (w - 1) / 2, cy + dy - (w - 1) / 2, c)

    # ---------- shaded shapes ----------
    def ellipse(self, cx, cy, rx, ry, r, flat=False, light=LIGHT, dither=True):
        """Filled ellipse shaded like a sphere with ramp r (4 tones)."""
        for y in range(int(cy - ry - 1), int(cy + ry + 2)):
            for x in range(int(cx - rx - 1), int(cx + rx + 2)):
                nx = (x + 0.5 - cx) / max(rx, 0.01)
                ny = (y + 0.5 - cy) / max(ry, 0.01)
                d = nx * nx + ny * ny
                if d > 1.0:
                    continue
                if flat:
                    self.set(x, y, r[2])
                    continue
                nz = math.sqrt(max(0.0, 1.0 - d))
                lum = nx * light[0] + ny * light[1] + nz * light[2]
                self.set(x, y, tone(r, lum, x, y, dither))

    def poly(self, pts, r, light_axis='y', invert=False, dither=True, solid=None):
        """Filled polygon with a simple directional gradient."""
        xs = [p[0] for p in pts]
        ys = [p[1] for p in pts]
        x0, x1, y0, y1 = min(xs), max(xs), min(ys), max(ys)
        for y in range(int(math.floor(y0)), int(math.ceil(y1)) + 1):
            for x in range(int(math.floor(x0)), int(math.ceil(x1)) + 1):
                if not _inside(x + 0.5, y + 0.5, pts):
                    continue
                if solid is not None:
                    self.set(x, y, solid)
                    continue
                if light_axis == 'y':
                    t = (y - y0) / max(1, (y1 - y0))
                elif light_axis == 'x':
                    t = (x - x0) / max(1, (x1 - x0))
                else:
                    t = ((x - x0) / max(1, (x1 - x0)) + (y - y0) / max(1, (y1 - y0))) / 2
                if invert:
                    t = 1 - t
                lum = 0.95 - 1.3 * t
                self.set(x, y, tone(r, lum, x, y, dither))

    def box(self, x0, y0, x1, y1, r, top=True):
        """Shaded rectangle: highlight on top/left edge, shadow bottom/right."""
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                c = r[2]
                if x == x1 or y == y1:
                    c = r[1]
                if top and (y == y0 or x == x0):
                    c = r[3]
                self.set(x, y, c)

    # ---------- finishing ----------
    def outline(self, color=None, darken=0.42, corners=False):
        """Sel-out: transparent pixels touching the sprite get a darkened neighbour colour."""
        add = {}
        for y in range(self.h):
            for x in range(self.w):
                if self.px[y][x] is not None:
                    continue
                nbs = [(0, -1), (0, 1), (-1, 0), (1, 0)]
                if corners:
                    nbs += [(-1, -1), (1, -1), (-1, 1), (1, 1)]
                for dx, dy in nbs:
                    n = self.get(x + dx, y + dy)
                    if n is not None and n[3] > 0:
                        add[(x, y)] = color if color else darken_c(n, darken)
                        break
        for (x, y), c in add.items():
            self.px[y][x] = c

    def shadow(self, cx, cy, rx, ry, alpha=70):
        """Soft ground shadow under objects (composited later, never outlined)."""
        self.shadows.append((cx, cy, rx, ry, alpha))

    def flip(self):
        c = Canvas(self.w, self.h)
        for y in range(self.h):
            c.px[y] = list(reversed(self.px[y]))
        c.shadows = [(self.w - cx, cy, rx, ry, a) for (cx, cy, rx, ry, a) in self.shadows]
        return c

    def image(self):
        im = Image.new('RGBA', (self.w, self.h), (0, 0, 0, 0))
        for (cx, cy, rx, ry, a) in self.shadows:
            for y in range(int(cy - ry), int(cy + ry) + 1):
                for x in range(int(cx - rx), int(cx + rx) + 1):
                    if 0 <= x < self.w and 0 <= y < self.h and ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 <= 1:
                        im.putpixel((x, y), (20, 12, 8, a))
        for y in range(self.h):
            for x in range(self.w):
                c = self.px[y][x]
                if c is not None:
                    im.putpixel((x, y), c)
        return im


def _inside(x, y, pts):
    inside = False
    n = len(pts)
    j = n - 1
    for i in range(n):
        xi, yi = pts[i]
        xj, yj = pts[j]
        if ((yi > y) != (yj > y)) and (x < (xj - xi) * (y - yi) / ((yj - yi) or 1e-9) + xi):
            inside = not inside
        j = i
    return inside


BAYER = [[0, 2], [3, 1]]


def tone(r, lum, x, y, dither=True):
    """Map luminance (-1..1) onto a 4-tone ramp with a light 2x2 ordered dither between bands."""
    v = (lum + 0.35) / 1.25 * 3.0  # -> roughly 0..3
    if dither:
        v += (BAYER[int(y) % 2][int(x) % 2] - 1.5) * 0.1
    i = int(math.floor(v))
    return r[max(0, min(3, i))]


def darken_c(c, f):
    return (int(c[0] * f), int(c[1] * f), int(c[2] * f), 255)


def lighten_c(c, f):
    return (int(c[0] + (255 - c[0]) * f), int(c[1] + (255 - c[1]) * f), int(c[2] + (255 - c[2]) * f), c[3])


def seeded(seed):
    return random.Random(seed)
