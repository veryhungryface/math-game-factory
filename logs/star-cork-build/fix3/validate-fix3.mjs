// Round-3 browser verification. Real pointer events only; the test hook is used
// for reading state, never for scoring.
import puppeteer from 'puppeteer';
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { serveStatic } from '../../../factory/lib/static-server.mjs';

const OUT = 'logs/star-cork-build/fix3';
fs.mkdirSync(OUT, { recursive: true });
function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const b of fs.readdirSync(base).sort().reverse())
    for (const rel of ['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
                       'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing']) {
      const p = path.join(base, b, rel);
      if (fs.existsSync(p)) return p;
    }
}
const sleep = ms => new Promise(r => setTimeout(r, ms));
const results = [];
const check = (name, pass, detail) => { results.push({ name, pass, detail }); console.log((pass ? 'PASS ' : 'FAIL ') + name, JSON.stringify(detail)); };

const srv = await serveStatic('public');
const url = `${srv.url}/g/star-cork/`;
const browser = await puppeteer.launch({ executablePath: resolveChrome(), headless: 'new' });

async function open() {
  const page = await browser.newPage();
  const errs = [];
  page.on('console', m => { if (m.type() === 'error') errs.push(m.text()); });
  page.on('pageerror', e => errs.push(String(e)));
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  await page.goto(url, { waitUntil: 'networkidle2' });
  await page.evaluate(() => localStorage.clear());
  await page.reload({ waitUntil: 'networkidle2' });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 20000 });
  // every pointerdown the page sees must be a trusted, browser-generated event
  await page.evaluate(() => { window.__untrusted = 0; document.addEventListener('pointerdown', e => { if (!e.isTrusted) window.__untrusted++; }, true); });
  return { page, errs };
}
const tap = async (page, x, y, settle = 90) => {
  await page.mouse.move(x, y); await page.mouse.down(); await sleep(16); await page.mouse.up(); await sleep(settle);
};
// Belt/plate geometry mirrored from index.html positions().
const geom = page => page.evaluate(() => {
  const s = window.__GAME_TEST__.getState();
  const land = document.documentElement.classList.contains('land');
  const W = Math.min(Math.max(innerWidth, 320), 1280), H = Math.max(innerHeight, 420);
  const short = H < 760 && !land;
  const fr = land ? 46 : 24;
  const beltY = land ? H * .35 : (short ? 244 : 267);
  const legend = land ? { x: fr, y: 118, w: Math.min(460, W * .4), h: 76 }
                      : { x: fr, y: short ? 119 : 146, w: W - fr * 2, h: 61 };
  const plate = land ? { x: W * .47, y: H * .65, w: Math.min(W * .50, W - fr - W * .47), h: 128 }
                     : { x: fr, y: H * .77, w: W - fr * 2, h: short ? 88 : Math.min(105, H * .14) };
  return {
    W, H, land, legend, plate,
    belt: s.candies.filter(c => c.status === 'belt' && c.progress >= 0 && c.progress <= 1)
             .map(c => ({ id: c.id, type: c.type, progress: c.progress, x: 30 + c.progress * (W - 60), y: beltY })),
    plateIds: s.plate.slice(),
    types: Object.fromEntries(s.candies.map(c => [c.id, c.type])),
    phase: s.phase, frozen: s.frozen, tutorial: s.tutorial, lives: s.lives, solved: s.solved,
    score: s.score, level: s.level, attempt: s.attempt, orderIndex: s.orderIndex,
    problem: s.problem && { id: s.problem.id, target: s.problem.target, legendBig: s.problem.legendBig, solution: s.problem.solution.slice(), type: s.problem.type },
  };
});
const countOf = arr => arr.reduce((c, t) => (c[t] = (c[t] || 0) + 1, c), { big: 0, small: 0, half: 0 });

// Clear the frozen practice board with real taps on the three stars.
async function clearPractice(page) {
  for (let guard = 0; guard < 40; guard++) {
    const g = await geom(page);
    if (!g.frozen) return true;
    const c = g.belt[0];
    if (!c) { await sleep(200); continue; }
    await tap(page, c.x, c.y);
  }
  return false;
}
// Play one order correctly with real taps. Adds AND removes, so a repair order
// (which starts with a pre-loaded plate) is handled the way a child must.
async function playOrder(page, log, { overfill = false } = {}) {
  const start = Date.now();
  const g0 = await geom(page);
  const key0 = `${g0.orderIndex}:${g0.attempt}`;
  while (Date.now() - start < 45000) {
    const g = await geom(page);
    if (g.phase === 'won' || g.phase === 'lost') return g.phase;
    if (g.phase !== 'playing') { await sleep(100); continue; }
    if (`${g.orderIndex}:${g.attempt}` !== key0) { log.push({ order: g0.orderIndex, id: g0.problem.id, target: g0.problem.target, next: g.solved }); return 'next'; }
    const need = countOf(g.problem.solution);
    // The deliberate mistake has to survive to the wave judgement, so in this
    // mode the bot loads one star MORE than the order and never takes it back.
    if (overfill) { const extra = g.belt.some(c => c.type === 'small') ? 'small' : 'big'; need[extra] = (need[extra] || 0) + 1; }
    const heldTypes = g.plateIds.map(id => g.types[id]);
    const held = countOf(heldTypes);
    const overIdx = overfill ? -1 : heldTypes.findIndex((t, i) => heldTypes.slice(0, i + 1).filter(x => x === t).length > (need[t] || 0));
    if (overIdx >= 0) {
      const n = g.plateIds.length;
      const pitch = Math.min(55, Math.max(44, (g.plate.w - 26) / Math.max(1, n)));
      const rowW = n * pitch, sx = g.plate.x + Math.max(22, (g.plate.w - rowW) / 2 + pitch / 2);
      await tap(page, sx + overIdx * pitch, g.plate.y + g.plate.h * .42, 70);
      continue;
    }
    const short = t => (held[t] || 0) < (need[t] || 0);
    const cands = g.belt.filter(c => short(c.type) && c.progress > .08 && c.progress < .90)
                        .sort((a, b) => b.progress - a.progress);      // grab the one about to leave
    if (!cands.length) { await sleep(110); continue; }
    await tap(page, cands[0].x + 6, cands[0].y, 60);
  }
  return 'timeout';
}
async function playSession(page, { mistakeOnFirst = false } = {}) {
  const log = [];
  await page.click('#start');
  await sleep(500);
  await clearPractice(page);
  await sleep(600);
  let first = true;
  for (let i = 0; i < 60; i++) {
    const g = await geom(page);
    if (g.phase === 'won' || g.phase === 'lost') break;
    const r = await playOrder(page, log, { overfill: mistakeOnFirst && first });
    first = false;
    if (r === 'won' || r === 'lost' || r === 'timeout') break;
    await sleep(120);
  }
  const g = await geom(page);
  return { ...g, log };
}

// ---- A. regression guard: offset taps still load the practice stars ----
{
  const { page, errs } = await open();
  await page.click('#start'); await sleep(900);
  const g = await geom(page);
  for (const c of g.belt) await tap(page, c.x + 40, c.y);   // 40px out: past the old 34px radius
  const st = await geom(page);
  check('practice: taps 40px off centre still load all three stars', st.plateIds.length === 3,
    { plate: st.plateIds.length, stars: g.belt.length });
  check('A: console errors 0', errs.length === 0, errs.slice(0, 3));
  await page.close();
}

// ---- B. the 100/10 legend never blocks shooting (no unjudged mandatory tap) ----
{
  const { page, errs } = await open();
  await page.click('#start'); await sleep(400);
  const jump = await page.evaluate(() => {
    const t = window.__GAME_TEST__;
    for (let i = 0; i < 20; i++) { const s = t.getState(); if (s.problem && s.problem.legendBig === 100) break; t.answerCorrect(); }
    const st = t.getState();
    return { legendBig: st.problem.legendBig, target: st.problem.target, id: st.problem.id };
  });
  // wait for the plate to be empty at the start of the 100/10 wave
  let g = null;
  for (let i = 0; i < 60; i++) { g = await geom(page); if (g.phase === 'playing' && g.plateIds.length === 0 && g.problem.legendBig === 100) break; await sleep(150); }
  const before = g.plateIds.length;
  await page.screenshot({ path: `${OUT}/legend-100-10.png` });
  let after = before, taps = 0;
  for (let i = 0; i < 8 && after === before; i++) {
    const gg = await geom(page);
    const c = gg.belt.filter(x => x.progress > .12 && x.progress < .85).sort((a, b) => b.progress - a.progress)[0];
    if (!c) { await sleep(120); continue; }
    taps++;
    await tap(page, c.x + 6, c.y, 110);
    after = (await geom(page)).plateIds.length;
  }
  check('level 3 (100/10): a belt tap loads a star immediately — no confirm-tap gate',
    after === before + 1 && taps === 1, { legendBig: jump.legendBig, target: jump.target, id: jump.id, before, after, taps });
  check('B: console errors 0', errs.length === 0, errs.slice(0, 3));
  await page.close();
}

// ---- C. an underfilled plate must not print the answer ----
{
  const { page, errs } = await open();
  await page.click('#start'); await sleep(600);
  await clearPractice(page); await sleep(700);
  const g0 = await geom(page);
  // let the whole belt pass with an empty plate -> 'under'
  for (let i = 0; i < 120; i++) { const g = await geom(page); if (g.phase !== 'playing') break; await sleep(250); }
  const feedback = await page.evaluate(() => {
    const d = window.__depart_probe;
    return d || null;
  });
  const st = await geom(page);
  check('underfill costs no life', st.lives === 3, { lives: st.lives, phase: st.phase });
  const leak = await page.evaluate(() => {
    // read what the feedback layer actually holds this frame
    const s = window.__GAME_TEST__.getState();
    return { reason: s.lastEvent && s.lastEvent.reason, hasSolution: !!(s.lastEvent && s.lastEvent.solution), hasEquation: !!(s.lastEvent && s.lastEvent.equation), actual: s.lastEvent && s.lastEvent.actual, target: s.lastEvent && s.lastEvent.target };
  });
  check('underfill feedback carries no solution arrangement and no worked equation',
    leak.reason === 'under' && !leak.hasSolution && !leak.hasEquation, leak);
  check('C: console errors 0', errs.length === 0, errs.slice(0, 3));
  await page.close();
}

// ---- D. a clean full clear, real pointer only ----
{
  const { page, errs } = await open();
  const end = await playSession(page);
  const untrusted = await page.evaluate(() => window.__untrusted);
  check('real-pointer clean run reaches 승리 (phase "won")', end.phase === 'won',
    { phase: end.phase, solved: end.solved, lives: end.lives, score: end.score, orders: end.log });
  check('D: every pointerdown was a trusted browser event', untrusted === 0, { untrusted });
  check('D: console errors 0', errs.length === 0, errs.slice(0, 3));
  await page.screenshot({ path: `${OUT}/clean-run-end.png` });
  await page.close();
}

// ---- E. recovery: deliberately overfill the first order, then still win ----
{
  const { page, errs } = await open();
  const end = await playSession(page, { mistakeOnFirst: true });
  check('real-pointer run recovers after a first-order mistake and still wins',
    end.phase === 'won' && end.lives < 3, { phase: end.phase, solved: end.solved, lives: end.lives, orders: end.log });
  check('E: console errors 0', errs.length === 0, errs.slice(0, 3));
  await page.screenshot({ path: `${OUT}/recovery-run-end.png` });
  await page.close();
}

await browser.close(); await srv.close();
const report = {
  at: new Date().toISOString(),
  indexSha256: crypto.createHash('sha256').update(fs.readFileSync('public/g/star-cork/index.html')).digest('hex'),
  engineSha256: crypto.createHash('sha256').update(fs.readFileSync('public/g/star-cork/engine.js')).digest('hex'),
  passed: results.filter(r => r.pass).length, total: results.length, results,
};
fs.writeFileSync(`${OUT}/report.json`, JSON.stringify(report, null, 2) + '\n');
console.log(`\n${report.passed}/${report.total} checks passed`);
if (report.passed !== report.total) process.exitCode = 1;
