// 빗변 철길 C# 문제은행과 같은 30개 정수 튜플 × 12개 과업 문맥 회귀검사.
// 일련번호·지명을 다양성으로 세지 않고, 수치·과업 구조가 포함된 핵심 문장만 센다.
import fs from 'node:fs';
import crypto from 'node:crypto';

const triples = [
  [3,4,5],[5,12,13],[6,8,10],[7,24,25],[8,15,17],[9,12,15],[9,40,41],[10,24,26],[11,60,61],[12,16,20],
  [12,35,37],[13,84,85],[14,48,50],[15,20,25],[15,36,39],[15,112,113],[16,30,34],[16,63,65],[18,24,30],[18,80,82],
  [20,21,29],[20,48,52],[20,99,101],[21,28,35],[21,72,75],[22,120,122],[24,32,40],[24,45,51],[24,70,74],[25,60,65]
];
const taskNames = [
  '직각변 직접 계산','신호판 대각 지지대','제곱식 빈칸','수직 통로 최단 케이블','정사각형 넓이',
  '릴레이 탑 와이어','세 변 구성','△ABC 기호식','점검식 검산','수직 이동 거리','삼각형 노선','릴레이 봉'
];
const min = 4, max = 25;
const rows = [];
for (let style = 0; style < taskNames.length; style++) {
  for (const [a,b,c] of triples) {
    const hyp = c <= max && style % 3 !== 2;
    let known = style % 2 === 0 ? a : b;
    let answer = hyp ? c : (known === a ? b : a);
    if (!hyp && (answer < min || answer > max)) {
      known = known === a ? b : a;
      answer = known === a ? b : a;
    }
    const core = `${taskNames[style]}|${hyp ? '빗변' : '다른변'}|${a}|${b}|${c}|${known}`;
    rows.push({core,a,b,c,known,answer,mode:hyp?'hypotenuse':'missing-leg'});
  }
}
const errors = [];
for (const r of rows) {
  if (r.a*r.a + r.b*r.b !== r.c*r.c) errors.push(`tuple:${r.core}`);
  if (r.answer < min || r.answer > max) errors.push(`range:${r.core}:${r.answer}`);
  const ok = r.mode === 'hypotenuse'
    ? r.a*r.a + r.b*r.b === r.answer*r.answer
    : r.answer*r.answer + r.known*r.known === r.c*r.c;
  if (!ok) errors.push(`judge:${r.core}:${r.answer}`);
}
const uniqueCore = new Set(rows.map(r => r.core));
const result = {
  run_id: `bitbyeon-bank-${new Date().toISOString()}`,
  source_sha256: crypto.createHash('sha256').update(fs.readFileSync(new URL('../../Scripts/BitbyeonRules.cs', import.meta.url))).digest('hex'),
  generated: rows.length,
  unique_core: uniqueCore.size,
  unique_ratio: uniqueCore.size / rows.length,
  answer_range: [Math.min(...rows.map(r=>r.answer)), Math.max(...rows.map(r=>r.answer))],
  errors,
  passed: rows.length === 360 && uniqueCore.size === 360 && errors.length === 0
};
fs.writeFileSync(new URL('./problem-bank-results.json', import.meta.url), JSON.stringify(result, null, 2) + '\n');
console.log(JSON.stringify(result, null, 2));
if (!result.passed) process.exitCode = 1;
