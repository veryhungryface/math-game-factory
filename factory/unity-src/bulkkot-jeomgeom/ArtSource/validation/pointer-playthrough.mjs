import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const SHOTS = path.join(HERE, 'pointer-playthrough');
const VALIDATION_RUN_ID = process.env.VALIDATION_RUN_ID || `bulkkot-fix2-${new Date().toISOString()}`;
fs.mkdirSync(SHOTS, { recursive: true });

function artifactHash() {
  const hash = crypto.createHash('sha256');
  const game = path.join(PUBLIC, 'g', 'bulkkot-jeomgeom');
  for (const file of ['index.html', ...fs.readdirSync(path.join(game, 'Build')).sort().map(f => `Build/${f}`)]) {
    hash.update(file); hash.update(fs.readFileSync(path.join(game, file)));
  }
  return hash.digest('hex');
}

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
  throw new Error('Chrome for Testing not found');
}

const state = page => page.evaluate(() => window.__GAME_TEST__.getState());

async function point(page, kind, card = 0, ring = 0) {
  const s = await state(page), v = page.viewport();
  const xDevice = kind === 'fuse' ? s.fusePx[0] : s.cardPx[card * 3];
  const yDevice = kind === 'fuse' ? s.fusePx[1] : s.cardPx[card * 3 + 1 + ring];
  return { x: xDevice * v.width / s.screenW, y: v.height - yDevice * v.height / s.screenH };
}

async function drag(page, from, dx, dy) {
  await page.mouse.move(from.x, from.y); await page.mouse.down(); await new Promise(r => setTimeout(r, 28));
  await page.mouse.move(from.x + dx * .5, from.y + dy * .5, { steps: 3 }); await new Promise(r => setTimeout(r, 24));
  await page.mouse.move(from.x + dx, from.y + dy, { steps: 3 }); await new Promise(r => setTimeout(r, 24));
  await page.mouse.up(); await new Promise(r => setTimeout(r, 70));
}

async function dragRing(page, card, ring, direction = 1) { await drag(page, await point(page, 'ring', card, ring), 42 * direction, 0); }
async function pullFuse(page) { await drag(page, await point(page, 'fuse'), 0, 78); }

function decode(id) {
  const p = id.split(':'); return p[0] === 'P' ? { product: true, a: +p[1], b: +p[2] } : { product: false, a: p[0] === 'A' ? 0 : 1, b: +p[1] };
}

async function repairRoster(page) {
  let s = await state(page);
  const wanted = new Set(s.canonicalCaseIds);
  const counts = new Map();
  for (const id of s.currentCaseIds) counts.set(id, (counts.get(id) || 0) + 1);
  const missing = s.canonicalCaseIds.filter(id => !counts.has(id));
  const repairCards = [];
  const kept = new Set();
  for (let card = 0; card < s.currentCaseIds.length; card++) {
    const id = s.currentCaseIds[card];
    if (!wanted.has(id) || kept.has(id)) repairCards.push(card);
    else kept.add(id);
  }
  if (repairCards.length !== missing.length) throw new Error(`bad roster delta: cards=${repairCards.length} missing=${missing.length}`);
  for (let i = 0; i < repairCards.length; i++) {
    const card = repairCards[i];
    const target = decode(missing[i]);
    let cur = decode((await state(page)).currentCaseIds[card]);
    if (cur.product) {
      for (let k = 0; k < 7 && cur.a !== target.a; k++) { await dragRing(page, card, 0); s = await state(page); cur = decode(s.currentCaseIds[card]); }
      for (let k = 0; k < 7 && cur.b !== target.b; k++) { await dragRing(page, card, 1); s = await state(page); cur = decode(s.currentCaseIds[card]); }
    } else {
      if (cur.a !== target.a) { await dragRing(page, card, 0); s = await state(page); cur = decode(s.currentCaseIds[card]); }
      for (let k = 0; k < 7 && cur.b !== target.b; k++) { await dragRing(page, card, 1); s = await state(page); cur = decode(s.currentCaseIds[card]); }
    }
    if (s.currentCaseIds[card] !== missing[i]) throw new Error(`repair failed card=${card}: ${s.currentCaseIds[card]} != ${missing[i]}`);
  }
  s = await state(page);
  if (new Set(s.currentCaseIds).size !== s.canonicalCaseIds.length) throw new Error('repair did not produce a complete unique roster');
}

async function waitRoundChange(page, before) {
  await page.waitForFunction(b => { const s = window.__GAME_TEST__.getState(); return s.phase === 'clear' || s.phase === 'gameover' || s.roundId !== b || (!s.frozen && s.phase === 'playing'); }, { timeout: 5000 }, before);
  await new Promise(r => setTimeout(r, 120));
}

async function enterThroughPractice(page) {
  await page.mouse.move(195, 420); await page.mouse.down(); await new Promise(r => setTimeout(r, 55)); await page.mouse.up(); await new Promise(r => setTimeout(r, 180));
  let s = await state(page); if (!s.onboarding) throw new Error(`title did not enter practice: ${JSON.stringify(s)}`);
  await repairRoster(page); await page.screenshot({ path: path.join(SHOTS, '01-practice-repaired.png') });
  await pullFuse(page); await page.screenshot({ path: path.join(SHOTS, '02-practice-launch.png') });
  await page.waitForFunction(() => { const s = window.__GAME_TEST__.getState(); return !s.onboarding && s.roundId === 1 && !s.frozen; }, { timeout: 5000 });
}

async function runRecovery(page) {
  await enterThroughPractice(page);
  const firstRoundStart = await state(page);
  await page.screenshot({ path: path.join(SHOTS, '03-round1-start.png') });
  await pullFuse(page);
  await page.screenshot({ path: path.join(SHOTS, '04-round1-wrong.png') });
  await page.waitForFunction(() => { const s = window.__GAME_TEST__.getState(); return !s.frozen && s.roundId === 1 && s.lives === 2; }, { timeout: 5000 });
  for (let expected = 1; expected <= 6; expected++) {
    const before = (await state(page)).roundId;
    await repairRoster(page); await pullFuse(page); await waitRoundChange(page, before);
  }
  const end = await state(page);
  await page.screenshot({ path: path.join(SHOTS, '05-recovery-clear.png') });
  if (end.phase !== 'clear' || end.solved !== 6 || end.firstCorrect !== 5 || end.lives !== 2) throw new Error(`recovery path failed: ${JSON.stringify(end)}`);
  return { firstRoundStart, end };
}

async function runClean(page, url) {
  await page.goto(url, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 30000 });
  await enterThroughPractice(page);
  const secondRoundStart = await state(page);
  for (let expected = 1; expected <= 6; expected++) {
    const before = (await state(page)).roundId;
    await repairRoster(page); await pullFuse(page); await waitRoundChange(page, before);
  }
  const end = await state(page);
  await page.screenshot({ path: path.join(SHOTS, '06-clean-clear.png') });
  if (end.phase !== 'clear' || end.solved !== 6 || end.firstCorrect !== 6 || end.lives !== 3) throw new Error(`clean path failed: ${JSON.stringify(end)}`);
  return { secondRoundStart, end };
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  const page = await browser.newPage(); await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 2 });
  const errors = []; page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); }); page.on('pageerror', e => errors.push(String(e)));
  const url = `${server.url}/g/bulkkot-jeomgeom/`;
  await page.goto(url, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 30000 });
  await page.screenshot({ path: path.join(SHOTS, '00-title.png') });
  const recovery = await runRecovery(page);
  const clean = await runClean(page, url);
  const result = {
    run_id: VALIDATION_RUN_ID,
    artifact_hash: artifactHash(),
    method: '390×844 실제 browser mouse down/move/up만으로 타이틀→고리→도화선 경로 실행; answerCorrect/answerWrong 미사용',
    recovery,
    clean,
    second_play_changed: JSON.stringify(recovery.firstRoundStart.currentCaseIds) !== JSON.stringify(clean.secondRoundStart.currentCaseIds),
    consoleErrors: errors
  };
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (errors.length) process.exitCode = 1;
} finally { await browser.close(); await server.close(); }
