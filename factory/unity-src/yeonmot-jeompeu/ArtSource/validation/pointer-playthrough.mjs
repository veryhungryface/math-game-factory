#!/usr/bin/env node
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/yeonmot-jeompeu');
const FRAMES = path.join(HERE, 'pointer-playthrough');
fs.mkdirSync(FRAMES, { recursive: true });
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const candidate = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(candidate)) return candidate;
  }
  throw new Error('Chrome for Testing not found');
}

function artifactHash() {
  const hash = crypto.createHash('sha256');
  const walk = dir => fs.readdirSync(dir, { withFileTypes: true }).sort((a, b) => a.name.localeCompare(b.name)).forEach(entry => {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) walk(full);
    else { hash.update(path.relative(GAME, full)); hash.update(fs.readFileSync(full)); }
  });
  walk(GAME);
  return hash.digest('hex');
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, label, timeout = 7000) {
  const until = Date.now() + timeout; let latest;
  while (Date.now() < until) {
    latest = await state(page);
    if (predicate(latest)) return latest;
    await sleep(70);
  }
  throw new Error(`${label}: ${JSON.stringify(latest)}`);
}

async function drag(page, from, to) {
  await page.mouse.move(from[0], from[1]);
  await page.mouse.down();
  await sleep(90);
  await page.mouse.move((from[0] + to[0]) / 2, (from[1] + to[1]) / 2, { steps: 8 });
  await sleep(110);
  await page.mouse.move(to[0], to[1], { steps: 8 });
  await sleep(110);
  await page.mouse.up();
}

function point(array, index) { return [array[index * 2], array[index * 2 + 1]]; }

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: chromePath(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'],
});
const page = await browser.newPage();
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
const consoleErrors = [];
page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
page.on('pageerror', error => consoleErrors.push(String(error)));

async function load() {
  await page.goto(`${server.url}/g/yeonmot-jeompeu/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 30000 });
  const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(1000));
  return bank.length;
}

const permutations = [
  [0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0],
];
function sideIndex(a, b) {
  if ((a === 0 && b === 1) || (a === 1 && b === 0)) return 0;
  if ((a === 1 && b === 2) || (a === 2 && b === 1)) return 1;
  return 2;
}
function ratioEqual(a, b) { return a.every((value, index) => value > 0 && b[index] > 0) && a[0] * b[1] === a[1] * b[0] && a[1] * b[2] === a[2] * b[1]; }
function mappingWorks(s, leaf, permutation) {
  const sides = s.candidateSides.slice(leaf * 3, leaf * 3 + 3);
  const angles = s.candidateAngles.slice(leaf * 3, leaf * 3 + 3);
  if (s.problemKind === 'SSS') {
    const mapped = [
      sides[sideIndex(permutation[0], permutation[1])],
      sides[sideIndex(permutation[1], permutation[2])],
      sides[sideIndex(permutation[2], permutation[0])],
    ];
    return ratioEqual(s.sourceSides, mapped);
  }
  if (s.problemKind === 'SAS') {
    const mapped = [
      sides[sideIndex(permutation[0], permutation[1])],
      sides[sideIndex(permutation[0], permutation[2])],
    ];
    return angles[permutation[0]] === s.sourceAngles[0] &&
      s.sourceSides[0] * mapped[1] === s.sourceSides[2] * mapped[0];
  }
  return permutation.every((candidateVertex, sourceVertex) => angles[candidateVertex] === s.sourceAngles[sourceVertex]);
}

function correctSlot(s) {
  const solutions = [];
  for (let leaf = 0; leaf < 2; leaf++) for (const permutation of permutations) {
    if (mappingWorks(s, leaf, permutation)) solutions.push({ leaf, permutation });
  }
  if (solutions.length !== 1) throw new Error(`independent math solutions=${solutions.length}: ${JSON.stringify(s)}`);
  const { leaf, permutation } = solutions[0];
  const targetVertex = permutation[s.sourceActiveVertex];
  const positions = s.positionToVertex.slice(leaf * 3, leaf * 3 + 3);
  const position = positions.indexOf(targetVertex);
  if (position < 0) throw new Error(`target vertex missing: ${JSON.stringify({ leaf, permutation, positions, targetVertex })}`);
  return leaf * 3 + position;
}

async function submit(page, slot) {
  const before = await state(page);
  const from = point(before.sourcePx, before.sourceActiveVertex);
  const to = point(before.targetPx, slot);
  await drag(page, from, to);
  return waitFor(page, s => s.submissionCount > before.submissionCount || s.phase === 'clear' || s.phase === 'gameover', `submit slot ${slot}`);
}

async function enterMain(label) {
  const startedAt = Date.now();
  await page.mouse.click(195, 690);
  let s = await waitFor(page, x => x.onboarding && x.currentProblemId === 'practice-345', `${label} practice`);
  const beforePractice = s;
  const practiceSlot = correctSlot(s);
  await submit(page, practiceSlot);
  s = await waitFor(page, x => !x.onboarding && x.currentProblemId !== 'practice-345', `${label} main after B→F`, 8000);
  return {
    elapsedMs: Date.now() - startedAt,
    input: { from: point(beforePractice.sourcePx, 1), to: point(beforePractice.targetPx, practiceSlot), slot: practiceSlot, mathematicalAnswer: 'B↔F' },
    state: s,
  };
}

async function solveCurrent(wrongFirst = false) {
  let s = await state(page);
  const problemId = s.currentProblemId;
  const answer = correctSlot(s);
  let wrong = null;
  if (wrongFirst) {
    const wrongSlot = (answer + 1) % 6;
    wrong = await submit(page, wrongSlot);
    await waitFor(page, x => x.lives === s.lives - 1 && x.currentProblemId === problemId, `wrong ${problemId}`);
    s = await waitFor(page, x => x.selectedVertex === -1 && x.currentProblemId === problemId, `wrong recovery ${problemId}`, 5000);
  }
  const solvedBefore = s.solved;
  await submit(page, answer);
  const after = await waitFor(page, x => x.solved > solvedBefore || x.phase === 'clear' || x.phase === 'gameover', `correct ${problemId}`, 8000);
  await waitFor(page, x => x.phase === 'clear' || x.phase === 'gameover' || x.currentProblemId !== problemId, `advance ${problemId}`, 5000);
  return { problemId, answer, wrong, after };
}

async function finishRun(name, wrongOrders = []) {
  const actions = [];
  for (let order = 0; order < 8; order++) actions.push(await solveCurrent(wrongOrders.includes(order)));
  const final = await waitFor(page, x => x.phase === 'clear' || x.phase === 'gameover', `${name} end`);
  return { actions, final };
}

const result = {
  generatedAt: new Date().toISOString(),
  artifactHashSha256: artifactHash(),
  viewport: '390x844',
  input: 'trusted Puppeteer mouse down/move/up on projected Unity vertex coordinates; answerCorrect/answerWrong hooks were never called',
  scenarios: {},
  consoleErrors,
};
const resume = process.env.RESUME || '';
const checkpointName = resume ? `pointer-playthrough-${resume}-partial.json` : 'pointer-playthrough-partial.json';
const checkpoint = () => fs.writeFileSync(path.join(HERE, checkpointName), `${JSON.stringify(result, null, 2)}\n`);

try {
  if (!resume) {
    result.bankCount = await load();
    result.scenarios.onboarding = await enterMain('normal');
    result.scenarios.normal = await finishRun('normal');
    checkpoint();

    // 결과 화면의 지정 재시작 표식만 실제 pointer로 눌러 두 번째 독립 덱을 시작한다.
    const firstIds = result.scenarios.normal.actions.map(action => action.problemId);
    await page.mouse.click(195, 608);
    await waitFor(page, s => s.phase === 'playing' && !s.onboarding && s.solved === 0 && s.currentProblemId, 'second run start');
    result.scenarios.second = await finishRun('second');
    const secondIds = result.scenarios.second.actions.map(action => action.problemId);
    result.scenarios.second.differentProblems = secondIds.some((id, index) => id !== firstIds[index]);
    checkpoint();

    await load();
    result.scenarios.recoveryOneOnboarding = await enterMain('recovery-one');
    result.scenarios.recoveryOne = await finishRun('recovery-one', [0]);
    checkpoint();
  } else {
    result.bankCount = await load();
  }

  await load();
  result.scenarios.recoveryTwoOnboarding = await enterMain('recovery-two');
  result.scenarios.recoveryTwo = await finishRun('recovery-two', [0, 1]);
  checkpoint();

  await load();
  result.scenarios.gameoverOnboarding = await enterMain('gameover');
  const beforeGameover = await state(page);
  const answer = correctSlot(beforeGameover);
  for (let attempt = 0; attempt < 3; attempt++) {
    await submit(page, (answer + 1 + attempt) % 6);
    if (attempt < 2) await waitFor(page, s => s.selectedVertex === -1 && s.phase === 'playing', `gameover retry ${attempt}`, 5000);
  }
  const ended = await waitFor(page, s => s.phase === 'gameover' && s.lives === 0, 'three wrong gameover', 8000);
  await page.mouse.click(28, 700); // 재시작 표식 밖의 입력은 잠겨 있어야 한다.
  await sleep(500);
  const locked = await state(page);
  result.scenarios.gameover = { ended, locked, inputLocked: JSON.stringify(ended) === JSON.stringify(locked) };
  checkpoint();

  const tailPassed = result.bankCount >= 300 && result.scenarios.recoveryTwoOnboarding.elapsedMs < 10000 &&
    result.scenarios.recoveryTwo.final.phase === 'clear' && result.scenarios.recoveryTwo.final.firstCorrect === 6 &&
    result.scenarios.gameover.inputLocked && consoleErrors.length === 0;
  result.passed = resume ? tailPassed : tailPassed &&
    result.scenarios.onboarding.elapsedMs < 10000 &&
    result.scenarios.normal.final.phase === 'clear' && result.scenarios.normal.final.firstCorrect === 8 &&
    result.scenarios.second.final.phase === 'clear' && result.scenarios.second.final.firstCorrect === 8 && result.scenarios.second.differentProblems &&
    result.scenarios.recoveryOne.final.phase === 'clear' && result.scenarios.recoveryOne.final.firstCorrect === 7;
  const outputName = resume ? `pointer-playthrough-${resume}-results.json` : 'pointer-playthrough-results.json';
  fs.writeFileSync(path.join(HERE, outputName), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await page.close();
  await browser.close();
  await server.close();
}
