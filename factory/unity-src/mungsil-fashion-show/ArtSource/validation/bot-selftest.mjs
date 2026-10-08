#!/usr/bin/env node
// B3 무뇌 봇 자가 테스트. C# MungsilRules.PickSession의 12버킷/8문항 순환과
// 바늘 3개 실패 상태를 그대로 재현해 4종 정책을 각각 200판 실행한다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const RUNS = 200;
const GOAL = 8;
const BUCKETS = [4, 5, 6, 7, 8, 9, 10, 12, 15, 18, 20, 24];

function difficulty(answer) {
  if (answer === 5 || answer === 6 || answer === 8) return 1;
  if (answer === 4 || answer === 12 || answer === 18) return 2;
  return 3;
}

function session(runSerial) {
  const start = (runSerial * 5) % BUCKETS.length;
  const picked = Array.from({ length: GOAL }, (_, i) => BUCKETS[(start + i) % BUCKETS.length]);
  return picked.sort((a, b) => difficulty(a) - difficulty(b) || a - b);
}

function lcg(seed) {
  let x = seed >>> 0;
  return () => {
    x = (Math.imul(x, 1664525) + 1013904223) >>> 0;
    return x / 0x100000000;
  };
}

function simulate(name, nextInput, submits = true) {
  let firstAttempts = 0;
  let firstCorrect = 0;
  let completed = 0;
  let totalSolved = 0;

  for (let run = 1; run <= RUNS; run++) {
    if (!submits) continue; // 같은 지점 연타는 드래그가 아니어서 제출·진도 0.
    const answers = session(run);
    let lives = 3;
    let solved = 0;
    let inputNo = 0;
    while (lives > 0 && solved < GOAL) {
      const answer = answers[solved];
      const first = nextInput({ run, solved, inputNo: inputNo++ });
      firstAttempts++;
      if (first === answer) {
        firstCorrect++;
        solved++;
        continue;
      }
      lives--;
      while (lives > 0) {
        const retry = nextInput({ run, solved, inputNo: inputNo++ });
        if (retry === answer) { solved++; break; }
        lives--;
      }
    }
    totalSolved += solved;
    if (solved >= GOAL) completed++;
  }
  return {
    name,
    runs: RUNS,
    firstAttempts,
    firstCorrect,
    firstAttemptRate: firstAttempts ? firstCorrect / firstAttempts : 0,
    completionRate: completed / RUNS,
    meanProgress: totalSolved / RUNS
  };
}

const random = lcg(1);
const results = [
  simulate('repeat-tap', () => 0, false),
  simulate('cycle-1-2-3-4', ({ inputNo }) => inputNo % 4 + 1),
  simulate('uniform-random-1-30', () => 1 + Math.floor(random() * 30)),
  simulate('no-input', () => 0, false)
];

const bucketCounts = Object.fromEntries(BUCKETS.map(v => [v, 0]));
for (let run = 1; run <= RUNS; run++) for (const answer of session(run)) bucketCounts[answer]++;
const fixedWorst = Math.max(...Object.values(bucketCounts)) / (RUNS * GOAL);
const report = {
  generatedAt: new Date().toISOString(),
  contract: {
    runsPerBot: RUNS,
    answerBuckets: BUCKETS.length,
    fixedPolicyChance: 1 / BUCKETS.length,
    empiricalFixedWorst: fixedWorst,
    uniformPositionChance: 1 / 30,
    noInputProgressRequired: 0
  },
  bucketCounts,
  results,
  pass: results[0].firstAttemptRate <= 1 / BUCKETS.length &&
        results[1].firstAttemptRate <= 1 / 30 &&
        results[2].firstAttemptRate <= 1 / 30 &&
        results[3].meanProgress === 0
};

const here = path.dirname(fileURLToPath(import.meta.url));
fs.writeFileSync(path.join(here, 'bot-results.json'), JSON.stringify(report, null, 2) + '\n');
console.log(JSON.stringify(report, null, 2));
if (!report.pass) process.exitCode = 1;
