#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

export const HERE = path.dirname(fileURLToPath(import.meta.url));
export const ROOT = path.resolve(HERE, '../../../../..');
export const PUBLIC = path.join(ROOT, 'public');
export const GAME = path.join(PUBLIC, 'g/saeu-isa');
export const W = 390, H = 844;
export const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome',
  ]) { const candidate = path.join(base, build, rel); if (fs.existsSync(candidate)) return candidate; }
  throw new Error('Chrome for Testing not found');
}

export function artifactHash() {
  const hash = crypto.createHash('sha256');
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(file => `Build/${file}`)]) {
    hash.update(name); hash.update(fs.readFileSync(path.join(GAME, name)));
  }
  return hash.digest('hex');
}

export async function createSession() {
  const server = await serveStatic(PUBLIC);
  const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), protocolTimeout: 900000,
    args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
  const page = await browser.newPage();
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  await page.goto(`${server.url}/g/saeu-isa/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  return { server, browser, page };
}

export async function closeSession(session) {
  await session.browser.close(); await session.server.close();
}

export async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }

export async function waitFor(page, predicate, label, timeout = 10000) {
  const began = Date.now();
  while (Date.now() - began < timeout) {
    const s = await state(page); if (predicate(s)) return s; await sleep(20);
  }
  throw new Error(`timeout ${label}: ${JSON.stringify(await state(page))}`);
}

export async function startRun(page) {
  await page.evaluate(() => window.__GAME_TEST__.start());
  const ready = await waitFor(page, s => s.phase === 'playing' && s.currentCorrectClass >= 0 && s.sourceX > 0, 'run ready');
  // C# SendMessage는 동기지만 WebGL Input은 다음 rAF부터 새 장면의 hitbox를 안정적으로 샘플링한다.
  await sleep(55);
  return ready;
}

export async function dragCurrent(page, classIndex) {
  const before = await waitFor(page, s => s.phase === 'playing' && s.currentCorrectClass >= 0 && s.sourceX > 0, 'draggable sample');
  const x = before.classX[classIndex], y = before.targetY;
  // 2.5D 카메라에서 root 원점보다 리넨 앞면의 시각 중심이 CSS y 기준 약 14px 아래다.
  // 화면에 실제 보이는 48px+ 주머니 몸통 중앙을 누른다.
  const sourceY = before.sourceY + (before.currentOriginClass >= 0 ? 4 : 14);
  await page.mouse.move(before.sourceX, sourceY); await page.mouse.down(); await sleep(45);
  await page.mouse.move((before.sourceX + x) / 2, (sourceY + y) / 2, { steps: 4 });
  await sleep(45); await page.mouse.move(x, y, { steps: 4 }); await sleep(45); await page.mouse.up();
  await sleep(90);
  return { before, after: await state(page) };
}
