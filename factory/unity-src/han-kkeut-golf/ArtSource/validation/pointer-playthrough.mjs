#!/usr/bin/env node
// 실제 pointer 플레이스루. 답 입력은 전부 진짜 마우스 드래그다(__GAME_TEST__ 의 start/answerCorrect/answerWrong 미사용).
// 정답 각은 getState().theta(화면에 표시된 OA 의 각과 같은 값)로 θ+90° 를 계산한다 — 화면 OCR 이 아니므로
// 「독립 사람 플레이」 증거가 아니라 입력 경로·상태 전이·실패/회복 경로 검증이다.
// 사용: node pointer-playthrough.mjs [W] [H] [tag]
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const W = +(process.argv[2] || 390), H = +(process.argv[3] || 844), TAG = process.argv[4] || `${W}x${H}`;
const OUT = path.join(HERE, 'pointer-playthrough', TAG);
fs.mkdirSync(OUT, { recursive: true });
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
function chromePath() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const c = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(c)) return c;
  }
}
const srv = await serveStatic(path.join(ROOT, 'public'));
const url = `${srv.url}/g/han-kkeut-golf/index.html`;
const browser = await puppeteer.launch({ executablePath: chromePath(), headless: 'new', protocolTimeout: 180000, args: ['--use-angle=metal', '--enable-gpu'] });
const errors = []; const log = []; const results = {};
const page = await browser.newPage();
page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
page.on('pageerror', (e) => errors.push('pageerror: ' + e.message));
await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
await page.goto(url, { waitUntil: 'load' });
await page.waitForFunction(() => window.__GAME_TEST__ && window.__GAME_TEST__.ready, { timeout: 30000 });
await sleep(900);
const state = () => page.evaluate(() => window.__GAME_TEST__.getState());
async function waitFor(pred, ms = 9000) { const t0 = Date.now(); while (Date.now() - t0 < ms) { const s = await state(); if (pred(s)) return s; await sleep(60); } return state(); }
let shot = 0;
const snap = async (name) => { await page.screenshot({ path: path.join(OUT, `${String(shot++).padStart(2, '0')}-${name}.png`) }); };
// 레일 몸통(A 에서 0.6L 지점)을 잡아 원호로 돌린다 — 손잡이 원호가 화면 밖으로 나가 pointerup 을 잃지 않게.
const handleAt = (s, a, k = 0.6) => [s.aPx + k * s.railPx * Math.cos(a * Math.PI / 180), s.aPy - k * s.railPx * Math.sin(a * Math.PI / 180)];
async function rotateTo(target) {
  const s = await state();
  let from = s.railAngle; let d = ((target - from) % 180 + 180) % 180; if (d > 90) d -= 180;
  // 화면 안에 있는 쪽 손잡이를 잡는다
  let [x0, y0] = handleAt(s, from); let flip = 0;
  if (x0 < 8 || x0 > W - 8 || y0 < 140 || y0 > H - 8) { flip = 180; [x0, y0] = handleAt(s, from + 180); }
  await page.mouse.move(x0, y0); await page.mouse.down();
  const n = Math.max(8, Math.abs(d));
  for (let i = 1; i <= n; i++) { const [x, y] = handleAt(s, from + flip + d * i / n); await page.mouse.move(x, y); await sleep(8); }
  const [xe, ye] = handleAt(s, target + flip); await page.mouse.move(xe, ye); await sleep(30);
  const before = await state();
  await page.mouse.up();
  return before.railAngle;
}
async function drawRadius() {
  const s = await state();
  await page.mouse.move(s.oPx, s.oPy); await page.mouse.down();
  for (let i = 1; i <= 12; i++) { await page.mouse.move(s.oPx + (s.aPx - s.oPx) * i / 12, s.oPy + (s.aPy - s.oPy) * i / 12); await sleep(12); }
  await page.mouse.up();
  return waitFor((x) => x.radiusDrawn, 2000);
}
const tangent = (t) => (t + 90) % 180;
async function playCourse(plan, label) {
  let s = await waitFor((x) => (x.flow === 'aim' || x.flow === 'draw') || x.phase !== 'playing');
  if (s.phase !== 'playing') return s;
  if (s.flow === 'draw') {
    if (label.includes('snap')) await snap(`${label}-draw-before`);
    // 빈 잔디 탭 → 반응(리플+안내)
    await page.mouse.click(W * .5, H * .2); await sleep(150);
    s = await drawRadius();
    log.push(`${label}: radius drawn=${s.radiusDrawn}`);
  }
  s = await waitFor((x) => x.flow === 'aim');
  for (const step of plan) {
    const cur = (await state()).railAngle;
    let target = step === 'ok' ? tangent(s.theta) : step === 'parallel' ? s.theta : step;
    if (target === cur && step !== 'ok') target = (s.theta + 30) % 180; // 같은 각 재제출은 탭이 되므로 다른 오답 각
    const released = await rotateTo(target);
    const r = await waitFor((x) => x.flow === 'rolling' || x.phase !== 'playing', 2500);
    log.push(`${label}: θ=${s.theta} target=${target} released=${released} → ${r.lastResult} mis=${r.misconceptionId} lives=${r.lives} first=${r.firstCorrect}/${r.firstAttempts}`);
    await sleep(step === 'ok' ? 1300 : 900);
    if (label.includes('snap')) await snap(`${label}-${step === 'ok' ? 'pass' : 'miss'}`);
    s = await waitFor((x) => x.flow === 'aim' || x.flow === 'transition' || x.flow === 'draw' || x.flow === 'end', 9000);
    if (s.flow === 'end') return s;
    s = await waitFor((x) => x.flow === 'aim' || x.flow === 'draw' || x.flow === 'end' || x.course !== s.course, 9000);
  }
  return s;
}

// ── 1판: 연습 → 8코스(5코스에서 첫 실수 1회 후 수리) → clear 기대
await snap('title');
await page.mouse.click(W * .5, H * .5);
let s = await waitFor((x) => x.flow === 'aim' && x.onboarding, 6000);
await snap('practice');
results.practiceStart = s.railAngle;
await page.mouse.click(W * .5, H * .25); await sleep(120); await snap('practice-empty-tap');
s = await playCourse([s.theta + 10, 'ok'], 'practice-snap');
for (let c = 1; c <= 8; c++) {
  s = await waitFor((x) => x.course === c && (x.flow === 'aim' || x.flow === 'draw'), 9000);
  if (c === 1 || c === 3 || c === 6) await snap(`course${c}-start`);
  s = await playCourse(c === 5 ? ['parallel', 'ok'] : ['ok'], `c${c}${c === 5 || c === 6 ? '-snap' : ''}`);
  if (s.phase !== 'playing') break;
}
s = await waitFor((x) => x.phase !== 'playing', 9000);
await sleep(1300); await snap('run1-end');
results.run1 = { phase: s.phase, solved: s.solved, firstCorrect: s.firstCorrect, firstAttempts: s.firstAttempts, lives: s.lives, score: s.score };

// ── 2판: 서로 다른 두 코스 첫 실패 → gameover(숙달 불가)
await page.mouse.click(W * .5, H * .5);
s = await waitFor((x) => x.course === 1 && x.flow === 'aim', 9000);
s = await playCourse(['parallel', 'ok'], 'r2c1');
s = await waitFor((x) => x.course === 2 && x.flow === 'aim', 9000);
s = await playCourse(['parallel'], 'r2c2');
s = await waitFor((x) => x.flow === 'end', 9000);
await sleep(1200); await snap('run2-end');
results.run2 = { phase: s.phase, solved: s.solved, firstCorrect: s.firstCorrect, firstAttempts: s.firstAttempts, lives: s.lives };

// ── 3판: 공 0 (같은 코스 오답 반복) → gameover
await page.mouse.click(W * .5, H * .5);
s = await waitFor((x) => x.course === 1 && x.flow === 'aim', 9000);
s = await playCourse(['parallel', 'parallel', 'parallel'], 'r3c1');
s = await waitFor((x) => x.flow === 'end', 9000);
results.run3 = { phase: s.phase, lives: s.lives, firstMisses: s.firstMisses };

results.errors = errors; results.log = log;
fs.writeFileSync(path.join(HERE, `pointer-playthrough-${TAG}.json`), JSON.stringify(results, null, 2));
console.log(JSON.stringify({ run1: results.run1, run2: results.run2, run3: results.run3, errors: errors.length }, null, 1));
console.log(log.join('\n'));
await browser.close(); await srv.close?.();
process.exit(0);
