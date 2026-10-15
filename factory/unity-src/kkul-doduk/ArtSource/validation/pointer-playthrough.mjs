import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const OUT = path.dirname(new URL(import.meta.url).pathname);
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));

function chrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, build, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
}

function point(value) {
  const t = (value - 1) / 23;
  return { x: 195, y: 844 - 844 * (.22 + .45 * t) };
}

async function state(page) { return page.evaluate(() => window.__GAME_TEST__.getState()); }

async function cut(page, value) {
  const s = await state(page), from = point(s.selectedCellCount), to = point(value);
  await page.mouse.move(from.x, from.y); await page.mouse.down(); await sleep(25);
  await page.mouse.move(to.x, to.y, { steps: 12 }); await sleep(90);
  await page.mouse.move(to.x + 82, to.y, { steps: 8 }); await sleep(20); await page.mouse.up();
  await sleep(80);
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({ headless: true, executablePath: chrome(), args: ['--no-sandbox', '--mute-audio'] });
const page = await browser.newPage();
await page.setViewport({ width: 390, height: 844, deviceScaleFactor: 1 });
const errors = [];
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
page.on('pageerror', e => errors.push(String(e)));
await page.goto(`${server.url}/g/kkul-doduk/?playthrough=1`, { waitUntil: 'load', timeout: 45000 });
await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 60000 });
await page.evaluate(() => window.__GAME_TEST__.start()); await sleep(80);

// runSerial=1의 공개 생성 순서. 두 일반 실수 뒤에도 같은 문제를 직접 고쳐 최종 조건에 도달하는지 본다.
const answers = [5, 9, 9, 15, 21, 18, 12, 6];
const start = await state(page);
await cut(page, 1);
const firstWrong = await state(page);
await page.screenshot({ path: path.join(OUT, 'playthrough-01-wrong.png') });
await sleep(1750); await cut(page, answers[0]); await sleep(2200);
await cut(page, 1);
const secondWrong = await state(page);
await sleep(1750); await cut(page, answers[1]); await sleep(2200);
for (let i = 2; i < answers.length; i++) { await cut(page, answers[i]); await sleep(2200); }
const end = await state(page);
await page.screenshot({ path: path.join(OUT, 'playthrough-02-clear-after-two-errors.png') });

const result = {
  method: '390×844 실제 canvas pointer 입력만 사용. 첫 두 의뢰에서 오답 후 같은 의뢰를 수정하고 나머지를 첫 절단에 해결.',
  predeterminedAnswers: answers,
  start,
  firstWrong,
  secondWrong,
  end,
  errors,
  passed: firstWrong.lives === 2 && firstWrong.solved === 0 &&
    secondWrong.lives === 1 && secondWrong.solved === 1 &&
    end.phase === 'clear' && end.solved === 8 && end.firstCorrect === 6 && end.firstAttempted === 8 && end.lives === 1 && errors.length === 0
};
fs.writeFileSync(new URL('./pointer-playthrough-results.json', import.meta.url), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
await page.close(); await browser.close(); await server.close();
if (!result.passed) process.exitCode = 1;
