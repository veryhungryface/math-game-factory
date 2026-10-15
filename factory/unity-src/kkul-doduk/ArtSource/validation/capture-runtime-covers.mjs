import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/kkul-doduk');

function chrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome'
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
  throw new Error('Chrome for Testing을 찾을 수 없다.');
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: chrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']
});

async function capture(width, height, name) {
  const page = await browser.newPage();
  const errors = [];
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.setViewport({ width, height, deviceScaleFactor: 1 });
  await page.goto(`${server.url}/g/kkul-doduk/?cover=${width}x${height}`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  await new Promise(resolve => setTimeout(resolve, 900));
  await page.screenshot({ path: path.join(GAME, name) });
  await page.close();
  if (errors.length) throw new Error(`${name}: ${errors.join(' | ')}`);
}

try {
  await capture(1200, 630, 'thumb.png');
  await capture(1080, 1080, 'square.png');
  console.log(JSON.stringify({ thumb: '1200x630', square: '1080x1080', source: 'Unity runtime title, GPU Chrome' }, null, 2));
} finally {
  await browser.close();
  await server.close();
}
