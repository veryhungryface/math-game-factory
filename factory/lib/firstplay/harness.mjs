#!/usr/bin/env node
/**
 * 첫 플레이 이해도 감사 — 「처음 보는 학생」 녹화 하네스.
 *
 *   node factory/lib/firstplay/harness.mjs <slug> [slug...]
 *   FIRSTPLAY_OUT=/some/dir node factory/lib/firstplay/harness.mjs <slug>
 *   FIRSTPLAY_PLAY_MS=45000 node factory/lib/firstplay/harness.mjs <slug>   # 짧은 스모크용
 *
 * 하는 일: public/g/<slug>/ 를 390×844 모바일 뷰포트로 띄우고, **정답을 전혀 모르는
 * 순진한 학생**처럼 "화면에서 눈에 띄는 것"만 무작정 누르고 끌면서 90초를 보낸다.
 * 매 행동 직후 스크린샷을 찍어 <out>/<slug>/frame-NN-tSSS-<phase>.png 로 저장하고,
 * manifest.json 에 프레임 목록·콘솔 에러를 남긴다.
 *
 * ⚠️ `window.__GAME_TEST__` 훅은 **로드 완료(ready) 감지에만** 쓴다. 정답이나 게임
 * 상태를 들여다보거나 조작하는 데는 절대 쓰지 않는다 — 순진한 학생 시뮬레이션이
 * 이 도구의 전부다. 훅을 쓰면 "설명 없이도 이해되는가"라는 질문 자체가 무너진다.
 *
 * 출력 기본 위치는 repo-root `scratchpad/firstplay/`(gitignored)다. 캠페인성 전수 조사는
 * 이 기본값을 쓴다 — **`factory/work/` 를 영속 보관소로 쓰지 마라**(매 사이클 rm -rf 된다).
 * 다만 생산 사이클 안에서는 run.sh 가 `FIRSTPLAY_OUT=factory/work/qa/<slug>/firstplay` 로
 * 불러서 그 회차 검수관에게 프레임을 준다(P0-2). 그 프레임은 `logs/<RUN_ID>/firstplay-N/`
 * 으로도 복사되므로 영속 증거는 로그 쪽에 남는다.
 *
 * 다음 단계: `judge.mjs <slug>` 로 codex 판정 → `aggregate.mjs` 로 SUMMARY.json.
 * 규격은 `docs/onboarding-spec.md`.
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../..');
const PUBLIC_DIR = path.join(ROOT, 'public');
export const DEFAULT_OUT_ROOT = path.join(ROOT, 'scratchpad/firstplay');
const OUT_ROOT = process.env.FIRSTPLAY_OUT || DEFAULT_OUT_ROOT;
const PLAY_MS = Number(process.env.FIRSTPLAY_PLAY_MS || 90_000);

const slugs = process.argv.slice(2);
if (!slugs.length) {
  console.error('usage: node factory/lib/firstplay/harness.mjs <slug> [slug...]');
  process.exit(1);
}

console.error(`[cfg] ROOT=${ROOT}`);
console.error(`[cfg] PUBLIC_DIR=${PUBLIC_DIR} exists=${fs.existsSync(PUBLIC_DIR)}`);
console.error(`[cfg] OUT_ROOT=${OUT_ROOT} PLAY_MS=${PLAY_MS}`);

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const rand = (a, b) => a + Math.random() * (b - a);
const pick = (arr) => arr[Math.floor(Math.random() * arr.length)];

/** qa.mjs 와 같은 규칙으로 로컬 Chrome for Testing 을 찾는다. */
function resolveChrome() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  const builds = fs.readdirSync(base).filter((d) => fs.statSync(path.join(base, d)).isDirectory()).sort();
  for (const b of builds.reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome',
    ]) {
      const p = path.join(base, b, rel);
      if (fs.existsSync(p)) return p;
    }
  }
  return undefined;
}

/** 화면에서 "학생 눈에 띌 만한" 조작 후보를 찾는다. 정답 지식 없음 — 순수 DOM 가시성 기준. */
async function scanTargets(page) {
  return page.evaluate(() => {
    function visible(el) {
      const r = el.getBoundingClientRect();
      if (r.width < 15 || r.height < 15) return false;
      if (r.bottom < 0 || r.top > window.innerHeight || r.right < 0 || r.left > window.innerWidth) return false;
      const cs = getComputedStyle(el);
      if (cs.visibility === 'hidden' || cs.display === 'none' || Number(cs.opacity) === 0) return false;
      return true;
    }
    function rectOf(el) {
      const r = el.getBoundingClientRect();
      return { x: r.left, y: r.top, w: r.width, h: r.height, cx: r.left + r.width / 2, cy: r.top + r.height / 2 };
    }
    const overlaySel = '[id*="onboard" i], [class*="onboard" i], [id*="modal" i], [class*="modal" i], [id*="overlay" i], [class*="overlay" i], [id*="tutorial" i], [class*="tutorial" i], [id*="help" i], [class*="help" i]';
    let overlay = null;
    for (const el of document.querySelectorAll(overlaySel)) {
      if (!visible(el)) continue;
      const r = el.getBoundingClientRect();
      const area = Math.max(0, Math.min(r.right, window.innerWidth) - Math.max(r.left, 0)) *
        Math.max(0, Math.min(r.bottom, window.innerHeight) - Math.max(r.top, 0));
      if (area > window.innerWidth * window.innerHeight * 0.25) {
        const btns = Array.from(el.querySelectorAll('button, [role="button"], a, .btn')).filter(visible);
        const dismiss = btns.find((b) => /확인|닫기|시작|다음|알겠|계속|play|start|got it|ok/i.test(b.textContent || '')) || btns[btns.length - 1] || el;
        overlay = rectOf(dismiss);
        break;
      }
    }
    const startCandidates = Array.from(document.querySelectorAll('button, [role="button"], a, .btn'))
      .filter(visible)
      .filter((el) => /시작|플레이|play|start/i.test(el.id + ' ' + el.className + ' ' + (el.textContent || '')));
    const startBtn = startCandidates[0] ? rectOf(startCandidates[0]) : null;

    const clickable = Array.from(document.querySelectorAll('button, [role="button"], a[href], input[type="button"], input[type="submit"], .btn, .choice, .option, .card, [onclick], label'))
      .filter(visible)
      .map(rectOf);

    const canvas = document.querySelector('canvas');
    const canvasRect = canvas && visible(canvas) ? rectOf(canvas) : null;

    return { overlay, startBtn, clickable, canvasRect, vw: window.innerWidth, vh: window.innerHeight };
  });
}

async function tap(page, x, y) {
  await page.mouse.move(x, y);
  await page.mouse.down();
  await sleep(60);
  await page.mouse.up();
}

async function drag(page, x1, y1, x2, y2) {
  await page.mouse.move(x1, y1);
  await page.mouse.down();
  await page.mouse.move((x1 + x2) / 2, (y1 + y2) / 2, { steps: 6 });
  await page.mouse.move(x2, y2, { steps: 6 });
  await sleep(40);
  await page.mouse.up();
}

/** 순진한 학생의 다음 한 수: 오버레이는 닫고, 시작 버튼을 누르고, 그다음엔 눈에 띄는 걸 찍어 본다. */
async function naiveStep(page, startedRef) {
  const t = await scanTargets(page);

  if (t.overlay) {
    await tap(page, t.overlay.cx, t.overlay.cy);
    return 'dismiss-overlay';
  }
  if (!startedRef.started) {
    if (t.startBtn) {
      await tap(page, t.startBtn.cx, t.startBtn.cy);
      startedRef.started = true;
      return 'tap-start';
    }
    await tap(page, t.vw / 2, t.vh / 2);
    startedRef.started = true;
    return 'tap-center-as-start';
  }

  const roll = Math.random();
  if (roll < 0.25 && t.clickable.length) {
    const c = pick(t.clickable);
    await tap(page, c.cx, c.cy);
    return 'tap-dom-element';
  }
  if (roll < 0.45 && t.canvasRect) {
    const r = t.canvasRect;
    const x1 = rand(r.x + r.w * 0.2, r.x + r.w * 0.8);
    const y1 = rand(r.y + r.h * 0.2, r.y + r.h * 0.8);
    const x2 = rand(r.x + r.w * 0.2, r.x + r.w * 0.8);
    const y2 = rand(r.y + r.h * 0.2, r.y + r.h * 0.8);
    await drag(page, x1, y1, x2, y2);
    return 'drag-canvas';
  }
  const area = t.canvasRect || { x: 0, y: t.vh * 0.15, w: t.vw, h: t.vh * 0.7 };
  const x = rand(area.x + area.w * 0.15, area.x + area.w * 0.85);
  const y = rand(area.y + area.h * 0.15, area.y + area.h * 0.85);
  await tap(page, x, y);
  return 'tap-game-area';
}

async function run(slug, browser, serverUrl) {
  const outDir = path.join(OUT_ROOT, slug);
  fs.mkdirSync(outDir, { recursive: true });
  const manifest = [];
  const consoleErrors = [];
  const pageErrors = [];

  const page = await browser.newPage();
  page.on('console', (m) => { if (m.type() === 'error') consoleErrors.push(m.text().slice(0, 300)); });
  page.on('pageerror', (e) => pageErrors.push(String(e).slice(0, 300)));
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });

  const url = `${serverUrl}/g/${slug}/`;
  const t0 = Date.now();
  let loadOk = true;
  let readyOk = false;
  try {
    await page.goto(url, { waitUntil: 'networkidle2', timeout: 45000 });
  } catch (e) {
    loadOk = false;
    consoleErrors.push('GOTO_FAILED: ' + String(e).slice(0, 200));
  }
  if (loadOk) {
    readyOk = await page
      .waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 20000 })
      .then(() => true)
      .catch(() => false);
  }

  async function shot(idx, phase, extra) {
    const elapsed = Math.round((Date.now() - t0) / 1000);
    const fname = `frame-${String(idx).padStart(2, '0')}-t${String(elapsed).padStart(3, '0')}-${phase}.png`;
    const fpath = path.join(outDir, fname);
    try {
      await page.screenshot({ path: fpath });
    } catch (e) {
      consoleErrors.push('SCREENSHOT_FAILED: ' + String(e).slice(0, 200));
    }
    manifest.push({ idx, elapsed_s: elapsed, phase, file: fname, ...extra });
  }

  let idx = 0;
  await shot(idx++, 'title', {});
  await sleep(3000);
  await shot(idx++, 'title', {});

  const startedRef = { started: false };
  const playStart = Date.now();
  while (Date.now() - playStart < PLAY_MS) {
    let action = 'none';
    try {
      action = await naiveStep(page, startedRef);
    } catch (e) {
      action = 'error:' + String(e).slice(0, 100);
    }
    await shot(idx++, startedRef.started ? 'play' : 'onboarding', { action });
    await sleep(rand(2000, 4000));
  }

  fs.writeFileSync(
    path.join(outDir, 'manifest.json'),
    JSON.stringify({ slug, loadOk, readyOk, consoleErrors, pageErrors, frames: manifest }, null, 2)
  );
  await page.close();
  console.log(`[done] ${slug} — ${manifest.length} frames, readyOk=${readyOk}, consoleErrors=${consoleErrors.length}`);
}

const { url: serverUrl, close } = await serveStatic(PUBLIC_DIR);
console.error(`[cfg] server=${serverUrl}`);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: [
    '--no-sandbox',
    '--disable-dev-shm-usage',
    '--mute-audio',
    '--enable-unsafe-swiftshader',
    '--use-gl=angle',
    '--use-angle=swiftshader',
    '--hide-scrollbars',
  ],
});

for (const slug of slugs) {
  try {
    await run(slug, browser, serverUrl);
  } catch (e) {
    console.error(`[FAIL] ${slug}: ${e}`);
  }
}

await browser.close();
await close();
