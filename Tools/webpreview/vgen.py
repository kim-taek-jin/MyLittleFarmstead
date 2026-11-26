"""Regenerate the VILLAGERS block in game.js from Assets/Script/VillagerDB.cs (English text only).

    python3 Tools/webpreview/vgen.py
"""
import json, os, re

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, '..', '..'))
SRC = open(os.path.join(ROOT, 'Assets', 'Script', 'VillagerDB.cs'), encoding='utf-8-sig').read()

STR = r'"((?:[^"\\]|\\.)*)"'
L_RE = re.compile(r'L\(\s*' + STR + r'\s*,\s*' + STR + r'\s*\)', re.S)


def unesc(s):
    return s.replace('\\"', '"').replace('\\n', '\n').replace('\\\\', '\\')


def braced(text, start):
    """Text inside the { } that opens at or after start."""
    i = text.index('{', start)
    depth = 0
    for j in range(i, len(text)):
        if text[j] == '{':
            depth += 1
        elif text[j] == '}':
            depth -= 1
            if depth == 0:
                return text[i + 1:j]
    raise ValueError('unbalanced')


def one(field, text):
    m = re.search(r'\b' + field + r'\s*=\s*L\(\s*' + STR, text, re.S)
    return unesc(m.group(1)) if m else None


def many(field, text):
    m = re.search(r'\b' + field + r'\s*=\s*new\s*\[\]', text)
    if not m:
        return []
    return [unesc(a) for a, _ in L_RE.findall(braced(text, m.end()))]


def vec(field, text):
    m = re.search(r'\b' + field + r'\s*=\s*new Vector2\(\s*([-\d.]+)f?\s*,\s*([-\d.]+)f?\s*\)', text)
    return [float(m.group(1)), float(m.group(2))] if m else None


def plain(field, text):
    m = re.search(r'\b' + field + r'\s*=\s*"([^"]*)"', text)
    return m.group(1) if m else None


def items(text):
    m = re.search(r'rewardItems\s*=\s*Items\(([^)]*)\)', text)
    if not m:
        return []
    parts = [p.strip() for p in m.group(1).split(',')]
    return [[parts[i].strip('"'), int(parts[i + 1])] for i in range(0, len(parts), 2)]


out = []
for block_start in [m.end() for m in re.finditer(r'All\.Add\(new VillagerDef', SRC)]:
    body = braced(SRC, block_start)
    head = body.split('chapters = new[]')[0]
    v = {
        'id': plain('id', head), 'name': one('name', head), 'scene': plain('scene', head).lower(),
        'x': vec('position', head)[0], 'y': vec('position', head)[1],
        'ex': (vec('eveningPosition', head) or [0, 0])[0], 'ey': (vec('eveningPosition', head) or [0, 0])[1],
        'place': one('eveningPlace', head),
        'intro': one('intro', head), 'gray': many('grayIdle', head), 'color': many('colorIdle', head),
        'season': many('seasonLines', head), 'restored': one('restored', head), 'masil': many('masilLines', head),
        'chapters': [],
    }
    chap_text = braced(body, body.index('chapters = new[]'))
    for piece in chap_text.split('new ChapterDef')[1:]:
        c = braced(piece, 0)
        ch = {'kind': re.search(r'ChapterKind\.(\w+)', c).group(1), 'invite': one('invite', c), 'scene': many('scene', c),
              'hint': one('hint', c), 'spot': plain('spot', c), 'steps': many('steps', c), 'need': plain('needItem', c),
              'reward': items(c), 'unlock': plain('unlock', c), 'unlockText': one('unlockText', c),
              'favor': plain('favor', c), 'favorText': one('favorText', c)}
        m = re.search(r'heart\s*=\s*([\d.]+)f', c)
        ch['heart'] = float(m.group(1)) if m else 0
        m = re.search(r'gold\s*=\s*(\d+)', c)
        ch['gold'] = int(m.group(1)) if m else 0
        v['chapters'].append({k: val for k, val in ch.items() if val not in (None, [], 0) or k == 'heart'})
    out.append(v)

js_path = os.path.join(HERE, 'game.js')
js = open(js_path, encoding='utf-8').read()
a = js.index('const VILLAGERS = [')
b = js.index('];  // generated from VillagerDB.cs by vgen.py', a)
js = js[:a] + 'const VILLAGERS = ' + json.dumps(out, indent=1, ensure_ascii=False)[:-1] + js[b:]
open(js_path, 'w', encoding='utf-8').write(js)
print(f'{len(out)} villagers, {sum(len(v["chapters"]) for v in out)} chapters')
