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
const FRAMES = path.join(HERE, 'fix2-regression');
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
async function waitFor(page, predicate, label, timeout = 8000) {
  const until = Date.now() + timeout;
  let latest;
  while (Date.now() < until) {
    latest = await state(page);
    if (predicate(latest)) return latest;
    await sleep(70);
  }
  throw new Error(`${label}: ${JSON.stringify(latest)}`);
}

function point(array, index) { return [array[index * 2], array[index * 2 + 1]]; }
async function drag(page, from, to) {
  await page.mouse.move(from[0], from[1]);
  await page.mouse.down();
  await sleep(100);
  await page.mouse.move((from[0] + to[0]) / 2, (from[1] + to[1]) / 2, { steps: 10 });
  await sleep(120);
  await page.mouse.move(to[0], to[1], { steps: 10 });
  await sleep(120);
  await page.mouse.up();
}

const permutations = [
  [0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0],
];
function sideIndex(a, b) {
  if ((a === 0 && b === 1) || (a === 1 && b === 0)) return 0;
  if ((a === 1 && b === 2) || (a === 2 && b === 1)) return 1;
  return 2;
}
function ratioEqual(a, b) {
  return a.every((value, index) => value > 0 && b[index] > 0) &&
    a[0] * b[1] === a[1] * b[0] && a[1] * b[2] === a[2] * b[1];
}
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
    const mapped = [sides[sideIndex(permutation[0], permutation[1])], sides[sideIndex(permutation[0], permutation[2])]];
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
  if (solutions.length !== 1) throw new Error(`independent solutions=${solutions.length}`);
  const { leaf, permutation } = solutions[0];
  const targetVertex = permutation[s.sourceActiveVertex];
  const position = s.positionToVertex.slice(leaf * 3, leaf * 3 + 3).indexOf(targetVertex);
  if (position < 0) throw new Error('target vertex missing');
  return leaf * 3 + position;
}

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

const result = {
  generatedAt: new Date().toISOString(),
  artifactHashSha256: artifactHash(),
  viewport: '390x844',
  input: 'trusted Puppeteer mouse click/down/move/up; answerCorrect/answerWrong hooks were never called',
  beforeFix: {
    artifactHashSha256: '36f7a9d8224c9974a459d395794d26c6d9a38f3a55f275eea8ea32cc0eb73ef3',
    sameInput: 'title click, then no successful frog drag for at least 16 seconds',
    observed: 'automatic B→F jump advanced to the main course after 14.5 seconds',
  },
  consoleErrors,
};

try {
  await page.goto(`${server.url}/g/yeonmot-jeompeu/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 30000 });
  await page.mouse.click(195, 690);
  let s = await waitFor(page, x => x.onboarding && x.currentProblemId === 'practice-345', 'practice start');
  await page.screenshot({ path: path.join(FRAMES, '00-practice-start.png') });

  // 잘못된 꼭짓점 탭도 진도를 올리지 않고 출발점과 F 안내를 다시 강조해야 한다.
  const practiceAnswer = correctSlot(s);
  const wrongTap = practiceAnswer === 0 ? 1 : 0;
  await page.mouse.click(...point(s.targetPx, wrongTap));
  await sleep(500);
  const afterWrongTap = await state(page);
  await page.screenshot({ path: path.join(FRAMES, '01-wrong-tap-guide.png') });

  // 수정 전 자동 전환이 일어난 동일 입력 사례를 16초 동안 재현한다.
  await sleep(16000);
  const afterNoInput = await state(page);
  await page.screenshot({ path: path.join(FRAMES, '02-after-16s-no-auto.png') });

  // 실제 B→F 드래그만 연습을 통과시켜야 한다.
  await drag(page, point(afterNoInput.sourcePx, afterNoInput.sourceActiveVertex), point(afterNoInput.targetPx, practiceAnswer));
  const main = await waitFor(page, x => !x.onboarding && x.currentProblemId !== 'practice-345', 'manual B→F enters main');
  await page.screenshot({ path: path.join(FRAMES, '03-main-after-manual-practice.png') });

  // 같은 실제 입력 경로로 본판 첫 문제도 성공시킨다.
  const firstAnswer = correctSlot(main);
  await drag(page, point(main.sourcePx, main.sourceActiveVertex), point(main.targetPx, firstAnswer));
  const firstSolved = await waitFor(page, x => x.solved === 1, 'first main answer');
  await page.screenshot({ path: path.join(FRAMES, '04-first-main-success.png') });

  result.afterFix = {
    sameInputAfter16Seconds: {
      onboarding: afterNoInput.onboarding,
      currentProblemId: afterNoInput.currentProblemId,
      solved: afterNoInput.solved,
      submissionCount: afterNoInput.submissionCount,
    },
    wrongTap: {
      onboarding: afterWrongTap.onboarding,
      currentProblemId: afterWrongTap.currentProblemId,
      solved: afterWrongTap.solved,
      submissionCount: afterWrongTap.submissionCount,
      inputVersionAdvanced: afterWrongTap.inputVersion > s.inputVersion,
    },
    manualPractice: {
      mathematicalAnswer: 'B↔F; △ABC∽△DFE',
      slot: practiceAnswer,
      enteredMain: !main.onboarding,
      currentProblemId: main.currentProblemId,
    },
    firstMainSuccess: {
      independentlySolvedSlot: firstAnswer,
      solved: firstSolved.solved,
      firstCorrect: firstSolved.firstCorrect,
      lives: firstSolved.lives,
    },
  };
  result.passed = afterWrongTap.onboarding && afterWrongTap.submissionCount === 0 &&
    afterNoInput.onboarding && afterNoInput.currentProblemId === 'practice-345' &&
    afterNoInput.solved === 0 && afterNoInput.submissionCount === 0 &&
    !main.onboarding && firstSolved.solved === 1 && firstSolved.firstCorrect === 1 &&
    consoleErrors.length === 0;
  fs.writeFileSync(path.join(HERE, 'fix2-regression-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await page.close();
  await browser.close();
  await server.close();
}
