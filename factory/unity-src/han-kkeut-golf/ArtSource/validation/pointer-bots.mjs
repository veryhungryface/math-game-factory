#!/usr/bin/env node
// B3 무뇌 봇 — 실제 pointer 이벤트(390×844). __GAME_TEST__.start() 로 연습만 건너뛰고, 답은 전부 진짜 마우스 드래그다.
// 봇은 θ·정답을 읽지 않는다. 화면 좌표(aPx·railPx·dial)만 쓴다. 측정: 첫 제출 정답률 vs 우연 5/180.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';
const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const RUNS = +(process.argv[2] || 12);
const W = 390, H = 844;
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
function chromePath() { const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome'); for (const b of fs.readdirSync(base).sort().reverse()) { const c = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'); if (fs.existsSync(c)) return c; } }
const srv = await serveStatic(path.join(ROOT, 'public'));
const url = `${srv.url}/g/han-kkeut-golf/index.html`;
const browser = await puppeteer.launch({ executablePath: chromePath(), headless: 'new', protocolTimeout: 600000 });
let seed = 12345; const rnd = () => ((seed = (seed * 1103515245 + 12345) & 0x7fffffff) / 0x7fffffff);
async function runBot(name, policy) {
  const page = await browser.newPage(); const errors = [];
  page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text()); });
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  await page.goto(url, { waitUntil: 'load' });
  await page.waitForFunction(() => window.__GAME_TEST__ && window.__GAME_TEST__.ready, { timeout: 30000 });
  const st = () => page.evaluate(() => window.__GAME_TEST__.getState());
  const waitFor = async (pred, ms) => { const t0 = Date.now(); while (Date.now() - t0 < ms) { const s = await st(); if (pred(s)) return s; await sleep(80); } return st(); };
  let att = 0, cor = 0, solved = 0, clears = 0, subs = 0, tick = 0;
  for (let run = 0; run < RUNS; run++) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    let s = await waitFor((x) => x.flow === 'aim', 4000);
    for (let guard = 0; guard < 40 && s.phase === 'playing'; guard++) {
      if (s.flow === 'draw') {
        // 무뇌: 화면 아무 데서 아무 데로 끈다(반지름 긋기 성공 기대 없음)
        const x0 = rnd() * W, y0 = 150 + rnd() * 450, x1 = rnd() * W, y1 = 150 + rnd() * 450;
        await page.mouse.move(x0, y0); await page.mouse.down(); await page.mouse.move(x1, y1, { steps: 6 }); await page.mouse.up();
        await sleep(150); s = await st(); if (++tick > 12) break; continue;
      }
      if (s.flow !== 'aim') { s = await waitFor((x) => x.flow === 'aim' || x.flow === 'draw' || x.phase !== 'playing', 8000); continue; }
      const target = policy(tick++, s);
      if (target === null) break;
      const from = s.railAngle; let d = ((target - from) % 180 + 180) % 180; if (d > 90) d -= 180; if (Math.abs(d) < 4) d = d < 0 ? d - 6 : d + 6;
      const at = (a) => [s.aPx + .6 * s.railPx * Math.cos(a * Math.PI / 180), s.aPy - .6 * s.railPx * Math.sin(a * Math.PI / 180)];
      let flip = 0; let [x0, y0] = at(from); if (y0 < 140 || y0 > H - 250) { flip = 180; [x0, y0] = at(from + 180); }
      await page.mouse.move(x0, y0); await page.mouse.down();
      for (let i = 1; i <= 10; i++) { const [x, y] = at(from + flip + d * i / 10); await page.mouse.move(x, y); await sleep(6); }
      await page.mouse.up(); subs++;
      await waitFor((x) => x.flow === 'rolling' || x.phase !== 'playing', 1500);
      s = await waitFor((x) => x.flow === 'aim' || x.flow === 'draw' || x.phase !== 'playing', 9000);
    }
    s = await st();
    att += s.firstAttempts; cor += s.firstCorrect; solved += s.solved; if (s.phase === 'clear') clears++;
  }
  await page.close();
  return { bot: name, runs: RUNS, submissions: subs, firstAttempts: att, firstCorrect: cor, firstRate: +(cor / Math.max(1, att)).toFixed(4), chance: 0.0278, solvedTotal: solved, clears, consoleErrors: errors.length };
}
const bots = [
  ['연타(항상 90°로 끌기)', () => 90],
  ['순환(0→45→90→135°)', (i) => [0, 45, 90, 135][i % 4]],
  ['무작위 각', () => Math.floor(rnd() * 180)],
];
const results = []; for (const [n, p] of bots) results.push(await runBot(n, p)); // 백그라운드 탭은 rAF 가 멈추므로 순차 실행
// 무입력: start 후 아무것도 안 함 — 진도 0, 시계는 판단 단계에서만 흐른다(90초 뒤 time 종료)
{
  const page = await browser.newPage(); await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  await page.goto(url, { waitUntil: 'load' }); await page.waitForFunction(() => window.__GAME_TEST__ && window.__GAME_TEST__.ready, { timeout: 30000 });
  await page.evaluate(() => window.__GAME_TEST__.start());
  const s0 = await page.evaluate(() => window.__GAME_TEST__.getState()); await sleep(95000);
  const s1 = await page.evaluate(() => window.__GAME_TEST__.getState());
  results.push({ bot: '무입력(95초 방치)', before: { phase: s0.phase, solved: s0.solved }, after: { phase: s1.phase, solved: s1.solved, firstAttempts: s1.firstAttempts, submissionSerial: s1.submissionSerial } });
  await page.close();
}
fs.writeFileSync(path.join(HERE, 'pointer-bots-results.json'), JSON.stringify(results, null, 2));
console.log(JSON.stringify(results, null, 1));
await browser.close(); process.exit(0);
