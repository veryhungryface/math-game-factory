#!/usr/bin/env node
// 중2-1 단원1 유리수와 순환소수 [9수01-06].
// Decimals come from exact integer long division of p/q, so no 9-repeating decimal (0.49…) can appear
// (교육과정: 유한소수를 순환소수로 나타내는 것은 다루지 않는다). The game font has no combining dot,
// so repeating decimals are shown expanded ("0.2454545…"), with at least 3 visible periods (2 for 4+ digits).
import {amount, fraction, choice, writeUnitPack, rat, j, gcd, reduce} from './m2s1-lib.mjs';
const U = 'm2s1-u1', T = s => `${U}.${s}`;

function expand(p, q) {
  const int = Math.floor(p / q); let r = p % q; const digits = [], seen = new Map();
  while (r !== 0 && !seen.has(r)) { seen.set(r, digits.length); r *= 10; digits.push(Math.floor(r / q)); r %= q; }
  if (r === 0) return {int, pre: digits.join(''), rep: ''};
  const s = seen.get(r); return {int, pre: digits.slice(0, s).join(''), rep: digits.slice(s).join('')};
}
function show({int, pre, rep}) { const reps = rep.length <= 3 ? 3 : 2; let body = pre, k = 0; while (k < reps || body.length < 5) { body += rep; k++; } return `${int}.${body}…`; }
function strip25(q) { while (q % 2 === 0) q /= 2; while (q % 5 === 0) q /= 5; return q; }
const terminates = (p, q) => strip25(reduce(p, q)[1]) === 1;
function factorText(q) { const f = []; let n = q; for (let p = 2; p <= n; p++) { let e = 0; while (n % p === 0) { n /= p; e++; } if (e) f.push(p + (e > 1 ? `<sup>${e}</sup>` : '')); } return f.join('×'); }
const ORD = ['첫째', '둘째', '셋째', '넷째'];
const digitAt = (e, n) => { const s = e.pre.length, L = e.rep.length; return Number(n <= s ? e.pre[n - 1] : e.rep[(n - s - 1) % L]); };
const frac = (p, q) => `{frac:${p}/${q}}`; // raw (possibly unreduced) token for prompts

const fractionsUpTo = (qMax, pMax) => { const out = []; for (let q = 2; q <= qMax; q++) for (let p = 1; p <= pMax(q); p++) if (gcd(p, q) === 1 && !terminates(p, q)) out.push([p, q, expand(p, q)]); return out; };

// A. 순환마디 찾기
const blockPool = [];
for (const [p, q, e] of fractionsUpTo(99, q => 4 * q)) {
  if (e.int > 3 || e.rep.length > 2 || e.pre.length > 2 || e.rep[0] === '0') continue;
  const v = Number(e.rep), shown = show(e);
  blockPool.push(amount({prompt: `순환소수 ${shown}의 순환마디를 구하시오.`, answer: v,
    explain: `${e.pre ? `소수점 아래 ${j(e.pre, '은')} 되풀이되지 않고 ` : ''}${j(e.rep, '이')} 한없이 되풀이되므로 순환마디는 ${e.rep}입니다.`,
    concept: '순환마디', difficulty: 1, params: {t: 'period-block', p, q, shown},
    traps: [{v: Number(e.pre + e.rep), tag: T('nonrepeating-included')}, {v: Number(e.rep + e.rep), tag: T('period-over-extended')}, {v: Number(e.rep.split('').reverse().join('')), tag: T('period-start-misread')}]}));
}
// B. 순환마디를 이루는 숫자의 개수
const lengthPool = [];
for (const [p, q, e] of fractionsUpTo(99, q => q - 1)) {
  const L = e.rep.length; if (L < 2 || L > 6 || e.pre.length > 1) continue;
  lengthPool.push(amount({prompt: `분수 ${j(frac(p, q), '을')} 소수로 나타낼 때, 순환마디를 이루는 숫자의 개수를 구하시오.`, answer: L,
    explain: `${frac(p, q)}=${show(e)}이므로 순환마디는 ${e.rep}, 숫자는 ${L}개입니다.`, concept: '순환소수와 순환마디', difficulty: 2,
    params: {t: 'period-length', p, q}, traps: [{v: L + e.pre.length, tag: T('nonrepeating-included')}, {v: q - 1, tag: T('period-equals-denominator-minus-one')}]}));
}
// C. 소수점 아래 n번째 자리의 숫자
const nthPure = [], nthMixed = [];
for (const [p, q, e] of fractionsUpTo(99, q => q - 1)) {
  const L = e.rep.length, s = e.pre.length; if (L < 2 || L > 6 || s > 1) continue;
  for (const n of [15, 20, 23, 25, 30, 37, 40, 45, 50, 60]) {
    const d = digitAt(e, n), m = n - s, k = Math.floor((m - 1) / L), r = m - L * k;
    const naive = Number(e.rep[(n - 1) % L]);
    const item = amount({prompt: `${frac(p, q)}=${show(e)}일 때, 소수점 아래 ${n}번째 자리의 숫자를 구하시오.`, answer: d,
      explain: s ? `첫째 자리 ${j(e.pre, '을')} 빼면 순환마디 ${j(e.rep, '이')} ${n - s}번째 자리까지 이어지고, ${n - s}=${L}×${k}+${r}이므로 ${r}번째 숫자 ${d}입니다.`
                 : `순환마디 ${e.rep}의 숫자가 ${L}개이고 ${n}=${L}×${k}+${r}이므로 순환마디의 ${r}번째 숫자 ${d}입니다.`,
      concept: '소수점 아래 n번째 자리의 숫자', difficulty: s ? 4 : 3, params: {t: 'nth-digit', p, q, n},
      traps: [{v: naive, tag: T('nonrepeating-ignored')}, {v: Number(e.rep[r % L]), tag: T('remainder-misread')}, {v: Number(e.rep[(r + L - 2) % L]), tag: T('remainder-misread')}]});
    (s ? nthMixed : nthPure).push(item);
  }
}

// D. 곱하여 유한소수가 되게 하는 가장 작은 자연수
const minReduced = [], minUnreduced = [];
for (let q = 6; q <= 240; q++) {
  if (q % 2 && q % 5) continue;
  for (let p = 1; p < q; p++) {
    const [, d] = reduce(p, q), m = strip25(d); if (m < 2 || m > 59) continue;
    const unreduced = gcd(p, q) > 1; if (unreduced && strip25(q) === m) continue; // the trap must bite
    if (unreduced && p > 60) continue;
    const item = amount({prompt: `분수 ${frac(p, q)}에 자연수 a를 곱하여 유한소수가 되게 하려고 한다. a의 값 중 가장 작은 수를 구하시오.`, answer: m,
      explain: unreduced ? `${frac(p, q)}=${rat(p, q)}이고 분모 ${d}=${factorText(d)}에서 2, 5가 아닌 소인수의 곱이 ${m}입니다.` : `분모 ${q}=${factorText(q)}에서 2, 5가 아닌 소인수를 모두 곱한 ${m}입니다.`,
      concept: '유한소수가 되게 하는 수', difficulty: unreduced ? 4 : 3, params: {t: 'min-multiplier', p, q},
      traps: [{v: strip25(q), tag: T('unreduced-denominator')}, {v: q, tag: T('whole-denominator')}]});
    (unreduced ? minUnreduced : minReduced).push(item);
  }
}
// E. 목록에서 유한소수의 개수 (약분 함정 포함)
function lcg(seed) { let s = seed >>> 0; return () => (s = (Math.imul(s, 1664525) + 1013904223) >>> 0) / 2 ** 32; }
const T_red = [], T_trap = [], N_red = [], N_trap = [];
for (const b of [4, 8, 16, 20, 25, 40, 50]) for (let a = 1; a < b; a++) if (gcd(a, b) === 1) { T_red.push([a, b]); for (const k of [3, 7]) if (k * b <= 99) T_trap.push([k * a, k * b]); }
for (const b of [6, 12, 14, 15, 18, 24, 28, 30, 35, 36, 45, 55, 60]) for (let a = 1; a < b; a++) if (gcd(a, b) === 1) { N_red.push([a, b]); if (2 * b <= 99 && b % 2) N_trap.push([2 * a, 2 * b]); }
const countPool = [];
for (let i = 0; i < 800; i++) {
  const rnd = lcg(9001 + i * 7919), pick = arr => arr[Math.floor(rnd() * arr.length)];
  const t = 2 + (i % 5), size = Math.max(t + 1, 5 + (i % 4)), list = [pick(T_trap)];
  while (list.length < t) list.push(rnd() < .3 ? pick(T_trap) : pick(T_red));
  list.push(pick(N_red)); while (list.length < size) list.push(rnd() < .4 ? pick(N_trap) : pick(N_red));
  for (let k = list.length - 1; k > 0; k--) { const r = Math.floor(rnd() * (k + 1)); [list[k], list[r]] = [list[r], list[k]]; }
  if (new Set(list.map(([a, b]) => rat(a, b))).size !== size) continue;
  const count = list.filter(([a, b]) => terminates(a, b)).length;
  const naive = list.filter(([, b]) => strip25(b) === 1).length, loose = list.filter(([, b]) => b % 2 === 0 || b % 5 === 0).length;
  countPool.push(amount({prompt: `다음 중 유한소수로 나타낼 수 있는 분수의 개수를 구하시오.  ${list.map(([a, b]) => frac(a, b)).join(', ')}`, answer: count,
    explain: `약분한 뒤 분모의 소인수가 2나 5뿐인 것은 ${list.filter(([a, b]) => terminates(a, b)).map(([a, b]) => frac(a, b)).join(', ')}의 ${count}개입니다.`,
    concept: '유한소수로 나타낼 수 있는 분수', difficulty: 2, params: {t: 'count-terminating', list},
    traps: [{v: naive, tag: T('unreduced-denominator')}, {v: loose, tag: T('factor-2-or-5-suffices')}]}));
}
// F. 순환소수를 기약분수로
const toFrac = {1: [], 2: [], 3: [], 4: []};
for (let q = 3; q <= 59; q++) for (let p = 1; p <= 59; p++) {
  if (gcd(p, q) !== 1 || terminates(p, q)) continue; const e = expand(p, q), s = e.pre.length, L = e.rep.length;
  if (e.int > 5 || s + L > 3) continue;
  const A = 10 ** (s + L), B = 10 ** s, D = (A - B) * p / q, shown = show(e);
  const diff = s === 0 && L === 1 && e.int === 0 ? 1 : s === 0 && (L === 2 && e.int === 0 || L === 1) ? 2 : s === 1 && L === 1 && e.int === 0 ? 3 : 4;
  const whole = Number(e.int + e.pre + e.rep), nines = Number('9'.repeat(L));
  toFrac[diff].push(fraction({prompt: `순환소수 ${j(shown, '을')} 기약분수로 나타내시오.`, n: p, d: q, accept: 'reduced',
    explain: `x=${j(shown, '이라')} 하면 ${A}x−${B === 1 ? '' : B}x=${D}이므로 x=${frac(D, A - B)}=${rat(p, q)}입니다.`,
    concept: '순환소수를 분수로 나타내기', difficulty: diff, params: {t: 'to-fraction', p, q, shown},
    traps: [{v: [Number(e.rep), nines], tag: T('nonrepeating-ignored')}, {v: [whole, Number('9'.repeat(s + L))], tag: T('all-digits-over-nines')}]}));
}
// G. 10ᵃx−10ᵇx의 값
const shiftPool = {1: [], 2: [], 3: []};
for (let q = 3; q <= 99; q++) for (let p = 1; p <= 6 * q; p++) {
  if (gcd(p, q) !== 1 || terminates(p, q)) continue; const e = expand(p, q), s = e.pre.length, L = e.rep.length;
  if (!((s === 0 && L <= 2) || (s === 1 && L === 1))) continue;
  const A = 10 ** (s + L), B = 10 ** s, V = (A - B) * p / q; if (!Number.isInteger(V)) throw Error('shift');
  const diff = s === 1 ? 3 : L === 1 && e.int === 0 ? 1 : 2, expr = `${A}x−${B === 1 ? '' : B}x`;
  shiftPool[diff].push(amount({prompt: `x=${show(e)}일 때, ${expr}의 값을 구하시오.`, answer: V,
    explain: `${A}x와 ${B === 1 ? '' : B}x의 소수점 아래 부분이 같으므로 빼면 ${Math.floor(A * p / q)}−${Math.floor(B * p / q)}=${V}입니다.`,
    concept: '순환소수를 분수로 나타내기', difficulty: diff, params: {t: 'shift-subtract', p, q, A, B},
    traps: [{v: Math.floor(A * p / q), tag: T('subtraction-skipped')}, {v: Number(e.rep), tag: T('integer-part-ignored')}]}));
}
// H. (선택) 계산 결과가 정수가 되는 식
const CAND = [[10, 1], [100, 1], [1000, 1], [100, 10], [1000, 10], [1000, 100]], shiftChoice = [];
for (let q = 3; q <= 99; q++) for (let p = 1; p < q; p++) {
  if (gcd(p, q) !== 1 || terminates(p, q)) continue; const e = expand(p, q), s = e.pre.length, L = e.rep.length;
  if (s + L > 3 || (s === 0 && L === 1)) continue;
  const A = 10 ** (s + L), B = 10 ** s, label = ([a, b]) => `${a}x−${b === 1 ? '' : b}x`;
  const wrongs = CAND.filter(([a, b]) => ((a - b) * p) % q !== 0).map(([a, b]) => ({v: label([a, b]), tag: b === 1 && s > 0 ? T('nonrepeating-ignored') : Math.round(Math.log10(a / b)) !== L ? T('period-length-miscounted') : T('shift-misaligned')}));
  const shown = show(e);
  shiftChoice.push(choice({prompt: `순환소수 x=${j(shown, '을')} 분수로 나타내려고 한다. 다음 중 계산 결과가 정수가 되는 식을 고르시오.`, answer: label([A, B]),
    distractors: [wrongs[0], wrongs.at(-1), ...wrongs.slice(1, -1)], explain: `${A}x와 ${B === 1 ? '' : B}x는 소수점 아래 부분이 같아 빼면 ${j(String((A - B) * p / q), '이')} 됩니다.`,
    concept: '순환소수를 분수로 나타내기', difficulty: 3, params: {t: 'shift-choice', p, q, shown}}));
}
// I. (선택) 유한소수로 나타낼 수 있는 분수 고르기
const termChoice = [];
{
  const good = [], bad2or5 = [], badNum = [];
  for (const b of [8, 16, 20, 25, 40, 50, 80]) for (let a = 1; a < b; a++) if (gcd(a, b) === 1) good.push([a, b]);
  for (const b of [6, 12, 14, 15, 24, 28, 30, 35, 45, 55, 60, 70]) for (let a = 1; a < b; a++) if (gcd(a, b) === 1) bad2or5.push([a, b]);
  for (const b of [3, 7, 9, 11, 21, 33]) for (const a of [2, 4, 5, 8, 10, 20, 25]) if (a < b && gcd(a, b) === 1) badNum.push([a, b]);
  for (let i = 0; i < 200; i++) {
    const rnd = lcg(424242 + i * 104729), pick = arr => arr[Math.floor(rnd() * arr.length)];
    const ans = pick(good), w = [pick(bad2or5), pick(bad2or5), pick(badNum)];
    const tokens = [ans, ...w].map(([a, b]) => rat(a, b)); if (new Set(tokens).size !== 4) continue;
    const it = choice({prompt: '', answer: rat(...ans), distractors: [{v: rat(...w[0]), tag: T('factor-2-or-5-suffices')}, {v: rat(...w[1]), tag: T('factor-2-or-5-suffices')}, {v: rat(...w[2]), tag: T('numerator-considered')}],
      explain: `${rat(...ans)}의 분모 ${j(`${ans[1]}=${factorText(ans[1])}`, '은')} 소인수가 2나 5뿐입니다.`, concept: '유한소수로 나타낼 수 있는 분수', difficulty: 2, params: {t: 'terminating-choice'}, format: 'frac', answerNumeric: ans[0] / ans[1]});
    if (!it) continue;
    const order = [...tokens].sort((x, y) => (hashS(x) - hashS(y)));
    it.prompt = `분수 ${order.join(', ')} 중에서 유한소수로 나타낼 수 있는 것을 고르시오.`;
    termChoice.push(it);
  }
}
function hashS(s) { let h = 7; for (const c of s) h = (h * 31 + c.codePointAt(0)) % 100003; return h; }
// J. x가 될 수 있는 N 이하의 자연수의 개수
const countMult = [];
for (const N of [30, 40, 50, 60, 80, 99, 100]) for (let q = 6; q <= 120; q++) for (let p = 1; p < q; p++) {
  const [, d] = reduce(p, q), m = strip25(d); if (m < 3 || q % 2 && q % 5) continue;
  const c = Math.floor(N / m), unreduced = gcd(p, q) > 1; if (c < 2 || c > 59 || (unreduced && strip25(q) === m) || (!unreduced && (p * 7 + q + N) % 5)) continue;
  countMult.push(amount({prompt: `분수 ${frac(p, q)}에 자연수 x를 곱하여 유한소수가 되게 할 때, x가 될 수 있는 ${N} 이하의 자연수의 개수를 구하시오.`, answer: c,
    explain: `${unreduced ? `${frac(p, q)}=${rat(p, q)}이므로 ` : ''}x는 ${m}의 배수여야 하고 ${N} 이하에서 ${c}개입니다.`, concept: '유한소수가 되게 하는 수', difficulty: 4,
    params: {t: 'count-multipliers', p, q, N}, traps: [{v: Math.floor(N / strip25(q)), tag: T('unreduced-denominator')}, {v: N - c, tag: T('complement-counted')}]}));
}
// K. (선택) 순환소수와 유리수의 관계
const conceptChoice = [];
for (const [p, q, e] of fractionsUpTo(99, q => 2 * q)) {
  if (e.rep.length > 3 || e.pre.length > 1 || (p * 13 + q) % 7) continue; const shown = show(e);
  conceptChoice.push(choice({prompt: `순환소수 ${shown}에 대한 설명으로 옳은 것을 고르시오.`, answer: '유리수이다',
    distractors: [{v: '유리수가 아니다', tag: T('repeating-not-rational')}, {v: '분수로 나타낼 수 없다', tag: T('repeating-not-rational')}, {v: '유한소수이다', tag: T('repeating-is-finite')}],
    explain: `${shown}=${rat(p, q)}처럼 분수로 나타낼 수 있으므로 유리수입니다.`, concept: '유리수와 순환소수의 관계', difficulty: 1, params: {t: 'concept-rational', p, q, shown}}));
}

const intro = amount({prompt: '순환소수 0.33333…의 순환마디를 구하시오.', answer: 3, explain: '3이 한없이 되풀이되므로 순환마디는 3입니다. 3닢을 붓습니다.', concept: '순환마디', difficulty: 1, params: {t: 'period-block', p: 1, q: 3, shown: '0.33333…'}, intro: true});
writeUnitPack({id: U, title: '유리수와 순환소수', unit: U, standards: ['[9수01-06]'], intro, groups: [
  [blockPool, 52], [conceptChoice, 20], [toFrac[1], 8], [shiftPool[1], 7],
  [lengthPool, 18], [countPool, 25], [toFrac[2], 18], [shiftPool[2], 15], [termChoice, 22],
  [nthPure, 30], [minReduced, 25], [toFrac[3], 22], [shiftPool[3], 10], [shiftChoice, 30],
  [nthMixed, 22], [minUnreduced, 20], [toFrac[4], 22], [countMult, 25],
]});
