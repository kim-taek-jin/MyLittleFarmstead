"""Scene backgrounds at the same pixel density as every other sprite (32 px = 1 world unit).

Each background is laid out around the real object positions of its scene
(portals, well, farm plot, bed, merchant) so the art and the gameplay agree.
Ground is drawn from repeated hand-designed tiles; props are small outlined sprites.
No random noise fields: every detail is placed on purpose.
"""
from pixel import Canvas, P, darken_c, lighten_c, ramp, hex2rgba
import items
import world


def mix(a, b, t):
    return (int(a[0] + (b[0] - a[0]) * t), int(a[1] + (b[1] - a[1]) * t), int(a[2] + (b[2] - a[2]) * t), 255)


# ======================================================================
# Seasons ("seasons of the heart": winter = burnout ... autumn = fruition)
# ======================================================================
SNOW = ramp('#7d8ea6', '#b8c6d8', '#e4ecf4', '#ffffff')
SNOW_EDGE = hex2rgba('#5c6c84')
SUMMER = ramp('#163f1f', '#24702c', '#3f9e38', '#86d455')
AUTUMN_GRASS = ramp('#4a4220', '#7a6a2c', '#a4913e', '#d2bd68')
AUT_ORANGE = ramp('#5a2a0c', '#a8501a', '#e08a2c', '#ffc46a')
AUT_RED = ramp('#4e1414', '#922a20', '#cc4a2c', '#f2896a')
AUT_YELLOW = ramp('#5a4a10', '#a8861c', '#e0c03a', '#fff07a')
TRAMPLED = ramp('#7e828c', '#a6aab4', '#c8cad0', '#e8eaee')
ICE = ramp('#6a8cac', '#9cc0dc', '#cfe6f4', '#ffffff')

SEASONS = ('winter', 'spring', 'summer', 'autumn')


def season_ground(season):
    return {'winter': SNOW, 'spring': P['leaf'], 'summer': SUMMER, 'autumn': AUTUMN_GRASS}[season]


def snow_cap(c):
    """Pile a little snow on every upward-facing edge of a sprite (in place)."""
    tops = []
    for y in range(c.h):
        for x in range(c.w):
            p = c.get(x, y)
            if p is not None and p[3] == 255 and (y == 0 or c.get(x, y - 1) is None):
                tops.append((x, y))
    for (x, y) in tops:
        c.set(x, y, SNOW[3])
        for k, tone in ((1, SNOW[3]), (2, SNOW[2])):
            below = c.get(x, y + k)
            if below is None or below[3] != 255 or below in (SNOW[2], SNOW[3]):
                break
            if k == 2 and (x * 7 + y) % 3 == 0:
                break
            c.set(x, y + k, tone)
        if y > 0:
            c.px[y - 1][x] = SNOW_EDGE
    return c


class BG:
    def __init__(self, w, h, world_w, world_h, center=(0.0, 0.0)):
        self.w, self.h = w, h
        self.c = Canvas(w, h)
        self.sx, self.sy = w / world_w, h / world_h
        self.cx, self.cy = center
        self.props = []
        self.season = 'spring'

    def px(self, wx, wy):
        return (int(round((wx - self.cx) * self.sx + self.w / 2)), int(round(self.h / 2 - (wy - self.cy) * self.sy)))

    def prop(self, canvas, wx=None, wy=None, x=None, y=None):
        """Place a prop sprite with its bottom-centre at world (wx, wy) or pixel (x, y)."""
        if x is None:
            x, y = self.px(wx, wy)
        self.props.append((y, canvas, x, y))

    def image(self):
        img = self.c.image()
        for _, canvas, x, y in sorted(self.props, key=lambda p: p[0]):
            if self.season == 'winter' and not getattr(canvas, 'nosnow', False):
                snow_cap(canvas)
            pi = canvas.image()
            img.alpha_composite(pi, (int(x - canvas.w // 2), int(y - canvas.h)))
        return img


# ======================================================================
# Ground tiles
# ======================================================================
TUFTS = {
    0: [(5, 7), (20, 4), (13, 19), (27, 24), (3, 28)],
    1: [(9, 12), (24, 15), (16, 28), (2, 2)],
    2: [(4, 21), (18, 9), (28, 30), (11, 3)],
}
# a little three-blade tuft:  L = light tip, D = dark blade
TUFT = [(-1, -1, 3), (1, -2, 3), (-1, 0, 1), (0, 0, 3), (1, -1, 1), (0, 1, 1), (2, 0, 1)]


def tuft(bg, x, y, g):
    for dx, dy, k in TUFT:
        bg.c.set(x + dx, y + dy, g[k])


def grass(bg, x0=0, y0=0, x1=None, y1=None, base=None):
    x1 = bg.w if x1 is None else x1
    y1 = bg.h if y1 is None else y1
    g = base or P['leaf']
    for y in range(y0, y1):
        for x in range(x0, x1):
            bg.c.set(x, y, g[2])
    for ty in range(y0 // 32, y1 // 32 + 1):
        for tx in range(x0 // 32, x1 // 32 + 1):
            v = [0, 0, 1, 2, 2][(tx * 7 + ty * 13) % 5]
            for (dx, dy) in TUFTS[v]:
                x, y = tx * 32 + dx, ty * 32 + dy
                if x0 + 2 <= x < x1 - 2 and y0 + 2 <= y < y1 - 2:
                    tuft(bg, x, y, g)


def patch(bg, cx, cy, rx, ry, color, ground=None, tufts=None):
    """Crisp blob of a second grass colour (three overlapping ellipses) to break up tiling."""
    blobs = [(cx, cy, rx, ry), (cx - rx * 0.55, cy + ry * 0.25, rx * 0.6, ry * 0.7), (cx + rx * 0.5, cy - ry * 0.2, rx * 0.55, ry * 0.75)]
    cells = []
    for y in range(int(cy - ry * 1.6), int(cy + ry * 1.6) + 1):
        for x in range(int(cx - rx * 1.6), int(cx + rx * 1.6) + 1):
            if any(((x - bx) / brx) ** 2 + ((y - by) / bry) ** 2 <= 1 for bx, by, brx, bry in blobs):
                g = ground or P['leaf']
                if bg.c.get(x, y) in (g[1], g[2], g[3]):
                    bg.c.set(x, y, color)
                    cells.append((x, y))
    olive = tufts or P['leaf2']
    for i, (x, y) in enumerate(cells):
        if (x * 13 + y * 7) % 97 == 0:
            tuft(bg, x, y, olive)


def _h(x, y, k=0):
    """Cheap deterministic hash for 'hand placed' variation."""
    v = (x * 374761393 + y * 668265263 + k * 2246822519) & 0xFFFFFFFF
    v = (v ^ (v >> 13)) * 1274126177 & 0xFFFFFFFF
    return v ^ (v >> 16)


def path(bg, rects, r=P['tan']):
    """Dirt path from pixel rectangles (x0, y0, x1, y1): wobbly edges, a darker rim, scattered pebbles."""
    mask = set()
    for (x0, y0, x1, y1) in rects:
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                if 0 <= x < bg.w and 0 <= y < bg.h:
                    mask.add((x, y))
    # wobble: nibble the outermost pixel row in short runs
    nibble = set()
    for (x, y) in mask:
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            if (x + dx, y + dy) not in mask and 0 <= x + dx < bg.w and 0 <= y + dy < bg.h:
                run = (x // 5 if dy else y // 5)
                if _h(run, dx * 3 + dy, 7) % 3 == 0:
                    nibble.add((x, y))
    mask -= nibble
    for (x, y) in mask:
        edge = any((x + dx, y + dy) not in mask and 0 <= x + dx < bg.w and 0 <= y + dy < bg.h
                   for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
        bg.c.set(x, y, r[1] if edge else r[2])
    # pebbles: small 2-3 px stones, irregularly spaced
    for (x, y) in mask:
        hv = _h(x, y, 1) % 1000
        if hv < 6:
            bg.c.set(x, y, r[3]); bg.c.set(x + 1, y, r[3]); bg.c.set(x, y + 1, r[1]); bg.c.set(x + 1, y + 1, r[1])
        elif hv < 14:
            bg.c.set(x, y, r[1])
    # grass shadow line under the top edge, light rim at the bottom edge
    for (x, y) in mask:
        if (x, y - 1) not in mask and y > 0:
            bg.c.set(x, y, darken_c(r[2], 0.82))
    return mask


# ======================================================================
# Props
# ======================================================================
def flower(color, h=3):
    c = Canvas(5, 6)
    c.line(2, 5, 2, 6 - h, P['leaf'][1])
    for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1)):
        c.set(2 + dx, 2 + dy, color[2])
    c.set(2, 2, P['yellow'][3])
    c.set(1, 1, color[3])
    return c


def flower_cluster(bg, x, y, color, n=4):
    offs = [(0, 0), (4, 2), (-3, 3), (2, 5), (6, -1), (-5, 0)][:n]
    for dx, dy in offs:
        bg.prop(flower(color), x=x + dx, y=y + dy)


def bush(w=30, h=22, r=P['leaf'], blossom=None):
    c = Canvas(w, h)
    c.shadow(w / 2, h - 2, w / 2 - 2, 2.5)
    c.ellipse(w * 0.32, h * 0.6, w * 0.3, h * 0.36, r)
    c.ellipse(w * 0.68, h * 0.6, w * 0.3, h * 0.36, r)
    c.ellipse(w * 0.5, h * 0.42, w * 0.34, h * 0.38, r)
    if blossom:
        for (fx, fy) in [(0.3, 0.45), (0.55, 0.3), (0.72, 0.55), (0.45, 0.62), (0.2, 0.7), (0.82, 0.38)]:
            x, y = int(w * fx), int(h * fy)
            if c.get(x, y) is not None:
                c.set(x, y, blossom[3]); c.set(x + 1, y, blossom[2]); c.set(x, y + 1, blossom[2])
    c.outline()
    return c


def snowman():
    c = Canvas(16, 24)
    c.shadow(8, 23, 7, 1.5)
    c.ellipse(8, 17, 6, 6, SNOW)
    c.ellipse(8, 8, 4.5, 4.5, SNOW)
    c.set(6, 7, P['outline']); c.set(9, 7, P['outline'])
    c.rect(8, 9, 10, 9, P['orange'][2])
    c.rect(4, 3, 11, 4, P['shirt'][2]); c.rect(5, 0, 10, 3, P['outline'])
    c.set(8, 15, P['outline']); c.set(8, 18, P['outline'])
    c.outline()
    c.nosnow = True
    return c


def leaf_pile(bg, x, y, n=6):
    """A few fallen autumn leaves, placed in a loose cluster."""
    cols = [AUT_ORANGE, AUT_RED, AUT_YELLOW]
    offs = [(0, 0), (3, 1), (-2, 2), (5, -1), (1, 3), (-4, 0), (6, 2), (2, -2)][:n]
    for i, (dx, dy) in enumerate(offs):
        col = cols[(i + x) % 3]
        bg.c.set(x + dx, y + dy, col[2]); bg.c.set(x + dx + 1, y + dy, col[1]); bg.c.set(x + dx, y + dy - 1, col[3])


def pumpkin_deco():
    c = Canvas(14, 11)
    c.shadow(7, 10, 6, 1)
    for cx in (4.5, 7, 9.5):
        c.ellipse(cx, 6.5, 3.2, 3.5, P['orange'])
    c.rect(6, 1, 7, 3, P['leaf2'][1])
    c.outline()
    return c


def fence_h(length):
    c = Canvas(length, 14)
    for x in range(1, length - 1, 12):
        c.box(x, 1, x + 2, 12, P['woodlight'])
    c.rect(0, 4, length - 1, 5, P['woodlight'][2])
    c.rect(0, 8, length - 1, 9, P['woodlight'][1])
    c.line(0, 4, length - 1, 4, P['woodlight'][3])
    c.outline()
    return c


def fence_v(length):
    c = Canvas(8, length)
    for y in range(2, length - 2, 12):
        c.box(2, y, 4, y + 9, P['woodlight'])
    c.rect(3, 0, 3, length - 1, P['woodlight'][1])
    c.outline()
    return c


def cottage(roof=None):
    roof = roof or P['shirt']
    c = Canvas(80, 72)
    c.shadow(40, 70, 38, 3)
    # wall
    c.box(6, 30, 73, 68, P['woodlight'])
    for y in range(34, 68, 5):
        c.line(7, y, 72, y, P['woodlight'][1])
    # roof (red tiles, rows)
    c.poly([(0, 32), (12, 6), (68, 6), (79, 32)], roof, light_axis='y')
    for y in range(10, 32, 5):
        for x in range(0, 80):
            if c.get(x, y) is not None:
                c.set(x, y, roof[1])
        for x in range(4 + (y // 5 % 2) * 4, 77, 8):
            if c.get(x, y - 1) is not None:
                c.set(x, y - 1, roof[3])
    c.rect(0, 31, 79, 32, roof[0])
    # chimney
    c.box(54, 0, 61, 10, P['stone'])
    # door
    c.box(33, 44, 46, 68, P['wood'])
    c.line(39, 45, 39, 67, P['wood'][1])
    c.set(43, 56, P['sun'][2])
    c.rect(31, 42, 48, 43, P['wood'][0])
    # windows
    for wx in (13, 55):
        c.box(wx, 42, wx + 11, 53, P['wood'])
        c.box(wx + 1, 43, wx + 10, 52, P['water'])
        c.line(wx + 6, 43, wx + 6, 52, P['wood'][1]); c.line(wx + 1, 48, wx + 10, 48, P['wood'][1])
        c.set(wx + 2, 44, P['water'][3]); c.set(wx + 3, 44, P['water'][3])
        c.rect(wx - 1, 54, wx + 12, 55, P['wood'][2])          # sill
        for k in range(0, 12, 3):                                 # flower box
            c.set(wx + k, 53, P['red'][2])
    c.outline()
    return c


def mailbox():
    c = Canvas(12, 20)
    c.shadow(6, 19, 4, 1)
    c.rect(5, 9, 6, 19, P['wood'][1])
    c.box(1, 3, 10, 9, P['denim'])
    c.rect(9, 1, 9, 4, P['red'][2]); c.rect(10, 1, 10, 2, P['red'][2])
    c.outline()
    return c


def log_pile():
    c = Canvas(26, 16)
    c.shadow(13, 15, 12, 1.5)
    for (x, y) in [(5, 11), (12, 11), (19, 11), (8.5, 6), (15.5, 6)]:
        c.ellipse(x, y, 3.5, 3.5, P['woodlight'], flat=True)
        c.set(int(x), int(y), P['wood'][1])
    c.outline()
    return c


def signpost(icon_fn):
    c = Canvas(28, 34)
    c.shadow(14, 33, 5, 1)
    c.box(12, 14, 15, 33, P['wood'])
    c.box(2, 3, 23, 15, P['woodlight'])
    c.poly([(23, 3), (27, 9), (23, 15)], P['woodlight'], solid=P['woodlight'][2])
    mini = icon_fn()
    for y in range(32):
        for x in range(32):
            p = mini.get(x, y)
            if p is not None and x % 3 == 0 and y % 3 == 0:
                c.set(8 + x // 3, 4 + y // 3, p)
    c.outline()
    return c


def coin_bag():
    c = Canvas(32, 32)
    c.ellipse(16, 20, 10, 9, P['tan'])
    c.rect(12, 8, 19, 12, P['tan'][1])
    c.ellipse(16, 21, 3, 4, P['sun'], flat=True)
    c.outline()
    return c


def gate_arch(width=48):
    c = Canvas(width, 38)
    c.shadow(width / 2, 37, width / 2 - 2, 2)
    c.box(2, 8, 6, 37, P['wood']); c.box(width - 7, 8, width - 3, 37, P['wood'])
    c.box(0, 4, width - 1, 10, P['woodlight'])
    c.line(0, 4, width - 1, 4, P['woodlight'][3])
    # hanging lantern
    c.line(width // 2, 11, width // 2, 15, P['metal'][1])
    c.box(width // 2 - 2, 15, width // 2 + 2, 20, P['sun'])
    # vines
    for (x, y) in [(3, 14), (5, 20), (3, 27), (width - 5, 17), (width - 4, 24), (10, 9), (width - 12, 9)]:
        c.set(x, y, P['leaf'][2]); c.set(x + 1, y + 1, P['leaf'][3])
    c.outline()
    return c


def pond(bg, cx, cy, rx, ry, frozen=False):
    if frozen:
        bg.c.ellipse(cx, cy, rx + 3, ry + 3, [SNOW[1], SNOW[2], SNOW[2], SNOW[3]], flat=False, dither=False)
        for y in range(int(cy - ry), int(cy + ry) + 1):
            for x in range(int(cx - rx), int(cx + rx) + 1):
                d = ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2
                if d <= 1:
                    bg.c.set(x, y, ICE[1] if d > 0.82 else ICE[2])
        for (x0, y0, x1, y1) in [(-0.5, -0.3, 0.1, 0.2), (0.1, 0.2, 0.5, 0.1), (0.1, 0.2, 0.0, 0.6), (-0.3, -0.5, -0.5, -0.3)]:
            bg.c.line(cx + rx * x0, cy + ry * y0, cx + rx * x1, cy + ry * y1, ICE[0])
        bg.c.line(cx - rx * 0.6, cy - ry * 0.55, cx - rx * 0.2, cy - ry * 0.55, ICE[3])
        return
    # shore
    bg.c.ellipse(cx, cy, rx + 3, ry + 3, [P['tan'][1], P['tan'][2], P['tan'][2], P['tan'][3]], flat=False, dither=False)
    # water bands (top darker = depth reflects sky at bottom)
    for y in range(int(cy - ry), int(cy + ry) + 1):
        for x in range(int(cx - rx), int(cx + rx) + 1):
            d = ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2
            if d <= 1:
                t = (y - (cy - ry)) / (2 * ry)
                col = P['water'][1] if t < 0.35 else P['water'][2]
                if d > 0.82:
                    col = P['water'][0] if y < cy else P['water'][1]
                bg.c.set(x, y, col)
    # ripples
    for (dx, dy, l) in [(-rx * 0.4, -ry * 0.1, 6), (rx * 0.2, ry * 0.35, 8), (rx * 0.35, -ry * 0.45, 4)]:
        bg.c.line(cx + dx, cy + dy, cx + dx + l, cy + dy, P['water'][3])
    # lily pads
    for (dx, dy) in [(-rx * 0.5, ry * 0.4), (rx * 0.45, ry * 0.1)]:
        lp = Canvas(9, 6)
        lp.ellipse(4.5, 3, 4, 2.5, P['leaf'])
        lp.set(4, 1, None); lp.set(5, 2, None)
        lp.set(6, 2, P['red'][3])
        bg.prop(lp, x=int(cx + dx), y=int(cy + dy) + 3)
    # reeds
    for (dx, dy) in [(rx - 2, -ry + 2), (rx + 1, -ry + 6), (-rx + 1, ry - 1)]:
        rd = Canvas(7, 14)
        for k, h in enumerate((12, 9, 11)):
            rd.line(1 + k * 2, 13, 1 + k * 2, 13 - h, P['leaf2'][2])
        rd.rect(3, 2, 3, 5, P['wood'][1])
        rd.outline()
        bg.prop(rd, x=int(cx + dx), y=int(cy + dy) + 3)


# ======================================================================
# Farm
# ======================================================================
def farm(season='spring'):
    # World bounds of the old background: 17.92 x 10.24 around (-0.03, -0.03)
    bg = BG(574, 328, 17.92, 10.24, (-0.03, -0.03))
    bg.season = season
    ground = season_ground(season)
    grass(bg, base=ground)
    patch_col = {'winter': SNOW[1], 'spring': P['leaf2'][2], 'summer': SUMMER[1], 'autumn': AUT_ORANGE[1]}[season]
    patch_tuft = {'winter': SNOW, 'spring': P['leaf2'], 'summer': SUMMER, 'autumn': AUT_YELLOW}[season]
    for (wx, wy, rx, ry) in [(-4.5, 3.6, 70, 18), (5.6, -2.4, 60, 22), (6.8, 3.0, 40, 14), (-7.6, -0.6, 30, 30)]:
        x, y = bg.px(wx, wy)
        patch(bg, x, y, rx, ry, patch_col, ground, patch_tuft)

    # --- paths (pixel rects from world coordinates) ---
    def wr(x0, y0, x1, y1):
        a = bg.px(x0, y1)
        b = bg.px(x1, y0)
        return (a[0], a[1], b[0], b[1])
    path(bg, [
        wr(-9.2, -5.3, 9.2, -3.95),     # main road along the bottom (home <-> market)
        wr(0.35, -4.0, 1.55, 3.4),      # up past the field
        wr(-0.7, 3.0, 1.55, 3.9),       # jog
        wr(-0.7, 3.0, 0.55, 5.4),       # north exit (haggle)
        wr(1.5, -0.55, 2.7, 0.45),      # to the well
    ], TRAMPLED if season == 'winter' else P['tan'])

    # field soil underlay (the Dirt tiles render on top of this exact area)
    for (x0, y0, x1, y1) in [(-8, 0, -1, 3), (-6, -3, 0, 0)]:
        a, b = bg.px(x0, y1), bg.px(x1, y0)
        for y in range(a[1], b[1]):
            for x in range(a[0], b[0]):
                bg.c.set(x, y, P['dirt'][2])

    # --- tree line along the top (gap for the north path) ---
    def hedge_ramp(i):
        if season == 'winter':
            return P['leaf2']
        if season == 'summer':
            return SUMMER
        if season == 'autumn':
            return [AUT_ORANGE, AUT_RED, AUT_YELLOW, P['leaf2']][i % 4]
        return P['leaf2'] if i % 4 == 1 else P['leaf']
    blossom = P['jam'] if season == 'spring' else None
    back = [-9.0, -8.0, -7.0, -6.0, -5.0, -4.0, -3.0, -2.0, -1.2, 1.1, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0]
    for i, wx in enumerate(back):
        bg.prop(bush(36, 24, hedge_ramp(i)), wx=wx, wy=4.85)
    front = [-8.5, -7.4, -6.3, -5.3, -4.2, -3.1, -2.1, 2.3, 3.4, 4.5, 5.6, 6.6, 7.7, 8.7]
    for i, wx in enumerate(front):
        bg.prop(bush(32, 22, hedge_ramp(i + 2), blossom if i % 2 == 0 else None),
                wx=wx + (0.15 if i % 2 else -0.1), wy=4.35 + (0.08 if i % 3 == 0 else 0))
    bg.prop(gate_arch(52), wx=-0.07, wy=3.95)

    # --- farm plot fence (top and left of the field) ---
    a = bg.px(-8.35, 3.35)
    bg.prop(fence_h(bg.px(-0.75, 0)[0] - a[0]), x=(a[0] + bg.px(-0.75, 0)[0]) // 2, y=a[1])
    top = bg.px(-8.4, 3.2)
    bot = bg.px(-8.4, 0.0)
    bg.prop(fence_v(bot[1] - top[1]), x=top[0], y=bot[1])

    # --- home ---
    bg.prop(cottage(SNOW if season == 'winter' else None), wx=-7.75, wy=-3.85)
    bg.prop(mailbox(), wx=-6.25, wy=-3.85)
    bg.prop(log_pile(), wx=-9.0, wy=-2.05)

    # --- market signpost and pond ---
    bg.prop(signpost(coin_bag), wx=7.3, wy=-3.85)
    x, y = bg.px(7.05, -0.6)
    pond(bg, x, y, 30, 20, frozen=(season == 'winter'))

    # --- seasonal details, placed by hand ---
    spots = [(-5.6, -3.7), (-4.2, -3.6), (2.2, -3.65), (5.0, -3.6), (1.9, 3.5), (-2.0, 3.75),
             (6.1, 1.4), (2.9, -1.4), (8.5, 0.9), (-8.7, 0.6), (-3.0, -3.65)]
    if season == 'winter':
        bg.prop(snowman(), wx=-5.2, wy=-3.7)
        for (wx, wy) in spots[::3]:                      # red winter berries poking out of the snow
            x, y = bg.px(wx, wy)
            for dx, dy in ((0, 0), (2, 1), (-1, 2)):
                bg.c.set(x + dx, y + dy, P['red'][2]); bg.c.set(x + dx, y + dy + 1, P['leaf2'][1])
    elif season == 'autumn':
        for (wx, wy) in spots:
            x, y = bg.px(wx, wy)
            leaf_pile(bg, x, y, 7)
        bg.prop(pumpkin_deco(), wx=-6.75, wy=-3.85)
        bg.prop(pumpkin_deco(), wx=-8.9, wy=-3.9)
    else:
        if season == 'spring':
            cols = [P['jam'], P['white'], P['cloth'], P['sun'], P['jam'], P['white'], P['red'], P['cloth'], P['white'], P['jam'], P['sun']]
        else:
            cols = [P['red'], P['sun'], P['white'], P['cloth'], P['red'], P['sun'], P['white'], P['jam'], P['sun'], P['cloth'], P['white']]
        for (wx, wy), col in zip(spots, cols):
            x, y = bg.px(wx, wy)
            flower_cluster(bg, x, y, col, 5 if season == 'summer' else 4)
    return bg.image()


# ======================================================================
# Home (interior)
# ======================================================================
def home():
    bg = BG(590, 328, 18.43, 10.24, (0.0, -0.027))
    W, H = bg.w, bg.h
    wall_bottom = 58
    # floor: long horizontal planks (10 px tall), sparse staggered joints, grain lines
    fl = P['woodlight']
    for y in range(wall_bottom, H):
        row = (y - wall_bottom) // 10
        ry = (y - wall_bottom) % 10
        base = fl[2] if row % 3 != 1 else mix(fl[2], fl[1], 0.3)
        seg_len = 150 + (row * 41) % 60
        for x in range(W):
            c = base
            if ry == 0:
                c = fl[0]
            elif ry == 1:
                c = lighten_c(base, 0.12)
            joint = (x + row * 67) % seg_len
            if joint == 0 and ry > 0:
                c = fl[0]
            elif joint == 1 and ry > 0:
                c = lighten_c(base, 0.12)
            elif ry == 5 and (x * 3 + row * 11) % 37 < 9:   # wood grain
                c = mix(base, fl[1], 0.45)
            bg.c.set(x, y, c)
    # back wall: striped wallpaper + wainscot
    for y in range(0, wall_bottom):
        for x in range(W):
            stripe = (x // 6) % 2
            c = P['paper'][2] if stripe else mix(P['paper'][2], P['paper'][1], 0.3)
            if y > wall_bottom - 18:
                c = P['wood'][2] if (x % 12) else P['wood'][1]
                if y == wall_bottom - 18:
                    c = P['wood'][3]
            bg.c.set(x, y, c)
    bg.c.rect(0, wall_bottom - 1, W - 1, wall_bottom, P['wood'][0])
    # side walls
    for x in list(range(0, 10)) + list(range(W - 10, W)):
        for y in range(wall_bottom, H):
            bg.c.set(x, y, P['wood'][1] if x not in (9, W - 10) else P['wood'][0])
    # doorway on the right wall (exit portal at x 8.9, y 0)
    _, dy = bg.px(0, 0.0)
    bg.c.rect(W - 10, dy - 26, W - 1, dy + 26, P['dirt'][1])
    bg.c.rect(W - 10, dy - 26, W - 1, dy - 25, P['wood'][0])
    bg.c.rect(W - 34, dy - 14, W - 12, dy + 14, P['shirt'][1])     # door mat
    for yy in range(dy - 12, dy + 13, 4):
        bg.c.line(W - 32, yy, W - 14, yy, P['shirt'][2])

    # windows with daylight patches on the floor (banded, not gradients)
    for wxw in (-3.9, 3.7):
        x, _ = bg.px(wxw, 0)
        win = Canvas(40, 34)
        win.box(0, 0, 39, 33, P['wood'])
        win.box(3, 3, 36, 30, P['water'])
        for yy in range(3, 31):
            for xx in range(3, 37):
                if yy > 22:
                    win.set(xx, yy, P['leaf'][2] if (xx // 5 + yy // 4) % 3 else P['leaf'][1])
        win.line(19, 3, 19, 30, P['wood'][1]); win.line(3, 16, 36, 16, P['wood'][1])
        win.set(6, 6, P['water'][3]); win.set(7, 6, P['water'][3]); win.set(6, 7, P['water'][3])
        win.rect(0, 30, 39, 33, P['wood'][2])
        win.outline()
        bg.prop(win, x=x, y=40)
        for yy in range(wall_bottom + 2, wall_bottom + 70):
            k = yy - wall_bottom
            for xx in range(x - 16 + k // 3, x + 18 + k // 3):
                cur = bg.c.get(xx, yy)
                if cur is not None and (k < 50 or (xx + yy) % 2 == 0):
                    bg.c.set(xx, yy, lighten_c(cur, 0.16))

    # rug below the bed
    rx0, ry0 = bg.px(-2.6, -1.75)
    rx1, ry1 = bg.px(2.6, -3.9)
    bg.c.rect(rx0, ry0, rx1, ry1, P['cloth'][1])
    bg.c.rect(rx0 + 3, ry0 + 3, rx1 - 3, ry1 - 3, P['shirt'][1])
    bg.c.rect(rx0 + 6, ry0 + 6, rx1 - 6, ry1 - 6, P['shirt'][2])
    for xx in range(rx0 + 10, rx1 - 9, 10):
        bg.c.set(xx, (ry0 + ry1) // 2, P['sun'][2]); bg.c.set(xx + 1, (ry0 + ry1) // 2 - 1, P['sun'][2]); bg.c.set(xx + 1, (ry0 + ry1) // 2 + 1, P['sun'][2]); bg.c.set(xx + 2, (ry0 + ry1) // 2, P['sun'][2])
    for xx in range(rx0, rx1 + 1, 3):
        bg.c.set(xx, ry0 - 1, P['white'][2]); bg.c.set(xx, ry1 + 1, P['white'][2])

    # fireplace on the back wall (left)
    fp = Canvas(64, 60)
    fp.box(0, 8, 63, 59, P['stone'])
    for yy in range(12, 58, 6):
        for xx in range(((yy // 6) % 2) * 8, 64, 16):
            fp.line(xx, yy, xx + 13, yy, P['stone'][1])
            fp.set(xx, yy + 1, P['stone'][3])
    fp.box(16, 26, 47, 59, [P['outline']] * 4)
    fp.rect(16, 26, 47, 59, (30, 18, 14, 255))
    fp.thick_line(20, 57, 44, 52, P['wood'][2], 3)
    fp.poly([(22, 55), (28, 40), (32, 32), (37, 41), (42, 55)], P['fire'], light_axis='y', invert=True)
    fp.box(0, 4, 63, 9, P['wood'])
    fp.outline()
    bg.prop(fp, wx=-6.9, wy=2.35)

    # bookshelf on the back wall (right)
    bs = Canvas(48, 66)
    bs.box(0, 0, 47, 65, P['wood'])
    bs.rect(3, 3, 44, 62, P['wood'][0])
    cols = [P['shirt'], P['cloth'], P['leaf'], P['sun'], P['jam'], P['denim']]
    for shelf, sy in enumerate((5, 25, 45)):
        x = 4
        i = shelf
        while x < 43:
            w = 3 + (i * 7) % 3
            h = 12 + (i * 5) % 6
            c = cols[i % len(cols)]
            bs.rect(x, sy + 18 - h, x + w - 1, sy + 17, c[2])
            bs.line(x, sy + 18 - h, x, sy + 17, c[3])
            x += w + (1 if i % 4 else 2)
            i += 1
        bs.rect(3, sy + 18, 44, sy + 19, P['wood'][2])
    bs.outline()
    bg.prop(bs, wx=6.6, wy=2.15)

    # table with chairs and a mug (left, mid)
    tb = Canvas(60, 44)
    tb.shadow(30, 42, 27, 2.5)
    tb.box(4, 6, 55, 26, P['woodlight'])
    tb.rect(4, 26, 55, 29, P['wood'][1])
    tb.box(7, 29, 10, 41, P['wood']); tb.box(49, 29, 52, 41, P['wood'])
    tb.box(24, 10, 29, 16, P['white']); tb.set(30, 12, P['white'][1]); tb.set(30, 13, P['white'][1])
    tb.ellipse(40, 14, 6, 3, P['red'])
    tb.outline()
    bg.prop(tb, wx=-5.6, wy=-2.35)
    for (wx, wy) in [(-7.25, -2.2), (-3.95, -2.2)]:
        ch = Canvas(16, 26)
        ch.box(1, 0, 14, 12, P['wood'])
        ch.box(1, 12, 14, 17, P['woodlight'])
        ch.box(2, 17, 4, 25, P['wood']); ch.box(11, 17, 13, 25, P['wood'])
        ch.outline()
        bg.prop(ch, wx=wx, wy=wy)

    # plants in the corners
    for (wx, wy) in [(-8.2, 2.0), (4.9, -4.4), (-8.2, -4.4)]:
        pl = Canvas(22, 30)
        pl.shadow(11, 29, 8, 1.5)
        pl.box(5, 18, 16, 28, P['copper'])
        pl.ellipse(7, 12, 6, 6, P['leaf']); pl.ellipse(15, 11, 6, 7, P['leaf']); pl.ellipse(11, 7, 6, 6, P['leaf'])
        pl.outline()
        bg.prop(pl, wx=wx, wy=wy)

    # wall decorations: picture frame and clock
    pic = Canvas(26, 20)
    pic.box(0, 0, 25, 19, P['sun'])
    pic.rect(2, 2, 23, 17, P['water'][2])
    pic.rect(2, 12, 23, 17, P['leaf'][2])
    pic.poly([(4, 12), (10, 5), (16, 12)], P['stone'], solid=P['stone'][2])
    pic.set(19, 5, P['sun'][3]); pic.set(20, 5, P['sun'][3]); pic.set(19, 6, P['sun'][3])
    pic.outline()
    bg.prop(pic, wx=-0.1, wy=4.75)
    clk = Canvas(16, 16)
    clk.ellipse(8, 8, 7, 7, P['white'])
    clk.line(8, 8, 8, 3, P['outline']); clk.line(8, 8, 11, 9, P['outline'])
    clk.outline()
    bg.prop(clk, wx=-5.0, wy=4.55)
    return bg.image()


# ======================================================================
# Market square
# ======================================================================
def cobble(bg, y0):
    """Rounded cobblestones: nearest jittered point per pixel, grout between stones, top-left light."""
    cw, ch = 11, 8
    pts = {}
    for gy in range(-1, (bg.h - y0) // ch + 2):
        for gx in range(-1, bg.w // cw + 2):
            off = (gy % 2) * cw // 2
            jx = _h(gx, gy, 3) % 4 - 1.5
            jy = _h(gx, gy, 5) % 3 - 1
            pts[(gx, gy)] = (gx * cw + off + cw / 2 + jx, y0 + gy * ch + ch / 2 + jy)
    tones = [mix(P['stone'][2], P['tan'][2], 0.42), mix(P['stone'][2], P['tan'][2], 0.3), mix(P['stone'][2], P['tan'][3], 0.4)]
    grout = mix(P['stone'][1], P['tan'][1], 0.45)
    owner = {}
    for y in range(y0, bg.h):
        gy0 = (y - y0) // ch
        for x in range(bg.w):
            best = second = 1e9
            bk = None
            for gy in (gy0 - 1, gy0, gy0 + 1):
                gx0 = (x - (gy % 2) * cw // 2) // cw
                for gx in (gx0 - 1, gx0, gx0 + 1):
                    p = pts.get((gx, gy))
                    if p is None:
                        continue
                    d = ((x + 0.5 - p[0]) / cw) ** 2 + ((y + 0.5 - p[1]) / ch) ** 2
                    if d < best:
                        second, best, bk = best, d, (gx, gy)
                    elif d < second:
                        second = d
            owner[(x, y)] = bk
            if second - best < 0.09:
                bg.c.set(x, y, grout)
            else:
                bg.c.set(x, y, tones[_h(*bk, 9) % 3])
    # shading per stone: light where the pixel above-left is grout, dark where below-right is grout
    for y in range(y0 + 1, bg.h - 1):
        for x in range(1, bg.w - 1):
            cur = bg.c.get(x, y)
            if cur == grout:
                continue
            if bg.c.get(x, y - 1) == grout or bg.c.get(x - 1, y) == grout:
                bg.c.set(x, y, lighten_c(cur, 0.12))
            elif bg.c.get(x, y + 1) == grout or bg.c.get(x + 1, y) == grout:
                bg.c.set(x, y, darken_c(cur, 0.9))


def shopfront(width, wall, sign_icon, awning):
    c = Canvas(width, 84)
    c.box(0, 14, width - 1, 83, wall)
    for y in range(20, 83, 6):
        c.line(1, y, width - 2, y, wall[1])
    # roof edge
    c.box(0, 0, width - 1, 14, P['wood'])
    for x in range(0, width, 6):
        c.line(x, 1, x, 13, P['wood'][1])
    # door
    dx = width // 2 - 9
    c.box(dx, 52, dx + 18, 83, P['wood'])
    c.line(dx + 9, 53, dx + 9, 82, P['wood'][1])
    c.set(dx + 15, 68, P['sun'][2])
    # windows
    for wx in (8, width - 30):
        if wx + 22 >= dx and wx <= dx + 18:
            continue
        c.box(wx, 50, wx + 21, 70, P['wood'])
        c.box(wx + 2, 52, wx + 19, 68, P['water'])
        c.line(wx + 11, 52, wx + 11, 68, P['wood'][1])
        c.set(wx + 4, 54, P['water'][3]); c.set(wx + 5, 54, P['water'][3])
    # striped awning
    for x in range(2, width - 2):
        col = awning[2] if (x // 7) % 2 == 0 else P['white'][3]
        for y in range(36, 46):
            c.set(x, y, col if y < 45 else darken_c(col, 0.7))
        if x % 7 == 3:
            c.set(x, 46, col)
    # hanging sign with icon
    sx = width // 2
    c.box(sx - 11, 18, sx + 10, 33, P['woodlight'])
    mini = sign_icon()
    for y in range(32):
        for x in range(32):
            p = mini.get(x, y)
            if p is not None and x % 3 == 0 and y % 3 == 0:
                c.set(sx - 5 + x // 3, 21 + y // 3, p)
    c.outline()
    return c


def crate_of(fn):
    c = Canvas(26, 22)
    c.shadow(13, 21, 12, 1.5)
    c.box(1, 8, 24, 20, P['wood'])
    c.line(1, 14, 24, 14, P['wood'][1])
    mini = fn()
    for y in range(32):
        for x in range(32):
            p = mini.get(x, y)
            if p is not None and x % 2 == 0 and y % 2 == 0 and y < 22:
                c.set(5 + x // 2, y // 2, p)
    c.outline()
    return c


def barrel():
    c = Canvas(18, 24)
    c.shadow(9, 23, 8, 1.5)
    c.box(2, 3, 15, 22, P['wood'])
    c.ellipse(8.5, 3.5, 6.5, 2.5, P['woodlight'], flat=True)
    for y in (7, 17):
        c.line(2, y, 15, y, P['metal'][1])
    c.outline()
    return c


def bunting(bg, x0, x1, y, sag=10):
    cols = [P['red'][2], P['sun'][2], P['cloth'][2], P['leaf'][2]]
    n = x1 - x0
    for i in range(n):
        x = x0 + i
        t = i / n
        yy = y + int(sag * 4 * t * (1 - t))
        bg.c.set(x, yy, P['wood'][0])
        if i % 12 == 4:
            col = cols[(i // 12) % 4]
            for k in range(6):
                for j in range(-3 + k // 2, 4 - k // 2):
                    bg.c.set(x + j, yy + 1 + k, col if j > -3 + k // 2 else darken_c(col, 0.75))


def market(season='summer'):
    bg = BG(585, 328, 18.28, 10.24, (0.09, -0.08))
    bg.season = season
    cobble(bg, 0)
    if season == 'winter':
        # snow drifts along the walls and the bottom edge, packed snow where people walk
        for y in range(bg.h):
            for x in range(bg.w):
                edge = min(y - 88, bg.h - 1 - y) if y >= 88 else 0
                wob = (_h(x // 6, 3, 11) % 5)
                if (y >= 88 and edge < 12 + wob * 2) or (_h(x, y, 21) % 17 == 0 and y >= 88):
                    bg.c.set(x, y, SNOW[2] if edge < 8 + wob else SNOW[1])
        for k in range(30):   # a few snow patches out in the square
            cx, cy = 30 + (k * 151) % 525, 120 + (k * 89) % 190
            for y in range(cy - 4, cy + 5):
                for x in range(cx - 9, cx + 10):
                    if ((x - cx) / 9) ** 2 + ((y - cy) / 4) ** 2 <= 1 and (x + y) % 5:
                        bg.c.set(x, y, SNOW[2])
    elif season == 'autumn':
        for k in range(46):
            leaf_pile(bg, 20 + (k * 131) % 545, 110 + (k * 67) % 200, 3)

    # building row along the top
    bg.prop(shopfront(150, P['woodlight'], items.bread, P['shirt']), x=95, y=88)
    bg.prop(shopfront(120, P['paper'], items.hoe, P['cloth']), x=245, y=88)
    bg.prop(shopfront(110, mix_ramp(P['shirt'], P['tan'], 0.55), items.strawberry, P['leaf']), x=530, y=88)

    # merchant's stall (merchant sprite stands at world 3.92, 3.01)
    mx, my = bg.px(3.92, 3.01)
    stall = Canvas(120, 86)
    for x in (4, 113):
        stall.box(x, 18, x + 3, 85, P['wood'])
    for x in range(0, 120):
        col = P['sun'][2] if (x // 10) % 2 == 0 else P['white'][3]
        for y in range(6, 22):
            stall.set(x, y, col if y < 20 else darken_c(col, 0.7))
        if x % 10 == 5:
            stall.set(x, 22, col); stall.set(x, 23, col)
    stall.box(0, 2, 119, 6, P['wood'])
    stall.outline()
    bg.prop(stall, x=mx, y=my + 40)

    bunting(bg, 0, bg.w, 92, 12)

    # crates, barrels and planters (kept to the edges so the square stays walkable)
    for (wx, wy, fn) in [(6.2, 1.25, items.apple), (7.0, 1.25, items.corn), (1.6, 1.4, items.carrot)]:
        bg.prop(crate_of(fn), wx=wx, wy=wy)
    for (wx, wy) in [(-8.3, 2.0), (-7.7, 1.9), (8.4, -4.2)]:
        bg.prop(barrel(), wx=wx, wy=wy)
    for wx in (-6.0, -3.0, 0.0, 3.0, 6.0):
        pl = Canvas(30, 20)
        pl.shadow(15, 19, 14, 1.5)
        pl.box(1, 10, 28, 19, P['wood'])
        foliage = {'winter': P['leaf2'], 'autumn': AUT_ORANGE, 'summer': SUMMER}.get(season, P['leaf'])
        pl.ellipse(8, 8, 6, 5, foliage); pl.ellipse(21, 8, 6, 5, foliage); pl.ellipse(14.5, 5, 6, 5, foliage)
        if season != 'winter':
            petals = [P['jam'][2], P['white'][3], P['cloth'][3]] if season == 'spring' else [P['red'][2], P['sun'][2], P['white'][3]]
            if season == 'autumn':
                petals = [AUT_RED[2], AUT_YELLOW[2], AUT_ORANGE[3]]
            for (x, y) in [(6, 5), (14, 2), (22, 6), (11, 8), (18, 9)]:
                pl.set(x, y, petals[(x + y) % 3])
        pl.outline()
        bg.prop(pl, wx=wx, wy=-4.6)

    # compass mosaic in the middle of the square
    cx, cy = bg.px(0.4, -1.4)
    ring_a, ring_b = P['woodlight'][2], P['tan'][3]
    for y in range(cy - 32, cy + 33):
        for x in range(cx - 48, cx + 49):
            d = ((x - cx) / 46) ** 2 + ((y - cy) / 30) ** 2
            if 0.78 <= d <= 1.0:
                bg.c.set(x, y, ring_a if ((x - cx) // 4 + (y - cy) // 3) % 2 else ring_b)
    for (dx, dy) in ((0, -1), (0, 1), (-1, 0), (1, 0)):
        for k in range(0, 24 if dy else 36):
            w = max(0, 3 - k // (8 if dy else 12))
            for j in range(-w, w + 1):
                x = cx + dx * k + (j if dy else 0)
                y = cy + dy * k * 0.7 + (0 if dy else j * 0.7)
                bg.c.set(x, y, P['shirt'][2] if (dx, dy) == (0, -1) else P['woodlight'][1])
    bg.c.rect(cx - 2, cy - 2, cx + 2, cy + 2, P['sun'][2])
    bg.c.set(cx - 1, cy - 1, P['sun'][3])
    return bg.image()


def mix_ramp(a, b, t):
    return [mix(a[i], b[i], t) for i in range(4)]


# ======================================================================
# Haggle minigame (close-up at the counter)
# ======================================================================
def minigame():
    bg = BG(574, 321, 17.95, 10.04, (-0.035, -0.079))
    W, H = bg.w, bg.h
    counter_top = 214
    # back wall: vertical planks
    for y in range(0, counter_top):
        for x in range(W):
            px = x % 18
            c = P['wood'][2] if (x // 18) % 2 == 0 else mix(P['wood'][2], P['wood'][1], 0.4)
            if px == 0:
                c = P['wood'][0]
            elif px == 1:
                c = P['wood'][3]
            if (y + (x // 18) * 23) % 61 == 0 and px > 2:
                c = P['wood'][1]
            bg.c.set(x, y, c)
    # shelves with jars and sacks
    jar_cols = [P['jam'], P['soup'], P['leaf'], P['sun'], P['red']]
    for sy in (46, 104):
        bg.c.rect(250, sy, W - 6, sy + 4, P['woodlight'][2])
        bg.c.line(250, sy, W - 6, sy, P['woodlight'][3])
        bg.c.rect(250, sy + 5, W - 6, sy + 6, P['wood'][0])
        for i, x in enumerate(range(262, W - 30, 34)):
            jar = Canvas(20, 26)
            col = jar_cols[(i + sy) % len(jar_cols)]
            jar.box(3, 7, 16, 25, P['glass'])
            jar.box(5, 12, 14, 24, col)
            jar.box(2, 3, 17, 7, P['shirt'] if i % 2 else P['cloth'])
            jar.set(5, 9, P['white'][3])
            jar.outline()
            bg.prop(jar, x=x, y=sy)
    # window with the market outside
    win = Canvas(120, 70)
    win.box(0, 0, 119, 69, P['wood'])
    win.rect(4, 4, 115, 65, P['water'][2])
    win.rect(4, 44, 115, 65, mix(P['stone'][2], P['tan'][2], 0.4))
    for x in range(4, 116):
        for y in range(28, 44):
            if (x // 14) % 2 == 0 and y > 28 + (x * 7) % 9:
                win.set(x, y, mix(P['woodlight'][2], P['water'][2], 0.35))
    for x in range(4, 116):
        if (x // 8) % 2 == 0:
            win.set(x, 30, P['red'][2]); win.set(x, 31, P['red'][2])
    win.line(60, 4, 60, 65, P['wood'][1]); win.line(4, 34, 115, 34, P['wood'][1])
    win.set(10, 9, P['white'][3]); win.set(11, 9, P['white'][3]); win.set(10, 10, P['white'][3])
    win.outline()
    bg.prop(win, x=150, y=150)

    # the merchant (big, behind the counter)
    m = Canvas(170, 150)
    # shoulders and torso (shirt), apron with straps
    m.poly([(10, 150), (22, 112), (50, 98), (120, 98), (148, 112), (160, 150)], P['tan'], light_axis='x')
    m.poly([(52, 150), (58, 108), (112, 108), (118, 150)], P['apron'], light_axis='x')
    m.thick_line(64, 108, 60, 98, P['apron'][2], 3); m.thick_line(106, 108, 110, 98, P['apron'][2], 3)
    m.box(80, 120, 90, 128, P['apron'])                      # apron pocket
    # neck and head
    m.box(72, 86, 98, 100, P['skin'])
    m.ellipse(85, 62, 33, 34, P['skin'])
    m.ellipse(52, 64, 5, 8, P['skin']); m.ellipse(118, 64, 5, 8, P['skin'])
    # eyes and brows
    for ex in (72, 98):
        m.rect(ex - 2, 58, ex + 1, 62, P['outline'])
        m.set(ex - 1, 59, P['white'][3])
        m.rect(ex - 5, 52, ex + 4, 53, P['hair'][1])
    # small blush, nose, moustache, smile
    for ex in (64, 106):
        m.rect(ex - 3, 70, ex + 3, 71, P['red'][3])
    m.rect(83, 62, 87, 70, P['skin'][1]); m.rect(84, 62, 86, 68, P['skin'][2]); m.set(84, 63, P['skin'][3])
    m.poly([(66, 77), (76, 72), (85, 75), (94, 72), (104, 77), (96, 81), (85, 79), (74, 81)], P['hair'], light_axis='y')
    m.rect(79, 85, 91, 86, P['skin'][0]); m.set(78, 84, P['skin'][0]); m.set(92, 84, P['skin'][0])
    # cap
    m.ellipse(85, 33, 36, 14, P['leaf2'])
    m.rect(46, 38, 124, 43, P['leaf2'][1])
    m.line(46, 38, 124, 38, P['leaf2'][3])
    m.outline()
    bg.prop(m, x=150, y=counter_top + 6)

    # counter
    for y in range(counter_top, H):
        for x in range(W):
            if y < counter_top + 14:
                c = P['woodlight'][3] if y == counter_top else (P['woodlight'][2] if (x + y // 2) % 40 else P['woodlight'][1])
            elif y == counter_top + 14:
                c = P['wood'][0]
            else:
                c = P['wood'][2] if ((x // 30) % 2 == 0) else mix(P['wood'][2], P['wood'][1], 0.4)
                if x % 30 == 0:
                    c = P['wood'][0]
                if (y - counter_top) % 40 == 20:
                    c = P['wood'][1]
            bg.c.set(x, y, c)
    # things on the counter: scale, coins, basket
    sc = Canvas(60, 50)
    sc.box(28, 10, 31, 46, P['copper'])
    sc.box(16, 44, 43, 49, P['copper'])
    sc.line(6, 12, 53, 12, P['copper'][2])
    for (bx, by) in ((6, 26), (53, 22)):
        sc.line(bx - 6, by, bx, 12, P['metal'][1]); sc.line(bx + 6, by, bx, 12, P['metal'][1])
        sc.ellipse(bx, by + 2, 8, 3, P['copper'])
    sc.ellipse(53, 20, 4, 3, P['sun'], flat=True)
    sc.outline()
    bg.prop(sc, x=420, y=counter_top + 6)
    for i, (x, y) in enumerate([(320, 218), (328, 216), (335, 219), (324, 221), (340, 222)]):
        cn = Canvas(9, 5)
        cn.ellipse(4.5, 2.5, 4, 2, P['sun'])
        cn.outline()
        bg.prop(cn, x=x, y=y)
    bk = Canvas(50, 34)
    bk.poly([(2, 14), (47, 14), (42, 33), (7, 33)], P['straw'], light_axis='x')
    for y in range(17, 33, 4):
        bk.line(5, y, 44, y, P['straw'][1])
    for (fx, fy, fn) in [(14, 10, items.carrot), (26, 8, items.apple), (36, 10, items.potato)]:
        mini = fn()
        for yy in range(32):
            for xx in range(32):
                p = mini.get(xx, yy)
                if p is not None and xx % 3 == 0 and yy % 3 == 0:
                    bk.set(fx - 5 + xx // 3, fy - 6 + yy // 3, p)
    bk.outline()
    bg.prop(bk, x=505, y=counter_top + 10)
    return bg.image()


# ======================================================================
# The gray city (intro / memory)
# ======================================================================
def city():
    bg = BG(570, 325, 17.81, 10.15, (0, 0))
    W, H = bg.w, bg.h
    grays = [(74, 78, 86, 255), (96, 100, 108, 255), (122, 126, 132, 255), (150, 152, 158, 255), (178, 180, 184, 255)]
    # sky bands
    for y in range(H):
        band = min(4, y * 5 // H)
        c = grays[4 - band // 2] if band < 4 else grays[3]
        for x in range(W):
            if band in (1, 3) and (x + y) % 2 == 0:
                c2 = grays[4 - (band + 1) // 2]
            else:
                c2 = c
            bg.c.set(x, y, c2)

    def skyline(seed, base_y, min_h, max_h, color, win_color, lit):
        x = -4
        i = seed
        while x < W:
            w = 26 + (i * 17) % 34
            h = min_h + (i * 37) % (max_h - min_h)
            top = base_y - h
            for yy in range(top, H):
                for xx in range(x, min(W, x + w)):
                    bg.c.set(xx, yy, color)
            bg.c.line(x, top, x + w - 1, top, lighten_c(color, 0.12))
            if i % 3 == 0:
                bg.c.rect(x + w // 2 - 1, top - 10, x + w // 2, top - 1, color)
            for wy in range(top + 6, H - 10, 9):
                for wx in range(x + 4, x + w - 5, 7):
                    on = ((wx * 3 + wy * 5 + i) % 11) < lit
                    bg.c.rect(wx, wy, wx + 2, wy + 3, win_color if on else darken_c(color, 0.85))
            x += w + 2
            i += 1

    skyline(3, 170, 40, 130, grays[2], (168, 168, 150, 255), 2)
    skyline(8, 230, 60, 170, grays[1], (186, 182, 150, 255), 2)
    skyline(5, 300, 80, 200, grays[0], (200, 194, 150, 255), 1)
    # street
    bg.c.rect(0, 300, W - 1, H - 1, (60, 62, 68, 255))
    for x in range(0, W, 40):
        bg.c.rect(x, 311, x + 18, 312, (140, 140, 132, 255))
    # rain
    for k in range(260):
        x = (k * 97) % W
        y = (k * 53) % H
        bg.c.set(x, y, (200, 204, 210, 255)); bg.c.set(x - 1, y + 1, (170, 174, 180, 255))
    return bg.image()


BACKGROUNDS = {
    'bg_farm': farm,
    'bg_home': home,
    'bg_market': market,
    'bg_minigame': lambda: __import__('haggle').scene(),
    'bg_city': city,
}
