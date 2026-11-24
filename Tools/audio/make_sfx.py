"""Generate the game's small sound effects as 16-bit mono WAVs.

Run:  python3 Tools/audio/make_sfx.py
Out:  Assets/Resources/Audio/Sfx/*.wav   (loaded by Sfx.cs with Resources.Load)
Everything is synthesized here, so there are no license questions.
"""
import os, wave
import numpy as np

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Resources", "Audio", "Sfx")
rng = np.random.default_rng(7)


def t(d): return np.arange(int(SR * d)) / SR


def env(n, attack=0.005, decay=8.0):
    x = np.arange(n) / SR
    a = np.clip(x / max(attack, 1e-4), 0, 1)
    return a * np.exp(-x * decay)


def tone(freq, d, kind="sine", decay=8.0, attack=0.005):
    x = t(d)
    if np.isscalar(freq):
        f = freq
    else:
        f = np.asarray(freq)[: len(x)]
        if len(f) < len(x): f = np.concatenate([f, np.full(len(x) - len(f), f[-1])])
    ph = 2 * np.pi * np.cumsum(np.broadcast_to(f, x.shape)) / SR
    if kind == "square":
        w = np.sign(np.sin(ph)) * 0.5
    elif kind == "tri":
        w = 2 / np.pi * np.arcsin(np.sin(ph))
    else:
        w = np.sin(ph)
    return w * env(len(x), attack, decay)


def noise(d, decay=10.0, lp=0.2, attack=0.002):
    n = rng.uniform(-1, 1, int(SR * d))
    # one-pole low-pass: lp near 0 = darker
    y = np.zeros_like(n); acc = 0.0
    for i, v in enumerate(n):
        acc += lp * (v - acc); y[i] = acc
    y /= max(1e-6, np.abs(y).max())
    return y * env(len(n), attack, decay)


def seq(*parts, gap=0.0):
    out = []
    for p in parts:
        out.append(p); out.append(np.zeros(int(SR * gap)))
    return np.concatenate(out)


def mix(*xs):
    n = max(len(x) for x in xs)
    y = np.zeros(n)
    for x in xs: y[: len(x)] += x
    return y


def save(name, y, vol=0.8):
    y = y / max(1e-6, np.abs(y).max()) * vol
    fade = min(len(y), int(SR * 0.01))
    y[-fade:] *= np.linspace(1, 0, fade)
    data = (y * 32767).astype(np.int16)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR); w.writeframes(data.tobytes())


def sweep(f0, f1, d): return np.linspace(f0, f1, int(SR * d))


os.makedirs(OUT, exist_ok=True)
save("till", mix(noise(0.18, decay=22, lp=0.08), tone(sweep(140, 70, 0.15), 0.15, decay=25) * 0.8), 0.7)
save("water", mix(*[np.concatenate([np.zeros(int(SR * s)), tone(sweep(f, f * 1.8, 0.06), 0.06, decay=40) * 0.5])
                    for s, f in [(0, 500), (0.05, 650), (0.11, 560), (0.17, 720), (0.22, 610)]],
                  noise(0.35, decay=7, lp=0.35) * 0.35), 0.55)
save("plant", mix(tone(sweep(300, 520, 0.08), 0.1, decay=30), noise(0.06, decay=40, lp=0.1) * 0.4), 0.6)
save("harvest", seq(tone(sweep(400, 900, 0.05), 0.06, decay=30), tone(1046, 0.18, "tri", decay=14)), 0.65)
save("pickup", seq(tone(784, 0.06, "square", decay=25), tone(1175, 0.12, "square", decay=18)), 0.35)
save("coin", seq(tone(988, 0.07, "square", decay=20), tone(1319, 0.25, "square", decay=10)), 0.32)
save("heart", mix(*[np.concatenate([np.zeros(int(SR * i * 0.09)), tone(f, 0.6, "sine", decay=5)])
                    for i, f in enumerate([523, 659, 784, 1046])]), 0.5)
save("hit", mix(noise(0.12, decay=35, lp=0.15), tone(sweep(220, 110, 0.1), 0.1, decay=35)), 0.7)
save("swish", noise(0.22, decay=6, lp=0.5, attack=0.08), 0.45)
save("click", tone(1500, 0.03, "square", decay=120), 0.3)
save("step", noise(0.05, decay=60, lp=0.06), 0.35)
save("open", seq(tone(523, 0.05, "tri", decay=30), tone(784, 0.1, "tri", decay=20)), 0.45)
save("close", seq(tone(784, 0.05, "tri", decay=30), tone(523, 0.1, "tri", decay=20)), 0.45)
print("wrote", sorted(os.listdir(OUT)))
