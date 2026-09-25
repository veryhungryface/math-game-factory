# Independent implementation audit — 자를 벌려

Read-only source audit of `index.html` and `engine.js` against the actual `factory/work/chosen.json`, `CLAUDE.md`, and the 4-2 triangle textbook/curriculum unit. No game source, shared state, or browser session was changed by this reviewer. The parent and engine owner were editing concurrently; resolved items below were rechecked after their changes.

## Priority findings

1. **Resolved: a square placed outside the triangle could be accepted.** The original `releaseSquare()` checked only whether the rotatable arm matched either adjacent ray. Because the drawn square extends clockwise by 90°, one matching ray pointed its body outside the triangle. The updated function accepts only the ray whose clockwise interval toward the other side is the triangle's interior. Extracting the actual updated function and replaying 351 base triangles × mirrored/unmirrored × three corners × two arm rays produced **2,106 interior acceptances, 2,106 exterior refusals, zero failures**. This is function-level geometry evidence, not a browser-input claim.

2. **Resolved: errors originally made the piece smaller without constraining future space.** The parent added a labeled obstruction strip and reduced layout width cumulatively by 22 px per error on portrait / 48 px on landscape (up to two errors). The engine now refills available slots within the current difficulty group, including after 20 seconds or an ordinary error in the early group. Boundaries between learning groups remain ordered. The UI remaps existing ruler and square positions when the board layout changes, rather than changing the measured physical length.

3. **Resolved: incomplete sampled action solutions.** Acute angle jobs now include all three measurements and use `answerNumeric: 3` (other angle jobs use one measurement). A follow-up audit also found omitted angle steps in combined jobs and the omitted initial unlock in error-finding jobs; these were fixed together. `actionSolutions` now consistently contains alternative complete action sequences. Replayed **all 2,745 routes from 1,000 samples** through the actual UI ruler/square release functions and engine lock/unlock/discard actions: every route completed with lives 3, **zero errors**. This exercised 2,478 locks, 200 unlocks, 3,396 edge comparisons, 167 discards, and 888 individual angle measurements. The initial flat-array audit had to be updated when the engine owner normalized the schema during review; final results use the normalized nested routes.

4. **Resolved: a reverse job could have no equal-angle marks.** The generator now assigns reverse and error-finding variants only to the later isosceles pieces without changing the early 3:1 isosceles/scalene composition. Rechecked **1,000 seeded decks**: each had one reverse piece with exactly two appropriate equal-angle marks, and one error-finding piece initially locked to the unequal side; **zero mismatches**.

5. **Resolved: late error feedback is preserved through timeout.** Replayed a wrong action at 89.9 elapsed seconds, then advanced to exactly 90 seconds: phase became `gameover`, time became 0, lives stayed 2, and the complete mathematical feedback object remained unchanged for the UI's existing 1.15-second hold. Replayed a non-final correct action at 89.9 seconds with the same result: the earned score/reveal remained, but the unfinished run ended. Subsequent `forceCorrect()`, `forceWrong()`, and `tick()` changed neither terminal snapshot. An ordinary timeout without a pending reveal still emits timeout feedback. This closes the last identified source/function defect.

6. **Resolved after real QA: an offscreen square arm handle prevented rotation.** The square now retains its mathematical pose and draws a visible tethered handle when the physical tip is outside the canvas. Independently exercised **7,278,336 handle placements** using the actual layout/handle functions across all 351 base triangles, 24 rotations, both flips, one/two pieces, zero/two scars, and 320/390/820/1280 widths: every handle remained at least **25 px from the canvas rim and 48 px from the corner**, with zero failures. A further arbitrary edge/corner grid covered 93,636 cases with zero failures.

   The actual `inputDown()` / `inputMove()` functions were replayed for **9,700 handle grabs** across edge/corner placements and multiple full turns. Pointer-down never changed the square pose, every grab entered the rotation path, a stationary first move preserved the angle, and subsequent pointer motion changed the angle by precisely the user's angular delta (maximum numerical error 9.6×10⁻¹⁵). The offset comes only from visible handle/corner geometry; it does not contain the correct side or angle. The square-reset handler preserved the full mathematical state, including already measured corners, and was inert in clear/gameover states.

   This audit also found a signed-remainder defect: equivalent correct orientations at minus two or more turns were refused. The parent replaced the remainder expression with `atan2(sin(delta), cos(delta))`. Rechecked **221,130 cases** across all triangles/corners/flips, −10 through +10 turns, and ±7.99° / ±8.01° tolerances: zero failures. These are source/function regressions; the QA agent owns the actual browser usability replay.

## Exact model and recovery checks

- All **351** base triangles have strict triangle inequalities, their rendered Euclidean edge lengths match the stored corresponding integer lengths (tolerance used only for this drawing audit), and independent longest-side-square classification matches `classify()`: **zero geometry/classification errors**.
- Executed **200 normal runs** and **200 runs with one ordinary initial mistake**, solving through the public engine action methods (`lock`, `compare`, `discard`, `measureAngle`) instead of `forceCorrect`. Each action group advanced three game seconds. All **400 runs reached 10/10 clear**, with lives 3 / 2 respectively. The mistake retains the same solvable piece; lives do not recover. This verifies model reachability under a short successful-input schedule, not a novice's real-time completion speed.
- The locked ruler's `len` survives both rotation and translation. The real release handler uses vertex identification / alignment tolerances and stored integer side comparison; no correct edge is automatically selected for the learner. The parent's updated square check also preserves manual rotation.
- Integer side equality, equilateral inclusion in isosceles tasks, and the requirement to measure all three acute angles are present in actual state transitions. Non-acute jobs reject a smaller acute corner.
- Two-piece selection keeps separate engine state; the active piece is selected from the actual pointer hit. This source audit does not replace the QA agent's browser test of selecting and completing the second piece first.
- Follow-up facing/angle regression exercised **12,636 cases** using the actual `releaseSquare()` and engine: interior alignment and ±7.99° accepted, ±8.01° and exterior orientation refused, exactly the selected corner was measured, and one acute-corner measurement never completed the piece. **Zero failures**.
- Actual `layout()` geometry-remapping regression at **320×720, 390×844, 820×1180, 1280×800** retained arbitrary ruler endpoint poses, the locked length in physical units, square angle, and square corner position relative to the piece through one→two pieces, two cumulative scars, and two→one pieces. Maximum numerical drift was **2.7×10⁻¹⁵**.
- Three ordinary errors produced an irreversible gameover; subsequent `forceCorrect()`, `forceWrong()`, and `tick()` left the terminal snapshot unchanged. Public QA hook wrappers also guard terminal phases.

## Limits / smaller discrepancies

- The 90-second clock now advances through feedback holds. The help overlay still pauses play and `frame()` caps a delayed frame at 50 ms, so this audit does not establish a strict wall-clock 90-second duration under backgrounding or a stalled browser.
- Later progression uses task bands; early spawning additionally changes at 20 seconds, and the strip beginning after 30 active seconds is a visible time-pressure cue. There is no mandatory 55-second jump that could skip unfinished prerequisite tasks.
- The explicit three-ray square regression and 400 recovery runs were performed in Node. This reviewer did not launch Chrome, measure FPS, inspect screenshots, or rerun official QA / firstplay / B3; those remain the designated QA agent's scope.

## Final reviewed file identity

No open source/function findings remain in this review scope. Object labels remain based on their board slot, QA start avoids audio initialization, and the last audited control changes are the tethered square handle, its geometry-derived drag offset, the reset control, and robust signed-angle comparison.

SHA-256 recorded after the final square-control regression:

| File | SHA-256 |
| --- | --- |
| `public/g/jareul-beollyeo/index.html` | `988e04a5f1e66aa572a01db400a785186425971ed87423bacd0ec718b4a43a76` |
| `public/g/jareul-beollyeo/engine.js` | `a421b5c3d703d3acbc75e6c785799b342c1db266fe1a62831ca3feee4087ba90` |
| `public/g/jareul-beollyeo/meta.json` | `72b162aa6f7017a81df29398cb76152801ce3bc979e5e90aa14bbb32d1bde385` |
