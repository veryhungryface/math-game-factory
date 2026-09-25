#!/usr/bin/env node
/**
 * Trusted-pointer end-to-end playthroughs for public/g/star-out.
 *
 * Run after the game edit is complete:
 *   node logs/star-out-build/playthrough-browser.mjs
 *
 * The harness may inspect the in-page geometry and problem model to locate the
 * answer, but every start, training, wrong, and correct action is delivered by
 * Puppeteer's page.mouse API. It never invokes answer/test-input helpers and
 * never synthesizes a DOM event.
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
const OUT_FILE = path.join(HERE, 'playthrough-browser.json');
const VIEWPORT = { width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true };
const PLAY_TIMEOUT_MS = positiveInt(process.env.PLAYTHROUGH_TIMEOUT_MS, 75_000);
const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));

function positiveInt(value, fallback) {
  const parsed = Number(value);
  return Number.isInteger(parsed) && parsed > 0 ? parsed : fallback;
}

function sha256(file) {
  return crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');
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

function serve(rootDir) {
  const absoluteRoot = path.resolve(rootDir);
  const server = http.createServer((request, response) => {
    let pathname;
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

async function installPreload(page, seed) {
  await page.evaluateOnNewDocument((initialSeed) => {
    let randomState = initialSeed >>> 0;
    Math.random = () => {
      randomState = (Math.imul(randomState, 1664525) + 1013904223) >>> 0;
      return randomState / 4294967296;
    };
    try {
      localStorage.clear();
    } catch {}
    window.__PLAY_INPUT_MODE__ = 'none';
    window.__PLAY_POINTERS__ = {
      trusted: 0,
      untrusted: 0,
      byMode: {
        start: { down: 0, move: 0, up: 0 },
        training: { down: 0, move: 0, up: 0 },
        gameplay: { down: 0, move: 0, up: 0 },
        none: { down: 0, move: 0, up: 0 },
      },
    };
    for (const type of ['pointerdown', 'pointermove', 'pointerup']) {
      addEventListener(
        type,
        (event) => {
          const counters = window.__PLAY_POINTERS__;
          const key = type.slice('pointer'.length);
          if (!event.isTrusted) {
            counters.untrusted++;
            return;
          }
          counters.trusted++;
          const mode = Object.hasOwn(counters.byMode, window.__PLAY_INPUT_MODE__)
            ? window.__PLAY_INPUT_MODE__
            : 'none';
          counters.byMode[mode][key]++;
        },
        true
      );
    }
  }, seed);
}

async function setInputMode(page, mode) {
  await page.evaluate((nextMode) => {
    window.__PLAY_INPUT_MODE__ = nextMode;
  }, mode);
}

async function mouseTap(page, point, mode, counters) {
  await setInputMode(page, mode);
  try {
    await page.mouse.move(point.x, point.y);
    await page.mouse.down();
    await sleep(35);
    await page.mouse.up();
  } finally {
    await setInputMode(page, 'none');
  }
  counters.taps++;
  if (mode === 'start') counters.startClicks++;
}

async function mouseDrag(page, from, to, mode, counters) {
  await setInputMode(page, mode);
  try {
    await page.mouse.move(from.x, from.y);
    await page.mouse.down();
    await page.mouse.move((from.x + to.x) / 2, (from.y + to.y) / 2, { steps: 3 });
    await page.mouse.move(to.x, to.y, { steps: 3 });
    await sleep(25);
    await page.mouse.up();
  } finally {
    await setInputMode(page, 'none');
  }
  counters.drags++;
  if (mode === 'training') counters.trainingDrags++;
  if (mode === 'gameplay') counters.gameplayDrags++;
}

async function clickVisible(page, selector, mode, counters) {
  const point = await page.$eval(selector, (element) => {
    const rect = element.getBoundingClientRect();
    if (!rect.width || !rect.height) throw new Error(`${selector} is not visible`);
    return { x: rect.left + rect.width / 2, y: rect.top + rect.height / 2 };
  });
  await mouseTap(page, point, mode, counters);
}

/**
 * The game script deliberately keeps S and geometry in its global lexical
 * environment. We read those values only as an oracle for pointer coordinates;
 * no state is mutated here.
 */
async function snapshot(page) {
  return page.evaluate(() => {
    if (typeof S === 'undefined' || typeof geometry !== 'function') {
      throw new Error('star-out S/geometry oracle is unavailable');
    }
    const plate = S.plate;
    const base = {
      phase: S.phase,
      score: S.score,
      lives: S.lives,
      level: S.level,
      solved: S.solved,
      combo: S.combo,
      time: S.time,
      frozen: S.frozen,
      tutorial: S.tutorial,
      discardGuide: S.discardGuide,
      lastMisconceptionId: S.lastMisconceptionId,
      hint: document.getElementById('feedback')?.textContent || '',
      history: Array.isArray(S.history)
        ? S.history.map((entry) => ({
            id: String(entry.id),
            kind: String(entry.kind),
            n: Number(entry.n),
            pre: Number(entry.pre || 0),
            firstCorrect: !!entry.firstCorrect,
            completed: !!entry.completed,
            misconceptionId: entry.misconceptionId || null,
          }))
        : [],
      plate: null,
    };
    if (!plate) return base;
    const figure = geometry();
    const canvas = document.getElementById('table');
    const rect = canvas.getBoundingClientRect();
    const key = (pair) => {
      const a = Number(pair[0]);
      const b = Number(pair[1]);
      return a < b ? `${a}:${b}` : `${b}:${a}`;
    };
    const present = new Set((plate.lines || []).map((line) => key(line.pair)));
    const remainingPairs = (plate.requiredPairs || [])
      .filter((pair) => !present.has(key(pair)))
      .map((pair) => [Number(pair[0]), Number(pair[1])]);
    base.plate = {
      id: String(plate.id),
      kind: String(plate.kind),
      n: Number(plate.n),
      marked: Number(plate.marked),
      discard: !!plate.discard,
      guided: !!plate.guided,
      locked: !!plate.locked,
      fallen: !!plate.fallen,
      completed: !!plate.completed,
      elapsed: Number(plate.elapsed || 0),
      lineCount: Array.isArray(plate.lines) ? plate.lines.length : 0,
      remainingPairs,
      center: { x: rect.left + figure.cx, y: rect.top + figure.cy },
      vertices: figure.vertices.map((point) => ({ x: rect.left + point.x, y: rect.top + point.y })),
      discardTarget: {
        x: rect.left + play.x + play.w - 8,
        y: rect.top + figure.cy,
      },
    };
    return base;
  });
}

function reportState(state) {
  return {
    phase: state.phase,
    score: state.score,
    lives: state.lives,
    level: state.level,
    solved: state.solved,
    combo: state.combo,
    time: state.time,
    frozen: state.frozen,
    tutorial: state.tutorial,
    discardGuide: state.discardGuide,
    lastMisconceptionId: state.lastMisconceptionId,
    hint: state.hint,
    plate: state.plate
      ? {
          id: state.plate.id,
          kind: state.plate.kind,
          n: state.plate.n,
          marked: state.plate.marked,
          discard: state.plate.discard,
          guided: state.plate.guided,
          locked: state.plate.locked,
          fallen: state.plate.fallen,
          completed: state.plate.completed,
          lineCount: state.plate.lineCount,
          remainingPairs: state.plate.remainingPairs,
        }
      : null,
    history: state.history,
  };
}

async function waitForState(page, predicate, label, timeout = 6000) {
  const deadline = Date.now() + timeout;
  let latest = null;
  while (Date.now() < deadline) {
    latest = await snapshot(page);
    if (predicate(latest)) return latest;
    await sleep(35);
  }
  throw new Error(`timeout waiting for ${label}: ${JSON.stringify(reportState(latest || {}))}`);
}

async function drawPair(page, state, pair, mode, counters) {
  const latest = await snapshot(page);
  if (!latest.plate || latest.plate.id !== state.plate.id) {
    throw new Error(`plate changed before drag: ${state.plate.id} -> ${latest.plate?.id || 'none'}`);
  }
  const from = latest.plate.vertices[pair[0]];
  const to = latest.plate.vertices[pair[1]];
  if (!from || !to) throw new Error(`invalid pair ${JSON.stringify(pair)} on ${latest.plate.id}`);
  await mouseDrag(page, from, to, mode, counters);
}

async function solveDrawable(page, state, mode, counters) {
  const plateId = state.plate.id;
  let guard = 0;
  while (guard++ < 8) {
    const current = await snapshot(page);
    if (current.phase !== 'playing' || !current.plate || current.plate.id !== plateId || current.plate.locked) return;
    const pair = current.plate.remainingPairs[0];
    if (!pair) return;
    const beforeLines = current.plate.lineCount;
    const beforeLives = current.lives;
    await drawPair(page, current, pair, mode, counters);
    await waitForState(
      page,
      (next) =>
        next.phase !== 'playing' ||
        !next.plate ||
        next.plate.id !== plateId ||
        next.plate.locked ||
        next.plate.lineCount > beforeLines ||
        next.lives < beforeLives,
      `pair ${pair.join(':')} to register on ${plateId}`,
      1800
    );
  }
  throw new Error(`too many pair attempts on ${plateId}`);
}

async function discardPlate(page, state, mode, counters) {
  const plateId = state.plate.id;
  await mouseDrag(page, state.plate.center, state.plate.discardTarget, mode, counters);
  await waitForState(
    page,
    (next) =>
      next.phase !== 'playing' ||
      !next.plate ||
      next.plate.id !== plateId ||
      next.plate.locked,
    `discard ${plateId}`,
    2200
  );
}

async function waitUntilActionable(page, startedAt) {
  while (Date.now() - startedAt < PLAY_TIMEOUT_MS) {
    const state = await snapshot(page);
    if (state.phase !== 'playing') return state;
    if (state.plate && !state.plate.locked) return state;
    await sleep(30);
  }
  throw new Error(`scenario exceeded ${PLAY_TIMEOUT_MS}ms`);
}

async function makeRecoveryError(page, state, counters) {
  const before = reportState(state);
  const neighbour = (state.plate.marked + 1) % state.plate.n;
  await drawPair(page, state, [state.plate.marked, neighbour], 'gameplay', counters);
  const afterState = await waitForState(
    page,
    (next) =>
      next.lives === state.lives - 1 ||
      next.phase !== 'playing' ||
      next.plate?.id !== state.plate.id,
    'one adjacent-vertex error',
    1800
  );
  if (afterState.phase !== 'playing') throw new Error('recovery error ended the game');
  if (afterState.plate?.id !== state.plate.id) throw new Error('wrong answer advanced to another plate');
  if (afterState.lives !== state.lives - 1) {
    throw new Error(`wrong answer did not cost exactly one life: ${state.lives} -> ${afterState.lives}`);
  }
  if (afterState.plate.locked) throw new Error('wrong answer locked the drawable plate');
  return { before, after: reportState(afterState) };
}

async function pointerCounts(page) {
  return page.evaluate(() => JSON.parse(JSON.stringify(window.__PLAY_POINTERS__)));
}

function attachErrorCapture(page) {
  const errors = { console: [], page: [], network: [] };
  page.on('console', (message) => {
    if (message.type() === 'error') errors.console.push(message.text().slice(0, 500));
  });
  page.on('pageerror', (error) => errors.page.push(String(error).slice(0, 500)));
  page.on('requestfailed', (request) => {
    if (!/favicon\.ico/.test(request.url())) {
      errors.network.push(`${request.url()} ${request.failure()?.errorText || 'request failed'}`);
    }
  });
  page.on('response', (response) => {
    if (response.status() >= 400 && !/favicon\.ico/.test(response.url())) {
      errors.network.push(`${response.status()} ${response.url()}`);
    }
  });
  return errors;
}

async function runScenario(browser, baseUrl, specification) {
  const context = await browser.createBrowserContext();
  const page = await context.newPage();
  const errors = attachErrorCapture(page);
  const counters = {
    taps: 0,
    drags: 0,
    startClicks: 0,
    trainingDrags: 0,
    gameplayDrags: 0,
  };
  const startedAt = Date.now();
  let fixedTraining = 0;
  let guidedDiscardTraining = 0;
  let ordinaryDrawableSeen = 0;
  let recovery = null;
  try {
    await page.setViewport(VIEWPORT);
    await installPreload(page, specification.seed);
    await page.goto(`${baseUrl}/g/star-out/?playthrough=${specification.name}&seed=${specification.seed}`, {
      waitUntil: 'networkidle0',
      timeout: 30_000,
    });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 15_000 });
    await clickVisible(page, '#begin', 'start', counters);
    await waitForState(page, (state) => state.phase === 'playing' && !!state.plate, 'actual start button');

    while (Date.now() - startedAt < PLAY_TIMEOUT_MS) {
      const state = await waitUntilActionable(page, startedAt);
      if (state.phase !== 'playing') break;
      const training = state.tutorial || state.plate.guided || state.discardGuide;
      const mode = training ? 'training' : 'gameplay';

      if (training && state.plate.id === 'practice-square') fixedTraining++;
      if (training && state.plate.discard) guidedDiscardTraining++;

      if (!training && !state.plate.discard) {
        ordinaryDrawableSeen++;
        if (specification.recovery && !recovery) {
          recovery = await makeRecoveryError(page, state, counters);
        }
      }

      const current = await snapshot(page);
      if (current.phase !== 'playing' || !current.plate || current.plate.locked) continue;
      if (current.plate.discard) await discardPlate(page, current, mode, counters);
      else await solveDrawable(page, current, mode, counters);
    }

    const final = await waitForState(page, (state) => state.phase !== 'playing', 'clear state', 5000);
    const pointers = await pointerCounts(page);
    const allErrors = [...errors.console, ...errors.page, ...errors.network];
    const expectedLives = specification.recovery ? 2 : 3;
    const assertions = {
      clear: final.phase === 'clear',
      solvedEight: final.solved === 8,
      expectedLives: final.lives === expectedLives,
      actualStartClick: counters.startClicks === 1 && pointers.byMode.start.down >= 1 && pointers.byMode.start.up >= 1,
      fixedTrainingCompleted: fixedTraining >= 1,
      guidedDiscardCompleted: guidedDiscardTraining >= 1,
      recoveryPerformed: specification.recovery ? !!recovery : !recovery,
      recoveryWasFirstDrawable: specification.recovery ? ordinaryDrawableSeen >= 1 && !!recovery : true,
      trustedAnswerPointers:
        pointers.byMode.training.down >= 2 &&
        pointers.byMode.training.up >= 2 &&
        pointers.byMode.gameplay.down >= 1 &&
        pointers.byMode.gameplay.up >= 1,
      noUntrustedPointers: pointers.untrusted === 0,
      noBrowserErrors: allErrors.length === 0,
    };
    const pass = Object.values(assertions).every(Boolean);
    return {
      name: specification.name,
      seed: specification.seed,
      pass,
      assertions,
      finalState: reportState(final),
      wrongBefore: recovery?.before || null,
      wrongAfter: recovery?.after || null,
      firstDrawableId: recovery?.before?.plate?.id || null,
      training: {
        fixedPointJoinVisits: fixedTraining,
        guidedDiscardVisits: guidedDiscardTraining,
      },
      inputs: { ...counters, pointerEvents: pointers },
      durationMs: Date.now() - startedAt,
      errors,
    };
  } catch (error) {
    const pointers = await pointerCounts(page).catch(() => null);
    const debugPath = path.join(HERE, `playthrough-browser-debug-${specification.name}.png`);
    await page.screenshot({ path: debugPath, fullPage: true }).catch(() => {});
    return {
      name: specification.name,
      seed: specification.seed,
      pass: false,
      error: String(error?.stack || error),
      inputs: { ...counters, pointerEvents: pointers },
      wrongBefore: recovery?.before || null,
      wrongAfter: recovery?.after || null,
      durationMs: Date.now() - startedAt,
      errors,
      debugScreenshot: debugPath,
    };
  } finally {
    await context.close().catch(() => {});
  }
}

function selfCheck() {
  const source = fs.readFileSync(fileURLToPath(import.meta.url), 'utf8');
  const forbiddenCalls = [
    /__GAME_TEST__\s*\.\s*answerCorrect\s*\(/,
    /__GAME_TEST__\s*\.\s*answerWrong\s*\(/,
    /__GAME_TEST__\s*\.\s*simulateInput\s*\(/,
    /\bdispatchEvent\s*\(/,
  ];
  const failures = forbiddenCalls.filter((pattern) => pattern.test(source)).map(String);
  if (!/page\.mouse\.(?:down|up|move)\s*\(/.test(source)) failures.push('page.mouse input missing');
  if (failures.length) throw new Error(`self-check failed: ${failures.join(', ')}`);
}

const report = {
  slug: 'star-out',
  status: 'starting',
  startedAt: new Date().toISOString(),
  sourceSha256: fs.existsSync(GAME_FILE) ? sha256(GAME_FILE) : null,
  harnessSha256: sha256(fileURLToPath(import.meta.url)),
  settings: {
    viewport: VIEWPORT,
    timeoutMs: PLAY_TIMEOUT_MS,
    scenarios: [
      { name: 'normal', seed: 0x51a70a11, expected: 'clear, solved=8, lives=3' },
      { name: 'recovery', seed: 0x51a70a22, recovery: true, expected: 'first ordinary drawable wrong once, then clear, solved=8, lives=2' },
    ],
  },
  method:
    'The harness reads S.plate/geometry only to locate visible answer coordinates. All start, fixed point-join training, guided discard training, intentional wrong, and correct answers use trusted Puppeteer page.mouse input.',
  scenarios: [],
  errors: [],
};

function writeReport() {
  fs.writeFileSync(OUT_FILE, `${JSON.stringify(report, null, 2)}\n`);
}

async function main() {
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
    for (const specification of report.settings.scenarios) {
      const result = await runScenario(browser, server.url, specification);
      report.scenarios.push(result);
      writeReport();
    }
    report.status = 'complete';
    report.completedAt = new Date().toISOString();
    report.pass = report.scenarios.length === 2 && report.scenarios.every((scenario) => scenario.pass);
    writeReport();
    console.log(
      JSON.stringify({
        pass: report.pass,
        sourceSha256: report.sourceSha256,
        scenarios: report.scenarios.map((scenario) => ({
          name: scenario.name,
          pass: scenario.pass,
          phase: scenario.finalState?.phase,
          solved: scenario.finalState?.solved,
          lives: scenario.finalState?.lives,
          durationMs: scenario.durationMs,
        })),
      })
    );
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
