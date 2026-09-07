import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {oracle} from './math-oracle.mjs';
const base='logs/manual-20260907-sunbasket-farm',hash=b=>createHash('sha256').update(b).digest('hex');
const sourceReport=JSON.parse(await readFile(`${base}/validation/math-source-report.json`,'utf8'));
const manifest=JSON.parse(await readFile(`${base}/artifact-manifest.json`,'utf8'));
const qaBytes=await readFile(`${base}/qa/report.json`),qa=JSON.parse(qaBytes);
const mathHash=hash(await readFile('public/g/sunbasket-farm/math.mjs'));
const issues=[];
if(mathHash!==sourceReport.math_sha256)issues.push('Math source changed after independent pool audit');
for(const file of manifest.files){if(hash(await readFile(`public/g/sunbasket-farm/${file.path}`))!==file.sha256)issues.push(`Final artifact file changed: ${file.path}`);}
const verification=[];
for(const [index,p]of qa.problems_sample.entries()){
 const m=p.prompt.match(/(\d+)상자를 수확하려고 가로가 (\d+)m인 직사각형 밭을 만들 때, 세로는 몇 m여야 하나요/);
 if(!m){issues.push(`Unparsed final Korean prompt ${index}`);continue;}
 const parsed={targetBoxes:Number(m[1]),width:Number(m[2])},o=oracle(parsed,p.answerNumeric);
 const correct=o.correct&&p.answer===`${o.expectedHeight}m`&&p.height===o.expectedHeight&&parsed.width===p.width&&parsed.targetBoxes===p.targetBoxes&&p.prompt.includes('1m²마다 작물 1상자');
 verification.push({index,prompt:p.prompt,claimed:p.answer,my_calculation:o.explanation,correct});if(!correct)issues.push(`Final prompt or claimed answer mismatch ${index}`);
}
const result={slug:'sunbasket-farm',run_id:'manual-20260907-sunbasket-farm',reviewed_at:new Date().toISOString(),verdict:issues.length?'fail':'pass',artifact_hash:manifest.artifact_sha256,math_sha256:mathHash,math_unchanged_since_pool_audit:mathHash===sourceReport.math_sha256,scope:'Independent integer audit of all source-pool/valid-and-invalid predicate cases, generated campaigns and samples, followed by final QA Korean-prompt recalculation and artifact file hash binding. Browser gameplay is reviewed separately.',source_audit:`${base}/validation/math-source-report.json`,source_audit_counts:sourceReport.counts,qa_report:`${base}/qa/report.json`,qa_sha256:hash(qaBytes),final_qa_samples:verification.length,math_verification:verification,policies:sourceReport.policies,policy_scope:sourceReport.ordinary_distribution_note,issues};
await writeFile(`${base}/mathcheck.json`,JSON.stringify(result,null,2)+'\n');
console.log(JSON.stringify({verdict:result.verdict,artifact_hash:result.artifact_hash,final_qa_samples:result.final_qa_samples,math_sha256:mathHash,issues}));
if(issues.length)process.exitCode=1;
