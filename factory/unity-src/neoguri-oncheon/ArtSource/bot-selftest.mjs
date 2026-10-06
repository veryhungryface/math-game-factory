// B3 무뇌 봇: 실제 게임과 같은 24칸 단일 정수 제출/3회 첫 실패 종료/9탕 완료 규칙.
// 답은 기획 계약대로 1..24 균등 순환하고, 봇은 문제 정보에 접근하지 않는다.
const CHANCE = 1 / 24;
function lcg(seed) { let s=seed>>>0; return () => ((s=(Math.imul(s,1664525)+1013904223)>>>0) / 2**32); }
function runBot(name, pick) {
  let first=0, correct=0, complete=0, progress=0;
  for (let run=0; run<200; run++) {
    let misses=0, solved=0;
    for (let q=0; q<9 && misses<3; q++) {
      // k 우선 균등 생성: 연속 24문항에서 각 답이 정확히 한 번씩 나온다.
      const answer=((run*9+q+7)%24)+1;
      const value=pick(run,q,first);
      if (value==null) break;
      first++;
      if (value===answer) { correct++; solved++; progress++; }
      else misses++;
    }
    if (solved===9) complete++;
  }
  return {name, correct, attempts:first, firstAttemptRate:first?correct/first:0, chance:CHANCE, completed:complete, runs:200, progress};
}
const rand=lcg(0x4e454f47);
const results=[
  runBot('연타',()=>12),
  runBot('순환',(_r,_q,i)=>(i%24)+1),
  runBot('무작위',()=>1+Math.floor(rand()*24)),
  runBot('무입력',()=>null)
];
console.log(JSON.stringify({contract:'24 integer ticks; no pre-release feedback',results},null,2));
if(results.some(r=>r.firstAttemptRate>r.chance+1e-12)) process.exitCode=1;
