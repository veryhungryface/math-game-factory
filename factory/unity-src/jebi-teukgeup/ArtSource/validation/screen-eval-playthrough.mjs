#!/usr/bin/env node
// 독립 두 판: macOS Vision OCR로 실제 화면 발문을 읽어 답을 계산하고 실제 mouse drag만 보낸다.
// __GAME_TEST__는 ready와 판 종료 후 감사에만 쓰며 problemId·은행·정답은 읽지 않는다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { launch, startServer, openGame, pull, sleep } from './common.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const GAME = path.join(ROOT, 'public/g/jebi-teukgeup');
const OUT = path.join(HERE, 'screen-eval');
const EYE = process.env.SCREEN_OCR || '/tmp/jebi-screen-ocr';
const W = 390, H = 844;
const RUN_ID = process.env.MGF_RUN_ID || `jebi-fix1-screen-${new Date().toISOString()}`;
fs.rmSync(OUT, { recursive: true, force: true }); fs.mkdirSync(OUT, { recursive: true });
execFileSync('swiftc', [path.join(HERE, 'screen-ocr.swift'), '-O', '-o', EYE], { stdio: 'inherit' });

function hash() {
  const h = crypto.createHash('sha256');
  for (const rel of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(n => `Build/${n}`)]) {
    h.update(rel); h.update(fs.readFileSync(path.join(GAME, rel)));
  }
  return h.digest('hex');
}
const ocr = file => JSON.parse(execFileSync(EYE, [file], { encoding: 'utf8' }));
const tmp = path.join(OUT, '_look.png');
let shot = 0;
async function look(page) {
  await page.screenshot({ path: tmp });
  const words = ocr(tmp); const all = words.map(x => x.text).join(' | ');
  const norm = all.replace(/[\s|,，]/g, '').replace(/[＝]/g, '=');
  return { words, all, norm };
}
async function keep(name) {
  const file = `${String(shot++).padStart(2, '0')}-${name}.png`;
  fs.copyFileSync(tmp, path.join(OUT, file)); return file;
}
function solve(norm) {
  let m = norm.match(/PA[=은이가]*(\d+)cm.*PB.*길이/);
  if (m && !/PA\+PB/.test(norm)) return { kind: 'pair', answer: +m[1], calculation: `PB=PA=${m[1]}` };
  m = norm.match(/PA\+PB[=은이가]*(\d+)cm/);
  if (m) return { kind: 'sum', answer: +m[1] / 2, calculation: `PB=(${m[1]})÷2=${+m[1] / 2}` };
  const ab = norm.match(/AB[=은이가]*(\d+)cm/), ad = norm.match(/AD[=은이가]*(\d+)cm/);
  if (ab && ad) return { kind: 'incircle', answer: +ab[1] - +ad[1], calculation: `BE=BD=AB−AD=${ab[1]}−${ad[1]}=${+ab[1] - +ad[1]}` };
  throw new Error(`화면 발문 파싱 실패: ${norm}`);
}
function startValue(norm) {
  const m = norm.match(/뽑아둔줄[:：]?(\d+)cm/); return m ? +m[1] : 0;
}
async function readProblem(page, waitMs = 9000) {
  const started = Date.now(); let seen;
  while (Date.now() - started < waitMs) {
    seen = await look(page);
    try { return { screen: seen, math: solve(seen.norm), start: startValue(seen.norm) }; } catch {}
    await sleep(220);
  }
  throw new Error(`발문 OCR 대기 실패: ${seen?.norm || ''}`);
}
async function submit(page, from, to, label, inputLog) {
  await pull(page, W, H, from, to, { wiggle: from === to, steps: 12 });
  inputLog.push({ type: 'real_pointer_drag', label, from_cm: from, to_cm: to });
}

const server = await startServer(); const browser = await launch();
const { page, errors } = await openGame(browser, server, W, H, RUN_ID);
const beforeHash = hash(), inputLog = [];
await look(page); const titleShot = await keep('title');
await page.mouse.click(W * 0.5, H * 0.5); await sleep(1100);
let p = await readProblem(page); const practiceShot = await keep('practice-before');
if (p.math.answer !== 7) throw new Error(`연습 답 7 cm 불일치: ${JSON.stringify(p.math)}`);
await submit(page, 0, 12, 'practice-deliberate-wrong', inputLog); await sleep(1800);
await look(page); const practiceWrongShot = await keep('practice-wrong');
await submit(page, 12, 7, 'practice-screen-solved', inputLog); await sleep(4800);

async function playRun(run, deliberateFirstWrong) {
  const rounds = [];
  for (let round = 1; round <= 8; round++) {
    const current = await readProblem(page, 12000); const before = await keep(`run${run}-round${round}-before`);
    const { answer, calculation, kind } = current.math;
    if (!Number.isInteger(answer) || answer < 1 || answer > 24) throw new Error(`화면 계산값 범위 오류: ${JSON.stringify(current.math)}`);
    if (deliberateFirstWrong && round === 1) {
      const wrong = answer === 24 ? 23 : answer + 1;
      await submit(page, current.start, wrong, 'deliberate-first-mistake', inputLog); await sleep(2400);
      await look(page); const wrongShot = await keep('run1-round1-wrong');
      await submit(page, wrong, answer, 'same-problem-screen-math-repair', inputLog);
      rounds.push({ round, kind, visible_text: current.screen.all, calculation, first_submission: wrong, repair_submission: answer, before, wrong_shot: wrongShot });
    } else {
      await submit(page, current.start, answer, 'screen-solved-first-try', inputLog);
      rounds.push({ round, kind, visible_text: current.screen.all, calculation, first_submission: answer, before });
    }
    // WebGL 저사양 모드에서도 피드백+전환을 완전히 끝낸 뒤 다음 화면을 읽는다.
    // 3·5역의 개념 다리는 5.2초, 일반 전환은 3.15초가 정본이므로 여유를 둔다.
    await sleep(round === 3 || round === 5 ? 6800 : round === 8 ? 5000 : 4800);
  }
  await look(page); const endShot = await keep(`run${run}-end`);
  const audit = await page.evaluate(() => {
    const s = window.__GAME_TEST__.getState();
    return { phase: s.phase, result: s.result, firstCorrect: s.firstCorrect, lives: s.lives, solved: s.solved, stationCount: s.stationCount };
  });
  return { run, rounds, end_shot: endShot, audit_after_run_not_used_for_decisions: audit };
}

const run1 = await playRun(1, true);
await page.mouse.click(W * 0.5, H * 0.85); await sleep(1300);
const run2 = await playRun(2, false);
const result = {
  slug: 'jebi-teukgeup', run_id: RUN_ID,
  method: 'screen-only evaluator: macOS Vision OCR reads rendered prompts; evaluator computes tangent lengths; real pointer drags only; __GAME_TEST__ read only after each completed run for audit',
  artifact_hash: beforeHash, artifact_unchanged: beforeHash === hash(), viewport: `${W}x${H}@1`,
  title_shot: titleShot, practice: { before: practiceShot, wrong: practiceWrongShot, wrong_submission: 12, correct_submission: 7 },
  runs: [run1, run2], input_log: inputLog, console_errors: errors
};
result.passed = result.artifact_unchanged && errors.length === 0 &&
  run1.audit_after_run_not_used_for_decisions.phase === 'clear' && run1.audit_after_run_not_used_for_decisions.firstCorrect === 7 && run1.audit_after_run_not_used_for_decisions.lives === 2 &&
  run2.audit_after_run_not_used_for_decisions.phase === 'clear' && run2.audit_after_run_not_used_for_decisions.firstCorrect === 8 && run2.audit_after_run_not_used_for_decisions.lives === 3;
fs.rmSync(tmp, { force: true });
fs.writeFileSync(path.join(HERE, 'screen-eval-results.json'), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify({ passed: result.passed, run_id: RUN_ID, hash: beforeHash, run1: run1.audit_after_run_not_used_for_decisions, run2: run2.audit_after_run_not_used_for_decisions, errors }, null, 2));
await page.close(); await browser.close(); server.close?.(); if (!result.passed) process.exitCode = 1;
