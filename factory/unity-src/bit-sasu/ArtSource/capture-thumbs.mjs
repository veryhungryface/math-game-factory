// 허브 썸네일은 생성 회화가 아니라 실제 타이틀 장면(실시간 광학 데크) 캡처로 만든다.
import fs from 'node:fs'; import os from 'node:os'; import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../lib/static-server.mjs';
const ROOT = path.resolve(import.meta.dirname, '../../../..');
const base = path.join(os.homedir(), '.cache/puppeteer/chrome'); let exe;
for (const b of fs.readdirSync(base).sort().reverse()) { const f = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'); if (fs.existsSync(f)) { exe = f; break; } }
const server = await serveStatic(path.join(ROOT, 'public'));
const br = await puppeteer.launch({ headless: true, executablePath: exe, args: ['--no-sandbox', '--use-angle=metal', '--mute-audio'] });
for (const [w, h, name] of [[1200, 630, 'thumb.png'], [1080, 1080, 'square.png']]) {
  const page = await br.newPage();
  await page.setViewport({ width: w, height: h, deviceScaleFactor: 1 });
  await page.goto(`${server.url}/g/bit-sasu/`);
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });
  await new Promise(r => setTimeout(r, 3500));
  await page.screenshot({ path: path.join(ROOT, 'public/g/bit-sasu', name) });
  await page.close();
}
await br.close(); process.exit(0);
