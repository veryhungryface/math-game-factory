#!/usr/bin/env node
// 최종 WebGL 두 판을 실제 pointer로 완주한다. 정답 훅/문제은행/상태 prompt는 읽지 않는다.
// macOS Vision OCR로 캔버스에 실제 렌더된 발문과 12개 게 라벨만 읽어 답 집합을 계산한다.
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { execFileSync } from 'node:child_process';
import puppeteer from 'puppeteer';

const root = path.resolve(process.cwd());
const out = path.join(root, 'factory/unity-src/ssak-geonjyeo/ArtSource/validation/two-run-pointer-ocr');
fs.mkdirSync(out, { recursive: true });
const ocrSource = path.join(root, 'factory/unity-src/ssak-geonjyeo/ArtSource/validation/screen-ocr.swift');
const ocrBinary = path.join(os.tmpdir(), `mgf-ssak-ocr-${process.pid}`);
execFileSync('swiftc', [ocrSource, '-o', ocrBinary], { stdio: 'inherit' });

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const rel = 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing';
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
  throw new Error('Chrome for Testing not found');
}

const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
const SCALE = 2;
const VIEW_W = 390 * SCALE;
const VIEW_H = 844 * SCALE;
const P = (x, y) => ({ x: x * SCALE, y: y * SCALE });
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--use-angle=metal', '--mute-audio'],
});
const page = await browser.newPage();
await page.setViewport({ width: VIEW_W, height: VIEW_H, deviceScaleFactor: 1 });
await page.goto(process.env.SSAK_URL || 'http://127.0.0.1:8127/g/ssak-geonjyeo/', { waitUntil: 'networkidle2', timeout: 45_000 });
await page.waitForFunction('window.__GAME_TEST__?.ready === true', { timeout: 20_000 });

const events = [];
async function tap(x, y) {
  await page.mouse.move(x, y); await page.mouse.down(); await sleep(70); await page.mouse.up(); await sleep(260);
}
async function drag(points) {
  await page.mouse.move(points[0].x, points[0].y); await page.mouse.down();
  for (let i = 1; i < points.length; i++) await page.mouse.move(points[i].x, points[i].y, { steps: 3 });
  await sleep(50); await page.mouse.up(); await sleep(260);
}
async function state() {
  return page.evaluate(() => {
    const s = window.__GAME_TEST__.getState();
    return {
      phase: s.phase, score: s.score, lives: s.lives, solved: s.solved, tide: s.tide,
      capacity: s.capacity, selectedIds: s.selectedIds, attempts: s.attempts,
      firstAttemptTotal: s.firstAttemptTotal, firstAttemptCorrect: s.firstAttemptCorrect,
      onboarding: s.onboarding, currentProblem: s.currentProblem,
    };
  });
}
async function screenshot(name) {
  const file = path.join(out, `${name}.png`);
  await page.screenshot({ path: file });
  return file;
}
function ocr(file) {
  return JSON.parse(execFileSync(ocrBinary, [file], { encoding: 'utf8', maxBuffer: 4 * 1024 * 1024 }));
}
function normalize(text) {
  return text.replace(/[\s·.,:]/g, '').replaceAll('집계번호', '집게번호').replaceAll('등번오', '등번호');
}
function digits(text, max) {
  return [...text.matchAll(/[1-9]/g)].map((m) => Number(m[0])).filter((n) => n <= max);
}
function eventSet(description) {
  const text = normalize(description);
  const bi = text.indexOf('등번호가');
  const ci = text.indexOf('집게번호가');
  let backs = [1, 2, 3, 4];
  let claws = [1, 2, 3];
  if (bi >= 0) {
    const end = ci >= 0 ? ci : text.length;
    backs = digits(text.slice(bi + 4, end), 4);
  }
  if (ci >= 0) claws = digits(text.slice(ci + 5), 3);
  if ((bi >= 0 && !backs.length) || (ci >= 0 && !claws.length)) throw new Error(`발문 숫자 OCR 실패: ${description}`);
  const result = new Set();
  for (const back of backs) for (const claw of claws) result.add(back * 10 + claw);
  return result;
}
function answerFromPrompt(prompt) {
  const compact = normalize(prompt);
  if (compact.includes('사건A') && compact.includes('사건B')) {
    const ai = compact.indexOf('사건A');
    const bi = compact.indexOf('사건B');
    const end = compact.indexOf('A또는B');
    const a = eventSet(compact.slice(ai, bi));
    const b = eventSet(compact.slice(bi, end >= 0 ? end : compact.length));
    return new Set([...a, ...b]);
  }
  return eventSet(compact);
}
function screenModel(rows) {
  const promptRows = rows.filter((r) => r.y > .79 && r.y < .95).sort((a, b) => b.y - a.y);
  const prompt = promptRows.map((r) => r.text).join(' ');
  const labels = [];
  for (const row of rows.filter((r) => r.y > .42 && r.y < .72)) {
    const text = normalize(row.text).replaceAll('집기', '집게').replaceAll('절게', '집게');
    const match = text.match(/등([1-4])집게([1-3])/);
    if (!match) continue;
    labels.push({
      id: Number(match[1]) * 10 + Number(match[2]),
      text: row.text,
      x: (row.x + row.w / 2) * VIEW_W,
      // OCR 상자는 번호표 중심이다. 실제 sweep 히트 중심은 몸통 쪽으로 약 52px 아래다.
      y: (1 - row.y - row.h / 2) * VIEW_H + 52,
    });
  }
  const unique = new Map(labels.map((label) => [label.id, label]));
  if (unique.size !== 12) throw new Error(`게 라벨 OCR ${unique.size}/12: ${JSON.stringify(labels)}`);
  const answer = answerFromPrompt(prompt);
  if (answer.size < 3 || answer.size > 6) throw new Error(`답 집합 OCR 오류(${answer.size}): ${prompt}`);
  return { prompt, labels: [...unique.values()], answer: [...answer].sort((a, b) => a - b) };
}

function hitsObstacle(a, b, obstacles, radius = 84) {
  const dx = b.x - a.x, dy = b.y - a.y;
  const dd = dx * dx + dy * dy;
  return obstacles.some((o) => {
    const t = dd < 1 ? 0 : Math.max(0, Math.min(1, ((o.x - a.x) * dx + (o.y - a.y) * dy) / dd));
    const px = a.x + dx * t, py = a.y + dy * t;
    return Math.hypot(o.x - px, o.y - py) < radius;
  });
}
function pathToTargets(labels, answer) {
  const targets = labels.filter((l) => answer.includes(l.id));
  const obstacles = labels.filter((l) => !answer.includes(l.id));
  const bounds = { minX: 16, maxX: 764, minY: 410, maxY: 1180 };
  const step = 24;
  const start = P(350, 560);
  const route = [start];
  let current = start;
  const remaining = targets.slice();
  while (remaining.length) {
    remaining.sort((a, b) => Math.hypot(a.x - current.x, a.y - current.y) - Math.hypot(b.x - current.x, b.y - current.y));
    const target = remaining.shift();
    const key = (x, y) => `${x},${y}`;
    const sx = Math.round(current.x / step), sy = Math.round(current.y / step);
    const tx = Math.round(target.x / step), ty = Math.round(target.y / step);
    const queue = [[sx, sy]];
    const parent = new Map([[key(sx, sy), null]]);
    const dirs = [[1,0],[-1,0],[0,1],[0,-1],[1,1],[1,-1],[-1,1],[-1,-1]];
    let found = null;
    for (let qi = 0; qi < queue.length && !found; qi++) {
      const [x, y] = queue[qi];
      for (const [ox, oy] of dirs) {
        const nx = x + ox, ny = y + oy;
        const p = { x: nx * step, y: ny * step };
        const k = key(nx, ny);
        if (parent.has(k) || p.x < bounds.minX || p.x > bounds.maxX || p.y < bounds.minY || p.y > bounds.maxY) continue;
        if (obstacles.some((o) => Math.hypot(o.x - p.x, o.y - p.y) < 92)) continue;
        parent.set(k, [x, y]); queue.push([nx, ny]);
        if (Math.hypot(p.x - target.x, p.y - target.y) < step * 1.5) { found = [nx, ny]; break; }
      }
    }
    if (!found) throw new Error(`경로 탐색 실패: ${target.id}`);
    const segment = [{ x: target.x, y: target.y }];
    for (let node = found; node; node = parent.get(key(node[0], node[1]))) segment.push({ x: node[0] * step, y: node[1] * step });
    segment.reverse();
    // 직선 시야가 확보되는 노드만 남겨 pointer 이벤트 수를 줄인다.
    let anchor = current;
    for (let i = 1; i < segment.length; ) {
      let far = i;
      while (far + 1 < segment.length && !hitsObstacle(anchor, segment[far + 1], obstacles)) far++;
      route.push(segment[far]); anchor = segment[far]; i = far + 1;
    }
    current = target;
  }
  return route;
}

async function completePractice(run) {
  await tap(195 * SCALE, 420 * SCALE);
  await tap(80 * SCALE, 560 * SCALE); // 실패 탭 뒤 2칸 시범 이동
  await drag([P(340, 560), P(120, 365), P(280, 365)]);
  await screenshot(`run${run}-practice-correct`);
  await sleep(1750);
}
async function setCapacityByPointer(n) {
  const target = P(25 + (n - 1) * 26, 565);
  const observed = [];
  for (const start of [P(25, 565), P(42, 565), P(25, 548), P(42, 548), P(30, 580), P(50, 580)]) {
    await drag([start, target]);
    const after = await state();
    observed.push({ start, target, capacity: after.capacity });
    if (after.capacity === n) return after;
  }
  throw new Error(`칸 수 실제 포인터 입력 실패: ${n} / ${JSON.stringify(observed)}`);
}
async function completeRun(run) {
  for (let tide = 1; tide <= 7; tide++) {
    const before = await screenshot(`run${run}-tide${tide}-screen`);
    const rows = ocr(before);
    const model = screenModel(rows);
    const n = model.answer.length;
    await setCapacityByPointer(n);
    const route = pathToTargets(model.labels, model.answer);
    console.log(JSON.stringify({ run, tide, promptOcr: model.prompt, answerFromScreen: model.answer, route }));
    await drag(route);
    const submitted = await state();
    events.push({ run, tide, promptOcr: model.prompt, labelsOcr: model.labels, answerFromScreen: model.answer, route, submitted });
    await screenshot(`run${run}-tide${tide}-submitted`);
    if (submitted.lives < 3 || submitted.firstAttemptCorrect !== tide || submitted.solved !== tide)
      throw new Error(`run${run} tide${tide} 첫 시도 실패: ${JSON.stringify(submitted)}`);
    await sleep(1750);
  }
  const final = await state();
  await screenshot(`run${run}-end`);
  return final;
}

const title = await screenshot('00-title');
await completePractice(1);
const run1 = await completeRun(1);
await tap(195 * SCALE, 420 * SCALE);
await completePractice(2);
const run2 = await completeRun(2);
const result = { title, run1, run2, events, answerHooksCalled: false, input: 'page.mouse pointer only', source: 'Vision OCR of rendered prompt and crab labels' };
fs.writeFileSync(path.join(out, 'results.json'), JSON.stringify(result, null, 2));
console.log(JSON.stringify({ run1, run2, rounds: events.length, answerHooksCalled: false }));
await browser.close();
fs.rmSync(ocrBinary, { force: true });
