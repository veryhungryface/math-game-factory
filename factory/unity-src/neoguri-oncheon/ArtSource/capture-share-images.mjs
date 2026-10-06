import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../lib/static-server.mjs';
import { acquireMachineLock } from '../../lib/machine-lock.mjs';
// 공유 이미지를 실제 게임 화면(실 GPU 캡처)으로 만든다 — 카탈로그 얼굴과 플레이가 같은 물건이 되게.
const root = new URL('../../../public/', import.meta.url).pathname;
const outDir = new URL('./shots3/', import.meta.url).pathname;
function chromePath() {
  const base = path.join(process.env.HOME, '.cache/puppeteer/chrome');
  for (const b of fs.readdirSync(base).sort().reverse()) {
    const e = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(e)) return e;
  }
}
const wait = (ms) => new Promise((r) => setTimeout(r, ms));
const { url, close } = await serveStatic(root);
const release = await acquireMachineLock('gpu', { label: 'neoguri thumbs' });
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), protocolTimeout: 120000, args: ['--no-sandbox', '--mute-audio', '--use-gl=angle'] });
try {
  for (const [w, h, name] of [[1200, 630, 'cap-thumb'], [1080, 1080, 'cap-square']]) {
    const page = await browser.newPage();
    await page.setViewport({ width: w, height: h, deviceScaleFactor: 1 });
    await page.goto(`${url}/g/neoguri-oncheon/`, { waitUntil: 'networkidle2', timeout: 45000 });
    await page.waitForFunction('window.__GAME_TEST__?.ready === true', { timeout: 30000 });
    await page.addStyleTag({ content: 'button,.mute,#mute,[class*=mute]{display:none!important}' });
    await wait(2500);
    await page.screenshot({ path: `${outDir}${name}-title.png` });
    // 실제 플레이 순간: 첫 문항 정답(실제 판정 경로) 직후 물길이 열리고 탕이 솟는 장면
    await page.evaluate(() => { window.__GAME_TEST__.start(); });
    await wait(900);
    await page.evaluate(() => { window.__GAME_TEST__.answerCorrect(); });
    await wait(700);
    await page.screenshot({ path: `${outDir}${name}-play.png` });
    await page.close();
  }
} finally { await browser.close(); await release(); await close(); }
