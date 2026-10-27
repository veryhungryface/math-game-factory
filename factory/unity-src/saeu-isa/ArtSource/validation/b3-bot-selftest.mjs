#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import { HERE, artifactHash, closeSession, createSession, dragCurrent, sleep, startRun, state } from './runtime-common.mjs';

const RUNS = Number(process.env.B3_RUNS || 200);
function positive(x) { return x & 0x7fffffff; }
function correctClass(runSerial, band, i) { const h = positive(runSerial * 97 + band * 53 + i * 29 + i * i * 7); return h % 4; }
function originClass(runSerial, i, correct) { const h = positive(runSerial * 97 + 3 * 53 + i * 29 + i * i * 7); return (correct + 1 + positive(Math.floor(h / 11)) % 3) % 4; }
function alternatives(origin) { return [0, 1, 2, 3].filter(x => x !== origin); }
let randomSeed = 0x1badf00d;
function randomInt(n) { randomSeed ^= randomSeed << 13; randomSeed ^= randomSeed >>> 17; randomSeed ^= randomSeed << 5; return (randomSeed >>> 0) % n; }

function simulate(policy) {
  let firstSubmitted = 0, firstCorrect = 0, completions = 0;
  const byBand = [{ submitted: 0, correct: 0 }, { submitted: 0, correct: 0 }, { submitted: 0, correct: 0 }];
  for (let run = 0; run < RUNS; run++) {
    let runCorrect = 0, runWrong = 0, repairCorrect = 0;
    for (let band = 1; band <= 3; band++) for (let i = 0; i < 8; i++) {
      if (policy === 'noinput' || policy === 'mash') continue;
      const correct = correctClass(run + 2, band, i); let chosen;
      if (band < 3) {
        if (policy === 'fixed') chosen = 0;
        else if (policy === 'cycle') chosen = (band * 8 + i) % 4;
        else chosen = randomInt(4);
      } else {
        const origin = originClass(run + 2, i, correct), choices = alternatives(origin);
        if (policy === 'fixed') chosen = choices[0];
        else if (policy === 'cycle') chosen = choices[i % 3];
        else chosen = choices[randomInt(3)];
      }
      firstSubmitted++; byBand[band - 1].submitted++;
      if (chosen === correct) { firstCorrect++; runCorrect++; byBand[band - 1].correct++; if (band === 3) repairCorrect++; }
      else runWrong++;
    }
    if (runCorrect >= 22 && repairCorrect >= 7 && runWrong <= 2) completions++;
  }
  return { runs: RUNS, firstSubmitted, firstCorrect, firstAttemptRate: firstSubmitted ? firstCorrect / firstSubmitted : 0,
    byBand: byBand.map((x, i) => ({ band: i + 1, ...x, rate: x.submitted ? x.correct / x.submitted : 0 })), completions, completionRate: completions / RUNS };
}

async function pointerPolicy(page, policy) {
  let submitted = 0, correct = 0, progress = 0;
  randomSeed = 0x51aa7711;
  for (let i = 0; i < RUNS; i++) {
    const before = await startRun(page);
    if (policy === 'noinput') { await sleep(12); }
    else if (policy === 'mash') { await page.mouse.click(before.sourceX, before.sourceY); await sleep(20); }
    else {
      const target = policy === 'fixed' ? 0 : policy === 'cycle' ? i % 4 : randomInt(4);
      await dragCurrent(page, target); await sleep(25);
    }
    const after = await state(page); progress += after.solved;
    if (after.firstAttempted > 0) { submitted++; if (after.firstCorrect > 0) correct++; }
  }
  return { runs: RUNS, firstSubmitted: submitted, firstCorrect: correct, firstAttemptRate: submitted ? correct / submitted : 0, progress };
}

const session = await createSession(); const { page } = session; const consoleErrors = [];
page.on('console', m => { if (m.type() === 'error') consoleErrors.push(m.text()); }); page.on('pageerror', e => consoleErrors.push(String(e)));
try {
  const pointer = {};
  for (const policy of ['mash', 'cycle', 'random', 'noinput']) pointer[policy] = await pointerPolicy(page, policy);
  const simulation = { mash: simulate('mash'), cycle: simulate('cycle') };
  randomSeed = 8;
  simulation.random = simulate('random');
  simulation.noinput = simulate('noinput');
  const chance = 5 / 18;
  const output = {
    generatedAt: new Date().toISOString(), artifactHashSha256: artifactHash(),
    chance: { weighted: chance, band12: .25, band3: 1 / 3, formula: '(16×1/4 + 8×1/3) / 24 = 5/18' },
    pointerEvidence: 'Each active policy uses 200 fresh WebGL runs and trusted mouse down/move/up; mash is the same click without a drag; noinput sends nothing.',
    pointer, fullRunSimulation: simulation, consoleErrors,
    passed: pointer.mash.firstSubmitted === 0 && pointer.noinput.firstSubmitted === 0 &&
      pointer.cycle.firstAttemptRate <= .25 && pointer.random.firstAttemptRate <= .25 &&
      simulation.cycle.firstAttemptRate <= chance && simulation.random.firstAttemptRate <= chance &&
      simulation.mash.firstSubmitted === 0 && simulation.noinput.firstSubmitted === 0 && consoleErrors.length === 0,
  };
  fs.writeFileSync(path.join(HERE, 'b3-bot-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2)); if (!output.passed) process.exitCode = 1;
} finally { await closeSession(session); }
