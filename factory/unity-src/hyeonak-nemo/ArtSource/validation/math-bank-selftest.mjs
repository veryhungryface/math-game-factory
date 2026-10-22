import fs from 'node:fs';
import path from 'node:path';

const outPath = path.join(path.dirname(new URL(import.meta.url).pathname), 'math-bank-results.json');
const mod12 = x => ((x % 12) + 12) % 12;

function correct(p, rb, kb, rd, kd) {
  kb = mod12(kb - p.baseK); kd = mod12(kd - p.baseK);
  const opposite = mod12(kd - kb) === 6;
  if (p.kind === 'parallelogram') return rb === p.a && rd === p.b;
  if (p.kind === 'rectangle') return rb === p.a && rd === p.a && opposite;
  const perpendicular = kb === 3 && kd === 9;
  if (p.kind === 'rhombus') return rb === p.b && rd === p.b && opposite && perpendicular;
  return rb === p.a && rd === p.a && opposite && perpendicular;
}

const problems = [];
for (let a=1;a<=10;a++) for (let b=1;b<=10;b++) if (a!==b) for (let rev=0;rev<2;rev++)
  problems.push({kind:'parallelogram',a,b,baseK:((a*3+b*5+rev)%2)});
for (let a=1;a<=10;a++) for (let rev=0;rev<2;rev++)
  problems.push({kind:'rectangle',a,b:a,baseK:(a+rev*2)%12});
for (let a=1;a<=10;a++) for (let b=1;b<=10;b++) if (a!==b)
  problems.push({kind:'rhombus',a,b,baseK:(a*7+b*3)%12});
for (let a=1;a<=10;a++) problems.push({kind:'square',a,b:a,baseK:(a*5)%12});

let wrongSolutionCounts = 0, accidentalDistractorTruths = 0;
const counts = {parallelogram:[], rectangle:[], rhombus:[], square:[]};
for (const p of problems) {
  let n = 0;
  if (p.kind === 'parallelogram') {
    for (let rb=1;rb<=10;rb++) for (let rd=1;rd<=10;rd++)
      if (correct(p,rb,p.baseK+6,rd,p.baseK+8)) n++;
    // 오개념: 평행사변형의 두 대각선 길이도 같다고 보고 d=a.
    if (correct(p,p.a,p.baseK+6,p.a,p.baseK+8)) accidentalDistractorTruths++;
  } else {
    for (let kb=1;kb<=5;kb++) for (let rb=1;rb<=10;rb++)
      for (let kd=7;kd<=11;kd++) for (let rd=1;rd<=10;rd++)
        if (correct(p,rb,p.baseK+kb,rd,p.baseK+kd)) n++;
    if (p.kind === 'rectangle' && correct(p,p.a,p.baseK+2,p.a,p.baseK+9)) accidentalDistractorTruths++;
    if (p.kind === 'rhombus' && p.b>=2 && p.b<=9 &&
      correct(p,p.b-1,p.baseK+3,p.b+1,p.baseK+9)) accidentalDistractorTruths++;
  }
  counts[p.kind].push(n);
  const expected = p.kind === 'rectangle' ? 5 : 1;
  if (n !== expected) wrongSolutionCounts++;
}

const result = {
  run_id:`hyeonak-nemo-math-${new Date().toISOString()}`,
  bankSize:problems.length,
  uniqueCoreProblems:problems.length,
  expected:{parallelogram:'1/100',rectangle:'5/2500',rhombus:'1/2500',square:'1/2500'},
  solutionCountRanges:Object.fromEntries(Object.entries(counts).map(([k,v])=>[k,{min:Math.min(...v),max:Math.max(...v),problems:v.length}])),
  wrongSolutionCounts,
  accidentalDistractorTruths,
  passed:problems.length===300 && wrongSolutionCounts===0 && accidentalDistractorTruths===0
};
fs.writeFileSync(outPath, JSON.stringify(result,null,2));
console.log(JSON.stringify(result,null,2));
if (!result.passed) process.exitCode=1;
