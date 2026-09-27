// Art round 4 acceptance gates. This is deliberately a black-box WebGL test:
// the game supplies read-only world-to-screen projections and this harness
// measures the rendered pixels/UI geometry. Missing or malformed evidence is a
// failure, never a pass.
//
// Usage (repo root):
//   node factory/unity-src/hyeopgok-sasu/ArtSource/validation/art-r4/gates.mjs
// Optional:
//   PUPPETEER_EXECUTABLE_PATH=/path/to/chrome ART_R4_KEEP_FRAMES=0 node ...
//
// Canonical read-only debug schema (CSS pixels, top-left origin):
// window.__HYEOPGOK_ART_DEBUG__.snapshot() => {
//   version: 1,
//   samples: [{
//     id: 'king', type: 'king'|'soldier'|'building'|'tree', visible: true,
//     foot: {x, y},                 // ground pixel directly below the object
//     ground: {x, y},               // optional nearby unobstructed ground
//     ring: [{x, y}, ...]           // nearby ground; >=3 in-frame refs required
//   }],
//   frontContact: {x, y, visible: true},
//   questionRect: {x, y, width, height, visible: true},
//   activePadRects: [{id, x, y, width, height, visible: true}]
// }
// getSnapshot() and a plain data object are accepted aliases. Points in [0,1]
// are accepted as normalized coordinates but CSS pixels are the source contract.

import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../../lib/static-server.mjs';

const HERE = path.dirname(new URL(import.meta.url).pathname);
const ROOT = path.resolve(HERE, '../../../../../..');
const OUT = path.join(HERE, 'gates.json');
const FRAME_DIR = path.join(HERE, 'gate-frames');
const GAME_URL_PATH = '/g/hyeopgok-sasu/?pack=m2s2-u7&artprobe=1&artr4gate=1';
const TIMES_MS = [3000, 8000, 15000];
const VIEWPORTS = [
  { id: '390', width: 390, height: 844, mobile: true, deviceScaleFactor: 2 },
  { id: '1280', width: 1280, height: 800, mobile: false, deviceScaleFactor: 1 },
];
const REQUIRED_TYPES = ['king', 'soldier', 'building', 'tree'];
const THRESHOLDS = Object.freeze({
  shadowDarkness: 0.12,
  shadowRatio: 0.80,
  edgeMargin: 0.06,
  uiIntersectionArea: 0,
  minimumVisiblePerType: 1,
  minimumGroundReferencesPerSample: 3,
});

const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function resolveChrome() {
  const explicit = process.env.PUPPETEER_EXECUTABLE_PATH;
  if (explicit && fs.existsSync(explicit)) return explicit;
  const cache = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (fs.existsSync(cache)) {
    const builds = fs.readdirSync(cache).sort((a, b) => b.localeCompare(a, undefined, { numeric: true }));
    for (const build of builds) {
      const candidate = path.join(cache, build, 'chrome-mac-arm64', 'Google Chrome for Testing.app', 'Contents', 'MacOS', 'Google Chrome for Testing');
      if (fs.existsSync(candidate) && fs.statSync(candidate).isFile()) return candidate;
    }
  }
  const bundled = puppeteer.executablePath();
  if (bundled && fs.existsSync(bundled)) return bundled;
  throw new Error('Chrome executable not found; set PUPPETEER_EXECUTABLE_PATH');
}

function finite(n) { return typeof n === 'number' && Number.isFinite(n); }

function readPoint(value, viewport) {
  if (!value) return null;
  let x;
  let y;
  if (Array.isArray(value)) [x, y] = value;
  else ({ x, y } = value);
  x = Number(x); y = Number(y);
  if (!finite(x) || !finite(y)) return null;
  // Explicitly support normalized projections, while preserving their source.
  const normalized = x >= 0 && x <= 1 && y >= 0 && y <= 1 && value.normalized !== false;
  return {
    x: normalized ? x * viewport.width : x,
    y: normalized ? y * viewport.height : y,
    source: normalized ? 'normalized' : 'css-px',
  };
}

function readRect(value, viewport) {
  if (!value) return null;
  let x = Number(value.x ?? value.left);
  let y = Number(value.y ?? value.top);
  let width = Number(value.width);
  let height = Number(value.height);
  if (!finite(width) && finite(Number(value.right)) && finite(x)) width = Number(value.right) - x;
  if (!finite(height) && finite(Number(value.bottom)) && finite(y)) height = Number(value.bottom) - y;
  if (![x, y, width, height].every(finite) || width < 0 || height < 0) return null;
  const normalized = x >= 0 && x <= 1 && y >= 0 && y <= 1 && width <= 1 && height <= 1 && value.normalized !== false;
  if (normalized) {
    x *= viewport.width; width *= viewport.width;
    y *= viewport.height; height *= viewport.height;
  }
  return { x, y, width, height, source: normalized ? 'normalized' : 'css-px' };
}

function canonicalType(type) {
  const t = String(type || '').toLowerCase();
  if (t === 'king' || t === 'hero') return 'king';
  if (t === 'soldier' || t === 'unit' || t === 'troop' || t === 'red-soldier' || t === 'blue-soldier') return 'soldier';
  if (t === 'building' || t === 'tower' || t === 'barracks' || t === 'structure' || t === 'prop') return 'building';
  if (t === 'tree' || t === 'foliage') return 'tree';
  return t;
}

function normalizeSnapshot(raw, viewport) {
  const sampleSource = raw?.samples ?? raw?.shadowSamples ?? raw?.groundSamples ?? [];
  const samples = Array.isArray(sampleSource) ? sampleSource.map((sample, index) => {
    const ringSource = sample?.ring ?? sample?.surround ?? sample?.surroundPoints ?? sample?.groundPoints ?? [];
    let ring = (Array.isArray(ringSource) ? ringSource : [ringSource])
      .map(p => readPoint(p, viewport)).filter(p => pointInFrame(p, viewport));
    const ground = readPoint(sample?.ground ?? sample?.reference ?? sample?.ambient, viewport);
    if (pointInFrame(ground, viewport)) ring.push(ground);
    ring = [...new Map(ring.map(point => [`${point.x.toFixed(3)},${point.y.toFixed(3)}`, point])).values()];
    return {
      id: String(sample?.id ?? `${sample?.type ?? 'sample'}-${index + 1}`),
      type: canonicalType(sample?.type ?? sample?.category),
      declaredVisible: sample?.visible !== false,
      foot: readPoint(sample?.foot ?? sample?.shadow ?? sample?.shadowPoint ?? sample?.center, viewport),
      ring,
      explicitReferenceCount: ring.length,
      hasExplicitReference: ring.length > 0,
      hasSufficientReference: ring.length >= THRESHOLDS.minimumGroundReferencesPerSample,
    };
  }) : [];
  const front = readPoint(raw?.frontContact ?? raw?.front ?? raw?.front_contact, viewport);
  const question = readRect(raw?.questionRect ?? raw?.scrollRect ?? raw?.question ?? raw?.scroll, viewport);
  const padsSource = raw?.activePadRects ?? raw?.pads ?? raw?.padRects ?? [];
  const activePadRects = (Array.isArray(padsSource) ? padsSource : []).filter(p => p?.active !== false && p?.visible !== false).map((p, index) => ({
    id: String(p?.id ?? `pad-${index + 1}`),
    rect: readRect(p?.rect ?? p, viewport),
  }));
  return {
    version: raw?.version ?? null,
    samples,
    frontContact: front ? { ...front, declaredVisible: raw?.frontContact?.visible !== false } : null,
    questionRect: question ? { ...question, declaredVisible: (raw?.questionRect ?? raw?.scrollRect)?.visible !== false } : null,
    activePadRects,
  };
}

function pointInFrame(point, viewport) {
  return !!point && point.x >= 0 && point.x < viewport.width && point.y >= 0 && point.y < viewport.height;
}

function rectInFrame(rect, viewport) {
  return !!rect && rect.width > 0 && rect.height > 0 && rect.x < viewport.width && rect.y < viewport.height && rect.x + rect.width > 0 && rect.y + rect.height > 0;
}

function intersection(a, b) {
  if (!a || !b) return { x: 0, y: 0, width: 0, height: 0, area: null };
  const left = Math.max(a.x, b.x);
  const top = Math.max(a.y, b.y);
  const right = Math.min(a.x + a.width, b.x + b.width);
  const bottom = Math.min(a.y + a.height, b.y + b.height);
  const width = Math.max(0, right - left);
  const height = Math.max(0, bottom - top);
  return { x: left, y: top, width, height, area: width * height };
}

async function readDebugSnapshot(page) {
  return page.evaluate(() => {
    const debug = window.__HYEOPGOK_ART_DEBUG__;
    if (!debug) return { __error: 'window.__HYEOPGOK_ART_DEBUG__ is missing' };
    try {
      const value = typeof debug === 'function' ? debug()
        : typeof debug.snapshot === 'function' ? debug.snapshot()
          : typeof debug.getSnapshot === 'function' ? debug.getSnapshot()
            : debug;
      return JSON.parse(JSON.stringify(value));
    } catch (error) {
      return { __error: `debug snapshot failed: ${String(error)}` };
    }
  });
}

async function measurePixels(page, pngBase64, samples, viewport) {
  return page.evaluate(async ({ pngBase64: encoded, samples: requested, viewport: view }) => {
    const image = new Image();
    image.src = `data:image/png;base64,${encoded}`;
    await new Promise((resolve, reject) => { image.onload = resolve; image.onerror = reject; });
    const canvas = document.createElement('canvas');
    canvas.width = image.naturalWidth; canvas.height = image.naturalHeight;
    const ctx = canvas.getContext('2d', { willReadFrequently: true });
    ctx.drawImage(image, 0, 0);
    const pixels = ctx.getImageData(0, 0, canvas.width, canvas.height).data;
    const scaleX = canvas.width / view.width;
    const scaleY = canvas.height / view.height;
    const luminanceAt = (point, radiusCss = 1.5) => {
      const cx = point.x * scaleX;
      const cy = point.y * scaleY;
      const rx = Math.max(1, Math.round(radiusCss * scaleX));
      const ry = Math.max(1, Math.round(radiusCss * scaleY));
      const values = [];
      for (let yy = Math.max(0, Math.floor(cy - ry)); yy <= Math.min(canvas.height - 1, Math.ceil(cy + ry)); yy++) {
        for (let xx = Math.max(0, Math.floor(cx - rx)); xx <= Math.min(canvas.width - 1, Math.ceil(cx + rx)); xx++) {
          const dx = (xx - cx) / rx;
          const dy = (yy - cy) / ry;
          if (dx * dx + dy * dy > 1) continue;
          const at = (yy * canvas.width + xx) * 4;
          // Gamma-coded Rec.709 luma is intentional: the gate compares rendered
          // screen brightness, not radiometric light energy.
          values.push((0.2126 * pixels[at] + 0.7152 * pixels[at + 1] + 0.0722 * pixels[at + 2]) / 255);
        }
      }
      values.sort((a, b) => a - b);
      return values.length ? values[Math.floor(values.length / 2)] : null;
    };
    const measured = requested.map(sample => {
      // Reference pixels must be supplied by the Unity world projection. An
      // automatically generated ring can land on another model or on UI and
      // would turn this visual gate into a biased heuristic.
      const refs = sample.ring || [];
      const centerLuminance = luminanceAt(sample.foot);
      const surroundingSamples = refs.map(point => ({ point, luminance: luminanceAt(point) })).filter(item => item.luminance != null);
      // The upper median resists spill from the object's own shadow while still
      // describing local ground rather than a single brightest pixel.
      const sorted = surroundingSamples.map(item => item.luminance).sort((a, b) => a - b);
      const surroundingLuminance = sorted.length ? sorted[Math.min(sorted.length - 1, Math.floor(sorted.length * 0.625))] : null;
      const darkness = centerLuminance != null && surroundingLuminance != null && surroundingLuminance > 1e-6
        ? (surroundingLuminance - centerLuminance) / surroundingLuminance
        : null;
      return { ...sample, centerLuminance, surroundingLuminance, surroundingSamples, darkness };
    });
    return {
      screenshot: {
        width: canvas.width,
        height: canvas.height,
        viewportWidth: view.width,
        viewportHeight: view.height,
        scaleX,
        scaleY,
      },
      samples: measured,
    };
  }, { pngBase64, samples, viewport });
}

function evaluateSnapshot(snapshot, viewport, timeMs) {
  const ignoredSamples = snapshot.samples.filter(sample => !REQUIRED_TYPES.includes(sample.type));
  const visibleSamples = snapshot.samples.filter(sample => REQUIRED_TYPES.includes(sample.type) && sample.declaredVisible && pointInFrame(sample.foot, viewport));
  const counts = Object.fromEntries(REQUIRED_TYPES.map(type => [type, visibleSamples.filter(sample => sample.type === type).length]));
  const missingCategories = REQUIRED_TYPES.filter(type => counts[type] < THRESHOLDS.minimumVisiblePerType);
  const sampleResults = visibleSamples.map(sample => ({
    ...sample,
    darknessPercent: sample.darkness == null ? null : sample.darkness * 100,
    pass: sample.hasSufficientReference && sample.darkness != null && sample.darkness >= THRESHOLDS.shadowDarkness,
  }));
  const passingSamples = sampleResults.filter(sample => sample.pass).length;
  const ratio = sampleResults.length ? passingSamples / sampleResults.length : 0;

  const marginX = viewport.width * THRESHOLDS.edgeMargin;
  const marginY = viewport.height * THRESHOLDS.edgeMargin;
  const front = snapshot.frontContact;
  const frontPass = !!front && front.declaredVisible !== false &&
    front.x >= marginX && front.x <= viewport.width - marginX &&
    front.y >= marginY && front.y <= viewport.height - marginY;

  const question = snapshot.questionRect;
  const validQuestion = !!question && question.declaredVisible !== false && rectInFrame(question, viewport);
  const pads = snapshot.activePadRects.filter(pad => rectInFrame(pad.rect, viewport));
  const invalidPads = snapshot.activePadRects.filter(pad => !rectInFrame(pad.rect, viewport));
  const overlaps = pads.map(pad => ({ padId: pad.id, padRect: pad.rect, intersection: intersection(question, pad.rect) }));
  const totalIntersectionArea = overlaps.reduce((sum, item) => sum + (item.intersection.area ?? 0), 0);
  const uiPass = validQuestion && pads.length > 0 && invalidPads.length === 0 && overlaps.every(item => item.intersection.area === 0);

  return {
    viewport: { id: viewport.id, width: viewport.width, height: viewport.height },
    timeMs,
    debugVersion: snapshot.version,
    shadow: {
      counts,
      missingCategories,
      visibleSamples: sampleResults.length,
      passingSamples,
      ratio,
      requiredRatio: THRESHOLDS.shadowRatio,
      ignoredSamples: ignoredSamples.map(sample => ({ id: sample.id, type: sample.type })),
      samples: sampleResults,
      pass: missingCategories.length === 0 && ratio >= THRESHOLDS.shadowRatio,
    },
    frontVisibility: {
      point: front,
      margins: { x: marginX, y: marginY, percent: THRESHOLDS.edgeMargin * 100 },
      bounds: { left: marginX, top: marginY, right: viewport.width - marginX, bottom: viewport.height - marginY },
      pass: frontPass,
    },
    uiObstruction: {
      questionRect: question,
      activePadCount: pads.length,
      activePadRects: pads,
      invalidPadRects: invalidPads,
      overlaps,
      totalIntersectionArea,
      requiredIntersectionArea: THRESHOLDS.uiIntersectionArea,
      pass: uiPass,
    },
  };
}

const report = {
  schemaVersion: 1,
  generatedAt: new Date().toISOString(),
  commit: null,
  gameUrlPath: GAME_URL_PATH,
  thresholds: THRESHOLDS,
  chrome: null,
  snapshots: [],
  errors: [],
  consoleErrors: [],
  failedRequests: [],
  gates: null,
  overall: { pass: false },
};

fs.mkdirSync(HERE, { recursive: true });
if (process.env.ART_R4_KEEP_FRAMES !== '0') fs.mkdirSync(FRAME_DIR, { recursive: true });
try {
  const { execFileSync } = await import('node:child_process');
  report.commit = execFileSync('git', ['rev-parse', '--short', 'HEAD'], { cwd: ROOT, encoding: 'utf8' }).trim();
} catch { report.commit = 'unknown'; }

let browser;
let server;
try {
  const chrome = resolveChrome();
  report.chrome = chrome;
  server = await serveStatic(path.join(ROOT, 'public'));
  browser = await puppeteer.launch({ headless: true, executablePath: chrome, args: ['--no-sandbox', '--mute-audio'] });
  const page = await browser.newPage();
  page.on('pageerror', error => report.consoleErrors.push(String(error)));
  page.on('console', message => { if (message.type() === 'error') report.consoleErrors.push(message.text()); });
  page.on('requestfailed', request => report.failedRequests.push({ url: request.url(), error: request.failure()?.errorText ?? 'unknown' }));
  page.on('response', response => { if (response.status() >= 400) report.failedRequests.push({ url: response.url(), status: response.status() }); });

  for (const viewport of VIEWPORTS) {
    await page.setViewport({
      width: viewport.width,
      height: viewport.height,
      deviceScaleFactor: viewport.deviceScaleFactor,
      isMobile: viewport.mobile,
      hasTouch: true,
    });
    await page.goto(server.url + GAME_URL_PATH, { waitUntil: 'networkidle0', timeout: 90000 });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 90000 });
    await sleep(500);
    await page.evaluate(() => window.__GAME_TEST__.start());
    await page.waitForFunction(() => window.__GAME_TEST__?.getState?.().phase === 'playing', { timeout: 10000 });
    const startedAt = Date.now();
    for (const timeMs of TIMES_MS) {
      await sleep(Math.max(0, startedAt + timeMs - Date.now()));
      const raw = await readDebugSnapshot(page);
      const frameName = `${viewport.id}-${Math.round(timeMs / 1000)}s.png`;
      const png = await page.screenshot({ encoding: 'base64' });
      if (process.env.ART_R4_KEEP_FRAMES !== '0') fs.writeFileSync(path.join(FRAME_DIR, frameName), Buffer.from(png, 'base64'));
      if (raw?.__error) {
        report.errors.push(`${viewport.id}@${timeMs}ms: ${raw.__error}`);
        report.snapshots.push({ viewport: { id: viewport.id, width: viewport.width, height: viewport.height }, timeMs, error: raw.__error, raw });
        continue;
      }
      const normalized = normalizeSnapshot(raw, viewport);
      const eligible = normalized.samples.filter(sample => sample.declaredVisible && pointInFrame(sample.foot, viewport));
      let pixelSamples = [];
      let screenshot = null;
      try {
        const pixelResult = await measurePixels(page, png, eligible, viewport);
        pixelSamples = pixelResult.samples;
        screenshot = pixelResult.screenshot;
      } catch (error) {
        report.errors.push(`${viewport.id}@${timeMs}ms pixel decode: ${String(error)}`);
      }
      const measured = { ...normalized, samples: pixelSamples };
      const result = evaluateSnapshot(measured, viewport, timeMs);
      result.screenshot = screenshot;
      result.raw = raw;
      report.snapshots.push(result);
      console.log(`${viewport.id} ${Math.round(timeMs / 1000)}s shadow=${result.shadow.passingSamples}/${result.shadow.visibleSamples} (${(result.shadow.ratio * 100).toFixed(1)}%) front=${result.frontVisibility.pass} ui=${result.uiObstruction.totalIntersectionArea}`);
    }
  }
} catch (error) {
  report.errors.push(String(error?.stack || error));
} finally {
  if (browser) await browser.close().catch(() => {});
  if (server) await server.close().catch(() => {});
}

const completeSnapshots = report.snapshots.filter(snapshot => !snapshot.error);
const shadowSamples = completeSnapshots.flatMap(snapshot => snapshot.shadow?.samples ?? []);
const shadowPassing = shadowSamples.filter(sample => sample.pass).length;
const shadowRatio = shadowSamples.length ? shadowPassing / shadowSamples.length : 0;
const expectedSnapshotCount = VIEWPORTS.length * TIMES_MS.length;
const everySnapshotPresent = completeSnapshots.length === expectedSnapshotCount;
const shadowPass = everySnapshotPresent && completeSnapshots.every(snapshot => snapshot.shadow.pass) && shadowRatio >= THRESHOLDS.shadowRatio;
const frontPass = everySnapshotPresent && completeSnapshots.every(snapshot => snapshot.frontVisibility.pass);
const uiPass = everySnapshotPresent && completeSnapshots.every(snapshot => snapshot.uiObstruction.pass);
report.gates = {
  groundShadow: {
    passingSamples: shadowPassing,
    totalVisibleSamples: shadowSamples.length,
    ratio: shadowRatio,
    ratioPercent: shadowRatio * 100,
    missingCategories: completeSnapshots.flatMap(snapshot => snapshot.shadow.missingCategories.map(type => `${snapshot.viewport.id}@${snapshot.timeMs}ms:${type}`)),
    perSnapshotPass: completeSnapshots.map(snapshot => ({ viewport: snapshot.viewport.id, timeMs: snapshot.timeMs, ratio: snapshot.shadow.ratio, pass: snapshot.shadow.pass })),
    pass: shadowPass,
  },
  frontVisibility: {
    passingSnapshots: completeSnapshots.filter(snapshot => snapshot.frontVisibility.pass).length,
    totalSnapshots: expectedSnapshotCount,
    marginPercent: THRESHOLDS.edgeMargin * 100,
    pass: frontPass,
  },
  uiObstruction: {
    passingSnapshots: completeSnapshots.filter(snapshot => snapshot.uiObstruction.pass).length,
    totalSnapshots: expectedSnapshotCount,
    totalIntersectionArea: completeSnapshots.reduce((sum, snapshot) => sum + (snapshot.uiObstruction.totalIntersectionArea || 0), 0),
    pass: uiPass,
  },
};
const harnessPass = report.errors.length === 0 && report.consoleErrors.length === 0 && report.failedRequests.length === 0 && everySnapshotPresent;
report.overall = {
  pass: harnessPass && shadowPass && frontPass && uiPass,
  harnessPass,
  expectedSnapshots: expectedSnapshotCount,
  measuredSnapshots: completeSnapshots.length,
};
fs.writeFileSync(OUT, JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({ output: OUT, gates: report.gates, overall: report.overall, errors: report.errors }, null, 2));
if (!report.overall.pass) process.exitCode = 1;
