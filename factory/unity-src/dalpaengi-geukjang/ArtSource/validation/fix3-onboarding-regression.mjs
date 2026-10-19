import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/dalpaengi-geukjang');
const OUT = path.join(ROOT, 'factory/unity-src/dalpaengi-geukjang/ArtSource/validation');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

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

async function tap(page, x, y) {
  await page.mouse.move(x, y); await page.mouse.down(); await sleep(60); await page.mouse.up();
}

async function dragToFive(page) {
  // 첫 무효 입력 뒤 실제 손잡이 밖에서도 잡히는 넓은 영역을 사용한다.
  // Unity 좌표는 아래가 +y이므로 브라우저에서는 짧게 위로 이동한다.
  const x = 92, startTop = 510;
  const trace = [];
  await page.mouse.move(x, startTop);
  await page.mouse.down();
  await sleep(40);
  trace.push((await page.evaluate(() => window.__GAME_TEST__.getState())).currentSeatCount);
  for (const distance of [5, 10, 19]) {
    await page.mouse.move(x, startTop - distance, { steps: 4 });
    await sleep(55);
    trace.push((await page.evaluate(() => window.__GAME_TEST__.getState())).currentSeatCount);
  }
  await page.mouse.up();
  return trace;
}

const hashBefore = artifactHash();
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--mute-audio']
});
const errors = [];
let result;
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
  page.on('console', msg => { if (msg.type() === 'error') errors.push(msg.text()); });
  page.on('pageerror', err => errors.push(String(err)));
  const startedAt = Date.now();
  await page.goto(`${server.url}/g/dalpaengi-geukjang/?fix3=${startedAt}`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  await tap(page, 195, 422);
  await page.waitForFunction(() => window.__GAME_TEST__.getState().onboarding === true, { timeout: 5000 });

  // 검수와 같은 종류의 무효 탭을 한 번 보낸다. 즉시 넓은 잡기 영역과 고정 시범이 나타나야 한다.
  await tap(page, 52, 205);
  await sleep(500);
  const assisted = await page.evaluate(() => window.__GAME_TEST__.getState());
  await page.screenshot({ path: path.join(OUT, 'fix3-onboarding-scaffold.png') });

  // 정답 훅 없이 화면에 표시된 경로보다 짧은 19px drag로 2→3→4→5를 실제 밟고 release한다.
  const seat_trace = await dragToFive(page);
  await page.waitForFunction(() => {
    const s = window.__GAME_TEST__.getState();
    return s.onboarding === false && s.phase === 'playing' && /^dg-run-/.test(s.currentProblemId);
  }, { timeout: 5000 });
  const firstMain = await page.evaluate(() => window.__GAME_TEST__.getState());
  const elapsedMs = Date.now() - startedAt;
  await page.screenshot({ path: path.join(OUT, 'fix3-first-main-before-30s.png') });
  result = {
    run_id: `dalpaengi-geukjang-fix3-onboarding-${new Date().toISOString()}`,
    artifact_hash: hashBefore,
    artifact_hash_before: hashBefore,
    artifact_hash_after: artifactHash(),
    prior_failure_reproduction: '공식 firstplay에서 첫 본문이 t045에 나타나 39초 동안 합 연습 값 2에 머물렀다.',
    assisted_state: assisted,
    seat_trace,
    after_fixed_input: firstMain,
    elapsed_ms_to_first_main: elapsedMs,
    input: '실제 mouse 시작 탭 + 무효 탭 1회 + 손잡이 밖 넓은 영역에서 19px mouse down/move/up. answerCorrect/start 훅 미사용.',
    screenshots: ['fix3-onboarding-scaffold.png', 'fix3-first-main-before-30s.png'],
    console_errors: errors,
    passed: assisted.invalidHandleTaps === 1 && assisted.submittedValue === 0 && assisted.onboarding === true &&
      JSON.stringify(seat_trace) === JSON.stringify([2, 3, 4, 5]) &&
      firstMain.onboarding === false && firstMain.phase === 'playing' && elapsedMs < 30000 &&
      errors.length === 0 && hashBefore === artifactHash()
  };
  await page.close();
} finally {
  await browser.close();
  await server.close();
}

fs.writeFileSync(path.join(OUT, 'fix3-onboarding-results.json'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
if (!result.passed) process.exitCode = 1;
