// Run from the isolated repository root. Args: final QA JSON, final artifact manifest JSON.
import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {resolve,relative,dirname} from 'node:path';
import {fileURLToPath} from 'node:url';
import assert from 'node:assert/strict';
import {oracle} from './math-oracle.mjs';
const reviewDir=dirname(fileURLToPath(import.meta.url)),root=resolve(reviewDir,'../../..');
assert.equal(resolve(process.cwd()),root,'Run from the isolated repository root, not the main checkout');
const game=resolve(root,'public/g/sunbasket-farm'),sha=b=>createHash('sha256').update(b).digest('hex');
const baseline=JSON.parse(await readFile(resolve(reviewDir,'baseline.json')));
const [oldSource,newSource,math]=await Promise.all([readFile(resolve(reviewDir,'baseline/game.js'),'utf8'),readFile(resolve(game,'game.js'),'utf8'),readFile(resolve(game,'math.mjs'))]);
const expectedMath=baseline.files.find(f=>f.path==='math.mjs').sha256;
const core=baseline.protected_core_functions.map(f=>{const line=newSource.split('\n').find(l=>l.startsWith(`function ${f.name}(`));return{name:f.name,before_sha256:f.sha256,after_sha256:line?sha(line):null,exact_same:!!line&&sha(line)===f.sha256};});
const extraNames=['updateCamera','cellPosition','projectWorld','localPlacement','placeLabels','resize','updatePlot','updateCargo','updateCrops','animate'];
const geometry=extraNames.map(name=>{const a=oldSource.split('\n').find(l=>l.startsWith(`function ${name}(`)),b=newSource.split('\n').find(l=>l.startsWith(`function ${name}(`));return{name,exact_same:!!a&&!!b&&a===b};});
const oldKey=oldSource.match(/SAVE_KEY\s*=\s*(['"])(.*?)\1/)?.[2],newKey=newSource.match(/SAVE_KEY\s*=\s*(['"])(.*?)\1/)?.[2];
const issues=[];
if(sha(math)!==expectedMath||!baseline.math_matches_previous_exhaustive_audit)issues.push('Mathematics changed: inherited exhaustive evidence is invalid.');
if(!oldKey||oldKey!==newKey)issues.push('Storage key changed.');
const changedCore=core.filter(f=>!f.exact_same).map(f=>f.name);
// Changes in a visual-bearing core function require an explicit separately authored review.
let exceptions=null;
if(changedCore.length){
 try{exceptions=JSON.parse(await readFile(resolve(reviewDir,'core-change-review.json')));}catch{}
 for(const name of changedCore){const item=exceptions?.functions?.find(f=>f.name===name);const current=core.find(f=>f.name===name);if(!item||item.after_sha256!==current.after_sha256||item.verdict!=='pass'||!item.evidence)issues.push(`Unreviewed core-function change: ${name}`);}
}
const qaPath=process.argv[2],manifestPath=process.argv[3],verification=[];let qaHash=null,artifactHash=null;
if(qaPath){
 const bytes=await readFile(resolve(root,qaPath)),qa=JSON.parse(bytes);qaHash=sha(bytes);assert.ok(qa.problems_sample?.length,'Missing final QA samples');
 for(const [index,p]of qa.problems_sample.entries()){
  const m=p.prompt.match(/(\d+)상자를 수확하려고 가로가 (\d+)m인 직사각형 밭을 만들 때, 세로는 몇 m여야 하나요/);
  let calculated=null,correct=false;
  if(m){const targetBoxes=Number(m[1]),width=Number(m[2]),o=oracle({width,targetBoxes},p.answerNumeric);calculated=o.explanation;correct=o.correct&&p.answer===`${o.expectedHeight}m`&&p.height===o.expectedHeight&&p.width===width&&p.targetBoxes===targetBoxes&&p.prompt.includes('1m²마다 작물 1상자');}
  verification.push({index,prompt:p.prompt,claimed:p.answer,my_calculation:calculated,correct});if(!correct)issues.push(`Final QA visible prompt/metadata/answer mismatch at ${index}`);
 }
}
if(manifestPath){
 const manifest=JSON.parse(await readFile(resolve(root,manifestPath)));artifactHash=manifest.artifact_sha256;
 assert.ok(artifactHash&&Array.isArray(manifest.files),'Expected artifact_sha256 and files[]');
 for(const f of manifest.files){assert.ok(!f.path.startsWith('/')&&!f.path.split('/').includes('..'),'Unsafe manifest-relative path');if(sha(await readFile(resolve(game,f.path)))!==f.sha256)issues.push(`Artifact file differs: ${f.path}`);}
}
const final=!!qaPath&&!!manifestPath;
const report={slug:'sunbasket-farm',version:3,run_id:'manual-20260908-sunbasket-v3',reviewed_at:new Date().toISOString(),verdict:issues.length?'fail':final?'pass':'preliminary',baseline_commit:baseline.baseline_commit,math_sha256:sha(math),prior_math_sha256:expectedMath,math_unchanged:sha(math)===expectedMath,core_comparison:{method:'Exact whole-line comparison of named baseline one-line functions. Any changed function requires a separately reviewed, current-hash-bound exception with evidence.',functions:core,all_exact_same:core.every(f=>f.exact_same),change_review:exceptions},geometry_comparison:geometry,storage_key:{before:oldKey,after:newKey,same:oldKey===newKey},inherited_evidence:baseline.prior_audit,inherited_counts:baseline.prior_counts,qa_report:qaPath??null,qa_sha256:qaHash,final_qa_samples:verification.length,math_verification:verification,artifact_manifest:manifestPath??null,artifact_hash:artifactHash,issues,scope:'Reuse of the prior independent exhaustive mathematics audit only on identical math source; fresh BigInt calculation of final QA visible Korean prompts and final artifact hash binding. Input/render/performance evidence is separate. Prior full campaigns are not claimed as rerun.'};
await writeFile(resolve(reviewDir,final?'mathcheck.json':'invariance-preliminary.json'),JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({verdict:report.verdict,math_unchanged:report.math_unchanged,changed_core:changedCore,changed_geometry:geometry.filter(f=>!f.exact_same).map(f=>f.name),final_qa_samples:verification.length,artifact_hash:artifactHash,issues,output:relative(root,resolve(reviewDir,final?'mathcheck.json':'invariance-preliminary.json'))}));
if(issues.length)process.exitCode=1;
