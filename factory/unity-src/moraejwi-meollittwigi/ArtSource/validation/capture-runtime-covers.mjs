#!/usr/bin/env node
// 카탈로그 표지를 실제 WebGL 런타임에서 캡처하여 플레이 화면보다 과장된 별도 삽화를 없앤다.
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/moraejwi-meollittwigi');
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
  for (const name of ['index.html', ...fs.readdirSync(path.join(GAME, 'Build')).sort().map(file => `Build/${file}`)]) {
    hash.update(name);
    hash.update(fs.readFileSync(path.join(GAME, name)));
  }
  return hash.digest('hex');
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
const captures = [];
try {
  for (const target of [
    { width: 1200, height: 630, file: 'thumb.png', scene: 'animated-title-runtime' },
    { width: 1080, height: 1080, file: 'square.png', scene: 'practice-runtime' },
  ]) {
    const page = await browser.newPage();
    await page.setViewport({ width: target.width, height: target.height, deviceScaleFactor: 1 });
    await page.goto(`${server.url}/g/moraejwi-meollittwigi/`, { waitUntil: 'networkidle2', timeout: 45000 });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 30000 });
    await sleep(900);
    if (target.scene === 'practice-runtime') {
      await page.mouse.click(target.width / 2, target.height * .5);
      await page.waitForFunction(() => window.__GAME_TEST__.getState().onboarding === true, { timeout: 5000 });
      await sleep(450);
    }
    const output = path.join(GAME, target.file);
    await page.screenshot({ path: output });
    captures.push({ file: target.file, width: target.width, height: target.height, scene: target.scene, bytes: fs.statSync(output).size });
    await page.close();
  }
  const result = {
    generated_at: new Date().toISOString(),
    run_id: process.env.MGF_RUN_ID || null,
    artifact_hash: artifactHash(),
    source: 'current Unity WebGL runtime; no illustration substitution or post-processing',
    captures,
    passed: captures.every(c => c.bytes > 10000),
  };
  fs.writeFileSync(path.join(HERE, 'cover-capture-results.json'), `${JSON.stringify(result, null, 2)}\n`);
  console.log(JSON.stringify(result, null, 2));
  if (!result.passed) process.exitCode = 1;
} finally {
  await browser.close();
  await server.close();
}
