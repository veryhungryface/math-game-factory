// 무뇌 정책 4종 × 200판. 실제 생성기 계약(각 문항 답 1~8 독립 균등, 6문항)을 그대로 시뮬레이션한다.
// 고정/순환/무작위 정책은 답값을 읽지 않으며, 무입력은 유효 release가 없어 진도 0이다.
const RUNS = 200, ROUNDS = 6;
let seed = 7; // 재현 가능한 정책 비교용 고정 시드
function rand() { seed ^= seed << 13; seed ^= seed >>> 17; seed ^= seed << 5; return (seed >>> 0) / 4294967296; }
function answer() { return 1 + Math.floor(rand() * 8); }
function audit(name, guess) {
  let first = 0, correct = 0, clears = 0;
  for (let run=0; run<RUNS; run++) {
    let runCorrect = 0;
    for (let round=0; round<ROUNDS; round++) {
      const a = answer(), g = guess(run, round);
      if (g !== null) { first++; if (g === a) { correct++; runCorrect++; } }
    }
    if (runCorrect >= 5) clears++;
  }
  return {name, firstAttemptRate:first?correct/first:0, progress:first, completionRate:clears/RUNS};
}
const rows = [
  audit('연타-고정 4칸', () => 4),
  audit('순환 1→8', (_run, round) => 1 + (round % 8)),
  audit('무작위 1~8', () => 1 + Math.floor(rand()*8)),
  audit('무입력', () => null)
];
console.log(JSON.stringify({runsPerPolicy:RUNS,chance:1/8,rows},null,2));
