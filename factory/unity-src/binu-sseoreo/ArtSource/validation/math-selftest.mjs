#!/usr/bin/env node
// C# BinuRules와 같은 624개 제약 만족 풀을 독립 정수식으로 전수 검산한다.
import fs from 'node:fs';
import path from 'node:path';

const out = path.join(process.cwd(), 'factory/unity-src/binu-sseoreo/ArtSource/validation/math-results.json');
const problems = [];
for (let a = 2; a <= 5; a++) for (let b = a; b <= 5; b++) for (let cut = 1; cut <= 24; cut++) {
  for (const piece of ['lower', 'upper']) {
    const pieceH = piece === 'lower' ? cut : 25 - cut;
    problems.push({ solid: 'rect', piece, a, b, cut, base: a * b, target: a * b * pieceH });
  }
}
for (const r of [1, 3, 4]) for (let cut = 1; cut <= 24; cut++) {
  for (const piece of ['lower', 'upper']) {
    const pieceH = piece === 'lower' ? cut : 25 - cut;
    problems.push({ solid: 'cylinder', piece, r, cut, base: r * r, target: r * r * pieceH });
  }
}

function correct(p, h) {
  const pieceH = p.piece === 'lower' ? h : 25 - h;
  return p.base * pieceH === p.target;
}

function misconception(p, h) {
  if (h === p.cut) return '';
  const pieceH = p.piece === 'lower' ? p.cut : 25 - p.cut;
  if (p.piece === 'upper' && h === pieceH) return 'upper_piece_height_as_cut';
  if (p.solid === 'rect' && p.piece === 'lower' && p.target % (p.a + p.b) === 0 && h === p.target / (p.a + p.b)) return 'rectangle_side_sum_as_area';
  if (p.solid === 'cylinder' && p.piece === 'lower' && p.target % p.r === 0 && h === p.target / p.r) return 'radius_not_squared';
  if (p.solid === 'cylinder' && p.piece === 'lower' && p.target % (2 * p.r) === 0 && h === p.target / (2 * p.r)) return 'circumference_as_base_area';
  return 'volume_height_mismatch';
}

let uniqueFailures = 0;
let accidentalTruths = 0;
const misconceptionCounts = {};
for (const p of problems) {
  const solutions = [];
  for (let h = 1; h <= 24; h++) {
    if (correct(p, h)) solutions.push(h);
    const id = misconception(p, h);
    if (h === p.cut && id) accidentalTruths++;
    if (h !== p.cut && id !== 'volume_height_mismatch') misconceptionCounts[id] = (misconceptionCounts[id] || 0) + 1;
  }
  if (solutions.length !== 1 || solutions[0] !== p.cut) uniqueFailures++;
}
const result = {
  passed: problems.length === 624 && uniqueFailures === 0 && accidentalTruths === 0,
  problems: problems.length,
  checkedCandidateCuts: problems.length * 24,
  uniqueFailures,
  accidentalTruths,
  misconceptionCounts,
};
fs.writeFileSync(out, `${JSON.stringify(result, null, 2)}\n`);
console.log(JSON.stringify(result, null, 2));
if (!result.passed) process.exitCode = 1;
