"""Item icons (32x32)."""
from pixel import Canvas, P, darken_c, lighten_c

S = 32


def _leaf(c, x, y, dx, dy, length, r, width=2):
    """Pointed leaf from (x,y) toward direction (dx,dy)."""
    for i in range(length):
        t = i / max(1, length - 1)
        w = max(1, int(round(width * (1 - abs(t - 0.45) * 1.6))))
        cx, cy = x + dx * i, y + dy * i
        for k in range(-w + 1, w):
            px, py = cx - dy * k * 0.5, cy + dx * k * 0.5
            c.set(px, py, r[3] if k < 0 and i > 1 else (r[2] if k <= 0 else r[1]))


# ---------------- produce ----------------
def carrot(c=None, ox=0, oy=0, scale=1.0):
    c = c or Canvas(S, S)
    # body: tapered cone pointing down-left
    pts = [(ox + 13, oy + 9), (ox + 22, oy + 12), (ox + 8, oy + 28), (ox + 7, oy + 27)]
    c.poly(pts, P['orange'], light_axis='x', invert=False)
    # ridges
    for (x, y) in [(15, 14), (13, 18), (11, 22)]:
        c.line(ox + x, oy + y, ox + x + 3, oy + y + 1, P['orange'][1])
    c.set(ox + 17, oy + 12, P['orange'][3]); c.set(ox + 16, oy + 13, P['orange'][3])
    # leaves
    _leaf(c, ox + 18, oy + 10, 0.3, -1, 8, P['leaf'])
    _leaf(c, ox + 19, oy + 10, 0.9, -0.6, 8, P['leaf'])
    _leaf(c, ox + 17, oy + 10, -0.5, -1, 7, P['leaf'])
    c.outline()
    return c


def potato(c=None):
    c = c or Canvas(S, S)
    c.ellipse(16, 18, 11, 8, P['tan'])
    c.ellipse(11, 16, 5, 5, P['tan'])
    for (x, y) in [(12, 15), (19, 19), (15, 22), (21, 15)]:
        c.set(x, y, P['tan'][0]); c.set(x + 1, y, P['tan'][1])
    c.outline()
    return c


def corn(c=None):
    c = c or Canvas(S, S)
    # cob
    c.ellipse(17, 14, 5, 10, P['yellow'])
    for y in range(6, 23, 2):
        for x in range(13, 22, 2):
            if c.get(x, y) is not None:
                c.set(x, y, darken_c(c.get(x, y), 0.85))
    # husk leaves
    c.poly([(9, 28), (12, 14), (16, 22), (15, 29)], P['leaf2'], light_axis='x')
    c.poly([(24, 28), (21, 15), (18, 23), (19, 29)], P['leaf2'], light_axis='x', invert=True)
    c.line(14, 24, 16, 29, P['leaf2'][3])
    # silk
    c.line(17, 4, 18, 2, P['tan'][2]); c.set(16, 3, P['tan'][3])
    c.outline()
    return c


def strawberry(c=None):
    c = c or Canvas(S, S)
    pts = [(7, 13), (12, 10), (20, 10), (25, 13), (24, 19), (17, 28), (15, 28), (8, 19)]
    c.poly(pts, P['red'], light_axis='diag')
    c.ellipse(16, 16, 9, 7, P['red'])
    # seeds
    for (x, y) in [(11, 15), (15, 14), (19, 15), (22, 18), (13, 19), (17, 19), (15, 23), (19, 22), (10, 18)]:
        c.set(x, y, P['yellow'][3])
    # calyx
    for dx, dy in [(-5, 1), (-2, 2), (2, 2), (5, 1), (0, 3)]:
        c.line(16, 9, 16 + dx, 9 + dy, P['leaf'][2])
    c.rect(15, 6, 16, 9, P['leaf'][1])
    c.set(15, 6, P['leaf'][3])
    c.outline()
    return c


def apple(c=None):
    c = c or Canvas(S, S)
    c.ellipse(12, 18, 8, 9, P['red'])
    c.ellipse(20, 18, 8, 9, P['red'])
    c.ellipse(16, 19, 9, 9, P['red'])
    c.rect(12, 11, 19, 12, P['red'][1])
    c.set(10, 14, P['red'][3]); c.set(11, 13, P['red'][3]); c.set(10, 15, P['white'][3])
    # stem + leaf
    c.line(16, 11, 17, 6, P['wood'][1])
    _leaf(c, 18, 8, 1, -0.3, 6, P['leaf'])
    c.outline()
    return c


# ---------------- food ----------------
def bread():
    c = Canvas(S, S)
    c.ellipse(16, 18, 13, 8, P['bread'])
    c.ellipse(16, 14, 11, 6, P['bread'])
    # scoring
    for x in (10, 15, 20):
        c.line(x, 11, x + 3, 15, P['bread'][1])
        c.line(x + 1, 11, x + 4, 15, P['bread'][3])
    c.rect(5, 23, 27, 24, P['bread'][1])
    c.outline()
    return c


def plate(c, cy=22):
    c.ellipse(16, cy, 14, 5, P['white'], flat=False)
    c.ellipse(16, cy - 1, 10, 3, P['white'], flat=True)


def roasted_carrot():
    c = Canvas(S, S)
    plate(c)
    roast = [P['orange'][0], (168, 70, 24, 255), P['orange'][2], P['orange'][3]]
    for (x, y) in [(6, 18), (9, 14), (13, 17)]:
        c.poly([(x, y + 1), (x + 13, y - 2), (x + 14, y + 1), (x + 1, y + 4)], roast, light_axis='y')
        for k in (3, 7, 11):  # grill marks
            c.set(x + k, y + 1 - k * 0.2, P['wood'][0]); c.set(x + k, y + 2 - k * 0.2, P['wood'][0])
    for (x, y) in [(22, 12), (24, 14), (23, 16)]:  # herbs
        c.set(x, y, P['leaf'][2]); c.set(x + 1, y, P['leaf'][3])
    c.outline()
    return c


def baked_potato():
    c = Canvas(S, S)
    plate(c)
    c.ellipse(16, 16, 10, 6, P['tan'])
    # split with butter
    c.line(9, 14, 23, 15, P['yellow'][2])
    c.rect(14, 12, 18, 14, P['yellow'][3])
    c.rect(15, 13, 17, 13, P['white'][3])
    c.set(11, 18, P['tan'][0]); c.set(20, 19, P['tan'][0])
    c.outline()
    return c


def bowl(c, r_soup):
    # bowl body
    c.poly([(4, 15), (28, 15), (24, 26), (8, 26)], P['woodlight'], light_axis='x')
    c.rect(10, 26, 22, 27, P['wood'][1])
    c.ellipse(16, 15, 12, 3, r_soup, flat=False)


def corn_soup():
    c = Canvas(S, S)
    bowl(c, P['soup'])
    for (x, y) in [(10, 15), (14, 14), (19, 16), (22, 14), (16, 16)]:
        c.set(x, y, P['yellow'][3])
    c.set(13, 15, P['leaf'][2]); c.set(20, 15, P['leaf'][2])
    # steam
    for x in (11, 17, 22):
        c.set(x, 9, (255, 255, 255, 140)); c.set(x + 1, 7, (255, 255, 255, 110)); c.set(x, 5, (255, 255, 255, 80))
    c.outline()
    return c


def strawberry_jam():
    c = Canvas(S, S)
    # jar
    c.box(8, 12, 23, 28, P['jam'])
    c.ellipse(15.5, 20, 7.5, 8, P['jam'])
    c.rect(9, 12, 22, 14, P['jam'][2])
    c.set(10, 16, P['white'][3]); c.set(10, 17, P['white'][3]); c.set(11, 15, P['white'][2])
    # lid with cloth
    c.box(7, 7, 24, 11, P['shirt'])
    for x in range(8, 24, 3):
        c.set(x, 9, P['white'][3])
    c.rect(8, 11, 23, 11, P['wood'][2])
    # label
    c.box(11, 18, 20, 23, P['paper'])
    c.set(15, 20, P['red'][2]); c.set(16, 20, P['red'][2]); c.set(15, 21, P['red'][1]); c.set(16, 21, P['red'][2])
    c.outline()
    return c


# ---------------- resources ----------------
def wood():
    c = Canvas(S, S)
    for (y, x0, x1) in [(21, 3, 23), (12, 8, 27)]:
        c.box(x0, y - 4, x1, y + 4, P['wood'])
        c.set(x0, y - 4, None); c.set(x0, y + 4, None)
        for x in range(x0 + 2, x1 - 4, 5):
            c.line(x, y - 1, x + 3, y - 1, P['wood'][1])
            c.line(x + 2, y + 2, x + 4, y + 2, P['wood'][1])
        # cut end with rings
        c.ellipse(x1, y, 3.5, 4.5, P['woodlight'], flat=True)
        c.ellipse(x1, y, 2, 2.6, [P['woodlight'][1]] * 4, flat=True)
        c.set(x1, y, P['woodlight'][3])
    c.outline()
    return c


def stone():
    c = Canvas(S, S)
    c.poly([(5, 22), (9, 12), (16, 8), (24, 10), (28, 18), (26, 26), (8, 27)], P['stone'], light_axis='diag')
    c.line(13, 13, 18, 18, P['stone'][1]); c.line(18, 18, 22, 17, P['stone'][1])
    c.set(12, 11, P['stone'][3]); c.set(13, 10, P['stone'][3])
    c.outline()
    return c


# ---------------- essences ----------------
def sunshine_shard():
    c = Canvas(S, S)
    # glow
    for r, a in [(13, 40), (10, 70)]:
        for y in range(32):
            for x in range(32):
                if (x - 15.5) ** 2 + (y - 15.5) ** 2 <= r * r and c.get(x, y) is None:
                    c.set(x, y, (255, 230, 120, a))
    c.poly([(16, 3), (23, 14), (16, 29), (9, 14)], P['sun'], light_axis='diag')
    c.line(16, 3, 16, 29, P['sun'][3])
    c.poly([(16, 3), (23, 14), (16, 14)], P['sun'], solid=P['sun'][2])
    c.set(13, 10, (255, 255, 255, 255)); c.set(12, 12, (255, 255, 255, 255))
    for (x, y) in [(5, 6), (26, 7), (27, 24), (6, 25)]:
        c.set(x, y, P['sun'][3]); c.set(x + 1, y, P['sun'][2]); c.set(x - 1, y, P['sun'][2]); c.set(x, y + 1, P['sun'][2]); c.set(x, y - 1, P['sun'][2])
    return c


def earths_breath():
    c = Canvas(S, S)
    for r, a in [(13, 35), (10, 60)]:
        for y in range(32):
            for x in range(32):
                if (x - 15.5) ** 2 + (y - 16.5) ** 2 <= r * r and c.get(x, y) is None:
                    c.set(x, y, (120, 230, 170, a))
    # swirl orb
    c.ellipse(16, 17, 8, 8, P['spirit'])
    c.line(10, 18, 14, 13, P['spirit'][3]); c.line(14, 13, 19, 14, P['spirit'][3]); c.line(19, 14, 20, 18, P['spirit'][3])
    c.line(20, 18, 17, 21, P['spirit'][1])
    # sprout on top
    c.line(16, 9, 16, 5, P['leaf'][2])
    _leaf(c, 16, 6, -1, -0.4, 5, P['leaf'])
    _leaf(c, 17, 5, 1, -0.6, 5, P['leaf'])
    for (x, y) in [(6, 9), (25, 11), (24, 26)]:
        c.set(x, y, P['spirit'][3])
    return c


# ---------------- tools ----------------
def hoe(head=P['metal'], glow=False):
    c = Canvas(S, S)
    c.thick_line(7, 28, 23, 6, P['woodlight'][1], w=3)
    c.line(7, 27, 22, 6, P['woodlight'][3])
    # blade at top
    c.poly([(19, 4), (28, 4), (28, 7), (25, 12), (23, 12), (23, 8), (19, 8)], head, light_axis='y')
    c.line(20, 5, 27, 5, head[3])
    if glow:
        for (x, y) in [(10, 8), (27, 15), (5, 18)]:
            c.set(x, y, P['leaf'][3]); c.set(x + 1, y, P['leaf'][2]); c.set(x - 1, y, P['leaf'][2]); c.set(x, y - 1, P['leaf'][2]); c.set(x, y + 1, P['leaf'][2])
    c.outline()
    return c


def watering_can(r=P['metal']):
    c = Canvas(S, S)
    c.box(7, 13, 21, 27, r)
    c.ellipse(14, 20, 7.5, 7.5, r)
    c.rect(7, 13, 21, 14, r[3])
    # spout
    c.thick_line(21, 22, 28, 12, r[1], w=2)
    c.rect(27, 10, 29, 12, r[2])
    # handle
    for x, y in [(9, 12), (9, 9), (11, 7), (17, 7), (19, 9), (19, 12)]:
        pass
    c.line(9, 12, 10, 8, r[1]); c.line(10, 8, 18, 8, r[1]); c.line(18, 8, 19, 12, r[1])
    c.line(10, 9, 17, 9, r[3])
    # water drop
    c.set(29, 15, P['water'][2]); c.set(29, 16, P['water'][3])
    c.outline()
    return c


def scythe():
    c = Canvas(S, S)
    c.thick_line(10, 29, 15, 6, P['woodlight'][1], w=3)
    c.line(10, 28, 14, 7, P['woodlight'][3])
    c.rect(11, 20, 13, 21, P['wood'][0])
    # curved blade
    for i in range(16):
        t = i / 15
        x = 15 + t * 14
        y = 6 + (t * t) * 10 - t * 3
        c.set(x, y, P['metal'][2]); c.set(x, y - 1, P['metal'][3]); c.set(x - 0.5, y + 1, P['metal'][1] if t < 0.8 else None)
    c.outline()
    return c


def seed_packet(icon_fn, band):
    c = Canvas(S, S)
    c.box(6, 4, 25, 28, P['paper'])
    c.rect(6, 4, 25, 7, band[2]); c.rect(6, 8, 25, 8, band[1])
    # zigzag top
    for x in range(6, 26, 2):
        c.set(x, 4, None)
    # little picture
    mini = icon_fn()
    for y in range(32):
        for x in range(32):
            p = mini.get(x, y)
            if p is not None and x % 2 == 0 and y % 2 == 0:
                c.set(8 + x // 2, 10 + y // 2, p)
    c.rect(9, 25, 22, 25, P['paper'][1])
    c.outline()
    return c


def campfire():
    c = Canvas(S, S)
    # stones ring
    for (x, y) in [(6, 25), (11, 27), (17, 28), (23, 27), (27, 24)]:
        c.ellipse(x, y, 3, 2, P['stone'])
    # logs
    c.thick_line(7, 26, 25, 19, P['wood'][2], w=3)
    c.thick_line(7, 19, 25, 26, P['wood'][1], w=3)
    # flame
    c.poly([(10, 21), (13, 12), (16, 4), (19, 11), (22, 21)], P['fire'], light_axis='y', invert=True)
    c.poly([(13, 21), (15, 14), (17, 9), (19, 15), (19, 21)], P['fire'], solid=P['fire'][3])
    c.outline()
    return c


ITEMS = {
    'hoe': lambda: hoe(),
    'fertile_hoe': lambda: hoe(head=P['leaf'], glow=True),
    'watering_can': lambda: watering_can(),
    'copper_watering_can': lambda: watering_can(P['copper']),
    'scythe': scythe,
    'carrot': carrot,
    'potato': potato,
    'corn': corn,
    'strawberry': strawberry,
    'apple': apple,
    'carrot_seed': lambda: seed_packet(carrot, P['orange']),
    'potato_seed': lambda: seed_packet(potato, P['tan']),
    'corn_seed': lambda: seed_packet(corn, P['yellow']),
    'strawberry_seed': lambda: seed_packet(strawberry, P['red']),
    'bread': bread,
    'roasted_carrot': roasted_carrot,
    'baked_potato': baked_potato,
    'corn_soup': corn_soup,
    'strawberry_jam': strawberry_jam,
    'wood': wood,
    'stone': stone,
    'sunshine_shard': sunshine_shard,
    'earths_breath': earths_breath,
    'campfire': campfire,
}
