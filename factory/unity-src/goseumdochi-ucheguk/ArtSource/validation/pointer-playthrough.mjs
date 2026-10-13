#!/usr/bin/env node
// 정상 경로와 한 번 실수한 뒤의 회복 경로를 답 훅 없이 실제 Unity 캔버스 입력으로 완주한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/goseumdochi-ucheguk');
const W = 390, H = 844;
const tens = [70, 507], one = [148, 507], stamp = [130, 601], bell = [291, 601];
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

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

function artifactHash() {
  const hash = crypto.createHash('sha256');
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(file => `Build/${file}`)]) {
    hash.update(fs.readFileSync(path.join(GAME, name)));
  }
  return hash.digest('hex');
}

function cards(count) {
  const spacing = count >= 5 ? 66 : 76;
  return Array.from({ length: count }, (_, i) => [195 + (i - (count - 1) / 2) * spacing, 756]);
}

async function drag(page, from, to) {
  await page.mouse.move(from[0], from[1]);
  await page.mouse.down(); await sleep(32);
  await page.mouse.move(to[0], to[1], { steps: 3 }); await sleep(32);
  await page.mouse.up(); await sleep(42);
}

async function pull(page, from) { await drag(page, from, [from[0], from[1] + 82]); }

async function stampPair(page, count, first, second) {
  const before = (await state(page)).stamps;
  const shelf = cards(count);
  for (let attempt = 0; attempt < 3; attempt++) {
    await drag(page, shelf[first], tens);
    await drag(page, shelf[second], one);
    await pull(page, stamp);
    if ((await state(page)).stamps > before) return;
    await sleep(100);
  }
  throw new Error(`stamp input did not register for ${first},${second}`);
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }

async function waitFor(page, predicate, label, timeoutMs = 6000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    const current = await state(page);
    if (predicate(current)) return current;
    await sleep(80);
  }
  throw new Error(`timeout waiting for ${label}: ${JSON.stringify(await state(page))}`);
}

async function allOrdered(page, count, allowedFirst = [...Array(count).keys()], skip = '') {
  for (const first of allowedFirst) for (let second = 0; second < count; second++) {
    if (first === second || `${first},${second}` === skip) continue;
    await stampPair(page, count, first, second);
  }
}

async function allUnordered(page, count) {
  for (let first = 0; first < count; first++) for (let second = first + 1; second < count; second++) {
    await stampPair(page, count, first, second);
  }
}

async function finishRemainingZones(page) {
  await allOrdered(page, 4, [1, 2, 3]);
  await pull(page, bell);
  await waitFor(page, current => current.zone === 3 && current.substage === 0 && current.phase === 'playing', 'ordered role zone');
  await allOrdered(page, 5);
  await pull(page, bell);
  await waitFor(page, current => current.zone === 3 && current.substage === 1 && current.phase === 'playing', 'unordered role zone');
  await allUnordered(page, 5);
  await pull(page, bell);
  return waitFor(page, current => current.phase === 'clear' || current.phase === 'practice_complete', 'end screen');
}

async function normal(page) {
  await page.evaluate(() => window.__GAME_TEST__.start()); await sleep(100);
  await allOrdered(page, 3);
  await pull(page, bell);
  await waitFor(page, current => current.zone === 2 && current.phase === 'playing', 'zone 2');
  return finishRemainingZones(page);
}

async function oneMistakeRecovery(page) {
  await page.evaluate(() => window.__GAME_TEST__.start()); await sleep(100);
  await stampPair(page, 3, 0, 1);
  await pull(page, bell);
  const afterMistake = await waitFor(page, current => current.zone === 1 && current.phase === 'playing' && current.lives === 2, 'recoverable zone 1');
  await allOrdered(page, 3, [0, 1, 2], '0,1');
  await pull(page, bell);
  await waitFor(page, current => current.zone === 2 && current.phase === 'playing', 'zone 2 after recovery');
  const final = await finishRemainingZones(page);
  return { afterMistake, final };
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), protocolTimeout: 900000, args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const consoleErrors = [];
try {
  const page = await browser.newPage();
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
  page.on('pageerror', error => consoleErrors.push(String(error)));
  await page.goto(`${server.url}/g/goseumdochi-ucheguk/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  const clean = await normal(page);
  const recovery = await oneMistakeRecovery(page);
  const output = {
    generatedAt: new Date().toISOString(), artifactHashSha256: artifactHash(), viewport: `${W}x${H}`,
    input: 'trusted Puppeteer mouse down/move/up; start() only enters a new run; answer hooks are never called',
    normal: clean,
    oneMistakeRecovery: recovery,
    consoleErrors,
    passed: clean.phase === 'clear' && recovery.afterMistake.lives === 2 && recovery.final.phase === 'clear' && consoleErrors.length === 0,
  };
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
  if (!output.passed) process.exitCode = 1;
  await page.close();
} finally {
  await browser.close(); await server.close();
}
