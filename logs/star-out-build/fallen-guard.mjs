#!/usr/bin/env node
/**
 * 낙하 실패 보존 검사 — 판이 바닥에 닿아 깨진 뒤, 그 다음 입력이 '깨졌어요'를 '잠겼어요'로
 * 덮지 않는지 본다 (review must_fix #2 후반).
 *
 * 본판 한 장을 일부러 방치해 떨어뜨린 뒤, 곧바로 실제 마우스로 (1) 판 안쪽 탭,
 * (2) 꼭짓점→꼭짓점 드래그, (3) 가운데→테두리 밀기를 넣고 매 40ms 로 #feedback 을 채집한다.
 */
import fs from 'node:fs';
import http from 'node:http';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../..');
const PUBLIC_DIR = path.join(ROOT, 'public');
const GAME_FILE = path.join(PUBLIC_DIR, 'g', 'star-out', 'index.html');
const OUT_FILE = path.join(HERE, 'fallen-guard.json');
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const MIME = { '.html': 'text/html; charset=utf-8', '.css': 'text/css', '.js': 'text/javascript', '.png': 'image/png', '.json': 'application/json', '.woff2': 'font/woff2' };
const LOCK_WORDS = ['잠겼', '랙에 걸렸', '모두 이었'];
const BROKEN = '유리가 아래 테두리에 닿아 깨졌어요.';

function serve() {
  return new Promise((resolve) => {
    const server = http.createServer((req, res) => {
      const file = path.join(PUBLIC_DIR, decodeURIComponent(new URL(req.url, 'http://x').pathname));
      if (!file.startsWith(PUBLIC_DIR) || !fs.existsSync(file) || fs.statSync(file).isDirectory()) { res.writeHead(404); res.end(); return; }
      res.writeHead(200, { 'content-type': MIME[path.extname(file)] || 'application/octet-stream' });
      res.end(fs.readFileSync(file));
    });
    server.listen(0, '127.0.0.1', () => resolve(server));
  });
}
function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache', 'puppeteer', 'chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const dir of fs.readdirSync(base).sort().reverse())
    for (const cand of [
      path.join(base, dir, 'chrome-mac-arm64', 'Google Chrome for Testing.app', 'Contents/MacOS/Google Chrome for Testing'),
      path.join(base, dir, 'chrome-linux64', 'chrome'),
    ]) if (fs.existsSync(cand)) return cand;
  return undefined;
}

const snapshot = (page) => page.evaluate(() => {
  const rect = document.getElementById('table').getBoundingClientRect();
  const g = geometry(); const p = S.plate;
  const key = (a, b) => (a < b ? `${a}:${b}` : `${b}:${a}`);
  const done = new Set((p?.lines || []).map((l) => key(l.pair[0], l.pair[1])));
  return {
    phase: S.phase, lives: S.lives, tutorial: S.tutorial, discardGuide: S.discardGuide,
    hint: document.getElementById('feedback')?.textContent || '',
    plate: p && { id: p.id, kind: p.kind, discard: !!p.discard, guided: !!p.guided, locked: !!p.locked, outcome: p.outcome,
      remaining: (p.requiredPairs || []).filter((q) => !done.has(key(q[0], q[1]))) },
    view: { cx: rect.left + g.cx, cy: rect.top + g.cy, right: rect.left + rect.width },
    vertices: g.vertices.map((v) => ({ x: rect.left + v.x, y: rect.top + v.y })),
  };
});
async function drag(page, a, b) {
  await page.mouse.move(a.x, a.y); await page.mouse.down();
  for (let i = 1; i <= 6; i++) await page.mouse.move(a.x + (b.x - a.x) * i / 6, a.y + (b.y - a.y) * i / 6);
  await page.mouse.up();
}

async function main() {
  const server = await serve();
  const browser = await puppeteer.launch({ headless: true, executablePath: resolveChrome(), args: ['--no-sandbox', '--mute-audio'] });
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
  const errors = [];
  page.on('pageerror', (e) => errors.push(String(e)));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
  await page.goto(`http://127.0.0.1:${server.address().port}/g/star-out/index.html`, { waitUntil: 'load' });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 20000 });
  await page.evaluate(() => localStorage.clear());
  await page.reload({ waitUntil: 'load' });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 20000 });
  const bb = await (await page.$('#begin')).boundingBox();
  await page.mouse.click(bb.x + bb.width / 2, bb.y + bb.height / 2);
  await sleep(400);

  // 온보딩(고정 정사각형 + 밀기 연습)만 실제 마우스로 통과한다.
  const guard = Date.now() + 30000;
  while (Date.now() < guard) {
    const s = await snapshot(page);
    if (s.phase !== 'playing' || !s.plate) break;
    if (!s.tutorial && !s.plate.guided && !s.discardGuide) break;
    if (s.plate.locked) { await sleep(100); continue; }
    if (s.discardGuide || s.plate.discard) await drag(page, { x: s.view.cx, y: s.view.cy }, { x: s.view.right - 3, y: s.view.cy });
    else { const q = s.plate.remaining[0]; if (q) await drag(page, s.vertices[q[0]], s.vertices[q[1]]); }
    await sleep(200);
  }

  // 본판 한 장을 방치해 떨어뜨린다.
  let fell = null;
  const fallDeadline = Date.now() + 20000;
  while (Date.now() < fallDeadline) {
    const s = await snapshot(page);
    if (s.phase !== 'playing') break;
    if (s.plate?.outcome === 'fallen') { fell = s; break; }
    await sleep(120);
  }
  if (!fell) throw new Error('본판이 낙하하지 않았다');

  const timeline = [{ t: 0, hint: fell.hint, note: '낙하 직후' }];
  const t0 = Date.now();
  const probe = async (note) => {
    const s = await snapshot(page);
    timeline.push({ t: Date.now() - t0, hint: s.hint, note, outcome: s.plate?.outcome, lives: s.lives });
    return s;
  };
  // (1) 판 안쪽 탭
  let s = await probe('탭 직전');
  await page.mouse.move(s.view.cx, s.view.cy); await page.mouse.down(); await sleep(40); await page.mouse.up();
  await sleep(120); s = await probe('판 안쪽 탭 직후');
  // (2) 꼭짓점 → 꼭짓점 드래그
  if (s.vertices.length >= 3) await drag(page, s.vertices[0], s.vertices[2]);
  await sleep(120); s = await probe('꼭짓점 드래그 직후');
  // (3) 가운데 → 테두리 밀기
  await drag(page, { x: s.view.cx, y: s.view.cy }, { x: s.view.right - 3, y: s.view.cy });
  await sleep(120); s = await probe('밀기 직후');
  // 2초까지 유지되는지
  while (Date.now() - t0 < 2100) { await sleep(180); await probe('보존 확인'); }
  const afterWindow = await probe('2.1초 경과');

  const withinTwoSeconds = timeline.filter((e) => e.t <= 2000);
  const brokenHeld = withinTwoSeconds.every((e) => e.hint === BROKEN);
  const noLockWording = timeline.filter((e) => e.t <= 2000).every((e) => !LOCK_WORDS.some((w) => e.hint.includes(w)));
  const report = {
    slug: 'star-out',
    sourceSha256: crypto.createHash('sha256').update(fs.readFileSync(GAME_FILE)).digest('hex'),
    ranAt: new Date().toISOString(),
    fallenPlate: { id: fell.plate.id, kind: fell.plate.kind, livesAtFall: fell.lives },
    timeline, afterWindow: { t: afterWindow.t, hint: afterWindow.hint },
    brokenMessageHeldTwoSeconds: brokenHeld,
    neverReplacedByLockWording: noLockWording,
    errors,
    pass: brokenHeld && noLockWording && errors.length === 0,
  };
  fs.writeFileSync(OUT_FILE, `${JSON.stringify(report, null, 2)}\n`);
  console.log(JSON.stringify({ pass: report.pass, brokenHeld, noLockWording, fallen: report.fallenPlate, timeline: timeline.map((e) => `${e.t}ms ${e.note}: ${e.hint}`) }, null, 2));
  await browser.close(); server.close();
}
main().catch((e) => { console.error(e); process.exit(1); });
