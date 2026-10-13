import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/byeolkan-gwancheukso');
const FRAMES = path.join(HERE, 'pointer-playthrough');
const VIEW = { width: 390, height: 844, deviceScaleFactor: 1 };
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
fs.mkdirSync(FRAMES, { recursive: true });

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'
  ]) { const candidate = path.join(base, build, rel); if (fs.existsSync(candidate)) return candidate; }
  throw new Error('Chrome for Testing not found');
}

function buildHash() {
  const hash = crypto.createHash('sha256');
  for (const rel of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(name => `Build/${name}`)]) {
    hash.update(rel); hash.update(fs.readFileSync(path.join(GAME, rel)));
  }
  return hash.digest('hex');
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitState(page, predicate, label, timeout = 7000) {
  const until = Date.now() + timeout; let s;
  while (Date.now() < until) { s = await state(page); if (predicate(s)) return s; await sleep(70); }
  throw new Error(`${label}: ${JSON.stringify(s)}`);
}
async function tap(page, x, y) {
  await page.mouse.move(x, y); await page.mouse.down(); await sleep(28); await page.mouse.up(); await sleep(40);
}
async function drag(page, from, to) {
  await page.mouse.move(...from); await page.mouse.down(); await sleep(25);
  await page.mouse.move(...to, { steps: 8 }); await sleep(35); await page.mouse.up(); await sleep(45);
}
async function crank(page) {
  const c = [335, 701], r = 31;
  await page.mouse.move(c[0] + r, c[1]); await page.mouse.down(); await sleep(25);
  for (let i = 1; i <= 18; i++) {
    const a = i / 18 * Math.PI * 2;
    await page.mouse.move(c[0] + Math.cos(a) * r, c[1] + Math.sin(a) * r);
    await sleep(18);
  }
  await page.mouse.up(); await sleep(45);
}

const rackHome = [[86, 701], [222, 701]];
const crossDock = [195, 430];
const endDocks = [[70, 430], [320, 430]];

async function placeTopology(page, topology, sources = rackHome) {
  const targets = topology === 'cross' ? [crossDock, crossDock] : endDocks;
  await drag(page, sources[0], targets[0]);
  await drag(page, sources[1], targets[1]);
}

function cellPoint(rows, cols, index) {
  const board = { x: 195, y: 430, w: 350, h: 338 };
  const mx = 34, my = 34;
  const cw = (board.w - mx * 2) / cols, ch = (board.h - my * 2) / rows;
  const x = index % cols, y = Math.floor(index / cols);
  return [board.x + (x - (cols - 1) / 2) * cw, board.y - ((rows - 1) / 2 - y) * ch];
}
async function mark(page, rows, cols, indices) {
  for (const index of indices) {
    const p = cellPoint(rows, cols, index); await tap(page, p[0], p[1]);
    const after = await state(page);
    if (!after.maskIds.split(',').includes(String(index))) throw new Error(`cell ${index} missed at ${p.map(Math.round)}; state=${JSON.stringify(after)}`);
  }
}

async function finishPractice(page, log, tag) {
  const before = await state(page);
  await placeTopology(page, 'cross'); await crank(page);
  const after = await waitState(page, s => !s.onboarding && s.problemId !== 'practice-2x3', `${tag} practice did not finish`);
  log.push({ action: `${tag}-practice-cross-crank`, before, after });
  return after;
}

async function waitAdvance(page, before, label) {
  const feedback = await waitState(page, s => s.solved > before.solved || s.lives < before.lives || s.phase === 'clear', `${label} no verdict`);
  if (feedback.lives < before.lives) return feedback;
  if (feedback.phase === 'clear') return feedback;
  return waitState(page, s => s.problemId !== before.problemId || s.phase === 'clear', `${label} no advance`);
}

async function solveMission(page, index, log) {
  const before = await state(page);
  if (before.problemId === 'mission-union') await placeTopology(page, 'end');
  if (before.problemId === 'mission-product') await placeTopology(page, 'cross');
  if (index === 2) { await placeTopology(page, 'cross'); await page.screenshot({ path: path.join(FRAMES, '03b-dice-grid.png') }); await mark(page, 6, 6, [3, 8, 13, 18]); }
  if (index === 3) { await placeTopology(page, 'cross'); await mark(page, 4, 4, [0, 1, 2, 3, 5, 10, 15]); }
  if (index === 4) { await placeTopology(page, 'cross'); await mark(page, 4, 4, [0, 1, 2, 3, 5, 10, 15]); }
  if (index === 5) {
    await placeTopology(page, 'cross'); await mark(page, 5, 5, [0, 6, 12, 18, 24]);
    await drag(page, [355, 430], [260, 430]);
  }
  const configured = await state(page);
  const expectedTopology = before.problemId === 'mission-union' ? 'endjoined+endjoined' : 'crossed+crossed';
  if (configured.rackTopology !== expectedTopology) throw new Error(`mission ${index + 1} topology setup failed: ${JSON.stringify({ before, configured, expectedTopology })}`);
  await crank(page);
  const after = await waitAdvance(page, before, `mission ${index + 1}`);
  log.push({ action: `mission-${index + 1}`, problemId: before.problemId, before, after });
  return after;
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
let result;
try {
  const page = await browser.newPage(); await page.setViewport(VIEW);
  const errors = []; page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); }); page.on('pageerror', e => errors.push(String(e)));
  await page.goto(`${server.url}/g/byeolkan-gwancheukso/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 30000 });
  const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(9999));
  await page.evaluate(() => {
    window.__BYEOLKAN_POINTER_AUDIT__ = [];
    const canvas = document.querySelector('canvas');
    for (const type of ['pointerdown', 'pointerup']) canvas.addEventListener(type, e => window.__BYEOLKAN_POINTER_AUDIT__.push({ type, trusted: e.isTrusted, x: Math.round(e.clientX), y: Math.round(e.clientY) }), true);
  });
  const log = [];
  await page.screenshot({ path: path.join(FRAMES, '00-title.png') });
  await tap(page, 36, 790);
  const invalidTitle = await state(page);
  await page.screenshot({ path: path.join(FRAMES, '01-title-refusal.png') });
  const titleStart = Date.now(); await tap(page, 195, 616);
  await waitState(page, s => s.onboarding, 'title handle did not enter practice');
  const titleStartMs = Date.now() - titleStart;
  await page.screenshot({ path: path.join(FRAMES, '02-practice-guide.png') });
  await finishPractice(page, log, 'perfect');
  await page.screenshot({ path: path.join(FRAMES, '03-first-mission.png') });
  for (let i = 0; i < 6; i++) await solveMission(page, i, log);
  const perfect = await waitState(page, s => s.phase === 'clear', 'perfect run did not clear');
  await page.screenshot({ path: path.join(FRAMES, '04-perfect-clear.png') });

  await tap(page, 195, 582); await waitState(page, s => s.onboarding, 'restart did not begin practice');
  await finishPractice(page, log, 'recovery');
  const wrongBefore = await state(page);
  const wrongTopology = wrongBefore.problemId === 'mission-union' ? 'cross' : 'end';
  const correctTopology = wrongTopology === 'cross' ? 'end' : 'cross';
  await placeTopology(page, wrongTopology);
  const wrongConfigured = await state(page);
  if (wrongTopology === 'end' && wrongConfigured.rackTopology !== 'endjoined+endjoined') throw new Error(`end topology pointer setup failed: ${JSON.stringify({ wrongBefore, wrongConfigured })}`);
  if (wrongTopology === 'cross' && wrongConfigured.rackTopology !== 'crossed+crossed') throw new Error(`cross topology pointer setup failed: ${JSON.stringify({ wrongBefore, wrongConfigured })}`);
  await crank(page);
  const wrong = await waitState(page, s => s.lives === 2, 'wrong topology did not cost a seal');
  await page.screenshot({ path: path.join(FRAMES, '05-wrong-evidence.png') });
  await waitState(page, s => s.lives === 2 && s.problemId === wrongBefore.problemId && s.misconceptionId === 'sum-product-swapped', 'wrong board did not return');
  await sleep(1250);
  await placeTopology(page, correctTopology); await crank(page);
  await waitState(page, s => s.solved > wrongBefore.solved, 'repaired first mission no success');
  await waitState(page, s => s.problemId !== wrongBefore.problemId, 'repaired first mission no advance');
  for (let i = 1; i < 6; i++) await solveMission(page, i, log);
  const recovery = await waitState(page, s => s.phase === 'clear', 'recovery run did not clear');
  await page.screenshot({ path: path.join(FRAMES, '06-recovery-clear.png') });

  const audit = await page.evaluate(() => window.__BYEOLKAN_POINTER_AUDIT__);
  const checks = {
    bankAtLeast300: bank.length >= 300,
    invalidTitleTapRefused: invalidTitle.phase === 'title' && invalidTitle.pointerVersion > 0,
    titleStartsWithin10Seconds: titleStartMs < 10000,
    everyPointerEventTrusted: audit.length > 0 && audit.every(e => e.trusted),
    perfectClear: perfect.phase === 'clear' && perfect.solved === 6 && perfect.lives === 3 && perfect.firstAttemptCorrect === 6,
    recoveryClear: recovery.phase === 'clear' && recovery.solved === 6 && recovery.lives === 2 && recovery.firstAttemptCorrect === 5,
    noConsoleErrors: errors.length === 0
  };
  result = { generated_at: new Date().toISOString(), build_sha256: buildHash(), evidence: 'trusted browser pointer full playthrough', bank_count: bank.length,
    title_start_ms: titleStartMs, perfect, recovery, pointer_audit: { count: audit.length, trusted: audit.filter(e => e.trusted).length }, errors, log, checks,
    result: Object.values(checks).every(Boolean) ? 'pass' : 'fail' };
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify({ build_sha256: result.build_sha256, bank_count: result.bank_count, title_start_ms: titleStartMs, perfect, recovery,
    pointer_audit: result.pointer_audit, checks, result: result.result }, null, 2));
  if (result.result !== 'pass') process.exitCode = 1;
} finally { await browser.close(); await server.close(); }
