#!/usr/bin/env node
// 실제 pointer 전체 경로: 타이틀→고정 연습→8/8, 결과 1탭 재시작,
// 둘째 판 첫 오답→같은 새 수리→첫 시도 기록 보존→7개 추가 정답→성공.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';
import { acquireMachineLock } from '../../../../lib/machine-lock.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const OUT = path.join(HERE, 'pointer-playthrough');
fs.mkdirSync(OUT, { recursive: true });
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const b of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
}

const state = page => page.evaluate(() => window.__GAME_TEST__.getState());

function rotationOf(s) {
  const m = /-r(\d+)$/.exec(s.problemId || '');
  return m ? Number(m[1]) : 0;
}

function qIndexOf(s) {
  if (s.problemId === 'practice') return 25;
  const m = /-q(\d+)-/.exec(s.problemId || '');
  if (!m) throw new Error('qIndex를 읽을 수 없음: ' + JSON.stringify(s));
  return Number(m[1]);
}

function screenPoint(s, index) {
  const dx = s.handlePxX - s.pivotPxX, dy = s.handlePxY - s.pivotPxY;
  const delta = (20 + index * 2 - s.inputAngle) * Math.PI / 180;
  const c = Math.cos(delta), sn = Math.sin(delta);
  return [s.pivotPxX + c * dx + sn * dy, s.pivotPxY - sn * dx + c * dy];
}

async function waitStable(page) {
  await page.waitForFunction(() => {
    const s = window.__GAME_TEST__.getState();
    if (!(s.phase === 'playing' && (s.subphase === 'playing' || s.subphase === 'practice' || s.subphase === 'repair') && s.handlePxX > 1)) return false;
    const m = /-r(\d+)$/.exec(s.problemId || '');
    const rotation = m ? Number(m[1]) : 0;
    const dx = s.handlePxX - s.pivotPxX, dy = s.handlePxY - s.pivotPxY;
    let seen = Math.atan2(-dy, dx) * 180 / Math.PI; if (seen < 0) seen += 360;
    const expected = (s.inputAngle + rotation * 90) % 360;
    return Math.abs(((seen - expected + 540) % 360) - 180) < 3;
  }, { timeout: 5000 });
  await sleep(70);
  return state(page);
}

async function releaseAt(page, before, index) {
  let grabbed = null;
  // 새 문항 직후 렌더 한 프레임과 브리지 상태 알림이 엇갈릴 수 있으므로, 실제 dragging 상태까지 확인한다.
  for (let attempt = 0; attempt < 3; attempt++) {
    const current = await waitStable(page);
    await page.mouse.move(current.handlePxX, current.handlePxY);
    await page.mouse.down();
    await sleep(80);
    const afterDown = await state(page);
    if (afterDown.dragging) { grabbed = afterDown; break; }
    await page.mouse.up();
    await sleep(120);
  }
  if (!grabbed) throw new Error(`손잡이 잡기 실패: ${JSON.stringify(await state(page))}`);

  // 정답이 시작점과 가까워도 계약의 12px 경로를 실제로 만든다: 반대 원호까지 갔다가 정답으로 돌아온다.
  const detourIndex = (grabbed.inputIndex + 24) % 61;
  const detour = screenPoint(grabbed, detourIndex);
  await page.mouse.move(detour[0], detour[1], { steps: 5 }); await sleep(45);
  // 원근 투영 때문에 계산한 화면점이 드물게 이웃한 2° 눈금에 닿는다. 손을 놓기 전에
  // 실제 상태를 읽고 현재 손잡이 위치에서 남은 각도만큼 다시 움직여 보정한다.
  for (let attempt = 0; attempt < 5; attempt++) {
    const current = await state(page);
    if (current.inputIndex === index) break;
    const to = screenPoint(current, index);
    await page.mouse.move(to[0], to[1], { steps: 3 });
    await sleep(55);
  }
  const aimed = await state(page);
  if (aimed.inputIndex !== index) {
    await page.mouse.up();
    throw new Error(`눈금 조준 실패 target=${index}: ${JSON.stringify(aimed)}`);
  }
  await page.mouse.up();
  await page.waitForFunction(({ total, solved, practice }) => {
    const s = window.__GAME_TEST__.getState();
    return practice ? s.subphase === 'launch' : (s.firstAttemptTotal > total || s.solved > solved || s.subphase === 'repair_reveal');
  }, { timeout: 3000 }, { total: before.firstAttemptTotal, solved: before.solved, practice: before.problemId === 'practice' });
  return state(page);
}

async function solveCurrent(page, correct = true) {
  const s = await waitStable(page);
  let target = qIndexOf(s);
  if (!correct) target = (target + 17) % 61;
  return releaseAt(page, s, target);
}

async function waitNext(page, solvedBefore) {
  await page.waitForFunction(n => {
    const s = window.__GAME_TEST__.getState();
    return s.phase !== 'playing' || ((s.subphase === 'playing' || s.subphase === 'repair') && s.solved >= n);
  }, { timeout: 6000 }, solvedBefore);
  return state(page);
}

const unlock = await acquireMachineLock('gpu', { label: 'jongisae-chulgyeok 실제 포인터 완주' });
const server = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({
  headless: true, executablePath: chromePath(), protocolTimeout: 120000,
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio', '--enable-unsafe-swiftshader', '--use-gl=angle', '--use-angle=swiftshader']
});
const result = { generatedAt: new Date().toISOString(), errors: [], clean: {}, recovery: {} };

try {
  const page = await browser.newPage();
  page.on('console', m => { if (m.type() === 'error') result.errors.push(m.text()); });
  page.on('pageerror', e => result.errors.push(String(e)));
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
  await page.goto(`${server.url}/g/jongisae-chulgyeok/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  await page.screenshot({ path: path.join(OUT, '00-title.png') });

  await page.mouse.click(195, 430);
  await page.waitForFunction(() => window.__GAME_TEST__.getState().subphase === 'practice', { timeout: 3000 });
  await page.screenshot({ path: path.join(OUT, '01-practice.png') });
  await solveCurrent(page, true);
  await page.screenshot({ path: path.join(OUT, '02-practice-equation.png') });
  await page.waitForFunction(() => window.__GAME_TEST__.getState().subphase === 'playing', { timeout: 5000 });

  for (let i = 0; i < 8; i++) {
    const before = await waitStable(page);
    const after = await releaseAt(page, before, qIndexOf(before));
    if (i === 0 || i === 5 || i === 7) await page.screenshot({ path: path.join(OUT, `clean-${i + 1}.png`) });
    if (after.solved !== i + 1) throw new Error(`clean ${i + 1} 판정 실패: ${JSON.stringify(after)}`);
    if (i < 7) await waitNext(page, i + 1);
  }
  await page.waitForFunction(() => window.__GAME_TEST__.getState().phase === 'clear', { timeout: 5000 });
  result.clean = await state(page);
  await page.screenshot({ path: path.join(OUT, 'clean-90-result.png') });

  // 결과 세계 안 접힌 새의 몸통 중앙(390×844 세로 화면).
  await page.mouse.click(195, 535);
  await page.waitForFunction(() => window.__GAME_TEST__.getState().subphase === 'practice', { timeout: 3000 });
  result.restartTap = true;
  await page.evaluate(() => window.__GAME_TEST__.start()); // 둘째 판은 고정 연습만 건너뛴다.
  await waitStable(page);

  const first = await waitStable(page);
  const wrong = await releaseAt(page, first, (qIndexOf(first) + 17) % 61);
  if (wrong.lives !== 2 || wrong.firstAttemptTotal !== 1 || wrong.firstAttemptCorrect !== 0 || !wrong.repairPending)
    throw new Error('첫 오답 기록 실패: ' + JSON.stringify(wrong));
  await page.screenshot({ path: path.join(OUT, 'recovery-01-wrong.png') });
  await waitNext(page, 0);
  const repair = await waitStable(page);
  const repaired = await releaseAt(page, repair, qIndexOf(repair));
  if (repaired.solved !== 1 || repaired.firstAttemptCorrect !== 0)
    throw new Error('수리 기록 보존 실패: ' + JSON.stringify(repaired));
  await page.screenshot({ path: path.join(OUT, 'recovery-02-repaired.png') });
  await waitNext(page, 1);

  for (let i = 1; i < 8; i++) {
    const before = await waitStable(page);
    const after = await releaseAt(page, before, qIndexOf(before));
    if (after.solved !== i + 1) throw new Error(`recovery ${i + 1} 판정 실패: ${JSON.stringify(after)}`);
    if (i < 7) await waitNext(page, i + 1);
  }
  await page.waitForFunction(() => window.__GAME_TEST__.getState().phase === 'clear', { timeout: 5000 });
  result.recovery = await state(page);
  await page.screenshot({ path: path.join(OUT, 'recovery-90-result.png') });
  result.pass = result.errors.length === 0
    && result.clean.phase === 'clear' && result.clean.solved === 8 && result.clean.firstAttemptCorrect === 8
    && result.recovery.phase === 'clear' && result.recovery.solved === 8
    && result.recovery.firstAttemptCorrect === 7 && result.recovery.lives === 2;
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), JSON.stringify(result, null, 2) + '\n');
  console.log(JSON.stringify(result, null, 2));
  if (!result.pass) process.exitCode = 1;
} finally {
  await browser.close();
  server.close();
  await unlock();
}
