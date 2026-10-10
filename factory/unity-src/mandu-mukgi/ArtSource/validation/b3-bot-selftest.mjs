// B3 무뇌 봇 자가 테스트 — 실제 빌드를 띄우고 브라우저의 진짜 마우스 이벤트(down/move/up)로 반죽을 잇는다.
// 판정·통계는 게임 C# 상태(firstAttemptCorrect/firstAttemptResolved, 단계별)를 그대로 읽는다(별도 시뮬레이터 아님).
// 배속: ?bot=N 은 Unity Time.timeScale 만 올린다(규칙·판정 동일). 각 정책 200판.
//
// 정책
//  spam-fixed : 매번 화면의 같은 두 점을 긋는다(대상 위치와 무관한 연타)
//  spam-slot  : 매 반죽에서 화면 각도 순 같은 슬롯 쌍(0,1)만 긋는다
//  cycle      : 슬롯 쌍 (0,1)→(1,2)→(2,0) 순환
//  random     : 무작위 반죽·무작위 두 대상
//  lowest-two : 화면에서 가장 아래 두 대상(밑각=아래 두 각 오개념)
//  sweep      : 세 대상을 모두 긋는다(전체 훑기)
//  exclude-pre: 무작위이되 단계3에서 미리 그려진 연결 쌍은 피한다(정보 하나를 쓰는 봇, 우연 기준 상향)
//  idle       : 아무것도 안 한다
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { HERE, PUBLIC, chromePath, sleep } from './lib.mjs';
import { serveStatic } from '../../../../lib/static-server.mjs';

const GAMES = Number(process.env.B3_GAMES || 200);
const W = 390, H = 844;
const ALL = ['spam-fixed', 'spam-slot', 'cycle', 'random', 'lowest-two', 'sweep', 'exclude-pre', 'idle'];
const policies = (process.env.B3_POLICIES || ALL.join(',')).split(',');
const PAR = Number(process.env.B3_PAR || 4);

function rng(seed) { let x = seed >>> 0 || 1; return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; }; }

const server = await serveStatic(PUBLIC);

async function worker(policy) {
  const browser = await puppeteer.launch({ executablePath: chromePath(), headless: true, protocolTimeout: 300000 });
  const page = await browser.newPage();
  const errors = [];
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', e => errors.push(String(e)));
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  const speed = policy === 'idle' || policy === 'spam-fixed' ? 12 : 8;
  await page.goto(`${server.url}/g/mandu-mukgi/index.html?bot=${speed}`, { waitUntil: 'load' });
  await page.waitForFunction(() => window.__GAME_TEST__ && window.__GAME_TEST__.ready, { timeout: 45000 });
  const R = rng(0x5eed + policy.length * 7919 + policy.charCodeAt(0));
  const st = () => page.evaluate(() => window.__GAME_TEST__.getState());
  const px = (d, k) => [d.tx[k] / 1000 * W, d.ty[k] / 1000 * H];
  async function drag(points) {
    await page.mouse.move(points[0][0], points[0][1]);
    await page.mouse.down(); await sleep(40);
    for (let i = 1; i < points.length; i++) {
      const [a, b] = [points[i - 1], points[i]];
      for (let j = 1; j <= 4; j++) { await page.mouse.move(a[0] + (b[0] - a[0]) * j / 4, a[1] + (b[1] - a[1]) * j / 4); await sleep(12); }
    }
    await sleep(35); await page.mouse.up(); await sleep(30);
  }
  // 화면 각도 순 슬롯(맨 위에서 시계 방향) — 정답과 독립인 화면 기준 순서
  function slots(d) {
    const pts = [0, 1, 2].map(k => ({ k, x: d.tx[k], y: d.ty[k] }));
    const cx = pts.reduce((a, p) => a + p.x, 0) / 3, cy = pts.reduce((a, p) => a + p.y, 0) / 3;
    pts.forEach(p => { p.a = (Math.atan2(p.x - cx, -(p.y - cy)) + 2 * Math.PI) % (2 * Math.PI); });
    return pts.sort((a, b) => a.a - b.a).map(p => p.k);
  }
  const agg = { games: 0, firstCorrect: 0, firstResolved: 0, byBand: [[0, 0], [0, 0], [0, 0]], completed: 0, mastered: 0, solved: 0, submissions: 0, expired: 0, overStitch: 0, endReasons: {} };
  let cycleIdx = 0;
  const t0 = Date.now();
  for (let g = 0; g < GAMES; g++) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    let s = await st();
    let lastSub = s.submissions, guard = Date.now();
    while (s.phase === 'playing' && Date.now() - guard < 60000) {
      if (policy === 'idle') { await sleep(150); s = await st(); continue; }
      const ds = s.doughs || [];
      if (ds.length === 0 || s.locked) { await sleep(20); s = await st(); continue; }
      let d = policy === 'random' || policy === 'exclude-pre' ? ds[Math.floor(R() * ds.length)] : ds[0];
      let pair;
      if (policy === 'spam-fixed') { await drag([[W * 0.32, H * 0.48], [W * 0.68, H * 0.48]]); }
      else {
        if (policy === 'spam-slot') { const o = slots(d); pair = [o[0], o[1]]; }
        else if (policy === 'cycle') { const o = slots(d); const c = cycleIdx++ % 3; pair = [o[c], o[(c + 1) % 3]]; }
        else if (policy === 'random') { const a = Math.floor(R() * 3); let b = Math.floor(R() * 2); if (b >= a) b++; pair = [a, b]; }
        else if (policy === 'lowest-two') { const o = [0, 1, 2].sort((a, b) => d.ty[b] - d.ty[a]); pair = [o[0], o[1]]; }
        else if (policy === 'exclude-pre') {
          const cands = [[0, 1], [0, 2], [1, 2]].filter(([a, b]) => !(d.fix && d.pre === ((1 << a) | (1 << b))));
          pair = cands[Math.floor(R() * cands.length)];
        }
        if (policy === 'sweep') { const o = slots(d); await drag([px(d, o[0]), px(d, o[1]), px(d, o[2])]); }
        else await drag([px(d, pair[0]), px(d, pair[1])]);
      }
      const until = Date.now() + 400;
      do { await sleep(20); s = await st(); } while (s.phase === 'playing' && s.submissions === lastSub && Date.now() < until);
      lastSub = s.submissions;
    }
    s = await st();
    agg.games++;
    agg.firstCorrect += s.firstAttemptCorrect; agg.firstResolved += s.firstAttemptResolved;
    for (let b = 0; b < 3; b++) { agg.byBand[b][0] += s.firstByBand[b]; agg.byBand[b][1] += s.resolvedByBand[b]; }
    if (s.solved >= 12) agg.completed++;
    if (s.mastered) agg.mastered++;
    agg.solved += s.solved; agg.submissions += s.submissions; agg.expired += s.expired;
    agg.endReasons[s.endReason || s.phase] = (agg.endReasons[s.endReason || s.phase] || 0) + 1;
    if ((g + 1) % 50 === 0) console.log(`[${policy}] ${g + 1}/${GAMES} first ${agg.firstCorrect}/${agg.firstResolved} · ${((Date.now() - t0) / 1000).toFixed(0)}s`);
  }
  await browser.close();
  agg.firstAttemptRate = agg.firstResolved ? agg.firstCorrect / agg.firstResolved : 0;
  agg.byBandRate = agg.byBand.map(([c, r]) => r ? c / r : null);
  agg.completionRate = agg.completed / agg.games;
  agg.masteryRate = agg.mastered / agg.games;
  agg.avgSolved = agg.solved / agg.games;
  agg.consoleErrors = errors.length;
  agg.seconds = Math.round((Date.now() - t0) / 1000);
  return agg;
}

// 우연 수준: 3대상 중 2개를 고르는 무순서 쌍 3개 중 1개(1/3). 단계3에서 기존 연결을 제외하는 봇은 1/2.
// 12문항 가중 기준(4·4·4): 일반 봇 1/3, exclude-pre (4/3+4/3+4/2)/12 = 38.89%.
const CHANCE = { 'exclude-pre': (4 / 3 + 4 / 3 + 4 / 2) / 12 };
const results = {};
const queue = [...policies];
await Promise.all(Array.from({ length: Math.min(PAR, queue.length) }, async () => {
  while (queue.length) { const p = queue.shift(); results[p] = await worker(p); console.log(`[${p}] done`, JSON.stringify({ rate: results[p].firstAttemptRate.toFixed(4), completed: results[p].completed, mastered: results[p].mastered })); }
}));

// 판정: 첫 시도 정답률이 우연 수준을 유의하게 넘지 않을 것(단측 z, 표본 비율 SE). 무입력 진도 0.
const out = { run_id: `mandu-mukgi-b3-${new Date().toISOString()}`, games_per_policy: GAMES, viewport: `${W}x${H}`, method: '실제 WebGL 빌드 + puppeteer 진짜 마우스 down/move/up. 통계는 C# getState(firstAttemptCorrect/Resolved, firstByBand/resolvedByBand). ?bot=8~12 은 Time.timeScale 배속만.', policies: {} };
let pass = true;
for (const p of policies) {
  const r = results[p];
  const chance = CHANCE[p] ?? 1 / 3;
  const n = r.firstResolved, rate = r.firstAttemptRate;
  const se = Math.sqrt(chance * (1 - chance) / Math.max(1, n));
  const z = n ? (rate - chance) / se : 0;
  const ok = p === 'idle' ? (r.solved === 0 && r.firstCorrect === 0) : z < 2.33;
  if (!ok) pass = false;
  out.policies[p] = { ...r, chance, z: Number(z.toFixed(2)), passed: ok };
}
out.passed = pass;
fs.writeFileSync(path.join(HERE, 'b3-bot-results.json'), JSON.stringify(out, null, 2) + '\n');
for (const p of policies) { const r = out.policies[p]; console.log(`${p.padEnd(12)} first ${(r.firstAttemptRate * 100).toFixed(1)}% (${r.firstCorrect}/${r.firstResolved}) chance ${(r.chance * 100).toFixed(1)}% z=${r.z} · bands ${r.byBandRate.map(x => x == null ? '-' : (x * 100).toFixed(0) + '%').join('/')} · complete ${r.completed}/${r.games} · mastered ${r.mastered} · avgSolved ${r.avgSolved.toFixed(2)} · ${r.passed ? 'PASS' : 'FAIL'}`); }
console.log('overall', pass ? 'PASS' : 'FAIL');
server.close();
