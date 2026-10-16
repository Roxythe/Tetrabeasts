"""Original dry stone-on-stone impacts. Deterministic synthesis; no external samples.

Layer a short low body, inharmonic stone resonances and scattered grit contacts.
Run this file to reproduce the three alternate clips without changing the old cue.
"""
import math
import random
import struct
import wave
from pathlib import Path

OUT = Path(__file__).resolve().parents[1] / "Assets/Audio/LevelModifier"
RATE = 44100


def render(name, seed, body, duration):
    rng = random.Random(seed)
    modes = [(f * rng.uniform(.93, 1.07), rng.uniform(.016, .045), rng.uniform(0, 6.28))
             for f in (390, 647, 1063, 1751, 2833)]
    contacts = [(0., 1.)] + [(rng.uniform(.035, .18), rng.uniform(.08, .23)) for _ in range(5)]
    samples = []
    low = previous = 0.
    for i in range(int(RATE * duration)):
        t = i / RATE
        noise = rng.uniform(-1., 1.)
        low += .08 * (noise - low)
        high = noise - previous
        previous = noise
        value = .75 * math.sin(math.tau * body * t) * math.exp(-t / .035)
        value += low * 1.4 * math.exp(-t / .065)
        for onset, gain in contacts:
            age = t - onset
            if age < 0:
                continue
            attack = min(1., age / .0007)
            clack = high * .27 * math.exp(-age / .006)
            clack += sum(math.sin(math.tau * f * age + phase) * math.exp(-age / decay)
                         for f, decay, phase in modes) * .09
            value += gain * attack * clack
        value *= min(1., t / .0015) * min(1., (duration - t) / .02)
        samples.append(value)
    gain = .85 / max(abs(v) for v in samples)
    pcm = b"".join(struct.pack("<h", round(v * gain * 32767)) for v in samples)
    with wave.open(str(OUT / (name + ".wav")), "wb") as output:
        output.setparams((1, 2, RATE, 0, "NONE", "not compressed"))
        output.writeframes(pcm)
    print(f"{name}: {duration:.2f}s, peak .85, RMS {math.sqrt(sum((v * gain)**2 for v in samples)/len(samples)):.3f}")


if __name__ == "__main__":
    render("BoulderStoneThud", 9031, 112, .32)
    render("BoulderStoneCrack", 9032, 153, .28)
    render("BoulderStoneClatter", 9033, 87, .40)
