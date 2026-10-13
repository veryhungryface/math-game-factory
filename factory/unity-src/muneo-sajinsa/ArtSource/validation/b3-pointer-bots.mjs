#!/usr/bin/env node
// B3: 발문·정답을 읽지 않는 네 정책을 실제 Chrome mouse down/move/up으로 각각 200판 검증한다.
// start()는 타이틀을 건너뛰는 데만 쓰며 answerCorrect()/answerWrong()은 호출하지 않는다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/muneo-sajinsa');
const TRIALS = +(process.env.TRIALS || 200);
const W = 390, H = 844, CHANCE = 24 / 256; // 4!/4^4 = 9.375%, K=U=4를 유리하게 가정한 상한
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing', 'chrome-linux64/chrome'
  ]) { const p = path.join(base, build, rel); if (fs.existsSync(p)) return p; }
  throw new Error('Chrome for Testing not found');
}

function artifactHash() {
  const h = crypto.createHash('sha256');
  for (const n of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(x => 'Build/' + x)]) h.update(fs.readFileSync(path.join(GAME, n)));
  return h.digest('hex');
}

function rng(seed) { let x = seed >>> 0; return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; }; }
const xPositions = [55, 148, 242, 336];
const sourceY = [540, 616];
const slot = [[148, 354], [242, 354]];
const camera = [328, 388];
const tear = [359, 768];

async function drag(page, a, b) {
  await page.mouse.move(a[0], a[1]); await page.mouse.down(); await sleep(18);
  await page.mouse.move(b[0], b[1], {steps: 2}); await sleep(18); await page.mouse.up(); await sleep(18);
}
async function tap(page, p) { await page.mouse.move(p[0], p[1]); await page.mouse.down(); await sleep(18); await page.mouse.up(); await sleep(18); }
async function onePhotoAttempt(page, group, xIndex) { await drag(page, [xPositions[xIndex], sourceY[group]], slot[group]); await tap(page, camera); }
async function submit(page) { await drag(page, tear, [tear[0], tear[1] - 92]); await sleep(36); }

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({headless: true, executablePath: chromePath(), protocolTimeout: 300000, args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']});

async function openPage() {
  const page = await browser.newPage(), errors = [];
  await page.setViewport({width: W, height: H, deviceScaleFactor: 1});
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', e => errors.push(String(e)));
  await page.goto(`${server.url}/g/muneo-sajinsa/`, {waitUntil: 'load', timeout: 45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  return {page, errors};
}

async function run(policy) {
  const {page, errors} = await openPage(); const random = rng(0x20261014 ^ policy.length);
  let correct = 0, submitted = 0, completed = 0;
  for (let trial = 0; trial < TRIALS; trial++) {
    await page.evaluate(() => window.__GAME_TEST__.start()); await sleep(28);
    for (let k = 0; k < 4; k++) {
      let group, x;
      if (policy === 'repeat') { group = 0; x = 1; }
      else if (policy === 'cycle') { const q = k % 4; group = q < 2 ? 0 : 1; x = q; }
      else { group = random() < .5 ? 0 : 1; x = Math.floor(random() * 4); }
      await onePhotoAttempt(page, group, x);
    }
    await submit(page);
    let s = await page.evaluate(() => window.__GAME_TEST__.getState());
    if (s.firstAttemptTotal < 1) { await sleep(70); s = await page.evaluate(() => window.__GAME_TEST__.getState()); }
    if (s.firstAttemptTotal > 0) submitted++;
    if (s.firstAttemptCorrect > 0) correct++;
    if (s.phase === 'clear' || s.phase === 'practice_complete') completed++;
  }
  await page.close();
  return {games: TRIALS, submitted, firstAttemptCorrect: correct, firstAttemptRate: correct / TRIALS, chance: CHANCE, completionRate: completed / TRIALS, consoleErrors: errors, passed: correct / TRIALS <= CHANCE && errors.length === 0};
}

try {
  const repeat = await run('repeat'); console.log('repeat', repeat.firstAttemptRate);
  const cycle = await run('cycle'); console.log('cycle', cycle.firstAttemptRate);
  const random = await run('random'); console.log('random', random.firstAttemptRate);
  const {page, errors} = await openPage(); let progressed = 0;
  for (let i = 0; i < TRIALS; i++) {
    const [a, b] = await page.evaluate(async () => { window.__GAME_TEST__.start(); const a = window.__GAME_TEST__.getState().solved; await new Promise(r => setTimeout(r, 34)); return [a, window.__GAME_TEST__.getState().solved]; });
    if (b !== a) progressed++;
  }
  await page.close();
  const idle = {games: TRIALS, submitted: 0, firstAttemptCorrect: 0, firstAttemptRate: 0, chance: CHANCE, completionRate: 0, progressed, consoleErrors: errors, passed: progressed === 0 && errors.length === 0};
  const result = {
    runId: `muneo-sajinsa-pointer-bots-${new Date().toISOString()}`,
    artifactHashSha256: artifactHash(),
    criterion: '각 200판 최초 필름 제출 정답률이 4!/4^4=9.375% 이하, 무입력 진도 0',
    method: '실제 WebGL + trusted Puppeteer mouse events. 각 판은 raw 좌표 정책으로 네 번의 소품 드래그/렌즈 탭 뒤 필름을 절취한다. 정답·발문·소품 의미를 읽지 않고 answer hooks는 호출하지 않는다.',
    repeat, cycle, random, idle
  };
  result.passed = repeat.passed && cycle.passed && random.passed && idle.passed;
  fs.writeFileSync(path.join(HERE, 'b3-pointer-results.json'), JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify(result, null, 2)); if (!result.passed) process.exitCode = 1;
} finally { await browser.close(); await server.close(); }
