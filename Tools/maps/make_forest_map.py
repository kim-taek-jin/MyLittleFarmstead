"""Author the forest (30 x 20 tiles), opened by spring of the heart: Assets/Resources/Maps/forest.json

Same rules as the farm map (one glyph per tile, column c -> x = originX + c, row r -> y = originY - 1 - r).
In Unity the forest lives inside the Farm scene, far to the east (like Star Hill lives far above),
so walking through the flower arch is a short teleport, not a scene load.

  #  dense trees (blocking)     ~  stream (blocking)      b  bridge
  =  path                       -  clearing floor         .  forest floor
  c  sapling spot (one per clearing: plant an acorn here)
  O  the old tree (3x3, blocking)   w  spring pool (blocking)   s  stump seat by the spring
  f  forage spot                L  fallen log (clear it with a mattock)   B  bush (blocking)
"""
import json, os

W, H = 30, 20
OX, OY = 190, 10
g = [['#'] * W for _ in range(H)]


def put(c, r, ch, over=None):
    if 0 <= c < W and 0 <= r < H and (over is None or g[r][c] in over):
        g[r][c] = ch


def ellipse(cx, cy, rx, ry, ch):
    for r in range(H):
        for c in range(W):
            if ((c - cx) / rx) ** 2 + ((r - cy) / ry) ** 2 <= 1:
                put(c, r, ch)


def path(points, ch='=', width=2):
    for (c0, r0), (c1, r1) in zip(points, points[1:]):
        if r0 == r1:
            for c in range(min(c0, c1), max(c0, c1) + 1):
                for k in range(width):
                    put(c, r0 + k, ch, over='#.-')
        else:
            for r in range(min(r0, r1), max(r0, r1) + 1):
                for k in range(width):
                    put(c0 + k, r, ch, over='#.-')


# the central plaza around the old tree, and the three clearings
ellipse(13.5, 8.5, 5.2, 4.0, '.')
ellipse(6.5, 4.0, 3.2, 2.2, '-')      # north clearing
ellipse(4.0, 11.5, 2.8, 2.6, '-')     # west clearing
ellipse(12.0, 16.0, 3.4, 2.1, '-')    # south clearing
# pockets east of the stream
ellipse(25.0, 3.5, 2.6, 1.8, '.')     # north-east pocket
ellipse(25.5, 15.0, 2.6, 1.8, '.')    # south-east pocket (behind a log)

# paths: entrance (east edge) -> bridge -> plaza, and plaza -> each clearing
path([(29, 9), (16, 9)])
path([(10, 5), (10, 8)])              # plaza -> north clearing
path([(7, 5), (10, 5)])
path([(5, 11), (9, 11)])              # plaza -> west clearing
path([(12, 12), (12, 14)])            # plaza -> south clearing
path([(24, 5), (24, 9)])              # entrance path -> north-east pocket
path([(25, 11), (25, 14)])            # entrance path -> south-east pocket

# the stream runs north-south east of the plaza; the path crosses it on a bridge
for r in range(H):
    c = 20 if (r // 4) % 2 == 0 else 21
    for cc in (c, c + 1):
        put(cc, r, 'b' if g[r][cc] == '=' else '~')

# the old tree in the middle of the plaza, the spring and its seat
for r in range(6, 9):
    for c in range(12, 15):
        put(c, r, 'O')
for (c, r) in [(16, 11), (17, 11), (16, 12), (17, 12)]:
    put(c, r, 'w')
put(15, 12, 's')

# sapling spots: the middle of each clearing
put(6, 4, 'c'); put(4, 11, 'c'); put(12, 16, 'c')

# fallen logs: one blocks the way to the south clearing, one blocks the south-east pocket
put(12, 14, 'L'); put(13, 14, 'L')
put(25, 13, 'L'); put(26, 13, 'L')

# a few bushes and lone trees so open ground isn't empty
for (c, r) in [(9, 7), (17, 6), (16, 5), (9, 10), (18, 9 - 3)]:
    put(c, r, 'B', over='.')
# forage spots (the game picks a few of them each morning)
for (c, r) in [(5, 3), (8, 4), (4, 5), (2, 11), (5, 13), (3, 10), (10, 15), (14, 17), (13, 16),
               (11, 4), (17, 8), (15, 10), (10, 9), (24, 3), (26, 4), (25, 15), (26, 16), (27, 8)]:
    put(c, r, 'f', over='.-=')

data = {
    'name': 'forest',
    'originX': OX, 'originY': OY, 'width': W, 'height': H,
    'rows': [''.join(row) for row in g],
    'exits': [
        {'to': 'Farm', 'x0': 219.4, 'y0': -1.0, 'x1': 220.0, 'y1': 1.0, 'spawnX': -7.6, 'spawnY': -9.1, 'locked': ''},
    ],
    'signs': [],
    'arrivals': [{'from': 'Farm', 'x': 218.0, 'y': 0.1}],
}
out = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Resources', 'Maps', 'forest.json')
with open(out, 'w') as f:
    json.dump(data, f, indent=1)
print('\n'.join(data['rows']))
