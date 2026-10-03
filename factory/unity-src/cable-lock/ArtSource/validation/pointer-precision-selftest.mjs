// 확대 스냅 경계 정밀도: 1·2·24·25·47·48 m를 각각 20회 실제 포인터로 놓는다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const OUT = path.dirname(new URL(import.meta.url).pathname);
const TARGETS = [1, 2, 24, 25, 47, 48];
const REPEATS = 20;
const sleep = ms => new Promise(r => setTimeout(r, ms));

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const b of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
}

function artifactHash() {
  const dir = path.join(PUBLIC, 'g/cable-lock'), files = [];
  (function walk(d) {
    for (const f of fs.readdirSync(d).sort()) {
      const p = path.join(d, f);
      if (fs.statSync(p).isDirectory()) walk(p); else files.push(p);
    }
  })(dir);
  const h = crypto.createHash('sha256');
  for (const f of files) { h.update(path.relative(dir, f)); h.update(fs.readFileSync(f)); }
  return h.digest('hex');
}

function yFor(length) { return 731 - (length - 1) * (300 / 47); }
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({headless: true, executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']});
const page = await browser.newPage();
await page.setViewport({width: 390, height: 844, deviceScaleFactor: 1});
const errors = [];
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
page.on('pageerror', e => errors.push(String(e)));
await page.goto(`${server.url}/g/cable-lock/`, {waitUntil: 'load', timeout: 45000});
await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});

const rows = [];
let captured = false;
for (const target of TARGETS) {
  let misses = 0;
  const selected = [];
  for (let i = 0; i < REPEATS; i++) {
    await page.evaluate(() => { window.__GAME_TEST__.start(); window.__GAME_TEST__.start(); });
    await page.mouse.move(195, yFor(target));
    await page.mouse.down();
    await page.mouse.move(195, yFor(target) + (target === 1 ? -4 : 4), {steps: 4});
    await sleep(30);
    if (!captured && target === 25) {
      await page.screenshot({path: path.join(OUT, 'precision-magnifier-held.png')});
      captured = true;
    }
    await page.mouse.up();
    await sleep(45);
    const value = await page.evaluate(() => window.__GAME_TEST__.getState().selectedLength);
    selected.push(value);
    if (value !== target) misses++;
  }
  rows.push({target, attempts: REPEATS, misses, missRate: misses / REPEATS, selected});
}

const result = {
  run_id: `cable-lock-pointer-precision-${new Date().toISOString()}`,
  artifact_hash_sha256: artifactHash(),
  method: 'real Chrome pointer down/move/up at mobile 390x844; held-frame magnifier captured',
  rows, totalAttempts: TARGETS.length * REPEATS,
  totalMisses: rows.reduce((n, r) => n + r.misses, 0), errors,
  passed: rows.every(r => r.misses === 0) && errors.length === 0
};
fs.writeFileSync(path.join(OUT, 'pointer-precision-results.json'), JSON.stringify(result, null, 2));
console.log(JSON.stringify({...result, rows: rows.map(({selected, ...r}) => r)}, null, 2));
await page.close();
await browser.close();
await server.close();
if (!result.passed) process.exitCode = 1;
