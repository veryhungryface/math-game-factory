// 2차 수정 검증: ① 본판 문제 카드 탭 → 제출 없음 + 유령 손가락, ② 카드 위 좌우 당김 → 실 길이 변화,
// ③ 정답+1 cm 오답 → 판정 기준이 리셋 눈금이 아니라 제출 길이(silk_too_long)인지, ④ 풀기 전 화면 캡처.
import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const OUT = path.resolve(new URL('./fix2-frames/', import.meta.url).pathname);
fs.mkdirSync(OUT, {recursive: true});
const sleep = ms => new Promise(r => setTimeout(r, ms));
function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const c = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(c)) return c;
  }
}
function solve(prompt) {
  let m = prompt.match(/선분 AD, DB, AE의 길이가 각각 (\d+) cm, (\d+) cm, (\d+) cm.*선분 EC의 길이/);
  if (m) return m[3] * m[2] / m[1];
  m = prompt.match(/선분 AD, DB, BC의 길이가 각각 (\d+) cm, (\d+) cm, (\d+) cm.*선분 DE의 길이/);
  if (m) return m[3] * m[1] / (+m[1] + +m[2]);
  m = prompt.match(/선분 BC의 길이가 (\d+) cm.*선분 MN의 길이/);
  if (m) return m[1] / 2;
  throw new Error(prompt);
}
const xFor = v => 41 + (v - 1) * (308 / 23);
const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({headless: true, executablePath: resolveChrome(), args: ['--no-sandbox', '--mute-audio']});
const page = await browser.newPage();
const errors = [];
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
page.on('pageerror', e => errors.push(String(e)));
await page.setViewport({width: 390, height: 844, deviceScaleFactor: 1});
await page.goto(`${server.url}/g/sil-taneun-geomi/`, {waitUntil: 'load'});
await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
const st = () => page.evaluate(() => window.__GAME_TEST__.getState());
const result = {};

await page.evaluate(() => window.__GAME_TEST__.start());
await sleep(600);
await page.screenshot({path: path.join(OUT, '01-main-before-solve.png')});
const s0 = await st();
// ① 문제 카드 중앙 탭
await page.mouse.click(195, 330);
await sleep(350);
await page.screenshot({path: path.join(OUT, '02-card-tap-ghost-guide.png')});
const s1 = await st();
result.cardTap = {before: s0.selectedLength, after: s1.selectedLength, submitted: s1.firstAttemptTotal, pass: s1.selectedLength === s0.selectedLength && s1.firstAttemptTotal === 0};
// ② 카드 위 좌우 당김(+80px ≈ +6 눈금) → 놓으면 제출
await page.mouse.move(150, 330); await page.mouse.down(); await sleep(30);
await page.mouse.move(190, 330, {steps: 3}); await sleep(30);
await page.mouse.move(230, 330, {steps: 3}); await sleep(60);
const sMid = await st();
await page.screenshot({path: path.join(OUT, '03-card-drag-live-label.png')});
await page.mouse.up(); await sleep(80);
const s2 = await st();
result.cardDrag = {start: s1.selectedLength, during: sMid.selectedLength, submittedAfterRelease: s2.firstAttemptTotal, pass: sMid.selectedLength !== s1.selectedLength && s2.firstAttemptTotal === 1};
await sleep(2200);

// ③ 새 판: 정답+1 cm 를 계량 실에서 끌어 제출
await page.evaluate(() => window.__GAME_TEST__.start());
await sleep(500);
const p = await st();
const ans = solve(p.prompt);
const wrong = ans === 24 ? 23 : ans + 1;
await page.mouse.move(xFor(wrong === 1 ? 2 : wrong - 1), 620); await page.mouse.down(); await sleep(20);
await page.mouse.move(xFor(wrong), 620, {steps: 3}); await sleep(20); await page.mouse.up();
await sleep(400);
await page.screenshot({path: path.join(OUT, '04-wrong-reveal-card.png')});
const sW = await st();
await sleep(1500);
await page.screenshot({path: path.join(OUT, '05-wrong-after-reset-toast.png')});
const sR = await st();
result.wrongDirection = {answer: ans, submitted: wrong, misconceptionId: sW.misconceptionId, resetLength: sR.selectedLength,
  expected: wrong > ans ? 'silk_too_long' : 'silk_too_short', pass: sW.misconceptionId === (wrong > ans ? 'silk_too_long' : 'silk_too_short') && sR.selectedLength !== wrong};
// ④ 본판 무입력 7초 → 유령 손가락 재등장
await sleep(7600);
await page.screenshot({path: path.join(OUT, '06-idle-ghost-guide.png')});
result.consoleErrors = errors;
console.log(JSON.stringify(result, null, 2));
fs.writeFileSync(new URL('./fix2-results.json', import.meta.url), JSON.stringify({runAt: new Date().toISOString(), ...result}, null, 2));
await browser.close(); await server.close();
