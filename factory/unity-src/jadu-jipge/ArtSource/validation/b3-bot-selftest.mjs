import fs from 'node:fs';

// JaduRules의 정수 분포를 그대로 옮긴 무뇌 정책 게이트.
// 한 게임의 10문항(초·중반 6개 12×12, 후반 4개 12×12×9)을 모두 첫 시도 표본으로 삼아
// 정책 자체의 성공 확률을 측정하고, 별도로 패드 3개 규칙을 적용한 실제 완주율을 계산한다.
const GAMES = 200;
const EARLY_SPACE = 12 * 12;
const LATE_SPACE = 12 * 12 * 9;
const CHANCE = (6 / EARLY_SPACE + 4 / LATE_SPACE) / 10;

function xorshift(seed) {
  let x = seed | 0;
  return () => {
    x ^= x << 13; x ^= x >>> 17; x ^= x << 5;
    return (x >>> 0) / 4294967296;
  };
}

const rndTarget = xorshift(0x4a414455);
const rndGuess = xorshift(0x20261017);
const ri = (rnd, n) => Math.floor(rnd() * n);

function targetFor(game, order) {
  // q는 화면 자료만 바꾸고 답 공간의 균등성에는 영향을 주지 않는다.
  const u = 1 + ri(rndTarget, 12);
  const v = 1 + ri(rndTarget, 12);
  const angle = ri(rndTarget, 9);
  const late = order >= 6;
  return { game, order, u, v, angle, late };
}

function same(a, b) {
  return a.u === b.u && a.v === b.v && (!a.late || a.angle === b.angle);
}

function simulate(name, choose) {
  let firstAttempts = 0;
  let firstCorrect = 0;
  let completed = 0;
  let totalProgress = 0;
  let policyAction = 0;
  const ratesByBand = { earlyAttempts: 0, earlyCorrect: 0, lateAttempts: 0, lateCorrect: 0 };

  for (let game = 0; game < GAMES; game++) {
    let lives = 3;
    let progress = 0;
    const deck = Array.from({ length: 10 }, (_, order) => targetFor(game, order));

    // 게이트 표본: 실패로 뒤 문항이 관측되지 않더라도 계획된 10문항의 첫 판단을 모두 평가한다.
    for (const target of deck) {
      const guess = choose(target, policyAction++);
      if (guess == null) continue;
      firstAttempts++;
      const ok = same(target, guess);
      if (target.late) {
        ratesByBand.lateAttempts++;
        if (ok) ratesByBand.lateCorrect++;
      } else {
        ratesByBand.earlyAttempts++;
        if (ok) ratesByBand.earlyCorrect++;
      }
      if (ok) firstCorrect++;
    }

    // 완주율: 실제 패드 규칙에서는 첫 오답 세 번이면 즉시 종료한다. 무입력은 진도 0이다.
    for (const target of deck) {
      if (lives <= 0) break;
      const guess = choose(target, policyAction++);
      if (guess == null) break;
      if (same(target, guess)) { progress++; }
      else lives--;
    }
    if (progress === 10) completed++;
    totalProgress += progress;
  }

  const rate = firstAttempts ? firstCorrect / firstAttempts : 0;
  // 95% 이항 상한은 참고치다. 판정은 설계 우연율과의 유의한 초과 재현 여부로 본다.
  const se = firstAttempts ? Math.sqrt(CHANCE * (1 - CHANCE) / firstAttempts) : 0;
  return {
    bot: name,
    games: GAMES,
    first_attempts: firstAttempts,
    first_correct: firstCorrect,
    first_attempt_rate: rate,
    chance_level: CHANCE,
    z_over_chance: se ? (rate - CHANCE) / se : 0,
    completion_rate: completed / GAMES,
    total_progress: totalProgress,
    ...ratesByBand,
    pass: name === 'no-input' ? firstAttempts === 0 && totalProgress === 0 : (rate - CHANCE) <= 1.96 * se
  };
}

const repeatGuess = { u: 6, v: 6, angle: 4 };
const results = [
  simulate('repeat', target => ({ ...repeatGuess, late: target.late })),
  simulate('cycle', (target, action) => ({
    u: 1 + (action % 12),
    v: 1 + (Math.floor(action / 12) % 12),
    angle: Math.floor(action / 144) % 9,
    late: target.late
  })),
  simulate('random', target => ({
    u: 1 + ri(rndGuess, 12),
    v: 1 + ri(rndGuess, 12),
    angle: ri(rndGuess, 9),
    late: target.late
  })),
  simulate('no-input', () => null)
];

// distractor_rules 전수 검산: 정답 제거 뒤 남는 오개념 값이 정답이 되는 경우는 0이어야 한다.
let collisionErrors = 0;
for (let q = 2; q <= 4; q++) for (let u = 1; u <= 12; u++) for (let v = 1; v <= 12; v++) {
  const refU = q * u, refV = q * v;
  for (let d = 1; d <= 36; d++) {
    const wrongU = refU - d, wrongV = refV - d;
    const inRange = wrongU >= 1 && wrongU <= 12 && wrongV >= 1 && wrongV <= 12;
    const sameAsAnswer = wrongU === u && wrongV === v;
    if (inRange && !sameAsAnswer && q * wrongU === refU && q * wrongV === refV) collisionErrors++;
  }
  if (u !== v && v === u && u === v) collisionErrors++;
  for (let a = 0; a < 9; a++) if (((a + 1) % 9) === a) collisionErrors++;
}

const out = {
  generated_at: new Date().toISOString(),
  games_per_policy: GAMES,
  sampled_first_attempts_per_active_policy: GAMES * 10,
  chance_level_weighted: CHANCE,
  chance_level_early: 1 / EARLY_SPACE,
  chance_level_late: 1 / LATE_SPACE,
  exhaustive: { parameter_sets: 3 * 12 * 12 * 9, misconception_collision_errors: collisionErrors },
  results,
  passed: collisionErrors === 0 && results.every(r => r.pass)
};

fs.writeFileSync(new URL('./b3-bot-results.json', import.meta.url), JSON.stringify(out, null, 2) + '\n');
console.log(JSON.stringify(out, null, 2));
if (!out.passed) process.exit(1);
