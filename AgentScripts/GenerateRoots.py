"""Placement-grid art for the night-forest levels (Level4, Level5): gnarled tree roots half-buried in
the dirt path along the grid lines (diving under soil mounds here and there), plus little glowing mushroom clusters
(teal / pink, like the scene's GlowingMushrooms) that stand at some of the root crossings.

  python AgentScripts/GenerateRoots.py <ArcaneKeepers project root> Level4|Level5

Writes Assets/Sprites/Roots/<Level>/:
  forest_roots_floor.png   root net, lies flat on the path (lit, like the bridge ivy)
  forest_shroom_<n>.png    upright mushroom clusters (unlit so they glow), billboarded at crossings
  forest_shroom_glow.png   soft round glow pool laid on the ground under each cluster

Same chunky toon look as GenerateVines.py: flat 2-tone cel fills, thick near-black outlines.
"""
import math, os, random, sys
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import GenerateVines as gv

SS = gv.SS
OUTLINE = (26, 18, 14, 255)      # mushrooms only
# roots: earthy bark sitting IN the path (Dirt_texture is ~(106,56,3)..(135,80,22)); darker and cooler
# than the dirt so the grid reads, with a dark-brown rim instead of a black sticker outline
RIM = (44, 26, 14, 255)
TRENCH = (16, 10, 3, 140)
BARK = (92, 62, 40, 255)
BARK_DARK = (66, 42, 26, 255)
BARK_LIT = (128, 92, 60, 255)
CRACK = (46, 28, 16, 255)
SOIL = (46, 32, 9, 255)     # = the path as it RENDERS in game (terrain shows ~0.35x its texture; sprites ~1x)
SOIL_LIT = (64, 46, 16, 255)

# grid (same in every level): 10x5 cells of 2 units, lower-left corner at (-9.16, -5.04)
GX0, GZ0, COLS, ROWS, CELL = -9.16, -5.04, 10, 5, 2.0
MARGIN = 0.45
LEFT = 1.95   # floor art reaches left to the castle's door-frame chamfer foot (x -11.04)
X0, Z0 = GX0 - LEFT, GZ0 - 3.3
X1, Z1 = GX0 + COLS * CELL + 3.2, GZ0 + ROWS * CELL + 3.4

VARIANTS = {  # seed, share of crossings with mushrooms, (teal, pink) weights
    "Level4": (401, 0.30, (0.7, 0.3)),
    "Level5": (502, 0.30, (0.35, 0.65)),
}


def wobble_line(rnd, a, b, u, amp, step=0.08):
    """Points from a to b (px) wandering gently off the straight line, pinned at both ends."""
    (ax, ay), (bx, by) = a, b
    L = math.hypot(bx - ax, by - ay)
    n = max(2, int(L / (step * u)))
    tx, ty = (bx - ax) / L, (by - ay) / L
    nx, ny = -ty, tx
    ph1, ph2 = rnd.uniform(0, 6.3), rnd.uniform(0, 6.3)
    f1, f2 = rnd.uniform(1.6, 2.4), rnd.uniform(4.0, 5.5)
    pts = []
    for i in range(n + 1):
        t = i / n
        env = math.sin(math.pi * t) ** 0.6
        o = amp * u * env * (0.7 * math.sin(ph1 + f1 * math.pi * t) + 0.3 * math.sin(ph2 + f2 * math.pi * t))
        pts.append((ax + tx * L * t + nx * o, ay + ty * L * t + ny * o))
    return pts


def offset(pts, d):
    out = []
    for i, (x, y) in enumerate(pts):
        a, b = pts[max(0, i - 1)], pts[min(len(pts) - 1, i + 1)]
        tx, ty = b[0] - a[0], b[1] - a[1]
        L = math.hypot(tx, ty) or 1
        out.append((x - ty / L * d, y + tx / L * d))
    return out


def ribbon(c, hw, a, b):
    """Polygon of the band between normal offsets a*hw and b*hw along centreline c."""
    left, right = [], []
    for i, (x, y) in enumerate(c):
        p, q = c[max(0, i - 1)], c[min(len(c) - 1, i + 1)]
        tx, ty = q[0] - p[0], q[1] - p[1]
        L = math.hypot(tx, ty) or 1
        nx, ny = -ty / L, tx / L
        left.append((x + nx * hw[i] * a, y + ny * hw[i] * a))
        right.append((x + nx * hw[i] * b, y + ny * hw[i] * b))
    return left + right[::-1]


def normals(c):
    out = []
    for i in range(len(c)):
        p, q = c[max(0, i - 1)], c[min(len(c) - 1, i + 1)]
        tx, ty = q[0] - p[0], q[1] - p[1]
        L = math.hypot(tx, ty) or 1
        out.append((-ty / L, tx / L))
    return out


def band(c, off_a, off_b):
    """Polygon between absolute normal offsets off_a[i] and off_b[i] (px)."""
    ns = normals(c)
    A = [(x + nx * o, y + ny * o) for (x, y), (nx, ny), o in zip(c, ns, off_a)]
    B = [(x + nx * o, y + ny * o) for (x, y), (nx, ny), o in zip(c, ns, off_b)]
    return A + B[::-1]


def meander(rnd, a, b, u, amp, step):
    """Centreline from a to b wandering in natural 1-3.5u bends (not one long arc)."""
    (ax, ay), (bx, by) = a, b
    L = math.hypot(bx - ax, by - ay)
    n = max(2, int(L / (step * u)))
    tx, ty = (bx - ax) / L, (by - ay) / L
    nx, ny = -ty, tx
    l1, l2 = rnd.uniform(2.5, 3.5) * u, rnd.uniform(0.9, 1.3) * u
    p1, p2 = rnd.uniform(0, 6.3), rnd.uniform(0, 6.3)
    pts = []
    for i in range(n + 1):
        s = L * i / n
        o = amp * u * (0.65 * math.sin(p1 + 2 * math.pi * s / l1) + 0.35 * math.sin(p2 + 2 * math.pi * s / l2))
        pts.append((ax + tx * s + nx * o, ay + ty * s + ny * o))
    return pts


def swell(rnd, n, base, step_u, taper_in=0.0, taper_out=0.0, var=0.4):
    """Half-widths along a root: slow swelling and thinning (never an even pipe), optional pointed ends."""
    ph = [rnd.uniform(0, 6.3) for _ in range(3)]
    fr = [rnd.uniform(2.0, 3.0), rnd.uniform(5.0, 7.0), rnd.uniform(12.0, 16.0)]
    out = []
    for i in range(n):
        s = i * step_u
        v = 0.55 * math.sin(ph[0] + fr[0] * s) + 0.3 * math.sin(ph[1] + fr[1] * s) + 0.15 * math.sin(ph[2] + fr[2] * s)
        w = base * (1 + var * v)
        t = i / max(1, n - 1)
        if taper_in and t < taper_in:
            w *= 0.15 + 0.85 * (t / taper_in) ** 0.7
        if taper_out and t > 1 - taper_out:
            w *= 0.15 + 0.85 * ((1 - t) / taper_out) ** 0.7
        out.append(w)
    return out


def body(d, rnd, c, hw, ow, cracks=True, shadow=True):
    """Half-buried root: soft trench shadow toward the camera, dark-brown rim, bark fill, broken
    lit patches on the top side (no continuous stripe), short irregular bark cracks."""
    n = len(c)
    if n < 2:
        return
    if shadow:
        sh = [(x, y + ow * 1.6) for x, y in c]
        d.polygon(ribbon(sh, [h + ow * 1.2 for h in hw], -1, 1), fill=TRENCH)
    d.polygon(ribbon(c, [h + ow for h in hw], -1, 1), fill=RIM)
    d.polygon(ribbon(c, hw, -1, 1), fill=BARK)
    d.polygon(ribbon(c, hw, 0.35, 1.0), fill=BARK_DARK)          # shaded underside
    i = rnd.randint(0, 3)
    while i < n - 2:                                              # broken lit patches
        j = min(n, i + rnd.randint(4, 9))
        if j - i >= 3 and rnd.random() < 0.75:
            m = j - i
            env = [math.sin(math.pi * (q + 0.5) / m) ** 0.8 for q in range(m)]
            ctr = rnd.uniform(-0.55, -0.4)
            top = [h * (ctr - 0.3 * e) for h, e in zip(hw[i:j], env)]
            bot = [h * (ctr + 0.25 * e) for h, e in zip(hw[i:j], env)]
            d.polygon(band(c[i:j], top, bot), fill=BARK_LIT)
        i = j + rnd.randint(1, 3)
    if cracks:
        i = rnd.randint(1, 3)
        while i < n - 2:
            j = min(n - 1, i + rnd.randint(1, 2))
            o = rnd.uniform(-0.4, 0.3)
            seg = ribbon(c[i:j + 1], hw[i:j + 1], o, o)[: j + 1 - i]
            if len(seg) >= 2:
                d.line(seg, fill=CRACK, width=max(1, int(ow * 0.75)))
            i += rnd.randint(2, 5)


def mound(d, rnd, x, y, r):
    """Soil heaped over a root where it dives underground (the path's own dirt colours)."""
    pts = [(x + math.cos(t) * r * rnd.uniform(0.85, 1.1), y + math.sin(t) * r * 0.62 * rnd.uniform(0.85, 1.1))
           for t in [k * math.pi / 7 for k in range(14)]]
    d.polygon([(px, py + r * 0.12) for px, py in pts], fill=TRENCH)
    d.polygon(pts, fill=SOIL)
    d.polygon([(px * 0.7 + x * 0.3, py * 0.7 + (y - r * 0.12) * 0.3) for px, py in pts], fill=SOIL_LIT)


def crumbs(d, rnd, x, y, spread, n, u):
    for _ in range(n):
        cx, cy = x + rnd.uniform(-spread, spread), y + rnd.uniform(-spread, spread) * 0.8
        r = rnd.uniform(0.018, 0.04) * u
        pts = [(cx + math.cos(t) * r * rnd.uniform(0.7, 1.2), cy + math.sin(t) * r * 0.75 * rnd.uniform(0.7, 1.2))
               for t in [k * math.pi / 3 for k in range(6)]]
        d.polygon([(px, py + r * 0.35) for px, py in pts], fill=TRENCH)
        d.polygon(pts, fill=rnd.choice((SOIL, SOIL_LIT, SOIL_LIT)))


def floor(path, seed, ppu=110):
    rnd = random.Random(seed)
    u = ppu * SS
    W, H = int((X1 - X0) * u), int((Z1 - Z0) * u)
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    P = lambda wx, wz: ((wx - X0) * u, (Z1 - wz) * u)
    base, ow, step = 0.112 * u, 0.015 * u, 0.06

    xs = [GX0 + i * CELL for i in range(COLS + 1)]
    zs = [GZ0 + j * CELL for j in range(ROWS + 1)]
    # Less "ruler grid", still a grid: every grid line is a chain of separate roots (2-4 cells long)
    # that overlap and taper past each other at mid-cell, sometimes diving under a soil mound there.
    # Breaks are only ever mid-cell, so every crossing (cell corner) is still drawn. Lines along x
    # (the enemy lanes' borders) = thick main roots; lines across the lanes = thinner roots.
    later = []   # drawn on top of everything: mounds where roots dive under the soil, crumbs
    segs = []    # (start px, end px, is_row, taper fraction)
    def chain(fixed, t0, t1, is_row):
        mids = [t0 + 0.3 + CELL * (k + 0.5) for k in range(int(round((t1 - t0 - 0.6) / CELL)))]
        cuts = [m for m in mids[1:-1] if rnd.random() < 0.45]
        bounds = [t0] + cuts + [t1]
        side = rnd.choice((-1, 1))
        for k in range(len(bounds) - 1):
            ta = bounds[k] - (0.32 if k > 0 else 0)
            tb = bounds[k + 1] + (0.32 if k < len(bounds) - 2 else 0)
            lat = side * 0.07 * (1 if is_row else 0.8)
            side = -side
            if is_row:
                A, B = P(ta, fixed + lat), P(tb, fixed + lat)
            else:
                A, B = P(fixed + lat, tb), P(fixed + lat, ta)
            tap = min(0.3, 0.38 / max(0.5, tb - ta))
            segs.append((A, B, is_row, tap))
        for m in cuts:
            if rnd.random() < 0.5:
                later.append(("mound", *(P(m, fixed) if is_row else P(fixed, m)), base * (1.0 if is_row else 0.75) * rnd.uniform(1.8, 2.2)))
    for z in zs:
        chain(z, GX0 - 0.3, xs[-1] + 0.3, True)
    for x in xs:
        chain(x, GZ0 - 0.3, zs[-1] + 0.3, False)
    # columns under rows; inside each group random order
    cols = [sg for sg in segs if not sg[2]]
    rows = [sg for sg in segs if sg[2]]
    rnd.shuffle(cols); rnd.shuffle(rows)

    off_grid(d, rnd, P, u, base, ow, later)
    for a, b, is_row, tap in cols + rows:
        c = meander(rnd, a, b, u, 0.11 if is_row else 0.09, step)
        n = len(c)
        hw = swell(rnd, n, base * (1.0 if is_row else 0.72), step, taper_in=tap, taper_out=tap, var=0.45)
        # rootlets under the main root
        for _ in range(int(n * step / (1.4 if is_row else 2.4))):
            k = rnd.randint(2, n - 3)
            (x0, y0), (x1, y1) = c[k], c[k + 1]
            sd = rnd.choice((-1, 1))
            aa = math.atan2(y1 - y0, x1 - x0) + sd * rnd.uniform(0.35, 0.7)
            pts, x, y = [(x0, y0)], x0, y0
            for _ in range(rnd.randint(7, 12)):
                aa += sd * rnd.uniform(-0.05, 0.16)
                x += math.cos(aa) * 0.05 * u
                y += math.sin(aa) * 0.05 * u
                pts.append((x, y))
            body(d, rnd, pts, swell(rnd, len(pts), hw[k] * 0.5, 0.05, taper_out=0.95, var=0.15), ow * 0.8, cracks=False)
        # a thin second strand braiding round the main one: under it mostly, over it in places
        comp = None
        if is_row and rnd.random() < 0.6:
            ph, fr = rnd.uniform(0, 6.3), rnd.uniform(0.9, 1.3)
            ns = normals(c)
            full = [(x + nx * math.sin(ph + i * step * fr * 3.2) * hw[i] * 1.15,
                     y + ny * math.sin(ph + i * step * fr * 3.2) * hw[i] * 1.15) for i, ((x, y), (nx, ny)) in enumerate(zip(c, ns))]
            s0 = rnd.randint(0, n // 3)
            s1 = rnd.randint(min(n, s0 + n // 3), n)
            comp = full[s0:s1]
            chw = swell(rnd, len(comp), base * 0.5, step, 0.12, 0.12, 0.25)
            body(d, rnd, comp, chw, ow, cracks=False)
        body(d, rnd, c, hw, ow)
        if comp:
            k = rnd.randint(2, max(3, len(comp) // 3))
            while k < len(comp) - 4:
                m = rnd.randint(4, 7)
                body(d, rnd, comp[k:k + m], chw[k:k + m], ow, cracks=False, shadow=False)
                k += m + rnd.randint(6, 12)
        for _ in range(int(n * step / 0.7)):
            k = rnd.randint(0, n - 1)
            later.append(("crumbs", c[k][0], c[k][1] + hw[k] * rnd.choice((-1.3, 1.3)), hw[k]))
    # roots running from the grid's left corners to the castle wall feet, where they start climbing
    # (castle in Level4/5: near jamb face x -10.21 z -7.41..-4.93; door-frame chamfer
    # (-11.04, z 4.42) -> (-10.21, z 5.25); ground is flat -0.25 only for z -5.5..5.3)
    for pts_w in ([(-9.16, -5.04), (-9.6, -5.2), (-10.0, -5.15), (-10.19, -5.0)],
                  [(-9.16, -5.04), (-9.7, -4.95), (-10.19, -5.35)],
                  [(-9.16, 4.96), (-9.8, 4.85), (-10.4, 4.8), (-10.95, 4.55)],
                  [(-9.16, 4.96), (-9.7, 5.05), (-10.19, 5.24)]):
        dense = []
        for (ax, az), (bx, bz) in zip(pts_w, pts_w[1:]):
            seg = meander(rnd, P(ax, az), P(bx, bz), u, 0.03, step)
            dense += seg if not dense else seg[1:]
        hw = swell(rnd, len(dense), base * 0.85, step, taper_in=0.0, taper_out=0.0, var=0.3)
        body(d, rnd, dense, hw, ow)
        for _ in range(2):
            k = rnd.randint(0, len(dense) - 1)
            later.append(("crumbs", dense[k][0], dense[k][1] + hw[k] * 1.3, hw[k]))
    for kind, x, y, r in later:
        if kind == "mound":
            mound(d, rnd, x, y, r)
            crumbs(d, rnd, x, y, r * 1.3, 3, u)
        else:
            crumbs(d, rnd, x, y, r * 0.8, rnd.randint(1, 3), u)
    gv.finish(img, path)
    with open(os.path.splitext(path)[0] + ".rect.txt", "w") as f:
        f.write("%.4f %.4f %.4f %.4f\n" % (X0, Z0, X1, Z1))


# trees / stumps around the path (same in Level4 + Level5): (x, z, trunk radius). Their roots reach
# across the grass bank and join the grid's outer line, so the grid grows out of the forest.
TOP_ANCHORS = []   # user removed the tree/stump roots on the top bank (2026-11-15)
BOTTOM_ANCHORS = [(-5.12, -7.16, 0.6), (0.10, -7.67, 0.55)]


def wander_root(d, rnd, x, y, ang, length, base, ow, u, depth=0, ends=None):
    """Free root on the ground from (x, y) px heading ang: drifts, forks, tapers to a point."""
    step = 0.06
    pts, turn = [(x, y)], 0.0
    for _ in range(max(3, int(length / step))):
        turn = max(-1.0, min(1.0, turn + rnd.uniform(-0.3, 0.3)))
        ang += turn * 0.06
        x += math.cos(ang) * step * u
        y += math.sin(ang) * step * u
        pts.append((x, y))
    n = len(pts)
    hw = swell(rnd, n, base, step, taper_in=0.0, taper_out=0.85, var=0.3)
    if depth < 2:
        for _ in range(rnd.choice((0, 1, 1, 2))):
            k = rnd.randint(n // 4, n * 2 // 3)
            wander_root(d, rnd, pts[k][0], pts[k][1], ang + rnd.choice((-1, 1)) * rnd.uniform(0.4, 0.8),
                        length * (1 - k / n) * rnd.uniform(0.6, 0.9), hw[k] * 0.75, ow, u, depth + 1, ends)
    body(d, rnd, pts, hw, ow, cracks=depth == 0)
    if ends is not None:
        ends.append(pts[-1])


def off_grid(d, rnd, P, u, base, ow, later):
    """Roots running off the grid into the scene, drawn UNDER the grid roots so they emerge from them."""
    top, bot, right = GZ0 + ROWS * CELL, GZ0, GX0 + COLS * CELL
    # tree + stump roots reaching the grid's top / bottom line
    for anchors, line, sgn in ((TOP_ANCHORS, top, 1), (BOTTOM_ANCHORS, bot, -1)):
        for ax, az, r in anchors:
            # not a straight stick: the root leaves the trunk heading down the bank, then bends and
            # runs INTO the grid line at a shallow angle, so it merges with it like a joining root
            dx = 1 if ax < (GX0 + right) / 2 else -1
            drop = abs(az - line)
            tx = max(GX0 + 0.3, min(right - 0.3, ax + dx * rnd.uniform(1.4, 1.9)))
            p0 = (ax, az + sgn * 0.1)          # starts under the trunk, so no cut end shows
            p1 = (ax + dx * 0.15, az - sgn * drop * 0.6)
            p2 = (tx - dx * 0.9, line + sgn * 0.08)
            p3 = (tx, line - sgn * 0.03)
            c, steps = [], max(8, int(drop * 2 / 0.06))
            for i in range(steps + 1):
                t = i / steps
                wx = (1-t)**3*p0[0] + 3*(1-t)**2*t*p1[0] + 3*(1-t)*t*t*p2[0] + t**3*p3[0]
                wz = (1-t)**3*p0[1] + 3*(1-t)**2*t*p1[1] + 3*(1-t)*t*t*p2[1] + t**3*p3[1]
                wz += 0.05 * math.sin(t * 9 + ax) * math.sin(t * math.pi)       # slight natural wobble
                c.append(P(wx, wz))
            n = len(c)
            hw = swell(rnd, n, base * 1.2, 0.06, taper_in=0.0, taper_out=0.0, var=0.2)
            hw = [h * (1 - 0.55 * (i / n) ** 0.7) for i, h in enumerate(hw)]   # wide at the trunk
            body(d, rnd, c, hw, ow)
            # two short buttress roots flaring from the trunk foot, so the tree sits IN the ground
            for side in (-1, 1):
                # image y points down (= toward smaller z): "down the bank" is +pi/2 for the top bank
                ang = (math.pi / 2 if sgn > 0 else -math.pi / 2) + side * rnd.uniform(0.9, 1.3)
                sx, sy = P(ax + side * r * 0.2, az)
                L = rnd.uniform(0.3, 0.45)
                bp = [(sx + math.cos(ang) * L * u * t, sy + math.sin(ang) * L * u * t) for t in (0, 0.35, 0.7, 1.0)]
                body(d, rnd, bp, [base * 1.0, base * 0.75, base * 0.55, base * 0.12], ow, cracks=False)
            # where it meets the line, a little soil heaped over the join
            later.append(("mound", *P(tx - dx * 0.25, line + sgn * 0.02), base * 1.6))
    # between the grid's left line and the castle door: roots reaching back to the wall, so the grid
    # looks like it grows out of the castle
    z = bot + rnd.uniform(0.2, 0.5)
    while z < top - 0.2:
        sx, sy = P(GX0, z)
        wander_root(d, rnd, sx, sy, math.pi + rnd.uniform(-0.3, 0.3), rnd.uniform(2.4, 3.2), base * 1.0, ow, u)
        z += rnd.uniform(0.7, 1.1)
    # along the open path to the right: forking roots that fade out under the dirt
    ends = []
    z = bot + rnd.uniform(0.2, 0.8)
    while z < top - 0.2:
        sx, sy = P(right, z)
        wander_root(d, rnd, sx, sy, rnd.uniform(-0.6, 0.6), rnd.uniform(1.4, 2.9), base * 0.85, ow, u, ends=ends)
        z += rnd.uniform(1.0, 1.8)
    for ex, ey in ends:
        if rnd.random() < 0.35:
            later.append(("mound", ex, ey, base * rnd.uniform(1.0, 1.4)))


# ---------------------------------------------------------------- roots climbing the castle
def climber_root(d, rnd, x, y, height, u, base, ow, lo, hi, depth=0, lean=0.0):
    """A root gripping its way up a castle wall from (x, y) px. height is in WORLD units.
    Mostly upward with a gentle lean and wander (kept inside lo..hi px), thick at the foot,
    tapering to a point; forks into thinner tendrils that splay out like fingers on the stone."""
    step = 0.05
    n = max(4, int(height / step))
    pts, ang, turn = [(x, y)], -math.pi / 2 + lean, 0.0
    for _ in range(n):
        turn = max(-1.0, min(1.0, turn * 0.92 + rnd.uniform(-0.4, 0.4)))
        ang += turn * 0.13                               # snakes over the stone
        ang += (-math.pi / 2 + lean * 0.6 - ang) * 0.035 # but keeps creeping up
        if x < lo + 0.08 * u: ang += 0.08
        if x > hi - 0.08 * u: ang -= 0.08
        x = max(lo, min(hi, x + math.cos(ang) * step * u))
        y += math.sin(ang) * step * u
        pts.append((x, y))
    n = len(pts)
    hw = swell(rnd, n, base, step, taper_in=0.0, taper_out=0.75, var=0.25)
    hw = [h * (1 - 0.45 * i / n) for i, h in enumerate(hw)]
    # forks: thinner tendrils peeling off to the sides (drawn first, so they leave from under it)
    if depth < 3:
        forks = rnd.choice((2, 3)) if depth == 0 else rnd.choice((1, 1, 2)) if depth == 1 else rnd.choice((0, 1))
        for _ in range(forks):
            k = rnd.randint(n // 5, n * 3 // 4)
            sd = rnd.choice((-1, 1))
            climber_root(d, rnd, pts[k][0], pts[k][1], height * (1 - k / n) * rnd.uniform(0.6, 0.95), u,
                         hw[k] * 0.72, ow, lo, hi, depth + 1, lean=sd * rnd.uniform(0.8, 1.2))
    # a few short side shoots hugging the stone (soft, angled upward, not thorny spikes)
    for _ in range(0):
        k = rnd.randint(n // 5, n * 3 // 4)
        sd = rnd.choice((-1, 1))
        a = -math.pi / 2 + sd * rnd.uniform(0.7, 1.0)
        L = rnd.uniform(0.18, 0.32) * u
        sx, sy = pts[k]
        sp = [(sx + math.cos(a) * L * t + sd * 0.02 * u * math.sin(t * 3), sy + math.sin(a) * L * t) for t in (0, 0.35, 0.7, 1.0)]
        body(d, rnd, sp, [hw[k] * 0.55, hw[k] * 0.4, hw[k] * 0.25, hw[k] * 0.08], ow * 0.8, cracks=False, shadow=False)
    body(d, rnd, pts, hw, ow, cracks=depth == 0)


def wall_roots(path, width, heights, seed, ppu=110):
    """Roots climbing a castle wall face: image = width x (max height + 0.4) world units, the wall
    foot on the bottom edge. A runner hugs the foot of the wall (placed a little below the ground so
    it reads as coming out of the earth) and each climber rises from it."""
    rnd = random.Random(seed)
    u = ppu * SS
    top_pad = 0.4
    W, H = int(width * u), int((max(heights) + top_pad) * u)
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    base, ow = 0.075 * u, 0.014 * u
    lo, hi = 0.1 * u, W - 0.1 * u
    foot = H - 0.06 * u
    n = len(heights)
    xs = [lo + (hi - lo) * (i + rnd.uniform(0.3, 0.7)) / n for i in range(n)]
    for x, h in zip(xs, heights):
        climber_root(d, rnd, x, foot, h, u, base * rnd.uniform(0.9, 1.1), ow, lo, hi,
                     lean=rnd.uniform(-0.15, 0.15))
    # runner along the wall foot, over the climbers' feet
    run = meander(rnd, (0.0, foot), (W, foot), u, 0.025, 0.05)
    body(d, rnd, run, swell(rnd, len(run), base * 1.05, 0.05, var=0.3), ow)
    gv.finish(img, path, shadow=(2, 3, 3, 0.5))


# ---------------------------------------------------------------- glowing mushrooms
# caps are darker/more saturated than they look in game: bloom + tonemapping wash bright caps to white
TEAL = dict(cap=(40, 196, 170, 255), cap_lit=(110, 236, 210, 255), spot=(220, 255, 246, 255),
            gill=(22, 120, 112, 255), stem=(170, 214, 206, 255), stem_dark=(110, 160, 156, 255),
            glow=(90, 255, 215))
PINK = dict(cap=(196, 92, 214, 255), cap_lit=(236, 160, 246, 255), spot=(255, 230, 255, 255),
            gill=(120, 52, 138, 255), stem=(214, 188, 220, 255), stem_dark=(162, 132, 172, 255),
            glow=(215, 130, 255))


def mushroom(d, cx, by, h, capw, c, ow, lean):
    """Upright toon mushroom (seen from the 45-degree game camera): short tapered stem, round dome
    cap with a gill rim showing underneath. Stem foot at (cx, by) px, total height h px."""
    cw, ch = capw / 2, capw * 0.38          # cap half-width, dome height
    ty = by - h + ch                          # cap rim line
    tx = cx + lean * h
    sw0, sw1 = capw * 0.17, capw * 0.13       # stem half-width at foot / under the cap
    stem = [(cx - sw0, by), (tx - sw1, ty), (tx + sw1, ty), (cx + sw0, by)]
    rim = [tx - cw, ty - ch * 0.22, tx + cw, ty + ch * 0.3]      # gill underside ellipse
    dome = [(tx + math.cos(t) * cw, ty - math.sin(t) * ch) for t in [k * math.pi / 32 for k in range(33)]]
    def grown(poly, k, ox, oy):
        return [(x + (x - ox) / max(1, math.hypot(x - ox, y - oy)) * k, y + (y - oy) / max(1, math.hypot(x - ox, y - oy)) * k) for x, y in poly]
    # outline pass (all shapes grown), then fills
    d.polygon([(stem[0][0] - ow, by + ow * 0.6), (stem[1][0] - ow, ty), (stem[2][0] + ow, ty), (stem[3][0] + ow, by + ow * 0.6)], fill=OUTLINE)
    d.ellipse([rim[0] - ow, rim[1] - ow, rim[2] + ow, rim[3] + ow], fill=OUTLINE)
    d.polygon(grown(dome, ow, tx, ty), fill=OUTLINE)
    d.polygon(stem, fill=c["stem"])
    mid_b, mid_t = cx + sw0 * 0.25, tx + sw1 * 0.25
    d.polygon([(mid_b, by), (mid_t, ty), stem[2], stem[3]], fill=c["stem_dark"])
    d.ellipse(rim, fill=c["gill"])
    d.polygon(dome, fill=c["cap"])
    lit = [(tx - cw * 0.25 + math.cos(t) * cw * 0.62, ty - ch * 0.18 - math.sin(t) * ch * 0.72) for t in [math.pi * (0.45 + 0.55 * k / 16) for k in range(17)]]
    d.polygon(lit + [(tx - cw * 0.25, ty - ch * 0.18)], fill=c["cap_lit"])
    for sx, sy, sr in ((0.38, 0.5, 0.11), (0.05, 0.78, 0.075), (0.66, 0.24, 0.065)):
        r = capw * sr
        px, py = tx + sx * cw, ty - sy * ch
        d.ellipse([px - r, py - r * 0.8, px + r, py + r * 0.8], fill=c["spot"])


def shroom_cluster(path, rnd, palette, ppu=220):
    """2-3 mushrooms of different sizes, feet on the image's bottom edge (pivot bottom-centre),
    with a soft halo so they glow even before bloom."""
    u = ppu * SS
    W, H = int(1.0 * u), int(0.62 * u)
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    ow = 0.022 * u
    n = rnd.choice((2, 3, 3))
    specs = []
    for i in range(n):
        big = i == 0
        h = (rnd.uniform(0.40, 0.48) if big else rnd.uniform(0.2, 0.27)) * u
        capw = h * rnd.uniform(1.05, 1.2)
        side = (-1, 1)[i % 2] if i else 0
        x = W / 2 + side * rnd.uniform(0.24, 0.29) * u
        specs.append((x, H - 0.05 * u - (0 if big else rnd.uniform(0, 0.04) * u), h, capw, rnd.uniform(-0.12, 0.12)))
    specs.sort(key=lambda s: s[1])  # back ones first
    for x, by, h, capw, lean in specs:
        mushroom(d, x, by, h, capw, palette, ow, lean)
    w, h = img.size
    img = img.resize((w // SS, h // SS), Image.LANCZOS)
    a = img.split()[3].filter(ImageFilter.GaussianBlur(9)).point(lambda v: int(v * 0.55))
    halo = Image.new("RGBA", img.size, palette["glow"] + (0,))
    halo.putalpha(a)
    halo.alpha_composite(img)
    halo.save(path)
    print("wrote", path, halo.size)


def glow_pool(path, size=256):
    """White radial falloff, tinted per cluster by the SpriteRenderer colour."""
    img = Image.new("RGBA", (size, size), (255, 255, 255, 0))
    px = img.load()
    c = size / 2
    for y in range(size):
        for x in range(size):
            r = math.hypot(x + 0.5 - c, y + 0.5 - c) / c
            a = max(0.0, 1 - r) ** 2.2
            px[x, y] = (255, 255, 255, int(255 * a))
    img.save(path)
    print("wrote", path, img.size)


if __name__ == "__main__":
    root_dir, level = sys.argv[1], sys.argv[2]
    seed, share, (wt, wp) = VARIANTS[level]
    out = os.path.join(root_dir, "Assets", "Sprites", "Roots", level)
    os.makedirs(out, exist_ok=True)
    floor(os.path.join(out, "forest_roots_floor.png"), seed)
    # castle roots + mushrooms are 3D now (PlaceForestRoots: low-poly ForestBark tubes, copies of the
    # scene's GlowingMushrooms clusters); only the floor art and the ground glow are generated here
    glow_pool(os.path.join(out, "forest_shroom_glow.png"))
