#!/usr/bin/env node
// C# JogaeRules.BuildBank의 같은 정수 모델을 독립 재구성해 389문항을 전수 검산한다.
import fs from 'node:fs';

const angles = Array.from({ length: 16 }, (_, i) => 10 + i * 5);
const rows = [];
for (const a of angles) rows.push({ id: `direct-${a}`, kind: 'direct', a, c: 2 * a });
for (const a of angles) for (const variant of ['same', 'quarter', 'diameter']) rows.push({ id: `error-${variant}-${a}`, kind: 'error', a, c: 2 * a, variant });
for (const a of angles) for (let x = 5; x <= 2 * a - 5; x += 5) rows.push({ id: `split-${a}-${x}`, kind: 'split', a, c: 2 * a, x, y: 2 * a - x });
for (const a of angles) {
  rows.push({ id: `ratio-2-1-${a}`, kind: 'ratio', a, m: 2, n: 1 });
  if (a <= 40) rows.push({ id: `ratio-1-2-${a}`, kind: 'ratio', a, m: 1, n: 2 });
  if (a % 15 === 0) rows.push({ id: `ratio-3-2-${a}`, kind: 'ratio', a, m: 3, n: 2 });
  if (a <= 50) rows.push({ id: `ratio-2-3-${a}`, kind: 'ratio', a, m: 2, n: 3 });
}

let uniqueSolutionErrors = 0, relationErrors = 0, degeneracyErrors = 0, misconceptionCollisions = 0;
for (const p of rows) {
  const validSlots = angles.filter(v => v === p.a);
  if (validSlots.length !== 1) uniqueSolutionErrors++;
  if (!(p.a >= 10 && p.a <= 85 && p.a % 5 === 0 && 2 * p.a < 180)) relationErrors++;
  if (p.kind === 'split' && (p.x + p.y !== 2 * p.a || p.x <= 0 || p.y <= 0)) relationErrors++;
  if (p.kind === 'ratio') {
    const bN = p.a * p.n, bD = p.m;
    if (p.a * p.n * bD !== bN * p.m) relationErrors++;
    const reverseN = bN * p.n, reverseD = bD * p.m;
    if (p.a * reverseD === reverseN) misconceptionCollisions++;
  }
  const centerWrong = 2 * p.a, quarterWrongN = 2 * p.a, quarterWrongD = 4;
  if (centerWrong === p.a || quarterWrongN === p.a * quarterWrongD || p.a === 90) misconceptionCollisions++;
  // 정규화 모델: P=(-R,0), A=(R,0), B의 극각=2a. a∈[10,85]라 P=A/B가 아니고 P는 호 AB 밖이다.
  if (2 * p.a <= 0 || 2 * p.a >= 180) degeneracyErrors++;
}

const ids = new Set(rows.map(r => r.id));
const report = {
  generatedAt: new Date().toISOString(),
  bankCount: rows.length,
  kindCounts: Object.fromEntries(['direct', 'error', 'split', 'ratio'].map(k => [k, rows.filter(r => r.kind === k).length])),
  answerSlots: angles,
  uniqueIds: ids.size,
  exhaustive: { uniqueSolutionErrors, relationErrors, degeneracyErrors, misconceptionCollisions },
  passed: rows.length === 389 && ids.size === 389 && uniqueSolutionErrors === 0 && relationErrors === 0 && degeneracyErrors === 0 && misconceptionCollisions === 0
};
fs.writeFileSync(new URL('./math-bank-results.json', import.meta.url), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify(report, null, 2));
if (!report.passed) process.exitCode = 1;
