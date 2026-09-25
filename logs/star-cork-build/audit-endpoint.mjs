import fs from 'node:fs';import vm from 'node:vm';import crypto from 'node:crypto';import assert from 'node:assert/strict';
const source=fs.readFileSync('public/g/star-cork/engine.js','utf8'),box={};vm.createContext(box);vm.runInContext(source,box);const E=box.StarCork;
const choose=(n,k)=>{let r=1;for(let i=1;i<=k;i++)r=r*(n-i+1)/i;return r;};
const report={sha256:crypto.createHash('sha256').update(source).digest('hex'),problems:E.allProblems.length,mathErrors:[],null:{},constantPolicy:{},recovery:[]};
const v=(t,p)=>t==='big'?p.legendBig:t==='small'?p.legendSmall:p.legendBig/2;
function counts(ts){const c={big:0,small:0,half:0};for(const t of ts)c[t]++;return c;}
for(const [name,ps] of Object.entries(E.pools)){
 let totalChance=0,maxFixed=0;
 for(const p of ps){
  const c=counts([...p.initial,...p.belt]);let ways=0;
  for(let b=0;b<=c.big;b++)for(let s=0;s<=c.small;s++)for(let h=0;h<=c.half;h++)if(b*p.legendBig+s*p.legendSmall+h*p.legendBig/2===p.target)ways+=choose(c.big,b)*choose(c.small,s)*choose(c.half,h);
  const probability=ways/2**(c.big+c.small+c.half);totalChance+=probability;
  if(probability!==E.chanceProbability(p))report.mathErrors.push([p.id,'endpoint probability mismatch',probability,E.chanceProbability(p)]);
 }
 report.null[name]={problems:ps.length,independent50PercentEndpoint:totalChance/ps.length};
 // Whole-value fixed guesses cannot use target/legend/row data. Select exactly
 // one (b,s,h) count triple regardless of which uniform pool target is drawn.
 for(let b=0;b<=7;b++)for(let s=0;s<=12;s++)for(let h=0;h<=1;h++){
  const successes=ps.filter(p=>b*p.legendBig+s*p.legendSmall+h*p.legendBig/2===p.target).length;
  if(successes/ps.length>maxFixed)maxFixed=successes/ps.length;
 }
 report.constantPolicy[name]={maxTargetBlindFixedCompositionPopulationChance:maxFixed};
}
function init(seed){const g=new E.Game();g.setViewport(390);g.start(seed);for(const c of [...g.state.candies])g.shoot(c.id);while(g.state.phase==='feedback')g.tick(.01);return g;}
function play(seed,wrongOrder=-1){const g=init(seed);let injected=false;
 for(let steps=0;steps<15000&&['playing','feedback'].includes(g.state.phase);steps++){
  const s=g.state;
  if(s.phase==='playing'){
   const emptyThis=s.orderIndex===wrongOrder&&!injected;
   if(emptyThis){if(s.plate.length)g.remove(s.plate[s.plate.length-1]);}
   else {
    const need=counts(s.problem.solution),have=counts(g.plateTypes());
    const extra=s.plate.find(id=>{const t=s.candies.find(c=>c.id===id).type;return have[t]>need[t];});
    if(extra)g.remove(extra);else{const c=s.candies.find(c=>c.status==='belt'&&c.progress>=0&&c.progress<=1&&have[c.type]<need[c.type]);if(c)g.shoot(c.id);}
   }
  }
  g.tick(.01);
  if(g.state.phase==='feedback'&&g.state.lastEvent.type==='wrong'&&g.state.orderIndex===wrongOrder)injected=true;
 }
 return {seed,wrongOrder,phase:g.state.phase,solved:g.state.solved,lives:g.state.lives,elapsed:g.state.elapsed,last:g.state.lastEvent.type};
}
for(let i=-1;i<6;i++)report.recovery.push(play(1021,i));
console.log(JSON.stringify(report,null,2));
