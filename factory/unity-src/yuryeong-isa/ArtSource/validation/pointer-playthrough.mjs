#!/usr/bin/env node
// 정상판, 첫 실수→직접 역이사→재성공판, 세 번째 실수 gameover를 실제 pointer로 검증한다.
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const OUT_DIR = path.join(HERE, 'pointer-playthrough');
const OUT_JSON = path.join(HERE, 'pointer-playthrough-results.json');
const W = 390, H = 844;
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
fs.rmSync(OUT_DIR, { recursive: true, force: true }); fs.mkdirSync(OUT_DIR, { recursive: true });

function chromePath() {
  const base = path.join(os.homedir(), '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome'
  ]) { const p = path.join(base, build, rel); if (fs.existsSync(p)) return p; }
}

function variance(values) {
  const mean = values.reduce((a, b) => a + b, 0) / 4;
  return values.reduce((sum, value) => sum + (value - mean) ** 2, 0) / 4;
}
function solve(state) {
  for (let from = 0; from < 4; from++) for (let to = 0; to < 4; to++) if (from !== to) {
    const result = [...state.originalValues]; result[from] -= 2; result[to] += 2;
    if (variance(result) === state.targetVariance) return [from, to];
  }
  throw new Error(`no solution ${JSON.stringify(state)}`);
}
function wrongMove(state) {
  const [answerFrom, answerTo] = solve(state);
  for (let from = 0; from < 4; from++) for (let to = 0; to < 4; to++) if (from !== to && (from !== answerFrom || to !== answerTo)) return [from, to];
}

const server = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({ headless: 'new', executablePath: chromePath(), protocolTimeout: 180000, args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const page = await browser.newPage(); await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
const errors = []; const log = [];
page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
page.on('pageerror', error => errors.push(String(error)));
await page.goto(`${server.url}/g/yuryeong-isa/?playthrough=${Date.now()}`, { waitUntil: 'load', timeout: 45000 });
await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 45000 });
await page.evaluate(() => {
  window.__YI_PLAY_POINTER__ = [];
  const canvas = document.querySelector('canvas');
  for (const type of ['pointerdown', 'pointerup']) canvas.addEventListener(type, event => window.__YI_PLAY_POINTER__.push({ type, trusted: event.isTrusted }), true);
});
const state = () => page.evaluate(() => window.__GAME_TEST__.getState());

async function drag(from, to, label) {
  const before = await state();
  const sx = before.houseXPx[from], sy = H - before.handleYPx, tx = before.houseXPx[to], ty = H - before.dropYPx;
  await page.mouse.move(sx, sy); await page.mouse.down(); await sleep(45);
  await page.mouse.move((sx + tx) / 2, (sy + ty) / 2 - 18, { steps: 4 }); await sleep(45);
  await page.mouse.move(tx, ty, { steps: 4 }); await sleep(45); await page.mouse.up();
  await sleep(90); const after = await state();
  log.push({ label, from, to, before: { problemId: before.problemId, solved: before.solved, lives: before.lives, first: before.firstAttemptCorrectCount }, after: { problemId: after.problemId, solved: after.solved, lives: after.lives, first: after.firstAttemptCorrectCount, repair: after.repairState } });
  return { before, after };
}

async function waitNext(problemId, terminal = false) {
  await page.waitForFunction(({ problemId, terminal }) => {
    const s = window.__GAME_TEST__.getState();
    return s.problemId !== problemId || (terminal && (s.phase === 'clear' || s.phase === 'gameover'));
  }, { timeout: 5000 }, { problemId, terminal });
  await sleep(180);
}

await page.screenshot({ path: path.join(OUT_DIR, '00-title.png') });
await page.mouse.click(W / 2, H * .55); await sleep(500);
let s = await state();
if (!s.onboarding) throw new Error('title pointer did not start onboarding');
await drag(3, 0, 'practice-fixed-20-to-2');
await page.screenshot({ path: path.join(OUT_DIR, '01-practice-reveal.png') });
await page.waitForFunction(() => { const s = window.__GAME_TEST__.getState(); return !s.onboarding && s.round === 1; }, { timeout: 5000 });

async function cleanRun(name) {
  for (let round = 1; round <= 8; round++) {
    const before = await state(), [from, to] = solve(before);
    await drag(from, to, `${name}-round-${round}-correct`);
    await waitNext(before.problemId, round === 8);
  }
  await page.screenshot({ path: path.join(OUT_DIR, `${name}-result.png`) });
  return state();
}

const clean = await cleanRun('02-clean');
await page.mouse.click(W / 2, H / 2); await sleep(500);

async function recoveryRun() {
  for (let round = 1; round <= 8; round++) {
    let before = await state();
    if (round === 1) {
      const [wf, wt] = wrongMove(before); await drag(wf, wt, 'recovery-first-wrong');
      await page.screenshot({ path: path.join(OUT_DIR, '03-wrong-state.png') }); await sleep(1200);
      await drag(wt, wf, 'recovery-direct-reverse'); await sleep(1200);
      before = await state();
    }
    const [from, to] = solve(before); await drag(from, to, `recovery-round-${round}-correct`);
    await waitNext(before.problemId, round === 8);
  }
  await page.screenshot({ path: path.join(OUT_DIR, '04-recovery-result.png') });
  return state();
}

const recovery = await recoveryRun();
await page.mouse.click(W / 2, H / 2); await sleep(500);

async function gameoverRun() {
  for (let round = 1; round <= 3; round++) {
    const before = await state(), [wf, wt] = wrongMove(before); await drag(wf, wt, `gameover-round-${round}-wrong`); await sleep(1200);
    if (round < 3) { await drag(wt, wf, `gameover-round-${round}-repair`); await sleep(1200); const retry = await state(); const [cf, ct] = solve(retry); await drag(cf, ct, `gameover-round-${round}-later-correct`); await waitNext(retry.problemId); }
  }
  await page.waitForFunction(() => window.__GAME_TEST__.getState().phase === 'gameover', { timeout: 5000 });
  await page.screenshot({ path: path.join(OUT_DIR, '05-third-miss-gameover.png') });
  return state();
}

const gameover = await gameoverRun();
const audit = await page.evaluate(() => window.__YI_PLAY_POINTER__);
const result = {
  slug: 'yuryeong-isa', method: 'actual trusted pointer title + practice + clean run + wrong/reverse/retry run + three-miss gameover',
  clean, recovery, gameover, pointer_events: audit.length, pointer_events_all_trusted: audit.every(event => event.trusted), console_errors: errors, input_log: log
};
result.passed = clean.phase === 'clear' && clean.solved === 8 && clean.firstAttemptCorrectCount === 8 && clean.lives === 3 &&
  recovery.phase === 'clear' && recovery.solved === 8 && recovery.firstAttemptCorrectCount === 7 && recovery.lives === 2 &&
  gameover.phase === 'gameover' && gameover.lives === 0 && result.pointer_events_all_trusted && errors.length === 0;
fs.writeFileSync(OUT_JSON, JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify({ passed: result.passed, clean: { phase: clean.phase, solved: clean.solved, first: clean.firstAttemptCorrectCount, lives: clean.lives }, recovery: { phase: recovery.phase, solved: recovery.solved, first: recovery.firstAttemptCorrectCount, lives: recovery.lives }, gameover: { phase: gameover.phase, solved: gameover.solved, first: gameover.firstAttemptCorrectCount, lives: gameover.lives }, pointer_events: result.pointer_events, errors }, null, 2));
await page.close(); await browser.close(); server.server?.close?.(); server.close?.();
if (!result.passed) process.exitCode = 1;
