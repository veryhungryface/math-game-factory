#!/usr/bin/env python3
"""Install and validate the Art R4 AI3D assets for Hyeopgok Sasu.

The six Meshy-derived source FBX/PNG pairs below ``ArtSource/ai3d/out`` are
immutable inputs.  This script copies them into the game Resources folder,
cleans the installed soldier team masks, creates two optional attachment
meshes in the same model-space coordinates as their parents, and derives nine
upgrade-ready tower meshes from the AI crossbow tower:

* ``king_crown``: a bold five-point crown using the R4 gold palette;
* ``giant_blade_glow``: two crossed, additive-ready blade ribbons.
* ``tower_{crossbow,cannon,magic}_lv{1,2,3}``: one-mesh/one-material towers.

The three tower levels use deliberately different architectural bodies:
an open timber watchtower, an AI-authored stone/blue-cloth keep, and a broad
crenellated gold-trimmed fortress with standards and bright rune stones.  The
weapon head still identifies crossbow/cannon/magic at every level.  Authored
height is normalised because runtime applies the exact 1.0/1.2/1.44 level
scale.  All nine tower FBXs share ``tower_shared.png``; no level duplicates
the 512px atlas.

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
``-- --soldier-textures-only`` to reproducibly refresh just the two installed
team-mask textures and their report, or
``-- --towers-only --determinism-check`` to rebuild only the nine tower FBXs,
or
``-- --preview-dir /tmp/hyeopgok-ai3d-r4`` for local visual proof renders.
No ``.meta`` files are written; the Unity track intentionally lets the warm
workspace generate and preserve them.
"""

from __future__ import annotations

import argparse
import bmesh
import gzip
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

TOWER_TYPES = ("crossbow", "cannon", "magic")
TOWER_LEVELS = (1, 2, 3)
TOWER_IDS = tuple(
    f"tower_{tower_type}_lv{level}"
    for tower_type in TOWER_TYPES
    for level in TOWER_LEVELS
)
TOWER_TEXTURE_ID = "tower_shared"
TOWER_ATLAS_SIZE = 512
TOWER_CONTENT_SIZE = 480
TOWER_AUTHORED_HEIGHT = 2.10
TOWER_RUNTIME_SCALE = (1.0, 1.2, 1.44)
TOWER_BODY_STYLE = {
    1: "open_timber_watchtower",
    2: "stone_keep_blue_cloth",
    3: "crenellated_gold_rune_fortress",
}

# The top 32px of tower_shared.png is a deterministic 16-swatch strip.  The
# AI atlas is resampled into the lower-left 480px square and its UVs remapped;
# generated pieces point at a swatch centre.  This lets nine models retain the
# detailed AI base while importing exactly one texture.
TOWER_SWATCHES = {
    "stone": "#C9CCD1",
    "stone_dark": "#596371",
    "wood": "#87542F",
    "wood_light": "#B97A45",
    "blue": "#0C73D5",
    "blue_dark": "#0658C7",
    "gold": "#F2B705",
    "gold_dark": "#A96500",
    "iron": "#303844",
    "iron_light": "#667181",
    "bronze": "#A65D2C",
    "muzzle": "#1B222B",
    "magic_cyan": "#38DFF5",
    "magic_blue": "#246DEB",
    "magic_violet": "#7B4DE2",
    "magic_core": "#E7FCFF",
}

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
        "--soldier-textures-only", action="store_true",
        help="refresh only installed ally/enemy mask textures and their report",
    )
    parser.add_argument(
        "--towers-only", action="store_true",
        help="rebuild only the nine tower FBXs and report/manifest",
    )
    parser.add_argument(
        "--preview-dir", type=Path,
        help="optional local preview directory (recommended below /tmp)",
    )
    parser.add_argument(
        "--determinism-check", action="store_true",
        help=(
            "generate twice and require identical imported FBX geometry/material/bounds "
            "plus byte-identical generated PNGs"
        ),
    )
    return parser.parse_args(list(argv) if argv is not None else blender_argv())


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def target_binary_hashes() -> Dict[str, str]:
    if not TARGET_DIR.is_dir():
        return {}
    return {
        path.name: sha256(path)
        for path in sorted(TARGET_DIR.iterdir())
        if path.is_file() and path.suffix.lower() in {".fbx", ".png"}
    }


GENERATED_FBX_IDS = ("king_crown", "giant_blade_glow") + TOWER_IDS
GENERATED_PNG_IDS = (
    "ally_soldier",
    "enemy_soldier",
    "king_crown",
    "giant_blade_glow",
    TOWER_TEXTURE_ID,
)


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
    # Clear unused material/image datablocks too.  Without this, consecutive
    # headless builds export ``AI3D_TowerShared.001``-style suffixes even
    # though the intended shared material name is identical.
    for datablocks in (
        bpy.data.meshes,
        bpy.data.curves,
        bpy.data.cameras,
        bpy.data.lights,
        bpy.data.materials,
        bpy.data.images,
    ):
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


def clean_ally_texture(source: Path, target: Path) -> Dict[str, object]:
    """Expand the ally mask over cobalt and blown-out armour bake islands.

    The source mask covers only a few isolated blue facets, so the army reads
    white at gameplay scale.  Cool blue/cyan/magenta pixels and bright neutral
    armour become team-tintable luminance.  Warm skin/leather/gold and dark
    neutral metal stay authored with alpha=1, preserving the face and weapon.
    """

    image = bpy.data.images.load(str(source), check_existing=False)
    image.colorspace_settings.name = "sRGB"
    width, height = (int(value) for value in image.size)
    pixels = list(image.pixels[:])
    before_team = 0
    added_blue = 0
    added_cyan = 0
    added_cool_magenta = 0
    added_bright_neutral = 0
    for index in range(0, len(pixels), 4):
        r, g, b, alpha = pixels[index:index + 4]
        if alpha < 0.5:
            before_team += 1
        maximum = max(r, g, b)
        minimum = min(r, g, b)
        blue = b > 0.18 and b > r * 1.12 and b > g * 1.05
        cyan = b > 0.25 and g > 0.18 and r < min(g, b) * 0.70
        cool_magenta = (
            b > 0.28 and r > 0.22 and g < min(r, b) * 0.55
            and b >= r * 0.75
        )
        warm_authored = r > g * 1.06 and g > b * 1.04
        bright_neutral = (
            maximum > 0.50 and maximum - minimum < 0.18
            and not warm_authored
        )
        make_team = (
            alpha < 0.5 or blue or cyan or cool_magenta or bright_neutral
        )
        if make_team:
            if alpha >= 0.5:
                if blue:
                    added_blue += 1
                elif cyan:
                    added_cyan += 1
                elif cool_magenta:
                    added_cool_magenta += 1
                else:
                    added_bright_neutral += 1
            luminance = max(
                0.075,
                min(0.82, 0.2126 * r + 0.7152 * g + 0.0722 * b),
            )
            pixels[index:index + 4] = [luminance, luminance, luminance, 0.0]
        else:
            pixels[index + 3] = 1.0
    image.pixels[:] = pixels
    image.update()
    save_image(image, target)
    after_team = sum(value < 0.5 for value in pixels[3::4])
    bpy.data.images.remove(image)
    return {
        "method": (
            "deterministic blue/cyan/cool-magenta/bright-neutral expansion; "
            "warm skin and dark metal retained; RGB neutralized"
        ),
        "width": width,
        "height": height,
        "pixels": width * height,
        "team_pixels_before": before_team,
        "team_pixels_after": after_team,
        "team_ratio_before": round(before_team / (width * height), 6),
        "team_ratio_after": round(after_team / (width * height), 6),
        "added_blue": added_blue,
        "added_cyan": added_cyan,
        "added_cool_magenta": added_cool_magenta,
        "added_bright_neutral": added_bright_neutral,
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
    # Broad enough to survive the gameplay camera's small on-screen sword,
    # while remaining inside the original giant mesh bounds.
    side_xz = Vector((-direction.z, 0.0, direction.x)).normalized() * 0.052
    side_y = Vector((0.0, 0.044, 0.0))
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


def create_tower_atlas(source: Path, target: Path) -> Dict[str, object]:
    """Pack the AI tower atlas and generated-piece swatches into one 512 map."""

    image = bpy.data.images.load(str(source), check_existing=False)
    image.colorspace_settings.name = "sRGB"
    width, height = (int(value) for value in image.size)
    if (width, height) != (512, 512):
        bpy.data.images.remove(image)
        raise PipelineError(f"tower source atlas must be 512x512, got {width}x{height}")
    source_pixels = list(image.pixels[:])
    bpy.data.images.remove(image)

    size = TOWER_ATLAS_SIZE
    content = TOWER_CONTENT_SIZE
    background = hex_linear("#23272D")
    pixels = list(background) * (size * size)
    # Nearest-neighbour resampling preserves the intentionally faceted AI bake.
    for y in range(content):
        source_y = min(height - 1, int(round(y * (height - 1) / (content - 1))))
        for x in range(content):
            source_x = min(width - 1, int(round(x * (width - 1) / (content - 1))))
            source_index = (source_y * width + source_x) * 4
            target_index = (y * size + x) * 4
            pixels[target_index:target_index + 4] = source_pixels[source_index:source_index + 4]

    names = list(TOWER_SWATCHES)
    swatch_width = size // len(names)
    for slot, name in enumerate(names):
        rgba = hex_linear(TOWER_SWATCHES[name])
        start_x = slot * swatch_width
        end_x = size if slot == len(names) - 1 else (slot + 1) * swatch_width
        for y in range(content, size):
            for x in range(start_x, end_x):
                index = (y * size + x) * 4
                pixels[index:index + 4] = rgba
    # The unused right strip is a neutral stone ramp, so bilinear samples at
    # the packed AI atlas edge never pull transparent or magenta pixels.
    stone = hex_linear(TOWER_SWATCHES["stone"])
    stone_dark = hex_linear(TOWER_SWATCHES["stone_dark"])
    for y in range(content):
        blend = y / max(1, content - 1)
        rgba = tuple(
            stone_dark[channel] * (1.0 - blend) + stone[channel] * blend
            for channel in range(3)
        ) + (1.0,)
        for x in range(content, size):
            index = (y * size + x) * 4
            pixels[index:index + 4] = rgba

    atlas = bpy.data.images.new(
        "tower_shared", width=size, height=size, alpha=True
    )
    atlas.colorspace_settings.name = "sRGB"
    atlas.pixels[:] = pixels
    atlas.update()
    save_image(atlas, target)
    bpy.data.images.remove(atlas)
    return {
        "source": source.name,
        "source_size": [width, height],
        "packed_content_size": [content, content],
        "atlas_size": [size, size],
        "swatch_count": len(names),
        "swatches": TOWER_SWATCHES,
        "level_texture_duplicates": 0,
    }


def tower_swatch_uv(name: str) -> Tuple[float, float]:
    names = list(TOWER_SWATCHES)
    slot = names.index(name)
    swatch_width = TOWER_ATLAS_SIZE / len(names)
    return (
        (slot * swatch_width + swatch_width * 0.5) / TOWER_ATLAS_SIZE,
        (TOWER_CONTENT_SIZE + (TOWER_ATLAS_SIZE - TOWER_CONTENT_SIZE) * 0.5)
        / TOWER_ATLAS_SIZE,
    )


def active_object(obj: bpy.types.Object) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def apply_object_transform(obj: bpy.types.Object) -> None:
    active_object(obj)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)


def tower_material() -> bpy.types.Material:
    mat = material("AI3D_TowerShared", f"{TOWER_TEXTURE_ID}.png")
    mat["r4_shared_texture_id"] = TOWER_TEXTURE_ID
    mat["r4_level_texture_duplicates"] = 0
    return mat


def set_single_material(obj: bpy.types.Object, mat: bpy.types.Material) -> None:
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    for polygon in obj.data.polygons:
        polygon.material_index = 0


def set_generated_surface(
    obj: bpy.types.Object,
    mat: bpy.types.Material,
    swatch: str,
    ao: float = 0.94,
) -> None:
    mesh = obj.data
    while len(mesh.uv_layers):
        mesh.uv_layers.remove(mesh.uv_layers[0])
    uv_layer = mesh.uv_layers.new(name="BaseColorUV")
    uv = tower_swatch_uv(swatch)
    for loop in uv_layer.data:
        loop.uv = uv
    while len(mesh.color_attributes):
        mesh.color_attributes.remove(mesh.color_attributes[0])
    colors = mesh.color_attributes.new(
        name="Color", type="BYTE_COLOR", domain="CORNER"
    )
    rgba = (1.0, 1.0, 1.0, max(0.0, min(1.0, ao)))
    for entry in colors.data:
        entry.color_srgb = rgba
    mesh.color_attributes.active_color = colors
    mesh.color_attributes.render_color_index = 0
    set_single_material(obj, mat)


def add_box(
    name: str,
    location: Tuple[float, float, float],
    dimensions: Tuple[float, float, float],
    swatch: str,
    mat: bpy.types.Material,
    rotation: Tuple[float, float, float] = (0.0, 0.0, 0.0),
    bevel: float = 0.04,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    apply_object_transform(obj)
    if bevel > 0.0:
        modifier = obj.modifiers.new("low_poly_bevel", "BEVEL")
        modifier.width = min(bevel, min(dimensions) * 0.24)
        modifier.segments = 1
        modifier.limit_method = "ANGLE"
        active_object(obj)
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    set_generated_surface(obj, mat, swatch)
    return obj


def add_cylinder(
    name: str,
    location: Tuple[float, float, float],
    radius: float,
    depth: float,
    swatch: str,
    mat: bpy.types.Material,
    vertices: int = 12,
    rotation: Tuple[float, float, float] = (0.0, 0.0, 0.0),
    bevel: float = 0.0,
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices, radius=radius, depth=depth,
        end_fill_type="NGON", location=location, rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    apply_object_transform(obj)
    if bevel > 0.0:
        modifier = obj.modifiers.new("low_poly_bevel", "BEVEL")
        modifier.width = bevel
        modifier.segments = 1
        modifier.limit_method = "ANGLE"
        active_object(obj)
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    set_generated_surface(obj, mat, swatch)
    return obj


def add_cone(
    name: str,
    location: Tuple[float, float, float],
    radius1: float,
    radius2: float,
    depth: float,
    swatch: str,
    mat: bpy.types.Material,
    vertices: int = 10,
    rotation: Tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices, radius1=radius1, radius2=radius2, depth=depth,
        end_fill_type="NGON", location=location, rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    apply_object_transform(obj)
    set_generated_surface(obj, mat, swatch)
    return obj


def add_torus(
    name: str,
    location: Tuple[float, float, float],
    major_radius: float,
    minor_radius: float,
    swatch: str,
    mat: bpy.types.Material,
    rotation: Tuple[float, float, float] = (0.0, 0.0, 0.0),
) -> bpy.types.Object:
    bpy.ops.mesh.primitive_torus_add(
        align="WORLD", major_segments=12, minor_segments=4,
        location=location, rotation=rotation,
        major_radius=major_radius, minor_radius=minor_radius,
    )
    obj = bpy.context.object
    obj.name = name
    apply_object_transform(obj)
    set_generated_surface(obj, mat, swatch)
    return obj


def add_crystal(
    name: str,
    location: Tuple[float, float, float],
    radius: float,
    height: float,
    swatch: str,
    mat: bpy.types.Material,
    sides: int = 6,
) -> bpy.types.Object:
    vertices: List[Tuple[float, float, float]] = []
    faces: List[Tuple[int, ...]] = []
    bottom_tip = len(vertices)
    vertices.append((0.0, 0.0, -height * 0.50))
    bottom_ring = len(vertices)
    for index in range(sides):
        angle = 2.0 * math.pi * index / sides
        vertices.append((math.cos(angle) * radius * 0.72, math.sin(angle) * radius * 0.72, -height * 0.18))
    top_ring = len(vertices)
    for index in range(sides):
        angle = 2.0 * math.pi * index / sides
        vertices.append((math.cos(angle) * radius, math.sin(angle) * radius, height * 0.20))
    top_tip = len(vertices)
    vertices.append((0.0, 0.0, height * 0.50))
    for index in range(sides):
        nxt = (index + 1) % sides
        faces.extend([
            (bottom_tip, bottom_ring + nxt, bottom_ring + index),
            (bottom_ring + index, bottom_ring + nxt, top_ring + nxt, top_ring + index),
            (top_ring + index, top_ring + nxt, top_tip),
        ])
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(vertices, [], faces)
    mesh.validate(verbose=False, clean_customdata=False)
    mesh.update(calc_edges=True)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    obj.location = location
    apply_object_transform(obj)
    set_generated_surface(obj, mat, swatch, ao=1.0)
    return obj


def add_beam_between(
    name: str,
    start: Vector,
    end: Vector,
    thickness: float,
    swatch: str,
    mat: bpy.types.Material,
) -> bpy.types.Object:
    midpoint = (start + end) * 0.5
    direction = end - start
    obj = add_box(
        name, tuple(midpoint), (thickness, thickness, direction.length),
        swatch, mat, bevel=thickness * 0.22,
    )
    obj.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    apply_object_transform(obj)
    return obj


def import_stone_keep_body(mat: bpy.types.Material) -> bpy.types.Object:
    """Import only the AI-authored lower keep for Lv2/Lv3 architecture."""

    bpy.ops.import_scene.fbx(
        filepath=str(SOURCE_DIR / "crossbow_tower.fbx"), use_custom_normals=True
    )
    objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if len(objects) != 1:
        raise PipelineError("AI crossbow tower must import as exactly one mesh")
    body = objects[0]
    body.name = "tower_ai_body"
    apply_object_transform(body)

    uv_layer = body.data.uv_layers.get("BaseColorUV") or body.data.uv_layers.active
    if uv_layer is None:
        raise PipelineError("AI crossbow tower has no UV0")
    # Keep all samples inside the 480px content region, away from the swatch strip.
    low = 0.5 / TOWER_ATLAS_SIZE
    span = (TOWER_CONTENT_SIZE - 1.0) / TOWER_ATLAS_SIZE
    for entry in uv_layer.data:
        entry.uv = (
            low + max(0.0, min(1.0, entry.uv.x)) * span,
            low + max(0.0, min(1.0, entry.uv.y)) * span,
        )
    uv_layer.name = "BaseColorUV"

    mesh = body.data
    work = bmesh.new()
    work.from_mesh(mesh)
    doomed = [
        face for face in work.faces
        if face.calc_center_median().z > 1.43
    ]
    bmesh.ops.delete(work, geom=doomed, context="FACES")
    loose = [vertex for vertex in work.verts if not vertex.link_faces]
    if loose:
        bmesh.ops.delete(work, geom=loose, context="VERTS")
    work.to_mesh(mesh)
    work.free()
    mesh.validate(verbose=False, clean_customdata=False)
    mesh.update(calc_edges=True)
    set_single_material(body, mat)
    return body


def add_wood_watchtower_body(
    parts: List[bpy.types.Object],
    mat: bpy.types.Material,
) -> float:
    """Build an unmistakably open, all-timber Lv1 watchtower."""

    # Four massive posts and X braces keep the body readable at gameplay size.
    for x in (-0.53, 0.53):
        for y in (-0.47, 0.47):
            parts.append(add_box(
                f"timber_post_{x:+.0f}_{y:+.0f}", (x, y, 0.66),
                (0.19, 0.19, 1.32), "wood", mat, bevel=0.035,
            ))
            parts.append(add_box(
                f"timber_foot_{x:+.0f}_{y:+.0f}", (x, y, 0.08),
                (0.31, 0.31, 0.16), "wood_light", mat, bevel=0.035,
            ))
    for y in (-0.49, 0.49):
        parts.append(add_beam_between(
            f"front_back_brace_a_{y:+.0f}", Vector((-0.52, y, 0.18)),
            Vector((0.52, y, 1.12)), 0.095, "wood_light", mat,
        ))
        parts.append(add_beam_between(
            f"front_back_brace_b_{y:+.0f}", Vector((0.52, y, 0.18)),
            Vector((-0.52, y, 1.12)), 0.095, "wood", mat,
        ))
    for x in (-0.55, 0.55):
        parts.append(add_beam_between(
            f"side_brace_a_{x:+.0f}", Vector((x, -0.45, 0.18)),
            Vector((x, 0.45, 1.12)), 0.09, "wood_light", mat,
        ))
        parts.append(add_beam_between(
            f"side_brace_b_{x:+.0f}", Vector((x, 0.45, 0.18)),
            Vector((x, -0.45, 1.12)), 0.09, "wood", mat,
        ))
    # Separated planks make the top read as a wooden platform rather than stone.
    for index in range(5):
        parts.append(add_box(
            f"deck_plank_{index}", (-0.56 + index * 0.28, 0.0, 1.25),
            (0.25, 1.34, 0.16), "wood_light" if index % 2 else "wood",
            mat, bevel=0.025,
        ))
    for x in (-0.66, 0.66):
        for y in (-0.55, 0.55):
            parts.append(add_box(
                f"rail_post_{x:+.0f}_{y:+.0f}", (x, y, 1.48),
                (0.10, 0.10, 0.48), "wood", mat, bevel=0.02,
            ))
    for y in (-0.58, 0.58):
        parts.append(add_box(
            f"rail_x_{y:+.0f}", (0.0, y, 1.62),
            (1.42, 0.10, 0.10), "wood_light", mat, bevel=0.02,
        ))
    # Front ladder is a second strong wooden read from the isometric camera.
    for x in (-0.30, 0.30):
        parts.append(add_box(
            f"ladder_side_{x:+.0f}", (x, -0.63, 0.69),
            (0.08, 0.08, 1.04), "wood_light", mat, bevel=0.015,
        ))
    for index in range(4):
        parts.append(add_box(
            f"ladder_rung_{index}", (0.0, -0.64, 0.29 + index * 0.25),
            (0.67, 0.09, 0.075), "wood_light", mat, bevel=0.012,
        ))
    return 1.52


def add_stone_keep_body(
    parts: List[bpy.types.Object],
    mat: bpy.types.Material,
) -> float:
    """Build the dense stone-and-blue-cloth Lv2 keep."""

    body = import_stone_keep_body(mat)
    parts.append(body)
    parts.append(add_cylinder(
        "stone_keep_crown", (0.0, 0.0, 1.43), 0.79, 0.18,
        "stone", mat, vertices=12, bevel=0.035,
    ))
    parts.append(add_box(
        "blue_keep_banner", (0.0, -0.735, 0.99),
        (0.48, 0.075, 0.72), "blue", mat, bevel=0.035,
    ))
    parts.append(add_box(
        "blue_banner_gold_header", (0.0, -0.78, 1.34),
        (0.56, 0.09, 0.10), "gold_dark", mat, bevel=0.02,
    ))
    return 1.55


def add_fortress_body(
    parts: List[bpy.types.Object],
    mat: bpy.types.Material,
) -> float:
    """Build a broad Lv3 fortress shell, battlements, standards and runes."""

    # The AI keep remains an inner core, but a new broad wall mass owns the
    # silhouette.  This is deliberately architectural, not a plinth add-on.
    body = import_stone_keep_body(mat)
    parts.append(body)
    parts.append(add_cylinder(
        "fortress_lower_wall", (0.0, 0.0, 0.57), 1.06, 1.14,
        "stone_dark", mat, vertices=12, bevel=0.055,
    ))
    parts.append(add_cylinder(
        "fortress_upper_wall", (0.0, 0.0, 1.27), 0.96, 0.70,
        "stone", mat, vertices=12, bevel=0.045,
    ))
    parts.append(add_torus(
        "fortress_gold_belt", (0.0, 0.0, 1.04), 1.02, 0.075,
        "gold", mat,
    ))
    for index in range(8):
        angle = 2.0 * math.pi * index / 8.0
        parts.append(add_box(
            f"battlement_{index}",
            (math.cos(angle) * 0.84, math.sin(angle) * 0.84, 1.72),
            (0.38, 0.36, 0.40), "stone", mat,
            rotation=(0.0, 0.0, angle), bevel=0.045,
        ))
    # Four buttresses expand the footprint into a fortress, not a tall turret.
    for index, (x, y) in enumerate(((-0.88, -0.72), (0.88, -0.72), (-0.88, 0.72), (0.88, 0.72))):
        parts.append(add_box(
            f"fortress_buttress_{index}", (x, y, 0.53),
            (0.34, 0.40, 0.98), "stone_dark", mat,
            rotation=(0.0, 0.0, math.radians(16.0 if x * y > 0 else -16.0)),
            bevel=0.045,
        ))
    # A large cyan rune and gold halo remain readable even with the weapon head.
    parts.append(add_crystal(
        "fortress_glowing_rune", (0.0, -1.075, 0.91),
        0.20, 0.62, "magic_core", mat, sides=6,
    ))
    parts.append(add_torus(
        "fortress_rune_gold_halo", (0.0, -1.105, 0.91),
        0.31, 0.045, "gold", mat,
        rotation=(math.radians(90.0), 0.0, 0.0),
    ))
    for side in (-1.0, 1.0):
        x = side * 1.13
        parts.append(add_cylinder(
            f"fortress_standard_pole_{side:+.0f}", (x, 0.16, 1.53),
            0.055, 1.58, "gold_dark", mat, vertices=8,
        ))
        parts.append(add_box(
            f"fortress_large_flag_{side:+.0f}",
            (x - side * 0.25, 0.12, 2.02),
            (0.55, 0.075, 0.64), "blue", mat,
            rotation=(0.0, 0.0, math.radians(side * 6.0)), bevel=0.025,
        ))
        parts.append(add_box(
            f"fortress_flag_gold_edge_{side:+.0f}",
            (x - side * 0.25, 0.075, 2.32),
            (0.58, 0.085, 0.075), "gold", mat,
            rotation=(0.0, 0.0, math.radians(side * 6.0)), bevel=0.015,
        ))
    return 1.82


def add_level_body(
    parts: List[bpy.types.Object],
    mat: bpy.types.Material,
    level: int,
) -> float:
    if level == 1:
        return add_wood_watchtower_body(parts, mat)
    if level == 2:
        return add_stone_keep_body(parts, mat)
    return add_fortress_body(parts, mat)


def add_crossbow_head(
    parts: List[bpy.types.Object],
    mat: bpy.types.Material,
    level: int,
    deck_z: float,
) -> None:
    width = (0.86, 1.04, 1.17)[level - 1]
    z = deck_z + 0.27
    limb_swatch = "wood_light" if level == 1 else "iron_light"
    parts.append(add_cylinder(
        "crossbow_pivot", (0.0, 0.0, deck_z + 0.10),
        0.18, 0.25, "wood" if level == 1 else "gold_dark", mat,
        vertices=10,
    ))
    for side in (-1.0, 1.0):
        parts.append(add_beam_between(
            f"crossbow_limb_{side:+.0f}", Vector((side * 0.08, 0.0, z - 0.05)),
            Vector((side * width, 0.02, z + 0.15)),
            0.105 if level == 1 else 0.09, limb_swatch, mat,
        ))
        parts.append(add_beam_between(
            f"crossbow_string_{side:+.0f}", Vector((side * width, 0.02, z + 0.15)),
            Vector((0.0, 0.28, z - 0.03)), 0.025, "iron", mat,
        ))
    rail_count = 1 if level == 1 else 2 if level == 2 else 3
    for index in range(rail_count):
        x = (index - (rail_count - 1) * 0.5) * 0.25
        parts.append(add_box(
            f"crossbow_rail_{index}", (x, -0.31, z),
            (0.11, 1.06, 0.11), "wood" if level == 1 else "iron", mat,
            rotation=(0.0, 0.0, math.radians((index - 1) * 4.0 if level == 3 else 0.0)),
            bevel=0.025,
        ))
    parts.append(add_cone(
        "crossbow_bolt", (0.0, -0.40, z + 0.07), 0.075, 0.025, 1.16,
        "gold" if level >= 2 else "wood_light", mat, vertices=8,
        rotation=(math.radians(90.0), 0.0, 0.0),
    ))
    if level >= 3:
        for side in (-1.0, 1.0):
            parts.append(add_cylinder(
                f"bow_gold_cap_{side:+.0f}", (side * width, 0.02, z + 0.15),
                0.13, 0.18, "gold", mat, vertices=8,
                rotation=(0.0, math.radians(90.0), 0.0),
            ))
        parts.append(add_crystal(
            "crossbow_rune_sight", (0.0, 0.02, z + 0.39), 0.12, 0.34,
            "magic_core", mat, sides=6,
        ))


def add_cannon_head(
    parts: List[bpy.types.Object],
    mat: bpy.types.Material,
    level: int,
    deck_z: float,
) -> None:
    scale = (0.88, 1.0, 1.13)[level - 1]
    parts.append(add_cylinder(
        "cannon_turntable", (0.0, 0.0, deck_z + 0.03), 0.71 * scale, 0.18,
        "wood" if level == 1 else "stone", mat, vertices=12, bevel=0.035,
    ))
    parts.append(add_box(
        "cannon_shield", (0.0, -0.18, deck_z + 0.29),
        (1.20 * scale, 0.16, 0.55 * scale),
        "wood_light" if level == 1 else "blue_dark", mat,
        bevel=0.07,
    ))
    barrel_length = (0.92, 1.18, 1.42)[level - 1]
    barrel_radius = (0.16, 0.205, 0.245)[level - 1]
    barrel_y = -0.20 - barrel_length * 0.34
    barrel_z = deck_z + 0.33 + 0.06 * (level - 1)
    parts.append(add_cylinder(
        "cannon_barrel", (0.0, barrel_y, barrel_z),
        barrel_radius, barrel_length, "iron", mat, vertices=12,
        rotation=(math.radians(90.0), 0.0, 0.0), bevel=0.025,
    ))
    muzzle_y = barrel_y - barrel_length * 0.5
    parts.append(add_cylinder(
        "cannon_muzzle", (0.0, muzzle_y, barrel_z),
        barrel_radius * 1.33, 0.18 + level * 0.025, "muzzle", mat,
        vertices=12, rotation=(math.radians(90.0), 0.0, 0.0),
        bevel=0.018,
    ))
    parts.append(add_cylinder(
        "cannon_breech", (0.0, 0.18, barrel_z),
        barrel_radius * 1.18, 0.34, "bronze", mat, vertices=10,
        rotation=(math.radians(90.0), 0.0, 0.0), bevel=0.025,
    ))
    for side in (-1.0, 1.0):
        parts.append(add_cylinder(
            f"cannon_wheel_{side:+.0f}",
            (side * (0.43 + level * 0.035), 0.08, deck_z + 0.13),
            0.27 + level * 0.025, 0.14, "wood", mat, vertices=10,
            rotation=(0.0, math.radians(90.0), 0.0),
        ))
    if level >= 2:
        parts.append(add_torus(
            "cannon_gold_collar", (0.0, muzzle_y + 0.10, barrel_z),
            barrel_radius * 1.12, 0.045, "gold", mat,
            rotation=(math.radians(90.0), 0.0, 0.0),
        ))
    if level >= 3:
        for side in (-1.0, 1.0):
            parts.append(add_cylinder(
                f"recoil_piston_{side:+.0f}", (side * 0.30, barrel_y + 0.10, barrel_z - 0.26),
                0.07, barrel_length * 0.72, "bronze", mat, vertices=8,
                rotation=(math.radians(90.0), 0.0, 0.0),
            ))


def add_magic_head(
    parts: List[bpy.types.Object],
    mat: bpy.types.Material,
    level: int,
    deck_z: float,
) -> None:
    scale = (0.84, 1.0, 1.15)[level - 1]
    parts.append(add_cylinder(
        "magic_deck", (0.0, 0.0, deck_z + 0.03), 0.70 * scale, 0.18,
        "wood" if level == 1 else "stone", mat, vertices=12, bevel=0.035,
    ))
    core_z = deck_z + 0.39 + 0.08 * (level - 1)
    core_radius = 0.23 + 0.035 * (level - 1)
    core_height = 0.62 + 0.11 * (level - 1)
    parts.append(add_crystal(
        "magic_core", (0.0, 0.0, core_z), core_radius, core_height,
        "magic_cyan" if level < 3 else "magic_core", mat, sides=6,
    ))
    prongs = 2 + level
    for index in range(prongs):
        angle = 2.0 * math.pi * index / prongs + math.pi * 0.5
        start = Vector((
            math.cos(angle) * 0.57 * scale,
            math.sin(angle) * 0.57 * scale,
            deck_z + 0.10,
        ))
        end = Vector((math.cos(angle) * 0.25 * scale, math.sin(angle) * 0.25 * scale, core_z + 0.06))
        parts.append(add_beam_between(
            f"magic_prong_{index}", start, end, 0.075,
            "gold_dark" if level == 1 else "gold", mat,
        ))
    if level >= 2:
        parts.append(add_torus(
            "magic_orbit_a", (0.0, 0.0, core_z),
            0.43 * scale, 0.045, "magic_blue", mat,
            rotation=(math.radians(62.0), 0.0, math.radians(18.0)),
        ))
    if level >= 3:
        parts.append(add_torus(
            "magic_orbit_b", (0.0, 0.0, core_z + 0.03),
            0.52 * scale, 0.04, "magic_violet", mat,
            rotation=(math.radians(-55.0), math.radians(32.0), 0.0),
        ))
        for side in (-1.0, 1.0):
            parts.append(add_crystal(
                f"magic_satellite_{side:+.0f}",
                (side * 0.61, 0.02, core_z + 0.04),
                0.105, 0.30, "magic_violet", mat, sides=5,
            ))


def normalize_tower_geometry(
    tower: bpy.types.Object,
    target_height: float = TOWER_AUTHORED_HEIGHT,
) -> None:
    """Ground, centre and height-normalise a joined tower mesh.

    Runtime owns the exact +20% level scaling.  Keeping every authored FBX at
    one logical height prevents asset geometry and runtime scale multiplying
    into the previous 27-34% jumps.
    """

    vertices = tower.data.vertices
    if not vertices:
        raise PipelineError(f"{tower.name}: tower mesh has no vertices")
    minimum_x = min(vertex.co.x for vertex in vertices)
    maximum_x = max(vertex.co.x for vertex in vertices)
    minimum_y = min(vertex.co.y for vertex in vertices)
    maximum_y = max(vertex.co.y for vertex in vertices)
    minimum_z = min(vertex.co.z for vertex in vertices)
    maximum_z = max(vertex.co.z for vertex in vertices)
    height = maximum_z - minimum_z
    if height <= 0.001:
        raise PipelineError(f"{tower.name}: invalid tower height {height}")
    scale = target_height / height
    centre_x = (minimum_x + maximum_x) * 0.5
    centre_y = (minimum_y + maximum_y) * 0.5
    for vertex in vertices:
        vertex.co.x = (vertex.co.x - centre_x) * scale
        vertex.co.y = (vertex.co.y - centre_y) * scale
        vertex.co.z = (vertex.co.z - minimum_z) * scale
    tower.data.update(calc_edges=True)


def join_tower(parts: Sequence[bpy.types.Object], tower_id: str) -> bpy.types.Object:
    if not parts:
        raise PipelineError(f"{tower_id}: no tower parts")
    bpy.ops.object.select_all(action="DESELECT")
    for obj in parts:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    tower = bpy.context.object
    tower.name = tower_id
    # Lv1 starts with an offset timber post rather than an origin-centred AI
    # body.  Bake that active-object origin before grounding/centring.
    active_object(tower)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    for polygon in tower.data.polygons:
        polygon.material_index = 0
    while len(tower.data.materials) > 1:
        tower.data.materials.pop(index=len(tower.data.materials) - 1)
    triangulate = tower.modifiers.new("export_triangulate", "TRIANGULATE")
    active_object(tower)
    bpy.ops.object.modifier_apply(modifier=triangulate.name)
    tower.data.validate(verbose=False, clean_customdata=False)
    tower.data.update(calc_edges=True)
    normalize_tower_geometry(tower)
    return tower


def build_tower_variant(tower_type: str, level: int) -> Dict[str, object]:
    reset_scene()
    mat = tower_material()
    parts: List[bpy.types.Object] = []
    deck_z = add_level_body(parts, mat, level)
    if tower_type == "crossbow":
        add_crossbow_head(parts, mat, level, deck_z)
    elif tower_type == "cannon":
        add_cannon_head(parts, mat, level, deck_z)
    elif tower_type == "magic":
        add_magic_head(parts, mat, level, deck_z)
    else:
        raise PipelineError(f"unknown tower type: {tower_type}")
    tower_id = f"tower_{tower_type}_lv{level}"
    tower = join_tower(parts, tower_id)
    export_fbx([tower], TARGET_DIR / f"{tower_id}.fbx")
    return {
        "id": tower_id,
        "tower_type": tower_type,
        "level": level,
        "texture_id": TOWER_TEXTURE_ID,
        "body_style": TOWER_BODY_STYLE[level],
        "ai_base": "none" if level == 1 else "crossbow_tower_inner_keep",
        "head": f"procedural_{tower_type}",
        "authored_height": TOWER_AUTHORED_HEIGHT,
        "runtime_scale": TOWER_RUNTIME_SCALE[level - 1],
    }


def build_tower_variants() -> List[Dict[str, object]]:
    metadata = []
    for tower_type in TOWER_TYPES:
        for level in TOWER_LEVELS:
            metadata.append(build_tower_variant(tower_type, level))
    return metadata


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
    for tower_id in TOWER_IDS:
        duplicate_texture = TARGET_DIR / f"{tower_id}.png"
        if duplicate_texture.exists():
            duplicate_texture.unlink()
    for asset_id in ASSET_IDS:
        shutil.copy2(SOURCE_DIR / f"{asset_id}.fbx", TARGET_DIR / f"{asset_id}.fbx")
        if asset_id not in {"ally_soldier", "enemy_soldier"}:
            shutil.copy2(SOURCE_DIR / f"{asset_id}.png", TARGET_DIR / f"{asset_id}.png")
    ally_cleanup = clean_ally_texture(
        SOURCE_DIR / "ally_soldier.png", TARGET_DIR / "ally_soldier.png"
    )
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
    tower_atlas = create_tower_atlas(
        SOURCE_DIR / "crossbow_tower.png",
        TARGET_DIR / f"{TOWER_TEXTURE_ID}.png",
    )
    tower_variants = build_tower_variants()
    return {
        "ally_cleanup": ally_cleanup,
        "enemy_cleanup": enemy_cleanup,
        "tower_atlas": tower_atlas,
        "tower_variants": tower_variants,
    }


def install_towers_only() -> Dict[str, object]:
    """Regenerate only tower FBXs while preserving the shared atlas and masks."""

    TARGET_DIR.mkdir(parents=True, exist_ok=True)
    atlas_path = TARGET_DIR / f"{TOWER_TEXTURE_ID}.png"
    if not atlas_path.is_file():
        raise PipelineError(
            f"--towers-only requires existing shared atlas: {atlas_path}"
        )
    metadata: Dict[str, object] = {}
    if REPORT_PATH.is_file():
        previous = json.loads(REPORT_PATH.read_text(encoding="utf-8"))
        for key in ("ally_cleanup", "enemy_cleanup", "tower_atlas"):
            if previous.get(key) is not None:
                metadata[key] = previous[key]
    for tower_id in TOWER_IDS:
        duplicate_texture = TARGET_DIR / f"{tower_id}.png"
        if duplicate_texture.exists():
            duplicate_texture.unlink()
    metadata["tower_variants"] = build_tower_variants()
    return metadata


def import_meshes(path: Path) -> List[bpy.types.Object]:
    reset_scene()
    bpy.ops.import_scene.fbx(filepath=str(path), use_custom_normals=True)
    return [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]


def fbx_stats(path: Path) -> Dict[str, object]:
    objects = import_meshes(path)
    if not objects:
        raise PipelineError(f"FBX has no mesh objects: {path}")
    triangle_count = 0
    vertex_count = 0
    logical_points: List[Tuple[float, float, float]] = []
    uv_layers = 0
    color_layers = set()
    material_slots = 0
    material_names = set()
    transforms = []
    for obj in objects:
        mesh = obj.data
        mesh.calc_loop_triangles()
        triangle_count += len(mesh.loop_triangles)
        vertex_count += len(mesh.vertices)
        uv_layers = max(uv_layers, len(mesh.uv_layers))
        color_layers.update(layer.name for layer in mesh.color_attributes)
        material_slots += len(mesh.materials)
        material_names.update(material.name for material in mesh.materials if material)
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
        "vertices": vertex_count,
        "triangles": triangle_count,
        "uv_layers": uv_layers,
        "color_layers": sorted(color_layers),
        "material_slots": material_slots,
        "material_names": sorted(material_names),
        "unity_logical_bounds_min": round_vec(minimum),
        "unity_logical_bounds_max": round_vec(maximum),
        "unity_logical_height": round(maximum[1] - minimum[1], 6),
        "imported_transforms": transforms,
    }


def generated_repro_signatures(
    fbx_ids: Sequence[str] = GENERATED_FBX_IDS,
    png_ids: Sequence[str] = GENERATED_PNG_IDS,
) -> Dict[str, object]:
    """Return exporter-metadata-independent signatures for generated assets.

    Blender's binary FBX exporter writes volatile creation metadata, so two
    correct exports need not have the same file hash.  Freshly importing each
    FBX and comparing the geometry, material, bounds, and transforms is the
    meaningful reproducibility contract.  Generated PNGs remain byte exact.
    """

    semantic_keys = (
        "mesh_objects",
        "vertices",
        "triangles",
        "uv_layers",
        "color_layers",
        "material_slots",
        "material_names",
        "unity_logical_bounds_min",
        "unity_logical_bounds_max",
        "unity_logical_height",
        "imported_transforms",
    )
    fbx = {}
    for asset_id in fbx_ids:
        stats = fbx_stats(TARGET_DIR / f"{asset_id}.fbx")
        fbx[asset_id] = {key: stats[key] for key in semantic_keys}
    png = {
        asset_id: sha256(TARGET_DIR / f"{asset_id}.png")
        for asset_id in png_ids
    }
    return {"fbx_semantic": fbx, "png_sha256": png}


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
        if asset_id not in {"ally_soldier", "enemy_soldier"} and png["sha256"] != source_contract[asset_id]["png_sha256"]:
            errors.append(f"{asset_id}: installed PNG is not byte-identical to immutable source")
        if asset_id in {"ally_soldier", "enemy_soldier"} and png["sha256"] == source_contract[asset_id]["png_sha256"]:
            errors.append(f"{asset_id}: cleanup did not change installed texture")
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

    attachment_by_id = {item["id"]: item for item in attachments}
    crown = attachment_by_id.get("king_crown")
    glow = attachment_by_id.get("giant_blade_glow")
    crown_above_head = False
    glow_inside_parent = False
    glow_alpha_ready = False
    if crown:
        crown_min = crown["fbx"]["unity_logical_bounds_min"]
        crown_max = crown["fbx"]["unity_logical_bounds_max"]
        crown_above_head = crown_min[1] >= 1.19 and crown_max[1] >= 1.45
        if not crown_above_head:
            errors.append("king_crown: crown does not sit visibly above the AI king head")
    if glow:
        glow_min = glow["fbx"]["unity_logical_bounds_min"]
        glow_max = glow["fbx"]["unity_logical_bounds_max"]
        giant = next((item for item in assets if item["id"] == "giant"), None)
        glow_inside_parent = giant is not None
        if giant:
            giant_min = giant["fbx"]["unity_logical_bounds_min"]
            giant_max = giant["fbx"]["unity_logical_bounds_max"]
            for axis in range(3):
                if glow_min[axis] < giant_min[axis] - 0.03 or glow_max[axis] > giant_max[axis] + 0.03:
                    errors.append("giant_blade_glow: attachment falls outside giant bounds")
                    glow_inside_parent = False
                    break
        glow_alpha_ready = (
            glow["png"]["alpha_min"] < 0.1
            and glow["png"]["alpha_max"] > 0.9
        )
        if not glow_alpha_ready:
            errors.append("giant_blade_glow: texture lacks transparent edge or bright core")

    tower_texture_path = TARGET_DIR / f"{TOWER_TEXTURE_ID}.png"
    tower_texture = None
    if not tower_texture_path.is_file():
        errors.append(f"missing shared tower atlas: {TOWER_TEXTURE_ID}.png")
    else:
        tower_texture = png_stats(tower_texture_path)
        if (tower_texture["width"], tower_texture["height"]) != (
            TOWER_ATLAS_SIZE, TOWER_ATLAS_SIZE
        ):
            errors.append("tower_shared: unexpected texture dimensions")

    tower_variants = []
    tower_by_type: Dict[str, List[Dict[str, object]]] = {
        tower_type: [] for tower_type in TOWER_TYPES
    }
    generated_meta = {
        item["id"]: item for item in install_meta.get("tower_variants", [])
    }
    for tower_type in TOWER_TYPES:
        for level in TOWER_LEVELS:
            tower_id = f"tower_{tower_type}_lv{level}"
            fbx_path = TARGET_DIR / f"{tower_id}.fbx"
            duplicate_png = TARGET_DIR / f"{tower_id}.png"
            if not fbx_path.is_file():
                errors.append(f"missing tower variant: {tower_id}.fbx")
                continue
            if duplicate_png.exists():
                errors.append(f"{tower_id}: per-level texture duplicate must not exist")
            fbx = fbx_stats(fbx_path)
            if fbx["mesh_objects"] != 1:
                errors.append(f"{tower_id}: mesh objects {fbx['mesh_objects']} != 1")
            if fbx["material_slots"] != 1:
                errors.append(f"{tower_id}: material slots {fbx['material_slots']} != 1")
            if fbx["material_names"] != ["AI3D_TowerShared"]:
                errors.append(
                    f"{tower_id}: shared material name is {fbx['material_names']}, "
                    "expected AI3D_TowerShared"
                )
            if fbx["uv_layers"] != 1:
                errors.append(f"{tower_id}: UV layers {fbx['uv_layers']} != 1")
            if "Color" not in fbx["color_layers"]:
                errors.append(f"{tower_id}: missing Color AO channel")
            # Lv1 is intentionally an efficient procedural timber frame; Lv2/3
            # retain the denser AI stone core.  The upper ceiling remains the
            # meaningful WebGL budget guard.
            if not 200 <= fbx["triangles"] <= 6200:
                errors.append(f"{tower_id}: triangles {fbx['triangles']} outside 200..6200")
            if abs(fbx["unity_logical_bounds_min"][1]) > 0.012:
                errors.append(
                    f"{tower_id}: base is not grounded at Y=0 "
                    f"({fbx['unity_logical_bounds_min'][1]})"
                )
            transforms_ok = all(
                transform["location"] == [0.0, 0.0, 0.0]
                and transform["scale"] == [1.0, 1.0, 1.0]
                for transform in fbx["imported_transforms"]
            )
            if not transforms_ok:
                errors.append(f"{tower_id}: imported transform is not unit/zero")
            item = {
                "id": tower_id,
                "tower_type": tower_type,
                "level": level,
                "texture_id": TOWER_TEXTURE_ID,
                "body_style": generated_meta.get(tower_id, {}).get(
                    "body_style", TOWER_BODY_STYLE[level]
                ),
                "ai_base": generated_meta.get(tower_id, {}).get(
                    "ai_base", "none" if level == 1 else "crossbow_tower_inner_keep"
                ),
                "head": (
                    generated_meta.get(tower_id, {}).get("head")
                    or f"procedural_{tower_type}"
                ),
                "authored_height": TOWER_AUTHORED_HEIGHT,
                "runtime_scale": TOWER_RUNTIME_SCALE[level - 1],
                "fbx": fbx,
            }
            tower_variants.append(item)
            tower_by_type[tower_type].append(item)

    level_progression = {}
    for tower_type, variants in tower_by_type.items():
        variants.sort(key=lambda item: item["level"])
        triangles = [item["fbx"]["triangles"] for item in variants]
        heights = [item["fbx"]["unity_logical_height"] for item in variants]
        effective_heights = [
            round(height * TOWER_RUNTIME_SCALE[index], 6)
            for index, height in enumerate(heights)
        ]
        effective_ratios = [
            round(effective_heights[index] / effective_heights[index - 1], 6)
            for index in range(1, len(effective_heights))
        ]
        triangle_pass = len(triangles) == 3 and triangles[0] < triangles[1] < triangles[2]
        height_pass = len(heights) == 3 and all(
            abs(height - TOWER_AUTHORED_HEIGHT) <= 0.006 for height in heights
        )
        runtime_scale_pass = len(effective_ratios) == 2 and all(
            abs(ratio - 1.2) <= 0.006 for ratio in effective_ratios
        )
        if not triangle_pass:
            errors.append(f"tower_{tower_type}: level triangle complexity does not increase")
        if not height_pass:
            errors.append(
                f"tower_{tower_type}: authored heights are not normalised to "
                f"{TOWER_AUTHORED_HEIGHT}m"
            )
        if not runtime_scale_pass:
            errors.append(f"tower_{tower_type}: runtime effective height is not +20% per level")
        level_progression[tower_type] = {
            "triangles": triangles,
            "heights": heights,
            "body_styles": [item["body_style"] for item in variants],
            "runtime_scales": list(TOWER_RUNTIME_SCALE),
            "effective_heights": effective_heights,
            "effective_height_ratios": effective_ratios,
            "triangle_complexity_increases": triangle_pass,
            "authored_height_normalized": height_pass,
            "runtime_twenty_percent_steps": runtime_scale_pass,
            "passed": triangle_pass and height_pass and runtime_scale_pass,
        }

    ally_cleanup = install_meta.get("ally_cleanup")
    enemy_cleanup = install_meta.get("enemy_cleanup")
    if ally_cleanup and not 0.28 <= ally_cleanup.get("team_ratio_after", 0.0) <= 0.48:
        errors.append("ally_soldier: cleaned team-mask coverage outside 28..48%")
    if enemy_cleanup and not 0.35 <= enemy_cleanup.get("team_ratio_after", 0.0) <= 0.55:
        errors.append("enemy_soldier: cleaned team-mask coverage outside 35..55%")
    reproducibility = install_meta.get("reproducibility")
    if reproducibility and reproducibility.get("checked") and not reproducibility.get("passed"):
        errors.append("headless regeneration changed imported FBX semantics or PNG bytes")

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
    total_gzip_bytes = sum(
        len(gzip.compress(path.read_bytes(), compresslevel=9, mtime=0))
        for path in TARGET_DIR.iterdir()
        if path.is_file() and path.suffix.lower() in {".fbx", ".png"}
    )
    report = {
        "schema_version": 3,
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "generator": str(SCRIPT.relative_to(SCRIPT.parents[5])),
        "blender_version": bpy.app.version_string,
        "scope": "Art R4/V3 AI 3D assets and nine tower upgrades only; no runtime code or public build",
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
            "tower_variants": (
                "tower_{crossbow,cannon,magic}_lv{1,2,3}; "
                "Lv1 timber watchtower, Lv2 stone/blue-cloth keep, "
                "Lv3 crenellated gold/rune fortress; one mesh/material each"
            ),
            "tower_runtime_scale": "authored height 2.10m; runtime 1.0/1.2/1.44 gives exact +20% steps",
            "tower_texture": f"all nine variants share {TOWER_TEXTURE_ID}.png; per-level PNG duplicates forbidden",
            "unity_meta": "not committed by unity-track contract; warm workspace generates/preserves .meta files",
        },
        "ally_cleanup": ally_cleanup,
        "enemy_cleanup": enemy_cleanup,
        "tower_atlas": install_meta.get("tower_atlas"),
        "assets": assets,
        "attachments": attachments,
        "attachment_checks": {
            "king_crown_above_head": crown_above_head,
            "giant_glow_inside_parent_bounds": glow_inside_parent,
            "giant_glow_alpha_ready": glow_alpha_ready,
            "ally_team_mask_ratio_28_48_percent": bool(
                ally_cleanup
                and 0.28 <= ally_cleanup.get("team_ratio_after", 0.0) <= 0.48
            ),
            "enemy_team_mask_ratio_35_55_percent": bool(
                enemy_cleanup
                and 0.35 <= enemy_cleanup.get("team_ratio_after", 0.0) <= 0.55
            ),
        },
        "tower_shared_texture": tower_texture,
        "tower_variants": tower_variants,
        "tower_level_progression": level_progression,
        "reproducibility": reproducibility,
        "total_installed_binary_bytes": total_bytes,
        "total_installed_gzip_bytes": total_gzip_bytes,
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

    for tower_type in TOWER_TYPES:
        for level in TOWER_LEVELS:
            tower_id = f"tower_{tower_type}_lv{level}"
            reset_scene()
            bpy.ops.import_scene.fbx(
                filepath=str(TARGET_DIR / f"{tower_id}.fbx"), use_custom_normals=True
            )
            tower = max(
                (obj for obj in bpy.context.scene.objects if obj.type == "MESH"),
                key=lambda obj: len(obj.data.vertices),
            )
            configure_preview_material(
                tower, TARGET_DIR / f"{TOWER_TEXTURE_ID}.png"
            )
            points = [tower.matrix_world @ vertex.co for vertex in tower.data.vertices]
            minimum_z = min(point.z for point in points)
            maximum_z = max(point.z for point in points)
            extent = max(
                maximum_z - minimum_z,
                max(point.x for point in points) - min(point.x for point in points),
                max(point.y for point in points) - min(point.y for point in points),
            )
            studio_render(
                preview_dir / f"{tower_id}.png",
                Vector((0.0, 0.0, (minimum_z + maximum_z) * 0.5)),
                max(1.15, extent * 0.72),
            )


def write_report(report: Dict[str, object]) -> None:
    text = json.dumps(report, ensure_ascii=False, indent=2) + "\n"
    TARGET_MANIFEST.write_text(text, encoding="utf-8")
    REPORT_PATH.write_text(text, encoding="utf-8")


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    selected_modes = sum(bool(value) for value in (
        args.verify_only, args.soldier_textures_only, args.towers_only,
    ))
    if selected_modes > 1:
        raise PipelineError(
            "--verify-only, --soldier-textures-only and --towers-only are mutually exclusive"
        )
    source_hashes = assert_inputs()
    install_meta: Dict[str, object] = {}
    if args.soldier_textures_only:
        previous_reproducibility = None
        if REPORT_PATH.is_file():
            previous = json.loads(REPORT_PATH.read_text(encoding="utf-8"))
            for key in (
                "tower_atlas", "tower_variants",
            ):
                if previous.get(key) is not None:
                    install_meta[key] = previous[key]
            previous_reproducibility = previous.get("reproducibility")
        install_meta["ally_cleanup"] = clean_ally_texture(
            SOURCE_DIR / "ally_soldier.png", TARGET_DIR / "ally_soldier.png"
        )
        install_meta["enemy_cleanup"] = clean_enemy_texture(
            SOURCE_DIR / "enemy_soldier.png", TARGET_DIR / "enemy_soldier.png"
        )
        first_soldier_hashes = {
            asset_id: sha256(TARGET_DIR / f"{asset_id}.png")
            for asset_id in ("ally_soldier", "enemy_soldier")
        }
        install_meta["ally_cleanup"] = clean_ally_texture(
            SOURCE_DIR / "ally_soldier.png", TARGET_DIR / "ally_soldier.png"
        )
        install_meta["enemy_cleanup"] = clean_enemy_texture(
            SOURCE_DIR / "enemy_soldier.png", TARGET_DIR / "enemy_soldier.png"
        )
        second_soldier_hashes = {
            asset_id: sha256(TARGET_DIR / f"{asset_id}.png")
            for asset_id in ("ally_soldier", "enemy_soldier")
        }
        soldier_changed = sorted(
            asset_id for asset_id in first_soldier_hashes
            if first_soldier_hashes[asset_id] != second_soldier_hashes[asset_id]
        )
        prior_checked = bool(
            previous_reproducibility
            and previous_reproducibility.get("checked")
            and previous_reproducibility.get("passed")
        )
        prior_png_identical = bool(
            previous_reproducibility
            and previous_reproducibility.get("png_byte_identical")
        )
        prior_png_changed = (
            previous_reproducibility.get("png_changed", [])
            if previous_reproducibility else []
        )
        install_meta["reproducibility"] = {
            **(previous_reproducibility or {}),
            "checked": prior_checked,
            "method": (
                "composed reproducibility evidence: prior two consecutive full "
                "generations for the existing 15 outputs, plus two consecutive "
                "soldier-mask refreshes from immutable sources for the 16-output set"
            ),
            "generated_files": len(GENERATED_FBX_IDS) + len(GENERATED_PNG_IDS),
            "png_byte_identical": prior_png_identical and not soldier_changed,
            "png_changed": sorted(set(prior_png_changed) | set(soldier_changed)),
            "soldier_texture_check": {
                "checked": True,
                "method": "two consecutive cleanup passes from immutable source PNGs",
                "first_sha256": first_soldier_hashes,
                "second_sha256": second_soldier_hashes,
                "changed": soldier_changed,
                "passed": not soldier_changed,
            },
            "passed": prior_checked and prior_png_identical and not soldier_changed,
        }
    elif not args.verify_only:
        regenerate = install_towers_only if args.towers_only else install_assets
        reproducible_fbx_ids = TOWER_IDS if args.towers_only else GENERATED_FBX_IDS
        reproducible_png_ids: Sequence[str] = () if args.towers_only else GENERATED_PNG_IDS
        install_meta = regenerate()
        if args.determinism_check:
            first_hashes = target_binary_hashes()
            first_signatures = generated_repro_signatures(
                reproducible_fbx_ids, reproducible_png_ids
            )
            install_meta = regenerate()
            second_hashes = target_binary_hashes()
            second_signatures = generated_repro_signatures(
                reproducible_fbx_ids, reproducible_png_ids
            )
            fbx_byte_changed = sorted(
                name for name in set(first_hashes) | set(second_hashes)
                if name.endswith(".fbx")
                and name.removesuffix(".fbx") in reproducible_fbx_ids
                and first_hashes.get(name) != second_hashes.get(name)
            )
            semantic_changed = sorted(
                asset_id for asset_id in reproducible_fbx_ids
                if first_signatures["fbx_semantic"].get(asset_id)
                != second_signatures["fbx_semantic"].get(asset_id)
            )
            png_changed = sorted(
                asset_id for asset_id in reproducible_png_ids
                if first_signatures["png_sha256"].get(asset_id)
                != second_signatures["png_sha256"].get(asset_id)
            )
            passed = not semantic_changed and not png_changed
            install_meta["reproducibility"] = {
                "checked": True,
                "method": (
                    "two consecutive Blender --background generations"
                    + (" of the nine tower FBXs only" if args.towers_only else "")
                    + "; fresh-import "
                    "FBX geometry/material/bounds/transforms plus generated PNG bytes"
                ),
                "scope": "tower_fbx_only" if args.towers_only else "all_generated_assets",
                "generated_files": len(reproducible_fbx_ids) + len(reproducible_png_ids),
                "fbx_semantic_identical": not semantic_changed,
                "fbx_semantic_changed": semantic_changed,
                "png_byte_identical": not png_changed,
                "png_changed": png_changed,
                "fbx_byte_identical": not fbx_byte_changed,
                "fbx_byte_changed": fbx_byte_changed,
                "fbx_byte_note": (
                    "informational only: Blender FBX creation metadata can vary while "
                    "fresh-import content remains identical"
                ),
                "passed": passed,
            }
    elif REPORT_PATH.is_file():
        previous = json.loads(REPORT_PATH.read_text(encoding="utf-8"))
        for key in (
            "ally_cleanup", "enemy_cleanup", "tower_atlas",
            "tower_variants", "reproducibility",
        ):
            if previous.get(key) is not None:
                install_meta[key] = previous[key]
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
