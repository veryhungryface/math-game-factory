#!/usr/bin/env node
/**
 * 정답 누설 A/B — 판 종류(밀기 판 vs 유효 정다각형 판)에 따라 화면 안내가 갈리는지 본다.
 *
 * 각 판에서 (1) 판 안쪽 빈 곳을 실제 마우스로 눌렀다 놓아 #feedback 문구를 채집하고,
 * (2) 캔버스를 캡처해 눈금·호 표시와 바닥 이음선 캡션을 눈으로 대조한 뒤,
 * (3) 실제 마우스 드래그로 그 판을 처리해 다음 판으로 넘어간다.
 * __GAME_TEST__ 의 answerCorrect/answerWrong/simulateInput 은 쓰지 않는다.
 * S/geometry 는 "화면에 보이는 좌표"를 얻는 데만 쓰고 판정은 게임이 한다.
 */
import crypto from 'node:crypto';
import fs from 'node:fs';
import http from 'node:http';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../..');
const PUBLIC_DIR = path.join(ROOT, 'public');
const GAME_FILE = path.join(PUBLIC_DIR, 'g', 'star-out', 'index.html');
const OUT_FILE = path.join(HERE, 'leak-ab.json');
const SHOT_DIR = path.join(HERE, 'leak-ab');
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const MIME = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.css': 'text/css', '.png': 'image/png', '.json': 'application/json', '.woff2': 'font/woff2' };

function serve() {
  return new Promise((resolve) => {
    const server = http.createServer((req, res) => {
      const rel = decodeURIComponent(new URL(req.url, 'http://x').pathname);
      const file = path.join(PUBLIC_DIR, rel);
      if (!file.startsWith(PUBLIC_DIR) || !fs.existsSync(file) || fs.statSync(file).isDirectory()) { res.writeHead(404); res.end(); return; }
      res.writeHead(200, { 'content-type': MIME[path.extname(file)] || 'application/octet-stream' });
      res.end(fs.readFileSync(file));
    });
    server.listen(0, '127.0.0.1', () => resolve(server));
  });
}

function resolveChrome() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache', 'puppeteer', 'chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const dir of fs.readdirSync(base).sort().reverse()) {
    for (const cand of [
      path.join(base, dir, 'chrome-mac-arm64', 'Google Chrome for Testing.app', 'Contents/MacOS/Google Chrome for Testing'),
      path.join(base, dir, 'chrome-mac-x64', 'Google Chrome for Testing.app', 'Contents/MacOS/Google Chrome for Testing'),
      path.join(base, dir, 'chrome-linux64', 'chrome'),
    ]) if (fs.existsSync(cand)) return cand;
  }
  return undefined;
}

const snapshot = (page) => page.evaluate(() => {
  const rect = document.getElementById('table').getBoundingClientRect();
  const g = geometry();
  const p = S.plate;
  const key = (a, b) => (a < b ? `${a}:${b}` : `${b}:${a}`);
  const done = new Set((p?.lines || []).map((l) => key(l.pair[0], l.pair[1])));
  return {
    phase: S.phase, lives: S.lives, solved: S.solved, tutorial: S.tutorial, frozen: S.frozen,
    discardGuide: S.discardGuide,
    hint: document.getElementById('feedback')?.textContent || '',
    plate: p && {
      id: p.id, kind: p.kind, n: p.n, marked: p.marked, discard: !!p.discard,
      guided: !!p.guided, locked: !!p.locked,
      remaining: (p.requiredPairs || []).filter((q) => !done.has(key(q[0], q[1]))),
    },
    view: { left: rect.left, top: rect.top, right: rect.left + rect.width, cx: rect.left + g.cx, cy: rect.top + g.cy },
    vertices: g.vertices.map((v) => ({ x: rect.left + v.x, y: rect.top + v.y })),
  };
});

async function drag(page, a, b) {
  await page.mouse.move(a.x, a.y);
  await page.mouse.down();
  for (let i = 1; i <= 8; i++) await page.mouse.move(a.x + (b.x - a.x) * i / 8, a.y + (b.y - a.y) * i / 8);
  await page.mouse.up();
  await sleep(90);
}

async function main() {
  fs.rmSync(SHOT_DIR, { recursive: true, force: true });
  fs.mkdirSync(SHOT_DIR, { recursive: true });
  const server = await serve();
  const browser = await puppeteer.launch({ headless: true, executablePath: resolveChrome(), args: ['--no-sandbox', '--mute-audio'] });
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
  const consoleErrors = [];
  page.on('pageerror', (e) => consoleErrors.push(String(e)));
  page.on('console', (m) => { if (m.type() === 'error') consoleErrors.push(m.text()); });
  await page.goto(`http://127.0.0.1:${server.address().port}/g/star-out/index.html`, { waitUntil: 'load' });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 20000 });
  await page.evaluate(() => localStorage.clear());
  await page.reload({ waitUntil: 'load' });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 20000 });
  const begin = await page.$('#begin');
  const bb = await begin.boundingBox();
  await page.mouse.click(bb.x + bb.width / 2, bb.y + bb.height / 2);
  await sleep(400);

  const samples = {};
  const deadline = Date.now() + 70000;
  while (Date.now() < deadline) {
    let s = await snapshot(page);
    if (s.phase !== 'playing' || !s.plate) break;
    if (s.plate.locked) { await sleep(120); continue; }
    const kind = s.plate.kind;
    const probeable = !s.tutorial && !s.plate.guided && !s.discardGuide && !samples[kind];
    if (probeable) {
      // 판 안쪽 빈 곳(중심)을 눌렀다 제자리에서 놓는다 — 판정 없는 refuse 경로.
      await page.mouse.move(s.view.cx, s.view.cy);
      await page.mouse.down();
      await sleep(60);
      await page.mouse.move(s.view.cx + 3, s.view.cy + 2);
      await page.mouse.up();
      await sleep(140);
      const after = await snapshot(page);
      const shot = path.join(SHOT_DIR, `${kind}.png`);
      await page.screenshot({ path: shot });
      samples[kind] = {
        plateId: s.plate.id, discardPlate: s.plate.discard,
        hintAfterInsideTap: after.hint,
        livesBefore: s.lives, livesAfter: after.lives,
        shot: path.relative(ROOT, shot),
      };
      s = await snapshot(page);
      if (s.phase !== 'playing' || !s.plate || s.plate.locked) continue;
    }
    // 실제 마우스로 이 판을 처리한다.
    if (s.plate.discard || s.discardGuide) {
      await drag(page, { x: s.view.cx, y: s.view.cy }, { x: s.view.right - 3, y: s.view.cy });
    } else {
      const pair = s.plate.remaining[0];
      if (!pair) { await sleep(120); continue; }
      await drag(page, s.vertices[pair[0]], s.vertices[pair[1]]);
    }
    await sleep(120);
  }

  const hints = Object.values(samples).map((v) => v.hintAfterInsideTap);
  const kinds = Object.keys(samples);
  const sawBothSides = Object.values(samples).some((v) => v.discardPlate) && Object.values(samples).some((v) => !v.discardPlate);
  const identical = hints.length >= 2 && hints.every((h) => h === hints[0]);
  const report = {
    slug: 'star-out',
    sourceSha256: crypto.createHash('sha256').update(fs.readFileSync(GAME_FILE)).digest('hex'),
    ranAt: new Date().toISOString(),
    method: '판 안쪽 빈 곳을 page.mouse 로 누르고 놓은 뒤 #feedback 문자열을 채집. 진행도 전부 page.mouse 드래그. answerCorrect/answerWrong/simulateInput 미사용.',
    kinds, samples,
    coveredDiscardAndDrawable: sawBothSides,
    identicalHintAcrossKinds: identical,
    consoleErrors,
    pass: identical && sawBothSides && consoleErrors.length === 0,
  };
  fs.writeFileSync(OUT_FILE, `${JSON.stringify(report, null, 2)}\n`);
  console.log(JSON.stringify(report, null, 2));
  await browser.close();
  server.close();
}

main().catch((e) => { console.error(e); process.exit(1); });
