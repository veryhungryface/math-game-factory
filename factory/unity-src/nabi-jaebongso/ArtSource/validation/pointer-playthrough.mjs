#!/usr/bin/env node
// 나비 재봉소 실제 포인터 검증: 화면 발문만 읽는 독립 2회, 원호 구간 회귀, 오답 회복.
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';

const root = path.resolve(import.meta.dirname, '../../../../..');
const outDir = path.join(import.meta.dirname, 'pointer-playthrough');
fs.mkdirSync(outDir, { recursive: true });
const url = process.env.NABI_URL || 'http://127.0.0.1:4321/g/nabi-jaebongso/';
const chrome = process.env.PUPPETEER_EXECUTABLE_PATH ||
  path.join(process.env.HOME, '.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');

const browser = await puppeteer.launch({ headless: true, executablePath: chrome, args: ['--no-sandbox', '--disable-dev-shm-usage'] });
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const errors = [];
const snapshots = [];

async function makePage() {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', (e) => errors.push(String(e)));
  await page.goto(url, { waitUntil: 'networkidle2', timeout: 60000 });
  await page.waitForFunction('window.__GAME_TEST__?.ready === true', { timeout: 30000 });
  return page;
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function drag(page, a, b, steps = 12) {
  await page.mouse.move(a.x, a.y);
  await page.mouse.down();
  await page.mouse.move(b.x, b.y, { steps });
  await page.mouse.up();
  await sleep(130);
}
function sourcePoint(s, v) {
  return v === 0 ? { x: s.sourceAScreenX, y: s.sourceAScreenY } :
    v === 1 ? { x: s.sourceBScreenX, y: s.sourceBScreenY } : { x: s.sourceCScreenX, y: s.sourceCScreenY };
}
function edgeVertices(edge) { return edge === 0 ? [0, 1] : edge === 1 ? [0, 2] : [1, 2]; }
function edgeLength(s, edge) {
  const [a, b] = edgeVertices(edge).map((v) => sourcePoint(s, v));
  return Math.hypot(a.x - b.x, a.y - b.y);
}
function edgeBetween(a, b) {
  const lo = Math.min(a, b), hi = Math.max(a, b);
  return lo === 0 && hi === 1 ? 0 : lo === 0 && hi === 2 ? 1 : 2;
}
function pairFromVisiblePrompt(prompt) {
  const orderMatch = prompt.match(/△ABC≡△([DEF]{3})/);
  const baseMatch = prompt.match(/선분\s*([DEF])([DEF])=(AB|AC|BC)/);
  if (!orderMatch || !baseMatch) throw new Error(`화면 발문 파싱 실패: ${prompt}`);
  const order = orderMatch[1];
  const leftSource = order.indexOf(baseMatch[1]);
  const rightSource = order.indexOf(baseMatch[2]);
  const apexSource = [0, 1, 2].find((v) => v !== leftSource && v !== rightSource);
  if (leftSource < 0 || rightSource < 0 || apexSource === undefined)
    throw new Error(`화면 합동 순서 대응 실패: ${prompt}`);
  return [edgeBetween(leftSource, apexSource), edgeBetween(rightSource, apexSource)];
}
function intersections(p0, p1, r0, r1) {
  const dx = p1.x - p0.x, dy = p1.y - p0.y, d = Math.hypot(dx, dy);
  const x = (r0 * r0 - r1 * r1 + d * d) / (2 * d);
  const h = Math.sqrt(Math.max(0, r0 * r0 - x * x));
  const ux = dx / d, uy = dy / d;
  const mid = { x: p0.x + ux * x, y: p0.y + uy * x };
  return [
    { x: mid.x - uy * h, y: mid.y + ux * h },
    { x: mid.x + uy * h, y: mid.y - ux * h },
  ];
}

async function selectEdge(page, edge) {
  const s = await state(page);
  let [va, vb] = edgeVertices(edge);
  if (s.problemId === 'practice-fixed' && edge === 2) {
    const a = sourcePoint(s, va), b = sourcePoint(s, vb);
    const dx = b.x - a.x, dy = b.y - a.y, length = Math.hypot(dx, dy);
    const offset = { x: -dy / length * 8, y: dx / length * 8 };
    const at = (t) => ({ x: a.x + dx * t + offset.x, y: a.y + dy * t + offset.y });
    // 꼭짓점 정중앙이 아닌 B→C 복도 안쪽에서 시작·종료해 넓어진 첫 조작 허용폭을 검증한다.
    await drag(page, at(0.28), at(0.82));
    return;
  }
  // 이미 놓인 컴퍼스 연필이 한 꼭짓점 위에 겹치면 그 점은 원호 손잡이로 해석된다.
  // 같은 변의 반대 꼭짓점에서 시작해 실제 학생에게도 남아 있는 비가림 경로를 사용한다.
  const pencil = { x: s.pencilScreenX, y: s.pencilScreenY };
  if (Math.hypot(sourcePoint(s, va).x - pencil.x, sourcePoint(s, va).y - pencil.y) <
      Math.hypot(sourcePoint(s, vb).x - pencil.x, sourcePoint(s, vb).y - pencil.y)) [va, vb] = [vb, va];
  await drag(page, sourcePoint(s, va), sourcePoint(s, vb));
}
function localAngle(center, point) {
  return Math.atan2(center.y - point.y, point.x - center.x);
}
function normalizeAngle(value) {
  while (value > Math.PI) value -= Math.PI * 2;
  while (value < -Math.PI) value += Math.PI * 2;
  return value;
}
function screenPointAt(center, radius, angle) {
  return { x: center.x + Math.cos(angle) * radius, y: center.y - Math.sin(angle) * radius };
}
function awaySweep(center, radius, start, intersectionsAtBothSides) {
  const blocked = intersectionsAtBothSides.map((p) => localAngle(center, p));
  const sweep = 0.36;
  const margin = 0.12;
  const onCanvas = (angle) => {
    const p = screenPointAt(center, radius, angle);
    return p.x >= 6 && p.x <= 384 && p.y >= 118 && p.y <= 738;
  };
  // 화면에 실제로 보이는 연필 손잡이에서 시작하되 두 교점 어느 쪽도 지나지 않는다.
  for (const direction of [1, -1]) {
    const end = start + direction * sweep;
    const containsBlocked = blocked.some((angle) => {
      const delta = normalizeAngle(angle - start);
      return direction > 0 ? delta >= -margin && delta <= sweep + margin : delta <= margin && delta >= -sweep - margin;
    });
    if (!containsBlocked && onCanvas(end)) return [start, end];
  }
  throw new Error('현재 연필 위치에서 가상 교점 둘을 피하는 짧은 원호를 찾지 못함');
}
async function drawArc(page, centerIndex, edge, apex, mode = 'through', bothIntersections = []) {
  await selectEdge(page, edge);
  let s = await state(page);
  const hinge = { x: s.hingeScreenX, y: s.hingeScreenY };
  const center = centerIndex === 0 ? { x: s.targetLeftScreenX, y: s.targetLeftScreenY } :
    { x: s.targetRightScreenX, y: s.targetRightScreenY };
  await drag(page, hinge, center);
  s = await state(page);
  const radius = edgeLength(s, edge);
  const pencil = { x: s.pencilScreenX, y: s.pencilScreenY };
  const pencilAngle = localAngle(center, pencil);
  let startAngle, endAngle;
  if (mode === 'away') {
    [startAngle, endAngle] = awaySweep(center, radius, pencilAngle, bothIntersections);
  } else {
    const apexAngle = localAngle(center, apex);
    startAngle = pencilAngle;
    let delta = normalizeAngle(apexAngle - pencilAngle);
    if (Math.abs(delta) < 0.34) delta = (delta < 0 ? -1 : 1) * 0.38;
    else delta += (delta < 0 ? -1 : 1) * 0.10;
    endAngle = pencilAngle + delta;
  }
  await drag(page, pencil, screenPointAt(center, radius, endAngle), 20);
}
async function solveCurrent(page, mode = 'correct', choices = []) {
  let s = await state(page);
  if (s.tool !== 'compass') {
    await page.mouse.click(s.compassButtonScreenX, s.compassButtonScreenY);
    await sleep(120);
    s = await state(page);
  }
  const derivedPair = pairFromVisiblePrompt(s.screenPrompt);
  const pair = mode === 'swapped' ? [derivedPair[1], derivedPair[0]] : [...derivedPair];
  const left = { x: s.targetLeftScreenX, y: s.targetLeftScreenY };
  const right = { x: s.targetRightScreenX, y: s.targetRightScreenY };
  const bothIntersections = intersections(left, right, edgeLength(s, pair[0]), edgeLength(s, pair[1]));
  const apex = bothIntersections[0].y < bothIntersections[1].y ? bothIntersections[0] : bothIntersections[1];
  choices.push({
    problemId: s.problemId,
    visiblePrompt: s.screenPrompt,
    derivedPair,
    submittedPair: pair,
    mode,
  });
  // 고정 연습은 왼쪽 원호가 시범으로 주어져 학생은 나머지 한 원호부터 직접 잇는다.
  if (s.problemId !== 'practice-fixed')
    await drawArc(page, 0, pair[0], apex, mode === 'short-away' ? 'away' : 'through', bothIntersections);
  await drawArc(page, 1, pair[1], apex, mode === 'short-away' ? 'away' : 'through', bothIntersections);
  s = await state(page);
  await page.mouse.click(s.rulerButtonScreenX, s.rulerButtonScreenY);
  await sleep(120);
  s = await state(page);
  await drag(page, left, apex, 16);
  await drag(page, right, apex, 16);
  await sleep(180);
  return state(page);
}
async function waitPastReveal(page) {
  await page.waitForFunction('window.__GAME_TEST__.getState().phase !== "reveal"', { timeout: 5000 });
  return state(page);
}
async function shot(page, name) {
  const file = path.join(outDir, name + '.png');
  await page.screenshot({ path: file });
  snapshots.push(file);
}

// 정상판: 제목 CTA도 포인터로 누르고, 고정 연습부터 여섯 날개까지 모두 작도한다.
const normal = await makePage();
const normalChoices = [];
await shot(normal, '00-title');
await normal.mouse.click(195, 760);
await normal.waitForFunction('window.__GAME_TEST__.getState().phase === "practice"', { timeout: 4000 });
await shot(normal, '01-practice-guide');
// 6초 무진전 뒤 강한 귀신손+문구가 나오는 동일 첫 플레이에서,
// 끝점 정중앙이 아닌 BC 레일 안쪽 드래그로 15초 안에 중심 이동 단계까지 진전한다.
const onboardingStart = Date.now();
await sleep(6200);
await shot(normal, '01b-strong-guide');
await selectEdge(normal, 2);
let onboardingState = await state(normal);
if (onboardingState.compassSourceEdge !== 2)
  throw new Error(`넓은 BC 레일이 폭을 잡지 못함: ${JSON.stringify(onboardingState)}`);
await drag(normal,
  { x: onboardingState.hingeScreenX, y: onboardingState.hingeScreenY },
  { x: onboardingState.targetRightScreenX, y: onboardingState.targetRightScreenY });
onboardingState = await state(normal);
const onboardingElapsedMs = Date.now() - onboardingStart;
if (onboardingState.compassCenter !== 1 || onboardingElapsedMs >= 15000)
  throw new Error(`15초 안에 중심 이동 단계 진입 실패: ${JSON.stringify({ onboardingElapsedMs, onboardingState })}`);
await shot(normal, '01c-center-within-15s');
const practiceReveal = await solveCurrent(normal, 'correct', normalChoices);
if (practiceReveal.phase !== 'reveal' || practiceReveal.score <= 0) throw new Error(`연습 작도 실패: ${JSON.stringify(practiceReveal)}`);
await waitPastReveal(normal);
await shot(normal, '02-first-order');
for (let i = 0; i < 6; i++) {
  const reveal = await solveCurrent(normal, 'correct', normalChoices);
  if (reveal.phase !== 'reveal' || reveal.firstCorrect !== i + 1) throw new Error(`정상 ${i + 1}번 날개 실패: ${JSON.stringify(reveal)}`);
  if (i < 5) await waitPastReveal(normal);
}
await normal.waitForFunction('window.__GAME_TEST__.getState().phase === "clear"', { timeout: 5000 });
const normalEnd = await state(normal);
await shot(normal, '03-normal-clear');
await normal.close();

// 회복 클리어: 첫 날개를 틀린 뒤 같은 문제를 고치고 남은 5개를 첫 제출 정답으로 완주한다.
const recovery = await makePage();
const recoveryChoices = [];
await recovery.evaluate(() => window.__GAME_TEST__.start());
await sleep(200);
const wrongOne = await solveCurrent(recovery, 'swapped', recoveryChoices);
if (wrongOne.lives !== 2 || wrongOne.firstCorrect !== 0) throw new Error(`첫 오답 손실 실패: ${JSON.stringify(wrongOne)}`);
await shot(recovery, '04-wrong-ghost');
await waitPastReveal(recovery);
const recovered = await solveCurrent(recovery, 'correct', recoveryChoices);
if (recovered.score <= 0 || recovered.solved !== 1) throw new Error(`오답 회복 실패: ${JSON.stringify(recovered)}`);
await waitPastReveal(recovery);
await shot(recovery, '05-recovered-next-wing');
for (let i = 1; i < 6; i++) {
  const reveal = await solveCurrent(recovery, 'correct', recoveryChoices);
  if (reveal.phase !== 'reveal' || reveal.solved !== i + 1 || reveal.firstCorrect !== i)
    throw new Error(`회복 완주 ${i + 1}번 실패: ${JSON.stringify(reveal)}`);
  if (i < 5) await waitPastReveal(recovery);
}
await recovery.waitForFunction('window.__GAME_TEST__.getState().phase === "clear"', { timeout: 5000 });
const recoveryEnd = await state(recovery);
if (recoveryEnd.solved !== 6 || recoveryEnd.firstCorrect !== 5 || recoveryEnd.lives !== 2)
  throw new Error(`회복 클리어 최종 불일치: ${JSON.stringify(recoveryEnd)}`);
await shot(recovery, '06-recovery-clear');
await recovery.close();

// 숙달 불가 gameover는 별도 페이지에서 두 문제의 첫 제출을 틀려 증명한다.
const gameover = await makePage();
const gameoverChoices = [];
await gameover.evaluate(() => window.__GAME_TEST__.start());
await sleep(200);
const gameoverFirstMiss = await solveCurrent(gameover, 'swapped', gameoverChoices);
await waitPastReveal(gameover);
const gameoverRecovered = await solveCurrent(gameover, 'correct', gameoverChoices);
await waitPastReveal(gameover);
const secondFirstMiss = await solveCurrent(gameover, 'swapped', gameoverChoices);
if (secondFirstMiss.firstAttempts !== 2) throw new Error(`두 번째 첫 제출 기록 실패: ${JSON.stringify(secondFirstMiss)}`);
await gameover.waitForFunction('window.__GAME_TEST__.getState().phase === "gameover"', { timeout: 5000 });
const failEnd = await state(gameover);
await shot(gameover, '07-mastery-gameover');
await gameover.close();

// mathcheck 회귀: 올바른 두 반지름과 가상 원 교점까지의 선분은 그대로 두되,
// 실제 원호 두 구간을 교점에서 떼어 놓는다. 수정 전에는 정답이던 같은 입력이 이제 거부되어야 한다.
let shortArcResult = null;
let regressionChoices = [];
let arcRegressionAttempts = 0;
for (let attempt = 1; attempt <= 5; attempt++) {
  arcRegressionAttempts = attempt;
  const arcRegression = await makePage();
  const trialChoices = [];
  await arcRegression.evaluate(() => window.__GAME_TEST__.start());
  await sleep(200);
  try {
    const trial = await solveCurrent(arcRegression, 'short-away', trialChoices);
    if (trial.phase === 'reveal' && trial.lives === 2 && trial.solved === 0 &&
        trial.misconceptionId === 'one_radius_only') {
      shortArcResult = trial;
      regressionChoices = trialChoices;
      await shot(arcRegression, '08-short-arcs-rejected');
      await arcRegression.close();
      break;
    }
  } catch (error) {
    if (!String(error).includes('현재 연필 위치')) throw error;
  }
  await arcRegression.close();
}
if (!shortArcResult)
  throw new Error(`5개 독립 문항에서 짧은 원호 가상 교점 거부 증거를 만들지 못함`);

// 독립 두 번째 정상판: 문제은행 answer를 전혀 읽지 않고 화면 TMP 발문만 파싱해 매번 대응변을 계산한다.
const second = await makePage();
const secondChoices = [];
await second.evaluate(() => window.__GAME_TEST__.start());
await sleep(200);
for (let i = 0; i < 6; i++) {
  const reveal = await solveCurrent(second, 'correct', secondChoices);
  if (reveal.phase !== 'reveal' || reveal.firstCorrect !== i + 1)
    throw new Error(`독립 2회차 ${i + 1}번 실패: ${JSON.stringify(reveal)}`);
  if (i < 5) await waitPastReveal(second);
}
await second.waitForFunction('window.__GAME_TEST__.getState().phase === "clear"', { timeout: 5000 });
const secondEnd = await state(second);
await shot(second, '09-independent-second-clear');
await second.close();

const firstRunChoices = normalChoices.filter((c) => c.problemId !== 'practice-fixed');
const distinctProblemDeck = firstRunChoices.map((c) => c.problemId).join(',') !== secondChoices.map((c) => c.problemId).join(',');
const distinctDerivedChoices = firstRunChoices.some((c, i) =>
  c.derivedPair.join(',') !== secondChoices[i]?.derivedPair.join(','));
if (!distinctProblemDeck || !distinctDerivedChoices)
  throw new Error(`독립 2회차 문항/선택 차이 부족: ${JSON.stringify({ firstRunChoices, secondChoices })}`);

await browser.close();
const result = {
  url,
  onboarding: {
    waitedForStrongGuideMs: 6200,
    elapsedToCenterStageMs: onboardingElapsedMs,
    usedCorridorInterior: { startT: 0.28, endT: 0.82, perpendicularOffsetCss: 8 },
    state: onboardingState,
    passed: onboardingState.compassSourceEdge === 2 && onboardingState.compassCenter === 1 && onboardingElapsedMs < 15000,
  },
  normal: normalEnd,
  recoveryClear: { firstWrong: wrongOne, repairedSameProblem: recovered, end: recoveryEnd, choices: recoveryChoices },
  masteryGameover: { firstMiss: gameoverFirstMiss, recovered: gameoverRecovered, secondFirstMiss, end: failEnd, choices: gameoverChoices },
  arcSweepRegression: {
    priorBehavior: 'same correct radii and virtual-intersection segments were accepted even when the drawn arcStart..arcEnd ranges missed the intersection',
    after: shortArcResult,
    choices: regressionChoices,
    attempts: arcRegressionAttempts,
    passed: shortArcResult.lives === 2 && shortArcResult.solved === 0 && shortArcResult.misconceptionId === 'one_radius_only',
  },
  independentVisiblePromptRuns: {
    answerApiCalls: 0,
    first: firstRunChoices,
    second: secondChoices,
    distinctProblemDeck,
    distinctDerivedChoices,
    secondEnd,
  },
  errors,
  screenshots: snapshots.map((p) => path.relative(root, p)),
  passed: normalEnd.phase === 'clear' && normalEnd.solved === 6 && normalEnd.firstCorrect === 6 &&
    onboardingState.compassCenter === 1 && onboardingElapsedMs < 15000 &&
    recoveryEnd.phase === 'clear' && recoveryEnd.solved === 6 && recoveryEnd.firstCorrect === 5 && recoveryEnd.lives === 2 &&
    failEnd.phase === 'gameover' && failEnd.firstAttempts === 2 &&
    shortArcResult.lives === 2 && shortArcResult.solved === 0 &&
    secondEnd.phase === 'clear' && secondEnd.firstCorrect === 6 &&
    distinctProblemDeck && distinctDerivedChoices && errors.length === 0,
};
fs.writeFileSync(path.join(import.meta.dirname, 'pointer-playthrough-results.json'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
if (!result.passed) process.exitCode = 1;
