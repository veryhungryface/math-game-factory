import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const GAME = path.join(ROOT, 'public/g/ge-jimkkun');

function chromePath() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
  throw new Error('Chrome for Testing not found');
}

const server = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({
  headless: true,
  executablePath: chromePath(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'],
});

const consoleErrors = [];
try {
  for (const spec of [
    { width: 1200, height: 630, file: 'thumb.png' },
    { width: 1080, height: 1080, file: 'square.png' },
  ]) {
    const page = await browser.newPage();
    page.on('console', (m) => { if (m.type() === 'error') consoleErrors.push(m.text()); });
    await page.setViewport({ width: spec.width, height: spec.height, deviceScaleFactor: 1 });
    await page.goto(`${server.url}/g/ge-jimkkun/`, { waitUntil: 'networkidle2', timeout: 45000 });
    await page.waitForFunction('window.__GAME_TEST__?.ready === true', { timeout: 30000 });
    await new Promise((resolve) => setTimeout(resolve, 1200));
    await page.screenshot({ path: path.join(GAME, spec.file) });
    await page.close();
  }
  const result = {
    source: 'final WebGL title scene, no compositing',
    files: ['public/g/ge-jimkkun/thumb.png', 'public/g/ge-jimkkun/square.png'],
    dimensions: ['1200x630', '1080x1080'],
    consoleErrors,
    passed: consoleErrors.length === 0,
  };
  fs.writeFileSync(path.join(HERE, 'runtime-cover-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
