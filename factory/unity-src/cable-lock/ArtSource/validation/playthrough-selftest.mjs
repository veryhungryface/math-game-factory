import fs from 'node:fs';
import path from 'node:path';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const sleep = ms => new Promise(r => setTimeout(r, ms));

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const b of fs.readdirSync(base).sort().reverse()) {
    const p = path.join(base, b, 'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing');
    if (fs.existsSync(p)) return p;
  }
  return undefined;
}

function yFor(length) { return 731 - (length - 1) * (300 / 47); }

async function dragLength(page, from, to) {
  await page.mouse.move(195, yFor(to));
  await page.mouse.down();
  await page.mouse.move(195, yFor(to) + (to === 1 ? -4 : 4), {steps: 4});
  await sleep(80);
  await page.mouse.up();
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--enable-unsafe-swiftshader', '--use-angle=swiftshader', '--mute-audio']
});
const page = await browser.newPage();
await page.setViewport({width: 390, height: 844, deviceScaleFactor: 1});
const errors = [];
page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
page.on('pageerror', e => errors.push(String(e)));
await page.goto(`${server.url}/g/cable-lock/`, {waitUntil: 'load', timeout: 45000});
await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 20000});

// 실제 타이틀 CTA 위치를 탭하고, 고정 연습 5→6 m를 실제 pointer drag로 수행한다.
await page.mouse.click(195, 775);
await sleep(500);
const practiceBefore = await page.evaluate(() => window.__GAME_TEST__.getState());
await dragLength(page, 5, 6);
await sleep(120);
const practiceLocked = await page.evaluate(() => window.__GAME_TEST__.getState());
await sleep(1800);

// 본판에서 문제 id의 생성 답을 읽어 오답 길이를 고르되, 제출은 실제 드래그 이벤트로만 한다.
const beforeWrong = await page.evaluate(() => window.__GAME_TEST__.getState());
const pieces = beforeWrong.problemId.split('-');
const answer = Number(pieces[3]);
const wrong = answer === 48 ? 1 : 48;
await dragLength(page, beforeWrong.selectedLength, wrong);
await sleep(120);
const afterWrong = await page.evaluate(() => window.__GAME_TEST__.getState());
await sleep(1400);
await dragLength(page, wrong, answer);
await sleep(120);
const afterRecovery = await page.evaluate(() => window.__GAME_TEST__.getState());

const result = {
  practiceBefore,
  practiceLocked,
  beforeWrong,
  chosenWrongLength: wrong,
  answerLength: answer,
  afterWrong,
  afterRecovery,
  errors,
  passed: practiceBefore.onboarding === true && practiceLocked.score > practiceBefore.score &&
    afterWrong.lives === beforeWrong.lives - 1 && afterRecovery.solved === beforeWrong.solved + 1 && errors.length === 0
};
console.log(JSON.stringify(result, null, 2));
await page.close();
await browser.close();
await server.close();
if (!result.passed) process.exitCode = 1;
