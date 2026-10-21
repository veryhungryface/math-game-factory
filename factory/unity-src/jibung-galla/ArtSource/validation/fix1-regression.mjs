#!/usr/bin/env node
// 1차 수정 회귀: 최종 WebGL 문제은행 수학·D 정의와 1280/1440/2000 가로 안전영역을 실측한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/jibung-galla');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
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
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(file => `Build/${file}`)]) {
    hash.update(name); hash.update(fs.readFileSync(path.join(GAME, name)));
  }
  return hash.digest('hex');
}

function checkProblem(p) {
  const h = p.answerNumeric;
  if (!Number.isInteger(h) || h < 25 || h > 64) return false;
  if (!p.prompt.startsWith('AB=AC인 이등변삼각형 ABC에서') || !p.prompt.includes('만나는 점을 D라고 할 때') || !p.prompt.includes('선 AD')) return false;
  if (p.answer !== `∠BAD=${h}°인 내부의 선 AD`) return false;
  const apex = p.prompt.match(/∠A=(\d+)°/); if (apex && Number(apex[1]) !== h * 2) return false;
  const base = p.prompt.match(/∠B=(\d+)°/); if (base && Number(base[1]) !== 90 - h) return false;
  return Boolean(apex || base);
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const consoleErrors = [];
try {
  const layouts = [];
  let bankAudit = null;
  for (const spec of [{ w: 1280, h: 800 }, { w: 1440, h: 900 }, { w: 2000, h: 1045 }]) {
    const page = await browser.newPage(); await page.setViewport({ width: spec.w, height: spec.h, deviceScaleFactor: 1 });
    page.on('console', m => { if (m.type() === 'error') consoleErrors.push(`${spec.w}: ${m.text()}`); });
    page.on('pageerror', e => consoleErrors.push(`${spec.w}: ${String(e)}`));
    await page.goto(`${server.url}/g/jibung-galla/`, { waitUntil: 'load', timeout: 45000 });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
    if (!bankAudit) {
      const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(320));
      bankAudit = { count: bank.length, uniqueIds: new Set(bank.map(p => p.id)).size, allMathAndWordingValid: bank.every(checkProblem) };
    }
    await page.evaluate(() => window.__GAME_TEST__.start());
    await page.waitForFunction(() => { const s = window.__GAME_TEST__.getState(); return s.phase === 'playing' && s.safeGap !== 0; }, { timeout: 5000 });
    await sleep(250);
    const state = await page.evaluate(() => window.__GAME_TEST__.getState());
    const layout = { viewport: `${spec.w}x${spec.h}`, hudRight: state.hudRight, goalRight: state.goalRight, promptLeft: state.promptLeft, safeGap: state.safeGap, passed: state.safeGap >= 16 };
    layouts.push(layout);
    await page.screenshot({ path: path.join(HERE, `fix1-wide-${spec.w}.png`) });
    await page.close();
  }
  const passed = bankAudit.count === 320 && bankAudit.uniqueIds === 320 && bankAudit.allMathAndWordingValid && layouts.every(x => x.passed) && consoleErrors.length === 0;
  const out = { generatedAt: new Date().toISOString(), artifactHashSha256: artifactHash(), bankAudit, layouts, consoleErrors, passed };
  fs.writeFileSync(path.join(HERE, 'fix1-regression-results.json'), `${JSON.stringify(out, null, 2)}\n`);
  console.log(JSON.stringify(out, null, 2));
  if (!passed) process.exitCode = 1;
} finally { await browser.close(); await server.close(); }
