#!/usr/bin/env node
// 독립 화면 판독 플레이스루.
// 이 하네스는 rawWeights/problem bank/정답 훅을 읽지 않는다. 매 WAIT 뒤 사람이 PNG의 수를 읽어
// 중앙값×2를 stdin에 입력하면, 실제 마우스 드래그로 제출하고 화면 판정만 기록한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import readline from 'node:readline/promises';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const mode = process.argv[2] === 'recovery' ? 'recovery' : 'clean';
const runId = `screen-${mode}-${new Date().toISOString().replace(/[-:.TZ]/g, '').slice(0, 14)}`;
const OUT = path.join(HERE, 'manual-screen-playthrough', runId);
fs.mkdirSync(OUT, { recursive: true });
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const rl = readline.createInterface({ input: process.stdin, output: process.stdout });

function chromePath() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
  throw new Error('Chrome for Testing not found');
}

const srv = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({ executablePath: chromePath(), headless: 'new', protocolTimeout: 180000, args: ['--use-angle=metal'] });
const page = await browser.newPage();
const consoleErrors = [];
page.on('console', (m) => { if (m.type() === 'error') consoleErrors.push(m.text()); });
page.on('pageerror', (e) => consoleErrors.push(`pageerror: ${e.message}`));
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
await page.goto(`${srv.url}/g/dotori-nakha/index.html`, { waitUntil: 'load' });
await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });
await sleep(900);

const state = () => page.evaluate(() => {
  const s = window.__GAME_TEST__.getState();
  // 답 계산에 쓸 수 있는 배열은 의도적으로 반환하지 않는다.
  return {
    phase: s.phase, round: s.round, solved: s.solved, onboarding: s.onboarding,
    activeProblemId: s.activeProblemId, selectedIndex: s.selectedIndex,
    lastResult: s.lastResult, lives: s.lives, firstCorrect: s.firstCorrect,
    attemptedRounds: s.attemptedRounds, repairMode: s.repairMode,
    railLeftPermille: s.railLeftPermille, railRightPermille: s.railRightPermille,
    railYPermille: s.railYPermille
  };
});

async function waitFor(predicate, timeout = 7000) {
  const start = Date.now();
  while (Date.now() - start < timeout) {
    const s = await state();
    if (predicate(s)) return s;
    await sleep(80);
  }
  throw new Error(`wait timeout: ${JSON.stringify(await state())}`);
}

async function dragTo(value2) {
  const s = await state();
  const left = 390 * s.railLeftPermille / 1000;
  const right = 390 * s.railRightPermille / 1000;
  const y = 844 * (1 - s.railYPermille / 1000);
  const x = (v2) => left + (right - left) * (v2 - 2) / 22;
  const from = x(s.selectedIndex + 2), to = x(value2);
  const start = Math.abs(to - from) < 14 ? Math.max(left, Math.min(right, to + (to < 195 ? 36 : -36))) : from;
  await page.mouse.move(start, y); await page.mouse.down();
  for (let i = 1; i <= 14; i++) { await page.mouse.move(start + (to - start) * i / 14, y); await sleep(18); }
  await page.mouse.up();
}

async function passFixedPractice(id, answer2) {
  const before = await state();
  const file = path.join(OUT, `${String(steps.length).padStart(2, '0')}-${id}-screen.png`);
  await page.screenshot({ path: file });
  await dragTo(answer2);
  const judged = await waitFor((s) => s.lastResult !== '' || s.phase !== 'playing');
  steps.push({ kind: 'practice', id, screen: path.basename(file), choice2: answer2, result: judged.lastResult });
  await waitFor((s) => s.lastResult === '' || s.phase !== 'playing');
}

const steps = [];
await page.screenshot({ path: path.join(OUT, '00-title.png') });
await page.mouse.click(195, 658);
await waitFor((s) => s.onboarding && s.activeProblemId === 'practice-odd');
await passFixedPractice('practice-odd', 10);

let injectedMistake = false;
while (true) {
  let s = await state();
  if (s.phase !== 'playing') break;
  if (s.onboarding && s.activeProblemId === 'practice-even') {
    await passFixedPractice('practice-even', 11);
    continue;
  }
  const label = `${mode}-r${s.round}${s.repairMode ? '-repair' : '-first'}`;
  const file = path.join(OUT, `${String(steps.length).padStart(2, '0')}-${label}-screen.png`);
  await page.screenshot({ path: file });
  process.stdout.write(`WAIT ${label} ${file}\n`);
  const typed = (await rl.question('visible median x2> ')).trim();
  const choice2 = Number.parseInt(typed, 10);
  if (!Number.isInteger(choice2) || choice2 < 2 || choice2 > 24) throw new Error(`bad choice2: ${typed}`);
  const before = await state();
  await dragTo(choice2);
  const judged = await waitFor((x) => x.lastResult !== '' || x.phase !== 'playing');
  const resultFile = path.join(OUT, `${String(steps.length).padStart(2, '0')}-${label}-${judged.lastResult || judged.phase}.png`);
  await page.screenshot({ path: resultFile });
  steps.push({
    kind: 'round', round: before.round, attempt: before.repairMode ? 'repair' : 'first',
    screen: path.basename(file), choice2, choice: choice2 / 2,
    result: judged.lastResult || judged.phase, lives: judged.lives,
    firstCorrect: judged.firstCorrect, attemptedRounds: judged.attemptedRounds
  });
  if (mode === 'recovery' && !injectedMistake && judged.lastResult === 'wrong') injectedMistake = true;
  if (judged.phase !== 'playing') break;
  await waitFor((x) => x.lastResult === '' || x.phase !== 'playing');
}

await sleep(900);
const final = await state();
await page.screenshot({ path: path.join(OUT, '99-final.png') });
const artifactNames = fs.readdirSync(path.join(ROOT, 'public/g/dotori-nakha/Build')).sort().join('|');
const result = {
  run_id: runId,
  mode,
  evaluator_input: 'screen_numbers_only',
  answer_source_guard: 'state projection excludes rawWeights; no answerCorrect/answerWrong/sampleProblems calls',
  artifact_id: crypto.createHash('sha256').update(artifactNames).digest('hex'),
  steps,
  final: {
    phase: final.phase, solved: final.solved, firstCorrect: final.firstCorrect,
    attemptedRounds: final.attemptedRounds, lives: final.lives
  },
  spontaneous_retry: mode === 'recovery' ? steps.some((x) => x.attempt === 'repair' && x.result === 'correct') : null,
  consoleErrors
};
fs.writeFileSync(path.join(OUT, 'result.json'), JSON.stringify(result, null, 2));
process.stdout.write(`DONE ${JSON.stringify(result)}\n`);
rl.close();
await browser.close();
srv.close?.();
