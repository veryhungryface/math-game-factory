#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import {
  HERE, W, H, artifactHash, closeSession, createSession, moveCargo,
  sleep, state, submitCandidate, waitFor,
} from './pointer-common.mjs';

const session = await createSession();
const { page } = session;
const consoleErrors = [];
page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
page.on('pageerror', error => consoleErrors.push(String(error)));
const framesDir = path.join(HERE, 'tutorial-pointer');
fs.mkdirSync(framesDir, { recursive: true });

try {
  const startedAt = Date.now();
  await page.mouse.click(W * .5, H * .46);
  const practiceStart = await waitFor(page, s => s.onboarding && s.phase === 'playing', 'pointer-started practice');
  await page.screenshot({ path: path.join(framesDir, '00-practice-start.png') });

  await moveCargo(page, 0, 3, 0);
  const firstMove = await waitFor(page, s => s.moves > 0 && s.tutorialStep === 1, 'guided H-01 move');
  const firstMoveElapsedMs = Date.now() - startedAt;
  await page.screenshot({ path: path.join(framesDir, '01-first-move.png') });

  const practiceSubmit = await submitCandidate(page, 1);
  const normalPlay = await waitFor(page, s => !s.onboarding && s.phase === 'playing' && s.solved === 0, 'practice completion', 6000);
  await sleep(250);
  await page.screenshot({ path: path.join(framesDir, '02-normal-play.png') });

  const output = {
    generatedAt: new Date().toISOString(),
    artifactHashSha256: artifactHash(),
    viewport: `${W}x${H}`,
    input: 'trusted Puppeteer mouse click starts from title; mouse down/move/up performs both tutorial drags; no game answer hooks',
    practiceStart,
    firstMove,
    firstMoveElapsedMs,
    practiceSubmit,
    normalPlay,
    consoleErrors,
    passed: firstMoveElapsedMs < 15000 && firstMove.tutorialStep === 1 && practiceSubmit.lastResult === 'correct' && !normalPlay.onboarding && consoleErrors.length === 0,
  };
  fs.writeFileSync(path.join(HERE, 'tutorial-pointer-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
  if (!output.passed) process.exitCode = 1;
} finally {
  await closeSession(session);
}
