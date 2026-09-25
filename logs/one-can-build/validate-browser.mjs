import assert from 'node:assert/strict';
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import puppeteer from 'puppeteer';

const root = path.resolve('public/g/one-can');
const out = path.resolve('logs/one-can-build/browser-playthrough');
fs.mkdirSync(out, { recursive: true });
const executablePath =
  '/Users/sitpo/Library/Caches/ms-playwright/chromium_headless_shell-1234/' +
  'chrome-headless-shell-mac-arm64/chrome-headless-shell';
const report = {
  generatedAt: new Date().toISOString(),
  engineSha256: crypto.createHash('sha256').update(fs.readFileSync(path.join(root, 'engine.js'))).digest('hex'),
  environment: 'Real Chromium file:// run with Puppeteer pointer events; single-process sandbox fallback.',
  errors: [],
  requests: [],
  runs: [],
};

const browser = await puppeteer.launch({
  headless: 'shell',
  pipe: true,
  executablePath,
  args: [
    '--no-sandbox', '--single-process', '--no-zygote', '--disable-gpu',
    '--disable-software-rasterizer', '--disable-dev-shm-usage',
    '--disable-breakpad', '--disable-crash-reporter',
    '--disable-features=Vulkan,UseChromeOSDirectVideoDecoder,OptimizationHints,MediaRouter',
  ],
});

try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  page.on('console', (m) => { if (m.type() === 'error') report.errors.push('console: ' + m.text()); });
  page.on('pageerror', (e) => report.errors.push('page: ' + String(e)));
  page.on('requestfailed', (r) => report.errors.push('request: ' + r.url() + ' — ' + r.failure()?.errorText));
  page.on('request', (r) => report.requests.push(r.url()));
  await page.goto(pathToFileURL(path.join(root, 'index.html')).href, { waitUntil: 'load', timeout: 15000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 10000 });

  const state = () => page.evaluate(() => window.__GAME_TEST__.getState());
  const shot = (name) => page.screenshot({ path: path.join(out, name) });
  const waitAnimation = async () => {
    await new Promise((resolve) => setTimeout(resolve, 80));
    for (let i = 0; i < 100; i++) {
      const current = await state();
      if (!current.animating) return;
      await new Promise((resolve) => setTimeout(resolve, 80));
    }
    throw new Error('animation did not settle: ' + JSON.stringify(await state()));
  };
  const clickTarget = async (index) => {
    const targets = await page.evaluate(() => window.__GAME_TEST__.getInputTargets());
    assert(targets[index], `missing visible target ${index}`);
    await page.mouse.click(targets[index].x, targets[index].y);
    await waitAnimation();
  };
  const correctIndex = (s) => {
    const capacity = Math.min(1000, s.problem.targetMl - s.orderShipped * 1000);
    const need = capacity - s.canMl;
    const exact = s.rail.findIndex((c) => c.ml === need);
    if (exact >= 0) return exact;
    let best = -1;
    s.rail.forEach((c, i) => {
      if (c.ml <= need && s.dropsLeft > 1 && (best < 0 || c.ml > s.rail[best].ml)) best = i;
    });
    return best;
  };
  const solveToEnd = async (label, maxInputs = 80) => {
    const before = await state();
    const milestones = new Set();
    let inputs = 0;
    while ((await state()).phase === 'playing' && inputs < maxInputs) {
      const s = await state();
      const index = correctIndex(s);
      assert(index >= 0, `${label}: no productive rail cup for ${s.problem.id}`);
      const prior = JSON.stringify([s.score, s.lives, s.solved, s.shipped, s.canMl, s.dropsLeft]);
      await clickTarget(index);
      const after = await state();
      const changed = JSON.stringify([after.score, after.lives, after.solved, after.shipped, after.canMl, after.dropsLeft]);
      assert.notEqual(changed, prior, `${label}: real pointer ${inputs} did not change play state`);
      inputs++;
      if ((after.shipped === 4 || after.shipped === 7) && !milestones.has(after.shipped)) {
        milestones.add(after.shipped);
        await shot(`${label}-shipped-${after.shipped}.png`);
      }
    }
    const after = await state();
    assert.equal(after.phase, 'clear', `${label}: did not reach clear`);
    assert(after.shipped >= 8, `${label}: clear before eight shipped cans`);
    await shot(`${label}-clear.png`);
    return { before, after, inputs };
  };

  await shot('00-title.png');
  const startRect = await page.evaluate(() => {
    const r = document.getElementById('startButton').getBoundingClientRect();
    return { x: r.left + r.width / 2, y: r.top + r.height / 2 };
  });
  await page.mouse.click(startRect.x, startRect.y);
  await new Promise((resolve) => setTimeout(resolve, 100));
  let s = await state();
  assert(s.frozen && s.problem.id === 'tutorial-500-500');

  await page.mouse.click(20, 420);
  await new Promise((resolve) => setTimeout(resolve, 120));
  const refusal = await page.evaluate(() => document.getElementById('feedback').textContent);
  assert(/컵|레일/.test(refusal), 'invalid tap did not explain the real target');
  await shot('01-invalid-tap.png');

  await clickTarget(0);
  s = await state();
  assert.equal(s.canMl, 500);
  assert.equal(s.frozen, true);
  await shot('02-tutorial-half.png');
  await clickTarget(0);
  s = await state();
  assert.equal(s.frozen, false);
  assert.equal(s.shipped, 1);
  await shot('03-tutorial-lid.png');
  report.runs.push({ name: 'normal-real-pointer', ...(await solveToEnd('normal')) });

  await page.evaluate(() => window.__GAME_TEST__.start());
  await new Promise((resolve) => setTimeout(resolve, 800));
  s = await state();
  const capacity = Math.min(1000, s.problem.targetMl - s.orderShipped * 1000);
  const free = capacity - s.canMl;
  const wrongIndex = s.rail.findIndex((c) => c.ml > free);
  assert(wrongIndex >= 0, 'no overflow cup available for recovery run');
  await clickTarget(wrongIndex);
  s = await state();
  assert.equal(s.lives, 2);
  assert.equal(s.phase, 'playing');
  await shot('recovery-first-overflow.png');
  report.runs.push({ name: 'first-error-recovery-real-pointer', ...(await solveToEnd('recovery')) });

  report.final = await state();
  report.refusal = refusal;
  report.horizontalOverflow = await page.evaluate(() => document.documentElement.scrollWidth > innerWidth);
  report.externalRequests = report.requests.filter((u) => !u.startsWith('file://'));
  assert.equal(report.horizontalOverflow, false);
  assert.deepEqual(report.externalRequests, []);
  assert.deepEqual(report.errors, []);
} finally {
  await browser.close();
  fs.writeFileSync(path.join(out, 'report.json'), JSON.stringify(report, null, 2) + '\n');
}

console.log(JSON.stringify(report, null, 2));
