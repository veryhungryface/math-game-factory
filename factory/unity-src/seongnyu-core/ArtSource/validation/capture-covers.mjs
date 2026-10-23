import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/seongnyu-core');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  const root = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(root)) return undefined;
  for (const version of fs.readdirSync(root).sort().reverse()) {
    const binary = path.join(root, version, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(binary)) return binary;
  }
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: chromePath(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']
});

for (const [name, width, height] of [['thumb.png', 1200, 630], ['square.png', 1080, 1080]]) {
  const page = await browser.newPage();
  await page.setViewport({width, height, deviceScaleFactor: 1});
  await page.goto(`${server.url}/g/seongnyu-core/`, {waitUntil: 'load', timeout: 45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  await sleep(900);
  await page.screenshot({path: path.join(GAME, name), type: 'png'});
  await page.close();
}

await browser.close();
await server.close();
console.log('captured runtime covers: thumb.png 1200x630, square.png 1080x1080');
