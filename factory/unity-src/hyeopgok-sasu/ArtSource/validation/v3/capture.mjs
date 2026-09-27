// 협곡 사수 v3 final evidence capture. Every gameplay transition is reached
// through the public __GAME_TEST__ contract; no state is injected or forced.
//
// Usage:
//   node factory/unity-src/hyeopgok-sasu/ArtSource/validation/v3/capture.mjs
//   ONLY=title,packs,play node .../capture.mjs

import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import {
  HERE,
  VIEWPORTS,
  attachDiagnostics,
  centerOf,
  currentState,
  loadGame,
  longestChoiceCases,
  normalizeSnapshot,
  openBrowserHarness,
  packHashes,
  readDebugSnapshot,
  readPackIndex,
  readPacks,
  sleep,
  startGame,
  touch,
} from './harness-lib.mjs';

const OUT = path.join(HERE, 'final');
const only = new Set((process.env.ONLY || '').split(',').map(value => value.trim()).filter(Boolean));
const allowedOnly = new Set(['title', 'packs', 'play', 'after4correct', 'upgrade', 'chest', 'wrong', 'geometry', 'victory']);
const unknownOnly = [...only].filter(value => !allowedOnly.has(value));
if (unknownOnly.length) throw new Error(`Unknown ONLY capture group(s): ${unknownOnly.join(', ')}`);
const want = key => only.size === 0 || only.has(key);
const index = readPackIndex();
const records = readPacks(index);
const choiceCases = longestChoiceCases(records).filter(entry => entry.item);
const defaultCase = choiceCases.find(entry => entry.packId === index.default_pack) ?? choiceCases[0];
if (!defaultCase) throw new Error('No choice item is available for v3 capture');
fs.mkdirSync(OUT, { recursive: true });

const report = {
  schemaVersion: 3,
  generatedAt: new Date().toISOString(),
  output: OUT,
  chrome: null,
  packIntegrity: { before: packHashes(index), after: null, stable: false },
  frames: [],
  errors: [],
  consoleErrors: [],
  consoleWarnings: [],
  failedRequests: [],
  requiredFrames: [],
  pass: false,
};

const expectedNames = view => [
  `title-${view.id}.png`,
  `packs-${view.id}.png`,
  `play-${view.id}-3s.png`,
  `play-${view.id}-8s.png`,
  `play-${view.id}-15s.png`,
  `after4correct-${view.id}.png`,
  `upgrade-pour-${view.id}.png`,
  `chest-coin-${view.id}.png`,
  `chest-tower-${view.id}.png`,
  `wrong-feedback-${view.id}.png`,
  `geometry-${view.id}.png`,
  `victory-${view.id}.png`,
];
report.requiredFrames = VIEWPORTS.flatMap(expectedNames);

let browser;
let server;
let page;
let overlay = { ...defaultCase.pack, items: [defaultCase.item, ...defaultCase.pack.items.filter(item => item.id !== defaultCase.item.id)] };
let overlayPackId = defaultCase.packId;

function stateNumber(state, ...keys) {
  for (const key of keys) if (Number.isFinite(Number(state?.[key]))) return Number(state[key]);
  return null;
}

function rewardKind(state) {
  const value = String(state?.rewardKind ?? state?.chestReward ?? state?.reward?.kind ?? '').toLowerCase();
  if (/coin|gold|코인/.test(value)) return 'coin';
  if (/tower|crossbow|cannon|magic|석궁|대포|마법/.test(value)) return 'tower';
  return '';
}

function rewardSerial(state) {
  return stateNumber(state, 'rewardSerial', 'rewardCount', 'chestSerial', 'chestsOpened') ?? `${rewardKind(state)}:${state?.questionId ?? ''}:${state?.attempts ?? ''}`;
}

async function load(view, packCase = defaultCase, suffix = '') {
  overlayPackId = packCase.packId;
  overlay = { ...packCase.pack, items: [packCase.item, ...packCase.pack.items.filter(item => item.id !== packCase.item.id)] };
  await loadGame(page, server, view, `pack=${encodeURIComponent(packCase.packId)}&artprobe=1&v3capture=${encodeURIComponent(`${view.id}-${suffix}-${Date.now()}`)}`);
}

async function shot(file, note = '', extra = null) {
  const state = await currentState(page).catch(() => null);
  await page.screenshot({ path: path.join(OUT, file) });
  report.frames.push({ file, note, extra, state });
  console.log(`frame ${file}`);
}

async function waitUntil(predicate, timeoutMs = 15000) {
  const deadline = Date.now() + timeoutMs;
  let state;
  while (Date.now() < deadline) {
    state = await currentState(page);
    if (predicate(state)) return state;
    await sleep(30);
  }
  throw new Error(`state timeout: ${JSON.stringify(state)}`);
}

async function answer(method, settle = true) {
  const before = await currentState(page);
  const beforeAttempts = stateNumber(before, 'attempts', 'answered', 'questionAttempts') ?? 0;
  const beforeSolved = stateNumber(before, 'solved', 'correct', 'score') ?? 0;
  const accepted = await page.evaluate(command => window.__GAME_TEST__?.[command]?.(), method);
  if (!accepted) throw new Error(`__GAME_TEST__.${method}() was not accepted`);
  const observed = await waitUntil(state => {
    const attempts = stateNumber(state, 'attempts', 'answered', 'questionAttempts');
    const solved = stateNumber(state, 'solved', 'correct', 'score');
    return (attempts != null && attempts > beforeAttempts) || (solved != null && solved > beforeSolved) ||
      state?.feedbackVisible === true || state?.rewardPhase === true || /feedback|reward|chest/i.test(String(state?.phase));
  }, 12000);
  if (settle) {
    await sleep(1650);
    await waitUntil(state => state?.phase !== 'loading', 3000).catch(() => {});
  }
  return observed;
}

async function fourCorrect(view) {
  await load(view, defaultCase, 'after4');
  await startGame(page);
  await sleep(650);
  for (let count = 0; count < 4; count++) await answer('answerCorrect', true);
  await shot(`after4correct-${view.id}.png`, 'four correct answers through real choice-pad command path');
}

async function captureWrong(view) {
  await load(view, defaultCase, 'wrong');
  await startGame(page);
  await sleep(500);
  const observed = await answer('answerWrong', false);
  await waitUntil(state => state?.lastCorrect === false || state?.wrongFeedback === true || /wrong|feedback/i.test(String(state?.feedback ?? state?.phase)), 2500).catch(() => observed);
  await sleep(120);
  await shot(`wrong-feedback-${view.id}.png`, 'wrong pad feedback before the automatic 1.5 second advance');
}

async function readUpgradeTarget(view, state = null) {
  const raw = await readDebugSnapshot(page);
  if (raw?.__error) throw new Error(raw.__error);
  const snapshot = normalizeSnapshot(raw, view);
  const visible = snapshot.upgradePadRects.filter(candidate => candidate.visible && candidate.active && candidate.rect);
  const pad = state ? visible.find(candidate => {
    const slot = Number(String(candidate.id).match(/(\d+)$/)?.[1]);
    if (!Number.isInteger(slot)) return false;
    const level = Array.isArray(state.towerLevels) ? Number(state.towerLevels[slot]) : 0;
    const remaining = Array.isArray(state.upgradeRemaining) ? Number(state.upgradeRemaining[slot]) : 1;
    return level >= 0 && remaining > 0;
  }) : visible[0];
  if (!pad) throw new Error('No visible built tower with a remaining upgrade cost in v3 debug snapshot');
  return { pad, point: centerOf(pad.rect) };
}

async function captureUpgrade(view) {
  await load(view, defaultCase, 'upgrade');
  await startGame(page);
  await sleep(650);
  let target;
  for (let attempt = 0; attempt < 10; attempt++) {
    const state = await currentState(page);
    const coins = stateNumber(state, 'coins', 'gold') ?? 0;
    target = await readUpgradeTarget(view, state).catch(() => null);
    const slot = target ? Number(String(target.pad.id).match(/(\d+)$/)?.[1]) : -1;
    const remaining = slot >= 0 && Array.isArray(state?.upgradeRemaining) ? Number(state.upgradeRemaining[slot]) : null;
    // A partial payment is a valid capture: the feature deliberately counts the
    // displayed cost down one coin at a time rather than requiring full funding.
    if (target && coins > 0 && (remaining == null || remaining > 0)) break;
    if (state?.phase !== 'playing') break;
    await answer('answerCorrect', true);
  }
  if (!target) target = await readUpgradeTarget(view, await currentState(page));
  const candidates = [[.5, .5], [.5, .38], [.5, .62], [.38, .5], [.62, .5], [.38, .38], [.62, .38], [.38, .62], [.62, .62]];
  let pouring = null;
  for (const [fx, fy] of candidates) {
    const state = await currentState(page);
    target = await readUpgradeTarget(view, state).catch(() => target);
    const point = {
      x: target.pad.rect.x + target.pad.rect.width * fx,
      y: target.pad.rect.y + target.pad.rect.height * fy,
    };
    await touch(page, point.x, point.y, 160);
    pouring = await waitUntil(candidate => candidate?.upgradePouring === true || candidate?.upgradePhase === 'pouring' ||
      candidate?.upgradeCostCountdown === true || /pour|countdown|upgrade/i.test(String(candidate?.interactionPhase ?? '')), 2600).catch(() => null);
    if (pouring) {
      await shot(`upgrade-pour-${view.id}.png`, 'real touch on an affordable tower upgrade pad during the cost countdown/pour', { pad: target.pad, point, observed: pouring });
      return;
    }
  }
  throw new Error(`upgrade capture could not enter the pour state: ${JSON.stringify(await currentState(page))}`);
}

async function captureRewards(view) {
  const captured = new Set();
  let lastSerial = null;
  for (let session = 0; session < 8 && captured.size < 2; session++) {
    await load(view, defaultCase, `reward-${session}`);
    await startGame(page);
    await sleep(500);
    for (let question = 0; question < 10 && captured.size < 2; question++) {
      const before = await currentState(page);
      lastSerial = rewardSerial(before);
      const state = await answer('answerCorrect', false);
      let observed = state;
      try {
        observed = await waitUntil(candidate => {
          const kind = rewardKind(candidate);
          return kind && (rewardSerial(candidate) !== lastSerial || candidate?.chestOpen === true || candidate?.rewardPhase === true);
        }, 4500);
      } catch {}
      const kind = rewardKind(observed);
      if (kind && !captured.has(kind)) {
        observed = await waitUntil(candidate => {
          const mask = Number(candidate?.worldLabelMask) || 0;
          return (mask & 0x80) !== 0 && (mask & 0x7f) === 0;
        }, 2500);
        await shot(`chest-${kind}-${view.id}.png`, `naturally drawn ${kind} chest reward`, { observed });
        captured.add(kind);
      }
      await sleep(1650);
      const after = await currentState(page);
      if (after?.phase !== 'playing') break;
    }
  }
  if (!captured.has('coin') || !captured.has('tower')) throw new Error(`reward capture did not naturally observe both variants: ${[...captured].join(',') || 'none'}`);
}

async function captureVictory(view) {
  await load(view, defaultCase, 'victory');
  await startGame(page);
  await sleep(500);
  for (let answerIndex = 0; answerIndex < 12; answerIndex++) {
    const state = await currentState(page);
    if (state?.victory === true || /victory|won|complete/i.test(String(state?.phase))) break;
    if (state?.phase !== 'playing' && answerIndex > 0) break;
    await answer('answerCorrect', true);
  }
  await waitUntil(state => state?.victory === true || /victory|won|complete/i.test(String(state?.phase)), 15000);
  await sleep(350);
  await shot(`victory-${view.id}.png`, 'completed choice pack through real correct-pad command path');
}

function geometryCase() {
  const candidates = choiceCases.flatMap(packCase => packCase.pack.items.filter(item => item.answer_mode === 'choice' && Array.isArray(item.choices) && item.choices.length === 4)
    .map(item => ({ ...packCase, item })));
  const score = candidate => {
    const prompt = String(candidate.item.prompt || '');
    return (prompt.match(/[△∠°□⊥∥]/gu) || []).length * 100 + [...prompt].length;
  };
  return candidates.reduce((best, candidate) => !best || score(candidate) > score(best) ? candidate : best, null) ?? defaultCase;
}

try {
  const opened = await openBrowserHarness();
  ({ browser, server } = opened);
  report.chrome = opened.chrome;
  page = await browser.newPage();
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

  for (const view of VIEWPORTS) {
    if (want('title') || want('packs')) {
      await load(view, defaultCase, 'title');
      if (want('title')) await shot(`title-${view.id}.png`);
      if (want('packs')) {
        const x = ((view.mobile ? 0.5 : 0.23) - 108 * view.height / (844 * view.width)) * view.width;
        const y = (view.mobile ? 0.85 : 0.83) * view.height;
        await touch(page, x, y, 90);
        await sleep(450);
        await shot(`packs-${view.id}.png`, 'pack browser opened by a real title-screen tap');
      }
    }
    if (want('play')) {
      await load(view, defaultCase, 'timed');
      await startGame(page);
      const startedAt = Date.now();
      for (const seconds of [3, 8, 15]) {
        await sleep(Math.max(0, startedAt + seconds * 1000 - Date.now()));
        await shot(`play-${view.id}-${seconds}s.png`, `${seconds} seconds after start`);
      }
    }
    if (want('after4correct')) await fourCorrect(view);
    if (want('upgrade')) await captureUpgrade(view);
    if (want('chest')) await captureRewards(view);
    if (want('wrong')) await captureWrong(view);
    if (want('geometry')) {
      const selected = geometryCase();
      await load(view, selected, 'geometry');
      await startGame(page);
      await sleep(900);
      await shot(`geometry-${view.id}.png`, 'choice geometry prompt selected from an existing pack item', { packId: selected.packId, itemId: selected.item.id, prompt: selected.item.prompt });
    }
    if (want('victory')) await captureVictory(view);
  }
} catch (error) {
  report.errors.push(String(error?.stack || error));
  if (page) await page.screenshot({ path: path.join(OUT, 'failure.png') }).catch(() => {});
} finally {
  if (browser) await browser.close().catch(() => {});
  if (server) await server.close().catch(() => {});
  report.packIntegrity.after = packHashes(index);
  report.packIntegrity.stable = JSON.stringify(report.packIntegrity.before) === JSON.stringify(report.packIntegrity.after);
}

const requiredForThisRun = only.size === 0 ? report.requiredFrames : report.requiredFrames.filter(file => {
  if (file.startsWith('title-')) return want('title');
  if (file.startsWith('packs-')) return want('packs');
  if (file.startsWith('play-')) return want('play');
  if (file.startsWith('after4correct-')) return want('after4correct');
  if (file.startsWith('upgrade-pour-')) return want('upgrade');
  if (file.startsWith('chest-')) return want('chest');
  if (file.startsWith('wrong-feedback-')) return want('wrong');
  if (file.startsWith('geometry-')) return want('geometry');
  if (file.startsWith('victory-')) return want('victory');
  return false;
});
const generatedThisRun = new Set(report.frames.map(frame => frame.file));
report.missingFrames = requiredForThisRun.filter(file => !generatedThisRun.has(file) || !fs.existsSync(path.join(OUT, file)));
report.pass = report.errors.length === 0 && report.consoleErrors.length === 0 && report.failedRequests.length === 0 &&
  report.packIntegrity.stable && report.missingFrames.length === 0;
fs.writeFileSync(path.join(OUT, 'report.json'), JSON.stringify(report, null, 2) + '\n');

if (only.size === 0 && report.pass) {
  const compared = spawnSync('python3', [path.join(HERE, 'make-compare.py')], { cwd: path.resolve(HERE, '../../../../../..'), encoding: 'utf8' });
  if (compared.status !== 0) {
    report.errors.push(`make-compare.py failed: ${compared.stderr || compared.stdout}`);
    report.pass = false;
    fs.writeFileSync(path.join(OUT, 'report.json'), JSON.stringify(report, null, 2) + '\n');
  }
}
console.log(JSON.stringify({ output: OUT, frames: report.frames.length, missingFrames: report.missingFrames, packStable: report.packIntegrity.stable, pass: report.pass, errors: report.errors }, null, 2));
if (!report.pass) process.exitCode = 1;
