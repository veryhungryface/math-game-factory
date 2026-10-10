// 표지(thumb 1200×630, square 1080×1080)를 실제 런타임 타이틀 화면에서 캡처한다(실 GPU).
//   node factory/unity-src/ttak-matneun-bangul/ArtSource/capture-covers.mjs [outDir]
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../lib/static-server.mjs';
const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../..');
const OUT = process.argv[2] || path.join(ROOT, 'public/g/ttak-matneun-bangul');
const sleep = ms => new Promise(r => setTimeout(r, ms));
function chromePath() {
  const base = path.join(process.env.HOME, '.cache/puppeteer/chrome');
  for (const b of fs.readdirSync(base).sort().reverse()) { const c = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'); if (fs.existsSync(c)) return c; }
}
const srv = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({ executablePath: chromePath(), headless: true });
for (const [W, H, name] of [[1200, 630, 'thumb.png'], [1080, 1080, 'square.png']]) {
  const page = await browser.newPage();
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  await page.goto(srv.url + '/g/ttak-matneun-bangul/index.html?cover=1');
  await page.waitForFunction(() => window.__GAME_TEST__ && window.__GAME_TEST__.ready, { timeout: 30000 });
  await page.addStyleTag({ content: '#mgf-mute{display:none!important}' });
  await sleep(3200);
  await page.screenshot({ path: path.join(OUT, name) });
  console.log('wrote', name);
  await page.close();
}
await browser.close(); srv.close();
