# Sunbasket v3 asset handoff

Worktree: /Users/sitpo/math-game-factory-sunbasket-v3. Only assigned asset/source/log paths changed. No game logic, UI code, math, meta, queue, publishing or git operations were edited by this asset worker.

The farmhouse now has a projecting gabled porch, actual recessed entry opening, thick curved eaves, overlapping roof courses, framed/shuttered windows and integrated planters. The stall has a sagging curved canvas, scalloped hem, sturdy feet and lower divided produce trays. The apple tree has asymmetric lobed crowns, exposed branching gaps and removable fruit. Carrot has a tapered root, divided folded fronds, and separate sprout/young/mature models.

| Asset | Triangles | Primitives |
| --- | ---: | ---: |
| farmhouse | 19840 | 8 |
| market-stall | 7436 | 4 |
| apple-tree | 4112 | 3 |
| carrot | 644 | 1 |
| carrot-young | 188 | 1 |
| carrot-sprout | 108 | 1 |
| strawberry | 456 | 1 |
| corn | 344 | 1 |
| crate | 1728 | 1 |
| cart | 6496 | 1 |
| windmill | 5544 | 2 |
| fence | 860 | 1 |

All 12 GLBs together are 47756 unique triangles; scene instances multiply these counts. FPS and maximum crop interactions are measured separately by runtime/root. Original other-asset silhouettes remain; their common material/palette and metadata are refreshed. Trees have one source shape, size/rotation variants and an optional fruit part, not three independently sculpted trees.

The farmhouse has a real left door hinge pivot. Six named Empty anchors cover doorstep/planters, stall display/sign and trunk top. Major parts have kit_asset/kit_part extras. Mature crops are height1, young .60, sprout .25, each one double-sided vertex-color primitive. Crops/root axes remain compatible with the existing normalization. Geometry was optimized by removing hidden blade thickness; visible contours were preserved.

## Evidence

- buildings-v3.png and orchard-crops-v3.png show complete model silhouettes and three carrot growth stages. Directly inspected.
- sample-small-farm.png and sample-village-market.png demonstrate the same mesh sources in two arrangements, with a porch-less house, canvas tint and fruit-hidden tree. Their editable .blend files are in art/sunbasket-kit. Directly inspected and approved by the independent visual reviewer.
- thumb.png/square.png and three crop icons rerender the actual final model geometry. All raster dimensions pass.
- kit-verification.json independently checks actual GLB accessor bounds through node transforms, ground0, crop heights, vertex colors, primitive counts, parts, anchors and door pivot. All pass.
- source-package.json: 3490734-byte ZIP, 31 entries, CRC pass. All12 GLBs and frozen runtime6 files are byte-identical to the game. Runtime modules include both farm-polish.mjs and crop-growth.mjs. The package also includes generator/detail/kit render scripts, verification and packaging tools, palette example, documentation, source .blend and both sample scenes.
- Main source, models and runtime are distinguished in README/RUNTIME.md. No proprietary reference geometry/textures or system font is redistributed.

Runtime reported final freeze before packaging. Asset/source freeze hashes are in frozen-hashes.json. Blender browsers/rendering processes have all terminated; GPU ownership was returned. .blend1 and Python cache intermediates are removed. Final QA/review/publish/commit/HANDOVER are parent responsibilities.
