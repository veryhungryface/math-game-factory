// QA의 1280 캡처가 타이틀/로더 프레임에 걸리지 않도록 ready+playing을 명시 확인한 증거를 남긴다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const WORK = path.resolve(ROOT, process.env.MGF_WORK || 'factory/work');
const SHOT = path.join(WORK, 'qa/cable-lock/desktop-1280.png');
const OUT = path.dirname(new URL(import.meta.url).pathname);

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

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({headless: true, executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']});
const page = await browser.newPage();
await page.setViewport({width: 1280, height: 800, deviceScaleFactor: 1});
const errors = [];
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
page.on('pageerror', e => errors.push(String(e)));
await page.goto(`${server.url}/g/cable-lock/`, {waitUntil: 'load', timeout: 45000});
await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
await page.evaluate(() => window.__GAME_TEST__.start());
await page.waitForFunction(() => window.__GAME_TEST__?.getState?.().phase === 'playing', {timeout: 10000});
await new Promise(r => setTimeout(r, 900));
const state = await page.evaluate(() => ({state: window.__GAME_TEST__.getState(), layout: window.__GAME_TEST__.getLayout()}));
fs.mkdirSync(path.dirname(SHOT), {recursive: true});
await page.screenshot({path: SHOT});
const result = {
  run_id: `cable-lock-1280-playing-${new Date().toISOString()}`,
  artifact_hash_sha256: artifactHash(), screenshot: path.relative(ROOT, SHOT),
  phase: state.state.phase, layout: state.layout, errors,
  passed: state.state.phase === 'playing' && state.layout.land === true && state.layout.playW === 1280 && errors.length === 0
};
fs.writeFileSync(path.join(OUT, 'desktop-1280-results.json'), JSON.stringify(result, null, 2));
console.log(JSON.stringify(result, null, 2));
await page.close();
await browser.close();
await server.close();
if (!result.passed) process.exitCode = 1;
