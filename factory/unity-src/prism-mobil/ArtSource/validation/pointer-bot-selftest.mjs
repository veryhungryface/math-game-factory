import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';
import {acquireMachineLock} from '../../../../lib/machine-lock.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const TRIALS = 200;
const CHANCE = 1 / 24;
const sleep = ms => new Promise(r => setTimeout(r, ms));

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const b of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome'
    ]) {
      const p = path.join(base, b, rel);
      if (fs.existsSync(p)) return p;
    }
  }
  return undefined;
}

function randomSource(seed) {
  let x = seed >>> 0;
  return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; };
}

function artifactHash() {
  const dir = path.join(PUBLIC, 'g/prism-mobil');
  const files = [];
  (function walk(d) { for (const f of fs.readdirSync(d).sort()) { const p = path.join(d, f); fs.statSync(p).isDirectory() ? walk(p) : files.push(p); } })(dir);
  const h = crypto.createHash('sha256');
  for (const f of files) { h.update(path.relative(dir, f)); h.update(fs.readFileSync(f)); }
  return h.digest('hex');
}

const release = await acquireMachineLock('gpu', {label: 'prism-mobil 무뇌 봇'});
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({headless: true, executablePath: resolveChrome(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']});

async function openPage() {
  const page = await browser.newPage();
  const errors = [];
  await page.setViewport({width: 390, height: 844, deviceScaleFactor: 1});
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', e => errors.push(String(e)));
  await page.goto(`${server.url}/g/prism-mobil/`, {waitUntil: 'load', timeout: 45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  return {page, errors};
}

async function pointerGuess(page, guess) {
  // 세로 레이아웃의 실제 AB 눈금: A=Stage(.50,.90), B=Stage(.10,.10).
  // 정답 훅을 부르지 않고 화면에 보이는 기울어진 선분 위 한 눈금을 직접 누른다.
  const t = guess / 24;
  const nx = 0.50 + (0.18 - 0.50) * t;
  const ny = 0.675 + (0.315 - 0.675) * t;
  const x = nx * 390;
  const y = (1 - ny) * 844;
  await page.mouse.move(x, y);
  await page.mouse.down();
  await sleep(16);
  await page.mouse.move(x + 3, y - 2);
  await sleep(16);
  await page.mouse.up();
  await sleep(24);
}

async function run(kind) {
  const {page, errors} = await openPage();
  const random = randomSource(0x50a17e11);
  let correct = 0;
  const firstAttempts = [];
  for (let i = 0; i < TRIALS; i++) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    await sleep(30);
    // B3 순환 봇은 1→2→3→4의 네 입력을 반복한다. 연속 눈금에서는 네 구간 대표점으로 대응시킨다.
    const cycleGuess = [1, 7, 13, 19][i % 4];
    const guess = kind === 'fixed' ? 1 : kind === 'cycle' ? cycleGuess : Math.floor(random() * 24) + 1;
    let s;
    for (let retry = 0; retry < 3; retry++) {
      await pointerGuess(page, guess);
      s = await page.evaluate(() => window.__GAME_TEST__.getState());
      if (s.firstAttemptTotal === 1) break;
      await sleep(40);
    }
    if (s.firstAttemptTotal !== 1) throw new Error(`${kind} trial ${i}: pointer did not submit ${JSON.stringify(s)}`);
    const ok = s.firstAttemptCorrect === 1;
    correct += ok ? 1 : 0;
    firstAttempts.push(ok);
  }
  await page.close();
  // 실제 게임은 첫 시도 실패 2회면 즉시 종료한다. 측정열을 7문항 묶음으로 재생해 완주율도 함께 기록한다.
  let sessions = 0, completed = 0;
  for (let i = 0; i + 6 < firstAttempts.length; i += 7) {
    sessions++; const seven = firstAttempts.slice(i, i + 7); if (seven.filter(Boolean).length >= 6) completed++;
  }
  return {trials: TRIALS, correct, firstTryRate: correct / TRIALS, chance: CHANCE,
    modeledSessions: sessions, completed, completionRate: sessions ? completed / sessions : 0,
    consoleErrors: errors, passed: correct / TRIALS <= CHANCE + 0.015 && errors.length === 0};
}

const fixed = await run('fixed');
const cycle = await run('cycle');
const random = await run('random');
const {page: idlePage, errors: idleErrors} = await openPage();
let idleProgressed = 0;
for (let i = 0; i < TRIALS; i++) {
  const pair = await idlePage.evaluate(async () => {
    window.__GAME_TEST__.start(); const a = window.__GAME_TEST__.getState();
    await new Promise(r => setTimeout(r, 30)); const b = window.__GAME_TEST__.getState();
    return [a.solved, b.solved];
  });
  if (pair[1] !== pair[0]) idleProgressed++;
}
await idlePage.close();
const idle = {trials: TRIALS, correct: 0, firstTryRate: 0, chance: CHANCE, completionRate: 0,
  progressed: idleProgressed, consoleErrors: idleErrors, passed: idleProgressed === 0 && idleErrors.length === 0};

const result = {
  run_id: `prism-mobil-pointer-bots-${new Date().toISOString()}`,
  artifact_hash_sha256: artifactHash(),
  criterion: 'first-try rate at/below 1/24 chance within 1.5 percentage-point finite-sample tolerance; idle progress 0',
  method: 'Chrome CDP mouse down/move/up on Unity canvas for fixed/cycle/random; 200 isolated first-attempt trials each. No answer read and no answerCorrect/answerWrong call.',
  fixed, cycle, random, idle,
  passed: fixed.passed && cycle.passed && random.passed && idle.passed
};
console.log(JSON.stringify(result, null, 2));
fs.writeFileSync(path.join(path.dirname(new URL(import.meta.url).pathname), 'bot-results.json'), JSON.stringify(result, null, 2));
await browser.close(); await server.close(); release();
if (!result.passed) process.exitCode = 1;
