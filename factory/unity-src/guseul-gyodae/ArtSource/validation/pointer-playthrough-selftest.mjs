import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/guseul-gyodae');
const FRAMES = path.join(HERE, 'pointer-playthrough');
fs.mkdirSync(FRAMES, { recursive: true });

const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'
    ]) {
      const candidate = path.join(base, build, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
  throw new Error('Chrome for Testing not found');
}
function artifactHash() {
  const hash = crypto.createHash('sha256');
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(x => `Build/${x}`)]) {
    hash.update(fs.readFileSync(path.join(GAME, name)));
  }
  return hash.digest('hex');
}
async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, timeout = 5000) {
  const deadline = Date.now() + timeout;
  let current;
  while (Date.now() < deadline) {
    current = await state(page);
    if (predicate(current)) return current;
    await sleep(80);
  }
  throw new Error(`state timeout: ${JSON.stringify(current)}`);
}
async function drag(page, from, to) {
  await page.mouse.move(from[0], from[1]);
  await page.mouse.down();
  await page.mouse.move((from[0] + to[0]) / 2, (from[1] + to[1]) / 2, { steps: 5 });
  await page.mouse.move(to[0], to[1], { steps: 5 });
  await sleep(45);
  await page.mouse.up();
  await sleep(70);
}
async function tap(page, at) {
  await page.mouse.move(at[0], at[1]);
  await page.mouse.down();
  await sleep(80);
  await page.mouse.up();
  await sleep(100);
}
function point(array, offset) { return [array[offset], array[offset + 1]]; }
function correctK(s) {
  if (s.currentBand === 1) return Math.round(6 * s.targetN / s.targetD);
  if (s.currentBand === 2) return 6 - Math.round(6 * s.targetN / s.targetD);
  return Math.round(24 * s.targetN / (s.targetD * s.fixedABlue));
}
async function setCount(page, wanted) {
  let s = await state(page);
  for (let guard = 0; guard < 8 && s.currentBlue !== wanted; guard++) {
    const makeBlue = s.currentBlue < wanted;
    const slot = s.slots.findIndex(value => value !== (makeBlue ? 1 : 0));
    if (slot < 0) throw new Error(`no slot for K=${wanted}: ${JSON.stringify(s)}`);
    const supply = point(s.supplyPx, makeBlue ? 0 : 2);
    const target = point(s.slotPx, slot * 2);
    const before = s.currentBlue;
    for (const offset of [[0, 0], [10, 0], [0, -8], [10, -8]]) {
      await drag(page, [supply[0] + offset[0], supply[1] + offset[1]], target);
      s = await state(page);
      if (s.currentBlue !== before) break;
    }
  }
  if (s.currentBlue !== wanted) throw new Error(`count drag failed K=${wanted}: ${JSON.stringify(s)}`);
  return s;
}
async function dispatch(page) {
  const s = await state(page);
  const from = point(s.dispatchPx, 0);
  const target = point(s.dispatchPx, 2);
  const to = [Math.min(382, Math.max(target[0], from[0] + 82)), target[1]];
  await drag(page, from, to);
  return state(page);
}
async function submitCount(page, wanted) {
  await setCount(page, wanted);
  return dispatch(page);
}
async function waitNextOrder(page, previousProblem) {
  return waitFor(page, s => s.phase === 'clear' || s.phase === 'gameover' ||
    (s.currentProblem !== previousProblem && !s.submissionLatched), 4000);
}

const events = [];
const errors = [];
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/guseul-gyodae/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 30000 });
  await page.screenshot({ path: path.join(FRAMES, '00-title.png') });

  // 타이틀 실제 탭 → 고정 연습. 빈 탭은 자동 정답을 만들지 않는다.
  await page.mouse.click(195, 550);
  let s = await waitFor(page, x => x.onboarding === true);
  const practiceBefore = s;
  await page.mouse.click(360, 760);
  await sleep(250);
  s = await state(page);
  if (!s.onboarding || s.currentBlue !== 2 || s.solved !== 0) throw new Error(`blank tap solved practice: ${JSON.stringify(s)}`);
  await page.screenshot({ path: path.join(FRAMES, '01-practice-guide.png') });

  // 접근성 대체 입력도 실제 대상 두 개를 같은 순서로 누른다. 빈 탭/임의 드래그는 성공하지 않는다.
  const whiteSlot = s.slots.findIndex(x => x === 0);
  const blueSupply = point(s.supplyPx, 0);
  await tap(page, blueSupply);
  s = await state(page);
  if (s.lastAction !== 'blue-picked' || s.currentBlue !== 2) throw new Error(`supply tap did not arm marble: ${JSON.stringify(s)}`);
  await tap(page, point(s.slotPx, whiteSlot * 2));
  s = await state(page);
  if (s.currentBlue !== 3 || !s.onboarding) throw new Error(`practice supply-to-slot tap path failed: ${JSON.stringify(s)}`);
  await page.screenshot({ path: path.join(FRAMES, '02-practice-swapped.png') });
  const handle = point(s.dispatchPx, 0);
  await tap(page, handle);
  s = await state(page);
  if (s.lastAction !== 'spin-handle-picked' || !s.onboarding) throw new Error(`handle tap did not arm spin: ${JSON.stringify(s)}`);
  const orbit = point(s.dispatchPx, 2);
  await tap(page, orbit);
  s = await waitFor(page, x => !x.onboarding && x.currentProblem.startsWith('run-') && !x.submissionLatched, 5000);
  events.push({ name: 'practice-completed-with-two-visible-target-pairs', before: practiceBefore, after: s });
  await page.screenshot({ path: path.join(FRAMES, '03-first-order.png') });

  // 첫 판: 첫 주문 정답, 둘째 주문 오답→같은 구성 수리, 이후 실제 pointer로 완주.
  let previous = s.currentProblem;
  let made = await submitCount(page, correctK(s));
  if (made.solved !== 1 || made.firstCorrect !== 1) throw new Error(`first correct failed: ${JSON.stringify(made)}`);
  s = await waitNextOrder(page, previous);

  previous = s.currentProblem;
  const right = correctK(s);
  made = await submitCount(page, right === 6 ? 5 : right + 1);
  if (made.lives !== 2 || made.firstAttempts !== 2 || made.firstCorrect !== 1) throw new Error(`wrong first attempt failed: ${JSON.stringify(made)}`);
  await page.screenshot({ path: path.join(FRAMES, '04-wrong-repair-rail.png') });
  await waitFor(page, x => !x.submissionLatched && x.lastAction === 'repair', 4000);
  made = await submitCount(page, right);
  if (made.solved !== 2 || made.firstCorrect !== 1 || made.lives !== 2) throw new Error(`repair failed: ${JSON.stringify(made)}`);
  events.push({ name: 'wrong-then-repair', wrongK: right === 6 ? 5 : right + 1, correctK: right, state: made });
  await page.screenshot({ path: path.join(FRAMES, '05-repair-correct.png') });
  s = await waitNextOrder(page, previous);

  while (s.phase !== 'clear' && s.phase !== 'gameover') {
    previous = s.currentProblem;
    made = await submitCount(page, correctK(s));
    s = await waitNextOrder(page, previous);
  }
  if (s.phase !== 'clear' || s.solved !== 10 || s.lives !== 2 || s.firstCorrect !== 9 || s.band3FirstCorrect !== 4) {
    throw new Error(`recovery clear failed: ${JSON.stringify(s)}`);
  }
  events.push({ name: 'clear-after-one-repair', state: s });
  await page.screenshot({ path: path.join(FRAMES, '06-clear-after-recovery.png') });

  // 둘째 판: 마지막 단계 두 주문의 첫 시도를 틀리고 수리해 10개를 출고하되 숙련 미달.
  await page.evaluate(() => window.__GAME_TEST__.start());
  s = await waitFor(page, x => x.lastAction.startsWith('order-') && !x.submissionLatched);
  while (s.phase !== 'clear' && s.phase !== 'gameover') {
    previous = s.currentProblem;
    const order = s.firstAttempts + 1;
    const k = correctK(s);
    if (order === 7 || order === 8) {
      made = await submitCount(page, k === 6 ? 5 : k + 1);
      if (made.lives !== (order === 7 ? 2 : 1)) throw new Error(`mastery miss did not cost life: ${JSON.stringify(made)}`);
      await waitFor(page, x => !x.submissionLatched && x.lastAction === 'repair', 4000);
      await submitCount(page, k);
    } else {
      await submitCount(page, k);
    }
    s = await waitNextOrder(page, previous);
  }
  if (s.lastAction !== 'end-mastery' || s.solved !== 10 || s.firstCorrect !== 8 || s.band3FirstCorrect !== 2) {
    throw new Error(`mastery gate failed: ${JSON.stringify(s)}`);
  }
  events.push({ name: 'ten-deliveries-but-mastery-fail', state: s });
  await page.screenshot({ path: path.join(FRAMES, '07-mastery-fail.png') });

  // 셋째 판: 같은 오답 구성을 세 번 실제 출고하면 볼트 0에서 입력이 잠긴다.
  await page.evaluate(() => window.__GAME_TEST__.start());
  s = await waitFor(page, x => x.lastAction.startsWith('order-') && !x.submissionLatched);
  const bad = correctK(s) === 6 ? 5 : correctK(s) + 1;
  for (let i = 0; i < 3; i++) {
    await submitCount(page, bad);
    s = await state(page);
    if (i < 2) s = await waitFor(page, x => !x.submissionLatched && x.lastAction === 'repair', 4000);
  }
  s = await waitFor(page, x => x.phase === 'gameover' && x.lives === 0, 4000);
  const beforeLockedTap = s.pointerVersion;
  await page.mouse.click(195, 650);
  await sleep(200);
  const afterLockedTap = await state(page);
  if (afterLockedTap.solved !== s.solved || afterLockedTap.lives !== 0 || afterLockedTap.lastAction !== 'end-locked') throw new Error(`gameover not locked: ${JSON.stringify(afterLockedTap)}`);
  await page.screenshot({ path: path.join(FRAMES, '08-gameover.png') });
  await page.mouse.click(afterLockedTap.restartPx[0], afterLockedTap.restartPx[1]);
  const restarted = await waitFor(page, x => x.onboarding === true && x.lastAction === 'practice-start');
  events.push({ name: 'three-wrongs-gameover-lock', beforeLockedTap, afterLockedTap });
  events.push({ name: 'gameover-restart-one-tap', state: restarted });
  await page.screenshot({ path: path.join(FRAMES, '09-restarted-practice.png') });

  const result = {
    generated_at: new Date().toISOString(), artifact_hash_sha256: artifactHash(), viewport: '390x844',
    method: '연습은 보이는 공급→슬롯·손잡이→궤도 대상쌍 탭, 실전은 Chrome mouse down/move/up 드래그; hooks used only to start second and third runs',
    console_errors: errors, events, passed: errors.length === 0
  };
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
