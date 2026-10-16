#!/usr/bin/env node
// 정답 훅 없이 화면의 N을 정수 세제곱으로 해석해 정상 경로와 한 번 실수 뒤 회복 경로를 완주한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/nuntokki-siso');
const OUT = path.join(HERE, 'pointer-playthrough');
const W = 390, H = 844;
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
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(file => `Build/${file}`)]) hash.update(fs.readFileSync(path.join(GAME, name)));
  return hash.digest('hex');
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, label, timeoutMs = 6000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    const current = await state(page); if (predicate(current)) return current; await sleep(70);
  }
  throw new Error(`timeout ${label}: ${JSON.stringify(await state(page))}`);
}
function solveK(n) { for (let k = 2; k <= 8; k++) if (k * k * k === n) return k; throw new Error(`not a cube: ${n}`); }
function tickY(control, k) { return control.gaugeBottomY - (control.gaugeBottomY - control.gaugeTopY) * ((k - 2) / 6); }
async function submit(page, k) {
  let control = await state(page); const y = tickY(control, k);
  const mid = (control.gaugeTopY + control.gaugeBottomY) / 2;
  const detour = Math.max(control.gaugeTopY + 8, Math.min(control.gaugeBottomY - 8, y + (y < mid ? 38 : -38)));
  await page.mouse.move(control.handleX, control.handleY); await page.mouse.down();
  await page.mouse.move(control.handleX, detour, { steps: 2 }); await page.mouse.move(control.handleX, y, { steps: 3 }); await page.mouse.up(); await sleep(30);
  control = await state(page);
  await page.mouse.move(control.pinX, control.pinY); await page.mouse.down();
  await page.mouse.move(control.pinX, control.pinY + 88, { steps: 4 }); await page.mouse.up();
  return waitFor(page, s => s.actionPhase === 'reveal', 'reveal');
}
async function finishRun(page, label, mistakeFirst) {
  await page.evaluate(() => window.__GAME_TEST__.start()); await sleep(70);
  const ids = [], frames = {};
  fs.mkdirSync(OUT, { recursive: true });
  await page.screenshot({ path: path.join(OUT, `${label}-start.png`) });
  if (mistakeFirst) {
    const before = await state(page); const correct = solveK(before.targetMini); const wrong = correct === 8 ? 7 : correct + 1;
    ids.push(before.problemId); await submit(page, wrong);
    frames.afterMistake = await state(page); await page.screenshot({ path: path.join(OUT, `${label}-first-mistake.png`) });
    await waitFor(page, s => s.actionPhase === 'playing' && s.lives === 2, 'retry after one mistake', 5000);
  }
  while (true) {
    const before = await state(page);
    if (before.phase === 'clear' || before.phase === 'not_mastered' || before.phase === 'gameover') break;
    if (before.actionPhase !== 'playing') { await sleep(80); continue; }
    ids.push(before.problemId); await submit(page, solveK(before.targetMini));
    await waitFor(page, s => s.actionPhase !== 'reveal', 'next order or end', 5000);
  }
  const final = await state(page); await page.screenshot({ path: path.join(OUT, `${label}-clear.png`) });
  return { ids, frames, final };
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), protocolTimeout: 900000, args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const consoleErrors = [];
try {
  const page = await browser.newPage(); await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
  page.on('pageerror', error => consoleErrors.push(String(error)));
  await page.goto(`${server.url}/g/nuntokki-siso/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 90000 });
  const bankIds = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(1000).map(p => p.id));
  const normal = await finishRun(page, 'normal', false);
  const recovery = await finishRun(page, 'recovery', true);
  const playedIds = [...normal.ids, ...recovery.ids];
  const output = {
    generatedAt: new Date().toISOString(), artifactHashSha256: artifactHash(), viewport: `${W}x${H}`,
    input: 'trusted Puppeteer mouse down/move/up; targetMini is read as the same visible N and solved by integer k*k*k; answer hooks never called',
    bankCount: bankIds.length, playedProblemIds: playedIds, problemIdsInBank: playedIds.every(id => bankIds.includes(id)),
    normal, recovery, consoleErrors,
  };
  output.passed = normal.final.phase === 'clear' && normal.final.solved === 7 && recovery.frames.afterMistake.lives === 2 && recovery.final.phase === 'clear' && recovery.final.solved === 7 && output.problemIdsInBank && consoleErrors.length === 0;
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
  if (!output.passed) process.exitCode = 1;
  await page.close();
} finally {
  await browser.close(); await server.close();
}
