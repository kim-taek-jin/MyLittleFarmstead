"""Author the expanded farm map (36 x 20 tiles) as data: Assets/Resources/Maps/farm.json

One character per tile (1 tile = 1 world unit = 32 px). Column c -> world x = originX + c,
row r -> world cell y = originY - 1 - r (row 0 is the top). The old one-screen farm stays in the
north-west quarter (x -9..9, y -5..5) and is drawn by the art code; this file adds everything new.

  .  grass                 =  path   (lanes: west -> forest, south -> sunset hill, south-east -> lake)
  x  open soil (tillable)  g  weeds on soil      k  stump on soil      o  boulder on soil
  T  tree (resource)       r  rock (resource)    a  apple (resource)   *  flowers
  ~  keep (old farm area, drawn by the art code)
"""
import json, os

W, H = 36, 20
OX, OY = -9, 5
grid = [['.'] * W for _ in range(H)]

def put(c, r, ch):
    if 0 <= c < W and 0 <= r < H:
        grid[r][c] = ch

# old one-screen farm (north-west quarter) is kept as-is
for r in range(0, 10):
    for c in range(0, 18):
        grid[r][c] = '~'

# main road continues east to the market (cells y -5..-4 -> rows 8..9)
for c in range(18, 36):
    put(c, 8, '='); put(c, 9, '=')
# south lane from the road at x 0.35..1.55 (cols 9..10) down to the southern edge (railway, autumn)
for r in range(10, 20):
    put(9, r, '='); put(10, r, '=')
# east lane to the lake (summer): down from the road at cols 32..33, then east along rows 15..16 to the edge
for r in range(10, 17):
    put(32, r, '='); put(33, r, '=')
for c in range(32, 36):
    put(c, 15, '='); put(c, 16, '=')
# west lane to the forest (spring): row 13..14 from the south lane to the west edge
for c in range(0, 9):
    put(c, 13, '='); put(c, 14, '=')

# the overgrown plot (south-east): soil with weeds, stumps and boulders to clear
def h(c, r, k=0):
    v = (c * 73856093) ^ (r * 19349663) ^ (k * 83492791)
    return (v >> 3) % 100
for r in range(11, 18):
    for c in range(13, 31):
        roll = h(c, r)
        if roll < 52:
            ch = 'g'
        elif roll < 62:
            ch = 'k'
        elif roll < 70:
            ch = 'o'
        else:
            ch = 'x'
        put(c, r, ch)
# a cleared corner to start with, right off the road
for r in range(11, 13):
    for c in range(13, 16):
        put(c, r, 'x')

# east meadow: resource trees, rocks, apples and flowers
for (c, r, ch) in [(20, 1, 'T'), (22, 2, 'T'), (25, 1, 'T'), (29, 2, 'T'), (32, 1, 'T'), (27, 4, 'T'),
                   (21, 5, 'r'), (30, 5, 'r'), (34, 3, 'r'),
                   (24, 6, 'a'), (31, 6, 'a'), (19, 3, 'a'),
                   (23, 4, '*'), (28, 6, '*'), (33, 5, '*'), (26, 2, '*')]:
    put(c, r, ch)
# south-west field: trees, rocks, flowers
for (c, r, ch) in [(2, 11, 'T'), (5, 16, 'T'), (1, 18, 'T'), (7, 11, 'T'),
                   (4, 17, 'r'), (6, 12, 'r'), (3, 15, 'a'), (2, 16, '*'), (6, 17, '*'), (8, 16, '*')]:
    put(c, r, ch)
# south-east corner below the plot
for (c, r, ch) in [(34, 10, 'T'), (34, 18, 'T'), (32, 18, 'r'), (28, 18, '*'), (16, 18, '*'), (22, 18, 'a')]:
    put(c, r, ch)

data = {
    'name': 'farm',
    'originX': OX, 'originY': OY, 'width': W, 'height': H,
    'rows': [''.join(row) for row in grid],
    # exits: rectangles in world units; 'to' is a scene, 'locked' is the season of the heart that opens it
    # 'art' is drawn while locked, 'openArt' once the season has opened it
    'exits': [
        {'to': 'Market', 'x0': 26.3, 'y0': -5.2, 'x1': 27.0, 'y1': -3.8, 'spawnX': -7.6, 'spawnY': -3.6, 'locked': ''},
        {'to': 'Forest', 'x0': -9.0, 'y0': -10.2, 'x1': -8.4, 'y1': -8.8, 'spawnX': 0, 'spawnY': 0, 'locked': 'Spring',
         'art': 'thorns', 'openArt': 'flower_arch',
         'sign': "The forest path is overgrown with gray thorns. Maybe when spring comes in your heart..."},
        {'to': 'Lake', 'x0': 26.4, 'y0': -12.0, 'x1': 27.0, 'y1': -10.0, 'spawnX': 0, 'spawnY': 0, 'locked': 'Summer',
         'art': 'reeds_gray', 'openArt': 'reeds_open',
         'sign': "Tall gray reeds have closed over the path to the lake. They'll wake when summer comes to your heart."},
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
    # where players arrive from other scenes
    'arrivals': [
        {'from': 'Home', 'x': -7.2, 'y': -4.9},
        {'from': 'Market', 'x': 25.6, 'y': -4.6},
        {'from': 'MiniGame', 'x': 0.0, 'y': 3.8},
        {'from': 'Forest', 'x': -7.6, 'y': -9.1},
        {'from': 'Lake', 'x': 25.0, 'y': -10.9},
        {'from': 'Hill', 'x': 1.0, 'y': -13.4},
    ],
}
out = os.path.join(os.path.dirname(__file__), '..', '..', 'Assets', 'Resources', 'Maps', 'farm.json')
with open(out, 'w') as f:
    json.dump(data, f, indent=1)
print('\n'.join(data['rows']))
