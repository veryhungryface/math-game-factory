/** 수학과 무관한 눈치 전략 측정: "가장 고립된 돌", "가장 큰/작은 돌", "가운데 값" */
import fs from 'node:fs';
const P = process.env.FILE || '/Users/sitpo/math-game-factory/factory/state/rejected/20260827-051214-stone-hop/index.html';
const body = fs.readFileSync(P, 'utf8').match(/<script>([\s\S]*?)<\/script>\s*<\/body>/)[1];
const code = body.slice(body.indexOf('const PLACE ='), body.indexOf('function sampleProblems(n)'));
const PONDS = [{id:1,name:'첫',goal:8},{id:2,name:'열린',goal:8},{id:3,name:'자리까지',goal:10},{id:4,name:'자리에서',goal:10},{id:5,name:'상자',goal:10},{id:6,name:'거꾸로',goal:10}];
const M = new Function('PONDS', code + '; buildPools(); return {rowForPond};')(PONDS);
const N = Number(process.env.N || 20000);
for (const n of [5, 7]) {
  console.log('== 돌 ' + n + '개');
  for (const pond of PONDS) {
    let iso = 0, big = 0, small = 0, mid = 0, rows = 0;
    for (let i = 0; i < N; i++) {
      const row = M.rowForPond(pond, i % pond.goal, n);
      const st = row.stones; rows++;
      const ans = st.findIndex((x) => x.safe);
      // 최소 이웃 거리 최대인 돌
      let best = -1, bd = -1;
      for (let j = 0; j < st.length; j++) {
        const l = j > 0 ? st[j].n - st[j - 1].n : Infinity;
        const r = j < st.length - 1 ? st[j + 1].n - st[j].n : Infinity;
        const d = Math.min(l, r);
        if (d > bd) { bd = d; best = j; }
      }
      if (best === ans) iso++;
      if (ans === st.length - 1) big++;
      if (ans === 0) small++;
      if (ans === (st.length / 2 | 0)) mid++;
    }
    console.log(`  연못${pond.id} 고립돌=${(iso/rows*100).toFixed(1)}%  최대값=${(big/rows*100).toFixed(1)}%  최소값=${(small/rows*100).toFixed(1)}%  가운데=${(mid/rows*100).toFixed(1)}%  (무작위 기대 ${(100/n).toFixed(1)}%)`);
  }
}
