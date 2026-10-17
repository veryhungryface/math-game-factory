// 화면 확인용 캡처: 타이틀 → 박쥐 탭 → 연습 → 실제 드래그 → 본판. 출력: probe/*.png
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';
const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const OUT = path.join(path.dirname(new URL(import.meta.url).pathname), 'probe');
fs.mkdirSync(OUT, {recursive: true});
const sleep = ms => new Promise(r => setTimeout(r, ms));
function resolveChrome() {
  const base = path.join(process.env.HOME, '.cache/puppeteer/chrome');
  for (const b of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
}
const W = Number(process.env.W || 390), H = Number(process.env.H || 844), tag = process.env.TAG || `${W}`;
const server = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({headless: true, executablePath: resolveChrome(), protocolTimeout: 120000,
  args: ['--no-sandbox', '--mute-audio', '--enable-unsafe-swiftshader', '--use-angle=metal']});
const page = await browser.newPage();
const errs = [];
page.on('console', m => { if (m.type() === 'error') errs.push(m.text()); });
page.on('pageerror', e => errs.push(String(e)));
await page.setViewport({width: W, height: H, deviceScaleFactor: 1});
await page.goto(`${server.url}/g/bakjwi-kkaewo/index.html`, {waitUntil: 'load'});
await page.waitForFunction(() => window.__GAME_TEST__ && window.__GAME_TEST__.ready, {timeout: 30000});
await sleep(1200);
const st = () => page.evaluate(() => window.__GAME_TEST__.getState());
await page.screenshot({path: `${OUT}/${tag}-00-title.png`});
await page.mouse.move(W * .5, H * .45); await page.mouse.down(); await sleep(120);
await page.screenshot({path: `${OUT}/${tag}-01-press.png`});
await page.mouse.up(); await sleep(900);
await page.screenshot({path: `${OUT}/${tag}-02-practice.png`});
console.log('practice', JSON.stringify(await st()));
const layout = JSON.parse(process.env.PTS || 'null');
// 연습 A→B (390 세로 기준 앵커 .27/.58, .73/.58)
const pts = layout || (W > H ? [[.125, .47], [.375, .47]] : [[.251, .36], [.749, .36]]);
await page.mouse.move(W * pts[0][0], H * pts[0][1]); await page.mouse.down();
for (let i = 1; i <= 12; i++) { await page.mouse.move(W * (pts[0][0] + (pts[1][0] - pts[0][0]) * i / 12), H * pts[0][1]); await sleep(16); }
await page.screenshot({path: `${OUT}/${tag}-03-stroke.png`});
await page.mouse.up(); await sleep(400);
await page.screenshot({path: `${OUT}/${tag}-04-reveal.png`});
await sleep(2200);
console.log('after practice', JSON.stringify(await st()));
await page.screenshot({path: `${OUT}/${tag}-05-play.png`});
// 오답 한 번: 네 마리 다 긁기
await page.mouse.move(W * .1, H * (W > H ? .47 : .36)); await page.mouse.down();
for (let i = 1; i <= 16; i++) { await page.mouse.move(W * (.1 + .8 * i / 16), H * ((W > H ? .47 : .36) + (W > H ? 0 : .235) * (i > 8 ? 1 : 0))); await sleep(16); }
await page.mouse.up(); await sleep(350);
await page.screenshot({path: `${OUT}/${tag}-06-wrong.png`});
await sleep(1500);
console.log('after wrong', JSON.stringify(await st()));
await page.screenshot({path: `${OUT}/${tag}-07-retry.png`});
// 단계 화면 확인용(캡처 전용 — 훅으로 장면만 넘긴다)
const hook = (n, name) => page.evaluate((n, name) => { for (let i = 0; i < n; i++) window.__GAME_TEST__[name](); }, n, name);
await hook(1, 'start'); await sleep(300);
await hook(3, 'answerCorrect'); await sleep(2400);
await page.screenshot({path: `${OUT}/${tag}-08-sas-interlude.png`});
await sleep(1800);
await page.screenshot({path: `${OUT}/${tag}-09-sas.png`});
await hook(3, 'answerCorrect'); await sleep(4200);
await page.screenshot({path: `${OUT}/${tag}-10-aa.png`});
await hook(1, 'answerCorrect'); await sleep(250);
await page.screenshot({path: `${OUT}/${tag}-11-aa-reveal.png`});
await hook(3, 'answerWrong'); await sleep(2600);
await page.screenshot({path: `${OUT}/${tag}-12-end.png`});
console.log('end', JSON.stringify(await st()));
console.log('errors', errs);
await browser.close(); server.close?.();
