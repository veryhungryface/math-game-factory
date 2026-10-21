#!/usr/bin/env node
// 2차 수정 회귀: 실패했던 A 밖 시작 입력, 교과서 발문, CSS 판형/DPR 2 안전영역을 최종 WebGL에서 실측한다.
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

function bankProblemOk(p) {
  const h = p.answerNumeric;
  if (!Number.isInteger(h) || h < 25 || h > 64) return false;
  if (!p.prompt.startsWith('AB=AC인 이등변삼각형 ABC에서')) return false;
  if (!p.prompt.includes('BC 위에 점 D') || !p.prompt.includes('점 D를 지나도록 ∠A의 이등분선')) return false;
  if (p.prompt.includes('∠BAE') && (!p.prompt.includes('BC 위의 점 E') || !p.prompt.includes('선분 AE'))) return false;
  if (p.answer !== `∠BAD=${h}°인 ∠A의 이등분선 AD`) return false;
  const apex = p.prompt.match(/∠A=(\d+)°/); if (apex && Number(apex[1]) !== h * 2) return false;
  const base = p.prompt.match(/∠B=(\d+)°/); if (base && Number(base[1]) !== 90 - h) return false;
  return Boolean(apex || base);
}

async function readyPage(browser, serverUrl, viewport, errors) {
  const page = await browser.newPage();
  await page.setViewport(viewport);
  page.on('console', m => { if (m.type() === 'error') errors.push(`${viewport.width}x${viewport.height}: ${m.text()}`); });
  page.on('pageerror', e => errors.push(`${viewport.width}x${viewport.height}: ${String(e)}`));
  await page.goto(`${serverUrl}/g/jibung-galla/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  return page;
}

async function click(page, x, y) {
  await page.mouse.move(x, y); await page.mouse.down(); await sleep(70); await page.mouse.up();
}

async function drag(page, a, b) {
  await page.mouse.move(a.x, a.y); await page.mouse.down();
  await page.mouse.move((a.x + b.x) / 2, (a.y + b.y) / 2, { steps: 8 });
  await page.mouse.move(b.x, b.y, { steps: 8 }); await sleep(70); await page.mouse.up();
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const consoleErrors = [];
try {
  const mobile = await readyPage(browser, server.url, { width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true }, consoleErrors);
  const bank = await mobile.evaluate(() => window.__GAME_TEST__.sampleProblems(320));
  const bankAudit = { count: bank.length, uniqueIds: new Set(bank.map(p => p.id)).size, allMathAndWordingValid: bank.every(bankProblemOk) };

  const onboardingStart = Date.now();
  await click(mobile, 195, 422);
  await mobile.waitForFunction(() => window.__GAME_TEST__.getState().onboarding === true, { timeout: 5000 });
  const before = await mobile.evaluate(() => window.__GAME_TEST__.getState());
  await click(mobile, 34, 410);
  await mobile.waitForFunction(() => window.__GAME_TEST__.getState().practiceAssistArmed === true, { timeout: 3000 });
  await mobile.screenshot({ path: path.join(HERE, 'fix2-onboarding-funnel.png') });
  const armed = await mobile.evaluate(() => window.__GAME_TEST__.getState());
  const coords = await mobile.evaluate(() => {
    const s = window.__GAME_TEST__.getState();
    const c = document.querySelector('canvas');
    const sx = c.width / innerWidth, sy = c.height / innerHeight;
    const pt = (x, y) => ({ x: x / sx, y: innerHeight - y / sy });
    const a = pt(s.apexX, s.apexY), b = pt(s.bX, s.bY), cc = pt(s.cX, s.cY);
    const d = { x: (b.x + cc.x) / 2, y: (b.y + cc.y) / 2 };
    return { a, b, c: cc, start: { x: a.x, y: a.y - 110 }, end: { x: a.x + (d.x - a.x) * 1.20, y: a.y + (d.y - a.y) * 1.20 } };
  });
  await drag(mobile, coords.start, coords.end);
  await mobile.waitForFunction(() => window.__GAME_TEST__.getState().lastResult === 'correct', { timeout: 5000 });
  await mobile.screenshot({ path: path.join(HERE, 'fix2-onboarding-correct.png') });
  await mobile.waitForFunction(() => { const s = window.__GAME_TEST__.getState(); return !s.onboarding && s.round === 1 && s.phase === 'playing'; }, { timeout: 5000 });
  const after = await mobile.evaluate(() => window.__GAME_TEST__.getState());
  const onboarding = {
    elapsedMs: Date.now() - onboardingStart,
    sameInput: `A에서 110 CSS px 위에서 down → BC 중점 밖에서 up`,
    oldHitRadiusPx: 48,
    startDistanceFromA: Math.round(Math.hypot(coords.start.x - coords.a.x, coords.start.y - coords.a.y)),
    before: { onboarding: before.onboarding, round: before.round, invalidCount: before.invalidCount },
    armed: { practiceAssistArmed: armed.practiceAssistArmed, invalidCount: armed.invalidCount },
    after: { onboarding: after.onboarding, round: after.round, phase: after.phase, lastResult: after.lastResult },
    passed: after.onboarding === false && after.round === 1 && Date.now() - onboardingStart < 30000,
  };
  await mobile.close();

  const layouts = [];
  for (const spec of [
    { name: 'tablet-820x1180-dpr2', width: 820, height: 1180, deviceScaleFactor: 2, expectedLand: false },
    { name: 'wide-1280x700-dpr2', width: 1280, height: 700, deviceScaleFactor: 2, expectedLand: true },
  ]) {
    const page = await readyPage(browser, server.url, { width: spec.width, height: spec.height, deviceScaleFactor: spec.deviceScaleFactor }, consoleErrors);
    await page.evaluate(() => window.__GAME_TEST__.start()); await sleep(350);
    const info = await page.evaluate(() => ({ state: window.__GAME_TEST__.getState(), hook: window.__GAME_TEST__.getLayout() }));
    const s = info.state;
    const inBounds = s.hudLeft >= 0 && s.hudRight <= s.screenW && s.hudBottom >= 0 && s.hudTop <= s.screenH &&
      s.goalLeft >= 0 && s.goalRight <= s.screenW && s.goalBottom >= 0 && s.goalTop <= s.screenH &&
      s.promptLeft >= 0 && s.promptRight <= s.screenW && s.promptBottom >= 0 && s.promptTop <= s.screenH &&
      [s.apexX, s.bX, s.cX].every(x => x >= 0 && x <= s.screenW) && [s.apexY, s.bY, s.cY].every(y => y >= 0 && y <= s.screenH);
    const row = {
      name: spec.name, css: `${spec.width}x${spec.height}`, dpr: spec.deviceScaleFactor,
      render: `${s.screenW}x${s.screenH}`, hookLand: info.hook.land, unityLand: s.layoutLand,
      hud: [s.hudLeft, s.hudBottom, s.hudRight, s.hudTop], prompt: [s.promptLeft, s.promptBottom, s.promptRight, s.promptTop],
      points: { A: [s.apexX, s.apexY], B: [s.bX, s.bY], C: [s.cX, s.cY] },
      inBounds, passed: info.hook.land === spec.expectedLand && s.layoutLand === spec.expectedLand && inBounds,
    };
    layouts.push(row);
    await page.screenshot({ path: path.join(HERE, `fix2-${spec.name}.png`) });
    await page.close();
  }

  const passed = bankAudit.count === 320 && bankAudit.uniqueIds === 320 && bankAudit.allMathAndWordingValid &&
    onboarding.passed && onboarding.startDistanceFromA > onboarding.oldHitRadiusPx && layouts.every(x => x.passed) && consoleErrors.length === 0;
  const result = { generatedAt: new Date().toISOString(), artifactHashSha256: artifactHash(), bankAudit, onboarding, layouts, consoleErrors, passed };
  fs.writeFileSync(path.join(HERE, 'fix2-regression-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (!passed) process.exitCode = 1;
} finally {
  await browser.close(); await server.close();
}
