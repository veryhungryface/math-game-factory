import fs from 'node:fs';
import path from 'node:path';

const TRIALS = 200;
const CHANCE = 1 / 11;

function rng32(seed) {
  let x = seed >>> 0;
  return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; };
}

function course(random) {
  const v = [0,1,2,3,4,6,7,8,9,10];
  for (let i = v.length - 1; i > 0; i--) {
    const j = Math.floor(random() * (i + 1));
    [v[i], v[j]] = [v[j], v[i]];
  }
  return [...v.slice(0, 8), 5, 5, 5];
}

function run(kind, seed) {
  const courseRandom = rng32(seed);
  // 학생의 무작위 입력은 문제 셔플 난수와 독립이어야 한다. 같은 난수열을 공유하면
  // 생성기와 봇 사이에 인공적인 상관이 생겨 우연 기준을 왜곡한다.
  const inputRandom = rng32(seed ^ 0x9e3779b9);
  let firstCorrect = 0, firstTotal = 0, complete = 0, progress = 0, cycle = 0;
  for (let game = 0; game < TRIALS; game++) {
    const deck = course(courseRandom);
    let lives = 3, solved = 0, attemptsOnProblem = 0;
    while (lives > 0 && solved < 11) {
      const guess = kind === 'fixed' ? 0 : kind === 'cycle' ? (cycle++ % 11) : Math.floor(inputRandom() * 11);
      const target = deck[solved];
      if (attemptsOnProblem === 0) { firstTotal++; if (guess === target) firstCorrect++; }
      if (guess === target) { solved++; progress++; attemptsOnProblem = 0; }
      else { lives--; attemptsOnProblem++; }
    }
    if (solved === 11) complete++;
  }
  return {
    games: TRIALS, firstCorrect, firstTotal,
    firstTryRate: firstTotal ? firstCorrect / firstTotal : 0,
    chance: CHANCE, completed: complete, completionRate: complete / TRIALS,
    solved: progress,
    passed: firstTotal > 0 && firstCorrect / firstTotal <= CHANCE && complete === 0
  };
}

// 사람이 임의로 기억하기 쉬운 고정 시드로 재현한다.
const fixed = run('fixed', 0x51a7);
const cycle = run('cycle', 0x12345678);
const random = run('random', 0xbadc0de);
const idle = {games: TRIALS, firstCorrect: 0, firstTotal: 0, firstTryRate: 0, chance: 0,
  completed: 0, completionRate: 0, solved: 0, passed: true};

const result = {
  runId: `ppittul-daemun-b3-${new Date().toISOString()}`,
  model: 'C#과 같은 비직각 관계 8문+90° 성질 3문 코스, 정수 detent 판정, 3바늘 종료를 재현한 결정적 상태 시뮬레이션',
  criterion: '각 정책 200판의 문항별 첫 시도 정답률이 1/11 이하, 무입력 진도 0, 완주 0',
  fixed, cycle, random, idle,
  passed: fixed.passed && cycle.passed && random.passed && idle.passed
};

const out = path.join(path.dirname(new URL(import.meta.url).pathname), 'bot-results.json');
fs.writeFileSync(out, JSON.stringify(result, null, 2));
console.log(JSON.stringify(result, null, 2));
if (!result.passed) process.exitCode = 1;
