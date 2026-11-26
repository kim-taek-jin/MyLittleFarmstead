"""H-1: the inside of the house as a Korean ondol room.

Drawn to the player's scale: the player sprite is 32 px tall (about 1.6 m), so 1 m is about 20 px.
A yo (mattress) is ~1 x 2 m, the wardrobe 1.3 m wide, the low table 0.9 m across, the door ~1.2 m.
The picture is 288 x 162 px = 9 x 5.06 world units (16:9), centred at (0, 0) and placed at its native
pixel size (HomeRoom.PlaceRoom), so the camera zooms in to it and the player reads at a proper size.

    back wall   : earth plaster between wooden posts, a paper lattice window, a calendar, a wall clock
    floor       : oiled-paper floor (jangpan) in sheets; the warm end (araetmok) on the left, browned
    left        : bedding on the araetmok (yo, quilt folded back, a pillow with embroidered round ends)
    back left   : a black lacquer wardrobe with mother-of-pearl inlay (jagaejang)
    back right  : a low desk with a radio and the letters from the city; quilts stacked on a chest
    middle      : a round low table (soban) with a meal, two floor cushions
    front right : a sliding paper door (the way out; kept clear of the hotbar in the middle of the screen)
"""
from PIL import Image
from pixel import Canvas, P, ramp, darken_c, lighten_c, seeded
from backgrounds import _h

W, H = 288, 162
WALL_Y = 38          # the back wall meets the floor
FRONT_Y = 144        # the floor meets the front wall
DOOR = (220, 244)    # sliding door in the front wall (x range)

POST = ramp('#3a2416', '#5a3a22', '#7a5232', '#9c6e46')
PLASTER = ramp('#a8946c', '#c8b48a', '#e0d0a8', '#f0e4c4')
HANJI = ramp('#c8bc9c', '#e4dac0', '#f4eedc', '#fffaee')
JANGPAN = ramp('#8a5a22', '#b47a34', '#d29a4c', '#e8b866')
LACQUER = ramp('#0c0a0c', '#1a161a', '#2a2428', '#403840')
NACRE = [(236, 230, 240, 255), (198, 226, 222, 255), (240, 206, 214, 255), (214, 222, 246, 255)]
SILK_R = ramp('#5a1018', '#8e1c26', '#c03440', '#e06a70')
SILK_G = ramp('#18402e', '#24604a', '#3a8a68', '#6ab694')
SILK_Y = ramp('#7a5a10', '#b88a1e', '#e0b844', '#f6dc86')
COTTON = ramp('#b8aca0', '#dcd2c6', '#f0e8de', '#fffaf4')
WOOD = ramp('#3a2010', '#5c3418', '#7e4c26', '#a26a3a')


def _post(c, x0, y0, x1, y1):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            k = 3 if x == x0 else 1 if x == x1 else 2
            if _h(x, y // 4, 3) % 9 == 0:
                k = max(0, k - 1)
            c.set(x, y, POST[k])


def _beam(c, x0, y0, x1, y1):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            k = 3 if y == y0 else 1 if y == y1 else 2
            if _h(x // 5, y, 5) % 11 == 0:
                k = max(0, k - 1)
            c.set(x, y, POST[k])


def back_wall(c):
    for y in range(0, WALL_Y):
        for x in range(W):
            k = 2
            if _h(x // 3, y // 2, 7) % 19 == 0:
                k = 1                                   # straw in the earth plaster
            elif _h(x // 4, y // 3, 9) % 23 == 0:
                k = 3
            c.set(x, y, PLASTER[k])
    _beam(c, 0, 0, W - 1, 4)                            # beam under the ceiling
    for x in range(0, W, 2):
        c.set(x, 5, darken_c(PLASTER[1], 0.82))
    _beam(c, 0, WALL_Y - 4, W - 1, WALL_Y - 1)          # skirting board
    for px in (96, 190):
        _post(c, px, 0, px + 4, WALL_Y - 1)


def lattice_window(c, x0, y0, x1, y1):
    _beam(c, x0 - 2, y0 - 2, x1 + 2, y0 - 1)
    _beam(c, x0 - 2, y1 + 1, x1 + 2, y1 + 2)
    _post(c, x0 - 2, y0 - 2, x0 - 1, y1 + 2)
    _post(c, x1 + 1, y0 - 2, x1 + 2, y1 + 2)
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            c.set(x, y, HANJI[3] if y - y0 < (y1 - y0) * 0.4 else HANJI[2])
    for x in range(x0 + 4, x1, 4):
        c.line(x, y0, x, y1, POST[2])
    for y in range(y0 + 4, y1, 4):
        c.line(x0, y, x1, y, POST[2])
    c.line((x0 + x1) // 2, y0, (x0 + x1) // 2, y1, POST[1])


def calendar(c, x0, y0):
    c.rect(x0, y0, x0 + 7, y0 + 9, HANJI[3])
    c.rect(x0, y0, x0 + 7, y0 + 2, (176, 40, 44, 255))
    for r in range(3):
        for k in range(3):
            c.set(x0 + 1 + k * 2, y0 + 4 + r * 2, (176, 40, 44, 255) if k == 0 else (80, 70, 64, 255))
    c.set(x0 + 3, y0 - 1, POST[0])


def wall_clock(c, cx, cy):
    c.ellipse(cx, cy, 4, 4, WOOD)
    c.ellipse(cx, cy, 2.6, 2.6, [HANJI[3]] * 4, flat=True)
    c.line(cx, cy, cx, cy - 2, (40, 30, 26, 255)); c.line(cx, cy, cx + 1, cy, (40, 30, 26, 255))
    c.line(cx, cy + 4, cx, cy + 8, POST[1]); c.set(cx, cy + 8, P['yellow'][2])   # pendulum


def floor(c):
    rnd = seeded(17)
    shade = {(sx, sy): rnd.choice((0.0, 0.0, 0.03, -0.03, 0.05)) for sx in range(9) for sy in range(6)}
    base = tuple((a + b) // 2 for a, b in zip(JANGPAN[1], JANGPAN[2]))
    for y in range(WALL_Y, FRONT_Y):
        for x in range(W):
            sx, sy = x // 36, (y - WALL_Y) // 26                     # oiled-paper sheets (~1.8 x 1.3 m)
            v = shade[(sx, sy)]
            col = lighten_c(base, v) if v >= 0 else darken_c(base, 1 + v)
            t = x / 110.0 + ((x * 3 + y * 5) % 8) / 64.0             # the araetmok, browned in soft steps
            if t < 0.45:
                col = darken_c(col, 0.75)
            elif t < 0.75:
                col = darken_c(col, 0.85)
            elif t < 1.0:
                col = darken_c(col, 0.93)
            if x % 36 == 0 or (y - WALL_Y) % 26 == 0:
                col = darken_c(col, 0.88)
            c.set(x, y, col)
    for y in range(WALL_Y + 3, WALL_Y + 34):                         # window light on the floor
        k = (y - WALL_Y - 3) / 31
        x0, x1 = int(124 + k * 16), int(164 + k * 16)
        for x in range(x0, x1):
            edge = min(x - x0, x1 - x, y - WALL_Y - 3, WALL_Y + 34 - y)
            p = c.get(x, y)
            if p and (edge > 2 or (x + y) % 2 == 0):
                c.set(x, y, lighten_c(p, 0.1 * (1 - k)))
    for y in range(WALL_Y, WALL_Y + 2):
        for x in range(W):
            c.set(x, y, darken_c(c.get(x, y), 0.85))


def front_wall(c):
    _beam(c, 0, FRONT_Y, W - 1, FRONT_Y + 3)                         # the sill (munjibang)
    for y in range(FRONT_Y + 4, H):
        for x in range(W):
            c.set(x, y, PLASTER[1] if (x // 3 + y // 2) % 11 else PLASTER[0])
    _beam(c, 0, H - 3, W - 1, H - 1)
    for px in (60, 150):
        _post(c, px, FRONT_Y, px + 4, H - 1)
    d0, d1 = DOOR
    for y in range(FRONT_Y, H):                                      # the opening: porch boards and daylight
        for x in range(d0, d1 + 1):
            c.set(x, y, (226, 214, 186, 255) if (y - FRONT_Y) % 3 else (198, 178, 140, 255))
    # the door leaf slid aside to the left (paper lattice), and the frame
    lx0, lx1 = d0 - 22, d0 - 2
    for y in range(FRONT_Y + 3, H - 3):
        for x in range(lx0, lx1 + 1):
            c.set(x, y, HANJI[2])
    for x in range(lx0 + 3, lx1, 4):
        c.line(x, FRONT_Y + 3, x, H - 4, POST[2])
    for y in range(FRONT_Y + 6, H - 3, 4):
        c.line(lx0, y, lx1, y, POST[2])
    _post(c, d0 - 2, FRONT_Y, d0 - 1, H - 1)
    _post(c, d1 + 1, FRONT_Y, d1 + 2, H - 1)
    for y in range(FRONT_Y - 18, FRONT_Y):                           # daylight in through the door
        t = (FRONT_Y - y) / 18
        for x in range(int(d0 + 2 - t * 4), int(d1 - 2 + t * 4)):
            if (x + y) % 2 == 0:
                p = c.get(x, y)
                if p:
                    c.set(x, y, lighten_c(p, 0.1 * (1 - t)))


def jagaejang(c, x0, y0):
    """Black lacquer wardrobe (26 x 38 px, ~1.3 x 1.9 m) with mother-of-pearl: a crane, a pine twig, flowers."""
    w, h = 26, 36
    c.shadow(x0 + w / 2, y0 + h + 2, w / 2 + 1, 1.5)
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            k = 3 if (y == y0 or x == x0) else 1 if x >= x0 + w - 2 else 2
            c.set(x, y, LACQUER[k])
    c.line(x0 + w // 2, y0 + 2, x0 + w // 2, y0 + h - 7, LACQUER[0])
    c.line(x0 + 1, y0 + h - 7, x0 + w - 2, y0 + h - 7, LACQUER[0])
    c.set(x0 + w // 2 - 2, y0 + 15, P['yellow'][2]); c.set(x0 + w // 2 + 2, y0 + 15, P['yellow'][2])
    c.set(x0 + w // 2, y0 + h - 4, P['yellow'][2])
    c.line(x0 + 3, y0 + 9, x0 + 7, y0 + 11, NACRE[0]); c.set(x0 + 5, y0 + 7, NACRE[0]); c.set(x0 + 5, y0 + 6, (220, 60, 70, 255))   # crane
    c.line(x0 + 15, y0 + 6, x0 + 22, y0 + 4, NACRE[1]); c.set(x0 + 17, y0 + 5, NACRE[1]); c.set(x0 + 20, y0 + 6, NACRE[1])       # pine
    for (fx, fy) in [(x0 + 17, y0 + 18), (x0 + 21, y0 + 22), (x0 + 6, y0 + 22)]:
        c.set(fx, fy, NACRE[2]); c.set(fx + 1, fy, NACRE[0])
    for x in (x0 + 1, x0 + w - 3):
        c.rect(x, y0 + h, x + 1, y0 + h + 1, LACQUER[1])


def desk(c, x0, y0):
    """A low writing desk (seoan) against the wall: a little radio and the letters from the city."""
    w = 26
    c.shadow(x0 + w / 2, y0 + 10, w / 2, 1.2)
    c.rect(x0, y0, x0 + w - 1, y0 + 2, WOOD[3]); c.rect(x0, y0 + 3, x0 + w - 1, y0 + 5, WOOD[2])
    for x in (x0 + 1, x0 + w - 3):
        c.rect(x, y0 + 6, x + 1, y0 + 9, WOOD[1])
    c.rect(x0 + 3, y0 - 4, x0 + 9, y0 - 1, WOOD[1]); c.rect(x0 + 4, y0 - 3, x0 + 6, y0 - 2, (60, 50, 44, 255))   # radio
    c.set(x0 + 8, y0 - 3, P['yellow'][3]); c.line(x0 + 9, y0 - 4, x0 + 11, y0 - 8, P['metal'][2])
    c.rect(x0 + 14, y0 - 1, x0 + 19, y0, COTTON[3]); c.rect(x0 + 15, y0 - 2, x0 + 20, y0 - 1, COTTON[2])          # letters
    c.set(x0 + 17, y0 - 1, (176, 40, 44, 255))


def quilt_stack(c, x0, y0):
    """Folded quilts on a low chest (bandaji), ~1.3 m wide."""
    chest = ramp('#4a2a14', '#6e4220', '#946032', '#b88250')
    w = 26
    c.shadow(x0 + w / 2, y0 + 13, w / 2 + 1, 1.5)
    for y in range(y0, y0 + 11):
        for x in range(x0, x0 + w):
            c.set(x, y, chest[3] if y == y0 else chest[1] if x >= x0 + w - 2 else chest[2])
    c.rect(x0 + w // 2 - 1, y0 + 3, x0 + w // 2 + 1, y0 + 6, P['metal'][2])
    for x in (x0 + 3, x0 + w - 5):
        c.rect(x, y0 + 2, x + 2, y0 + 3, P['metal'][1])
    y = y0
    for r in (SILK_R, COTTON, SILK_G, SILK_Y, SILK_R):
        th = 3 if r is not COTTON else 2
        y -= th
        for yy in range(y, y + th):
            for x in range(x0 + 1, x0 + w - 1):
                c.set(x, yy, r[3] if yy == y else r[1] if yy == y + th - 1 else r[2])


def bedding(c, x0, y0):
    """A yo laid on the araetmok (~1.1 x 1.9 m), the quilt folded back, a pillow with embroidered ends."""
    w, h = 26, 42
    c.shadow(x0 + w / 2, y0 + h, w / 2, 1.2, alpha=40)
    mat = ramp('#c0a8a0', '#e2cec6', '#f2e2da', '#fff4ee')
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            c.set(x, y, mat[1] if (x == x0 + w - 1 or y == y0 + h - 1) else mat[2])
    qy = y0 + 14
    for y in range(qy, y0 + h - 1):
        for x in range(x0 + 1, x0 + w - 1):
            border = x < x0 + 3 or x > x0 + w - 4 or y > y0 + h - 4
            k = 1 if border else 2
            if not border and ((x - x0) * 3 + (y - qy) * 5) % 13 == 0:
                k = 3
            if x == x0 + w - 2 or y == y0 + h - 2:
                k = 0
            c.set(x, y, SILK_R[k])
    for x in range(x0 + 1, x0 + w - 1):
        c.set(x, qy - 1, COTTON[3]); c.set(x, qy, COTTON[1])
    for y in range(y0 + 3, y0 + 8):                                  # the pillow
        for x in range(x0 + 5, x0 + w - 5):
            c.set(x, y, COTTON[3] if y < y0 + 5 else COTTON[2])
    for ex in (x0 + 4, x0 + w - 5):
        c.rect(ex, y0 + 3, ex, y0 + 7, SILK_G[2]); c.set(ex, y0 + 5, SILK_Y[3])


def clothes_pole(c, x0, x1, y):
    """A bamboo pole on the back wall (hwaetdae) with the city suit jacket still hanging on it, and a scarf."""
    bamboo = ramp('#6a5a2a', '#9a8640', '#c4ae5e', '#e2d08a')
    for (x, yy) in ((x0, y), (x1, y)):
        c.line(x, yy - 3, x, yy, POST[1])                            # the cords it hangs from
    c.line(x0, y, x1, y, bamboo[2]); c.line(x0, y + 1, x1, y + 1, bamboo[1])
    for x in range(x0 + 6, x1, 9):
        c.set(x, y, bamboo[0])                                       # the joints
    jx = x0 + 9                                                      # the suit jacket: dark gray, a white collar
    suit = ramp('#1e2026', '#2e3038', '#42444e', '#5a5c68')
    c.poly([(jx, y + 2), (jx + 12, y + 2), (jx + 13, y + 17), (jx - 1, y + 17)], suit, light_axis='x')
    c.line(jx + 6, y + 2, jx + 6, y + 15, suit[0])
    c.set(jx + 5, y + 3, COTTON[3]); c.set(jx + 7, y + 3, COTTON[3]); c.set(jx + 6, y + 4, (120, 30, 40, 255))   # collar, tie
    sx = x1 - 10                                                     # a knitted scarf
    for yy in range(y + 2, y + 12):
        c.set(sx, yy, SILK_Y[2] if yy % 2 else SILK_Y[1]); c.set(sx + 1, yy, SILK_Y[1])


def soban(c, cx, cy):
    """A round low table (~0.9 m) with rice, soup, kimchi and greens."""
    c.shadow(cx, cy + 7, 10, 1.5)
    for x in (cx - 6, cx + 5):
        c.rect(x, cy + 2, x + 1, cy + 6, WOOD[1])
    c.ellipse(cx, cy, 10, 4, WOOD)
    c.ellipse(cx, cy - 0.5, 8.5, 3, [WOOD[2]] * 4, flat=True)
    for (dx, dy, col) in [(-4, -1, COTTON[3]), (0, -1, (170, 110, 60, 255)), (4, 0, (196, 60, 44, 255)), (-1, 1, (90, 140, 70, 255))]:
        c.set(cx + dx, cy + dy, col); c.set(cx + dx + 1, cy + dy, col); c.set(cx + dx, cy + dy + 1, P['white'][2])


def cushion(c, cx, cy, r):
    for y in range(cy - 2, cy + 3):
        for x in range(cx - 4, cx + 5):
            if abs(x - cx) == 4 and abs(y - cy) == 2:
                continue
            c.set(x, y, r[3] if y == cy - 2 else r[1] if y == cy + 2 else r[2])


def home_room():
    c = Canvas(W, H)
    back_wall(c)
    lattice_window(c, 126, 9, 160, 27)
    calendar(c, 80, 12)
    clothes_pole(c, 40, 74, 9)
    wall_clock(c, 214, 13)
    floor(c)
    front_wall(c)
    jagaejang(c, 9, 7)
    quilt_stack(c, 252, 31)
    desk(c, 206, 40)
    bedding(c, 12, 58)
    soban(c, 150, 90)
    cushion(c, 136, 96, SILK_G)
    cushion(c, 164, 96, SILK_Y)
    for x in (0, W - 6):
        _post(c, x, 0, x + 5, H - 1)
    return c


# world-space layout for HomeRoom.cs and the web preview
def world(px, py):
    return round((px - W / 2) / 32, 3), round((H / 2 - py) / 32, 3)


if __name__ == '__main__':
    import sys
    home_room().image().resize((W * 4, H * 4), Image.NEAREST).save(sys.argv[1] if len(sys.argv) > 1 else 'home_room.png')
    for name, (px, py) in {'floor top': (144, WALL_Y), 'front': (144, FRONT_Y), 'left inner': (6, 0), 'right inner': (W - 6, 0),
                           'door x': (DOOR[0], 0), 'door x1': (DOOR[1] + 1, 0), 'bedding centre': (25, 79), 'bed x0,y1': (12, 100),
                           'wardrobe x0,x1 base': (9, 45), 'wardrobe x1': (35, 0), 'chest x0 base': (252, 44), 'chest x1': (278, 0),
                           'desk x0 base': (206, 50), 'desk x1': (232, 0), 'soban legs': (150, 96)}.items():
        print(name, world(px, py))
