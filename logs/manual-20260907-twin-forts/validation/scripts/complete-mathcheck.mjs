import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import assert from 'node:assert/strict';
import {oracle,validateRows} from './oracle.mjs';
const root=process.cwd(),run='manual-20260907-twin-forts';
const base=`${root}/logs/${run}`;
const manifest=JSON.parse(await readFile(`${base}/artifact-manifest.json`));
const qa=JSON.parse(await readFile(`${base}/qa/report.json`));
const source=JSON.parse(await readFile(`${root}/logs/manual-20260907-twin-forts/validation/math-source-report.json`));
for(const file of manifest.files){
 const actual=createHash('sha256').update(await readFile(`${root}/public/g/twin-forts/${file.path}`)).digest('hex');
 assert.equal(actual,file.sha256,`Artifact manifest mismatch: ${file.path}`);
}
assert.equal(source.mathSha256,manifest.files.find(f=>f.path==='math.mjs').sha256);
const rows=qa.problems_sample,checked=validateRows(rows),issues=[...source.issues,...checked.issues];
const verified=rows.map(p=>{
 const match=p.prompt.match(/병사 (\d+)명을 왼쪽과 오른쪽에 (\d+)\s*:\s*(\d+)로/);
 if(!match)throw new Error(`Unparsed prompt ${p.prompt}`);
 const [,total,a,b]=match.map(Number);const answer=oracle(total,a,b);
 const correct=p.answerNumeric===answer.left&&p.answer===`${answer.left}명`&&p.total===total&&p.a===a&&p.b===b;
 if(!correct)issues.push({where:'QA visible prompt/answer',problem:p,calculated:answer});
 return {prompt:p.prompt,claimed:p.answer,my_calculation:answer.expression,correct};
});
const gameSource=await readFile(`${root}/public/g/twin-forts/game.js`,'utf8');
assert.match(gameSource,/import\s*\{campaign,isCorrect,sampleProblems,POOL\}\s*from\s*'\.\/math\.mjs'/);
const result={slug:'twin-forts',verdict:issues.length?'fail':'pass',reviewed_at:new Date().toISOString(),
 reviewer:'Independent Codex researcher/validator; BigInt oracle separate from production implementation',
 run_id:run,artifact_hash:manifest.artifact_sha256,artifact_manifest:`logs/${run}/artifact-manifest.json`,
 curriculum:{grade:6,semester:2,unit:'g6s2-u4',standard:'[6수02-05]',
  standardText:'비례배분을 알고, 주어진 양을 비례배분 할 수 있다.',
  textbookLesson:'6-2 4단원 6차시: 비례배분을 해 볼까요 (익힘책 60쪽)',alignment:'direct'},
 methods:['Independent BigInt N*a/(a+b), N-left and left*b==right*a',
  'All 596 public pool rows + fixed tutorial; exhaustive integer allocations and invalid inputs',
  '5,000 actual campaign() calls; 40,000 ordinary questions; tutorial excluded from first-attempt baselines',
  'sampleProblems(40/17/63) source pool membership and visible prompt parsing',
  `Final QA combined ${rows.length} samples re-parsed from Korean prompts and independently recalculated`,
  'Production game imports same campaign/isCorrect/sampleProblems/POOL from math.mjs; math source hash unchanged'],
 source_report:'logs/manual-20260907-twin-forts/validation/math-source-report.json',
 qa_report:`logs/${run}/qa/report.json`,qaSampleCount:rows.length,
 pool:source.pool,campaigns:source.campaigns,predicateChecks:source.predicateChecks,
 sampleChecks:source.sampleChecks,policyBaselines:source.policyBaselines,
 arithmeticBounds:'N<=90 and a,b<=9; production integer cross-products are <=810, exactly representable as JavaScript integers.',
 edgeCases:['Left/right reversals','Unreduced ratios','Equal ratios','All allocations 0..N','Nonintegral/out-of-range submissions',
  'Valid unreduced ratios whose sum does not divide total: 15 at 2:4 -> 5 and 10'],
 math_verification:verified,issues,limitations:['This verdict concerns mathematics and source/sample linkage. Actual UI play, onboarding, and game quality are independently assessed in review.json.']};
await writeFile(`${base}/mathcheck.json`,JSON.stringify(result,null,2));
console.log(JSON.stringify({verdict:result.verdict,artifact_hash:result.artifact_hash,qaSamples:rows.length,pool:result.pool.count,predicateChecks:result.predicateChecks,issues,output:`${base}/mathcheck.json`},null,2));
