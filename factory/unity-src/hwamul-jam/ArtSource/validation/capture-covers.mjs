#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';
import { artifactHash, chromePath, GAME, PUBLIC, sleep } from './pointer-common.mjs';

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: chromePath(),
  protocolTimeout: 180000,
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'],
});

const covers = [
  { name: 'thumb.png', width: 1200, height: 630 },
  { name: 'square.png', width: 1080, height: 1080 },
];

try {
  for (const cover of covers) {
    const page = await browser.newPage();
    await page.setViewport({ width: cover.width, height: cover.height, deviceScaleFactor: 1 });
    await page.goto(`${server.url}/g/hwamul-jam/`, { waitUntil: 'load', timeout: 45000 });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
    await sleep(1800);
    await page.screenshot({ path: path.join(GAME, cover.name) });
    await page.close();
  }
  const output = {
    generatedAt: new Date().toISOString(),
    artifactHashSha256: artifactHash(),
    source: 'final Unity WebGL title, captured at native output dimensions',
    covers: covers.map(cover => ({ ...cover, bytes: fs.statSync(path.join(GAME, cover.name)).size })),
  };
  fs.writeFileSync(new URL('./cover-capture-results.json', import.meta.url), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
} finally {
  await browser.close();
  await server.close();
}
