"""Gugak-style instruments made from sine waves and noise (for the music and a few sound effects).

  gayageum   plucked silk strings: bright attack, upper partials die fast; nonghyeon (농현) bends and vibrato
  daegeum    big bamboo flute: breathy, slow scoop into the note, late vibrato, a buzzing membrane (청) when loud
  saenghwang mouth organ: a soft reedy chord (a warm pad)
  janggu     hourglass drum: kung (low left head), deok (stick on the right head), deong (both)
  buk        barrel drum, kkwaenggwari small gong, jing big gong
"""
import numpy as np

from dsp import SR, t, env_ad, env_asr, phase, bandpass, lowpass, highpass, noise, place

A4 = 440.0
NAMES = {'C': 0, 'Db': 1, 'D': 2, 'Eb': 3, 'E': 4, 'F': 5, 'Gb': 6, 'G': 7, 'Ab': 8, 'A': 9, 'Bb': 10, 'B': 11}


def hz(name):
    """'Eb4' -> Hz"""
    n, o = name[:-1], int(name[-1])
    midi = 12 * (o + 1) + NAMES[n]
    return A4 * 2 ** ((midi - 69) / 12)


def contour(f, n, tech, sr=SR):
    """Pitch over time for nonghyeon (농현): 'vib' (떠는 소리), 'down' (꺾는 소리), 'up' (밀어 올림), 'scoop'."""
    x = np.arange(n) / sr
    c = np.ones(n)
    if tech in ('vib', 'vibx'):
        depth = 0.012 if tech == 'vib' else 0.026
        grow = np.clip((x - 0.18) / 0.5, 0, 1)
        c *= 1 + depth * grow * np.sin(2 * np.pi * 4.6 * x)
    elif tech == 'down':                 # sound the note pressed a step high, then let it fall
        c *= 1 + 0.07 * np.exp(-np.clip(x - 0.05, 0, None) * 14)
    elif tech == 'up':                   # push up a little at the end of the note
        c *= 1 + 0.06 * np.clip((x - n / sr * 0.55) / 0.15, 0, 1)
    elif tech == 'scoop':
        c *= 1 - 0.03 * np.exp(-x * 18)
    return f * c


def gayageum(freq, dur, vel=0.8, tech=None, sr=SR):
    n = int((dur + 1.4) * sr)                       # strings ring past the written length
    f = contour(freq, n, tech, sr)
    y = np.zeros(n)
    for k in range(1, 13):
        amp = vel * k ** -1.15 * abs(np.sin(np.pi * k * 0.2)) + 0.002
        dec = 1.6 + 0.85 * k
        y += amp * np.sin(phase(f * k * (1 + 0.0003 * k * k), n, sr)) * np.exp(-np.arange(n) / sr * dec)
    y *= np.clip(np.arange(n) / (0.002 * sr), 0, 1)
    pluck = highpass(noise(0.015, sr), 2500, 2, sr) * env_ad(int(0.015 * sr), 0.0005, 260, sr) * vel * 0.25
    y[: len(pluck)] += pluck
    # let it be damped a little after the written length (the hand rests on the string)
    damp = np.ones(n)
    k0 = int(dur * sr)
    damp[k0:] = np.exp(-np.arange(n - k0) / sr * 3.5)
    return y * damp * 0.32


def daegeum(freq, dur, vel=0.7, tech='vib', sr=SR):
    n = int((dur + 0.25) * sr)
    x = np.arange(n) / sr
    f = freq * (1 - 0.025 * np.exp(-x * 9))                         # breathe up into the note
    if tech:
        grow = np.clip((x - 0.35) / 0.6, 0, 1)
        f = f * (1 + 0.011 * grow * np.sin(2 * np.pi * 5.2 * x))
    amps = [1.0, 0.42, 0.22, 0.1, 0.06, 0.035]
    y = np.zeros(n)
    for k, a in enumerate(amps, 1):
        y += a * np.sin(phase(f * k, n, sr))
    # the buzzing membrane (청): odd upper partials that come in when the player blows harder
    loud = np.clip((vel - 0.55) / 0.4, 0, 1)
    if loud > 0:
        buzz = sum(0.05 / k * np.sin(phase(f * k, n, sr)) for k in (5, 7, 9, 11))
        y += buzz * loud * (0.6 + 0.4 * np.sin(2 * np.pi * 3 * x) ** 2)
    breath = bandpass(noise(n / sr, sr), freq * 2.2, 1.2, sr) * 0.22
    e = env_asr(n, 0.09, 0.22, sr)
    return (y * 0.55 + breath) * e * vel * 0.33


def saenghwang(freqs, dur, vel=0.5, sr=SR):
    n = int((dur + 0.5) * sr)
    y = np.zeros(n)
    for j, fr in enumerate(freqs):
        for k in range(1, 9):
            y += (0.6 / k) * np.sin(phase(fr * k * (1 + 0.0006 * j), n, sr) + j)
    y = lowpass(y, 1800, 1, sr)
    return y * env_asr(n, 0.45, 0.5, sr) * vel * 0.06


# ---------------------------------------------------------------- percussion
def janggu(stroke, vel=0.8, sr=SR):
    if stroke == 'kung':      # 궁편 (left, palm): a low round boom
        n = int(0.7 * sr)
        f = 70 + 55 * np.exp(-np.arange(n) / sr * 22)
        y = np.sin(phase(f, n, sr)) * env_ad(n, 0.002, 7, sr)
        y += lowpass(noise(0.7, sr), 500, 2, sr) * env_ad(n, 0.001, 35, sr) * 0.4
        return y * vel * 0.6
    if stroke == 'deok':      # 채편 (right, thin bamboo stick): a dry crack
        n = int(0.25 * sr)
        y = bandpass(noise(0.25, sr), 2600, 1.2, sr) * env_ad(n, 0.0005, 45, sr)
        y += np.sin(phase(560, n, sr)) * env_ad(n, 0.0005, 38, sr) * 0.35
        return y * vel * 0.55
    if stroke == 'deong':
        return mix2(janggu('kung', vel, sr), janggu('deok', vel, sr))
    if stroke == 'gi':        # the grace note before deok (기덕)
        return janggu('deok', vel * 0.45, sr)
    raise ValueError(stroke)


def buk(vel=0.8, sr=SR):
    n = int(0.9 * sr)
    f = 52 + 40 * np.exp(-np.arange(n) / sr * 16)
    y = np.sin(phase(f, n, sr)) * env_ad(n, 0.002, 5, sr)
    y += lowpass(noise(0.9, sr), 300, 2, sr) * env_ad(n, 0.001, 25, sr) * 0.5
    return y * vel * 0.7


def kkwaenggwari(vel=0.8, muted=False, sr=SR):
    n = int((0.25 if muted else 0.9) * sr)
    base = 980.0
    y = np.zeros(n)
    for ratio, a in [(1, 1.0), (1.52, 0.7), (2.17, 0.55), (2.83, 0.4), (3.41, 0.3), (4.4, 0.2)]:
        y += a * np.sin(phase(base * ratio, n, sr)) * env_ad(n, 0.0005, (30 if muted else 6) + ratio * 2, sr)
    y += highpass(noise(n / sr, sr), 4000, 2, sr) * env_ad(n, 0.0005, 60, sr) * 0.4
    return y * vel * 0.16


def jing(vel=0.8, sr=SR):
    n = int(4.5 * sr)
    x = np.arange(n) / sr
    y = np.zeros(n)
    for ratio, a, d in [(1, 1.0, 0.55), (1.012, 0.8, 0.6), (2.02, 0.35, 0.9), (2.94, 0.2, 1.3), (4.1, 0.1, 2.0)]:
        f = 118 * ratio * (1 + 0.012 * (1 - np.exp(-x * 2)))     # the gong rises a little as it blooms
        y += a * np.sin(phase(f, n, sr)) * np.exp(-x * d)
    y *= np.clip(x / 0.04, 0, 1)
    return y * vel * 0.4


def mix2(a, b):
    n = max(len(a), len(b))
    y = np.zeros(n)
    y[: len(a)] += a
    y[: len(b)] += b
    return y


# ---------------------------------------------------------------- writing music down
def render_line(notes, unit, inst, sr=SR, start=0.0, lag=0.0, vel=0.8, octave=0):
    """notes: list of (name or 'r', units, tech). Returns (mono array, end time)."""
    y = np.zeros(int(sr * 2))
    at = start
    for item in notes:
        name, units = item[0], item[1]
        tech = item[2] if len(item) > 2 else None
        v = item[3] if len(item) > 3 else vel
        d = units * unit
        if name != 'r':
            f = hz(name) * 2 ** octave
            y = place(y, inst(f, d, v, tech, sr) if inst is not saenghwang else saenghwang([f], d, v, sr), at + lag, 1.0, sr)
        at += d
    return y, at
