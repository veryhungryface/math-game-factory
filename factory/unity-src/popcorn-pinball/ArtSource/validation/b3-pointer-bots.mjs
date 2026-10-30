#!/usr/bin/env node
// B3: 정답/문제 은행을 읽지 않고 실제 WebGL mouse down/move/up으로 4정책을 각 200판 검증한다.
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
const TRIALS = +(process.env.TRIALS || 200);
const W = 390, H = 844, CHANCE = 1 / 17;
const RUN_ID = process.env.MGF_RUN_ID || `popcorn-fix2-b3-${Date.now()}`;
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome',
  ]) { const p = path.join(base, build, rel); if (fs.existsSync(p)) return p; }
  throw new Error('Chrome for Testing not found');
}

function artifactHash() {
  const h = crypto.createHash('sha256');
  for (const name of ['index.html', 'meta.json', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(x => `Build/${x}`)]) {
    h.update(name); h.update(fs.readFileSync(path.join(GAME, name)));
  }
  return h.digest('hex');
}

function randomSource(seed) {
  let x = seed >>> 0;
  return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; };
}

function binomialTail(k, n, p) {
  if (!n) return 1;
  let prob = Math.pow(1 - p, n), tail = k === 0 ? prob : 0;
  for (let i = 1; i <= n; i++) {
    prob *= ((n - i + 1) / i) * (p / (1 - p));
    if (i >= k) tail += prob;
  }
  return Math.min(1, tail);
}

async function openPage(browser, server, errors) {
  const page = await browser.newPage();
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/popcorn-pinball/?b3=${Date.now()}`, { waitUntil: 'load', timeout: 60000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 90000 });
  return page;
}

async function dragTick(page, tick) {
  const s = await page.evaluate(() => window.__GAME_TEST__.getState());
  const sx = W / s.screenW, sy = H / s.screenH;
  const px = s.pX * sx, py = H - s.pY * sy;
  const rx = s.refX * sx, ry = H - s.refY * sy;
  const vx = rx - px, vy = ry - py;
  const a = tick * 5 * Math.PI / 180, c = Math.cos(a), sn = Math.sin(a);
  const tx = px + c * vx - sn * vy, ty = py + sn * vx + c * vy;
  // 포신의 넓은 P 축 히트박스에서 잡으면 모든 눈금이 같은 74px 드래그 길이를 갖는다.
  // 최종 놓는 각도만 판정되므로 17개 입력 띠의 폭은 그대로다.
  await page.mouse.move(px, py);
  await page.mouse.down(); await sleep(10);
  await page.mouse.move(tx, ty, { steps: 5 }); await sleep(10);
  await page.mouse.up();
  try {
    await page.waitForFunction(before => window.__GAME_TEST__.getState().shots > before, { timeout: 700, polling: 15 }, s.shots || 0);
  } catch {
    return false;
  }
  try {
    await page.waitForFunction(() => window.__GAME_TEST__.getState().phase !== 'feedback', { timeout: 1200, polling: 20 });
  } catch { await sleep(80); }
  return true;
}

async function runPolicy(browser, server, policy) {
  const errors = [], random = randomSource(0x50494e ^ policy.length);
  let firstAttempted = 0, firstCorrect = 0, completions = 0, solvedTotal = 0, shots = 0;
  const chunkSize = 50;
  for (let chunkStart = 0; chunkStart < TRIALS; chunkStart += chunkSize) {
    const page = await openPage(browser, server, errors);
    const chunkEnd = Math.min(TRIALS, chunkStart + chunkSize);
    for (let game = chunkStart; game < chunkEnd; game++) {
      await page.evaluate(() => window.__GAME_TEST__.start());
      await sleep(35);
      for (let guard = 0; guard < 40; guard++) {
        const before = await page.evaluate(() => window.__GAME_TEST__.getState());
        if (before.phase === 'clear' || before.phase === 'gameover') break;
        const tick = policy === 'repeat' ? 9 : policy === 'cycle' ? (shots % 17) + 1 : Math.floor(random() * 17) + 1;
        let submitted = false;
        for (let retry = 0; retry < 3 && !submitted; retry++) submitted = await dragTick(page, tick);
        if (!submitted) throw new Error(`${policy} game ${game}: pointer submission missing for tick ${tick}`);
        shots++;
      }
      const end = await page.evaluate(() => window.__GAME_TEST__.getState());
      firstAttempted += end.firstAttempted || 0;
      firstCorrect += end.firstAttemptCorrect || 0;
      solvedTotal += end.solved || 0;
      if (end.phase === 'clear') completions++;
    }
    await page.close();
    console.log(`${policy}: ${chunkEnd}/${TRIALS}`);
  }
  const rate = firstAttempted ? firstCorrect / firstAttempted : 0;
  const pValueAboveChance = binomialTail(firstCorrect, firstAttempted, CHANCE);
  return {
    policy, games: TRIALS, shots, firstAttempted, firstCorrect, firstAttemptRate: rate,
    chance: CHANCE, pValueAboveChance, completions, completionRate: completions / TRIALS,
    averageSolved: solvedTotal / TRIALS, consoleErrors: errors,
    passed: firstAttempted > 0 && pValueAboveChance >= .05 && errors.length === 0,
  };
}

async function runIdle(browser, server) {
  const errors = [], page = await openPage(browser, server, errors);
  let progress = 0, completions = 0;
  for (let game = 0; game < TRIALS; game++) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    const before = await page.evaluate(() => window.__GAME_TEST__.getState());
    await sleep(45);
    const after = await page.evaluate(() => window.__GAME_TEST__.getState());
    if (after.solved !== before.solved || after.firstAttempted !== before.firstAttempted) progress++;
    if (after.phase === 'clear') completions++;
  }
  await page.close();
  return {
    policy: 'idle', games: TRIALS, firstAttempted: 0, firstCorrect: 0, firstAttemptRate: 0,
    chance: CHANCE, progress, completions, completionRate: completions / TRIALS,
    consoleErrors: errors, passed: progress === 0 && completions === 0 && errors.length === 0,
  };
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true, executablePath: chromePath(), protocolTimeout: 900000,
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'],
});
try {
  // 장시간 600판 뒤 새 Unity 페이지가 뜨지 않는 브라우저 수명 문제를 피하려고 가벼운 무입력을 먼저 측정한다.
  const idle = await runIdle(browser, server);
  const repeat = await runPolicy(browser, server, 'repeat');
  const cycle = await runPolicy(browser, server, 'cycle');
  const random = await runPolicy(browser, server, 'random');
  const output = {
    generatedAt: new Date().toISOString(), runId: RUN_ID, artifactHashSha256: artifactHash(), viewport: `${W}x${H}`,
    method: 'trusted Puppeteer mouse down/move/up on the final Unity WebGL canvas; start() only resets runs; answer/sample hooks and problem answers are never read',
    criterion: 'active policies are not significantly above 1/17 chance (one-sided binomial p>=0.05); idle progress is zero',
    chance: { formula: '1/17', rate: CHANCE }, repeat, cycle, random, idle,
  };
  output.passed = repeat.passed && cycle.passed && random.passed && idle.passed;
  fs.writeFileSync(path.join(HERE, 'b3-pointer-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
  if (!output.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
