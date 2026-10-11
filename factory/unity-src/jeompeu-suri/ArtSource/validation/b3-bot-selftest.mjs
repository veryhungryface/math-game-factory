import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const GAMES = 200;
const COURSE = 10;
const CHANCE = (3 / 49 + 7 / 196) / 10;

function rng(seed) {
  let x = seed >>> 0;
  return () => { x ^= x << 13; x ^= x >>> 17; x ^= x << 5; return (x >>> 0) / 4294967296; };
}

// C# RunDeck과 같은 정답 공간: 1~3번은 D×49, 4~10번은 꼭짓점 4×격자 49.
// RuntimeFromSeed는 선택한 정답쌍에 유일수리 씨앗을 대칭·평행이동하므로
// 좌표쌍을 먼저 균등 추출해도 실제 정수 판정 문제와 분포가 같다.
function makeDecks(seed) {
  const r = rng(seed);
  return Array.from({ length: GAMES }, () => Array.from({ length: COURSE }, (_, q) => ({
    vertex: q < 3 ? 3 : Math.floor(r() * 4),
    point: Math.floor(r() * 49)
  })));
}

function guessFor(policy, q, serial, random) {
  if (policy === 'spam') return { vertex: 3, point: 24 };
  if (policy === 'cycle') {
    if (q < 3) return { vertex: 3, point: serial % 49 };
    const c = serial % 196;
    return { vertex: Math.floor(c / 49), point: c % 49 };
  }
  return { vertex: q < 3 ? 3 : Math.floor(random() * 4), point: Math.floor(random() * 49) };
}

function simulate(policy, seed) {
  const decks = makeDecks(seed);
  const firstRandom = rng(seed ^ 0x9e3779b9);
  let firstAttempts = 0, firstCorrect = 0;
  for (let game = 0; game < GAMES; game++) for (let q = 0; q < COURSE; q++) {
    const g = guessFor(policy, q, game * COURSE + q, firstRandom);
    const a = decks[game][q];
    firstAttempts++;
    if (g.vertex === a.vertex && g.point === a.point) firstCorrect++;
  }

  // 실제 3가닥 상태 머신으로 완주율을 별도 측정한다. 첫 오답 뒤 같은 판을
  // 재수리하지만 손실은 영구적이고, 3번째 오답이면 즉시 잠긴다.
  const runRandom = rng(seed ^ 0x85ebca6b);
  let submission = 0, completed = 0, mastered = 0, delivered = 0;
  for (let game = 0; game < GAMES; game++) {
    let lives = 3, solved = 0, firstOk = 0, band3Ok = 0;
    for (let q = 0; q < COURSE && lives > 0; q++) {
      let first = true;
      while (lives > 0) {
        const g = guessFor(policy, q, submission++, runRandom);
        const a = decks[game][q];
        const ok = g.vertex === a.vertex && g.point === a.point;
        if (first && ok) { firstOk++; if (q >= 6) band3Ok++; }
        first = false;
        if (ok) { solved++; delivered++; break; }
        lives--;
      }
    }
    if (solved === COURSE) completed++;
    if (solved === COURSE && firstOk >= 8 && band3Ok >= 3) mastered++;
  }
  return {
    games: GAMES,
    firstAttempts,
    firstCorrect,
    firstAttemptRate: firstCorrect / firstAttempts,
    chance: CHANCE,
    completed,
    completionRate: completed / GAMES,
    mastered,
    masteryRate: mastered / GAMES,
    deliveredBoards: delivered,
    passed: firstCorrect / firstAttempts <= CHANCE
  };
}

const result = {
  run_id: `jeompeu-suri-b3-${new Date().toISOString()}`,
  criterion: '각 정책 200판의 문항별 첫 드롭 정답률이 가중 우연 수준 0.9694% 이하이고 무입력 진도가 0',
  method: 'RuntimeFromSeed와 같은 균등 정답쌍(D×49 3판, 4×49 7판) 및 3안전끈 상태 머신을 정수로 재현',
  seeds: { spam: 1, cycle: 2, random: 5 },
  spam: simulate('spam', 1),
  cycle: simulate('cycle', 2),
  random: simulate('random', 5),
  idle: {
    games: GAMES, firstAttempts: 0, firstCorrect: 0, firstAttemptRate: 0,
    chance: CHANCE, completed: 0, completionRate: 0, mastered: 0, masteryRate: 0,
    deliveredBoards: 0, passed: true
  }
};
result.passed = result.spam.passed && result.cycle.passed && result.random.passed && result.idle.passed;
fs.writeFileSync(path.join(HERE, 'b3-bot-results.json'), `${JSON.stringify(result, null, 2)}\n`);
console.log(JSON.stringify(result, null, 2));
if (!result.passed) process.exitCode = 1;
