"""Original, deterministic textures and quiet synthesized field placeholders.
Run with python3 tools/build_assets.py. Uses only the Python standard library.
No downloaded samples, copied game assets or runtime asset generation.
"""
from pathlib import Path
import math
import random
import struct
import wave
import zlib

ROOT = Path(__file__).resolve().parents[1]


def png(name, size, pixel):
    rng = random.Random(1987)
    raw = b"".join(b"\0" + bytes(c for x in range(size) for c in pixel(x, y, rng)) for y in range(size))
    def chunk(kind, data):
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xffffffff)
    data = b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 2, 0, 0, 0))
    path = ROOT / "assets/textures" / name
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data + chunk(b"IDAT", zlib.compress(raw)) + chunk(b"IEND", b""))


def grass(x, y, r):
    n = r.randrange(-24, 25) + (10 if (x + y * 3) % 11 < 2 else 0)
    return (max(0, 124 + n), max(0, 141 + n), max(0, 72 + n // 2))


def plaster(x, y, r):
    n = r.randrange(-9, 10) - (25 if y > 26 and r.random() < 0.24 else 0)
    return (215 + n, 211 + n, 184 + n)


def roof(x, y, r):
    n = int(18 * math.cos(x * math.tau / 8)) + r.randrange(-10, 11)
    n -= 22 if y % 12 == 0 else 0
    return (157 + n, 78 + n // 2, 46 + n // 3)


png("grass.png", 64, grass)
png("plaster.png", 32, plaster)
png("roof.png", 48, roof)
png("wood.png", 32, lambda x, y, r: (88 + r.randrange(20), 75 + r.randrange(15), 52 + r.randrange(12)))

RATE = 22050


def audio(name, seconds, sample):
    r = random.Random(338)
    path = ROOT / "assets/audio" / (name + ".wav")
    path.parent.mkdir(parents=True, exist_ok=True)
    values = []
    state = 0.0
    for i in range(int(seconds * RATE)):
        t = i / RATE
        noise = r.uniform(-1, 1)
        state = state * 0.96 + noise * 0.04
        value = sample(t, noise, state)
        edge = min(1, t / 0.04, (seconds - t) / 0.12)
        values.append(struct.pack("<h", int(max(-1, min(1, value * edge)) * 32767)))
    with wave.open(str(path), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(values))


audio("wind", 12, lambda t, n, s: s * 0.48 * (0.65 + 0.35 * math.sin(t * 0.6)))
audio("insects", 5, lambda t, n, s: 0.022 * math.sin(t * 7800) * max(0, math.sin(t * 19)) ** 8)
audio("bird", 1.6, lambda t, n, s: 0.13 * math.sin(math.tau * (1900*t + 160*math.sin(t*13))) * max(0, math.sin(t*12)) ** 8 * math.exp(-t))
audio("dog", 2.2, lambda t, n, s: (0.13*math.sin(680*t + 6*math.sin(94*t)) + s*0.4) * sum(math.exp(-((t-p)/0.065)**2) for p in (0.3, 0.62)))
audio("step_grass", 0.32, lambda t, n, s: (n * 0.15 + s * 0.5) * math.exp(-t*22))
audio("step_dirt", 0.3, lambda t, n, s: (n * 0.22 + math.sin(240*t)*0.15) * math.exp(-t*30))
audio("step_cement", 0.28, lambda t, n, s: (math.sin(330*t)*0.25 + n*0.06) * math.exp(-t*36))
audio("empty_house", 3.5, lambda t, n, s: (math.sin(t*math.tau*183) + 0.3*math.sin(t*math.tau*366)) * 0.018 * math.sin(t*0.9)**2)
print("Original textures and 8 audio placeholders written to assets/.")
