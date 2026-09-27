// 협곡 사수 v3 black-box acceptance gates.
//
// Run from the repository root after building public/g/hyeopgok-sasu:
//   node factory/unity-src/hyeopgok-sasu/ArtSource/validation/v3/gates.mjs
//
// The harness never edits a pack. For the longest-prompt cases it intercepts
// the selected pack response in Chrome and moves one existing choice item to
// the front in memory. Missing probe evidence is a failure, never a pass.

import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import {
  GAME,
  HERE,
  VIEWPORTS,
  attachDiagnostics,
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
  sleep,
  startGame,
} from './harness-lib.mjs';

const TIMES_MS = Object.freeze([3000, 8000, 15000]);
const REQUIRED_SHADOW_TYPES = Object.freeze(['king', 'soldier', 'building', 'tree']);
const CHOICE_FIXTURE_SPECS = Object.freeze([
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
  minimumVisibleShadowSamplesPerType: 1,
  frontLineMargin: 0.06,
  panelPadIntersectionArea: 0,
  choicePadCount: 4,
  minimumUpgradePadCount: 1,
  minimumMeasuredGlyphs: 4,
  glyphHeight: { '390': 14, '1280': 20 },
  expectedPackCount: 13,
  expectedChoiceFixtures: CHOICE_FIXTURE_SPECS.length,
  worldLabelIntersectionArea: 0,
  landscapeBattlefieldWidthRatio: 0.70,
});
const KEEP_FRAMES = process.env.V3_KEEP_FRAMES !== '0';
const SKIP_LONGEST = process.argv.includes('--quick') || process.env.V3_SKIP_LONGEST === '1';
const OUT = path.join(HERE, 'gates.json');
const FRAME_DIR = path.join(HERE, 'gate-frames');
const FIX1_DIR = path.join(HERE, 'fix1');

function commit() {
  try { return execFileSync('git', ['rev-parse', '--short', 'HEAD'], { cwd: path.resolve(HERE, '../../../../../..'), encoding: 'utf8' }).trim(); }
  catch { return 'unknown'; }
}

function comparablePrompt(value) {
  return String(value ?? '').replace(/\s+/g, ' ').trim();
}

function validBodyGlyph(glyph) {
  return glyph.visible && (!glyph.kind || glyph.kind === 'body' || glyph.kind === 'prompt') &&
    /[0-9A-Z가-힣]/u.test(glyph.char) && glyph.rect;
}

function evaluateGlyphs(snapshot, pixels, viewport) {
  const threshold = THRESHOLDS.glyphHeight[viewport.id];
  const glyphs = pixels.glyphs.filter(validBodyGlyph).map(glyph => ({
    id: glyph.id,
    char: glyph.char,
    rect: glyph.rect,
    inkHeightPx: glyph.inkHeightCss,
    reason: glyph.reason,
    pass: Number.isFinite(glyph.inkHeightCss) && glyph.inkHeightCss >= threshold,
  }));
  const heights = glyphs.map(glyph => glyph.inkHeightPx).filter(Number.isFinite).sort((a, b) => a - b);
  return {
    requiredPx: threshold,
    reportedGlyphs: snapshot.question.glyphs.length,
    measuredBodyGlyphs: glyphs.length,
    minimumPx: heights.length ? heights[0] : null,
    maximumPx: heights.length ? heights[heights.length - 1] : null,
    glyphs,
    pass: glyphs.length >= THRESHOLDS.minimumMeasuredGlyphs && glyphs.every(glyph => glyph.pass),
  };
}

function evaluatePads(snapshot, viewport) {
  const question = snapshot.question.panelRect;
  const choices = snapshot.choicePadRects.filter(pad => pad.visible && pad.active);
  const upgrades = snapshot.upgradePadRects.filter(pad => pad.visible && pad.active);
  const allPads = [...choices.map(pad => ({ ...pad, kind: 'choice' })), ...upgrades.map(pad => ({ ...pad, kind: 'upgrade' }))];
  const invalid = allPads.filter(pad => !rectInFrame(pad.rect, viewport));
  const overlaps = allPads.map(pad => ({
    id: pad.id,
    kind: pad.kind,
    rect: pad.rect,
    intersection: intersection(question, pad.rect),
  }));
  const totalIntersectionArea = overlaps.reduce((sum, overlap) => sum + (overlap.intersection.area ?? 0), 0);
  return {
    questionPanelRect: question,
    questionPanelInFrame: rectInFrame(question, viewport),
    choicePadCount: choices.length,
    upgradePadCount: upgrades.length,
    invalidPads: invalid,
    overlaps,
    totalIntersectionArea,
    pass: rectInFrame(question, viewport) && choices.length === THRESHOLDS.choicePadCount &&
      upgrades.length >= THRESHOLDS.minimumUpgradePadCount && invalid.length === 0 &&
      overlaps.every(overlap => overlap.intersection.area === THRESHOLDS.panelPadIntersectionArea),
  };
}

function evaluateWorldLabels(snapshot, viewport, requirement = 'normal') {
  const visible = snapshot.worldLabels.filter(label => label.visible && label.active);
  const invalidLabels = visible.filter(label => !label.rect || !rectInFrame(label.rect, viewport));
  const ids = new Set();
  const duplicateIds = [];
  for (const label of visible) {
    if (ids.has(label.id)) duplicateIds.push(label.id);
    ids.add(label.id);
  }
  const overlaps = [];
  for (let left = 0; left < visible.length; left++) {
    for (let right = left + 1; right < visible.length; right++) {
      const overlap = intersection(visible[left].rect, visible[right].rect);
      overlaps.push({
        left: visible[left].id,
        right: visible[right].id,
        intersection: overlap,
      });
    }
  }
  const counts = Object.fromEntries(['choice', 'upgrade', 'reward'].map(kind => [kind, visible.filter(label => label.kind === kind).length]));
  const inventoryPass = requirement === 'reward'
    ? counts.reward >= 1
    : counts.choice === THRESHOLDS.choicePadCount && counts.upgrade >= THRESHOLDS.minimumUpgradePadCount;
  const totalIntersectionArea = overlaps.reduce((sum, overlap) => sum + (overlap.intersection.area ?? 0), 0);
  return {
    requirement,
    labels: visible,
    counts,
    invalidLabels,
    duplicateIds,
    overlaps,
    totalIntersectionArea,
    pass: inventoryPass && invalidLabels.length === 0 && duplicateIds.length === 0 &&
      overlaps.every(overlap => overlap.intersection.area === THRESHOLDS.worldLabelIntersectionArea),
  };
}

function evaluateShadow(snapshot, pixels, viewport) {
  const relevant = pixels.shadows.filter(sample => REQUIRED_SHADOW_TYPES.includes(sample.type) && sample.visible && pointInFrame(sample.foot, viewport));
  const counts = Object.fromEntries(REQUIRED_SHADOW_TYPES.map(type => [type, relevant.filter(sample => sample.type === type).length]));
  const missingTypes = REQUIRED_SHADOW_TYPES.filter(type => counts[type] < THRESHOLDS.minimumVisibleShadowSamplesPerType);
  const samples = relevant.map(sample => ({
    id: sample.id,
    type: sample.type,
    foot: sample.foot,
    ring: sample.ring,
    referenceCount: sample.ring.length,
    footLuminance: sample.footLuminance,
    groundLuminance: sample.groundLuminance,
    darkness: sample.darkness,
    darknessPercent: Number.isFinite(sample.darkness) ? sample.darkness * 100 : null,
    pass: sample.ring.length >= THRESHOLDS.minimumShadowReferences &&
      Number.isFinite(sample.darkness) && sample.darkness >= THRESHOLDS.shadowDarkness,
  }));
  const passing = samples.filter(sample => sample.pass).length;
  const ratio = samples.length ? passing / samples.length : 0;
  return {
    counts,
    missingTypes,
    samples,
    passingSamples: passing,
    totalSamples: samples.length,
    ratio,
    ratioPercent: ratio * 100,
    pass: missingTypes.length === 0 && ratio >= THRESHOLDS.shadowPassRatio,
  };
}

function evaluateFront(snapshot, viewport) {
  const points = snapshot.frontLine.points;
  const insideFrame = points.filter(point => pointInFrame(point, viewport));
  const insideSafeFrame = points.filter(point => pointInSafeFrame(point, viewport, THRESHOLDS.frontLineMargin));
  const battlefieldWidthRatio = snapshot.battlefieldRect?.width / viewport.width;
  const battlefieldCoveragePass = viewport.id !== '1280' ||
    (Number.isFinite(battlefieldWidthRatio) && battlefieldWidthRatio >= THRESHOLDS.landscapeBattlefieldWidthRatio);
  return {
    declaredVisible: snapshot.frontLine.visible,
    points,
    marginPercent: THRESHOLDS.frontLineMargin * 100,
    insideFrame: insideFrame.length,
    insideSafeFrame: insideSafeFrame.length,
    battlefieldRect: snapshot.battlefieldRect,
    battlefieldWidthRatio,
    requiredLandscapeBattlefieldWidthRatio: THRESHOLDS.landscapeBattlefieldWidthRatio,
    battlefieldCoveragePass,
    pass: snapshot.frontLine.visible && points.length > 0 && insideSafeFrame.length === points.length && battlefieldCoveragePass,
  };
}

function evaluateClipping(snapshot, viewport, expectedPrompt) {
  const question = snapshot.question;
  const reports = question.diagnosticsReported;
  const diagnosticsComplete = Object.values(reports).every(Boolean);
  const characterCountsComplete = Number.isFinite(question.visibleCharacters) && Number.isFinite(question.totalCharacters) &&
    question.visibleCharacters >= question.totalCharacters && question.totalCharacters > 0;
  const promptMatches = comparablePrompt(question.prompt) === comparablePrompt(expectedPrompt);
  const panelInFrame = rectInFrame(question.panelRect, viewport);
  const bodyInPanel = rectContains(question.panelRect, question.bodyRect);
  const textInBody = rectContains(question.bodyRect, question.renderedTextRect);
  return {
    expectedPrompt,
    renderedPrompt: question.prompt,
    promptMatches,
    panelRect: question.panelRect,
    bodyRect: question.bodyRect,
    renderedTextRect: question.renderedTextRect,
    panelInFrame,
    bodyInPanel,
    textInBody,
    diagnosticsReported: reports,
    diagnosticsComplete,
    isFolded: question.isFolded,
    hasEllipsis: question.hasEllipsis,
    isTruncated: question.isTruncated,
    isOverflowing: question.isOverflowing,
    visibleCharacters: question.visibleCharacters,
    totalCharacters: question.totalCharacters,
    characterCountsComplete,
    pass: promptMatches && panelInFrame && bodyInPanel && textInBody && diagnosticsComplete && characterCountsComplete &&
      !question.isFolded && !question.hasEllipsis && !question.isTruncated && !question.isOverflowing,
  };
}

function evaluateChoiceFixture(snapshot, viewport, fixture) {
  const choices = snapshot.choices;
  const actualChoices = choices.map(choice => choice.text);
  const exactChoices = choices.length === THRESHOLDS.choicePadCount &&
    JSON.stringify([...actualChoices].sort()) === JSON.stringify([...fixture.choices].sort());
  const labels = choices.map(choice => {
    const characterCountsComplete = Number.isFinite(choice.visibleCharacters) && Number.isFinite(choice.totalCharacters) &&
      choice.visibleCharacters >= choice.totalCharacters && choice.totalCharacters > 0;
    const tofuGlyphs = choice.glyphs.filter(glyph => glyph.char === '□' || glyph.char === '�');
    const bodyInFrame = rectInFrame(choice.bodyRect, viewport);
    const textInBody = rectContains(choice.bodyRect, choice.renderedTextRect);
    return {
      id: choice.id,
      text: choice.text,
      bodyRect: choice.bodyRect,
      renderedTextRect: choice.renderedTextRect,
      bodyInFrame,
      textInBody,
      isTruncated: choice.isTruncated,
      isOverflowing: choice.isOverflowing,
      hasMissingGlyph: choice.hasMissingGlyph,
      visibleCharacters: choice.visibleCharacters,
      totalCharacters: choice.totalCharacters,
      characterCountsComplete,
      glyphCount: choice.glyphs.length,
      tofuGlyphs,
      pass: bodyInFrame && textInBody && characterCountsComplete && choice.glyphs.length > 0 &&
        tofuGlyphs.length === 0 && !choice.hasMissingGlyph && !choice.isTruncated && !choice.isOverflowing,
    };
  });
  const labelOverlaps = [];
  for (let left = 0; left < choices.length; left++) {
    for (let right = left + 1; right < choices.length; right++) {
      labelOverlaps.push({
        left: choices[left].id,
        right: choices[right].id,
        intersection: intersection(choices[left].renderedTextRect, choices[right].renderedTextRect),
      });
    }
  }
  const worldLabels = evaluateWorldLabels(snapshot, viewport, 'normal');
  const totalIntersectionArea = labelOverlaps.reduce((sum, overlap) => sum + (overlap.intersection.area ?? 0), 0);
  return {
    expectedChoices: fixture.choices,
    actualChoices,
    exactChoices,
    labels,
    labelOverlaps,
    totalIntersectionArea,
    tofuGlyphCount: labels.reduce((sum, label) => sum + label.tofuGlyphs.length, 0),
    worldLabels,
    pass: exactChoices && labels.length === THRESHOLDS.choicePadCount && labels.every(label => label.pass) &&
      labelOverlaps.every(overlap => overlap.intersection.area === 0) && worldLabels.pass,
  };
}

async function measureCase(page, viewport, label, expectedPrompt, timeMs, report) {
  const raw = await readDebugSnapshot(page);
  const png = await page.screenshot({ encoding: 'base64' });
  if (KEEP_FRAMES) fs.writeFileSync(path.join(FRAME_DIR, `${label}.png`), Buffer.from(png, 'base64'));
  if (raw?.__error) throw new Error(`${label}: ${raw.__error}`);
  const snapshot = normalizeSnapshot(raw, viewport);
  if (snapshot.version < 3) throw new Error(`${label}: debug version ${snapshot.version || 'missing'}; v3 required`);
  const pixels = await measureScreenshot(page, png, snapshot, viewport);
  const result = {
    label,
    viewport: { id: viewport.id, width: viewport.width, height: viewport.height },
    timeMs,
    debugVersion: snapshot.version,
    screenshot: pixels.screenshot,
    glyphHeight: evaluateGlyphs(snapshot, pixels, viewport),
    uiObstruction: evaluatePads(snapshot, viewport),
    worldLabels: evaluateWorldLabels(snapshot, viewport, 'normal'),
    clipping: evaluateClipping(snapshot, viewport, expectedPrompt),
    raw,
  };
  report.measurements.push(result);
  return { result, snapshot, pixels };
}

const index = readPackIndex();
const records = readPacks(index);
const longest = longestChoiceCases(records);
const choiceFixtures = CHOICE_FIXTURE_SPECS.map(spec => {
  const record = records.find(candidate => candidate.id === spec.packId) ?? null;
  const item = record?.pack?.items?.find(candidate => candidate.id === spec.itemId) ?? null;
  const choicesMatch = Array.isArray(item?.choices) && JSON.stringify(item.choices) === JSON.stringify(spec.choices);
  return { ...spec, record, item, choicesMatch };
});
const beforeHashes = packHashes(index);
const packAudit = {
  count: records.length,
  expectedCount: THRESHOLDS.expectedPackCount,
  cases: longest.map(entry => ({
    packId: entry.packId,
    schemaVersion: entry.schemaVersion,
    totalItems: entry.totalItems,
    choiceItems: entry.choiceItems,
    longestId: entry.item?.id ?? null,
    longestPrompt: entry.item?.prompt ?? null,
    longestPromptCharacters: entry.item ? [...String(entry.item.prompt)].length : 0,
    pass: entry.schemaVersion === 3 && entry.choiceItems > 0 && !!entry.item,
  })),
};
packAudit.pass = packAudit.count === packAudit.expectedCount && packAudit.cases.every(entry => entry.pass);
const choiceFixtureAudit = {
  expectedFixtures: THRESHOLDS.expectedChoiceFixtures,
  cases: choiceFixtures.map(fixture => ({
    id: fixture.id,
    packId: fixture.packId,
    itemId: fixture.itemId,
    found: !!fixture.item,
    choicesMatch: fixture.choicesMatch,
    pass: fixture.record?.pack?.schema_version === 3 && !!fixture.item && fixture.choicesMatch,
  })),
};
choiceFixtureAudit.pass = choiceFixtureAudit.cases.length === choiceFixtureAudit.expectedFixtures && choiceFixtureAudit.cases.every(entry => entry.pass);

const report = {
  schemaVersion: 3,
  generatedAt: new Date().toISOString(),
  commit: commit(),
  thresholds: THRESHOLDS,
  inputs: {
    viewports: VIEWPORTS,
    timesMs: TIMES_MS,
    keepFrames: KEEP_FRAMES,
    longestPromptCasesSkipped: SKIP_LONGEST,
    game: GAME,
  },
  chrome: null,
  packAudit,
  choiceFixtureAudit,
  packIntegrity: { before: beforeHashes, after: null, stable: false },
  baseline: [],
  longestPromptCases: [],
  worldLabelRewardCases: [],
  choiceCases: [],
  choiceScreenshots: [],
  measurements: [],
  errors: [],
  consoleErrors: [],
  consoleWarnings: [],
  failedRequests: [],
  gates: null,
  overall: { pass: false },
};

if (KEEP_FRAMES) fs.mkdirSync(FRAME_DIR, { recursive: true });
fs.mkdirSync(FIX1_DIR, { recursive: true });
let browser;
let server;
let overlay = null;
let overlayPackId = '';
try {
  const opened = await openBrowserHarness();
  ({ browser, server } = opened);
  report.chrome = opened.chrome;
  const page = await browser.newPage();
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

  const defaultCase = longest.find(entry => entry.packId === index.default_pack && entry.item) ?? longest.find(entry => entry.item);
  if (!defaultCase) throw new Error('No choice item exists for the timed baseline');
  overlayPackId = defaultCase.packId;
  overlay = { ...defaultCase.pack, items: [defaultCase.item, ...defaultCase.pack.items.filter(item => item.id !== defaultCase.item.id)] };
  for (const viewport of VIEWPORTS) {
    await loadGame(page, server, viewport, `pack=${encodeURIComponent(defaultCase.packId)}&artprobe=1&v3gate=baseline-${viewport.id}`);
    await startGame(page);
    const startedAt = Date.now();
    for (const timeMs of TIMES_MS) {
      await sleep(Math.max(0, startedAt + timeMs - Date.now()));
      try {
        const measured = await measureCase(page, viewport, `baseline-${viewport.id}-${timeMs / 1000}s`, defaultCase.item.prompt, timeMs, report);
        const shadow = evaluateShadow(measured.snapshot, measured.pixels, viewport);
        const frontVisibility = evaluateFront(measured.snapshot, viewport);
        measured.result.shadow = shadow;
        measured.result.frontVisibility = frontVisibility;
        report.baseline.push(measured.result);
        console.log(`${viewport.id}@${timeMs}ms shadow=${shadow.passingSamples}/${shadow.totalSamples} front=${frontVisibility.pass} overlap=${measured.result.uiObstruction.totalIntersectionArea} glyph=${measured.result.glyphHeight.minimumPx}`);
      } catch (error) {
        report.errors.push(String(error?.stack || error));
      }
    }
  }

  if (!SKIP_LONGEST) {
    for (const entry of longest) {
      if (!entry.item) {
        report.errors.push(`${entry.packId}: no choice item for longest-prompt gate`);
        continue;
      }
      overlayPackId = entry.packId;
      overlay = { ...entry.pack, items: [entry.item, ...entry.pack.items.filter(item => item.id !== entry.item.id)] };
      for (const viewport of VIEWPORTS) {
        const label = `longest-${entry.packId}-${viewport.id}`;
        try {
          await loadGame(page, server, viewport, `pack=${encodeURIComponent(entry.packId)}&artprobe=1&v3gate=${encodeURIComponent(label)}`);
          await startGame(page);
          await sleep(850);
          const measured = await measureCase(page, viewport, label, entry.item.prompt, 850, report);
          const value = {
            ...measured.result,
            packId: entry.packId,
            itemId: entry.item.id,
            promptCharacters: [...String(entry.item.prompt)].length,
          };
          report.longestPromptCases.push(value);
          console.log(`${label} clip=${value.clipping.pass} glyph=${value.glyphHeight.minimumPx} overlap=${value.uiObstruction.totalIntersectionArea}`);
        } catch (error) {
          report.errors.push(String(error?.stack || error));
        }
      }
    }
  }

  overlayPackId = defaultCase.packId;
  overlay = { ...defaultCase.pack, items: [defaultCase.item, ...defaultCase.pack.items.filter(item => item.id !== defaultCase.item.id)] };
  for (const viewport of VIEWPORTS) {
    const label = `world-label-reward-${viewport.id}`;
    try {
      await loadGame(page, server, viewport, `pack=${encodeURIComponent(defaultCase.packId)}&artprobe=1&reward=tower&v3gate=${encodeURIComponent(label)}`);
      await startGame(page);
      await sleep(500);
      const accepted = await page.evaluate(() => window.__GAME_TEST__?.answerCorrect?.());
      if (!accepted) throw new Error(`${label}: __GAME_TEST__.answerCorrect() was not accepted`);
      await page.waitForFunction(() => {
        const state = window.__GAME_TEST__?.getState?.();
        return state?.feedbackVisible === true && state?.rewardKind === 'tower';
      }, { timeout: 12000, polling: 35 });
      await page.waitForFunction(() => {
        const state = window.__GAME_TEST__?.getState?.();
        const mask = Number(state?.worldLabelMask ?? 0);
        return (mask & 0x80) !== 0 && (mask & 0x7f) === 0;
      }, { timeout: 3000, polling: 25 });
      const raw = await readDebugSnapshot(page);
      if (raw?.__error) throw new Error(`${label}: ${raw.__error}`);
      const snapshot = normalizeSnapshot(raw, viewport);
      if (snapshot.version < 3) throw new Error(`${label}: debug version ${snapshot.version || 'missing'}; v3 required`);
      const state = await page.evaluate(() => window.__GAME_TEST__?.getState?.() ?? null);
      const worldLabels = evaluateWorldLabels(snapshot, viewport, 'reward');
      const value = {
        label,
        viewport: { id: viewport.id, width: viewport.width, height: viewport.height },
        rewardKind: state?.rewardKind ?? null,
        feedbackVisible: state?.feedbackVisible === true,
        worldLabels,
        pass: state?.rewardKind === 'tower' && state?.feedbackVisible === true && worldLabels.pass,
      };
      report.worldLabelRewardCases.push(value);
      console.log(`${label} reward=${value.rewardKind} labels=${worldLabels.labels.length} overlap=${worldLabels.totalIntersectionArea}`);
    } catch (error) {
      report.errors.push(String(error?.stack || error));
    }
  }

  for (const fixture of choiceFixtures) {
    if (!fixture.record || !fixture.item) continue;
    overlayPackId = fixture.packId;
    overlay = { ...fixture.record.pack, items: [fixture.item, ...fixture.record.pack.items.filter(item => item.id !== fixture.itemId)] };
    for (const viewport of VIEWPORTS) {
      const label = `choices-${fixture.id}-${viewport.id}`;
      const file = `${label}.png`;
      try {
        await loadGame(page, server, viewport, `pack=${encodeURIComponent(fixture.packId)}&artprobe=1&v3choices=1&v3gate=${encodeURIComponent(label)}`);
        await startGame(page);
        await sleep(850);
        const raw = await readDebugSnapshot(page);
        if (raw?.__error) throw new Error(`${label}: ${raw.__error}`);
        const snapshot = normalizeSnapshot(raw, viewport);
        if (snapshot.version < 3) throw new Error(`${label}: debug version ${snapshot.version || 'missing'}; v3 required`);
        const state = await page.evaluate(() => window.__GAME_TEST__?.getState?.() ?? null);
        const evidence = evaluateChoiceFixture(snapshot, viewport, fixture);
        evidence.questionId = state?.questionId ?? null;
        evidence.questionMatches = evidence.questionId === fixture.itemId;
        evidence.pass = evidence.pass && evidence.questionMatches;
        await page.screenshot({ path: path.join(FIX1_DIR, file) });
        report.choiceScreenshots.push(file);
        report.choiceCases.push({
          label,
          fixture: fixture.id,
          packId: fixture.packId,
          itemId: fixture.itemId,
          viewport: { id: viewport.id, width: viewport.width, height: viewport.height },
          screenshot: path.join('fix1', file),
          evidence,
          pass: evidence.pass,
        });
        console.log(`${label} exact=${evidence.exactChoices} clip=${evidence.labels.every(entry => entry.pass)} tofu=${evidence.tofuGlyphCount} overlap=${evidence.totalIntersectionArea}`);
      } catch (error) {
        report.errors.push(String(error?.stack || error));
      }
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

const expectedBaseline = VIEWPORTS.length * TIMES_MS.length;
const shadowSamples = report.baseline.flatMap(result => result.shadow?.samples ?? []);
const shadowPassing = shadowSamples.filter(sample => sample.pass).length;
const shadowRatio = shadowSamples.length ? shadowPassing / shadowSamples.length : 0;
const expectedLongest = SKIP_LONGEST ? 0 : THRESHOLDS.expectedPackCount * VIEWPORTS.length;
const glyphCases = report.measurements;
const byViewport = Object.fromEntries(VIEWPORTS.map(viewport => {
  const cases = glyphCases.filter(entry => entry.viewport.id === viewport.id);
  const heights = cases.flatMap(entry => entry.glyphHeight.glyphs.map(glyph => glyph.inkHeightPx)).filter(Number.isFinite);
  return [viewport.id, {
    requiredPx: THRESHOLDS.glyphHeight[viewport.id],
    measuredCases: cases.length,
    passingCases: cases.filter(entry => entry.glyphHeight.pass).length,
    minimumPx: heights.length ? Math.min(...heights) : null,
    pass: cases.length > 0 && cases.every(entry => entry.glyphHeight.pass),
  }];
}));

const gateA = {
  requiredDarknessPercent: THRESHOLDS.shadowDarkness * 100,
  requiredPassPercent: THRESHOLDS.shadowPassRatio * 100,
  passingSamples: shadowPassing,
  totalSamples: shadowSamples.length,
  ratio: shadowRatio,
  ratioPercent: shadowRatio * 100,
  perSnapshot: report.baseline.map(entry => ({ label: entry.label, ratioPercent: entry.shadow?.ratioPercent ?? 0, missingTypes: entry.shadow?.missingTypes ?? REQUIRED_SHADOW_TYPES, pass: entry.shadow?.pass === true })),
};
gateA.pass = report.baseline.length === expectedBaseline && report.baseline.every(entry => entry.shadow?.pass) && shadowRatio >= THRESHOLDS.shadowPassRatio;
const gateB = {
  marginPercent: THRESHOLDS.frontLineMargin * 100,
  passingSnapshots: report.baseline.filter(entry => entry.frontVisibility?.pass).length,
  expectedSnapshots: expectedBaseline,
  failures: report.baseline.filter(entry => !entry.frontVisibility?.pass).map(entry => ({ label: entry.label, evidence: entry.frontVisibility })),
};
gateB.pass = report.baseline.length === expectedBaseline && gateB.passingSnapshots === expectedBaseline;
const gateCMeasurements = report.measurements;
const gateC = {
  requiredIntersectionArea: 0,
  measuredCases: gateCMeasurements.length,
  expectedCases: expectedBaseline + expectedLongest,
  totalIntersectionArea: gateCMeasurements.reduce((sum, entry) => sum + (entry.uiObstruction?.totalIntersectionArea ?? 0), 0),
  failures: gateCMeasurements.filter(entry => !entry.uiObstruction?.pass).map(entry => ({ label: entry.label, evidence: entry.uiObstruction })),
};
gateC.pass = gateC.measuredCases === gateC.expectedCases && gateCMeasurements.every(entry => entry.uiObstruction?.pass);
const gateD = {
  thresholdsPx: THRESHOLDS.glyphHeight,
  byViewport,
  failures: glyphCases.filter(entry => !entry.glyphHeight.pass).map(entry => ({ label: entry.label, evidence: entry.glyphHeight })),
};
gateD.pass = Object.values(byViewport).every(value => value.pass) && glyphCases.length === expectedBaseline + expectedLongest;
const gateE = {
  expectedPacks: THRESHOLDS.expectedPackCount,
  expectedCases: expectedLongest,
  measuredCases: report.longestPromptCases.length,
  clippedCases: report.longestPromptCases.filter(entry => !entry.clipping.pass).length,
  failures: report.longestPromptCases.filter(entry => !entry.clipping.pass).map(entry => ({ packId: entry.packId, viewport: entry.viewport.id, evidence: entry.clipping })),
  skippedByQuickMode: SKIP_LONGEST,
};
gateE.pass = !SKIP_LONGEST && packAudit.pass && gateE.measuredCases === gateE.expectedCases && gateE.clippedCases === 0;

const normalWorldLabelCases = report.measurements;
const expectedNormalWorldLabelCases = expectedBaseline + expectedLongest;
const gateF = {
  requiredIntersectionArea: THRESHOLDS.worldLabelIntersectionArea,
  expectedNormalCases: expectedNormalWorldLabelCases,
  measuredNormalCases: normalWorldLabelCases.length,
  expectedRewardCases: VIEWPORTS.length,
  measuredRewardCases: report.worldLabelRewardCases.length,
  totalIntersectionArea: [...normalWorldLabelCases.map(entry => entry.worldLabels), ...report.worldLabelRewardCases.map(entry => entry.worldLabels)]
    .reduce((sum, evidence) => sum + (evidence?.totalIntersectionArea ?? 0), 0),
  normalFailures: normalWorldLabelCases.filter(entry => !entry.worldLabels?.pass).map(entry => ({ label: entry.label, evidence: entry.worldLabels })),
  rewardFailures: report.worldLabelRewardCases.filter(entry => !entry.pass).map(entry => ({ label: entry.label, evidence: entry })),
};
gateF.pass = gateF.measuredNormalCases === gateF.expectedNormalCases && normalWorldLabelCases.every(entry => entry.worldLabels?.pass) &&
  gateF.measuredRewardCases === gateF.expectedRewardCases && report.worldLabelRewardCases.every(entry => entry.pass) &&
  gateF.totalIntersectionArea === THRESHOLDS.worldLabelIntersectionArea;

const expectedChoiceCases = THRESHOLDS.expectedChoiceFixtures * VIEWPORTS.length;
const expectedChoiceScreenshots = CHOICE_FIXTURE_SPECS.flatMap(fixture => VIEWPORTS.map(viewport => `choices-${fixture.id}-${viewport.id}.png`));
const generatedChoiceScreenshots = new Set(report.choiceScreenshots);
const missingChoiceScreenshots = expectedChoiceScreenshots.filter(file => !generatedChoiceScreenshots.has(file) || !fs.existsSync(path.join(FIX1_DIR, file)));
const gateG = {
  expectedFixtures: THRESHOLDS.expectedChoiceFixtures,
  fixtureAuditPass: choiceFixtureAudit.pass,
  expectedCases: expectedChoiceCases,
  measuredCases: report.choiceCases.length,
  expectedScreenshots: expectedChoiceScreenshots,
  generatedScreenshots: report.choiceScreenshots,
  missingScreenshots: missingChoiceScreenshots,
  clippedCases: report.choiceCases.filter(entry => entry.evidence.labels.some(label => !label.pass)).length,
  tofuGlyphs: report.choiceCases.reduce((sum, entry) => sum + entry.evidence.tofuGlyphCount, 0),
  totalChoiceIntersectionArea: report.choiceCases.reduce((sum, entry) => sum + entry.evidence.totalIntersectionArea, 0),
  totalWorldLabelIntersectionArea: report.choiceCases.reduce((sum, entry) => sum + entry.evidence.worldLabels.totalIntersectionArea, 0),
  packIntegrityStable: report.packIntegrity.stable,
  failures: report.choiceCases.filter(entry => !entry.pass).map(entry => ({ label: entry.label, evidence: entry.evidence })),
};
gateG.pass = choiceFixtureAudit.pass && gateG.measuredCases === gateG.expectedCases && report.choiceCases.every(entry => entry.pass) &&
  report.choiceScreenshots.length === expectedChoiceCases && missingChoiceScreenshots.length === 0 && gateG.clippedCases === 0 &&
  gateG.tofuGlyphs === 0 && gateG.totalChoiceIntersectionArea === 0 && gateG.totalWorldLabelIntersectionArea === 0 &&
  report.packIntegrity.stable;

report.gates = {
  a_groundShadow: gateA,
  b_frontLineVisibility: gateB,
  c_zeroUiObstruction: gateC,
  d_renderedGlyphHeight: gateD,
  e_longestPromptClipping: gateE,
  f_zeroWorldLabelOverlap: gateF,
  g_choiceRendering: gateG,
};
const harnessPass = report.errors.length === 0 && report.consoleErrors.length === 0 && report.failedRequests.length === 0 && report.packIntegrity.stable;
report.overall = {
  pass: harnessPass && Object.values(report.gates).every(gate => gate.pass),
  harnessPass,
  packAuditPass: packAudit.pass,
  packIntegrityPass: report.packIntegrity.stable,
  gatePasses: Object.fromEntries(Object.entries(report.gates).map(([key, gate]) => [key, gate.pass])),
};

fs.writeFileSync(OUT, JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({ output: OUT, gates: report.overall.gatePasses, overall: report.overall, errors: report.errors }, null, 2));
if (!report.overall.pass) process.exitCode = 1;
