# Art round 1 — reproducible Blender geometry and vertex AO

```sh
/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup \
  --python factory/unity-src/hyeopgok-sasu/ArtSource/blender/build_models.py
/Applications/Blender.app/Contents/MacOS/Blender -b --factory-startup \
  --python factory/unity-src/hyeopgok-sasu/ArtSource/blender/verify_ao.py
```

Append `-- --no-render` to the first command to skip only the source preview.
Models, their baked attributes, the manifest and the `.blend` are still rebuilt.

`ao_bake.py` casts 48 cosine-weighted hemisphere rays from each mesh corner into
Blender's `BVHTree`. A finite-radius falloff softens contact; this is actual
geometry occlusion, not a height-colour approximation. A temporary ground plane
receives standalone objects, while terrain and its rocks occlude one another.
The temporary plane is neither an exported mesh nor an extra runtime draw.

The exported channel contract is:

| Channel | Meaning |
|---|---|
| `COLOR.rgb` | Original sRGB palette colour |
| `COLOR.a` | Baked linear AO factor, 0.42–1.0 |
| `TEXCOORD0.x` | Original team mask: 0 team tint, 1 fixed colour |
| `TEXCOORD0.y` | 1 marks the new baked encoding |
| Blender `BakedAO` | Inspectable greyscale copy of the baked colour attribute |

The runtime shader decodes sRGB, applies the team colour and multiplies by AO.
Code-generated meshes without the UV marker retain the previous colour contract.
`verify_ao.py` independently reimports all 44 FBX files, checks the actual exported
AO/UV channels and writes SHA-256 hashes and statistics to `ao-bake-report.json`.
The current deterministic bake fires 4,164,480 rays.

Geometry additions: irregular convex-hull sandstone masses in three sizes; 40
individual roof tiles per barracks roof; dressed stone corners and foundation
courses; enemy hanging cloth; blue-painted palisade tips; larger readable troop
helmets; grass, flowers, pebbles, sack, log pile and torch meshes. The terrain
extends to ±80 m and uses interpolated road shoulder colours plus two wheel ruts.
Gameplay road/pad/plateau coordinates remain unchanged.

`world-preview.png` is a Blender source preview, not evidence of the Unity game.
The actual game screenshots and performance evidence are in `../validation/art-r1/`.

Second geometry pass replaces all concentric rock bands with fractured convex-hull faces.
The plateau and outer U cliff retain their top perimeter and walkable heights, but
use staggered diagonal facets instead of repeated horizontal colour stripes.
`art-r1-rock-study.png` is a dedicated Blender render of the three rock silhouettes.

Third geometry pass gives stage 2 the royal-blue slate roof, and stage 3 brighter
bevelled individual slate plus a tiled porch. Tower standards now fold and have a
swallowtail silhouette. Soldier faces gain only four dark eye-slot triangles.
`art-r1-building-study.png` shows the three staged buildings in Blender.

Final actor pass replaces box hands, boots and arm sections on the king and heavy
units with 32-triangle faceted capsules; shoulder/sallet silhouettes are weighted.
All three actor bounds remain unchanged, ordinary troops remain 222/198 triangles.
The terrain uses the same ±5% field evaluated per vertex for continuous mottling,
with zero added triangles. See `geometry-refinement-report.json` and
`art-r1-character-study.png` for verification.
