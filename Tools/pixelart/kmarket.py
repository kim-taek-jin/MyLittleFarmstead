"""The market as a Korean country market lane (direction C).

Same frame as the old market (585 x 328 px, 18.28 x 10.24 world units around (0.09, -0.08)) so the
Market scene's people and colliders still line up:
  Grandma Boksun (-6.0, 1.0) in front of the rice mill, Mr. Dongsu (-1.4, 1.0) in front of the hardware
  store, the shopkeeper (3.92, 3.01) under the blue tarp, the sharing box (-4.2, -2.0), exit on the west edge.

Exports (through generate.py):
  World/bg_market_<season>   the market every day
  World/market_fair          market-day stalls laid over it (popped-rice cart, produce stall, odds-and-ends stall)
  World/haggler              the old farmer you haggle with on market days
  World/merchant             the corner-store auntie (replaces the old merchant)
  World/well, World/tree     a Korean stone well with a bucket, a Korean red pine (gatherable tree)
"""
import os

from PIL import Image, ImageDraw, ImageFont

from pixel import Canvas, P, ramp, darken_c, lighten_c, seeded
import backgrounds as B
from backgrounds import BG, path, tuft, _h, SNOW, SUMMER, AUTUMN_GRASS, AUT_ORANGE, AUT_RED, AUT_YELLOW, TRAMPLED
import items
import kvillage as K
from haggle import PixelMap, OUTLINE

HERE = os.path.dirname(os.path.abspath(__file__))
FONT = os.path.join(HERE, '..', '..', 'Assets', 'Resources', 'Fonts', 'Galmuri11.ttf')

TARP = ramp('#163a6e', '#2456a0', '#3a7ad0', '#86b6f0')
BLOCK = ramp('#6c6a66', '#9c9a94', '#c4c2bc', '#e6e4de')
TIN = ramp('#1d3644', '#2d5568', '#457a90', '#7fb0c2')
RED_TIN = ramp('#4a1a16', '#7e2c22', '#b04432', '#d87a5e')
GREEN_SIGN = ramp('#123a24', '#1e5a36', '#2e7e4c', '#6ab486')
STYRO = ramp('#a8aab0', '#d0d2d6', '#eceef0', '#ffffff')
BASIN = ramp('#5a1414', '#9a2420', '#d0402c', '#f07a5e')     # red rubber basin (go-mu dae-ya)


def sign(text, w, h, board, ink):
    """A shop sign board with Korean text in the pixel font (crisp, no anti-aliasing)."""
    c = Canvas(w, h)
    c.box(0, 0, w - 1, h - 1, board)
    img = Image.new('L', (w, h), 0)
    d = ImageDraw.Draw(img)
    d.fontmode = '1'
    f = ImageFont.truetype(FONT, 12)
    l, t, r, b = d.textbbox((0, 0), text, font=f)
    d.text(((w - (r - l)) // 2 - l, (h - (b - t)) // 2 - t), text, font=f, fill=255)
    for y in range(h):
        for x in range(w):
            if img.getpixel((x, y)):
                c.set(x, y, ink)
    c.outline()
    return c


def building(width, wall, roof, label, board, ink, door='glass'):
    """A one-storey block building with a tin roof edge and a sign board."""
    c = Canvas(width, 86)
    c.box(0, 18, width - 1, 85, wall)
    for y in range(24, 85, 7):                                   # block courses
        for x in range(0, width):
            if (x + (y // 7) * 6) % 12 == 0:
                c.set(x, y - 3, wall[1])
        c.line(1, y, width - 2, y, wall[1])
    # tin roof edge with corrugations
    c.poly([(-2, 18), (4, 2), (width - 5, 2), (width + 1, 18)], roof, light_axis='y')
    for x in range(0, width, 4):
        for y in range(4, 17):
            if c.get(x, y) is not None:
                c.set(x, y, roof[1])
    c.rect(0, 17, width - 1, 18, roof[0])
    # sign board over the door
    sb = sign(label, min(width - 16, 64), 16, board, ink)
    for y in range(sb.h):
        for x in range(sb.w):
            p = sb.get(x, y)
            if p is not None:
                c.set((width - sb.w) // 2 + x, 22 + y, p)
    # door: sliding glass (aluminium frame) or a roll-up shutter half open
    dx = width // 2 - 12
    if door == 'glass':
        c.box(dx, 48, dx + 24, 85, P['metal'])
        for x0 in (dx + 2, dx + 13):
            c.rect(x0, 50, x0 + 9, 83, P['glass'][1])
            c.line(x0 + 1, 51, x0 + 4, 54, P['glass'][3])
    else:
        c.box(dx - 6, 44, dx + 30, 85, P['metal'])
        for y in range(46, 64, 2):
            c.line(dx - 5, y, dx + 29, y, P['metal'][1])
        c.rect(dx - 5, 64, dx + 29, 84, darken_c(P['wood'][0], 0.8))
    c.outline()
    return c


def tools_rack():
    """Shovels, a rake and buckets leaning out front of the hardware store."""
    c = Canvas(40, 40)
    c.shadow(20, 39, 18, 1.5)
    for (x, top, head) in ((6, 4, 'spade'), (13, 2, 'rake'), (20, 6, 'hoe')):
        c.line(x, top, x + 2, 34, P['wood'][2])
        if head == 'spade':
            c.poly([(x, 30), (x + 6, 30), (x + 6, 38), (x + 3, 40), (x, 38)], P['metal'], light_axis='x')
        elif head == 'rake':
            c.rect(x - 2, 34, x + 6, 35, P['metal'][2])
            for k in range(-2, 7, 2):
                c.rect(x + k, 36, x + k, 38, P['metal'][1])
        else:
            c.poly([(x, 31), (x + 7, 33), (x + 7, 36), (x, 34)], P['metal'], light_axis='y')
    for (x, col) in ((27, P['red']), (33, P['cloth'])):          # stacked plastic buckets
        c.poly([(x, 24), (x + 6, 24), (x + 5, 38), (x + 1, 38)], col, light_axis='x')
        c.rect(x, 24, x + 6, 25, col[3])
    c.outline()
    return c


def rice_sacks():
    c = Canvas(34, 26)
    c.shadow(17, 25, 16, 1.5)
    sack = ramp('#8a7a5a', '#c0ae84', '#e6d6aa', '#f8eed2')
    for (x, y) in ((2, 8), (16, 8), (9, 0)):
        c.poly([(x, y + 16), (x + 1, y + 3), (x + 15, y + 3), (x + 16, y + 16)], sack, light_axis='x')
        c.rect(x + 2, y + 1, x + 14, y + 3, sack[1])
        c.line(x + 5, y + 9, x + 11, y + 9, P['red'][2])
    c.outline()
    return c


def basin_of(fn):
    """A red rubber basin full of produce (the market-lane classic)."""
    c = Canvas(30, 18)
    c.shadow(15, 17, 14, 1.5)
    c.ellipse(15, 11, 14, 6, BASIN)
    c.ellipse(15, 8, 12, 3.5, BASIN, flat=True)
    mini = fn()
    for y in range(32):
        for x in range(32):
            p = mini.get(x, y)
            if p is not None and x % 3 == 0 and y % 3 == 0:
                for (ox, oy) in ((4, 0), (13, -1), (19, 1)):
                    c.set(ox + x // 3, oy + y // 3, p)
    c.outline()
    return c


def styro_box(season, kind=0):
    """A styrofoam box turned into a little vegetable bed (green onions, lettuce)."""
    c = Canvas(30, 22)
    c.shadow(15, 21, 14, 1.5)
    c.box(1, 9, 28, 21, STYRO)
    c.rect(2, 9, 27, 11, K.SOIL[2])
    if season == 'winter':
        c.rect(2, 8, 27, 10, SNOW[2])
    else:
        for x in range(4, 27, 3):
            if kind == 0:
                c.line(x, 9, x, 2 + (x % 4), P['leaf'][2]); c.set(x, 1 + (x % 4), P['leaf'][3])
            else:
                c.ellipse(x, 7, 2.2, 2, ramp('#2c5a24', '#4c8c34', '#86c04c', '#c8e888'))
    c.outline()
    return c


def plastic_chair(col):
    c = Canvas(14, 20)
    c.shadow(7, 19, 6, 1.2)
    c.rect(2, 2, 11, 9, col[2]); c.rect(2, 2, 11, 3, col[3])
    c.rect(1, 10, 12, 12, col[2])
    for x in (2, 11):
        c.rect(x, 13, x, 19, col[1])
    c.outline()
    return c


def hand_cart():
    """A two-wheeled hand cart (ri-eo-ka)."""
    c = Canvas(44, 28)
    c.shadow(22, 27, 20, 1.5)
    c.box(4, 8, 36, 16, P['wood'])
    for x in range(8, 36, 6):
        c.line(x, 9, x, 15, P['wood'][1])
    c.line(36, 12, 43, 6, P['metal'][1])
    c.ellipse(12, 21, 5, 5, P['metal'])
    c.ellipse(12, 21, 2, 2, P['stone'])
    c.outline()
    return c


def tarp_stall(width, tarp=TARP, goods=None):
    """A blue tarp canopy on poles over a low table of baskets."""
    c = Canvas(width, 70)
    c.shadow(width / 2, 69, width / 2 - 2, 2)
    for x in (3, width - 6):
        c.box(x, 10, x + 2, 69, P['metal'])
    c.poly([(0, 14), (6, 0), (width - 7, 0), (width - 1, 14)], tarp, light_axis='y')
    for x in range(0, width, 9):
        c.set(x, 14, tarp[0]); c.set(x + 1, 15, tarp[1])
    c.box(6, 44, width - 7, 56, P['woodlight'])
    for x in range(8, width - 9, 6):
        c.line(x, 57, x, 68, P['wood'][1])
    if goods:
        for i, fn in enumerate(goods):
            mini = fn()
            ox = 10 + i * ((width - 24) // max(1, len(goods)))
            for y in range(32):
                for x in range(32):
                    p = mini.get(x, y)
                    if p is not None and x % 3 == 0 and y % 3 == 0:
                        c.set(ox + x // 3, 34 + y // 3, p)
    c.outline()
    return c


def popped_rice_cart():
    """The popped-rice man's cart: a black pressure drum on a stand, big bags of puffed rice."""
    c = Canvas(64, 52)
    c.shadow(32, 51, 30, 2)
    c.box(8, 36, 56, 44, P['wood'])
    for x in (10, 54):
        c.rect(x, 44, x + 1, 51, P['wood'][0])
    drum = ramp('#141214', '#2a262a', '#46404a', '#787078')
    c.ellipse(24, 26, 12, 8, drum)
    c.rect(34, 24, 40, 28, drum[1])
    c.rect(10, 34, 38, 36, P['metal'][1])
    c.line(12, 34, 18, 18, P['metal'][1])
    bag = ramp('#9aa0a8', '#c8ccd4', '#eaedf2', '#ffffff')
    for (x, y) in ((42, 10), (50, 16)):
        c.poly([(x, y + 20), (x + 1, y), (x + 12, y), (x + 13, y + 20)], bag, light_axis='x')
        for k in range(4):
            c.set(x + 3 + k * 3, y + 6 + (k % 2) * 5, P['straw'][3])
    c.outline()
    return c


def clothes_stall():
    """Odds and ends on market day: hanging momppae trousers, rubber shoes, hats."""
    c = tarp_stall(70, tarp=ramp('#5a3a10', '#9a6a1e', '#d49a34', '#f4c870'))
    for (x, col) in ((14, ramp('#5a2a4a', '#9a3e6e', '#d0679a', '#f2a6c8')), (28, ramp('#1c3a5a', '#2e5a8a', '#4a86c0', '#8ab8e8')), (42, ramp('#3a4a1a', '#5e7a2a', '#86a83c', '#b8d070'))):
        c.rect(x, 16, x + 8, 30, col[2])
        c.rect(x, 16, x + 8, 17, col[3])
        c.line(x + 4, 22, x + 4, 30, col[1])
        for k in range(3):
            c.set(x + 2 + k * 2, 20 + k * 3, P['white'][3])
    for x in (18, 26, 34, 44):                                    # rubber shoes on the table
        c.rect(x, 40, x + 5, 42, P['white'][2]); c.rect(x, 42, x + 5, 42, P['white'][1])
    return c


# ---------------------------------------------------------------- people
def merchant():
    """The corner-store auntie: permed hair, a sun visor, an apron and arm sleeves, behind the counter."""
    pal = {
        's': P['skin'][2], 'S': P['skin'][1], 'l': P['skin'][3], 'E': OUTLINE, 'o': (120, 52, 40, 255), 'r': (232, 140, 120, 255),
        'H': P['hair'][1], 'J': P['hair'][2], 'K': P['hair'][0],
        'v': P['red'][2], 'V': P['red'][1],
        'a': P['cloth'][2], 'A': P['cloth'][1], 'q': P['cloth'][3],
        'f': P['white'][3], 'F': P['white'][1],
    }
    W, H = 20, 30
    m = PixelMap(W, H)
    m.seg(1, 6, 9, 'H'); m.seg(2, 4, 9, 'H')
    for y in range(3, 7):
        m.seg(y, 3, 9, 'H')
    for (x, y) in ((4, 2), (6, 1), (3, 7)):
        m.put(x, y, 'H'); m.put(W - 1 - x, y, 'H')
    m.seg(5, 3, 9, 'v'); m.seg(6, 2, 9, 'V')                       # sun visor
    for y in range(7, 14):
        m.seg(y, 5, 9, 's')
    m.seg(9, 7, 7, 'E')
    m.seg(11, 5, 5, 'r')
    m.seg(12, 8, 9, 'o')
    m.seg(14, 8, 9, 'S')
    for y in range(14, 30):
        m.seg(y, 3, 9, 'a')
    for y in range(16, 30):
        m.seg(y, 6, 9, 'f')                                       # apron
    for y in range(17, 23):
        m.seg(y, 1, 2, 'q')                                       # arm sleeves (to-si)
    m.seg(23, 1, 2, 's')
    m.shade({'s': ('S', 'l'), 'a': ('A', 'q'), 'f': ('F', None), 'H': ('K', 'J')})
    body = m.canvas(pal)
    c = Canvas(32, 48)
    for y in range(body.h):
        for x in range(body.w):
            p = body.get(x, y)
            if p is not None:
                c.set(x + 5, y, p)
    # counter in front of her
    c.box(1, 30, 30, 47, P['woodlight'])
    c.line(1, 38, 30, 38, P['woodlight'][1])
    for (ox, fn) in ((3, items.apple), (13, items.carrot), (21, items.corn)):
        mini = fn()
        for y in range(32):
            for x in range(32):
                p = mini.get(x, y)
                if p is not None and x % 3 == 0 and y % 3 == 0:
                    c.set(ox + x // 3, 25 + y // 3, p)
    c.outline()
    return c


def haggler():
    """The old farmer who haggles on market day: straw hat, white hanbok-style shirt, a long pipe."""
    pal = {
        's': P['skin'][1], 'S': P['skin'][0], 'l': P['skin'][2], 'E': OUTLINE, 'o': (120, 52, 40, 255),
        'y': P['straw'][2], 'Y': P['straw'][1], 'h': P['straw'][3],
        'w': P['white'][2], 'W': P['white'][1], 'u': P['white'][3],
        'g': P['stone'][2], 'G': P['stone'][1], 'b': P['wood'][1], 'k': P['wood'][0], 'p': P['wood'][2],
    }
    W, H = 20, 29
    m = PixelMap(W, H)
    m.seg(2, 6, 9, 'y'); m.seg(3, 5, 9, 'y')
    m.seg(4, 2, 9, 'Y')                                           # wide straw hat
    for y in range(5, 13):
        m.seg(y, 5, 9, 's')
    m.seg(7, 6, 8, 'g')                                           # gray brows
    m.seg(8, 7, 7, 'E')
    m.seg(10, 6, 9, 'g'); m.seg(11, 7, 9, 'g')                     # white beard
    m.seg(13, 7, 9, 's')
    for y in range(14, 23):
        m.seg(y, 3, 9, 'w')
    for y in range(16, 22):
        m.seg(y, 2, 2, 'w')
    m.seg(22, 1, 2, 's')
    m.seg(16, 8, 9, 'W')                                          # jacket tie
    for y in range(23, 27):
        m.seg(y, 5, 8, 'g')
    m.seg(27, 4, 8, 'b'); m.seg(28, 4, 8, 'k')
    m.shade({'s': ('S', 'l'), 'w': ('W', 'u'), 'y': ('Y', 'h'), 'g': ('G', None)})
    body = m.canvas(pal)
    c = Canvas(32, 32)
    c.shadow(16, 30, 7, 1.5, alpha=60)
    for y in range(body.h):
        for x in range(body.w):
            p = body.get(x, y)
            if p is not None:
                c.set(x + 5, y + 1, p)
    c.line(19, 14, 27, 10, P['wood'][1])                          # a long pipe
    c.set(28, 9, P['metal'][1])
    return c


# ---------------------------------------------------------------- world objects
def well():
    """A round stone well with a wooden lid half open and a bucket on a rope (du-re-bak)."""
    c = Canvas(32, 40)
    c.shadow(16, 37, 13, 2)
    c.ellipse(16, 30, 12, 7, K.WALLSTONE)
    rnd = seeded(7)
    for y in range(24, 37, 3):
        for x in range(5, 28, 5):
            xx = x + (y // 3 % 2) * 2
            if c.get(xx, y) is not None:
                c.set(xx, y, K.WALLSTONE[1])
    c.ellipse(16, 24, 11, 4, K.WALLSTONE, flat=True)
    c.ellipse(16, 24, 8, 2.6, P['water'], flat=True)
    c.rect(6, 21, 16, 23, P['wood'][2]); c.rect(6, 21, 16, 21, P['wood'][3])   # half lid
    c.line(23, 8, 23, 22, P['wood'][1])                           # post and the rope
    c.line(23, 8, 18, 8, P['wood'][1])
    c.line(18, 8, 18, 14, P['straw'][1])
    c.box(15, 14, 21, 19, P['woodlight'])                          # bucket
    c.outline()
    return c


def tree():
    """A Korean red pine (gatherable): reddish trunk, flat layered needles."""
    t = K.pine(seed=3, h=48)
    c = Canvas(32, 48)
    for y in range(48):
        for x in range(32):
            p = t.get(x + 8, y)
            if p is not None:
                c.set(x, y, p)
    return c


# ---------------------------------------------------------------- the market lane
def market(season='summer'):
    bg = BG(585, 328, 18.28, 10.24, (0.09, -0.08))
    bg.season = season
    snow = season == 'winter'
    ground = TRAMPLED if snow else K.YARD
    # behind the shops: the back hill with pines
    hill = {'winter': SNOW, 'spring': P['leaf'], 'summer': SUMMER, 'autumn': AUTUMN_GRASS}[season]
    for y in range(0, 90):
        for x in range(bg.w):
            bg.c.set(x, y, darken_c(hill[2], 0.78) if (x * 7 + y * 3) % 11 else darken_c(hill[1], 0.8))
    for i, x in enumerate(range(-10, bg.w + 20, 30)):
        bg.prop(K.pine(seed=60 + i, h=40 + (i * 7) % 12), x=x + (i * 13) % 9, y=34 + (i * 5) % 8)
    path(bg, [(0, 80, bg.w - 1, bg.h - 1)], r=ground)
    # a concrete apron in front of the shops, with a gutter line
    for y in range(80, 112):
        for x in range(bg.w):
            col = BLOCK[2] if not snow else SNOW[2]
            if (x // 24 + y // 16) % 2 == 0 and not snow:
                col = BLOCK[1] if (x % 24 == 0 or y % 16 == 0) else col
            bg.c.set(x, y, col)
    for x in range(bg.w):
        bg.c.set(x, 112, darken_c(ground[1], 0.8)); bg.c.set(x, 113, ground[1])
    # tire tracks and puddles (snow patches in winter)
    for k in range(28):
        cx, cy = 30 + (k * 151) % 525, 150 + (k * 89) % 160
        if snow:
            for y in range(cy - 4, cy + 5):
                for x in range(cx - 9, cx + 10):
                    if ((x - cx) / 9) ** 2 + ((y - cy) / 4) ** 2 <= 1 and (x + y) % 5:
                        bg.c.set(x, y, SNOW[2])
        elif k % 4 == 0 and season in ('spring', 'summer'):
            for y in range(cy - 2, cy + 3):
                for x in range(cx - 7, cx + 8):
                    if ((x - cx) / 7) ** 2 + ((y - cy) / 2.5) ** 2 <= 1:
                        bg.c.set(x, y, P['water'][1] if y < cy else P['water'][2])
        elif season == 'autumn':
            B.leaf_pile(bg, cx, cy, 3)
    for x in range(0, bg.w, 2):                                    # tire tracks along the lane
        for ty in (238, 252):
            if _h(x // 6, ty, 3) % 3:
                bg.c.set(x, ty, darken_c(ground[2], 0.88))

    # the row of shops
    bg.prop(building(150, BLOCK, TIN, '방앗간', ramp('#e8e4d8', '#f4f0e4', '#fffdf6', '#ffffff'), P['red'][1], door='shutter'), x=95, y=88)
    bg.prop(building(120, ramp('#6e5a3a', '#a8865a', '#d0b07e', '#ecd4a6'), RED_TIN, '철물점', TARP, P['white'][3]), x=245, y=88)
    bg.prop(building(110, BLOCK, TIN, '행복슈퍼', GREEN_SIGN, P['white'][3]), x=530, y=88)

    # shopkeeper's tarp stall (the shopkeeper stands at world 3.92, 3.01)
    mx, my = bg.px(3.92, 3.01)
    bg.prop(tarp_stall(120, goods=[items.apple, items.carrot, items.potato, items.corn]), x=mx, y=my + 40)

    # out front: rice sacks by the mill, tools by the hardware store, a low bench and chairs by the store
    bg.prop(rice_sacks(), wx=-8.0, wy=2.1)
    bg.prop(tools_rack(), wx=0.4, wy=2.3)
    pc = K.pyeongsang()
    for x in range(10, 31):
        for y in range(9, 13):
            if pc.get(x, y) in (P['red'][2], P['red'][1], P['straw'][2], P['straw'][3]):
                pc.set(x, y, P['woodlight'][2])
    bg.prop(pc, wx=7.6, wy=1.0)
    bg.prop(plastic_chair(P['red']), wx=6.3, wy=1.1)
    bg.prop(plastic_chair(P['cloth']), wx=8.7, wy=0.9)
    for (wx, wy, fn) in [(1.6, 1.4, items.carrot), (6.0, 2.0, items.apple)]:
        bg.prop(basin_of(fn), wx=wx, wy=wy)
    bg.prop(hand_cart(), wx=8.2, wy=-4.0)

    # styrofoam-box vegetable beds along the bottom (where the old planters were)
    for i, wx in enumerate((-6.0, -3.0, 0.0, 3.0, 6.0)):
        bg.prop(styro_box(season, i % 2), wx=wx, wy=-4.6)

    # a utility pole between the mill and the hardware store
    bg.prop(K.pole(speaker=True), wx=-3.75, wy=1.7)       # the village loudspeaker (Village Head Park's broadcasts)
    img = bg.image()
    return img


def market_fair():
    """Market-day stalls, drawn on a transparent sheet the size of the market background."""
    bg = BG(585, 328, 18.28, 10.24, (0.09, -0.08))
    bg.c = Canvas(585, 328)
    bg.prop(popped_rice_cart(), wx=-1.6, wy=-2.7)
    bg.prop(tarp_stall(84, tarp=ramp('#1a4a1a', '#2a7a2a', '#40a840', '#8ad88a'), goods=[items.corn, items.potato, items.carrot]), wx=2.6, wy=-3.0)
    bg.prop(clothes_stall(), wx=6.4, wy=-2.6)
    return bg.image()


FAIR_SPOTS = [(-1.6, -2.7, 1.0), (2.6, -3.0, 1.3), (6.4, -2.6, 1.1)]   # world x, y (bottom middle), half width: colliders in the game
HAGGLER_AT = (2.6, -2.2)


if __name__ == '__main__':
    import sys
    out = sys.argv[1] if len(sys.argv) > 1 else '.'
    os.makedirs(out, exist_ok=True)
    for s in ('winter', 'spring', 'summer', 'autumn'):
        market(s).save(os.path.join(out, f'bg_market_{s}.png'))
    market_fair().save(os.path.join(out, 'market_fair.png'))
    print('ok')
