#!/usr/bin/env node
// 카탈로그 표지는 최종 Unity 타이틀 세계를 정확한 규격으로 직접 캡처한다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../../../../..');
const publicDir = path.join(root, 'public');
const gameDir = path.join(publicDir, 'g/gereul-kkeonae');

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome',
  ]) { const p = path.join(base, build, rel); if (fs.existsSync(p)) return p; }
  throw new Error('Chrome for Testing not found');
}

const server = await serveStatic(publicDir);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  // 카탈로그 표지는 타이틀 세계와 로고에 집중하고, 하단 CTA가 잘린 채 보이지 않도록 안전 영역 위를 캡처한다.
  for (const target of [
    { width: 1200, height: 630, renderHeight: 900, clipY: 0, file: 'thumb.png' },
    { width: 1080, height: 1080, renderHeight: 1400, clipY: 0, file: 'square.png' },
  ]) {
    const page = await browser.newPage();
    await page.setViewport({ width: target.width, height: target.renderHeight, deviceScaleFactor: 1 });
    await page.goto(`${server.url}/g/gereul-kkeonae/`, { waitUntil: 'load', timeout: 45000 });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
    await new Promise(resolve => setTimeout(resolve, 1200));
    await page.screenshot({ path: path.join(gameDir, target.file), clip: { x: 0, y: target.clipY, width: target.width, height: target.height } });
    await page.close();
  }
  console.log('captured runtime thumb.png 1200x630 and square.png 1080x1080');
} finally { await browser.close(); await server.close(); }
