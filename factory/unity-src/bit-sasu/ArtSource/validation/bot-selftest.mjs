import fs from 'node:fs/promises';
import fsSync from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(import.meta.dirname, '../../../../..');
const OUT = path.join(import.meta.dirname, 'bot-results.json');
const GAMES = Number(process.env.BOT_GAMES || 200);
const VIEW = { width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true };

function resolveChrome() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(os.homedir(), '.cache/puppeteer/chrome');
  if (!fsSync.existsSync(base)) return undefined;
  for (const build of fsSync.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome'
    ]) {
      const file = path.join(base, build, rel);
      if (fsSync.existsSync(file)) return file;
    }
  }
  return undefined;
}

const server = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--use-angle=metal', '--disable-dev-shm-usage', '--mute-audio']
});

async function readyPage() {
  const page = await browser.newPage();
  await page.setViewport(VIEW);
  await page.goto(`${server.url}/g/bit-sasu/`, { waitUntil: 'domcontentloaded' });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });
  await page.evaluate(() => window.__GAME_TEST__.start());
  await new Promise(r => setTimeout(r, 60));
  return page;
}

async function aim(page, target) {
  const before = await page.evaluate(() => window.__GAME_TEST__.getState());
  const startY = 620;
  const endY = target == null ? startY : startY - (target - before.selectedAngle) * 4;
  const touch = page.touchscreen;
  await touch.touchStart(195, startY);
  await new Promise(r => setTimeout(r, 40));
  if (target != null) {
    const midY = (startY + endY) * 0.5;
    await touch.touchMove(195, midY);
    await new Promise(r => setTimeout(r, 24));
    await touch.touchMove(195, endY);
    await new Promise(r => setTimeout(r, 40));
  }
  await touch.touchEnd();
  try {
    await page.waitForFunction(
      n => window.__GAME_TEST__.getState().shots > n,
      { timeout: 1500 }, before.shots
    );
  } catch (error) {
    const debug = await page.evaluate(() => window.__GAME_TEST__.getState());
    throw new Error(`pointer did not submit: before=${JSON.stringify(before)} after=${JSON.stringify(debug)} (${error})`);
  }
  const after = await page.evaluate(() => window.__GAME_TEST__.getState());
  return { before, after };
}

async function runPolicy(name, choose) {
  const page = await readyPage();
  let games = 0, completed = 0, first = 0, correct = 0, step = 0;
  while (games < GAMES) {
    const s = await page.evaluate(() => window.__GAME_TEST__.getState());
    const target = choose(s, step++);
    const shot = await aim(page, target);
    first += shot.after.firstAttemptTotal - shot.before.firstAttemptTotal;
    correct += shot.after.firstAttemptCorrect - shot.before.firstAttemptCorrect;
    await page.evaluate(() => window.__GAME_TEST__.start()); // 발사 연출만 완료
    await new Promise(r => setTimeout(r, 20));
    let end = await page.evaluate(() => window.__GAME_TEST__.getState());
    if (end.phase === 'clear' || end.phase === 'gameover') {
      games++;
      if (end.phase === 'clear') completed++;
      await page.evaluate(() => window.__GAME_TEST__.start());
      await new Promise(r => setTimeout(r, 20));
    }
  }
  await page.close();
  return {
    name, games, firstAttempts: first, firstCorrect: correct,
    firstAttemptRate: first ? correct / first : 0,
    completionRate: completed / games
  };
}

async function runNoInput() {
  const page = await readyPage();
  let progressed = 0;
  for (let i = 0; i < GAMES; i++) {
    const a = await page.evaluate(() => window.__GAME_TEST__.getState());
    await new Promise(r => setTimeout(r, 20));
    const b = await page.evaluate(() => window.__GAME_TEST__.getState());
    if (b.solved !== a.solved || b.firstAttemptTotal !== a.firstAttemptTotal) progressed++;
    await page.evaluate(() => window.__GAME_TEST__.start());
  }
  await page.close();
  return { name: 'no-input', games: GAMES, firstAttempts: 0, firstCorrect: 0, firstAttemptRate: 0, completionRate: 0, progressed };
}

try {
  const results = [];
  results.push(await runPolicy('repeat', () => null));
  results.push(await runPolicy('cycle', (_s, i) => [15, 35, 55, 74][i % 4]));
  let seed = 0x51a7c3;
  results.push(await runPolicy('random', () => {
    seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0;
    return 15 + (seed % 60);
  }));
  results.push(await runNoInput());
  const report = { generatedAt: new Date().toISOString(), chance: 1 / 60, gamesPerPolicy: GAMES, results };
  await fs.writeFile(OUT, JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify(report, null, 2));
} finally {
  await browser.close();
  await server.close();
}
