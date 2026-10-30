#!/usr/bin/env node
// 최종 WebGL ProblemBankJson을 C# 생성기와 독립된 정규식·정수 계산으로 전수 검산한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/popcorn-pinball');
const RUN_ID = process.env.MGF_RUN_ID || `popcorn-fix2-math-${Date.now()}`;

function chromePath() {
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
  for (const name of ['index.html', 'meta.json', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(x => `Build/${x}`)]) {
    hash.update(name); hash.update(fs.readFileSync(path.join(GAME, name)));
  }
  return hash.digest('hex');
}

function expected(prompt) {
  let match = prompt.match(/∠AOT=(\d+)°, ∠TOB=(\d+)°/);
  if (match) return (+match[1] + +match[2]) / 2;
  match = prompt.match(/∠AQB=(\d+)°/);
  if (match) return +match[1];
  match = prompt.match(/중심각 ∠AOB의 크기가 (\d+)°/);
  if (match) return +match[1] / 2;
  throw new Error(`알 수 없는 발문: ${prompt}`);
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--mute-audio'] });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
  await page.goto(`${server.url}/g/popcorn-pinball/?math=${Date.now()}`, { waitUntil: 'load', timeout: 60000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 90000 });
  const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(10000));
  const errors = [], prompts = new Set();
  for (const problem of bank) {
    const want = expected(problem.prompt);
    if (!Number.isInteger(want) || want < 5 || want > 85 || want % 5) errors.push(`${problem.id}: 계산 범위 ${want}`);
    if (problem.answerNumeric !== want) errors.push(`${problem.id}: answerNumeric ${problem.answerNumeric} != ${want}`);
    if (problem.answer !== `${want}°`) errors.push(`${problem.id}: answer ${problem.answer} != ${want}°`);
    if (!problem.prompt.includes('호 AB에 대한 중심각') && !problem.prompt.includes('작은 호 AB에 대한 원주각 ∠APB')
      && !problem.prompt.includes('모두 작은 호 AB에 대한 원주각'))
      errors.push(`${problem.id}: 목표 호-각 대응 누락`);
    if (/호의 길이|정비례/.test(problem.prompt + problem.unitConcept)) errors.push(`${problem.id}: 교육과정 밖 비례 표현`);
    if (prompts.has(problem.prompt)) errors.push(`${problem.id}: 중복 발문`);
    prompts.add(problem.prompt);
  }
  const result = {
    generatedAt: new Date().toISOString(), runId: RUN_ID, artifactHashSha256: artifactHash(),
    source: 'final WebGL ProblemBankJson via sampleProblems(10000)', bankCount: bank.length,
    uniquePrompts: prompts.size, checkedIntegerPaths: bank.length * 17, errors, passed: bank.length === 323 && prompts.size === 323 && errors.length === 0,
  };
  fs.writeFileSync(path.join(HERE, 'math-bank-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
  await page.close();
} finally {
  await browser.close();
  await server.close();
}
