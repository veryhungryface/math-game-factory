#!/usr/bin/env node
// 완성 런타임에서 thumb.png(1200×630)·square.png(1080×1080)를 캡처한다(?cover=1 → 타이틀 UI 크롬 숨김, 음소거 버튼 숨김).
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';
const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const GAME = path.join(ROOT, 'public/g/han-kkeut-golf');
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
function chromePath() { const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome'); for (const b of fs.readdirSync(base).sort().reverse()) { const c = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'); if (fs.existsSync(c)) return c; } }
const srv = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({ executablePath: chromePath(), headless: 'new' });
for (const [name, w, h] of [['thumb', 1200, 630], ['square', 1080, 1080]]) {
  const page = await browser.newPage();
  await page.setViewport({ width: w, height: h, deviceScaleFactor: 1 });
  await page.goto(`${srv.url}/g/han-kkeut-golf/index.html?cover=1`, { waitUntil: 'load' });
  await page.waitForFunction(() => window.__GAME_TEST__ && window.__GAME_TEST__.ready, { timeout: 30000 });
  await page.addStyleTag({ content: '#mgf-mute{display:none!important}' });
  await sleep(2600);
  await page.screenshot({ path: path.join(GAME, `${name}.png`) });
  await page.close();
}
await browser.close(); process.exit(0);
