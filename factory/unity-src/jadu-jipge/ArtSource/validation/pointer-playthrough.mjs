import fs from 'node:fs';
import crypto from 'node:crypto';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(import.meta.dirname, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const OUT = path.join(import.meta.dirname, 'pointer-playthrough');
fs.mkdirSync(OUT, { recursive: true });

function webglArtifactHash(gameDir) {
  const buildDir = path.join(gameDir, 'Build');
  const rels = ['index.html', ...fs.readdirSync(buildDir).sort().map(name => `Build/${name}`)];
  const hash = crypto.createHash('sha256');
  for (const rel of rels) {
    hash.update(rel);
    hash.update(fs.readFileSync(path.join(gameDir, rel)));
  }
  return hash.digest('hex');
}

const artifactHash = webglArtifactHash(path.join(PUBLIC, 'g', 'jadu-jipge'));

function resolveChrome() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  const builds = fs.readdirSync(base).filter(name => {
    try { return fs.statSync(path.join(base, name)).isDirectory(); } catch { return false; }
  }).sort().reverse();
  for (const build of builds) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome'
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
  return undefined;
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-setuid-sandbox', '--use-angle=metal']
});
const consoleErrors = [];
const pageErrors = [];
const url = `${server.url}/g/jadu-jipge/`;
let page;

async function openFreshPage() {
  const next = await browser.newPage();
  await next.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
  next.on('console', msg => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });
  next.on('pageerror', err => pageErrors.push(String(err)));
  await next.goto(url, { waitUntil: 'networkidle0', timeout: 45000 });
  await next.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 20000 });
  return next;
}

page = await openFreshPage();

const state = () => page.evaluate(() => window.__GAME_TEST__.getState());
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

async function heldClick(x, y) {
  await page.mouse.move(x, y);
  await page.mouse.down();
  await sleep(120);
  await page.mouse.up();
  await sleep(350);
}

function answerFromPrompt(prompt) {
  const refs = prompt.match(/AB=(\d+) cm, AC=(\d+) cm, ∠A=(\d+)°/);
  const ratio = prompt.match(/닮음비=(\d+):1/);
  if (!refs || !ratio) throw new Error(`prompt parse: ${prompt}`);
  const q = Number(ratio[1]);
  return { u: Number(refs[1]) / q, v: Number(refs[2]) / q, deg: Number(refs[3]) };
}

async function dragControl(index, target, currentField) {
  const s = await state();
  const p = s.controlPx;
  const h = { x: p[index * 2], y: p[index * 2 + 1] };
  const j = { x: p[6], y: p[7] };
  let destination;
  if (index < 2) {
    const current = s[currentField];
    const vx = (h.x - j.x) / current;
    const vy = (h.y - j.y) / current;
    destination = { x: j.x + vx * target, y: j.y + vy * target };
    await page.mouse.move(h.x, h.y);
    await page.mouse.down();
    if (Math.hypot(destination.x - h.x, destination.y - h.y) < 12) {
      const n = Math.hypot(vx, vy) || 1;
      await page.mouse.move(h.x - vy / n * 28, h.y + vx / n * 28, { steps: 4 });
      await sleep(80);
    }
  } else {
    const angleIndex = (target - 30) / 15;
    destination = { x: j.x - 84 + angleIndex * 21, y: h.y };
    await page.mouse.move(h.x, h.y);
    await page.mouse.down();
    if (Math.hypot(destination.x - h.x, destination.y - h.y) < 12) {
      await page.mouse.move(h.x + 26, h.y, { steps: 4 });
      await sleep(80);
    }
  }
  await page.mouse.move(destination.x, destination.y, { steps: 7 });
  await sleep(90);
  await page.mouse.up();
  await sleep(120);
}

async function submit(values) {
  const before = await state();
  await dragControl(0, values.u, 'currentU');
  await dragControl(1, values.v, 'currentV');
  if (before.level >= 3) await dragControl(2, values.deg, 'currentAngleIndex');
  await sleep(180);
  return state();
}

// 수정 전 결함 재현 입력: 고정 연습에서 DE=12 cm, DF=9 cm를 제출한다.
// 수정 후에는 점수·패드·진도 변화 없이 같은 연습에 남아야 하며, 올바른
// DE=4 cm, DF=6 cm를 실제 포인터로 맞춘 뒤에만 첫 본문항으로 넘어간다.
await heldClick(195, 422);
const practiceStart = await state();
if (!practiceStart.onboarding || practiceStart.problemId !== 'practice')
  throw new Error(`practice did not start from real title pointer: ${JSON.stringify(practiceStart)}`);
const practiceWrongReveal = await submit({ u: 12, v: 9, deg: 60 });
await page.screenshot({ path: path.join(OUT, '00-practice-wrong-reveal.png') });
await sleep(1750);
const practiceRetry = await state();
await page.screenshot({ path: path.join(OUT, '00b-practice-retry.png') });
if (!practiceRetry.onboarding || practiceRetry.problemId !== 'practice' || practiceRetry.solved !== 0
    || practiceRetry.score !== 0 || practiceRetry.lives !== 3)
  throw new Error(`wrong practice escaped or spent run state: ${JSON.stringify(practiceRetry)}`);
const practiceCorrectReveal = await submit({ u: 4, v: 6, deg: 60 });
await page.screenshot({ path: path.join(OUT, '00c-practice-correct-reveal.png') });
await sleep(1750);
const startState = await state();
await page.screenshot({ path: path.join(OUT, '00d-main-after-practice.png') });
if (startState.onboarding || startState.order !== 1 || startState.solved !== 0)
  throw new Error(`correct practice did not enter main run: ${JSON.stringify(startState)}`);
const correct0 = answerFromPrompt(startState.prompt);
const firstRunPrompts = [startState.prompt];
let earlyAngleLocks = 0;
function assertEarlyAngleLocked(s, answer) {
  if (s.level >= 3) return;
  const expectedIndex = (answer.deg - 30) / 15;
  if (s.currentAngleIndex !== expectedIndex)
    throw new Error(`early included angle not locked: ${JSON.stringify({ state: s, answer })}`);
  earlyAngleLocks++;
}
assertEarlyAngleLocked(startState, correct0);
const wrong = correct0.u !== correct0.v
  ? { ...correct0, u: correct0.v, v: correct0.u }
  : { ...correct0, u: correct0.u === 12 ? 11 : correct0.u + 1 };
const afterWrong = await submit(wrong);
await page.screenshot({ path: path.join(OUT, '01-first-mistake.png') });
if (afterWrong.lives !== 2 || afterWrong.firstAttemptWrong !== 1)
  throw new Error(`real wrong path failed: ${JSON.stringify(afterWrong)}`);
if (correct0.u !== correct0.v && afterWrong.misconceptionId !== 'correspondence_order')
  throw new Error(`swapped correspondence was not rejected: ${JSON.stringify(afterWrong)}`);

await sleep(1450);
const retryState = await state();
const retryAnswer = answerFromPrompt(retryState.prompt);
const afterRetry = await submit(retryAnswer);
if (afterRetry.solved !== 1 || afterRetry.score <= 0)
  throw new Error(`real retry path failed: ${JSON.stringify(afterRetry)}`);
await page.screenshot({ path: path.join(OUT, '02-recovered.png') });

for (let expectedSolved = 2; expectedSolved <= 10; expectedSolved++) {
  await sleep(1600);
  const s = await state();
  firstRunPrompts.push(s.prompt);
  const answer = answerFromPrompt(s.prompt);
  assertEarlyAngleLocked(s, answer);
  const after = await submit(answer);
  if (after.solved !== expectedSolved)
    throw new Error(`solve ${expectedSolved}: ${JSON.stringify(after)}`);
}
await sleep(1750);
const finalState = await state();
await page.screenshot({ path: path.join(OUT, '03-clear-after-recovery.png') });
if (finalState.phase !== 'clear' || finalState.firstAttemptCorrect !== 9 || finalState.band3FirstCorrect !== 4)
  throw new Error(`recovery clear failed: ${JSON.stringify(finalState)}`);

// 동일 WebGL 산출물을 새 페이지에서 다시 로드해 첫 판과 독립된 두 번째 판을
// 실제 포인터만으로 푼다. 대응 순서가 달라진 문항과 SAS 끼인각 조절을 모두
// 거쳤는지 기록하고, 결과 화면을 실제로 눌러 자발적 재도전 경로도 확인한다.
await page.close();
page = await openFreshPage();
await page.evaluate(() => window.__GAME_TEST__.start());
const secondRunPrompts = [];
for (let expectedSolved = 1; expectedSolved <= 10; expectedSolved++) {
  const s = await state();
  secondRunPrompts.push(s.prompt);
  const answer = answerFromPrompt(s.prompt);
  assertEarlyAngleLocked(s, answer);
  const after = await submit(answer);
  if (after.solved !== expectedSolved)
    throw new Error(`second play solve ${expectedSolved}: ${JSON.stringify(after)}`);
  await sleep(expectedSolved === 10 ? 1750 : 1600);
}
const secondFinal = await state();
await page.screenshot({ path: path.join(OUT, '04-second-play-clear.png') });
if (secondFinal.phase !== 'clear' || secondFinal.firstAttemptCorrect !== 10 || secondFinal.band3FirstCorrect !== 4)
  throw new Error(`second play clear failed: ${JSON.stringify(secondFinal)}`);

const triangleOf = prompt => prompt.match(/△ABC∽(△[A-F]{3})/)?.[1] || '';
const distinctCorrespondenceOrders = secondRunPrompts.filter((prompt, i) =>
  triangleOf(prompt) !== triangleOf(firstRunPrompts[i])).length;
const rotatedCorrespondenceProblems = secondRunPrompts.filter(prompt => triangleOf(prompt) !== '△DEF').length;
const sasAngleProblems = secondRunPrompts.filter(prompt => prompt.includes('두 변과 그 끼인각')).length;
if (distinctCorrespondenceOrders < 1 || rotatedCorrespondenceProblems < 1 || sasAngleProblems !== 4)
  throw new Error(`second play coverage missing: ${JSON.stringify({ distinctCorrespondenceOrders, rotatedCorrespondenceProblems, sasAngleProblems })}`);

await heldClick(195, 422);
const replayAfterClear = await state();
await page.screenshot({ path: path.join(OUT, '05-replay-after-second-clear.png') });
if (replayAfterClear.phase !== 'playing' || replayAfterClear.solved !== 0 || replayAfterClear.order !== 1)
  throw new Error(`real replay action failed: ${JSON.stringify(replayAfterClear)}`);

const result = {
  generated_at: new Date().toISOString(),
  url,
  artifact_hash: artifactHash,
  viewport: { width: 390, height: 844 },
  practice: {
    start: practiceStart,
    wrong_reveal: practiceWrongReveal,
    retry_same_problem: practiceRetry,
    correct_reveal: practiceCorrectReveal,
    main_after_correct_only: startState
  },
  start: startState,
  after_wrong: afterWrong,
  after_retry: afterRetry,
  final: finalState,
  second_play: {
    prompts: secondRunPrompts,
    final: secondFinal,
    distinct_correspondence_orders_vs_first: distinctCorrespondenceOrders,
    rotated_correspondence_problems: rotatedCorrespondenceProblems,
    sas_angle_problems: sasAngleProblems,
    replay_action: 'result-screen real pointer click',
    replay_after_clear: replayAfterClear
  },
  early_angle_locks_verified: earlyAngleLocks,
  console_errors: consoleErrors,
  page_errors: pageErrors,
  passed: consoleErrors.length === 0 && pageErrors.length === 0 && finalState.phase === 'clear'
    && secondFinal.phase === 'clear' && practiceRetry.onboarding && !startState.onboarding
};
fs.writeFileSync(path.join(import.meta.dirname, 'pointer-playthrough-results.json'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
await browser.close();
await server.close();
if (!result.passed) process.exit(1);
