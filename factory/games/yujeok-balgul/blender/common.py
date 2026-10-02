"""Shared helpers for 유적 발굴단 low-poly GLB builders (original assets, no textures).

Authoring space is THREE.JS space: x right, y up, z toward the viewer (front), metres.
`Builder.to_object` converts to Blender Z-up (x, -z, y); the glTF exporter (export_yup)
converts back, so what you author here is exactly what three.js sees.
Materials are flat Principled BSDF colours named from PALETTE (game recolours by name).
"""
import bpy, bmesh, math, random
from mathutils import Vector, Matrix

PALETTE = {
    'stone': '#c9b48f', 'stone_top': '#d8c7a3', 'stone_far': '#e3b778', 'stone_moss': '#8c9b74',
    'wood': '#a8703f', 'wood_dark': '#7a4e2b', 'metal': '#8d97a3', 'cloth': '#f4e6c8',
    'cloth_stripe': '#e0723c', 'rope': '#c9a46b', 'bark': '#8a5a34', 'leaf': '#5fae4a',
    'leaf_dark': '#3f8a3a', 'clay': '#d9824a', 'clay_dark': '#9c4f2a', 'bone': '#f3ead6',
    'paper': '#f1e2b9', 'lizard': '#6cc04a', 'lizard_belly': '#e6e08a', 'flame': '#ffb437',
    'rock': '#b99a76', 'ice': '#bfe6ff', 'ice_dark': '#7fbfe8', 'gold': '#ffc93c',
    'gem': '#e2465a', 'gem_blue': '#3fa7e0', 'bronze': '#c47f3a', 'fossil': '#cdb79a',
    'jade': '#4fb38a', 'skin': '#f2c29b', 'hair': '#4a2f1f', 'hat': '#d9b26a',
    'hat_band': '#7a4e2b', 'shirt': '#2fa39b', 'shorts': '#c9a46b', 'boot': '#6b4127',
    'satchel': '#b5652f', 'scarf': '#e2563b', 'black': '#222222', 'white': '#ffffff',
}
METALLIC = {'metal', 'gold', 'bronze'}
DOUBLE_SIDED = {'leaf', 'leaf_dark'}          # thin fronds only

# three.js -> Blender basis (det +1)
C3 = Matrix(((1, 0, 0), (0, 0, -1), (0, 1, 0)))


def v3(p):
    return Vector((float(p[0]), float(p[1]), float(p[2])))


def to_bl(p):
    return C3 @ v3(p)


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def _lin(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def get_mat(name):
    m = bpy.data.materials.get(name)
    if m:
        return m
    h = PALETTE[name].lstrip('#')
    lin = tuple(_lin(int(h[i:i + 2], 16) / 255) for i in (0, 2, 4))
    m = bpy.data.materials.new(name)
    try:
        m.use_nodes = True
    except Exception:
        pass
    bsdf = m.node_tree.nodes.get('Principled BSDF')
    bsdf.inputs['Base Color'].default_value = lin + (1.0,)
    bsdf.inputs['Roughness'].default_value = 0.8
    bsdf.inputs['Metallic'].default_value = 0.3 if name in METALLIC else 0.0
    m.diffuse_color = lin + (1.0,)
    m.roughness = 0.8
    m.use_backface_culling = name not in DOUBLE_SIDED
    return m


# ---------- matrices (three.js space) ----------
def T(x=0.0, y=0.0, z=0.0):
    return Matrix.Translation((x, y, z))


def R(axis, deg):
    return Matrix.Rotation(math.radians(deg), 4, axis)


def S(x, y=None, z=None):
    y = x if y is None else y
    z = x if z is None else z
    return Matrix.Diagonal((x, y, z, 1.0))


def align_y(d):
    """Rotation taking +Y onto direction d."""
    d = v3(d).normalized()
    return Vector((0, 1, 0)).rotation_difference(d).to_matrix().to_4x4()


def frame(xa, ya, za, origin=(0, 0, 0)):
    """4x4 from column axes."""
    m = Matrix.Identity(4)
    for i, ax in enumerate((xa, ya, za)):
        a = v3(ax)
        m[0][i], m[1][i], m[2][i] = a.x, a.y, a.z
    m[0][3], m[1][3], m[2][3] = origin
    return m


class Builder:
    """Accumulates flat-shaded geometry as plain lists. Every primitive is built in its own
    temporary bmesh (normals recalculated there on a closed shell), then appended."""

    def __init__(self, seed=1):
        self.V = []      # Vector, three.js space
        self.F = []      # tuples of vertex indices
        self.FM = []     # material index per face
        self.mats = []
        self.rng = random.Random(seed)

    def _mi(self, name):
        if name not in self.mats:
            get_mat(name)
            self.mats.append(name)
        return self.mats.index(name)

    def _commit(self, bm, mat, M=None, recalc=True):
        """mat: str for all faces, or list of names aligned with bm.faces order."""
        names = [mat] * len(bm.faces) if isinstance(mat, str) else list(mat)
        if M is not None:
            for v in bm.verts:
                v.co = M @ v.co
        bm.verts.index_update()
        faces = list(bm.faces)
        for f, nm in zip(faces, names):
            f.material_index = self._mi(nm)
        if recalc:
            bmesh.ops.recalc_face_normals(bm, faces=faces)
        base = len(self.V)
        self.V.extend(v.co.copy() for v in bm.verts)
        bm.verts.index_update()
        for f in faces:
            self.F.append(tuple(base + v.index for v in f.verts))
            self.FM.append(f.material_index)
        n = len(faces)
        bm.free()
        return n

    # ---- primitives ----
    def box(self, c, s, mat, bevel=0.0, segs=1, M=None):
        return self.obox(c, s, mat, M if M is not None else Matrix.Identity(4), bevel=bevel, segs=segs)

    def obox(self, c, s, mat, M, bevel=0.0, segs=1):
        """Box of size s centred at local c, then transformed by M."""
        bm = bmesh.new()
        r = bmesh.ops.create_cube(bm, size=1.0, matrix=S(*s))
        if bevel > 0:
            bmesh.ops.bevel(bm, geom=list(bm.verts) + list(bm.edges), offset=bevel, offset_type='OFFSET',
                            segments=segs, profile=0.5, affect='EDGES', clamp_overlap=True)
        return self._commit(bm, mat, M @ T(*c))

    def beam(self, a, b, w, t, mat, side=(0, 0, 1), bevel=0.0):
        """Box from a to b; width w along `side` (projected), thickness t."""
        a, b = v3(a), v3(b)
        u = (b - a).normalized()
        sd = v3(side)
        sd = (sd - u * sd.dot(u)).normalized()
        n = sd.cross(u)
        M = frame(u, n, sd, (a + b) / 2)
        return self.obox((0, 0, 0), ((b - a).length, t, w), mat, M, bevel=bevel)

    def ico(self, c, r, mat, sub=2, M=None, jitter=0.0):
        """Blender subdivisions: 1 = 20-face icosahedron, 2 = 80 faces, 3 = 320 faces."""
        if not isinstance(r, (tuple, list)):
            r = (r, r, r)
        bm = bmesh.new()
        bmesh.ops.create_icosphere(bm, subdivisions=sub, radius=1.0, matrix=Matrix.Identity(4))
        if jitter:
            for v in bm.verts:
                v.co = v.co * (1 + self.rng.uniform(-jitter, jitter))
        base = (M if M is not None else Matrix.Identity(4)) @ T(*c) @ S(*r)
        return self._commit(bm, mat, base)

    def sphere(self, c, r, mat, u=10, v=7, M=None, phase=0.0):
        """Faceted UV ellipsoid."""
        if not isinstance(r, (tuple, list)):
            r = (r, r, r)
        prof = [(math.sin(math.pi * k / v), -math.cos(math.pi * k / v) * r[1]) for k in range(v + 1)]
        prof[0] = (0.0, prof[0][1])
        prof[-1] = (0.0, prof[-1][1])
        base = (M if M is not None else Matrix.Identity(4)) @ T(*c)
        return self.lathe(prof, mat, n=u, M=base, phase=phase, aspect=(r[0], r[2]))

    def loft(self, rings, mats, cap0=None, cap1=None, closed=False, open_ring=False, recalc=True):
        """rings: list of lists of 3D points (len 1 = apex). mats: str | list per band | f(j,i)."""
        bm = bmesh.new()
        vr = [[bm.verts.new(v3(p)) for p in r] for r in rings]
        nb = len(vr) if closed else len(vr) - 1
        names = []

        def mat_of(j, i):
            if isinstance(mats, str):
                return mats
            if callable(mats):
                return mats(j, i)
            return mats[min(j, len(mats) - 1)]

        for j in range(nb):
            A, B = vr[j], vr[(j + 1) % len(vr)]
            if len(A) == 1 and len(B) == 1:
                continue
            n = max(len(A), len(B))
            for i in (range(n - 1) if open_ring else range(n)):
                k = (i + 1) % n
                if len(A) == 1:
                    f = [A[0], B[i], B[k]]
                elif len(B) == 1:
                    f = [A[i], A[k], B[0]]
                else:
                    f = [A[i], A[k], B[k], B[i]]
                bm.faces.new(f)
                names.append(mat_of(j, i))
        if not closed:
            if cap0 and len(vr[0]) > 2:
                bm.faces.new(list(reversed(vr[0])))
                names.append(cap0)
            if cap1 and len(vr[-1]) > 2:
                bm.faces.new(vr[-1])
                names.append(cap1)
        return self._commit(bm, names, recalc=recalc)

    def lathe(self, profile, mats, n=8, M=None, phase=0.0, cap0=None, cap1=None, aspect=(1, 1)):
        """profile: [(r, y)] along +Y. r==0 -> apex. caps default to the end band material."""
        M = M if M is not None else Matrix.Identity(4)
        rings = []
        for r, y in profile:
            if r <= 1e-6:
                rings.append([M @ Vector((0, y, 0))])
            else:
                rings.append([M @ Vector((math.cos(phase + i * math.tau / n) * r * aspect[0], y,
                                          math.sin(phase + i * math.tau / n) * r * aspect[1]))
                              for i in range(n)])
        first = mats if isinstance(mats, str) else (mats[0] if isinstance(mats, list) else mats(0, 0))
        last = mats if isinstance(mats, str) else (mats[-1] if isinstance(mats, list) else mats(len(profile) - 2, 0))
        return self.loft(rings, mats, cap0=cap0 or first, cap1=cap1 or last)

    def cyl(self, a, b, r, mat, n=8, r2=None, phase=0.0, aspect=(1, 1), cap0=None, cap1=None):
        a, b = v3(a), v3(b)
        L = (b - a).length
        M = T(*a) @ align_y(b - a)
        return self.lathe([(r, 0), (r if r2 is None else r2, L)], mat, n=n, M=M, phase=phase,
                          aspect=aspect, cap0=cap0, cap1=cap1)

    def tube(self, pts, radii, mats, n=6, cap0=True, cap1=True, ref=None, closed=False, aspect=(1, 1), phase=0.0):
        pts = [v3(p) for p in pts]
        if not isinstance(radii, (list, tuple)):
            radii = [radii] * len(pts)
        m = len(pts)
        tans = []
        for i in range(m):
            if closed:
                t = pts[(i + 1) % m] - pts[i - 1]
            else:
                t = pts[min(i + 1, m - 1)] - pts[max(i - 1, 0)]
            tans.append(t.normalized())
        rings = []
        if ref is not None:
            refv = v3(ref)
        else:
            t0 = tans[0]
            refv = t0.cross(Vector((0, 0, 1)) if abs(t0.z) < 0.9 else Vector((1, 0, 0)))
        nrm = (refv - tans[0] * refv.dot(tans[0])).normalized()
        for i, p in enumerate(pts):
            t = tans[i]
            if ref is not None:
                nrm = (refv - t * refv.dot(t)).normalized()
            elif i > 0:
                nrm = tans[i - 1].rotation_difference(t) @ nrm
                nrm = (nrm - t * nrm.dot(t)).normalized()
            bn = t.cross(nrm)
            r = radii[i]
            if r <= 1e-6:
                rings.append([p])
            else:
                rings.append([p + (nrm * math.cos(phase + k * math.tau / n) * aspect[0]
                                   + bn * math.sin(phase + k * math.tau / n) * aspect[1]) * r
                              for k in range(n)])
        first = mats if isinstance(mats, str) else (mats[0] if isinstance(mats, list) else mats(0, 0))
        last = mats if isinstance(mats, str) else (mats[-1] if isinstance(mats, list) else mats(m - 2, 0))
        return self.loft(rings, mats, cap0=first if cap0 else None, cap1=last if cap1 else None, closed=closed)

    def prism(self, pts2, z0, z1, mat, M=None, cap=None):
        """2D polygon (x,y) extruded along local z from z0 to z1, transformed by M."""
        M = M if M is not None else Matrix.Identity(4)
        r0 = [M @ Vector((x, y, z0)) for x, y in pts2]
        r1 = [M @ Vector((x, y, z1)) for x, y in pts2]
        return self.loft([r0, r1], mat, cap0=cap or mat, cap1=cap or mat)

    def clamp_ground(self, y0=0.0, start=0):
        for v in self.V[start:]:
            if v.y < y0:
                v.y = y0

    def tri_count(self):
        return sum(len(f) - 2 for f in self.F)

    def to_object(self, name, pivot=(0, 0, 0), parent=None, parent_pivot=(0, 0, 0)):
        pv = v3(pivot)
        verts = [tuple(C3 @ (v - pv)) for v in self.V]
        me = bpy.data.meshes.new(name + '_mesh')
        me.from_pydata(verts, [], self.F)
        for mn in self.mats:
            me.materials.append(get_mat(mn))
        for p, mi in zip(me.polygons, self.FM):
            p.material_index = mi
            p.use_smooth = False
        me.validate(clean_customdata=False)
        me.update()
        ob = bpy.data.objects.new(name, me)
        bpy.context.scene.collection.objects.link(ob)
        if parent is not None:
            ob.parent = parent
        ob.location = C3 @ (pv - v3(parent_pivot))
        return ob


def empty(name, loc=(0, 0, 0), parent=None):
    ob = bpy.data.objects.new(name, None)
    ob.empty_display_size = 0.2
    bpy.context.scene.collection.objects.link(ob)
    ob.location = to_bl(loc)
    if parent is not None:
        ob.parent = parent
    return ob


def mesh_tris(ob):
    me = ob.data
    me.calc_loop_triangles()
    return len(me.loop_triangles)


def export_glb(path):
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='DESELECT')
    kw = dict(filepath=str(path), export_format='GLB', export_yup=True, export_apply=True,
              use_selection=False, export_materials='EXPORT', export_normals=True,
              export_texcoords=False, export_tangents=False, export_cameras=False,
              export_lights=False, export_animations=False, export_vertex_color='NONE',
              export_extras=False, export_image_format='NONE')
    bpy.ops.export_scene.gltf(**kw)
