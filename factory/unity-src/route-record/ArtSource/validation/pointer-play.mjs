#!/usr/bin/env node
// 답 훅 없이 실제 pointer 이벤트(탭·드래그)만으로 루트 레코드를 플레이한다.
//  - 타이틀 → 톤암 탭 → 연습(노치 5개 탭 + 톤암 드래그) → 본판 6장
//  - normal: 모든 재킷을 첫 시도에 맞힌다 / recovery: 첫 재킷에서 한 번 틀린 뒤 고쳐서 완주
//  - 단계별 스크린숏을 shots/<viewport>/ 에 남긴다.
// 사용: node pointer-play.mjs [390x844|1280x800 ...]
import fs from 'node:fs';
import crypto from 'node:crypto';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/route-record');
const WORK = path.resolve(ROOT, process.env.MGF_WORK || 'factory/work');
const sleep = ms => new Promise(r => setTimeout(r, ms));
const RUN_ID = process.env.MGF_RUN_ID || `route-record-fix1-${new Date().toISOString()}`;

function artifactHash() {
  const hash = crypto.createHash('sha256');
  const rels = ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(name => `Build/${name}`)];
  for (const rel of rels) {
    hash.update(rel);
    hash.update(fs.readFileSync(path.join(GAME, rel)));
  }
  return hash.digest('hex');
}

function stampedEvidence(file, artifactHashValue, expectedSlug) {
  if (!fs.existsSync(file)) throw new Error(`evidence file missing: ${file}`);
  const data = JSON.parse(fs.readFileSync(file, 'utf8'));
  if (data.slug !== expectedSlug) throw new Error(`evidence slug mismatch in ${file}: ${data.slug}`);
  data.run_id = RUN_ID;
  data.artifact_hash = artifactHashValue;
  data.evidence_captured_at = new Date().toISOString();
  return data;
}

function atomicWriteJson(file, data) {
  const temp = `${file}.tmp-${process.pid}`;
  fs.writeFileSync(temp, JSON.stringify(data, null, 2) + '\n');
  fs.renameSync(temp, file);
}

function verifyEvidenceChain(files, artifactHashValue) {
  const evidence = files.map((file) => ({ file, data: JSON.parse(fs.readFileSync(file, 'utf8')) }));
  for (const { file, data } of evidence) {
    if (data.run_id !== RUN_ID) throw new Error(`evidence run_id mismatch in ${file}: ${data.run_id}`);
    if (data.artifact_hash !== artifactHashValue) throw new Error(`evidence artifact_hash mismatch in ${file}: ${data.artifact_hash}`);
  }
  const current = artifactHash();
  if (current !== artifactHashValue) throw new Error(`current WebGL artifact mismatch: ${current} != ${artifactHashValue}`);
  return { run_id: RUN_ID, artifact_hash: artifactHashValue, current_artifact_hash: current, files: files.map((file) => path.relative(ROOT, file)) };
}

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const c = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(c)) return c;
  }
  throw new Error('Chrome for Testing not found');
}

// 판정 규칙의 독립 재구현(검산용): 한 면 안은 곱, 두 면은 합.
const SIDES = { Serial2: [0, 0], Serial3: [0, 0, 0], Sum2: [0, 1], MixA2: [0, 0, 1], MixB2: [0, 1, 1] };
function paths(kind, c) {
  const side = SIDES[kind]; const prod = [1, 1]; const has = [false, false];
  c.forEach((v, i) => { prod[side[i]] *= v; has[side[i]] = true; });
  return (has[0] ? prod[0] : 0) + (has[1] ? prod[1] : 0);
}
function* vecs(m) { if (!m.length) { yield []; return; } for (const r of vecs(m.slice(1))) for (let a = 0; a <= m[0]; a++) yield [a, ...r]; }
function solve(st, wrong = false) {
  const m = st.maxima.split('-').map(Number);
  let best = null;
  for (const c of vecs(m)) {
    const used = c.reduce((a, b) => a + b, 0);
    if (used > st.budget || used === 0) continue;
    const ok = paths(st.structure, c) === st.target;
    if (ok !== !wrong) continue;
    if (wrong && paths(st.structure, c) === 0) continue;
    if (!best || used < best.used) best = { c, used };
  }
  return best.c;
}

function targets(st, W, H) {
  const out = {};
  for (const part of (st.inputTargets || '').split('|')) {
    if (!part) continue;
    const [k, v] = part.split(':'); const [x, y] = v.split(',').map(Number);
    out[k] = [x * W, y * H];
  }
  return out;
}

async function run(viewport, mode) {
  const [W, H] = viewport.split('x').map(Number);
  const server = await serveStatic(PUBLIC);
  const browser = await puppeteer.launch({ executablePath: chromePath(), headless: 'new', protocolTimeout: 120000, args: ['--autoplay-policy=no-user-gesture-required'] });
  const page = await browser.newPage();
  const errors = [];
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', e => errors.push(String(e)));
  await page.setViewport({ width: W, height: H, deviceScaleFactor: 1, isMobile: W < 800, hasTouch: W < 800 });
  await page.goto(`${server.url}/g/route-record/index.html`, { waitUntil: 'load' });
  await page.waitForFunction(() => window.__GAME_TEST__ && window.__GAME_TEST__.ready, { timeout: 30000 });
  const shotDir = path.join(HERE, 'shots', `${viewport}-${mode}`);
  fs.mkdirSync(shotDir, { recursive: true });
  let n = 0;
  const shot = async name => page.screenshot({ path: path.join(shotDir, `${String(n++).padStart(2, '0')}-${name}.png`) });
  const state = () => page.evaluate(() => window.__GAME_TEST__.getState());
  const waitFor = async (pred, label, ms = 9000) => {
    const t0 = Date.now();
    while (Date.now() - t0 < ms) { const s = await state(); if (pred(s)) return s; await sleep(70); }
    throw new Error(`timeout: ${label} ${JSON.stringify(await state())}`);
  };
  const tap = async ([x, y]) => { await page.mouse.move(x, y); await page.mouse.down(); await sleep(40); await page.mouse.up(); await sleep(60); };
  const drag = async ([x0, y0], [x1, y1]) => {
    await page.mouse.move(x0, y0); await page.mouse.down(); await sleep(40);
    for (let i = 1; i <= 8; i++) { await page.mouse.move(x0 + (x1 - x0) * i / 8, y0 + (y1 - y0) * i / 8); await sleep(16); }
    await sleep(40); await page.mouse.up(); await sleep(80);
  };
  const lowerArm = async t => {
    const from = t.armFrom, to = t.armTo;
    const over = [to[0] + (to[0] - from[0]) * .25, to[1] + (to[1] - from[1]) * .25];
    await drag(from, over);
  };
  const placeCounts = async (st, counts) => {
    const t = targets(st, W, H);
    for (let g = 0; g < counts.length; g++) for (let j = 0; j < counts[g]; j++) await tap(t[`s${g}-${j}`]);
  };
  const clearAll = async st => {
    const t = targets(st, W, H);
    for (const id of (st.placedBridgeIds || '').split(',').filter(Boolean)) {
      const g = id.charCodeAt(0) - 65, j = Number(id.slice(1)) - 1; await tap(t[`s${g}-${j}`]);
    }
  };

  await sleep(1200); await shot('title');
  // 첫 탭이 레코드 바깥(빈 곳)이면 거절 피드백만
  await tap([W * .5, H * .03]); await sleep(150); await shot('title-refuse');
  await tap([W * .55, H * .5]);
  let st = await waitFor(s => s.onboarding && (s.inputTargets || '').includes('s1-2'), 'practice');
  await sleep(300); await shot('practice-start');
  await sleep(8500); await shot('practice-idle-8s');
  const t0 = targets(st, W, H);
  await tap(t0['s0-0']); await sleep(120); await shot('practice-first-bridge');
  await drag(t0.bin, t0['s0-1']); await tap(t0['s1-0']); await tap(t0['s1-1']); await tap(t0['s1-2']);
  st = await waitFor(s => s.bridgesUsed === 5, 'practice filled'); await shot('practice-filled');
  await lowerArm(t0); await sleep(500); await shot('practice-test-pulses');
  await sleep(1400); await shot('practice-reveal');
  st = await waitFor(s => !s.onboarding && s.phase === 'playing' && (s.inputTargets || '').includes('s0-0') && s.currentProblemId.startsWith('rr-'), 'main start', 12000);
  await sleep(500); await shot('main-1');

  const log = [];
  for (let jacket = 0; jacket < 6; jacket++) {
    st = await waitFor(s => s.solved === jacket && !s.tonearmLocked && (s.inputTargets || '').length > 0, `jacket ${jacket}`, 12000);
    await sleep(350);
    st = await state();
    if (mode === 'recovery' && jacket === 0) {
      const w = solve(st, true);
      await placeCounts(st, w);
      const t = targets(await state(), W, H);
      await lowerArm(t); await sleep(250);
      const after = await state();
      log.push({ jacket, structure: st.structure, target: st.target, wrongCounts: w, pathCount: after.pathCountAfterSubmit, needles: after.needles });
      await sleep(600); await shot(`jacket${jacket + 1}-wrong-test`);
      await waitFor(s => !s.tonearmLocked, 'wrong resolved', 9000); await sleep(300); await shot(`jacket${jacket + 1}-wrong-after`);
      st = await state(); await clearAll(st); st = await state();
    }
    const c = solve(st);
    await placeCounts(st, c);
    st = await state();
    if (jacket === 0 || jacket === 3 || jacket === 4) await shot(`jacket${jacket + 1}-built`);
    await lowerArm(targets(st, W, H)); await sleep(300);
    const after = await state();
    log.push({ jacket, structure: st.structure, maxima: st.maxima, target: st.target, budget: st.budget, counts: c, pathCount: after.pathCountAfterSubmit, solved: after.solved });
    if (jacket === 0 || jacket === 4) { await sleep(700); await shot(`jacket${jacket + 1}-test`); }
    if (jacket === 4) { await sleep(1200); await shot(`jacket${jacket + 1}-chord`); }
  }
  st = await waitFor(s => s.phase === 'clear' || s.phase === 'gameover', 'end', 12000);
  await sleep(5200); await shot("end");
  const end = await state();
  await browser.close(); await server.close();
  return { viewport, mode, phase: end.phase, solved: end.solved, firstCorrect: end.firstCorrect, firstAttempted: end.firstAttempted, needles: end.needles, score: end.score, errors, log };
}

const views = process.argv.slice(2).length ? process.argv.slice(2) : ['390x844'];
const artifactHashBefore = artifactHash();
const results = [];
for (const v of views) for (const mode of ['normal', 'recovery']) {
  try { results.push(await run(v, mode)); } catch (e) { results.push({ viewport: v, mode, error: String(e && e.stack || e) }); }
  console.log(JSON.stringify(results[results.length - 1]));
}
const artifactHashAfter = artifactHash();
if (artifactHashAfter !== artifactHashBefore) throw new Error(`WebGL artifact changed during pointer run: ${artifactHashBefore} -> ${artifactHashAfter}`);
const bad = results.filter(r => r.error || r.phase !== 'clear' || (r.errors && r.errors.length));
if (bad.length) throw new Error(`pointer evidence incomplete: ${JSON.stringify(bad)}`);
const qaReport = path.join(WORK, 'qa/route-record/report.json');
const firstplayManifest = path.join(WORK, 'qa/route-record/firstplay/route-record/manifest.json');
const pointerResults = path.join(HERE, 'pointer-play-results.json');
const evidenceFiles = [qaReport, firstplayManifest, pointerResults];
const pointerData = {
  run_id: RUN_ID,
  artifact_hash: artifactHashBefore,
  generatedAt: new Date().toISOString(),
  results
};
// 세 JSON을 모두 준비한 뒤 rename으로 교체하고, 바로 현재 산출물까지 다시 대조한다.
atomicWriteJson(qaReport, stampedEvidence(qaReport, artifactHashBefore, 'route-record'));
atomicWriteJson(firstplayManifest, stampedEvidence(firstplayManifest, artifactHashBefore, 'route-record'));
atomicWriteJson(pointerResults, pointerData);
pointerData.evidence_chain = verifyEvidenceChain(evidenceFiles, artifactHashBefore);
atomicWriteJson(pointerResults, pointerData);
verifyEvidenceChain(evidenceFiles, artifactHashBefore);
