import fs from 'node:fs';import vm from 'node:vm';import crypto from 'node:crypto';
const source=fs.readFileSync('public/g/pane-wipe/engine.js','utf8'),box={};vm.createContext(box);vm.runInContext(source,box);const E=box.PaneWipeEngine;
const lineFraction=(g,f)=>({x:g.lines[0].a.x+(g.lines[0].b.x-g.lines[0].a.x)*f,y:g.lines[0].a.y+(g.lines[0].b.y-g.lines[0].a.y)*f});
const stroke=(a,b)=>({type:'stroke',a,b,points:[a,b]});
const policies=[];
for(const factor of [1,1.5,2,2.5,3])for(const center of ['screen','geometry'])policies.push({name:center+'-center-'+factor,kind:'center',action:p=>{const g=E.geometry(p),c=center==='screen'?{x:160,y:210}:g.center,a=g.handle,b={x:a.x+(c.x-a.x)*factor,y:a.y+(c.y-a.y)*factor};return stroke(a,b);}});
for(const f of [0,.25,1/3,.4,.5,.6,2/3,.75,1])policies.push({name:'visible-gasket-'+f.toFixed(4),kind:'gasket',fraction:f,action:p=>{const g=E.geometry(p);return stroke(g.handle,lineFraction(g,f));}});
const report={engineSha256:crypto.createHash('sha256').update(source).digest('hex'),scope:'Production engine state; spatial policies. Chance for target-line policies is an independently uniform endpoint on the actual target line, NOT the tested policy own pool acceptance.',errors:[],policies:[]};
function uniformEndpointChance(p){
  if(p.requiresDiscard)return 0;
  const g=E.geometry(p),l=Math.hypot(g.lines[0].b.x-g.lines[0].a.x,g.lines[0].b.y-g.lines[0].a.y),u={x:(g.lines[0].b.x-g.lines[0].a.x)/l,y:(g.lines[0].b.y-g.lines[0].a.y)/l},projected=(g.foot.x-g.lines[0].a.x)*u.x+(g.foot.y-g.lines[0].a.y)*u.y;
  const span=g.gap*Math.tan(4*Math.PI/180),lo=Math.max(0,projected-span),hi=Math.min(l,projected+span);
  return Math.max(0,hi-lo)/l;
}
for(const policy of policies){
  const byMode={};for(const p of E.pool){const row=byMode[p.mode]||(byMode[p.mode]={correct:0,total:0});row.total++;row.correct+=Number(E.judge(p,policy.action(p)).ok);}
  const r={name:policy.name,kind:policy.kind,fraction:policy.fraction,games:200,firstCorrect:0,firstAttempts:0,completed:0,expectedCorrect:0,variance:0,poolAcceptance:byMode,runs:[]};
  for(let seed=1;seed<=200;seed++){
    const g=E.create(40000+seed);g.start({tutorial:false});let lastCount=0;
    for(let k=0;k<200;k++){
      const s=g.getState();if(['won','lost'].includes(s.phase))break;if(s.phase==='feedback'){g.tick(3);continue;}
      const p=g.current(),a=policy.action(p),ev=g.submit(a),after=g.getState();
      if(after.firstAttempts>lastCount){const probability=policy.kind==='center'?8/180:uniformEndpointChance(p);r.expectedCorrect+=probability;r.variance+=probability*(1-probability);lastCount=after.firstAttempts;}
      g.tick(.6);
    }
    const s=g.getState();r.firstCorrect+=s.firstCorrect;r.firstAttempts+=s.firstAttempts;r.completed+=Number(s.phase==='won');r.runs.push({seed:40000+seed,phase:s.phase,firstCorrect:s.firstCorrect,firstAttempts:s.firstAttempts,solved:s.solved,lives:s.lives});
    if(!['won','lost'].includes(s.phase))report.errors.push('not terminated '+policy.name+' '+seed);
  }
  r.firstRate=r.firstAttempts?r.firstCorrect/r.firstAttempts:0;r.expectedRate=r.firstAttempts?r.expectedCorrect/r.firstAttempts:0;r.z=r.variance?(r.firstCorrect-r.expectedCorrect)/Math.sqrt(r.variance):0;
  if(r.z>1.645)report.errors.push('significant excess over independent chance '+policy.name);
  r.rawAtOrBelowChance=r.firstRate<=r.expectedRate;
  r.interpretation=policy.kind==='center'?'Compare fixed center following to 8/180 direction upper bound; the full source prior allows some geometrically correct center-directed strokes. Zero was an inappropriate target that created a nonuniform source distribution.':'Null input independently chooses the destination uniformly on the entire actual target line. Chance is intersection length of [projected source − gap*tan4, projected source + gap*tan4] with target segment, divided by segment length. The policy own pool acceptance above is diagnostic only, never used as the chance baseline.';
  report.policies.push(r);
}
report.verdict=report.errors.length?'fail':'pass';fs.writeFileSync('logs/pane-wipe-build/audit-spatial-policies.json',JSON.stringify(report,null,2)+'\n');console.log(JSON.stringify({...report,policies:report.policies.map(({runs,...r})=>r)},null,2));if(report.errors.length)process.exitCode=1;
