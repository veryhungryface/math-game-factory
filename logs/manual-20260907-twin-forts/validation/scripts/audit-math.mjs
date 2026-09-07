import {readFile,writeFile,mkdir} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import assert from 'node:assert/strict';
import {pathToFileURL} from 'node:url';
import {oracle,accepts,validateRows,policyBaselines,selfTest} from './oracle.mjs';

const root=process.cwd();
const modulePath=`${root}/public/g/twin-forts/math.mjs`;
const source=await readFile(modulePath,'utf8');
const math=await import(pathToFileURL(modulePath));
const issues=[];
let predicateChecks=0;
const checked=validateRows([math.TUTORIAL,...math.POOL]);
issues.push(...checked.issues);
const keys=new Set(math.POOL.map(p=>`${p.total}:${p.a}:${p.b}`));
const ids=new Set(math.POOL.map(p=>p.id));
if(keys.size!==math.POOL.length||ids.size!==math.POOL.length) issues.push({where:'pool',issue:'duplicate equations or IDs'});
const ordinary=[];
let seed=0x6322026;
const rng=()=>{seed^=seed<<13;seed^=seed>>>17;seed^=seed<<5;return(seed>>>0)/4294967296;};
for(let run=0;run<5000;run++) {
  const rows=math.campaign(rng);
  assert.equal(rows.length,9);
  assert.equal(rows[0].tutorial,true);
  assert.equal(rows[0].total,30);
  assert.equal(rows[0].a,2);assert.equal(rows[0].b,3);
  const seen=new Set();
  rows.slice(1).forEach((p,index)=>{
    const key=`${p.total}:${p.a}:${p.b}`;
    if(!keys.has(key))issues.push({where:'campaign',run,index,issue:'not from public pool',problem:p});
    if(seen.has(p.id))issues.push({where:'campaign',run,index,issue:'repeated ordinary ID',problem:p});
    seen.add(p.id);
    if(p.tutorial)issues.push({where:'campaign',run,index,issue:'tutorial in ordinary rounds'});
    const calculated=oracle(p.total,p.a,p.b);
    if(p.left!==calculated.left||p.right!==calculated.right) issues.push({where:'campaign',run,index,problem:p,calculated});
    ordinary.push(p);
  });
}
for(const p of [math.TUTORIAL,...math.POOL]) {
  for(let left=0;left<=p.total;left++) {
    const expected=accepts(p.total,p.a,p.b,left),actual=math.isCorrect(p,left);
    predicateChecks++;
    if(actual!==expected)issues.push({where:'predicate',problem:p,left,expected,actual});
  }
  for(const left of [-1,p.total+1,0.5,NaN,Infinity,'12',null,undefined]) {
    predicateChecks++;
    if(math.isCorrect(p,left))issues.push({where:'invalid input',problem:p,left:String(left)});
  }
  if(math.isCorrect(p,p.left,p.right+1)) issues.push({where:'wrong total',problem:p});
}
// Rejecting a valid but unreduced ratio would be a mathematical defect even if
// the production generator does not currently emit that representation.
for(const p of [{total:15,a:2,b:4},{total:14,a:9,b:9}]){
  const expected=oracle(p.total,p.a,p.b);
  if(!math.isCorrect(p,expected.left,expected.right))issues.push({where:'unreduced validity',problem:p});
}
const samples=[];
const sampleChecks=[];
for(const n of [40,17,63]){
 const rows=math.sampleProblems(n);assert.equal(rows.length,n);
 const unique=new Set();
 for(const p of rows){
  const m=p.prompt.match(/병사 (\d+)명을 왼쪽과 오른쪽에 (\d+)\s*:\s*(\d+)로/);
  if(!m){issues.push({where:'prompt parse',problem:p});continue;}
  const [,N,a,b]=m.map(Number);const calculated=oracle(N,a,b);
  const key=`${N}:${a}:${b}`;unique.add(key);
  if(!keys.has(key))issues.push({where:'sample',issue:'not from pool',problem:p});
  if(p.total!==N||p.a!==a||p.b!==b||p.answerNumeric!==calculated.left||p.answer!==`${calculated.left}명`)
    issues.push({where:'sample answer/prompt mismatch',problem:p,calculated});
  samples.push({prompt:p.prompt,claimed:p.answer,calculation:calculated.expression,correct:true});
 }
 sampleChecks.push({requested:n,returned:rows.length,coreUnique:unique.size,coreVariety:unique.size/n});
}
function gcd(a,b){while(b)[a,b]=[b,a%b];return a;}
const summarize=rows=>({count:rows.length,equalRatios:rows.filter(p=>p.a===p.b).length,
 leftLarger:rows.filter(p=>p.a>p.b).length,rightLarger:rows.filter(p=>p.a<p.b).length,
 unreduced:rows.filter(p=>gcd(p.a,p.b)>1).length,
 minTotal:Math.min(...rows.map(p=>p.total)),maxTotal:Math.max(...rows.map(p=>p.total))});
const report={slug:'twin-forts',stage:'independent math source audit; UI validation pending',
 verdict:issues.length?'fail':'pass',created_at:new Date().toISOString(),
 mathSha256:createHash('sha256').update(source).digest('hex'),oracleSelfTest:selfTest(),
 sourceConnection:{gameImport:'pending game.js inspection',campaign:'campaign() selects cloned rows from shuffledPool(); shuffledPool copies POOL',
 sample:'sampleProblems() iterates shuffledPool(), spreading each same POOL row into returned objects'},
 pool:summarize(math.POOL),campaigns:{runs:5000,...summarize(ordinary)},predicateChecks,sampleChecks,
 policyBaselines:policyBaselines(ordinary),issues,math_verification:samples,allPoolVerification:checked.verified};
await mkdir(`${root}/scratchpad/twin-forts-validation`,{recursive:true});
await writeFile(`${root}/scratchpad/twin-forts-validation/math-source-report.json`,JSON.stringify(report,null,2));
console.log(JSON.stringify({...report,math_verification:`${samples.length} rows in report`,allPoolVerification:`${checked.verified.length} rows in report`},null,2));
