"""The expanded farm (36 x 20 tiles = four screens), drawn from Assets/Resources/Maps/farm.json.

The old one-screen farm sits in the north-west quarter exactly where it was (same world coordinates),
so the field, cottage, well, pond and gate keep working. The rest is new: the road east to the market,
an overgrown plot to clear (weeds / stumps / boulders are runtime objects drawn on top of plain soil),
a meadow, a south-west field, and two lanes that end in gray thorns until a season of the heart opens them.
"""
import json
import os

import backgrounds as B
from backgrounds import BG, P, SNOW, SUMMER, AUTUMN_GRASS, AUT_ORANGE, AUT_RED, AUT_YELLOW, season_ground, snow_cap
from pixel import Canvas, darken_c, lighten_c, ramp
import crops

ROOT = os.path.join(os.path.dirname(__file__), '..', '..')
MAP_PATH = os.path.join(ROOT, 'Assets', 'Resources', 'Maps', 'farm.json')


def load_map():
    with open(MAP_PATH) as f:
        return json.load(f)


SOIL = set('xgko')


# ----------------------------------------------------------------- debris (runtime objects)
def weeds(variant=0, r=None):
    """A clump of wild grass and weeds, the size of one field cell."""
    r = r or P['leaf2']
    c = Canvas(30, 26)
    c.shadow(15, 23, 12, 2)
    blades = [(-9, 13, -3), (-6, 17, -1), (-3, 20, 0), (0, 22, 1), (3, 18, 2), (6, 16, 3), (9, 12, 4), (-1, 14, -2), (5, 21, 1)]
    if variant == 1:
        blades = [(-10, 11, -4), (-6, 15, -2), (-2, 19, -1), (1, 16, 1), (4, 20, 2), (8, 14, 4), (11, 9, 5), (2, 12, 0)]
    for i, (dx, h, lean) in enumerate(blades):
        x0, y0 = 15 + dx, 23
        x1, y1 = 15 + dx + lean, 23 - h
        c.line(x0, y0, x1, y1, r[1] if i % 2 else r[2])
        c.set(x1, y1, r[3])
        if i % 3 == 0:
            c.set(x0 + 1, y0 - 1, r[1])
    # two seed heads / a little flower
    heads = [(15 + 1, 23 - 22), (15 - 6 - 1, 23 - 17)] if variant == 0 else [(15 + 6, 23 - 20), (15 - 2, 23 - 19)]
    for (x, y) in heads:
        c.set(x, y, P['straw'][2]); c.set(x, y - 1, P['straw'][3]); c.set(x + 1, y, P['straw'][1])
    c.outline()
    return c


def stump():
    c = Canvas(30, 24)
    c.shadow(15, 21, 13, 2.5)
    # roots
    for (x0, x1, y1) in [(6, 2, 21), (24, 28, 21), (10, 7, 22), (20, 23, 22)]:
        c.thick_line(x0, 17, x1, y1, P['wood'][1], 2)
    # trunk side
    c.box(6, 9, 23, 20, P['wood'])
    for x in (9, 13, 18, 21):
        c.line(x, 11, x, 19, P['wood'][1])
    # cut top with rings
    c.ellipse(14.5, 9, 9, 4, P['woodlight'], flat=True)
    c.ellipse(14.5, 9, 6, 2.6, [P['woodlight'][1]] * 4, flat=True)
    c.ellipse(14.5, 9, 4, 1.6, [P['woodlight'][2]] * 4, flat=True)
    c.set(14, 9, P['wood'][1]); c.set(15, 9, P['wood'][1])
    c.line(18, 7, 21, 8, P['wood'][1])          # an old crack
    c.set(7, 13, P['leaf'][2]); c.set(8, 14, P['leaf'][3]); c.set(22, 16, P['leaf'][2])   # moss
    c.outline()
    return c


def boulder():
    c = Canvas(32, 28)
    c.shadow(16, 25, 14, 3)
    c.poly([(2, 24), (4, 14), (10, 6), (19, 4), (27, 9), (30, 17), (29, 24)], P['stone'], light_axis='diag')
    c.poly([(17, 24), (21, 15), (29, 16), (29, 24)], P['stone'], light_axis='diag')
    c.line(12, 8, 15, 15, P['stone'][1]); c.line(15, 15, 13, 21, P['stone'][1])   # crack
    for (x, y) in [(8, 9), (9, 8), (10, 7), (11, 7)]:
        c.set(x, y, P['stone'][3])
    for (x, y) in [(4, 22), (5, 21), (6, 22), (25, 22), (26, 21), (21, 6), (22, 7)]:
        c.set(x, y, P['leaf'][2])
    c.outline()
    return c


def thorns():
    """Gray brambles that close a lane until a season of the heart arrives (no colour = not yet).
    A see-through tangle of thick thorny stems, not a solid bush."""
    g = ramp('#2e2a36', '#4f4a5a', '#7a7486', '#aaa4b4')
    c = Canvas(56, 52)
    c.shadow(28, 49, 25, 3)
    stems = [
        [(3, 48), (8, 34), (16, 24), (25, 20), (33, 23)],
        [(53, 48), (49, 35), (41, 25), (33, 22), (26, 25)],
        [(12, 49), (16, 38), (26, 33), (37, 35), (44, 47)],
        [(20, 48), (21, 36), (17, 27), (19, 15), (26, 9), (32, 10)],
        [(38, 49), (36, 38), (39, 27), (37, 16), (31, 9)],
        [(6, 30), (13, 33), (22, 43)],
        [(50, 29), (44, 32), (35, 43)],
        [(26, 49), (28, 41), (27, 30)],
    ]
    for pts in stems:
        for (a, b) in zip(pts, pts[1:]):
            c.thick_line(a[0], a[1], b[0], b[1], g[1], 2)
    for pts in stems:
        for (a, b) in zip(pts, pts[1:]):
            c.line(a[0], a[1] - 1, b[0], b[1] - 1, g[2])        # lit top edge of each stem
    # thorns: short light spikes along the stems
    k = 0
    for pts in stems:
        for (a, b) in zip(pts, pts[1:]):
            for t in (0.35, 0.75):
                x = int(a[0] + (b[0] - a[0]) * t)
                y = int(a[1] + (b[1] - a[1]) * t)
                d = 1 if k % 2 else -1
                c.set(x + d, y - 2, g[3]); c.set(x + 2 * d, y - 3, g[3])
                k += 1
    # a few dry gray leaves
    for (x, y) in [(10, 27), (30, 18), (45, 31), (18, 40), (40, 41), (24, 13)]:
        c.set(x, y, g[2]); c.set(x + 1, y, g[3]); c.set(x, y + 1, g[1])
    c.outline()
    return c


def icon_tree():
    c = Canvas(32, 32)
    c.poly([(6, 26), (16, 3), (26, 26)], P['leaf'], light_axis='x')
    c.box(14, 25, 18, 31, P['wood'])
    return c


def icon_sun():
    """A sun setting behind a hill (the sunset hill path)."""
    c = Canvas(32, 32)
    c.ellipse(16, 18, 9, 9, P['sun'])
    c.poly([(0, 31), (6, 20), (16, 16), (26, 21), (31, 31)], P['leaf'], light_axis='y')
    return c


def icon_water():
    """A water drop over ripples (the lake path)."""
    c = Canvas(32, 32)
    c.poly([(16, 2), (24, 16), (8, 16)], P['water'], light_axis='x')
    c.ellipse(16, 18, 8, 8, P['water'])
    c.line(4, 29, 12, 29, P['water'][2]); c.line(20, 29, 28, 29, P['water'][2])
    return c


DEBRIS = {
    'weeds_0': lambda: weeds(0),
    'weeds_1': lambda: weeds(1),
    'stump': stump,
    'boulder': boulder,
    'thorns': thorns,
}
SEASON_WEEDS = {'summer': SUMMER, 'autumn': AUTUMN_GRASS, 'winter': ramp('#4e4a34', '#7a7454', '#a49c74', '#cfc8a2')}


def debris_variants():
    """Seasonal looks of the debris (spring is the default sprite)."""
    out = {}
    for season, r in SEASON_WEEDS.items():
        for v in (0, 1):
            w = weeds(v, r)
            out[f'weeds_{v}_{season}'] = snow_cap(w) if season == 'winter' else w
    out['stump_winter'] = snow_cap(stump())
    out['boulder_winter'] = snow_cap(boulder())
    out['thorns_winter'] = snow_cap(thorns())
    return out


# ----------------------------------------------------------------- the big background
def farmmap(season='spring'):
    m = load_map()
    W, H, OX, OY = m['width'], m['height'], m['originX'], m['originY']
    bg = BG(W * 32, H * 32, W, H, (OX + W / 2, OY - H / 2))
    rows = m['rows']

    lanes = [
        (0.35, OY - H - 0.2, 1.55, -5.2),        # south lane to the old railway
        (-9.2, -9.8, 1.0, -8.45),                # west lane to the forest
    ]
    B.farm(season, bg=bg, east=OX + W + 0.2, extra_paths=lanes)
    ground = season_ground(season)

    # a few calm patches of the second grass colour in the new areas
    patch_col = {'winter': SNOW[1], 'spring': P['leaf2'][2], 'summer': SUMMER[1], 'autumn': AUT_ORANGE[1]}[season]
    patch_tuft = {'winter': SNOW, 'spring': P['leaf2'], 'summer': SUMMER, 'autumn': AUT_YELLOW}[season]
    for (wx, wy, rx, ry) in [(14.0, 2.5, 60, 20), (22.5, -0.6, 70, 18), (-5.0, -12.5, 60, 22), (24.5, -13.6, 46, 14), (8.5, -14.0, 40, 12)]:
        x, y = bg.px(wx, wy)
        B.patch(bg, x, y, rx, ry, patch_col, ground, patch_tuft)

    # the overgrown plot: plain soil (the dirt tile itself) with a dug rim; debris is drawn by the game on top
    dirt = crops.dirt_tile()
    soil = set()
    for r, row in enumerate(rows):
        for c, ch in enumerate(row):
            if ch in SOIL:
                for y in range(r * 32, r * 32 + 32):
                    for x in range(c * 32, c * 32 + 32):
                        bg.c.set(x, y, dirt.get(x % 32, y % 32))
                        soil.add((x, y))
    B.soil_rim(bg, soil, season)

    # broken fence posts at the plot's corners (it was a field once)
    for (x0, x1, wy) in [(3.9, 6.2, -5.95), (19.4, 21.9, -5.95)]:
        a, b = bg.px(x0, wy), bg.px(x1, wy)
        f = B.fence_h(b[0] - a[0])
        for x in range(f.w - 7, f.w - 1):        # one rail has fallen off
            for y in range(7, 11):
                f.set(x, y, None)
        bg.prop(f, x=(a[0] + b[0]) // 2, y=a[1])

    # hedges framing the map (gaps where the road, the lanes and the gate go through)
    def ramp_for(i):
        if season == 'winter':
            return P['leaf2']
        if season == 'summer':
            return SUMMER
        if season == 'autumn':
            return [AUT_ORANGE, AUT_RED, AUT_YELLOW, P['leaf2']][i % 4]
        return P['leaf2'] if i % 3 == 1 else P['leaf']
    east_x = OX + W - 0.35
    y = OY - 0.9
    i = 0
    while y > OY - H + 0.2:
        if not (-5.6 < y < -3.6) and not (-12.5 < y < -9.6):       # the road to the market, the lane to the lake
            bg.prop(B.bush(30, 22, ramp_for(i)), wx=east_x + (0.12 if i % 2 else 0), wy=y)
        y -= 0.62; i += 1
    south_y = OY - H + 0.3
    x = OX + 0.4
    while x < OX + W:
        if not (-0.4 < x < 2.3):                                   # the south lane
            bg.prop(B.bush(32, 22, ramp_for(i)), wx=x, wy=south_y + (0.1 if i % 2 else 0))
        x += 0.95; i += 1
    west_x = OX + 0.3
    y = -5.9
    while y > OY - H + 0.6:
        if not (-10.4 < y < -7.9):                                 # the forest lane
            bg.prop(B.bush(28, 22, ramp_for(i)), wx=west_x - (0.1 if i % 2 else 0), wy=y)
        y -= 0.62; i += 1

    # signposts
    icons = {'Market': B.coin_bag, 'Forest': icon_tree, 'Sunset Hill': icon_sun, 'Lake': icon_water}
    for s in m['signs']:
        bg.prop(B.signpost(icons[s['text']]), wx=s['x'], wy=s['y'])
    bg.prop(B.log_pile(), wx=13.2, wy=-4.0 + 0.95)

    # hand-placed seasonal details on the map's flower cells
    k = 0
    for r, row in enumerate(rows):
        for c, ch in enumerate(row):
            if ch != '*':
                continue
            x, y = bg.px(OX + c + 0.5, OY - r - 0.5)
            k += 1
            if season == 'winter':
                for dx, dy in ((0, 0), (2, 1), (-1, 2)):
                    bg.c.set(x + dx, y + dy, P['red'][2]); bg.c.set(x + dx, y + dy + 1, P['leaf2'][1])
            elif season == 'autumn':
                B.leaf_pile(bg, x, y, 7)
            else:
                cols = [P['jam'], P['white'], P['sun'], P['cloth'], P['red']]
                B.flower_cluster(bg, x, y, cols[k % len(cols)], 5 if season == 'summer' else 4)
    if season == 'autumn':
        bg.prop(B.pumpkin_deco(), wx=14.2, wy=-3.1)
    if season == 'winter':
        bg.prop(B.snowman(), wx=23.6, wy=-2.2)
    return bg.image()
