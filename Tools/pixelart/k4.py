"""K-4, seasonal rites: the winter solstice (red bean porridge) and Jeongwol Daeboreum (the moon house).

World sprites at 32 px per tile, placed by their bottom-middle. rite_moon is a 640 x 360 view picture
(shown in the same frame as the Sunset Hill overlook).
"""
import math
from PIL import Image
from pixel import Canvas, P, ramp, darken_c, lighten_c, seeded
import kvillage as K

RED = ramp('#3e0c0c', '#6e1a16', '#9a2c22', '#c4503a')          # red bean porridge
RICE = ramp('#b8b0a0', '#dcd6c8', '#f2eee4', '#ffffff')
IRON = ramp('#141418', '#26262c', '#3e3e48', '#6a6a78')
STRAW = P['straw']
PINE = K.PINE
STEAM = [(255, 255, 255, 170), (255, 255, 255, 120), (255, 255, 255, 80)]


# ---------------------------------------------------------------- the solstice
def rite_pot():
    """A cast-iron gamasot of red bean porridge on a clay brazier, steam rising, a ladle resting on the rim."""
    c = Canvas(40, 36)
    c.shadow(20, 34, 18, 2)
    clay = ramp('#4a2a1a', '#7a4a30', '#a87048', '#cc9a6a')
    c.box(6, 22, 33, 33, clay)                                # the brazier
    c.rect(15, 27, 24, 33, (30, 14, 10, 255))
    for (x, k) in [(16, 2), (18, 3), (20, 2), (22, 3)]:
        c.set(x, 31, P['fire'][k]); c.set(x + 1, 32, P['fire'][k - 1])
    c.ellipse(20, 18, 16, 6, IRON)                            # the pot
    c.rect(3, 15, 37, 17, IRON[1]); c.set(3, 16, IRON[3]); c.set(37, 16, IRON[2])
    c.ellipse(20, 15, 13, 3.4, RED, flat=True)                # porridge
    for (x, y) in [(13, 15), (18, 14), (24, 15), (27, 14), (16, 16)]:
        c.set(x, y, RICE[2]); c.set(x + 1, y, RICE[3])        # little rice balls
    c.line(26, 15, 34, 6, P['wood'][2]); c.ellipse(26, 15, 2, 1, P['wood'], flat=True)   # ladle
    for i, x in enumerate((12, 19, 26)):
        for k in range(3):
            c.set(x + (k % 2), 10 - k * 2 - (i % 2), STEAM[k])
    c.outline()
    return c


def red_bean_porridge():
    c = Canvas(32, 32)
    c.shadow(16, 28, 11, 2)
    c.poly([(4, 15), (28, 15), (24, 26), (8, 26)], P['white'], light_axis='x')   # a white bowl
    c.rect(10, 26, 22, 27, P['white'][1])
    c.ellipse(16, 15, 12, 3.2, RED, flat=False)
    for (x, y) in [(10, 15), (15, 14), (20, 16), (23, 14), (13, 16)]:
        c.set(x, y, RICE[3]); c.set(x + 1, y, RICE[2])
    c.set(18, 13, (40, 30, 20, 255))                          # a pine nut on top
    for i, x in enumerate((11, 17, 22)):
        for k in range(3):
            c.set(x + (k % 2), 10 - k * 2 - (i % 2), STEAM[k])
    c.outline()
    return c


def rite_redbeans():
    """Red beans scattered on the ground at the gate."""
    c = Canvas(40, 14)
    rnd = seeded(11)
    for _ in range(34):
        x = int(20 + rnd.gauss(0, 8)); y = int(8 + rnd.gauss(0, 2.4))
        c.set(x, y, RED[2]); c.set(x + 1, y, RED[1])
        if rnd.random() < 0.3:
            c.set(x, y - 1, RED[3])
    return c


# ---------------------------------------------------------------- the moon house (daljip)
W_D, H_D = 64, 88


def _poles(c, charred=False):
    col = ramp('#1a1210', '#2e201a', '#3e2c22', '#544034') if charred else P['wood']
    for (x0, x1) in [(10, 30), (54, 34), (22, 31), (42, 33)]:
        c.thick_line(x0, 84, x1, 8, col[1], 2)
        c.line(x0, 84, x1, 8, col[3] if not charred else col[2])


def daljip_frame():
    c = Canvas(W_D, H_D)
    c.shadow(32, 86, 26, 2)
    _poles(c)
    c.line(14, 64, 50, 64, STRAW[1]); c.line(18, 46, 46, 46, STRAW[1])   # first rope rings
    for (x, y) in [(6, 82), (12, 84), (52, 83)]:                          # a few bundles waiting
        c.rect(x, y - 3, x + 5, y, STRAW[2]); c.line(x, y - 3, x + 5, y - 3, STRAW[3])
    c.outline()
    return c


def daljip_built(fire=None):
    """A cone of straw and pine branches on poles, a straw rope wound round it, white wish papers tied on.
    fire: None, 0 or 1 (two flicker frames)."""
    c = Canvas(W_D, H_D)
    c.shadow(32, 86, 28, 2)
    rnd = seeded(7)
    # the cone body: bundles of straw hanging in uneven vertical strands, pine branches pushed in higher up
    col_off = [rnd.randint(0, 5) for _ in range(W_D)]
    for y in range(12, 85):
        half = 3 + (y - 12) * 0.36
        for x in range(int(32 - half), int(32 + half) + 1):
            t = (x - (32 - half)) / max(1, 2 * half)
            k = 3 if t < 0.22 else 2 if t < 0.62 else 1              # light from the left
            h = (x * 7349 + ((y + col_off[x]) // 4) * 2689) % 11
            if h == 0:
                k = max(0, k - 1)                                    # a darker strand
            elif h == 1 and k < 3:
                k += 1                                               # a stray bright stalk
            if y < 52 and (x * 13 + y * 5) % 23 == 0:
                c.set(x, y, PINE[k])
            else:
                c.set(x, y, STRAW[k])
    for y in range(16, 82, 5):                                 # straw strands
        x = 32 + rnd.randint(-int((y - 12) * 0.3), int((y - 12) * 0.3))
        c.line(x, y, x + rnd.choice((-1, 1)), y + 3, STRAW[0])
    for (x, y) in [(28, 26), (38, 34), (24, 44), (40, 52)]:   # pine tufts
        c.ellipse(x, y, 3, 2, PINE)
    for x in (31, 33):                                         # poles poking out the top
        c.line(x, 12, x - 1 + (x - 32) * 2, 3, P['wood'][1])
    # the straw rope (saekki) wound round, with wish papers
    for y in (40, 58, 74):
        half = 3 + (y - 12) * 0.36
        for x in range(int(32 - half), int(32 + half) + 1):
            c.set(x, y + (1 if x % 4 == 0 else 0), P['straw'][0])
        for x in range(int(32 - half) + 3, int(32 + half) - 2, 6):
            c.rect(x, y + 1, x + 1, y + 5, (246, 242, 232, 255))
            c.set(x, y + 5, (220, 214, 200, 255))
    if fire is not None:
        f = P['fire']
        for i in range(5):                                     # flame tongues, swaying, rounded at the base
            fx = 17 + i * 7.5
            h = 20 + ((i * 2 + fire) % 3) * 8 + (6 if i == 2 else 0)
            for y in range(h):
                t = y / h
                w = 4.2 * math.sin(math.pi * min(1.0, (1 - t) * 0.9 + 0.1)) ** 0.8
                sway = math.sin(t * 4 + i * 1.7 + fire * 1.3) * 2.2 * t
                cx = fx + sway
                for x in range(int(cx - w), int(cx + w) + 1):
                    d = abs(x - cx) / max(0.6, w)
                    k = 3 if d < 0.35 and t < 0.55 else 2 if d < 0.75 else 1
                    c.set(x, 84 - y, f[k])
        for (x, y) in [(30, 20), (36, 28), (26, 34), (33, 10), (39, 16), (24, 22)]:   # the top catching
            c.set(x + fire, y, f[2]); c.set(x + fire, y - 1, f[3])
        for (x, y) in [(20, 4), (44, 2), (28, 0), (50, 8)]:   # sparks
            c.set(x - fire * 2, y + fire, f[3])
    c.outline()
    return c


def daljip_ash():
    c = Canvas(W_D, H_D)
    c.shadow(32, 86, 26, 2)
    ash = ramp('#2a2624', '#4a4440', '#6e6660', '#9a928a')
    c.ellipse(32, 82, 24, 5, ash)
    for (x0, x1, top) in [(16, 26, 60), (48, 38, 64), (30, 33, 52)]:
        c.thick_line(x0, 82, x1, top, (30, 22, 18, 255), 2)
    for (x, y) in [(24, 81), (34, 80), (41, 82), (29, 83)]:
        c.set(x, y, P['fire'][2]); c.set(x + 1, y, P['fire'][1])   # embers
    c.outline()
    return c


def jwibul_spark():
    c = Canvas(8, 8)
    f = P['fire']
    c.ellipse(4, 4, 3, 3, f)
    c.rect(3, 3, 4, 4, f[3])
    return c


# ---------------------------------------------------------------- the full moon (view picture, 640 x 360)
def rite_moon():
    W, H = 640, 360
    c = Canvas(W, H)
    sky = [(10, 12, 34), (18, 20, 52), (30, 28, 72), (52, 38, 86), (84, 52, 92)]
    for y in range(H):
        t = y / 250
        band = min(len(sky) - 1, int(t * len(sky)))
        nxt = min(len(sky) - 1, band + 1)
        frac = t * len(sky) - band
        for x in range(W):
            use = nxt if ((x * 7 + y * 13) % 16) / 16 < frac and frac > 0.55 else band   # ordered dither between bands
            c.set(x, y, sky[use] + (255,))
    rnd = seeded(3)
    for _ in range(140):                                       # stars
        x, y = rnd.randint(0, W - 1), rnd.randint(0, 200)
        c.set(x, y, (230, 230, 255, 255) if rnd.random() < 0.2 else (160, 160, 200, 255))
    # the moon with a halo
    mx, my, mr = 470, 82, 34
    for y in range(my - 70, my + 70):
        for x in range(mx - 70, mx + 70):
            d = math.hypot(x - mx, y - my)
            if mr < d < 66 and ((x + y) % 2 == 0 or d < mr + 10):
                p = c.get(x, y)
                if p:
                    k = 0.18 * (1 - (d - mr) / 32)
                    c.set(x, y, (int(p[0] + (255 - p[0]) * k), int(p[1] + (240 - p[1]) * k), int(p[2] + (200 - p[2]) * k), 255))
    moon = ramp('#c8b88a', '#e8dcb0', '#fbf3d2', '#fffbea')
    c.ellipse(mx, my, mr, mr, moon, flat=True)
    for y in range(my - mr, my + mr + 1):
        for x in range(mx - mr, mx + mr + 1):
            d = math.hypot(x - mx, y - my)
            if d <= mr and d > mr - 4 and x > mx:
                c.set(x, y, moon[1])
    for (cx, cy, r) in [(458, 70, 7), (482, 92, 5), (470, 102, 4), (452, 92, 3), (488, 68, 3)]:
        c.ellipse(cx, cy, r, r * 0.8, [moon[0]] * 4, flat=True)
    # far mountains, near hill, then the village rooftops
    for layer, (base, amp, col, seed) in enumerate([(206, 34, (30, 30, 62), 5), (232, 22, (22, 22, 46), 9)]):
        r2 = seeded(seed)
        ph = [r2.random() * 6 for _ in range(3)]
        for x in range(W):
            top = base - amp * (0.55 * math.sin(x / 90 + ph[0]) + 0.3 * math.sin(x / 37 + ph[1]) + 0.15 * math.sin(x / 13 + ph[2]))
            for y in range(int(top), H):
                c.set(x, y, col + (255,))
    roof = (14, 14, 30, 255)
    for (x0, w, h, lit) in [(20, 70, 26, True), (110, 54, 20, False), (176, 80, 28, True), (300, 46, 18, True), (560, 70, 24, False)]:
        y0 = 262 - h
        for y in range(y0, 264):
            inset = max(0, 8 - (y - y0)) if y < y0 + 8 else 0
            for x in range(x0 + inset - 4, x0 + w - inset + 4):
                c.set(x, y, roof)
        if lit:
            for wx in range(x0 + 10, x0 + w - 10, 18):
                c.rect(wx, y0 + 12, wx + 6, y0 + 17, (255, 206, 120, 255))
    # the dark paddy in front
    for y in range(262, H):
        for x in range(W):
            c.set(x, y, (16, 14, 22, 255) if (x // 40 + y // 14) % 2 else (20, 18, 28, 255))
    for y in range(280, H, 18):
        c.line(0, y, W - 1, y, (28, 24, 34, 255))
    # the moon house burning, its glow on the ground
    fx, fy = 300, 318
    for y in range(240, H):
        for x in range(150, 450):
            d = math.hypot((x - fx) / 1.6, y - fy)
            if d < 90 and (x + y) % 2 == 0:
                p = c.get(x, y)
                k = 0.35 * (1 - d / 90)
                c.set(x, y, (int(p[0] + (255 - p[0]) * k), int(p[1] + (140 - p[1]) * k), int(p[2] + (60 - p[2]) * k), 255))
    dj = daljip_built(fire=0).image().resize((128, 176), Image.NEAREST)
    big = c.image()
    big.alpha_composite(dj, (fx - 64, fy - 176))
    out = Canvas(W, H)
    for y in range(H):
        for x in range(W):
            out.px[y][x] = big.getpixel((x, y))
    c = out
    f = P['fire']
    for (x, y, k) in [(276, 112, 3), (318, 96, 2), (300, 80, 3), (290, 60, 2), (330, 70, 3), (262, 90, 2), (312, 40, 2), (288, 30, 3)]:
        c.rect(x, y, x + 1, y + 1, f[k])                       # sparks rising toward the moon
    # people around the fire, in silhouette (one small: Mina, with a jwibul ring)
    sil = (8, 6, 12, 255)
    for (x, h, w) in [(188, 46, 12), (222, 50, 13), (380, 48, 13), (414, 44, 12), (452, 30, 9)]:
        top = fy + 6 - h
        c.ellipse(x, top + 5, w * 0.42, 6, [sil] * 4, flat=True)                   # head
        c.poly([(x - w // 2, top + 11), (x + w // 2, top + 11), (x + w // 2 + 2, fy + 6), (x - w // 2 - 2, fy + 6)], None, solid=sil)
    for a in range(0, 360, 12):                                 # Mina's jwibul ring
        rx = 452 + math.cos(math.radians(a)) * 20
        ry = fy - 36 + math.sin(math.radians(a)) * 20
        k = 3 if a % 36 == 0 else 2
        c.set(rx, ry, f[k]); c.set(rx + 1, ry, f[k - 1])
    return c


def world_sprites():
    return {
        'rite_pot': rite_pot(),
        'rite_redbeans': rite_redbeans(),
        'daljip_frame': daljip_frame(),
        'daljip_built': daljip_built(),
        'daljip_fire_0': daljip_built(fire=0),
        'daljip_fire_1': daljip_built(fire=1),
        'daljip_ash': daljip_ash(),
        'jwibul_spark': jwibul_spark(),
    }
