#!/usr/bin/env node
// 1차 수정 회귀: 중앙 탭, 2단계 연습 안내, 넓이비 오답 문구, 2000px 안전 폭을 최종 WebGL에서 확인한다.
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
const FRAMES = path.join(HERE, 'fix1');
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

const gcd = (a, b) => { while (b) [a, b] = [b, a % b]; return Math.abs(a); };
const ratio = (a, b) => { const g = gcd(a, b); return `${a / g}:${b / g}`; };
const xFor = d => 390 * (.12 + (d - 2) * .70 / 8);

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, timeout = 6000) {
  const end = Date.now() + timeout; let s;
  while (Date.now() < end) { s = await state(page); if (predicate(s)) return s; await sleep(60); }
  throw new Error(`상태 대기 초과: ${JSON.stringify(s)}`);
}
async function dragPuppet(page, d) {
  const adjacent = d === 2 ? 3 : d - 1;
  await page.mouse.move(xFor(adjacent), 608); await page.mouse.down(); await sleep(80);
  await page.mouse.move(xFor(d), 608, {steps: 5}); await sleep(80); await page.mouse.up(); await sleep(120);
}
async function pullCurtain(page) {
  await page.mouse.move(343, 397); await page.mouse.down(); await sleep(80);
  await page.mouse.move(343, 540, {steps: 5}); await sleep(80); await page.mouse.up(); await sleep(120);
}

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
  await page.screenshot({path: path.join(FRAMES, '00-live-title-mobile.png')});

  const startAt = Date.now();
  await page.mouse.move(195, 422); await page.mouse.down(); await sleep(90); await page.mouse.up();
  let s = await waitFor(page, x => x.onboarding === true);
  const centerTapMs = Date.now() - startAt;
  await page.screenshot({path: path.join(FRAMES, '01-rail-guide.png')});

  await dragPuppet(page, 6);
  s = await waitFor(page, x => x.onboarding === true && x.selectedD === 6 && x.guideStep === 'curtain');
  await page.screenshot({path: path.join(FRAMES, '02-curtain-guide.png')});
  await pullCurtain(page);
  s = await waitFor(page, x => x.onboarding === false && x.solved === 0, 8000);

  // 검수에서 제시한 동일 사례(목표 16:9, d=8)를 찾는다. 훅은 각 막의 실제
  // SubmitCurtain 판정 경로를 타고, 최종 오답만 trusted pointer로 제출한다.
  let foundReviewCase = false;
  for (let run = 0; run < 18 && !foundReviewCase; run++) {
    if (run > 0) { await page.evaluate(() => window.__GAME_TEST__.start()); await sleep(100); }
    for (let i = 0; i < 5; i++) {
      await page.evaluate(() => window.__GAME_TEST__.answerCorrect());
      await sleep(90);
    }
    await sleep(1750);
    for (let area = 0; area < 3; area++) {
      s = await waitFor(page, x => x.problemKind === 'AreaRatio' && x.frozen === false, 8000);
      if (s.targetRatio === '16:9') { foundReviewCase = true; break; }
      await page.evaluate(() => window.__GAME_TEST__.answerCorrect());
      await sleep(1750);
    }
  }
  if (!foundReviewCase) throw new Error('18개 덱에서 검수 사례 16:9를 찾지 못함');
  const target = s.targetRatio.match(/^(\d+):(\d+)$/);
  if (!target) throw new Error(`목표 비 파싱 실패: ${JSON.stringify(s)}`);
  const p = Number(target[1]), q = Number(target[2]);
  let answer = 0;
  for (let d = 2; d <= 10; d++) if (144 * q === d * d * p) answer = d;
  if (!answer) throw new Error(`넓이비 정수 정답 없음: ${s.targetRatio}`);
  const wrong = 8;
  await dragPuppet(page, wrong);
  await pullCurtain(page);
  s = await waitFor(page, x => x.feedback?.startsWith('선택한 넓이의 비'));
  const actual = ratio(144, wrong * wrong);
  const cmp = 144 * q - wrong * wrong * p;
  const relation = cmp > 0 ? '보다 크다' : '보다 작다';
  const expectedFragment = `현재 넓이의 비 ${actual}는 목표 ${p}:${q}${relation}.`;
  const feedbackCorrect = s.feedback.includes(expectedFragment) && !s.feedback.includes('현재 12:d');
  await page.screenshot({path: path.join(FRAMES, '03-area-feedback.png')});
  await sleep(1700);

  await page.setViewport({width: 2000, height: 800, deviceScaleFactor: 1});
  await sleep(900);
  await page.screenshot({path: path.join(FRAMES, '04-wide-2000.png')});

  // 표지는 생성 원화가 아니라 같은 최종 런타임의 타이틀을 직접 캡처한다.
  const cover = await browser.newPage();
  await cover.setViewport({width: 1200, height: 630, deviceScaleFactor: 1});
  await cover.goto(`${server.url}/g/geurimja-geukjang/`, {waitUntil: 'load', timeout: 45000});
  await cover.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  await sleep(500);
  await cover.screenshot({path: path.join(GAME, 'thumb.png')});
  await cover.setViewport({width: 1080, height: 1080, deviceScaleFactor: 1});
  await sleep(500);
  await cover.screenshot({path: path.join(GAME, 'square.png')});
  await cover.close();

  const result = {
    runId: `geurimja-geukjang-fix1-${new Date().toISOString()}`,
    artifactHashSha256: artifactHash(),
    centerTapMs,
    practice: {railD: 6, guideStepAfterRail: 'curtain', submittedToMain: true},
    areaFeedback: {target: `${p}:${q}`, chosenD: wrong, actual, expectedFragment, observed: s.feedback, passed: feedbackCorrect},
    wideViewport: '2000x800', consoleErrors: errors,
    passed: centerTapMs < 800 && feedbackCorrect && errors.length === 0
  };
  fs.writeFileSync(path.join(HERE, 'fix1-results.json'), JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
