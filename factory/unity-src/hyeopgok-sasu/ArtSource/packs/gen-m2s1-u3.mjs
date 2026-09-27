#!/usr/bin/env node
// 중2-1 단원3 일차부등식 [9수02-11]·[9수02-12].
// A solution set (x>−3) cannot be poured, so pour items ask for a natural number derived from the set:
// 가장 작은 자연수 / 가장 큰 정수(양수일 때) / 자연수·음의 정수 해의 개수 / 활용의 최댓값·최솟값.
// Negative bounds are kept (they are essential to the unit) and are asked through the count of negative
// integer solutions or through choice items (x>−3 vs x<−3 vs x>3 vs x≥−3, 수직선 ●/○).
// 연립일차부등식은 2015 개정 이후 중학교 범위가 아니므로 다루지 않는다.
import {amount, choice, writeUnitPack, j, gcd, reduce, num, rat, dec, M} from './m2s1-lib.mjs';
const U = 'm2s1-u3', T = s => `${U}.${s}`;

// ---- rationals as [n, d] ----
const R = (n, d = 1) => reduce(n, d), rsub = (a, b) => R(a[0] * b[1] - b[0] * a[1], a[1] * b[1]), rdiv = (a, b) => R(a[0] * b[1], a[1] * b[0]);
const rmul = (a, b) => R(a[0] * b[0], a[1] * b[1]);
const FLIP = {'>': '<', '<': '>', '≥': '≤', '≤': '≥'};
// linear a·x+b with rational coefficients, displayed in mode int|dec|frac
function termTxt([n, d], mode, withX, first) {
  if (n === 0) return '';
  const neg = n < 0, an = Math.abs(n); let body;
  if (withX && an === 1 && d === 1) body = 'x';
  else { body = d === 1 ? String(an) : mode === 'dec' ? dec(an, d) : `{frac:${an}/${d}}`; if (withX) body += 'x'; }
  return (neg ? M : first ? '' : '+') + body;
}
function linTxt(a, b, mode = 'int') { const s = termTxt(a, mode, true, true); const t = termTxt(b, mode, false, !s); return (s + t) || '0'; }
function solve(L, rel, Rr) { // L,R: {a,b}; returns {rel, bound}
  const A = rsub(L.a, Rr.a), B = rsub(Rr.b, L.b); if (A[0] === 0) return null;
  const bound = rdiv(B, A); return {rel: A[0] > 0 ? rel : FLIP[rel], bound};
}
const sat = (x, {rel, bound}) => { const c = x * bound[1] - bound[0]; return rel === '>' ? c > 0 : rel === '≥' ? c >= 0 : rel === '<' ? c < 0 : c <= 0; };
const minNatural = s => { for (let x = 1; x <= 500; x++) if (sat(x, s)) return x; return null; };
const maxInteger = s => { for (let x = 500; x >= -500; x--) if (sat(x, s)) return x; return null; };
const solTxt = ({rel, bound}) => `x${rel}${rat(...bound)}`;
const ineqTxt = (L, rel, Rr, mode) => `${linTxt(L.a, L.b, mode)}${rel}${linTxt(Rr.a, Rr.b, mode)}`;
const RELS = ['>', '<', '≥', '≤'];
function lcg(seed) { let s = seed >>> 0; return () => (s = (Math.imul(s, 1664525) + 1013904223) >>> 0) / 2 ** 32; }

// A1. 자연수 범위에서 해의 개수
const countUpto = [], countList = [];
for (let a = 1; a <= 5; a++) for (let b = -9; b <= 9; b++) for (let c = -5; c <= 25; c++) for (const rel of RELS) for (const N of [6, 8, 10, 12]) {
  if (b === 0 || (a * 7 + b * 3 + c + N + rel.charCodeAt(0)) % 11) continue;
  const L = {a: R(a), b: R(b)}, Rr = {a: R(0), b: R(c)}, s = solve(L, rel, Rr), ineq = ineqTxt(L, rel, Rr);
  let cnt = 0; for (let x = 1; x <= N; x++) if (sat(x, s)) cnt++;
  const loose = [...Array(N).keys()].map(i => i + 1).filter(x => sat(x, {rel: {'>': '≥', '≥': '>', '<': '≤', '≤': '<'}[s.rel], bound: s.bound})).length;
  countUpto.push(amount({prompt: `x의 값이 ${N} 이하의 자연수일 때, 부등식 ${ineq}의 해의 개수를 구하시오.`, answer: cnt,
    explain: `부등식을 풀면 ${solTxt(s)}이고, ${N} 이하의 자연수 중 이를 만족하는 수는 ${cnt}개입니다.`, concept: '부등식의 해', difficulty: 1,
    params: {t: 'ineq-count-upto', ineq, N}, traps: [{v: loose, tag: T('boundary-inclusion')}, {v: N - cnt, tag: T('complement-counted')}]}));
}
for (let a = 1; a <= 4; a++) for (let b = -6; b <= 6; b++) for (let c = -8; c <= 10; c++) for (const rel of RELS) for (const [lo, hi] of [[-3, 3], [-2, 4], [-4, 2], [-1, 5]]) {
  if ((a * 5 + b * 7 + c * 3 + lo + rel.charCodeAt(0)) % 9) continue;
  const list = []; for (let x = lo; x <= hi; x++) list.push(x);
  const L = {a: R(a), b: R(b)}, Rr = {a: R(0), b: R(c)}, s = solve(L, rel, Rr), ineq = ineqTxt(L, rel, Rr), cnt = list.filter(x => sat(x, s)).length;
  countList.push(amount({prompt: `x의 값이 ${list.map(num).join(', ')}일 때, 부등식 ${ineq}의 해의 개수를 구하시오.`, answer: cnt,
    explain: `각 값을 대입해 참이 되는 것은 ${list.filter(x => sat(x, s)).map(num).join(', ')}의 ${cnt}개입니다.`, concept: '부등식의 해', difficulty: 1,
    params: {t: 'ineq-count-list', ineq, list}, traps: [{v: list.length - cnt, tag: T('complement-counted')}]}));
}
// B. 일차부등식을 풀어 가장 작은 자연수 / 가장 큰 정수
const solveBasic = [];
for (let a = -6; a <= 7; a++) for (let c = -5; c <= 5; c++) for (let b = -12; b <= 12; b++) for (let d = -15; d <= 30; d++) for (const rel of RELS) {
  if (a === c || a === 0 || b === 0 || (a * 13 + c * 7 + b * 5 + d * 3 + rel.charCodeAt(0)) % 23) continue;
  const L = {a: R(a), b: R(b)}, Rr = {a: R(c), b: R(d)}, s = solve(L, rel, Rr), ineq = ineqTxt(L, rel, Rr);
  const flipNeg = a - c < 0, wrongDir = {rel: flipNeg ? FLIP[s.rel] : s.rel, bound: s.bound};
  if (s.rel === '>' || s.rel === '≥') {
    const ans = minNatural(s); if (!ans || ans < 2) continue;
    solveBasic.push(amount({prompt: `일차부등식 ${j(ineq, '을')} 만족하는 가장 작은 자연수 x를 구하시오.`, answer: ans,
      explain: `${flipNeg ? '양변을 음수로 나누면 부등호의 방향이 바뀌어 ' : ''}${solTxt(s)}이므로 가장 작은 자연수는 ${ans}입니다.`, concept: '일차부등식의 풀이', difficulty: 2,
      params: {t: 'ineq-min-natural', ineq}, traps: [{v: minNatural({rel: s.rel === '>' ? '≥' : '>', bound: s.bound}) ?? -1, tag: T('boundary-inclusion')}, {v: flipNeg ? (maxInteger(wrongDir) ?? -1) : -1, tag: T('sign-not-flipped')}]}));
  } else {
    const ans = maxInteger(s); if (ans === null || ans < 2 || ans > 59) continue;
    solveBasic.push(amount({prompt: `일차부등식 ${j(ineq, '을')} 만족하는 가장 큰 정수 x를 구하시오.`, answer: ans,
      explain: `${flipNeg ? '양변을 음수로 나누면 부등호의 방향이 바뀌어 ' : ''}${solTxt(s)}이므로 가장 큰 정수는 ${ans}입니다.`, concept: '일차부등식의 풀이', difficulty: 2,
      params: {t: 'ineq-max-integer', ineq}, traps: [{v: maxInteger({rel: s.rel === '<' ? '≤' : '<', bound: s.bound}) ?? -1, tag: T('boundary-inclusion')}, {v: flipNeg ? (minNatural(wrongDir) ?? -1) : -1, tag: T('sign-not-flipped')}]}));
  }
}
// C. 괄호가 있는 일차부등식 — 자연수 해의 개수, 음의 정수 해의 개수
const parenCount = [], negCount = [];
for (const a of [2, 3, 4, 5, -2, -3]) for (let b = -6; b <= 6; b++) for (let c = -4; c <= 7; c++) for (let d = -20; d <= 20; d++) for (const rel of RELS) {
  if (b === 0 || a === c || (a * 11 + b * 7 + c * 5 + d * 3 + rel.charCodeAt(0)) % 13) continue;
  const lhs = `${a === -1 ? M : num(a)}(${linTxt(R(1), R(b))})`, rhs = linTxt(R(c), R(d));
  const s = solve({a: R(a), b: R(a * b)}, rel, {a: R(c), b: R(d)}), ineq = `${lhs}${rel}${rhs}`;
  const wrongDir = {rel: a - c < 0 ? FLIP[s.rel] : s.rel, bound: s.bound};
  if (s.rel === '<' || s.rel === '≤') {
    let cnt = 0; for (let x = 1; x <= 500; x++) if (sat(x, s)) cnt++;
    if (cnt >= 2 && cnt <= 59) parenCount.push(amount({prompt: `일차부등식 ${j(ineq, '을')} 만족하는 자연수 x의 개수를 구하시오.`, answer: cnt,
      explain: `괄호를 풀어 정리하면 ${solTxt(s)}이므로 자연수 x는 1부터 ${cnt}까지 ${cnt}개입니다.`, concept: '괄호가 있는 일차부등식', difficulty: 3,
      params: {t: 'ineq-count-natural', ineq}, traps: [{v: [...Array(500).keys()].filter(x => x >= 1 && sat(x, {rel: s.rel === '<' ? '≤' : '<', bound: s.bound})).length, tag: T('boundary-inclusion')}, {v: Math.abs(b * a), tag: T('distribution-partial')}]}));
  } else if (s.bound[0] < 0) {
    let cnt = 0; for (let x = -500; x <= -1; x++) if (sat(x, s)) cnt++;
    if (cnt >= 2 && cnt <= 59) negCount.push(amount({prompt: `일차부등식 ${j(ineq, '을')} 만족하는 음의 정수 x의 개수를 구하시오.`, answer: cnt,
      explain: `괄호를 풀어 정리하면 ${solTxt(s)}이므로 음의 정수는 ${num(-cnt)}부터 −1까지 ${cnt}개입니다.`, concept: '괄호가 있는 일차부등식', difficulty: 4,
      params: {t: 'ineq-count-negative', ineq}, traps: [{v: [...Array(500).keys()].map(x => -1 - x).filter(x => sat(x, {rel: s.rel === '>' ? '≥' : '>', bound: s.bound})).length, tag: T('boundary-inclusion')}, {v: cnt + 1, tag: T('zero-counted-as-negative')}]}));
  }
}
// D. 계수가 소수·분수인 일차부등식
const decimalIneq = [], fracIneq = [];
for (let a = 1; a <= 9; a++) for (let c = -5; c <= 5; c++) for (let b = -30; b <= 30; b += 3) for (let d = -20; d <= 40; d += 4) for (const rel of RELS) {
  if (a === c || (a % 10 === 0) || (a * 7 + c * 11 + b + d + rel.charCodeAt(0)) % 17) continue;
  const L = {a: R(a, 10), b: R(b, 10)}, Rr = {a: R(c, 10), b: R(d, 10)}, s = solve(L, rel, Rr), ineq = ineqTxt(L, rel, Rr, 'dec');
  if (!/\./.test(ineq)) continue;
  const ask = s.rel === '>' || s.rel === '≥' ? 'min' : 'max', ans = ask === 'min' ? minNatural(s) : maxInteger(s);
  if (!ans || ans < 2 || ans > 59) continue;
  decimalIneq.push(amount({prompt: `일차부등식 ${j(ineq, '을')} 만족하는 ${ask === 'min' ? '가장 작은 자연수' : '가장 큰 정수'} x를 구하시오.`, answer: ans,
    explain: `양변에 10을 곱하면 ${ineqTxt({a: R(a), b: R(b)}, rel, {a: R(c), b: R(d)})}이고, ${solTxt(s)}이므로 ${ans}입니다.`, concept: '계수가 소수인 일차부등식', difficulty: 3,
    params: {t: ask === 'min' ? 'ineq-min-natural' : 'ineq-max-integer', ineq}, traps: [{v: ask === 'min' ? (minNatural({rel: {'>': '≥', '≥': '>'}[s.rel], bound: s.bound}) ?? -1) : (maxInteger({rel: {'<': '≤', '≤': '<'}[s.rel], bound: s.bound}) ?? -1), tag: T('boundary-inclusion')}]}));
}
for (const [p, q] of [[2, 3], [3, 4], [2, 5], [4, 6], [3, 2], [5, 3], [6, 4]]) for (let u = 1; u <= 3; u++) for (let wv = 1; wv <= 3; wv++) for (let b = -6; b <= 6; b++) for (let d = -4; d <= 8; d++) for (const rel of RELS) {
  if (u * q === wv * p || b === 0 || (p * 3 + q * 5 + u * 7 + wv + b * 11 + d + rel.charCodeAt(0)) % 19) continue;
  // (u/p)(x+b) rel (w/q)x + d
  const coef = `{frac:${u}/${p}}`, lhs = `${coef}(${linTxt(R(1), R(b))})`, rhs = linTxt(R(wv, q), R(d), 'frac');
  if (u === p) continue;
  const s = solve({a: R(u, p), b: R(u * b, p)}, rel, {a: R(wv, q), b: R(d)}), ineq = `${lhs}${rel}${rhs}`;
  const ask = s.rel === '>' || s.rel === '≥' ? 'min' : 'max', ans = ask === 'min' ? minNatural(s) : maxInteger(s);
  if (!ans || ans < 2 || ans > 59) continue;
  fracIneq.push(amount({prompt: `일차부등식 ${j(ineq, '을')} 만족하는 ${ask === 'min' ? '가장 작은 자연수' : '가장 큰 정수'} x를 구하시오.`, answer: ans,
    explain: `양변에 분모의 최소공배수를 곱해 정리하면 ${solTxt(s)}이므로 ${ans}입니다.`, concept: '계수가 분수인 일차부등식', difficulty: 4,
    params: {t: ask === 'min' ? 'ineq-min-natural' : 'ineq-max-integer', ineq}, traps: [{v: ask === 'min' ? (minNatural({rel: FLIP[s.rel], bound: s.bound}) ?? -1) : (maxInteger({rel: FLIP[s.rel], bound: s.bound}) ?? -1), tag: T('sign-not-flipped')}]}));
}
// E. 일차부등식의 활용
const wp3 = [], wp4 = [];
const GOODS = [['사과', '바구니'], ['배', '상자'], ['복숭아', '상자'], ['장미', '꽃다발 포장지'], ['공책', '가방']];
for (const [item, box] of GOODS) for (const p of [300, 400, 500, 600, 700, 800, 900, 1200, 1500]) for (const b of [1000, 1500, 2000, 2500, 3000]) for (const Tt of [8000, 10000, 12000, 15000, 20000, 25000]) {
  const n = Math.floor((Tt - b) / p); if (n < 2 || n > 59 || (p / 100 + b / 500 + Tt / 1000 + item.length) % 3) continue;
  wp3.push(amount({prompt: `한 개에 ${p}원인 ${j(item, '을')} ${b}원짜리 ${box} 하나에 담아 전체 가격이 ${Tt}원 이하가 되게 하려고 한다. ${j(item, '은')} 최대 몇 개까지 담을 수 있는지 구하시오.`, answer: n,
    explain: `${item} x개라 하면 ${p}x+${b}≤${Tt}, x≤${rat(Tt - b, p)}이므로 최대 ${n}개입니다.`, concept: '일차부등식의 활용', difficulty: 3,
    params: {t: 'wp-budget', p, b, T: Tt}, traps: [{v: Math.ceil((Tt - b) / p), tag: T('round-up-count')}, {v: Math.floor(Tt / p), tag: T('fixed-cost-ignored')}]}));
}
for (const W of [400, 500, 600, 700, 800, 1000]) for (const m of [50, 60, 65, 70, 75, 80]) for (const w of [8, 12, 15, 18, 20, 25, 30]) {
  const n = Math.floor((W - m) / w); if (n < 2 || n > 59 || (W / 100 + m / 5 + w) % 2) continue;
  wp3.push(amount({prompt: `최대 ${W} kg까지 실을 수 있는 엘리베이터에 몸무게가 ${m} kg인 사람이 한 개에 ${w} kg인 상자를 싣고 함께 타려고 한다. 상자는 최대 몇 개까지 실을 수 있는지 구하시오.`, answer: n,
    explain: `상자 x개라 하면 ${m}+${w}x≤${W}, x≤${rat(W - m, w)}이므로 최대 ${n}개입니다.`, concept: '일차부등식의 활용', difficulty: 3,
    params: {t: 'wp-elevator', W, m, w}, traps: [{v: Math.ceil((W - m) / w), tag: T('round-up-count')}, {v: Math.floor(W / w), tag: T('fixed-cost-ignored')}]}));
}
for (let k = 1; k <= 9; k++) for (let P = 20; P <= 200; P += 2) {
  const x = Math.floor((P - 2 * k) / 4); if (x < 2 || x > 59 || (k + P) % 4) continue;
  wp3.push(amount({prompt: `세로의 길이가 가로의 길이보다 ${k} cm 긴 직사각형의 둘레의 길이가 ${P} cm 이하가 되게 하려고 한다. 가로의 길이는 최대 몇 cm인지 구하시오. (단, 가로의 길이는 자연수이다.)`, answer: x,
    explain: `가로를 x cm라 하면 2(x+x+${k})≤${P}, x≤${rat(P - 2 * k, 4)}이므로 최대 ${x} cm입니다.`, concept: '일차부등식의 활용', difficulty: 3,
    params: {t: 'wp-rectangle', k, P}, traps: [{v: Math.floor((P - k) / 2), tag: T('perimeter-halved-wrong')}, {v: Math.floor(P / 4), tag: T('fixed-cost-ignored')}]}));
}
for (const a of [30000, 25000, 20000, 18000, 15000]) for (const b of [5000, 8000, 10000, 12000]) for (const pp of [1000, 1500, 2000, 2500]) for (const q of [3000, 3500, 4000, 5000, 6000]) {
  if (a <= b || q <= pp) continue; const n = Math.floor((a - b) / (q - pp)) + 1; if (n < 2 || n > 59 || (a / 1000 + b / 1000 + pp / 500 + q / 500) % 2) continue;
  wp3.push(amount({prompt: `현재 형의 저금액은 ${a}원, 동생의 저금액은 ${b}원이다. 다음 주부터 매주 형은 ${pp}원씩, 동생은 ${q}원씩 저금할 때, 동생의 저금액이 형의 저금액보다 많아지는 것은 몇 주 후부터인지 구하시오.`, answer: n,
    explain: `x주 후라 하면 ${b}+${q}x>${a}+${pp}x, x>${rat(a - b, q - pp)}이므로 ${n}주 후부터입니다.`, concept: '일차부등식의 활용', difficulty: 3,
    params: {t: 'wp-savings', a, b, p: pp, q}, traps: [{v: Number.isInteger((a - b) / (q - pp)) ? (a - b) / (q - pp) : -1, tag: T('boundary-inclusion')}, {v: Math.ceil((a - b) / q), tag: T('rate-difference-ignored')}]}));
}
for (let s1 = 8; s1 <= 20; s1++) for (let s2 = 6; s2 <= 20; s2++) for (let s3 = 5; s3 <= 20; s3++) for (const Mv of [12, 13, 14, 15, 16, 17]) {
  const need = 4 * Mv - s1 - s2 - s3; if (need < 2 || need > 20 || (s1 * 3 + s2 * 5 + s3 * 7 + Mv) % 29) continue;
  wp3.push(amount({prompt: `20점 만점인 쪽지 시험을 세 번 보았더니 점수가 ${s1}점, ${s2}점, ${s3}점이었다. 네 번의 점수의 평균이 ${Mv}점 이상이 되려면 네 번째 시험에서 몇 점 이상을 받아야 하는지 구하시오.`, answer: need,
    explain: `네 번째 점수를 x점이라 하면 ${j(`(${s1}+${s2}+${s3}+x)÷4`, '은')} ${Mv} 이상이므로 x≥${need}입니다.`, concept: '일차부등식의 활용', difficulty: 3,
    params: {t: 'wp-average', s: [s1, s2, s3], M: Mv}, traps: [{v: 3 * Mv - s1 - s2 - s3 > 0 ? 3 * Mv - s1 - s2 - s3 : -1, tag: T('divisor-miscounted')}, {v: Mv, tag: T('average-as-answer')}]}));
}
for (let S = 12; S <= 180; S++) {
  const mid = Math.ceil(S / 3) - 1, top = mid + 1; if (top < 3 || top > 59 || S % 3 === 0 && S % 2) continue;
  wp3.push(amount({prompt: `연속하는 세 자연수의 합이 ${S}보다 작다. 이러한 세 자연수 중 가장 큰 수가 될 수 있는 가장 큰 자연수를 구하시오.`, answer: top,
    explain: `가운데 수를 x라 하면 (x−1)+x+(x+1)<${S}, x<${rat(S, 3)}이므로 x는 최대 ${mid}, 가장 큰 수는 ${top}입니다.`, concept: '일차부등식의 활용', difficulty: 3,
    params: {t: 'wp-consecutive', S}, traps: [{v: mid, tag: T('middle-number-answered')}, {v: Math.floor(S / 3) + 1 !== top ? Math.floor(S / 3) + 1 : -1, tag: T('boundary-inclusion')}]}));
}
for (const A of [5000, 8000, 10000, 12000, 15000]) for (const B of [15000, 18000, 20000, 25000, 30000]) for (const a of [100, 120, 150, 180, 200, 250, 300]) for (const b of [20, 40, 50, 60, 80, 100]) {
  if (B <= A || a <= b) continue; const bound = (B - A) / (a - b), n = Math.floor(bound) + 1; if (n < 2 || n > 59 || (A / 1000 + B / 1000 + a / 10 + b / 10) % 3) continue;
  wp4.push(amount({prompt: `A 요금제는 기본요금이 ${A}원이고 통화 1분에 ${a}원, B 요금제는 기본요금이 ${B}원이고 통화 1분에 ${b}원이다. B 요금제가 A 요금제보다 저렴하려면 한 달 통화 시간이 최소 몇 분이어야 하는지 구하시오. (단, 통화 시간은 분 단위의 자연수이다.)`, answer: n,
    explain: `x분 통화한다고 하면 ${A}+${a}x>${B}+${b}x, x>${rat(B - A, a - b)}이므로 최소 ${n}분입니다.`, concept: '일차부등식의 활용', difficulty: 4,
    params: {t: 'wp-plans', A, B, a, b}, traps: [{v: Number.isInteger(bound) ? bound : -1, tag: T('boundary-inclusion')}, {v: Math.floor((B - A) / a), tag: T('rate-difference-ignored')}]}));
}
for (const price of [3000, 4000, 5000, 6000, 8000]) for (const G of [20, 25, 30, 40, 50]) for (const r of [10, 20, 25, 30, 40]) {
  const lim = G * (100 - r) / 100, n = Math.floor(lim) + 1; if (n < 2 || n >= G || n > 59 || (price / 1000 + G + r / 5) % 2) continue;
  wp4.push(amount({prompt: `한 사람의 입장료가 ${price}원인 미술관에서 ${G}명 이상의 단체는 입장료의 ${r}%를 할인해 준다. ${G}명 미만인 단체가 ${G}명의 단체 입장권을 사는 것이 유리하려면 최소 몇 명이어야 하는지 구하시오.`, answer: n,
    explain: `x명이라 하면 ${price}x>${price}×${G}×${rat(100 - r, 100)}, x>${rat(G * (100 - r), 100)}이므로 최소 ${n}명입니다.`, concept: '일차부등식의 활용', difficulty: 4,
    params: {t: 'wp-group', price, G, r}, traps: [{v: Number.isInteger(lim) ? lim : -1, tag: T('boundary-inclusion')}, {v: Math.floor(G * r / 100), tag: T('discount-rate-as-ratio')}]}));
}
for (const [u, w] of [[2, 3], [3, 4], [2, 4], [3, 6], [4, 6], [2, 6], [3, 5], [4, 5], [4, 12], [6, 12]]) for (let Tn = 1; Tn <= 12; Tn++) for (const Td of [1, 2]) {
  const Tq = R(Tn, Td), maxd = rdiv(rmul(Tq, R(u * w)), R(u + w)); if (maxd[1] !== 1 || maxd[0] < 2 || maxd[0] > 59 || gcd(Tn, Td) !== 1) continue;
  const timeTxt = Td === 1 ? `${Tn}시간` : `${(Tn - 1) / 2}시간 30분`; if (Td === 2 && Tn < 3) continue;
  wp4.push(amount({prompt: `등산을 하는데 올라갈 때는 시속 ${u} km로, 내려올 때는 같은 길을 시속 ${w} km로 걸어서 ${timeTxt} 이내에 돌아오려고 한다. 최대 몇 km 지점까지 올라갔다 올 수 있는지 구하시오.`, answer: maxd[0],
    explain: `x km 지점까지라 하면 x÷${u}+x÷${w}≤${rat(...Tq)}에서 x≤${maxd[0]}이므로 최대 ${maxd[0]} km입니다.`, concept: '일차부등식의 활용', difficulty: 4,
    params: {t: 'wp-hiking', u, w, Tn, Td}, traps: [{v: Math.floor(Tn / Td * u / 2), tag: T('round-trip-ignored')}, {v: Math.floor(Tn / Td * (u + w) / 2), tag: T('average-speed-misused')}]}));
}
// F1. (선택) 일차부등식의 해 고르기
const solveChoice = [];
for (let a = -7; a <= 7; a++) for (let c = -4; c <= 4; c++) for (let b = -12; b <= 12; b++) for (let d = -12; d <= 12; d++) for (const rel of RELS) {
  if (a === 0 || a === c || (a * 17 + c * 5 + b * 3 + d * 7 + rel.charCodeAt(0)) % 31) continue;
  const L = {a: R(a), b: R(b)}, Rr = {a: R(c), b: R(d)}, s = solve(L, rel, Rr); if (s.bound[0] === 0) continue;
  const neg = a - c < 0; if (!neg && (a + b + d) % 3) continue; // mostly negative-coefficient items
  const ineq = ineqTxt(L, rel, Rr), opp = {'>': '≥', '≥': '>', '<': '≤', '≤': '<'};
  solveChoice.push(choice({prompt: `일차부등식 ${j(ineq, '을')} 푸시오.`, answer: solTxt(s), distractors: [
    {v: solTxt({rel: FLIP[s.rel], bound: s.bound}), tag: neg ? T('sign-not-flipped') : T('flipped-without-negative')},
    {v: solTxt({rel: s.rel, bound: [-s.bound[0], s.bound[1]]}), tag: T('sign-error-in-transposition')},
    {v: solTxt({rel: opp[s.rel], bound: s.bound}), tag: T('boundary-inclusion')}],
    explain: `${neg ? `x의 계수가 음수이므로 양변을 나눌 때 부등호의 방향이 바뀌어 ` : ''}${solTxt(s)}입니다.`, concept: '일차부등식의 풀이', difficulty: neg ? 2 : 1, params: {t: 'ineq-solve-choice', ineq}}));
}
// F2. (선택) 부등식의 성질
const propChoice = [];
for (const base of ['a<b', 'a>b']) for (const k of [-9, -7, -5, -4, -3, -2, -1, 2, 3, 4, 5, 7]) for (const c of [-8, -5, -3, -1, 0, 1, 2, 4, 6, 9]) for (const form of ['k', 'kfrac', 'c-k']) {
  if (form === 'kfrac' && (Math.abs(k) > 5 || Math.abs(k) === 1)) continue;
  const side = v => form === 'k' ? `${k === 1 ? '' : k === -1 ? M : num(k)}${v}${c ? (c > 0 ? '+' : M) + Math.abs(c) : ''}` : form === 'kfrac' ? `${k < 0 ? M : ''}{frac:${v === 'a' ? 1 : 1}/${Math.abs(k)}}${v}${c ? (c > 0 ? '+' : M) + Math.abs(c) : ''}` : `${num(c)}${k < 0 ? '+' : M}${Math.abs(k) === 1 ? '' : Math.abs(k)}${v}`;
  if (form === 'c-k' && c === 0) continue;
  const eff = form === 'c-k' ? -k : k, lt = base === 'a<b', correct = (lt ? eff > 0 : eff < 0) ? '<' : '>';
  propChoice.push(choice({prompt: `${base}일 때, ${side('a')} □ ${side('b')}의 □ 안에 알맞은 부등호를 고르시오.`, answer: correct, distractors: [
    {v: correct === '<' ? '>' : '<', tag: eff < 0 ? T('sign-not-flipped') : T('flipped-without-negative')}, {v: '=', tag: T('equality-confusion')}, {v: '알 수 없다', tag: T('negative-multiplication-indeterminate')}],
    explain: `부등식의 양변에 ${eff < 0 ? '음수를 곱하면 부등호의 방향이 바뀌고' : '양수를 곱하면 부등호의 방향은 그대로이고'}, 같은 수를 더하거나 빼도 방향은 그대로이므로 □ 안에는 ${correct}가 알맞습니다.`, concept: '부등식의 성질', difficulty: 1, params: {t: 'ineq-property-choice', base, left: side('a'), right: side('b')}}));
}
// F3. (선택) 해를 수직선 위에 나타내기
const lineChoice = [];
for (let a = -5; a <= 5; a++) for (let b = -9; b <= 9; b++) for (let c = -12; c <= 12; c++) for (const rel of RELS) {
  if (a === 0 || b === 0 || (a * 7 + b * 3 + c + rel.charCodeAt(0)) % 7) continue;
  const L = {a: R(a), b: R(b)}, Rr = {a: R(0), b: R(c)}, s = solve(L, rel, Rr); if (s.bound[1] !== 1) continue;
  const B = num(s.bound[0]), closed = s.rel.length && (s.rel === '≥' || s.rel === '≤'), right = s.rel === '>' || s.rel === '≥';
  const lab = (cl, rt) => `${B}에 ${cl ? '●' : '○'}, ${rt ? '오른쪽' : '왼쪽'}`, ineq = ineqTxt(L, rel, Rr);
  lineChoice.push(choice({prompt: `일차부등식 ${ineq}의 해를 수직선 위에 나타낼 때, 옳은 것을 고르시오.`, answer: lab(closed, right), distractors: [
    {v: lab(!closed, right), tag: T('open-closed-dot-swapped')}, {v: lab(closed, !right), tag: a < 0 ? T('sign-not-flipped') : T('direction-reversed')}, {v: lab(!closed, !right), tag: T('open-closed-dot-swapped')}],
    explain: `해는 ${solTxt(s)}이므로 ${B}에 ${closed ? '●(포함)' : '○(제외)'}을 찍고 ${right ? '오른쪽' : '왼쪽'}으로 긋습니다.`, concept: '부등식의 해를 수직선에 나타내기', difficulty: 2, params: {t: 'ineq-numberline-choice', ineq}}));
}

const intro = amount({prompt: 'x의 값이 5 이하의 자연수일 때, 부등식 x+1>4의 해의 개수를 구하시오.', answer: 2, explain: 'x>3이므로 4, 5의 2개입니다. 2닢을 붓습니다.', concept: '부등식의 해', difficulty: 1, params: {t: 'ineq-count-upto', ineq: 'x+1>4', N: 5}, intro: true});
const byT = (pool, t) => pool.filter(q => q && q.params.t === t);
if (process.env.DBG) { const {uniq} = await import('./m2s1-lib.mjs'); for (const t of ['wp-budget', 'wp-elevator', 'wp-rectangle', 'wp-savings', 'wp-average', 'wp-consecutive']) console.log(t, uniq(byT(wp3, t)).length); for (const t of ['wp-plans', 'wp-group', 'wp-hiking']) console.log(t, uniq(byT(wp4, t)).length); process.exit(0); }
writeUnitPack({id: U, title: '일차부등식', unit: U, standards: ['[9수02-11]', '[9수02-12]'], intro, groups: [
  [countUpto, 30], [countList, 20], [propChoice, 25],
  [solveBasic, 55], [solveChoice, 40], [lineChoice, 25],
  [parenCount, 35], [decimalIneq, 25], [byT(wp3, 'wp-budget'), 8], [byT(wp3, 'wp-elevator'), 7], [byT(wp3, 'wp-rectangle'), 7], [byT(wp3, 'wp-savings'), 6], [byT(wp3, 'wp-average'), 6], [byT(wp3, 'wp-consecutive'), 6],
  [negCount, 20], [fracIneq, 25], [byT(wp4, 'wp-plans'), 12], [byT(wp4, 'wp-group'), 10], [byT(wp4, 'wp-hiking'), 13],
]});
