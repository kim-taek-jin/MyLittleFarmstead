"""H-1: the inside of the house as a Korean ondol room (replaces the old Western room with a fireplace).

Same picture size and world placement as before (590 x 328 px, 18.43 x 10.24 world units, 32 px per unit,
centre (0, -0.027)), so the game's background object can keep using it. HomeRoom.cs puts the walls,
the bedding (sleep), the furniture colliders and the sliding door (exit, bottom middle) on top of it.

    back wall   : earth-plastered wall between wooden posts, a paper lattice window, a calendar
    floor       : oiled-paper floor (jangpan) in sheets; the warm end (araetmok) on the left, browned by the ondol
    left        : bedding laid out on the araetmok (yo, quilt folded back, a pillow with embroidered ends)
    back left   : a black lacquer wardrobe inlaid with mother-of-pearl (jagaejang)
    back right  : folded quilts stacked on a low chest (bandaji), a little radio
    middle right: a round low table (soban) with rice, soup and side dishes, two floor cushions
    bottom      : the front wall with a sliding paper door (the way out to the porch)
"""
import math
from PIL import Image
from pixel import Canvas, P, ramp, darken_c, lighten_c, seeded
from backgrounds import _h

W, H = 590, 328
WALL_Y = 78          # where the back wall meets the floor
FRONT_Y = 300        # where the floor meets the front wall
DOOR = (255, 335)    # the sliding door in the front wall (x range)

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


def _wood_v(c, x0, y0, x1, y1, r):
    """A vertical wooden post with grain."""
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            k = 2 if x - x0 < (x1 - x0) * 0.6 else 1
            if x == x0:
                k = 3
            if _h(x, y // 5, 3) % 9 == 0:
                k = max(0, k - 1)
            c.set(x, y, r[k])


def _wood_h(c, x0, y0, x1, y1, r):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            k = 3 if y == y0 else 1 if y == y1 else 2
            if _h(x // 7, y, 5) % 11 == 0:
                k = max(0, k - 1)
            c.set(x, y, r[k])


def back_wall(c):
    for y in range(0, WALL_Y):
        for x in range(W):
            k = 2
            if _h(x // 3, y // 2, 7) % 13 == 0:
                k = 1                                   # straw flecks in the earth plaster
            elif _h(x // 4, y // 3, 9) % 17 == 0:
                k = 3
            c.set(x, y, PLASTER[k])
    _wood_h(c, 0, 0, W - 1, 9, POST)                    # the beam under the ceiling
    for x in range(0, W, 3):
        c.set(x, 10, darken_c(PLASTER[1], 0.8))         # its shadow on the wall
    _wood_h(c, 0, WALL_Y - 8, W - 1, WALL_Y - 1, POST)  # skirting board
    for px in (0, 196, 388, W - 10):                    # posts
        _wood_v(c, px, 0, px + 9, WALL_Y - 1, POST)


def lattice_window(c, x0, y0, x1, y1):
    """A paper window with a wooden lattice (munsal), lit from outside."""
    _wood_h(c, x0 - 3, y0 - 3, x1 + 3, y0 - 1, POST)
    _wood_h(c, x0 - 3, y1 + 1, x1 + 3, y1 + 3, POST)
    for x in (x0 - 3, x1 + 1):
        _wood_v(c, x, y0 - 3, x + 2, y1 + 3, POST)
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            t = (y - y0) / max(1, y1 - y0)
            c.set(x, y, HANJI[3] if t < 0.35 else HANJI[2])
    for x in range(x0 + 7, x1, 8):                      # vertical bars
        c.line(x, y0, x, y1, POST[2])
    for y in range(y0 + 6, y1, 7):                      # horizontal bars
        c.line(x0, y, x1, y, POST[2])
    mid = (x0 + x1) // 2
    c.line(mid, y0, mid, y1, POST[1])                   # the two leaves meet here


def calendar(c, x0, y0):
    c.rect(x0, y0, x0 + 20, y0 + 26, HANJI[3])
    c.rect(x0, y0, x0 + 20, y0 + 5, (176, 40, 44, 255))  # red header
    for r in range(4):
        for k in range(5):
            col = (176, 40, 44, 255) if k == 0 else (70, 60, 56, 255)
            c.set(x0 + 2 + k * 4, y0 + 9 + r * 4, col)
    c.set(x0 + 10, y0 - 2, POST[0])                     # the nail
    c.line(x0 + 10, y0 - 1, x0 + 4, y0, POST[1]); c.line(x0 + 10, y0 - 1, x0 + 16, y0, POST[1])


def floor(c):
    rnd = seeded(17)
    shade = {}
    for sy in range(6):
        for sx in range(9):
            shade[(sx, sy)] = rnd.choice((0.0, 0.0, 0.03, -0.03, 0.05))    # each oiled sheet a touch different
    base = tuple((a + b) // 2 for a, b in zip(JANGPAN[1], JANGPAN[2]))
    for y in range(WALL_Y, FRONT_Y):
        for x in range(W):
            sx, sy = x // 74, (y - WALL_Y) // 44
            col = lighten_c(base, shade[(sx, sy)]) if shade[(sx, sy)] >= 0 else darken_c(base, 1 + shade[(sx, sy)])
            # the araetmok: browned by years of the ondol, in soft steps (ordered dither only at the step edges)
            t = x / 200.0 + ((x * 3 + y * 5) % 8) / 64.0
            if t < 0.45:
                col = darken_c(col, 0.74)
            elif t < 0.75:
                col = darken_c(col, 0.84)
            elif t < 1.0:
                col = darken_c(col, 0.93)
            if (x % 74 == 0) or ((y - WALL_Y) % 44 == 0):
                col = darken_c(col, 0.88)                    # seams between sheets
            c.set(x, y, col)
    # daylight from the paper window lying on the floor (a soft slanted patch)
    for y in range(WALL_Y + 6, WALL_Y + 70):
        k = (y - WALL_Y - 6) / 64
        for x in range(int(226 + k * 34), int(358 + k * 34)):
            p = c.get(x, y)
            edge = min(x - (226 + k * 34), (358 + k * 34) - x, y - WALL_Y - 6, WALL_Y + 70 - y)
            if p and (edge > 3 or (x + y) % 2 == 0):
                c.set(x, y, lighten_c(p, 0.1 * (1 - k)))
    for y in range(WALL_Y, WALL_Y + 4):                  # the wall's shadow on the floor
        for x in range(W):
            c.set(x, y, darken_c(c.get(x, y), 0.85))


def front_wall(c):
    _wood_h(c, 0, FRONT_Y, W - 1, FRONT_Y + 6, POST)     # the sill (munjibang)
    for y in range(FRONT_Y + 7, H):
        for x in range(W):
            c.set(x, y, POST[1] if (x // 30 + y) % 9 else POST[0])
    d0, d1 = DOOR
    # the sliding paper door, half open: outside light through the opening
    for y in range(FRONT_Y - 2, H):
        for x in range(d0, d1 + 1):
            c.set(x, y, (232, 226, 206, 255) if y > FRONT_Y + 6 else POST[3])
    lattice_window(c, d0 - 40, FRONT_Y + 8, d0 - 4, H - 2)   # the door leaf slid aside
    for x in (d0 - 2, d1 + 1):
        _wood_v(c, x, FRONT_Y - 2, x + 2, H - 1, POST)
    for y in range(FRONT_Y + 9, H, 3):                  # the porch boards seen through the door
        c.line(d0 + 2, y, d1 - 2, y, (196, 176, 140, 255))
    # light falling in across the floor from the open door
    for y in range(FRONT_Y - 40, FRONT_Y):
        t = (FRONT_Y - y) / 40
        for x in range(int(d0 + 6 - t * 10), int(d1 - 6 + t * 10)):
            if (x + y) % 2 == 0 and _h(x, y, 2) % 3:
                p = c.get(x, y)
                if p:
                    c.set(x, y, lighten_c(p, 0.12 * (1 - t)))


def jagaejang(c, x0, y0):
    """Black lacquer wardrobe with mother-of-pearl inlay: cranes, a pine branch, flowers."""
    w, h = 90, 86
    c.shadow(x0 + w / 2, y0 + h + 2, w / 2 + 2, 3)
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            k = 2 if x < x0 + w - 6 else 1
            if y == y0 or x == x0:
                k = 3
            c.set(x, y, LACQUER[k])
    c.line(x0 + w // 2, y0 + 4, x0 + w // 2, y0 + h - 12, LACQUER[0])    # the two doors
    c.line(x0 + 2, y0 + h - 12, x0 + w - 3, y0 + h - 12, LACQUER[0])     # the drawer
    for (x, y) in [(x0 + w // 2 - 4, y0 + 36), (x0 + w // 2 + 3, y0 + 36), (x0 + w // 2 - 1, y0 + h - 7)]:
        c.rect(x, y, x + 1, y + 3, P['yellow'][2])                       # brass handles
    rnd = seeded(41)
    # a crane on the left door: body, neck, a wing
    cx, cy = x0 + 18, y0 + 26
    for i in range(9):
        c.set(cx + i, cy + i // 3, NACRE[0])
    c.line(cx + 2, cy - 1, cx + 6, cy - 9, NACRE[0]); c.set(cx + 6, cy - 10, (220, 60, 70, 255))
    c.line(cx + 3, cy + 1, cx + 12, cy - 4, NACRE[3])
    # a pine branch and flowers on the right door
    c.line(x0 + 52, y0 + 18, x0 + 80, y0 + 12, NACRE[1])
    for k in range(6):
        c.set(x0 + 56 + k * 4, y0 + 15 - k // 2 + (k % 2), NACRE[1])
    for (fx, fy) in [(x0 + 60, y0 + 46), (x0 + 72, y0 + 54), (x0 + 64, y0 + 60)]:
        for (dx, dy) in [(0, -1), (1, 0), (0, 1), (-1, 0)]:
            c.set(fx + dx, fy + dy, NACRE[2])
        c.set(fx, fy, NACRE[0])
    for _ in range(26):                                                    # nacre glints
        c.set(rnd.randint(x0 + 6, x0 + w - 8), rnd.randint(y0 + 6, y0 + h - 16), rnd.choice(NACRE))
    for x in (x0 + 4, x0 + w - 8):                                         # short legs
        c.rect(x, y0 + h, x + 3, y0 + h + 3, LACQUER[1])


def quilt_stack(c, x0, y0):
    """Folded quilts on a low chest (bandaji), a small radio on the side."""
    chest = ramp('#4a2a14', '#6e4220', '#946032', '#b88250')
    w = 96
    c.shadow(x0 + w / 2, y0 + 64, w / 2 + 2, 3)
    for y in range(y0 + 40, y0 + 62):
        for x in range(x0, x0 + w):
            c.set(x, y, chest[3] if y == y0 + 40 else chest[2] if x < x0 + w - 5 else chest[1])
    for x in range(x0 + 6, x0 + w - 6, 22):                                # iron fittings
        c.rect(x, y0 + 44, x + 6, y0 + 46, P['metal'][1])
    c.rect(x0 + w // 2 - 4, y0 + 48, x0 + w // 2 + 4, y0 + 54, P['metal'][2])   # the lock plate
    layers = [(SILK_R, 0), (COTTON, 1), (SILK_G, 2), (SILK_Y, 3), (SILK_R, 4)]
    y = y0 + 40
    for (r, i) in layers:
        th = 7 if r is not COTTON else 5
        y -= th
        inset = 3 + (i % 2) * 2
        for yy in range(y, y + th):
            for x in range(x0 + inset, x0 + w - inset):
                k = 3 if yy == y else 1 if yy == y + th - 1 else 2
                if r is not COTTON and (x + yy * 2) % 9 == 0:
                    k = 3                                                  # woven pattern on the silk
                c.set(x, yy, r[k])
        c.set(x0 + inset, y + 1, r[1]); c.set(x0 + w - inset - 1, y + 1, r[1])
    # radio
    rx, ry = x0 + w - 26, y0 + 30
    c.rect(rx, ry, rx + 18, ry + 10, chest[1]); c.rect(rx + 2, ry + 2, rx + 8, ry + 8, (60, 50, 44, 255))
    for k in range(3):
        c.line(rx + 3, ry + 3 + k * 2, rx + 7, ry + 3 + k * 2, (110, 96, 80, 255))
    c.set(rx + 13, ry + 4, P['yellow'][3]); c.set(rx + 15, ry + 6, (200, 190, 170, 255))
    c.line(rx + 16, ry, rx + 21, ry - 8, P['metal'][2])                    # antenna


def bedding(c, x0, y0):
    """A yo (mattress) laid on the warm floor, the quilt folded back, a pillow with embroidered round ends."""
    w, h = 110, 92
    c.shadow(x0 + w / 2, y0 + h, w / 2, 3, alpha=40)
    mat = ramp('#c0a8a0', '#e2cec6', '#f2e2da', '#fff4ee')
    for y in range(y0, y0 + h):
        for x in range(x0, x0 + w):
            k = 2 if x > x0 + 2 else 3
            if x == x0 + w - 1 or y == y0 + h - 1:
                k = 1
            c.set(x, y, mat[k])
    # the quilt over the lower two thirds, its top folded back (lining shows); a wide border, a quiet print
    qy = y0 + 30
    for y in range(qy, y0 + h - 2):
        for x in range(x0 + 4, x0 + w - 4):
            border = x < x0 + 12 or x > x0 + w - 13 or y > y0 + h - 11
            k = 1 if border else 2
            if not border and ((x - x0) * 3 + (y - qy) * 5) % 37 == 0:
                k = 3                                   # a scattered flower print
            if x == x0 + w - 5 or y == y0 + h - 3:
                k = 0                                   # the quilt's thickness on the shaded sides
            c.set(x, y, SILK_R[k])
    for x in range(x0 + 14, x0 + w - 14):               # a soft highlight where the quilt rounds over
        c.set(x, qy + 6, lighten_c(SILK_R[2], 0.12))
    for x in range(x0 + 4, x0 + w - 4):                 # the folded edge, white lining
        c.set(x, qy - 1, COTTON[3]); c.set(x, qy, COTTON[2]); c.set(x, qy + 1, COTTON[1])
    # the pillow: a long bolster with embroidered round ends (beggae)
    px0, px1, py = x0 + 22, x0 + w - 22, y0 + 10
    for y in range(py, py + 12):
        for x in range(px0, px1):
            c.set(x, y, COTTON[3] if y < py + 4 else COTTON[2] if y < py + 9 else COTTON[1])
    for ex in (px0, px1):
        c.ellipse(ex, py + 6, 4, 6, SILK_G)
        c.set(ex, py + 6, SILK_Y[3]); c.set(ex - 1, py + 4, SILK_R[2]); c.set(ex + 1, py + 8, SILK_R[2])


def soban(c, cx, cy):
    """A round low table with rice, soup, kimchi and two side dishes; spoon and chopsticks."""
    wood = ramp('#3a2010', '#5c3418', '#7e4c26', '#a26a3a')
    c.shadow(cx, cy + 22, 34, 4)
    for x in (cx - 22, cx + 18):                        # legs
        c.rect(x, cy + 6, x + 3, cy + 20, wood[1])
    c.ellipse(cx, cy, 36, 13, wood)
    c.ellipse(cx, cy - 1, 32, 10, [wood[2]] * 4, flat=True)
    bowl = P['white']
    c.ellipse(cx - 12, cy - 3, 6, 3.5, bowl); c.ellipse(cx - 12, cy - 5, 5, 2, [COTTON[3]] * 4, flat=True)      # rice
    c.ellipse(cx + 4, cy - 3, 6, 3.5, bowl); c.ellipse(cx + 4, cy - 5, 5, 2, [(170, 110, 60, 255)] * 4, flat=True)   # doenjang soup
    c.ellipse(cx + 18, cy + 2, 5, 3, bowl); c.ellipse(cx + 18, cy + 1, 4, 1.6, [(196, 60, 44, 255)] * 4, flat=True)  # kimchi
    c.ellipse(cx - 2, cy + 5, 4, 2.4, bowl); c.ellipse(cx - 2, cy + 4, 3, 1.2, [(90, 140, 70, 255)] * 4, flat=True)  # greens
    c.line(cx - 26, cy + 3, cx - 18, cy + 8, P['metal'][3]); c.line(cx - 24, cy + 2, cx - 16, cy + 7, P['metal'][2])  # spoon, chopsticks


def cushion(c, cx, cy, r):
    for y in range(cy - 7, cy + 8):
        for x in range(cx - 12, cx + 13):
            if abs(x - cx) / 12 + abs(y - cy) / 7.5 <= 1.6 and abs(x - cx) <= 12 and abs(y - cy) <= 7:
                k = 3 if y < cy - 4 else 1 if y > cy + 4 else 2
                c.set(x, y, r[k])
    c.set(cx, cy, r[0])


def home_room():
    c = Canvas(W, H)
    back_wall(c)
    lattice_window(c, 232, 16, 352, 60)
    calendar(c, 160, 22)
    floor(c)
    front_wall(c)
    bedding(c, 34, 132)
    soban(c, 400, 186)
    cushion(c, 352, 200, SILK_G)
    cushion(c, 448, 200, SILK_Y)
    jagaejang(c, 22, 14)
    quilt_stack(c, 470, 44)
    for x in (0, W - 12):                               # side posts framing the room
        _wood_v(c, x, 0, x + 11, H - 1, POST)
    c.outline_skip = True
    return c


# world-space layout for HomeRoom.cs / the web preview (world = (px - 295) / 32, (164 - py) / 32 - 0.027)
def world(px, py):
    return round((px - 295) / 32, 2), round((164 - py) / 32 - 0.027, 2)


if __name__ == '__main__':
    import sys
    home_room().image().resize((W * 2, H * 2), Image.NEAREST).save(sys.argv[1] if len(sys.argv) > 1 else 'home_room.png')
    for name, (px, py) in {'bedding centre': (89, 178), 'door': (295, 312), 'table': (400, 200), 'floor top': (295, WALL_Y), 'front': (295, FRONT_Y)}.items():
        print(name, world(px, py))
