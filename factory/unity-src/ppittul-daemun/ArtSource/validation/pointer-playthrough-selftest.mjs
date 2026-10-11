import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const HERE = path.dirname(new URL(import.meta.url).pathname);
const FRAMES = path.join(HERE, 'pointer-playthrough');
fs.mkdirSync(FRAMES, {recursive: true});
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

function artifactHash() {
  const dir = path.join(PUBLIC, 'g/ppittul-daemun');
  const files = [];
  (function walk(d) { for (const f of fs.readdirSync(d).sort()) { const p = path.join(d, f); fs.statSync(p).isDirectory() ? walk(p) : files.push(p); } })(dir);
  const h = crypto.createHash('sha256');
  for (const f of files) { h.update(path.relative(dir, f)); h.update(fs.readFileSync(f)); }
  return h.digest('hex');
}

function targetFromPrompt(prompt) {
  if (prompt.includes('직사각형이면서 마름모')) return 5;
  if (prompt.includes('두 대각선의 길이가 같')) return 5;
  if (prompt.includes('직사각형이 되도록')) return 5;
  const m = prompt.match(/∠([ABCD])=(\d+)°/);
  if (!m) throw new Error(`발문 해석 불가: ${prompt}`);
  const value = Number(m[2]);
  const angleA = m[1] === 'A' || m[1] === 'C' ? value : 180 - value;
  return (angleA - 40) / 10;
}

function xFor(detent) { return 390 * (.10 + .80 * detent / 10); }
const trackY = 844 * (1 - .245); // Puppeteer 좌표는 위가 0, 게임 좌표 track y는 아래에서 24.5%.

async function dragDetent(page, from, to) {
  await page.mouse.move(xFor(from), trackY);
  await page.mouse.down();
  await sleep(35);
  await page.mouse.move((xFor(from) + xFor(to)) * .5, trackY - 2, {steps: 3});
  await page.mouse.move(xFor(to), trackY, {steps: 4});
  await sleep(35);
  await page.mouse.up();
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitNext(page, oldId, oldSolved, timeout = 4000) {
  await page.waitForFunction((id, solved) => {
    const s = window.__GAME_TEST__.getState();
    return s.problemId !== id || s.solved > solved || s.phase === 'clear' || s.phase === 'gameover';
  }, {timeout}, oldId, oldSolved);
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({headless: true, executablePath: resolveChrome(), args: ['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
const page = await browser.newPage();
await page.setViewport({width: 390, height: 844, deviceScaleFactor: 1});
const errors = [];
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
page.on('pageerror', e => errors.push(String(e)));
await page.goto(`${server.url}/g/ppittul-daemun/`, {waitUntil: 'load', timeout: 45000});
await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
await page.screenshot({path: path.join(FRAMES, '00-title.png')});

// 타이틀 세계의 종끈을 실제 포인터로 당긴다.
await page.mouse.move(195, 844 * .47); await page.mouse.down();
await page.mouse.move(195, 844 * .30, {steps: 7}); await page.mouse.up();
await sleep(250);
await page.screenshot({path: path.join(FRAMES, '01-practice-a.png')});

// 고정 연습 2문도 화면 발문만 읽어 실제 손잡이로 푼다.
for (let practice = 0; practice < 2; practice++) {
  const s = await state(page); const target = targetFromPrompt(s.prompt);
  await dragDetent(page, s.selectedDetent, target);
  await sleep(1450);
}
await page.screenshot({path: path.join(FRAMES, '02-first-real.png')});

async function solveRun(makeOneMistake) {
  let mistakeDone = false, guard = 0;
  while (guard++ < 30) {
    const s = await state(page);
    if (s.phase === 'clear' || s.phase === 'gameover') return s;
    const target = targetFromPrompt(s.prompt);
    let guess = target;
    if (makeOneMistake && !mistakeDone) { guess = (target + 1) % 11; mistakeDone = true; }
    const id = s.problemId, solved = s.solved;
    await dragDetent(page, s.selectedDetent, guess);
    if (makeOneMistake && mistakeDone && guess !== target) {
      await sleep(1450);
      const retry = await state(page);
      await dragDetent(page, retry.selectedDetent, target);
    }
    await sleep(1450);
    const after = await state(page);
    if (after.problemId === id && after.solved === solved && after.phase === 'playing') throw new Error(`진도 정지: ${JSON.stringify(after)}`);
  }
  throw new Error('완주 guard 초과');
}

const clean = await solveRun(false);
await page.screenshot({path: path.join(FRAMES, '03-clean-clear.png')});

// 결과 종끈을 실제로 눌러 두 번째 판 연습으로 돌아간 뒤, 테스트 훅은 연습만 건너뛴다.
await page.mouse.click(195, 844 * .80);
await sleep(150);
await page.evaluate(() => window.__GAME_TEST__.start());
await sleep(150);
const recovery = await solveRun(true);
await page.screenshot({path: path.join(FRAMES, '04-recovery-clear.png')});

const result = {
  runId: `ppittul-daemun-pointer-${new Date().toISOString()}`,
  artifactHashSha256: artifactHash(),
  method: '타이틀 종끈·연습 2문·실전 정답 제출을 Chrome CDP 실제 mouse down/move/up으로 수행. 실전 목표는 화면 prompt를 중2 각 관계로 계산.',
  clean, recovery, consoleErrors: errors,
  passed: clean.phase === 'clear' && clean.solved === 11 && clean.lives === 3 &&
    recovery.phase === 'clear' && recovery.solved === 11 && recovery.lives === 2 && errors.length === 0
};
fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), JSON.stringify(result, null, 2));
console.log(JSON.stringify(result, null, 2));
await page.close(); await browser.close(); await server.close();
if (!result.passed) process.exitCode = 1;
