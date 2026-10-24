#!/usr/bin/env node
// 나비 재봉소 실제 포인터 검증: 고정 연습, 정상 6날개, 오답 회복, 숙달 불가 gameover.
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
function edgeId(name) { return name === 'AB' ? 0 : name === 'AC' ? 1 : 2; }
function answerEdges(problem) {
  const m = problem.answer.match(/중심에 (AB|AC|BC), .*중심에 (AB|AC|BC)/);
  if (!m) throw new Error(`대응변 파싱 실패: ${problem.answer}`);
  return [edgeId(m[1]), edgeId(m[2])];
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
  const [va, vb] = edgeVertices(edge);
  await drag(page, sourcePoint(s, va), sourcePoint(s, vb));
}
async function drawArc(page, centerIndex, edge) {
  await selectEdge(page, edge);
  let s = await state(page);
  const hinge = { x: s.hingeScreenX, y: s.hingeScreenY };
  const center = centerIndex === 0 ? { x: s.targetLeftScreenX, y: s.targetLeftScreenY } :
    { x: s.targetRightScreenX, y: s.targetRightScreenY };
  await drag(page, hinge, center);
  s = await state(page);
  const pencil = { x: s.pencilScreenX, y: s.pencilScreenY };
  const a0 = Math.atan2(pencil.y - center.y, pencil.x - center.x);
  const radius = Math.hypot(pencil.x - center.x, pencil.y - center.y);
  const end = { x: center.x + Math.cos(a0 - 1.1) * radius, y: center.y + Math.sin(a0 - 1.1) * radius };
  await drag(page, pencil, end, 20);
}
async function solveCurrent(page, useCorrectPair = true) {
  const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(360));
  let s = await state(page);
  if (s.tool !== 'compass') {
    await page.mouse.click(s.compassButtonScreenX, s.compassButtonScreenY);
    await sleep(120);
    s = await state(page);
  }
  let pair;
  if (s.problemId === 'practice-fixed') pair = [1, 2];
  else {
    const problem = bank.find((p) => p.id === s.problemId);
    if (!problem) throw new Error(`은행에 없는 현재 문제: ${s.problemId}`);
    pair = answerEdges(problem);
  }
  if (!useCorrectPair) pair = [pair[1], pair[0]];
  // 고정 연습은 왼쪽 원호가 시범으로 주어져 학생은 나머지 한 원호부터 직접 잇는다.
  if (s.problemId !== 'practice-fixed') await drawArc(page, 0, pair[0]);
  await drawArc(page, 1, pair[1]);
  s = await state(page);
  await page.mouse.click(s.rulerButtonScreenX, s.rulerButtonScreenY);
  await sleep(120);
  s = await state(page);
  const left = { x: s.targetLeftScreenX, y: s.targetLeftScreenY };
  const right = { x: s.targetRightScreenX, y: s.targetRightScreenY };
  const [a, b] = intersections(left, right, edgeLength(s, pair[0]), edgeLength(s, pair[1]));
  const apex = a.y < b.y ? a : b;
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
await shot(normal, '00-title');
await normal.mouse.click(195, 760);
await normal.waitForFunction('window.__GAME_TEST__.getState().phase === "practice"', { timeout: 4000 });
await shot(normal, '01-practice-guide');
const practiceReveal = await solveCurrent(normal, true);
if (practiceReveal.phase !== 'reveal' || practiceReveal.score <= 0) throw new Error(`연습 작도 실패: ${JSON.stringify(practiceReveal)}`);
await waitPastReveal(normal);
await shot(normal, '02-first-order');
for (let i = 0; i < 6; i++) {
  const reveal = await solveCurrent(normal, true);
  if (reveal.phase !== 'reveal' || reveal.firstCorrect !== i + 1) throw new Error(`정상 ${i + 1}번 날개 실패: ${JSON.stringify(reveal)}`);
  if (i < 5) await waitPastReveal(normal);
}
await normal.waitForFunction('window.__GAME_TEST__.getState().phase === "clear"', { timeout: 5000 });
const normalEnd = await state(normal);
await shot(normal, '03-normal-clear');
await normal.close();

// 회복/실패판: 첫 날개를 실제 포인터로 틀린 뒤 고치고, 다음 날개의 두 번째 첫 실수로 숙달 불가 종료.
const recovery = await makePage();
await recovery.evaluate(() => window.__GAME_TEST__.start());
await sleep(200);
const wrongOne = await solveCurrent(recovery, false);
if (wrongOne.lives !== 2 || wrongOne.firstCorrect !== 0) throw new Error(`첫 오답 손실 실패: ${JSON.stringify(wrongOne)}`);
await shot(recovery, '04-wrong-ghost');
await waitPastReveal(recovery);
const recovered = await solveCurrent(recovery, true);
if (recovered.score <= 0 || recovered.solved !== 1) throw new Error(`오답 회복 실패: ${JSON.stringify(recovered)}`);
await waitPastReveal(recovery);
await shot(recovery, '05-recovered-next-wing');
const secondFirstMiss = await solveCurrent(recovery, false);
if (secondFirstMiss.firstAttempts !== 2) throw new Error(`두 번째 첫 제출 기록 실패: ${JSON.stringify(secondFirstMiss)}`);
await recovery.waitForFunction('window.__GAME_TEST__.getState().phase === "gameover"', { timeout: 5000 });
const failEnd = await state(recovery);
await shot(recovery, '06-mastery-gameover');
await recovery.close();

await browser.close();
const result = {
  url,
  normal: normalEnd,
  recovery: { firstWrong: wrongOne, recovered, secondFirstMiss, end: failEnd },
  errors,
  screenshots: snapshots.map((p) => path.relative(root, p)),
  passed: normalEnd.phase === 'clear' && normalEnd.solved === 6 && normalEnd.firstCorrect === 6 &&
    failEnd.phase === 'gameover' && failEnd.firstAttempts === 2 && errors.length === 0,
};
fs.writeFileSync(path.join(import.meta.dirname, 'pointer-playthrough-results.json'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
if (!result.passed) process.exitCode = 1;
