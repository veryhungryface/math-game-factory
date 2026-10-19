import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
  throw new Error('Chrome for Testing not found');
}
function validRoster(canonical, roster) {
  return canonical.length === roster.length && new Set(canonical).size === canonical.length &&
    new Set(roster).size === roster.length && roster.every(id => canonical.includes(id));
}

const server = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  const page = await browser.newPage();
  await page.goto(`${server.url}/g/bulkkot-jeomgeom/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 30000 });
  const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(420));
  const errors = [];
  for (const p of bank) {
    if (!Array.isArray(p.canonicalCaseIds) || +p.answer !== p.canonicalCaseIds.length || p.answerNumeric !== p.canonicalCaseIds.length) errors.push(`${p.id}: answer`);
    if (!Array.isArray(p.initialCaseIds) || validRoster(p.canonicalCaseIds, p.initialCaseIds)) errors.push(`${p.id}: initial`);
    const repaired = [...(p.initialCaseIds || [])];
    for (const r of p.validCorrectionExample || []) {
      if (repaired[r.slot] !== r.from) errors.push(`${p.id}: repair-from`);
      repaired[r.slot] = r.to;
    }
    if (!validRoster(p.canonicalCaseIds || [], repaired)) errors.push(`${p.id}: repair-result`);
    if (!Array.isArray(p.misconceptionIds) || p.misconceptionIds.length < 2) errors.push(`${p.id}: misconceptions`);
    if (/순열|조합|확률|제곱근|에서\s*반올림/.test(p.prompt)) errors.push(`${p.id}: expression`);
  }
  const result = {
    run_id: `bulkkot-bank-${new Date().toISOString()}`,
    count: bank.length,
    uniqueIds: new Set(bank.map(p => p.id)).size,
    uniquePrompts: new Set(bank.map(p => p.prompt)).size,
    exhaustiveChecks: ['answer=|S|', 'canonical unique', 'initial invalid', 'repair example yields exact roster', 'misconception ids>=2', 'forbidden expressions absent'],
    errors,
    passed: bank.length >= 300 && new Set(bank.map(p => p.id)).size === bank.length && new Set(bank.map(p => p.prompt)).size === bank.length && errors.length === 0
  };
  fs.writeFileSync(path.join(HERE, 'math-bank-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2)); if (!result.passed) process.exitCode = 1;
} finally { await browser.close(); await server.close(); }
