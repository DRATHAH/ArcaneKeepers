"""Cuts the in-level tower cards out of their black backgrounds.

python AgentScripts/CutoutCards.py
  reads  Assets/UserInterface/<Art>-Card.png  (RGB, parchment card on black)
  writes Assets/UserInterface/Cards/<Tower>-Card.png (RGBA, cropped, anti-aliased edge)
The Assassin has no card art, so its card is a blank parchment (character painted out of
the Goat card) with Heroes/Mouse-Hero.png on top.
"""
import os
import numpy as np
from PIL import Image
from scipy import ndimage

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "UserInterface")
OUT = os.path.join(ROOT, "Cards")
WIDTH = 640  # cells are 275 wide at 1080 ref; 640 keeps it crisp on 2-3x phones

CARDS = {"Archer": "Archer-Card", "Wizard": "Cat-Card", "Cleric": "Goat-Card", "Knight": "wolf-Card"}


def cutout(rgb):
    """Returns RGBA float image with the border-connected black removed and a soft edge."""
    a = rgb.astype(np.float32) / 255.0
    bright = a.max(axis=2)
    dark = bright < 0.16
    labels, _ = ndimage.label(dark)
    edge_ids = np.unique(np.concatenate([labels[0], labels[-1], labels[:, 0], labels[:, -1]]))
    bg = np.isin(labels, edge_ids[edge_ids > 0])
    # The card's own dark outline also touches the black, so grow the hard mask back a little
    # and fade the ring between by brightness (black matte -> alpha, then un-premultiply).
    band = bg & ndimage.binary_dilation(~bg, iterations=3)
    alpha = np.where(bg, 0.0, 1.0)
    ramp = np.clip((bright - 0.04) / 0.30, 0.0, 1.0)
    alpha[band] = ramp[band]
    alpha = np.clip(ndimage.gaussian_filter(alpha, 0.6), 0, 1)
    safe = np.maximum(alpha, 1e-3)[..., None]
    col = np.where(alpha[..., None] < 0.999, np.clip(a / safe, 0, 1), a)
    return np.dstack([col, alpha])


def crop(img, pad=4):
    ys, xs = np.nonzero(img[..., 3] > 0.02)
    y0, y1 = max(ys.min() - pad, 0), min(ys.max() + pad + 1, img.shape[0])
    x0, x1 = max(xs.min() - pad, 0), min(xs.max() + pad + 1, img.shape[1])
    return img[y0:y1, x0:x1]


def save(img, name):
    im = Image.fromarray((img * 255 + 0.5).astype(np.uint8), "RGBA")
    h = round(im.height * WIDTH / im.width)
    im.resize((WIDTH, h), Image.LANCZOS).save(os.path.join(OUT, name + "-Card.png"))
    print("wrote", name, WIDTH, h)


def blank_card(rgb):
    """Paints the Goat out of its card by mirroring empty parchment in from both sides."""
    a = rgb.astype(np.float32)
    x0, x1, mid = 440, 840, 640
    out = a.copy()
    for x in range(x0, x1):
        if x < mid:
            out[:, x] = a[:, 2 * x0 - x]
        else:
            out[:, x] = a[:, 2 * x1 - x]
    # Blend the seam in the middle so the mirrored halves meet softly
    for x in range(mid - 30, mid + 30):
        t = (x - (mid - 30)) / 60.0
        left = a[:, 2 * x0 - x]
        right = a[:, 2 * x1 - x]
        out[:, x] = left * (1 - t) + right * t
    return out.clip(0, 255).astype(np.uint8)


def main():
    os.makedirs(OUT, exist_ok=True)
    for tower, art in CARDS.items():
        rgb = np.array(Image.open(os.path.join(ROOT, art + ".png")).convert("RGB"))
        save(crop(cutout(rgb)), tower)

    # Assassin: blank parchment + Mouse hero, sized like the other characters (~64% of card height)
    goat = np.array(Image.open(os.path.join(ROOT, "Goat-Card.png")).convert("RGB"))
    card = cutout(blank_card(goat))
    base = Image.fromarray((card * 255 + 0.5).astype(np.uint8), "RGBA")
    hero = Image.open(os.path.join(ROOT, "Heroes", "Mouse-Hero.png")).convert("RGBA")
    hero = hero.crop(hero.getbbox())
    target_h = 480
    hero = hero.resize((round(hero.width * target_h / hero.height), target_h), Image.LANCZOS)
    cx, feet = 672, 612
    base.alpha_composite(hero, (cx - hero.width // 2, feet - target_h))
    save(crop(np.array(base).astype(np.float32) / 255.0), "Assassin")


if __name__ == "__main__":
    main()
