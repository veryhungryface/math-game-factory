#!/usr/bin/env node
// 중2-1 단원6 일차함수와 일차방정식의 관계 [9수02-17]·[9수02-18].
// Pour items: positive intercepts/slopes of ax+by+c=0, coordinates of intersection points built to be
// positive, areas bounded by lines, and coefficient values (a+b, a) that make systems have none/infinitely
// many solutions. Axis-parallel line equations (x=3 vs y=−1) and "해의 개수 ↔ 평행·일치" are choice items,
// because they are statements about the graph rather than numbers.
// Points are written in words ("x좌표가 3이고 y좌표가 −1인 점") where the validator's parenthesis-stripping
// dedupe would otherwise merge distinct prompts.
import {amount, fraction, choice, writeUnitPack, j, gcd, reduce, num, rat, M} from './m2s1-lib.mjs';
const U = 'm2s1-u6', T = s => `${U}.${s}`;
const R = (n, d = 1) => reduce(n, d);

function lin2(a, b, c = 0) { // a·x + b·y + c (c appended as constant term)
  const term = (k, v, first) => { if (k === 0) return ''; const neg = k < 0, ak = Math.abs(k); return (neg ? M : first ? '' : '+') + (v && ak === 1 ? '' : String(ak)) + v; };
  const s = term(a, 'x', true), t = term(b, 'y', !s); return s + t + term(c, '', !(s + t));
}
const eq0 = (a, b, c) => `${lin2(a, b, c)}=0`, eqc = (a, b, c) => `${lin2(a, b)}=${num(c)}`;
const yfx = (m, k) => { const s = m === 0 ? '' : `${m === 1 ? '' : m === -1 ? M : num(m)}x`; return `y=${s}${k === 0 ? (s ? '' : '0') : (k > 0 ? (s ? '+' : '') : M) + Math.abs(k)}`; };
const det = (a1, b1, a2, b2) => a1 * b2 - a2 * b1;
const yForm = (sn, sd, yn, yd) => { const c = sd === 1 ? (sn === 1 ? '' : sn === -1 ? M : num(sn)) : rat(sn, sd); const k = rat(yn, yd); return `y=${c}x${yn === 0 ? '' : yn > 0 ? '+' + k : k}`; };

// A. ax+by+c=0 의 절편·기울기
const eqInt = [], eqSlopeFrac = [];
for (let a = -6; a <= 6; a++) for (let b = -6; b <= 6; b++) for (let c = -30; c <= 30; c++) {
  if (!a || !b || !c || b < 0 && a < 0 || gcd(gcd(a, b), c) !== 1 || (a * 7 + b * 5 + c) % 3) continue;
  const form = (a + b + c) % 2 ? eq0(a, b, c) : eqc(a, b, -c); const [yn, yd] = R(-c, b), [xn, xd] = R(-c, a), [sn, sd] = R(-a, b);
  const E = `일차방정식 ${form}의 그래프의`;
  if (yd === 1 && yn >= 2 && yn <= 59) eqInt.push(amount({prompt: `${E} y절편을 구하시오.`, answer: yn, explain: `y에 대하여 풀면 ${yForm(sn, sd, yn, yd)}이므로 y절편은 ${yn}입니다.`, concept: '일차방정식의 그래프', difficulty: 1, params: {t: 'eq-intercept', eq: form, ask: 'y'}, traps: [{v: Math.abs(c), tag: T('constant-as-intercept')}, {v: xd === 1 ? Math.abs(xn) : -1, tag: T('intercepts-confused')}]}));
  if (xd === 1 && xn >= 2 && xn <= 59) eqInt.push(amount({prompt: `${E} x절편을 구하시오.`, answer: xn, explain: `y=0을 대입하면 ${lin2(a, 0)}=${num(-c)}이므로 x절편은 ${xn}입니다.`, concept: '일차방정식의 그래프', difficulty: 1, params: {t: 'eq-intercept', eq: form, ask: 'x'}, traps: [{v: Math.abs(c), tag: T('constant-as-intercept')}, {v: yd === 1 ? Math.abs(yn) : -1, tag: T('intercepts-confused')}]}));
  if (sn > 0 && sd === 1 && sn >= 2) eqInt.push(amount({prompt: `${E} 기울기를 구하시오.`, answer: sn, explain: `y에 대하여 풀면 x의 계수가 ${sn}이므로 기울기는 ${sn}입니다.`, concept: '일차방정식의 그래프', difficulty: 1, params: {t: 'eq-intercept', eq: form, ask: 'slope'}, traps: [{v: Math.abs(a), tag: T('x-coefficient-as-slope')}]}));
  if (sn > 0 && sd > 1) eqSlopeFrac.push(fraction({prompt: `${E} 기울기를 구하시오.`, n: sn, d: sd, accept: 'equivalent', explain: `${form}을 y에 대하여 풀면 x의 계수가 ${rat(sn, sd)}이므로 기울기는 ${rat(sn, sd)}입니다.`.replace(`${form}을`, j(form, '을')), concept: '일차방정식의 그래프', difficulty: 2, params: {t: 'eq-intercept', eq: form, ask: 'slope'}, traps: [{v: [Math.abs(b), Math.abs(a)], tag: T('ratio-reversed')}, {v: [Math.abs(a), 1], tag: T('x-coefficient-as-slope')}]}));
}
// B. 그래프가 지나는 점
const eqPoint = [];
for (let a = 1; a <= 6; a++) for (let b = -6; b <= 6; b++) for (let k = 2; k <= 30; k++) for (let m = -6; m <= 8; m++) {
  if (!b || !m || (a * 5 + b * 3 + k * 7 + m) % 23) continue; const c = a * k + b * m; if (gcd(gcd(a, b), c) !== 1) continue;
  const e = eqc(a, b, c);
  eqPoint.push(amount({prompt: `일차방정식 ${e}의 그래프가 점 (k, ${num(m)})를 지날 때, k의 값을 구하시오.`.replace(`(k, ${num(m)})를`, j(`(k, ${num(m)})`, '을')), answer: k, explain: `x=k, y=${num(m)}을 대입하면 ${a === 1 ? '' : a}k${b * m >= 0 ? '+' : M}${Math.abs(b * m)}=${num(c)}이므로 k=${k}입니다.`.replace(`y=${num(m)}을`, j(`y=${num(m)}`, '을')), concept: '일차방정식의 그래프', difficulty: 1, params: {t: 'eq-point', eq: e, y0: m}, traps: [{v: Math.abs(c), tag: T('constant-as-coordinate')}]}));
}
// C. 좌표축에 평행한 직선
const rect = [], rectEq = [], axisChoice = [], descChoice = [];
for (let x1 = -6; x1 <= 8; x1++) for (let x2 = x1 + 1; x2 <= 9; x2++) for (let y1 = -6; y1 <= 6; y1++) for (let y2 = y1 + 1; y2 <= 8; y2++) {
  const A = (x2 - x1) * (y2 - y1); if (A < 2 || A > 59 || (x1 * 7 + x2 * 5 + y1 * 3 + y2) % 11) continue;
  rect.push(amount({prompt: `네 직선 x=${num(x1)}, x=${num(x2)}, y=${num(y1)}, y=${num(y2)}로 둘러싸인 도형의 넓이를 구하시오.`, answer: A, explain: `가로 ${x2 - x1}, 세로 ${y2 - y1}인 직사각형이므로 넓이는 ${A}입니다.`, concept: '좌표축에 평행한 직선', difficulty: 2, params: {t: 'rect-area4', xs: [x1, x2], ys: [y1, y2]}, traps: [{v: Math.abs(x2 * y2 - x1 * y1), tag: T('coordinates-multiplied')}, {v: 2 * (x2 - x1 + y2 - y1), tag: T('perimeter-for-area')}]}));
}
for (let px = -9; px <= 9; px++) for (let py = -9; py <= 9; py++) {
  const A = Math.abs(px * py); if (!px || !py || A < 2 || A > 59) continue;
  rect.push(amount({prompt: `두 직선 x=${num(px)}, y=${num(py)}와 x축, y축으로 둘러싸인 도형의 넓이를 구하시오.`.replace(`y=${num(py)}와`, j(`y=${num(py)}`, '과')), answer: A, explain: `가로 ${Math.abs(px)}, 세로 ${Math.abs(py)}인 직사각형이므로 넓이는 ${A}입니다.`, concept: '좌표축에 평행한 직선', difficulty: 2, params: {t: 'rect-area-axes', px, py}, traps: [{v: Math.abs(px) + Math.abs(py), tag: T('coordinates-added')}]}));
}
for (const p of [1, 2, 3, 4, 5]) for (let X = -9; X <= 9; X++) for (const r of [1, 2, 3, 4]) for (let Y = -9; Y <= 9; Y++) {
  const A = Math.abs(X * Y); if (!X || !Y || A < 2 || A > 59 || (p * 3 + X * 5 + r * 7 + Y) % 5) continue;
  const e1 = eq0(p, 0, -p * X), e2 = eq0(0, r, -r * Y); if (p === 1 && r === 1) continue;
  rectEq.push(amount({prompt: `두 일차방정식 ${e1}, ${e2}의 그래프와 x축, y축으로 둘러싸인 도형의 넓이를 구하시오.`, answer: A, explain: `${e1}에서 x=${num(X)}, ${e2}에서 y=${num(Y)}이므로 넓이는 ${Math.abs(X)}×${Math.abs(Y)}=${A}입니다.`, concept: '좌표축에 평행한 직선', difficulty: 3, params: {t: 'rect-area-eqs', e1, e2}, traps: [{v: Math.abs(p * X * r * Y) <= 59 ? Math.abs(p * X * r * Y) : -1, tag: T('coefficient-kept')}, {v: Math.abs(X) + Math.abs(Y), tag: T('coordinates-added')}]}));
}
for (let px = -9; px <= 9; px++) for (let py = -9; py <= 9; py++) for (const axis of ['x', 'y']) {
  if (!px || !py || Math.abs(px) === Math.abs(py)) continue;
  const ans = axis === 'y' ? `x=${num(px)}` : `y=${num(py)}`;
  axisChoice.push(choice({prompt: `x좌표가 ${num(px)}이고 y좌표가 ${num(py)}인 점을 지나고 ${axis}축에 평행한 직선의 방정식을 고르시오.`,
    answer: ans, distractors: [{v: axis === 'y' ? `y=${num(py)}` : `x=${num(px)}`, tag: T('axis-parallel-swapped')}, {v: axis === 'y' ? `x=${num(py)}` : `y=${num(px)}`, tag: T('coordinate-swapped')}, {v: axis === 'y' ? `y=${num(px)}` : `x=${num(py)}`, tag: T('axis-parallel-swapped')}],
    explain: `${axis}축에 평행한 직선 위의 점은 ${axis === 'y' ? 'x' : 'y'}좌표가 모두 ${num(axis === 'y' ? px : py)}이므로 ${ans}입니다.`, concept: '좌표축에 평행한 직선', difficulty: 1, params: {t: 'axis-parallel-choice', px, py, axis}}));
}
for (const v of ['x', 'y']) for (let k = -9; k <= 9; k++) {
  if (!k || Math.abs(k) === 1) continue; const other = v === 'x' ? 'y' : 'x', pt = v === 'x' ? `(0, ${num(k)})` : `(${num(k)}, 0)`;
  descChoice.push(choice({prompt: `방정식 ${v}=${num(k)}의 그래프에 대한 설명으로 옳은 것을 고르시오.`, answer: `${other}축에 평행하다`,
    distractors: [{v: `${v}축에 평행하다`, tag: T('axis-parallel-swapped')}, {v: `점 ${j(pt, '을')} 지난다`, tag: T('coordinate-swapped')}, {v: `기울기가 ${num(k)}이다`, tag: T('equation-as-linear-function')}],
    explain: `${v}=${num(k)}의 그래프 위의 점은 ${v}좌표가 모두 ${num(k)}이므로 ${other}축에 평행한 직선입니다.`, concept: '좌표축에 평행한 직선', difficulty: 1, params: {t: 'line-desc-choice', eqn: `${v}=${num(k)}`}}));
}
// D. 두 그래프의 교점
const inter = [];
for (let m1 = -4; m1 <= 5; m1++) for (let m2 = -4; m2 <= 5; m2++) for (const [x0, y0] of [[2, 3], [3, 7], [4, 2], [5, 9], [1, 6], [6, 4], [2, 11], [7, 5], [3, 13], [8, 3], [4, 17], [9, 10]]) {
  if (m1 === m2 || !m1 || !m2 || (m1 * 5 + m2 * 3 + x0 * 7 + y0) % 3) continue;
  const f1 = yfx(m1, y0 - m1 * x0), f2 = yfx(m2, y0 - m2 * x0);
  for (const ask of ['x', 'y']) { const v = ask === 'x' ? x0 : y0; if (v < 2) continue;
    inter.push(amount({prompt: `두 일차함수 ${f1}, ${f2}의 그래프의 교점의 ${ask}좌표를 구하시오.`, answer: v, explain: `연립방정식 ${f1}, ${f2}를 풀면 x=${x0}, y=${y0}이므로 교점은 (${x0}, ${y0})입니다.`.replace(`${f2}를`, j(f2, '을')), concept: '두 그래프의 교점과 연립방정식', difficulty: 2, params: {t: 'intersection', e1: f1, e2: f2, ask}, traps: [{v: ask === 'x' ? y0 : x0, tag: T('coordinate-swapped')}]})); }
}
for (let a1 = 1; a1 <= 5; a1++) for (let b1 = -4; b1 <= 4; b1++) for (let a2 = 1; a2 <= 5; a2++) for (let b2 = -4; b2 <= 4; b2++) for (const [x0, y0] of [[2, 3], [3, 1], [4, 5], [5, 2], [1, 4], [6, 3], [2, 7], [8, 5], [3, 10]]) {
  if (!b1 || !b2 || !det(a1, b1, a2, b2) || (a1 * 7 + b1 * 5 + a2 * 3 + b2 + x0 * 11 + y0) % 17) continue;
  const c1 = a1 * x0 + b1 * y0, c2 = a2 * x0 + b2 * y0; if (gcd(gcd(a1, b1), c1) !== 1 || gcd(gcd(a2, b2), c2) !== 1) continue;
  const e1 = eqc(a1, b1, c1), e2 = eqc(a2, b2, c2);
  for (const ask of ['x', 'y']) { const v = ask === 'x' ? x0 : y0; if (v < 2) continue;
    inter.push(amount({prompt: `두 일차방정식 ${e1}, ${e2}의 그래프의 교점의 ${ask}좌표를 구하시오.`, answer: v, explain: `두 그래프의 교점의 좌표는 연립방정식의 해와 같으므로 x=${x0}, y=${y0}입니다.`, concept: '두 그래프의 교점과 연립방정식', difficulty: 2, params: {t: 'intersection', e1, e2, ask}, traps: [{v: ask === 'x' ? y0 : x0, tag: T('coordinate-swapped')}]})); }
}
// E. 교점이 주어졌을 때 계수 구하기
const interCoef = [];
for (let a = 1; a <= 8; a++) for (let b = 1; b <= 8; b++) for (const [x0, y0] of [[2, 3], [3, 1], [1, 4], [4, 2], [2, 5], [5, 3], [3, 4], [1, 2], [6, 1]]) for (const [p, r] of [[1, 1], [2, 1], [1, 2], [3, 1]]) {
  if ((a * 5 + b * 3 + x0 * 7 + y0 + p) % 3) continue;
  const e1 = `ax${M}${p === 1 ? '' : p}y=${num(a * x0 - p * y0)}`, e2 = `${r === 1 ? '' : r}x+by=${num(r * x0 + b * y0)}`;
  interCoef.push(amount({prompt: `두 일차방정식 ${e1}, ${e2}의 그래프의 교점의 좌표가 (${x0}, ${y0})일 때, a+b의 값을 구하시오.`, answer: a + b, explain: `${j(`x=${x0}, y=${y0}`, '을')} 두 식에 대입하면 a=${a}, b=${b}입니다.`, concept: '두 그래프의 교점과 연립방정식', difficulty: 3, params: {t: 'intersection-coef', e1, e2, x0, y0}, traps: [{v: x0 + y0, tag: T('coordinates-added')}, {v: a * b, tag: T('asked-quantity-misread')}]}));
}
// F. 세 직선이 한 점에서 만날 때
const three = [];
for (const [[a1, b1], [a2, b2]] of [[[1, 1], [2, -1]], [[1, -1], [1, 2]], [[2, 1], [1, -1]], [[3, 1], [1, 1]], [[1, 2], [2, -3]], [[2, -1], [3, 1]]]) for (const [x0, y0] of [[2, 3], [1, 4], [3, 2], [4, 1], [2, 5], [5, 3], [3, 7], [1, 6]]) for (let a = 2; a <= 9; a++) for (const [b3, c3n] of [[1, 0], [-1, 0], [2, 0], [-2, 0]]) {
  void c3n; if ((a1 + a2 * 3 + x0 * 5 + y0 * 7 + a + b3) % 4) continue;
  const c3 = a * x0 + b3 * y0, e1 = eqc(a1, b1, a1 * x0 + b1 * y0), e2 = eqc(a2, b2, a2 * x0 + b2 * y0), e3 = `ax${b3 > 0 ? '+' : M}${Math.abs(b3) === 1 ? '' : Math.abs(b3)}y=${num(c3)}`;
  three.push(amount({prompt: `세 직선 ${e1}, ${e2}, ${e3}가 한 점에서 만날 때, a의 값을 구하시오.`.replace(`${e3}가`, j(e3, '이')), answer: a, explain: `두 직선 ${e1}, ${e2}의 교점 (${x0}, ${y0})을 셋째 식에 대입하면 a=${a}입니다.`.replace(`(${x0}, ${y0})을`, j(`(${x0}, ${y0})`, '을')), concept: '두 그래프의 교점과 연립방정식', difficulty: 4, params: {t: 'three-lines', e1, e2, e3}, traps: [{v: Math.abs(c3 - b3 * y0 - x0) <= 59 ? Math.abs(c3 - b3 * y0 - x0) : -1, tag: T('substitution-incomplete')}]}));
}
// G. 해의 개수와 두 그래프의 위치 관계
const infinite = [], none = [], countChoice = [];
for (let a = 1; a <= 6; a++) for (const p of [-3, -2, -1, 1, 2, 3]) for (let q = -6; q <= 8; q++) for (const k of [2, 3, 4]) {
  if (!q || gcd(gcd(a, p), q) !== 1 || (a * 3 + p * 5 + q * 7 + k) % 3) continue; const b = k * q; if (a + b < 2 || a + b > 59) continue;
  const e1 = `ax${p > 0 ? '+' : M}${Math.abs(p) === 1 ? '' : Math.abs(p)}y=${num(q)}`, e2 = `${k * a}x${k * p > 0 ? '+' : M}${Math.abs(k * p)}y=b`;
  infinite.push(amount({prompt: `연립방정식 ${e1}, ${e2}의 해가 무수히 많을 때, a+b의 값을 구하시오.`, answer: a + b, explain: `두 그래프가 일치해야 하므로 둘째 식은 첫째 식의 ${k}배입니다. a=${a}, b=${b}입니다.`, concept: '연립방정식의 해의 개수', difficulty: 4, params: {t: 'infinite-solutions', e1, e2}, traps: [{v: a + q <= 59 ? a + q : -1, tag: T('scale-factor-ignored')}]}));
}
for (let k = 2; k <= 4; k++) for (let a = 2; a <= 9; a++) for (const p of [-4, -3, -2, 2, 3, 4, 5]) for (let q = -6; q <= 6; q++) for (let c2 = -9; c2 <= 12; c2++) {
  if (!q || !c2 || c2 === k * q || (k * 7 + a * 3 + p * 5 + q + c2) % 29 || gcd(gcd(a, p), q) !== 1) continue;
  const e1 = `ax${p > 0 ? '+' : M}${Math.abs(p)}y=${num(q)}`, e2 = eqc(k * a, k * p, c2); if (gcd(gcd(k * a, k * p), c2) !== 1) continue;
  none.push(amount({prompt: `연립방정식 ${e1}, ${e2}의 해가 없을 때, a의 값을 구하시오.`, answer: a, explain: `두 그래프가 평행해야 하므로 x, y의 계수의 비가 같고 상수항의 비는 달라야 합니다. a=${a}입니다.`, concept: '연립방정식의 해의 개수', difficulty: 4, params: {t: 'no-solution', e1, e2}, traps: [{v: k * a, tag: T('scale-factor-ignored')}]}));
}
const STMT = {one: '한 점, 해 한 쌍', none: '평행, 해 없음', inf: '일치, 해 무수히 많음', badNone: '평행, 해 무수히 많음', badInf: '일치, 해 없음'};
for (let a1 = 1; a1 <= 5; a1++) for (let b1 = -4; b1 <= 4; b1++) for (let c1 = -6; c1 <= 8; c1++) for (const kind of ['none', 'inf', 'one']) for (const k of [2, 3]) {
  if (!b1 || !c1 || gcd(gcd(a1, b1), c1) !== 1 || (a1 * 5 + b1 * 3 + c1 * 7 + k + kind.length) % 13) continue;
  const a2 = k * a1, b2 = k * b1 + (kind === 'one' ? 1 : 0), c2 = k * c1 + (kind === 'none' ? 1 : 0);
  if (kind === 'one' && !det(a1, b1, a2, b2)) continue;
  const e1 = eqc(a1, b1, c1), e2 = eqc(a2, b2, c2), ans = STMT[kind];
  const wrong = kind === 'none' ? [['badNone', 'solution-count-swapped'], ['inf', 'parallel-coincident-confused'], ['badInf', 'solution-count-swapped']] : kind === 'inf' ? [['badInf', 'solution-count-swapped'], ['none', 'parallel-coincident-confused'], ['badNone', 'solution-count-swapped']] : [['none', 'coefficient-ratio-misread'], ['inf', 'coefficient-ratio-misread'], ['badNone', 'solution-count-swapped']];
  countChoice.push(choice({prompt: `연립방정식 ${e1}, ${e2}의 두 그래프의 위치 관계와 해의 개수로 옳은 것을 고르시오.`, answer: ans, distractors: wrong.map(([w, tag]) => ({v: STMT[w], tag: T(tag)})),
    explain: kind === 'one' ? '기울기가 다르므로 두 그래프는 한 점에서 만나고 해는 한 쌍입니다.' : kind === 'none' ? '기울기는 같고 y절편이 다르므로 평행하고, 교점이 없어 해가 없습니다.' : '둘째 식이 첫째 식의 양변에 같은 수를 곱한 것이므로 두 그래프가 일치하고 해가 무수히 많습니다.', concept: '연립방정식의 해의 개수', difficulty: 3, params: {t: 'solution-count-choice', e1, e2}}));
}
// H. 두 직선과 좌표축으로 둘러싸인 삼각형
const triangles = [];
for (let m1 = 1; m1 <= 4; m1++) for (let m2 = -4; m2 <= -1; m2++) for (let x1 = -8; x1 <= 2; x1++) for (let x2 = x1 + 2; x2 <= 12; x2++) for (const axis of ['x', 'y']) {
  // line1 through (x1,0) slope m1, line2 through (x2,0) slope m2
  const b1 = -m1 * x1, b2 = -m2 * x2; const [xn, xd] = R(b2 - b1, m1 - m2); const [yn, yd] = R(m1 * (b2 - b1) + b1 * (m1 - m2), m1 - m2);
  if ((m1 * 5 + m2 * 3 + x1 * 7 + x2 + axis.length) % 7) continue;
  let A; if (axis === 'x') A = R(Math.abs((x2 - x1) * yn), 2 * yd); else { if (b1 === b2) continue; A = R(Math.abs((b2 - b1) * xn), 2 * xd); }
  if (A[1] !== 1 || A[0] < 2 || A[0] > 59 || (axis === 'y' && xn === 0) || (axis === 'x' && yn === 0)) continue;
  const f1 = yfx(m1, b1), f2 = yfx(m2, b2);
  triangles.push(amount({prompt: `두 직선 ${f1}, ${f2}와 ${axis}축으로 둘러싸인 삼각형의 넓이를 구하시오.`.replace(`${f2}와`, j(f2, '과')), answer: A[0],
    explain: axis === 'x' ? `x절편은 ${num(x1)}, ${num(x2)}이고 교점의 y좌표는 ${rat(yn, yd)}이므로 넓이는 ${A[0]}입니다.` : `y절편은 ${num(b1)}, ${num(b2)}이고 교점의 x좌표는 ${rat(xn, xd)}이므로 넓이는 ${A[0]}입니다.`, concept: '두 그래프와 넓이', difficulty: 4,
    params: {t: 'two-line-area', f1, f2, axis}, traps: [{v: 2 * A[0] <= 59 ? 2 * A[0] : -1, tag: T('half-omitted')}]}));
}
// J. 일차함수의 그래프인 것의 개수
const graphCount = [];
{ let seed = 4242; const rnd = () => (seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0) / 2 ** 32;
  for (let i = 0; i < 700; i++) {
    const list = [], n = 5 + (i % 2); let cnt = 0;
    for (let k = 0; k < n; k++) {
      const kind = rnd(); let a = 1 + Math.floor(rnd() * 5), b = [-3, -2, -1, 1, 2, 3][Math.floor(rnd() * 6)], c = Math.floor(rnd() * 13) - 6;
      if (kind < .45) { cnt++; } else if (kind < .72) b = 0; else a = 0;
      if (!a && !b) continue; if (!a && b < 0) b = -b; if (gcd(gcd(a, b), c) !== 1) continue; const s = rnd() < .5 ? eq0(a, b, c) : eqc(a, b, -c); if (/^=|^0=/.test(s) || s.startsWith('=')) continue; list.push(s);
    }
    if (list.length !== n || new Set(list).size !== n) continue;
    cnt = list.filter(s => { const hasX = /x/.test(s), hasY = /y/.test(s); return hasX && hasY; }).length; if (cnt < 2 || cnt > 5) continue;
    graphCount.push(amount({prompt: `다음 일차방정식의 그래프 중 일차함수의 그래프인 것의 개수를 구하시오.  ${list.join(',  ')}`, answer: cnt,
      explain: `x=p 꼴은 y축에 평행하고 y=q 꼴은 x축에 평행해 일차함수의 그래프가 아닙니다. x, y가 모두 있는 ${cnt}개가 일차함수의 그래프입니다.`, concept: '일차방정식과 일차함수', difficulty: 3, params: {t: 'linear-graph-count', eqs: list},
      traps: [{v: list.length, tag: T('any-line-is-linear-function')}, {v: list.filter(s => /y/.test(s)).length, tag: T('horizontal-line-counted')}]}));
  }
}

const intro = amount({prompt: '일차방정식 x+y−3=0의 그래프의 y절편을 구하시오.', answer: 3, explain: 'y에 대하여 풀면 y=−x+3이므로 y절편은 3입니다. 3닢을 붓습니다.', concept: '일차방정식의 그래프', difficulty: 1, params: {t: 'eq-intercept', eq: 'x+y−3=0', ask: 'y'}, intro: true});
const byT = (pool, t) => pool.filter(q => q && q.params.t === t);
writeUnitPack({id: U, title: '일차함수와 일차방정식의 관계', unit: U, standards: ['[9수02-17]', '[9수02-18]'], intro, groups: [
  [eqInt, 40], [eqPoint, 25], [axisChoice, 20], [descChoice, 15],
  [byT(rect, 'rect-area4'), 15], [byT(rect, 'rect-area-axes'), 12], [byT(inter, 'intersection').filter(q => q.prompt.includes('일차함수')), 25], [byT(inter, 'intersection').filter(q => q.prompt.includes('일차방정식')), 25], [eqSlopeFrac, 18],
  [rectEq, 15], [interCoef, 25], [countChoice, 22], [graphCount, 20],
  [three, 20], [infinite, 18], [none, 15], [triangles, 25],
]});
