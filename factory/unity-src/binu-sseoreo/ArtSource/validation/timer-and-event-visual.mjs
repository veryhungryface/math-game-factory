#!/usr/bin/env node
// 390×844 실제 포인터 연습 뒤 120·99·9초 HUD와 새 균열길 정답 사건을 캡처한다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../../../../..');
const publicDir = path.join(root, 'public');
const outDir = path.join(here, 'timer-and-event-visual');
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
  throw new Error('Chrome for Testing not found');
}

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const server = await serveStatic(publicDir);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const results = { viewport: '390x844', input: 'real pointer for title and practice', screenshots: [], consoleErrors: [], pageErrors: [] };
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  page.on('console', (m) => { if (m.type() === 'error') results.consoleErrors.push(m.text()); });
  page.on('pageerror', (e) => results.pageErrors.push(String(e)));
  await page.goto(`${server.url}/g/binu-sseoreo/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 30000 });

  await page.mouse.click(195, 610);
  await sleep(700);
  const practiceY = 844 - 844 * (0.18 + 0.46 * 4 / 25);
  await page.mouse.move(390 * .18, practiceY);
  await page.mouse.down();
  await page.mouse.move(390 * .5, practiceY, { steps: 8 });
  await page.mouse.move(390 * .89, practiceY, { steps: 10 });
  await page.mouse.up();
  await sleep(1450);

  for (const target of [
    { label: '120', wait: 0 },
    { label: '99', wait: 21400 },
    { label: '9', wait: 90000 },
  ]) {
    await sleep(target.wait);
    const file = path.join(outDir, `timer-${target.label}.png`);
    await page.screenshot({ path: file });
    results.screenshots.push(file);
  }
  await page.close();

  const eventPage = await browser.newPage();
  await eventPage.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  eventPage.on('console', (m) => { if (m.type() === 'error') results.consoleErrors.push(m.text()); });
  eventPage.on('pageerror', (e) => results.pageErrors.push(String(e)));
  await eventPage.goto(`${server.url}/g/binu-sseoreo/`, { waitUntil: 'load', timeout: 45000 });
  await eventPage.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 30000 });
  await eventPage.evaluate(() => window.__GAME_TEST__.start());
  await sleep(250);
  await eventPage.evaluate(() => window.__GAME_TEST__.answerCorrect());
  await sleep(480);
  const eventFile = path.join(outDir, 'correct-fault-growth.png');
  await eventPage.screenshot({ path: eventFile });
  results.screenshots.push(eventFile);
  results.eventCapture = 'answerCorrect hook used only to inspect the visual event; input.real is verified by official QA';
  await eventPage.close();
} finally {
  await browser.close();
  await server.close();
}

results.passed = results.consoleErrors.length === 0 && results.pageErrors.length === 0;
fs.writeFileSync(path.join(here, 'timer-and-event-visual-results.json'), `${JSON.stringify(results, null, 2)}\n`);
console.log(JSON.stringify(results, null, 2));
if (!results.passed) process.exitCode = 1;
