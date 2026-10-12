#!/usr/bin/env node
// 2차 수정 회귀: 닮음비 용어, 연습 보조, 정답 청사진 사건, 3개 가로 판형 bounds를 최종 WebGL에서 검증한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/geurimja-geukjang');
const FRAMES = path.join(HERE, 'fix2');
fs.mkdirSync(FRAMES, {recursive: true});
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
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
  throw new Error('Chrome for Testing not found');
}

function artifactHash() {
  const files = [];
  (function walk(dir) {
    for (const name of fs.readdirSync(dir).sort()) {
      const file = path.join(dir, name);
      if (fs.statSync(file).isDirectory()) walk(file); else files.push(file);
    }
  })(GAME);
  const hash = crypto.createHash('sha256');
  for (const file of files) { hash.update(path.relative(GAME, file)); hash.update(fs.readFileSync(file)); }
  return hash.digest('hex');
}

function solve(prompt) {
  let m = prompt.match(/넓이의 비를 (\d+):(\d+)로 맞추도록/);
  if (m) for (let d = 2; d <= 10; d++) if (144 * Number(m[2]) === d * d * Number(m[1])) return d;
  m = prompt.match(/인형의 길이는 (\d+) cm이고 그림자의 길이는 (\d+) cm/);
  if (m) return 12 * Number(m[1]) / Number(m[2]);
  m = prompt.match(/닮음비를 (\d+):(\d+)로 맞추도록/);
  if (m) return 12 * Number(m[2]) / Number(m[1]);
  throw new Error(`발문 정수 풀이 실패: ${prompt}`);
}

const xFor = d => 390 * (.12 + (d - 2) * .70 / 8);
async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, timeout = 8000) {
  const end = Date.now() + timeout; let s;
  while (Date.now() < end) { s = await state(page); if (predicate(s)) return s; await sleep(60); }
  throw new Error(`상태 대기 초과: ${JSON.stringify(s)}`);
}
async function drag(page, x1, y1, x2, y2) {
  await page.mouse.move(x1, y1); await page.mouse.down(); await sleep(70);
  await page.mouse.move(x2, y2, {steps: 5}); await sleep(70); await page.mouse.up(); await sleep(100);
}
async function dragPuppet(page, d) { await drag(page, xFor(d === 2 ? 3 : d - 1), 608, xFor(d), 608); }
async function pullCurtain(page) { await drag(page, 343, 397, 343, 540); }

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']});
const errors = [];
try {
  const page = await browser.newPage();
  await page.setViewport({width: 390, height: 844, deviceScaleFactor: 1});
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/geurimja-geukjang/`, {waitUntil: 'load', timeout: 45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  await page.screenshot({path: path.join(FRAMES, '00-title-mobile.png')});

  const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(432));
  const lengthProblems = bank.filter(p => p.id.includes('-len-'));
  const independentlySolved = bank.map(p => ({
    id: p.id,
    expected: solve(p.prompt),
    reported: Number(p.answerNumeric),
    answer: p.answer
  }));
  const bankMath = {
    count: bank.length,
    uniqueIds: new Set(bank.map(p => p.id)).size,
    lengthCount: bank.filter(p => p.id.includes('-len-')).length,
    reverseCount: bank.filter(p => p.id.includes('-rev-')).length,
    areaCount: bank.filter(p => p.id.includes('-area-')).length,
    allIndependentSolutionsMatch: independentlySolved.every(x => x.expected === x.reported && x.answer === `${x.expected} cm`),
    mismatchIds: independentlySolved.filter(x => x.expected !== x.reported || x.answer !== `${x.expected} cm`).map(x => x.id)
  };
  const terminology = {
    lengthCount: lengthProblems.length,
    allUseSimilarityRatio: lengthProblems.every(p => p.prompt.includes('닮음비')),
    legacyLengthPhraseCount: lengthProblems.filter(p => p.prompt.includes('그림자와 인형의 길이의 비')).length
  };

  const startAt = Date.now();
  // Unity polls pointer edges per rendered frame; keep the press across frames so
  // headless Chromium does not collapse down/up into a single missed poll.
  await page.mouse.move(195, 422); await page.mouse.down(); await sleep(120); await page.mouse.up();
  await waitFor(page, s => s.onboarding === true);
  // 검수와 같은 첫 실패 입력: 공개된 6 cm 대신 4 cm로 끈다. 연습 자석이 6 cm로 보조한다.
  await dragPuppet(page, 4);
  let s = await waitFor(page, x => x.onboarding && x.selectedD === 6 && x.guideStep === 'curtain');
  await sleep(450);
  await page.screenshot({path: path.join(FRAMES, '01-moving-handle-guide.png')});
  // 손잡이를 정확히 찾지 못한 짧은 탭도 손잡이에 흡착되어 자동 당김을 보여 준다.
  await page.mouse.move(110, 330); await page.mouse.down(); await sleep(80); await page.mouse.up();
  s = await waitFor(page, x => x.onboarding === false && x.solved === 0);
  const onboardingMs = Date.now() - startAt;
  await page.screenshot({path: path.join(FRAMES, '02-main-under-30s.png')});

  const answer = solve(s.prompt);
  await dragPuppet(page, answer); await pullCurtain(page);
  s = await waitFor(page, x => x.frozen === true && x.feedback);
  const correctFeedback = s.feedback;
  await page.screenshot({path: path.join(FRAMES, '03-cyanotype-develops.png')});
  await sleep(1800);

  const layouts = [];
  for (const [width, height, file] of [[1280,800,'04-layout-1280x800.png'],[1440,900,'05-layout-1440x900.png'],[2000,1045,'06-layout-2000x1045.png']]) {
    await page.setViewport({width, height, deviceScaleFactor: 1});
    s = await waitFor(page, x => x.layoutInfo?.startsWith(`${width}x${height}`));
    await page.screenshot({path: path.join(FRAMES, file)});
    layouts.push({width, height, safe: s.layoutSafe, bounds: s.layoutInfo});
  }

  const cover = await browser.newPage();
  await cover.setViewport({width: 1200, height: 630, deviceScaleFactor: 1});
  await cover.goto(`${server.url}/g/geurimja-geukjang/`, {waitUntil: 'load', timeout: 45000});
  await cover.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  await sleep(500); await cover.screenshot({path: path.join(GAME, 'thumb.png')});
  await cover.setViewport({width: 1080, height: 1080, deviceScaleFactor: 1});
  await sleep(500); await cover.screenshot({path: path.join(GAME, 'square.png')});
  await cover.close();

  const result = {
    runId: `geurimja-geukjang-fix2-${new Date().toISOString()}`,
    artifactHashSha256: artifactHash(), terminology, bankMath, onboardingMs,
    onboarding: {wrongRailDragSnappedTo6: true, missedHandleTapAssistedToMain: true, under30Seconds: onboardingMs < 30000},
    layouts, correctEvent: {answerD: answer, feedback: correctFeedback, screenshot: '03-cyanotype-develops.png'},
    consoleErrors: errors
  };
  result.passed = terminology.allUseSimilarityRatio && terminology.legacyLengthPhraseCount === 0
    && bankMath.count === 432 && bankMath.uniqueIds === 432
    && bankMath.lengthCount === 144 && bankMath.reverseCount === 144 && bankMath.areaCount === 144
    && bankMath.allIndependentSolutionsMatch
    && onboardingMs < 30000 && layouts.every(x => x.safe) && errors.length === 0;
  fs.writeFileSync(path.join(HERE, 'fix2-results.json'), JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
