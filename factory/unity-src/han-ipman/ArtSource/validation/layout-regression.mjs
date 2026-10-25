#!/usr/bin/env node
// 2차 수정 회귀: 실제 CanvasScaler 논리 사각형에서 4개 플레이 뷰포트와 누적 아치 사건을 보존한다.
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../../../../..');
const outDir = path.join(here, 'fix2-layout');
const resultFile = path.join(here, 'layout-regression-results.json');
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
fs.mkdirSync(outDir, { recursive: true });

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
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
  throw new Error('Chrome for Testing not found');
}

function sha256(file) {
  return crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
}

const server = await serveStatic(path.join(root, 'public'));
const browser = await puppeteer.launch({
  headless: true,
  executablePath: chromePath(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio', '--hide-scrollbars'],
});
const captures = [];
const browserErrors = [];

async function shot(page, file, label, viewport) {
  const target = path.join(outDir, file);
  await page.screenshot({ path: target });
  captures.push({ label, viewport, file: path.relative(root, target), sha256: sha256(target) });
}

try {
  for (const viewport of [
    { name: 'tablet-820x1180', width: 820, height: 1180 },
    { name: 'desktop-1280x800', width: 1280, height: 800 },
    { name: 'desktop-1440x900', width: 1440, height: 900 },
    { name: 'wide-2000x1045', width: 2000, height: 1045 },
  ]) {
    const page = await browser.newPage();
    page.on('console', (message) => { if (message.type() === 'error') browserErrors.push(`${viewport.name}: ${message.text()}`); });
    page.on('pageerror', (error) => browserErrors.push(`${viewport.name}: ${String(error)}`));
    await page.setViewport({ width: viewport.width, height: viewport.height, deviceScaleFactor: 1 });
    await page.goto(`${server.url}/g/han-ipman/`, { waitUntil: 'networkidle2', timeout: 45000 });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 25000 });
    await page.evaluate(() => window.__GAME_TEST__.start());
    await sleep(700);
    await shot(page, `${viewport.name}-play.png`, 'playing layout', viewport);

    if (viewport.width === 1280) {
      await page.evaluate(() => window.__GAME_TEST__.answerCorrect());
      await sleep(260);
      await shot(page, `${viewport.name}-correct-event.png`, 'same Submit path correct event', viewport);
      await sleep(1050);
      await shot(page, `${viewport.name}-arch-persist.png`, 'solved angle tile persists', viewport);
    }
    await page.close();
  }

  if (browserErrors.length) throw new Error(JSON.stringify(browserErrors));
  const qa = JSON.parse(fs.readFileSync(path.join(root, 'factory/work/qa/han-ipman/report.json'), 'utf8'));
  const result = {
    passed: true,
    artifactHash: qa.artifact_hash,
    inputPath: 'window.__GAME_TEST__.start/answerCorrect; correct hook calls the same Submit(qAnswer) method as pointer input',
    captures,
    browserErrors,
  };
  fs.writeFileSync(resultFile, `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
} finally {
  await browser.close().catch(() => {});
  await server.close().catch(() => {});
}
