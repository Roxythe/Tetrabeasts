"""Rebuild original retro modifier cues; deterministic PCM, peak-normalized, no dependencies."""
import math, random, struct, wave
from pathlib import Path

OUT = Path(__file__).resolve().parents[1] / 'Assets/Audio/LevelModifier'
RATE = 22050

def render(name, duration, kind):
    rng = random.Random(428)
    samples, phase, noise = [], 0.0, 0.0
    for i in range(int(duration * RATE)):
        t = i / RATE
        u = t / duration
        if i % 3 == 0: noise = rng.uniform(-1, 1)
        if kind in ('rock', 'bomb'):
            freq = (125 if kind == 'rock' else 85) * (1 - u * .7)
            phase += freq / RATE
            v = (math.sin(phase * math.tau) * .65 + noise * .5) * math.exp(-u * 7)
            v += noise * .25 * math.exp(-((u - .12) / .06) ** 2)
        else:
            start, end = {'spawn': (180, 780), 'merge': (300, 1150), 'death': (850, 90), 'invert': (420, 200), 'flip': (160, 640)}[kind]
            step = math.floor(u * 12) / 12
            freq = start + (end - start) * step
            freq *= 1 + .04 * math.sin(t * 65)
            phase += freq / RATE
            pulse = 1 if phase % 1 < .3 else -.43
            v = (.55 * pulse + .35 * math.sin(phase * math.tau * .5)) * math.sin(math.pi * u) ** .6
            if kind == 'death': v += noise * .15 * (1 - u)
        v *= min(1, t / .004) * min(1, (duration - t) / .02)
        samples.append(v)
    gain = .92 / max(abs(v) for v in samples)
    data = b''.join(struct.pack('<h', round(max(-1, min(1, v * gain)) * 32767)) for v in samples)
    with wave.open(str(OUT / (name + '.wav')), 'wb') as f:
        f.setparams((1, 2, RATE, 0, 'NONE', 'not compressed')); f.writeframes(data)
    rms = math.sqrt(sum((v * gain) ** 2 for v in samples) / len(samples))
    print(name, 'peak=0.92 RMS=%.3f' % rms)

for args in [('BoulderImpact', .24, 'rock'), ('GhostSpawn', .33, 'spawn'), ('GhostMerge', .38, 'merge'),
             ('GhostDestroyed', .42, 'death'), ('BombardmentImpact', .5, 'bomb'),
             ('InversionActivate', .45, 'invert'), ('BoardFlip', .36, 'flip')]:
    render(*args)
