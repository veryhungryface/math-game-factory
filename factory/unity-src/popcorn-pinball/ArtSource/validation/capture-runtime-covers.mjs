#!/usr/bin/env node
// 최종 Unity 타이틀을 그대로 캡처해 런타임과 표지의 실루엣·재질·배치를 일치시킨다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/popcorn-pinball');

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
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

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--mute-audio'] });
try {
  for (const spec of [
    { width: 1200, height: 630, file: 'thumb.png' },
    { width: 1080, height: 1080, file: 'square.png' },
  ]) {
    const page = await browser.newPage();
    await page.setViewport({ width: spec.width, height: spec.height, deviceScaleFactor: 1 });
    await page.goto(`${server.url}/g/popcorn-pinball/?cover=${Date.now()}`, { waitUntil: 'load', timeout: 60000 });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 90000 });
    await new Promise(resolve => setTimeout(resolve, 900));
    await page.screenshot({ path: path.join(GAME, spec.file), fullPage: false });
    await page.close();
    console.log(`${spec.file}: ${spec.width}x${spec.height}`);
  }
} finally {
  await browser.close();
  await server.close();
}
