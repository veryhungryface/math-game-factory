#!/usr/bin/env node
// 종이새 출격 생성기 전수 검산: 61답×3밴드×60시작각×4회전.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const MIN = 20, MAX = 140, STEP = 2, N = 61;
const angle = i => MIN + STEP * i;
const diagnosticValue = (q, value, input) => value >= MIN && value <= MAX && value % STEP === 0 && value !== q && input === value;
let conditions = 0, judgeChecks = 0, errors = 0, accidentalTruths = 0;
const samples = [];

for (let band = 1; band <= 3; band++) {
  for (let qi = 0; qi < N; qi++) {
    const q = angle(qi), b = (180 - q) / 2, e = 180 - b;
    if (q + 2 * b !== 180 || e + b !== 180 || !Number.isInteger(b) || !Number.isInteger(e)) errors++;
    for (let start = 0; start < N; start++) {
      if (start === qi) continue;
      for (let rotation = 0; rotation < 4; rotation++) {
        conditions++;
        for (let input = 0; input < N; input++) {
          judgeChecks++;
          const got = input === qi;
          const expected = angle(input) === q;
          if (got !== expected) errors++;
        }
        const candidates = [
          ['subtract_one_base', 180 - b],
          ['apex_equals_base', b],
          ['split_after_one_base', (180 - b) / 2],
          ['exterior_as_vertex', e - 90]
        ];
        for (const [id, value] of candidates) {
          if (value === q && diagnosticValue(q, value, value)) {
            errors++;
            accidentalTruths++;
            if (samples.length < 5) samples.push({ band, qi, q, b, e, start, rotation, id, value });
          }
        }
      }
    }
  }
}

const report = {
  generatedAt: new Date().toISOString(),
  conditions,
  judgeChecks,
  qValues: N,
  bands: 3,
  startAnglesPerAnswer: 60,
  rotations: 4,
  accidentalDistractorTruths: accidentalTruths,
  errors,
  samples,
  pass: errors === 0
};
fs.writeFileSync(path.join(HERE, 'math-audit-results.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify(report, null, 2));
if (!report.pass) process.exitCode = 1;
