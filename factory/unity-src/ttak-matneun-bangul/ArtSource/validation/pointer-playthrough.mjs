// 실제 pointer 이벤트만으로 한 판 전체를 플레이한다(훅 미사용): 타이틀 → 연습 → R1~R6(접기 띠 포함) → 결과.
// 2) 오답 → 수리 경로. 결과: pointer-playthrough-results.json + pointer-playthrough/*.png
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';
const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const OUT = path.join(HERE, 'pointer-playthrough'); fs.mkdirSync(OUT, { recursive: true });
const sleep = ms => new Promise(r => setTimeout(r, ms));
function chromePath() {
  const base = path.join(process.env.HOME, '.cache/puppeteer/chrome');
  for (const b of fs.readdirSync(base).sort().reverse()) { const c = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing'); if (fs.existsSync(c)) return c; }
}
const srv = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({ executablePath: chromePath(), headless: true, protocolTimeout: 120000 });
const results = { runs: [], errors: [] };
const st = p => p.evaluate(() => window.__GAME_TEST__.getState());
async function waitIdle(p, pred, ms = 8000) {
  const end = Date.now() + ms; let s;
  while (Date.now() < end) { s = await st(p); if (s.seq === '' && pred(s)) return s; await sleep(60); }
  throw new Error('waitIdle timeout ' + JSON.stringify(s).slice(0, 300));
}
function px(s, W, H, arr, i) { return [arr[i * 2] / 10000 * W, arr[i * 2 + 1] / 10000 * H]; }
function centers(s, W, H) {
  const A = px(s, W, H, s.triPx, 0), B = px(s, W, H, s.triPx, 1), C = px(s, W, H, s.triPx, 2);
  const d = (p, q) => Math.hypot(p[0] - q[0], p[1] - q[1]);
  const a = d(B, C), b = d(C, A), c = d(A, B), per = a + b + c;
  const I = [(a * A[0] + b * B[0] + c * C[0]) / per, (a * A[1] + b * B[1] + c * C[1]) / per];
  const D = 2 * (A[0] * (B[1] - C[1]) + B[0] * (C[1] - A[1]) + C[0] * (A[1] - B[1]));
  const sq = q => q[0] * q[0] + q[1] * q[1];
  const O = [(sq(A) * (B[1] - C[1]) + sq(B) * (C[1] - A[1]) + sq(C) * (A[1] - B[1])) / D, (sq(A) * (C[0] - B[0]) + sq(B) * (A[0] - C[0]) + sq(C) * (B[0] - A[0])) / D];
  const G = [(A[0] + B[0] + C[0]) / 3, (A[1] + B[1] + C[1]) / 3];
  return { A, B, C, I, O, G };
}
async function drag(p, from, to, steps = 14) {
  await p.mouse.move(from[0], from[1]); await p.mouse.down();
  for (let i = 1; i <= steps; i++) { await p.mouse.move(from[0] + (to[0] - from[0]) * i / steps, from[1] + (to[1] - from[1]) * i / steps); await sleep(25); }
  await sleep(60); await p.mouse.up();
}
for (const [W, H, nm] of [[390, 844, 'port'], [1280, 800, 'land']]) {
  const page = await browser.newPage();
  page.on('console', m => { if (m.type() === 'error') results.errors.push(nm + ': ' + m.text()); });
  page.on('pageerror', e => results.errors.push(nm + ' pageerror: ' + String(e)));
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  await page.goto(srv.url + '/g/ttak-matneun-bangul/index.html');
  await page.waitForFunction(() => window.__GAME_TEST__ && window.__GAME_TEST__.ready, { timeout: 30000 });
  await sleep(1500);
  const run = { view: nm, rounds: [] };
  await page.screenshot({ path: path.join(OUT, `${nm}-00-title.png`) });
  await page.mouse.click(W * 0.5, H * 0.55); await sleep(1300);
  await page.screenshot({ path: path.join(OUT, `${nm}-01-demo.png`) });
  let s = await waitIdle(page, s => s.phase === 'practice');
  await page.screenshot({ path: path.join(OUT, `${nm}-02-guide.png`) });
  let c = centers(s, W, H);
  await drag(page, px(s, W, H, s.seedPx, 0), c.I);
  await sleep(1500);
  await page.screenshot({ path: path.join(OUT, `${nm}-03-practice-correct.png`) });
  for (let r = 1; r <= 6; r++) {
    s = await waitIdle(page, s => s.phase === 'playing' && s.round === r, 9000);
    c = centers(s, W, H);
    const rec = { round: r, kind: s.kind, shape: s.shape, stripsBefore: s.strips };
    if (r === 3 || r === 4) {
      // 접기 띠: 두루마리 → 꼭짓점 → 다른 꼭짓점 (수직이등분선 하나)
      const roll = px(s, W, H, s.stripPx, 0);
      const t0 = px(s, W, H, s.targetPx, 0), t1 = px(s, W, H, s.targetPx, r === 3 ? 1 : 2);
      await drag(page, roll, t0); await sleep(250);
      const sa = await st(page); console.log('fold1', nm, r, 'pv', sa.pointerVersion, 'strips', sa.strips, 'att', sa.attempts, 'roll', roll.map(Math.round), 't0', t0.map(Math.round));
      await drag(page, [t0[0] + 30, t0[1] + 30], t1); await sleep(500);
      const s2 = await st(page); rec.linesAfterFold = s2.lines; rec.stripsAfter = s2.strips;
      if (r === 3) await page.screenshot({ path: path.join(OUT, `${nm}-r3-fold.png`) });
      // 잘못 포개기(꼭짓점+표식) → 띠가 돌아오고 장수 유지
      if (r === 4) {
        await drag(page, roll, px(s, W, H, s.targetPx, 0)); await sleep(200);
        await drag(page, [W / 2, H / 2], px(s, W, H, s.targetPx, 7)); await sleep(400);
        const s3 = await st(page); rec.invalidFoldKeepsStrips = s3.strips === s2.strips;
      }
    }
    const target = s.kind === 'in' ? c.I : c.O;
    await drag(page, px(s, W, H, s.seedPx, 0), target);
    await sleep(r === 1 ? 1700 : 1100);
    if (r === 1 || r === 4 || r === 6) await page.screenshot({ path: path.join(OUT, `${nm}-r${r}-good.png`) });
    const s4 = await st(page);
    rec.solved = s4.solved; rec.firstTry = `${s4.firstTryCorrect}/${s4.firstTryTotal}`; rec.lives = s4.lives;
    run.rounds.push(rec);
  }
  await sleep(2600);
  s = await st(page);
  run.end = { phase: s.phase, rescued: s.rescued, firstTry: `${s.firstTryCorrect}/${s.firstTryTotal}`, mastery: s.mastery, score: s.score, lives: s.lives };
  await sleep(1200);
  await page.screenshot({ path: path.join(OUT, `${nm}-90-end.png`) });
  // 다시 구조(결과 카드의 방울 버튼) → R1 오답(무게중심) → 수리 → 정답
  if (nm === 'port') {
    const btn = await page.evaluate(() => null);
    await page.mouse.click(W / 2, H / 2 + 40 + 165 - 62 - 46 + 46); // 결과 카드 아래쪽 버튼 근처
    await sleep(400);
    let s5 = await st(page);
    if (s5.phase !== 'playing') {
      // 위치가 다르면 카드 아래 영역을 훑는다
      for (let y = H / 2 + 60; y < H / 2 + 220 && s5.phase !== 'playing'; y += 20) { await page.mouse.click(W / 2, y); await sleep(250); s5 = await st(page); }
    }
    run.replay = s5.phase;
    s5 = await waitIdle(page, s => s.phase === 'playing' && s.round === 1, 9000);
    c = centers(s5, W, H);
    await drag(page, px(s5, W, H, s5.seedPx, 0), c.G); await sleep(700);
    await page.screenshot({ path: path.join(OUT, `${nm}-91-wrong.png`) });
    let s6 = await waitIdle(page, s => s.phase === 'playing', 6000);
    run.afterWrong = { lives: s6.lives, repair: s6.repair, mis: s6.misconceptionId, firstTry: `${s6.firstTryCorrect}/${s6.firstTryTotal}` };
    await page.screenshot({ path: path.join(OUT, `${nm}-92-repair.png`) });
    await drag(page, px(s6, W, H, s6.seedPx, 0), c.I); await sleep(900);
    const s7 = await st(page);
    run.afterRepair = { solved: s7.solved, firstTry: `${s7.firstTryCorrect}/${s7.firstTryTotal}`, score: s7.score };
  }
  results.runs.push(run);
  console.log(JSON.stringify(run));
  await page.close();
}
results.ranAt = new Date().toISOString();
fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), JSON.stringify(results, null, 2));
console.log('errors', results.errors.length, results.errors.slice(0, 5));
await browser.close(); srv.close();
