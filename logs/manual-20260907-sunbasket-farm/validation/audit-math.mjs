import {readFile,writeFile,mkdir} from 'node:fs/promises';
import {resolve} from 'node:path';
import {pathToFileURL} from 'node:url';
import {createHash} from 'node:crypto';
import {oracle,expectedNumericDomain,unusualInputs,policyBaselines} from './math-oracle.mjs';
const gamePath=resolve('public/g/sunbasket-farm/math.mjs');
const out=resolve('logs/manual-20260907-sunbasket-farm/validation');
await mkdir(out,{recursive:true});
const source=await readFile(gamePath);
const sha=createHash('sha256').update(source).digest('hex');
const production=await import(pathToFileURL(gamePath).href+'?sha='+sha);
const {POOL,TUTORIAL,isCorrect,createDay,sampleProblems}=production;
const issues=[];
let predicateChecks=0;
const check=(condition,detail)=>{if(!condition)issues.push(detail);};
const key=p=>`${p.width}:${p.targetBoxes}`;
const expected=expectedNumericDomain();
const domain=new Map(expected.map(p=>[key(p),p]));
check(POOL.length===99,{kind:'pool-size',actual:POOL.length,expected:99});
check(new Set(POOL.map(key)).size===POOL.length,{kind:'duplicate-pool'});
for(const p of POOL){
  check(domain.has(key(p)),{kind:'unexpected-pool-item',p});
  const o=oracle(p,p.height);
  check(o.correct,{kind:'wrong-claimed-answer',p,o});
}
for(const p of expected)check(POOL.some(q=>key(q)===key(p)),{kind:'missing-pool-item',p});
check(TUTORIAL.width===4&&TUTORIAL.height===3&&TUTORIAL.targetBoxes===12&&TUTORIAL.tutorial===true,{kind:'tutorial-contract',TUTORIAL});
for(const p of [...POOL,TUTORIAL]){
  for(const h of [...Array.from({length:16},(_,i)=>i-1),...unusualInputs]){
    const o=oracle(p,h), actual=isCorrect(p,h); predicateChecks++;
    check(actual===o.correct,{kind:'predicate',p,input:String(h),inputType:typeof h,actual,expected:o.correct});
  }
}
function seeded(seed){let counter=0;return()=>createHash('sha256').update(`sunbasket-review:${seed}:${counter++}`).digest().readUInt32BE(0)/4294967296;}
const ordinary=[];
let dayCount=0;
const heightCounts=Object.fromEntries(Array.from({length:10},(_,i)=>[i+3,0]));
const widths=new Set(),answers=new Set(),targets=new Set(),crops=new Set();
function verifyDay(day,label,includeStats){
  dayCount++;
  check(day.length===9,{kind:'day-length',label,length:day.length});
  check(day[0]?.tutorial===true&&key(day[0])==='4:12',{kind:'day-tutorial',label});
  check(new Set(day.map(key)).size===9,{kind:'duplicate-day-numeric-pair',label,ids:day.map(key)});
  for(let i=0;i<day.length;i++){
    const p=day[i],o=oracle(p,p.height);
    check(o.correct,{kind:'day-answer',label,i,p,o});
    check(domain.has(key(p)),{kind:'day-outside-pool',label,i,p});
    check(p.crop===(i<3?'carrot':i<6?'strawberry':'corn'),{kind:'crop-progression',label,i,p});
    if(i>0){
      check(p.tutorial===false,{kind:'ordinary-marked-tutorial',label,i,p});
      check(key(p)!=='4:12',{kind:'tutorial-pair-in-ordinary',label,i});
      if(i<3)check(p.width<=7&&p.height<=7,{kind:'early-size',label,i,p});
      if(i<6)check(p.width<=10,{kind:'middle-size',label,i,p});
      if(includeStats){ordinary.push(p);heightCounts[p.height]++;widths.add(p.width);answers.add(p.height);targets.add(p.targetBoxes);crops.add(p.crop);}
    }
  }
}
for(let seed=1;seed<=5000;seed++)verifyDay(createDay(seeded(seed)),seed,true);
for(const [name,rng] of [['zero',()=>0],['near-one',()=>1-Number.EPSILON]])verifyDay(createDay(rng),name,false);
const samples=sampleProblems(2000),sampleCoverage=new Set();
check(samples.length===2000,{kind:'sample-count',actual:samples.length});
const sampleVerification=[];
for(let i=0;i<samples.length;i++){
  const p=samples[i],o=oracle(p,p.height);sampleCoverage.add(key(p));
  const m=p.prompt.match(/(\d+)상자를 수확하려고 가로가 (\d+)m인 직사각형 밭을 만들 때, 세로는 몇 m여야 하나요/);
  check(!!m,{kind:'unparsed-korean-prompt',i,prompt:p.prompt});
  if(m){const visible=oracle({width:Number(m[2]),targetBoxes:Number(m[1])},p.answerNumeric);check(visible.correct,{kind:'visible-prompt-answer',i,p,visible});}
  check(p.prompt.includes('1m²마다 작물 1상자'),{kind:'missing-yield-assumption',i});
  check(o.correct&&p.answerNumeric===o.expectedHeight&&p.answer===`${o.expectedHeight}m`,{kind:'sample-claimed-answer',i,p,o});
  check(domain.has(key(p))&&p.choices===null,{kind:'sample-domain-or-choice',i,p});
  if(i<99)sampleVerification.push({prompt:p.prompt,claimed:p.answer,my_calculation:o.explanation,correct:o.correct});
}
check(sampleCoverage.size===99,{kind:'sample-coverage',actual:sampleCoverage.size});
const policies=policyBaselines(ordinary);
let randomCorrect=0;const policyRandom=seeded(0x13579bdf);
for(const p of ordinary){const h=1+Math.floor(policyRandom()*12);if(oracle(p,h).correct)randomCorrect++;}
const report={slug:'sunbasket-farm',run_id:'manual-20260907-sunbasket-farm',reviewed_at:new Date().toISOString(),verdict:issues.length?'fail':'pass',math_sha256:sha,scope:'Independent source-pool, predicate, seeded campaign and Korean prompt audit. No browser input verification claimed.',counts:{pool:POOL.length,tutorial:1,predicateChecks,randomCampaigns:5000,edgeRngCampaigns:2,ordinaryQuestions:ordinary.length,sampleProblems:samples.length,sampleNumericCoverage:sampleCoverage.size},coverage:{widths:[...widths].sort((a,b)=>a-b),answers:[...answers].sort((a,b)=>a-b),targetCount:targets.size,crops:[...crops],heightCounts},policies,uniformIntegerPolicySimulation:{seed:'0x13579bdf',correct:randomCorrect,attempts:ordinary.length,rate:randomCorrect/ordinary.length,interpretation:'Independent policy random stream. Compare with exact conditional probability 1/12; this is not actual UI input.'},ordinary_distribution_note:'Five thousand createDay executions using a deterministic SHA-256 seed/counter random stream, eight ordinary questions each. This is an empirical distribution for policy baselines, not an exact closed-form population measure. The early stages restrict width/height, so naive uniform-domain assumptions are inappropriate for fixed values.',sample_domain_note:'sampleProblems uses the same 99-pair POOL as createDay. Ordinary days exclude 4×3=12 because the tutorial already uses it; QA samples include this mathematically valid tutorial pair without ordinary frequency claims.',math_verification:sampleVerification,issues};
await writeFile(resolve(out,'math-source-under-test.mjs'),source);
await writeFile(resolve(out,'math-source-report.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({verdict:report.verdict,sha,counts:report.counts,policies,uniformIntegerPolicySimulation:report.uniformIntegerPolicySimulation,issues},null,2));
if(issues.length)process.exitCode=1;
