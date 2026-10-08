#!/usr/bin/env node
// 3차 수정 회귀: 세 번의 실패는 시범만 보여 주고, 실제 성공 sweep만 실전으로 넘긴다.
// 훅은 상태 읽기에만 사용하며 모든 조작은 브라우저 pointer 이벤트로 보낸다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const root = path.resolve(process.cwd());
const out = path.join(root, 'factory/unity-src/ssak-geonjyeo/ArtSource/validation/practice-gate-regression');
fs.mkdirSync(out, { recursive: true });
const server = await serveStatic(path.join(root, 'public'), 0);
const executablePath = process.env.PUPPETEER_EXECUTABLE_PATH
  || path.join(process.env.HOME, '.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
const browser = await puppeteer.launch({ headless: true, executablePath, args: ['--no-sandbox', '--use-angle=metal'] });
const page = await browser.newPage();
const consoleErrors = [];
page.on('console', (message) => { if (message.type() === 'error') consoleErrors.push(message.text()); });
page.on('pageerror', (error) => consoleErrors.push(String(error)));

const width = 390, height = 844;
await page.setViewport({ width, height, deviceScaleFactor: 2 });
await page.goto(`${server.url}/g/ssak-geonjyeo/`, { waitUntil: 'networkidle2', timeout: 45000 });
await page.waitForFunction('window.__GAME_TEST__?.ready === true', { timeout: 30000 });
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const state = () => page.evaluate(() => window.__GAME_TEST__.getState());
const toCss = (s, x, y) => [x * width / s.screenW, y * height / s.screenH];
async function drag(points, steps = 8) {
  await page.mouse.move(points[0][0], points[0][1]);
  await page.mouse.down();
  await sleep(70);
  for (let i = 1; i < points.length; i++) {
    await page.mouse.move(points[i][0], points[i][1], { steps });
    await sleep(45);
  }
  await page.mouse.up();
  await sleep(420);
}
async function waitFor(predicate, timeout = 6000) {
  const start = Date.now();
  while (Date.now() - start < timeout) {
    const current = await state();
    if (predicate(current)) return current;
    await sleep(120);
  }
  return state();
}

await page.mouse.click(width / 2, height / 2);
let current = await waitFor((s) => s.onboarding === true);
const [capacityX, capacityY] = toCss(current, current.controlPx[4], current.controlPx[5]);
await drag([[capacityX, capacityY], [capacityX + 30, capacityY], [capacityX + 60, capacityY]]);
current = await state();
const afterCapacity = current;

const failedStates = [];
for (let i = 0; i < 3; i++) {
  const [sweepX, sweepY] = toCss(current, current.controlPx[6], current.controlPx[7]);
  await drag([[sweepX, sweepY], [sweepX + 4, sweepY - 4]], 2);
  current = await state();
  failedStates.push(current);
}
await page.screenshot({ path: path.join(out, 'after-three-failures.png') });
const afterThreeFailures = await state();
await sleep(6200);
const afterDemoExpired = await state();
await page.screenshot({ path: path.join(out, 'after-demo-expired.png') });

// 시범 뒤에도 같은 실제 연습판에 남아 있어야 하며, 학생이 두 정답 게를 직접 지나 놓는다.
const active = [];
for (let i = 0; i < afterDemoExpired.crabPx.length; i += 4) {
  const back = afterDemoExpired.crabPx[i], claw = afterDemoExpired.crabPx[i + 1];
  if (back !== 1 || (claw !== 1 && claw !== 2)) continue;
  const [x, y] = toCss(afterDemoExpired, afterDemoExpired.crabPx[i + 2], afterDemoExpired.crabPx[i + 3]);
  active.push({ claw, x, y });
}
active.sort((a, b) => b.x - a.x);
const [sweepX, sweepY] = toCss(afterDemoExpired, afterDemoExpired.controlPx[6], afterDemoExpired.controlPx[7]);
const pathPoints = [[sweepX, sweepY], [width - 7, sweepY - 100], [width - 7, active[0].y - 45], ...active.map((p) => [p.x, p.y])];
await drag(pathPoints, 10);
const afterActualSuccess = await waitFor((s) => s.onboarding === false && s.tide === 1, 7000);
await page.screenshot({ path: path.join(out, 'after-actual-success.png') });

const buildHash = crypto.createHash('sha256');
for (const file of fs.readdirSync(path.join(root, 'public/g/ssak-geonjyeo/Build')).sort())
  buildHash.update(fs.readFileSync(path.join(root, 'public/g/ssak-geonjyeo/Build', file)));

const stayedInPractice = afterThreeFailures.onboarding === true
  && afterThreeFailures.tide === 0
  && afterThreeFailures.solved === 0
  && afterThreeFailures.score === 0
  && afterThreeFailures.capacity === 2
  && afterThreeFailures.selectedIds.length === 0
  && afterDemoExpired.onboarding === true
  && afterDemoExpired.tide === 0
  && afterDemoExpired.solved === 0;
const actualSuccessAdvanced = afterActualSuccess.onboarding === false
  && afterActualSuccess.tide === 1
  && afterActualSuccess.solved === 0;
const result = {
  slug: 'ssak-geonjyeo',
  viewport: '390x844@2',
  build_hash: buildHash.digest('hex'),
  hooks_used: ['getState (read only)'],
  afterCapacity,
  failedStates,
  afterThreeFailures,
  afterDemoExpired,
  afterActualSuccess,
  stayedInPractice,
  actualSuccessAdvanced,
  consoleErrors,
  ok: stayedInPractice && actualSuccessAdvanced && consoleErrors.length === 0,
};
fs.writeFileSync(path.join(out, 'result.json'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify({ stayedInPractice, actualSuccessAdvanced, afterThreeFailures, afterActualSuccess, consoleErrors, ok: result.ok }, null, 2));
await browser.close();
await server.close();
if (!result.ok) process.exitCode = 1;
