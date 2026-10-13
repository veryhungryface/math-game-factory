#!/usr/bin/env node
// 1차 수정 검증: 360문항 수학 모델 전수검사 + 실제 pointer 무실수/1회 실수 회복 완주.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {fileURLToPath} from 'node:url';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/golem-hapche');
const WORK = path.resolve(ROOT, process.env.MGF_WORK || 'factory/work-lanes/20261013-055311-L2');
const OUT = path.join(WORK, 'validation/golem-hapche-fix1');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome'
    ]) {
      const candidate = path.join(base, build, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
  throw new Error('Chrome for Testing not found');
}

function artifactHash() {
  const files = [];
  (function walk(dir) {
    for (const name of fs.readdirSync(dir).sort()) {
      const file = path.join(dir, name);
      if (fs.statSync(file).isDirectory()) walk(file); else files.push(file);
    }
  })(GAME);
  const hash = crypto.createHash('sha256');
  for (const file of files) { hash.update(path.relative(GAME, file)); hash.update(fs.readFileSync(file)); }
  return hash.digest('hex');
}

function pairByIndex(index) {
  let n = 0;
  for (let a = 0; a < 6; a++) for (let b = a + 1; b < 6; b++, n++) if (n === index) return [a, b];
  throw new Error(`bad pair ${index}`);
}

function pairLabel(index) {
  const [a, b] = pairByIndex(index);
  return `${'ABCDEF'[a]}와 ${'ABCDEF'[b]}`;
}

function shieldPoint(index) {
  const col = index % 2, row = Math.floor(index / 2);
  return {x: 390 * (col === 0 ? .27 : .73), y: 844 * (1 - (.485 - row * .18))};
}

async function dragPair(page, pairIndex) {
  const [a, b] = pairByIndex(pairIndex), from = shieldPoint(a), to = shieldPoint(b);
  await page.mouse.move(from.x, from.y);
  await page.mouse.down();
  await sleep(50);
  await page.mouse.move(to.x, to.y, {steps: 5});
  await sleep(50);
  await page.mouse.up();
}

function angleSignature(a, b) {
  return [a, b, 180 - a - b].sort((x, y) => x - y).join(':');
}

function auditBank(bank) {
  const errors = [];
  let aa = 0, volume = 0, labelledTriangles = 0;
  for (const problem of bank) {
    const segments = problem.prompt.split(' / ').slice(-6);
    if (problem.id.endsWith('-aa')) {
      aa++;
      const groups = new Map();
      for (const segment of segments) {
        const match = segment.match(/([A-F])=△([A-Z]{3}) ∠([A-Z])=(\d+)° · ∠([A-Z])=(\d+)°/);
        if (!match) { errors.push(`${problem.id}: 꼭짓점 결합 각 표기 누락`); continue; }
        const signature = angleSignature(Number(match[4]), Number(match[6]));
        const list = groups.get(signature) || []; list.push(match[1]); groups.set(signature, list);
      }
      const duplicate = [...groups.values()].filter(list => list.length === 2);
      if (duplicate.length !== 1 || groups.size !== 5) errors.push(`${problem.id}: AA 실제 닮은 쌍 ${duplicate.length}개`);
      else if (`${duplicate[0][0]}와 ${duplicate[0][1]}` !== problem.answer) errors.push(`${problem.id}: AA 판정과 answer 불일치`);
    } else if (problem.id.endsWith('-volume')) {
      volume++;
      const solids = [];
      for (const segment of segments) {
        const match = segment.match(/([A-F])=직육면체 [A-F] 가로=(\d+) cm · 세로=(\d+) cm 높이=(\d+) cm/);
        if (!match) { errors.push(`${problem.id}: 입체 3축 표기 누락`); continue; }
        solids.push({letter: match[1], dims: match.slice(2).map(Number)});
      }
      const matches = [];
      for (let i = 0; i < solids.length; i++) for (let j = i + 1; j < solids.length; j++) {
        const x = solids[i].dims, y = solids[j].dims;
        const similar = x[0] * y[1] === x[1] * y[0] && x[0] * y[2] === x[2] * y[0];
        const twoToThree = similar && (x[0] * 3 === y[0] * 2 || y[0] * 3 === x[0] * 2);
        if (twoToThree) matches.push(`${solids[i].letter}와 ${solids[j].letter}`);
      }
      if (matches.length !== 1 || matches[0] !== problem.answer) errors.push(`${problem.id}: 입체 닮음비 2:3 정답쌍 ${JSON.stringify(matches)}`);
    } else {
      for (const segment of segments) {
        if (/=△/.test(segment) && /[A-Z]{2}=\d+ cm/.test(segment)) labelledTriangles++;
      }
    }
  }
  if (bank.length !== 360) errors.push(`은행 크기 ${bank.length}`);
  return {bankCount: bank.length, aaCount: aa, volumeCount: volume, labelledTriangleCards: labelledTriangles, errors, passed: errors.length === 0};
}

async function completeRun(page, runSerial, firstWrong) {
  await page.evaluate(() => window.__GAME_TEST__.start());
  await sleep(120);
  const inputs = [];
  for (let contract = 0; contract < 6; contract++) {
    const correct = ((runSerial * 7 + contract * 5 + contract * contract) % 15 + 15) % 15;
    if (firstWrong && contract === 0) {
      const wrong = (correct + 1) % 15;
      await dragPair(page, wrong); await sleep(1650);
      inputs.push({contract: contract + 1, pair: pairLabel(wrong), expected: 'wrong'});
    }
    await dragPair(page, correct); await sleep(contract === 5 ? 1900 : 1800);
    inputs.push({contract: contract + 1, pair: pairLabel(correct), expected: 'correct'});
  }
  const state = await page.evaluate(() => window.__GAME_TEST__.getState());
  return {runSerial, firstWrong, inputs, state};
}

fs.mkdirSync(OUT, {recursive: true});
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']});
try {
  const page = await browser.newPage(), consoleErrors = [];
  await page.setViewport({width: 390, height: 844, deviceScaleFactor: 1});
  page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
  page.on('pageerror', error => consoleErrors.push(String(error)));
  await page.goto(`${server.url}/g/golem-hapche/`, {waitUntil: 'load', timeout: 45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(1000));
  const mathAudit = auditBank(bank);
  const perfect = await completeRun(page, 1, false);
  await page.screenshot({path: path.join(OUT, 'perfect-clear.png')});
  const recovered = await completeRun(page, 2, true);
  await page.screenshot({path: path.join(OUT, 'recovered-clear.png')});
  const result = {
    runId: `golem-hapche-fix1-${new Date().toISOString()}`,
    artifactHashSha256: artifactHash(),
    mathAudit,
    perfect,
    recovered,
    consoleErrors
  };
  result.passed = mathAudit.passed && perfect.state.phase === 'clear' && perfect.state.firstAttemptCorrect === 6 &&
    recovered.state.phase === 'clear' && recovered.state.firstAttemptCorrect === 5 && recovered.state.lives === 2 && consoleErrors.length === 0;
  fs.writeFileSync(path.join(OUT, 'results.json'), JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
