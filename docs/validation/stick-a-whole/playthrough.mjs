#!/usr/bin/env node
/**
 * stick-a-whole — 실제 pointer 입력만으로 하는 완주/실수 관용도 증거 수집기.
 *
 *   node docs/validation/stick-a-whole/playthrough.mjs
 *
 * 규칙(검수 must_fix #4 대응):
 *  - 입력은 전부 실제 CDP 마우스 이벤트(드래그/탭)다. __GAME_TEST__ 의 answerCorrect/
 *    dropCard 같은 내부 판정 경로는 **입력에 쓰지 않는다**.
 *  - 어떤 조각을 고를지는 「화면에 보이는 것」만으로 정한다 — 카드의 aria-label 과
 *    레일의 data-target 은 학생이 눈으로 읽는 정보다.
 *  - getState() 는 결과를 **기록**할 때만 쓴다(판정 대체 아님).
 *
 * 시나리오
 *   A normal   : 규칙을 아는 평가자의 정상 완주 (8방 clear)
 *   B mistake  : 2방에서 평범한 오답 1회 → 그대로 회복해 완주
 *   C second   : 두 번째 판에서 같은 목표의 **다른 정답쌍**을 골라 이후 손패가 달라지는지
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../factory/lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../..');
const OUT = HERE;
const sleep = ms => new Promise(r => setTimeout(r, ms));

function resolveChrome() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const b of fs.readdirSync(base).sort().reverse())
    for (const rel of ['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
                       'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
                       'chrome-linux64/chrome']) {
      const p = path.join(base, b, rel);
      if (fs.existsSync(p)) return p;
    }
  return undefined;
}

/** 화면만 읽는다: 손패 분수, 레일 목표, 잠금 여부, 현재 안내 문구. */
const readScreen = page => page.evaluate(() => {
  const rect = el => { const r = el.getBoundingClientRect(); return { cx: r.left + r.width / 2, cy: r.top + r.height / 2, left: r.left, top: r.top, w: r.width, h: r.height }; };
  const cards = [...document.querySelectorAll('[data-card]')].map(el => {
    const b = el.querySelectorAll('.frac b');
    return { i: +el.dataset.card, n: +b[0].textContent, d: +b[1].textContent,
             locked: el.classList.contains('locked'), hint: el.classList.contains('hint'),
             used: el.classList.contains('used'), rect: rect(el) };
  });
  const rails = [...document.querySelectorAll('.rail')].map(el => ({ ri: +el.dataset.rail, target: +el.dataset.target, cells: el.querySelectorAll('.cell').length, rect: rect(el) }));
  return { cards, rails, goal: document.getElementById('goal').textContent.trim(),
           message: document.getElementById('message').textContent.trim(),
           boundary: (() => { const b = document.querySelector('[data-boundary]'); return b ? rect(b) : null; })() };
});

async function drag(page, from, to, steps = 14) {
  await page.mouse.move(from.x, from.y);
  await page.mouse.down();
  for (let i = 1; i <= steps; i++) {
    await page.mouse.move(from.x + (to.x - from.x) * i / steps, from.y + (to.y - from.y) * i / steps);
    await sleep(9);
  }
  await page.mouse.up();
  await sleep(90);
}

/** 조각 i 를 레일 ri 의 pos 번째 칸부터 놓는다 — 실제 드래그. */
async function place(page, i, ri, pos) {
  const s = await readScreen(page);
  const card = s.cards.find(c => c.i === i);
  const rail = s.rails.find(r => r.ri === ri);
  const cw = rail.rect.w / rail.cells;
  await drag(page, { x: card.rect.cx, y: card.rect.cy }, { x: rail.rect.left + pos * cw + 2, y: rail.rect.top + rail.rect.h / 2 });
}

async function moveBoundary(page, cells) {
  const s = await readScreen(page);
  if (!s.boundary) return false;
  const rail = s.rails.reduce((a, b) => (b.target > a.target ? b : a));
  const cw = rail.rect.w / rail.cells;
  await drag(page, { x: s.boundary.cx, y: s.boundary.cy }, { x: rail.rect.left + cells * cw, y: s.boundary.cy });
  return true;
}

/** 학생이 화면에서 읽어 낼 수 있는 정답쌍 목록. */
function pairsOnScreen(cards, target) {
  const live = cards.filter(c => !c.used);
  const out = [];
  for (let a = 0; a < live.length; a++)
    for (let b = a + 1; b < live.length; b++)
      if (live[a].d === live[b].d && live[a].n + live[b].n === target) out.push([live[a], live[b]]);
  return out;
}

async function startRun(page, url) {
  await page.goto(url, { waitUntil: 'networkidle0' });
  await page.waitForFunction('window.__GAME_TEST__?.ready === true', { timeout: 15000 });
  const t = await page.evaluate(() => { const r = document.getElementById('start').getBoundingClientRect(); return { x: r.left + 30, y: r.top + r.height / 2 }; });
  await drag(page, t, { x: t.x + 120, y: t.y }, 18);   // 물밀대를 실제로 민다
  await sleep(500);
}

/** 연습(1방)을 실제 드래그로 통과한다. */
async function clearPractice(page, log) {
  const t0 = Date.now();
  for (let step = 0; step < 4; step++) {
    const s = await readScreen(page);
    const hint = s.cards.find(c => c.hint);
    if (!hint) break;
    await place(page, hint.i, 0, s.cards.filter(c => c.used).reduce((a, c) => a + c.n, 0));
    await sleep(250);
  }
  await sleep(1400);
  const st = await page.evaluate(() => window.__GAME_TEST__.getState());
  const screen = await readScreen(page);
  log.practice = { seconds: +((Date.now() - t0) / 1000).toFixed(1), solved: st.solved, room: st.room, explanation: screen.message };
  return st;
}

/** 한 방을 푼다. pairIndex 로 어느 정답쌍을 쓸지 고른다. */
async function solveRoom(page, { pairIndex = 0, deliberateMistake = false } = {}) {
  const s = await readScreen(page);
  // 6방부터는 레일이 둘이다 — 화면에 적힌 두 목표 중 「더 큰 분수」 쪽을 고른다.
  const rail = s.rails.reduce((a, b) => (b.target > a.target ? b : a));
  const target = rail.target;
  const ps = pairsOnScreen(s.cards, target);
  if (deliberateMistake) {                     // 흔한 실수: 합이 목표보다 한 칸 모자란 짝
    const live = s.cards.filter(c => !c.used && c.d === rail.cells / (rail.cells > 4 ? 2 : 1));
    let bad = null;
    for (let a = 0; a < s.cards.length && !bad; a++)
      for (let b = a + 1; b < s.cards.length && !bad; b++)
        if (s.cards[a].d === s.cards[b].d && s.cards[a].n + s.cards[b].n !== target && s.cards[a].n + s.cards[b].n <= rail.cells) bad = [s.cards[a], s.cards[b]];
    if (!bad) return { skipped: true };
    await place(page, bad[0].i, 0, 0);
    await place(page, bad[1].i, 0, bad[0].n);
    await sleep(400);
    const shown = (await readScreen(page)).message;      // 오답 순간에 실제로 뜬 설명
    await sleep(1400);
    const st = await page.evaluate(() => window.__GAME_TEST__.getState());
    return { mistake: true, placed: bad.map(c => `${c.n}/${c.d}`), lives_after: st.lives, phase: st.phase, message: shown };
  }
  if (!ps.length) return { stuck: true, target, cards: s.cards.map(c => `${c.n}/${c.d}`) };
  const key = p => p.map(c => c.n).sort().join('+');
  // 2번 선택지: 값이 실제로 다른 짝이 있으면 그쪽을 고른다(같은 목표의 「다른 풀이」).
  let pair = ps[0];
  if (pairIndex > 0) pair = ps.find(p => key(p) !== key(ps[0])) || ps[Math.min(pairIndex, ps.length - 1)];
  const handBefore = s.cards.map(c => `${c.n}/${c.d}`);
  await place(page, pair[0].i, rail.ri, 0);
  await place(page, pair[1].i, rail.ri, pair[0].n);
  await sleep(300);
  const mid = await page.evaluate(() => window.__GAME_TEST__.getState());
  if (mid.phase === 'grouping') { await moveBoundary(page, rail.cells / 2); await sleep(400); }
  await sleep(1500);
  const st = await page.evaluate(() => window.__GAME_TEST__.getState());
  const after = await readScreen(page);
  return { target, options: ps.length, all_pairs: ps.map(key), hand_before: handBefore,
           used: pair.map(c => `${c.n}/${c.d}`), used_slots: pair.map(c => c.i + 1),
           kept: handBefore.filter((_, i) => !pair.some(c => c.i === i)),
           hand_after: after.cards.map(c => `${c.n}/${c.d}`), next_target: after.rails.map(r => r.target),
           solved: st.solved, room_after: st.room, phase: st.phase, lives: st.lives };
}

async function run(page, url, name, opts = {}) {
  const log = { scenario: name, rooms: [] };
  await startRun(page, url);
  await clearPractice(page, log);
  for (let guard = 0; guard < 20; guard++) {
    const st = await page.evaluate(() => window.__GAME_TEST__.getState());
    if (['clear', 'gameover'].includes(st.phase)) break;
    const r = await solveRoom(page, {
      pairIndex: opts.pairIndex && st.room === opts.pairIndexRoom ? opts.pairIndex : 0,
      deliberateMistake: opts.mistakeRoom === st.room && !log.rooms.some(x => x.mistake),
    });
    log.rooms.push({ room: st.room, ...r });   // room = 이 방의 번호(풀기 전)
    if (r.stuck) break;
  }
  await sleep(600);
  const fin = await page.evaluate(() => window.__GAME_TEST__.getState());
  const finText = await page.evaluate(() => document.getElementById('resultText')?.textContent || '');
  log.final = { phase: fin.phase, solved: fin.solved, lives: fin.lives, score: fin.score, streak: fin.streak, resultText: finText };
  await page.screenshot({ path: path.join(OUT, `run-${name}.png`) });
  return log;
}

const { url, close } = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({ headless: 'new', executablePath: resolveChrome(),
  args: ['--no-sandbox', '--use-gl=swiftshader', '--window-size=390,844'] });
const page = await browser.newPage();
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 2, isMobile: true, hasTouch: true });
const errors = [];
page.on('console', m => m.type() === 'error' && errors.push(m.text()));
page.on('pageerror', e => errors.push(String(e)));

const target = `${url}/g/stick-a-whole/`;
const out = { checked_at: new Date().toISOString(), runs: [] };
out.runs.push(await run(page, target, 'A-normal'));
out.runs.push(await run(page, target, 'B-first-mistake', { mistakeRoom: 2 }));
out.runs.push(await run(page, target, 'C-second-play-other-pair', { pairIndex: 1, pairIndexRoom: 2 }));
out.console_errors = errors;

fs.writeFileSync(path.join(OUT, 'playthrough.json'), JSON.stringify(out, null, 2));
console.log(JSON.stringify(out.runs.map(r => ({ s: r.scenario, practice: r.practice, final: r.final })), null, 2));
console.log('console errors:', errors.length);
await browser.close(); close();
