#!/usr/bin/env node
/**
 * 자동 QA 하네스.
 *
 *   node factory/lib/qa.mjs <slug> [--out <dir>]
 *
 * 정적 검사 + 헤드리스 브라우저 플레이 테스트를 돌리고
 *   factory/work/qa/<slug>/report.json
 *   factory/work/qa/<slug>/{mobile,tablet,desktop}.png
 * 를 남긴다. 치명적 결함이 있으면 exit 1.
 *
 * 이 하네스는 "기계가 확실히 판정할 수 있는 것"만 본다.
 * 재미·비주얼·교육과정 정합성 같은 판단은 40-review.md 검수 에이전트가 맡는다.
 */
import fs from 'node:fs';
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

let problems = [];
let perf = {};

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

    // 문제 생성기 표본 검증
    problems = await page.evaluate(() => {
      try {
        return window.__GAME_TEST__.sampleProblems(40) || [];
      } catch (e) {
        return { __error: String(e) };
      }
    });

    if (Array.isArray(problems)) {
      add('math.count', '문제 표본 40개 생성', problems.length >= 20, `${problems.length}개`);

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

      const uniquePrompts = new Set(problems.map((p) => String(p?.prompt)));
      const variety = problems.length ? uniquePrompts.size / problems.length : 0;
      add('math.variety', '문제 다양성 70% 이상', variety >= 0.7, `${Math.round(variety * 100)}% (${uniquePrompts.size}/${problems.length})`);

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

    // 성능
    perf = await page.evaluate(
      () =>
        new Promise((resolve) => {
          let frames = 0;
          const t0 = performance.now();
          const tick = () => {
            frames++;
            if (performance.now() - t0 < 3000) requestAnimationFrame(tick);
            else resolve({ fps: Math.round((frames * 1000) / (performance.now() - t0)) });
          };
          requestAnimationFrame(tick);
        })
    );
    add('perf.fps', 'FPS 30 이상', (perf.fps ?? 0) >= 30, `${perf.fps}fps`);
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

  await page.setViewport({ width: 1440, height: 900, deviceScaleFactor: 1 });
  await new Promise((r) => setTimeout(r, 900));
  await page.screenshot({ path: path.join(OUT, 'desktop.png') });

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
    screenshots: ['mobile.png', 'tablet.png', 'desktop.png']
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
