import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/dalpaengi-geukjang');
const VIEW = { width: 390, height: 844 };
const TRIALS = 200;
const CHANCE = 1 / 16;
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome'
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
}

function xorshift(seed) {
  let x = seed >>> 0;
  return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; };
}

function artifactHash() {
  const hash = crypto.createHash('sha256');
  const rels = ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(name => `Build/${name}`)];
  for (const rel of rels) { hash.update(rel); hash.update(fs.readFileSync(path.join(GAME, rel))); }
  return hash.digest('hex');
}

function wilson(correct, total) {
  if (!total) return [0, 0];
  const z = 1.959963984540054, p = correct / total, d = 1 + z * z / total;
  const mid = (p + z * z / (2 * total)) / d;
  const half = z * Math.sqrt((p * (1 - p) + z * z / (4 * total)) / total) / d;
  return [Math.max(0, mid - half), Math.min(1, mid + half)];
}

async function submitSeat(page, target) {
  // Unity는 좌하단 원점. 시작 손잡이 y=143px, 한 자리당 위로 24px이다.
  const x = VIEW.width * .5;
  const startTop = VIEW.height - VIEW.height * .17;
  const targetTop = startTop - (Math.max(2, Math.min(17, target)) - 2) * 24;
  await page.mouse.move(x, startTop);
  await page.mouse.down();
  await sleep(28);
  await page.mouse.move(x, targetTop, { steps: 4 });
  await sleep(180);
  await page.mouse.up();
}

const artifactHashBefore = artifactHash();
const server = await serveStatic(PUBLIC);
const launch = () => puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']
});

async function openPage(browser) {
  const page = await browser.newPage(), errors = [];
  await page.setViewport({ ...VIEW, deviceScaleFactor: 1 });
  page.on('console', msg => { if (msg.type() === 'error') errors.push(msg.text()); });
  page.on('pageerror', err => errors.push(String(err)));
  await page.goto(`${server.url}/g/dalpaengi-geukjang/?b3=${Date.now()}`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  return { page, errors };
}

async function runPolicy(kind) {
  const browser = await launch();
  try {
    const { page, errors } = await openPage(browser);
    const random = xorshift(20261020);
    let firstCorrect = 0, firstTotal = 0, completed = 0;
    for (let trial = 0; trial < TRIALS; trial++) {
      await page.evaluate(() => window.__GAME_TEST__.start());
      await sleep(24);
      const guess = kind === 'repeat' ? 2 : kind === 'cycle' ? 2 + (trial % 4) : 2 + Math.floor(random() * 16);
      await submitSeat(page, guess);
      await page.waitForFunction(() => window.__GAME_TEST__.getState().firstAttempts === 1, { timeout: 3000 });
      const state = await page.evaluate(() => window.__GAME_TEST__.getState());
      firstTotal += state.firstAttempts;
      firstCorrect += state.firstCorrect;
      if (state.phase === 'clear') completed++;
    }
    const rate = firstCorrect / firstTotal;
    return {
      games: TRIALS,
      firstAttemptCorrect: firstCorrect,
      firstAttemptTotal: firstTotal,
      firstAttemptRate: rate,
      chance: CHANCE,
      wilson95: wilson(firstCorrect, firstTotal),
      completed,
      completionRate: completed / TRIALS,
      consoleErrors: errors,
      passed: firstTotal === TRIALS && rate <= CHANCE && errors.length === 0
    };
  } finally { await browser.close(); }
}

const repeat = await runPolicy('repeat'); console.log('repeat', JSON.stringify(repeat));
const cycle = await runPolicy('cycle'); console.log('cycle', JSON.stringify(cycle));
const random = await runPolicy('random'); console.log('random', JSON.stringify(random));

const idleBrowser = await launch();
let idleProgressed = 0, idleErrors = [];
try {
  const opened = await openPage(idleBrowser), page = opened.page; idleErrors = opened.errors;
  for (let trial = 0; trial < TRIALS; trial++) {
    const before = await page.evaluate(() => { window.__GAME_TEST__.start(); return window.__GAME_TEST__.getState(); });
    await sleep(24);
    const after = await page.evaluate(() => window.__GAME_TEST__.getState());
    if (after.solved !== before.solved || after.firstAttempts !== before.firstAttempts) idleProgressed++;
  }
  await page.close();
} finally { await idleBrowser.close(); }
const idle = {
  games: TRIALS,
  firstAttemptCorrect: 0,
  firstAttemptTotal: 0,
  firstAttemptRate: 0,
  chance: CHANCE,
  completed: 0,
  completionRate: 0,
  progressed: idleProgressed,
  consoleErrors: idleErrors,
  passed: idleProgressed === 0 && idleErrors.length === 0
};

const artifactHashAfter = artifactHash();
const runId = `dalpaengi-geukjang-b3-${new Date().toISOString()}`;
const result = {
  runId,
  run_id: runId,
  artifactHashSha256: artifactHashBefore,
  artifact_hash: artifactHashBefore,
  artifact_hash_before: artifactHashBefore,
  artifact_hash_after: artifactHashAfter,
  criterion: '각 200판 첫 시도 정답률이 1/16 이하이고 무입력 진도가 0',
  method: '390×844 Chrome의 실제 canvas mouse down/move/up. 매 판 start로 독립 본판을 만든 뒤 첫 홀드·드래그·릴리스만 측정한다. 정책은 문제·prompt·answer·문제 id를 읽지 않는다.',
  repeat, cycle, random, idle,
  passed: repeat.passed && cycle.passed && random.passed && idle.passed && artifactHashBefore === artifactHashAfter
};
console.log(JSON.stringify(result, null, 2));
fs.writeFileSync(new URL('./b3-results.json', import.meta.url), JSON.stringify(result, null, 2) + '\n');
await server.close();
if (!result.passed) process.exitCode = 1;
