# Farm runtime v3

The visual revision retains the existing camera, integer area grid, field handle, sweep coordinates, score, lives and saved progress. `logic-preserved.json` records the old/new comparison of twenty gameplay/camera functions plus the math module hash.

`assets/farm-polish.mjs` provides reusable landscape construction:

```js
const scenery = decorateMeadow(scene, {
  seed: 7303,
  paths: [{ points: [[-4,-5],[-5,0],[-2,6]], width: 1.3 }],
  fieldClearance: { minX:-4.85, maxX:4.85, minZ:-5.15, maxZ:5.65 },
  meadowBeds: [[-6,1,0.8]],
  orchardBeds: [[-7,-3,1.1]],
  yards: [{x:-5,z:-6,rx:1.5,rz:1.1,color:0xc0ae7d}],
  hills: [[-12,-18,7,0x91b77d]]
});
```

Paths include shoulders, worn wheel tracks, pebbles and low border leaves. Placement arrays use world x/z coordinates and radius. The clearance is a vegetation exclusion rectangle; callers should also place their paths/yards/hills outside an interactive field. The returned root can be positioned in another scene; geometry and placement do not read gameplay state. Shared feather/contact textures are generated locally once.

New buildings own their detailed planters and awning. `decorateBuildings` adds soft grounding and optional stepping stones at the named GLB `anchor-doorstep`; there are no legacy fixed coordinates for window planters or awning trim. Pass `{doorstep:false}` to suppress stepping stones.

`assets/crop-growth.mjs` is a visual-only carrot growth timeline: sprout→young→mature across the unchanged 1.14-second growing phase. Height ratios .25/.60/1.0 match the supplied original GLB stages. Each stage uses the same actual cell positions and an instanced mesh; stage appearance never enters the harvest count or answer predicate.

Original model images are maintained by the asset builder. Runtime source files required in the source ZIP: index.html, game.js, style.css, math.mjs, assets/farm-polish.mjs and assets/crop-growth.mjs.
