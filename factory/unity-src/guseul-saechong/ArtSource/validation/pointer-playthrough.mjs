import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/guseul-saechong');
const FRAMES = path.join(HERE, 'pointer-playthrough');
fs.mkdirSync(FRAMES, { recursive: true });
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
  throw new Error('Chrome for Testing not found');
}

function artifactHash() {
  const h = crypto.createHash('sha256');
  for (const file of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(x => `Build/${x}`)])
    h.update(fs.readFileSync(path.join(GAME, file)));
  return h.digest('hex');
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, timeout = 6000) {
  const until = Date.now() + timeout;
  let s;
  while (Date.now() < until) {
    s = await state(page);
    if (predicate(s)) return s;
    await sleep(30);
  }
  throw new Error(`state timeout: ${JSON.stringify(s)}`);
}

function frac(text) {
  const [n, d = '1'] = String(text).split('/');
  return [Number(n), Number(d)];
}
function made(s, purple) {
  const colorCount = s.targetColor === '보라' ? purple : s.N - purple;
  let n = colorCount, d = s.N;
  if (s.eventType === 'not') n = s.N - colorCount;
  if (s.eventType === 'both') { n = colorCount * colorCount; d = s.N * s.N; }
  if (s.eventType === 'atLeastOne') { n = s.N * s.N - (s.N - colorCount) ** 2; d = s.N * s.N; }
  return [n, d];
}
function correctPurple(s) {
  const [tn, td] = frac(s.targetFraction);
  for (let p = 0; p <= s.N; p++) {
    const [n, d] = made(s, p);
    if (n * td === tn * d) return p;
  }
  throw new Error(`no solution: ${JSON.stringify(s)}`);
}

async function tap(page, point) {
  await page.mouse.move(point[0], point[1]);
  await page.mouse.down(); await sleep(18); await page.mouse.up(); await sleep(25);
}
async function drag(page, from, to) {
  await page.mouse.move(from[0], from[1]);
  await page.mouse.down();
  await page.mouse.move((from[0] + to[0]) / 2, (from[1] + to[1]) / 2, { steps: 4 });
  await page.mouse.move(to[0], to[1], { steps: 5 });
  await sleep(25); await page.mouse.up(); await sleep(35);
}
function marblePoint(s, i) { return [s.marblePx[i * 2], s.marblePx[i * 2 + 1]]; }
async function setCount(page, wanted) {
  let s = await state(page);
  for (let guard = 0; guard < 20; guard++) {
    const current = s.colors.slice(0, s.N).reduce((a, b) => a + b, 0);
    if (current === wanted) return s;
    const makePurple = current < wanted;
    const i = s.colors.slice(0, s.N).findIndex(v => v !== (makePurple ? 1 : 0));
    if (i < 0) throw new Error(`no marble to change: ${JSON.stringify(s)}`);
    await tap(page, marblePoint(s, i));
    s = await state(page);
  }
  throw new Error(`setCount guard: ${JSON.stringify(s)}`);
}
async function shoot(page) {
  const s = await state(page);
  await drag(page, [s.pouchPx[0], s.pouchPx[1]], [s.pouchPx[2], s.pouchPx[3]]);
  return state(page);
}
async function submit(page, count) { await setCount(page, count); return shoot(page); }
async function waitPlayable(page, oldId = '') {
  return waitFor(page, s => (s.phase === 'playing' && !s.submissionLatched && (!oldId || s.currentProblem !== oldId)) || s.phase === 'clear' || s.phase === 'gameover', 7000);
}

const consoleErrors = [];
const events = [];
const startHash = artifactHash();
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  page.on('console', m => { if (m.type() === 'error') consoleErrors.push(m.text()); });
  page.on('pageerror', e => consoleErrors.push(String(e)));
  await page.goto(`${server.url}/g/guseul-saechong/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 30000 });
  await page.screenshot({ path: path.join(FRAMES, '00-title.png') });

  await tap(page, [195, 700]);
  let s = await waitFor(page, x => x.phase === 'practice' && x.onboarding);
  if (s.N !== 6 || s.colors.slice(0, 6).some(Boolean)) throw new Error(`practice not fixed: ${JSON.stringify(s)}`);
  await page.screenshot({ path: path.join(FRAMES, '01-practice.png') });
  await setCount(page, 2);
  await page.screenshot({ path: path.join(FRAMES, '02-practice-painted.png') });
  await shoot(page);
  s = await waitFor(page, x => x.phase === 'playing' && !x.onboarding, 8000);
  events.push({ name: 'real-pointer-practice', state: s });
  await page.screenshot({ path: path.join(FRAMES, '03-first-problem.png') });

  // 첫 실전만 오답 뒤 같은 판을 직접 고쳐도 최종 목표에 도달해야 한다.
  let old = s.currentProblem;
  const correct = correctPurple(s);
  const wrong = correct === s.N ? correct - 1 : correct + 1;
  await submit(page, wrong);
  let wrongState = await waitFor(page, x => x.phase === 'reveal' && x.lives === 2);
  await page.screenshot({ path: path.join(FRAMES, '04-wrong-evidence.png') });
  s = await waitFor(page, x => x.phase === 'playing' && x.currentProblem === old && !x.submissionLatched, 5000);
  await submit(page, correct);
  s = await waitPlayable(page, old);
  events.push({ name: 'one-mistake-recovery', wrongState, recoveredState: s });
  await page.screenshot({ path: path.join(FRAMES, '05-recovered.png') });

  while (s.phase === 'playing') {
    old = s.currentProblem;
    await submit(page, correctPurple(s));
    s = await waitPlayable(page, old);
  }
  if (s.phase !== 'clear' || s.solved !== 6 || s.firstAttemptCorrect !== 5 || s.lives !== 2)
    throw new Error(`recovery clear failed: ${JSON.stringify(s)}`);
  events.push({ name: 'clear-after-one-mistake', state: s });
  await page.screenshot({ path: path.join(FRAMES, '06-clear.png') });

  // 새 판에서 서로 다른 오답 배합 세 번: 안전잎 0 즉시 종료와 입력 잠금.
  await page.evaluate(() => window.__GAME_TEST__.start());
  s = await waitFor(page, x => x.phase === 'playing');
  const solution = correctPurple(s);
  const wrongs = [];
  for (let p = 0; p <= s.N && wrongs.length < 3; p++) if (p !== solution) wrongs.push(p);
  for (let i = 0; i < 3; i++) {
    await submit(page, wrongs[i]);
    if (i < 2) s = await waitFor(page, x => x.phase === 'playing' && x.lives === 2 - i && !x.submissionLatched, 5000);
  }
  s = await waitFor(page, x => x.phase === 'gameover' && x.lives === 0, 5000);
  const locked = JSON.stringify(s);
  await tap(page, [40, 420]); // 결과 CTA 밖 탭도 재시작은 되지만 답 진도는 생기지 않는다.
  const after = await state(page);
  if (after.solved !== 0 || after.lives !== 3 || after.phase !== 'practice') throw new Error(`one-tap restart failed: ${JSON.stringify(after)}`);
  events.push({ name: 'three-wrongs-lock-and-restart', locked: JSON.parse(locked), after });
  await page.screenshot({ path: path.join(FRAMES, '07-restarted-practice.png') });

  const endHash = artifactHash();
  if (startHash !== endHash) throw new Error('artifact changed during pointer validation');
  if (consoleErrors.length) throw new Error(`console errors: ${consoleErrors.join(' | ')}`);
  const result = { run_id: `guseul-saechong-pointer-${new Date().toISOString()}`, artifact_hash: startHash, consoleErrors, events, passed: true };
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
} finally {
  await browser.close();
  await server.close();
}
