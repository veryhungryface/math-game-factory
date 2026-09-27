// 협곡 사수 v3 WebGL performance gate.
//
// Measures the built public game, after a fixed warmup, for one uninterrupted
// 15 second window at each required viewport. Missing hooks/samples are hard
// failures. This file is read-only with respect to the game and writes only
// ArtSource/validation/v3/performance.json.
//
// Usage (repository root):
//   node factory/unity-src/hyeopgok-sasu/ArtSource/validation/v3/performance.mjs

import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';
import {
  GAME,
  HERE,
  VIEWPORTS,
  attachDiagnostics,
  currentState,
  loadGame,
  openBrowserHarness,
  sleep,
  startGame,
} from './harness-lib.mjs';

const OUT = path.join(HERE, 'performance.json');
const WARMUP_MS = 4000;
const MEASURE_MS = 15000;
const EDGE_WINDOW_MS = 4000;
const FPS_GATE = 30;
const RETENTION_GATE = 0.60;
const DRAW_CALL_GATE = 150;
const GZIP_GATE_BYTES = 12_000_000;
const SCOPES = Object.freeze([
  { index: 0, id: 'Game', callback: 'HyeopgokGame.Update' },
  { index: 1, id: 'Battle', callback: 'HyeopgokBattle.Update' },
  { index: 2, id: 'LateUpdate', callback: 'HyeopgokEnvironment.LateUpdate' },
]);

const round = (value, digits = 3) => Number.isFinite(value) ? Number(value.toFixed(digits)) : null;
const sha256 = bytes => crypto.createHash('sha256').update(bytes).digest('hex');

function walkFiles(directory) {
  return fs.readdirSync(directory, { withFileTypes: true })
    .sort((left, right) => left.name.localeCompare(right.name))
    .flatMap(entry => {
      const file = path.join(directory, entry.name);
      return entry.isDirectory() ? walkFiles(file) : entry.isFile() && !entry.name.startsWith('.') ? [file] : [];
    });
}

function gameSize() {
  const files = walkFiles(GAME).map(file => {
    const bytes = fs.readFileSync(file);
    // Compatibility target: Art round 3 summarize.py. Every file is gzip-9
    // independently with mtime=0, even when a .unityweb payload is itself gzip.
    const compressed = zlib.gzipSync(bytes, { level: 9, mtime: 0 });
    return {
      file: path.relative(GAME, file),
      rawBytes: bytes.length,
      gzipBytes: compressed.length,
      sha256: sha256(bytes),
      gzipSha256: sha256(compressed),
      gzipMagic: bytes.length >= 2 && bytes[0] === 0x1f && bytes[1] === 0x8b,
    };
  });
  const rawBytes = files.reduce((sum, file) => sum + file.rawBytes, 0);
  const gzipBytes = files.reduce((sum, file) => sum + file.gzipBytes, 0);
  return {
    method: 'Each non-hidden public game file independently gzip level 9, mtime 0; includes already-gzipped .unityweb payloads (art-r3 summarize.py compatible).',
    fileCount: files.length,
    rawBytes,
    gzipBytes,
    gateBytes: GZIP_GATE_BYTES,
    remainingBytes: GZIP_GATE_BYTES - gzipBytes,
    files,
    pass: files.length > 0 && gzipBytes < GZIP_GATE_BYTES,
  };
}

function manifest(size) {
  return Object.fromEntries(size.files.map(file => [file.file, file.sha256]));
}

function percentile(sorted, fraction) {
  if (!sorted.length) return null;
  const rank = Math.max(0, Math.min(sorted.length - 1, Math.ceil(sorted.length * fraction) - 1));
  return sorted[rank];
}

function drawStatistics(values) {
  const samples = Array.isArray(values) ? values.filter(Number.isFinite) : [];
  const sorted = [...samples].sort((left, right) => left - right);
  const middle = Math.floor(sorted.length / 2);
  const median = !sorted.length ? null : sorted.length % 2 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
  return {
    samples: sorted.length,
    min: sorted.length ? sorted[0] : null,
    median,
    p95: percentile(sorted, 0.95),
    max: sorted.length ? sorted[sorted.length - 1] : null,
  };
}

function allocationStatistics(raw) {
  const arraysPresent = raw && ['counts', 'zero', 'totals', 'max', 'positive'].every(key =>
    Array.isArray(raw[key]) && raw[key].length >= 3 && raw[key].slice(0, 3).every(Number.isFinite));
  if (!arraysPresent) return { present: false, scopes: [], stableZero: false, pass: false };
  const scopes = SCOPES.map(scope => {
    const callbacks = raw.counts[scope.index];
    const zeroCallbacks = raw.zero[scope.index];
    const totalBytes = raw.totals[scope.index];
    const maxBytes = raw.max[scope.index];
    const positiveCallbacks = raw.positive[scope.index];
    const zeroAllocation = callbacks > 0 && totalBytes === 0 && maxBytes === 0 && positiveCallbacks === 0 && zeroCallbacks === callbacks;
    return {
      ...scope,
      callbacks,
      zeroCallbacks,
      totalBytes,
      maxBytes,
      positiveCallbacks,
      zeroAllocation,
      pass: zeroAllocation,
    };
  });
  return {
    present: true,
    calibration: raw.counts.length > 3 ? {
      callbacks: raw.counts[3],
      totalBytes: raw.totals[3],
      maxBytes: raw.max[3],
      positiveCallbacks: raw.positive[3],
    } : null,
    scopes,
    stableZero: scopes.every(scope => scope.zeroAllocation),
    pass: scopes.every(scope => scope.pass),
  };
}

function fpsStatistics(raw) {
  const elapsedMs = Number(raw?.elapsedMs);
  const frames = Number(raw?.frames);
  const earlyFrames = Number(raw?.earlyFrames);
  const lateFrames = Number(raw?.lateFrames);
  const overall = frames * 1000 / MEASURE_MS;
  const early = earlyFrames * 1000 / EDGE_WINDOW_MS;
  const late = lateFrames * 1000 / EDGE_WINDOW_MS;
  const retention = early > 0 ? late / early : null;
  const present = [elapsedMs, frames, earlyFrames, lateFrames].every(Number.isFinite) && elapsedMs >= MEASURE_MS && frames > 0 && earlyFrames > 0 && lateFrames > 0;
  return {
    measurementMs: MEASURE_MS,
    actualElapsedMs: round(elapsedMs),
    edgeWindowMs: EDGE_WINDOW_MS,
    frames,
    earlyFrames,
    lateFrames,
    overall: round(overall),
    early: round(early),
    late: round(late),
    retention: round(retention, 4),
    fpsGate: FPS_GATE,
    retentionGate: RETENTION_GATE,
    present,
    pass: present && overall >= FPS_GATE && early >= FPS_GATE && late >= FPS_GATE && retention >= RETENTION_GATE,
  };
}

const sizeAtStart = gameSize();
const report = {
  schemaVersion: 3,
  generatedAt: new Date().toISOString(),
  scenario: {
    game: GAME,
    pack: 'm2s2-u7',
    warmupMs: WARMUP_MS,
    measurementMs: MEASURE_MS,
    earlyLateWindowMs: EDGE_WINDOW_MS,
    viewports: VIEWPORTS.map(viewport => ({
      id: viewport.id,
      width: viewport.width,
      height: viewport.height,
      deviceScaleFactor: viewport.deviceScaleFactor,
      mobile: viewport.mobile,
    })),
  },
  thresholds: {
    fps: FPS_GATE,
    retention: RETENTION_GATE,
    drawCallsPerFrame: DRAW_CALL_GATE,
    stableAllocatedBytes: 0,
    gzipBytes: GZIP_GATE_BYTES,
  },
  chrome: null,
  runs: [],
  size: null,
  publicGameStable: false,
  errors: [],
  consoleErrors: [],
  consoleWarnings: [],
  failedRequests: [],
  gates: null,
  pass: false,
};

let browser;
let server;
try {
  const opened = await openBrowserHarness();
  ({ browser, server } = opened);
  report.chrome = opened.chrome;
  const page = await browser.newPage();
  page.setDefaultTimeout(90000);
  attachDiagnostics(page, report);

  // Install before the Unity loader. The counter wraps every render submission
  // method and the Module main-loop callbacks turn the cumulative count into a
  // per-Unity-frame ring buffer without allocating in the measured game code.
  await page.evaluateOnNewDocument(() => {
    const capacity = 32768;
    const draw = window.__HYEOPGOK_PERF_DRAW__ = {
      total: 0,
      count: 0,
      at: 0,
      calls: new Uint16Array(capacity),
      hooked: false,
      methods: Object.create(null),
    };
    const submissionNames = ['drawArrays', 'drawElements', 'drawArraysInstanced', 'drawElementsInstanced', 'drawRangeElements'];
    const patchMethod = (owner, name, label = name) => {
      if (!owner) return;
      const descriptor = Object.getOwnPropertyDescriptor(owner, name);
      if (!descriptor || typeof descriptor.value !== 'function' || descriptor.value.__hyeopgokDrawHook) return;
      const original = descriptor.value;
      const wrapped = function (...args) {
        draw.total++;
        draw.methods[label] = (draw.methods[label] || 0) + 1;
        return Reflect.apply(original, this, args);
      };
      Object.defineProperty(wrapped, '__hyeopgokDrawHook', { value: true });
      try { Object.defineProperty(owner, name, { ...descriptor, value: wrapped }); } catch {}
    };
    for (const prototype of [window.WebGLRenderingContext?.prototype, window.WebGL2RenderingContext?.prototype]) {
      for (const name of submissionNames) patchMethod(prototype, name);
      if (!prototype) continue;
      const getExtensionDescriptor = Object.getOwnPropertyDescriptor(prototype, 'getExtension');
      if (getExtensionDescriptor && typeof getExtensionDescriptor.value === 'function' && !getExtensionDescriptor.value.__hyeopgokDrawHook) {
        const originalGetExtension = getExtensionDescriptor.value;
        const wrappedGetExtension = function (...args) {
          const extension = Reflect.apply(originalGetExtension, this, args);
          if (extension && String(args[0] || '').toLowerCase() === 'angle_instanced_arrays') {
            patchMethod(extension, 'drawArraysInstancedANGLE', 'drawArraysInstancedANGLE');
            patchMethod(extension, 'drawElementsInstancedANGLE', 'drawElementsInstancedANGLE');
          }
          return extension;
        };
        Object.defineProperty(wrappedGetExtension, '__hyeopgokDrawHook', { value: true });
        try { Object.defineProperty(prototype, 'getExtension', { ...getExtensionDescriptor, value: wrappedGetExtension }); } catch {}
      }
    }
    document.addEventListener('load', event => {
      if (event.target?.tagName !== 'SCRIPT' || !String(event.target.src).includes('.loader.js')) return;
      const create = window.createUnityInstance;
      if (typeof create !== 'function' || create.__hyeopgokMainLoopHook) return;
      const wrappedCreate = function (...args) {
        return create.apply(this, args).then(instance => {
          const module = instance.Module;
          const beforeLoop = module.preMainLoop;
          const afterLoop = module.postMainLoop;
          let before = 0;
          module.preMainLoop = function (...loopArgs) {
            before = draw.total;
            return beforeLoop?.apply(this, loopArgs);
          };
          module.postMainLoop = function (...loopArgs) {
            const result = afterLoop?.apply(this, loopArgs);
            draw.calls[draw.at] = Math.min(65535, Math.max(0, draw.total - before));
            draw.at = (draw.at + 1) % draw.calls.length;
            draw.count = Math.min(draw.calls.length, draw.count + 1);
            return result;
          };
          draw.hooked = true;
          return instance;
        });
      };
      Object.defineProperty(wrappedCreate, '__hyeopgokMainLoopHook', { value: true });
      window.createUnityInstance = wrappedCreate;
    }, true);
  });

  for (const viewport of VIEWPORTS) {
    const run = {
      viewport: {
        id: viewport.id,
        width: viewport.width,
        height: viewport.height,
        deviceScaleFactor: viewport.deviceScaleFactor,
        mobile: viewport.mobile,
      },
      warmupMs: WARMUP_MS,
      renderer: null,
      stateBefore: null,
      stateAfter: null,
      fps: null,
      drawCalls: null,
      allocation: null,
      missing: [],
      pass: false,
    };
    try {
      await loadGame(page, server, viewport, `pack=m2s2-u7&artprobe=1&v3perf=${viewport.id}-${Date.now()}`);
      await startGame(page);
      await sleep(WARMUP_MS);
      run.stateBefore = await currentState(page);
      const raw = await page.evaluate(async ({ measureMs, edgeMs }) => {
        const probe = window.__HYEOPGOK_ART_PROBE__;
        if (probe) {
          for (const key of ['counts', 'zero', 'totals', 'max', 'positive']) {
            if (probe[key]?.fill) probe[key].fill(0);
          }
          if (probe.frames?.fill) probe.frames.fill(0);
          probe.at = 0;
        }
        const draw = window.__HYEOPGOK_PERF_DRAW__;
        if (draw) {
          draw.total = 0;
          draw.count = 0;
          draw.at = 0;
          draw.calls.fill(0);
          draw.methods = Object.create(null);
        }
        const canvas = document.getElementById('unity-canvas');
        const gl = canvas?.getContext('webgl2') || canvas?.getContext('webgl');
        const debugRenderer = gl?.getExtension('WEBGL_debug_renderer_info');
        const renderer = gl ? String(debugRenderer ? gl.getParameter(debugRenderer.UNMASKED_RENDERER_WEBGL) : gl.getParameter(gl.RENDERER)) : null;
        const vendor = gl ? String(debugRenderer ? gl.getParameter(debugRenderer.UNMASKED_VENDOR_WEBGL) : gl.getParameter(gl.VENDOR)) : null;
        const started = performance.now();
        const end = started + measureMs;
        const times = [];
        await new Promise(resolve => {
          const frame = timestamp => {
            if (timestamp < end) times.push(timestamp);
            if (performance.now() < end) requestAnimationFrame(frame);
            else resolve();
          };
          requestAnimationFrame(frame);
        });
        const finished = performance.now();
        const earlyFrames = times.filter(timestamp => timestamp >= started && timestamp < started + edgeMs).length;
        const lateFrames = times.filter(timestamp => timestamp >= end - edgeMs && timestamp < end).length;
        let drawCalls = null;
        if (draw) {
          if (draw.count < draw.calls.length) drawCalls = Array.from(draw.calls.subarray(0, draw.count));
          else drawCalls = Array.from(draw.calls.subarray(draw.at)).concat(Array.from(draw.calls.subarray(0, draw.at)));
        }
        const allocation = probe ? Object.fromEntries(['counts', 'zero', 'totals', 'max', 'positive'].map(key => [key, Array.from(probe[key] || [])])) : null;
        return {
          raf: { elapsedMs: finished - started, frames: times.length, earlyFrames, lateFrames },
          draw: draw ? { hooked: draw.hooked, calls: drawCalls, methods: { ...draw.methods } } : null,
          allocation,
          renderer: {
            renderer,
            vendor,
            software: /swiftshader|software|llvmpipe|basic render/i.test(renderer || ''),
            userAgent: navigator.userAgent,
            devicePixelRatio: window.devicePixelRatio,
            canvasWidth: canvas?.width ?? null,
            canvasHeight: canvas?.height ?? null,
          },
        };
      }, { measureMs: MEASURE_MS, edgeMs: EDGE_WINDOW_MS });
      run.renderer = raw.renderer;
      run.fps = fpsStatistics(raw.raf);
      const draws = drawStatistics(raw.draw?.calls);
      run.drawCalls = {
        hooked: raw.draw?.hooked === true,
        methods: raw.draw?.methods ?? null,
        ...draws,
        gate: DRAW_CALL_GATE,
        pass: raw.draw?.hooked === true && draws.samples > 0 && Number.isFinite(draws.max) && draws.max <= DRAW_CALL_GATE,
      };
      run.allocation = allocationStatistics(raw.allocation);
      run.stateAfter = await currentState(page);
      if (!run.fps.present) run.missing.push('requestAnimationFrame samples');
      if (!run.drawCalls.hooked) run.missing.push('Unity Module preMainLoop/postMainLoop draw hook');
      if (run.drawCalls.samples === 0) run.missing.push('per-frame WebGL draw samples');
      if (!run.allocation.present) run.missing.push('__HYEOPGOK_ART_PROBE__ allocation arrays');
      for (const scope of run.allocation.scopes) if (!(scope.callbacks > 0)) run.missing.push(`${scope.id} allocation callbacks`);
      if (!run.renderer?.renderer) run.missing.push('WebGL renderer identity');
      if (run.stateBefore?.phase !== 'playing' || run.stateAfter?.phase !== 'playing') run.missing.push('continuous playing state across measurement');
      run.pass = run.missing.length === 0 && run.fps.pass && run.drawCalls.pass && run.allocation.pass;
      console.log(`${viewport.id}: early=${run.fps.early} late=${run.fps.late} retention=${run.fps.retention} drawMax=${run.drawCalls.max} allocZero=${run.allocation.stableZero}`);
    } catch (error) {
      const message = `${viewport.id}: ${String(error?.stack || error)}`;
      run.missing.push('measurement completed');
      report.errors.push(message);
    }
    report.runs.push(run);
  }
} catch (error) {
  report.errors.push(String(error?.stack || error));
} finally {
  if (browser) await browser.close().catch(() => {});
  if (server) await server.close().catch(() => {});
}

report.size = gameSize();
report.publicGameStable = JSON.stringify(manifest(sizeAtStart)) === JSON.stringify(manifest(report.size));
const expectedRuns = VIEWPORTS.length;
report.gates = {
  fpsAndRetention: {
    expectedRuns,
    measuredRuns: report.runs.filter(run => run.fps?.present).length,
    runs: report.runs.map(run => ({ viewport: run.viewport.id, ...run.fps })),
    pass: report.runs.length === expectedRuns && report.runs.every(run => run.fps?.pass === true),
  },
  drawCalls: {
    gate: DRAW_CALL_GATE,
    expectedRuns,
    runs: report.runs.map(run => ({ viewport: run.viewport.id, ...run.drawCalls })),
    maximum: report.runs.length && report.runs.every(run => Number.isFinite(run.drawCalls?.max))
      ? Math.max(...report.runs.map(run => run.drawCalls.max)) : null,
    pass: report.runs.length === expectedRuns && report.runs.every(run => run.drawCalls?.pass === true),
  },
  stableAllocation: {
    requiredBytes: 0,
    expectedScopes: SCOPES.map(scope => scope.id),
    runs: report.runs.map(run => ({ viewport: run.viewport.id, ...run.allocation })),
    pass: report.runs.length === expectedRuns && report.runs.every(run => run.allocation?.pass === true),
  },
  gzipSize: {
    gzipBytes: report.size.gzipBytes,
    gateBytes: GZIP_GATE_BYTES,
    remainingBytes: report.size.remainingBytes,
    method: report.size.method,
    pass: report.size.pass,
  },
};
const diagnosticsPass = report.errors.length === 0 && report.consoleErrors.length === 0 && report.failedRequests.length === 0;
report.pass = diagnosticsPass && report.publicGameStable && report.runs.length === expectedRuns &&
  report.runs.every(run => run.pass) && Object.values(report.gates).every(gate => gate.pass);
fs.writeFileSync(OUT, JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify({
  output: OUT,
  pass: report.pass,
  publicGameStable: report.publicGameStable,
  runs: report.runs.map(run => ({
    viewport: run.viewport.id,
    fps: run.fps,
    drawCalls: run.drawCalls,
    allocationZero: run.allocation?.stableZero,
    missing: run.missing,
    pass: run.pass,
  })),
  gzipBytes: report.size.gzipBytes,
  errors: report.errors,
  consoleErrors: report.consoleErrors,
  failedRequests: report.failedRequests,
}, null, 2));
if (!report.pass) process.exitCode = 1;
