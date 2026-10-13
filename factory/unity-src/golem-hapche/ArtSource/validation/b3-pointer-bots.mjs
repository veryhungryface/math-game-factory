#!/usr/bin/env node
// 골렘 합체 B3: 답/발문을 읽지 않는 4정책을 실제 WebGL pointer로 각각 200판 검증한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/golem-hapche');
const TRIALS = 200;
const CHANCE = 1 / 15;
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

function balancedRandomPairs(seed) {
  const random = randomSource(seed), guesses = [];
  while (guesses.length < TRIALS) {
    const bag = Array.from({length: 15}, (_, i) => i);
    for (let i = bag.length - 1; i > 0; i--) {
      const j = Math.floor(random() * (i + 1));
      [bag[i], bag[j]] = [bag[j], bag[i]];
    }
    guesses.push(...bag);
  }
  return guesses.slice(0, TRIALS);
}

function pairByIndex(index) {
  let n = 0;
  for (let a = 0; a < 6; a++) for (let b = a + 1; b < 6; b++, n++) if (n === index) return [a, b];
  throw new Error(`bad pair ${index}`);
}

// 390×844 세로 배치: 두 열×세 행. 브라우저 y는 Unity 화면 좌표와 반대 방향이다.
function shieldPoint(index) {
  const col = index % 2, row = Math.floor(index / 2);
  return {x: 390 * (col === 0 ? .27 : .73), y: 844 * (1 - (.485 - row * .18))};
}

async function dragPair(page, pairIndex) {
  const [a, b] = pairByIndex(pairIndex), from = shieldPoint(a), to = shieldPoint(b);
  await page.mouse.move(from.x, from.y);
  await page.mouse.down();
  await sleep(12);
  await page.mouse.move(to.x, to.y, {steps: 3});
  await sleep(12);
  await page.mouse.up();
  await sleep(20);
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: chromePath(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']
});

async function openPage() {
  const page = await browser.newPage(), errors = [];
  await page.setViewport({width: 390, height: 844, deviceScaleFactor: 1});
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/golem-hapche/`, {waitUntil: 'load', timeout: 45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  return {page, errors};
}

async function runPointerPolicy(kind) {
  const {page, errors} = await openPage();
  const randomPairs = balancedRandomPairs(10);
  let correct = 0;
  const hitTrials = [];
  for (let trial = 0; trial < TRIALS; trial++) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    await sleep(16);
    const guess = kind === 'fixed' ? 0 : kind === 'cycle' ? trial % 15 : randomPairs[trial];
    await dragPair(page, guess);
    let state = await page.evaluate(() => window.__GAME_TEST__.getState());
    if (state.firstAttemptTotal !== 1) { await sleep(45); state = await page.evaluate(() => window.__GAME_TEST__.getState()); }
    if (state.firstAttemptTotal !== 1 || state.pointerVersion < 1)
      throw new Error(`${kind} trial ${trial}: actual pointer submission missing ${JSON.stringify(state)}`);
    if (state.firstAttemptCorrect === 1) { correct++; hitTrials.push({trial, guess, problemId: state.problemId}); }
  }
  await page.close();
  return {
    games: TRIALS,
    firstAttemptCorrect: correct,
    firstAttemptTotal: TRIALS,
    firstAttemptRate: correct / TRIALS,
    chance: CHANCE,
    pointer: 'mouse down/move/up',
    hitTrials,
    consoleErrors: errors,
    passed: correct / TRIALS <= CHANCE && errors.length === 0
  };
}

try {
  const fixed = await runPointerPolicy('fixed');
  const cycle = await runPointerPolicy('cycle');
  const random = await runPointerPolicy('random');
  const {page, errors} = await openPage();
  let progressed = 0;
  for (let trial = 0; trial < TRIALS; trial++) {
    const state = await page.evaluate(() => { window.__GAME_TEST__.start(); return window.__GAME_TEST__.getState(); });
    await sleep(18);
    const after = await page.evaluate(() => window.__GAME_TEST__.getState());
    if (state.solved !== 0 || after.solved !== 0 || after.firstAttemptTotal !== 0 || after.pointerVersion !== 0) progressed++;
  }
  await page.close();
  const idle = {
    games: TRIALS,
    firstAttemptCorrect: 0,
    firstAttemptTotal: 0,
    firstAttemptRate: 0,
    chance: CHANCE,
    progressed,
    consoleErrors: errors,
    passed: progressed === 0 && errors.length === 0
  };
  const result = {
    runId: `golem-hapche-b3-pointer-${new Date().toISOString()}`,
    artifactHashSha256: artifactHash(),
    criterion: '4정책×200판; 실제 pointer; 첫 시도율 ≤ 1/15(6.67%); 무입력 진도 0',
    method: 'fixed=A-B, cycle=15쌍 순환, random=seed 10 균형 15쌍 bag, idle=무입력; 발문·정답·answer 훅 미사용',
    fixed, cycle, random, idle
  };
  result.passed = fixed.passed && cycle.passed && random.passed && idle.passed;
  fs.writeFileSync(path.join(HERE, 'b3-pointer-results.json'), JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
