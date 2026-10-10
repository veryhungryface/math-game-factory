// 만두 묶기 검증 공용 도우미 — 진짜 pointer 이벤트(puppeteer mouse/touch)로 게임을 조작한다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

export const HERE = path.dirname(fileURLToPath(import.meta.url));
export const ROOT = path.resolve(HERE, '../../../../..');
export const PUBLIC = path.join(ROOT, 'public');
export const sleep = ms => new Promise(r => setTimeout(r, ms));

export function chromePath() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const c = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(c)) return c;
  }
  throw new Error('Chrome for Testing not found');
}

export async function launch() {
  const server = await serveStatic(PUBLIC);
  const browser = await puppeteer.launch({ executablePath: chromePath(), headless: true, protocolTimeout: 180000, args: ['--autoplay-policy=no-user-gesture-required'] });
  return { server, browser };
}

export async function openGame(browser, server, { w = 390, h = 844, query = '', mobile = false } = {}) {
  const page = await browser.newPage();
  const errors = [];
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', e => errors.push(String(e)));
  await page.setViewport({ width: w, height: h, deviceScaleFactor: 1, isMobile: mobile, hasTouch: mobile });
  await page.goto(`${server.url}/g/mandu-mukgi/index.html${query}`, { waitUntil: 'load' });
  await page.waitForFunction(() => window.__GAME_TEST__ && window.__GAME_TEST__.ready, { timeout: 45000 });
  await sleep(300);
  return { page, errors, w, h };
}

export async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }

export async function waitState(page, pred, timeout = 8000) {
  const end = Date.now() + timeout; let s;
  while (Date.now() < end) { s = await state(page); if (pred(s)) return s; await sleep(40); }
  throw new Error('waitState timeout ' + JSON.stringify(s).slice(0, 400));
}

/** 대상 k 의 CSS 픽셀 좌표 */
export function targetPx(g, dough, k) { return [dough.tx[k] / 1000 * g.w, dough.ty[k] / 1000 * g.h]; }

export async function drag(page, pts, { steps = 6, hold = 30 } = {}) {
  await page.mouse.move(pts[0][0], pts[0][1]);
  await page.mouse.down();
  for (let i = 1; i < pts.length; i++) await page.mouse.move(pts[i][0], pts[i][1], { steps });
  await sleep(hold);
  await page.mouse.up();
}

export async function tap(page, p) { await page.mouse.move(p[0], p[1]); await page.mouse.down(); await sleep(50); await page.mouse.up(); }

export async function shot(page, file) { await page.screenshot({ path: file }); }
