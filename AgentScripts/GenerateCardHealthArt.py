"""Hand-drawn sprites for the in-level card health bar (PvZ-style chunky bar + heart badge).
Uses the tutorial's hand-drawn helpers so it matches the wave meter.
  python AgentScripts/GenerateCardHealthArt.py      (from the project root)
Writes Assets/UserInterface/LevelHUD/Art/Cards/HealthBar/CardHP_Frame/Fill/Heart.png.
FILL_X / BAR_SIZE are mirrored in CardHealthBar.cs (fillMin / fillMax)."""
import math
import os
import sys

import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
sys.path.insert(0, os.path.join(ROOT, "Assets", "UserInterface", "Tutorial", "Tools"))
from handdrawn import INK, Sheet, band, densify, round_rect_pts, wobble  # noqa: E402

OUT = os.path.join(ROOT, "Assets", "UserInterface", "LevelHUD", "Art", "Cards", "HealthBar")

WOOD = (138, 84, 50, 255)
WOOD_LIGHT = (178, 120, 72, 255)
WOOD_DARK = (98, 58, 38, 255)

# 2x of the 260x52 reference px it is shown at
BAR_SIZE = (520, 104)
FILL_X = (40, 492)
FILL_Y = (34, 72)


def frame():
    w, h = BAR_SIZE
    s = Sheet(w, h)
    plank = s.mask(wobble(round_rect_pts(10, 14, w - 10, h - 12, 30, 2), 2.5, 7, wavelength=40))
    for cx, cy, r in ((150, 14, 6), (380, h - 12, 7)):
        plank &= ~s.ellipse(cx, cy, r, r * 0.8, 1.0, cx)
    s.paint(plank, WOOD)
    s.rgba[band(plank, 0, 9)] = WOOD_LIGHT
    s.rgba[band(plank, 0, -8)] = WOOD_DARK
    s.edge(plank, 3.2, seed=8)

    groove = s.mask(wobble(round_rect_pts(FILL_X[0] - 6, FILL_Y[0] - 6, FILL_X[1] + 6, FILL_Y[1] + 6, 20, 2), 1.2, 9, wavelength=50))
    s.paint(groove, (46, 30, 44, 255))
    s.rgba[band(groove, 0, 8)] = (28, 18, 28, 255)
    s.edge(groove, 2.6, seed=10)

    s.outline(4.0, seed=11)
    return s.image()


def fill():
    """Grey-scale cel fill on the frame's canvas: tinted green/yellow/red at runtime, Filled from the left."""
    w, h = BAR_SIZE
    s = Sheet(w, h)
    m = s.mask(wobble(round_rect_pts(FILL_X[0], FILL_Y[0], FILL_X[1], FILL_Y[1], 17, 2), 0.8, 21, wavelength=60))
    s.paint(m, (214, 214, 214, 255))
    ys = np.arange(s.H)[:, None] / 4
    xs = np.arange(s.W)[None, :] / 4
    light = m & (ys < FILL_Y[0] + 13 + 2 * np.sin(xs / 31.0) + 1.5 * np.sin(xs / 11.0 + 1))
    s.rgba[light] = (255, 255, 255, 255)
    s.rgba[band(m, 0, -8)] = (150, 150, 150, 255)
    for x, ln in ((70, 30), (230, 16), (360, 24)):
        s.line([(x, FILL_Y[0] + 7), (x + ln, FILL_Y[0] + 7)], 4, (255, 255, 255, 255))
    # segment notches, like the chunky bars in PvZ
    for i in range(1, 5):
        x = FILL_X[0] + (FILL_X[1] - FILL_X[0]) * i / 5
        s.line(wobble(densify([(x, FILL_Y[0] + 3), (x - 2, FILL_Y[1] - 3)], 1.0, False), 0.6, 30 + i, closed=False), 3.0, (120, 120, 120, 255))
    return s.image()


def heart():
    n = 128
    s = Sheet(n, n)
    pts = []
    for a in np.linspace(0, 2 * math.pi, 240, endpoint=False):
        x = 16 * math.sin(a) ** 3
        y = 13 * math.cos(a) - 5 * math.cos(2 * a) - 2 * math.cos(3 * a) - math.cos(4 * a)
        pts.append((64 + x * 3.3, 60 - y * 3.3))
    body = s.mask(wobble(pts, 1.4, 51, wavelength=30))
    s.paint(body, (226, 56, 66, 255))
    s.rgba[band(body, 7, 8)] = (250, 112, 112, 255)
    s.rgba[band(body, -9, -10)] = (156, 30, 48, 255)
    s.edge(body, 3.2, seed=52)
    # shine
    shine = s.ellipse(42, 40, 11, 7, 0.8, 53)
    s.rgba[shine & body] = (255, 236, 236, 255)
    s.rgba[s.ellipse(58, 34, 3.5, 3.5) & body] = (255, 236, 236, 255)
    s.outline(4.2, seed=54)
    return s.image()


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    frame().save(os.path.join(OUT, "CardHP_Frame.png"))
    fill().save(os.path.join(OUT, "CardHP_Fill.png"))
    heart().save(os.path.join(OUT, "CardHP_Heart.png"))
    print("wrote", OUT)
