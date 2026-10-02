"""Builds kit.glb — props, backdrops and 15 artifacts for 유적 발굴단 (all original).

Run: Blender -b --factory-startup --python factory/games/yujeok-balgul/blender/build_kit.py
Every root object sits at world origin with its origin at bottom-centre (y=0), authored in
three.js space (y up, +z = front). Children: Lizard/Tail (tail base), Torch/Flame (flame base).
"""
import sys, json, math
from pathlib import Path
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from common import *  # noqa

OUT = HERE.parents[3] / 'public' / 'g' / 'yujeok-balgul' / 'assets' / 'models'
OUT.mkdir(parents=True, exist_ok=True)
reset_scene()
TRIS = {}


def finish(b, name, parent=None, pivot=(0, 0, 0), parent_pivot=(0, 0, 0), clamp=True):
    if clamp and parent is None:
        b.clamp_ground()   # origin = bottom-centre: nothing may dip below y=0
    TRIS[name] = b.tri_count()
    return b.to_object(name, pivot, parent=parent, parent_pivot=parent_pivot)


def ring_pts(hw, hd, ch, y, mid=2, jit=0.0, rng=None):
    """Chamfered rectangle (counter-clockwise from +x,+z corner) with `mid` points per edge."""
    corners = [(1, 1), (-1, 1), (-1, -1), (1, -1)]
    pts = []
    for k, (sx, sz) in enumerate(corners):
        c = ch[k]
        nx, nz = corners[(k + 1) % 4]
        # start of chamfer on this corner (coming from previous edge) and end
        if sx == sz:
            a = (sx * hw, y, sz * (hd - c))
            bb = (sx * (hw - c), y, sz * hd)
        else:
            a = (sx * (hw - c), y, sz * hd)
            bb = (sx * hw, y, sz * (hd - c))
        pts += [a, bb]
        # intermediate points toward the next corner's first point
        nc = ch[(k + 1) % 4]
        if nx == nz:
            na = (nx * hw, y, nz * (hd - nc))
        else:
            na = (nx * (hw - nc), y, nz * hd)
        for m in range(1, mid + 1):
            t = m / (mid + 1)
            p = [bb[0] + (na[0] - bb[0]) * t, y, bb[2] + (na[2] - bb[2]) * t]
            if jit and rng:
                # push inward along edge normal for chipped look
                if abs(na[0] - bb[0]) > abs(na[2] - bb[2]):
                    p[2] -= math.copysign(rng.uniform(0, jit), p[2])
                else:
                    p[0] -= math.copysign(rng.uniform(0, jit), p[0])
            pts.append(tuple(p))
    return pts


# =============================== props ===============================
def tablet():
    b = Builder(101)
    r0 = ring_pts(0.475, 0.325, [0.06, 0.05, 0.075, 0.055], 0.0, jit=0.012, rng=b.rng)
    r1 = ring_pts(0.50, 0.35, [0.075, 0.05, 0.09, 0.06], 0.095, jit=0.014, rng=b.rng)
    r2 = ring_pts(0.47, 0.32, [0.03, 0.03, 0.03, 0.03], 0.14)
    b.loft([r0, r1, r2], ['stone', 'stone'], cap0='stone', cap1='stone_top')
    finish(b, 'Tablet')


def pot():
    b = Builder(102)
    prof = [(0.0, 0.0), (0.07, 0.0), (0.105, 0.03), (0.15, 0.11), (0.16, 0.19), (0.155, 0.25), (0.125, 0.33),
            (0.08, 0.39), (0.062, 0.43), (0.072, 0.465), (0.088, 0.50), (0.066, 0.50), (0.06, 0.485)]
    bands = ['clay', 'clay', 'clay', 'clay_dark', 'clay', 'clay', 'clay', 'clay', 'clay', 'clay_dark', 'clay_dark', 'clay_dark']
    b.lathe(prof, bands, n=10, cap1='clay_dark')
    b.lathe([(0.157, 0.205), (0.162, 0.225)], 'clay_dark', n=10)
    for sx in (1, -1):
        b.tube([(sx * 0.065, 0.445, 0), (sx * 0.135, 0.45, 0), (sx * 0.16, 0.39, 0), (sx * 0.14, 0.32, 0)],
               0.018, 'clay', n=6)
    finish(b, 'Pot')


def bone(b, a, c, r=0.02):
    a, c = v3(a), v3(c)
    b.cyl(a, c, r, 'bone', n=6)
    d = (c - a).normalized()
    side = d.cross(Vector((0, 1, 0))).normalized() * (r * 0.9)
    for e in (a, c):
        b.ico(e + side, r * 1.45, 'bone', sub=1)
        b.ico(e - side, r * 1.45, 'bone', sub=1)


def bones():
    b = Builder(103)
    bone(b, (-0.2, 0.029, -0.08), (0.12, 0.029, -0.14))
    bone(b, (-0.16, 0.029, 0.12), (0.06, 0.029, -0.02), r=0.018)
    bone(b, (-0.1, 0.07, -0.13), (0.0, 0.035, 0.12), r=0.017)
    # cute skull facing +Z
    sc = (0.12, 0.072, 0.06)
    b.sphere(sc, (0.075, 0.07, 0.072), 'bone', u=10, v=7)
    b.box((0.12, 0.022, 0.1), (0.075, 0.044, 0.05), 'bone', bevel=0.012)
    for sx in (1, -1):
        b.ico((0.12 + sx * 0.03, 0.08, 0.122), (0.02, 0.024, 0.012), 'black', sub=1)
    b.ico((0.12, 0.05, 0.13), (0.008, 0.01, 0.006), 'black', sub=1)
    for k in range(3):
        b.box((0.12 - 0.02 + k * 0.02, 0.02, 0.126), (0.006, 0.02, 0.004), 'black')
    finish(b, 'Bones')


def scroll():
    b = Builder(104)
    b.box((0, 0.012, 0), (0.30, 0.024, 0.11), 'wood', bevel=0.006)
    for sx in (1, -1):
        b.box((sx * 0.12, 0.045, 0), (0.035, 0.05, 0.11), 'wood_dark', bevel=0.006)
    cy = 0.112
    b.cyl((-0.19, cy, 0), (0.19, cy, 0), 0.055, 'paper', n=10)
    for sx in (1, -1):
        b.cyl((sx * 0.19, cy, 0), (sx * 0.215, cy, 0), 0.02, 'wood', n=6)
        b.ico((sx * 0.226, cy, 0), 0.026, 'wood', sub=1)
    # unrolled lip
    b.obox((0, 0, 0), (0.36, 0.008, 0.06), 'paper', T(0, cy - 0.05, 0.065) @ R('X', 25))
    # ribbon + bow
    b.cyl((-0.016, cy, 0), (0.016, cy, 0), 0.059, 'scarf', n=10)
    b.ico((0.0, cy + 0.062, 0.0), (0.016, 0.014, 0.014), 'scarf', sub=1)
    for sx in (1, -1):
        b.ico((sx * 0.026, cy + 0.066, 0.0), (0.026, 0.016, 0.012), 'scarf', sub=1,
              M=T(sx * 0.026, cy + 0.066, 0) @ R('Z', sx * 20) @ T(-sx * 0.026, -(cy + 0.066), 0))
    finish(b, 'Scroll')


def lizard():
    b = Builder(105)
    b.sphere((0, 0.042, 0.0), (0.056, 0.034, 0.11), 'lizard', u=8, v=6)
    b.sphere((0, 0.03, 0.005), (0.047, 0.02, 0.095), 'lizard_belly', u=8, v=4)
    b.sphere((0, 0.048, 0.12), (0.046, 0.032, 0.058), 'lizard', u=8, v=5)
    b.sphere((0, 0.038, 0.125), (0.038, 0.018, 0.05), 'lizard_belly', u=8, v=5)
    for sx in (1, -1):
        b.ico((sx * 0.03, 0.074, 0.135), 0.017, 'lizard', sub=1)
        b.ico((sx * 0.034, 0.08, 0.145), 0.01, 'black', sub=1)
        for fz in (0.065, -0.06):
            b.tube([(sx * 0.04, 0.035, fz), (sx * 0.085, 0.03, fz + 0.012), (sx * 0.095, 0.01, fz + 0.025)], 0.012, 'lizard', n=5)
            b.ico((sx * 0.1, 0.008, fz + 0.032), (0.02, 0.008, 0.02), 'lizard', sub=1)
    # back spots
    for z in (-0.04, 0.0, 0.04):
        b.ico((0, 0.074, z), (0.014, 0.006, 0.014), 'lizard_belly', sub=1)
    root = finish(b, 'Lizard')
    t = Builder(106)
    pivot = (0, 0.04, -0.09)
    t.tube([(0, 0.042, -0.07), (0.008, 0.036, -0.14), (0.03, 0.027, -0.21), (0.058, 0.019, -0.265), (0.075, 0.014, -0.30)],
           [0.034, 0.026, 0.017, 0.009, 0.0], 'lizard', n=6)
    finish(t, 'Tail', parent=root, pivot=pivot)


def tent():
    b = Builder(107)
    H, HW, HD = 1.6, 1.0, 0.9
    n_str = 7
    for side in (1, -1):
        for k in range(n_str):
            z0 = -HD + k * (2 * HD / n_str)
            zc = z0 + HD / n_str
            mat = 'cloth_stripe' if k % 2 == 1 else 'cloth'
            b.beam((0, H, zc), (side * HW, 0.02, zc), 2 * HD / n_str + 0.002, 0.04, mat, side=(0, 0, 1))
    # back wall + dark interior
    tri = [(0, H - 0.02), (-HW + 0.03, 0.0), (HW - 0.03, 0.0)]
    b.prism(tri, -HD, -HD + 0.04, 'cloth')
    b.prism([(0, H - 0.12), (-HW + 0.18, 0.0), (HW - 0.18, 0.0)], -HD + 0.04, -HD + 0.06, 'wood_dark')
    b.box((0, 0.006, 0), (2 * HW - 0.2, 0.012, 2 * HD - 0.1), 'wood_dark')
    # front flaps tied open
    for side in (1, -1):
        flap = [(0, H - 0.03), (side * (HW - 0.02), 0.0), (side * 0.52, 0.0)]
        b.prism(flap, HD - 0.03, HD + 0.01, 'cloth_stripe')
        b.cyl((side * 0.5, 0.5, HD + 0.02), (side * 0.62, 0.36, HD + 0.02), 0.028, 'rope', n=6)
    # poles
    for z in (-HD - 0.04, HD + 0.05):
        b.cyl((0, 0, z), (0, H + 0.14, z), 0.035, 'wood', n=6)
        b.ico((0, H + 0.15, z), 0.045, 'wood', sub=1)
    b.cyl((0, H + 0.02, -HD - 0.06), (0, H + 0.02, HD + 0.07), 0.03, 'wood', n=6)
    # guy lines + stakes
    for side in (1, -1):
        for z in (-0.6, 0.6):
            top = (side * 0.62, 0.62, z)
            st = (side * 1.22, 0.04, z)
            b.cyl(top, st, 0.011, 'rope', n=4)
            b.box((st[0], 0.05, st[2]), (0.04, 0.1, 0.04), 'wood_dark')
    finish(b, 'Tent')


def crate():
    b = Builder(108)
    s = 0.5
    b.box((0, s / 2, 0), (s - 0.03, s - 0.03, s - 0.03), 'wood')
    h = s / 2
    e = 0.055
    # 12 edge beams
    for x in (-1, 1):
        for z in (-1, 1):
            b.box((x * (h - e / 2), h, z * (h - e / 2)), (e, s, e), 'wood_dark')
    for y in (0, 1):
        yy = e / 2 + y * (s - e)
        for z in (-1, 1):
            b.box((0, yy, z * (h - e / 2)), (s - 2 * e, e, e), 'wood_dark')
        for x in (-1, 1):
            b.box((x * (h - e / 2), yy, 0), (e, e, s - 2 * e), 'wood_dark')
    # plank grooves + diagonal braces on front/back and sides
    for k in (1, 2):
        y = e + k * (s - 2 * e) / 3
        for z in (-1, 1):
            b.box((0, y, z * (h - 0.012)), (s - 2 * e, 0.008, 0.008), 'wood_dark')
        for x in (-1, 1):
            b.box((x * (h - 0.012), y, 0), (0.008, 0.008, s - 2 * e), 'wood_dark')
    for z in (-1, 1):
        b.beam((-h + e, e, z * (h - 0.01)), (h - e, s - e, z * (h - 0.01)), 0.05, 0.02, 'wood_dark', side=(0, 0, 1))
    for k in (1, 2):
        x = -h + e + k * (s - 2 * e) / 3
        b.box((x, s - 0.012, 0), (0.008, 0.008, s - 2 * e), 'wood_dark')
    finish(b, 'Crate')


def cart():
    b = Builder(109)
    # tray (frustum) with sand load
    r0 = [(0.15, 0.24, 0.17), (-0.15, 0.24, 0.17), (-0.15, 0.24, -0.17), (0.15, 0.24, -0.17)]
    r1 = [(0.25, 0.47, 0.30), (-0.25, 0.47, 0.30), (-0.25, 0.47, -0.27), (0.25, 0.47, -0.27)]
    r1i = [(0.22, 0.47, 0.27), (-0.22, 0.47, 0.27), (-0.22, 0.47, -0.24), (0.22, 0.47, -0.24)]
    b.loft([r0, r1, r1i], 'metal', cap0='metal', cap1='rock')
    b.ico((0, 0.46, 0.02), (0.2, 0.07, 0.24), 'rock', sub=2, jitter=0.08)
    # wheel
    wz = 0.33
    b.cyl((-0.04, 0.13, wz), (0.04, 0.13, wz), 0.13, 'metal', n=12)
    b.cyl((-0.055, 0.13, wz), (0.055, 0.13, wz), 0.035, 'wood_dark', n=6)
    # frame / handles / legs
    for sx in (1, -1):
        b.beam((sx * 0.06, 0.13, wz), (sx * 0.2, 0.42, -0.44), 0.035, 0.035, 'wood', side=(1, 0, 0))
        b.cyl((sx * 0.2, 0.42, -0.44), (sx * 0.215, 0.45, -0.52), 0.024, 'wood_dark', n=6)
        b.beam((sx * 0.14, 0.27, -0.16), (sx * 0.15, 0.0, -0.2), 0.035, 0.035, 'wood', side=(1, 0, 0))
        b.box((sx * 0.15, 0.012, -0.2), (0.05, 0.024, 0.06), 'wood_dark')
    finish(b, 'Cart')


def torch():
    b = Builder(110)
    b.lathe([(0.07, 0.0), (0.05, 0.06), (0.032, 0.1)], 'wood_dark', n=6)
    b.cyl((0, 0.08, 0), (0, 0.9, 0), 0.032, 'wood_dark', n=6, r2=0.028)
    for y in (0.74, 0.8):
        b.cyl((0, y, 0), (0, y + 0.035, 0), 0.036, 'rope', n=6)
    b.lathe([(0.03, 0.86), (0.1, 0.94), (0.125, 1.0), (0.105, 1.0), (0.0, 0.985)], 'metal', n=8)
    root = finish(b, 'Torch')
    f = Builder(111)
    base = 0.97
    f.lathe([(0.0, base), (0.09, base + 0.03), (0.095, base + 0.09), (0.06, base + 0.16), (0.0, base + 0.25)],
            'flame', n=5, M=T(0, 0, 0) @ R('Y', 10))
    f.lathe([(0.0, base + 0.02), (0.045, base + 0.06), (0.035, base + 0.11), (0.0, base + 0.17)], 'flame', n=4,
            M=T(0.06, base, 0.02) @ R('Z', -18) @ T(0, -base, 0))
    f.lathe([(0.0, base + 0.02), (0.04, base + 0.05), (0.03, base + 0.09), (0.0, base + 0.15)], 'flame', n=4,
            M=T(-0.055, base, -0.03) @ R('Z', 16) @ T(0, -base, 0))
    finish(f, 'Flame', parent=root, pivot=(0, base, 0))


def palm():
    b = Builder(112)
    segs = 8
    top_t = 1.0
    P = lambda t: Vector((0.38 * t * t, 2.3 * t, 0.05 * t))
    b.lathe([(0.17, 0.0), (0.14, 0.12)], 'bark', n=7)
    for i in range(segs):
        t0, t1 = i / segs, (i + 1) / segs
        a, c = P(t0), P(t1)
        r = 0.13 - 0.045 * t0
        b.cyl(a, c + (c - a) * 0.04, r * 1.12, 'bark', n=7, r2=r * 0.86, phase=i * 0.3)
    crown = P(top_t)
    b.sphere(crown + Vector((0, 0.05, 0)), (0.16, 0.12, 0.16), 'leaf_dark', u=7, v=4)
    for k in range(3):
        ang = k * math.tau / 3 + 0.4
        b.ico(crown + Vector((math.cos(ang) * 0.1, -0.08, math.sin(ang) * 0.1)), 0.07, 'wood_dark', sub=1)
    nf = 7
    for f in range(nf):
        phi = f * math.tau / nf + 0.2
        L = 1.15 + 0.1 * math.sin(f * 2.3)
        dirv = Vector((math.cos(phi), 0, math.sin(phi)))
        perp = Vector((-dirv.z, 0, dirv.x))
        rings = []
        steps = 9
        for s in range(steps + 1):
            u = s / steps
            h = 0.28 * math.sin(math.pi * u * 0.75) - 0.75 * u * u
            c = crown + dirv * (L * u) + Vector((0, 0.08 + h, 0))
            w = (0.2 * math.sin(math.pi * min(u * 1.1, 1.0)) ** 0.7 + 0.02) * (1.0 if s % 2 == 0 else 0.78)
            fold = 0.05 * w / 0.2
            # ordered so the frond's faces point up (+Y) toward the game camera
            rings.append([c - perp * w + Vector((0, -fold, 0)), c + Vector((0, fold * 0.6, 0)), c + perp * w + Vector((0, -fold, 0))])
        if True:
            # rings are open strips of 3 points -> use loft with open_ring
            mat = 'leaf' if f % 2 == 0 else 'leaf_dark'
            b.loft(rings, mat, open_ring=True, recalc=False)
    finish(b, 'Palm')


def jungle_tree():
    b = Builder(113)
    b.tube([(0, 0, 0), (0.02, 0.5, 0.0), (0.08, 1.0, -0.02), (0.1, 1.5, 0.0)], [0.2, 0.15, 0.13, 0.11], 'bark', n=7)
    for k in range(5):
        a = k * math.tau / 5 + 0.3
        b.cyl((math.cos(a) * 0.1, 0.25, math.sin(a) * 0.1), (math.cos(a) * 0.32, 0.0, math.sin(a) * 0.32), 0.07, 'bark', n=5, r2=0.03)
    b.tube([(0.06, 1.05, 0), (0.4, 1.5, 0.15), (0.6, 1.75, 0.2)], [0.07, 0.05, 0.04], 'bark', n=5)
    b.tube([(0.05, 1.2, 0), (-0.35, 1.6, -0.1), (-0.55, 1.8, -0.1)], [0.07, 0.05, 0.04], 'bark', n=5)
    blobs = [((0.05, 2.05, 0.0), 0.72, 'leaf'), ((0.62, 1.85, 0.2), 0.48, 'leaf_dark'), ((-0.55, 1.9, -0.1), 0.52, 'leaf'),
             ((0.1, 2.35, -0.3), 0.5, 'leaf_dark'), ((-0.25, 2.2, 0.42), 0.46, 'leaf_dark'), ((0.4, 2.3, -0.3), 0.42, 'leaf'),
             ((-0.05, 2.45, 0.15), 0.36, 'leaf')]
    for c, r, m in blobs:
        b.sphere(c, (r, r * 0.8, r), m, u=8, v=5, phase=r * 3)
    finish(b, 'JungleTree')


def ice_crystal():
    b = Builder(114)
    b.ico((0, 0.06, 0), (0.42, 0.14, 0.36), 'ice_dark', sub=2, jitter=0.12)
    b.clamp_ground()
    shards = [(0.95, 0.12, 0, 0, 'ice'), (0.62, 0.095, 24, 40, 'ice_dark'), (0.55, 0.09, 28, 160, 'ice'),
              (0.42, 0.08, 32, 250, 'ice_dark'), (0.7, 0.1, 17, 300, 'ice'), (0.34, 0.07, 38, 95, 'ice')]
    for h, r, tilt, d, m in shards:
        M = R('Y', d) @ T(0.1 if tilt else 0, 0.04, 0) @ R('Z', -tilt)
        b.lathe([(r, 0.0), (r * 1.05, h * 0.72), (0.0, h)], m, n=6, M=M, phase=0.3)
    b.clamp_ground()
    finish(b, 'IceCrystal')


def cactus():
    b = Builder(115)
    alt = lambda j, i: 'leaf' if i % 2 == 0 else 'leaf_dark'
    b.lathe([(0.12, 0.0), (0.135, 0.1), (0.135, 0.86), (0.115, 0.98), (0.065, 1.06), (0.0, 1.085)], alt, n=8,
            cap0='leaf_dark', cap1='leaf')
    b.tube([(0.1, 0.42, 0), (0.24, 0.43, 0), (0.29, 0.5, 0), (0.3, 0.6, 0), (0.3, 0.76, 0)], 0.072, alt, n=8, cap1=False)
    b.sphere((0.3, 0.76, 0), 0.072, 'leaf', u=8, v=4)
    b.tube([(-0.1, 0.32, 0), (-0.21, 0.33, 0), (-0.25, 0.4, 0), (-0.26, 0.48, 0), (-0.26, 0.6, 0)], 0.062, alt, n=8, cap1=False)
    b.sphere((-0.26, 0.6, 0), 0.062, 'leaf', u=8, v=4)
    for k in range(3):
        a = k * math.tau / 3
        b.ico((math.cos(a) * 0.035, 1.085, math.sin(a) * 0.035), (0.03, 0.02, 0.03), 'gem', sub=1)
    finish(b, 'Cactus')


def rock(name, size, seed):
    b = Builder(seed)
    sx, sy, sz = size
    b.ico((0, sy * 0.38, 0), (sx / 2, sy * 0.62, sz / 2), 'rock', sub=2, jitter=0.16)
    b.clamp_ground()
    finish(b, name)


def rope_post():
    b = Builder(116)
    b.lathe([(0.05, 0.0), (0.05, 0.49), (0.022, 0.55), (0.0, 0.552)], 'wood', n=4, phase=math.pi / 4)
    for y in (0.37, 0.42):
        pts = [(math.cos(a) * 0.062, y + 0.006 * math.sin(a * 2), math.sin(a) * 0.062) for a in
               [k * math.tau / 8 for k in range(8)]]
        b.tube(pts, 0.022, 'rope', n=4, closed=True, phase=math.pi / 4)
    b.tube([(0.06, 0.36, 0.0), (0.09, 0.28, 0.03), (0.08, 0.18, 0.06), (0.1, 0.12, 0.08)], 0.016, 'rope', n=4)
    finish(b, 'RopePost')


def pyramid():
    b = Builder(117)
    for i in range(6):
        w = 10 - 1.45 * i
        b.box((0, i + 0.5, 0), (w, 1.0, w), 'stone_far')
    # doorway on front (+Z) of the first two tiers
    b.prism([(-0.55, 0.0), (0.55, 0.0), (0.42, 0.8), (-0.42, 0.8)], 4.95, 5.03, 'wood_dark')
    b.box((0, 0.88, 5.0), (1.4, 0.16, 0.14), 'stone_far', bevel=0.03)
    for sx in (1, -1):
        b.box((sx * 0.68, 0.4, 5.0), (0.18, 0.8, 0.14), 'stone_far', bevel=0.03)
    finish(b, 'Pyramid')


def temple():
    b = Builder(118)
    sizes = [8.0, 6.8, 5.6, 4.4]
    for i, w in enumerate(sizes):
        b.box((0, i + 0.5, 0), (w, 1.0, w), 'stone_moss')
    # stairs on the front
    n = 16
    for k in range(n):
        y1 = (k + 1) * 0.25
        z = 4.55 - k * 0.15
        b.box((0, y1 / 2, z), (1.7, y1, 0.16), 'stone_moss')
    for sx in (1, -1):
        b.beam((sx * 0.95, 0.0, 4.7), (sx * 0.95, 4.0, 2.2), 0.2, 0.3, 'stone_moss', side=(1, 0, 0))
    # shrine
    b.box((0, 4.75, 0), (2.4, 1.5, 2.4), 'stone_moss', bevel=0.05)
    b.lathe([(1.75, 5.5), (0.0, 6.05)], 'stone_moss', n=4, phase=math.pi / 4)
    b.box((0, 4.6, 1.2), (0.9, 1.2, 0.08), 'wood_dark')
    # overgrowth
    rng = b.rng
    for k in range(12):
        tier = rng.randint(0, 3)
        w = sizes[tier] / 2
        side = rng.choice([(1, 0), (-1, 0), (0, -1), (0.7, 1), (-0.7, 1)])
        x = side[0] * w if abs(side[0]) == 1 else side[0] * w * 0.8
        z = side[1] * w if side[1] != 0 else rng.uniform(-w * 0.6, w * 0.6)
        if side[1] == 0:
            x = side[0] * w
        b.ico((x, tier + 1.0, z), (rng.uniform(0.35, 0.6), 0.22, rng.uniform(0.35, 0.6)), 'leaf' if k % 2 else 'leaf_dark', sub=1, jitter=0.15)
    for k in range(8):
        sx = rng.choice([-1, 1]) * rng.uniform(1.5, 3.8)
        b.box((sx, 0.6, 4.02), (0.08, 1.0, 0.04), 'leaf_dark')
    finish(b, 'Temple')


def ice_arch():
    b = Builder(119)
    pts = [(-3.0, 0.0, 0), (-3.05, 0.9, 0), (-3.0, 1.6, 0)]
    for k in range(1, 10):
        a = math.pi - k * math.pi / 10
        pts.append((math.cos(a) * 3.0, 1.6 + math.sin(a) * 3.0 * 0.95, 0))
    pts += [(3.0, 1.6, 0), (3.05, 0.9, 0), (3.0, 0.0, 0)]
    alt = lambda j, i: 'ice' if (i + j) % 3 else 'ice_dark'
    b.tube(pts, [0.85, 0.9, 0.85] + [0.8 - 0.1 * math.sin(k * math.pi / 10) for k in range(1, 10)] + [0.85, 0.9, 0.85],
           alt, n=6, ref=(0, 0, 1), aspect=(1.0, 1.15), phase=math.pi / 6)
    b.clamp_ground()
    # icicles under the arch
    for k in range(2, 9):
        a = math.pi - k * math.pi / 10
        x = math.cos(a) * 2.25
        y = 1.6 + math.sin(a) * 2.25 * 0.95
        for dz in (-0.45, 0.35):
            h = 0.35 + 0.25 * ((k * 7 + int(dz * 10)) % 3) / 2
            b.lathe([(0.12, 0.0), (0.0, -h)], 'ice', n=5, M=T(x, y + 0.05, dz))
    for sx in (1, -1):
        for k in range(3):
            b.lathe([(0.22, 0.0), (0.24, 0.5 + 0.3 * k), (0.0, 0.85 + 0.35 * k)], 'ice_dark' if k % 2 else 'ice', n=5,
                    M=T(sx * (3.45 + 0.2 * k), 0, (k - 1) * 0.5) @ R('Z', sx * -10))
    finish(b, 'IceArch')


def shard():
    b = Builder(120)
    N = 6
    outer, inner = [], []
    for k in range(N + 1):
        a = math.radians(-32 + 64 * k / N)
        outer.append((math.sin(a) * 0.1, math.cos(a) * 0.1 - 0.09))
        inner.append((math.sin(a) * 0.088, math.cos(a) * 0.088 - 0.09))
    poly = outer + list(reversed(inner))
    tops = [0.045, 0.05, 0.03, 0.048, 0.036, 0.052, 0.04]
    r0 = [(x, -0.045 + (0.008 if i % 3 == 0 else 0), z) for i, (x, z) in enumerate(poly)]
    r1 = []
    for i, (x, z) in enumerate(poly):
        k = i if i <= N else 2 * N + 1 - i
        r1.append((x, tops[k], z))

    def m(j, i):
        return 'clay' if i < N else ('clay_dark' if N < i < 2 * N + 1 else 'clay')
    b.loft([r0, r1], m, cap0='clay', cap1='clay')
    finish(b, 'Shard', clamp=False)


def bucket():
    b = Builder(121)
    b.lathe([(0.0, 0.0), (0.095, 0.0), (0.125, 0.2), (0.135, 0.205), (0.118, 0.205), (0.09, 0.03), (0.0, 0.03)],
            ['metal', 'metal', 'metal', 'metal', 'metal', 'metal'], n=10)
    b.lathe([(0.1, 0.06), (0.104, 0.08)], 'metal', n=10)
    b.lathe([(0.112, 0.15), (0.116, 0.17)], 'metal', n=10)
    b.lathe([(0.112, 0.16), (0.0, 0.17)], 'rock', n=10)
    pts = [(math.cos(a) * 0.128, 0.19 + math.sin(a) * 0.09, 0) for a in [k * math.pi / 8 for k in range(9)]]
    b.tube(pts, 0.007, 'metal', n=4)
    b.cyl((-0.04, 0.28, 0), (0.04, 0.28, 0), 0.018, 'wood', n=6)
    finish(b, 'Bucket')


def sign():
    b = Builder(122)
    b.cyl((0, 0, 0), (0, 0.9, 0), 0.035, 'wood_dark', n=6)
    b.box((0, 0.68, 0.045), (0.56, 0.26, 0.04), 'wood', bevel=0.015)
    b.prism([(0.28, 0.55), (0.38, 0.68), (0.28, 0.81)], 0.025, 0.065, 'wood')
    for y in (0.64, 0.72):
        b.box((0, y, 0.067), (0.5, 0.008, 0.006), 'wood_dark')
    for x in (-0.23, 0.23):
        for y in (0.6, 0.76):
            b.ico((x, y, 0.068), 0.012, 'wood_dark', sub=1)
    b.lathe([(0.04, 0.9), (0.0, 0.94)], 'wood_dark', n=6)
    finish(b, 'Sign')


# =============================== artifacts ===============================
def art_00():
    b = Builder(200)
    prof = [(0.0, 0.0), (0.055, 0.0), (0.075, 0.02), (0.11, 0.1), (0.108, 0.16), (0.075, 0.22), (0.05, 0.25),
            (0.058, 0.28), (0.068, 0.30), (0.052, 0.30), (0.05, 0.285)]
    bands = ['clay', 'clay', 'clay_dark', 'clay', 'clay', 'clay', 'clay_dark', 'clay_dark', 'clay_dark', 'clay_dark']
    b.lathe(prof, bands, n=10, cap1='clay_dark')
    b.lathe([(0.112, 0.125), (0.111, 0.14)], 'clay_dark', n=10)
    finish(b, 'Art_00')


def art_01():
    b = Builder(201)
    b.box((0, 0.015, 0), (0.15, 0.03, 0.09), 'gold', bevel=0.01)
    # headdress behind the face
    b.box((0, 0.2, -0.03), (0.27, 0.2, 0.04), 'gold', bevel=0.02)
    for sx in (1, -1):
        b.box((sx * 0.115, 0.115, -0.02), (0.06, 0.16, 0.05), 'gold')
        for k in range(3):
            b.box((sx * 0.115, 0.07 + k * 0.045, 0.006), (0.062, 0.012, 0.004), 'gem_blue')
    b.sphere((0, 0.19, 0.0), (0.1, 0.13, 0.06), 'gold', u=10, v=7)
    b.box((0, 0.3, 0.0), (0.22, 0.04, 0.06), 'gold', bevel=0.012)
    b.ico((0, 0.3, 0.035), (0.022, 0.022, 0.012), 'gem', sub=1)
    for sx in (1, -1):
        b.ico((sx * 0.042, 0.205, 0.052), (0.028, 0.012, 0.01), 'black', sub=1)
    b.lathe([(0.012, 0.0), (0.0, 0.035)], 'gold', n=4, M=T(0, 0.19, 0.055) @ R('X', 180) @ T(0, -0.0, 0))
    b.box((0, 0.13, 0.05), (0.04, 0.008, 0.008), 'black')
    finish(b, 'Art_01')


def art_02():
    b = Builder(202)
    pts, radii = [], []
    a0, k = 0.0068, 0.19
    turns = 2.5
    steps = 24
    for s in range(steps + 1):
        th = turns * math.tau * s / steps
        r = a0 * math.exp(k * th)
        pts.append((math.cos(th) * r, math.sin(th) * r, 0))
        radii.append(r * 0.45 + 0.002)
    miny = min(p[1] - rr for p, rr in zip(pts, radii))
    pts = [(x, y - miny + 0.03, z) for x, y, z in pts]
    alt = lambda j, i: 'fossil' if j % 3 else 'rock'
    b.tube(pts, radii, alt, n=6, ref=(0, 0, 1), cap0=False)
    b.box((0, 0.018, 0), (0.2, 0.036, 0.09), 'rock', bevel=0.012)
    finish(b, 'Art_02')


def art_03():
    b = Builder(203)
    b.lathe([(0.0, 0.0), (0.075, 0.0), (0.075, 0.02), (0.04, 0.035), (0.0, 0.04)], 'bronze', n=8)
    b.cyl((0, 0.03, 0), (0, 0.12, 0), 0.016, 'bronze', n=6)
    b.ico((0, 0.09, 0), (0.026, 0.02, 0.026), 'bronze', sub=2)
    M = T(0, 0.215, 0) @ R('X', 90)
    b.lathe([(0.1, -0.016), (0.1, 0.016)], 'bronze', n=12, M=M)
    b.lathe([(0.08, 0.016), (0.08, 0.022)], 'metal', n=12, M=M)
    b.ico((0, 0.32, 0), 0.02, 'bronze', sub=2)
    finish(b, 'Art_03')


def art_04():
    b = Builder(204)

    def stack(x, z, n, seed):
        for k in range(n):
            dx = b.rng.uniform(-0.006, 0.006)
            dz = b.rng.uniform(-0.006, 0.006)
            b.cyl((x + dx, k * 0.028, z + dz), (x + dx, k * 0.028 + 0.026, z + dz), 0.055, 'gold', n=8, phase=k * 0.3)
    stack(-0.05, -0.01, 9, 1)
    stack(0.075, 0.02, 5, 2)
    b.lathe([(0.055, 0.0), (0.055, 0.026)], 'gold', n=8, M=T(0.05, 0.055, 0.085) @ R('X', 70))
    finish(b, 'Art_04')


def art_05():
    b = Builder(205)
    pts = [(math.cos(a) * 0.088, 0.112 + math.sin(a) * 0.088, 0) for a in [k * math.tau / 16 for k in range(16)]]
    b.tube(pts, 0.024, 'gold', n=6, closed=True, ref=(0, 0, 1))
    b.lathe([(0.02, 0.185), (0.045, 0.22), (0.05, 0.228)], 'gold', n=6)
    for k in range(4):
        a = k * math.tau / 4 + math.pi / 4
        b.cyl((math.cos(a) * 0.035, 0.215, math.sin(a) * 0.035), (math.cos(a) * 0.045, 0.255, math.sin(a) * 0.045), 0.008, 'gold', n=4)
    b.lathe([(0.0, 0.205), (0.052, 0.245), (0.042, 0.272), (0.0, 0.28)], 'gem', n=8)
    finish(b, 'Art_05')


def art_06():
    b = Builder(206)
    b.lathe([(0.0, 0.0), (0.07, 0.0), (0.06, 0.03), (0.022, 0.05), (0.0, 0.055)], 'gold', n=8)
    b.cyl((0, 0.04, -0.02), (0, 0.1, -0.02), 0.014, 'gold', n=6)
    for sx in (1, -1):
        wing = [(sx * 0.035, 0.14), (sx * 0.17, 0.26), (sx * 0.215, 0.215), (sx * 0.19, 0.15), (sx * 0.13, 0.11), (sx * 0.05, 0.1)]
        if sx < 0:
            wing = list(reversed(wing))
        b.prism(wing, -0.03, -0.01, 'gold')
        for k in range(3):
            b.beam((sx * 0.06, 0.13 + k * 0.012, -0.004), (sx * (0.15 + k * 0.02), 0.18 + k * 0.03, -0.004), 0.012, 0.008, 'gem_blue',
                   side=(0, 0, 1))
    b.sphere((0, 0.165, 0.0), (0.06, 0.075, 0.035), 'gem_blue', u=8, v=6)
    b.box((0, 0.165, 0.032), (0.006, 0.12, 0.012), 'gold')
    b.sphere((0, 0.245, 0.0), (0.035, 0.025, 0.025), 'gold', u=6, v=4)
    pts = [(math.cos(a) * 0.035, 0.31 + math.sin(a) * 0.035, -0.01) for a in [k * math.tau / 10 for k in range(10)]]
    b.tube(pts, 0.008, 'gold', n=4, closed=True, ref=(0, 0, 1))
    finish(b, 'Art_06')


def art_07():
    b = Builder(207)
    b.cyl((0, 0.03, 0), (0, 0.29, 0), 0.026, 'bone', n=8)
    b.sphere((0, 0.035, 0), (0.04, 0.035, 0.04), 'bone', u=8, v=4)
    b.sphere((0, 0.295, 0), (0.036, 0.032, 0.036), 'bone', u=8, v=4)
    for y in (0.1, 0.14, 0.18, 0.22):
        b.ico((0, y, 0.024), (0.01, 0.01, 0.006), 'black', sub=1)
    b.ico((0, 0.31, 0.022), (0.012, 0.008, 0.008), 'black', sub=1)
    finish(b, 'Art_07')


def art_08():
    b = Builder(208)
    b.box((0, 0.03, 0), (0.13, 0.06, 0.09), 'wood_dark', bevel=0.012)
    b.lathe([(0.0, 0.05), (0.03, 0.085), (0.034, 0.2)], 'bronze', n=4, aspect=(1, 0.25), cap1='bronze')
    b.box((0, 0.205, 0), (0.13, 0.024, 0.036), 'bronze', bevel=0.008)
    b.cyl((0, 0.215, 0), (0, 0.29, 0), 0.018, 'wood_dark', n=6)
    b.cyl((0, 0.25, 0), (0, 0.258, 0), 0.021, 'bronze', n=6)
    b.sphere((0, 0.3, 0), 0.026, 'bronze', u=6, v=4)
    finish(b, 'Art_08')


def art_09():
    b = Builder(209)
    b.sphere((0, 0.06, 0), (0.088, 0.085, 0.088), 'gem', u=10, v=6)
    b.clamp_ground(0.02)
    b.lathe([(0.1, 0.0), (0.112, 0.0), (0.112, 0.11), (0.1, 0.11)], 'gold', n=12)
    n = 6
    for k in range(n):
        a = k * math.tau / n + math.pi / 2
        x, z = math.cos(a) * 0.104, math.sin(a) * 0.104
        b.lathe([(0.035, 0.0), (0.0, 0.12)], 'gold', n=4, M=T(x, 0.1, z) @ R('Y', -math.degrees(a)), aspect=(0.45, 1))
        b.ico((x * 1.0, 0.225, z * 1.0), 0.018, 'gem' if k % 2 else 'gem_blue', sub=1)
        b.ico((math.cos(a + math.pi / n) * 0.114, 0.055, math.sin(a + math.pi / n) * 0.114), 0.016,
              'gem_blue' if k % 2 else 'gem', sub=1)
    finish(b, 'Art_09')


def art_10():
    b = Builder(210)
    b.lathe([(0.0, 0.0), (0.075, 0.0), (0.07, 0.03), (0.052, 0.14), (0.038, 0.175), (0.0, 0.185)], 'clay', n=8)
    b.lathe([(0.064, 0.07), (0.061, 0.085)], 'clay_dark', n=8)
    b.sphere((0, 0.225, 0), (0.055, 0.055, 0.05), 'clay', u=8, v=6)
    b.sphere((0, 0.245, -0.008), (0.06, 0.042, 0.055), 'clay_dark', u=8, v=4)
    for sx in (1, -1):
        b.ico((sx * 0.02, 0.225, 0.046), 0.008, 'clay_dark', sub=1)
        b.tube([(sx * 0.045, 0.16, 0), (sx * 0.075, 0.13, 0.02), (sx * 0.035, 0.11, 0.05)], 0.016, 'clay', n=5)
    for k in range(5):
        a = math.pi / 2 + (k - 2) * 0.35
        b.ico((math.cos(a) * 0.05, 0.155 - abs(k - 2) * 0.006, math.sin(a) * 0.05), 0.008, 'clay_dark', sub=1)
    finish(b, 'Art_10')


def art_11():
    b = Builder(211)
    b.lathe([(0.0, 0.0), (0.07, 0.0), (0.065, 0.02), (0.02, 0.03), (0.0, 0.032)], 'gold', n=8)
    b.cyl((0, 0.02, -0.03), (0, 0.33, -0.03), 0.01, 'gold', n=6)
    b.cyl((-0.1, 0.32, -0.03), (0.1, 0.32, -0.03), 0.01, 'gold', n=6)
    for sx in (1, -1):
        b.ico((sx * 0.1, 0.32, -0.03), 0.016, 'gold', sub=1)
    mats = ['gem', 'gold', 'gem_blue', 'gold', 'jade', 'gold']
    nb = 14
    for k in range(nb):
        t = -math.pi + (k + 0.5) * math.tau / nb
        x = 0.085 * math.sin(t)
        y = 0.205 + 0.105 * math.cos(t)
        rr = 0.016 if mats[k % 6] != 'gold' else 0.011
        b.ico((x, y, 0.0), rr, mats[k % 6], sub=1)
    b.lathe([(0.0, 0.035), (0.03, 0.06), (0.022, 0.09), (0.0, 0.1)], 'jade', n=6, M=T(0, 0.0, 0.0))
    b.cyl((0, 0.098, 0), (0, 0.105, 0), 0.01, 'gold', n=6)
    finish(b, 'Art_11')


def art_12():
    b = Builder(212)
    b.lathe([(0.0, 0.0), (0.05, 0.0), (0.06, 0.015), (0.04, 0.03)], 'clay_dark', n=8)
    b.lathe([(0.0, 0.025), (0.07, 0.028), (0.1, 0.06), (0.085, 0.1), (0.035, 0.12), (0.0, 0.12)], 'clay', n=10,
            aspect=(1.0, 0.85))
    b.ico((0, 0.12, 0.0), (0.03, 0.006, 0.03), 'clay_dark', sub=1)
    b.tube([(0, 0.07, 0.06), (0, 0.085, 0.12), (0, 0.095, 0.16)], [0.04, 0.03, 0.025], 'clay', n=6)
    b.tube([(0, 0.07, -0.07), (0, 0.11, -0.12), (0, 0.06, -0.12)], 0.013, 'clay', n=5)
    base = 0.1
    b.lathe([(0.0, base), (0.03, base + 0.035), (0.026, base + 0.09), (0.0, base + 0.15)], 'flame', n=5,
            M=T(0, 0, 0.165))
    finish(b, 'Art_12')


def art_13():
    b = Builder(213)
    b.ico((0, 0.035, 0), (0.12, 0.06, 0.09), 'rock', sub=2, jitter=0.12)
    b.clamp_ground()
    b.lathe([(0.05, 0.03), (0.06, 0.07)], 'bone', n=7)
    pts = [(0, 0.07, 0), (0, 0.14, 0.008), (0, 0.21, 0.028), (0, 0.27, 0.058), (0, 0.31, 0.092)]
    b.tube(pts, [0.058, 0.05, 0.037, 0.021, 0.0], 'fossil', n=7, aspect=(0.75, 1.0))
    finish(b, 'Art_13')


def art_14():
    b = Builder(214)
    b.box((0, 0.006, 0), (0.15, 0.012, 0.15), 'gold', bevel=0.004)
    b.box((0, 0.075, 0), (0.14, 0.13, 0.14), 'jade', bevel=0.016)
    b.sphere((0, 0.165, -0.01), (0.055, 0.042, 0.065), 'jade', u=8, v=5)
    b.sphere((0, 0.19, 0.05), (0.034, 0.03, 0.032), 'jade', u=8, v=5)
    for sx in (1, -1):
        b.ico((sx * 0.02, 0.215, 0.04), 0.011, 'jade', sub=1)
        b.ico((sx * 0.014, 0.196, 0.078), 0.006, 'gold', sub=1)
        b.ico((sx * 0.05, 0.145, 0.04), (0.018, 0.02, 0.018), 'jade', sub=1)
    b.lathe([(0.012, 0.0), (0.0, 0.05)], 'jade', n=4, M=T(0, 0.165, -0.07) @ R('X', -110))
    pts = [(0.07, 0.11, 0.0), (0.09, 0.08, 0.0), (0.095, 0.04, 0.0)]
    b.tube(pts, 0.006, 'gold', n=4)
    b.lathe([(0.0, 0.0), (0.016, -0.01), (0.012, -0.045), (0.0, -0.05)], 'gold', n=6, M=T(0.095, 0.045, 0))
    finish(b, 'Art_14')


for fn in (tablet, pot, bones, scroll, lizard, tent, crate, cart, torch, palm, jungle_tree, ice_crystal, cactus,
           rope_post, pyramid, temple, ice_arch, shard, bucket, sign,
           art_00, art_01, art_02, art_03, art_04, art_05, art_06, art_07, art_08, art_09, art_10, art_11, art_12, art_13, art_14):
    fn()
rock('Rock', (0.62, 0.5, 0.55), 301)
rock('RockFlat', (0.9, 0.25, 0.6), 302)

out = OUT / 'kit.glb'
export_glb(out)
print('KIT_TRIS', json.dumps(TRIS), 'TOTAL', sum(TRIS.values()))
print('WROTE', out, out.stat().st_size)
