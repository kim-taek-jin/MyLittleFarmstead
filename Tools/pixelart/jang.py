"""K-2, making sauces (jang): the agungi, the meju rack, open jar lids and the new item icons.

World sprites are drawn at 32 px per tile and placed by their bottom-middle (AreaKit / JangSpots.SetArt).
The jar-lid overlay is the same size as kvillage.jangdokdae() so it lines up with the platform drawn
into the farm background.
"""
from pixel import Canvas, P, ramp, darken_c, lighten_c, seeded
import kvillage as K

CLAY = ramp('#4a3424', '#7a5a40', '#a8835e', '#cfae84')       # mud-plastered stove
IRON = ramp('#141418', '#26262c', '#3e3e48', '#6a6a78')         # cast-iron cauldron
MEJU_FRESH = ramp('#7a5a2a', '#b89258', '#dcbc82', '#f2dcaa')
MEJU_DRY = ramp('#3e2610', '#6a4420', '#94683a', '#bc915c')
SAUCE = ramp('#120804', '#24120a', '#3c2010', '#6a3e1e')
DOENJANG = ramp('#4a2c10', '#7a4e1e', '#a2702e', '#c89a52')
TOFU = ramp('#a8a08c', '#d6d0bc', '#f0ecdc', '#ffffff')
STEAM = [(255, 255, 255, 150), (255, 255, 255, 110), (255, 255, 255, 70)]


# ---------------------------------------------------------------- agungi (부뚜막 + 가마솥)
def agungi(lit=False):
    c = Canvas(38, 34)
    c.shadow(19, 32, 18, 2)
    # the stove: a mud-plastered block with a stone rim
    for y in range(14, 32):
        for x in range(2, 36):
            t = CLAY[2] if (x * 3 + y * 5) % 11 else CLAY[1]
            c.set(x, y, t)
    c.rect(2, 14, 35, 15, CLAY[3])                    # top edge catches the light
    c.rect(34, 16, 35, 31, CLAY[1])                   # right side in shade
    for x in range(3, 35, 5):                         # a few stones showing through the plaster
        c.rect(x, 29, x + 3, 31, K.WALLSTONE[2 if x % 2 else 1])
    # the fire mouth (arched)
    mouth = [(12, 31), (12, 25), (14, 22), (24, 22), (26, 25), (26, 31)]
    c.poly(mouth, None, solid=(18, 10, 8, 255))
    if lit:
        c.poly([(14, 31), (14, 27), (16, 25), (22, 25), (24, 27), (24, 31)], None, solid=P['fire'][1])
        for (x, y, k) in [(16, 27, 2), (19, 25, 3), (22, 28, 2), (18, 29, 3), (20, 28, 3)]:
            c.set(x, y, P['fire'][k]); c.set(x, y + 1, P['fire'][k - 1])
        c.rect(14, 30, 24, 31, P['fire'][3])           # embers
        for (x, y) in [(13, 21), (25, 21), (19, 20)]:  # glow on the plaster
            c.set(x, y, lighten_c(CLAY[3], 0.3))
    else:
        c.rect(14, 30, 24, 31, (60, 40, 30, 255))      # cold ash
        c.set(17, 29, P['wood'][1]); c.set(18, 29, P['wood'][2]); c.set(21, 30, P['wood'][1])
    # the cauldron sitting in the stove top, with its heavy wooden lid
    c.ellipse(19, 14, 13, 5, IRON)
    c.rect(5, 12, 33, 14, IRON[1])
    c.set(5, 13, IRON[3]); c.set(33, 13, IRON[2])     # the two "ears" of the gamasot
    c.ellipse(19, 10, 10, 3.2, P['wood'], flat=False)
    c.rect(17, 6, 21, 8, P['wood'][2]); c.line(17, 6, 21, 6, P['wood'][3])   # lid handle
    if lit:
        for i, x in enumerate((13, 19, 25)):
            for k in range(3):
                c.set(x + (k % 2), 4 - k * 2 + (i % 2), STEAM[k])
    c.outline()
    return c


# ---------------------------------------------------------------- meju rack (메주 걸대)
def _meju_block(c, x, y, ramp_, dried):
    c.box(x, y, x + 5, y + 6, ramp_)
    if dried:                                         # cracks and a white bloom
        c.set(x + 2, y + 3, ramp_[0]); c.set(x + 3, y + 4, ramp_[0]); c.set(x + 1, y + 5, (230, 226, 210, 255))
    # the straw tie that holds it
    c.line(x - 1, y + 2, x + 6, y + 2, P['straw'][2])
    c.line(x + 2, y - 3, x + 2, y, P['straw'][1]); c.line(x + 3, y - 3, x + 3, y, P['straw'][2])


def meju_rack(state='empty', snow=False):
    c = Canvas(40, 40)
    c.shadow(20, 38, 18, 2)
    # two posts with a crossbar, and a little straw roof against the snow
    for x in (4, 34):
        c.box(x, 10, x + 2, 37, P['wood'])
    c.rect(2, 11, 37, 12, P['wood'][1]); c.line(2, 11, 37, 11, P['wood'][3])
    c.poly([(0, 10), (6, 3), (34, 3), (40, 10)], P['straw'], light_axis='y')
    for x in range(3, 38, 3):
        c.line(x, 5, x - 1, 9, P['straw'][1])
    c.outline()
    if snow:
        from backgrounds import snow_cap
        snow_cap(c)                                   # only the roof and posts: the meju hang under it
    if state != 'empty':
        dried = state == 'dried'
        m = Canvas(40, 40)
        for i, x in enumerate((9, 16, 23, 29)):
            _meju_block(m, x, 17 + (i % 2) * 2, MEJU_DRY if dried else MEJU_FRESH, dried)
        m.outline()
        for y in range(40):
            for x in range(40):
                p = m.get(x, y)
                if p is not None:
                    c.set(x, y, p)
    return c


# ---------------------------------------------------------------- open lids on the jangdokdae
JARS = [(6, 6, 20, 26), (28, 4, 22, 28), (52, 7, 19, 25), (12, 22, 13, 15), (34, 24, 11, 13), (50, 22, 14, 15), (64, 25, 10, 12)]


def jangdok_open():
    """Same size as kvillage.jangdokdae(): the lids are off and dark sauce shows in each jar mouth."""
    c = Canvas(78, 52)
    for (x, y, w, h) in JARS:
        cx = x + w / 2 + 1
        rx = max(2.5, w * 0.3)
        c.ellipse(cx, y + 2, rx + 0.6, 2.2, [K.ONGGI[0]] * 4, flat=True)   # the dark lip
        c.ellipse(cx, y + 2, rx - 0.4, 1.4, SAUCE, flat=True)              # sauce inside
        c.line(int(cx - rx + 1), y + 1, int(cx + rx - 1), y + 1, K.ONGGI[3])  # rim catching the light
        c.set(int(cx) + 1, y + 2, (255, 236, 190, 255))                     # the sun on the surface
    return c


# ---------------------------------------------------------------- item icons (32 x 32)
def _sack(c, color):
    c.shadow(16, 29, 11, 2)
    c.poly([(7, 28), (8, 12), (24, 12), (25, 28)], color, light_axis='x')
    c.rect(10, 9, 22, 12, color[1]); c.line(9, 11, 23, 11, color[0])


def soybeans():
    c = Canvas(32, 32)
    _sack(c, ramp('#5a4428', '#8a6a40', '#b8935e', '#dcc08a'))
    bean = ramp('#8a6a2a', '#c4a050', '#e8cc7c', '#fff0b4')
    for (x, y) in [(12, 9), (15, 8), (18, 9), (20, 10), (13, 10), (16, 10)]:
        c.ellipse(x, y, 1.6, 1.3, bean)
    for (x, y) in [(9, 27), (23, 28)]:                 # a couple spilled
        c.ellipse(x, y, 1.6, 1.3, bean)
    c.outline()
    return c


def salt():
    c = Canvas(32, 32)
    c.shadow(16, 28, 12, 2)
    c.poly([(5, 18), (27, 18), (23, 27), (9, 27)], P['woodlight'], light_axis='x')       # a wooden bowl
    c.ellipse(16, 17, 11, 6, P['white'])                                                   # a heap of coarse salt
    for (x, y) in [(11, 15), (15, 13), (19, 15), (14, 17), (21, 18), (17, 12)]:
        c.set(x, y, (255, 255, 255, 255)); c.set(x + 1, y + 1, P['white'][1])
    c.outline()
    return c


def meju_icon(dried):
    c = Canvas(32, 32)
    r = MEJU_DRY if dried else MEJU_FRESH
    c.shadow(16, 28, 11, 2)
    c.box(7, 12, 24, 27, r)
    c.rect(8, 12, 24, 13, r[3])
    if dried:
        for (x, y) in [(11, 17), (12, 18), (19, 21), (20, 22), (14, 24)]:
            c.set(x, y, r[0])
        for (x, y) in [(9, 25), (22, 15), (17, 26)]:
            c.set(x, y, (232, 228, 212, 255))           # the white bloom of good meju
        c.line(6, 19, 25, 19, P['straw'][2]); c.line(6, 20, 25, 20, P['straw'][1])   # straw tie
        c.line(15, 6, 15, 18, P['straw'][1]); c.line(16, 6, 16, 18, P['straw'][2])
    else:
        c.set(12, 16, r[1]); c.set(19, 22, r[1]); c.set(15, 20, r[1])      # bean bumps
    c.outline()
    return c


def doenjang():
    c = Canvas(32, 32)
    c.shadow(16, 29, 11, 2)
    j = K.jar(20, 22, lid=False)
    for y in range(j.h):
        for x in range(j.w):
            p = j.get(x, y)
            if p is not None:
                c.set(5 + x, 6 + y, p)
    c.ellipse(16, 8, 6, 1.6, DOENJANG, flat=True)
    c.set(14, 7, DOENJANG[3]); c.set(18, 8, DOENJANG[1])
    c.line(20, 2, 22, 8, P['wood'][2])                 # a wooden spoon in it
    c.outline()
    return c


def ganjang():
    c = Canvas(32, 32)
    c.shadow(16, 29, 11, 2)
    bottle = ramp('#1a0e08', '#2e1a0e', '#4a2c16', '#7a5236')
    c.poly([(10, 28), (10, 14), (13, 10), (19, 10), (22, 14), (22, 28)], bottle, light_axis='x')
    c.rect(14, 4, 18, 10, bottle[1])
    c.rect(13, 3, 19, 5, P['tan'][2]); c.line(13, 3, 19, 3, P['tan'][3])      # a cork-and-paper stopper
    c.rect(12, 17, 20, 23, P['paper'][3])                                     # a hand-written label
    c.line(14, 19, 18, 19, P['wood'][0]); c.line(14, 21, 17, 21, P['wood'][0])
    c.set(12, 13, (255, 255, 255, 140)); c.set(11, 15, (255, 255, 255, 110))
    c.outline()
    return c


def ttukbaegi(c):
    """Earthenware pot (뚝배기) on a straw trivet."""
    c.ellipse(16, 28, 12, 2.4, P['straw'], flat=True)
    c.poly([(4, 15), (28, 15), (26, 26), (6, 26)], K.ONGGI, light_axis='x')
    c.rect(3, 14, 29, 16, K.ONGGI[1]); c.line(3, 14, 29, 14, K.ONGGI[3])


def doenjang_stew():
    c = Canvas(32, 32)
    ttukbaegi(c)
    c.ellipse(16, 15, 11, 3, DOENJANG, flat=True)
    for (x, y) in [(10, 15), (17, 14), (21, 16)]:                    # tofu
        c.rect(x, y, x + 2, y + 1, TOFU[2]); c.set(x, y, TOFU[3])
    for (x, y) in [(13, 16), (23, 14)]:                              # potato
        c.rect(x, y, x + 1, y + 1, P['tan'][3])
    for (x, y) in [(8, 14), (15, 16), (19, 13), (24, 16)]:           # green onion
        c.set(x, y, P['leaf'][3]); c.set(x + 1, y, P['leaf'][2])
    for i, x in enumerate((10, 16, 22)):
        for k in range(3):
            c.set(x + (k % 2), 10 - k * 2 - (i % 2), STEAM[k])
    c.outline()
    return c


def braised_potatoes():
    c = Canvas(32, 32)
    c.ellipse(16, 22, 14, 5, P['white'])
    c.ellipse(16, 21, 10, 3, P['white'], flat=True)
    glaze = ramp('#4a2a10', '#7e4a1e', '#b0783a', '#e4b070')
    for (x, y) in [(11, 18), (16, 16), (21, 18), (14, 21), (19, 21)]:
        c.ellipse(x, y, 3, 2.4, glaze)
        c.set(x - 1, y - 1, glaze[3])
    for (x, y) in [(9, 21), (23, 20), (16, 19)]:                     # sesame
        c.set(x, y, (250, 244, 220, 255))
    c.outline()
    return c


ITEMS = {
    'soybeans': soybeans,
    'salt': salt,
    'fresh_meju': lambda: meju_icon(False),
    'dried_meju': lambda: meju_icon(True),
    'doenjang': doenjang,
    'ganjang': ganjang,
    'doenjang_stew': doenjang_stew,
    'braised_potatoes': braised_potatoes,
}


def world_sprites():
    """name -> canvas, plus a snowy winter variant of each outdoor object."""
    out = {
        'agungi': agungi(False),
        'agungi_fire': agungi(True),
        'meju_rack': meju_rack('empty'),
        'meju_rack_fresh': meju_rack('fresh'),
        'meju_rack_dried': meju_rack('dried'),
        'jangdok_open': jangdok_open(),
    }
    # the agungi sits under the eaves (no snow); the rack's straw roof catches it
    for name, state in (('meju_rack', 'empty'), ('meju_rack_fresh', 'fresh'), ('meju_rack_dried', 'dried')):
        out[name + '_winter'] = meju_rack(state, snow=True)
    return out
