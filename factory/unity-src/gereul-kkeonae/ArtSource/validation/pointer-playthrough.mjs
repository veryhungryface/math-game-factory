#!/usr/bin/env node
// 8문항 정상 경로와 첫 문항 1회 실수 후 회복 경로를 답 훅 없이 실제 포인터로 완주한다.
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/gereul-kkeonae');
const SHOTS = path.join(HERE, 'pointer-playthrough');
const W = 390, H = 844;
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome',
  ]) { const p = path.join(base, build, rel); if (fs.existsSync(p)) return p; }
  throw new Error('Chrome for Testing not found');
}

function artifactHash() {
  const hash = crypto.createHash('sha256');
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(x => `Build/${x}`)]) hash.update(fs.readFileSync(path.join(GAME, name)));
  return hash.digest('hex');
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }
async function waitFor(page, predicate, label, timeout = 5000) {
  const start = Date.now();
  while (Date.now() - start < timeout) { const s = await state(page); if (predicate(s)) return s; await sleep(24); }
  throw new Error(`timeout ${label}: ${JSON.stringify(await state(page))}`);
}
const bp = (x, unityY) => [x, H - unityY];
async function drag(page, from, points) {
  await page.mouse.move(...from); await page.mouse.down(); await sleep(48);
  for (const to of points) { await page.mouse.move(...to, { steps: 2 }); await sleep(64); }
  await page.mouse.up(); await sleep(48);
}
function lengthPoint(s, cm) {
  const t = (cm - 1) / 23;
  return s.layout === 'land' ? bp(s.handleX, s.rulerMin + (s.rulerMax - s.rulerMin) * t) : bp(s.rulerMin + (s.rulerMax - s.rulerMin) * t, s.handleY);
}
async function submit(page, cm) {
  let s = await state(page); const points = [];
  if (s.drawnLength === cm) points.push(lengthPoint(s, cm === 24 ? 23 : cm + 1));
  points.push(lengthPoint(s, cm));
  await drag(page, bp(s.handleX, s.handleY), points);
  s = await waitFor(page, x => x.toolStep === 'cut', 'cut step');
  await drag(page, bp(s.cutX - 34, s.cutY), [bp(s.cutX + 34, s.cutY)]);
  s = await waitFor(page, x => x.toolStep === 'connect', 'connect step');
  await drag(page, bp(s.pieceX, s.pieceY), [bp(s.gapX, s.gapY)]);
  return waitFor(page, x => x.phase === 'reveal', 'reveal');
}
function answerOf(s) { const m = s.problemId.match(/-l(\d+)-/); if (!m) throw new Error(`no answer in ${s.problemId}`); return Number(m[1]); }

async function run(page, withMistake) {
  await page.evaluate(() => window.__GAME_TEST__.start());
  let s = await waitFor(page, x => x.phase === 'playing' && x.round === 1, 'run start');
  const played = []; let recovery = null;
  for (let round = 1; round <= 8; round++) {
    const answer = answerOf(s); played.push({ round, problemId: s.problemId, answer });
    if (withMistake && round === 1) {
      const wrong = answer === 24 ? 23 : answer + 1;
      const wrongReveal = await submit(page, wrong);
      recovery = { wrong, state: wrongReveal };
      s = await waitFor(page, x => x.phase === 'playing' && x.round === 1 && x.toolStep === 'pull', 'recover same net', 5000);
    }
    await submit(page, answer);
    s = await waitFor(page, x => x.phase === 'clear' || x.phase === 'gameover' || (x.phase === 'playing' && x.round === round + 1), `advance ${round}`, 6000);
  }
  return { played, recovery, final: s };
}

fs.mkdirSync(SHOTS, { recursive: true });
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), protocolTimeout: 900000, args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const consoleErrors = [];
try {
  const page = await browser.newPage(); await page.setViewport({ width: W, height: H, deviceScaleFactor: 1 });
  page.on('console', m => { if (m.type() === 'error') consoleErrors.push(m.text()); }); page.on('pageerror', e => consoleErrors.push(String(e)));
  await page.goto(`${server.url}/g/gereul-kkeonae/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  const normal = await run(page, false); await page.screenshot({ path: path.join(SHOTS, 'normal-clear.png') });
  const recovery = await run(page, true); await page.screenshot({ path: path.join(SHOTS, 'recovery-clear.png') });
  const out = {
    runId: `gereul-kkeonae-pointer-${new Date().toISOString()}`, generatedAt: new Date().toISOString(), artifactHashSha256: artifactHash(), viewport: `${W}x${H}`,
    input: 'trusted Puppeteer mouse down/move/up; start() only enters a run; answer hooks never called', normal, oneMistakeRecovery: recovery, consoleErrors,
    passed: normal.final.phase === 'clear' && normal.final.solved === 8 && normal.final.firstCorrect === 8 && recovery.recovery?.state.lives === 2 && recovery.final.phase === 'clear' && recovery.final.solved === 8 && recovery.final.firstCorrect === 7 && consoleErrors.length === 0,
  };
  fs.writeFileSync(path.join(HERE, 'pointer-playthrough-results.json'), `${JSON.stringify(out, null, 2)}\n`);
  console.log(JSON.stringify(out, null, 2)); if (!out.passed) process.exitCode = 1;
  await page.close();
} finally { await browser.close(); await server.close(); }
