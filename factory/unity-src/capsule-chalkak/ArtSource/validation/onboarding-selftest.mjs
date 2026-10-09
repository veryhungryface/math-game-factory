import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const OUT = path.join(HERE, 'onboarding');
fs.mkdirSync(OUT, { recursive: true });
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

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

async function state(page) {
  return page.evaluate(() => window.__GAME_TEST__.getState());
}

async function waitFor(page, predicate, timeout = 4000) {
  const end = Date.now() + timeout;
  let value;
  while (Date.now() < end) {
    value = await state(page);
    if (predicate(value)) return value;
    await sleep(40);
  }
  throw new Error(`state timeout: ${JSON.stringify(value)}`);
}

async function clickAt(page, point) {
  await page.mouse.move(point[0], point[1]);
  await sleep(60);
  await page.mouse.down();
  await sleep(80);
  await page.mouse.up();
  await sleep(100);
}

async function chain(page, value, indices) {
  const point = index => [value.tilePx[index * 2], value.tilePx[index * 2 + 1]];
  await page.mouse.move(...point(indices[0]));
  await sleep(60);
  await page.mouse.down();
  await sleep(70);
  for (let i = 1; i < indices.length; i++) {
    await page.mouse.move(...point(indices[i]));
    await sleep(70);
  }
  await page.mouse.up();
  await sleep(100);
}

async function pullLever(page, value) {
  const x = value.controlPx[0], y = value.controlPx[1];
  await page.mouse.move(x, y);
  await sleep(70);
  await page.mouse.down();
  await sleep(70);
  await page.mouse.move(x, Math.min(value.screenH - 4, y + 260), { steps: 8 });
  await sleep(70);
  const held = await state(page);
  if (held.leverPullRatio < .99) {
    await page.screenshot({ path: path.join(OUT, 'debug-lever-held.png') });
    await page.mouse.up();
    throw new Error(`lever threshold not reached: ${JSON.stringify(held)}`);
  }
  await page.mouse.up();
  return waitFor(page, next => next.phase === 'reveal', 2000);
}

const errors = [];
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox'] });
try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('pageerror', error => errors.push(String(error)));
  await page.goto(`${server.url}/g/capsule-chalkak/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 20000 });

  const title = await state(page);
  await page.screenshot({ path: path.join(OUT, '00-title.png') });
  await clickAt(page, [title.controlPx[4], title.controlPx[5]]);
  const practice = await waitFor(page, value => value.phase === 'practice' && value.onboarding && value.frozen);
  await page.screenshot({ path: path.join(OUT, '01-practice-guide.png') });

  // 빈 곳도 무반응이 아니라 포인터 버전과 수압 파문/안내가 즉시 변한다.
  const pointerBefore = practice.pointerVersion;
  await clickAt(page, [195, 700]);
  const refused = await state(page);
  if (refused.pointerVersion <= pointerBefore || refused.selectedCount !== 0 || refused.attempts !== 0) {
    throw new Error(`blank tap feedback failed: ${JSON.stringify(refused)}`);
  }
  await page.screenshot({ path: path.join(OUT, '02-blank-feedback.png') });

  await chain(page, refused, [2, 3]);
  let selected = await state(page);
  if (selected.selectedCount !== 2) throw new Error(`practice chain failed: ${JSON.stringify(selected)}`);
  const reveal = await pullLever(page, selected);
  if (reveal.score <= 0 || reveal.lives !== 3) throw new Error(`practice reveal failed: ${JSON.stringify(reveal)}`);
  await page.screenshot({ path: path.join(OUT, '03-practice-reveal.png') });
  const playing = await waitFor(page, value => value.phase === 'playing' && !value.onboarding && !value.frozen, 3500);
  await page.screenshot({ path: path.join(OUT, '04-first-order.png') });

  const output = {
    generatedAt: new Date().toISOString(),
    viewport: '390x844',
    input: 'actual Puppeteer mouse down/move/up only',
    title,
    practice,
    refused,
    reveal,
    playing,
    consoleErrors: errors,
    passed: errors.length === 0,
  };
  fs.writeFileSync(path.join(HERE, 'onboarding-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify({ passed: output.passed, phases: [title.phase, practice.phase, reveal.phase, playing.phase], selected: reveal.selectedCount, score: reveal.score, consoleErrors: errors }, null, 2));
  if (!output.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
