#!/usr/bin/env node
// 2차 수정 회귀: 절단 안내 그대로의 실입력, 첫 무효 입력 후 확장 히트영역,
// 연습2→본판 전이, 820 세로/1280×1400 land 판형, 최종 WebGL 360문항을 한 산출물에서 확인한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/gereul-kkeonae');
const OUT = path.join(HERE, 'fix2');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome',
  ]) { const p = path.join(base, build, rel); if (fs.existsSync(p)) return p; }
  throw new Error('Chrome for Testing not found');
}

function artifactHash() {
  const hash = crypto.createHash('sha256');
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(x => `Build/${x}`)]) {
    hash.update(name); hash.update(fs.readFileSync(path.join(GAME, name)));
  }
  return hash.digest('hex');
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, label, timeout = 7000) {
  const start = Date.now();
  while (Date.now() - start < timeout) { const s = await state(page); if (predicate(s)) return s; await sleep(30); }
  throw new Error(`timeout ${label}: ${JSON.stringify(await state(page))}`);
}
function browserPoint(s, x, unityY, height) { return [x, height - unityY]; }
function lengthPoint(s, cm, height) {
  const t = (cm - 1) / 23;
  return s.layout === 'land'
    ? browserPoint(s, s.handleX, s.rulerMin + (s.rulerMax - s.rulerMin) * t, height)
    : browserPoint(s, s.rulerMin + (s.rulerMax - s.rulerMin) * t, s.handleY, height);
}
async function drag(page, from, to) {
  await page.mouse.move(...from); await page.mouse.down(); await sleep(55);
  await page.mouse.move(...to, { steps: 8 }); await sleep(75); await page.mouse.up(); await sleep(70);
}
async function open(page, serverUrl, width, height, dpr = 1) {
  await page.setViewport({ width, height, deviceScaleFactor: dpr });
  await page.goto(`${serverUrl}/g/gereul-kkeonae/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  await sleep(400);
}
async function enterPractice(page, height, answer) {
  await page.mouse.click(195, height / 2);
  let s = await waitFor(page, x => x.problemId === 'practice-angle' && x.toolStep === 'pull', 'practice one');
  await drag(page, browserPoint(s, s.handleX, s.handleY, height), lengthPoint(s, answer, height));
  return waitFor(page, x => x.toolStep === 'cut', 'cut step');
}
async function connect(page, s, height) {
  await drag(page, browserPoint(s, s.pieceX, s.pieceY, height), browserPoint(s, s.gapX, s.gapY, height));
  return waitFor(page, x => x.phase === 'reveal', 'reveal');
}

fs.mkdirSync(OUT, { recursive: true });
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), protocolTimeout: 900000, args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const errors = [];
const result = { runId: `gereul-kkeonae-fix2-${new Date().toISOString()}`, artifactHashSha256: artifactHash() };
try {
  // 수정 전 프레임이 가리킨 ±58px 가이드 시작·도착을 그대로 드래그한다.
  const guided = await browser.newPage(); guided.on('console', m => { if (m.type() === 'error') errors.push(m.text()); }); guided.on('pageerror', e => errors.push(String(e)));
  await open(guided, server.url, 390, 844);
  let s = await enterPractice(guided, 844, 8);
  const guideStart = browserPoint(s, s.cutX - 58, s.cutY, 844), guideFinish = browserPoint(s, s.cutX + 58, s.cutY, 844);
  await drag(guided, guideStart, guideFinish);
  s = await waitFor(guided, x => x.toolStep === 'connect', 'same guide swipe reaches connect');
  await guided.screenshot({ path: path.join(OUT, '01-guide-swipe-cut.png') });
  await connect(guided, s, 844);
  s = await waitFor(guided, x => x.problemId === 'practice-bisector' && x.toolStep === 'pull', 'practice two');
  await drag(guided, browserPoint(s, s.handleX, s.handleY, 844), lengthPoint(s, 6, 844));
  s = await waitFor(guided, x => x.toolStep === 'cut', 'practice two cut');
  await drag(guided, browserPoint(s, s.cutX - 58, s.cutY, 844), browserPoint(s, s.cutX + 58, s.cutY, 844));
  s = await waitFor(guided, x => x.toolStep === 'connect', 'practice two connect');
  await connect(guided, s, 844);
  const runState = await waitFor(guided, x => x.phase === 'playing' && x.round === 1 && !x.onboarding, 'main run', 8000);
  await guided.screenshot({ path: path.join(OUT, '02-main-run-entered.png') });
  result.sameGuideInput = { from: guideStart, to: guideFinish, reachedConnect: true, mainRun: { round: runState.round, problemId: runState.problemId } };
  await guided.close();

  // 첫 무효 입력은 확장 가이드를 보이고, 다음 눈금 띠 크로스 드래그를 받아야 한다.
  const assisted = await browser.newPage(); assisted.on('console', m => { if (m.type() === 'error') errors.push(m.text()); }); assisted.on('pageerror', e => errors.push(String(e)));
  await open(assisted, server.url, 390, 844);
  s = await enterPractice(assisted, 844, 8);
  const miss = browserPoint(s, s.rulerMin + 18, s.handleY, 844);
  await assisted.mouse.click(...miss); await sleep(300);
  const afterMiss = await state(assisted);
  await assisted.screenshot({ path: path.join(OUT, '03-expanded-cut-assist.png') });
  await drag(assisted, browserPoint(afterMiss, afterMiss.rulerMin + 24, afterMiss.cutY, 844), browserPoint(afterMiss, afterMiss.rulerMax - 24, afterMiss.cutY, 844));
  const afterAssist = await waitFor(assisted, x => x.toolStep === 'connect', 'expanded ruler swipe reaches connect');
  result.invalidThenExpanded = { invalidInputCount: afterMiss.inputCount, toolAfterInvalid: afterMiss.toolStep, reachedConnect: afterAssist.toolStep === 'connect' };
  await assisted.close();

  // 템플릿과 Unity가 같은 CSS 판형 판정을 쓰는지 DPR 2 태블릿과 1280×1400에서 교차 확인한다.
  for (const spec of [
    { name: 'tablet-820x1180', width: 820, height: 1180, dpr: 2, expected: false },
    { name: 'desktop-1280x1400', width: 1280, height: 1400, dpr: 1, expected: true },
  ]) {
    const page = await browser.newPage(); await open(page, server.url, spec.width, spec.height, spec.dpr);
    const hook = await page.evaluate(() => window.__GAME_TEST__.getLayout());
    await page.evaluate(() => window.__GAME_TEST__.start());
    const st = await waitFor(page, x => x.phase === 'playing' && x.round === 1, `${spec.name} run layout`);
    await page.screenshot({ path: path.join(OUT, `${spec.name}.png`) });
    result[spec.name] = { hookLand: hook.land, unityLayout: st.layout, expected: spec.expected };
    await page.close();
  }

  // 실제 WebGL이 푸시한 전체 은행을 별도 시험지 없이 독립 정수 규칙으로 확인한다.
  const mathPage = await browser.newPage(); await open(mathPage, server.url, 390, 844);
  const bank = await mathPage.evaluate(() => window.__GAME_TEST__.sampleProblems(999));
  const ids = new Set(), prompts = new Set(), mathErrors = [];
  const bands = [0, 0, 0, 0];
  for (const p of bank) {
    const m = /^gk-b([123])-l(\d{2})-a([1-5])$/.exec(p.id || '');
    if (!m) { mathErrors.push(`bad id ${p.id}`); continue; }
    const band = Number(m[1]), answer = Number(m[2]); bands[band]++;
    if (ids.has(p.id) || prompts.has(p.prompt)) mathErrors.push(`duplicate ${p.id}`);
    ids.add(p.id); prompts.add(p.prompt);
    if (p.answerNumeric !== answer || p.answer !== `${answer} cm`) mathErrors.push(`answer mismatch ${p.id}`);
    if (band === 1 && !p.prompt.startsWith('삼각형 ABC에서 ')) mathErrors.push(`declaration missing ${p.id}`);
    if (band >= 2 && !p.prompt.startsWith('AB=AC인 이등변삼각형 ABC에서 ')) mathErrors.push(`declaration missing ${p.id}`);
    if (!p.prompt.endsWith('길이를 구하시오.')) mathErrors.push(`ending mismatch ${p.id}`);
  }
  result.mathBank = { count: bank.length, uniqueIds: ids.size, uniquePrompts: prompts.size, perBand: bands.slice(1), errors: mathErrors };
  await mathPage.close();

  result.consoleErrors = errors;
  result.passed = result.sameGuideInput.reachedConnect && result.sameGuideInput.mainRun.round === 1 &&
    result.invalidThenExpanded.reachedConnect && result['tablet-820x1180'].hookLand === false && result['tablet-820x1180'].unityLayout === 'portrait' &&
    result['desktop-1280x1400'].hookLand === true && result['desktop-1280x1400'].unityLayout === 'land' &&
    bank.length === 360 && ids.size === 360 && prompts.size === 360 && mathErrors.length === 0 && errors.length === 0;
  fs.writeFileSync(path.join(HERE, 'fix2-regression-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close(); await server.close();
}
