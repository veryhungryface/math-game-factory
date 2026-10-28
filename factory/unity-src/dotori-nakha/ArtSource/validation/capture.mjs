#!/usr/bin/env node
// 화면 캡처: 타이틀 → 연습(실제 pointer 드래그) → 실전 → 결과. 390×844 와 1280×800.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const OUT = path.join(HERE, 'shots');
fs.mkdirSync(OUT, { recursive: true });
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
function chromePath() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const c = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(c)) return c;
  }
}
const srv = await serveStatic(path.join(ROOT, 'public'));
const url = `${srv.url}/g/dotori-nakha/index.html`;
const browser = await puppeteer.launch({ executablePath: chromePath(), headless: 'new', protocolTimeout: 120000, args: ['--use-angle=metal'] });
const errors = [];
async function run(w, h, tag) {
  const page = await browser.newPage();
  page.on('console', (m) => { if (m.type() === 'error') errors.push(tag + ': ' + m.text()); });
  page.on('pageerror', (e) => errors.push(tag + ' pageerror: ' + e.message));
  await page.setViewport({ width: w, height: h, deviceScaleFactor: 1 });
  await page.goto(url, { waitUntil: 'load' });
  await page.waitForFunction(() => window.__GAME_TEST__ && window.__GAME_TEST__.ready, { timeout: 30000 });
  await sleep(1500);
  await page.screenshot({ path: path.join(OUT, `${tag}-0-title.png`) });
  await page.mouse.click(w / 2, h * .78);
  await sleep(900);
  await page.screenshot({ path: path.join(OUT, `${tag}-1-practice.png`) });
  const st = await page.evaluate(() => window.__GAME_TEST__.getState());
  const land = st.layoutLand;
  const railY = h * (1 - st.railYPermille / 1000);
  const L = w * st.railLeftPermille / 1000, R = w * st.railRightPermille / 1000;
  const xFor = (v2) => L + (R - L) * (v2 - 2) / 22;
  // drag to 5 g (v2=10) by real pointer
  await page.mouse.move(xFor(3), railY); await page.mouse.down();
  for (let i = 1; i <= 10; i++) { await page.mouse.move(xFor(3) + (xFor(10) - xFor(3)) * i / 10, railY); await sleep(30); }
  await page.screenshot({ path: path.join(OUT, `${tag}-2-dragging.png`) });
  await page.mouse.up();
  await sleep(350);
  await page.screenshot({ path: path.join(OUT, `${tag}-3-catch.png`) });
  await sleep(1600);
  await page.screenshot({ path: path.join(OUT, `${tag}-4-round1.png`) });
  const s2 = await page.evaluate(() => window.__GAME_TEST__.getState());
  // wrong answer by pointer: drag to v2 that is not the answer (use far end)
  await page.mouse.move(xFor(12), railY); await page.mouse.down();
  for (let i = 1; i <= 6; i++) { await page.mouse.move(xFor(12) + 8 * i, railY); await sleep(30); }
  await page.mouse.up();
  await sleep(300);
  await page.screenshot({ path: path.join(OUT, `${tag}-5-after-submit.png`) });
  await sleep(1500);
  await page.evaluate(() => { const t = window.__GAME_TEST__; for (let i = 0; i < 2; i++) t.answerWrong(); });
  await sleep(1500);
  await page.screenshot({ path: path.join(OUT, `${tag}-6-end.png`) });
  console.log(tag, JSON.stringify(s2), JSON.stringify(await page.evaluate(() => window.__GAME_TEST__.getState())));
  await page.close();
}
await run(390, 844, 'p');
await run(1280, 800, 'l');
console.log('errors', errors);
await browser.close(); srv.close?.();
