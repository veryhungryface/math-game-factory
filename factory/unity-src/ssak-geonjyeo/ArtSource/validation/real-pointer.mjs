import fs from 'node:fs/promises';
import path from 'node:path';
import puppeteer from 'puppeteer';

const base = process.env.MGF_URL || 'http://127.0.0.1:4321/g/ssak-geonjyeo/';
const outDir = path.dirname(new URL(import.meta.url).pathname);
const cacheRoot = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
let executablePath;
for (const build of (await fs.readdir(cacheRoot)).sort().reverse()) {
  for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
  ]) {
    const candidate = path.join(cacheRoot, build, rel);
    try { await fs.access(candidate); executablePath = candidate; break; } catch {}
  }
  if (executablePath) break;
}
const browser = await puppeteer.launch({
  headless: true,
  executablePath,
  args: ['--enable-webgl', '--ignore-gpu-blocklist', '--no-sandbox'],
});

const page = await browser.newPage();
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
await page.goto(base, { waitUntil: 'networkidle0', timeout: 60_000 });
await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 30_000 });
const state = () => page.evaluate(() => window.__GAME_TEST__.getState());
const pause = ms => new Promise(resolve => setTimeout(resolve, ms));
const drag = async points => {
  await page.mouse.move(points[0][0], points[0][1]);
  await page.mouse.down();
  for (let i = 1; i < points.length; i++) {
    await page.mouse.move(points[i][0], points[i][1], { steps: 8 });
    await pause(40);
  }
  await page.mouse.up();
};

const result = { title: await state() };
await page.screenshot({ path: path.join(outDir, 'title-pointer.png') });
await page.mouse.move(195, 620);
await page.mouse.down();
await pause(180);
await page.mouse.up();
await page.waitForFunction(() => window.__GAME_TEST__.getState().onboarding === true, { timeout: 10_000 });
await pause(800);
result.practice = await state();
await page.screenshot({ path: path.join(outDir, 'practice-pointer.png') });

// Mobile portrait layout: capacity shell sits on the left end of the pearl rail.
await drag([[24, 568], [32, 568], [39, 568], [45, 568]]);
await pause(350);
result.afterCapacity = await state();
await page.screenshot({ path: path.join(outDir, 'capacity-two.png') });

// The sweep must start on the gold handle, then cross only (1,1) and (1,2).
await drag([[375, 568], [382, 440], [382, 300], [270, 350], [120, 350]]);
await pause(2_100);
result.afterSweep = await state();
await page.screenshot({ path: path.join(outDir, 'after-practice-sweep.png') });

// State-machine invariant: one ordinary first-attempt mistake must still leave
// a real path to the declared 5/7 mastery target and seven solved tides.
await page.evaluate(() => window.__GAME_TEST__.answerWrong());
result.afterFirstWrong = await state();
for (let i = 0; i < 7; i++) await page.evaluate(() => window.__GAME_TEST__.answerCorrect());
await pause(1_750);
result.afterRecovery = await state();

result.ok = result.afterCapacity.capacity === 2
  && !result.afterSweep.onboarding && result.afterSweep.tide === 1
  && result.afterFirstWrong.lives === 2
  && result.afterRecovery.phase === 'clear'
  && result.afterRecovery.solved === 7
  && result.afterRecovery.firstAttemptCorrect === 6;
await fs.writeFile(path.join(outDir, 'real-pointer-result.json'), JSON.stringify(result, null, 2) + '\n');
await browser.close();
if (!result.ok) process.exitCode = 1;
