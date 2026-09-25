import fs from 'node:fs';
import vm from 'node:vm';
import crypto from 'node:crypto';
const path='public/g/star-cork/engine.js',source=fs.readFileSync(path,'utf8'),box={};
vm.createContext(box);vm.runInContext(source,box);const E=box.StarCork;
const report={sha256:crypto.createHash('sha256').update(source).digest('hex'),count:E.allProblems.length,errors:[],pools:{}};
const v=(t,p)=>t==='big'?p.legendBig:t==='small'?p.legendSmall:p.legendBig/2;
for(const [type,ps] of Object.entries(E.pools)){
  const stats=report.pools[type]={count:ps.length,minChance:1,maxChance:0,meanChance:0,requestedRowIsMax:0};
  for(const p of ps){
    const s=p.solution.reduce((n,t)=>n+v(t,p),0);if(s!==p.target)report.errors.push([p.id,'solution',s]);
    const cc={big:0,small:0,half:0};for(const t of p.solution)cc[t]++;
    for(const d of E.distractors(p)){if(d.value===p.target)report.errors.push([p.id,'true distractor',d]);let independently;
      if(d.misconceptionId==='count-pictures')independently=cc.big+cc.small+cc.half;
      else if(d.misconceptionId==='small-as-big')independently=(cc.big+cc.small+cc.half)*p.legendBig;
      else if(d.misconceptionId==='previous-legend')independently=cc.big*10+cc.small;
      else if(d.misconceptionId==='half-as-one')independently=cc.big*p.legendBig+cc.small*p.legendSmall+cc.half;
      else if(d.misconceptionId==='half-as-whole')independently=(cc.big+cc.half)*p.legendBig+cc.small*p.legendSmall;
      if(independently!==undefined&&independently!==d.value)report.errors.push([p.id,'misconception formula',d]);
    }
    if(p.rows?.length){const vals=p.rows.map(r=>r.big*p.legendBig+r.small*p.legendSmall+r.half*p.legendBig/2);if(p.type==='max'&&p.target!==Math.max(...vals))report.errors.push([p.id,'max']);if(p.type==='difference'&&p.target!==Math.max(...vals)-Math.min(...vals))report.errors.push([p.id,'difference']);if(p.type==='read'){const i=p.rows.findIndex(r=>p.prompt.includes(r.name));if(i<0||vals[i]!==p.target)report.errors.push([p.id,'named read']);if(vals[i]===Math.max(...vals))stats.requestedRowIsMax++;}}
    const c=E.chanceProbability(p);stats.meanChance+=c/ps.length;stats.minChance=Math.min(stats.minChance,c);stats.maxChance=Math.max(stats.maxChance,c);
  }
}
// No reading target, legend or solution: for first four ordinary rounds,
// ignore first large candy, first three small candies. Half is selected on
// the second ordinary order only. Observe each physical candy at the same
// conveyor crossing; no offscreen inventory inspection.
const runs=[];
for(let seed=1;seed<=200;seed++){
 const g=new E.Game();g.setViewport(390);g.start(seed);for(const c of [...g.state.candies])g.shoot(c.id);while(g.state.phase==='feedback')g.tick(.05);
 let last=-1,seen=new Set(),counts={big:0,small:0,half:0};
 for(let tick=0;tick<3000&&g.state.orderIndex<4&&['playing','feedback'].includes(g.state.phase);tick++){
  const s=g.state;if(s.orderIndex!==last){last=s.orderIndex;seen=new Set();counts={big:0,small:0,half:0};}
  if(s.phase==='playing'){
    const arriving=s.candies.filter(c=>c.status==='belt'&&c.progress>=.82&&c.progress<=1&&!seen.has(c.id)).sort((a,b)=>b.progress-a.progress);
    for(const c of arriving){seen.add(c.id);counts[c.type]++;if(c.type==='big'?counts.big>1:c.type==='small'?counts.small>3:s.orderIndex===1)g.shoot(c.id);if(g.state.phase!=='playing')break;}
  }
  g.tick(.04);
 }
 runs.push({seed,solved:g.state.solved,lives:g.state.lives,phase:g.state.phase,elapsed:g.state.elapsed,first:g.state.attemptLog.filter(x=>!x.tutorial&&x.attempt===1).map(x=>({order:x.orderIndex,success:x.success}))});
}
report.offsetExploit={description:'At a single crossing skip first big and first 3 small; take half only second ordinary order. No target/legend/solution access.',games:runs.length,firstCorrect:runs.reduce((a,r)=>a+r.first.filter(x=>x.success).length,0),firstCount:runs.reduce((a,r)=>a+r.first.length,0),fourOrdersCleared:runs.filter(r=>r.solved>=4).length,maxSeconds:Math.max(...runs.map(r=>r.elapsed)),runs};
console.log(JSON.stringify({...report,offsetExploit:{...report.offsetExploit,runs:undefined}},null,2));
