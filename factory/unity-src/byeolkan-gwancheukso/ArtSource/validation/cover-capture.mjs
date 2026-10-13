import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/byeolkan-gwancheukso');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
  throw new Error('Chrome for Testing not found');
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  for (const shot of [
    { file: 'thumb.png', width: 1200, height: 630 },
    { file: 'square.png', width: 1080, height: 1080 }
  ]) {
    const page = await browser.newPage();
    await page.setViewport({ width: shot.width, height: shot.height, deviceScaleFactor: 1 });
    await page.goto(`${server.url}/g/byeolkan-gwancheukso/`, { waitUntil: 'load', timeout: 45000 });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 30000 });
    await sleep(1800);
    await page.screenshot({ path: path.join(GAME, shot.file) });
    await page.close();
  }
  console.log(JSON.stringify({ thumb: '1200x630', square: '1080x1080', source: 'final Unity WebGL title frame' }, null, 2));
} finally {
  await browser.close();
  await server.close();
}
