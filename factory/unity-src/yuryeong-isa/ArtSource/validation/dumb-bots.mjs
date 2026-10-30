#!/usr/bin/env node
// B3 무뇌 게이트: 각 정책 204개의 독립 첫 문제를 같은 WebGL 빌드에 실제 pointer drag로 제출한다.
// start()는 독립 판을 여는 데만 쓰고, 정오 판정은 캔버스의 trusted mouse 이벤트로만 발생한다.
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const OUT = path.join(HERE, 'dumb-bots-results.json');
const W = 390, H = 844, N = Number(process.env.BOT_N || 204);
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  const base = path.join(os.homedir(), '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome'
  ]) { const p = path.join(base, build, rel); if (fs.existsSync(p)) return p; }
}

function moveAt(index) {
  const from = Math.floor(index / 3), r = index % 3;
  return [from, r >= from ? r + 1 : r];
}

let randomState = 0x51a7c3d9;
function randomMove() {
  randomState ^= randomState << 13; randomState ^= randomState >>> 17; randomState ^= randomState << 5;
  return moveAt((randomState >>> 0) % 12);
}

const server = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({
  headless: 'new', executablePath: chromePath(), protocolTimeout: 180000,
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']
});
const page = await browser.newPage();
await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
const consoleErrors = [];
page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
page.on('pageerror', error => consoleErrors.push(String(error)));
await page.goto(`${server.url}/g/yuryeong-isa/?bots=${Date.now()}`, { waitUntil: 'load', timeout: 45000 });
await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 45000 });
await page.evaluate(() => {
  window.__YI_POINTER_AUDIT__ = [];
  const canvas = document.querySelector('canvas');
  for (const type of ['pointerdown', 'pointerup']) canvas.addEventListener(type, e => {
    window.__YI_POINTER_AUDIT__.push({ type, isTrusted: e.isTrusted, x: Math.round(e.clientX), y: Math.round(e.clientY) });
  }, true);
});

async function newTrial() {
  await page.evaluate(() => window.__GAME_TEST__.start());
  await page.waitForFunction(() => {
    const s = window.__GAME_TEST__.getState();
    return s.phase === 'playing' && s.round === 1 && s.firstAttemptTotal === 0 && s.houseXPx?.length === 4 && s.houseXPx[0] > 0 && s.handleYPx > 0;
  }, { timeout: 2000 });
  // 직전 판의 짧은 카메라/간판 피드백이 끝난 뒤 새 판의 실제 화면 좌표를 읽는다.
  await sleep(320);
  return page.evaluate(() => window.__GAME_TEST__.getState());
}

async function dragMove(state, from, to) {
  const layout = await page.evaluate(() => ({ iw: innerWidth, ih: innerHeight, canvas: document.querySelector('canvas').getBoundingClientRect().toJSON() }));
  const sx = state.houseXPx[from], sy = layout.ih - state.handleYPx;
  const tx = state.houseXPx[to], ty = layout.ih - state.dropYPx;
  await page.mouse.move(sx, sy); await page.mouse.down(); await sleep(24);
  await page.mouse.move((sx + tx) / 2, (sy + ty) / 2 - 18, { steps: 2 }); await sleep(24);
  await page.mouse.move(tx, ty, { steps: 2 }); await sleep(24); await page.mouse.up();
  try { await page.waitForFunction(() => window.__GAME_TEST__.getState().firstAttemptTotal === 1, { timeout: 1500 }); }
  catch (error) {
    const after = await page.evaluate(() => window.__GAME_TEST__.getState());
    throw new Error(`pointer submit timeout from=${from} to=${to} coords=${JSON.stringify({sx,sy,tx,ty,layout,state,after})}`, { cause: error });
  }
  return page.evaluate(() => window.__GAME_TEST__.getState());
}

async function runPolicy(name, pickMove, noInput = false) {
  let correct = 0, completed = 0, progressed = 0;
  for (let trial = 0; trial < N; trial++) {
    const before = await newTrial();
    if (noInput) {
      await sleep(8);
      const after = await page.evaluate(() => window.__GAME_TEST__.getState());
      if (after.solved > 0 || after.firstAttemptTotal > 0) progressed++;
      if (after.phase === 'clear') completed++;
      continue;
    }
    const [from, to] = pickMove(trial);
    const after = await dragMove(before, from, to);
    if (after.firstAttemptCorrectCount === 1) correct++;
    if (after.solved > 0) progressed++;
    if (after.phase === 'clear') completed++;
  }
  const rate = correct / N;
  return { name, trials: N, first_attempt_correct: correct, first_attempt_rate: rate, chance_rate: 1 / 12, progressed, completed, completion_rate: completed / N };
}

const policies = [];
policies.push(await runPolicy('repeat_same_0_to_1', () => [0, 1]));
const houseCycle = [[0, 1], [1, 2], [2, 3], [3, 0]];
policies.push(await runPolicy('cycle_house_1_to_2_to_3_to_4', trial => houseCycle[trial % houseCycle.length]));
policies.push(await runPolicy('random_direction', () => randomMove()));
policies.push(await runPolicy('no_input', () => [0, 1], true));
const pointerAudit = await page.evaluate(() => window.__YI_POINTER_AUDIT__);
const result = {
  slug: 'yuryeong-isa',
  method: `${N} independent round-1 sessions per policy; real trusted CDP mouse drag on Unity canvas; start hook only resets the independent session`,
  viewport: `${W}x${H}@1`,
  chance_rate: 1 / 12,
  policies,
  pointer_events: pointerAudit.length,
  pointer_events_all_trusted: pointerAudit.every(event => event.isTrusted),
  no_input_progress_zero: policies[3].progressed === 0,
  console_errors: consoleErrors
};
fs.writeFileSync(OUT, JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
await page.close(); await browser.close(); server.server?.close?.(); server.close?.();
if (consoleErrors.length || !result.pointer_events_all_trusted || !result.no_input_progress_zero || policies.some(p => p.first_attempt_rate > 1 / 12 + 0.025)) process.exitCode = 1;
