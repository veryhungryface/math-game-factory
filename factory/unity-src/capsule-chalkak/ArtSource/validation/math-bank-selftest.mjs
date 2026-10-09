import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    ]) {
      const candidate = path.join(base, build, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
  throw new Error('Chrome for Testing not found');
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox'] });
try {
  const page = await browser.newPage();
  await page.goto(`${server.url}/g/capsule-chalkak/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 20000 });
  const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(10000));
  const errors = [];
  const ids = new Set();
  const prompts = new Set();
  for (const problem of bank) {
    if (!problem.id || ids.has(problem.id)) errors.push(`duplicate/missing id: ${problem.id}`);
    ids.add(problem.id);
    prompts.add(problem.prompt);
    if (!Array.isArray(problem.choices) || problem.choices.length !== 4) errors.push(`${problem.id}: choices length`);
    const unique = new Set(problem.choices || []);
    if (unique.size !== (problem.choices || []).length) errors.push(`${problem.id}: duplicate choice`);
    if (!(problem.choices || []).includes(problem.answer)) errors.push(`${problem.id}: answer absent`);
    const wrong = (problem.choices || []).filter(choice => choice !== problem.answer);
    if (wrong.length !== 3) errors.push(`${problem.id}: distractor accidentally equals answer`);
    if (!Number.isFinite(problem.answerNumeric) || problem.answerNumeric < 0 || problem.answerNumeric > 1) errors.push(`${problem.id}: invalid numeric answer`);
    if (/[√]/.test(problem.prompt)) errors.push(`${problem.id}: middle-2 forbidden radical`);
    if (/의 자리에서/.test(problem.prompt)) errors.push(`${problem.id}: forbidden rounding phrase`);
  }
  if (bank.length < 300) errors.push(`bank too small: ${bank.length}`);
  const output = {
    generatedAt: new Date().toISOString(),
    totalProblems: bank.length,
    uniqueIds: ids.size,
    uniquePrompts: prompts.size,
    checked: [
      'answer belongs to four unique choices',
      'three distractors differ from the answer for every generated parameter tuple',
      'answerNumeric is finite and within 0..1',
      'no middle-2 radical or forbidden rounding phrase',
    ],
    errors,
    passed: errors.length === 0,
  };
  fs.writeFileSync(path.join(HERE, 'math-bank-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
  if (!output.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
