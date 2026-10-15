#!/usr/bin/env node
// 폴드라인 수정 검증: start()는 판 진입에만 쓰고, 여섯 답은 모두 실제 mouse down/move/up으로 제출한다.
// 정상판, 첫 일반 실수 뒤 회복판, 독립 두 번째 판, 첫 시도 5/6 조기 실패를 한 WebGL 산출물에서 재현한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/foldline-lab');
const OUT = path.join(HERE, 'pointer-playthrough');
const W = 390, H = 844;
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome',
  ]) { const candidate = path.join(base, build, rel); if (fs.existsSync(candidate)) return candidate; }
  throw new Error('Chrome for Testing not found');
}

function artifactHash() {
  const hash = crypto.createHash('sha256');
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(file => `Build/${file}`)]) {
    hash.update(name); hash.update(fs.readFileSync(path.join(GAME, name)));
  }
  return hash.digest('hex');
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, label, timeoutMs = 8000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    const current = await state(page);
    if (predicate(current)) return current;
    await sleep(70);
  }
  throw new Error(`timeout ${label}: ${JSON.stringify(await state(page))}`);
}

async function waitForMaybe(page, predicate, timeoutMs = 1200) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    const current = await state(page);
    if (predicate(current)) return current;
    await sleep(70);
  }
  return null;
}

function targets(st) {
  const m = /^line:([\d.]+),([\d.]+)>([\d.]+),([\d.]+);flag:([\d.]+),([\d.]+)$/.exec(st.inputTargets || '');
  if (!m) throw new Error(`bad inputTargets: ${st.inputTargets}`);
  const p = i => [Number(m[i]) * W, Number(m[i + 1]) * H];
  return { lineA: p(1), lineB: p(3), flag: p(5) };
}

function diagnosticTargets(st) {
  const m = /^line:([\d.]+),([\d.]+)>([\d.]+),([\d.]+)$/.exec(st.diagnosticTarget || '');
  if (!m) throw new Error(`bad diagnosticTarget: ${st.diagnosticTarget}`);
  return { from: [Number(m[1]) * W, Number(m[2]) * H], to: [Number(m[3]) * W, Number(m[4]) * H] };
}

async function drag(page, from, to) {
  // The WebGL old Input Manager can keep the last pressed position while ignoring
  // hover-only movement at low FPS. Every prior submission parks that sampled
  // position outside the frame. This short real gesture then moves it onto the
  // next handle without creating a mathematical attempt.
  await sleep(250);
  await page.mouse.move(from[0], from[1]);
  const primeVersion = (await state(page)).pointerVersion;
  await page.mouse.down();
  for (let i = 0; i < 8 && (await state(page)).pointerVersion === primeVersion; i++) {
    await page.mouse.move(from[0] + (i % 2), from[1] + (i % 2));
    await sleep(90);
  }
  if ((await state(page)).pointerVersion === primeVersion) {
    await page.mouse.up();
    throw new Error(`Unity did not consume priming down at ${from.join(',')}`);
  }
  await page.mouse.up(); await sleep(320);

  const pointerVersion = (await state(page)).pointerVersion;
  await page.mouse.down();
  for (let i = 0; i < 8 && (await state(page)).pointerVersion === pointerVersion; i++) {
    await page.mouse.move(from[0] + (i % 2), from[1] + (i % 2));
    await sleep(90);
  }
  if ((await state(page)).pointerVersion === pointerVersion) {
    await page.mouse.up();
    throw new Error(`Unity did not consume submission down at ${from.join(',')}`);
  }
  await page.mouse.move((from[0] + to[0]) / 2, (from[1] + to[1]) / 2, { steps: 7 });
  await page.mouse.move(to[0], to[1], { steps: 7 }); await sleep(90);
  await page.mouse.up();

  // Park outside during the feedback lock so the next priming Down cannot start
  // a line from this submission's endpoint.
  const locked = await waitForMaybe(page, s => s.inputLocked, 900);
  if (locked) {
    const parkVersion = locked.pointerVersion;
    await page.mouse.down();
    for (let i = 0; i < 6 && (await state(page)).pointerVersion === parkVersion; i++) {
      await page.mouse.move(to[0] + (i % 2), to[1] + (i % 2));
      await sleep(70);
    }
    await page.mouse.move(6, H - 6, { steps: 5 }); await sleep(100);
    await page.mouse.up(); await sleep(120);
  }
}

function wrongLine(a, b) {
  const mx = (a[0] + b[0]) / 2, my = (a[1] + b[1]) / 2;
  const dx = b[0] - a[0], dy = b[1] - a[1], mag = Math.max(80, Math.hypot(dx, dy));
  const px = -dy / Math.max(1, Math.hypot(dx, dy)) * mag * .48;
  const py = dx / Math.max(1, Math.hypot(dx, dy)) * mag * .48;
  const clamp = (x, lo, hi) => Math.max(lo, Math.min(hi, x));
  return [[clamp(mx - px, 40, W - 40), clamp(my - py, 210, H - 150)], [clamp(mx + px, 40, W - 40), clamp(my + py, 210, H - 150)]];
}

async function submitCorrect(page, log) {
  const before = await state(page), t = targets(before);
  const completed = s => s.phase === 'clear' || s.phase === 'gameover' || (s.phase === 'playing' && s.sail !== before.sail);
  const from = before.trap ? t.flag : t.lineA;
  await drag(page, from, t.lineB);
  // Sail 2 is the physical-flag task. Park the real cursor on that prop while
  // sail 1's feedback is still resolving, so Unity's low-FPS input sampler has
  // the correct hover position before the next Down edge.
  if (before.sail === 1) {
    await page.mouse.move(t.flag[0], t.flag[1], { steps: 7 });
    await sleep(300);
  }
  let after = await waitForMaybe(page, completed);
  let retriedReverse = false;
  // Unity can miss the first Down immediately after feedback animation. Retrying the same
  // mathematical segment from its other endpoint is still a real pointer submission.
  if (!after && !before.trap) {
    retriedReverse = true;
    await drag(page, t.lineB, t.lineA);
    after = await waitForMaybe(page, completed, 2200);
  }
  if (!after) after = await waitFor(page, completed, `correct sail ${before.sail}`);
  log.push({ action: before.trap ? 'flag-drag' : 'crease-drag', problemId: before.problemId, sail: before.sail, from, to: t.lineB, retriedReverse, after });
  return after;
}

async function submitWrong(page, log) {
  const before = await state(page), t = targets(before);
  const [from, to] = before.trap ? [t.lineA, t.lineB] : wrongLine(t.lineA, t.lineB);
  await drag(page, from, to);
  const after = await waitFor(page, s => s.phase === 'gameover' || (s.phase === 'playing' && s.sail === before.sail && s.lives === before.lives - 1), `wrong sail ${before.sail}`);
  log.push({ action: 'deliberate-wrong-crease', problemId: before.problemId, sail: before.sail, from, to, after });
  return after;
}

async function begin(page) {
  await page.evaluate(() => window.__GAME_TEST__.start());
  return waitFor(page, s => s.phase === 'playing' && s.sail === 1, 'new run');
}

async function clearRun(page, label, frames) {
  const actions = []; await begin(page);
  await page.screenshot({ path: path.join(OUT, `${label}-start.png`) });
  while ((await state(page)).phase === 'playing') await submitCorrect(page, actions);
  const final = await state(page);
  await sleep(1300);
  await page.screenshot({ path: path.join(OUT, `${label}-clear.png`) });
  frames.push(`${label}-start.png`, `${label}-clear.png`);
  return { actions, final };
}

async function recoveryRun(page, frames) {
  const actions = []; await begin(page);
  const afterMistake = await submitWrong(page, actions);
  await page.screenshot({ path: path.join(OUT, 'recovery-first-mistake.png') });
  while ((await state(page)).phase === 'playing') await submitCorrect(page, actions);
  const final = await state(page);
  await sleep(1300);
  await page.screenshot({ path: path.join(OUT, 'recovery-clear.png') });
  frames.push('recovery-first-mistake.png', 'recovery-clear.png');
  return { afterMistake, actions, final };
}

async function earlyMasteryFail(page, frames) {
  const actions = []; await begin(page);
  await submitWrong(page, actions);
  await submitCorrect(page, actions);
  await submitWrong(page, actions);
  const afterSecondMiss = await waitFor(page, s => s.phase === 'gameover', 'early mastery gameover');
  await page.screenshot({ path: path.join(OUT, 'mastery-impossible-gameover.png') });
  frames.push('mastery-impossible-gameover.png');
  return { actions, final: afterSecondMiss };
}


async function swappedCenterFeedback(page, frames) {
  const actions = []; await begin(page);
  await submitCorrect(page, actions);
  await submitCorrect(page, actions);
  const before = await state(page);
  const t = diagnosticTargets(before);
  await drag(page, t.from, t.to);
  const after = await waitFor(page, s => s.inputLocked && s.lastResult === 'wrong', 'swapped center feedback');
  await page.screenshot({ path: path.join(OUT, 'swapped-center-feedback.png') });
  frames.push('swapped-center-feedback.png');
  return { before, gesture: t, after };
}

fs.mkdirSync(OUT, { recursive: true });
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), protocolTimeout: 900000, args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const consoleErrors = [], frames = [];
try {
  const page = await browser.newPage();
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
  page.on('pageerror', error => consoleErrors.push(String(error)));
  const hashBefore = artifactHash();
  await page.goto(`${server.url}/g/foldline-lab/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  const browserGeometry = await page.evaluate(() => {
    const canvas = document.querySelector('canvas');
    const r = canvas?.getBoundingClientRect();
    return { innerWidth, innerHeight, dpr: devicePixelRatio, canvasWidth: canvas?.width, canvasHeight: canvas?.height,
      rect: r && { x: r.x, y: r.y, width: r.width, height: r.height } };
  });
  console.log('browserGeometry', JSON.stringify(browserGeometry));
  const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(1000));
  const normal = await clearRun(page, 'normal', frames);
  const recovery = await recoveryRun(page, frames);
  const second = await clearRun(page, 'second', frames);
  const earlyFail = await earlyMasteryFail(page, frames);
  const swappedFeedback = await swappedCenterFeedback(page, frames);
  const playedIds = [...normal.actions, ...recovery.actions, ...second.actions, ...earlyFail.actions].map(x => x.problemId);
  const bankIds = new Set(bank.map(x => x.id));
  const typeCounts = {
    symmetry: bank.filter(x => /두 쪽이 겹치게/.test(x.prompt)).length,
    impossible: bank.filter(x => /안전 깃/.test(x.prompt)).length,
    vertexPair: bank.filter(x => /꼭짓점 A가 [BC]에/.test(x.prompt)).length,
    sidePair: bank.filter(x => /변 (AB가 AC|BA가 BC)에/.test(x.prompt)).length,
  };
  const impossibleMath = bank.filter(x => /안전 깃/.test(x.prompt)).every(x =>
    /AB=4 cm/.test(x.prompt) && /BC=5 cm/.test(x.prompt) && /CA=3 cm/.test(x.prompt) && /모두 다르/.test(x.prompt) && x.answer === '선대칭 접기 불가능');
  const hashAfter = artifactHash();
  const output = {
    runId: process.env.MGF_RUN_ID || `foldline-lab-fix1-${new Date().toISOString()}`,
    generatedAt: new Date().toISOString(), artifactHashSha256: hashAfter, viewport: `${W}x${H}`, browserGeometry,
    input: 'trusted Puppeteer mouse down/move/up for every mathematical submission; start() only enters a run; answerCorrect()/answerWrong() are never called',
    frames, bankCount: bank.length, typeCounts, impossibleMath,
    playedProblemIds: playedIds, allPlayedProblemIdsInBank: playedIds.every(id => bankIds.has(id)),
    normal, oneMistakeRecovery: recovery, independentSecondRun: second, earlyMasteryFail: earlyFail, swappedFeedback,
    artifactStable: hashBefore === hashAfter, consoleErrors,
  };
  output.passed = output.artifactStable && consoleErrors.length === 0 && bank.length === 420 &&
    Object.values(typeCounts).reduce((a, b) => a + b, 0) === 420 && impossibleMath && output.allPlayedProblemIdsInBank &&
    normal.final.phase === 'clear' && normal.final.firstCorrect === 6 &&
    recovery.afterMistake.lives === 2 && recovery.final.phase === 'clear' && recovery.final.firstCorrect === 5 &&
    second.final.phase === 'clear' && second.final.firstCorrect === 6 &&
    earlyFail.final.phase === 'gameover' && /더 이상 달성할 수 없다/.test(earlyFail.final.endReason || '') &&
    swappedFeedback.after.misconceptionId === 'circumcenter_incenter_swapped' &&
    swappedFeedback.after.feedbackText === '외심은 세 변의 수직이등분선의 교점이고, 내심은 세 내각의 이등분선의 교점이다';
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
  if (!output.passed) process.exitCode = 1;
  await page.close();
} finally {
  await browser.close(); await server.close();
}
