#!/usr/bin/env node
/**
 * stick-a-whole — sampleProblems() 독립 재검산.
 *
 * mathcheck 가 fail 시킨 지점(「두 조각」 조건이 없어 목표와 같은 단일 조각 풀이도 성립)을
 * 다시 검사한다. 게임 코드의 pairs()/sample() 을 신뢰하지 않고 **여기서 새로 열거**한다.
 *
 *   node docs/validation/stick-a-whole/verify-samples.mjs
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import puppeteer from 'puppeteer';
import { serveStatic } from '../../../factory/lib/static-server.mjs';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(HERE, '../../..');
function resolveChrome() {
  if (process.env.PUPPETEER_EXECUTABLE_PATH) return process.env.PUPPETEER_EXECUTABLE_PATH;
  const base = path.join(process.env.HOME || '', '.cache/puppeteer/chrome');
  if (!fs.existsSync(base)) return undefined;
  for (const b of fs.readdirSync(base).sort().reverse())
    for (const rel of ['chrome-mac-arm64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
                       'chrome-mac-x64/Google Chrome for Testing.app/Contents/MacOS/Google Chrome for Testing',
                       'chrome-linux64/chrome']) {
      const p = path.join(base, b, rel);
      if (fs.existsSync(p)) return p;
    }
  return undefined;
}

const { url, close } = await serveStatic(path.join(ROOT, 'public'));
const browser = await puppeteer.launch({ headless: 'new', executablePath: resolveChrome(), args: ['--no-sandbox', '--use-gl=swiftshader'] });
const page = await browser.newPage();
await page.goto(`${url}/g/stick-a-whole/`, { waitUntil: 'networkidle0' });
await page.waitForFunction('window.__GAME_TEST__?.ready === true', { timeout: 15000 });

const problems = {};
for (const n of [40, 17, 63]) problems[n] = await page.evaluate(k => window.__GAME_TEST__.sampleProblems(k), n);

const errors = [];
const stats = { checked: 0, soloEqualsTarget: 0, twoPieceStated: 0 };

for (const [n, list] of Object.entries(problems)) {
  if (list.length !== Number(n)) errors.push(`n=${n}: ${list.length}개만 반환`);
  for (const p of list) {
    stats.checked++;
    const hand = p.hand.map(c => c.n);
    const d = p.target.d, t = p.target.n;

    // ① 발문이 「두 조각」 조건을 명시하는가 — mathcheck 가 지적한 바로 그 조건
    const stated = /반드시\s*두\s*조각/.test(p.prompt);
    if (stated) stats.twoPieceStated++;
    else errors.push(`${p.id}: 발문에 두 조각 조건 없음 → "${p.prompt}"`);

    // ② 목표와 같은 단일 조각이 손패에 있는가 (있어도 조건이 명시되면 모호하지 않다)
    if (hand.includes(t)) stats.soloEqualsTarget++;

    // ③ 이 문항의 「두 조각」 정답을 여기서 새로 전수 열거해 게임 주장과 대조
    const mine = [];
    for (let i = 0; i < hand.length; i++)
      for (let j = i + 1; j < hand.length; j++)
        if (p.hand[i].d === d && p.hand[j].d === d && hand[i] + hand[j] === t) mine.push(`${i + 1}+${j + 1}`);
    const claimed = (p.correctPairs || []).map(([i, j]) => `${i + 1}+${j + 1}`);
    if (mine.join(',') !== claimed.join(',')) errors.push(`${p.id}: 정답쌍 불일치 내계산=${mine} 게임주장=${claimed}`);
    if (mine.length !== 2) errors.push(`${p.id}: 정답쌍이 ${mine.length}개 (설계는 정확히 2개)`);

    // ④ answer 가 실제 정답쌍 중 하나를 가리키는가 · choices 안에 있는가
    const m = /^(\d+)번과 (\d+)번 조각을 첫 칸부터 붙이기$/.exec(p.answer);
    if (!m) errors.push(`${p.id}: answer 형식 이상 "${p.answer}"`);
    else if (!mine.includes(`${m[1]}+${m[2]}`)) errors.push(`${p.id}: answer 가 정답쌍이 아님 ${p.answer}`);
    if (!p.choices.includes(p.answer)) errors.push(`${p.id}: choices 에 answer 없음`);
    if (new Set(p.choices).size !== p.choices.length) errors.push(`${p.id}: choices 중복`);

    // ⑤ 오답 선택지가 실제로 오답인가 (같은 두 조각 쌍을 다른 문구로 제시하지 않는가)
    for (const ch of p.choices) {
      if (ch === p.answer) continue;
      const mm = /^(\d+)번과 (\d+)번 조각을 첫 칸부터 붙이기$/.exec(ch);
      if (mm && mine.includes(`${mm[1]}+${mm[2]}`)) errors.push(`${p.id}: 오답 선택지가 실제 정답 "${ch}"`);
    }

    // ⑥ 수치 정합
    if (Math.abs(p.answerNumeric - t / d) > 1e-9) errors.push(`${p.id}: answerNumeric ${p.answerNumeric} ≠ ${t}/${d}`);
    // ⑦ 3학년 2학기 범위: 분모 4 고정, 목표는 3~7칸(진분수·가분수 경계 포함)
    if (d !== 4) errors.push(`${p.id}: 분모 ${d} — 슬롯 범위 밖`);
    if (t < 3 || t > 7) errors.push(`${p.id}: 목표 ${t} — 범위 밖`);
  }
}

const out = {
  checked_at: new Date().toISOString(),
  samples: Object.fromEntries(Object.entries(problems).map(([k, v]) => [k, v.length])),
  stats,
  two_piece_condition_rate: `${stats.twoPieceStated}/${stats.checked}`,
  solo_equals_target_rate: `${stats.soloEqualsTarget}/${stats.checked}`,
  errors,
  verdict: errors.length ? 'fail' : 'pass',
};
fs.writeFileSync(path.join(HERE, 'verify-samples.json'), JSON.stringify(out, null, 2));
console.log(JSON.stringify(out, null, 2).slice(0, 3000));
await browser.close(); await close();
process.exit(errors.length ? 1 : 0);
