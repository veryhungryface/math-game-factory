#!/usr/bin/env node
import fs from 'node:fs';

const sums = [4,5,6,7,8,9,10,11,12];
const products = [6,8,9,10,12,14,15,16,18,20,21,24];
let sumPairs = 0, productPairs = 0, accidentalTruths = 0;
const errors = [];

for (const target of sums) {
  let count = 0;
  for (let m = 1; m <= 6; m++) for (let n = 1; n <= 6; n++) {
    if (m + n !== target || m * n === target) continue;
    count++; sumPairs++;
    const wrong = [m * n, Math.max(m,n), Math.abs(m-n)];
    for (const v of wrong) if (v === target) accidentalTruths++;
  }
  if (!count) errors.push(`sum target ${target} has no pair`);
}

for (const target of products) {
  let count = 0;
  for (let m = 2; m <= 8; m++) for (let n = 2; n <= 8; n++) {
    if (m * n !== target || m + n === target) continue;
    count++; productPairs++;
    const wrong = [m + n, m, n, ...(m === n ? [m * (m + 1) / 2] : [])];
    for (const v of wrong) if (v === target) accidentalTruths++;
  }
  if (!count) errors.push(`product target ${target} has no pair`);
}

const result = {
  generatedAt: new Date().toISOString(),
  sumTargets: sums.length,
  productTargets: products.length,
  sumPairs,
  productPairs,
  accidentalTruths,
  errors,
  passed: errors.length === 0 && accidentalTruths === 0
};
const out = new URL('./math-audit-results.json', import.meta.url);
fs.writeFileSync(out, JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
if (!result.passed) process.exit(1);
