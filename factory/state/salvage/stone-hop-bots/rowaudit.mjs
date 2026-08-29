/** 줄 생성기 오프라인 전수 감사 (DOM 없이 수학·배치 구간만 뽑아 실행) */
import fs from 'node:fs';
const P = process.env.FILE || '/Users/sitpo/math-game-factory/factory/state/rejected/20260827-051214-stone-hop/index.html';
const src = fs.readFileSync(P, 'utf8');
const body = src.match(/<script>([\s\S]*?)<\/script>\s*<\/body>/)[1];
const start = body.indexOf('const PLACE =');
const end = body.indexOf('function sampleProblems(n)');
const code = body.slice(start, end);
const PONDS = [
  { id: 1, name: '첫 연못', goal: 8 }, { id: 2, name: '열린 연못', goal: 8 },
  { id: 3, name: '자리까지 연못', goal: 10 }, { id: 4, name: '자리에서 연못', goal: 10 },
  { id: 5, name: '상자 연못', goal: 10 }, { id: 6, name: '거꾸로 연못', goal: 10 },
  { id: 7, name: '끝없는 연못', goal: 99 },
];
const M = new Function('PONDS', code + '; buildPools(); return {rowForPond, nextRowFor, isCorrectSpec, inRange, applyMode, bootMath, pow10};')(PONDS);
console.log('bootMath 자체검사 실패항목:', M.bootMath());

const N = Number(process.env.N || 20000);
let bad = { safeFlag: 0, zeroSafe: 0, dup: 0, fewMis: 0, total: 0 };
const examples = [];
const idxHist = {};
for (const pond of PONDS) {
  const hist = [0, 0, 0, 0, 0];
  let rows = 0, fewMis = 0, singleRows = 0;
  for (let s = 0; s < N; s++) {
    const r = s % (pond.goal === 99 ? 12 : pond.goal);
    const row = M.rowForPond(pond, r, 5);
    rows++; bad.total++;
    const nums = row.stones.map((x) => x.n);
    if (new Set(nums).size !== nums.length) { bad.dup++; if (examples.length < 6) examples.push(['dup', pond.id, row.prompt, nums]); }
    if (!row.stones.some((x) => x.safe)) { bad.zeroSafe++; if (examples.length < 6) examples.push(['zeroSafe', pond.id, row.prompt, nums]); }
    // 독립 재계산으로 safe 플래그 대조
    for (const st of row.stones) {
      let ok;
      if (row.kind === 'range' || row.kind === 'trace') ok = M.inRange(st.n, row.lo, row.loInc, row.hi, row.hiInc);
      else if (row.kind === 'round-match') ok = M.applyMode(row.mode, st.n, row.p) === row.target;
      else if (row.kind === 'round-result') ok = st.n === row.target;
      else if (row.kind === 'life') ok = st.n === row.answer;
      else ok = st.safe;
      if (ok !== !!st.safe) { bad.safeFlag++; if (examples.length < 6) examples.push(['safeFlag', pond.id, row.prompt, st.n, st.safe, ok]); }
    }
    row.stones.forEach((st, i) => { if (st.safe && i < 5) hist[i]++; });
    const ids = new Set(row.stones.filter((x) => !x.safe && x.misconceptionId && x.misconceptionId !== 'fill').map((x) => x.misconceptionId));
    if (row.kind === 'round-result' || row.kind === 'life') {
      singleRows++;
      if (ids.size < 2) { fewMis++; bad.fewMis++; if (examples.length < 8) examples.push(['fewMis', pond.id, row.prompt, row.stones.map((x) => x.n + (x.safe ? '*' : '') + ':' + (x.misconceptionId || '-'))]); }
    }
  }
  idxHist[pond.id] = { name: pond.name, safeByIdx: hist.map((h) => +(h / rows).toFixed(3)), singleRows, fewMisPct: singleRows ? +((fewMis / singleRows) * 100).toFixed(1) : 0 };
}
console.log(JSON.stringify(idxHist, null, 1));
console.log('결함 집계', bad);
if (examples.length) console.log('예시', JSON.stringify(examples.slice(0, 8), null, 1));
