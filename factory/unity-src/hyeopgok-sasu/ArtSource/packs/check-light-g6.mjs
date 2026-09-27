// 협곡 사수 초6 경량 팩(g6s1-u1~u6, g6s2-u1~u6) 가벼운 검산 — SPEC-LIGHT-PACKS.md
// 1) 구조: 30문항·4지선다·정답 1개·값 중복 없음(런타임 동치 규칙 포함)·정답 위치 7~8개씩·난도 분포·태그 3개
// 2) 계산 문항: 발문에 적힌 수를 다시 읽어 정수·유리수(BigInt)로 정답을 새로 계산해 대조 (생성기 코드와 무관)
// 3) 개념 문항: 목록만 출력(--concepts)하고 사람이 읽어 확인
// 실행: node factory/unity-src/hyeopgok-sasu/ArtSource/packs/check-light-g6.mjs [--concepts]
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const root = path.resolve(here, '../../../../..');
const packDir = process.env.PACK_DIR || path.join(root, 'public/g/hyeopgok-sasu/packs');
const curriculum = JSON.parse(fs.readFileSync(path.join(root, 'curriculum/2022-elementary-math.json'), 'utf8'));
const realCodes = new Set(JSON.stringify(curriculum.standards).match(/\[6수\d\d-\d\d\]/g));

// ── 정확한 유리수 ──
const bgcd = (a, b) => { a = a < 0n ? -a : a; b = b < 0n ? -b : b; while (b) [a, b] = [b, a % b]; return a || 1n; };
class Q {
  constructor(n, d = 1n) { n = BigInt(n); d = BigInt(d); if (d === 0n) throw Error('den 0'); if (d < 0n) { n = -n; d = -d; } const g = bgcd(n, d); this.n = n / g; this.d = d / g; }
  static of(x) { return x instanceof Q ? x : new Q(x); }
  add(o) { o = Q.of(o); return new Q(this.n * o.d + o.n * this.d, this.d * o.d); }
  sub(o) { o = Q.of(o); return new Q(this.n * o.d - o.n * this.d, this.d * o.d); }
  mul(o) { o = Q.of(o); return new Q(this.n * o.n, this.d * o.d); }
  div(o) { o = Q.of(o); return new Q(this.n * o.d, this.d * o.n); }
  eq(o) { o = Q.of(o); return this.n === o.n && this.d === o.d; }
  cmp(o) { o = Q.of(o); const l = this.n * o.d, r = o.n * this.d; return l < r ? -1 : l > r ? 1 : 0; }
  floor() { const f = this.n / this.d; return new Q(this.n < 0n && this.n % this.d ? f - 1n : f); }
  toString() { return this.d === 1n ? `${this.n}` : `${this.n}/${this.d}`; }
}
const dec = s => { const [i, f = ''] = s.split('.'); return new Q(BigInt(i + f), 10n ** BigInt(f.length)); };
const PI = new Q(314, 100);
const HUND = new Q(100);

// 발문 속 수(대분수·분수·소수·자연수)를 나온 순서대로
function tokens(prompt) {
  const out = [];
  for (const m of prompt.matchAll(/(\d+)?\{frac:(\d+)\/(\d+)\}|\d+(?:\.\d+)?/g)) {
    if (m[2]) out.push(new Q(BigInt(m[2]) + BigInt(m[1] || 0) * BigInt(m[3]), BigInt(m[3])));
    else out.push(dec(m[0]));
  }
  return out;
}
// 보기 하나의 값: {q, unit} | {ratio:[a,b]} | {expr:Q} | null(글)
function value(s) {
  let t = s.trim().replace(/^약\s*/, '');
  let m = t.match(/^(\d+)\s*:\s*(\d+)$/); if (m) return { ratio: [BigInt(m[1]), BigInt(m[2])] };
  m = t.match(/^\{frac:(\d+)\/(\d+)\}×\{frac:(\d+)\/(\d+)\}$/); if (m) return { expr: new Q(m[1], m[2]).mul(new Q(m[3], m[4])) };
  m = t.match(/^\{frac:(\d+)\/(\d+)\}×(\d+)$/); if (m) return { expr: new Q(m[1], m[2]).mul(new Q(m[3])) };
  m = t.match(/^(?:(\d+)\{frac:(\d+)\/(\d+)\}|\{frac:(\d+)\/(\d+)\}|(\d+(?:\.\d+)?))\s*(.*)$/);
  if (!m) return null;
  const unit = m[7];
  if (unit && !/^(cm³|cm²|m³|m²|cm|m|kg|g|L|mL|%|개|명|칸|층|배|컵|병|자루|분|°)$/.test(unit)) return null;
  let q;
  if (m[1]) q = new Q(BigInt(m[1]) * BigInt(m[3]) + BigInt(m[2]), BigInt(m[3]));
  else if (m[4]) q = new Q(m[4], m[5]);
  else q = dec(m[6]);
  return { q, unit };
}
const sameValue = (a, b) => {
  const va = value(a), vb = value(b);
  if (!va || !vb) return a === b;
  if (va.ratio && vb.ratio) return va.ratio[0] * vb.ratio[1] === va.ratio[1] * vb.ratio[0];
  if (va.expr && vb.expr) return va.expr.eq(vb.expr);
  if (va.q && vb.q) return va.q.eq(vb.q) && va.unit === vb.unit;
  return false;
};
// Unity 로더(HyeopgokPacks.TryChoiceValue)와 같은 동치 규칙 — 이 규칙에 걸리면 문항이 통째로 버려진다
function runtimeKey(s) {
  s = s.trim();
  let m = s.match(/^\{frac:(-?\d+)\/(\d+)\}$/); if (m) { const q = new Q(m[1], m[2]); return `num:${q}:`; }
  m = s.match(/^(-?\d+):(-?\d+)$/); if (m) { const q = new Q(m[1], m[2]); return `ratio:${q}`; }
  m = s.match(/^(-?\d+)(?:\s*(°|cm²|cm³|cm|m))?$/); if (m) return `num:${BigInt(m[1])}:${m[2] || ''}`;
  return `text:${s}`;
}
const rate = s => { const v = value(s); if (!v || !v.q) throw Error(`rate? ${s}`); return v.unit === '%' ? v.q.div(HUND) : v.q; };
const divChoice = s => { const m = s.match(/^(\d+(?:\.\d+)?) ÷ (\d+(?:\.\d+)?)$/); if (!m) throw Error(`÷? ${s}`); return dec(m[1]).div(dec(m[2])); };
const only = (arr, what) => { if (arr.length !== 1) throw Error(`${what}: ${arr.length}개`); return arr[0]; };
function roundTo(q, places) { const s = new Q(10n ** BigInt(places)); return q.mul(s).add(new Q(1, 2)).floor().div(s); }
function simplest(a, b) { // 두 유리수의 비 → 가장 간단한 자연수의 비
  const r = a.div(b); return { ratio: [r.n, r.d] };
}
const sum = a => a.reduce((x, y) => x.add(y), new Q(0));
const max = a => a.reduce((x, y) => (x.cmp(y) >= 0 ? x : y));
const layer = (nums, k) => new Q(nums.filter(x => x.cmp(k) >= 0).length);

// 각기둥·각뿔
const SIDES = { 삼: 3, 사: 4, 오: 5, 육: 6, 칠: 7, 팔: 8, 구: 9 };
const NAME = Object.fromEntries(Object.entries(SIDES).map(([k, v]) => [v, k]));
function solidProps(n, kind) {
  return kind === '기둥' ? { 밑면: 2, 옆면: n, 면: n + 2, 모서리: 3 * n, 꼭짓점: 2 * n } : { 밑면: 1, 옆면: n, 면: n + 1, 모서리: 2 * n, 꼭짓점: n + 1 };
}
function solidAsk(p) {
  const m = p.match(/(삼|사|오|육|칠|팔|구)각(기둥|뿔)의 (?:전개도에서 )?(밑면|옆면|꼭짓점|모서리|면)/);
  return new Q(solidProps(SIDES[m[1]], m[2])[m[3]]);
}
function solidFind(p) { // "모서리가 9개인 각기둥" → 이름
  const m = p.match(/(면|꼭짓점|모서리)이? ?가? ?(\d+)개인 각(기둥|뿔)/) || p.match(/(면|꼭짓점|모서리)[이가] (\d+)개인 각(기둥|뿔)/);
  const hits = [];
  for (let n = 3; n <= 9; n++) if (solidProps(n, m[3])[m[1]] === +m[2]) hits.push(n);
  return only(hits, 'solid');
}

const div01 = t => t[0].div(t[1]);
const pct = (a, b) => a.div(b).mul(HUND);
const S = s => ({ str: s });

// 팩별 재계산 규칙: 번호 → (t: 발문 속 수, p: 발문, it: 문항) → 기대값
const D = n => Object.fromEntries(n.map(i => [i, div01]));
const CALC = {
  'g6s1-u1': {
    ...D([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 15, 16, 17, 19, 20, 21, 23, 24, 25, 26, 28, 29]),
    14: t => ({ expr: t[0].div(t[1]) }),
    18: t => t[1].mul(t[0]),
    22: t => new Q(1).div(t[1]),
    27: (t, p, it) => S(only(it.choices.filter(c => divChoice(c).cmp(1) > 0), '몫>1')),
    30: t => t[0].mul(t[1]),
  },
  'g6s1-u2': {
    ...Object.fromEntries([1, 2, 3, 4, 9, 10, 11, 12, 13, 14, 15, 16, 24, 25, 29, 30].map(i => [i, (t, p) => solidAsk(p)])),
    5: (t, p) => { const m = p.match(/(.)각형인 각(기둥|뿔)/); return S(`${m[1]}각${m[2]}`); },
    6: (t, p) => { const m = p.match(/(.)각형인 각(기둥|뿔)/); return S(`${m[1]}각${m[2]}`); },
    21: (t, p) => S(`${NAME[solidFind(p)]}각기둥`),
    22: (t, p) => S(`${NAME[solidFind(p)]}각기둥`),
    23: (t, p) => S(`${NAME[solidFind(p)]}각뿔`),
    27: (t, p) => new Q(solidProps(solidFind(p), '기둥').모서리),
  },
  'g6s1-u3': {
    ...D([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 20, 21, 22, 23, 24, 25, 26, 27, 28, 30]),
    19: t => t[1].div(t[0]),
    29: t => t[1].div(t[0]),
  },
  'g6s1-u4': {
    1: t => ({ ratio: [t[0].n, t[1].n] }),
    2: t => t[1], 3: t => t[0], 23: t => t[1],
    4: t => ({ ratio: [t[1].n, t[0].n] }),
    5: t => ({ ratio: [t[0].n, t[1].n] }),
    6: div01, 7: div01, 8: div01, 25: t => t[0],
    9: t => t[0].mul(HUND), 10: t => t[0].mul(HUND), 20: t => t[0].mul(HUND),
    11: t => t[0].div(HUND), 21: t => t[0].div(HUND),
    12: t => t[1].div(t[0]), 28: t => t[1].div(t[0]),
    13: t => pct(t[1], t[0]), 24: t => pct(t[1], t[0]),
    14: t => ({ ratio: [t[0].n, t[1].n] }),
    15: t => t[1].div(t[0]),
    16: t => t[1].div(t[0]),
    17: t => pct(t[0].sub(t[1]), t[0]),
    18: (t, p, it) => S(only(it.choices.filter(c => rate(c).cmp(1) > 0), '비율>1')),
    22: t => pct(t[0], t[1]),
    26: t => pct(t[0], t[1]),
    27: t => ({ ratio: [t[1].n, t[0].n] }),
    29: t => t[0].mul(t[1]).div(HUND),
    30: (t, p, it) => { const best = max(it.choices.map(rate)); return S(only(it.choices.filter(c => rate(c).eq(best)), '최대 비율')); },
  },
  'g6s1-u5': {
    2: t => pct(t[1], t[0]), 3: t => pct(t[1], t[0]), 9: t => pct(t[1], t[0]), 23: t => pct(t[1], t[0]), 24: t => pct(t[1], t[0]), 25: t => pct(t[1], t[0]),
    4: t => HUND.sub(sum(t.slice(0, 3))), 22: t => HUND.sub(sum(t.slice(0, 3))),
    5: (t, p) => { const items = [...p.matchAll(/([가-힣]+) (\d+) %/g)].map(m => [m[1], +m[2]]); const top = Math.max(...items.map(x => x[1])); return S(only(items.filter(x => x[1] === top), 'max')[0]); },
    6: div01,
    7: t => t[0].mul(t[1]),
    8: t => t[1].div(t[0]),
    13: t => t[0].mul(t[1]).div(HUND), 15: t => t[0].mul(t[1]).div(HUND), 29: t => t[0].mul(t[1]).div(HUND),
    14: t => t[0].div(HUND), 19: t => t[0].div(HUND),
    16: t => pct(t[0], sum(t.slice(0, 3))), 17: t => pct(t[1], sum(t.slice(0, 3))), 18: t => pct(t[2], sum(t.slice(0, 3))), 27: t => pct(t[2], sum(t.slice(0, 3))),
    20: t => t[0].div(t[3]),
    21: t => { const a = t[0].mul(t[2]), b = t[1].mul(t[2]); return S(a.cmp(b) > 0 ? `${t[0]}명인 학교` : `${t[1]}명인 학교`); },
    26: t => t[0].sub(t[1]),
    30: t => { const a = t[0].mul(t[1]), b = t[2].mul(t[3]); return S(a.cmp(b) > 0 ? '올해' : '작년'); },
  },
  'g6s1-u6': {
    ...Object.fromEntries([2, 3, 9, 18, 21, 23, 28].map(i => [i, t => t[0].mul(t[1]).mul(t[2])])),
    4: t => t[0].mul(t[0]).mul(t[0]), 19: t => t[0].mul(t[0]).mul(t[0]), 20: t => t[0].mul(t[0]).mul(t[0]),
    5: t => t[1].mul(t[2]).mul(t[3]),
    6: t => t[0].mul(1000000), 7: t => t[0].mul(1000000), 24: t => t[0].mul(1000000),
    8: t => t[0].div(1000000),
    10: t => t[0].mul(t[0]).mul(6), 11: t => t[0].mul(t[0]).mul(6),
    ...Object.fromEntries([12, 13, 14, 26].map(i => [i, t => t[0].mul(t[1]).add(t[1].mul(t[2])).add(t[2].mul(t[0])).mul(2)])),
    17: t => t[0].div(t[1].mul(t[2])),
    22: t => t[0].mul(6),
    27: t => t[1].mul(t[2]),
    29: t => t[0].mul(t[0]).mul(t[0]).div(t[1].mul(t[1]).mul(t[1])),
    30: t => { const face = t[0].div(6); for (let e = 1n; e < 100n; e++) if (new Q(e * e).eq(face)) return new Q(e); throw Error('no edge'); },
  },
  'g6s2-u1': {
    ...D([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 23, 24, 26, 27, 28, 29, 30]),
    21: t => ({ expr: t[0].div(t[1]) }),
    22: t => S(t[0].div(t[1]).cmp(t[0]) > 0 ? `${t[0]}보다 크다` : `${t[0]}보다 작다`),
    25: t => t[1].div(t[0]),
  },
  'g6s2-u2': {
    ...D([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 16, 20, 21, 23, 24, 25, 26, 27, 29]),
    ...Object.fromEntries([12, 13, 14, 15, 30].map(i => [i, (t, p) => {
      const places = /일의 자리까지/.test(p) ? 0 : /소수 첫째 자리까지/.test(p) ? 1 : /소수 둘째 자리까지/.test(p) ? 2 : null;
      if (places === null) throw Error('자리 표현 없음'); return { rounded: roundTo(t[0].div(t[1]), places), places };
    }])),
    17: t => t[0].sub(t[0].div(t[1]).floor().mul(t[1])),
    18: t => t[0].sub(t[0].div(t[1]).floor().mul(t[1])),
    19: t => t[0].div(t[1]).floor(),
    28: (t, p, it) => { const best = max(it.choices.map(divChoice)); return S(only(it.choices.filter(c => divChoice(c).eq(best)), '최대 몫')); },
  },
  'g6s2-u3': {
    ...Object.fromEntries([1, 2, 3, 12, 14, 17, 22].map(i => [i, t => sum(t)])),
    4: t => t[1].add(t[3]).add(t[5]), 18: t => t[1].add(t[3]).add(t[5]), 27: t => t[1].add(t[3]).add(t[5]),
    5: t => t[1].add(t[3]), 19: t => t[1].add(t[3]),
    7: t => t[1], 8: t => t[0], 25: t => t[1], 28: t => t[0],
    9: t => layer(t.slice(0, 3), t[3]), 10: t => layer(t.slice(0, 3), t[3]),
    11: t => max(t.slice(0, 4)), 15: t => max(t.slice(0, 3)), 16: t => max(t.slice(0, 2)),
    13: t => t[0].sub(t[2]).sub(t[4]),
    20: t => t[0].sub(t[2]),
    21: t => t[0].mul(t[1]),
    26: t => sum(t.slice(0, 3)).sub(layer(t.slice(0, 3), new Q(1))),
    29: t => max([t[0], t[2]]),
    30: t => sum(t.slice(0, 4)),
  },
  'g6s2-u4': {
    1: t => t[0], 2: t => t[1],
    3: t => ({ set: [t[0], t[3]] }), 4: t => ({ set: [t[1], t[2]] }),
    5: t => ({ ratio: [t[0].mul(t[2]).n, t[1].mul(t[2]).n] }),
    6: t => simplest(t[0], t[1]), 7: t => simplest(t[0], t[1]), 8: t => simplest(t[0], t[1]), 20: t => simplest(t[0], t[1]),
    9: t => t[0].mul(t[1]).div(t[2]),
    10: t => t[1].mul(t[2]).div(t[0]), 11: t => t[1].mul(t[2]).div(t[0]), 23: t => t[1].mul(t[2]).div(t[0]), 27: t => t[1].mul(t[2]).div(t[0]),
    12: t => t[0].mul(t[2]).div(t[1]),
    14: (t, p, it) => S(only(it.choices.filter(c => { const v = value(c); return v && v.ratio && v.ratio[0] * t[1].n === v.ratio[1] * t[0].n; }), '같은 비율')),
    15: t => t[0].mul(t[1]).div(t[1].add(t[2])), 16: t => t[0].mul(t[1]).div(t[1].add(t[2])),
    17: t => t[0].mul(max([t[1], t[2]])).div(t[1].add(t[2])),
    18: t => t[0].mul(t[1].cmp(t[2]) < 0 ? t[1] : t[2]).div(t[1].add(t[2])),
    22: t => ({ ratio: [t[0].div(t[2]).n, t[1].div(t[2]).n] }),
    24: t => t[1].mul(t[2]).div(t[0]),
    25: t => t[1].mul(t[2]).div(t[0]),
    26: t => new Q(60).mul(t[0]).mul(t[2]).div(t[1].add(t[2])),
    28: t => { if (!t[0].mul(t[3]).eq(t[1].mul(t[2]))) throw Error('비례식 아님'); return t[0].mul(t[3]); },
    29: t => t[0].mul(t[1]).div(t[2]),
    30: t => t[0].mul(t[1]).div(t[1].add(t[2])).sub(t[0].mul(t[2]).div(t[1].add(t[2]))),
  },
  'g6s2-u5': {
    ...Object.fromEntries([3, 4, 21, 22, 24].map(i => [i, t => t[0].mul(PI)])),
    ...Object.fromEntries([5, 6, 28].map(i => [i, t => t[0].mul(2).mul(PI)])),
    ...Object.fromEntries([7, 8, 9, 25].map(i => [i, t => t[0].mul(t[0]).mul(PI)])),
    ...Object.fromEntries([10, 11, 27, 29].map(i => [i, t => t[0].div(2).mul(t[0].div(2)).mul(PI)])),
    12: t => t[0].div(PI), 13: t => t[0].div(PI).div(2), 26: t => t[0].div(PI).div(2),
    19: t => t[0].mul(t[0]).mul(PI).div(2),
    20: t => t[0].mul(t[0]).mul(PI).div(t[1]),
    23: t => t[0].mul(t[0]).sub(t[1].mul(t[1])).mul(PI),
    30: t => t[0].mul(PI).div(2),
  },
  'g6s2-u6': {
    11: t => t[0].mul(2).mul(PI), 29: t => t[0].mul(2).mul(PI),
    12: t => t[0].mul(PI),
    17: t => t[0].mul(2), 22: t => t[0].mul(2),
    21: t => t[0], 23: t => t[1], 28: t => t[1],
    30: t => t[0].div(PI).div(2),
  },
};
const ORDER = ['g6s1-u1', 'g6s1-u2', 'g6s1-u3', 'g6s1-u4', 'g6s1-u5', 'g6s1-u6', 'g6s2-u1', 'g6s2-u2', 'g6s2-u3', 'g6s2-u4', 'g6s2-u5', 'g6s2-u6'];
const TITLES = { 'g6s1-u1': '분수의 나눗셈', 'g6s1-u2': '각기둥과 각뿔', 'g6s1-u3': '소수의 나눗셈', 'g6s1-u4': '비와 비율', 'g6s1-u5': '여러 가지 그래프', 'g6s1-u6': '직육면체의 부피와 겉넓이', 'g6s2-u1': '분수의 나눗셈', 'g6s2-u2': '소수의 나눗셈', 'g6s2-u3': '공간과 입체', 'g6s2-u4': '비례식과 비례배분', 'g6s2-u5': '원의 둘레와 넓이', 'g6s2-u6': '원기둥, 원뿔, 구' };

const errors = [], warns = [], concepts = [];
const err = (id, msg) => errors.push(`${id}: ${msg}`);
const warn = (id, msg) => warns.push(`${id}: ${msg}`);
let total = 0, calcOk = 0;

for (const [k, packId] of ORDER.entries()) {
  const file = path.join(packDir, `${packId}.json`);
  if (!fs.existsSync(file)) { err(packId, '파일 없음'); continue; }
  const pk = JSON.parse(fs.readFileSync(file, 'utf8'));
  const [, g, s, u] = packId.match(/^g(\d)s(\d)-u(\d)$/);
  const head = { schema_version: 3, pack_id: packId, title: TITLES[packId], school: 'elementary', grade: +g, semester: +s, unit_id: packId, unit_order: +u };
  for (const [key, v] of Object.entries(head)) if (pk[key] !== v) err(packId, `${key}=${JSON.stringify(pk[key])} (기대 ${JSON.stringify(v)})`);
  if (JSON.stringify(pk.economy) !== '{"carry_capacity":120,"coin_per_kill":1,"min_spawn_coins":140}') err(packId, 'economy 블록 불일치');
  if (!Array.isArray(pk.standards)) err(packId, 'standards 없음');
  for (const c of pk.standards || []) if (!realCodes.has(c)) err(packId, `없는 성취기준 ${c}`);
  const items = pk.items || [];
  if (items.length !== 30) err(packId, `문항 ${items.length}개 (30 필요)`);
  const pos = [0, 0, 0, 0], lv = { 1: 0, 2: 0, 3: 0 }, ids = new Set(), prompts = new Set();
  const rules = CALC[packId] || {};
  items.forEach((it, i) => {
    total++;
    const id = it.id, no = i + 1;
    if (id !== `${packId}-${String(no).padStart(3, '0')}`) err(id, 'id 순번 불일치');
    if (ids.has(id)) err(id, 'id 중복'); ids.add(id);
    if (prompts.has(it.prompt)) err(id, '발문 중복'); prompts.add(it.prompt);
    if (it.answer_mode !== 'choice') err(id, 'answer_mode');
    for (const f of ['prompt', 'answer', 'explain', 'unitConcept']) if (typeof it[f] !== 'string' || !it[f]) err(id, `${f} 없음`);
    if (!Array.isArray(it.choices) || it.choices.length !== 4) { err(id, '보기 4개 아님'); return; }
    if (it.choices.filter(c => c === it.answer).length !== 1) err(id, '정답이 보기에 정확히 1번 있지 않음');
    pos[it.choices.indexOf(it.answer)]++;
    for (let a = 0; a < 4; a++) for (let b = a + 1; b < 4; b++) {
      if (sameValue(it.choices[a], it.choices[b])) err(id, `값이 같은 보기 ${it.choices[a]} / ${it.choices[b]}`);
      if (runtimeKey(it.choices[a]) === runtimeKey(it.choices[b])) err(id, `런타임 동치 보기 ${it.choices[a]} / ${it.choices[b]}`);
    }
    if (!Array.isArray(it.distractor_tags) || it.distractor_tags.length !== 3) err(id, 'distractor_tags 3개 아님(스키마 v3 로더가 버림)');
    else for (const tag of it.distractor_tags) if (!tag.startsWith(`${packId}.`)) err(id, `태그 접두어 ${tag}`);
    if (![1, 2, 3].includes(it.difficulty)) err(id, `difficulty ${it.difficulty}`); else lv[it.difficulty]++;
    const fmt = it.answer.includes('{frac:') ? 'frac' : /^\d+$/.test(it.answer) ? 'int' : 'text';
    if (it.format !== fmt) err(id, `format ${it.format} (기대 ${fmt})`);
    const av = value(it.answer);
    if (av && av.q) {
      if (typeof it.answerNumeric !== 'number' || Math.abs(it.answerNumeric - Number(av.q.n) / Number(av.q.d)) > 1e-9) err(id, `answerNumeric ${it.answerNumeric}`);
    }
    // 표기
    const all = [it.prompt, it.explain, ...it.choices].join(' ');
    if (/의 자리에서\s*(올림|버림|반올림)/.test(all)) err(id, '「○의 자리에서 반올림」 표현');
    if (/\d,\d{3}/.test(all)) err(id, '자릿점 콤마');
    if (/√|㎠|㎤|cm\^|cm2|cm3/.test(all)) err(id, '금지 기호');
    if (/\{(?!frac:\d+\/\d+\})/.test(all)) err(id, '잘못된 {frac} 토큰');
    if (/(?<![{\d/])\d+\/\d+(?![^{]*\})/.test(all.replace(/\{frac:\d+\/\d+\}/g, ''))) err(id, '슬래시 분수');
    for (const c of it.choices) if (/\d\.\d*0(?!\d)/.test(c)) err(id, `소수 끝자리 0: ${c}`);
    const shown = it.prompt.replace(/\{frac:(\d+)\/(\d+)\}/g, '$1/$2');
    if (shown.length > 70) warn(id, `발문 ${shown.length}자 (70자 권장)`);
    for (const c of it.choices) { const len = c.replace(/\{frac:(\d+)\/(\d+)\}/g, (m, a, b) => 'x'.repeat(Math.max(a.length, b.length) + 1)).length; if (len > 12) warn(id, `긴 보기 ${c}`); }
    // 분수 답 형식: 기약·대분수
    const mf = it.answer.match(/^(\d+)?\{frac:(\d+)\/(\d+)\}/);
    if (mf) {
      const [n, d] = [BigInt(mf[2]), BigInt(mf[3])];
      if (bgcd(n, d) !== 1n) err(id, '정답 분수가 기약분수가 아님');
      if (mf[1] && n >= d) err(id, '대분수의 분수 부분이 진분수가 아님');
      if (/대분수로/.test(it.prompt) && !mf[1]) err(id, '대분수로 요구했는데 대분수가 아님');
    }
    // 재계산
    const rule = rules[no];
    if (!rule) { concepts.push(`${id} | ${it.prompt} → ${it.answer} | 오답: ${it.choices.filter(c => c !== it.answer).join(', ')}`); return; }
    let exp;
    try { exp = rule(tokens(it.prompt), it.prompt, it); } catch (e) { err(id, `재계산 실패: ${e.message}`); return; }
    let ok;
    if (exp instanceof Q) ok = av && av.q && av.q.eq(exp);
    else if (exp.str !== undefined) ok = it.answer === exp.str;
    else if (exp.ratio) ok = av && av.ratio && av.ratio[0] === exp.ratio[0] && av.ratio[1] === exp.ratio[1];
    else if (exp.expr) ok = av && av.expr && av.expr.eq(exp.expr);
    else if (exp.set) { const nums = (it.answer.match(/\d+/g) || []).map(x => new Q(x)); ok = nums.length === 2 && exp.set.every(x => nums.some(y => y.eq(x))) && !nums[0].eq(nums[1]); }
    else if (exp.rounded) { const places = (it.answer.split('.')[1] || '').length; ok = av && av.q && av.q.eq(exp.rounded) && places === exp.places; }
    if (!ok) err(id, `정답 불일치: 팩 ${it.answer}, 재계산 ${exp instanceof Q ? exp : JSON.stringify(exp, (k2, v) => (typeof v === 'bigint' ? `${v}` : v instanceof Q ? `${v}` : v))}`);
    else calcOk++;
    // 오답이 우연히 정답 값과 같지 않은지
    if (exp instanceof Q) for (const c of it.choices) if (c !== it.answer) { const v = value(c); if (v && v.q && v.q.eq(exp) && v.unit === (av && av.unit)) err(id, `오답 ${c}이 정답 값과 같음`); }
  });
  if (items[0] && items[0].difficulty !== 1) err(packId, 'items[0] 이 난도 1이 아님');
  if (pos.some(c => c < 7 || c > 8)) err(packId, `정답 위치 분포 ${pos.join('/')}`);
  if (lv[1] < 16 || lv[1] > 20) err(packId, `난도1 ${lv[1]}개 (약 18)`);
  if (lv[3] > 2) err(packId, `난도3 ${lv[3]}개 (2 이하)`);
  console.log(`${packId} ${TITLES[packId].padEnd(12)} 문항 ${items.length} | 위치 ${pos.join('/')} | 난도 ${lv[1]}/${lv[2]}/${lv[3]} | 재계산 ${items.filter((_, i) => rules[i + 1]).length}, 개념 ${items.filter((_, i) => !rules[i + 1]).length}`);
}

if (process.argv.includes('--concepts')) { console.log('\n[개념 문항 — 사람이 읽어 확인]'); concepts.forEach(c => console.log('  ' + c)); }
if (warns.length) { console.log(`\n경고 ${warns.length}건`); warns.forEach(w => console.log('  ' + w)); }
console.log(`\n총 ${total}문항 · 재계산 일치 ${calcOk} · 개념 ${concepts.length} · 오류 ${errors.length}`);
if (errors.length) { errors.forEach(e => console.log('  ✗ ' + e)); process.exit(1); }
