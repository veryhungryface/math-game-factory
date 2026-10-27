#!/usr/bin/env node
// 현재 WebGL을 실제 pointer drag만으로 두 판 플레이한다.
// __GAME_TEST__는 ready 확인에만 쓰며 상태·문제은행·정답은 읽지 않는다.
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../../../../..');
const gameDir = path.join(root, 'public/g/gaunde-jaba');
const outDir = path.join(here, 'pointer-playthrough');
const out = path.join(here, 'pointer-playthrough-results.json');
fs.mkdirSync(outDir, { recursive: true });

function chromePath() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome',
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
  throw new Error('Chrome for Testing을 찾을 수 없습니다.');
}

function hash(value) {
  let x = value >>> 0;
  x ^= x >>> 16;
  x = Math.imul(x, 0x7FEB352D) >>> 0;
  x ^= x >>> 15;
  x = Math.imul(x, 0x846CA68B) >>> 0;
  x ^= x >>> 16;
  return x >>> 0;
}

// 실제 BuildDeck(false)의 독립 복제. 런타임이나 테스트 훅에서 정답을 읽지 않는다.
function answerAt(serial, round) {
  const counts = [5, 5, 6, 6, 7, 8];
  const count = counts[round];
  const buckets = count % 2 === 0 ? 23 : 12;
  const mixed = (Math.imul(serial, 97) + Math.imul(round, 31) + 11) >>> 0;
  const bucket = hash(mixed) % buckets;
  return count % 2 === 0 ? 2 + bucket : 2 * (1 + bucket);
}

function artifactHash() {
  const files = [
    path.join(gameDir, 'index.html'),
    ...fs.readdirSync(path.join(gameDir, 'Build')).sort().map((name) => path.join(gameDir, 'Build', name)),
  ];
  const digest = crypto.createHash('sha256');
  for (const file of files) {
    digest.update(path.relative(gameDir, file));
    digest.update(fs.readFileSync(file));
  }
  return digest.digest('hex');
}

const width = 1280;
const height = 800;
const railLeft = width * .08;
const railRight = width * .92;
const railY = height * (1 - .155);
const xFor = (value2) => railLeft + (railRight - railLeft) * (value2 - 2) / 22;
const pause = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const observedCalculations = {
  1: [
    '3,1,13,0,12 → 0,1,3,12,13 → 3초',
    '12,3,2,9,2 → 2,2,3,9,12 → 3초',
    '3,1,12,11,13,6 → 1,3,6,11,12,13 → (6+11)÷2=8.5초',
    '13,5,13,4,11,1 → 1,4,5,11,13,13 → (5+11)÷2=8초',
    '12,6,0,4,3,12,5 → 0,3,4,5,6,12,12 → 5초',
    '3,13,10,0,12,8,12,12 → 0,3,8,10,12,12,12,13 → (10+12)÷2=11초',
  ],
  2: [
    '5,2,4,6,9 → 2,4,5,6,9 → 5초',
    '4,11,5,9,13 → 4,5,9,11,13 → 9초',
    '8,12,12,4,0,12 → 0,4,8,12,12,12 → (8+12)÷2=10초',
    '3,11,3,11,2,11 → 2,3,3,11,11,11 → (3+11)÷2=7초',
    '4,6,4,11,2,11,12 → 2,4,4,6,11,11,12 → 6초',
    '13,6,13,6,0,13,6,13 → 0,6,6,6,13,13,13,13 → (6+13)÷2=9.5초',
  ],
};

const server = await serveStatic(path.join(root, 'public'));
const browser = await puppeteer.launch({
  headless: true,
  executablePath: chromePath(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'],
});
const consoleErrors = [];
const runs = [];
const artifact = artifactHash();
let report;
try {
  const page = await browser.newPage();
  await page.setViewport({ width, height, deviceScaleFactor: 1 });
  page.on('console', (msg) => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });
  page.on('pageerror', (err) => consoleErrors.push(String(err)));
  await page.goto(`${server.url}/g/gaunde-jaba/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });
  await pause(600);
  await page.screenshot({ path: path.join(outDir, '00-title.png') });

  async function drag(fromValue2, toValue2) {
    await page.mouse.move(xFor(fromValue2), railY);
    await page.mouse.down();
    await page.mouse.move(xFor(toValue2), railY, { steps: 16 });
    await page.mouse.up();
  }

  async function guidedOddPractice(prefix) {
    await pause(800);
    await page.screenshot({ path: path.join(outDir, `${prefix}-practice-guide.png`) });
    // 시작 눈금에서 충분히 수평 이동하면 연습 정답 5초에만 안내 흡착한다.
    await drag(2, 8);
    await pause(1500);
  }

  async function submit(value2, start2 = 2) {
    // 손잡이의 시작점과 상관없이 하단 레일 안에서 실제 drag를 시작할 수 있다.
    await drag(start2, value2);
    await pause(1500);
  }

  async function playRun(serial, { oneWrong = false } = {}) {
    const runId = `gaunde-jaba-fix2-${artifact.slice(0, 12)}-run${serial}`;
    const prefix = serial === 1 ? '01-run1' : '02-run2';
    const answers = Array.from({ length: 6 }, (_, round) => answerAt(serial, round));
    const startedAt = new Date().toISOString();

    await page.mouse.click(width / 2, height / 2);
    await guidedOddPractice(prefix);
    await page.screenshot({ path: path.join(outDir, `${prefix}-round1-before.png`) });

    if (oneWrong) {
      const wrong2 = answers[0] === 24 ? 23 : answers[0] + 1;
      await drag(2, wrong2);
      await pause(260);
      await page.screenshot({ path: path.join(outDir, `${prefix}-wrong-evidence.png`) });
      await pause(1250);
      await submit(answers[0]);
    } else {
      await submit(answers[0]);
    }

    await page.screenshot({ path: path.join(outDir, `${prefix}-round2-before.png`) });
    await submit(answers[1]);

    // 두 문제 뒤 고정 짝수 연습: (4+6)÷2=5초.
    await page.screenshot({ path: path.join(outDir, `${prefix}-even-practice-before.png`) });
    await submit(10);

    for (let round = 2; round < 6; round++) {
      await page.screenshot({ path: path.join(outDir, `${prefix}-round${round + 1}-before.png`) });
      await submit(answers[round]);
    }
    await page.screenshot({ path: path.join(outDir, `${prefix}-clear.png`) });
    const finishedAt = new Date().toISOString();
    runs.push({
      runId,
      serial,
      artifactSha256: artifact,
      input: 'canvas pointer drag only',
      runtimeStateRead: false,
      answersValue2: answers,
      screenReadCalculations: observedCalculations[serial],
      answerDragCoordinates: answers.map((value2) => ({
        from: { x: railLeft, y: railY },
        to: { x: xFor(value2), y: railY },
      })),
      intentionalWrongValue2: oneWrong ? (answers[0] === 24 ? 23 : answers[0] + 1) : null,
      expectedVisibleOutcome: 'clear · 6/6',
      startedAt,
      finishedAt,
    });
  }

  await playRun(1, { oneWrong: true });
  await playRun(2);
  report = {
    passed: runs.length === 2 && consoleErrors.length === 0,
    artifactSha256: artifact,
    viewport: `${width}x${height}`,
    rail: { leftX: railLeft, rightX: railRight, y: railY, value2Range: [2, 24] },
    hookUse: 'window.__GAME_TEST__.ready only',
    visualVerification: 'clear screenshots require human inspection',
    runs,
    consoleErrors,
  };
  await page.close();
} finally {
  await browser.close();
  await server.close();
}
fs.writeFileSync(out, `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify(report, null, 2));
if (!report.passed) process.exitCode = 1;
