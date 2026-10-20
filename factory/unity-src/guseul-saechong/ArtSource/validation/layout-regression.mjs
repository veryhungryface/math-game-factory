#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../../../../..');
const publicDir = path.join(root, 'public');
const gameDir = path.join(publicDir, 'g/guseul-saechong');
const frameDir = path.join(here, 'layout-regression');
fs.mkdirSync(frameDir, { recursive: true });

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
  throw new Error('Chrome for Testing not found');
}

function artifactHash() {
  const hash = crypto.createHash('sha256');
  const rels = ['index.html', ...fs.readdirSync(path.join(gameDir, 'Build')).sort().map(name => `Build/${name}`)];
  for (const rel of rels) { hash.update(rel); hash.update(fs.readFileSync(path.join(gameDir, rel))); }
  return hash.digest('hex');
}

const before = artifactHash();
const errors = [];
const captures = [];
const server = await serveStatic(publicDir);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: chromePath(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'],
});
try {
  for (const viewport of [
    { width: 1280, height: 800 },
    { width: 1440, height: 900 },
    { width: 2000, height: 1045 },
  ]) {
    const page = await browser.newPage();
    await page.setViewport({ ...viewport, deviceScaleFactor: 1 });
    page.on('console', msg => { if (msg.type() === 'error') errors.push(`${viewport.width}: ${msg.text()}`); });
    page.on('pageerror', err => errors.push(`${viewport.width}: ${String(err)}`));
    await page.goto(`${server.url}/g/guseul-saechong/?layout=${viewport.width}`, { waitUntil: 'networkidle2', timeout: 45000 });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });
    await page.screenshot({ path: path.join(frameDir, `${viewport.width}-title.png`) });
    await page.evaluate(() => window.__GAME_TEST__.start());
    await new Promise(resolve => setTimeout(resolve, 250));
    const state = await page.evaluate(() => window.__GAME_TEST__.getState());
    const within = state.promptRectPx[0] >= 0 && state.promptRectPx[1] >= 0
      && state.promptRectPx[2] <= viewport.width && state.promptRectPx[3] <= viewport.height
      && state.recordRectPx[0] >= 0 && state.recordRectPx[1] >= 0
      && state.recordRectPx[2] <= viewport.width && state.recordRectPx[3] <= viewport.height;
    const targetInsidePrompt = state.targetRectPx[0] >= state.promptRectPx[0]
      && state.targetRectPx[2] <= state.promptRectPx[2];
    const noOverlap = state.layoutGapPx >= 20;
    if (!within || !targetInsidePrompt || !noOverlap)
      errors.push(`${viewport.width}: layout ${JSON.stringify({ within, targetInsidePrompt, noOverlap, state })}`);
    await page.screenshot({ path: path.join(frameDir, `${viewport.width}-playing.png`) });
    captures.push({ ...viewport, promptRectPx: state.promptRectPx, recordRectPx: state.recordRectPx,
      targetRectPx: state.targetRectPx, layoutGapPx: state.layoutGapPx, within, targetInsidePrompt, noOverlap });
    await page.close();
  }
} finally {
  await browser.close();
  await server.close();
}

const after = artifactHash();
const result = {
  run_id: `guseul-saechong-layout-${new Date().toISOString()}`,
  artifact_hash: before,
  artifact_hash_after: after,
  captures,
  errors,
  passed: before === after && errors.length === 0,
};
fs.writeFileSync(path.join(here, 'layout-regression-results.json'), `${JSON.stringify(result, null, 2)}\n`);
console.log(JSON.stringify(result, null, 2));
if (!result.passed) process.exitCode = 1;
