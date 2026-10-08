#!/usr/bin/env node
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const slug = 'mungsil-fashion-show';
const here = path.dirname(fileURLToPath(import.meta.url));
const out = path.join(here, 'pointer-playthrough');
const publicDir = path.resolve(here, '../../../../../public');
const gameDir = path.join(publicDir, 'g', slug);
const resultPath = path.join(here, 'pointer-playthrough-results.json');
fs.mkdirSync(out, { recursive: true });

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  const builds = fs.readdirSync(base).filter(d => fs.statSync(path.join(base, d)).isDirectory()).sort().reverse();
  for (const build of builds) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome'
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
  return undefined;
}

function artifactHash(dir) {
  const files = [];
  function walk(current) {
    for (const name of fs.readdirSync(current).sort()) {
      const full = path.join(current, name);
      const rel = path.relative(dir, full);
      if (fs.statSync(full).isDirectory()) walk(full);
      else files.push(rel);
    }
  }
  walk(dir);
  const hash = crypto.createHash('sha256');
  for (const rel of files) {
    hash.update(rel); hash.update('\0'); hash.update(fs.readFileSync(path.join(dir, rel))); hash.update('\0');
  }
  return { sha256: hash.digest('hex'), fileCount: files.length };
}

const validationRunId = crypto.randomUUID();
const artifact = artifactHash(gameDir);
const { url, close } = await serveStatic(publicDir);
const browser = await puppeteer.launch({ executablePath: resolveChrome(), headless: true, args: ['--no-sandbox', '--disable-setuid-sandbox'] });
const page = await browser.newPage();
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
const errors = [];
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
page.on('pageerror', e => errors.push(String(e)));

const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
const state = () => page.evaluate(() => window.__GAME_TEST__.getState());
const shot = name => page.screenshot({ path: path.join(out, name) });
const xFor = value => 38 + (value - 1) * 314 / 29;

async function waitUntil(label, predicate, timeout = 10000) {
  const deadline = Date.now() + timeout;
  let latest;
  while (Date.now() < deadline) {
    latest = await state();
    if (predicate(latest)) return latest;
    await sleep(100);
  }
  throw new Error(`${label} 대기 시간 초과: ${JSON.stringify(latest)}`);
}

async function dragTo(value) {
  await page.mouse.move(38, 712);
  await page.mouse.down();
  await page.mouse.move(xFor(value), 712, { steps: 14 });
  await page.mouse.up();
}

async function currentAnswer() {
  return page.evaluate(() => {
    const s = window.__GAME_TEST__.getState();
    const p = window.__GAME_TEST__.sampleProblems(10000).find(item => item.id === s.problemId);
    return p ? Number(p.answerNumeric) : null;
  });
}

async function completePractice(prefix) {
  await page.mouse.click(130, 596);
  await waitUntil(`${prefix} 룰렛`, s => s.rouletteSpins >= 1);
  await dragTo(6);
  const ready = await waitUntil(`${prefix} 연습 완료`, s => s.onboarding === false && s.solved === 0 && s.rope === 0, 12000);
  await shot(`${prefix}-practice-complete.png`);
  return ready;
}

async function completeRun(prefix, wrongFirst) {
  const runId = crypto.randomUUID();
  const orders = [];
  let didWrong = false;
  while (true) {
    const before = await state();
    if (before.phase === 'clear') break;
    if (before.solved >= 8) {
      await waitUntil(`${prefix} 클리어`, s => s.phase === 'clear', 12000);
      break;
    }
    const answer = await currentAnswer();
    if (!Number.isInteger(answer)) throw new Error(`${prefix} 문제를 은행에서 찾지 못함: ${JSON.stringify(before)}`);
    const problemId = before.problemId;
    const record = { order: before.solved + 1, problemId, answer, pointerVersionBefore: before.pointerVersion };

    if (wrongFirst && !didWrong) {
      const wrong = answer < 30 ? answer + 1 : answer - 1;
      record.wrong = wrong;
      await dragTo(wrong);
      record.afterWrong = await waitUntil(`${prefix} 오답 판정`, s => s.lives === before.lives - 1 && s.problemId === problemId);
      await shot(`${prefix}-wrong-feedback.png`);
      record.retryReady = await waitUntil(`${prefix} 오답 복구`, s => s.problemId === problemId && s.rope === 0, 12000);
      didWrong = true;
    }

    await dragTo(answer);
    const targetSolved = before.solved + 1;
    record.afterCorrect = await waitUntil(`${prefix} ${targetSolved}번 정답`, s => s.solved >= targetSolved);
    if (targetSolved === 1) await shot(`${prefix}-first-correct.png`);
    if (targetSolved < 8) {
      record.nextReady = await waitUntil(`${prefix} ${targetSolved}번 전환`, s => s.solved === targetSolved && s.rope === 0 && s.problemId !== problemId, 12000);
    } else {
      record.clear = await waitUntil(`${prefix} 최종 클리어`, s => s.phase === 'clear', 12000);
    }
    orders.push(record);
  }
  const finalState = await state();
  await shot(`${prefix}-clear.png`);
  return {
    runId,
    wrongFirst,
    orders,
    finalState,
    pass: finalState.phase === 'clear' && finalState.solved === 8 &&
      finalState.firstAttemptCorrect >= 6 && orders.length === 8
  };
}

let report;
try {
  await page.goto(`${url}/g/${slug}/`, { waitUntil: 'networkidle0', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 20000 });
  await shot('00-title.png');

  await page.mouse.click(195, 422);
  await waitUntil('첫 연습 시작', s => s.onboarding === true);
  await shot('01-practice-guide.png');
  const firstPractice = await completePractice('run1');
  const run1 = await completeRun('run1', true);

  await page.mouse.click(195, 658);
  await waitUntil('두 번째 연습 시작', s => s.onboarding === true);
  const secondPractice = await completePractice('run2');
  const run2 = await completeRun('run2', false);

  report = {
    validationRunId,
    slug,
    artifactHash: artifact.sha256,
    artifactFileCount: artifact.fileCount,
    viewport: { width: 390, height: 844, deviceScaleFactor: 1 },
    inputRoute: 'Puppeteer mouse pointer only; test hooks are read-only for state and problem lookup',
    firstPractice,
    secondPractice,
    runs: [run1, run2],
    consoleErrors: errors,
    pass: run1.pass && run2.pass && errors.length === 0
  };
} catch (error) {
  report = { validationRunId, slug, artifactHash: artifact.sha256, error: String(error?.stack || error), consoleErrors: errors, pass: false };
  process.exitCode = 1;
} finally {
  fs.writeFileSync(resultPath, JSON.stringify(report, null, 2) + '\n');
  const work = process.env.MGF_WORK;
  if (work) {
    const qaDir = path.resolve(work, 'qa', slug);
    fs.mkdirSync(qaDir, { recursive: true });
    fs.writeFileSync(path.join(qaDir, 'pointer-playthrough.json'), JSON.stringify(report, null, 2) + '\n');
  }
  console.log(JSON.stringify(report, null, 2));
  await browser.close();
  await close();
}

if (!report.pass) process.exitCode = 1;
