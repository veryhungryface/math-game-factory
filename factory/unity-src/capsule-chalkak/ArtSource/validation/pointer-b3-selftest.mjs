import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/capsule-chalkak');
const FRAMES = path.join(HERE, 'pointer-playthrough');
fs.mkdirSync(FRAMES, { recursive: true });

const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    ]) {
      const candidate = path.join(base, build, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
  throw new Error('Chrome for Testing not found');
}

function buildHash() {
  const hash = crypto.createHash('sha256');
  const names = ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(name => `Build/${name}`)];
  for (const name of names) hash.update(fs.readFileSync(path.join(GAME, name)));
  return hash.digest('hex');
}

async function state(page) {
  return page.evaluate(() => window.__GAME_TEST__.getState());
}

async function waitFor(page, predicate, timeout = 5000) {
  const end = Date.now() + timeout;
  let last;
  while (Date.now() < end) {
    last = await state(page);
    if (predicate(last)) return last;
    await sleep(30);
  }
  throw new Error(`state timeout: ${JSON.stringify(last)}`);
}

async function restart(page) {
  await page.evaluate(() => window.__GAME_TEST__.start());
  await waitFor(page, value => value.phase === 'playing' && value.tilePx.length > 0, 2500);
  // SendMessage 기반 TestStart와 다음 실제 포인터 입력이 같은 Unity 프레임에
  // 합쳐지지 않도록 새 판의 레이아웃/입력 상태를 한 프레임 이상 안정시킨다.
  await sleep(80);
  return state(page);
}

function tilePoint(value, index) {
  return [value.tilePx[index * 2], value.tilePx[index * 2 + 1]];
}

async function chain(page, value, indices, dwell = 34) {
  if (indices.length === 0) return;
  await page.mouse.move(...tilePoint(value, indices[0]));
  await sleep(dwell);
  await page.mouse.down();
  await sleep(dwell);
  for (let i = 1; i < indices.length; i++) {
    await page.mouse.move(...tilePoint(value, indices[i]));
    await sleep(dwell);
  }
  await page.mouse.up();
  await sleep(dwell);
}

async function pullLever(page, value, dwell = 45) {
  const x = value.controlPx[0];
  const y = value.controlPx[1];
  for (let attempt = 0; attempt < 3; attempt++) {
    await page.mouse.move(x, y);
    await sleep(dwell);
    await page.mouse.down();
    await sleep(dwell);
    // 브라우저/Unity 조합의 Y축 변환을 레버 당김 비율로 실측한다.
    await page.mouse.move(x, Math.min(value.screenH - 4, y + 260), { steps: 8 });
    await sleep(dwell);
    let held = await state(page);
    if (held.leverPullRatio < .99) {
      await page.mouse.move(x, Math.max(4, y - 260), { steps: 8 });
      await sleep(dwell);
      held = await state(page);
    }
    if (held.leverPullRatio < .99) {
      await page.mouse.up();
      await sleep(140);
      continue;
    }
    await page.mouse.up();
    await sleep(140);
    const after = await state(page);
    if (after.phase === 'reveal' || after.phase === 'lost' || after.phase === 'clear') return after;
  }
  return waitFor(page, next => next.phase === 'reveal' || next.phase === 'lost' || next.phase === 'clear', 1800);
}

function correctIndices(value) {
  const prompt = value.prompt;
  const labels = value.labels;
  let predicate;
  const bag = prompt.match(/,\s*([가-힣]+) 공이 나올 확률/);
  const dice = prompt.match(/합이\s*(\d+)일 확률/);
  if (bag) {
    const target = bag[1] === '하얀' ? '하양' : bag[1] === '검은' ? '검정' : bag[1];
    predicate = label => label.startsWith(`${target} `);
  } else if (dice) {
    const target = Number(dice[1]);
    predicate = label => {
      const pair = label.match(/\((\d+),(\d+)\)/);
      return pair && Number(pair[1]) + Number(pair[2]) === target;
    };
  } else if (prompt.includes('적어도 한 개는 앞면')) {
    predicate = label => label !== '뒤뒤뒤';
  } else if (prompt.includes('동전은 앞면') && prompt.includes('소수')) {
    predicate = label => /^앞·[235]$/.test(label);
  } else if (prompt.includes('동전은 앞면') && prompt.includes('짝수')) {
    predicate = label => /^앞·[246]$/.test(label);
  } else {
    throw new Error(`unknown prompt: ${prompt}`);
  }
  return labels.map((label, index) => predicate(label) ? index : -1).filter(index => index >= 0);
}

async function runRecoveryPlaythrough(page) {
  const actions = [];
  let value = await restart(page);
  const firstProblem = value.currentProblem;
  const firstCorrect = correctIndices(value);
  if (firstCorrect.length < 2) throw new Error(`first problem lacks recovery path: ${JSON.stringify(value)}`);

  // 정답 집합 중 한 칸만 선택해 '누락' 오답을 만든다. 답 밖 칸을 넣지 않으므로
  // 오답 뒤 남은 정답 칸을 실제 체인으로 더해 같은 주문에서 회복할 수 있다.
  await chain(page, value, [firstCorrect[0]]);
  value = await state(page);
  const wrong = await pullLever(page, value);
  if (wrong.lives !== 2 || wrong.firstAttemptTotal !== 1 || wrong.firstAttemptCorrect !== 0) {
    throw new Error(`wrong path did not count once: ${JSON.stringify(wrong)}`);
  }
  actions.push({ action: 'first-order-partial-chain-wrong', state: wrong });
  await page.screenshot({ path: path.join(FRAMES, '01-first-wrong.png') });
  value = await waitFor(page, next => next.phase === 'playing' && next.currentProblem === firstProblem, 2500);
  const remaining = firstCorrect.filter(index => !value.selectedIds.includes(index));
  await chain(page, value, remaining);
  value = await state(page);
  const recovered = await pullLever(page, value);
  if (recovered.solved !== 1 || recovered.lives !== 2) throw new Error(`recovery failed: ${JSON.stringify(recovered)}`);
  actions.push({ action: 'first-order-recovered', state: recovered });
  await page.screenshot({ path: path.join(FRAMES, '02-first-recovered.png') });

  while (true) {
    value = await waitFor(page, next => next.phase === 'playing' || next.phase === 'clear', 3000);
    if (value.phase === 'clear') break;
    const before = value.solved;
    const answer = correctIndices(value);
    await chain(page, value, answer);
    value = await state(page);
    const submitted = await pullLever(page, value);
    if (submitted.solved !== before + 1) throw new Error(`order ${before + 1} failed: ${JSON.stringify(submitted)}`);
    actions.push({ action: `correct-order-${before + 1}`, state: submitted });
  }
  const finalState = await state(page);
  if (finalState.phase !== 'clear' || finalState.solved !== 6 || finalState.lives !== 2) {
    throw new Error(`clear after recovery failed: ${JSON.stringify(finalState)}`);
  }
  await page.screenshot({ path: path.join(FRAMES, '03-clear.png') });
  return { firstProblem, actions, finalState };
}

function sampleWithoutReplacement(count, total) {
  const pool = Array.from({ length: total }, (_, index) => index);
  for (let i = pool.length - 1; i > 0; i--) {
    // B3 입력 자체는 비결정적이어야 하므로 Math.random을 쓴다.
    const j = Math.floor(Math.random() * (i + 1));
    [pool[i], pool[j]] = [pool[j], pool[i]];
  }
  return pool.slice(0, count);
}

async function runBot(page, policy, trials = 200) {
  let firstCorrect = 0;
  let completions = 0;
  let submissions = 0;
  for (let trial = 0; trial < trials; trial++) {
    let value = await restart(page);
    let indices;
    if (policy === 'fixed') indices = [0];
    else if (policy === 'cycle') indices = [trial % 4];
    else if (policy === 'random') indices = sampleWithoutReplacement(4, value.labels.length);
    else throw new Error(`unknown policy: ${policy}`);
    await chain(page, value, indices, 45);
    value = await state(page);
    const result = await pullLever(page, value, 45);
    submissions += result.firstAttemptTotal;
    firstCorrect += result.firstAttemptCorrect;
    if (result.phase === 'clear') completions++;
  }
  return {
    policy,
    trials,
    submissions,
    firstAttemptCorrect: firstCorrect,
    firstAttemptRate: firstCorrect / trials,
    completions,
    completionRate: completions / trials,
    input: policy === 'random' ? '4 random tile positions in random order, then pointer lever drag' : 'one tile position, then pointer lever drag',
  };
}

async function runIdleBot(page, trials = 200) {
  let progress = 0;
  for (let trial = 0; trial < trials; trial++) {
    const value = await restart(page);
    progress += value.solved;
  }
  return {
    policy: 'idle',
    trials,
    submissions: 0,
    firstAttemptCorrect: 0,
    firstAttemptRate: 0,
    completions: 0,
    completionRate: 0,
    progress,
    input: 'no pointer input after each fresh run start',
  };
}

const errors = [];
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: chromePath(),
  args: ['--no-sandbox', '--disable-setuid-sandbox'],
});

try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/capsule-chalkak/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 20000 });
  await page.screenshot({ path: path.join(FRAMES, '00-title.png') });

  const playthrough = await runRecoveryPlaythrough(page);
  const bots = [];
  bots.push(await runBot(page, 'fixed'));
  bots.push(await runBot(page, 'cycle'));
  bots.push(await runBot(page, 'random'));
  bots.push(await runIdleBot(page));

  const chanceRate = 1 / 56;
  const passed = errors.length === 0
    && bots.every(bot => bot.firstAttemptRate <= chanceRate)
    && bots.find(bot => bot.policy === 'idle').progress === 0;
  const output = {
    generatedAt: new Date().toISOString(),
    buildSha256: buildHash(),
    viewport: '390x844',
    input: 'Puppeteer mouse events delivered to the Unity canvas; answerCorrect/answerWrong were never called',
    conservativeChance: { formula: '1/C(8,5)', rate: chanceRate },
    playthrough,
    bots,
    consoleErrors: errors,
    passed,
  };
  fs.writeFileSync(path.join(HERE, 'pointer-b3-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
  if (!passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
