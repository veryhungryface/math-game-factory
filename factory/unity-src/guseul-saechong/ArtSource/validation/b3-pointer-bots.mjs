import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/guseul-saechong');
const GAMES = Number(process.env.B3_GAMES || 200);
const ONLY = process.env.B3_ONLY || '';
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
  throw new Error('Chrome for Testing not found');
}
function artifactHash() {
  const h = crypto.createHash('sha256');
  for (const file of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(x => `Build/${x}`)]) {
    h.update(file);
    h.update(fs.readFileSync(path.join(GAME, file)));
  }
  return h.digest('hex');
}
function seeded(seed) {
  let x = seed >>> 0;
  return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; };
}
async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, timeout = 3000) {
  const until = Date.now() + timeout;
  let s;
  while (Date.now() < until) {
    s = await state(page);
    if (predicate(s)) return s;
    await sleep(8);
  }
  throw new Error(`timeout: ${JSON.stringify(s)}`);
}
function marblePoint(s, i) { return [s.marblePx[i * 2], s.marblePx[i * 2 + 1]]; }
async function setCount(page, wanted) {
  let s = await state(page);
  for (let guard = 0; guard < 32; guard++) {
    const current = s.colors.slice(0, s.N).reduce((a, b) => a + b, 0);
    if (current === wanted) return s;
    const toPurple = current < wanted;
    const i = s.colors.slice(0, s.N).findIndex(v => v !== (toPurple ? 1 : 0));
    const p = marblePoint(s, i);
    await page.mouse.click(p[0], p[1], { delay: 3 });
    await sleep(6);
    s = await state(page);
  }
  if (s.colors.slice(0, s.N).reduce((a, b) => a + b, 0) === wanted) return s;
  throw new Error(`setCount failed wanted=${wanted} state=${JSON.stringify(s)}`);
}
async function submit(page, count) {
  const s = await setCount(page, count);
  const attemptsBefore = s.attemptsByProblem.reduce((a, b) => a + b, 0);
  await page.mouse.move(s.pouchPx[0], s.pouchPx[1]);
  await page.mouse.down();
  await page.mouse.move((s.pouchPx[0] + s.pouchPx[2]) / 2, (s.pouchPx[1] + s.pouchPx[3]) / 2, { steps: 4 });
  await page.mouse.move(s.pouchPx[2], s.pouchPx[3], { steps: 5 });
  await sleep(25);
  await page.mouse.up();
  await sleep(35);
  // Fast test reveals last 35 ms, so a correct shot can already be on the next
  // problem before JavaScript samples the state. Accept the durable submission
  // counters/state changes as evidence instead of requiring the transient reveal.
  return waitFor(page, x => x.phase === 'reveal' || x.phase === 'clear' || x.phase === 'gameover'
    || x.lastAction === 'same-mixture-refused' || x.currentProblem !== s.currentProblem
    || x.firstAttempts > s.firstAttempts
    || x.attemptsByProblem.reduce((a, b) => a + b, 0) > attemptsBefore
    || x.lives !== s.lives || x.score !== s.score);
}

async function runPolicy(page, name, seed) {
  const random = seeded(seed);
  let firstAttempts = 0, firstCorrect = 0, chanceSum = 0, chanceVariance = 0;
  let completed = 0, mastered = 0, delivered = 0, action = 0;
  for (let game = 0; game < GAMES; game++) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    let s = await waitFor(page, x => x.phase === 'playing' && x.currentProblem);
    let problem = s.currentProblem;
    let seen = new Set();
    for (let guard = 0; guard < 28 && s.phase === 'playing'; guard++) {
      const first = !seen.has(s.currentProblem);
      if (first) {
        seen.add(s.currentProblem);
        firstAttempts++;
        const p = 1 / (s.N + 1);
        chanceSum += p;
        chanceVariance += p * (1 - p);
      }
      const guess = name === 'spam' ? Math.min(3, s.N)
        : name === 'cycle' ? action % (s.N + 1)
        : Math.floor(random() * (s.N + 1));
      action++;
      const beforeFirst = s.firstAttemptCorrect;
      const beforeSolved = s.solved;
      s = await submit(page, guess);
      if (s.lastAction === 'same-mixture-refused') break;
      if (first && s.firstAttemptCorrect > beforeFirst) firstCorrect++;
      s = await waitFor(page, x => x.phase === 'playing' || x.phase === 'clear' || x.phase === 'gameover', 3000);
      if (s.solved > beforeSolved) delivered += s.solved - beforeSolved;
      if (s.phase === 'clear') { completed++; mastered++; break; }
      if (s.phase === 'gameover') break;
      problem = s.currentProblem;
    }
  }
  const rate = firstCorrect / Math.max(1, firstAttempts);
  const chance = chanceSum / Math.max(1, firstAttempts);
  const zScore = chanceVariance > 0 ? (firstCorrect - chanceSum) / Math.sqrt(chanceVariance) : 0;
  return {
    games: GAMES, firstAttempts, firstCorrect, firstAttemptRate: rate, chance,
    expectedCorrect: chanceSum, zScore, significanceThreshold: 1.96,
    completed, completionRate: completed / GAMES, mastered, masteryRate: mastered / GAMES,
    delivered, passed: zScore <= 1.96
  };
}

async function runIdle(page) {
  let progress = 0, firstAttempts = 0;
  for (let game = 0; game < GAMES; game++) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    const s = await waitFor(page, x => x.phase === 'playing');
    await sleep(12);
    const after = await state(page);
    progress += after.solved;
    firstAttempts += after.firstAttempts;
  }
  return { games: GAMES, firstAttempts, firstCorrect: 0, firstAttemptRate: 0, chance: 0, completed: 0, completionRate: 0, progress, passed: progress === 0 && firstAttempts === 0 };
}

const errors = [];
const startHash = artifactHash();
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', e => errors.push(String(e)));
  await page.goto(`${server.url}/g/guseul-saechong/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 30000 });
  const resultPath = path.join(HERE, 'b3-pointer-results.json');
  const previous = ONLY && fs.existsSync(resultPath) ? JSON.parse(fs.readFileSync(resultPath, 'utf8')) : null;
  const spam = ONLY && ONLY !== 'spam' ? previous.spam : await runPolicy(page, 'spam', 0x13131313);
  const cycle = ONLY && ONLY !== 'cycle' ? previous.cycle : await runPolicy(page, 'cycle', 0x27272727);
  const random = ONLY && ONLY !== 'random' ? previous.random : await runPolicy(page, 'random', 0x5eed2026);
  const idle = ONLY && ONLY !== 'idle' ? previous.idle : await runIdle(page);
  const endHash = artifactHash();
  const result = {
    run_id: `guseul-saechong-b3-${new Date().toISOString()}`,
    artifact_hash: startHash,
    method: '동일 WebGL 빌드에서 TestStart로 판만 초기화하고 모든 배합·발사는 실제 Puppeteer mouse pointer 이벤트로 실행. 문항별 첫 제출만 집계.',
    criterion: '실제 만난 문항별 p=1/(N+1)의 포아송-이항 기대·분산에서 우연 수준 초과가 95% 유의하지 않을 것(z≤1.96), 무입력 진도 0',
    retest: previous ? { policy: ONLY, previous_run_id: previous.run_id, previous_policy_result: previous[ONLY] } : null,
    spam, cycle, random, idle, consoleErrors: errors,
    passed: spam.passed && cycle.passed && random.passed && idle.passed && errors.length === 0 && startHash === endHash
  };
  fs.writeFileSync(resultPath, `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
