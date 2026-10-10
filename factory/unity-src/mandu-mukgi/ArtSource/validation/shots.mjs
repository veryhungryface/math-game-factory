// 눈 확인용 캡처: 타이틀 → 연습 첫 획 → 실전 → 오답 → 1280 가로
import fs from 'node:fs';
import path from 'node:path';
import { HERE, launch, openGame, state, waitState, targetPx, drag, tap, shot, sleep } from './lib.mjs';

const OUT = path.join(HERE, 'shots'); fs.mkdirSync(OUT, { recursive: true });
const { server, browser } = await launch();
const sizes = process.argv[2] === 'land' ? [[1280, 800]] : process.argv[2] === 'both' ? [[390, 844], [1280, 800]] : [[390, 844]];
for (const [w, h] of sizes) {
  const tagp = `${w}x${h}`;
  const g = await openGame(browser, server, { w, h });
  const p = g.page;
  await sleep(400); await shot(p, `${OUT}/${tagp}-00-title-early.png`);
  await sleep(1400); await shot(p, `${OUT}/${tagp}-01-title.png`);
  let s = await state(p);
  console.log('title doughs', JSON.stringify(s.doughs));
  // 타이틀에서 B 호를 눌러 그대로 C 까지 끈다(연습 첫 획)
  const d0 = s.doughs[0];
  await tap(p, [w * 0.5, h * 0.95]); await sleep(200); await shot(p, `${OUT}/${tagp}-02-refuse.png`);
  await tap(p, targetPx(g, d0, 1)); await sleep(500);
  s = await state(p); console.log('after tap', s.stage, s.phase, s.lastResult);
  await shot(p, `${OUT}/${tagp}-03-practice-after-tap.png`);
  await sleep(2600); await shot(p, `${OUT}/${tagp}-04-practice-ghost.png`);
  s = await state(p);
  const d1 = s.doughs[0];
  await drag(p, [targetPx(g, d1, 1), targetPx(g, d1, 2)], { steps: 12 });
  await sleep(250); await shot(p, `${OUT}/${tagp}-05-seal-a.png`);
  await sleep(700); await shot(p, `${OUT}/${tagp}-06-seal-b.png`);
  await sleep(900); await shot(p, `${OUT}/${tagp}-07-seal-c.png`);
  s = await waitState(p, x => x.stage === 'play' && x.doughs.length > 0 && !x.doughs[0].id.startsWith('practice'), 9000);
  await sleep(700); await shot(p, `${OUT}/${tagp}-08-play.png`);
  s = await state(p); console.log('play', JSON.stringify(s).slice(0, 600));
  // 오답 하나(꼭지각 포함 쌍): 임의 두 대상을 잇는다
  const d2 = s.doughs[0];
  await drag(p, [targetPx(g, d2, 0), targetPx(g, d2, 1)], { steps: 10 });
  await sleep(300); await shot(p, `${OUT}/${tagp}-09-judge-a.png`);
  await sleep(700); await shot(p, `${OUT}/${tagp}-10-judge-b.png`);
  s = await state(p); console.log('judged', s.lastResult, s.misconceptionId, s.lives, s.score);
  // 훅으로 진행해 2단계 화면 보기
  for (let i = 0; i < 4; i++) { await p.evaluate(() => window.__GAME_TEST__.answerCorrect()); await sleep(150); }
  await sleep(1500); await shot(p, `${OUT}/${tagp}-11-band2.png`);
  s = await state(p); console.log('band', s.level, s.stage, JSON.stringify(s.doughs).slice(0, 300));
  for (let i = 0; i < 4; i++) { await p.evaluate(() => window.__GAME_TEST__.answerCorrect()); await sleep(150); }
  await sleep(1500); await shot(p, `${OUT}/${tagp}-12-band3.png`);
  for (let i = 0; i < 6; i++) { await p.evaluate(() => window.__GAME_TEST__.answerCorrect()); await sleep(150); }
  await sleep(2200); await shot(p, `${OUT}/${tagp}-13-end.png`);
  s = await state(p); console.log('end', s.phase, s.endReason, s.score, s.firstAttemptCorrect);
  console.log('errors', g.errors);
  await p.close();
}
await browser.close(); server.close();
