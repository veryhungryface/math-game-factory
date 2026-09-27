#!/usr/bin/env python3
"""Deterministic, texture-free v3.2 biome maps for Hyeopgok Sasu.

The script owns only these outputs:

* Resources/HyeopgokSasu/Maps/terrain_desert.fbx
* Resources/HyeopgokSasu/Maps/terrain_snow.fbx
* ArtSource/blender/v32-maps-report.json

Every FBX is one flat-shaded mesh with one material.  Authored sRGB lives in
the ``Color`` vertex-colour channel and deterministic geometric AO is baked to
its alpha channel.  UV0.x preserves the fixed-colour mask and UV0.y marks the
baked encoding, matching the existing Hyeopgok model contract.

Run twice to prove reproducibility in the report::

    /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup \
      --python factory/unity-src/hyeopgok-sasu/ArtSource/blender/v32_maps.py

    The second run records the canonical mesh-semantic comparison and also reports
    Blender's non-contractual FBX byte comparison for diagnosis.  No scene-wide
    delete is used: live MCP sessions lose only objects,
meshes and materials whose names begin with ``v32_``.
"""

from __future__ import annotations

import gzip
import hashlib
import json
import math
import random
from pathlib import Path
from typing import Dict, Iterable, List, Sequence, Tuple

import bpy
from mathutils import Vector


HERE = Path(__file__).resolve().parent
PROJECT = HERE.parent.parent
OUTPUT = PROJECT / "Resources" / "HyeopgokSasu" / "Maps"
REPORT = HERE / "v32-maps-report.json"
AO_SOURCE = HERE / "ao_bake.py"

SEED = 320928
OWNED_PREFIX = "v32_"
COLLECTION_NAME = "V32_Maps"
RAW_BUDGET_BYTES = 130_000
TRIANGLE_BUDGET = 1_500
VERTEX_BUDGET = 4_500

Color = Tuple[float, float, float, float]
Point = Tuple[float, float, float]
Point2 = Tuple[float, float]


def hex_color(value: str, alpha: float = 1.0) -> Color:
    value = value.lstrip("#")
    return tuple(int(value[index:index + 2], 16) / 255.0 for index in (0, 2, 4)) + (alpha,)


DESERT = {
    "sand": hex_color("#D8B26B"),
    "sand_light": hex_color("#E6C680"),
    "sand_dark": hex_color("#C99550"),
    "path": hex_color("#C8B59C"),
    "path_light": hex_color("#D9C9B4"),
    "sandstone_top": hex_color("#E9C9B0"),
    "sandstone": hex_color("#9C897D"),
    "sandstone_dark": hex_color("#695E63"),
    "water": hex_color("#559FB0"),
    "water_deep": hex_color("#358B9F"),
    "wood": hex_color("#8A5B39"),
    "wood_light": hex_color("#BE9465"),
    "wood_dark": hex_color("#68412F"),
    "cactus": hex_color("#3E8B4E"),
    "cactus_light": hex_color("#66A85B"),
    "deadwood": hex_color("#76513B"),
}

SNOW = {
    "snow": hex_color("#EAF3F4"),
    "snow_light": hex_color("#F7FCFC"),
    "snow_shadow": hex_color("#CBDCE3"),
    "path": hex_color("#607883"),
    "path_light": hex_color("#8197A0"),
    "rock": hex_color("#607381"),
    "rock_dark": hex_color("#475966"),
    "ice": hex_color("#78B9C8"),
    "ice_light": hex_color("#B7E8EE"),
    "ice_deep": hex_color("#5A9EAF"),
    "pine": hex_color("#1F5A4A"),
    "pine_light": hex_color("#2E6D2A"),
    "trunk": hex_color("#604938"),
}

# Current v3 interaction coordinates remain obstacle-free.  Ground paint and
# rivers may pass nearby; every raised prop is checked against these circles.
CLEAR_ZONES = [
    ("answer_1", -1.8, 2.0, 1.24),
    ("answer_2", 0.6, 2.0, 1.24),
    ("answer_3", -1.8, -1.0, 1.24),
    ("answer_4", 0.6, -1.0, 1.24),
    ("tower_1", 1.78, -3.72, 1.58),
    ("tower_2", 1.82, 4.62, 1.58),
    ("tower_3", -3.14, -3.72, 1.58),
]


def unity_to_blender(point: Point) -> Point:
    """Match build_models.py: Unity +Y/+Z becomes Blender +Z/-Y."""
    return (point[0], -point[2], point[1])


def rotate_y(point: Point, yaw: float) -> Point:
    cosine, sine = math.cos(yaw), math.sin(yaw)
    return (
        point[0] * cosine + point[2] * sine,
        point[1],
        -point[0] * sine + point[2] * cosine,
    )


class MeshBuilder:
    """Small deterministic triangle builder using authored Unity coordinates."""

    def __init__(self, name: str):
        self.name = name
        self.vertices: List[Point] = []
        self.faces: List[Tuple[int, int, int]] = []
        self.colors: List[Color] = []

    def triangle(self, a: Point, b: Point, c: Point, color: Color) -> None:
        start = len(self.vertices)
        self.vertices.extend((unity_to_blender(a), unity_to_blender(b), unity_to_blender(c)))
        self.faces.append((start, start + 1, start + 2))
        self.colors.extend((color, color, color))

    def quad(self, a: Point, b: Point, c: Point, d: Point, color: Color) -> None:
        self.triangle(a, b, c, color)
        self.triangle(a, c, d, color)

    def box(
        self,
        center: Point,
        size: Point,
        color: Color,
        yaw: float = 0.0,
        top_color: Color | None = None,
    ) -> None:
        hx, hy, hz = (value * 0.5 for value in size)
        local = [
            (-hx, -hy, -hz), (hx, -hy, -hz), (hx, -hy, hz), (-hx, -hy, hz),
            (-hx, hy, -hz), (hx, hy, -hz), (hx, hy, hz), (-hx, hy, hz),
        ]
        points = []
        for point in local:
            rotated = rotate_y(point, yaw)
            points.append(tuple(center[index] + rotated[index] for index in range(3)))
        # Winding is outward after the Unity-to-Blender axis conversion.
        for indices, face_color in (
            ((0, 1, 2, 3), color),
            ((4, 7, 6, 5), top_color or color),
            ((0, 4, 5, 1), color),
            ((3, 2, 6, 7), color),
            ((0, 3, 7, 4), color),
            ((1, 5, 6, 2), color),
        ):
            self.quad(*(points[index] for index in indices), face_color)

    def frustum(
        self,
        center: Point2,
        bottom_y: float,
        top_y: float,
        bottom_radii: Sequence[float],
        top_radii: Sequence[float],
        side_colors: Sequence[Color],
        top_color: Color,
        angle_offset: float = 0.0,
        cap_bottom: bool = True,
        cap_top: bool = True,
    ) -> None:
        count = len(bottom_radii)
        if count < 3 or len(top_radii) != count:
            raise ValueError("frustum rings must have the same 3+ point count")
        bottom, top = [], []
        for index in range(count):
            angle = angle_offset + math.tau * index / count
            cosine, sine = math.cos(angle), math.sin(angle)
            bottom.append((center[0] + cosine * bottom_radii[index], bottom_y, center[1] + sine * bottom_radii[index]))
            top.append((center[0] + cosine * top_radii[index], top_y, center[1] + sine * top_radii[index]))
        for index in range(count):
            nxt = (index + 1) % count
            self.quad(bottom[index], top[index], top[nxt], bottom[nxt], side_colors[index % len(side_colors)])
        bottom_center = (center[0], bottom_y, center[1])
        top_center = (center[0], top_y, center[1])
        if cap_bottom:
            for index in range(count):
                nxt = (index + 1) % count
                self.triangle(bottom_center, bottom[index], bottom[nxt], side_colors[index % len(side_colors)])
        if cap_top:
            for index in range(count):
                nxt = (index + 1) % count
                self.triangle(top_center, top[nxt], top[index], top_color)

    def cylinder(
        self,
        center: Point,
        radius: float,
        height: float,
        color: Color,
        sides: int = 6,
        top_color: Color | None = None,
    ) -> None:
        radii = [radius] * sides
        self.frustum(
            (center[0], center[2]), center[1] - height * 0.5, center[1] + height * 0.5,
            radii, radii, [color], top_color or color,
        )

    def object(self, collection: bpy.types.Collection) -> bpy.types.Object:
        mesh = bpy.data.meshes.new(f"{OWNED_PREFIX}{self.name}_mesh")
        mesh.from_pydata(self.vertices, [], self.faces)
        mesh.validate(verbose=False, clean_customdata=False)
        mesh.update(calc_edges=True)
        colors = mesh.color_attributes.new(name="Color", type="BYTE_COLOR", domain="CORNER")
        for index, color in enumerate(self.colors):
            colors.data[index].color_srgb = color
        mesh.color_attributes.active_color = colors
        mesh.color_attributes.render_color_index = 0
        material = bpy.data.materials.new(f"{OWNED_PREFIX}{self.name}_vertex_color")
        material.diffuse_color = (1.0, 1.0, 1.0, 1.0)
        material.use_nodes = False
        mesh.materials.append(material)
        obj = bpy.data.objects.new(f"{OWNED_PREFIX}{self.name}", mesh)
        collection.objects.link(obj)
        return obj


def catmull(points: Sequence[Point2], steps: int = 3) -> List[Point2]:
    padded = [points[0], *points, points[-1]]
    output: List[Point2] = []
    for segment in range(1, len(padded) - 2):
        a, b, c, d = (Vector(value) for value in padded[segment - 1:segment + 3])
        for step in range(steps):
            t = step / steps
            value = 0.5 * ((2 * b) + (-a + c) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (-a + 3 * b - 3 * c + d) * t * t * t)
            output.append((float(value.x), float(value.y)))
    output.append(points[-1])
    return output


def ribbon(builder: MeshBuilder, controls: Sequence[Point2], y: float, width: float, colors: Sequence[Color], steps: int = 3) -> None:
    samples = catmull(controls, steps)
    edges = []
    for index, point in enumerate(samples):
        previous = Vector(samples[max(0, index - 1)])
        following = Vector(samples[min(len(samples) - 1, index + 1)])
        tangent = (following - previous).normalized()
        normal = Vector((-tangent.y, tangent.x)) * (width * 0.5)
        edges.append(((point[0] + normal.x, y, point[1] + normal.y), (point[0] - normal.x, y, point[1] - normal.y)))
    for index in range(len(edges) - 1):
        left, right = edges[index]
        next_left, next_right = edges[index + 1]
        builder.quad(left, next_left, next_right, right, colors[index % len(colors)])


def ground_grid(builder: MeshBuilder, palette: Sequence[Color]) -> None:
    x_values = (-15.0, -10.0, -5.0, 0.0, 5.0, 10.0, 15.0)
    z_values = (-14.0, -9.0, -4.0, 1.0, 6.0, 11.0, 17.0)
    for z_index in range(len(z_values) - 1):
        for x_index in range(len(x_values) - 1):
            x0, x1 = x_values[x_index], x_values[x_index + 1]
            z0, z1 = z_values[z_index], z_values[z_index + 1]
            color = palette[(x_index * 3 + z_index * 5) % len(palette)]
            builder.quad((x0, 1.2, z0), (x0, 1.2, z1), (x1, 1.2, z1), (x1, 1.2, z0), color)


class ClearanceAudit:
    def __init__(self) -> None:
        self.entries: List[Dict[str, float | str]] = []
        self.minimum = float("inf")

    def raised(self, label: str, x: float, z: float, radius: float) -> None:
        nearest = float("inf")
        nearest_zone = ""
        for zone, zx, zz, zone_radius in CLEAR_ZONES:
            clearance = math.hypot(x - zx, z - zz) - radius - zone_radius
            if clearance < nearest:
                nearest, nearest_zone = clearance, zone
        if nearest < 0.0:
            raise RuntimeError(f"{label} overlaps clear zone {nearest_zone}: {nearest:.3f}m")
        self.minimum = min(self.minimum, nearest)
        self.entries.append({"feature": label, "nearest_zone": nearest_zone, "clearance_m": round(nearest, 3)})


def add_mesa(builder: MeshBuilder, audit: ClearanceAudit, rng: random.Random, label: str, x: float, z: float, radius: float, height: float) -> None:
    audit.raised(label, x, z, radius)
    count = 8
    variation = [0.82 + rng.random() * 0.30 for _ in range(count)]
    bottom = [radius * (1.04 + (index % 2) * 0.05) * variation[index] for index in range(count)]
    top = [radius * 0.72 * variation[index] for index in range(count)]
    builder.frustum(
        (x, z), 1.2, 1.2 + height, bottom, top,
        [DESERT["sandstone"], DESERT["sandstone_dark"]], DESERT["sandstone_top"],
        angle_offset=rng.random() * 0.5,
    )


def add_cactus(builder: MeshBuilder, audit: ClearanceAudit, label: str, x: float, z: float, scale: float, yaw: float) -> None:
    audit.raised(label, x, z, 0.72 * scale)
    builder.cylinder((x, 1.2 + 0.78 * scale, z), 0.18 * scale, 1.56 * scale, DESERT["cactus"], 6, DESERT["cactus_light"])
    for side in (-1.0, 1.0):
        offset_x = math.cos(yaw) * side * 0.36 * scale
        offset_z = -math.sin(yaw) * side * 0.36 * scale
        builder.box((x + offset_x, 1.2 + 0.83 * scale, z + offset_z), (0.48 * scale, 0.18 * scale, 0.18 * scale), DESERT["cactus"], yaw)
        builder.cylinder((x + offset_x * 1.58, 1.2 + 1.02 * scale, z + offset_z * 1.58), 0.13 * scale, 0.55 * scale, DESERT["cactus"], 6, DESERT["cactus_light"])


def add_dead_tree(builder: MeshBuilder, audit: ClearanceAudit, label: str, x: float, z: float, scale: float, yaw: float) -> None:
    audit.raised(label, x, z, 0.72 * scale)
    builder.box((x, 1.2 + 0.72 * scale, z), (0.22 * scale, 1.44 * scale, 0.22 * scale), DESERT["deadwood"], yaw)
    for side in (-1.0, 1.0):
        local = rotate_y((side * 0.33 * scale, 0.0, 0.0), yaw)
        builder.box((x + local[0], 1.2 + (1.12 if side < 0 else 0.88) * scale, z + local[2]), (0.72 * scale, 0.14 * scale, 0.14 * scale), DESERT["deadwood"], yaw + side * 0.28)


def add_bridge(builder: MeshBuilder, audit: ClearanceAudit, x: float, z: float) -> None:
    # The generous audit radius encloses the enlarged deck corners, not just
    # its centreline, so the bridge cannot steal a tower or answer socket.
    audit.raised("desert_bridge", x, z, 1.80)
    for index in range(7):
        plank_z = z - 1.14 + index * 0.38
        builder.box((x, 1.38 + (index % 2) * 0.012, plank_z), (2.36, 0.16, 0.34), DESERT["wood_light"] if index % 3 else DESERT["wood"])
    for side in (-1.0, 1.0):
        builder.box((x + side * 1.13, 1.71, z), (0.12, 0.12, 2.68), DESERT["wood_dark"])
        for index in range(4):
            builder.box((x + side * 1.13, 1.50, z - 1.12 + index * 0.75), (0.14, 0.64, 0.14), DESERT["wood_dark"])


def add_snow_rock(builder: MeshBuilder, audit: ClearanceAudit, rng: random.Random, label: str, x: float, z: float, radius: float, height: float) -> None:
    audit.raised(label, x, z, radius)
    count = 7
    variation = [0.82 + rng.random() * 0.30 for _ in range(count)]
    bottom = [radius * factor for factor in variation]
    shoulder = [radius * 0.62 * factor for factor in variation]
    builder.frustum((x, z), 1.2, 1.2 + height * 0.74, bottom, shoulder, [SNOW["rock"], SNOW["rock_dark"]], SNOW["snow_shadow"], rng.random() * 0.5)
    cap_bottom = [value * 1.06 for value in shoulder]
    cap_top = [value * 0.55 for value in shoulder]
    builder.frustum((x, z), 1.2 + height * 0.70, 1.2 + height, cap_bottom, cap_top, [SNOW["snow_shadow"], SNOW["snow"]], SNOW["snow_light"], rng.random() * 0.2)


def add_snow_pine(builder: MeshBuilder, audit: ClearanceAudit, label: str, x: float, z: float, scale: float, yaw: float) -> None:
    audit.raised(label, x, z, 0.82 * scale)
    builder.cylinder((x, 1.2 + 0.56 * scale, z), 0.13 * scale, 1.12 * scale, SNOW["trunk"], 6)
    lower = [0.72 * scale] * 6
    upper = [0.12 * scale] * 6
    builder.frustum((x, z), 1.62 * scale + 0.54, 2.60 * scale + 0.18, lower, upper, [SNOW["pine"], SNOW["pine_light"]], SNOW["pine_light"], yaw, cap_bottom=False)
    snow_bottom = [0.54 * scale] * 6
    snow_top = [0.07 * scale] * 6
    builder.frustum((x, z), 1.2 + 1.32 * scale, 1.2 + 2.16 * scale, snow_bottom, snow_top, [SNOW["snow_shadow"], SNOW["snow"]], SNOW["snow_light"], yaw + 0.08, cap_bottom=False)


def add_snow_fortress(builder: MeshBuilder, audit: ClearanceAudit, x: float, z: float) -> None:
    """Compact tall fortress that remains legible in both camera aspects."""
    audit.raised("snow_fortress", x, z, 1.80)
    builder.box((x, 2.0, z), (2.8, 1.6, 0.72), SNOW["rock_dark"], top_color=SNOW["snow"])
    # Four oversized merlons preserve the keep silhouette after the footprint
    # is narrowed enough to sit beside the central interaction sockets.
    for index in range(4):
        builder.box((x - 1.05 + index * 0.70, 3.02, z), (0.42, 0.48, 0.78), SNOW["rock"], top_color=SNOW["snow_light"])
    builder.box((x, 2.0, z - 0.38), (0.80, 1.48, 0.08), SNOW["rock_dark"])
    for side in (-1.0, 1.0):
        tx = x + side * 1.27
        builder.frustum((tx, z), 1.2, 3.28, [0.50] * 6, [0.47] * 6,
                        [SNOW["rock"], SNOW["rock_dark"]], SNOW["snow_shadow"], angle_offset=0.16)
        builder.frustum((tx, z), 3.18, 3.55, [0.48] * 6, [0.38] * 6,
                        [SNOW["snow_shadow"], SNOW["snow"]], SNOW["snow_light"], angle_offset=0.16)
        builder.cylinder((tx, 4.18, z), 0.055, 1.5, SNOW["trunk"], 6)
        builder.box((tx + side * 0.30, 4.38, z), (0.62, 0.52, 0.08), SNOW["ice_deep"])


def build_desert() -> Tuple[MeshBuilder, Dict[str, object]]:
    builder = MeshBuilder("terrain_desert")
    audit = ClearanceAudit()
    rng = random.Random(SEED + 11)
    ground_grid(builder, (DESERT["sand"], DESERT["sand_light"], DESERT["sand_dark"]))

    # Bring the river/bridge into the shared portrait-landscape combat focus.
    # A broad, almost-horizontal tangent at the bridge keeps the north/south
    # deck visibly perpendicular to the water.  The water sits 4.8 cm above
    # the ground; the former near-coplanar glint ribbon was deliberately
    # removed because it produced long orange/teal z-fight streaks in WebGL.
    river_controls = [
        (-15.0, 3.0), (-9.0, 3.5), (-3.0, 4.8), (0.0, 2.5), (2.2, 0.2),
        (3.6, 0.0), (6.5, 0.1), (11.0, -1.0), (15.0, -1.5),
    ]
    ribbon(builder, river_controls, 1.248, 2.72, (DESERT["water_deep"], DESERT["water"]), steps=3)
    path_controls = [
        (6.0, 16.5), (5.8, 10.0), (5.2, 5.0), (3.6, 2.5), (3.6, 0.0),
        (5.0, -2.5), (2.5, -5.5), (-1.0, -7.0), (-4.5, -4.0), (-4.2, -0.8),
    ]
    ribbon(builder, path_controls, 1.265, 2.12, (DESERT["path"], DESERT["path_light"]), steps=3)
    add_bridge(builder, audit, 3.6, 0.0)

    for index, values in enumerate((
        (-7.1, -5.4, 2.2, 2.45), (-6.2, 4.6, 1.9, 2.05), (-5.8, 10.0, 2.35, 2.6),
        (8.6, 8.0, 2.35, 2.7), (9.6, -0.5, 1.85, 2.25), (9.5, -8.2, 2.25, 2.4),
        (-6.0, -10.2, 1.95, 1.9),
    )):
        add_mesa(builder, audit, rng, f"desert_mesa_{index}", *values)
    for index, values in enumerate((
        (-3.9, 6.4, 0.90, 0.2), (-5.1, -1.6, 0.95, 0.1), (-8.6, 2.0, 1.05, 0.5), (6.2, 6.0, 1.05, 1.0),
        (5.8, -1.5, 0.9, 1.8), (-4.7, -7.8, 1.0, 2.4), (5.2, -8.6, 1.1, 0.8),
    )):
        add_cactus(builder, audit, f"desert_cactus_{index}", *values)
    for index, values in enumerate((
        (-3.5, 4.9, 1.0, 0.25), (2.5, -6.8, 1.1, 1.1), (-8.8, -2.0, 0.9, 2.0), (9.8, -3.2, 1.0, 2.6),
    )):
        add_dead_tree(builder, audit, f"desert_dead_tree_{index}", *values)
    return builder, {
        "biome": "desert_river_bridge",
        "path_shape": "S",
        "path_controls_unity_xz": path_controls,
        "river_controls_unity_xz": river_controls,
        "bridge_center_unity_xz": [3.6, 0.0],
        "water_surface_y": 1.248,
        "water_glint_geometry": "none; coplanar stripe removed",
        "visible_features": ["sand", "sandstone mesas", "blue river", "wood bridge bottleneck", "cacti", "dead trees", "S path"],
        "palette": {key: "#" + "".join(f"{round(channel * 255):02X}" for channel in value[:3]) for key, value in DESERT.items()},
        "clearance": {"minimum_raised_prop_clearance_m": round(audit.minimum, 3), "checks": audit.entries},
    }


def build_snow() -> Tuple[MeshBuilder, Dict[str, object]]:
    builder = MeshBuilder("terrain_snow")
    audit = ClearanceAudit()
    rng = random.Random(SEED + 29)
    ground_grid(builder, (SNOW["snow"], SNOW["snow_light"], SNOW["snow_shadow"]))

    river_controls = [(-15.0, -11.8), (-8.0, -11.3), (0.0, -11.7), (7.5, -11.2), (15.0, -11.65)]
    ribbon(builder, river_controls, 1.248, 3.0, (SNOW["ice_deep"], SNOW["ice"]), steps=4)

    # Put the split and merge below the question parchment in both aspect
    # ratios.  The two outer lanes avoid every pad and form a high-contrast Y
    # with a single tail to the ice river instead of reading as a broad U.
    trunk_in = [(5.0, 16.5), (5.0, 9.0), (0.0, 3.0)]
    left_branch = [(0.0, 3.0), (-4.2, 0.5), (-4.2, -2.5), (0.0, -4.0)]
    right_branch = [(0.0, 3.0), (3.3, 1.0), (3.2, -2.0), (0.0, -4.0)]
    trunk_out = [(0.0, -4.0), (-2.4, -6.8), (-4.0, -10.4)]
    for controls in (trunk_in, left_branch, right_branch, trunk_out):
        ribbon(builder, controls, 1.265, 1.72, (SNOW["path"], SNOW["path_light"]), steps=3)

    for index, values in enumerate((
        (-10.4, -5.8, 2.1, 2.15), (-10.7, 0.8, 2.35, 2.45), (-6.8, 12.6, 2.2, 2.5),
        (10.4, 11.2, 2.3, 2.65), (10.8, 0.0, 2.15, 2.25), (8.6, -7.0, 2.25, 2.4),
        (10.5, 5.6, 1.18, 1.5),
    )):
        add_snow_rock(builder, audit, rng, f"snow_rock_{index}", *values)
    for index, values in enumerate((
        (-10.4, 5.3, 0.88, 0.2), (-7.2, -2.2, 0.92, 0.15), (-10.0, 9.0, 1.12, 0.45), (-4.9, 11.0, 0.95, 0.9),
        (11.1, 5.0, 1.08, 1.4), (9.7, -4.2, 0.96, 2.0), (-6.8, -8.0, 1.05, 2.45),
        (5.8, -12.1, 1.18, 0.65), (11.2, -10.8, 0.9, 1.15),
    )):
        add_snow_pine(builder, audit, f"snow_pine_{index}", *values)
    for index, (x, z, yaw) in enumerate(((-11.8, -11.7, 0.2), (-6.3, -11.4, 0.8), (6.0, -11.8, 1.2), (11.0, -11.5, 0.45))):
        audit.raised(f"ice_chunk_{index}", x, z, 0.65)
        builder.box((x, 1.32, z), (1.0, 0.22, 0.72), SNOW["ice_light"], yaw, SNOW["snow_light"])
    add_snow_fortress(builder, audit, 3.6, 0.0)
    return builder, {
        "biome": "snow_fortress",
        "path_shape": "fork_merge",
        "path_controls_unity_xz": {
            "entry": trunk_in,
            "left_branch": left_branch,
            "right_branch": right_branch,
            "merged_exit": trunk_out,
        },
        "river_controls_unity_xz": river_controls,
        "fortress_center_unity_xz": [3.6, 0.0],
        "ice_surface_y": 1.248,
        "visible_features": ["snow ground", "snow-capped rocks", "snow-capped conifers", "ice river", "cold palette", "fork-merge path", "snow fortress with blue banners"],
        "palette": {key: "#" + "".join(f"{round(channel * 255):02X}" for channel in value[:3]) for key, value in SNOW.items()},
        "clearance": {"minimum_raised_prop_clearance_m": round(audit.minimum, 3), "checks": audit.entries},
    }


def clean_owned_scene() -> bpy.types.Collection:
    """Remove only this generator's objects/data, preserving a shared MCP scene."""
    for obj in list(bpy.data.objects):
        if obj.name.startswith(OWNED_PREFIX):
            bpy.data.objects.remove(obj, do_unlink=True)
    for mesh in list(bpy.data.meshes):
        if mesh.name.startswith(OWNED_PREFIX) and mesh.users == 0:
            bpy.data.meshes.remove(mesh)
    for material in list(bpy.data.materials):
        if material.name.startswith(OWNED_PREFIX) and material.users == 0:
            bpy.data.materials.remove(material)
    old = bpy.data.collections.get(COLLECTION_NAME)
    if old is not None:
        bpy.data.collections.remove(old)
    collection = bpy.data.collections.new(COLLECTION_NAME)
    bpy.context.scene.collection.children.link(collection)
    return collection


def semantic_hash(obj: bpy.types.Object) -> str:
    mesh = obj.data
    color = mesh.color_attributes["Color"]
    uv = mesh.uv_layers.active
    payload = {
        "vertices": [[round(value, 6) for value in vertex.co] for vertex in mesh.vertices],
        "triangles": [list(polygon.vertices) for polygon in mesh.polygons],
        "color": [[round(value, 6) for value in color.data[index].color_srgb] for index in range(len(color.data))],
        "uv0": [[round(value, 6) for value in uv.data[index].uv] for index in range(len(uv.data))],
        "material_slots": len(mesh.materials),
    }
    canonical = json.dumps(payload, sort_keys=True, separators=(",", ":"))
    return hashlib.sha256(canonical.encode("utf-8")).hexdigest()


def unity_bounds(obj: bpy.types.Object) -> Tuple[List[float], List[float]]:
    points = [(vertex.co.x, vertex.co.z, -vertex.co.y) for vertex in obj.data.vertices]
    minimum = [round(min(point[axis] for point in points), 4) for axis in range(3)]
    maximum = [round(max(point[axis] for point in points), 4) for axis in range(3)]
    return minimum, maximum


def export_asset(obj: bpy.types.Object, asset_id: str, ao_stats: Dict[str, object]) -> Dict[str, object]:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    target = OUTPUT / f"{asset_id}.fbx"
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=str(target), use_selection=True, object_types={"MESH"},
        global_scale=1.0, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        use_mesh_modifiers=True, mesh_smooth_type="OFF", use_tspace=False,
        add_leaf_bones=False, bake_anim=False, path_mode="AUTO",
        embed_textures=False, colors_type="SRGB", use_custom_props=False,
    )
    raw = target.read_bytes()
    triangles = len(obj.data.polygons)
    vertices = len(obj.data.vertices)
    if triangles > TRIANGLE_BUDGET:
        raise RuntimeError(f"{asset_id}: {triangles} triangles exceeds {TRIANGLE_BUDGET}")
    if vertices > VERTEX_BUDGET:
        raise RuntimeError(f"{asset_id}: {vertices} vertices exceeds {VERTEX_BUDGET}")
    if len(raw) > RAW_BUDGET_BYTES:
        raise RuntimeError(f"{asset_id}: {len(raw)} bytes exceeds {RAW_BUDGET_BYTES}")
    if len(obj.data.materials) != 1:
        raise RuntimeError(f"{asset_id}: expected exactly one material")
    minimum, maximum = unity_bounds(obj)
    return {
        "id": asset_id,
        "resource": f"HyeopgokSasu/Maps/{asset_id}",
        "fbx_bytes": len(raw),
        "fbx_gzip9_bytes": len(gzip.compress(raw, compresslevel=9, mtime=0)),
        "fbx_sha256": hashlib.sha256(raw).hexdigest(),
        "semantic_sha256": semantic_hash(obj),
        "triangles": triangles,
        "vertices": vertices,
        "meshes": 1,
        "materials": 1,
        "textures": 0,
        "bounds_unity_min": minimum,
        "bounds_unity_max": maximum,
        "vertex_color": "Color RGB = authored sRGB; alpha = baked AO",
        "uv0": "x = fixed-colour mask; y = baked encoding marker",
        "ao_bake": ao_stats,
    }


def read_previous() -> Dict[str, object]:
    if not REPORT.is_file():
        return {}
    try:
        return json.loads(REPORT.read_text())
    except (OSError, json.JSONDecodeError):
        return {}


def main() -> None:
    previous = read_previous()
    collection = clean_owned_scene()
    namespace = {"__file__": str(AO_SOURCE), "math": math, "Vector": Vector}
    exec(compile(AO_SOURCE.read_text(), str(AO_SOURCE), "exec"), namespace)
    bake_vertex_ao = namespace["bake_vertex_ao"]

    records = []
    map_metadata: Dict[str, object] = {}
    for builder, metadata in (build_desert(), build_snow()):
        obj = builder.object(collection)
        obj.data.update(calc_edges=True)
        ao_stats = bake_vertex_ao(obj)
        record = export_asset(obj, builder.name, ao_stats)
        records.append(record)
        map_metadata[builder.name] = metadata

    semantic = {record["id"]: record["semantic_sha256"] for record in records}
    binary = {record["id"]: record["fbx_sha256"] for record in records}
    old_determinism = previous.get("determinism", {}) if isinstance(previous, dict) else {}
    prior_semantic = previous.get("semantic_hashes", {}) if isinstance(previous, dict) else {}
    prior_binary = previous.get("fbx_hashes", {}) if isinstance(previous, dict) else {}
    semantic_match = bool(prior_semantic) and prior_semantic == semantic
    binary_match = bool(prior_binary) and prior_binary == binary
    run_fingerprint = hashlib.sha256(json.dumps(semantic, sort_keys=True, separators=(",", ":")).encode("utf-8")).hexdigest()

    report = {
        "schema": 1,
        "generator": "ArtSource/blender/v32_maps.py",
        "generator_sha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
        "seed": SEED,
        "contract": {
            "unity_axes": "+X right, +Y up, +Z forward; FBX exports -Z forward/+Y up at 1 metre scale",
            "single_mesh": True,
            "single_material": True,
            "texture_files": [],
            "raw_budget_each_bytes": RAW_BUDGET_BYTES,
            "triangle_budget_each": TRIANGLE_BUDGET,
            "vertex_budget_each": VERTEX_BUDGET,
            "owned_scene_prefix": OWNED_PREFIX,
            "owned_collection": COLLECTION_NAME,
        },
        "assets": records,
        "maps": map_metadata,
        "clear_zones_unity_xz_radius": [
            {"id": label, "x": x, "z": z, "radius": radius}
            for label, x, z, radius in CLEAR_ZONES
        ],
        "semantic_hashes": semantic,
        "fbx_hashes": binary,
        "run_fingerprint": run_fingerprint,
        "determinism": {
            "required_headless_runs": 2,
            "previous_run_fingerprint": previous.get("run_fingerprint", "") if isinstance(previous, dict) else "",
            "current_run_fingerprint": run_fingerprint,
            "previous_semantic_hashes": prior_semantic,
            "previous_fbx_hashes": prior_binary,
            "semantic_match": semantic_match,
            "fbx_byte_match": binary_match,
            # Blender writes volatile FBX header metadata.  Reproducibility is
            # therefore defined by the canonical payload above: positions,
            # triangle indices, Color/AO, UV0 and material-slot count.
            "verification_basis": "canonical mesh semantics (vertices, triangles, Color/AO, UV0, material slots)",
            "fbx_byte_stability_required": False,
            "verified": semantic_match,
            "prior_verified": bool(old_determinism.get("verified", False)) if isinstance(old_determinism, dict) else False,
        },
        "totals": {
            "fbx_bytes": sum(record["fbx_bytes"] for record in records),
            "fbx_gzip9_bytes": sum(record["fbx_gzip9_bytes"] for record in records),
            "triangles": sum(record["triangles"] for record in records),
            "vertices": sum(record["vertices"] for record in records),
            "textures": 0,
        },
    }
    REPORT.write_text(json.dumps(report, ensure_ascii=False, indent=2, sort_keys=True) + "\n")
    print("V32_MAPS_COMPLETE", json.dumps({
        "assets": [{key: item[key] for key in ("id", "fbx_bytes", "fbx_gzip9_bytes", "triangles", "vertices", "semantic_sha256")} for item in records],
        "determinism": report["determinism"],
    }, sort_keys=True))


if __name__ == "__main__":
    main()
