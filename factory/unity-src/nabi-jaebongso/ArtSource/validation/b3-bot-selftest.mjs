#!/usr/bin/env node
// NabiRules / CandidateGeometryOkay의 정수 변 ID + 2% 실측 판정을 1:1로 옮긴 B3 감사.
// 각 정책 200판 × 6문항의 독립 첫 제출을 세며, 봇은 문제/정답/좌표를 읽지 않는다.
import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const rulesPath = join(here, '../../Scripts/NabiRules.cs');
const gamePath = join(here, '../../Scripts/NabiJaebongsoGame.cs');
const outPath = join(here, 'b3-bot-results.json');
const GAMES = 200;
const QUESTIONS = 6;
const CHANCE_UPPER = 1 / 6; // 서로 다른 3변의 순서쌍 6개 중 올바른 두 중심 배정 1개.

const sidePool = [
  [5,6,7],[4,6,7],[5,7,9],[6,7,8],[6,8,9],
  [7,8,10],[7,9,11],[8,9,10],[8,10,11],[9,10,12],
  [5,8,10],[6,9,10],[7,10,12],[8,11,13],[9,11,13],
  [10,12,15],[7,11,13],[8,12,13],[9,12,14],[10,13,15],
];
const edgeBetween = (a, b) => {
  const lo = Math.min(a,b), hi = Math.max(a,b);
  return lo === 0 && hi === 1 ? 0 : lo === 0 && hi === 2 ? 1 : 2;
};
const edgeLength = (p, e) => e === 0 ? p.ab : e === 1 ? p.ac : p.bc;

function buildBank() {
  const bank = [];
  for (let i = 0; i < 360; i++) {
    const band = Math.floor(i / 120) + 1;
    const [ab, ac, bc] = sidePool[(i * 7 + band * 3) % sidePool.length];
    const baseEdge = band < 3 ? 0 : (i * 5 + Math.floor(i / 9)) % 3;
    let u, v, w;
    if (baseEdge === 0) [u,v,w] = [0,1,2];
    else if (baseEdge === 1) [u,v,w] = [0,2,1];
    else [u,v,w] = [1,2,0];
    const swapBase = (Math.floor(i / 3) + band) % 2 === 1;
    const left = swapBase ? v : u, right = swapBase ? u : v;
    bank.push({ id: i + 1, band, ab, ac, bc, baseEdge,
      expectedLeftEdge: edgeBetween(left,w), expectedRightEdge: edgeBetween(right,w) });
  }
  return bank;
}
const bank = buildBank();

function xorshift(seed) {
  let x = seed >>> 0;
  return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 0x100000000; };
}
function deck(run) {
  const r = xorshift(0x6e616269 ^ run);
  const out = [];
  for (let band = 1; band <= 3; band++) {
    const pool = bank.filter((p) => p.band === band);
    const a = Math.floor(r() * pool.length);
    let b = Math.floor(r() * (pool.length - 1)); if (b >= a) b++;
    out.push(pool[a], pool[b]);
  }
  return out;
}
function judge(p, action) {
  if (!action) return false;
  if (action.edges[0] !== p.expectedLeftEdge || action.edges[1] !== p.expectedRightEdge) return false;
  const base = edgeLength(p, p.baseEdge), leftR = edgeLength(p, action.edges[0]), rightR = edgeLength(p, action.edges[1]);
  const tol = base * 0.02;
  const left = { x: -base / 2, y: 0 }, right = { x: base / 2, y: 0 };
  const dl = Math.hypot(action.point.x - left.x, action.point.y - left.y);
  const dr = Math.hypot(action.point.x - right.x, action.point.y - right.y);
  return Math.abs(dl - leftR) <= tol + 1e-12 && Math.abs(dr - rightR) <= tol + 1e-12;
}
function policy(name, seed) {
  const rand = xorshift(seed); let n = 0;
  const pairs = [[0,1],[1,2],[2,0],[1,0],[2,1],[0,2]];
  const ys = [-5,-3,0,3,5,1.5];
  return () => {
    if (name === 'idle') return null;
    if (name === 'fixed') return { edges: [1,2], point: { x: 0, y: 0 } };
    if (name === 'cycle4') { const k = n++ % 4; return { edges: pairs[k], point: { x: 0, y: ys[k] } }; }
    const a = Math.floor(rand() * 3); let b = Math.floor(rand() * 2); if (b >= a) b++;
    return { edges: [a,b], point: { x: rand() * 16 - 8, y: rand() * 12 - 6 } };
  };
}
function simulate(name) {
  let firstAttempts = 0, firstCorrect = 0, clears = 0, solvedTotal = 0;
  for (let run = 0; run < GAMES; run++) {
    const audit = policy(name, 20261025 ^ run);
    for (const p of deck(run)) {
      const action = audit();
      if (action) { firstAttempts++; if (judge(p, action)) firstCorrect++; }
    }
    if (name === 'idle') continue;
    const play = policy(name, 0x9e3779b9 ^ run);
    let lives = 3, solved = 0, firstMisses = 0;
    for (const p of deck(run)) {
      const first = play();
      if (judge(p, first)) { solved++; continue; }
      lives--; firstMisses++;
      if (lives <= 0 || firstMisses >= 2) break;
      while (lives > 0) {
        if (judge(p, play())) { solved++; break; }
        lives--;
      }
      if (lives <= 0) break;
    }
    solvedTotal += solved;
    if (solved === QUESTIONS) clears++;
  }
  const firstRate = firstAttempts ? firstCorrect / firstAttempts : 0;
  return {
    policy: name,
    games: GAMES,
    firstAttempts,
    firstCorrect,
    firstRate: +firstRate.toFixed(6),
    chanceUpper: +CHANCE_UPPER.toFixed(6),
    clears,
    clearRate: +(clears / GAMES).toFixed(6),
    meanSolved: +(solvedTotal / GAMES).toFixed(4),
    progress: name === 'idle' ? 0 : solvedTotal,
    passed: name === 'idle' ? firstAttempts === 0 && solvedTotal === 0 && clears === 0 : firstRate <= CHANCE_UPPER && clears === 0,
  };
}

const policies = ['fixed','cycle4','random','idle'].map(simulate);
const result = {
  runId: `nabi-jaebongso-b3-${new Date().toISOString()}`,
  rulesSha256: createHash('sha256').update(readFileSync(rulesPath)).digest('hex'),
  judgeSha256: createHash('sha256').update(readFileSync(gamePath)).digest('hex'),
  method: 'NabiRules 360-bank / CandidateGeometryOkay mirror; 200 games per policy, six independent first submissions per game; bots do not inspect problem, correspondence, or geometry; both segment ends are generously treated as the same blind point, so measured rates are an upper bound for real random dragging.',
  chanceModel: 'ordered distinct source-edge pair: 1/6 upper bound; continuous two-annulus 2% intersection requirement only lowers chance further',
  policies,
  passed: policies.every((p) => p.passed),
};
writeFileSync(outPath, JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
if (!result.passed) process.exitCode = 1;
