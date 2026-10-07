import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const HERE=path.dirname(fileURLToPath(import.meta.url));
const ROOT=path.resolve(HERE,'../../../../..');
const work=process.env.MGF_WORK||'factory/work-lanes/20261008-013556-L1';
const report=JSON.parse(fs.readFileSync(path.resolve(ROOT,work,'qa/jegop-hwaljuro/report.json'),'utf8'));
const problems=report.problems_sample||[];
const errors=[];
let reverseCount=0;

for(const p of problems){
  const areaMode=p.prompt.includes('넓이의 합');
  if(p.prompt.includes('직각을 만드는 두 패널'))errors.push(`${p.id}: 모호한 종전 발문`);
  if(p.unitConcept.includes('역')){
    reverseCount++;
    if(!p.prompt.includes('가장 긴 변의 길이가')||!p.prompt.includes('직각삼각형이 되도록'))errors.push(`${p.id}: 역 조건 누락`);
  }
  if(p.prompt.includes('√'))errors.push(`${p.id}: 중2 근호 사용`);
  const evalChoice=choice=>{
    const nums=(choice.match(/\d+/g)||[]).map(Number);
    if(nums.length!==2)return NaN;
    return areaMode?nums[0]+nums[1]:nums[0]*nums[0]+nums[1]*nums[1];
  };
  const correct=p.choices.filter(c=>evalChoice(c)===p.answerNumeric);
  if(correct.length!==1)errors.push(`${p.id}: 정답쌍 ${correct.length}개`);
  if(evalChoice(p.answer)!==p.answerNumeric)errors.push(`${p.id}: answer 산술 불일치`);
  if(!p.choices.includes(p.answer))errors.push(`${p.id}: answer가 choices에 없음`);

  const raw=[...new Set(p.choices.flatMap(c=>(c.match(/\d+/g)||[]).map(Number)))];
  const sides=areaMode?raw.map(v=>Math.sqrt(v)):raw;
  const c=Math.sqrt(p.answerNumeric);
  if(!Number.isInteger(c)||sides.some(v=>!Number.isInteger(v)))errors.push(`${p.id}: 자연수 길이 아님`);
  if(sides.length!==5)errors.push(`${p.id}: 패널 값 ${sides.length}개`);
  if(sides.some(v=>v>=c))errors.push(`${p.id}: c 이상 오답으로 크기 누설`);
  if(p.unitConcept.includes('역')){
    for(let i=0;i<sides.length;i++)for(let j=i+1;j<sides.length;j++)
      if(sides[i]+sides[j]<=c)errors.push(`${p.id}: 삼각형이 아닌 선택쌍 ${sides[i]},${sides[j]},${c}`);
  }
  const sorted=[...sides].sort((a,b)=>a-b);
  const isPair=(x,y)=>x*x+y*y===p.answerNumeric;
  if(isPair(sorted[0],sorted[1]))errors.push(`${p.id}: smallest-two 우회`);
  if(isPair(sorted.at(-1),sorted.at(-2)))errors.push(`${p.id}: largest/target-nearest 우회`);
}

const out={generated_at:new Date().toISOString(),verified_count:problems.length,reverse_prompt_count:reverseCount,
  checks:['integer identity','one correct pair','answer in choices','middle-2 no radicals','explicit reverse wording','all reverse pairs satisfy triangle inequality','all distractors below c','smallest/largest/target-nearest blocked'],
  errors,verdict:problems.length>=104&&errors.length===0?'pass':'fail'};
fs.writeFileSync(path.join(HERE,'math-recheck-results.json'),JSON.stringify(out,null,2)+'\n');
console.log(JSON.stringify(out,null,2));if(out.verdict!=='pass')process.exit(1);
