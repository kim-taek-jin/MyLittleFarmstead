"""Render a labelled contact sheet of sprites at integer zoom for review."""
import sys
from PIL import Image, ImageDraw


def sheet(named_images, out, zoom=4, cols=8, bg=(74, 86, 70)):
    cell_w = max(im.width for _, im in named_images) * zoom + 12
    cell_h = max(im.height for _, im in named_images) * zoom + 24
    rows = (len(named_images) + cols - 1) // cols
    canvas = Image.new('RGB', (cols * cell_w, rows * cell_h), bg)
    d = ImageDraw.Draw(canvas)
    for i, (name, im) in enumerate(named_images):
        x = (i % cols) * cell_w + 6
        y = (i // cols) * cell_h + 4
        big = im.resize((im.width * zoom, im.height * zoom), Image.NEAREST)
        canvas.paste(big, (x, y), big)
        d.text((x, y + big.height + 2), name[:22], fill=(240, 240, 230))
    canvas.save(out)


if __name__ == '__main__':
    print('use from generate.py')
