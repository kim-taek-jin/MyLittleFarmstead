"""K-3, neighbours: sitting poses (masil), the four pumasi spots, the village hall.

Sitting poses are made from the standing sprites (front view, sitting on the edge of a pyeong-sang):
the head and body drop a few pixels, a knee band shows the thighs coming toward us, and the shins and
shoes stay where they were. Same 32 x 32 canvas, so they swap in place.
"""
from pixel import Canvas, P, ramp, darken_c, lighten_c
import kvillage as K
import villagers as V

DROP = 3

# hip row (canvas y of the first trouser row) for each standing sprite
HIPS = {'rosa': 23, 'theo': 24, 'mina': 24, 'mayor': 24, 'player': 24}


def sit(src, hip, drop=DROP):
    c = Canvas(src.w, src.h)
    c.shadows = list(src.shadows)
    # shins and shoes stay put
    for y in range(hip + drop, src.h):
        for x in range(src.w):
            p = src.get(x, y)
            if p is not None:
                c.set(x, y, p)
    # head and body drop
    for y in range(0, hip):
        for x in range(src.w):
            p = src.get(x, y)
            if p is not None:
                c.set(x, y + drop, p)
    # knee band: the trouser colour, one pixel wider on each side, lit on top
    row = [(x, src.get(x, hip + 1)) for x in range(src.w) if src.get(x, hip + 1) is not None]
    if row:
        x0, x1 = min(x for x, _ in row) - 1, max(x for x, _ in row) + 1
        mid = row[len(row) // 2][1]
        for x in range(x0, x1 + 1):
            edge = x in (x0, x1)
            c.set(x, hip + drop, darken_c(mid, 0.55) if edge else lighten_c(mid, 0.12))
            c.set(x, hip + drop + 1, darken_c(mid, 0.55) if edge else darken_c(mid, 0.85))
    return c


def villager_sits():
    out = {}
    makers = dict(V.VILLAGERS)
    makers['mayor'] = V.mayor
    for vid, fn in makers.items():
        for i, canvas in enumerate(V.stages(sit(fn(), HIPS[vid]))):
            out[f'{vid}_sit_{i}'] = canvas
    for i, canvas in enumerate(V.stages(V.mayor())):        # the village head is gray at first, like everyone
        out[f'mayor_{i}'] = canvas
    return out


def player_sit():
    import characters as C
    return sit(C.player_frames()['player_down_0'], HIPS['player'])


# ---------------------------------------------------------------- pumasi spots
STEAM = [(255, 255, 255, 170), (255, 255, 255, 120), (255, 255, 255, 80)]
SIRU = ramp('#5a3a22', '#8a5a36', '#b07a4a', '#d4a06a')         # earthenware steamer
POT = P['metal']


def steamer(on=False):
    """A rice-cake steamer (siru) on a big pot over a gas ring on three iron legs."""
    c = Canvas(30, 34)
    c.shadow(15, 32, 12, 2)
    for x in (6, 15, 24):                                    # the iron stand
        c.line(x, 22, x + (0 if x == 15 else (-2 if x < 15 else 2)), 32, P['metal'][0])
    c.rect(5, 21, 25, 22, P['metal'][1])
    if on:
        for x in range(8, 23, 2):
            c.set(x, 20, (90, 150, 255, 255)); c.set(x + 1, 20, (170, 210, 255, 255))
    c.ellipse(15, 16, 10, 5, POT)                            # the pot
    c.poly([(5, 13), (25, 13), (23, 3), (7, 3)], SIRU, light_axis='x')   # the siru, wider at the bottom
    c.rect(4, 12, 26, 13, P['white'][2])                     # the cloth band sealing pot and siru
    c.line(4, 12, 26, 12, P['white'][3])
    c.ellipse(15, 3, 8, 1.6, P['woodlight'], flat=True)     # a wooden lid
    if on:
        for i, x in enumerate((9, 15, 21)):
            for k in range(3):
                c.set(x + (k % 2), 1 - k + (i % 2), STEAM[k])
    c.outline()
    return c


BRICK = ramp('#4a1e14', '#7a3424', '#a8503a', '#cc7a5e')


def forge(on=False):
    """A small outdoor forge: a brick hearth with coals, a box bellows with a handle, an anvil stump."""
    c = Canvas(44, 30)
    c.shadow(22, 28, 20, 2)
    for y in range(12, 28):                                  # brick hearth
        for x in range(14, 36):
            c.set(x, y, BRICK[2] if ((x + (y // 3 % 2) * 3) % 6) else BRICK[1])
    c.rect(14, 12, 35, 13, BRICK[3])
    coal = (40, 34, 32, 255)
    for x in range(16, 34):
        c.set(x, 11, coal); c.set(x, 10, coal if x % 3 else (60, 50, 46, 255))
    if on:
        for x in range(17, 33, 2):
            c.set(x, 10, P['fire'][2]); c.set(x + 1, 11, P['fire'][3])
        for (x, y) in [(20, 8), (25, 6), (29, 8), (23, 4)]:
            c.set(x, y, P['fire'][3])
    c.box(1, 16, 12, 26, P['wood'])                          # the box bellows, its handle up
    c.line(6, 16, 6, 8, P['wood'][3]); c.line(4, 8, 8, 8, P['wood'][2])
    c.line(12, 21, 14, 21, P['metal'][1])                    # pipe into the hearth
    c.box(37, 20, 43, 27, P['wood'])                         # stump with the anvil
    c.rect(36, 17, 43, 19, P['metal'][1]); c.line(36, 17, 43, 17, P['metal'][3])
    c.outline()
    return c


def briquettes(on=False):
    """Coal briquettes (yeontan) stacked on a pallet; carried in, fewer remain. Tongs lean on the stack."""
    c = Canvas(34, 30)
    c.shadow(17, 28, 15, 2)
    c.rect(2, 25, 31, 27, P['wood'][2]); c.line(2, 25, 31, 25, P['wood'][3])
    stacks = [(4, 3), (12, 4), (20, 3)] if not on else [(4, 2), (12, 1)]
    tan = ramp('#141414', '#242424', '#383838', '#585858')
    for (x0, n) in stacks:
        for k in range(n):
            y = 24 - k * 6
            c.rect(x0, y - 5, x0 + 7, y, tan[2])
            c.line(x0, y - 5, x0 + 7, y - 5, tan[3])
            for hx in (x0 + 2, x0 + 5):
                c.set(hx, y - 5, tan[0])
            c.set(x0 + 7, y - 2, tan[1])
    c.line(29, 24, 26, 10, P['metal'][2]); c.line(31, 24, 28, 10, P['metal'][1])   # tongs
    c.outline()
    return c


def sled(on=False):
    """A wooden ice sled (sit-on plank with two iron runners) and two ice picks, on a patch of ice."""
    c = Canvas(38, 22)
    ice = ramp('#6c8ea8', '#9cbcd4', '#c8e0f0', '#eef8ff')
    c.ellipse(19, 17, 18, 4, ice, flat=True)
    for x in range(4, 34, 5):
        c.set(x, 16, ice[3]); c.set(x + 1, 18, ice[1])
    c.box(9, 9, 29, 13, P['woodlight'])                      # plank seat
    c.line(9, 14, 29, 14, P['metal'][1]); c.line(10, 15, 28, 15, P['metal'][2])   # runners
    for (x, lean) in ((5, -1), (33, 1)):                     # ice picks with knob handles
        c.line(x, 15, x + lean * 3, 3, P['wood'][2])
        c.set(x + lean * 3, 2, P['wood'][3]); c.set(x, 16, P['metal'][3])
    if on:
        for (x, y) in [(31, 12), (34, 10), (33, 14), (36, 12)]:
            c.set(x, y, (255, 255, 255, 220))
    c.outline()
    return c


def world_sprites():
    out = {}
    for name, fn in (('steamer', steamer), ('forge', forge), ('briquettes', briquettes), ('sled', sled)):
        out[f'chore_{name}'] = fn(False)
        out[f'chore_{name}_on'] = fn(True)
    return out


# ---------------------------------------------------------------- the village hall (drawn into the market)
def village_hall():
    from kmarket import building
    wall = ramp('#7a6a50', '#b8a684', '#d8caa8', '#f0e6cc')
    roof = ramp('#1e3a26', '#2e5a3a', '#4a8256', '#7ab088')
    hall = building(100, wall, roof, '마을회관', ramp('#1e3a8a', '#2a4aa8', '#3a5cc4', '#6a8ae0'), P['white'][3])
    return hall


def hall_bench():
    c = Canvas(60, 16)
    c.shadow(30, 15, 28, 1.5)
    for x in (4, 54):
        c.box(x, 8, x + 2, 15, P['wood'])
    c.box(1, 5, 58, 9, P['woodlight'])
    for x in range(6, 56, 8):
        c.line(x, 6, x, 8, P['woodlight'][1])
    c.outline()
    return c
