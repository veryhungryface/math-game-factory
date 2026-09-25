import fs from 'node:fs';
import vm from 'node:vm';
import crypto from 'node:crypto';

const source=fs.readFileSync('public/g/pane-wipe/engine.js','utf8');
const sandbox={Math,Date};vm.createContext(sandbox);vm.runInContext(source,sandbox);
const E=sandbox.PaneWipeEngine;
const R={engineSha256:crypto.createHash('sha256').update(source).digest('hex'),scope:'Production engine, not browser pointer delivery',errors:[],counts:{pool:E.pool.length,correct:0,distractors:0,geometryPoints:0,angleChecks:0},samples:[],sessions:{},bots:[]};
const assert=(v,m)=>{if(!v&&R.errors.length<150)R.errors.push(m);};
R.sourceDistribution={};
for(const mode of ['point','pair','error','mixed','target']){
  const bins=new Map();for(const p of E.pool.filter(x=>x.mode===mode))bins.set(p.offset,(bins.get(p.offset)||0)+1);
  R.sourceDistribution[mode]={positions:bins.size,minCount:Math.min(...bins.values()),maxCount:Math.max(...bins.values()),minOffset:Math.min(...bins.keys()),maxOffset:Math.max(...bins.keys())};
  assert(bins.size===(mode==='target'?144:198),'source support not full gasket '+mode);
}
const rad=Math.PI/180;
const dist=(a,b)=>Math.hypot(a.x-b.x,a.y-b.y);
const dot=(a,b)=>a.x*b.x+a.y*b.y;
const sub=(a,b)=>({x:a.x-b.x,y:a.y-b.y});
const norm=v=>Math.hypot(v.x,v.y);
function segmentDistance(p,a,b){const v=sub(b,a),w=sub(p,a),t=Math.max(0,Math.min(1,dot(w,v)/dot(v,v)));return Math.hypot(p.x-a.x-t*v.x,p.y-a.y-t*v.y);}
function oracle(p,a){
  if(a.type==='discard')return p.delta!==0||(p.mode==='target'&&p.d!==4);
  if(a.type!=='stroke')return false;
  const g=E.geometry(p),v=sub(a.b,a.a),l=norm(v);if(l<25)return false;
  if(p.mode==='point'?dist(a.a,g.handle)>12+1e-9:segmentDistance(a.a,g.lines[1].a,g.lines[1].b)>12+1e-9)return false;
  if(segmentDistance(a.b,g.lines[0].a,g.lines[0].b)>12+1e-9)return false;
  if(a.points?.some(q=>segmentDistance(q,a.a,a.b)>10+1e-9))return false;
  const deviation=line=>Math.abs(90-Math.acos(Math.max(-1,Math.min(1,dot(sub(line.b,line.a),v)/(dist(line.a,line.b)*l))))/rad);
  if(deviation(g.lines[0])>4+1e-7)return false;
  if(p.mode!=='point'&&deviation(g.lines[1])>4+1e-7)return false;
  return !(p.delta!==0||(p.mode==='target'&&p.d!==4));
}
for(const p of E.pool){
  const g=E.geometry(p),correct=E.correctAction(p);
  assert(Number.isInteger(p.d)&&p.d>=3&&p.d<=6,'integer distance '+p.id);
  assert(p.delta===0||p.delta>8,'nonparallel angle cones overlap '+p.id);
  const lineLength=dist(g.lines[0].a,g.lines[0].b);
  const actualGap=Math.abs((g.lines[0].b.x-g.lines[0].a.x)*(g.lines[1].a.y-g.lines[0].a.y)-(g.lines[0].b.y-g.lines[0].a.y)*(g.lines[1].a.x-g.lines[0].a.x))/lineLength;
  if(!p.delta)assert(Math.abs(actualGap-p.d*26)<1e-7,'wrong represented cm '+p.id);
  for(const q of [...g.vertices,g.handle,g.foot]){R.counts.geometryPoints++;assert(q.x>=12&&q.x<=308&&q.y>=42&&q.y<=378,'geometry outside pane '+p.id);}
  assert(E.judge(p,correct).ok&&oracle(p,correct),'correct rejected '+p.id);R.counts.correct++;
  const bad=E.wrongActions(p);assert(bad.length>=2,'fewer than 2 distractors '+p.id);assert(new Set(bad.map(x=>x.misconceptionId)).size>=2,'fewer than 2 misconception IDs '+p.id);
  for(const b of bad){
    R.counts.distractors++;assert(!E.judge(p,b.action).ok&&!oracle(p,b.action),'accidental true distractor '+p.id+' '+b.misconceptionId);
    if(b.misconceptionId==='visible-noncrossing-is-parallel')assert(p.delta!==0,'nonparallel explanation on parallel lines '+p.id);
    if(b.misconceptionId==='screen-vertical-is-perpendicular')assert(Math.abs(Math.sin(p.theta*rad))>Math.sin(4*rad),'vertical explanation false '+p.id);
    if(b.misconceptionId==='screen-horizontal-is-perpendicular')assert(Math.abs(Math.cos(p.theta*rad))>Math.sin(4*rad),'horizontal explanation false '+p.id);
  }
  if(!p.requiresDiscard){
    for(const deviation of [-4,-3.9,3.9,4,-4.1,4.1]){
      const along=g.gap*Math.tan(deviation*rad);const b={x:g.foot.x+g.u.x*along,y:g.foot.y+g.u.y*along};const a={type:'stroke',a:g.handle,b,points:[g.handle,b]};
      R.counts.angleChecks++;assert(E.judge(p,a).ok===(Math.abs(deviation)<=4),'angle boundary '+p.id+' '+deviation);assert(E.judge(p,a).ok===oracle(p,a),'independent angle oracle '+p.id+' '+deviation);
    }
    const a={type:'stroke',a:g.handle,b:g.foot,points:[g.handle,{x:(g.handle.x+g.foot.x)/2+g.u.x*11,y:(g.handle.y+g.foot.y)/2+g.u.y*11},g.foot]};
    assert(!E.judge(p,a).ok,'curved candidate accepted '+p.id);
  }
  if(p.shape==='square'){
    const lengths=g.vertices.map((a,i)=>dist(a,g.vertices[(i+1)%4]));assert(lengths.every(x=>Math.abs(x-p.d*26)<1e-8),'square side mismatch '+p.id);
    for(let i=0;i<4;i++)assert(Math.abs(dot(sub(g.vertices[(i+3)%4],g.vertices[i]),sub(g.vertices[(i+1)%4],g.vertices[i])))<1e-7,'square angle mismatch '+p.id);
  }
}
for(const n of [40,17,63]){
  const a=E.sampleProblems(n),distinct=new Set(a.map(x=>x.id)).size;
  assert(a.length===n&&distinct/n>=.7,'sample count or variety '+n);
  for(const p of a){assert(p.choices.includes(p.answer),'sample answer missing '+p.id);assert(new Set(p.choices).size===p.choices.length,'sample duplicate choice '+p.id);assert(E.judge(p.problem,p.answerAction).ok,'sample detached from real judge '+p.id);assert(p.answerNumeric===(p.problem.requiresDiscard?0:p.problem.d),'sample numeric '+p.id);}
  R.samples.push({requested:n,returned:a.length,uniqueProblems:distinct,corePromptKinds:new Set(a.map(x=>x.prompt.replace(/\([^)]*\)/g,''))).size});
}
function advance(g,dt=3){g.tick(dt);return g.getState();}
function submitSolved(g){let e=g.submit(E.correctAction(g.current()));if(e.kind==='blocked'){const before=g.getState();g.submit({type:'lift'});assert(g.getState().firstAttempts===before.firstAttempts,'lifting counted as answer');e=g.submit(E.correctAction(g.current()));}return e;}
for(const kind of ['normal','first_wrong_recovery']){
  let complete=0;let minTimeLeft=90;let maxTimeLeft=0;const finals=[];
  for(let seed=1;seed<=200;seed++){
    const g=E.create(seed);g.start({tutorial:false});
    if(kind==='first_wrong_recovery'){const e=g.submit({type:'discard'});assert(e.kind==='wrong'&&g.getState().lives===2,'firstwrong did not cost one');advance(g);}
    for(let n=0;n<8;n++){const e=submitSolved(g);assert(e.kind==='correct','correct session path '+seed+' '+n);advance(g);}
    const s=g.getState();assert(s.phase==='won'&&s.solved===8&&s.lives===(kind==='normal'?3:2),'session fail '+kind+' '+seed);if(s.phase==='won')complete++;
    assert(s.labels.length===5,'five square family labels missing');minTimeLeft=Math.min(minTimeLeft,s.timeLeft);maxTimeLeft=Math.max(maxTimeLeft,s.timeLeft);finals.push({seed,lives:s.lives,solved:s.solved,score:s.score,firstAttempts:s.firstAttempts,firstCorrect:s.firstCorrect,timeLeft:s.timeLeft});
  }
  R.sessions[kind]={played:200,completed:complete,minTimeLeft,maxTimeLeft,finals};
}
{
  const g=E.create(801);g.start({tutorial:false});g.submit({type:'discard'});advance(g);g.submit({type:'lift'});g.submit(E.correctAction(g.current()));advance(g);const prior=g.getState().lives;g.submit({type:'discard'});assert(g.getState().lives===prior-1,'retry credit leaked into next question');advance(g);g.submit({type:'discard'});advance(g);const before=g.getState();g.submit(E.correctAction(g.current()));g.tick(100);const after=g.getState();assert(before.phase==='lost'&&after.phase==='lost'&&after.lives===0&&after.score===before.score,'life zero recovered or accepted action');
}
{
  const g=E.create(802);g.start();g.tick(100);assert(g.getState().phase==='tutorial'&&g.getState().timeLeft===90,'tutorial not frozen');g.submit({type:'discard'});advance(g);assert(g.getState().lives===3&&g.getState().timeLeft===90,'tutorial lost resource');g.submit(E.correctAction(g.current()));advance(g);assert(g.getState().phase==='playing'&&g.getState().solved===0&&g.getState().firstAttempts===0&&g.getState().timeLeft===90,'tutorial counted as live progress');
}
{
  let blockedChecks=0,liftPasses=0,relocationPasses=0;
  for(let seed=1;seed<=200;seed++)for(const strategy of ['lift','relocate']){
    const g=E.create(seed+90000);g.start({tutorial:false});
    for(let n=0;n<8;n++){
      const p=g.current(),before=g.getState();
      if(p.mode==='error'){
        const correct=E.correctAction(p),ev=g.submit(correct),after=g.getState();
        assert(ev.kind==='blocked'&&after.score===before.score&&after.firstAttempts===before.firstAttempts&&after.lives===before.lives,'stain did not block before grading');blockedChecks++;
        if(strategy==='lift'){const t=g.getState().timeLeft;g.submit({type:'lift'});assert(Math.abs(g.getState().timeLeft-(t-.8))<1e-7,'lift time cost mismatch');assert(g.getState().marks.length>0,'lifting silently erased stain');assert(g.submit(correct).kind==='correct','lift did not allow correct stroke');liftPasses++;}
        else {const shape=E.geometry(p),direction=p.offset>0?-1:1,shift=q=>({x:q.x+shape.u.x*30*direction,y:q.y+shape.u.y*30*direction});const a=shift(correct.a),b=shift(correct.b);assert(g.submit({type:'stroke',a,b,points:[a,b]}).kind==='correct','alternate actual source did not bypass stain');relocationPasses++;}
      }else submitSolved(g);
      advance(g);
    }
    assert(g.getState().phase==='won','stain strategy cannot reach victory');
  }
  R.stainStrategies={blockedChecks,liftPasses,relocationPasses,allCompleted:400};
}
function random(seed){let x=seed>>>0;return()=>{x=(1664525*x+1013904223)>>>0;return x/4294967296;};}
function stroke(a,b){return{type:'stroke',a,b,points:[a,b]};}
function angleAction(g,angle,len=104){const a=E.geometry(g.current()).handle;return stroke({...a},{x:a.x+Math.cos(angle*rad)*len,y:a.y+Math.sin(angle*rad)*len});}
const policies=[
  {name:'repeat',note:'visible handle, same upward 104 px stroke',action:(g,k,r)=>angleAction(g,-90)},
  {name:'cycle',note:'visible handle, up/right/down/left fixed 104 px strokes',action:(g,k,r)=>angleAction(g,[-90,0,90,180][k%4])},
  {name:'random',note:'uniform independent start/end in the 320×420 input plane',action:(g,k,r)=>stroke({x:r()*320,y:r()*420},{x:r()*320,y:r()*420})},
  {name:'idle',note:'no input for 91 game seconds',action:()=>null},
  {name:'strong_repeat',note:'visible handle + granted correct stroke length, always upward',action:(g,k,r)=>angleAction(g,-90,g.current().d*26)},
  {name:'strong_cycle_diagonal',note:'visible handle + granted correct length, fixed 8 screen directions',action:(g,k,r)=>angleAction(g,[-90,-45,0,45,90,135,180,225][k%8],g.current().d*26)},
  {name:'strong_random_angle',note:'visible handle + granted correct length, uniform full 360° direction',action:(g,k,r)=>angleAction(g,r()*360,g.current().d*26)},
  {name:'always_discard',note:'every pane discarded without inspecting parallelism or order',action:()=>({type:'discard'})},
];
const baselineCache=new Map();
function finitePoolChance(policy,mode,k){
  if(policy.name==='idle'||policy.name==='always_discard')return 0;
  if(policy.name==='random')return null;
  if(policy.name==='strong_random_angle')return 8/360;
  const key=policy.name+':'+mode+':'+k%8;if(baselineCache.has(key))return baselineCache.get(key);
  const options=E.pool.filter(p=>p.mode===mode);let correct=0;
  for(const p of options){const pretend={current:()=>p};correct+=Number(E.judge(p,policy.action(pretend,k,()=>.5)).ok);}
  const chance=correct/options.length;baselineCache.set(key,chance);return chance;
}
for(const policy of policies){
  const result={name:policy.name,note:policy.note,games:200,firstCorrect:0,firstAttempts:0,presented:0,completed:0,submitted:0,byMode:{},runs:[],finitePoolExpectedCorrect:0,finitePoolVariance:0};
  for(let seed=1;seed<=200;seed++){
    const g=E.create(40000+seed),r=random(88000+seed);g.start({tutorial:false});let k=0;const seen=new Set();
    for(let guard=0;guard<160;guard++){
      const s=g.getState();if(s.phase==='won'||s.phase==='lost')break;if(s.phase==='feedback'){advance(g);continue;}
      const p=g.current();if(!seen.has(p.id)){seen.add(p.id);result.presented++;}
      const actionIndex=k++;const a=policy.action(g,actionIndex,r);if(!a){g.tick(91);continue;}
      const first=s.questionAttempts===0;
      if(first){const chance=finitePoolChance(policy,p.mode,actionIndex);if(chance!==null){result.finitePoolExpectedCorrect+=chance;result.finitePoolVariance+=chance*(1-chance);}}
      const event=g.submit(a);result.submitted++;if(first){const row=result.byMode[p.mode]||(result.byMode[p.mode]={firstAttempts:0,firstCorrect:0,discardRequired:0});row.firstAttempts++;row.firstCorrect+=Number(event.kind==='correct');row.discardRequired+=Number(p.requiresDiscard);}
      g.tick(.25);
    }
    const s=g.getState();result.firstCorrect+=s.firstCorrect;result.firstAttempts+=s.firstAttempts;result.completed+=Number(s.phase==='won');result.runs.push({seed:40000+seed,phase:s.phase,firstCorrect:s.firstCorrect,firstAttempts:s.firstAttempts,solved:s.solved,lives:s.lives});
    assert(s.phase==='won'||s.phase==='lost','bot session did not terminate '+policy.name+' '+seed);
  }
  result.firstRate=result.firstAttempts?result.firstCorrect/result.firstAttempts:0;result.completionRate=result.completed/200;
  result.chanceUpper=policy.name==='idle'||policy.name==='always_discard'?0:policy.name==='strong_random_angle'?8/360:8/180;
  // This direction-only upper bound is valid for independent fixed input or
  // uniform-angle policies on the actual 1800-rotation first band. Endpoint,
  // length and start-point requirements only reduce acceptance. No policy
  // may silently apply it to deterministic final square or discard branches.
  assert(Object.keys(result.byMode).every(m=>m==='point'||m==='pair'||m==='error'),'bot reached branch needing separate chance derivation '+policy.name);
  result.chanceFormula=result.chanceUpper===0?'0':policy.name==='strong_random_angle'?'8° / 360°; endpoint constraints reduce this upper bound':'80 rotations / 1800 rotations = 8° / 180°; endpoint constraints reduce this upper bound';
  result.z=result.chanceUpper&&result.firstAttempts?(result.firstCorrect-result.firstAttempts*result.chanceUpper)/Math.sqrt(result.firstAttempts*result.chanceUpper*(1-result.chanceUpper)):0;
  result.rawAtOrBelowChance=result.firstRate<=result.chanceUpper;
  result.significantExcess=result.z>1.645;
  result.finitePoolChance=policy.name==='random'?null:result.firstAttempts?result.finitePoolExpectedCorrect/result.firstAttempts:0;
  result.finitePoolZ=result.finitePoolVariance?(result.firstCorrect-result.finitePoolExpectedCorrect)/Math.sqrt(result.finitePoolVariance):0;
  result.finitePoolCaveat='Policy-specific pool acceptance is diagnostic only, NOT an independent chance baseline or a pass criterion. Random coordinate input reports only the proven direction upper bound.';
  result.rawAtOrBelowFinitePoolChance=result.finitePoolChance===null?null:result.firstRate<=result.finitePoolChance;
  if(policy.name.startsWith('strong_')){
    result.uniformAngleChance=8/360;result.uniformAngleZ=(result.firstCorrect-result.firstAttempts*result.uniformAngleChance)/Math.sqrt(result.firstAttempts*result.uniformAngleChance*(1-result.uniformAngleChance));
    assert(result.uniformAngleZ<=1.645,'independent uniform-angle significant excess '+policy.name);
  }
  assert(!result.significantExcess,'B3 significant excess '+policy.name);
  R.bots.push(result);
}
R.verdict=R.errors.length?'fail':'pass';
fs.writeFileSync('logs/pane-wipe-build/audit-engine.json',JSON.stringify(R,null,2)+'\n');
console.log(JSON.stringify({...R,sessions:Object.fromEntries(Object.entries(R.sessions).map(([k,v])=>[k,{...v,finals:undefined}])),bots:R.bots.map(x=>({...x,runs:undefined}))},null,2));
if(R.errors.length)process.exitCode=1;
