// Validate the production-rules v3 bot simulation and reward-RNG separation.
//
// Default: run the Unity Editor method, then validate its JSON.
//   node factory/unity-src/hyeopgok-sasu/ArtSource/validation/v3/bots.mjs
// Validate an existing report without launching Unity:
//   node .../bots.mjs --report /absolute/path/to/bot-results.json

import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { HERE, ROOT, packHashes, readPackIndex } from './harness-lib.mjs';

const GAMES = 200;
const CHANCE = 0.25;
const REQUIRED = Object.freeze(['fixed', 'cycle', 'random', 'idle', 'nearest']);
const RAW_DIR = path.join(HERE, 'bot-run');
const OUT = path.join(HERE, 'bots.json');
const UNITY_LOG = path.join(HERE, 'bots-unity.log');
const CLI_LOG = path.join(HERE, 'bots-cli.log');

function argument(name) {
  const index = process.argv.indexOf(name);
  return index >= 0 ? process.argv[index + 1] : null;
}

if (process.argv.includes('--help')) {
  console.log('bots.mjs [--report PATH] [--workspace PATH]');
  process.exit(0);
}

function editorVersion(workspace) {
  const file = path.join(workspace, 'ProjectSettings/ProjectVersion.txt');
  if (!fs.existsSync(file)) return process.env.MGF_UNITY_VERSION || '6000.3.24f1';
  const match = fs.readFileSync(file, 'utf8').match(/^m_EditorVersion:\s*(\S+)/m);
  return match?.[1] || process.env.MGF_UNITY_VERSION || '6000.3.24f1';
}

function canonicalBot(row) {
  const explicit = String(row.id ?? row.botId ?? row.strategyId ?? '').toLowerCase().replace(/[\s_-]+/g, '');
  const text = `${explicit} ${row.bot ?? ''} ${row.name ?? ''} ${row.strategy ?? ''}`.toLowerCase();
  if (/nearest|closest|가장\s*가까|최근접/.test(text)) return 'nearest';
  if (/idle|noinput|무입력|가만/.test(text)) return 'idle';
  if (/cycle|roundrobin|순환|차례/.test(text)) return 'cycle';
  if (/random|무작위|랜덤/.test(text)) return 'random';
  if (/fixed|repeat|firstpad|pad0|고정|첫\s*패드|항상\s*첫/.test(text)) return 'fixed';
  return null;
}

function numberAt(row, ...paths) {
  for (const chain of paths) {
    let value = row;
    for (const key of chain.split('.')) value = value?.[key];
    const numeric = Number(value);
    if (Number.isFinite(numeric)) return numeric;
  }
  return null;
}

function evaluateBot(row, id) {
  const games = numberAt(row, 'games', 'runs', 'gameCount');
  const submitted = numberAt(row, 'submitted', 'firstAttempts', 'all.submitted');
  const correct = numberAt(row, 'correct', 'firstCorrect', 'all.correct');
  let rate = numberAt(row, 'firstAttemptRate', 'first_attempt_rate', 'all.firstAttemptRate');
  if (rate == null && submitted != null && correct != null) rate = submitted > 0 ? correct / submitted : 0;
  const explicitProgress = row.progress ?? row.madeProgress ?? row.progressed;
  const progressCount = numberAt(row, 'maximumSolved', 'solved', 'correct', 'all.correct');
  const idleNoProgress = id !== 'idle' || (explicitProgress === false || progressCount === 0) && (submitted === 0 || submitted == null);
  const failures = [];
  if (games !== GAMES) failures.push(`games=${games}; expected ${GAMES}`);
  if (rate == null || rate < 0 || rate > 1) failures.push(`invalid firstAttemptRate=${rate}`);
  else if (rate > CHANCE) failures.push(`firstAttemptRate ${(rate * 100).toFixed(3)}% > 25%`);
  if (id !== 'idle' && !(submitted > 0)) failures.push(`submitted=${submitted}; non-idle bot must commit through a pad`);
  if (!idleNoProgress) failures.push('idle bot made progress');
  if (row.blind !== true) failures.push('bot must explicitly declare blind=true');
  return {
    id,
    sourceName: row.bot ?? row.name ?? row.id ?? null,
    games,
    submitted,
    correct,
    firstAttemptRate: rate,
    firstAttemptPercent: rate == null ? null : rate * 100,
    idleNoProgress,
    failures,
    pass: failures.length === 0,
  };
}

function variantCount(orthogonality) {
  const direct = numberAt(orthogonality, 'rewardOutcomeVariants', 'outcomeVariants', 'variantCount');
  if (direct != null) return direct;
  const outcomes = orthogonality.rewardOutcomes ?? orthogonality.outcomes ?? orthogonality.observedRewards;
  return Array.isArray(outcomes) ? new Set(outcomes.map(value => typeof value === 'string' ? value : JSON.stringify(value))).size : null;
}

function evaluateOrthogonality(raw) {
  const source = raw.rewardOrthogonality ?? raw.reward_rng_orthogonality ?? raw.rewardRngOrthogonality;
  if (!source || typeof source !== 'object') return { pass: false, failures: ['rewardOrthogonality block is missing'] };
  const testedSeeds = numberAt(source, 'testedSeeds', 'seedCount') ?? (Array.isArray(source.seeds) ? source.seeds.length : null);
  const comparisons = numberAt(source, 'comparisons', 'comparedDecisions', 'comparedAnswers', 'decisionComparisons');
  const answerMismatches = numberAt(source, 'answerMismatches', 'answerDecisionMismatches', 'correctnessMismatches');
  const questionOrderMismatches = numberAt(source, 'questionOrderMismatches', 'orderMismatches');
  const variants = variantCount(source);
  const failures = [];
  if (!(testedSeeds >= 2)) failures.push(`testedSeeds=${testedSeeds}; expected at least 2`);
  if (!(comparisons > 0)) failures.push(`comparisons=${comparisons}; expected > 0`);
  if (answerMismatches !== 0) failures.push(`answerMismatches=${answerMismatches}; expected 0`);
  if (questionOrderMismatches !== 0) failures.push(`questionOrderMismatches=${questionOrderMismatches}; expected 0`);
  if (!(variants >= 2)) failures.push(`rewardOutcomeVariants=${variants}; expected coin/tower diversity`);
  if (source.pass === false) failures.push('producer marked reward orthogonality failed');
  return {
    testedSeeds,
    comparisons,
    answerMismatches,
    questionOrderMismatches,
    rewardOutcomeVariants: variants,
    producerPass: source.pass ?? null,
    failures,
    pass: failures.length === 0,
  };
}

const index = readPackIndex();
const packsBefore = packHashes(index);
let reportFile = argument('--report');
let unityRun = null;
if (!reportFile) {
  const workspace = path.resolve(argument('--workspace') || process.env.MGF_UNITY_WORKSPACE || '/Users/sitpo/UnityProjects/MGF-Workspace');
  const unity = process.env.UNITY_CLI || 'unity';
  fs.mkdirSync(RAW_DIR, { recursive: true });
  reportFile = path.join(RAW_DIR, 'bot-results.json');
  // Never let an Editor compile/start failure validate a stale green report.
  fs.rmSync(reportFile, { force: true });
  const args = [
    '--no-banner', '--non-interactive', 'run', workspace,
    '--editor-version', editorVersion(workspace), '--timeout', process.env.V3_BOT_TIMEOUT || '900', '--',
    '-nographics',
    '-executeMethod', 'Mgf.HyeopgokSasu.HyeopgokBotSelfTest.Run',
    '-hyeopgokRepo', ROOT,
    '-logFile', UNITY_LOG,
  ];
  const startedAt = Date.now();
  const run = spawnSync(unity, args, {
    cwd: ROOT,
    env: { ...process.env, MGF_HYEOPGOK_REPO: ROOT, MGF_HYEOPGOK_VALIDATION_OUT: RAW_DIR },
    encoding: 'utf8',
    maxBuffer: 16 * 1024 * 1024,
  });
  fs.writeFileSync(CLI_LOG, `${run.stdout || ''}${run.stderr || ''}`);
  unityRun = {
    command: [unity, ...args],
    workspace,
    editorVersion: editorVersion(workspace),
    status: run.status,
    signal: run.signal,
    elapsedSeconds: (Date.now() - startedAt) / 1000,
    error: run.error ? String(run.error) : null,
  };
}

const result = {
  schemaVersion: 3,
  generatedAt: new Date().toISOString(),
  thresholds: { gamesPerBot: GAMES, maximumFirstAttemptRate: CHANCE, requiredBots: REQUIRED },
  reportFile: path.resolve(reportFile),
  unityRun,
  sourceSchemaVersion: null,
  bots: [],
  rewardOrthogonality: null,
  packIntegrity: { before: packsBefore, after: null, stable: false },
  failures: [],
  pass: false,
};

if (!fs.existsSync(reportFile)) {
  result.failures.push(`bot report missing: ${reportFile}`);
} else {
  try {
    const raw = JSON.parse(fs.readFileSync(reportFile, 'utf8'));
    result.sourceSchemaVersion = raw.schemaVersion ?? raw.schema_version ?? null;
    if (Number(result.sourceSchemaVersion) < 3) result.failures.push(`bot report schema ${result.sourceSchemaVersion}; v3 required`);
    const rows = Array.isArray(raw.results) ? raw.results : Array.isArray(raw.bots) ? raw.bots : [];
    for (const id of REQUIRED) {
      const matches = rows.filter(row => canonicalBot(row) === id);
      if (matches.length === 0) {
        result.failures.push(`required bot missing: ${id}`);
        continue;
      }
      for (const row of matches) result.bots.push(evaluateBot(row, id));
    }
    const unexpectedDuplicates = REQUIRED.filter(id => result.bots.filter(bot => bot.id === id).length > 1 && !rows.some(row => row.pack || row.packId));
    if (unexpectedDuplicates.length) result.failures.push(`duplicate bots without pack identity: ${unexpectedDuplicates.join(', ')}`);
    result.rewardOrthogonality = evaluateOrthogonality(raw);
    for (const bot of result.bots) for (const failure of bot.failures) result.failures.push(`${bot.id}: ${failure}`);
    for (const failure of result.rewardOrthogonality.failures) result.failures.push(`reward RNG: ${failure}`);
  } catch (error) {
    result.failures.push(`cannot parse bot report: ${String(error?.stack || error)}`);
  }
}

result.packIntegrity.after = packHashes(index);
result.packIntegrity.stable = JSON.stringify(result.packIntegrity.before) === JSON.stringify(result.packIntegrity.after);
if (!result.packIntegrity.stable) result.failures.push('pack files changed during bot validation');
result.pass = result.failures.length === 0 && result.bots.length >= REQUIRED.length && result.rewardOrthogonality?.pass === true;
fs.writeFileSync(OUT, JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify({ output: OUT, source: result.reportFile, bots: result.bots.map(bot => ({ id: bot.id, games: bot.games, firstAttemptPercent: bot.firstAttemptPercent, pass: bot.pass })), rewardOrthogonality: result.rewardOrthogonality, failures: result.failures, pass: result.pass }, null, 2));
if (!result.pass) process.exitCode = 1;
