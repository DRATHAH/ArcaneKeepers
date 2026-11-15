"""Generates the ivy art for the level bridge.

  python AgentScripts/GenerateVines.py <ArcaneKeepers project root>

Writes:
  Assets/Sprites/Vines/bridge_vine_floor.png  ivy creeping in from the walls and gate, lies flat on the deck
  Assets/Sprites/Vines/bridge_vine_face.png   garland + hanging strands for the near bridge wall
  Assets/Sprites/Vines/bridge_vine_back.png   low creeper along the far parapet's inner wall
  Assets/Sprites/Vines/bridge_vine_leg.png    strands wrapping a bridge leg

Tile stems use the same deterministic wiggle on every edge, so neighbouring tiles draw the
shared edge as ONE vine (seamless); only the leaves are random.
"""
import math, os, random, sys
from PIL import Image, ImageDraw

SS = 3  # supersample

# Art style = the level's chunky toon look: flat 2-tone cel fills, thick near-black outlines (same
# weight as the brick seams), few big simple leaves rather than lots of small detailed ivy.
# Old castle ivy: woody brown stems and dark, muted ivy leaves (user rejected bright lime "tubes"
# and asked for the castle/medieval look).
STEM = (70, 98, 48, 255)        # green vine stem (brown "sticks" didn't read as vines)
STEM_OLD = (84, 92, 52, 255)    # older, slightly woody second stem
OUTLINE = (22, 28, 20, 255)
LEAF_DARK = (54, 100, 50, 255)
LEAF_LIGHT = (80, 130, 62, 255)
LEAF_YOUNG = (104, 148, 70, 255)
LEAF_DEEP = (40, 78, 44, 255)
VEIN = (30, 56, 30, 255)
LEAF_SCALE = 1.45   # every leaf is drawn this much bigger than the size asked for
OW_SCALE = 1.8      # and its outline this much thicker

# pointed vine leaf, tip to -y, unit size: sharp tip, full belly, rounded base
def _leaf_shape(n=48):
    pts = []
    for k in range(n):
        t = 2 * math.pi * k / n
        x = 0.58 * math.sin(t) * ((1 - math.cos(t)) / 2) ** 0.45
        y = -math.cos(t) * (0.85 if math.cos(t) < 0 else 1.0)  # rounded base
        pts.append((x, y))
    return pts
LEAF_FULL = _leaf_shape()
LEAF_HALF = [(max(0.0, x), y) for x, y in LEAF_FULL]  # right half (lit side)


def xf(pts, cx, cy, s, ang):
    c, si = math.cos(ang), math.sin(ang)
    return [(cx + (x * c - y * si) * s, cy + (x * si + y * c) * s) for x, y in pts]


def leaf(d, cx, cy, size, ang, ow, rnd):
    r = rnd.random()
    if r < 0.18:    # young, bright
        base, lit = LEAF_LIGHT, LEAF_YOUNG
    elif r < 0.38:  # old, deep (breaks up the flat green so it reads as real ivy)
        base, lit = LEAF_DEEP, LEAF_DARK
    else:
        base, lit = LEAF_DARK, LEAF_LIGHT
    size, ow = size * LEAF_SCALE, ow * OW_SCALE
    d.polygon(xf(LEAF_FULL, cx, cy, size + ow, ang), fill=OUTLINE)
    d.polygon(xf(LEAF_FULL, cx, cy, size, ang), fill=base)
    d.polygon(xf(LEAF_HALF, cx, cy, size * 0.9, ang), fill=lit)
    a, b = xf([(0, 0.85), (0, -0.55)], cx, cy, size, ang)
    d.line([a, b], fill=VEIN, width=max(1, int(ow * 0.7)))


def stem(d, pts, w, ow):
    w, ow = w * 1.3, ow * OW_SCALE
    d.line(pts, fill=OUTLINE, width=int(w + 2 * ow), joint="curve")
    d.line(pts, fill=STEM, width=int(w), joint="curve")
    r = (w + 2 * ow) / 2
    for p in (pts[0], pts[-1]):
        d.ellipse([p[0] - r, p[1] - r, p[0] + r, p[1] + r], fill=OUTLINE)
        d.ellipse([p[0] - w / 2, p[1] - w / 2, p[0] + w / 2, p[1] + w / 2], fill=STEM)


def leaves_along(d, pts, rnd, spacing, size, ow, density=1.0, stalk=1.2, skip_ends=0):
    """Leaves on alternating sides of a polyline."""
    acc, side = rnd.uniform(0, spacing), rnd.choice((-1, 1))
    out = []
    for i in range(1, len(pts)):
        (x0, y0), (x1, y1) = pts[i - 1], pts[i]
        seg = math.hypot(x1 - x0, y1 - y0)
        if seg == 0:
            continue
        tx, ty = (x1 - x0) / seg, (y1 - y0) / seg
        t = acc
        while t < seg:
            idx_frac = (i - 1 + t / seg) / (len(pts) - 1)
            if skip_ends <= idx_frac <= 1 - skip_ends and rnd.random() < density:
                px, py = x0 + tx * t, y0 + ty * t
                nx, ny = -ty * side, tx * side
                s = size * rnd.uniform(0.75, 1.15)
                lx, ly = px + nx * s * stalk, py + ny * s * stalk
                ang = math.atan2(ny, nx) + math.pi / 2 + rnd.uniform(-0.5, 0.5)
                out.append((px, py, lx, ly, s, ang))
                side = -side
            t += spacing * 1.35 * rnd.uniform(0.75, 1.25)
        acc = t - seg
    for px, py, lx, ly, s, ang in out:  # stalks first, then leaves on top
        d.line([(px, py), (lx, ly)], fill=OUTLINE, width=int(ow * 3.2))
    for px, py, lx, ly, s, ang in out:
        leaf(d, lx, ly, s, ang, ow, rnd)


def finish(img, path, shadow=None, shadow_clip=None):
    """shadow = (dx, dy, blur, alpha) in final pixels: a soft contact shadow under the ivy so it sits
    ON the stone instead of floating. shadow_clip = last image row that may carry shadow (rows below
    are open air, e.g. under the wall)."""
    w, h = img.size
    img = img.resize((w // SS, h // SS), Image.LANCZOS)
    if shadow:
        from PIL import ImageFilter
        dx, dy, blur, alpha = shadow
        a = img.split()[3].filter(ImageFilter.GaussianBlur(blur)).point(lambda v: int(v * alpha))
        if shadow_clip is not None:
            a.paste(0, (0, int(shadow_clip), a.size[0], a.size[1]))
        sh = Image.new("RGBA", img.size, (18, 26, 16, 0))
        sh.putalpha(a)
        base = Image.new("RGBA", img.size, (0, 0, 0, 0))
        base.paste(sh, (dx, dy))
        img = Image.alpha_composite(base, img)
    os.makedirs(os.path.dirname(path), exist_ok=True)
    img.save(path)
    print("wrote", path, img.size)


# ---------------------------------------------------------------- tiles
def tile(path, seed, ppu=160, cell=2.0, margin=0.2):
    rnd = random.Random(seed)
    units = cell + 2 * margin
    N = int(round(units * ppu)) * SS
    u = N / units  # px per unit (supersampled)
    img = Image.new("RGBA", (N, N), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    m, e = margin * u, (margin + cell) * u
    A = 0.025 * u  # deterministic wiggle -> neighbours' stems coincide exactly

    def edge(p0, p1):
        pts = []
        for k in range(49):
            t = k / 48
            off = A * math.sin(2 * math.pi * t)
            x = p0[0] + (p1[0] - p0[0]) * t
            y = p0[1] + (p1[1] - p0[1]) * t
            if p0[0] == p1[0]:
                x += off
            else:
                y += off
            pts.append((x, y))
        return pts

    edges = [edge((m, m), (e, m)), edge((m, e), (e, e)), edge((m, m), (m, e)), edge((e, m), (e, e))]
    w, ow = 0.026 * u, 0.008 * u
    for pts in edges:
        stem(d, pts, w, ow)
    # leaves: half density per tile because each inner edge is drawn by two tiles
    for pts in edges:
        leaves_along(d, pts, rnd, spacing=0.2 * u, size=0.08 * u, ow=ow, density=0.6, stalk=0.7, skip_ends=0.06)
    # an occasional small cluster at one corner
    for cx, cy in [(m, m), (e, m), (m, e), (e, e)]:
        if rnd.random() < 0.3:
            for _ in range(2):
                leaf(d, cx + rnd.uniform(-0.06, 0.06) * u, cy + rnd.uniform(-0.06, 0.06) * u,
                     0.075 * u, rnd.uniform(0, 2 * math.pi), ow, rnd)
    finish(img, path)


# ---------------------------------------------------------------- hanging strand helper
def strand(d, rnd, x, y, length, u, w, ow, sway=0.08, leaf_size=0.08):
    pts = []
    ph, amp = rnd.uniform(0, 6.28), sway * u * rnd.uniform(0.6, 1.2)
    n = max(6, int(length / (0.05 * u)))
    for k in range(n + 1):
        t = k / n
        pts.append((x + amp * math.sin(ph + t * 5) * t + 0.15 * u * t * t * rnd.choice((-1, 1)) * 0,
                    y + length * t))
    stem(d, pts, w * 0.8, ow)
    leaves_along(d, pts, rnd, spacing=0.22 * u, size=leaf_size * u, ow=ow, density=0.65)
    tip = pts[-1]
    leaf(d, tip[0], tip[1] + leaf_size * u * 0.6, leaf_size * u * 0.9, math.pi + rnd.uniform(-0.3, 0.3), ow, rnd)


# ---------------------------------------------------------------- near bridge wall
def face(path, x0=-9.7, x1=21.0, top=0.36, bottom=-1.35, ppu=110, seed=7, stone_bottom=-0.75):
    """Image spans world x0..x1, y bottom..top on the near wall (z=-6.84)."""
    rnd = random.Random(seed)
    W, H = int((x1 - x0) * ppu) * SS, int((top - bottom) * ppu) * SS
    u = ppu * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    X = lambda wx: (wx - x0) * u
    Y = lambda wy: (top - wy) * u
    w, ow = 0.045 * u, 0.013 * u

    # two runners swagging along the wall, crossing each other (reads as wrapped)
    anchors = [x0 + 0.1]
    while anchors[-1] < x1 - 0.5:
        anchors.append(anchors[-1] + rnd.uniform(2.2, 3.4))
    anchors[-1] = x1 - 0.05
    for r, (hy, sag, ph) in enumerate([(0.12, 0.42, 0.0)]):
        pts = []
        for i in range(len(anchors) - 1):
            a, b = anchors[i], anchors[i + 1]
            for k in range(24):
                t = k / 24
                wx = a + (b - a) * t
                wy = hy - sag * math.sin(math.pi * t) ** 2 + 0.05 * math.sin(ph + wx * 1.7)
                pts.append((X(wx), Y(wy)))
        pts.append((X(anchors[-1]), Y(hy)))
        stem(d, pts, w * (1 if r == 0 else 0.75), ow)
        leaves_along(d, pts, rnd, spacing=0.26 * u, size=0.11 * u, ow=ow, density=0.6)

    # short strands draping down the wall from the swags; they end on the stone or just tip over its
    # bottom edge (long ones dangling in mid-air read as broken)
    for i in range(len(anchors) - 1):
        for _ in range(rnd.choice((0, 1, 1))):
            a, b = anchors[i], anchors[i + 1]
            t = 0.5 + rnd.uniform(-0.3, 0.3)
            sx = a + (b - a) * t
            sy = 0.12 - 0.42 * math.sin(math.pi * t) ** 2 + 0.05 * math.sin(sx * 1.7)
            to_edge = sy - stone_bottom
            length = rnd.uniform(to_edge * 0.45, to_edge + 0.3)
            strand(d, rnd, X(sx), Y(sy), length * u, u, w, ow, sway=0.04, leaf_size=0.1)
    finish(img, path, shadow=(2, 4, 3, 0.5), shadow_clip=(top - stone_bottom) * ppu)


# ---------------------------------------------------------------- far parapet inner wall
def back(path, x0=-10.0, x1=21.0, top=0.38, bottom=-0.25, ppu=110, seed=11):
    rnd = random.Random(seed)
    W, H = int((x1 - x0) * ppu) * SS, int((top - bottom) * ppu) * SS
    u = ppu * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    X = lambda wx: (wx - x0) * u
    Y = lambda wy: (top - wy) * u
    w, ow = 0.042 * u, 0.013 * u
    pts = [(X(wx), Y(-0.12 + 0.06 * math.sin(wx * 1.3) + 0.04 * math.sin(wx * 3.1)))
           for wx in [x0 + i * 0.05 for i in range(int((x1 - x0) / 0.05) + 1)]]
    stem(d, pts, w, ow)
    leaves_along(d, pts, rnd, spacing=0.26 * u, size=0.1 * u, ow=ow, density=0.55)
    # short climbers reaching up the wall
    wx = x0 + rnd.uniform(0.5, 1.5)
    while wx < x1 - 0.5:
        cl = []
        h = rnd.uniform(0.25, 0.45)
        lean = rnd.uniform(-0.25, 0.25)
        for k in range(13):
            t = k / 12
            cl.append((X(wx + lean * t + 0.04 * math.sin(t * 7)), Y(-0.12 + h * t)))
        stem(d, cl, w * 0.7, ow)
        leaves_along(d, cl, rnd, spacing=0.14 * u, size=0.07 * u, ow=ow)
        wx += rnd.uniform(2.6, 4.2)
    finish(img, path, shadow=(2, 3, 3, 0.45))


# ---------------------------------------------------------------- bridge leg
def leg(path, width=1.83, top=-0.74, bottom=-6.0, ppu=110, seed=3):
    """Ivy spilling down the leg's front face from under the deck: a hidden runner along the top,
    strands of mixed length hanging from it, all kept inside the face so nothing sticks out into
    the air beside the leg."""
    rnd = random.Random(seed)
    W, H = int(width * ppu) * SS, int((top - bottom) * ppu) * SS
    u = ppu * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    w, ow = 0.05 * u, 0.013 * u
    pad = 0.55
    runner = [(x * u, (0.08 + 0.04 * math.sin(x * 2.3)) * u)
              for x in [pad * 0.5 + i * 0.05 for i in range(int((width - pad) / 0.05) + 1)]]
    stem(d, runner, w, ow)
    # the near wall hides the top ~2 units of the leg from the game camera, so every strand is long
    # enough to show, and they stay in the middle of the face so no leaf hangs off its edges
    n = 3
    for i, length in enumerate(rnd.sample([2.9, 3.6, 4.4], 3)):
        sx = pad + (width - 2 * pad) * (i + rnd.uniform(0.35, 0.65)) / n
        strand(d, rnd, sx * u, (0.08 + 0.04 * math.sin(sx * 2.3)) * u, length * u, u, w, ow,
               sway=0.035, leaf_size=0.11)
    finish(img, path, shadow=(3, 4, 3, 0.5))


# ---------------------------------------------------------------- bridge floor (lies flat on the deck)
def tendril(d, rnd, x, y, ang, length, u, w, ow, leaf_size, out_pts=None, branch=True):
    """Curving, tapering creeper from (x, y) heading ang (px space); leaves thin out toward the tip."""
    pts, turn = [(x, y)], rnd.uniform(-1, 1)
    step = 0.04 * u
    n = int(length / step)
    for k in range(n):
        turn += rnd.uniform(-0.35, 0.35)
        turn = max(-1.2, min(1.2, turn))
        ang += turn * 0.05
        x += math.cos(ang) * step
        y += math.sin(ang) * step
        pts.append((x, y))
    # tapered stem: draw back-to-front in a few chunks
    chunks = 6
    for c in range(chunks):
        a = int(len(pts) * c / chunks)
        b = min(len(pts), int(len(pts) * (c + 1) / chunks) + 1)
        if b - a >= 2:
            ww = w * (1 - 0.55 * c / chunks)
            d.line(pts[a:b], fill=OUTLINE, width=int(ww + 2 * ow), joint="curve")
    for c in range(chunks):
        a = int(len(pts) * c / chunks)
        b = min(len(pts), int(len(pts) * (c + 1) / chunks) + 1)
        if b - a >= 2:
            ww = w * (1 - 0.55 * c / chunks)
            d.line(pts[a:b], fill=STEM, width=int(ww), joint="curve")
    if branch and length > 1.2 * u:
        for _ in range(rnd.choice((1, 1, 2))):
            i = rnd.randint(len(pts) // 4, len(pts) * 2 // 3)
            bx, by = pts[i]
            tendril(d, rnd, bx, by, ang + rnd.choice((-1, 1)) * rnd.uniform(0.5, 1.0),
                    length * rnd.uniform(0.3, 0.5), u, w * 0.7, ow, leaf_size * 0.9, branch=False)
    # leaves: dense near the root, sparse at the tip
    head = pts[: max(2, int(len(pts) * 0.85))]
    leaves_along(d, head, rnd, spacing=0.22 * u, size=leaf_size * u, ow=ow, density=0.6, stalk=0.8)
    tip = pts[-1]
    leaf(d, tip[0], tip[1], leaf_size * u * 0.7, ang + math.pi / 2, ow, rnd)


def floor(path, x0=-11.45, x1=21.0, z0=-5.87, z1=5.79, ppu=110, seed=21, grid=None):
    rnd = random.Random(seed)
    W, H = int((x1 - x0) * ppu) * SS, int((z1 - z0) * ppu) * SS
    u = ppu * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    X = lambda wx: (wx - x0) * u
    Z = lambda wz: (z1 - wz) * u  # image top = far wall
    w, ow = 0.06 * u, 0.016 * u

    # runs hugging the foot of each wall, in patches with bare stretches between
    for wall_z, inward in ((z1 - 0.06, 1), (z0 + 0.06, -1)):
        wx = x0 + rnd.uniform(0.0, 0.6)
        while wx < x1 - 1:
            run = rnd.uniform(3.0, 6.0)
            pts = []
            for k in range(int(run / 0.05) + 1):
                px = wx + k * 0.05
                pts.append((X(px), Z(wall_z - inward * (0.05 + 0.05 * math.sin(px * 1.9)))))
            stem(d, pts, w * 0.9, ow)
            leaves_along(d, pts, rnd, spacing=0.3 * u, size=0.11 * u, ow=ow, density=0.45)
            # a creeper or two reaching out onto the stones
            for _ in range(rnd.choice((0, 1))):
                bx = wx + rnd.uniform(0.6, run - 0.6)
                base_ang = math.pi / 2 if inward == 1 else -math.pi / 2  # +y in image = toward near wall
                tendril(d, rnd, X(bx), Z(wall_z), base_ang + rnd.uniform(-0.7, 0.7),
                        rnd.uniform(0.6, 1.3) * u, u, w, ow, 0.12)
            wx += run + rnd.uniform(2.0, 4.5)

    # the gate end: a runner hugging the foot of the castle wall, with short creepers that grow
    # across the stones and plug into the grid's first line (anchored at both ends, no loose sprigs)
    gate_x = -9.66
    pts = [(X(gate_x + 0.04 * math.sin(wz * 2.1)), Z(wz)) for wz in
           [z0 + 0.1 + i * 0.05 for i in range(int((z1 - z0 - 0.2) / 0.05) + 1)]]
    stem(d, pts, w * 0.9, ow)
    leaves_along(d, pts, rnd, spacing=0.3 * u, size=0.11 * u, ow=ow, density=0.45)
    if grid:
        gz = grid[1] + 0.6
        while gz < grid[1] + grid[3] * grid[4] - 0.4:
            tendril(d, rnd, X(gate_x), Z(gz), rnd.uniform(-0.35, 0.35), rnd.uniform(0.6, 0.85) * u,
                    u, w * 0.8, ow, 0.12, branch=False)
            gz += rnd.uniform(2.4, 3.6)
        vine_grid(d, rnd, X, Z, u, *grid)
    finish(img, path, shadow=(2, 5, 4, 0.55))


def vine_grid(d, rnd, X, Z, u, gx0, gz0, cols, rows, cell):
    """The placement grid as ONE vine net. Each grid line is a real-looking vine: two green stems
    meandering round the line and crossing now and then, leaves on short stalks pointing forward
    along the growth, and small curly tendrils. Stays thick so the squares read from the camera."""
    gx1, gz1 = gx0 + cols * cell, gz0 + rows * cell
    w, ow = 0.085 * u, 0.026 * u
    vines = []  # (stem A pts, stem B pts, direction (dx, dy) in px of the line)
    def run(a, b, fixed, horizontal):
        A, B = [], []
        pa, pb, pc = rnd.uniform(0, 6.28), rnd.uniform(0, 6.28), rnd.uniform(0, 6.28)
        n = int((b - a) / 0.02)
        for k in range(n + 1):
            p = a + (b - a) * k / n
            base = 0.025 * math.sin(pc + p * 0.9)
            oa = base + 0.06 * math.sin(pa + p * 2 * math.pi / 1.4)
            ob = base + 0.05 * math.sin(pb + p * 2 * math.pi / 1.1)
            A.append((X(p), Z(fixed + oa)) if horizontal else (X(fixed + oa), Z(p)))
            B.append((X(p), Z(fixed + ob)) if horizontal else (X(fixed + ob), Z(p)))
        vines.append((A, B))
    for r in range(rows + 1):
        run(gx0 - 0.1, gx1 + 0.1, gz0 + r * cell, True)
    for c in range(cols + 1):
        run(gz0 - 0.1, gz1 + 0.1, gx0 + c * cell, False)

    # curly tendrils: a short stalk then a tightening spiral (the classic vine signifier)
    curls = []
    for A, B in vines:
        k = rnd.randint(10, 40)
        while k < len(A) - 10:
            (x0, y0), (x1, y1) = A[k - 1], A[k + 1]
            tang = math.atan2(y1 - y0, x1 - x0)
            side = rnd.choice((-1, 1))
            ang = tang + side * rnd.uniform(0.5, 0.9)
            x, y = A[k]
            pts = [(x, y)]
            step = 0.012 * u
            for i in range(14):  # stalk
                x += math.cos(ang) * step; y += math.sin(ang) * step; pts.append((x, y))
            r0 = 0.07 * u
            for i in range(40):  # spiral in
                ang += side * 0.32
                r = r0 * (1 - i / 46)
                x += math.cos(ang) * r * 0.32; y += math.sin(ang) * r * 0.32; pts.append((x, y))
            curls.append(pts)
            k += rnd.randint(80, 130)

    # leaves: alternate sides, on short stalks, angled forward along the growth direction
    leaves = []
    for A, B in vines:
        grow = rnd.choice((1, -1))  # which way this vine grows
        pts = A if grow == 1 else A[::-1]
        side = rnd.choice((-1, 1))
        k = rnd.randint(3, 12)
        while k < len(pts) - 3:
            (x0, y0), (x1, y1) = pts[k - 2], pts[k + 2]
            tang = math.atan2(y1 - y0, x1 - x0)
            dirn = tang + side * rnd.uniform(0.6, 1.0)
            s = 0.13 * u * rnd.uniform(0.8, 1.2)
            bx, by = pts[k]
            sx, sy = bx + math.cos(dirn) * w * 0.9, by + math.sin(dirn) * w * 0.9
            if rnd.random() < 0.8:  # some gaps so the line isn't an even leafy hedge
                leaves.append(((bx, by), (sx, sy), s, dirn))
            side = -side
            k += int(rnd.uniform(0.45, 0.7) * u / (0.02 * u))  # user: "a bit too many leaves"
    # stalks go UNDER the stems so their bases tuck into the vine
    for (b, st, s, dirn) in leaves:
        d.line([b, st], fill=OUTLINE, width=int(w * 0.38 + ow), joint="curve")
        d.line([b, st], fill=STEM, width=int(w * 0.38))
    # stems: outlines first so where stems cross they merge into one vine
    for A, B in vines:
        d.line(A, fill=OUTLINE, width=int(w + 2 * ow), joint="curve")
        d.line(B, fill=OUTLINE, width=int(w * 0.7 + 2 * ow), joint="curve")
    for c in curls:
        d.line(c, fill=OUTLINE, width=int(w * 0.32 + 2 * ow * 0.7), joint="curve")
    for A, B in vines:
        d.line(B, fill=STEM_OLD, width=int(w * 0.7), joint="curve")
    for c in curls:
        d.line(c, fill=STEM, width=int(w * 0.32), joint="curve")
    for A, B in vines:
        d.line(A, fill=STEM, width=int(w), joint="curve")
    for A, B in vines:
        for p in (A[0], A[-1], B[0], B[-1]):
            r = w * 0.45
            d.ellipse([p[0] - r, p[1] - r, p[0] + r, p[1] + r], fill=STEM)

    for (b, (sx, sy), s, dirn) in leaves:
        cx, cy = sx + math.cos(dirn) * s * LEAF_SCALE * 0.92, sy + math.sin(dirn) * s * LEAF_SCALE * 0.92
        leaf(d, cx, cy, s, dirn + math.pi / 2, 0.016 * u, rnd)


# ---------------------------------------------------------------- castle wall beside the gate
def climber(d, rnd, x, y, height, u, w, ow, lo, hi, leaf_size, branch=True):
    """Stem climbing up a wall from (x, y) px, wandering side to side but steered to stay within
    lo..hi px; tapers, sprouts a side shoot or two, leaves sparse and thinning to the tip."""
    pts, ang, turn = [(x, y)], -math.pi / 2, 0.0
    step = 0.04 * u
    for k in range(int(height / step)):
        turn = max(-0.9, min(0.9, turn + rnd.uniform(-0.3, 0.3)))
        ang += turn * 0.06
        ang += (-math.pi / 2 - ang) * 0.08          # keeps climbing up
        if x < lo + 0.1 * u: ang += 0.08              # steer away from the face edges
        if x > hi - 0.1 * u: ang -= 0.08
        x = max(lo, min(hi, x + math.cos(ang) * step))
        y += math.sin(ang) * step
        pts.append((x, y))
    chunks = 6
    for col, extra in ((OUTLINE, 2 * ow * OW_SCALE), (STEM, 0)):
        for c in range(chunks):
            a = int(len(pts) * c / chunks)
            b = min(len(pts), int(len(pts) * (c + 1) / chunks) + 1)
            if b - a >= 2:
                d.line(pts[a:b], fill=col, width=int(w * 1.3 * (1 - 0.5 * c / chunks) + extra), joint="curve")
    if branch:
        for _ in range(rnd.choice((1, 2))):
            i = rnd.randint(len(pts) // 4, len(pts) * 3 // 5)
            bx, by = pts[i]
            climber(d, rnd, bx, by, height * rnd.uniform(0.25, 0.4), u, w * 0.65, ow, lo, hi,
                    leaf_size * 0.9, branch=False)
    head = pts[: max(2, int(len(pts) * 0.9))]
    leaves_along(d, head, rnd, spacing=0.34 * u, size=leaf_size * u, ow=ow, density=0.6, stalk=0.8)
    tip = pts[-1]
    leaf(d, tip[0], tip[1], leaf_size * u * 0.7, rnd.uniform(-0.3, 0.3), ow, rnd)


def wall(path, z0, z1, heights, bottom=-0.25, ppu=110, seed=31):
    """Ivy climbing the castle's outer wall face (x = -9.7, facing the deck) beside the gate.
    Image spans world z0..z1 left to right, y bottom..bottom+max height+0.4; roots sit on the deck
    and join the gate runner of the floor ivy."""
    rnd = random.Random(seed)
    top = bottom + max(heights) + 0.45
    W, H = int((z1 - z0) * ppu) * SS, int((top - bottom) * ppu) * SS
    u = ppu * SS
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    w, ow = 0.05 * u, 0.013 * u
    lo, hi = 0.18 * u, W - 0.18 * u
    # a short root run along the wall foot
    base = [(x, H - (0.06 + 0.03 * math.sin(x / u * 3)) * u) for x in
            [0.1 * u + i * 0.05 * u for i in range(int((z1 - z0 - 0.2) / 0.05) + 1)]]
    stem(d, base, w, ow)
    n = len(heights)
    for i, h in enumerate(heights):
        x = lo + (hi - lo) * (i + rnd.uniform(0.35, 0.65)) / n
        climber(d, rnd, x, H - 0.06 * u, h * u, u, w, ow, lo, hi, 0.12)
    leaves_along(d, base, rnd, spacing=0.34 * u, size=0.11 * u, ow=ow, density=0.5)
    finish(img, path, shadow=(3, 4, 3, 0.5))


if __name__ == "__main__":
    root = sys.argv[1]
    sp = os.path.join(root, "Assets", "Sprites")
    # DemoLevel placement grid: 10x5 cells of 2 units, lower-left corner at (-9.16, -5.04)
    floor(os.path.join(sp, "Vines", "bridge_vine_floor.png"), grid=(-9.16, -5.04, 10, 5, 2.0))
    face(os.path.join(sp, "Vines", "bridge_vine_face.png"))
    back(os.path.join(sp, "Vines", "bridge_vine_back.png"))
    leg(os.path.join(sp, "Vines", "bridge_vine_leg.png"))
    # castle wall either side of the gate (outer face x=-9.7: near jamb z -7.5..-4.9, far 5.2..7.7)
    wall(os.path.join(sp, "Vines", "castle_vine_wall_near.png"), -7.5, -4.9, [4.4, 2.6, 3.6], seed=31)
    # the 45-degree chamfer of the door frame right of the gate ((-10.55, z 4.36) -> (-9.72, z 5.19),
    # 1.17 wide, faces the camera): the most visible stone beside the door
    wall(os.path.join(sp, "Vines", "castle_vine_frame_far.png"), 0.0, 1.17, [5.0, 3.3], seed=41)
    wall(os.path.join(sp, "Vines", "castle_vine_wall_far.png"), 5.2, 7.7, [3.0, 4.0], seed=37)
