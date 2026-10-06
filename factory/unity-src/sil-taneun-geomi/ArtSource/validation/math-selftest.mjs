import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
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
  return undefined;
}

function expected(prompt) {
  let match = prompt.match(/AD=(\d+) cm, DB=(\d+) cm, AE=(\d+) cm.*EC의 길이/);
  if (match) {
    const [, ad, db, ae] = match.map(Number);
    return (ae * db) / ad;
  }
  match = prompt.match(/AD=(\d+) cm, DB=(\d+) cm, BC=(\d+) cm.*DE의 길이/);
  if (match) {
    const [, ad, db, bc] = match.map(Number);
    return (bc * ad) / (ad + db);
  }
  match = prompt.match(/BC=(\d+) cm.*MN의 길이/);
  if (match) return Number(match[1]) / 2;
  match = prompt.match(/왼쪽 위 구간=(\d+) cm, 왼쪽 아래 구간=(\d+) cm, 오른쪽 위 구간=(\d+) cm.*오른쪽 아래 구간/);
  if (match) {
    const [, leftTop, leftBottom, rightTop] = match.map(Number);
    return (leftBottom * rightTop) / leftTop;
  }
  match = prompt.match(/왼쪽 위 구간=(\d+) cm, 왼쪽 아래 구간=(\d+) cm, 오른쪽 아래 구간=(\d+) cm.*오른쪽 위 구간/);
  if (match) {
    const [, leftTop, leftBottom, rightBottom] = match.map(Number);
    return (leftTop * rightBottom) / leftBottom;
  }
  throw new Error(`독립 풀이기가 읽지 못한 발문: ${prompt}`);
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']
});
const page = await browser.newPage();
const errors = [];
page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
page.on('pageerror', error => errors.push(String(error)));
await page.goto(`${server.url}/g/sil-taneun-geomi/`, {waitUntil: 'load', timeout: 45000});
await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(480));

const ids = new Set();
const prompts = new Set();
const mismatches = [];
let forbidden = 0;
let contractErrors = 0;
for (const problem of bank) {
  if (ids.has(problem.id)) contractErrors++;
  ids.add(problem.id);
  prompts.add(`${problem.prompt}|${problem.answer}`);
  if (!Number.isInteger(problem.answerNumeric) || problem.answerNumeric < 1 || problem.answerNumeric > 24) contractErrors++;
  if (problem.answer !== `${problem.answerNumeric} cm`) contractErrors++;
  if (problem.choices !== null) contractErrors++;
  if (/반올림|제곱근|√|가정|결론/.test(problem.prompt)) forbidden++;
  const solved = expected(problem.prompt);
  if (!Number.isInteger(solved) || solved !== problem.answerNumeric) {
    mismatches.push({id: problem.id, expected: solved, actual: problem.answerNumeric, prompt: problem.prompt});
  }
}

const result = {
  runId: `sil-taneun-geomi-math-${new Date().toISOString()}`,
  sampleCount: bank.length,
  uniqueIds: ids.size,
  uniquePromptAnswers: prompts.size,
  independentIntegerMismatches: mismatches.length,
  forbiddenExpressions: forbidden,
  contractErrors,
  consoleErrors: errors,
  mismatches: mismatches.slice(0, 10),
  passed: bank.length === 480 && ids.size === 480 && prompts.size === 480 && mismatches.length === 0 && forbidden === 0 && contractErrors === 0 && errors.length === 0
};
console.log(JSON.stringify(result, null, 2));
fs.writeFileSync(new URL('./math-results.json', import.meta.url), JSON.stringify(result, null, 2));
await browser.close();
await server.close();
if (!result.passed) process.exitCode = 1;
