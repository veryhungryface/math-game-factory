import fs from 'node:fs';
import crypto from 'node:crypto';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
export const wait = ms => new Promise(resolve => setTimeout(resolve, ms));
export const url = pathToFileURL(path.resolve('public/g/pane-wipe/index.html')).href;
export const hashes = () => Object.fromEntries(['index.html', 'engine.js', 'meta.json'].filter(f => fs.existsSync('public/g/pane-wipe/' + f)).map(f => [f, crypto.createHash('sha256').update(fs.readFileSync('public/g/pane-wipe/' + f)).digest('hex')]));
export async function installProbe(page, report) {
  page.on('console', m => { if (m.type() === 'error') report.errors.push(m.text()); });
  page.on('pageerror', e => report.errors.push(String(e)));
  page.on('requestfailed', r => report.errors.push(r.url() + ': ' + r.failure()?.errorText));
  page.on('request', r => report.requests.push(r.url()));
  await page.evaluateOnNewDocument(() => {
    window.__trustedQA = { down: 0, move: 0, up: 0, touch: 0, mouse: 0, untrusted: 0 };
    for (const [type, key] of [['pointerdown', 'down'], ['pointermove', 'move'], ['pointerup', 'up']]) {
      document.addEventListener(type, e => {
        if (e.isTrusted) {
          window.__trustedQA[key]++;
          window.__trustedQA[e.pointerType === 'touch' ? 'touch' : 'mouse']++;
        } else window.__trustedQA.untrusted++;
      }, true);
    }
  });
}
export async function pointerDrag(page, a, b, touch = false) {
  if (touch) {
    await page.touchscreen.touchStart(a.x, a.y);
    for (let i = 1; i <= 10; i++) {
      await page.touchscreen.touchMove(a.x + (b.x - a.x) * i / 10, a.y + (b.y - a.y) * i / 10);
      await wait(20);
    }
    await page.touchscreen.touchEnd();
  } else {
    await page.mouse.move(a.x, a.y);
    await page.mouse.down();
    for (let i = 1; i <= 10; i++) {
      await page.mouse.move(a.x + (b.x - a.x) * i / 10, a.y + (b.y - a.y) * i / 10);
      await wait(20);
    }
    await page.mouse.up();
  }
}
