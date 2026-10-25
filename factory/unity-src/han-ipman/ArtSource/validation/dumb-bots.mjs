#!/usr/bin/env node
// 기획 계약 B3: 판단 정보와 독립인 입력 4종을 각각 200판 시뮬레이션한다.
// 게임과 같은 q∈[1,23], 목숨 3, 8접시 종료 규칙을 사용한다.
import fs from 'node:fs';
import path from 'node:path';

const GAMES = 200;
const TARGET = 8;
const START_LIVES = 3;
const CHANCE = 1 / 23;
const OUT = path.join(process.cwd(), 'factory/work/qa/han-ipman/dumb-bots.json');

function rng(seed) {
  let x = seed >>> 0;
  return () => {
    x ^= x << 13; x ^= x >>> 17; x ^= x << 5;
    return (x >>> 0) / 0x100000000;
  };
}

function runBot(name, seed, choose) {
  const problemRandom = rng(seed ^ 0x9e3779b9);
  const inputRandom = rng(seed ^ 0x85ebca6b);
  let firstAttempts = 0;
  let firstCorrect = 0;
  let completed = 0;
  let totalSolved = 0;
  let inputSerial = 0;

  for (let game = 0; game < GAMES; game++) {
    let lives = START_LIVES;
    let solved = 0;
    while (lives > 0 && solved < TARGET) {
      const answer = 1 + Math.floor(problemRandom() * 23);
      let first = true;
      while (lives > 0) {
        const input = choose({ game, solved, inputSerial: inputSerial++, random: inputRandom });
        if (input == null) { lives = 0; break; }
        if (first) {
          firstAttempts++;
          if (input === answer) firstCorrect++;
          first = false;
        }
        if (input === answer) { solved++; break; }
        lives--;
      }
    }
    totalSolved += solved;
    if (solved === TARGET && lives > 0) completed++;
  }

  return {
    name,
    games: GAMES,
    firstCorrect,
    firstAttempts,
    firstAttemptRate: firstAttempts ? firstCorrect / firstAttempts : 0,
    chanceRate: CHANCE,
    completionRate: completed / GAMES,
    meanProgress: totalSolved / GAMES,
  };
}

const results = [
  runBot('spam', 0x48495001, () => 1),
  runBot('cycle', 0x48495002, ({ inputSerial }) => 1 + (inputSerial % 4)),
  runBot('random', 0x48495003, ({ random }) => 1 + Math.floor(random() * 23)),
  runBot('no-input', 0x48495004, () => null),
];

// 200판 표본에서는 우연 수준을 조금 넘는 관측값 자체보다 유의한 초과가 결함이다.
// 단측 99% 상한(정규 근사)을 넘지 않는지와 무입력 진도 0을 함께 게이트한다.
for (const result of results) {
  const n = Math.max(1, result.firstAttempts);
  const standardError = Math.sqrt(CHANCE * (1 - CHANCE) / n);
  result.significantUpperGate = CHANCE + 2.326 * standardError;
  result.passed = result.name === 'no-input'
    ? result.firstAttempts === 0 && result.meanProgress === 0 && result.completionRate === 0
    : result.firstAttemptRate <= result.significantUpperGate;
}

const report = { passed: results.every((result) => result.passed), gamesPerBot: GAMES, results };
fs.mkdirSync(path.dirname(OUT), { recursive: true });
fs.writeFileSync(OUT, `${JSON.stringify(report, null, 2)}\n`);
console.log(JSON.stringify(report, null, 2));
if (!report.passed) process.exitCode = 1;
