"""The color-eating spirit, sharing (donation box) and Star Hill art.

Spirit forms: gray baby -> young / grown in one of four colors (sun, earth, water, bloom),
plus the rare 'prism' form when all four colors are fed evenly.
Cel shaded by hand rules (hard crescent shadow, one highlight), dark outline like the characters.
"""
import math
from pixel import Canvas, P, ramp
from haggle import OUTLINE

GRAY = ramp('#5a5e68', '#8a8e98', '#b8bcc4', '#e4e6ea')
BLOOM = ramp('#7a2a4a', '#c4507a', '#f08aaa', '#ffd0e0')
COLORS = {'sun': P['sun'], 'earth': P['leaf'], 'water': P['water'], 'bloom': BLOOM}


def blob(c, cx, cy, rx, ry, r):
    """Round body: base fill, hard crescent shadow bottom-right, small highlight top-left."""
    for y in range(int(cy - ry - 1), int(cy + ry + 2)):
        for x in range(int(cx - rx - 1), int(cx + rx + 2)):
            if ((x + 0.5 - cx) / rx) ** 2 + ((y + 0.5 - cy) / ry) ** 2 > 1:
                continue
            lit = ((x + 0.5 - cx + 1.6) / rx) ** 2 + ((y + 0.5 - cy + 1.6) / ry) ** 2 <= 1
            c.set(x, y, r[2] if lit else r[1])
    hx, hy = int(cx - rx * 0.45), int(cy - ry * 0.5)
    c.set(hx, hy, r[3]); c.set(hx + 1, hy, r[3]); c.set(hx, hy + 1, r[3])


def face(c, cx, cy, happy=True):
    c.set(cx - 3, cy, OUTLINE); c.set(cx - 3, cy - 1, OUTLINE)
    c.set(cx + 2, cy, OUTLINE); c.set(cx + 2, cy - 1, OUTLINE)
    if happy:
        c.set(cx - 1, cy + 2, OUTLINE); c.set(cx, cy + 2, OUTLINE)
    c.set(cx - 5, cy + 2, (240, 150, 150, 255)); c.set(cx + 4, cy + 2, (240, 150, 150, 255))


def wisp(c, cx, cy, r, length=5):
    """Little flame-like tail under the body (spirits float)."""
    for i in range(length):
        w = max(0, 2 - i // 2)
        for dx in range(-w, w + 1):
            c.set(cx + dx + (i % 2), cy + i, r[2] if dx <= 0 else r[1])


def spirit(color, stage):
    """stage 0 = baby (gray), 1 = young, 2 = grown."""
    c = Canvas(28, 28)
    r = COLORS.get(color, GRAY) if stage > 0 else GRAY
    size = {0: 6.0, 1: 7.0, 2: 8.5}[stage]
    cx, cy = 14, 15
    wisp(c, cx, int(cy + size - 1), r, 4 + stage)
    blob(c, cx, cy, size, size * 0.92, r)

    if stage >= 1:
        if color == 'sun':                 # rays
            n = 6 if stage == 1 else 10
            for i in range(n):
                a = -math.pi / 2 + (i - (n - 1) / 2) * (math.pi / (n + 1)) * 1.6
                for k in range(2 + stage):
                    c.set(cx + math.cos(a) * (size + 1 + k), cy + math.sin(a) * (size + 1 + k), r[3] if k == 0 else r[2])
        elif color == 'earth':             # sprout / leafy crown
            c.line(cx, cy - size, cx, cy - size - 3, P['leaf'][1])
            for (dx, dy, l) in ([(-1, -0.6, 4), (1, -0.7, 4)] if stage == 1 else [(-1, -0.5, 5), (1, -0.6, 5), (-1, -1.2, 4), (1, -1.3, 4), (0, -1, 3)]):
                for i in range(l):
                    c.set(cx + dx * i, cy - size - 3 + dy * i, P['leaf'][3] if i == l - 1 else P['leaf'][2])
            if stage == 2:
                c.set(cx - 6, cy + 5, P['red'][2]); c.set(cx + 5, cy + 6, P['sun'][2])   # tiny flowers on the body
        elif color == 'water':             # fins and droplet
            for side in (-1, 1):
                for i in range(2 + stage * 2):
                    c.set(cx + side * (size + 1 + i // 2), cy - 1 + i, P['water'][3] if i == 0 else P['water'][2])
            c.set(cx, cy - size - 2, P['water'][3]); c.set(cx, cy - size - 1, P['water'][2])
            c.set(cx - 1, cy - size - 1, P['water'][2]); c.set(cx + 1, cy - size - 1, P['water'][1])
        elif color == 'bloom':             # petals
            petals = 3 if stage == 1 else 6
            for i in range(petals):
                a = -math.pi / 2 + (i - (petals - 1) / 2) * 0.55
                px, py = cx + math.cos(a) * (size + 1), cy + math.sin(a) * (size + 1)
                for dx, dy in ((0, 0), (1, 0), (0, 1), (1, 1)):
                    c.set(px + dx, py + dy, BLOOM[3] if dx == 0 and dy == 0 else BLOOM[2])
    face(c, cx, cy)
    c.outline(color=OUTLINE)
    if stage == 2:
        for (x, y) in [(3, 6), (24, 9), (22, 24)]:
            c.set(x, y, r[3])
    return c


def spirit_prism():
    c = Canvas(28, 28)
    cx, cy, size = 14, 15, 8.5
    bands = [P['red'], P['orange'], P['sun'], P['leaf'], P['water'], BLOOM]
    wisp(c, cx, int(cy + size - 1), BLOOM, 6)
    for y in range(int(cy - size - 1), int(cy + size + 2)):
        for x in range(int(cx - size - 1), int(cx + size + 2)):
            if ((x + 0.5 - cx) / size) ** 2 + ((y + 0.5 - cy) / (size * 0.92)) ** 2 > 1:
                continue
            band = bands[min(5, int((y - (cy - size)) / (2 * size) * 6))]
            lit = ((x + 0.5 - cx + 1.6) / size) ** 2 + ((y + 0.5 - cy + 1.6) / (size * 0.92)) ** 2 <= 1
            c.set(x, y, band[2] if lit else band[1])
    for i in range(8):                       # star crown
        a = -math.pi / 2 + (i - 3.5) * 0.3
        c.set(cx + math.cos(a) * (size + 2), cy + math.sin(a) * (size + 2), (255, 255, 230, 255))
    c.set(cx - 4, cy - 5, (255, 255, 255, 255)); c.set(cx - 3, cy - 5, (255, 255, 255, 255))
    face(c, cx, cy)
    c.outline(color=OUTLINE)
    for (x, y) in [(2, 5), (25, 8), (23, 25), (4, 22)]:
        c.set(x, y, (255, 255, 230, 255))
    return c


def spirit_sprites():
    out = {'spirit_gray': spirit('gray', 0), 'spirit_prism': spirit_prism()}
    for color in COLORS:
        out[f'spirit_{color}_young'] = spirit(color, 1)
        out[f'spirit_{color}_grown'] = spirit(color, 2)
    return out


# ---------------- sharing + Star Hill ----------------
def star_fragment():
    c = Canvas(32, 32)
    pts = []
    for i in range(10):
        a = -math.pi / 2 + i * math.pi / 5
        rad = 10 if i % 2 == 0 else 4.5
        pts.append((15.5 + math.cos(a) * rad, 16 + math.sin(a) * rad))
    c.poly(pts, P['sun'], light_axis='diag')
    c.set(13, 12, (255, 255, 255, 255)); c.set(12, 13, (255, 255, 255, 255))
    c.outline(color=(120, 80, 10, 255))
    # soft glow added after the outline so it stays clean
    for rr, a in [(13, 35), (11, 60)]:
        for y in range(32):
            for x in range(32):
                if (x - 15.5) ** 2 + (y - 16) ** 2 <= rr * rr and c.get(x, y) is None:
                    c.set(x, y, (255, 236, 150, a))
    return c


def donation_box():
    c = Canvas(32, 36)
    c.shadow(16, 34, 12, 1.5)
    c.box(5, 14, 26, 33, P['woodlight'])
    c.box(4, 10, 27, 15, P['wood'])
    c.rect(11, 11, 20, 12, P['outline'])                 # coin / gift slot
    for (dx, dy) in [(1, 0), (3, 0), (0, 1), (1, 1), (2, 1), (3, 1), (4, 1), (1, 2), (2, 2), (3, 2), (2, 3)]:
        c.set(13 + dx, 21 + dy, P['jam'][2])             # heart
    c.set(14, 21, P['jam'][3])
    # little sign on a stick
    c.rect(26, 2, 27, 12, P['wood'][1])
    c.box(22, 0, 31, 6, P['paper'])
    c.set(25, 3, P['jam'][2]); c.set(28, 3, P['jam'][2]); c.set(26, 4, P['jam'][2]); c.set(27, 4, P['jam'][2])
    c.outline(color=OUTLINE)
    return c


def star_ladder():
    """Glowing vine that grows toward the sky on clear nights."""
    c = Canvas(24, 72)
    c.shadow(12, 71, 8, 1.5)
    for y in range(0, 70):
        x = 12 + math.sin(y / 6.0) * 4
        c.set(x, y, P['leaf'][1]); c.set(x + 1, y, P['leaf'][2])
        if y % 7 == 0:
            side = 1 if (y // 7) % 2 else -1
            for i in range(4):
                c.set(x + side * (i + 1), y - i * 0.5, P['leaf'][3] if i == 3 else P['leaf'][2])
        if y % 11 == 5:
            c.set(x - 2, y, (255, 246, 190, 255)); c.set(x - 3, y, (255, 230, 140, 255))
    c.outline(color=OUTLINE)
    return c


def bg_starhill():
    """Night sky over the hill behind the farm. Same size and density as the farm background."""
    W, H = 574, 328
    c = Canvas(W, H)
    sky = [(14, 18, 44, 255), (20, 28, 64, 255), (28, 40, 86, 255), (40, 56, 108, 255)]
    horizon = 196
    for y in range(horizon):
        band = min(3, y * 4 // horizon)
        for x in range(W):
            col = sky[band]
            if band < 3 and (y * 4 // horizon) != ((y + 3) * 4 // horizon) and (x + y) % 2 == 0:
                col = sky[band + 1]
            c.set(x, y, col)
    # hand-placed stars: big, medium, tiny
    big = [(60, 30), (180, 52), (300, 22), (420, 70), (520, 40), (250, 110), (110, 120), (480, 130)]
    for (x, y) in big:
        c.set(x, y, (255, 255, 240, 255))
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            c.set(x + dx, y + dy, (200, 210, 255, 255))
    for k in range(140):
        x, y = (k * 197) % W, (k * 83) % (horizon - 20)
        c.set(x, y, (180, 190, 230, 255) if k % 3 else (255, 245, 200, 255))
    # moon
    for y in range(20, 52):
        for x in range(470, 502):
            d = (x - 486) ** 2 + (y - 36) ** 2
            if d <= 225 and (x - 492) ** 2 + (y - 32) ** 2 > 150:
                c.set(x, y, (250, 240, 200, 255) if d < 170 else (220, 210, 170, 255))
    # milky way: soft clumps of tiny stars along a diagonal (lighter sky patches + stars)
    for k in range(34):
        t = k / 34
        cx = 30 + t * 520 + ((k * 41) % 30) - 15
        cy = 150 - t * 115 + ((k * 29) % 22) - 11
        rx, ry = 16 + (k * 7) % 12, 6 + (k * 5) % 5
        for y in range(int(cy - ry), int(cy + ry) + 1):
            for x in range(int(cx - rx), int(cx + rx) + 1):
                if 0 <= y < horizon and ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 <= 1 and (x * 3 + y * 7) % 5 == 0:
                    cur = c.get(x, y)
                    if cur is not None:
                        c.set(x, y, (min(255, cur[0] + 14), min(255, cur[1] + 16), min(255, cur[2] + 24), 255))
        for j in range(9):
            x = int(cx + ((j * 37 + k * 11) % (2 * rx)) - rx)
            y = int(cy + ((j * 23 + k * 7) % (2 * ry)) - ry)
            if 0 <= y < horizon:
                c.set(x, y, (200, 205, 240, 255))
    # far hills silhouette
    for x in range(W):
        top = int(horizon - 18 - 10 * math.sin(x / 70.0) - 6 * math.sin(x / 23.0))
        for y in range(top, horizon + 8):
            c.set(x, y, (30, 44, 60, 255))
    # the hilltop meadow (night-tinted grass with hand-placed tufts)
    g = [(22, 52, 44, 255), (34, 72, 58, 255), (46, 92, 70, 255), (82, 132, 96, 255)]
    for x in range(W):
        crest = int(horizon - 6 + 10 * math.sin((x - 60) / 150.0) ** 2)   # the hill's own crest
        for y in range(crest, H):
            col = g[2]
            if y < crest + 2:
                col = g[3]                       # moonlit rim
            elif y > H - 40 + int(4 * math.sin(x / 40.0)):
                col = g[1]                       # darker foreground
            c.set(x, y, col)
    # a lone tree silhouette on the left of the hill
    tx, ty = 52, horizon + 6
    c.rect(tx, ty - 18, tx + 2, ty + 6, (26, 34, 40, 255))
    for (dx, dy, rr) in [(-6, -22, 8), (6, -24, 9), (0, -32, 9), (-2, -16, 7), (8, -14, 6)]:
        for y in range(ty + dy - rr, ty + dy + rr + 1):
            for x in range(tx + dx - rr, tx + dx + rr + 1):
                if (x - tx - dx) ** 2 + (y - ty - dy) ** 2 <= rr * rr:
                    c.set(x, y, (24, 40, 46, 255) if (x - tx - dx) + (y - ty - dy) > 2 else (34, 56, 60, 255))
    for k in range(70):
        x, y = 8 + (k * 151) % (W - 16), horizon + 10 + (k * 61) % (H - horizon - 14)
        c.set(x, y, g[3]); c.set(x - 1, y + 1, g[1]); c.set(x + 1, y + 1, g[1])
    # glowing night flowers
    for (x, y) in [(70, 240), (150, 290), (330, 250), (420, 300), (520, 230), (240, 300)]:
        for dx, dy in ((0, 0), (3, 1), (-2, 2)):
            c.set(x + dx, y + dy, (180, 220, 255, 255)); c.set(x + dx, y + dy + 1, g[1])
    # bench (right side) - the player can sit here
    bench = Canvas(50, 26)
    bench.shadow(25, 24, 22, 2, alpha=90)
    bench.box(1, 12, 48, 15, P['woodlight'])                 # seat
    bench.box(1, 2, 48, 5, P['woodlight'])                   # backrest
    bench.box(1, 7, 48, 9, P['woodlight'])
    for lx in (4, 44):
        bench.rect(lx, 2, lx + 1, 24, P['wood'][0])
    bench.outline(color=OUTLINE)
    night = lambda p: (int(p[0] * 0.62), int(p[1] * 0.66), int(p[2] * 0.85), p[3])
    bimg = bench.image()
    for y in range(bimg.height):
        for x in range(bimg.width):
            p = bimg.getpixel((x, y))
            if p[3] == 255:
                c.set(426 + x, 214 + y, night(p))
    # vine top poking up from below (way back to the farm), with little lights
    for y in range(H - 44, H):
        x = 287 + int(math.sin(y / 6.0) * 4)
        c.set(x - 1, y, OUTLINE); c.set(x, y, P['leaf'][1]); c.set(x + 1, y, P['leaf'][2]); c.set(x + 2, y, OUTLINE)
        if y % 9 == 0:
            c.set(x + 3, y, (255, 240, 170, 255)); c.set(x - 2, y - 1, (255, 240, 170, 255))
    return c.image()
