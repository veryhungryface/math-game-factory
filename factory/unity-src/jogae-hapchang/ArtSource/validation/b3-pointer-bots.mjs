#!/usr/bin/env node
// B3: 문제·발문·정답을 읽지 않는 네 정책을 실제 WebGL mouse down/move/up으로 각각 200판 측정한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/jogae-hapchang');
const VIEW = { width: 390, height: 844 };
const TRIALS = +(process.env.TRIALS || 200);
const CHANCE = 1 / 16;
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome'
  ]) { const p = path.join(base, build, rel); if (fs.existsSync(p)) return p; }
  throw new Error('Chrome for Testing not found');
}
function xorshift(seed) { let x = seed >>> 0; return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; }; }
function artifactHash() {
  const hash = crypto.createHash('sha256');
  for (const rel of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(n => `Build/${n}`)]) { hash.update(rel); hash.update(fs.readFileSync(path.join(GAME, rel))); }
  return hash.digest('hex');
}
function rotate(x, y, degrees) { const a = degrees * Math.PI / 180, c = Math.cos(a), s = Math.sin(a); return [x * c - y * s, x * s + y * c]; }
async function submitSlot(page, slot) {
  const s = await page.evaluate(() => window.__GAME_TEST__.getState());
  const bx = s.anchorX - s.pivotX, by = s.anchorY - s.pivotY, bl = Math.hypot(bx, by);
  const radius = Math.hypot(s.handleX - s.pivotX, s.handleY - s.pivotY);
  const [rx, ry] = rotate(bx / bl * radius, by / bl * radius, 10 + slot * 5);
  await page.mouse.move(s.handleX, VIEW.height - s.handleY); await page.mouse.down(); await sleep(34);
  await page.mouse.move(s.pivotX + rx, VIEW.height - (s.pivotY + ry), { steps: 6 }); await sleep(34); await page.mouse.up();
}
async function open(browser) {
  const page = await browser.newPage(), errors = [];
  await page.setViewport({ ...VIEW, deviceScaleFactor: 1 });
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); }); page.on('pageerror', e => errors.push(String(e)));
  await page.goto(`${server.url}/g/jogae-hapchang/?b3=${Date.now()}`, { waitUntil: 'domcontentloaded', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  return { page, errors };
}
async function runPolicy(kind) {
  const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
  try {
    const { page, errors } = await open(browser), random = xorshift(20261030);
    let firstCorrect = 0, firstTotal = 0, completed = 0;
    for (let trial = 0; trial < TRIALS; trial++) {
      await page.evaluate(() => window.__GAME_TEST__.start());
      await sleep(34);
      const slot = kind === 'repeat' ? 0 : kind === 'cycle' ? trial % 16 : Math.floor(random() * 16);
      await submitSlot(page, slot);
      await page.waitForFunction(() => window.__GAME_TEST__.getState().firstAttempted === 1, { timeout: 3000 });
      const st = await page.evaluate(() => window.__GAME_TEST__.getState());
      firstTotal += st.firstAttempted; firstCorrect += st.firstCorrect; if (st.phase === 'clear') completed++;
      if ((trial + 1) % 50 === 0) console.log(`${kind} ${trial + 1}/${TRIALS} correct=${firstCorrect}`);
    }
    const rate = firstTotal ? firstCorrect / firstTotal : 0;
    return { policy: kind, games: TRIALS, firstAttemptCorrect: firstCorrect, firstAttemptTotal: firstTotal, firstAttemptRate: rate, chance: CHANCE, completed, completionRate: completed / TRIALS, consoleErrors: errors, passed: firstTotal === TRIALS && rate <= CHANCE && errors.length === 0 };
  } finally { await browser.close(); }
}

const artifactHashBefore = artifactHash();
const server = await serveStatic(PUBLIC);
const repeat = await runPolicy('repeat');
const cycle = await runPolicy('cycle');
const random = await runPolicy('random');
const idleBrowser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
let idleErrors, progressed = 0;
try {
  const opened = await open(idleBrowser), page = opened.page; idleErrors = opened.errors;
  for (let trial = 0; trial < TRIALS; trial++) {
    const before = await page.evaluate(() => { window.__GAME_TEST__.start(); return window.__GAME_TEST__.getState(); });
    await sleep(50); const after = await page.evaluate(() => window.__GAME_TEST__.getState());
    if (after.solved !== before.solved || after.firstAttempted !== before.firstAttempted) progressed++;
  }
  await page.close();
} finally { await idleBrowser.close(); }
const idle = { policy: 'idle', games: TRIALS, firstAttemptCorrect: 0, firstAttemptTotal: 0, firstAttemptRate: 0, chance: CHANCE, completed: 0, completionRate: 0, progressed, consoleErrors: idleErrors, passed: progressed === 0 && idleErrors.length === 0 };
await server.close();
const report = {
  runId: `jogae-hapchang-b3-${new Date().toISOString()}`,
  artifactHashSha256: artifactHashBefore,
  criterion: '각 정책 200판의 첫 유효 릴리스 정답률이 1/16 이하이며 무입력 진도는 0',
  method: '390×844 실제 canvas mouse down/move/up. 각 판 start 뒤 발문·answer·problemId를 읽지 않고 repeat=10° 연타, cycle=16눈금 순환, random=16눈금 균등 입력을 한 번 제출한다. idle은 200회 새 판에서 입력 없이 진도 불변을 확인한다.',
  chance: CHANCE, repeat, cycle, random, idle,
  passed: repeat.passed && cycle.passed && random.passed && idle.passed && artifactHashBefore === artifactHash()
};
fs.writeFileSync(new URL('./b3-pointer-results.json', import.meta.url), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify(report, null, 2));
if (!report.passed) process.exitCode = 1;
