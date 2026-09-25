import fs from 'node:fs';
import vm from 'node:vm';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
import { exactPrefixChance } from './chance.mjs';

const source=fs.readFileSync('public/g/star-cork/engine.js','utf8');
const box={module:{exports:{}},console};vm.runInNewContext(source,box);
const E=box.module.exports;
const out='logs/star-cork-build';
const report={at:new Date().toISOString(),engineSha256:crypto.createHash('sha256').update(source).digest('hex'),checks:[],bots:[],normal:[],recovery:[],timeoutRecovery:[]};
const copy=x=>JSON.parse(JSON.stringify(x));
function check(name,fn){try{const detail=fn();report.checks.push({name,pass:true,detail});}catch(e){report.checks.push({name,pass:false,error:e.stack});throw e;}}
function worth(type,p){return type==='big'?p.legendBig:type==='small'?p.legendSmall:p.legendBig/2;}
function plateSum(s){return s.plate.reduce((n,id)=>n+worth(s.candies.find(c=>c.id===id).type,s.problem),0);}
function settle(g){while(g.state.phase==='feedback')g.tick(.05);}
function start(seed){const g=new E.Game();g.setViewport(390);g.start(seed);for(const c of [...g.state.candies])g.shoot(c.id);settle(g);assert.equal(g.state.orderIndex,0);assert.equal(g.state.solved,0);return g;}
function bestSubset(candies,target,p){const dp=new Map([[0,[]]]);for(const c of candies){for(const [sum,list] of [...dp].sort((a,b)=>b[0]-a[0])){const next=sum+worth(c.type,p);if(next<=target&&(!dp.has(next)||dp.get(next).length>list.length+1))dp.set(next,[...list,c.id]);}}return dp.get(target);}
function solve(g){
  let steps=0;
  while(['playing','feedback'].includes(g.state.phase)&&steps++<12000){
    const s=g.state;
    if(s.phase==='feedback'){g.tick(.05);continue;}
    const available=s.candies.filter(c=>c.status!=='gone');
    const want=bestSubset(available,s.problem.target,s.problem);
    assert(want,`no attainable subset at ${s.problem.id} / ${s.elapsed}`);
    const unneeded=s.plate.find(id=>!want.includes(id));
    if(unneeded){g.remove(unneeded);g.tick(.09);continue;}
    const next=s.candies.find(c=>want.includes(c.id)&&c.status==='belt'&&c.progress>=0&&c.progress<=1);
    if(next){g.shoot(next.id);g.tick(.09);}else g.tick(.05);
  }
  assert.equal(g.state.phase,'won',JSON.stringify({phase:g.state.phase,p:g.state.problem.id,lives:g.state.lives,elapsed:g.state.elapsed,log:g.state.attemptLog}));
  assert.equal(g.state.solved,6);
  return {phase:g.state.phase,lives:g.state.lives,score:g.state.score,elapsed:g.state.elapsed,firstAttemptCount:g.state.firstAttemptCount,firstAttemptCorrect:g.state.firstAttemptCorrect};
}

try{
check('finite-pool integer math, distractors, valid compositions, exact null',()=>{
  let wrong=0,prefixStates=0,compositionChecks=0;
  const counts={};
  for(const [kind,pool] of Object.entries(E.pools)){
    counts[kind]=pool.length;
    for(const p of pool){
      assert([10,100].includes(p.legendBig));assert.equal(p.legendSmall,p.legendBig/10);
      assert(Number.isInteger(p.target));
      assert.equal(p.solution.reduce((n,t)=>n+worth(t,p),0),p.target,p.id);
      assert.equal(E.evaluate(p.solution,p),'correct');
      assert(bestSubset([...p.initial,...p.belt].map((type,i)=>({id:i,type})),p.target,p),p.id);
      if(p.rows.length){
        const values=p.rows.map(r=>r.big*p.legendBig+r.small*p.legendSmall+r.half*p.legendBig/2);
        assert(values.every(Number.isInteger));
        if(p.type==='max')assert.equal(p.target,Math.max(...values));
        else if(p.type==='difference')assert.equal(p.target,Math.max(...values)-Math.min(...values));
        else { const row=p.rows.find(r=>p.prompt.includes(r.name));assert(row,p.id+' requested source row');assert.equal(p.target,row.big*p.legendBig+row.small*p.legendSmall+row.half*p.legendBig/2); }
      }
      assert(p.belt.reduce((sum,t)=>sum+worth(t,p),0)>p.target,p.id+' stock surplus');
      assert(p.type==='open_construct'?p.belt.length>=15&&p.belt.length<=17:p.belt.length>=8&&p.belt.length<=12,p.id+' finite belt bounds');
      const d=E.distractors(p);assert(d.length>=2);assert.equal(new Set(d.map(x=>x.value)).size,d.length);
      assert(d.filter(x=>['count-pictures','small-as-big','previous-legend','half-as-one','half-as-whole'].includes(x.misconceptionId)).length>=2,p.id+' misconception count');
      for(const item of d){assert(Number.isInteger(item.value));assert.notEqual(item.value,p.target,p.id+' false distractor');wrong++;}
      const max={big:0,small:0,half:0};for(const t of [...p.initial,...p.belt])max[t]++;
      for(let b=0;b<=max.big;b++)for(let s=0;s<=max.small;s++)for(let h=0;h<=max.half;h++){
        const independent=b*p.legendBig+s*p.legendSmall+h*p.legendBig/2;
        const types=[...Array(b).fill('big'),...Array(s).fill('small'),...Array(h).fill('half')];
        assert.equal(E.evaluate(types,p),independent===p.target?'correct':independent>p.target?'over':'under');compositionChecks++;
      }
      const chance=exactPrefixChance({target:p.target,initial:p.initial.map(t=>worth(t,p)),belt:p.belt.map(t=>worth(t,p)),submitAtEnd:true});
      assert.equal(chance.probability,E.chanceProbability(p),p.id+' chance mismatch');prefixStates+=chance.states;
    }
  }
  return {poolCounts:counts,problems:E.allProblems.length,distractors:wrong,chanceDPStates:prefixStates,compositionChecks};
});
check('QA requested sample sizes and semantic diversity',()=>[40,17,63].map(n=>{
  const g=new E.Game(),before=copy(g.state),samples=g.sampleProblems(n);
  assert.equal(samples.length,n);assert.deepEqual(copy(g.state),before);
  for(const p of samples){assert(p.choices.includes(p.answer));assert.equal(new Set(p.choices).size,p.choices.length);assert.equal(Number(p.answer),p.answerNumeric);assert(E.allProblems.some(x=>x.id===p.problemId));}
  const unique=new Set(samples.map(p=>p.prompt)).size;
  assert(unique/n>=.7,`${n} sample uniqueness ${unique}`);
  return {requested:n,received:samples.length,uniquePrompts:unique,rate:unique/n};
}));
check('normal and one-real-overflow then six-order win, 200 seeds each',()=>{
  for(let seed=1001;seed<=1200;seed++){
    const g=start(seed);report.normal.push({seed,...solve(g)});
    assert.equal(g.state.lives,3);
    const h=start(seed);
    for(let t=0;t<1200&&h.state.lives===3;t++){
      const target=h.state.candies.find(c=>c.status==='belt'&&c.progress>=0&&c.progress<=1);
      if(target)h.shoot(target.id);
      h.tick(.04);
    }
    assert.equal(h.state.lives,2);assert.equal(h.state.solved,0,'all taps unexpectedly solved first order');
    settle(h);report.recovery.push({seed,...solve(h)});assert.equal(h.state.lives,2);
  }
  for(let seed=1001;seed<=1200;seed++){const g=start(seed);while(g.state.lives===3)g.tick(.05);assert.equal(g.state.lives,2);settle(g);report.timeoutRecovery.push({seed,...solve(g)});assert.equal(g.state.lives,2);}
  return {normal:report.normal.length,recovery:report.recovery.length,timeoutRecovery:report.timeoutRecovery.length,maxNormalSeconds:Math.max(...report.normal.map(x=>x.elapsed)),maxRecoverySeconds:Math.max(...report.recovery.map(x=>x.elapsed)),maxTimeoutRecoverySeconds:Math.max(...report.timeoutRecovery.map(x=>x.elapsed))};
});
check('terminal failure remains terminal under tick and ordinary inputs',()=>{
  const g=start(7233);
  for(let i=0;i<10000&&g.state.phase!=='lost';i++){const c=g.state.candies.find(x=>x.status==='belt'&&x.progress>=0&&x.progress<=1);if(c)g.shoot(c.id);g.tick(.08);}
  assert.equal(g.state.phase,'lost');assert.equal(g.state.lives,0);
  const before=copy(g.state);for(let i=0;i<100;i++){g.tick(1);for(const c of g.state.candies)g.shoot(c.id);}
  assert.deepEqual(copy(g.state),before);return {lives:g.state.lives,phase:g.state.phase};
});
check('terminal answer reveal survives the 90-second boundary',()=>{
  const g=new E.Game();g.start(9917,false);g.state.lives=1;g.state.elapsed=89.8;g.state.remaining=.2;g.forceWrong();
  assert.equal(g.state.phase,'feedback');assert.equal(g.state.lives,0);assert.equal(g.state.lastEvent.terminal,true);
  g.tick(.3);assert.equal(g.state.phase,'feedback');assert.equal(g.state.remaining,0);assert(g.state.feedbackRemaining>.8);
  g.tick(.86);assert.equal(g.state.phase,'lost');assert.equal(g.state.lives,0);
  return {phase:g.state.phase,lives:g.state.lives,remaining:g.state.remaining};
});

const policies=['repeat','cycle','random','idle'];
for(const policy of policies){
  const aggregate={policy,games:200,firstAttemptCorrect:0,firstAttemptCount:0,chanceExpected:0,variance:0,completed:0,ordinarySolved:0,sessions:[]};
  for(let seed=1001;seed<=1200;seed++){
    const g=start(seed),random=E.rng(seed*971+23),encountered=new Map();
    let actionNo=0;
    for(let steps=0;steps<4000&&['playing','feedback'].includes(g.state.phase);steps++){
      const s=g.state;
      if(s.orderIndex>=0&&!encountered.has(s.orderIndex))encountered.set(s.orderIndex,{problemId:s.problem.id,chance:policy==='idle'?0:E.chanceProbability(s.problem),correct:false});
      if(policy!=='idle'&&s.phase==='playing'){
        const x=policy==='repeat'?.5:policy==='cycle'?[.125,.375,.625,.875][actionNo++%4]:random();
        // Current 390px UI geometry: canvas x=30+progress*(390-60),
        // radius 34px big/half and 24px small. Only belt-center taps.
        // Every input is the same shoot method as the pointer path; no answers,
        // totals, problem target, legends, color or solution inform the policy.
        const candidates=s.candies.filter(c=>c.status==='belt'&&c.progress>=0&&c.progress<=1&&Math.abs(30+c.progress*330-x*390)<=(c.type==='small'?24:34));
        if(candidates.length){candidates.sort((a,b)=>Math.abs(30+a.progress*330-x*390)-Math.abs(30+b.progress*330-x*390));g.shoot(candidates[0].id);}
      }
      g.tick(policy==='idle'?.10:.08);
    }
    for(const item of g.state.attemptLog)if(!item.tutorial&&item.attempt===1&&item.success)encountered.get(item.orderIndex).correct=true;
    const session={seed,phase:g.state.phase,solved:g.state.solved,lives:g.state.lives,first:[...encountered.values()]};
    aggregate.sessions.push(session);aggregate.completed+=g.state.phase==='won'?1:0;aggregate.ordinarySolved+=g.state.solved;
    for(const row of session.first){aggregate.firstAttemptCount++;aggregate.firstAttemptCorrect+=row.correct?1:0;aggregate.chanceExpected+=row.chance;aggregate.variance+=row.chance*(1-row.chance);}
  }
  aggregate.rate=aggregate.firstAttemptCorrect/aggregate.firstAttemptCount;
  aggregate.chance=aggregate.chanceExpected/aggregate.firstAttemptCount;
  aggregate.z=aggregate.variance?(aggregate.firstAttemptCorrect-aggregate.chanceExpected)/Math.sqrt(aggregate.variance):0;
  aggregate.significantAboveChance=aggregate.z>1.96;
  report.bots.push(aggregate);
}
report.pass=report.checks.every(x=>x.pass)&&report.bots.every(x=>!x.significantAboveChance)&&report.bots.find(x=>x.policy==='idle').ordinarySolved===0;
}catch(e){report.error=e.stack;report.pass=false;}finally{fs.writeFileSync(`${out}/engine-report.json`,JSON.stringify(report,null,2)+'\n');}
console.log(JSON.stringify({...report,normal:report.normal.length,recovery:report.recovery.length,timeoutRecovery:report.timeoutRecovery.length,bots:report.bots.map(({sessions,...rest})=>rest)},null,2));
if(!report.pass)process.exitCode=1;
