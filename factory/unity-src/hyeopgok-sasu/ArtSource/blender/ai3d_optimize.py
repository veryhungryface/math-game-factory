#!/usr/bin/env python3
"""Optimize Meshy GLB assets for the Hyeopgok Sasu asset-only A stage.

This script deliberately writes only below ``ArtSource/ai3d`` (plus reads from
the caller-selected raw directory).  It never installs assets into Unity's
``Resources`` folder.  Run it through Blender so ``bpy`` is available::

    /Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup \
      --python factory/unity-src/hyeopgok-sasu/ArtSource/blender/ai3d_optimize.py \
      -- --all

The current Unity runtime does not sample these textures.  A later integration
stage must bind the PNG and update the shader while preserving the contract
written to ``manifest.json``:

* UV0: base-colour atlas
* COLOR.rgb: white multiplier
* COLOR.a: geometric AO
* texture alpha: 0=tintable team colour, 1=fixed authored colour

Raw GLBs are treated as licensed task inputs.  No remote calls or credentials
are used here.
"""

from __future__ import annotations

import argparse
import colorsys
import hashlib
import json
import math
import os
import sys
import traceback
from dataclasses import asdict, dataclass
from datetime import datetime, timezone
from pathlib import Path
from typing import Dict, Iterable, List, Optional, Sequence, Tuple

import bpy
import bmesh
from mathutils import Matrix, Vector
from mathutils.bvhtree import BVHTree


SCRIPT = Path(__file__).resolve()
HERE = SCRIPT.parent
ART_SOURCE = HERE.parent


def find_repo_root(path: Path) -> Path:
    for candidate in (path, *path.parents):
        if (candidate / ".git").exists():
            return candidate
    # Stable fallback for factory/unity-src/<slug>/ArtSource/blender/<script>.
    return SCRIPT.parents[5]


REPO_ROOT = find_repo_root(HERE)
DEFAULT_RAW = REPO_ROOT / "scratchpad/ai3d-raw/hyeopgok-sasu-a"
DEFAULT_OUT = ART_SOURCE / "ai3d/out"
DEFAULT_PREVIEWS = ART_SOURCE / "ai3d/previews"
MANIFEST_VERSION = 1
AO_SAMPLES = 48
AO_MIN = 0.42
AO_STRENGTH = 0.62


@dataclass(frozen=True)
class AssetSpec:
    asset_id: str
    label: str
    aliases: Tuple[str, ...]
    target_triangles: int
    max_triangles: int
    target_height_m: float
    atlas_size: int
    min_triangles: int = 0
    body_height_m: Optional[float] = None
    team: Optional[str] = None


ASSET_SPECS: Dict[str, AssetSpec] = {
    "king": AssetSpec(
        "king", "King", ("king", "hero", "wang"), 5500, 6000, 1.3275, 512
    ),
    "ally_soldier": AssetSpec(
        "ally_soldier", "Ally soldier",
        ("ally_soldier", "ally-soldier", "blue_soldier", "blue-soldier", "ally", "blue"),
        1300, 1500, 0.78, 256, min_triangles=800, body_height_m=0.78, team="ally"
    ),
    "enemy_soldier": AssetSpec(
        "enemy_soldier", "Enemy soldier",
        ("enemy_soldier", "enemy-soldier", "red_soldier", "red-soldier", "enemy", "red"),
        1300, 1500, 1.09, 256, min_triangles=800, body_height_m=0.78, team="enemy"
    ),
    "crossbow_tower": AssetSpec(
        "crossbow_tower", "Crossbow tower",
        ("crossbow_tower", "crossbow-tower", "tower", "crossbow"),
        4500, 5000, 2.05, 512
    ),
    "barracks": AssetSpec(
        "barracks", "Barracks", ("barracks", "barrack", "byeongyeong"),
        4500, 5000, 1.845, 512
    ),
    "giant": AssetSpec(
        "giant", "Enemy giant", ("giant", "enemy_giant", "enemy-giant", "geo-in"),
        3500, 4000, 1.95, 512
    ),
}
ASSET_ORDER = tuple(ASSET_SPECS)

# Selected Meshy variants.  The first soldier attempts deliberately remain in
# the raw audit log, but A-pose generation removed their held equipment.
SELECTED_RAW_DIRS = {
    "king": "remeshed/king",
    "ally_soldier": "remeshed/ally-soldier",
    "enemy_soldier": "remeshed/enemy-soldier",
    "crossbow_tower": "remeshed/crossbow-tower",
    "barracks": "remeshed/barracks",
    "giant": "remeshed/enemy-giant",
}


class PipelineError(RuntimeError):
    pass


def canonical_asset(value: str) -> str:
    normalized = value.strip().lower().replace("-", "_")
    if normalized in ASSET_SPECS:
        return normalized
    for asset_id, spec in ASSET_SPECS.items():
        if value.strip().lower() in spec.aliases:
            return asset_id
    raise argparse.ArgumentTypeError(
        "unknown asset %r (choose one of: %s)" % (value, ", ".join(ASSET_ORDER))
    )


def blender_argv() -> List[str]:
    return sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def parse_args(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Optimize six Meshy GLBs into FBX + one RGBA atlas each."
    )
    parser.add_argument("--raw-dir", type=Path, default=DEFAULT_RAW,
                        help="Meshy download root (default: %(default)s)")
    parser.add_argument("--out-dir", type=Path, default=DEFAULT_OUT,
                        help="FBX/PNG/manifest output root (default: %(default)s)")
    parser.add_argument("--preview-dir", type=Path, default=DEFAULT_PREVIEWS,
                        help="Per-asset preview output root (default: %(default)s)")
    selection = parser.add_mutually_exclusive_group(required=True)
    selection.add_argument("--asset", type=canonical_asset,
                           help="Process one asset: %s" % ", ".join(ASSET_ORDER))
    selection.add_argument("--all", action="store_true", help="Process all six assets")
    parser.add_argument("--dry-run", action="store_true",
                        help="Resolve inputs and print the plan without creating files")
    return parser.parse_args(list(argv) if argv is not None else blender_argv())


def absolute(path: Path) -> Path:
    return path.expanduser().resolve()


def display_path(path: Path) -> str:
    resolved = absolute(path)
    try:
        return str(resolved.relative_to(REPO_ROOT))
    except ValueError:
        return str(resolved)


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def safe_output_dir(path: Path, label: str) -> Path:
    resolved = absolute(path)
    allowed_repo_root = absolute(ART_SOURCE / "ai3d")
    if resolved == REPO_ROOT or REPO_ROOT in resolved.parents:
        if resolved != allowed_repo_root and allowed_repo_root not in resolved.parents:
            raise PipelineError(
                f"{label} inside this repository must stay below {allowed_repo_root}"
            )
    forbidden = [
        REPO_ROOT / "public",
        REPO_ROOT / "factory/unity-src/hyeopgok-sasu/Resources",
        REPO_ROOT / "factory/lib",
        REPO_ROOT / "factory/prompts",
    ]
    for root in forbidden:
        root = absolute(root)
        if resolved == root or root in resolved.parents:
            raise PipelineError(f"{label} may not be inside protected path {root}")
    return resolved


def normalized_token(value: str) -> str:
    return "".join(ch if ch.isalnum() else "_" for ch in value.lower())


def find_raw_glb(raw_dir: Path, spec: AssetSpec) -> Path:
    raw_dir = absolute(raw_dir)
    if not raw_dir.is_dir():
        raise PipelineError(f"raw directory does not exist: {raw_dir}")
    selected = raw_dir / SELECTED_RAW_DIRS[spec.asset_id] / "model.glb"
    if selected.is_file():
        return selected
    if selected.parent.exists():
        raise PipelineError(f"selected GLB is missing: {selected}")
    candidates = sorted(raw_dir.rglob("*.glb"))
    if not candidates:
        raise PipelineError(f"no GLB files below {raw_dir}")

    aliases = tuple(normalized_token(alias) for alias in spec.aliases)
    scored: List[Tuple[int, int, Path]] = []
    for path in candidates:
        relative = normalized_token(str(path.relative_to(raw_dir)))
        stem = normalized_token(path.stem)
        parent = normalized_token(path.parent.name)
        score = 0
        for alias in aliases:
            if stem == alias:
                score = max(score, 100)
            if parent == alias:
                score = max(score, 95)
            if alias in relative:
                score = max(score, 70 + min(20, len(alias)))
        # Meshy downloads are commonly <asset>/model.glb.
        if path.name.lower() == "model.glb" and any(a in parent for a in aliases):
            score += 10
        if score:
            scored.append((score, -len(path.parts), path))
    if not scored:
        raise PipelineError(
            f"could not match {spec.asset_id}; GLBs found: "
            + ", ".join(display_path(path) for path in candidates)
        )
    scored.sort(reverse=True)
    best_score = scored[0][0]
    tied = [row[2] for row in scored if row[0] == best_score]
    if len(tied) > 1:
        raise PipelineError(
            f"ambiguous GLB for {spec.asset_id}: "
            + ", ".join(display_path(path) for path in tied)
        )
    return scored[0][2]


def load_orientation_overrides(raw_dir: Path) -> Dict[str, float]:
    """Optional non-destructive yaw corrections for raw providers.

    ``orientation.json`` may contain ``{"king": {"yaw_degrees": 180}}``.
    Heights and budgets intentionally cannot be overridden here.
    """
    path = absolute(raw_dir) / "orientation.json"
    if not path.exists():
        return {}
    data = json.loads(path.read_text(encoding="utf-8"))
    result: Dict[str, float] = {}
    for asset_id, value in data.items():
        key = canonical_asset(asset_id)
        if isinstance(value, dict):
            value = value.get("yaw_degrees", 0)
        result[key] = float(value)
    return result


def reset_scene() -> None:
    if bpy.context.object and bpy.context.object.mode != "OBJECT":
        bpy.ops.object.mode_set(mode="OBJECT")
    # Operators cannot select objects hidden by the previous preview pass.  A
    # direct datablock removal is therefore required before fresh-reimport
    # validation; otherwise the hidden high-poly source contaminates the mesh
    # count and bounds.
    for obj in list(bpy.data.objects):
        bpy.data.objects.remove(obj, do_unlink=True)
    for collection in (
        bpy.data.meshes, bpy.data.materials, bpy.data.cameras,
        bpy.data.lights, bpy.data.armatures, bpy.data.curves,
    ):
        for block in list(collection):
            if block.users == 0:
                collection.remove(block)
    for image in list(bpy.data.images):
        if image.name not in {"Render Result", "Viewer Node"} and image.users == 0:
            bpy.data.images.remove(image)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0


def triangle_count(mesh: bpy.types.Mesh) -> int:
    mesh.calc_loop_triangles()
    return len(mesh.loop_triangles)


def mesh_bounds(mesh: bpy.types.Mesh) -> Tuple[Vector, Vector]:
    if not mesh.vertices:
        raise PipelineError("mesh has no vertices")
    minimum = Vector((math.inf, math.inf, math.inf))
    maximum = Vector((-math.inf, -math.inf, -math.inf))
    for vertex in mesh.vertices:
        for axis in range(3):
            minimum[axis] = min(minimum[axis], vertex.co[axis])
            maximum[axis] = max(maximum[axis], vertex.co[axis])
    return minimum, maximum


def logical_bounds(mesh: bpy.types.Mesh) -> Tuple[List[float], List[float]]:
    # Blender x/y/z -> Unity logical x/z/y, with logical +Z == Blender -Y.
    points = [(v.co.x, v.co.z, -v.co.y) for v in mesh.vertices]
    minimum = [min(point[i] for point in points) for i in range(3)]
    maximum = [max(point[i] for point in points) for i in range(3)]
    return ([round(value, 5) for value in minimum],
            [round(value, 5) for value in maximum])


def import_static_joined(glb_path: Path, asset_id: str) -> bpy.types.Object:
    reset_scene()
    bpy.ops.import_scene.gltf(filepath=str(glb_path), import_pack_images=True)
    source_meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not source_meshes:
        raise PipelineError(f"{asset_id}: GLB contains no mesh objects")

    depsgraph = bpy.context.evaluated_depsgraph_get()
    baked_objects: List[bpy.types.Object] = []
    for source in source_meshes:
        evaluated = source.evaluated_get(depsgraph)
        mesh = bpy.data.meshes.new_from_object(
            evaluated, preserve_all_data_layers=True, depsgraph=depsgraph
        )
        world = source.matrix_world.copy()
        mesh.transform(world)
        if world.to_3x3().determinant() < 0:
            mesh.flip_normals()
        mesh.validate(verbose=False, clean_customdata=False)
        mesh.update()
        baked = bpy.data.objects.new(f"{asset_id}_static_part", mesh)
        bpy.context.collection.objects.link(baked)
        baked_objects.append(baked)

    # Remove the imported hierarchy, including armatures, lights and cameras.
    baked_set = set(baked_objects)
    for obj in list(bpy.data.objects):
        if obj not in baked_set:
            bpy.data.objects.remove(obj, do_unlink=True)

    bpy.ops.object.select_all(action="DESELECT")
    for obj in baked_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = baked_objects[0]
    if len(baked_objects) > 1:
        bpy.ops.object.join()
    joined = bpy.context.view_layer.objects.active
    joined.name = asset_id + "_high"
    joined.data.name = asset_id + "_high_mesh"
    joined.location = Vector((0, 0, 0))
    joined.rotation_euler = Vector((0, 0, 0))
    joined.scale = Vector((1, 1, 1))
    if triangle_count(joined.data) == 0:
        raise PipelineError(f"{asset_id}: joined mesh has no triangles")
    return joined


def normalize_mesh(obj: bpy.types.Object, spec: AssetSpec, yaw_degrees: float) -> Dict[str, object]:
    mesh = obj.data
    if yaw_degrees:
        mesh.transform(Matrix.Rotation(math.radians(yaw_degrees), 4, "Z"))
    minimum, maximum = mesh_bounds(mesh)
    source_height = maximum.z - minimum.z
    if source_height <= 1e-6:
        raise PipelineError(f"{spec.asset_id}: zero-height mesh")
    scale = spec.target_height_m / source_height
    mesh.transform(Matrix.Scale(scale, 4))
    minimum, maximum = mesh_bounds(mesh)
    offset = Vector((-(minimum.x + maximum.x) * 0.5,
                     -(minimum.y + maximum.y) * 0.5,
                     -minimum.z))
    mesh.transform(Matrix.Translation(offset))
    mesh.update()
    after_min, after_max = mesh_bounds(mesh)
    return {
        "assumed_front": "Blender -Y / Unity logical +Z",
        "yaw_degrees": yaw_degrees,
        "source_height_m": round(source_height, 6),
        "uniform_scale": round(scale, 8),
        "blender_bounds_min": [round(v, 5) for v in after_min],
        "blender_bounds_max": [round(v, 5) for v in after_max],
    }


def duplicate_object(source: bpy.types.Object, name: str) -> bpy.types.Object:
    duplicate = source.copy()
    duplicate.data = source.data.copy()
    duplicate.name = name
    duplicate.data.name = name + "_mesh"
    bpy.context.collection.objects.link(duplicate)
    return duplicate


def apply_modifier(obj: bpy.types.Object, modifier: bpy.types.Modifier) -> None:
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)


def decimate_and_triangulate(low: bpy.types.Object, spec: AssetSpec) -> Dict[str, object]:
    source_triangles = triangle_count(low.data)
    if source_triangles > spec.target_triangles:
        modifier = low.modifiers.new("AI3D triangle budget", "DECIMATE")
        modifier.decimate_type = "COLLAPSE"
        modifier.ratio = max(0.01, min(1.0, spec.target_triangles / source_triangles))
        modifier.use_collapse_triangulate = True
        apply_modifier(low, modifier)

    triangulate = low.modifiers.new("AI3D final triangles", "TRIANGULATE")
    triangulate.quad_method = "BEAUTY"
    triangulate.ngon_method = "BEAUTY"
    apply_modifier(low, triangulate)
    current = triangle_count(low.data)

    if current > spec.max_triangles:
        modifier = low.modifiers.new("AI3D hard cap", "DECIMATE")
        modifier.decimate_type = "COLLAPSE"
        modifier.ratio = max(0.01, min(1.0, spec.max_triangles * 0.985 / current))
        modifier.use_collapse_triangulate = True
        apply_modifier(low, modifier)
        triangulate = low.modifiers.new("AI3D hard-cap triangles", "TRIANGULATE")
        apply_modifier(low, triangulate)
        current = triangle_count(low.data)

    low.data.validate(verbose=False, clean_customdata=False)
    low.data.update()
    if current > spec.max_triangles:
        raise PipelineError(
            f"{spec.asset_id}: {current} triangles exceed cap {spec.max_triangles}"
        )
    if spec.min_triangles and current < spec.min_triangles:
        raise PipelineError(
            f"{spec.asset_id}: source only yields {current} triangles; required range is "
            f"{spec.min_triangles}-{spec.max_triangles}. Refine in Meshy instead of fake subdivision."
        )
    return {
        "source_triangles": source_triangles,
        "target_triangles": spec.target_triangles,
        "final_triangles": current,
        "hard_min": spec.min_triangles or None,
        "hard_max": spec.max_triangles,
    }


def renormalize_pair_after_decimation(
    high: bpy.types.Object, low: bpy.types.Object, spec: AssetSpec
) -> Dict[str, object]:
    """Restore exact export bounds without changing high/low correspondence.

    Collapse decimation can move or remove the vertices that defined the source
    extrema.  Apply one shared transform to both meshes so the low mesh meets
    the metre-scale/origin contract while selected-to-active bake rays retain
    their pre-correction geometric correspondence.
    """
    before_min, before_max = mesh_bounds(low.data)
    height = before_max.z - before_min.z
    if height <= 1e-6:
        raise PipelineError(f"{spec.asset_id}: zero height after decimation")
    correction = spec.target_height_m / height
    scale_matrix = Matrix.Scale(correction, 4)
    high.data.transform(scale_matrix)
    low.data.transform(scale_matrix)

    scaled_min, scaled_max = mesh_bounds(low.data)
    offset = Vector((
        -(scaled_min.x + scaled_max.x) * 0.5,
        -(scaled_min.y + scaled_max.y) * 0.5,
        -scaled_min.z,
    ))
    translation = Matrix.Translation(offset)
    high.data.transform(translation)
    low.data.transform(translation)
    high.data.update()
    low.data.update()
    final_min, final_max = mesh_bounds(low.data)
    return {
        "pre_height_m": round(height, 6),
        "uniform_scale": round(correction, 8),
        "translation": [round(value, 6) for value in offset],
        "final_height_m": round(final_max.z - final_min.z, 6),
        "final_ground_m": round(final_min.z, 6),
        "final_center_xy_m": [
            round((final_min.x + final_max.x) * 0.5, 6),
            round((final_min.y + final_max.y) * 0.5, 6),
        ],
    }


def unwrap_atlas(obj: bpy.types.Object) -> None:
    mesh = obj.data
    while mesh.uv_layers:
        mesh.uv_layers.remove(mesh.uv_layers[0])
    mesh.uv_layers.new(name="BaseColorUV")
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(
        angle_limit=math.radians(66.0), island_margin=0.02,
        correct_aspect=True, scale_to_bounds=True
    )
    bpy.ops.object.mode_set(mode="OBJECT")
    mesh.uv_layers.active_index = 0


def make_bake_material(image: bpy.types.Image, name: str) -> bpy.types.Material:
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    nodes = material.node_tree.nodes
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    texture = nodes.new("ShaderNodeTexImage")
    texture.name = "AI3D_ATLAS"
    texture.image = image
    texture.interpolation = "Linear"
    nodes.active = texture
    # Keep the active image node disconnected during selected-to-active bake.
    # Reading from the same image being written produces a circular dependency
    # and can leave atlas pixels unchanged on Blender 5.x.
    bsdf.inputs["Base Color"].default_value = (0.5, 0.5, 0.5, 1.0)
    material.node_tree.links.new(bsdf.outputs["BSDF"], output.inputs["Surface"])
    bsdf.inputs["Roughness"].default_value = 0.78
    return material


def ensure_source_material(high: bpy.types.Object) -> None:
    if high.data.materials:
        return
    material = bpy.data.materials.new(high.name + "_fallback_source")
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = (0.5, 0.52, 0.55, 1)
        bsdf.inputs["Roughness"].default_value = 0.8
    high.data.materials.append(material)


def bake_base_color(high: bpy.types.Object, low: bpy.types.Object,
                    spec: AssetSpec) -> Tuple[bpy.types.Image, bpy.types.Material]:
    size = spec.atlas_size
    image = bpy.data.images.new(
        spec.asset_id + "_atlas", width=size, height=size,
        alpha=True, float_buffer=False
    )
    image.alpha_mode = "STRAIGHT"
    image.colorspace_settings.name = "sRGB"
    # Unmistakable sentinel lets mask generation distinguish unoccupied pixels.
    image.generated_color = (0.9375, 0.03125, 0.96875, 1.0)
    material = make_bake_material(image, spec.asset_id + "_single_material")
    low.data.materials.clear()
    low.data.materials.append(material)
    for polygon in low.data.polygons:
        polygon.material_index = 0
    ensure_source_material(high)

    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 1
    scene.cycles.device = "CPU"
    scene.render.bake.use_selected_to_active = True
    scene.render.bake.use_pass_direct = False
    scene.render.bake.use_pass_indirect = False
    scene.render.bake.use_pass_color = True
    # Preserve the sentinel in texels not reached by the bake.  Clearing here
    # would turn every gutter black and make mask coverage validation report
    # the whole atlas as authored content.
    scene.render.bake.use_clear = False
    scene.render.bake.margin = 8 if size >= 512 else 4
    scene.render.bake.cage_extrusion = max(0.0025, spec.target_height_m * 0.012)
    scene.render.bake.max_ray_distance = max(0.006, spec.target_height_m * 0.035)

    bpy.ops.object.select_all(action="DESELECT")
    high.hide_render = False
    high.select_set(True)
    low.select_set(True)
    bpy.context.view_layer.objects.active = low
    try:
        bpy.ops.object.bake(type="DIFFUSE")
    except RuntimeError as exc:
        raise PipelineError(f"{spec.asset_id}: base-colour bake failed: {exc}") from exc
    high.hide_render = True
    image.update()
    return image, material


def linear_to_srgb(value: float) -> float:
    value = max(0.0, min(1.0, value))
    if value <= 0.0031308:
        return value * 12.92
    return 1.055 * (value ** (1 / 2.4)) - 0.055


def srgb_to_linear(value: float) -> float:
    value = max(0.0, min(1.0, value))
    if value <= 0.04045:
        return value / 12.92
    return ((value + 0.055) / 1.055) ** 2.4


def is_sentinel(r: float, g: float, b: float) -> bool:
    # ``Image.generated_color`` is stored verbatim in ``Image.pixels`` even
    # when the image colour space is tagged sRGB.  Baked texels, in contrast,
    # are scene-linear and are converted below only for HSV classification.
    return abs(r - 0.9375) < 0.025 and abs(g - 0.03125) < 0.025 and abs(b - 0.96875) < 0.025


def team_candidate(team: str, r: float, g: float, b: float) -> bool:
    sr, sg, sb = (linear_to_srgb(r), linear_to_srgb(g), linear_to_srgb(b))
    hue, saturation, value = colorsys.rgb_to_hsv(sr, sg, sb)
    if value < 0.075:
        return False
    if team == "ally":
        return 0.50 <= hue <= 0.72 and saturation >= 0.28 and sb > sr * 1.08
    return (hue <= 0.055 or hue >= 0.94) and saturation >= 0.50 and sr > sg * 1.12


def apply_texture_team_mask(image: bpy.types.Image, spec: AssetSpec) -> Dict[str, object]:
    pixels = list(image.pixels[:])
    occupied = 0
    candidates = 0
    for index in range(0, len(pixels), 4):
        r, g, b = pixels[index:index + 3]
        if is_sentinel(r, g, b):
            # Neutral gutter prevents magenta mip bleed in later Unity import.
            pixels[index:index + 4] = [0.18, 0.18, 0.18, 1.0]
            continue
        occupied += 1
        tintable = bool(spec.team and team_candidate(spec.team, r, g, b))
        if tintable:
            luminance = max(0.055, min(0.82, 0.2126 * r + 0.7152 * g + 0.0722 * b))
            pixels[index:index + 4] = [luminance, luminance, luminance, 0.0]
            candidates += 1
        else:
            pixels[index + 3] = 1.0
    image.pixels[:] = pixels
    image.update()

    coverage = candidates / max(1, occupied)
    uncertain = bool(spec.team and (coverage < 0.04 or coverage > 0.68))
    if not spec.team:
        confidence = "not-applicable"
    elif uncertain:
        confidence = "low"
    elif coverage < 0.09 or coverage > 0.55:
        confidence = "medium"
    else:
        confidence = "candidate"
    return {
        "encoding": "texture alpha: 0 tintable / 1 fixed",
        "method": "HSV candidate from authored blue/red pixels; RGB neutralized to luminance",
        "team": spec.team,
        "occupied_pixels": occupied,
        "tintable_pixels": candidates,
        "tintable_ratio": round(coverage, 6),
        "confidence": confidence,
        "manual_review_required": bool(spec.team),
        "uncertain": uncertain,
    }


def ao_directions(samples: int) -> List[Vector]:
    directions: List[Vector] = []
    golden = math.pi * (3 - math.sqrt(5))
    for index in range(samples):
        radius = math.sqrt((index + 0.5) / samples)
        phi = index * golden
        directions.append(Vector((
            math.cos(phi) * radius,
            math.sin(phi) * radius,
            math.sqrt(max(0.0, 1 - radius * radius)),
        )))
    return directions


def bake_vertex_ao(obj: bpy.types.Object, samples: int = AO_SAMPLES) -> Dict[str, object]:
    mesh = obj.data
    mesh.update()
    points = [vertex.co.copy() for vertex in mesh.vertices]
    polygons = [tuple(poly.vertices) for poly in mesh.polygons]
    minimum, maximum = mesh_bounds(mesh)
    extent = maximum - minimum
    radius = min(2.1, max(0.24, max(extent) * 0.52))

    # A temporary implicit floor produces contact AO but is not exported.
    span = max(4.0, max(extent) * 3.0)
    floor_z = minimum.z - 0.006
    offset = len(points)
    points.extend([
        Vector((-span, -span, floor_z)), Vector((span, -span, floor_z)),
        Vector((span, span, floor_z)), Vector((-span, span, floor_z)),
    ])
    polygons.append((offset, offset + 1, offset + 2, offset + 3))
    tree = BVHTree.FromPolygons(points, polygons, all_triangles=False, epsilon=0.0)

    old = mesh.color_attributes.get("Color")
    if old:
        mesh.color_attributes.remove(old)
    color = mesh.color_attributes.new(name="Color", type="FLOAT_COLOR", domain="CORNER")
    directions = ao_directions(samples)
    values: List[float] = []
    hits = 0
    for polygon in mesh.polygons:
        normal = polygon.normal.normalized()
        helper = Vector((0, 0, 1)) if abs(normal.z) < 0.92 else Vector((0, 1, 0))
        tangent = normal.cross(helper).normalized()
        bitangent = normal.cross(tangent).normalized()
        center = polygon.center
        for loop_index in polygon.loop_indices:
            vertex = mesh.vertices[mesh.loops[loop_index].vertex_index].co
            origin = vertex.lerp(center, 0.035) + normal * 0.0025
            obscured = 0.0
            for sample in directions:
                direction = tangent * sample.x + bitangent * sample.y + normal * sample.z
                location, _hit_normal, _hit_face, distance = tree.ray_cast(origin, direction, radius)
                if location is not None:
                    obscured += (1 - (distance / radius) ** 2) ** 2
                    hits += 1
            ao = max(AO_MIN, 1 - AO_STRENGTH * obscured / samples)
            color.data[loop_index].color_srgb = (1.0, 1.0, 1.0, ao)
            values.append(ao)
    mesh.color_attributes.active_color = color
    mesh.color_attributes.render_color_index = list(mesh.color_attributes).index(color)
    return {
        "method": "BVHTree cosine-weighted hemisphere rays plus temporary floor",
        "samples_per_corner": samples,
        "rays": len(values) * samples,
        "hits": hits,
        "radius_m": round(radius, 5),
        "min": round(min(values), 6),
        "max": round(max(values), 6),
        "mean": round(sum(values) / len(values), 6),
        "darkened_corners": sum(value < 0.97 for value in values),
        "channel": "COLOR.a",
    }


def mesh_topology(mesh: bpy.types.Mesh) -> Dict[str, int]:
    bm = bmesh.new()
    bm.from_mesh(mesh)
    bm.verts.ensure_lookup_table()
    unseen = set(bm.verts)
    components = 0
    while unseen:
        components += 1
        stack = [unseen.pop()]
        while stack:
            vertex = stack.pop()
            for edge in vertex.link_edges:
                other = edge.other_vert(vertex)
                if other in unseen:
                    unseen.remove(other)
                    stack.append(other)
    result = {
        "connected_components": components,
        "non_manifold_edges": sum(1 for edge in bm.edges if not edge.is_manifold),
        "boundary_edges": sum(1 for edge in bm.edges if edge.is_boundary),
        "loose_vertices": sum(1 for vertex in bm.verts if not vertex.link_faces),
    }
    bm.free()
    return result


def hex_linear(value: str) -> Tuple[float, float, float, float]:
    value = value.lstrip("#")
    rgb = [int(value[index:index + 2], 16) / 255 for index in (0, 2, 4)]
    return tuple(srgb_to_linear(channel) for channel in rgb) + (1.0,)


def configure_final_material(material: bpy.types.Material, image: bpy.types.Image,
                             spec: AssetSpec) -> None:
    material.use_nodes = True
    nodes = material.node_tree.nodes
    links = material.node_tree.links
    nodes.clear()
    output = nodes.new("ShaderNodeOutputMaterial")
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
    texture = nodes.new("ShaderNodeTexImage")
    texture.name = "AI3D_BASECOLOR_TEAM_MASK"
    texture.image = image
    texture.interpolation = "Linear"
    texture.extension = "EXTEND"
    vertex = nodes.new("ShaderNodeVertexColor")
    vertex.layer_name = "Color"

    team_hex = "#0C73D5" if spec.team == "ally" else "#D0060C"
    team_color = hex_linear(team_hex) if spec.team else (1, 1, 1, 1)
    tint = nodes.new("ShaderNodeMixRGB")
    tint.name = "Neutral RGB x team colour"
    tint.blend_type = "MULTIPLY"
    tint.inputs[0].default_value = 1.0
    tint.inputs[2].default_value = team_color
    links.new(texture.outputs["Color"], tint.inputs[1])

    fixed = nodes.new("ShaderNodeMixRGB")
    fixed.name = "Alpha chooses tintable or fixed"
    links.new(texture.outputs["Alpha"], fixed.inputs[0])
    links.new(tint.outputs[0], fixed.inputs[1])
    links.new(texture.outputs["Color"], fixed.inputs[2])

    ao = nodes.new("ShaderNodeMixRGB")
    ao.name = "Vertex AO"
    ao.blend_type = "MULTIPLY"
    ao.inputs[0].default_value = 1.0
    links.new(fixed.outputs[0], ao.inputs[1])
    links.new(vertex.outputs["Alpha"], ao.inputs[2])
    links.new(ao.outputs[0], bsdf.inputs["Base Color"])
    links.new(bsdf.outputs["BSDF"], output.inputs["Surface"])
    bsdf.inputs["Roughness"].default_value = 0.74
    bsdf.inputs["Metallic"].default_value = 0.04
    material.diffuse_color = (0.6, 0.6, 0.6, 1)


def save_texture(image: bpy.types.Image, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    image.filepath_raw = str(path)
    image.file_format = "PNG"
    image.save()
    if not path.is_file() or path.stat().st_size == 0:
        raise PipelineError(f"texture was not written: {path}")


def validate_saved_texture(path: Path, spec: AssetSpec,
                           mask: Dict[str, object]) -> Dict[str, object]:
    """Reload the PNG so the manifest describes the file, not RAM state."""
    fresh = bpy.data.images.load(str(path), check_existing=False)
    try:
        width, height = (int(value) for value in fresh.size)
        pixels = list(fresh.pixels[:])
        alpha = pixels[3::4]
        tintable = sum(value < 0.5 for value in alpha)
        failures: List[str] = []
        if (width, height) != (spec.atlas_size, spec.atlas_size):
            failures.append(
                f"PNG size {width}x{height} != {spec.atlas_size}x{spec.atlas_size}"
            )
        if fresh.channels != 4:
            failures.append(f"PNG channels {fresh.channels} != 4 (RGBA)")
        expected_tintable = int(mask["tintable_pixels"])
        if tintable != expected_tintable:
            failures.append(
                f"PNG tint-mask pixels {tintable} != in-memory {expected_tintable}"
            )
        result = {
            "fresh_reload": True,
            "valid": not failures,
            "hard_failures": failures,
            "width": width,
            "height": height,
            "channels": int(fresh.channels),
            "alpha_min": round(min(alpha), 6),
            "alpha_max": round(max(alpha), 6),
            "tintable_pixels_alpha_lt_0_5": tintable,
        }
    finally:
        bpy.data.images.remove(fresh)
    if result["hard_failures"]:
        raise PipelineError(
            f"{spec.asset_id}: texture validation failed: "
            + "; ".join(result["hard_failures"])
        )
    return result


def export_fbx(obj: bpy.types.Object, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    obj.location = Vector((0, 0, 0))
    obj.rotation_euler = Vector((0, 0, 0))
    obj.scale = Vector((1, 1, 1))
    bpy.ops.export_scene.fbx(
        filepath=str(path), use_selection=True, object_types={"MESH"},
        global_scale=1.0, apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        axis_forward="-Z", axis_up="Y", bake_space_transform=True,
        use_mesh_modifiers=True, mesh_smooth_type="FACE", use_tspace=False,
        add_leaf_bones=False, bake_anim=False, path_mode="RELATIVE",
        embed_textures=False, colors_type="SRGB", use_custom_props=False,
    )
    if not path.is_file() or path.stat().st_size == 0:
        raise PipelineError(f"FBX was not written: {path}")


def look_at(obj: bpy.types.Object, target: Vector) -> None:
    obj.rotation_euler = (target - obj.location).to_track_quat("-Z", "Y").to_euler()


def add_studio(scene: bpy.types.Scene, center: Vector, extent: Vector,
               resolution: Tuple[int, int]) -> bpy.types.Object:
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x, scene.render.resolution_y = resolution
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "Medium High Contrast"
    scene.view_settings.exposure = 0.0
    scene.view_settings.gamma = 1.0
    scene.world.color = (0.92, 0.94, 0.95)
    world_nodes = scene.world.node_tree.nodes if scene.world.use_nodes else None
    if world_nodes is not None:
        background = world_nodes.get("Background")
        if background:
            background.inputs["Color"].default_value = (0.92, 0.94, 0.95, 1)
            background.inputs["Strength"].default_value = 0.8

    floor_material = bpy.data.materials.new("AI3D studio floor")
    floor_material.diffuse_color = (0.86, 0.88, 0.90, 1)
    bpy.ops.mesh.primitive_plane_add(size=max(30.0, extent.x * 5), location=(center.x, center.y, -0.006))
    floor = bpy.context.object
    floor.name = "AI3D preview floor"
    floor.data.materials.append(floor_material)

    def area(name: str, location: Tuple[float, float, float], energy: float,
             size: float, color: Tuple[float, float, float]) -> None:
        data = bpy.data.lights.new(name, "AREA")
        data.energy = energy
        data.shape = "DISK"
        data.size = size
        data.color = color
        light = bpy.data.objects.new(name, data)
        bpy.context.collection.objects.link(light)
        light.location = location
        look_at(light, center)

    radius = max(2.5, max(extent) * 1.8)
    area("Warm key", (center.x - radius, center.y - radius, center.z + radius * 1.5),
         850, radius * 0.9, (1.0, 0.86, 0.72))
    area("Cool fill", (center.x + radius, center.y + radius * 0.7, center.z + radius),
         500, radius, (0.64, 0.82, 1.0))

    camera_data = bpy.data.cameras.new("AI3D preview camera")
    camera = bpy.data.objects.new("AI3D preview camera", camera_data)
    bpy.context.collection.objects.link(camera)
    scene.camera = camera
    camera_data.type = "ORTHO"
    camera_data.ortho_scale = max(extent.z * 1.35, extent.x * 1.35, extent.y * 1.7, 1.0)
    distance = camera_data.ortho_scale * 2.2
    camera.location = center + Vector((distance * 0.58, -distance, distance * 0.50))
    look_at(camera, center + Vector((0, 0, extent.z * 0.02)))
    return camera


def render_asset_preview(obj: bpy.types.Object, spec: AssetSpec, path: Path) -> None:
    obj.hide_render = False
    minimum, maximum = mesh_bounds(obj.data)
    center = (minimum + maximum) * 0.5
    extent = maximum - minimum
    add_studio(bpy.context.scene, center, extent, (1024, 1024))
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    if not path.is_file() or path.stat().st_size == 0:
        raise PipelineError(f"preview was not written: {path}")


def fresh_reimport_validation(fbx_path: Path, spec: AssetSpec) -> Dict[str, object]:
    reset_scene()
    bpy.ops.import_scene.fbx(filepath=str(fbx_path), use_custom_normals=True)
    objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    if not objects:
        raise PipelineError(f"{spec.asset_id}: fresh FBX reimport has no mesh")
    triangles = 0
    logical_points: List[Tuple[float, float, float]] = []
    material_indices = set()
    material_slots = 0
    ao_values: List[float] = []
    uv_layers = 0
    for obj in objects:
        mesh = obj.data
        mesh.calc_loop_triangles()
        triangles += len(mesh.loop_triangles)
        uv_layers = max(uv_layers, len(mesh.uv_layers))
        material_slots += len(mesh.materials)
        material_indices.update((obj.name, polygon.material_index) for polygon in mesh.polygons)
        for vertex in mesh.vertices:
            point = obj.matrix_world @ vertex.co
            logical_points.append((point.x, point.z, -point.y))
        if mesh.color_attributes:
            attribute = mesh.color_attributes.get("Color") or mesh.color_attributes[0]
            ao_values.extend(float(item.color_srgb[3]) for item in attribute.data)

    minimum = [min(point[i] for point in logical_points) for i in range(3)]
    maximum = [max(point[i] for point in logical_points) for i in range(3)]
    height = maximum[1] - minimum[1]
    hard_failures: List[str] = []
    if len(objects) != 1:
        hard_failures.append(f"mesh objects {len(objects)} != 1")
    if len(material_indices) != 1:
        hard_failures.append(f"submeshes {len(material_indices)} != 1")
    if material_slots != 1:
        hard_failures.append(f"material slots {material_slots} != 1")
    if triangles > spec.max_triangles:
        hard_failures.append(f"triangles {triangles} > {spec.max_triangles}")
    if spec.min_triangles and triangles < spec.min_triangles:
        hard_failures.append(f"triangles {triangles} < {spec.min_triangles}")
    if abs(height - spec.target_height_m) > max(0.006, spec.target_height_m * 0.005):
        hard_failures.append(f"height {height:.5f} != {spec.target_height_m:.5f}")
    if abs(minimum[1]) > 0.006:
        hard_failures.append(f"ground logical y is {minimum[1]:.5f}, expected 0")
    center_x = (minimum[0] + maximum[0]) * 0.5
    center_z = (minimum[2] + maximum[2]) * 0.5
    if abs(center_x) > 0.006:
        hard_failures.append(f"logical x center is {center_x:.5f}, expected 0")
    if abs(center_z) > 0.006:
        hard_failures.append(f"logical z center is {center_z:.5f}, expected 0")
    if uv_layers < 1:
        hard_failures.append("missing base-colour UV")
    if not ao_values:
        hard_failures.append("missing vertex colour AO")
    elif max(ao_values) - min(ao_values) < 0.01:
        hard_failures.append("vertex AO is effectively flat")
    return {
        "fresh_reimport": True,
        "valid": not hard_failures,
        "hard_failures": hard_failures,
        "mesh_objects": len(objects),
        "submesh_keys": len(material_indices),
        "material_slots": material_slots,
        "triangles": triangles,
        "uv_layers": uv_layers,
        "bounds_min": [round(value, 5) for value in minimum],
        "bounds_max": [round(value, 5) for value in maximum],
        "height_m": round(height, 6),
        "center_xz_m": [round(center_x, 6), round(center_z, 6)],
        "ao_min": round(min(ao_values), 6) if ao_values else None,
        "ao_max": round(max(ao_values), 6) if ao_values else None,
    }


def process_asset(spec: AssetSpec, raw_path: Path, out_dir: Path,
                  preview_dir: Path, yaw_degrees: float) -> Dict[str, object]:
    print(f"AI3D_START {spec.asset_id} raw={display_path(raw_path)}", flush=True)
    high = import_static_joined(raw_path, spec.asset_id)
    imported = {
        "mesh_objects_joined": 1,
        "source_triangles": triangle_count(high.data),
        "source_materials": len(high.data.materials),
    }
    normalization = normalize_mesh(high, spec, yaw_degrees)
    low = duplicate_object(high, spec.asset_id)
    budgets = decimate_and_triangulate(low, spec)
    normalization["post_decimation"] = renormalize_pair_after_decimation(
        high, low, spec
    )
    unwrap_atlas(low)
    image, material = bake_base_color(high, low, spec)
    mask = apply_texture_team_mask(image, spec)
    ao = bake_vertex_ao(low)
    topology = mesh_topology(low.data)
    configure_final_material(material, image, spec)
    low.data.materials.clear()
    low.data.materials.append(material)
    for polygon in low.data.polygons:
        polygon.material_index = 0
    low.name = spec.asset_id
    low.data.name = spec.asset_id + "_mesh"
    low.location = Vector((0, 0, 0))
    low.rotation_euler = Vector((0, 0, 0))
    low.scale = Vector((1, 1, 1))
    high.hide_render = True
    high.hide_viewport = True

    texture_path = out_dir / f"{spec.asset_id}.png"
    fbx_path = out_dir / f"{spec.asset_id}.fbx"
    preview_path = preview_dir / f"{spec.asset_id}.png"
    save_texture(image, texture_path)
    texture_validation = validate_saved_texture(texture_path, spec, mask)
    export_fbx(low, fbx_path)
    render_asset_preview(low, spec, preview_path)
    local_min, local_max = logical_bounds(low.data)
    validation = fresh_reimport_validation(fbx_path, spec)
    if not validation["valid"]:
        raise PipelineError(
            f"{spec.asset_id}: fresh reimport validation failed: "
            + "; ".join(validation["hard_failures"])
        )

    warnings: List[str] = []
    if mask["uncertain"]:
        warnings.append("automatic soldier tint-mask coverage is uncertain; inspect texture alpha manually")
    if topology["non_manifold_edges"]:
        warnings.append(f"mesh has {topology['non_manifold_edges']} non-manifold edges")
    if topology["connected_components"] > 24:
        warnings.append(f"mesh has {topology['connected_components']} disconnected components")
    if spec.body_height_m and spec.asset_id == "enemy_soldier":
        warnings.append("enemy body height 0.78m is a visual target; only total spear height 1.09m is automatic")

    result = {
        "id": spec.asset_id,
        "label": spec.label,
        "source": {
            "glb": display_path(raw_path),
            "bytes": raw_path.stat().st_size,
            "sha256": sha256(raw_path),
        },
        "spec": asdict(spec),
        "import": imported,
        "normalization": normalization,
        "triangles": budgets,
        "texture": {
            "path": display_path(texture_path),
            "width": spec.atlas_size,
            "height": spec.atlas_size,
            "bytes": texture_path.stat().st_size,
            "sha256": sha256(texture_path),
            "channels": "RGB base colour; A team mask (0 tintable / 1 fixed)",
            "validation": texture_validation,
        },
        "bounds_min": local_min,
        "bounds_max": local_max,
        "ao": ao,
        "team_mask": mask,
        "topology": topology,
        "files": {
            "fbx": display_path(fbx_path),
            "fbx_bytes": fbx_path.stat().st_size,
            "fbx_sha256": sha256(fbx_path),
            "preview": display_path(preview_path),
            "preview_sha256": sha256(preview_path),
        },
        "validation": validation,
        "warnings": warnings,
    }
    print(
        "AI3D_DONE %s triangles=%s texture=%sx%s" % (
            spec.asset_id, budgets["final_triangles"], spec.atlas_size, spec.atlas_size
        ),
        flush=True,
    )
    return result


def import_lineup_asset(spec: AssetSpec, fbx_path: Path, texture_path: Path) -> Tuple[bpy.types.Object, List[bpy.types.Object]]:
    before = set(bpy.context.scene.objects)
    bpy.ops.import_scene.fbx(filepath=str(fbx_path), use_custom_normals=True)
    imported = [obj for obj in bpy.context.scene.objects if obj not in before]
    meshes = [obj for obj in imported if obj.type == "MESH"]
    if not meshes:
        raise PipelineError(f"lineup import has no mesh for {spec.asset_id}")
    root = bpy.data.objects.new(spec.asset_id + "_lineup_root", None)
    bpy.context.collection.objects.link(root)
    for obj in imported:
        if obj.parent is None:
            world = obj.matrix_world.copy()
            obj.parent = root
            obj.matrix_world = world
    image = bpy.data.images.load(str(texture_path), check_existing=False)
    image.colorspace_settings.name = "sRGB"
    material = make_bake_material(image, spec.asset_id + "_lineup_material")
    configure_final_material(material, image, spec)
    for obj in meshes:
        obj.data.materials.clear()
        obj.data.materials.append(material)
        for polygon in obj.data.polygons:
            polygon.material_index = 0
    return root, meshes


def objects_world_bounds(objects: Iterable[bpy.types.Object]) -> Tuple[Vector, Vector]:
    minimum = Vector((math.inf, math.inf, math.inf))
    maximum = Vector((-math.inf, -math.inf, -math.inf))
    found = False
    for obj in objects:
        if obj.type != "MESH":
            continue
        for vertex in obj.data.vertices:
            point = obj.matrix_world @ vertex.co
            found = True
            for axis in range(3):
                minimum[axis] = min(minimum[axis], point[axis])
                maximum[axis] = max(maximum[axis], point[axis])
    if not found:
        raise PipelineError("cannot compute lineup bounds")
    return minimum, maximum


def render_lineup(asset_ids: Sequence[str], out_dir: Path,
                  preview_dir: Path) -> Optional[Path]:
    missing = [asset_id for asset_id in asset_ids
               if not (out_dir / f"{asset_id}.fbx").is_file()
               or not (out_dir / f"{asset_id}.png").is_file()]
    if missing:
        print("AI3D_LINEUP_SKIPPED missing=" + ",".join(missing), flush=True)
        return None
    reset_scene()
    entries = []
    cursor = 0.0
    gap = 0.48
    for asset_id in asset_ids:
        spec = ASSET_SPECS[asset_id]
        root, meshes = import_lineup_asset(
            spec, out_dir / f"{asset_id}.fbx", out_dir / f"{asset_id}.png"
        )
        minimum, maximum = objects_world_bounds(meshes)
        width = maximum.x - minimum.x
        root.location.x += cursor - minimum.x
        bpy.context.view_layer.update()
        cursor += width + gap
        entries.append((root, meshes, spec, width))

    all_meshes = [obj for _root, meshes, _spec, _width in entries for obj in meshes]
    minimum, maximum = objects_world_bounds(all_meshes)
    total_center_x = (minimum.x + maximum.x) * 0.5
    for root, _meshes, _spec, _width in entries:
        root.location.x -= total_center_x
    bpy.context.view_layer.update()
    minimum, maximum = objects_world_bounds(all_meshes)
    center = (minimum + maximum) * 0.5
    extent = maximum - minimum
    add_studio(bpy.context.scene, center, extent, (2200, 1200))
    camera = bpy.context.scene.camera
    # Ortho scale is vertical, so fit horizontal extent through the 2200:1200
    # aspect ratio rather than treating row width as a vertical measurement.
    # The depth allowance covers the oblique 3/4 projection without leaving the
    # assets as a narrow strip in the middle of the frame.
    aspect = 2200 / 1200
    camera.data.ortho_scale = max(
        extent.x / aspect * 1.82,
        extent.z * 1.58,
        extent.y * 0.58,
        2.8,
    )
    distance = camera.data.ortho_scale * 2.25
    camera.location = center + Vector((distance * 0.26, -distance, distance * 0.44))
    look_at(camera, center + Vector((0, 0, extent.z * 0.02)))
    # Keep custom test/output roots self-contained.  With the documented
    # default (ArtSource/ai3d/out), this still resolves to the required
    # ArtSource/ai3d/lineup.png.
    path = safe_output_dir(out_dir.parent, "lineup parent") / "lineup.png"
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(path)
    bpy.ops.render.render(write_still=True)
    if not path.is_file() or path.stat().st_size == 0:
        raise PipelineError("lineup render was not written")
    print("AI3D_LINEUP " + display_path(path), flush=True)
    return path


def manifest_path(out_dir: Path) -> Path:
    return out_dir / "manifest.json"


def read_manifest(out_dir: Path) -> Dict[str, object]:
    path = manifest_path(out_dir)
    if not path.exists():
        return {}
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError):
        return {}
    return data if isinstance(data, dict) else {}


def write_manifest(out_dir: Path, preview_dir: Path,
                   assets: Dict[str, object], lineup: Optional[Path]) -> Path:
    out_dir.mkdir(parents=True, exist_ok=True)
    path = manifest_path(out_dir)
    payload = {
        "schema_version": MANIFEST_VERSION,
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "generator": display_path(SCRIPT),
        "blender_version": bpy.app.version_string,
        "scope": "asset-only A stage; not installed into Unity Resources",
        "contracts": {
            "coordinates": "Blender Z up/front -Y; FBX -Z forward/+Y up; Unity logical x right/y up/z forward",
            "origin": "centered Blender X/Y, ground Z=0; metre scale",
            "mesh": "one static triangulated mesh, one submesh, one material",
            "uv0": "single base-colour atlas",
            "vertex_color": "COLOR.rgb white multiplier; COLOR.a geometric AO",
            "texture": "RGB base colour; alpha 0 team-tintable / 1 fixed",
            "runtime_warning": "current Hyeopgok Horde shader does not sample this texture; integration is a later stage",
        },
        "output_dir": display_path(out_dir),
        "preview_dir": display_path(preview_dir),
        "lineup": display_path(lineup) if lineup else None,
        "assets": [assets[key] for key in ASSET_ORDER if key in assets],
    }
    temporary = path.with_suffix(".json.tmp")
    temporary.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    os.replace(temporary, path)
    return path


def dry_run_plan(asset_ids: Sequence[str], raw_dir: Path,
                 out_dir: Path, preview_dir: Path) -> int:
    rows = []
    for asset_id in asset_ids:
        spec = ASSET_SPECS[asset_id]
        try:
            raw = find_raw_glb(raw_dir, spec)
            status = "ready"
            source = display_path(raw)
        except PipelineError as exc:
            status = "missing"
            source = str(exc)
        rows.append({
            "asset": asset_id,
            "status": status,
            "raw": source,
            "target_triangles": spec.target_triangles,
            "triangle_range": [spec.min_triangles or 0, spec.max_triangles],
            "height_m": spec.target_height_m,
            "atlas": [spec.atlas_size, spec.atlas_size],
            "fbx": display_path(out_dir / f"{asset_id}.fbx"),
            "texture": display_path(out_dir / f"{asset_id}.png"),
            "preview": display_path(preview_dir / f"{asset_id}.png"),
        })
    print(json.dumps({
        "dry_run": True,
        "blender_version": bpy.app.version_string,
        "raw_dir": display_path(raw_dir),
        "assets": rows,
    }, indent=2, ensure_ascii=False))
    return 0


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    raw_dir = absolute(args.raw_dir)
    out_dir = safe_output_dir(args.out_dir, "--out-dir")
    preview_dir = safe_output_dir(args.preview_dir, "--preview-dir")
    asset_ids = list(ASSET_ORDER if args.all else (args.asset,))
    if args.dry_run:
        return dry_run_plan(asset_ids, raw_dir, out_dir, preview_dir)

    raw_paths = {asset_id: find_raw_glb(raw_dir, ASSET_SPECS[asset_id])
                 for asset_id in asset_ids}
    overrides = load_orientation_overrides(raw_dir)
    out_dir.mkdir(parents=True, exist_ok=True)
    preview_dir.mkdir(parents=True, exist_ok=True)
    existing = read_manifest(out_dir)
    previous_assets = {
        item["id"]: item for item in existing.get("assets", [])
        if isinstance(item, dict) and item.get("id") in ASSET_SPECS
    }

    for asset_id in asset_ids:
        result = process_asset(
            ASSET_SPECS[asset_id], raw_paths[asset_id], out_dir,
            preview_dir, overrides.get(asset_id, 0.0)
        )
        previous_assets[asset_id] = result
        write_manifest(out_dir, preview_dir, previous_assets, None)

    lineup = render_lineup(ASSET_ORDER, out_dir, preview_dir)
    path = write_manifest(out_dir, preview_dir, previous_assets, lineup)
    print("AI3D_MANIFEST " + display_path(path), flush=True)
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except PipelineError as error:
        print(json.dumps({
            "error": str(error),
            "type": type(error).__name__,
        }, ensure_ascii=False), file=sys.stderr, flush=True)
        raise SystemExit(2)
    except Exception as error:  # Preserve a useful Blender traceback for resumability.
        traceback.print_exc()
        print(json.dumps({
            "error": str(error),
            "type": type(error).__name__,
        }, ensure_ascii=False), file=sys.stderr, flush=True)
        raise SystemExit(3)
