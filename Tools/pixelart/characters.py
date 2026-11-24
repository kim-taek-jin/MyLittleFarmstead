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


def _side_walk(frame):
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
