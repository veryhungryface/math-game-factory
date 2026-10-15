#!/usr/bin/env node
// 최종 Unity 타이틀을 그대로 1200×630·1080×1080 표지로 캡처한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/foldline-lab');
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

const sha = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const consoleErrors = [];
try {
  for (const spec of [
    { file: 'thumb.png', width: 1200, height: 630 },
    { file: 'square.png', width: 1080, height: 1080 },
  ]) {
    const page = await browser.newPage();
    page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
    page.on('pageerror', error => consoleErrors.push(String(error)));
    await page.setViewport({ width: spec.width, height: spec.height, deviceScaleFactor: 1 });
    await page.goto(`${server.url}/g/foldline-lab/`, { waitUntil: 'load', timeout: 45000 });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
    await page.evaluate(() => { const mute = document.querySelector('#mgf-mute'); if (mute) mute.style.display = 'none'; });
    await sleep(1400);
    await page.screenshot({ path: path.join(GAME, spec.file), type: 'png' });
    await page.close();
  }
  const output = {
    generatedAt: new Date().toISOString(),
    source: 'final Unity runtime title; generated key art hidden; browser mute chrome hidden only for cover capture',
    files: {
      thumb: { size: '1200x630', sha256: sha(path.join(GAME, 'thumb.png')) },
      square: { size: '1080x1080', sha256: sha(path.join(GAME, 'square.png')) },
    },
    consoleErrors,
    passed: consoleErrors.length === 0,
  };
  fs.writeFileSync(path.join(HERE, 'cover-capture-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
  if (!output.passed) process.exitCode = 1;
} finally {
  await browser.close(); await server.close();
}
