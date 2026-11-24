"""Villagers (hand pixel-mapped like the player): Korean country people. Gray -> colour stages.

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
MAROON = ramp('#3e1622', '#6e2a3a', '#9c4256', '#c8708a')
MOMPPAE = ramp('#5a2a4a', '#9a3e6e', '#d0679a', '#f2a6c8')
NAVY = ramp('#141c34', '#24345e', '#3a5288', '#6a86ba')
KHAKI = ramp('#3e3a20', '#6a6438', '#948c56', '#c0b884')

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
    """Grandma Boksun of the rice mill: gray permed curls, a maroon knit vest over a cream blouse,
    flowery 'momppae' work trousers, black rubber shoes. (id stays 'rosa' for old saves)"""
    pal = dict(BASE)
    pal.update({'w': P['white'][1], 'W': P['stone'][2], 'v': P['white'][2],
                'd': MAROON[2], 'D': MAROON[1], 'u': MAROON[3],
                'c': P['paper'][3], 'C': P['paper'][2],
                'm': MOMPPAE[2], 'M': MOMPPAE[1], 'f': P['white'][3], 'F': P['sun'][3]})
    m = PixelMap(W, H)
    # permed curls: a round bumpy cap of gray hair
    m.seg(1, 6, 9, 'w'); m.seg(2, 4, 9, 'w')
    for y in range(3, 8):
        m.seg(y, 3, 9, 'w')
    for (x, y) in ((3, 2), (5, 1), (3, 8), (4, 9)):
        m.put(x, y, 'w'); m.put(W - 1 - x, y, 'w')
    for (x, y) in ((4, 3), (6, 2), (8, 4), (5, 5), (7, 6)):
        m.put(x, y, 'W'); m.put(W - 1 - x, y, 'W')
    for y in range(7, 14):
        m.seg(y, 5, 9, 's')
    m.seg(7, 5, 9, 'w')
    m.seg(13, 7, 9, 's')
    m.seg(10, 7, 7, 'E')
    m.seg(11, 5, 5, 'r')
    m.seg(12, 8, 9, 'o')
    m.seg(14, 8, 9, 'S')
    # blouse and knit vest, arms in blouse sleeves
    for y in range(14, 22):
        m.seg(y, 3, 9, 'c')
    for y in range(15, 22):
        m.seg(y, 4, 7, 'd')
    for y in range(16, 22):
        m.seg(y, 2, 2, 'c'); m.seg(y, 3, 3, 'C')
    m.seg(22, 1, 3, 's')
    # flowery momppae trousers, gathered at the ankle
    for y in range(22, 27):
        m.seg(y, 4, 9, 'm')
    for (x, y) in ((5, 23), (8, 24), (6, 26), (9, 22)):
        m.put(x, y, 'f'); m.put(W - 1 - x, y + 1, 'F')
    m.seg(27, 4, 8, 'k'); m.seg(28, 4, 8, 'k')
    m.shade({'s': ('S', 'l'), 'd': ('D', 'u'), 'c': ('C', None), 'm': ('M', None), 'w': ('W', 'v')})
    m.put(6, 9, 'l')
    return _frame(m, pal)


def theo():
    """Mr. Dongsu of the hardware store: navy cap, short graying hair, a moustache,
    a khaki work vest with pockets over a gray shirt, dark trousers, work boots. (id stays 'theo')"""
    pal = dict(BASE)
    pal.update({'H': P['hair'][1], 'J': P['stone'][2], 'K': P['hair'][3],
                'n': NAVY[2], 'N': NAVY[1], 'x': NAVY[3],
                'g': STEEL[2], 'G': STEEL[1], 'q': STEEL[3],
                'v': KHAKI[2], 'V': KHAKI[1], 'z': KHAKI[3],
                't': P['denim'][0], 'T': P['outline'], 'B': P['sun'][2]})
    m = PixelMap(W, H)
    # cap with a brim
    m.seg(1, 6, 9, 'n'); m.seg(2, 5, 9, 'n'); m.seg(3, 5, 9, 'n')
    m.seg(4, 4, 9, 'N')
    m.put(9, 2, 'x')
    for y in range(5, 13):
        m.seg(y, 5, 9, 's')
    m.seg(5, 5, 5, 'J'); m.seg(6, 5, 5, 'J'); m.seg(7, 5, 5, 'J')       # gray at the temples
    m.seg(6, 7, 8, 'H')                                                   # brows
    m.seg(8, 7, 7, 'E')
    m.seg(10, 7, 9, 'H')                                                  # moustache
    m.seg(11, 8, 9, 'o')
    m.seg(8, 4, 4, 's')                                                   # ears
    m.seg(13, 7, 9, 'S')
    # gray shirt, khaki work vest with pockets
    for y in range(13, 23):
        m.seg(y, 2, 9, 'g')
    for y in range(14, 23):
        m.seg(y, 3, 7, 'v')
    for (y0, x0) in ((16, 4), (19, 4)):
        m.seg(y0, x0, x0 + 2, 'V'); m.seg(y0 + 1, x0, x0 + 2, 'z')
    for y in range(16, 22):
        m.seg(y, 2, 2, 'g'); m.seg(y, 1, 1, 'G')
    m.seg(22, 1, 2, 's')
    for y in range(23, 27):
        m.seg(y, 5, 8, 't')
    m.seg(27, 4, 8, 'b'); m.seg(28, 4, 8, 'k')
    m.shade({'s': ('S', 'l'), 'g': ('G', 'q'), 'v': ('V', 'z'), 'n': ('N', 'x')})
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
