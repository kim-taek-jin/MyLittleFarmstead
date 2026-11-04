"""Author the lake (30 x 20 tiles), opened by summer of the heart: Assets/Resources/Maps/lake.json

Same rules as the other maps. In Unity it lives inside the Farm scene too, east of the forest (x 250..280).
The lake starts gray and murky; releasing the fish you caught clears it, one zone at a time.

  #  trees (blocking)      ~  lake water (blocking)      ,  sand          .  grass
  =  path                  p  wooden pier (walkable, over water)         R  reeds (blocking)
  F  fishing spot          X  stone steps where you release fish        h  bench under the willow (rest)
  W  the willow's trunk (blocking)    f  forage spot    B  bush (blocking)
"""
import json, os

W, H = 30, 20
OX, OY = 250, 10
g = [['#'] * W for _ in range(H)]


def put(c, r, ch, over=None):
    if 0 <= c < W and 0 <= r < H and (over is None or g[r][c] in over):
        g[r][c] = ch


def blob(parts, ch, over=None):
    for r in range(H):
        for c in range(W):
            if any(((c - cx) / rx) ** 2 + ((r - cy) / ry) ** 2 <= 1 for cx, cy, rx, ry in parts):
                put(c, r, ch, over)


LAKE = [(17, 9, 7.5, 5.2), (10.5, 12.5, 3.2, 2.4), (23.5, 5.5, 3.6, 2.8)]   # centre, west cove, east cove
grow = lambda parts, d: [(cx, cy, rx + d, ry + d) for cx, cy, rx, ry in parts]

blob(grow(LAKE, 3.2), '.')          # meadow around the lake
blob(grow(LAKE, 1.0), ',')          # sandy shore
blob(LAKE, '~')                     # water

# path: in from the west edge (rows 9..10), along to the west shore
for c in range(0, 6):
    put(c, 9, '='); put(c, 10, '=')
for c in range(3, 8):
    for r in (9, 10):
        put(c, r, '=', over='#.,')
# the path runs on along the south shore to the pier
for c in range(6, 18):
    for r in (16, 17):
        put(c, r, '=', over='#.,')
for r in range(10, 17):
    put(5, r, '=', over='#.,'); put(6, r, '=', over='#.,')

# pier: from the south shore out into the lake
for r in range(10, 16):
    put(17, r, 'p')
put(17, 10, 'F')                      # fish at the end of the pier
# a second fishing spot on the east shore, steps for releasing fish on the west shore
put(26, 10, 'F', over=',')
put(7, 11, 'X', over=',~')

# the willow and its bench on the north shore
put(11, 2, 'W', over='.,#'); put(12, 3, 'h', over='.,')
for (c, r) in [(10, 2), (10, 3), (12, 2), (11, 1)]:
    put(c, r, '.', over='#')

# reeds on some shore cells (not next to the path, pier, steps or fishing spots), a few bushes
for r in range(H):
    for c in range(W):
        if g[r][c] != ',' or (c * 7 + r * 3) % 9:
            continue
        near = [g[rr][cc] for rr in range(max(0, r - 1), min(H, r + 2)) for cc in range(max(0, c - 1), min(W, c + 2))]
        if '~' in near and not any(ch in near for ch in '=pXFh'):
            g[r][c] = 'R'
for (c, r) in [(4, 5), (27, 14), (3, 14)]:
    put(c, r, 'B', over='.')
put(25, 10, 'F', over=',')

# forage spots
for (c, r) in [(5, 4), (7, 3), (4, 7), (8, 14), (3, 12), (14, 2), (17, 2), (20, 1), (25, 13), (26, 15),
               (22, 16), (28, 9), (27, 7), (9, 17), (13, 18)]:
    put(c, r, 'f', over='.,')

# a closed ring of trees around the edge, except the way in
for c in range(W):
    for r in (0, H - 1):
        put(c, r, '#')
for r in range(H):
    for c in (0, W - 1):
        if not (c == 0 and r in (9, 10)):
            put(c, r, '#')

data = {
    'name': 'lake',
    'originX': OX, 'originY': OY, 'width': W, 'height': H,
    'rows': [''.join(row) for row in g],
    'exits': [
        {'to': 'Farm', 'x0': 250.0, 'y0': -1.0, 'x1': 250.6, 'y1': 1.0, 'spawnX': 25.0, 'spawnY': -10.9, 'locked': ''},
    ],
    'signs': [],
    'arrivals': [{'from': 'Farm', 'x': 252.0, 'y': 0.1}],
    # the three parts of the lake that clear up in turn (world centre, mask size): west cove, middle, east cove
    'zones': [
        {'x': OX + 10.5 + 0.5, 'y': OY - 12.5 - 0.5, 'size': 's'},
        {'x': OX + 17 + 0.5, 'y': OY - 9 - 0.5, 'size': 'l'},
        {'x': OX + 23.5 + 0.5, 'y': OY - 5.5 - 0.5, 'size': 's'},
    ],
}
out = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Resources', 'Maps', 'lake.json')
with open(out, 'w') as f:
    json.dump(data, f, indent=1)
print('\n'.join(data['rows']))
