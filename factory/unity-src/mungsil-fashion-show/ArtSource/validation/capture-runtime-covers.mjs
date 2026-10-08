#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const publicDir = path.resolve(here, '../../../../../public');
const gameDir = path.join(publicDir, 'g', 'mungsil-fashion-show');

function chromePath() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome'
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
  return undefined;
}

const { url, close } = await serveStatic(publicDir);
const browser = await puppeteer.launch({ executablePath: chromePath(), headless: true, args: ['--no-sandbox'] });
const captured = [];
for (const [file, width, height] of [['thumb.png', 1200, 630], ['square.png', 1080, 1080]]) {
  const page = await browser.newPage();
  await page.setViewport({ width, height, deviceScaleFactor: 1 });
  await page.goto(`${url}/g/mungsil-fashion-show/`, { waitUntil: 'networkidle0', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 20000 });
  await new Promise(resolve => setTimeout(resolve, 900));
  await page.screenshot({ path: path.join(gameDir, file) });
  captured.push({ file, width, height, source: 'live WebGL title' });
  await page.close();
}
await browser.close();
await close();
fs.writeFileSync(path.join(here, 'runtime-covers.json'), JSON.stringify({ capturedAt: new Date().toISOString(), captured }, null, 2) + '\n');
console.log(JSON.stringify(captured));
