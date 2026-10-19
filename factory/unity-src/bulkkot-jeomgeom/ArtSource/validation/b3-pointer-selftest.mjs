import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const TRIALS = 200;
const VALIDATION_RUN_ID = process.env.VALIDATION_RUN_ID || `bulkkot-fix2-${new Date().toISOString()}`;

function artifactHash() {
  const hash = crypto.createHash('sha256');
  const game = path.join(PUBLIC, 'g', 'bulkkot-jeomgeom');
  for (const file of ['index.html', ...fs.readdirSync(path.join(game, 'Build')).sort().map(f => `Build/${f}`)]) {
    hash.update(file); hash.update(fs.readFileSync(path.join(game, file)));
  }
  return hash.digest('hex');
}

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
  throw new Error('Chrome for Testing not found');
}

function rng(seed) { let x = seed >>> 0; return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; }; }
const state = page => page.evaluate(() => window.__GAME_TEST__.getState());

async function pt(page, kind, card = 0, ring = 0) {
  const s = await state(page), v = page.viewport();
  const xd = kind === 'fuse' ? s.fusePx[0] : s.cardPx[card * 3];
  const yd = kind === 'fuse' ? s.fusePx[1] : s.cardPx[card * 3 + 1 + ring];
  return { x: xd * v.width / s.screenW, y: v.height - yd * v.height / s.screenH };
}
async function gesture(page, p, dx, dy) {
  await page.mouse.move(p.x, p.y); await page.mouse.down(); await new Promise(r => setTimeout(r, 18));
  await page.mouse.move(p.x + dx, p.y + dy, { steps: 2 }); await new Promise(r => setTimeout(r, 18)); await page.mouse.up(); await new Promise(r => setTimeout(r, 34));
}
async function ring(page, card, band, dir) { await gesture(page, await pt(page, 'ring', card, band), 42 * dir, 0); }
async function submit(page) { await gesture(page, await pt(page, 'fuse'), 0, 78); }

function interval(k, n) {
  const z = 1.96, p = k / n, d = 1 + z * z / n, c = (p + z * z / (2 * n)) / d;
  const h = z * Math.sqrt((p * (1 - p) + z * z / (4 * n)) / n) / d; return [Math.max(0, c - h), Math.min(1, c + h)];
}

function decode(id) {
  const p = id.split(':');
  return p[0] === 'P' ? { product: true, a: +p[1], b: +p[2] } : { product: false, a: p[0] === 'A' ? 0 : 1, b: +p[1] };
}
function encode(v) { return v.product ? `P:${v.a}:${v.b}` : `${v.a === 0 ? 'A' : 'B'}:${v.b}`; }
function valuesFor(s) {
  const decoded = s.canonicalCaseIds.map(decode);
  return {
    a: [...new Set(decoded.map(v => v.a))],
    bySource: [
      [...new Set(decoded.filter(v => !v.product && v.a === 0).map(v => v.b))],
      [...new Set(decoded.filter(v => !v.product && v.a === 1).map(v => v.b))]
    ],
    productA: [...new Set(decoded.filter(v => v.product).map(v => v.a))],
    productB: [...new Set(decoded.filter(v => v.product).map(v => v.b))]
  };
}
function cycle(values, current, dir) {
  let index = values.indexOf(current); if (index < 0) index = 0;
  return values[(index + (dir >= 0 ? 1 : values.length - 1)) % values.length];
}
function applyAction(s, id, band, dir) {
  const v = decode(id), values = valuesFor(s);
  if (v.product) {
    if (band === 0) v.a = cycle(values.productA, v.a, dir);
    else v.b = cycle(values.productB, v.b, dir);
  } else if (band === 0) {
    v.a = v.a === 0 ? 1 : 0;
    const items = values.bySource[v.a]; v.b = items[Math.abs(v.b) % items.length];
  } else {
    v.b = cycle(values.bySource[v.a], v.b, dir);
  }
  return encode(v);
}
function validAfter(s, card, band, dir) {
  const roster = [...s.currentCaseIds]; roster[card] = applyAction(s, roster[card], band, dir);
  if (new Set(roster).size !== roster.length) return false;
  const wanted = new Set(s.canonicalCaseIds); return roster.every(id => wanted.has(id));
}
function randomBlindChance(s) {
  let wins = 0, total = 0;
  for (let card = 0; card < s.currentCaseIds.length; card++) for (let band = 0; band < 2; band++) for (const dir of [-1, 1]) {
    total++; if (validAfter(s, card, band, dir)) wins++;
  }
  return wins / total;
}
function fixedBlindChance(s, band, dir) {
  const counts = new Map(); for (const id of s.currentCaseIds) counts.set(id, (counts.get(id) || 0) + 1);
  const duplicate = [...counts].find(([, n]) => n > 1)?.[0];
  const missing = s.canonicalCaseIds.find(id => !counts.has(id));
  if (!duplicate || !missing || applyAction(s, duplicate, band, dir) !== missing) return 0;
  return counts.get(duplicate) / s.currentCaseIds.length;
}

async function policyRun(page, name, random) {
  let firstCorrect = 0, completed = 0, chanceSum = 0, submissions = 0;
  for (let i = 0; i < TRIALS; i++) {
    await page.evaluate(() => window.__GAME_TEST__.start()); await new Promise(r => setTimeout(r, 45));
    let s = await state(page);
    if (name !== 'idle') {
      let card = 0, band = 1, dir = 1;
      if (name === 'cycle') { card = i % Math.min(4, s.currentCaseIds.length); band = i % 2; dir = i % 4 < 2 ? 1 : -1; }
      if (name === 'random') { card = Math.floor(random() * s.currentCaseIds.length); band = Math.floor(random() * 2); dir = random() < .5 ? -1 : 1; }
      chanceSum += name === 'random' ? randomBlindChance(s) : fixedBlindChance(s, band, dir);
      await ring(page, card, band, dir); await submit(page); submissions++;
      s = await state(page); if (s.firstCorrect > 0) firstCorrect++; if (s.solved >= 6 || s.phase === 'clear') completed++;
    } else {
      await new Promise(r => setTimeout(r, 12)); s = await state(page); if (s.solved > 0 || s.firstAttempts > 0) completed++;
    }
  }
  const rate = firstCorrect / TRIALS, chance = name === 'idle' ? 0 : chanceSum / TRIALS, ci95 = interval(firstCorrect, TRIALS);
  return { trials: TRIALS, submissions, firstCorrect, firstAttemptRate: rate, chance, ci95, completionRate: completed / TRIALS,
    passed: name === 'idle' ? firstCorrect === 0 && completed === 0 : ci95[0] <= chance };
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
try {
  const page = await browser.newPage(); await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 2 });
  await page.goto(`${server.url}/g/bulkkot-jeomgeom/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 30000 });
  const random = rng(20261020);
  const result = {
    run_id: VALIDATION_RUN_ID,
    artifact_hash: artifactHash(),
    criterion: '각 정책 200판의 첫 도화선 제출 정답률이 정책의 실제 한 칸 전이분포 우연 기준을 95% 수준에서 유의하게 넘지 않고, 무입력은 제출·진도 0',
    method: '매 판 start()로 새 덱만 준비하고, 답 입력은 browser mouse down/move/up으로 고리 1회와 도화선을 실제 조작. random은 카드×고리×방향 전수, 고정 정책은 섞인 카드 위치를 주변화해 기준선 산출',
    spam: await policyRun(page, 'spam', random),
    cycle: await policyRun(page, 'cycle', random),
    random: await policyRun(page, 'random', random),
    idle: await policyRun(page, 'idle', random)
  };
  result.passed = result.spam.passed && result.cycle.passed && result.random.passed && result.idle.passed;
  fs.writeFileSync(path.join(HERE, 'b3-pointer-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2)); if (!result.passed) process.exitCode = 1;
} finally { await browser.close(); await server.close(); }
