#!/usr/bin/env node
// 화면 캡처만 보는 독립 평가자가 계산한 눈금을 실제 pointerdown→move→pointerup으로 제출한다.
// 금지: answerCorrect/answerWrong, sampleProblems, StateJson의 problemId/prompt, 문제은행, 문항 id 파싱.
// 실행 중 각 캡처를 직접 보고 `q|계산 근거`(q=각도/15)를 입력한다.
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { stdin as input, stdout as output } from 'node:process';
import { createInterface } from 'node:readline/promises';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../../../../..');
const publicDir = path.join(root, 'public');
const outDir = path.join(here, 'independent-play');
const workReport = path.join(root, 'factory/work/qa/han-ipman/pointer-playthrough.json');
const sourceReport = path.join(here, 'independent-play-results.json');
const qaReport = path.join(root, 'factory/work/qa/han-ipman/report.json');
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
fs.mkdirSync(outDir, { recursive: true });

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

function sha256(file) {
  return crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
}

function safeState(s) {
  return {
    score: s.score,
    lives: s.lives,
    level: s.level,
    phase: s.phase,
    solved: s.solved,
    roundId: s.roundId,
    submittedCount: s.submittedCount,
    firstAttempted: s.firstAttempted,
    firstCorrect: s.firstCorrect,
    firstWrong: s.firstWrong,
    repairMode: s.repairMode,
  };
}

function parseDecision(raw) {
  const split = raw.indexOf('|');
  const q = Number((split < 0 ? raw : raw.slice(0, split)).trim());
  const calculation = (split < 0 ? '' : raw.slice(split + 1)).trim();
  if (!Number.isInteger(q) || q < 1 || q > 23) throw new Error('q는 1~23 정수여야 합니다.');
  if (calculation.length < 6) throw new Error('화면에서 읽은 값과 계산 근거를 6자 이상 기록해야 합니다.');
  return { q, angle: q * 15, calculation };
}

const { url, close } = await serveStatic(publicDir);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio', '--hide-scrollbars'],
});
const page = await browser.newPage();
await page.setViewport({ width: 1280, height: 800, deviceScaleFactor: 1 });
const rl = createInterface({ input, output });
const consoleErrors = [];
const pageErrors = [];
const networkFailures = [];
page.on('console', (message) => { if (message.type() === 'error') consoleErrors.push(message.text()); });
page.on('pageerror', (error) => pageErrors.push(String(error)));
page.on('requestfailed', (request) => networkFailures.push(`${request.url()} :: ${request.failure()?.errorText || 'failed'}`));

async function state() {
  return page.evaluate(() => window.__GAME_TEST__.getState());
}

async function waitForState(label, predicate, timeout = 6000) {
  const deadline = Date.now() + timeout;
  let last = null;
  while (Date.now() < deadline) {
    last = await state();
    if (predicate(last)) return last;
    await sleep(80);
  }
  throw new Error(`${label} 대기 실패: ${JSON.stringify(safeState(last || {}))}`);
}

async function capture(file) {
  const target = path.join(outDir, file);
  await page.screenshot({ path: target });
  return { file: path.relative(root, target), sha256: sha256(target) };
}

let pointerSubmissions = 0;
async function dragToQ(q) {
  const before = await state();
  const width = 1280;
  const height = 800;
  const cx = before.cakeXPermille / 1000 * width;
  // Unity 화면 좌표는 아래→위, Puppeteer CSS 좌표는 위→아래다.
  const cy = height - before.cakeYPermille / 1000 * height;
  const radius = before.outerRadiusPermille / 1000 * Math.min(width, height) * 1.10;
  const radians = (before.oaTick + q) * 15 * Math.PI / 180;
  const x = cx + Math.cos(radians) * radius;
  const y = cy - Math.sin(radians) * radius;
  await page.mouse.move(cx, cy);
  await page.mouse.down();
  for (let i = 1; i <= 10; i++) {
    await page.mouse.move(cx + (x - cx) * i / 10, cy + (y - cy) * i / 10, { steps: 1 });
  }
  await page.mouse.up();
  pointerSubmissions++;
  return waitForState('실제 드래그 제출', (next) => next.submittedCount > before.submittedCount, 2200);
}

async function askDecision(label, screenshot) {
  output.write(`\n[화면 증거] ${screenshot.file}\n`);
  while (true) {
    try {
      return parseDecision(await rl.question(`${label} q|계산 근거 > `));
    } catch (error) {
      if (error?.name === 'AbortError' || /closed/i.test(String(error?.message || error))) throw error;
      output.write(`[입력 오류] ${error.message}\n`);
    }
  }
}

async function enterAndFinishPractice(run) {
  await page.mouse.click(640, 700);
  await waitForState(`${run} 연습 진입`, (s) => s.phase === 'playing' && s.roundId === 0, 3500);
  // Flow.Opening도 브리지 phase는 playing이다. 덮개 340ms 연출이 끝나고 실제
  // 연습 발문·식이 화면에 놓인 뒤에만 평가자용 캡처를 만든다.
  await sleep(800);
  const shot = await capture(`${run}-00-practice.png`);
  const decision = await askDecision(`${run} 연습`, shot);
  const before = safeState(await state());
  await dragToQ(decision.q);
  await waitForState(`${run} 실전 진입`, (s) => s.phase === 'playing' && s.roundId === 1, 4200);
  return { screenshot: shot, decision, before, after: safeState(await state()) };
}

async function playRun(run, { forceRepairFirst = false } = {}) {
  const rounds = [];
  for (let round = 1; round <= 8; round++) {
    await waitForState(`${run} ${round}번`, (s) => s.phase === 'playing' && s.roundId === round && !s.repairMode, 4200);
    const promptShot = await capture(`${run}-${String(round).padStart(2, '0')}-prompt.png`);
    const decision = await askDecision(`${run} ${round}/8`, promptShot);
    const before = safeState(await state());
    const attempts = [];
    let performedRepair = false;

    if (forceRepairFirst && round === 1) {
      const deliberateWrongQ = decision.q === 23 ? 22 : decision.q + 1;
      await dragToQ(deliberateWrongQ);
      attempts.push({ q: deliberateWrongQ, angle: deliberateWrongQ * 15, purpose: '수선 경로 검증용 고의 오답' });
      await sleep(1400);
      await waitForState('수선 가능 상태', (s) => s.roundId === 1 && s.repairMode && s.lives === 2, 2000);
      const repairShot = await capture(`${run}-${String(round).padStart(2, '0')}-repair.png`);
      await dragToQ(decision.q);
      attempts.push({ q: decision.q, angle: decision.angle, purpose: '화면 계산값으로 같은 주문 수선', screenshot: repairShot });
      performedRepair = true;
    } else {
      await dragToQ(decision.q);
      attempts.push({ q: decision.q, angle: decision.angle, purpose: '화면 계산값 첫 제출' });
    }

    const advanced = await waitForState(`${run} ${round}번 판정`, (s) =>
      s.phase === 'clear' || s.phase === 'gameover' || s.roundId !== round || (!performedRepair && s.roundId === round && s.repairMode), 5200);
    if (!performedRepair && advanced.roundId === round && advanced.repairMode) {
      throw new Error(`${run} ${round}번 화면 계산이 오답이었습니다. 증거를 보존하고 중단합니다.`);
    }
    const afterShot = await capture(`${run}-${String(round).padStart(2, '0')}-after.png`);
    rounds.push({ round, promptScreenshot: promptShot, decision, attempts, before, after: safeState(advanced), afterScreenshot: afterShot });
  }
  const final = await waitForState(`${run} 결과`, (s) => s.phase === 'clear' || s.phase === 'gameover', 4500);
  const finalShot = await capture(`${run}-09-final.png`);
  return { rounds, final: safeState(final), finalScreenshot: finalShot };
}

try {
  await page.goto(`${url}/g/han-ipman/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 25000 });
  const titleShot = await capture('00-title.png');

  const practice1 = await enterAndFinishPractice('run1');
  const repairedRun = await playRun('run1', { forceRepairFirst: true });
  if (repairedRun.final.phase !== 'clear' || repairedRun.final.firstCorrect !== 7 || repairedRun.final.lives !== 2) {
    throw new Error(`수선 판 결과 불일치: ${JSON.stringify(repairedRun.final)}`);
  }

  const retryDecision = (await rl.question('\n첫 판 결과 화면을 본 뒤 독립 둘째 판을 다시 한 이유 > ')).trim();
  if (retryDecision.length < 8) throw new Error('자발적 재도전 이유를 8자 이상 기록해야 합니다.');
  const practice2 = await enterAndFinishPractice('run2');
  const cleanRun = await playRun('run2');
  if (cleanRun.final.phase !== 'clear' || cleanRun.final.firstCorrect !== 8 || cleanRun.final.lives !== 3) {
    throw new Error(`무실수 둘째 판 결과 불일치: ${JSON.stringify(cleanRun.final)}`);
  }
  if (consoleErrors.length || pageErrors.length || networkFailures.length) {
    throw new Error(`브라우저 오류: ${JSON.stringify({ consoleErrors, pageErrors, networkFailures })}`);
  }

  const artifactHash = fs.existsSync(qaReport) ? JSON.parse(fs.readFileSync(qaReport, 'utf8')).artifact_hash : null;
  const report = {
    passed: true,
    evidenceMode: 'interactive-screen-only-evaluator',
    artifactHash,
    viewport: '1280x800',
    prohibitedSources: {
      answerHooksUsed: 0,
      problemIdRead: false,
      problemBankRead: false,
      promptStateRead: false,
      sampleProblemsUsed: false,
    },
    pointerSubmissions,
    titleScreenshot: titleShot,
    repairedRun: { practice: practice1, ...repairedRun },
    retryDecision,
    cleanSecondRun: { practice: practice2, ...cleanRun },
    consoleErrors,
    pageErrors,
    networkFailures,
  };
  fs.mkdirSync(path.dirname(workReport), { recursive: true });
  fs.writeFileSync(workReport, `${JSON.stringify(report, null, 2)}\n`);
  fs.writeFileSync(sourceReport, `${JSON.stringify(report, null, 2)}\n`);
  output.write(`\n${JSON.stringify({ passed: report.passed, artifactHash, pointerSubmissions, repaired: repairedRun.final, second: cleanRun.final }, null, 2)}\n`);
} finally {
  rl.close();
  await page.close().catch(() => {});
  await browser.close().catch(() => {});
  await close().catch(() => {});
}
