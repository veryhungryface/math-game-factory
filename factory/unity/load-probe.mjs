#!/usr/bin/env node
// Unity 빌드 로딩 시간 측정: goto → __GAME_TEST__.ready 까지(ms). QA·첫플레이 하네스의 45초/20초 예산 점검용.
//   node factory/unity/load-probe.mjs <slug> [--throttle <Mbps>] [--runs 3]
// 로컬 정적 서버(무압축, .wasm=octet-stream)로 잰다 — Vercel 전송 압축 효과는 반영되지 않는다(보수적).
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../lib/static-server.mjs';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const slug = process.argv[2];
if (!slug) { console.error('사용법: node factory/unity/load-probe.mjs <slug> [--throttle Mbps] [--runs n]'); process.exit(2); }
const opt = (k, d) => { const i = process.argv.indexOf(k); return i > 0 ? Number(process.argv[i + 1]) : d; };
const mbps = opt('--throttle', 0);
const runs = opt('--runs', 3);

// qa.mjs 와 같은 규칙: 환경변수 → puppeteer 캐시의 Chrome for Testing
function chrome() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const b of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
  return undefined;
}
const server = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({ headless: true, executablePath: chrome(), args: ['--no-sandbox', '--mute-audio'] });
const out = [];
try {
  for (let i = 0; i < runs; i++) {
    const ctx = await browser.createBrowserContext(); // 매번 빈 캐시
    const page = await ctx.newPage();
    const errors = [];
    page.on('console', (m) => { if (m.type() === 'error') errors.push(m.text().slice(0, 160)); });
    await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 2, isMobile: true, hasTouch: true });
    if (mbps) {
      const c = await page.createCDPSession();
      await c.send('Network.emulateNetworkConditions', { offline: false, latency: 60, downloadThroughput: (mbps * 1e6) / 8, uploadThroughput: 1e6 });
    }
    const t0 = Date.now();
    await page.goto(`${server.url}/g/${slug}/`, { waitUntil: 'domcontentloaded', timeout: 180000 });
    const tDom = Date.now() - t0;
    await page.waitForFunction('window.__GAME_TEST__ && window.__GAME_TEST__.ready === true', { timeout: 180000, polling: 50 });
    const tReady = Date.now() - t0;
    out.push({ run: i + 1, dom_ms: tDom, ready_ms: tReady, console_errors: errors.length });
    await ctx.close();
  }
} finally {
  await browser.close();
  await server.close();
}
console.log(JSON.stringify({ slug, throttle_mbps: mbps || null, runs: out }, null, 1));
