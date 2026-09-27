// Real-pointer regression for the v3 answer and upgrade pad rectangles.
//
// Each case starts a fresh run, projects the authored ground rectangle's four
// 5%-inset corners plus its centre, dispatches one trusted CDP touch, and then
// waits for the game's normal movement + 0.4 second confirmation dwell.

// Run from the repository root after the WebGL build:
//   node factory/unity-src/hyeopgok-sasu/ArtSource/validation/v3/fix2/tap-grid.mjs

import fs from 'node:fs';
import path from 'node:path';
import {
  VIEWPORTS,
  attachDiagnostics,
  currentState,
  loadGame,
  openBrowserHarness,
  packHashes,
  readPackIndex,
  sleep,
  startGame,
  touch,
} from '../harness-lib.mjs';

const HERE = path.dirname(new URL(import.meta.url).pathname);
const OUT = path.join(HERE, 'tap-grid.json');
const POINTS = Object.freeze(['north-west-5%', 'north-east-5%', 'south-east-5%', 'south-west-5%', 'center']);
const INPUT_METHOD = 'CDP Input.dispatchTouchEvent touchStart/touchEnd';
const PAD_CENTERS = Object.freeze({
  choice: Object.freeze([{ x: -1.8, z: 2 }, { x: 0.6, z: 2 }, { x: -1.8, z: -1 }, { x: 0.6, z: -1 }]),
  upgrade: Object.freeze([{ x: 0.92, z: -4.35 }, { x: 0.98, z: 3.42 }, { x: -2.52, z: -4.22 }]),
});
const index = readPackIndex();
const ONLY = String(process.env.FIX2_ONLY || '').split(':');
const selectedViewports = VIEWPORTS.filter(viewport => !ONLY[0] || viewport.id === ONLY[0]);
const wanted = (kind, pad, point) => (!ONLY[1] || ONLY[1] === kind) && (!ONLY[2] || Number(ONLY[2]) === pad) && (!ONLY[3] || Number(ONLY[3]) === point);
const expectedCases = selectedViewports.reduce((sum) => sum + ['choice', 'upgrade'].reduce((kindSum, kind) => {
  const pads = kind === 'choice' ? 4 : 3;
  for (let pad = 0; pad < pads; pad += 1) for (let point = 0; point < POINTS.length; point += 1) if (wanted(kind, pad, point)) kindSum += 1;
  return kindSum;
}, 0), 0);

const report = {
  schemaVersion: 1,
  generatedAt: new Date().toISOString(),
  inputMethod: INPUT_METHOD,
  rectangleContract: {
    pointsPerPad: POINTS,
    cornerInsetFromEachEdge: '5%',
    choiceVisualHalfExtents: { x: 0.9125, z: 0.8925 },
    upgradeVisualHalfExtents: { x: 0.7575, z: 0.6175 },
    hitPadding: '8%',
    confirmationDwellSeconds: 0.4,
  },
  viewports: VIEWPORTS.map(({ id, width, height, deviceScaleFactor }) => ({ id, width, height, deviceScaleFactor })),
  packIntegrity: { before: packHashes(index), after: null, stable: false },
  neutralTutorial: [],
  cases: [],
  filter: process.env.FIX2_ONLY || null,
  counts: { expected: expectedCases, measured: 0, passed: 0, failed: 0 },
  chrome: null,
  errors: [],
  consoleErrors: [],
  consoleWarnings: [],
  failedRequests: [],
  overall: { pass: false },
};

const clone = value => JSON.parse(JSON.stringify(value));
const numeric = value => Number.isFinite(Number(value)) ? Number(value) : null;

function projectedPoint(state, field, pad, point, viewport) {
  const flat = state?.[field];
  const at = pad * 10 + point * 2;
  if (!Array.isArray(flat) || flat.length < at + 2) throw new Error(`${field}[${pad},${point}] is missing`);
  const normalized = { x: numeric(flat[at]), y: numeric(flat[at + 1]) };
  if (normalized.x == null || normalized.y == null) throw new Error(`${field}[${pad},${point}] is not finite`);
  return {
    normalized,
    css: { x: normalized.x * viewport.width, y: normalized.y * viewport.height },
  };
}

async function inputEvents(page) {
  return page.evaluate(() => JSON.parse(JSON.stringify(window.__HYEOPGOK_FIX2_INPUT__ || [])));
}

async function projectionSettled(page, field, timeoutMs = 3500) {
  const started = Date.now();
  let previous = null;
  let stable = 0;
  // Let the landing camera settle before freezing the projection used for a
  // one-pixel-precise corner tap.
  await sleep(700);
  while (Date.now() - started < timeoutMs) {
    const state = await currentState(page);
    const current = Array.isArray(state?.[field]) ? state[field].map(Number) : null;
    if (current && previous && current.length === previous.length) {
      const maximumDelta = current.reduce((value, coordinate, at) => Math.max(value, Math.abs(coordinate - previous[at])), 0);
      stable = maximumDelta <= 0.0005 ? stable + 1 : 0;
      if (stable >= 3) return state;
    }
    previous = current;
    await sleep(180);
  }
  throw new Error(`${field} projection did not settle`);
}

async function freshRun(page, field) {
  await startGame(page);
  await page.waitForFunction(() => {
    const state = window.__GAME_TEST__?.getState?.();
    return state?.phase === 'playing' && state?.attempts === 0;
  }, { timeout: 15000, polling: 25 });
  return projectionSettled(page, field);
}

async function firstQuestionTutorial(page) {
  await startGame(page);
  await page.waitForFunction(() => {
    const state = window.__GAME_TEST__?.getState?.();
    return state?.phase === 'playing' && state?.attempts === 0;
  }, { timeout: 15000, polling: 25 });
  await sleep(250);
  return currentState(page);
}

async function waitForDwell(page, kind, before, pad) {
  if (kind === 'choice') {
    await page.waitForFunction(prior => {
      const state = window.__GAME_TEST__?.getState?.();
      return state?.attempts === prior && Number(state?.dwell) > 0;
    }, { timeout: 6000, polling: 20 }, before.attempts);
  } else {
    await page.waitForFunction((spent, target) => {
      const state = window.__GAME_TEST__?.getState?.();
      return state?.spent === spent && state?.activeUpgradePad === target && Number(state?.upgradeDwell) > 0;
    }, { timeout: 6000, polling: 20 }, before.spent, pad);
  }
  return currentState(page);
}

function trustedTouch(events) {
  const relevant = events.filter(event => event.type === 'pointerdown' || event.type === 'pointerup' || event.type === 'touchstart' || event.type === 'touchend');
  const hasDown = relevant.some(event => event.type === 'pointerdown' || event.type === 'touchstart');
  const hasUp = relevant.some(event => event.type === 'pointerup' || event.type === 'touchend');
  return { relevant, pass: hasDown && hasUp && relevant.every(event => event.isTrusted === true) };
}

async function exerciseChoice(page, viewport, pad, point) {
  const state = await freshRun(page, 'choicePadTapPoints');
  const target = projectedPoint(state, 'choicePadTapPoints', pad, point, viewport);
  const beforeEvents = await inputEvents(page);
  const before = clone(state);
  const started = Date.now();
  await touch(page, target.css.x, target.css.y, 80);
  let dwellError = null;
  let dwell;
  try {
    dwell = await waitForDwell(page, 'choice', before, pad);
  } catch (error) {
    dwellError = String(error?.message || error);
    dwell = await currentState(page);
  }
  let confirmationError = null;
  try {
    await page.waitForFunction(attempts => window.__GAME_TEST__?.getState?.().attempts === attempts + 1,
      { timeout: 6000, polling: 20 }, before.attempts);
  } catch (error) {
    confirmationError = String(error?.message || error);
  }
  const after = await currentState(page);
  await sleep(250);
  const stable = await currentState(page);
  const allEvents = await inputEvents(page);
  const pointer = trustedTouch(allEvents.slice(beforeEvents.length));
  const intended = PAD_CENTERS.choice[pad];
  const intendedPadReached = Math.abs(numeric(after.kingX) - intended.x) <= 0.001 && Math.abs(numeric(after.kingZ) - intended.z) <= 0.001;
  const pass = dwellError === null && confirmationError === null && after.attempts === before.attempts + 1 && stable.attempts === after.attempts &&
    numeric(dwell.dwell) > 0 && intendedPadReached && pointer.pass;
  return {
    kind: 'choice', pad, point: POINTS[point], target, inputMethod: INPUT_METHOD,
    before: { attempts: before.attempts, moves: before.moves, dwell: before.dwell, kingX: before.kingX, kingZ: before.kingZ },
    dwellObserved: { attempts: dwell.attempts, dwell: dwell.dwell },
    after: { attempts: after.attempts, moves: after.moves, pending: after.pending, dwell: after.dwell, lastCorrect: after.lastCorrect, kingX: after.kingX, kingZ: after.kingZ,
      screenWidth: after.screenWidth, screenHeight: after.screenHeight, lastPointerX: after.lastPointerX, lastPointerY: after.lastPointerY,
      lastPointerWorldX: after.lastPointerWorldX, lastPointerWorldZ: after.lastPointerWorldZ },
    stableAfter250ms: { attempts: stable.attempts },
    intendedPadReached,
    pointerEvents: pointer.relevant,
    elapsedMs: Date.now() - started,
    dwellError,
    confirmationError,
    pass,
  };
}

async function prepareUpgrades(page) {
  await freshRun(page, 'upgradePadTapPoints');
  for (let attempt = 1; attempt <= 3; attempt += 1) {
    const accepted = await page.evaluate(() => window.__GAME_TEST__?.answerCorrect?.());
    if (!accepted) throw new Error(`upgrade setup answer ${attempt} was not accepted`);
    await page.waitForFunction(expected => window.__GAME_TEST__?.getState?.().attempts === expected,
      { timeout: 6000, polling: 20 }, attempt);
  }
  await page.waitForFunction(() => {
    const state = window.__GAME_TEST__?.getState?.();
    return state?.phase === 'playing' && state?.feedbackVisible === false && state?.builtTowers === 3 &&
      state?.coins > 0 && Array.isArray(state?.towerLevels) && state.towerLevels.every(level => level >= 0);
  }, { timeout: 8000, polling: 35 });
  return projectionSettled(page, 'upgradePadTapPoints');
}

async function exerciseUpgrade(page, viewport, pad, point) {
  const state = await prepareUpgrades(page);
  const target = projectedPoint(state, 'upgradePadTapPoints', pad, point, viewport);
  const beforeEvents = await inputEvents(page);
  const before = clone(state);
  if (!(before.upgradeRemaining?.[pad] > 0)) throw new Error(`upgrade-${pad} has no remaining cost`);
  const started = Date.now();
  await touch(page, target.css.x, target.css.y, 80);
  let dwellError = null;
  let dwell;
  try {
    dwell = await waitForDwell(page, 'upgrade', before, pad);
  } catch (error) {
    dwellError = String(error?.message || error);
    dwell = await currentState(page);
  }
  let confirmationError = null;
  try {
    await page.waitForFunction((spent, targetPad, remaining) => {
      const state = window.__GAME_TEST__?.getState?.();
      return state?.spent > spent && state?.upgradeRemaining?.[targetPad] < remaining;
    }, { timeout: 7000, polling: 20 }, before.spent, pad, before.upgradeRemaining[pad]);
  } catch (error) {
    confirmationError = String(error?.message || error);
  }
  const after = await currentState(page);
  const allEvents = await inputEvents(page);
  const pointer = trustedTouch(allEvents.slice(beforeEvents.length));
  const otherPadsStable = before.upgradeRemaining.every((remaining, at) => at === pad || after.upgradeRemaining[at] === remaining);
  const intended = PAD_CENTERS.upgrade[pad];
  const intendedPadReached = Math.abs(numeric(after.kingX) - intended.x) <= 0.001 && Math.abs(numeric(after.kingZ) - intended.z) <= 0.001;
  const pass = after.spent > before.spent && after.upgradeRemaining[pad] < before.upgradeRemaining[pad] &&
    dwellError === null && confirmationError === null && after.attempts === before.attempts && numeric(dwell.upgradeDwell) > 0 && dwell.spent === before.spent &&
    dwell.activeUpgradePad === pad && intendedPadReached && otherPadsStable && pointer.pass;
  return {
    kind: 'upgrade', pad, point: POINTS[point], target, inputMethod: INPUT_METHOD,
    before: { attempts: before.attempts, spent: before.spent, coins: before.coins, upgradeRemaining: before.upgradeRemaining },
    dwellObserved: { activeUpgradePad: dwell.activeUpgradePad, upgradeDwell: dwell.upgradeDwell, spent: dwell.spent },
    after: { attempts: after.attempts, spent: after.spent, coins: after.coins, kingX: after.kingX, kingZ: after.kingZ, activeUpgradePad: after.activeUpgradePad,
      upgradeDwell: after.upgradeDwell, upgradePhase: after.upgradePhase, upgradeRemaining: after.upgradeRemaining },
    pointerEvents: pointer.relevant,
    elapsedMs: Date.now() - started,
    intendedPadReached,
    otherPadsStable,
    dwellError,
    confirmationError,
    pass,
  };
}

let browser;
let server;
let page;
try {
  const opened = await openBrowserHarness();
  ({ browser, server } = opened);
  report.chrome = opened.chrome;
  const newPage = async () => {
    const next = await browser.newPage();
    next.setDefaultTimeout(90000);
    attachDiagnostics(next, report);
    await next.evaluateOnNewDocument(() => {
      const events = [];
      window.__HYEOPGOK_FIX2_INPUT__ = events;
      const record = event => events.push({
        type: event.type,
        pointerType: event.pointerType || 'touch',
        isTrusted: event.isTrusted,
        x: Number(event.clientX ?? event.changedTouches?.[0]?.clientX ?? 0),
        y: Number(event.clientY ?? event.changedTouches?.[0]?.clientY ?? 0),
        at: performance.now(),
      });
      for (const type of ['pointerdown', 'pointerup', 'touchstart', 'touchend']) window.addEventListener(type, record, true);
    });
    return next;
  };

  for (const viewport of selectedViewports) {
    if (page) await page.close();
    page = await newPage();
    await loadGame(page, server, viewport, `pack=${encodeURIComponent(index.default_pack)}&artprobe=1&reward=tower&fix2=tap-grid-${viewport.id}`);
    // Capture the first-use hint promptly; coordinate settling belongs to the
    // later tap cases and must not consume the hint's intentionally short life.
    const tutorialState = await firstQuestionTutorial(page);
    const tutorial = {
      viewport: viewport.id,
      text: tutorialState.tutorialHint,
      arrowVisible: tutorialState.tutorialArrowVisible,
      handVisible: tutorialState.tutorialHandVisible,
      pass: tutorialState.tutorialHint === '보기 패드를 탭하세요' && tutorialState.tutorialArrowVisible === false && tutorialState.tutorialHandVisible === false,
    };
    report.neutralTutorial.push(tutorial);
    await page.screenshot({ path: path.join(HERE, `tutorial-${viewport.id}.png`) });

    for (let pad = 0; pad < 4; pad += 1) {
      for (let point = 0; point < POINTS.length; point += 1) {
        if (!wanted('choice', pad, point)) continue;
        const result = await exerciseChoice(page, viewport, pad, point);
        result.viewport = viewport.id;
        report.cases.push(result);
        console.log(`${viewport.id} choice-${pad} ${POINTS[point]} dwell=${result.dwellObserved.dwell} pass=${result.pass}`);
      }
    }
    for (let pad = 0; pad < 3; pad += 1) {
      for (let point = 0; point < POINTS.length; point += 1) {
        if (!wanted('upgrade', pad, point)) continue;
        const result = await exerciseUpgrade(page, viewport, pad, point);
        result.viewport = viewport.id;
        report.cases.push(result);
        console.log(`${viewport.id} upgrade-${pad} ${POINTS[point]} dwell=${result.dwellObserved.upgradeDwell} pass=${result.pass}`);
      }
    }
  }
} catch (error) {
  report.errors.push(String(error?.stack || error));
} finally {
  if (page) await page.close().catch(() => {});
  if (browser) await browser.close().catch(() => {});
  if (server) await server.close().catch(() => {});
  report.packIntegrity.after = packHashes(index);
  report.packIntegrity.stable = JSON.stringify(report.packIntegrity.before) === JSON.stringify(report.packIntegrity.after);
}

report.counts.measured = report.cases.length;
report.counts.passed = report.cases.filter(result => result.pass).length;
report.counts.failed = report.cases.filter(result => !result.pass).length;
report.overall = {
  pass: report.counts.measured === report.counts.expected && report.counts.failed === 0 &&
    report.neutralTutorial.length === selectedViewports.length && report.neutralTutorial.every(result => result.pass) &&
    report.errors.length === 0 && report.consoleErrors.length === 0 && report.failedRequests.length === 0 && report.packIntegrity.stable,
  tapCasesPass: report.counts.measured === report.counts.expected && report.counts.failed === 0,
  neutralTutorialPass: report.neutralTutorial.length === selectedViewports.length && report.neutralTutorial.every(result => result.pass),
  diagnosticsPass: report.errors.length === 0 && report.consoleErrors.length === 0 && report.failedRequests.length === 0,
  packIntegrityPass: report.packIntegrity.stable,
};

fs.writeFileSync(OUT, JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({ output: OUT, counts: report.counts, neutralTutorial: report.neutralTutorial, overall: report.overall, errors: report.errors }, null, 2));
if (!report.overall.pass) process.exitCode = 1;
