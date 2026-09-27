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
  if (/[0-9a-z)]-[0-9a-z(]|-\d/.test(s)) throw Error(`${where}: ASCII hyphen used as minus in "${s}"`);
  if (/기울기을|절편를|[+−]{2}|=\+/.test(s)) throw Error(`${where}: josa/sign typo in "${s}"`);
}
export const normKey = p => p.replace(/\([^)]*\)/g, '').replace(/\s+/g, ' ').trim();

// ---------- item builders (schema v3: every item is a 4-choice question) ----------
// Numeric values are integers or [n, d] rationals. Wrong choices ("traps") must come from a named
// misconception: {v, tag}. A trap with v === null / undefined is "not applicable" and is skipped.
// The first three traps whose values differ from the answer AND from each other (as reduced rationals)
// become the distractors, so list traps in order of preference.
function tagsOf(ws) { return ws.map(w => w.tag); }
const toR = v => Array.isArray(v) ? reduce(v[0], v[1]) : [v, 1];
const rkey = v => { const [n, d] = toR(v); return `${n}/${d}`; };
const isNumVal = v => Number.isSafeInteger(v) || (Array.isArray(v) && v.length === 2 && Number.isSafeInteger(v[0]) && Number.isSafeInteger(v[1]) && v[1] !== 0);
const showV = v => { const [n, d] = toR(v); return rat(n, d); };
/** Absolute values (as reduced rational keys) of every number visible in a prompt: integers, decimals, {frac} tokens. */
export function promptNumbers(prompt) {
  const out = new Set(); let t = prompt.replace(/\{frac:(\d+)\/(\d+)\}/g, (_, n, d) => { const [a, b] = reduce(+n, +d); out.add(`${a}/${b}`); return ' '; });
  t = t.replace(/<\/?sup>/g, ' ');
  for (const m of t.matchAll(/\d+(?:\.\d+)?/g)) { const [a, b = ''] = m[0].split('.'); const [x, y] = reduce(Number(a + b), 10 ** b.length); out.add(`${x}/${y}`); }
  return out;
}
const absKey = v => { const [n, d] = toR(v); return `${Math.abs(n)}/${d}`; };
/**
 * Copy guard (cross-check finding: the answer equals a number printed in the prompt, so copying it wins).
 * An item is rejected when |answer| is printed in the prompt and no distractor's |value| is — copying would
 * then single out the answer. When at least one distractor is also printed, copying no longer decides.
 */
export function copyVulnerable(prompt, answer, wrongs) { const nums = promptNumbers(prompt); return nums.has(absKey(answer)) && !wrongs.some(w => nums.has(absKey(w))); }

/**
 * Numeric 4-choice item. answer: integer or [n, d]. traps: [{v, tag}] (v integer | [n, d] | null).
 * neg=false (default) drops negative distractors — for counts, lengths, times and other quantities that
 * cannot be negative. far: distractors farther than max(100, 10·|answer|) from zero are dropped (an absurd
 * magnitude would give the answer away). decimal=true shows non-integer distractors as terminating decimals
 * (context problems whose data are decimals, e.g. 0.6 °C씩); non-terminating ones are skipped.
 */
const terminating = ([n, d]) => { while (d % 2 === 0) d /= 2; while (d % 5 === 0) d /= 5; return d === 1; };
export function numChoice({prompt, answer, explain, concept, difficulty, params, traps = [], neg = false, copyOk = false, decimal = false}) {
  if (!isNumVal(answer)) throw Error(`bad answer in ${prompt}`);
  const [an, ad] = toR(answer); const aKey = `${an}/${ad}`, lim = Math.max(100, 10 * Math.abs(an / ad));
  if (!neg && an < 0) throw Error(`negative answer needs neg:true: ${prompt}`);
  const seen = new Set([aKey]), cands = [];
  for (const t of traps) {
    if (!t || t.v === null || t.v === undefined || !isNumVal(t.v)) continue;
    const [n, d] = toR(t.v); const k = `${n}/${d}`;
    if (seen.has(k) || (!neg && n < 0) || Math.abs(n / d) > lim || (decimal && !terminating([n, d]))) continue;
    seen.add(k); cands.push({v: [n, d], tag: t.tag, text: decimal && d !== 1 ? dec(n, d) : showV([n, d])});
  }
  // Every admissible 3-subset (≥2 distinct misconception tags, not copy-vulnerable), in preference order.
  const subsets = [];
  for (let i = 0; i < cands.length; i++) for (let j = i + 1; j < cands.length; j++) for (let k = j + 1; k < cands.length; k++) {
    const w = [cands[i], cands[j], cands[k]];
    if (new Set(w.map(x => x.tag)).size < 2 || (!copyOk && copyVulnerable(prompt, [an, ad], w.map(x => x.v)))) continue;
    subsets.push([i, j, k]);
  }
  if (!subsets.length) return null;
  const picked = subsets[0].map(i => cands[i]);
  return {prompt, answer_mode: 'choice', answer: rat(an, ad), choices: null, format: ad === 1 ? 'int' : 'frac', answerNumeric: an / ad, explain, unitConcept: concept, difficulty,
    distractor_tags: tagsOf(picked), params: {...params}, _wrong: picked.map(w => w.text), _tags: tagsOf(picked), _num: {ans: [an, ad], cands, subsets}};
}
/** Rank (0 = smallest … 3 = largest) of the answer among itself and three distractor values. */
const rankOf = (ans, ws) => ws.filter(([n, d]) => n * ans[1] < ans[0] * d).length;
/** v2 amount items, now numeric choices (same call shape as before; `intro` kept for call compatibility). */
export function amount({intro, ...o}) { void intro; return numChoice(o); }
/** v2 fraction items, now numeric choices. The answer is always shown reduced; no equivalent distractor can appear. */
export function fraction({prompt, n, d, accept, ...o}) {
  void accept; if (accept === 'reduced' && !prompt.includes('기약분수')) throw Error(`reduced needs 기약분수: ${prompt}`);
  return numChoice({prompt, answer: [n, d], ...o});
}
/** Text 4-choice item. distractors: [{v:'text', tag}] — the first three distinct values that differ from the answer are used. */
export function choice({prompt, answer, distractors, explain, concept, difficulty, params, format = 'text', answerNumeric}) {
  const seen = new Set([answer]), picked = [];
  for (const w of distractors) if (w && !seen.has(w.v)) { seen.add(w.v); picked.push(w); if (picked.length === 3) break; }
  if (picked.length !== 3 || new Set(picked.map(w => w.tag)).size < 2) return null;
  const item = {prompt, answer_mode: 'choice', answer, choices: null, format, explain, unitConcept: concept, difficulty, distractor_tags: picked.map(w => w.tag), params: {...params}, _wrong: picked.map(w => w.v), _tags: picked.map(w => w.tag)};
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
 * Answer slots are balanced (each of the 4 positions gets ⌊N/4⌋ or ⌈N/4⌉ answers, shuffled), and the
 * three distractors are shuffled; distractor_tags[i] belongs to the i-th wrong choice in display order.
 */
export function writeUnitPack({id, title, unit, standards, intro, groups, semester = 1, capShare = 0.06}) {
  if (!intro) throw Error(`${id}: intro item was rejected by the builder`);
  const total = 1 + groups.reduce((a, [, n]) => a + n, 0), cap = Math.ceil(total * capShare);
  if (process.env.POOLS) { for (const [gi, [pool, n]] of groups.entries()) { const u = uniq(pool); const t = u[0]?.params?.t ?? pool.find(Boolean)?.params?.t; console.log(`${u.length < n ? '!!' : '  '} group ${gi} ${t}: ${u.length} / need ${n} (raw ${pool.length}, built ${pool.filter(Boolean).length})`); } }
  const lists = groups.map(([pool, n], gi) => { const u = uniq(pool); if (u.length < n) throw Error(`${id} group ${gi} (${u[0]?.params?.t}): pool ${u.length} < ${n}`); return pickDiverse(u, u.length, 7919 * (gi + 1) + id.length); });
  const ansKey = q => q.format === 'text' ? null : q.answer;
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
  // Value-rank balancing: misconception distractors skew one way (e.g. "not divided" is always bigger), so a
  // student who always picks the 2nd-largest number would beat chance. Greedily choose, for each numeric item,
  // the admissible distractor subset that puts the answer at the rank used least so far (ties → preference order).
  const rankCount = [0, 0, 0, 0];
  for (const q of ordered) {
    if (!q._num) continue; const {ans, cands, subsets} = q._num; let best = null, bestScore = Infinity;
    for (const sub of subsets) { const r = rankOf(ans, sub.map(i => cands[i].v)); if (rankCount[r] < bestScore) { bestScore = rankCount[r]; best = [sub, r]; } }
    rankCount[best[1]]++; const picked = best[0].map(i => cands[i]);
    q._wrong = picked.map(w => w.text); q._tags = tagsOf(picked); q.distractor_tags = q._tags;
  }
  let st = (hash(id) ^ 0x9e3779b9) >>> 0; const rnd = () => (st = (Math.imul(st, 1664525) + 1013904223) >>> 0) / 2 ** 32;
  const slots = ordered.map((_, i) => i % 4); for (let i = slots.length - 1; i > 0; i--) { const r = Math.floor(rnd() * (i + 1)); [slots[i], slots[r]] = [slots[r], slots[i]]; }
  const items = ordered.map((raw, i) => {
    const {_wrong, _tags, _num, ...q} = raw; const item = {id: `${id}-${String(i + 1).padStart(3, '0')}`, ...q};
    const order = [0, 1, 2]; for (let k = 2; k > 0; k--) { const r = Math.floor(rnd() * (k + 1)); [order[k], order[r]] = [order[r], order[k]]; }
    item.choices = order.map(k => _wrong[k]); item.distractor_tags = order.map(k => _tags[k]); item.choices.splice(slots[i], 0, q.answer);
    for (const f of ['prompt', 'explain']) checkText(item[f], `${item.id}.${f}`);
    for (const c of item.choices) checkText(c, `${item.id}.choice`);
    if (/약분하지|코인|닢|붓/.test(item.prompt + item.explain)) throw Error(`${item.id}: pour-era wording`);
    scanKeys(item.params, item.id);
    return item;
  });
  if (items.length < 300) throw Error(`${id}: only ${items.length} unique items`);
  // economy is kept byte-identical to the m2s2 v3 packs so the loader sees one pack shape (DESIGN-V3 §3 moves coins to
  // tower upgrades; the pack no longer prices any answer).
  const pack = {schema_version: 3, pack_id: id, title, school: 'middle', grade: 2, semester, unit_id: unit, standards, economy: {carry_capacity: 120, coin_per_kill: 1, min_spawn_coins: 140}, items};
  fs.mkdirSync(output, {recursive: true});
  fs.writeFileSync(path.join(output, `${id}.json`), JSON.stringify(pack));
  const diff = {}, pos = [0, 0, 0, 0], fmt = {}; for (const q of items) { diff[q.difficulty] = (diff[q.difficulty] ?? 0) + 1; pos[q.choices.indexOf(q.answer)]++; fmt[q.format] = (fmt[q.format] ?? 0) + 1; }
  console.log(`${id}: ${items.length} items / ${Buffer.byteLength(JSON.stringify(pack))} bytes / difficulty ${JSON.stringify(diff)} / answer slots ${pos.join(',')} / value ranks ${rankCount.join(',')} / formats ${JSON.stringify(fmt)}`);
  return pack;
}
