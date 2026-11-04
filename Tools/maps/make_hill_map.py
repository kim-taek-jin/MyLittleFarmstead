"""Author Sunset Hill (30 x 20 tiles), opened by autumn of the heart: Assets/Resources/Maps/hill.json

You come in at the bottom-left from the farm's south lane (walking out west takes you back, so you never
bounce straight back through the gate you came from) and climb three terraces to the overlook at the top.
In Unity it lives inside the Farm scene too (x 310..340), like the forest and the lake.

  #  trees (blocking)    .  grass    =  path    C  terrace cliff (blocking)    S  stone stairs
  A / P / K  an old apple / pear / persimmon tree (its trunk blocks)   D  persimmon drying rack (blocking)
  V  the overlook bench (sit here: E)    R  wooden railing at the top (blocking)    B  bush (blocking)
"""
import json, os

W, H = 30, 20
OX, OY = 310, 10
g = [['.'] * W for _ in range(H)]


def put(c, r, ch):
    if 0 <= c < W and 0 <= r < H:
        g[r][c] = ch


# woods around the hill (ragged edges, so it doesn't look like a box)
for r in range(H):
    for c in range(W):
        edge = min(c, W - 1 - c)
        if edge < 2 or (edge < 3 and (r * 7 + c) % 4 == 0) or r == 0:
            put(c, r, '#')
for c in range(W):
    put(c, H - 1, '#')

# terrace cliffs with stairs (bottom terrace: rows 15..18, middle: 10..13, top: 5..8, summit: 1..3)
for (row, stairs) in [(14, (21, 22)), (9, (6, 7)), (4, (14, 15))]:
    for c in range(2, W - 2):
        put(c, row, 'S' if c in stairs else 'C')

# the path: in from the bottom edge, zig-zagging up the stairs to the overlook
def path_h(r, c0, c1):
    for c in range(min(c0, c1), max(c0, c1) + 1):
        for k in (0, 1):
            if g[r + k][c] in '.':
                put(c, r + k, '=')
def path_v(c, r0, r1):
    for r in range(min(r0, r1), max(r0, r1) + 1):
        for k in (0, 1):
            if g[r][c + k] in '.':
                put(c + k, r, '=')
path_h(15, 14, 22)
# the way in and out: along the bottom terrace to the west edge
for c in range(0, 16):
    put(c, 16, '='); put(c, 17, '=')
path_v(21, 10, 13)
path_h(10, 6, 22)
path_v(6, 5, 8)
path_h(5, 6, 15)
path_v(14, 1, 3)

# the orchard: two old trees on each terrace, and the drying rack by the persimmons
for (c, r, ch) in [(7, 18, 'A'), (18, 17, 'A'), (12, 12, 'P'), (24, 11, 'P'), (9, 7, 'K'), (21, 7, 'K')]:
    put(c, r, ch)
put(17, 7, 'D')

# the overlook at the top: a railing along the edge and a bench looking out
for c in range(6, 24):
    put(c, 1, 'R')
put(14, 2, 'V'); put(15, 2, 'V')
for (c, r) in [(4, 3), (25, 2), (3, 12), (26, 16), (4, 17)]:
    if g[r][c] == '.':
        put(c, r, 'B')

data = {
    'name': 'hill',
    'originX': OX, 'originY': OY, 'width': W, 'height': H,
    'rows': [''.join(row) for row in g],
    'exits': [
        {'to': 'Farm', 'x0': 310.0, 'y0': -8.0, 'x1': 310.6, 'y1': -6.0, 'spawnX': 1.0, 'spawnY': -13.4, 'locked': ''},
    ],
    'signs': [],
    'arrivals': [{'from': 'Farm', 'x': 312.6, 'y': -6.6}],
}
out = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Resources', 'Maps', 'hill.json')
with open(out, 'w') as f:
    json.dump(data, f, indent=1)
print('\n'.join(data['rows']))
