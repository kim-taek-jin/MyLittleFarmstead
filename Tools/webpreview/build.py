"""Build the browser preview: inline every pixel-art PNG as a data URI into one HTML file.

Usage: python3 Tools/webpreview/build.py <output.html>
"""
import base64, glob, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
ART = os.path.join(ROOT, 'Assets', 'Resources', 'PixelArt')

assets = {}
for path in sorted(glob.glob(os.path.join(ART, '*', '*.png'))):
    key = os.path.relpath(path, ART).replace(os.sep, '/')[:-4]
    with open(path, 'rb') as f:
        assets[key] = 'data:image/png;base64,' + base64.b64encode(f.read()).decode()

with open(os.path.join(HERE, 'template.html'), encoding='utf-8') as f:
    html = f.read()
with open(os.path.join(HERE, 'game.js'), encoding='utf-8') as f:
    js = f.read()
with open(os.path.join(ROOT, 'Assets', 'Resources', 'Maps', 'farm.json'), encoding='utf-8') as f:
    farm_map = json.load(f)
js = js.replace('/*__ASSETS__*/{}', json.dumps(assets)).replace('/*__MAP__*/null', json.dumps(farm_map, separators=(',', ':')))
html = html.replace('/*__GAME__*/', js)
out = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, 'Docs', 'web_preview.html')
with open(out, 'w', encoding='utf-8') as f:
    f.write(html)
print(f'{len(assets)} images, {len(html) // 1024} KB -> {out}')
