"""Re-lays the castle brick inside the placement grid so the mortar seams sit exactly on the grid
lines (the ivy grid then grows along real seams instead of across bricks).

  python AgentScripts/GenerateDeckPaving.py <ArcaneKeepers project root>

Bricks are cut from Assets/Materials/CastleBrick.png (the bridge's own texture) so colour, speckle
and outline weight match. Each 2x2 cell = two courses 1 unit wide running along z (like the deck):
one course is a full 2-unit brick, the other two half bricks; the two swap per cell.
Image x = world x (left = grid x0), image y = world z (top = far side). Placed by
AgentScripts/PlaceBridgeVines.cs as Level/BridgeVines/Paving (URP Lit quad, same look as blinn1).
"""
import os, random, sys
import numpy as np
from PIL import Image

GX0, GZ0, COLS, ROWS, CELL = -9.16, -5.04, 10, 5, 2.0
PPU = 64
MORTAR = 0.1  # units


def cut_bricks(path):
    im = Image.open(path).convert("RGB")
    a = np.asarray(im.convert("L")).astype(float)
    rows = a.mean(1)
    dark_r = rows < rows.mean() - 15
    bands, start = [], None
    for y in range(a.shape[0]):
        if not dark_r[y] and start is None:
            start = y
        if dark_r[y] and start is not None:
            if y - start > 30:
                bands.append((start + 1, y - 1))
            start = None
    bricks = []
    for y0, y1 in bands:
        cols = a[y0:y1].mean(0)
        dark_c = cols < cols.mean() - 25
        s = None
        for x in range(a.shape[1]):
            if not dark_c[x] and s is None:
                s = x
            if dark_c[x] and s is not None:
                if x - s > 70:
                    bricks.append(im.crop((s + 1, y0, x - 1, y1)))
                s = None
    # mortar colour = darkest band median
    mortar = tuple(int(c) for c in np.median(np.asarray(im)[dark_r].reshape(-1, 3), axis=0)) if dark_r.any() else (40, 40, 42)
    return bricks, mortar


def main(root):
    bricks, mortar = cut_bricks(os.path.join(root, "Assets", "Materials", "CastleBrick.png"))
    rnd = random.Random(5)
    W, H = int(COLS * CELL * PPU), int(ROWS * CELL * PPU)
    img = Image.new("RGB", (W, H), mortar)
    m = int(round(MORTAR * PPU / 2))  # half mortar on each side of a brick

    def put(x0, z0, x1, z1):
        """Brick filling world rect (relative to grid origin); long side along z."""
        px0, px1 = int(round(x0 * PPU)) + m, int(round(x1 * PPU)) - m
        py0, py1 = H - int(round(z1 * PPU)) + m, H - int(round(z0 * PPU)) - m
        b = rnd.choice(bricks)
        if (py1 - py0) < (px1 - px0) * 1.3:  # half brick: use a squarer piece of a brick
            w = b.size[1]
            ox = rnd.randint(0, max(0, b.size[0] - w))
            b = b.crop((ox, 0, ox + w, b.size[1]))
        else:
            b = b.rotate(90, expand=True)
        if rnd.random() < 0.5:
            b = b.transpose(Image.FLIP_TOP_BOTTOM)
        img.paste(b.resize((px1 - px0, py1 - py0), Image.LANCZOS), (px0, py0))

    for c in range(COLS):
        for r in range(ROWS):
            x, z = c * CELL, r * CELL
            swap = (c + r) % 2
            for k in range(2):  # two courses per cell, each 1 unit wide in x
                cx0, cx1 = x + k * 1.0, x + (k + 1) * 1.0
                if (k + swap) % 2 == 0:
                    put(cx0, z, cx1, z + CELL)
                else:
                    put(cx0, z, cx1, z + 1.0)
                    put(cx0, z + 1.0, cx1, z + CELL)
    out = os.path.join(root, "Assets", "Sprites", "Vines", "bridge_deck_paving.png")
    img.save(out)
    print("wrote", out, img.size, "bricks", len(bricks), "mortar", mortar)


if __name__ == "__main__":
    main(sys.argv[1])
