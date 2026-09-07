// Narrow v2 regression: reuse the independent BigInt oracle, not production answers.
// Run from the repository root after the host freezes the final game and QA report.
import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {pathToFileURL} from 'node:url';
import assert from 'node:assert/strict';
const root=process.cwd(),run='manual-20260907-twin-forts-art-v2';
const base=`${root}/logs/${run}`;
const {oracle}=await import(pathToFileURL(`${root}/logs/manual-20260907-twin-forts/validation/scripts/oracle.mjs`));
const baseline=JSON.parse(await readFile(`${base}/validation/baseline.json`));
const sha=bytes=>createHash('sha256').update(bytes).digest('hex');
const mathSource=await readFile(`${root}/public/g/twin-forts/math.mjs`);
assert.equal(sha(mathSource),baseline.math_sha256_baseline,'math.mjs changed: prior exhaustive validation cannot be inherited');
const qaPath=process.argv[2]??`${base}/qa/report.json`;
const qaBytes=await readFile(qaPath),qa=JSON.parse(qaBytes);
assert.ok(Array.isArray(qa.problems_sample)&&qa.problems_sample.length>0,'Final QA samples missing');
const issues=[],verified=[];
for(const [index,p]of qa.problems_sample.entries()){
 const match=p.prompt.match(/병사 (\d+)명을 왼쪽과 오른쪽에 (\d+)\s*:\s*(\d+)로/);
 if(!match){issues.push({index,issue:'Visible Korean prompt could not be parsed',problem:p});continue;}
 const [,N,a,b]=match.map(Number),calculated=oracle(N,a,b);
 const correct=p.answerNumeric===calculated.left&&p.answer===`${calculated.left}명`&&p.total===N&&p.a===a&&p.b===b;
 verified.push({index,prompt:p.prompt,claimed:p.answer,my_calculation:calculated.expression,correct});
 if(!correct)issues.push({index,issue:'Visible prompt, metadata, or claimed answer disagree',problem:p,calculated});
}
const gameSource=await readFile(`${root}/public/g/twin-forts/game.js`,'utf8');
assert.match(gameSource,/import\s*\{campaign,isCorrect,sampleProblems,POOL\}\s*from\s*'\.\/math\.mjs'/,'Game no longer imports the validated math module');
let artifactHash=null;
try{
 const manifest=JSON.parse(await readFile(`${base}/artifact-manifest.json`));
 for(const file of manifest.files)assert.equal(sha(await readFile(`${root}/public/g/twin-forts/${file.path}`)),file.sha256,`Final artifact mismatch: ${file.path}`);
 artifactHash=manifest.artifact_sha256;
}catch(error){if(error.code!=='ENOENT')throw error;}
const report={slug:'twin-forts',run_id:run,reviewed_at:new Date().toISOString(),verdict:issues.length?'fail':'pass',artifact_hash:artifactHash,
 scope:'Final v2 math-source invariance and independent recalculation of final QA prompts. No repeat of the previous exhaustive campaign/predicate suite.',
 math_sha256:sha(mathSource),prior_math_sha256:baseline.math_sha256_baseline,math_unchanged:true,
 final_qa_report:qaPath.replace(`${root}/`,''),final_qa_sha256:sha(qaBytes),qaSampleCount:qa.problems_sample.length,
 inherited_evidence:'logs/manual-20260907-twin-forts/mathcheck.json',
 inherited_scope:'596 pool questions + tutorial; 33,341 predicate checks; 5,000 campaigns and 40,000 ordinary questions. These were tested on the same unchanged math source.',
 math_verification:verified,issues,
 limitation:'Unchanged mathematics does not prove that the changed layout, rendering, animation, and actual input still work. Those are covered separately by final v2 QA and real input evidence.'};
await writeFile(`${base}/validation/mathcheck.json`,JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({verdict:report.verdict,math_unchanged:true,qaSampleCount:report.qaSampleCount,artifact_hash:artifactHash,issues:issues.length,output:`logs/${run}/validation/mathcheck.json`}));
