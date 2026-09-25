#!/usr/bin/env node
/**
 * 시각 정체성 증거 — 「수직 유리 랙 + 완성 판 기립 + 후면 투광 그림자」가 실제로 화면에 나오는지.
 *
 * 실제 마우스 드래그로 판을 처리하고, 잠기는 순간(기립 0.0s/0.25s/0.55s)과 오각별 완성,
 * 낙하 실패 직후를 캡처한다. 판정 훅은 쓰지 않는다.
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
const SHOT_DIR = path.join(HERE, 'visual-evidence');
const OUT_FILE = path.join(HERE, 'visual-evidence.json');
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const MIME = { '.html': 'text/html; charset=utf-8', '.css': 'text/css', '.js': 'text/javascript', '.png': 'image/png', '.json': 'application/json', '.woff2': 'font/woff2' };

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
  for (const dir of fs.readdirSync(base).sort().reverse()) {
    for (const cand of [
      path.join(base, dir, 'chrome-mac-arm64', 'Google Chrome for Testing.app', 'Contents/MacOS/Google Chrome for Testing'),
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
    phase: S.phase, solved: S.solved, lives: S.lives, tutorial: S.tutorial, discardGuide: S.discardGuide,
    hint: document.getElementById('feedback')?.textContent || '',
    plate: p && { id: p.id, kind: p.kind, discard: !!p.discard, guided: !!p.guided, locked: !!p.locked, outcome: p.outcome, star: !!p.star, rackPose: g.rackPose,
      remaining: (p.requiredPairs || []).filter((q) => !done.has(key(q[0], q[1]))),
      allDiagonals: p.n === 5 ? [[0,2],[0,3],[1,3],[1,4],[2,4]].filter((q) => !done.has(key(q[0], q[1]))) : [] },
    view: { cx: rect.left + g.cx, cy: rect.top + g.cy, right: rect.left + rect.width },
    vertices: g.vertices.map((v) => ({ x: rect.left + v.x, y: rect.top + v.y })),
  };
});

async function drag(page, a, b) {
  await page.mouse.move(a.x, a.y);
  await page.mouse.down();
  for (let i = 1; i <= 8; i++) await page.mouse.move(a.x + (b.x - a.x) * i / 8, a.y + (b.y - a.y) * i / 8);
  await page.mouse.up();
}

async function main() {
  fs.rmSync(SHOT_DIR, { recursive: true, force: true });
  fs.mkdirSync(SHOT_DIR, { recursive: true });
  const server = await serve();
  const browser = await puppeteer.launch({ headless: true, executablePath: resolveChrome(), args: ['--no-sandbox', '--mute-audio'] });
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 2 });
  const errors = [];
  page.on('pageerror', (e) => errors.push(String(e)));
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
  await page.goto(`http://127.0.0.1:${server.address().port}/g/star-out/index.html`, { waitUntil: 'load' });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 20000 });
  await page.evaluate(() => localStorage.clear());
  await page.reload({ waitUntil: 'load' });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 20000 });

  const shots = [];
  const shoot = async (name, note) => {
    const file = path.join(SHOT_DIR, `${name}.png`);
    await page.screenshot({ path: file });
    shots.push({ name, note, file: path.relative(ROOT, file) });
  };
  await shoot('00-title', '타이틀 화면');

  const bb = await (await page.$('#begin')).boundingBox();
  await page.mouse.click(bb.x + bb.width / 2, bb.y + bb.height / 2);
  await sleep(400);

  let lockShots = 0, starShot = false;
  const deadline = Date.now() + 80000;
  while (Date.now() < deadline) {
    const s = await snapshot(page);
    if (s.phase !== 'playing' || !s.plate) break;
    if (s.plate.locked) { await sleep(100); continue; }
    const ordinary = !s.tutorial && !s.plate.guided && !s.discardGuide && !s.plate.discard;
    if (s.plate.discard || s.discardGuide) {
      await drag(page, { x: s.view.cx, y: s.view.cy }, { x: s.view.right - 3, y: s.view.cy });
      await sleep(150);
      await shoot(`20-discard-${s.plate.kind}`, '받지 않는 판을 랙 밖으로 밀어 낸 직후');
    } else {
      const pairs = s.plate.remaining;
      for (let k = 0; k < pairs.length; k++) {
        const cur = await snapshot(page);
        if (!cur.plate || cur.plate.locked) break;
        await drag(page, cur.vertices[pairs[k][0]], cur.vertices[pairs[k][1]]);
        await sleep(60);
      }
      if (ordinary && lockShots < 2) {
        lockShots++;
        await shoot(`10-lock-${lockShots}-a`, '완성 직후 — 기립 시작');
        await sleep(230);
        await shoot(`10-lock-${lockShots}-b`, '기립 중 — 후면 투광 그림자와 랙 클립');
        await sleep(300);
        await shoot(`10-lock-${lockShots}-c`, '랙에 걸린 상태');
      }
      // 별 기회 판이면 남은 대각선을 계속 이어 오각별을 완성한다.
      let after = await snapshot(page);
      if (after.plate?.kind === 'star_bonus' && after.plate.locked && !after.plate.star && !starShot) {
        for (const q of after.plate.allDiagonals) {
          const cur = await snapshot(page);
          if (!cur.plate || cur.plate.star) break;
          await drag(page, cur.vertices[q[0]], cur.vertices[q[1]]);
          await sleep(40);
        }
        const st = await snapshot(page);
        if (st.plate?.star) { starShot = true; await shoot('30-star', '오각별 완성 — 가운데 작은 오각형과 별 빛 그림자'); }
      }
    }
    await sleep(120);
  }
  const final = await snapshot(page);
  await sleep(600);
  await shoot('40-result', `종료 화면 (phase=${final.phase}, solved=${final.solved})`);

  const report = {
    slug: 'star-out',
    sourceSha256: crypto.createHash('sha256').update(fs.readFileSync(GAME_FILE)).digest('hex'),
    ranAt: new Date().toISOString(),
    method: '실제 page.mouse 드래그로만 진행. 판정 훅 미사용. 각 국면에서 화면 캡처.',
    shots, starCaptured: starShot, lockSequences: lockShots,
    finalPhase: final.phase, finalSolved: final.solved, errors,
  };
  fs.writeFileSync(OUT_FILE, `${JSON.stringify(report, null, 2)}\n`);
  console.log(JSON.stringify({ shots: shots.map((s) => s.name), starCaptured: starShot, finalPhase: final.phase, finalSolved: final.solved, errors }, null, 2));
  await browser.close();
  server.close();
}

main().catch((e) => { console.error(e); process.exit(1); });
