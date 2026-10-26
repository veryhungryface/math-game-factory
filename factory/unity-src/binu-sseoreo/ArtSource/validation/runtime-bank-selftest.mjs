#!/usr/bin/env node
// 최종 WebGL이 실제로 푸시한 624문항의 화면 발문·답·정수 판정 의미를 전수 검사한다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../../lib/static-server.mjs';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../../../../..');
const publicDir = path.join(root, 'public');
const out = path.join(here, 'runtime-bank-results.json');

function chromePath() {
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  for (const build of fs.readdirSync(base).sort().reverse()) for (const rel of [
    'chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
    'chrome-linux64/chrome',
  ]) {
    const candidate = path.join(base, build, rel);
    if (fs.existsSync(candidate)) return candidate;
  }
}

const server = await serveStatic(publicDir);
const browser = await puppeteer.launch({ headless: true, executablePath: chromePath(), args: ['--no-sandbox', '--disable-dev-shm-usage', '--mute-audio'] });
let result;
try {
  const page = await browser.newPage();
  await page.goto(`${server.url}/g/binu-sseoreo/`, { waitUntil: 'load', timeout: 45000 });
  await page.waitForFunction(() => window.__GAME_TEST__?.ready, { timeout: 30000 });
  const bank = await page.evaluate(() => window.__GAME_TEST__.sampleProblems(9999));
  const errors = [];
  const ids = new Set();
  let rect = 0, cylinder = 0, upper = 0;
  for (const p of bank) {
    if (!p.id || ids.has(p.id)) errors.push(`duplicate id ${p.id}`); else ids.add(p.id);
    if (!p.prompt.includes('전체 높이가 25 cm') || !p.prompt.includes('바닥에서 몇 cm 높이')) errors.push(`missing reference ${p.id}`);
    if (p.choices !== null) errors.push(`choices must be null ${p.id}`);
    const cut = Number.parseInt(p.answer, 10);
    if (cut !== p.answerNumeric || cut < 1 || cut > 24) errors.push(`answer mismatch ${p.id}`);
    const isUpper = p.prompt.includes('위쪽 조각');
    if (isUpper) upper++;
    const pieceHeight = isUpper ? 25 - cut : cut;
    let base, target;
    const rm = /가로 (\d+) cm, 세로 (\d+) cm인 직사각형인 사각기둥/.exec(p.prompt);
    if (rm) {
      rect++; base = Number(rm[1]) * Number(rm[2]);
      const vm = /부피가 (\d+) cm³/.exec(p.prompt); target = vm && Number(vm[1]);
      if (!p.prompt.includes('직사각형인 사각기둥')) errors.push(`ambiguous rectangle ${p.id}`);
    } else {
      const cm = /반지름의 길이가 (\d+) cm인 원기둥/.exec(p.prompt);
      const vm = /부피가 (\d*)π cm³/.exec(p.prompt);
      if (!cm || !vm) { errors.push(`unparsed prompt ${p.id}`); continue; }
      cylinder++; base = Number(cm[1]) ** 2; target = vm[1] === '' ? 1 : Number(vm[1]);
    }
    if (base * pieceHeight !== target) errors.push(`equation mismatch ${p.id}: ${base}*${pieceHeight}!=${target}`);
  }
  result = { passed: bank.length === 624 && ids.size === 624 && errors.length === 0, total: bank.length, unique: ids.size, rect, cylinder, upper, errors };
  await page.close();
} finally {
  await browser.close(); await server.close();
}
fs.writeFileSync(out, `${JSON.stringify(result, null, 2)}\n`);
console.log(JSON.stringify(result, null, 2));
if (!result.passed) process.exitCode = 1;
