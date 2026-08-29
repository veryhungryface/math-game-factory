/**
 * stone-hop 무뇌봇 실측 하네스
 * 실제 게임 규칙(목숨·무료 튕김·물살 드리프트 실패·90초 타이머)을 그대로 태운 채
 * 진짜 pointerdown 입력으로 봇을 돌려 연못 클리어율을 잰다.
 *
 * 사용: DIR=<게임폴더> RUNS=20 BOTS=a,b PONDS=1,2 MODES=easy,hard SCALE=4 OUT=x.json node botrun.mjs
 */
import http from 'node:http';
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from '/Users/sitpo/math-game-factory/node_modules/puppeteer/lib/esm/puppeteer/puppeteer.js';

const GAME_DIR = path.resolve(process.env.DIR || process.argv[2]);
const RUNS = Number(process.env.RUNS || 20);
const SCALE = Number(process.env.SCALE || 4);
const WORKERS = Number(process.env.WORKERS || 6);
const OUT = process.env.OUT || null;
const BOTS = (process.env.BOTS || 'fixedCenter,fixedLeft,fixedRight,random,smart').split(',');
const PONDS = (process.env.PONDS || '1,2,3,4,5,6').split(',').map(Number);
const MODES = (process.env.MODES || 'easy,hard').split(',').map((m) => m === 'hard');

const MIME = { '.html': 'text/html', '.js': 'text/javascript', '.json': 'application/json', '.png': 'image/png', '.jpg': 'image/jpeg', '.css': 'text/css', '.mp3': 'audio/mpeg', '.woff2': 'font/woff2' };
const server = http.createServer((req, res) => {
  let p = decodeURIComponent(req.url.split('?')[0]);
  if (p === '/' || p.endsWith('/')) p += 'index.html';
  const f = path.join(GAME_DIR, p);
  if (!f.startsWith(GAME_DIR) || !fs.existsSync(f)) { res.writeHead(404); res.end(); return; }
  res.writeHead(200, { 'Content-Type': MIME[path.extname(f)] || 'application/octet-stream' });
  res.end(fs.readFileSync(f));
});
await new Promise((r) => server.listen(0, r));
const url = `http://127.0.0.1:${server.address().port}/index.html`;

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const d of fs.readdirSync(base).sort().reverse()) {
    for (const arch of ['chrome-mac-arm64', 'chrome-mac-x64']) {
      const c = path.join(base, d, arch, 'Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
      if (fs.existsSync(c)) return c;
    }
  }
  return undefined;
}

const LAUNCH = {
  headless: true,
  protocolTimeout: 900000,
  executablePath: resolveChrome(),
  args: ['--disable-background-timer-throttling', '--disable-backgrounding-occluded-windows', '--disable-renderer-backgrounding', '--no-sandbox', '--disable-dev-shm-usage', '--mute-audio', '--enable-unsafe-swiftshader', '--use-gl=angle', '--use-angle=swiftshader', '--hide-scrollbars'],
};

const INJECT = () => {
  const cv = document.getElementById('cv');
  function tap(x, y) {
    const ev = { bubbles: true, cancelable: true, clientX: x, clientY: y, pointerId: 1, pointerType: 'touch', isPrimary: true };
    cv.dispatchEvent(new PointerEvent('pointerdown', ev));
    cv.dispatchEvent(new PointerEvent('pointerup', ev));
  }
  window.__runBot = async function (kind, pond, hard, scale) {
    const T = window.__GAME_TEST__;
    T.beginPond(pond, hard, scale);
    const t0 = performance.now();
    let taps = 0;
    for (;;) {
      const v = T.botView();
      if (v.phase !== 'playing') return { cleared: v.phase === 'clear', phase: v.phase, rows: v.rows, taps };
      if (performance.now() - t0 > 120000) return { cleared: false, phase: 'timeout', rows: v.rows, taps };
      if (!v.busy && v.stones.length) {
        const n = v.stones.length;
        let target = null;
        if (kind.startsWith('pat:')) {
          // 고정 순환 패턴: 지정한 칸들을 순서대로 계속 누른다 (가라앉은 칸은 실제 게임처럼 입력이 무시된다)
          const cols = kind.slice(4).split('').map(Number).filter((c) => c < n);
          target = v.stones[cols[taps % cols.length]] || v.stones[0];
        } else if (kind === 'fixedCenter') target = v.stones[(n / 2) | 0];
        else if (kind === 'fixedLeft') target = v.stones[0];
        else if (kind === 'fixedRight') target = v.stones[n - 1];
        else if (kind === 'random') target = v.stones[(Math.random() * n) | 0];
        else if (kind === 'randomLive') {
          const live = v.stones.filter((s) => !s.dead);
          target = live[(Math.random() * live.length) | 0] || v.stones[0];
        } else if (kind === 'human85' || kind === 'human60') {
          const acc = kind === 'human85' ? 0.85 : 0.6;
          const live = v.stones.filter((st) => !st.dead);
          const safe = live.filter((st) => st.safe);
          target = (Math.random() < acc && safe.length) ? safe[(Math.random() * safe.length) | 0]
                                                        : live[(Math.random() * live.length) | 0];
        } else if (kind === 'smart') {
          const safe = v.stones.filter((s) => s.safe && !s.dead);
          target = safe[(Math.random() * safe.length) | 0] || v.stones[0];
        }
        if (target) { tap(target.x, target.y); taps++; }
      }
      await new Promise((r) => requestAnimationFrame(r));
    }
  };
};

const errs = [];
const browsers = [];
async function makePage() {
  const browser = await puppeteer.launch(LAUNCH);
  browsers.push(browser);
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  page.on('pageerror', (e) => errs.push(String(e).slice(0, 200)));
  page.on('console', (m) => { if (m.type() === 'error') errs.push(m.text().slice(0, 200)); });
  await page.goto(url, { waitUntil: 'networkidle2' });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 30000 });
  await page.evaluate(INJECT);
  return page;
}

const jobs = [];
for (const hard of MODES) for (const pond of PONDS) for (const bot of BOTS) jobs.push({ hard, pond, bot });

const results = {};
let ji = 0;
const pages = await Promise.all(Array.from({ length: Math.min(WORKERS, jobs.length) }, makePage));
await Promise.all(pages.map(async (page) => {
  for (;;) {
    const j = jobs[ji++];
    if (!j) return;
    let cleared = 0, rowsSum = 0;
    for (let i = 0; i < RUNS; i++) {
      const r = await page.evaluate((b, p, h, s) => window.__runBot(b, p, h, s), j.bot, j.pond, j.hard, SCALE);
      if (r.cleared) cleared++;
      rowsSum += r.rows;
    }
    const key = `${j.hard ? '아슬' : '여유'}/연못${j.pond}/${j.bot}`;
    results[key] = { runs: RUNS, cleared, rate: +(cleared / RUNS).toFixed(3), avgRows: +(rowsSum / RUNS).toFixed(2) };
    console.log(key.padEnd(30), `클리어 ${String(cleared).padStart(3)}/${RUNS} (${((cleared / RUNS) * 100).toFixed(1)}%)  평균 ${(rowsSum / RUNS).toFixed(1)}칸`);
  }
}));

if (errs.length) console.log('PAGE ERRORS:', [...new Set(errs)].slice(0, 5));
if (OUT) fs.writeFileSync(OUT, JSON.stringify({ dir: GAME_DIR, runs: RUNS, scale: SCALE, results, errors: [...new Set(errs)].slice(0, 10) }, null, 2));
await Promise.all(browsers.map((b) => b.close()));
server.close();
console.log('DONE');
