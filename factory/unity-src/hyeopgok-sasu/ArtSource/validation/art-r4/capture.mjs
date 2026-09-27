// Art round 4 visual evidence at the independent judge's fixed states/timings.
// Every image is an actual WebGL screenshot; the geometry fixture is served in
// memory and never edits a pack file.
//
// Usage (repo root):
//   node factory/unity-src/hyeopgok-sasu/ArtSource/validation/art-r4/capture.mjs
//   ONLY=judge,run,pour,geo node .../capture.mjs [output-tag]

import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../../lib/static-server.mjs';

const HERE = path.dirname(new URL(import.meta.url).pathname);
const ROOT = path.resolve(HERE, '../../../../../..');
const tag = process.argv[2] || 'final';
if (!/^[a-z0-9-]+$/.test(tag)) throw new Error('Invalid output tag');
const only = (process.env.ONLY || '').split(',').map(value => value.trim()).filter(Boolean);
const want = key => only.length === 0 || only.includes(key);
const OUT = path.join(HERE, tag);
const GAME = path.join(ROOT, 'public/g/hyeopgok-sasu');
const PACK_DIR = path.join(GAME, 'packs');
fs.mkdirSync(OUT, { recursive: true });

const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
const hash = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
const packHashes = () => Object.fromEntries(fs.readdirSync(PACK_DIR).filter(file => file.endsWith('.json')).sort().map(file => [file, hash(path.join(PACK_DIR, file))]));
const packsAtStart = packHashes();

function resolveChrome() {
  const explicit = process.env.PUPPETEER_EXECUTABLE_PATH;
  if (explicit && fs.existsSync(explicit)) return explicit;
  const cache = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (fs.existsSync(cache)) {
    const builds = fs.readdirSync(cache).sort((a, b) => b.localeCompare(a, undefined, { numeric: true }));
    for (const build of builds) {
      const candidate = path.join(cache, build, 'chrome-mac-arm64', 'Google Chrome for Testing.app', 'Contents', 'MacOS', 'Google Chrome for Testing');
      if (fs.existsSync(candidate) && fs.statSync(candidate).isFile()) return candidate;
    }
  }
  const bundled = puppeteer.executablePath();
  if (bundled && fs.existsSync(bundled)) return bundled;
  throw new Error('Chrome executable not found; set PUPPETEER_EXECUTABLE_PATH');
}

const server = await serveStatic(path.join(ROOT, 'public'));
const chrome = resolveChrome();
const browser = await puppeteer.launch({ headless: true, executablePath: chrome, args: ['--no-sandbox', '--mute-audio'] });
const page = await browser.newPage();
const cdp = await page.createCDPSession();
const errors = [];
const failedRequests = [];
const frames = [];
let overlay = null;
let overlayId = '';

await page.setRequestInterception(true);
page.on('request', request => {
  if (overlay && request.url().split('?')[0].endsWith(`/packs/${overlayId}.json`)) {
    request.respond({ status: 200, contentType: 'application/json', body: JSON.stringify(overlay) });
  } else request.continue();
});
page.on('pageerror', error => { if (errors.length < 30) errors.push(String(error)); });
page.on('console', message => { if (message.type() === 'error' && errors.length < 30) errors.push(message.text()); });
page.on('requestfailed', request => failedRequests.push({ url: request.url(), error: request.failure()?.errorText ?? 'unknown' }));
page.on('response', response => { if (response.status() >= 400) failedRequests.push({ url: response.url(), status: response.status() }); });

const state = () => page.evaluate(() => window.__GAME_TEST__.getState());

async function load(view, query) {
  await page.setViewport({
    width: view.width,
    height: view.height,
    deviceScaleFactor: view.mobile ? 2 : 1,
    isMobile: view.mobile,
    hasTouch: true,
  });
  await page.goto(`${server.url}/g/hyeopgok-sasu/?${query}`, { waitUntil: 'networkidle0', timeout: 90000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 90000 });
  await sleep(500);
}

async function shot(name, note = '') {
  const file = `${name}.png`;
  await page.screenshot({ path: path.join(OUT, file) });
  frames.push({ file, note, state: await state().catch(() => null) });
  console.log(`frame ${file}`);
}

async function until(predicate, timeoutMs = 15000) {
  const deadline = Date.now() + timeoutMs;
  let value;
  do {
    value = await state();
    if (predicate(value)) return value;
    await sleep(35);
  } while (Date.now() < deadline);
  throw new Error(`state wait timed out: ${JSON.stringify(value)}`);
}

async function touch(type, x, y) {
  await cdp.send('Input.dispatchTouchEvent', { type, touchPoints: type === 'touchEnd' ? [] : [{ x, y, id: 1 }] });
}

async function tapNormalized(x, y, view) {
  await touch('touchStart', x * view.width, y * view.height);
  await sleep(80);
  await touch('touchEnd', 0, 0);
  await sleep(100);
}

async function startPlaying() {
  await page.evaluate(() => window.__GAME_TEST__.start());
  await page.waitForFunction(() => window.__GAME_TEST__?.getState?.().phase === 'playing', { timeout: 10000 });
}

async function answerCorrectAndSettle(targetSolved, settleMs = 1450) {
  await page.evaluate(() => window.__GAME_TEST__.answerCorrect());
  await until(value => value.solved >= targetSolved, 8000);
  await sleep(settleMs);
}

const views = [
  { id: '390', width: 390, height: 844, mobile: true },
  { id: '1280', width: 1280, height: 800, mobile: false },
];

try {
  for (const view of views) {
    const wide = !view.mobile;
    if (want('judge')) {
      await load(view, 'pack=m2s2-u7&artprobe=1');
      await shot(`title-${view.id}`);
      // This is the real title-screen pack card, not a test command.
      const packX = (wide ? 0.23 : 0.5) - 108 * view.height / (844 * view.width);
      await tapNormalized(packX, wide ? 0.83 : 0.85, view);
      await sleep(350);
      await shot(`packs-${view.id}`);

      await load(view, 'pack=m2s2-u7&artprobe=1');
      await startPlaying();
      const startedAt = Date.now();
      await sleep(Math.max(0, startedAt + 3000 - Date.now()));
      await shot(`play-${view.id}-3s`);
      await sleep(Math.max(0, startedAt + 8000 - Date.now()));
      await shot(`play-${view.id}-8s`);
      await sleep(Math.max(0, startedAt + 15000 - Date.now()));
      await shot(`play-${view.id}-15s`);
    }

    if (want('run')) {
      await load(view, 'pack=m2s2-u7&artprobe=1');
      await startPlaying();
      await sleep(1800);
      for (let index = 1; index <= 4; index++) await answerCorrectAndSettle(index, index === 4 ? 500 : 1450);
      await shot(`play-${view.id}-after4correct`, 'four correct answers through the unchanged test contract');

      // A fraction-parts question exposes both active semantic pads.
      await load(view, 'pack=m2s2-u7&artprobe=1');
      await startPlaying();
      await sleep(1800);
      await answerCorrectAndSettle(1, 1750);
      await shot(`fraction-${view.id}`, 'second probability question with active fraction pads');

      // Complete the same public pack to capture the actual victory screen.
      for (let target = 2; target <= 10; target++) await answerCorrectAndSettle(target, target === 10 ? 1700 : 1350);
      await until(value => value.phase !== 'playing', 15000).catch(() => {});
      await sleep(600);
      await shot(`victory-${view.id}`);
    }

    if (want('pour')) {
      await load(view, 'pack=m2s2-u6&artprobe=1');
      await startPlaying();
      const funded = await until(value => value.coins >= 8, 45000);
      if (!Array.isArray(funded.padScreen) || funded.padScreen.length < 2) throw new Error('padScreen missing for pour capture');
      await touch('touchStart', funded.padScreen[0] * view.width, funded.padScreen[1] * view.height);
      await until(value => Array.isArray(value.poured) && value.poured[0] >= 2, 10000);
      await sleep(120);
      await shot(`pour-${view.id}`, 'real touch hold during multi-coin pour');
      await touch('touchEnd', 0, 0);
      await sleep(180);
    }

    if (want('geo')) {
      const pack = JSON.parse(fs.readFileSync(path.join(PACK_DIR, 'm2s2-u1.json'), 'utf8'));
      const rank = question => ((question.prompt.match(/[∠△°]/g) || []).length * 10) + (question.prompt.length > 60 && question.prompt.length < 120 ? 5 : 0);
      const selected = pack.items.reduce((best, candidate) => rank(candidate) > rank(best) ? candidate : best);
      overlay = { ...pack, items: [selected, ...pack.items.filter(item => item.id !== selected.id)] };
      overlayId = pack.pack_id;
      await load(view, 'pack=m2s2-u1&artprobe=1');
      await startPlaying();
      await sleep(1300);
      await shot(`geometry-${view.id}`, { pack: pack.pack_id, id: selected.id, prompt: selected.prompt, choices: selected.choices });
      overlay = null;
      overlayId = '';
    }
  }
} catch (error) {
  errors.push(String(error?.stack || error));
  await shot('failure', String(error)).catch(() => {});
  process.exitCode = 1;
} finally {
  const builds = fs.existsSync(path.join(GAME, 'Build'))
    ? Object.fromEntries(fs.readdirSync(path.join(GAME, 'Build')).sort().map(file => [file, hash(path.join(GAME, 'Build', file))]))
    : {};
  const packsAtEnd = packHashes();
  const report = {
    schemaVersion: 1,
    tag,
    generatedAt: new Date().toISOString(),
    chrome,
    build: builds,
    packsStable: JSON.stringify(packsAtStart) === JSON.stringify(packsAtEnd),
    frames,
    errors,
    failedRequests,
  };
  fs.writeFileSync(path.join(OUT, 'report.json'), JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify({ output: OUT, frameCount: frames.length, packsStable: report.packsStable, errors, failedRequests }, null, 2));
  await browser.close().catch(() => {});
  await server.close().catch(() => {});
  if (errors.length || failedRequests.length || !report.packsStable) process.exitCode = 1;
}

