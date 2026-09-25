import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import puppeteer from './browser-pipe.mjs';
import { hashes } from './browser-utils.mjs';

const out = path.resolve('logs/pane-wipe-build/browser-layout-final');
fs.mkdirSync(out, { recursive: true });
const report = { at: new Date().toISOString(), hashes: hashes(), environment: 'Real headless Chromium, CDP pipe, file:// transport; restricted software rendering.', errors: [], requests: [], layouts: [] };
const browser = await puppeteer.launch({ timeout: 15000 });
try {
  const page = await browser.newPage();
  page.on('console', m => { if (m.type() === 'error') report.errors.push(m.text()); });
  page.on('pageerror', e => report.errors.push(String(e)));
  page.on('requestfailed', r => report.errors.push(r.url() + ': ' + r.failure()?.errorText));
  page.on('request', r => report.requests.push(r.url()));
  for (const [width, height] of [[390,844], [320,720], [820,1180], [1280,800], [1920,1080]]) {
    await page.setViewport({ width, height, deviceScaleFactor: 1, isMobile: width < 900, hasTouch: width < 900 });
    await page.goto(pathToFileURL(path.resolve('public/g/pane-wipe/index.html')).href, { waitUntil: 'load', timeout: 15000 });
    await page.waitForFunction(() => window.__GAME_TEST__?.ready === true, { timeout: 10000 });
    await page.screenshot({ path: path.join(out, `title-${width}.png`) });
    const start = await page.evaluate(() => {
      const el = [...document.querySelectorAll('button')].find(el => /시작|play|start/i.test(el.id + el.textContent));
      if (!el) return null;
      const r = el.getBoundingClientRect();
      return { x: r.x + r.width/2, y: r.y + r.height/2 };
    });
    if (!start) throw new Error('No visible start CTA');
    await page.mouse.click(start.x, start.y);
    await new Promise(r => setTimeout(r, 250));
    await page.screenshot({ path: path.join(out, `play-${width}.png`) });
    report.layouts.push(await page.evaluate(() => ({
      width: innerWidth, height: innerHeight, land: document.documentElement.classList.contains('land'),
      scrollW: document.documentElement.scrollWidth,
      state: window.__GAME_TEST__.getState(),
      geometry: window.__GAME_TEST__.getInputTargets?.(),
      text: document.body.innerText,
      buttons: [...document.querySelectorAll('button')].filter(e => e.getBoundingClientRect().width).map(e => { const r=e.getBoundingClientRect();return {text:e.textContent,w:r.width,h:r.height}; }),
      failedImages: [...document.images].filter(e => !e.complete || !e.naturalWidth).map(e=>e.getAttribute('src')),
    })));
  }
  report.externalRequests = report.requests.filter(u => /^https?:/.test(u));
} finally {
  await browser.close();
  fs.writeFileSync(path.join(out, 'report.json'), JSON.stringify(report, null, 2) + '\n');
}
console.log(JSON.stringify(report, null, 2));
