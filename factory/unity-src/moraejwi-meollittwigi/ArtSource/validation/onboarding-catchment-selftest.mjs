#!/usr/bin/env node
// 수정 전에는 거부되던 유령 경로 중점을 같은 입력으로 잡아 30초 안에 연습을 벗어나는지 확인한다.
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/moraejwi-meollittwigi');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

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

function artifactHash() {
  const hash = crypto.createHash('sha256');
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(file => `Build/${file}`)]) {
    hash.update(name);
    hash.update(fs.readFileSync(path.join(GAME, name)));
  }
  return hash.digest('hex');
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, timeout = 7000) {
  const end = Date.now() + timeout; let s;
  while (Date.now() < end) { s = await state(page); if (predicate(s)) return s; await sleep(70); }
  throw new Error(`state timeout: ${JSON.stringify(s)}`);
}

function gridPoint(s, a, b) {
  const [x0, y0, x1, y1, x2, y2] = s.gridPx;
  return [x0 + (x1 - x0) * a + (x2 - x0) * b, y0 + (y1 - y0) * a + (y2 - y0) * b];
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
  await page.goto(`${server.url}/g/moraejwi-meollittwigi/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 30000 });
  const startedAt = Date.now();
  await page.mouse.click(195, 766);
  await waitFor(page, s => s.onboarding && s.frozen);
  // 타이틀→플레이 카메라 보간이 끝난 현재 격자 좌표를 사용한다.
  await sleep(550);
  const practice = await state(page);
  const origin = gridPoint(practice, 0, 0), target = gridPoint(practice, 3, 4);
  const midpoint = [(origin[0] + target[0]) / 2, (origin[1] + target[1]) / 2];
  await page.mouse.move(midpoint[0], midpoint[1]);
  await sleep(70);
  await page.mouse.down();
  await page.mouse.move(target[0], target[1], { steps: 10 });
  // Unity가 마지막 move 프레임을 소비한 뒤 pointer-up을 받도록 한 프레임 이상 둔다.
  await sleep(70);
  await page.mouse.up();
  const after = await waitFor(page, s => !s.onboarding && s.round === 1, 7000);
  const elapsedMs = Date.now() - startedAt;
  const result = {
    generated_at: new Date().toISOString(),
    run_id: process.env.MGF_RUN_ID || null,
    artifact_hash: artifactHash(),
    regression_input: 'start at exact midpoint of visible practice guide, drag to (3,4)',
    start_px: midpoint.map(Math.round),
    target_px: target.map(Math.round),
    elapsed_ms: elapsedMs,
    final_state: after,
    passed: elapsedMs < 30000 && after.round === 1 && after.lastAction === 'run-start',
  };
  fs.writeFileSync(path.join(HERE, 'onboarding-catchment-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
