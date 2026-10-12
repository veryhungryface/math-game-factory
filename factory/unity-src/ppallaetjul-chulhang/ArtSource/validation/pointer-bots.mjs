import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/ppallaetjul-chulhang');
const TRIALS = 200;
const CHANCE = 1 / 7; // 평행선을 무작위로 하나 고르는 보수적 우연 수준.
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome',
    ]) {
      const candidate = path.join(base, build, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
}

function artifactHash() {
  const files = [];
  (function walk(dir) {
    for (const name of fs.readdirSync(dir).sort()) {
      const file = path.join(dir, name);
      fs.statSync(file).isDirectory() ? walk(file) : files.push(file);
    }
  })(GAME);
  const hash = crypto.createHash('sha256');
  for (const file of files) {
    hash.update(path.relative(GAME, file));
    hash.update(fs.readFileSync(file));
  }
  return hash.digest('hex');
}

function randomSource(seed) {
  let x = seed >>> 0;
  return () => {
    x ^= x << 13; x ^= x >>> 17; x ^= x << 5;
    return (x >>> 0) / 4294967296;
  };
}

function wilson95(correct, total) {
  if (!total) return [0, 0];
  const z = 1.959963984540054;
  const p = correct / total;
  const d = 1 + z * z / total;
  const center = (p + z * z / (2 * total)) / d;
  const half = z * Math.sqrt(p * (1 - p) / total + z * z / (4 * total * total)) / d;
  return [center - half, center + half];
}

function binomialTail(correct, total, chance) {
  if (!total) return 1;
  let probability = Math.pow(1 - chance, total), cdfBelow = 0;
  for (let k = 0; k < correct; k++) {
    cdfBelow += probability;
    probability *= ((total - k) / (k + 1)) * (chance / (1 - chance));
  }
  return Math.max(0, Math.min(1, 1 - cdfBelow));
}

// Unity 화면 좌표는 아래가 원점이지만 Puppeteer는 위가 원점이다.
// 390×844에서 A=(.5,.665), B=(.11,.275), C=(.89,.275)인 실제 돛 눈금 중심을 쓴다.
function tickPoint(side, tick) {
  const a = [195, (1 - .665) * 844];
  const end = side === 0 ? [.11 * 390, (1 - .275) * 844] : [.89 * 390, (1 - .275) * 844];
  const t = tick / 8;
  return [a[0] + (end[0] - a[0]) * t, a[1] + (end[1] - a[1]) * t];
}

async function cut(page, i, j) {
  const a = tickPoint(0, i), b = tickPoint(1, j);
  await page.mouse.move(a[0], a[1]);
  await page.mouse.down();
  await sleep(20);
  await page.mouse.move(b[0], b[1], { steps: 8 });
  await sleep(20);
  await page.mouse.up();
}

const server = await serveStatic(PUBLIC);
const launch = () => puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'],
});

async function openPage(browser, label) {
  const page = await browser.newPage();
  const errors = [];
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/ppallaetjul-chulhang/?pointerBot=${label}-${Date.now()}`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  return { page, errors };
}

async function run(kind) {
  const browser = await launch();
  try {
    const { page, errors } = await openPage(browser, kind);
    const random = randomSource(20261013);
    let correct = 0, total = 0, completed = 0;
    const inputs = Array(7).fill(0);
    for (let trial = 0; trial < TRIALS; trial++) {
      await page.evaluate(() => window.__GAME_TEST__.start());
      await sleep(34);
      let i, j;
      if (kind === 'fixed') i = j = 4;
      else if (kind === 'cycle') i = j = trial % 7 + 1;
      else { i = Math.floor(random() * 7) + 1; j = Math.floor(random() * 7) + 1; }
      if (i === j) inputs[i - 1]++;
      await cut(page, i, j);
      await page.waitForFunction(() => window.__GAME_TEST__.getState().firstAttemptTotal === 1, { timeout: 3000 });
      const state = await page.evaluate(() => window.__GAME_TEST__.getState());
      total += state.firstAttemptTotal;
      correct += state.firstAttemptCorrect;
      if (state.phase === 'clear') completed++;
    }
    const interval = wilson95(correct, total);
    const pValueAboveChance = binomialTail(correct, total, CHANCE);
    return {
      games: TRIALS,
      firstAttemptCorrect: correct,
      firstAttemptTotal: total,
      firstAttemptRate: correct / total,
      wilson95: interval,
      pValueAboveChance,
      chance: CHANCE,
      completionRate: completed / TRIALS,
      parallelInputHistogram: inputs,
      consoleErrors: errors,
      statisticallyAboveChance: correct / total > CHANCE && pValueAboveChance < .05,
      passed: total === TRIALS && pValueAboveChance >= .05 && errors.length === 0,
    };
  } finally {
    await browser.close();
  }
}

const fixed = await run('fixed');
console.log('fixed', JSON.stringify(fixed));
const cycle = await run('cycle');
console.log('cycle', JSON.stringify(cycle));
const random = await run('random');
console.log('random', JSON.stringify(random));

const idleBrowser = await launch();
let idleProgressed = 0, idleErrors = [];
try {
  const opened = await openPage(idleBrowser, 'idle');
  idleErrors = opened.errors;
  for (let trial = 0; trial < TRIALS; trial++) {
    const changed = await opened.page.evaluate(async () => {
      window.__GAME_TEST__.start();
      const before = window.__GAME_TEST__.getState();
      await new Promise(resolve => setTimeout(resolve, 34));
      const after = window.__GAME_TEST__.getState();
      return after.solved !== before.solved || after.firstAttemptTotal !== before.firstAttemptTotal;
    });
    if (changed) idleProgressed++;
  }
  await opened.page.close();
} finally {
  await idleBrowser.close();
}
const idle = {
  games: TRIALS,
  firstAttemptCorrect: 0,
  firstAttemptTotal: 0,
  firstAttemptRate: 0,
  wilson95: [0, 0],
  chance: CHANCE,
  completionRate: 0,
  progressed: idleProgressed,
  consoleErrors: idleErrors,
  passed: idleProgressed === 0 && idleErrors.length === 0,
};

const result = {
  runId: `ppallaetjul-chulhang-pointer-bots-${new Date().toISOString()}`,
  artifactHashSha256: artifactHash(),
  criterion: '정답을 읽지 않는 실제 포인터 첫 절단 200회씩: 단측 이항검정에서 1/7을 유의하게 초과하지 않음(p≥.05), 무입력 진도 0',
  method: '390×844 Chrome 실제 canvas mouse down/move/up. start()는 각 독립 시행 초기화에만 사용하며 answerCorrect/answerWrong은 호출하지 않는다. fixed=(4,4), cycle=(1,1)…(7,7), random=두 변 눈금 독립 균등.',
  fixed, cycle, random, idle,
  passed: fixed.passed && cycle.passed && random.passed && idle.passed,
};
console.log(JSON.stringify(result, null, 2));
fs.writeFileSync(new URL('./bot-results.json', import.meta.url), JSON.stringify(result, null, 2));
await server.close();
if (!result.passed) process.exitCode = 1;
