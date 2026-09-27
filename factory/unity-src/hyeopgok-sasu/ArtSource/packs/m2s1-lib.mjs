// Shared helpers for the m2s1 (중2 1학기) pack generators.
// Deliberately separate from common.mjs: that module's writePack() rewrites index.json,
// and the pack index is owned by the orchestrator. This module never touches index.json.
//
// Rendering contract (Unity TMP + MgfKR-Bold subset, checked with fontTools 2026-09-27):
//  - The font has ² ³ but NOT ⁴-⁹, and has no combining dot above (U+0307).
//    Exponents are therefore written with the TMP rich-text tag <sup>n</sup> (the font's
//    OS/2 superscript metrics are 350/600), and repeating decimals are shown as expanded
//    digits with "…" (e.g. 0.2454545…) instead of dot notation.
//  - TMP parses "<…>" as a tag. Any "<" that is not our own <sup> tag must never be followed by
//    ">" before the next "<" (tmpSafe below), so inequality text cannot be eaten as markup.
//  - Fractions are {frac:n/d} tokens with digit-only parts; negative fractions carry "−" outside.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';

export const here = path.dirname(fileURLToPath(import.meta.url));
export const output = path.resolve(here, '../../../../../public/g/hyeopgok-sasu/packs');
export const M = '−'; // U+2212, the textbook minus sign (in font)

export function gcd(a, b) { a = Math.abs(a); b = Math.abs(b); while (b) [a, b] = [b, a % b]; return a || 1; }
export function safe(n) { if (!Number.isSafeInteger(n)) throw Error(`unsafe integer ${n}`); return n; }
export function reduce(n, d) { if (d === 0) throw Error('zero denominator'); if (d < 0) { n = -n; d = -d; } const g = gcd(n, d); return [safe(n / g), safe(d / g)]; }

// ---------- display formatting ----------
export function num(n) { return n < 0 ? M + (-n) : String(n); }
export function sup(e) { return e === 1 ? '' : `<sup>${e}</sup>`; }
/** Rational n/d as display text: integer, {frac:a/b}, or −{frac:a/b}. */
export function rat(n, d = 1) { const [a, b] = reduce(n, d); if (b === 1) return num(a); return (a < 0 ? M : '') + `{frac:${Math.abs(a)}/${b}}`; }
/** Decimal display of a rational with a terminating expansion (used for 0.3x style coefficients). */
export function dec(n, d) {
  const [a, b] = reduce(n, d);
  for (let k = 0; k <= 6; k++) { const p = 10 ** k; if ((p % b) === 0) { const v = Math.abs(a) * (p / b); const s = String(v).padStart(k + 1, '0'); const body = k ? s.slice(0, s.length - k) + '.' + s.slice(s.length - k) : s; return (a < 0 ? M : '') + body; } }
  throw Error(`non-terminating decimal ${n}/${d}`);
}
/** Monomial with signed coefficient. vars: [['x',2],['y',1]] (zero exponents skipped). */
export function mono(c, vars = [], {decimal = false, den = 1} = {}) {
  const vs = vars.filter(([, e]) => e !== 0).map(([v, e]) => v + sup(e)).join('');
  if (c === 0) return '0';
  const [a, b] = reduce(c, den);
  if (!vs) return decimal && b !== 1 ? dec(a, b) : rat(a, b);
  if (b === 1) { if (a === 1) return vs; if (a === -1) return M + vs; return num(a) + vs; }
  return (decimal ? dec(a, b) : rat(a, b)) + vs;
}
/** Polynomial from [[coef, vars], …]; zero terms skipped. opts.den applies a common denominator. */
export function poly(terms, opts = {}) {
  let out = '';
  for (const [c, vars] of terms) {
    if (c === 0) continue; const m = mono(c, vars, opts);
    if (!out) out = m; else out += m.startsWith(M) ? M + m.slice(1) : '+' + m;
  }
  return out || '0';
}
export function lin(a, b, v = 'x', opts) { return poly([[a, [[v, 1]]], [b, []]], opts); }

// ---------- 조사 ----------
const DIGIT = {0: [1, 0], 1: [1, 1], 2: [0, 0], 3: [1, 0], 4: [0, 0], 5: [0, 0], 6: [1, 0], 7: [1, 1], 8: [1, 1], 9: [0, 0]};
const LETTER = {a: [0, 0], b: [0, 0], c: [0, 0], d: [0, 0], f: [0, 0], k: [0, 0], l: [1, 1], m: [1, 0], n: [1, 0], p: [0, 0], q: [0, 0], r: [1, 1], x: [0, 0], y: [0, 0], A: [0, 0], B: [0, 0], P: [0, 0], Q: [0, 0]};
function sound(s) {
  let t = s.trimEnd();
  for (;;) {
    if (/<sup>[^<]*<\/sup>$/.test(t) || /[²³]$/.test(t)) return [1, 0]; // …제곱
    const fr = /\{frac:(\d+)\/(\d+)\}$/.exec(t); if (fr) { t = fr[1]; continue; } // b분의 a → a
    if (/[)\]}…]$/.test(t)) { t = t.slice(0, -1); continue; }
    break;
  }
  const ch = t.at(-1);
  if (/\d/.test(ch)) {
    const m = /(\d+)$/.exec(t)[1]; const zeros = /0*$/.exec(m)[0].length;
    if (Number(m) === 0) return [1, 0];
    if (/\.\d*$/.test(t) || zeros === 0) return DIGIT[ch];
    return zeros === 1 ? [1, 0] : zeros === 2 ? [1, 0] : zeros === 3 ? [1, 0] : [1, 0]; // 십/백/천/만
  }
  if (LETTER[ch]) return LETTER[ch];
  const code = ch.codePointAt(0);
  if (code >= 0xac00 && code <= 0xd7a3) { const jong = (code - 0xac00) % 28; return [jong ? 1 : 0, jong === 8 ? 1 : 0]; }
  if (ch === '°') return [0, 0]; // 도
  if (ch === '□') return [0, 0]; // 네모
  throw Error(`josa: unknown tail in "${s}"`);
}
/** Append a particle chosen by the preceding sound: j(s,'을'|'이'|'은'|'과'|'으로'). */
export function j(s, p) {
  const [jong, rieul] = sound(s);
  const table = {을: ['을', '를'], 이: ['이', '가'], 은: ['은', '는'], 과: ['과', '와'], 이다: ['이다', '다'], 이라: ['이라', '라']};
  if (p === '으로') return s + (jong && !rieul ? '으로' : '로');
  return s + (jong ? table[p][0] : table[p][1]);
}

// ---------- guards ----------
export function tmpSafe(s) {
  // TMP only turns "<…>" into markup when "<" (optionally "</") is followed by a tag name made of
  // letters/hyphens and then a space, "=" or ">" (or by "#" for colours). Math text like "4x+27<5x이고, x>27"
  // or "a<b일 때" never matches that shape. Our own <sup>n</sup> tags are the only allowed markup.
  const t = s.replace(/<\/?sup>/g, '');
  return !/<\/?(?:[A-Za-z-]+[\s=>]|#)/.test(t);
}
const FORBIDDEN_PARAM_KEYS = new Set(['answer', 'max', 'coin_budget', 'carry_capacity', 'coin_per_kill', 'min_spawn_coins', 'schema_version', 'economy']);
function scanKeys(o, where) { if (Array.isArray(o)) o.forEach(v => scanKeys(v, where)); else if (o && typeof o === 'object') for (const [k, v] of Object.entries(o)) { if (FORBIDDEN_PARAM_KEYS.has(k)) throw Error(`${where}: params key "${k}" collides with the Unity pack normalizer`); scanKeys(v, where); } }
function checkText(s, where) {
  if (typeof s !== 'string') return;
  if (!tmpSafe(s)) throw Error(`${where}: TMP tag hazard in "${s}"`);
  if (s.includes('√')) throw Error(`${where}: 근호 금지`);
  if (/[⁰⁴⁵⁶⁷⁸⁹̇]/.test(s)) throw Error(`${where}: glyph missing from game font in "${s}"`);
  if (/\d\s*\/\s*\d/.test(s.replace(/\{frac:\d+\/\d+\}/g, ''))) throw Error(`${where}: plain fraction in "${s}"`);
  if (/[0-9a-z)]-[0-9a-z(]/.test(s)) throw Error(`${where}: ASCII hyphen used as minus in "${s}"`);
}
export const normKey = p => p.replace(/\([^)]*\)/g, '').replace(/\s+/g, ' ').trim();

// ---------- item builders ----------
function tagsOf(traps) { return [...new Set(traps.map(t => t.tag))]; }
export function amount({prompt, answer, explain, concept, difficulty, params, traps = [], intro = false}) {
  if (!Number.isSafeInteger(answer) || answer < (intro ? 1 : 2) || answer > 59) return null; // 2~59 콘텐츠 원칙(1닢·상한 붓기 자동정답 방지)
  const tr = traps.filter(t => Number.isSafeInteger(t.v) && t.v >= 0 && t.v !== answer);
  return {prompt, answer_mode: 'amount', answer, max: 60, coin_budget: 60, answerNumeric: answer, choices: null, format: 'int', explain, unitConcept: concept, difficulty, distractor_tags: tagsOf(tr), params: {...params, traps: tr}};
}
export function fraction({prompt, n, d, accept = 'reduced', explain, concept, difficulty, params, traps = []}) {
  const [a, b] = reduce(n, d);
  if (b === 1 || a <= 0 || a > 60 || b > 60) return null;
  if (accept === 'reduced' && !prompt.includes('기약분수')) throw Error(`reduced needs 기약분수: ${prompt}`);
  const tr = traps.filter(t => Array.isArray(t.v) && t.v[0] >= 0 && t.v[1] > 0 && t.v[0] * b !== a * t.v[1]);
  return {prompt, answer_mode: 'fraction_parts', answer: {num: a, den: b}, accept, num_label: '분자', den_label: '분모', max: 60, coin_budget: 120, answerNumeric: a / b, choices: null, format: 'frac', explain, unitConcept: concept, difficulty, distractor_tags: tagsOf(tr), params: {...params, traps: tr}};
}
/** distractors: [{v:'text', tag}] — the first three distinct values that differ from the answer are used. */
export function choice({prompt, answer, distractors, explain, concept, difficulty, params, format = 'text', answerNumeric}) {
  const seen = new Set([answer]), picked = [];
  for (const w of distractors) if (w && !seen.has(w.v)) { seen.add(w.v); picked.push(w); if (picked.length === 3) break; }
  if (picked.length !== 3 || new Set(picked.map(w => w.tag)).size < 2) return null;
  const item = {prompt, answer_mode: 'choice', answer, choices: null, format, explain, unitConcept: concept, difficulty, distractor_tags: picked.map(w => w.tag), params: {...params}, _wrong: picked.map(w => w.v)};
  if (format !== 'text') item.answerNumeric = answerNumeric;
  return item;
}
function hash(s) { let h = 2166136261; for (const ch of s) { h ^= ch.codePointAt(0); h = Math.imul(h, 16777619) >>> 0; } return h; }

export function spread(pool, n) { if (pool.length < n) throw Error(`Pool only ${pool.length}, need ${n}`); return Array.from({length: n}, (_, i) => pool[Math.floor(i * pool.length / n)]); }
/**
 * Answer-diverse sampling: bucket the pool by answer, then take round-robin across buckets
 * (each bucket deterministically shuffled). Keeps any single answer value from dominating,
 * so a constant-pour bot cannot beat chance on the pack.
 */
export function pickDiverse(pool, n, seed = 1) {
  if (pool.length < n) throw Error(`Pool only ${pool.length}, need ${n}`);
  const buckets = new Map();
  for (const q of pool) { const k = JSON.stringify(q.answer); if (!buckets.has(k)) buckets.set(k, []); buckets.get(k).push(q); }
  let st = seed >>> 0; const rnd = () => (st = (Math.imul(st, 1664525) + 1013904223) >>> 0) / 2 ** 32;
  const lists = [...buckets.values()].map(b => { const a = [...b]; for (let i = a.length - 1; i > 0; i--) { const r = Math.floor(rnd() * (i + 1)); [a[i], a[r]] = [a[r], a[i]]; } return a; });
  for (let i = lists.length - 1; i > 0; i--) { const r = Math.floor(rnd() * (i + 1)); [lists[i], lists[r]] = [lists[r], lists[i]]; }
  const out = []; for (let round = 0; out.length < n; round++) for (const l of lists) { if (round < l.length) { out.push(l[round]); if (out.length === n) break; } }
  return out;
}
export function uniq(pool, seed = 97) {
  // Shuffle first so that, when several prompts share one validator key (the validator ignores text
  // inside parentheses), the surviving representative is not always the smallest parameter.
  let st = seed >>> 0; const rnd = () => (st = (Math.imul(st, 1664525) + 1013904223) >>> 0) / 2 ** 32;
  const a = pool.filter(Boolean); for (let i = a.length - 1; i > 0; i--) { const r = Math.floor(rnd() * (i + 1)); [a[i], a[r]] = [a[r], a[i]]; }
  const s = new Set(), out = []; for (const q of a) { if (!q) continue; const k = normKey(q.prompt); if (!s.has(k)) { s.add(k); out.push(q); } } return out; }

/**
 * groups: [[pool, count], …]. pools are deduplicated by the validator's normalized prompt key, sampled
 * evenly, interleaved deterministically, and globally deduplicated. The intro item is always items[0].
 */
export function writeUnitPack({id, title, unit, standards, intro, groups, semester = 1, capShare = 0.045}) {
  const total = 1 + groups.reduce((a, [, n]) => a + n, 0), cap = Math.ceil(total * capShare);
  const lists = groups.map(([pool, n], gi) => { const u = uniq(pool); if (u.length < n) throw Error(`${id} group ${gi}: pool ${u.length} < ${n}`); return pickDiverse(u, u.length, 7919 * (gi + 1) + id.length); });
  const ansKey = q => q.answer_mode === 'choice' ? null : JSON.stringify(q.answer);
  const ordered = [intro], seen = new Set([normKey(intro.prompt)]), count = new Map([[ansKey(intro), 1]]);
  const ptr = groups.map(() => 0), taken = groups.map(() => 0);
  for (let round = 0; taken.some((t, g) => t < groups[g][1]); round++) for (let g = 0; g < groups.length; g++) {
    if (taken[g] >= groups[g][1]) continue;
    for (;;) {
      const q = lists[g][ptr[g]++];
      if (!q) throw Error(`${id} group ${g}: ran out after ${taken[g]}/${groups[g][1]} under answer cap ${cap}`);
      const k = normKey(q.prompt), a = ansKey(q);
      if (seen.has(k) || (a && (count.get(a) ?? 0) >= cap)) continue;
      seen.add(k); if (a) count.set(a, (count.get(a) ?? 0) + 1); ordered.push(q); taken[g]++; break;
    }
  }
  const items = ordered.map((raw, i) => {
    const {_wrong, ...q} = raw; const item = {id: `${id}-${String(i + 1).padStart(3, '0')}`, ...q};
    if (q.answer_mode === 'choice') { const slot = hash(q.prompt) % 4; item.choices = [..._wrong]; item.choices.splice(slot, 0, q.answer); }
    for (const f of ['prompt', 'explain']) checkText(item[f], `${item.id}.${f}`);
    for (const c of item.choices ?? []) checkText(c, `${item.id}.choice`);
    scanKeys(item.params, item.id);
    return item;
  });
  if (items.length < 300) throw Error(`${id}: only ${items.length} unique items`);
  const pack = {schema_version: 2, pack_id: id, title, school: 'middle', grade: 2, semester, unit_id: unit, standards, economy: {carry_capacity: 120, coin_per_kill: 1, min_spawn_coins: 140}, items};
  fs.mkdirSync(output, {recursive: true});
  fs.writeFileSync(path.join(output, `${id}.json`), JSON.stringify(pack));
  const modes = {}, diff = {}; for (const q of items) { modes[q.answer_mode] = (modes[q.answer_mode] ?? 0) + 1; diff[q.difficulty] = (diff[q.difficulty] ?? 0) + 1; }
  const pour = items.filter(q => q.answer_mode !== 'choice'), freq = {}; for (const q of pour) { const k = JSON.stringify(q.answer); freq[k] = (freq[k] ?? 0) + 1; }
  const top = Object.entries(freq).sort((a, b) => b[1] - a[1])[0];
  console.log(`${id}: most common pour answer ${top[0]} = ${top[1]}/${pour.length} (${(100 * top[1] / pour.length).toFixed(1)}%)`);
  console.log(`${id}: ${items.length} items / ${Buffer.byteLength(JSON.stringify(pack))} bytes / modes ${JSON.stringify(modes)} / difficulty ${JSON.stringify(diff)}`);
  return pack;
}
