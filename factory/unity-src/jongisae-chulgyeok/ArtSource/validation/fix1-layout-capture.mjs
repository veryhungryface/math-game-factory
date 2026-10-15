#!/usr/bin/env node
// 1차 수정 증거: 1280/1440/2000 극장형 플레이 배치를 실제 WebGL에서 캡처한다.
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';
import { acquireMachineLock } from '../../../../lib/machine-lock.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const WORK = path.resolve(ROOT, process.env.MGF_WORK || 'factory/work');
const OUT = path.join(WORK, 'qa/jongisae-chulgyeok/layout-final');
fs.mkdirSync(OUT, { recursive: true });

function chromePath() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const b of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
}

const unlock = await acquireMachineLock('gpu', { label: 'jongisae fix1 layout' });
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const rows = [];
try {
  for (const width of [1280, 1440, 2000]) {
    const page = await browser.newPage();
    const errors = [];
    page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
    page.on('pageerror', e => errors.push(String(e)));
    await page.setViewport({ width, height: 800, deviceScaleFactor: 1 });
    await page.goto(`${server.url}/g/jongisae-chulgyeok/`, { waitUntil: 'load', timeout: 45000 });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
    await page.evaluate(() => window.__GAME_TEST__.start());
    await page.waitForFunction(() => window.__GAME_TEST__.getState().phase === 'playing', { timeout: 10000 });
    await new Promise(resolve => setTimeout(resolve, 700));
    const state = await page.evaluate(() => ({ state: window.__GAME_TEST__.getState(), layout: window.__GAME_TEST__.getLayout() }));
    const screenshot = path.join(OUT, `${width}x800-playing.png`);
    await page.screenshot({ path: screenshot });
    rows.push({ width, screenshot: path.relative(ROOT, screenshot), phase: state.state.phase, layout: state.layout, errors });
    await page.close();
  }
} finally {
  await browser.close();
  await server.close();
  await unlock();
}
const result = { generatedAt: new Date().toISOString(), rows, pass: rows.every(r => r.phase === 'playing' && r.layout.land && r.layout.playW === r.width && r.errors.length === 0) };
fs.writeFileSync(path.join(OUT, 'results.json'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
if (!result.pass) process.exitCode = 1;
