// v3.2 WebGL performance gate: three maps x mobile/desktop, each measured for
// one uninterrupted 15 second steady-state window after warmup.

import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import zlib from 'node:zlib';
import {
  GAME,
  MAPS,
  VIEWPORTS,
  V32_HERE as HERE,
  attachDiagnostics,
  currentState,
  loadGame,
  openBrowserHarness,
  startGame,
  stateContract,
} from './harness-lib.mjs';

const OUT = path.join(HERE, 'performance.json');
const WARMUP_MS = 4000;
const MEASURE_MS = 15000;
const EDGE_MS = 4000;
const FPS_GATE = 30;
const RETENTION_GATE = 0.60;
const DRAW_GATE = 150;
const GZIP_GATE = 12_000_000;
const SCOPES = Object.freeze([
  { index: 0, id: 'Game', callback: 'HyeopgokGame.Update' },
  { index: 1, id: 'Battle', callback: 'HyeopgokBattle.Update' },
  { index: 2, id: 'Environment', callback: 'HyeopgokEnvironment.LateUpdate' },
]);

const round = (value, digits = 3) => Number.isFinite(value) ? Number(value.toFixed(digits)) : null;
const sha256 = bytes => crypto.createHash('sha256').update(bytes).digest('hex');

function walk(directory) {
  return fs.readdirSync(directory, { withFileTypes: true })
    .sort((left, right) => left.name.localeCompare(right.name))
    .flatMap(entry => {
      const file = path.join(directory, entry.name);
      if (entry.isDirectory()) return walk(file);
      return entry.isFile() && !entry.name.startsWith('.') ? [file] : [];
    });
}

function gameSize() {
  const files = walk(GAME).map(file => {
    const bytes = fs.readFileSync(file);
    const gzip = zlib.gzipSync(bytes, { level: 9, mtime: 0 });
    return {
      file: path.relative(GAME, file),
      rawBytes: bytes.length,
      gzipBytes: gzip.length,
      sha256: sha256(bytes),
      gzipSha256: sha256(gzip),
    };
  });
  const rawBytes = files.reduce((sum, file) => sum + file.rawBytes, 0);
  const gzipBytes = files.reduce((sum, file) => sum + file.gzipBytes, 0);
  return {
    method: 'Every non-hidden public game file independently gzip level 9 with mtime=0; already-compressed .unityweb files remain included.',
    fileCount: files.length,
    rawBytes,
    gzipBytes,
    gateBytes: GZIP_GATE,
    remainingBytes: GZIP_GATE - gzipBytes,
    files,
    pass: files.length > 0 && gzipBytes < GZIP_GATE,
  };
}

function manifest(size) {
  return Object.fromEntries(size.files.map(file => [file.file, file.sha256]));
}

function percentile(sorted, fraction) {
  if (!sorted.length) return null;
  return sorted[Math.max(0, Math.min(sorted.length - 1, Math.ceil(sorted.length * fraction) - 1))];
}

function drawStats(raw) {
  const values = Array.isArray(raw) ? raw.filter(Number.isFinite).sort((a, b) => a - b) : [];
  const middle = Math.floor(values.length / 2);
  const median = !values.length ? null : values.length % 2 ? values[middle] : (values[middle - 1] + values[middle]) / 2;
  return {
    samples: values.length,
    min: values.length ? values[0] : null,
    median,
    p95: percentile(values, 0.95),
    max: values.length ? values[values.length - 1] : null,
  };
}

function fpsStats(raw) {
  const elapsedMs = Number(raw?.elapsedMs);
  const frames = Number(raw?.frames);
  const earlyFrames = Number(raw?.earlyFrames);
  const lateFrames = Number(raw?.lateFrames);
  const overall = frames * 1000 / MEASURE_MS;
  const early = earlyFrames * 1000 / EDGE_MS;
  const late = lateFrames * 1000 / EDGE_MS;
  const retention = early > 0 ? late / early : null;
  const present = [elapsedMs, frames, earlyFrames, lateFrames].every(Number.isFinite) &&
    elapsedMs >= MEASURE_MS && frames > 0 && earlyFrames > 0 && lateFrames > 0;
  return {
    measurementMs: MEASURE_MS,
    elapsedMs: round(elapsedMs),
    edgeWindowMs: EDGE_MS,
    frames, earlyFrames, lateFrames,
    overall: round(overall), early: round(early), late: round(late), retention: round(retention, 4),
    present,
    pass: present && overall >= FPS_GATE && early >= FPS_GATE && late >= FPS_GATE && retention >= RETENTION_GATE,
  };
}

function allocationStats(raw) {
  const present = raw && ['counts', 'zero', 'totals', 'max', 'positive'].every(key =>
    Array.isArray(raw[key]) && raw[key].length >= SCOPES.length && raw[key].slice(0, SCOPES.length).every(Number.isFinite));
  if (!present) return { present: false, scopes: [], stableZero: false, pass: false };
  const scopes = SCOPES.map(scope => {
    const callbacks = raw.counts[scope.index];
    const zeroCallbacks = raw.zero[scope.index];
    const totalBytes = raw.totals[scope.index];
    const maxBytes = raw.max[scope.index];
    const positiveCallbacks = raw.positive[scope.index];
    const zeroAllocation = callbacks > 0 && zeroCallbacks === callbacks && totalBytes === 0 && maxBytes === 0 && positiveCallbacks === 0;
    return { ...scope, callbacks, zeroCallbacks, totalBytes, maxBytes, positiveCallbacks, zeroAllocation, pass: zeroAllocation };
  });
  return { present: true, scopes, stableZero: scopes.every(scope => scope.zeroAllocation), pass: scopes.every(scope => scope.pass) };
}

const sizeBefore = gameSize();
const report = {
  schemaVersion: 4,
  generatedAt: new Date().toISOString(),
  scenario: {
    maps: MAPS,
    viewports: VIEWPORTS.map(({ id, width, height, deviceScaleFactor, mobile }) => ({ id, width, height, deviceScaleFactor, mobile })),
    warmupMs: WARMUP_MS,
    measurementMs: MEASURE_MS,
    earlyLateWindowMs: EDGE_MS,
  },
  thresholds: { fps: FPS_GATE, retention: RETENTION_GATE, drawCallsPerFrame: DRAW_GATE, stableAllocatedBytes: 0, gzipBytes: GZIP_GATE },
  chrome: null,
  runs: [],
  size: null,
  publicGameStable: false,
  gates: null,
  errors: [],
  consoleErrors: [],
  consoleWarnings: [],
  failedRequests: [],
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

  await page.evaluateOnNewDocument(() => {
    const capacity = 32768;
    const draw = window.__HYEOPGOK_V32_DRAW__ = {
      total: 0, count: 0, at: 0, calls: new Uint16Array(capacity), hooked: false, methods: Object.create(null),
    };
    const names = ['drawArrays', 'drawElements', 'drawArraysInstanced', 'drawElementsInstanced', 'drawRangeElements'];
    const patch = (owner, name, label = name) => {
      if (!owner) return;
      const descriptor = Object.getOwnPropertyDescriptor(owner, name);
      if (!descriptor || typeof descriptor.value !== 'function' || descriptor.value.__hyeopgokV32DrawHook) return;
      const original = descriptor.value;
      const wrapped = function (...args) {
        draw.total++;
        draw.methods[label] = (draw.methods[label] || 0) + 1;
        return Reflect.apply(original, this, args);
      };
      Object.defineProperty(wrapped, '__hyeopgokV32DrawHook', { value: true });
      try { Object.defineProperty(owner, name, { ...descriptor, value: wrapped }); } catch {}
    };
    for (const prototype of [window.WebGLRenderingContext?.prototype, window.WebGL2RenderingContext?.prototype]) {
      for (const name of names) patch(prototype, name);
      if (!prototype) continue;
      const descriptor = Object.getOwnPropertyDescriptor(prototype, 'getExtension');
      if (!descriptor || typeof descriptor.value !== 'function' || descriptor.value.__hyeopgokV32DrawHook) continue;
      const original = descriptor.value;
      const wrapped = function (...args) {
        const extension = Reflect.apply(original, this, args);
        if (extension && String(args[0] || '').toLowerCase() === 'angle_instanced_arrays') {
          patch(extension, 'drawArraysInstancedANGLE', 'drawArraysInstancedANGLE');
          patch(extension, 'drawElementsInstancedANGLE', 'drawElementsInstancedANGLE');
        }
        return extension;
      };
      Object.defineProperty(wrapped, '__hyeopgokV32DrawHook', { value: true });
      try { Object.defineProperty(prototype, 'getExtension', { ...descriptor, value: wrapped }); } catch {}
    }
    document.addEventListener('load', event => {
      if (event.target?.tagName !== 'SCRIPT' || !String(event.target.src).includes('.loader.js')) return;
      const create = window.createUnityInstance;
      if (typeof create !== 'function' || create.__hyeopgokV32MainLoopHook) return;
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
      Object.defineProperty(wrappedCreate, '__hyeopgokV32MainLoopHook', { value: true });
      window.createUnityInstance = wrappedCreate;
    }, true);
  });

  for (const map of MAPS) {
    for (const viewport of VIEWPORTS) {
      const run = {
        map,
        viewport: { id: viewport.id, width: viewport.width, height: viewport.height, deviceScaleFactor: viewport.deviceScaleFactor },
        renderer: null,
        stateBefore: null,
        stateAfter: null,
        stateContract: null,
        fps: null,
        drawCalls: null,
        allocation: null,
        missing: [],
        pass: false,
      };
      try {
        await loadGame(page, server, viewport, `pack=m2s2-u7&artprobe=1&v32map=${map.id}&v32perf=${map.key}-${viewport.id}-${Date.now()}`);
        await startGame(page);
        await new Promise(resolve => setTimeout(resolve, WARMUP_MS));
        run.stateBefore = await currentState(page);
        run.stateContract = stateContract(run.stateBefore);
        const raw = await page.evaluate(async ({ measureMs, edgeMs }) => {
          const probe = window.__HYEOPGOK_ART_PROBE__;
          if (probe) {
            for (const key of ['counts', 'zero', 'totals', 'max', 'positive']) if (probe[key]?.fill) probe[key].fill(0);
            if (probe.frames?.fill) probe.frames.fill(0);
            probe.at = 0;
          }
          const draw = window.__HYEOPGOK_V32_DRAW__;
          if (draw) {
            draw.total = 0; draw.count = 0; draw.at = 0; draw.calls.fill(0); draw.methods = Object.create(null);
          }
          const canvas = document.getElementById('unity-canvas');
          const gl = canvas?.getContext('webgl2') || canvas?.getContext('webgl');
          const info = gl?.getExtension('WEBGL_debug_renderer_info');
          const renderer = gl ? String(info ? gl.getParameter(info.UNMASKED_RENDERER_WEBGL) : gl.getParameter(gl.RENDERER)) : null;
          const vendor = gl ? String(info ? gl.getParameter(info.UNMASKED_VENDOR_WEBGL) : gl.getParameter(gl.VENDOR)) : null;
          const started = performance.now();
          const end = started + measureMs;
          const times = [];
          await new Promise(resolve => {
            const frame = timestamp => {
              if (timestamp < end) times.push(timestamp);
              if (performance.now() < end) requestAnimationFrame(frame); else resolve();
            };
            requestAnimationFrame(frame);
          });
          const finished = performance.now();
          const earlyFrames = times.filter(time => time >= started && time < started + edgeMs).length;
          const lateFrames = times.filter(time => time >= end - edgeMs && time < end).length;
          let calls = null;
          if (draw) calls = draw.count < draw.calls.length
            ? Array.from(draw.calls.subarray(0, draw.count))
            : Array.from(draw.calls.subarray(draw.at)).concat(Array.from(draw.calls.subarray(0, draw.at)));
          const allocation = probe ? Object.fromEntries(['counts', 'zero', 'totals', 'max', 'positive'].map(key => [key, Array.from(probe[key] || [])])) : null;
          return {
            raf: { elapsedMs: finished - started, frames: times.length, earlyFrames, lateFrames },
            draw: draw ? { hooked: draw.hooked, calls, methods: { ...draw.methods } } : null,
            allocation,
            renderer: { renderer, vendor, software: /swiftshader|software|llvmpipe|basic render/i.test(renderer || ''), canvasWidth: canvas?.width ?? null, canvasHeight: canvas?.height ?? null },
          };
        }, { measureMs: MEASURE_MS, edgeMs: EDGE_MS });
        run.renderer = raw.renderer;
        run.fps = fpsStats(raw.raf);
        const draws = drawStats(raw.draw?.calls);
        run.drawCalls = { hooked: raw.draw?.hooked === true, methods: raw.draw?.methods ?? null, ...draws, gate: DRAW_GATE,
          pass: raw.draw?.hooked === true && draws.samples > 0 && Number.isFinite(draws.max) && draws.max <= DRAW_GATE };
        run.allocation = allocationStats(raw.allocation);
        run.stateAfter = await currentState(page);
        if (!run.stateContract.valid) run.missing.push(`v32 state fields: ${run.stateContract.missing.join(',')}`);
        if (Number(run.stateBefore?.map) !== map.id || Number(run.stateAfter?.map) !== map.id) run.missing.push('requested map fixture did not stay active');
        if (run.stateBefore?.phase !== 'playing' || run.stateAfter?.phase !== 'playing') run.missing.push('continuous playing state');
        if (!run.fps.present) run.missing.push('requestAnimationFrame samples');
        if (!run.drawCalls.hooked || run.drawCalls.samples === 0) run.missing.push('per-frame WebGL draw samples');
        if (!run.allocation.present) run.missing.push('allocation probe arrays');
        for (const scope of run.allocation.scopes) if (!(scope.callbacks > 0)) run.missing.push(`${scope.id} allocation callbacks`);
        if (!run.renderer?.renderer) run.missing.push('renderer identity');
        run.pass = run.missing.length === 0 && run.fps.pass && run.drawCalls.pass && run.allocation.pass;
        console.log(`${map.key}/${viewport.id}: fps=${run.fps.overall}, retention=${run.fps.retention}, draw=${run.drawCalls.max}, alloc0=${run.allocation.stableZero}`);
      } catch (error) {
        run.missing.push('measurement completed');
        report.errors.push(`${map.key}/${viewport.id}: ${String(error?.stack || error)}`);
      }
      report.runs.push(run);
    }
  }
} catch (error) {
  report.errors.push(String(error?.stack || error));
} finally {
  if (browser) await browser.close().catch(() => {});
  if (server) await server.close().catch(() => {});
}

report.size = gameSize();
report.publicGameStable = JSON.stringify(manifest(sizeBefore)) === JSON.stringify(manifest(report.size));
const expectedRuns = MAPS.length * VIEWPORTS.length;
report.gates = {
  fpsAndRetention: {
    expectedRuns,
    measuredRuns: report.runs.filter(run => run.fps?.present).length,
    runs: report.runs.map(run => ({ map: run.map.key, viewport: run.viewport.id, ...run.fps })),
    pass: report.runs.length === expectedRuns && report.runs.every(run => run.fps?.pass === true),
  },
  drawCalls: {
    gate: DRAW_GATE,
    runs: report.runs.map(run => ({ map: run.map.key, viewport: run.viewport.id, ...run.drawCalls })),
    maximum: report.runs.length && report.runs.every(run => Number.isFinite(run.drawCalls?.max)) ? Math.max(...report.runs.map(run => run.drawCalls.max)) : null,
    pass: report.runs.length === expectedRuns && report.runs.every(run => run.drawCalls?.pass === true),
  },
  stableAllocation: {
    requiredBytes: 0,
    expectedScopes: SCOPES.map(scope => scope.id),
    runs: report.runs.map(run => ({ map: run.map.key, viewport: run.viewport.id, ...run.allocation })),
    pass: report.runs.length === expectedRuns && report.runs.every(run => run.allocation?.pass === true),
  },
  gzipSize: {
    gzipBytes: report.size.gzipBytes,
    gateBytes: GZIP_GATE,
    remainingBytes: report.size.remainingBytes,
    method: report.size.method,
    pass: report.size.pass,
  },
};
const diagnosticsPass = report.errors.length === 0 && report.consoleErrors.length === 0 && report.failedRequests.length === 0;
report.pass = diagnosticsPass && report.publicGameStable && report.runs.length === expectedRuns &&
  report.runs.every(run => run.pass) && Object.values(report.gates).every(gate => gate.pass);
fs.writeFileSync(OUT, `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify({
  output: OUT,
  pass: report.pass,
  publicGameStable: report.publicGameStable,
  runs: report.runs.map(run => ({ map: run.map.key, viewport: run.viewport.id, fps: run.fps?.overall, retention: run.fps?.retention, drawMax: run.drawCalls?.max, allocationZero: run.allocation?.stableZero, missing: run.missing, pass: run.pass })),
  gzipBytes: report.size.gzipBytes,
  errors: report.errors,
}, null, 2));
if (!report.pass) process.exitCode = 1;

