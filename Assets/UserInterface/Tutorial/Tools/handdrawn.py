"""Hand-drawn helpers: wobbly shapes, uneven ink outlines and cel shading, so generated sprites
sit next to the game's painted art (cat wizard staff, tower cards) instead of looking like clip-art.

Everything is drawn as boolean masks at SS x resolution, then flattened and scaled down.
"""
import math

import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage

SS = 4
INK = (29, 21, 40, 255)


def field(h, w, sigma, seed):
    """Smooth random field, roughly in [-1, 1]."""
    rng = np.random.default_rng(seed)
    f = ndimage.gaussian_filter(rng.standard_normal((h, w)), sigma)
    return np.clip(f / (f.std() + 1e-6) / 2.0, -1, 1)


def densify(pts, step=1.5, closed=True):
    out = []
    pts = list(pts)
    segs = len(pts) if closed else len(pts) - 1
    for i in range(segs):
        (x0, y0), (x1, y1) = pts[i], pts[(i + 1) % len(pts)]
        n = max(1, int(math.hypot(x1 - x0, y1 - y0) / step))
        for k in range(n):
            t = k / n
            out.append((x0 + (x1 - x0) * t, y0 + (y1 - y0) * t))
    if not closed:
        out.append(pts[-1])
    return out


def wobble(pts, amp, seed, wavelength=14.0, closed=True):
    """Push each point along its normal by a smooth random amount (shaky hand line)."""
    p = np.array(pts, float)
    n = len(p)
    seglen = np.hypot(*np.diff(np.vstack([p, p[:1]]) if closed else p, axis=0).T)
    s = np.concatenate([[0], np.cumsum(seglen)])[:n]
    total = s[-1] + (seglen[-1] if closed else 0)
    rng = np.random.default_rng(seed)
    k = max(3, int(total / wavelength))
    ctrl = rng.uniform(-1, 1, k + 1)
    if closed:
        ctrl[-1] = ctrl[0]
    t = s / max(total, 1e-6) * k
    noise = np.interp(t, np.arange(k + 1), ctrl)
    noise = ndimage.gaussian_filter1d(noise, max(1.0, n / k / 3), mode="wrap" if closed else "nearest")
    tan = np.roll(p, -1, 0) - np.roll(p, 1, 0)
    if not closed:
        tan[0] = p[1] - p[0]
        tan[-1] = p[-1] - p[-2]
    nrm = np.stack([tan[:, 1], -tan[:, 0]], 1)
    nrm /= np.linalg.norm(nrm, axis=1, keepdims=True) + 1e-9
    return [tuple(v) for v in p + nrm * (noise * amp)[:, None]]


def round_rect_pts(x0, y0, x1, y1, r, step=1.5):
    pts = []
    for cx, cy, a0 in ((x1 - r, y0 + r, -90), (x1 - r, y1 - r, 0), (x0 + r, y1 - r, 90), (x0 + r, y0 + r, 180)):
        for i in range(12):
            a = math.radians(a0 + i * 90 / 11)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    return densify(pts, step)


def bezier(p0, p1, p2, p3, n=60):
    out = []
    for i in range(n + 1):
        t = i / n
        u = 1 - t
        out.append((u ** 3 * p0[0] + 3 * u * u * t * p1[0] + 3 * u * t * t * p2[0] + t ** 3 * p3[0],
                    u ** 3 * p0[1] + 3 * u * u * t * p1[1] + 3 * u * t * t * p2[1] + t ** 3 * p3[1]))
    return out


def stroke(center, w0, w1, seed=0, amp=0.0):
    """Closed outline of a tapered stroke along a centre line (width w0 at start -> w1 at end)."""
    c = np.array(center, float)
    tan = np.gradient(c, axis=0)
    nrm = np.stack([tan[:, 1], -tan[:, 0]], 1)
    nrm /= np.linalg.norm(nrm, axis=1, keepdims=True) + 1e-9
    w = np.linspace(w0, w1, len(c))[:, None] / 2
    left = c + nrm * w
    right = c - nrm * w
    # round caps
    cap = []
    a = math.atan2(*(-tan[-1])[::-1])
    for i in range(1, 8):
        ang = math.atan2(nrm[-1, 1], nrm[-1, 0]) - math.pi * i / 8
        cap.append(tuple(c[-1] + np.array([math.cos(ang), math.sin(ang)]) * w[-1, 0]))
    pts = [tuple(v) for v in left] + cap + [tuple(v) for v in right[::-1]]
    return wobble(pts, amp, seed) if amp else pts


class Sheet:
    def __init__(self, w, h):
        self.w, self.h = w, h
        self.W, self.H = w * SS, h * SS
        self.rgba = np.zeros((self.H, self.W, 4), np.uint8)
        self.rgba[..., :3] = INK[:3]
        self.union = np.zeros((self.H, self.W), bool)

    def mask(self, pts):
        m = Image.new("L", (self.W, self.H), 0)
        ImageDraw.Draw(m).polygon([(x * SS, y * SS) for x, y in pts], fill=255)
        return np.array(m) > 127

    def ellipse(self, cx, cy, rx, ry, amp=0.0, seed=0):
        pts = [(cx + rx * math.cos(a), cy + ry * math.sin(a)) for a in np.linspace(0, 2 * math.pi, 90, endpoint=False)]
        return self.mask(wobble(pts, amp, seed) if amp else pts)

    def paint(self, m, color):
        self.rgba[m] = color
        self.union |= m

    def edge(self, m, width, seed=1, var=0.35):
        """Ink line just inside the mask's edge (uneven width, like a brush)."""
        d = ndimage.distance_transform_edt(m)
        f = field(self.H, self.W, 10 * SS, seed)
        self.rgba[m & (d <= width * SS * (1 + var * f))] = INK

    def outline(self, width, seed=2, var=0.35):
        """Heavier ink silhouette around everything painted so far."""
        d = ndimage.distance_transform_edt(~self.union)
        f = field(self.H, self.W, 12 * SS, seed)
        ring = (~self.union) & (d <= width * SS * (1 + var * f))
        self.rgba[ring] = INK
        self.union |= ring

    def line(self, pts, width, color=INK):
        img = Image.new("L", (self.W, self.H), 0)
        ImageDraw.Draw(img).line([(x * SS, y * SS) for x, y in pts], fill=255, width=max(1, int(width * SS)), joint="curve")
        m = np.array(img) > 127
        self.rgba[m] = color
        self.union |= m
        return m

    def image(self):
        a = np.where(self.union, 255, 0).astype(np.uint8)
        out = self.rgba.copy()
        out[..., 3] = a
        return Image.fromarray(out, "RGBA").resize((self.w, self.h), Image.LANCZOS)


def shift(m, dx, dy):
    """Mask moved by (dx, dy) final pixels, empty fill."""
    dx, dy = int(round(dx * SS)), int(round(dy * SS))
    out = np.zeros_like(m)
    H, W = m.shape
    ys = slice(max(0, dy), min(H, H + dy))
    yd = slice(max(0, -dy), min(H, H - dy))
    xs = slice(max(0, dx), min(W, W + dx))
    xd = slice(max(0, -dx), min(W, W - dx))
    out[ys, xs] = m[yd, xd]
    return out


def band(m, dx, dy):
    """Pixels of m within (dx, dy) of the edge on the side opposite the shift: cel light/shade bands."""
    return m & ~shift(m, dx, dy)
