"""The player: a city person come home to the country (direction C), hand pixel-mapped (same method and outline as the merchant).

Frames: down / up / side x (idle, step A, step B). Left-facing uses the side frames flipped.
"""
from pixel import Canvas, P
from haggle import PixelMap, OUTLINE

CARDIGAN = [(78, 62, 44, 255), (146, 120, 86, 255), (196, 170, 128, 255), (232, 214, 176, 255)]   # oatmeal knit
PAL = {
    's': P['skin'][2], 'S': P['skin'][1], 'l': P['skin'][3], 'r': (232, 140, 120, 255),
    'H': P['hair'][1], 'J': P['hair'][2], 'K': P['hair'][0],
    'E': OUTLINE, 'o': (120, 52, 40, 255),
    'c': CARDIGAN[2], 'C': CARDIGAN[1], 'q': CARDIGAN[3],
    'w': P['white'][2], 'v': P['white'][1],
    'a': P['denim'][2], 'A': P['denim'][1], 'd': P['denim'][3],
    'n': P['white'][3], 'N': P['white'][1], 'k': P['stone'][1],
}
# shading groups: base -> (shadow, light)
SHADE = {'s': ('S', 'l'), 'c': ('C', 'q'), 'a': ('A', 'd'), 'H': ('K', 'J'), 'n': ('N', None)}
W, H = 20, 29


def _front(back=False):
    """A city person come home to the country: short dark hair, an open oatmeal cardigan over a white tee,
    jeans and white sneakers. (No straw hat, no overalls.)"""
    m = PixelMap(W, H)
    # hair: rounded top, falls to the jaw at the sides
    m.seg(1, 7, 9, 'H'); m.seg(2, 5, 9, 'H')
    for y in range(3, 7):
        m.seg(y, 4, 9, 'H')
    # head
    for y in range(7, 13):
        m.seg(y, 5, 9, 's')
    m.seg(13, 7, 9, 's')
    for y in range(7, 11):
        m.seg(y, 4, 4, 'H')                       # side hair down to the cheek
    if back:
        for y in range(6, 13):
            m.seg(y, 5, 9, 'H')
        m.seg(13, 7, 9, 'H')
    else:
        m.seg(7, 6, 7, 'H')                       # fringe, parted
        m.put(9, 7, 'H')
        m.seg(9, 7, 7, 'E'); m.seg(10, 7, 7, 'E')
        m.seg(11, 5, 6, 'r')
        m.seg(12, 9, 9, 'o')
    m.seg(14, 8, 9, 'S')                          # neck shadow
    # cardigan (sleeves to the wrist) over a white tee
    m.seg(14, 5, 7, 'c')
    for y in range(15, 23):
        m.seg(y, 3, 9, 'c')
    for y in range(16, 22):
        m.seg(y, 2, 2, 'c')
    m.seg(22, 2, 3, 's')
    for y in range(16, 22):
        m.seg(y, 3, 3, 'C')                       # arm / body separation
    if not back:
        for y in range(15, 21):
            m.seg(y, 8, 9, 'w')                   # the open front shows the tee
        m.seg(20, 8, 9, 'v')
    # jeans and sneakers
    for y in range(22, 27):
        m.seg(y, 5, 8, 'a')
    m.seg(22, 4, 9, 'c')                          # cardigan hem over the hips
    m.seg(27, 4, 8, 'n'); m.seg(28, 4, 8, 'k')
    m.shade(SHADE)
    if not back:
        m.put(6, 8, 'l')
        m.put(9, 3, 'J'); m.put(10, 3, 'J')       # shine on the hair
    return m


def _walk(m, frame, side=False):
    """Lift one foot and swing the opposite arm by moving whole pixel rows."""
    if frame == 0:
        return m
    lift_left = frame == 1
    xs = range(0, W // 2) if lift_left else range(W // 2, W)
    # lift a leg: shift its boot up by one row
    for x in xs:
        for y in range(23, H):
            m.g[y - 1][x] = m.g[y][x] if y < H else '.'
        m.g[H - 1][x] = '.'
    # swing the opposite hand down by one pixel
    hand_xs = range(W // 2, W) if lift_left else range(0, W // 2)
    for x in hand_xs:
        if m.g[22][x] in ('s', 'S', 'l'):
            m.g[23][x] = m.g[22][x]
            m.g[22][x] = 'c'
    return m


def _side_walk(frame, arm=True):
    """Profile facing right. frame 0 idle, 1/2 legs scissored (and arm swinging) in opposite directions."""
    m = PixelMap(W, H)
    put = lambda y, x0, x1, ch: m.seg(y, x0, x1, ch, mirror=False)
    # hair
    put(1, 8, 12, 'H'); put(2, 6, 13, 'H')
    for y in range(3, 7):
        put(y, 5, 14, 'H')
    # head in profile
    for y in range(7, 13):
        put(y, 6, 14, 's')
    put(13, 8, 13, 's')
    m.put(15, 9, 's'); m.put(15, 10, 'S')            # nose
    for y in range(7, 12):
        put(y, 5, 8, 'H')                            # hair at the back, to the nape
    put(7, 12, 14, 'H')                              # fringe
    m.put(9, 9, 'S'); m.put(9, 10, 'S')              # ear
    m.put(13, 9, 'E'); m.put(13, 10, 'E')
    m.put(12, 11, 'r')
    m.put(14, 12, 'o')
    put(14, 9, 11, 'S')
    # cardigan, the tee showing at the front
    for y in range(15, 23):
        put(y, 6, 13, 'c')
    for y in range(15, 21):
        put(y, 13, 13, 'w')
    # legs (jeans) and sneakers
    if frame == 0:
        legs = [(8, 12)]
    elif frame == 1:
        legs = [(6, 8), (11, 13)]
    else:
        legs = [(7, 9), (10, 12)]
    for (x0, x1) in legs:
        for y in range(23, 27):
            put(y, x0, x1, 'a')
        put(27, x0, x1 + 1, 'n'); put(28, x0, x1 + 1, 'k')
    if frame == 2:                                  # back foot lifted
        put(28, 7, 10, '.'); put(27, 7, 10, 'n'); put(26, 7, 9, 'a')
    m.shade(SHADE)
    if not arm:                                     # action poses draw their own arm
        m.put(7, 2, 'J')
        return m
    # arm in front (after shading so it keeps its own dark edge)
    swing = {0: 0, 1: 1, 2: -1}[frame]
    for y in range(16, 22):
        put(y, 9 + swing, 10 + swing, 'C')
        m.put(11 + swing, y, 'c')
    put(22, 9 + swing, 11 + swing, 's')
    m.put(7, 2, 'J')
    return m


def _frame(pm):
    body = pm.canvas(PAL)
    c = Canvas(32, 32)
    c.shadow(16, 30, 7, 1.5, alpha=60)
    for y in range(body.h):
        for x in range(body.w):
            p = body.get(x, y)
            if p is not None:
                c.set(x + 5, y + 1, p)
    return c


def player_frames():
    out = {}
    for f in range(3):
        out[f'player_down_{f}'] = _frame(_walk(_front(), f))
        out[f'player_up_{f}'] = _frame(_walk(_front(back=True), f))
        out[f'player_side_{f}'] = _frame(_side_walk(f))
    return out


# ---------------------------------------------------------------- action poses (D-2): hoe, watering can, harvest
# Built on the idle frames: the hand that holds the tool is taken off the body and drawn again on the tool,
# so a pose never shows three hands. The tool goes in front of the body (down, side) or behind it (up).
HANDLE = P['woodlight']
BLADE = P['metal']
CAN = P['cloth']                                    # a blue tin can, like the hotbar icon
SKIN = P['skin']


def _no_hand(m, right=True, left=False):
    """Front/back map: take a hand off (row 22) so the pose can draw it where the tool is."""
    cols = []
    if right:
        cols += [16, 17]
    if left:
        cols += [2, 3]
    for x in cols:
        if m.g[22][x] in 'sSl':
            m.g[22][x] = '.'
    return m


def _compose(body, tool, front=True):
    c = Canvas(32, 32)
    c.shadows = list(body.shadows)
    for layer in ((body, tool) if front else (tool, body)):
        for y in range(32):
            for x in range(32):
                p = layer.get(x, y)
                if p is not None:
                    c.set(x, y, p)
    return c


def _hand(c, x, y):
    c.rect(x, y, x + 1, y + 1, SKIN[2])
    c.set(x, y, SKIN[3])


def _sleeve(c, x0, y0, x1, y1):
    """An arm in the knit sleeve. Drawn after the tool's outline, so it sits on the body like the idle arm
    (light on top, a darker line under it) instead of getting its own outline."""
    c.thick_line(x0, y0 + 1, x1, y1 + 1, CARDIGAN[1], 1)
    c.thick_line(x0, y0, x1, y1, CARDIGAN[2], 1)


def _hoe(c, gx, gy, tx, ty, blade_dx, blade_dy):
    """Handle from the grip (gx, gy) to the top end (tx, ty), and a blade bent off the top end."""
    c.thick_line(gx, gy, tx, ty, HANDLE[2], 2)
    c.line(gx, gy, tx, ty, HANDLE[3])
    bx, by = tx + blade_dx, ty + blade_dy
    c.thick_line(tx, ty, bx, by, BLADE[2], 3)
    c.set(bx, by, BLADE[3])


def _can(c, x, y, tilt):
    """A small watering can whose body's top-left is (x, y); tilt 1 pours forward (to the right)."""
    c.box(x, y + tilt, x + 6, y + 5 + tilt, CAN)
    c.rect(x + 1, y - 1 + tilt, x + 4, y - 1 + tilt, CAN[1])           # handle on top
    if tilt:
        c.thick_line(x + 6, y + 3, x + 9, y + 6, CAN[1], 1)               # spout down
    else:
        c.thick_line(x + 6, y + 3, x + 9, y + 1, CAN[1], 1)               # spout up


def _drops(c, x, y, n=3):
    for k in range(n):
        c.set(x + (k % 2), y + k * 2, P['water'][3])
        c.set(x + (k % 2), y + k * 2 + 1, P['water'][2])


def _crouch(c, drop=2):
    """Bend down: move everything above the hips down by `drop` rows (the legs look bent)."""
    out = Canvas(32, 32)
    out.shadows = list(c.shadows)
    for y in range(32):
        for x in range(32):
            p = c.get(x, y)
            if p is None:
                continue
            ny = y + drop if y < 24 else y
            if ny < 32 and (y < 24 or out.get(x, ny) is None):
                out.set(x, ny, p)
    return out


def action_frames():
    out = {}
    down = lambda: _frame(_no_hand(_front()))
    up = lambda: _frame(_no_hand(_front(back=True)))
    side = lambda: _frame(_side_walk(0, arm=False))

    # ---- hoe: 0 raised, 1 striking the ground
    t = Canvas(32, 32); _hoe(t, 22, 17, 25, 4, -3, 2); _hand(t, 21, 16); t.outline()
    out['player_hoe_down_0'] = _compose(down(), t)
    t = Canvas(32, 32); _hoe(t, 21, 20, 25, 27, -3, 3); _hand(t, 20, 20); t.outline()
    out['player_hoe_down_1'] = _compose(down(), t)
    t = Canvas(32, 32); _hoe(t, 21, 16, 24, 3, -3, 1); _hand(t, 20, 15); t.outline()
    out['player_hoe_up_0'] = _compose(up(), t, front=False)
    t = Canvas(32, 32); _hoe(t, 20, 12, 17, 2, -3, 0); t.outline()       # the blade bites the ground ahead (up the screen)
    out['player_hoe_up_1'] = _compose(up(), t, front=False)
    t = Canvas(32, 32); _hoe(t, 13, 13, 7, 3, -2, 3); _hand(t, 12, 12); t.outline(); _sleeve(t, 14, 17, 13, 14)
    out['player_hoe_side_0'] = _compose(side(), t)
    t = Canvas(32, 32); _hoe(t, 19, 20, 27, 25, 1, 4); _hand(t, 18, 19); t.outline(); _sleeve(t, 14, 17, 17, 19)
    out['player_hoe_side_1'] = _compose(side(), t)

    # ---- watering can: 0 held, 1 pouring
    for f in (0, 1):
        t = Canvas(32, 32); _can(t, 19, 21, f); _hand(t, 20, 19); t.outline()
        if f:
            _drops(t, 28, 26)
        out[f'player_water_down_{f}'] = _compose(down(), t)
        t = Canvas(32, 32); _can(t, 19, 17, f); t.outline()
        if f:
            _drops(t, 27, 10)
        out[f'player_water_up_{f}'] = _compose(up(), t, front=False)
        t = Canvas(32, 32); _can(t, 17, 19, f); _hand(t, 17, 18); t.outline(); _sleeve(t, 14, 17, 16, 18)
        if f:
            _drops(t, 27, 25)
        out[f'player_water_side_{f}'] = _compose(side(), t)

    # ---- harvest: 0 bent down reaching, 1 pulling up (a green top in hand)
    for f in (0, 1):
        for d, base in (('down', down), ('up', up), ('side', side)):
            body = _crouch(base(), 2 if f == 0 else 1)
            t = Canvas(32, 32)
            if d == 'side':
                hx, hy = (20, 25) if f == 0 else (19, 21)
            else:
                hx, hy = (15, 26) if f == 0 else (15, 21)
            _hand(t, hx, hy)
            if f:
                t.rect(hx, hy - 3, hx + 1, hy - 1, P['leaf'][2]); t.set(hx - 1, hy - 3, P['leaf'][3])
            t.outline()
            if d == 'side':
                _sleeve(t, 14, 19, hx - 1, hy)
            out[f'player_harvest_{d}_{f}'] = _compose(body, t, front=(d != 'up'))
    return out
