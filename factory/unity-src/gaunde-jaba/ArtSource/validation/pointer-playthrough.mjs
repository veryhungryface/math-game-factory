#!/usr/bin/env node
// 화면에 공개된 수들을 직접 계산하고 실제 pointer drag만으로 연습→오답 복구→클리어한다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../../../../..');
const outDir = path.join(here, 'pointer-playthrough');
const out = path.join(here, 'pointer-playthrough-results.json');
fs.mkdirSync(outDir, { recursive: true });

function chromePath() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome',
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
  throw new Error('Chrome for Testing을 찾을 수 없습니다.');
}

function median2(values) {
  const sorted = [...values].sort((a, b) => a - b);
  return sorted.length % 2
    ? 2 * sorted[Math.floor(sorted.length / 2)]
    : sorted[sorted.length / 2 - 1] + sorted[sorted.length / 2];
}

const server = await serveStatic(path.join(root, 'public'));
const browser = await puppeteer.launch({
  headless: true,
  executablePath: chromePath(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'],
});
const consoleErrors = [];
let report;
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
  page.on('console', (msg) => { if (msg.type() === 'error') consoleErrors.push(msg.text()); });
  page.on('pageerror', (err) => consoleErrors.push(String(err)));
  await page.goto(`${server.url}/g/gaunde-jaba/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });
  await page.screenshot({ path: path.join(outDir, '00-title.png') });
  await page.mouse.click(195, 422);
  await page.waitForFunction(() => window.__GAME_TEST__.getState().activeProblemId === 'practice-odd', { timeout: 10000 });

  const state = () => page.evaluate(() => window.__GAME_TEST__.getState());
  async function dragValue(value2) {
    const s = await state();
    const left = 390 * s.railLeftPermille / 1000;
    const right = 390 * s.railRightPermille / 1000;
    const y = 844 * (1 - s.railYPermille / 1000);
    const xFor = (v2) => left + (right - left) * (v2 - 2) / 22;
    await page.mouse.move(xFor(s.netValue2), y);
    await page.mouse.down();
    await page.mouse.move(xFor(value2), y, { steps: 12 });
    await page.mouse.up();
  }
  async function solveCurrent() {
    const before = await state();
    await dragValue(median2(before.rawValues));
    return before;
  }
  async function waitForProblemChange(id, solved) {
    await page.waitForFunction((oldId, oldSolved) => {
      const s = window.__GAME_TEST__.getState();
      return s.phase === 'clear' || s.phase === 'gameover' || s.activeProblemId !== oldId || s.solved !== oldSolved;
    }, { timeout: 6000 }, id, solved);
  }

  let s = await solveCurrent();
  await waitForProblemChange(s.activeProblemId, s.solved);
  await page.screenshot({ path: path.join(outDir, '01-main.png') });

  s = await state();
  const correct2 = median2(s.rawValues);
  const wrong2 = correct2 === 24 ? 23 : correct2 + 1;
  await dragValue(wrong2);
  await page.waitForFunction(() => window.__GAME_TEST__.getState().lives === 2, { timeout: 3000 });
  await page.screenshot({ path: path.join(outDir, '02-wrong-evidence.png') });
  await page.waitForFunction((id) => {
    const x = window.__GAME_TEST__.getState();
    return x.activeProblemId === id && x.lastResult === '' && x.phase === 'playing';
  }, { timeout: 5000 }, s.activeProblemId);
  await dragValue(correct2);
  await waitForProblemChange(s.activeProblemId, s.solved);

  while (true) {
    s = await state();
    if (s.phase === 'clear' || s.phase === 'gameover') break;
    const oldId = s.activeProblemId;
    const oldSolved = s.solved;
    await solveCurrent();
    await waitForProblemChange(oldId, oldSolved);
  }
  const final = await state();
  await page.screenshot({ path: path.join(outDir, '03-clear.png') });
  report = {
    passed: final.phase === 'clear' && final.solved === 6 && final.lives === 2 && final.firstCorrect === 5 && consoleErrors.length === 0,
    path: 'title pointer → odd practice → one wrong → repair → even practice → clear',
    final,
    consoleErrors,
  };
  await page.close();
} finally {
  await browser.close();
  await server.close();
}
fs.writeFileSync(out, `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify(report, null, 2));
if (!report.passed) process.exitCode = 1;
