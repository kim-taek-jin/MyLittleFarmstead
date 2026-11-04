"""Villagers (hand pixel-mapped like the player) and their gray -> colour stages.

Stage 0 is fully gray (how they look when we meet them), stage 3 is full colour.
Each fulfilled wish moves a villager one stage toward colour.
"""
from pixel import Canvas, P, ramp
from haggle import PixelMap, OUTLINE
from characters import W, H

LAVENDER = ramp('#4e3462', '#7c5a9a', '#ae8cc8', '#dcc8ec')
LEATHER = ramp('#3a2414', '#5e3a22', '#875636', '#b07a4e')
STEEL = ramp('#3a3e48', '#5c6270', '#8a91a0', '#c0c6d2')
SUNNY = ramp('#7a5a10', '#c99a1e', '#f2d24a', '#fff2a0')

BASE = {
    's': P['skin'][2], 'S': P['skin'][1], 'l': P['skin'][3], 'r': (232, 140, 120, 255),
    'E': OUTLINE, 'o': (120, 52, 40, 255), 'p': P['shirt'][2], 'b': P['wood'][1], 'k': P['wood'][0],
}


def _frame(pm, palette, yoff=1):
    body = pm.canvas(palette)
    c = Canvas(32, 32)
    c.shadow(16, 30, 7, 1.5, alpha=60)
    for y in range(body.h):
        for x in range(body.w):
            p = body.get(x, y)
            if p is not None:
                c.set(x + 5, y + yoff, p)
    return c


def rosa():
    """Grandma Rosa, the baker: white bun, glasses, lavender dress, white apron."""
    pal = dict(BASE)
    pal.update({'w': P['white'][2], 'W': P['white'][1], 'v': P['white'][3], 'q': P['copper'][1],
                'd': LAVENDER[2], 'D': LAVENDER[1], 'u': LAVENDER[3],
                'a': P['white'][3], 'A': P['white'][1]})
    m = PixelMap(W, H)
    m.seg(0, 8, 9, 'w'); m.seg(1, 7, 9, 'w'); m.seg(2, 7, 9, 'w')         # bun
    m.seg(3, 6, 9, 'W')
    m.seg(4, 5, 9, 'w'); m.seg(5, 4, 9, 'w')
    for y in range(6, 13):
        m.seg(y, 5, 9, 's')
    m.seg(6, 5, 9, 'w'); m.seg(7, 5, 5, 'w')
    m.seg(13, 7, 9, 's')
    m.seg(9, 6, 8, 'q')                                                   # glasses
    m.seg(10, 7, 7, 'E')
    m.seg(11, 5, 5, 'r')
    m.seg(12, 9, 9, 'o')
    m.seg(14, 8, 9, 'S')
    for y in range(14, 22):
        m.seg(y, 3, 9, 'd')
    for y in range(22, 27):
        m.seg(y, 2, 9, 'd')
    for y in range(16, 22):
        m.seg(y, 2, 2, 'd')
        m.seg(y, 3, 3, 'D')
    m.seg(22, 1, 3, 's')                                                  # hands
    for y in range(16, 26):
        m.seg(y, 6, 9, 'a')                                               # apron
    m.seg(15, 7, 9, 'a')
    m.seg(27, 5, 8, 'b'); m.seg(28, 5, 8, 'k')
    m.shade({'s': ('S', 'l'), 'd': ('D', 'u'), 'a': ('A', None), 'w': ('W', 'v')})
    m.put(13, 9, 'q'); m.put(6, 9, 'q')
    m.put(6, 8, 'l')
    return _frame(m, pal)


def theo():
    """Theo, the blacksmith: bald with a red headband, big beard, leather apron, rolled sleeves."""
    pal = dict(BASE)
    pal.update({'H': P['hair'][1], 'J': P['hair'][2], 'K': P['hair'][3],
                'g': STEEL[2], 'G': STEEL[1], 'n': STEEL[3],
                'L': LEATHER[2], 'M': LEATHER[1], 'N': LEATHER[3],
                't': P['denim'][1], 'T': P['denim'][0], 'B': P['sun'][2]})
    m = PixelMap(W, H)
    for y in range(2, 11):
        m.seg(y, 5, 9, 's')
    m.seg(1, 6, 9, 's')
    m.seg(4, 5, 9, 'p')                                                   # headband
    m.seg(8, 4, 4, 's'); m.seg(9, 4, 4, 's')                              # ears
    m.seg(6, 6, 8, 'H')                                                   # brows
    m.seg(7, 7, 7, 'E')
    for y in range(9, 15):                                                # beard
        m.seg(y, 5 if y < 13 else 6, 9, 'J')
    m.seg(15, 7, 9, 'J')
    m.seg(10, 8, 9, 'o')
    m.seg(9, 8, 9, 'H')
    for y in range(13, 23):                                               # shirt
        m.seg(y, 2, 9, 'g')
    for y in range(16, 23):                                               # bare forearms
        m.seg(y, 1, 2, 's')
    m.seg(15, 1, 3, 'g')
    for y in range(15, 26):                                               # leather apron
        m.seg(y, 5, 9, 'L')
    m.seg(17, 6, 6, 'B')
    for y in range(23, 27):
        m.seg(y, 5, 8, 't')
    m.seg(27, 4, 8, 'b'); m.seg(28, 4, 8, 'k')
    m.shade({'s': ('S', 'l'), 'g': ('G', 'n'), 'L': ('M', 'N'), 'J': ('H', 'K')})
    m.put(7, 2, 'l'); m.put(8, 2, 'l')                                    # shiny head
    m.put(12, 11, 'K')
    return _frame(m, pal)


def mina():
    """Mina, the child: brown pigtails with red ribbons, sunny yellow dress."""
    pal = dict(BASE)
    pal.update({'H': P['hair'][1], 'J': P['hair'][2], 'K': P['hair'][3],
                'y': SUNNY[2], 'Y': SUNNY[1], 'h': SUNNY[3], 'c': P['white'][3]})
    m = PixelMap(W, H)
    for y in range(8, 15):
        m.seg(y, 5, 9, 'J')
    m.seg(7, 6, 9, 'J')
    for y in range(11, 15):
        m.seg(y, 2, 4, 'J')                                               # pigtails
    m.seg(10, 3, 4, 'p')                                                  # ribbons
    for y in range(10, 17):
        m.seg(y, 6, 9, 's')
    m.seg(10, 6, 9, 'J')
    m.seg(13, 7, 7, 'E')
    m.seg(15, 6, 6, 'r')
    m.seg(16, 9, 9, 'o')
    m.seg(17, 7, 9, 'c')
    for y in range(18, 23):
        m.seg(y, 5, 9, 'y')
    for y in range(23, 26):
        m.seg(y, 3, 9, 'y')
    for y in range(18, 23):
        m.seg(y, 4, 4, 's')
    for y in range(26, 28):
        m.seg(y, 6, 7, 's')
    m.seg(28, 5, 8, 'b')
    m.shade({'s': ('S', 'l'), 'y': ('Y', 'h'), 'J': ('H', 'K')})
    m.put(7, 8, 'K')
    return _frame(m, pal, yoff=0)


def bubble():
    """Little '!' speech bubble shown when a villager has something to say."""
    c = Canvas(12, 14)
    c.rect(1, 0, 10, 9, P['white'][3])
    c.rect(0, 1, 11, 8, P['white'][3])
    c.set(4, 10, P['white'][3]); c.set(5, 10, P['white'][3]); c.set(4, 11, P['white'][3])
    c.rect(5, 2, 6, 5, P['red'][1])
    c.rect(5, 7, 6, 7, P['red'][1])
    c.outline(color=OUTLINE)
    return c


def stages(canvas):
    """Gray -> colour versions of a sprite: saturation 0, 1/3, 2/3, 1."""
    out = []
    for t in (0.0, 1 / 3, 2 / 3, 1.0):
        c = Canvas(canvas.w, canvas.h)
        c.shadows = list(canvas.shadows)
        for y in range(canvas.h):
            for x in range(canvas.w):
                p = canvas.get(x, y)
                if p is None:
                    continue
                lum = 0.3 * p[0] + 0.59 * p[1] + 0.11 * p[2]
                g = (lum * 0.92 + 6, lum * 0.92 + 8, lum * 0.92 + 12)   # slightly cool gray
                c.set(x, y, (int(g[0] + (p[0] - g[0]) * t), int(g[1] + (p[1] - g[1]) * t),
                             int(g[2] + (p[2] - g[2]) * t), p[3]))
        out.append(c)
    return out


VILLAGERS = {'rosa': rosa, 'theo': theo, 'mina': mina}
