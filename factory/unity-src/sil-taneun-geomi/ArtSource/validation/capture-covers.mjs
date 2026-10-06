import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/sil-taneun-geomi');

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
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

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']
});

const outputs = [
  {file: 'thumb.png', width: 1200, height: 630},
  {file: 'square.png', width: 1080, height: 1080}
];
const records = [];
for (const output of outputs) {
  const page = await browser.newPage();
  const errors = [];
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.setViewport({width: output.width, height: output.height, deviceScaleFactor: 1});
  await page.goto(`${server.url}/g/sil-taneun-geomi/`, {waitUntil: 'load', timeout: 45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  await new Promise(resolve => setTimeout(resolve, 1200));
  await page.screenshot({path: path.join(GAME, output.file), type: 'png'});
  records.push({...output, source: 'final Unity WebGL title render', consoleErrors: errors});
  await page.close();
}

const result = {
  capturedAt: new Date().toISOString(),
  purpose: '표지와 실제 플레이의 재질·카메라·캐릭터 불일치를 없애기 위한 정직한 최종 빌드 캡처',
  outputs: records,
  passed: records.every(record => record.consoleErrors.length === 0)
};
fs.writeFileSync(new URL('./cover-capture.json', import.meta.url), JSON.stringify(result, null, 2));
console.log(JSON.stringify(result, null, 2));
await browser.close();
await server.close();
if (!result.passed) process.exitCode = 1;
