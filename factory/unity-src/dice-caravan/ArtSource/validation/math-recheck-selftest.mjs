import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/dice-caravan');

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'
    ]) {
      const candidate = path.join(base, build, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
  throw new Error('Chrome for Testing not found');
}

function buildHash() {
  const hash = crypto.createHash('sha256');
  const names = ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(name => `Build/${name}`)];
  for (const name of names) {
    hash.update(name); hash.update('\0'); hash.update(fs.readFileSync(path.join(GAME, name))); hash.update('\0');
  }
  return hash.digest('hex');
}

function expectedFromPrompt(problem) {
  const prompt = problem.prompt;
  let match;
  if ((match = prompt.match(/1부터 (\d+)까지의 자연수 중 (\d+)의 배수 또는 (\d+)의 배수/))) {
    const [, n, a, b] = match.map(Number);
    let count = 0;
    for (let value = 1; value <= n; value++) if (value % a === 0 || value % b === 0) count++;
    const countA = Math.floor(n / a), countB = Math.floor(n / b);
    return { family: 'disjoint-union', expected: count, misconceptions: [countA * countB, Math.max(countA, countB), n] };
  }
  if ((match = prompt.match(/1부터 (\d+)까지의 자연수 중 (\d+)의 배수/))) {
    const [, n, divisor] = match.map(Number);
    const expected = Math.floor(n / divisor);
    return { family: 'single-event', expected, misconceptions: [n, divisor, n - expected] };
  }
  if ((match = prompt.match(/(\d+)종류와 .+? (\d+)종류 중에서 각각 하나씩/))) {
    const [, rows, cols] = match.map(Number);
    return { family: 'product-rule', expected: rows * cols, misconceptions: [rows + cols, Math.max(rows, cols), (rows + 1) * (cols + 1)] };
  }
  if ((match = prompt.match(/두 눈의 수의 합이 (\d+)인/))) {
    const target = Number(match[1]);
    let count = 0;
    for (let a = 1; a <= 6; a++) for (let b = 1; b <= 6; b++) if (a + b === target) count++;
    return { family: 'ordered-dice-sum', expected: count, misconceptions: [Math.max(1, Math.ceil(count / 2)), target, 36] };
  }
  if ((match = prompt.match(/두 눈의 수의 곱이 (\d+)인/))) {
    const target = Number(match[1]);
    let count = 0;
    for (let a = 1; a <= 6; a++) for (let b = 1; b <= 6; b++) if (a * b === target) count++;
    return { family: 'ordered-dice-product', expected: count, misconceptions: [Math.max(1, Math.ceil(count / 2)), target, 36] };
  }
  if ((match = prompt.match(/카드 꾸러미에 ((?:\d, ){3}\d) 숫자 카드가 각각 한 장씩/))) {
    const digits = match[1].split(', ').map(Number);
    let count = 0;
    for (let tens = 0; tens < digits.length; tens++) for (let ones = 0; ones < digits.length; ones++) {
      if (tens !== ones && digits[tens] !== 0) count++;
    }
    return { family: 'digit-cards', expected: count, digits, misconceptions: [digits.length * (digits.length - 1), digits.length ** 2, (digits.length - 1) * (digits.length - 2)] };
  }
  if ((match = prompt.match(/(\d+)명의 후보 중에서 (회장 1명과 부회장 1명|대표 2명)/))) {
    const n = Number(match[1]);
    const roles = match[2].startsWith('회장');
    return { family: roles ? 'role-representatives' : 'unordered-representatives', expected: roles ? n * (n - 1) : n * (n - 1) / 2,
      misconceptions: roles ? [n * (n - 1) / 2, n * n] : [n * (n - 1), n * (n + 1) / 2] };
  }
  throw new Error(`unrecognized prompt: ${problem.id}: ${prompt}`);
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
let report;
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
  await page.goto(`${server.url}/g/dice-caravan/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 30000 });
  const problems = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(500));
  const errors = [];
  const familyCounts = {};
  let digitPrompts = 0;
  for (const problem of problems) {
    let independent;
    try { independent = expectedFromPrompt(problem); }
    catch (error) { errors.push(String(error.message || error)); continue; }
    familyCounts[independent.family] = (familyCounts[independent.family] || 0) + 1;
    if (independent.family === 'digit-cards') {
      digitPrompts++;
      if (independent.digits.length !== 4 || new Set(independent.digits).size !== 4 || independent.digits.some(digit => !Number.isInteger(digit) || digit < 0 || digit > 9)) {
        errors.push(`${problem.id}: four separate one-digit cards were not shown`);
      }
    }
    if (problem.answerNumeric !== independent.expected) errors.push(`${problem.id}: answerNumeric=${problem.answerNumeric}, independently expected=${independent.expected}`);
    if (problem.answer !== `${independent.expected}가지`) errors.push(`${problem.id}: answer text mismatch (${problem.answer})`);
    if (!problem.choices.includes(problem.answer)) errors.push(`${problem.id}: answer absent from choices`);
    const numericChoices = problem.choices.map(choice => Number.parseInt(choice, 10));
    if (numericChoices.filter(value => value === independent.expected).length !== 1) errors.push(`${problem.id}: expected value is not unique in choices`);
    const targeted = [...new Set(independent.misconceptions.filter(value => value > 0 && value !== independent.expected))]
      .filter(value => numericChoices.includes(value));
    if (targeted.length < 2) errors.push(`${problem.id}: only ${targeted.length} parameter-derived misconception distractor(s): ${numericChoices.join(',')}`);
  }
  report = {
    generated_at: new Date().toISOString(),
    build_sha256: buildHash(),
    source: 'live WebGL window.__GAME_TEST__.sampleProblems(500)',
    verified_count: problems.length,
    unique_ids: new Set(problems.map(problem => problem.id)).size,
    family_counts: familyCounts,
    digit_prompts_checked: digitPrompts,
    checks: ['independent integer recomputation from every prompt', 'answerNumeric agreement', 'answer text and unique choice agreement', 'at least two parameter-derived misconception distractors per problem', 'all digit prompts show four comma-separated one-digit cards without a dangling Korean particle'],
    errors,
    verdict: problems.length >= 300 && digitPrompts > 0 && errors.length === 0 ? 'pass' : 'fail'
  };
  fs.writeFileSync(path.join(HERE, 'math-recheck-results.json'), `${JSON.stringify(report, null, 2)}\n`);
  console.log(JSON.stringify(report, null, 2));
  if (report.verdict !== 'pass') process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
