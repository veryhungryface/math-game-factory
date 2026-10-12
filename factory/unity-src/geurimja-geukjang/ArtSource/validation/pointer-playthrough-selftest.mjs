#!/usr/bin/env node
// 공개 발문을 독립 정수 풀이하고 실제 pointer로 정상 1판과 오답 복구 1판을 완주한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/geurimja-geukjang');
const FRAMES = path.join(HERE, 'pointer-playthrough');
fs.mkdirSync(FRAMES, {recursive: true});
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
  throw new Error('Chrome for Testing not found');
}

function artifactHash() {
  const files = [];
  (function walk(dir) { for (const name of fs.readdirSync(dir).sort()) { const p = path.join(dir, name); if (fs.statSync(p).isDirectory()) walk(p); else files.push(p); } })(GAME);
  const hash = crypto.createHash('sha256');
  for (const file of files) { hash.update(path.relative(GAME, file)); hash.update(fs.readFileSync(file)); }
  return hash.digest('hex');
}

function solve(prompt) {
  const area = prompt.match(/넓이의 비(?:가|를) (\d+):(\d+)(?:가 되도록|로 맞추도록)/);
  if (area) {
    const p = Number(area[1]), q = Number(area[2]);
    for (let d = 2; d <= 10; d++) if (144 * q === d * d * p) return d;
  }
  const reverse = prompt.match(/인형의 길이는 (\d+) cm이고 그림자의 길이는 (\d+) cm/);
  if (reverse) {
    const h = Number(reverse[1]), s = Number(reverse[2]);
    if ((12 * h) % s === 0) return 12 * h / s;
  }
  const length = prompt.match(/길이의 비(?:가|를) (\d+):(\d+)(?:가 되도록|로 맞추도록)/);
  if (length) {
    const p = Number(length[1]), q = Number(length[2]);
    if ((12 * q) % p === 0) return 12 * q / p;
  }
  throw new Error(`공개 발문 정수 풀이 실패: ${prompt}`);
}

const xFor = d => 390 * (.12 + (d - 2) * .70 / 8);
async function dragPuppet(page, d) {
  const adjacent = d === 2 ? 3 : d - 1;
  await page.mouse.move(xFor(adjacent), 608); await page.mouse.down(); await sleep(70);
  await page.mouse.move(xFor(d), 608, {steps: 4}); await sleep(70); await page.mouse.up(); await sleep(80);
}
async function pullCurtain(page) {
  await page.mouse.move(343, 397); await page.mouse.down(); await sleep(70);
  await page.mouse.move(343, 540, {steps: 4}); await sleep(70); await page.mouse.up(); await sleep(100);
}
async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, timeout = 5000) {
  const end = Date.now() + timeout; let s;
  while (Date.now() < end) { s = await state(page); if (predicate(s)) return s; await sleep(70); }
  throw new Error(`상태 대기 초과: ${JSON.stringify(s)}`);
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']});
const errors = [], log = [];
try {
  const page = await browser.newPage();
  await page.setViewport({width: 390, height: 844, deviceScaleFactor: 1});
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/geurimja-geukjang/`, {waitUntil: 'load', timeout: 45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  await page.screenshot({path: path.join(FRAMES, '00-title.png')});

  async function playRun(run, injectWrong) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    let wrongDone = false;
    while (true) {
      let s = await state(page);
      if (s.phase === 'clear' || s.phase === 'gameover') return s;
      const before = {problemId: s.problemId, solved: s.solved, lives: s.lives, firstAttemptCorrect: s.firstAttemptCorrect};
      const answer = solve(s.prompt);
      if (injectWrong && !wrongDone) {
        const wrong = answer === 2 ? 3 : answer - 1;
        await dragPuppet(page, wrong); await pullCurtain(page); wrongDone = true;
        s = await waitFor(page, x => x.lives === before.lives - 1);
        log.push({run, type: 'wrong', prompt: s.prompt, chosenD: wrong, correctD: answer, before, after: {lives: s.lives, firstAttemptCorrect: s.firstAttemptCorrect}});
        await page.screenshot({path: path.join(FRAMES, `0${run}-wrong.png`)});
        await sleep(1500);
        continue;
      }
      await dragPuppet(page, answer); await pullCurtain(page);
      s = await waitFor(page, x => x.solved === before.solved + 1 || x.phase === 'clear');
      log.push({run, type: 'correct', prompt: s.prompt, chosenD: answer, before, after: {solved: s.solved, lives: s.lives, firstAttemptCorrect: s.firstAttemptCorrect}});
      await sleep(1750);
    }
  }

  const run1 = await playRun(1, false);
  await page.screenshot({path: path.join(FRAMES, '01-run1-clear.png')});
  const run2 = await playRun(2, true);
  await page.screenshot({path: path.join(FRAMES, '03-run2-recovered-clear.png')});
  const result = {
    runId: `geurimja-geukjang-pointer-playthrough-${new Date().toISOString()}`,
    artifactHashSha256: artifactHash(), viewport: '390x844',
    method: 'trusted Chrome mouse down/move/up; displayed Korean prompt independently solved with integer equations; no answer hooks',
    run1: {phase: run1.phase, solved: run1.solved, lives: run1.lives, firstAttemptCorrect: run1.firstAttemptCorrect, firstAttemptTotal: run1.firstAttemptTotal},
    run2: {phase: run2.phase, solved: run2.solved, lives: run2.lives, firstAttemptCorrect: run2.firstAttemptCorrect, firstAttemptTotal: run2.firstAttemptTotal},
    consoleErrors: errors, log
  };
  result.passed = run1.phase === 'clear' && run1.solved === 8 && run1.lives === 3 && run1.firstAttemptCorrect === 8
    && run2.phase === 'clear' && run2.solved === 8 && run2.lives === 2 && run2.firstAttemptCorrect === 7 && errors.length === 0;
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify({...result, log: `${log.length} entries`}, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
