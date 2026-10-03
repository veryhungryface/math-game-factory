import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const TRIALS = 200;
const CHANCE = 1 / 48;
// 검수 게이트의 원시 상한: 구판 24칸 우연 수준(1/24). 48칸 판에서는 이론 우연 수준이 1/48이다.
const RAW_CEILING = 1 / 24;

function binomialTail(k, n, p) {
  let prob = Math.pow(1 - p, n), tail = k === 0 ? prob : 0;
  for (let i = 1; i <= n; i++) { prob *= ((n - i + 1) / i) * (p / (1 - p)); if (i >= k) tail += prob; }
  return Math.min(1, tail);
}
const sleep = ms => new Promise(r => setTimeout(r, ms));

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const b of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
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

function yFor(length) { return 731 - (length - 1) * (300 / 47); }

async function pointerGuess(page, guess) {
  // 첫 접점에서 48칸 중 가까운 값을 고르고, 4px 미세 이동으로 확대 스냅을 유지한 채 놓는다.
  // 문제 id·정답·시작값은 이 좌표 계산에 사용하지 않는다.
  await page.mouse.move(195, yFor(guess));
  await page.mouse.down();
  await sleep(18);
  await page.mouse.move(195, yFor(guess) + (guess === 1 ? -4 : 4));
  await sleep(18);
  await page.mouse.up();
  await sleep(18);
}

function artifactHash() {
  const dir = path.join(PUBLIC, 'g/cable-lock');
  const files = [];
  (function walk(d) {
    for (const f of fs.readdirSync(d).sort()) {
      const p = path.join(d, f);
      if (fs.statSync(p).isDirectory()) walk(p); else files.push(p);
    }
  })(dir);
  const h = crypto.createHash('sha256');
  for (const f of files) { h.update(path.relative(dir, f)); h.update(fs.readFileSync(f)); }
  return h.digest('hex');
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
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', e => errors.push(String(e)));
  await page.goto(`${server.url}/g/cable-lock/`, {waitUntil: 'load', timeout: 45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  return {page, errors};
}

async function run(kind) {
  const {page, errors} = await openPage();
  const random = randomSource(kind === 'random' ? 0x5a17c0de : 0x1);
  let correct = 0;
  for (let i = 0; i < TRIALS; i++) {
    // 첫 호출은 직전 reveal을 끝내고, 두 번째 호출은 새 판을 만든다. 답 정보는 읽지 않는다.
    const shown = await page.evaluate(() => { window.__GAME_TEST__.start(); window.__GAME_TEST__.start(); return window.__GAME_TEST__.getState().selectedLength; });
    // relative: 화면에 보이는 시작 눈금에서 항상 같은 칸 수만큼 끄는 무뇌 입력(시작점 누설 검사).
    const guess = kind === 'fixed' ? 1 : kind === 'cycle' ? (i % 48) + 1
      : kind === 'relative' ? (((shown - 1 + 7) % 48) + 1) : Math.floor(random() * 48) + 1;
    let after;
    for (let retry = 0; retry < 3; retry++) {
      await pointerGuess(page, guess);
      after = await page.evaluate(() => window.__GAME_TEST__.getState());
      if (after.firstAttemptTotal === 1) break;
      await sleep(50);
    }
    if (after.firstAttemptCorrect === 1) correct++;
    if (after.firstAttemptTotal !== 1) throw new Error(`${kind} trial ${i}: pointer submit missing ${JSON.stringify(after)}`);
  }
  await page.close();
  const pValueAboveChance = binomialTail(correct, TRIALS, CHANCE);
  return {trials: TRIALS, correct, rate: correct / TRIALS, chance: CHANCE, rawCeiling: RAW_CEILING, pValueAboveChance,
    consoleErrors: errors,
    passed: correct / TRIALS <= RAW_CEILING && pValueAboveChance >= 0.05 && errors.length === 0};
}

// Unity WebGL 인스턴스 세 개를 동시에 띄우면 메모리 압박으로 ready가 지연되므로 전략별로 직렬 실행한다.
const fixed = await run('fixed');
const cycle = await run('cycle');
const random = await run('random');
const relative = await run('relative');
const {page: idlePage, errors: idleErrors} = await openPage();
const idleBefore = await idlePage.evaluate(() => { window.__GAME_TEST__.start(); return window.__GAME_TEST__.getState(); });
await new Promise(r => setTimeout(r, 500));
const idleAfter = await idlePage.evaluate(() => window.__GAME_TEST__.getState());
const idle = {progressBefore: idleBefore.solved, progressAfter: idleAfter.solved, consoleErrors: idleErrors,
  passed: idleBefore.solved === idleAfter.solved && idleErrors.length === 0};
await idlePage.close();
const result = {
  run_id: `cable-lock-pointer-bots-${new Date().toISOString()}`,
  artifact_hash_sha256: artifactHash(),
  criterion: 'raw first-try rate <= 1/24 (review ceiling) AND not significantly above 1/48 chance (one-sided binomial p >= 0.05); idle progress 0',
  method: 'Chrome CDP mouse down/move/up on Unity canvas; no problemId, sampleProblems, answerCorrect, or answerWrong reads/calls',
  fixed, cycle, random, relative, idle,
  passed: fixed.passed && cycle.passed && random.passed && relative.passed && idle.passed
};
console.log(JSON.stringify(result, null, 2));
fs.writeFileSync(path.join(path.dirname(new URL(import.meta.url).pathname), 'bot-results.json'), JSON.stringify(result, null, 2));
await browser.close();
await server.close();
if (!result.passed) process.exitCode = 1;
