// 제비 특급 검증 공용: 정적 서버·크롬·지급 눈금 좌표(JebiView.cs 의 RailX/RailY1/RailY24 와 같은 식).
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

export const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
export const PUBLIC = path.join(ROOT, 'public');
export const SLUG = 'jebi-teukgeup';
export const sleep = ms => new Promise(r => setTimeout(r, ms));

export function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const c = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(c)) return c;
  }
}

export async function launch(extra = []) {
  return puppeteer.launch({ headless: true, executablePath: resolveChrome(), protocolTimeout: 120000,
    args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio', ...extra] });
}

export async function startServer() { return serveStatic(PUBLIC); }

export async function openGame(browser, server, w, h, tag = 'v') {
  const page = await browser.newPage();
  const errors = [];
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', e => errors.push(String(e)));
  await page.setViewport({ width: w, height: h, deviceScaleFactor: 1 });
  await page.goto(`${server.url}/g/${SLUG}/?${tag}=${Date.now()}`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
  await sleep(300);
  return { page, errors };
}

// CanvasScaler(390×844, match .5) 논리 좌표 → CSS px. DPR 1 기준.
export function rail(w, h) {
  const scale = Math.sqrt((w / 390) * (h / 844));
  const W = w / scale, H = h / scale;
  const land = (w >= h && w >= 640) || w >= 1024;
  const x = land ? W - Math.min(100, W * 0.1) : W - 46;
  const y1 = land ? H * 0.15 : H * 0.19;
  const y24 = land ? H * 0.79 : H * 0.75;
  const sp = (y24 - y1) / 23;
  const toCss = (lx, ly) => [lx * scale, h - ly * scale];
  return {
    scale, land,
    valuePt: v => toCss(x, y1 + (Math.max(0, Math.min(25, v)) - 1) * sp),
    yRange: [toCss(x, y1 - sp * 0.5)[1], toCss(x, y24 + sp * 0.5)[1]],
    x: x * scale,
  };
}

export async function pull(page, w, h, fromV, toV, { wiggle = false, steps = 10 } = {}) {
  const r = rail(w, h);
  const a = r.valuePt(fromV), b = r.valuePt(toV);
  await page.mouse.move(a[0], a[1]);
  await page.mouse.down();
  await sleep(30);
  if (wiggle && fromV === toV) {
    const c = r.valuePt(toV <= 22 ? toV + 2 : toV - 2);
    await page.mouse.move(c[0], c[1], { steps: 4 });
    await sleep(20);
  }
  await page.mouse.move(b[0], b[1], { steps });
  await sleep(30);
  await page.mouse.up();
}

export const state = page => page.evaluate(() => window.__GAME_TEST__.getState());
