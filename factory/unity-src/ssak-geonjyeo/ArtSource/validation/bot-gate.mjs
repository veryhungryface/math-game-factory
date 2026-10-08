import fs from 'node:fs/promises';
import path from 'node:path';

// Deterministic mirror of SsakRules: 12 ordered outcomes, exact integer masks,
// two single-event + two disjoint-sum + three product tides per run.
const U = 12;
const RUNS = 200;
const LIVES = 3;
const bitCount = n => { let c = 0; while (n) { c += n & 1; n >>>= 1; } return c; };
const rectangle = (xm, ym) => {
  let mask = 0;
  for (let a = 0; a < 4; a++) for (let b = 0; b < 3; b++)
    if ((xm & (1 << a)) && (ym & (1 << b))) mask |= 1 << (a * 3 + b);
  return mask;
};
const choose = (n, k) => {
  let x = 1;
  for (let i = 1; i <= k; i++) x = x * (n - k + i) / i;
  return x;
};
const makeRng = seed => () => {
  seed |= 0; seed = seed + 0x6D2B79F5 | 0;
  let t = Math.imul(seed ^ seed >>> 15, 1 | seed);
  t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
  return ((t ^ t >>> 14) >>> 0) / 4294967296;
};
const pick = (rng, list) => list[Math.floor(rng() * list.length)];

const singles = [];
for (let a = 0; a < 4; a++) singles.push(rectangle(1 << a, 7));
for (let b = 0; b < 3; b++) singles.push(rectangle(15, 1 << b));

const rects = [];
const seen = new Set();
for (let xm = 1; xm < 16; xm++) for (let ym = 1; ym < 8; ym++) {
  const m = rectangle(xm, ym), c = bitCount(m);
  if (c >= 1 && c <= 4 && !seen.has(m)) { seen.add(m); rects.push(m); }
}
const sums = [];
for (let i = 0; i < rects.length; i++) for (let j = i + 1; j < rects.length; j++) {
  const a = rects[i], b = rects[j], n = bitCount(a) + bitCount(b);
  if (!(a & b) && n >= 3 && n <= 6) sums.push(a | b);
  if (7 + sums.length >= 307) break;
}
const products = [];
for (let xm = 1; xm < 16; xm++) for (let ym = 1; ym < 8; ym++) {
  const n = bitCount(xm) * bitCount(ym);
  if (n >= 3 && n <= 6) products.push(rectangle(xm, ym));
}

const sampleDistinct = (rng, pool, n) => {
  const chosen = [];
  while (chosen.length < n) {
    const m = pick(rng, pool);
    if (!chosen.includes(m)) chosen.push(m);
  }
  return chosen;
};
const deck = rng => [
  ...sampleDistinct(rng, singles, 2),
  ...sampleDistinct(rng, sums, 2),
  ...sampleDistinct(rng, products, 3),
];
const shuffled = rng => {
  const p = Array.from({ length: U }, (_, i) => i);
  for (let i = U - 1; i > 0; i--) { const j = Math.floor(rng() * (i + 1)); [p[i], p[j]] = [p[j], p[i]]; }
  return p;
};
const selectionMask = (positions, permutation) => positions.reduce((m, p) => m | (1 << permutation[p]), 0);

function runPolicy(name, seed) {
  const rng = makeRng(seed);
  let firstAttempts = 0, firstCorrect = 0, expectedChance = 0;
  let completed = 0, victories = 0, totalProgress = 0;
  for (let run = 0; run < RUNS; run++) {
    if (name === 'idle') { firstAttempts++; continue; }
    let lives = LIVES, solved = 0, mastery = 0, cycle = 0;
    for (const answer of deck(rng)) {
      const k = bitCount(answer);
      const perm = shuffled(rng);
      expectedChance += 1 / choose(U, k);
      firstAttempts++;
      let first = true, solvedThis = false;
      while (lives > 0 && !solvedThis) {
        let capacity, pos;
        if (name === 'fixed') {
          capacity = 1;
          pos = [0];
        } else if (name === 'cycle') {
          capacity = cycle % 4 + 1;
          pos = Array.from({ length: capacity }, (_, i) => (i + cycle) % U);
          cycle++;
        } else {
          capacity = 1 + Math.floor(rng() * 12);
          const selectedCount = Math.floor(rng() * 13);
          pos = shuffled(rng).slice(0, selectedCount);
        }
        const correct = capacity === k && selectionMask(pos, perm) === answer;
        if (first && correct) { firstCorrect++; mastery++; }
        first = false;
        if (correct) { solved++; solvedThis = true; }
        else lives--;
      }
      if (!solvedThis) break;
    }
    totalProgress += solved;
    if (solved === 7) completed++;
    if (solved === 7 && mastery >= 5) victories++;
  }
  const chance = name === 'idle' ? 0 : expectedChance / firstAttempts;
  return {
    runs: RUNS,
    firstAttempts,
    firstCorrect,
    firstAttemptRate: firstCorrect / firstAttempts,
    conservativeChanceRate: name === 'idle' ? 0 : 1 / choose(12, 3),
    deckWeightedChanceRate: chance,
    completionRate: completed / RUNS,
    victoryRate: victories / RUNS,
    meanSolvedTides: totalProgress / RUNS,
    pass: firstCorrect / firstAttempts <= (name === 'idle' ? 0 : 1 / choose(12, 3)) && (name !== 'idle' || totalProgress === 0),
  };
}

const result = {
  model: 'exact 12-outcome set equality; fixed/cycle/random inputs do not inspect labels; reported chance grants bots the correct k (conservative upper bound)',
  policies: {
    fixed: runPolicy('fixed', 0x51A7),
    cycle: runPolicy('cycle', 0xC1C1E),
    random: runPolicy('random', 0xB07),
    idle: runPolicy('idle', 0x1D1E),
  },
};
result.pass = Object.values(result.policies).every(x => x.pass);
const out = path.join(path.dirname(new URL(import.meta.url).pathname), 'bot-gate-result.json');
await fs.writeFile(out, JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
if (!result.pass) process.exitCode = 1;
