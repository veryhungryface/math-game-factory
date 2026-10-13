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
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'
  ]) { const p = path.join(base, build, rel); if (fs.existsSync(p)) return p; }
  throw new Error('Chrome for Testing not found');
}

function expected(problem) {
  let m;
  if ((m = problem.prompt.match(/(\d+)종류 또는 .+? (\d+)종류 중 하나만/))) return { type: 'union', value: Number(m[1]) + Number(m[2]) };
  if ((m = problem.prompt.match(/(\d+)종류와 .+? (\d+)종류 중에서 각각 하나씩/))) return { type: 'product', value: Number(m[1]) * Number(m[2]) };
  if ((m = problem.prompt.match(/두 눈의 수의 합이 (\d+)인/))) {
    const target = Number(m[1]); let count = 0;
    for (let a = 1; a <= 6; a++) for (let b = 1; b <= 6; b++) if (a + b === target) count++;
    return { type: 'dice', value: count };
  }
  if ((m = problem.prompt.match(/에 ([0-9, ]+)([이가]) 각각 하나씩 적힌 카드/))) {
    const digits = m[1].split(',').map(v => Number(v.trim())); let count = 0;
    for (let a = 0; a < digits.length; a++) for (let b = 0; b < digits.length; b++) if (a !== b && digits[a] !== 0) count++;
    const finalDigit = digits.at(-1);
    const particleExpected = [0, 1, 3, 6, 7, 8].includes(finalDigit) ? '이' : '가';
    return { type: 'digits', value: count, particle: m[2], particleExpected };
  }
  if ((m = problem.prompt.match(/후보 (\d+)명 중에서 역할 구분 없이 대표 2명/))) {
    const n = Number(m[1]); return { type: 'representatives', value: n * (n - 1) / 2 };
  }
  throw new Error(`unparsed prompt: ${problem.prompt}`);
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
let result;
try {
  const page = await browser.newPage();
  await page.goto(`${server.url}/g/byeolkan-gwancheukso/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 30000 });
  const problems = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(9999));
  const errors = [], types = {};
  for (const p of problems) {
    try {
      const e = expected(p); types[e.type] = (types[e.type] || 0) + 1;
      if (e.particle && e.particle !== e.particleExpected) errors.push(`${p.id}: particle ${e.particle} != ${e.particleExpected}`);
      if (p.answerNumeric !== e.value) errors.push(`${p.id}: numeric ${p.answerNumeric} != ${e.value}`);
      if (p.answer !== `${e.value}가지`) errors.push(`${p.id}: answer ${p.answer}`);
      if (!Array.isArray(p.choices) || !p.choices.includes(p.answer)) errors.push(`${p.id}: answer absent from choices`);
      if (new Set(p.choices).size !== p.choices.length) errors.push(`${p.id}: duplicate choice`);
      const distractors = p.choices.filter(choice => choice !== p.answer);
      if (distractors.length < 3 || distractors.some(choice => choice === `${e.value}가지`)) errors.push(`${p.id}: accidental-true distractor`);
    } catch (error) { errors.push(`${p.id}: ${error.message}`); }
  }
  result = { generated_at: new Date().toISOString(), live_bank_count: problems.length, types, errors, accidental_true_distractors: errors.filter(e => e.includes('accidental-true')).length,
    passed: problems.length >= 300 && Object.keys(types).length === 5 && errors.length === 0 };
  fs.writeFileSync(path.join(HERE, 'math-recheck-results.json'), JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally { await browser.close(); await server.close(); }
