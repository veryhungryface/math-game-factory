// 카탈로그 표지를 실제 Unity 타이틀 장면과 동일하게 유지한다.
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(import.meta.dirname, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/ppittul-daemun');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function resolveChrome() {
  const base = path.join(os.homedir(), '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome'
    ]) {
      const executable = path.join(base, build, rel);
      if (fs.existsSync(executable)) return executable;
    }
  }
  return undefined;
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']
});

for (const [file, width, height] of [
  ['thumb.png', 1200, 630],
  ['square.png', 1080, 1080]
]) {
  const page = await browser.newPage();
  await page.setViewport({width, height, deviceScaleFactor: 1});
  await page.goto(`${server.url}/g/ppittul-daemun/`, {waitUntil: 'load', timeout: 45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, {timeout: 60000});
  await sleep(1600);
  await page.screenshot({path: path.join(GAME, file)});
  await page.close();
}

await browser.close();
await server.close();
console.log('captured runtime title: thumb.png 1200x630, square.png 1080x1080');
