"""Compose an in-game style mockup (Docs/pixelart_scene_mock.png) to judge the art in context."""
import os
import random
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from PIL import Image  # noqa: E402
import crops  # noqa: E402
import world  # noqa: E402
from pixel import P  # noqa: E402

ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))


def main():
    W, H = 16 * 32, 10 * 32
    im = Image.new('RGBA', (W, H), P['leaf'][2])
    r = random.Random(3)
    for _ in range(1400):
        im.putpixel((r.randrange(W), r.randrange(H)), P['leaf'][r.choice([1, 3])])
    dirt = crops.TILES['dirt']().image()
    till = crops.TILES['tilled']().image()
    wet = crops.TILES['tilled_wet']().image()
    for gx in range(3, 13):
        for gy in range(4, 9):
            im.paste(dirt, (gx * 32, gy * 32))
    plots = [(4, 5, 'carrot', 3, True), (5, 5, 'carrot', 2, False), (6, 5, 'carrot', 1, True),
             (4, 6, 'potato', 3, True), (5, 6, 'potato', 2, True), (6, 6, 'potato', 0, False),
             (8, 5, 'corn', 3, True), (9, 5, 'corn', 2, False), (10, 5, 'corn', 1, True),
             (8, 6, 'strawberry', 3, True), (9, 6, 'strawberry', 2, True), (10, 6, 'strawberry', 1, False),
             (11, 7, None, 0, False), (4, 7, None, 0, True)]
    for gx, gy, crop, stage, is_wet in plots:
        im.paste(wet if is_wet else till, (gx * 32, gy * 32))
        if crop:
            im.alpha_composite(crops.CROPS[crop][stage]().image(), (gx * 32, gy * 32))
    for x, y in [(20, 40), (60, 20), (450, 40)]:
        im.alpha_composite(world.tree().image(), (x, y))
    for x, y in [(420, 250), (20, 270)]:
        im.alpha_composite(world.rock().image(), (x, y))
    im.alpha_composite(world.apple_ground().image().resize((20, 20), Image.NEAREST), (110, 100))
    im.alpha_composite(world.well().image(), (300, 40))
    im.alpha_composite(world.storage_box().image(), (200, 70))
    im.alpha_composite(world.player_frames()['player_down_1'].image(), (232, 250))
    im.resize((W * 2, H * 2), Image.NEAREST).save(os.path.join(ROOT, 'Docs', 'pixelart_scene_mock.png'))


if __name__ == '__main__':
    main()
