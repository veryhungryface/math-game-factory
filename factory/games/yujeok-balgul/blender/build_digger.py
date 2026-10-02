"""Builds digger.glb — original chibi desert explorer for 유적 발굴단.

Run: Blender -b --factory-startup --python factory/games/yujeok-balgul/blender/build_digger.py
Authoring is in three.js space (y up, character faces +Z). Character's own LEFT = +X.
Each part's origin sits at its joint so the game can rotate it.
"""
import sys, json
from pathlib import Path
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))
from common import *  # noqa

OUT = HERE.parents[3] / 'public' / 'g' / 'yujeok-balgul' / 'assets' / 'models'
OUT.mkdir(parents=True, exist_ok=True)
reset_scene()

HIP = (0, 0.36, 0)
NECK = (0, 0.585, 0)
SH_L = (0.150, 0.552, 0)
SH_R = (-0.150, 0.552, 0)
HAND_L = (0.205, 0.335, 0.0)
HAND_R = (-0.205, 0.335, 0.0)
LEG_L = (0.075, 0.30, 0)
LEG_R = (-0.075, 0.30, 0)
TRIS = {}

root = empty('Digger')

# ---------------- Torso ----------------
b = Builder(3)
b.box((0, 0.335, 0), (0.25, 0.13, 0.17), 'shorts', bevel=0.04, segs=2)
b.box((0, 0.402, 0), (0.256, 0.032, 0.176), 'boot', bevel=0.01)
b.box((0.0, 0.402, 0.089), (0.035, 0.026, 0.008), 'gold')                 # belt buckle
b.box((0, 0.49, 0), (0.25, 0.19, 0.165), 'shirt', bevel=0.055, segs=2)
# collar / scarf ring + knot + tails
b.lathe([(0.088, 0.565), (0.098, 0.585), (0.085, 0.605)], 'scarf', n=10)
b.ico((0.0, 0.56, 0.088), (0.034, 0.03, 0.022), 'scarf', sub=1)
b.beam((0.005, 0.55, 0.095), (0.035, 0.485, 0.105), 0.035, 0.012, 'scarf', side=(1, 0, 0))
b.beam((-0.005, 0.55, 0.095), (-0.022, 0.495, 0.103), 0.03, 0.012, 'scarf', side=(1, 0, 0))
# satchel strap: right shoulder -> across chest -> left hip (front), and back
b.beam((-0.095, 0.585, 0.083), (0.118, 0.385, 0.087), 0.034, 0.014, 'satchel', side=(0, 0, 1))
b.beam((-0.095, 0.585, -0.083), (0.118, 0.385, -0.087), 0.034, 0.014, 'satchel', side=(0, 0, 1))
b.box((-0.095, 0.592, 0), (0.036, 0.014, 0.17), 'satchel')
# hip bag on the character's left (+X), slightly forward
b.box((0.145, 0.335, 0.025), (0.06, 0.11, 0.13), 'satchel', bevel=0.02, segs=1)
b.box((0.148, 0.372, 0.025), (0.068, 0.045, 0.138), 'boot', bevel=0.012)
b.box((0.183, 0.355, 0.025), (0.008, 0.022, 0.024), 'gold')
# backpack + bedroll
b.box((0, 0.475, -0.108), (0.185, 0.15, 0.07), 'satchel', bevel=0.025, segs=1)
b.box((0, 0.455, -0.146), (0.12, 0.06, 0.012), 'boot', bevel=0.004)
b.cyl((-0.115, 0.575, -0.112), (0.115, 0.575, -0.112), 0.048, 'cloth', n=8)
for x in (-0.06, 0.06):
    b.cyl((x - 0.012, 0.575, -0.112), (x + 0.012, 0.575, -0.112), 0.052, 'boot', n=8)
TRIS['Torso'] = b.tri_count()
torso = b.to_object('Torso', HIP, parent=root)

# ---------------- Head ----------------
b = Builder(5)
b.cyl((0, 0.565, 0), (0, 0.63, 0), 0.045, 'skin', n=8)
b.sphere((0, 0.77, 0.005), (0.19, 0.175, 0.18), 'skin', u=14, v=9)
for sx in (1, -1):
    b.ico((sx * 0.183, 0.755, -0.005), (0.032, 0.04, 0.03), 'skin', sub=1)
# hair: back cap + bangs
b.sphere((0, 0.795, -0.03), (0.198, 0.168, 0.178), 'hair', u=12, v=8)
for x, y, z, r in ((-0.085, 0.855, 0.135, 0.055), (0.0, 0.87, 0.15, 0.058), (0.085, 0.855, 0.135, 0.055),
                   (-0.15, 0.80, 0.08, 0.05), (0.15, 0.80, 0.08, 0.05)):
    b.ico((x, y, z), (r, r * 0.8, r * 0.8), 'hair', sub=1)
# face
for sx in (1, -1):
    b.ico((sx * 0.062, 0.762, 0.17), (0.026, 0.034, 0.016), 'black', sub=2)
    b.ico((sx * 0.062 + sx * -0.008, 0.775, 0.184), 0.008, 'white', sub=1)
    b.ico((sx * 0.112, 0.72, 0.142), (0.03, 0.016, 0.01), 'scarf', sub=1,
          M=T(sx * 0.112, 0.72, 0.142) @ R('Y', sx * 38) @ T(-sx * 0.112, -0.72, -0.142))
b.ico((0, 0.735, 0.182), (0.02, 0.016, 0.014), 'skin', sub=1)
b.box((0, 0.695, 0.166), (0.034, 0.008, 0.012), 'black', M=T(0, 0.695, 0.166) @ R('X', -12) @ T(0, -0.695, -0.166))
# pith helmet (tilted a touch back)
HM = T(0, 0.89, 0) @ R('X', -7) @ T(0, -0.89, 0)
b.lathe([(0.0, 0.873), (0.29, 0.858), (0.292, 0.872), (0.19, 0.893), (0.0, 0.90)], 'hat', n=14, M=HM, aspect=(1, 1.1))
b.lathe([(0.193, 0.885), (0.197, 0.928)], 'hat_band', n=14, M=HM)
b.lathe([(0.19, 0.885), (0.195, 0.93), (0.177, 0.982), (0.128, 1.017), (0.052, 1.031), (0.0, 1.033)], 'hat', n=14, M=HM)
b.ico((0, 1.035, 0), (0.024, 0.016, 0.024), 'hat_band', sub=1, M=HM)
TRIS['Head'] = b.tri_count()
head = b.to_object('Head', NECK, parent=torso, parent_pivot=HIP)

# ---------------- Arms ----------------
def arm(name, sx):
    b = Builder(7)
    sh = (sx * 0.16, 0.535, 0)
    b.ico(sh, (0.062, 0.062, 0.062), 'shirt', sub=2)
    b.cyl((sx * 0.165, 0.545, 0), (sx * 0.182, 0.468, 0), 0.054, 'shirt', n=8, r2=0.052)
    b.cyl((sx * 0.18, 0.48, 0), (sx * 0.2, 0.365, 0), 0.036, 'skin', n=7, r2=0.034)
    b.ico((sx * 0.205, 0.335, 0.0), (0.046, 0.048, 0.046), 'skin', sub=2)
    TRIS[name] = b.tri_count()
    return b.to_object(name, SH_L if sx > 0 else SH_R, parent=torso, parent_pivot=HIP)

arm_l = arm('ArmL', 1)
arm_r = arm('ArmR', -1)

# tools held in right hand: forward (+Z) and slightly down
d = Vector((0, -0.45, 1.0)).normalized()
g = v3(HAND_R)
side_x = Vector((1, 0, 0))

b = Builder(9)
b.cyl(g - d * 0.045, g + d * 0.13, 0.016, 'wood', n=6)
b.cyl(g + d * 0.13, g + d * 0.16, 0.021, 'metal', n=6)
up = side_x.cross(d)  # thickness axis
Mb = frame(side_x, d, up, (0, 0, 0))
b.obox((0, 0, 0), (0.12, 0.035, 0.045), 'wood_dark', T(*(g + d * 0.175)) @ Mb, bevel=0.008)
b.obox((0, 0, 0), (0.115, 0.075, 0.032), 'rope', T(*(g + d * 0.228)) @ Mb, bevel=0.006)
TRIS['Brush'] = b.tri_count()
brush = b.to_object('Brush', HAND_R, parent=arm_r, parent_pivot=SH_R)

b = Builder(11)
b.cyl(g - d * 0.045, g + d * 0.215, 0.016, 'wood', n=6)
e = d.cross(side_x).normalized()      # head axis, perpendicular to handle in the YZ plane
hc = g + d * 0.215
Mh = frame(side_x, e, d, (0, 0, 0))
b.obox((0, 0.0, 0), (0.04, 0.08, 0.042), 'metal', T(*hc) @ Mh, bevel=0.006)
b.cyl(hc + e * 0.04, hc + e * 0.105, 0.019, 'metal', n=4, r2=0.0, phase=math.pi / 4)
b.cyl(hc - e * 0.04, hc - e * 0.058, 0.026, 'metal', n=6)
b.cyl(g + d * 0.15, g + d * 0.19, 0.019, 'boot', n=6)
TRIS['Chisel'] = b.tri_count()
chisel = b.to_object('Chisel', HAND_R, parent=arm_r, parent_pivot=SH_R)

# ---------------- Legs ----------------
def leg(name, sx):
    b = Builder(13)
    x = sx * 0.075
    b.cyl((x, 0.31, 0), (x, 0.215, 0), 0.064, 'shorts', n=8, r2=0.06)
    b.cyl((x, 0.23, 0), (x, 0.11, 0), 0.037, 'skin', n=7)
    b.cyl((x, 0.135, 0), (x, 0.095, 0), 0.047, 'cloth', n=8)
    b.box((x, 0.06, 0.018), (0.1, 0.1, 0.155), 'boot', bevel=0.035, segs=2)
    b.box((x, 0.012, 0.02), (0.108, 0.024, 0.165), 'wood_dark', bevel=0.008)
    TRIS[name] = b.tri_count()
    return b.to_object(name, LEG_L if sx > 0 else LEG_R, parent=root)

leg('LegL', 1)
leg('LegR', -1)

out = OUT / 'digger.glb'
export_glb(out)
print('DIGGER_TRIS', json.dumps(TRIS), 'TOTAL', sum(TRIS.values()))
print('WROTE', out, out.stat().st_size)
