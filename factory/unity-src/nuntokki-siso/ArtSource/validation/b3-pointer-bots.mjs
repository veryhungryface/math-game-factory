#!/usr/bin/env node
// B3: 발문·정답을 읽지 않는 네 정책을 실제 Unity 캔버스 mouse down/move/up으로 200판씩 검증한다.
// __GAME_TEST__.start()는 새 판 진입에만 사용하며 answerCorrect/answerWrong은 호출하지 않는다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/nuntokki-siso');
const TRIALS = +(process.env.TRIALS || 200);
const W = 390, H = 844, CHANCE = 1 / 7;
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
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(file => `Build/${file}`)]) hash.update(fs.readFileSync(path.join(GAME, name)));
  return hash.digest('hex');
}

function rng(seed) { let x = seed >>> 0; return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; }; }

async function drag(page, from, to, steps = 3) {
  await page.mouse.move(from[0], from[1]); await page.mouse.down(); await sleep(12);
  await page.mouse.move(to[0], to[1], { steps }); await sleep(12); await page.mouse.up(); await sleep(18);
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }

function tickY(control, k) {
  const t = (k - 2) / 6;
  return control.gaugeBottomY - (control.gaugeBottomY - control.gaugeTopY) * t;
}

async function blindSubmit(page, k) {
  let control = await state(page);
  const y = tickY(control, k);
  const detour = Math.max(control.gaugeTopY + 8, Math.min(control.gaugeBottomY - 8, y + (y < (control.gaugeTopY + control.gaugeBottomY) / 2 ? 38 : -38)));
  await page.mouse.move(control.handleX, control.handleY); await page.mouse.down();
  await page.mouse.move(control.handleX, detour, { steps: 2 });
  await page.mouse.move(control.handleX, y, { steps: 3 }); await page.mouse.up(); await sleep(22);
  control = await state(page);
  await drag(page, [control.pinX, control.pinY], [control.pinX, control.pinY + 86]);
  await sleep(45);
  return state(page);
}

async function openPage(browser, server, errors) {
  const page = await browser.newPage();
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/nuntokki-siso/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 90000 });
  return page;
}

async function runPolicy(browser, server, policy) {
  const errors = [], random = rng(1);
  let submissions = 0, firstAttemptCorrect = 0, completions = 0;
  // 한 페이지에서 200개 연속 runSerial을 써야 실제 덱 분포를 본다. 50판마다 페이지를
  // 다시 열면 seed 1..50만 네 번 반복되어 정책과 덱 사이에 가짜 상관이 생긴다.
  const chunkSize = TRIALS;
  for (let chunk = 0; chunk < TRIALS; chunk += chunkSize) {
    const page = await openPage(browser, server, errors);
    const end = Math.min(TRIALS, chunk + chunkSize);
    for (let trial = chunk; trial < end; trial++) {
      await page.evaluate(() => window.__GAME_TEST__.start()); await sleep(28);
      const k = policy === 'repeat' ? 2 : policy === 'cycle' ? 2 + (trial % 7) : 2 + Math.floor(random() * 7);
      let result = await blindSubmit(page, k);
      if (result.firstAttemptTotal === 0) { await sleep(45); result = await blindSubmit(page, k); }
      if (result.firstAttemptTotal === 1) submissions++;
      if (result.firstAttemptCorrect === 1) firstAttemptCorrect++;
      if (result.phase === 'clear') completions++;
    }
    await page.close();
    console.log(`${policy}: ${end}/${TRIALS}`);
  }
  const rate = firstAttemptCorrect / TRIALS;
  return { policy, games: TRIALS, submissions, firstAttemptCorrect, firstAttemptRate: rate, chance: CHANCE, completions, completionRate: completions / TRIALS, consoleErrors: errors, passed: submissions === TRIALS && rate <= CHANCE && errors.length === 0 };
}

async function runIdle(browser, server) {
  const errors = [], page = await openPage(browser, server, errors);
  let progress = 0;
  for (let trial = 0; trial < TRIALS; trial++) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    const before = await state(page); await sleep(20); const after = await state(page);
    if (after.solved !== before.solved || after.firstAttemptTotal !== before.firstAttemptTotal) progress++;
  }
  await page.close();
  return { policy: 'idle', games: TRIALS, submissions: 0, firstAttemptCorrect: 0, firstAttemptRate: 0, chance: CHANCE, completions: 0, completionRate: 0, progress, consoleErrors: errors, passed: progress === 0 && errors.length === 0 };
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), protocolTimeout: 900000, args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  const repeat = await runPolicy(browser, server, 'repeat');
  const cycle = await runPolicy(browser, server, 'cycle');
  const random = await runPolicy(browser, server, 'random');
  const idle = await runIdle(browser, server);
  const output = {
    generatedAt: new Date().toISOString(), artifactHashSha256: artifactHash(), viewport: `${W}x${H}`,
    input: 'trusted Puppeteer mouse down/move/up; start() only resets; answer hooks never called',
    chance: { formula: '1/7', rate: CHANCE }, repeat, cycle, random, idle,
  };
  output.passed = repeat.passed && cycle.passed && random.passed && idle.passed;
  fs.writeFileSync(path.join(HERE, 'b3-pointer-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
  if (!output.passed) process.exitCode = 1;
} finally {
  await browser.close(); await server.close();
}
