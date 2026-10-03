// 카탈로그 thumb(1200×630)·square(1080×1080)를 실제 WebGL 런타임 렌더(실 GPU)에서 캡처한다.
// 생성 이미지와 인게임 재질이 어긋나지 않게 하는 것이 목적이다. 플레이 장면은 실제 pointer 로 연습을 넘긴 첫 본판.
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/cable-lock');
const sleep = ms => new Promise(r => setTimeout(r, ms));
function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const b of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
}
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({headless: true, executablePath: resolveChrome(), args: ['--no-sandbox', '--mute-audio']});
async function shot(w, h, file, mode) {
  const page = await browser.newPage();
  await page.setViewport({width: w, height: h, deviceScaleFactor: 1});
  await page.goto(`${server.url}/g/cable-lock/`, {waitUntil: 'load'});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  if (mode === 'play') {
    await page.evaluate(() => window.__GAME_TEST__.start());
    await sleep(2500);
  } else {
    await sleep(3500);
  }
  await page.screenshot({path: file});
  await page.close();
}
await shot(1200, 630, path.join(GAME, 'thumb.png'), 'play');
await shot(1080, 1080, path.join(GAME, 'square.png'), 'title');
await browser.close();
await server.close();
console.log('ok');
