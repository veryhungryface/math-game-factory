// 박쥐 깨워 실포인터 자가 검증 (390×844). 답을 훅에서 읽지 않는다: 화면 자료를 문장화한 state.prompt 를
// 이 파일의 독립 실수 풀이기(코사인 법칙·각의 합)로 풀고, 제출은 전부 진짜 mouse down/move/up 이다.
// answerCorrect/answerWrong/sampleProblems 는 호출하지 않는다(무뇌 봇의 첫 판 시작에만 start() 사용).
//   A. 두 판 실플레이: 타이틀 탭 → 연습(무입력 9초·오답·정답) → 본판1(첫 장면 과잉 선택 → 깃 손실 → 같은 장면 수정) → 결과 탭 → 본판2
//   B. 경계: 탭만으로는 제출 안 됨 · 깃 0 후 입력 잠금 · 재시작
//   C. 무뇌 봇(실포인터): 고정 획·순환·무작위·네 마리 전부 각 N판, 연타·무입력
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const HERE = path.dirname(new URL(import.meta.url).pathname);
const OUT = path.join(HERE, 'pointer-selftest');
fs.mkdirSync(OUT, {recursive: true});
const N = Number(process.env.BOT_RUNS || 40);
const sleep = ms => new Promise(r => setTimeout(r, ms));
function resolveChrome() {
  const base = path.join(process.env.HOME, '.cache/puppeteer/chrome');
  for (const b of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
}

// 세로 판형 날개 중심(화면 px): 발 위치 (98|292, 598|396) + 날개 중심 −56, 화면 y = 844 − 설계 y
const C = [[98, 302], [292, 302], [98, 504], [292, 504]];
const R = 52;
function segDist(p, a, b) {
  const dx = b[0] - a[0], dy = b[1] - a[1], n = dx * dx + dy * dy;
  const t = n < 1e-9 ? 0 : Math.max(0, Math.min(1, ((p[0] - a[0]) * dx + (p[1] - a[1]) * dy) / n));
  return Math.hypot(a[0] + dx * t - p[0], a[1] + dy * t - p[1]);
}
function maskOf(pts) { let m = 0; for (let i = 1; i < pts.length; i++) for (let k = 0; k < 4; k++) if (segDist(C[k], pts[i - 1], pts[i]) <= R) m |= 1 << k; return m; }
function perms(a) { if (a.length <= 1) return [a]; return a.flatMap((x, i) => perms(a.filter((_, j) => j !== i)).map(r => [x, ...r])); }
function pathFor(mask) {
  const ws = [0, 1, 2, 3].filter(i => mask >> i & 1);
  if (ws.length === 1) { const c = C[ws[0]]; return [[c[0] - 30, c[1]], [c[0] + 30, c[1]]]; }
  for (const p of perms(ws)) { const pts = p.map(i => C[i]); if (maskOf(pts) === mask) return pts; }
  throw new Error('no path ' + mask);
}

// ── 독립 풀이기 ──
function anglesFromSides(a, b, c) {
  const A = Math.acos((b * b + c * c - a * a) / (2 * b * c)) * 180 / Math.PI, B = Math.acos((a * a + c * c - b * b) / (2 * a * c)) * 180 / Math.PI;
  return [A, B, 180 - A - B].sort((x, y) => x - y);
}
function sasAngles(a, b, deg) { const c = Math.sqrt(a * a + b * b - 2 * a * b * Math.cos(deg * Math.PI / 180)); return anglesFromSides(a, b, c); }
function nums(s) { return [...s.matchAll(/=(\d+)/g)].map(m => Number(m[1])); }
function solve(prompt) {
  const parts = prompt.split(' / ');
  const ref = parts[0].slice(parts[0].indexOf('기준 △ABC:'));
  const kind = /CA=/.test(ref) ? 'SSS' : /AC=/.test(ref) ? 'SAS' : 'AA';
  const ang = (s) => { const v = nums(s); return kind === 'SSS' ? anglesFromSides(...v) : kind === 'SAS' ? sasAngles(...v) : [v[0], v[1], 180 - v[0] - v[1]].sort((x, y) => x - y); };
  const t = ang(ref);
  let mask = 0;
  parts.slice(1).forEach((w, i) => { const c = ang(w.slice(w.indexOf('△') + 4)); if (c.every((x, j) => Math.abs(x - t[j]) < 1e-6)) mask |= 1 << i; });
  return {kind, mask};
}

async function stroke(page, pts, steps = 8) {
  await page.mouse.move(pts[0][0], pts[0][1]); await page.mouse.down(); await sleep(30);
  for (let i = 1; i < pts.length; i++) for (let s = 1; s <= steps; s++) {
    const a = pts[i - 1], b = pts[i];
    await page.mouse.move(a[0] + (b[0] - a[0]) * s / steps, a[1] + (b[1] - a[1]) * s / steps); await sleep(12);
  }
  await page.mouse.up();
}
const S = page => page.evaluate(() => window.__GAME_TEST__.getState());
async function waitState(page, pred, ms = 8000) {
  const t0 = Date.now();
  while (Date.now() - t0 < ms) { const s = await S(page); if (pred(s)) return s; await sleep(60); }
  return S(page);
}
const settled = s => !s.frozen || s.phase !== 'playing';

const server = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({headless: true, executablePath: resolveChrome(), protocolTimeout: 180000,
  args: ['--no-sandbox', '--mute-audio', '--use-angle=metal']});
async function open() {
  const page = await browser.newPage();
  page.errs = [];
  page.on('console', m => { if (m.type() === 'error') page.errs.push(m.text()); });
  page.on('pageerror', e => page.errs.push(String(e)));
  await page.setViewport({width: 390, height: 844, deviceScaleFactor: 1});
  await page.goto(`${server.url}/g/bakjwi-kkaewo/index.html`, {waitUntil: 'load'});
  await page.waitForFunction(() => window.__GAME_TEST__ && window.__GAME_TEST__.ready, {timeout: 90000});
  await sleep(800);
  return page;
}
const result = {viewport: '390x844', playthroughs: [], edges: {}, bots: []};
const shot = (page, n) => page.screenshot({path: path.join(OUT, n + '.png')});

// ── A. 두 판 실플레이 ──
{
  const page = await open();
  await shot(page, '00-title');
  await stroke(page, [[195, 360], [196, 361]], 1);                 // 가운데 박쥐 탭
  let s = await waitState(page, s => s.onboarding && s.sceneId === 'practice-fixed');
  await sleep(9000);                                              // 연습 무입력 9초: 진도 0, 연습 유지
  s = await S(page);
  const practiceIdle = {sceneId: s.sceneId, solved: s.solved, onboarding: s.onboarding};
  await shot(page, '01-practice-idle');
  await stroke(page, [C[0], C[1], C[3], C[2]]);                     // 연습 오답(네 마리 전부)
  s = await waitState(page, s => s.lastVerdict === 'wrong');
  await waitState(page, s => s.onboarding && !s.frozen, 6000);
  const practiceWrong = {sceneId: (await S(page)).sceneId, lives: (await S(page)).lives, solved: (await S(page)).solved};
  await stroke(page, pathFor(3));                                  // 연습 정답 1→2
  await waitState(page, s => !s.onboarding && s.sceneId !== 'practice-fixed' && !s.frozen, 8000);
  await shot(page, '02-main-start');
  for (let run = 1; run <= 2; run++) {
    const log = [];
    let guard = 0;
    while (guard++ < 40) {
      s = await S(page);
      if (s.phase !== 'playing') break;
      if (s.frozen) { await waitState(page, settled); continue; }
      const {kind, mask} = solve(s.prompt);
      let m = mask;
      if (run === 1 && s.order === 1 && s.attempted === false && s.lives === 3) m = mask === 14 ? 15 : (mask | ((~mask) & 15 & -((~mask) & 15)));  // 과잉 선택(정답+닮지 않은 날개 1)
      const before = s.sceneId;
      await stroke(page, pathFor(m));
      const after = await waitState(page, x => x.submissionCount > s.submissionCount);
      log.push({scene: before, kind, solverMask: mask, strokeMask: m, verdict: after.lastVerdict, lives: after.lives, solved: after.solved});
      if (log.length === 1) await shot(page, `0${run + 2}-run${run}-first-verdict`);
      await waitState(page, settled, 9000);
      const nx = await S(page);
      if (after.lastVerdict === 'wrong' && nx.phase === 'playing' && nx.sceneId !== before) throw new Error('wrong advanced scene');
    }
    s = await waitState(page, x => x.phase !== 'playing', 6000);
    await sleep(1500);
    await shot(page, `0${run + 4}-run${run}-end`);
    result.playthroughs.push({run, phase: s.phase, endReason: s.endReason, solved: s.solved, score: s.score, lives: s.lives,
      firstAttempt: `${s.firstAttemptCorrect}/${s.firstAttemptTotal}`, aa: `${s.aaFirstCorrect}/${s.aaFirstAttempted}`, steps: log});
    // 종료 뒤 붓 입력은 점수·정답 수를 바꾸지 않는다 → 0.9초 뒤 탭으로 재시작
    const lockBefore = await S(page);
    if (run === 1) {
      await stroke(page, pathFor(3));
      await sleep(400);
      const lockAfter = await S(page);
      result.edges.afterEndStrokeKeepsScore = lockAfter.phase !== 'playing' ? lockAfter.score === lockBefore.score : 'restarted';
      if (lockAfter.phase === 'playing') { /* 0.9초가 지나 재시작 탭으로 처리됨 */ }
      else { await stroke(page, [[195, 700], [196, 700]], 1); }
      await waitState(page, x => x.phase === 'playing' && !x.frozen, 8000);
    }
  }
  result.practice = {idle: practiceIdle, afterWrong: practiceWrong};
  result.errorsA = page.errs;
  await page.close();
}

// ── B. 경계 ──
{
  const page = await open();
  await page.evaluate(() => window.__GAME_TEST__.start()); await sleep(500);
  let s = await S(page);
  await stroke(page, [[98, 302], [99, 302]], 1);                  // 날개 탭만
  await sleep(500);
  let t = await S(page);
  result.edges.tapOnlyNoSubmit = t.submissionCount === s.submissionCount && t.lives === s.lives;
  for (let i = 0; i < 3; i++) {                                    // 네 마리 전부 ×3 → 깃 0
    s = await waitState(page, settled);
    if (s.phase !== 'playing') break;
    await stroke(page, [C[0], C[1], C[3], C[2]]);
    await waitState(page, x => x.submissionCount > s.submissionCount);
  }
  s = await waitState(page, x => x.phase !== 'playing', 9000);
  result.edges.allFourThriceEnds = {phase: s.phase, endReason: s.endReason, lives: s.lives};
  await stroke(page, pathFor(3)); await sleep(300);                // 0.9초 안 입력: 잠금
  t = await S(page);
  result.edges.inputLockedAfterLoss = t.phase === s.phase && t.score === s.score && t.solved === s.solved;
  await sleep(1200);
  await stroke(page, [[195, 700], [196, 700]], 1); await sleep(500);
  t = await S(page);
  result.edges.restartByTap = t.phase === 'playing' && t.lives === 3 && t.solved === 0;
  result.errorsB = page.errs;
  await page.close();
}

// ── C. 무뇌 봇(실포인터) ──
const cyc = [[0, 1], [1, 3], [3, 2], [2, 0]];
const policies = {
  'fixed-stroke-1-2': (k) => [C[0], C[1]],
  'cycle': (k) => cyc[k % 4].map(i => C[i]),
  'random': (k) => Array.from({length: 2 + Math.floor(Math.random() * 3)}, () => [Math.random() * 390, 150 + Math.random() * 600]),
  'all-four': (k) => [C[0], C[1], C[3], C[2]],
};
async function runBot(name, fn) {
  const page = await open();
  await page.evaluate(() => window.__GAME_TEST__.start()); await sleep(400);
  let fa = 0, fo = 0, completed = 0, subs = 0, k = 0, runs = 0;
  while (runs < N) {
    let s = await waitState(page, settled, 9000);
    if (s.phase !== 'playing') {
      fa += s.firstAttemptTotal; fo += s.firstAttemptCorrect; if (s.solved >= 10) completed++;
      runs++;
      await sleep(950); await stroke(page, [[195, 760], [196, 760]], 1);
      await waitState(page, x => x.phase === 'playing' && x.solved === 0 && !x.frozen, 6000);
      continue;
    }
    const pts = fn(k++);
    await stroke(page, pts, 5);
    const t = await waitState(page, x => x.submissionCount > s.submissionCount || x.phase !== 'playing', 900);
    if (t.submissionCount > s.submissionCount) subs++;
  }
  const rate = fa ? fo / fa : 0;
  result.bots.push({policy: name, runs, firstAttempts: fa, firstCorrect: fo, firstRate: Number(rate.toFixed(4)), chance: Number((1 / 14).toFixed(4)), completionRate: completed / runs, submissions: subs, errors: page.errs.length});
  await page.close();
}
fs.writeFileSync(path.join(HERE, 'pointer-selftest-results.json'), JSON.stringify(result, null, 2));
await Promise.all(Object.entries(policies).map(([n, f], i) => sleep(i * 4000).then(() => runBot(n, f))));
// 연타(같은 자리 탭 30회)·무입력(25초)
{
  const page = await open();
  await page.evaluate(() => window.__GAME_TEST__.start()); await sleep(400);
  const s0 = await S(page);
  for (let i = 0; i < 30; i++) { await stroke(page, [[98, 302], [98, 302]], 1); await sleep(60); }
  const s1 = await S(page);
  result.bots.push({policy: 'mash-tap(30 taps)', submissions: s1.submissionCount - s0.submissionCount, solved: s1.solved, firstAttempts: s1.firstAttemptTotal});
  await sleep(25000);
  const s2 = await S(page);
  result.bots.push({policy: 'no-input(25s)', submissions: s2.submissionCount - s1.submissionCount, solved: s2.solved, firstAttempts: s2.firstAttemptTotal, phase: s2.phase});
  await page.close();
}
fs.writeFileSync(path.join(HERE, 'pointer-selftest-results.json'), JSON.stringify(result, null, 2));
console.log(JSON.stringify({playthroughs: result.playthroughs.map(p => ({...p, steps: p.steps.length, firstSteps: p.steps.slice(0, 2)})), practice: result.practice, edges: result.edges, bots: result.bots, errA: result.errorsA, errB: result.errorsB}, null, 1));
await browser.close(); server.close?.();
