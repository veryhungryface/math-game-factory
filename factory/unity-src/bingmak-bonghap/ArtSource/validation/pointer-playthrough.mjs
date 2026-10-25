#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../../../../..');
const publicDir = path.join(root, 'public');
const outDir = path.join(here, 'pointer-playthrough');
fs.mkdirSync(outDir, { recursive: true });

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

const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
const state = page => page.evaluate(() => window.__GAME_TEST__.getState());
async function waitState(page, predicate, timeout = 8000) {
  const start = Date.now();
  while (Date.now() - start < timeout) {
    const s = await state(page);
    if (predicate(s)) return s;
    await sleep(80);
  }
  throw new Error(`state timeout: ${JSON.stringify(await state(page))}`);
}

function problemGeometry(problem) {
  const m = problem.answer.match(/target=\(([-\d.]+),([-\d.]+)\)/);
  if (!m) throw new Error(`target missing: ${problem.id}`);
  let rotation = 0, mirror = false;
  const serial = Number(problem.id.replace('bb-', ''));
  if (Number.isFinite(serial)) {
    const g = Math.floor((serial - 1) / 6), c = (serial - 1) % 6;
    const band = g < 20 ? 1 : g < 40 ? 2 : 3;
    rotation = band === 1 ? 0 : [-18, -10, 10, 18][(g + c) % 4];
    mirror = band >= 2 && ((g + c) & 1) === 1;
  }
  return { target: { x: Number(m[1]), y: Number(m[2]) }, rotation, mirror };
}

// 390×844, MgfText CanvasScaler reference와 현재 portrait Relayout을 그대로 계산한다.
function modelToScreen(model, geometry) {
  const width = 390, height = 844;
  const refH = Math.max(112, Math.min(140, height * .145));
  const refY = height * .5 - 92 - refH * .5;
  const workTop = refY - refH * .5 - 57;
  const workBottom = -height * .5 + 69;
  const workCenterY = (workTop + workBottom) * .5;
  const workH = workTop - workBottom;
  const scale = Math.min((358 - 30) / 15.2, (workH - 12 - 30) / 13.2);
  let x = model.x - 5, y = model.y - 4.5;
  if (geometry.mirror) x = -x;
  const a = geometry.rotation * Math.PI / 180;
  const rx = x * Math.cos(a) - y * Math.sin(a);
  const ry = x * Math.sin(a) + y * Math.cos(a);
  // RectTransform의 실제 픽셀 중심은 계산상 중심보다 약 5px 좌상단에 놓인다.
  // 고정 보정은 상태의 fx100/fy100을 실제 포인터 왕복으로 대조해 얻었다.
  return { x: width * .5 + rx * scale + 5, y: height * .5 - (workCenterY + ry * scale) - 5 };
}

async function dragModel(page, from, to, geometry) {
  const a = modelToScreen(from, geometry), b = modelToScreen(to, geometry);
  await page.mouse.move(a.x, a.y);
  await page.mouse.down();
  await page.mouse.move((a.x + b.x) * .5, (a.y + b.y) * .5, { steps: 8 });
  await page.mouse.move(b.x, b.y, { steps: 10 });
  let pointer = { ...b };
  // Held 상태의 실제 C# 좌표를 읽고 pointer 자체를 미세 조정한다. 판정 함수를 부르는 것이 아니라
  // CSS→CanvasScaler→회전/반사 좌표계의 반올림 오차를 실제 드래그로 닫는 검증용 서보다.
  for (let i = 0; i < 5; i++) {
    // 브리지는 변경 상태를 최대 0.2초 주기로 푸시하므로 새 좌표를 읽기 전에 한 주기를 기다린다.
    await sleep(260);
    const s = await state(page);
    let dx = to.x - s.fx100 / 100, dy = to.y - s.fy100 / 100;
    if (Math.hypot(dx, dy) < .06) break;
    if (geometry.mirror) dx = -dx;
    const r = geometry.rotation * Math.PI / 180;
    const rx = dx * Math.cos(r) - dy * Math.sin(r);
    const ry = dx * Math.sin(r) + dy * Math.cos(r);
    const scale = (358 - 30) / 15.2;
    pointer = { x: pointer.x + rx * scale, y: pointer.y - ry * scale };
    await page.mouse.move(pointer.x, pointer.y, { steps: 4 });
  }
  await sleep(260);
  await page.mouse.up();
}

async function solveCurrent(page, bank, { wrongFirst = false } = {}) {
  const before = await state(page);
  const problem = bank.get(before.currentProblemId);
  if (!problem) throw new Error(`problem missing from bank: ${before.currentProblemId}`);
  const geometry = problemGeometry(problem);
  const from = { x: before.fx100 / 100, y: before.fy100 / 100 };
  console.log('[solve]', before.currentProblemId, { from, target: geometry.target, rotation: geometry.rotation, mirror: geometry.mirror, wrongFirst });
  if (wrongFirst) {
    const wrong = Math.hypot(geometry.target.x + .5, geometry.target.y - .5) > 1
      ? { x: -.5, y: .5 } : { x: 10.5, y: .5 };
    await dragModel(page, from, wrong, geometry);
    const wrongState = await waitState(page, s => s.lastResult === 'wrong' && s.lives === before.lives - 1);
    console.log('[wrong-state]', { x: wrongState.fx100 / 100, y: wrongState.fy100 / 100, lives: wrongState.lives });
    await page.screenshot({ path: path.join(outDir, '02-run1-wrong.png') });
    await sleep(1400);
    const repairStart = await state(page);
    await dragModel(page, { x: repairStart.fx100 / 100, y: repairStart.fy100 / 100 }, geometry.target, geometry);
    // 같은 실제 드래그가 정답 피드백으로 들어간 직후를 남긴다. 훅으로 연출을 호출하지 않는다.
    await sleep(520);
    await page.screenshot({ path: path.join(outDir, '02b-run1-repaired-arch.png') });
    console.log('[repair-state]', await state(page));
    const repaired = await waitState(page, s => s.phase === 'clear' ||
      (s.solved === before.solved + 1 && !s.frozen && s.currentProblemId !== before.currentProblemId), 5000);
    return { problemId: before.currentProblemId, wrong: wrongState, repaired };
  }
  await dragModel(page, from, geometry.target, geometry);
  const correct = await waitState(page, s => s.phase === 'clear' ||
    (s.solved === before.solved + 1 && !s.frozen && s.currentProblemId !== before.currentProblemId), 5000);
  return { problemId: before.currentProblemId, correct };
}

const consoleErrors = [], pageErrors = [];
const server = await serveStatic(publicDir);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
let results;
try {
  const page = await browser.newPage();
  page.on('console', m => { if (m.type() === 'error') consoleErrors.push(m.text()); });
  page.on('pageerror', e => pageErrors.push(String(e)));
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  await page.goto(`${server.url}/g/bingmak-bonghap/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });
  const problems = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(1000));
  const bank = new Map(problems.map(p => [p.id, p]));
  await page.screenshot({ path: path.join(outDir, '00-title.png') });

  // 실제 첫 탭과 실제 드래그로 연습을 끝낸다. start/correct/wrong 훅은 호출하지 않는다.
  await page.mouse.click(195, 422);
  await waitState(page, s => s.currentProblemId === 'practice-345');
  await page.screenshot({ path: path.join(outDir, '01-practice.png') });
  await page.mouse.move(210, 520); await page.mouse.down();
  await page.mouse.move(250, 455, { steps: 12 }); await page.mouse.up();
  await waitState(page, s => s.round === 1 && s.currentProblemId !== 'practice-345', 5000);

  const run1 = [];
  run1.push(await solveCurrent(page, bank, { wrongFirst: true }));
  for (let i = 1; i < 8; i++) run1.push(await solveCurrent(page, bank));
  const clear1 = await waitState(page, s => s.phase === 'clear', 5000);
  await page.screenshot({ path: path.join(outDir, '03-run1-clear.png') });

  // 결과 화면 CTA의 실제 pointer로 독립 둘째 판을 시작한다.
  await page.mouse.click(195, 690);
  await waitState(page, s => s.phase === 'playing' && s.round === 1 && s.solved === 0, 5000);
  const run2 = [];
  for (let i = 0; i < 8; i++) run2.push(await solveCurrent(page, bank));
  const clear2 = await waitState(page, s => s.phase === 'clear', 5000);
  await page.screenshot({ path: path.join(outDir, '04-run2-clear.png') });

  results = {
    artifact: fs.readdirSync(path.join(publicDir, 'g', 'bingmak-bonghap', 'Build')).sort(),
    hooksUsed: { start: 0, answerCorrect: 0, answerWrong: 0 },
    onboarding: { method: 'title click + canvas mouse drag', transitionedToRound: 1 },
    run1: { repairProblem: run1[0].problemId, final: clear1, rounds: run1.map(x => x.problemId) },
    run2: { final: clear2, rounds: run2.map(x => x.problemId) },
    consoleErrors, pageErrors,
  };
  fs.writeFileSync(path.join(here, 'pointer-playthrough-results.json'), JSON.stringify(results, null, 2) + '\n');
  console.log(JSON.stringify(results, null, 2));
} finally {
  await browser.close();
  await server.close();
}
