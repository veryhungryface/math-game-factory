#!/usr/bin/env node
// 실제 mouse down/move/up만으로 온보딩, 무실수 완주, 첫 오답 뒤 수리 완주, 두 번째 판을 검증한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/chamsae-chakji');
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
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(file => `Build/${file}`)]) {
    hash.update(name); hash.update(fs.readFileSync(path.join(GAME, name)));
  }
  return hash.digest('hex');
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, label, timeout = 7000) {
  const began = Date.now();
  while (Date.now() - began < timeout) {
    const s = await state(page); if (predicate(s)) return s; await sleep(20);
  }
  throw new Error(`timeout ${label}: ${JSON.stringify(await state(page))}`);
}
function parseAnswer(problem) { return String(problem.answer).split(',').map(v => Number(v.trim())); }
function xFor(s, value, shift) { return s.railLeftPx + (value - shift) / 8 * (s.railRightPx - s.railLeftPx); }
function browserY(unityY) { return H - unityY; }

async function dragValue(page, s, row, fromValue, toValue, shift) {
  const from = [xFor(s, fromValue, shift), browserY(s.rowYPx[row])];
  const to = [xFor(s, toValue, shift), browserY(s.rowYPx[row])];
  await page.mouse.move(from[0], from[1]); await page.mouse.down(); await sleep(36);
  await page.mouse.move(to[0], to[1], { steps: 8 }); await sleep(36); await page.mouse.up();
}

async function submit(page, bankById, answerMode = 'correct') {
  const before = await waitFor(page, s => s.phase === 'playing' && !s.submitted && s.problemId && s.railRightPx > s.railLeftPx, 'problem ready');
  const problem = bankById.get(before.problemId);
  if (!problem) throw new Error(`problem missing from bank: ${before.problemId}`);
  const answer = parseAnswer(problem);
  const shift = Math.floor(Math.min(...answer) / 10) * 10;
  let targets;
  if (answerMode === 'correct') targets = answer;
  else if (answer.length === 1) targets = [shift + ((answer[0] - shift + 1) % 9)];
  else targets = [shift, shift];
  if (before.level === 1) {
    await dragValue(page, before, 3, before.placements[0], targets[0], shift);
  } else {
    await dragValue(page, before, 2, before.placements[0], targets[0], shift);
    const pinned = await waitFor(page, s => (s.handlePlacedMask & 1) === 1 && !s.submitted, 'first perch pinned');
    await dragValue(page, pinned, 3, pinned.placements[1], targets[1], shift);
  }
  return waitFor(page, s => s.submitted || s.phase === 'gameover' || s.phase === 'clear', `submit ${answerMode}`);
}

async function waitNext(page, priorId) {
  return waitFor(page, s => s.phase === 'clear' || s.phase === 'gameover' || (s.phase === 'playing' && !s.submitted && s.problemId !== priorId), 'next problem', 9000);
}

async function finishCorrect(page, bankById, alreadyStarted = false) {
  const attempts = [];
  if (!alreadyStarted) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    await waitFor(page, s => s.phase === 'playing' && s.round === 1 && !s.submitted, 'hook start');
  }
  while (true) {
    const before = await state(page); const after = await submit(page, bankById, 'correct');
    attempts.push({ id: before.problemId, level: before.level, result: after.lastFeedback, solved: after.solved, firstCorrect: after.firstCorrect });
    if (after.phase === 'clear') return { final: after, attempts };
    const next = await waitNext(page, before.problemId); if (next.phase === 'clear') return { final: next, attempts };
  }
}

async function onboardingThenClean(page, bankById) {
  await page.mouse.click(W / 2, H / 2);
  const practice = await waitFor(page, s => s.phase === 'playing' && s.onboarding && s.problemId === 'practice-deviation', 'practice from title');
  await page.screenshot({ path: path.join(HERE, '01-practice-guide.png') });
  await dragValue(page, practice, 3, practice.placements[0], 4, 0);
  const reveal = await waitFor(page, s => s.submitted && s.lastFeedback === 'correct', 'practice correct');
  await page.screenshot({ path: path.join(HERE, '02-practice-reveal.png') });
  await waitFor(page, s => s.phase === 'playing' && !s.onboarding && s.round === 1 && !s.submitted, 'first run');
  const run = await finishCorrect(page, bankById, true);
  return { practice: { start: practice, reveal }, ...run };
}

async function restartAndClean(page, bankById) {
  await page.mouse.click(W / 2, H / 2);
  await waitFor(page, s => s.phase === 'playing' && s.round === 1 && !s.onboarding, 'real restart');
  return finishCorrect(page, bankById, true);
}

async function repairRun(page, bankById) {
  await page.evaluate(() => window.__GAME_TEST__.start());
  const first = await waitFor(page, s => s.phase === 'playing' && s.round === 1 && !s.submitted, 'repair run start');
  const wrong = await submit(page, bankById, 'wrong');
  await page.screenshot({ path: path.join(HERE, '03-wrong-evidence.png') });
  const repair = await waitFor(page, s => s.phase === 'playing' && !s.submitted && s.problemId === first.problemId && s.repairMode, 'repair same problem', 9000);
  const fixed = await submit(page, bankById, 'correct');
  await waitNext(page, first.problemId);
  const rest = await finishCorrect(page, bankById, true);
  return { wrong, repair, fixed, final: rest.final, attempts: rest.attempts };
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), protocolTimeout: 900000, args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const consoleErrors = [];
try {
  const page = await browser.newPage(); await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  page.on('console', m => { if (m.type() === 'error') consoleErrors.push(m.text()); }); page.on('pageerror', e => consoleErrors.push(String(e)));
  await page.goto(`${server.url}/g/chamsae-chakji/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  await page.screenshot({ path: path.join(HERE, '00-title.png') });
  const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(2000));
  const bankById = new Map(bank.map(p => [p.id, p]));
  const clean = await onboardingThenClean(page, bankById); await page.screenshot({ path: path.join(HERE, '04-clean-clear.png') });
  const second = await restartAndClean(page, bankById); await page.screenshot({ path: path.join(HERE, '05-second-clear.png') });
  const repaired = await repairRun(page, bankById); await page.screenshot({ path: path.join(HERE, '06-repair-clear.png') });
  const differentDeck = clean.attempts.map(x => x.id).join('|') !== second.attempts.map(x => x.id).join('|');
  const passed = bank.length === 1092 && bankById.size === 1092 && clean.final.phase === 'clear' && clean.final.solved === 6 && clean.final.firstCorrect === 6 && clean.final.lives === 3 &&
    second.final.phase === 'clear' && second.final.solved === 6 && second.final.firstCorrect === 6 && differentDeck &&
    repaired.wrong.lastFeedback === 'wrong' && repaired.wrong.lives === 2 && repaired.final.phase === 'clear' && repaired.final.solved === 6 && repaired.final.firstCorrect === 5 && repaired.final.lives === 2 && consoleErrors.length === 0;
  const out = { generatedAt: new Date().toISOString(), artifactHashSha256: artifactHash(), viewport: `${W}x${H}`, method: 'Puppeteer real mouse down/move/up on runtime rail geometry; answer hooks are never used for submissions', bankCount: bank.length, clean, second, differentDeck, repaired, consoleErrors, passed };
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(out, null, 2)}\n`);
  console.log(JSON.stringify({ clean: clean.final, second: second.final, differentDeck, repaired: repaired.final, consoleErrors, passed }, null, 2));
  if (!passed) process.exitCode = 1;
  await page.close();
} finally { await browser.close(); await server.close(); }
