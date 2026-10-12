#!/usr/bin/env node
// B3: 문제/정답을 읽지 않는 네 정책을 실제 Chrome pointer 경로로 각각 200회 검증한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/geurimja-geukjang');
const TRIALS = 200;
const CHANCE = 1 / 9;
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome'
    ]) {
      const candidate = path.join(base, build, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
  throw new Error('Chrome for Testing not found');
}

function artifactHash() {
  const files = [];
  (function walk(dir) {
    for (const name of fs.readdirSync(dir).sort()) {
      const file = path.join(dir, name);
      if (fs.statSync(file).isDirectory()) walk(file); else files.push(file);
    }
  })(GAME);
  const hash = crypto.createHash('sha256');
  for (const file of files) { hash.update(path.relative(GAME, file)); hash.update(fs.readFileSync(file)); }
  return hash.digest('hex');
}

function randomSource(seed) {
  let x = seed >>> 0;
  return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; };
}

function balancedRandomGuesses(seed) {
  const random = randomSource(seed), guesses = [];
  while (guesses.length < TRIALS) {
    const bag = Array.from({length: 9}, (_, i) => i + 2);
    for (let i = bag.length - 1; i > 0; i--) {
      const j = Math.floor(random() * (i + 1));
      [bag[i], bag[j]] = [bag[j], bag[i]];
    }
    guesses.push(...bag);
  }
  return guesses.slice(0, TRIALS);
}

const xFor = d => 390 * (.12 + (d - 2) * .70 / 8);

async function dragPuppet(page, d) {
  const adjacent = d === 2 ? 3 : d - 1;
  await page.mouse.move(xFor(adjacent), 608);
  await page.mouse.down();
  await sleep(38);
  await page.mouse.move(xFor(d), 608, {steps: 3});
  await sleep(38);
  await page.mouse.up();
  await sleep(38);
}

async function pullCurtain(page) {
  await page.mouse.move(343, 397);
  await page.mouse.down();
  await sleep(38);
  await page.mouse.move(343, 540, {steps: 3});
  await sleep(38);
  await page.mouse.up();
  await sleep(38);
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true, executablePath: chromePath(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']
});

async function openPage() {
  const page = await browser.newPage(), errors = [];
  await page.setViewport({width: 390, height: 844, deviceScaleFactor: 1});
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/geurimja-geukjang/`, {waitUntil: 'load', timeout: 45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  return {page, errors};
}

async function run(kind) {
  const {page, errors} = await openPage();
  const randomGuesses = balancedRandomGuesses(1);
  let correct = 0;
  for (let trial = 0; trial < TRIALS; trial++) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    await sleep(20);
    // 고정=3, 순환=2→3→4→5, 무작위=2~10 균형 bag. 상태/발문/답은 읽지 않는다.
    const guess = kind === 'fixed' ? 3 : kind === 'cycle' ? 2 + (trial % 4) : randomGuesses[trial];
    await dragPuppet(page, guess);
    await pullCurtain(page);
    let state = await page.evaluate(() => window.__GAME_TEST__.getState());
    if (state.firstAttemptTotal !== 1) { await sleep(60); state = await page.evaluate(() => window.__GAME_TEST__.getState()); }
    if (state.firstAttemptTotal !== 1) throw new Error(`${kind} trial ${trial}: submission missing ${JSON.stringify(state)}`);
    if (state.firstAttemptCorrect === 1) correct++;
  }
  await page.close();
  return {
    games: TRIALS, firstAttemptCorrect: correct, firstAttemptTotal: TRIALS,
    firstAttemptRate: correct / TRIALS, chance: CHANCE, completionRate: 0,
    consoleErrors: errors, passed: correct / TRIALS <= CHANCE && errors.length === 0
  };
}

try {
  const fixed = await run('fixed');
  const cycle = await run('cycle');
  const random = await run('random');
  const {page: idlePage, errors: idleErrors} = await openPage();
  let progressed = 0;
  for (let trial = 0; trial < TRIALS; trial++) {
    const [before, after] = await idlePage.evaluate(async () => {
      window.__GAME_TEST__.start();
      const a = window.__GAME_TEST__.getState();
      await new Promise(resolve => setTimeout(resolve, 34));
      const b = window.__GAME_TEST__.getState();
      return [a.solved, b.solved];
    });
    if (after !== before) progressed++;
  }
  await idlePage.close();
  const idle = {
    games: TRIALS, firstAttemptCorrect: 0, firstAttemptTotal: 0, firstAttemptRate: 0,
    chance: CHANCE, completionRate: 0, progressed, consoleErrors: idleErrors,
    passed: progressed === 0 && idleErrors.length === 0
  };
  const result = {
    runId: `geurimja-geukjang-pointer-bots-${new Date().toISOString()}`,
    artifactHashSha256: artifactHash(),
    criterion: '각 200판 첫 시도 정답률이 1/9 이하, 무입력 진도 0',
    method: 'trusted Chrome mouse down/move/up on Unity canvas; fixed=3, cycle=2→3→4→5, random=seed 1 balanced 2~10 bags; no prompt/answer read and no answer hooks',
    fixed, cycle, random, idle
  };
  result.passed = fixed.passed && cycle.passed && random.passed && idle.passed;
  fs.writeFileSync(path.join(HERE, 'pointer-bot-results.json'), JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
