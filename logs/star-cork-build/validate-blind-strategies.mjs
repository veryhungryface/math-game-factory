import fs from 'node:fs';
import vm from 'node:vm';
import crypto from 'node:crypto';
import assert from 'node:assert/strict';
const src=fs.readFileSync('public/g/star-cork/engine.js','utf8');const box={module:{exports:{}},console};vm.runInNewContext(src,box);const E=box.module.exports;
const bands=['construct','read','difference','scaled','repair','open'];
const caps=bands.map(key=>{const counts={};for(const p of E.pools[key])counts[p.target]=(counts[p.target]||0)+1;return{key,counts,chance:Math.max(...Object.values(counts))/E.pools[key].length};});
const report={at:new Date().toISOString(),engineSha256:crypto.createHash('sha256').update(src).digest('hex'),nullModel:'Best possible fixed final value without reading the target, conditional on the known band. Targets are uniform within each band; copied initial/stock clues checked separately.',bands:caps,strategies:[]};
for(const policy of ['two_big_four_small','stock_minus_one_big_three_small']){
  const result={policy,games:200,correct:0,first:0,expected:0,variance:0,completed:0,sessions:[]};
  for(let seed=1001;seed<=1200;seed++){
    const g=new E.Game();g.start(seed);for(const c of [...g.state.candies])g.shoot(c.id);while(g.state.phase==='feedback')g.tick(.05);
    const encounters=new Map();let quota=null,attemptKey='';
    for(let step=0;step<2500&&['playing','feedback'].includes(g.state.phase);step++){
      const s=g.state;if(!encounters.has(s.orderIndex))encounters.set(s.orderIndex,{id:s.problem.id,chance:caps[s.orderIndex].chance,correct:false});
      if(s.phase==='playing'){
        const key=s.orderIndex+':'+s.attempt;
        if(key!==attemptKey){attemptKey=key;const stock={big:0,small:0,half:0};for(const c of s.candies)if(c.status==='belt')stock[c.type]++;quota=policy==='two_big_four_small'?{big:2,small:4,half:0}:{big:Math.max(0,stock.big-1),small:Math.max(0,stock.small-3),half:stock.half};}
        const held={big:0,small:0,half:0};for(const id of s.plate)held[s.candies.find(c=>c.id===id).type]++;
        const remove=s.candies.find(c=>c.status==='plate'&&held[c.type]>quota[c.type]);
        if(remove)g.remove(remove.id);
        else {const c=s.candies.find(c=>c.status==='belt'&&c.progress>=0&&c.progress<=1&&held[c.type]<quota[c.type]);if(c)g.shoot(c.id);}
      }
      g.tick(.05);
    }
    for(const row of g.state.attemptLog)if(!row.tutorial&&row.attempt===1&&row.success)encounters.get(row.orderIndex).correct=true;
    result.completed+=g.state.phase==='won'?1:0;
    result.sessions.push({seed,phase:g.state.phase,solved:g.state.solved,orders:[...encounters.values()]});
    for(const row of encounters.values()){result.first++;result.correct+=row.correct?1:0;result.expected+=row.chance;result.variance+=row.chance*(1-row.chance);}
  }
  result.rate=result.correct/result.first;result.chance=result.expected/result.first;result.z=(result.correct-result.expected)/Math.sqrt(result.variance);result.pass=result.z<=1.96;report.strategies.push(result);
}
report.pass=report.strategies.every(x=>x.pass);fs.writeFileSync('logs/star-cork-build/blind-strategy-report.json',JSON.stringify(report,null,2)+'\n');
console.log(JSON.stringify({...report,strategies:report.strategies.map(({sessions,...rest})=>rest)},null,2));if(!report.pass)process.exitCode=1;
