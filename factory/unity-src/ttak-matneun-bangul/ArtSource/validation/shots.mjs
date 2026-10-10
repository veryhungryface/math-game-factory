// 화면 점검용 캡처(실 GPU). node .../shots.mjs [tag]
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';
const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const OUT = path.join(HERE, 'shots'); fs.mkdirSync(OUT, { recursive: true });
const tag = process.argv[2] || 'a';
const sleep = ms => new Promise(r => setTimeout(r, ms));
function chromePath() {
  const base = path.join(process.env.HOME, '.cache/puppeteer/chrome');
  for (const b of fs.readdirSync(base).sort().reverse()) { const c = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'); if (fs.existsSync(c)) return c; }
}
const srv = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({ executablePath: chromePath(), headless: true, protocolTimeout: 120000 });
const errors = [];
async function open(w, h) {
  const page = await browser.newPage();
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); if (m.text().startsWith('BANGUL')) console.log(m.text().slice(0, 4000)); });
  page.on('pageerror', e => errors.push(String(e)));
  await page.setViewport({ width: w, height: h, deviceScaleFactor: 1, isMobile: w < 600, hasTouch: w < 600 });
  await page.goto(srv.url + '/g/ttak-matneun-bangul/index.html', { waitUntil: 'load' });
  await page.waitForFunction(() => window.__GAME_TEST__ && window.__GAME_TEST__.ready, { timeout: 30000 });
  await sleep(1800);
  return page;
}
const st = p => p.evaluate(() => window.__GAME_TEST__.getState());
for (const [w, h, nm] of [[390, 844, 'port'], [1280, 800, 'land']]) {
  const page = await open(w, h);
  await page.screenshot({ path: path.join(OUT, `${tag}-${nm}-0title.png`) });
  // 타이틀 탭 → 연습 시연
  await page.mouse.click(w / 2, h * 0.6); await sleep(1500);
  await page.screenshot({ path: path.join(OUT, `${tag}-${nm}-1demo.png`) });
  await sleep(2600);
  await page.screenshot({ path: path.join(OUT, `${tag}-${nm}-2guide.png`) });
  let s = await st(page);
  // 씨앗을 끌어 무게중심 근처(오답)에 놓아 보기
  const T = s.triPx; const toX = v => v / 10000 * w, toY = v => v / 10000 * h;
  const gx = (toX(T[0]) + toX(T[2]) + toX(T[4])) / 3, gy = (toY(T[1]) + toY(T[3]) + toY(T[5])) / 3;
  const sx = toX(s.seedPx[0]), sy = toY(s.seedPx[1]);
  await page.mouse.move(sx, sy); await page.mouse.down();
  for (let i = 1; i <= 12; i++) { await page.mouse.move(sx + (gx - sx) * i / 12, sy + (gy - sy) * i / 12); await sleep(30); }
  await page.screenshot({ path: path.join(OUT, `${tag}-${nm}-3drag.png`) });
  await page.mouse.up(); await sleep(1000);
  await page.screenshot({ path: path.join(OUT, `${tag}-${nm}-4wrong.png`) });
  await sleep(1500);
  console.log(nm, 'after practice wrong', JSON.stringify(await st(page)).slice(0, 300));
  // 실전 바로 시작 + 정답 훅
  await page.evaluate(() => window.__GAME_TEST__.start()); await sleep(900);
  await page.screenshot({ path: path.join(OUT, `${tag}-${nm}-5r1.png`) });
  await page.evaluate(() => window.__GAME_TEST__.answerCorrect()); await sleep(1100);
  await page.screenshot({ path: path.join(OUT, `${tag}-${nm}-6good.png`) });
  await sleep(1500);
  await page.screenshot({ path: path.join(OUT, `${tag}-${nm}-7r2.png`) });
  await page.evaluate(() => { const t = window.__GAME_TEST__; t.answerCorrect(); }); await sleep(2700);
  await page.evaluate(() => { const t = window.__GAME_TEST__; t.answerWrong(); }); await sleep(700);
  await page.screenshot({ path: path.join(OUT, `${tag}-${nm}-8r3wrong.png`) });
  await sleep(1500);
  await page.screenshot({ path: path.join(OUT, `${tag}-${nm}-9r3repair.png`) });
  await page.evaluate(() => { const t = window.__GAME_TEST__; t.answerCorrect(); t.answerCorrect(); }); await sleep(2700);
  await page.screenshot({ path: path.join(OUT, `${tag}-${nm}-10r5.png`) });
  await page.evaluate(() => { const t = window.__GAME_TEST__; t.answerCorrect(); t.answerCorrect(); t.answerCorrect(); }); await sleep(2800);
  await page.screenshot({ path: path.join(OUT, `${tag}-${nm}-11end.png`) });
  console.log(nm, 'end', JSON.stringify(await st(page)).slice(0, 400));
  await page.close();
}
console.log('errors', errors.length, errors.slice(0, 8));
await browser.close(); srv.close();
