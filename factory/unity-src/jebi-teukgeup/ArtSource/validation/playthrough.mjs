// 흐름 검증(정답을 아는 검증 봇 — 무뇌 봇이 아니다): 실제 포인터로만 제출한다. t 는 problemId(k?-tNN-v?)에서 읽는다.
//  A) 타이틀 탭 → 연습 12 cm 실패 → 7 cm 성공 → 실전 8역 전부 첫 제출 정답 → 도착(clear)
//  B) 1역 첫 제출 오답 → 같은 역 수리 → 나머지 정답 → 7/8 로 도착
//  C) 1역에서 오답 3번 → 고리 0 → gameover(rings)
import fs from 'node:fs';
import path from 'node:path';
import { launch, startServer, openGame, pull, state, sleep } from './common.mjs';
const HERE = path.dirname(new URL(import.meta.url).pathname);
const OUT = path.join(HERE, 'playthrough'); fs.mkdirSync(OUT, { recursive: true });
const W = Number(process.env.PW || 390), H = Number(process.env.PH || 844);
const tOf = s => Number(/t(\d+)/.exec(s.problemId)[1]);
const waitOpen = (page, pred, ms = 20000) => page.waitForFunction(new Function('s', `return (${pred})(window.__GAME_TEST__.getState())`), { timeout: ms }).catch(() => null);
async function waitState(page, fn, ms = 20000) {
  const t0 = Date.now();
  while (Date.now() - t0 < ms) { const s = await state(page); if (fn(s)) return s; await sleep(60); }
  return state(page);
}
const server = await startServer();
const browser = await launch();
const log = {};
const tag = `${W}x${H}`;

// A
{
  const { page, errors } = await openGame(browser, server, W, H, 'A');
  await page.mouse.click(W * 0.5, H * 0.45);
  let s = await waitState(page, s => s.inputOpen && s.onboarding);
  await pull(page, W, H, 0, 12);
  s = await waitState(page, s => s.submissions === 1);
  const practiceWrong = { lastSubmission: s.lastSubmission, firstAttempts: s.firstAttempts, lives: s.lives, onboarding: s.onboarding };
  s = await waitState(page, s => s.inputOpen && s.onboarding);
  await pull(page, W, H, s.selectedLength, 7);
  await page.screenshot({ path: path.join(OUT, `${tag}-A-practice-success.png`) });
  const rounds = [];
  for (let r = 1; r <= 8; r++) {
    s = await waitState(page, s => s.inputOpen && s.round === r && !s.onboarding, 25000);
    const t = tOf(s);
    await pull(page, W, H, s.selectedLength, t, { wiggle: s.selectedLength === t });
    if (r === 6) { await sleep(700); await page.screenshot({ path: path.join(OUT, `${tag}-A-r6-correct.png`) }); }
    const after = await waitState(page, x => x.firstAttempts >= r, 5000);
    rounds.push({ r, kind: s.kind, t, inspect: s.inspect, start: s.selectedLength, first: after.firstCorrect });
  }
  s = await waitState(page, s => s.phase === 'clear', 15000);
  await sleep(2600);
  await page.screenshot({ path: path.join(OUT, `${tag}-A-clear.png`) });
  log.A = { practiceWrong, rounds, final: { phase: s.phase, firstCorrect: s.firstCorrect, lives: s.lives, stationCount: s.stationCount, score: s.score }, errors,
    ok: practiceWrong.firstAttempts === 0 && practiceWrong.lives === 3 && s.phase === 'clear' && s.firstCorrect === 8 && s.lives === 3 };
  await page.close();
}
// B
{
  const { page, errors } = await openGame(browser, server, W, H, 'B');
  await page.evaluate(() => window.__GAME_TEST__.start());
  let s = await waitState(page, s => s.inputOpen && s.round === 1);
  const t1 = tOf(s); const wrong = t1 % 24 + 1;
  await pull(page, W, H, s.selectedLength, wrong);
  await sleep(900);
  await page.screenshot({ path: path.join(OUT, `${tag}-B-first-wrong.png`) });
  s = await waitState(page, s => s.inputOpen && s.round === 1 && s.submissions === 1);
  const afterWrong = { lives: s.lives, firstAttempts: s.firstAttempts, firstCorrect: s.firstCorrect, misconceptionId: s.misconceptionId };
  await pull(page, W, H, s.selectedLength, t1);
  for (let r = 2; r <= 8; r++) {
    s = await waitState(page, s => s.inputOpen && s.round === r, 25000);
    const t = tOf(s);
    await pull(page, W, H, s.selectedLength, t, { wiggle: s.selectedLength === t });
    await waitState(page, x => x.firstAttempts >= r, 5000);
  }
  s = await waitState(page, s => s.phase === 'clear' || s.phase === 'gameover', 15000);
  log.B = { afterWrong, final: { phase: s.phase, firstCorrect: s.firstCorrect, lives: s.lives, score: s.score }, errors,
    ok: afterWrong.lives === 2 && afterWrong.firstAttempts === 1 && afterWrong.firstCorrect === 0 && s.phase === 'clear' && s.firstCorrect === 7 };
  await page.close();
}
// C
{
  const { page, errors } = await openGame(browser, server, W, H, 'C');
  await page.evaluate(() => window.__GAME_TEST__.start());
  let s;
  for (let k = 0; k < 3; k++) {
    s = await waitState(page, s => s.inputOpen && s.round === 1);
    const t = tOf(s); const wrong = (t + 4 + k) % 24 + 1;
    await pull(page, W, H, s.selectedLength, wrong);
    s = await waitState(page, x => x.submissions === k + 1, 5000);
  }
  s = await waitState(page, s => s.phase === 'gameover', 6000);
  await sleep(2200);
  await page.screenshot({ path: path.join(OUT, `${tag}-C-gameover.png`) });
  log.C = { final: { phase: s.phase, result: s.result, lives: s.lives, firstAttempts: s.firstAttempts }, errors, ok: s.phase === 'gameover' && s.result === 'rings' && s.lives === 0 };
  await page.close();
}
await browser.close(); server.close?.();
const outFile = path.join(HERE, `playthrough-${tag}.json`);
fs.writeFileSync(outFile, JSON.stringify(log, null, 2));
console.log(JSON.stringify({ A: log.A.ok, B: log.B.ok, C: log.C.ok, Af: log.A.final, Bf: log.B.final, Cf: log.C.final, errs: [log.A.errors, log.B.errors, log.C.errors].flat() }));
process.exit(0);
