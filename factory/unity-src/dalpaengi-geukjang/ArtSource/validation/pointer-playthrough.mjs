import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const OUT = path.join(ROOT, 'factory/unity-src/dalpaengi-geukjang/ArtSource/validation');
const GAME = path.join(PUBLIC, 'g/dalpaengi-geukjang');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
const VIEW = { width: 390, height: 844 };

function artifactHash() {
  const hash = crypto.createHash('sha256');
  const rels = ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(name => `Build/${name}`)];
  for (const rel of rels) { hash.update(rel); hash.update(fs.readFileSync(path.join(GAME, rel))); }
  return hash.digest('hex');
}

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome'
  ]) { const p = path.join(base, build, rel); if (fs.existsSync(p)) return p; }
}

function solve(prompt) {
  const nums = [...prompt.matchAll(/(\d+)가지/g)].map(m => Number(m[1]));
  if (prompt.includes('모두')) return nums[0] - nums[1];
  if (prompt.includes('각각 하나씩')) return nums[0] * nums[1];
  return nums[0] + nums[1];
}

async function submit(page, target) {
  const x = 195, startTop = 844 - 844 * .17, targetTop = startTop - (target - 2) * 24;
  await page.mouse.move(x, startTop); await page.mouse.down(); await sleep(30);
  await page.mouse.move(x, targetTop, { steps: 5 }); await sleep(180); await page.mouse.up();
}

async function waitReadyForInput(page) {
  await page.waitForFunction(() => {
    const s = window.__GAME_TEST__.getState();
    return s.phase === 'playing' && s.submittedValue === 0 && !!s.prompt;
  }, { timeout: 5000 });
}

async function run(page, recovery, label) {
  // 타이틀 또는 결과 화면의 입장종을 실제 pointer로 누른다.
  await page.mouse.click(195, 844 - 844 * .21);
  await waitReadyForInput(page);
  let wrongDone = false, safety = 0, problemOrder = [];
  while (safety++ < 16) {
    const before = await page.evaluate(() => window.__GAME_TEST__.getState());
    if (before.phase === 'clear' || before.phase === 'gameover') return { ...before, problemOrder };
    if (!before.onboarding && problemOrder.at(-1) !== before.currentProblemId) problemOrder.push(before.currentProblemId);
    const answer = solve(before.prompt);
    if (!Number.isInteger(answer) || answer < 2 || answer > 17) throw new Error(`prompt parse failed: ${before.prompt} -> ${answer}`);
    const shouldWrong = recovery && !before.onboarding && !wrongDone;
    await submit(page, shouldWrong ? (answer === 2 ? 3 : 2) : answer);
    await page.waitForFunction(() => window.__GAME_TEST__.getState().submittedValue > 0, { timeout: 3000 });
    if (shouldWrong) wrongDone = true;
    if (label === 'normal' && !before.onboarding && problemOrder.length === 1 && !shouldWrong) {
      await sleep(620);
      await page.screenshot({ path: path.join(OUT, 'fix3-correct-snail-audience.png') });
      await sleep(1280);
    } else await sleep(1900);
    const after = await page.evaluate(() => window.__GAME_TEST__.getState());
    if (after.phase === 'clear' || after.phase === 'gameover') return { ...after, problemOrder };
    await waitReadyForInput(page);
  }
  throw new Error('playthrough safety exceeded');
}

const artifactHashBefore = artifactHash();
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: resolveChrome(), args: ['--no-sandbox', '--mute-audio'] });
const errors = [];
let normal, second, recovery;
try {
  const page = await browser.newPage();
  await page.setViewport({ ...VIEW, deviceScaleFactor: 1 });
  page.on('console', msg => { if (msg.type() === 'error') errors.push(msg.text()); });
  page.on('pageerror', err => errors.push(String(err)));
  await page.goto(`${server.url}/g/dalpaengi-geukjang/?play=${Date.now()}`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  normal = await run(page, false, 'normal');
  await page.screenshot({ path: path.join(OUT, 'normal-clear.png') });
  second = await run(page, false, 'second');
  await page.screenshot({ path: path.join(OUT, 'second-clear.png') });
  await page.reload({ waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  recovery = await run(page, true, 'recovery');
  await page.screenshot({ path: path.join(OUT, 'recovery-clear.png') });
  await page.close();
} finally { await browser.close(); await server.close(); }

const artifactHashAfter = artifactHash();
const runId = `dalpaengi-geukjang-pointer-playthrough-${new Date().toISOString()}`;
const result = {
  runId,
  run_id: runId,
  artifactHashSha256: artifactHashBefore,
  artifact_hash: artifactHashBefore,
  artifact_hash_before: artifactHashBefore,
  artifact_hash_after: artifactHashAfter,
  method: '타이틀 꽃종부터 모든 연습·본문 좌석 제출까지 실제 mouse down/move/up. 정상판 결과 종을 실제 클릭해 별도 두 번째 덱을 완주한 뒤, 페이지를 새로 로드해 오답 회복판을 수행했다. 화면 prompt의 공개된 수를 독립 정수 계산했으며 answerCorrect/answerWrong을 호출하지 않음.',
  normal,
  second,
  recovery,
  correctRevealScreenshot: 'fix3-correct-snail-audience.png',
  consoleErrors: errors,
  passed: normal?.phase === 'clear' && normal.solved === 8 && normal.firstCorrect === 8 && normal.lives === 3 &&
    second?.phase === 'clear' && second.solved === 8 && second.firstCorrect === 8 && second.lives === 3 &&
    JSON.stringify(normal.problemOrder) !== JSON.stringify(second.problemOrder) &&
    recovery?.phase === 'clear' && recovery.solved === 8 && recovery.firstCorrect === 7 && recovery.lives === 2 &&
    errors.length === 0 && artifactHashBefore === artifactHashAfter
};
console.log(JSON.stringify(result, null, 2));
fs.writeFileSync(new URL('./pointer-playthrough-results.json', import.meta.url), JSON.stringify(result, null, 2) + '\n');
if (!result.passed) process.exitCode = 1;
