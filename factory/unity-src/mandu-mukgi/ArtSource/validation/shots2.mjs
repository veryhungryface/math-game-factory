// 와우(첫 3연속)·결과 화면·가로 타이틀 캡처
import fs from 'node:fs';
import path from 'node:path';
import { HERE, launch, openGame, state, waitState, targetPx, drag, shot, sleep } from './lib.mjs';
const OUT = path.join(HERE, 'shots'); fs.mkdirSync(OUT, { recursive: true });
const { server, browser } = await launch();
for (const [w, h] of [[390, 844], [1280, 800]]) {
  const tg = `${w}x${h}`;
  const g = await openGame(browser, server, { w, h });
  const p = g.page;
  await sleep(1300); await shot(p, `${OUT}/${tg}-20-title.png`);
  await p.evaluate(() => window.__GAME_TEST__.start());
  await sleep(900);
  for (let i = 0; i < 3; i++) { await p.evaluate(() => window.__GAME_TEST__.answerCorrect()); await sleep(i < 2 ? 700 : 100); }
  await sleep(500); await shot(p, `${OUT}/${tg}-21-wow-a.png`);
  await sleep(500); await shot(p, `${OUT}/${tg}-22-wow-b.png`);
  // 2단계 두 반죽 동시
  await p.evaluate(() => window.__GAME_TEST__.answerCorrect()); await sleep(2600);
  await shot(p, `${OUT}/${tg}-23-band2-two.png`);
  let s = await state(p); console.log(tg, 'doughs', s.doughs.length, s.level);
  for (let i = 0; i < 3; i++) { await p.evaluate(() => window.__GAME_TEST__.answerWrong()); await sleep(60); }
  await sleep(2200); await shot(p, `${OUT}/${tg}-24-end.png`);
  s = await state(p); console.log(tg, s.phase, s.endReason, g.errors.length);
  await p.close();
}
await browser.close(); server.close();
