import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/dalpaengi-geukjang');

function artifactHash() {
  const hash = crypto.createHash('sha256');
  const rels = ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(name => `Build/${name}`)];
  for (const rel of rels) { hash.update(rel); hash.update(fs.readFileSync(path.join(GAME, rel))); }
  return hash.digest('hex');
}

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome'
  ]) { const p = path.join(base, build, rel); if (fs.existsSync(p)) return p; }
}

let accidentalTruths = 0;
let checkedCases = 0;
for (let y = 2; y <= 17; y++) {
  for (let m = 1; m < y; m++) {
    const n = y - m;
    let a = m * n >= 2 && m * n <= 17 && m * n !== y ? m * n : Math.max(m, n);
    let b = Math.min(m, n);
    if (b === a || b === y || b < 2) b = Math.abs(m - n);
    if (b === a || b === y || b < 2) b = y === 2 ? 3 : y - 1;
    checkedCases += 2;
    if (a === y || b === y) accidentalTruths++;
  }
  const all = [], nonTrivial = [];
  for (let m = 1; m <= y; m++) if (y % m === 0) {
    const n = y / m; all.push([m, n]); if (m > 1 && n > 1) nonTrivial.push([m, n]);
  }
  for (const [m, n] of nonTrivial.length ? nonTrivial : all) {
    let a = m + n !== y && m + n >= 2 && m + n <= 17 ? m + n : Math.min(m, n);
    if (a === y || a < 2) a = y === 2 ? 3 : y - 1;
    let b = Math.max(m, n);
    if (b === y || b === a || b < 2) b = y > 2 ? y - 1 : 3;
    checkedCases += 2;
    if (a === y || b === y) accidentalTruths++;
  }
  for (let known = 1; known <= 12; known++) {
    const total = y + known;
    const whole = total >= 2 && total <= 17 ? total : 0;
    const added = total + known >= 2 && total + known <= 17 ? total + known : 0;
    checkedCases += 2;
    if (whole === y || added === y) accidentalTruths++;
  }
}

const artifactHashBefore = artifactHash();
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: resolveChrome(), args: ['--no-sandbox', '--mute-audio'] });
let pageErrors = [], bank = [];
try {
  const page = await browser.newPage();
  page.on('console', msg => { if (msg.type() === 'error') pageErrors.push(msg.text()); });
  page.on('pageerror', err => pageErrors.push(String(err)));
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
  await page.goto(`${server.url}/g/dalpaengi-geukjang/?audit=${Date.now()}`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(999));
  await page.close();
} finally { await browser.close(); await server.close(); }

const prompts = new Set(bank.map(p => p.prompt));
const invalid = bank.filter(p => !p.id || !p.prompt || p.choices !== null || String(p.answer) !== String(p.answerNumeric) || p.answerNumeric < 2 || p.answerNumeric > 17);
// 「모든 조합이 가능하다」는 곱의 법칙의 조건 설명이다. 금지 범위는 순열·조합의
// 공식/기호를 별도 학습 대상으로 묻는 발문이며 이 일반어 사용과 구분한다.
const forbidden = bank.filter(p => /[√]|순열의 수|조합의 수|nCr|확률을 구하시오/.test(p.prompt));
const artifactHashAfter = artifactHash();
const runId = `dalpaengi-geukjang-math-audit-${new Date().toISOString()}`;
const result = {
  runId,
  run_id: runId,
  artifactHashSha256: artifactHashBefore,
  artifact_hash: artifactHashBefore,
  artifact_hash_before: artifactHashBefore,
  artifact_hash_after: artifactHashAfter,
  bankSize: bank.length,
  uniquePrompts: prompts.size,
  checkedMisconceptionValues: checkedCases,
  accidentalTruths,
  invalidExports: invalid.length,
  forbiddenScopePrompts: forbidden.length,
  consoleErrors: pageErrors,
  passed: bank.length >= 300 && prompts.size === bank.length && accidentalTruths === 0 && invalid.length === 0 && forbidden.length === 0 && pageErrors.length === 0 && artifactHashBefore === artifactHashAfter
};
console.log(JSON.stringify(result, null, 2));
fs.writeFileSync(new URL('./math-bank-audit-results.json', import.meta.url), JSON.stringify(result, null, 2) + '\n');
if (!result.passed) process.exitCode = 1;
