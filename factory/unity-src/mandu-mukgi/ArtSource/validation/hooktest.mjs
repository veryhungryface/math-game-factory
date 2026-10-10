import { launch, openGame, state, sleep } from './lib.mjs';
const { server, browser } = await launch();
const g = await openGame(browser, server, { w: 390, h: 844 });
const p = g.page;
const brief = s => `${s.stage}/${s.phase} L${s.level} score=${s.score} lives=${s.lives} solved=${s.solved} first=${s.firstAttemptCorrect}/${s.firstAttemptResolved} doughs=${s.doughs.map(d=>d.id).join(',')}`;
await p.evaluate(() => window.__GAME_TEST__.start());
console.log('start', brief(await state(p)));
for (let i = 0; i < 14; i++) { await p.evaluate(() => window.__GAME_TEST__.answerCorrect()); console.log('c'+i, brief(await state(p))); }
await p.evaluate(() => window.__GAME_TEST__.start());
for (let i = 0; i < 4; i++) { await p.evaluate(() => window.__GAME_TEST__.answerWrong()); console.log('w'+i, brief(await state(p))); }
const b = await p.evaluate(() => window.__GAME_TEST__.sampleProblems(5));
console.log(JSON.stringify(b, null, 1));
console.log('errors', g.errors);
await browser.close(); server.close();
