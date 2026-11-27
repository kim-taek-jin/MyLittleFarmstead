"""F-1, spring: the paddies (dry, flooded, planted), the seedbed, the sluice, the rice-planting line,
the Samjitnal flower-pancake lid, spring items, the pepper crop, and Mrs. Myeongsuk of the corner store.

Paddy overlays are the size of the whole farm map (like bg_farmmap_*) and transparent except inside the
paddies, so the game lays them over the spring background as the season goes on:
    spring background (dry, ploughed) -> paddy_flooded (water let in) -> paddy_planted (after rice planting)
The levees drawn by the background stay visible because the overlay leaves their bands empty.
"""
import json
import math
import os

from PIL import Image

from pixel import Canvas, P, ramp, darken_c, lighten_c
from haggle import PixelMap
from characters import W, H
import kvillage as K
import villagers as V
import crops as CR

HERE = os.path.dirname(os.path.abspath(__file__))
MAP = os.path.join(HERE, '..', '..', 'Assets', 'Resources', 'Maps', 'farm.json')

PLOUGHED = ramp('#3e2a18', '#5c4128', '#7c5c3a', '#9c7c56')
PADDY_WATER = ramp('#33505a', '#4f727c', '#7898a0', '#b8d2d2')
MUD = ramp('#3a2a1a', '#584230', '#76603f', '#957e5c')
SEEDLING = ramp('#2c5a24', '#4a8c34', '#7cbc4c', '#b8e27a')
AZALEA = ramp('#7a1e4e', '#b8357a', '#e566a6', '#f8b2d4')
TRAY = ramp('#141414', '#262626', '#3a3a3a', '#585858')


def _h(x, y, k=0):
    n = (x * 374761393 + y * 668265263 + k * 2147483647) & 0xFFFFFFFF
    n = (n ^ (n >> 13)) * 1274126177 & 0xFFFFFFFF
    return n ^ (n >> 16)


# ---------------------------------------------------------------- paddy pixels (absolute map pixel coords)
def dry_px(x, y):
    """Early spring: last year's paddy ploughed into wavy furrows, clods, a stubble stalk, a weed here and there."""
    w = int(round(1.3 * math.sin(x / 13.0 + (y // 7) * 0.8)))
    k = (y + w) % 7
    col = PLOUGHED[2]
    if k == 0:
        col = PLOUGHED[1]
    elif k == 1:
        col = PLOUGHED[3]
    elif k == 6 and _h(x // 3, y // 7, 1) % 3 == 0:
        col = darken_c(PLOUGHED[2], 0.92)
    if _h(x // 2, y // 2, 2) % 31 == 0:
        col = PLOUGHED[1]                                    # clods
    if k in (2, 3) and _h(x, y // 7, 3) % 41 == 0:
        col = K.STUBBLE[2] if k == 2 else K.STUBBLE[1]       # last year's stubble
    if k in (3, 4) and _h(x // 2, y // 7, 4) % 53 == 0:
        col = P['leaf'][2] if k == 3 else P['leaf'][1]       # a weed
    return col


def flooded_px(x, y):
    """Water let in: murky paddy water, the sky in short broken glints, mud showing through in places."""
    col = PADDY_WATER[2]
    if (y + (x // 19) % 2) % 5 == 0 and _h(x // 4, y, 5) % 3 != 0:
        col = PADDY_WATER[1]                                 # faint ripples
    if _h((x + (y // 2) * 3) // 6, y // 2, 6) % 13 == 0 and y % 2 == 0:
        col = PADDY_WATER[3]                                 # sky glints
    if _h(x // 2, y // 2, 7) % 97 == 0 and _h(x, y, 9) % 2 == 0:
        col = MUD[2]                                         # a little mud showing through
    return col


def planted_px(x, y):
    """Rice seedlings in neat hand-planted rows (a little uneven), each tuft with a dark reflection under it."""
    col = flooded_px(x, y)
    row = y // 7
    lx = (x + row * 2 + _h(row, 0, 8) % 3) % 6
    ly = (y + (_h(x // 6, row, 11) % 2)) % 7
    if ly == 2 and lx == 2:
        col = SEEDLING[3]
    elif ly == 3 and lx in (1, 2, 3):
        col = SEEDLING[2] if lx != 2 else SEEDLING[3]
    elif ly == 4 and lx in (1, 2, 3):
        col = SEEDLING[1] if lx != 2 else SEEDLING[2]
    elif ly == 5 and lx == 2:
        col = SEEDLING[1]
    elif ly == 6 and lx in (1, 2, 3):
        col = darken_c(PADDY_WATER[1], 0.85)
    return col


STYLES = {'dry': dry_px, 'flooded': flooded_px, 'planted': planted_px}


def _paddy_cells():
    with open(MAP) as f:
        m = json.load(f)
    rows = m['rows']
    groups = []
    for chars in ('R', 'Sb'):
        g = set()
        for r, row in enumerate(rows):
            for c, ch in enumerate(row):
                if ch in chars:
                    g.add((c, r))
        groups.append(g)
    return m, groups


def inside_levees(c, r, group, x, y):
    """True if pixel (x, y) of cell (c, r) is paddy, not one of the levee bands the background draws."""
    x0, y0, x1, y1 = c * 32, r * 32, c * 32 + 31, r * 32 + 31
    if (c, r - 1) not in group and y <= y0 + 4:
        return False
    if (c - 1, r) not in group and x <= x0 + 2:
        return False
    if (c + 1, r) not in group and x >= x1 - 1:
        return False
    if (c, r + 1) not in group and y >= y1 - 1:
        return False
    return True


def paddy_overlay(style):
    m, groups = _paddy_cells()
    img = Image.new('RGBA', (m['width'] * 32, m['height'] * 32), (0, 0, 0, 0))
    px = img.load()
    fn = STYLES[style]
    for g in groups:
        for (c, r) in g:
            for y in range(r * 32, r * 32 + 32):
                for x in range(c * 32, c * 32 + 32):
                    if inside_levees(c, r, g, x, y):
                        px[x, y] = fn(x, y)
    return img


# ---------------------------------------------------------------- yard and paddy props
def seedbed(stage):
    """Black plastic seedling trays on a raised dirt bed: 0 soil, 1 seed sown, 2 shoots, 3 ready seedlings."""
    c = Canvas(48, 22)
    c.shadow(24, 20, 23, 2)
    c.rect(1, 9, 46, 19, K.YARD[1]); c.rect(1, 9, 46, 9, K.YARD[2])               # the raised bed
    c.rect(1, 18, 46, 19, K.YARD[0])
    for i, x0 in enumerate((3, 18, 33)):
        c.rect(x0, 7, x0 + 12, 16, TRAY[1]); c.rect(x0, 7, x0 + 12, 7, TRAY[3]); c.rect(x0, 16, x0 + 12, 16, TRAY[0])
        for y in range(8, 16):
            for x in range(x0 + 1, x0 + 12):
                col = PLOUGHED[2] if (x + y) % 4 else PLOUGHED[1]
                if stage == 1 and _h(x, y, i) % 6 == 0:
                    col = P['straw'][3]                                         # rice seed
                if stage == 2 and _h(x, y, i + 9) % 3 == 0:
                    col = SEEDLING[2] if y % 2 else SEEDLING[3]
                if stage == 3:
                    col = SEEDLING[2] if (x + y * 2) % 3 else SEEDLING[1]
                c.set(x, y, col)
        if stage == 3:                                                          # seedlings stand above the tray
            for x in range(x0 + 1, x0 + 12):
                h = 2 + _h(x, i, 5) % 3
                for y in range(7 - h, 8):
                    c.set(x, y, SEEDLING[3] if y == 7 - h else SEEDLING[2])
    c.outline()
    c.nosnow = True
    return c


def sluice():
    """The mul-kko: a gap cut in the levee with a wooden board, water trickling into the paddy."""
    c = Canvas(20, 14)
    c.rect(0, 6, 19, 13, P['leaf'][2]); c.rect(0, 6, 19, 6, P['leaf'][3])
    c.rect(7, 6, 12, 13, PADDY_WATER[2]); c.set(8, 9, PADDY_WATER[3]); c.set(11, 11, PADDY_WATER[3])
    c.rect(6, 1, 13, 7, P['wood'][2]); c.rect(6, 1, 13, 1, P['wood'][3]); c.rect(6, 7, 13, 7, P['wood'][1])
    c.line(9, 2, 9, 6, P['wood'][1])
    c.outline()
    return c


def rice_tuft():
    c = Canvas(8, 10)
    for (x, y, col) in [(3, 1, SEEDLING[3]), (2, 2, SEEDLING[2]), (4, 2, SEEDLING[2]), (1, 3, SEEDLING[2]), (5, 3, SEEDLING[2]),
                        (3, 3, SEEDLING[3]), (3, 4, SEEDLING[1]), (2, 5, SEEDLING[1]), (4, 5, SEEDLING[1]), (3, 6, SEEDLING[1])]:
        c.set(x, y, col)
    for x in (2, 3, 4):
        c.set(x, 8, darken_c(PADDY_WATER[1], 0.8))
    return c


def monaegi_line(width=300):
    """The planting line (motjul): a rope stretched across the paddy with red cloth marks every hand-span."""
    c = Canvas(width, 8)
    for x in range(width):
        y = 4 + int(round(1.2 * math.sin(x / width * math.pi)))
        c.set(x, y, P['straw'][1]); c.set(x, y - 1, P['straw'][2])
        if x % 12 == 6:
            c.set(x, y - 2, P['red'][2]); c.set(x, y + 1, P['red'][1])
    for x0 in (0, width - 4):                                     # stakes at both ends
        c.rect(x0, 0, x0 + 2, 7, P['wood'][1]); c.set(x0 + 1, 0, P['wood'][3])
    c.nosnow = True
    return c


def hwajeon_lid():
    """Samjitnal: an upturned cauldron lid on three stones over a small fire, flower pancakes frying on it."""
    c = Canvas(44, 32)
    c.shadow(22, 30, 20, 2)
    for (x, y, col) in [(17, 25, P['red'][2]), (20, 23, P['sun'][2]), (23, 24, P['red'][2]), (26, 25, P['sun'][3]), (21, 26, P['red'][1])]:
        c.set(x, y, col); c.set(x + 1, y, col)
    for x0 in (8, 22, 36):
        c.ellipse(x0, 26, 5, 3.5, K.WALLSTONE)
    lid = ramp('#2a2320', '#433933', '#5e524a', '#80746a')            # old cast iron, rust-brown where it's worn
    c.ellipse(22, 17, 20, 6.5, lid)
    c.ellipse(22, 16, 17, 4.5, [lid[2]] * 4, flat=True)                  # the oiled cooking face
    batter = ramp('#c0ae8c', '#e2d4b8', '#f2ead8', '#fffaf0')
    for (x, y) in ((13, 16), (22, 15), (31, 16)):
        c.ellipse(x, y, 4.5, 2.6, batter)
        c.set(x - 1, y - 1, AZALEA[3]); c.set(x, y - 1, AZALEA[2]); c.set(x + 1, y - 1, AZALEA[3])
        c.set(x, y, AZALEA[1]); c.set(x + 2, y, P['leaf'][2])
    c.outline()
    c.nosnow = True
    return c


def azalea_ground():
    c = Canvas(16, 14)
    c.line(8, 13, 8, 5, P['wood'][1]); c.line(8, 9, 4, 6, P['wood'][1]); c.line(8, 8, 12, 5, P['wood'][1])
    for (x, y) in ((4, 4), (8, 3), (12, 4), (6, 7), (10, 7)):
        c.ellipse(x, y, 2, 1.8, AZALEA)
        c.set(x, y, AZALEA[3])
    c.outline()
    return c


# ---------------------------------------------------------------- item icons (32 x 32)
def azalea():
    c = Canvas(32, 32)
    c.line(10, 28, 18, 12, P['wood'][1]); c.line(11, 28, 19, 12, P['wood'][2]); c.line(15, 19, 24, 16, P['wood'][1])
    for (x, y, s) in ((18, 10, 4.5), (25, 14, 4), (12, 16, 3.5), (21, 20, 3)):
        c.ellipse(x, y, s, s * 0.85, AZALEA)
        c.set(int(x), int(y), AZALEA[0]); c.set(int(x), int(y) - 1, P['sun'][3])
    c.outline()
    return c


def azalea_hwajeon():
    c = Canvas(32, 32)
    c.shadow(16, 28, 13, 2)
    c.ellipse(16, 24, 14, 4, P['white'])                                     # a plate
    for (x, y) in ((11, 19), (21, 19), (16, 14)):
        c.ellipse(x, y, 6, 3, ramp('#c8b89a', '#e8dcc4', '#f6f0e2', '#ffffff'))
        c.set(x, y - 1, AZALEA[2]); c.set(x - 1, y, AZALEA[2]); c.set(x + 1, y, AZALEA[3]); c.set(x, y, AZALEA[1])
        c.set(x + 2, y, P['leaf'][2])
    c.outline()
    return c


def rice_flour():
    c = Canvas(32, 32)
    c.shadow(16, 29, 11, 2)
    c.poly([(8, 28), (9, 11), (23, 11), (24, 28)], P['paper'], light_axis='x')
    c.ellipse(16, 11, 7, 2.5, P['white'], flat=True)
    c.rect(11, 17, 21, 22, P['white'][3]); c.line(13, 19, 19, 19, P['jam'][1]); c.line(13, 21, 17, 21, P['jam'][1])
    c.outline()
    return c


def pepper_seedling():
    c = Canvas(32, 32)
    c.shadow(16, 29, 8, 2)
    c.poly([(10, 28), (9, 19), (23, 19), (22, 28)], TRAY, light_axis='x')       # a little black pot
    c.ellipse(16, 19, 7, 1.6, PLOUGHED, flat=True)
    c.line(16, 18, 16, 9, P['leaf'][1])
    for (x, y, dx) in ((16, 9, -1), (16, 12, 1), (16, 15, -1)):
        c.ellipse(x + dx * 4, y, 4, 1.8, P['leaf'])
    c.outline()
    return c


def red_pepper():
    """Two long, slightly curved red peppers with green caps."""
    c = Canvas(32, 32)
    c.shadow(16, 29, 11, 2)
    for (x0, y0, bend) in ((8, 7, 3), (19, 5, 3)):
        for t in range(18):
            f = t / 17
            x = x0 + bend * math.sin(f * math.pi * 0.8) + f * 3
            y = y0 + f * 20
            r = 2.3 * (1 - f) + 0.6
            c.ellipse(x, y, r, r, P['red'])
        c.set(int(x0 + 1), int(y0 + 3), P['red'][3])
        c.rect(int(x0) - 1, y0 - 2, int(x0) + 2, y0 - 1, P['leaf'][1]); c.set(int(x0) + 1, y0 - 4, P['leaf'][2]); c.set(int(x0) + 1, y0 - 3, P['leaf'][2])
    c.outline()
    return c


# ---------------------------------------------------------------- pepper crop (32 x 32, like crops.py)
def pepper_2():
    c = Canvas(CR.S, CR.S)
    CR.mound(c)
    CR.stem(c, 16, CR.GROUND - 1, 16, CR.GROUND - 13)
    for (x, y, dx) in ((16, 15, -1), (16, 18, 1), (16, 21, -1), (16, 23, 1)):
        CR.leaf(c, x, y, dx * 5, -1, 5, P['leaf'])
    for (x, y) in ((12, 17), (20, 20)):
        c.set(x, y, P['white'][3])                                            # small white flowers
    c.outline()
    return c


def pepper_3():
    c = pepper_2()
    for (x, y) in ((11, 20), (21, 23), (14, 23), (19, 17)):
        c.rect(x, y, x, y + 4, P['red'][2]); c.set(x, y + 4, P['red'][1]); c.set(x, y - 1, P['leaf'][1])
    return c


CROP_STAGES = [CR.seedling, CR.sprout, pepper_2, pepper_3]


# ---------------------------------------------------------------- Mrs. Myeongsuk of Happy Mart (행복슈퍼 명숙 아주머니)
CARDIGAN = ramp('#3e2a52', '#5c4078', '#8466a4', '#b49ad0')
APRON = ramp('#7a1e28', '#b03440', '#d8606a', '#f4a4a8')


def myeong():
    """Mrs. Myeongsuk: a tight black perm, a lavender cardigan, a red check apron over dark slacks, slippers."""
    pal = dict(V.BASE)
    pal.update({'w': (38, 30, 30, 255), 'W': (62, 50, 48, 255), 'v': (88, 72, 68, 255),
                'd': CARDIGAN[2], 'D': CARDIGAN[1], 'u': CARDIGAN[3],
                'a': APRON[2], 'A': APRON[1], 'q': APRON[3], 'z': P['white'][3],
                'g': (52, 54, 62, 255), 'G': (36, 38, 44, 255)})
    m = PixelMap(W, H)
    m.seg(2, 5, 9, 'w')
    for y in range(3, 8):
        m.seg(y, 4, 9, 'w')
    for (x, y) in ((4, 2), (6, 1), (8, 1), (3, 5), (3, 8)):
        m.put(x, y, 'w'); m.put(W - 1 - x, y, 'w')
    for (x, y) in ((5, 3), (7, 2), (6, 5), (8, 4), (4, 6)):
        m.put(x, y, 'W'); m.put(W - 1 - x, y, 'v')
    for y in range(7, 14):
        m.seg(y, 5, 9, 's')
    m.seg(7, 5, 9, 'w')
    m.seg(10, 7, 7, 'E')
    m.seg(11, 5, 5, 'r')
    m.seg(12, 8, 9, 'o')
    m.seg(13, 7, 9, 'S')
    for y in range(14, 23):                     # cardigan
        m.seg(y, 2, 9, 'd')
    for y in range(15, 23):                     # apron bib and skirt with a check
        m.seg(y, 5, 9, 'a')
    for y in range(16, 23, 2):
        m.seg(y, 5, 9, 'A')
    m.seg(14, 7, 9, 'z')
    m.seg(22, 1, 2, 's')
    for y in range(23, 27):
        m.seg(y, 5, 8, 'g')
    m.seg(27, 4, 8, 'G'); m.seg(28, 4, 8, 'b')
    m.shade({'s': ('S', 'l'), 'd': ('D', 'u'), 'a': ('A', 'q')})
    return V._frame(m, pal)


def myeong_sprites():
    import k3
    out = {}
    for i, c in enumerate(V.stages(myeong())):
        out[f'myeong_{i}'] = c
    for i, c in enumerate(V.stages(k3.sit(myeong(), 24))):
        out[f'myeong_sit_{i}'] = c
    return out


def world_sprites():
    out = {f'seedbed_{i}': seedbed(i) for i in range(4)}
    out.update({'sluice': sluice(), 'rice_tuft': rice_tuft(), 'monaegi_line': monaegi_line(), 'hwajeon_lid': hwajeon_lid(),
                'azalea_ground': azalea_ground()})
    return out


def item_icons():
    return {'azalea': azalea(), 'azalea_hwajeon': azalea_hwajeon(), 'rice_flour': rice_flour(),
            'pepper_seedling': pepper_seedling(), 'red_pepper': red_pepper()}
