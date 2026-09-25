#!/usr/bin/env node
/**
 * bots-browser.json 후처리 — 정책별 첫 시도 정답률을 "같은 잣대"로 비교한다.
 *
 * 하네스의 chanceRate 는 정책별 결정 수열 자체의 성공 확률이라, repeat/cycle/idle 에서는
 * 정의상 0 이다. 그래서 잡음(꼭짓점 히트박스를 빗나가 판정 없이 무시된 드래그) 한 번만 섞여도
 * z 가 무한대가 되어 "우연 초과"로 읽힌다. 하드 불변 3의 기준은 "우연 수준"이므로,
 * 정책과 무관하게 도달한 판에서 눈감고 합법적인 획을 그었을 때의 완주 확률
 * (uniformChanceRate) 을 같이 계산해 함께 보고한다. 원본 리포트는 건드리지 않는다.
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const IN_FILE = path.join(HERE, 'bots-browser.json');
const OUT_FILE = path.join(HERE, 'bots-summary.json');
const DISCARD_KINDS = new Set(['not_polygon', 'not_regular', 'triangle_discard']);
const MULTIPLE_CHOICE_REFERENCE = 0.25; // 4지선다 우연

function choose(n, k) {
  if (k < 0 || k > n) return 0;
  let value = 1;
  for (let i = 1; i <= k; i++) value = (value * (n - k + i)) / i;
  return value;
}

// 도달한 판에서, 표시점 말고 보이는 끝점들을 무작위 순서로 이었을 때 변을 건드리지 않고
// 그 판을 끝낼 확률. 밀어야 하는 판은 획 자체가 오답이므로 0.
function uniformChance(trial) {
  if (DISCARD_KINDS.has(trial.kind)) return 0;
  const pre = Number(trial.pre || 0);
  const available = Number(trial.n) - 1 - pre;
  const needed = Number(trial.n) - 3 - pre;
  const denominator = choose(available, needed);
  return denominator > 0 ? 1 / denominator : 0;
}

const report = JSON.parse(fs.readFileSync(IN_FILE, 'utf8'));
const policies = report.policies.map((entry) => {
  const trials = (entry.gamesDetail || []).flatMap((game) => game.firstAttempts || []);
  const correct = trials.filter((t) => t.firstCorrect).length;
  const expected = trials.reduce((sum, t) => sum + uniformChance(t), 0);
  const variance = trials.reduce((sum, t) => { const c = uniformChance(t); return sum + c * (1 - c); }, 0);
  const rate = trials.length ? correct / trials.length : null;
  const uniformRate = trials.length ? expected / trials.length : null;
  const z = variance > 0 ? (correct - expected) / Math.sqrt(variance) : correct > 0 ? Infinity : 0;
  return {
    policy: entry.policy,
    games: entry.games,
    trials: trials.length,
    firstCorrect: correct,
    firstAttemptRate: rate,
    sequenceChanceRate: entry.chanceRate,
    uniformChanceRate: uniformRate,
    zVsUniformChance: z,
    upliftOverUniformChance: z > 1.644854,
    atOrBelowUniformChance: rate != null && uniformRate != null && rate <= uniformRate,
    belowMultipleChoiceReference: rate != null && rate <= MULTIPLE_CHOICE_REFERENCE,
    completions: entry.completions,
    completionRate: entry.completionRate,
    solvedMax: Math.max(0, ...(entry.gamesDetail || []).map((g) => Number(g.solved) || 0)),
  };
});

const summary = {
  slug: report.slug,
  sourceSha256: report.sourceSha256,
  harnessSha256: report.harnessSha256,
  basedOn: path.basename(IN_FILE),
  runsPerPolicy: report.settings?.runsPerPolicy,
  status: report.status,
  multipleChoiceReference: MULTIPLE_CHOICE_REFERENCE,
  policies,
  allAtOrBelowUniformChance: policies.every((p) => p.atOrBelowUniformChance),
  noUpliftOverUniformChance: policies.every((p) => !p.upliftOverUniformChance),
  allBelowMultipleChoiceReference: policies.every((p) => p.belowMultipleChoiceReference),
  idleProgressZero: policies.filter((p) => p.policy === 'idle').every((p) => p.solvedMax === 0),
  generatedAt: new Date().toISOString(),
};
summary.pass = summary.noUpliftOverUniformChance && summary.allBelowMultipleChoiceReference && summary.idleProgressZero;
fs.writeFileSync(OUT_FILE, `${JSON.stringify(summary, null, 2)}\n`);
console.log(JSON.stringify({ ...summary, policies: policies.map((p) => ({
  policy: p.policy, games: p.games, trials: p.trials,
  firstAttempt: `${(p.firstAttemptRate * 100).toFixed(2)}%`,
  uniformChance: `${(p.uniformChanceRate * 100).toFixed(2)}%`,
  z: Number.isFinite(p.zVsUniformChance) ? p.zVsUniformChance.toFixed(2) : String(p.zVsUniformChance),
  uplift: p.upliftOverUniformChance, completions: p.completions, solvedMax: p.solvedMax,
})) }, null, 2));
