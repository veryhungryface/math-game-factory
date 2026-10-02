"""Imports digger.glb / kit.glb and renders Workbench lineup previews to ./preview/.

Run: Blender -b --factory-startup --python render_preview.py [-- digger|kit|all]
"""
import bpy, sys, math
from pathlib import Path
from mathutils import Vector

HERE = Path(__file__).resolve().parent
MODELS = HERE.parents[3] / 'public' / 'g' / 'yujeok-balgul' / 'assets' / 'models'
PREV = HERE / 'preview'
PREV.mkdir(exist_ok=True)
which = sys.argv[sys.argv.index('--') + 1] if '--' in sys.argv else 'all'

bpy.ops.wm.read_factory_settings(use_empty=True)
sc = bpy.context.scene
sc.render.engine = 'BLENDER_WORKBENCH'
sh = sc.display.shading
sh.light = 'STUDIO'
sh.color_type = 'MATERIAL'
sh.show_shadows = True
sh.show_cavity = False
sh.show_object_outline = True
sh.object_outline_color = (0.15, 0.1, 0.05)
sh.show_backface_culling = True
sc.render.film_transparent = False
sc.world = bpy.data.worlds.new('W')
sc.world.color = (0.55, 0.45, 0.32)
sc.render.resolution_percentage = 100


def import_glb(p):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(p))
    return [o for o in bpy.data.objects if o not in before]


def roots(objs):
    return [o for o in objs if o.parent is None]


def world_bbox(o):
    pts = []
    for c in [o] + list(o.children_recursive):
        if c.type == 'MESH':
            pts += [c.matrix_world @ Vector(v) for v in c.bound_box]
    if not pts:
        return Vector((0, 0, 0)), Vector((0, 0, 0))
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx


def ground(size=200):
    bpy.ops.mesh.primitive_plane_add(size=size, location=(0, 0, 0))
    g = bpy.context.active_object
    m = bpy.data.materials.new('ground_prev')
    m.diffuse_color = (0.75, 0.6, 0.4, 1)
    g.data.materials.append(m)
    g.name = 'PREVIEW_GROUND'
    return g


def camera(center, dist, elev_deg=25, azim_deg=0, ortho=None, res=(1600, 900)):
    sc.render.resolution_x, sc.render.resolution_y = res
    cam_d = bpy.data.cameras.new('cam')
    cam = bpy.data.objects.new('cam', cam_d)
    sc.collection.objects.link(cam)
    e, a = math.radians(elev_deg), math.radians(azim_deg)
    # three.js front (+Z) == Blender -Y
    off = Vector((math.sin(a) * math.cos(e), -math.cos(a) * math.cos(e), math.sin(e))) * dist
    cam.location = Vector(center) + off
    direction = Vector(center) - cam.location
    cam.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
    if ortho:
        cam_d.type = 'ORTHO'
        cam_d.ortho_scale = ortho
    cam_d.clip_end = 1000
    sc.camera = cam
    return cam


def render(name):
    sc.render.filepath = str(PREV / name)
    bpy.ops.render.render(write_still=True)
    print('RENDERED', PREV / name)


def clear():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)


if which in ('digger', 'all'):
    clear()
    ground()
    objs = None
    for i, az in enumerate((0, 90, 180, 270, 35)):
        objs = import_glb(MODELS / 'digger.glb')
        r = roots(objs)[0]
        r.location.x = (i - 2) * 0.9
        r.rotation_mode = 'XYZ'
        r.rotation_euler.z += math.radians(az)
        # tool visibility: show brush on first three, chisel on last two
        for o in objs:
            base = o.name.split('.')[0]
            if base == 'Chisel' and i < 3:
                o.hide_render = True
            if base == 'Brush' and i >= 3:
                o.hide_render = True
    camera((0, 0, 0.55), 6.0, elev_deg=12, ortho=4.8, res=(1800, 700))
    render('digger_turnaround.png')
    clear()
    ground()
    objs = import_glb(MODELS / 'digger.glb')
    for o in objs:
        if o.name.split('.')[0] == 'Chisel':
            o.hide_render = True
    camera((0, 0, 0.55), 3.0, elev_deg=38, azim_deg=20, ortho=1.35, res=(900, 1000))
    render('digger_closeup.png')

if which in ('kit', 'all'):
    clear()
    objs = import_glb(MODELS / 'kit.glb')
    rs = {o.name: o for o in roots(objs)}
    big = ['Pyramid', 'Temple', 'IceArch']
    mid = ['Tent', 'Palm', 'JungleTree', 'IceCrystal', 'Cactus', 'Torch', 'Sign', 'Cart', 'Crate', 'Rock', 'RockFlat', 'RopePost']
    small = ['Tablet', 'Pot', 'Bones', 'Scroll', 'Lizard', 'Bucket', 'Shard']
    arts = sorted(n for n in rs if n.startswith('Art_'))
    print('KIT ROOTS', sorted(rs))

    def layout(names, spacing_fn, z0=0.0):
        x = 0.0
        placed = []
        for n in names:
            o = rs[n]
            mn, mx = world_bbox(o)
            w = mx.x - mn.x
            o.location.x = x - mn.x  # place bbox min x at cursor (location was 0)
            x += w + spacing_fn(w)
            placed.append(o)
        return x

    def show_only(names):
        for n, o in rs.items():
            vis = n in names
            for c in [o] + list(o.children_recursive):
                c.hide_render = not vis

    # groups
    for grp, names, spacing, elev, fname in (
        ('big', big, lambda w: 2.0, 20, 'kit_backdrops.png'),
        ('mid', mid[:6], lambda w: 0.4, 20, 'kit_props_a.png'),
        ('mid2', mid[6:], lambda w: 0.25, 24, 'kit_props_b.png'),
        ('small', small, lambda w: 0.2, 30, 'kit_small.png'),
        ('arts', arts[:8], lambda w: 0.08, 22, 'kit_artifacts_a.png'),
        ('arts2', arts[8:], lambda w: 0.08, 22, 'kit_artifacts_b.png'),
    ):
        for o in rs.values():
            o.location = (0, 0, 0)
        total = layout(names, spacing)
        for n in names:
            rs[n].location.x -= total / 2
        show_only(names)
        heights = [world_bbox(rs[n])[1].z for n in names]
        hmax = max(heights)
        g = ground()
        camera((0, 0, hmax * 0.45), total * 2 + 10, elev_deg=elev, ortho=max(total * 1.04, hmax * 2.6), res=(2000, 700))
        render(fname)
        bpy.data.objects.remove(g, do_unlink=True)
