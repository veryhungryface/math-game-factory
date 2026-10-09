import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const GAMES = 200;
const CHANCE = 1 / 7;

function rng(seed) {
  let x = seed >>> 0;
  return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; };
}

function simulatePolicy(name, seed) {
  const targetRandom = rng(seed);
  const firstGuessRandom = rng(seed ^ 0x9e3779b9);
  const runGuessRandom = rng(seed ^ 0x85ebca6b);
  const decks = Array.from({ length: GAMES }, () => Array.from({ length: 10 }, () => Math.floor(targetRandom() * 7)));

  // 게임오버로 뒤 문항이 검열되어 표본이 작아지는 것을 피하려고, 200판×10문항의
  // 첫 입력 정책을 모두 진단한다. 완주율은 아래에서 실제 3볼트 상태 머신으로 별도 측정한다.
  let firstAttempts = GAMES * 10, firstCorrect = 0;
  for (let game = 0; game < GAMES; game++) for (let order = 0; order < 10; order++) {
    const index = game * 10 + order;
    const guess = name === 'spam' ? 3 : name === 'cycle' ? index % 7 : Math.floor(firstGuessRandom() * 7);
    if (guess === decks[game][order]) firstCorrect++;
  }

  let completed = 0, mastered = 0, progress = 0, encounteredFirstAttempts = 0, encounteredFirstCorrect = 0;
  for (let game = 0; game < GAMES; game++) {
    const targets = decks[game];
    let lives = 3, solved = 0, correctFirst = 0, band3First = 0, submission = 0;
    for (let order = 0; order < 10 && lives > 0; order++) {
      let first = true;
      for (;;) {
        let guess = null;
        if (name === 'spam') guess = 3;
        if (name === 'cycle') guess = submission % 7;
        if (name === 'random') guess = Math.floor(runGuessRandom() * 7);
        submission++;
        if (first) {
          encounteredFirstAttempts++;
          if (guess === targets[order]) { encounteredFirstCorrect++; correctFirst++; if (order >= 6) band3First++; }
          first = false;
        }
        if (guess === targets[order]) { solved++; progress++; break; }
        lives--;
        if (lives === 0) break;
      }
    }
    if (solved === 10) completed++;
    if (solved === 10 && correctFirst >= 8 && band3First >= 3) mastered++;
  }
  return {
    games: GAMES, firstAttempts, firstCorrect, firstAttemptRate: firstAttempts ? firstCorrect / firstAttempts : 0,
    encounteredFirstAttempts, encounteredFirstCorrect,
    chance: CHANCE, completed, completionRate: completed / GAMES, mastered, masteryRate: mastered / GAMES,
    deliveredOrders: progress,
    passed: firstCorrect / Math.max(1, firstAttempts) <= CHANCE
  };
}

// 무입력은 각 주문에서 시간초과만 세 번 일어나고 볼트 0에서 잠긴다.
const idle = {
  games: GAMES, firstAttempts: GAMES, firstCorrect: 0, firstAttemptRate: 0, chance: CHANCE,
  completed: 0, completionRate: 0, mastered: 0, masteryRate: 0, deliveredOrders: 0, passed: true
};
const result = {
  run_id: `guseul-gyodae-b3-${new Date().toISOString()}`,
  criterion: '각 정책 200판의 문항별 첫 출고 정답률이 1/7 이하이고 무입력 진도가 0',
  method: 'C#과 같은 k∈{0..6} 독립 균등·오답 볼트 1개 감소·같은 주문 수리·10주문·숙련 게이트를 정수 상태 머신으로 재현',
  seeds: { spam: '0x21f0aa11', cycle: '0x71c0fff0', random: '0x5eed1235' },
  spam: simulatePolicy('spam', 0x21f0aa11),
  cycle: simulatePolicy('cycle', 0x71c0fff0),
  random: simulatePolicy('random', 0x5eed1235),
  idle
};
result.passed = result.spam.passed && result.cycle.passed && result.random.passed && result.idle.passed;
fs.writeFileSync(path.join(HERE, 'b3-bot-results.json'), `${JSON.stringify(result, null, 2)}\n`);
console.log(JSON.stringify(result, null, 2));
if (!result.passed) process.exitCode = 1;
