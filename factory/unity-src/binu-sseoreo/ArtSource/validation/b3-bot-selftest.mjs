#!/usr/bin/env node
// B3: 정답 정보와 독립인 입력 4종을 각각 200판, 첫 완료 제스처 기준으로 시뮬레이션한다.
// 실제 브라우저 pointer 경로의 유효성은 qa.mjs input.real과 pointer-smoke.mjs가 별도로 검증한다.
import fs from 'node:fs';
import path from 'node:path';

const GAMES = 200;
const ORDERS = 8;
const LIVES = 3;
const CHANCE = 1 / 24;
const out = path.join(process.cwd(), 'factory/unity-src/binu-sseoreo/ArtSource/validation/b3-bot-results.json');

function rng(seed) {
  let x = seed >>> 0;
  return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 0x100000000; };
}

function answerAt(game, order) {
  // 입력 정책·시작 손잡이와 독립인 32비트 혼합. 선형 순환 입력과의 위상 결합을 피한다.
  let x = (Math.imul(game + 1, 0x9E3779B1) ^ Math.imul(order + 1, 0x85EBCA77) ^ 0xB10A2026) >>> 0;
  x ^= x >>> 16; x = Math.imul(x, 0x7FEB352D) >>> 0; x ^= x >>> 15; x = Math.imul(x, 0x846CA68B) >>> 0; x ^= x >>> 16;
  return 1 + (x % 24);
}

function run(name, choose, seed) {
  const random = rng(seed);
  let firstAttempts = 0, firstCorrect = 0, completions = 0, totalSolved = 0, inputSerial = 0;
  for (let game = 0; game < GAMES; game++) {
    let lives = LIVES, solved = 0;
    while (lives > 0 && solved < ORDERS) {
      const answer = answerAt(game, solved);
      let first = true;
      while (lives > 0) {
        const input = choose({ game, solved, inputSerial: inputSerial++, random });
        if (input == null) { lives = 0; break; }
        if (first) { firstAttempts++; if (input === answer) firstCorrect++; first = false; }
        if (input === answer) { solved++; break; }
        lives--;
      }
    }
    totalSolved += solved;
    if (solved === ORDERS && lives > 0) completions++;
  }
  return {
    name,
    games: GAMES,
    firstCorrect,
    firstAttempts,
    firstAttemptRate: firstAttempts ? firstCorrect / firstAttempts : 0,
    chanceRate: CHANCE,
    completionRate: completions / GAMES,
    meanProgress: totalSolved / GAMES,
  };
}

const results = [
  run('spam', () => 1, 0xB10A0001),
  run('cycle', ({ inputSerial }) => 1 + (inputSerial % 4), 0xB10A0002),
  run('random', ({ random }) => 1 + Math.floor(random() * 24), 0xB10A0003),
  run('no-input', () => null, 0xB10A0004),
];

for (const r of results) {
  const n = Math.max(1, r.firstAttempts);
  r.significantUpperGate = CHANCE + 2.326 * Math.sqrt(CHANCE * (1 - CHANCE) / n);
  r.passed = r.name === 'no-input'
    ? r.firstAttempts === 0 && r.meanProgress === 0 && r.completionRate === 0
    : r.firstAttemptRate <= r.significantUpperGate;
}
const report = { passed: results.every((r) => r.passed), gamesPerBot: GAMES, chanceRate: CHANCE, results };
fs.writeFileSync(out, `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify(report, null, 2));
if (!report.passed) process.exitCode = 1;
