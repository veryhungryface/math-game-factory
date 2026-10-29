// B3 무뇌 봇: 정답을 읽지 않는 실제 포인터 입력 정책별 200판(첫 역의 첫 제출).
// start() 는 각 독립 시행 초기화에만 쓰고 answerCorrect/answerWrong 은 쓰지 않는다(점검역 정책의 사전 진행 제외).
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { launch, startServer, openGame, pull, state, sleep, PUBLIC, SLUG, rail } from './common.mjs';

const TRIALS = Number(process.env.TRIALS || 200);
const INSPECT_TRIALS = Number(process.env.INSPECT_TRIALS || 120);
const CHANCE = 1 / 24;
const W = 390, H = 844;

function rng(seed) { let x = seed >>> 0; return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; }; }
function wilson(c, n) { if (!n) return [0, 0]; const z = 1.96, p = c / n, d = 1 + z * z / n; const m = (p + z * z / (2 * n)) / d, h = z * Math.sqrt(p * (1 - p) / n + z * z / (4 * n * n)) / d; return [m - h, m + h]; }
function tail(c, n, p) { let pr = Math.pow(1 - p, n), below = 0; for (let k = 0; k < c; k++) { below += pr; pr *= ((n - k) / (k + 1)) * (p / (1 - p)); } return Math.max(0, Math.min(1, 1 - below)); }
function artifactHash() {
  const dir = path.join(PUBLIC, 'g', SLUG), build = path.join(dir, 'Build');
  const rels = ['index.html', ...fs.readdirSync(build).sort().map(n => `Build/${n}`)];
  const h = crypto.createHash('sha256');
  for (const rel of rels) { h.update(rel); h.update(fs.readFileSync(path.join(dir, rel))); }
  return h.digest('hex');
}

async function waitOpen(page, round) {
  await page.waitForFunction(r => { const s = window.__GAME_TEST__.getState(); return s.inputOpen && s.round === r && !s.dragging; }, { timeout: 15000 }, round);
}

async function runPolicy(browser, server, kind) {
  const { page, errors } = await openGame(browser, server, W, H, kind);
  const R = rng(20261029 + kind.length * 977);
  let correct = 0, total = 0, completed = 0, cancels = 0;
  const hist = Array(25).fill(0), tHist = Array(25).fill(0);
  const trials = kind.startsWith('inspect') ? INSPECT_TRIALS : TRIALS;
  for (let i = 0; i < trials; i++) {
    await page.evaluate(() => window.__GAME_TEST__.start());
    let round = 1;
    if (kind.startsWith('inspect')) {
      // 점검역(5역)까지 훅으로 진행(측정 대상 아님) → 5역의 첫 제출만 실제 포인터로 잰다
      await page.evaluate(() => { const g = window.__GAME_TEST__; for (let k = 0; k < 4; k++) g.answerCorrect(); });
      round = 5;
    }
    await waitOpen(page, round);
    const s0 = await state(page);
    const from = s0.selectedLength;
    tHist[Number(/t(\d+)/.exec(s0.problemId)[1])]++; // 기록만 — 정책은 이 값을 읽지 않는다
    let v;
    if (kind === 'fixed') v = 12;
    else if (kind === 'cycle') v = (i % 24) + 1;
    else if (kind === 'random') v = 1 + Math.floor(R() * 24);
    else if (kind === 'inspect-keep') v = from;             // 견습 줄 q 를 그대로 확인·제출
    else if (kind === 'inspect-exclude') { do { v = 1 + Math.floor(R() * 24); } while (v === from); } // q 를 배제한 무작위
    hist[v]++;
    await pull(page, W, H, from, v, { wiggle: v === from });
    await page.waitForFunction(fa => window.__GAME_TEST__.getState().firstAttempts >= fa, { timeout: 4000 }, s0.firstAttempts + 1).catch(() => {});
    const s1 = await state(page);
    if (s1.firstAttempts === s0.firstAttempts + 1) { total++; if (s1.firstCorrect === s0.firstCorrect + 1) correct++; }
    else cancels++;
    if (s1.phase === 'clear') completed++;
  }
  await page.close();
  const pv = tail(correct, total, CHANCE);
  return { trials, firstAttemptCorrect: correct, firstAttemptTotal: total, firstAttemptRate: total ? correct / total : 0,
    wilson95: wilson(correct, total), pValueAboveChance: pv, chance: CHANCE, completionRate: completed / trials,
    cancels, valueHistogram: hist.slice(1), answerHistogram: tHist.slice(1), consoleErrors: errors,
    passed: total === trials && pv >= 0.05 && errors.length === 0 };
}

async function runIdle(browser, server) {
  const { page, errors } = await openGame(browser, server, W, H, 'idle');
  let progressed = 0;
  for (let i = 0; i < TRIALS; i++) {
    const changed = await page.evaluate(async () => {
      const g = window.__GAME_TEST__; g.start();
      const a = g.getState(); await new Promise(r => setTimeout(r, 40)); const b = g.getState();
      return b.solved !== a.solved || b.firstAttempts !== a.firstAttempts || b.stationCount !== a.stationCount;
    });
    if (changed) progressed++;
  }
  // 한 판을 90초 넘게 방치: 진도 0 이고 시간 종료로 끝나야 한다
  await page.evaluate(() => window.__GAME_TEST__.start());
  const t0 = Date.now();
  await page.waitForFunction(() => window.__GAME_TEST__.getState().phase === 'gameover', { timeout: 110000 }).catch(() => {});
  const fin = await state(page);
  await page.close();
  return { trials: TRIALS, progressed, longIdle: { seconds: Math.round((Date.now() - t0) / 1000), phase: fin.phase, result: fin.result, solved: fin.solved, firstAttempts: fin.firstAttempts },
    firstAttemptRate: 0, chance: CHANCE, completionRate: 0, consoleErrors: errors,
    passed: progressed === 0 && fin.solved === 0 && fin.firstAttempts === 0 && fin.result === 'time' && errors.length === 0 };
}

const server = await startServer();
const kinds = (process.env.POLICIES || 'fixed,cycle,random,inspect-keep,inspect-exclude').split(',');
const browsers = await Promise.all([...kinds, 'idle'].map(() => launch()));
const results = await Promise.all([
  ...kinds.map((k, i) => runPolicy(browsers[i], server, k)),
  runIdle(browsers[kinds.length], server),
]);
for (const b of browsers) await b.close();
server.close?.();
const runId = process.env.MGF_RUN_ID || `${SLUG}-pointer-bots-${new Date().toISOString()}`;
const artifact = artifactHash();
const out = { run_id: runId, artifact_hash: artifact, runId, artifactHashSha256: artifact,
  criterion: '정답을 읽지 않는 실제 포인터 첫 제출: 단측 이항검정에서 우연 수준 1/24 를 유의하게 넘지 않음(p≥.05), 무입력 진도 0',
  method: '390×844 Chrome. 지급 고리를 실제 mouse down/move/up 으로 끌어 놓는다. fixed=항상 12 cm, cycle=1→24 순환, random=1..24 균등, inspect-keep=5역 견습 줄 q 그대로(흔들었다 같은 눈금에 놓기), inspect-exclude=q 를 뺀 무작위. 점검역 정책은 1~4역을 훅으로 넘긴 뒤 5역 첫 제출만 잰다.',
  ...Object.fromEntries(kinds.map((k, i) => [k, results[i]])), idle: results[kinds.length] };
out.passed = [...kinds, 'idle'].every(k => out[k].passed);
fs.writeFileSync(path.join(path.dirname(new URL(import.meta.url).pathname), process.env.OUT || 'bot-results.json'), JSON.stringify(out, null, 2));
for (const k of [...kinds, 'idle']) console.log(k, JSON.stringify({ rate: out[k].firstAttemptRate, n: out[k].firstAttemptTotal, p: out[k].pValueAboveChance, ci: out[k].wilson95, done: out[k].completionRate, cancels: out[k].cancels, errors: out[k].consoleErrors?.length, progressed: out[k].progressed, longIdle: out[k].longIdle }));
console.log('passed', out.passed);
process.exit(0);
