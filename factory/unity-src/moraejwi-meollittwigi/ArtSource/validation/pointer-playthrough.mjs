#!/usr/bin/env node
// 현재 WebGL 산출물에서 답 훅 없이, 타이틀→연습→완주→재시작→연습→두 번째 완주를
// 모두 신뢰된 pointer 입력으로 실행한다.
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/moraejwi-meollittwigi');
const FRAMES = path.join(HERE, 'pointer-playthrough');
const RUN_ID = process.env.MGF_RUN_ID || `moraejwi-fix1-${new Date().toISOString()}`;
fs.mkdirSync(FRAMES, { recursive: true });
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome',
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
  throw new Error('Chrome for Testing not found');
}

function artifactHash() {
  const hash = crypto.createHash('sha256');
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(file => `Build/${file}`)]) {
    hash.update(name);
    hash.update(fs.readFileSync(path.join(GAME, name)));
  }
  return hash.digest('hex');
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, label, timeout = 7000) {
  const end = Date.now() + timeout; let s;
  while (Date.now() < end) {
    s = await state(page);
    if (predicate(s)) return s;
    await sleep(70);
  }
  throw new Error(`state timeout (${label}): ${JSON.stringify(s)}`);
}

function gridPoint(s, a, b) {
  const [x0, y0, x1, y1, x2, y2] = s.gridPx;
  return [x0 + (x1 - x0) * a + (x2 - x0) * b, y0 + (y1 - y0) * a + (y2 - y0) * b];
}

function solutions(s) {
  const found = [];
  for (let a = 1; a <= s.gridN; a++) for (let b = 1; b <= s.gridN; b++) {
    if (s.lockedAxis && a !== s.currentA) continue;
    if (s.currentBand === 2 && a - b !== s.currentDeltaAB) continue;
    if (a * a + b * b === s.currentS) found.push({ a, b });
  }
  return found;
}

function wrongPoint(s) {
  const right = new Set(solutions(s).map(p => `${p.a},${p.b}`));
  const aStart = s.lockedAxis ? s.currentA : 1;
  const aEnd = s.lockedAxis ? s.currentA : s.gridN;
  for (let a = aStart; a <= aEnd; a++) for (let b = 1; b <= s.gridN; b++) {
    if (!right.has(`${a},${b}`) && (a !== s.currentA || b !== s.currentB)) return { a, b };
  }
  throw new Error('no wrong point');
}

async function drag(page, from, to) {
  await page.mouse.move(from[0], from[1]);
  await sleep(70);
  await page.mouse.down();
  await page.mouse.move((from[0] + to[0]) / 2, (from[1] + to[1]) / 2, { steps: 5 });
  await page.mouse.move(to[0], to[1], { steps: 6 });
  await sleep(70);
  await page.mouse.up();
}

async function submit(page, point, start = null) {
  const s = await state(page);
  await drag(page, start || s.handlePx, gridPoint(s, point.a, point.b));
  return waitFor(page, x => x.frozen || x.phase === 'clear' || x.phase === 'gameover', 'submission result', 3500);
}

async function completePractice(page, useGuideMidpoint, frameName) {
  const started = Date.now();
  await waitFor(page, x => x.onboarding && x.frozen, 'practice ready');
  // 타이틀/결과 카메라에서 플레이 카메라로 보간된 뒤의 좌표를 읽는다.
  await sleep(550);
  const s = await state(page);
  if (s.currentS !== 25 || s.gridN !== 6) throw new Error(`practice mismatch: ${JSON.stringify(s)}`);
  await page.screenshot({ path: path.join(FRAMES, frameName) });
  const target = gridPoint(s, 3, 4);
  const origin = gridPoint(s, 0, 0);
  const start = useGuideMidpoint ? [(origin[0] + target[0]) / 2, (origin[1] + target[1]) / 2] : s.handlePx;
  await submit(page, { a: 3, b: 4 }, start);
  const next = await waitFor(page, x => !x.onboarding && !x.frozen && x.round === 1, 'practice exit', 7000);
  const elapsed = Date.now() - started;
  return {
    elapsed_ms: elapsed,
    start_kind: useGuideMidpoint ? 'visible-guide-midpoint' : 'mouse-body',
    start_px: start.map(Math.round),
    target_px: target.map(Math.round),
    input_serial_after: next.inputSerial,
    passed_under_30s: elapsed < 30000,
  };
}

async function finishRun(page, { mistake, preferLastSolution, prefix }) {
  let s = await state(page);
  const problemIds = [s.currentId];
  let recovery = null;
  if (mistake) {
    const firstId = s.currentId;
    const wrong = wrongPoint(s);
    await submit(page, wrong);
    s = await waitFor(page, x => x.frozen && x.lives === 2, `${prefix} wrong feedback`);
    await page.screenshot({ path: path.join(FRAMES, `${prefix}-wrong.png`) });
    s = await waitFor(page, x => !x.frozen && x.currentId === firstId, `${prefix} repair ready`);
    const choices = solutions(s);
    const right = choices[preferLastSolution ? choices.length - 1 : 0];
    await submit(page, right);
    s = await waitFor(page, x => x.solved === 1 && x.frozen, `${prefix} repair accepted`);
    recovery = { wrong, right, after: s };
    await page.screenshot({ path: path.join(FRAMES, `${prefix}-repaired.png`) });
    s = await waitFor(page, x => !x.frozen && x.round === 2, `${prefix} round 2`);
    problemIds.push(s.currentId);
  }

  while (s.phase !== 'clear' && s.phase !== 'gameover') {
    const id = s.currentId;
    const choices = solutions(s);
    const right = choices[preferLastSolution ? choices.length - 1 : 0];
    if (!right) throw new Error(`no solution: ${JSON.stringify(s)}`);
    await submit(page, right);
    s = await waitFor(page, x => x.phase === 'clear' || x.phase === 'gameover' || (!x.frozen && x.currentId !== id), `${prefix} next round`, 7000);
    if (s.phase === 'playing' && problemIds.at(-1) !== s.currentId) problemIds.push(s.currentId);
  }
  if (s.phase !== 'clear' || s.solved !== 8 || s.lives !== (mistake ? 2 : 3)) {
    throw new Error(`${prefix} clear gate mismatch: ${JSON.stringify(s)}`);
  }
  if (mistake && (s.firstCorrect !== 7 || s.firstAttempts !== 8)) throw new Error(`${prefix} recovery score mismatch`);
  if (!mistake && (s.firstCorrect !== 8 || s.firstAttempts !== 8)) throw new Error(`${prefix} clean score mismatch`);
  await page.screenshot({ path: path.join(FRAMES, `${prefix}-clear.png`) });
  return { problem_ids: problemIds, recovery, final: s };
}

const hashBefore = artifactHash();
const consoleErrors = [];
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), protocolTimeout: 900000, args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
  page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
  page.on('pageerror', error => consoleErrors.push(String(error)));
  await page.goto(`${server.url}/g/moraejwi-meollittwigi/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 30000 });
  await page.screenshot({ path: path.join(FRAMES, '00-title.png') });

  // 첫 진입은 세계 안 CTA, 첫 연습은 수정 전 거부되던 유령 경로 중점을 누른다.
  await page.mouse.click(195, 766);
  const onboarding1 = await completePractice(page, true, '01-practice-guide-midpoint.png');
  await page.screenshot({ path: path.join(FRAMES, '02-run1-start.png') });
  // 첫 실전의 같은 빗나간 입력 사례가 입력 위치 리플과 몸통 방향 화살표를 재생하는지 남긴다.
  await page.mouse.click(195, 270);
  const missedStartAssist = await waitFor(page, x => x.lastAction === 'refuse-point-handle' && x.round === 1, 'first-round missed start assist');
  await page.screenshot({ path: path.join(FRAMES, '02b-first-round-missed-start-guide.png') });
  const run1 = await finishRun(page, { mistake: true, preferLastSolution: false, prefix: 'run1' });

  // 결과 천막의 실제 재시작 CTA를 누르고, 연습부터 독립된 두 번째 판을 완주한다.
  await page.mouse.click(195, 766);
  const onboarding2 = await completePractice(page, false, '06-run2-practice.png');
  await page.screenshot({ path: path.join(FRAMES, '07-run2-start.png') });
  const run2 = await finishRun(page, { mistake: false, preferLastSolution: true, prefix: 'run2' });

  const deckChanged = run1.problem_ids.join('|') !== run2.problem_ids.join('|');
  const hashAfter = artifactHash();
  const result = {
    generated_at: new Date().toISOString(),
    run_id: RUN_ID,
    artifact_hash: hashBefore,
    artifact_hash_after: hashAfter,
    viewport: '390x844',
    method: 'trusted Puppeteer mouse click/down/move/up; title and restart CTA plus both practices and 16 scored landings; answer/start hooks never called',
    onboarding: { first_run: onboarding1, second_run: onboarding2 },
    first_run_missed_start_assist: {
      input: 'trusted click at 195,270 outside the mouse body',
      last_action: missedStartAssist.lastAction,
      input_serial: missedStartAssist.inputSerial,
      screenshot: 'pointer-playthrough/02b-first-round-missed-start-guide.png',
    },
    run1,
    run2,
    deck_changed: deckChanged,
    console_errors: consoleErrors,
    passed: hashBefore === hashAfter && onboarding1.passed_under_30s && onboarding2.passed_under_30s && deckChanged && run1.final.phase === 'clear' && run2.final.phase === 'clear' && consoleErrors.length === 0,
  };
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
