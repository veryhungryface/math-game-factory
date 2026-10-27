#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import { HERE, artifactHash, closeSession, createSession, dragCurrent, sleep, startRun, state, waitFor } from './runtime-common.mjs';

async function finish(page, withFirstMistake) {
  let s = await startRun(page); const attempts = [];
  if (withFirstMistake) {
    const wrong = (s.currentCorrectClass + 1) % 4;
    const r = await dragCurrent(page, wrong); attempts.push({ id: r.before.problemId, submitted: wrong, result: r.after.lastResult });
    await sleep(760);
    s = await waitFor(page, x => x.phase === 'playing' && x.problemId === r.before.problemId && x.sourceX > 0, 'same sample recovery');
    const repaired = await dragCurrent(page, s.currentCorrectClass); attempts.push({ id: s.problemId, submitted: s.currentCorrectClass, recovery: true, result: repaired.after.lastResult });
    await sleep(760);
  }
  let guard = 0;
  while (guard++ < 40) {
    s = await state(page);
    if (s.phase === 'clear' || s.phase === 'gameover') break;
    if (s.currentCorrectClass < 0 || s.sourceX <= 0) { await sleep(120); continue; }
    const r = await dragCurrent(page, s.currentCorrectClass);
    attempts.push({ id: r.before.problemId, submitted: s.currentCorrectClass, onboarding: s.onboarding, solved: r.after.solved });
    await sleep(r.after.solved > 0 && r.after.solved % 8 === 0 ? 1550 : 680);
  }
  return { final: await state(page), attempts };
}

async function gameover(page) {
  let s = await startRun(page); const attempts = [];
  for (let i = 0; i < 3; i++) {
    const wrong = (s.currentCorrectClass + 1) % 4;
    const r = await dragCurrent(page, wrong); attempts.push({ lives: r.after.lives, phase: r.after.phase });
    await sleep(760); s = await state(page); if (s.phase === 'gameover') break;
  }
  return { final: s, attempts };
}

const session = await createSession(); const { page } = session;
const consoleErrors = []; page.on('console', m => { if (m.type() === 'error') consoleErrors.push(m.text()); }); page.on('pageerror', e => consoleErrors.push(String(e)));
const frames = path.join(HERE, 'pointer-playthrough'); fs.mkdirSync(frames, { recursive: true });
try {
  // 온보딩도 실제 pointer로 시작하고 고정 23 mm를 직접 옮긴다.
  await page.mouse.click(195, 420);
  const practice = await waitFor(page, s => s.onboarding && s.level === 0 && s.currentValueMm === 23, 'fixed practice');
  await page.screenshot({ path: path.join(frames, '00-practice.png') });
  const practiceMove = await dragCurrent(page, practice.currentCorrectClass); await sleep(1400);
  const afterPractice = await waitFor(page, s => !s.onboarding && s.level === 1 && s.solved === 0, 'practice finished');
  await page.screenshot({ path: path.join(frames, '01-first-band.png') });

  const clean = await finish(page, false); await page.screenshot({ path: path.join(frames, '02-clean-clear.png') });
  const recovered = await finish(page, true); await page.screenshot({ path: path.join(frames, '03-recovery-clear.png') });
  const failed = await gameover(page); await page.screenshot({ path: path.join(frames, '04-lives-zero.png') });
  const output = {
    generatedAt: new Date().toISOString(), artifactHashSha256: artifactHash(), viewport: '390x844',
    input: 'all learning submissions use Puppeteer mouse down/move/up from runtime source coordinates to graph class coordinates; answerCorrect/answerWrong are never called',
    practice: { before: practice, submitted: practice.currentCorrectClass, after: practiceMove.after, enteredBand1: afterPractice },
    clean: { final: clean.final, attempts: clean.attempts.length }, recovered: { final: recovered.final, attempts: recovered.attempts.length },
    livesZero: failed, consoleErrors,
    passed: practiceMove.after.lastResult === 'correct' && clean.final.phase === 'clear' && clean.final.solved === 24 && clean.final.firstCorrect === 24 && clean.final.lives === 3 &&
      recovered.final.phase === 'clear' && recovered.final.solved === 24 && recovered.final.firstCorrect === 23 && recovered.final.lives === 2 &&
      failed.final.phase === 'gameover' && failed.final.lives === 0 && consoleErrors.length === 0,
  };
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2)); if (!output.passed) process.exitCode = 1;
} finally { await closeSession(session); }
