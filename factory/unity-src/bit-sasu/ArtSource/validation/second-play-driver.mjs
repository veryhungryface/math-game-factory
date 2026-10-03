// 평가자 두 번째 플레이 드라이버. 화면 캡처만 넘기고, 판단은 사람이(평가 에이전트가) 화면을 읽고 한다.
// problemId·정답 훅·getState를 플레이 중에 쓰지 않는다. ready 대기와 클리어 후 최종 상태 1회만 읽는다.
// 명령: CMD_DIR/cmd-N.json {op:'shot'|'tap'|'drag'|'final'|'quit', x,y,dy}  → CMD_DIR/res-N.json (+ shot-N.png)
// 캡처 직후 빈 탭을 앞으로 가져와 게임 탭을 hidden 으로 만들어 rAF(=Unity 시계)를 멈추고 화면을 읽는다(입력과 샷 연출은 정상 시간으로 돈다).
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(import.meta.dirname, '../../../../..');
const DIR = process.env.CMD_DIR || '/tmp/bsplay';
const sleep = ms => new Promise(r => setTimeout(r, ms));
function resolveChrome() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(os.homedir(), '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const f = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(f)) return f;
  }
}
const server = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({ headless: true, executablePath: resolveChrome(),
  args: ['--no-sandbox', '--use-angle=metal', '--mute-audio'] });
const page = await browser.newPage();
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1, isMobile: true, hasTouch: true });
await page.goto(`${server.url}/g/bit-sasu/`, { waitUntil: 'domcontentloaded' });
await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });
const blank = await browser.newPage();
await page.bringToFront();
let paused = false;
async function pause() { if (!paused) { await blank.bringToFront(); paused = true; await sleep(60); } }
async function resume() { if (paused) { await page.bringToFront(); paused = false; await sleep(30); } }
const log = [];
let n = 0;
await sleep(1500);
for (;;) {
  const f = path.join(DIR, `cmd-${n}.json`);
  if (!fs.existsSync(f)) { await sleep(150); continue; }
  const c = JSON.parse(fs.readFileSync(f, 'utf8'));
  const res = { n, op: c.op, t: Date.now() };
  try {
    if (c.op === 'tap' || c.op === 'drag') {
      await resume();
      const t = page.touchscreen;
      await t.touchStart(c.x, c.y); await sleep(50);
      if (c.op === 'drag') {
        const steps = Math.max(2, Math.ceil(Math.abs(c.dy) / 12));
        for (let i = 1; i <= steps; i++) { await t.touchMove(c.x + (c.dx || 0) * i / steps, c.y + c.dy * i / steps); await sleep(18); }
        await sleep(60);
      }
      await t.touchEnd();
      await sleep(c.wait ?? 1900);
      log.push({ op: c.op, x: c.x, y: c.y, dx: c.dx || 0, dy: c.dy || 0, note: c.note || '' });
    }
    if (c.op === 'wait') { await resume(); await sleep(c.ms || 1000); }
    if (c.op === 'final') { await resume(); res.final = await page.evaluate(() => window.__GAME_TEST__.getState()); }
    if (c.op !== 'final' && c.op !== 'quit') { await resume(); await sleep(120); await page.screenshot({ path: path.join(DIR, `shot-${n}.png`) }); await pause(); }
    res.ok = true;
  } catch (e) { res.ok = false; res.err = String(e); }
  res.log = c.op === 'final' || c.op === 'quit' ? log : undefined;
  fs.writeFileSync(path.join(DIR, `res-${n}.json`), JSON.stringify(res, null, 1));
  n++;
  if (c.op === 'quit') break;
}
await resume().catch(() => {});
await browser.close(); server.close?.();
process.exit(0);
