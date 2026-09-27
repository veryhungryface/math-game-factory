// v3.2 final evidence capture (33 required frames).
// Gameplay beats use the public test bridge and real CDP touches.  Tower/fire
// galleries use the documented v32showcase fixtures and require state evidence
// that the fixture was actually entered.

import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import {
  MAPS,
  VIEWPORTS,
  V32_HERE as HERE,
  attachDiagnostics,
  centerOf,
  currentState,
  flatNormalizedRects,
  loadGame,
  openBrowserHarness,
  packHashes,
  readPackIndex,
  rectInFrame,
  sleep,
  startGame,
  stateContract,
  touch,
} from './harness-lib.mjs';

const OUT = path.join(HERE, 'final');
const index = readPackIndex();
const DEFAULT_PACK = index.default_pack;
fs.mkdirSync(OUT, { recursive: true });

const battleFrames = MAPS.flatMap(map => VIEWPORTS.map(viewport => `battle-${map.key}-${viewport.id}.png`));
const phaseFrames = VIEWPORTS.flatMap(viewport => [
  `boss-wave-${viewport.id}.png`,
  `region-recovered-${viewport.id}.png`,
  `permanent-upgrade-choice-${viewport.id}.png`,
]);
const upgradeFrames = Array.from({ length: 6 }, (_, index) => `upgrade-seq-${index + 1}.png`);
const galleryFrames = VIEWPORTS.map(viewport => `tower-gallery-${viewport.id}.png`);
const fireFrames = ['crossbow', 'cannon', 'magic'].flatMap(type =>
  [1, 2, 3].map(level => `fire-${type}-lv${level}.png`));
const resultFrames = VIEWPORTS.flatMap(viewport => [
  `game-over-result-${viewport.id}.png`,
  `best-record-${viewport.id}.png`,
]);
const REQUIRED = [...battleFrames, ...phaseFrames, ...upgradeFrames, ...galleryFrames, ...fireFrames, ...resultFrames];
if (REQUIRED.length !== 33 || new Set(REQUIRED).size !== 33) throw new Error(`capture manifest must contain 33 unique frames, got ${REQUIRED.length}`);

const report = {
  schemaVersion: 4,
  generatedAt: new Date().toISOString(),
  output: OUT,
  requiredFrames: REQUIRED,
  frames: [],
  packIntegrity: { before: packHashes(index), after: null, stable: false },
  chrome: null,
  errors: [],
  consoleErrors: [],
  consoleWarnings: [],
  failedRequests: [],
  missingFrames: [],
  invalidFrames: [],
  pass: false,
};

let browser;
let server;
let page;

function sha256(file) {
  return crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
}

function stateNumber(state, ...keys) {
  for (const key of keys) {
    const value = Number(state?.[key]);
    if (Number.isFinite(value)) return value;
  }
  return null;
}

async function waitState(predicate, timeoutMs = 20000) {
  const deadline = Date.now() + timeoutMs;
  let state = null;
  while (Date.now() < deadline) {
    state = await currentState(page);
    if (predicate(state)) return state;
    await sleep(25);
  }
  throw new Error(`state timeout after ${timeoutMs}ms: ${JSON.stringify(state)}`);
}

async function load(viewport, query, shouldStart = true) {
  await loadGame(page, server, viewport, `pack=${encodeURIComponent(DEFAULT_PACK)}&artprobe=1&${query}&v32capture=${Date.now()}`);
  if (shouldStart) await startGame(page);
  return currentState(page);
}

async function shot(file, note, validity = true, extra = null) {
  const state = await currentState(page).catch(() => null);
  const contract = stateContract(state);
  const target = path.join(OUT, file);
  await page.screenshot({ path: target });
  const entry = {
    file,
    note,
    valid: validity === true && contract.valid,
    extra,
    state,
    stateContract: contract,
    bytes: fs.statSync(target).size,
    sha256: sha256(target),
  };
  report.frames.push(entry);
  console.log(`frame ${file} valid=${entry.valid}`);
  return entry;
}

async function submit(method) {
  const before = await currentState(page);
  const attempts = Number(before?.attempts ?? 0);
  const accepted = await page.evaluate(name => window.__GAME_TEST__?.[name]?.(), method);
  if (!accepted) throw new Error(`__GAME_TEST__.${method} rejected`);
  return waitState(state => Number(state?.attempts ?? 0) > attempts || state?.phase === 'gameover', 15000);
}

async function captureBattles() {
  for (const map of MAPS) {
    for (const viewport of VIEWPORTS) {
      await load(viewport, `v32map=${map.id}`);
      await sleep(3000);
      const state = await currentState(page);
      const valid = stateContract(state).valid && Number(state?.map) === map.id && state?.phase === 'playing';
      await shot(`battle-${map.key}-${viewport.id}.png`, `${map.name} 실전 전장`, valid, { fixtureMap: map.id });
    }
  }
}

async function answerTen() {
  while (true) {
    const state = await currentState(page);
    if (Number(state?.attempts ?? 0) >= 10 || state?.phase === 'gameover') return state;
    if (state?.stagePhase !== 'question' || state?.feedbackVisible === true || state?.pending === true) {
      await sleep(30);
      continue;
    }
    await submit('answerCorrect');
    await sleep(60);
  }
}

async function captureStageEnd(viewport) {
  await load(viewport, `v32stagecapture=${viewport.id}`);
  await answerTen();
  const wanted = new Set(['boss', 'recovered', 'perk']);
  const deadline = Date.now() + 70000;
  while (Date.now() < deadline && wanted.size) {
    const state = await currentState(page);
    const phase = String(state?.stagePhase ?? '');
    if (wanted.has('boss') && (phase === 'boss' || state?.bossWave === true)) {
      await shot(`boss-wave-${viewport.id}.png`, '10번째 문제 후 보스 웨이브', state?.bossWave === true || phase === 'boss');
      wanted.delete('boss');
    }
    if (wanted.has('recovered') && (phase === 'recovered' || state?.regionRecovered === true)) {
      await shot(`region-recovered-${viewport.id}.png`, '지역 수복 연출', state?.regionRecovered === true || phase === 'recovered');
      wanted.delete('recovered');
    }
    if (wanted.has('perk') && (phase === 'perk' || state?.perkSelection === true)) {
      const rects = flatNormalizedRects(state?.perkRects, 3, viewport, 'perk');
      const valid = Array.isArray(state?.perkChoices) && state.perkChoices.length === 3 && rects.length === 3 && rects.every(rect => rectInFrame(rect, viewport));
      await shot(`permanent-upgrade-choice-${viewport.id}.png`, '스테이지 사이 영구 강화 3택1', valid, { perkRects: rects });
      wanted.delete('perk');
    }
    if (state?.phase === 'gameover') break;
    await sleep(20);
  }
  if (wanted.size) throw new Error(`${viewport.id} stage-end captures missing phases: ${[...wanted].join(', ')}`);
}

function upgradeTarget(state, viewport) {
  const rects = flatNormalizedRects(state?.upgradePadRects, 3, viewport, 'upgrade');
  const levels = Array.isArray(state?.towerLevels) ? state.towerLevels.map(Number) : [];
  const remaining = Array.isArray(state?.upgradeRemaining) ? state.upgradeRemaining.map(Number) : [];
  for (let index = 0; index < rects.length; index += 1) {
    if (levels[index] >= 0 && levels[index] < 2 && remaining[index] > 0 && rectInFrame(rects[index], viewport)) {
      return { index, rect: rects[index], level: levels[index], remaining: remaining[index] };
    }
  }
  return null;
}

async function captureUpgradeSequence() {
  const viewport = VIEWPORTS.find(value => value.id === '1280') ?? VIEWPORTS[0];
  await load(viewport, 'reward=coin&v32upgradecapture=1');
  let target = null;
  let fundingAnswers = 0;
  const fundingDeadline = Date.now() + 60000;
  while (fundingAnswers < 20 && Date.now() < fundingDeadline) {
    const state = await currentState(page);
    target = upgradeTarget(state, viewport);
    if (target && Number(state?.coins) >= target.remaining) break;
    if (state?.phase === 'gameover') throw new Error('game over while funding upgrade sequence');
    if (state?.stagePhase !== 'question' || state?.feedbackVisible === true || state?.pending === true) {
      await sleep(35);
      continue;
    }
    await submit('answerCorrect');
    fundingAnswers += 1;
    await sleep(60);
  }
  const funded = await currentState(page);
  target = upgradeTarget(funded, viewport);
  if (!target || Number(funded?.coins) < target.remaining) throw new Error(`could not fully fund an upgrade: ${JSON.stringify(funded)}`);
  const serialBefore = Number(funded?.upgradeSeqSerial ?? 0);
  // Hold the real sequence at authored beats while Chromium encodes each PNG.
  // Without this, screenshot latency itself skips the 1.12 s blueprint/punch beats.
  await page.evaluate(() => { window.__HYEOPGOK_CAPTURE_UPGRADE__ = 0; });
  const candidates = [[.5, .5], [.5, .38], [.5, .62], [.38, .5], [.62, .5], [.38, .38], [.62, .38], [.38, .62], [.62, .62]];
  let first = null;
  let point = null;
  for (const [fx, fy] of candidates) {
    await waitState(state => state?.stagePhase === 'question' && state?.feedbackVisible !== true && state?.pending !== true, 10000);
    const state = await currentState(page);
    target = upgradeTarget(state, viewport) ?? target;
    point = {
      x: target.rect.x + target.rect.width * fx,
      y: target.rect.y + target.rect.height * fy,
    };
    await touch(page, point.x, point.y, 120);
    first = await waitState(value => value?.upgradePhase === 'pouring' || value?.upgradePouring === true,
      4000).catch(() => null);
    if (first) break;
  }
  if (!first) throw new Error(`real upgrade-pad touches never entered pouring: ${JSON.stringify(await currentState(page))}`);
  await shot('upgrade-seq-1.png', '코인 붓기와 비용 카운트다운', true, { target, point, state: first });

  const beats = [
    { progress: 0, phase: 'hitstop', note: '히트스톱과 순간 플래시' },
    { progress: 0.20, phase: 'blueprint', note: '아래에서 차오르는 청사진' },
    { progress: 0.36, phase: 'overshoot', note: '완성 외형의 오버슈트' },
    { progress: 0.70, phase: 'punch', note: '먼지·빛기둥·펀치 줌·팡파르' },
    { progress: 0.84, phase: 'stats', note: '사거리 링과 스탯 팝업' },
  ];
  try {
    for (let index = 0; index < beats.length; index += 1) {
      const beat = beats[index];
      await page.evaluate(progress => { window.__HYEOPGOK_CAPTURE_UPGRADE__ = progress; }, beat.progress);
      const state = await waitState(value => {
        const phase = String(value?.upgradeSeqPhase ?? 'idle');
        const progress = Number(value?.upgradeSeqProgress);
        const serial = Number(value?.upgradeSeqSerial ?? 0);
        return serial > serialBefore && phase === beat.phase && Number.isFinite(progress) && Math.abs(progress - beat.progress) <= 0.025;
      }, 15000);
      const file = `upgrade-seq-${index + 2}.png`;
      const valid = Number(state?.upgradeSeqSerial) > serialBefore &&
        String(state?.upgradeSeqPhase) === beat.phase &&
        Math.abs(Number(state?.upgradeSeqProgress) - beat.progress) <= 0.025 &&
        Number(state?.upgradeTargetLevel) === target.level + 2;
      await shot(file, `${beat.note} (${index + 2}/6)`, valid, { beat, sequenceState: state });
    }
  } finally {
    await page.evaluate(() => { delete window.__HYEOPGOK_CAPTURE_UPGRADE__; }).catch(() => {});
  }
}

function showcaseEvidence(state, mode) {
  const declared = String(state?.showcaseMode ?? state?.showcase ?? '');
  if (declared === mode) return true;
  const match = /^fire-(crossbow|cannon|magic)-(\d)$/u.exec(mode);
  if (!match) return mode === 'towers' && String(state?.stagePhase) === 'showcase';
  const expectedType = { crossbow: 0, cannon: 1, magic: 2 }[match[1]];
  const expectedLevel = Number(match[2]) - 1;
  return String(state?.stagePhase) === 'showcase' &&
    Array.isArray(state?.towerLevels) && Number(state.towerLevels[0]) === expectedLevel &&
    Array.isArray(state?.towerTypes) && Number(state.towerTypes[0]) === expectedType;
}

async function captureShowcase(mode, file, viewport) {
  await load(viewport, `v32showcase=${encodeURIComponent(mode)}`, false);
  let state = await currentState(page);
  if (state?.phase === 'title' || state?.phase === 'loading') {
    await page.evaluate(() => window.__GAME_TEST__?.start?.());
  }
  state = await waitState(value => showcaseEvidence(value, mode), 10000).catch(() => currentState(page));
  // Fire fixtures are a live range. Capture the first real volley in flight;
  // faster cannon/magic bolts need an earlier shutter than crossbow bolts.
  const fireDelay = mode.startsWith('fire-crossbow') ? 125 : mode.startsWith('fire-cannon') ? 90 : 75;
  await sleep(mode.startsWith('fire-') ? fireDelay : 700);
  await shot(file, `production showcase fixture: ${mode}`, showcaseEvidence(state, mode), { mode, fixtureState: state });
}

async function captureShowcases() {
  for (const viewport of VIEWPORTS) {
    await captureShowcase('towers', `tower-gallery-${viewport.id}.png`, viewport);
  }
  const viewport = VIEWPORTS.find(value => value.id === '1280') ?? VIEWPORTS[0];
  for (const type of ['crossbow', 'cannon', 'magic']) {
    for (const level of [1, 2, 3]) {
      await captureShowcase(`fire-${type}-${level}`, `fire-${type}-lv${level}.png`, viewport);
    }
  }
}

async function captureGameOver(viewport) {
  await load(viewport, `v32gameovercapture=${viewport.id}`);
  let state = await currentState(page);
  let wrongSubmissions = 0;
  const deadline = Date.now() + 60000;
  while (wrongSubmissions < 20 && Date.now() < deadline && state?.phase !== 'gameover') {
    if (state?.stagePhase === 'question' && state?.feedbackVisible !== true && state?.pending !== true) {
      await submit('answerWrong');
      wrongSubmissions += 1;
    }
    else if (state?.perkSelection === true || state?.stagePhase === 'perk') {
      const rects = flatNormalizedRects(state?.perkRects, 3, viewport, 'perk');
      if (rects.length !== 3) throw new Error('game-over path reached an unmeasurable perk screen');
      const point = centerOf(rects[0]);
      await touch(page, point.x, point.y, 90);
    } else await sleep(40);
    state = await currentState(page);
  }
  state = await waitState(value => value?.phase === 'gameover', 30000);
  const attempts = Number(state?.attempts ?? 0);
  const bestQuestions = Number(state?.bestQuestions);
  const terminalValid = Number(state?.hp ?? state?.lives) === 0 && state?.endReason === 'gate_hp_zero';
  await sleep(200);
  await shot(`game-over-result-${viewport.id}.png`, '성문 HP 0 결과 화면', terminalValid,
    { attempts, stage: state?.stage, map: state?.map, kills: state?.kills, firstAttemptRate: state?.firstAttemptRate });
  await sleep(300);
  await shot(`best-record-${viewport.id}.png`, '결과 화면의 localStorage 최고 기록', terminalValid && Number.isFinite(bestQuestions) && bestQuestions >= attempts,
    { attempts, bestQuestions });
}

try {
  const opened = await openBrowserHarness();
  ({ browser, server } = opened);
  report.chrome = opened.chrome;
  page = await browser.newPage();
  page.setDefaultTimeout(90000);
  await page.setCacheEnabled(false);
  attachDiagnostics(page, report);

  await captureBattles();
  for (const viewport of VIEWPORTS) await captureStageEnd(viewport);
  await captureUpgradeSequence();
  await captureShowcases();
  for (const viewport of VIEWPORTS) await captureGameOver(viewport);
} catch (error) {
  report.errors.push(String(error?.stack || error));
  if (page) await page.screenshot({ path: path.join(OUT, 'failure.png') }).catch(() => {});
} finally {
  if (browser) await browser.close().catch(() => {});
  if (server) await server.close().catch(() => {});
  report.packIntegrity.after = packHashes(index);
  report.packIntegrity.stable = JSON.stringify(report.packIntegrity.before) === JSON.stringify(report.packIntegrity.after);
}

const generated = new Set(report.frames.map(frame => frame.file));
report.missingFrames = REQUIRED.filter(file => !generated.has(file) || !fs.existsSync(path.join(OUT, file)));
report.invalidFrames = report.frames.filter(frame => !frame.valid || !(frame.bytes > 0)).map(frame => ({ file: frame.file, valid: frame.valid, bytes: frame.bytes }));
report.pass = report.frames.length === REQUIRED.length && generated.size === REQUIRED.length && report.missingFrames.length === 0 &&
  report.invalidFrames.length === 0 && report.errors.length === 0 && report.consoleErrors.length === 0 &&
  report.failedRequests.length === 0 && report.packIntegrity.stable;
fs.writeFileSync(path.join(OUT, 'report.json'), `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify({
  output: OUT,
  required: REQUIRED.length,
  generated: report.frames.length,
  missingFrames: report.missingFrames,
  invalidFrames: report.invalidFrames,
  packStable: report.packIntegrity.stable,
  pass: report.pass,
  errors: report.errors,
}, null, 2));
if (!report.pass) process.exitCode = 1;
