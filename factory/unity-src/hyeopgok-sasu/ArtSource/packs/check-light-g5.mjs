#!/usr/bin/env node
// 협곡 사수 — 초5 경량 팩 12개(g5s1-u1~u6, g5s2-u1~u6) 가벼운 검산 (SPEC-LIGHT-PACKS.md).
// 생성기를 가져오지 않는다. 정답은 (1) 발문 속 식을 직접 파싱해 정수·분수로 다시 계산하거나
// (2) 아래 ORACLE 표에 따로 적은 계산으로 다시 구해 팩의 answer 와 대조한다. 부동소수점 비교 없음.
// 사용법: node check-light-g5.mjs   (0 오류면 종료코드 0)
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// 2026-09-28: 초등 팩 발문을 교과서 해요체로 통일했다. 검산 패턴은 하시오체 기준이라 읽을 때만 되돌린다.
const __toHasio = (s) => typeof s === 'string' ? s.replaceAll('구해 보세요','구하시오').replaceAll('계산해 보세요','계산하시오').replaceAll('나타내어 보세요','나타내시오').replaceAll('찾아보세요','고르시오') : s;
const __normPack = (p) => { for (const it of (p.items||[])) it.prompt = __toHasio(it.prompt); return p; };


const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../../..');
const PACK_DIR = path.join(ROOT, 'public/g/hyeopgok-sasu/packs');
const IDS = ['g5s1-u1', 'g5s1-u2', 'g5s1-u3', 'g5s1-u4', 'g5s1-u5', 'g5s1-u6', 'g5s2-u1', 'g5s2-u2', 'g5s2-u3', 'g5s2-u4', 'g5s2-u5', 'g5s2-u6'];
const CURRICULUM = JSON.parse(fs.readFileSync(path.join(ROOT, 'curriculum/2022-elementary-math.json'), 'utf8'));
const STD_CODES = new Set(CURRICULUM.standards.map(s => s.code));

// ---------- 유리수 (정수 쌍) ----------
const gcd = (a, b) => { a = Math.abs(a); b = Math.abs(b); while (b) [a, b] = [b, a % b]; return a; };
const lcm = (a, b) => a / gcd(a, b) * b;
const Q = (n, d = 1) => { if (d === 0) throw new Error('분모 0'); if (d < 0) { n = -n; d = -d; } const g = gcd(n, d) || 1; return { n: n / g, d: d / g }; };
const add = (a, b) => Q(a.n * b.d + b.n * a.d, a.d * b.d);
const sub = (a, b) => Q(a.n * b.d - b.n * a.d, a.d * b.d);
const mul = (a, b) => Q(a.n * b.n, a.d * b.d);
const div = (a, b) => Q(a.n * b.d, a.d * b.n);
const eq = (a, b) => a.n === b.n && a.d === b.d;
const cmp = (a, b) => a.n * b.d - b.n * a.d;
const decQ = s => { const [i, f = ''] = s.split('.'); return Q(Number(i + f), 10 ** f.length); };
const show = q => (q.d === 1 ? `${q.n}` : `${q.n}/${q.d}`);

// ---------- 식 계산기: 정수·소수·{frac:a/b}·대분수 k{frac:a/b}, + - × ÷ ( ) ----------
function E(src) {
  const toks = []; let s = src.replace(/\s+/g, '');
  while (s.length) {
    let m;
    if ((m = /^(\d+)\{frac:(\d+)\/(\d+)\}/.exec(s))) toks.push(add(Q(+m[1]), Q(+m[2], +m[3])));
    else if ((m = /^\{frac:(\d+)\/(\d+)\}/.exec(s))) toks.push(Q(+m[1], +m[2]));
    else if ((m = /^\d+(\.\d+)?/.exec(s))) toks.push(decQ(m[0]));
    else if ((m = /^[-+×÷()]/.exec(s))) toks.push(m[0]);
    else throw new Error(`식 해석 실패: ${src}`);
    s = s.slice(m[0].length);
  }
  let i = 0;
  const atom = () => { const t = toks[i++]; if (t === '(') { const v = sum(); if (toks[i++] !== ')') throw new Error('괄호'); return v; } if (typeof t === 'object') return t; throw new Error(`식 해석 실패: ${src}`); };
  const prod = () => { let v = atom(); while (toks[i] === '×' || toks[i] === '÷') { const op = toks[i++]; const r = atom(); v = op === '×' ? mul(v, r) : div(v, r); } return v; };
  const sum = () => { let v = prod(); while (toks[i] === '+' || toks[i] === '-') { const op = toks[i++]; const r = prod(); v = op === '+' ? add(v, r) : sub(v, r); if (v.n < 0) throw new Error(`중간 결과 음수: ${src}`); } return v; };
  const v = sum(); if (i !== toks.length) throw new Error(`식 해석 실패: ${src}`); return v;
}

// ---------- 보기 값 해석 (단위 분리) ----------
const UNITS = ['cm²', 'm²', 'km²', 'cm', 'km', 'm', 'kg', 'L', '°', '개', '명', '살', '대', '일', '번', '원', '장', '권', '회', '점', '쪽', '상자', '자루', '분', '쌍', '봉지'];
function parseVal(str) {
  let t = str.trim(), unit = '';
  for (const u of UNITS) { if (t.endsWith(u)) { const head = t.slice(0, -u.length).trim(); if (/[\d}]$/.test(head)) { unit = u; t = head; break; } } }
  let m;
  if ((m = /^(\d+)\{frac:(\d+)\/(\d+)\}$/.exec(t))) return { q: add(Q(+m[1]), Q(+m[2], +m[3])), unit, kind: 'mixed', w: +m[1], fn: +m[2], fd: +m[3] };
  if ((m = /^\{frac:(\d+)\/(\d+)\}$/.exec(t))) return { q: Q(+m[1], +m[2]), unit, kind: 'frac', fn: +m[1], fd: +m[2] };
  if (/^\d+(\.\d+)?$/.test(t)) return { q: decQ(t), unit, kind: t.includes('.') ? 'dec' : 'int' };
  return null;
}

// ---------- 수학 도우미 ----------
const divisors = n => Array.from({ length: n }, (_, i) => i + 1).filter(k => n % k === 0);
const range = (a, b) => Array.from({ length: b - a + 1 }, (_, i) => a + i);
const mean = xs => Q(xs.reduce((s, x) => s + x, 0), xs.length);
function roundQ(x, unit, mode) { // x, unit: 유리수. 결과 = unit × (floor|ceil|halfUp)(x / unit)
  const r = div(x, unit); const fl = Math.floor(r.n / r.d);
  const k = mode === '버림' ? fl : mode === '올림' ? (r.n % r.d === 0 ? fl : fl + 1) : Math.floor((2 * r.n + r.d) / (2 * r.d));
  return mul(Q(k), unit);
}
const PLACE = { '일의 자리': Q(1), '십의 자리': Q(10), '백의 자리': Q(100), '천의 자리': Q(1000), '소수 첫째 자리': Q(1, 10), '소수 둘째 자리': Q(1, 100) };
const RULE_SUB = (expr, env) => expr.replace(/[○△□☆]/g, c => `(${env[c]})`);
function relHolds(choice, x, y, f) { // "L = R" 가 (x → y=f(x)) 관계와 모든 표본에서 맞는가
  const [L, R] = choice.split('=').map(s => s.trim());
  return range(1, 12).every(v => { const env = { [x]: f(v), [y]: v }; try { return eq(E(RULE_SUB(L, env)), E(RULE_SUB(R, env))); } catch { return false; } });
}

// ---------- 정답 오라클 ----------
// 반환: 유리수 {n,d} | {q, unit} | 문자열(정확히 일치) | {pick: fn(choice)=>bool} (보기 중 정확히 하나가 조건 만족) | {max:true}/{odd:true}
const V = (q, unit = '') => ({ q: typeof q === 'number' ? Q(q) : q, unit });
const I = (n, unit = '') => V(Q(n), unit);
const pick = fn => ({ pick: fn });
const num = c => parseVal(c)?.q;
const O = {
  // 5-1-1 자연수의 혼합 계산 (계산하시오 문항은 발문에서 자동 계산)
  'g5s1-u1-013': I(30 - 3 * 4, '개'), 'g5s1-u1-014': I(6 * 5 / 3, '개'), 'g5s1-u1-015': I(1000 - 300 * 2, '원'), 'g5s1-u1-016': I(100 - (20 + 15), '장'),
  'g5s1-u1-017': '곱셈', 'g5s1-u1-018': '( ) 안의 계산', 'g5s1-u1-026': I(800 * 2 + 300 * 3, '원'), 'g5s1-u1-027': I(48 / 6 - 3, '상자'),
  'g5s1-u1-028': { odd: true }, 'g5s1-u1-030': Q(range(0, 100).find(x => x * 4 + 6 === 30)),
  // 5-1-2 약수와 배수
  'g5s1-u2-001': divisors(8).join(', '), 'g5s1-u2-002': [1, 2, 3].map(k => 6 * k).join(', '), 'g5s1-u2-003': I(divisors(12).length, '개'),
  'g5s1-u2-004': Q(Math.max(...divisors(15))), 'g5s1-u2-005': Q(7), 'g5s1-u2-006': pick(c => +c % 4 === 0), 'g5s1-u2-007': pick(c => 36 % +c !== 0),
  'g5s1-u2-008': pick(c => +c % 9 !== 0), 'g5s1-u2-009': divisors(18).filter(k => 12 % k === 0).join(', '), 'g5s1-u2-010': Q(gcd(8, 12)), 'g5s1-u2-011': Q(lcm(4, 6)),
  'g5s1-u2-012': [1, 2, 3].map(k => lcm(4, 6) * k).join(', '), 'g5s1-u2-013': Q(gcd(10, 15)), 'g5s1-u2-014': Q(lcm(6, 9)),
  'g5s1-u2-015': Q(range(1, 100).find(n => divisors(n).join() === '1,2,3,6')), 'g5s1-u2-016': Q(gcd(12, 16)), 'g5s1-u2-017': Q(lcm(3, 5)),
  'g5s1-u2-018': '1은 모든 수의 약수입니다.', 'g5s1-u2-019': I(divisors(20).filter(k => k % 2 === 0).length, '개'), 'g5s1-u2-020': '최대공약수',
  'g5s1-u2-021': Q(gcd(24, 36)), 'g5s1-u2-022': Q(lcm(8, 12)), 'g5s1-u2-023': I(lcm(4, 6), '일'), 'g5s1-u2-024': I(gcd(12, 18), '명'),
  'g5s1-u2-025': I(divisors(30).length, '개'), 'g5s1-u2-026': I(range(1, 49).filter(n => n % 8 === 0).length, '개'), 'g5s1-u2-027': divisors(6).join(', '),
  'g5s1-u2-028': pick(c => +c % 3 === 0 && +c % 4 === 0), 'g5s1-u2-029': I(range(1, 49).filter(n => n % 6 === 0 && n % 8 === 0).length, '개'), 'g5s1-u2-030': I(gcd(12, 18), 'cm'),
  // 5-1-3 대응 관계 (식 고르기는 관계식을 표본 12개에 대입해 참인 보기가 정확히 하나인지)
  'g5s1-u3-001': I(5 * 4, '개'), 'g5s1-u3-002': Q(4 * (9 / 3)), 'g5s1-u3-003': I(9 + 3, '살'), 'g5s1-u3-004': pick(c => relHolds(c, '△', '○', v => v + 5)),
  'g5s1-u3-005': Q(5 * 6), 'g5s1-u3-006': Q(7 + 8), 'g5s1-u3-007': Q(18 / 2), 'g5s1-u3-008': pick(c => relHolds(c, '△', '○', v => v * 7)),
  'g5s1-u3-009': I(21 / 3, '대'), 'g5s1-u3-010': I(8 * 3, '개'), 'g5s1-u3-011': Q(range(0, 100).find(x => x - 4 === 10)), 'g5s1-u3-012': Q(6 * (20 / 4)),
  'g5s1-u3-013': pick(c => relHolds(c, '☆', '□', v => v * 4)), 'g5s1-u3-014': pick(c => relHolds(c, '△', '○', v => v + 4)),
  'g5s1-u3-015': pick(c => relHolds(c, '△', '○', v => v * 3)), 'g5s1-u3-016': pick(c => relHolds(c, '△', '○', v => v + 5)),
  'g5s1-u3-017': I(6 * 4, '명'), 'g5s1-u3-018': I(8 * 5, 'L'), 'g5s1-u3-019': Q(range(0, 100).find(x => x * (12 / 3) === 36)),
  'g5s1-u3-020': pick(c => relHolds(c, '△', '○', v => v + 1)), 'g5s1-u3-021': I(7 + 1, '개'), 'g5s1-u3-022': pick(c => relHolds(c, '△', '○', v => v * 4)),
  'g5s1-u3-023': pick(c => relHolds(c, '△', '○', v => v - 1)), 'g5s1-u3-024': Q(45 / 5), 'g5s1-u3-025': Q(27 / 3), 'g5s1-u3-026': I(54 / 6, '일'),
  'g5s1-u3-027': pick(c => relHolds(c, '△', '○', v => v / 2)), 'g5s1-u3-028': Q(20 - 7), 'g5s1-u3-029': Q(6 * 8 - 5 * 8), 'g5s1-u3-030': Q(48 / 4 - 36 / 4),
  // 5-1-4 약분과 통분
  'g5s1-u4-001': pick(c => eq(num(c), Q(1, 2))), 'g5s1-u4-005': `{frac:${2 * 3}/${3 * 3}}`, 'g5s1-u4-007': pick(c => { const p = parseVal(c); return gcd(p.fn, p.fd) === 1; }),
  'g5s1-u4-008': `{frac:${1 * 4}/${3 * 4}}, {frac:${1 * 3}/${4 * 3}}`, 'g5s1-u4-009': Q(7, 10), 'g5s1-u4-012': pick(c => eq(num(c), decQ('0.5'))),
  'g5s1-u4-014': `{frac:${8 / 4}/${12 / 4}}`, 'g5s1-u4-015': '통분', 'g5s1-u4-016': '약분', 'g5s1-u4-017': pick(c => eq(num(c), Q(2, 7))), 'g5s1-u4-018': { max: true },
  'g5s1-u4-019': `{frac:${1 * (12 / 4)}/12}, {frac:${1 * (12 / 6)}/12}`, 'g5s1-u4-021': Q(lcm(6, 9)), 'g5s1-u4-022': `{frac:${3 * 3}/12}, {frac:${5 * 2}/12}`,
  'g5s1-u4-023': decQ('0.35'), 'g5s1-u4-024': `{frac:7/10} ${cmp(Q(7, 10), decQ('0.65')) > 0 ? '>' : cmp(Q(7, 10), decQ('0.65')) < 0 ? '<' : '='} 0.65`, 'g5s1-u4-025': { max: true },
  'g5s1-u4-026': cmp(Q(2, 3), Q(5, 8)) > 0 ? '{frac:2/3}가 더 큽니다.' : '{frac:5/8}이 더 큽니다.', 'g5s1-u4-028': pick(c => !(+c % 4 === 0 && +c % 6 === 0)),
  'g5s1-u4-029': I(range(1, 11).filter(k => gcd(k, 12) === 1).length, '개'), 'g5s1-u4-030': (() => { const k = 34 / (5 + 12); return `{frac:${5 * k}/${12 * k}}`; })(),
  // 5-1-5 분수의 덧셈과 뺄셈 (계산 문항은 발문에서 자동)
  'g5s1-u5-016': V(E('{frac:1/2}+{frac:1/5}'), 'L'), 'g5s1-u5-017': V(E('{frac:3/4}-{frac:1/3}'), 'm'), 'g5s1-u5-018': '통분한 뒤 분자끼리 더합니다.',
  'g5s1-u5-024': V(E('2{frac:1/2}-1{frac:2/3}'), 'L'), 'g5s1-u5-028': V(E('{frac:3/5}+{frac:3/4}'), 'km'), 'g5s1-u5-030': E('1{frac:1/4}-{frac:2/3}'),
  // 5-1-6 다각형의 둘레와 넓이
  'g5s1-u6-001': I(5 * 4, 'cm'), 'g5s1-u6-002': I(6 * 3, 'cm'), 'g5s1-u6-003': I(4 * 6, 'cm'), 'g5s1-u6-004': I((7 + 3) * 2, 'cm'), 'g5s1-u6-005': I(8 * 5, 'cm²'),
  'g5s1-u6-006': I(9 * 9, 'cm²'), 'g5s1-u6-007': I(6 * 4, 'cm²'), 'g5s1-u6-008': I(8 * 5 / 2, 'cm²'), 'g5s1-u6-009': I(6 * 10 / 2, 'cm²'), 'g5s1-u6-010': I((3 + 5) * 4 / 2, 'cm²'),
  'g5s1-u6-011': I(100 * 100, 'cm²'), 'g5s1-u6-012': I(1000 * 1000, 'm²'), 'g5s1-u6-013': I(7 * 4, 'cm'), 'g5s1-u6-014': I((8 + 5) * 2, 'cm'), 'g5s1-u6-015': '1 제곱센티미터',
  'g5s1-u6-016': I(10 * 7 / 2, 'cm²'), 'g5s1-u6-017': I(12 * 5, 'm²'), 'g5s1-u6-018': '모양이 달라도 넓이는 같을 수 있습니다.', 'g5s1-u6-019': I(36 / 9, 'cm'),
  'g5s1-u6-020': I(32 / 4, 'cm'), 'g5s1-u6-021': I(24 * 2 / 8, 'cm'), 'g5s1-u6-022': I(3 * 10000, 'cm²'), 'g5s1-u6-023': I(200 * 300, 'cm²'), 'g5s1-u6-024': I(5000000 / 1000000, 'km²'),
  'g5s1-u6-025': I((6 + 10) * 5 / 2, 'cm²'), 'g5s1-u6-026': I(10 * 6 / 2, 'cm²'), 'g5s1-u6-027': I(48 / 12, 'cm'), 'g5s1-u6-028': I(30 / 2 - 9, 'cm'),
  'g5s1-u6-029': I(12 * 5 * 2 / 10, 'cm'), 'g5s1-u6-030': I(40 * 2 / (3 + 7), 'cm'),
  // 5-2-1 수의 범위와 어림하기 (어림 발문은 자동 계산)
  'g5s2-u1-001': pick(c => +c > 30), 'g5s2-u1-002': pick(c => +c < 18), 'g5s2-u1-003': pick(c => +c <= 45), 'g5s2-u1-004': pick(c => +c >= 12),
  'g5s2-u1-005': I(range(1, 100).filter(n => n > 10 && n <= 15).length, '개'), 'g5s2-u1-006': pick(c => !(+c >= 20 && +c < 25)),
  'g5s2-u1-016': '15와 같거나 큰 수입니다.', 'g5s2-u1-017': '8보다 큰 수입니다.', 'g5s2-u1-018': '●으로 나타냅니다.',
  'g5s2-u1-021': pick(c => { const v = num(c); return cmp(v, Q(40)) > 0 && cmp(v, Q(45)) <= 0; }), 'g5s2-u1-022': I(Math.ceil(243 / 10), '개'),
  'g5s2-u1-023': V(roundQ(Q(5680), Q(1000), '버림'), '원'), 'g5s2-u1-024': V(roundQ(Q(3782), Q(100), '반올림'), '명'),
  'g5s2-u1-025': pick(c => eq(roundQ(Q(+c), Q(10), '반올림'), Q(50))), 'g5s2-u1-026': pick(c => eq(roundQ(Q(+c), Q(100), '올림'), Q(600))),
  'g5s2-u1-028': Q(Math.max(...range(1, 100).filter(n => n >= 32 && n < 40))),
  'g5s2-u1-029': (() => { const ok = range(1, 200).filter(n => eq(roundQ(Q(n), Q(10), '반올림'), Q(70))); return `${ok[0]} 이상 ${ok[ok.length - 1] + 1} 미만`; })(),
  'g5s2-u1-030': I(Math.floor(1375 / 100), '상자'),
  // 5-2-2 분수의 곱셈 (계산 문항은 발문에서 자동)
  'g5s2-u2-009': E('12×{frac:3/4}'), 'g5s2-u2-015': '8보다 작습니다.', 'g5s2-u2-016': '분자는 분자끼리, 분모는 분모끼리 곱합니다.', 'g5s2-u2-017': V(E('{frac:2/3}×6'), 'L'),
  'g5s2-u2-018': E('{frac:3/4}×{frac:1/2}'), 'g5s2-u2-024': V(E('{frac:3/4}×{frac:2/3}'), 'm²'), 'g5s2-u2-026': V(E('60×{frac:5/12}'), '분'), 'g5s2-u2-028': { max: true },
  'g5s2-u2-029': mul(sub(Q(9, 10), Q(2, 5)), Q(2, 5)), 'g5s2-u2-030': V(E('1{frac:1/2}×{frac:2/3}'), 'L'),
  // 5-2-3 합동과 대칭
  'g5s2-u3-001': '서로 합동', 'g5s2-u3-002': '대응변', 'g5s2-u3-003': I(7, 'cm'), 'g5s2-u3-004': I(80, '°'), 'g5s2-u3-005': I(4, '개'), 'g5s2-u3-006': I(2, '개'),
  'g5s2-u3-007': I(3, '개'), 'g5s2-u3-008': I(6, '개'), 'g5s2-u3-009': '점대칭도형', 'g5s2-u3-010': '선대칭도형', 'g5s2-u3-011': I(1, '개'), 'g5s2-u3-012': I(90, '°'),
  'g5s2-u3-013': I(5, 'cm'), 'g5s2-u3-014': I(24, 'cm'), 'g5s2-u3-015': '넓이가 같아도 합동이 아닐 수 있습니다.', 'g5s2-u3-016': '똑같이 둘로 나누어집니다.', 'g5s2-u3-017': I(65, '°'),
  'g5s2-u3-018': '정사각형', 'g5s2-u3-019': '변 ㅂㅅ', 'g5s2-u3-020': I(180 - 50 - 70, '°'), 'g5s2-u3-021': I(5, '개'), 'g5s2-u3-022': '정삼각형', 'g5s2-u3-023': '이등변삼각형',
  'g5s2-u3-024': I(12 / 2, 'cm'), 'g5s2-u3-025': I(3 + 5 + 3 + 5, 'cm'), 'g5s2-u3-026': I(15, 'cm²'), 'g5s2-u3-027': I(6, 'cm'), 'g5s2-u3-028': '정육각형',
  'g5s2-u3-029': I(360 - 90 - 100 - 80, '°'), 'g5s2-u3-030': I((180 - 40) / 2, '°'),
  // 5-2-4 소수의 곱셈 (계산 문항은 발문에서 자동)
  'g5s2-u4-015': V(E('1.2×3'), 'kg'), 'g5s2-u4-016': V(E('0.6×4'), 'm'), 'g5s2-u4-017': '0.8보다 작습니다.', 'g5s2-u4-022': (37 * 24 === 888 ? E('3.7×2.4') : Q(-1)),
  'g5s2-u4-025': V(E('1.6×2.5'), 'kg'), 'g5s2-u4-026': div(Q(314), decQ('3.14')), 'g5s2-u4-027': V(E('2.5×1.2'), 'm²'), 'g5s2-u4-028': { max: true }, 'g5s2-u4-030': V(E('0.35×12'), 'm'),
  // 5-2-5 직육면체
  'g5s2-u5-001': I(6, '개'), 'g5s2-u5-002': I(12, '개'), 'g5s2-u5-003': I(8, '개'), 'g5s2-u5-004': '정사각형', 'g5s2-u5-005': '직사각형', 'g5s2-u5-006': I(3, '개'),
  'g5s2-u5-007': I(3, '개'), 'g5s2-u5-008': I(1, '개'), 'g5s2-u5-009': I(9, '개'), 'g5s2-u5-010': I(1, '개'), 'g5s2-u5-011': I(4, '개'), 'g5s2-u5-012': I(3, '쌍'),
  'g5s2-u5-013': I(5 * 12, 'cm'), 'g5s2-u5-014': '점선으로 그립니다.', 'g5s2-u5-015': '정육면체는 직육면체라고 할 수 있습니다.', 'g5s2-u5-016': '수직으로 만납니다.',
  'g5s2-u5-017': '꼭짓점', 'g5s2-u5-018': '모서리', 'g5s2-u5-019': I((5 + 3 + 4) * 4, 'cm'), 'g5s2-u5-020': I(4, '개'), 'g5s2-u5-021': I(96 / 12, 'cm'), 'g5s2-u5-022': '점선으로 그립니다.',
  'g5s2-u5-023': '셋째 면', 'g5s2-u5-024': I(3, '개'), 'g5s2-u5-025': I(2, '개'), 'g5s2-u5-026': I(6, '개'), 'g5s2-u5-027': I(9 - 3, '개'), 'g5s2-u5-028': I((4 + 4 + 7) * 4, 'cm'),
  'g5s2-u5-029': I(72 / 4 - 8 - 6, 'cm'), 'g5s2-u5-030': Q(range(1, 6).filter(f => f !== 1 && f !== 7 - 1).reduce((s, f) => s + f, 0)),
  // 5-2-6 평균과 가능성
  'g5s2-u6-001': mean([5, 7, 9]), 'g5s2-u6-002': mean([8, 12, 10, 14]), 'g5s2-u6-003': V(mean([20, 25, 30, 25]), '회'), 'g5s2-u6-004': mean([6, 0, 9]),
  'g5s2-u6-005': '(자룟값을 모두 더한 수) ÷ (자료의 수)', 'g5s2-u6-006': V(mean([4, 6, 8]), '권'), 'g5s2-u6-007': I(10 * 4, '점'), 'g5s2-u6-008': '불가능하다',
  'g5s2-u6-009': '확실하다', 'g5s2-u6-010': '반반이다', 'g5s2-u6-011': Q(1, 2), 'g5s2-u6-012': Q(0), 'g5s2-u6-013': Q(1), 'g5s2-u6-014': '반반이다',
  'g5s2-u6-015': '~일 것 같다', 'g5s2-u6-016': '~아닐 것 같다', 'g5s2-u6-017': '불가능하다', 'g5s2-u6-018': Q(1), 'g5s2-u6-019': I(84 * 5 - 330, '점'),
  'g5s2-u6-020': mean([12, 15, 0, 9]), 'g5s2-u6-021': cmp(Q(80, 4), Q(90, 5)) > 0 ? '가 모둠' : '나 모둠', 'g5s2-u6-022': Q([2, 4, 6].length, 6),
  'g5s2-u6-023': '주사위를 굴려 1 이상의 눈이 나올 가능성', 'g5s2-u6-024': Q(20 * 4 - (18 + 22 + 25)), 'g5s2-u6-025': '~아닐 것 같다', 'g5s2-u6-026': '반반이다',
  'g5s2-u6-027': '~아닐 것 같다', 'g5s2-u6-028': 35 > 32 ? '지우' : '민수', 'g5s2-u6-029': I(152 * 5 - 150 * 4, 'cm'), 'g5s2-u6-030': I(80 * 4 - (75 + 80 + 78), '점'),
};

// 발문에서 자동으로 정답을 다시 계산하는 규칙
function autoOracle(prompt) {
  let m;
  if ((m = /^(.+?)(을|를) 계산(하시오|하여 (기약분수|대분수)로 나타내시오)\.$/.exec(prompt))) return E(m[1]);
  if ((m = /^([\d.]+)(을|를) (올림|버림|반올림)하여 (.+?)까지 나타내시오\.$/.exec(prompt))) {
    if (!PLACE[m[4]]) throw new Error(`자리 이름 해석 실패: ${m[4]}`);
    return roundQ(decQ(m[1]), PLACE[m[4]], m[3]);
  }
  if ((m = /^\{frac:(\d+)\/(\d+)\}(을|를) 약분하여 기약분수로 나타내시오\.$/.exec(prompt))) return Q(+m[1], +m[2]);
  if ((m = /^\{frac:(\d+)\/(\d+)\}(을|를) 소수로 나타내시오\.$/.exec(prompt))) return Q(+m[1], +m[2]);
  if ((m = /^([\d.]+)(을|를) 분수로 나타내시오\.$/.exec(prompt))) return decQ(m[1]);
  return undefined;
}

// ---------- 검사 ----------
const errors = [], warnings = [], summary = [];
const POSSIBILITY = ['불가능하다', '~아닐 것 같다', '반반이다', '~일 것 같다', '확실하다'];
for (const pid of IDS) {
  const file = path.join(PACK_DIR, `${pid}.json`);
  if (!fs.existsSync(file)) { errors.push(`${pid}: 파일 없음`); continue; }
  const pack = __normPack(JSON.parse(fs.readFileSync(file, 'utf8')));
  const err = m => errors.push(`${pid} ${m}`);
  const [, g, s, u] = /^g(\d)s(\d)-u(\d)$/.exec(pid);
  if (pack.schema_version !== 3 || pack.pack_id !== pid || pack.unit_id !== pid || pack.school !== 'elementary' || pack.grade !== +g || pack.semester !== +s || pack.unit_order !== +u) err('최상위 필드 오류');
  if (!pack.title) err('title 없음');
  if (JSON.stringify(pack.economy) !== JSON.stringify({ carry_capacity: 120, coin_per_kill: 1, min_spawn_coins: 140 })) err('economy 블록 불일치');
  for (const c of pack.standards ?? []) if (!STD_CODES.has(c)) err(`성취기준 코드 없음 ${c}`);
  const items = pack.items ?? [];
  if (items.length !== 30) err(`문항 수 ${items.length} (30 필요)`);
  const slots = [0, 0, 0, 0], diff = { 1: 0, 2: 0, 3: 0 }, ids = new Set(), prompts = new Set();
  let computed = 0, auto = 0, textChecked = 0;
  items.forEach((q, i) => {
    const e = m => err(`${q.id}: ${m}`);
    if (q.id !== `${pid}-${String(i + 1).padStart(3, '0')}` || ids.has(q.id)) e('id 순서/중복'); ids.add(q.id);
    if (prompts.has(q.prompt)) e('발문 중복'); prompts.add(q.prompt);
    if (q.answer_mode !== 'choice') e('answer_mode');
    if (!Array.isArray(q.choices) || q.choices.length !== 4 || new Set(q.choices).size !== 4) e('보기 4개/중복');
    const ai = q.choices.indexOf(q.answer); if (ai < 0 || q.choices.filter(c => c === q.answer).length !== 1) e('answer 가 보기에 정확히 하나가 아님'); else slots[ai]++;
    if (!['int', 'frac', 'text'].includes(q.format)) e('format');
    if (q.format === 'int' && !q.choices.every(c => /^\d+$/.test(c))) e('format:int 인데 정수가 아닌 보기');
    if (![1, 2, 3].includes(q.difficulty)) e('difficulty 1~3'); else diff[q.difficulty]++;
    if (!q.explain || /\n/.test(q.explain) || !q.unitConcept) e('explain/unitConcept');
    if (!Array.isArray(q.distractor_tags) || q.distractor_tags.length < 1) e('distractor_tags');
    // 값이 같은 보기 금지 (단위가 같을 때 유리수로 비교; 대분수 = 가분수도 같은 값으로 본다)
    const vals = q.choices.map(parseVal);
    for (let a = 0; a < 4; a++) for (let b = a + 1; b < 4; b++) if (vals[a] && vals[b] && vals[a].unit === vals[b].unit && eq(vals[a].q, vals[b].q)) e(`값이 같은 보기 ${q.choices[a]} = ${q.choices[b]}`);
    const av = parseVal(q.answer);
    if (av && (typeof q.answerNumeric !== 'number' || Math.abs(q.answerNumeric - av.q.n / av.q.d) > 1e-9)) e('answerNumeric 불일치');
    // 표기·표현 규칙
    const text = [q.prompt, q.explain, ...q.choices].join(' ');
    if (/\d+\s*\/\s*\d+/.test(text.replace(/\{frac:\d+\/\d+\}/g, ''))) e('평문 분수');
    if (/자리에서\s*(올림|버림|반올림)|적어도|√|\{|\}/.test(text.replace(/\{frac:\d+\/\d+\}/g, ''))) e('금지 표현(AT_PLACE/적어도/√/잘못된 중괄호)');
    if (/[a-zA-Z]/.test(text.replace(/\{frac:\d+\/\d+\}/g, '').replace(/\b(cm|km|kg|m|L)\b|cm²|m²|km²/g, ''))) e('영문자(단위 외) 사용');
    if (!/(구하시오|고르시오|나타내시오|계산하시오)\.$/.test(q.prompt)) e('발문 종결형');
    if (q.prompt.length > 110) warnings.push(`${q.id}: 발문 ${q.prompt.length}자`);
    if (q.choices.some(c => POSSIBILITY.includes(c)) && !q.choices.every(c => POSSIBILITY.includes(c))) e('가능성 어휘는 다섯 단계 표현만');
    // 답 형식 (발문에 명시된 형식)
    if (av && /기약분수/.test(q.prompt) && av.fd && gcd(av.fn, av.fd) !== 1) e('기약분수가 아닌 정답');
    if (av && /대분수/.test(q.prompt) && !(av.kind === 'int' || (av.kind === 'mixed' && av.fn < av.fd))) e('대분수가 아닌 정답');
    if (av && av.kind === 'mixed' && (av.fn >= av.fd || gcd(av.fn, av.fd) !== 1)) e('정답 대분수의 분수 부분이 기약 진분수가 아님');
    // 정답 재계산
    let oracle = O[q.id];
    if (oracle === undefined) { oracle = autoOracle(q.prompt); if (oracle !== undefined) auto++; }
    if (oracle === undefined) { e('오라클 없음 — 검산되지 않은 문항'); return; }
    if (typeof oracle === 'string') { textChecked++; if (oracle !== q.answer) e(`정답 불일치: 기대 "${oracle}" / 팩 "${q.answer}"`); return; }
    computed++;
    if (oracle.pick) { const hits = q.choices.filter(oracle.pick); if (hits.length !== 1 || hits[0] !== q.answer) e(`조건을 만족하는 보기 ${JSON.stringify(hits)} / 정답 ${q.answer}`); return; }
    if (oracle.max || oracle.odd) {
      const vs = q.choices.map(c => { const p = parseVal(c); return p ? p.q : E(c); });
      let k;
      if (oracle.max) { k = 0; vs.forEach((v, j) => { if (cmp(v, vs[k]) > 0) k = j; }); if (vs.filter(v => eq(v, vs[k])).length !== 1) e('가장 큰 값이 여럿'); }
      else { k = vs.findIndex(v => vs.filter(w => eq(w, v)).length === 1); if (vs.filter(v => vs.filter(w => eq(w, v)).length === 1).length !== 1) e('다른 하나가 유일하지 않음'); }
      if (q.choices[k] !== q.answer) e(`정답 불일치: 기대 ${q.choices[k]} / 팩 ${q.answer}`);
      return;
    }
    const want = 'q' in oracle ? oracle : { q: oracle, unit: undefined };
    if (!av) { e(`정답을 수로 읽을 수 없음: ${q.answer}`); return; }
    if (!eq(av.q, want.q)) e(`정답 불일치: 기대 ${show(want.q)} / 팩 ${q.answer}`);
    if (want.unit !== undefined && want.unit !== '' && av.unit !== want.unit) e(`단위 불일치: 기대 ${want.unit} / 팩 ${q.answer}`);
    for (const c of q.choices) if (c !== q.answer) { const p = parseVal(c); if (p && eq(p.q, want.q) && (want.unit === undefined || want.unit === '' || p.unit === want.unit)) e(`오답 보기 ${c} 도 정답과 값이 같음`); }
  });
  if (items[0]?.difficulty !== 1) err('items[0] 은 난이도 1');
  if (slots.some(n => n < 7 || n > 8)) err(`정답 위치 분포 ${slots} (각 7~8)`);
  if (diff[1] < 16 || diff[1] > 20 || diff[2] < 8 || diff[2] > 12 || diff[3] > 2) err(`난이도 분포 ${JSON.stringify(diff)} (1≈18, 2≈10, 3≤2)`);
  summary.push({ pack: pid, title: pack.title, items: items.length, difficulty: diff, answerSlots: slots, recomputed: computed, recomputedFromPrompt: auto, conceptTextChecked: textChecked });
}
for (const r of summary) console.log(`${r.pack} ${r.title.padEnd(12)} 문항 ${r.items}  난이도 ${JSON.stringify(r.difficulty)}  정답위치 ${r.answerSlots.join('/')}  재계산 ${r.recomputed}(발문 자동 ${r.recomputedFromPrompt})  개념 ${r.conceptTextChecked}`);
for (const w of warnings) console.log('경고', w);
if (errors.length) { for (const x of errors) console.log('오류', x); console.log(`\n오류 ${errors.length}건`); process.exitCode = 1; }
else console.log(`\n오류 0건 — ${summary.reduce((s, r) => s + r.items, 0)}문항`);
