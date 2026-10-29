#!/usr/bin/env node
// B3 무뇌 게이트: 정답을 읽지 않는 네 정책을 각 200판, 실제 게임의 18개 균등 각도·3끈·8주름 규칙으로 실행한다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const GAMES = 200;
const GOAL = 8;
const CHANCE = 1 / 18;
const here = path.dirname(fileURLToPath(import.meta.url));
const out = path.join(here, 'b3-bot-results.json');

function rng(seed) {
  let x = seed >>> 0;
  return () => {
    x ^= x << 13; x ^= x >>> 17; x ^= x << 5;
    return (x >>> 0) / 0x100000000;
  };
}

function run(name, choose, answerSeed, inputSeed) {
  const answerRandom = rng(answerSeed);
  const inputRandom = rng(inputSeed);
  let firstCorrect = 0;
  let firstAttempts = 0;
  let completions = 0;
  let progress = 0;
  let inputSerial = 0;
  for (let game = 0; game < GAMES; game++) {
    let lives = 3;
    let solved = 0;
    let target = 5 + Math.floor(answerRandom() * 18) * 5;
    let first = true;
    while (lives > 0 && solved < GOAL) {
      const selected = choose({ game, solved, inputSerial: inputSerial++, random: inputRandom });
      if (selected == null) break;
      if (first) {
        firstAttempts++;
        if (selected === target) firstCorrect++;
      }
      if (selected === target) {
        solved++;
        if (solved < GOAL) target = 5 + Math.floor(answerRandom() * 18) * 5;
        first = true;
      } else {
        lives--;
        first = false;
      }
    }
    progress += solved;
    if (solved === GOAL && lives > 0) completions++;
  }
  const rate = firstAttempts ? firstCorrect / firstAttempts : 0;
  return {
    name,
    games: GAMES,
    firstCorrect,
    firstAttempts,
    firstAttemptRate: rate,
    chanceRate: CHANCE,
    completions,
    completionRate: completions / GAMES,
    meanProgress: progress / GAMES,
    passed: name === 'no-input' ? firstAttempts === 0 && progress === 0 : rate <= CHANCE,
  };
}

const cycle = [20, 40, 60, 80];
const results = [
  run('spam', () => 45, 0x4E414C11, 0x57494E11),
  run('cycle', ({ inputSerial }) => cycle[inputSerial % cycle.length], 0x4E414C23, 0x57494E22),
  run('random', ({ random }) => 5 + Math.floor(random() * 18) * 5, 0x4E414C33, 0x57494E33),
  run('no-input', () => null, 0x4E414C44, 0x57494E44),
];
const report = {
  passed: results.every((r) => r.passed),
  gamesPerBot: GAMES,
  chanceModel: '18개 동일한 5° 눈금 중 1개 = 1/18',
  chanceRate: CHANCE,
  results,
};
fs.writeFileSync(out, `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify(report, null, 2));
if (!report.passed) process.exitCode = 1;
