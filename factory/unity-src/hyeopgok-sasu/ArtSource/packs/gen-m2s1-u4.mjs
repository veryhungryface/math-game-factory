#!/usr/bin/env node
// 중2-1 단원4 연립일차방정식 [9수02-13].
// Systems are built backward from a chosen integer solution. Schema v3 (4-choice): wrong choices are
// recomputed from mis-solved systems — solution order swapped, 가감법 with the wrong operation, one side
// only multiplied (교육과정 오개념), coefficient not divided, a point that satisfies one equation only.
import {amount, choice, writeUnitPack, j, gcd, num, dec, rat, M} from './m2s1-lib.mjs';
const U = 'm2s1-u4', T = s => `${U}.${s}`;

function lin2(a, b, {x = 'x', y = 'y', mode = 'int', den = 1} = {}) { // a·x + b·y as text (coefficients divided by den)
  const term = (c, v, first) => { if (c === 0) return ''; const neg = c < 0, ac = Math.abs(c); let coef; if (den === 1) coef = ac === 1 ? '' : String(ac); else { const g = gcd(ac, den), n = ac / g, d = den / g; coef = d === 1 ? (n === 1 ? '' : String(n)) : mode === 'dec' ? dec(n, d) : `{frac:${n}/${d}}`; } return (neg ? M : first ? '' : '+') + coef + v; };
  const s = term(a, x, true); return s + term(b, y, !s);
}
const eq = (a, b, c, o = {}) => `${lin2(a, b, o)}=${o.den && o.den !== 1 ? (Number.isInteger(c / o.den) ? num(c / o.den) : o.mode === 'dec' ? dec(c, o.den) : (c < 0 ? M : '') + `{frac:${Math.abs(c) / gcd(Math.abs(c), o.den)}/${o.den / gcd(Math.abs(c), o.den)}}`) : num(c)}`;
const sys = (e1, e2) => `연립방정식 ${e1}, ${j(e2, '을')}`;
const det = (a1, b1, a2, b2) => a1 * b2 - a2 * b1;
const pair = (x, y) => `(${num(x)}, ${num(y)})`;
const ifInt = (n, d) => d !== 0 && n % d === 0 ? n / d : null;
/** Misconception values for "x (or y) of the system" from integer standard forms [[a1,b1,c1],[a2,b2,c2]]. */
function sysTraps([[a1, b1, c1], [a2, b2, c2]], x0, y0, ask) {
  const D = det(a1, b1, a2, b2), v = ask === 'x' ? x0 : y0, other = ask === 'x' ? y0 : x0, Dp = a1 * b2 + a2 * b1;
  const numP = ask === 'x' ? c1 * b2 + c2 * b1 : a1 * c2 + a2 * c1;
  const one = ask === 'x' ? (Math.abs(b2) !== 1 ? c1 - c2 * b1 : Math.abs(b1) !== 1 ? c1 * b2 - c2 : null) : (Math.abs(a2) !== 1 ? a1 * c2 - c1 : Math.abs(a1) !== 1 ? c2 - a2 * c1 : null);
  return [{v: other, tag: T('solution-order-swapped')}, {v: Dp ? [numP, Dp] : null, tag: T('elimination-sign-error')}, {v: one === null ? null : [one, D], tag: T('multiplied-one-side-only')},
    {v: Math.abs(D) !== 1 ? v * Math.abs(D) : null, tag: T('coefficient-not-divided')}, {v: -v, tag: T('sign-error')}];
}

// A. 미지수가 2개인 일차방정식의 자연수 해
const natCount = [], oneSol = [];
for (let a = 1; a <= 6; a++) for (let b = 1; b <= 6; b++) for (let c = 5; c <= 60; c++) {
  if (gcd(a, b) !== 1 || (a * 7 + b * 5 + c) % 4) continue;
  const sols = []; for (let x = 1; x < c; x++) { const r = c - a * x; if (r > 0 && r % b === 0) sols.push([x, r / b]); }
  let nonneg = 0; for (let x = 0; a * x <= c; x++) if ((c - a * x) % b === 0) nonneg++;
  if (sols.length >= 2 && sols.length <= 12) natCount.push(amount({prompt: `x, y가 자연수일 때, 일차방정식 ${eq(a, b, c)}의 해의 개수를 구하시오.`, answer: sols.length,
    explain: `x에 1, 2, 3, …을 차례로 대입하면 ${sols.map(([x, y]) => pair(x, y)).join(', ')}의 ${sols.length}개입니다.`, concept: '미지수가 2개인 일차방정식의 해', difficulty: 1,
    params: {t: 'lin-natural-count', eq: eq(a, b, c)}, traps: [{v: nonneg, tag: T('zero-counted-as-natural')}, {v: Math.floor(c / a), tag: T('only-x-condition-checked')}, {v: Math.floor((c - b) / a), tag: T('divisibility-ignored')}]}));
}
for (let a = -5; a <= 6; a++) for (let b = -4; b <= 5; b++) for (let x0 = -3; x0 <= 8; x0++) for (let y0 = -9; y0 <= 20; y0++) {
  if (y0 > -2 && y0 < 2) continue;
  if (a <= 0 || b === 0 || x0 === 0 || (a * 7 + b * 11 + x0 * 3 + y0) % 13) continue;
  const c = a * x0 + b * y0, e = eq(a, b, c); if (gcd(gcd(a, b), c) !== 1) continue;
  oneSol.push(amount({neg: true, prompt: `일차방정식 ${e}의 한 해가 x=${num(x0)}, y=k일 때, k의 값을 구하시오.`, answer: y0, explain: `x=${num(x0)}을 대입하면 ${lin2(0, b)}=${num(c - a * x0)}이므로 k=${num(y0)}입니다.`.replace(`x=${num(x0)}을`, j(`x=${num(x0)}`, '을')), concept: '미지수가 2개인 일차방정식의 해', difficulty: 1,
    params: {t: 'lin-one-solution', eq: e, x0}, traps: [{v: [c + a * x0, b], tag: T('substitution-sign-error')}, {v: Math.abs(b) !== 1 ? c - a * x0 : null, tag: T('coefficient-not-divided')}, {v: -y0, tag: T('sign-error')}, {v: [c, b], tag: T('x-term-dropped')}]}));
}
// B/C/D. 연립방정식 풀기
const addSub = [], substi = [], general = [], hard = [];
function pushSolve(pool, e1, e2, x0, y0, difficulty, how, concept, co, extra = () => []) {
  for (const ask of ['x', 'y']) { const v = ask === 'x' ? x0 : y0; if (v < 2 || v > 59) continue;
    pool.push(amount({prompt: `${sys(e1, e2)} 풀 때, ${ask}의 값을 구하시오.`, answer: v, neg: true, explain: `${how} 해는 x=${num(x0)}, y=${num(y0)}입니다.`, concept, difficulty, params: {t: 'system-solve', e1, e2, ask},
      traps: [...extra(ask), ...sysTraps(co, x0, y0, ask)]})); }
}
for (let x0 = 1; x0 <= 25; x0++) for (let y0 = 1; y0 <= 25; y0++) {
  if (x0 === y0 || (x0 * 7 + y0 * 3) % 3) continue;
  pushSolve(addSub, eq(1, 1, x0 + y0), eq(1, -1, x0 - y0), x0, y0, 1, '두 식을 더하면 x를, 빼면 y를 구할 수 있어', '가감법', [[1, 1, x0 + y0], [1, -1, x0 - y0]],
    ask => [{v: ask === 'x' ? x0 + y0 : x0 - y0, tag: T('one-equation-only')}]);
}
for (let m = -4; m <= 4; m++) for (let k = -9; k <= 9; k++) for (let a = 1; a <= 5; a++) for (let b = -3; b <= 4; b++) for (let x0 = 1; x0 <= 18; x0++) {
  if (m === 0 || b === 0 || det(-m, 1, a, b) === 0 || (m * 7 + k * 3 + a * 5 + b + x0 * 11) % 29) continue;
  const y0 = m * x0 + k; if (y0 < 1 || y0 > 45) continue;
  const e1 = `y=${lin2(m, 0)}${k ? (k > 0 ? '+' : M) + Math.abs(k) : ''}`, e2 = eq(a, b, a * x0 + b * y0); if (gcd(gcd(a, b), a * x0 + b * y0) !== 1) continue;
  pushSolve(substi, e1, e2, x0, y0, 2, `첫째 식을 둘째 식의 y에 대입하면`, '대입법', [[-m, 1, k], [a, b, a * x0 + b * y0]],
    ask => ask === 'y' ? [{v: m * x0 - k, tag: T('substitution-sign-error')}] : [{v: a + b * m ? [a * x0 + b * y0 - k, a + b * m] : null, tag: T('substitution-without-parentheses')}]);
}
for (let a1 = 1; a1 <= 9; a1++) for (let b1 = -7; b1 <= 7; b1++) for (let a2 = 1; a2 <= 8; a2++) for (let b2 = -7; b2 <= 7; b2++) for (const [x0, y0] of [[2, 3], [3, 5], [4, 1], [5, 2], [1, 6], [6, 4], [7, 3], [2, 9], [8, 5], [3, 11], [10, 4], [12, 7], [9, 2], [4, 13], [15, 6], [7, 16], [19, 3], [13, 10], [5, 22], [24, 9]]) {
  if (a1 <= 0 || !b1 || a2 <= 0 || !b2 || !det(a1, b1, a2, b2) || (a1 * 13 + b1 * 7 + a2 * 5 + b2 * 3 + x0 * 17 + y0) % 53) continue;
  const unit = [a1, b1, a2, b2].some(v => Math.abs(v) === 1), e1 = eq(a1, b1, a1 * x0 + b1 * y0), e2 = eq(a2, b2, a2 * x0 + b2 * y0);
  if (gcd(gcd(a1, b1), a1 * x0 + b1 * y0) > 1 || gcd(gcd(a2, b2), a2 * x0 + b2 * y0) > 1) continue;
  pushSolve(unit ? general : hard, e1, e2, x0, y0, unit ? 2 : 3, unit ? '한 미지수의 계수의 절댓값을 같게 한 뒤 더하거나 빼면' : '두 식에 각각 적당한 수를 곱해 한 미지수를 없애면', '가감법', [[a1, b1, a1 * x0 + b1 * y0], [a2, b2, a2 * x0 + b2 * y0]]);
}
// E. 괄호·소수·분수 계수
const special = [];
for (let k = 2; k <= 4; k++) for (let m = -3; m <= 3; m++) for (let n = -4; n <= 4; n++) for (const [a2, b2] of [[1, -1], [1, 1], [2, -1], [1, 2], [3, -2]]) for (const [x0, y0] of [[2, 3], [3, 1], [4, 5], [5, 2], [6, 3], [1, 4], [7, 2], [3, 8], [10, 5], [12, 7], [15, 4], [9, 11], [14, 3], [6, 17], [18, 5], [11, 13], [21, 8], [4, 19]]) {
  if (!m || !n || (k * 3 + m * 5 + n * 7 + a2 + x0 * 11 + y0) % 7) continue;
  const bb = k * m + n; if (!det(k, bb, a2, b2)) continue;
  const e1 = `${k}(${lin2(1, m)})${n > 0 ? '+' : M}${Math.abs(n) === 1 ? '' : Math.abs(n)}y=${num(k * x0 + bb * y0)}`, e2 = eq(a2, b2, a2 * x0 + b2 * y0); if (gcd(gcd(k, n), k * x0 + bb * y0) !== 1) continue;
  const co2 = [a2, b2, a2 * x0 + b2 * y0], partial = [k, m + n, k * x0 + bb * y0];
  const pd = det(partial[0], partial[1], a2, b2);
  pushSolve(special, e1, e2, x0, y0, 3, `괄호를 풀어 정리한 뒤 풀면`, '괄호가 있는 연립방정식', [[k, bb, k * x0 + bb * y0], co2],
    ask => [{v: pd ? (ask === 'x' ? [partial[2] * b2 - co2[2] * partial[1], pd] : [partial[0] * co2[2] - a2 * partial[2], pd]) : null, tag: T('distribution-partial')}]);
}
for (const [a1, b1] of [[2, 3], [3, 2], [5, 3], [4, 7], [3, -2], [2, -5], [7, 4], [6, 5], [1, 4]]) for (const [a2, b2] of [[1, -1], [1, 1], [2, 1], [1, -2], [3, 1]]) for (const [x0, y0] of [[2, 3], [3, 1], [4, 5], [5, 2], [6, 3], [1, 4], [7, 2], [3, 8], [10, 5], [12, 7], [15, 4], [9, 11], [14, 3], [6, 17], [18, 5], [11, 13], [21, 8], [4, 19]]) for (const mode of ['dec', 'frac']) {
  if (!det(a1, b1, a2, b2) || (a1 * 3 + b1 * 5 + a2 * 7 + x0 * 11 + y0 + (mode === 'dec' ? 1 : 0)) % 3) continue;
  let e1, co1 = [a1, b1, a1 * x0 + b1 * y0];
  if (mode === 'dec') e1 = eq(a1, b1, a1 * x0 + b1 * y0, {mode: 'dec', den: 10});
  else { const [p, q] = [[2, 3], [3, 4], [2, 5], [4, 3], [6, 4]][(((a1 + b1 + x0) % 5) + 5) % 5]; // (a1/p)x + (b1/q)y = c, c integer or fraction
    const c = `{frac:${Math.abs(a1)}/${p}}`; if (gcd(Math.abs(a1), p) !== 1 || gcd(Math.abs(b1), q) !== 1) continue;
    const L = p * q / gcd(p, q), cn = a1 * x0 * (L / p) + b1 * y0 * (L / q); if (cn % L !== 0) continue; // integer right side only
    co1 = [a1 * (L / p), b1 * (L / q), cn];
    e1 = `${a1 < 0 ? M : ''}${c}x${b1 < 0 ? M : '+'}{frac:${Math.abs(b1)}/${q}}y=${cn % L === 0 ? num(cn / L) : (cn < 0 ? M : '') + `{frac:${Math.abs(cn) / gcd(Math.abs(cn), L)}/${L / gcd(Math.abs(cn), L)}}`}`; }
  pushSolve(special, e1, eq(a2, b2, a2 * x0 + b2 * y0), x0, y0, 3, mode === 'dec' ? '첫째 식의 양변에 10을 곱해 정리하면' : '첫째 식의 양변에 분모의 최소공배수를 곱해 정리하면', mode === 'dec' ? '계수가 소수인 연립방정식' : '계수가 분수인 연립방정식', [co1, [a2, b2, a2 * x0 + b2 * y0]]);
}
// F. 해가 주어졌을 때 계수 구하기
const coefFind = [], coefShared = [];
for (let a = 1; a <= 7; a++) for (let b = 1; b <= 7; b++) for (const [x0, y0] of [[2, 3], [3, 1], [1, 4], [4, 2], [2, 5], [5, 3], [3, 4], [1, 2], [6, 1], [2, 7]]) for (const [p, s1, s2] of [[1, 1, -1], [2, -1, 1], [3, 1, 1], [1, -1, 1]]) {
  if ((a * 5 + b * 7 + x0 * 3 + y0 + p) % 3) continue;
  const c1 = a * x0 + s1 * p * y0, c2 = x0 * s2 * 1 + (-b) * y0; // ax ± p·y = c1,  ±x − by = c2
  const e1 = `ax${s1 > 0 ? '+' : M}${p === 1 ? '' : p}y=${num(c1)}`, e2 = `${s2 > 0 ? '' : M}x${M}by=${num(c2)}`;
  if (a + b < 2 || a + b > 59) continue;
  coefFind.push(amount({prompt: `연립방정식 ${e1}, ${e2}의 해가 x=${x0}, y=${y0}일 때, a+b의 값을 구하시오.`, answer: a + b,
    explain: `${j(`x=${x0}, y=${y0}`, '을')} 두 식에 대입하면 a=${num(a)}, b=${num(b)}이므로 a+b=${a + b}입니다.`, concept: '연립방정식의 해의 뜻', difficulty: 3, params: {t: 'coef-find', e1, e2, x0, y0},
    traps: [{v: a - b, tag: T('sign-dropped')}, {v: a * b, tag: T('asked-quantity-misread')}, {v: x0 + y0, tag: T('solution-as-answer')}]}));
}
for (const [[a1, b1], [a2, b2]] of [[[1, 1], [1, -1]], [[2, 1], [1, -1]], [[1, 2], [3, -1]], [[3, 1], [1, 1]], [[2, -1], [1, 1]], [[1, -2], [2, 1]]]) for (const [x0, y0] of [[3, 2], [2, 1], [4, 3], [1, 2], [5, 2], [3, 4], [2, 5]]) for (let a = 1; a <= 5; a++) for (let b = 1; b <= 5; b++) {
  if ((a1 * 3 + a2 * 5 + x0 * 7 + y0 + a * 11 + b) % 4) continue;
  const k1 = [1, -1, 2][(a + x0) % 3], k2 = [2, 1, 3][(b + y0) % 3];
  const e1 = eq(a1, b1, a1 * x0 + b1 * y0), f1 = `ax${k1 > 0 ? '+' : M}${Math.abs(k1) === 1 ? '' : Math.abs(k1)}y=${num(a * x0 + k1 * y0)}`;
  const e2 = eq(a2, b2, a2 * x0 + b2 * y0), f2 = `${k2 === 1 ? '' : k2}x+by=${num(k2 * x0 + b * y0)}`;
  coefShared.push(amount({prompt: `연립방정식 ${e1}, ${f1}의 해와 연립방정식 ${e2}, ${f2}의 해가 서로 같을 때, a+b의 값을 구하시오.`, answer: a + b,
    explain: `계수를 아는 두 식 ${e1}, ${e2}를 연립하면 x=${x0}, y=${y0}이고, 이를 대입하면 a=${num(a)}, b=${num(b)}입니다.`.replace(`${e2}를`, j(e2, '을')), concept: '연립방정식의 해의 뜻', difficulty: 4, params: {t: 'coef-shared', e1, f1, e2, f2},
    traps: [{v: a - b, tag: T('sign-dropped')}, {v: x0 + y0, tag: T('solution-as-answer')}, {v: a * b, tag: T('asked-quantity-misread')}]}));
}
// G. A=B=C 꼴
const abc = [];
for (let a1 = 1; a1 <= 6; a1++) for (let b1 = -6; b1 <= 6; b1++) for (let a2 = 1; a2 <= 7; a2++) for (let b2 = -6; b2 <= 6; b2++) for (const [x0, y0] of [[2, 3], [3, 1], [4, 2], [1, 5], [5, 3], [2, 6], [6, 2], [3, 7], [4, 5], [5, 4], [7, 3], [2, 9], [8, 3], [3, 5], [10, 3], [4, 11], [12, 5], [6, 13], [9, 7], [11, 2], [14, 6]]) {
  if (!b1 || !b2 || a1 === a2 || !det(a1, b1, a2, b2) || a1 * x0 + b1 * y0 !== a2 * x0 + b2 * y0 || a1 * x0 + b1 * y0 === 0) continue;
  const c = a1 * x0 + b1 * y0, e = `${lin2(a1, b1)}=${lin2(a2, b2)}=${num(c)}`;
  for (const ask of ['x', 'y']) { const v = ask === 'x' ? x0 : y0; if (v < 2) continue;
    abc.push(amount({prompt: `방정식 ${j(e, '을')} 풀 때, ${ask}의 값을 구하시오.`, answer: v, explain: `${lin2(a1, b1)}=${num(c)}, ${j(`${lin2(a2, b2)}=${num(c)}`, '을')} 연립하여 풀면 x=${x0}, y=${y0}입니다.`, concept: 'A=B=C 꼴의 방정식', difficulty: 4,
      params: {t: 'abc-solve', e, ask}, neg: true, traps: [{v: ask === 'x' ? y0 : x0, tag: T('solution-order-swapped')}, {v: c, tag: T('constant-as-solution')}, ...sysTraps([[a1, b1, c], [a2, b2, c]], x0, y0, ask).slice(1)]})); }
}
// H. 연립방정식의 활용
const wpA = [], wpB = [];
for (let H = 5; H <= 40; H++) for (let r = 2; r <= H - 2; r++) {
  const c = H - r, L = 2 * c + 4 * r; if ((H * 3 + r * 7) % 5) continue;
  for (const ask of ['토끼', '닭']) { const v = ask === '토끼' ? r : c;
    wpA.push(amount({prompt: `닭과 토끼가 모두 ${H}마리 있고, 다리 수의 합이 ${L}개이다. ${j(ask, '은')} 몇 마리인지 구하시오.`, answer: v, explain: `닭 x마리, 토끼 y마리라 하면 x+y=${H}, 2x+4y=${L}이므로 x=${c}, y=${r}입니다.`, concept: '연립방정식의 활용', difficulty: 3,
      params: {t: 'wp-heads-legs', H, L, ask}, traps: [{v: ask === '토끼' ? c : r, tag: T('solution-order-swapped')}, {v: 2 * v, tag: T('coefficient-not-divided')}, {v: Math.floor(L / 4), tag: T('one-equation-only')}, {v: Math.floor(L / 2), tag: T('one-equation-only')}]})); }
}
for (const [pn, pu, p] of [['연필', '자루', 500], ['연필', '자루', 600], ['지우개', '개', 400], ['볼펜', '자루', 800]]) for (const [qn, qu, q] of [['공책', '권', 1200], ['공책', '권', 1500], ['수첩', '권', 2000], ['자', '개', 1000]]) for (let n = 5; n <= 30; n++) for (let y0 = 2; y0 <= n - 2; y0++) {
  if (p >= q || (n * 7 + y0 * 3 + p / 100 + q / 100) % 11) continue;
  const Tt = p * (n - y0) + q * y0;
  wpA.push(amount({prompt: `한 ${pu}에 ${p}원인 ${pn}과 한 ${qu}에 ${q}원인 ${j(qn, '을')} 합하여 ${n}개 사고 ${Tt}원을 냈다. ${j(qn, '은')} 몇 ${qu} 샀는지 구하시오.`.replace(`${pn}과`, j(pn, '과')), answer: y0,
    explain: `${pn} x${pu}, ${qn} ${j(`y${qu}`, '이라')} 하면 x+y=${n}, ${p}x+${q}y=${Tt}이므로 y=${y0}입니다.`, concept: '연립방정식의 활용', difficulty: 3, params: {t: 'wp-two-prices', n, p, q, T: Tt},
    traps: [{v: n - y0, tag: T('solution-order-swapped')}, {v: Math.floor(Tt / q), tag: T('one-equation-only')}, {v: ifInt(Tt + p * n, q + p), tag: T('elimination-sign-error')}, {v: ifInt(Tt - p * n, q), tag: T('coefficient-not-divided')}, {v: Math.floor(Tt / p) - n > 0 ? Math.floor(Tt / p) - n : null, tag: T('one-equation-only')}]}));
}
for (let a = 1; a <= 5; a++) for (let b = a + 1; b <= 9; b++) {
  const orig = 10 * a + b, d = 9 * (b - a);
  wpB.push(amount({prompt: `두 자리의 자연수가 있다. 각 자리의 숫자의 합은 ${a + b}이고, 십의 자리의 숫자와 일의 자리의 숫자를 바꾼 수는 처음 수보다 ${d}만큼 크다. 처음 수를 구하시오.`, answer: orig,
    explain: `십의 자리 숫자를 x, 일의 자리 숫자를 y라 하면 x+y=${a + b}, 10y+x=(10x+y)+${d}이므로 처음 수는 ${orig}입니다.`, concept: '연립방정식의 활용', difficulty: 4, params: {t: 'wp-digits', s: a + b, d},
    traps: [{v: 10 * b + a, tag: T('solution-order-swapped')}, {v: a + b, tag: T('asked-quantity-misread')}, {v: a, tag: T('one-digit-answered')}, {v: b, tag: T('one-digit-answered')}]}));
}
for (let s = 5; s <= 20; s++) for (const k of [3, 5, 8, 10, 12, 15]) for (const m of [2, 3]) {
  const Mo = m * (s + k) - k; if (Mo - s < 22 || Mo > 58 || (s + k + m) % 2) continue;
  wpB.push(amount({prompt: `현재 어머니와 아들의 나이의 합은 ${Mo + s}살이고, ${k}년 후에는 어머니의 나이가 아들의 나이의 ${m}배가 된다. 현재 아들의 나이는 몇 살인지 구하시오.`, answer: s,
    explain: `어머니 x살, 아들 y살이라 하면 x+y=${Mo + s}, x+${k}=${m}(y+${k})이므로 y=${s}입니다.`, concept: '연립방정식의 활용', difficulty: 4, params: {t: 'wp-ages', S: Mo + s, k, m},
    traps: [{v: s + k, tag: T('future-age-answered')}, {v: ifInt(Mo + s, m + 1), tag: T('time-shift-ignored')}, {v: Mo, tag: T('other-person-answered')}, {v: s - k > 0 ? s - k : null, tag: T('time-shift-reversed')}]}));
}
for (const [v1, v2] of [[3, 6], [4, 8], [3, 9], [4, 6], [2, 6], [4, 12], [5, 10], [3, 12]]) for (let L = 6; L <= 40; L++) for (let x = 2; x < L; x++) {
  const minutes = 60 * x / v1 + 60 * (L - x) / v2; if (!Number.isInteger(minutes) || minutes % 10 || (L * 3 + x * 7 + v1) % 5) continue;
  const h = Math.floor(minutes / 60), mi = minutes % 60, time = `${h ? `${h}시간` : ''}${h && mi ? ' ' : ''}${mi ? `${mi}분` : ''}`;
  wpB.push(amount({prompt: `집에서 ${L} km 떨어진 도서관까지 가는데 처음에는 시속 ${v1} km로 걷다가 도중에 시속 ${v2} km로 뛰어서 모두 ${time}이 걸렸다. 걸어간 거리는 몇 km인지 구하시오.`, answer: x,
    explain: `걸어간 거리 x km, 뛰어간 거리 y km라 하면 x+y=${L}, x÷${v1}+y÷${v2}=${rat(minutes, 60)}이므로 x=${x}입니다.`, concept: '연립방정식의 활용', difficulty: 4, params: {t: 'wp-walk-run', L, v1, v2, minutes},
    traps: [{v: L - x, tag: T('solution-order-swapped')}, {v: ifInt(v1 * minutes, 60), tag: T('one-equation-only')}, {v: [L * v1, v1 + v2], tag: T('distance-split-by-speed')}, {v: ifInt(v2 * minutes, 60), tag: T('one-equation-only')}]}));
}
// I1. (선택) 연립방정식의 해 고르기
const solChoice = [];
for (let a1 = 1; a1 <= 5; a1++) for (let b1 = -4; b1 <= 4; b1++) for (let a2 = 1; a2 <= 5; a2++) for (let b2 = -4; b2 <= 4; b2++) for (const [x0, y0] of [[3, 2], [1, 4], [2, 5], [4, 1], [5, 3], [2, -1], [-1, 3], [6, 2]]) {
  if (a1 <= 0 || !b1 || a2 <= 0 || !b2 || !det(a1, b1, a2, b2) || (a1 * 7 + b1 * 3 + a2 * 11 + b2 * 5 + x0 * 13 + y0) % 17) continue;
  const c1 = a1 * x0 + b1 * y0, c2 = a2 * x0 + b2 * y0; if (gcd(gcd(a1, b1), c1) !== 1 || gcd(gcd(a2, b2), c2) !== 1) continue; const on = (a, b, c) => { for (let dx = 1; dx <= 6; dx++) for (const sgn of [1, -1]) { const x = x0 + sgn * dx, r = c - a * x; if (r % b === 0) return [x, r / b]; } return null; };
  const p1 = on(a1, b1, c1), p2 = on(a2, b2, c2); if (!p1 || !p2) continue;
  solChoice.push(choice({prompt: `연립방정식 ${eq(a1, b1, c1)}, ${eq(a2, b2, c2)}의 해를 고르시오.`, answer: pair(x0, y0),
    distractors: [{v: pair(y0, x0), tag: T('solution-order-swapped')}, {v: pair(...p1), tag: T('one-equation-only')}, {v: pair(...p2), tag: T('one-equation-only')}],
    explain: `${j(`x=${num(x0)}, y=${num(y0)}`, '은')} 두 일차방정식을 모두 만족합니다.`, concept: '연립방정식의 해', difficulty: 1, params: {t: 'system-pair-choice', e1: eq(a1, b1, c1), e2: eq(a2, b2, c2)}}));
}
// I2. (선택) 가감법에서 없앨 미지수에 맞는 식
const elimChoice = [];
for (let a1 = -6; a1 <= 6; a1++) for (let b1 = -6; b1 <= 6; b1++) for (let a2 = -6; a2 <= 6; a2++) for (let b2 = -6; b2 <= 6; b2++) for (const target of ['x', 'y']) {
  if (a1 <= 0 || !b1 || a2 <= 0 || !b2 || !det(a1, b1, a2, b2) || (a1 * 3 + b1 * 5 + a2 * 7 + b2 * 11 + (target === 'x' ? 1 : 0)) % 7) continue;
  const [c1, c2, o1, o2] = target === 'x' ? [a1, a2, b1, b2] : [b1, b2, a1, a2];
  const g = gcd(c1, c2), m1 = Math.abs(c2) / g, m2 = Math.abs(c1) / g; if (m1 === 1 && m2 === 1) continue;
  const op = Math.sign(c1) === Math.sign(c2) ? '−' : '+', other = op === '−' ? '+' : '−';
  const lab = (x, o, y) => `${x === 1 ? '①' : `①×${x}`}${o}${y === 1 ? '②' : `②×${y}`}`;
  const og = gcd(o1, o2), n1 = Math.abs(o2) / og, n2 = Math.abs(o1) / og, oop = Math.sign(o1) === Math.sign(o2) ? '−' : '+';
  const k1 = 2 + ((a1 * 3 + b2) & 7), k2 = 1 + ((b1 * 5 + a2) & 7), e1 = eq(a1, b1, a1 * k1 + b1 * k2), e2 = eq(a2, b2, a2 * k1 + b2 * k2); if (gcd(gcd(a1, b1), a1 * k1 + b1 * k2) !== 1 || gcd(gcd(a2, b2), a2 * k1 + b2 * k2) !== 1) continue;
  elimChoice.push(choice({prompt: `연립방정식 ① ${e1}, ② ${e2}에서 ${j(target, '을')} 없애기 위한 식으로 알맞은 것을 고르시오.`, answer: lab(m1, op, m2),
    distractors: [{v: lab(m1, other, m2), tag: T('elimination-sign-error')}, {v: lab(n1, oop, n2), tag: T('wrong-variable-eliminated')}, {v: lab(m2, op, m1), tag: T('multiplier-mismatch')}, {v: lab(n1, oop === '−' ? '+' : '−', n2), tag: T('elimination-sign-error')}],
    explain: `${target}의 계수 ${num(c1)}, ${num(c2)}의 절댓값을 ${Math.abs(c1 * m1)}로 같게 맞춘 뒤 ${op === '+' ? '부호가 다르므로 더합니다' : '부호가 같으므로 뺍니다'}.`.replace(/(\d+)로 같게/, (_, n) => `${j(n, '으로')} 같게`), concept: '가감법', difficulty: 2, params: {t: 'elim-choice', e1, e2, target}}));
}

const intro = amount({prompt: '연립방정식 x+y=5, x−y=1을 풀 때, x의 값을 구하시오.', answer: 3, explain: '두 식을 더하면 2x=6이므로 x=3입니다.', concept: '가감법', difficulty: 1, params: {t: 'system-solve', e1: 'x+y=5', e2: 'x−y=1', ask: 'x'},
  traps: [{v: 2, tag: T('solution-order-swapped')}, {v: 6, tag: T('coefficient-not-divided')}, {v: 4, tag: T('one-equation-only')}]});
const byT = (pool, t) => pool.filter(q => q && q.params.t === t);
writeUnitPack({id: U, title: '연립일차방정식', unit: U, standards: ['[9수02-13]'], intro, groups: [
  [natCount, 15], [oneSol, 15], [addSub, 30], [solChoice, 25],
  [substi, 30], [general, 45], [elimChoice, 20],
  [hard, 20], [special, 40], [coefFind, 20], [byT(wpA, 'wp-heads-legs'), 15], [byT(wpA, 'wp-two-prices'), 15],
  [coefShared, 15], [abc, 20], [byT(wpB, 'wp-digits'), 15], [byT(wpB, 'wp-ages'), 15], [byT(wpB, 'wp-walk-run'), 15],
]});
