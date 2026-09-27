// 협곡 사수 v3.2 black-box acceptance gates.
//
// (a)-(g) retain the v3 pixel/layout definitions and run on all three map
// fixtures.  (h)-(i) use one unforced endless run.  (j) drives blind browser
// bots through physical pad touches; it intentionally reports a one-run smoke
// sample rather than pretending to be the future large Editor simulation.

import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import {
  GAME,
  MAPS,
  VIEWPORTS,
  V32_HERE as HERE,
  attachDiagnostics,
  canonicalProblems,
  centerOf,
  currentState,
  expectedDifficultyMultiplier,
  eligibleChoiceItems,
  flatNormalizedRects,
  intersection,
  loadGame,
  longestChoiceCases,
  normalizeSnapshot,
  openBrowserHarness,
  packHashes,
  pointInFrame,
  pointInSafeFrame,
  readDebugSnapshot,
  readPackIndex,
  readPacks,
  rectContains,
  rectInFrame,
  measureScreenshot,
  sampleWholeBankRaw,
  sleep,
  startGame,
  stateContract,
  touch,
} from './harness-lib.mjs';

const TIMES_MS = Object.freeze([3000, 8000, 15000]);
const REQUIRED_SHADOW_TYPES = Object.freeze(['king', 'soldier', 'building', 'tree']);
const CHOICE_FIXTURES = Object.freeze([
  { id: 'degree', packId: 'm2s2-u1', itemId: 'm2s2-u1-003', choices: ['70°', '80°', '140°', '40°'] },
  { id: 'area-unit', packId: 'm2s2-u3', itemId: 'm2s2-u3-011', choices: ['6 cm²', '12 cm²', '24 cm²', '3 cm²'] },
  { id: 'ratio', packId: 'm2s2-u3', itemId: 'm2s2-u3-067', choices: ['2:3', '8:27', '4:9', '9:4'] },
  { id: 'negative-frac', packId: 'm2s1-u5', itemId: 'm2s1-u5-034', choices: ['{frac:4/3}', '{frac:3/4}', '−{frac:3/4}', '−6'] },
  { id: 'decimal', packId: 'm2s1-u5', itemId: 'm2s1-u5-027', choices: ['12', '23.8', '13', '37'] },
  { id: 'zero-one-frac', packId: 'm2s2-u7', itemId: 'm2s2-u7-020', choices: ['0', '{frac:1/4}', '{frac:1/2}', '1'] },
  { id: 'longest', packId: 'm2s1-u6', itemId: 'm2s1-u6-022', choices: ['점 (−5, 0)을 지난다', 'y축에 평행하다', 'x축에 평행하다', '기울기가 −5이다'] },
]);
const THRESHOLDS = Object.freeze({
  shadowDarkness: 0.12,
  shadowPassRatio: 0.80,
  minimumShadowReferences: 3,
  frontLineMargin: 0.06,
  choicePadCount: 4,
  minimumUpgradePadCount: 1,
  minimumMeasuredGlyphs: 4,
  glyphHeight: { '390': 14, '1280': 20 },
  expectedPackCount: 13,
  landscapeBattlefieldWidthRatio: 0.70,
  stageOneLowDifficultyMinimum: 7,
  stageThreeHighDifficultyMinimum: 1,
});

const KEEP_FRAMES = process.env.V32_KEEP_FRAMES !== '0';
const SKIP_LONGEST = process.argv.includes('--quick') || process.env.V32_SKIP_LONGEST === '1';
const SKIP_BROWSER_BOTS = process.env.V32_SKIP_BROWSER_BOTS === '1';
const RUN_BROWSER_BOT_SMOKE = process.env.V32_BROWSER_BOT_SMOKE === '1' && !SKIP_BROWSER_BOTS;
const OUT = path.join(HERE, 'gates.json');
const FRAME_DIR = path.join(HERE, 'gate-frames');
const CHOICE_DIR = path.join(HERE, 'choice-frames');
const EDITOR_BOTS = path.join(HERE, 'bots.json');

function stableJson(value) {
  if (Array.isArray(value)) return `[${value.map(stableJson).join(',')}]`;
  if (value && typeof value === 'object') {
    return `{${Object.keys(value).sort().map(key => `${JSON.stringify(key)}:${stableJson(value[key])}`).join(',')}}`;
  }
  return JSON.stringify(value);
}

function sourceExactResult(samples, packCase) {
  const eligible = eligibleChoiceItems(packCase.pack);
  const sourceById = new Map(eligible.map(problem => [String(problem?.id ?? ''), problem]));
  const actual = Array.isArray(samples) ? samples : [];
  const actualIds = new Set(actual.map(problem => String(problem?.id ?? '')));
  const pass = eligible.length === packCase.choiceItems && actual.length === eligible.length &&
    actualIds.size === actual.length && actual.every(problem => {
    const id = String(problem?.id ?? '');
    return sourceById.has(id) && stableJson(problem) === stableJson(sourceById.get(id));
  });
  return { packId: packCase.packId, expected: packCase.choiceItems, actual: actual.length, pass };
}

function readEditorBotSummary() {
  if (!fs.existsSync(EDITOR_BOTS)) return { source: EDITOR_BOTS, present: false, pass: false, error: 'bots.json is missing' };
  try {
    const source = JSON.parse(fs.readFileSync(EDITOR_BOTS, 'utf8'));
    const bots = Array.isArray(source?.bots) ? source.bots.map(value => ({
      bot: value?.bot ?? null,
      blind: value?.blind === true,
      runs: Number(value?.runs),
      deaths: Number(value?.deaths),
      deathsBeforeStage3: Number(value?.deathsBeforeStage3),
      reachedStage3: Number(value?.reachedStage3),
      reachedStage4: Number(value?.reachedStage4),
      maxStage: Number(value?.maxStage),
      pathFailures: Number(value?.pathFailures),
      pass: value?.pass === true,
    })) : [];
    const expectedRunsPerBot = Number(source?.packCount) * Number(source?.seedsPerPack);
    const blindIds = ['idle', 'random', 'fixed', 'nearest'];
    const blind = blindIds.map(id => bots.find(value => value.bot === id)).filter(Boolean);
    const perfect = bots.find(value => value.bot === 'perfect') ?? null;
    const rules = source?.rules ?? {};
    const runCount = Array.isArray(source?.runs) ? source.runs.length : 0;
    const aggregatePass = Number(source?.schemaVersion ?? source?.schema_version) === 4 &&
      Number(source?.packCount) === THRESHOLDS.expectedPackCount && Number(source?.seedsPerPack) >= 12 &&
      expectedRunsPerBot === 156 && runCount === expectedRunsPerBot * 5 && source?.actualPadPath === true &&
      source?.blindBotsDieBeforeStage3 === true && source?.perfectReachesStage3 === true &&
      source?.gateHpOnly === true && source?.difficultyCurve === true && source?.pass === true &&
      rules?.tenQuestionsContinue === true && rules?.twentyQuestionsContinue === true &&
      rules?.thirtyQuestionsContinue === true && rules?.gateHpZeroEnds === true && rules?.mapCycle === true &&
      blind.length === blindIds.length && blind.every(value => value.blind && value.runs === expectedRunsPerBot &&
        value.deathsBeforeStage3 === value.runs && value.reachedStage3 === 0 && value.pathFailures === 0 && value.pass) &&
      perfect && !perfect.blind && perfect.runs === expectedRunsPerBot && perfect.reachedStage3 === perfect.runs &&
      perfect.pathFailures === 0 && perfect.pass;
    return {
      source: EDITOR_BOTS,
      present: true,
      schemaVersion: Number(source?.schemaVersion ?? source?.schema_version),
      generatedPass: source?.pass === true,
      packCount: Number(source?.packCount),
      seedsPerPack: Number(source?.seedsPerPack),
      expectedRunsPerBot,
      runCount,
      actualPadPath: source?.actualPadPath === true,
      blindBotsDieBeforeStage3: source?.blindBotsDieBeforeStage3 === true,
      perfectReachesStage3: source?.perfectReachesStage3 === true,
      gateHpOnly: source?.gateHpOnly === true,
      difficultyCurve: source?.difficultyCurve === true,
      rules: {
        tenQuestionsContinue: rules?.tenQuestionsContinue === true,
        twentyQuestionsContinue: rules?.twentyQuestionsContinue === true,
        thirtyQuestionsContinue: rules?.thirtyQuestionsContinue === true,
        gateHpZeroEnds: rules?.gateHpZeroEnds === true,
        mapCycle: rules?.mapCycle === true,
      },
      bots,
      pass: !!aggregatePass,
    };
  } catch (error) {
    return { source: EDITOR_BOTS, present: true, pass: false, error: String(error?.stack || error) };
  }
}

function commit() {
  try {
    return execFileSync('git', ['rev-parse', '--short', 'HEAD'], {
      cwd: path.resolve(HERE, '../../../../../..'), encoding: 'utf8',
    }).trim();
  } catch {
    return 'unknown';
  }
}

function comparable(value) {
  return String(value ?? '').replace(/\s+/gu, ' ').trim();
}

// v3.2's curriculum curve only guarantees the authored first item when it is
// introductory.  Visual fixtures temporarily pin the requested authored item
// into that band so the renderer, not the seeded selector, is under test.
function pinVisualFixture(pack, item, mapId, itemId = item?.id) {
  const desiredDifficulty = Number(mapId) === 3 ? 3 : Number(mapId) === 2 ? 2 : 1;
  const eligibleDummy = eligibleChoiceItems(pack).find(value => value.id !== itemId);
  if (Number(mapId) >= 2) {
    const others = pack.items.filter(value => value.id !== itemId && value !== eligibleDummy);
    // DebugSetStage is applied after Rules.Start has already dealt one stage-one
    // question.  Feed that initial draw a deterministic dummy, leaving the
    // requested item as the sole exact match for stage 2/3's first difficulty.
    return {
      ...pack,
      items: [
        { ...eligibleDummy, difficulty: 1 },
        { ...item, difficulty: desiredDifficulty },
        ...others.map(value => ({ ...value, difficulty: 1 })),
      ],
    };
  }
  return {
    ...pack,
    items: [{ ...item, difficulty: desiredDifficulty }, ...pack.items.filter(value => value.id !== itemId)],
  };
}

function validBodyGlyph(glyph) {
  return glyph.visible && (!glyph.kind || glyph.kind === 'body' || glyph.kind === 'prompt') &&
    /[0-9A-Z가-힣]/u.test(glyph.char) && glyph.rect;
}

function evaluateGlyphs(snapshot, pixels, viewport) {
  const requiredPx = THRESHOLDS.glyphHeight[viewport.id];
  const glyphs = pixels.glyphs.filter(validBodyGlyph).map(glyph => ({
    id: glyph.id,
    char: glyph.char,
    inkHeightPx: glyph.inkHeightCss,
    pass: Number.isFinite(glyph.inkHeightCss) && glyph.inkHeightCss >= requiredPx,
  }));
  const heights = glyphs.map(glyph => glyph.inkHeightPx).filter(Number.isFinite);
  return {
    requiredPx,
    measuredBodyGlyphs: glyphs.length,
    minimumPx: heights.length ? Math.min(...heights) : null,
    maximumPx: heights.length ? Math.max(...heights) : null,
    glyphs,
    pass: glyphs.length >= THRESHOLDS.minimumMeasuredGlyphs && glyphs.every(glyph => glyph.pass),
  };
}

function evaluatePads(snapshot, viewport) {
  const question = snapshot.question.panelRect;
  const choices = snapshot.choicePadRects.filter(pad => pad.visible && pad.active);
  const upgrades = snapshot.upgradePadRects.filter(pad => pad.visible && pad.active);
  const pads = [
    ...choices.map(pad => ({ ...pad, kind: 'choice' })),
    ...upgrades.map(pad => ({ ...pad, kind: 'upgrade' })),
  ];
  const invalidPads = pads.filter(pad => !rectInFrame(pad.rect, viewport));
  const overlaps = pads.map(pad => ({
    id: pad.id,
    kind: pad.kind,
    intersection: intersection(question, pad.rect),
  }));
  const totalIntersectionArea = overlaps.reduce((sum, value) => sum + (value.intersection.area ?? 0), 0);
  return {
    questionPanelRect: question,
    choicePadCount: choices.length,
    upgradePadCount: upgrades.length,
    invalidPads,
    overlaps,
    totalIntersectionArea,
    pass: rectInFrame(question, viewport) && choices.length === 4 && upgrades.length >= 1 &&
      invalidPads.length === 0 && overlaps.every(value => value.intersection.area === 0),
  };
}

function evaluateWorldLabels(snapshot, viewport, requirement = 'normal') {
  const labels = snapshot.worldLabels.filter(label => label.visible && label.active);
  const counts = Object.fromEntries(['choice', 'upgrade', 'reward'].map(kind => [kind, labels.filter(label => label.kind === kind).length]));
  const invalidLabels = labels.filter(label => !rectInFrame(label.rect, viewport));
  const duplicateIds = labels.map(label => label.id).filter((id, index, all) => all.indexOf(id) !== index);
  const overlaps = [];
  for (let left = 0; left < labels.length; left += 1) {
    for (let right = left + 1; right < labels.length; right += 1) {
      overlaps.push({ left: labels[left].id, right: labels[right].id, intersection: intersection(labels[left].rect, labels[right].rect) });
    }
  }
  const inventoryPass = requirement === 'reward' ? counts.reward >= 1 : counts.choice === 4 && counts.upgrade >= 1;
  const totalIntersectionArea = overlaps.reduce((sum, value) => sum + (value.intersection.area ?? 0), 0);
  return {
    requirement, labels, counts, invalidLabels, duplicateIds, overlaps, totalIntersectionArea,
    pass: inventoryPass && invalidLabels.length === 0 && duplicateIds.length === 0 &&
      overlaps.every(value => value.intersection.area === 0),
  };
}

function evaluateShadow(snapshot, pixels, viewport) {
  const relevant = pixels.shadows.filter(sample => REQUIRED_SHADOW_TYPES.includes(sample.type) && sample.visible && pointInFrame(sample.foot, viewport));
  const counts = Object.fromEntries(REQUIRED_SHADOW_TYPES.map(type => [type, relevant.filter(sample => sample.type === type).length]));
  const missingTypes = REQUIRED_SHADOW_TYPES.filter(type => counts[type] < 1);
  const samples = relevant.map(sample => ({
    id: sample.id,
    type: sample.type,
    referenceCount: sample.ring.length,
    darkness: sample.darkness,
    pass: sample.ring.length >= THRESHOLDS.minimumShadowReferences &&
      Number.isFinite(sample.darkness) && sample.darkness >= THRESHOLDS.shadowDarkness,
  }));
  const passingSamples = samples.filter(sample => sample.pass).length;
  const ratio = samples.length ? passingSamples / samples.length : 0;
  return {
    counts, missingTypes, samples, passingSamples, totalSamples: samples.length, ratio, ratioPercent: ratio * 100,
    pass: missingTypes.length === 0 && ratio >= THRESHOLDS.shadowPassRatio,
  };
}

function evaluateFront(snapshot, viewport) {
  const points = snapshot.frontLine.points;
  const insideSafe = points.filter(point => pointInSafeFrame(point, viewport, THRESHOLDS.frontLineMargin));
  const battlefieldWidthRatio = snapshot.battlefieldRect?.width / viewport.width;
  const coveragePass = viewport.id !== '1280' ||
    Number.isFinite(battlefieldWidthRatio) && battlefieldWidthRatio >= THRESHOLDS.landscapeBattlefieldWidthRatio;
  return {
    declaredVisible: snapshot.frontLine.visible,
    points,
    insideSafeFrame: insideSafe.length,
    battlefieldRect: snapshot.battlefieldRect,
    battlefieldWidthRatio,
    coveragePass,
    pass: snapshot.frontLine.visible && points.length > 0 && insideSafe.length === points.length && coveragePass,
  };
}

function evaluateClipping(snapshot, viewport, expectedPrompt) {
  const question = snapshot.question;
  const diagnosticsComplete = Object.values(question.diagnosticsReported).every(Boolean);
  const characterCountsComplete = Number.isFinite(question.visibleCharacters) && Number.isFinite(question.totalCharacters) &&
    question.totalCharacters > 0 && question.visibleCharacters >= question.totalCharacters;
  const result = {
    promptMatches: comparable(question.prompt) === comparable(expectedPrompt),
    panelInFrame: rectInFrame(question.panelRect, viewport),
    bodyInPanel: rectContains(question.panelRect, question.bodyRect),
    textInBody: rectContains(question.bodyRect, question.renderedTextRect),
    diagnosticsComplete,
    characterCountsComplete,
    isFolded: question.isFolded,
    hasEllipsis: question.hasEllipsis,
    isTruncated: question.isTruncated,
    isOverflowing: question.isOverflowing,
    visibleCharacters: question.visibleCharacters,
    totalCharacters: question.totalCharacters,
  };
  result.pass = result.promptMatches && result.panelInFrame && result.bodyInPanel && result.textInBody &&
    result.diagnosticsComplete && result.characterCountsComplete && !result.isFolded && !result.hasEllipsis &&
    !result.isTruncated && !result.isOverflowing;
  return result;
}

function evaluateChoiceFixture(snapshot, viewport, fixture) {
  const choices = snapshot.choices;
  const actualChoices = choices.map(choice => choice.text);
  const exactChoices = choices.length === 4 &&
    JSON.stringify([...actualChoices].sort()) === JSON.stringify([...fixture.choices].sort());
  const labels = choices.map(choice => {
    const complete = Number.isFinite(choice.visibleCharacters) && Number.isFinite(choice.totalCharacters) &&
      choice.totalCharacters > 0 && choice.visibleCharacters >= choice.totalCharacters;
    const tofu = choice.glyphs.filter(glyph => glyph.char === '□' || glyph.char === '�');
    const pass = rectInFrame(choice.bodyRect, viewport) && rectContains(choice.bodyRect, choice.renderedTextRect) &&
      complete && choice.glyphs.length > 0 && tofu.length === 0 && !choice.hasMissingGlyph &&
      !choice.isTruncated && !choice.isOverflowing;
    return { id: choice.id, text: choice.text, complete, tofu, pass };
  });
  const overlaps = [];
  for (let left = 0; left < choices.length; left += 1) {
    for (let right = left + 1; right < choices.length; right += 1) {
      overlaps.push(intersection(choices[left].renderedTextRect, choices[right].renderedTextRect));
    }
  }
  const worldLabels = evaluateWorldLabels(snapshot, viewport, 'normal');
  return {
    expectedChoices: fixture.choices,
    actualChoices,
    exactChoices,
    labels,
    overlaps,
    tofuGlyphCount: labels.reduce((sum, label) => sum + label.tofu.length, 0),
    worldLabels,
    pass: exactChoices && labels.length === 4 && labels.every(label => label.pass) &&
      overlaps.every(value => value.area === 0) && worldLabels.pass,
  };
}

async function measureVisual(page, viewport, map, label, expectedPrompt, timeMs, report) {
  const raw = await readDebugSnapshot(page);
  if (raw?.__error) throw new Error(`${label}: ${raw.__error}`);
  const snapshot = normalizeSnapshot(raw, viewport);
  if (snapshot.version < 3) throw new Error(`${label}: debug snapshot version ${snapshot.version || 'missing'}; >=3 required`);
  const state = await currentState(page);
  const contract = stateContract(state);
  const png = await page.screenshot({ encoding: 'base64' });
  if (KEEP_FRAMES) fs.writeFileSync(path.join(FRAME_DIR, `${label}.png`), Buffer.from(png, 'base64'));
  const pixels = await measureScreenshot(page, png, snapshot, viewport);
  const value = {
    label,
    map,
    viewport: { id: viewport.id, width: viewport.width, height: viewport.height },
    timeMs,
    state: {
      stage: state?.stage ?? null,
      map: state?.map ?? null,
      mapName: state?.mapName ?? null,
      loop: state?.loop ?? null,
      stagePhase: state?.stagePhase ?? null,
    },
    stateContract: contract,
    fixtureMatches: Number(state?.map) === map.id,
    shadow: evaluateShadow(snapshot, pixels, viewport),
    front: evaluateFront(snapshot, viewport),
    ui: evaluatePads(snapshot, viewport),
    glyph: evaluateGlyphs(snapshot, pixels, viewport),
    clipping: evaluateClipping(snapshot, viewport, expectedPrompt),
    worldLabels: evaluateWorldLabels(snapshot, viewport, 'normal'),
  };
  value.pass = contract.valid && value.fixtureMatches;
  report.measurements.push(value);
  return { value, snapshot, state };
}

function stateSignature(state) {
  return [state?.phase, state?.stagePhase, state?.stage, state?.map, state?.loop, state?.stageQuestion,
    state?.attempts, state?.hp, state?.mapSerial, state?.transitionSerial, state?.questionId].join('|');
}

function pushTrace(trace, state) {
  const signature = stateSignature(state);
  if (trace.length && trace[trace.length - 1].signature === signature) return;
  trace.push({
    atMs: Date.now(), signature,
    phase: state?.phase ?? null,
    stagePhase: state?.stagePhase ?? null,
    stage: Number(state?.stage), map: Number(state?.map), loop: Number(state?.loop),
    stageQuestion: Number(state?.stageQuestion), attempts: Number(state?.attempts),
    solved: Number(state?.solved), hp: Number(state?.hp ?? state?.lives), maxHp: Number(state?.maxHp),
    coins: Number(state?.coins), questionId: state?.questionId ?? null,
    bossWave: state?.bossWave === true, regionRecovered: state?.regionRecovered === true,
    perkSelection: state?.perkSelection === true,
    mapSerial: Number(state?.mapSerial), transitionSerial: Number(state?.transitionSerial),
    endReason: state?.endReason ?? null,
  });
}

async function waitState(page, predicate, timeoutMs, trace = null) {
  const deadline = Date.now() + timeoutMs;
  let state = null;
  while (Date.now() < deadline) {
    state = await currentState(page);
    if (trace) pushTrace(trace, state);
    if (predicate(state)) return state;
    await sleep(35);
  }
  throw new Error(`state timeout after ${timeoutMs}ms: ${JSON.stringify(state)}`);
}

function validChoicePads(state, viewport) {
  const rects = flatNormalizedRects(state?.choicePadRects, 4, viewport, 'choice');
  return rects.length === 4 && rects.every(rect => rectInFrame(rect, viewport));
}

async function touchFirstPerk(page, state, viewport) {
  const choices = Array.isArray(state?.perkChoices) ? state.perkChoices : [];
  const rects = flatNormalizedRects(state?.perkRects, 3, viewport, 'perk');
  if (choices.length !== 3 || rects.length !== 3 || !rectInFrame(rects[0], viewport)) {
    throw new Error(`perk surface is not measurable: ${JSON.stringify({ choices, rects })}`);
  }
  const point = centerOf(rects[0]);
  await touch(page, point.x, point.y, 100);
  return { choices, rects, selected: 0, point };
}

async function submitHook(page, method, trace) {
  const before = await currentState(page);
  const attempts = Number(before?.attempts ?? 0);
  const accepted = await page.evaluate(name => window.__GAME_TEST__?.[name]?.(), method);
  if (!accepted) throw new Error(`__GAME_TEST__.${method}() was not accepted`);
  return waitState(page, state => Number(state?.attempts ?? 0) > attempts || state?.phase === 'gameover', 15000, trace);
}

async function waitNaturalTransition(page, stage, viewport, trace) {
  const observation = {
    fromStage: stage,
    boss: false,
    recovered: false,
    perk: false,
    transition: false,
    stageAdvanced: false,
    perkTouch: null,
    preRecoveryHp: null,
    recoveredHp: null,
    recoveredMaxHp: null,
    states: [],
  };
  const deadline = Date.now() + 70000;
  let lastStagePhase = '';
  while (Date.now() < deadline) {
    const state = await currentState(page);
    pushTrace(trace, state);
    const phase = String(state?.stagePhase ?? '');
    if (phase !== lastStagePhase || observation.states.length === 0) {
      observation.states.push({
        stagePhase: phase,
        stage: state?.stage ?? null,
        map: state?.map ?? null,
        loop: state?.loop ?? null,
        hp: state?.hp ?? state?.lives ?? null,
        attempts: state?.attempts ?? null,
        mapSerial: state?.mapSerial ?? null,
        transitionSerial: state?.transitionSerial ?? null,
      });
      lastStagePhase = phase;
    }
    if (state?.bossWave === true || phase === 'boss') {
      observation.boss = true;
      observation.preRecoveryHp = Number(state?.hp ?? state?.lives);
    }
    if (state?.regionRecovered === true || phase === 'recovered') {
      observation.recovered = true;
      observation.recoveredHp = Number(state?.hp ?? state?.lives);
      observation.recoveredMaxHp = Number(state?.maxHp);
    }
    if (state?.perkSelection === true || phase === 'perk') {
      observation.perk = true;
      if (!observation.perkTouch) observation.perkTouch = await touchFirstPerk(page, state, viewport);
    }
    if (phase === 'transition') observation.transition = true;
    if (Number(state?.stage) > stage) observation.stageAdvanced = true;
    if (observation.stageAdvanced && phase === 'question' && state?.feedbackVisible !== true) {
      observation.after = state;
      observation.pass = observation.boss && observation.recovered && observation.perk && observation.transition &&
        observation.perkTouch != null;
      return observation;
    }
    if (state?.phase === 'gameover') {
      observation.after = state;
      observation.pass = false;
      observation.error = 'game over before next stage';
      return observation;
    }
    await sleep(35);
  }
  observation.pass = false;
  observation.error = `stage ${stage} transition timeout`;
  return observation;
}

async function runNaturalJourney(page, server, viewport, packCase, traceReport, resilientLoad = null) {
  const query = `pack=${encodeURIComponent(packCase.packId)}&artprobe=1&v32natural=${Date.now()}`;
  if (resilientLoad) page = await resilientLoad(viewport, query);
  else await loadGame(page, server, viewport, query);
  await startGame(page);
  const trace = [];
  const rawBankBefore = await sampleWholeBankRaw(page);
  const bankBefore = canonicalProblems(rawBankBefore);
  const bankIds = new Set(bankBefore.map(problem => problem.id));
  const sourceById = new Map((packCase.pack?.items ?? []).map(problem => [String(problem?.id ?? ''), problem]));
  const sourceExact = sourceExactResult(rawBankBefore, packCase).pass;
  const initial = await currentState(page);
  pushTrace(trace, initial);
  const checkpoints = [];
  const transitions = [];
  const difficultyByStage = { 1: [], 2: [], 3: [] };
  const bankChecks = [];
  const stageStarts = [{
    stage: Number(initial?.stage), coins: Number(initial?.coins),
    towerLevels: Array.isArray(initial?.towerLevels) ? [...initial.towerLevels] : null,
    towerTypes: Array.isArray(initial?.towerTypes) ? [...initial.towerTypes] : null,
  }];

  for (let stage = 1; stage <= 3; stage += 1) {
    const targetAttempts = stage * 10;
    while (true) {
      const state = await currentState(page);
      pushTrace(trace, state);
      if (state?.phase === 'gameover') break;
      if (Number(state?.attempts ?? 0) >= targetAttempts) break;
      if (Number(state?.stage) !== stage || String(state?.stagePhase) !== 'question' ||
          state?.feedbackVisible === true || state?.pending === true) {
        await sleep(35);
        continue;
      }
      const difficulty = Number(state?.questionDifficulty);
      if (Number.isFinite(difficulty)) difficultyByStage[stage].push(difficulty);
      await submitHook(page, 'answerCorrect', trace);
      // Let feedback advance without asking the hook to skip a production beat.
      await sleep(80);
    }
    const atTen = await currentState(page);
    if (atTen?.phase === 'gameover') {
      checkpoints.push({ stage, targetAttempts, state: atTen, pass: false, reason: 'game over at count boundary' });
      break;
    }
    const transition = await waitNaturalTransition(page, stage, viewport, trace);
    transitions.push(transition);
    const after = transition.after ?? await currentState(page);
    const expectedStage = stage + 1;
    const expectedMap = ((expectedStage - 1) % 3) + 1;
    const expectedLoop = Math.floor((expectedStage - 1) / 3) + 1;
    const contract = stateContract(after);
    const rawBankAfter = await sampleWholeBankRaw(page);
    const bankAfter = canonicalProblems(rawBankAfter);
    const bankStable = JSON.stringify(canonicalProblems(bankAfter)) === JSON.stringify(canonicalProblems(bankBefore));
    const sourceExactAfter = rawBankAfter.length === rawBankBefore.length && rawBankAfter.every(problem =>
      sourceById.has(String(problem?.id ?? '')) && stableJson(problem) === stableJson(sourceById.get(String(problem.id))));
    bankChecks.push({ afterStage: stage, size: bankAfter.length, stable: bankStable, sourceExact: sourceExactAfter });
    const checkpoint = {
      afterQuestions: targetAttempts,
      expected: { stage: expectedStage, map: expectedMap, loop: expectedLoop },
      actual: {
        attempts: Number(after?.attempts), stage: Number(after?.stage), map: Number(after?.map),
        mapName: after?.mapName ?? null, loop: Number(after?.loop), hp: Number(after?.hp ?? after?.lives),
        maxHp: Number(after?.maxHp), phase: after?.phase ?? null, stagePhase: after?.stagePhase ?? null,
        endReason: after?.endReason ?? null, difficultyMultiplier: Number(after?.difficultyMultiplier),
        pack_id: after?.pack_id ?? null, questionId: after?.questionId ?? null,
        solved: Number(after?.solved), score: Number(after?.score),
        mapSerial: Number(after?.mapSerial), transitionSerial: Number(after?.transitionSerial),
      },
      stateContract: contract,
      choicePadsValid: validChoicePads(after, viewport),
      questionInBank: bankIds.has(String(after?.questionId ?? '')),
      bankStable,
      sourceExact: sourceExact && sourceExactAfter,
      pass: contract.valid && Number(after?.attempts) === targetAttempts && Number(after?.stage) === expectedStage &&
        Number(after?.map) === expectedMap && Number(after?.loop) === expectedLoop &&
        Number(after?.hp ?? after?.lives) > 0 && after?.phase === 'playing' && after?.stagePhase === 'question' &&
        !after?.endReason && Number(after?.solved) === targetAttempts && String(after?.mapName ?? '').length > 0 &&
        validChoicePads(after, viewport) && bankIds.has(String(after?.questionId ?? '')) && bankStable && sourceExact && sourceExactAfter &&
        Math.abs(Number(after?.difficultyMultiplier) - expectedDifficultyMultiplier(expectedLoop)) < 0.001,
    };
    checkpoints.push(checkpoint);
    stageStarts.push({
      stage: Number(after?.stage), coins: Number(after?.coins),
      towerLevels: Array.isArray(after?.towerLevels) ? [...after.towerLevels] : null,
      towerTypes: Array.isArray(after?.towerTypes) ? [...after.towerTypes] : null,
    });
    if (!transition.pass || !checkpoint.pass) break;
  }

  const perfectCheckpoint = checkpoints.find(value => value.expected?.stage === 3 && value.pass) ?? null;
  let terminal = await currentState(page);
  // Prove the converse after the 30-question checkpoints: wrong answers use the
  // same real pad command path and continue until the gate reaches zero.
  let wrongSubmissions = 0;
  const wrongDeadline = Date.now() + 60000;
  while (Date.now() < wrongDeadline && wrongSubmissions < 20 && terminal?.phase !== 'gameover') {
    terminal = await currentState(page);
    pushTrace(trace, terminal);
    if (terminal?.phase === 'gameover') break;
    if (terminal?.perkSelection === true || terminal?.stagePhase === 'perk') {
      await touchFirstPerk(page, terminal, viewport);
      await sleep(100);
      continue;
    }
    if (terminal?.stagePhase !== 'question' || terminal?.feedbackVisible === true || terminal?.pending === true) {
      await sleep(50);
      continue;
    }
    await submitHook(page, 'answerWrong', trace);
    wrongSubmissions += 1;
    await sleep(80);
  }
  terminal = await waitState(page, state => state?.phase === 'gameover' || Number(state?.hp ?? state?.lives) <= 0, 30000, trace)
    .catch(() => currentState(page));
  pushTrace(trace, terminal);

  const gameoverSamples = trace.filter(value => value.phase === 'gameover');
  const hpZeroSamples = trace.filter(value => value.hp === 0);
  const gateH = {
    checkpoints,
    terminal,
    gameoverSamples,
    hpZeroSamples,
    pass: checkpoints.length === 3 && checkpoints.every(value => value.pass) &&
      terminal?.phase === 'gameover' && Number(terminal?.hp ?? terminal?.lives) === 0 &&
      terminal?.endReason === 'gate_hp_zero' && gameoverSamples.length > 0 &&
      gameoverSamples.every(value => value.hp === 0) && hpZeroSamples.some(value => value.phase === 'gameover'),
  };

  const history = Array.isArray(terminal?.questionHistory) ? terminal.questionHistory.map(String) : [];
  const firstThirty = history.slice(0, 30);
  const resetStarts = stageStarts.filter(value => value.stage >= 2 && value.stage <= 4);
  const resetSignature = resetStarts[0] ? JSON.stringify({ levels: resetStarts[0].towerLevels, types: resetStarts[0].towerTypes }) : null;
  const resetsPass = resetStarts.length === 3 && resetStarts.every(value => value.coins === 0 &&
    JSON.stringify({ levels: value.towerLevels, types: value.towerTypes }) === resetSignature);
  const lowStageOne = difficultyByStage[1].filter(value => value <= 2).length;
  const highStageThree = difficultyByStage[3].filter(value => value >= 3).length;
  const recoveriesPass = transitions.every(value => {
    const before = Number(value.preRecoveryHp);
    const after = Number(value.after?.hp ?? value.after?.lives);
    const max = Number(value.after?.maxHp);
    if (![before, after, max].every(Number.isFinite)) return false;
    return after >= before && after <= max && after - before <= 30 &&
      (after === max || after - before > 0);
  });
  const mapSerials = checkpoints.map(value => Number(value.actual?.mapSerial));
  const transitionSerials = checkpoints.map(value => Number(value.actual?.transitionSerial));
  const serialsPass = mapSerials.every(Number.isFinite) && transitionSerials.every(Number.isFinite) &&
    mapSerials.every((value, at) => value > Number(at ? mapSerials[at - 1] : initial?.mapSerial)) &&
    transitionSerials.every((value, at) => value > Number(at ? transitionSerials[at - 1] : initial?.transitionSerial));
  const gateI = {
    sequence: checkpoints.map(value => value.actual ? ({ stage: value.actual.stage, map: value.actual.map, loop: value.actual.loop }) : null),
    expectedSequence: [{ stage: 2, map: 2, loop: 1 }, { stage: 3, map: 3, loop: 1 }, { stage: 4, map: 1, loop: 2 }],
    transitions,
    bankChecks,
    stageStarts,
    resetsPass,
    recoveriesPass,
    serials: { mapSerials, transitionSerials, pass: serialsPass },
    questionHistory: { length: history.length, firstThirty, uniqueFirstThirty: new Set(firstThirty).size },
    difficultyByStage,
    difficultyCurve: {
      stageOneLow: lowStageOne,
      stageOneMeasured: difficultyByStage[1].length,
      stageThreeHigh: highStageThree,
      stageThreeMeasured: difficultyByStage[3].length,
    },
    packId: initial?.pack_id ?? null,
    sourceExact,
    pass: checkpoints.length === 3 && checkpoints.every(value => value.pass) && transitions.length === 3 &&
      transitions.every(value => value.pass) && bankChecks.every(value => value.stable && value.sourceExact) && sourceExact && resetsPass &&
      recoveriesPass && serialsPass &&
      firstThirty.length >= 30 && new Set(firstThirty).size === firstThirty.length &&
      checkpoints.every(value => value.actual?.pack_id === initial?.pack_id) &&
      difficultyByStage[1].length === 10 && lowStageOne >= THRESHOLDS.stageOneLowDifficultyMinimum &&
      difficultyByStage[3].length === 10 && highStageThree >= THRESHOLDS.stageThreeHighDifficultyMinimum,
  };

  traceReport.push(...trace);
  return { initial, bankBefore, checkpoints, transitions, difficultyByStage, perfectCheckpoint, terminal, gateH, gateI };
}

function lcg(seed) {
  let value = seed >>> 0;
  return () => {
    value = (Math.imul(value, 1664525) + 1013904223) >>> 0;
    return value / 0x100000000;
  };
}

async function stateChoiceRects(page, state, viewport) {
  let rects = flatNormalizedRects(state?.choicePadRects, 4, viewport, 'choice');
  if (rects.length === 4 && rects.every(rect => rectInFrame(rect, viewport))) return rects;
  const raw = await readDebugSnapshot(page);
  if (raw?.__error) return [];
  rects = normalizeSnapshot(raw, viewport).choicePadRects.filter(value => value.visible && value.active && value.rect).map(value => value.rect);
  return rects;
}

async function runBlindBrowserBot(page, server, viewport, packCase, id, resilientLoad = null) {
  const result = {
    id,
    runs: 1,
    blind: true,
    selectionInputs: id === 'nearest' ? ['choicePadRects', 'kingScreen'] : id === 'random' ? ['choicePadRects', 'fixed PRNG'] : id === 'fixed' ? ['choicePadRects', 'constant index'] : [],
    submitted: 0,
    correct: 0,
    stageReached: 0,
    attempts: 0,
    terminal: null,
    errors: [],
    pass: false,
  };
  try {
    const query = `pack=${encodeURIComponent(packCase.packId)}&artprobe=1&v32bot=${id}-${Date.now()}`;
    if (resilientLoad) page = await resilientLoad(viewport, query);
    else await loadGame(page, server, viewport, query);
    await startGame(page);
    const random = lcg(0x320000 + id.length * 997);
    const timeoutMs = id === 'idle' ? Number(process.env.V32_IDLE_TIMEOUT_MS || 210000) : Number(process.env.V32_BOT_TIMEOUT_MS || 120000);
    const deadline = Date.now() + timeoutMs;
    let lastSubmittedQuestion = null;
    while (Date.now() < deadline) {
      const state = await currentState(page);
      result.stageReached = Math.max(result.stageReached, Number(state?.stage ?? 0));
      result.attempts = Math.max(result.attempts, Number(state?.attempts ?? 0));
      result.correct = Math.max(result.correct, Number(state?.solved ?? 0));
      if (state?.phase === 'gameover' || Number(state?.stage) >= 3) {
        result.terminal = state;
        break;
      }
      if (id === 'idle') {
        await sleep(100);
        continue;
      }
      if (state?.perkSelection === true || state?.stagePhase === 'perk') {
        await touchFirstPerk(page, state, viewport);
        await sleep(120);
        continue;
      }
      if (state?.stagePhase !== 'question' || state?.feedbackVisible === true || state?.pending === true ||
          String(state?.questionId ?? '') === lastSubmittedQuestion) {
        await sleep(45);
        continue;
      }
      const rects = await stateChoiceRects(page, state, viewport);
      if (rects.length !== 4) throw new Error('four measurable choice pad rects are required');
      let index = 0;
      if (id === 'random') index = Math.floor(random() * 4);
      if (id === 'nearest') {
        const king = Array.isArray(state?.kingScreen) && state.kingScreen.length >= 2
          ? { x: Number(state.kingScreen[0]) * viewport.width, y: Number(state.kingScreen[1]) * viewport.height }
          : null;
        if (!king || !Number.isFinite(king.x) || !Number.isFinite(king.y)) throw new Error('nearest bot requires kingScreen');
        index = rects.map((rect, at) => ({ at, distance: Math.hypot(centerOf(rect).x - king.x, centerOf(rect).y - king.y) }))
          .sort((left, right) => left.distance - right.distance || left.at - right.at)[0].at;
      }
      const before = Number(state?.attempts ?? 0);
      lastSubmittedQuestion = String(state?.questionId ?? '');
      const point = centerOf(rects[index]);
      await touch(page, point.x, point.y, 80);
      const after = await waitState(page, candidate => Number(candidate?.attempts ?? 0) > before || candidate?.phase === 'gameover', 15000);
      if (Number(after?.attempts ?? 0) > before) result.submitted += 1;
      await sleep(80);
    }
    if (!result.terminal) result.terminal = await currentState(page);
    result.stageReached = Math.max(result.stageReached, Number(result.terminal?.stage ?? 0));
    result.attempts = Math.max(result.attempts, Number(result.terminal?.attempts ?? 0));
    result.correct = Math.max(result.correct, Number(result.terminal?.solved ?? 0));
    result.pass = result.terminal?.phase === 'gameover' && Number(result.terminal?.hp ?? result.terminal?.lives) === 0 &&
      Number(result.terminal?.stage) < 3 && result.terminal?.endReason === 'gate_hp_zero' &&
      (id === 'idle' ? result.submitted === 0 : result.submitted > 0);
  } catch (error) {
    result.errors.push(String(error?.stack || error));
  }
  return result;
}

const index = readPackIndex();
const records = readPacks(index);
const longest = longestChoiceCases(records);
const defaultCase = longest.find(value => value.packId === index.default_pack && value.item) ?? longest.find(value => value.item);
if (!defaultCase) throw new Error('No eligible choice problem exists for v3.2 gates');
const fixtures = CHOICE_FIXTURES.map(spec => {
  const record = records.find(value => value.id === spec.packId) ?? null;
  const item = record?.pack?.items?.find(value => value.id === spec.itemId) ?? null;
  return {
    ...spec,
    record,
    item,
    sourcePass: record?.pack?.schema_version === 3 && !!item &&
      JSON.stringify(item.choices) === JSON.stringify(spec.choices),
  };
});
const hashesBefore = packHashes(index);
const editorBotResults = readEditorBotSummary();
const packAudit = {
  expectedCount: THRESHOLDS.expectedPackCount,
  count: records.length,
  cases: longest.map(value => ({
    packId: value.packId,
    schemaVersion: value.schemaVersion,
    choiceItems: value.choiceItems,
    longestId: value.item?.id ?? null,
    pass: value.schemaVersion === 3 && value.choiceItems > 0 && !!value.item,
  })),
};
packAudit.pass = packAudit.count === packAudit.expectedCount && packAudit.cases.every(value => value.pass) &&
  fixtures.length === CHOICE_FIXTURES.length && fixtures.every(value => value.sourcePass);

const report = {
  schemaVersion: 4,
  generatedAt: new Date().toISOString(),
  commit: commit(),
  game: GAME,
  thresholds: THRESHOLDS,
  inputs: {
    maps: MAPS,
    viewports: VIEWPORTS,
    timesMs: TIMES_MS,
    keepFrames: KEEP_FRAMES,
    skipLongest: SKIP_LONGEST,
    skipBrowserBots: SKIP_BROWSER_BOTS,
    runBrowserBotSmoke: RUN_BROWSER_BOT_SMOKE,
  },
  chrome: null,
  packAudit,
  packIntegrity: { before: hashesBefore, after: null, stable: false },
  measurements: [],
  baseline: [],
  longestCases: [],
  rewardCases: [],
  choiceCases: [],
  sampleExactCases: [],
  naturalJourney: null,
  editorBotResults,
  blindBots: [],
  stateTrace: [],
  gates: null,
  errors: [],
  consoleErrors: [],
  consoleWarnings: [],
  failedRequests: [],
  loadRetries: [],
  overall: { pass: false },
};

if (KEEP_FRAMES) fs.mkdirSync(FRAME_DIR, { recursive: true });
fs.mkdirSync(CHOICE_DIR, { recursive: true });

let browser;
let server;
let page;
let overlay = null;
let overlayPackId = '';
try {
  const opened = await openBrowserHarness();
  ({ browser, server } = opened);
  report.chrome = opened.chrome;
  let pageLoads = 0;
  const replacePage = async () => {
    if (page) {
      page.removeAllListeners();
      await page.close().catch(() => {});
    }
    page = await browser.newPage();
    page.setDefaultTimeout(90000);
    await page.setCacheEnabled(false);
    attachDiagnostics(page, report);
    await page.setRequestInterception(true);
    page.on('request', request => {
      let pathname = '';
      try { pathname = new URL(request.url()).pathname; } catch {}
      if (overlay && pathname.endsWith(`/packs/${overlayPackId}.json`)) {
        request.respond({ status: 200, contentType: 'application/json; charset=utf-8', body: JSON.stringify(overlay) }).catch(() => {});
      } else request.continue().catch(() => {});
    });
  };
  const loadFixture = async (viewport, query) => {
    pageLoads += 1;
    // Unity WebGL heaps are not guaranteed to be reclaimed by navigation. A
    // fresh CDP page every 18 loads keeps the 145-case sweep below Chromium's
    // long-session memory cliff without changing the browser or build.
    if (!page || (pageLoads > 1 && (pageLoads - 1) % 18 === 0)) await replacePage();
    try {
      await loadGame(page, server, viewport, query);
    } catch (error) {
      report.loadRetries.push({ load: pageLoads, query, error: String(error?.message || error) });
      await replacePage();
      await loadGame(page, server, viewport, query);
    }
    return page;
  };
  await replacePage();

  // Audit the synchronous test hook against each unmodified source pack before
  // installing any pinned visual fixture used by the pixel gates below.
  for (const entry of longest) {
    overlayPackId = entry.packId;
    overlay = entry.pack;
    try {
      await loadFixture(VIEWPORTS[0],
        `pack=${encodeURIComponent(entry.packId)}&v32gate=sample-exact-${encodeURIComponent(entry.packId)}`);
      report.sampleExactCases.push(sourceExactResult(await sampleWholeBankRaw(page), entry));
    } catch (error) {
      report.sampleExactCases.push({ packId: entry.packId, expected: entry.choiceItems, actual: 0, pass: false });
      report.errors.push(`sample exact ${entry.packId}: ${String(error?.stack || error)}`);
    }
  }

  for (const map of MAPS) {
    overlayPackId = defaultCase.packId;
    overlay = pinVisualFixture(defaultCase.pack, defaultCase.item, map.id);
    for (const viewport of VIEWPORTS) {
      await loadFixture(viewport,
        `pack=${encodeURIComponent(defaultCase.packId)}&artprobe=1&v32map=${map.id}&v32gate=baseline-${map.key}-${viewport.id}`);
      await startGame(page);
      const startedAt = Date.now();
      for (const timeMs of TIMES_MS) {
        await sleep(Math.max(0, startedAt + timeMs - Date.now()));
        const label = `baseline-${map.key}-${viewport.id}-${timeMs / 1000}s`;
        try {
          const measured = await measureVisual(page, viewport, map, label, defaultCase.item.prompt, timeMs, report);
          report.baseline.push(measured.value);
          console.log(`${label} shadow=${measured.value.shadow.ratioPercent.toFixed(1)} front=${measured.value.front.pass} glyph=${measured.value.glyph.minimumPx}`);
        } catch (error) {
          report.errors.push(String(error?.stack || error));
        }
      }
    }

    if (!SKIP_LONGEST) {
      for (const entry of longest) {
        if (!entry.item) continue;
        overlayPackId = entry.packId;
        overlay = pinVisualFixture(entry.pack, entry.item, map.id);
        for (const viewport of VIEWPORTS) {
          const label = `longest-${map.key}-${entry.packId}-${viewport.id}`;
          try {
            await loadFixture(viewport,
              `pack=${encodeURIComponent(entry.packId)}&artprobe=1&v32map=${map.id}&v32gate=${encodeURIComponent(label)}`);
            await startGame(page);
            await sleep(850);
            const measured = await measureVisual(page, viewport, map, label, entry.item.prompt, 850, report);
            report.longestCases.push({ ...measured.value, packId: entry.packId, itemId: entry.item.id });
          } catch (error) {
            report.errors.push(String(error?.stack || error));
          }
        }
      }
    }

    overlayPackId = defaultCase.packId;
    overlay = defaultCase.pack;
    for (const viewport of VIEWPORTS) {
      const label = `reward-${map.key}-${viewport.id}`;
      try {
        await loadFixture(viewport,
          `pack=${encodeURIComponent(defaultCase.packId)}&artprobe=1&reward=tower&v32map=${map.id}&v32gate=${label}`);
        await startGame(page);
        const accepted = await page.evaluate(() => window.__GAME_TEST__?.answerCorrect?.());
        if (!accepted) throw new Error(`${label}: answerCorrect rejected`);
        await waitState(page, value => value?.feedbackVisible === true && value?.rewardKind === 'tower', 15000);
        const state = await waitState(page, value => {
          const mask = Number(value?.worldLabelMask ?? 0);
          return (mask & 0x80) !== 0 && (mask & 0x7f) === 0;
        }, 3000);
        const raw = await readDebugSnapshot(page);
        if (raw?.__error) throw new Error(`${label}: ${raw.__error}`);
        const snapshot = normalizeSnapshot(raw, viewport);
        const worldLabels = evaluateWorldLabels(snapshot, viewport, 'reward');
        const value = {
          label, map, viewport: viewport.id, rewardKind: state?.rewardKind ?? null,
          fixtureMatches: Number(state?.map) === map.id,
          stateContract: stateContract(state),
          worldLabels,
        };
        value.pass = value.fixtureMatches && value.stateContract.valid && state?.rewardKind === 'tower' && worldLabels.pass;
        report.rewardCases.push(value);
      } catch (error) {
        report.errors.push(String(error?.stack || error));
      }
    }

    for (const fixture of fixtures) {
      if (!fixture.record || !fixture.item) continue;
      overlayPackId = fixture.packId;
      overlay = pinVisualFixture(fixture.record.pack, fixture.item, map.id, fixture.itemId);
      for (const viewport of VIEWPORTS) {
        const label = `choice-${map.key}-${fixture.id}-${viewport.id}`;
        try {
          await loadFixture(viewport,
            `pack=${encodeURIComponent(fixture.packId)}&artprobe=1&v3choices=1&v32map=${map.id}&v32gate=${label}`);
          await startGame(page);
          await sleep(850);
          const raw = await readDebugSnapshot(page);
          if (raw?.__error) throw new Error(`${label}: ${raw.__error}`);
          const snapshot = normalizeSnapshot(raw, viewport);
          const state = await currentState(page);
          const evidence = evaluateChoiceFixture(snapshot, viewport, fixture);
          const file = `${label}.png`;
          await page.screenshot({ path: path.join(CHOICE_DIR, file) });
          const value = {
            label, map, viewport: viewport.id, fixture: fixture.id,
            itemId: fixture.itemId, screenshot: path.join('choice-frames', file),
            questionMatches: state?.questionId === fixture.itemId,
            fixtureMatches: Number(state?.map) === map.id,
            stateContract: stateContract(state),
            evidence,
          };
          value.pass = value.questionMatches && value.fixtureMatches && value.stateContract.valid && evidence.pass;
          report.choiceCases.push(value);
        } catch (error) {
          report.errors.push(String(error?.stack || error));
        }
      }
    }
  }

  // No v32map fixture is present below this point: h/i/j must prove the real
  // stage progression rather than a showcase state.
  overlayPackId = defaultCase.packId;
  overlay = defaultCase.pack;
  try {
    await replacePage();
    report.naturalJourney = await runNaturalJourney(page, server, VIEWPORTS[0], defaultCase, report.stateTrace, loadFixture);
  } catch (error) {
    report.errors.push(`natural journey: ${String(error?.stack || error)}`);
  }

  if (RUN_BROWSER_BOT_SMOKE) {
    for (const id of ['idle', 'random', 'fixed', 'nearest']) {
      const value = await runBlindBrowserBot(page, server, VIEWPORTS[0], defaultCase, id, loadFixture);
      report.blindBots.push(value);
      for (const error of value.errors) report.errors.push(`browser bot ${id}: ${error}`);
      console.log(`bot ${id}: stage=${value.stageReached} attempts=${value.attempts} pass=${value.pass}`);
    }
  }
} catch (error) {
  report.errors.push(String(error?.stack || error));
} finally {
  if (browser) await browser.close().catch(() => {});
  if (server) await server.close().catch(() => {});
  report.packIntegrity.after = packHashes(index);
  report.packIntegrity.stable = JSON.stringify(report.packIntegrity.before) === JSON.stringify(report.packIntegrity.after);
}

const expectedBaselinePerMap = VIEWPORTS.length * TIMES_MS.length;
const expectedLongestPerMap = SKIP_LONGEST ? 0 : THRESHOLDS.expectedPackCount * VIEWPORTS.length;
const expectedMeasurementsPerMap = expectedBaselinePerMap + expectedLongestPerMap;
const expectedChoicePerMap = CHOICE_FIXTURES.length * VIEWPORTS.length;
packAudit.sampleExact = {
  expected: THRESHOLDS.expectedPackCount,
  measured: report.sampleExactCases.length,
  cases: report.sampleExactCases,
  pass: report.sampleExactCases.length === THRESHOLDS.expectedPackCount && report.sampleExactCases.every(value => value.pass),
};
packAudit.pass = packAudit.pass && packAudit.sampleExact.pass;

function mapSummary(cases, predicate, expectedPerMap) {
  return MAPS.map(map => {
    const selected = cases.filter(value => value.map?.id === map.id);
    const passing = selected.filter(predicate).length;
    return {
      map,
      expected: expectedPerMap,
      measured: selected.length,
      passing,
      failures: selected.filter(value => !predicate(value)).map(value => value.label),
      pass: selected.length === expectedPerMap && passing === expectedPerMap,
    };
  });
}

const gateA = {
  perMap: mapSummary(report.baseline, value => value.pass && value.shadow.pass, expectedBaselinePerMap),
};
gateA.pass = gateA.perMap.every(value => value.pass);
const gateB = {
  perMap: mapSummary(report.baseline, value => value.pass && value.front.pass, expectedBaselinePerMap),
};
gateB.pass = gateB.perMap.every(value => value.pass);
const gateC = {
  perMap: mapSummary(report.measurements, value => value.pass && value.ui.pass, expectedMeasurementsPerMap),
};
gateC.pass = gateC.perMap.every(value => value.pass);
const gateD = {
  thresholdsPx: THRESHOLDS.glyphHeight,
  perMap: mapSummary(report.measurements, value => value.pass && value.glyph.pass, expectedMeasurementsPerMap),
};
gateD.pass = gateD.perMap.every(value => value.pass);
const gateE = {
  skipped: SKIP_LONGEST,
  perMap: mapSummary(report.longestCases, value => value.pass && value.clipping.pass, expectedLongestPerMap),
};
gateE.pass = !SKIP_LONGEST && packAudit.pass && gateE.perMap.every(value => value.pass);
const gateF = {
  normalPerMap: mapSummary(report.measurements, value => value.pass && value.worldLabels.pass, expectedMeasurementsPerMap),
  rewardPerMap: mapSummary(report.rewardCases, value => value.pass, VIEWPORTS.length),
};
gateF.pass = gateF.normalPerMap.every(value => value.pass) && gateF.rewardPerMap.every(value => value.pass);
const gateG = {
  perMap: mapSummary(report.choiceCases, value => value.pass, expectedChoicePerMap),
  packIntegrityStable: report.packIntegrity.stable,
};
gateG.pass = packAudit.pass && gateG.perMap.every(value => value.pass) && report.packIntegrity.stable;

const gateH = report.naturalJourney?.gateH ?? {
  pass: false,
  error: 'natural endless journey was not measured',
};
const gateI = report.naturalJourney?.gateI ?? {
  pass: false,
  error: 'natural map transitions were not measured',
};
const perfect = report.naturalJourney?.perfectCheckpoint;
const browserSmokePass = report.blindBots.length === 4 && report.blindBots.every(value => value.pass);
const gateJ = {
  mode: 'editor-authoritative-with-browser-smoke',
  editor: report.editorBotResults,
  browserSmoke: {
    auxiliary: true,
    skipped: !RUN_BROWSER_BOT_SMOKE,
    runs: report.blindBots,
    pass: RUN_BROWSER_BOT_SMOKE ? browserSmokePass : null,
  },
  naturalPerfectCheckpoint: perfect ? {
    runs: 1,
    stageReached: perfect.actual?.stage ?? null,
    hp: perfect.actual?.hp ?? null,
    pass: Number(perfect.actual?.stage) >= 3 && Number(perfect.actual?.hp) > 0,
  } : { runs: 0, stageReached: null, hp: null, pass: false },
};
gateJ.pass = report.editorBotResults?.pass === true && gateJ.naturalPerfectCheckpoint.pass;

report.gates = {
  a_groundShadowAllMaps: gateA,
  b_frontLineVisibilityAllMaps: gateB,
  c_zeroUiObstructionAllMaps: gateC,
  d_renderedGlyphHeightAllMaps: gateD,
  e_longestPromptClippingAllMaps: gateE,
  f_zeroWorldLabelOverlapAllMaps: gateF,
  g_choiceRenderingAllMaps: gateG,
  h_gameOverOnlyAtGateHpZero: gateH,
  i_naturalMapTransitionContinuity: gateI,
  j_browserBotSurvivalCurve: gateJ,
};

const diagnosticsPass = report.errors.length === 0 && report.consoleErrors.length === 0 && report.failedRequests.length === 0;
report.overall = {
  pass: diagnosticsPass && packAudit.pass && report.packIntegrity.stable && Object.values(report.gates).every(value => value.pass),
  diagnosticsPass,
  packAuditPass: packAudit.pass,
  packIntegrityPass: report.packIntegrity.stable,
  gatePasses: Object.fromEntries(Object.entries(report.gates).map(([key, value]) => [key, value.pass])),
};

fs.writeFileSync(OUT, `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify({ output: OUT, gates: report.overall.gatePasses, overall: report.overall, errors: report.errors }, null, 2));
if (!report.overall.pass) process.exitCode = 1;
