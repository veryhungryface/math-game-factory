# Independent audit — star-cork

Auditor scope: read-only `index.html`, `engine.js`, metadata and curriculum; no browser launched and no implementation file edited. Browser QA belongs to the separate validation worker. Scripts below load the actual engine in a Node VM.

## Current findings and resolved defects

- **Resolved: transient equality rewarded target-blind input.** Original automatic shipping at every matching prefix made an observed fixed-skip strategy solve 721/751 first attempts (200 sessions). The later constant-stock version still rewarded 156/353 because several intermediate partial sums could be rewarded. Ordinary rounds now judge the final plate at the end of the finite conveyor pass; overshoots still cost a life immediately. The fixed tutorial remains immediate. This is a deliberate difference from the proposal's flawed automatic-equality premise, required to preserve the learning gate. Help text now describes waiting until the conveyor passes.
- **Resolved: belt and initial plate encoded target counts.** Stock was solution large count + 1 and solution small count + 3. Each band now has constant stock independent of its requested number. Repair starts with seven large stars for every target, with a truthful generic overfull-plate prompt. Belt shuffling no longer checks target prefixes.
- **Resolved: graph answer placement/rank shortcuts.** Rows are shuffled; named read targets span low/middle/high ranks, and the highest/lowest names in difference questions vary. The half-apple marker shortcut is also resolved: all three rows in all 48 read/max problems have a half apple, and all requested/max values remain correct.
- **Resolved: final-wave first error made winning impossible.** The 14-second open round made otherwise perfect play plus a first error in that round exceed 90 seconds. Open duration is now 12 seconds. Independent normal play and a first full-wave error at each of six stages all win, with the slowest tested path 87.72 seconds.
- **Resolved: six plates could become a timeout loss during celebration.** A sixth plate sent at 89.5 seconds now remains a success: feedback at 90.1 seconds, then won with zero time bonus. Timeout loss requires fewer than six solved plates.
- **Resolved: pictograph title mismatch.** UI now labels apple pictographs as “모은 사과 수”, matching their pictures and prompts.

## Verified arithmetic and state behavior

At engine SHA-256 `bc04fe9c2309e168d6db33900221c10fd30196062a244f2a166af96038790302`:

- All 205 generated problems have exact integer solution sums, truthful distractor formulas, and consistent named-row/max/difference targets. No sampled numerical coincidence marks a correct value wrong.
- `g3s2-u6` and `[4수04-01]` exist in the curriculum; lessons 1–3 cover representation, reading, and interpretation used here. Units are 10/1 and 100/10; half is 5 only in the 10/1 band. No vertical scale is introduced.
- Zero lives remains terminal under ticks, ordinary shoot/remove, and forced QA correct/wrong hooks. Tutorial is fixed at 21 and time/lives freeze until its direct play completion.
- UI uses real conveyor hits and plate removal as answer input. No bottom answer-choice controls or numeric live plate total were found. Hit regions are at least 44 pixels. Title/image loads have explicit fallbacks. Audio initializes in gesture handlers.

## Probability definitions — do not conflate these

The independent 50% per-physical-candy null is the probability that the **final** selected multiset equals the target. Direct combinatorial counting agrees exactly with the engine's endpoint dynamic program for every problem. Mean probabilities over each finite pool are:

| Pool | Independent 50% final subset | Best target-blind fixed composition over the target population |
| --- | ---: | ---: |
| Construct | 5.981445% | 12.5% |
| Read/max | 3.479004% | 12.5% |
| Difference | 3.784180% | 12.5% |
| Scaled | 4.984538% | 8.333333% |
| Repair | 1.028061% | 12.5% |
| Open | 0.425975% | 33.333333% |

A fixed count policy and independent coin selection are distinct null policies. The former can target one of 8 equally likely quantities (or 12 / 3) and should be compared with that population chance, not with the 50% subset probability. Under the revised end-of-wave judge, the prior fixed-skip strategy gave 21/221 first-attempt outcomes (9.50%), and 0/200 sessions cleared the first four orders. The four required spatial-input bots are separately measured by the validation worker; this audit does not replace that run or report its results as its own.

## Reproducible scripts

- `node logs/star-cork-build/audit-pools.mjs`: arithmetic, distractor formulas, source graph targets, independent target-blind skip policy over 200 sessions.
- `node logs/star-cork-build/audit-endpoint.mjs`: independent combinatorial endpoint probabilities, distinct fixed-composition population chance, normal run and first full-wave failure/recovery in each stage.

No browser performance, visual first-play or real browser-pointer pass is claimed by this read-only audit. See the validation worker's actual browser evidence for those requirements.

Final arithmetic, endpoint-null, fixed-policy and recovery scripts were rerun against the final hash above after the half-row patch. No unresolved high-severity arithmetic or gameplay-state finding remains in this audit scope.
