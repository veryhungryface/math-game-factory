#!/usr/bin/env node
// 카탈로그 표지를 최종 WebGL 타이틀의 실제 프레임으로만 만든다.
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../../../../..');
const publicDir = path.join(root, 'public');
const gameDir = path.join(publicDir, 'g', 'han-ipman');
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome',
    ]) {
      const candidate = path.join(base, build, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
  throw new Error('Chrome for Testing not found');
}

function hash(file) {
  return crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
}

const server = await serveStatic(publicDir);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const captures = [];
try {
  for (const target of [
    { file: 'thumb.png', width: 1200, height: 630 },
    { file: 'square.png', width: 1080, height: 1080 },
  ]) {
    const page = await browser.newPage();
    const errors = [];
    page.on('console', (message) => { if (message.type() === 'error') errors.push(message.text()); });
    page.on('pageerror', (error) => errors.push(String(error)));
    await page.setViewport({ width: target.width, height: target.height, deviceScaleFactor: 1 });
    await page.goto(`${server.url}/g/han-ipman/`, { waitUntil: 'networkidle2', timeout: 45000 });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 25000 });
    await sleep(900);
    const file = path.join(gameDir, target.file);
    await page.screenshot({ path: file });
    if (errors.length) throw new Error(`${target.file} 브라우저 오류: ${JSON.stringify(errors)}`);
    captures.push({ file: path.relative(root, file), width: target.width, height: target.height, sha256: hash(file), source: 'final WebGL title frame' });
    await page.close();
  }
  const result = { passed: true, captures };
  fs.writeFileSync(path.join(here, 'cover-capture-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
} finally {
  await browser.close();
  await server.close();
}
