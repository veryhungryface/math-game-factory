#!/usr/bin/env node
// 초3 경량 팩(g3s1-u1~u6, g3s2-u1~u6) 가벼운 검산 — SPEC-LIGHT-PACKS.md
// 1) 계산 문항: 발문의 수로 정답을 다시 계산해(정수·유리수 정확 연산) 팩의 정답과 대조한다.
//    팩 생성 원고를 import 하지 않는다. 아래 EXPECT 표는 발문만 보고 따로 쓴 식이다.
// 2) 구조: 4지선다·정답 1개·보기 값 중복(단위 환산 포함) 없음·정답 위치 분포·난도 분포·태그 3개.
// 3) 표기: 하십시오체·평문 분수·자릿점 콤마·「○의 자리에서 올림」 등 금지 표현.
// 개념 문항(T)은 사람이 읽고 확인한다 — 스크립트는 개수만 센다.
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../../..');
const PACK_DIR = path.join(ROOT, 'public/g/hyeopgok-sasu/packs');
const CURRICULUM = JSON.parse(fs.readFileSync(path.join(ROOT, 'curriculum/2022-elementary-math.json'), 'utf8'));
const STANDARD_CODES = new Set(CURRICULUM.standards.map(s => s.code));

// ---------- 유리수 ----------
const gcd = (a, b) => { a = Math.abs(a); b = Math.abs(b); while (b) [a, b] = [b, a % b]; return a || 1; };
const R = (n, d = 1) => { if (!Number.isInteger(n) || !Number.isInteger(d) || d === 0) throw new Error(`유리수 아님 ${n}/${d}`); if (d < 0) { n = -n; d = -d; } const g = gcd(n, d); return [n / g, d / g]; };
const req = (a, b) => a[0] === b[0] && a[1] === b[1];
const rcmp = (a, b) => a[0] * b[1] - b[0] * a[1];
const radd = (a, b) => R(a[0] * b[1] + b[0] * a[1], a[1] * b[1]);
const rsub = (a, b) => R(a[0] * b[1] - b[0] * a[1], a[1] * b[1]);
const rmul = (a, b) => R(a[0] * b[0], a[1] * b[1]);
const rdiv = (a, b) => R(a[0] * b[1], a[1] * b[0]);
const dec = s => { const [i, f = ''] = s.split('.'); return R(Number(i + f), 10 ** f.length); };
const div = (a, b) => { if (a % b !== 0) throw new Error(`${a} ÷ ${b} 나누어떨어지지 않음`); return a / b; };

// ---------- 보기 값 해석 ----------
const LEN = { mm: 1, cm: 10, m: 1000, km: 1000000 }, CAPU = { mL: 1, L: 1000 }, WTU = { g: 1, kg: 1000, t: 1000000 };
const UNIT_DIM = u => (u in LEN ? ['len', LEN[u]] : u in CAPU ? ['cap', CAPU[u]] : u in WTU ? ['wt', WTU[u]] : null);
function parseValue(raw) {
  let s = String(raw).trim(), approx = false;
  if (s.startsWith('약 ')) { approx = true; s = s.slice(2); }
  let m;
  if ((m = /^(\d+)?\{frac:(\d+)\/(\d+)\}$/.exec(s))) return { dim: 'num', v: radd(R(Number(m[1] || 0)), R(Number(m[2]), Number(m[3]))), units: '', approx };
  if ((m = /^(\d+(?:\.\d+)?)$/.exec(s))) return { dim: 'num', v: dec(m[1]), units: '', approx };
  if ((m = /^(\d+)시간(?: (\d+)분)?$/.exec(s))) return { dim: 'dur', v: R(Number(m[1]) * 3600 + Number(m[2] || 0) * 60), units: m[2] ? '시간 분' : '시간' };
  if ((m = /^(\d+)분(?: (\d+)초)?$/.exec(s))) return { dim: 'dur', v: R(Number(m[1]) * 60 + Number(m[2] || 0)), units: m[2] ? '분 초' : '분' };
  if ((m = /^(\d+)초$/.exec(s))) return { dim: 'dur', v: R(Number(m[1])), units: '초' };
  if ((m = /^(\d+)시(?: (\d+)분)?$/.exec(s))) return { dim: 'clock', v: R(Number(m[1]) * 60 + Number(m[2] || 0)), units: '' };
  const toks = s.split(' ');
  if (toks.length % 2 === 0 && toks.length > 0) {
    let dim = null, v = R(0), units = [];
    for (let i = 0; i < toks.length; i += 2) {
      const ud = UNIT_DIM(toks[i + 1]);
      if (!/^\d+(?:\.\d+)?$/.test(toks[i]) || !ud || (dim && dim !== ud[0])) { dim = null; break; }
      dim = ud[0]; v = radd(v, rmul(dec(toks[i]), R(ud[1]))); units.push(toks[i + 1]);
    }
    if (dim) return { dim, v, units: units.join(' '), approx };
  }
  if ((m = /^(\d+)([가-힣]+)$/.exec(s))) return { dim: 'cnt:' + m[2], v: R(Number(m[1])), units: m[2], approx };
  if ((m = /^큰 (\d+), 작은 (\d+)$/.exec(s))) return { dim: 'pic', v: R(Number(m[1]) * 1000 + Number(m[2])), units: '' };
  return null;
}
// 「42 ÷ 7」「5 × 9 + 2」 같은 식 계산(× ÷ 먼저). 등식이면 참/거짓.
function evalExpr(s) {
  const terms = s.split(' + ').map(t => {
    const tk = t.trim().split(' ');
    let v = R(Number(tk[0]));
    for (let i = 1; i < tk.length; i += 2) { const b = R(Number(tk[i + 1])); v = tk[i] === '×' ? rmul(v, b) : tk[i] === '÷' ? rdiv(v, b) : (() => { throw new Error(`식 해석 실패 ${s}`); })(); }
    return v;
  });
  return terms.reduce(radd);
}
const eqTrue = s => { const [l, r] = s.split(' = '); return req(evalExpr(l), evalExpr(r)); };

// ---------- 기대값 도우미 ----------
const N = (n, d = 1) => ({ kind: 'val', dim: 'num', v: R(n, d) });
const A = n => ({ kind: 'val', dim: 'num', v: R(n), approx: true });
const C = (n, counter) => ({ kind: 'val', dim: 'cnt:' + counter, v: R(n) });
const L = (mm, units) => ({ kind: 'val', dim: 'len', v: R(mm), units });
const V = (ml, units) => ({ kind: 'val', dim: 'cap', v: R(ml), units });
const W = (g, units) => ({ kind: 'val', dim: 'wt', v: R(g), units });
const D = (sec, units) => ({ kind: 'val', dim: 'dur', v: R(sec), units });
const K = min => ({ kind: 'val', dim: 'clock', v: R(min) });
const F = (n, d, form) => ({ kind: 'val', dim: 'num', v: R(n, d), form });
const MAX = { kind: 'sel', dir: 1 }, MIN = { kind: 'sel', dir: -1 };
const P = fn => ({ kind: 'pred', fn });
const T = { kind: 'text' };
const r10 = x => Math.round(x / 10) * 10, r100 = x => Math.round(x / 100) * 100;
const comb2 = n => (n * (n - 1)) / 2;
const fracTok = s => /^\{frac:(\d+)\/(\d+)\}$/.exec(s);
const mixedTok = s => /^(\d+)\{frac:(\d+)\/(\d+)\}$/.exec(s);
const pic = (s, big, small) => { const m = /^큰 (\d+), 작은 (\d+)$/.exec(s); return m ? Number(m[1]) * big + Number(m[2]) * small : NaN; };
const cards = ds => { const a = [...ds].sort((x, y) => y - x); return [Number(a.join('')), Number([...a].reverse().join(''))]; };

const EXPECT = {
'g3s1-u1': [N(200 + 300), N(243 + 125), N(416 + 257), N(352 + 174), N(358 + 275), N(487 + 346), N(586 - 43), N(572 - 138), N(645 - 281), N(723 - 458),
  N(500 - 267), C(235 + 142, '권'), C(164 - 128, '번'), C(358 + 276, '개'), C(412 - 175, '개'), A(r100(298) + r100(405)), N(537 - 214), N(10), N(618 - 205), N(452 + 36),
  N(365 + 8), N(786 - 52), N(370 + 250), N(405 - 178), N(432 - 156), N(cards([2, 8, 5])[0] - cards([2, 8, 5])[1]), N(128 + 64), C(342 - 57, '명'), N(609 + 185), C(480 - 150, '장')],
'g3s1-u2': [T, T, T, T, T, T, T, C(1, '개'), C(4, '개'), C(3, '개'),
  C(2, '개'), T, T, T, T, T, T, T, T, L(4 * 5 * 10, 'cm'),
  L(4 * 7 * 10, 'cm'), L(2 * (6 + 4) * 10, 'cm'), L(2 * (9 + 5) * 10, 'cm'), L((3 + 4 + 5) * 10, 'cm'), C(comb2(3), '개'), C(comb2(4), '개'),
  P(s => { const h = parseInt(s, 10), a = (30 * h) % 360; return Math.min(a, 360 - a) === 90; }), C(4 + 4, '개'), C(2 * 1 + 4, '개'), L(2 * (8 * 2 + 3) * 10, 'cm')],
'g3s1-u3': [N(div(6, 2)), N(div(12, 3)), N(div(20, 4)), N(div(35, 7)), N(div(48, 6)), N(div(56, 8)), N(div(27, 9)), N(div(36, 4)), N(div(63, 7)), N(div(40, 5)),
  C(div(18, 3), '개'), C(div(24, 4), '봉지'), C(div(30, 5), '자루'), C(div(42, 7), '접시'), N(div(32, 8)),
  P(s => s.startsWith('42 ÷') && eqTrue(s)), P(s => s.startsWith('9 ×') && eqTrue(s) && s.endsWith('= 54')), N(6 * 7), C(div(64, 8), '장'), C(div(40, 5), '묶음'),
  P(s => req(evalExpr(s), R(div(24, 6)))), C(div(21, 3), '대'), N(4 * 8), N(7 * 8), MAX, C(div(32, 4), '모둠'), C(div(4 * 6, 3), '권'), N(div(72, 9)), P(s => req(evalExpr(s), R(6))), T],
'g3s1-u4': [N(20 * 3), N(40 * 2), N(30 * 5), N(12 * 3), N(23 * 2), N(41 * 3), N(72 * 3), N(16 * 4), N(15 * 3), N(27 * 3),
  N(38 * 4), N(46 * 5), N(67 * 3), C(30 * 4, '개'), C(12 * 4, '자루'), C(25 * 6, '번'), C(43 * 3, '명'), N(60 * 7), N(24 * 2), MAX,
  N(14 * 5), C(18 * 5, '개'), C(div(30 * 4, 3 * 4), '배'), N(34 * 2), N(90 * 2), N(21 * 4), N((23 - 6) * 6), N(47 * 2), N(56 * 3), N(13 * 2)],
'g3s1-u5': [L(10, 'mm'), L(1000 * 1000, 'm'), D(60, '초'), L(3 * 10 + 5, 'mm'), L(42, 'cm mm'), L(2 * 1000000 + 300 * 1000, 'm'), L(4050 * 1000, 'km m'), D(2 * 60 + 10, '초'), D(95, '분 초'), L(57 + 21, 'cm mm'),
  L(38 + 15, 'cm mm'), K(5 * 60 + 40 + 35), D((60 + 30 + 50) * 60, '시간 분'), D(((3 * 60 + 40) - (2 * 60 + 10)) * 60, '시간 분'), K(4 * 60 + 10 - 25), D((3 * 60 + 20) - (60 + 40), '분 초'), L((7400 + 2300) * 1000, 'km m'), T, T, T,
  D(3 * 60, '초'), L(62 - 35, 'cm mm'), L((5000 - 1200) * 1000, 'km m'), L((1500 + 800) * 1000, 'km m'), T, D(65 - 40, '초'), K(3 * 60 + 50 + 60 + 25), L((1000 + 300) * 1000, 'm'), D((60 + 20) * 60, '분'), L((1000 - 350) * 1000, 'm')],
'g3s1-u6': [F(1, 3), F(2, 5), N(4), T, F(3, 4), C(4, '개'), T, MAX, MAX, MIN,
  N(7, 10), N(3, 10), F(7, 10), N(34, 10), L(1, 'cm'), L(53, 'cm'), MAX, MIN, N(12, 10), P(s => { const m = fracTok(s); return !!m && rcmp(R(+m[1], +m[2]), R(4, 7)) > 0; }),
  N(1), F(8 - 3, 8), L(1000 * 4 / 10, 'm'), N(7, 10), P(s => rcmp(R(1, Number(s)), R(1, 5)) > 0), C(5, '개'), MAX, N(5), T, N(1)],
'g3s2-u1': [N(200 * 4), N(213 * 3), N(124 * 2), N(215 * 3), N(142 * 3), N(256 * 3), N(347 * 4), N(4 * 30), N(23 * 30), N(45 * 20),
  N(6 * 14), N(7 * 36), N(24 * 13), N(32 * 21), N(46 * 38), C(125 * 3, '개'), C(30 * 20, '쪽'), C(24 * 15, '개'), C(18 * 40, '개'), A(r10(49) * r10(21)),
  MAX, N(5 * 60), N((27 - 15) * 15), N(302 * 3), N(111 * 5), C(250 * 4, '쪽'), N(8 * 50), N(12 * 40), N(37 * 12), N(8 * 21)],
'g3s2-u2': [N(div(60, 3)), N(div(90, 3)), N(div(48, 4)), N(div(69, 3)), N(div(70, 5)), N(div(72, 3)), N(div(84, 6)), N(17 % 5), N(Math.floor(29 / 4)), N(38 % 6),
  N(58 % 4), N(Math.floor(95 / 7)), P(s => Number(s) >= 5), P(s => { const [a, b] = s.split(' ÷ ').map(Number); return a % b === 0; }), N(4 * 23 + 3), P(eqTrue), N(36 % 3), C(div(96, 3), '장'), C(div(75, 5), '봉지'), C(Math.floor(45 / 6), '상자'),
  C(38 % 5, '개'), N(div(369, 3)), N(div(624, 4)), N(div(412, 4)), N(Math.floor(536 / 5)), N(div(50, 5)), N(26 - 3 * 8), N(6 * 8 + 5), T, N(Math.floor(35 / 8))],
'g3s2-u3': [T, T, T, L(2 * 4 * 10, 'cm'), L(10 * 10 / 2, 'cm'), L(2 * 7 * 10, 'cm'), L(16 * 10 / 2, 'cm'), T, T, C(2, '배'),
  T, L(3 * 10, 'cm'), L(12 * 10 / 2, 'cm'), T, L((5 + 5) * 10, 'cm'), L(6 * 3 * 10, 'cm'), L(10 * 10, 'cm'), L(14 * 10 / 2, 'cm'), L(2 * (2 * 3) * 10, 'cm'), C(1, '개'),
  T, L(2 * 2 * 10, 'mm'), L(20 * 10 / 2, 'cm'), L(2 * 11 * 10, 'cm'), T, L(div(32, 4) / 2 * 10, 'cm'), L(div(18, 3) * 10, 'cm'), L(2 * 6 * 10, 'cm'), L(2 * 4 * 10, 'cm'), L(2 * (2 * 3) * 10, 'cm')],
'g3s2-u4': [N(div(12, 3)), F(3, 12), N(div(20, 4)), N(div(18, 3) * 2), N(div(24, 8) * 3), D(div(60, 4) * 60, '분'), L(div(1000, 5) * 3, 'cm'), T, T, T,
  P(s => { const m = fracTok(s); return !!m && +m[1] >= +m[2]; }), P(s => { const m = fracTok(s); return !!m && +m[1] < +m[2]; }), P(s => { const m = mixedTok(s); return !!m && +m[2] < +m[3]; }),
  F(7, 3, 'mixed'), F(11, 4, 'mixed'), F(2 * 5 + 3, 5, 'improper'), F(1 * 7 + 2, 7, 'improper'), MAX, MAX, P(s => { const m = fracTok(s); return !!m && +m[2] === 5 && +m[1] >= +m[2]; }),
  F(7, 5, 'improper'), C(3 * 4, '개'), C(div(15, 5), '개'), C(div(16, 4) * 3, '장'), MIN, D(div(24, 6) * 3600, '시간'), N(div(5 * 4 * 3, 4)), F(3 * 2 + 1, 2, 'improper'), F(30, 60), T],
'g3s2-u5': [V(1000, 'mL'), W(1000, 'g'), W(1000000, 'kg'), V(2300, 'mL'), W(3040, 'g'), V(4500, 'L mL'), W(5020, 'kg g'), V(1200 + 2500, 'L mL'), V(1800 + 1600, 'L mL'), W(5000 - 2650, 'kg g'),
  W(3700 - 1200, 'kg g'), V(2300 - 800, 'L mL'), T, T, T, T, V(1400 - 600, 'mL'), W(1800 + 700, 'kg g'), MAX, MAX,
  W(1000, 'kg'), V(3000, 'mL'), W(2000000, 'kg'), V(1500 + 750, 'L mL'), W(4300 - 3800, 'g'), W(1500, 'g'), C(div(3000, 500), '번'), W(2100 - 500, 'kg g'), T, W(2300 + 400, 'kg g')],
'g3s2-u6': [C(2 * 10 + 3, '개'), C(3 * 10 + 4, '명'), C(2 * 100 + 5 * 10, '개'), C(Math.floor(34 / 10), '개'), C(52 % 10, '개'), C(Math.floor(45 / 10) + (45 % 10), '개'), C((2 * 10 + 1) - (1 * 10 + 6), '명'), C(10, '개'), C(3 * 100 + 1 * 10, '상자'), W((4 * 10 + 7) * 1000, 'kg'),
  P(s => pic(s, 10, 1) === 26), C(Math.floor((2 * 10 + 13) / 10), '개'), C(6 * 10 + 3, '대'), P(s => pic(s, 100, 10) === 230), C((2 * 10 + 5) + (1 * 10 + 8), '명'), C(4 * 10 + 5, '명'), C(5 * 10 + 5, '개'), C(7 * 10 + 2, '통'), C((4 * 10 + 1) - (2 * 10 + 3), '개'), C(1 * 100 + 4 * 10, '권'),
  C((350 % 100) / 10, '개'), C(5 * 10, '개'), C((2 * 10 + 7) + (3 * 10 + 5), '권'), C(2 * 10, '마리'), C(div(60, 10), '개'), C(3 * 10, '명'), C((2 * 100 + 6 * 10) - (1 * 100 + 9 * 10), '대'), C(60 - (2 * 10 + 5), '명'), C(8 * 10, '명'), C(38 % 10, '개')],
};

// ---------- 검사 ----------
const errors = [], warnings = [], summary = [];
const err = (id, msg) => errors.push(`${id}: ${msg}`);
const FORBIDDEN = [
  [/구하시오|고르시오|쓰시오|나타내시오/, '하십시오체'], [/다음 중/, '「다음 중」'], [/적어도|최소한/, '「적어도」'], [/\d,\d{3}/, '자릿점 콤마'],
  [/자리에서\s*(올림|버림|반올림)/, '「○의 자리에서 올림」 표현'], [/약분|기약분수/, '3학년 범위 밖 약분'], [/√/, '√'],
];
const UNIT_RULES = {
  'g3s1-u3': [[/나머지|나누어떨어/, '3-1 나눗셈에 나머지 용어']],
  'g3s1-u4': [[/받아올림/, '곱셈에 「받아올림」(교과서는 「올림」)']],
  'g3s2-u1': [[/받아올림/, '곱셈에 「받아올림」(교과서는 「올림」)']],
  'g3s2-u5': [[/부피/, '3-2에 「부피」']],
  'g3s2-u3': [[/원주율|넓이/, '3-2 원 범위 밖']],
};
for (const packId of Object.keys(EXPECT)) {
  const file = path.join(PACK_DIR, `${packId}.json`);
  const pack = JSON.parse(fs.readFileSync(file, 'utf8'));
  const exp = EXPECT[packId];
  let computed = 0, text = 0;
  if (pack.schema_version !== 3 || pack.pack_id !== packId || pack.unit_id !== packId || pack.school !== 'elementary' || pack.grade !== 3) err(packId, '최상위 필드 불일치');
  if (JSON.stringify(pack.economy) !== JSON.stringify({ carry_capacity: 120, coin_per_kill: 1, min_spawn_coins: 140 })) err(packId, 'economy 블록이 기존 팩과 다름');
  for (const code of pack.standards) if (!STANDARD_CODES.has(code)) err(packId, `실재하지 않는 성취기준 ${code}`);
  if (pack.items.length !== 30) err(packId, `문항 수 ${pack.items.length}`);
  if (exp.length !== pack.items.length) err(packId, `검산 표 길이 ${exp.length} ≠ 문항 ${pack.items.length}`);
  const posCount = [0, 0, 0, 0], diff = { 1: 0, 2: 0, 3: 0 }, prompts = new Set();
  pack.items.forEach((q, i) => {
    const id = q.id;
    if (id !== `${packId}-${String(i + 1).padStart(3, '0')}`) err(id, 'id 형식');
    if (q.answer_mode !== 'choice') err(id, 'answer_mode');
    if (!Array.isArray(q.choices) || q.choices.length !== 4 || new Set(q.choices).size !== 4) err(id, '보기 4개가 아니거나 같은 문자열');
    if (q.choices.filter(c => c === q.answer).length !== 1) err(id, '정답이 보기에 정확히 1번 있지 않음');
    posCount[q.choices.indexOf(q.answer)]++;
    if (!Array.isArray(q.distractor_tags) || q.distractor_tags.length !== 3 || new Set(q.distractor_tags).size !== 3 || q.distractor_tags.some(t => !t)) err(id, 'distractor_tags 는 서로 다른 3개');
    if (!['int', 'frac', 'text'].includes(q.format)) err(id, 'format');
    if (q.format === 'int' && !/^\d+$/.test(q.answer)) err(id, 'format int 인데 정답이 정수 문자열이 아님');
    if (q.format === 'frac' && !fracTok(q.answer)) err(id, 'format frac 인데 정답이 {frac:} 하나가 아님');
    if (q.format !== 'text' && !Number.isFinite(q.answerNumeric)) err(id, 'answerNumeric 필요');
    if (q.distractor_tags?.some(t => !t.startsWith(`${packId}.`))) err(id, 'distractor_tags 는 "<pack_id>.<오개념>" 형식');
    if (!q.explain || !q.unitConcept) err(id, 'explain/unitConcept');
    if (![1, 2, 3].includes(q.difficulty)) err(id, 'difficulty'); else diff[q.difficulty]++;
    const norm = q.prompt.replace(/\([^)]*\)/g, '').replace(/\s+/g, ' ').trim();
    if (prompts.has(norm)) err(id, '중복 발문'); prompts.add(norm);
    if ([...q.prompt].length > 90) warnings.push(`${id}: 발문 ${[...q.prompt].length}자`);
    const all = [q.prompt, q.explain, ...q.choices].join(' ');
    for (const [re, why] of [...FORBIDDEN, ...(UNIT_RULES[packId] || [])]) if (re.test(all)) err(id, why);
    if (/\d\s*\/\s*\d/.test(all.replace(/\{frac:\d+\/\d+\}/g, ''))) err(id, '평문 분수');
    if (/\{frac:(?!\d+\/\d+\})/.test(all)) err(id, '렌더링 안 되는 분수 토큰');
    // 보기끼리 값이 같으면(단위 환산 포함) 안 된다
    const vals = q.choices.map(parseValue);
    for (let a = 0; a < 4; a++) for (let b = 0; b < a; b++) if (vals[a] && vals[b] && vals[a].dim === vals[b].dim && req(vals[a].v, vals[b].v)) err(id, `값이 같은 보기 「${q.choices[a]}」「${q.choices[b]}」`);
    for (const c of q.choices) { const m = fracTok(c) || mixedTok(c); if (m) { const [n, d] = m.length === 4 ? [+m[2], +m[3]] : [+m[1], +m[2]]; if (gcd(n, d) !== 1 || d === 1) err(id, `기약분수가 아니거나 분모 1인 보기 ${c} (validate-pack-v3 규칙)`); } }
    if (/붓|코인|닢/.test(q.prompt + q.explain)) err(id, '붓기 시대 문구(validate-pack-v3 규칙)');
    if (q.answerNumeric !== undefined) { const pv = parseValue(q.answer); if (pv && pv.dim !== 'clock' && Math.abs(pv.v[0] / pv.v[1] - q.answerNumeric) > 1e-9 && pv.dim === 'num') err(id, 'answerNumeric 불일치'); }
    // 정답 재계산
    const e = exp[i];
    if (!e) return;
    if (e.kind === 'text') { text++; return; }
    computed++;
    if (e.kind === 'val') {
      const pv = parseValue(q.answer);
      if (!pv) return err(id, `정답 해석 실패 「${q.answer}」`);
      if (pv.dim !== e.dim || !req(pv.v, e.v) || !!pv.approx !== !!e.approx) err(id, `정답 불일치: 팩 「${q.answer}」, 재계산 ${e.dim} ${e.v[0]}/${e.v[1]}`);
      if (e.units !== undefined && pv.units !== e.units) err(id, `답 단위 형식 「${pv.units}」 ≠ 요구 「${e.units}」`);
      if (e.form === 'mixed' && !mixedTok(q.answer)) err(id, '대분수 형식 아님');
      if (e.form === 'improper' && !fracTok(q.answer)) err(id, '가분수 형식 아님');
      q.choices.forEach((c, k) => { if (c !== q.answer && vals[k] && vals[k].dim === e.dim && req(vals[k].v, e.v)) err(id, `오답 「${c}」이 정답과 같은 값`); });
    } else if (e.kind === 'sel') {
      const score = c => { const pv = parseValue(c); return pv ? pv.v : evalExpr(c); };
      const scored = q.choices.map(c => ({ c, v: score(c) })).sort((a, b) => e.dir * rcmp(b.v, a.v));
      if (scored[0].c !== q.answer || rcmp(scored[0].v, scored[1].v) === 0) err(id, `최댓값/최솟값 선택 불일치: ${scored[0].c}`);
    } else if (e.kind === 'pred') {
      const ok = q.choices.filter(c => e.fn(c));
      if (ok.length !== 1 || ok[0] !== q.answer) err(id, `조건을 만족하는 보기 ${JSON.stringify(ok)} (정답 「${q.answer}」)`);
    }
  });
  if (posCount.some(n => n < 7 || n > 8)) err(packId, `정답 위치 분포 ${posCount.join('/')}`);
  if (diff[3] > 2 || diff[1] < 15 || diff[1] > 21 || pack.items[0].difficulty !== 1) err(packId, `난도 분포 ${diff[1]}/${diff[2]}/${diff[3]}`);
  summary.push({ pack: packId, title: pack.title, items: pack.items.length, computed, concept: text, difficulty: `${diff[1]}/${diff[2]}/${diff[3]}`, positions: posCount.join('/') });
}
console.table(summary);
if (warnings.length) console.log('경고:\n  ' + warnings.join('\n  '));
console.log(errors.length ? `오류 ${errors.length}건:\n  ${errors.join('\n  ')}` : '오류 0건');
process.exitCode = errors.length ? 1 : 0;
