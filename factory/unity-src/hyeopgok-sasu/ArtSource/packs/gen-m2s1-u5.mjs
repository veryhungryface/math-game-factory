#!/usr/bin/env node
// 중2-1 단원5 일차함수와 그 그래프 [9수02-14]~[9수02-16].
// Schema v3 (4-choice). Slopes, intercepts, function values and shifts may be negative or fractional
// again (textbook phrasing: 「x절편을 구하시오」「기울기를 구하시오」). Fractions are shown reduced and no
// equivalent fraction is ever offered as a distractor. Wrong choices come from 기울기·절편 혼동, 증가량의 비
// 뒤집기, 부호(이항·대입) 실수, 평행이동 방향 오류.
import {amount, fraction, choice, writeUnitPack, j, gcd, reduce, num, rat, dec, M} from './m2s1-lib.mjs';
const U = 'm2s1-u5', T = s => `${U}.${s}`;

const R = (n, d = 1) => reduce(n, d);
function coefTxt([n, d], withX, first) { if (n === 0) return ''; const neg = n < 0, an = Math.abs(n); let body = d === 1 ? (withX && an === 1 ? '' : String(an)) : `{frac:${an}/${d}}`; if (withX) body += 'x'; return (neg ? M : first ? '' : '+') + body; }
const toR = v => Array.isArray(v) ? R(v[0], v[1]) : R(v, 1);
const lf = (a, b) => { const s = coefTxt(toR(a), true, true); return (s + coefTxt(toR(b), false, !s)) || '0'; };
const fAt = (a, b, x) => a * x + b; // integer slope/intercept helper
const ifInt = (n, d) => d !== 0 && n % d === 0 ? n / d : null;
const digitsOf = (n, d) => Number(dec(n, d).replace('.', '')); // 0.25 → 25 (소수점을 무시하고 읽음)
const QUAD = ['제1사분면', '제2사분면', '제3사분면', '제4사분면'];

// A. 함숫값
const fval = [], fsum = [], finv = [];
for (let a = -6; a <= 6; a++) for (let b = -10; b <= 10; b++) {
  if (!a) continue; const fx = lf(a, b);
  for (let k = -5; k <= 9; k++) { const v = fAt(a, b, k); if (Math.abs(v) >= 2 && Math.abs(v) <= 59 && k) fval.push(amount({neg: true, prompt: `일차함수 f(x)=${fx}에 대하여 f(${num(k)})의 값을 구하시오.`, answer: v, explain: `x에 ${num(k)}를 대입하면 ${num(a)}×${k < 0 ? `(${num(k)})` : k}${b ? (b > 0 ? '+' : M) + Math.abs(b) : ''}=${num(v)}입니다.`.replace(`x에 ${num(k)}를`, `x에 ${j(num(k), '을')}`), concept: '함숫값', difficulty: 1, params: {t: 'fval', fx, k}, traps: [{v: a + k + b, tag: T('substitution-as-addition')}, {v: fAt(a, b, -k), tag: T('sign-error-substitution')}, {v: a * k - b, tag: T('intercept-sign-error')}]})); }
  for (const [p, q] of [[1, 2], [2, 3], [-1, 2], [1, 3], [-2, 4], [0, 5], [3, 4], [-3, 1]]) { const v = fAt(a, b, p) + fAt(a, b, q); if (Math.abs(v) >= 2 && Math.abs(v) <= 59) fsum.push(amount({neg: true, prompt: `일차함수 f(x)=${fx}에서 f(${num(p)})+f(${num(q)})의 값을 구하시오.`, answer: v, explain: `f(${num(p)})=${num(fAt(a, b, p))}, f(${num(q)})=${num(fAt(a, b, q))}이므로 합은 ${num(v)}입니다.`, concept: '함숫값', difficulty: 2, params: {t: 'fval-sum', fx, p, q}, traps: [{v: fAt(a, b, p + q), tag: T('function-additive-assumed')}, {v: a * (p + q), tag: T('intercept-omitted')}, {v: p < 0 || q < 0 ? fAt(a, b, Math.abs(p)) + fAt(a, b, Math.abs(q)) : null, tag: T('sign-error-substitution')}, {v: fAt(a, b, p) + q, tag: T('function-notation-misread')}]})); }
  for (let k = -9; k <= 30; k++) { const m = fAt(a, b, k); if (Math.abs(k) < 2 || Math.abs(m) > 60 || (a * 7 + b * 3 + k) % 4) continue; finv.push(amount({neg: true, prompt: `일차함수 f(x)=${fx}에서 f(a)=${num(m)}일 때, a의 값을 구하시오.`, answer: k, explain: `${lf(a, 0).replace('x', 'a')}${b ? (b > 0 ? '+' : M) + Math.abs(b) : ''}=${num(m)}을 풀면 a=${num(k)}입니다.`.replace(`=${num(m)}을`, `=${j(num(m), '을')}`), concept: '함숫값', difficulty: 2, params: {t: 'fval-inverse', fx, m}, traps: [{v: fAt(a, b, m), tag: T('value-and-input-swapped')}, {v: [m + b, a], tag: T('sign-error-in-transposition')}, {v: Math.abs(a) !== 1 ? m - b : null, tag: T('coefficient-not-divided')}, {v: -k, tag: T('sign-error-in-transposition')}]})); }
}
// B. 함수 판별 — 목록 중 y가 x의 함수인 것의 개수
const STATEMENTS = [
  ['s-plus3', '자연수 x보다 3만큼 큰 수 y', true], ['s-bread', '한 개에 700원인 빵 x개의 값 y원', true], ['s-square', '한 변의 길이가 x cm인 정사각형의 둘레의 길이 y cm', true],
  ['s-mod5', '자연수 x를 5로 나눈 나머지 y', true], ['s-speed', '시속 x km로 3시간 동안 달린 거리 y km', true], ['s-divcount', '자연수 x의 약수의 개수 y', true],
  ['s-area20', '넓이가 20 cm²인 직사각형의 가로의 길이 x cm와 세로의 길이 y cm', true], ['s-day', '하루 중 낮의 길이 x시간과 밤의 길이 y시간', true], ['s-age', 'x살인 학생의 5년 후의 나이 y살', true],
  ['n-divisor', '자연수 x의 약수 y', false], ['n-multiple', '자연수 x의 배수 y', false], ['n-abs', '절댓값이 x인 정수 y (x는 자연수)', false],
  ['n-oddless', '자연수 x보다 작은 홀수 y', false], ['n-perimeter', '둘레의 길이가 x cm인 직사각형의 넓이 y cm²', false], ['n-coprime', '자연수 x와 서로소인 자연수 y', false],
];
const funcCount = [];
{ let seed = 12345; const rnd = () => (seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0) / 2 ** 32; const LAB = ['ㄱ', 'ㄴ', 'ㄷ', 'ㄹ', 'ㅁ'];
  for (let i = 0; i < 600; i++) {
    const pool = [...STATEMENTS]; for (let k = pool.length - 1; k > 0; k--) { const r = Math.floor(rnd() * (k + 1)); [pool[k], pool[r]] = [pool[r], pool[k]]; }
    const five = pool.slice(0, 5), cnt = five.filter(s => s[2]).length; if (cnt < 2 || cnt > 4) continue;
    funcCount.push(amount({prompt: `다음 중 y가 x의 함수인 것의 개수를 구하시오.  ${five.map((s, k) => `${LAB[k]}. ${s[1]}`).join('  ')}`, answer: cnt,
      explain: `x의 값 하나에 y의 값이 오직 하나씩 정해지는 것은 ${five.map((s, k) => s[2] ? LAB[k] : '').filter(Boolean).join(', ')}의 ${cnt}개입니다.`, concept: '함수의 뜻', difficulty: 2, params: {t: 'function-count', ids: five.map(s => s[0])},
      traps: [{v: 5, tag: T('any-relation-is-function')}, {v: 5 - cnt, tag: T('complement-counted')}, {v: five.filter(s => s[2] || s[0] === 'n-divisor' || s[0] === 'n-multiple').length, tag: T('one-to-many-accepted')}, {v: five.filter(s => s[2] && !['s-mod5', 's-divcount', 's-day'].includes(s[0])).length, tag: T('function-needs-formula')}]}));
  }
}
// B2. 일차함수인 것의 개수
const EXPRS = [['y=2x+1', 1], ['y=−3x', 1], ['y={frac:1/2}x−4', 1], ['y=0.5x−1', 1], ['y=5−2x', 1], ['y=x(x+1)−x²', 1], ['y=3x−(x+2)', 1], ['y=x²−x(x−2)', 1], ['y=−{frac:2/3}x+1', 1],
  ['y=5', 0], ['y=x²+1', 0], ['y=2(x−1)−2x', 0], ['y=4−x²', 0], ['y=2x(x−1)', 0], ['y=x(x+3)', 0], ['y=x+2−x', 0]];
const linCount = [];
{ let seed = 777; const rnd = () => (seed = (Math.imul(seed, 1664525) + 1013904223) >>> 0) / 2 ** 32;
  for (let i = 0; i < 600; i++) {
    const pool = [...EXPRS]; for (let k = pool.length - 1; k > 0; k--) { const r = Math.floor(rnd() * (k + 1)); [pool[k], pool[r]] = [pool[r], pool[k]]; }
    const six = pool.slice(0, 6), cnt = six.filter(e => e[1]).length; if (cnt < 2 || cnt > 5 || !six.some(e => /x\(|\(x/.test(e[0]))) continue;
    linCount.push(amount({prompt: `다음 중 y가 x에 대한 일차함수인 것의 개수를 구하시오.  ${six.map(e => e[0]).join(',  ')}`, answer: cnt,
      explain: `식을 정리했을 때 y=ax+b(a≠0) 꼴인 것은 ${six.filter(e => e[1]).map(e => e[0]).join(', ')}의 ${cnt}개입니다.`, concept: '일차함수의 뜻', difficulty: 3, params: {t: 'linear-count', exprs: six.map(e => e[0])},
      traps: [{v: six.filter(e => !/x²/.test(e[0]) && e[0] !== 'y=5').length, tag: T('unsimplified-form-judged')}, {v: six.filter(e => /x/.test(e[0].slice(2))).length, tag: T('constant-function-counted')}, {v: six.filter(e => e[1] && !/[({]/.test(e[0])).length, tag: T('unsimplified-form-judged')}, {v: 6 - cnt, tag: T('complement-counted')}]}));
  }
}
// C. 기울기·절편
const intercepts = [], interceptFrac = [];
for (let a = -9; a <= 9; a++) for (let b = -30; b <= 30; b++) {
  if (!a || !b) continue; const fx = lf(a, b), [xn, xd] = R(-b, a);
  const xTraps = [{v: b, tag: T('intercepts-confused')}, {v: [b, a], tag: T('sign-error-in-transposition')}, {v: [-a, b], tag: T('ratio-reversed')}, {v: -b, tag: T('intercepts-confused')}];
  if (xd === 1 && Math.abs(xn) >= 1 && Math.abs(xn) <= 59) intercepts.push(amount({neg: true, prompt: `일차함수 y=${fx}의 그래프의 x절편을 구하시오.`, answer: xn, explain: `y=0을 대입하면 ${lf(a, 0)}=${num(-b)}이므로 x=${num(xn)}입니다.`, concept: 'x절편', difficulty: 1, params: {t: 'intercept', fx, ask: 'x'}, traps: xTraps}));
  if (xd > 1 && (a + b) % 2 === 0) interceptFrac.push(fraction({neg: true, prompt: `일차함수 y=${fx}의 그래프의 x절편을 구하시오.`, n: xn, d: xd, explain: `y=0을 대입하면 ${lf(a, 0)}=${num(-b)}이므로 x=${rat(xn, xd)}입니다.`, concept: 'x절편', difficulty: 2, params: {t: 'intercept', fx, ask: 'x'}, traps: xTraps}));
  if (Math.abs(b) >= 2 && (a * 5 + b) % 3 === 0) intercepts.push(amount({neg: true, prompt: `일차함수 y=${fx}의 그래프의 y절편을 구하시오.`, answer: b, explain: `x=0을 대입하면 y=${num(b)}이므로 y절편은 ${num(b)}입니다.`, concept: 'y절편', difficulty: 1, params: {t: 'intercept', fx, ask: 'y'}, traps: [{v: [xn, xd], tag: T('intercepts-confused')}, {v: a, tag: T('slope-as-intercept')}, {v: -b, tag: T('sign-error')}]}));
  if (Math.abs(a) >= 2 && (a * 3 + b) % 5 === 0) intercepts.push(amount({neg: true, prompt: `일차함수 y=${fx}의 그래프의 기울기를 구하시오.`, answer: a, explain: `y=ax+b에서 x의 계수 a가 기울기이므로 ${num(a)}입니다.`, concept: '기울기', difficulty: 1, params: {t: 'intercept', fx, ask: 'slope'}, traps: [{v: b, tag: T('slope-as-intercept')}, {v: -a, tag: T('sign-error')}, {v: [xn, xd], tag: T('intercepts-confused')}]}));
}
// D. 증가량
const incr = [];
for (let a = -9; a <= 9; a++) for (let b = -9; b <= 9; b++) for (let m = 2; m <= 8; m++) {
  if (!a || (a * 3 + b + m) % 4) continue; const fx = lf(a, b);
  incr.push(amount({neg: true, prompt: `일차함수 y=${fx}에서 x의 값이 ${m}만큼 증가할 때, y의 값의 증가량을 구하시오.`, answer: a * m, explain: `(y의 값의 증가량)=(기울기)×(x의 값의 증가량)=${a < 0 ? `(${num(a)})` : a}×${m}=${num(a * m)}입니다.`, concept: '기울기', difficulty: 2, params: {t: 'increase', fx, dx: m}, traps: [{v: a + m, tag: T('slope-added')}, {v: fAt(a, b, m), tag: T('function-value-as-increase')}, {v: a, tag: T('slope-as-increase')}, {v: -a * m, tag: T('sign-error')}]}));
}
for (const [dn, dd] of [[1, 2], [2, 3], [3, 4], [3, 2], [5, 2], [4, 3], [2, 5], [1, 3]]) for (let b = -6; b <= 6; b++) for (const [p, q] of [[2, 8], [1, 7], [-2, 4], [0, 12], [3, 15], [-4, 8], [6, 18]]) {
  const inc = dn * (q - p) / dd; if (!Number.isInteger(inc) || inc < 2 || (dn + b + p) % 3) continue; const fx = lf([dn, dd], b);
  incr.push(amount({prompt: `일차함수 y=${fx}에서 x의 값이 ${num(p)}에서 ${q}까지 증가할 때, y의 값의 증가량을 구하시오.`, answer: inc, explain: `x의 값의 증가량은 ${q - p}이므로 y의 값의 증가량은 ${rat(dn, dd)}×${q - p}=${inc}입니다.`, concept: '기울기', difficulty: 2, params: {t: 'increase', fx, dx: q - p, from: p, to: q}, traps: [{v: [dd * (q - p), dn], tag: T('ratio-reversed')}, {v: q - p, tag: T('x-increase-as-answer')}, {v: [dn * q + b * dd, dd], tag: T('function-value-as-increase')}]}));
}
// E. 증가량에서 기울기 구하기
const slopeAmt = [], slopeFrac = [], slopeNegChoice = [];
for (let p = -4; p <= 6; p++) for (let q = p + 1; q <= p + 8; q++) for (let r = -8; r <= 12; r++) for (let s = -8; s <= 40; s++) {
  if (s === r || (p * 7 + q * 5 + r * 3 + s) % 13) continue;
  const [n, d] = R(s - r, q - p), stem = `일차함수의 그래프에서 x의 값이 ${num(p)}에서 ${num(q)}까지 증가할 때, y의 값은 ${num(r)}에서 ${num(s)}까지 ${s > r ? '증가' : '감소'}한다. 이 그래프의 기울기를 구하시오.`;
  const sTraps = [{v: s - r, tag: T('x-increase-ignored')}, {v: [q - p, s - r], tag: T('ratio-reversed')}, {v: [-n, d], tag: s < r ? T('decrease-sign-lost') : T('sign-error')}, {v: q ? [s, q] : null, tag: T('point-used-instead-of-increase')}];
  if (d === 1 && Math.abs(n) >= 2 && Math.abs(n) <= 59) slopeAmt.push(amount({neg: true, prompt: stem, answer: n, explain: `기울기는 (y의 값의 증가량)÷(x의 값의 증가량)=${num(s - r)}÷${q - p}=${num(n)}입니다.`, concept: '기울기', difficulty: 2, params: {t: 'slope-change', p, q, r, s}, traps: sTraps}));
  if (d > 1) slopeFrac.push(fraction({neg: true, prompt: stem, n, d, explain: `기울기는 (y의 값의 증가량)÷(x의 값의 증가량)=${num(s - r)}÷${q - p}=${rat(n, d)}입니다.`, concept: '기울기', difficulty: 3, params: {t: 'slope-change', p, q, r, s}, traps: sTraps}));
}
// F. 평행이동
const shift = [], shiftMatch = [], shiftChoice = [];
for (let a = -5; a <= 6; a++) for (let b = -9; b <= 9; b++) for (let px = -4; px <= 8; px++) {
  if (!a || !b || !px) continue; const k = a * px + b; if (Math.abs(k) < 2 || Math.abs(k) > 59 || (a * 3 + b * 5 + px) % 3) continue;
  shift.push(amount({neg: true, prompt: `일차함수 y=${lf(a, 0)}의 그래프를 y축의 방향으로 ${num(b)}만큼 평행이동한 그래프가 점 (${num(px)}, k)를 지날 때, k의 값을 구하시오.`, answer: k, explain: `평행이동한 그래프의 식은 y=${lf(a, b)}이고 x=${num(px)}을 대입하면 k=${num(k)}입니다.`.replace(`x=${num(px)}을`, j(`x=${num(px)}`, '을')), concept: '일차함수의 그래프의 평행이동', difficulty: 2, params: {t: 'translate-point', slope: a, dy: b, px}, traps: [{v: a * px - b, tag: T('shift-direction-reversed')}, {v: a * (px + b), tag: T('shift-applied-to-x')}, {v: a * px, tag: T('shift-omitted')}]}));
}
for (let a = -6; a <= 6; a++) for (let b = -9; b <= 9; b++) for (let m = -12; m <= 20; m++) {
  if (!a || Math.abs(m) < 2 || (a * 5 + b * 3 + m) % 5) continue; const c = b + m;
  shiftMatch.push(amount({neg: true, prompt: `일차함수 y=${lf(a, b)}의 그래프를 y축의 방향으로 m만큼 평행이동하면 일차함수 y=${lf(a, c)}의 그래프와 겹쳐진다. m의 값을 구하시오.`, answer: m, explain: `y절편이 ${num(b)}에서 ${num(c)}로 바뀌었으므로 m=${num(c)}−(${num(b)})=${num(m)}입니다.`.replace(`${num(c)}로`, j(num(c), '으로')).replace(`−(${num(b)})`, b < 0 ? `−(${num(b)})` : `−${b}`), concept: '일차함수의 그래프의 평행이동', difficulty: 2, params: {t: 'translate-match', f1: lf(a, b), f2: lf(a, c)}, traps: [{v: c + b, tag: T('intercepts-added')}, {v: c, tag: T('final-intercept-as-shift')}, {v: b - c, tag: T('shift-direction-reversed')}]}));
}
for (let a = -6; a <= 7; a++) for (let b = -9; b <= 9; b++) {
  if (!a || !b || a === b || a + b === 0 || Math.abs(a) === 1) continue;
  shiftChoice.push(choice({prompt: `일차함수 y=${lf(a, 0)}의 그래프를 y축의 방향으로 ${num(b)}만큼 평행이동한 그래프의 식을 고르시오.`, answer: `y=${lf(a, b)}`,
    distractors: [{v: `y=${lf(a, -b)}`, tag: T('shift-direction-reversed')}, {v: `y=${lf(b, a)}`, tag: T('slope-intercept-swapped')}, {v: `y=${lf(a + b, 0)}`, tag: T('shift-added-to-slope')}],
    explain: `y축의 방향으로 ${num(b)}만큼 평행이동하면 y절편에 ${num(b)}가 더해져 y=${lf(a, b)}입니다.`.replace(`y절편에 ${num(b)}가`, `y절편에 ${j(num(b), '이')}`), concept: '일차함수의 그래프의 평행이동', difficulty: 1, params: {t: 'translate-choice', slope: a, dy: b}}));
}
// G. 일차함수의 식 구하기
const formula = [];
for (let a = -5; a <= 6; a++) for (let x0 = -6; x0 <= 6; x0++) {
  if (!a || !x0) continue; const b = -a * x0; if (Math.abs(b) < 2) continue;
  formula.push(amount({neg: true, prompt: `기울기가 ${num(a)}이고 x절편이 ${num(x0)}인 일차함수의 그래프의 y절편을 구하시오.`, answer: b, explain: `y=${lf(a, 0)}+b에 x=${num(x0)}, y=0을 대입하면 b=${num(b)}입니다.`, concept: '일차함수의 식 구하기', difficulty: 3, params: {t: 'eq-slope-xint', slope: a, x0}, traps: [{v: a * x0, tag: T('sign-error-in-transposition')}, {v: x0, tag: T('intercepts-confused')}, {v: a, tag: T('slope-as-intercept')}]}));
}
for (let p = -3; p <= 4; p++) for (let q = p + 1; q <= p + 5; q++) for (let fp = -6; fp <= 12; fp++) for (let fq = -6; fq <= 20; fq++) for (const k of [5, 6, 8, 10, -2]) {
  if (fq === fp || (p * 7 + q * 5 + fp * 3 + fq + k) % 19) continue; const [sn, sd] = R(fq - fp, q - p); if (sd !== 1) continue;
  const v = fp + sn * (k - p); if (Math.abs(v) < 2 || Math.abs(v) > 59 || k === p || k === q) continue;
  formula.push(amount({neg: true, prompt: `일차함수 f(x)에서 f(${num(p)})=${num(fp)}, f(${num(q)})=${num(fq)}일 때, f(${num(k)})의 값을 구하시오.`, answer: v, explain: `기울기는 ${num(fq - fp)}÷${q - p}=${num(sn)}이고 f(x)=${lf(sn, fp - sn * p)}이므로 f(${num(k)})=${num(v)}입니다.`, concept: '두 점을 지나는 일차함수의 식', difficulty: 4, params: {t: 'eq-two-values', p, fp, q, fq, k}, traps: [{v: sn * k, tag: T('intercept-omitted')}, {v: fp + sn * k, tag: T('point-misplaced')}, {v: [fp * (fq - fp) + (q - p) * (k - p), fq - fp], tag: T('ratio-reversed')}]}));
}
for (let xi = -6; xi <= 8; xi++) for (let yi = -9; yi <= 12; yi++) for (let px = -4; px <= 10; px++) {
  if (!xi || !yi || !px || px === xi || (xi * 5 + yi * 3 + px) % 7) continue; const [n, d] = R(yi * (xi - px), xi); if (d !== 1 || Math.abs(n) < 2 || Math.abs(n) > 59) continue;
  formula.push(amount({neg: true, prompt: `x절편이 ${num(xi)}, y절편이 ${num(yi)}인 일차함수의 그래프가 점 (${num(px)}, k)를 지날 때, k의 값을 구하시오.`, answer: n, explain: `두 점 (${num(xi)}, 0), ${j(`(0, ${num(yi)})`, '을')} 지나므로 기울기는 ${rat(-yi, xi)}이고, x=${num(px)}일 때 k=${num(n)}입니다.`, concept: '일차함수의 식 구하기', difficulty: 3, params: {t: 'eq-intercepts', xi, yi, px}, traps: [{v: [yi * (xi + px), xi], tag: T('slope-sign-error')}, {v: [yi * yi - xi * px, yi], tag: T('ratio-reversed')}, {v: -n, tag: T('sign-error')}, {v: yi, tag: T('intercept-copied')}]}));
}
// H. 평행·일치
const parallel = [];
for (let s = -6; s <= 8; s++) for (const c of [1, 2, 3, -1, -2]) for (const b1 of [3, -5, 7, -2]) for (const b2 of [-2, 4, 1, -6]) {
  if (!s || b1 === b2) continue; const a = s + c; if (Math.abs(a) < 2 || (s * 3 + c * 5 + b1 + b2) % 3) continue;
  const left = `y=(a${c > 0 ? M : '+'}${Math.abs(c)})x${b1 > 0 ? '+' : M}${Math.abs(b1)}`;
  parallel.push(amount({neg: true, prompt: `두 일차함수 ${left}, y=${lf(s, b2)}의 그래프가 서로 평행할 때, a의 값을 구하시오.`, answer: a, explain: `평행하면 기울기가 같으므로 a${c > 0 ? M : '+'}${Math.abs(c)}=${num(s)}, a=${num(a)}입니다.`, concept: '일차함수의 그래프의 평행', difficulty: 3, params: {t: 'parallel-param', left, right: `y=${lf(s, b2)}`}, traps: [{v: s - c, tag: T('transposition-sign-error')}, {v: s, tag: T('slope-copied')}, {v: b2, tag: T('intercept-matched-instead')}]}));
}
for (let a = -6; a <= 7; a++) for (let b = -9; b <= 9; b++) {
  if (!a || !b || Math.abs(a - b) < 2) continue;
  const f1 = `y=ax${b > 0 ? '+' : M}${Math.abs(b)}`, f2 = `y=${lf(a, 0)}+b`;
  parallel.push(amount({neg: true, prompt: `두 일차함수 ${f1}, ${f2}의 그래프가 일치할 때, a−b의 값을 구하시오.`, answer: a - b, explain: `일치하면 기울기와 y절편이 각각 같으므로 a=${num(a)}, b=${num(b)}이고 a−b=${num(a - b)}입니다.`, concept: '일차함수의 그래프의 일치', difficulty: 4, params: {t: 'coincide-params', f1, f2, combo: 'a−b'}, traps: [{v: a + b, tag: T('asked-quantity-misread')}, {v: b - a, tag: T('subtraction-order-reversed')}, {v: a * b, tag: T('asked-quantity-misread')}]}));
}
// I. 그래프와 좌표축으로 둘러싸인 도형의 넓이
const area = [];
for (let a = -8; a <= 8; a++) for (let b = -24; b <= 24; b++) {
  if (!a || !b) continue; const [xn, xd] = R(-b, a); const [An, Ad] = R(Math.abs(xn * b), 2 * xd); if (Ad !== 1 || An < 2 || An > 59) continue;
  area.push(amount({prompt: `일차함수 y=${lf(a, b)}의 그래프와 x축, y축으로 둘러싸인 도형의 넓이를 구하시오.`, answer: An, explain: `x절편 ${rat(xn, xd)}, y절편 ${num(b)}이므로 넓이는 {frac:1/2}×${rat(Math.abs(xn), xd)}×${Math.abs(b)}=${An}입니다.`, concept: '일차함수의 그래프와 넓이', difficulty: xd === 1 ? 3 : 4, params: {t: 'triangle-area', fx: lf(a, b)}, traps: [{v: 2 * An, tag: T('half-omitted')}, {v: [Math.abs(xn) + Math.abs(b) * xd, xd], tag: T('intercepts-added')}, {v: [An, 2], tag: T('half-applied-twice')}]}));
}
// J. 일차함수의 활용
const wp3 = [], wp4 = [];
for (const L of [20, 24, 25, 30, 36, 40, 45, 50]) for (const [rn, rd] of [[1, 2], [1, 4], [3, 10], [2, 5], [1, 5], [3, 4], [1, 1], [2, 1]]) for (let t = 2; t <= 60; t++) {
  const left = L - rn * t / rd; if (!Number.isInteger(left) || left < 2 || left > 59 || (L + t * 3 + rn * 7) % 7) continue;
  wp3.push(amount({prompt: `길이가 ${L} cm인 양초가 1분에 ${dec(rn, rd)} cm씩 일정하게 탄다. 불을 붙인 지 ${t}분 후에 남은 양초의 길이는 몇 cm인지 구하시오.`, answer: left, explain: `x분 후 남은 길이를 y cm라 하면 y=${L}−${dec(rn, rd)}x이고, x=${t}일 때 y=${left}입니다.`, concept: '일차함수의 활용', difficulty: 3, params: {t: 'wp-candle-length', L, rn, rd, min: t}, decimal: true, traps: [{v: [rn * t, rd], tag: T('burned-length-answered')}, {v: [L * rd + rn * t, rd], tag: T('decrease-as-increase')}, {v: rd > 1 && L - digitsOf(rn, rd) * t > 0 ? L - digitsOf(rn, rd) * t : null, tag: T('decimal-point-ignored')}, {v: L - t, tag: T('rate-ignored')}]}));
  const k = left; if ((L + t) % 3 === 0) wp4.push(amount({prompt: `길이가 ${L} cm인 양초가 1분에 ${dec(rn, rd)} cm씩 일정하게 탄다. 남은 양초의 길이가 ${k} cm가 되는 것은 불을 붙인 지 몇 분 후인지 구하시오.`, answer: t, explain: `y=${L}−${dec(rn, rd)}x에 y=${k}를 대입하면 x=${t}입니다.`.replace(`y=${k}를`, j(`y=${k}`, '을')), concept: '일차함수의 활용', difficulty: 4, params: {t: 'wp-candle-time', L, rn, rd, k}, decimal: true, traps: [{v: [k * rd, rn], tag: T('remaining-used-as-burned')}, {v: [(L + k) * rd, rn], tag: T('sign-error-in-transposition')}, {v: [L * rd, rn], tag: T('remaining-ignored')}, {v: L - k, tag: T('rate-ignored')}]}));
}
for (const V of [30, 40, 50, 60, 80, 100, 120]) for (const r of [2, 3, 4, 5, 6, 8]) for (let t = 2; t <= 30; t++) {
  const left = V - r * t; if (left < 2 || left > 59 || (V + r * 3 + t) % 5) continue;
  wp3.push(amount({prompt: `물이 ${V} L 들어 있는 물통에서 1분에 ${r} L씩 일정하게 물이 빠져나간다. ${t}분 후에 물통에 남은 물의 양은 몇 L인지 구하시오.`, answer: left, explain: `y=${V}−${r}x에 x=${t}를 대입하면 ${left} L입니다.`.replace(`x=${t}를`, j(`x=${t}`, '을')), concept: '일차함수의 활용', difficulty: 3, params: {t: 'wp-tank-left', V, r, min: t}, traps: [{v: r * t, tag: T('burned-length-answered')}, {v: V + r * t, tag: T('decrease-as-increase')}, {v: V - r, tag: T('one-step-only')}]}));
  void 0;
}
for (const [place, unitL] of [['물통', 'L'], ['수조', 'L'], ['물탱크', 'L']]) for (let V = 20; V <= 200; V += 4) for (const r of [2, 3, 4, 5, 6, 8, 10]) {
  if (V % r || V / r < 2 || V / r > 59 || (V / 4 + r + place.length) % 3) continue;
  wp4.push(amount({prompt: `물이 ${V} ${unitL} 들어 있는 ${place}에서 1분에 ${r} ${unitL}씩 일정하게 물이 빠져나간다. ${place}의 물이 모두 빠져나가는 것은 몇 분 후인지 구하시오.`, answer: V / r, explain: `y=${V}−${r}x에서 y=0이 되는 x를 구하면 ${V / r}입니다.`, concept: '일차함수의 활용', difficulty: 4, params: {t: 'wp-tank-empty', V, r}, traps: [{v: V - r, tag: T('one-step-only')}, {v: V, tag: T('intercepts-confused')}, {v: [r, V], tag: T('ratio-reversed')}]}));
}
for (const T0 of [15, 18, 20, 22, 24, 25, 28, 30]) for (const [dn, dd] of [[6, 10], [5, 10], [3, 5]]) for (let hkm = 1; hkm <= 5; hkm++) {
  const drop = 10 * dn * hkm / dd, val = T0 - drop; if (!Number.isInteger(val) || val < -15 || val === 0) continue; // 100 m마다 dn/dd °C → 1 km마다 10·dn/dd °C
  wp4.push(amount({neg: true, prompt: `지면의 기온이 ${T0} °C이고, 지면에서 100 m 높아질 때마다 기온이 ${dec(dn, dd)} °C씩 내려간다. 지면으로부터 높이가 ${hkm} km인 곳의 기온은 몇 °C인지 구하시오.`, answer: val, explain: `${hkm} km는 ${hkm * 1000} m이므로 기온은 ${T0}−${dec(dn, dd)}×${hkm * 10}=${num(val)} °C입니다.`, concept: '일차함수의 활용', difficulty: 4, params: {t: 'wp-temperature', T0, dn, dd, hkm}, decimal: true, traps: [{v: [T0 * dd - dn * hkm, dd], tag: T('unit-conversion-missed')}, {v: drop, tag: T('change-as-answer')}, {v: T0 + drop, tag: T('decrease-as-increase')}]}));
}
for (const L0 of [10, 12, 15, 20, 25]) for (const [st, inc] of [[10, 1], [10, 2], [20, 3], [5, 1], [50, 4]]) for (let w = st; w <= 400; w += st) {
  const len = L0 + inc * w / st; if (len > 59 || (L0 + w / st + inc) % 3) continue;
  wp3.push(amount({prompt: `길이가 ${L0} cm인 용수철에 무게가 ${st} g인 물체를 하나 더 매달 때마다 용수철의 길이가 ${inc} cm씩 늘어난다. 무게가 ${w} g인 물체를 매달았을 때 용수철의 길이는 몇 cm인지 구하시오.`, answer: len, explain: `x g을 매달 때 길이를 y cm라 하면 y=${L0}+${rat(inc, st)}x이므로 ${len} cm입니다.`, concept: '일차함수의 활용', difficulty: 3, params: {t: 'wp-spring', L0, st, inc, w}, traps: [{v: inc * w / st, tag: T('initial-value-omitted')}, {v: L0 + w / st, tag: T('increment-count-only')}, {v: L0 + inc * w, tag: T('per-unit-misread')}]}));
}
// K. (선택) 사분면·x절편
const quadChoice = [], xintChoice = [];
for (let a = -7; a <= 7; a++) for (let b = -12; b <= 12; b++) {
  if (!a || !b || (a * 5 + b * 3) % 3) continue; const fx = lf(a, b);
  const not = a > 0 ? (b > 0 ? 4 : 2) : (b > 0 ? 3 : 1); // 지나지 않는 사분면
  const tagFor = qd => { const flipA = a > 0 ? (b > 0 ? 3 : 1) : (b > 0 ? 4 : 2), flipB = a > 0 ? (b > 0 ? 2 : 4) : (b > 0 ? 1 : 3); return qd === flipA ? T('slope-sign-ignored') : qd === flipB ? T('intercept-sign-ignored') : T('slope-sign-as-position'); };
  quadChoice.push(choice({prompt: `일차함수 y=${fx}의 그래프가 지나지 않는 사분면을 고르시오.`, answer: QUAD[not - 1], distractors: [1, 2, 3, 4].filter(qd => qd !== not).map(qd => ({v: QUAD[qd - 1], tag: tagFor(qd)})),
    explain: `기울기가 ${a > 0 ? '양수' : '음수'}이고 y절편이 ${b > 0 ? '양수' : '음수'}이므로 ${QUAD[not - 1]}을 지나지 않습니다.`, concept: '일차함수의 그래프의 성질', difficulty: 2, params: {t: 'quadrant-choice', fx}}));
  const [xn, xd] = R(-b, a); if (xn < 0 && Math.abs(b) !== Math.abs(xn * 1) && (a + b) % 2 === 0) xintChoice.push(choice({prompt: `일차함수 y=${fx}의 그래프의 x절편을 고르시오.`, answer: rat(xn, xd),
    distractors: [{v: rat(-xn, xd), tag: T('sign-error-in-transposition')}, {v: num(b), tag: T('intercepts-confused')}, {v: rat(xd * (xn < 0 ? -1 : 1), Math.abs(xn)), tag: T('ratio-reversed')}, {v: num(-b), tag: T('intercepts-confused')}],
    explain: `y=0을 대입하면 ${lf(a, 0)}=${num(-b)}이므로 x절편은 ${rat(xn, xd)}입니다.`, concept: 'x절편', difficulty: 1, params: {t: 'intercept-choice', fx}, format: xd === 1 ? 'int' : 'frac', answerNumeric: xn / xd}));
}
for (const q of quadChoice) if (q) q.explain = q.explain.replace(/(제\d사분면)을/, (_, s) => j(s, '을'));

const intro = amount({prompt: '일차함수 f(x)=2x+1에 대하여 f(3)의 값을 구하시오.', answer: 7, explain: 'x에 3을 대입하면 2×3+1=7입니다.', concept: '함숫값', difficulty: 1, params: {t: 'fval', fx: '2x+1', k: 3}, neg: true,
  traps: [{v: 6, tag: T('substitution-as-addition')}, {v: -5, tag: T('sign-error-substitution')}, {v: 5, tag: T('intercept-sign-error')}]});
const byT = (pool, t) => pool.filter(q => q && q.params.t === t);
const byD = (pool, d) => pool.filter(q => q && q.difficulty === d);
writeUnitPack({id: U, title: '일차함수와 그 그래프', unit: U, standards: ['[9수02-14]', '[9수02-15]', '[9수02-16]'], intro, groups: [
  [fval, 30], [intercepts, 45], [shiftChoice, 18], // v3: xintChoice / slopeNegChoice folded into intercepts / slope-change (negatives allowed)
  [fsum, 10], [finv, 10], [funcCount, 15], [interceptFrac, 16], [incr, 15], [slopeAmt, 20], [shift, 12], [shiftMatch, 10], [quadChoice, 18],
  [linCount, 16], [slopeFrac, 20], [byD(formula, 3), 20], [byD(parallel, 3), 15], [byD(area, 3), 12], [byT(wp3, 'wp-candle-length'), 8], [byT(wp3, 'wp-tank-left'), 7], [byT(wp3, 'wp-spring'), 7],
  [byD(formula, 4), 18], [byD(parallel, 4), 12], [byD(area, 4), 8], [byT(wp4, 'wp-candle-time'), 12], [byT(wp4, 'wp-tank-empty'), 10], [byT(wp4, 'wp-temperature'), 12],
]});
