#!/usr/bin/env node
// Structural gate for schema v3 packs (every item is a 4-choice question — DESIGN-V3.md §4).
// Written for the m2s1 conversion; it is generic (no unit-specific rules) and does not touch validate-pack.mjs,
// which still gates v1/v2 packs. Mathematics needs an independent oracle (check-m2s1.mjs).
import fs from 'node:fs';
import {pathToFileURL} from 'node:url';

const gcd = (a, b) => { a = a < 0n ? -a : a; b = b < 0n ? -b : b; while (b) [a, b] = [b, a % b]; return a || 1n; };
/** Value of a numeric choice string as a reduced BigInt rational "n/d", or null for text choices. */
export function choiceValue(s) {
  if (typeof s !== 'string') return null;
  let m = /^(−?)(\d+)$/.exec(s); if (m) return `${m[1] ? -BigInt(m[2]) : BigInt(m[2])}/1`;
  m = /^(−?)(\d+)\.(\d+)$/.exec(s); if (m) { let n = BigInt(m[2] + m[3]), d = 10n ** BigInt(m[3].length); if (m[1]) n = -n; const g = gcd(n, d); return `${n / g}/${d / g}`; }
  m = /^(−?)\{frac:(\d+)\/(\d+)\}$/.exec(s); if (m) { let n = BigInt(m[2]), d = BigInt(m[3]); if (d === 0n) return 'NaN'; if (m[1]) n = -n; const g = gcd(n, d); return `${n / g}/${d / g}`; }
  return null;
}
const normText = s => s.replace(/\s+/g, '').replace(/-/g, '−');
const FORBIDDEN_PARAM_KEYS = new Set(['answer', 'max', 'coin_budget', 'carry_capacity', 'coin_per_kill', 'min_spawn_coins', 'schema_version', 'economy']);
function keysDeep(o, out = []) { if (Array.isArray(o)) o.forEach(v => keysDeep(v, out)); else if (o && typeof o === 'object') for (const [k, v] of Object.entries(o)) { out.push(k); keysDeep(v, out); } return out; }

export function validatePackV3(pack, {minItems = 300, slotTolerance = 0.05} = {}) {
  const errors = [], warnings = [], assert = (ok, msg) => { if (!ok) errors.push(msg); };
  assert(pack.schema_version === 3, 'schema_version: 3');
  assert(/^[a-z0-9][a-z0-9-]*$/.test(pack.pack_id ?? ''), 'pack_id: 영문 소문자·숫자·하이픈');
  assert(typeof pack.title === 'string' && pack.title.length > 0, 'title 필요');
  assert(['elementary', 'middle'].includes(pack.school), 'school');
  assert(Number.isInteger(pack.grade) && pack.grade >= 1, 'grade'); assert([1, 2].includes(pack.semester), 'semester');
  assert(typeof pack.unit_id === 'string', 'unit_id'); assert(Array.isArray(pack.standards) && pack.standards.length > 0, 'standards');
  const items = Array.isArray(pack.items) ? pack.items : [];
  assert(items.length >= minItems, `고유 문항 ${minItems}개 이상 필요 (${items.length})`);
  if (pack.economy !== undefined) assert(pack.economy && typeof pack.economy === 'object' && !Array.isArray(pack.economy), 'economy 는 객체');
  const ids = new Set(), prompts = new Set(), bands = new Set(), slots = [0, 0, 0, 0];
  for (const q of items) {
    const id = q.id; assert(typeof id === 'string' && !ids.has(id), `중복 또는 없는 id: ${id}`); ids.add(id);
    const norm = (q.prompt ?? '').replace(/\([^)]*\)/g, '').replace(/\s+/g, ' ').trim(); assert(norm && !prompts.has(norm), `${id}: 중복 또는 빈 발문`); prompts.add(norm);
    assert(q.answer_mode === 'choice', `${id}: v3 는 answer_mode "choice" 만`);
    assert(['int', 'frac', 'text'].includes(q.format), `${id}: format`);
    assert(q.explain?.length > 0 && q.unitConcept?.length > 0, `${id}: explain/unitConcept`);
    assert(!/\n/.test(q.explain ?? ''), `${id}: explain 은 한 줄`);
    assert([1, 2, 3, 4].includes(q.difficulty), `${id}: difficulty 1~4`); bands.add(q.difficulty);
    const ch = Array.isArray(q.choices) ? q.choices : [];
    assert(ch.length === 4 && ch.every(c => typeof c === 'string' && c.length > 0), `${id}: 보기는 비어 있지 않은 문자열 4개`);
    assert(typeof q.answer === 'string' && ch.filter(c => c === q.answer).length === 1, `${id}: answer 는 보기 중 정확히 하나`);
    const slot = ch.indexOf(q.answer); if (slot >= 0) slots[slot]++;
    const vals = ch.map(choiceValue), keys = ch.map((c, i) => vals[i] ?? `text:${normText(c)}`);
    assert(new Set(keys).size === 4, `${id}: 값이 같은(동치) 보기 — ${ch.join(' | ')}`);
    assert(!vals.includes('NaN'), `${id}: 분모 0`);
    if (q.format !== 'text') {
      const av = choiceValue(q.answer); assert(av !== null, `${id}: 수 형식 문항의 answer 는 수여야 한다`);
      assert(Number.isFinite(q.answerNumeric), `${id}: answerNumeric 필요`);
      if (av) { const [n, d] = av.split('/').map(Number); assert(Math.abs(q.answerNumeric - n / d) < 1e-9, `${id}: answerNumeric 불일치`); }
      assert(q.format === 'frac' ? /\{frac:/.test(q.answer) : !/\{frac:/.test(q.answer), `${id}: format 과 answer 모양 불일치`);
    }
    for (const c of ch) { const m = /\{frac:(\d+)\/(\d+)\}/.exec(c); if (m) { assert(gcd(BigInt(m[1]), BigInt(m[2])) === 1n && m[2] !== '1', `${id}: 기약분수가 아닌 보기 ${c}`); } }
    const tags = q.distractor_tags; assert(Array.isArray(tags) && tags.length === 3 && tags.every(t => typeof t === 'string' && t.startsWith(`${pack.pack_id}.`)), `${id}: distractor_tags 는 "<pack_id>.<오개념>" 3개`);
    assert(new Set(tags ?? []).size >= 2, `${id}: 서로 다른 오개념 태그 2개 이상`);
    const text = [q.prompt, q.explain, ...ch].join(' ');
    assert(!/\d+\s*\/\s*\d+/.test(text.replace(/\{frac:\d+\/\d+\}/g, '')), `${id}: 평문 분수 금지`);
    assert(!/약분하지|약분하지 않아도|코인|닢|붓/.test(q.prompt + q.explain), `${id}: 붓기 시대 문구`);
    assert(!('max' in q) && !('coin_budget' in q) && !('accept' in q), `${id}: v2 붓기 필드(max/coin_budget/accept) 금지`);
    for (const k of keysDeep(q.params ?? {})) assert(!FORBIDDEN_PARAM_KEYS.has(k), `${id}: params 키 "${k}" 는 Unity 정규화기와 충돌`);
  }
  assert([1, 2, 3, 4].every(n => bands.has(n)), '난이도 1~4 모두 필요');
  assert(items[0]?.difficulty === 1, '첫 문항은 난이도 1');
  const N = items.length || 1; slots.forEach((c, i) => assert(Math.abs(c / N - 0.25) <= slotTolerance, `정답 위치 ${i + 1}번 비율 ${(100 * c / N).toFixed(1)}% (25±${100 * slotTolerance}%)`));
  return {pack_id: pack.pack_id, schema_version: pack.schema_version, items: items.length, answer_slots: slots, structural_errors: errors.length, errors: errors.slice(0, 50), warnings};
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const files = process.argv.slice(2).filter(a => !a.startsWith('--'));
  if (!files.length) { console.error('사용법: node validate-pack-v3-m2s1.mjs <팩.json>… [--fixture]'); process.exit(2); }
  let bad = 0;
  for (const f of files) { const r = validatePackV3(JSON.parse(fs.readFileSync(f, 'utf8')), {minItems: process.argv.includes('--fixture') ? 1 : 300}); console.log(JSON.stringify(r)); bad += r.structural_errors; }
  if (bad) process.exitCode = 1;
}
