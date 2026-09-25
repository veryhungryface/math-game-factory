#!/usr/bin/env node
/**
 * Trusted-browser B3 audit for public/g/star-out.
 *
 *   B3_RUNS=200 B3_CONCURRENCY=8 node logs/star-out-build/bots-browser.mjs
 *   node logs/star-out-build/bots-browser.mjs --self-check
 *
 * Answer actions are sent only through Puppeteer's page.mouse API. The harness
 * never invokes game answer hooks, synthesizes DOM events, or reads the game's
 * private S/geometry/requiredPairs values. Vertex and soldering-iron locations
 * are recovered from the pixels actually painted on the game canvas.
 *
 * The fixed square and the explicitly guided first discard are onboarding.
 * They are completed with trusted mouse drags and counted separately from each
 * bot's policy inputs. The game itself excludes those plates from firstAttempts.
 */

import crypto from 'node:crypto';
import fs from 'node:fs';
import http from 'node:http';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../..');
const PUBLIC_DIR = path.join(ROOT, 'public');
const GAME_FILE = path.join(PUBLIC_DIR, 'g', 'star-out', 'index.html');
const OUT_FILE = path.join(HERE, 'bots-browser.json');
const POLICIES = ['repeat', 'cycle', 'random', 'idle'];
const RUNS = positiveInt(process.env.B3_RUNS, 200);
const CONCURRENCY = positiveInt(process.env.B3_CONCURRENCY, 4);
const GAME_TIMEOUT_MS = positiveInt(process.env.B3_GAME_TIMEOUT_MS, 120_000);
const BASE_SEED = 0x91e10da5;
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

function positiveInt(value, fallback) {
  const n = Number(value);
  return Number.isInteger(n) && n > 0 ? n : fallback;
}

function sha256(file) {
  return crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
}

function seeded(seed) {
  let a = seed >>> 0;
  return () => {
    a += 0x6d2b79f5;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

function shuffle(values, random) {
  const out = values.slice();
  for (let i = out.length - 1; i > 0; i--) {
    const j = Math.floor(random() * (i + 1));
    [out[i], out[j]] = [out[j], out[i]];
  }
  return out;
}

function choose(n, k) {
  if (!Number.isInteger(n) || !Number.isInteger(k) || k < 0 || k > n) return 0;
  let value = 1;
  for (let i = 1; i <= k; i++) value = (value * (n - k + i)) / i;
  return value;
}

const DISCARD_KINDS = new Set(['not_polygon', 'not_regular', 'triangle_discard']);
function chanceFor(policy, trial) {
  if (policy !== 'random' || DISCARD_KINDS.has(trial.kind)) return 0;
  const pre = Number(trial.pre || 0);
  const available = Number(trial.n) - 1 - pre;
  const needed = Number(trial.n) - 3 - pre;
  const denominator = choose(available, needed);
  return denominator > 0 ? 1 / denominator : 0;
}

function resolveChrome() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache', 'puppeteer', 'chrome');
  if (!fs.existsSync(base)) return undefined;
  const builds = fs
    .readdirSync(base)
    .filter((entry) => {
      try {
        return fs.statSync(path.join(base, entry)).isDirectory();
      } catch {
        return false;
      }
    })
    .sort()
    .reverse();
  for (const build of builds) {
    for (const relative of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome',
    ]) {
      const candidate = path.join(base, build, relative);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
  return undefined;
}

const MIME = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.png': 'image/png',
  '.svg': 'image/svg+xml',
  '.woff2': 'font/woff2',
};

/** A local server whose listen errors reject instead of crashing unhandled. */
function serve(rootDir) {
  const absoluteRoot = path.resolve(rootDir);
  const server = http.createServer((request, response) => {
    let pathname = '/';
    try {
      pathname = decodeURIComponent(new URL(request.url || '/', 'http://127.0.0.1').pathname);
    } catch {
      response.writeHead(400).end('bad request');
      return;
    }
    if (pathname === '/favicon.ico') {
      response.writeHead(204).end();
      return;
    }
    let file = path.resolve(absoluteRoot, `.${pathname}`);
    if (file !== absoluteRoot && !file.startsWith(`${absoluteRoot}${path.sep}`)) {
      response.writeHead(403).end('forbidden');
      return;
    }
    try {
      if (fs.existsSync(file) && fs.statSync(file).isDirectory()) file = path.join(file, 'index.html');
      if (!fs.existsSync(file) || !fs.statSync(file).isFile()) {
        response.writeHead(404).end('not found');
        return;
      }
      response.writeHead(200, {
        'content-type': MIME[path.extname(file).toLowerCase()] || 'application/octet-stream',
        'cache-control': 'no-store',
      });
      fs.createReadStream(file).pipe(response);
    } catch (error) {
      response.writeHead(500).end(String(error));
    }
  });
  return new Promise((resolve, reject) => {
    const onError = (error) => reject(error);
    server.once('error', onError);
    server.listen(0, '127.0.0.1', () => {
      server.off('error', onError);
      const address = server.address();
      resolve({
        url: `http://127.0.0.1:${address.port}`,
        close: () => new Promise((done) => server.close(done)),
      });
    });
  });
}

function gameSeed(index) {
  return (BASE_SEED + Math.imul(index + 1, 2654435761)) >>> 0;
}

async function installPreload(page) {
  await page.evaluateOnNewDocument(() => {
    const raw = new URLSearchParams(location.search).get('b3seed');
    let state = Number(raw || 1) >>> 0;
    Math.random = () => {
      state = (Math.imul(state, 1664525) + 1013904223) >>> 0;
      return state / 4294967296;
    };
    // Every run receives the same first-play contract. This is test isolation,
    // not an answer shortcut; the tutorial is still completed by real input.
    try {
      localStorage.removeItem('onboardingSeen:star-out');
    } catch {}

    window.__B3_INPUT_MODE__ = 'none';
    window.__B3_POINTERS__ = {
      trusted: 0,
      untrusted: 0,
      byMode: {
        start: { down: 0, move: 0, up: 0 },
        training: { down: 0, move: 0, up: 0 },
        policy: { down: 0, move: 0, up: 0 },
        none: { down: 0, move: 0, up: 0 },
      },
    };
    for (const type of ['pointerdown', 'pointermove', 'pointerup']) {
      addEventListener(
        type,
        (event) => {
          const counts = window.__B3_POINTERS__;
          if (!event.isTrusted) {
            counts.untrusted++;
            return;
          }
          counts.trusted++;
          const mode = Object.hasOwn(counts.byMode, window.__B3_INPUT_MODE__)
            ? window.__B3_INPUT_MODE__
            : 'none';
          const key = type.slice('pointer'.length);
          counts.byMode[mode][key]++;
        },
        true
      );
    }
  });
}

async function publicState(page) {
  return page.evaluate(() => {
    const source = window.__GAME_TEST__?.getState?.() || {};
    const plateToken = source.plateId == null ? null : String(source.plateId);
    const tokenParts = plateToken?.split('-') || [];
    const tokenVertexCount = Number(tokenParts[1]);
    return {
      phase: source.phase,
      score: source.score,
      lives: source.lives,
      solved: source.solved,
      time: source.time,
      tutorial: !!source.tutorial,
      plateToken,
      // This comes from the public test-state plate token and reveals only the
      // number of visibly painted corners, never the plate kind or answer.
      vertexCount: plateToken === 'practice-square' ? 4 : Number.isInteger(tokenVertexCount) ? tokenVertexCount : null,
      lineCount: Number(source.lines || 0),
      historyCount: Array.isArray(source.firstAttempts) ? source.firstAttempts.length : 0,
    };
  });
}

async function finalState(page) {
  return page.evaluate(() => {
    const source = window.__GAME_TEST__?.getState?.() || {};
    return {
      phase: source.phase,
      score: source.score,
      lives: source.lives,
      solved: source.solved,
      time: source.time,
      firstAttempts: Array.isArray(source.firstAttempts)
        ? source.firstAttempts.map((trial) => ({
            id: String(trial.id),
            kind: String(trial.kind),
            n: Number(trial.n),
            pre: Number(trial.pre || 0),
            firstCorrect: !!trial.firstCorrect,
            completed: !!trial.completed,
          }))
        : [],
    };
  });
}

async function pointerCounts(page) {
  return page.evaluate(() => JSON.parse(JSON.stringify(window.__B3_POINTERS__)));
}

async function setInputMode(page, mode) {
  await page.evaluate((next) => {
    window.__B3_INPUT_MODE__ = next;
  }, mode);
}

async function mouseTap(page, point, mode) {
  await setInputMode(page, mode);
  await page.mouse.move(point.x, point.y);
  await page.mouse.down();
  await sleep(45);
  await page.mouse.up();
  await setInputMode(page, 'none');
}

async function mouseDrag(page, from, to, mode) {
  await setInputMode(page, mode);
  await page.mouse.move(from.x, from.y);
  await page.mouse.down();
  await page.mouse.move((from.x + to.x) / 2, (from.y + to.y) / 2, { steps: 5 });
  await page.mouse.move(to.x, to.y, { steps: 5 });
  await sleep(45);
  await page.mouse.up();
  await setInputMode(page, 'none');
}

async function clickVisible(page, selector, mode) {
  const point = await page.$eval(selector, (element) => {
    const rect = element.getBoundingClientRect();
    if (!rect.width || !rect.height) throw new Error(`${element.id || element.tagName} is not visible`);
    return { x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 };
  });
  await mouseTap(page, point, mode);
}

/**
 * Recover visible vertex centres and the marked (soldering-iron) vertex from
 * canvas pixels only. No game closure/private state is touched here.
 */
async function observePlatePixels(page, expectedVertexCount) {
  return page.evaluate((expectedCount) => {
    const canvas = document.querySelector('canvas');
    if (!canvas) return { error: 'canvas missing', vertices: [] };
    const rect = canvas.getBoundingClientRect();
    const width = Math.max(1, Math.round(rect.width));
    const height = Math.max(1, Math.round(rect.height));
    const probe = document.createElement('canvas');
    probe.width = width;
    probe.height = height;
    const context = probe.getContext('2d', { willReadFrequently: true });
    context.drawImage(canvas, 0, 0, width, height);
    const pixels = context.getImageData(0, 0, width, height).data;

    const rgb = (x, y) => {
      const px = Math.max(0, Math.min(width - 1, Math.round(x)));
      const py = Math.max(0, Math.min(height - 1, Math.round(y)));
      const offset = (py * width + px) * 4;
      return [pixels[offset], pixels[offset + 1], pixels[offset + 2]];
    };
    const paper = (color) => color[0] > 232 && color[1] > 229 && color[2] > 216;
    const lead = (color) => color[0] < 88 && color[1] < 98 && color[2] < 118;
    const candidates = [];
    const spokes = 20;
    // The plate travels the full height of the rack, so scan the whole playfield.
    const yStart = Math.max(110, Math.floor(height * 0.12));
    const yEnd = Math.max(yStart + 1, height - 130);

    for (let y = yStart; y <= yEnd; y += 3) {
      for (let x = 16; x <= width - 16; x += 3) {
        let whiteRing = 0;
        let darkRim = 0;
        for (let k = 0; k < spokes; k++) {
          const angle = (k * Math.PI * 2) / spokes;
          const cosine = Math.cos(angle);
          const sine = Math.sin(angle);
          if (paper(rgb(x + cosine * 15, y + sine * 15)) || paper(rgb(x + cosine * 18, y + sine * 18))) whiteRing++;
          if (
            lead(rgb(x + cosine * 22, y + sine * 22)) ||
            lead(rgb(x + cosine * 24, y + sine * 24)) ||
            lead(rgb(x + cosine * 26, y + sine * 26))
          ) darkRim++;
        }
        if (whiteRing >= 11 && darkRim >= 7) {
          candidates.push({ x, y, score: whiteRing * 2 + darkRim });
        }
      }
    }

    candidates.sort((a, b) => b.score - a.score);
    const vertices = [];
    for (const candidate of candidates) {
      if (vertices.every((point) => Math.hypot(point.x - candidate.x, point.y - candidate.y) > 42)) {
        vertices.push(candidate);
      }
      if (vertices.length >= (expectedCount || 6)) break;
    }

    // The soldering iron is drawn rotated toward the plate centre, so its screen
    // angle changes with every vertex. Probe the band 25-34px from the vertex
    // along the inward direction (with a small skew sweep) instead of a fixed angle.
    const hull = {
      x: vertices.reduce((sum, point) => sum + point.x, 0) / (vertices.length || 1),
      y: vertices.reduce((sum, point) => sum + point.y, 0) / (vertices.length || 1),
    };
    const ironScore = (point) => {
      const inward = Math.atan2(hull.y - point.y, hull.x - point.x);
      let best = 0;
      for (const skew of [-0.34, -0.24, -0.14, -0.04, 0.06]) {
        const angle = inward + skew;
        const cosine = Math.cos(angle);
        const sine = Math.sin(angle);
        let dark = 0;
        let total = 0;
        for (let d = 25; d <= 34; d += 1.5) {
          for (const off of [-4, 0, 4]) {
            const x = point.x + cosine * d - sine * off;
            const y = point.y + sine * d + cosine * off;
            total++;
            if (lead(rgb(x, y))) dark++;
          }
        }
        if (total && dark / total > best) best = dark / total;
      }
      return best;
    };

    for (const vertex of vertices) vertex.iron = ironScore(vertex);
    const marked = vertices.reduce((best, point) => (!best || point.iron > best.iron ? point : best), null);
    if (
      vertices.length < 3 ||
      (expectedCount && vertices.length !== expectedCount) ||
      !marked ||
      marked.iron < 0.42
    ) {
      return {
        error: `visual vertex detection failed: count=${vertices.length}, marked=${marked?.iron ?? 'none'}`,
        vertices,
      };
    }

    const center = {
      x: vertices.reduce((sum, point) => sum + point.x, 0) / vertices.length,
      y: vertices.reduce((sum, point) => sum + point.y, 0) / vertices.length,
    };
    const ordered = vertices
      .slice()
      .sort((a, b) => Math.atan2(a.y - center.y, a.x - center.x) - Math.atan2(b.y - center.y, b.x - center.x));
    const markedIndex = ordered.indexOf(marked);
    return {
      canvas: { left: rect.left, top: rect.top, right: rect.right, bottom: rect.bottom },
      center: { x: rect.left + center.x, y: rect.top + center.y },
      vertices: ordered.map((point) => ({
        x: rect.left + point.x,
        y: rect.top + point.y,
        iron: point.iron,
      })),
      markedIndex,
    };
  }, expectedVertexCount);
}

function endpointsFrom(observation) {
  const { vertices, markedIndex } = observation;
  const marked = vertices[markedIndex];
  const endpoints = [];
  for (let offset = 1; offset < vertices.length; offset++) {
    endpoints.push(vertices[(markedIndex + offset) % vertices.length]);
  }
  return { marked, endpoints };
}

async function waitFor(page, predicate, label, timeout = 6000) {
  const deadline = Date.now() + timeout;
  let latest = null;
  while (Date.now() < deadline) {
    latest = await publicState(page);
    if (predicate(latest)) return latest;
    await sleep(70);
  }
  throw new Error(`timeout waiting for ${label}: ${JSON.stringify(latest)}`);
}

async function completeTraining(page, initialState) {
  const token = initialState.plateToken;
  for (let attempt = 0; attempt < 4; attempt++) {
    const observation = await observePlatePixels(page, initialState.vertexCount);
    if (observation.error) {
      await sleep(100);
      continue;
    }
    if (observation.vertices.length === 4) {
      const { markedIndex, vertices } = observation;
      await mouseDrag(page, vertices[markedIndex], vertices[(markedIndex + 2) % 4], 'training');
    } else {
      await mouseDrag(
        page,
        observation.center,
        { x: observation.canvas.right - 8, y: observation.center.y },
        'training'
      );
    }
    try {
      return await waitFor(
        page,
        (state) => state.phase !== 'playing' || state.plateToken !== token,
        'guided plate to leave',
        3500
      );
    } catch {
      // Pixel location may have moved between observation and pointer-up; retry visibly.
    }
  }
  throw new Error(`could not complete visible training plate ${token} with trusted mouse input`);
}

async function performPolicyInput(page, policy, memory, random, vertexCount) {
  const observation = await observePlatePixels(page, vertexCount);
  if (observation.error) throw new Error(observation.error);
  const { marked, endpoints } = endpointsFrom(observation);
  if (!marked || endpoints.length < 2) throw new Error('not enough visible endpoints');

  let endpoint = null;
  if (policy === 'repeat') {
    endpoint = endpoints[0];
  } else if (policy === 'cycle') {
    endpoint = endpoints[memory.cursor % endpoints.length];
    memory.cursor++;
  } else if (policy === 'random') {
    if (!memory.order.length) memory.order = shuffle([...endpoints.keys()], random);
    endpoint = endpoints[memory.order.shift()];
  } else {
    throw new Error(`unexpected active policy: ${policy}`);
  }
  await mouseDrag(page, marked, endpoint, 'policy');
}

async function runGame(page, baseUrl, policy, index, pageErrors) {
  const seed = gameSeed(index);
  const random = seeded(seed ^ 0x5bd1e995);
  const startedAt = Date.now();
  pageErrors.length = 0;
  await page.goto(`${baseUrl}/g/star-out/?b3seed=${seed}&b3policy=${policy}&b3run=${index}`, {
    waitUntil: 'networkidle0',
    timeout: 45_000,
  });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 20_000 });
  await clickVisible(page, '#begin', 'start');
  let state = await waitFor(page, (value) => value.phase === 'playing', 'visible start button');
  let plateToken = null;
  let historyAtPlate = 0;
  let memory = { cursor: 0, order: [] };

  while (state.phase === 'playing') {
    if (Date.now() - startedAt > GAME_TIMEOUT_MS) {
      throw new Error(`game timeout after ${GAME_TIMEOUT_MS}ms: ${JSON.stringify(state)}`);
    }
    if (state.tutorial) {
      state = await completeTraining(page, state);
      plateToken = null;
      continue;
    }
    if (state.plateToken !== plateToken) {
      plateToken = state.plateToken;
      historyAtPlate = state.historyCount;
      memory = { cursor: 0, order: [] };
    }
    if (state.historyCount > historyAtPlate) {
      await sleep(80);
      state = await publicState(page);
      continue;
    }
    if (policy === 'idle') {
      await sleep(100);
    } else {
      await performPolicyInput(page, policy, memory, random, state.vertexCount);
      await sleep(65);
    }
    state = await publicState(page);
  }

  const finished = await finalState(page);
  const pointers = await pointerCounts(page);
  const policyPointers = pointers.byMode.policy;
  if (pointers.untrusted !== 0) throw new Error(`untrusted pointer event observed: ${pointers.untrusted}`);
  if (policy === 'idle' && (policyPointers.down !== 0 || policyPointers.up !== 0)) {
    throw new Error(`idle policy emitted gameplay input: ${JSON.stringify(policyPointers)}`);
  }
  if (policy !== 'idle' && (policyPointers.down < 1 || policyPointers.up < 1)) {
    throw new Error(`policy lacked trusted pointer activity: ${JSON.stringify(policyPointers)}`);
  }
  if (!finished.firstAttempts.length) throw new Error('terminal game recorded no ordinary first attempts');
  if (pageErrors.length) throw new Error(`browser errors: ${pageErrors.join(' | ')}`);

  return {
    index,
    seed,
    phase: finished.phase,
    solved: finished.solved,
    lives: finished.lives,
    score: finished.score,
    remainingTime: finished.time,
    firstTrials: finished.firstAttempts.length,
    firstCorrect: finished.firstAttempts.filter((trial) => trial.firstCorrect).length,
    firstAttempts: finished.firstAttempts,
    trustedPointers: pointers,
    durationMs: Date.now() - startedAt,
  };
}

function summarizePolicy(policy, games) {
  const trials = games.flatMap((game) => game.firstAttempts);
  const correct = trials.filter((trial) => trial.firstCorrect).length;
  const expected = trials.reduce((sum, trial) => sum + chanceFor(policy, trial), 0);
  const variance = trials.reduce((sum, trial) => {
    const chance = chanceFor(policy, trial);
    return sum + chance * (1 - chance);
  }, 0);
  const firstAttemptRate = trials.length ? correct / trials.length : null;
  const chanceRate = trials.length ? expected / trials.length : null;
  const zScore = variance > 0 ? (correct - expected) / Math.sqrt(variance) : correct > 0 ? Infinity : 0;
  const significantUplift = zScore > 1.644854;
  const completions = games.filter((game) => game.phase === 'clear').length;
  const idleProgressZero = policy !== 'idle' || games.every((game) => game.solved === 0);
  return {
    policy,
    games: games.length,
    trials: trials.length,
    firstCorrect: correct,
    firstAttemptRate,
    expectedFirstCorrect: expected,
    chanceRate,
    rawAtOrBelowChance: firstAttemptRate != null && chanceRate != null && firstAttemptRate <= chanceRate,
    zScore,
    significantUpliftOneSided5pct: significantUplift,
    completions,
    completionRate: games.length ? completions / games.length : null,
    idleProgressZero,
    pass: !significantUplift && idleProgressZero,
    gamesDetail: games,
  };
}

const report = {
  slug: 'star-out',
  status: 'starting',
  startedAt: new Date().toISOString(),
  sourceSha256: fs.existsSync(GAME_FILE) ? sha256(GAME_FILE) : null,
  harnessSha256: sha256(fileURLToPath(import.meta.url)),
  settings: {
    runsPerPolicy: RUNS,
    concurrency: CONCURRENCY,
    gameTimeoutMs: GAME_TIMEOUT_MS,
    viewport: { width: 390, height: 844, deviceScaleFactor: 1 },
    policies: POLICIES,
    seedSchedule: '(0x91e10da5 + imul(gameIndex + 1, 2654435761)) >>> 0; identical across policies',
  },
  method:
    'Chromium page.mouse creates trusted pointer events. Policies see only canvas-derived visible vertices/marked soldering iron plus public phase/token/count fields. Fixed-square and guided-discard onboarding use separately counted trusted training drags and are excluded. No answer hooks or DOM event synthesis are used.',
  chanceDefinitions: {
    repeat: '0: marked point to the same clockwise-neighbour endpoint on every attempt; it is always a side.',
    cycle: '0: clockwise +1,+2,+3,+4 sequence; the first submitted segment is always the adjacent side.',
    random:
      'Seeded uniform permutation of visible endpoints other than the marked point. Drawable plate chance is 1/C(n-1-pre,n-3-pre); discard plates receive a vertex stroke, so chance is 0. Reached trials are weighted individually.',
    idle: '0: no policy pointer event. Onboarding controller events are separately tagged and excluded; ordinary plates expire without progress.',
  },
  policies: [],
  errors: [],
};

function writeReport() {
  fs.writeFileSync(OUT_FILE, `${JSON.stringify(report, null, 2)}\n`);
}

function selfCheck() {
  const source = fs.readFileSync(fileURLToPath(import.meta.url), 'utf8');
  const forbiddenCalls = [
    /__GAME_TEST__\s*\.\s*answerCorrect\s*\(/,
    /__GAME_TEST__\s*\.\s*answerWrong\s*\(/,
    /__GAME_TEST__\s*\.\s*simulateInput\s*\(/,
    /\.dispatchEvent\s*\(/,
  ];
  const failures = forbiddenCalls.filter((pattern) => pattern.test(source)).map(String);
  if (POLICIES.length !== 4 || new Set(POLICIES).size !== 4) failures.push('four unique policies required');
  if (Math.abs(chanceFor('random', { kind: 'polygon_diag', n: 4, pre: 0 }) - 1 / 3) > 1e-12)
    failures.push('square chance');
  if (Math.abs(chanceFor('random', { kind: 'polygon_diag', n: 5, pre: 0 }) - 1 / 6) > 1e-12)
    failures.push('pentagon chance');
  if (Math.abs(chanceFor('random', { kind: 'polygon_diag', n: 6, pre: 0 }) - 1 / 10) > 1e-12)
    failures.push('hexagon chance');
  if (Math.abs(chanceFor('random', { kind: 'reverse_partial', n: 6, pre: 1 }) - 1 / 6) > 1e-12)
    failures.push('reverse chance');
  if (chanceFor('random', { kind: 'triangle_discard', n: 3, pre: 0 }) !== 0) failures.push('discard chance');
  if (failures.length) throw new Error(`self-check failed: ${failures.join(', ')}`);
  console.log('bots-browser self-check: OK');
}

async function runPolicy(browser, baseUrl, policy) {
  const games = Array(RUNS);
  let cursor = 0;
  let firstFailure = null;
  const workers = Array.from({ length: Math.min(CONCURRENCY, RUNS) }, async () => {
    // Separate storage per worker prevents onboardingSeen/high-score writes in
    // one concurrent game from changing another game's initial conditions.
    const context = await browser.createBrowserContext();
    const page = await context.newPage();
    const pageErrors = [];
    page.on('pageerror', (error) => pageErrors.push(`pageerror ${String(error).slice(0, 240)}`));
    page.on('console', (message) => {
      if (message.type() === 'error') pageErrors.push(`console ${message.text().slice(0, 240)}`);
    });
    page.on('requestfailed', (request) => {
      if (!/favicon\.ico/.test(request.url())) pageErrors.push(`request ${request.url()} ${request.failure()?.errorText}`);
    });
    page.on('response', (response) => {
      if (response.status() >= 400 && !/favicon\.ico/.test(response.url())) {
        pageErrors.push(`response ${response.status()} ${response.url()}`);
      }
    });
    await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
    await installPreload(page);
    try {
      while (!firstFailure) {
        const index = cursor++;
        if (index >= RUNS) break;
        try {
          games[index] = await runGame(page, baseUrl, policy, index, pageErrors);
          report.status = `running:${policy}:${games.filter(Boolean).length}/${RUNS}`;
          const existing = report.policies.find((entry) => entry.policy === policy);
          const partial = summarizePolicy(policy, games.filter(Boolean));
          if (existing) Object.assign(existing, partial);
          else report.policies.push(partial);
          writeReport();
        } catch (error) {
          const debug = path.join(HERE, `bots-browser-debug-${policy}-${index}.png`);
          await page.screenshot({ path: debug }).catch(() => {});
          firstFailure = new Error(`${policy} game ${index}: ${error.stack || error}`);
        }
      }
    } finally {
      await context.close().catch(() => {});
    }
  });
  await Promise.all(workers);
  if (firstFailure) throw firstFailure;
  return summarizePolicy(policy, games);
}

async function main() {
  if (process.argv.includes('--self-check')) {
    selfCheck();
    return;
  }
  let server = null;
  let browser = null;
  try {
    selfCheck();
    if (!fs.existsSync(GAME_FILE)) throw new Error(`game missing: ${GAME_FILE}`);
    server = await serve(PUBLIC_DIR);
    browser = await puppeteer.launch({
      headless: true,
      executablePath: resolveChrome(),
      args: [
        '--no-sandbox',
        '--disable-dev-shm-usage',
        '--mute-audio',
        '--hide-scrollbars',
        '--disable-background-timer-throttling',
        '--disable-backgrounding-occluded-windows',
        '--disable-renderer-backgrounding',
      ],
    });
    report.status = 'running';
    report.browser = { executablePath: resolveChrome() || null };
    writeReport();
    for (const policy of POLICIES) {
      const completed = await runPolicy(browser, server.url, policy);
      const index = report.policies.findIndex((entry) => entry.policy === policy);
      if (index >= 0) report.policies[index] = completed;
      else report.policies.push(completed);
      writeReport();
      console.log(
        JSON.stringify({
          policy,
          games: completed.games,
          trials: completed.trials,
          firstAttemptRate: completed.firstAttemptRate,
          chanceRate: completed.chanceRate,
          completions: completed.completions,
          pass: completed.pass,
        })
      );
    }
    report.status = 'complete';
    report.completedAt = new Date().toISOString();
    report.pass = report.policies.length === POLICIES.length && report.policies.every((entry) => entry.pass);
    writeReport();
    if (!report.pass) process.exitCode = 1;
  } catch (error) {
    report.status = /EPERM|Failed to launch the browser process/.test(String(error)) ? 'blocked' : 'failed';
    report.errors.push(String(error?.stack || error));
    report.completedAt = new Date().toISOString();
    report.pass = false;
    writeReport();
    console.error(error?.stack || error);
    process.exitCode = 1;
  } finally {
    if (browser) await browser.close().catch(() => {});
    if (server) await server.close().catch(() => {});
  }
}

await main();
