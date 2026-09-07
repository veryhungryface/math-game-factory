// Independent final polish regression. Optional args: slug, final QA JSON, final artifact manifest.
import {readFile,writeFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import assert from 'node:assert/strict';
import {oracle as fortOracle} from '../../manual-20260907-twin-forts/validation/scripts/oracle.mjs';
import {oracle as farmOracle} from '../../manual-20260907-sunbasket-farm/validation/math-oracle.mjs';
const run='manual-20260907-two-game-polish',base=`logs/${run}`;
const slug=process.argv[2];
assert.ok(['twin-forts','sunbasket-farm'].includes(slug));
const sha=x=>createHash('sha256').update(x).digest('hex');
const source=`public/g/${slug}`,baseline=`scratchpad/two-game-polish-host/baseline/${slug}`;
const [oldMath,newMath,oldGame,newGame]=await Promise.all([readFile(`${baseline}/math.mjs`),readFile(`${source}/math.mjs`),readFile(`${baseline}/game.js`,'utf8'),readFile(`${source}/game.js`,'utf8')]);
const expectedHashes={'twin-forts':'0820163ea9543938ade05d00ffbd94f1c52dd33697846f310e50c81620907e36','sunbasket-farm':'8d440d5b36d58d3146deaa4d155843bc25aebde60fe48adce121b35e87189dd8'};
const names=slug==='twin-forts'?['updateAllocation','prepareAllocation','setAllocation','releaseHold','releaseDrag','plan','start','commit','finishBattle','activate','finish','hookPrepare','updateRecords']:['save','updateGrowth','setPhase','planning','start','setHeight','plant','beginHarvest','updateHarvestMeter','harvestCells','harvestRow','finishDelivery','nextOrder','finish','primaryAction','pointOnGround','dragHandle','releaseInputs','cropScreenPoints','sweepAt','prepareHook'];
const functions=names.map(name=>{const pattern=new RegExp(`^function ${name}\\([^\\n]+$`,'m'),a=oldGame.match(pattern)?.[0],b=newGame.match(pattern)?.[0];return {name,found_before:!!a,found_after:!!b,same:!!a&&!!b&&a===b,before_sha256:a?sha(a):null,after_sha256:b?sha(b):null};});
const geometryNames=slug==='sunbasket-farm'?['updateCamera','cellPosition','projectWorld','localPlacement','placeLabels','resize','updatePlot','updateCargo','updateCrops','animate']:[];
const geometry=geometryNames.map(name=>{const pattern=new RegExp(`^function ${name}\\([^\\n]+$`,'m'),a=oldGame.match(pattern)?.[0],b=newGame.match(pattern)?.[0];return {name,same:!!a&&!!b&&a===b};});
const issues=[];
if(sha(newMath)!==sha(oldMath)||sha(newMath)!==expectedHashes[slug])issues.push('Math source changed: inherited exhaustive audit is invalid.');
for(const f of functions)if(!f.same)issues.push(`Core function differs: ${f.name}`);
for(const f of geometry)if(!f.same)issues.push(`Inherited field/crop geometry function differs: ${f.name}`);
const verification=[];let qaHash=null,artifactHash=null;
if(process.argv[3]){
 const bytes=await readFile(process.argv[3]),qa=JSON.parse(bytes);qaHash=sha(bytes);
 assert.ok(qa.problems_sample?.length,'Final QA sample missing');
 for(const [index,p]of qa.problems_sample.entries()){
  let correct=false,calculation=null;
  if(slug==='twin-forts'){
   const m=p.prompt.match(/병사 (\d+)명을 왼쪽과 오른쪽에 (\d+)\s*:\s*(\d+)로/);
   if(m){const [,N,a,b]=m.map(Number),o=fortOracle(N,a,b);calculation=o.expression;correct=p.answerNumeric===o.left&&p.answer===`${o.left}명`&&p.total===N&&p.a===a&&p.b===b;}
  }else{
   const m=p.prompt.match(/(\d+)상자를 수확하려고 가로가 (\d+)m인 직사각형 밭을 만들 때, 세로는 몇 m여야 하나요/);
   if(m){const targetBoxes=Number(m[1]),width=Number(m[2]),o=farmOracle({width,targetBoxes},p.answerNumeric);calculation=o.explanation;correct=o.correct&&p.answer===`${o.expectedHeight}m`&&p.height===o.expectedHeight&&width===p.width&&targetBoxes===p.targetBoxes&&p.prompt.includes('1m²마다 작물 1상자');}
  }
  verification.push({index,prompt:p.prompt,claimed:p.answer,my_calculation:calculation,correct});
  if(!correct)issues.push(`Final QA visible prompt or answer mismatch at ${index}`);
 }
}
if(process.argv[4]){
 const manifest=JSON.parse(await readFile(process.argv[4]));artifactHash=manifest.artifact_sha256;
 for(const f of manifest.files)if(sha(await readFile(`${source}/${f.path}`))!==f.sha256)issues.push(`Final artifact mismatch: ${f.path}`);
}
const out={slug,run_id:run,reviewed_at:new Date().toISOString(),verdict:issues.length?'fail':process.argv[3]?'pass':'preliminary',math_sha256:sha(newMath),prior_math_sha256:sha(oldMath),math_unchanged:sha(newMath)===sha(oldMath),core_comparison_method:'Exact whole-line comparison of named preexisting one-line function declarations. Changed rendering is reviewed separately.',functions,all_listed_functions_unchanged:functions.every(f=>f.same),inherited_evidence:slug==='twin-forts'?'logs/manual-20260907-twin-forts/mathcheck.json':'logs/manual-20260907-sunbasket-farm/mathcheck.json',inherited_scope:slug==='twin-forts'?'596 pool questions plus tutorial; 33,341 predicate checks; 5,000 campaigns / 40,000 ordinary questions. Same unchanged math SHA.':'99 pool questions plus tutorial; 3,400 predicate checks; 5,000 days / 40,000 ordinary questions and 2,000 samples. Same unchanged math SHA.',qa_report:process.argv[3]??null,qa_sha256:qaHash,final_qa_samples:verification.length,math_verification:verification,artifact_manifest:process.argv[4]??null,artifact_hash:artifactHash,issues,limitation:'Source invariance supports reuse of previous math and state-transition evidence. It does not prove changed render visibility, performance, animation completion, or actual input reachability.'};
out.inherited_geometry_comparison=geometry;
await writeFile(`${base}/${slug}-mathcheck.json`,JSON.stringify(out,null,2)+'\n');
console.log(JSON.stringify({slug,verdict:out.verdict,math_unchanged:out.math_unchanged,core_unchanged:out.all_listed_functions_unchanged,final_qa_samples:verification.length,artifact_hash:artifactHash,issues}));
if(issues.length)process.exitCode=1;
