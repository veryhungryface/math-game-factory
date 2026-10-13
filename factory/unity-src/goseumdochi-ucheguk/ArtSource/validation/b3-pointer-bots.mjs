#!/usr/bin/env node
// B3: 발문·정답·훅 정답을 읽지 않는 네 정책을 실제 WebGL mouse down/move/up으로 200판씩 검증한다.
// __GAME_TEST__.start()는 타이틀을 건너뛰고 새 판을 여는 데만 사용한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/goseumdochi-ucheguk');
const TRIALS = +(process.env.TRIALS || 200);
const W = 390, H = 844;
const CHANCE = 720 / (9 ** 6); // 6!/9^6 = 0.13548%, 정답 개수 6회를 맞춘 유리한 상한
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
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(x => `Build/${x}`)]) h.update(fs.readFileSync(path.join(GAME, name)));
  return h.digest('hex');
}

function rng(seed) { let x = seed >>> 0; return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; }; }

const cards3 = [[118, 756], [195, 756], [271, 756]];
const tens = [70, 507], one = [148, 507], stamp = [130, 601], bell = [291, 601];

async function drag(page, a, b, dwell = 18) {
  await page.mouse.move(a[0], a[1]);
  await page.mouse.down(); await sleep(dwell);
  await page.mouse.move(b[0], b[1], { steps: 2 }); await sleep(dwell);
  await page.mouse.up(); await sleep(dwell);
}

async function pull(page, at, amount = 82) { await drag(page, at, [at[0], at[1] + amount]); }

async function stampPair(page, a, b) {
  await drag(page, cards3[a], tens);
  await drag(page, cards3[b], one);
  await pull(page, stamp);
}

async function restart(page) {
  await page.evaluate(() => window.__GAME_TEST__.start());
  await sleep(90);
}

async function openPage(browser, server, errors) {
  const page = await browser.newPage();
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/goseumdochi-ucheguk/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 90000 });
  return page;
}

async function runPolicy(browser, server, policy) {
  const errors = [], random = rng(0x6f7374 ^ policy.length);
  let firstAttemptCorrect = 0, submissions = 0, completions = 0;
  const chunkSize = 50;
  for (let chunkStart = 0; chunkStart < TRIALS; chunkStart += chunkSize) {
    const page = await openPage(browser, server, errors);
    const chunkEnd = Math.min(TRIALS, chunkStart + chunkSize);
    for (let trial = chunkStart; trial < chunkEnd; trial++) {
      await restart(page);
      for (let i = 0; i < 6; i++) {
        let a, b;
        if (policy === 'repeat') { a = 0; b = 0; }
        else if (policy === 'cycle') { a = i % 3; b = (i + 1) % 3; }
        else { a = Math.floor(random() * 3); b = Math.floor(random() * 3); }
        await stampPair(page, a, b);
      }
      await pull(page, bell); await sleep(85);
      const state = await page.evaluate(() => window.__GAME_TEST__.getState());
      if (state.firstAttemptTotal > 0) submissions++;
      if (state.firstAttemptCorrect > 0) firstAttemptCorrect++;
      if (state.phase === 'clear' || state.phase === 'practice_complete') completions++;
    }
    await page.close();
    console.log(`${policy}: ${chunkEnd}/${TRIALS}`);
  }
  const firstAttemptRate = firstAttemptCorrect / TRIALS;
  return { policy, games: TRIALS, submissions, firstAttemptCorrect, firstAttemptRate, chance: CHANCE, completions, completionRate: completions / TRIALS, consoleErrors: errors, passed: submissions === TRIALS && firstAttemptRate <= CHANCE && errors.length === 0 };
}

async function runIdle(browser, server) {
  const errors = [], page = await openPage(browser, server, errors); let progress = 0;
  for (let trial = 0; trial < TRIALS; trial++) {
    await restart(page); const before = await page.evaluate(() => window.__GAME_TEST__.getState().solved);
    await sleep(40); const after = await page.evaluate(() => window.__GAME_TEST__.getState().solved);
    if (after !== before) progress++;
  }
  await page.close();
  return { policy: 'idle', games: TRIALS, submissions: 0, firstAttemptCorrect: 0, firstAttemptRate: 0, chance: CHANCE, completions: 0, completionRate: 0, progress, consoleErrors: errors, passed: progress === 0 && errors.length === 0 };
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), protocolTimeout: 900000, args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  // 공유 빌드 머신의 WebGL 메모리 피크를 피하려고 정책을 한 페이지씩 실행한다.
  const repeat = await runPolicy(browser, server, 'repeat');
  const cycle = await runPolicy(browser, server, 'cycle');
  const random = await runPolicy(browser, server, 'random');
  const idle = await runIdle(browser, server);
  const output = {
    generatedAt: new Date().toISOString(), artifactHashSha256: artifactHash(), viewport: `${W}x${H}`,
    input: 'trusted Puppeteer mouse down/move/up on Unity canvas; start() only resets the run; answer hooks are never called',
    chance: { formula: '6!/9^6', rate: CHANCE }, repeat, cycle, random, idle,
  };
  output.passed = repeat.passed && cycle.passed && random.passed && idle.passed;
  fs.writeFileSync(path.join(HERE, 'b3-pointer-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
  if (!output.passed) process.exitCode = 1;
} finally {
  await browser.close(); await server.close();
}
