// Shared black-box helpers for the v3.2 validation suite.
//
// The proven pixel/layout primitives remain in v3/harness-lib.mjs.  Keeping
// those functions in one place avoids silently changing the meaning of gates
// (a)-(g); v3.2 adds only endless-run state and fixture helpers here.

import path from 'node:path';
import { fileURLToPath } from 'node:url';

export * from '../v3/harness-lib.mjs';

export const V32_HERE = path.dirname(fileURLToPath(import.meta.url));
export const MAPS = Object.freeze([
  { id: 1, key: 'grass', name: '초원 협곡' },
  { id: 2, key: 'desert', name: '사막 강 다리' },
  { id: 3, key: 'snow', name: '설원 요새' },
]);

export const REQUIRED_V32_STATE = Object.freeze([
  'stage', 'map', 'mapName', 'loop', 'stageQuestion', 'stagePhase',
  'bossWave', 'regionRecovered', 'perkSelection', 'perkChoices', 'perkRects',
  'mapSerial', 'transitionSerial', 'difficultyMultiplier', 'maxHp',
  'endReason', 'kills', 'firstAttemptRate', 'bestQuestions',
  'questionDifficulty', 'questionHistory', 'upgradeSeqPhase',
  'upgradeSeqProgress', 'upgradeSeqSerial', 'upgradeTargetType',
  'upgradeTargetLevel', 'upgradeStatBefore', 'upgradeStatAfter', 'rangeRing',
]);

export function mapForStage(stage) {
  const value = Math.max(1, Math.trunc(Number(stage) || 1));
  return MAPS[(value - 1) % MAPS.length];
}

export function loopForStage(stage) {
  const value = Math.max(1, Math.trunc(Number(stage) || 1));
  return Math.floor((value - 1) / MAPS.length) + 1;
}

export function expectedDifficultyMultiplier(loop) {
  return Math.pow(1.25, Math.max(0, Math.trunc(Number(loop) || 1) - 1));
}

export function stateContract(state) {
  const missing = REQUIRED_V32_STATE.filter(key => !Object.hasOwn(state ?? {}, key));
  const map = Number(state?.map);
  const stage = Number(state?.stage);
  const loop = Number(state?.loop);
  const stageQuestion = Number(state?.stageQuestion);
  const valid = missing.length === 0 && Number.isInteger(stage) && stage >= 1 &&
    Number.isInteger(map) && map >= 1 && map <= 3 && Number.isInteger(loop) && loop >= 1 &&
    Number.isInteger(stageQuestion) && stageQuestion >= 0 && stageQuestion <= 10 &&
    Array.isArray(state?.perkChoices) && Array.isArray(state?.perkRects) &&
    Array.isArray(state?.questionHistory);
  return { missing, valid };
}

export function flatNormalizedRects(flat, count, viewport, prefix = 'rect') {
  if (!Array.isArray(flat) || flat.length < count * 4) return [];
  const result = [];
  for (let index = 0; index < count; index += 1) {
    const at = index * 4;
    const values = flat.slice(at, at + 4).map(Number);
    if (!values.every(Number.isFinite)) continue;
    const [x, y, width, height] = values;
    result.push({
      id: `${prefix}-${index}`,
      normalized: true,
      x: x * viewport.width,
      y: y * viewport.height,
      width: width * viewport.width,
      height: height * viewport.height,
    });
  }
  return result;
}

export function canonicalProblems(problems) {
  if (!Array.isArray(problems)) return [];
  return problems.map(problem => ({
    id: String(problem?.id ?? ''),
    prompt: String(problem?.prompt ?? ''),
    choices: Array.isArray(problem?.choices) ? problem.choices.map(String) : null,
    answer: String(problem?.answer ?? ''),
    answerNumeric: Number.isFinite(problem?.answerNumeric) ? problem.answerNumeric : null,
    unitConcept: String(problem?.unitConcept ?? ''),
  })).sort((left, right) => left.id.localeCompare(right.id));
}

export async function sampleWholeBank(page) {
  const problems = await page.evaluate(() => window.__GAME_TEST__?.sampleProblems?.(100000) ?? null);
  return canonicalProblems(problems);
}

export async function sampleWholeBankRaw(page) {
  const problems = await page.evaluate(() => window.__GAME_TEST__?.sampleProblems?.(100000) ?? null);
  return Array.isArray(problems) ? problems : [];
}

export function rectCenter(rect) {
  return rect ? { x: rect.x + rect.width / 2, y: rect.y + rect.height / 2 } : null;
}

export function finiteNumber(value) {
  return typeof value === 'number' && Number.isFinite(value);
}
