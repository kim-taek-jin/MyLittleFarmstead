"""Small sound toolkit shared by make_sfx.py and make_music.py (numpy only).

Everything is synthesized from sine waves and noise, so there are no license questions.
Filtering and reverb are done in the frequency domain (FFT) so long tracks stay fast.
"""
import os
import subprocess
import wave

import numpy as np

SR = 44100
rng = np.random.default_rng(7)


def t(d, sr=SR):
    return np.arange(int(sr * d)) / sr


def silence(d, sr=SR):
    return np.zeros(int(sr * d))


def env_ad(n, attack=0.005, decay=8.0, sr=SR):
    x = np.arange(n) / sr
    return np.clip(x / max(attack, 1e-4), 0, 1) * np.exp(-x * decay)


def env_asr(n, attack, release, sr=SR):
    """Rise over attack, hold, fall over release at the end."""
    x = np.arange(n) / sr
    a = np.clip(x / max(attack, 1e-4), 0, 1)
    r = np.clip((n / sr - x) / max(release, 1e-4), 0, 1)
    return np.minimum(a, r) ** 1.5


def phase(freq, n, sr=SR):
    f = np.broadcast_to(np.asarray(freq, dtype=float), (n,)) if np.ndim(freq) == 0 else np.asarray(freq, dtype=float)[:n]
    if len(f) < n:
        f = np.concatenate([f, np.full(n - len(f), f[-1])])
    return 2 * np.pi * np.cumsum(f) / sr


def fft_filter(x, response, sr=SR):
    """Filter x with a magnitude response given as a function of frequency (Hz) -> gain."""
    n = len(x)
    if n == 0:
        return x
    m = 1 << int(np.ceil(np.log2(n + 1)))
    X = np.fft.rfft(x, m)
    f = np.fft.rfftfreq(m, 1 / sr)
    y = np.fft.irfft(X * response(f), m)[:n]
    return y


def lowpass(x, fc, order=2, sr=SR):
    return fft_filter(x, lambda f: 1 / np.sqrt(1 + (f / fc) ** (2 * order)), sr)


def highpass(x, fc, order=2, sr=SR):
    return fft_filter(x, lambda f: 1 / np.sqrt(1 + (fc / np.maximum(f, 1e-3)) ** (2 * order)), sr)


def bandpass(x, fc, q=2.0, sr=SR):
    def r(f):
        f = np.maximum(f, 1e-3)
        return 1 / np.sqrt(1 + q * q * (f / fc - fc / f) ** 2)
    return fft_filter(x, r, sr)


def peak(x, fc, gain_db, q=1.5, sr=SR):
    g = 10 ** (gain_db / 20) - 1
    return fft_filter(x, lambda f: 1 + g / (1 + q * q * (np.maximum(f, 1e-3) / fc - fc / np.maximum(f, 1e-3)) ** 2), sr)


def noise(d, sr=SR, seed=None):
    r = rng if seed is None else np.random.default_rng(seed)
    return r.uniform(-1, 1, int(sr * d))


def burst(d, fc, q=1.5, decay=30.0, attack=0.001, sr=SR, seed=None):
    """A filtered noise hit."""
    n = noise(d, sr, seed)
    return bandpass(n, fc, q, sr) * env_ad(len(n), attack, decay, sr)


def place(dst, src, at, gain=1.0, sr=SR):
    """Add src into dst starting at time `at` (seconds); grows dst if needed. Returns dst."""
    i = int(at * sr)
    end = i + len(src)
    if end > len(dst):
        dst = np.concatenate([dst, np.zeros(end - len(dst))])
    dst[i:end] += src * gain
    return dst


def mix(*xs):
    n = max(len(x) for x in xs)
    y = np.zeros(n)
    for x in xs:
        y[: len(x)] += x
    return y


def impulse(seconds, decay, bright=4000.0, sr=SR, seed=1):
    """A synthetic room/hall: decaying, darkening noise."""
    n = int(seconds * sr)
    r = np.random.default_rng(seed).standard_normal(n)
    e = np.exp(-np.arange(n) / sr * decay)
    ir = lowpass(r * e, bright, 1, sr)
    ir[: int(0.012 * sr)] *= np.linspace(0, 1, int(0.012 * sr))   # a little pre-delay feel
    return ir / np.sqrt(np.sum(ir ** 2))


def convolve(x, ir):
    n = len(x) + len(ir) - 1
    m = 1 << int(np.ceil(np.log2(n)))
    return np.fft.irfft(np.fft.rfft(x, m) * np.fft.rfft(ir, m), m)[:n]


def reverb(x, wet=0.25, seconds=1.8, decay=3.0, bright=4000.0, sr=SR, seed=1):
    ir = impulse(seconds, decay, bright, sr, seed)
    w = convolve(x, ir)
    w = w / max(1e-9, np.abs(w).max()) * max(1e-9, np.abs(x).max())
    return mix(x * (1 - wet), w * wet)


def loop_reverb(x, wet=0.25, seconds=1.8, decay=3.0, bright=4000.0, sr=SR, seed=1):
    """Reverb for a looping track: the tail wraps round to the start so the loop has no seam."""
    ir = impulse(seconds, decay, bright, sr, seed)
    w = convolve(x, ir)
    n = len(x)
    tail = w[n:]
    w = w[:n].copy()
    w[: len(tail)] += tail[: n]
    w = w / max(1e-9, np.abs(w).max()) * max(1e-9, np.abs(x).max())
    return x * (1 - wet) + w * wet


def wrap_tail(y, n):
    """Fold anything past n samples back onto the start (for loops)."""
    if len(y) <= n:
        return np.concatenate([y, np.zeros(n - len(y))])
    out = y[:n].copy()
    tail = y[n:]
    while len(tail):
        k = min(n, len(tail))
        out[:k] += tail[:k]
        tail = tail[k:]
    return out


def normalize(y, peak_=0.9):
    m = np.abs(y).max()
    return y if m < 1e-9 else y / m * peak_


def fade_edges(y, ms=6, sr=SR):
    k = min(len(y) // 2, int(sr * ms / 1000))
    if k > 0:
        y = y.copy()
        y[:k] *= np.linspace(0, 1, k)
        y[-k:] *= np.linspace(1, 0, k)
    return y


def save_wav(path, y, sr=SR, stereo=None):
    """y: mono array, or pass stereo=(l, r)."""
    os.makedirs(os.path.dirname(path), exist_ok=True)
    if stereo is not None:
        l, r = stereo
        data = np.empty(len(l) * 2)
        data[0::2] = l
        data[1::2] = r
        ch = 2
    else:
        data = y
        ch = 1
    data = (np.clip(data, -1, 1) * 32767).astype(np.int16)
    with wave.open(path, 'wb') as w:
        w.setnchannels(ch)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes(data.tobytes())


def save_ogg(path, y=None, sr=SR, stereo=None, quality=5):
    """Encode with ffmpeg (libvorbis). Unity imports .ogg and loops it without a gap."""
    tmp = path + '.tmp.wav'
    save_wav(tmp, y, sr, stereo)
    subprocess.run(['ffmpeg', '-y', '-loglevel', 'error', '-i', tmp, '-c:a', 'libvorbis', '-q:a', str(quality), path], check=True)
    os.remove(tmp)


def to_mp3(src, dst, kbps=64, mono=True):
    """Small copies for the web preview."""
    args = ['ffmpeg', '-y', '-loglevel', 'error', '-i', src]
    if mono:
        args += ['-ac', '1']
    args += ['-c:a', 'libmp3lame', '-b:a', f'{kbps}k', dst]
    subprocess.run(args, check=True)


def write_audio_meta(path, streaming=True):
    """Unity import settings for long loops: stream from disk (Vorbis) instead of unpacking into memory.
    Written once; the guid comes from the file name so it stays the same between runs."""
    import hashlib
    meta = path + '.meta'
    if os.path.exists(meta):
        return
    guid = hashlib.md5(('mlf-audio/' + os.path.basename(path)).encode()).hexdigest()
    with open(meta, 'w') as f:
        f.write(f"""fileFormatVersion: 2
guid: {guid}
AudioImporter:
  externalObjects: {{}}
  serializedVersion: 8
  defaultSettings:
    serializedVersion: 2
    loadType: {2 if streaming else 1}
    sampleRateSetting: 0
    sampleRateOverride: 44100
    compressionFormat: 1
    quality: 0.8
    conversionMode: 0
    preloadAudioData: 0
  platformSettingOverrides: {{}}
  forceToMono: 0
  normalize: 0
  loadInBackground: 1
  ambisonic: 0
  3D: 0
  userData: 
  assetBundleName: 
  assetBundleVariant: 
""")
