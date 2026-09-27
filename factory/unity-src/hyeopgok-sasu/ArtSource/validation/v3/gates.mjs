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
});
const KEEP_FRAMES = process.env.V3_KEEP_FRAMES !== '0';
const SKIP_LONGEST = process.argv.includes('--quick') || process.env.V3_SKIP_LONGEST === '1';
const OUT = path.join(HERE, 'gates.json');
const FRAME_DIR = path.join(HERE, 'gate-frames');

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
  return {
    declaredVisible: snapshot.frontLine.visible,
    points,
    marginPercent: THRESHOLDS.frontLineMargin * 100,
    insideFrame: insideFrame.length,
    insideSafeFrame: insideSafeFrame.length,
    pass: snapshot.frontLine.visible && points.length > 0 && insideSafeFrame.length === points.length,
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
    clipping: evaluateClipping(snapshot, viewport, expectedPrompt),
    raw,
  };
  report.measurements.push(result);
  return { result, snapshot, pixels };
}

const index = readPackIndex();
const records = readPacks(index);
const longest = longestChoiceCases(records);
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
  packIntegrity: { before: beforeHashes, after: null, stable: false },
  baseline: [],
  longestPromptCases: [],
  measurements: [],
  errors: [],
  consoleErrors: [],
  consoleWarnings: [],
  failedRequests: [],
  gates: null,
  overall: { pass: false },
};

if (KEEP_FRAMES) fs.mkdirSync(FRAME_DIR, { recursive: true });
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

report.gates = {
  a_groundShadow: gateA,
  b_frontLineVisibility: gateB,
  c_zeroUiObstruction: gateC,
  d_renderedGlyphHeight: gateD,
  e_longestPromptClipping: gateE,
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
