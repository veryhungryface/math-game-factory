#!/usr/bin/env node
// 최종 WebGL이 실제로 푸시한 중앙값 은행 전체를 화면 발문 기준으로 전수 검산한다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../../../../..');
const out = path.join(here, 'runtime-bank-results.json');

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

function formatHalf(value2) {
  return value2 % 2 === 0 ? String(value2 / 2) : `${Math.floor(value2 / 2)}.5`;
}

const server = await serveStatic(path.join(root, 'public'));
const browser = await puppeteer.launch({
  headless: true,
  executablePath: chromePath(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'],
});
let report;
try {
  const page = await browser.newPage();
  await page.goto(`${server.url}/g/gaunde-jaba/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });
  const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(9999));
  const errors = [];
  const ids = new Set();
  const prompts = new Set();
  let odd = 0;
  let even = 0;
  let errorFind = 0;

  for (const p of bank) {
    if (!p.id || ids.has(p.id)) errors.push(`duplicate id: ${p.id}`); else ids.add(p.id);
    if (!p.prompt || prompts.has(p.prompt)) errors.push(`duplicate prompt: ${p.id}`); else prompts.add(p.prompt);
    const match = /^관측 시간 ([0-9, ]+)초(?:(?:의 )|(?:에 놓인 ([0-9.]+)초 표식은 잘못되었다\. ))중앙값을 구하시오\.$/.exec(p.prompt);
    if (!match) { errors.push(`unparsed prompt: ${p.id}`); continue; }
    const values = match[1].split(',').map((v) => Number.parseInt(v.trim(), 10));
    const sorted = [...values].sort((a, b) => a - b);
    const answer2 = sorted.length % 2
      ? 2 * sorted[Math.floor(sorted.length / 2)]
      : sorted[sorted.length / 2 - 1] + sorted[sorted.length / 2];
    if (sorted.length % 2) odd++; else even++;
    if (match[2] !== undefined) {
      errorFind++;
      const wrong2 = Math.round(Number(match[2]) * 2);
      if (wrong2 === answer2) errors.push(`wrong mark equals answer: ${p.id}`);
      const candidates = new Set();
      const add = (v2) => { if (v2 >= 2 && v2 <= 24 && v2 !== answer2) candidates.add(v2); };
      add(values.length % 2 ? 2 * values[Math.floor(values.length / 2)] : values[values.length / 2 - 1] + values[values.length / 2]);
      if (values.length % 2 === 0) {
        add(2 * sorted[sorted.length / 2 - 1]);
        add(2 * sorted[sorted.length / 2]);
      }
      const sum2 = 2 * values.reduce((a, b) => a + b, 0);
      if (sum2 % values.length === 0) add(sum2 / values.length);
      add(2 * sorted[sorted.length - 1]);
      const counts = new Map();
      for (const v of sorted) counts.set(v, (counts.get(v) || 0) + 1);
      const maxCount = Math.max(...counts.values());
      const modes = [...counts.entries()].filter(([, c]) => c === maxCount && c > 1);
      if (modes.length === 1) add(2 * modes[0][0]);
      const fallback2 = answer2 === 24 ? 22 : answer2 + 2;
      if (candidates.size > 0 && !candidates.has(wrong2)) errors.push(`wrong mark is not a modeled misconception: ${p.id}`);
      if (candidates.size === 0 && wrong2 !== fallback2) errors.push(`unexplained order-error fallback: ${p.id}`);
    }
    if (answer2 < 2 || answer2 > 24) errors.push(`answer out of range: ${p.id}`);
    if (p.answer !== `${formatHalf(answer2)}초`) errors.push(`answer mismatch: ${p.id}`);
    if (p.answerNumeric !== answer2 / 2) errors.push(`answerNumeric mismatch: ${p.id}`);
    if (p.choices !== null && (!Array.isArray(p.choices) || p.choices.length !== 0)) {
      errors.push(`continuous input must have no choices: ${p.id}`);
    }
    const exactSolutions = Array.from({ length: 23 }, (_, i) => i + 2).filter((v2) => v2 === answer2);
    if (exactSolutions.length !== 1) errors.push(`non-unique solution: ${p.id}`);
  }

  report = {
    passed: bank.length === 516 && ids.size === 516 && prompts.size === 516 && errors.length === 0,
    total: bank.length,
    uniqueIds: ids.size,
    uniquePrompts: prompts.size,
    odd,
    even,
    errorFind,
    errors,
  };
  await page.close();
} finally {
  await browser.close();
  await server.close();
}
fs.writeFileSync(out, `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify(report, null, 2));
if (!report.passed) process.exitCode = 1;
