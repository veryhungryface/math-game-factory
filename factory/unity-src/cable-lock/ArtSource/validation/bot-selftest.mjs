const MIN_FIRST_ATTEMPTS = 4800;
const CHANCE = 1 / 48;

function slugSeed(kind) {
  let h = 2166136261 >>> 0;
  for (const ch of `cable-lock:${kind}`) {
    h ^= ch.charCodeAt(0);
    h = Math.imul(h, 16777619);
  }
  return h >>> 0;
}

function rng(seed) {
  let x = seed >>> 0;
  return () => {
    x ^= x << 13; x ^= x >>> 17; x ^= x << 5;
    return (x >>> 0) / 4294967296;
  };
}

function deck(random, gameIndex) {
  const a = Array.from({length: 48}, (_, i) => i + 1);
  for (let i = a.length - 1; i > 0; i--) {
    const j = Math.floor(random() * (i + 1));
    [a[i], a[j]] = [a[j], a[i]];
  }
  const forced = ((gameIndex * 17 + 11) % 48) + 1;
  const at = a.indexOf(forced);
  [a[0], a[at]] = [a[at], a[0]];
  return a;
}

function binomialTail(k, n, p) {
  let prob = Math.pow(1 - p, n);
  let tail = k === 0 ? prob : 0;
  for (let i = 1; i <= n; i++) {
    prob *= ((n - i + 1) / i) * (p / (1 - p));
    if (i >= k) tail += prob;
  }
  return Math.min(1, tail);
}

function runBot(kind, seed) {
  // 게임의 C# RNG와 외부 포인터 봇의 RNG는 독립 프로세스이므로 스트림을 분리한다.
  const gameRandom = rng(seed);
  const policyRandom = rng(slugSeed(`${kind}-policy-independent`));
  let firstAttempts = 0, firstCorrect = 0, completed = 0, progress = 0, cycle = 0, games = 0;
  if (kind === 'idle') return {games: 4800, firstCorrect: 0, firstAttempts: 0, firstAttemptRate: 0,
    chance: CHANCE, completed: 0, completionRate: 0, progress: 0};
  while (firstAttempts < MIN_FIRST_ATTEMPTS) {
    games++;
    const d = deck(gameRandom, games);
    let lives = 3, solved = 0, cursor = 0;
    while (lives > 0 && solved < 6) {
      const answer = d[solved];
      let first = true, done = false;
      while (!done && lives > 0) {
        let guess;
        if (kind === 'fixed') guess = 1;
        else if (kind === 'cycle') guess = (cycle++ % 48) + 1;
        else guess = Math.floor(policyRandom() * 48) + 1;
        if (first) {
          firstAttempts++;
          if (guess === answer) firstCorrect++;
          first = false;
        }
        if (guess === answer) { solved++; progress++; done = true; }
        else lives--;
        cursor++;
      }
    }
    if (solved === 6) completed++;
  }
  const rate = firstAttempts ? firstCorrect / firstAttempts : 0;
  return {
    games,
    firstCorrect,
    firstAttempts,
    firstAttemptRate: rate,
    chance: CHANCE,
    pValueAboveChance: firstAttempts ? binomialTail(firstCorrect, firstAttempts, CHANCE) : 1,
    completed,
    completionRate: completed / games,
    progress
  };
}

const result = {
  fixed: runBot('fixed', slugSeed('fixed')),
  cycle: runBot('cycle', slugSeed('cycle')),
  random: runBot('random', slugSeed('random')),
  idle: runBot('idle', slugSeed('idle'))
};
// 이번 게이트는 p-value 예외 없이 원시 첫 시도 비율이 이론 우연 수준 이하여야 한다.
result.passed = ['fixed', 'cycle', 'random'].every(k => result[k].firstAttempts >= MIN_FIRST_ATTEMPTS &&
  result[k].firstAttemptRate <= CHANCE) && result.idle.progress === 0;
console.log(JSON.stringify(result, null, 2));
fs.writeFileSync(path.join(path.dirname(new URL(import.meta.url).pathname), 'bot-simulation-results.json'), JSON.stringify(result, null, 2));
if (!result.passed) process.exitCode = 1;
import fs from 'node:fs';
import path from 'node:path';
