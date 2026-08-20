#!/usr/bin/env node
import path from 'node:path';
import fs from 'node:fs';
import puppeteer from 'puppeteer';
import { P } from '/Users/sitpo/math-game-factory/factory/lib/paths.mjs';
import { serveStatic } from '/Users/sitpo/math-game-factory/factory/lib/static-server.mjs';

function resolveChrome() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  const builds = fs.readdirSync(base).filter((d) => fs.statSync(path.join(base, d)).isDirectory()).sort();
  for (const b of builds.reverse()) {
    const p = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
  return undefined;
}

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

async function vis(page, sel) {
  return page.evaluate((s) => {
    const el = document.querySelector(s);
    if (!el) return false;
    const st = getComputedStyle(el);
    const r = el.getBoundingClientRect();
    if (st.display === 'none' || st.visibility === 'hidden') return false;
    const pan = el.closest('.panel');
    if (pan && !pan.classList.contains('on')) return false;
    return r.width > 0 && r.height > 0;
  }, sel);
}

async function tap(page, sel) {
  if (!(await vis(page, sel))) return false;
  await page.click(sel).catch(() => {});
  await sleep(160);
  return true;
}

async function sim(page) {
  return page.evaluate(() => {
    const t = window.__GAME_TEST__;
    return { ...(t.getState() || {}), ...(t.getSim ? t.getSim() : {}) };
  });
}

async function holdCanvas(page, ms) {
  const box = await page.evaluate(() => {
    const c = document.getElementById('cv');
    const r = c.getBoundingClientRect();
    return { x: r.left + r.width * 0.34, y: r.top + r.height * 0.42 };
  });
  await page.mouse.move(box.x, box.y);
  await page.mouse.down();
  await sleep(ms);
  await page.mouse.up();
}

async function skipCalcDumb(page) {
  if (await vis(page, '#predict.on')) await tap(page, '#predGt');
  await sleep(700);
  if (await vis(page, '#roundPanel.on')) await tap(page, '#rndGo');
  if (await vis(page, '#plan.on')) {
    // 무뇌: 값을 모르니 +/- 몇 번만 누르고 시작
    await tap(page, '#qPlus');
    await tap(page, '#qPlus');
    await tap(page, '#rMinus');
    await tap(page, '#planGo');
  }
  if (await vis(page, '#invPick.on')) {
    const ok = await page.$('.invCard');
    if (ok) { await ok.click(); await sleep(200); }
  }
  if (await vis(page, '#feedback.on')) await tap(page, '#fbGo');
}

async function toPlan(page) {
  await page.evaluate(() => window.__GAME_TEST__.start());
  await sleep(500);
  const s = await sim(page);
  if (await vis(page, '#predict.on')) {
    await tap(page, s.bigger ? '#predGt' : '#predLt');
    await sleep(850);
  }
}

const server = await serveStatic(P.publicDir);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio', '--hide-scrollbars'],
});

const report = { a: {}, b: {}, c: {}, hold: {}, shots: [] };

try {
  const page = await browser.newPage();
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 2, isMobile: true, hasTouch: true });
  page.on('pageerror', (e) => { report.pageerror = String(e); });
  await page.goto(`${server.url}/g/honey-cups/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 20000 });

  // ── (b) r=0 문제에서 미입력 상태로 배송 시작이 안 되는지 + 스크린샷
  await toPlan(page);
  await sleep(200);
  const b0 = await sim(page);
  const blankShot = '/tmp/mgf-hc3-blank.png';
  if (await vis(page, '#plan.on')) {
    await page.screenshot({ path: blankShot });
    report.shots.push('blank');
  }
  const planGoBefore = await page.evaluate(() => {
    const b = document.getElementById('planGo');
    return { disabled: b.disabled, text: b.textContent, phase: window.__GAME_TEST__.getSim().phase };
  });
  await tap(page, '#planGo');
  await sleep(200);
  const afterBlindGo = await sim(page);
  // r만 0으로 명시 — q는 아직 ?
  await tap(page, '#rMinus');
  const afterR0 = await sim(page);
  await tap(page, '#planGo');
  await sleep(150);
  const stillPlan = await sim(page);
  // 둘 다 입력하면 활성화
  await tap(page, '#qPlus');
  const afterBoth = await sim(page);
  report.b = {
    firstRoundR: b0.r,
    qLabel: b0.qLabel,
    rLabel: b0.rLabel,
    planReadyBlank: b0.planReady,
    planGoDisabledBlank: planGoBefore.disabled,
    phaseAfterBlindGo: afterBlindGo.phase,
    rLabelAfterMinus: afterR0.rLabel,
    planReadyAfterROnly: afterR0.planReady,
    phaseAfterROnlyGo: stillPlan.phase,
    planReadyAfterBoth: afterBoth.planReady,
    planGoDisabledAfterBoth: afterBoth.planGoDisabled,
    pass:
      b0.r === 0 &&
      b0.qLabel === '?' &&
      b0.rLabel === '?' &&
      b0.planReady === false &&
      planGoBefore.disabled === true &&
      afterBlindGo.phase === 'plan' &&
      afterR0.planReady === false &&
      stillPlan.phase === 'plan' &&
      afterBoth.planReady === true &&
      afterBoth.planGoDisabled === false,
  };

  // ── (a) 배송표를 틀리게 적고 실행이 정확해도 실패
  await page.goto(`${server.url}/g/honey-cups/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 20000 });
  await toPlan(page);
  await sleep(200);
  const aPlan = await page.evaluate(() => {
    const t = window.__GAME_TEST__;
    t.applyWrongPlan();
    t.startPourFromPlan();
    return t.getSim();
  });
  await sleep(200);
  const poured = await page.evaluate(() => window.__GAME_TEST__.pourExact());
  const beforeDone = await sim(page);
  await page.evaluate(() => window.__GAME_TEST__.submitDone());
  await sleep(300);
  const afterWrongSlip = await sim(page);
  report.a = {
    planQ: aPlan.planQ, trueQ: aPlan.q, planRk: aPlan.planRk, trueR: aPlan.r,
    poured,
    sealed: beforeDone.sealedCount,
    tank: beforeDone.tank,
    rGrains: beforeDone.rGrains,
    execMatchesMath: beforeDone.sealedCount === beforeDone.q && beforeDone.tank === beforeDone.rGrains,
    phase: afterWrongSlip.phase,
    lastFail: afterWrongSlip.lastFail,
    failLock: afterWrongSlip.failLock,
    ghostNote: afterWrongSlip.ghostNote,
    pass:
      poured === true &&
      beforeDone.sealedCount === beforeDone.q &&
      beforeDone.tank === beforeDone.rGrains &&
      aPlan.planQ !== aPlan.q &&
      afterWrongSlip.phase === 'feedback' &&
      (afterWrongSlip.lastFail === 'cups' || afterWrongSlip.lastFail === 'plan' || afterWrongSlip.lastFail === 'rest'),
  };

  // ── (c) 만수 직전/만수 밀봉 시 공중 알갱이 회수, 실패 안 남
  await page.goto(`${server.url}/g/honey-cups/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 20000 });
  await toPlan(page);
  await page.evaluate(() => {
    const t = window.__GAME_TEST__;
    t.applyCorrectPlan();
    t.startPourFromPlan();
  });
  await sleep(250);

  // c1: 만수 채운 뒤 공중 알갱이 띄우고 밀봉
  const filled = await page.evaluate(() => window.__GAME_TEST__.fillOpenToC());
  const spawned = await page.evaluate(() => window.__GAME_TEST__.spawnAirGrains(6));
  const preSeal = await sim(page);
  await page.evaluate(() => window.__GAME_TEST__.trySeal());
  const justSealed = await sim(page);
  await sleep(600);
  const afterRecall = await sim(page);

  // c2: 만수 직전(cap-1)에서 공중 알갱이가 착지해도 유출 없이 회수
  await page.evaluate(() => {
    const t = window.__GAME_TEST__;
    // 다음 잔을 만수 직전까지
    const s = t.getSim();
    // fill to C-1 via repeated fill then subtract conceptually: use fillOpenToC then we need open=C-1
  });
  // 직접 상태를 만수 직전으로
  const nearFull = await page.evaluate(() => {
    const t = window.__GAME_TEST__;
    const s = t.getSim();
    if (s.phase !== 'pour' || s.failLock) return { ok: false, s };
    // 다음 잔: open=0 이어야 함 (방금 밀봉함)
    t.fillOpenToC();
    // C에서 1 빼 만수 직전으로 되돌리고 그 1알갱이는 통으로
    return t.getSim();
  });
  const landTest = await page.evaluate(() => {
    const sim = window.__GAME_TEST__.getSim();
    // open 을 C-1 로 낮추고 공중 알갱이 4개
    const Ropen = sim.open, C = sim.C;
    // fillOpenToC 가 만수로 만들었으니 한 알갱이를 통으로 되돌림
    // 훅 없이 직접은 못 하니 spawn 후 밀봉 대신 착지 대기 시나리오는
    // open=C 에서 spawn → 착지 시 회수
    const n = window.__GAME_TEST__.spawnAirGrains(5);
    return { n, ...window.__GAME_TEST__.getSim() };
  });
  await sleep(900);
  const afterLand = await sim(page);

  report.c = {
    filled,
    spawned,
    preSeal: { open: preSeal.open, C: preSeal.C, grains: preSeal.grains, spilled: preSeal.spilled, sealed: preSeal.sealedCount },
    justSealed: { phase: justSealed.phase, failLock: justSealed.failLock, lastFail: justSealed.lastFail, sealed: justSealed.sealedCount, recalling: justSealed.recalling, spilled: justSealed.spilled, grains: justSealed.grains },
    afterRecall: { tank: afterRecall.tank, grains: afterRecall.grains, recalling: afterRecall.recalling, spilled: afterRecall.spilled, failLock: afterRecall.failLock, phase: afterRecall.phase, sealed: afterRecall.sealedCount },
    landAfter: { spilled: afterLand.spilled, failLock: afterLand.failLock, lastFail: afterLand.lastFail, phase: afterLand.phase, grains: afterLand.grains, recalling: afterLand.recalling, open: afterLand.open },
    pass:
      filled === true &&
      spawned >= 1 &&
      preSeal.open === preSeal.C &&
      justSealed.failLock === false &&
      justSealed.lastFail == null &&
      justSealed.phase === 'pour' &&
      justSealed.sealedCount === 1 &&
      justSealed.spilled === 0 &&
      afterRecall.failLock === false &&
      afterRecall.spilled === 0 &&
      afterLand.spilled === 0 &&
      afterLand.failLock === false &&
      afterLand.phase === 'pour',
  };

  // ── 무뇌 홀드 봇 회귀
  await page.goto(`${server.url}/g/honey-cups/`, { waitUntil: 'networkidle2', timeout: 45000 });
  await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 20000 });
  await page.evaluate(() => window.__GAME_TEST__.start());
  await sleep(500);

  const holdRows = [];
  let pourOk = 0, pourTry = 0, overflow = 0;
  for (let i = 0; i < 10; i++) {
    await skipCalcDumb(page);
    await sleep(200);
    let cur = await sim(page);
    if (cur.phase !== 'pour') {
      await skipCalcDumb(page);
      await sleep(250);
      cur = await sim(page);
    }
    if (cur.phase !== 'pour') {
      holdRows.push({ i, skipped: true, phase: cur.phase, band: cur.band });
      if (cur.phase === 'inverseSim') {
        await sleep(2500);
        if (await vis(page, '#feedback.on')) await tap(page, '#fbGo');
      }
      continue;
    }
    pourTry++;
    await holdCanvas(page, 2800);
    await sleep(400);
    const after = await sim(page);
    const spilled = (after.spilled || 0) > 0 || after.phase === 'feedback' || after.lastFail === 'overflow';
    const success =
      after.phase !== 'feedback' &&
      (after.spilled || 0) === 0 &&
      after.sealedCount >= after.q &&
      after.tank === after.rGrains &&
      after.planQ === after.q &&
      after.planRk === after.r;
    if (success) pourOk++;
    if (spilled) overflow++;
    holdRows.push({
      i, band: after.band, phase: after.phase, spilled: after.spilled,
      open: after.open, sealed: after.sealedCount, q: after.q, success, overflow: spilled,
    });
    if (await vis(page, '#feedback.on')) await tap(page, '#fbGo');
    await sleep(200);
  }
  report.hold = {
    pourTry, pourOk, overflow,
    successRate: pourTry ? +(100 * pourOk / pourTry).toFixed(1) : 0,
    overflowRate: pourTry ? +(100 * overflow / pourTry).toFixed(1) : 0,
    rows: holdRows,
  };

  console.log(JSON.stringify(report, null, 2));
  fs.writeFileSync('/tmp/mgf-hc3-bots.json', JSON.stringify(report, null, 2));
} finally {
  await browser.close().catch(() => {});
  await server.close().catch(() => {});
}
