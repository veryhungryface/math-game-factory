#!/usr/bin/env node
import fs from 'node:fs';
import path from 'node:path';
import { HERE, artifactHash, closeSession, createSession } from './pointer-common.mjs';

const ints = (text, pattern, label) => {
  const match = text.match(pattern);
  if (!match) throw new Error(`${label} parse failed: ${text}`);
  return match.slice(1).map(Number);
};
const triangle = ([a, b, c]) => a + b > c && b + c > a && c + a > b;

function independentlyCorrect(problem, choice) {
  if (problem.id.startsWith('sss-')) {
    const abc = ints(problem.prompt, /AB (\d+) cm · BC (\d+) cm · CA (\d+) cm/, 'SSS target');
    const def = ints(choice, /△DEF\s+DE (\d+) cm · EF (\d+) cm · FD (\d+) cm/, 'SSS choice');
    if (!triangle(abc) || !triangle(def)) throw new Error(`invalid triangle: ${problem.id} ${choice}`);
    return abc[0] * def[1] === abc[1] * def[0] && abc[0] * def[2] === abc[2] * def[0];
  }
  if (problem.id.startsWith('sas-')) {
    const [ab, ac, angleA] = ints(problem.prompt, /AB (\d+) cm · AC (\d+) cm · ∠A (\d+)°/, 'SAS target');
    const [de, df, angleD] = ints(choice, /DE (\d+) cm · DF (\d+) cm · ∠D (\d+)°/, 'SAS choice');
    return ab * df === ac * de && angleA === angleD;
  }
  if (problem.id.startsWith('aa-')) {
    const abc = ints(problem.prompt, /∠A (\d+)° · ∠B (\d+)° · ∠C (\d+)°/, 'AA target');
    const def = ints(choice, /∠D (\d+)° · ∠E (\d+)° · ∠F (\d+)°/, 'AA choice');
    if (abc.reduce((a, b) => a + b, 0) !== 180 || def.reduce((a, b) => a + b, 0) !== 180) throw new Error(`invalid angle sum: ${problem.id} ${choice}`);
    return abc.every((value, index) => value === def[index]);
  }
  throw new Error(`unknown problem kind: ${problem.id}`);
}

const session = await createSession();
try {
  const bank = await session.page.evaluate(() => window.__GAME_TEST__.sampleProblems(1000));
  const ids = new Set();
  const counts = { sss: 0, sas: 0, aa: 0 };
  const failures = [];
  for (const problem of bank) {
    const kind = problem.id.split('-')[0];
    counts[kind]++;
    if (ids.has(problem.id)) failures.push(`${problem.id}: duplicate id`);
    ids.add(problem.id);
    if (problem.choices.length !== 8 || new Set(problem.choices).size !== 8) failures.push(`${problem.id}: choices not 8 unique`);
    if (problem.prompt.includes('√') || problem.choices.some(choice => choice.includes('√'))) failures.push(`${problem.id}: grade-inappropriate root symbol`);
    const correct = problem.choices.filter(choice => independentlyCorrect(problem, choice));
    if (correct.length !== 1) failures.push(`${problem.id}: independently correct choices=${correct.length}`);
    if (correct[0] !== problem.answer) failures.push(`${problem.id}: declared answer differs from integer-ratio proof`);
    if (problem.choices[problem.answerNumeric] !== problem.answer) failures.push(`${problem.id}: answerNumeric mismatch`);
  }
  const output = {
    generatedAt: new Date().toISOString(),
    artifactHashSha256: artifactHash(),
    source: 'all problems returned by the final WebGL ProblemBankJson path',
    bankCount: bank.length,
    uniqueIds: ids.size,
    counts,
    proof: 'SSS and SAS use integer cross-products; AA uses corresponding integer angles and 180-degree sums',
    failures,
    passed: bank.length >= 300 && ids.size === bank.length && failures.length === 0,
  };
  fs.writeFileSync(path.join(HERE, 'math-bank-results.json'), `${JSON.stringify(output, null, 2)}\n`);
  console.log(JSON.stringify(output, null, 2));
  if (!output.passed) process.exitCode = 1;
} finally {
  await closeSession(session);
}
