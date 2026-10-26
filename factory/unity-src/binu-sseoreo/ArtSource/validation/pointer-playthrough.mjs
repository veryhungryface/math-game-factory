import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const HERE = path.dirname(new URL(import.meta.url).pathname);
const OUT = path.join(HERE, 'pointer-playthrough');
fs.mkdirSync(OUT, { recursive: true });

const W = 390;
const H = 844;
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

function chromePath() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const version of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome',
    ]) {
      const candidate = path.join(base, version, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
  return undefined;
}

function artifactHash() {
  const game = path.join(PUBLIC, 'g/binu-sseoreo');
  const rels = ['index.html', ...fs.readdirSync(path.join(game, 'Build')).sort().map((name) => `Build/${name}`)];
  const hash = crypto.createHash('sha256');
  for (const rel of rels) {
    hash.update(rel);
    hash.update(fs.readFileSync(path.join(game, rel)));
  }
  return hash.digest('hex');
}

const state = (page) => page.evaluate(() => window.__GAME_TEST__.getState());
const answerFromId = (id) => {
  const match = /-h(\d+)-(?:lo|up)$/.exec(id);
  if (!match) throw new Error(`cannot recover public problem height from ${id}`);
  return Number(match[1]);
};

function browserY(height) {
  const unityY = H * (0.20 + 0.58 * height / 25);
  return H - unityY;
}

async function slice(page, height) {
  const y = browserY(height);
  await page.mouse.move(W * 0.18, y);
  await page.mouse.down();
  await sleep(35);
  await page.mouse.move(W * 0.40, y, { steps: 5 });
  await page.mouse.move(W * 0.82, y, { steps: 8 });
  await sleep(35);
  await page.mouse.up();
}

async function enterRun(page) {
  await page.mouse.click(W / 2, H * 0.70);
  await page.waitForFunction(() => window.__GAME_TEST__.getState().onboarding === true, { timeout: 4000 });
  await slice(page, 4);
  await page.waitForFunction(() => {
    const s = window.__GAME_TEST__.getState();
    return s.onboarding === false && s.round === 1 && s.gesturePhase === 'idle';
  }, { timeout: 5000 });
}

async function solve(page, { wrong = false } = {}) {
  const before = await state(page);
  const answer = answerFromId(before.activeProblemId);
  const selected = wrong ? (answer === 24 ? 23 : answer + 1) : answer;
  await slice(page, selected);
  await page.waitForFunction((attempts) => window.__GAME_TEST__.getState().attemptCount > attempts, { timeout: 2500 }, before.attemptCount);
  await sleep(1550);
  return state(page);
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: chromePath(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'],
});
const page = await browser.newPage();
const consoleErrors = [];
const pageErrors = [];
page.on('console', (message) => { if (message.type() === 'error') consoleErrors.push(message.text()); });
page.on('pageerror', (error) => pageErrors.push(String(error)));
await page.setViewport({ width: W, height: H, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
await page.goto(`${server.url}/g/binu-sseoreo/`, { waitUntil: 'load', timeout: 45000 });
await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 60000 });
await page.screenshot({ path: path.join(OUT, '00-title.png') });

// 실제 mouse down/move/up만 사용한다. 테스트 정답/오답 훅은 호출하지 않는다.
await enterRun(page);
await page.screenshot({ path: path.join(OUT, '01-first-order.png') });

await solve(page, { wrong: true });
const afterWrong = await state(page);
await page.screenshot({ path: path.join(OUT, '02-repair-same-order.png') });
if (afterWrong.lives !== 2 || afterWrong.solved !== 0 || !afterWrong.repairMode || afterWrong.firstAttemptTotal !== 1) {
  throw new Error(`first mistake was not recoverable: ${JSON.stringify(afterWrong)}`);
}
for (let i = 0; i < 8; i++) {
  const current = await state(page);
  if (current.phase === 'clear') break;
  await solve(page);
}
const recovered = await state(page);
await page.screenshot({ path: path.join(OUT, '03-recovered-clear.png') });
if (recovered.phase !== 'clear' || recovered.solved !== 8 || recovered.lives !== 2 || recovered.firstAttemptCorrect !== 7) {
  throw new Error(`recovery run failed: ${JSON.stringify(recovered)}`);
}

await enterRun(page);
for (let i = 0; i < 8; i++) await solve(page);
const clean = await state(page);
await page.screenshot({ path: path.join(OUT, '04-clean-clear.png') });
if (clean.phase !== 'clear' || clean.solved !== 8 || clean.lives !== 3 || clean.firstAttemptCorrect !== 8) {
  throw new Error(`clean run failed: ${JSON.stringify(clean)}`);
}

await enterRun(page);
for (let i = 0; i < 3; i++) {
  await solve(page, { wrong: true });
  if ((await state(page)).phase === 'gameover') break;
}
const gameover = await state(page);
await page.screenshot({ path: path.join(OUT, '05-zero-soap-gameover.png') });
if (gameover.phase !== 'gameover' || gameover.lives !== 0) {
  throw new Error(`zero-soap gameover failed: ${JSON.stringify(gameover)}`);
}

const result = {
  run_id: `binu-sseoreo-pointer-${new Date().toISOString()}`,
  artifact_hash: artifactHash(),
  method: 'All title, practice, cut, repair, restart, and gameover actions use Chrome real mouse down/move/up. Heights are reconstructed from the integer h encoded by the same public problem generator; answerCorrect/answerWrong/start hooks are never called.',
  practicePassed: true,
  recovered,
  clean,
  gameover,
  consoleErrors,
  pageErrors,
  passed: consoleErrors.length === 0 && pageErrors.length === 0,
};
fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(result, null, 2)}\n`);
console.log(JSON.stringify(result, null, 2));

await page.close();
await browser.close();
await server.close();
if (!result.passed) process.exitCode = 1;
