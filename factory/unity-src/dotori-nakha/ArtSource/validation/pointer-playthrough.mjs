#!/usr/bin/env node
// 실제 pointer 플레이스루(390×844). 답 입력은 전부 진짜 마우스 드래그다(훅의 answerCorrect/answerWrong/start 미사용).
// 주의: 중앙값 계산에는 getState().rawWeights(화면 무게표와 같은 값)를 읽는다 — 화면 OCR 이 아니므로
// 「독립 사람 플레이」 증거가 아니라 입력 경로·상태 전이·실패/회복 경로 검증이다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const OUT = path.join(HERE, 'pointer-playthrough');
fs.mkdirSync(OUT, { recursive: true });
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
function chromePath() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const c = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(c)) return c;
  }
}
const W = 390, H = 844;
const median2 = (raw) => { const s = [...raw].sort((a, b) => a - b), n = s.length; return n % 2 ? 2 * s[(n - 1) / 2] : s[n / 2 - 1] + s[n / 2]; };
const srv = await serveStatic(path.join(ROOT, 'public'));
const url = `${srv.url}/g/dotori-nakha/index.html`;
const browser = await puppeteer.launch({ executablePath: chromePath(), headless: 'new', protocolTimeout: 180000 });
const errors = [];
const results = {};

async function open(tag) {
  const page = await browser.newPage();
  page.on('console', (m) => { if (m.type() === 'error') errors.push(tag + ': ' + m.text()); });
  page.on('pageerror', (e) => errors.push(tag + ' pageerror: ' + e.message));
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  await page.goto(url, { waitUntil: 'load' });
  await page.waitForFunction(() => window.__GAME_TEST__ && window.__GAME_TEST__.ready, { timeout: 30000 });
  await sleep(800);
  return page;
}
const state = (page) => page.evaluate(() => window.__GAME_TEST__.getState());
async function waitFor(page, pred, ms = 8000) {
  const t0 = Date.now();
  while (Date.now() - t0 < ms) { const s = await state(page); if (pred(s)) return s; await sleep(80); }
  return state(page);
}
function railX(s, v2) { const L = W * s.railLeftPermille / 1000, R = W * s.railRightPermille / 1000; return L + (R - L) * (v2 - 2) / 22; }
const railY = (s) => H * (1 - s.railYPermille / 1000);
async function dragBasket(page, v2) {
  const s = await state(page);
  const y = railY(s), from = railX(s, s.selectedIndex + 2), to = railX(s, v2);
  const start = Math.abs(to - from) < 14 ? to + 40 : from;
  await page.mouse.move(start, y); await page.mouse.down();
  for (let i = 1; i <= 12; i++) { await page.mouse.move(start + (to - start) * i / 12, y); await sleep(16); }
  await page.mouse.up();
}
const sortX = (i, n) => W * (0.09 + 0.82 * (n <= 1 ? 0.5 : i / (n - 1)));
const sortY = H * (1 - 0.565);
async function dragTag(page, from, to, n) {
  await page.mouse.move(sortX(from, n), sortY); await page.mouse.down();
  for (let i = 1; i <= 10; i++) { await page.mouse.move(sortX(from, n) + (sortX(to, n) - sortX(from, n)) * i / 10, sortY); await sleep(16); }
  await page.mouse.up(); await sleep(60);
}
async function sortByHand(page) {
  const s = await state(page); const n = s.studentOrder.length;
  for (let p = 0; p < n; p++) {
    const cur = (await state(page)).studentOrder;
    let m = p; for (let i = p + 1; i < n; i++) if (cur[i] < cur[m]) m = i;
    if (m !== p) await dragTag(page, m, p, n);
  }
  return (await state(page)).studentOrder;
}
async function submit(page, v2) {
  await dragBasket(page, v2);
  await waitFor(page, (x) => x.lastResult !== '' || x.phase !== 'playing', 2500);
  return waitFor(page, (x) => (x.lastResult === '' && !x.onboarding) || (x.lastResult === '' && x.onboarding) || x.phase !== 'playing', 6000);
}
async function passPractice(page) {
  await page.mouse.click(W / 2, H * 0.8); // 타이틀 매듭
  let s = await waitFor(page, (x) => x.onboarding && x.activeProblemId === 'practice-odd');
  await submit(page, 10); // 5 g
  return waitFor(page, (x) => !x.onboarding && x.round === 1 && x.lastResult === '', 6000);
}
async function playRound(page, { wrongFirst = false, sort = false } = {}) {
  let s = await waitFor(page, (x) => x.lastResult === '' || x.phase !== 'playing', 6000);
  if (s.onboarding && s.activeProblemId === 'practice-even') { await submit(page, 11); s = await waitFor(page, (x) => !x.onboarding && x.lastResult === '', 6000); }
  const target = median2(s.rawWeights), round = s.round, solved = s.solved;
  let sorted = null;
  if (sort) sorted = await sortByHand(page);
  if (wrongFirst) {
    s = await submit(page, target === 24 ? 23 : target + 1);
    if (s.phase !== 'playing') return { s, sorted };
  }
  s = await submit(page, target);
  return { s: await state(page), sorted, round };
}

// A. 정상판: 연습 → 1무리 손 정렬 → 8무리 정답 → 클리어
{
  const page = await open('A');
  await page.screenshot({ path: path.join(OUT, 'A0-title.png') });
  let s = await passPractice(page);
  const log = [];
  for (let r = 0; r < 8 && s.phase === 'playing'; r++) {
    const res = await playRound(page, { sort: r === 0 || r === 5 });
    if (r === 0) await page.screenshot({ path: path.join(OUT, 'A1-after-sorted-round1.png') });
    if (res.sorted) log.push({ round: r + 1, sortedByHand: res.sorted, ascending: res.sorted.every((v, i, a) => i === 0 || a[i - 1] <= v) });
    s = res.s;
    if (r === 4) await page.screenshot({ path: path.join(OUT, 'A2-mid-run.png') });
  }
  await sleep(1400);
  await page.screenshot({ path: path.join(OUT, 'A3-clear.png') });
  s = await state(page);
  results.A_normal = { phase: s.phase, solved: s.solved, firstCorrect: s.firstCorrect, attempted: s.attemptedRounds, lives: s.lives, score: s.score, sortLog: log };
  await page.close();
}
// B. 첫 오답 뒤 회복: 1무리 오답(걸쇠 1) → 수리 → 나머지 정답 → 클리어(첫 시도 7/8)
{
  const page = await open('B');
  let s = await passPractice(page);
  for (let r = 0; r < 8 && s.phase === 'playing'; r++) {
    const res = await playRound(page, { wrongFirst: r === 0 });
    if (r === 0) await page.screenshot({ path: path.join(OUT, 'B1-after-repair.png') });
    s = res.s;
  }
  await sleep(600);
  s = await state(page);
  results.B_wrong_then_recover = { phase: s.phase, solved: s.solved, firstCorrect: s.firstCorrect, attempted: s.attemptedRounds, lives: s.lives };
  await page.close();
}
// C. 걸쇠 0 잠금: 오답 3회 → gameover, 직후 입력 잠금
{
  const page = await open('C');
  let s = await passPractice(page);
  for (let k = 0; k < 3 && s.phase === 'playing'; k++) {
    s = await state(page);
    const t = median2(s.rawWeights);
    s = await submit(page, t === 2 ? 3 : t - 1);
  }
  await sleep(300);
  await page.screenshot({ path: path.join(OUT, 'C1-gameover.png') });
  const before = await state(page);
  await page.mouse.click(W / 2, H * 0.3); // 0.7초 안 입력 → 잠금(재시작 안 됨)
  const after = await state(page);
  results.C_latches_zero = { phase: before.phase, lives: before.lives, attempted: before.attemptedRounds, firstCorrect: before.firstCorrect, lockedImmediatelyAfter: after.phase === before.phase };
  await page.close();
}
// D. 무입력: 연습 대기(시계 멈춤) 10초, 실전 무입력 125초 → 시간 종료
{
  const page = await open('D');
  await page.mouse.click(W / 2, H * 0.8);
  await sleep(10000);
  const p = await state(page);
  await page.screenshot({ path: path.join(OUT, 'D1-practice-idle-10s.png') });
  await submit(page, 10);
  await waitFor(page, (x) => !x.onboarding && x.round === 1, 6000);
  await sleep(125000);
  const s = await state(page);
  await page.screenshot({ path: path.join(OUT, 'D2-timeout.png') });
  results.D_no_input = { practiceStillOnboarding: p.onboarding, practiceSolved: p.solved, afterTimeoutPhase: s.phase, solved: s.solved, attempted: s.attemptedRounds };
  await page.close();
}
results.consoleErrors = errors;
fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), JSON.stringify(results, null, 2));
console.log(JSON.stringify(results, null, 2));
await browser.close(); srv.close?.();
