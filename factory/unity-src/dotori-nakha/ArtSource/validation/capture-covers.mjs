#!/usr/bin/env node
// 표지 재촬영: 완성 런타임의 실제 화면에서 thumb(1200×630)·square(1080×1080)를 찍는다(AI 키아트 대체).
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';
const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const base = path.join(process.env.HOME, '.cache/puppeteer/chrome');
const exe = fs.readdirSync(base).sort().reverse().map((b) => path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing')).find(fs.existsSync);
const srv = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({ executablePath: exe, headless: 'new' });
for (const [name, w, h, mode] of [['thumb', 1200, 630, process.argv[2] || 'title'], ['square', 1080, 1080, process.argv[3] || 'title']]) {
  const page = await browser.newPage();
  await page.setViewport({ width: w, height: h, deviceScaleFactor: 1 });
  await page.goto(`${srv.url}/g/dotori-nakha/index.html`, { waitUntil: 'load' });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });
  await page.evaluate(() => { const b = document.getElementById('mgf-mute'); if (b) b.style.visibility = 'hidden'; });
  await sleep(2200);
  const out = path.join(ROOT, 'public/g/dotori-nakha', name + '.png');
  await page.screenshot({ path: out });
  console.log(out);
  await page.close();
}
await browser.close(); srv.close?.();
