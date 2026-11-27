"""W-1, drinking water: the village water tap by the hall and the barley-tea icons.

The tap is drawn into the market background (kmarket.py) at world (4.6, -1.0), left of the village hall.
In winter an old cloth is wrapped round the pipe (how country houses keep the tap from freezing) and a
small icicle hangs from the spout.
Icons are 32 x 32 like the other items (Items/<snake name>).
"""
from pixel import Canvas, P, ramp

CONCRETE = ramp('#4a4a4c', '#76767a', '#9e9e9e', '#c4c2bc')
BRASS = ramp('#4a3410', '#8a6420', '#c49a3a', '#f0d27a')
RUBBER = ramp('#5a0c10', '#9a1a1e', '#c8302e', '#f0705e')      # the red rubber tub (고무 대야)
ALU = ramp('#4a5058', '#7c8490', '#aeb6c0', '#e2e8ee')          # aluminium bucket
TEA = ramp('#3a1c08', '#6a3612', '#9a5a22', '#c88a4a')          # barley tea
RAG = ramp('#3a2a40', '#5e4a68', '#8a7290', '#b8a2bc')          # a faded old cloth
PET = (210, 236, 246, 150)                                      # a clear plastic bottle


def water_tap(winter=False):
    """A concrete standpipe with a brass tap over a low concrete sink, a red rubber tub and a bucket."""
    c = Canvas(48, 40)
    c.shadow(24, 38, 22, 2)
    # the low concrete sink (개수대): a rim and a darker inside, a drain line in front
    c.box(6, 27, 34, 37, CONCRETE)
    c.rect(9, 29, 31, 32, CONCRETE[1])
    c.rect(10, 30, 30, 31, P['water'][1] if not winter else (188, 204, 220, 255))   # a little water (ice in winter)
    c.set(12, 30, P['water'][3] if not winter else (240, 248, 255, 255))
    for x in range(8, 33, 6):
        c.set(x, 35, CONCRETE[1])                                    # chipped front edge
    # the standpipe (a gray pipe on the back of the sink) and the tap
    c.rect(18, 6, 21, 29, CONCRETE[2]); c.rect(18, 6, 18, 29, CONCRETE[3]); c.rect(21, 6, 21, 29, CONCRETE[1])
    c.rect(17, 5, 22, 6, CONCRETE[1])
    c.rect(22, 11, 27, 13, BRASS[2]); c.line(22, 11, 27, 11, BRASS[3])  # spout
    c.rect(26, 13, 27, 15, BRASS[1])
    c.rect(23, 8, 25, 9, BRASS[2]); c.set(24, 7, BRASS[3])               # handle
    if winter:
        # an old cloth wrapped round the pipe, tied with twine, and an icicle
        c.rect(17, 14, 22, 24, RAG[2])
        for y in range(14, 25, 3):
            c.line(17, y, 22, y, RAG[1])
        c.rect(17, 14, 17, 24, RAG[3])
        c.line(17, 19, 22, 19, P['straw'][1])
        c.set(26, 16, (220, 236, 250, 255)); c.set(26, 17, (200, 222, 244, 255)); c.set(27, 16, (240, 248, 255, 255))
    else:
        c.set(26, 17, P['water'][3]); c.set(26, 20, P['water'][2])       # a drip
    # the red rubber tub on the ground to the right: a wide round tub seen from a little above
    c.poly([(34, 30), (47, 30), (45, 37), (36, 37)], RUBBER, light_axis='x')
    c.ellipse(40.5, 30, 6.8, 2.6, [RUBBER[3]] * 4, flat=True)                     # rim
    inner = [P['water'][1]] * 4 if not winter else [(200, 214, 228, 255)] * 4      # water (ice in winter)
    c.ellipse(40.5, 30.3, 5.2, 1.6, inner, flat=True)
    c.set(38, 30, P['water'][3] if not winter else (244, 250, 255, 255))
    # an aluminium bucket to the left
    c.poly([(0, 26), (7, 26), (6, 36), (1, 36)], ALU, light_axis='x')
    c.rect(0, 26, 7, 26, ALU[3])
    c.line(1, 25, 6, 23, ALU[1])
    c.outline()
    c.nosnow = False
    return c


# ---------------------------------------------------------------- item icons
def barley_tea():
    """Bottled barley tea from the corner store: a small clear bottle, tea brown, a paper label, a green cap."""
    c = Canvas(32, 32)
    c.shadow(16, 29, 8, 2)
    c.poly([(11, 28), (11, 13), (13, 10), (19, 10), (21, 13), (21, 28)], TEA, light_axis='x')
    c.rect(14, 6, 18, 10, PET)
    c.rect(13, 4, 19, 6, P['leaf'][1]); c.line(13, 4, 19, 4, P['leaf'][2])
    c.rect(11, 17, 21, 23, P['paper'][3])                       # label: a barley ear
    c.rect(11, 17, 21, 17, P['straw'][2]); c.rect(11, 23, 21, 23, P['straw'][2])
    for y in (19, 20, 21):
        c.set(15, y, P['straw'][1]); c.set(17, y, P['straw'][1])
    c.set(16, 18, P['straw'][1]); c.line(16, 19, 16, 22, P['leaf'][1])
    c.set(12, 14, (255, 255, 255, 170)); c.set(12, 25, (255, 255, 255, 120))
    c.outline()
    return c


def homemade_barley_tea():
    """Barley tea boiled at home and kept in an old ridged glass juice bottle with a plastic cap."""
    c = Canvas(32, 32)
    c.shadow(16, 29, 10, 2)
    c.poly([(9, 28), (9, 14), (12, 10), (20, 10), (23, 14), (23, 28)], TEA, light_axis='x')
    for y in range(15, 28, 3):                                   # the ridges of the glass
        c.line(10, y, 22, y, TEA[3])
    c.rect(13, 6, 19, 10, (196, 214, 210, 150))
    c.rect(12, 3, 20, 6, P['cloth'][2]); c.line(12, 3, 20, 3, P['cloth'][3])
    c.set(10, 16, (255, 255, 255, 170)); c.set(10, 17, (255, 255, 255, 150)); c.set(10, 22, (255, 255, 255, 110))
    c.outline()
    return c


def roasted_barley():
    """Roasted barley in a folded paper bag, a few grains spilled."""
    c = Canvas(32, 32)
    c.shadow(16, 29, 12, 2)
    c.poly([(7, 28), (8, 12), (24, 12), (25, 28)], P['paper'], light_axis='x')
    c.line(8, 12, 24, 12, P['paper'][1])
    c.ellipse(16, 12, 7.5, 2.5, TEA, flat=True)                  # grains at the open top
    for (x, y) in [(11, 11), (14, 12), (17, 11), (20, 12), (13, 13), (19, 13)]:
        c.set(x, y, TEA[3])
    c.rect(11, 18, 21, 22, P['white'][2])                        # a hand-written tag
    c.line(13, 20, 19, 20, P['wood'][0])
    for (x, y) in [(4, 28), (27, 27), (26, 29)]:
        c.set(x, y, TEA[2]); c.set(x + 1, y, TEA[1])
    c.outline()
    return c


def all_icons():
    return {'barley_tea': barley_tea(), 'homemade_barley_tea': homemade_barley_tea(), 'roasted_barley': roasted_barley()}
