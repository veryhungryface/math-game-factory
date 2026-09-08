# Sunbasket Kit v3

Original reusable 3D farm assets for **햇살 바구니 / Sunbasket Farm**. The v3 farmhouse, market stall, apple tree and carrot are rebuilt from original Blender geometry. The visual study used official Hay Day, FarmVille 3, Farm Together 2 and Township images to study broad silhouettes, projecting entrances, fabric curvature, foliage gaps and crop readability. No meshes, textures, characters or logos from those games are included.

The kit includes 12 GLBs: the game's ten existing assets plus `carrot-sprout` and `carrot-young`. It includes the same editable Blender source that produces the game models, source scripts, and two alternate layouts that reuse those models. Models retain project-original vertex colors and require no external textures.

## Open and regenerate

Use Blender 5.2 or newer. `sunbasket-farm-assets.blend` contains the cover scene; its hidden **Original export meshes** collection contains the exact origin-aligned mesh parts exported to the game. The two `sample-*.blend` files show the same geometry in a small farm and village market. The sample market hides the farmhouse porch and one tree's fruit and changes the roof/canvas tint to demonstrate part variants. These are previews, not separate finished games.

From the repository root:

```sh
blender --background --python art/sunbasket-kit/generate_assets.py -- --output-dir public/g/sunbasket-farm --source-dir art/sunbasket-kit
blender --background --python art/sunbasket-kit/render_details.py -- --source art/sunbasket-kit/sunbasket-farm-assets.blend --output-dir public/g/sunbasket-farm/assets --preview-dir logs/manual-20260908-sunbasket-v3/assets
blender --background --python art/sunbasket-kit/render_kit.py -- --source art/sunbasket-kit/sunbasket-farm-assets.blend --output logs/manual-20260908-sunbasket-v3/assets
```

The generator loads `v3_geometry.py` next to itself. `--models-only` exports GLBs and the source collection without cover renders. `--palette path/to/colors.json` overrides selected linear RGB palette entries. `palette-market.json` is an example; use a separate output directory when generating variants. `render_kit.py --only buildings-v3` limits preview rendering to one view. Other preview names are `orchard-crops-v3`, `sample-small-farm` and `sample-village-market`.

In a downloaded source ZIP, run the same scripts from its root with `--output-dir ./generated-game --source-dir .`. Pass `--font-path /path/to/a/licensed/Korean-font.ttf` to the generator/detail renderer if not on macOS. The macOS default is installed Apple SD Gothic Neo; no system font file is distributed. Text in the saved cover source is converted to mesh geometry.

## Coordinates, scale and attachment points

The Blender authoring space is +Z up, -Y front. GLB export is **+Y up, +Z front, ground y=0**. Geometry is in consistent kit units, not a claim of physically accurate dimensions. Mature crops are height 1; young carrot is .60 and sprout is .25. The game's cell size and crop display scales are separate from the learning rule “one square metre produces one box.”

Non-crop assets keep authored dimensions; exact bounds, part names and pivots are in `manifest.json`. The runtime currently centres each model by its full bounds and normalizes it to a desired height, so any attachment should use a named GLB anchor or transform coordinates through the model root, not assume the source coordinates are already game-world coordinates.

| Asset | Named parts / reuse contract |
| --- | --- |
| farmhouse | `walls`, `roof`, `entry-frame`, `door`, `windows`, `windows_glass`, `porch`, `chimney`; omit porch to make a compact workshop silhouette. The door origin is its left hinge. Rotate about local +Y in glTF to open it. |
| market-stall | `frame`, `awning`, `produce`, `sign`; canvas and produce can be recolored/hidden independently. |
| apple-tree | `trunk`, `canopy`, `fruit`; hide fruit for the harvested state and apply uniform scale/rotation to make an orchard. Three mesh primitives total. |
| carrot / carrot-young / carrot-sprout | Each is one mesh, one vertex-color material and one primitive, suitable for 144 instances. Replace geometry at the same cell position for growth; do not normalize all three to the same height. |
| windmill | Existing named `windmill-rotor` retains its actual rotation pivot. Rotate local Z, preserve its initial glTF position. |
| strawberry / corn / crate / cart / fence | Original silhouettes retained, with common updated material/palette. Single primitive each; the cart wheels remain baked into its body and do not have independent wheel animation. |

Named empty anchors exported in GLB:

- Farmhouse: `anchor-doorstep`, `anchor-pot-left`, `anchor-pot-right`.
- Market: `anchor-display`, `anchor-sign`.
- Tree: `anchor-trunk-top`.

Anchors carry `kit_asset` and `kit_part` extras and are not meshes. Never count them as draw calls. The farmhouse front projection is deeper than v2; runtime landscaping should follow `anchor-doorstep`. Model nodes use `kit_asset` / `kit_part` extras so tooling does not have to guess roles from object order.

## Materials and variations

`FARM_VERTEX_COLOR` and `WINDOW_VERTEX_COLOR` read `COLOR_0`; keep vertex colors enabled. Most color changes are authored into vertex colors, allowing every crop to stay a single primitive. Rebuild with a palette JSON for an exact color-family change. Values are **linear RGB**, each in 0..1.

For a quick runtime tint, find a named mesh part, clone its material before changing `material.color`, and keep its geometry shared. This multiplies its existing vertex colors, rather than replacing each surface with a flat color. Clone per part/variant because several parts share the same material instance after GLTFLoader import. A roof-only recolor therefore changes the roof node's cloned material, not the common material used by the whole house.

Leaf blades are folded, double-sided surfaces with no hidden thickness. This saves triangles without changing their visible contour. Do not globally disable double-sided rendering. The models have no rigs or baked animation tracks; named door/rotor pivots are the supported animated parts.

## Runtime scenery helpers

`RUNTIME.md` documents the reusable path/shoulder/vegetation constructor and visual-only crop growth module. The scenery constructor accepts placement arrays, seed and field exclusion bounds; it does not require the Sunbasket gameplay state. Building grounding uses named GLB anchors. Its local canvas textures need a browser DOM, so this helper module is a Three.js/browser asset, whereas the GLBs are renderer-independent.

## Game package and source verification

The source ZIP contains `models/*.glb` byte-identical to the game, the same Blender source/scripts, both sample layout sources, the manifest, and an exact `runtime/` snapshot of index/game/style/math plus direct asset JavaScript modules. Runtime files expect the complete game checkout's existing assets and local `public/vendor/` import map; the snapshot alone is not a standalone server bundle. `meta.json` is omitted because publishing changes QA metadata.

`package_source.py` refreshes manifest hashes and creates the source ZIP only after runtime files are frozen. `verify_kit.py` validates the GLB primitive/color/ground/height contracts, named parts and anchors, raster sizes, and source archive CRC/byte identity. Cover and icon images are renders of these actual models, not reference screenshots or generated illustration substitutes.
