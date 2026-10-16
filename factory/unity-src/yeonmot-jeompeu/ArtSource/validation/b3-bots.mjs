#!/usr/bin/env node

// B3 blind-bot audit for the exact six-slot RunDeck distribution in YeonmotRules.cs.
// This ports PondRng/RunDeck and the game's three-life, retry-without-first-credit rules.
// It intentionally never reads mathematical content or the correct slot before choosing.

const GAMES = 200;
const BASE_SEED = 20261017;
const CHANCE = 1 / 6;

function pondRng(seed) {
  let state = ((seed >>> 0) + 0x9e3779b9) >>> 0;
  if (state === 0) state = 1;
  return () => {
    let x = state;
    x ^= x << 13;
    x ^= x >>> 17;
    x ^= x << 5;
    state = x >>> 0;
    return state;
  };
}

function randomBotRng(seed) {
  let state = seed >>> 0;
  return () => {
    let t = (state += 0x6d2b79f5);
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return (t ^ (t >>> 14)) >>> 0;
  };
}

function correctSlots(seed) {
  const next = pondRng((seed ^ 0xb5297a4d) | 0);
  return Array.from({ length: 8 }, () => next() % 6);
}

function simulateGame(kind, seed) {
  if (kind === "noinput") {
    return { firstAttempts: 0, firstCorrect: 0, completed: false, progress: 0 };
  }

  const slots = correctSlots(seed);
  const random = randomBotRng((seed ^ 0xa511e9b3) >>> 0);
  let problem = 0;
  let lives = 3;
  let inputIndex = 0;
  let firstAttempts = 0;
  let firstCorrect = 0;

  while (problem < 8 && lives > 0) {
    let isFirst = true;
    while (problem < 8 && lives > 0) {
      // The cycle bot follows the requested 1→2→3→4 pattern; the game has six slots.
      const pick = kind === "fixed" ? 0 : kind === "cycle" ? inputIndex % 4 : random() % 6;
      inputIndex += 1;
      const correct = pick === slots[problem];
      if (isFirst) {
        firstAttempts += 1;
        if (correct) firstCorrect += 1;
        isFirst = false;
      }
      if (correct) {
        problem += 1;
        break;
      }
      lives -= 1;
    }
  }

  return {
    firstAttempts,
    firstCorrect,
    completed: problem === 8 && firstCorrect >= 6,
    progress: problem,
  };
}

const bots = ["fixed", "cycle", "random", "noinput"];
const results = {};
for (const kind of bots) {
  const total = { games: GAMES, firstAttempts: 0, firstCorrect: 0, completed: 0, progress: 0 };
  for (let game = 1; game <= GAMES; game += 1) {
    const run = simulateGame(kind, BASE_SEED + game);
    total.firstAttempts += run.firstAttempts;
    total.firstCorrect += run.firstCorrect;
    total.completed += Number(run.completed);
    total.progress += run.progress;
  }
  total.firstAttemptRate = total.firstAttempts === 0 ? 0 : total.firstCorrect / total.firstAttempts;
  total.completionRate = total.completed / total.games;
  total.chanceRate = CHANCE;
  total.pass = total.firstAttemptRate <= CHANCE && (kind !== "noinput" || total.progress === 0);
  results[kind] = total;
}

const report = {
  slug: "yeonmot-jeompeu",
  method: "headless deterministic simulation of YeonmotRules.RunDeck and game life/retry semantics",
  gamesPerBot: GAMES,
  seedRange: [BASE_SEED + 1, BASE_SEED + GAMES],
  chanceRate: CHANCE,
  results,
  passed: bots.every((kind) => results[kind].pass),
};

console.log(JSON.stringify(report, null, 2));
if (!report.passed) process.exitCode = 1;
