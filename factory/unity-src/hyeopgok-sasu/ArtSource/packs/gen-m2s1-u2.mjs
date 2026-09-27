#!/usr/bin/env node
// 중2-1 단원2 식의 계산 [9수02-08]~[9수02-10].
// Coin input cannot express a symbolic answer, so items use the textbook "□ 안에 알맞은 수" form:
// the student computes the whole result, and pours one coefficient/exponent of it. Every □ answer is a
// natural number 2~59; negative results appear only on the visible side of the identity or in choice items.
// Exponents stay natural numbers (교육과정: 0·음의 지수 금지); (다항식)÷(단항식) quotients are polynomials.
import {amount, choice, writeUnitPack, j, gcd, sup, num, rat, M} from './m2s1-lib.mjs';
const U = 'm2s1-u2', T = s => `${U}.${s}`;
const BOX = '□';

// ---- tiny symbolic polynomial algebra (generator side only) ----
const key = e => Object.keys(e).sort().filter(v => e[v]).map(v => v + e[v]).join('');
function P(...terms) { const m = new Map(); for (const [c, e] of terms) { const k = key(e); const cur = m.get(k); m.set(k, {c: (cur?.c ?? 0) + c, e: {...(cur?.e ?? e)}}); } return [...m.values()].filter(t => t.c !== 0); }
const add = (A, B, s = 1) => P(...A.map(t => [t.c, t.e]), ...B.map(t => [s * t.c, t.e]));
const scale = (A, k) => A.map(t => ({c: t.c * k, e: t.e}));
const mulMono = (A, c, e) => A.map(t => { const ne = {...t.e}; for (const [v, p] of Object.entries(e)) ne[v] = (ne[v] ?? 0) + p; return {c: t.c * c, e: ne}; });
const deg = e => Object.values(e).reduce((a, b) => a + b, 0);
function order(A, vars) { return [...A].sort((s, t) => deg(t.e) - deg(s.e) || vars.map(v => (t.e[v] ?? 0) - (s.e[v] ?? 0)).find(x => x) || 0); }
function monoText(c, e, vars, {box = false, boxExp = null} = {}) {
  const vs = vars.filter(v => e[v]).map(v => v + (boxExp === v ? `<sup>${BOX}</sup>` : sup(e[v]))).join('');
  const coef = box ? BOX : !vs ? num(c) : c === 1 ? '' : c === -1 ? M : num(c);
  return coef + vs;
}
/** Format polynomial; opts.boxTerm = index whose coefficient becomes □ (must be positive); opts.boxExp = [index, var]. */
function polyText(A, vars, opts = {}) {
  let s = '';
  A.forEach((t, i) => {
    const box = opts.boxTerm === i, bx = opts.boxExp && opts.boxExp[0] === i ? opts.boxExp[1] : null;
    const m = monoText(box ? 1 : t.c, t.e, vars, {box, boxExp: bx});
    if (!s) s = m; else s += (box || t.c > 0) ? '+' + m : m;
  });
  return s || '0';
}
const E = (x = 0, y = 0, vars = ['x', 'y']) => ({[vars[0]]: x, [vars[1]]: y});
const boxItem = ({lhs, rhs, ans, explain, concept, difficulty, traps, t = 'identity-box'}) => amount({prompt: `${lhs}=${rhs}일 때, □ 안에 알맞은 수를 구하시오.`, answer: ans, explain, concept, difficulty, params: {t, lhs, rhs}, traps});
const LETTERS = ['a', 'x', 'y', 'b'], PAIRS = [['a', 'b'], ['x', 'y']];

// A. 거듭제곱의 곱
const expProd = [];
for (const v of LETTERS) for (let m = 1; m <= 9; m++) for (let n = 2; n <= 9; n++) {
  expProd.push(boxItem({lhs: `${v}${sup(m)}×${v}${sup(n)}`, rhs: `${v}<sup>${BOX}</sup>`, ans: m + n, explain: `밑이 같은 거듭제곱의 곱은 지수끼리 더하므로 ${m}+${n}=${m + n}입니다.`, concept: '지수법칙', difficulty: 1, traps: [{v: m * n, tag: T('exponents-multiplied')}]}));
}
// B. 거듭제곱의 거듭제곱
const powPlain = [], powExtra = [];
for (const v of LETTERS) for (let m = 2; m <= 9; m++) for (let n = 2; n <= 7; n++) {
  if (m * n <= 59) powPlain.push(boxItem({lhs: `(${v}${sup(m)})${sup(n)}`, rhs: `${v}<sup>${BOX}</sup>`, ans: m * n, explain: `거듭제곱의 거듭제곱은 지수끼리 곱하므로 ${m}×${n}=${m * n}입니다.`, concept: '지수법칙', difficulty: 1, traps: [{v: m + n, tag: T('power-exponents-added')}]}));
  for (let k = 1; k <= 7; k++) if (m * n + k <= 59) powExtra.push(boxItem({lhs: `(${v}${sup(m)})${sup(n)}×${v}${sup(k)}`, rhs: `${v}<sup>${BOX}</sup>`, ans: m * n + k, explain: `(${v}${sup(m)})${sup(n)}=${v}${sup(m * n)}이므로 ${m * n}+${k}=${m * n + k}입니다.`, concept: '지수법칙', difficulty: 2, traps: [{v: m + n + k, tag: T('power-exponents-added')}, {v: m * n * k, tag: T('exponents-multiplied')}]}));
}
// C. 거듭제곱의 나눗셈
const divPlain = [], divMixed = [];
for (const v of LETTERS) for (let m = 4; m <= 20; m++) for (let n = 1; n <= m - 2; n++) {
  divPlain.push(boxItem({lhs: `${v}${sup(m)}÷${v}${sup(n)}`, rhs: `${v}<sup>${BOX}</sup>`, ans: m - n, explain: `밑이 같은 거듭제곱의 나눗셈은 지수끼리 빼므로 ${m}−${n}=${m - n}입니다.`, concept: '지수법칙', difficulty: 1, traps: [{v: m % n === 0 ? m / n : -1, tag: T('exponents-divided')}, {v: m + n, tag: T('exponents-added-on-division')}]}));
  for (let k = 2; k <= 6; k++) if (m - n - k >= 2) divMixed.push(boxItem({lhs: `${v}${sup(m)}÷${v}${sup(n)}÷${v}${sup(k)}`, rhs: `${v}<sup>${BOX}</sup>`, ans: m - n - k, explain: `앞에서부터 차례로 나누면 ${m}−${n}−${k}=${m - n - k}입니다.`, concept: '지수법칙', difficulty: 2, traps: [{v: m - (n - k), tag: T('division-order')}, {v: m % (n * k) === 0 ? m / (n * k) : -1, tag: T('exponents-divided')}]}));
}
// D. 곱의 거듭제곱 (2x²y)³ 꼴
const powMono = [];
for (const [x, y] of PAIRS) for (const c of [2, 3, 4, 5, 6, 7, -2, -3, -4, -5]) for (let p = 1; p <= 4; p++) for (let q = 1; q <= 3; q++) for (const n of [2, 3]) {
  const coef = c ** n, lhs = `(${monoText(c, E(p, q, [x, y]), [x, y])})${sup(n)}`, res = [{c: coef, e: E(p * n, q * n, [x, y])}];
  if (coef >= 2 && coef <= 59) powMono.push(boxItem({lhs, rhs: polyText(res, [x, y], {boxTerm: 0}), ans: coef, explain: `계수 ${num(c)}도 ${n}번 곱하므로 ${c < 0 ? `(${num(c)})` : c}${sup(n)}=${coef}입니다.`, concept: '단항식의 곱셈', difficulty: 2, traps: [{v: Math.abs(c) * n, tag: T('coefficient-times-exponent')}, {v: Math.abs(c), tag: T('coefficient-not-powered')}]}));
  if (p * n >= 2) powMono.push(boxItem({lhs, rhs: polyText(res, [x, y], {boxExp: [0, x]}), ans: p * n, explain: `${x}${sup(p)}를 ${n}번 곱하므로 지수는 ${p}×${n}=${p * n}입니다.`.replace(`${x}${sup(p)}를`, j(`${x}${sup(p)}`, '을')), concept: '단항식의 곱셈', difficulty: 2, traps: [{v: p + n, tag: T('power-exponents-added')}, {v: p, tag: T('exponent-not-powered')}]}));
}
// E1. 단항식의 나눗셈
const monoDiv = [];
for (const [x, y] of PAIRS) for (const c2 of [2, 3, 4, 5, 6]) for (const k of [2, 3, 4, 5, 6, 7, 8, 9]) for (let p1 = 2; p1 <= 6; p1++) for (let p2 = 1; p2 < p1; p2++) for (const [q1, q2] of [[2, 1], [3, 1], [3, 2], [1, 1], [4, 2]]) {
  if ((c2 * 7 + k * 3 + p1 + p2 * 5 + q1) % 4) continue;
  const A = [{c: c2 * k, e: E(p1, q1, [x, y])}], B = {c: c2, e: E(p2, q2, [x, y])}, res = [{c: k, e: E(p1 - p2, q1 - q2, [x, y])}];
  const lhs = `${polyText(A, [x, y])}÷${monoText(B.c, B.e, [x, y])}`;
  const askExp = (p1 + q1 + k) % 3 === 0 && p1 - p2 >= 2;
  monoDiv.push(boxItem({lhs, rhs: polyText(res, [x, y], askExp ? {boxExp: [0, x]} : {boxTerm: 0}), ans: askExp ? p1 - p2 : k,
    explain: `계수끼리 ${c2 * k}÷${c2}=${k}, 문자끼리 지수를 빼면 ${polyText(res, [x, y])}입니다.`, concept: '단항식의 나눗셈', difficulty: 2,
    traps: askExp ? [{v: p1 % p2 === 0 ? p1 / p2 : -1, tag: T('exponents-divided')}, {v: p1 + p2, tag: T('exponents-added-on-division')}] : [{v: c2 * k - c2, tag: T('coefficients-subtracted')}, {v: c2 * k * c2, tag: T('reciprocal-not-taken')}]}));
}
// E2. A÷B×C (세 단항식)
const monoMixed = [];
for (const [x, y] of PAIRS) for (const a of [4, 6, 8, 9, 10, 12, 15, 16, 18]) for (const b of [2, 3, 4, 6]) for (const c of [2, 3, 5]) for (const sg of [[1, 1, 1], [-1, -1, 1], [-1, 1, -1], [1, -1, -1]]) {
  if (a % b || (a / b) * c > 59 || (a + b * 3 + c * 5 + sg[0] + 2) % 3) continue;
  const [ea, eb, ec] = [E(3, 2, [x, y]), E(2, 1, [x, y]), E(1, 2, [x, y])];
  const coef = sg[0] * sg[1] * sg[2] * (a / b) * c; if (coef < 2) continue;
  const part = (s, v, e) => { const t = monoText(s * v, e, [x, y]); return s < 0 ? `(${t})` : t; };
  const lhs = `${part(sg[0], a, ea)}÷${part(sg[1], b, eb)}×${part(sg[2], c, ec)}`;
  const res = [{c: coef, e: E(2, 3, [x, y])}];
  monoMixed.push(boxItem({lhs, rhs: polyText(res, [x, y], {boxTerm: 0}), ans: coef, explain: `나눗셈을 역수의 곱셈으로 바꾸면 계수는 ${a}×{frac:1/${b}}×${c}=${coef}이고 부호는 +입니다.`, concept: '단항식의 곱셈과 나눗셈', difficulty: 3,
    traps: [{v: Number.isInteger(a / (b * c)) ? a / (b * c) : -1, tag: T('division-order')}, {v: a * b * c, tag: T('reciprocal-not-taken')}]}));
}
// E3. 분수 계수 단항식으로 나누기
const monoFracDiv = [];
for (const x of ['x', 'a']) for (const c1 of [2, 3, 4, 6, 8, 9, 10, 12, 15]) for (const [u, w] of [[1, 2], [1, 3], [2, 3], [3, 4], [3, 2], [2, 5], [4, 3], [5, 2], [1, 4]]) for (const [p1, p2] of [[5, 2], [4, 1], [6, 3], [3, 1], [7, 4]]) {
  const coef = c1 * w / u; if (!Number.isInteger(coef) || coef < 2 || coef > 59) continue;
  const lhs = `${c1}${x}${sup(p1)}÷{frac:${u}/${w}}${x}${sup(p2)}`;
  monoFracDiv.push(boxItem({lhs, rhs: `${BOX}${x}${sup(p1 - p2)}`, ans: coef, explain: `${j(`{frac:${u}/${w}}${x}${sup(p2)}`, '으로')} 나누는 것은 역수를 곱하는 것이므로 계수는 ${c1}×${rat(w, u)}=${coef}입니다.`, concept: '단항식의 나눗셈', difficulty: 4,
    traps: [{v: Number.isInteger(c1 * u / w) ? c1 * u / w : -1, tag: T('reciprocal-not-taken')}]}));
}
// F. 다항식의 덧셈과 뺄셈
const polySub = [], polyQuad = [], polyNested = [], polySubChoice = [];
for (const [a, b] of PAIRS) for (let al = 1; al <= 9; al++) for (let be = -6; be <= 9; be++) for (let ga = 1; ga <= 7; ga++) for (const de of [-6, -5, -4, -3, -2, -1, 1, 2, 3, 4, 5]) {
  if (be === 0 || (al * 5 + be * 3 + ga * 7 + de) % 6) continue;
  const Pp = P([al, E(1, 0, [a, b])], [be, E(0, 1, [a, b])]), Q = P([ga, E(1, 0, [a, b])], [de, E(0, 1, [a, b])]);
  const R = order(add(Pp, Q, -1), [a, b]), lhs = `${polyText(Pp, [a, b])}−(${polyText(Q, [a, b])})`;
  const firstOnly = order(P([al - ga, E(1, 0, [a, b])], [be + de, E(0, 1, [a, b])]), [a, b]);
  R.forEach((t, i) => { if (t.c >= 2 && t.c <= 59 && R.length === 2) polySub.push(boxItem({lhs, rhs: polyText(R, [a, b], {boxTerm: i}), ans: t.c, explain: `빼는 식의 모든 항의 부호를 바꾸면 ${polyText(Pp, [a, b])}${polyText(scale(Q, -1), [a, b]).replace(/^(?!−)/, '+')}=${polyText(R, [a, b])}입니다.`, concept: '다항식의 덧셈과 뺄셈', difficulty: 2,
    traps: [{v: t.e[b] ? be + de : al + ga, tag: t.e[b] ? T('minus-first-term-only') : T('subtraction-as-addition')}]})); });
  if (R.length === 2) {
    const wrong = (S, tag) => ({v: polyText(order(S, [a, b]), [a, b]), tag});
    polySubChoice.push(choice({prompt: `${j(lhs, '을')} 계산하시오.`, answer: polyText(R, [a, b]), distractors: [wrong(firstOnly, T('minus-first-term-only')), wrong(add(Pp, Q), T('subtraction-as-addition')), wrong(P([al + ga, E(1, 0, [a, b])], [be - de, E(0, 1, [a, b])]), T('minus-last-term-only'))],
      explain: `괄호 앞의 −는 괄호 안 모든 항의 부호를 바꿉니다: ${polyText(R, [a, b])}.`, concept: '다항식의 덧셈과 뺄셈', difficulty: 2, params: {t: 'identity-choice', lhs}}));
  }
}
for (const x of ['x', 'a']) for (let a2 = 2; a2 <= 7; a2++) for (const a1 of [-5, -3, -2, 3, 4]) for (const a0 of [-4, 1, 5, 6]) for (let b2 = 1; b2 < a2; b2++) for (const b1 of [-7, -4, -1, 2]) for (const b0 of [-3, 2, 4]) {
  if ((a2 * 3 + a1 * 5 + a0 + b2 * 11 + b1 + b0 * 7) % 9) continue;
  const V = [x], e2 = {[x]: 2}, e1 = {[x]: 1}, e0 = {};
  const Pp = P([a2, e2], [a1, e1], [a0, e0]), Q = P([b2, e2], [b1, e1], [b0, e0]), R = order(add(Pp, Q, -1), V);
  const lhs = `${polyText(order(Pp, V), V)}−(${polyText(order(Q, V), V)})`;
  R.forEach((t, i) => { if (t.c >= 2 && t.c <= 59) polyQuad.push(boxItem({lhs, rhs: polyText(R, V, {boxTerm: i}), ans: t.c, explain: `동류항끼리 모으면 ${polyText(R, V)}입니다.`, concept: '이차식의 덧셈과 뺄셈', difficulty: 3, traps: [{v: Math.abs((Pp.find(s => key(s.e) === key(t.e))?.c ?? 0) + (Q.find(s => key(s.e) === key(t.e))?.c ?? 0)), tag: T('subtraction-as-addition')}]})); });
}
for (const [x, y] of PAIRS) for (let al = 1; al <= 6; al++) for (let be = 1; be <= 5; be++) for (let ga = 1; ga <= 6; ga++) for (let de = 1; de <= 5; de++) {
  if ((al * 3 + be * 5 + ga + de * 7) % 4) continue;
  // αx−{βy−(γx−δy)} = (α+γ)x−(β+δ)y
  const lhs = `${al === 1 ? '' : al}${x}−{${be === 1 ? '' : be}${y}−(${ga === 1 ? '' : ga}${x}−${de === 1 ? '' : de}${y})}`;
  const R = P([al + ga, E(1, 0, [x, y])], [-(be + de), E(0, 1, [x, y])]);
  polyNested.push(boxItem({lhs, rhs: polyText(R, [x, y], {boxTerm: 0}), ans: al + ga, explain: `안쪽 괄호부터 풀면 ${al === 1 ? '' : al}${x}−${be === 1 ? '' : be}${y}+${ga === 1 ? '' : ga}${x}−${de === 1 ? '' : de}${y}=${polyText(R, [x, y])}입니다.`, concept: '다항식의 덧셈과 뺄셈', difficulty: 4, traps: [{v: Math.abs(al - ga), tag: T('inner-sign-not-reversed')}]}));
  if (be + de >= 2) polyNested.push(boxItem({lhs, rhs: `${polyText([R[0]], [x, y])}−${BOX}${y}`, ans: be + de, explain: `안쪽 괄호부터 풀면 ${polyText(R, [x, y])}이므로 □=${be + de}입니다.`, concept: '다항식의 덧셈과 뺄셈', difficulty: 4, traps: [{v: Math.abs(be - de), tag: T('inner-sign-not-reversed')}]}));
}
// G. (단항식)×(다항식)
const monoPoly = [];
for (const [x, y] of PAIRS) for (const c of [-6, -5, -4, -3, -2, 2, 3, 4, 5, 6]) for (const mv of [x, y]) for (const r1 of [-5, -4, -3, -2, 2, 3, 4, 5]) for (const r2 of [-7, -5, -3, -1, 1, 2, 4, 6]) for (const r0 of [0, -4, 3]) {
  if ((c * 7 + r1 * 3 + r2 * 5 + r0 + (mv === x ? 1 : 0)) % 7) continue;
  const Q = P([r1, E(1, 0, [x, y])], [r2, E(0, 1, [x, y])], [r0, {}]), me = mv === x ? E(1, 0, [x, y]) : E(0, 1, [x, y]);
  const R = order(mulMono(Q, c, me), [x, y]), lhs = `${monoText(c, me, [x, y])}(${polyText(Q, [x, y])})`;
  R.forEach((t, i) => { if (t.c >= 2 && t.c <= 59) monoPoly.push(boxItem({lhs, rhs: polyText(R, [x, y], {boxTerm: i}), ans: t.c, explain: `분배법칙으로 괄호 안의 각 항에 ${monoText(c, me, [x, y])}를 곱하면 ${polyText(R, [x, y])}입니다.`.replace(/([^ ]+)를 곱하면/, (_, s) => `${j(s, '을')} 곱하면`), concept: '(단항식)×(다항식)', difficulty: r0 ? 3 : 2, traps: [{v: Math.abs(Q.find(s => { const e = {...s.e}; e[mv] = (e[mv] ?? 0) + 1; return key(e) === key(t.e); })?.c ?? 0), tag: T('partial-distribution')}, {v: Math.abs(c), tag: T('coefficient-only')}]})); });
}
// H. (다항식)÷(단항식)
const polyDiv = [], polyDivFrac = [];
for (const [x, y] of PAIRS) for (const c of [2, 3, 4, 5, 6, -2, -3]) for (const de of [E(1, 0, [x, y]), E(1, 1, [x, y]), E(0, 1, [x, y])]) for (const r1 of [2, 3, 4, 5, 6, 7, 8, 9, 11, 12, 14, -3, -2, -5]) for (const r2 of [-9, -7, -5, -4, -3, 2, 3, 6, 8, 10, 13]) {
  if ((c * 5 + r1 * 3 + r2 + deg(de)) % 2) continue;
  const R = order(P([r1, E(2, 1, [x, y])], [r2, E(1, 2, [x, y])]), [x, y]); // quotient terms before division
  const Q = mulMono(R, c, de), quo = R;
  const dividend = polyText(order(Q, [x, y]), [x, y]), divisor = monoText(c, de, [x, y]);
  quo.forEach((t, i) => { if (t.c >= 2 && t.c <= 59) { const rhs = polyText(quo, [x, y], {boxTerm: i}); polyDiv.push(amount({prompt: `${j(dividend, '을')} ${j(divisor, '으로')} 나누면 ${rhs}이다. □ 안에 알맞은 수를 구하시오.`, answer: t.c, explain: `각 항을 ${j(divisor, '으로')} 나누면 ${polyText(quo, [x, y])}입니다.`, concept: '(다항식)÷(단항식)', difficulty: 3, params: {t: 'div-box', dividend, divisor, rhs}, traps: [{v: i === 1 ? Math.abs(Q[1].c) : -1, tag: T('divide-first-term-only')}, {v: Math.abs(t.c * c * c), tag: T('reciprocal-not-taken')}]})); } });
}
for (const x of ['x', 'a']) for (const [u, w] of [[1, 2], [2, 3], [3, 4], [1, 3], [3, 2], [2, 5]]) for (const r1 of [2, 3, 4, 6]) for (const r2 of [-9, -6, -4, 3, 8]) for (const r0 of [0, 5, -2]) {
  const quo = P([r1 * w, {[x]: 1}], [r2 * w, {}]); if (quo.some(t => t.c % 1)) continue;
  const Q = quo.map(t => ({c: t.c * u / w, e: {...t.e, [x]: (t.e[x] ?? 0) + 1}})); // Q = (u/w)x × quo
  if (Q.some(t => !Number.isInteger(t.c)) || (r1 + r2 * 3 + u + w + r0) % 3) continue;
  const dividend = polyText(order(Q, [x]), [x]), divisor = `{frac:${u}/${w}}${x}`, rhs = polyText(quo, [x], {boxTerm: 0});
  if (quo[0].c >= 2 && quo[0].c <= 59) polyDivFrac.push(amount({prompt: `${j(dividend, '을')} ${j(divisor, '으로')} 나누면 ${rhs}이다. □ 안에 알맞은 수를 구하시오.`, answer: quo[0].c, explain: `각 항에 ${j(rat(w, u), '을')} 곱한 뒤 ${j(x, '으로')} 나누면 ${polyText(quo, [x])}입니다.`, concept: '(다항식)÷(단항식)', difficulty: 4, params: {t: 'div-box', dividend, divisor, rhs}, traps: [{v: Number.isInteger(Q[0].c * u / w) ? Q[0].c * u / w : -1, tag: T('reciprocal-not-taken')}]}));
}
// I1. A, B를 대입하여 정리하기
const substAB = [];
for (const [x, y] of PAIRS) for (const [pa, pb] of [[2, -1], [3, 2], [1, -3], [4, -1], [2, 3], [3, -2], [1, 4], [5, -2]]) for (const [qa, qb] of [[1, 3], [-1, 2], [2, -1], [1, -2], [3, 1], [-2, 1]]) for (const [kA, kB, form, hard] of [[2, -1, '2A−B', 0], [1, -2, 'A−2B', 0], [3, 1, '3A+B', 0], [1, 3, 'A+3B', 0], [1, -2, '3A−2(A+B)', 1]]) {
  const V = [x, y], A = P([pa, E(1, 0, V)], [pb, E(0, 1, V)]), B = P([qa, E(1, 0, V)], [qb, E(0, 1, V)]);
  const R = order(add(scale(A, kA), scale(B, kB)), V); if (R.length !== 2) continue;
  const def = `A=${polyText(A, V)}, B=${polyText(B, V)}`, k = n => Math.abs(n) === 1 ? '' : String(Math.abs(n));
  const expansion = `${k(kA)}(${polyText(A, V)})${kB < 0 ? M : '+'}${k(kB)}(${polyText(B, V)})`;
  // 괄호 없이 대입: B의 둘째 항에는 kB가 곱해지지 않는다 → y의 계수 kA·pb + sign(kB)·qb
  const noParen = kA * pb + Math.sign(kB) * qb;
  R.forEach((t, i) => { if (t.c >= 2 && t.c <= 59) substAB.push(amount({prompt: `${def}일 때, ${j(form, '을')} ${x}, ${y}에 대한 식으로 나타내면 ${polyText(R, V, {boxTerm: i})}이다. □ 안에 알맞은 수를 구하시오.`, answer: t.c,
    explain: `${hard ? `${form}=A−2B이므로 ` : ''}괄호를 써서 대입하면 ${expansion}=${polyText(R, V)}입니다.`, concept: '다항식의 덧셈과 뺄셈', difficulty: hard ? 4 : 3, params: {t: 'subst-box', A: polyText(A, V), B: polyText(B, V), form, rhs: polyText(R, V, {boxTerm: i})},
    traps: [{v: t.e[y] ? Math.abs(noParen) : -1, tag: T('substitution-without-parentheses')}]})); });
}
// I2. 간단히 한 뒤 식의 값
const evalMono = [];
for (const [x, y] of PAIRS) for (const [c1, c2] of [[12, 4], [18, 6], [20, 5], [24, 8], [15, 3], [16, 8], [30, 10], [14, 7]]) for (const [p1, q1, p2, q2] of [[3, 2, 1, 1], [4, 2, 2, 1], [3, 3, 2, 1], [4, 3, 3, 2], [2, 3, 1, 2]]) for (const x0 of [-3, -2, -1, 1, 2, 3]) for (const y0 of [-2, -1, 1, 2, 3]) {
  const k = c1 / c2, ex = p1 - p2, ey = q1 - q2, v = k * x0 ** ex * y0 ** ey;
  if (v < 2 || v > 59 || (c1 + p1 * 3 + x0 * 5 + y0 * 7 + q2) % 4) continue;
  const expr = `${c1}${x}${sup(p1)}${y}${sup(q1)}÷${c2}${x}${sup(p2)}${y}${sup(q2)}`;
  evalMono.push(amount({prompt: `${x}=${num(x0)}, ${y}=${num(y0)}일 때, ${expr}의 값을 구하시오.`, answer: v, explain: `먼저 간단히 하면 ${monoText(k, E(ex, ey, [x, y]), [x, y])}이고, 대입하면 ${v}입니다.`, concept: '식의 값', difficulty: 3, params: {t: 'eval', expr, vals: {[x]: x0, [y]: y0}}, traps: [{v: Math.abs(k * x0 * y0), tag: T('exponents-dropped')}]}));
}
// J. 수의 거듭제곱
const numExp = [], baseChange = [], powSum = [], digitCount = [];
for (const b of [2, 3, 5, 7]) for (let m = 2; m <= 12; m++) for (let n = m + 2; n <= 30; n++) numExp.push(amount({prompt: `${b}<sup>x</sup>×${b}${sup(m)}=${b}${sup(n)}일 때, 자연수 x의 값을 구하시오.`, answer: n - m, explain: `지수끼리 더하면 x+${m}=${n}이므로 x=${n - m}입니다.`, concept: '지수법칙', difficulty: 3, params: {t: 'exp-solve', lhs: `${b}<sup>x</sup>×${b}${sup(m)}`, rhs: `${b}${sup(n)}`}, traps: [{v: n % m === 0 ? n / m : -1, tag: T('exponents-multiplied')}, {v: n + m, tag: T('exponents-added-on-division')}]}));
for (const [B, b, k] of [[4, 2, 2], [8, 2, 3], [16, 2, 4], [32, 2, 5], [9, 3, 2], [27, 3, 3], [81, 3, 4], [25, 5, 2], [125, 5, 3], [49, 7, 2]]) for (let m = 2; m <= 9; m++) for (let extra = 0; extra <= 5; extra++) {
  if (k * m + extra > 59) continue;
  const lhs = extra ? `${B}${sup(m)}×${b}${sup(extra)}` : `${B}${sup(m)}`;
  baseChange.push(boxItem({lhs, rhs: `${b}<sup>${BOX}</sup>`, ans: k * m + extra, explain: `${B}=${b}${sup(k)}이므로 ${B}${sup(m)}=${b}${sup(k * m)}${extra ? `, 여기에 ${b}${sup(extra)}를 곱하면 ${b}${sup(k * m + extra)}` : ''}입니다.`.replace(`${b}${sup(extra)}를`, j(`${b}${sup(extra)}`, '을')), concept: '지수법칙', difficulty: extra ? 4 : 3, traps: [{v: m + extra, tag: T('base-not-rewritten')}, {v: k + m + extra, tag: T('power-exponents-added')}]}));
}
for (const [b, cnt] of [[2, 2], [3, 3], [4, 4], [5, 5], [2, 4], [3, 9], [2, 8]]) for (let m = 2; m <= 12; m++) {
  const add1 = Math.round(Math.log(cnt) / Math.log(b)); if (b ** add1 !== cnt) continue;
  const lhs = Array(cnt).fill(`${b}${sup(m)}`).join('+'); if (cnt > 5) continue;
  powSum.push(boxItem({lhs, rhs: `${b}<sup>${BOX}</sup>`, ans: m + add1, explain: `${j(`${b}${sup(m)}`, '을')} ${cnt}번 더하면 ${cnt}×${b}${sup(m)}=${cnt === b ? '' : `${b}${sup(add1)}×${b}${sup(m)}=`}${b}${sup(m + add1)}입니다.`, concept: '지수법칙', difficulty: 4, traps: [{v: m * cnt, tag: T('sum-as-product-of-powers')}, {v: m + cnt, tag: T('count-added-to-exponent')}]}));
}
for (const x of [2, 4, 8]) for (const y of [4, 8, 16, 32]) for (let m = 2; m <= 9; m++) for (let n = 2; n <= 9; n++) {
  const lx = Math.log2(x), ly = Math.log2(y), s = lx * m + ly * n; if (s > 59 || x === y || (m + n * 3 + x) % 3) continue;
  powSum.push(boxItem({lhs: `${x}${sup(m)}×${y}${sup(n)}`, rhs: `2<sup>${BOX}</sup>`, ans: s, explain: `${x}=2${sup(lx)}, ${y}=2${sup(ly)}이므로 지수는 ${lx}×${m}+${ly}×${n}=${s}입니다.`, concept: '지수법칙', difficulty: 4, traps: [{v: m + n, tag: T('base-not-rewritten')}]}));
}
for (let a = 3; a <= 30; a++) for (let b = 3; b <= 30; b++) for (const extra of [1, 3, 7]) {
  const val = 2n ** BigInt(a) * 5n ** BigInt(b) * BigInt(extra), digits = val.toString().length;
  if (Math.abs(a - b) > 6 || digits < 2 || digits > 59 || (a * 3 + b * 7 + extra) % 5) continue;
  const expr = `2${sup(a)}×${extra > 1 ? extra + '×' : ''}5${sup(b)}`;
  digitCount.push(amount({prompt: `${j(expr, '은')} 몇 자리의 자연수인지 구하시오.`, answer: digits,
    explain: `2와 5를 짝지으면 ${(val / 10n ** BigInt(Math.min(a, b))).toString()}×10${sup(Math.min(a, b))}이므로 ${digits - Math.min(a, b)}+${Math.min(a, b)}=${digits}자리입니다.`, concept: '지수법칙의 활용', difficulty: 4, params: {t: 'digits', expr},
    traps: [{v: a + b, tag: T('digits-as-exponent-sum')}, {v: Math.max(a, b), tag: T('digits-as-larger-exponent')}]}));
}
// K1. (선택) 음수의 거듭제곱
const signChoice = [];
for (const [x, y] of PAIRS) for (const k of [2, 3, 4, 5]) for (const n of [2, 3]) for (const c of [1, 2, 3, 4, 5]) for (const ey of [1, 2]) {
  const e = E(n, ey, [x, y]), correct = (-k) ** n * c, lhs = `(${M}${k}${x})${sup(n)}×${monoText(c, E(0, ey, [x, y]), [x, y])}`;
  const txt = cc => monoText(cc, e, [x, y]).replace(/<sup>2<\/sup>/g, '²').replace(/<sup>3<\/sup>/g, '³');
  signChoice.push(choice({prompt: `${j(lhs, '을')} 간단히 하시오.`, answer: txt(correct),
    distractors: [{v: txt(-correct), tag: T('negative-base-sign-lost')}, {v: txt(-k * n * c), tag: T('coefficient-times-exponent')}, {v: txt(k * n * c), tag: T('coefficient-times-exponent')}, {v: txt(-k * c), tag: T('coefficient-not-powered')}],
    explain: `(${M}${k})${sup(n)}=${num((-k) ** n)}이므로 ${txt(correct)}입니다.`, concept: '단항식의 곱셈', difficulty: 2, params: {t: 'identity-choice', lhs}}));
}

const intro = amount({prompt: 'a<sup>2</sup>×a=a<sup>□</sup>일 때, □ 안에 알맞은 수를 구하시오.', answer: 3, explain: 'a<sup>2</sup>×a<sup>1</sup>이므로 지수를 더해 2+1=3입니다. 3닢을 붓습니다.', concept: '지수법칙', difficulty: 1, params: {t: 'identity-box', lhs: 'a<sup>2</sup>×a', rhs: 'a<sup>□</sup>'}, intro: true});
writeUnitPack({id: U, title: '식의 계산', unit: U, standards: ['[9수02-08]', '[9수02-09]', '[9수02-10]'], intro, groups: [
  [expProd, 35], [powPlain, 20], [divPlain, 25],
  [powExtra, 12], [divMixed, 8], [powMono, 22], [monoDiv, 18], [polySub, 20], [monoPoly, 16], [signChoice, 20], [polySubChoice, 20],
  [monoMixed, 18], [polyQuad, 15], [polyDiv, 22], [substAB, 15], [evalMono, 15], [numExp, 15], [baseChange, 12],
  [monoFracDiv, 14], [polyNested, 15], [polyDivFrac, 10], [powSum, 18], [digitCount, 15],
]});
