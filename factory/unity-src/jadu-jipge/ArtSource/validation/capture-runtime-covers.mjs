import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';
import { acquireMachineLock } from '../../../../lib/machine-lock.mjs';

const ROOT = path.resolve(import.meta.dirname, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const OUT = path.join(PUBLIC, 'g/jadu-jipge');

function resolveChrome() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome'
    ]) {
      const candidate = path.join(base, build, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
  return undefined;
}

const unlock = await acquireMachineLock('gpu', { label: 'jadu-jipge 런타임 표지 캡처' });
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-setuid-sandbox', '--use-angle=metal']
});

async function capture(width, height, file) {
  const page = await browser.newPage();
  await page.setViewport({ width, height, deviceScaleFactor: 1 });
  await page.goto(`${server.url}/g/jadu-jipge/`, { waitUntil: 'networkidle0', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 20000 });
  await new Promise(resolve => setTimeout(resolve, 900));
  await page.screenshot({ path: path.join(OUT, file) });
  await page.close();
}

try {
  await capture(1200, 630, 'thumb.png');
  await capture(1080, 1080, 'square.png');
  console.log(JSON.stringify({ thumb: '1200x630', square: '1080x1080', source: 'Unity runtime title' }));
} finally {
  await browser.close();
  await server.close();
  unlock();
}
