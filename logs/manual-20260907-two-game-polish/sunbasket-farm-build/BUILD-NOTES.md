# Sunbasket Farm v2 visual polish

Scope: public/g/sunbasket-farm/. Replaced repeated cone grass with leaf and daisy clusters; added soil path rims, irregular doorstep stones, window flower boxes and pots, market produce basket and awning ties, and contact shadows. Adjusted lighting/shadow filtering and paper/button finishes. New assets/farm-polish.mjs contains static original Three.js scenery. The maximum 12x12 field stays clear.

## Preserved behavior and art

math.mjs is byte-identical to v1. Forty-three existing game.js functions, including camera/field coordinates, answer checking, scoring, lives, first-attempt accounting, persistence and harvest, are unchanged (invariants.json). All ten original GLBs, the Blender source and generator scripts, and cover PNGs are unchanged. Version is 2; the prior qa object (score88/passedtrue) is preserved for final publication by the parent.

## Validation

- preview.json: before/after planning and harvest at390px; after at320/820/1280px, no console errors or horizontal overflow. Parent and independent reviewer directly inspected390px comparisons and approved the visual direction.
- full-ui-report.json: actual handle drag; held input released on blur; CDP touch harvesting the initial12 crops; Space and mouse-sweep harvest; all9 orders complete. Only state/coordinate read hooks were used; no answer/completion mutators. Seed5, one intentional mistake: score810, delivered621, lives2, firstTry7/8, growth3. Saved growth/wins survived reload; three wrong attempts lost; restart reset correctly. No console errors.
- Max144 crops: zero DOM occlusions and horizontal overflow at320/390/820/1280px. Screenshots directly inspected; decorations do not cover the field.
- node --check passed game.js and farm-polish.mjs.
- source-verification.json: ZIP CRC passed. Runtime index/game/style/math/assets/farm-polish.mjs plus10 GLBs are byte-identical to game files; manifest matches inside/outside ZIP. Blender source and generation scripts match the prior ZIP. README distinguishes runtime polish from unchanged Blender art and describes the required complete game checkout.

## Reproduction

Run from repository root:

    node logs/manual-20260907-two-game-polish/sunbasket-farm-build/full-ui.mjs

The script serves public/ on an ephemeral port and uses the local Chrome151/Puppeteer installation. It closes its browser/server in finally. Only one browser/GPU owner may run at once. preview.mjs requires the archived v1 files at logs/manual-20260907-two-game-polish/sunbasket-farm-build/baseline-v1/ for comparison interception.

Final file hashes are in frozen-hashes.json. CSS/game/farm-polish imports use ?v=2. The final source-download URL can append ?v=2; there was no existing in-game source link, so no new UI was introduced. The browser and temporary server have closed; GPU slot returned. Final QA/review/publish/commit/HANDOVER remain with the parent.
