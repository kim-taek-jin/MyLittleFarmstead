"""The game's sound effects and outdoor sounds (S-1), all synthesized here: no license questions.

Run:  python3 Tools/audio/make_sfx.py
Out:  Assets/Resources/Audio/Sfx/*.wav      short effects (Sfx.Play("name"))
      Assets/Resources/Audio/Ambience/*.ogg looping outdoor sounds (Ambience.cs)
      Tools/webpreview/audio/*.mp3          small copies for the browser preview
"""
import os

import numpy as np

from dsp import (SR, t, env_ad, env_asr, phase, bandpass, lowpass, highpass, peak, noise, burst, place, mix,
                 reverb, loop_reverb, normalize, fade_edges, save_wav, save_ogg, to_mp3, wrap_tail, write_audio_meta)
import instruments as I

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.join(HERE, '..', '..')
OUT = os.path.join(ROOT, 'Assets', 'Resources', 'Audio', 'Sfx')
AMB = os.path.join(ROOT, 'Assets', 'Resources', 'Audio', 'Ambience')
WEB = os.path.join(ROOT, 'Tools', 'webpreview', 'audio')
rng = np.random.default_rng(11)
SFX = {}


def sfx(name, y, vol=0.8, verb=0.0):
    if verb > 0:
        y = reverb(y, verb, 1.2, 4.5, 5000)
    SFX[name] = fade_edges(normalize(y, vol), 3)


def grains(d, count, fc, q=0.8, peak_at=0.3, seed=0, click=0.0006):
    """Many tiny clicks (snow crunch, soil scatter, chewing)."""
    r = np.random.default_rng(seed)
    y = np.zeros(int(SR * d))
    pos = np.clip(r.normal(peak_at, 0.28, count), 0, 0.98) * d
    for p in pos:
        k = int(p * SR)
        c = r.uniform(-1, 1, int(SR * click)) * r.uniform(0.3, 1.0)
        y[k:k + len(c)] += c[: len(y) - k]
    return bandpass(y, fc, q)


def thud(d=0.12, fc=320, decay=40.0, seed=0):
    return lowpass(noise(d, seed=seed), fc, 2) * env_ad(int(SR * d), 0.002, decay)


def ring(freqs_amps, d, decay, attack=0.0008):
    n = int(SR * d)
    y = np.zeros(n)
    for f, a in freqs_amps:
        y += a * np.sin(phase(f, n)) * env_ad(n, attack, decay * (1 + f / 8000))
    return y


# ---------------------------------------------------------------- footsteps
sfx('step_snow', mix(grains(0.16, 70, 2600, 0.7, 0.35, 1), thud(0.12, 260, 30, 2) * 0.6), 0.45)   # 뽀득
sfx('step_dirt', mix(thud(0.1, 420, 45, 3), grains(0.1, 14, 1500, 1.0, 0.2, 4) * 0.4), 0.35)
sfx('step_floor', mix(thud(0.08, 300, 60, 5), burst(0.06, 900, 1.2, 70, seed=6) * 0.25), 0.28)        # 장판
SFX['step'] = SFX['step_dirt']

# ---------------------------------------------------------------- farm work
sfx('till', mix(thud(0.2, 240, 22, 7), burst(0.25, 1200, 0.9, 18, seed=8) * 0.5,
                place(np.zeros(1), grains(0.25, 30, 2200, 0.8, 0.3, 9) * 0.5, 0.05)), 0.7)
drops = np.zeros(int(SR * 0.6))
for i in range(12):
    at = rng.uniform(0.02, 0.5)
    f0 = rng.uniform(900, 1600)
    n = int(SR * 0.04)
    d = np.sin(phase(np.linspace(f0, f0 * 1.9, n), n)) * env_ad(n, 0.001, 70)
    drops = place(drops, d * rng.uniform(0.2, 0.5), at)
pour = lowpass(bandpass(noise(0.6, seed=12), 2200, 0.9), 5000) * env_asr(int(SR * 0.6), 0.04, 0.2) * (0.6 + 0.4 * np.abs(np.sin(t(0.6) * 37)))
sfx('water', mix(pour * 0.6, drops), 0.5)
sfx('plant', mix(thud(0.1, 380, 40, 13), place(np.zeros(1), thud(0.1, 380, 45, 14) * 0.7, 0.13)), 0.55)
rustle = highpass(noise(0.18, seed=15), 3000) * env_asr(int(SR * 0.18), 0.02, 0.1) * 0.4
pop = np.sin(phase(np.linspace(280, 640, int(SR * 0.07)), int(SR * 0.07))) * env_ad(int(SR * 0.07), 0.002, 40)
ding = I.gayageum(I.hz('Bb5'), 0.3, 0.6)
sfx('harvest', mix(rustle, place(np.zeros(1), pop, 0.12), place(np.zeros(1), ding * 0.7, 0.16)), 0.62, 0.15)
sfx('pickup', mix(I.gayageum(I.hz('F5'), 0.12, 0.6), place(np.zeros(1), I.gayageum(I.hz('Bb5'), 0.3, 0.55), 0.07)), 0.45, 0.12)
coin = lambda: ring([(2300, 1), (3150, 0.7), (4480, 0.5), (6100, 0.3)], 0.45, 9)
sfx('coin', mix(coin(), place(np.zeros(1), coin() * 0.8, 0.07)), 0.32)
arp = np.zeros(1)
for i, nm in enumerate(['Eb5', 'F5', 'G5', 'Bb5', 'C6']):
    arp = place(arp, I.gayageum(I.hz(nm), 0.5, 0.55 + i * 0.05, 'vib' if i == 4 else None), i * 0.085)
sfx('heart', arp, 0.5, 0.3)
sfx('hit', mix(burst(0.15, 1800, 1.0, 40, seed=16), ring([(180, 1), (410, 0.4)], 0.15, 30) * 0.6), 0.65)
sw = highpass(noise(0.28, seed=17), 700) * env_asr(int(SR * 0.28), 0.09, 0.15)
sfx('swish', bandpass(sw, 1600, 0.5), 0.45)
sfx('click', ring([(1700, 1), (880, 0.5)], 0.05, 110), 0.3)                            # a small wooden tick
paper = lambda d, s: highpass(noise(d, seed=s), 2500) * env_asr(int(SR * d), d * 0.4, d * 0.5)
sfx('open', mix(paper(0.11, 18) * 0.6, place(np.zeros(1), ring([(1500, 1), (760, 0.4)], 0.05, 100) * 0.5, 0.09)), 0.4)
sfx('close', mix(ring([(1300, 1), (650, 0.4)], 0.05, 100) * 0.5, place(np.zeros(1), paper(0.1, 19) * 0.6, 0.03)), 0.4)
sfx('page', mix(paper(0.09, 20), place(np.zeros(1), paper(0.16, 21) * 0.8, 0.07)), 0.4)   # the diary page

# ---------------------------------------------------------------- doors, house, water
rattle = np.zeros(1)
for i in range(11):                                         # 미닫이문: a wooden door rattling along its track (드르륵)
    rattle = place(rattle, burst(0.03, 650 + 60 * (i % 3), 1.5, 120, seed=30 + i) * (0.5 + 0.5 * np.sin(i / 10 * np.pi)), i * 0.034)
rumble = lowpass(noise(0.42, seed=41), 260) * env_asr(int(SR * 0.42), 0.05, 0.08) * 0.5
sfx('door_slide', mix(rattle, rumble, place(np.zeros(1), thud(0.08, 500, 60, 42), 0.38)), 0.5)
bell = lambda: ring([(2620, 1), (3430, 0.6), (5900, 0.35), (7400, 0.2)], 0.8, 5)
sfx('door_bell', mix(bell(), place(np.zeros(1), bell() * 0.7, 0.11), place(np.zeros(1), bell() * 0.45, 0.2)), 0.32, 0.12)   # 딸랑
lid = lambda: mix(ring([(420, 1), (1130, 0.55), (1870, 0.35)], 0.25, 22), burst(0.04, 2500, 1, 90, seed=43) * 0.4)
sfx('jar_lid', mix(lid(), place(np.zeros(1), lid() * 0.35, 0.07)), 0.5)                 # 옹기 뚜껑 덜컥
tap = lowpass(bandpass(noise(1.0, seed=44), 1100, 0.6), 4500) * env_asr(int(SR), 0.05, 0.3) * (0.75 + 0.25 * np.sin(t(1.0) * 29) * np.sin(t(1.0) * 7))
bub = np.zeros(1)
for i in range(16):                                         # bubbles in the basin
    nb = int(SR * 0.03)
    f0 = rng.uniform(500, 1100)
    bub = place(bub, np.sin(phase(np.linspace(f0, f0 * 1.6, nb), nb)) * env_ad(nb, 0.001, 90) * rng.uniform(0.1, 0.3), rng.uniform(0.05, 0.8))
sfx('tap_water', mix(tap, bub), 0.5)
creak = np.sin(phase(np.linspace(820, 690, int(SR * 0.35)), int(SR * 0.35)))
creak = bandpass(np.sign(creak) * 0.5 + creak, 1200, 2) * env_asr(int(SR * 0.35), 0.05, 0.1) * 0.25
slosh = lowpass(noise(0.6, seed=46), 1100) * (np.sin(np.linspace(0, 2 * np.pi, int(SR * 0.6))) ** 2)
sfx('well_bucket', mix(creak, place(np.zeros(1), slosh, 0.3)), 0.5)
def gulp():
    n = int(SR * 0.13)
    g = np.sin(phase(np.linspace(240, 120, n), n)) * env_ad(n, 0.004, 28)
    return mix(bandpass(g + noise(0.13, seed=47) * 0.15, 400, 1.5), burst(0.02, 1800, 2, 200, seed=48) * 0.3)
sfx('gulp', mix(gulp(), place(np.zeros(1), gulp() * 0.9, 0.3)), 0.5)                    # 꿀꺽
munch = np.zeros(1)
for i in range(3):
    munch = place(munch, grains(0.09, 26, 2100, 0.9, 0.3, 50 + i), i * 0.17)
sfx('eat', munch, 0.45)

# ---------------------------------------------------------------- village
def vibe(nm, d=1.6):
    f = I.hz(nm)
    n = int(SR * d)
    return (np.sin(phase(f, n)) + 0.25 * np.sin(phase(f * 4, n)) * np.exp(-t(d) * 6)) * env_ad(n, 0.002, 2.2)
chime = np.zeros(1)
for i, nm in enumerate(['E5', 'C5', 'D5', 'G4']):            # the village loudspeaker chime (딩동댕동)
    chime = place(chime, vibe(nm), i * 0.42)
sfx('chime_broadcast', bandpass(chime, 1200, 0.35), 0.5, 0.35)
sfx('hwatu', mix(burst(0.06, 1300, 1.2, 80, seed=51), thud(0.06, 300, 70, 52) * 0.6), 0.45)   # a hwatu card slapped down

# ---------------------------------------------------------------- rites
roar = mix(lowpass(noise(1.4, seed=53), 400), bandpass(noise(1.4, seed=54), 1400, 0.7) * np.clip(t(1.4) / 1.0, 0, 1))
roar *= env_asr(int(SR * 1.4), 0.5, 0.5)
pops = np.zeros(1)
for i in range(14):
    pops = place(pops, burst(0.02, rng.uniform(1500, 4000), 1.5, 160, seed=60 + i) * rng.uniform(0.3, 1), rng.uniform(0.3, 1.3))
sfx('fire_whoosh', mix(roar, pops * 0.6), 0.6)
sfx('bureom_crack', mix(burst(0.05, 2600, 1.4, 90, seed=75), ring([(900, 1), (1700, 0.5)], 0.06, 70) * 0.5,
                        place(np.zeros(1), grains(0.12, 10, 3000, 1, 0.3, 76) * 0.4, 0.03)), 0.6)   # 딱!

# ---------------------------------------------------------------- creatures, far away (low-passed and roomy so they sit in the distance)
def kak(seed):
    n = int(SR * 0.07)
    tone_ = np.sin(phase(np.linspace(1700, 1250, n), n)) + 0.6 * np.sign(np.sin(phase(np.linspace(1700, 1250, n) * 2, n)))
    return mix(bandpass(tone_, 1800, 1.2), burst(0.07, 3200, 1.0, 25, seed=seed) * 0.8) * env_ad(n, 0.004, 30)
mag = np.zeros(1)
for i in range(5):                                           # 까치: a dry "kkak-kkak-kkak"
    mag = place(mag, kak(80 + i) * (1 - i * 0.08), i * 0.15)
sfx('magpie', lowpass(mag, 5000), 0.35, 0.3)
def bark(seed, f0=430):
    n = int(SR * 0.2)
    f = f0 * (1 + 0.25 * np.exp(-t(0.2) * 30)) * (1 - 0.2 * t(0.2) / 0.2)
    v = sum(np.sin(phase(f * k, n)) / k for k in range(1, 8))
    v = mix(bandpass(v, 700, 1.0), bandpass(noise(0.2, seed=seed), 900, 1.0) * 0.4)
    return v * env_ad(n, 0.006, 14)
dog = mix(bark(90), place(np.zeros(1), bark(91, 400) * 0.85, 0.33))
sfx('dog_far', lowpass(dog, 1300), 0.25, 0.5)
n = int(SR * 1.5)
x = t(1.5)
fr = np.interp(x, [0, 0.12, 0.3, 0.5, 0.95, 1.5], [480, 620, 560, 880, 820, 560])       # 꼬끼오
v = sum(np.sin(phase(fr * k, n)) / (k ** 0.8) for k in range(1, 10))
v = mix(bandpass(v, 1100, 1.3), bandpass(v, 2300, 2.0) * 0.5)
gaps = np.ones(n)
for a, b in [(0.11, 0.14), (0.29, 0.32)]:
    gaps[int(a * SR):int(b * SR)] = 0.15
sfx('rooster', lowpass(v * gaps * env_asr(n, 0.02, 0.35), 2200), 0.28, 0.45)

# ---------------------------------------------------------------- spring (F-1): a skylark over the fields
lark = np.zeros(1)
for i in range(14):                                          # a long tumbling trill of quick chirps
    f0 = rng.uniform(2800, 4200)
    nb = int(SR * rng.uniform(0.04, 0.08))
    chirp = np.sin(phase(np.linspace(f0, f0 * rng.uniform(0.8, 1.25), nb), nb)) * env_ad(nb, 0.003, 30)
    lark = place(lark, chirp * rng.uniform(0.4, 1.0), i * 0.09 + rng.uniform(0, 0.03))
sfx('lark', lowpass(lark, 7000), 0.25, 0.35)

# ---------------------------------------------------------------- write the effects
os.makedirs(OUT, exist_ok=True)
for name, y in SFX.items():
    save_wav(os.path.join(OUT, name + '.wav'), y)


# ---------------------------------------------------------------- looping outdoor sounds (Ambience.cs)
def wind(d, seed, dark=700, gust_rate=0.09, strength=1.0):
    """Winter wind: two noise bands whose loudness drifts in slow gusts. Loops seamlessly."""
    r = np.random.default_rng(seed)
    n = int(SR * d)
    x = np.arange(n) / SR
    y = np.zeros(n)
    for fc, q, a in [(dark, 0.6, 1.0), (dark * 2.6, 1.3, 0.35)]:
        b = bandpass(r.uniform(-1, 1, n), fc, q)
        # gusts: a few sine swells with whole numbers of cycles in d, so the loop meets itself
        g = 0.55 + sum(r.uniform(0.1, 0.25) * np.sin(2 * np.pi * k / d * x + r.uniform(0, 6)) for k in (1, 2, 3, 5))
        y += b * np.clip(g, 0.1, None) * a
    y = wrap_tail(y, n)
    # crossfade the noise seam
    k = int(SR * 0.4)
    y[:k] = y[:k] * np.linspace(0, 1, k) + y[-k:] * np.linspace(1, 0, k)
    y = y[:n - k]
    return normalize(y, 0.5 * strength)


def crackle(d, seed):
    r = np.random.default_rng(seed)
    n = int(SR * d)
    y = lowpass(r.uniform(-1, 1, n), 500) * 0.25                     # the low hush of the fire
    for i in range(int(d * 9)):
        at = r.uniform(0, d - 0.05)
        y = place(y, burst(0.03, r.uniform(1200, 4200), 1.4, 150, seed=int(r.integers(1e6))) * r.uniform(0.2, 1), at)
    y = y[:n]
    k = int(SR * 0.2)
    y[:k] = y[:k] * np.linspace(0, 1, k) + y[n - k:] * np.linspace(1, 0, k)
    return normalize(y[: n - k], 0.6)


os.makedirs(AMB, exist_ok=True)
def frogs(d, seed):
    """Spring nights by the flooded paddies: many frogs croaking in overlapping pulses (gaegul-gaegul)."""
    r = np.random.default_rng(seed)
    n = int(SR * d)
    y = np.zeros(n)
    for f in range(9):                                           # nine frogs, each with its own pitch and rhythm
        f0 = r.uniform(380, 900)
        rate = r.uniform(5.0, 8.0)
        at = r.uniform(0, 1.0)
        while at < d - 0.3:
            burst_len = r.integers(4, 9)                          # a run of croaks, then a rest
            for k in range(burst_len):
                nb = int(SR * 0.06)
                v = sum(np.sin(phase(f0 * h * (1 - 0.15 * np.linspace(0, 1, nb)), nb)) / h for h in (1, 2, 3))
                y = place(y, bandpass(v, f0 * 1.6, 1.2) * env_ad(nb, 0.004, 45) * r.uniform(0.2, 0.5) / (1 + f * 0.15), at)
                at += 1 / rate
            at += r.uniform(0.8, 3.0)
    y = lowpass(y[:n], 3000)
    k = int(SR * 0.3)
    y[:k] = y[:k] * np.linspace(0, 1, k) + y[n - k:] * np.linspace(1, 0, k)
    return normalize(y[: n - k], 0.45)


AMBS = {
    'amb_frogs': frogs(20.3, 104),                              # F-1: spring nights by the flooded paddies
    'amb_wind_winter': wind(24.4, 101, 650, strength=1.0),     # day outdoors in winter (stronger when it snows)
    'amb_night': wind(30.4, 102, 420, strength=0.7),           # a quieter, darker night
    'amb_fire': crackle(8.2, 103),                              # near the lit agungi and the burning moon house
}
for name, y in AMBS.items():
    save_ogg(os.path.join(AMB, name + '.ogg'), y, quality=3)
    write_audio_meta(os.path.join(AMB, name + '.ogg'), streaming=False)

# ---------------------------------------------------------------- small copies for the browser preview
os.makedirs(WEB, exist_ok=True)
for name in SFX:
    to_mp3(os.path.join(OUT, name + '.wav'), os.path.join(WEB, name + '.mp3'), 64)
for name in AMBS:
    to_mp3(os.path.join(AMB, name + '.ogg'), os.path.join(WEB, name + '.mp3'), 48)
print('sfx:', len(SFX), sorted(SFX))
print('ambience:', sorted(AMBS))
