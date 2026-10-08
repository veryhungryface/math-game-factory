#!/usr/bin/env node
// 최종 WebGL을 화면으로 읽고 실제 pointer만 보내는 수동 2판 검증 콘솔.
// __GAME_TEST__의 answerCorrect/answerWrong/sampleProblems는 호출하지 않는다.
import fs from 'node:fs';
import path from 'node:path';
import readline from 'node:readline';
import { execFileSync } from 'node:child_process';
import puppeteer from 'puppeteer';

const root = path.resolve(process.cwd());
const out = path.join(root, process.env.SSAK_POINTER_OUT || 'factory/unity-src/ssak-geonjyeo/ArtSource/validation/two-run-pointer');
fs.mkdirSync(out, { recursive: true });

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  const builds = fs.readdirSync(base).sort().reverse();
  for (const build of builds) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    ]) {
      const candidate = path.join(base, build, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
  throw new Error('Chrome for Testing not found');
}

const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--use-angle=metal'],
});
const page = await browser.newPage();
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
await page.goto(process.env.SSAK_URL || 'http://127.0.0.1:8127/g/ssak-geonjyeo/', { waitUntil: 'networkidle2', timeout: 45_000 });
await page.waitForFunction('window.__GAME_TEST__?.ready === true', { timeout: 20_000 });

const events = [];
let shotIndex = 0;
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
let frozen = false;
let stoppedPids = [];
function descendants(rootPid) {
  const rows = execFileSync('/bin/ps', ['-axo', 'pid=,ppid='], { encoding: 'utf8' })
    .trim().split('\n').map((line) => line.trim().split(/\s+/).map(Number));
  const byParent = new Map();
  for (const [pid, ppid] of rows) {
    if (!byParent.has(ppid)) byParent.set(ppid, []);
    byParent.get(ppid).push(pid);
  }
  const found = [];
  const queue = [...(byParent.get(rootPid) || [])];
  while (queue.length) {
    const pid = queue.shift();
    found.push(pid);
    queue.push(...(byParent.get(pid) || []));
  }
  return found;
}
async function freeze() {
  if (frozen) return;
  stoppedPids = descendants(browser.process().pid);
  for (const pid of stoppedPids) {
    try { process.kill(pid, 'SIGSTOP'); } catch {}
  }
  frozen = true;
}
async function resume() {
  if (!frozen) return;
  for (const pid of stoppedPids.slice().reverse()) {
    try { process.kill(pid, 'SIGCONT'); } catch {}
  }
  stoppedPids = [];
  frozen = false;
  await sleep(120);
}

async function state() {
  return page.evaluate(() => {
    const s = window.__GAME_TEST__.getState();
    return {
      phase: s.phase, score: s.score, lives: s.lives, solved: s.solved,
      tide: s.tide, capacity: s.capacity, selectedIds: s.selectedIds,
      attempts: s.attempts, firstAttemptTotal: s.firstAttemptTotal,
      firstAttemptCorrect: s.firstAttemptCorrect, onboarding: s.onboarding,
      currentProblem: s.currentProblem,
    };
  });
}

async function shot(label) {
  const file = `${String(shotIndex++).padStart(2, '0')}-${label}.png`;
  await page.screenshot({ path: path.join(out, file) });
  const s = await state();
  events.push({ type: 'shot', file, state: s });
  console.log(JSON.stringify({ file, state: s }));
  await freeze();
}

async function tap(x, y) {
  await resume();
  await page.mouse.move(x, y);
  await page.mouse.down();
  await sleep(80);
  await page.mouse.up();
  await sleep(420);
  events.push({ type: 'tap', x, y, state: await state() });
  await freeze();
}

async function drag(points) {
  await resume();
  await page.mouse.move(points[0][0], points[0][1]);
  await page.mouse.down();
  for (let i = 1; i < points.length; i++) {
    await page.mouse.move(points[i][0], points[i][1], { steps: 10 });
    await sleep(45);
  }
  await page.mouse.up();
  await sleep(420);
  events.push({ type: 'drag', points, state: await state() });
  await freeze();
}

await shot('title');
console.log('READY: tap x y | drag x1 y1 x2 y2 [x3 y3 ...] | shot label | wait ms | state | quit');

const rl = readline.createInterface({ input: process.stdin, output: process.stdout, terminal: true });
for await (const raw of rl) {
  const parts = raw.trim().split(/\s+/);
  const cmd = parts.shift();
  try {
    if (cmd === 'tap') await tap(Number(parts[0]), Number(parts[1]));
    else if (cmd === 'drag') {
      const nums = parts.map(Number);
      if (nums.length < 4 || nums.length % 2) throw new Error('drag requires coordinate pairs');
      const points = [];
      for (let i = 0; i < nums.length; i += 2) points.push([nums[i], nums[i + 1]]);
      await drag(points);
    } else if (cmd === 'shot') await shot(parts.join('-') || 'shot');
    else if (cmd === 'wait') { await resume(); await sleep(Number(parts[0] || 1000)); await freeze(); }
    else if (cmd === 'state') console.log(JSON.stringify(await state()));
    else if (cmd === 'quit') break;
    else console.log('unknown command');
  } catch (error) {
    console.error(String(error));
  }
}

fs.writeFileSync(path.join(out, 'events.json'), JSON.stringify({ url: page.url(), events }, null, 2));
await resume();
await browser.close();
