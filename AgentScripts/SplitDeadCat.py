# Splits ArtSource/Dead_Cat.webp into a body layer and a head layer (same canvas size, so they
# stack exactly) so the lose screen can animate the head on its own. Also writes the whole picture.
# Usage: python AgentScripts/SplitDeadCat.py   (run from the Unity project root)
import json
import numpy as np
from PIL import Image
from scipy import ndimage

SRC = "ArtSource/Dead_Cat.webp"
OUT = "Assets/UserInterface/LevelEnd/Art/DeadCat/"
HEAD_X = 655  # pieces whose centre lies right of this (source pixels) belong to the head

im = Image.open(SRC).convert("RGBA")
x0, y0, x1, y1 = im.getchannel("A").getbbox()
pad = 6
box = (max(0, x0 - pad), max(0, y0 - pad), min(im.width, x1 + pad), min(im.height, y1 + pad))
im = im.crop(box)
a = np.array(im)
alpha = a[:, :, 3] > 0

labels, n = ndimage.label(alpha, structure=np.ones((3, 3)))
head = np.zeros_like(alpha)
for i, (cy, cx) in enumerate(ndimage.center_of_mass(alpha, labels, range(1, n + 1)), start=1):
    if cx + box[0] > HEAD_X:
        head |= labels == i

def layer(mask):
    b = a.copy()
    b[~mask, 3] = 0
    return Image.fromarray(b)

import os
os.makedirs(OUT, exist_ok=True)
red = (a[:, :, 0] > 190) & (a[:, :, 1] < 70) & (a[:, :, 2] < 70) & alpha
# the body's spurt (red, not head); grow 1px so its anti-aliased rim comes along
spurt = ndimage.binary_dilation(red & ~head, iterations=1) & alpha & ~head
layer(alpha & ~head & ~spurt).save(OUT + "DeadCat-Body.png")
layer(spurt).save(OUT + "DeadCat-Spurt.png")
layer(head).save(OUT + "DeadCat-Head.png")

# Head pivot = centre of the furry head itself (ignore the blood strokes above it): lowest 60% of the head mask
ys, xs = np.nonzero(head)
top, bottom = ys.min(), ys.max()
lower = ys > top + (bottom - top) * 0.4
px, py = xs[lower].mean(), ys[lower].mean()
info = {"w": im.width, "h": im.height, "headPivot": [px / im.width, 1 - py / im.height],
        "headBottom": 1 - bottom / im.height}
sy, sx = np.nonzero(spurt)
info["spurtPivot"] = [sx[sy > sy.max() - 8].mean() / im.width, 1 - sy.max() / im.height]
print(json.dumps(info))
