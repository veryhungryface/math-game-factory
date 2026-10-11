import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';
import {acquireMachineLock} from '../../../../lib/machine-lock.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const HERE = path.dirname(new URL(import.meta.url).pathname);
const FRAMES = path.join(HERE, 'playthrough-frames');
fs.mkdirSync(FRAMES, {recursive: true});
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome'
    ]) {
      const candidate = path.join(base, build, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
  return undefined;
}

function artifactHash() {
  const dir = path.join(PUBLIC, 'g/yeon-ttwiwo');
  const files = [];
  (function walk(current) {
    for (const name of fs.readdirSync(current).sort()) {
      const item = path.join(current, name);
      fs.statSync(item).isDirectory() ? walk(item) : files.push(item);
    }
  })(dir);
  const hash = crypto.createHash('sha256');
  for (const file of files) {
    hash.update(path.relative(dir, file));
    hash.update(fs.readFileSync(file));
  }
  return hash.digest('hex');
}

function rotate(vx, vy, quarter) {
  quarter = ((quarter % 4) + 4) % 4;
  if (quarter === 1) return [-vy, vx];
  if (quarter === 2) return [-vx, -vy];
  if (quarter === 3) return [vy, -vx];
  return [vx, vy];
}

function accepted(state, dock, cart, rotation) {
  const goal = state.dockGeometry[dock * 3];
  const ux = state.dockGeometry[dock * 3 + 1];
  const uy = state.dockGeometry[dock * 3 + 2];
  const vx0 = state.cartridgeGeometry[cart * 4];
  const vy0 = state.cartridgeGeometry[cart * 4 + 1];
  const p = state.cartridgeGeometry[cart * 4 + 2];
  const q = state.cartridgeGeometry[cart * 4 + 3];
  const [vx, vy] = rotate(vx0, vy0, rotation);
  const bisects = p === q;
  const equal = 4 * (ux * ux + uy * uy) === (p + q) * (p + q) * (vx * vx + vy * vy);
  const perpendicular = ux * vx + uy * vy === 0;
  const nondegenerate = ux * vy - uy * vx !== 0;
  return nondegenerate && bisects && (goal === 0 || (goal === 1 && equal) ||
    (goal === 2 && perpendicular) || (goal === 3 && equal && perpendicular));
}

function findPlacement(state, wantCorrect) {
  for (let dock = 0; dock < 9; dock++) {
    if (state.cells[dock] !== 0) continue;
    for (let cart = 0; cart < 3; cart++) {
      // 삽입 전 실제 회전을 요구하므로 0 대신 동치인 1/2/3부터 찾는다.
      for (const rotation of [1, 2, 3]) {
        if (accepted(state, dock, cart, rotation) === wantCorrect) return {dock, cart, rotation};
      }
    }
  }
  throw new Error(`${wantCorrect ? '정답' : '오답'} 배치를 찾지 못함: ${JSON.stringify(state)}`);
}

function findDegenerateSquarePlacement(snapshot) {
  for (let dock = 0; dock < 9; dock++) {
    if (snapshot.cells[dock] !== 0 || snapshot.dockGeometry[dock * 3] !== 3) continue;
    const ux = snapshot.dockGeometry[dock * 3 + 1];
    const uy = snapshot.dockGeometry[dock * 3 + 2];
    for (let cart = 0; cart < 3; cart++) {
      const vx0 = snapshot.cartridgeGeometry[cart * 4];
      const vy0 = snapshot.cartridgeGeometry[cart * 4 + 1];
      const p = snapshot.cartridgeGeometry[cart * 4 + 2];
      const q = snapshot.cartridgeGeometry[cart * 4 + 3];
      if (p !== q) continue;
      for (const rotation of [1, 2, 3]) {
        const [vx, vy] = rotate(vx0, vy0, rotation);
        const cross = ux * vy - uy * vx;
        const dot = ux * vx + uy * vy;
        const equal = 4 * (ux * ux + uy * uy) === (p + q) * (p + q) * (vx * vx + vy * vy);
        if (cross === 0 && dot !== 0 && !equal) return {dock, cart, rotation};
      }
    }
  }
  return null;
}

async function state(page) {
  return page.evaluate(() => window.__GAME_TEST__.getState());
}

async function rotateAndInsert(page, snapshot, placement) {
  const cx = snapshot.cartridgePx[placement.cart * 2];
  const cy = snapshot.cartridgePx[placement.cart * 2 + 1];
  const dx = snapshot.dockPx[placement.dock * 2];
  const dy = snapshot.dockPx[placement.dock * 2 + 1];
  const direction = placement.rotation === 3 ? -1 : 1;
  const steps = placement.rotation === 3 ? 1 : placement.rotation;
  let angle = 0;

  await page.mouse.move(cx, cy);
  await page.mouse.down();
  await sleep(55);
  await page.mouse.move(cx + 26, cy);
  await sleep(55);
  for (let i = 1; i <= steps; i++) {
    angle = direction * 35 * i;
    const rad = angle * Math.PI / 180;
    await page.mouse.move(cx + Math.cos(rad) * 44, cy - Math.sin(rad) * 44);
    await sleep(65);
  }
  const rad = angle * Math.PI / 180;
  await page.mouse.move(cx + Math.cos(rad) * 108, cy - Math.sin(rad) * 108, {steps: 3});
  await sleep(70);
  await page.mouse.move((cx + dx) / 2, (cy + dy) / 2, {steps: 3});
  await sleep(55);
  await page.mouse.move(dx, dy, {steps: 4});
  await sleep(70);
  await page.mouse.up();
}

// 3차 수정 회귀 입력: 수정 전에는 한 grab의 긴 원호가 18°마다 반복 회전되어
// 180°(짝수 사분면)로 끝나 연습 오답이 났다. 같은 입력을 보존해 한 번의
// 90° 홈에 잠긴 뒤 실전으로 넘어가는지 검사한다.
async function longArcPracticeAndInsert(page, snapshot) {
  const cx = snapshot.cartridgePx[0];
  const cy = snapshot.cartridgePx[1];
  const dx = snapshot.dockPx[8];
  const dy = snapshot.dockPx[9];
  await page.mouse.move(cx, cy);
  await page.mouse.down();
  await sleep(55);
  await page.mouse.move(cx + 44, cy);
  for (const degrees of [30, 60, 90, 120, 150, 180]) {
    const rad = degrees * Math.PI / 180;
    await page.mouse.move(cx + Math.cos(rad) * 44, cy - Math.sin(rad) * 44);
    await sleep(55);
  }
  await page.mouse.move((cx + dx) / 2, (cy + dy) / 2, {steps: 3});
  await page.mouse.move(dx, dy, {steps: 4});
  await sleep(70);
  await page.mouse.up();
}

async function waitForNextBundle(page, placements) {
  await page.waitForFunction(previous => {
    const s = window.__GAME_TEST__.getState();
    return s.phase !== 'playing' ||
      (s.placements === previous && typeof s.lastAction === 'string' && s.lastAction.startsWith('bundle-'));
  }, {timeout: 5000}, placements);
}

async function solveRun(page, oneWrong) {
  let wrongDone = false;
  for (let guard = 0; guard < 28; guard++) {
    const before = await state(page);
    if (before.phase !== 'playing') return before;
    const makeWrong = oneWrong && !wrongDone;
    const placement = findPlacement(before, !makeWrong);
    await rotateAndInsert(page, before, placement);
    await page.waitForFunction(previous => window.__GAME_TEST__.getState().placements > previous,
      {timeout: 2500}, before.placements);
    const judged = await state(page);
    if (makeWrong) {
      if (judged.lives !== before.lives - 1 || judged.lastAction !== 'wrong-knot')
        throw new Error(`실제 오답 판정 실패: ${JSON.stringify({before, judged, placement})}`);
      wrongDone = true;
    } else if (judged.lastAction !== 'correct-place') {
      throw new Error(`실제 정답 판정 실패: ${JSON.stringify({before, judged, placement})}`);
    }
    await waitForNextBundle(page, judged.placements);
  }
  throw new Error('완주 guard 초과');
}

const release = await acquireMachineLock('gpu', {label: 'yeon-ttwiwo 실제 포인터 플레이스루'});
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio', '--enable-unsafe-swiftshader', '--use-gl=angle', '--use-angle=swiftshader']
});

let exitCode = 0;
try {
  const page = await browser.newPage();
  await page.setViewport({width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true});
  const errors = [];
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/yeon-ttwiwo/`, {waitUntil: 'load', timeout: 45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  await page.screenshot({path: path.join(FRAMES, '00-title.png')});

  const boot = await state(page);
  const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(360));
  const uniquePrompts = new Set(bank.map(item => item.prompt)).size;
  const promptStyleOk = bank.every(item => item.prompt.includes('두 대각선의 교점을 O라 할 때') &&
    item.prompt.includes('AO=CO=') && item.prompt.includes('BO=') && item.prompt.includes('DO=') &&
    !item.prompt.includes('AC 반쪽') && !/살대\s+[A-Z]는/.test(item.prompt) &&
    !/BO=\d+(?!\d)(?!\s+cm)/.test(item.prompt) && !/DO=\d+(?!\d)(?!\s+cm)/.test(item.prompt) &&
    !/살대\s+[A-Z]\s+\d+·\d+/.test(item.prompt));
  // WebGL 브리지는 공개 sampleProblems에서 확장 기하 필드를 의도적으로 숨긴다. 부팅 전
  // YeonRules.ValidateAll이 360개 legal/accepted/chance와 480개 오개념 조합을 전수 검사하며,
  // ready=true가 그 검사를 통과했다는 증거다. 여기서는 공개 계약의 개수·핵심 발문 다양성을 재확인한다.
  const mathBankOk = bank.length === 360 && uniquePrompts >= 300 && promptStyleOk && boot.wrongExplanationChecks >= 40000;

  // 중앙을 짧게 누르는 실제 pointer 입력으로 타이틀을 열고, 고정 연습도 원호→삽입으로 푼다.
  const tapStartedAt = Date.now();
  await page.mouse.move(195, 422); await page.mouse.down(); await sleep(90); await page.mouse.up();
  await page.waitForFunction(() => window.__GAME_TEST__.getState().onboarding === true, {timeout: 2500});
  const titleTapToPracticeMs = Date.now() - tapStartedAt;
  await sleep(150);
  await page.screenshot({path: path.join(FRAMES, '01-practice-guide.png')});
  const practice = await state(page);
  const practiceStartedAt = Date.now();
  await longArcPracticeAndInsert(page, practice);
  await page.waitForFunction(() => {
    const s = window.__GAME_TEST__.getState();
    return s.onboarding === false && s.lastAction?.startsWith('bundle-');
  }, {timeout: 5000});
  const longArcPracticeToRunMs = Date.now() - practiceStartedAt;
  await page.screenshot({path: path.join(FRAMES, '02-first-run.png')});

  const clean = await solveRun(page, false);
  await page.screenshot({path: path.join(FRAMES, '03-clean-clear.png')});

  // 결과 손잡이 1탭 재시작도 실제 입력으로 확인하고, 두 번째 판은 연습만 훅으로 건너뛴다.
  const end = await state(page);
  for (let retry = 0; retry < 3; retry++) {
    if ((await state(page)).onboarding) break;
    await page.mouse.move(end.chromePx[2], end.chromePx[3]); await page.mouse.down(); await sleep(140); await page.mouse.up();
    await sleep(220);
  }
  await page.waitForFunction(() => window.__GAME_TEST__.getState().onboarding === true, {timeout: 4000});
  await page.evaluate(() => window.__GAME_TEST__.start());
  await page.waitForFunction(() => {
    const s = window.__GAME_TEST__.getState();
    return s.phase === 'playing' && s.frozen === false && s.lastAction?.startsWith('bundle-');
  }, {timeout: 2500});
  const recovery = await solveRun(page, true);
  await page.screenshot({path: path.join(FRAMES, '04-one-wrong-clear.png')});

  // 같은 실제 입력 경로에서 외적 0인 정사각형 오답을 찾아 화면 설명까지 검증한다.
  await page.evaluate(() => window.__GAME_TEST__.start());
  await page.waitForFunction(() => window.__GAME_TEST__.getState().lastAction?.startsWith('bundle-'), {timeout: 2500});
  let degeneratePlacement = null;
  let degenerateBefore = null;
  for (let i = 0; i < 24 && !degeneratePlacement; i++) {
    degenerateBefore = await state(page);
    degeneratePlacement = findDegenerateSquarePlacement(degenerateBefore);
    if (degeneratePlacement) break;
    const previous = degenerateBefore.placements;
    await page.evaluate(() => window.__GAME_TEST__.answerCorrect());
    await page.waitForFunction(p => window.__GAME_TEST__.getState().placements > p, {timeout: 2500}, previous);
    await page.waitForFunction(() => {
      const s = window.__GAME_TEST__.getState();
      return s.phase !== 'playing' || (s.frozen === false && s.lastAction?.startsWith('bundle-'));
    }, {timeout: 5000});
    if ((await state(page)).phase !== 'playing') {
      await page.evaluate(() => window.__GAME_TEST__.start());
      await page.waitForFunction(() => {
        const s = window.__GAME_TEST__.getState();
        return s.phase === 'playing' && s.frozen === false && s.lastAction?.startsWith('bundle-');
      }, {timeout: 2500});
    }
  }
  if (!degeneratePlacement) throw new Error('외적 0 정사각형 오답 배치를 찾지 못함');
  await rotateAndInsert(page, degenerateBefore, degeneratePlacement);
  await page.waitForFunction(previous => window.__GAME_TEST__.getState().placements > previous,
    {timeout: 2500}, degenerateBefore.placements);
  const degenerateAfter = await state(page);
  await page.screenshot({path: path.join(FRAMES, '05-degenerate-wrong-evidence.png')});
  const degenerateExplanationOk = degenerateAfter.misconceptionId === 'degenerate_diagonals' &&
    degenerateAfter.lastExplanation === '두 대각선이 한 직선에 놓여 사각형이 만들어지지 않는다';

  async function captureViewport(width, height, name, start = true) {
    const p = await browser.newPage();
    await p.setViewport({width, height, deviceScaleFactor: 1});
    await p.goto(`${server.url}/g/yeon-ttwiwo/`, {waitUntil: 'load', timeout: 45000});
    await p.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
    await sleep(500);
    if (start) {
      await p.evaluate(() => window.__GAME_TEST__.start());
      await p.waitForFunction(() => window.__GAME_TEST__.getState().phase === 'playing', {timeout: 2500});
      await sleep(150);
    }
    const snapshot = await state(p);
    const panelRects = [];
    for (let i = 0; i < 16; i += 4) panelRects.push(snapshot.panelPx.slice(i, i + 4));
    const panelsInside = !start || panelRects.every(([x, y, w, h]) => x >= 0 && y >= 0 && x + w <= width && y + h <= height);
    await p.screenshot({path: path.join(FRAMES, name)});
    await p.close();
    return {width, height, panelRects, panelsInside};
  }

  const layouts = [];
  layouts.push(await captureViewport(1280, 800, '06-layout-1280.png'));
  layouts.push(await captureViewport(1440, 900, '07-layout-1440.png'));
  layouts.push(await captureViewport(2000, 1045, '08-layout-2000.png'));
  await captureViewport(1200, 630, path.relative(FRAMES, path.join(PUBLIC, 'g/yeon-ttwiwo/thumb.png')), false);
  await captureViewport(1080, 1080, path.relative(FRAMES, path.join(PUBLIC, 'g/yeon-ttwiwo/square.png')), false);

  const policies = ['repeat', 'cycle', 'random', 'idle'];
  const bots = policies.map((policy, i) => ({
    policy,
    games: 200,
    firstTryRate: boot.botFirstRates[i],
    chanceBaseline: boot.botChanceBaselines[i],
    completions: boot.botCompletions[i],
    completionRate: boot.botCompletions[i] / 200,
    passed: policy === 'idle' ? boot.botFirstRates[i] === 0 && boot.botCompletions[i] === 0
      : boot.botFirstRates[i] <= boot.botChanceBaselines[i] + 0.02
  }));
  const result = {
    runId: `yeon-ttwiwo-${new Date().toISOString()}`,
    artifactHashSha256: artifactHash(),
    method: 'Chrome 실제 mouse down/move/up으로 타이틀 1탭, 연습 원호 회전·삽입, 무실수 완주, 오답 1회 뒤 완주를 수행. 정답 계산은 StateJson의 화면 표시 정수 벡터를 독립 재계산. B3는 동일 C# 생성기 RunBotAudit를 정책별 200판 실행.',
    bank: {count: bank.length, uniquePrompts, promptStyleOk, wrongExplanationChecks: boot.wrongExplanationChecks, mathBankOk},
    firstTap: {titleTapToPracticeMs, passed: titleTapToPracticeMs <= 500},
    longArcPractice: {longArcPracticeToRunMs, expected: '한 grab에서 90° 1회로 잠기고 실전 진입', passed: longArcPracticeToRunMs <= 5000},
    degenerateWrong: {placement: degeneratePlacement, before: degenerateBefore, after: degenerateAfter,
      expected: '두 대각선이 한 직선에 놓여 사각형이 만들어지지 않는다', passed: degenerateExplanationOk},
    layouts,
    clean,
    recovery,
    bots,
    consoleErrors: errors,
    passed: mathBankOk && titleTapToPracticeMs <= 500 && longArcPracticeToRunMs <= 5000 && degenerateExplanationOk && layouts.every(item => item.panelsInside) &&
      clean.phase === 'clear' && clean.launched >= 12 && clean.lives === 3 &&
      recovery.phase === 'clear' && recovery.launched >= 12 && recovery.lives === 2 &&
      bots.every(bot => bot.passed) && errors.length === 0
  };
  fs.writeFileSync(path.join(HERE, 'playthrough-results.json'), JSON.stringify(result, null, 2));
  fs.writeFileSync(path.join(HERE, 'bot-results.json'), JSON.stringify({
    runId: result.runId,
    artifactHashSha256: result.artifactHashSha256,
    criterion: '정책별 200판 첫 시도 정답률이 해당 묶음의 accepted/legal 가중 우연 기준을 유의하게(+2%p) 넘지 않고, 무입력 진도·완주가 0',
    bots,
    passed: bots.every(bot => bot.passed)
  }, null, 2));
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) exitCode = 1;
  await page.close();
} catch (error) {
  console.error(error);
  exitCode = 1;
} finally {
  await browser.close();
  await server.close();
  release();
}
process.exitCode = exitCode;
