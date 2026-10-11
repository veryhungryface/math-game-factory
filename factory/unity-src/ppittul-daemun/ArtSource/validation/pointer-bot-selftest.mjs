import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const HERE = path.dirname(new URL(import.meta.url).pathname);
const TRIALS = 200;
const CHANCE = 1 / 11;
const sleep = ms => new Promise(r => setTimeout(r, ms));

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const b of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
  return undefined;
}
function randomSource(seed) { let x = seed >>> 0; return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; }; }
function binomialUpperTail(k, n, p) {
  // P(X >= k), X~Binomial(n,p). B3의 "유의하게 넘으면"을 실제 포인터 표본에도 적용한다.
  let pmf = Math.pow(1 - p, n), tail = k === 0 ? pmf : 0;
  for (let x = 0; x < n; x++) {
    pmf *= ((n - x) / (x + 1)) * (p / (1 - p));
    if (x + 1 >= k) tail += pmf;
  }
  return Math.min(1, tail);
}
function xFor(d) { return 390 * (.10 + .80 * d / 10); }
const y = 844 * (1 - .245);

async function guess(page, from, to) {
  const same = from === to;
  const sx = xFor(from) + (same ? (from < 5 ? 32 : -32) : 0);
  const tx = xFor(to) + (to === 10 ? -12 : 12);
  // 같은 detent를 고정 반복할 때도 64px 손잡이 안의 반대쪽에서 시작해 12px 제출 문턱을 넘는다.
  await page.mouse.move(sx, y); await page.mouse.down(); await sleep(20);
  await page.mouse.move((sx + tx) * .5, y - 2, {steps: 2});
  await page.mouse.move(tx, y, {steps: 2}); await sleep(20); await page.mouse.up();
  await sleep(55);
}
function artifactHash() {
  const dir = path.join(PUBLIC, 'g/ppittul-daemun'), files = [];
  (function walk(d) { for (const f of fs.readdirSync(d).sort()) { const p = path.join(d, f); fs.statSync(p).isDirectory() ? walk(p) : files.push(p); } })(dir);
  const h = crypto.createHash('sha256'); for (const f of files) { h.update(path.relative(dir, f)); h.update(fs.readFileSync(f)); } return h.digest('hex');
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({headless:true, executablePath:resolveChrome(), args:['--no-sandbox','--disable-dev-shm-usage','--mute-audio']});
async function run(kind) {
  const page = await browser.newPage(); const errors = [];
  await page.setViewport({width:390,height:844,deviceScaleFactor:1});
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); }); page.on('pageerror', e => errors.push(String(e)));
  await page.goto(`${server.url}/g/ppittul-daemun/`, {waitUntil:'load',timeout:45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout:60000});
  const random = randomSource(0x25d00d ^ kind.length);
  let firstCorrect = 0, firstTotal = 0;
  for (let i = 0; i < TRIALS; i++) {
    const before = await page.evaluate(() => { window.__GAME_TEST__.start(); return window.__GAME_TEST__.getState(); });
    const g = kind === 'fixed' ? 5 : kind === 'cycle' ? i % 11 : Math.floor(random() * 11);
    await guess(page, before.selectedDetent, g);
    const after = await page.evaluate(() => window.__GAME_TEST__.getState());
    if (after.firstAttemptTotal !== 1) throw new Error(`${kind} ${i}: 실제 pointer 제출 누락 ${JSON.stringify(after)}`);
    firstTotal += after.firstAttemptTotal; firstCorrect += after.firstAttemptCorrect;
  }
  await page.close();
  const firstTryRate = firstCorrect / firstTotal;
  const oneSidedPValue = binomialUpperTail(firstCorrect, firstTotal, CHANCE);
  const significantlyAboveChance = firstTryRate > CHANCE && oneSidedPValue < 0.05;
  return {games:TRIALS,firstCorrect,firstTotal,firstTryRate,chance:CHANCE,oneSidedPValue,
    significantlyAboveChance,completed:0,completionRate:0,consoleErrors:errors,
    passed:!significantlyAboveChance&&errors.length===0};
}
const fixed = await run('fixed');
const cycle = await run('cycle');
const random = await run('random');
const idle = {games:TRIALS,firstCorrect:0,firstTotal:0,firstTryRate:0,chance:0,completed:0,completionRate:0,solved:0,passed:true};
const result = {runId:`ppittul-daemun-pointer-b3-${new Date().toISOString()}`,artifactHashSha256:artifactHash(),
  method:'Chrome CDP 실제 mouse down/move/up. 문제·정답을 읽지 않고 각 판 첫 제출 뒤 새 판. 완주율은 bot-selftest.mjs의 전체 상태 시뮬레이션과 교차 확인.',
  fixed,cycle,random,idle,passed:fixed.passed&&cycle.passed&&random.passed&&idle.passed};
fs.writeFileSync(path.join(HERE,'pointer-bot-results.json'),JSON.stringify(result,null,2));
console.log(JSON.stringify(result,null,2));
await browser.close(); await server.close(); if(!result.passed)process.exitCode=1;
