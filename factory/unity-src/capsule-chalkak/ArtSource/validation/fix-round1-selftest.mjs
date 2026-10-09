import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const OUT = path.join(HERE, 'fix-round1');
fs.mkdirSync(OUT, { recursive: true });
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

async function state(page) {
  return page.evaluate(() => window.__GAME_TEST__.getState());
}

async function waitFor(page, predicate, timeout = 5000) {
  const end = Date.now() + timeout;
  let value;
  while (Date.now() < end) {
    value = await state(page);
    if (predicate(value)) return value;
    await sleep(45);
  }
  throw new Error(`state timeout: ${JSON.stringify(value)}`);
}

async function tap(page, x, y) {
  await page.mouse.move(x, y);
  await page.mouse.down();
  await sleep(75);
  await page.mouse.up();
  await sleep(90);
}

async function chain(page, value, indices) {
  if (!indices.length) return;
  const point = index => [value.tilePx[index * 2], value.tilePx[index * 2 + 1]];
  await page.mouse.move(...point(indices[0]));
  await page.mouse.down();
  await sleep(55);
  for (let i = 1; i < indices.length; i++) {
    await page.mouse.move(...point(indices[i]), { steps: 3 });
    await sleep(55);
  }
  await page.mouse.up();
  await sleep(90);
}

async function selectByTaps(page, value, indices) {
  for (const index of indices) {
    const fresh = await state(page);
    await tap(page, fresh.tilePx[index * 2], fresh.tilePx[index * 2 + 1]);
  }
}

async function pullLever(page, value) {
  for (let attempt = 0; attempt < 3; attempt++) {
    const fresh = await state(page);
    const x = fresh.controlPx[0], y = fresh.controlPx[1];
    const endY = Math.min(fresh.screenH - 5, y + Math.max(92, Math.min(170, fresh.screenH - y - 5)));
    await page.mouse.move(x, y);
    await page.mouse.down();
    await sleep(80);
    await page.mouse.move(x, endY, { steps: 10 });
    await sleep(100);
    const held = await state(page);
    await page.mouse.up();
    if (held.leverPullRatio >= .99) return waitFor(page, next => next.phase === 'reveal', 2500);
    await sleep(160);
  }
  throw new Error(`lever threshold not reached after retries: ${JSON.stringify(await state(page))}`);
}

async function openGame(browser, serverUrl, width, height, errors) {
  const page = await browser.newPage();
  await page.setViewport({ width, height, deviceScaleFactor: 1, isMobile: width < 600, hasTouch: width < 600 });
  page.on('console', message => { if (message.type() === 'error') errors.push(`[${width}x${height}] ${message.text()}`); });
  page.on('pageerror', error => errors.push(`[${width}x${height}] ${String(error)}`));
  await page.goto(`${serverUrl}/g/capsule-chalkak/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 20000 });
  return page;
}

async function enterPractice(page, width, height) {
  const before = await state(page);
  // title controlPx를 쓰지 않고 화면 중앙을 눌러 '어디든 시작' 경로를 검증한다.
  await tap(page, Math.round(width / 2), Math.round(height / 2));
  const practice = await waitFor(page, value => value.phase === 'practice' && value.onboarding, 3000);
  if (practice.pointerVersion <= before.pointerVersion) throw new Error('central title tap did not reach Unity input');
  return practice;
}

async function finishPractice(page, practice) {
  await chain(page, practice, [2, 3]);
  const selected = await state(page);
  if (selected.selectedCount !== 2) throw new Error(`practice chain failed: ${JSON.stringify(selected)}`);
  const reveal = await pullLever(page, selected);
  if (reveal.score !== 20 || reveal.lives !== 3) throw new Error(`practice reveal failed: ${JSON.stringify(reveal)}`);
  return waitFor(page, value => value.phase === 'playing' && !value.onboarding, 4000);
}

function bounds(stateValue) {
  const names = ['prompt', 'goal', 'board', 'lever', 'rawFraction'];
  const result = {};
  for (let i = 0; i < names.length; i++) result[names[i]] = stateValue.layoutPx.slice(i * 4, i * 4 + 4);
  return result;
}

function overlap(a, b) {
  return Math.max(0, Math.min(a[2], b[2]) - Math.max(a[0], b[0]))
    * Math.max(0, Math.min(a[3], b[3]) - Math.max(a[1], b[1]));
}

function validateLayout(value) {
  const rects = bounds(value);
  const violations = [];
  for (const [name, r] of Object.entries(rects)) {
    if (r.length !== 4 || r[0] < -2 || r[1] < -2 || r[2] > value.screenW + 2 || r[3] > value.screenH + 2 || r[2] <= r[0] || r[3] <= r[1]) {
      violations.push(`${name} out of bounds: ${JSON.stringify(r)}`);
    }
  }
  for (const [a, b] of [['prompt', 'board'], ['board', 'lever'], ['prompt', 'goal'], ['board', 'rawFraction'], ['lever', 'rawFraction']]) {
    if (overlap(rects[a], rects[b]) > 4) violations.push(`${a}/${b} overlap=${overlap(rects[a], rects[b])}`);
  }
  const xs = [];
  for (let i = 0; i < value.tilePx.length; i += 2) xs.push(value.tilePx[i]);
  if (xs.length && Math.max(...xs) >= rects.lever[0]) violations.push(`tile/lever x collision: maxTile=${Math.max(...xs)} leverLeft=${rects.lever[0]}`);
  return { rects, violations, passed: violations.length === 0 };
}

function correctIndices(value, complement = false) {
  const labels = value.labels;
  const prompt = value.prompt;
  if (value.currentProblem.startsWith('run-bag')) {
    const m = prompt.match(/(빨간|파란|흰|검은) 공이 나올 확률/);
    if (!m) throw new Error(`bag target not found: ${prompt}`);
    const noun = { 빨간: '빨강', 파란: '파랑', 흰: '하양', 검은: '검정' }[m[1]];
    return labels.map((label, i) => label.startsWith(noun) ? i : -1).filter(i => i >= 0);
  }
  if (value.currentProblem === 'run-certain') {
    return prompt.includes('하얀 공이 나올') ? labels.map((_, i) => i) : [];
  }
  if (value.currentProblem.startsWith('run-dice')) {
    const sum = Number(prompt.match(/합이 (\d+)일/)[1]);
    return labels.map((label, i) => {
      const m = label.match(/\((\d+),(\d+)\)/);
      return m && Number(m[1]) + Number(m[2]) === sum ? i : -1;
    }).filter(i => i >= 0);
  }
  if (value.currentProblem === 'run-coins') {
    return labels.map((label, i) => complement ? (label === '뒤뒤뒤' ? i : -1) : (label !== '뒤뒤뒤' ? i : -1)).filter(i => i >= 0);
  }
  if (value.currentProblem === 'run-coindie') {
    const prime = prompt.includes('소수의 눈');
    return labels.map((label, i) => {
      const die = Number(label.split('·')[1]);
      const ok = prime ? [2, 3, 5].includes(die) : die % 2 === 0;
      return label.startsWith('앞·') && ok ? i : -1;
    }).filter(i => i >= 0);
  }
  throw new Error(`unknown problem: ${value.currentProblem}`);
}

async function waitAfterReveal(page) {
  return waitFor(page, value => value.phase === 'playing' || value.phase === 'clear' || value.phase === 'lost', 4500);
}

async function solveRun(page, runNo) {
  const actions = [];
  let wrongFrame = null;
  let complementFrame = null;
  while (true) {
    let value = await state(page);
    if (value.phase === 'clear' || value.phase === 'lost') return { end: value, actions, wrongFrame, complementFrame };
    if (value.phase !== 'playing') value = await waitFor(page, v => v.phase === 'playing' || v.phase === 'clear' || v.phase === 'lost', 4500);
    if (value.phase !== 'playing') continue;

    const order = value.order;
    let useComplement = runNo === 2 && value.currentProblem === 'run-coins';
    if (useComplement) {
      await tap(page, value.controlPx[2], value.controlPx[3]);
      value = await waitFor(page, v => v.complementMode === true, 1500);
      complementFrame = `run2-order${order}-complement.png`;
      await page.screenshot({ path: path.join(OUT, complementFrame) });
    }
    const correct = correctIndices(value, useComplement);

    if (runNo === 1 && order === 1) {
      const missing = correct[correct.length - 1];
      await selectByTaps(page, value, correct.slice(0, -1));
      const partial = await state(page);
      const wrong = await pullLever(page, partial);
      if (wrong.lives !== 2 || wrong.misconceptionId !== 'condition_inside_missing') throw new Error(`expected recoverable miss: ${JSON.stringify(wrong)}`);
      wrongFrame = 'run1-order1-missing.png';
      await page.screenshot({ path: path.join(OUT, wrongFrame) });
      value = await waitFor(page, v => v.phase === 'playing' && v.currentProblem === partial.currentProblem, 3500);
      await selectByTaps(page, value, [missing]);
      actions.push({ run: 1, order, strategy: '누락 오답 뒤 같은 선택에 마지막 경우 추가', path: [...correct.slice(0, -1), missing] });
    } else if (runNo === 2 && order === 1 && correct.length >= 2) {
      let pair = [correct[0], correct[1]];
      let best = Infinity;
      for (let i = 0; i < correct.length; i++) for (let j = i + 1; j < correct.length; j++) {
        const a = correct[i], b = correct[j];
        const dx = value.tilePx[a * 2] - value.tilePx[b * 2];
        const dy = value.tilePx[a * 2 + 1] - value.tilePx[b * 2 + 1];
        const d = Math.hypot(dx, dy);
        if (d < best) { best = d; pair = [a, b]; }
      }
      await selectByTaps(page, value, pair);
      let afterTwo = await state(page);
      await chain(page, afterTwo, [pair[1], pair[0]]); // 실제로 붙어 있는 직전 칸 되짚기
      let afterUndo = await state(page);
      const remainder = correct.filter(index => index !== pair[0]).reverse();
      await selectByTaps(page, afterUndo, remainder);
      actions.push({ run: 2, order, strategy: '인접 두 칸 연결→한 칸 되짚기→역순 경로', path: [pair[0], pair[1], pair[0], ...remainder] });
    } else {
      const pathOrder = runNo === 2 ? [...correct].reverse() : correct;
      await selectByTaps(page, value, pathOrder);
      actions.push({ run: runNo, order, strategy: useComplement ? '여사건 링으로 뒤뒤뒤 한 칸' : (runNo === 2 ? '정답 집합 역순 체인' : '정답 집합 정순 체인'), path: pathOrder });
    }

    value = await state(page);
    const reveal = await pullLever(page, value);
    if (reveal.lives <= 0) throw new Error(`unexpected game over: ${JSON.stringify(reveal)}`);
    await waitAfterReveal(page);
  }
}

const errors = [];
const responsive = [];
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  const squareTitlePage = await openGame(browser, server.url, 1080, 1080, errors);
  await squareTitlePage.screenshot({ path: path.join(OUT, 'title-1080x1080.png') });
  await squareTitlePage.close();

  for (const [width, height] of [[1280, 800], [1440, 900], [2000, 1045]]) {
    const page = await openGame(browser, server.url, width, height, errors);
    const practice = await enterPractice(page, width, height);
    const playing = await finishPractice(page, practice);
    const layout = validateLayout(playing);
    const file = `playing-${width}x${height}.png`;
    await page.screenshot({ path: path.join(OUT, file) });
    responsive.push({ viewport: `${width}x${height}`, phase: playing.phase, problem: playing.currentProblem, layout, screenshot: file });
    await page.close();
  }

  const page = await openGame(browser, server.url, 390, 844, errors);
  let practice = await enterPractice(page, 390, 844);
  await page.screenshot({ path: path.join(OUT, 'first-central-tap-practice.png') });
  await finishPractice(page, practice);
  const run1 = await solveRun(page, 1);
  await page.screenshot({ path: path.join(OUT, 'run1-end.png') });
  if (run1.end.phase !== 'clear' || run1.end.firstAttemptCorrect !== 5 || run1.end.firstAttemptTotal !== 6 || run1.end.lives !== 2) {
    throw new Error(`run1 result mismatch: ${JSON.stringify(run1.end)}`);
  }

  await tap(page, run1.end.controlPx[6], run1.end.controlPx[7]);
  practice = await waitFor(page, value => value.phase === 'practice' && value.onboarding, 3000);
  await page.screenshot({ path: path.join(OUT, 'run2-restart-practice.png') });
  await finishPractice(page, practice);
  const run2 = await solveRun(page, 2);
  await page.screenshot({ path: path.join(OUT, 'run2-end.png') });
  if (run2.end.phase !== 'clear' || run2.end.firstAttemptCorrect !== 6 || run2.end.firstAttemptTotal !== 6 || run2.end.lives !== 3 || run2.end.complementRings !== 1) {
    throw new Error(`run2 result mismatch: ${JSON.stringify(run2.end)}`);
  }

  const output = {
    generatedAt: new Date().toISOString(),
    input: 'actual Puppeteer mouse down/move/up only; no answer hooks',
    responsive,
    secondPlay: {
      restart: 'run1 결과 화면의 실제 restart handle pointer tap',
      run1: { result: run1.end, actions: run1.actions, wrongFrame: run1.wrongFrame, screenshot: 'run1-end.png' },
      run2: { result: run2.end, actions: run2.actions, complementFrame: run2.complementFrame, screenshot: 'run2-end.png' },
      changedStrategy: 'run1은 첫 주문 누락 오답 후 회복·정순 체인, run2는 되짚기·역순 체인·여사건 링을 사용',
    },
    consoleErrors: errors,
    passed: responsive.every(item => item.layout.passed) && errors.length === 0,
  };
  fs.writeFileSync(path.join(HERE, 'fix-round1-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify({
    passed: output.passed,
    responsive: responsive.map(item => ({ viewport: item.viewport, phase: item.phase, violations: item.layout.violations })),
    run1: { phase: run1.end.phase, lives: run1.end.lives, first: `${run1.end.firstAttemptCorrect}/${run1.end.firstAttemptTotal}`, score: run1.end.score },
    run2: { phase: run2.end.phase, lives: run2.end.lives, first: `${run2.end.firstAttemptCorrect}/${run2.end.firstAttemptTotal}`, score: run2.end.score, rings: run2.end.complementRings },
    consoleErrors: errors,
  }, null, 2));
  if (!output.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
