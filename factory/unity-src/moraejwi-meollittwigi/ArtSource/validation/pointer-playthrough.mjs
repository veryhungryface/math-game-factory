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

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, timeout = 7000) {
  const end = Date.now() + timeout; let s;
  while (Date.now() < end) {
    s = await state(page);
    if (predicate(s)) return s;
    await sleep(70);
  }
  throw new Error(`state timeout: ${JSON.stringify(s)}`);
}

function gridPoint(s, a, b) {
  const [x0, y0, x1, y1, x2, y2] = s.gridPx;
  return [x0 + (x1 - x0) * a + (x2 - x0) * b, y0 + (y1 - y0) * a + (y2 - y0) * b];
}

function solutions(s) {
  const found = [];
  for (let a = 1; a <= s.gridN; a++) for (let b = 1; b <= s.gridN; b++) {
    if (s.lockedAxis && a !== s.currentA) continue;
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
  await page.mouse.down();
  await page.mouse.move((from[0] + to[0]) / 2, (from[1] + to[1]) / 2, { steps: 5 });
  await page.mouse.move(to[0], to[1], { steps: 6 });
  await sleep(70);
  await page.mouse.up();
}

async function submit(page, point) {
  const s = await state(page);
  await drag(page, s.handlePx, gridPoint(s, point.a, point.b));
  return waitFor(page, x => x.frozen || x.phase === 'clear' || x.phase === 'gameover', 3000);
}

const errors = [], events = [];
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', e => errors.push(String(e)));
  await page.goto(`${server.url}/g/moraejwi-meollittwigi/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 30000 });
  await page.screenshot({ path: path.join(FRAMES, '00-title.png') });

  // 세계 안의 단일 CTA를 실제 pointer로 누른다.
  await page.mouse.click(195, 766);
  let s = await waitFor(page, x => x.onboarding && x.frozen);
  await sleep(350);
  await page.screenshot({ path: path.join(FRAMES, '01-practice-guide.png') });
  if (s.currentS !== 25 || s.gridN !== 6) throw new Error(`practice mismatch: ${JSON.stringify(s)}`);

  // 고정 연습은 공개 훅 없이 실제 3,4 격자점으로 드래그한다.
  await submit(page, { a: 3, b: 4 });
  s = await waitFor(page, x => !x.onboarding && !x.frozen && x.round === 1, 7000);
  events.push({ name: 'practice-real-drag', state: s });
  await page.screenshot({ path: path.join(FRAMES, '02-first-round.png') });

  // 첫 일반 실수는 안전끈 하나와 첫 제출 기록을 잃되 같은 문제를 유지한다.
  const firstId = s.currentId;
  const wrong = wrongPoint(s);
  await submit(page, wrong);
  s = await waitFor(page, x => x.frozen && x.lives === 2);
  if (s.currentId !== firstId || s.firstAttempts !== 1 || s.firstCorrect !== 0) throw new Error(`wrong consequence mismatch: ${JSON.stringify(s)}`);
  await page.screenshot({ path: path.join(FRAMES, '03-wrong-evidence.png') });
  s = await waitFor(page, x => !x.frozen && x.currentId === firstId, 6000);
  const recovery = solutions(s)[0];
  await submit(page, recovery);
  s = await waitFor(page, x => x.solved === 1 && x.frozen);
  events.push({ name: 'wrong-then-repair', wrong, recovery, state: s });
  await page.screenshot({ path: path.join(FRAMES, '04-recovery-leap.png') });
  s = await waitFor(page, x => !x.frozen && x.round === 2, 6000);

  // 남은 일곱 도약을 모두 실제 화면 드래그로 완주한다.
  while (s.phase !== 'clear' && s.phase !== 'gameover') {
    const id = s.currentId;
    const right = solutions(s)[0];
    if (!right) throw new Error(`no solution: ${JSON.stringify(s)}`);
    await submit(page, right);
    s = await waitFor(page, x => x.phase === 'clear' || x.phase === 'gameover' || (!x.frozen && x.currentId !== id), 6500);
  }
  if (s.phase !== 'clear' || s.solved !== 8 || s.firstCorrect !== 7 || s.firstAttempts !== 8 || s.lives !== 2) {
    throw new Error(`clear gate mismatch: ${JSON.stringify(s)}`);
  }
  events.push({ name: 'clear-after-one-real-mistake', state: s });
  await page.screenshot({ path: path.join(FRAMES, '05-clear.png') });

  // 안전끈 0 뒤 입력 잠금: 테스트 명령은 실제 Submit 경로를 사용하고 종료 상태를 바꿀 수 없어야 한다.
  await page.evaluate(() => window.__GAME_TEST__.start());
  for (let i = 0; i < 3; i++) {
    await page.evaluate(() => window.__GAME_TEST__.answerWrong());
    await sleep(1200);
  }
  s = await waitFor(page, x => x.phase === 'gameover' && x.lives === 0, 5000);
  const locked = JSON.stringify(s);
  await page.mouse.click(195, 420); await sleep(250);
  const afterLockedTap = await state(page);
  if (afterLockedTap.score !== s.score || afterLockedTap.solved !== s.solved || afterLockedTap.lives !== 0) throw new Error('gameover input changed progress');
  events.push({ name: 'zero-rope-input-lock', before: JSON.parse(locked), after: afterLockedTap });

  const result = {
    generated_at: new Date().toISOString(),
    viewport: '390x844',
    method: 'Chrome trusted mouse down/move/up; 연습·오답·수리·8회 완주를 화면에 노출된 격자 변환으로 실행',
    console_errors: errors,
    events,
    passed: errors.length === 0
  };
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
