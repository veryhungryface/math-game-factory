#!/usr/bin/env python3
"""Install and validate the Art R4 AI3D assets for Hyeopgok Sasu.

The six Meshy-derived source FBX/PNG pairs below ``ArtSource/ai3d/out`` are
immutable inputs.  This script copies them into the game Resources folder,
cleans only the installed enemy texture, and creates two optional attachment
meshes in the same model-space coordinates as their parents:

* ``king_crown``: a bold five-point crown using the R4 gold palette;
* ``giant_blade_glow``: two crossed, additive-ready blade ribbons.

Unity attachment contract:

* import FBX with Unity's normal -Z-forward/+Y-up conversion;
* parent the attachment under the corresponding model root;
* use local position/rotation = zero and local scale = one;
* sample soldier PNG alpha as data (0 = team tint, 1 = authored fixed colour),
  never as transparency.

Run from the repository root::

    /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup \
      --python factory/unity-src/hyeopgok-sasu/ArtSource/blender/art_r4_ai3d.py

Use ``-- --verify-only`` to verify installed files without rewriting them and
``-- --preview-dir /tmp/hyeopgok-ai3d-r4`` for local visual proof renders.
No ``.meta`` files are written; the Unity track intentionally lets the warm
workspace generate and preserve them.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import shutil
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Sequence, Tuple

import bpy
from mathutils import Vector


SCRIPT = Path(__file__).resolve()
BLENDER_DIR = SCRIPT.parent
ART_SOURCE = BLENDER_DIR.parent
GAME_ROOT = ART_SOURCE.parent
SOURCE_DIR = ART_SOURCE / "ai3d/out"
TARGET_DIR = GAME_ROOT / "Resources/HyeopgokSasu/AI3D"
SOURCE_MANIFEST = SOURCE_DIR / "manifest.json"
REPORT_PATH = BLENDER_DIR / "art-r4-ai3d-report.json"
TARGET_MANIFEST = TARGET_DIR / "manifest.json"

ASSET_IDS = (
    "king",
    "ally_soldier",
    "enemy_soldier",
    "crossbow_tower",
    "barracks",
    "giant",
)

EXPECTED = {
    "king": {"triangles": 5500, "height": 1.3275, "texture": 512},
    "ally_soldier": {"triangles": 1300, "height": 0.78, "texture": 256},
    "enemy_soldier": {"triangles": 1300, "height": 1.09, "texture": 256},
    "crossbow_tower": {"triangles": 4468, "height": 2.05, "texture": 512},
    "barracks": {"triangles": 4407, "height": 1.845, "texture": 512},
    "giant": {"triangles": 3499, "height": 1.95, "texture": 512},
}

CROWN_GOLD = "#F2B705"
CROWN_HIGHLIGHT = "#FFD75A"
CROWN_SHADOW = "#A96500"


class PipelineError(RuntimeError):
    pass


def blender_argv() -> List[str]:
    return sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def parse_args(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Install Art R4 AI3D assets.")
    parser.add_argument(
        "--verify-only", action="store_true",
        help="verify installed assets without rewriting binaries",
    )
    parser.add_argument(
        "--preview-dir", type=Path,
        help="optional local preview directory (recommended below /tmp)",
    )
    return parser.parse_args(list(argv) if argv is not None else blender_argv())


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def round_vec(value: Iterable[float], digits: int = 6) -> List[float]:
    return [round(float(component), digits) for component in value]


def srgb_channel_to_linear(value: float) -> float:
    return value / 12.92 if value <= 0.04045 else ((value + 0.055) / 1.055) ** 2.4


def hex_linear(value: str, alpha: float = 1.0) -> Tuple[float, float, float, float]:
    value = value.lstrip("#")
    rgb = [int(value[index:index + 2], 16) / 255.0 for index in (0, 2, 4)]
    return tuple(srgb_channel_to_linear(channel) for channel in rgb) + (alpha,)


def reset_scene() -> None:
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def assert_inputs() -> Dict[str, str]:
    missing = []
    hashes: Dict[str, str] = {}
    for asset_id in ASSET_IDS:
        for suffix in ("fbx", "png"):
            path = SOURCE_DIR / f"{asset_id}.{suffix}"
            if not path.is_file():
                missing.append(str(path))
            else:
                hashes[path.name] = sha256(path)
    if not SOURCE_MANIFEST.is_file():
        missing.append(str(SOURCE_MANIFEST))
    if missing:
        raise PipelineError("missing immutable AI3D inputs: " + ", ".join(missing))
    return hashes


def source_manifest_contract() -> Dict[str, Dict[str, object]]:
    data = json.loads(SOURCE_MANIFEST.read_text(encoding="utf-8"))
    result = {}
    for item in data.get("assets", []):
        result[item["id"]] = {
            "fbx_sha256": item["files"]["fbx_sha256"],
            "png_sha256": item["texture"]["sha256"],
            "triangles": item["triangles"]["final_triangles"],
            "texture_size": [item["texture"]["width"], item["texture"]["height"]],
            "bounds_min": item["bounds_min"],
            "bounds_max": item["bounds_max"],
        }
    if set(result) != set(ASSET_IDS):
        raise PipelineError("source manifest does not contain exactly the six R4 assets")
    return result


def save_image(image: bpy.types.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.filepath_raw = str(path)
    image.file_format = "PNG"
    image.save()
    if not path.is_file() or path.stat().st_size == 0:
        raise PipelineError(f"failed to write PNG: {path}")


def create_texture(
    path: Path,
    width: int,
    height: int,
    pixel_fn,
    name: str,
) -> None:
    image = bpy.data.images.new(name, width=width, height=height, alpha=True)
    image.colorspace_settings.name = "sRGB"
    pixels: List[float] = []
    for y in range(height):
        v = (y + 0.5) / height
        for x in range(width):
            u = (x + 0.5) / width
            pixels.extend(pixel_fn(u, v))
    image.pixels[:] = pixels
    image.update()
    save_image(image, path)
    bpy.data.images.remove(image)


def clean_enemy_texture(source: Path, target: Path) -> Dict[str, object]:
    """Expand the enemy team mask over the blown-out white/red bake islands.

    Meshy's triangle-soup bake left bright fixed-colour islands between the red
    plates.  They read as white luminous blotches in the preview.  The cleanup
    is deliberately deterministic: hot neutral pixels and missed red/magenta
    team pixels become neutral luminance with alpha=0.  Dark metal, skin and
    spear pixels stay authored/fixed with alpha=1.
    """

    image = bpy.data.images.load(str(source), check_existing=False)
    image.colorspace_settings.name = "sRGB"
    width, height = (int(value) for value in image.size)
    pixels = list(image.pixels[:])
    before_team = 0
    added_hot_neutral = 0
    added_red = 0
    added_magenta = 0
    for index in range(0, len(pixels), 4):
        r, g, b, alpha = pixels[index:index + 4]
        if alpha < 0.5:
            before_team += 1
        maximum = max(r, g, b)
        minimum = min(r, g, b)
        magenta = r > 0.35 and b > 0.35 and g < min(r, b) * 0.55
        red = r > 0.24 and r > g * 1.28 and r > b * 1.12
        hot_neutral = maximum > 0.50 and maximum - minimum < 0.20
        make_team = alpha < 0.5 or magenta or red or hot_neutral
        if make_team:
            if alpha >= 0.5:
                if magenta:
                    added_magenta += 1
                elif red:
                    added_red += 1
                else:
                    added_hot_neutral += 1
            luminance = max(0.075, min(0.82, 0.2126 * r + 0.7152 * g + 0.0722 * b))
            pixels[index:index + 4] = [luminance, luminance, luminance, 0.0]
        else:
            pixels[index + 3] = 1.0
    image.pixels[:] = pixels
    image.update()
    save_image(image, target)
    after_team = sum(value < 0.5 for value in pixels[3::4])
    bpy.data.images.remove(image)
    return {
        "method": "deterministic hot-neutral/red/magenta expansion; RGB neutralized",
        "width": width,
        "height": height,
        "pixels": width * height,
        "team_pixels_before": before_team,
        "team_pixels_after": after_team,
        "team_ratio_before": round(before_team / (width * height), 6),
        "team_ratio_after": round(after_team / (width * height), 6),
        "added_hot_neutral": added_hot_neutral,
        "added_red": added_red,
        "added_magenta": added_magenta,
    }


def material(name: str, texture_name: str) -> bpy.types.Material:
    mat = bpy.data.materials.new(name)
    mat.diffuse_color = (1.0, 1.0, 1.0, 1.0)
    mat["r4_texture"] = texture_name
    return mat


def add_uv_and_color(
    mesh: bpy.types.Mesh,
    color: Tuple[float, float, float, float],
    uvs: Optional[Sequence[Tuple[float, float]]] = None,
) -> None:
    uv_layer = mesh.uv_layers.new(name="BaseColorUV")
    color_layer = mesh.color_attributes.new(name="Color", type="FLOAT_COLOR", domain="CORNER")
    if uvs is None:
        uvs = [(0.5, 0.5)] * len(mesh.loops)
    if len(uvs) != len(mesh.loops):
        raise PipelineError("UV loop count does not match mesh loops")
    for loop_index, uv in enumerate(uvs):
        uv_layer.data[loop_index].uv = uv
        color_layer.data[loop_index].color_srgb = color
    mesh.color_attributes.active_color = color_layer
    mesh.color_attributes.render_color_index = list(mesh.color_attributes).index(color_layer)


def make_crown() -> bpy.types.Object:
    """Create a chunky five-point crown in the king's original model space."""

    vertices: List[Tuple[float, float, float]] = []
    faces: List[Tuple[int, ...]] = []
    segments = 16
    center = Vector((0.005, -0.035, 0.0))
    outer_x, outer_y = 0.172, 0.143
    inner_x, inner_y = 0.126, 0.101
    bottom_z, top_z = 1.205, 1.278

    # Elliptical open band: outer/inner walls and top/bottom rims.
    for z in (bottom_z, top_z):
        for radius_x, radius_y in ((outer_x, outer_y), (inner_x, inner_y)):
            for index in range(segments):
                angle = 2 * math.pi * index / segments
                vertices.append((
                    center.x + math.cos(angle) * radius_x,
                    center.y + math.sin(angle) * radius_y,
                    z,
                ))
    outer_bottom = 0
    inner_bottom = segments
    outer_top = segments * 2
    inner_top = segments * 3
    for index in range(segments):
        nxt = (index + 1) % segments
        faces.extend([
            (outer_bottom + index, outer_bottom + nxt, outer_top + nxt, outer_top + index),
            (inner_bottom + nxt, inner_bottom + index, inner_top + index, inner_top + nxt),
            (outer_top + index, outer_top + nxt, inner_top + nxt, inner_top + index),
            (outer_bottom + nxt, outer_bottom + index, inner_bottom + index, inner_bottom + nxt),
        ])

    # Five broad wedge-shaped teeth across the front (-Y) arc.  The middle
    # tooth is tallest; all teeth are thick prisms rather than paper triangles.
    tooth_angles = (-142.0, -116.0, -90.0, -64.0, -38.0)
    tooth_heights = (0.125, 0.165, 0.205, 0.165, 0.125)
    for angle_degrees, height in zip(tooth_angles, tooth_heights):
        angle = math.radians(angle_degrees)
        radial = Vector((math.cos(angle), math.sin(angle), 0.0))
        tangent = Vector((-math.sin(angle), math.cos(angle), 0.0))
        base_center = Vector((
            center.x + radial.x * outer_x * 0.91,
            center.y + radial.y * outer_y * 0.91,
            top_z - 0.005,
        ))
        half_width = 0.038
        half_depth = 0.026
        base = len(vertices)
        for depth_sign in (-1.0, 1.0):
            for width_sign in (-1.0, 1.0):
                point = base_center + radial * (depth_sign * half_depth) + tangent * (width_sign * half_width)
                vertices.append((point.x, point.y, point.z))
        tip_center = base_center + radial * 0.010 + Vector((0.0, 0.0, height))
        tip_a = tip_center - radial * 0.014
        tip_b = tip_center + radial * 0.014
        vertices.extend([(tip_a.x, tip_a.y, tip_a.z), (tip_b.x, tip_b.y, tip_b.z)])
        # bottom indices are depth-major/width-minor: 0,1,2,3; tips 4,5.
        faces.extend([
            (base + 0, base + 2, base + 3, base + 1),
            (base + 0, base + 1, base + 4),
            (base + 2, base + 5, base + 3),
            (base + 0, base + 4, base + 5, base + 2),
            (base + 1, base + 3, base + 5, base + 4),
        ])

    mesh = bpy.data.meshes.new("king_crown")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(material("AI3D_KingCrown_Gold", "king_crown.png"))
    mesh.validate(verbose=False, clean_customdata=False)
    mesh.update(calc_edges=True)
    gold = hex_linear(CROWN_GOLD)
    add_uv_and_color(mesh, gold)
    obj = bpy.data.objects.new("king_crown", mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def make_blade_glow() -> bpy.types.Object:
    """Create two crossed additive-ready ribbons over the giant sword groove."""

    tip = Vector((-1.035, -0.655, 0.180))
    base = Vector((-0.605, -0.105, 0.682))
    direction = (base - tip).normalized()
    side_xz = Vector((-direction.z, 0.0, direction.x)).normalized() * 0.034
    side_y = Vector((0.0, 0.029, 0.0))
    vertices: List[Tuple[float, float, float]] = []
    faces: List[Tuple[int, ...]] = []
    uvs: List[Tuple[float, float]] = []
    # Slightly inset the visible ribbon from both ends of the groove.
    start = tip + direction * 0.045
    end = base - direction * 0.035
    for side in (side_xz, side_y):
        offset = len(vertices)
        points = (start - side, start + side, end + side, end - side)
        vertices.extend(tuple(point) for point in points)
        faces.append((offset, offset + 1, offset + 2, offset + 3))
        uvs.extend(((0.0, 0.0), (0.0, 1.0), (1.0, 1.0), (1.0, 0.0)))
    mesh = bpy.data.meshes.new("giant_blade_glow")
    mesh.from_pydata(vertices, [], faces)
    glow_material = material("AI3D_GiantBladeGlow_Additive", "giant_blade_glow.png")
    glow_material["r4_blend"] = "additive"
    glow_material["r4_emission_hex"] = "#FF4A19"
    mesh.materials.append(glow_material)
    mesh.validate(verbose=False, clean_customdata=False)
    mesh.update(calc_edges=True)
    add_uv_and_color(mesh, hex_linear("#FF6A20", 0.92), uvs)
    obj = bpy.data.objects.new("giant_blade_glow", mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def export_fbx(objects: Sequence[bpy.types.Object], path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
        obj.location = (0.0, 0.0, 0.0)
        obj.rotation_euler = (0.0, 0.0, 0.0)
        obj.scale = (1.0, 1.0, 1.0)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=str(path), use_selection=True, object_types={"MESH"},
        global_scale=1.0, apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        use_mesh_modifiers=True, mesh_smooth_type="FACE", use_tspace=False,
        add_leaf_bones=False, bake_anim=False, path_mode="RELATIVE",
        embed_textures=False, colors_type="SRGB", use_custom_props=True,
    )
    if not path.is_file() or path.stat().st_size == 0:
        raise PipelineError(f"failed to write FBX: {path}")


def install_assets() -> Dict[str, object]:
    TARGET_DIR.mkdir(parents=True, exist_ok=True)
    for asset_id in ASSET_IDS:
        shutil.copy2(SOURCE_DIR / f"{asset_id}.fbx", TARGET_DIR / f"{asset_id}.fbx")
        if asset_id != "enemy_soldier":
            shutil.copy2(SOURCE_DIR / f"{asset_id}.png", TARGET_DIR / f"{asset_id}.png")
    enemy_cleanup = clean_enemy_texture(
        SOURCE_DIR / "enemy_soldier.png", TARGET_DIR / "enemy_soldier.png"
    )

    reset_scene()
    crown = make_crown()
    export_fbx([crown], TARGET_DIR / "king_crown.fbx")

    crown_shadow = hex_linear(CROWN_SHADOW)
    crown_gold = hex_linear(CROWN_GOLD)
    crown_highlight = hex_linear(CROWN_HIGHLIGHT)

    def crown_pixel(u: float, v: float) -> Tuple[float, float, float, float]:
        # Keep the atlas unmistakably in the requested #F2B705 family.  The
        # lower third supplies chunky amber form shadow; the narrow upper band
        # adds a warm highlight without washing the crown toward white.
        if v < 0.34:
            blend = v / 0.34
            rgb = tuple(
                crown_shadow[index] * (1.0 - blend) + crown_gold[index] * blend
                for index in range(3)
            )
        else:
            blend = min(1.0, (v - 0.34) / 0.66) * 0.42
            rgb = tuple(
                crown_gold[index] * (1.0 - blend) + crown_highlight[index] * blend
                for index in range(3)
            )
        # Gentle horizontal modulation preserves a low-poly read in unlit use.
        modulation = 0.94 + 0.06 * max(0.0, math.sin(math.pi * u))
        return tuple(channel * modulation for channel in rgb) + (1.0,)

    create_texture(
        TARGET_DIR / "king_crown.png", 64, 64,
        crown_pixel,
        "king_crown_texture",
    )

    reset_scene()
    glow = make_blade_glow()
    export_fbx([glow], TARGET_DIR / "giant_blade_glow.fbx")

    edge_rgb = hex_linear("#FF2812")
    core_rgb = hex_linear("#FFD45A")

    def glow_pixel(u: float, v: float) -> Tuple[float, float, float, float]:
        across = max(0.0, math.sin(math.pi * v)) ** 2.2
        along = 0.30 + 0.70 * max(0.0, math.sin(math.pi * u)) ** 0.45
        core = across ** 2.0
        rgb = tuple(edge_rgb[i] * (1.0 - core) + core_rgb[i] * core for i in range(3))
        return rgb + (across * along,)

    create_texture(
        TARGET_DIR / "giant_blade_glow.png", 128, 32,
        glow_pixel, "giant_blade_glow_texture",
    )
    return {"enemy_cleanup": enemy_cleanup}


def import_meshes(path: Path) -> List[bpy.types.Object]:
    reset_scene()
    bpy.ops.import_scene.fbx(filepath=str(path), use_custom_normals=True)
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def fbx_stats(path: Path) -> Dict[str, object]:
    objects = import_meshes(path)
    if not objects:
        raise PipelineError(f"FBX has no mesh objects: {path}")
    triangle_count = 0
    logical_points: List[Tuple[float, float, float]] = []
    uv_layers = 0
    color_layers = set()
    material_slots = 0
    transforms = []
    for obj in objects:
        mesh = obj.data
        mesh.calc_loop_triangles()
        triangle_count += len(mesh.loop_triangles)
        uv_layers = max(uv_layers, len(mesh.uv_layers))
        color_layers.update(layer.name for layer in mesh.color_attributes)
        material_slots += len(mesh.materials)
        transforms.append({
            "name": obj.name,
            "location": round_vec(obj.location),
            "rotation_euler": round_vec(obj.rotation_euler),
            "scale": round_vec(obj.scale),
        })
        for vertex in mesh.vertices:
            point = obj.matrix_world @ vertex.co
            logical_points.append((point.x, point.z, -point.y))
    minimum = [min(point[axis] for point in logical_points) for axis in range(3)]
    maximum = [max(point[axis] for point in logical_points) for axis in range(3)]
    return {
        "bytes": path.stat().st_size,
        "sha256": sha256(path),
        "mesh_objects": len(objects),
        "triangles": triangle_count,
        "uv_layers": uv_layers,
        "color_layers": sorted(color_layers),
        "material_slots": material_slots,
        "unity_logical_bounds_min": round_vec(minimum),
        "unity_logical_bounds_max": round_vec(maximum),
        "unity_logical_height": round(maximum[1] - minimum[1], 6),
        "imported_transforms": transforms,
    }


def png_stats(path: Path) -> Dict[str, object]:
    image = bpy.data.images.load(str(path), check_existing=False)
    try:
        width, height = (int(value) for value in image.size)
        alpha = list(image.pixels[3::4])
        return {
            "bytes": path.stat().st_size,
            "sha256": sha256(path),
            "width": width,
            "height": height,
            "channels": int(image.channels),
            "alpha_min": round(min(alpha), 6),
            "alpha_max": round(max(alpha), 6),
            "alpha_lt_half": sum(value < 0.5 for value in alpha),
        }
    finally:
        bpy.data.images.remove(image)


def verify(source_hashes: Dict[str, str], install_meta: Dict[str, object]) -> Dict[str, object]:
    errors: List[str] = []
    source_contract = source_manifest_contract()
    assets = []
    for asset_id in ASSET_IDS:
        fbx_path = TARGET_DIR / f"{asset_id}.fbx"
        png_path = TARGET_DIR / f"{asset_id}.png"
        if not fbx_path.is_file() or not png_path.is_file():
            errors.append(f"missing installed pair: {asset_id}")
            continue
        fbx = fbx_stats(fbx_path)
        png = png_stats(png_path)
        expected = EXPECTED[asset_id]
        if fbx["triangles"] != expected["triangles"]:
            errors.append(f"{asset_id}: triangles {fbx['triangles']} != {expected['triangles']}")
        if abs(fbx["unity_logical_height"] - expected["height"]) > 0.006:
            errors.append(
                f"{asset_id}: height {fbx['unity_logical_height']} != {expected['height']}"
            )
        if (png["width"], png["height"]) != (expected["texture"], expected["texture"]):
            errors.append(f"{asset_id}: unexpected texture dimensions")
        if fbx["sha256"] != source_contract[asset_id]["fbx_sha256"]:
            errors.append(f"{asset_id}: installed FBX is not byte-identical to immutable source")
        if asset_id != "enemy_soldier" and png["sha256"] != source_contract[asset_id]["png_sha256"]:
            errors.append(f"{asset_id}: installed PNG is not byte-identical to immutable source")
        if asset_id == "enemy_soldier" and png["sha256"] == source_contract[asset_id]["png_sha256"]:
            errors.append("enemy_soldier: cleanup did not change installed texture")
        assets.append({"id": asset_id, "fbx": fbx, "png": png})

    attachments = []
    for attachment_id, texture_size, max_triangles in (
        ("king_crown", (64, 64), 256),
        ("giant_blade_glow", (128, 32), 8),
    ):
        fbx_path = TARGET_DIR / f"{attachment_id}.fbx"
        png_path = TARGET_DIR / f"{attachment_id}.png"
        if not fbx_path.is_file() or not png_path.is_file():
            errors.append(f"missing attachment pair: {attachment_id}")
            continue
        fbx = fbx_stats(fbx_path)
        png = png_stats(png_path)
        if fbx["triangles"] > max_triangles:
            errors.append(f"{attachment_id}: triangles {fbx['triangles']} > {max_triangles}")
        if (png["width"], png["height"]) != texture_size:
            errors.append(f"{attachment_id}: unexpected texture dimensions")
        attachments.append({"id": attachment_id, "fbx": fbx, "png": png})

    # Immutable inputs must remain byte-identical after every run.
    after_hashes = {
        path.name: sha256(path)
        for asset_id in ASSET_IDS
        for path in (SOURCE_DIR / f"{asset_id}.fbx", SOURCE_DIR / f"{asset_id}.png")
    }
    if after_hashes != source_hashes:
        errors.append("immutable ArtSource/ai3d/out inputs changed during installation")

    total_bytes = sum(
        path.stat().st_size for path in TARGET_DIR.iterdir()
        if path.is_file() and path.suffix.lower() in {".fbx", ".png"}
    )
    report = {
        "schema_version": 1,
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "generator": str(SCRIPT.relative_to(SCRIPT.parents[5])),
        "blender_version": bpy.app.version_string,
        "scope": "Art R4 AI 3D asset installation only; no runtime code or public build",
        "source": {
            "concept_generator": "OpenAI built-in image generation, 2026-09-27 KST",
            "mesh_generator": "Meshy 7.1 Image-to-3D plus Remesh",
            "original_design": True,
            "reference_use": "Kingshot captures informed broad proportions/style only; no asset, emblem, costume, or scene was copied",
            "third_party_game_asset_files": 0,
            "credit_record": "ArtSource/ai3d/meshy-session.json",
            "concept_provenance": "ArtSource/ai3d/concepts/README.md",
            "immutable_input_hashes": source_hashes,
            "immutable_inputs_preserved": after_hashes == source_hashes,
        },
        "contracts": {
            "fbx": "metre; Blender -Z forward/+Y up export; Unity logical x-right/y-up/z-forward",
            "attachment_transform": "parent under matching model at local position (0,0,0), rotation (0,0,0), scale (1,1,1)",
            "soldier_texture_alpha": "data mask: 0=team tintable, 1=fixed authored colour; never opacity",
            "crown": f"separate opaque attachment, dominant gold {CROWN_GOLD}",
            "giant_glow": "separate crossed ribbons; transparent texture intended for additive unlit rendering, Cull Off, ZWrite Off",
            "unity_meta": "not committed by unity-track contract; warm workspace generates/preserves .meta files",
        },
        "enemy_cleanup": install_meta.get("enemy_cleanup"),
        "assets": assets,
        "attachments": attachments,
        "total_installed_binary_bytes": total_bytes,
        "validation": {"passed": not errors, "errors": errors},
    }
    return report


def configure_preview_material(
    obj: bpy.types.Object,
    texture_path: Path,
    team_color: Optional[str] = None,
    emission: bool = False,
) -> None:
    image = bpy.data.images.load(str(texture_path), check_existing=False)
    image.colorspace_settings.name = "sRGB"
    mat = bpy.data.materials.new(f"preview_{obj.name}")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    links = mat.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    texture = nodes.new("ShaderNodeTexImage")
    texture.image = image
    if emission:
        shader = nodes.new("ShaderNodeEmission")
        links.new(texture.outputs["Color"], shader.inputs["Color"])
        links.new(texture.outputs["Alpha"], shader.inputs["Strength"])
        links.new(shader.outputs["Emission"], output.inputs["Surface"])
    else:
        shader = nodes.new("ShaderNodeBsdfPrincipled")
        if team_color:
            team = nodes.new("ShaderNodeMixRGB")
            team.blend_type = "MULTIPLY"
            team.inputs[0].default_value = 1.0
            team.inputs[2].default_value = hex_linear(team_color)
            links.new(texture.outputs["Color"], team.inputs[1])
            fixed = nodes.new("ShaderNodeMixRGB")
            links.new(texture.outputs["Alpha"], fixed.inputs[0])
            links.new(team.outputs[0], fixed.inputs[1])
            links.new(texture.outputs["Color"], fixed.inputs[2])
            links.new(fixed.outputs[0], shader.inputs["Base Color"])
        else:
            links.new(texture.outputs["Color"], shader.inputs["Base Color"])
        shader.inputs["Roughness"].default_value = 0.72
        links.new(shader.outputs["BSDF"], output.inputs["Surface"])
    obj.data.materials.clear()
    obj.data.materials.append(mat)


def look_at(obj: bpy.types.Object, target: Vector) -> None:
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def studio_render(path: Path, focus: Vector, extent: float) -> None:
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 768
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.world.color = (0.88, 0.91, 0.94)
    camera_data = bpy.data.cameras.new("PreviewCamera")
    camera = bpy.data.objects.new("PreviewCamera", camera_data)
    bpy.context.collection.objects.link(camera)
    scene.camera = camera
    camera.data.lens = 52
    camera.location = focus + Vector((extent * 1.6, -extent * 2.7, extent * 1.25))
    look_at(camera, focus)
    light_data = bpy.data.lights.new("PreviewKey", "AREA")
    light_data.energy = 1050
    light_data.shape = "DISK"
    light_data.size = extent * 2.0
    light = bpy.data.objects.new("PreviewKey", light_data)
    bpy.context.collection.objects.link(light)
    light.location = focus + Vector((-extent, -extent * 1.7, extent * 2.6))
    look_at(light, focus)
    path.parent.mkdir(parents=True, exist_ok=True)
    scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)


def render_previews(preview_dir: Path) -> None:
    preview_dir = preview_dir.expanduser().resolve()
    preview_dir.mkdir(parents=True, exist_ok=True)

    # King plus crown, both aligned at their exported zero transforms.
    reset_scene()
    bpy.ops.import_scene.fbx(filepath=str(TARGET_DIR / "king.fbx"), use_custom_normals=True)
    king = max((o for o in bpy.context.scene.objects if o.type == "MESH"), key=lambda o: len(o.data.vertices))
    configure_preview_material(king, TARGET_DIR / "king.png")
    bpy.ops.import_scene.fbx(filepath=str(TARGET_DIR / "king_crown.fbx"), use_custom_normals=True)
    crown = min((o for o in bpy.context.scene.objects if o.type == "MESH"), key=lambda o: len(o.data.vertices))
    configure_preview_material(crown, TARGET_DIR / "king_crown.png")
    studio_render(preview_dir / "king-crowned.png", Vector((0.0, 0.0, 0.74)), 1.05)

    reset_scene()
    bpy.ops.import_scene.fbx(filepath=str(TARGET_DIR / "enemy_soldier.fbx"), use_custom_normals=True)
    enemy = max((o for o in bpy.context.scene.objects if o.type == "MESH"), key=lambda o: len(o.data.vertices))
    configure_preview_material(enemy, TARGET_DIR / "enemy_soldier.png", team_color="#D0060C")
    studio_render(preview_dir / "enemy-clean.png", Vector((0.0, 0.0, 0.52)), 0.82)

    reset_scene()
    bpy.ops.import_scene.fbx(filepath=str(TARGET_DIR / "giant.fbx"), use_custom_normals=True)
    giant = max((o for o in bpy.context.scene.objects if o.type == "MESH"), key=lambda o: len(o.data.vertices))
    configure_preview_material(giant, TARGET_DIR / "giant.png")
    bpy.ops.import_scene.fbx(filepath=str(TARGET_DIR / "giant_blade_glow.fbx"), use_custom_normals=True)
    glow = min((o for o in bpy.context.scene.objects if o.type == "MESH"), key=lambda o: len(o.data.vertices))
    configure_preview_material(glow, TARGET_DIR / "giant_blade_glow.png", emission=True)
    studio_render(preview_dir / "giant-glow.png", Vector((0.0, 0.0, 0.88)), 1.55)


def write_report(report: Dict[str, object]) -> None:
    text = json.dumps(report, ensure_ascii=False, indent=2) + "\n"
    TARGET_MANIFEST.write_text(text, encoding="utf-8")
    REPORT_PATH.write_text(text, encoding="utf-8")


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    source_hashes = assert_inputs()
    install_meta: Dict[str, object] = {}
    if not args.verify_only:
        install_meta = install_assets()
    elif REPORT_PATH.is_file():
        previous = json.loads(REPORT_PATH.read_text(encoding="utf-8"))
        if previous.get("enemy_cleanup"):
            install_meta["enemy_cleanup"] = previous["enemy_cleanup"]
    report = verify(source_hashes, install_meta)
    write_report(report)
    if args.preview_dir:
        render_previews(args.preview_dir)
    validation = report["validation"]
    print(
        "ART_R4_AI3D_RESULT " + json.dumps({
            "passed": validation["passed"],
            "errors": validation["errors"],
            "assets": len(report["assets"]),
            "attachments": len(report["attachments"]),
            "bytes": report["total_installed_binary_bytes"],
            "report": str(REPORT_PATH),
        }, ensure_ascii=False),
        flush=True,
    )
    if not validation["passed"]:
        raise PipelineError("; ".join(validation["errors"]))
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:
        print(f"ART_R4_AI3D_ERROR {type(exc).__name__}: {exc}", file=sys.stderr, flush=True)
        raise
