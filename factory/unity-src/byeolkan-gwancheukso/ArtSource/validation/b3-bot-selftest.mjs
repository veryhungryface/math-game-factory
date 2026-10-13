import fs from 'node:fs';

// B3 exact-graph simulation, 200 complete sessions per blind policy. The runtime uses the same
// invariants: both rack topologies, every marked contact, and fold state must match exactly.
const RUNS = 200;
let seed = 0x57a4c0de;
const rnd = () => ((seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0) / 2 ** 32);

const missions = run => {
  const union = { id: 'union', topology: 'end', cells: 0, mask: 0n, fold: false, chance: 1 / 2 };
  const product = { id: 'product', topology: 'cross', cells: 0, mask: 0n, fold: false, chance: 1 / 2 };
  const diceMask = [3, 8, 13, 18].reduce((m, i) => m | 1n << BigInt(i), 0n);
  const digitMask = [0, 1, 2, 3, 5, 10, 15].reduce((m, i) => m | 1n << BigInt(i), 0n);
  const repMask = [0, 6, 12, 18, 24].reduce((m, i) => m | 1n << BigInt(i), 0n);
  const advanced = [
    { id: 'dice-sum', topology: 'cross', cells: 36, mask: diceMask, fold: false, chance: 1 / (2 * 2 ** 36) },
    { id: 'digits-a', topology: 'cross', cells: 16, mask: digitMask, fold: false, chance: 1 / (2 * 2 ** 16) },
    { id: 'digits-b', topology: 'cross', cells: 16, mask: digitMask, fold: false, chance: 1 / (2 * 2 ** 16) },
    { id: 'representatives', topology: 'cross', cells: 25, mask: repMask, fold: true, chance: 1 / (4 * 2 ** 25) }
  ];
  return run % 2 === 0 ? [union, product, ...advanced] : [product, union, ...advanced];
};

function randomMask(n) { let m = 0n; for (let i = 0; i < n; i++) if (rnd() < .5) m |= 1n << BigInt(i); return m; }
function parityMask(n, parity) { let m = 0n; for (let i = 0; i < n; i++) if (i % 2 === parity) m |= 1n << BigInt(i); return m; }
function matches(input, target) { return input && input.topology === target.topology && input.mask === target.mask && input.fold === target.fold; }

const policies = {
  fixed: () => ({ topology: 'cross', mask: 0n, fold: false }),
  cycle: (i, t) => {
    const mode = i % 4;
    return { topology: mode === 0 ? 'end' : 'cross', mask: mode < 2 ? 0n : parityMask(t.cells, mode - 2), fold: mode === 3 };
  },
  random: (_i, t) => ({ topology: rnd() < .5 ? 'end' : 'cross', mask: randomMask(t.cells), fold: rnd() < .5 }),
  idle: () => null
};

function simulate(name, policy) {
  let firstCorrect = 0, firstTotal = 0, completed = 0, progress = 0, chanceSum = 0, actionIndex = 0;
  for (let run = 0; run < RUNS; run++) {
    let lives = 3, solved = 0;
    const deck = missions(run);
    while (lives > 0 && solved < deck.length) {
      const target = deck[solved];
      const input = policy(actionIndex++, target);
      if (!input) break;
      firstTotal++; chanceSum += target.chance;
      const correct = matches(input, target);
      if (correct) { firstCorrect++; solved++; progress++; }
      else {
        lives--;
        // Retry the same board, but retries are not counted as additional first attempts.
        while (lives > 0 && solved < deck.length) {
          const retry = policy(actionIndex++, target);
          if (!retry) { lives = 0; break; }
          if (matches(retry, target)) { solved++; progress++; break; }
          lives--;
        }
      }
    }
    if (solved === 6) completed++;
  }
  return {
    policy: name,
    sessions: RUNS,
    first_attempt_correct: firstCorrect,
    first_attempt_total: firstTotal,
    first_attempt_rate_pct: firstTotal ? +(firstCorrect * 100 / firstTotal).toFixed(3) : 0,
    chance_rate_pct: firstTotal ? +(chanceSum * 100 / firstTotal).toFixed(3) : 0,
    completed,
    completion_rate_pct: +(completed * 100 / RUNS).toFixed(3),
    progress
  };
}

const bots = Object.entries(policies).map(([name, fn]) => simulate(name, fn));
const result = {
  generated_at: new Date().toISOString(),
  runs_per_bot: RUNS,
  contract: 'exact two-rack topology + exact contact bitset + exact fold state; retries do not alter first-attempt counters',
  pointer_evidence: 'See pointer-playthrough-results.json: the same submission invariant is exercised through trusted browser pointer events.',
  bots,
  passed: bots.every(b => b.policy === 'idle' ? b.progress === 0 : b.first_attempt_rate_pct <= b.chance_rate_pct + 0.001)
};
fs.writeFileSync(new URL('./b3-bot-results.json', import.meta.url), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
if (!result.passed) process.exitCode = 1;
