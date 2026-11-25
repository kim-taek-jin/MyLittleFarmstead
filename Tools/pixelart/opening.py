"""Title screen and the short opening (D-1).

  title_bg       640 x 360  the homestead, gray on the left and in colour on the right (the whole game in one picture)
  op_bus         640 x 360  a country bus stop at dusk: the bus pulls away, you stand there with a suitcase
  op_arrive      640 x 360  the gate of the empty house, all gray
  op_arrive_color 640 x 360 the same, with a round patch of colour coming back around you and the persimmon tree
  guide_arrow    small gold arrow that bobs over the place the first-day guide points at

The city panel reuses World/bg_city. The farm pictures are crops of the real farm map (bg_farmmap_<season>),
so the opening shows the same place you then walk around in. All the gray is forest.gray (a fixed per-colour
mapping), the colour patch uses a dithered edge: no soft gradients anywhere.
"""
import math
import os

from PIL import Image

import forest
import kvillage as K
from pixel import Canvas, P, ramp

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.join(HERE, '..', '..', 'Assets', 'Resources', 'PixelArt')
W, H = 640, 360
BAYER = [[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]


def _farm(season):
    path = os.path.join(ART, 'World', f'bg_farmmap_{season}.png')
    return Image.open(path).convert('RGBA')


def _sprite(rel):
    return Image.open(os.path.join(ART, rel)).convert('RGBA')


def _scale(img, k):
    return img.resize((img.width * k, img.height * k), Image.NEAREST)


def _paste_canvas(img, canvas, x, y, k=1):
    """Paste a pixel Canvas with its bottom-middle at (x, y), scaled by k."""
    for yy in range(canvas.h):
        for xx in range(canvas.w):
            p = canvas.get(xx, yy)
            if p is None:
                continue
            p = p if len(p) == 4 else tuple(p) + (255,)
            if p[3] == 0:
                continue
            for dy in range(k):
                for dx in range(k):
                    px, py = int(x - canvas.w * k / 2 + xx * k + dx), int(y - canvas.h * k + yy * k + dy)
                    if 0 <= px < img.width and 0 <= py < img.height:
                        img.putpixel((px, py), p)


def _dither_mix(gray, color, inside):
    """Per pixel: colour where inside(x, y) returns >= 1, gray where <= 0, ordered dither in between."""
    out = gray.copy()
    gp, cp, op = gray.load(), color.load(), out.load()
    for y in range(gray.height):
        for x in range(gray.width):
            t = inside(x, y)
            if t >= 1 or (t > 0 and t * 16 > BAYER[y % 4][x % 4]):
                op[x, y] = cp[x, y]
    return out


# ---------------------------------------------------------------- title
def title_bg():
    """The yard, the house and the plots in autumn: gray on the left, colour on the right, split by a
    gentle diagonal with a dithered seam. The bottom is darkened a little so the menu reads on top."""
    color = _farm('autumn').crop((96, 0, 96 + W, H))
    gray = forest.gray(color)

    def inside(x, y):                                  # colour on the left (house, persimmon tree), gray on the right
        seam = 360 - (y - H / 2) * 0.35 + 10 * math.sin(y / 23)
        return (seam - x + 40) / 80
    img = _dither_mix(gray, color, inside)
    px = img.load()
    for y in range(H - 120, H):                       # darker band at the bottom, in dithered steps
        t = (y - (H - 120)) / 120
        for x in range(W):
            if t * 16 > BAYER[y % 4][x % 4]:
                r, g, b, a = px[x, y]
                px[x, y] = (int(r * 0.55), int(g * 0.55), int(b * 0.6), a)
    return img


# ---------------------------------------------------------------- the bus stop
def _bus():
    c = Canvas(150, 62)
    body = ramp('#2a4a36', '#3e6a4a', '#5a8e62', '#8cbc8a')
    c.shadow(75, 59, 70, 3)
    c.box(4, 10, 146, 52, body)
    c.rect(4, 36, 146, 40, P['white'][2])                 # a pale stripe
    for x in range(14, 132, 20):                          # windows
        c.box(x, 16, x + 15, 31, P['glass'])
    c.box(132, 14, 143, 46, P['glass'])                   # the door at the back (it's driving away)
    for x in (30, 118):
        c.ellipse(x, 52, 8, 8, P['metal'])
        c.ellipse(x, 52, 3, 3, [P['metal'][3]] * 4, flat=True)
    c.rect(1, 44, 4, 48, P['red'][2])                     # tail light
    c.outline()
    return c


def _shelter():
    c = Canvas(110, 72)
    c.shadow(55, 70, 50, 3)
    for x in (12, 96):
        c.box(x, 22, x + 4, 70, P['metal'])
    c.poly([(2, 24), (8, 10), (102, 10), (108, 24)], K.ROOF, light_axis='x')
    c.box(22, 50, 90, 56, P['woodlight'])                 # bench
    for x in (26, 84):
        c.box(x, 56, x + 3, 68, P['wood'])
    c.outline()
    return c


def _sign():
    c = Canvas(26, 76)
    c.box(11, 22, 14, 74, P['metal'])
    c.ellipse(12.5, 12, 11, 11, P['white'])
    c.ellipse(12.5, 12, 8.5, 8.5, P['cloth'])
    c.box(7, 8, 18, 15, P['white'])                       # a tiny bus on the plate
    c.set(9, 16, P['outline']); c.set(16, 16, P['outline'])
    c.outline()
    return c


def _suitcase():
    c = Canvas(18, 18)
    c.box(1, 5, 16, 17, P['tan'])
    c.rect(6, 1, 11, 2, P['wood'][1]); c.rect(6, 1, 6, 5, P['wood'][1]); c.rect(11, 1, 11, 5, P['wood'][1])
    c.rect(1, 10, 16, 10, P['tan'][1])
    c.outline()
    return c


def op_bus():
    img = Image.new('RGBA', (W, H))
    px = img.load()
    sky = [(118, 112, 128), (138, 128, 140), (160, 146, 150), (184, 166, 158)]   # dusk, almost gray
    for y in range(0, 210):
        t = y / 210 * (len(sky) - 1)
        i = int(t)
        f = t - i
        col = sky[min(len(sky) - 1, i + 1)] if f * 16 > BAYER[y % 4][0] else sky[i]
        for x in range(W):
            c2 = sky[min(len(sky) - 1, i + 1)] if f * 16 > BAYER[y % 4][x % 4] else sky[i]
            px[x, y] = c2 + (255,)
    # far and near ridges
    for (base, amp, col, k) in [(150, 26, (112, 112, 122), 0), (178, 22, (88, 92, 98), 1)]:
        for x in range(W):
            top = int(base - amp * (0.6 * math.sin(x / 61 + k * 2) + 0.4 * math.sin(x / 23 + k)))
            for y in range(top, 236):
                px[x, y] = col + (255,)
    # fields (flat paddies after harvest), with bund lines
    for y in range(206, 290):
        for x in range(W):
            col = (120, 118, 100) if (y // 7) % 2 else (132, 128, 108)
            if (y - 206) % 21 == 0:
                col = (96, 92, 80)
            px[x, y] = col + (255,)
    # the road
    for y in range(290, 336):
        for x in range(W):
            col = (78, 78, 84)
            if y in (290, 291):
                col = (150, 146, 134)
            if 311 <= y <= 312 and (x // 18) % 2 == 0:
                col = (196, 190, 170)
            px[x, y] = col + (255,)
    for y in range(336, H):
        for x in range(W):
            px[x, y] = ((104, 100, 86) if (x + y) % 5 else (92, 88, 76)) + (255,)
    # telephone poles along the road, getting smaller into the distance
    for k, (x, h) in enumerate([(40, 140), (300, 110), (470, 86), (580, 70)]):
        for y in range(290 - h, 292):
            for dx in range(3):
                px[x + dx, y] = (70, 62, 56, 255)
        for dx in range(-10, 13):
            px[x + dx, 290 - h + 8] = (70, 62, 56, 255)
    _paste_canvas(img, _shelter(), 150, 300)
    _paste_canvas(img, _sign(), 236, 302)
    _paste_canvas(img, _bus(), 540, 316)
    for k in range(4):                                      # exhaust puffs behind the bus
        cx, cy, r = 456 - k * 16, 300 - k * 4, 4 + k * 2
        for y in range(cy - r, cy + r):
            for x in range(cx - r, cx + r):
                if (x - cx) ** 2 + (y - cy) ** 2 <= r * r and (x + y + k) % 2 == 0:
                    px[x, y] = (176, 176, 180, 255)
    you = _scale(_sprite('Player/player_down_0.png'), 2)
    img.alpha_composite(you, (282, 322 - you.height))
    _paste_canvas(img, _suitcase(), 336, 324, 2)
    return forest.gray(img, darker=0.95)


# ---------------------------------------------------------------- arriving at the house
ARRIVE_CROP = (32, 0, 32 + W, H)                  # in farm-map pixels, shown 1:1 like in the game


def _arrive_color(season='winter'):
    img = _farm(season).crop(ARRIVE_CROP)
    you = _sprite('Player/player_up_0.png')
    gx = 11 * 32 - ARRIVE_CROP[0]                      # the front gate (cols 10..11)
    gy = 8 * 32 + 22 - ARRIVE_CROP[1]
    img.alpha_composite(you, (gx - you.width // 2, gy - you.height))
    _paste_canvas(img, _suitcase(), gx + 16, gy + 1)
    return img, (gx, gy)


def op_arrive():
    img, _ = _arrive_color()
    return forest.gray(img)


def op_arrive_color():
    winter, (gx, gy) = _arrive_color('winter')
    gray = forest.gray(winter)
    color, _ = _arrive_color('spring')                 # inside the patch it is already spring (in your heart)
    tree = (5 * 32 + 16 - ARRIVE_CROP[0], 4 * 32 - ARRIVE_CROP[1])   # the persimmon tree in the yard

    def inside(x, y):
        best = 0
        for (cx, cy, r) in [(gx, gy - 16, 70), (tree[0], tree[1] - 10, 78)]:
            d = math.hypot(x - cx, y - cy)
            best = max(best, (r - d) / 16)
        return best
    return _dither_mix(gray, color, inside)


def guide_arrow():
    c = Canvas(14, 16)
    gold = ramp('#7a5410', '#c49018', '#f0c848', '#fff2a0')
    c.box(4, 0, 9, 7, gold)
    c.poly([(0, 7), (13, 7), (6.5, 15)], gold, light_axis='x')
    c.outline()
    return c


def images():
    return {'title_bg': title_bg(), 'op_bus': op_bus(), 'op_arrive': op_arrive(), 'op_arrive_color': op_arrive_color()}


if __name__ == '__main__':
    out = '/tmp/opening_preview.png'
    imgs = images()
    sheet = Image.new('RGBA', (W * 2, H * 2))
    for i, k in enumerate(['title_bg', 'op_bus', 'op_arrive', 'op_arrive_color']):
        sheet.paste(imgs[k], ((i % 2) * W, (i // 2) * H))
    sheet.save(out)
    print(out)
