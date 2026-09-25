// Round-2 browser verification with real pointer events only.
import puppeteer from 'puppeteer';
import fs from 'node:fs';
import path from 'node:path';
import { serveStatic } from '../../../factory/lib/static-server.mjs';

const OUT = 'logs/star-cork-build/fix2';
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
  return { page, errs };
}
const tap = async (page, x, y) => { await page.mouse.move(x, y); await page.mouse.down(); await sleep(50); await page.mouse.up(); await sleep(120); };
// Belt geometry, mirrored from index.html positions(): x = 30 + progress*(W-60).
const beltPts = page => page.evaluate(() => {
  const s = window.__GAME_TEST__.getState(), W = Math.min(innerWidth, 1280);
  const short = innerHeight < 760 && !document.documentElement.classList.contains('land');
  const y = document.documentElement.classList.contains('land') ? innerHeight * .35 : (short ? 244 : 267);
  return s.candies.filter(c => c.status === 'belt').map(c => ({ id: c.id, type: c.type, x: 30 + c.progress * (W - 60), y }));
});

// ---- A. onboarding: taps offset from the star centre must still load it ----
{
  const { page, errs } = await open();
  await page.click('#start');
  await sleep(900);
  const before = await beltPts(page);
  const OFFSET = 40; // outside the old big-star radius (34), inside the practice grab (46)
  for (const p of before) await tap(page, p.x + OFFSET, p.y);
  const st = await page.evaluate(() => window.__GAME_TEST__.getState());
  check('practice: three taps offset 40px from centre load all three stars',
    st.plate.length === 3, { offset: OFFSET, plateCount: st.plate.length, stars: before.length });
  await sleep(2200);
  const st2 = await page.evaluate(() => window.__GAME_TEST__.getState());
  check('practice: filling 21 clears the tutorial and starts the timed game',
    st2.tutorial === false && st2.frozen === false, { tutorial: st2.tutorial, frozen: st2.frozen, phase: st2.phase });
  check('practice run: console errors 0', errs.length === 0, errs.slice(0, 3));
  await page.close();
}

// ---- B. onboarding: repeated misses must escalate the prompt ----
{
  const { page, errs } = await open();
  await page.click('#start');
  await sleep(600);
  // Keep tapping empty backboard: input never stops, but nothing is ever loaded.
  // The label box sits in the band under the belt; sample only that strip so the
  // looping hand animation cannot make the comparison pass on its own.
  const strip = { x: 40, y: 310, width: 300, height: 36 };
  const t0 = Date.now();
  const shots = {};
  let taps = 0;
  while (Date.now() - t0 < 19000) {
    await tap(page, 200, 620); taps++;             // empty backboard: input, never progress
    const el = (Date.now() - t0) / 1000;
    if (el > 3 && !shots.early) shots.early = await page.screenshot({ clip: strip });
    if (el > 10 && !shots.mid) shots.mid = await page.screenshot({ clip: strip });
    if (el > 16 && !shots.late) shots.late = await page.screenshot({ clip: strip });
    await sleep(700);
  }
  const st = await page.evaluate(() => window.__GAME_TEST__.getState());
  for (const k of ['early', 'mid', 'late']) fs.writeFileSync(`${OUT}/escalate-${k}.png`, shots[k]);
  const ink = b => b.length;   // PNG of a flat strip compresses far smaller than one with a label
  check('a student who keeps tapping the wrong place still gets a bigger prompt',
    ink(shots.late) > ink(shots.early) * 1.3 && st.plate.length === 0,
    { taps, earlyBytes: ink(shots.early), midBytes: ink(shots.mid), lateBytes: ink(shots.late), plate: st.plate.length });
  check('escalation run: console errors 0', errs.length === 0, errs.slice(0, 3));
  await page.close();
}

// ---- C. under-fill returns the plate intact; over-fill cracks one ----
{
  const { page, errs } = await open();
  await page.click('#start');
  await sleep(600);
  // clear the tutorial with exact taps
  for (const p of await beltPts(page)) await tap(page, p.x, p.y);
  await sleep(2400);
  let st = await page.evaluate(() => window.__GAME_TEST__.getState());
  const livesBefore = st.lives;
  // let the wave expire with an empty plate -> 'under'
  await page.evaluate(() => { const s = window.__GAME_TEST__.getState(); s.waveElapsed = s.waveDuration - 0.05; });
  await sleep(1800);
  st = await page.evaluate(() => window.__GAME_TEST__.getState());
  check('a missed (underfilled) plate costs no life',
    st.lives === livesBefore, { before: livesBefore, after: st.lives, lastEvent: st.lastEvent && st.lastEvent.reason });
  // now overfill: load every belt star, which always exceeds the order
  await sleep(1200);
  // The belt moves once the practice plate is over, so re-read before every tap.
  for (let i = 0; i < 14; i++) {
    const pts = await beltPts(page);
    if (!pts.length) { await sleep(300); continue; }
    await tap(page, pts[0].x, pts[0].y);
    const now = await page.evaluate(() => window.__GAME_TEST__.getState());
    if (now.lives < livesBefore) break;
  }
  await sleep(1500);
  st = await page.evaluate(() => window.__GAME_TEST__.getState());
  check('an overfilled plate does cost a life (fail state still has teeth)',
    st.lives < livesBefore, { before: livesBefore, after: st.lives, lastEvent: st.lastEvent && st.lastEvent.reason });
  check('under/over run: console errors 0', errs.length === 0, errs.slice(0, 3));
  await page.screenshot({ path: `${OUT}/labels-390.png` });
  await page.close();
}

// ---- D. the order card and the plate no longer carry the same label ----
{
  const { page } = await open();
  await page.click('#start');
  await sleep(900);
  const png = await page.screenshot({ path: `${OUT}/onboarding-390.png` });
  const html = fs.readFileSync('public/g/star-cork/index.html', 'utf8');
  const dupe = (html.match(/'별사탕 모은 개수'/g) || []).length;
  check('the string "별사탕 모은 개수" is no longer drawn in two places', dupe === 0, { occurrences: dupe });
  const helpOk = /모자라면 접시는 그대로 돌아오고/.test(html) && !/넘치거나 모자라면 접시가 갈라져요/.test(html);
  check('help text matches what the engine judges', helpOk, {});
  await page.close();
}

await browser.close();
await srv.close();
fs.writeFileSync(`${OUT}/report.json`, JSON.stringify({ at: new Date().toISOString(), results }, null, 2));
const failed = results.filter(r => !r.pass);
console.log(`\n${results.length - failed.length}/${results.length} passed`);
process.exit(failed.length ? 1 : 0);
