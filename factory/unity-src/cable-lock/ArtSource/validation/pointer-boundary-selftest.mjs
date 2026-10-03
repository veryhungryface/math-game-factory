// 1·24·25·48 m 경계값을 실제 pointer down/move/up으로 제출한다.
// 각 목표가 정답인 본판을 화면 발문으로 찾아, 선택값과 실제 정오 판정이 함께 맞는지 확인한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const OUT = path.dirname(new URL(import.meta.url).pathname);
const TARGETS = [1, 24, 25, 48];
const sleep = ms => new Promise(r => setTimeout(r, ms));

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const b of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
}

function solve(prompt) {
  const angle = Number((prompt.match(/∠A=(\d+)°/) || prompt.match(/올려본각이 (\d+)°/))[1]);
  const value = s => {
    const m = s.match(/(\d*)√(\d+)/);
    return m ? (m[1] ? Number(m[1]) : 1) * Math.sqrt(Number(m[2])) : Number(s);
  };
  const tan = Math.tan(angle * Math.PI / 180), sin = Math.sin(angle * Math.PI / 180), cos = Math.cos(angle * Math.PI / 180);
  let m;
  if ((m = prompt.match(/(이웃변|빗변|대변) [A-C]{2}=([\d√]+) m/))) {
    const given = m[1], v = value(m[2]), asked = prompt.match(/(대변|이웃변) 케이블/)[1];
    if (given === '이웃변' && asked === '대변') return Math.round(v * tan);
    if (given === '대변' && asked === '이웃변') return Math.round(v / tan);
    if (given === '빗변' && asked === '대변') return Math.round(v * sin);
    if (given === '빗변' && asked === '이웃변') return Math.round(v * cos);
  }
  if ((m = prompt.match(/수평으로 ([\d√]+) m/))) return Math.round(value(m[1]) * tan);
  if ((m = prompt.match(/높이가 ([\d√]+) m/))) return Math.round(value(m[1]) / tan);
  throw new Error(`unparsed prompt: ${prompt}`);
}

function artifactHash() {
  const dir = path.join(PUBLIC, 'g/cable-lock');
  const files = [];
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
async function submit(page, length) {
  await page.mouse.move(195, yFor(length));
  await page.mouse.down();
  await page.mouse.move(195, yFor(length) + (length === 1 ? -4 : 4), {steps: 4});
  await sleep(50);
  await page.mouse.up();
  await sleep(100);
}

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
for (const target of TARGETS) {
  let before;
  for (let tries = 1; tries <= 1200; tries++) {
    before = await page.evaluate(() => { window.__GAME_TEST__.start(); window.__GAME_TEST__.start(); return window.__GAME_TEST__.getState(); });
    if (solve(before.prompt) === target) { before.tries = tries; break; }
  }
  if (solve(before.prompt) !== target) throw new Error(`target ${target} not found`);
  await submit(page, target);
  const after = await page.evaluate(() => window.__GAME_TEST__.getState());
  rows.push({target, tries: before.tries, prompt: before.prompt, start: before.selectedLength,
    selected: after.selectedLength, firstAttemptTotal: after.firstAttemptTotal,
    firstAttemptCorrect: after.firstAttemptCorrect,
    passed: after.selectedLength === target && after.firstAttemptTotal === 1 && after.firstAttemptCorrect === 1});
}

const result = {
  run_id: `cable-lock-boundary-${new Date().toISOString()}`,
  artifact_hash_sha256: artifactHash(),
  method: 'displayed prompt solved independently; real Chrome pointer down/move/up; no answer hooks',
  rows, errors,
  passed: rows.every(r => r.passed) && errors.length === 0
};
fs.writeFileSync(path.join(OUT, 'pointer-boundary-results.json'), JSON.stringify(result, null, 2));
console.log(JSON.stringify(result, null, 2));
await page.close();
await browser.close();
await server.close();
if (!result.passed) process.exitCode = 1;
