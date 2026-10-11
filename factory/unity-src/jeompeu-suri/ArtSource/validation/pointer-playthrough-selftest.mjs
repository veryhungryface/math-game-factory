import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const FRAMES = path.join(HERE, 'pointer-playthrough');
fs.mkdirSync(FRAMES, { recursive: true });
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
  throw new Error('Chrome for Testing not found');
}

function convex(v) {
  if (new Set(v.map(p => `${p.x},${p.y}`)).size !== 4) return false;
  let sign = 0;
  for (let i = 0; i < 4; i++) {
    const a = v[i], b = v[(i + 1) % 4], c = v[(i + 2) % 4];
    const z = (b.x - a.x) * (c.y - b.y) - (b.y - a.y) * (c.x - b.x);
    if (!z) return false;
    if (sign && Math.sign(z) !== sign) return false;
    sign = Math.sign(z);
  }
  return true;
}

function matches(v, kind) {
  if (!convex(v)) return false;
  const [A, B, C, D] = v;
  const p = A.x + C.x === B.x + D.x && A.y + C.y === B.y + D.y;
  const ac = { x: A.x - C.x, y: A.y - C.y };
  const bd = { x: B.x - D.x, y: B.y - D.y };
  const equal = ac.x ** 2 + ac.y ** 2 === bd.x ** 2 + bd.y ** 2;
  const perp = ac.x * bd.x + ac.y * bd.y === 0;
  return p && (kind === '평행사변형' || (kind === '직사각형' && equal) || (kind === '마름모' && perp) || (kind === '정사각형' && equal && perp));
}

function repair(s, strict = true) {
  const v = Array.from({ length: 4 }, (_, i) => ({ x: s.vertices[i * 2], y: s.vertices[i * 2 + 1] }));
  const found = [];
  for (let i = 0; i < 4; i++) if (s.allowedMask & (1 << i)) for (let y = 0; y < 7; y++) for (let x = 0; x < 7; x++) {
    const q = v.map(p => ({ ...p })); q[i] = { x, y };
    if (matches(q, s.targetPredicate)) found.push({ vertex: i, x, y });
  }
  if (!found.length || (strict && found.length !== 1)) throw new Error(`accepted repairs=${found.length} state=${JSON.stringify(s)}`);
  return found[0];
}

function screenGrid(s, x, y) {
  const x0 = s.gridPx[0], y0 = s.gridPx[1];
  const xx = s.gridPx[2] - x0, xy = s.gridPx[3] - y0;
  const yx = s.gridPx[4] - x0, yy = s.gridPx[5] - y0;
  return [x0 + xx * x + yx * y, y0 + xy * x + yy * y];
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, fn, timeout = 5000) {
  const until = Date.now() + timeout; let s;
  while (Date.now() < until) { s = await state(page); if (fn(s)) return s; await sleep(70); }
  throw new Error(`state timeout: ${JSON.stringify(s)}`);
}
async function drag(page, from, to) {
  await page.mouse.move(from[0], from[1]); await page.mouse.down();
  await page.mouse.move((from[0] + to[0]) / 2, (from[1] + to[1]) / 2, { steps: 5 });
  await page.mouse.move(to[0], to[1], { steps: 5 }); await sleep(60); await page.mouse.up();
}
async function submit(page, move) {
  const s = await state(page);
  const from = [s.handlePx[move.vertex * 2], s.handlePx[move.vertex * 2 + 1]];
  const to = screenGrid(s, move.x, move.y);
  await drag(page, from, to);
  return waitFor(page, x => x.frozen || x.phase === 'clear' || x.phase === 'gameover', 2500);
}

const errors = [], events = [], firstMoves = [], secondMoves = [];
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', e => errors.push(String(e)));
  await page.goto(`${server.url}/g/jeompeu-suri/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 30000 });
  await page.screenshot({ path: path.join(FRAMES, '00-title.png') });

  await page.mouse.move(195, 760); await page.mouse.down(); await sleep(90); await page.mouse.up(); await sleep(120);
  let s = await waitFor(page, x => x.onboarding && x.frozen);
  await page.screenshot({ path: path.join(FRAMES, '01-practice-guide.png') });
  const practiceRepair = repair(s);
  await submit(page, practiceRepair);
  s = await waitFor(page, x => !x.onboarding && !x.frozen && x.currentBand === 1, 6000);
  events.push({ name: 'practice-real-drag', move: practiceRepair, state: s });
  await page.screenshot({ path: path.join(FRAMES, '02-first-course.png') });

  // 실제 오답 드롭: 같은 허용 꼭짓점을 정답과 다른 점에 놓고 끈이 하나 줄어드는지 확인.
  const right = repair(s);
  let wrong = { vertex: right.vertex, x: (right.x + 1) % 7, y: right.y };
  if (wrong.x === s.vertices[right.vertex * 2] && wrong.y === s.vertices[right.vertex * 2 + 1]) wrong.y = (wrong.y + 1) % 7;
  const beforeWrong = s;
  s = await submit(page, wrong);
  if (s.lives !== 2 || s.firstAttempts !== 1 || s.firstCorrect !== 0) throw new Error(`wrong path failed: ${JSON.stringify(s)}`);
  await page.screenshot({ path: path.join(FRAMES, '03-wrong-evidence.png') });
  s = await waitFor(page, x => !x.frozen && x.currentProblem === beforeWrong.currentProblem, 5000);
  if (JSON.stringify(s.vertices) !== JSON.stringify(beforeWrong.vertices) || s.changedVertices !== 0) {
    throw new Error(`wrong reveal did not restore immutable start: ${JSON.stringify({ before: beforeWrong.vertices, after: s.vertices, changed: s.changedVertices })}`);
  }
  events.push({ name: 'wrong-restores-original-board', changedVertices: s.changedVertices, vertices: s.vertices });
  const recovery = repair(s, false);
  firstMoves.push(recovery);
  await submit(page, recovery);
  s = await waitFor(page, x => !x.frozen && x.solved === 1, 5000);
  events.push({ name: 'wrong-then-repair-real-drag', wrong, recovery, state: s });
  await page.screenshot({ path: path.join(FRAMES, '04-recovered.png') });

  // 남은 9개도 모두 화면 좌표로 드래그한다. 강제 정답 훅은 사용하지 않는다.
  while (s.phase !== 'clear' && s.phase !== 'gameover') {
    const beforeProblem = s.currentProblem;
    const move = repair(s);
    firstMoves.push(move);
    await submit(page, move);
    s = await waitFor(page, x => x.phase === 'clear' || x.phase === 'gameover' || (!x.frozen && x.currentProblem !== beforeProblem), 5000);
  }
  if (s.phase !== 'clear' || s.solved !== 10 || s.lives !== 2 || s.firstCorrect !== 9 || s.band3FirstCorrect !== 4) {
    throw new Error(`clear gate failed: ${JSON.stringify(s)}`);
  }
  events.push({ name: 'ten-board-clear-real-drag', state: s });
  await page.screenshot({ path: path.join(FRAMES, '05-clear.png') });

  // 결과 화면에서 실제 pointer로 다시 시작해, 첫 판과 독립된 새 덱을 두 번째로 완주한다.
  await page.mouse.move(195, 760); await page.mouse.down(); await sleep(90); await page.mouse.up();
  s = await waitFor(page, x => x.onboarding && x.frozen, 5000);
  await page.screenshot({ path: path.join(FRAMES, '06-second-practice.png') });
  const practiceRepair2 = repair(s);
  await submit(page, practiceRepair2);
  s = await waitFor(page, x => !x.onboarding && !x.frozen && x.currentBand === 1, 6000);
  const secondStart = { vertices: s.vertices.slice(), move: repair(s) };
  await page.screenshot({ path: path.join(FRAMES, '07-second-course.png') });
  while (s.phase !== 'clear' && s.phase !== 'gameover') {
    const beforeProblem = s.currentProblem;
    const move = repair(s);
    secondMoves.push(move);
    await submit(page, move);
    s = await waitFor(page, x => x.phase === 'clear' || x.phase === 'gameover' || (!x.frozen && x.currentProblem !== beforeProblem), 5000);
  }
  if (s.phase !== 'clear' || s.solved !== 10 || s.lives !== 3 || s.firstCorrect !== 10 || s.band3FirstCorrect !== 4) {
    throw new Error(`second clear gate failed: ${JSON.stringify(s)}`);
  }
  const differentActions = JSON.stringify(firstMoves) !== JSON.stringify(secondMoves);
  if (!differentActions) throw new Error('second run repeated the first move sequence');
  events.push({ name: 'independent-second-run', restart: 'result-screen-real-pointer', secondStart, moves: secondMoves, differentActions, state: s });
  await page.screenshot({ path: path.join(FRAMES, '08-second-clear.png') });

  // 이전 결함의 동일 반례: 밴드2에서 키보드로 서로 다른 두 꼭짓점을 누적 이동한 뒤 제출한다.
  await page.reload({ waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 30000 });
  await page.evaluate(() => window.__GAME_TEST__.start());
  for (let i = 0; i < 3; i++) {
    await page.evaluate(() => window.__GAME_TEST__.answerCorrect());
    await waitFor(page, x => x.frozen, 2500); await sleep(1750);
    await waitFor(page, x => !x.frozen || x.phase === 'clear', 2500);
  }
  s = await waitFor(page, x => !x.frozen && x.currentBand === 2, 3500);
  const keyboardStart = s.vertices.slice();
  await page.mouse.click(8, 500); await sleep(80);
  const answerVertex = repair(s).vertex;
  await page.keyboard.press(s.vertices[answerVertex * 2] < 6 ? 'ArrowRight' : 'ArrowLeft');
  s = await waitFor(page, x => x.changedVertices === 1, 1500);
  await page.keyboard.press('Tab');
  const secondVertex = (answerVertex + 1) & 3;
  await page.keyboard.press(s.vertices[secondVertex * 2] < 6 ? 'ArrowRight' : 'ArrowLeft');
  s = await waitFor(page, x => x.changedVertices === 2, 1500);
  await page.keyboard.press('Space');
  const rejectedTwoVertex = await waitFor(page, x => x.frozen && x.lastAction === 'repair-wrong', 2500);
  if (rejectedTwoVertex.lives !== 2 || rejectedTwoVertex.changedVertices !== 2) throw new Error(`two-vertex keyboard path was not rejected: ${JSON.stringify(rejectedTwoVertex)}`);
  const restoredKeyboard = await waitFor(page, x => !x.frozen && x.changedVertices === 0, 5000);
  if (JSON.stringify(restoredKeyboard.vertices) !== JSON.stringify(keyboardStart)) throw new Error(`keyboard rejection did not restore start: ${JSON.stringify(restoredKeyboard)}`);
  events.push({ name: 'two-vertex-keyboard-regression', before: keyboardStart, rejected: rejectedTwoVertex, restored: restoredKeyboard, passed: true });

  const result = {
    generated_at: new Date().toISOString(), viewport: '390x844',
    method: 'Chrome trusted mouse down/move/up; 정답은 공개 훅이 아니라 화면 상태의 정수 좌표를 독립 판정해 실제 격자 픽셀로 드래그',
    console_errors: errors, events, passed: errors.length === 0
  };
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  fs.writeFileSync(path.join(HERE, 'second-play-results.json'), `${JSON.stringify({
    generated_at: result.generated_at,
    method: result.method,
    firstRun: events.find(x => x.name === 'ten-board-clear-real-drag'),
    secondRun: events.find(x => x.name === 'independent-second-run'),
    console_errors: errors,
    passed: result.passed && differentActions
  }, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close(); await server.close();
}
