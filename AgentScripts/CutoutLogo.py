# Removes a baked-in checkerboard/white background from a logo PNG (flood fill from the border
# through light, unsaturated pixels), feathers the edge, and trims to the content.
# Usage: python AgentScripts/CutoutLogo.py <in.png> <out.png> [--holes]
# --holes also clears enclosed checkerboard pockets (both checker tones, strictly neutral grey/white).
import sys
from collections import deque
from PIL import Image, ImageFilter

src, dst = sys.argv[1], sys.argv[2]
holes = "--holes" in sys.argv[3:]
im = Image.open(src).convert("RGB")
w, h = im.size
px = im.load()

def is_bg(c):
    r, g, b = c
    return min(r, g, b) > 185 and max(r, g, b) - min(r, g, b) < 40

bg = bytearray(w * h)
q = deque()
for x in range(w):
    q.append((x, 0)); q.append((x, h - 1))
for y in range(h):
    q.append((0, y)); q.append((w - 1, y))
while q:
    x, y = q.popleft()
    i = y * w + x
    if bg[i] or not is_bg(px[x, y]):
        continue
    bg[i] = 1
    if x > 0: q.append((x - 1, y))
    if x < w - 1: q.append((x + 1, y))
    if y > 0: q.append((x, y - 1))
    if y < h - 1: q.append((x, y + 1))

def is_checker(c):
    r, g, b = c
    return min(r, g, b) >= 222 and max(r, g, b) - min(r, g, b) <= 6

if holes:
    seen = bytearray(w * h)
    for sy in range(h):
        for sx in range(w):
            i0 = sy * w + sx
            if bg[i0] or seen[i0] or not is_checker(px[sx, sy]):
                continue
            comp, tones, q = [], set(), deque([(sx, sy)])
            seen[i0] = 1
            while q:
                x, y = q.popleft()
                comp.append(y * w + x)
                tones.add(px[x, y][0] >= 245)
                for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                    if 0 <= nx < w and 0 <= ny < h:
                        j = ny * w + nx
                        if not seen[j] and not bg[j] and is_checker(px[nx, ny]):
                            seen[j] = 1
                            q.append((nx, ny))
            if len(comp) >= 300 and len(tones) == 2:
                for j in comp:
                    bg[j] = 1

hard = Image.frombytes("L", (w, h), bytes(0 if b else 255 for b in bg))
# Eat the light fringe left by the old background.
core = hard.filter(ImageFilter.MinFilter(5))
# Anti-aliased edge: supersample 4x, smooth the contour, threshold, box-downsample back.
S = 4
big = core.resize((w * S, h * S), Image.BICUBIC).filter(ImageFilter.GaussianBlur(S * 1.2))
big = big.point(lambda a: 255 if a >= 128 else 0)
mask = big.resize((w, h), Image.BOX)
# Decontaminate: semi-transparent edge pixels take the dark outline colour, not the old white.
rgb = im.copy()
edge = im.filter(ImageFilter.MinFilter(7))
rgb.paste(edge, mask=Image.eval(core, lambda a: 255 - a))
out = rgb.convert("RGBA")
out.putalpha(mask)
out = out.crop(mask.point(lambda a: 255 if a > 8 else 0).getbbox())
out.save(dst)
print(dst, out.size)
