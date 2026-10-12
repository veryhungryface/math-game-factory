import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/ppallaetjul-chulhang');
const OUT = new URL('./playthrough/', import.meta.url);
fs.mkdirSync(OUT, { recursive: true });
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    ]) {
      const file = path.join(base, build, rel);
      if (fs.existsSync(file)) return file;
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
  for (const file of files) { hash.update(path.relative(GAME, file)); hash.update(fs.readFileSync(file)); }
  return hash.digest('hex');
}

function tickPoint(side, tick) {
  const a = [195, (1 - .665) * 844];
  const end = side === 0 ? [.11 * 390, (1 - .275) * 844] : [.89 * 390, (1 - .275) * 844];
  const t = tick / 8;
  return [a[0] + (end[0] - a[0]) * t, a[1] + (end[1] - a[1]) * t];
}

async function cut(page, i, j, log, label) {
  const from = tickPoint(0, i), to = tickPoint(1, j);
  await page.mouse.move(from[0], from[1]);
  await page.mouse.down();
  await sleep(30);
  await page.mouse.move(to[0], to[1], { steps: 12 });
  await sleep(30);
  await page.mouse.up();
  log.push({ label, type: 'pointer-drag', from, to, ticks: [i, j] });
}

function targetTick(state) {
  const parts = state.problemId.split('-');
  const tick = Number(parts[3]);
  if (!Number.isInteger(tick) || tick < 1 || tick > 7) throw new Error(`정답 눈금을 읽을 수 없음: ${state.problemId}`);
  return tick;
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function shot(page, name) { await page.screenshot({ path: new URL(name, OUT).pathname }); }

async function enterPractice(page, log) {
  for (const point of [[195, 717], [195, 790], [195, 755]]) {
    await page.mouse.click(point[0], point[1]);
    log.push({ label: 'title-start', type: 'pointer-tap', at: point });
    await sleep(150);
    if ((await state(page)).onboarding) return;
  }
  throw new Error('타이틀의 실제 시작 히트 영역을 누르지 못함');
}

async function clearPractice(page, log, label) {
  await cut(page, 4, 4, log, `${label}-practice`);
  await sleep(1700);
  const after = await state(page);
  if (after.onboarding || after.firstAttemptTotal !== 0) throw new Error(`연습 게이트 실패: ${JSON.stringify(after)}`);
}

async function solveRun(page, log, { wrongFirst, prefix }) {
  let s = await state(page);
  if (wrongFirst) {
    const k = targetTick(s), wrong = k === 1 ? 2 : 1;
    await cut(page, wrong, wrong, log, `${prefix}-first-wrong`);
    await sleep(1400);
    s = await state(page);
    await cut(page, k, k, log, `${prefix}-first-repair`);
    await sleep(1700);
  }
  while ((s = await state(page)).phase === 'playing' && s.solved < 9) {
    const k = targetTick(s);
    await cut(page, k, k, log, `${prefix}-order-${s.solved + 1}`);
    await sleep(1700);
  }
  return state(page);
}

async function restart(page, log) {
  for (const point of [[195, 539], [195, 575], [195, 610], [195, 650]]) {
    await page.mouse.click(point[0], point[1]);
    log.push({ label: 'restart', type: 'pointer-tap', at: point });
    await sleep(150);
    const s = await state(page);
    if (s.phase === 'playing' && s.solved === 0 && !s.onboarding) return;
  }
  throw new Error('결과 화면의 실제 다시하기 히트 영역을 누르지 못함');
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: resolveChrome(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const page = await browser.newPage();
const errors = [], inputLog = [];
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
page.on('pageerror', error => errors.push(String(error)));
await page.goto(`${server.url}/g/ppallaetjul-chulhang/?playthrough=${Date.now()}`, { waitUntil: 'load', timeout: 45000 });
await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
await sleep(500);
await shot(page, '00-title.png');

await enterPractice(page, inputLog);
await shot(page, '01-practice.png');
await clearPractice(page, inputLog, 'run1');
await shot(page, '02-run1-first-order.png');
const run1 = await solveRun(page, inputLog, { wrongFirst: true, prefix: 'run1' });
await shot(page, '03-run1-clear-after-repair.png');

await restart(page, inputLog);
await shot(page, '04-run2-first-order.png');
const run2 = await solveRun(page, inputLog, { wrongFirst: false, prefix: 'run2' });
await shot(page, '05-run2-clear.png');

const result = {
  artifactHashSha256: artifactHash(),
  method: '타이틀→고정 연습→오답 1회와 직접 수리→9척 성공→결과 화면 실제 재시작→9척 성공을 390×844 Chrome canvas mouse down/move/up만으로 수행. __GAME_TEST__는 상태 관찰에만 사용.',
  run1,
  run2,
  inputLog,
  errors,
  passed: run1.phase === 'clear' && run1.solved === 9 && run1.lives === 2
    && run1.firstAttemptTotal === 9 && run1.firstAttemptCorrect === 8 && run1.finalBandFirst === 3
    && run2.phase === 'clear' && run2.solved === 9 && run2.lives === 3
    && run2.firstAttemptTotal === 9 && run2.firstAttemptCorrect === 9 && run2.finalBandFirst === 3
    && errors.length === 0,
};
console.log(JSON.stringify(result, null, 2));
fs.writeFileSync(new URL('./playthrough-results.json', import.meta.url), JSON.stringify(result, null, 2));
await browser.close();
await server.close();
if (!result.passed) process.exitCode = 1;
