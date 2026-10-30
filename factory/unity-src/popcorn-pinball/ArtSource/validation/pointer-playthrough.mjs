#!/usr/bin/env node
// 화면에 보이는 prompt만 계산하고 실제 Unity pointer로 연습·수리·클리어를 검증한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/popcorn-pinball');
const OUT = path.join(HERE, 'pointer-playthrough');
fs.mkdirSync(OUT, { recursive: true });
const W = 390, H = 844, sleep = ms => new Promise(r => setTimeout(r, ms));
const RUN_ID = process.env.MGF_RUN_ID || `popcorn-fix2-${Date.now()}`;

function artifactHash() {
  const hash = crypto.createHash('sha256');
  for (const name of ['index.html', 'meta.json', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(x => `Build/${x}`)]) {
    hash.update(name); hash.update(fs.readFileSync(path.join(GAME, name)));
  }
  return hash.digest('hex');
}

function chromePath() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
  throw new Error('Chrome for Testing not found');
}

function solve(prompt) {
  let m = prompt.match(/∠AOT=(\d+)°, ∠TOB=(\d+)°/);
  if (m) return (+m[1] + +m[2]) / 10;
  m = prompt.match(/∠AQB=(\d+)°/);
  if (m) return +m[1] / 5;
  m = prompt.match(/중심각 ∠AOB의 크기가 (\d+)°/);
  if (m) return +m[1] / 10;
  throw new Error(`발문을 계산할 수 없음: ${prompt}`);
}

async function dragTick(page, tick, inputLog, label) {
  const s = await page.evaluate(() => window.__GAME_TEST__.getState());
  const sx = W / s.screenW, sy = H / s.screenH;
  const px = s.pX * sx, py = H - s.pY * sy, rx = s.refX * sx, ry = H - s.refY * sy;
  const vx = rx - px, vy = ry - py, a = tick * 5 * Math.PI / 180;
  const tx = px + Math.cos(a) * vx - Math.sin(a) * vy;
  const ty = py + Math.sin(a) * vx + Math.cos(a) * vy;
  await page.mouse.move(px, py); await page.mouse.down();
  await page.mouse.move(tx, ty, { steps: 6 }); await page.mouse.up();
  await page.waitForFunction(before => window.__GAME_TEST__.getState().shots > before, { timeout: 1000 }, s.shots || 0);
  const after = await page.evaluate(() => window.__GAME_TEST__.getState());
  inputLog.push({ at: new Date().toISOString(), label, problemId: s.currentProblemId, prompt: s.prompt, selectedTick: tick,
    before: { phase: s.phase, solved: s.solved, firstAttemptCorrect: s.firstAttemptCorrect, lives: s.lives },
    after: { phase: after.phase, solved: after.solved, firstAttemptCorrect: after.firstAttemptCorrect, lives: after.lives, lastAction: after.lastAction } });
}

async function waitPlayable(page) {
  for (let guard = 0; guard < 100; guard++) {
    const s = await page.evaluate(() => window.__GAME_TEST__.getState());
    if (s.phase === 'playing' || s.phase === 'practice' || s.phase === 'clear' || s.phase === 'gameover') return s;
    if (s.phase === 'bridge' && s.lastAction === 'same-arc-bridge') {
      await page.mouse.move(85, 402); await page.mouse.down(); await page.mouse.move(305, 402, { steps: 8 }); await page.mouse.up();
    }
    await sleep(80);
  }
  throw new Error('플레이 가능 상태 대기 초과');
}

async function run(page, recovery, prefix, fromResult = false) {
  const inputLog = [];
  await page.mouse.click(195, fromResult ? 570 : 598);
  await page.waitForFunction(() => window.__GAME_TEST__.getState().phase === 'practice', { timeout: 3000 });
  await dragTick(page, 8, inputLog, 'practice-40deg');
  await page.waitForFunction(() => window.__GAME_TEST__.getState().phase === 'playing', { timeout: 5000 });
  await page.screenshot({ path: path.join(OUT, `${prefix}-start.png`) });
  if (recovery) {
    const s = await page.evaluate(() => window.__GAME_TEST__.getState());
    const correct = solve(s.prompt), wrong = correct === 17 ? 16 : correct + 1;
    await dragTick(page, wrong, inputLog, 'intentional-first-mistake');
    await page.waitForFunction(() => window.__GAME_TEST__.getState().phase === 'playing', { timeout: 4000 });
    await page.screenshot({ path: path.join(OUT, `${prefix}-first-mistake-repaired-ready.png`) });
  }
  for (let guard = 0; guard < 12; guard++) {
    const s = await waitPlayable(page);
    if (s.phase === 'clear' || s.phase === 'gameover') break;
    const tick = solve(s.prompt);
    if (!Number.isInteger(tick) || tick < 1 || tick > 17) throw new Error(`잘못 계산한 tick ${tick}: ${s.prompt}`);
    await dragTick(page, tick, inputLog, `solve-${s.currentProblemIndex}`);
    await sleep(70);
  }
  const end = await page.evaluate(() => window.__GAME_TEST__.getState());
  await sleep(900);
  await page.screenshot({ path: path.join(OUT, `${prefix}-result.png`) });
  return { finalState: end, inputLog };
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--mute-audio'] });
const errors = [];
try {
  async function page() {
    const p = await browser.newPage(); await p.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
    p.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
    p.on('pageerror', e => errors.push(String(e)));
    await p.goto(`${server.url}/g/popcorn-pinball/?play=${Date.now()}`, { waitUntil: 'load', timeout: 60000 });
    await p.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 90000 });
    return p;
  }
  const hashBefore = artifactHash();
  const p1 = await page(), recoveryRun = await run(p1, true, 'recovery'); await p1.close();
  const p2 = await page(), cleanRun = await run(p2, false, 'clean');
  const secondRun = await run(p2, false, 'second', true); await p2.close();
  const recovery = recoveryRun.finalState, clean = cleanRun.finalState, second = secondRun.finalState;
  const result = {
    generatedAt: new Date().toISOString(), runId: RUN_ID, artifactHashSha256: hashBefore,
    artifactHashAfterSha256: artifactHash(), method: 'visible prompt math + trusted browser pointer only',
    recovery, clean, second,
    inputLogs: { recovery: recoveryRun.inputLog, clean: cleanRun.inputLog, second: secondRun.inputLog },
    consoleErrors: errors,
    passed: recovery.phase === 'clear' && recovery.solved === 8 && recovery.firstAttemptCorrect === 7 && recovery.lives === 2
      && clean.phase === 'clear' && clean.solved === 8 && clean.firstAttemptCorrect === 8 && clean.lives === 3
      && second.phase === 'clear' && second.solved === 8 && second.firstAttemptCorrect === 8 && second.lives === 3
      && hashBefore === artifactHash() && errors.length === 0,
  };
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally { await browser.close(); await server.close(); }
