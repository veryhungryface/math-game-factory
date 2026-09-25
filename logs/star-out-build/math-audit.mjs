import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import { fileURLToPath } from 'node:url';
import { createRuntime } from './runtime-vm.mjs';

// Independent mathematical oracle. Browser rendering and trusted input are outside
// this audit: the runtime executes the unchanged game script in DOM/canvas mocks.
const HERE=path.dirname(fileURLToPath(import.meta.url));
const ROOT=path.resolve(HERE,'../..');
const runtime=createRuntime();
const failures=[],warnings=[];
const key=([i,j])=>[i,j].sort((a,b)=>a-b).join(':');
const same=(a,b)=>JSON.stringify(a)===JSON.stringify(b);
const isDiagonal=(n,i,j)=>i!==j&&j!==(i+1)%n&&j!==(i+n-1)%n;
const expectedPairs=p=>p.discard?[]:Array.from({length:p.n},(_,j)=>j).filter(j=>isDiagonal(p.n,p.marked,j)).map(j=>[p.marked,j]);
const canonical=a=>a.map(key).sort();
const DISCARD_MISCONCEPTION={
  not_polygon:'closed_curve_is_polygon',
  not_regular:'equal_sides_only',
  triangle_discard:'triangle_has_diagonal',
};
function check(ok,code,context){if(!ok)failures.push({code,...context});}
function fresh(p){runtime.evaluate(`Object.assign(S,{phase:'playing',score:0,lives:3,level:1,solved:0,combo:0,maxCombo:0,time:90,frozen:false,tutorial:false,history:[],stars:0,bonusNext:false,discardGuide:false,lastMisconceptionId:null}); localStorage.removeItem('star-out:firstStarAt'); install(${JSON.stringify(p)});`);}

const pools=runtime.expose('POOLS'),all=Object.values(pools).flat();
const totals={poolSize:all.length,endpointCases:0,choiceCases:0,misconceptionCases:0,normalCompletionCases:0,recoveryCases:0,geometryCases:0,annotationCases:0,curvedOutlineCases:0,starIsolationCases:0,runtimeMisconceptionCases:0};
const IDs=new Set();
let minSpacing=Infinity,maxEqualEdgeRelativeError=0;
let irregularGeometry=null;
const unit=JSON.parse(fs.readFileSync(path.join(ROOT,'curriculum/2022-elementary-math.json'),'utf8'));
const textbook=JSON.parse(fs.readFileSync(path.join(ROOT,'curriculum/textbook-structure.json'),'utf8'));
const meta=JSON.parse(fs.readFileSync(path.join(ROOT,'public/g/star-out/meta.json'),'utf8'));
const unitMatch=unit.units.find(x=>x.id===meta.unit.id);
check(!!unitMatch,'curriculum.unit',{unit:meta.unit.id});
for(const code of meta.standards)check(unit.standards.some(s=>s.code===code),'curriculum.standard',{standard:code});
const textbookUnit=textbook.volumes.find(v=>v.volume==='4-2')?.units.find(u=>u.order===6);
check(same(textbookUnit?.lessons.slice(0,3).map(l=>l.title),meta.textbook_alignment.lesson_titles),'curriculum.lessons',{});

// The fixed square is onboarding only. A separate square pool must be reachable as
// the first main-band task so lesson 1 is represented in actual scored play.
const mainOrder=Array.from({length:11},(_,i)=>runtime.expose(`orderFor(${i+1})`));
check(Array.isArray(pools['polygon_diag-4'])&&pools['polygon_diag-4'].length>0,'pool.square_main_exists',{poolSize:pools['polygon_diag-4']?.length||0});
check(mainOrder[0]==='polygon_diag-4','progression.square_first_main',{mainOrder});

for(let ix=0;ix<all.length;ix++){
  const p=all[ix],ctx={id:p.id};
  check(!IDs.has(p.id),'pool.duplicate_id',ctx);IDs.add(p.id);
  check(p.n>=3&&p.n<=6&&Number.isInteger(p.n),'pool.n',ctx);
  check(p.labels.length===p.n&&new Set(p.labels).size===p.n&&p.labels.every(x=>/^[ㄱㄴㄷㄹㅁㅂ]$/.test(x)),'pool.labels',ctx);
  check(same(canonical(p.requiredPairs),canonical(expectedPairs(p))),'pool.required_pairs',ctx);
  check(p.pre.every(pair=>p.requiredPairs.some(q=>key(q)===key(pair)))&&new Set(p.pre.map(key)).size===p.pre.length,'pool.prefill_valid',ctx);
  check(p.pre.length===(p.kind==='reverse_partial'?1:0),'pool.prefill_kind',ctx);
  check(p.discard||p.requiredPairs.length>p.pre.length,'pool.initial_not_solved',ctx);
  const sample=runtime.expose(`sampleProblem(${JSON.stringify(p)},${ix})`);
  const remaining=expectedPairs(p).filter(pair=>!p.pre.some(q=>key(q)===key(pair)));
  const segment=pair=>pair.map(i=>p.labels[i]).join('');
  const plan=pairs=>pairs.map(segment).sort().join(' · ')+' 잇기';
  const expectedAnswer=p.discard?'판을 밖으로 밀기':plan(remaining);
  check(sample.answer===expectedAnswer,'sample.answer',ctx);
  check(sample.answerNumeric===remaining.length,'sample.numeric',ctx);
  check(sample.choices.length===4&&sample.choices.includes(expectedAnswer)&&new Set(sample.choices).size===sample.choices.length,'sample.choices',ctx);
  check(sample.distractors.filter(d=>sample.choices.includes(d.value)&&d.misconceptionId).length>=2,'sample.two_misconception_choices',ctx);
  if(p.kind==='polygon_diag'&&p.n===4)check(sample.figureFacts?.boundary==='line_segments'&&sample.figureFacts?.vertexLabels?.length===4&&sample.figureFacts?.equalEdgeMarks===true&&sample.figureFacts?.equalAngleMarks===true,'sample.square_facts',{...ctx,figureFacts:sample.figureFacts});
  for(const d of sample.distractors){
    totals.misconceptionCases++;
    check(d.value!==expectedAnswer,'distractor.accidental_answer',{...ctx,misconceptionId:d.misconceptionId,value:d.value});
    const pairs=Array.isArray(d.pairs)?d.pairs:[];
    check(pairs.length>0&&pairs.every(pair=>pair.length===2&&pair.every(i=>Number.isInteger(i)&&i>=0&&i<p.n)),'distractor.range',{...ctx,value:d.value,pairs});
    check(d.value===plan(pairs),'distractor.value_matches_pairs',{...ctx,value:d.value,pairs});
    if(d.misconceptionId==='side_is_diagonal')check(!p.discard&&pairs.length===remaining.length&&pairs.some(pair=>!isDiagonal(p.n,...pair)),'distractor.side_provenance',{...ctx,pairs});
    else if(d.misconceptionId==='closed_curve_is_polygon')check(p.kind==='not_polygon','distractor.curve_provenance',ctx);
    else if(d.misconceptionId==='equal_sides_only')check(p.kind==='not_regular','distractor.regular_provenance',ctx);
    else if(d.misconceptionId==='triangle_has_diagonal')check(p.n===3&&pairs.every(pair=>!isDiagonal(3,...pair)),'distractor.triangle_provenance',{...ctx,pairs});
    else check(false,'distractor.unknown_id',{...ctx,misconceptionId:d.misconceptionId});
  }
  totals.choiceCases+=sample.choices.length;
  for(let i=0;i<p.n;i++)for(let j=0;j<p.n;j++){
    fresh(p);
    const outcome=runtime.expose(`(()=>{const accepted=stroke(${i},${j});return {accepted,lives:S.lives,lines:S.plate.lines.length};})()`);
    const valid=isDiagonal(p.n,i,j),existing=p.pre.some(pair=>key(pair)===key([i,j]));
    const accepted=!p.discard&&valid&&i===p.marked&&!existing;
    const wrong=p.discard||!valid;
    check(outcome.accepted===accepted&&outcome.lines===p.pre.length+Number(accepted)&&outcome.lives===3-Number(wrong),'runtime.endpoint',{...ctx,i,j,expected:{accepted,lines:p.pre.length+Number(accepted),lives:3-Number(wrong)},actual:outcome});
    totals.endpointCases++;
  }
  for(const withError of [false,true]){
    fresh(p);
    if(withError)runtime.evaluate(`stroke(${p.marked},${(p.marked+1)%p.n});`);
    if(p.discard)runtime.evaluate('discard();');
    else for(const pair of remaining)runtime.evaluate(`stroke(${pair[0]},${pair[1]});`);
    const outcome=runtime.expose('({score:S.score,lives:S.lives,locked:S.plate.locked,solved:S.solved,combo:S.combo,history:S.history})');
    const expectedScore=p.discard?0:100;
    const expectedSolved=p.discard?0:1;
    const expectedCombo=p.discard?0:1;
    check(outcome.locked&&outcome.score===expectedScore&&outcome.lives===(withError?2:3)&&outcome.solved===expectedSolved&&outcome.combo===expectedCombo&&outcome.history.length===1&&outcome.history[0].firstCorrect===!withError,withError?'runtime.recovery':'runtime.normal',{...ctx,expected:{score:expectedScore,solved:expectedSolved,combo:expectedCombo,lives:withError?2:3},actual:outcome});
    if(withError)totals.recoveryCases++;else totals.normalCompletionCases++;
  }
  fresh(p);
  const g=runtime.expose('geometry()'),v=g.vertices;
  const lengths=v.map((a,i)=>Math.hypot(a.x-v[(i+1)%p.n].x,a.y-v[(i+1)%p.n].y));
  const edgeError=(Math.max(...lengths)-Math.min(...lengths))/Math.max(...lengths);
  maxEqualEdgeRelativeError=Math.max(maxEqualEdgeRelativeError,edgeError);
  const angles=v.map((a,i)=>{const b=v[(i+p.n-1)%p.n],c=v[(i+1)%p.n],u=[b.x-a.x,b.y-a.y],w=[c.x-a.x,c.y-a.y];return Math.acos(Math.max(-1,Math.min(1,(u[0]*w[0]+u[1]*w[1])/(Math.hypot(...u)*Math.hypot(...w)))))*180/Math.PI;});
  for(let i=0;i<p.n;i++)for(let j=i+1;j<p.n;j++)minSpacing=Math.min(minSpacing,Math.hypot(v[i].x-v[j].x,v[i].y-v[j].y));
  check(edgeError<1e-12,'geometry.equal_edges',{...ctx,edgeError});
  check(Math.abs(angles.reduce((a,b)=>a+b,0)-(p.n-2)*180)<1e-8,'geometry.convex_angle_sum',ctx);
  if(p.kind==='not_regular'){
    const groups=[1,2,3,3,2];
    for(let i=0;i<5;i++)for(let j=i+1;j<5;j++)check((Math.abs(angles[i]-angles[j])<1e-8)===(groups[i]===groups[j]),'geometry.angle_groups',{...ctx,i,j,angles});
    irregularGeometry??={vertices:v,edgeLengths:lengths,anglesDegrees:angles,expectedEqualityGroups:groups};
  }else check(Math.max(...angles)-Math.min(...angles)<1e-8,'geometry.regular_angles',ctx);
  totals.geometryCases++;
  // Capture only actual Canvas primitive arguments, without approximating pixels.
  const drawing=runtime.expose(`(()=>{const arcs=[],curves=[];const oldArc=ctx.arc,oldCurve=ctx.quadraticCurveTo;
    ctx.arc=(...args)=>arcs.push(args);ctx.quadraticCurveTo=(...args)=>curves.push(args);
    try{annotations(geometry(),S.plate);panePath(geometry(),S.plate);}finally{ctx.arc=oldArc;ctx.quadraticCurveTo=oldCurve;}
    return {arcs,curves};})()`);
  const expectedCounts=p.kind==='not_polygon'?Array(p.n).fill(0):p.kind==='not_regular'?[1,2,3,3,2]:Array(p.n).fill(1);
  for(let i=0;i<p.n;i++){
    const arcs=drawing.arcs.filter(a=>Math.hypot(a[0]-v[i].x,a[1]-v[i].y)<1e-8);
    check(arcs.length===expectedCounts[i],'render.annotation_group',{...ctx,i,count:arcs.length,expected:expectedCounts[i]});
    for(const a of arcs)check(Math.abs((a[4]-a[3])*180/Math.PI-angles[i])<1e-8,'render.annotation_angle',{...ctx,i,rendered:(a[4]-a[3])*180/Math.PI,actual:angles[i]});
    totals.annotationCases++;
  }
  check(drawing.curves.length===(p.kind==='not_polygon'?1:0),'render.curve_presence',ctx);
  if(p.kind==='not_polygon'){
    const c=drawing.curves[0],a=v[1],b=v[2];
    check(Math.abs((c[0]-a.x)*(b.y-a.y)-(c[1]-a.y)*(b.x-a.x))>1,'render.curve_noncollinear',ctx);
    check(Math.hypot(c[2]-b.x,c[3]-b.y)<1e-8,'render.curve_endpoint',ctx);
  }
  totals.curvedOutlineCases++;
}

// Runtime feedback must identify the misconception that produced the penalty,
// not merely carry IDs in the detached sampleProblems data.
const misconceptionProbes=[
  {p:pools['polygon_diag-4'][0],pair:p=>[p.marked,(p.marked+1)%p.n],expected:'side_is_diagonal'},
  {p:pools['not_polygon-5'][0],pair:p=>[p.marked,(p.marked+2)%p.n],expected:DISCARD_MISCONCEPTION.not_polygon},
  {p:pools['not_regular-5'][0],pair:p=>[p.marked,(p.marked+2)%p.n],expected:DISCARD_MISCONCEPTION.not_regular},
  {p:pools['triangle_discard-3'][0],pair:p=>[p.marked,(p.marked+1)%p.n],expected:DISCARD_MISCONCEPTION.triangle_discard},
];
for(const probe of misconceptionProbes){
  fresh(probe.p);const [i,j]=probe.pair(probe.p);
  const observed=runtime.expose(`(()=>{stroke(${i},${j});return {last:S.lastMisconceptionId,bad:S.plate.bad?.misconceptionId||null};})()`);
  check(observed.last===probe.expected||observed.bad===probe.expected,'runtime.misconception_id',{kind:probe.p.kind,expected:probe.expected,actual:observed});
  totals.runtimeMisconceptionCases++;
}

// Completing five diagonals may reveal a star only on the explicitly announced
// star_bonus plate. A normal pentagon is already finished after its marked-point
// task and must not silently become another scoring path while it exits.
function starAttempt(p){
  fresh(p);
  const remaining=expectedPairs(p).filter(pair=>!p.pre.some(q=>key(q)===key(pair)));
  for(const [i,j]of remaining)runtime.evaluate(`stroke(${i},${j});`);
  const base=runtime.expose('({score:S.score,solved:S.solved,combo:S.combo,stars:S.stars,lines:S.plate.lines.length,star:S.plate.star})');
  for(let i=0;i<p.n;i++)for(let j=i+1;j<p.n;j++)if(isDiagonal(p.n,i,j))runtime.evaluate(`stroke(${i},${j});`);
  return {base,after:runtime.expose("({score:S.score,solved:S.solved,combo:S.combo,stars:S.stars,lines:S.plate.lines.length,star:S.plate.star,stamp:localStorage.getItem('star-out:firstStarAt')})")};
}
const ordinaryStar=starAttempt(pools['polygon_diag-5'][0]);
check(ordinaryStar.after.stars===0&&ordinaryStar.after.star===false&&ordinaryStar.after.score===ordinaryStar.base.score&&ordinaryStar.after.lines===ordinaryStar.base.lines&&!ordinaryStar.after.stamp,'runtime.star_ordinary_blocked',{actual:ordinaryStar});
totals.starIsolationCases++;
const bonusStar=starAttempt(pools['star_bonus-5'][0]);
check(bonusStar.after.stars===1&&bonusStar.after.star===true&&bonusStar.after.score===bonusStar.base.score+300&&bonusStar.after.lines===5&&!!bonusStar.after.stamp,'runtime.star_bonus_only',{actual:bonusStar});
totals.starIsolationCases++;

const sampleChecks=[];
for(const n of [40,17,63]){
  const samples=runtime.expose(`window.__GAME_TEST__.sampleProblems(${n})`);
  // Same visible topology and partly filled segments drive the actual game.
  // Ignore sample IDs and rotation/color when counting distinct tasks.
  const topology=s=>JSON.stringify([s.kind,s.labels,s.labels[s.marked],canonical(s.pre)]);
  const uniqueTopology=new Set(samples.map(topology)).size;
  const normalizedPrompt=s=>s.replace(/\([^)]*\)|\[[^\]]*\]/g,'').replace(/\s+/g,' ').trim();
  const uniquePrompt=new Set(samples.map(s=>normalizedPrompt(s.prompt))).size;
  check(samples.length===n,'samples.count',{n,actual:samples.length});
  check(uniqueTopology/n>=.7&&uniquePrompt/n>=.7,'samples.variety',{n,uniqueTopology,uniquePrompt});
  sampleChecks.push({requested:n,returned:samples.length,distinctTopology:uniqueTopology,distinctPrompt:uniquePrompt,topologyRate:uniqueTopology/n,promptRate:uniquePrompt/n});
}

const sampledWording=runtime.expose('sampleProblem(POOLS["not_regular-5"][0],0).prompt');
if(sampledWording.includes('각은 서로 다른'))warnings.push({code:'wording.nonregular_angles',detail:'각은 서로 다른 may imply five pairwise-distinct angles; actual angles form 1/2/2 equality groups. Prefer 각의 크기가 모두 같지는 않은.'});
warnings.push({code:'scope.standard_4su03_12',detail:'[4수03-12] supports making shapes or tiling and explaining methods. This build covers star construction only; tessellation and explanation are not implemented.'});

const functionNames=['diagonal','pairKey','fixedPlate','install','makeProblem','signature','take','orderFor','nextPlate','sampleProblem','sampleProblems','recordPlate','complete','discardReason','misconceptionFor','wrong','stroke','discard','geometry','annotations','panePath'];
const modelSources=runtime.expose(`[${functionNames.join(',')}].map(f=>f.toString())`);
const evidence={at:new Date().toISOString(),verdict:failures.length?'fail':'pass',...runtime.metadata,
  modelSha256:crypto.createHash('sha256').update(modelSources.join('\n')).digest('hex'),
  auditedFunctions:functionNames,limitations:'Mathematical and unchanged-script behavioral audit in Node VM mocks. Not browser rendering, trusted input, network, performance, or child learning verification. Continuous render coordinates use a 1e-12 relative tolerance only for geometry validation; game answers use integer topology.',
  curriculum:{unit:meta.unit.id,standards:meta.standards,lessons:[1,2,3]},poolSizes:Object.fromEntries(Object.entries(pools).map(([k,v])=>[k,v.length])),totals,
  geometry:{minVertexSpacing390:minSpacing,maxEqualEdgeRelativeError,irregularPentagon:irregularGeometry},sampleChecks,
  accidentalTrueDistractors:failures.filter(x=>x.code==='distractor.accidental_answer').length,failures,warnings};
fs.writeFileSync(path.join(HERE,'math-audit.json'),JSON.stringify(evidence,null,2)+'\n');
console.log(JSON.stringify({verdict:evidence.verdict,modelSha256:evidence.modelSha256,totals,sampleChecks,failures:failures.slice(0,10),warningCount:warnings.length},null,2));
if(failures.length)process.exitCode=1;
