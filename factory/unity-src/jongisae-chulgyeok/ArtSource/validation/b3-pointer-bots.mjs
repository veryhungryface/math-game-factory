#!/usr/bin/env node
// B3 무뇌 봇 4종×200판. 첫 유효 제출은 CDP page.mouse가 만든 실제 pointer 이벤트만 사용한다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';
import { acquireMachineLock } from '../../../../lib/machine-lock.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
const RUNS = 200;
const CHANCE = 1 / 60; // 현재 startIndex를 제외한 60개 유효 해제 눈금 중 하나가 정답.
const ONLY = process.env.B3_POLICY || '';
const RESULT_PATH = path.join(HERE, 'b3-pointer-results.json');

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome'
    ]) {
      const p = path.join(base, build, rel);
      if (fs.existsSync(p)) return p;
    }
  }
  throw new Error('Chrome for Testing을 찾지 못함');
}

function lcg(seed) {
  let s = seed >>> 0;
  return () => ((s = (Math.imul(s, 1664525) + 1013904223) >>> 0) / 4294967296);
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }

async function startRun(page) {
  await page.evaluate(() => window.__GAME_TEST__.start());
  await page.waitForFunction(() => {
    const s = window.__GAME_TEST__.getState();
    if (!(s.phase === 'playing' && s.subphase === 'playing' && s.problemId
      && s.handlePxX > 1 && s.handlePxY > 1 && s.pivotPxX > 1 && s.pivotPxY > 1)) return false;
    const match = /-r(\d+)$/.exec(s.problemId);
    const rotation = match ? Number(match[1]) : 0;
    const dx = s.handlePxX - s.pivotPxX, dy = s.handlePxY - s.pivotPxY;
    let seen = Math.atan2(-dy, dx) * 180 / Math.PI;
    if (seen < 0) seen += 360;
    const expected = (s.inputAngle + rotation * 90) % 360;
    const diff = Math.abs(((seen - expected + 540) % 360) - 180);
    return diff < 3;
  }, { timeout: 3000 });
  await sleep(60);
  return state(page);
}

function pointAtIndex(s, targetIndex) {
  const dx = s.handlePxX - s.pivotPxX;
  const dy = s.handlePxY - s.pivotPxY;
  const delta = (20 + targetIndex * 2 - s.inputAngle) * Math.PI / 180;
  const c = Math.cos(delta), sn = Math.sin(delta);
  return [s.pivotPxX + c * dx + sn * dy, s.pivotPxY - sn * dx + c * dy];
}

async function actualRelease(page, s, targetIndex) {
  // 같은 눈금에서 놓으면 유효 제출이 아니다. 무판단 정책이 현재값을 만났을 때는 정반대 쪽
  // 고정 눈금으로 옮긴다. 어느 경우든 q는 start에서 제외되어 있어 조건부 우연은 1/60이다.
  if (targetIndex === s.inputIndex) targetIndex = (targetIndex + 30) % 61;
  let to = pointAtIndex(s, targetIndex);
  if (Math.hypot(to[0] - s.handlePxX, to[1] - s.handlePxY) < 16) {
    targetIndex = (targetIndex + 30) % 61;
    to = pointAtIndex(s, targetIndex);
  }
  await page.mouse.move(s.handlePxX, s.handlePxY);
  await page.mouse.down();
  await sleep(45);
  await page.mouse.move((s.handlePxX + to[0]) / 2, (s.handlePxY + to[1]) / 2, { steps: 2 });
  await sleep(45);
  await page.mouse.move(to[0], to[1], { steps: 2 });
  await sleep(45);
  await page.mouse.up();
  try {
    await page.waitForFunction(() => window.__GAME_TEST__.getState().firstAttemptTotal === 1,
      { timeout: 2500 });
  } catch (error) {
    const failed = await state(page);
    await page.screenshot({ path: path.join(HERE, 'b3-pointer-debug.png') });
    console.error('pointer release failed', JSON.stringify({ before: s, targetIndex, to, failed }));
    throw error;
  }
  return state(page);
}

const releaseLock = await acquireMachineLock('gpu', { label: 'jongisae-chulgyeok B3 실제 포인터 봇' });
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  protocolTimeout: 120000,
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio', '--enable-unsafe-swiftshader', '--use-gl=angle', '--use-angle=swiftshader']
});

const report = {
  generatedAt: new Date().toISOString(),
  runsPerPolicy: RUNS,
  chanceRate: CHANCE,
  chancePercent: CHANCE * 100,
  actualPointer: true,
  policies: {},
  consoleErrors: []
};
if (ONLY && fs.existsSync(RESULT_PATH)) {
  const prior = JSON.parse(fs.readFileSync(RESULT_PATH, 'utf8'));
  report.policies = prior.policies || {};
}

try {
  const page = await browser.newPage();
  page.on('console', m => { if (m.type() === 'error') report.consoleErrors.push(m.text()); });
  page.on('pageerror', e => report.consoleErrors.push(String(e)));
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  await page.goto(`${server.url}/g/jongisae-chulgyeok/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });

  const random = lcg(0x20261016);
  const policies = {
    repeat: (_run, _s) => 30, // 중앙 80°를 계속 놓는 고정 연타
    cycle: (run, _s) => [0, 20, 40, 60][run % 4],
    random: (_run, s) => {
      const raw = Math.floor(random() * 60);
      return raw >= s.inputIndex ? raw + 1 : raw;
    }
  };

  for (const [name, policy] of Object.entries(policies)) {
    if (ONLY && ONLY !== name) continue;
    let firstAttempts = 0, firstCorrect = 0, completions = 0, exposed = 0;
    for (let run = 0; run < RUNS; run++) {
      const before = await startRun(page);
      exposed++;
      const after = await actualRelease(page, before, policy(run, before));
      firstAttempts += after.firstAttemptTotal;
      firstCorrect += after.firstAttemptCorrect;
      if (after.phase === 'clear') completions++;
    }
    report.policies[name] = {
      runs: RUNS,
      exposedProblems: exposed,
      firstAttempts,
      firstCorrect,
      firstAttemptRate: firstAttempts ? firstCorrect / firstAttempts : 0,
      firstAttemptPercent: firstAttempts ? firstCorrect / firstAttempts * 100 : 0,
      completionRate: completions / RUNS
    };
  }

  if (!ONLY || ONLY === 'noInput') {
    let idleProgress = 0;
    for (let run = 0; run < RUNS; run++) {
      await startRun(page);
      await sleep(8);
      const after = await state(page);
      if (after.solved !== 0 || after.firstAttemptTotal !== 0 || after.score !== 0) idleProgress++;
    }
    report.policies.noInput = {
      runs: RUNS,
      exposedProblems: RUNS,
      firstAttempts: 0,
      firstCorrect: 0,
      firstAttemptRate: 0,
      firstAttemptPercent: 0,
      completionRate: 0,
      progressRuns: idleProgress
    };
  }

  report.pass = report.consoleErrors.length === 0
    && Object.values(report.policies).every(p => p.firstAttemptRate <= CHANCE + 1e-12)
    && report.policies.noInput.progressRuns === 0;
  fs.writeFileSync(RESULT_PATH, JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify(report, null, 2));
  if (!report.pass) process.exitCode = 1;
} finally {
  await browser.close();
  server.close();
  await releaseLock();
}
