# One staggered line facing the skeletons: back/front alternate by a small step, neighbours overlap slightly.
import sys
from PIL import Image
A = r'C:/Users/phoebe/OneDrive/Desktop/ArcaneKeepers-main/ArcaneKeepers/Assets/UserInterface/'
SC = 2.0
def load(n, s):
    im = Image.open(A + 'Heroes/' + n + '-Hero.png').convert('RGBA')
    im = im.crop(im.split()[-1].getbbox())
    return im.resize((round(im.width * s * SC), round(im.height * s * SC)), Image.LANCZOS)
OVERLAP = float(sys.argv[2]) if len(sys.argv) > 2 else 0.22
STEP = 34
line = [('Ferret', 'b'), ('Goat', 'f'), ('Cat', 'b'), ('Mouse', 'f'), ('Dog', 'b')]
ims = [(n, d, load(n, 0.94 if d == 'b' else 1.0)) for n, d in line]
xs, x = [], 0
for i, (n, d, im) in enumerate(ims):
    if i: x -= int(min(im.width, ims[i-1][2].width) * OVERLAP)
    xs.append(x); x += im.width
W = x; H = max(im.height for _, _, im in ims) + int(STEP * SC)
out = Image.new('RGBA', (W, H), (0, 0, 0, 0))
for d_pass in ('b', 'f'):
    for (n, d, im), x in zip(ims, xs):
        if d == d_pass:
            out.alpha_composite(im, (x, H - im.height - (int(STEP * SC) if d == 'b' else 0)))
out.save(sys.argv[1]); print(W, H, W / H)
