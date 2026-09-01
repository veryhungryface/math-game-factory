#!/usr/bin/env node
/**
 * 자동 QA 하네스.
 *
 *   node factory/lib/qa.mjs <slug> [--out <dir>]
 *
 * 정적 검사 + 헤드리스 브라우저 플레이 테스트를 돌리고
 *   factory/work/qa/<slug>/report.json
 *   factory/work/qa/<slug>/{mobile,tablet,desktop-1280,desktop,wide}.png
 * 를 남긴다. 치명적 결함이 있으면 exit 1.
 *
 * 이 하네스는 "기계가 확실히 판정할 수 있는 것"만 본다.
 * 재미·비주얼·교육과정 정합성 같은 판단은 40-review.md 검수 에이전트가 맡는다.
 */
import fs from 'node:fs';
import os from 'node:os';
import crypto from 'node:crypto';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { P, readJSON, writeJSON, nowKST } from './paths.mjs';
import { serveStatic } from './static-server.mjs';

/** PNG IHDR 청크에서 width/height 를 읽는다. 외부 라이브러리 없이 표지 이미지 규격을 검증하기 위함. */
function pngSize(file) {
  try {
    const buf = fs.readFileSync(file);
    if (buf.length < 24 || buf.toString('hex', 0, 8) !== '89504e470d0a1a0a') return null;
    return { w: buf.readUInt32BE(16), h: buf.readUInt32BE(20) };
  } catch {
    return null;
  }
}

/** puppeteer 캐시에서 풀 Chrome 실행파일을 찾는다 (WebGL2 지원용). 없으면 번들 기본값. */
function resolveChrome() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  const builds = fs
    .readdirSync(base)
    .filter((d) => fs.statSync(path.join(base, d)).isDirectory())
    .sort();
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

const FPS_GATE = 30;
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
const median = (arr) => {
  const s = [...arr].sort((a, b) => a - b);
  return s.length ? s[Math.floor(s.length / 2)] : 0;
};
/** 코어당 1분 부하. 소프트웨어 렌더러라 머신이 바쁘면 fps 가 통째로 내려앉는다. */
const loadPerCore = () => os.loadavg()[0] / (os.cpus().length || 1);

/**
 * 이 머신이 지금 얼마나 여유가 있는지를 같은 브라우저 안에서 잰다.
 *
 * QA 브라우저는 swiftshader(소프트웨어 렌더러)라서 머신이 바쁘면 게임 fps 가
 * 통째로 내려앉는다. 공장은 에이전트를 병렬로 돌리므로 "한가한 머신"을 기다릴 수
 * 없다. 그래서 고정 비용 캔버스 작업의 fps 를 재서 그 시점의 처리 능력을 추정하고,
 * 게임 fps 를 그 비율로 보정한다. 한가한 머신에서 이 벤치는 vsync 상한(≈60)에
 * 붙는다 — BENCH_IDLE 이 그 기준선이다.
 */
const BENCH_IDLE = 55;
const BENCH_CONTENDED = 48; // 이보다 낮으면 "머신이 바쁘다"고 본다
let _capacityCache = null;
async function machineCapacity(browser) {
  if (_capacityCache) return _capacityCache;
  let bench = BENCH_IDLE;
  let samples = [];
  let bp = null;
  try {
    bp = await browser.newPage();
    await bp.setViewport({ width: 390, height: 844, deviceScaleFactor: 2 });
    await bp.setContent('<canvas id="qabench" width="390" height="844"></canvas>');
    const one = () =>
      bp.evaluate(
        (N) =>
          new Promise((res) => {
            const g = document.getElementById('qabench').getContext('2d');
            let f = 0;
            let sd = 1;
            const t0 = performance.now();
            const tick = () => {
              f++;
              g.fillStyle = '#123';
              g.fillRect(0, 0, 390, 844);
              for (let i = 0; i < N; i++) {
                sd = (sd * 1103515245 + 12345) & 0x7fffffff;
                g.fillStyle = `hsl(${sd % 360},60%,50%)`;
                g.fillRect((sd >> 3) % 350, (sd >> 7) % 800, 40, 40);
              }
              if (performance.now() - t0 < 1200) requestAnimationFrame(tick);
              else res(Math.round((f * 1000) / (performance.now() - t0)));
            };
            requestAnimationFrame(tick);
          }),
        1400
      );
    for (let i = 0; i < 4; i++) samples.push(await one());
    samples.shift(); // 첫 표본은 워밍업이라 항상 낮게 나온다
    bench = median(samples);
  } catch {
    /* 벤치 실패 시엔 보정 없이 원값으로 판정한다 */
  } finally {
    if (bp) await bp.close().catch(() => {});
  }
  _capacityCache = {
    bench,
    samples,
    contended: bench < BENCH_CONTENDED,
    factor: Math.min(1, Math.max(0.25, bench / BENCH_IDLE)),
    load: +loadPerCore().toFixed(2),
  };
  return _capacityCache;
}

/** rAF 프레임 수를 세서 fps 1표본을 얻는다. */
function fpsSample(page, ms = 1500) {
  return page.evaluate(
    (ms) =>
      new Promise((resolve) => {
        let frames = 0;
        const t0 = performance.now();
        const tick = () => {
          frames++;
          if (performance.now() - t0 < ms) requestAnimationFrame(tick);
          else resolve(Math.round((frames * 1000) / (performance.now() - t0)));
        };
        requestAnimationFrame(tick);
      }),
    ms
  );
}

/**
 * fps 를 3회 표본의 중앙값으로 측정하고 게이트 판정까지 낸다.
 *
 * 단일 3초 표본은 머신 부하에 따라 11~60fps 로 요동쳐서 오탐(정상 게임 탈락)과
 * 미탐(느린 게임 통과)이 둘 다 실제로 있었다. 그래서
 *   ① 1.5초 표본 3회의 중앙값을 쓰고,
 *   ② 중앙값이 게이트 미만이면 2.5초 쉬고 1회 더 재서 더 좋은 쪽을 채택하고,
 *   ③ 그래도 미달이면 합성 벤치로 머신 여유를 재서 보정값을 계산한다.
 *
 * 판정:
 *   중앙값 ≥ 30            → 통과
 *   중앙값 < 30, 보정값 ≥ 30 → 실패하되 fatal 아님 (머신 부하 탓일 수 있어 판정 보류)
 *   중앙값 < 30, 보정값 < 30 → **fatal**  ← CLAUDE.md 절대규칙 6
 *
 * 보정값 = 중앙값 / (머신 여유). 머신이 한가하면 여유가 1이라 보정이 없다 —
 * 즉 한가한 머신에서 30fps 를 못 내면 그대로 탈락이다.
 */
async function fpsMedian(page, browser, label) {
  const load0 = loadPerCore();
  const run = async () => {
    const s = [];
    for (let i = 0; i < 3; i++) {
      s.push(await fpsSample(page, 1500));
      await sleep(150);
    }
    return s;
  };
  const s1 = await run();
  let s2 = null;
  const m1 = median(s1);
  if (m1 < FPS_GATE) {
    // ② 부하 스파이크 오탐 방지: 한 번만 자동 재측정
    await sleep(2500);
    s2 = await run();
  }
  const m2 = s2 ? median(s2) : -1;
  const fps = Math.max(m1, m2);

  const r = {
    label,
    fps,
    gate: FPS_GATE,
    samples: s1,
    samples_retry: s2,
    retried: !!s2,
    load_before: +load0.toFixed(2),
    load_after: +loadPerCore().toFixed(2),
    ok: fps >= FPS_GATE,
    fatal: false,
    capacity: null,
    normalized: null,
  };
  if (!r.ok) {
    // ③ 머신이 바빴는지 확인하고, 바빴다면 보정값을 낸다
    const cap = await machineCapacity(browser);
    r.capacity = cap;
    r.normalized = Math.round(fps / cap.factor);
    r.fatal = r.normalized < FPS_GATE;
  }
  return r;
}

/** fps 결과를 검사 항목 한 줄로 옮긴다. */
function fpsDetail(r) {
  const parts = [
    `중앙값 ${r.fps}fps · 표본 [${r.samples}]${r.retried ? ` · 재측정 [${r.samples_retry}]` : ''}`,
    `부하/코어 ${r.load_before}→${r.load_after}`,
  ];
  if (r.capacity)
    parts.push(
      `머신벤치 ${r.capacity.bench}fps [${r.capacity.samples}] → 여유 ${Math.round(r.capacity.factor * 100)}% · 보정 ${r.normalized}fps` +
        (r.fatal ? ' → 게임이 느리다(치명)' : ' → 머신 부하 가능성으로 판정 보류(비치명)')
    );
  return parts.join(' · ');
}

/**
 * 문제 문장에서 "핵심 문장"만 남긴다.
 *
 * cube-unfasten 사례: 사실상 같은 문제에 "(상자 3, 시점 A)" 같은 접미사만 바꿔 붙여
 * 문자열 전체 비교 기반 다양성 검사(math.variety)를 100% 로 통과시켰다.
 * 괄호·대괄호로 묶인 주석 덩어리와 꼬리 일련번호(#3, -3)를 걷어낸 뒤 비교한다.
 * 숫자 자체는 지우지 않는다 — "3/4 × 2/3" 과 "1/2 × 5/6" 은 서로 다른 문제다.
 */
function promptCore(s) {
  let t = String(s ?? '').trim();
  t = t.replace(/[(（[［{][^)）\]］}]*[)）\]］}]/gu, ' '); // 괄호/대괄호 주석 덩어리
  t = t.replace(/[\s·,]*[#№]\s*\d+\s*$/u, ''); // 꼬리 일련번호 (#3)
  t = t.replace(/[\s·,]*[-–—]\s*\d+\s*$/u, ''); // 꼬리 일련번호 (- 3)
  t = t.replace(/\s+/g, ' ').trim();
  return t || String(s ?? '').trim();
}

// ─────────────────────────────── 실입력(real input) 검사 ───────────────────────────────
//
// __GAME_TEST__.answerCorrect() 는 게임 내부 판정을 직접 부르기 때문에 입력 경로를
// 통째로 건너뛴다. 터치가 완전히 죽은 게임(plank-up)이 41개 검사 중 40개를 통과한
// 실제 사고가 있었다. 그래서 "브라우저가 실제로 만든 pointer 이벤트"만으로
// 게임이 반응하는지를 따로 본다.
//
// 판정은 게임별 정답을 모르는 범용 검사이므로 최소 기준만 본다:
//   무입력 기준선보다 확실히 큰 변화가 getState()·DOM·캔버스 픽셀 중 하나에 생겼는가.

/** 페이지에 pointer 핸들러 호출 카운터를 심는다. goto 전에 불러야 한다. */
async function installInputProbe(page) {
  await page.evaluateOnNewDocument(() => {
    const W = (window.__QA_INPUT__ = { total: 0, byType: {}, targets: {} });
    const KIND = /^(pointer|mouse|touch|click|dblclick)/;
    const origAdd = EventTarget.prototype.addEventListener;
    const origRm = EventTarget.prototype.removeEventListener;
    EventTarget.prototype.addEventListener = function (type, fn, opts) {
      if (typeof fn === 'function' && KIND.test(String(type))) {
        const self = this;
        const wrapped = function (ev) {
          try {
            if (ev && ev.isTrusted) {
              W.total++;
              W.byType[type] = (W.byType[type] || 0) + 1;
              const tag =
                self === window ? 'window' : self === document ? 'document' : (self.tagName || '?') + (self.id ? '#' + self.id : '');
              W.targets[tag] = (W.targets[tag] || 0) + 1;
            }
          } catch {}
          return fn.apply(this, arguments);
        };
        try {
          fn.__qaWrapped = wrapped;
        } catch {}
        return origAdd.call(this, type, wrapped, opts);
      }
      return origAdd.call(this, type, fn, opts);
    };
    EventTarget.prototype.removeEventListener = function (type, fn, opts) {
      return origRm.call(this, type, (fn && fn.__qaWrapped) || fn, opts);
    };
  });
}

/** getState / DOM 서명 / 캔버스 축소 픽셀을 한 번에 뜬다. */
function snapshotPage(page) {
  return page.evaluate(() => {
    let pix = null;
    const cv = document.querySelector('canvas');
    if (cv) {
      try {
        const c = document.createElement('canvas');
        c.width = 48;
        c.height = 96;
        const x = c.getContext('2d');
        x.drawImage(cv, 0, 0, 48, 96);
        const d = x.getImageData(0, 0, 48, 96).data;
        pix = [];
        for (let i = 0; i < d.length; i += 4) pix.push((d[i] >> 4) * 256 + (d[i + 1] >> 4) * 16 + (d[i + 2] >> 4));
      } catch {
        pix = null;
      }
    }
    const dom = [];
    const els = document.querySelectorAll('body *');
    for (let i = 0; i < Math.min(els.length, 400); i++) {
      const e = els[i];
      const r = e.getBoundingClientRect();
      dom.push(
        `${e.tagName}|${String(e.className)}|${Math.round(r.x)},${Math.round(r.y)},${Math.round(r.width)},${Math.round(r.height)}|` +
          (e.childElementCount ? '' : (e.textContent || '').trim().slice(0, 40))
      );
    }
    let state = '';
    try {
      state = JSON.stringify(window.__GAME_TEST__.getState() || {});
    } catch {}
    return { pix, dom, state };
  });
}

const seqDiff = (a, b) => {
  if (!a || !b) return -1;
  const n = Math.max(a.length, b.length) || 1;
  let d = Math.abs(a.length - b.length);
  for (let i = 0; i < Math.min(a.length, b.length); i++) if (a[i] !== b[i]) d++;
  return d / n;
};

/** 음소거·도움말 같은 게임 크롬 버튼은 "게임이 반응했다"의 근거가 될 수 없다. */
const CHROME_SELECTOR_NOTE =
  'mute|sound|audio|volume|speaker|help|howto|how-to|info|guide|설명|도움|소리|음소거|설정|option|setting|pause|일시정지|close|닫기|back|뒤로|fullscreen|전체화면|share|공유';

/** 화면에서 눌러볼 만한 지점을 뽑는다 (게임 크롬 제외). */
function collectTargets(page, chromeRe) {
  return page.evaluate((reSrc) => {
    const RE = new RegExp(reSrc, 'i');
    const isChrome = (el) => {
      for (let e = el; e && e !== document.body; e = e.parentElement) {
        const s = `${e.id || ''} ${String(e.className || '')} ${e.getAttribute ? e.getAttribute('aria-label') || '' : ''}`;
        if (RE.test(s)) return true;
      }
      return false;
    };
    const sel =
      'canvas, button, [role="button"], [tabindex], a[href], .card, .slot, .tile, .btn, .cell, .choice, .option, [data-index], [data-slot], [data-choice]';
    const out = [];
    const seen = new Set();
    for (const e of document.querySelectorAll(sel)) {
      const r = e.getBoundingClientRect();
      if (r.width < 8 || r.height < 8) continue;
      if (r.bottom < 4 || r.top > innerHeight - 4 || r.right < 4 || r.left > innerWidth - 4) continue;
      const st = getComputedStyle(e);
      if (st.visibility === 'hidden' || st.display === 'none' || st.pointerEvents === 'none' || Number(st.opacity) === 0) continue;
      if (isChrome(e)) continue;
      const x = Math.round(Math.min(Math.max(r.left + r.width / 2, 3), innerWidth - 3));
      const y = Math.round(Math.min(Math.max(r.top + r.height / 2, 3), innerHeight - 3));
      const k = `${x},${y}`;
      if (seen.has(k)) continue;
      seen.add(k);
      out.push({ x, y, big: r.width * r.height > innerWidth * innerHeight * 0.4 });
    }
    return out.slice(0, 24);
  }, chromeRe);
}

/** 격자점이 게임 크롬 위인지 확인한다 (크롬이면 그 제스처는 근거에서 뺀다). */
function pointIsChrome(page, x, y, chromeRe) {
  return page.evaluate(
    ({ x, y, reSrc }) => {
      const RE = new RegExp(reSrc, 'i');
      let e = document.elementFromPoint(x, y);
      for (; e && e !== document.body; e = e.parentElement) {
        const s = `${e.id || ''} ${String(e.className || '')} ${e.getAttribute ? e.getAttribute('aria-label') || '' : ''}`;
        if (RE.test(s)) return true;
      }
      return false;
    },
    { x, y, reSrc: chromeRe }
  );
}

async function realInputCheck(page, W, H) {
  const chromeRe = CHROME_SELECTOR_NOTE;

  // 1) 무입력 기준선 — 애니메이션만으로도 픽셀은 계속 바뀐다. 그 폭을 먼저 잰다.
  const b0 = await snapshotPage(page);
  await sleep(3000);
  const b1 = await snapshotPage(page);
  const idlePix = seqDiff(b0.pix, b1.pix);
  const idleDom = seqDiff(b0.dom, b1.dom);

  const base = await snapshotPage(page);
  const changedVs = (now) => {
    const dp = seqDiff(base.pix, now.pix);
    const dd = seqDiff(base.dom, now.dom);
    const stateChanged = now.state !== base.state && now.state !== '';
    const domChanged = dd >= 0 && dd > idleDom + 0.01;
    const pixChanged = dp >= 0 && dp > Math.max(idlePix * 1.6, idlePix + 0.04);
    return { hit: stateChanged || domChanged || pixChanged, dp, dd, stateChanged, domChanged, pixChanged };
  };

  // 2) 제스처 계획: 후보 엘리먼트 탭 → 후보끼리 드래그 → 격자 탭·드래그(캔버스 게임용)
  const targets = (await collectTargets(page, chromeRe)).filter((t) => !t.big);
  const gestures = [];
  for (const t of targets.slice(0, 10)) gestures.push({ kind: 'tap', x: t.x, y: t.y });
  for (let i = 0; i < Math.min(targets.length, 6); i++)
    for (let j = 0; j < Math.min(targets.length, 6); j++)
      if (i !== j) gestures.push({ kind: 'drag', x: targets[i].x, y: targets[i].y, x2: targets[j].x, y2: targets[j].y });
  for (const fx of [0.3, 0.5, 0.7])
    for (const fy of [0.32, 0.52, 0.72]) {
      const x = Math.round(W * fx);
      const y = Math.round(H * fy);
      gestures.push({ kind: 'tap', x, y, grid: true });
      gestures.push({ kind: 'drag', x, y, x2: Math.min(x + 60, W - 4), y2: Math.max(y - 90, 4), grid: true });
      gestures.push({ kind: 'drag', x, y, x2: Math.max(x - 60, 4), y2: Math.min(y + 90, H - 4), grid: true });
    }

  const runMouse = async (g) => {
    await page.mouse.move(g.x, g.y);
    await page.mouse.down();
    if (g.kind === 'drag') {
      await page.mouse.move((g.x + g.x2) / 2, (g.y + g.y2) / 2, { steps: 4 });
      await page.mouse.move(g.x2, g.y2, { steps: 4 });
    } else {
      await page.mouse.move(g.x + 1, g.y + 1);
    }
    await page.mouse.up();
  };
  const runTouch = async (g) => {
    const ts = page.touchscreen;
    if (!ts || typeof ts.touchStart !== 'function') throw new Error('no touchscreen api');
    await ts.touchStart(g.x, g.y);
    if (g.kind === 'drag') {
      await ts.touchMove((g.x + g.x2) / 2, (g.y + g.y2) / 2);
      await ts.touchMove(g.x2, g.y2);
    } else {
      await ts.touchMove(g.x + 1, g.y + 1);
    }
    await ts.touchEnd();
  };

  const pass = async (run) => {
    let n = 0;
    for (const g of gestures) {
      if (g.grid && (await pointIsChrome(page, g.x, g.y, chromeRe))) continue;
      n++;
      try {
        await run(g);
      } catch (e) {
        return { n, error: String(e).slice(0, 120) };
      }
      await sleep(70);
      const now = await snapshotPage(page);
      const r = changedVs(now);
      if (r.hit) return { n, hit: true, gesture: g, ...r };
    }
    return { n, hit: false };
  };

  let mouse = await pass(runMouse);
  let touch = null;
  if (!mouse.hit) {
    try {
      touch = await pass(runTouch);
    } catch (e) {
      touch = { error: String(e).slice(0, 120) };
    }
  }

  // 3) 그래도 못 잡으면 훅의 선택적 확장 simulateInput() 에 마지막 기회를 준다.
  //    (게임 고유 제스처가 범용 격자로 재현 불가능한 경우의 탈출구)
  let hook = null;
  if (!mouse.hit && !(touch && touch.hit)) {
    hook = await page.evaluate(async () => {
      const t = window.__GAME_TEST__;
      if (!t || typeof t.simulateInput !== 'function') return { available: false };
      try {
        await t.simulateInput();
        return { available: true, ok: true };
      } catch (e) {
        return { available: true, ok: false, error: String(e).slice(0, 120) };
      }
    });
    if (hook.available && hook.ok) {
      await sleep(500);
      const now = await snapshotPage(page);
      hook.result = changedVs(now);
    }
  }

  const reach = await page.evaluate(() => window.__QA_INPUT__ || { total: 0 });
  const hit = !!(mouse.hit || (touch && touch.hit) || (hook && hook.result && hook.result.hit));
  return {
    hit,
    via: mouse.hit ? 'mouse' : touch && touch.hit ? 'touch' : hook && hook.result && hook.result.hit ? 'simulateInput' : null,
    idlePix: +Number(idlePix).toFixed(4),
    idleDom: +Number(idleDom).toFixed(4),
    gestures: gestures.length,
    mouse,
    touch,
    hook,
    reach,
  };
}

const slug = process.argv[2];
if (!slug) {
  console.error('사용법: node factory/lib/qa.mjs <slug>');
  process.exit(2);
}

const outIdx = process.argv.indexOf('--out');
const OUT = outIdx > 0 ? process.argv[outIdx + 1] : path.join(P.work, 'qa', slug);
const gameDir = path.join(P.games, slug);
fs.mkdirSync(OUT, { recursive: true });

const checks = [];
// finish() 가 치명적 결함으로 조기 종료될 수도 있어서, 참조하는 변수는 전부 미리 초기화해 둔다
// (그렇지 않으면 let 의 TDZ 때문에 '치명적 결함 보고' 자체가 크래시로 죽는다 — 실제로 있었던 버그).
let problems = [];
let perf = {};
const add = (id, label, ok, detail = '', fatal = false) =>
  checks.push({ id, label, ok: !!ok, detail: String(detail).slice(0, 800), fatal });

// ─────────────────────────────────────────── 1. 정적 검사

if (!fs.existsSync(gameDir)) {
  add('exists', '게임 폴더 존재', false, gameDir, true);
  finish();
}

const htmlPath = path.join(gameDir, 'index.html');
const metaPath = path.join(gameDir, 'meta.json');
add('file.index', 'index.html 존재', fs.existsSync(htmlPath), htmlPath, true);
add('file.meta', 'meta.json 존재', fs.existsSync(metaPath), metaPath, true);
add('file.thumb', 'thumb.png 존재', fs.existsSync(path.join(gameDir, 'thumb.png')));
// square.png 는 2026-08-18 도입. 4회차(decimal-smash)에서 리뷰 게이트의 자가 복구
// (must_fix → fix 라운드에서 codex 로 생성)까지 검증됐으므로 fatal 로 승격한다.
add('file.square', 'square.png 존재 (공유용 정사각 이미지)', fs.existsSync(path.join(gameDir, 'square.png')), '', true);

const thumbDim = pngSize(path.join(gameDir, 'thumb.png'));
add(
  'thumb.dims',
  'thumb.png 가 1200×630',
  !!thumbDim && thumbDim.w === 1200 && thumbDim.h === 630,
  thumbDim ? `${thumbDim.w}×${thumbDim.h}` : '읽기 실패'
);

const squareDim = pngSize(path.join(gameDir, 'square.png'));
add(
  'square.dims',
  'square.png 가 정확히 정사각(가로=세로), 800px 이상',
  !!squareDim && squareDim.w === squareDim.h && squareDim.w >= 800,
  squareDim ? `${squareDim.w}×${squareDim.h}` : '읽기 실패',
  true
);

// 이미지 생성 에이전트가 동시성 버그로 다른 게임의 이미지를 잘못 가져온 사고가
// 실제로 있었다(symmetry-breaker 의 square.png 가 rounding-dash 와 byte-identical 이었음).
// 표지 이미지가 다른 게임과 완전히 동일하면 그 사고가 재발한 것이다.
const sha256 = (f) => {
  try {
    return crypto.createHash('sha256').update(fs.readFileSync(f)).digest('hex');
  } catch {
    return null;
  }
};
const myThumbHash = sha256(path.join(gameDir, 'thumb.png'));
const mySquareHash = sha256(path.join(gameDir, 'square.png'));
const dupes = [];
if (fs.existsSync(P.games)) {
  for (const other of fs.readdirSync(P.games)) {
    if (other === slug) continue;
    const otherDir = path.join(P.games, other);
    if (myThumbHash && myThumbHash === sha256(path.join(otherDir, 'thumb.png'))) dupes.push(`thumb.png = ${other}/thumb.png`);
    if (mySquareHash && mySquareHash === sha256(path.join(otherDir, 'square.png'))) dupes.push(`square.png = ${other}/square.png`);
  }
}
add(
  'image.notduplicate',
  '표지 이미지가 다른 게임과 동일하지 않음',
  dupes.length === 0,
  dupes.join(', '),
  true
);

const html = fs.existsSync(htmlPath) ? fs.readFileSync(htmlPath, 'utf8') : '';
const meta = readJSON(metaPath, null);

// meta 스키마
const REQUIRED_META = ['slug', 'title', 'tagline', 'grade', 'semester', 'unit', 'standards', 'mechanic', 'description', 'howto'];
if (meta) {
  const missing = REQUIRED_META.filter((k) => meta[k] === undefined || meta[k] === null || meta[k] === '');
  add('meta.schema', 'meta.json 필수 필드', missing.length === 0, missing.length ? `누락: ${missing.join(', ')}` : '', true);
  add('meta.slug', 'meta.slug 가 폴더명과 일치', meta.slug === slug, `${meta.slug} vs ${slug}`, true);
  add('meta.title', '제목 길이 2~24자', typeof meta.title === 'string' && meta.title.length >= 2 && meta.title.length <= 24, meta.title);

  // 성취기준이 교육과정에 실재하는지 대조
  const curriculum = readJSON(P.curriculum, { standards: [], units: [] });
  const known = new Set((curriculum.standards || []).map((s) => s.code));
  const bad = (meta.standards || []).filter((c) => !known.has(c));
  add(
    'meta.standards',
    '성취기준 코드가 교육과정에 실재',
    (meta.standards || []).length > 0 && bad.length === 0,
    bad.length ? `교육과정에 없는 코드: ${bad.join(', ')}` : `${(meta.standards || []).join(', ')}`,
    true
  );

  const unitExists = (curriculum.units || []).some((u) => u.id === meta.unit?.id);
  add('meta.unit', '단원 id 가 교육과정에 실재', unitExists, meta.unit?.id, true);
}

// CDN / 절대경로 금지
const cdnHits = [...html.matchAll(/https?:\/\/[^\s"'`)]+/g)]
  .map((m) => m[0])
  .filter((u) => !/^https?:\/\/(www\.)?(w3\.org|schema\.org)/.test(u));
add('static.nocdn', '외부 CDN·원격 리소스 없음', cdnHits.length === 0, cdnHits.slice(0, 6).join('\n'));

const absAssets = [...html.matchAll(/(?:src|href)\s*=\s*["']\/(?!\/)([^"']*)["']/g)].map((m) => m[0]);
add('static.relpath', '절대경로 리소스 없음', absAssets.length === 0, absAssets.slice(0, 6).join('\n'));

add('static.testhook', '__GAME_TEST__ 훅 코드 존재', /__GAME_TEST__/.test(html), '', true);
add('static.viewport', 'viewport 메타 태그', /<meta[^>]+name=["']viewport["']/i.test(html));
add('static.lang', 'lang="ko"', /<html[^>]+lang=["']ko["']/i.test(html));
add('static.title', '<title> 존재', /<title>[^<]{2,}<\/title>/i.test(html));

const sizeKB = fs.existsSync(htmlPath) ? Math.round(fs.statSync(htmlPath).size / 1024) : 0;
add('static.size', 'index.html 1MB 미만', sizeKB < 1024, `${sizeKB}KB`);

// 에셋 총 용량
let assetKB = 0;
const walk = (d) => {
  if (!fs.existsSync(d)) return;
  for (const e of fs.readdirSync(d, { withFileTypes: true })) {
    const f = path.join(d, e.name);
    if (e.isDirectory()) walk(f);
    else assetKB += fs.statSync(f).size / 1024;
  }
};
walk(gameDir);
assetKB = Math.round(assetKB);
add('static.assets', '게임 폴더 총 12MB 미만', assetKB < 12 * 1024, `${assetKB}KB`);

if (checks.some((c) => c.fatal && !c.ok)) finish();

// ─────────────────────────────────────────── 2. 브라우저 검사

const consoleErrors = [];
const pageErrors = [];
const failedRequests = [];

const server = await serveStatic(P.publicDir);
const browser = await puppeteer.launch({
  // shell 헤드리스는 WebGL2 가 없다. three.js 게임을 검사하려면 풀 Chrome + swiftshader 가 필요하다.
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

try {
  const page = await browser.newPage();
  // 게임과 무관한 브라우저 자동 요청은 잡음이므로 제외한다.
  const isNoise = (u = '') => /favicon\.ico|\/\.well-known\//.test(u);

  page.on('console', (m) => {
    if (m.type() === 'error' && !isNoise(m.text())) consoleErrors.push(m.text().slice(0, 300));
  });
  page.on('pageerror', (e) => pageErrors.push(String(e).slice(0, 300)));
  page.on('requestfailed', (r) => {
    if (!isNoise(r.url())) failedRequests.push(`${r.url()} — ${r.failure()?.errorText}`);
  });
  page.on('response', (r) => {
    if (r.status() >= 400 && !isNoise(r.url())) failedRequests.push(`${r.status()} ${r.url()}`);
  });

  const url = `${server.url}/g/${slug}/`;

  // 실입력 검사용 계측은 문서 로드 전에 심어야 한다.
  await installInputProbe(page);

  // --- 모바일 뷰포트
  await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 2, isMobile: true, hasTouch: true });
  await page.goto(url, { waitUntil: 'networkidle2', timeout: 45000 });

  // 훅 준비 대기
  const ready = await page
    .waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 20000 })
    .then(() => true)
    .catch(() => false);
  add('run.ready', '__GAME_TEST__.ready 20초 내 true', ready, '', true);

  if (ready) {
    const api = await page.evaluate(() => {
      const t = window.__GAME_TEST__;
      return ['start', 'getState', 'answerCorrect', 'answerWrong', 'sampleProblems'].filter(
        (k) => typeof t[k] !== 'function'
      );
    });
    add('run.api', '훅 필수 메서드 5종 구현', api.length === 0, api.length ? `누락: ${api.join(', ')}` : '', true);

    // 문제 생성기 표본 검증.
    // n 을 하나로 고정해 부르면 결정적 시드 게임은 매번 같은 40문항만 보여 준다.
    // 세 가지 크기로 나눠 부른 뒤 합집합(중복 제거)을 검산 표본으로 쓴다.
    const SAMPLE_SIZES = [40, 17, 63];
    const sampleRuns = [];
    for (const n of SAMPLE_SIZES) {
      const got = await page.evaluate((n) => {
        try {
          return window.__GAME_TEST__.sampleProblems(n) || [];
        } catch (e) {
          return { __error: String(e) };
        }
      }, n);
      sampleRuns.push({ n, got });
    }
    const okRuns = sampleRuns.filter((r) => Array.isArray(r.got));
    // 다양성은 "한 번의 호출 안에서" 재는 게 맞다. 합집합으로 재면 결정적 생성기가
    // 세 번 다 같은 문제를 돌려줘도 중복 제거 뒤엔 100% 처럼 보인다.
    const varietyRun = okRuns.length ? okRuns.reduce((a, b) => (b.got.length > a.got.length ? b : a)) : null;

    if (okRuns.length === sampleRuns.length) {
      const sig = (p) => JSON.stringify([p?.prompt, p?.answer, p?.choices]);
      const seenSig = new Set();
      problems = [];
      for (const r of okRuns)
        for (const p of r.got) {
          const k = sig(p);
          if (seenSig.has(k)) continue;
          seenSig.add(k);
          problems.push(p);
        }
    } else {
      problems = sampleRuns.find((r) => !Array.isArray(r.got))?.got ?? [];
    }

    if (Array.isArray(problems)) {
      add(
        'math.count',
        '문제 표본 생성 (n=40/17/63 합집합)',
        problems.length >= 20,
        `합집합 ${problems.length}개 · 호출별 ${sampleRuns.map((r) => `${r.n}→${Array.isArray(r.got) ? r.got.length : 'ERR'}`).join(', ')}`
      );

      const noPrompt = problems.filter((p) => !p?.prompt || String(p.prompt).trim().length < 2);
      add('math.prompt', '모든 문제에 문장 존재', noPrompt.length === 0, `${noPrompt.length}개 누락`);

      const noAnswer = problems.filter((p) => p?.answer === undefined || p?.answer === null || p?.answer === '');
      add('math.answer', '모든 문제에 정답 존재', noAnswer.length === 0, `${noAnswer.length}개 누락`, true);

      const mc = problems.filter((p) => Array.isArray(p.choices) && p.choices.length > 0);
      const answerNotInChoices = mc.filter((p) => !p.choices.map(String).includes(String(p.answer)));
      add(
        'math.choices',
        '정답이 선택지 안에 존재',
        answerNotInChoices.length === 0,
        answerNotInChoices.slice(0, 3).map((p) => `${p.prompt} → ${p.answer} ∉ [${p.choices}]`).join(' | '),
        true
      );

      const dupChoices = mc.filter((p) => new Set(p.choices.map(String)).size !== p.choices.length);
      add('math.dupchoices', '선택지 중복 없음', dupChoices.length === 0, `${dupChoices.length}문항`);

      // 다양성: 프롬프트 문자열 전체가 아니라 "핵심 문장"으로 센다.
      // 문자열 전체 비교는 "(상자 3, 시점 A)" 같은 접미사만 갈아 끼우면 무력화된다
      // (cube-unfasten 은 사실상 10문제로 100% 를 받았다).
      const varietyPool = (varietyRun?.got ?? problems).map((p) => String(p?.prompt ?? ''));
      const rawUniq = new Set(varietyPool);
      const coreUniq = new Set(varietyPool.map(promptCore));
      const variety = varietyPool.length ? coreUniq.size / varietyPool.length : 0;
      const rawVariety = varietyPool.length ? rawUniq.size / varietyPool.length : 0;
      add(
        'math.variety',
        '문제 다양성 70% 이상 (괄호 주석·꼬리 번호 제거한 핵심 문장 기준)',
        variety >= 0.7,
        `핵심 ${Math.round(variety * 100)}% (${coreUniq.size}/${varietyPool.length}) · 원문 ${Math.round(rawVariety * 100)}% · n=${varietyRun?.n ?? '?'}`
      );

      const badNumeric = problems.filter((p) => p.answerNumeric !== undefined && !Number.isFinite(Number(p.answerNumeric)));
      add('math.numeric', 'answerNumeric 유효', badNumeric.length === 0, `${badNumeric.length}개 비정상`);
    } else {
      add('math.count', '문제 표본 생성', false, JSON.stringify(problems).slice(0, 200), true);
      problems = [];
    }

    // 플레이 스모크 테스트
    const smoke = await page.evaluate(async () => {
      const t = window.__GAME_TEST__;
      const snap = () => JSON.parse(JSON.stringify(t.getState() || {}));
      const before = snap();
      t.start();
      await new Promise((r) => setTimeout(r, 600));
      const started = snap();
      t.answerCorrect();
      await new Promise((r) => setTimeout(r, 400));
      const afterCorrect = snap();
      t.answerWrong();
      await new Promise((r) => setTimeout(r, 400));
      const afterWrong = snap();
      return { before, started, afterCorrect, afterWrong };
    });
    add(
      'run.score',
      '정답 시 점수 상승',
      Number(smoke.afterCorrect?.score ?? 0) > Number(smoke.started?.score ?? 0),
      JSON.stringify(smoke)
    );
    add(
      'run.penalty',
      '오답 시 상태 변화(점수/목숨)',
      JSON.stringify(smoke.afterWrong) !== JSON.stringify(smoke.afterCorrect),
      ''
    );

    // 실입력 경로 — 훅을 거치지 않고 진짜 pointer 이벤트만으로 게임이 반응하는가
    const input = await realInputCheck(page, 390, 844);
    add(
      'input.real',
      '실제 pointer 제스처로 게임 상태·화면이 반응 (훅 우회)',
      input.hit,
      input.hit
        ? `${input.via} · ${input.mouse?.n ?? 0}번째 제스처에서 감지 · 핸들러 호출 ${input.reach?.total ?? 0}회`
        : `제스처 ${input.gestures}개 전부 무반응 (기준선 pix=${input.idlePix} dom=${input.idleDom}, ` +
          `pointer 핸들러 호출 ${input.reach?.total ?? 0}회 ${JSON.stringify(input.reach?.targets ?? {}).slice(0, 120)}). ` +
          `answerCorrect() 로는 통과해도 실제 손가락으로는 아무 반응이 없다 — 입력 경로가 죽었다.`,
      true
    );
    // 제스처 때문에 게임이 종료 상태로 갔을 수 있으니 fps 측정 전에 다시 시작한다.
    await page.evaluate(() => {
      try {
        window.__GAME_TEST__.start();
      } catch {}
    });
    await sleep(600);

    // 성능 — 모바일. 3회 표본의 중앙값 (단일 표본은 머신 부하로 요동친다)
    const fpsMobile = await fpsMedian(page, browser, 'mobile-390');
    perf = { fps: fpsMobile.fps, mobile: fpsMobile, input: { hit: input.hit, via: input.via, reach: input.reach?.total ?? 0 } };
    add('perf.fps', `모바일 FPS 중앙값 ${FPS_GATE} 이상 (3회 측정)`, fpsMobile.ok, fpsDetail(fpsMobile), fpsMobile.fatal);
  }

  // 모바일 레이아웃
  const overflow = await page.evaluate(() => ({
    scrollW: document.documentElement.scrollWidth,
    clientW: document.documentElement.clientWidth,
  }));
  add(
    'mobile.overflow',
    '모바일 가로 스크롤 없음',
    overflow.scrollW <= overflow.clientW + 2,
    `scrollW=${overflow.scrollW} clientW=${overflow.clientW}`
  );

  const smallTargets = await page.evaluate(() => {
    const sel = 'button, [role="button"], a, input[type="button"], .btn';
    return [...document.querySelectorAll(sel)]
      .filter((el) => {
        const r = el.getBoundingClientRect();
        const st = getComputedStyle(el);
        return r.width > 0 && r.height > 0 && st.visibility !== 'hidden' && (r.width < 40 || r.height < 40);
      })
      .slice(0, 5)
      .map((el) => `${el.tagName}.${el.className}`.slice(0, 60));
  });
  add('mobile.touch', '터치 타깃 40px 이상', smallTargets.length === 0, smallTargets.join(', '));

  await page.screenshot({ path: path.join(OUT, 'mobile.png') });

  // 화면이 비어있지 않은지 (픽셀 다양성)
  const variance = await page.evaluate(async () => {
    const c = document.createElement('canvas');
    c.width = 64;
    c.height = 128;
    const ctx = c.getContext('2d');
    // canvas 게임이면 그 캔버스를, 아니면 배경색 판단만
    const gameCanvas = document.querySelector('canvas');
    if (!gameCanvas) return null;
    try {
      ctx.drawImage(gameCanvas, 0, 0, 64, 128);
      const d = ctx.getImageData(0, 0, 64, 128).data;
      const seen = new Set();
      for (let i = 0; i < d.length; i += 16) seen.add(`${d[i] >> 4},${d[i + 1] >> 4},${d[i + 2] >> 4}`);
      return seen.size;
    } catch {
      return null;
    }
  });
  if (variance !== null) add('visual.notblank', '캔버스가 단색이 아님', variance >= 4, `색상군 ${variance}개`);

  // --- 태블릿 / 데스크톱
  await page.setViewport({ width: 820, height: 1180, deviceScaleFactor: 2, isMobile: true, hasTouch: true });
  await new Promise((r) => setTimeout(r, 900));
  const tabOverflow = await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 2);
  add('tablet.overflow', '태블릿 가로 스크롤 없음', tabOverflow);
  await page.screenshot({ path: path.join(OUT, 'tablet.png') });

  // 판형 게이트 (docs/playfield-spec.md) — 1280×800에서 플레이 화면이 와이드로
  // 재배치되는가. 기계 판정은 html.land 클래스(또는 게임이 노출하는 레이아웃 정보)의
  // 활성 여부까지만 보고, "중앙 좁은 컬럼+빈 거터" 여부는 desktop-1280.png 를
  // 검수관이 육안 확인한다. 구 640px 중앙 컬럼 규범은 2026-08-28 폐기됐다.
  await page.setViewport({ width: 1280, height: 800, deviceScaleFactor: 1 });
  await page.evaluate(() => { try { window.__GAME_TEST__ && typeof window.__GAME_TEST__.start === 'function' && window.__GAME_TEST__.start(); } catch {} });
  await new Promise((r) => setTimeout(r, 900));
  const wideLayout = await page.evaluate(() => {
    const root = document.documentElement;
    const stageWRaw = getComputedStyle(root).getPropertyValue('--stageW').trim();
    let hookLayout = null;
    try {
      const t = window.__GAME_TEST__;
      if (t && typeof t.getLayout === 'function') hookLayout = t.getLayout();
    } catch {}
    return { land: root.classList.contains('land'), stageW: stageWRaw, hookLayout };
  });
  const stagePx = parseFloat(wideLayout.stageW) || 0;
  const wideActive =
    wideLayout.land ||
    stagePx >= 960 ||
    (wideLayout.hookLayout && (wideLayout.hookLayout.land === true || Number(wideLayout.hookLayout.playW) >= 960));
  add(
    'layout.wide1280',
    '1280px에서 와이드 분기 활성 (html.land 또는 --stageW≥960 또는 getLayout)',
    wideActive,
    JSON.stringify(wideLayout).slice(0, 200)
  );
  await page.screenshot({ path: path.join(OUT, 'desktop-1280.png') });

  // 데스크톱 fps — 모바일에서만 재면 와이드 분기의 그리기 비용을 놓친다.
  if (ready) {
    const fpsDesktop = await fpsMedian(page, browser, 'desktop-1280');
    perf.desktop1280 = fpsDesktop;
    add('perf.fps1280', `데스크톱(1280) FPS 중앙값 ${FPS_GATE} 이상 (3회 측정)`, fpsDesktop.ok, fpsDetail(fpsDesktop), fpsDesktop.fatal);
  }

  await page.setViewport({ width: 1440, height: 900, deviceScaleFactor: 1 });
  await page.evaluate(() => { try { window.__GAME_TEST__ && typeof window.__GAME_TEST__.start === 'function' && window.__GAME_TEST__.start(); } catch {} });
  await new Promise((r) => setTimeout(r, 900));
  await page.screenshot({ path: path.join(OUT, 'desktop.png') });

  // 초와이드(≈2000px) — 실사용자가 와이드 창에서 네모공장 레이아웃이 통째로
  // 깨진 걸 리포트한 뒤 추가. 기계 판정은 가로 스크롤 여부뿐이지만, wide.png
  // 스크린샷을 남겨 검수관이 육안으로 깨짐을 확인하게 한다.
  await page.setViewport({ width: 2000, height: 1045, deviceScaleFactor: 1 });
  await page.evaluate(() => { try { window.__GAME_TEST__ && typeof window.__GAME_TEST__.start === 'function' && window.__GAME_TEST__.start(); } catch {} });
  await new Promise((r) => setTimeout(r, 900));
  const wideOverflow = await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 2);
  add('wide.overflow', '초와이드(2000px) 가로 스크롤 없음', wideOverflow);
  await page.screenshot({ path: path.join(OUT, 'wide.png') });

  add('run.console', '콘솔 에러 0건', consoleErrors.length === 0, consoleErrors.slice(0, 5).join('\n'), true);
  add('run.pageerror', '미처리 예외 0건', pageErrors.length === 0, pageErrors.slice(0, 5).join('\n'), true);
  add('run.network', '실패 요청 0건', failedRequests.length === 0, failedRequests.slice(0, 5).join('\n'), true);
} catch (err) {
  add('run.crash', 'QA 실행 중 예외', false, err?.stack || String(err), true);
} finally {
  await browser.close().catch(() => {});
  await server.close().catch(() => {});
}

finish();

// ───────────────────────────────────────────

function finish() {
  const failed = checks.filter((c) => !c.ok);
  const fatal = failed.filter((c) => c.fatal);
  const report = {
    slug,
    checked_at: nowKST(),
    total: checks.length,
    passed: checks.length - failed.length,
    failed: failed.length,
    fatal: fatal.length,
    auto_pass: fatal.length === 0,
    perf,
    // 검산 에이전트가 전수 검산할 표본. 줄이지 마라 — 이게 수학 오류를 잡는 근거다.
    problems_sample: Array.isArray(problems) ? problems : [],
    checks,
    screenshots: ['mobile.png', 'tablet.png', 'desktop-1280.png', 'desktop.png', 'wide.png']
      .map((f) => path.join(OUT, f))
      .filter((f) => fs.existsSync(f)),
  };
  writeJSON(path.join(OUT, 'report.json'), report);

  const mark = (c) => (c.ok ? '✅' : c.fatal ? '💀' : '⚠️ ');
  console.log(`\n━━━ QA: ${slug} ━━━`);
  for (const c of checks) console.log(`${mark(c)} ${c.label}${c.ok ? '' : ` — ${c.detail.split('\n')[0]}`}`);
  console.log(`\n통과 ${report.passed}/${report.total} · 치명적 결함 ${report.fatal}건`);
  console.log(`리포트: ${path.join(OUT, 'report.json')}`);

  process.exit(report.auto_pass ? 0 : 1);
}
