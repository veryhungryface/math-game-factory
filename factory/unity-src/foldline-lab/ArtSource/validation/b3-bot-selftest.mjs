#!/usr/bin/env node
// 폴드라인 B3: FoldlineRules.RuntimeDeck/Judge의 선 공간을 1:1로 옮긴 결정적 감사.
// 연습은 제외하고, 각 새 돛의 첫 유효 제출만 첫 시도 분모에 넣는다.
// 봇은 prompt/정답/삼각형을 읽지 않고 (theta, signed offset)만 낸다.

import { createHash } from 'node:crypto';
import { readFileSync, writeFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const rulesPath = join(here, '../../Scripts/FoldlineRules.cs');
const outPath = join(here, 'b3-bot-results.json');
const RUNS = 1000;
const POLICY_SEED = 20261016; // 제작 회차 KST 날짜 고정 시드
const L = 9.5;
const ANGLE_TOL = 4;
const OFFSET_TOL = 0.04 * L;
const chanceUpper = (2 * ANGLE_TOL / 180) * (2 * OFFSET_TOL / L);

const v = (x, y) => ({ x, y });
const add = (a, b) => v(a.x + b.x, a.y + b.y);
const sub = (a, b) => v(a.x - b.x, a.y - b.y);
const mul = (a, k) => v(a.x * k, a.y * k);
const len = a => Math.hypot(a.x, a.y);
const norm = a => { const m = len(a); return m < 1e-9 ? v(0, 1) : mul(a, 1 / m); };
const dot = (a, b) => a.x * b.x + a.y * b.y;
const rot = (p, deg) => {
  const r = deg * Math.PI / 180, c = Math.cos(r), s = Math.sin(r);
  return v(p.x * c - p.y * s, p.x * s + p.y * c);
};
const line = (point, direction) => ({ point, direction: norm(direction) });
const through = (a, b) => line(a, sub(b, a));
const perpBisector = (a, b) => {
  const d = sub(b, a);
  return line(mul(add(a, b), 0.5), v(-d.y, d.x));
};
const angleBisector = (vertex, p, q) =>
  line(vertex, add(norm(sub(p, vertex)), norm(sub(q, vertex))));
const normal = x => v(-x.direction.y, x.direction.x);
const offset = x => dot(normal(x), x.point);
const judge = (x, target) => {
  const cosine = Math.max(0, Math.min(1, Math.abs(dot(x.direction, target.direction))));
  const angleError = Math.acos(cosine) * 180 / Math.PI;
  const xn = normal(x), tn = normal(target);
  let xc = offset(x);
  if (dot(xn, tn) < 0) xc = -xc;
  const offsetError = Math.abs(xc - offset(target));
  return angleError <= ANGLE_TOL + 1e-9 && offsetError <= OFFSET_TOL + 1e-9;
};
const paramLine = (theta, signedOffset) => {
  const d = rot(v(1, 0), theta), n = v(-d.y, d.x);
  return line(mul(n, signedOffset), d);
};

function deck(serial) {
  const s = Math.abs(serial);
  const r1 = 28 + (s * 37 % 116);
  const axis1 = rot(v(0, 1), r1);
  const normal1 = v(-axis1.y, axis1.x);
  const side1 = (s % 2 === 0 ? 1 : -1) * (.95 + (s % 3) * .13);
  const sh1 = add(mul(normal1, side1), mul(axis1, -.2 + (s % 3) * .2));
  const a1 = add(rot(v(0, 3), r1), sh1);
  const b1 = add(rot(v(-3, -2), r1), sh1);
  const c1 = add(rot(v(3, -2), r1), sh1);

  const r3 = -18 + (s * 13 % 37);
  const sh3 = rot(v(-1.05 - (s % 3) * .12, .45 + (s % 2) * .12), r3);
  const oa = add(rot(v(-3.2, -1.7), r3), sh3);
  const ob = add(rot(v(3, -1.7), r3), sh3);
  const oc = add(rot(v(1.45, -.05), r3), sh3);

  const r4 = 14 + (s * 17 % 58);
  const sh4 = rot(v(-1.0 - (s % 3) * .1, .55 + (s % 2) * .12), r4);
  const ia = add(rot(v(0, 3), r4), sh4);
  const ib = add(rot(v(-3.1, -2.1), r4), sh4);
  const ic = add(rot(v(2.75, -1.65), r4), sh4);

  return [
    { kind: 'line', target: through(a1, mul(add(b1, c1), .5)) },
    { kind: 'flag', target: null },
    { kind: 'line', target: perpBisector(oa, ob) },
    { kind: 'line', target: perpBisector(oa, oc) },
    { kind: 'line', target: angleBisector(ia, ib, ic) },
    { kind: 'line', target: angleBisector(ib, ia, ic) }
  ];
}

function xorshift(seed) {
  let x = seed >>> 0;
  return () => {
    x ^= x << 13; x ^= x >>> 17; x ^= x << 5;
    return (x >>> 0) / 0x100000000;
  };
}

function blindPolicy(name, seed) {
  const random = xorshift(seed);
  let action = 0;
  return () => {
    let theta, u;
    if (name === 'fixed') { theta = 0; u = 0; }
    else if (name === 'cycle36') { theta = (action % 36) * 5; u = 0; }
    else { theta = random() * 180; u = (random() - .5) * L; }
    action++;
    return paramLine(theta, u);
  };
}

function simulate(name) {
  let firstAttempts = 0, firstCorrect = 0, clears = 0, solved = 0;
  if (name !== 'idle') {
    // 생명 0으로 뒤 문항이 가려지지 않게 여섯 문항 각각의 첫 입력도 독립 감사한다.
    const auditInput = blindPolicy(name, POLICY_SEED);
    for (let run = 0; run < RUNS; run++) {
      for (const task of deck(20261017 + run)) {
        firstAttempts++;
        if (task.kind === 'line' && judge(auditInput(), task.target)) firstCorrect++;
        else if (task.kind === 'flag') auditInput();
      }
    }
  }
  const runtimeInput = name === 'idle' ? null : blindPolicy(name, POLICY_SEED ^ 0x9e3779b9);
  for (let run = 0; run < RUNS; run++) {
    if (name === 'idle') continue;
    let lives = 3, taskIndex = 0;
    const tasks = deck(20261017 + run);
    while (lives > 0 && taskIndex < tasks.length) {
      const task = tasks[taskIndex];
      let correct = false;
      while (lives > 0 && !correct) {
        correct = task.kind === 'line' && judge(runtimeInput(), task.target);
        if (!correct) lives--;
      }
      if (correct) { solved++; taskIndex++; }
    }
    if (taskIndex === tasks.length) clears++;
  }
  const firstRate = firstAttempts ? firstCorrect / firstAttempts : 0;
  return {
    policy: name,
    runs: RUNS,
    firstAttempts,
    firstCorrect,
    firstRate: +firstRate.toFixed(6),
    clears,
    clearRate: clears / RUNS,
    meanSolved: +(solved / RUNS).toFixed(4),
    progress: name === 'idle' ? 0 : solved,
    passed: name === 'idle'
      ? solved === 0 && firstAttempts === 0 && clears === 0
      : firstRate <= chanceUpper + 1e-12 && clears === 0
  };
}

const policies = ['fixed', 'cycle36', 'random', 'idle'].map(simulate);
const result = {
  runId: `foldline-lab-b3-${new Date().toISOString()}`,
  rulesSha256: createHash('sha256').update(readFileSync(rulesPath)).digest('hex'),
  method: 'FoldlineRules.RuntimeDeck/Judge 1:1 JS mirror; practice excluded; every one of the six sails receives one independent blind first input (6,000 samples/policy), so life loss cannot hide later tasks; a separate runtime pass measures progress/clear with three lives; blind bots never inspect task geometry or use the safety flag.',
  lineSpace: {
    thetaDegrees: '[0,180)',
    signedOffset: `[-${L / 2},${L / 2}]`,
    angleToleranceDegrees: ANGLE_TOL,
    offsetTolerance: OFFSET_TOL,
    theoreticalRandomUpper: +chanceUpper.toFixed(9)
  },
  policies,
  passed: policies.every(x => x.passed)
};
writeFileSync(outPath, JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
if (!result.passed) process.exitCode = 1;
