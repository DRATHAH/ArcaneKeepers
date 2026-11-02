"""Art for the in-level MOVE button (round gold button, bottom-right, same family as the pause button).

python AgentScripts/GenerateMoveButtonArt.py
  writes Assets/UserInterface/LevelHUD/Art/MoveButton/MoveIcon.png
    white four-way "move" arrows with the dark ink outline used by CC_IconUp / the pause icons,
    a soft grey underside on each arrow and a small centre knob.
The button body is the shared CC_CircleGold; cooldown sweep uses CS_Disc; armed ring uses CS_DashRing.
"""
import math
import os

from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "..", "Assets", "UserInterface", "LevelHUD", "Art", "MoveButton")

INK = (29, 21, 51, 255)
WHITE = (255, 255, 255, 255)
SHADE = (214, 210, 228, 255)
SS = 4  # supersample


def arrow(cx, cy, ang, r0, r1, shaft, head_w, head_len):
    dx, dy = math.cos(ang), math.sin(ang)
    nx, ny = -dy, dx
    hb = r1 - head_len
    p = lambda r, s: (cx + dx * r + nx * s, cy + dy * r + ny * s)
    return [p(r0, -shaft / 2), p(hb, -shaft / 2), p(hb, -head_w / 2), p(r1, 0),
            p(hb, head_w / 2), p(hb, shaft / 2), p(r0, shaft / 2)]


def shape_mask(size, grow):
    """Union of the four arrows + centre knob, grown by `grow` px (for the outline)."""
    S = size * SS
    c = S / 2
    m = Image.new("L", (S, S), 0)
    d = ImageDraw.Draw(m)
    g = grow * SS
    k = SS * size / 256
    for i in range(4):
        a = i * math.pi / 2
        pts = arrow(c, c, a, 0, 112 * k, 30 * k, 92 * k, 48 * k)
        d.polygon(pts, fill=255)
        if g > 0:
            # stroke with round joins = outline of even width that follows the arrow, no gap fill
            ring = pts + [pts[0]]
            d.line(ring, fill=255, width=int(2 * g))
            for x, y in pts:
                d.ellipse((x - g, y - g, x + g, y + g), fill=255)
    return m


def icon(size=256):
    S = size * SS
    c = S / 2
    img = Image.new("RGBA", (S, S), (0, 0, 0, 0))

    outline = shape_mask(size, 9)
    fill = shape_mask(size, 0)
    img.paste(INK, (0, 0), outline)

    # White arrows with a soft grey underside (light from the top, like CC_IconUp)
    white = Image.new("RGBA", (S, S), WHITE)
    shade = Image.new("RGBA", (S, S), SHADE)
    under = fill.copy()
    shifted = Image.new("L", (S, S), 0)
    shifted.paste(fill, (0, -7 * SS))
    under = Image.composite(Image.new("L", (S, S), 0), fill, shifted)  # fill minus shifted-up fill
    img.paste(white, (0, 0), fill)
    img.paste(shade, (0, 0), under)

    # Centre knob: ink ring + gold dot, ties the icon to the gold button
    d = ImageDraw.Draw(img)
    r1, r2 = 30 * SS * size / 256, 20 * SS * size / 256
    d.ellipse((c - r1, c - r1, c + r1, c + r1), fill=INK)
    d.ellipse((c - r2, c - r2, c + r2, c + r2), fill=(255, 205, 64, 255))
    d.ellipse((c - r2 * 0.55, c - r2 * 0.75, c + r2 * 0.15, c - r2 * 0.1), fill=(255, 240, 180, 255))

    img = img.resize((size, size), Image.LANCZOS)
    img.save(os.path.join(OUT, "MoveIcon.png"))
    print("wrote MoveIcon", size)


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    icon()
