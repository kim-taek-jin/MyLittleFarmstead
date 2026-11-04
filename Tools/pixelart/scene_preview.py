"""Preview a background with the scene's real objects composited at their world positions."""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
from PIL import Image
import backgrounds, crops, world, items

def place(img, bgobj, sprite, wx, wy, world_h):
    """Paste sprite centred at world (wx, wy) scaled to world height world_h (nearest)."""
    s = sprite.image() if hasattr(sprite, 'image') else sprite
    h = max(1, round(world_h * bgobj.sy)); w = max(1, round(s.width * h / s.height))
    s = s.resize((w, h), Image.NEAREST)
    x, y = bgobj.px(wx, wy)
    img.alpha_composite(s, (x - w // 2, y - h // 2))

def farm_preview(out):
    bgobj = backgrounds.BG(574, 328, 17.92, 10.24, (-0.03, -0.03))
    img = backgrounds.farm()
    dirt = crops.TILES['dirt']().image(); till = crops.TILES['tilled']().image(); wet = crops.TILES['tilled_wet']().image()
    cells = [(x, y) for y in (0, 1, 2) for x in range(-8, -1)] + [(x, y) for y in (-3, -2, -1) for x in range(-6, 0)]
    for (cx, cy) in cells:
        t = dirt
        if (cx, cy) in [(-7, 1), (-6, 1), (-5, 1), (-7, 2)]: t = till
        if (cx, cy) in [(-4, -2), (-3, -2)]: t = wet
        x, y = bgobj.px(cx, cy + 1)
        img.alpha_composite(t, (x, y))
    for (cx, cy, cid, st) in [(-7, 1, 'carrot', 3), (-6, 1, 'carrot', 2), (-4, -2, 'corn', 3), (-3, -2, 'strawberry', 2)]:
        x, y = bgobj.px(cx, cy + 1)
        img.alpha_composite(crops.CROPS[cid][st]().image(), (x, y))
    for (wx, wy) in [(4.78, 1.47), (2.53, 2.35), (3.41, 2.31), (5.08, 2.28), (4.21, 2.3)]:
        place(img, bgobj, world.tree(), wx, wy, 1.84)
    for (wx, wy) in [(4.12, 1.44), (4.94, -4.71), (-4.72, -2.53), (2.51, 1.5), (-3.49, -4.45), (3.38, 1.46)]:
        place(img, bgobj, world.rock(), wx, wy, 0.66)
    for (wx, wy) in [(-7.29, 3.72), (3.5, -1.24), (-8.29, -1.63), (8.23, -2.72), (7.97, 4.18), (-0.44, -4.18)]:
        place(img, bgobj, world.apple_ground(), wx, wy, 0.45)
    place(img, bgobj, world.well(), 3.68, -0.03, 2.3)
    place(img, bgobj, world.storage_box(), -3.89, -1.07, 0.92)
    place(img, bgobj, world.player_frames()['player_down_0'], 0.95, -2.2, 1.84)
    img.resize((img.width * 2, img.height * 2), Image.NEAREST).save(out)

if __name__ == '__main__':
    farm_preview(sys.argv[1])
