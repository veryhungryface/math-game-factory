#!/usr/bin/env node
// 독립 플레이 검증: PNG 화면 OCR만으로 숫자와 위치를 읽는다.
// __GAME_TEST__는 ready 확인에만 쓰며 start/getState/sampleProblems/정답 훅은 호출하지 않는다.
import fs from 'node:fs';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const OUT = path.join(HERE, 'pointer-playthrough');
const OCR_BIN = '/tmp/beol-screen-ocr';
const OCR_SWIFT = path.join(HERE, 'screen-ocr.swift');
fs.mkdirSync(OUT, { recursive: true });
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

function chromePath() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome',
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
  throw new Error('Chrome for Testing not found');
}

execFileSync('swiftc', [OCR_SWIFT, '-o', OCR_BIN], { stdio: 'inherit' });
function ocr(file) { return JSON.parse(execFileSync(OCR_BIN, [file], { encoding: 'utf8' })); }
function visibleText(rows) { return rows.map((row) => row.text).join(' | '); }
function developedCount(rows) {
  for (const row of rows) {
    const match = row.text.match(/현상\s*(\d)\s*\/\s*9/);
    if (match) return Number(match[1]);
  }
  return null;
}

const BOARD_CENTERS = [
  { x: 95, y: 278 }, { x: 206, y: 292 }, { x: 317, y: 306 },
  { x: 61, y: 374 }, { x: 172, y: 387 }, { x: 284, y: 400 },
  { x: 87, y: 460 }, { x: 199, y: 473 }, { x: 308, y: 486 },
];

async function screenshotOcr(page, name) {
  const file = path.join(OUT, name);
  await page.screenshot({ path: file });
  return { file, rows: ocr(file) };
}

function boardFromScreenshot(file) {
  const rows = ocr(file).filter((row) => /^\d{1,2}$/.test(row.text));
  const candidates = rows.map((row) => ({
    value: Number(row.text),
    x: (row.x + row.w / 2) * 390,
    y: (1 - row.y - row.h / 2) * 844 + 24,
    confidence: row.confidence,
  })).filter((item) => item.y > 145 && item.y < 700 && item.x > 18 && item.x < 372)
    .sort((a, b) => b.confidence - a.confidence);
  const unique = [];
  for (const candidate of candidates) {
    if (unique.some((item) => Math.hypot(item.x - candidate.x, item.y - candidate.y) < 24)) continue;
    unique.push(candidate);
  }
  const used = new Set();
  return BOARD_CENTERS.map((center, index) => {
    let best = -1, bestDistance = 58;
    for (let i = 0; i < unique.length; i++) {
      if (used.has(i)) continue;
      const distance = Math.hypot(unique[i].x - center.x, unique[i].y - center.y);
      if (distance < bestDistance) { best = i; bestDistance = distance; }
    }
    if (best >= 0) { used.add(best); return unique[best]; }

    // 전체 화면 Vision이 같은 숫자가 연속된 칸 하나를 가끔 생략한다. 그 칸만
    // 화면에서 다시 확대 OCR한다. 상태·문제은행·정답 훅은 여전히 읽지 않는다.
    const crop = `/tmp/beol-tag-${process.pid}-${index}.png`;
    const left = Math.max(0, Math.round(center.x - 26));
    const top = Math.max(0, Math.round(center.y - 47));
    execFileSync('ffmpeg', ['-y', '-loglevel', 'error', '-i', file, '-vf', `crop=52:46:${left}:${top},scale=624:552`, crop]);
    const digit = ocr(crop).filter((row) => /^\d{1,2}$/.test(row.text)).sort((a, b) => b.confidence - a.confidence)[0];
    if (!digit) throw new Error(`화면 OCR 숫자표 ${index + 1}번을 확대해도 읽지 못함`);
    return { value: Number(digit.text), x: center.x, y: center.y, confidence: digit.confidence };
  });
}

async function readBoard(page, prefix) {
  let last;
  for (let attempt = 0; attempt < 4; attempt++) {
    last = await screenshotOcr(page, `${prefix}${attempt ? `-retry${attempt}` : ''}.png`);
    try { return { ...last, bees: boardFromScreenshot(last.file) }; }
    catch (error) { if (attempt === 3) throw error; await sleep(250); }
  }
  throw new Error('unreachable');
}

function modeIndices(bees) {
  const freq = new Map();
  for (const bee of bees) freq.set(bee.value, (freq.get(bee.value) || 0) + 1);
  const maxFrequency = Math.max(...freq.values());
  return bees.map((bee, index) => ({ bee, index })).filter(({ bee }) => freq.get(bee.value) === maxFrequency).map(({ index }) => index);
}

function deliberateWrongIndices(bees, answer) {
  const maxValue = Math.max(...bees.map((bee) => bee.value));
  let wrong = bees.map((bee, index) => ({ bee, index })).filter(({ bee }) => bee.value === maxValue).map(({ index }) => index);
  if (wrong.length === answer.length && wrong.every((value, index) => value === answer[index])) {
    const minValue = Math.min(...bees.map((bee) => bee.value));
    wrong = bees.map((bee, index) => ({ bee, index })).filter(({ bee }) => bee.value === minValue).map(({ index }) => index);
  }
  return wrong;
}

function distanceToSegment(point, a, b) {
  const dx = b.x - a.x, dy = b.y - a.y, n = dx * dx + dy * dy;
  const t = n < 1e-6 ? 0 : Math.max(0, Math.min(1, ((point.x - a.x) * dx + (point.y - a.y) * dy) / n));
  return Math.hypot(a.x + dx * t - point.x, a.y + dy * t - point.y);
}

function gridLeg(from, to, bees, selected, clearance) {
  const step = 14, minX = 8, maxX = 382, minY = 145, maxY = 710;
  const cols = Math.floor((maxX - minX) / step) + 1, rows = Math.floor((maxY - minY) / step) + 1;
  const key = (x, y) => y * cols + x;
  const point = (k) => ({ x: minX + (k % cols) * step, y: minY + Math.floor(k / cols) * step });
  const snap = (p) => ({ x: Math.max(0, Math.min(cols - 1, Math.round((p.x - minX) / step))), y: Math.max(0, Math.min(rows - 1, Math.round((p.y - minY) / step))) });
  const s = snap(from), g = snap(to), start = key(s.x, s.y), goal = key(g.x, g.y);
  const blocked = (k) => {
    const p = point(k);
    return bees.some((bee, index) => !selected.has(index) && Math.hypot(p.x - bee.x, p.y - bee.y) < clearance + 3);
  };
  const prev = new Int32Array(cols * rows); prev.fill(-2); prev[start] = -1;
  const queue = new Int32Array(cols * rows); let head = 0, tail = 0; queue[tail++] = start;
  const dirs = [[1,0],[-1,0],[0,1],[0,-1],[1,1],[1,-1],[-1,1],[-1,-1]];
  while (head < tail && prev[goal] === -2) {
    const cur = queue[head++], x = cur % cols, y = Math.floor(cur / cols);
    for (const [dx, dy] of dirs) {
      const nx = x + dx, ny = y + dy;
      if (nx < 0 || ny < 0 || nx >= cols || ny >= rows) continue;
      const next = key(nx, ny);
      if (prev[next] !== -2 || (next !== goal && blocked(next))) continue;
      prev[next] = cur; queue[tail++] = next;
    }
  }
  if (prev[goal] === -2) throw new Error('OCR 화면 경로를 찾지 못함');
  const reversed = [];
  for (let k = goal; k !== -1; k = prev[k]) reversed.push(point(k));
  reversed.reverse(); reversed[0] = from; reversed[reversed.length - 1] = to;
  const simple = [reversed[0]]; let anchor = 0;
  while (anchor < reversed.length - 1) {
    let far = reversed.length - 1;
    for (; far > anchor + 1; far--) {
      if (bees.every((bee, index) => selected.has(index) || distanceToSegment(bee, reversed[anchor], reversed[far]) >= clearance)) break;
    }
    simple.push(reversed[far]); anchor = far;
  }
  return simple;
}

function safePath(bees, indices, clearance = 38) {
  const selected = new Set(indices);
  const route = [{ x: bees[indices[0]].x, y: bees[indices[0]].y }];
  for (let i = 1; i < indices.length; i++) {
    const target = { x: bees[indices[i]].x, y: bees[indices[i]].y };
    const leg = gridLeg(route[route.length - 1], target, bees, selected, clearance);
    route.push(...leg.slice(1));
  }
  return route;
}

async function stroke(page, points) {
  await page.mouse.move(points[0].x, points[0].y); await page.mouse.down(); await sleep(60);
  for (let i = 1; i < points.length; i++) {
    const a = points[i - 1], b = points[i];
    await page.mouse.move(b.x, b.y, { steps: Math.max(6, Math.ceil(Math.hypot(b.x - a.x, b.y - a.y) / 12)) });
    await sleep(35);
  }
  await page.mouse.up();
}

async function solveVisibleBoard(page, prefix, wrong = false, clearance = 38, expectIncrement = true) {
  const view = await readBoard(page, prefix);
  const beforeDeveloped = developedCount(view.rows);
  const answer = modeIndices(view.bees);
  const chosen = wrong ? deliberateWrongIndices(view.bees, answer) : answer;
  await stroke(page, safePath(view.bees, chosen, clearance));
  const expected = beforeDeveloped + (expectIncrement ? 1 : 0);
  let after = null, afterDeveloped = null;
  for (let attempt = 0; attempt < 18; attempt++) {
    await sleep(attempt === 0 ? 1550 : 280);
    after = await screenshotOcr(page, `${prefix}-after${attempt ? `-${attempt}` : ''}.png`);
    afterDeveloped = developedCount(after.rows);
    if (afterDeveloped === expected && (expectIncrement ? attempt > 0 : attempt >= 2)) break;
  }
  if (beforeDeveloped === null || afterDeveloped === null) throw new Error(`${prefix}: 화면의 현상 진행을 OCR로 읽지 못함`);
  if (afterDeveloped !== expected) throw new Error(`${prefix}: 현상 ${beforeDeveloped}/9 → ${afterDeveloped}/9, 기대 ${expected}/9`);
  return { values: view.bees.map((bee) => bee.value), chosenValues: [...new Set(chosen.map((index) => view.bees[index].value))], answerValues: [...new Set(answer.map((index) => view.bees[index].value))], wrong, beforeDeveloped, afterDeveloped };
}

async function clickVisibleText(page, rows, regex, fallback) {
  const row = rows.find((item) => regex.test(item.text));
  const point = row ? { x: (row.x + row.w / 2) * 390, y: (1 - row.y - row.h / 2) * 844 } : fallback;
  await page.mouse.click(point.x, point.y); return point;
}

async function waitForText(page, regex, name, timeoutMs = 9000) {
  const started = Date.now(); let capture;
  while (Date.now() - started < timeoutMs) {
    capture = await screenshotOcr(page, `${name}-${Date.now() - started}.png`);
    if (regex.test(visibleText(capture.rows))) return capture;
    await sleep(300);
  }
  throw new Error(`${name}: 화면 전환을 기다리는 동안 ${regex}를 찾지 못함: ${visibleText(capture?.rows || [])}`);
}

async function runOne(page, run, injectWrong) {
  const details = [];
  if (run === 0) {
    const title = await screenshotOcr(page, '00-title.png');
    await clickVisibleText(page, title.rows, /빈.*건판|탑.*열기/, { x: 284, y: 405 });
    await sleep(1100);
    details.push({ stage: 'practice', ...(await solveVisibleBoard(page, '01-practice', false, 31, false)) });
  } else {
    const result = await waitForText(page, /현상\s*9\s*\/\s*9|꽃시계 현상 완료/, '20-run1-result-ready');
    await clickVisibleText(page, result.rows, /새.*건판|시작/, { x: 195, y: 558 });
    await sleep(900);
  }

  let mainSolved = 0;
  if (injectWrong) {
    details.push({ stage: 'main-1-wrong', ...(await solveVisibleBoard(page, '02-run1-wrong-before', true, 38, false)) });
    details.push({ stage: 'main-1-repair', ...(await solveVisibleBoard(page, '03-run1-repair-before')) });
    mainSolved = 1;
  }
  while (mainSolved < 9) {
    if (mainSolved === 6) details.push({ stage: 'concept-bridge', ...(await solveVisibleBoard(page, `${run ? '31' : '11'}-bridge-before`, false, 38, false)) });
    details.push({ stage: `main-${mainSolved + 1}`, ...(await solveVisibleBoard(page, `${run ? 32 : 12}-run${run + 1}-round-${mainSolved + 1}-before`)) });
    mainSolved++;
  }
  const end = await waitForText(page, /현상\s*9\s*\/\s*9|꽃시계 현상 완료/, run ? '40-run2-result-ready' : '20-run1-result-ready');
  await page.screenshot({ path: path.join(OUT, run ? '40-run2-result.png' : '20-run1-result.png') });
  return { run: run + 1, details, resultText: visibleText(end.rows), resultImage: run ? '40-run2-result.png' : '20-run1-result.png' };
}

const server = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), protocolTimeout: 180000, args: ['--no-sandbox', '--mute-audio', '--use-angle=metal'] });
const page = await browser.newPage();
const errors = [];
page.on('console', (message) => { if (message.type() === 'error') errors.push(message.text()); });
page.on('pageerror', (error) => errors.push(String(error)));
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
await page.goto(`${server.url}/g/beol-sodong/`, { waitUntil: 'networkidle2', timeout: 90000 });
await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });

let results;
let failure = null;
try {
  results = [await runOne(page, 0, true), await runOne(page, 1, false)];
} catch (error) {
  failure = String(error?.stack || error);
} finally {
  await browser.close(); await server.close();
}
const ok = !failure && errors.length === 0 && /현상\s*9\s*\/\s*9/.test(results[0].resultText) && /첫 제출\s*8\s*\/\s*9/.test(results[0].resultText) && /현상\s*9\s*\/\s*9/.test(results[1].resultText) && /첫 제출\s*9\s*\/\s*9/.test(results[1].resultText);
const payload = {
  method: 'screen-only OCR + real pointer; test hook used for ready only',
  forbiddenReads: ['getState', 'sampleProblems', 'start', 'answerCorrect', 'answerWrong'],
  passed: ok,
  failure,
  errors,
  results: results || [],
};
fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), JSON.stringify(payload, null, 2) + '\n');
console.log(JSON.stringify(payload, null, 2));
if (!ok) process.exitCode = 1;
