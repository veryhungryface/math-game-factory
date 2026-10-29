#!/usr/bin/env node
// 최종 Unity 타이틀을 카탈로그 표지로 직접 캡처해 런타임과 형상·재질을 일치시킨다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/jogae-hapchang');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome'
  ]) { const candidate = path.join(base, build, rel); if (fs.existsSync(candidate)) return candidate; }
  throw new Error('Chrome for Testing not found');
}
const sha = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const results = [];
try {
  for (const spec of [{ file: 'thumb.png', width: 1200, height: 630 }, { file: 'square.png', width: 1080, height: 1080 }]) {
    const page = await browser.newPage();
    await page.setViewport({ width: spec.width, height: spec.height, deviceScaleFactor: 1 });
    await page.goto(`${server.url}/g/jogae-hapchang/?cover=${Date.now()}`, { waitUntil: 'domcontentloaded', timeout: 45000 });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
    await sleep(900);
    const target = path.join(GAME, spec.file);
    await page.screenshot({ path: target });
    results.push({ ...spec, sha256: sha(target), source: 'final Unity WebGL title' });
    await page.close();
  }
} finally {
  await browser.close(); await server.close();
}
const output = { generatedAt: new Date().toISOString(), results };
fs.writeFileSync(new URL('./cover-capture-results.json', import.meta.url), `${JSON.stringify(output, null, 2)}\n`);
console.log(JSON.stringify(output, null, 2));
