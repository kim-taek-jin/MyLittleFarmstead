"""Player farmer, hand pixel-mapped (same method and outline as the merchant).

Frames: down / up / side x (idle, step A, step B). Left-facing uses the side frames flipped.
"""
from pixel import Canvas, P
from haggle import PixelMap, OUTLINE

PAL = {
    'y': P['straw'][2], 'Y': P['straw'][1], 'h': P['straw'][3], 'p': P['shirt'][1],
    's': P['skin'][2], 'S': P['skin'][1], 'l': P['skin'][3], 'r': (232, 140, 120, 255),
    'H': P['hair'][1], 'J': P['hair'][2],
    'E': OUTLINE, 'o': (120, 52, 40, 255),
    't': P['shirt'][2], 'T': P['shirt'][1], 'u': P['shirt'][3],
    'a': P['denim'][2], 'A': P['denim'][1], 'd': P['denim'][3],
    'B': P['sun'][2], 'b': P['wood'][1], 'k': P['wood'][0],
}
SHADE = {'s': ('S', 'l'), 't': ('T', 'u'), 'a': ('A', 'd'), 'y': ('Y', 'h')}
W, H = 20, 29


def _front(back=False):
    m = PixelMap(W, H)
    # straw hat
    m.seg(0, 7, 9, 'y'); m.seg(1, 6, 9, 'y'); m.seg(2, 6, 9, 'y')
    m.seg(3, 6, 9, 'p')
    m.seg(4, 2, 9, 'y'); m.seg(5, 1, 9, 'Y')
    # head
    for y in range(6, 13):
        m.seg(y, 5, 9, 's')
    m.seg(13, 7, 9, 's')
    if back:
        for y in range(6, 13):
            m.seg(y, 5, 9, 'H')
        m.seg(12, 6, 9, 'J')
    else:
        m.seg(6, 5, 9, 'H')
        m.seg(7, 5, 6, 'H')
        m.seg(9, 7, 7, 'E'); m.seg(10, 7, 7, 'E')
        m.seg(11, 5, 6, 'r')
        m.seg(12, 9, 9, 'o')
    m.seg(14, 8, 9, 'S')
    # shirt + arms
    m.seg(14, 5, 7, 't')
    for y in range(15, 22):
        m.seg(y, 3, 9, 't')
    for y in range(16, 22):
        m.seg(y, 2, 2, 't')
    m.seg(22, 2, 3, 's')
    for y in range(16, 22):
        m.seg(y, 3, 3, 'T')            # arm / body separation
    # overalls
    for y in range(18, 23):
        m.seg(y, 5, 9, 'a')
    m.seg(17, 6, 9, 'a')
    if back:
        m.put(6, 15, 'a'); m.put(7, 16, 'a'); m.put(12, 15, 'a'); m.put(13, 16, 'a')
    else:
        m.seg(15, 6, 6, 'a'); m.seg(16, 6, 6, 'a')
        m.seg(17, 6, 6, 'B')
    # legs + boots
    for y in range(23, 27):
        m.seg(y, 5, 8, 'a')
    m.seg(27, 4, 8, 'b'); m.seg(28, 4, 8, 'k')
    m.shade(SHADE)
    m.put(8, 1, 'h'); m.put(7, 1, 'h')       # hat highlight on the left
    if not back:
        m.put(6, 8, 'l')
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
            m.g[22][x] = 't'
    return m


def _side_walk(frame):
    """Profile facing right. frame 0 idle, 1/2 legs scissored (and arm swinging) in opposite directions."""
    m = PixelMap(W, H)
    put = lambda y, x0, x1, ch: m.seg(y, x0, x1, ch, mirror=False)
    # hat: brim reaches further forward
    put(0, 7, 11, 'y'); put(1, 6, 12, 'y'); put(2, 6, 12, 'y')
    put(3, 6, 12, 'p')
    put(4, 3, 16, 'y'); put(5, 3, 16, 'Y')
    # head in profile
    for y in range(6, 13):
        put(y, 6, 14, 's')
    put(13, 8, 13, 's')
    m.put(15, 9, 's'); m.put(15, 10, 'S')            # nose
    for y in range(6, 12):
        put(y, 6, 8, 'H')                            # hair at the back
    put(6, 9, 13, 'H')
    m.put(9, 9, 'S'); m.put(9, 10, 'S')              # ear
    m.put(13, 8, 'E'); m.put(13, 9, 'E')
    m.put(12, 11, 'r')
    m.put(14, 12, 'o')
    put(14, 9, 11, 'S')
    # body
    for y in range(15, 23):
        put(y, 6, 13, 't')
    for y in range(18, 23):
        put(y, 7, 13, 'a')
    put(16, 11, 13, 'a'); put(17, 11, 13, 'a')
    m.put(11, 16, 'B')
    # legs
    if frame == 0:
        legs = [(8, 12)]
    elif frame == 1:
        legs = [(6, 8), (11, 13)]
    else:
        legs = [(7, 9), (10, 12)]
    for (x0, x1) in legs:
        for y in range(23, 27):
            put(y, x0, x1, 'a')
        put(27, x0, x1 + 1, 'b'); put(28, x0, x1 + 1, 'k')
    if frame == 2:                                  # back foot lifted
        put(28, 7, 10, '.'); put(27, 7, 10, 'b'); put(26, 7, 9, 'a')
    m.shade(SHADE)
    # arm in front (after shading so it keeps its own dark edge)
    swing = {0: 0, 1: 1, 2: -1}[frame]
    for y in range(16, 22):
        put(y, 9 + swing, 10 + swing, 'T')
        m.put(11 + swing, y, 't')
    put(22, 9 + swing, 11 + swing, 's')
    m.put(8, 1, 'h')
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
