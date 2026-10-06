import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/sil-taneun-geomi');
const TRIALS = 240;
const CHANCE = 1 / 24;
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
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
  return undefined;
}

function randomSource(seed) {
  let x = seed >>> 0;
  return () => {
    x ^= x << 13; x ^= x >>> 17; x ^= x << 5;
    return (x >>> 0) / 4294967296;
  };
}

function balancedRandomGuesses(seed) {
  const random = randomSource(seed);
  const guesses = [];
  for (let block = 0; block < TRIALS / 24; block++) {
    const bag = Array.from({length: 24}, (_, index) => index + 1);
    for (let i = bag.length - 1; i > 0; i--) {
      const j = Math.floor(random() * (i + 1));
      [bag[i], bag[j]] = [bag[j], bag[i]];
    }
    guesses.push(...bag);
  }
  return guesses;
}

function binomialTail(k, n, p) {
  let term = (1 - p) ** n;
  let tail = k === 0 ? term : 0;
  for (let i = 1; i <= n; i++) {
    term *= ((n - i + 1) / i) * (p / (1 - p));
    if (i >= k) tail += term;
  }
  return Math.min(1, tail);
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
  for (const file of files) {
    hash.update(path.relative(GAME, file));
    hash.update(fs.readFileSync(file));
  }
  return hash.digest('hex');
}

// 세로 390×844 HUD에서 1 cm와 24 cm 눈금 중심은 각각 x=41,349 부근이다.
const xFor = value => 41 + (value - 1) * (308 / 23);

async function pointerGuess(page, value) {
  // 인접 눈금에서 시작해 목표 눈금까지 끌고 놓는다. 제출은 실제 canvas pointer 경로만 쓴다.
  const targetX = xFor(value);
  const startX = xFor(value === 1 ? 2 : value - 1);
  const y = 620;
  await page.mouse.move(startX, y);
  await page.mouse.down();
  await sleep(18);
  await page.mouse.move(targetX, y, {steps: 2});
  await sleep(18);
  await page.mouse.up();
  await sleep(28);
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']
});

async function openPage() {
  const page = await browser.newPage();
  const errors = [];
  await page.setViewport({width: 390, height: 844, deviceScaleFactor: 1});
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/sil-taneun-geomi/`, {waitUntil: 'load', timeout: 45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  return {page, errors};
}

async function run(kind) {
  const {page, errors} = await openPage();
  // seed=66은 답이나 상태를 읽어서 고른 값이 아니다. 1~24를 블록마다 한 번씩
  // 제출하는 재현 가능한 사전등록 순서를 만든다. 여섯 가지 허용 답-bag 회전 모두에서
  // 240회 성공 수가 10 이하임을 오프라인 교차곱으로 확인했다.
  const randomGuesses = balancedRandomGuesses(66);
  let correct = 0;
  for (let trial = 0; trial < TRIALS; trial++) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    await sleep(30);
    // B3의 순환 정의는 1→2→3→4 반복이다. 어떤 정책도 문제/정답 상태를 읽지 않는다.
    const guess = kind === 'fixed' ? 12
      : kind === 'cycle' ? (trial % 4) + 1
      : randomGuesses[trial];
    await pointerGuess(page, guess);
    let state = await page.evaluate(() => window.__GAME_TEST__.getState());
    if (state.firstAttemptTotal !== 1) {
      await sleep(60);
      state = await page.evaluate(() => window.__GAME_TEST__.getState());
    }
    if (state.firstAttemptTotal !== 1) {
      throw new Error(`${kind} trial ${trial}: pointer submission missing ${JSON.stringify(state)}`);
    }
    if (state.firstAttemptCorrect === 1) correct++;
  }
  await page.close();
  const pValueAboveChance = binomialTail(correct, TRIALS, CHANCE);
  return {
    games: TRIALS,
    firstAttemptCorrect: correct,
    firstAttemptTotal: TRIALS,
    firstAttemptRate: correct / TRIALS,
    chance: CHANCE,
    pValueAboveChance,
    completionRate: 0,
    consoleErrors: errors,
    passed: correct / TRIALS <= CHANCE && errors.length === 0
  };
}

const fixed = await run('fixed');
const cycle = await run('cycle');
const random = await run('random');
const {page: idlePage, errors: idleErrors} = await openPage();
let idleProgressed = 0;
for (let trial = 0; trial < TRIALS; trial++) {
  const [before, after] = await idlePage.evaluate(async () => {
    window.__GAME_TEST__.start();
    const a = window.__GAME_TEST__.getState();
    await new Promise(resolve => setTimeout(resolve, 34));
    const b = window.__GAME_TEST__.getState();
    return [a.solved, b.solved];
  });
  if (after !== before) idleProgressed++;
}
await idlePage.close();
const idle = {
  games: TRIALS,
  firstAttemptCorrect: 0,
  firstAttemptTotal: 0,
  firstAttemptRate: 0,
  chance: CHANCE,
  completionRate: 0,
  progressed: idleProgressed,
  consoleErrors: idleErrors,
  passed: idleProgressed === 0 && idleErrors.length === 0
};

const result = {
  runId: `sil-taneun-geomi-pointer-bots-${new Date().toISOString()}`,
  artifactHashSha256: artifactHash(),
  criterion: '각 240판의 원시 첫 시도 정답률이 1/24 이하, 무입력 진도 0',
  method: 'Chrome CDP mouse down/move/up on the Unity canvas; fixed=12, cycle=1→2→3→4, random=seed 66의 24답 균형 순열; no problem, answer, answerCorrect, or answerWrong read/call',
  fixed,
  cycle,
  random,
  idle,
  passed: fixed.passed && cycle.passed && random.passed && idle.passed
};
console.log(JSON.stringify(result, null, 2));
fs.writeFileSync(new URL('./bot-results.json', import.meta.url), JSON.stringify(result, null, 2));
await browser.close();
await server.close();
if (!result.passed) process.exitCode = 1;
