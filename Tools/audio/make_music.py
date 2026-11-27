"""Gugak-style music for the winter demo (S-2), written note by note and synthesized here.

Run:  python3 Tools/audio/make_music.py
Out:  Assets/Resources/Audio/Music/*.ogg   (MusicManager.cs)
      Tools/webpreview/audio/mus_*.mp3     small copies for the browser preview

Modes: pyeongjo (평조, bright: Eb F G Bb C) and gyemyeonjo (계면조, plaintive, on C: C Eb F G Bb,
the fifth shaken (떠는 소리), the third bent down into the tonic (꺾는 소리)).
Rhythms (장단): gutgeori (굿거리, 12/8, swaying), semachi (세마치, 9/8, light), jajinmori (자진모리, fast 12/8, for the pungmul band).

  title       gayageum alone, then the daegeum joins (gyemyeonjo, free and slow)
  farm_0/1/2  one piece in three layers that play together. MusicManager raises layers as the heart grows:
                0 gayageum (always), 1 daegeum + saenghwang, 2 janggu + buk (gutgeori)
  market_0/1  semachi tune; layer 1 (janggu + kkwaenggwari) on market days
  boreum      the pungmul band at the moon house fire (jajinmori)
  spring      the end of the demo: winter lets go (pyeongjo, gutgeori)
"""
import os

import numpy as np

from dsp import SR, place, wrap_tail, loop_reverb, normalize, save_ogg, to_mp3, lowpass, peak, write_audio_meta
import instruments as I

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, '..', '..')
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Audio', 'Music')
WEB = os.path.join(ROOT, 'Tools', 'webpreview', 'audio')


# ---------------------------------------------------------------- helpers
def bars(*bs):
    out = []
    for b in bs:
        out.extend(b)
    return out


def line(notes, unit, inst, n, start=0.0, lag=0.0, vel=0.8, octave=0, humanize=0.012, seed=0):
    """Render a list of (note, units[, tech[, vel]]) into an array of n samples."""
    r = np.random.default_rng(seed)
    y = np.zeros(n + SR * 4)
    at = start
    for item in notes:
        name, units = item[0], item[1]
        tech = item[2] if len(item) > 2 else None
        v = item[3] if len(item) > 3 else vel
        d = units * unit
        if name != 'r':
            f = I.hz(name) * 2 ** octave
            when = at + lag + r.uniform(-humanize, humanize)
            y = place(y, inst(f, d, v * r.uniform(0.92, 1.05), tech), max(0.0, when))
        at += d
    return y


def heterophony(notes, min_units=3):
    """The daegeum follows the same tune, holding through the quick notes (Korean ensembles play one
    melody together, each instrument in its own way)."""
    out = []
    for item in notes:
        name, units = item[0], item[1]
        if out and (units < min_units or name == 'r'):
            prev = list(out[-1])
            prev[1] += units
            out[-1] = tuple(prev)
        else:
            out.append((name, units, 'vib'))
    return out


def roots(per_bar, bar_units, unit, n, vel=0.5, seed=1):
    """Low gayageum strings: the bar's root on the downbeat and its fifth halfway."""
    notes = []
    for r, f in per_bar:
        h = bar_units // 2
        notes += [(r, h, None, vel), (f, bar_units - h, None, vel * 0.7)]
    return line(notes, unit, I.gayageum, n, seed=seed)


def drums(pattern, bars_n, bar_units, unit, n, vel=0.6, seed=2, start_bar=0):
    """pattern: list of (unit position, stroke, relative velocity). stroke: kung/deok/deong/gi/buk/kkwaeng/jit/jing."""
    r = np.random.default_rng(seed)
    y = np.zeros(n + SR * 5)
    for b in range(start_bar, bars_n):
        for pos, stroke, v in pattern:
            at = (b * bar_units + pos) * unit + r.uniform(-0.006, 0.006)
            vv = vel * v * r.uniform(0.9, 1.05)
            if stroke in ('kung', 'deok', 'deong', 'gi'):
                s = I.janggu(stroke, vv)
            elif stroke == 'buk':
                s = I.buk(vv)
            elif stroke == 'kkwaeng':
                s = I.kkwaenggwari(vv)
            elif stroke == 'jit':
                s = I.kkwaenggwari(vv, muted=True)
            elif stroke == 'jing':
                s = I.jing(vv)
            else:
                raise ValueError(stroke)
            y = place(y, s, max(0.0, at))
    return y


def pan(y, p):
    """p: -1 left .. 1 right (constant power)."""
    a = (p + 1) * np.pi / 4
    return y * np.cos(a), y * np.sin(a)


def finish(parts, n, wet=0.22, room=2.2, bright=5200, gain=0.85):
    """parts: list of (mono, pan). Loop-wrap, stereo, roomy reverb that wraps round the loop."""
    L = np.zeros(n)
    R = np.zeros(n)
    for y, p in parts:
        y = wrap_tail(y, n)
        l, r = pan(y, p)
        L += l
        R += r
    L = loop_reverb(L, wet, room, 2.6, bright, seed=1)
    R = loop_reverb(R, wet, room, 2.6, bright, seed=2)
    L = peak(L, 3200, -2.5, 0.8)        # take the edge off the bright strings a little
    R = peak(R, 3200, -2.5, 0.8)
    m = max(np.abs(L).max(), np.abs(R).max(), 1e-9)
    return L / m * gain, R / m * gain


TRACKS = {}


def keep(name, LR, rms):
    """Set every track to a planned loudness so layers sit right when they play together."""
    L, R = LR
    now = np.sqrt((np.mean(L ** 2) + np.mean(R ** 2)) / 2)
    k = rms / max(now, 1e-9)
    k = min(k, 0.97 / max(np.abs(L).max(), np.abs(R).max(), 1e-9))   # never clip
    TRACKS[name] = (L * k, R * k)


# ================================================================ title (gyemyeonjo on C, slow 12/8)
U = 60 / (44 * 3)                      # one eighth at dotted-quarter = 44
BAR = 12
title_mel = bars(
    [('G4', 6, 'vibx'), ('Eb4', 3, 'down'), ('C4', 3)],
    [('C4', 9, 'vib'), ('r', 3)],
    [('Eb4', 3), ('F4', 3), ('G4', 6, 'vibx')],
    [('Bb4', 3), ('G4', 3), ('F4', 3), ('Eb4', 3, 'down')],
    [('C4', 12, 'vib')],
    [('C5', 6, 'vibx'), ('Bb4', 3), ('G4', 3)],
    [('F4', 3), ('G4', 3), ('Eb4', 6, 'down')],
    [('C4', 6, 'vib'), ('Eb4', 3), ('F4', 3)],
    [('G4', 6, 'vibx'), ('Eb4', 3, 'down'), ('C4', 3)],
    [('C4', 12, 'vib')],
)
nb = 10
n = int(nb * BAR * U * SR)
gaya = line(title_mel, U, I.gayageum, n, vel=0.75, seed=3)
low = roots([('C3', 'G3')] * 2 + [('F3', 'C4'), ('Bb2', 'F3'), ('C3', 'G3'), ('Ab2', 'Eb3'), ('F3', 'C4'), ('C3', 'G3'), ('G2', 'D3'), ('C3', 'G3')], BAR, U, n, 0.35, 4)
# the daegeum joins at bar 6 (the first five bars are 13 notes, 60 eighths)
dae = line(heterophony(title_mel[13:]), U, I.daegeum, n, start=60 * U, lag=0.09, vel=0.6, seed=5)
keep('title', finish([(gaya, -0.25), (low, -0.35), (dae, 0.35)], n, wet=0.3, room=2.8), 0.085)

# ================================================================ farm (pyeongjo on Eb, gutgeori)
U = 60 / (56 * 3)
farm_mel = bars(
    [('Bb4', 6, 'vib'), ('G4', 3), ('F4', 3)],
    [('Eb4', 9, 'vib'), ('r', 3)],
    [('F4', 3), ('G4', 3), ('Bb4', 3), ('C5', 3)],
    [('Bb4', 6, 'up'), ('G4', 6)],
    [('C5', 6, 'vib'), ('Bb4', 3), ('G4', 3)],
    [('F4', 3), ('G4', 3), ('F4', 6, 'vib')],
    [('Eb4', 3), ('F4', 3), ('G4', 3), ('F4', 3)],
    [('Eb4', 12, 'vib')],
    [('Eb5', 6, 'vib'), ('C5', 3), ('Bb4', 3)],
    [('C5', 3), ('Eb5', 3), ('F5', 6, 'vibx')],
    [('Eb5', 3), ('C5', 3), ('Bb4', 6)],
    [('G4', 6), ('Bb4', 6, 'vib')],
    [('C5', 6), ('Bb4', 3), ('G4', 3)],
    [('F4', 6, 'vib'), ('G4', 3), ('F4', 3)],
    [('Eb4', 3), ('C4', 3), ('Eb4', 3), ('F4', 3)],
    [('Eb4', 12, 'vib')],
)
nb = 16
n = int(nb * 12 * U * SR)
farm_roots = [('Eb3', 'Bb3'), ('Eb3', 'Bb3'), ('F3', 'C4'), ('Eb3', 'Bb3'), ('C3', 'G3'), ('F3', 'C4'), ('Eb3', 'Bb3'), ('Eb3', 'Bb3'),
              ('C3', 'G3'), ('F3', 'C4'), ('C3', 'G3'), ('Eb3', 'Bb3'), ('C3', 'G3'), ('F3', 'C4'), ('C3', 'G3'), ('Eb3', 'Bb3')]
g0 = line(farm_mel, U, I.gayageum, n, vel=0.72, seed=6)
r0 = roots(farm_roots, 12, U, n, 0.32, 7)
keep('farm_0', finish([(g0, -0.2), (r0, -0.35)], n, wet=0.26), 0.075)
d1 = line(heterophony(farm_mel), U, I.daegeum, n, lag=0.1, vel=0.55, seed=8)
drone = np.zeros(n + SR * 4)
for b, (rt, fi) in enumerate(farm_roots):
    if b % 2 == 0:
        drone = place(drone, I.saenghwang([I.hz(rt) * 2, I.hz(fi) * 2], 24 * U, 0.3), b * 12 * U)
keep('farm_1', finish([(d1, 0.3), (drone, 0.0)], n, wet=0.3), 0.045)
gut = [(0, 'deong', 1.0), (2, 'gi', 0.6), (2.5, 'deok', 0.7), (3, 'kung', 0.8), (5, 'deok', 0.35), (5.25, 'deok', 0.3), (5.5, 'deok', 0.3), (5.75, 'deok', 0.35),
       (6, 'kung', 0.8), (8, 'gi', 0.6), (8.5, 'deok', 0.7), (9, 'kung', 0.8), (11, 'deok', 0.35), (11.25, 'deok', 0.3), (11.5, 'deok', 0.3), (11.75, 'deok', 0.35)]
jg = drums(gut, nb, 12, U, n, 0.45, 9)
bk = drums([(0, 'buk', 0.7)], nb, 12, U, n, 0.5, 10)
keep('farm_2', finish([(jg, 0.15), (bk, -0.1)], n, wet=0.18), 0.035)

# ================================================================ market (pyeongjo, semachi 9/8, livelier)
U = 60 / (84 * 3)
mk = bars(
    [('Bb4', 3), ('C5', 3), ('Bb4', 3)], [('G4', 6), ('F4', 3)], [('G4', 3), ('Bb4', 3), ('C5', 3)], [('Eb5', 9, 'vib')],
    [('C5', 3), ('Eb5', 3), ('C5', 3)], [('Bb4', 6), ('G4', 3)], [('F4', 3), ('G4', 3), ('Bb4', 3)], [('G4', 9, 'vib')],
    [('Eb5', 3), ('F5', 3), ('Eb5', 3)], [('C5', 6), ('Bb4', 3)], [('C5', 3), ('Bb4', 3), ('G4', 3)], [('F4', 9, 'vib')],
    [('G4', 3), ('F4', 3), ('Eb4', 3)], [('F4', 6), ('G4', 3)], [('Bb4', 3), ('G4', 3), ('F4', 3)], [('Eb4', 9, 'vib')],
)
mk_all = mk + mk
nb = 32
n = int(nb * 9 * U * SR)
mroots = [('Eb3', 'Bb3'), ('Eb3', 'Bb3'), ('C3', 'G3'), ('Eb3', 'Bb3'), ('C3', 'G3'), ('Eb3', 'Bb3'), ('F3', 'C4'), ('Eb3', 'Bb3'),
          ('C3', 'G3'), ('F3', 'C4'), ('C3', 'G3'), ('F3', 'C4'), ('Eb3', 'Bb3'), ('F3', 'C4'), ('F3', 'C4'), ('Eb3', 'Bb3')] * 2
mg = line(mk_all, U, I.gayageum, n, vel=0.7, seed=11)
mr = roots(mroots, 9, U, n, 0.3, 12)
md = line(heterophony(mk), U, I.daegeum, n, start=16 * 9 * U, lag=0.07, vel=0.58, seed=13, octave=0)   # the daegeum takes the second time round
keep('market_0', finish([(mg, -0.25), (mr, -0.35), (md, 0.3)], n, wet=0.2), 0.08)
semachi = [(0, 'deong', 1.0), (3, 'kung', 0.8), (5, 'deok', 0.7), (6, 'kung', 0.75), (7, 'deok', 0.7)]
mj = drums(semachi, nb, 9, U, n, 0.5, 14)
mkk = drums([(0, 'kkwaeng', 0.8), (3, 'jit', 0.5), (6, 'jit', 0.5)], nb, 9, U, n, 0.45, 15)
keep('market_1', finish([(mj, 0.15), (mkk, 0.4)], n, wet=0.16), 0.04)

# ================================================================ boreum: the pungmul band (jajinmori)
U = 60 / (100 * 3)
nb = 10
n = int(nb * 12 * U * SR)
jajin_jg = [(0, 'deong', 1.0), (2, 'deok', 0.7), (3, 'kung', 0.85), (4, 'deok', 0.7), (6, 'kung', 0.85), (8, 'deok', 0.7), (9, 'kung', 0.85), (10, 'deok', 0.7)]
jajin_kk = [(0, 'kkwaeng', 1.0), (2, 'kkwaeng', 0.6), (3, 'kkwaeng', 0.8), (4, 'jit', 0.6), (6, 'kkwaeng', 0.85), (8, 'kkwaeng', 0.6), (9, 'kkwaeng', 0.8), (10, 'jit', 0.6)]
bj = drums(jajin_jg, nb, 12, U, n, 0.6, 16)
bkk = drums(jajin_kk, nb, 12, U, n, 0.55, 17)
bb = drums([(0, 'buk', 1.0), (3, 'buk', 0.6), (6, 'buk', 0.8), (9, 'buk', 0.6)], nb, 12, U, n, 0.6, 18)
bjing = drums([(0, 'jing', 1.0)], nb, 12, U, n, 0.55, 19)
keep('boreum', finish([(bj, -0.2), (bkk, 0.35), (bb, 0.0), (bjing, -0.05)], n, wet=0.22, room=2.0, bright=7000), 0.09)

# ================================================================ spring (end of the demo)
U = 60 / (60 * 3)
sp = bars(
    [('Eb4', 3), ('F4', 3), ('G4', 3), ('Bb4', 3)],
    [('C5', 6, 'vib'), ('Bb4', 3), ('C5', 3)],
    [('Eb5', 6, 'vibx'), ('C5', 3), ('Bb4', 3)],
    [('G4', 9, 'vib'), ('r', 3)],
    [('Bb4', 3), ('C5', 3), ('Eb5', 3), ('F5', 3)],
    [('G5', 6, 'vibx'), ('F5', 3), ('Eb5', 3)],
    [('C5', 3), ('Bb4', 3), ('G4', 3), ('F4', 3)],
    [('Eb4', 12, 'vib')],
)
nb = 8
n = int(nb * 12 * U * SR)
sg = line(sp, U, I.gayageum, n, vel=0.75, seed=20)
sr_ = roots([('Eb3', 'Bb3'), ('F3', 'C4'), ('C3', 'G3'), ('Eb3', 'Bb3'), ('Eb3', 'Bb3'), ('C3', 'G3'), ('F3', 'C4'), ('Eb3', 'Bb3')], 12, U, n, 0.32, 21)
sd = line(heterophony(sp), U, I.daegeum, n, lag=0.09, vel=0.6, seed=22, octave=0)
sj = drums(gut, nb, 12, U, n, 0.35, 23)
keep('spring', finish([(sg, -0.25), (sr_, -0.35), (sd, 0.3), (sj, 0.1)], n, wet=0.26), 0.085)

# ---------------------------------------------------------------- write
os.makedirs(OUT, exist_ok=True)
os.makedirs(WEB, exist_ok=True)
for name, (L, R) in TRACKS.items():
    path = os.path.join(OUT, name + '.ogg')
    save_ogg(path, stereo=(L, R), quality=4)
    write_audio_meta(path, streaming=True)
    to_mp3(path, os.path.join(WEB, 'mus_' + name + '.mp3'), 56)
    print(f'{name}: {len(L) / SR:.1f}s')
