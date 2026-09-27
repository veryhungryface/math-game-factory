#!/usr/bin/env node
// Independent oracle for the m2s1-u1..u6 packs.
// It imports NO generator code and NO m2s1-lib helpers. Every answer is recomputed from the item's
// `params` (and, wherever possible, from the exact text the student sees) by a different method:
//   - display math is parsed by this file's own parser and evaluated with BigInt rationals;
//   - algebraic identities are checked by evaluation at several rational points (not by symbolic algebra);
//   - repeating decimals: digits by BigInt long division, period by multiplicative order,
//     decimal→fraction from the displayed digits; termination by long division;
//   - inequalities: integer brute force over [−400, 400] plus exact boundary probing;
//   - linear systems: integer grid search + determinant; word problems: brute-force enumeration.
// Schema v3 (every item 4-choice): the oracle value must match exactly one of the four choices BY VALUE
// (choices are parsed by this file's parser; 2/4 = 1/2 = 0.5), the other three must all be wrong,
// no two choices may be equal in value, answer positions must be balanced, and heuristic bots
// (fixed slot, copy-a-number-from-the-prompt, min/max/rank, odd-sign-out) must stay near chance.
// Structure is checked with validate-pack-v3-m2s1.mjs. `--selftest` plants errors and confirms each is caught.
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';
import {validatePackV3} from './validate-pack-v3-m2s1.mjs';

const here = path.dirname(fileURLToPath(import.meta.url)), root = path.resolve(here, '../../../../..');
const packDir = path.join(root, 'public/g/hyeopgok-sasu/packs');
const curriculum = JSON.parse(fs.readFileSync(path.join(root, 'curriculum/2022-middle-math.json'), 'utf8'));

// ---------------- BigInt rationals ----------------
const bgcd = (a, b) => { a = a < 0n ? -a : a; b = b < 0n ? -b : b; while (b) [a, b] = [b, a % b]; return a || 1n; };
function Q(n, d = 1n) { n = BigInt(n); d = BigInt(d); if (d === 0n) throw Error('division by zero'); if (d < 0n) { n = -n; d = -d; } const g = bgcd(n, d); return {n: n / g, d: d / g}; }
const qadd = (a, b) => Q(a.n * b.d + b.n * a.d, a.d * b.d), qsub = (a, b) => Q(a.n * b.d - b.n * a.d, a.d * b.d);
const qmul = (a, b) => Q(a.n * b.n, a.d * b.d), qdiv = (a, b) => Q(a.n * b.d, a.d * b.n);
const qeq = (a, b) => a.n === b.n && a.d === b.d, qneg = a => Q(-a.n, a.d);
const qpow = (a, e) => { if (e.d !== 1n || e.n < 0n) throw Error('exponent must be a natural number'); return Q(a.n ** e.n, a.d ** e.n); };
const qint = a => a.d === 1n, qcmp = (a, b) => { const x = a.n * b.d - b.n * a.d; return x > 0n ? 1 : x < 0n ? -1 : 0; };
const qnum = a => Number(a.n) / Number(a.d);

// ---------------- display-math parser ----------------
const REL = new Set(['=', '<', '>', '≤', '≥']);
function tokenize(s) {
  const t = []; let i = 0;
  while (i < s.length) {
    const c = s[i];
    if (c === ' ') { i++; continue; }
    if (s.startsWith('<sup>', i)) { const end = s.indexOf('</sup>', i); t.push({k: 'sup', v: s.slice(i + 5, end)}); i = end + 6; continue; }
    if (s.startsWith('{frac:', i)) { const m = /^\{frac:(\d+)\/(\d+)\}/.exec(s.slice(i)); t.push({k: 'num', v: Q(m[1], m[2])}); i += m[0].length; continue; }
    if (c === '²' || c === '³') { t.push({k: 'sup', v: c === '²' ? '2' : '3'}); i++; continue; }
    if (/\d/.test(c)) { const m = /^\d+(\.\d+)?/.exec(s.slice(i)); const [a, b = ''] = m[0].split('.'); t.push({k: 'num', v: Q(BigInt(a + b), 10n ** BigInt(b.length))}); i += m[0].length; continue; }
    if (/[a-zA-Z]/.test(c)) { t.push({k: 'var', v: c}); i++; continue; }
    if ('+−-×÷(){}[]=<>≤≥'.includes(c)) { t.push({k: 'op', v: c === '-' ? '−' : c}); i++; continue; }
    throw Error(`tokenize: unexpected "${c}" in "${s}"`);
  }
  return t;
}
function parse(tokens) {
  let i = 0; const peek = () => tokens[i], take = () => tokens[i++];
  const isOp = (v) => peek()?.k === 'op' && peek().v === v;
  const startsAtom = () => { const p = peek(); return p && (p.k === 'num' || p.k === 'var' || (p.k === 'op' && '({['.includes(p.v))); };
  function expr() {
    let node;
    if (isOp('−')) { take(); node = {op: 'neg', a: term()}; } else { if (isOp('+')) take(); node = term(); }
    while (isOp('+') || isOp('−')) { const o = take().v; node = {op: o === '+' ? 'add' : 'sub', a: node, b: term()}; }
    return node;
  }
  function term() { let node = factor(); while (isOp('×') || isOp('÷')) { const o = take().v; node = {op: o === '×' ? 'mul' : 'div', a: node, b: factor()}; } return node; }
  function factor() { if (isOp('−')) { take(); return {op: 'neg', a: factor()}; } return juxt(); }
  function juxt() { let node = power(); while (startsAtom()) node = {op: 'mul', a: node, b: power()}; return node; }
  function power() { let node = atom(); while (peek()?.k === 'sup') { const inner = take().v; node = {op: 'pow', a: node, b: parse(tokenize(inner))}; } return node; }
  function atom() {
    const p = take(); if (!p) throw Error('unexpected end');
    if (p.k === 'num') return {op: 'num', v: p.v};
    if (p.k === 'var') return {op: 'var', v: p.v};
    const close = {'(': ')', '{': '}', '[': ']'}[p.v];
    if (close) { const e = expr(); const c = take(); if (!c || c.v !== close) throw Error('bracket mismatch'); return e; }
    throw Error(`unexpected token ${p.v}`);
  }
  const e = expr(); if (i !== tokens.length) throw Error(`trailing tokens at ${i}: ${tokens.slice(i).map(t => t.v).join('')}`); return e;
}
const parseExpr = s => parse(tokenize(s));
function evalNode(n, env) {
  switch (n.op) {
    case 'num': return n.v;
    case 'var': if (!(n.v in env)) throw Error(`unbound ${n.v}`); return env[n.v];
    case 'neg': return qneg(evalNode(n.a, env));
    case 'add': return qadd(evalNode(n.a, env), evalNode(n.b, env));
    case 'sub': return qsub(evalNode(n.a, env), evalNode(n.b, env));
    case 'mul': return qmul(evalNode(n.a, env), evalNode(n.b, env));
    case 'div': return qdiv(evalNode(n.a, env), evalNode(n.b, env));
    case 'pow': return qpow(evalNode(n.a, env), evalNode(n.b, env));
  }
  throw Error(n.op);
}
const evalStr = (s, env = {}) => evalNode(parseExpr(s), env);
/** Split a relation string at top-level relation symbols (tags already tokenized away). */
function splitRel(s) {
  const tokens = tokenize(s), parts = [[]], rels = [];
  for (const t of tokens) if (t.k === 'op' && REL.has(t.v)) { rels.push(t.v); parts.push([]); } else parts.at(-1).push(t);
  return {parts: parts.map(p => parse(p)), rels};
}
const POINTS = [
  {a: Q(3), b: Q(-7), x: Q(5), y: Q(-2)}, {a: Q(-4), b: Q(11), x: Q(-3), y: Q(13)}, {a: Q(17), b: Q(2), x: Q(7), y: Q(19)},
  {a: Q(-5), b: Q(-3), x: Q(23), y: Q(-11)}, {a: Q(2, 3), b: Q(-5, 7), x: Q(3, 11), y: Q(13, 5)}, {a: Q(29), b: Q(-31), x: Q(-37), y: Q(41)},
];
function identical(lhs, rhs, extra = {}) { const L = parseExpr(lhs), R = parseExpr(rhs); return POINTS.every(p => qeq(evalNode(L, {...p, ...extra}), evalNode(R, {...p, ...extra}))); }

// ---------------- number-theory helpers (U1) ----------------
function v(q, p) { let e = 0; while (q % p === 0) { q /= p; e++; } return e; }
function decimalShape(p, q) { // preperiod s and period L of p/q, via 2/5-adic valuation and multiplicative order of 10
  const g = Number(bgcd(BigInt(p), BigInt(q))); q /= g; const s = Math.max(v(q, 2), v(q, 5)); let m = q; while (m % 2 === 0) m /= 2; while (m % 5 === 0) m /= 5;
  if (m === 1) return {s, L: 0}; let L = 1, r = 10 % m; while (r !== 1) { r = (r * 10) % m; L++; } return {s, L};
}
const digit = (p, q, n) => Number((BigInt(p) * 10n ** BigInt(n) / BigInt(q)) % 10n);
function terminatesLD(n, d) { let r = BigInt(n) % BigInt(d); for (let k = 0; k < 200 && r; k++) r = (r * 10n) % BigInt(d); return r === 0n; }
function parseShown(shown) { const m = /^(\d+)\.(\d+)…$/.exec(shown); if (!m) throw Error(`bad decimal ${shown}`); return {int: m[1], ds: m[2]}; }
function shownConsistent(shown, p, q) { const {int, ds} = parseShown(shown); if (BigInt(int) !== BigInt(p) / BigInt(q)) return false; for (let k = 1; k <= ds.length; k++) if (Number(ds[k - 1]) !== digit(p, q, k)) return false; return true; }
function readRepeating(shown) { // minimal (s, L) with ≥2 full periods visible; value from digits only
  const {int, ds} = parseShown(shown);
  for (let s = 0; s < ds.length; s++) for (let L = 1; s + 2 * L <= ds.length; L++) {
    let ok = true; for (let k = s + L; k < ds.length; k++) if (ds[k] !== ds[k - L]) { ok = false; break; }
    if (!ok) continue;
    const N = BigInt(int + ds.slice(0, s + L)), Mv = BigInt(int + ds.slice(0, s));
    return {s, L, value: Q(N - Mv, 10n ** BigInt(s) * (10n ** BigInt(L) - 1n))};
  }
  throw Error(`no repeating pattern in ${shown}`);
}
const fracTok = (n, d) => `{frac:${n}/${d}}`;
const numTxt = n => (n < 0 ? '−' + (-n) : String(n));

// ---------------- per-type oracles ----------------
// Each returns the oracle answer (number for amount, [n,d] for fractions, string for choice) or throws.
function substBox(rhs, ans) { if (!rhs.includes('□')) throw Error('no box'); const t = typeof ans === 'number' && ans < 0 ? `(${numTxt(ans)})` : String(ans); return rhs.split('□').join(t); }
function boxAnswer(lhs, rhs, candidates = 150) { // the unique integer that makes lhs ≡ rhs
  const hits = []; for (let a = -candidates; a <= candidates; a++) { let ok; try { ok = identical(lhs, substBox(rhs, a)); } catch { ok = false; } if (ok) hits.push(a); }
  if (hits.length !== 1) throw Error(`box has ${hits.length} solutions (${hits})`); return hits[0];
}
const ORACLE = {
  // ---- U1 ----
  'period-block'(q, P) { need(shownConsistent(P.shown, P.p, P.q), 'shown digits'); const {s, L} = decimalShape(P.p, P.q); need(L > 0, 'repeating'); need(q.prompt.includes(P.shown), 'prompt shows decimal'); let b = ''; for (let k = s + 1; k <= s + L; k++) b += digit(P.p, P.q, k); need(b[0] !== '0', 'no leading zero block'); const r = readRepeating(P.shown); need(r.s === s && r.L === L, 'display unambiguous'); return Number(b); },
  'period-length'(q, P) { need(q.prompt.includes(fracTok(P.p, P.q)), 'prompt fraction'); const {L} = decimalShape(P.p, P.q); return L; },
  'nth-digit'(q, P) { need(q.prompt.includes(`${fracTok(P.p, P.q)}=`) && q.prompt.includes(`아래 ${P.n}번째`), 'prompt'); const shown = /=([\d.]+…)일 때/.exec(q.prompt)[1]; need(shownConsistent(shown, P.p, P.q), 'shown digits'); return digit(P.p, P.q, P.n); },
  'min-multiplier'(q, P) { need(q.prompt.includes(fracTok(P.p, P.q)), 'prompt fraction'); for (let a = 1; a < 5000; a++) if (terminatesLD(P.p * a, P.q)) return a; throw Error('none'); },
  'count-terminating'(q, P) { for (const [n, d] of P.list) need(q.prompt.includes(fracTok(n, d)), 'prompt list'); need((q.prompt.match(/\{frac:/g) ?? []).length === P.list.length, 'list size'); return P.list.filter(([n, d]) => terminatesLD(n, d)).length; },
  'to-fraction'(q, P) { need(shownConsistent(P.shown, P.p, P.q) && q.prompt.includes(P.shown), 'shown'); const r = readRepeating(P.shown); const {s, L} = decimalShape(P.p, P.q); need(r.s === s && r.L === L, 'display unambiguous'); need(qeq(r.value, Q(P.p, P.q)), 'digits vs params'); return [Number(r.value.n), Number(r.value.d)]; },
  'shift-subtract'(q, P) { const shown = /x=([\d.]+…)일 때/.exec(q.prompt)[1]; need(shownConsistent(shown, P.p, P.q), 'shown'); need(q.prompt.includes(`${P.A}x−${P.B === 1 ? '' : P.B}x의 값`), 'prompt expr'); const x = readRepeating(shown).value; const val = qsub(qmul(Q(P.A), x), qmul(Q(P.B), x)); need(qint(val), 'integer'); return Number(val.n); },
  'shift-choice'(q, P) { need(shownConsistent(P.shown, P.p, P.q), 'shown'); const x = readRepeating(P.shown).value; const ok = q.choices.filter(c => { const m = /^(\d+)x−(\d*)x$/.exec(c); return qint(qmul(Q(BigInt(m[1]) - BigInt(m[2] || 1)), x)); }); need(ok.length === 1, `integer choices ${ok.length}`); return ok[0]; },
  'terminating-choice'(q) { for (const c of q.choices) need(q.prompt.includes(c), 'choices listed in prompt'); const ok = q.choices.filter(c => { const [, n, d] = /^\{frac:(\d+)\/(\d+)\}$/.exec(c); return terminatesLD(n, d); }); need(ok.length === 1, `terminating choices ${ok.length}`); return ok[0]; },
  'count-multipliers'(q, P) { need(q.prompt.includes(fracTok(P.p, P.q)) && q.prompt.includes(`${P.N} 이하`), 'prompt'); let c = 0; for (let x = 1; x <= P.N; x++) if (terminatesLD(P.p * x, P.q)) c++; return c; },
  'concept-rational'(q, P) { need(shownConsistent(P.shown, P.p, P.q), 'shown'); need(decimalShape(P.p, P.q).L > 0, 'is repeating'); const truth = {'유리수이다': true, '유리수가 아니다': false, '분수로 나타낼 수 없다': false, '유한소수이다': false}; need(q.choices.every(c => c in truth), 'known statements'); const ok = q.choices.filter(c => truth[c]); need(ok.length === 1, 'one true'); return ok[0]; },
  // ---- U2 ----
  'identity-box'(q, P) { need(q.prompt.includes(`${P.lhs}=${P.rhs}`), 'prompt identity'); return boxAnswer(P.lhs, P.rhs); },
  'div-box'(q, P) { need(q.prompt.includes(P.dividend) && q.prompt.includes(P.divisor) && q.prompt.includes(P.rhs), 'prompt parts'); return boxAnswer(`(${P.dividend})÷(${P.divisor})`, P.rhs); },
  'subst-box'(q, P) {
    need(q.prompt.includes(`A=${P.A}, B=${P.B}`) && q.prompt.includes(P.form) && q.prompt.includes(P.rhs), 'prompt parts');
    const hits = []; for (let a = -150; a <= 150; a++) { const R = parseExpr(substBox(P.rhs, a < 0 ? `(${numTxt(a)})` : a)), F = parseExpr(P.form), A = parseExpr(P.A), B = parseExpr(P.B); if (POINTS.every(p => qeq(evalNode(F, {A: evalNode(A, p), B: evalNode(B, p)}), evalNode(R, p)))) hits.push(a); }
    need(hits.length === 1, 'unique'); return hits[0];
  },
  eval(q, P) { need(q.prompt.includes(P.expr), 'prompt expr'); for (const [k, val] of Object.entries(P.vals)) need(q.prompt.includes(`${k}=${numTxt(val)}`), 'prompt values'); const r = evalStr(P.expr, Object.fromEntries(Object.entries(P.vals).map(([k, val]) => [k, Q(val)]))); need(qint(r), 'integer'); return Number(r.n); },
  'exp-solve'(q, P) { need(q.prompt.includes(`${P.lhs}=${P.rhs}`), 'prompt'); const hits = []; for (let a = 0; a <= 70; a++) if (qeq(evalStr(P.lhs, {x: Q(a)}), evalStr(P.rhs))) hits.push(a); need(hits.length === 1, 'unique'); return hits[0]; },
  digits(q, P) { need(q.prompt.startsWith(P.expr), 'prompt'); const r = evalStr(P.expr); need(qint(r), 'integer'); return r.n.toString().length; },
  'identity-choice'(q, P) { need(q.prompt.startsWith(P.lhs), 'prompt'); const ok = q.choices.filter(c => identical(P.lhs, c)); need(ok.length === 1, `identity choices ${ok.length}`); return ok[0]; },
};
function need(ok, msg) { if (!ok) throw Error(msg); }

// ---- U3: inequalities ----
function ineqPred(ineq) {
  const {parts, rels} = splitRel(ineq); need(parts.length === 2 && rels.length === 1, 'exactly one relation');
  const [L, Rn] = parts, rel = rels[0];
  return x => { const c = qcmp(evalNode(L, {x}), evalNode(Rn, {x})); return rel === '>' ? c > 0 : rel === '<' ? c < 0 : rel === '≥' ? c >= 0 : rel === '≤' ? c <= 0 : c === 0; };
}
function ineqSet(ineq) { // exact solution set from the displayed inequality: linear probe + boundary tests
  const {parts} = splitRel(ineq), g = x => qsub(evalNode(parts[0], {x}), evalNode(parts[1], {x}));
  const g0 = g(Q(0)), m = qsub(g(Q(1)), g0); need(m.n !== 0n, 'x coefficient nonzero');
  for (const t of [2, 3, -5, 7, 11]) need(qeq(g(Q(t)), qadd(g0, qmul(m, Q(t)))), 'linear');
  const root = qdiv(qneg(g0), m), pred = ineqPred(ineq), eps = Q(1, 1000);
  const right = pred(qadd(root, eps)), left = pred(qsub(root, eps)), at = pred(root); need(right !== left, 'one-sided solution');
  return {rel: right ? (at ? '≥' : '>') : (at ? '≤' : '<'), bound: root};
}
function eqPred(e) { const {parts, rels} = splitRel(e); need(parts.length === 2 && rels[0] === '=', 'one equation'); return env => qeq(evalNode(parts[0], env), evalNode(parts[1], env)); }
function coeffs(e) { // [a, b, c] of a·x+b·y=c by probing the displayed equation (also proves linearity)
  const {parts} = splitRel(e), f = (x, y) => qsub(evalNode(parts[0], {x: Q(x), y: Q(y)}), evalNode(parts[1], {x: Q(x), y: Q(y)}));
  const f0 = f(0, 0), a = qsub(f(1, 0), f0), b = qsub(f(0, 1), f0);
  for (const [x, y] of [[2, 3], [-5, 7], [11, -4]]) need(qeq(f(x, y), qadd(f0, qadd(qmul(a, Q(x)), qmul(b, Q(y))))), 'linear equation');
  return [a, b, qneg(f0)];
}
function solveEqs(pairs) { // pairs of parsed [lhs, rhs]; integer grid search, then determinant ⇒ unique
  const E = pairs.map(([l, r]) => env => qeq(evalNode(l, env), evalNode(r, env)));
  const hits = []; for (let x = -80; x <= 80; x++) for (let y = -80; y <= 80; y++) { const env = {x: Q(x), y: Q(y)}; if (E.every(f => f(env))) hits.push([x, y]); }
  need(hits.length === 1, `integer solutions ${hits.length}`);
  const co = pairs.map(([l, r]) => { const f = (x, y) => qsub(evalNode(l, {x: Q(x), y: Q(y)}), evalNode(r, {x: Q(x), y: Q(y)})); const f0 = f(0, 0); return [qsub(f(1, 0), f0), qsub(f(0, 1), f0)]; });
  need(qsub(qmul(co[0][0], co[1][1]), qmul(co[0][1], co[1][0])).n !== 0n, 'determinant nonzero');
  return hits[0];
}
function coeffsWith(e, env0) { const {parts} = splitRel(e), f = (x, y) => qsub(evalNode(parts[0], {...env0, x: Q(x), y: Q(y)}), evalNode(parts[1], {...env0, x: Q(x), y: Q(y)})); const f0 = f(0, 0); return [qsub(f(1, 0), f0), qsub(f(0, 1), f0), qneg(f0)]; }
function proportional(A, B) { // [a,b,c] ∝ [a',b',c'] (same line)
  return qeq(qmul(A[0], B[1]), qmul(A[1], B[0])) && qeq(qmul(A[0], B[2]), qmul(A[2], B[0])) && qeq(qmul(A[1], B[2]), qmul(A[2], B[1])) && (A[0].n || A[1].n) && (B[0].n || B[1].n);
}
function solveRational(es) { const [[a1, b1, c1], [a2, b2, c2]] = es.map(coeffs); const d = qsub(qmul(a1, b2), qmul(a2, b1)); need(d.n !== 0n, 'det'); return [qdiv(qsub(qmul(c1, b2), qmul(c2, b1)), d), qdiv(qsub(qmul(a1, c2), qmul(a2, c1)), d)]; }
const solveSystem = es => solveEqs(es.map(e => { const {parts} = splitRel(e); return parts; }));
function findAB(es, x0, y0) { const preds = es.map(e => { const {parts} = splitRel(e); return env => qeq(evalNode(parts[0], env), evalNode(parts[1], env)); }); const hits = []; for (let a = -60; a <= 60; a++) for (let b = -60; b <= 60; b++) { const env = {a: Q(a), b: Q(b), x: Q(x0), y: Q(y0)}; if (preds.every(f => f(env))) hits.push([a, b]); } need(hits.length === 1, `coefficient pairs ${hits.length}`); return hits[0]; }
// "y가 x의 함수인가" — every relation is re-implemented here as a set of y values per x (x = 1..30).
const FUNC_TEXT = {'s-plus3': '자연수 x보다 3만큼 큰 수 y', 's-bread': '한 개에 700원인 빵 x개의 값 y원', 's-square': '한 변의 길이가 x cm인 정사각형의 둘레의 길이 y cm', 's-mod5': '자연수 x를 5로 나눈 나머지 y', 's-speed': '시속 x km로 3시간 동안 달린 거리 y km', 's-divcount': '자연수 x의 약수의 개수 y', 's-area20': '넓이가 20 cm²인 직사각형의 가로의 길이 x cm와 세로의 길이 y cm', 's-day': '하루 중 낮의 길이 x시간과 밤의 길이 y시간', 's-age': 'x살인 학생의 5년 후의 나이 y살', 'n-divisor': '자연수 x의 약수 y', 'n-multiple': '자연수 x의 배수 y', 'n-abs': '절댓값이 x인 정수 y (x는 자연수)', 'n-oddless': '자연수 x보다 작은 홀수 y', 'n-perimeter': '둘레의 길이가 x cm인 직사각형의 넓이 y cm²', 'n-coprime': '자연수 x와 서로소인 자연수 y'};
const RELATIONS = {
  's-plus3': x => [x + 3], 's-bread': x => [700 * x], 's-square': x => [4 * x], 's-mod5': x => [x % 5], 's-speed': x => [3 * x],
  's-divcount': x => [[...Array(x).keys()].filter(d => x % (d + 1) === 0).length], 's-area20': x => [20 / x], 's-day': x => (x < 24 ? [24 - x] : [0]), 's-age': x => [x + 5],
  'n-divisor': x => [...Array(x).keys()].map(d => d + 1).filter(d => x % d === 0), 'n-multiple': x => [x, 2 * x, 3 * x], 'n-abs': x => [x, -x],
  'n-oddless': x => [...Array(x).keys()].filter(y => y % 2 === 1), 'n-perimeter': x => { const s = new Set(); for (let a = 1; 2 * a < x / 2; a++) s.add(a * (x / 2 - a)); return [...s]; },
  'n-coprime': x => [...Array(3 * x + 2).keys()].slice(1).filter(y => { let a = x, b = y; while (b) [a, b] = [b, a % b]; return a === 1; }),
};
const isFunctionRelation = id => { const f = RELATIONS[id]; if (!f) throw Error(`no relation ${id}`); return [...Array(30).keys()].map(i => i + 1).filter(x => id !== 'n-perimeter' || x >= 8).every(x => f(x).length === 1); };
const brute = (pred, from, to) => { const out = []; for (let x = from; x <= to; x++) if (pred(Q(x))) out.push(x); return out; };
const firstNat = (f, lim = 3000) => { for (let n = 1; n <= lim; n++) if (f(n)) return n; throw Error('none'); };
const lastNat = (f, from = 0, lim = 3000) => { need(!f(lim), 'bounded'); let best = null; for (let n = from; n < lim; n++) if (f(n)) best = n; return best; };
const addOracles = o => { for (const k of Object.keys(o)) if (k in ORACLE) throw Error(`duplicate oracle ${k}`); Object.assign(ORACLE, o); };
addOracles({
  'ineq-count-upto'(q, P) { need(q.prompt.includes(`${P.N} 이하의 자연수`) && q.prompt.includes(`부등식 ${P.ineq}의`), 'prompt'); return brute(ineqPred(P.ineq), 1, P.N).length; },
  'ineq-count-list'(q, P) { need(q.prompt.includes(`x의 값이 ${P.list.map(numTxt).join(', ')}일 때`) && q.prompt.includes(P.ineq), 'prompt'); const pr = ineqPred(P.ineq); return P.list.filter(x => pr(Q(x))).length; },
  'ineq-min-natural'(q, P) { need(q.prompt.includes(P.ineq) && q.prompt.includes('가장 작은 자연수'), 'prompt'); const pr = ineqPred(P.ineq); return firstNat(n => pr(Q(n))); },
  'ineq-max-integer'(q, P) { need(q.prompt.includes(P.ineq) && q.prompt.includes('가장 큰 정수'), 'prompt'); const pr = ineqPred(P.ineq); need(!pr(Q(3000)) && pr(Q(-3000)), 'bounded above'); for (let x = 3000; x >= -3000; x--) if (pr(Q(x))) return x; throw Error('none'); },
  'ineq-count-natural'(q, P) { need(q.prompt.includes(P.ineq) && q.prompt.includes('자연수 x의 개수'), 'prompt'); const pr = ineqPred(P.ineq); need(!pr(Q(3000)), 'bounded'); return brute(pr, 1, 3000).length; },
  'ineq-count-negative'(q, P) { need(q.prompt.includes(P.ineq) && q.prompt.includes('음의 정수 x의 개수'), 'prompt'); const pr = ineqPred(P.ineq); need(!pr(Q(-3000)), 'bounded'); return brute(pr, -3000, -1).length; },
  'ineq-solve-choice'(q, P) { need(q.prompt.includes(P.ineq), 'prompt'); const S = ineqSet(P.ineq); const ok = q.choices.filter(c => { const m = /^x([<>≤≥])(.+)$/.exec(c); return m[1] === S.rel && qeq(evalStr(m[2]), S.bound); }); need(ok.length === 1, `matching choices ${ok.length}`); return ok[0]; },
  'ineq-numberline-choice'(q, P) { need(q.prompt.includes(P.ineq), 'prompt'); const S = ineqSet(P.ineq); const ok = q.choices.filter(c => { const m = /^(.+)에 ([●○]), (오른쪽|왼쪽)$/.exec(c); return qeq(evalStr(m[1]), S.bound) && (m[2] === '●') === (S.rel === '≥' || S.rel === '≤') && (m[3] === '오른쪽') === (S.rel === '>' || S.rel === '≥'); }); need(ok.length === 1, `matching choices ${ok.length}`); return ok[0]; },
  'ineq-property-choice'(q, P) {
    need(q.prompt.startsWith(`${P.base}일 때, ${P.left} □ ${P.right}의`), 'prompt'); const seen = new Set(), L = parseExpr(P.left), Rn = parseExpr(P.right);
    for (let i = -12; i <= 12; i++) for (let k = -12; k <= 12; k++) { const a = Q(i, 2), b = Q(k, 3); const c0 = qcmp(a, b); if (P.base === 'a<b' ? c0 >= 0 : c0 <= 0) continue; const c = qcmp(evalNode(L, {a}), evalNode(Rn, {b})); seen.add(c > 0 ? '>' : c < 0 ? '<' : '='); }
    const truth = seen.size === 1 ? [...seen][0] : '알 수 없다'; need(q.choices.filter(c => c === truth).length === 1, 'truth among choices'); return truth;
  },
  'wp-budget'(q, P) { need([`${P.p}원`, `${P.b}원`, `${P.T}원 이하`].every(s => q.prompt.includes(s)), 'prompt'); return lastNat(n => P.b + P.p * n <= P.T); },
  'wp-elevator'(q, P) { need([`${P.W} kg`, `${P.m} kg`, `${P.w} kg`].every(s => q.prompt.includes(s)), 'prompt'); return lastNat(n => P.m + P.w * n <= P.W); },
  'wp-rectangle'(q, P) { need(q.prompt.includes(`${P.k} cm 긴`) && q.prompt.includes(`${P.P} cm 이하`), 'prompt'); return lastNat(x => 2 * (x + x + P.k) <= P.P, 1); },
  'wp-savings'(q, P) { need([`${P.a}원`, `${P.b}원`, `${P.p}원씩`, `${P.q}원씩`].every(s => q.prompt.includes(s)), 'prompt'); return firstNat(n => P.b + P.q * n > P.a + P.p * n); },
  'wp-average'(q, P) { need(P.s.every(v => q.prompt.includes(`${v}점`)) && q.prompt.includes(`평균이 ${P.M}점 이상`), 'prompt'); for (let x = 0; x <= 20; x++) if (qcmp(Q(P.s.reduce((a, b) => a + b, 0) + x, 4), Q(P.M)) >= 0) return x; throw Error('unreachable within 20'); },
  'wp-consecutive'(q, P) { need(q.prompt.includes(`합이 ${P.S}보다 작다`), 'prompt'); return lastNat(mid => mid >= 2 && (mid - 1) + mid + (mid + 1) < P.S, 2) + 1; },
  'wp-plans'(q, P) { need([`${P.A}원`, `${P.B}원`, `1분에 ${P.a}원`, `1분에 ${P.b}원`].every(s => q.prompt.includes(s)), 'prompt'); return firstNat(x => P.B + P.b * x < P.A + P.a * x); },
  'wp-group'(q, P) { need([`${P.price}원`, `${P.G}명 이상`, `${P.r}%`].every(s => q.prompt.includes(s)), 'prompt'); const n = firstNat(n => qcmp(Q(n * P.price), qmul(Q(P.G * P.price), Q(100 - P.r, 100))) > 0); need(n < P.G, 'fewer than group size'); return n; },
  // ---- U4: linear systems (substitution into the displayed equations; integer grid search + determinant) ----
  'system-solve'(q, P) { need(q.prompt.includes(`연립방정식 ${P.e1}, ${P.e2}`) && q.prompt.includes(`${P.ask}의 값`), 'prompt'); const [x, y] = solveSystem([P.e1, P.e2]); return P.ask === 'x' ? x : y; },
  'abc-solve'(q, P) { need(q.prompt.includes(`방정식 ${P.e}`), 'prompt'); const {parts, rels} = splitRel(P.e); need(rels.length === 2 && rels.every(r => r === '='), 'A=B=C'); const [x, y] = solveEqs([[parts[0], parts[2]], [parts[1], parts[2]]]); return P.ask === 'x' ? x : y; },
  'lin-natural-count'(q, P) { need(q.prompt.includes(`일차방정식 ${P.eq}의`), 'prompt'); const E = eqPred(P.eq); let n = 0; for (let x = 1; x <= 300; x++) for (let y = 1; y <= 300; y++) if (E({x: Q(x), y: Q(y)})) n++; return n; },
  'lin-one-solution'(q, P) { need(q.prompt.includes(`일차방정식 ${P.eq}의 한 해가 x=${numTxt(P.x0)}, y=k`), 'prompt'); const E = eqPred(P.eq); const ys = []; for (let y = -300; y <= 300; y++) if (E({x: Q(P.x0), y: Q(y)})) ys.push(y); need(ys.length === 1, 'unique k'); return ys[0]; },
  'system-pair-choice'(q, P) { need(q.prompt.includes(`연립방정식 ${P.e1}, ${P.e2}의 해`), 'prompt'); const A = eqPred(P.e1), B = eqPred(P.e2); const ok = q.choices.filter(c => { const m = /^\((−?\d+), (−?\d+)\)$/.exec(c); const env = {x: evalStr(m[1]), y: evalStr(m[2])}; return A(env) && B(env); }); need(ok.length === 1, `pairs satisfying both ${ok.length}`); return ok[0]; },
  'elim-choice'(q, P) {
    need(q.prompt.includes(`① ${P.e1}, ② ${P.e2}에서 ${P.target}`), 'prompt'); const c1 = coeffs(P.e1), c2 = coeffs(P.e2), idx = P.target === 'x' ? 0 : 1;
    const ok = q.choices.filter(c => { const m = /^①(?:×(\d+))?([+−])②(?:×(\d+))?$/.exec(c); const m1 = Q(m[1] ?? 1), m2 = Q(m[3] ?? 1), sg = m[2] === '+' ? 1n : -1n; const comb = i => qadd(qmul(m1, c1[i]), qmul(Q(sg * m2.n), c2[i])); return comb(idx).n === 0n && comb(1 - idx).n !== 0n; });
    need(ok.length === 1, `eliminating choices ${ok.length}`); return ok[0];
  },
  'coef-find'(q, P) { need(q.prompt.includes(`연립방정식 ${P.e1}, ${P.e2}의 해가 x=${P.x0}, y=${P.y0}일 때`), 'prompt'); const [a, b] = findAB([P.e1, P.e2], P.x0, P.y0); return a + b; },
  'coef-shared'(q, P) { need(q.prompt.includes(`연립방정식 ${P.e1}, ${P.f1}의 해와 연립방정식 ${P.e2}, ${P.f2}의 해`), 'prompt'); const [x0, y0] = solveSystem([P.e1, P.e2]); const [a, b] = findAB([P.f1, P.f2], x0, y0); return a + b; },
  'wp-heads-legs'(q, P) { need(q.prompt.includes(`모두 ${P.H}마리`) && q.prompt.includes(`합이 ${P.L}개`) && q.prompt.includes(`${P.ask}`), 'prompt'); const sols = []; for (let c = 0; c <= P.H; c++) { const r = P.H - c; if (2 * c + 4 * r === P.L) sols.push({닭: c, 토끼: r}); } need(sols.length === 1, 'unique'); return sols[0][P.ask]; },
  'wp-two-prices'(q, P) { need([`${P.p}원`, `${P.q}원`, `${P.n}개`, `${P.T}원`].every(s => q.prompt.includes(s)), 'prompt'); const ys = []; for (let y = 0; y <= P.n; y++) if (P.p * (P.n - y) + P.q * y === P.T) ys.push(y); need(ys.length === 1, 'unique'); return ys[0]; },
  'wp-digits'(q, P) { need(q.prompt.includes(`합은 ${P.s}이고`) && q.prompt.includes(`${P.d}만큼 크다`), 'prompt'); const ns = []; for (let n = 10; n <= 99; n++) { const t = Math.floor(n / 10), o = n % 10; if (t + o === P.s && 10 * o + t - n === P.d) ns.push(n); } need(ns.length === 1, 'unique'); return ns[0]; },
  'wp-ages'(q, P) { need(q.prompt.includes(`합은 ${P.S}살`) && q.prompt.includes(`${P.k}년 후`) && q.prompt.includes(`${P.m}배`), 'prompt'); const ss = []; for (let s = 0; s <= P.S; s++) { const Mo = P.S - s; if (Mo + P.k === P.m * (s + P.k)) ss.push(s); } need(ss.length === 1, 'unique'); return ss[0]; },
  'wp-walk-run'(q, P) {
    const tm = /모두 (?:(\d+)시간)? ?(?:(\d+)분)?이 걸렸다/.exec(q.prompt); need(tm, 'time text'); const minutes = Number(tm[1] ?? 0) * 60 + Number(tm[2] ?? 0);
    need(q.prompt.includes(`${P.L} km 떨어진`) && q.prompt.includes(`시속 ${P.v1} km로 걷다가`) && q.prompt.includes(`시속 ${P.v2} km로 뛰어서`), 'prompt');
    const xs = []; for (let x = 0; x <= P.L; x++) if (qeq(qadd(Q(60 * x, P.v1), Q(60 * (P.L - x), P.v2)), Q(minutes))) xs.push(x); need(xs.length === 1, 'unique'); return xs[0];
  },
  // ---- U5: linear functions (evaluation of the displayed formula) ----
  fval(q, P) { need(q.prompt.includes(`f(x)=${P.fx}`) && q.prompt.includes(`f(${numTxt(P.k)})의 값`), 'prompt'); const r = evalStr(P.fx, {x: Q(P.k)}); need(qint(r), 'int'); return Number(r.n); },
  'fval-sum'(q, P) { need(q.prompt.includes(`f(x)=${P.fx}`) && q.prompt.includes(`f(${numTxt(P.p)})+f(${numTxt(P.q)})`), 'prompt'); const r = qadd(evalStr(P.fx, {x: Q(P.p)}), evalStr(P.fx, {x: Q(P.q)})); return Number(r.n); },
  'fval-inverse'(q, P) { need(q.prompt.includes(`f(x)=${P.fx}`) && q.prompt.includes(`f(a)=${numTxt(P.m)}`), 'prompt'); const ks = []; for (let k = -500; k <= 500; k++) if (qeq(evalStr(P.fx, {x: Q(k)}), Q(P.m))) ks.push(k); need(ks.length === 1, 'unique'); return ks[0]; },
  'function-count'(q, P) { P.ids.forEach(id => need(q.prompt.includes(FUNC_TEXT[id]), `statement text ${id}`)); return P.ids.filter(isFunctionRelation).length; },
  'linear-count'(q, P) { P.exprs.forEach(e => need(q.prompt.includes(e), 'expr in prompt')); return P.exprs.filter(e => { const f = x => evalStr(e.replace(/^y=/, ''), {x: Q(x)}); const d1 = qsub(f(1), f(0)); return d1.n !== 0n && [1, 2, 3, 4].every(k => qeq(qsub(f(k + 1), f(k)), d1)); }).length; },
  intercept(q, P) {
    need(q.prompt.includes(`y=${P.fx}의 그래프의 ${P.ask === 'x' ? 'x절편' : P.ask === 'y' ? 'y절편' : '기울기'}`), 'prompt'); const f = x => evalStr(P.fx, {x}), b = f(Q(0)), m = qsub(f(Q(1)), b);
    const r = P.ask === 'y' ? b : P.ask === 'slope' ? m : qdiv(qneg(b), m); if (P.ask === 'x') need(f(r).n === 0n, 'root'); return qint(r) ? Number(r.n) : [Number(r.n), Number(r.d)];
  },
  'intercept-choice'(q, P) { need(q.prompt.includes(`y=${P.fx}의 그래프의 x절편`), 'prompt'); const f = x => evalStr(P.fx, {x}); const ok = q.choices.filter(c => f(evalStr(c)).n === 0n); need(ok.length === 1, `roots among choices ${ok.length}`); return ok[0]; },
  increase(q, P) { need(q.prompt.includes(`y=${P.fx}에서 x의 값이`), 'prompt'); if (P.from !== undefined) need(q.prompt.includes(`${numTxt(P.from)}에서 ${P.to}까지`), 'range'); else need(q.prompt.includes(`${P.dx}만큼 증가`), 'dx'); const f = x => evalStr(P.fx, {x: Q(x)}); const vals = [-3, 0, 5].map(x0 => qsub(f(x0 + P.dx), f(x0))); need(vals.every(v => qeq(v, vals[0])), 'constant increase'); if (P.from !== undefined) need(qeq(qsub(f(P.to), f(P.from)), vals[0]), 'range increase'); return Number(vals[0].n); },
  'slope-change'(q, P) { need(q.prompt.includes(`${numTxt(P.p)}에서 ${numTxt(P.q)}까지 증가할 때, y의 값은 ${numTxt(P.r)}에서 ${numTxt(P.s)}까지`), 'prompt'); const m = Q(P.s - P.r, P.q - P.p); return qint(m) ? Number(m.n) : [Number(m.n), Number(m.d)]; },
  'slope-change-choice'(q, P) { need(q.prompt.includes(`${numTxt(P.p)}에서 ${numTxt(P.q)}까지 증가할 때, y의 값은 ${numTxt(P.r)}에서 ${numTxt(P.s)}까지 감소`), 'prompt'); const m = Q(P.s - P.r, P.q - P.p); const ok = q.choices.filter(c => qeq(evalStr(c), m)); need(ok.length === 1, 'slope among choices'); return ok[0]; },
  'translate-point'(q, P) { const m = /y=([^의]+)의 그래프를 y축의 방향으로 (−?\d+)만큼 평행이동한 그래프가 점 \((−?\d+), k\)/.exec(q.prompt); need(m, 'prompt'); const f = x => qadd(evalStr(m[1], {x}), evalStr(m[2])); need(qeq(evalStr(m[1], {x: Q(1)}), Q(P.slope)) && qeq(evalStr(m[2]), Q(P.dy)), 'params'); const r = f(evalStr(m[3])); need(qint(r), 'int'); return Number(r.n); },
  'translate-match'(q, P) { need(q.prompt.includes(`y=${P.f1}의 그래프를`) && q.prompt.includes(`y=${P.f2}의 그래프와 겹쳐진다`), 'prompt'); const ms = []; for (let m = -100; m <= 100; m++) if (identical(`${P.f1}+${m < 0 ? `(${m})` : m}`.replace(/-/g, '−'), P.f2)) ms.push(m); need(ms.length === 1, 'unique shift'); return ms[0]; },
  'translate-choice'(q, P) { const m = /y=([^의]+)의 그래프를 y축의 방향으로 (−?\d+)만큼/.exec(q.prompt); need(m && qeq(evalStr(m[1], {x: Q(1)}), Q(P.slope)), 'prompt'); const target = `(${m[1]})+(${m[2]})`; const ok = q.choices.filter(c => identical(c.replace(/^y=/, ''), target)); need(ok.length === 1, 'formula among choices'); return ok[0]; },
  'eq-slope-xint'(q, P) { need(q.prompt.includes(`기울기가 ${numTxt(P.slope)}이고 x절편이 ${numTxt(P.x0)}인`), 'prompt'); const r = qmul(Q(P.slope), qsub(Q(0), Q(P.x0))); return Number(r.n); },
  'eq-two-values'(q, P) { need(q.prompt.includes(`f(${numTxt(P.p)})=${numTxt(P.fp)}, f(${numTxt(P.q)})=${numTxt(P.fq)}일 때, f(${numTxt(P.k)})`), 'prompt'); const r = qadd(qmul(Q(P.fp), Q(P.k - P.q, P.p - P.q)), qmul(Q(P.fq), Q(P.k - P.p, P.q - P.p))); need(qint(r), 'int'); return Number(r.n); },
  'eq-intercepts'(q, P) { need(q.prompt.includes(`x절편이 ${numTxt(P.xi)}, y절편이 ${numTxt(P.yi)}인`) && q.prompt.includes(`점 (${numTxt(P.px)}, k)`), 'prompt'); const r = qmul(Q(P.yi), qsub(Q(1), Q(P.px, P.xi))); need(qint(r), 'int'); return Number(r.n); },
  'parallel-param'(q, P) { need(q.prompt.includes(`${P.left}, ${P.right}의 그래프가 서로 평행`), 'prompt'); const L = P.left.replace(/^y=/, ''), Rr = P.right.replace(/^y=/, ''); const as = []; for (let a = -60; a <= 60; a++) { const f = x => evalStr(L, {a: Q(a), x: Q(x)}), g = x => evalStr(Rr, {x: Q(x)}); if (qeq(qsub(f(1), f(0)), qsub(g(1), g(0))) && !qeq(f(0), g(0))) as.push(a); } need(as.length === 1, 'unique a'); return as[0]; },
  'coincide-params'(q, P) { need(q.prompt.includes(`${P.f1}, ${P.f2}의 그래프가 일치`), 'prompt'); const hits = []; for (let a = -40; a <= 40; a++) for (let b = -40; b <= 40; b++) if (identical(P.f1.replace(/^y=/, ''), P.f2.replace(/^y=/, ''), {a: Q(a), b: Q(b)})) hits.push(a - b); need(hits.length === 1 && P.combo === 'a−b', 'unique'); return hits[0]; },
  'triangle-area'(q, P) { need(q.prompt.includes(`y=${P.fx}의 그래프와 x축, y축으로 둘러싸인`), 'prompt'); const f = x => evalStr(P.fx, {x}), b = f(Q(0)), root = qdiv(qneg(b), qsub(f(Q(1)), b)); const A = qmul(Q(1, 2), qmul(Q(root.n < 0n ? -root.n : root.n, root.d), Q(b.n < 0n ? -b.n : b.n, b.d))); need(qint(A), 'int'); return Number(A.n); },
  'wp-candle-length'(q, P) { need(q.prompt.includes(`길이가 ${P.L} cm인 양초`) && q.prompt.includes(`${P.min}분 후`), 'prompt'); const rate = evalStr(/1분에 ([\d.]+) cm씩/.exec(q.prompt)[1]); need(qeq(rate, Q(P.rn, P.rd)), 'rate'); let len = Q(P.L); for (let k = 0; k < P.min; k++) len = qsub(len, rate); need(qint(len), 'int'); return Number(len.n); },
  'wp-candle-time'(q, P) { need(q.prompt.includes(`길이가 ${P.L} cm인 양초`) && q.prompt.includes(`${P.k} cm가 되는`), 'prompt'); const rate = evalStr(/1분에 ([\d.]+) cm씩/.exec(q.prompt)[1]); let len = Q(P.L); for (let m = 0; m <= 500; m++) { if (qeq(len, Q(P.k))) return m; len = qsub(len, rate); } throw Error('never'); },
  'wp-tank-left'(q, P) { need(q.prompt.includes(`물이 ${P.V} L`) && q.prompt.includes(`1분에 ${P.r} L씩`) && q.prompt.includes(`${P.min}분 후`), 'prompt'); let v = P.V; for (let k = 0; k < P.min; k++) v -= P.r; return v; },
  'wp-tank-empty'(q, P) { need(q.prompt.includes(`물이 ${P.V} L`) && q.prompt.includes(`1분에 ${P.r} L씩`), 'prompt'); let v = P.V, m = 0; while (v > 0) { v -= P.r; m++; } need(v === 0, 'exact'); return m; },
  'wp-temperature'(q, P) { need(q.prompt.includes(`기온이 ${P.T0} °C`) && q.prompt.includes(`높이가 ${P.hkm} km`), 'prompt'); const step = evalStr(/기온이 ([\d.]+) °C씩/.exec(q.prompt)[1]); need(qeq(step, Q(P.dn, P.dd)), 'rate'); let t = Q(P.T0); for (let h = 0; h < P.hkm * 1000; h += 100) t = qsub(t, step); need(qint(t), 'int'); return Number(t.n); },
  'wp-spring'(q, P) { need(q.prompt.includes(`길이가 ${P.L0} cm인 용수철`) && q.prompt.includes(`무게가 ${P.st} g인 물체를 하나 더`) && q.prompt.includes(`${P.inc} cm씩`) && q.prompt.includes(`무게가 ${P.w} g인 물체를 매달았을`), 'prompt'); let len = P.L0; for (let g = 0; g < P.w; g += P.st) len += P.inc; return len; },
  'quadrant-choice'(q, P) {
    need(q.prompt.includes(`y=${P.fx}의 그래프가 지나지 않는 사분면`), 'prompt'); const seen = new Set();
    for (let k = -1600; k <= 1600; k++) { const x = Q(k, 8), y = evalStr(P.fx, {x}); const sx = qcmp(x, Q(0)), sy = qcmp(y, Q(0)); if (sx && sy) seen.add(sx > 0 ? (sy > 0 ? 1 : 4) : (sy > 0 ? 2 : 3)); }
    const miss = [1, 2, 3, 4].filter(k => !seen.has(k)); need(miss.length === 1, 'exactly one quadrant missed'); const name = `제${miss[0]}사분면`; need(q.choices.includes(name), 'choice'); return name;
  },
  // ---- U6: equations of lines ----
  'eq-intercept'(q, P) { need(q.prompt.includes(`일차방정식 ${P.eq}의 그래프의`), 'prompt'); const [a, b, c] = coeffs(P.eq); const r = P.ask === 'y' ? qdiv(c, b) : P.ask === 'x' ? qdiv(c, a) : qdiv(qneg(a), b); return qint(r) ? Number(r.n) : [Number(r.n), Number(r.d)]; },
  'eq-point'(q, P) { need(q.prompt.includes(`일차방정식 ${P.eq}의 그래프가 점 (k, ${numTxt(P.y0)})`), 'prompt'); const E = eqPred(P.eq); const ks = []; for (let k = -300; k <= 300; k++) if (E({x: Q(k), y: Q(P.y0)})) ks.push(k); need(ks.length === 1, 'unique'); return ks[0]; },
  'rect-area4'(q, P) { need(q.prompt.includes(`네 직선 x=${numTxt(P.xs[0])}, x=${numTxt(P.xs[1])}, y=${numTxt(P.ys[0])}, y=${numTxt(P.ys[1])}로`), 'prompt'); let n = 0; for (let x = P.xs[0]; x < P.xs[1]; x++) for (let y = P.ys[0]; y < P.ys[1]; y++) n++; return n; },
  'rect-area-axes'(q, P) { need(q.prompt.includes(`두 직선 x=${numTxt(P.px)}, y=${numTxt(P.py)}`), 'prompt'); let n = 0; for (let x = Math.min(0, P.px); x < Math.max(0, P.px); x++) for (let y = Math.min(0, P.py); y < Math.max(0, P.py); y++) n++; return n; },
  'rect-area-eqs'(q, P) { need(q.prompt.includes(`두 일차방정식 ${P.e1}, ${P.e2}의 그래프`), 'prompt'); const [a1, b1, c1] = coeffs(P.e1), [a2, b2, c2] = coeffs(P.e2); need(b1.n === 0n && a2.n === 0n, 'vertical and horizontal'); const X = qdiv(c1, a1), Y = qdiv(c2, b2); const A = qmul(X, Y); return Math.abs(Number(A.n) / Number(A.d)); },
  'axis-parallel-choice'(q, P) { need(q.prompt.includes(`x좌표가 ${numTxt(P.px)}이고 y좌표가 ${numTxt(P.py)}인 점을 지나고 ${P.axis}축에 평행한`), 'prompt'); const ok = q.choices.filter(c => { const m = /^([xy])=(.+)$/.exec(c); const vertical = m[1] === 'x'; const val = evalStr(m[2]); return (P.axis === 'y') === vertical && qeq(val, Q(vertical ? P.px : P.py)); }); need(ok.length === 1, 'one line'); return ok[0]; },
  'line-desc-choice'(q, P) {
    need(q.prompt.includes(`방정식 ${P.eqn}의 그래프`), 'prompt'); const [a, b, c] = coeffs(P.eqn); const vertical = b.n === 0n, horizontal = a.n === 0n;
    const truth = s => { let m; if ((m = /^([xy])축에 평행하다$/.exec(s))) return m[1] === 'x' ? horizontal : vertical; if ((m = /^점 \((−?\d+), (−?\d+)\)[을를] 지난다$/.exec(s))) return qeq(qadd(qmul(a, evalStr(m[1])), qmul(b, evalStr(m[2]))), c); if ((m = /^기울기가 (−?\d+)이다$/.exec(s))) return !vertical && qeq(qdiv(qneg(a), b), evalStr(m[1])); throw Error(`unknown statement ${s}`); };
    const ok = q.choices.filter(truth); need(ok.length === 1, 'one true statement'); return ok[0];
  },
  intersection(q, P) { need(q.prompt.includes(`${P.e1}, ${P.e2}의 그래프의 교점의 ${P.ask}좌표`), 'prompt'); const [x, y] = solveSystem([P.e1, P.e2]); return P.ask === 'x' ? x : y; },
  'intersection-coef'(q, P) { need(q.prompt.includes(`${P.e1}, ${P.e2}의 그래프의 교점의 좌표가 (${P.x0}, ${P.y0})`), 'prompt'); const [a, b] = findAB([P.e1, P.e2], P.x0, P.y0); return a + b; },
  'three-lines'(q, P) { need(q.prompt.includes(`세 직선 ${P.e1}, ${P.e2}, ${P.e3}`), 'prompt'); const [x0, y0] = solveSystem([P.e1, P.e2]); const {parts} = splitRel(P.e3); const as = []; for (let a = -60; a <= 60; a++) { const env = {a: Q(a), x: Q(x0), y: Q(y0)}; if (qeq(evalNode(parts[0], env), evalNode(parts[1], env))) as.push(a); } need(as.length === 1, 'unique a'); return as[0]; },
  'infinite-solutions'(q, P) { need(q.prompt.includes(`연립방정식 ${P.e1}, ${P.e2}의 해가 무수히 많을 때`), 'prompt'); const hits = []; for (let a = -60; a <= 60; a++) for (let b = -60; b <= 60; b++) { const E1 = coeffsWith(P.e1, {a: Q(a)}), E2 = coeffsWith(P.e2, {b: Q(b)}); if (proportional(E1, E2)) hits.push(a + b); } need(hits.length === 1, `pairs ${hits.length}`); return hits[0]; },
  'no-solution'(q, P) { need(q.prompt.includes(`연립방정식 ${P.e1}, ${P.e2}의 해가 없을 때`), 'prompt'); const as = []; for (let a = -60; a <= 60; a++) { const E1 = coeffsWith(P.e1, {a: Q(a)}), E2 = coeffsWith(P.e2, {}); const d = qsub(qmul(E1[0], E2[1]), qmul(E1[1], E2[0])); if (d.n === 0n && !proportional(E1, E2)) as.push(a); } need(as.length === 1, 'unique a'); return as[0]; },
  'solution-count-choice'(q, P) {
    need(q.prompt.includes(`연립방정식 ${P.e1}, ${P.e2}의 두 그래프`), 'prompt'); const E1 = coeffs(P.e1), E2 = coeffs(P.e2); const d = qsub(qmul(E1[0], E2[1]), qmul(E1[1], E2[0]));
    let sols = 0; for (let x = -60; x <= 60; x++) for (let y = -60; y <= 60; y++) if (eqPred(P.e1)({x: Q(x), y: Q(y)}) && eqPred(P.e2)({x: Q(x), y: Q(y)})) sols++;
    const kind = d.n !== 0n ? 'one' : proportional(E1, E2) ? 'inf' : 'none'; if (kind === 'none') need(sols === 0, 'no common point'); if (kind === 'inf') need(sols > 1, 'many common points');
    const truth = {one: '한 점, 해 한 쌍', none: '평행, 해 없음', inf: '일치, 해 무수히 많음'}[kind]; need(q.choices.filter(c => c === truth).length === 1, 'truth listed'); return truth;
  },
  'two-line-area'(q, P) {
    need(q.prompt.includes(`두 직선 ${P.f1}, ${P.f2}`) && q.prompt.includes(`${P.axis}축으로 둘러싸인 삼각형`), 'prompt'); const [x0, y0] = solveRational([P.f1, P.f2]);
    const g = f => { const [a, b, c] = coeffs(f); return P.axis === 'x' ? qdiv(c, a) : qdiv(c, b); }; const base = qsub(g(P.f1), g(P.f2)); const h = P.axis === 'x' ? y0 : x0;
    const A = qmul(Q(1, 2), qmul(base, h)); need(qint(A), 'int'); return Math.abs(Number(A.n));
  },
  'linear-graph-count'(q, P) { P.eqs.forEach(e => need(q.prompt.includes(e), 'eq in prompt')); return P.eqs.filter(e => { const [a, b] = coeffs(e); return a.n !== 0n && b.n !== 0n; }).length; },
  'wp-hiking'(q, P) { need(q.prompt.includes(`시속 ${P.u} km`) && q.prompt.includes(`시속 ${P.w} km`) && q.prompt.includes(P.Td === 1 ? `${P.Tn}시간 이내` : `${(P.Tn - 1) / 2}시간 30분 이내`), 'prompt'); const bound = qdiv(Q(P.Tn, P.Td), qadd(Q(1, P.u), Q(1, P.w))); need(qint(bound), 'integer distance'); const d = Number(bound.n); need(qcmp(qadd(Q(d, P.u), Q(d, P.w)), Q(P.Tn, P.Td)) <= 0 && qcmp(qadd(Q(d + 1, P.u), Q(d + 1, P.w)), Q(P.Tn, P.Td)) > 0, 'max'); return d; },
});

// ---------------- choice values, copy guard, bots ----------------
const NUMERIC = /^−?(?:\d+(?:\.\d+)?|\{frac:\d+\/\d+\})$/;
const valueOf = c => NUMERIC.test(c) ? evalStr(c) : null; // this file's parser: "−{frac:3/4}" → −3/4, "23.8" → 119/5
const canon = v => { if (qint(v)) return numTxt(Number(v.n)); const n = v.n < 0n ? -v.n : v.n; return `${v.n < 0n ? '−' : ''}{frac:${n}/${v.d}}`; };
const absKey = v => `${v.n < 0n ? -v.n : v.n}/${v.d}`;
function printedNumbers(prompt) { // |values| of integers, decimals and {frac} tokens visible in the prompt
  const out = new Set(); const t = prompt.replace(/\{frac:(\d+)\/(\d+)\}/g, (_, n, d) => { out.add(absKey(Q(n, d))); return ' '; }).replace(/<\/?sup>/g, ' ');
  for (const m of t.matchAll(/\d+(?:\.\d+)?/g)) out.add(absKey(evalStr(m[0]))); return out;
}
const BOTS = {
  'slot-1': q => q.choices[0], 'slot-2': q => q.choices[1], 'slot-3': q => q.choices[2], 'slot-4': q => q.choices[3],
  copy: (q, V, P) => q.choices.find((c, i) => V[i] && P.has(absKey(V[i]))) ?? q.choices[0],
  'anti-copy': (q, V, P) => q.choices.find((c, i) => V[i] && !P.has(absKey(V[i]))) ?? q.choices[0],
  ...Object.fromEntries([0, 1, 2, 3].map(r => [`rank-${r + 1}`, (q, V) => { const idx = [0, 1, 2, 3].sort((a, b) => qcmp(V[a], V[b])); return q.choices[idx[r]]; }])),
  'odd-sign': (q, V) => { const neg = V.map(v => v.n < 0n); const k = neg.filter(Boolean).length; return k === 1 ? q.choices[neg.indexOf(true)] : k === 3 ? q.choices[neg.indexOf(false)] : q.choices[0]; },
  'odd-kind': (q, V) => { const fr = V.map(v => !qint(v)); const k = fr.filter(Boolean).length; return k === 1 ? q.choices[fr.indexOf(true)] : k === 3 ? q.choices[fr.indexOf(false)] : q.choices[0]; },
};
const BOT_LIMIT = 0.35; // no heuristic may reach 35% on the numeric items of a pack (chance = 25%)

// ---------------- per-pack check ----------------
function checkPack(pack, id) {
  const failures = [], types = {}, diff = {}, fmt = {}, slots = [0, 0, 0, 0], bot = Object.fromEntries(Object.keys(BOTS).map(k => [k, 0])); let numericItems = 0;
  const structure = validatePackV3(pack); for (const e of structure.errors) failures.push(`structure: ${e}`);
  const unit = curriculum.units.find(u => u.id === pack.unit_id);
  if (!unit || pack.grade !== 2 || pack.semester !== 1 || pack.unit_id !== id) failures.push('unit/grade/semester');
  for (const c of pack.standards) { if (!curriculum.standards.some(s => s.code === c)) failures.push(`standard ${c} missing`); if (!unit?.standards.includes(c)) failures.push(`standard ${c} not in unit`); }
  pack.items.forEach(q => {
    const P = q.params ?? {}, fn = ORACLE[P.t]; types[P.t] = (types[P.t] ?? 0) + 1; diff[q.difficulty] = (diff[q.difficulty] ?? 0) + 1; fmt[q.format] = (fmt[q.format] ?? 0) + 1;
    try {
      need(q.answer_mode === 'choice' && Array.isArray(q.choices) && q.choices.length === 4, 'v3 four choices');
      const slot = q.choices.indexOf(q.answer); need(slot >= 0 && q.choices.lastIndexOf(q.answer) === slot, 'answer listed exactly once'); slots[slot]++;
      if (!fn) throw Error(`no oracle for ${P.t}`);
      const exp = fn(q, P);
      if (typeof exp === 'string') { // statement / expression choices: the oracle itself proved exactly one choice true
        need(q.answer === exp, `choice answer ${q.answer} vs oracle ${exp}`); need(q.format === 'text' || valueOf(q.answer), 'format');
        need(new Set(q.choices.map(c => c.replace(/\s+/g, ''))).size === 4, '4 distinct choices');
      } else {
        const want = Array.isArray(exp) ? Q(exp[0], exp[1]) : Q(exp); const V = q.choices.map(valueOf);
        need(V.every(Boolean), `non-numeric choice in a numeric item: ${q.choices.join(' | ')}`);
        const right = V.map((v, i) => qeq(v, want) ? i : -1).filter(i => i >= 0);
        need(right.length === 1, `${right.length} choices equal the oracle value ${canon(want)}`); need(q.choices[right[0]] === q.answer, `answer ${q.answer} but oracle value is choice ${q.choices[right[0]]}`);
        need(q.answer === canon(want), `answer not in canonical reduced form (${q.answer} vs ${canon(want)})`);
        need(new Set(V.map(v => `${v.n}/${v.d}`)).size === 4, `equal-valued choices ${q.choices.join(' | ')}`);
        need(q.format === (qint(want) ? 'int' : 'frac'), 'format'); need(Math.abs(q.answerNumeric - qnum(want)) < 1e-9, 'answerNumeric');
        const PN = printedNumbers(q.prompt), wrongIdx = [0, 1, 2, 3].filter(i => i !== right[0]);
        need(!(PN.has(absKey(want)) && wrongIdx.every(i => !PN.has(absKey(V[i])))), 'copy-vulnerable: only the answer is printed in the prompt');
        numericItems++; for (const [k, f] of Object.entries(BOTS)) if (f(q, V, PN) === q.answer) bot[k]++;
      }
      need(Array.isArray(q.distractor_tags) && q.distractor_tags.length === 3 && q.distractor_tags.every(t => t.startsWith(`${id}.`)) && new Set(q.distractor_tags).size >= 2, 'distractor_tags');
      const text = [q.prompt, q.explain, ...q.choices].join(' ');
      need(!/약분하지|코인|닢|붓/.test(q.prompt + q.explain), 'pour-era wording');
      need(!text.includes('√') && !/[⁰⁴⁵⁶⁷⁸⁹̇]/.test(text), 'glyph/root policy');
      need(!/-\d|[0-9a-z)]-[0-9a-z(]/.test(text), 'ASCII hyphen used as a minus sign'); need(!/기울기을|절편를|[+−]{2}|=\+/.test(text), 'josa/sign typo');
      need(!/\d\s*\/\s*\d/.test(text.replace(/\{frac:\d+\/\d+\}/g, '')), 'plain fraction');
      need(!/{frac:\d+\/1}/.test(text), 'integer written as fraction token');
      const noSup = text.replace(/<sup>\d+<\/sup>|<sup>[□x]<\/sup>/g, ''); need(!/<\/?(?:[A-Za-z-]+[\s=>]|#)/.test(noSup) && !/<\/?sup>/.test(noSup), 'TMP markup hazard or stray sup tag');
    } catch (e) { failures.push(`${q.id} [${P.t}] ${e.message} :: ${q.prompt}`); }
  });
  const N = pack.items.length; slots.forEach((c, i) => { if (Math.abs(c - N / 4) > Math.max(2, 0.02 * N)) failures.push(`answer slot ${i + 1}: ${c}/${N}`); });
  const botShare = Object.fromEntries(Object.entries(bot).map(([k, v]) => [k, +(v / Math.max(1, numericItems)).toFixed(3)]));
  for (const [k, v] of Object.entries(botShare)) if (v > BOT_LIMIT) failures.push(`bot ${k} scores ${(100 * v).toFixed(1)}% on numeric items (limit ${100 * BOT_LIMIT}%)`);
  return {failures, stats: {items: N, difficulty: diff, formats: fmt, answer_slots: slots, types, numeric_items: numericItems, bots: botShare}};
}

// ---------------- self-test: planted errors must all be caught ----------------
function selftest(pack, id) {
  const clone = () => JSON.parse(JSON.stringify(pack)), results = [];
  const firstOfType = p => { const seen = new Set(), out = []; p.items.forEach((q, i) => { if (i && !seen.has(q.params.t)) { seen.add(q.params.t); out.push(i); } }); return out; };
  const caught = (p, ids) => { const {failures} = checkPack(p, id); return ids.filter(x => failures.some(f => f.startsWith(`${x} `))).length; };
  const plant = (name, mutate) => { const p = clone(), ids = []; for (const i of firstOfType(p)) { const q = p.items[i]; if (mutate(q) !== false) ids.push(q.id); } results.push({mutation: name, planted: ids.length, caught: caught(p, ids)}); };
  plant('answer-points-at-a-distractor', q => { q.answer = q.choices.find(c => c !== q.answer); });
  plant('answer-and-its-choice-changed-to-a-wrong-value', q => { const v = valueOf(q.answer); if (!v) return false; const w = canon(qadd(v, Q(1))); if (q.choices.includes(w)) return false; q.choices[q.choices.indexOf(q.answer)] = w; q.answer = w; });
  plant('distractor-equivalent-to-answer', q => { const v = valueOf(q.answer), k = q.choices.findIndex(c => c !== q.answer); if (v) { const n = v.n < 0n ? -v.n : v.n; q.choices[k] = `${v.n < 0n ? '−' : ''}{frac:${2n * n}/${2n * v.d}}`; } else q.choices[k] = q.answer + ' '; });
  plant('distractor-is-a-second-correct-value', q => { const v = valueOf(q.answer); if (!v || !qint(v)) return false; const k = q.choices.findIndex(c => c !== q.answer); q.choices[k] = `${numTxt(Number(v.n))}.0`; });
  { const p = clone(); for (const q of p.items) { const i = q.choices.indexOf(q.answer); [q.choices[0], q.choices[i]] = [q.choices[i], q.choices[0]]; } const {failures} = checkPack(p, id); results.push({mutation: 'all-answers-in-slot-1', planted: 1, caught: failures.some(f => f.startsWith('answer slot')) ? 1 : 0}); }
  return results;
}

// ---------------- driver ----------------
const args = process.argv.slice(2), SELFTEST = args.includes('--selftest');
const packs = args.filter(a => !a.startsWith('--')).length ? args.filter(a => !a.startsWith('--')) : ['m2s1-u1', 'm2s1-u2', 'm2s1-u3', 'm2s1-u4', 'm2s1-u5', 'm2s1-u6'];
const report = {generated_at: new Date().toISOString(), schema_version: 3, method: 'independent re-derivation from params + displayed text; imports no generator code; choices compared by exact rational value', bot_limit: BOT_LIMIT, packs: []};
let totalFail = 0;
for (const id of packs) {
  const file = path.join(packDir, `${id}.json`); if (!fs.existsSync(file)) { console.log(`${id}: missing`); totalFail++; continue; }
  const pack = JSON.parse(fs.readFileSync(file, 'utf8')), {failures, stats} = checkPack(pack, id);
  const r = {pack_id: id, ...stats, failures: failures.length, failure_samples: failures.slice(0, 20), verdict: failures.length ? 'fail' : 'pass'};
  if (SELFTEST) { r.selftest = selftest(pack, id); const miss = r.selftest.filter(t => t.caught !== t.planted); if (miss.length) { r.verdict = 'fail'; failures.push(...miss.map(t => `selftest ${t.mutation}: caught ${t.caught}/${t.planted}`)); } }
  report.packs.push(r); totalFail += failures.length;
  const worstBot = Object.entries(stats.bots).sort((a, b) => b[1] - a[1])[0];
  console.log(`${id}: ${r.verdict} (${failures.length} failures) — ${stats.items} items, difficulty ${JSON.stringify(stats.difficulty)}, slots ${stats.answer_slots.join('/')}, best bot ${worstBot[0]} ${(100 * worstBot[1]).toFixed(1)}%${SELFTEST ? `, selftest ${r.selftest.map(t => `${t.caught}/${t.planted}`).join(' ')}` : ''}${failures.length ? `\n  ${failures.slice(0, 12).join('\n  ')}` : ''}`);
}
report.verdict = totalFail ? 'fail' : 'pass';
if (packs.length === 6) fs.writeFileSync(path.join(here, 'm2s1-check-report.json'), JSON.stringify(report, null, 2) + '\n');
console.log(`verdict: ${report.verdict}`);
process.exitCode = totalFail ? 1 : 0;
