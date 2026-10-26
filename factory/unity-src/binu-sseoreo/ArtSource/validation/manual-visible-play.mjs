#!/usr/bin/env node
// 화면 캡처의 발문만 사람이 읽어 계산하는 포인터 검증 드라이버.
// 금지: activeProblemId, sampleProblems, answerCorrect/answerWrong/start 훅.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import readline from 'node:readline/promises';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const HERE = path.dirname(new URL(import.meta.url).pathname);
const OUT = path.join(HERE, 'manual-visible-play');
const RESULT = path.join(HERE, 'manual-visible-play-results.json');
const W = 390, H = 844;
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
fs.mkdirSync(OUT, { recursive: true });

function chromePath() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const version of fs.readdirSync(base).sort().reverse()) {
    for (const rel of ['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing', 'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing', 'chrome-linux64/chrome']) {
      const candidate = path.join(base, version, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
}

function artifactHash() {
  const dir = path.join(PUBLIC, 'g/binu-sseoreo');
  const rels = ['index.html', ...fs.readdirSync(path.join(dir, 'Build')).sort().map((name) => `Build/${name}`)];
  const hash = crypto.createHash('sha256');
  for (const rel of rels) hash.update(rel).update(fs.readFileSync(path.join(dir, rel)));
  return hash.digest('hex');
}

function browserY(height) {
  const unityY = H * (0.18 + 0.46 * height / 25);
  return H - unityY;
}

async function slice(page, height) {
  const y = browserY(height);
  await page.mouse.move(W * 0.18, y);
  await page.mouse.down();
  await page.mouse.move(W * 0.50, y, { steps: 8 });
  await page.mouse.move(W * 0.89, y, { steps: 10 });
  await page.mouse.up();
}

// 허용 상태만 복사한다. 문제 id·도형 치수·정답은 반환하지 않는다.
async function publicProgress(page) {
  return page.evaluate(() => {
    const { phase, round, solved, lives, firstAttemptCorrect, firstAttemptTotal, onboarding, attemptCount, lastResult } = window.__GAME_TEST__.getState();
    return { phase, round, solved, lives, firstAttemptCorrect, firstAttemptTotal, onboarding, attemptCount, lastResult };
  });
}

const rl = readline.createInterface({ input: process.stdin, output: process.stdout });
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const page = await browser.newPage();
await page.setViewport({ width: W, height: H, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
const consoleErrors = [], pageErrors = [], calculations = [];
page.on('console', (m) => { if (m.type() === 'error') consoleErrors.push(m.text()); });
page.on('pageerror', (e) => pageErrors.push(String(e)));
await page.goto(`${server.url}/g/binu-sseoreo/`, { waitUntil: 'load', timeout: 45000 });
await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 60000 });

async function shot(name) {
  const file = path.join(OUT, name);
  await page.screenshot({ path: file });
  process.stdout.write(`SCREENSHOT ${file}\n`);
  return file;
}

async function enterRun(label) {
  await page.mouse.click(W / 2, H * .72);
  await sleep(700);
  await shot(`${label}-practice.png`);
  await slice(page, 4);
  await sleep(1550);
  const p = await publicProgress(page);
  if (p.onboarding || p.round !== 1) throw new Error(`practice did not enter run: ${JSON.stringify(p)}`);
}

async function playRun(label, requireMistake) {
  await enterRun(label);
  let mistakeSeen = false, serial = 0;
  while (true) {
    const before = await publicProgress(page);
    if (before.phase === 'clear' || before.phase === 'gameover') break;
    const file = await shot(`${label}-${String(serial).padStart(2, '0')}-round${before.round}-before.png`);
    const raw = await rl.question(`HEIGHT_JSON ${label} round=${before.round} screenshot=${file}\n`);
    const cmd = JSON.parse(raw);
    if (!Number.isInteger(cmd.height) || cmd.height < 1 || cmd.height > 24 || !cmd.rationale) throw new Error(`bad manual command: ${raw}`);
    await slice(page, cmd.height);
    await sleep(1550);
    const after = await publicProgress(page);
    calculations.push({ run: label, round: before.round, selectedHeight: cmd.height, rationale: cmd.rationale, before, after });
    if (after.lastResult === 'wrong') mistakeSeen = true;
    await shot(`${label}-${String(serial).padStart(2, '0')}-round${before.round}-after-${after.lastResult}.png`);
    serial++;
  }
  const end = await publicProgress(page);
  if (end.phase !== 'clear' || end.solved !== 8) throw new Error(`${label} did not clear: ${JSON.stringify(end)}`);
  if (requireMistake && !mistakeSeen) throw new Error(`${label} required one visible-prompt mistake and recovery`);
  if (!requireMistake && mistakeSeen) throw new Error(`${label} required a clean run`);
  await shot(`${label}-clear.png`);
  return end;
}

await shot('00-title.png');
const recovered = await playRun('run1-recovery', true);
const clean = await playRun('run2-clean', false);
const result = {
  run_id: `binu-visible-${new Date().toISOString()}`,
  artifact_hash: artifactHash(),
  method: 'A human evaluator read each captured Unity canvas prompt, entered a written calculation rationale and selected height, and used only real Chrome mouse pointer paths.',
  forbidden_calls: { activeProblemId: 0, sampleProblems: 0, answerCorrect: 0, answerWrong: 0, start: 0 },
  recovered, clean, calculations, consoleErrors, pageErrors,
  passed: recovered.phase === 'clear' && clean.phase === 'clear' && consoleErrors.length === 0 && pageErrors.length === 0,
};
fs.writeFileSync(RESULT, `${JSON.stringify(result, null, 2)}\n`);
fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(result, null, 2)}\n`);
process.stdout.write(`${JSON.stringify(result, null, 2)}\n`);
await rl.close(); await page.close(); await browser.close(); await server.close();
if (!result.passed) process.exitCode = 1;
