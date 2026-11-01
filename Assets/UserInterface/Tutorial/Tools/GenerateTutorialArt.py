"""Generates every sprite the tutorial uses into Assets/UserInterface/Tutorial/Art.

Run from anywhere:  python Assets/UserInterface/Tutorial/Tools/GenerateTutorialArt.py
Style matches the game's chunky "CC" UI: navy ink outlines, cream / gold fills, soft glows.
The wand and wave meter come from handdrawn_art.py (wobbly ink + cel shading, like the painted heroes).
Shapes are drawn 4x larger and scaled down so every edge is anti-aliased.
"""
import math
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
TUT = os.path.dirname(HERE)
UI = os.path.dirname(TUT)            # Assets/UserInterface
ASSETS = os.path.dirname(UI)
REPO = os.path.dirname(ASSETS)
ART = os.path.join(TUT, "Art")
FONT = os.path.join(UI, "Shared", "Fonts", "LilitaOne-Regular.ttf")

INK = (29, 21, 51, 255)
CREAM = (255, 244, 222, 255)
GOLD = (255, 200, 60, 255)
GOLD_DEEP = (238, 140, 24, 255)
WOOD = (122, 70, 44, 255)
RED = (230, 62, 56, 255)
SKY = (120, 200, 255, 255)
SS = 4  # supersampling


def save(img, *path):
    out = os.path.join(ART, *path)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    img.save(out)
    print("wrote", os.path.relpath(out, ASSETS))


def canvas(w, h):
    return Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))


def down(img, w, h):
    return img.resize((w, h), Image.LANCZOS)


def rr(d, box, r, fill):
    d.rounded_rectangle([v * SS for v in box], radius=r * SS, fill=fill)


# ---------------------------------------------------------------- distance-field helpers

def grid(size):
    ys, xs = np.mgrid[0:size, 0:size].astype(np.float32) + 0.5
    return xs, ys


def sd_round_rect(xs, ys, x0, y0, x1, y1, r):
    """Signed distance to a rounded rect (negative inside)."""
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    hx, hy = (x1 - x0) / 2 - r, (y1 - y0) / 2 - r
    qx = np.abs(xs - cx) - hx
    qy = np.abs(ys - cy) - hy
    outside = np.hypot(np.maximum(qx, 0), np.maximum(qy, 0))
    inside = np.minimum(np.maximum(qx, qy), 0)
    return outside + inside - r


def white_alpha(alpha):
    a = (np.clip(alpha, 0, 1) * 255).astype(np.uint8)
    rgb = np.full(a.shape + (3,), 255, np.uint8)
    return Image.fromarray(np.dstack([rgb, a]), "RGBA")


# ---------------------------------------------------------------- dog

def dog():
    src = Image.open(os.path.join(REPO, "ArtSource", "Dog-Hero.webp")).convert("RGBA")
    src = src.crop(src.getbbox())
    # 2x so he stays crisp at the size he's shown; a light unsharp keeps the ink lines bold
    big = src.resize((src.width * 2, src.height * 2), Image.LANCZOS)
    rgb = big.convert("RGB").filter(ImageFilter.UnsharpMask(radius=2, percent=60, threshold=2))
    big = Image.merge("RGBA", (*rgb.split(), big.getchannel("A")))
    pad = Image.new("RGBA", (big.width + 16, big.height + 8), (0, 0, 0, 0))
    pad.alpha_composite(big, (8, 8))
    save(pad, "Dog", "DogGuide.png")

    # soft oval shadow he stands on
    xs, ys = grid(128)
    d = np.hypot((xs - 64) / 60, (ys - 64) / 22)
    a = np.clip(1 - d, 0, 1) ** 1.4
    img = white_alpha(a)
    save(img, "Dog", "DogShadow.png")


# ---------------------------------------------------------------- speech bubble

def bubble():
    w = h = 256
    img = canvas(w, h)
    d = ImageDraw.Draw(img)
    rr(d, (2, 2, w - 2, h - 2), 66, INK)
    rr(d, (12, 12, w - 12, h - 12), 56, CREAM)
    # soft glossy band at the top
    hl = Image.new("RGBA", (w * SS, h * SS), (255, 255, 255, 0))  # white, so the blur doesn't go grey
    hd = ImageDraw.Draw(hl)
    rr(hd, (26, 22, w - 26, 70), 26, (255, 255, 255, 80))
    hl = hl.filter(ImageFilter.GaussianBlur(3 * SS))
    img.alpha_composite(hl)
    # keep the highlight inside the cream
    mask = canvas(w, h)
    rr(ImageDraw.Draw(mask), (12, 12, w - 12, h - 12), 56, (255, 255, 255, 255))
    out = canvas(w, h)
    rr(ImageDraw.Draw(out), (2, 2, w - 2, h - 2), 66, INK)
    out.paste(img, (0, 0), mask)
    save(down(out, w, h), "UI", "Bubble.png")

    # flat silhouette for the drop shadow
    sh = canvas(w, h)
    rr(ImageDraw.Draw(sh), (2, 2, w - 2, h - 2), 66, (255, 255, 255, 255))
    save(down(sh, w, h), "UI", "BubbleShadow.png")


def bubble_tail():
    # Points down, leaning right. The top edge has no outline so it covers the bubble's
    # border where the two meet.
    w, h = 128, 112
    img = canvas(w, h)
    d = ImageDraw.Draw(img)
    s = SS
    d.polygon([(8 * s, 0), (100 * s, 0), (114 * s, 110 * s)], fill=INK)
    d.polygon([(22 * s, 0), (85 * s, 0), (105 * s, 88 * s)], fill=CREAM)
    save(down(img, w, h), "UI", "BubbleTail.png")


def next_arrow():
    w, h = 48, 40
    img = canvas(w, h)
    d = ImageDraw.Draw(img)
    s = SS
    d.polygon([(4 * s, 4 * s), (44 * s, 4 * s), (24 * s, 36 * s)], fill=INK)
    d.polygon([(13 * s, 10 * s), (35 * s, 10 * s), (24 * s, 27 * s)], fill=GOLD)
    save(down(img, w, h), "UI", "NextArrow.png")


# ---------------------------------------------------------------- wand pointer

def star_points(cx, cy, r_out, r_in, n=5, rot=-90):
    pts = []
    for i in range(n * 2):
        r = r_out if i % 2 == 0 else r_in
        a = math.radians(rot + i * 180 / n)
        pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return pts


# ---------------------------------------------------------------- highlights

def glow_frame():
    """Bright rounded outline with a soft outer glow (9-sliced, tinted gold in Unity)."""
    n = 160
    xs, ys = grid(n)
    sd = sd_round_rect(xs, ys, 56, 56, n - 56, n - 56, 26)
    line = np.exp(-(sd / 3.2) ** 2)
    outer = np.where(sd > 0, np.exp(-(sd / 18.0) ** 2) * 0.55, np.exp(-(sd / 7.0) ** 2) * 0.35)
    save(white_alpha(np.maximum(line, outer)), "UI", "GlowFrame.png")


def tile_glow():
    n = 256
    xs, ys = grid(n)
    sd = sd_round_rect(xs, ys, 22, 22, n - 22, n - 22, 34)
    line = np.exp(-(sd / 5.5) ** 2)
    inner = np.where(sd < 0, np.clip(1 + sd / 70.0, 0, 1) ** 2 * 0.45, 0)
    outer = np.where(sd > 0, np.exp(-(sd / 10.0) ** 2) * 0.5, 0)
    save(white_alpha(np.maximum(np.maximum(line, inner), outer)), "UI", "TileGlow.png")


def spot_hole():
    """Opaque everywhere except a feathered rounded hole in the middle (9-sliced)."""
    n = 256
    xs, ys = grid(n)
    sd = sd_round_rect(xs, ys, 76, 76, n - 76, n - 76, 36)
    a = np.clip(sd / 36.0, 0, 1)
    a = a * a * (3 - 2 * a)
    save(white_alpha(a), "UI", "SpotHole.png")


def chevron():
    w, h = 96, 96
    img = canvas(w, h)
    d = ImageDraw.Draw(img)
    s = SS
    pts = [(62, 10), (86, 10), (52, 48), (86, 86), (62, 86), (28, 48)]  # points left
    d.line([(x * s, y * s) for x, y in [(70, 14), (34, 48), (70, 82)]], fill=INK, width=34 * s, joint="curve")
    for x, y in [(70, 14), (70, 82), (34, 48)]:
        d.ellipse([(x - 17) * s, (y - 17) * s, (x + 17) * s, (y + 17) * s], fill=INK)
    d.line([(x * s, y * s) for x, y in [(70, 14), (34, 48), (70, 82)]], fill=(255, 255, 255, 255), width=18 * s, joint="curve")
    for x, y in [(70, 14), (70, 82), (34, 48)]:
        d.ellipse([(x - 9) * s, (y - 9) * s, (x + 9) * s, (y + 9) * s], fill=(255, 255, 255, 255))
    save(down(img, w, h), "UI", "Chevron.png")


# ---------------------------------------------------------------- emotes

def glyph_emote(ch, fill, name):
    n = 128
    img = canvas(n, n)
    d = ImageDraw.Draw(img)
    font = ImageFont.truetype(FONT, 104 * SS)
    d.text((n * SS / 2, n * SS / 2 + 4 * SS), ch, font=font, anchor="mm", fill=fill,
           stroke_width=11 * SS, stroke_fill=INK)
    save(down(img, n, n), "UI", name)


def heart():
    n = 128
    img = canvas(n, n)
    d = ImageDraw.Draw(img)
    s = SS

    def heart_pts(cx, cy, k):
        pts = []
        for i in range(120):
            t = i / 120 * 2 * math.pi
            x = 16 * math.sin(t) ** 3
            y = 13 * math.cos(t) - 5 * math.cos(2 * t) - 2 * math.cos(3 * t) - math.cos(4 * t)
            pts.append(((cx + x * k) * s, (cy - y * k) * s))
        return pts

    d.polygon(heart_pts(64, 66, 3.2), fill=INK)
    d.polygon(heart_pts(64, 64, 2.55), fill=RED)
    d.ellipse([36 * s, 40 * s, 52 * s, 54 * s], fill=(255, 190, 180, 255))
    save(down(img, n, n), "UI", "EmoteHeart.png")


def sweat():
    n = 128
    img = canvas(n, n)
    d = ImageDraw.Draw(img)
    s = SS

    def drop(cx, cy, r):
        pts = [(cx, cy - r * 1.9)]
        for i in range(0, 181, 6):
            a = math.radians(-20 + i * (220 / 180))
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
        return [(x * s, y * s) for x, y in pts]

    d.polygon(drop(64, 76, 36), fill=INK)
    d.polygon(drop(64, 76, 26), fill=SKY)
    d.ellipse([48 * s, 70 * s, 60 * s, 88 * s], fill=(235, 250, 255, 255))
    save(down(img, n, n), "UI", "EmoteSweat.png")


def sparkle_emote():
    n = 128
    img = canvas(n, n)
    d = ImageDraw.Draw(img)
    s = SS
    d.polygon(star_points(64 * s, 64 * s, 60 * s, 18 * s, n=4), fill=INK)
    d.polygon(star_points(64 * s, 64 * s, 48 * s, 12 * s, n=4), fill=GOLD)
    d.polygon(star_points(100 * s, 26 * s, 24 * s, 8 * s, n=4), fill=INK)
    d.polygon(star_points(100 * s, 26 * s, 16 * s, 5 * s, n=4), fill=(255, 250, 220, 255))
    save(down(img, n, n), "UI", "EmoteSparkle.png")


# ---------------------------------------------------------------- fx

def fx():
    xs, ys = grid(64)
    r = np.hypot(xs - 32, ys - 32) / 32
    core = np.clip((0.3 - r) / 0.12, 0, 1)
    halo = np.clip(1 - r, 0, 1) ** 1.8 * 0.6
    save(white_alpha(np.maximum(core, halo)), "Fx", "SoftGlow.png")

    dx, dy = np.abs(xs - 32) / 32, np.abs(ys - 32) / 32
    star = dx ** 0.45 + dy ** 0.45
    save(white_alpha(np.clip((1.02 - star) / 0.1, 0, 1)), "Fx", "Sparkle.png")

    xs2, ys2 = grid(128)
    r2 = np.hypot(xs2 - 64, ys2 - 64) / 64
    save(white_alpha(np.clip(1 - np.abs(r2 - 0.86) / 0.08, 0, 1)), "Fx", "Ring.png")

    # puffy dust cloud (soft but with a defined edge, reads as cel-shaded smoke)
    xs3, ys3 = grid(96)
    r3 = np.hypot(xs3 - 48, ys3 - 48) / 46
    save(white_alpha(np.clip((1 - r3) / 0.18, 0, 1)), "Fx", "Puff.png")

    # small chunky gold star (celebration confetti)
    n = 48
    img = canvas(n, n)
    d = ImageDraw.Draw(img)
    d.polygon(star_points(24 * SS, 25 * SS, 22 * SS, 10 * SS), fill=INK)
    d.polygon(star_points(24 * SS, 25 * SS, 15 * SS, 7 * SS), fill=GOLD)
    save(down(img, n, n), "Fx", "StarChunk.png")


# ---------------------------------------------------------------- hand-drawn pieces

def hand_drawn():
    """Wand + wave meter, drawn with wobbly ink and cel shading to match the game's painted art."""
    import sys
    sys.path.insert(0, HERE)
    import handdrawn_art as hd
    save(hd.wand(), "UI", "Wand.png")
    save(hd.meter_frame(), "Hud", "MeterFrame.png")
    save(hd.meter_fill(), "Hud", "MeterFill.png")
    save(hd.skull(), "Hud", "SkullToken.png")
    save(hd.flag(), "Hud", "WaveFlag.png")


if __name__ == "__main__":
    dog()
    bubble()
    bubble_tail()
    next_arrow()
    glow_frame()
    tile_glow()
    spot_hole()
    chevron()
    glyph_emote("!", GOLD, "EmoteExclaim.png")
    glyph_emote("?", SKY, "EmoteQuestion.png")
    heart()
    sweat()
    sparkle_emote()
    hand_drawn()
    fx()
