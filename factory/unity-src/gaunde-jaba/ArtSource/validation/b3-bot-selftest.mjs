#!/usr/bin/env node
// B3: 정답과 독립인 연타·순환·무작위·무입력 정책을 각각 200판 시뮬레이션한다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const GAMES = 200;
const ROUNDS = 6;
const LIVES = 3;
const CHANCE = (3 / 12 + 3 / 23) / ROUNDS;
const here = path.dirname(fileURLToPath(import.meta.url));
const out = path.join(here, 'b3-bot-results.json');

function hash(value) {
  let x = value >>> 0;
  x ^= x >>> 16;
  x = Math.imul(x, 0x7FEB352D) >>> 0;
  x ^= x >>> 15;
  x = Math.imul(x, 0x846CA68B) >>> 0;
  x ^= x >>> 16;
  return x >>> 0;
}

function answerAt(serial, round) {
  const counts = [5, 5, 6, 6, 7, 8];
  const count = counts[round];
  const buckets = count % 2 === 0 ? 23 : 12;
  const mixed = (Math.imul(serial, 97) + Math.imul(round, 31) + 11) >>> 0;
  const bucket = hash(mixed) % buckets;
  return count % 2 === 0 ? 2 + bucket : 2 * (1 + bucket);
}

function rng(seed) {
  let x = seed >>> 0;
  return () => {
    x ^= x << 13; x ^= x >>> 17; x ^= x << 5;
    return (x >>> 0) / 0x100000000;
  };
}

function run(name, choose, seed) {
  const random = rng(seed);
  let firstAttempts = 0;
  let firstCorrect = 0;
  let completions = 0;
  let totalSolved = 0;
  let inputSerial = 0;
  for (let game = 0; game < GAMES; game++) {
    let lives = LIVES;
    let solved = 0;
    while (lives > 0 && solved < ROUNDS) {
      const answer2 = answerAt(game + 1, solved);
      let first = true;
      while (lives > 0) {
        const input = choose({ game, solved, inputSerial: inputSerial++, random });
        if (input == null) { lives = 0; break; }
        if (first) {
          firstAttempts++;
          if (input === answer2) firstCorrect++;
          first = false;
        }
        if (input === answer2) { solved++; break; }
        lives--;
      }
    }
    totalSolved += solved;
    if (solved === ROUNDS && lives > 0) completions++;
  }
  const firstAttemptRate = firstAttempts ? firstCorrect / firstAttempts : 0;
  const significantUpperGate = firstAttempts
    ? CHANCE + 2.326 * Math.sqrt(CHANCE * (1 - CHANCE) / firstAttempts)
    : CHANCE;
  return {
    name,
    games: GAMES,
    firstCorrect,
    firstAttempts,
    firstAttemptRate,
    chanceRate: CHANCE,
    significantUpperGate,
    completions,
    completionRate: completions / GAMES,
    meanProgress: totalSolved / GAMES,
  };
}

// 레일 중앙 연타와 네 구간 중심 순환. 양 끝점만 고르는 비현실적 정책은 쓰지 않는다.
const cycleValues = [4, 10, 17, 23];
const results = [
  run('spam', () => 13, 0x6A010001),
  run('cycle', ({ inputSerial }) => cycleValues[inputSerial % cycleValues.length], 0x6A010002),
  run('random', ({ random }) => 2 + Math.floor(random() * 23), 0x6A010003),
  run('no-input', () => null, 0x6A010004),
];
for (const r of results) {
  r.passed = r.name === 'no-input'
    ? r.firstAttempts === 0 && r.meanProgress === 0 && r.completionRate === 0
    : r.firstAttemptRate <= CHANCE;
}
const report = {
  passed: results.every((r) => r.passed),
  gamesPerBot: GAMES,
  chanceModel: '(3×1/12 + 3×1/23) ÷ 6',
  chanceRate: CHANCE,
  results,
};
fs.writeFileSync(out, `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify(report, null, 2));
if (!report.passed) process.exitCode = 1;
