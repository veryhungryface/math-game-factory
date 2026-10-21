#!/usr/bin/env node
// 3차 수정 회귀: 실제 타이틀 탭, 무효 탭 피드백, 눈금 직접 선택,
// 두 연습 30초 이내 완주, 태블릿 발문 가독성을 최종 artifact에서 확인한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/gereul-kkeonae');
const OUT = path.join(HERE, 'fix3');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome',
  ]) { const candidate = path.join(base, build, rel); if (fs.existsSync(candidate)) return candidate; }
  throw new Error('Chrome for Testing not found');
}

function artifactHash() {
  const hash = crypto.createHash('sha256');
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(file => `Build/${file}`)]) {
    hash.update(name); hash.update(fs.readFileSync(path.join(GAME, name)));
  }
  return hash.digest('hex');
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, label, timeout = 8000) {
  const start = Date.now();
  while (Date.now() - start < timeout) {
    const current = await state(page);
    if (predicate(current)) return current;
    await sleep(24);
  }
  throw new Error(`timeout ${label}: ${JSON.stringify(await state(page))}`);
}

function bp(height, x, unityY) { return [x, height - unityY]; }
function lengthPoint(s, cm, height) {
  const t = (cm - 1) / 23;
  return s.layout === 'land'
    ? bp(height, s.handleX, s.rulerMin + (s.rulerMax - s.rulerMin) * t)
    : bp(height, s.rulerMin + (s.rulerMax - s.rulerMin) * t, s.handleY);
}
async function drag(page, from, to) {
  await page.mouse.move(...from); await page.mouse.down(); await sleep(55);
  await page.mouse.move(...to, { steps: 8 }); await sleep(70);
  await page.mouse.up(); await sleep(70);
}
async function open(page, url, width, height, dpr = 1) {
  await page.setViewport({ width, height, deviceScaleFactor: dpr });
  await page.goto(`${url}/g/gereul-kkeonae/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  await sleep(350);
}
async function directSelect(page, s, answer, height) {
  const target = lengthPoint(s, answer, height);
  await page.mouse.click(...target);
  return waitFor(page, x => x.toolStep === 'cut' && x.drawnLength === answer, `direct-select ${answer}`);
}
async function cutAndConnect(page, s, height) {
  await drag(page, bp(height, s.cutX - 62, s.cutY), bp(height, s.cutX + 62, s.cutY));
  s = await waitFor(page, x => x.toolStep === 'connect', 'cut reaches connect');
  await drag(page, bp(height, s.pieceX, s.pieceY), bp(height, s.gapX, s.gapY));
  return waitFor(page, x => x.phase === 'reveal', 'connect reaches reveal');
}

fs.mkdirSync(OUT, { recursive: true });
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true, executablePath: chromePath(), protocolTimeout: 900000,
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'],
});
const consoleErrors = [];
const result = {
  runId: `gereul-kkeonae-fix3-${new Date().toISOString()}`,
  generatedAt: new Date().toISOString(), artifactHashSha256: artifactHash(),
};
try {
  const page = await browser.newPage();
  page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
  page.on('pageerror', error => consoleErrors.push(String(error)));
  await open(page, server.url, 390, 844);
  const beganAt = Date.now();
  await page.mouse.click(195, 422);
  let s = await waitFor(page, x => x.problemId === 'practice-angle' && x.toolStep === 'pull', 'title to practice one');

  // 수정 전과 같은 종류의 빈 공간 탭은 진도를 주지 않고 리플/매듭/경로 피드백만 낸다.
  const beforeInvalid = { inputCount: s.inputCount, pointerVersion: s.pointerVersion, drawnLength: s.drawnLength };
  await page.mouse.click(370, 400); await sleep(220);
  const afterInvalid = await state(page);
  await page.screenshot({ path: path.join(OUT, '01-invalid-tap-guides-handle.png') });

  // 드래그 시작점을 찾지 않아도 실제 8 cm 눈금을 누르면 같은 판정 경로로 이동한다.
  s = await directSelect(page, afterInvalid, 8, 844);
  await page.screenshot({ path: path.join(OUT, '02-direct-8cm-cut-guide.png') });
  await cutAndConnect(page, s, 844);
  s = await waitFor(page, x => x.problemId === 'practice-bisector' && x.toolStep === 'pull', 'practice two');
  s = await directSelect(page, s, 6, 844);
  await cutAndConnect(page, s, 844);
  const main = await waitFor(page, x => x.phase === 'playing' && x.round === 1 && !x.onboarding, 'main run', 8000);
  const elapsedMs = Date.now() - beganAt;
  await page.screenshot({ path: path.join(OUT, '03-main-under-30s.png') });
  result.onboarding = {
    invalidTapKeptPull: afterInvalid.toolStep === 'pull' && afterInvalid.drawnLength === beforeInvalid.drawnLength,
    invalidTapFeedbackStateChanged: afterInvalid.inputCount > beforeInvalid.inputCount && afterInvalid.pointerVersion > beforeInvalid.pointerVersion,
    directEightSelected: true, secondPracticeCompleted: true, mainRound: main.round, elapsedMs,
  };
  await page.close();

  const tablet = await browser.newPage();
  tablet.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
  tablet.on('pageerror', error => consoleErrors.push(String(error)));
  await open(tablet, server.url, 820, 1180, 2);
  await tablet.evaluate(() => window.__GAME_TEST__.start());
  const tabletState = await waitFor(tablet, x => x.phase === 'playing' && x.round === 1, 'tablet run');
  await tablet.screenshot({ path: path.join(OUT, 'tablet-820x1180.png') });
  const layout = await tablet.evaluate(() => window.__GAME_TEST__.getLayout());
  result.tablet = { hookLand: layout.land, unityLayout: tabletState.layout, screenshot: 'fix3/tablet-820x1180.png' };
  await tablet.close();

  result.consoleErrors = consoleErrors;
  result.passed = result.onboarding.invalidTapKeptPull && result.onboarding.invalidTapFeedbackStateChanged &&
    result.onboarding.mainRound === 1 && elapsedMs < 30000 && !result.tablet.hookLand &&
    result.tablet.unityLayout === 'portrait' && consoleErrors.length === 0;
  fs.writeFileSync(path.join(HERE, 'fix3-regression-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close(); await server.close();
}
