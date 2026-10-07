// 런타임이 실제로 내보내는 전체 문제은행을 독립 정수 계산으로 전수 검산한다.
import fs from 'node:fs/promises';
import fsSync from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(import.meta.dirname, '../../../../..');
const OUT = path.join(import.meta.dirname, 'math-bank-results.json');

function resolveChrome() {
  const base = path.join(os.homedir(), '.cache/puppeteer/chrome');
  if (!fsSync.existsSync(base)) return undefined;
  for (const build of fsSync.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome'
    ]) {
      const executable = path.join(base, build, rel);
      if (fsSync.existsSync(executable)) return executable;
    }
  }
  return undefined;
}

function root(n, id) {
  const value = Math.sqrt(n);
  if (!Number.isInteger(value)) throw new Error(`${id}: ${n} is not a perfect square`);
  return value;
}

function verify(problem) {
  const errors = [];
  const prompt = problem.prompt;
  if (/[√]|제곱근/.test(prompt + problem.answer)) errors.push('중2 금지 근호/제곱근');
  if (!Array.isArray(problem.choices) || !problem.choices.includes(problem.answer)) errors.push('정답이 선택지에 없음');
  if (new Set(problem.choices || []).size !== (problem.choices || []).length) errors.push('선택지 중복');

  let expectedText;
  let expectedNumeric;
  let m = prompt.match(/직각을 낀 두 변이 (\d+) cm, (\d+) cm/);
  if (m) {
    const answer = root(Number(m[1]) ** 2 + Number(m[2]) ** 2, problem.id);
    expectedText = `${answer} cm`; expectedNumeric = answer;
  } else if ((m = prompt.match(/빗변이 (\d+) cm이고 한 변이 (\d+) cm/))) {
    const answer = root(Number(m[1]) ** 2 - Number(m[2]) ** 2, problem.id);
    expectedText = `${answer} cm`; expectedNumeric = answer;
  } else if ((m = prompt.match(/AB=(\d+) cm, AD=(\d+) cm, AC=(\d+) cm/))) {
    if (!prompt.includes('D는 BC 위의 점이고 AD⊥BC이다')) errors.push('D 위치/수선 조건 누락');
    const h = Number(m[2]);
    const left = root(Number(m[1]) ** 2 - h ** 2, problem.id);
    const right = root(Number(m[3]) ** 2 - h ** 2, problem.id);
    expectedText = `BD=${left} cm, DC=${right} cm`; expectedNumeric = left + right;
  } else {
    errors.push('발문 파싱 실패');
  }
  if (expectedText !== undefined && problem.answer !== expectedText) errors.push(`정답 문자열 ${problem.answer} != ${expectedText}`);
  if (expectedNumeric !== undefined && problem.answerNumeric !== expectedNumeric) errors.push(`answerNumeric ${problem.answerNumeric} != ${expectedNumeric}`);
  return errors;
}

const server = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({ headless: true, executablePath: resolveChrome(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const page = await browser.newPage();
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
await page.goto(`${server.url}/g/keotlain/`, { waitUntil: 'load', timeout: 45000 });
await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 30000 });
const problems = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(1000));
const failures = problems.flatMap(problem => verify(problem).map(error => ({ id: problem.id, prompt: problem.prompt, error })));
const doubleCount = problems.filter(problem => problem.prompt.includes('이중 램프')).length;
const result = {
  verified: problems.length,
  doubleProblems: doubleCount,
  conditionChecked: 'D는 BC 위의 점이고 AD⊥BC이다',
  arithmetic: 'independent integer square/square-root identities',
  forbiddenRadicals: problems.filter(problem => /[√]|제곱근/.test(problem.prompt + problem.answer)).length,
  failures,
  passed: problems.length >= 300 && doubleCount > 0 && failures.length === 0
};
await fs.writeFile(OUT, JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
await page.close();
await browser.close();
await server.close();
if (!result.passed) process.exitCode = 1;
