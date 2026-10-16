#!/usr/bin/env node
// 1차 수정 회귀: 네 판형 안전 구역, 연습 핀 36px 성공, 짧은 당김 2회 뒤 가이드 재생,
// 오답 공개 카드와 눈금의 분리를 실제 Unity 포인터 입력으로 검증한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/nuntokki-siso');
const OUT = path.join(HERE, 'fix1-regression');
const VIEWPORTS = [
  { name: 'mobile-390', width: 390, height: 844 },
  { name: 'tablet-820', width: 820, height: 1180 },
  { name: 'desktop-1280', width: 1280, height: 800 },
  { name: 'wide-2000', width: 2000, height: 900 },
];
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
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(file => `Build/${file}`)]) hash.update(fs.readFileSync(path.join(GAME, name)));
  return hash.digest('hex');
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, label, timeoutMs = 6000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    const current = await state(page);
    if (predicate(current)) return current;
    await sleep(60);
  }
  throw new Error(`timeout ${label}: ${JSON.stringify(await state(page))}`);
}
function solveK(n) { for (let k = 2; k <= 8; k++) if (k * k * k === n) return k; throw new Error(`not a cube: ${n}`); }
function tickY(control, k) { return control.gaugeBottomY - (control.gaugeBottomY - control.gaugeTopY) * ((k - 2) / 6); }
async function drag(page, x0, y0, x1, y1, steps = 4) {
  await page.mouse.move(x0, y0); await page.mouse.down(); await sleep(30);
  await page.mouse.move(x1, y1, { steps }); await sleep(30); await page.mouse.up(); await sleep(80);
}
async function chooseK(page, k) {
  let control = await state(page); const y = tickY(control, k);
  const detour = Math.max(control.gaugeTopY + 8, Math.min(control.gaugeBottomY - 8, y + 42));
  await page.mouse.move(control.handleX, control.handleY); await page.mouse.down();
  await page.mouse.move(control.handleX, detour, { steps: 2 });
  await page.mouse.move(control.handleX, y, { steps: 3 }); await page.mouse.up(); await sleep(100);
  return state(page);
}
async function pullPin(page, distance) {
  const control = await state(page);
  await drag(page, control.pinX, control.pinY, control.pinX, control.pinY + distance);
  return state(page);
}
async function openPage(browser, server, viewport, errors) {
  const page = await browser.newPage();
  await page.setViewport({ width: viewport.width, height: viewport.height, deviceScaleFactor: 1 });
  page.on('console', message => { if (message.type() === 'error') errors.push(`${viewport.name}: ${message.text()}`); });
  page.on('pageerror', error => errors.push(`${viewport.name}: ${String(error)}`));
  await page.goto(`${server.url}/g/nuntokki-siso/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 90000 });
  return page;
}

fs.mkdirSync(OUT, { recursive: true });
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), protocolTimeout: 900000, args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const consoleErrors = [], layouts = [];
try {
  for (const viewport of VIEWPORTS) {
    const page = await openPage(browser, server, viewport, consoleErrors);
    await page.evaluate(() => window.__GAME_TEST__.start()); await sleep(250);
    const playing = await state(page);
    await page.screenshot({ path: path.join(OUT, `${viewport.name}-playing.png`) });
    const correct = solveK(playing.targetMini), wrong = correct === 8 ? 7 : correct + 1;
    await chooseK(page, wrong); await pullPin(page, 86);
    const reveal = await waitFor(page, s => s.actionPhase === 'reveal', `${viewport.name} reveal`);
    await sleep(250);
    await page.screenshot({ path: path.join(OUT, `${viewport.name}-wrong-reveal.png`) });
    layouts.push({ viewport, playing, reveal });
    await page.close();
  }

  const page = await openPage(browser, server, VIEWPORTS[0], consoleErrors);
  await page.mouse.click(195, 422); await waitFor(page, s => s.actionPhase === 'practice', 'practice start');
  await chooseK(page, 2);
  const afterHandle = await state(page);
  await pullPin(page, 8); const afterShort1 = await state(page);
  await pullPin(page, 8); const afterShort2 = await state(page);
  await sleep(180);
  await page.screenshot({ path: path.join(OUT, 'practice-after-two-short-pulls.png') });
  await pullPin(page, 40);
  const practiceReveal = await waitFor(page, s => s.actionPhase === 'reveal', 'practice 40px reveal');
  const main = await waitFor(page, s => s.actionPhase === 'playing' && !s.onboarding, 'main after practice', 6000);
  await page.screenshot({ path: path.join(OUT, 'practice-entered-main.png') });
  await page.close();

  const bank = JSON.parse(await (await fs.promises.readFile(path.join(GAME, 'meta.json'), 'utf8')));
  const output = {
    generatedAt: new Date().toISOString(), artifactHashSha256: artifactHash(),
    input: 'trusted Puppeteer mouse down/move/up; answer hooks never called',
    viewports: layouts.map(({ viewport, playing, reveal }) => ({
      name: viewport.name, size: `${viewport.width}x${viewport.height}`, layout: playing.layout,
      promptHasEqualArms: playing.prompt.includes('양팔 길이가 같은 시소에서'),
      wrongReveal: reveal.actionPhase === 'reveal',
    })),
    practice: {
      selectedAfterHandle: afterHandle.selectedK,
      guideAfterHandle: afterHandle.guidePhase,
      firstShortStayedPractice: afterShort1.actionPhase === 'practice',
      secondShortStayedPractice: afterShort2.actionPhase === 'practice',
      guideAfterSecondShort: afterShort2.guidePhase,
      fortyPixelPullReachedReveal: practiceReveal.actionPhase === 'reveal',
      enteredMain: main.actionPhase === 'playing' && !main.onboarding,
    },
    metaTitle: bank.title, consoleErrors,
  };
  output.passed = output.viewports.every(row => row.promptHasEqualArms && row.wrongReveal) &&
    output.practice.selectedAfterHandle === 2 && output.practice.guideAfterHandle === 'pull_pin_down' &&
    output.practice.firstShortStayedPractice && output.practice.secondShortStayedPractice &&
    output.practice.guideAfterSecondShort === 'pull_pin_down' && output.practice.fortyPixelPullReachedReveal &&
    output.practice.enteredMain && consoleErrors.length === 0;
  fs.writeFileSync(path.join(HERE, 'fix1-regression-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
  if (!output.passed) process.exitCode = 1;
} finally {
  await browser.close(); await server.close();
}
