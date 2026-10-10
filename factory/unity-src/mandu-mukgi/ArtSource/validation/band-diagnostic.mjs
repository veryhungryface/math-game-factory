// 단계별 진단: 무뇌 봇은 봉인 3개를 일찍 잃어 2·3단계에 거의 닿지 못한다(본 측정의 완주율 0).
// 그래서 2·3단계 문항에서의 첫 시도 정답률을 따로 잰다: 훅으로 해당 단계까지 건너뛴 뒤(그 훅 정답은 집계에서 뺀다),
// 봇이 진짜 마우스 획으로 낸 「반죽별 첫 제출」만 센다. 봇이 틀리면 훅으로 수리해 다음 반죽으로 넘어간다.
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { HERE, PUBLIC, chromePath, sleep } from './lib.mjs';
import { serveStatic } from '../../../../lib/static-server.mjs';

const N = Number(process.env.DIAG_N || 150);
const W = 390, H = 844;
const POLICIES = ['spam-slot', 'cycle', 'random', 'lowest-two', 'exclude-pre'];
const server = await serveStatic(PUBLIC);
function rng(seed) { let x = seed >>> 0 || 1; return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; }; }

async function worker(policy) {
  const browser = await puppeteer.launch({ executablePath: chromePath(), headless: true, protocolTimeout: 300000 });
  const page = await browser.newPage();
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  await page.goto(`${server.url}/g/mandu-mukgi/index.html?bot=8`, { waitUntil: 'load' });
  await page.waitForFunction(() => window.__GAME_TEST__ && window.__GAME_TEST__.ready, { timeout: 45000 });
  const R = rng(0xd1a6 + policy.length * 131);
  const st = () => page.evaluate(() => window.__GAME_TEST__.getState());
  const px = (d, k) => [d.tx[k] / 1000 * W, d.ty[k] / 1000 * H];
  async function drag(a, b) {
    await page.mouse.move(a[0], a[1]); await page.mouse.down(); await sleep(40);
    for (let j = 1; j <= 4; j++) { await page.mouse.move(a[0] + (b[0] - a[0]) * j / 4, a[1] + (b[1] - a[1]) * j / 4); await sleep(12); }
    await sleep(35); await page.mouse.up(); await sleep(30);
  }
  function slots(d) {
    const pts = [0, 1, 2].map(k => ({ k, x: d.tx[k], y: d.ty[k] }));
    const cx = pts.reduce((a, p) => a + p.x, 0) / 3, cy = pts.reduce((a, p) => a + p.y, 0) / 3;
    pts.forEach(p => { p.a = (Math.atan2(p.x - cx, -(p.y - cy)) + 2 * Math.PI) % (2 * Math.PI); });
    return pts.sort((a, b) => a.a - b.a).map(p => p.k);
  }
  const res = { 2: [0, 0], 3: [0, 0] };
  let cyc = 0;
  for (const band of [2, 3]) {
    while (res[band][1] < N) {
      await page.evaluate(() => window.__GAME_TEST__.start());
      for (let i = 0; i < 4 * (band - 1); i++) await page.evaluate(() => window.__GAME_TEST__.answerCorrect());
      const seen = new Set();
      let s = await st(), guard = Date.now();
      while (s.level !== band && Date.now() - guard < 8000) { await sleep(40); s = await st(); }
      guard = Date.now();
      while (s.phase === 'playing' && s.level === band && res[band][1] < N && Date.now() - guard < 30000) {
        if (s.locked || s.doughs.length === 0) { await sleep(20); s = await st(); continue; }
        const d = s.doughs.find(x => !seen.has(x.id) && !x.failed);
        if (!d) { // 남은 건 수리할 반죽뿐 → 훅으로 수리(진단 집계 밖)
          await page.evaluate(() => window.__GAME_TEST__.answerCorrect()); await sleep(30); s = await st(); continue;
        }
        let pair;
        if (policy === 'spam-slot') { const o = slots(d); pair = [o[0], o[1]]; }
        else if (policy === 'cycle') { const o = slots(d); const c = cyc++ % 3; pair = [o[c], o[(c + 1) % 3]]; }
        else if (policy === 'random') { const a = Math.floor(R() * 3); let b = Math.floor(R() * 2); if (b >= a) b++; pair = [a, b]; }
        else if (policy === 'lowest-two') { const o = [0, 1, 2].sort((a, b) => d.ty[b] - d.ty[a]); pair = [o[0], o[1]]; }
        else { const c = [[0, 1], [0, 2], [1, 2]].filter(([a, b]) => !(d.fix && d.pre === ((1 << a) | (1 << b)))); pair = c[Math.floor(R() * c.length)]; }
        const before = s.submissions, fc = s.firstAttemptCorrect, fr = s.firstAttemptResolved;
        await drag(px(d, pair[0]), px(d, pair[1]));
        const until = Date.now() + 600;
        do { await sleep(20); s = await st(); } while (s.submissions === before && Date.now() < until);
        if (s.submissions === before) continue; // 획 미등록 → 같은 반죽 재시도
        seen.add(d.id);
        if (s.firstAttemptResolved > fr) { res[band][1]++; if (s.firstAttemptCorrect > fc) res[band][0]++; }
        guard = Date.now();
      }
    }
  }
  await browser.close();
  return res;
}
const out = { n_per_band: N, policies: {} };
await Promise.all(POLICIES.map(async p => { const r = await worker(p); out.policies[p] = { band2: { correct: r[2][0], n: r[2][1], rate: r[2][0] / r[2][1] }, band3: { correct: r[3][0], n: r[3][1], rate: r[3][0] / r[3][1] }, chance: { band2: 1 / 3, band3: p === 'exclude-pre' ? 1 / 2 : 1 / 3 } }; console.log(p, JSON.stringify(out.policies[p])); }));
fs.writeFileSync(path.join(HERE, 'band-diagnostic-results.json'), JSON.stringify(out, null, 2) + '\n');
server.close();
