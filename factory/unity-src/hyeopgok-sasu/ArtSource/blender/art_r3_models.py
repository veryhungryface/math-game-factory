"""Art round 3 deterministic model pass for Hyeopgok Sasu.

Owns only the FBX names listed in ``OWNED_ASSETS`` and
``art-r3-models-report.json``.  The existing generators remain untouched.

Rebuild (repo root):
  /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup \
    --python factory/unity-src/hyeopgok-sasu/ArtSource/blender/art_r3_models.py

All source coordinates use Unity axes (x right, y up, z forward).  FBX export
uses -Z forward/+Y up, one flat-shaded vertex-colour mesh and one material.
COLOR.a contains geometric AO, UV0=(team mask, baked marker), and troop UV1
contains (rigid-part id, gait phase) for allocation-free vertex animation.
"""
import ast
import bpy
import bmesh
import hashlib
import json
import math
import random
import sys
from pathlib import Path

from mathutils import Matrix, Vector

HERE = Path(__file__).resolve().parent
SOURCE = HERE / 'build_models.py'
PHASE1 = HERE / 'phase1_models.py'
R2_ENV = HERE / 'art_r2_environment.py'
OUT = HERE.parent.parent / 'Resources' / 'HyeopgokSasu' / 'Models'
REPORT = HERE / 'art-r3-models-report.json'
OUT.mkdir(parents=True, exist_ok=True)


def load_build_constructors():
    """Load build_models definitions without its destructive export entrypoint."""
    module = ast.parse(SOURCE.read_text())
    cut = next(i for i, node in enumerate(module.body)
               if isinstance(node, ast.For))
    ns = {'__file__': str(SOURCE), '__name__': 'art_r3_build_constructors'}
    exec(compile(ast.Module(body=module.body[:cut], type_ignores=[]),
                 str(SOURCE), 'exec'), ns)
    return ns


BASE = load_build_constructors()
MeshMaker = BASE['MeshMaker']
PAL = BASE['PAL']
bcoord = BASE['bcoord']
tint = BASE['tint']
PATH_POINTS = BASE['PATH_POINTS']
PLATEAU = BASE['PLATEAU']

# R3 palette keeps the warm/cool three-value material hierarchy explicit.
PAL.update({
    'sandstone': (.914, .788, .690, 1),
    'sandstone_side': (.514, .533, .580, 1),
    'slate_deep': (.235, .285, .345, 1),
    'slate_mid': (.430, .475, .535, 1),
    'door_recess': (.055, .075, .078, 1),
    'ember': (1.0, .42, .055, 1),
})

OWNED_ASSETS = [
    'troop_sword', 'troop_spear', 'troop_shield', 'king',
    'tower_base_l1', 'tower_base_l2', 'tower_base_l3', 'tower_base',
    'barracks_l1', 'barracks_l2', 'barracks_l3', 'barracks',
    'castle_gate_l1', 'castle_gate_l2', 'castle_gate_l3', 'castle_gate',
    'terrain',
]

# UV1.x group codes are separated enough for branchless threshold masks.
MOTION_ENCODING = {
    'torso_head': [0.00, 0.00],
    'left_leg': [0.25, 0.00],
    'right_leg': [0.50, 0.50],
    'weapon_arm': [0.75, 0.25],
    'shield_offhand_cape': [1.00, 0.75],
}


class MotionMeshMaker(MeshMaker):
    """MeshMaker with a per-source-vertex rigid-part/phase channel."""
    def __init__(self, name):
        super().__init__(name)
        self.motion = []
        self.motion_value = tuple(MOTION_ENCODING['torso_head'])

    def set_motion(self, group):
        self.motion_value = tuple(MOTION_ENCODING[group])

    def face(self, pts, color):
        before = len(self.verts)
        super().face(pts, color)
        self.motion.extend([self.motion_value] * (len(self.verts) - before))


def with_motion(m, group, operation, *args, **kwargs):
    old = m.motion_value
    m.set_motion(group)
    operation(*args, **kwargs)
    m.motion_value = old


ASSETS = []
OBJECTS = {}


def remove_owned_scene_objects():
    """MCP-safe: remove only this pass' objects, never the shared scene."""
    for ob in list(bpy.data.objects):
        if ob.name in OWNED_ASSETS or any(ob.name.startswith(n + '.') for n in OWNED_ASSETS):
            bpy.data.objects.remove(ob, do_unlink=True)


def emit_r3(m, category, notes=None):
    """Triangulate, strengthen geometric AO, write UV1, and export one FBX."""
    ob = m.object()
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    tri = ob.modifiers.new('FlatTriangles', 'TRIANGULATE')
    bpy.ops.object.modifier_apply(modifier=tri.name)

    # Preserve the existing baker and contract while using the stronger R3 value.
    BASE['AO_STRENGTH'] = .68
    ao_stats = BASE['bake_vertex_ao'](ob)
    if isinstance(m, MotionMeshMaker):
        uv1 = ob.data.uv_layers.new(name='MotionMask_Phase')
        for poly in ob.data.polygons:
            for loop_index in poly.loop_indices:
                vertex = ob.data.loops[loop_index].vertex_index
                uv1.data[loop_index].uv = m.motion[vertex]

    file = OUT / (m.name + '.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(file), use_selection=True, object_types={'MESH'},
        global_scale=1.0, apply_unit_scale=True,
        apply_scale_options='FBX_SCALE_UNITS', axis_forward='-Z', axis_up='Y',
        bake_space_transform=True, use_mesh_modifiers=True,
        mesh_smooth_type='OFF', use_tspace=False, add_leaf_bones=False,
        bake_anim=False, path_mode='AUTO', colors_type='SRGB',
        use_custom_props=False)

    points = [(v.co.x, v.co.z, -v.co.y) for v in ob.data.vertices]
    info = {
        'id': m.name,
        'resource': 'HyeopgokSasu/Models/' + m.name,
        'category': category,
        'fbx_bytes': file.stat().st_size,
        'sha256': hashlib.sha256(file.read_bytes()).hexdigest(),
        'triangles': len(ob.data.polygons),
        'vertices': len(ob.data.vertices),
        'uv_layers': [layer.name for layer in ob.data.uv_layers],
        'ao_bake': ao_stats,
        'bounds_min': [round(min(p[i] for p in points), 4) for i in range(3)],
        'bounds_max': [round(max(p[i] for p in points), 4) for i in range(3)],
    }
    if notes:
        info['notes'] = notes
    ASSETS.append(info)
    OBJECTS[m.name] = ob
    ob['art_round'] = 3
    ob['source_script'] = 'art_r3_models.py'
    print('ART_R3_MODEL', json.dumps(info, ensure_ascii=False))
    return ob


def face_thickness(m, polygon, depth, front_color, rim_color, motion_group):
    """Extrude a flat front-facing x/y polygon along +z."""
    m.set_motion(motion_group)
    front = [(x, y, z) for x, y, z in polygon]
    back = [(x, y, z - depth) for x, y, z in polygon]
    m.face(front, front_color)
    m.face(list(reversed(back)), rim_color)
    for i in range(len(front)):
        j = (i + 1) % len(front)
        m.face([front[i], front[j], back[j], back[i]], rim_color)


def add_common_troop(m, helmet='round'):
    # Feet and legs keep wide negative space between them at mobile scale.
    with_motion(m, 'left_leg', m.capsule_box, (-.073, .052, .020),
                (.105, .104, .165), 'leather')
    with_motion(m, 'left_leg', m.capsule_box, (-.073, .165, 0),
                (.088, .165, .105), 'team_dark')
    with_motion(m, 'right_leg', m.capsule_box, (.073, .052, .020),
                (.105, .104, .165), 'leather')
    with_motion(m, 'right_leg', m.capsule_box, (.073, .165, 0),
                (.088, .165, .105), 'team_dark')
    with_motion(m, 'torso_head', m.cylinder, (0, .345, 0), .162, .285,
                'team', 6, .137)
    with_motion(m, 'torso_head', m.box, (0, .300, .137),
                (.293, .050, .025), 'leather')
    with_motion(m, 'torso_head', m.cylinder, (0, .548, .008), .112, .145,
                'skin', 7)
    if helmet == 'round':
        with_motion(m, 'torso_head', m.rings, (0, 0, -.012),
                    [(.515, .185), (.605, .194), (.690, .138), (.725, .045)],
                    'team', 8, (1, .92), 0)
        with_motion(m, 'torso_head', m.box, (0, .566, .115),
                    (.225, .035, .029), 'team_dark')
    elif helmet == 'spear':
        with_motion(m, 'torso_head', m.rings, (0, 0, -.010),
                    [(.515, .172), (.620, .180), (.705, .105), (.755, .018)],
                    'team', 7, (1, .88), .16)
        with_motion(m, 'torso_head', m.wedge, (0, .805, -.015),
                    (.075, .17, .19), 'gold')
    else:
        with_motion(m, 'torso_head', m.rings, (0, 0, -.010),
                    [(.515, .194), (.625, .198), (.690, .155)],
                    'team_dark', 6, (1.06, .92), math.pi / 6)
        with_motion(m, 'torso_head', m.box, (0, .640, .105),
                    (.280, .050, .038), 'team')
    # Two quiet eye slots survive the front three-quarter game camera.
    for x in (-.052, .052):
        m.set_motion('torso_head')
        m.face([(x - .016, .525, .109), (x + .016, .525, .109),
                (x + .014, .548, .109), (x - .014, .548, .109)], 'black')


def sword_blade(m, a, b, width=.042):
    a, b = Vector(a), Vector(b)
    d = (b - a).normalized()
    side = Vector((-d.y, d.x, 0)) * width
    thick = Vector((0, 0, .018))
    shoulder = b - d * .11
    pts = [a - side, a + side, shoulder + side, b, shoulder - side]
    m.set_motion('weapon_arm')
    m.face([tuple(p + thick) for p in pts], 'steel_light')
    m.face([tuple(p - thick) for p in reversed(pts)], 'steel')
    for i in range(len(pts)):
        j = (i + 1) % len(pts)
        m.face([tuple(pts[i] + thick), tuple(pts[j] + thick),
                tuple(pts[j] - thick), tuple(pts[i] - thick)], 'steel_dark')


def build_troop_sword():
    m = MotionMeshMaker('troop_sword')
    add_common_troop(m, 'round')
    with_motion(m, 'shield_offhand_cape', m.capsule_beam,
                (-.145, .420, .005), (-.205, .275, .115), .076, .076, 'team')
    with_motion(m, 'weapon_arm', m.capsule_beam,
                (.145, .420, 0), (.205, .300, .130), .078, .078, 'team')
    with_motion(m, 'weapon_arm', m.beam,
                (.205, .306, .130), (.266, .403, .143), .044, .036, 'wood')
    sword_blade(m, (.252, .390, .142), (.370, .805, .158), .046)
    with_motion(m, 'weapon_arm', m.box, (.244, .392, .145),
                (.205, .034, .035), 'gold')
    # Small empty offhand and broad blade make this the agile class.
    with_motion(m, 'shield_offhand_cape', m.capsule_box,
                (-.210, .260, .120), (.098, .090, .090), 'skin')
    return emit_r3(m, 'troop', 'broad raised sword; no shield; round helmet')


def spear_tip(m, base, tip, width=.072):
    base, tip = Vector(base), Vector(tip)
    d = (tip - base).normalized()
    side = Vector((-d.y, d.x, 0)) * width
    thick = Vector((0, 0, .021))
    mid = base + d * .11
    poly = [base, mid + side, tip, mid - side]
    m.set_motion('weapon_arm')
    m.face([tuple(p + thick) for p in poly], 'steel_light')
    m.face([tuple(p - thick) for p in reversed(poly)], 'steel')
    for i in range(4):
        j = (i + 1) % 4
        m.face([tuple(poly[i] + thick), tuple(poly[j] + thick),
                tuple(poly[j] - thick), tuple(poly[i] - thick)], 'steel_dark')


def build_troop_spear():
    m = MotionMeshMaker('troop_spear')
    add_common_troop(m, 'spear')
    with_motion(m, 'shield_offhand_cape', m.capsule_beam,
                (-.145, .420, 0), (-.175, .275, .095), .072, .072, 'team')
    with_motion(m, 'weapon_arm', m.capsule_beam,
                (.145, .420, 0), (.202, .300, .125), .072, .072, 'team')
    # Long diagonal shaft reads through a dense crowd without enlarging the body.
    with_motion(m, 'weapon_arm', m.beam,
                (.180, .180, .105), (.355, .930, .145), .030, .030, 'wood_gold')
    spear_tip(m, (.338, .852, .145), (.405, 1.095, .151), .066)
    with_motion(m, 'weapon_arm', m.capsule_box,
                (.203, .306, .126), (.095, .088, .088), 'skin')
    return emit_r3(m, 'troop', 'long diagonal spear; pointed helmet and plume')


def build_troop_shield():
    m = MotionMeshMaker('troop_shield')
    add_common_troop(m, 'shield')
    with_motion(m, 'shield_offhand_cape', m.capsule_beam,
                (-.145, .420, 0), (-.208, .280, .125), .080, .080, 'team')
    with_motion(m, 'weapon_arm', m.capsule_beam,
                (.145, .420, 0), (.195, .305, .105), .076, .076, 'team')
    shield = [(-.350, .505, .185), (-.075, .505, .185),
              (-.058, .245, .185), (-.212, .108, .185),
              (-.366, .245, .185)]
    face_thickness(m, shield, .048, 'team', 'steel_dark', 'shield_offhand_cape')
    with_motion(m, 'shield_offhand_cape', m.box,
                (-.212, .335, .213), (.052, .255, .025), 'gold')
    with_motion(m, 'shield_offhand_cape', m.box,
                (-.212, .335, .216), (.245, .046, .028), 'gold_shadow')
    # A short mace stays subordinate to the unmistakable kite shield.
    with_motion(m, 'weapon_arm', m.beam,
                (.190, .300, .110), (.262, .530, .125), .035, .035, 'wood')
    with_motion(m, 'weapon_arm', m.rings, (.275, .555, .125),
                [(-.045, .066), (.012, .082), (.072, .040)], 'steel', 6, (1, .75), 0)
    return emit_r3(m, 'troop', 'oversized kite shield; short mace; broad helmet')


def build_king():
    m = MotionMeshMaker('king')
    # The exported hero reaches 1.25 m; Unity's 1.25 scale makes him 2.2x a body.
    for x, group in ((-.135, 'left_leg'), (.135, 'right_leg')):
        with_motion(m, group, m.capsule_box, (x, .080, .040),
                    (.195, .160, .285), 'leather')
        with_motion(m, group, m.capsule_box, (x, .235, 0),
                    (.170, .205, .195), 'navy')
    with_motion(m, 'torso_head', m.cylinder, (0, .520, 0),
                .310, .455, 'royal', 8, .255)
    with_motion(m, 'torso_head', m.box, (0, .435, .228),
                (.455, .062, .040), 'gold')
    with_motion(m, 'torso_head', m.box, (0, .545, .246),
                (.135, .165, .035), 'gold_light')
    for side, group in ((-1, 'shield_offhand_cape'), (1, 'weapon_arm')):
        with_motion(m, group, m.capsule_box, (side * .310, .650, .010),
                    (.245, .185, .245), 'gold')
        with_motion(m, group, m.capsule_beam,
                    (side * .300, .620, .010), (side * .375, .360, .125),
                    .175, .175, 'royal')
        with_motion(m, group, m.capsule_box, (side * .378, .342, .128),
                    (.175, .165, .175), 'skin')
    with_motion(m, 'torso_head', m.cylinder, (0, .890, .015),
                .215, .300, 'skin', 10)
    with_motion(m, 'torso_head', m.rings, (0, 0, 0),
                [(.890, .226), (1.005, .220), (1.045, .180)],
                'leather', 10, (1, 1), 0)
    for x in (-.078, .078):
        m.set_motion('torso_head')
        m.face([(x - .018, .885, .216), (x + .018, .885, .216),
                (x + .016, .919, .216), (x - .016, .919, .216)], 'black')
    with_motion(m, 'torso_head', m.capsule_box, (0, .805, .195),
                (.245, .120, .110), 'leather')

    # Broad circlet and exactly five tall teeth; all five break the silhouette.
    with_motion(m, 'torso_head', m.rings, (0, 0, 0),
                [(1.020, .246), (1.105, .245)], 'gold', 10, (1, 1), 0)
    for i in range(5):
        a = i * math.tau / 5 + .30
        x, z = math.sin(a) * .225, math.cos(a) * .225
        with_motion(m, 'torso_head', m.wedge, (x, 1.205, z),
                    (.120, .245, .105), 'gold_light')
    with_motion(m, 'torso_head', m.box, (0, 1.065, .247),
                (.080, .090, .035), 'royal')

    # Wide three-panel blue mantle with navy folds and a gold hem.
    m.set_motion('shield_offhand_cape')
    left, right = (-.295, .755, -.170), (.295, .755, -.170)
    m.face([left, (-.405, .135, -.395), (0, .105, -.530), (0, .675, -.235)], 'royal')
    m.face([(0, .675, -.235), (0, .105, -.530), (.405, .135, -.395), right], 'roof')
    m.face([left, right, (0, .675, -.235)], 'royal')
    with_motion(m, 'shield_offhand_cape', m.beam,
                left, (-.405, .135, -.395), .043, .032, 'gold')
    with_motion(m, 'shield_offhand_cape', m.beam,
                right, (.405, .135, -.395), .043, .032, 'gold')
    return emit_r3(m, 'hero', 'five-tooth gold crown; broad folded blue mantle')


def load_phase1_functions():
    ns = dict(BASE)
    ns['__file__'] = str(PHASE1)
    ns['__name__'] = 'art_r3_phase1_constructors'
    # Existing stage functions return their MeshMaker instead of exporting.
    ns['emit'] = lambda m: m
    exec(compile(PHASE1.read_text(), str(PHASE1), 'exec'), ns)
    ns['emit'] = lambda m: m
    return ns


PHASE = load_phase1_functions()


def prop_crate(m, x, z, scale=.24):
    m.box((x, scale * .48, z), (scale, scale * .96, scale), 'wood')
    for dy in (-.30, .30):
        m.box((x, scale * (.48 + dy), z + scale * .51),
              (scale * 1.08, scale * .105, scale * .065), 'wood_gold')
    for dx in (-.43, .43):
        m.box((x + scale * dx, scale * .48, z + scale * .515),
              (scale * .075, scale * .92, scale * .055), 'wood_light')


def prop_barrel(m, x, z, scale=.24):
    m.rings((x, 0, z), [(0, scale * .31), (scale * .13, scale * .48),
                         (scale * .45, scale * .50), (scale * .83, scale * .43),
                         (scale, scale * .29)], 'wood', n=8)
    for y in (scale * .18, scale * .78):
        m.rings((x, 0, z), [(y, scale * .50), (y + scale * .045, scale * .50)],
                'steel_dark', n=8)


def prop_torch(m, x, z, height=.65):
    m.box((x, height * .46, z), (.055, height * .92, .055), 'wood')
    m.rings((x, 0, z), [(height * .78, .090), (height * .91, .065),
                         (height * 1.08, .025)], 'ember', n=6)
    m.rings((x, 0, z), [(height * .76, .072), (height * .82, .082)],
            'gold_light', n=6)


def add_building_details(m, kind, stage):
    # A cool stepped plinth is deliberately larger than every wall footprint.
    if kind == 'tower_base':
        m.box((0, .035, 0), (1.28, .070, 1.22), 'slate_deep')
        m.box((0, .090, 0), (1.18, .065, 1.12), 'stone_shadow')
        m.box((0, .455, .505), (.34, .70, .040), 'door_recess')
        for x in (-.22, .22):
            m.box((x, .470, .530), (.095, .78, .105), 'stone_light')
        m.box((0, .875, .532), (.56, .095, .112), 'stone_light')
        # Dark underside at the crown makes the roof/platform thickness legible.
        m.box((0, 1.405, 0), (1.17, .075, 1.12), 'mortar')
        prop_crate(m, -.48, .67, .22)
        if stage >= 2:
            prop_torch(m, .49, .59, .58)
        if stage == 3:
            prop_barrel(m, .43, -.52, .20)
    elif kind == 'barracks':
        m.box((0, .035, 0), (1.72, .070, 1.52), 'slate_deep')
        m.box((0, .095, 0), (1.61, .075, 1.42), 'stone_shadow')
        # Recess behind the existing doorway, then thick jambs and lintel.
        m.box((0, .545, .525), (.48, .76, .035), 'door_recess')
        for x in (-.285, .285):
            m.box((x, .565, .558), (.105, .84, .110), 'wood')
        m.box((0, .985, .558), (.67, .115, .115), 'wood_gold')
        # Broad dark beams under all four eaves add contact and material layering.
        for z in (-.710, .710):
            m.box((0, 1.205, z), (1.60, .085, .095), 'mortar')
        for x in (-.785, .785):
            m.box((x, 1.205, 0), (.095, .085, 1.33), 'mortar')
        prop_crate(m, -.64, .79, .27)
        prop_barrel(m, .61, .78, .25)
        if stage >= 2:
            prop_torch(m, .48, .735, .68)
    else:
        m.box((0, .035, 0), (1.82, .070, 1.02), 'slate_deep')
        for x in (-.69, .69):
            m.box((x, .100, 0), (.78, .105, .94), 'stone_shadow')
        # Gate interior sits behind the bars, so the entrance reads as a cavity.
        m.box((0, .720, -.205), (.94, 1.42, .045), 'door_recess')
        m.box((0, 1.475, -.185), (1.08, .120, .145), 'mortar')
        for x in (-.43, .43):
            prop_torch(m, x, .36, .75)
        prop_crate(m, -.97, .47, .24)
        if stage >= 2:
            prop_barrel(m, .98, .43, .22)


def build_buildings():
    specs = []
    for stage in (1, 2, 3):
        specs.extend([
            ('tower_base', stage, 'tower_base_l' + str(stage)),
            ('barracks', stage, 'barracks_l' + str(stage)),
            ('castle_gate', stage, 'castle_gate_l' + str(stage)),
        ])
    # Legacy/fallback aliases intentionally match the polished level-2 stage.
    specs.extend([('tower_base', 2, 'tower_base'),
                  ('barracks', 2, 'barracks'),
                  ('castle_gate', 2, 'castle_gate')])
    for kind, stage, name in specs:
        if kind == 'tower_base':
            m = PHASE['tower_stage'](stage, name, export=False)
        elif kind == 'barracks':
            m = PHASE['barracks_stage'](stage, name)
        else:
            m = PHASE['castle_stage'](stage, name)
        add_building_details(m, kind, stage)
        emit_r3(m, 'building',
                'cool contact plinth; recessed entry; dark eaves; asymmetric baked props')


def load_r2_environment_functions():
    module = ast.parse(R2_ENV.read_text())
    cut = next(i for i, node in enumerate(module.body) if isinstance(node, ast.For))
    ns = {'__file__': str(R2_ENV), '__name__': 'art_r3_environment_base'}
    exec(compile(ast.Module(body=module.body[:cut], type_ignores=[]),
                 str(R2_ENV), 'exec'), ns)
    ns['emit'] = lambda m: m
    return ns


ENV = load_r2_environment_functions()
SAND = ENV['hexcol']('#E9C9B0')
SLATE_LOW = ENV['hexcol']('#56657A')
SLATE_HIGH = ENV['hexcol']('#A7ACB6')


def ridge_block(m, x, z, radius, bottom, height, seed,
                aspect=1.0, lean=(0, 0), sides=7):
    """Four-ring faceted rock: warm cap, bevel, cool value-graded sides."""
    rng = random.Random(seed)
    phase = rng.uniform(0, math.tau)
    rings = []
    profiles = [(0, .80, (0, 0)), (.22, 1.0, (0, 0)),
                (.82, .91, (lean[0] * .78, lean[1] * .78)),
                (1, .72, lean)]
    for level, scale, shift in profiles:
        row = []
        for i in range(sides):
            angle = phase + i * math.tau / sides + rng.uniform(-.10, .10)
            rr = radius * scale * rng.uniform(.94, 1.06)
            row.append((x + shift[0] + math.cos(angle) * rr,
                        bottom + height * level,
                        z + shift[1] + math.sin(angle) * rr * aspect))
        rings.append(row)
    m.face(rings[0], tint(SLATE_LOW, .82))
    for level in range(3):
        t = level / 2
        base = tuple(SLATE_LOW[k] * (1 - t) + SLATE_HIGH[k] * t for k in range(3)) + (1,)
        for i in range(sides):
            j = (i + 1) % sides
            value = .94 + .08 * math.sin(seed * .17 + i * 1.31 + level)
            m.face([rings[level][i], rings[level + 1][i],
                    rings[level + 1][j], rings[level][j]], tint(base, value))
    m.face(list(reversed(rings[-1])), tint(SAND, .97 + rng.random() * .05))


def ridge_silhouette(m, family, x, z, radius, bottom, height, seed, yaw=0):
    """Four repeatable silhouette families, each still part of terrain's one mesh."""
    c, s = math.cos(yaw), math.sin(yaw)
    def offset(dx, dz):
        return x + dx * c - dz * s, z + dx * s + dz * c
    if family == 0:  # flat cap
        ridge_block(m, x, z, radius, bottom, height, seed, .88, (0, 0), 7)
    elif family == 1:  # twin step
        ridge_block(m, x, z, radius, bottom, height * .70, seed, .78, (0, 0), 7)
        qx, qz = offset(radius * .31, radius * .04)
        ridge_block(m, qx, qz, radius * .67, bottom + height * .43,
                    height * .57, seed + 101, .72, (radius * .08, 0), 6)
    elif family == 2:  # leaning wedge
        lx, lz = c * radius * .26, s * radius * .26
        ridge_block(m, x, z, radius * .92, bottom, height, seed, .60,
                    (lx, lz), 7)
    else:  # split buttress
        ridge_block(m, x, z, radius * .67, bottom, height, seed, .86,
                    (0, 0), 6)
        for side in (-1, 1):
            qx, qz = offset(side * radius * .62, -radius * .08)
            ridge_block(m, qx, qz, radius * .52, bottom,
                        height * (.52 if side < 0 else .64), seed + 211 + side,
                        .70, (side * radius * .05, 0), 6)


def build_r3_ridges(m):
    # Intentional clusters replace R2's even necklace.  Radius <= 1.02 m and
    # height <= 2.15 m keep outer masses subordinate to the closer battle.
    clusters = [
        (-10.75, -10.0, 0, 3, 0.88, 1.55, 0.10),
        (-11.20, -6.7, 2, 4, 0.92, 1.85, 0.03),
        (-11.35, -2.6, 3, 3, 0.86, 1.72, -.08),
        (-11.15, 1.3, 1, 4, 0.91, 2.05, .06),
        (-10.45, 5.0, 0, 3, 0.82, 1.62, .20),
        (-10.70, 9.3, 3, 4, 0.90, 1.90, .05),
        (10.55, -9.4, 1, 3, 0.88, 1.52, -.12),
        (11.00, -5.4, 2, 4, 0.95, 1.98, -.02),
        (11.18, -1.2, 0, 3, 0.90, 1.75, .08),
        (10.95, 3.1, 3, 4, 0.94, 2.12, -.16),
        (10.25, 6.9, 1, 3, 0.82, 1.70, -.18),
        (-7.2, 15.0, 2, 4, 1.00, 2.15, .26),
        (-2.7, 16.2, 0, 3, .91, 1.86, .11),
        (3.1, 16.8, 3, 4, .97, 2.02, -.09),
        (7.7, 15.5, 1, 3, .92, 1.90, -.24),
        (-8.4, -14.0, 0, 3, .74, 1.20, .18),
        (8.7, -14.0, 2, 3, .76, 1.24, -.18),
    ]
    rng = random.Random(20260927)
    for cluster_index, (x, z, family, count, radius, height, angle) in enumerate(clusters):
        direction = Vector((math.cos(angle), math.sin(angle)))
        side = Vector((-direction.y, direction.x))
        for i in range(count):
            along = (i - (count - 1) * .5) * radius * 1.32
            jitter = side * rng.uniform(-.24, .24)
            point = Vector((x, z)) + direction * along + jitter
            r = radius * rng.uniform(.70, 1.0)
            h = height * rng.uniform(.72, 1.0)
            ridge_silhouette(m, (family + i) % 4, point.x, point.y, r,
                             -1.52, h, 9101 + cluster_index * 97 + i * 13, angle)
    # Sparse upper-lip clusters make the western/eastern mesa profiles irregular.
    for i, (x, z, family) in enumerate([
            (-8.1, 4.55, 1), (-6.3, 4.72, 2), (-4.9, 5.75, 3),
            (8.6, 5.55, 0), (9.7, 6.75, 2)]):
        ridge_silhouette(m, family, x, z, .55 + .06 * (i % 2),
                         3.10 if x < 0 else 1.20, .72 + .10 * (i % 3),
                         12031 + i * 31, .2 * (i - 2))


def build_terrain():
    ENV['build_block_ridges'] = build_r3_ridges
    ENV['emit'] = lambda m: m
    m = ENV['terrain_r2']()
    return emit_r3(m, 'terrain',
                   'warm sandstone tops; cool slate sides; four clustered ridge silhouettes; reduced outer rock scale')


def arrange_mcp_preview():
    """Leave a legible, non-exported inspection layout in the live MCP scene."""
    for ob in OBJECTS.values():
        ob.hide_viewport = True
        ob.hide_render = True
    layout = {
        'troop_sword': (-3.0, -1.0, 0, 0),
        'troop_spear': (-1.9, -1.0, 0, 0),
        'troop_shield': (-.8, -1.0, 0, 0),
        'king': (1.0, -1.0, 0, 0),
        'tower_base_l3': (3.0, -.8, 0, 0),
        'barracks_l3': (-2.1, 2.25, 0, 0),
        'castle_gate_l3': (.5, 2.25, 0, 0),
    }
    for name, (x, y, z, turn) in layout.items():
        ob = OBJECTS[name]
        ob.hide_viewport = False
        ob.hide_render = False
        ob.location = (x, y, z)
        ob.rotation_euler.z = math.radians(turn)
    # Selection gives MCP a deterministic framing target without saving the .blend.
    bpy.ops.object.select_all(action='DESELECT')
    for name in layout:
        OBJECTS[name].select_set(True)
    bpy.context.view_layer.objects.active = OBJECTS['king']


def main():
    random.seed(20260927)
    remove_owned_scene_objects()
    build_troop_sword()
    build_troop_spear()
    build_troop_shield()
    build_king()
    build_buildings()
    build_terrain()
    arrange_mcp_preview()
    report = {
        'round': 3,
        'license': 'Original procedural art; no downloaded or third-party assets',
        'generator': 'ArtSource/blender/art_r3_models.py',
        'blender_version': bpy.app.version_string,
        'rebuild_command': '/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup --python factory/unity-src/hyeopgok-sasu/ArtSource/blender/art_r3_models.py',
        'contracts': {
            'color': 'COLOR.rgb authored sRGB; COLOR.a 48-ray geometric AO',
            'uv0': 'x team mask (0 tintable / 1 fixed), y baked marker 1',
            'uv1_troops': MOTION_ENCODING,
            'axes': 'Unity x right / y up / z forward; FBX -Z forward +Y up',
            'materials_per_asset': 1,
            'terrain_draw_meshes': 1,
        },
        'assets': ASSETS,
        'totals': {
            'assets': len(ASSETS),
            'triangles': sum(a['triangles'] for a in ASSETS),
            'fbx_bytes': sum(a['fbx_bytes'] for a in ASSETS),
        },
    }
    REPORT.write_text(json.dumps(report, indent=2, ensure_ascii=False) + '\n')
    print('ART_R3_MODELS_COMPLETE', json.dumps(report['totals']))


if __name__ == '__main__':
    main()
