#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import {
  HERE, artifactHash, createSession, closeSession, sleep, state,
  startRun, submitCandidate, waitFor,
} from './pointer-common.mjs';

const session = await createSession();
const { page } = session;
const consoleErrors = [];
page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
page.on('pageerror', error => consoleErrors.push(String(error)));
const framesDir = path.join(HERE, 'pointer-playthrough');
fs.mkdirSync(framesDir, { recursive: true });

const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(1000));
const byId = new Map(bank.map(problem => [problem.id, problem]));

function answerIndex(s) {
  const p = byId.get(s.problemId);
  if (!p) throw new Error(`problem not found in bank: ${s.problemId}`);
  const index = p.choices.indexOf(p.answer);
  if (index < 0) throw new Error(`answer not found in choices: ${s.problemId}`);
  return index;
}

async function screenshot(name) {
  await page.screenshot({ path: path.join(framesDir, `${name}.png`) });
  return `${name}.png`;
}

async function solveCurrent(firstWrong = false) {
  const before = await state(page);
  const correct = answerIndex(before);
  let wrongState = null;
  if (firstWrong) {
    const wrong = (correct + 1) % 8;
    wrongState = await submitCandidate(page, wrong);
    await waitFor(page, s => s.lastResult === 'wrong', `wrong ${before.problemId}`);
    // Incorrect feedback owns input for 1.25 s; leave margin for throttled WebGL frames.
    await sleep(1800);
  }
  const correctState = await submitCandidate(page, correct);
  const solved = correctState.solved;
  await waitFor(page, s => s.phase === 'clear' || s.phase === 'gameover' || s.solved > solved || (s.solved === solved && s.problemId !== before.problemId), `advance ${before.problemId}`, 4000).catch(async () => state(page));
  await sleep(450);
  return { problemId: before.problemId, correctIndex: correct, wrongState, afterCorrect: correctState };
}

async function playRun(name, wrongOrders = []) {
  const start = await startRun(page);
  const actions = [];
  await screenshot(`${name}-start`);
  for (let order = 0; order < 8; order++) {
    actions.push(await solveCurrent(wrongOrders.includes(order)));
    if (order === 0 && wrongOrders.includes(order)) await screenshot(`${name}-first-recovery`);
  }
  const final = await waitFor(page, s => s.phase === 'clear' || s.phase === 'gameover', `${name} end`, 6000);
  await screenshot(`${name}-end`);
  return { start, actions, final };
}

try {
  const normal = await playRun('normal');
  const twoMistakeRecovery = await playRun('two-mistake-recovery', [0, 1]);
  const second = await playRun('second');
  const firstIds = normal.actions.map(a => a.problemId);
  const secondIds = second.actions.map(a => a.problemId);
  const output = {
    runId: `hwamul-jam-fix1-${new Date().toISOString()}`,
    generatedAt: new Date().toISOString(),
    artifactHashSha256: artifactHash(),
    viewport: '390x844',
    input: 'trusted Puppeteer mouse down/move/up on the Unity canvas for every cargo move and submission; start() only starts a run; answer hooks are never called',
    bankCount: bank.length,
    frames: fs.readdirSync(framesDir).filter(name => name.endsWith('.png')).sort(),
    normal,
    twoMistakeRecovery,
    second,
    secondRunUsesDifferentProblems: secondIds.some((id, i) => id !== firstIds[i]),
    consoleErrors,
  };
  output.passed = normal.final.phase === 'clear' && normal.final.firstAttemptCorrect === 8 &&
    twoMistakeRecovery.final.phase === 'clear' && twoMistakeRecovery.final.lives === 1 && twoMistakeRecovery.final.firstAttemptCorrect === 6 &&
    second.final.phase === 'clear' && output.secondRunUsesDifferentProblems && consoleErrors.length === 0;
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
  if (!output.passed) process.exitCode = 1;
} finally {
  await closeSession(session);
}
