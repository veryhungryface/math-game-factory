#!/usr/bin/env node
// 협곡 사수 — 초4 경량 팩(g4s1-u1~u6, g4s2-u1~u6) 가벼운 검산 (SPEC-LIGHT-PACKS.md)
// 1) 구조: 4지선다·정답 1개·값 중복 없음·정답 위치 7~8·난도 분포·표기 금지어
// 2) 수학: 발문을 다시 읽어 정답을 독립 계산(정수·분수 정확 연산, 부동소수 비교 없음)해 answer 와 대조.
//    생성기 모듈을 가져오지 않는다. 계산 규칙이 없는 개념 문항은 「수동 확인」으로 따로 센다.
// 사용: node factory/unity-src/hyeopgok-sasu/ArtSource/packs/check-light-g4.mjs
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

// 2026-09-28: 초등 팩 발문을 교과서 해요체로 통일했다. 검산 패턴은 하시오체 기준이라 읽을 때만 되돌린다.
const __toHasio = (s) => typeof s === 'string' ? s.replaceAll('구해 보세요','구하시오').replaceAll('계산해 보세요','계산하시오').replaceAll('나타내어 보세요','나타내시오').replaceAll('찾아보세요','고르시오') : s;
const __normPack = (p) => { for (const it of (p.items||[])) it.prompt = __toHasio(it.prompt); return p; };


const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../../../../..');
const PACKS = path.join(ROOT, 'public/g/hyeopgok-sasu/packs');
const IDS = [1, 2].flatMap(s => [1, 2, 3, 4, 5, 6].map(u => `g4s${s}-u${u}`));
const errors = [], warnings = [];
const err = (id, m) => errors.push(`${id}: ${m}`);
const warn = (id, m) => warnings.push(`${id}: ${m}`);

// ---------- 유리수 (정수 쌍) ----------
const gcd = (a, b) => { a = Math.abs(a); b = Math.abs(b); while (b) [a, b] = [b, a % b]; return a || 1; };
const R = (n, d = 1) => { if (d < 0) { n = -n; d = -d; } const g = gcd(n, d); return [n / g, d / g]; };
const add = (a, b) => R(a[0] * b[1] + b[0] * a[1], a[1] * b[1]);
const sub = (a, b) => R(a[0] * b[1] - b[0] * a[1], a[1] * b[1]);
const mul = (a, b) => R(a[0] * b[0], a[1] * b[1]);
const div = (a, b) => R(a[0] * b[1], a[1] * b[0]);
const eq = (a, b) => a[0] === b[0] && a[1] === b[1];
const cmp = (a, b) => a[0] * b[1] - b[0] * a[1];
const I = n => R(n, 1);
const show = r => r[1] === 1 ? `${r[0]}` : `${r[0]}/${r[1]}`;

// ---------- 값 읽기 ----------
const UNIT = /\s*(°C|°|cm|kg|m|L|개|명|원|칸|권|자루|그루|쌍)$/;
const KUNIT = { 조: 1e12, 억: 1e8, 천만: 1e7, 백만: 1e6, 십만: 1e5, 만: 1e4 };
function knum(s) { // '7억 3500만', '2000만', '1만' → 정수
  const m = /^(?:(\d+)조)?\s*(?:(\d+)억)?\s*(?:(\d+)만)?\s*(\d+)?$/.exec(s.trim());
  if (!m || !/\d/.test(s) || !/[조억만]/.test(s)) return null;
  return (+(m[1] || 0)) * 1e12 + (+(m[2] || 0)) * 1e8 + (+(m[3] || 0)) * 1e4 + (+(m[4] || 0));
}
function num(s) { // 단위 없는 수 토큰 → 유리수 또는 null
  s = s.trim(); let m;
  if ((m = /^(\d+)?\{frac:(\d+)\/(\d+)\}$/.exec(s))) return add(I(+(m[1] || 0)), R(+m[2], +m[3]));
  if ((m = /^(\d+)\.(\d+)$/.exec(s))) return R(+(m[1] + m[2]), 10 ** m[2].length);
  if (/^\d+$/.test(s)) return I(+s);
  if (KUNIT[s]) return I(KUNIT[s]);
  const k = knum(s); if (k !== null) return I(k);
  return null;
}
function val(s) { // 보기 문자열 → {r, unit, approx} 또는 null(글 보기)
  let t = s.trim(), approx = false;
  if (t.startsWith('약 ')) { approx = true; t = t.slice(2); }
  const u = UNIT.exec(t); let unit = '';
  if (u && /[\d}]$/.test(t.slice(0, u.index))) { unit = u[1]; t = t.slice(0, u.index); }
  const r = num(t);
  return r ? { r, unit, approx } : null;
}
const key = s => { const v = val(s); return v ? `${v.approx ? '~' : ''}${show(v.r)}|${v.unit}` : `T:${s}`; };

// ---------- 식 계산 (+ − × ÷, 곱셈·나눗셈 먼저) ----------
function evalExpr(src) {
  const toks = src.replace(/°/g, '').trim().split(/\s+([+−×÷])\s+/);
  if (toks.length < 3 || toks.length % 2 === 0) return null;
  const vals = [], ops = [];
  for (let i = 0; i < toks.length; i++) {
    if (i % 2) ops.push(toks[i]); else { const v = num(toks[i]); if (!v) return null; vals.push(v); }
  }
  const v2 = [vals[0]], o2 = [];
  ops.forEach((o, i) => { if (o === '×') v2.push(mul(v2.pop(), vals[i + 1])); else if (o === '÷') v2.push(div(v2.pop(), vals[i + 1])); else { o2.push(o); v2.push(vals[i + 1]); } });
  return o2.reduce((acc, o, i) => o === '+' ? add(acc, v2[i + 1]) : sub(acc, v2[i + 1]), v2[0]);
}
const truthy = eqn => { const [l, r] = eqn.split(' = '); const a = evalExpr(l) ?? num(l), b = evalExpr(r) ?? num(r); return a && b ? eq(a, b) : null; };

// ---------- 한글 수 읽기 ----------
const DG = ['', '일', '이', '삼', '사', '오', '육', '칠', '팔', '구'];
function read4(n) { let s = ''; [[1000, '천'], [100, '백'], [10, '십'], [1, '']].forEach(([p, w]) => { const d = Math.floor(n / p) % 10; if (d) s += (d === 1 && w ? '' : DG[d]) + w; }); return s; }
function readInt(n) {
  const parts = []; [[1e12, '조'], [1e8, '억'], [1e4, '만'], [1, '']].forEach(([p, w]) => { const g = Math.floor(n / p) % 10000; if (g) parts.push(read4(g) + w); });
  return parts.join(' ') || '영';
}
const readDec = s => { const [a, b] = s.split('.'); return `${readInt(+a)} 점 ${[...b].map(d => d === '0' ? '영' : DG[+d]).join('')}`; };

// ---------- 도형 ----------
const PN = { 삼: 3, 사: 4, 오: 5, 육: 6, 칠: 7, 팔: 8, 구: 9, 십: 10 };
const polyN = w => PN[w] ?? null;
const polyName = n => Object.keys(PN).find(k => PN[k] === n) + '각형';
const angleKind = a => a > 0 && a < 90 ? '예각' : a === 90 ? '직각' : a > 90 && a < 180 ? '둔각' : '예각도 둔각도 아님';
const triKind = as => as.some(a => a > 90) ? '둔각삼각형' : as.includes(90) ? '직각삼각형' : '예각삼각형';
const DIR = { 위쪽: [0, 1], 오른쪽: [1, 0], 아래쪽: [0, -1], 왼쪽: [-1, 0] };
const dirName = v => Object.keys(DIR).find(k => DIR[k][0] === v[0] && DIR[k][1] === v[1]);
function arrow(prompt) {
  const m = /^(위쪽|아래쪽|왼쪽|오른쪽)을 가리키는 화살표를 (.+?) 화살표는/.exec(prompt); if (!m) return null;
  let v = [...DIR[m[1]]]; const ops = [];
  for (const x of m[2].matchAll(/(시계 반대 방향|시계 방향)으로 (\d+)°만큼(?: (\d)번)?/g)) ops.push([x.index, 'r', x[1], +x[2] * (+x[3] || 1)]);
  for (const x of m[2].matchAll(/(위쪽|아래쪽|왼쪽|오른쪽)으로 뒤집/g)) ops.push([x.index, 'f', x[1]]);
  ops.sort((a, b) => a[0] - b[0]);
  for (const o of ops) {
    if (o[1] === 'r') { const q = ((o[2] === '시계 방향' ? 1 : -1) * o[3] / 90 % 4 + 4) % 4; for (let i = 0; i < q; i++) v = [v[1], -v[0]]; }
    else if (o[2] === '왼쪽' || o[2] === '오른쪽') v = [-v[0], v[1]]; else v = [v[0], -v[1]];
  }
  return dirName(v);
}

// ---------- 수열 규칙 ----------
function nextTerm(xs) {
  const d = xs.slice(1).map((x, i) => sub(x, xs[i]));
  if (d.every(x => eq(x, d[0]))) return add(xs.at(-1), d[0]);
  const q = xs.slice(1).map((x, i) => div(x, xs[i]));
  if (xs.every(x => x[0] !== 0) && q.every(x => eq(x, q[0]))) return mul(xs.at(-1), q[0]);
  const dd = d.slice(1).map((x, i) => sub(x, d[i]));
  if (dd.length >= 2 && dd.every(x => eq(x, dd[0]))) return add(xs.at(-1), add(d.at(-1), dd[0]));
  return null;
}
const listBefore = (s, mark) => { const head = s.slice(0, s.indexOf(mark)).replace(/,\s*$/, ''); const out = []; for (const t of head.split(', ').reverse()) { const tok = t.replace(/^.*(?:면|\.) /, ''); const v = num(tok); if (!v) break; out.unshift(v); if (tok !== t) break; } return out; };
const pairs = (s, re) => [...s.matchAll(re)].map(m => [m[1], +m[2]]);

// ---------- 문항별 독립 계산 ----------
// 돌려주는 값: {r:유리수} 값 비교, {t:문자열} 글 비교, {pick:fn} 보기 중 조건을 만족하는 것이 정확히 하나, null 개념 문항
function expected(q, pid) {
  const p = q.prompt; let m;
  // 화살표 이동
  if ((m = arrow(p))) return { t: m };
  // □ 가 있는 등식: 보기를 넣어 참인 것이 정확히 하나
  if (/=/.test(p) && /□에 알맞은 수를 구하시오/.test(p)) {
    const e = /(?:^|\. )([^.]*?□[^.]*?)(?:일 때|에서)/.exec(p)?.[1] ?? /(\S.*=.*)(?:일 때|에서)/.exec(p)[1];
    return { pick: c => truthy(e.replace('□', c)) === true };
  }
  if ((m = /^(.+) = (\d+)일 때, □/.exec(p))) return { pick: c => truthy(`${m[1].replace('□', c)} = ${m[2]}`) };
  // 수열의 다음 수
  if (p.includes('□') && /□(에 알맞은 수|입니다)/.test(p)) { const xs = p.includes('규칙에 따라') ? p.split('구하시오. ')[1].split(', ').slice(0, -1).map(num) : listBefore(p, '□'); if (xs.length >= 3 && xs.every(Boolean)) return { r: nextTerm(xs) }; }
  if ((m = /^([\d, ]+)[은는] (?:얼마씩|몇씩)/.exec(p))) { const xs = m[1].split(', ').map(num); const d = sub(xs[1], xs[0]); if (xs.slice(1).every((x, i) => eq(sub(x, xs[i]), d))) return { r: d }; }
  if ((m = /^([\d, ]+)의 규칙으로/.exec(p))) { const xs = m[1].split(', ').map(num), q2 = div(xs[1], xs[0]); if (xs.slice(1).every((x, i) => eq(div(x, xs[i]), q2))) return { t: `${show(q2)}를 곱한다` }; }
  // 계산식 배열: 규칙의 다음 식
  if ((m = /^(.+)의 규칙에 따라 다음에 올 (?:계산식|나눗셈식)/.exec(p))) {
    const es = m[1].split(', ').map(e => e.split(' = ')[0].split(/\s+[+−×÷]\s+/).map(Number)); const op = /[+−×÷]/.exec(m[1])[0];
    const nx = [0, 1].map(k => nextTerm(es.map(e => I(e[k])))[0]); return { r: evalExpr(`${nx[0]} ${op} ${nx[1]}`) };
  }
  if ((m = /일 때, (.+)의 값을 구하시오/.exec(p))) return { r: evalExpr(m[1]) };
  // 식 계산
  if ((m = /^(.+?) ÷ (.+?)의 몫을 구하시오/.exec(p))) return { r: I(Math.floor(+m[1] / +m[2])) };
  if ((m = /^(.+?) ÷ (.+?)의 나머지를 구하시오/.exec(p))) return { r: I(+m[1] % +m[2]) };
  if ((m = /^(.+?)의 곱에서 0은 모두 몇 개/.exec(p))) { const v = evalExpr(m[1]); return { r: I((String(v[0]).match(/0/g) || []).length) }; }
  if ((m = /^(.+?)(?:의 값을 구하시오|의 값을 대분수로 나타내시오|를 계산하시오)\./.exec(p))) { const v = evalExpr(m[1]); if (v) return { r: v, frac: /frac/.test(p) ? m[1] : null }; }
  if ((m = /^(.+?)에서 (.+?)를 뺀 각도/.exec(p))) return { r: sub(m[1] === '직각' ? I(90) : num(m[1]), num(m[2].replace('°', ''))) };
  // 몇이 몇 개인 수
  if (/이 \d+개인 수/.test(p) && /(인 수를 구하시오|인 수는 몇만|의 합을 구하시오)/.test(p)) {
    let s = I(0); for (const [a, c] of pairs(p, /((?:\{frac:\d+\/\d+\})|[\d.]+[만억조]?|천만|십만|백만|만|억|조)이 (\d+)개/g)) s = add(s, mul(num(a), I(c))); return { r: s };
  }
  if ((m = /^(.+?)[은는] (.+?)이 몇 개인 수인지/.exec(p))) return { r: div(num(m[1]), num(m[2])) };
  if ((m = /^(.+?)[은는] (.+?)보다 얼마 더 큰 수/.exec(p))) return { r: sub(num(m[1]), num(m[2])) };
  if ((m = /^(.+?)을 숫자로 쓰면 0은 모두 몇 개/.exec(p))) return { r: I((String(knum(m[1])).match(/0/g) || []).length) };
  if ((m = /^(.+?)에서 (?:(\S+)의 자리|소수 (첫째|둘째|셋째) 자리) 숫자를 구하시오/.exec(p))) {
    const n = knum(m[1]) ?? m[1];
    if (m[3]) return { r: I(+String(m[1]).split('.')[1][{ 첫째: 0, 둘째: 1, 셋째: 2 }[m[3]]]) };
    const e = { 일: 0, 십: 1, 백: 2, 천: 3, 만: 4, 십만: 5, 백만: 6, 천만: 7, 억: 8, 조: 12 }[m[2]]; return { r: I(Math.floor(+n / 10 ** e) % 10) };
  }
  if ((m = /^(\S+)에서 숫자 (\d)(?:가|이) 나타내는 (?:값|수)/.exec(p))) {
    const [a, b = ''] = m[1].split('.'); const s = a + b; const at = s.indexOf(m[2]); if (s.lastIndexOf(m[2]) !== at) return null;
    return { r: R(+m[2] * 10 ** (s.length - 1 - at), 10 ** b.length) };
  }
  if ((m = /중 숫자 (\d)(?:가|이) 나타내는 값이 가장 큰 수/.exec(p))) { const d = m[1]; const place = c => { const i = c.indexOf(d); return i < 0 ? -1 : c.length - 1 - i; }; return { pick: c => q.choices.every(o => place(o) <= place(c)) }; }
  if ((m = /^(.+) 중 가장 (큰|작은) 수를 고르시오/.exec(p))) { const vs = m[1].split(', ').map(num); const best = vs.reduce((a, b) => (m[2] === '큰' ? cmp(b, a) > 0 : cmp(b, a) < 0) ? b : a); return { r: best }; }
  if ((m = /^(\d+)을 바르게 읽은 것/.exec(p))) return { t: readInt(+m[1]) };
  if ((m = /^([\d.]+)을 바르게 읽은 것/.exec(p))) return { t: readDec(m[1]) };
  if ((m = /^(.+?)을 숫자로 나타내시오/.exec(p))) { const w = m[1]; return { pick: c => readInt(+c) === w }; }
  if ((m = /^(\d+)과 (\{frac:\d+\/\d+\})을 소수로/.exec(p))) return { r: add(I(+m[1]), num(m[2])) };
  if ((m = /^(.+?)과 크기가 같은 수를 고르시오/.exec(p))) { const v = num(m[1]); return { pick: c => eq(num(c), v) }; }
  if ((m = /^([\d.]+)의 (\d+)배/.exec(p))) return { r: mul(num(m[1]), I(+m[2])) };
  if ((m = /^([\d.]+)의 \{frac:1\/(\d+)\}/.exec(p))) return { r: div(num(m[1]), I(+m[2])) };
  if (/^소수 세 자리 수를 고르시오/.test(p)) return { pick: c => /^\d+\.\d{3}$/.test(c) };
  if ((m = /수 카드 ([\d, ]+)을 한 번씩 모두 사용하여 만들 수 있는 가장 (큰|작은) 다섯 자리 수/.exec(p))) {
    const ds = m[1].split(', '); const perms = a => a.length <= 1 ? [a] : a.flatMap((x, i) => perms([...a.slice(0, i), ...a.slice(i + 1)]).map(r => [x, ...r]));
    const ns = perms(ds).filter(x => x[0] !== '0').map(x => +x.join('')); return { r: I(m[2] === '큰' ? Math.max(...ns) : Math.min(...ns)) };
  }
  if (/원짜리/.test(p)) { let s = 0; for (const [a, c] of pairs(p, /(\d+)원짜리 (?:지폐|동전) (\d+)(?:장|개)/g)) s += +a * c; return { r: I(s) }; }
  // 나눗셈 검산·문장제
  if ((m = /나누는 수가 (\d+), 몫이 (\d+), 나머지가 (\d+)/.exec(p)) || (m = /어떤 수를 (\d+)(?:으로|로) 나누었더니 몫이 (\d+), 나머지가 (\d+)/.exec(p))) return { r: I(+m[1] * +m[2] + +m[3]) };
  if ((m = /(\d+)(?:으로|로) 나누었을 때 나머지가 될 수 없는 수/.exec(p))) return { pick: c => +c >= +m[1] };
  if ((m = /한 상자에 (\d+)개씩 (\d+)상자/.exec(p))) return { r: I(+m[1] * +m[2]) };
  if ((m = /(\d+)자루를 (\d+)명에게 똑같이 나누어/.exec(p))) return { r: I(+m[1] / +m[2]) };
  if ((m = /(\d+)개를 한 봉지에 (\d+)개씩 담을 때, 남는/.exec(p))) return { r: I(+m[1] % +m[2]) };
  if ((m = /한 개에 (\d+)원인 \S+ (\d+)개의 값/.exec(p))) return { r: I(+m[1] * +m[2]) };
  // 각도
  if (/^직각은 몇 도/.test(p)) return { r: I(90) };
  if (/직각을 똑같이 90으로 나눈/.test(p)) return { r: I(1) };
  if (/^삼각형의 세 각의 크기의 합/.test(p)) return { r: I(180) };
  if (/^사각형의 네 각의 크기의 합/.test(p)) return { r: I(360) };
  if ((m = /^각도가 (\d+)°인 각은/.exec(p))) return { t: angleKind(+m[1]) };
  if ((m = /^([\d°, ]+) 중 (예각|둔각)인 것을 고르시오/.exec(p))) { const k = m[2]; return { pick: c => angleKind(+c.replace('°', '')) === k }; }
  if ((m = /^([\d°, ]+) 중 (예각|둔각)은 모두 몇 개/.exec(p))) return { r: I(m[1].split(', ').filter(a => angleKind(+a.replace('°', '')) === m[2]).length) };
  if ((m = /^삼각형의 두 각이 (\d+)°, (\d+)°입니다. 나머지 한 각/.exec(p))) return { r: I(180 - +m[1] - +m[2]) };
  if ((m = /^사각형의 세 각이 (\d+)°, (\d+)°, (\d+)°입니다/.exec(p))) return { r: I(360 - +m[1] - +m[2] - +m[3]) };
  if (/^사각형의 세 각이 모두 90°/.test(p)) return { r: I(360 - 270) };
  if ((m = /^(\d+)시일 때 시계의 긴바늘과 짧은바늘이 이루는 작은 쪽의 각도/.exec(p))) { const a = (+m[1] % 12) * 30; return { r: I(Math.min(a, 360 - a)) }; }
  if ((m = /크기가 같은 두 각이 각각 (\d+)°입니다. 나머지 한 각/.exec(p))) return { r: I(180 - 2 * +m[1]) };
  if ((m = /나머지 한 각이 (\d+)°입니다. 크기가 같은 (?:두 각 중 )?한 각/.exec(p))) return { r: R(180 - +m[1], 2) };
  if ((m = /길이가 같은 두 변에 있는 두 각 중 한 각이 (\d+)°입니다. 다른 한 각/.exec(p))) return { r: I(+m[1]) };
  if (/^정삼각형의 한 각의 크기/.test(p)) return { r: R(180, 3) };
  if (/^한 삼각형에 둔각은 가장 많이 몇 개/.test(p)) { let k = 0; while ((k + 1) * 91 < 180) k++; return { r: I(k) }; } // 둔각 k개의 합(최소 91°씩)이 180° 미만이어야 함
  if (/^정삼각형을 각의 크기에 따라/.test(p)) return { t: triKind([60, 60, 60]) };
  if (/^세 각의 크기가 모두 60°인 삼각형을 변의 길이에 따라/.test(p) || /^세 변의 길이가 모두 같은 삼각형의 이름/.test(p)) return { t: '정삼각형' };
  if (/^두 변의 길이가 같은 직각삼각형에서 크기가 같은 두 각 중 한 각/.test(p)) return { r: R(180 - 90, 2) };
  if ((m = /세 각의 크기가 (\d+)°, (\d+)°, (\d+)°인 삼각형을 각의 크기에 따라/.exec(p))) return { t: triKind([+m[1], +m[2], +m[3]]) };
  if ((m = /두 각의 크기가 (\d+)°, (\d+)°인 삼각형을 각의 크기에 따라/.exec(p))) return { t: triKind([+m[1], +m[2], 180 - +m[1] - +m[2]]) };
  if ((m = /세 변의 길이가 (\d+) cm, (\d+) cm, (\d+) cm인 삼각형을 변의 길이에 따라/.exec(p))) { const s = new Set([m[1], m[2], m[3]]).size; return { t: s === 1 ? '정삼각형' : s === 2 ? '이등변삼각형' : '세 변의 길이가 모두 다른 삼각형' }; }
  if (/두 변의 길이가 같고 한 각이 100°인 삼각형/.test(p)) return { t: `이등변삼각형, ${triKind([100, 40, 40])}` };
  if ((m = /길이가 같은 두 변이 각각 (\d+) cm이고 나머지 한 변이 (\d+) cm인 이등변삼각형의 세 변의 길이의 합/.exec(p))) return { r: I(2 * +m[1] + +m[2]) };
  if ((m = /세 변의 길이의 합이 (\d+) cm인 이등변삼각형에서 길이가 다른 한 변이 (\d+) cm/.exec(p))) return { r: R(+m[1] - +m[2], 2) };
  if ((m = /길이가 같은 두 변이 각각 (\d+) cm이고, 세 변의 길이의 합이 (\d+) cm입니다. 나머지 한 변/.exec(p))) return { r: I(+m[2] - 2 * +m[1]) };
  // 정다각형·마름모 둘레
  if ((m = /한 변(?:의 길이가|이) (\d+) cm인 (정(\S)각형|마름모)의 (?:세 |네 |모든 )?변의 길이의 합/.exec(p))) return { r: I(+m[1] * (m[3] ? polyN(m[3]) : 4)) };
  if ((m = /(?:세 |네 |모든 )?변의 길이의 합이 (\d+) cm인 (정(\S)각형|마름모)의 한 변의 길이/.exec(p))) return { r: R(+m[1], m[3] ? polyN(m[3]) : 4) };
  if ((m = /한 변이 (\d+) cm이고 모든 변의 길이의 합이 (\d+) cm인 정다각형의 이름/.exec(p))) return { t: '정' + polyName(+m[2] / +m[1]) };
  // 사각형
  if ((m = /^평행사변형의 한 변이 (\d+) cm일 때, 마주 보는 변/.exec(p))) return { r: I(+m[1]) };
  if ((m = /^(?:평행사변형|마름모)의 한 각이 (\d+)°일 때, 마주 보는 각/.exec(p))) return { r: I(+m[1]) };
  if ((m = /^(?:평행사변형|마름모)의 한 각이 (\d+)°일 때, 이웃한 각/.exec(p))) return { r: I(180 - +m[1]) };
  if ((m = /이웃한 두 변의 길이가 (\d+) cm, (\d+) cm인 평행사변형의 네 변의 길이의 합/.exec(p))) return { r: I(2 * (+m[1] + +m[2])) };
  if ((m = /한 변이 (\d+) cm이고, 네 변의 길이의 합이 (\d+) cm입니다. 다른 한 변/.exec(p))) return { r: I(+m[2] / 2 - +m[1]) };
  if (/평행사변형에서 이웃한 두 각의 크기의 합/.test(p)) return { r: I(180) };
  if ((m = /수직인 선분의 길이가 (\d+) cm입니다. 두 평행선 사이의 거리/.exec(p)) || (m = /평행선에 수직인 선분은 (\d+) cm, 비스듬한 선분은 \d+ cm/.exec(p))) return { r: I(+m[1]) };
  if (/^평행사변형은 마주 보는 몇 쌍의 변이/.test(p) || /^정사각형에서 서로 평행한 변은 모두 몇 쌍/.test(p)) return { r: I(2) };
  // 다각형
  if ((m = /^(?:변이|꼭짓점이) (\d+)개인 다각형의 이름/.exec(p)) || (m = /^선분 (\d+)개로 둘러싸인 도형의 이름/.exec(p))) return { t: polyName(+m[1]) };
  if ((m = /^변이 (\d+)개인 정다각형의 이름/.exec(p))) return { t: '정' + polyName(+m[1]) };
  if ((m = /^(\S)각형의 (?:변|꼭짓점)은 모두 몇 개/.exec(p))) return { r: I(polyN(m[1])) };
  if ((m = /^(\S)각형의 변의 수와 꼭짓점의 수의 합/.exec(p))) return { r: I(2 * polyN(m[1])) };
  if ((m = /^(\S)각형에 그을 수 있는 대각선은 모두 몇 개/.exec(p))) { const n = polyN(m[1]); return { r: I(n * (n - 3) / 2) }; }
  if ((m = /^(\S)각형의 한 꼭짓점에서 그을 수 있는 대각선/.exec(p))) return { r: I(polyN(m[1]) - 3) };
  if (/^정사각형의 한 각의 크기/.test(p) || /^정사각형의 두 대각선이 만나서 이루는 각/.test(p)) return { r: I(90) };
  if (/정삼각형 조각으로 한 변이 2 cm인 정육각형/.test(p)) return { r: R(6 * 60, 60) }; // 정육각형 중심 둘레 360°를 정삼각형 한 각 60°로 채움
  // 이동
  if ((m = /^시계 방향으로 90°만큼 (\d)번 돌린 것은 시계 방향으로 몇 도/.exec(p))) return { r: I(90 * +m[1]) };
  if ((m = /^시계 (반대 )?방향으로 (\d+)°만큼 돌린 것은 시계 (반대 )?방향으로 몇 도/.exec(p))) return { r: I(!!m[1] === !!m[3] ? +m[2] : 360 - +m[2]) };
  if (/밀/.test(p) && /(\d+) cm/.test(p)) {
    let x = 0; for (const [d, c] of pairs(p, /(왼쪽|오른쪽)으로 (\d+) cm/g)) x += (d === '오른쪽' ? 1 : -1) * c;
    if (/몇 cm 움직였는지 구하시오/.test(p)) return { r: I(Math.abs(x)) };
    return { t: x === 0 ? '처음 자리' : `${x > 0 ? '오른쪽' : '왼쪽'}으로 ${Math.abs(x)} cm` };
  }
  // 그래프 눈금
  if ((m = /눈금 (\d+)칸이 (\d+) ?(?:°C|명|개)?(?:를|을) 나타낼 때, 한 칸의 크기를 구하는 식/.exec(p))) return { t: `${m[2]} ÷ ${m[1]}` };
  if ((m = /눈금 (\d+)칸이 ([\d.]+) ?(?:°C|명|개|cm)?(?:를|을) 나타낼 때, .*한 칸/.exec(p))) return { r: R(+m[2], +m[1]) };
  if ((m = /물결선 위의 첫 눈금이 (\d+)이고 세로 눈금 (?:한 칸이 (\d+)|(\d+)칸이 (\d+)을 나타냅니다)입?니?다?\. \d+에서 (\d+)칸 위/.exec(p))) { const cell = m[2] ? I(+m[2]) : R(+m[4], +m[3]); return { r: add(I(+m[1]), mul(cell, I(+m[5]))) }; }
  if ((m = /눈금 (\d+)칸이 (\d+) ?(?:°C|명|개|cm)(?:을|를) 나타냅니다. (?:막대가|점이 0에서) (\d+)칸/.exec(p))) return { r: mul(R(+m[2], +m[1]), I(+m[3])) };
  if ((m = /눈금 한 칸이 ([\d.]+) ?(?:°C|명|개|cm|kg|권|원|그루)(?:을|를) 나타냅니다. (?:막대가|점이 0에서|[^.]*막대가) (\d+)칸/.exec(p))) return { r: mul(num(m[1]), I(+m[2])) };
  if ((m = /눈금 한 칸이 (\d+)(?:명|그루)(?:을|를) 나타냅니다. (\d+)(?:명|그루)(?:을|를) 나타내려면/.exec(p))) return { r: R(+m[2], +m[1]) };
  if ((m = /한 칸이 (\d+)명인 막대그래프에서 월요일은 (\d+)명, 화요일은 (\d+)명입니다. 화요일 막대는 몇 칸 더/.exec(p))) return { r: R(+m[3] - +m[2], +m[1]) };
  if ((m = /눈금 한 칸이 (\d+) ?(?:kg|명)을 나타냅니다. 가 막대는 (\d+)칸, 나 막대는 (\d+)칸일 때, .*의 (차|합)/.exec(p))) return { r: I(+m[1] * (m[4] === '차' ? +m[2] - +m[3] : +m[2] + +m[3])) };
  // 자료 읽기: '이름 수단위' 목록
  const data = pairs(p, /(사과|배|귤|포도|[1-4]반|[1-4]주|[3-6]월|월요일|화요일|수요일|목요일) (\d+) ?(?:명|cm|kg|개)/g);
  if (data.length >= 3) {
    const byName = Object.fromEntries(data), vs = data.map(d => d[1]);
    const rank = [...data].sort((a, b) => b[1] - a[1]);
    if (/가장 많은 학생이 좋아하는|가장 많은 반/.test(p)) return { t: rank[0][0] };
    if (/가장 적은 학생이 좋아하는/.test(p)) return { t: rank.at(-1)[0] };
    if (/두 번째로 많은/.test(p)) return { t: rank[1][0] };
    if ((m = /(\S+)[은는] (\S+)보다 몇 명 더/.exec(p))) return { r: I(byName[m[1]] - byName[m[2]]) };
    if (/모두 몇 명/.test(p)) return { r: I(vs.reduce((a, b) => a + b, 0)) };
    const diffs = data.slice(1).map((d, i) => [`${data[i][0]}와 ${d[0]} 사이`, d[1] - data[i][1]]);
    const fix = s => s.replace(/(\S)와 /, (w, c) => ((c.charCodeAt(0) - 0xAC00) % 28 ? `${c}과 ` : w)); // 받침 있는 말 뒤는 '과'
    if (/가장 많이 (자란|늘어난) 때/.test(p)) return { t: fix([...diffs].sort((a, b) => b[1] - a[1])[0][0]) };
    if (/변화가 없는 때/.test(p)) { const z = diffs.filter(d => d[1] === 0); return z.length === 1 ? { t: fix(z[0][0]) } : null; }
    if (/전날보다 생산량이 줄어든 요일/.test(p)) { const z = data.slice(1).filter((d, i) => d[1] < data[i][1]); return z.length === 1 ? { t: z[0][0] } : null; }
    if ((m = /(\S+)와 (\S+) 사이에 자란 키/.exec(p)) || (m = /(\S+)과 (\S+)의 몸무게의 차/.exec(p))) return { r: I(Math.abs(byName[m[2]] - byName[m[1]])) };
    if ((m = /(\d+)시 (\d+) °C, (\d+)시 (\d+) °C, (\d+)시 (\d+) °C입니다. (\d+)시 30분/.exec(p))) { const i = [+m[1], +m[3], +m[5]].indexOf(+m[7]); const lo = [+m[2], +m[4], +m[6]][i], hi = [+m[2], +m[4], +m[6]][i + 1]; return { pick: c => { const v = val(c); return v.approx && v.r[1] === 1 && v.r[0] > Math.min(lo, hi) && v.r[0] < Math.max(lo, hi); } }; }
  }
  if ((m = /(\d+)시 (\d+) °C, (\d+)시 (\d+) °C, (\d+)시 (\d+) °C입니다. (\d+)시 30분/.exec(p))) { const ts = [+m[1], +m[3], +m[5]], vs = [+m[2], +m[4], +m[6]]; const i = ts.indexOf(+m[7]); const lo = Math.min(vs[i], vs[i + 1]), hi = Math.max(vs[i], vs[i + 1]); return { pick: c => { const v = val(c); return v.approx && v.r[1] === 1 && v.r[0] > lo && v.r[0] < hi; } }; }
  // 분수·소수 문장제
  if ((m = /(\{frac:\d+\/\d+\}|\d*\{frac:\d+\/\d+\}|[\d.]+) (L|m|kg) 중에서 (\d*\{frac:\d+\/\d+\}|[\d.]+) \2(?:를|을) (?:마셨|사용했)/.exec(p))) return { r: sub(num(m[1]), num(m[3])), frac: /frac/.test(p) ? `${m[1]} − ${m[3]}` : null };
  if ((m = /길이가 (\d*\{frac:\d+\/\d+\}) m인 끈과 (\d*\{frac:\d+\/\d+\}) m인 끈의 길이의 합/.exec(p))) return { r: add(num(m[1]), num(m[2])), frac: `${m[1]} + ${m[2]}` };
  if ((m = /물 ([\d.]+) L에 물 ([\d.]+) L를 더 부었/.exec(p))) return { r: add(num(m[1]), num(m[2])) };
  // 규칙과 관계
  if ((m = /^(옳은|옳지 않은) 식을 고르시오/.exec(p))) { const want = m[1] === '옳은'; return { pick: c => truthy(c) === want }; }
  if ((m = /(\S+) 한 대의 바퀴는 (\d+)개입니다. \1 (\d+)대의 바퀴/.exec(p))) return { r: I(+m[2] * +m[3]) };
  if ((m = /바둑돌을 첫째에 (\d+)개, 둘째에 (\d+)개, 셋째에 (\d+)개(?:, 넷째에 (\d+)개)? 놓았습니다. (다섯째|열째)/.exec(p))) {
    let xs = [m[1], m[2], m[3], m[4]].filter(Boolean).map(x => I(+x)); const n = m[5] === '다섯째' ? 5 : 10; while (xs.length < n) xs.push(nextTerm(xs)); return { r: xs[n - 1] };
  }
  if ((m = /첫째 (\d+)개, 둘째 (\d+)개, 셋째 (\d+)개, 넷째 (\d+)개입니다. 다섯째/.exec(p))) return { r: nextTerm([m[1], m[2], m[3], m[4]].map(x => I(+x))) };
  if ((m = /삼각형 1개에 (\d+)개, 2개에 (\d+)개, 3개에 (\d+)개가 필요합니다. 삼각형 (\d+)개/.exec(p))) { const xs = [m[1], m[2], m[3]].map(x => I(+x)); while (xs.length < +m[4]) xs.push(nextTerm(xs)); return { r: xs[+m[4] - 1] }; }
  return null; // 개념 문항
}

// ---------- 검사 ----------
const curriculum = JSON.parse(fs.readFileSync(path.join(ROOT, 'curriculum/2022-elementary-math.json'), 'utf8'));
const codes = new Set(curriculum.standards.map(s => s.code));
const tbs = JSON.parse(fs.readFileSync(path.join(ROOT, 'curriculum/textbook-structure.json'), 'utf8'));
const economyRef = JSON.parse(fs.readFileSync(path.join(PACKS, 'm2s2-u7.json'), 'utf8')).economy;
const summary = [];
let totalItems = 0, totalComputed = 0;
const manual = [];

for (const pid of IDS) {
  const file = path.join(PACKS, `${pid}.json`);
  if (!fs.existsSync(file)) { err(pid, '파일 없음'); continue; }
  const pk = __normPack(JSON.parse(fs.readFileSync(file, 'utf8')));
  const [, sem, ord] = /^g4s(\d)-u(\d)$/.exec(pid).map(Number);
  const tbUnit = tbs.volumes.find(v => v.grade === 4 && v.semester === sem).units.find(u => u.order === ord);
  if (pk.schema_version !== 3) err(pid, 'schema_version 3 아님');
  if (pk.pack_id !== pid || pk.unit_id !== pid) err(pid, 'pack_id/unit_id 불일치');
  if (pk.title !== tbUnit.title) err(pid, `title "${pk.title}" ≠ 교과서 "${tbUnit.title}"`);
  if (pk.school !== 'elementary' || pk.grade !== 4 || pk.semester !== sem || pk.unit_order !== ord) err(pid, '학교급·학년·학기·단원 순서 불일치');
  if (!Array.isArray(pk.standards) || pk.standards.some(c => !codes.has(c))) err(pid, `실재하지 않는 성취기준 ${pk.standards}`);
  if (JSON.stringify(pk.economy) !== JSON.stringify(economyRef)) err(pid, 'economy 블록이 기존 팩과 다름');
  const items = pk.items || [];
  if (items.length !== 30) err(pid, `문항 수 ${items.length} ≠ 30`);
  totalItems += items.length;
  const ids = new Set(), prompts = new Set(), posCount = [0, 0, 0, 0], lv = { 1: 0, 2: 0, 3: 0 };
  let computed = 0;
  items.forEach((q, i) => {
    const id = q.id;
    if (id !== `${pid}-${String(i + 1).padStart(3, '0')}` || ids.has(id)) err(id, 'id 형식·중복'); ids.add(id);
    const norm = q.prompt.replace(/\([^)]*\)/g, '').replace(/\s+/g, ' ').trim();
    if (prompts.has(norm)) err(id, '발문 중복'); prompts.add(norm);
    if (q.answer_mode !== 'choice') err(id, 'answer_mode ≠ choice');
    if (!Array.isArray(q.choices) || q.choices.length !== 4) { err(id, '보기 4개 아님'); return; }
    if (q.choices.filter(c => c === q.answer).length !== 1) err(id, '정답이 보기 중 정확히 하나가 아님');
    if (new Set(q.choices.map(key)).size !== 4) err(id, `값이 같은 보기: ${q.choices.join(' | ')}`);
    posCount[q.choices.indexOf(q.answer)]++;
    if (![1, 2, 3].includes(q.difficulty)) err(id, 'difficulty 1~3 아님'); else lv[q.difficulty]++;
    if (!['int', 'frac', 'text'].includes(q.format)) err(id, 'format 오류');
    const av = val(q.answer);
    if (q.format !== 'text' && !av) err(id, '수치 format 인데 정답을 수로 읽지 못함');
    if (q.format !== 'text' && !Number.isFinite(q.answerNumeric)) err(id, 'answerNumeric 없음');
    if (av && q.answerNumeric !== undefined && Math.abs(q.answerNumeric * av.r[1] - av.r[0]) > 1e-6 * Math.max(1, Math.abs(av.r[0]))) err(id, `answerNumeric ${q.answerNumeric} ≠ ${show(av.r)}`);
    if (!q.explain || !q.unitConcept) err(id, 'explain/unitConcept 없음');
    if (new Set(q.distractor_tags || []).size < 2) err(id, 'distractor_tags 2종 이상 필요');
    const text = [q.prompt, q.explain, ...q.choices].join(' ');
    if (/\d+\s*\/\s*\d+/.test(text.replace(/\{frac:\d+\/\d+\}/g, ''))) err(id, '평문 분수(a/b) 금지');
    if (/√|적어도/.test(text)) err(id, '금지어(√·적어도)');
    if (/자리에서\s*(반올림|올림|버림)|반올림|올림하여|버림하여/.test(text)) err(id, '4학년 범위 밖 어림 표현');
    if (/\d\s*[a-zA-Z]+\s*\/|\bx\b|\by\b/.test(q.prompt)) err(id, '4학년에서 문자 사용');
    if (q.prompt.length > 70) warn(id, `발문 ${q.prompt.length}자(권장 70자 이내)`);
    if (q.choices.some(c => c.length > 16)) warn(id, '보기 16자 초과(기존 팩 최대 16자 — 패드 글자 크기)');
    // 수학
    let e; try { e = expected(q, pid); } catch (x) { err(id, `검산 규칙 예외: ${x.message}`); return; }
    if (!e) { manual.push(`${id} ${q.prompt}`); return; }
    computed++;
    if (e.pick) {
      const ok = q.choices.filter(c => { try { return e.pick(c); } catch { return false; } });
      if (ok.length !== 1 || ok[0] !== q.answer) err(id, `조건을 만족하는 보기 [${ok.join(', ')}] ≠ 정답 ${q.answer}`);
    } else if (e.t !== undefined) {
      if (e.t !== q.answer) err(id, `재계산 "${e.t}" ≠ 정답 "${q.answer}"`);
    } else {
      if (!e.r) { err(id, '재계산 실패'); return; }
      if (!av || !eq(av.r, e.r)) err(id, `재계산 ${show(e.r)} ≠ 정답 ${q.answer}`);
      for (const c of q.choices) if (c !== q.answer) { const cv = val(c); if (cv && eq(cv.r, e.r) && cv.unit === av?.unit) err(id, `오답 ${c} 도 정답과 값이 같음`); }
      // 4-2 분수: 약분하지 않음(분모 유지)·대분수 꼴
      if (e.frac) {
        const dens = [...e.frac.matchAll(/\{frac:\d+\/(\d+)\}/g)].map(x => +x[1]);
        const m = /^(\d+)?\{frac:(\d+)\/(\d+)\}/.exec(q.answer);
        if (m && dens.length && +m[3] !== dens[0]) err(id, `분모 ${m[3]} ≠ 문제 분모 ${dens[0]} (4-2는 약분하지 않음)`);
        if (m && m[1] && +m[2] >= +m[3]) err(id, '대분수의 분수 부분이 진분수가 아님');
        if (m && !m[1] && +m[2] >= +m[3]) err(id, '가분수 정답 — 대분수로 나타내야 함');
      }
    }
  });
  if (posCount.some(n => n < 7 || n > 8)) err(pid, `정답 위치 분포 ${posCount}`);
  if (lv[1] < 16 || lv[1] > 20 || lv[2] < 8 || lv[2] > 12 || lv[3] > 2) err(pid, `난도 분포 ${JSON.stringify(lv)}`);
  if (items[0]?.difficulty !== 1) err(pid, 'items[0] 이 난도 1 아님');
  totalComputed += computed;
  summary.push(`${pid} ${pk.title.padEnd(10, ' ')} 문항 ${items.length} · 재계산 ${computed} · 개념(수동) ${items.length - computed} · 난도 ${lv[1]}/${lv[2]}/${lv[3]} · 정답 위치 ${posCount.join('/')}`);
}

console.log(summary.join('\n'));
console.log(`\n합계 ${totalItems}문항 · 독립 재계산 ${totalComputed} · 개념 문항(수동 확인) ${manual.length}`);
if (process.argv.includes('--manual')) console.log('\n[개념 문항]\n' + manual.join('\n'));
if (warnings.length) console.log(`\n경고 ${warnings.length}건\n` + warnings.join('\n'));
console.log(errors.length ? `\n오류 ${errors.length}건\n` + errors.join('\n') : '\n오류 0건');
process.exitCode = errors.length ? 1 : 0;
