// 두 판 연속 실플레이 검증: 타이틀 CTA 탭 → 연습 → 본판 1(첫 오답 1회 포함) → 결과 화면 CTA 탭 → 본판 2.
// 답은 상태 훅에서 읽지 않는다. 화면에 보이는 발문(state.prompt)만 받아 이 파일의 독립 삼각비 풀이기로 길이를 계산하고,
// 제출은 전부 실제 pointer down/move/up 이다. answerCorrect/answerWrong/sampleProblems 는 호출하지 않는다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const OUT = path.join(path.dirname(new URL(import.meta.url).pathname), 'second-play');
fs.mkdirSync(OUT, {recursive: true});
const sleep = ms => new Promise(r => setTimeout(r, ms));

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const b of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
  return undefined;
}

function artifactHash() {
  const dir = path.join(PUBLIC, 'g/cable-lock');
  const files = [];
  (function walk(d) {
    for (const f of fs.readdirSync(d).sort()) {
      const p = path.join(d, f);
      if (fs.statSync(p).isDirectory()) walk(p); else files.push(p);
    }
  })(dir);
  const h = crypto.createHash('sha256');
  for (const f of files) { h.update(path.relative(dir, f)); h.update(fs.readFileSync(f)); }
  return h.digest('hex');
}

// 발문 → 길이. 각·주어진 변 역할·구하는 변 역할만 읽어 삼각비로 계산한다.
function solve(prompt) {
  const ang = Number((prompt.match(/∠A=(\d+)°/) || prompt.match(/올려본각이 (\d+)°/))[1]);
  const val = s => { const m = s.match(/(\d*)√(\d+)/); return m ? (m[1] ? Number(m[1]) : 1) * Math.sqrt(Number(m[2])) : Number(s); };
  const t = Math.tan(ang * Math.PI / 180), sn = Math.sin(ang * Math.PI / 180), cs = Math.cos(ang * Math.PI / 180);
  let m;
  if ((m = prompt.match(/(이웃변|빗변|대변) [A-C]{2}=([\d√]+) m/))) {
    const given = m[1], v = val(m[2]);
    const asked = prompt.match(/(대변|이웃변) 케이블/)[1];
    let r;
    if (given === '이웃변' && asked === '대변') r = v * t;
    else if (given === '대변' && asked === '이웃변') r = v / t;
    else if (given === '빗변' && asked === '대변') r = v * sn;
    else if (given === '빗변' && asked === '이웃변') r = v * cs;
    return Math.round(r);
  }
  if ((m = prompt.match(/수평으로 ([\d√]+) m/))) return Math.round(val(m[1]) * t);
  if ((m = prompt.match(/높이가 ([\d√]+) m/))) return Math.round(val(m[1]) / t);
  throw new Error('unparsed prompt: ' + prompt);
}

function yFor(length) { return 731 - (length - 1) * (300 / 47); }
async function dragLength(page, from, to) {
  await page.mouse.move(195, yFor(to));
  await page.mouse.down();
  await page.mouse.move(195, yFor(to) + (to === 1 ? -4 : 4), {steps: 4});
  await sleep(60);
  await page.mouse.up();
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({headless: true, executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']});
const page = await browser.newPage();
await page.setViewport({width: 390, height: 844, deviceScaleFactor: 1});
const errors = [];
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
page.on('pageerror', e => errors.push(String(e)));
await page.goto(`${server.url}/g/cable-lock/`, {waitUntil: 'load', timeout: 45000});
await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
const state = () => page.evaluate(() => window.__GAME_TEST__.getState());
const log = [];
let shot = 0;
const snap = async tag => { await page.screenshot({path: path.join(OUT, `${String(shot++).padStart(2, '0')}-${tag}.png`)}); };

async function playRun(runNo, injectWrong) {
  let wrongDone = false;
  for (let guard = 0; guard < 20; guard++) {
    const s = await state();
    if (s.phase !== 'playing') return s;
    const ans = solve(s.prompt);
    let target = ans;
    if (injectWrong && !wrongDone && s.attemptIndex === 0) { target = ans === 48 ? 47 : ans + 1; wrongDone = true; }
    await dragLength(page, s.selectedLength, target);
    await sleep(150);
    const after = await state();
    log.push({run: runNo, prompt: s.prompt, solverAnswer: ans, dragged: target, before: {solved: s.solved, lives: s.lives, first: s.firstAttemptCorrect},
      after: {solved: after.solved, lives: after.lives, first: after.firstAttemptCorrect, misconception: after.misconceptionId}});
    if (target !== ans) await snap(`run${runNo}-wrong`);
    await sleep(1700);
  }
  return state();
}

// 첫 판: 타이틀 CTA → 연습
await snap('title');
await page.mouse.click(195, 775);
await sleep(600);
const practice = await state();
await snap('practice');
await dragLength(page, practice.selectedLength, 6);
await sleep(2000);
const run1 = await playRun(1, true);
await sleep(600);
await snap('run1-end');
// 자발적 두 번째 판: 결과 화면 CTA 탭(실제 pointer)
await page.mouse.click(195, Math.round(844 * 0.80));
await sleep(900);
let s2 = await state();
if (s2.onboarding) { await dragLength(page, s2.selectedLength, 6); await sleep(2000); }
const run2 = await playRun(2, false);
await sleep(600);
await snap('run2-end');

const result = {
  run_id: `cable-lock-second-play-${new Date().toISOString()}`,
  artifact_hash_sha256: artifactHash(),
  method: 'real pointer only; answers from independent JS trig solver over the displayed prompt text',
  practiceOnboarding: practice.onboarding,
  run1: {phase: run1.phase, solved: run1.solved, lives: run1.lives, firstAttemptCorrect: run1.firstAttemptCorrect, score: run1.score},
  run2: {phase: run2.phase, solved: run2.solved, lives: run2.lives, firstAttemptCorrect: run2.firstAttemptCorrect, score: run2.score},
  solverMismatch: log.filter(l => l.dragged === l.solverAnswer && l.after.solved !== l.before.solved + 1).length,
  log, errors,
};
result.passed = result.practiceOnboarding === true && run1.phase === 'clear' && run2.phase === 'clear'
  && run1.firstAttemptCorrect === 5 && run2.firstAttemptCorrect === 6 && result.solverMismatch === 0 && errors.length === 0;
fs.writeFileSync(path.join(path.dirname(OUT), 'second-play-results.json'), JSON.stringify(result, null, 2));
console.log(JSON.stringify({...result, log: `${log.length} entries`}, null, 2));
await browser.close();
await server.close();
if (!result.passed) process.exitCode = 1;
