// 컷라인 실제 포인터 검증.
// 화면 발문을 독립 정수 계산으로 풀고, 스트립 드래그와 칼날 스와이프만으로
// 정상판과 첫 주문 오답 1회 후 회복판을 연속 완주한다.
import crypto from 'node:crypto';
import fs from 'node:fs/promises';
import fsSync from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(import.meta.dirname, '../../../../..');
const PUBLIC_GAME = path.join(ROOT, 'public/g/keotlain');
const OUT_DIR = path.join(import.meta.dirname, 'recovery-second-play');
const OUT_JSON = path.join(import.meta.dirname, 'recovery-second-play-results.json');
const VIEWPORT = { width: 390, height: 844, deviceScaleFactor: 1 };
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function resolveChrome() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(os.homedir(), '.cache/puppeteer/chrome');
  if (!fsSync.existsSync(base)) return undefined;
  for (const build of fsSync.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome'
    ]) {
      const executable = path.join(base, build, rel);
      if (fsSync.existsSync(executable)) return executable;
    }
  }
  return undefined;
}

async function artifactHash(root) {
  const files = [];
  async function walk(dir) {
    for (const entry of await fs.readdir(dir, { withFileTypes: true })) {
      const full = path.join(dir, entry.name);
      if (entry.isDirectory()) await walk(full);
      else if (entry.isFile()) files.push(full);
    }
  }
  await walk(root);
  const hash = crypto.createHash('sha256');
  for (const file of files.sort()) {
    hash.update(path.relative(root, file).split(path.sep).join('/'));
    hash.update('\0'); hash.update(await fs.readFile(file)); hash.update('\0');
  }
  return { algorithm: 'sha256', scope: 'public/g/keotlain recursive path+content', value: hash.digest('hex'), files: files.length };
}

function exactRoot(n, prompt) {
  const root = Math.sqrt(n);
  if (!Number.isInteger(root)) throw new Error(`non-integer result ${n}: ${prompt}`);
  return root;
}

function solve(prompt, pieceIndex) {
  let m = prompt.match(/직각을 낀 두 변이 (\d+) cm, (\d+) cm/);
  if (m) return exactRoot(Number(m[1]) ** 2 + Number(m[2]) ** 2, prompt);
  m = prompt.match(/빗변이 (\d+) cm이고 한 변이 (\d+) cm/);
  if (m) return exactRoot(Number(m[1]) ** 2 - Number(m[2]) ** 2, prompt);
  m = prompt.match(/AB=(\d+) cm, AD=(\d+) cm, AC=(\d+) cm/);
  if (m) {
    const h = Number(m[2]);
    const hyp = pieceIndex === 0 ? Number(m[1]) : Number(m[3]);
    return exactRoot(hyp ** 2 - h ** 2, prompt);
  }
  throw new Error(`unparsed prompt: ${prompt}`);
}

function xForLength(length) {
  return Math.round(VIEWPORT.width * (.08 + ((length - 1) / 28) * .60));
}

async function pointerDrag(page, from, to) {
  await page.mouse.move(from[0], from[1]);
  await page.mouse.down();
  await sleep(90);
  await page.mouse.move((from[0] + to[0]) / 2, (from[1] + to[1]) / 2, { steps: 6 });
  await sleep(90);
  await page.mouse.move(to[0], to[1], { steps: 6 });
  await sleep(90);
  await page.mouse.up();
}

async function state(page) {
  return page.evaluate(() => window.__GAME_TEST__.getState());
}

async function installPointerAudit(page) {
  await page.evaluate(() => {
    window.__KEOTLAIN_POINTER_AUDIT__ = [];
    const canvas = document.querySelector('canvas');
    for (const type of ['pointerdown', 'pointerup']) {
      canvas.addEventListener(type, event => window.__KEOTLAIN_POINTER_AUDIT__.push({
        type, isTrusted: event.isTrusted, x: Math.round(event.clientX), y: Math.round(event.clientY), at: performance.now()
      }), true);
    }
  });
}

async function dragStrip(page, answer) {
  const x = xForLength(answer);
  await pointerDrag(page, [195, 730], [x, 730]);
  await sleep(120);
  const observed = await state(page);
  if (observed.selectedLength !== answer) {
    const audit = await page.evaluate(() => window.__KEOTLAIN_POINTER_AUDIT__);
    throw new Error(`strip selected ${observed.selectedLength}, expected ${answer}; state=${JSON.stringify(observed)} audit=${JSON.stringify(audit.slice(-4))}`);
  }
  return { from: [195, 730], to: [x, 730] };
}

async function swipeBlade(page) {
  const from = [335, 696], to = [335, 792];
  await pointerDrag(page, from, to);
  return { from, to };
}

async function changeCartridge(page) {
  await page.mouse.click(38, 790);
  await sleep(140);
}

async function submit(page, target, log, label) {
  const before = await state(page);
  if (target > before.cartridgeRemaining) await changeCartridge(page);
  const strip = await dragStrip(page, target);
  const afterStrip = await state(page);
  const blade = await swipeBlade(page);
  await sleep(120);
  const reveal = await state(page);
  log.push({
    label,
    prompt: before.prompt,
    pieceIndex: before.pieceIndex,
    independentAnswer: solve(before.prompt, before.pieceIndex),
    submitted: target,
    pointer: { strip, blade },
    before: { phase: before.phase, solved: before.solved, lives: before.lives, firstAttemptCorrect: before.firstAttemptCorrect, remaining: before.cartridgeRemaining },
    afterStrip: { selectedLength: afterStrip.selectedLength },
    reveal: { phase: reveal.phase, solved: reveal.solved, lives: reveal.lives, firstAttemptCorrect: reveal.firstAttemptCorrect, misconceptionId: reveal.misconceptionId }
  });
  if (reveal.phase !== 'paused') throw new Error(`blade did not submit for ${label}: ${JSON.stringify(reveal)}`);
  await page.waitForFunction(() => window.__GAME_TEST__.getState().phase !== 'paused', { timeout: 3500 });
  return state(page);
}

async function startWithPractice(page, log, run) {
  await page.mouse.click(195, 422);
  await page.waitForFunction(() => window.__GAME_TEST__.getState().onboarding === true, { timeout: 2000 });
  const practice = await state(page);
  const answer = solve(practice.prompt, practice.pieceIndex);
  const settled = await submit(page, answer, log, `run${run}-practice`);
  if (settled.onboarding || settled.phase !== 'playing') throw new Error(`practice did not advance: ${JSON.stringify(settled)}`);
  return { practice, settled };
}

async function playRun(page, run, injectWrong, log) {
  let wrongDone = false;
  for (let guard = 0; guard < 20; guard++) {
    const before = await state(page);
    if (before.phase !== 'playing') return before;
    const answer = solve(before.prompt, before.pieceIndex);
    if (injectWrong && !wrongDone) {
      const wrong = answer === 29 ? 28 : answer + 1;
      await submit(page, wrong, log, `run${run}-first-wrong`);
      wrongDone = true;
      continue;
    }
    await submit(page, answer, log, `run${run}-correct-${before.solved + 1}-piece-${before.pieceIndex + 1}`);
  }
  throw new Error(`run ${run} exceeded action guard`);
}

const artifactBefore = await artifactHash(PUBLIC_GAME);
await fs.rm(OUT_DIR, { recursive: true, force: true });
await fs.mkdir(OUT_DIR, { recursive: true });
const server = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio', '--enable-unsafe-swiftshader', '--use-gl=angle', '--use-angle=swiftshader']
});

const errors = [];
const log = [];
let report;
try {
  const page = await browser.newPage();
  await page.setViewport(VIEWPORT);
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/keotlain/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 30000 });
  await installPointerAudit(page);
  await page.screenshot({ path: path.join(OUT_DIR, '00-title.png') });

  const firstStart = await startWithPractice(page, log, 1);
  await page.screenshot({ path: path.join(OUT_DIR, '01-after-practice.png') });
  const perfect = await playRun(page, 1, false, log);
  await page.screenshot({ path: path.join(OUT_DIR, '02-perfect-clear.png') });

  await page.mouse.click(195, 422);
  await page.waitForFunction(() => window.__GAME_TEST__.getState().phase === 'title', { timeout: 2000 });
  const secondStart = await startWithPractice(page, log, 2);
  await page.screenshot({ path: path.join(OUT_DIR, '03-second-practice-complete.png') });
  const recovery = await playRun(page, 2, true, log);
  await page.screenshot({ path: path.join(OUT_DIR, '04-recovery-clear.png') });

  const pointerAudit = await page.evaluate(() => window.__KEOTLAIN_POINTER_AUDIT__);
  const artifactAfter = await artifactHash(PUBLIC_GAME);
  report = {
    run_id: `keotlain-recovery-second-play-${new Date().toISOString()}`,
    artifact: artifactAfter,
    artifact_unchanged_during_test: artifactBefore.value === artifactAfter.value,
    method: 'trusted CDP pointer events only; answers independently derived from displayed prompt with integer arithmetic',
    firstPractice: { prompt: firstStart.practice.prompt, completed: !firstStart.settled.onboarding },
    secondPractice: { prompt: secondStart.practice.prompt, completed: !secondStart.settled.onboarding },
    perfect: { phase: perfect.phase, solved: perfect.solved, lives: perfect.lives, firstAttemptCorrect: perfect.firstAttemptCorrect, score: perfect.score },
    recovery: { phase: recovery.phase, solved: recovery.solved, lives: recovery.lives, firstAttemptCorrect: recovery.firstAttemptCorrect, score: recovery.score },
    pointerAudit: { events: pointerAudit.length, allTrusted: pointerAudit.every(event => event.isTrusted), sample: pointerAudit.slice(0, 8) },
    log,
    errors
  };
  report.passed = report.artifact_unchanged_during_test && report.firstPractice.completed && report.secondPractice.completed
    && perfect.phase === 'clear' && perfect.solved === 8 && perfect.lives === 3 && perfect.firstAttemptCorrect === 8
    && recovery.phase === 'clear' && recovery.solved === 8 && recovery.lives === 2 && recovery.firstAttemptCorrect === 7
    && report.pointerAudit.allTrusted && errors.length === 0;
} catch (error) {
  report = { passed: false, artifact: artifactBefore, method: 'trusted CDP pointer events', error: String(error.stack || error), log, errors };
  process.exitCode = 1;
} finally {
  await fs.writeFile(OUT_JSON, JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify({ ...report, log: `${log.length} entries` }, null, 2));
  await browser.close();
  await server.close();
}
if (!report.passed) process.exitCode = 1;
