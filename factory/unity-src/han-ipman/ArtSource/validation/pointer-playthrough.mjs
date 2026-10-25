#!/usr/bin/env node
// 실제 브라우저 pointerdown→move→pointerup만으로 연습과 실전 두 판을 완주한다.
// __GAME_TEST__는 ready/getState 관찰에만 쓰며 start/answerCorrect/answerWrong은 호출하지 않는다.
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = process.cwd();
const PUBLIC = path.join(ROOT, 'public');
const OUT = path.join(ROOT, 'factory/work/qa/han-ipman/pointer-playthrough.json');
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

function resolveChrome() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome',
    ]) {
      const candidate = path.join(base, build, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
  return undefined;
}

function answerQ(problemId) {
  const match = String(problemId || '').match(/-q(\d+)$/);
  if (!match) throw new Error(`문항 id에서 q를 읽을 수 없음: ${problemId}`);
  return Number(match[1]);
}

const { url, close } = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio', '--hide-scrollbars'],
});

const page = await browser.newPage();
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
const consoleErrors = [];
const pageErrors = [];
page.on('console', (message) => { if (message.type() === 'error') consoleErrors.push(message.text()); });
page.on('pageerror', (error) => pageErrors.push(String(error)));

async function state() {
  return page.evaluate(() => JSON.parse(JSON.stringify(window.__GAME_TEST__.getState())));
}

async function waitForState(label, predicate, timeout = 5000) {
  const deadline = Date.now() + timeout;
  let last = null;
  while (Date.now() < deadline) {
    last = await state();
    if (predicate(last)) return last;
    await sleep(80);
  }
  throw new Error(`${label} 대기 실패: ${JSON.stringify(last)}`);
}

let pointerSubmissions = 0;
async function dragToQ(q) {
  const before = await state();
  const width = 390;
  const height = 844;
  const cx = before.cakeXPermille / 1000 * width;
  // Unity screen 좌표는 아래에서 위, Puppeteer CSS 좌표는 위에서 아래다.
  const cy = height - before.cakeYPermille / 1000 * height;
  const radius = before.outerRadiusPermille / 1000 * Math.min(width, height) * 1.10;
  const radians = (before.oaTick + q) * 15 * Math.PI / 180;
  const x = cx + Math.cos(radians) * radius;
  const y = cy - Math.sin(radians) * radius;
  await page.mouse.move(cx, cy);
  await page.mouse.down();
  for (let i = 1; i <= 8; i++) {
    await page.mouse.move(cx + (x - cx) * i / 8, cy + (y - cy) * i / 8, { steps: 1 });
  }
  await page.mouse.up();
  pointerSubmissions++;
  return waitForState('실제 드래그 제출', (next) => next.submittedCount > before.submittedCount, 1800);
}

async function enterPractice() {
  await page.mouse.click(195, 720);
  return waitForState('연습 진입', (s) => String(s.problemId).startsWith('practice-'), 2500);
}

async function finishPractice() {
  const practice = await state();
  await dragToQ(answerQ(practice.problemId));
  return waitForState('실전 1번 진입', (s) => s.roundId === 1 && !String(s.problemId).startsWith('practice-'), 3000);
}

async function playRun({ wrongFirst }) {
  const rounds = [];
  for (let round = 1; round <= 8; round++) {
    let current = await waitForState(`실전 ${round}번`, (s) => s.roundId === round && s.phase === 'playing', 3200);
    const q = answerQ(current.problemId);
    if (wrongFirst && round === 1) {
      const wrongQ = q === 23 ? 22 : q + 1;
      await dragToQ(wrongQ);
      await sleep(1350);
      current = await waitForState('수선 가능 상태', (s) => s.roundId === 1 && s.repairMode && s.lives === 2, 1500);
      await dragToQ(q);
    } else {
      await dragToQ(q);
    }
    rounds.push({ round, problemId: current.problemId, answerQ: q, repaired: wrongFirst && round === 1 });
    if (round < 8) await waitForState(`실전 ${round + 1}번 전환`, (s) => s.roundId === round + 1, 3200);
  }
  const final = await waitForState('결과 화면', (s) => s.phase === 'clear' || s.phase === 'gameover', 3500);
  return { final, rounds };
}

try {
  await page.goto(`${url}/g/han-ipman/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 20000 });

  await enterPractice();
  await finishPractice();
  const repairedRun = await playRun({ wrongFirst: true });
  if (repairedRun.final.phase !== 'clear' || repairedRun.final.firstCorrect !== 7 || repairedRun.final.lives !== 2) {
    throw new Error(`수선 판 결과 불일치: ${JSON.stringify(repairedRun.final)}`);
  }

  await enterPractice();
  await finishPractice();
  const cleanRun = await playRun({ wrongFirst: false });
  if (cleanRun.final.phase !== 'clear' || cleanRun.final.firstCorrect !== 8 || cleanRun.final.lives !== 3) {
    throw new Error(`무실수 판 결과 불일치: ${JSON.stringify(cleanRun.final)}`);
  }
  if (consoleErrors.length || pageErrors.length) {
    throw new Error(`브라우저 오류: ${JSON.stringify({ consoleErrors, pageErrors })}`);
  }

  const report = {
    passed: true,
    answerHooksUsed: 0,
    pointerSubmissions,
    repairedRun: repairedRun.final,
    cleanRun: cleanRun.final,
    consoleErrors,
    pageErrors,
  };
  fs.mkdirSync(path.dirname(OUT), { recursive: true });
  fs.writeFileSync(OUT, `${JSON.stringify(report, null, 2)}\n`);
  console.log(JSON.stringify(report, null, 2));
} finally {
  await page.close().catch(() => {});
  await browser.close().catch(() => {});
  await close().catch(() => {});
}
