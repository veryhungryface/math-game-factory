import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/kkul-doduk');
const TRIALS = 200;
const VIEW = { width: 390, height: 844 };
const RANDOM_CHANCE = 1 / 24;
// 기획서의 단계별 답 사전분포 상한: (합 2문항×1/9 + 나머지 6문항×1/12) / 8.
const PRIOR_CHANCE = (2 / 9 + 6 / 12) / 8;
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
  const files = [];
  (function walk(dir) {
    for (const name of fs.readdirSync(dir).sort()) {
      const file = path.join(dir, name);
      fs.statSync(file).isDirectory() ? walk(file) : files.push(file);
    }
  })(GAME);
  const hash = crypto.createHash('sha256');
  for (const file of files) { hash.update(path.relative(GAME, file)); hash.update(fs.readFileSync(file)); }
  return hash.digest('hex');
}

function wilson(correct, total) {
  if (!total) return [0, 0];
  const z = 1.959963984540054, p = correct / total, d = 1 + z * z / total;
  const mid = (p + z * z / (2 * total)) / d;
  const half = z * Math.sqrt((p * (1 - p) + z * z / (4 * total)) / total) / d;
  return [Math.max(0, mid - half), Math.min(1, mid + half)];
}

function valuePoint(value) {
  const t = (Math.max(1, Math.min(24, value)) - 1) / 23;
  // Unity 포인터 원점은 좌하단, Puppeteer는 좌상단이다.
  return { x: VIEW.width * .5, y: VIEW.height - VIEW.height * (.22 + .45 * t) };
}

async function cutAt(page, currentValue, targetValue) {
  const from = valuePoint(currentValue), target = valuePoint(targetValue);
  await page.mouse.move(from.x, from.y);
  await page.mouse.down();
  await sleep(18);
  await page.mouse.move(target.x, target.y, { steps: 4 });
  await sleep(90); // 50ms 눈금 안정 조건을 실제 시간으로 넘긴다.
  await page.mouse.move(target.x + 82, target.y, { steps: 2 });
  await sleep(14);
  await page.mouse.up();
}

const server = await serveStatic(PUBLIC);
const launch = () => puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']
});

async function openPage(browser) {
  const page = await browser.newPage(), errors = [];
  await page.setViewport({ ...VIEW, deviceScaleFactor: 1 });
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/kkul-doduk/?b3=${Date.now()}`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  return { page, errors };
}

async function run(kind) {
  const browser = await launch();
  try {
    const { page, errors } = await openPage(browser);
    const random = xorshift(2);
    let firstCorrect = 0, firstTotal = 0, completed = 0;
    for (let trial = 0; trial < TRIALS; trial++) {
      await page.evaluate(() => window.__GAME_TEST__.start());
      await sleep(34);
      const before = await page.evaluate(() => window.__GAME_TEST__.getState());
      // 입력 정책은 문제·prompt·answer를 읽지 않는다. 상태에서 읽는 것은 화면에 그대로
      // 표시되는 현재 손잡이 눈금뿐이며, 이는 실제 손잡이를 잡기 위한 좌표다.
      const guess = kind === 'fixed' ? 1 : kind === 'cycle' ? 1 + (trial % 4) : 1 + Math.floor(random() * 24);
      await cutAt(page, before.selectedCellCount, guess);
      await page.waitForFunction(() => window.__GAME_TEST__.getState().firstAttempted === 1, { timeout: 3000 });
      const after = await page.evaluate(() => {
        const s = window.__GAME_TEST__.getState();
        return { firstAttempted: s.firstAttempted, firstCorrect: s.firstCorrect, phase: s.phase };
      });
      firstTotal += after.firstAttempted;
      firstCorrect += after.firstCorrect;
      if (after.phase === 'clear') completed++;
    }
    const rate = firstTotal ? firstCorrect / firstTotal : 0;
    const chance = kind === 'random' ? RANDOM_CHANCE : PRIOR_CHANCE;
    const ci = wilson(firstCorrect, firstTotal);
    await page.close();
    return {
      games: TRIALS,
      firstAttemptCorrect: firstCorrect,
      firstAttemptTotal: firstTotal,
      firstAttemptRate: rate,
      chance,
      wilson95: ci,
      completed,
      completionRate: completed / TRIALS,
      consoleErrors: errors,
      passed: firstTotal === TRIALS && rate <= chance && errors.length === 0
    };
  } finally { await browser.close(); }
}

const fixed = await run('fixed'); console.log('fixed', JSON.stringify(fixed));
const cycle = await run('cycle'); console.log('cycle', JSON.stringify(cycle));
const random = await run('random'); console.log('random', JSON.stringify(random));

const idleBrowser = await launch();
let idleProgressed = 0, idleErrors = [];
try {
  const opened = await openPage(idleBrowser), page = opened.page; idleErrors = opened.errors;
  for (let trial = 0; trial < TRIALS; trial++) {
    const changed = await page.evaluate(async () => {
      window.__GAME_TEST__.start();
      const a = window.__GAME_TEST__.getState();
      await new Promise(resolve => setTimeout(resolve, 34));
      const b = window.__GAME_TEST__.getState();
      return b.solved !== a.solved || b.firstAttempted !== a.firstAttempted;
    });
    if (changed) idleProgressed++;
  }
  await page.close();
} finally { await idleBrowser.close(); }
const idle = {
  games: TRIALS, firstAttemptCorrect: 0, firstAttemptTotal: 0, firstAttemptRate: 0,
  chance: RANDOM_CHANCE, completionRate: 0, progressed: idleProgressed, consoleErrors: idleErrors,
  passed: idleProgressed === 0 && idleErrors.length === 0
};

const result = {
  runId: `kkul-doduk-pointer-bots-${new Date().toISOString()}`,
  artifactHashSha256: artifactHash(),
  criterion: '각 200판 원시 첫 시도 정답률이 정책별 우연 수준 이하, 무입력 진도 0',
  method: '390×844 Chrome의 실제 canvas mouse down/move/up. 매 판 start로 새 본판을 만든 뒤 첫 절단만 측정한다. fixed=1, cycle=1→2→3→4, random=xorshift seed 2의 1~24 균등값. answer·prompt·문제 id·answerCorrect/answerWrong을 읽거나 호출하지 않는다.',
  fixed, cycle, random, idle,
  passed: fixed.passed && cycle.passed && random.passed && idle.passed
};
console.log(JSON.stringify(result, null, 2));
fs.writeFileSync(new URL('./b3-results.json', import.meta.url), JSON.stringify(result, null, 2) + '\n');
await server.close();
if (!result.passed) process.exitCode = 1;
