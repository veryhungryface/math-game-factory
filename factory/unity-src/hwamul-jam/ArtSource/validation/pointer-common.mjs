import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

export const HERE = path.dirname(fileURLToPath(import.meta.url));
export const ROOT = path.resolve(HERE, '../../../../..');
export const PUBLIC = path.join(ROOT, 'public');
export const GAME = path.join(PUBLIC, 'g/hwamul-jam');
export const W = 390;
export const H = 844;
export const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

export function chromePath() {
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

export function artifactHash() {
  const hash = crypto.createHash('sha256');
  const names = ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(file => `Build/${file}`)];
  for (const name of names) hash.update(fs.readFileSync(path.join(GAME, name)));
  return hash.digest('hex');
}

export async function createSession() {
  const server = await serveStatic(PUBLIC);
  const browser = await puppeteer.launch({
    headless: true,
    executablePath: chromePath(),
    protocolTimeout: 900000,
    args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'],
  });
  const page = await browser.newPage();
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  await page.goto(`${server.url}/g/hwamul-jam/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  return { server, browser, page };
}

export async function state(page) {
  return page.evaluate(() => window.__GAME_TEST__.getState());
}

function parseTargets(text) {
  const result = {};
  for (const part of String(text || '').split('|')) {
    const [name, pair] = part.split(':');
    if (!name || !pair) continue;
    const [x, y] = pair.split(',').map(Number);
    if (Number.isFinite(x) && Number.isFinite(y)) result[name] = [x * W, y * H];
  }
  return result;
}

async function drag(page, from, to, settleMs = 72) {
  await page.mouse.move(from[0], from[1]);
  await page.mouse.down();
  await sleep(20);
  await page.mouse.move((from[0] + to[0]) / 2, (from[1] + to[1]) / 2, { steps: 4 });
  await page.mouse.move(to[0], to[1], { steps: 4 });
  await sleep(24);
  await page.mouse.up();
  await sleep(settleMs);
}

async function geometry(page) {
  const s = await state(page);
  const targets = parseTargets(s.inputTargets);
  const grid = {};
  for (let y = 0; y < 4; y++) for (let x = 0; x < 4; x++) grid[`${x},${y}`] = targets[`g${x}${y}`];
  const positions = {};
  for (let i = 0; i < 8; i++) {
    const point = targets[`c${i}`];
    if (!point) continue;
    let best = null;
    for (const [key, g] of Object.entries(grid)) {
      const d = Math.hypot(point[0] - g[0], point[1] - g[1]);
      if (!best || d < best.d) best = { key, d };
    }
    const [x, y] = best.key.split(',').map(Number);
    positions[i] = { x, y };
  }
  return { state: s, targets, grid, positions };
}

export async function moveCargo(page, index, x, y) {
  const before = await geometry(page);
  const from = before.targets[`c${index}`];
  const to = before.grid[`${x},${y}`];
  if (!from || !to) throw new Error(`missing pointer target c${index} -> g${x}${y}`);
  await drag(page, from, to);
}

/**
 * 후보 index를 수학 정답 정보 없이 실제 격자 조작으로 도크에 넣는다.
 * 진단 훅은 화면 좌표만 제공하며 correctIndex는 노출하지 않는다.
 */
export async function submitCandidate(page, index, retry = 0) {
  let g = await geometry(page);
  if (!g.positions[index]) throw new Error(`candidate ${index} is not active: ${JSON.stringify({ state: g.state, targetKeys: Object.keys(g.targets), positions: g.positions })}`);
  let { x, y } = g.positions[index];

  const occupant = (positions, tx, ty, except = index) => Number(Object.keys(positions).find(key => Number(key) !== except && positions[key].x === tx && positions[key].y === ty));
  const hasOccupant = value => Number.isInteger(value) && value >= 0;

  if (y === 2) {
    const blocker = occupant(g.positions, x, 1);
    if (hasOccupant(blocker)) await moveCargo(page, blocker, x, 0);
    await moveCargo(page, index, x, 1);
    y = 1;
  }

  g = await geometry(page);
  x = g.positions[index].x;
  for (let tx = x + 1; tx <= 3; tx++) {
    g = await geometry(page);
    const blocker = occupant(g.positions, tx, 1);
    if (hasOccupant(blocker)) await moveCargo(page, blocker, tx, 0);
  }
  if (x !== 3) await moveCargo(page, index, 3, 1);

  g = await geometry(page);
  const from = g.targets[`c${index}`];
  if (!from || !g.targets.dock) throw new Error(`missing dock path for c${index}`);
  const beforeAttempts = g.state.firstAttemptTotal;
  const beforeSolved = g.state.solved;
  await drag(page, from, g.targets.dock, 35);
  const started = Date.now();
  while (Date.now() - started < 1800) {
    const after = await state(page);
    // lastResult intentionally persists while an incorrect cargo is quarantined.
    // Only a counter transition proves that this pointer submission registered.
    if (after.firstAttemptTotal > beforeAttempts || after.solved > beforeSolved) return after;
    await sleep(35);
  }
  // A drag that begins on the last feedback frame is intentionally refused by
  // the game. Retry the full physical route after feedback releases input.
  if (retry < 2) {
    await sleep(650);
    return submitCandidate(page, index, retry + 1);
  }
  throw new Error(`submission did not register for c${index}: ${JSON.stringify(await state(page))}`);
}

export async function startRun(page) {
  await page.evaluate(() => window.__GAME_TEST__.start());
  await sleep(180);
  return state(page);
}

export async function waitFor(page, predicate, label, timeoutMs = 5000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    const current = await state(page);
    if (predicate(current)) return current;
    await sleep(55);
  }
  throw new Error(`timeout waiting for ${label}: ${JSON.stringify(await state(page))}`);
}

export async function closeSession(session) {
  await session.page.close();
  await session.browser.close();
  await session.server.close();
}
