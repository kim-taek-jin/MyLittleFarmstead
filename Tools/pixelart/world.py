"""World objects and characters."""
from pixel import Canvas, P, darken_c, lighten_c, seeded
import items


def tree(r=None):
    r = r or P['leaf']
    c = Canvas(32, 48)
    c.shadow(16, 44, 11, 3)
    c.box(13, 32, 18, 44, P['wood'])
    c.set(15, 36, P['wood'][1]); c.set(16, 40, P['wood'][1])
    # three canopy tiers (pine, like the original art)
    tiers = [(36, 14, 9), (27, 11, 9), (19, 8, 9)]
    for (base, half, h) in tiers:
        pts = [(16 - half - 1, base), (16, base - h - 6), (16 + half + 1, base)]
        c.poly(pts, r, light_axis='x')
        # scalloped bottom edge
        for x in range(16 - half, 16 + half + 1, 3):
            c.set(x, base, r[0]); c.set(x + 1, base + 1, r[1])
    c.set(16, 3, r[3]); c.set(15, 5, r[3])
    for (x, y) in [(12, 17), (10, 26), (13, 31), (9, 34)]:
        c.set(x, y, r[3])
    c.outline()
    return c


def rock():
    c = Canvas(32, 32)
    c.shadow(16, 27, 12, 3)
    c.poly([(4, 26), (7, 15), (14, 9), (23, 10), (28, 18), (28, 26)], P['stone'], light_axis='diag')
    c.poly([(16, 26), (20, 17), (28, 19), (28, 26)], P['stone'], light_axis='diag', invert=False)
    c.line(14, 13, 18, 19, P['stone'][1])
    c.set(10, 14, P['stone'][3]); c.set(11, 13, P['stone'][3]); c.set(12, 12, P['stone'][3])
    # moss
    for (x, y) in [(6, 24), (7, 23), (8, 24), (24, 12), (25, 13)]:
        c.set(x, y, P['leaf'][2])
    c.outline()
    return c


def apple_ground():
    c = items.apple()
    return c


def well():
    c = Canvas(32, 40)
    c.shadow(16, 37, 13, 2)
    # stone base (cylinder)
    c.box(5, 24, 26, 35, P['stone'])
    rnd = seeded(7)
    for row, y in enumerate(range(24, 36, 3)):
        off = 0 if row % 2 == 0 else 3
        for x in range(5 + off, 27, 6):
            c.line(x, y, x, y + 2, P['stone'][1])
        c.line(5, y, 26, y, P['stone'][1])
    c.ellipse(15.5, 24, 11, 3, P['stone'], flat=True)
    c.ellipse(15.5, 24, 8, 2, [P['water'][0]] * 4, flat=True)
    c.set(13, 24, P['water'][2]); c.set(14, 24, P['water'][3])
    # posts
    c.box(6, 9, 7, 24, P['wood']); c.box(24, 9, 25, 24, P['wood'])
    # crank beam + rope + bucket
    c.line(7, 13, 24, 13, P['wood'][1])
    c.line(16, 14, 16, 18, P['tan'][2])
    c.box(14, 18, 18, 21, P['woodlight'])
    c.line(14, 19, 18, 19, P['metal'][2])
    c.set(26, 13, P['metal'][2]); c.set(27, 13, P['metal'][1]); c.set(27, 14, P['metal'][1])
    # roof
    c.poly([(2, 10), (16, 2), (29, 10)], P['shirt'], light_axis='x')
    c.line(2, 10, 29, 10, P['shirt'][0])
    for x in range(5, 28, 3):
        c.line(x, 9, x + 2, 6, P['shirt'][1])
    c.outline()
    return c


def storage_box():
    c = Canvas(32, 32)
    c.shadow(16, 29, 13, 2)
    c.box(4, 12, 27, 28, P['wood'])
    for y in (17, 22):
        c.line(4, y, 27, y, P['wood'][1])
    # lid
    c.box(3, 8, 28, 13, P['woodlight'])
    c.line(3, 13, 28, 13, P['wood'][0])
    # metal bands + lock
    for x in (7, 24):
        c.rect(x, 8, x + 1, 28, P['metal'][1]); c.set(x, 9, P['metal'][3])
    c.box(14, 12, 17, 16, P['sun'])
    c.set(15, 14, P['wood'][0])
    c.outline()
    return c


def bed():
    """Top-down bed (pillow at top), 32x48."""
    c = Canvas(32, 48)
    c.box(3, 2, 28, 46, P['wood'])
    c.box(3, 2, 28, 6, P['woodlight'])         # headboard
    c.box(5, 7, 26, 44, P['white'])             # sheet
    c.box(7, 8, 24, 14, P['white'])             # pillow
    c.line(8, 14, 23, 14, P['white'][1])
    c.box(5, 18, 26, 44, P['cloth'])            # blanket
    c.line(5, 18, 26, 18, P['white'][3])
    for y in range(22, 44, 6):
        for x in range(8, 26, 6):
            c.set(x, y, P['cloth'][3]); c.set(x + 1, y + 1, P['cloth'][1])
    c.box(3, 44, 28, 46, P['woodlight'])        # footboard
    c.outline()
    return c


def exit_sign():
    c = Canvas(32, 32)
    c.shadow(16, 30, 6, 1.5)
    c.box(14, 18, 17, 30, P['wood'])
    c.box(3, 6, 28, 19, P['woodlight'])
    c.line(3, 19, 28, 19, P['wood'][0])
    # arrow
    c.rect(8, 11, 19, 13, P['wood'][0])
    c.poly([(19, 8), (25, 12), (19, 16)], P['wood'], solid=P['wood'][0])
    c.set(5, 8, P['woodlight'][3]); c.set(26, 17, P['wood'][1])
    c.outline()
    return c


# ---------------- characters ----------------
def _farmer(direction, frame, skin=P['skin'], shirt=P['shirt'], pants=P['denim'], hat=P['straw'], hair=P['hair']):
    """32x32 farmer. direction: 'down' | 'up' | 'side'. frame 0 idle, 1/2 walking."""
    c = Canvas(32, 32)
    c.shadow(16, 30, 7, 1.5, alpha=60)
    bob = 1 if frame in (1, 2) else 0
    # legs
    lx = {0: (0, 0), 1: (-1, 1), 2: (1, -1)}[frame]
    if direction == 'side':
        c.box(13 + lx[0], 23, 15 + lx[0], 28, pants)
        c.box(16 + lx[1], 23, 18 + lx[1], 28, pants)
        c.rect(13 + lx[0], 28, 16 + lx[0], 29, P['wood'][0])
        c.rect(16 + lx[1], 28, 19 + lx[1], 29, P['wood'][0])
    else:
        c.box(12, 23, 15, 28 - (1 if frame == 1 else 0), pants)
        c.box(17, 23, 20, 28 - (1 if frame == 2 else 0), pants)
        c.rect(12, 28 - (1 if frame == 1 else 0), 15, 29 - (1 if frame == 1 else 0), P['wood'][0])
        c.rect(17, 28 - (1 if frame == 2 else 0), 20, 29 - (1 if frame == 2 else 0), P['wood'][0])
    y0 = 14 - bob + 1
    # torso: shirt + overalls
    c.box(10, y0, 21, y0 + 9, shirt)
    c.box(12, y0 + 3, 19, y0 + 9, pants)
    if direction != 'up':
        c.rect(12, y0, 13, y0 + 3, pants[2]); c.rect(18, y0, 19, y0 + 3, pants[2])
        c.set(13, y0 + 3, P['sun'][2]); c.set(18, y0 + 3, P['sun'][2])
    # arms
    swing = {0: 0, 1: 1, 2: -1}[frame]
    if direction == 'side':
        c.box(14 + swing, y0 + 1, 16 + swing, y0 + 7, shirt)
        c.rect(14 + swing, y0 + 7, 16 + swing, y0 + 8, skin[2])
    else:
        c.box(8, y0 + 1 + max(0, swing), 9, y0 + 7 + max(0, swing), shirt)
        c.box(22, y0 + 1 + max(0, -swing), 23, y0 + 7 + max(0, -swing), shirt)
        c.set(8, y0 + 8 + max(0, swing), skin[2]); c.set(9, y0 + 8 + max(0, swing), skin[2])
        c.set(22, y0 + 8 + max(0, -swing), skin[2]); c.set(23, y0 + 8 + max(0, -swing), skin[2])
    # head
    hy = 5 - bob + 1
    c.box(11, hy + 2, 20, hy + 9, skin)
    if direction == 'down':
        c.rect(11, hy + 2, 20, hy + 3, hair[2])
        c.set(13, hy + 6, P['outline']); c.set(18, hy + 6, P['outline'])
        c.set(12, hy + 7, P['red'][3]); c.set(19, hy + 7, P['red'][3])
        c.rect(15, hy + 8, 16, hy + 8, skin[0])
    elif direction == 'up':
        c.rect(11, hy + 2, 20, hy + 8, hair[2])
        c.line(12, hy + 8, 19, hy + 8, hair[1])
    else:
        c.rect(11, hy + 2, 15, hy + 7, hair[2])
        c.set(18, hy + 6, P['outline'])
        c.set(21, hy + 6, skin[2]); c.set(21, hy + 7, skin[1])
        c.set(17, hy + 7, P['red'][3])
    # straw hat
    c.ellipse(15.5, hy + 3, 9, 2.2, hat)
    c.box(11, hy - 1, 20, hy + 2, hat)
    c.line(11, hy + 1, 20, hy + 1, P['shirt'][1])
    c.outline()
    return c


def player_frames():
    # hand pixel-mapped frames (same method and outline as the merchant)
    import characters
    return characters.player_frames()


def merchant():
    """Friendly market merchant behind a crate of produce, 32x48."""
    c = Canvas(32, 48)
    # body
    c.box(6, 18, 25, 34, P['tan'])
    c.box(9, 20, 22, 34, P['apron'])
    c.box(4, 19, 6, 28, P['tan']); c.box(25, 19, 27, 28, P['tan'])
    # head
    c.box(9, 6, 22, 17, P['skin'])
    c.set(12, 11, P['outline']); c.set(19, 11, P['outline'])
    c.rect(11, 13, 20, 14, P['hair'][3])          # moustache
    c.set(10, 12, P['red'][3]); c.set(21, 12, P['red'][3])
    # cap
    c.box(8, 3, 23, 7, P['leaf2'])
    c.rect(6, 7, 25, 8, P['leaf2'][1])
    # crate with produce
    c.box(2, 32, 29, 46, P['wood'])
    c.line(2, 39, 29, 39, P['wood'][1])
    for i, fn in enumerate([items.carrot, items.apple, items.corn]):
        mini = fn()
        for y in range(32):
            for x in range(32):
                p = mini.get(x, y)
                if p is not None and x % 3 == 0 and y % 3 == 0:
                    c.set(3 + i * 9 + x // 3, 27 + y // 3, p)
    c.outline()
    return c


WORLD = {
    'tree': tree,
    'rock': rock,
    'apple_ground': apple_ground,
    'well': well,
    'storage_box': storage_box,
    'bed': bed,
    'exit_sign': exit_sign,
    'merchant': lambda: __import__('haggle').merchant_small(),
}
