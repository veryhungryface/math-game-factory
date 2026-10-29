#!/usr/bin/env node
// 화면에 표시되는 발문 문자열만 계산해 실제 canvas pointer로 회복·둘째 판·숙달 실패를 검증한다.
// 문제은행, currentProblemId, answerSlot, 정답 필드는 읽지 않는다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/jogae-hapchang');
const HERE = path.dirname(new URL(import.meta.url).pathname);
const SHOTS = path.join(HERE, 'pointer-playthrough');
const VIEW = { width: 390, height: 844 };
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
fs.mkdirSync(SHOTS, { recursive: true });

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome'
  ]) { const candidate = path.join(base, build, rel); if (fs.existsSync(candidate)) return candidate; }
  throw new Error('Chrome for Testing not found');
}
function artifactHash() {
  const hash = crypto.createHash('sha256');
  for (const rel of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(name => `Build/${name}`)]) {
    hash.update(rel); hash.update(fs.readFileSync(path.join(GAME, rel)));
  }
  return hash.digest('hex');
}
function rotate(x, y, degrees) {
  const a = degrees * Math.PI / 180, c = Math.cos(a), s = Math.sin(a);
  return [x * c - y * s, x * s + y * c];
}
function answerFromVisiblePrompt(prompt) {
  let match = prompt.match(/∠AOC=(\d+)°.*,\s*∠COB=(\d+)°/);
  if (match) return (+match[1] + +match[2]) / 2;
  match = prompt.match(/호 AB의 길이:호 CD의 길이=(\d+):(\d+).*∠CPD=([\d.]+)°/);
  if (match) return +match[3] * +match[1] / +match[2];
  match = prompt.match(/중심각(?:을|이)?\s*(\d+)°/);
  if (match) return +match[1] / 2;
  throw new Error(`화면 발문을 계산할 수 없음: ${prompt}`);
}
async function state(page) {
  return page.evaluate(() => {
    const s = window.__GAME_TEST__.getState();
    return { phase: s.phase, mode: s.mode, prompt: s.prompt, solved: s.solved, firstCorrect: s.firstCorrect,
      firstAttempted: s.firstAttempted, lives: s.lives, allowedFirstMistakes: s.allowedFirstMistakes,
      pivotX: s.pivotX, pivotY: s.pivotY, anchorX: s.anchorX, anchorY: s.anchorY, handleX: s.handleX, handleY: s.handleY };
  });
}
async function submitAngle(page, angle) {
  const s = await state(page);
  const bx = s.anchorX - s.pivotX, by = s.anchorY - s.pivotY, baseLength = Math.hypot(bx, by);
  const radius = Math.hypot(s.handleX - s.pivotX, s.handleY - s.pivotY);
  const [rx, ry] = rotate(bx / baseLength * radius, by / baseLength * radius, angle);
  await page.mouse.move(s.handleX, VIEW.height - s.handleY); await page.mouse.down();
  await page.mouse.move(s.pivotX + rx, VIEW.height - (s.pivotY + ry), { steps: 8 }); await page.mouse.up();
}
async function waitMode(page, predicate, timeout = 7000) {
  await page.waitForFunction(predicate, { timeout });
}
async function startWithPractice(page) {
  await page.mouse.click(VIEW.width / 2, VIEW.height / 2);
  await waitMode(page, () => window.__GAME_TEST__.getState().mode === 'practice');
  await submitAngle(page, 40);
  await waitMode(page, () => window.__GAME_TEST__.getState().mode === 'playing');
}
async function passBridge(page, log) {
  const before = await state(page);
  await page.mouse.move(105, 610); await page.mouse.down(); await page.mouse.move(285, 610, { steps: 10 }); await page.mouse.up();
  await waitMode(page, () => window.__GAME_TEST__.getState().mode === 'playing');
  log.push({ action: 'same-arc-P-drag', prompt: before.prompt });
}
async function waitRevealThenPlay(page) {
  await waitMode(page, () => window.__GAME_TEST__.getState().mode === 'reveal');
  await waitMode(page, () => window.__GAME_TEST__.getState().mode !== 'reveal', 7000);
}
async function playClear(page, name, recoverFirst) {
  const log = []; let mistakeDone = false;
  await startWithPractice(page);
  while (true) {
    const s = await state(page);
    if (s.phase === 'clear' || s.phase === 'gameover') break;
    if (s.mode === 'bridge') { await passBridge(page, log); continue; }
    if (s.mode !== 'playing') { await sleep(80); continue; }
    const answer = answerFromVisiblePrompt(s.prompt);
    if (recoverFirst && !mistakeDone) {
      const wrong = answer >= 85 ? answer - 5 : answer + 5;
      await submitAngle(page, wrong); await waitRevealThenPlay(page);
      log.push({ action: 'first-attempt-wrong', visiblePrompt: s.prompt, submittedAngle: wrong });
      mistakeDone = true;
      await submitAngle(page, answer); await waitRevealThenPlay(page);
      log.push({ action: 'repair-same-problem', visiblePrompt: s.prompt, submittedAngle: answer });
    } else {
      await submitAngle(page, answer); await waitRevealThenPlay(page);
      log.push({ action: 'visible-prompt-solve', visiblePrompt: s.prompt, submittedAngle: answer });
    }
  }
  const final = await state(page);
  await page.screenshot({ path: path.join(SHOTS, `${name}-end.png`) });
  return { name, log, final, passed: final.phase === 'clear' && final.solved === 8 && final.firstCorrect === (recoverFirst ? 7 : 8) };
}
async function playMasteryFailure(page) {
  const log = []; let wrongFirstAttempts = 0;
  await page.mouse.click(VIEW.width / 2, VIEW.height / 2);
  await waitMode(page, () => window.__GAME_TEST__.getState().mode === 'practice');
  await submitAngle(page, 40); await waitMode(page, () => window.__GAME_TEST__.getState().mode === 'playing');
  while (wrongFirstAttempts < 3) {
    let s = await state(page);
    if (s.mode === 'bridge') { await passBridge(page, log); continue; }
    const answer = answerFromVisiblePrompt(s.prompt), wrong = answer >= 85 ? answer - 5 : answer + 5;
    await submitAngle(page, wrong); await waitMode(page, () => window.__GAME_TEST__.getState().mode === 'reveal');
    wrongFirstAttempts++;
    const reveal = await state(page);
    log.push({ action: 'first-attempt-wrong', visiblePrompt: s.prompt, submittedAngle: wrong, after: reveal });
    if (wrongFirstAttempts === 3) await page.screenshot({ path: path.join(SHOTS, 'mastery-third-wrong-warning.png') });
    await waitMode(page, () => window.__GAME_TEST__.getState().mode !== 'reveal', 7000);
    s = await state(page);
    if (wrongFirstAttempts < 3) {
      await submitAngle(page, answer); await waitRevealThenPlay(page);
      log.push({ action: 'repair-before-next-first-attempt', submittedAngle: answer });
    }
  }
  const final = await state(page);
  await page.screenshot({ path: path.join(SHOTS, 'mastery-gameover.png') });
  return { name: 'mastery-gameover', log, final, passed: final.phase === 'gameover' && final.allowedFirstMistakes === 0 };
}

const hashBefore = artifactHash();
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const errors = [];
let report;
try {
  const page = await browser.newPage();
  await page.setViewport({ ...VIEW, deviceScaleFactor: 1 });
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/jogae-hapchang/?playthrough=${Date.now()}`, { waitUntil: 'domcontentloaded', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  const recovery = await playClear(page, 'recovery-clear', true);
  await page.mouse.click(VIEW.width / 2, VIEW.height / 2);
  const second = await playClear(page, 'second-clear', false);
  await page.mouse.click(VIEW.width / 2, VIEW.height / 2);
  const masteryFailure = await playMasteryFailure(page);
  report = {
    runId: `jogae-hapchang-pointer-${new Date().toISOString()}`,
    artifactHashSha256: hashBefore,
    method: '화면에 표시되는 prompt 문자열만 읽어 정수 관계를 계산하고 모든 제출·P 이동을 실제 canvas mouse down/move/up으로 수행. 문제은행·problemId·answerSlot·정답 필드 미사용.',
    recovery, second, masteryFailure, consoleErrors: errors,
    artifactHashAfterSha256: artifactHash()
  };
  report.passed = recovery.passed && second.passed && masteryFailure.passed && errors.length === 0 && report.artifactHashSha256 === report.artifactHashAfterSha256;
  await page.close();
} finally { await browser.close(); await server.close(); }
fs.writeFileSync(new URL('./pointer-playthrough-results.json', import.meta.url), `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify(report, null, 2));
if (!report.passed) process.exitCode = 1;
