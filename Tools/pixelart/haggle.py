"""Haggle minigame scene + the merchant, drawn the way a pixel artist would.

- The merchant is a hand-placed pixel map (left half mirrored, then asymmetric details),
  cel-shaded with hard 3-tone bands from a top-left light, and a dark brown outline.
- The scene is authored at half resolution and scaled 2x as a whole, so it is one
  consistent chunky pixel grid (it is a close-up screen with no world sprites in it).
- Every prop on the wall and counter is different and placed by hand.
"""
from pixel import Canvas, P, darken_c, lighten_c

OUTLINE = (46, 26, 20, 255)


# ----------------------------------------------------------------------
# Pixel-map character with automatic hard cel shading
# ----------------------------------------------------------------------
class PixelMap:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.g = [['.'] * w for _ in range(h)]

    def seg(self, y, x0, x1, ch, mirror=True):
        for x in range(x0, x1 + 1):
            self.put(x, y, ch)
            if mirror:
                self.put(self.w - 1 - x, y, ch)

    def put(self, x, y, ch):
        if 0 <= x < self.w and 0 <= y < self.h:
            self.g[y][x] = ch

    def get(self, x, y):
        if 0 <= x < self.w and 0 <= y < self.h:
            return self.g[y][x]
        return '.'

    def shade(self, groups):
        """groups: base_char -> (shadow_char, light_char).
        Hard bands: 1-2 px shadow on the right/bottom edge of a region, 1 px light on the top/left edge."""
        out = [row[:] for row in self.g]
        for y in range(self.h):
            for x in range(self.w):
                ch = self.g[y][x]
                if ch not in groups:
                    continue
                dark, light = groups[ch]
                same = lambda xx, yy: self.get(xx, yy) == ch
                if not same(x + 1, y) or not same(x + 2, y) and not same(x + 1, y + 1) or not same(x, y + 1):
                    out[y][x] = dark
                elif not same(x - 1, y) or not same(x, y - 1):
                    if light:
                        out[y][x] = light
        self.g = out

    def canvas(self, palette, outline=OUTLINE):
        c = Canvas(self.w + 2, self.h + 2)
        for y in range(self.h):
            for x in range(self.w):
                ch = self.g[y][x]
                if ch != '.':
                    c.set(x + 1, y + 1, palette[ch])
        c.outline(color=outline)
        return c


MERCHANT_PALETTE = {
    # cap (olive green)
    'g': P['leaf2'][2], 'G': P['leaf2'][1], 'h': P['leaf2'][3], 'k': P['leaf2'][1], 'K': P['leaf2'][0],
    # skin
    's': P['skin'][2], 'S': P['skin'][1], 'l': P['skin'][3], 'n': P['skin'][0], 'r': (226, 128, 112, 255),
    # hair / moustache
    'H': P['hair'][1], 'm': P['hair'][2], 'M': P['hair'][1], 'L': P['hair'][3],
    # eyes / mouth
    'E': OUTLINE, 'w': (250, 246, 236, 255), 'o': (92, 40, 32, 255),
    # shirt (blue) / apron (cream) / brass
    't': P['cloth'][2], 'T': P['cloth'][1], 'u': P['cloth'][3],
    'a': P['apron'][2], 'A': P['apron'][1], 'b': P['apron'][3],
    'B': P['sun'][2], 'y': P['sun'][2], 'Y': P['sun'][1], 'p': P['red'][2],
}


def merchant_bust():
    """48 x 58 bust of the merchant (shoulders cut by the counter)."""
    m = PixelMap(48, 58)
    # --- flat cap ---
    for y, x0 in enumerate([14, 11, 9, 8, 7, 6, 6]):
        m.seg(y, x0, 23, 'g')
    m.seg(7, 5, 23, 'k')
    m.seg(8, 5, 23, 'k')
    for y in range(2, 7):            # panel seams
        m.seg(y, 15, 15, 'G')
    m.put(23, 0, 'h'); m.put(24, 0, 'h')   # button
    m.seg(9, 10, 23, 'K')            # brim front
    # --- face ---
    for y in range(10, 27):
        m.seg(y, 8, 23, 's')
    for y, x0 in [(27, 10), (28, 11), (29, 13), (30, 15)]:
        m.seg(y, x0, 23, 's')
    m.seg(10, 9, 23, 'S')            # shadow under the brim
    for y in range(10, 18):          # sideburns
        m.seg(y, 8, 8, 'H')
    for y in range(10, 14):
        m.seg(y, 9, 9, 'H')
    for y in range(15, 21):          # ears
        m.seg(y, 6, 7, 's')
    for y in range(16, 20):
        m.seg(y, 7, 7, 'S')
    # brows, eyes
    m.seg(12, 13, 16, 'H'); m.seg(13, 12, 17, 'H')
    for y in range(15, 18):
        m.seg(y, 14, 15, 'E')
    m.put(14, 15, 'w'); m.put(33, 15, 'w')
    m.seg(18, 14, 15, 'S')
    # cheeks
    m.seg(20, 11, 13, 'r'); m.seg(21, 12, 13, 'r')
    # nose
    for y in range(19, 22):
        m.seg(y, 21, 21, 'S')
    m.seg(22, 22, 23, 'n')
    # moustache + smile
    m.seg(22, 16, 20, 'm')
    m.seg(23, 13, 23, 'm')
    m.seg(24, 12, 23, 'm')
    m.seg(25, 12, 15, 'M'); m.seg(25, 16, 23, 'o')
    m.seg(26, 21, 23, 'p')
    m.seg(27, 18, 23, 'S')
    # --- neck and collar ---
    for y in range(31, 34):
        m.seg(y, 17, 23, 'S')
    m.seg(31, 13, 15, 't'); m.seg(32, 12, 16, 't'); m.seg(33, 11, 17, 't')
    m.seg(34, 20, 23, 'S'); m.seg(35, 22, 23, 'S')
    # --- body ---
    m.seg(34, 7, 19, 't'); m.seg(35, 4, 21, 't')
    for y in range(36, 58):
        m.seg(y, 0 if y > 37 else (2 if y == 36 else 1), 23, 't')
    for y in range(36, 41):
        m.seg(y, 23, 23, 'T')        # placket
    m.put(23, 38, 'u'); m.put(24, 38, 'u')
    for y in range(42, 58):          # sleeve fold
        m.seg(y, 5, 5, 'T')
    # --- apron ---
    for y in range(40, 58):
        m.seg(y, 13, 23, 'a')
    m.seg(40, 13, 23, 'b')
    for (x, y) in [(9, 35), (10, 36), (10, 37), (11, 38), (12, 39)]:
        m.seg(y, x, x + 1, 'a')
    m.seg(41, 13, 13, 'B')
    m.seg(47, 17, 23, 'A')
    m.seg(53, 17, 23, 'A')
    for y in range(47, 54):
        m.seg(y, 17, 17, 'A')

    m.shade({'s': ('S', 'l'), 't': ('T', 'u'), 'a': ('A', 'b'), 'g': ('G', 'h')})

    # --- asymmetric details (after shading) ---
    for i, (x, y) in enumerate([(41, 11), (40, 12), (40, 13), (39, 14), (39, 15), (38, 16)]):   # pencil behind ear
        m.put(x, y, 'y' if i < 5 else 'o')
    m.put(42, 10, 'p')
    for y in range(43, 47):          # pencil in the apron pocket
        m.put(20, y, 'y')
    m.put(20, 42, 'o')
    m.put(22, 19, 'l'); m.put(22, 20, 'l')   # nose highlight (light from the left)
    m.put(11, 4, 'h'); m.put(12, 3, 'h'); m.put(13, 3, 'h')   # cap highlight
    return m.canvas(MERCHANT_PALETTE)


def merchant_small():
    """32 x 48 world sprite of the same merchant standing behind his crate."""
    m = PixelMap(30, 46)
    for y, x0 in enumerate([10, 8, 6, 6]):
        m.seg(y, x0, 14, 'g')
    m.seg(4, 4, 14, 'k')
    m.seg(5, 7, 14, 'K')
    for y in range(6, 16):
        m.seg(y, 7, 14, 's')
    m.seg(16, 9, 14, 's'); m.seg(17, 11, 14, 's')
    m.seg(6, 7, 14, 'S')
    for y in range(6, 11):
        m.seg(y, 7, 7, 'H')
    for y in range(9, 13):
        m.seg(y, 6, 6, 's')
    m.seg(8, 10, 12, 'H')
    m.seg(9, 11, 11, 'E'); m.seg(10, 11, 11, 'E')
    m.seg(12, 9, 10, 'r')
    m.seg(12, 13, 14, 'S')
    m.seg(13, 10, 14, 'm'); m.seg(14, 9, 14, 'm')
    m.seg(15, 12, 14, 'o')
    m.seg(18, 12, 14, 'S')
    m.seg(19, 7, 14, 't'); m.seg(20, 4, 14, 't')
    for y in range(21, 34):
        m.seg(y, 2, 14, 't')
    for y in range(23, 34):
        m.seg(y, 9, 14, 'a')
    m.seg(23, 9, 14, 'b')
    for (x, y) in [(6, 20), (7, 21), (8, 22)]:
        m.seg(y, x, x, 'a')
    for y in range(24, 34):
        m.seg(y, 4, 4, 'T')
    m.shade({'s': ('S', 'l'), 't': ('T', 'u'), 'a': ('A', 'b'), 'g': ('G', 'h')})
    m.put(13, 11, 'l')
    body = m.canvas(MERCHANT_PALETTE)

    c = Canvas(32, 48)
    for y in range(body.h):
        for x in range(body.w):
            p = body.get(x, y)
            if p is not None:
                c.set(x, y, p)
    # crate of produce in front
    c.box(2, 33, 29, 46, P['wood'])
    c.line(2, 39, 29, 39, P['wood'][1])
    c.line(2, 33, 29, 33, P['wood'][3])
    for (x, col) in [(6, P['red']), (11, P['orange']), (16, P['yellow']), (21, P['red']), (25, P['leaf'])]:
        c.rect(x - 2, 30, x + 1, 32, col[2])
        c.set(x - 2, 30, col[3])
        c.set(x + 1, 32, col[1])
    c.outline(color=OUTLINE)
    return c


# ----------------------------------------------------------------------
# Scene props (native half resolution)
# ----------------------------------------------------------------------
def flat_rect(c, x0, y0, x1, y1, r, light=True):
    """Flat fill with a 1px light edge on top/left and dark edge on bottom/right."""
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            col = r[2]
            if light and (y == y0 or x == x0):
                col = r[3]
            if y == y1 or x == x1:
                col = r[1]
            c.set(x, y, col)


def jar(c, x, y_bottom, w, h, fill, lid):
    flat_rect(c, x, y_bottom - h, x + w - 1, y_bottom - 1, P['glass'])
    flat_rect(c, x + 1, y_bottom - h + 3, x + w - 2, y_bottom - 2, fill, light=False)
    flat_rect(c, x, y_bottom - h - 2, x + w - 1, y_bottom - h, lid)
    c.set(x + 1, y_bottom - h + 1, (255, 255, 255, 255))


def sack(c, x, y_bottom):
    for y in range(y_bottom - 15, y_bottom):
        t = (y - (y_bottom - 15)) / 15
        half = int(4 + t * 4) if t < 0.7 else 7
        for xx in range(x + 8 - half, x + 8 + half):
            col = P['tan'][3] if xx < x + 6 else (P['tan'][2] if xx < x + 12 else P['tan'][1])
            c.set(xx, y, col)
    c.rect(x + 5, y_bottom - 16, x + 10, y_bottom - 15, P['tan'][1])  # tie
    for (dx, dy) in [(8, -9), (7, -8), (9, -8), (8, -7), (8, -6), (8, -5)]:  # wheat mark
        c.set(x + dx, y_bottom + dy, P['straw'][1])


def cheese(c, x, y_bottom):
    for y in range(y_bottom - 7, y_bottom):
        for xx in range(x, x + 13):
            col = P['yellow'][2] if y > y_bottom - 6 else P['yellow'][3]
            if xx == x + 12 or y == y_bottom - 1:
                col = P['yellow'][1]
            c.set(xx, y, col)
    for (dx, dy) in [(3, -3), (7, -4), (10, -2)]:
        c.set(x + dx, y_bottom + dy, P['yellow'][1])
    # cut wedge
    for k in range(4):
        for j in range(k + 1):
            c.set(x + 8 + j, y_bottom - 7 + k, None)
            c.px[y_bottom - 7 + k][x + 8 + j] = None


def sleeping_cat(c, x, y_bottom):
    """Curled-up orange tabby, drawn on its own canvas so it gets a proper outline."""
    o = P['orange']
    k = Canvas(24, 12)
    # body (loaf) with stripes
    for (y, x0, x1) in [(5, 8, 19), (6, 6, 20), (7, 5, 21), (8, 5, 21), (9, 6, 20), (10, 7, 19)]:
        for xx in range(x0, x1 + 1):
            col = o[2]
            if y == 5 or xx == x0:
                col = o[3]
            if y == 10 or xx == x1:
                col = o[1]
            k.set(xx, y, col)
    for xx in (11, 14, 17):
        k.set(xx, 6, o[1]); k.set(xx, 7, o[1])
    # head resting on the left: round with two pointy ears and a pale muzzle
    for (y, x0, x1) in [(4, 2, 7), (5, 1, 8), (6, 1, 8), (7, 1, 8), (8, 2, 7), (9, 3, 6)]:
        for xx in range(x0, x1 + 1):
            k.set(xx, y, o[3] if (y == 4 or xx == x0) else o[2])
    k.set(2, 3, o[2]); k.set(2, 2, o[3]); k.set(3, 3, o[2])       # left ear
    k.set(6, 3, o[2]); k.set(7, 2, o[2]); k.set(7, 3, o[1])       # right ear
    k.set(2, 6, OUTLINE); k.set(3, 6, OUTLINE)                    # closed eyes
    k.set(5, 6, OUTLINE); k.set(6, 6, OUTLINE)
    k.set(3, 8, (240, 226, 206, 255)); k.set(4, 8, (240, 226, 206, 255)); k.set(5, 8, (240, 226, 206, 255))
    k.set(4, 7, P['red'][3])                                      # nose
    # tail wrapped around the front
    for xx in range(9, 21):
        k.set(xx, 11, o[1] if xx % 3 else o[2])
    k.outline(color=(70, 38, 22, 255))
    for yy in range(k.h):
        for xx in range(k.w):
            p = k.get(xx, yy)
            if p is not None:
                c.set(x + xx - 1, y_bottom - 12 + yy, p)
    # Zzz
    for (dx, dy) in [(9, -15), (10, -15), (10, -14), (9, -13), (10, -13), (13, -18), (14, -18), (14, -17), (13, -16), (14, -16)]:
        c.set(x + dx, y_bottom + dy, (236, 232, 220, 255))


def calendar(c, x0, y0):
    flat_rect(c, x0, y0, x0 + 18, y0 + 21, P['paper'])
    c.rect(x0, y0, x0 + 18, y0 + 4, P['shirt'][2])
    c.set(x0 + 9, y0 - 2, P['metal'][1]); c.set(x0 + 9, y0 - 1, P['metal'][1])
    for row in range(4):
        for col in range(5):
            c.set(x0 + 3 + col * 3, y0 + 8 + row * 3, P['paper'][1])
    # today is circled
    cx, cy = x0 + 3 + 3 * 3, y0 + 8 + 2 * 3
    for (dx, dy) in [(-1, -1), (0, -2), (1, -1), (2, 0), (1, 1), (0, 2), (-1, 1), (-2, 0)]:
        c.set(cx + dx, cy + dy, P['red'][2])
    for (dx, dy) in [(1, 0), (2, 1), (3, 0)]:          # torn-off page corner
        c.set(x0 + 15 + dx - 1, y0 + 21 - dy, None)
        c.px[y0 + 21 - dy][x0 + 14 + dx] = None


def fg_crate(c, x, y_bottom):
    k = Canvas(30, 24)
    flat_rect(k, 1, 8, 28, 23, P['woodlight'])
    k.line(1, 15, 28, 15, P['woodlight'][1])
    k.line(1, 8, 28, 23, P['woodlight'][1])
    for (dx, col) in [(5, P['red']), (11, P['red']), (17, P['yellow']), (23, P['leaf'])]:
        k.rect(dx - 2, 3, dx + 2, 7, col[2]); k.set(dx - 2, 3, col[3]); k.set(dx + 2, 7, col[1])
        k.set(dx, 2, P['leaf'][1])
    k.outline(color=OUTLINE)
    for yy in range(k.h):
        for xx in range(k.w):
            p = k.get(xx, yy)
            if p is not None:
                c.set(x + xx, y_bottom - k.h + yy, p)


def fg_sack(c, x, y_bottom):
    k = Canvas(22, 24)
    for y in range(4, 24):
        half = min(9, 3 + (y - 4) // 2)
        for xx in range(11 - half, 11 + half):
            col = P['tan'][3] if xx < 8 else (P['tan'][2] if xx < 15 else P['tan'][1])
            k.set(xx, y, col)
    k.rect(8, 3, 13, 4, P['tan'][1])
    k.rect(9, 1, 12, 2, P['tan'][2])
    for (dx, dy) in [(11, 12), (10, 13), (12, 13), (11, 14), (11, 15), (11, 16), (11, 17)]:
        k.set(dx, dy, P['straw'][1])
    k.outline(color=OUTLINE)
    for yy in range(k.h):
        for xx in range(k.w):
            p = k.get(xx, yy)
            if p is not None:
                c.set(x + xx, y_bottom - k.h + yy, p)


def bottle(c, x, y_bottom, col, h=14):
    flat_rect(c, x, y_bottom - h + 5, x + 5, y_bottom - 1, col)
    flat_rect(c, x + 2, y_bottom - h, x + 3, y_bottom - h + 5, col)
    c.set(x + 2, y_bottom - h - 1, P['wood'][2]); c.set(x + 3, y_bottom - h - 1, P['wood'][2])
    c.set(x + 1, y_bottom - h + 7, (255, 255, 255, 255))


def round_flask(c, x, y_bottom, col):
    for y in range(y_bottom - 9, y_bottom):
        for xx in range(x, x + 9):
            if (xx - x - 4) ** 2 + (y - y_bottom + 4.5) ** 2 <= 20:
                c.set(xx, y, col[2] if xx < x + 6 else col[1])
    c.rect(x + 3, y_bottom - 13, x + 5, y_bottom - 9, P['glass'][1])
    c.set(x + 2, y_bottom - 7, (255, 255, 255, 255))


def candle(c, x, y_bottom):
    flat_rect(c, x, y_bottom - 2, x + 6, y_bottom - 1, P['copper'])
    flat_rect(c, x + 2, y_bottom - 9, x + 4, y_bottom - 3, P['white'])
    c.set(x + 3, y_bottom - 10, OUTLINE)
    c.set(x + 3, y_bottom - 12, P['fire'][3]); c.set(x + 3, y_bottom - 11, P['fire'][2])


def folded_cloth(c, x, y_bottom):
    flat_rect(c, x, y_bottom - 4, x + 13, y_bottom - 1, P['shirt'])
    flat_rect(c, x + 1, y_bottom - 8, x + 12, y_bottom - 5, P['cloth'])
    for xx in range(x + 2, x + 12, 3):
        c.set(xx, y_bottom - 3, P['white'][3])


def garlic_braid(c, x, y0):
    c.line(x, y0, x, y0 + 4, P['straw'][1])
    for i in range(5):
        bx = x - 2 + (i % 2) * 2
        by = y0 + 5 + i * 4
        for (dx, dy) in [(0, 0), (1, 0), (2, 0), (0, 1), (1, 1), (2, 1), (3, 1), (1, 2), (2, 2)]:
            c.set(bx + dx, by + dy, P['white'][3] if dx < 2 else P['white'][1])
        c.set(bx + 1, by - 1, P['leaf2'][2])


def lantern(c, x, y0):
    c.line(x, y0, x, y0 + 5, P['metal'][1])
    flat_rect(c, x - 3, y0 + 6, x + 3, y0 + 7, P['metal'])
    flat_rect(c, x - 3, y0 + 8, x + 3, y0 + 15, P['sun'])
    c.line(x - 3, y0 + 8, x - 3, y0 + 15, P['metal'][1]); c.line(x + 3, y0 + 8, x + 3, y0 + 15, P['metal'][1])
    c.set(x, y0 + 11, (255, 255, 230, 255))
    flat_rect(c, x - 3, y0 + 16, x + 3, y0 + 17, P['metal'])


def chalkboard(c, x0, y0, x1, y1):
    flat_rect(c, x0, y0, x1, y1, P['wood'])
    board = (44, 58, 50, 255)
    c.rect(x0 + 2, y0 + 2, x1 - 2, y1 - 2, board)
    chalk = (210, 214, 200, 255)
    dim = (140, 150, 140, 255)
    # scribbled "price list" lines (not real text) with little item doodles on the left
    for i, y in enumerate(range(y0 + 7, y1 - 4, 6)):
        length = [14, 18, 11, 16, 13][i % 5]
        for k in range(length):
            if (k * 7 + i * 3) % 5 != 0:
                c.set(x0 + 11 + k, y + ((k // 2) % 2), chalk if k % 6 else dim)
        c.set(x1 - 7, y, chalk); c.set(x1 - 6, y, chalk); c.set(x1 - 5, y + 1, chalk)  # "price"
        # doodle bullet
        dot = [P['orange'][2], P['red'][2], P['yellow'][2], P['leaf'][2], P['tan'][2]][i % 5]
        c.set(x0 + 6, y, dot); c.set(x0 + 7, y, dot); c.set(x0 + 6, y + 1, dot); c.set(x0 + 7, y + 1, dot)
    c.rect(x0 + 4, y1 - 2, x0 + 9, y1 - 2, chalk)   # chalk stick on the ledge


def ledger(c, x, y):
    page = P['paper']
    for k in range(9):
        for xx in range(x + k // 2, x + 26 - k // 2):
            col = page[3] if xx < x + 13 else page[2]
            if xx in (x + 12, x + 13):
                col = page[1]
            c.set(xx, y + k, col)
    for k in range(2, 8, 2):
        c.line(x + 3 + k // 2, y + k, x + 10, y + k, page[1])
        c.line(x + 16, y + k, x + 22 - k // 2, y + k, page[1])
    # quill
    for i in range(11):
        c.set(x + 22 + i, y - 4 - i // 2, P['white'][3] if i > 2 else OUTLINE)
        if i > 3:
            c.set(x + 22 + i, y - 3 - i // 2, P['white'][1])


def coin_stack(c, x, y_bottom, n):
    for i in range(n):
        y = y_bottom - 2 - i * 2
        c.rect(x, y, x + 6, y + 1, P['sun'][2])
        c.set(x, y, P['sun'][3]); c.set(x + 6, y + 1, P['sun'][1])
        c.line(x, y + 1, x + 6, y + 1, P['sun'][1])
    c.rect(x + 1, y_bottom - 2 * n - 1, x + 5, y_bottom - 2 * n - 1, P['sun'][3])


def bell(c, x, y_bottom):
    flat_rect(c, x, y_bottom - 2, x + 8, y_bottom - 1, P['wood'])
    for y in range(y_bottom - 8, y_bottom - 2):
        half = 1 + (y - (y_bottom - 8)) // 2
        for xx in range(x + 4 - half, x + 5 + half):
            c.set(xx, y, P['sun'][3] if xx < x + 4 else P['sun'][2])
    c.set(x + 4, y_bottom - 9, P['sun'][1])


def produce_basket(c, x, y_bottom):
    for (dx, dy, col) in [(3, -9, P['red']), (8, -10, P['orange']), (13, -9, P['yellow']), (6, -12, P['red']), (11, -12, P['leaf'])]:
        c.rect(x + dx, y_bottom + dy, x + dx + 3, y_bottom + dy + 2, col[2])
        c.set(x + dx, y_bottom + dy, col[3]); c.set(x + dx + 3, y_bottom + dy + 2, col[1])
    for y in range(y_bottom - 7, y_bottom):
        inset = (y - (y_bottom - 7)) // 3
        for xx in range(x + inset, x + 20 - inset):
            col = P['straw'][2] if (xx + y) % 3 else P['straw'][1]
            if y == y_bottom - 7:
                col = P['straw'][3]
            c.set(xx, y, col)


def balance_scale(c, x, y_bottom):
    flat_rect(c, x - 5, y_bottom - 2, x + 5, y_bottom - 1, P['copper'])
    c.rect(x, y_bottom - 20, x + 1, y_bottom - 3, P['copper'][2])
    c.line(x - 11, y_bottom - 19, x + 12, y_bottom - 21, P['copper'][1])
    for (bx, by) in ((x - 11, y_bottom - 19 + 9), (x + 12, y_bottom - 21 + 7)):
        c.line(bx - 3, by, bx, by - 8, P['metal'][2]); c.line(bx + 3, by, bx, by - 8, P['metal'][2])
        c.rect(bx - 4, by, bx + 4, by + 1, P['copper'][2])
        c.line(bx - 4, by + 1, bx + 4, by + 1, P['copper'][1])
    c.rect(x + 10, y_bottom - 17, x + 13, y_bottom - 15, P['orange'][2])   # a carrot being weighed
    c.set(x + 11, y_bottom - 18, P['leaf'][2])


# ----------------------------------------------------------------------
# Scene
# ----------------------------------------------------------------------
def scene():
    W, H = 287, 161
    counter_top = 108
    c = Canvas(W, H)

    # back wall: vertical planks with a hand-set tone order, nails and a few knots
    tones = [2, 1, 2, 2, 1, 2, 1, 1, 2, 2, 1, 2, 2, 1, 2, 1, 2, 2, 1, 2, 1, 2, 2, 1, 2, 1, 1, 2, 2, 1, 2, 2, 1, 2, 1, 2, 2, 1, 2, 1, 2]
    wood = P['wood']
    for i, x0 in enumerate(range(0, W, 7)):
        base = wood[2] if tones[i % len(tones)] == 2 else mixc(wood[2], wood[1], 0.45)
        for y in range(6, counter_top):
            for x in range(x0, min(W, x0 + 7)):
                col = base
                if x == x0:
                    col = wood[0]
                elif x == x0 + 1:
                    col = lighten_c(base, 0.1)
                c.set(x, y, col)
        for ny in (9, counter_top - 4):
            c.set(x0 + 3, ny, P['metal'][1])
    for (kx, ky) in [(23, 44), (150, 25), (204, 83), (64, 92)]:
        c.set(kx, ky, wood[0]); c.set(kx + 1, ky, wood[1]); c.set(kx, ky + 1, wood[1]); c.set(kx + 1, ky - 1, wood[1])
    # ceiling beam
    for y in range(0, 6):
        for x in range(W):
            c.set(x, y, wood[3] if y == 0 else (wood[1] if y < 5 else wood[0]))

    # hanging things
    lantern(c, 28, 6)
    calendar(c, 82, 16)
    garlic_braid(c, 112, 6)

    # shelves
    def shelf(y, x0, x1):
        flat_rect(c, x0, y, x1, y + 2, P['woodlight'])
        c.line(x0, y + 3, x1, y + 3, wood[0])
        for bx in (x0 + 4, x1 - 6):
            c.rect(bx, y + 4, bx + 2, y + 7, wood[1])
            c.set(bx, y + 7, wood[0]); c.set(bx + 1, y + 6, wood[0])

    shelf(36, 122, 232)
    jar(c, 126, 36, 9, 13, P['jam'], P['shirt'])
    jar(c, 137, 36, 8, 8, P['soup'], P['cloth'])
    sack(c, 148, 36)
    cheese(c, 167, 36)
    sleeping_cat(c, 184, 36)
    jar(c, 205, 36, 7, 10, P['leaf'], P['woodlight'])
    jar(c, 214, 36, 10, 7, P['red'], P['shirt'])

    shelf(72, 122, 232)
    bottle(c, 127, 72, P['leaf'], 16)
    bottle(c, 135, 72, P['leaf2'], 12)
    round_flask(c, 144, 72, P['water'])
    folded_cloth(c, 157, 72)
    candle(c, 175, 72)
    for i, (x, h) in enumerate([(186, 9), (193, 11), (200, 9)]):   # books lying + standing
        flat_rect(c, x, 72 - h, x + 5, 71, [P['shirt'], P['denim'], P['leaf2']][i])
    flat_rect(c, 207, 68, 224, 71, P['jam'])
    flat_rect(c, 208, 64, 223, 67, P['sun'])

    chalkboard(c, 242, 14, 280, 58)
    # little framed picture of the farm
    flat_rect(c, 246, 68, 272, 88, P['sun'])
    c.rect(248, 70, 270, 79, P['water'][2]); c.rect(248, 80, 270, 86, P['leaf'][2])
    c.rect(252, 76, 258, 80, P['shirt'][2]); c.rect(253, 74, 257, 75, P['shirt'][1])

    # the merchant behind the counter
    bust = merchant_bust()
    mx, my = 44, counter_top - 52
    for y in range(bust.h):
        for x in range(bust.w):
            p = bust.get(x, y)
            if p is not None and my + y < counter_top:
                c.set(mx + x, my + y, p)

    # counter top and front panel
    lw = P['woodlight']
    for y in range(counter_top, counter_top + 6):
        for x in range(W):
            col = lw[3] if y == counter_top else (lw[2] if y < counter_top + 5 else lw[0])
            if y == counter_top + 2 and (x * 5) % 37 < 6:
                col = mixc(lw[2], lw[1], 0.5)
            c.set(x, y, col)
    for y in range(counter_top + 6, H):
        for x in range(W):
            panel = x // 48
            px = x % 48
            col = wood[2] if panel % 2 == 0 else mixc(wood[2], wood[1], 0.3)
            if px == 0:
                col = wood[0]
            # inset panel
            if 5 <= px <= 42 and counter_top + 12 <= y <= H - 8:
                col = mixc(col, wood[1], 0.4)
                if y == counter_top + 12 or px == 5:
                    col = wood[0]
                elif y == H - 8 or px == 42:
                    col = lighten_c(wood[2], 0.15)
            c.set(x, y, col)

    # on the counter
    bell(c, 108, counter_top + 1)
    ledger(c, 132, counter_top - 7)
    coin_stack(c, 176, counter_top + 1, 3)
    coin_stack(c, 184, counter_top + 1, 5)
    coin_stack(c, 192, counter_top + 1, 2)
    c.rect(200, counter_top - 1, 205, counter_top, P['sun'][2])      # a loose coin
    balance_scale(c, 226, counter_top + 1)
    produce_basket(c, 254, counter_top + 1)

    # foreground props in front of the counter
    fg_crate(c, 6, H)
    fg_sack(c, 262, H)

    img = c.image().resize((W * 2, H * 2), 0)
    return img.crop((0, 0, 574, 321))


def mixc(a, b, t):
    return (int(a[0] + (b[0] - a[0]) * t), int(a[1] + (b[1] - a[1]) * t), int(a[2] + (b[2] - a[2]) * t), 255)
