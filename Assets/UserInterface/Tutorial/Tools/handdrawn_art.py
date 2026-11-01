"""Hand-drawn tutorial sprites: the guide's crystal wand and the wave meter (frame, fill, skull, flag).
Called from GenerateTutorialArt.py. Layout constants here are mirrored in WaveProgressMeter.cs
(fillMin / fillMax) and TutorialSetup.cs / TutorialTheme.cs (wand pivot)."""
import numpy as np

from handdrawn import INK, Sheet, band, bezier, densify, round_rect_pts, stroke, wobble

WOOD = (138, 84, 50, 255)
WOOD_LIGHT = (178, 120, 72, 255)
WOOD_DARK = (98, 58, 38, 255)
GEM = (118, 222, 236, 255)
GEM_LIGHT = (196, 250, 255, 255)
GEM_DARK = (58, 156, 196, 255)
GOLD = (255, 200, 60, 255)
GOLD_DEEP = (214, 140, 34, 255)
BONE = (242, 232, 206, 255)
BONE_SHADE = (198, 182, 150, 255)
RED = (212, 58, 48, 255)
RED_LIGHT = (240, 98, 72, 255)
RED_DARK = (150, 34, 36, 255)

# Wand: sprite is 128x400; the crystal's centre is the pivot
WAND_SIZE = (128, 400)
WAND_TIP = (63, 60)

# Meter: sprite size (2x of the 740x88 reference px it is shown at) and the fill's x range
METER_SIZE = (1480, 176)
FILL_X = (70, 1410)


def wand():
    w, h = WAND_SIZE
    s = Sheet(w, h)

    # crystal (behind the prongs)
    T, UR, LR, B, LL, UL, C = (64, 10), (90, 40), (86, 88), (61, 110), (38, 86), (40, 36), (57, 60)
    gem = s.mask(wobble(densify([T, UR, LR, B, LL, UL], 1.0), 0.7, 11))
    s.paint(gem, GEM)
    s.rgba[gem & s.mask([T, UL, LL, C])] = GEM_LIGHT
    s.rgba[gem & s.mask([C, LL, B, LR])] = GEM_DARK
    s.line(wobble(densify([T, C], 1.0, False), 0.5, 3, closed=False), 2.2)
    s.line(wobble(densify([LL, C, LR], 1.0, False), 0.5, 4, closed=False), 2.2)
    s.rgba[s.mask([(47, 30), (55, 20), (58, 24), (51, 42)])] = (255, 255, 255, 255)
    s.edge(gem, 2.6, seed=5)

    # staff: a crooked branch, thick at the bottom
    spine = bezier((70, 396), (58, 320), (70, 210), (62, 116), 80)
    staff = s.mask(stroke(spine, 26, 18, seed=21, amp=1.0))
    knot = s.ellipse(56, 252, 8, 6, 0.6, 22)
    twig = s.mask(stroke(bezier((66, 206), (74, 198), (80, 192), (86, 180), 20), 8, 3, seed=23, amp=0.4))
    wood = staff | knot | twig
    # prongs curling up around the crystal
    prongs = [
        s.mask(stroke(bezier((60, 126), (40, 116), (30, 92), (36, 64), 50), 12, 5, seed=31, amp=0.5)),
        s.mask(stroke(bezier((66, 124), (88, 114), (98, 92), (92, 66), 50), 12, 5, seed=32, amp=0.5)),
        s.mask(stroke(bezier((63, 122), (60, 112), (54, 104), (50, 98), 20), 8, 4, seed=33, amp=0.3)),
    ]
    for p in prongs:
        wood |= p
    s.paint(wood, WOOD)
    s.rgba[band(wood, 4, 0)] = WOOD_LIGHT       # lit from the left
    s.rgba[band(wood, -6, 0) & ~band(wood, 4, 0)] = WOOD_DARK
    # grain + knot hole
    for pts in ([(64, 300), (63, 318), (65, 332)], [(70, 160), (69, 176)], [(62, 370), (63, 384)]):
        s.line(densify(pts, 1.0, False), 1.6, WOOD_DARK)
    s.rgba[s.ellipse(56, 252, 3, 2.2) & wood] = WOOD_DARK
    for p in prongs:
        s.edge(p, 2.2, seed=7)
    s.edge(staff, 2.4, seed=8)
    s.edge(twig, 1.8, seed=9)

    s.outline(3.2, seed=12)
    return s.image()


def meter_frame():
    """Wooden plank with a dark groove the fill runs in. Not sliced: drawn at the bar's exact shape."""
    w, h = METER_SIZE
    s = Sheet(w, h)
    plank = s.mask(wobble(round_rect_pts(18, 26, w - 18, h - 22, 34, 3), 4.0, 41, wavelength=60))
    # a few chips knocked out of the edge, like the parchment cards
    for cx, cy, r in ((220, 26, 10), (930, h - 22, 12), (1300, 26, 9), (18, 70, 9)):
        plank &= ~s.ellipse(cx, cy, r, r * 0.8, 1.5, cx)
    s.paint(plank, WOOD)
    s.rgba[band(plank, 0, 16)] = WOOD_LIGHT
    s.rgba[band(plank, 0, -14)] = WOOD_DARK
    # grain lines along the plank
    for i, (y, x0, x1) in enumerate(((40, 120, 560), (44, 780, 1240), (h - 36, 260, 700), (h - 40, 1000, 1380))):
        s.line(wobble(densify([(x0, y), (x1, y)], 3, False), 1.5, 50 + i, 90, closed=False), 3, WOOD_DARK)
    s.edge(plank, 4.5, seed=42)

    groove = s.mask(wobble(round_rect_pts(FILL_X[0] - 8, 56, FILL_X[1] + 8, h - 50, 24, 3), 2.0, 43, wavelength=70))
    s.paint(groove, (46, 30, 44, 255))
    s.rgba[band(groove, 0, 12)] = (30, 19, 30, 255)
    s.edge(groove, 3.5, seed=44)

    # brass rivets at the ends
    for cx in (44, w - 44):
        r = s.ellipse(cx, h / 2 + 2, 11, 11, 0.6, cx)
        s.paint(r, GOLD)
        s.rgba[band(r, 0, -4)] = GOLD_DEEP
        s.rgba[s.ellipse(cx - 4, h / 2 - 3, 3, 3)] = (255, 246, 210, 255)
        s.edge(r, 2.5, seed=cx)

    s.outline(5.5, seed=45)
    return s.image()


def meter_fill():
    """Green fill on the same canvas as the frame, so it is stretched over it and Filled from the right."""
    w, h = METER_SIZE
    s = Sheet(w, h)
    fill = s.mask(wobble(round_rect_pts(FILL_X[0], 62, FILL_X[1], h - 56, 20, 3), 1.2, 61, wavelength=80))
    s.paint(fill, (120, 200, 66, 255))
    # light band with a wavy, painted lower edge
    ys = np.arange(s.H)[:, None] / 4
    xs = np.arange(s.W)[None, :] / 4
    light = fill & (ys < 62 + 15 + 3 * np.sin(xs / 41.0) + 2 * np.sin(xs / 13.0 + 1))
    s.rgba[light] = (176, 232, 104, 255)
    s.rgba[band(fill, 0, -10)] = (74, 148, 50, 255)
    # little painted shine dashes
    for x, ln in ((180, 40), (640, 22), (1100, 30)):
        s.line([(x, 71), (x + ln, 71)], 5, (226, 252, 180, 255))
    return s.image()


def skull():
    n = 128
    s = Sheet(n, n)
    head = s.ellipse(64, 54, 44, 40, 1.2, 71)
    jaw = s.mask(wobble(round_rect_pts(36, 74, 92, 112, 14, 1.5), 1.0, 72))
    bone = head | jaw
    s.paint(bone, BONE)
    s.rgba[band(bone, -8, -6)] = BONE_SHADE
    for cx, sd in ((46, 73), (82, 74)):
        s.rgba[s.ellipse(cx, 62, 12, 13, 1.2, sd)] = INK
    s.rgba[s.mask([(64, 76), (58, 88), (70, 88)])] = INK
    for x in (52, 64, 76):
        s.line([(x, 96), (x + 0.5, 110)], 3)
    s.line(wobble(densify([(70, 16), (66, 26), (74, 34), (70, 42)], 1, False), 0.6, 75, closed=False), 2.5)
    s.rgba[s.ellipse(40, 32, 7, 5)] = (255, 255, 255, 255)
    s.edge(bone, 2.5, seed=76)
    s.outline(4.0, seed=77)
    return s.image()


def flag():
    w, h = 88, 128
    s = Sheet(w, h)
    pole = s.mask(stroke(bezier((16, 124), (15, 90), (17, 50), (15, 14), 30), 9, 7, seed=81, amp=0.4))
    s.paint(pole, WOOD)
    s.rgba[band(pole, -3, 0)] = WOOD_DARK
    s.edge(pole, 1.8, seed=82)
    cloth_pts = [(18, 16), (52, 12), (80, 22), (66, 38), (82, 56), (52, 58), (34, 64), (18, 60)]
    cloth = s.mask(wobble(densify(cloth_pts, 1.0), 1.2, 83))
    s.paint(cloth, RED)
    s.rgba[band(cloth, 0, 7)] = RED_LIGHT
    s.rgba[band(cloth, 0, -6)] = RED_DARK
    s.line([(34, 30), (44, 46)], 2, RED_DARK)
    s.edge(cloth, 2.0, seed=84)
    knob = s.ellipse(15, 12, 8, 8, 0.4, 85)
    s.paint(knob, GOLD)
    s.rgba[band(knob, -3, -3)] = GOLD_DEEP
    s.edge(knob, 1.8, seed=86)
    s.outline(3.0, seed=87)
    return s.image()
