#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import {
  HERE, artifactHash, createSession, closeSession, sleep, state,
  startRun, submitCandidate,
} from './pointer-common.mjs';

const GAMES = 200;
const CHANCE = 1 / 8;
function xorshift(seed) { let x = seed | 0; return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; }; }
function wilson(successes, n, z = 1.96) {
  if (!n) return [0, 0];
  const p = successes / n, z2 = z * z, den = 1 + z2 / n;
  const center = (p + z2 / (2 * n)) / den;
  const margin = z * Math.sqrt((p * (1 - p) + z2 / (4 * n)) / n) / den;
  return [center - margin, center + margin];
}
const nullUpper95 = CHANCE + 1.96 * Math.sqrt(CHANCE * (1 - CHANCE) / GAMES);

const session = await createSession();
const { page } = session;
const globalConsoleErrors = [];
page.on('console', message => { if (message.type() === 'error') globalConsoleErrors.push(message.text()); });
page.on('pageerror', error => globalConsoleErrors.push(String(error)));

async function policyRun(name, chooser) {
  let submissions = 0, firstCorrect = 0, completions = 0;
  const startedAt = Date.now();
  for (let game = 0; game < GAMES; game++) {
    await startRun(page);
    const chosen = chooser(game);
    const after = await submitCandidate(page, chosen);
    submissions++;
    if (after.lastResult === 'correct') firstCorrect++;
    if (after.phase === 'clear') completions++;
  }
  const rate = firstCorrect / submissions;
  return {
    policy: name, games: GAMES, submissions, firstAttemptCorrect: firstCorrect,
    firstAttemptRate: rate, chance: CHANCE, observedWilson95: wilson(firstCorrect, submissions),
    nullUpper95, completions, completionRate: completions / GAMES,
    durationMs: Date.now() - startedAt,
    passed: rate <= nullUpper95 && completions === 0,
  };
}

try {
  const random = xorshift(0x20261017);
  const fixed = await policyRun('fixed-cargo-0', () => 0);
  const cycle = await policyRun('cycle-0-through-7', game => game % 8);
  const randomPolicy = await policyRun('seeded-uniform-random', () => Math.floor(random() * 8));
  let idleProgress = 0, idleSubmissions = 0;
  for (let game = 0; game < GAMES; game++) {
    await startRun(page);
    await sleep(20);
    const s = await state(page);
    idleProgress += s.solved;
    idleSubmissions += s.firstAttemptTotal;
  }
  const idle = { policy: 'idle', games: GAMES, submissions: idleSubmissions, firstAttemptCorrect: 0, firstAttemptRate: 0, chance: CHANCE, completions: 0, completionRate: 0, progress: idleProgress, passed: idleProgress === 0 && idleSubmissions === 0 };
  const output = {
    generatedAt: new Date().toISOString(), artifactHashSha256: artifactHash(), viewport: '390x844',
    input: 'trusted Puppeteer mouse down/move/up on final Unity WebGL for every non-idle submission; start() only resets each run; no answer hooks and no correctSlot/correctIndex are read',
    chance: { formula: '1/8', rate: CHANCE, binomialNullUpper95: nullUpper95 },
    policies: [fixed, cycle, randomPolicy, idle], consoleErrors: globalConsoleErrors,
  };
  output.passed = output.policies.every(policy => policy.passed) && globalConsoleErrors.length === 0;
  fs.writeFileSync(path.join(HERE, 'b3-pointer-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
  if (!output.passed) process.exitCode = 1;
} finally {
  await closeSession(session);
}
