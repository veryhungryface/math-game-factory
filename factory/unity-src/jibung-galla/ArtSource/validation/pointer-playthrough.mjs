#!/usr/bin/env node
// 실제 mouse down/move/up만으로 무실수 완주, 첫 오답 뒤 수리 완주, 벨트 0 게임오버를 검증한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/jibung-galla');
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
async function waitFor(page, predicate, label, timeout = 5000) {
  const began = Date.now();
  while (Date.now() - began < timeout) {
    const s = await state(page); if (predicate(s)) return s; await sleep(18);
  }
  throw new Error(`timeout ${label}: ${JSON.stringify(await state(page))}`);
}
function hFrom(s) { const m = String(s.currentProblemId).match(/-h(\d+)/); if (!m) throw new Error(`missing h: ${s.currentProblemId}`); return Number(m[1]); }
function browserPoint(x, unityY) { return [x, H - unityY]; }
function rotate(vx, vy, degrees) {
  const a = degrees * Math.PI / 180, c = Math.cos(a), s = Math.sin(a); return [vx * c - vy * s, vx * s + vy * c];
}

async function submitAngle(page, angle, startOffsetX = 0) {
  const before = await waitFor(page, s => s.phase === 'playing' && s.apexX > 0 && s.bX > 0, 'ready roof');
  const abx = before.bX - before.apexX, aby = before.bY - before.apexY;
  const [dx, dy] = rotate(abx, aby, angle); const scale = 1.38;
  const from = browserPoint(before.apexX + startOffsetX, before.apexY);
  const to = browserPoint(before.apexX + dx * scale, before.apexY + dy * scale);
  await page.mouse.move(from[0], from[1]); await page.mouse.down(); await sleep(42);
  await page.mouse.move(to[0], to[1], { steps: 5 }); await sleep(42); await page.mouse.up();
  return waitFor(page, s => s.phase === 'reveal' || s.phase === 'clear' || s.phase === 'gameover', `submit ${angle}`);
}

async function finishOnboardingWithFormerMiss(page) {
  const began = Date.now();
  await page.mouse.click(W / 2, H / 2);
  const practice = await waitFor(page, s => s.phase === 'playing' && s.onboarding && s.currentProblemId === 'practice-h30' && s.apexX > 0, 'practice from title');
  await page.screenshot({ path: path.join(HERE, 'fix1-onboarding-guide.png') });
  const reveal = await submitAngle(page, 30, 44);
  const firstRun = await waitFor(page, s => s.phase === 'playing' && !s.onboarding && s.round === 1, 'first run after practice');
  await page.screenshot({ path: path.join(HERE, 'fix1-onboarding-first-run.png') });
  return {
    method: 'title center tap, then mouse drag starting 44 px right of A (the same miss outside the old 38 px radius) through the 30° guide beyond BC',
    elapsedMs: Date.now() - began,
    practiceStart: { apexX: practice.apexX, apexY: practice.apexY },
    reveal: { phase: reveal.phase, lastResult: reveal.lastResult, selectedAngle: reveal.selectedAngle },
    firstRun: { phase: firstRun.phase, onboarding: firstRun.onboarding, round: firstRun.round },
    passed: reveal.lastResult === 'correct' && firstRun.round === 1 && Date.now() - began < 30000,
  };
}

async function startRun(page) {
  await page.evaluate(() => window.__GAME_TEST__.start());
  return waitFor(page, s => s.phase === 'playing' && s.apexX > 0, 'start run');
}

async function finishAllCorrect(page) {
  const attempts = [];
  await startRun(page);
  while (true) {
    const before = await state(page); const h = hFrom(before);
    const after = await submitAngle(page, h); attempts.push({ id: before.currentProblemId, input: h, result: after.lastResult, solved: after.solved });
    if (after.phase === 'clear') return { final: after, attempts };
    const next = await waitFor(page, s => s.phase === 'clear' || (s.phase === 'playing' && s.currentProblemId !== before.currentProblemId), 'next roof');
    if (next.phase === 'clear') return { final: next, attempts };
  }
}

async function finishAfterRepair(page) {
  const attempts = [];
  let before = await startRun(page); let h = hFrom(before);
  let after = await submitAngle(page, h === 49 ? h - 5 : h + 5);
  attempts.push({ id: before.currentProblemId, input: after.selectedAngle, result: after.lastResult, lives: after.lives, firstCorrect: after.firstCorrect });
  await waitFor(page, s => s.phase === 'playing' && s.currentProblemId === before.currentProblemId, 'repair same roof');
  after = await submitAngle(page, h); attempts.push({ id: before.currentProblemId, input: h, result: after.lastResult, solved: after.solved });
  while (after.phase !== 'clear') {
    const next = await waitFor(page, s => s.phase === 'clear' || (s.phase === 'playing' && s.currentProblemId !== before.currentProblemId), 'next repaired roof');
    if (next.phase === 'clear') { after = next; break; }
    before = next;
    h = hFrom(before); after = await submitAngle(page, h);
    attempts.push({ id: before.currentProblemId, input: h, result: after.lastResult, solved: after.solved });
  }
  return { final: after, attempts };
}

async function forceGameover(page) {
  const attempts = []; let before = await startRun(page); const h = hFrom(before); let after;
  for (let i = 0; i < 3; i++) {
    after = await submitAngle(page, h + 1); attempts.push({ input: h + 1, lives: after.lives, phase: after.phase });
    if (after.phase === 'gameover') break;
    const next = await waitFor(page, s => s.phase === 'playing' || s.phase === 'gameover', 'retry before gameover');
    if (next.phase === 'gameover') { after = next; break; }
  }
  return { final: after, attempts };
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), protocolTimeout: 900000, args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const consoleErrors = [];
try {
  const page = await browser.newPage(); await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  page.on('console', m => { if (m.type() === 'error') consoleErrors.push(m.text()); }); page.on('pageerror', e => consoleErrors.push(String(e)));
  await page.goto(`${server.url}/g/jibung-galla/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(320));
  const bankAudit = {
    count: bank.length,
    uniqueIds: new Set(bank.map(p => p.id)).size,
    uniquePrompts: new Set(bank.map(p => `${p.prompt}|${p.answer}`)).size,
    correctRelations: bank.every(p => Number.isInteger(p.answerNumeric) && p.answerNumeric >= 25 && p.answerNumeric <= 64 && !/[√]/.test(p.prompt)),
    triangleAndDDefined: bank.every(p => p.prompt.startsWith('AB=AC인 이등변삼각형 ABC에서') && p.prompt.includes('만나는 점을 D라고 할 때') && p.prompt.includes('선 AD')),
    answerMatchesIntegerHalf: bank.every(p => p.answer === `∠BAD=${p.answerNumeric}°인 내부의 선 AD`),
  };
  const onboarding = await finishOnboardingWithFormerMiss(page);
  const clean = await finishAllCorrect(page); await page.screenshot({ path: path.join(HERE, 'pointer-clean-clear.png') });
  const repaired = await finishAfterRepair(page); await page.screenshot({ path: path.join(HERE, 'pointer-repair-clear.png') });
  const gameover = await forceGameover(page); await page.screenshot({ path: path.join(HERE, 'pointer-gameover.png') });
  const passed = bankAudit.count === 320 && bankAudit.uniqueIds === 320 && bankAudit.uniquePrompts === 320 && bankAudit.correctRelations && bankAudit.triangleAndDDefined && bankAudit.answerMatchesIntegerHalf && onboarding.passed &&
    clean.final.phase === 'clear' && clean.final.solved === 10 && clean.final.firstCorrect === 10 && clean.final.lives === 3 &&
    repaired.final.phase === 'clear' && repaired.final.solved === 10 && repaired.final.firstCorrect === 9 && repaired.final.lives === 2 && repaired.final.firstAttemptCount === 10 &&
    gameover.final.phase === 'gameover' && gameover.final.lives === 0 && consoleErrors.length === 0;
  const out = { generatedAt: new Date().toISOString(), artifactHashSha256: artifactHash(), viewport: `${W}x${H}`, method: 'all submissions use Puppeteer mouse down/move/up from the runtime A coordinate through a chosen local integer angle; answer hooks are never called', bankAudit, onboarding, clean, repaired, gameover, consoleErrors, passed };
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(out, null, 2)}\n`);
  console.log(JSON.stringify({ bankAudit, onboarding, cleanFinal: clean.final, repairedFinal: repaired.final, gameoverFinal: gameover.final, consoleErrors, passed }, null, 2));
  if (!passed) process.exitCode = 1;
  await page.close();
} finally { await browser.close(); await server.close(); }
