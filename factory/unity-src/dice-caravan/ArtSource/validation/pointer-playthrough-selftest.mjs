import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/dice-caravan');
const FRAMES = path.join(HERE, 'pointer-playthrough');
const VIEWPORT = { width: 390, height: 844, deviceScaleFactor: 1 };
const sleep = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds));
let unorderedHiddenProbeCount = 0;
fs.mkdirSync(FRAMES, { recursive: true });

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'
    ]) {
      const candidate = path.join(base, build, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
  throw new Error('Chrome for Testing not found');
}

function buildHash() {
  const hash = crypto.createHash('sha256');
  const names = ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(name => `Build/${name}`)];
  for (const name of names) {
    hash.update(name); hash.update('\0'); hash.update(fs.readFileSync(path.join(GAME, name))); hash.update('\0');
  }
  return hash.digest('hex');
}

function boardFor(problem) {
  const prompt = problem.prompt;
  let match;
  if ((match = prompt.match(/1부터 (\d+)까지의 자연수 중 (\d+)의 배수(?: 또는 (\d+)의 배수)?/))) {
    const n = Number(match[1]), a = Number(match[2]), b = match[3] ? Number(match[3]) : null;
    const cols = n <= 15 ? 5 : 6, rows = Math.ceil(n / cols), answers = [];
    for (let value = 1; value <= n; value++) if (value % a === 0 || (b && value % b === 0)) answers.push(value - 1);
    return { rows, cols, answers };
  }
  if ((match = prompt.match(/(\d+)종류와 .+? (\d+)종류 중에서 각각 하나씩/))) {
    const r = Number(match[1]), c = Number(match[2]), rows = Math.min(6, r + 1), cols = Math.min(6, c + 1), answers = [];
    for (let y = 0; y < r; y++) for (let x = 0; x < c; x++) answers.push(y * cols + x);
    return { rows, cols, answers };
  }
  if ((match = prompt.match(/두 눈의 수의 (합|곱)이 (\d+)인/))) {
    const operation = match[1], target = Number(match[2]), answers = [];
    for (let y = 0; y < 6; y++) for (let x = 0; x < 6; x++) if (operation === '합' ? y + x + 2 === target : (y + 1) * (x + 1) === target) answers.push(y * 6 + x);
    return { rows: 6, cols: 6, answers };
  }
  if ((match = prompt.match(/카드 꾸러미에 ((?:\d, ){3}\d) 숫자 카드가 각각 한 장씩/))) {
    const digits = match[1].split(', ').map(Number), answers = [];
    for (let y = 0; y < 4; y++) for (let x = 0; x < 4; x++) if (y !== x && digits[y] !== 0) answers.push(y * 4 + x);
    return { rows: 4, cols: 4, answers };
  }
  if ((match = prompt.match(/(\d+)명의 후보 중에서 (회장 1명과 부회장 1명|대표 2명)/))) {
    const n = Number(match[1]), roles = match[2].startsWith('회장'), answers = [];
    for (let y = 0; y < n; y++) for (let x = 0; x < n; x++) if (roles ? y !== x : y < x) answers.push(y * n + x);
    return { rows: n, cols: n, answers };
  }
  throw new Error(`cannot derive board for ${problem.id}: ${prompt}`);
}

function pinPoint(index, rows, cols) {
  const area = 320;
  const cell = Math.min(52, area / cols, area / rows);
  const gap = cell * 0.06;
  const totalWidth = cols * cell + (cols - 1) * gap;
  const totalHeight = rows * cell + (rows - 1) * gap;
  const x = index % cols, y = Math.floor(index / cols);
  const localX = -totalWidth / 2 + cell / 2 + x * (cell + gap);
  const localY = totalHeight / 2 - cell / 2 - y * (cell + gap);
  return [VIEWPORT.width / 2 + localX, VIEWPORT.height - VIEWPORT.height * 0.425 - localY];
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }

async function waitForState(page, predicate, description, timeout = 6000) {
  const limit = Date.now() + timeout;
  let current;
  while (Date.now() < limit) {
    current = await state(page);
    if (predicate(current)) return current;
    await sleep(80);
  }
  throw new Error(`${description}: ${JSON.stringify(current)}`);
}

async function click(page, point) {
  await page.mouse.move(point[0], point[1]);
  await page.mouse.down(); await sleep(35); await page.mouse.up(); await sleep(35);
}

async function pullCord(page) {
  const from = [195, 768], to = [195, 838];
  await page.mouse.move(...from); await page.mouse.down();
  await page.mouse.move(195, 802, { steps: 6 }); await page.mouse.move(...to, { steps: 6 });
  await sleep(60); await page.mouse.up();
  return { from, to };
}

async function selectIndices(page, board, indices) {
  for (const index of indices) {
    const before = await state(page);
    await click(page, pinPoint(index, board.rows, board.cols));
    const after = await state(page);
    if (after.phase !== 'playing' || after.solved !== before.solved) {
      throw new Error(`pin ${index} unexpectedly resolved the problem at ${JSON.stringify(pinPoint(index, board.rows, board.cols))}: ${JSON.stringify({ before, after })}`);
    }
  }
}

async function finishPractice(page, log, prefix) {
  const practice = { rows: 6, cols: 6, answers: [3, 8, 13, 18] };
  for (let i = 0; i < practice.answers.length; i++) {
    const before = await state(page);
    await click(page, pinPoint(practice.answers[i], 6, 6));
    const after = await state(page);
    if (after.selectedPins !== i + 1 || !after.onboarding) throw new Error(`practice guide step ${i + 1} failed: ${JSON.stringify(after)}`);
    const screenshot = `${prefix}-practice-guide-${i + 1}.png`;
    await page.screenshot({ path: path.join(FRAMES, screenshot) });
    log.push({ action: `practice-pin-${i + 1}`, pointer: pinPoint(practice.answers[i], 6, 6).map(Math.round), before, after, screenshot });
  }
  const beforeSubmit = await state(page);
  const gesture = await pullCord(page);
  const after = await waitForState(page, current => !current.onboarding && current.problemId !== 'practice-sum5', 'practice did not enter first mission');
  await sleep(180);
  log.push({ action: 'practice-cord-submit', pointer: gesture, before: beforeSubmit, after });
  return after;
}

async function solveCurrent(page, problemsById, log, label) {
  const before = await state(page);
  const problem = problemsById.get(before.problemId);
  if (!problem) throw new Error(`problem absent from live bank: ${before.problemId}`);
  const board = boardFor(problem);
  if (board.answers.length !== problem.answerNumeric) throw new Error(`board/answer mismatch for ${problem.id}`);
  if (/대표 2명/.test(problem.prompt)) {
    // Probe two different reverse-direction cells (B-A and C-A).  Both mirror
    // visible canonical pins and must be physically absent, not merely marked wrong.
    for (const hiddenReverseIndex of [board.cols, board.cols * 2]) {
      const hiddenBefore = await state(page);
      await click(page, pinPoint(hiddenReverseIndex, board.rows, board.cols));
      const hiddenAfter = await state(page);
      if (hiddenAfter.selectedPins !== hiddenBefore.selectedPins || hiddenAfter.problemId !== hiddenBefore.problemId) {
        throw new Error(`hidden unordered mirror accepted pointer input for ${problem.id}: ${JSON.stringify({ hiddenBefore, hiddenAfter })}`);
      }
      unorderedHiddenProbeCount++;
      log.push({ action: 'unordered-hidden-mirror-rejected', problemId: problem.id, hiddenReverseIndex,
        pointer: pinPoint(hiddenReverseIndex, board.rows, board.cols).map(Math.round), before: hiddenBefore, after: hiddenAfter });
    }
  }
  await selectIndices(page, board, board.answers);
  const selected = await state(page);
  if (selected.selectedPins !== board.answers.length) throw new Error(`pointer selection mismatch for ${problem.id}: ${JSON.stringify(selected)}`);
  const gesture = await pullCord(page);
  const resolved = await waitForState(page, current => current.phase === 'clear' || current.solved > before.solved, `${label} did not resolve correctly`);
  if (label === 'perfect-mission-1') {
    await sleep(320);
    await page.screenshot({ path: path.join(FRAMES, '02-sail-unfurl-event.png') });
    await page.setViewport({ width: 1280, height: 800, deviceScaleFactor: 1 });
    await sleep(260);
    await page.screenshot({ path: path.join(FRAMES, '02b-sail-unfurl-event-wide.png') });
    await page.setViewport(VIEWPORT);
    await sleep(100);
  }
  const after = resolved.phase === 'clear' ? resolved : await waitForState(page, current => current.phase === 'clear' || current.problemId !== before.problemId, `${label} feedback did not advance`);
  log.push({ action: label, problemId: problem.id, answerIndices: board.answers, pointer: gesture, before, selected, after });
  return after;
}

async function captureCover(browser, width, height, filename, serverUrl) {
  const page = await browser.newPage();
  await page.setViewport({ width, height, deviceScaleFactor: 1 });
  await page.goto(`${serverUrl}/g/dice-caravan/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 30000 });
  await sleep(500);
  await page.screenshot({ path: path.join(GAME, filename) });
  await page.close();
}

const buildBefore = buildHash();
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
let result;
try {
  await captureCover(browser, 1200, 630, 'thumb.png', server.url);
  await captureCover(browser, 1080, 1080, 'square.png', server.url);

  const page = await browser.newPage();
  await page.setViewport(VIEWPORT);
  const errors = [];
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/dice-caravan/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 30000 });
  const problems = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(500));
  const problemsById = new Map(problems.map(problem => [problem.id, problem]));
  await page.evaluate(() => {
    window.__DICE_CARAVAN_POINTER_AUDIT__ = [];
    const canvas = document.querySelector('canvas');
    for (const type of ['pointerdown', 'pointerup']) canvas.addEventListener(type, event => window.__DICE_CARAVAN_POINTER_AUDIT__.push({ type, isTrusted: event.isTrusted, x: Math.round(event.clientX), y: Math.round(event.clientY), at: performance.now() }), true);
  });
  const actions = [];
  await page.screenshot({ path: path.join(FRAMES, '00-title.png') });

  const titleStartAt = Date.now();
  await click(page, [48, 720]);
  const afterWrongTitleTap = await state(page);
  if (afterWrongTitleTap.phase !== 'title') throw new Error(`wrong title tap unexpectedly started: ${JSON.stringify(afterWrongTitleTap)}`);
  await sleep(180);
  await page.screenshot({ path: path.join(FRAMES, '00b-title-spatial-guide.png') });
  await click(page, [195, 422]);
  await waitForState(page, current => current.onboarding === true, 'title cover did not start practice');
  const titleStartedMs = Date.now() - titleStartAt;
  actions.push({ action: 'title-invalid-tap-spatial-guide', point: [48, 720], after: afterWrongTitleTap, screenshot: '00b-title-spatial-guide.png' });
  actions.push({ action: 'title-centre-cover-pointer', point: [195, 422], titleStartedMs, after: await state(page) });
  await finishPractice(page, actions, '01');
  const firstMissionId = (await state(page)).problemId;
  for (let mission = 0; mission < 6; mission++) await solveCurrent(page, problemsById, actions, `perfect-mission-${mission + 1}`);
  const perfect = await waitForState(page, current => current.phase === 'clear', 'perfect run did not clear');
  await page.screenshot({ path: path.join(FRAMES, '02-perfect-clear.png') });

  await click(page, [195, 542]);
  await waitForState(page, current => current.onboarding === true, 'restart board did not start second practice');
  actions.push({ action: 'restart-pointer', point: [195, 542], after: await state(page) });
  await finishPractice(page, actions, '03');

  const wrongBefore = await state(page);
  const wrongProblem = problemsById.get(wrongBefore.problemId);
  const wrongBoard = boardFor(wrongProblem);
  const wrongIndex = Array.from({ length: wrongBoard.rows * wrongBoard.cols }, (_, index) => index).find(index => !wrongBoard.answers.includes(index));
  await click(page, pinPoint(wrongIndex, wrongBoard.rows, wrongBoard.cols));
  const wrongGesture = await pullCord(page);
  const wrongFeedback = await waitForState(page, current => current.lives === 2, 'deliberate wrong answer did not cost one life');
  await page.screenshot({ path: path.join(FRAMES, '04-recovery-wrong-feedback.png') });
  await sleep(1450);
  const recoveredReady = await state(page);
  if (recoveredReady.problemId !== wrongBefore.problemId || recoveredReady.lives !== 2 || recoveredReady.solved !== wrongBefore.solved) throw new Error(`same-problem recovery did not resume: ${JSON.stringify(recoveredReady)}`);
  actions.push({ action: 'recovery-first-mission-wrong', wrongIndex, pointer: wrongGesture, before: wrongBefore, feedback: wrongFeedback, after: recoveredReady });
  await click(page, pinPoint(wrongIndex, wrongBoard.rows, wrongBoard.cols));
  await solveCurrent(page, problemsById, actions, 'recovery-mission-1-corrected');
  for (let mission = 1; mission < 6; mission++) await solveCurrent(page, problemsById, actions, `recovery-mission-${mission + 1}`);
  const recovery = await waitForState(page, current => current.phase === 'clear', 'recovery run did not clear');
  await page.screenshot({ path: path.join(FRAMES, '05-recovery-clear.png') });

  const pointerAudit = await page.evaluate(() => window.__DICE_CARAVAN_POINTER_AUDIT__);
  const buildAfter = buildHash();
  const checks = {
    buildStable: buildBefore === buildAfter,
    bankAvailable: problems.length >= 300 && problemsById.has(firstMissionId),
    everyPointerEventTrusted: pointerAudit.length > 0 && pointerAudit.every(event => event.isTrusted),
    perfectClear: perfect.phase === 'clear' && perfect.solved === 6 && perfect.lives === 3 && perfect.firstAttemptTotal === 6 && perfect.firstAttemptCorrect === 6,
    recoveryClear: recovery.phase === 'clear' && recovery.solved === 6 && recovery.lives === 2 && recovery.firstAttemptTotal === 6 && recovery.firstAttemptCorrect === 5,
    titleStartsWithin10Seconds: titleStartedMs < 10000,
    unorderedMirrorsNotInteractive: unorderedHiddenProbeCount >= 2,
    noConsoleErrors: errors.length === 0
  };
  result = {
    generated_at: new Date().toISOString(),
    build_sha256: buildAfter,
    viewport: '390x844',
    evidence_class: 'trusted-pointer complete playthrough; no gameplay test-hook mutation',
    live_bank_count: problems.length,
    perfect: { phase: perfect.phase, solved: perfect.solved, lives: perfect.lives, firstAttemptTotal: perfect.firstAttemptTotal, firstAttemptCorrect: perfect.firstAttemptCorrect },
    recovery: { phase: recovery.phase, solved: recovery.solved, lives: recovery.lives, firstAttemptTotal: recovery.firstAttemptTotal, firstAttemptCorrect: recovery.firstAttemptCorrect },
    title_onboarding: { started_ms: titleStartedMs, wrong_tap_stayed_title: afterWrongTitleTap.phase === 'title', guide_screenshot: '00b-title-spatial-guide.png' },
    unordered_hidden_mirror_probes: unorderedHiddenProbeCount,
    pointer_audit: { count: pointerAudit.length, trusted_count: pointerAudit.filter(event => event.isTrusted).length, events: pointerAudit },
    console_errors: errors,
    actions,
    checks,
    result: Object.values(checks).every(Boolean) ? 'pass' : 'fail'
  };
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify({ build_sha256: result.build_sha256, live_bank_count: result.live_bank_count, perfect: result.perfect, recovery: result.recovery, pointer_audit: { count: result.pointer_audit.count, trusted_count: result.pointer_audit.trusted_count }, checks: result.checks, result: result.result }, null, 2));
  if (result.result !== 'pass') process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
