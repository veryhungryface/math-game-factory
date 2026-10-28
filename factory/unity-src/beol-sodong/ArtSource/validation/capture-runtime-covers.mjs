#!/usr/bin/env node
// 최종 WebGL 타이틀의 석회 부조·각진 관절벌·은염 건판 세계를 표지로 직접 캡처한다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../../../../..');
const publicDir = path.join(root, 'public');
const gameDir = path.join(publicDir, 'g', 'beol-sodong');

function chromePath() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome',
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
  throw new Error('Chrome for Testing not found');
}

const server = await serveStatic(publicDir);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--mute-audio', '--use-angle=metal'] });
try {
  for (const target of [
    { width: 1200, height: 630, file: 'thumb.png' },
    { width: 1080, height: 1080, file: 'square.png' },
  ]) {
    const page = await browser.newPage();
    await page.setViewport({ width: target.width, height: target.height, deviceScaleFactor: 1 });
    await page.goto(`${server.url}/g/beol-sodong/`, { waitUntil: 'networkidle2', timeout: 90000 });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });
    await new Promise((resolve) => setTimeout(resolve, 1200));
    await page.screenshot({ path: path.join(gameDir, target.file) });
    await page.close();
  }
  console.log('captured final runtime thumb.png 1200x630 and square.png 1080x1080');
} finally {
  await browser.close();
  await server.close();
}
