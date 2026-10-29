#!/usr/bin/env node
// 3차 수정 회귀: 정답을 읽지 않고 짧은 탭 두 번만으로 연습이 20초 안에 끝나는지 검증한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/jogae-hapchang');
const HERE = path.dirname(new URL(import.meta.url).pathname);
const SHOTS = path.join(HERE, 'fix3-onboarding');
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

async function tap(page, x, y) {
  await page.mouse.move(x, y); await page.mouse.down(); await sleep(80); await page.mouse.up();
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
  await page.goto(`${server.url}/g/jogae-hapchang/?fix3=${Date.now()}`, { waitUntil: 'domcontentloaded', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });

  const startedAt = Date.now();
  await tap(page, VIEW.width / 2, VIEW.height / 2);
  await page.waitForFunction(() => window.__GAME_TEST__.getState().mode === 'practice', { timeout: 5000 });
  const practiceAtMs = Date.now() - startedAt;
  const before = await page.evaluate(() => window.__GAME_TEST__.getState());

  // 기존 첫 플레이가 반복했던 것과 같은 짧은 탭 입력이다. 드래그와 정답 필드는 사용하지 않는다.
  await tap(page, 120, 520);
  await sleep(180);
  const armed = await page.evaluate(() => window.__GAME_TEST__.getState());
  await page.screenshot({ path: path.join(SHOTS, '01-first-tap-armed.png') });

  await tap(page, 290, 390);
  await page.waitForFunction(() => window.__GAME_TEST__.getState().mode === 'reveal', { timeout: 3000 });
  const revealAtMs = Date.now() - startedAt;
  const reveal = await page.evaluate(() => window.__GAME_TEST__.getState());
  await page.screenshot({ path: path.join(SHOTS, '02-second-tap-reveal.png') });
  await page.waitForFunction(() => window.__GAME_TEST__.getState().mode === 'playing', { timeout: 4000 });
  const playingAtMs = Date.now() - startedAt;
  const playing = await page.evaluate(() => window.__GAME_TEST__.getState());
  await page.screenshot({ path: path.join(SHOTS, '03-main-playing.png') });

  report = {
    runId: `jogae-hapchang-fix3-onboarding-${new Date().toISOString()}`,
    artifactHashSha256: hashBefore,
    priorFailure: '같은 짧은 탭 중심 입력으로 최신 firstplay가 연습 시작 뒤 34초 동안 40° 연습에 머물렀다.',
    method: '390×844 실제 canvas mouse down/up. 제목 탭 뒤 연습에서 서로 다른 위치를 짧게 두 번 탭하며 드래그·문제·정답 필드는 사용하지 않는다.',
    timingMs: { practice: practiceAtMs, correctReveal: revealAtMs, mainPlaying: playingAtMs },
    before: { mode: before.mode, selectedAngle: before.selectedAngle, inputRevision: before.inputRevision },
    afterFirstTap: { mode: armed.mode, selectedAngle: armed.selectedAngle, inputRevision: armed.inputRevision },
    afterSecondTap: { mode: reveal.mode, selectedAngle: reveal.selectedAngle, lastResult: reveal.lastResult, inputRevision: reveal.inputRevision },
    final: { mode: playing.mode, phase: playing.phase, practiceDone: playing.practiceDone, solved: playing.solved },
    consoleErrors: errors,
    artifactHashAfterSha256: artifactHash()
  };
  report.passed = report.timingMs.mainPlaying < 20000 && report.afterSecondTap.lastResult === 'correct' &&
    report.final.mode === 'playing' && report.final.practiceDone === true && errors.length === 0 &&
    report.artifactHashSha256 === report.artifactHashAfterSha256;
  await page.close();
} finally {
  await browser.close(); await server.close();
}

fs.writeFileSync(new URL('./fix3-onboarding-results.json', import.meta.url), `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify(report, null, 2));
if (!report.passed) process.exitCode = 1;
