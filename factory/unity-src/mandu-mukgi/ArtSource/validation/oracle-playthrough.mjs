// 정상 경로 + 「한 번 실수한 뒤 목표 도달」 경로를 진짜 마우스 획으로 끝까지 실행한다(타이틀 → 연습 → 다리 연습 2회 → 12문항).
// 정답 쌍은 반죽 id 의 꼭지각 글자(-A/-B/-C)에서 얻는다(수학을 아는 학생 역할). 실수는 꼭지각을 포함한 쌍으로 낸다.
import fs from 'node:fs';
import path from 'node:path';
import { HERE, launch, openGame, state, sleep, shot } from './lib.mjs';

const OUT = path.join(HERE, 'oracle'); fs.mkdirSync(OUT, { recursive: true });
const { server, browser } = await launch();
const W = 390, H = 844;
const apexOf = id => 'ABC'.indexOf(id.match(/-([ABC])(?:-p\d)?$/)[1]);
const practiceApex = { 'practice-1': 0, 'practice-2': 0, 'practice-3': 1 };

async function run(label, mistakesAt) {
  const g = await openGame(browser, server, { w: W, h: H });
  const p = g.page;
  const px = (d, k) => [d.tx[k] / 1000 * W, d.ty[k] / 1000 * H];
  async function stroke(a, b) {
    await p.mouse.move(a[0], a[1]); await p.mouse.down(); await sleep(60);
    for (let j = 1; j <= 8; j++) { await p.mouse.move(a[0] + (b[0] - a[0]) * j / 8, a[1] + (b[1] - a[1]) * j / 8); await sleep(16); }
    await sleep(50); await p.mouse.up(); await sleep(40);
  }
  await sleep(1300);
  let s = await state(p);
  let frame = 0, submitted = 0, log = [];
  const madeMistake = new Set();
  const t0 = Date.now();
  while (Date.now() - t0 < 240000) {
    s = await state(p);
    if (s.phase === 'gameover' || s.phase === 'clear') break;
    const d = (s.doughs || [])[0];
    if (!d || s.locked) { await sleep(60); continue; }
    const apex = d.id in practiceApex ? practiceApex[d.id] : apexOf(d.id);
    const good = [0, 1, 2].filter(k => k !== apex);
    const isReal = !d.id.startsWith('practice');
    const wrong = isReal && !d.failed && mistakesAt.includes(submitted) && !madeMistake.has(submitted);
    const pair = wrong ? [apex, good[0]] : good;
    const before = s.submissions;
    await stroke(px(d, pair[0]), px(d, pair[1]));
    // 판정 직후 프레임
    const until = Date.now() + 1500;
    do { await sleep(40); s = await state(p); } while (s.submissions === before && Date.now() < until);
    if (s.submissions === before) { log.push(`${d.id} (획 미등록 — 재시도)`); continue; }
    if (wrong) madeMistake.add(submitted);
    if (isReal && !wrong) submitted++;
    log.push(`${d.id} ${wrong ? 'WRONG' : 'ok'} → ${s.lastResult} ${s.misconceptionId} seals=${s.lives} score=${s.score}`);
    if (frame < 14) { await sleep(250); await shot(p, `${OUT}/${label}-${String(frame++).padStart(2, '0')}.png`); }
    await sleep(120);
  }
  await sleep(1700);
  await shot(p, `${OUT}/${label}-end.png`);
  s = await state(p);
  const res = { label, phase: s.phase, endReason: s.endReason, solved: s.solved, score: s.score, first: `${s.firstAttemptCorrect}/${s.firstAttemptResolved}`, byBand: s.firstByBand, seals: s.lives, repaired: s.repaired, consoleErrors: g.errors, log };
  await p.close();
  return res;
}

const results = [await run('clean', []), await run('one-mistake', [5])];
fs.writeFileSync(path.join(HERE, 'oracle-playthrough-results.json'), JSON.stringify(results, null, 2) + '\n');
for (const r of results) console.log(r.label, r.phase, r.endReason, 'solved', r.solved, 'first', r.first, 'bands', r.byBand.join('/'), 'seals', r.seals, 'errors', r.consoleErrors.length);
await browser.close(); server.close();
