"""In-level tower cards in the level's art style: chunky cel-shaded stone frame (like the bridge battlements),
keeper-coloured panel with a soft light behind the hero, full hero art whose head pokes over the top stones.

    python AgentScripts/GenerateStoneCards.py [outDir]

Writes <outDir>/<Tower>-Card.png (default: Assets/UserInterface/LevelHUD/Art/Cards, same files/GUIDs as before,
so Tower.cardArt keeps pointing at them). The old parchment originals stay in ArtSource/*-Card.png.
"""
import os
import random
import sys

from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HEROES = os.path.join(ROOT, "Assets", "UserInterface", "Shared", "Art", "Heroes")
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "Assets", "UserInterface", "LevelHUD", "Art", "Cards")

S = 3                      # supersample, shrunk at the end
W, H = 660, 420            # final size (cell is 330x210)
TOP = 34                   # empty headroom above the frame for the hero's head
RAD = 34
OUTLINE = 7
BAND = 30                  # stone band thickness

INK = (34, 26, 44, 255)
MORTAR = (64, 58, 66, 255)
STONE = [(156, 151, 146), (146, 141, 137), (164, 158, 150), (140, 136, 134)]
STONE_LIGHT = (196, 191, 182)
STONE_DARK = (108, 103, 102)

# tower name, hero art, accent (same as the keeper accents in MenuBuilder.CharacterSelect)
CARDS = [
    ("Knight", "Dog-Hero", (240, 88, 64)),
    ("Archer", "Ferret-Hero", (98, 190, 84)),
    ("Cleric", "Goat-Hero", (240, 186, 60)),
    ("Assassin", "Mouse-Hero", (176, 104, 230)),
    ("Wizard", "Cat-Hero", (80, 146, 240)),
]


def mix(a, b, t):
    return tuple(int(round(a[i] + (b[i] - a[i]) * t)) for i in range(3)) + (255,)


def rr_mask(size, box, radius):
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).rounded_rectangle(box, radius, fill=255)
    return m


def stone_band(size, outer, inner, seed):
    """Stone blocks filling outer minus inner: flat fill, lit top edge, shaded bottom edge, dark mortar."""
    rnd = random.Random(seed)
    w, h = size
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    x0, y0, x1, y1 = outer
    band = BAND * S
    m = 3 * S  # mortar gap

    def block(bx0, by0, bx1, by1):
        c = rnd.choice(STONE)
        r = 6 * S
        d.rounded_rectangle((bx0, by0, bx1, by1), r, fill=MORTAR)
        d.rounded_rectangle((bx0 + m, by0 + m, bx1 - m, by1 - m), r, fill=STONE_DARK)
        d.rounded_rectangle((bx0 + m, by0 + m, bx1 - m, by1 - m - 5 * S), r, fill=c + (255,))
        d.rounded_rectangle((bx0 + m, by0 + m, bx1 - m, by0 + m + 6 * S), r // 2, fill=STONE_LIGHT + (255,))
        d.rectangle((bx0 + m + r // 2, by0 + m + 4 * S, bx1 - m - r // 2, by0 + m + 8 * S), fill=c + (255,))
        # a chip or crack now and then
        if rnd.random() < 0.35:
            cx = rnd.randint(bx0 + 3 * m, max(bx0 + 3 * m, bx1 - 3 * m))
            cy = by1 - m - 9 * S
            d.line((cx, cy, cx + 6 * S, cy - 5 * S, cx + 11 * S, cy - 3 * S), fill=STONE_DARK, width=2 * S)

    def row(ry0, ry1, rx0, rx1, offset):
        x = rx0 - offset
        while x < rx1:
            bw = rnd.randint(70, 110) * S
            block(max(x, rx0 - 20 * S), ry0, min(x + bw, rx1 + 20 * S), ry1)
            x += bw

    def col(cx0, cx1, cy0, cy1):
        y = cy0
        while y < cy1:
            bh = rnd.randint(52, 74) * S
            block(cx0, max(y, cy0), cx1, min(y + bh, cy1))
            y += bh

    row(y0, y0 + band, x0, x1, rnd.randint(0, 40) * S)
    row(y1 - band, y1, x0, x1, rnd.randint(0, 40) * S)
    col(x0, x0 + band, y0 + band, y1 - band)
    col(x1 - band, x1, y0 + band, y1 - band)

    keep = ImageChops.subtract(rr_mask(size, outer, RAD * S), rr_mask(size, inner, (RAD - BAND + 10) * S))
    img.putalpha(ImageChops.multiply(img.getchannel("A"), keep))
    return img


def panel(size, inner, accent):
    w, h = size
    x0, y0, x1, y1 = inner
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    base = mix(accent, (40, 34, 60), 0.35)
    light = mix(accent, (255, 248, 225), 0.25)
    # cel two-tone: lit upper part, shaded lower part
    d.rectangle(inner, fill=base)
    split = y0 + int((y1 - y0) * 0.58)
    d.rectangle((x0, y0, x1, split), fill=mix(accent, (40, 34, 60), 0.12))
    # soft light behind the hero
    glow = Image.new("L", size, 0)
    cx = (x0 + x1) // 2
    ImageDraw.Draw(glow).ellipse((cx - 170 * S, y0 - 40 * S, cx + 170 * S, y1 - 20 * S), fill=170)
    glow = glow.filter(ImageFilter.GaussianBlur(40 * S))
    img = Image.composite(Image.new("RGBA", size, light), img, glow)
    d = ImageDraw.Draw(img)
    # faint sun rays
    rays = Image.new("L", size, 0)
    rd = ImageDraw.Draw(rays)
    for i in range(-5, 6, 2):
        rd.polygon([(cx, y1), (cx + i * 70 * S - 22 * S, y0 - 60 * S), (cx + i * 70 * S + 22 * S, y0 - 60 * S)], fill=34)
    img = Image.composite(Image.new("RGBA", size, (255, 255, 255, 255)), img, rays)
    # ground shadow where the hero stands
    sh = Image.new("L", size, 0)
    ImageDraw.Draw(sh).ellipse((cx - 120 * S, y1 - 46 * S, cx + 120 * S, y1 + 10 * S), fill=120)
    sh = sh.filter(ImageFilter.GaussianBlur(6 * S))
    img = Image.composite(Image.new("RGBA", size, mix(accent, (20, 14, 30), 0.7)), img, sh)
    img.putalpha(rr_mask(size, inner, (RAD - BAND + 10) * S))
    return img


def inner_shadow(size, inner):
    """Dark lip just inside the stones so the panel reads as set back."""
    m = rr_mask(size, inner, (RAD - BAND + 10) * S)
    edge = ImageChops.subtract(m, m.filter(ImageFilter.GaussianBlur(5 * S)).point(lambda v: 255 if v > 250 else 0))
    edge = edge.filter(ImageFilter.GaussianBlur(3 * S))
    edge = ImageChops.multiply(edge, m)
    img = Image.new("RGBA", size, (20, 14, 30, 0))
    img.putalpha(edge.point(lambda v: int(v * 0.7)))
    return img


def build(name, hero_file, accent, seed):
    size = (W * S, (H + TOP) * S)
    outer = (OUTLINE * S, (TOP + OUTLINE) * S, (W - OUTLINE) * S, (H + TOP - OUTLINE) * S)
    inner = (outer[0] + BAND * S, outer[1] + BAND * S, outer[2] - BAND * S, outer[3] - BAND * S)
    card = Image.new("RGBA", size, (0, 0, 0, 0))

    # drop shadow + ink outline
    sil = rr_mask(size, (0, TOP * S, W * S, (H + TOP) * S), (RAD + OUTLINE) * S)
    shadow = Image.new("RGBA", size, (0, 0, 0, 0))
    shadow.paste((10, 6, 18, 110), (0, 0), sil.filter(ImageFilter.GaussianBlur(4 * S)))
    card.alpha_composite(shadow, (0, 6 * S))
    card.paste(INK, (0, 0), sil)

    card.alpha_composite(panel(size, inner, accent))
    card.alpha_composite(inner_shadow(size, inner))

    # hero: as tall as the panel plus the headroom, head over the top stones, feet tucked behind the bottom stones
    hero = Image.open(os.path.join(HEROES, hero_file + ".png")).convert("RGBA")
    hero = hero.crop(hero.getbbox())
    target_h = (inner[3] - inner[1]) + (BAND + TOP) * S - 4 * S
    max_w = (inner[2] - inner[0]) - 30 * S
    scale = min(target_h / hero.height, max_w / hero.width)
    hero = hero.resize((int(hero.width * scale), int(hero.height * scale)), Image.LANCZOS)
    hx = (size[0] - hero.width) // 2
    hy = inner[3] + 14 * S - hero.height
    hero_layer = Image.new("RGBA", size, (0, 0, 0, 0))
    hero_layer.paste(hero, (hx, hy), hero)
    # keep the hero inside the panel's sides; only the head may cross the top band
    clip = Image.new("L", size, 0)
    ImageDraw.Draw(clip).rectangle((inner[0], 0, inner[2], inner[3]), fill=255)
    hero_layer.putalpha(ImageChops.multiply(hero_layer.getchannel("A"), clip))

    stones = stone_band(size, outer, inner, seed)
    card.alpha_composite(stones)
    # dark outline round the hero where it overlaps the stones, so it pops like the level art
    ha = hero_layer.getchannel("A")
    rim = ha.filter(ImageFilter.MaxFilter(2 * 4 + 1))
    rim_img = Image.new("RGBA", size, INK)
    rim_img.putalpha(rim)
    card.alpha_composite(rim_img)
    card.alpha_composite(hero_layer)
    # bottom stones go back over the feet
    bottom = stones.copy()
    cut = Image.new("L", size, 0)
    ImageDraw.Draw(cut).rectangle((0, inner[3] - 2 * S, size[0], size[1]), fill=255)
    bottom.putalpha(ImageChops.multiply(bottom.getchannel("A"), cut))
    card.alpha_composite(bottom)

    card = card.resize((W, H + TOP), Image.LANCZOS)
    card = card.crop(card.getbbox())
    path = os.path.join(OUT, name + "-Card.png")
    card.save(path)
    return path, card.size


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    for i, (name, hero, accent) in enumerate(CARDS):
        print(build(name, hero, accent, 11 + i * 7))
