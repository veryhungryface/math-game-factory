import fs from 'node:fs/promises';
import fsSync from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(import.meta.dirname, '../../../../..');
const OUT = path.join(import.meta.dirname, 'playthrough-results.json');
const sleep = ms => new Promise(r => setTimeout(r, ms));

function resolveChrome() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(os.homedir(), '.cache/puppeteer/chrome');
  if (!fsSync.existsSync(base)) return undefined;
  for (const build of fsSync.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome'
  ]) {
    const file = path.join(base, build, rel);
    if (fsSync.existsSync(file)) return file;
  }
}

const server = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--use-angle=metal', '--disable-dev-shm-usage', '--mute-audio']
});

async function pageAtTitle() {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  await page.goto(`${server.url}/g/bit-sasu/`, { waitUntil: 'domcontentloaded' });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });
  return page;
}

async function touchTap(page, x, y) {
  await page.touchscreen.touchStart(x, y); await sleep(45); await page.touchscreen.touchEnd();
}

async function aim(page, target) {
  const before = await page.evaluate(() => window.__GAME_TEST__.getState());
  const y0 = 620, y1 = y0 - (target - before.selectedAngle) * 4;
  await page.touchscreen.touchStart(195, y0); await sleep(35);
  await page.touchscreen.touchMove(195, (y0 + y1) / 2); await sleep(24);
  await page.touchscreen.touchMove(195, y1); await sleep(35);
  await page.touchscreen.touchEnd();
  await page.waitForFunction(n => window.__GAME_TEST__.getState().shots > n, { timeout: 1500 }, before.shots);
  const after = await page.evaluate(() => window.__GAME_TEST__.getState());
  if (after.selectedAngle !== target) throw new Error(`angle input ${after.selectedAngle} != ${target}`);
  return after;
}

async function finishShot(page) {
  await page.evaluate(() => window.__GAME_TEST__.start());
  await sleep(25);
  return page.evaluate(() => window.__GAME_TEST__.getState());
}

function answerOf(s) {
  const a = Number(String(s.problemId).split('-')[2]);
  if (!Number.isInteger(a) || a < 15 || a > 74) throw new Error(`bad problem id ${s.problemId}`);
  return a;
}

async function runPerfect(page) {
  for (let i = 0; i < 9; i++) {
    const s = await page.evaluate(() => window.__GAME_TEST__.getState());
    await aim(page, answerOf(s));
    await finishShot(page);
  }
  return page.evaluate(() => window.__GAME_TEST__.getState());
}

async function runRecovery(page) {
  let s = await page.evaluate(() => window.__GAME_TEST__.getState());
  const answer = answerOf(s);
  await aim(page, answer === 74 ? 73 : answer + 1);
  s = await finishShot(page);
  await aim(page, answerOf(s));
  await finishShot(page);
  for (let i = 1; i < 9; i++) {
    s = await page.evaluate(() => window.__GAME_TEST__.getState());
    await aim(page, answerOf(s));
    await finishShot(page);
  }
  return page.evaluate(() => window.__GAME_TEST__.getState());
}

try {
  const onboardingPage = await pageAtTitle();
  await touchTap(onboardingPage, 195, 785);
  await onboardingPage.waitForFunction(() => window.__GAME_TEST__.getState().onboarding === true, { timeout: 1500 });
  await aim(onboardingPage, 45);
  await onboardingPage.waitForFunction(() => {
    const s = window.__GAME_TEST__.getState();
    return s.onboarding === false && s.presentedCount === 1;
  }, { timeout: 2500 });
  const onboarding = await onboardingPage.evaluate(() => window.__GAME_TEST__.getState());
  await onboardingPage.close();

  const perfectPage = await pageAtTitle();
  await perfectPage.evaluate(() => window.__GAME_TEST__.start());
  const perfect = await runPerfect(perfectPage);
  await perfectPage.close();

  const recoveryPage = await pageAtTitle();
  await recoveryPage.evaluate(() => window.__GAME_TEST__.start());
  const recovery = await runRecovery(recoveryPage);
  await recoveryPage.close();

  if (perfect.phase !== 'clear' || perfect.solved !== 9 || perfect.firstAttemptCorrect !== 9) throw new Error(`perfect path failed ${JSON.stringify(perfect)}`);
  if (recovery.phase !== 'clear' || recovery.solved !== 9 || recovery.firstAttemptCorrect !== 8 || recovery.lives !== 2) throw new Error(`recovery path failed ${JSON.stringify(recovery)}`);
  const report = {
    generatedAt: new Date().toISOString(),
    onboarding: { passed: !onboarding.onboarding && onboarding.presentedCount === 1 && onboarding.firstAttemptTotal === 0 },
    perfect: { phase: perfect.phase, solved: perfect.solved, firstAttemptCorrect: perfect.firstAttemptCorrect, lives: perfect.lives },
    recovery: { phase: recovery.phase, solved: recovery.solved, firstAttemptCorrect: recovery.firstAttemptCorrect, lives: recovery.lives },
    result: 'pass'
  };
  await fs.writeFile(OUT, JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify(report, null, 2));
} finally {
  await browser.close();
  await server.close();
}
