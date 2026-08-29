/** 고정 패턴 봇 전수 + 안전칸 분포 (DOM 없이 오프라인 실행) */
import fs from 'node:fs';
const P = process.env.FILE || '/Users/sitpo/math-game-factory/factory/state/rejected/20260827-051214-stone-hop/index.html';
const body = fs.readFileSync(P, 'utf8').match(/<script>([\s\S]*?)<\/script>\s*<\/body>/)[1];
const code = body.slice(body.indexOf('const PLACE ='), body.indexOf('function sampleProblems(n)'))
  + body.slice(body.indexOf('function simulateBots('), body.indexOf('window.__GAME_TEST__'));
const PONDS = [
  { id: 1, name: '첫 연못', goal: 8 }, { id: 2, name: '열린 연못', goal: 8 },
  { id: 3, name: '자리까지 연못', goal: 10 }, { id: 4, name: '자리에서 연못', goal: 10 },
  { id: 5, name: '상자 연못', goal: 10 }, { id: 6, name: '거꾸로 연못', goal: 10 },
  { id: 7, name: '끝없는 연못', goal: 99 },
];
const M = new Function('PONDS', code + '; buildPools(); return {simulateBots, safeIndexAudit, rowForPond, bootMath};')(PONDS);
console.log('bootMath:', M.bootMath());
const N = Number(process.env.N || 400);
for (const hard of [false, true]) {
  const r = M.simulateBots(N, hard, 5);
  console.log('==', hard ? '아슬' : '여유', `(패턴 ${r[1].patterns}종 × ${N}판)`);
  for (const k of Object.keys(r)) {
    const v = r[k];
    console.log(`  연못${k} ${v.name.padEnd(8)} 최악패턴 [${v.worstPattern}] ${(v.worstRate * 100).toFixed(1)}%  ` +
      `길이별 1:${(v.byPatternLen[1] * 100).toFixed(1)}% 2:${(v.byPatternLen[2] * 100).toFixed(1)}% 3:${(v.byPatternLen[3] * 100).toFixed(1)}% 4:${(v.byPatternLen[4] * 100).toFixed(1)}%  무작위:${(v.randomRate * 100).toFixed(1)}%`);
  }
}
console.log(JSON.stringify(M.safeIndexAudit(Number(process.env.SI || 400)), null, 1));
