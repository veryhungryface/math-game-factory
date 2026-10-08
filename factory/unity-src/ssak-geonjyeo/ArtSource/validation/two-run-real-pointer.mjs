#!/usr/bin/env node
// 싹 건져 — 실제 pointer 두 판 검증 (1차 수정 라운드).
// 화면의 조건 문장(state.prompt = 조건 카드 문자열)과 화면의 게 라벨 위치(state.crabPx)만 읽고,
// puppeteer 마우스(=실제 pointer 이벤트)로 조개 손잡이를 끌고 그물 손잡이에서 게를 쓸어 담는다.
// __GAME_TEST__.answerCorrect/answerWrong/start/sampleProblems 는 호출하지 않는다(getState 읽기만).
// 1판: 타이틀 탭 → 연습 → 실전, 2번째 조수에서 일부러 한 마리를 빠뜨려 오답→같은 조수 재시도 → 끝까지.
// 2판: 결과 화면 탭으로 재시작 → 연습 → 실전 7조수 전부 첫 시도.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(process.cwd());
const OUT = path.join(ROOT, 'factory/unity-src/ssak-geonjyeo/ArtSource/validation/two-run-real-pointer');
fs.mkdirSync(OUT, { recursive: true });
const srv = await serveStatic(path.join(ROOT, 'public'), 0);
const exe = process.env.PUPPETEER_EXECUTABLE_PATH || (process.env.HOME + '/.cache/puppeteer/chrome/mac_arm-151.0.7922.47/chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
const browser = await puppeteer.launch({ headless: true, executablePath: exe, args: ['--no-sandbox', '--use-angle=metal'] });
const page = await browser.newPage();
const W = 390, H = 844;
await page.setViewport({ width: W, height: H, deviceScaleFactor: 2 });
await page.goto(`${srv.url}/g/ssak-geonjyeo/`, { waitUntil: 'networkidle2' });
await page.waitForFunction('window.__GAME_TEST__?.ready === true', { timeout: 30000 });
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const log = [];
let shot = 0;
const state = () => page.evaluate(() => window.__GAME_TEST__.getState());
const brief = (s) => ({ phase: s.phase, onboarding: s.onboarding, tide: s.tide, solved: s.solved, lives: s.lives, score: s.score, capacity: s.capacity, selectedIds: s.selectedIds, attempts: s.attempts, first: `${s.firstAttemptCorrect}/${s.firstAttemptTotal}`, problem: s.currentProblem, misconception: s.misconceptionId });
async function snap(name) { const f = `${String(shot++).padStart(2, '0')}-${name}.png`; await page.screenshot({ path: path.join(OUT, f) }); log.push({ type: 'shot', file: f, state: brief(await state()) }); }
function toCss(s, x, y) { return [x * W / s.screenW, y * H / s.screenH]; }

function parseTargets(prompt) {
  const lines = prompt.split('\n').filter((l) => l.includes('사건') && l.includes(':'));
  const events = lines.length ? lines : [prompt];
  const nums = (re, txt, all) => { const m = txt.match(re); return m ? m[1].split(/[,\s]+/).filter(Boolean).map(Number) : all; };
  const set = new Set();
  for (const e of events) {
    const backs = nums(/등번호가?\s*([\d,\s]+)/, e, [1, 2, 3, 4]);
    const claws = nums(/집게번호가?\s*([\d,\s]+)/, e, [1, 2, 3]);
    for (const b of backs) for (const c of claws) set.add(`${b},${c}`);
  }
  return set;
}

async function drag(points, stepsPer = 8) {
  await page.mouse.move(points[0][0], points[0][1]);
  await page.mouse.down();
  await sleep(60);
  for (let i = 1; i < points.length; i++) { await page.mouse.move(points[i][0], points[i][1], { steps: stepsPer }); await sleep(30); }
  await sleep(60);
  await page.mouse.up();
  log.push({ type: 'drag', points: points.map(([x, y]) => [Math.round(x), Math.round(y)]) });
}

async function setCapacity(n, practice) {
  const s = await state();
  const c = s.controlPx;
  const [hx, hy] = toCss(s, c[4], c[5]);
  if (practice) { await drag([[hx, hy], [hx + 30, hy], [hx + 60, hy]]); return; }
  const [ax, ay] = toCss(s, c[0], c[1]), [bx, by] = toCss(s, c[2], c[3]);
  const t = (n - 1) / 11;
  await drag([[hx, hy], [ax + (bx - ax) * t, ay + (by - ay) * t]]);
}

async function sweep(targets) {
  const s = await state();
  const crabs = [];
  for (let i = 0; i < s.crabPx.length; i += 4) { const [x, y] = toCss(s, s.crabPx[i + 2], s.crabPx[i + 3]); crabs.push({ key: `${s.crabPx[i]},${s.crabPx[i + 1]}`, x, y }); }
  const xs = [...new Set(crabs.map((c) => Math.round(c.x)))].sort((a, b) => a - b);
  const ys = [...new Set(crabs.map((c) => Math.round(c.y)))].sort((a, b) => a - b);
  const hx = xs.length > 1 ? (xs[1] - xs[0]) / 2 : 60, hy = ys.length > 1 ? (ys[1] - ys[0]) / 2 : 60;
  // 열 사이 세로 통로와 격자 바깥 아래 통로로만 이동한다(행 사이는 라벨 높이 때문에 판정 반경에 걸린다).
  // 목표 게마다: 바깥 통로 → 오른쪽 열 통로를 따라 그 행까지 → 수평으로 게 중심 → 되돌아 나온다.
  const [sx, sy] = toCss(s, s.controlPx[6], s.controlPx[7]);
  const outerY = ys[ys.length - 1] + hy * 2.2;
  const pts = [[sx, sy], [xs[xs.length - 1] + hx, outerY]];
  const order = crabs.filter((c) => targets.has(c.key)).sort((a, b) => b.x - a.x || b.y - a.y);
  for (const c of order) {
    const gx = c.x + hx;
    pts.push([gx, outerY], [gx, c.y], [c.x, c.y], [gx, c.y], [gx, outerY]);
  }
  await drag(pts);
}

async function waitPhase(pred, ms = 6000) { const t0 = Date.now(); while (Date.now() - t0 < ms) { const s = await state(); if (pred(s)) return s; await sleep(120); } return state(); }

async function practice(label) {
  let s = await waitPhase((x) => x.onboarding);
  await snap(`${label}-practice`);
  await setCapacity(2, true); await sleep(400);
  s = await state();
  log.push({ type: 'practice-capacity', state: brief(s) });
  await sweep(parseTargets(s.prompt));
  s = await waitPhase((x) => !x.onboarding && x.tide >= 1, 5000);
  log.push({ type: 'practice-done', state: brief(s) });
}

async function playRun(label, missOnTide) {
  let s = await state();
  let lastTide = -1, missed = false;
  for (let guard = 0; guard < 30; guard++) {
    s = await waitPhase((x) => x.phase !== 'playing' || (!x.onboarding && x.capacity === 0 && x.attempts >= 0), 4000);
    if (s.phase !== 'playing') break;
    const targets = parseTargets(s.prompt);
    let pick = new Set(targets);
    const deliberateMiss = !missed && s.tide === missOnTide && targets.size >= 2;
    if (deliberateMiss) { pick = new Set([...targets].slice(1)); missed = true; }
    log.push({ type: 'read', tide: s.tide, prompt: s.prompt, targets: [...targets], sweeping: [...pick], deliberateMiss });
    await setCapacity(targets.size, false); await sleep(250);
    const sc = await state();
    log.push({ type: 'capacity-set', want: targets.size, got: sc.capacity });
    await sweep(pick); await sleep(150);
    const after = await state();
    log.push({ type: 'submitted', state: brief(after) });
    if (deliberateMiss) { await sleep(500); await snap(`${label}-wrong-feedback`); }
    if (s.tide !== lastTide && s.tide === 1) { await sleep(300); await snap(`${label}-tide1-reveal`); }
    lastTide = s.tide;
    await waitPhase((x) => x.phase !== 'playing' || x.capacity === 0, 4000);
    await sleep(500);
  }
  s = await state();
  await snap(`${label}-end`);
  return s;
}

await snap('title');
await page.mouse.click(W / 2, H / 2);
log.push({ type: 'tap', x: W / 2, y: H / 2 });
await practice('run1');
const end1 = await playRun('run1', 2);
await page.mouse.click(W / 2, H / 2);
log.push({ type: 'tap-restart', x: W / 2, y: H / 2 });
await practice('run2');
const end2 = await playRun('run2', -1);

const hash = crypto.createHash('sha256');
for (const f of fs.readdirSync(path.join(ROOT, 'public/g/ssak-geonjyeo/Build')).sort()) hash.update(fs.readFileSync(path.join(ROOT, 'public/g/ssak-geonjyeo/Build', f)));
const result = { slug: 'ssak-geonjyeo', viewport: '390x844@2', build_hash: hash.digest('hex').slice(0, 16), hooks_used: ['getState (read only)'], run1: brief(end1), run2: brief(end2), events: log };
fs.writeFileSync(path.join(OUT, 'result.json'), JSON.stringify(result, null, 2));
console.log(JSON.stringify({ run1: result.run1, run2: result.run2, build_hash: result.build_hash }, null, 1));
await browser.close(); await srv.close(); process.exit(0);
