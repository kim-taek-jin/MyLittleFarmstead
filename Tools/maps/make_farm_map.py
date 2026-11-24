"""Author the home farm map (36 x 20 tiles) as data: Assets/Resources/Maps/farm.json

A Korean countryside homestead (direction C). One character per tile (1 tile = 1 world unit = 32 px).
Column c -> world x = originX + c, row r -> world cell y = originY - 1 - r (row 0 is the top).
The same file is read by the game (MapData.cs / FarmMap.cs) and by the art (Tools/pixelart/farmmap.py),
so walls in the game and walls in the picture always line up.

  walkable:  .  grass   =  lane   y  yard (packed earth)   s  stepping stone   p  plot (ridges)   S  harvested paddy
             *  flowers
  blocked:   P  pine hill   w  stream   W  stone wall   H  house   J  jang-dok-dae (sauce jars)
             K  persimmon tree   B  pyeong-sang (low wooden platform)   C  compost heap   R  rice paddy
             b  straw bale   |  utility pole
  resources: T  tree   r  rock   a  apple            (same gathering rules as before)
"""
import json, os

W, H = 36, 20
OX, OY = -9, 5
grid = [['.'] * W for _ in range(H)]


def put(c, r, ch):
    if 0 <= c < W and 0 <= r < H:
        grid[r][c] = ch


def fill(c0, r0, c1, r1, ch):
    for r in range(r0, r1 + 1):
        for c in range(c0, c1 + 1):
            put(c, r, ch)


def col(x):           # world x -> column
    return x - OX


def row(y):           # world cell y -> row
    return OY - 1 - y


# --- back hill: red pines along the top, a path up to the village behind (haggle gate) ---
fill(0, 0, 35, 1, 'P')
fill(8, 0, 9, 1, '=')

# --- stream down the west side (with stepping stones on the forest path) ---
stream = {r: c for r, c in enumerate([1, 1, 2, 2, 2, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2])}
for r, c in stream.items():
    put(c, r, 'w'); put(c + 1, r, 'w')
for r in (13, 14):
    c = stream[r]
    put(c, r, 's'); put(c + 1, r, 's')

# --- the yard inside a stone wall (cols 4..17, rows 2..7), gate at the bottom ---
fill(4, 2, 17, 7, 'W')
fill(5, 3, 16, 6, 'y')
fill(10, 7, 11, 7, 'y')            # front gate
fill(8, 2, 9, 2, 'y')              # back gate (path up the hill to the village behind)
put(4, 5, 'y')                     # side gate toward the stream
fill(10, 2, 14, 4, 'H')            # house against the back wall
put(5, 3, 'K'); put(5, 4, 'K')     # persimmon tree in the corner (trunk + the cell under the canopy)
fill(6, 6, 7, 6, 'B')              # pyeong-sang under the tree
fill(15, 3, 16, 4, 'J')            # jang-dok-dae beside the house

# --- vegetable plots (ridges) east of the yard, different sizes ---
plots = [
    # id,      cols,      rows,   start open
    ('front', (19, 24), (2, 4), True),
    ('long',  (26, 32), (2, 5), False),
    ('small', (20, 25), (6, 7), False),
    ('corner', (28, 32), (7, 7), False),
]
for _, (c0, c1), (r0, r1), _ in plots:
    fill(c0, r0, c1, r1, 'p')
put(18, 3, 'C')                    # compost heap between the yard wall and the plots
put(18, 4, '.')

# --- village lane: from the gate east to the market, south between the paddies to the hill ---
fill(10, 8, 35, 9, '=')
fill(9, 8, 9, 9, '=')
fill(9, 10, 10, 19, '=')
# lake lane (east), forest path (west, over the stepping stones)
fill(33, 10, 34, 16, '=')
fill(33, 15, 35, 16, '=')
for c in range(0, 9):
    for r in (13, 14):
        if grid[r][c] not in ('s',):
            put(c, r, '=')

# --- paddies along the bottom: rice, one harvested field with bales ---
fill(4, 15, 8, 19, 'R')
fill(11, 12, 20, 19, 'R')
fill(22, 12, 32, 15, 'S')
fill(22, 17, 32, 19, 'R')
for (c, r) in ((24, 13), (27, 12), (29, 14), (31, 13)):
    put(c, r, 'b')

# --- poles along the north side of the lane ---
put(18, 7, '|'); put(27, 7, '.')
put(34, 7, '|')

# --- meadow: resources and flowers (rows 10..11, west strip, north of the paddies) ---
for (c, r, ch) in [
    (13, 10, 'T'), (17, 11, 'T'), (24, 10, 'T'), (29, 11, 'T'), (31, 10, 'r'), (21, 11, 'r'), (15, 11, 'a'), (27, 10, 'a'),
    (12, 11, '*'), (19, 10, '*'), (26, 11, '*'), (35, 11, '*'),
    (0, 3, 'T'), (14, 11, 'T'), (0, 9, 'r'), (0, 11, 'a'), (0, 17, 'T'), (0, 15, 'r'), (0, 19, '*'), (3, 6, '*'),
    (5, 9, 'T'), (7, 11, 'r'), (6, 12, 'a'), (4, 10, '*'),
    (35, 4, 'T'), (33, 6, '*'),
]:
    put(c, r, ch)

data = {
    'name': 'farm',
    'originX': OX, 'originY': OY, 'width': W, 'height': H,
    'rows': [''.join(r) for r in grid],
    'plots': [
        {'id': pid, 'x0': c0 + OX, 'x1': c1 + OX, 'y1': OY - 1 - r0, 'y0': OY - 1 - r1, 'startOpen': start}
        for pid, (c0, c1), (r0, r1), start in plots
    ],
    # things that live in the Farm scene and are moved here at runtime (scene file untouched)
    'anchors': [
        {'name': 'door', 'x': 3.5, 'y': -0.15},        # HomePortal (the house door, bottom middle of the house)
        {'name': 'well', 'x': 6.9, 'y': -1.4},         # well_0 (yard, right side)
        {'name': 'storage', 'x': 0.4, 'y': 0.5},       # storage chest by the porch
        {'name': 'compost', 'x': 9.5, 'y': 1.5},       # compost heap
        {'name': 'pond', 'x': -6.6, 'y': -1.5},        # fishing spot: the stream by the side gate
        {'name': 'mina', 'x': -3.6, 'y': -0.6},        # Mina sits on the pyeong-sang... next to it
        {'name': 'starvine', 'x': 19.5, 'y': -6.4},    # star vine (Star Hill) at the meadow edge
        {'name': 'start', 'x': 2.0, 'y': -1.8},        # new game / unsafe position fallback
        {'name': 'cottage', 'x': 3.5, 'y': 0.0},       # bottom middle of the house picture (ending recolor)
    ],
    'exits': [
        {'to': 'Market', 'x0': 26.3, 'y0': -5.2, 'x1': 27.0, 'y1': -3.8, 'spawnX': -7.6, 'spawnY': -3.6, 'locked': ''},
        {'to': 'Forest', 'x0': -9.0, 'y0': -10.2, 'x1': -8.4, 'y1': -8.8, 'spawnX': 0, 'spawnY': 0, 'locked': 'Spring',
         'art': 'thorns', 'openArt': 'flower_arch',
         'sign': "The path up the back hill is overgrown with gray thorns. Maybe when spring comes in your heart..."},
        {'to': 'Lake', 'x0': 26.4, 'y0': -12.0, 'x1': 27.0, 'y1': -10.0, 'spawnX': 0, 'spawnY': 0, 'locked': 'Summer',
         'art': 'reeds_gray', 'openArt': 'reeds_open',
         'sign': "Tall gray reeds have closed over the path to the reservoir. They'll wake when summer comes to your heart."},
        {'to': 'Hill', 'x0': 0.2, 'y0': -15.0, 'x1': 1.8, 'y1': -14.4, 'spawnX': 0, 'spawnY': 0, 'locked': 'Autumn',
         'art': 'thorns', 'openArt': 'maple_arch',
         'sign': "The hill path is buried in gray thorns. Maybe when autumn comes to your heart..."},
    ],
    'signs': [
        {'x': -7.6, 'y': -8.0, 'text': 'Forest'},
        {'x': 2.6, 'y': -13.6, 'text': 'Sunset Hill'},
        {'x': 24.8, 'y': -3.2, 'text': 'Market'},
        {'x': 25.6, 'y': -9.4, 'text': 'Lake'},
    ],
    'arrivals': [
        {'from': 'Home', 'x': 3.5, 'y': -1.0},
        {'from': 'Market', 'x': 25.6, 'y': -4.6},
        {'from': 'MiniGame', 'x': 0.0, 'y': 2.9},
        {'from': 'Forest', 'x': -7.6, 'y': -9.1},
        {'from': 'Lake', 'x': 25.0, 'y': -10.9},
        {'from': 'Hill', 'x': 1.0, 'y': -13.4},
    ],
}
out = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Resources', 'Maps', 'farm.json')
with open(out, 'w') as f:
    json.dump(data, f, indent=1)
print('\n'.join(data['rows']))
