import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import puppeteer from 'puppeteer';
import {serveStatic} from '../../../../lib/static-server.mjs';

const ROOT = path.resolve(new URL('../../../../../', import.meta.url).pathname);
const PUBLIC = path.join(ROOT, 'public');
const GAME = path.join(PUBLIC, 'g/sil-taneun-geomi');
const OUT = path.resolve(new URL('./playthrough-frames/', import.meta.url).pathname);
const sleep = ms => new Promise(resolve => setTimeout(resolve, ms));
fs.mkdirSync(OUT, {recursive: true});

function resolveChrome() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const build of fs.readdirSync(base).sort().reverse()) {
    for (const rel of [
      'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
      'chrome-linux64/chrome'
    ]) {
      const candidate = path.join(base, build, rel);
      if (fs.existsSync(candidate)) return candidate;
    }
  }
  return undefined;
}

function artifactHash() {
  const files = [];
  (function walk(dir) {
    for (const name of fs.readdirSync(dir).sort()) {
      const file = path.join(dir, name);
      if (fs.statSync(file).isDirectory()) walk(file); else files.push(file);
    }
  })(GAME);
  const hash = crypto.createHash('sha256');
  for (const file of files) {
    hash.update(path.relative(GAME, file));
    hash.update(fs.readFileSync(file));
  }
  return hash.digest('hex');
}

// 화면에 적힌 조건만 독립 정수식으로 푼다. 게임의 answer 필드나 정답 훅은 읽지 않는다.
function solveVisiblePrompt(prompt) {
  let match = prompt.match(/선분 AD, DB, AE의 길이가 각각 (\d+) cm, (\d+) cm, (\d+) cm.*선분 EC의 길이/);
  if (match) return Number(match[3]) * Number(match[2]) / Number(match[1]);
  match = prompt.match(/선분 AD, DB, BC의 길이가 각각 (\d+) cm, (\d+) cm, (\d+) cm.*선분 DE의 길이/);
  if (match) return Number(match[3]) * Number(match[1]) / (Number(match[1]) + Number(match[2]));
  match = prompt.match(/선분 BC의 길이가 (\d+) cm.*선분 MN의 길이/);
  if (match) return Number(match[1]) / 2;
  match = prompt.match(/왼쪽의 l-m 구간은 (\d+) cm, m-n 구간은 (\d+) cm이고, 오른쪽의 l-m 구간은 (\d+) cm.*오른쪽 m-n 구간/);
  if (match) return Number(match[2]) * Number(match[3]) / Number(match[1]);
  match = prompt.match(/왼쪽의 l-m 구간은 (\d+) cm, m-n 구간은 (\d+) cm이고, 오른쪽의 m-n 구간은 (\d+) cm.*오른쪽 l-m 구간/);
  if (match) return Number(match[1]) * Number(match[3]) / Number(match[2]);
  throw new Error(`화면 발문을 풀 수 없음: ${prompt}`);
}

const xFor = value => 41 + (value - 1) * (308 / 23);

async function dragAnswer(page, value) {
  const start = value === 1 ? 2 : value - 1;
  await page.mouse.move(xFor(start), 620);
  await page.mouse.down();
  await sleep(30);
  await page.mouse.move(xFor(value), 620, {steps: 4});
  await sleep(30);
  await page.mouse.up();
}

async function state(page) {
  return page.evaluate(() => window.__GAME_TEST__.getState());
}

const server = await serveStatic(PUBLIC);
const browser = await puppeteer.launch({
  headless: true,
  executablePath: resolveChrome(),
  args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio']
});

async function run(label, firstMainAnswerWrong) {
  const page = await browser.newPage();
  const consoleErrors = [];
  await page.setViewport({width: 390, height: 844, deviceScaleFactor: 1});
  page.on('console', message => { if (message.type() === 'error') consoleErrors.push(message.text()); });
  page.on('pageerror', error => consoleErrors.push(String(error)));
  await page.goto(`${server.url}/g/sil-taneun-geomi/`, {waitUntil: 'load', timeout: 45000});
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, {timeout: 60000});
  await page.screenshot({path: path.join(OUT, `${label}-00-title.png`)});

  // 시작부터 실제 canvas pointer만 사용한다.
  await page.mouse.move(195, 465);
  await page.mouse.down();
  await sleep(110);
  await page.mouse.up();
  await sleep(550);
  let current = await state(page);
  if (!current.onboarding) throw new Error(`${label}: 실제 시작 탭이 연습으로 전이하지 않음`);
  await page.screenshot({path: path.join(OUT, `${label}-01-practice.png`)});
  await dragAnswer(page, solveVisiblePrompt(current.prompt));
  await sleep(2050);

  const actions = [];
  for (let route = 0; route < 9; route++) {
    current = await state(page);
    const answer = solveVisiblePrompt(current.prompt);
    if (!Number.isInteger(answer) || answer < 1 || answer > 24) throw new Error(`${label}: 정수 답 범위 오류 ${answer}`);
    if (current.prompt.includes('서로 평행한 세 직선')) {
      await page.screenshot({path: path.join(OUT, `${label}-route-${route + 1}-three-parallel.png`)});
    }

    if (route === 0 && firstMainAnswerWrong) {
      const wrong = answer === 24 ? 23 : answer + 1;
      const before = current;
      await dragAnswer(page, wrong);
      await sleep(1780);
      const afterWrong = await state(page);
      actions.push({route: 1, prompt: before.prompt, derivedAnswer: answer, pointerValue: wrong, result: 'wrong', before, after: afterWrong});
      if (afterWrong.firstAttemptTotal !== 1 || afterWrong.firstAttemptCorrect !== 0 || afterWrong.lives !== 2 || afterWrong.problemId !== before.problemId) {
        throw new Error(`${label}: 첫 오답 회복 조건 불일치 ${JSON.stringify(afterWrong)}`);
      }
      await page.screenshot({path: path.join(OUT, `${label}-02-first-wrong.png`)});
      await dragAnswer(page, answer);
      await sleep(2050);
      const afterRepair = await state(page);
      actions.push({route: 1, prompt: before.prompt, derivedAnswer: answer, pointerValue: answer, result: 'repaired', before: afterWrong, after: afterRepair});
      await page.screenshot({path: path.join(OUT, `${label}-03-first-repaired.png`)});
    } else {
      const before = current;
      await dragAnswer(page, answer);
      await sleep(2050);
      const after = await state(page);
      actions.push({route: route + 1, prompt: before.prompt, derivedAnswer: answer, pointerValue: answer, result: 'correct', before, after});
    }
  }

  const finalState = await state(page);
  await page.screenshot({path: path.join(OUT, `${label}-99-final.png`)});
  const expectedFirstCorrect = firstMainAnswerWrong ? 8 : 9;
  const passed = finalState.phase === 'clear' && finalState.solved === 9 &&
    finalState.firstAttemptTotal === 9 && finalState.firstAttemptCorrect === expectedFirstCorrect &&
    finalState.lives === (firstMainAnswerWrong ? 2 : 3) && consoleErrors.length === 0;
  await page.close();
  return {label, firstMainAnswerWrong, actions, finalState, consoleErrors, passed};
}

const recoveryPlay = await run('recovery-play', true);
const learnedSecondPlay = await run('learned-second-play', false);
const result = {
  runId: `sil-taneun-geomi-playthrough-${new Date().toISOString()}`,
  artifactHashSha256: artifactHash(),
  method: 'title, practice, all submissions use Chrome CDP canvas pointer; solver reads visible prompt only; no start/correct/wrong hooks and no answer field',
  recoveryPlay,
  learnedSecondPlay,
  passed: recoveryPlay.passed && learnedSecondPlay.passed
};
console.log(JSON.stringify(result, null, 2));
fs.writeFileSync(new URL('./playthrough-results.json', import.meta.url), JSON.stringify(result, null, 2));
await browser.close();
await server.close();
if (!result.passed) process.exitCode = 1;
