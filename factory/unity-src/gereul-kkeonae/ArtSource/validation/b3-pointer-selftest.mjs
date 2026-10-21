#!/usr/bin/env node
// B3: 각 정책 200판. start()는 새 판 진입에만 쓰고, 답 제출은 모두 실제 mouse pointer 경로다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/gereul-kkeonae');
const W = 390, H = 844, GAMES = 200, CHANCE = 1 / 24;
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
    hash.update(fs.readFileSync(path.join(GAME, name)));
  }
  return hash.digest('hex');
}

function xorshift(seed) {
  let x = seed >>> 0;
  return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; };
}

function balancedRandom(seed) {
  const random = xorshift(seed); let bag = [], at = 0;
  return () => {
    if (at >= bag.length) {
      bag = Array.from({ length: 24 }, (_, i) => i + 1);
      for (let i = bag.length - 1; i > 0; i--) { const j = Math.floor(random() * (i + 1)); [bag[i], bag[j]] = [bag[j], bag[i]]; }
      at = 0;
    }
    return bag[at++];
  };
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }

async function waitFor(page, predicate, label, timeoutMs = 2500) {
  const start = Date.now();
  while (Date.now() - start < timeoutMs) {
    const current = await state(page);
    if (predicate(current)) return current;
    await sleep(16);
  }
  throw new Error(`timeout ${label}: ${JSON.stringify(await state(page))}`);
}

const browserPoint = (x, unityY) => [x, H - unityY];

async function drag(page, from, points) {
  await page.mouse.move(from[0], from[1]);
  await page.mouse.down(); await sleep(48);
  for (const to of points) { await page.mouse.move(to[0], to[1], { steps: 2 }); await sleep(64); }
  await page.mouse.up(); await sleep(48);
}

function lengthPoint(s, cm) {
  const t = (cm - 1) / 23;
  if (s.layout === 'land') return browserPoint(s.handleX, s.rulerMin + (s.rulerMax - s.rulerMin) * t);
  return browserPoint(s.rulerMin + (s.rulerMax - s.rulerMin) * t, s.handleY);
}

async function submitLength(page, cm) {
  let s = await state(page);
  if (s.phase !== 'playing' || s.toolStep !== 'pull') throw new Error(`bad pull state: ${JSON.stringify(s)}`);
  const from = browserPoint(s.handleX, s.handleY);
  const points = [];
  // 시작 눈금과 같은 답도 실제로 손을 움직여 확정할 수 있어야 한다.
  if (s.drawnLength === cm) points.push(lengthPoint(s, cm === 24 ? 23 : cm + 1));
  points.push(lengthPoint(s, cm));
  await drag(page, from, points);
  s = await waitFor(page, x => x.toolStep === 'cut', 'pull→cut');

  const cutFrom = browserPoint(s.cutX - 34, s.cutY);
  const cutTo = browserPoint(s.cutX + 34, s.cutY);
  await drag(page, cutFrom, [cutTo]);
  s = await waitFor(page, x => x.toolStep === 'connect', 'cut→connect');

  const piece = browserPoint(s.pieceX, s.pieceY);
  const gap = browserPoint(s.gapX, s.gapY);
  if (process.env.DEBUG_POINTER === '1') await page.screenshot({ path: path.join(HERE, 'connect-debug.png') });
  await drag(page, piece, [gap]);
  return waitFor(page, x => x.phase === 'reveal', 'connect→reveal');
}

async function load(page, url) {
  await page.goto(url, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
}

async function runPolicy(page, url, name, choose) {
  await load(page, url);
  let firstCorrect = 0, firstAttempts = 0, completed = 0, progress = 0;
  const answerHistogram = Array(25).fill(0), inputHistogram = Array(25).fill(0);
  for (let game = 0; game < GAMES; game++) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    const before = await waitFor(page, x => x.phase === 'playing' && x.toolStep === 'pull', 'new run');
    const answer = Number(before.problemId.match(/-l(\d+)-/)[1]);
    const input = choose(game);
    answerHistogram[answer]++; inputHistogram[input]++;
    const after = await submitLength(page, input);
    firstCorrect += after.firstCorrect; firstAttempts += after.firstAttempts;
    progress += after.solved;
    if (after.phase === 'clear') completed++;
    if (after.firstAttempts !== 1 || after.inputCount < 3) throw new Error(`${name} did not submit through pointer path: ${JSON.stringify(after)}`);
  }
  const rate = firstCorrect / firstAttempts;
  return {
    bot: name, games: GAMES, firstAttempts, firstCorrect, firstAttemptRate: rate,
    chance: CHANCE, completionRate: completed / GAMES, progress,
    answerHistogram: answerHistogram.slice(1), inputHistogram: inputHistogram.slice(1),
    passed: firstAttempts === GAMES && rate <= CHANCE && completed === 0,
  };
}

async function runIdle(page, url) {
  await load(page, url);
  let progress = 0, firstAttempts = 0, inputCount = 0;
  for (let game = 0; game < GAMES; game++) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    await sleep(34);
    const s = await state(page); progress += s.solved; firstAttempts += s.firstAttempts; inputCount += s.inputCount;
  }
  return {
    bot: 'idle', games: GAMES, firstAttempts, firstCorrect: 0, firstAttemptRate: 0,
    chance: CHANCE, completionRate: 0, progress, inputCount,
    passed: firstAttempts === 0 && progress === 0 && inputCount === 0,
  };
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true, executablePath: chromePath(), protocolTimeout: 900000,
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'],
});
const consoleErrors = [];
try {
  const page = await browser.newPage();
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
  page.on('pageerror', error => consoleErrors.push(String(error)));
  const url = `${server.url}/g/gereul-kkeonae/`;
  const random = balancedRandom(0x6a09e667);
  const repeat = await runPolicy(page, url, 'repeat', () => 12);
  const cycle = await runPolicy(page, url, 'cycle', game => game % 24 + 1);
  const randomResult = await runPolicy(page, url, 'random', () => random());
  const idle = await runIdle(page, url);
  const results = [repeat, cycle, randomResult, idle];
  const output = {
    runId: `gereul-kkeonae-b3-${new Date().toISOString()}`,
    generatedAt: new Date().toISOString(), artifactHashSha256: artifactHash(), viewport: `${W}x${H}`,
    method: 'start() only opens a deterministic test run; 600 answers use trusted Puppeteer mouse down/move/up for pull, cut, and connect; answer hooks are never called',
    criterion: 'each 200-game first-connection rate <= 1/24; idle progress=0; one submitted first attempt per non-idle game',
    results, consoleErrors,
    passed: results.every(result => result.passed) && consoleErrors.length === 0,
  };
  fs.writeFileSync(path.join(HERE, 'b3-pointer-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
  if (!output.passed) process.exitCode = 1;
  await page.close();
} finally {
  await browser.close(); await server.close();
}
