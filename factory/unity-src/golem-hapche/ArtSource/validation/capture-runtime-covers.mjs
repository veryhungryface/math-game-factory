#!/usr/bin/env node
// 생성 일러스트와 런타임의 품질 약속이 어긋나지 않도록 실제 GPU 타이틀을 표지로 캡처한다.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../../../..');
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/golem-hapche');
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome'
    ]) {
      const candidate = path.join(base, build, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
  throw new Error('Chrome for Testing not found');
}

async function capture(browser, server, width, height, output) {
  const page = await browser.newPage();
  await page.setViewport({width, height, deviceScaleFactor: 1});
  await page.goto(`${server.url}/g/golem-hapche/`, {waitUntil: 'load', timeout: 45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  await sleep(900);
  await page.screenshot({path: output});
  await page.close();
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']});
try {
  await capture(browser, server, 1200, 630, path.join(GAME, 'thumb.png'));
  await capture(browser, server, 1080, 1080, path.join(GAME, 'square.png'));
  console.log(JSON.stringify({thumb: '1200x630 runtime title', square: '1080x1080 runtime title'}, null, 2));
} finally {
  await browser.close();
  await server.close();
}
