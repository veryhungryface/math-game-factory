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
const gameDir = path.join(publicDir, 'g', 'guseul-saechong');

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome',
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
const server = await serveStatic(publicDir);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: chromePath(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'],
});
const captures = [];
try {
  for (const target of [
    { width: 1200, height: 630, file: 'thumb.png' },
    { width: 1080, height: 1080, file: 'square.png' },
  ]) {
    const page = await browser.newPage();
    await page.setViewport({ width: target.width, height: target.height, deviceScaleFactor: 1 });
    await page.goto(`${server.url}/g/guseul-saechong/?cover=${target.width}`, { waitUntil: 'networkidle2', timeout: 45000 });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });
    await new Promise(resolve => setTimeout(resolve, 1100));
    await page.screenshot({ path: path.join(gameDir, target.file) });
    captures.push({ file: target.file, width: target.width, height: target.height });
    await page.close();
  }
} finally {
  await browser.close();
  await server.close();
}

const after = artifactHash();
const result = {
  run_id: `guseul-saechong-covers-${new Date().toISOString()}`,
  artifact_hash: before,
  artifact_hash_after: after,
  source: '최종 Unity WebGL 타이틀 화면 직접 캡처',
  captures,
  passed: before === after,
};
fs.writeFileSync(path.join(here, 'runtime-cover-results.json'), `${JSON.stringify(result, null, 2)}\n`);
console.log(JSON.stringify(result, null, 2));
if (!result.passed) process.exitCode = 1;
